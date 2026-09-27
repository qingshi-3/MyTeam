import json, statistics, sys
from pathlib import Path

base = Path("design-discussion/04-content-validation/artifacts/player-balance/matchup-diagnostics")
if len(sys.argv) > 1 and sys.argv[1] == "--merge-final":
    original = json.loads((base / "raw.json").read_text(encoding="utf-8"))
    corrected = json.loads((base / "raw-corrected.json").read_text(encoding="utf-8"))
    def key(r): return (r["CaseId"], r["Floor"], r["Kind"], r["Seed"])
    replacements = {key(r): r for r in corrected["Records"]}
    merged = []
    replaced = 0
    for row in original["Records"]:
        if key(row) in replacements:
            item = dict(replacements[key(row)])
            item["SourceArtifact"] = "raw-corrected.json"
            replaced += 1
        else:
            item = dict(row)
            item["SourceArtifact"] = "raw.json"
        merged.append(item)
    if replaced != 30 or len(merged) != 320: raise SystemExit(f"unexpected merge replaced={replaced} rows={len(merged)}")
    final = {"GeneratedFrom": ["raw.json", "raw-corrected.json"], "RecordCount": len(merged),
             "InvalidFixtureHistory": {"artifact": "raw.json", "rows": 30,
                 "scope": ["lab-shield-chain floors 14/15", "lab-crit-attack floors 14/15"],
                 "reason": "fourth melee placement overwrote an occupied deployment cell"},
             "Sources": {"raw.json": original["SourceFingerprint"], "raw-corrected.json": corrected["SourceFingerprint"]},
             "DeterminismPassed": bool(original.get("DeterminismPassed") and corrected.get("DeterminismPassed")),
             "Records": merged}
    (base / "final.json").write_text(json.dumps(final, ensure_ascii=False, indent=2), encoding="utf-8")
    print("wrote final.json")
    raise SystemExit(0)
source = Path(sys.argv[1] if len(sys.argv) > 1 else base / "raw.json")
data = json.loads(source.read_text(encoding="utf-8"))
records = data["Records"]
groups = {}
for row in records:
    key = (row["CaseId"], row["Floor"], row["Kind"])
    groups.setdefault(key, []).append(row)
summary = []
units = {}
for (case, floor, kind), rows in sorted(groups.items()):
    wins = sum(r["Outcome"] == "PlayerVictory" for r in rows)
    summary.append({"case": case, "category": rows[0]["Category"], "floor": floor, "kind": kind,
                    "battles": len(rows), "wins": wins,
                    "median_ticks": statistics.median(r["Ticks"] for r in rows),
                    "median_player_health_ratio": statistics.median(sum(u["FinalHealth"] for u in r["Units"] if u["Team"] == 0 and not u["Temporary"]) / max(1, sum(u["MaxHealth"] for u in r["Units"] if u["Team"] == 0 and not u["Temporary"])) for r in rows),
                    "timeouts": sum(r["Outcome"] == "Timeout" for r in rows)})
    for r in rows:
        for u in r["Units"]:
            if u["Team"] != 0: continue
            k=(case,floor,kind,u["ContentId"])
            units.setdefault(k,[]).append(u)
suffix = "-followup" if source.stem.endswith("followup") else "-corrected" if source.stem.endswith("corrected") else "-final" if source.stem == "final" else "-quick" if data.get("Quick") else ""
out = source.parent / f"summary{suffix}.json"
unit_summary=[]
for (case,floor,kind,content), values in sorted(units.items()):
    unit_summary.append({"case":case,"floor":floor,"kind":kind,"content_id":content,"samples":len(values),
        "median_damage":statistics.median(x["DamageDealt"] for x in values),"median_healing":statistics.median(x["HealingDone"] for x in values),
        "median_health_damage_taken":statistics.median(x["HealthDamageTaken"] for x in values),"median_shield_absorbed":statistics.median(x["ShieldAbsorbed"] for x in values),
        "median_mana_casts":statistics.median(x["ManaCasts"] for x in values),"median_hard_control_ticks":statistics.median(x["HardControlReceivedTicks"] for x in values)})
out.write_text(json.dumps({"source": source.name, "source_fingerprint": data.get("SourceFingerprint", data.get("Sources")), "groups": summary,"units":unit_summary}, ensure_ascii=False, indent=2), encoding="utf-8")
md = ["# 阵容交战自动汇总", "", f"来源：`{source.name}`；共 {len(records)} 场。固定种子筛查不代表自然胜率。", "", "| 阵容 | 分类 | 层 | 节点 | 胜场 | 中位 ticks | 中位余血 | 超时 |", "|---|---|---:|---|---:|---:|---:|---:|"]
for x in summary:
    md.append(f"| {x['case']} | {x['category']} | {x['floor']} | {x['kind']} | {x['wins']}/{x['battles']} | {x['median_ticks']:.0f} | {x['median_player_health_ratio']:.1%} | {x['timeouts']} |")
(source.parent / f"summary{suffix}.md").write_text("\n".join(md)+"\n", encoding="utf-8")
print(f"wrote {out}")
