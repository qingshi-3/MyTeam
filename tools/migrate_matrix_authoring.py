#!/usr/bin/env python3
"""Move flat Matrix operation fields into focused Resource parameter blocks.

Only scalar authoring layout changes. Existing operation IDs, nested order and all
values are retained. --check rejects unmigrated assets without writing them.
"""
import argparse
import json
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
GROUPS = json.loads((Path(__file__).with_name('matrix_authoring_fields.json')).read_text())
FIELDS = {field: group for group, fields in GROUPS.items() for field in fields}
SCRIPT = 'res://src/Abilities/Authoring/MatrixAbilityOperationSpec.cs'


def migrate(text):
    match = re.search(r'\[ext_resource type="Script" path="' + re.escape(SCRIPT) + r'" id="([^"]+)"\]', text)
    if not match:
        return text
    operation_script = match[1]
    sections = re.split(r'(?m)(?=^\[(?:sub_resource|resource)(?:\s|\]))', text)
    prefix = sections[0]
    existing = set(re.findall(r'\bid="([^"]+)"', text))
    used = {}
    blocks = []
    def unique(label):
        candidate = label
        index = 2
        while candidate in existing:
            candidate = label + '_' + str(index)
            index += 1
        existing.add(candidate)
        return candidate
    for section in sections[1:]:
        if not re.search(r'^script = ExtResource\("' + re.escape(operation_script) + r'"\)$', section, re.M):
            blocks.append(section)
            continue
        values = {}
        kept = []
        for line in section.splitlines():
            field = re.match(r'^(\w+) = (.*)$', line)
            if field and field[1] in FIELDS:
                values[field[1]] = field[2]
            else:
                kept.append(line)
        if not values:
            blocks.append(section)
            continue
        node_id = re.search(r'\bid="([^"]+)"', kept[0])
        label = node_id[1] if node_id else 'root'
        refs = []
        for group, fields in GROUPS.items():
            selected = [field for field in fields if field in values]
            if not selected:
                continue
            if group not in used:
                used[group] = unique('matrix_' + group.lower())
            script_id = used[group]
            block_id = unique(label + '_' + group.lower())
            blocks.append('[sub_resource type="Resource" id="' + block_id + '"]\nscript = ExtResource("' + script_id + '")\n' +
                          '\n'.join(field + ' = ' + values[field] for field in selected) + '\n\n')
            refs.append(group + ' = SubResource("' + block_id + '")')
        while kept and not kept[-1]:
            kept.pop()
        blocks.append('\n'.join(kept + refs) + '\n\n')
    if not used:
        return text
    prefix = re.sub(r'load_steps="?(\d+)"?', lambda m: 'load_steps=' + str(int(m[1]) + len(used) + sum(
        block.startswith('[sub_resource') for block in blocks) - sum(s.startswith('[sub_resource') for s in sections[1:])), prefix)
    prefix = prefix.rstrip() + '\n'
    for group, script_id in used.items():
        prefix += '[ext_resource type="Script" path="res://src/Abilities/Authoring/Matrix' + group + 'Parameters.cs" id="' + script_id + '"]\n'
    return prefix + '\n' + ''.join(blocks)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=ROOT / 'content')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    changed = []
    for file in sorted(args.root.rglob('*.tres')):
        before = file.read_text()
        after = migrate(before)
        if before == after:
            continue
        changed.append(str(file.relative_to(args.root)))
        if not args.check:
            file.write_text(after)
    print(json.dumps({'mode': 'check' if args.check else 'migrate', 'changed': len(changed), 'files': changed}, ensure_ascii=False))
    return int(args.check and bool(changed))

if __name__ == '__main__':
    raise SystemExit(main())
