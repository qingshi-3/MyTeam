# Project Agent Rules

This project is a Godot 4.7 .NET single-player tower-climbing army autobattler.

## Authority And Routing

- Player-facing rules live under `gameplay-design/`.
- Runtime ownership, scene contracts, and data flow live under `system-design/`.
- Confirmed execution scope and resume state live under `work-items/active/`.
- Test cases and manual QA live under `docs/testcases/`.
- Keep this file limited to stable routing and hard constraints.

Before state-changing work, read the relevant authority documents and the active work item. Direction conflicts return to discussion instead of being resolved in code.

## Stable Project Constraints

- Develop on `main`; do not create local development branches.
- `D:\godot\rpg` is a read-only donor. Copy only explicitly selected assets or adapted patterns into this repository; never create runtime dependencies on external absolute paths.
- Every concrete hero, soldier, enemy, and item is an independently instantiable `.tscn` scene that can be opened and tuned in isolation.
- Static definitions may be referenced `.tres` resources. Mutable run or battle state must not be written into shared resources.
- Build behavior through focused component scenes and typed signals. Content scenes must not depend on hidden nodes in a level, battle, UI, or autoload composition root.
- Prefer authored `.tscn`, `.tres`, `Theme`, and shader resources. Runtime code loads and binds them; it does not construct whole UI or content trees ad hoc.
- Player-visible text defaults to Chinese. Stable ids, class names, field names, and enum values remain ASCII/English.
- Use low-concurrency .NET builds (`-maxcpucount:2 -v:minimal`) and avoid unnecessary editor launches or repeated imports.

