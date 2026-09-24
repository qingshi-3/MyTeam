"""Rebuild selected CC0 feedback clips; requires Python and imageio-ffmpeg.

This maintenance script downloads only the files named below, never donor assets.
Run manually from any directory. source-manifest.json pins subsequent rebuilds.
"""
import pathlib, urllib.request, json, hashlib, subprocess, tempfile, sys
import imageio_ffmpeg

root = pathlib.Path(__file__).parent
manifest = root / 'source-manifest.json'
commit = (json.loads(manifest.read_text(encoding='utf-8'))['commit'] if manifest.exists()
          else json.load(urllib.request.urlopen('https://api.github.com/repos/open-duelyst/duelyst/commits/main'))['sha'])
base = f'https://raw.githubusercontent.com/open-duelyst/duelyst/{commit}/'
clips = {
    'arrow_release': ('sfx_neutral_crossbones_attack_swing.m4a', .45),
    'arrow_hit': ('sfx_neutral_crossbones_attack_impact.m4a', .4),
    'melee_swing': ('sfx_f1general_attack_swing.m4a', .4),
    'melee_hit': ('sfx_f1tank_attack_impact.m4a', .45),
    'freeze': ('sfx_spell_icepillar.m4a', .85),
    'shield': ('sfx_spell_forcebarrier.m4a', .7),
    'shield_break': ('sfx_spell_icepillar_melt.m4a', .6),
    'explosion': ('sfx_spell_graspofagony.m4a', .85),
    'summon': ('sfx_spell_nethersummoning.m4a', .85),
    'heal': ('sfx_spell_heal.m4a', .65),
    'skill': ('sfx_spell_innerfocus.m4a', .55),
    'death': ('sfx_neutral_golembloodshard_death.m4a', .6),
    'victory': ('sfx_victory_match.m4a', 1.4),
    'defeat': ('sfx_spell_darkseed.m4a', 1.1),
    'ui_select': ('sfx_ui_select.m4a', .16),
    'ui_error': ('sfx_ui_error.m4a', .35),
    'purchase': ('sfx_gold_reward_1.m4a', .5),
}
ffmpeg = imageio_ffmpeg.get_ffmpeg_exe()
only = set(sys.argv[1:])
entries = ([entry for entry in json.loads(manifest.read_text(encoding='utf-8'))['files']
            if entry['cue'] not in only] if only and manifest.exists() else [])
for cue, (name, duration) in clips.items():
    if only and cue not in only:
        continue
    url = base + 'app/resources/sfx/' + name
    raw = urllib.request.urlopen(url).read()
    with tempfile.TemporaryDirectory(prefix='my-team-audio-') as temp:
        source = pathlib.Path(temp) / name
        source.write_bytes(raw)
        target = root / 'feedback' / (cue + '.ogg')
        target.parent.mkdir(exist_ok=True)
        af = (f'silenceremove=start_periods=1:start_threshold=-48dB:start_silence=0.003,'
              f'atrim=duration={duration},asetpts=PTS-STARTPTS,loudnorm=I=-22:TP=-5:LRA=5,'
              f'afade=t=out:st={max(.03, duration-.1)}:d=0.1')
        subprocess.run([ffmpeg, '-hide_banner', '-loglevel', 'error', '-y', '-i', str(source),
                        '-af', af, '-ac', '1', '-ar', '44100', '-c:a', 'libvorbis', '-q:a', '4', str(target)], check=True)
        entries.append(dict(cue=cue, source_url=url, source_sha256=hashlib.sha256(raw).hexdigest(),
                            file='feedback/' + cue + '.ogg', sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                            max_duration=duration, processing=af))
        print(cue, target.stat().st_size)
(root / 'LICENSE.OpenDuelyst-CC0.txt').write_bytes(urllib.request.urlopen(base + 'LICENSE').read())
manifest.write_text(json.dumps(dict(upstream='https://github.com/open-duelyst/duelyst', commit=commit,
                                   license='CC0-1.0', license_url=base+'LICENSE', files=entries), indent=2)+'\n', encoding='utf-8')
