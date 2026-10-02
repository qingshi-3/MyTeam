from pathlib import Path
import hashlib
import json
import re
import shutil
import struct

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[3]
ASSETS = ROOT / "assets/portraits/illustrations"
PLAN = json.loads((BASE / "plan.json").read_text())


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def get(record, *keys):
    return next((record[k] for k in keys if k in record), None)


# Verify the entire batch before touching runtime assets.
for key, value in PLAN["lockedAssets"].items():
    assert digest(ASSETS / (key + ".png")) == value, key
for item in PLAN["items"]:
    key = item["id"]
    source = BASE / "after" / (key + ".png")
    record = json.loads((BASE / "records" / (key + ".json")).read_text())
    data = source.read_bytes()
    assert data[:8] == b"\x89PNG\r\n\x1a\n", key
    assert struct.unpack(">II", data[16:24]) == (1024, 1536), key
    recorded_hash = get(record, "sha256After", "output_sha256", "after_sha256")
    assert recorded_hash == digest(source), key
    assert digest(ASSETS / (key + ".png")) == item["sha256Before"], key

manifest = json.loads((ASSETS / "manifest.json").read_text())
bindings = json.loads((ASSETS / "runtime-bindings.json").read_text())
html = (ASSETS / "index.html").read_text()
replacement = {"date": "2026-09-30", "authorization": PLAN["authorization"],
               "method": "built-in image_gen, individual character edits",
               "lockedAssets": PLAN["lockedAssets"], "items": [], "changedPortraitResources": []}

for item in PLAN["items"]:
    key = item["id"]
    source = BASE / "after" / (key + ".png")
    record = json.loads((BASE / "records" / (key + ".json")).read_text())
    target = ASSETS / (key + ".png")
    shutil.copy2(source, target)
    value = digest(target)
    row = next(r for r in manifest["completed"] if r["id"] == key)
    # The plan preserves all previous metadata and the exact old PNG.
    for field in ["approvalQuote", "approvalDate", "baseCharacterApproval"]:
        row.pop(field, None)
    row.update({
        "prompt": record["prompt"],
        "generatedOriginal": get(record, "generatedOriginal", "generated_source", "generated_original", "generated_path"),
        "generationMethod": "built-in image_gen",
        "generationRecordFile": "../../../design-discussion/04-content-validation/artifacts/portrait-rollout-20260930/records/" + key + ".json",
        "background": "opaque low-saturation softly graded mid-dark field matched to unit palette",
        "revision": "20260930-shared-style-optimization",
        "status": "user_requested_replaced_pending_visual_feedback",
        "userAestheticApproval": False,
        "changeAuthorizationQuote": PLAN["authorization"],
        "visualReview": get(record, "visualReview", "visual_review", "visual_notes", "observations"),
        "editInput": item["beforeFile"], "priorAssetBackup": item["beforeFile"],
        "width": 1024, "height": 1536, "bytes": target.stat().st_size,
        "sha256": value, "runtimeMode": "direct_original",
    })
    html = re.sub(re.escape(key) + r"\.png\?v=[A-Za-z0-9-]+", key + ".png?v=" + value[:12], html)
    for binding in bindings["bindings"]:
        if binding.get("illustration") == item["sourceFile"]:
            binding["card_illustration"] = None
            binding["illustration_mode"] = "direct_original"
            binding["background_revision_record"] = "design-discussion/04-content-validation/artifacts/portrait-rollout-20260930/replacement.json"
    replacement["items"].append({"id": key, "runtimeFile": item["sourceFile"],
                                 "sha256": value, "previousFile": item["beforeFile"],
                                 "previousSha256": item["sha256Before"]})

# Every existing resource pointing at a revised portrait uses the existing full-image fallback.
keys = {x["id"] for x in PLAN["items"]}
for path in (ROOT / "content/portraits").rglob("*.tres"):
    text = path.read_text()
    revised = any('path="res://assets/portraits/illustrations/' + key + '.png"' in text for key in keys)
    if not revised:
        continue
    original = text
    text = re.sub(r'^\[ext_resource type="Texture2D" path="res://assets/portraits/illustrations/cutouts/[^\"]+" id="[^\"]+"\]\n', "", text, flags=re.M)
    text = re.sub(r'^CardIllustration = ExtResource\("[^\"]+"\)\n', "", text, flags=re.M)
    if text != original:
        path.write_text(text)
        replacement["changedPortraitResources"].append(str(path.relative_to(ROOT)))

manifest["latestRevision"] = {"date": "2026-09-30", "scope": sorted(keys),
                               "recordFile": "../../../design-discussion/04-content-validation/artifacts/portrait-rollout-20260930/replacement.json",
                               "status": "remaining32 optimized and installed; final aesthetic feedback pending"}
manifest["background"] = "Opaque low-saturation softly graded colored backgrounds across the optimized35 portrait collection."
manifest["status"] = "35 portraits available; all optimized with approved collection method; runtime binding is explicit; final latest batch aesthetics pending user feedback"
(ASSETS / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
(ASSETS / "runtime-bindings.json").write_text(json.dumps(bindings, ensure_ascii=False, indent=2) + "\n")
(ASSETS / "index.html").write_text(html)
(BASE / "replacement.json").write_text(json.dumps(replacement, ensure_ascii=False, indent=2) + "\n")
print("Installed", len(replacement["items"]), "images; updated", len(replacement["changedPortraitResources"]), "portrait resources.")
