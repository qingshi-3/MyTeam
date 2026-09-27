"""Assemble the local audition from downloaded official free samples.

Does not run RPG Maker projects or copy their game/runtime assets. The PNG
cell layouts retain the author's blend modes; target flashes/audio are omitted.
"""
from pathlib import Path
from PIL import Image
import json
import re
import shutil
import hashlib

web = Path(__file__).resolve().parent
repo = web.parents[1]
study = repo / '.godot/vfx-market-study'
assets = web / 'assets'
assets.mkdir(exist_ok=True)
frames = {}
database = json.loads((study / 'fa01/FA - MV Demo/data/Animations.json').read_text(encoding='utf-8-sig'))
for ident in ['01_Slash', '07_Cure', '10_Burst']:
    filename = f'60FPS_FA01_{ident}.png'
    target = assets / 'fa01/60FPS' / filename
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(study / 'fa01/60FPS' / filename, target)
    item = next(x for x in database if x and x.get('animation1Name') == filename[:-4])
    frames[ident] = {'frames': item['frames'], 'source': item['name'], 'fps': 60}
(assets / 'png-animations.json').write_text(json.dumps(frames, ensure_ascii=False), encoding='utf-8')
shutil.copy2(study / 'fa01/FA01_Manual.txt', assets / 'fa01/FA01_Manual.txt')
research = repo / '.godot/hero-vfx-research'
for pack in ['slash', 'medic', 'blazeforge', 'strike', 'warrior-monk', 'stormforge']:
    source = study if pack in ['slash', 'medic', 'blazeforge'] else research
    shutil.copytree(source / pack / 'VFX', assets / pack / 'VFX', dirs_exist_ok=True)
    manual = next((source / pack).glob('*Sample_Edition_Manual.txt'))
    shutil.copy2(manual, assets / pack / manual.name)
shutil.copytree(study / 'runtime', assets / 'runtime', dirs_exist_ok=True)

# First authored idle cell, without modifying the original character resource.
resource = (repo / 'assets/donor-units/f3_aymarahealer/frames.tres').read_text(encoding='utf-8')
blocks = re.findall(r'"frames": \[(.*?)\],\s*"loop": [^,]+,\s*"name": &"([^"]+)"', resource, re.S)
idle = next(block for block, name in blocks if name == 'idle')
texture = re.search(r'SubResource\("([^"]+)"\)', idle).group(1)
rect = re.search(r'id="' + texture + r'"\].*?region = Rect2\(([^)]+)\)', resource, re.S).group(1)
x, y, w, h = map(int, rect.split(', '))
sheet = Image.open(repo / 'assets/donor-units/f3_aymarahealer/f3_aymarahealer.png')
sheet.crop((x, y, x+w, y+h)).save(assets / 'actor.png')
sources = {
 'fa01': 'https://dreams-circle.itch.io/fa-01',
 'slash': 'https://dreams-circle.itch.io/evfx-slash',
 'medic': 'https://dreams-circle.itch.io/evfx-medic',
 'blazeforge': 'https://dreams-circle.itch.io/evfx-blazeforge',
 'strike': 'https://dreams-circle.itch.io/evfx-strike',
 'warrior-monk': 'https://dreams-circle.itch.io/evfx-warrior-monk',
 'stormforge': 'https://dreams-circle.itch.io/evfx-stormforge',
 'runtime': 'https://github.com/effekseer/EffekseerForWebGL/tree/master/docs',
}
manifest = {'retrieved':'2026-09-25', 'sources':sources, 'role':'local audition; not game integration', 'files':[]}
manifest['sourceNotes'] = {'strike': 'The official Strike sample contains a manual labeled Slash. Original preserved unchanged; correct credit and terms are on the Strike product page.'}
for file in sorted(assets.rglob('*')):
    if file.is_file() and file.name != 'provenance.json':
        manifest['files'].append({'path':file.relative_to(assets).as_posix(), 'sha256':hashlib.sha256(file.read_bytes()).hexdigest(), 'bytes':file.stat().st_size})
(assets / 'provenance.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Prepared {len(manifest["files"])} files, {sum(x["bytes"] for x in manifest["files"]):,} bytes')
