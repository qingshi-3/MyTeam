#!/usr/bin/env python3
"""Focused architecture checks. Build first; every engine process runs serially.

Exit zero requires completion markers, exit zero AND no unexpected engine errors.
Shutdown retention is reported separately but still fails the clean-run gate.
"""
import argparse
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
CASES = json.loads((ROOT / "tools/architecture_checks.json").read_text())


def inspect_log(case, output, returncode):
    lines = output.splitlines()
    missing = [marker for marker in case["markers"] if not any(line.startswith(marker) for line in lines)]
    unexpected, expected, shutdown = [], [], []
    injecting = False
    for line in lines:
        if line == "EXPECTED_SAVE_FAILURE_BEGIN":
            injecting = True
        elif line == "EXPECTED_SAVE_FAILURE_END":
            injecting = False
        elif "ObjectDB instances leaked at exit" in line:
            shutdown.append(line)
        elif re.match(r"\s*(ERROR:|SCRIPT ERROR:)", line):
            namespace = case.get("expected_save_namespace")
            pattern = rf"ERROR: Save failed for user://tests/{re.escape(namespace or '-')}/[0-9a-f]{{32}}/active_run\.json:"
            if injecting and namespace and re.match(pattern, line):
                expected.append(line)
            elif re.match(r"ERROR: \d+ resources still in use at exit", line):
                shutdown.append(line)
            else:
                unexpected.append(line)
    if injecting:
        unexpected.append("Fault injection block did not finish")
    assertions_passed = returncode == 0 and not missing
    return {
        "assertions_passed": assertions_passed,
        "clean_run": assertions_passed and not unexpected and not shutdown,
        "missing_markers": missing,
        "unexpected_errors": unexpected,
        "shutdown_errors": shutdown,
        "expected_fault_errors": expected,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--godot", default=os.environ.get("GODOT") or shutil.which("godot"))
    parser.add_argument("--case", action="append", choices=[case["name"] for case in CASES])
    parser.add_argument("--include-input", action="store_true", help="Include isolated 960x540 GUI checks")
    parser.add_argument("--output", type=Path, default=ROOT / ".godot/architecture-checks")
    parser.add_argument("--timeout", type=int, default=120)
    args = parser.parse_args()
    if not args.godot:
        parser.error("Pass --godot or set GODOT to the Godot .NET executable")
    selected = [case for case in CASES if case["name"] in args.case] if args.case else [
        case for case in CASES if args.include_input or not case.get("display")]
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / ".gdignore").touch()
    results = []
    for case in selected:
        command = [args.godot, "--path", str(ROOT)]
        command += ["--position=-4000,-4000", "--resolution", "960x540"] if case.get("display") else ["--headless"]
        command += [f'res://tests/{case["scene"]}.tscn']
        if case.get("args"):
            command += ["--", *case["args"]]
        start = time.monotonic()
        try:
            completed = subprocess.run(command, cwd=ROOT, stdout=subprocess.PIPE,
                                       stderr=subprocess.STDOUT, text=True, timeout=args.timeout)
            output, code = completed.stdout, completed.returncode
        except subprocess.TimeoutExpired as exception:
            captured = exception.stdout or b""
            output = captured.decode(errors="replace") if isinstance(captured, bytes) else captured
            output += "\nERROR: architecture check timed out\n"
            code = -1
        (args.output / f'{case["name"]}.log').write_text(output)
        result = {"name": case["name"], "command": command, "exit_code": code,
                  "seconds": round(time.monotonic() - start, 3), **inspect_log(case, output, code)}
        results.append(result)
        print(f'{case["name"]}: assertions={result["assertions_passed"]} clean={result["clean_run"]} '
              f'unexpected={len(result["unexpected_errors"])} shutdown={len(result["shutdown_errors"])}', flush=True)
    (args.output / "results.json").write_text(json.dumps(results, ensure_ascii=False, indent=2))
    return 0 if all(result["clean_run"] for result in results) else 1


if __name__ == "__main__":
    raise SystemExit(main())
