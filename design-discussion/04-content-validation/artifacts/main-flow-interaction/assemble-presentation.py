"""Synchronize the presentation source into the self-contained visualization."""
from pathlib import Path
from html.parser import HTMLParser
import re
import subprocess

ROOT = Path(__file__).resolve().parent
NODE = '/Users/qingshi/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node'
PYTHON = '/Users/qingshi/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/bin/python3'
RENDER = '/Users/qingshi/.codex/plugins/cache/openai-bundled/visualize/1.0.45/skills/visualize/scripts/render.py'
p = ROOT / 'prototype.html'
source = p.read_text()
for name, start, end in [
    ('presentation.css', '/* mf-presentation-start */', '/* mf-presentation-end */'),
    ('presentation.js', '// mf-presentation-start', '// mf-presentation-end'),
]:
    a = source.index(start) + len(start)
    b = source.index(end, a)
    content = (ROOT / name).read_text()
    if name == 'presentation.css':
        content += '\n' + (ROOT / 'opening-sample/opening.css').read_text()
    source = source[:a] + '\n' + content + '\n' + source[b:]

class Scripts(HTMLParser):
    def __init__(self):
        super().__init__()
        self.keep = False
        self.items = []
    def handle_starttag(self, tag, attrs):
        if tag == 'script':
            self.keep = dict(attrs).get('type') != 'application/json'
    def handle_endtag(self, tag):
        if tag == 'script':
            self.keep = False
    def handle_data(self, data):
        if self.keep:
            self.items.append(data)

parser = Scripts()
parser.feed(source)
for script in parser.items:
    subprocess.run([NODE, '--check', '-'], input=script, text=True, check=True)
p.write_text(source)
subprocess.run([PYTHON, RENDER, str(p), str(ROOT / 'preview.html'), '--force'], check=True)
print('Presentation synchronized; syntax checked.')
