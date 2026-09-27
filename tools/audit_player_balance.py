"""Read authored combat dependencies and capture the before/after tuning boundary."""
import argparse
import hashlib
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'design-discussion/04-content-validation/artifacts/player-balance'


def read(path):
    return path.read_text(encoding='utf-8-sig')


def refs(path):
    return [ROOT / p[6:] for p in re.findall(r'path="(res://[^"]+)"', read(path))
            if p.endswith(('.tres', '.tscn'))]


def closure(path, seen=None):
    seen = set() if seen is None else seen
    if path in seen:
        return seen
    seen.add(path)
    for child in refs(path):
        if child.exists():
            closure(child, seen)
    return seen


def entries(kind):
    text = read(ROOT / 'content/catalogs/alpha_catalog.tres')
    lookup = dict((i, ROOT / p[6:]) for p, i in re.findall(r'path="(res://[^"]+)" id="([^"]+)"', text))
    line = re.search(r'^' + kind + r' = (.+)$', text, re.M)[1]
    return [lookup[i] for i in re.findall(r'ExtResource\("([^"]+)"\)', line)]


def snapshot():
    heroes, enemies = entries('Heroes'), entries('Enemies')
    hero_graph = set().union(*(closure(p) for p in heroes))
    enemy_graph = set().union(*(closure(p) for p in enemies))
    def gameplay(p):
        return any(v in p.parts for v in ('definitions', 'abilities', 'statuses', 'enemies', 'soldiers'))
    units = []
    for entry in heroes:
        definition = next(p for p in refs(entry) if 'definitions' in p.parts)
        values = dict(re.findall(r'^(\w+) = (.+)$', read(definition), re.M))
        units.append(dict(path=definition.relative_to(ROOT).as_posix(), values=values))
    return dict(units=units,
        shared_gameplay=sorted(p.relative_to(ROOT).as_posix() for p in hero_graph & enemy_graph if gameplay(p)),
        shared_visuals=sorted(p.relative_to(ROOT).as_posix() for p in hero_graph & enemy_graph
                             if 'assets' in p.parts or 'vfx' in p.parts or 'components' in p.parts),
        enemy_resources={p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
                         for p in sorted(enemy_graph) if gameplay(p)})


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('label', choices=['before', 'after'])
    args = parser.parse_args()
    data = snapshot()
    OUT.mkdir(parents=True, exist_ok=True)
    target = OUT / (args.label + '-authoring.json')
    if args.label == 'before' and target.exists():
        raise SystemExit('Refusing to overwrite the original baseline')
    if args.label == 'after':
        baseline = json.loads((OUT / 'before-authoring.json').read_text(encoding='utf-8'))
        changed = [p for p in set(baseline['enemy_resources']) | set(data['enemy_resources'])
                   if baseline['enemy_resources'].get(p) != data['enemy_resources'].get(p)]
        if changed:
            raise SystemExit('Enemy tuning changed: ' + ', '.join(sorted(changed)))
    target.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding='utf-8')
    print(f"{args.label}: {len(data['units'])} published heroes; "
          f"{len(data['enemy_resources'])} enemy gameplay resources; "
          f"{len(data['shared_gameplay'])} shared gameplay / {len(data['shared_visuals'])} shared visuals/components")
