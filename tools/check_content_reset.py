#!/usr/bin/env python3
"""Check the v1 content boundary without importing Godot or touching saves."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
errors = []
for folder in ("content", "scenes", "tests"):
    for path in (ROOT / folder).rglob("*"):
        if path.is_relative_to(ROOT / "tests/archive"):
            continue
        if path.suffix not in (".tscn", ".tres"):
            continue
        for reference in re.findall(r'path="res://([^"\n]+)"', path.read_text()):
            if not (ROOT / reference).is_file():
                errors.append(f"{path.relative_to(ROOT)}: missing {reference}")

expected_items = {f"v1_equipment_e{n:02}" for n in (2, 3, 6, 7, 10, 11, 15)}
expected_items |= {f"v1_relic_r{n:02}" for n in range(1, 12)}
expected_equipment = {f"v1_equipment_e{n:02}" for n in (2, 3, 6, 7, 10, 11, 15)}
expected_formal_equipment_pool = {f"v1_equipment_e{n:02}" for n in (2, 3, 6, 7, 10, 11)}
expected_relics = {f"v1_relic_r{n:02}" for n in range(1, 12)}

def compare(label, actual, expected):
    if actual != expected:
        errors.append(f"{label} mismatch: {actual ^ expected}")

def authored_ids(folder, pattern, expression):
    result = set()
    for path in (ROOT / folder).glob(pattern):
        match = re.search(expression, path.read_text(), re.MULTILINE)
        if match:
            result.add(match.group(1))
    return result

compare("Current item scene stems",
        {path.stem for path in (ROOT / "content/items").glob("*.tscn")}, expected_items)
compare("Current ItemDefinition IDs",
        authored_ids("content/definitions/items", "*.tres", r'^Id = "([^"]+)"$'), expected_items)
compare("Current EquipmentDefinition IDs",
        authored_ids("content/equipment/definitions", "*.tres", r'^StableId = "([^"]+)"$'), expected_equipment)
compare("Current RelicDefinition IDs",
        authored_ids("content/relics/definitions", "*.tres", r'^StableId = "([^"]+)"$'), expected_relics)
for label, relative_path in (
    ("Current shop item pool", "content/expedition/current/items.tres"),
    ("Current general equipment reward pool", "content/expedition/current/v1_pools/equipment_all.tres"),
    ("Current bounty equipment pool", "content/expedition/current/v1_pools/bounty_equipment.tres"),
):
    pool_text = (ROOT / relative_path).read_text()
    match = re.search(r'^ContentIds = PackedStringArray\(([^)]*)\)$', pool_text, re.MULTILINE)
    actual_pool = re.findall(r'"([^"]+)"', match.group(1)) if match else []
    compare(label, set(actual_pool), expected_formal_equipment_pool)
    if len(actual_pool) != len(expected_formal_equipment_pool):
        errors.append(f"{label} contains duplicate or missing entries.")
compare("Current item catalog entry stems",
        {path.stem for path in (ROOT / "content/catalogs/entries").glob("v1_*.tres")
         if path.stem.startswith(("v1_equipment_", "v1_relic_"))}, expected_items)
compare("Current v1 status IDs",
        authored_ids("content/statuses", "v1_*.tres", r'^StableId = "([^"]+)"$'), {"v1_armor_break"})

expected = {
    "hero_hc38_grit_brawler", "enemy_ee03_trample_brute", "enemy_ee07_furnace_lizard",
    "enemy_ee09_swap_envoy", "soldier_dummy_static", "soldier_dummy_melee",
    "soldier_dummy_ranged", "enemy_resistance_dummy",
}
expected |= {f"prototype_u{n:02}" for n in (1,2,3,4,5,6,7,8,11,13,14,16,17,18,19,20,21,22,25)}
expected |= {
    "enemy_e31_iron_guard", "enemy_e32_wood_archer", "enemy_e33_field_medic", "enemy_e34_ambush_scout",
    "v1_boss_breach", "v1_boss_furnace", "v1_boss_stone_seat",
    "enemy_e31_iron_guard_ember_guard", "enemy_e31_iron_guard_crown_guard",
    "enemy_e32_wood_archer_ember_guard", "enemy_e32_wood_archer_gloam_guard",
    "enemy_e32_wood_archer_crown_guard", "enemy_e33_field_medic_gloam_guard",
    "enemy_e33_field_medic_crown_guard", "enemy_e34_ambush_scout_gloam_guard",
}
trait_ids = {p.stem for p in (ROOT / "content/traits/definitions").glob("*.tres")}
if trait_ids != {"prototype_" + n for n in ("002","009","013","020","032","063","270","756")}:
    errors.append("Only the eight selected prototype traits may be authored.")
actual = {p.stem for folder in ("heroes", "soldiers", "enemies")
          for p in (ROOT / "content" / folder).glob("*.tscn")}
if actual != expected:
    errors.append(f"Skill sample roster mismatch: {actual ^ expected}")
for folder in ("growth", "validation_matrix", "project/pools", "relics/pools"):
    if any(p.is_file() for p in (ROOT / "content" / folder).rglob("*")):
        errors.append(f"Retired recipes or supply still exist: content/{folder}")
for path in (ROOT / "content/expedition").rglob("*.tres"):
    if not path.is_relative_to(ROOT / "content/expedition/current"):
        errors.append(f"Retired expedition content: {path.relative_to(ROOT)}")
if (ROOT / "tests/fixtures/legacy-roster").exists():
    errors.append("The obsolete roster must not survive as an importable fixture.")

for error in errors:
    print(error)
if errors:
    raise SystemExit(1)
print("CONTENT_RESET_REFERENCES_OK zero-legacy-items eight-samples nineteen-prototypes fifteen-encounter-enemies six-formal-equipment seven-authored-equipment eleven-relics eight-new-traits no-dangling-resources no-old-supply")
