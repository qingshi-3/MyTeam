#!/usr/bin/env python3
"""Measure one fixed real-content battle with optimized C# and isolated GUI input.

Uses the existing checkout. Godot's development host loads its Debug directory;
we stage ExportRelease assemblies there atomically and verify optimization in-game.
A normal Debug build is restored at the end. Trace and recording measurements are
kept separate from uninstrumented timing samples.

This historical profile entry still depends on retired authored-content fixtures.
Including its test code only preserves diagnostic compilation; this runner has
not been revalidated against the current roster and is not a current passing gate.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default=shutil.which("dotnet"))
    parser.add_argument("--godot", default=os.environ.get("GODOT") or shutil.which("godot"))
    parser.add_argument("--mode", action="append", choices=["core", "gui", "record", "trace-core", "trace-gui"])
    parser.add_argument("--trace-tool", type=Path, default=ROOT / ".godot/architecture-tools/dotnet-trace")
    parser.add_argument("--output", type=Path, default=ROOT / ".godot/architecture-profile")
    args = parser.parse_args()
    if not args.dotnet or not args.godot:
        parser.error("Provide --dotnet and --godot, or put both on PATH")
    args.output = args.output.resolve()
    args.output.mkdir(parents=True, exist_ok=True)
    (args.output / ".gdignore").touch()
    environment = dict(os.environ, DOTNET_ROOT=str(Path(args.dotnet).resolve().parent))
    results = []

    def run(command, log, timeout):
        with log.open("w") as stream:
            return subprocess.run(command, cwd=ROOT, env=environment, stdout=stream,
                                  stderr=subprocess.STDOUT, timeout=timeout).returncode

    try:
        command = [args.dotnet, "build", "my-team.csproj", "-c", "ExportRelease",
                   "-p:GodotApiConfiguration=Debug", "-p:MyTeamIncludeTests=true",
                   "-maxcpucount:2", "-v:minimal"]
        if run(command, args.output / "build-release.log", 180):
            raise RuntimeError("Release build failed; see build-release.log")
        for mode in args.mode or ["core", "gui"]:
            for source in (ROOT / ".godot/mono/temp/bin/ExportRelease").glob("my-team.*"):
                target = ROOT / ".godot/mono/temp/bin/Debug" / source.name
                temporary = target.with_name(target.name + ".profile-tmp")
                shutil.copy2(source, temporary)
                os.replace(temporary, target)
            gui, trace = mode in ["gui", "record", "trace-gui"], mode.startswith("trace-")
            command = [args.godot, "--path", str(ROOT)]
            command += ["--position=-4000,-4000", "--resolution", "960x540"] if gui else ["--headless"]
            command += ["res://tests/ArchitectureBattleProfile.tscn", "--", f"--output={args.output / mode}"]
            if gui:
                command += ["--gui"]
            if mode == "record":
                command += ["--record"]
            if trace:
                command += ["--trace"]
                command = [str(args.trace_tool.resolve()), "collect", "--profile", "gc-verbose", "--providers",
                           "Microsoft-DotNETCore-SampleProfiler:0:5,MyTeam-ArchitectureProfile:0:5",
                           "--stopping-event-provider-name", "MyTeam-ArchitectureProfile",
                           "--stopping-event-event-name", "End", "--stopping-event-payload-filter",
                           "label:x4" if gui else "label:second", "--output", str(args.output / (mode + ".nettrace")),
                           "--show-child-io", "--", *command]
            log = args.output / (mode + ".log")
            code = run(command, log, 180)
            output = log.read_text()
            clean = code == 0 and "ARCHITECTURE_BATTLE_PROFILE_OK" in output and not any(
                bad in output for bad in ["ERROR:", "PROFILE_FAILED", "leaked at exit", "resources still in use"])
            results.append({"mode": mode, "exit_code": code, "clean": clean, "command": command})
            print(f"{mode}: clean={clean}", flush=True)
            if not clean:
                raise RuntimeError(f"{mode} failed; see {log}")
    finally:
        restore = run([args.dotnet, "build", "my-team.csproj", "-maxcpucount:2", "-v:minimal"],
                      args.output / "build-debug-restored.log", 180)
        (args.output / "runs.json").write_text(json.dumps({"debug_restore_exit": restore, "runs": results}, indent=2) + "\n")
        if restore:
            raise RuntimeError("Debug build restore failed; see build-debug-restored.log")


if __name__ == "__main__":
    main()
