"""Package existing portraits and sample-only generated material as small WebP assets."""
from pathlib import Path
from PIL import Image
import json, base64, io, re

HERE = Path(__file__).resolve().parent
ROOT = HERE.parent
REPO = ROOT.parents[3]
media = json.loads((ROOT / 'presentation-assets.json').read_text())
sources = json.loads((ROOT / 'presentation-assets-sources.json').read_text())['sources']

def encode(image, size, quality=76):
    image.thumbnail(size, Image.Resampling.LANCZOS)
    out = io.BytesIO()
    image.save(out, 'WEBP', quality=quality, method=6)
    return 'data:image/webp;base64,' + base64.b64encode(out.getvalue()).decode()

# Rebuild from originals, never repeatedly compress already compressed samples.
for key, source in sources.items():
    if source.endswith('.png'):
        p = REPO / source
        if key.startswith('mx'):
            cutout = p.parent / 'cutouts' / p.name
            p = cutout if cutout.exists() else p
        media[key] = encode(Image.open(p), (340,510) if key.startswith('mx') else (700,400) if key=='backdrop' else (100,100), 68)

for key, filename, size in [('opening-backdrop','mustering-hall.png',(1280,720)),('opening-frame','hero-stone-frame.png',(320,480))]:
    media[key] = encode(Image.open(HERE / 'assets' / filename), size, 74)
for key, filename, size in [('opening-ring','portrait-stone-ring.png',(140,140)),('opening-button','button-primary.png',(500,167))]:
    media[key] = encode(Image.open(REPO / 'assets/ui/tavern' / filename), size, 80)
atlas=Image.open(REPO / 'assets/ui/icons/unit-card/material-atlas.png')
for name in ['attack','spell-power','health']:
    text=(REPO / 'content/ui/unit-card-icons' / (name+'.tres')).read_text()
    x,y,w,h=map(int,re.search(r'Rect2\((\d+), (\d+), (\d+), (\d+)\)',text).groups())
    media['stat-'+name]=encode(atlas.crop((x,y,x+w,y+h)),(48,48),85)

# Identical portraits are encoded once, preserving current content bindings.
aliases = {}
seen = {}
for key, value in list(media.items()):
    if value in seen:
        aliases[key] = seen[value]
        del media[key]
    else:
        seen[value] = key
media['_aliases'] = aliases
payload = json.dumps(media,ensure_ascii=False,separators=(',',':'))
(HERE / 'media.json').write_text(payload)
source = (ROOT / 'prototype.html').read_text()
source = re.sub(r'(<script[^>]*id="mf-assets"[^>]*>).*?(</script>)',lambda m:m[1]+payload+m[2],source,flags=re.S)
(ROOT / 'prototype.html').write_text(source)
print('Inline artwork bytes:',len(payload))
