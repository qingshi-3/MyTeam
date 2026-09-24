"""Generate original, deterministic UI click auditions with Python's standard library.

Run manually: python assets/audio/ui-click-study/generate.py
Study files are excluded from Godot imports until a sound is selected.
"""

import hashlib
import json
import math
from pathlib import Path
import random
import struct
import wave


RATE = 48000
ROOT = Path(__file__).resolve().parent


def bandpass(values, frequency, q):
    omega = math.tau * frequency / RATE
    alpha = math.sin(omega) / (2 * q)
    a0 = 1 + alpha
    b0, b2 = alpha / a0, -alpha / a0
    a1, a2 = -2 * math.cos(omega) / a0, (1 - alpha) / a0
    x1 = x2 = y1 = y2 = 0.0
    result = []
    for x in values:
        y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2
        result.append(y)
        x2, x1, y2, y1 = x1, x, y1, y
    return result


def strike(duration, seed, modes, noise_frequency, noise_decay, noise_gain):
    count = round(duration * RATE)
    rng = random.Random(seed)
    noise = bandpass([rng.uniform(-1, 1) for _ in range(count)], noise_frequency, 0.8)
    result = []
    for i in range(count):
        t = i / RATE
        # Several brief, inharmonic body modes make a hard impact without a note.
        body = sum(gain * math.sin(math.tau * frequency * t) * math.exp(-t / decay)
                   for frequency, decay, gain in modes)
        grit = noise[i] * noise_gain * math.exp(-t / noise_decay)
        attack = min(1.0, t / 0.00016)
        release = min(1.0, (count - 1 - i) / (RATE * 0.008))
        result.append((body + grit) * attack * max(0.0, release))
    return result


def add_at(target, source, seconds, gain=1.0):
    offset = round(seconds * RATE)
    for i, value in enumerate(source):
        if offset + i >= len(target):
            break
        target[offset + i] += value * gain


def prepare(values):
    # Remove slow/DC movement, roll off brittle extreme highs, then set headroom.
    hp = []
    coefficient = math.exp(-math.tau * 160 / RATE)
    previous_x = previous_y = 0.0
    for x in values:
        y = coefficient * (previous_y + x - previous_x)
        hp.append(y)
        previous_x, previous_y = x, y
    coefficient = math.exp(-math.tau * 9000 / RATE)
    previous_y = 0.0
    for i, x in enumerate(hp):
        previous_y = (1 - coefficient) * x + coefficient * previous_y
        hp[i] = previous_y * min(1.0, (len(hp) - 1 - i) / (RATE * 0.006))
    gain = 10 ** (-9 / 20) / max(abs(x) for x in hp)
    return [x * gain for x in hp]


def write_wav(name, values):
    path = ROOT / name
    pcm = [round(x * 32767) for x in values]
    assert max(abs(x) for x in pcm) < 32767, name
    assert pcm[0] == pcm[-1] == 0, name
    with wave.open(str(path), 'wb') as output:
        output.setnchannels(1)
        output.setsampwidth(2)
        output.setframerate(RATE)
        output.writeframes(struct.pack('<' + 'h' * len(pcm), *pcm))
    with wave.open(str(path), 'rb') as check:
        assert (check.getnchannels(), check.getsampwidth(), check.getframerate(), check.getnframes()) == (1, 2, RATE, len(values))
    return {
        'file': name,
        'duration_seconds': len(values) / RATE,
        'peak_dbfs': round(20 * math.log10(max(abs(x) for x in pcm) / 32768), 2),
        'rms_dbfs': round(20 * math.log10(math.sqrt(sum(x * x for x in pcm) / len(pcm)) / 32768), 2),
        'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
    }


def main():
    # A: tiny switch, fast bright tick with very little housing resonance.
    light = strike(0.065, 1501,
                   [(1950, 0.0021, 0.55), (3470, 0.0015, 0.42), (5210, 0.0010, 0.2)],
                   4700, 0.0021, 1.5)
    add_at(light, strike(0.035, 1502, [(2780, 0.0014, 0.3)], 5300, 0.0013, 0.65), 0.010, 0.25)

    # B: a harder key mechanism, with a lower housing knock under the click.
    hard = strike(0.085, 1503,
                  [(820, 0.0043, 0.32), (1720, 0.0030, 0.65), (2930, 0.0019, 0.42), (4670, 0.0012, 0.2)],
                  3700, 0.0028, 1.5)
    add_at(hard, strike(0.05, 1504,
                       [(1120, 0.0032, 0.4), (2370, 0.0021, 0.45)],
                       4200, 0.0017, 0.95), 0.014, 0.42)

    # C: two distinct latch contacts, a light catch followed by a firm seat.
    latch = [0.0] * round(0.125 * RATE)
    add_at(latch, strike(0.06, 1505,
                        [(2120, 0.0022, 0.45), (4110, 0.0014, 0.25)],
                        4500, 0.0020, 1.1), 0, 0.58)
    add_at(latch, strike(0.085, 1506,
                        [(930, 0.0040, 0.38), (1840, 0.0028, 0.6), (3280, 0.0017, 0.35)],
                        3900, 0.0024, 1.45), 0.036)

    light, hard, latch = prepare(light), prepare(hard), prepare(latch)
    # The requested ka-ta is specifically B then A. Keep an 80 ms onset gap so
    # the two contacts remain distinct instead of merging into one short tap.
    b_then_a = [0.0] * (round(0.080 * RATE) + len(light))
    add_at(b_then_a, hard, 0)
    add_at(b_then_a, light, 0.080)
    entries = []
    for name, description, values in [
        ('a_light', '轻鼠标点击：细小、短促的开关触点', light),
        ('b_hard', '硬质按键：清晰触点加少量按键壳体敲击', hard),
        ('c_latch', '双段卡扣：轻触后落扣，两个接触点相隔36毫秒', latch),
        ('d_b_then_a', '用户指定咔—嗒：先B硬质按键，80毫秒后A轻鼠标点击', b_then_a),
    ]:
        sample = write_wav(name + '.wav', values)
        preview = [0.0] * round(3.5 * RATE)
        for position in [0.25, 0.90, 1.55, 2.45, 2.62, 2.79, 2.96]:
            add_at(preview, values, position)
        audition = write_wav(name + '_preview.wav', preview)
        entries.append({'id': name, 'design_intent': description, 'sample': sample, 'audition': audition})
        print(f'{name}: {len(values) / RATE:.3f}s, peak {sample["peak_dbfs"]} dBFS; audition 3.5s')
    (ROOT / 'manifest.json').write_text(json.dumps({
        'source': 'Original procedural synthesis; no recordings, downloaded audio, or generative audio model.',
        'format': '48 kHz, mono, signed 16-bit PCM WAV',
        'status': 'Source auditions; runtime selection is recorded in ../ui/source-manifest.json. Subjective listening acceptance is pending.',
        'preview_sequence': 'Three spaced clicks, then four clicks 170 ms apart.',
        'files': entries,
    }, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
