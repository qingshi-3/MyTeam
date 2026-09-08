# Project Agent Rules

This project is a Godot 4.7 .NET single-player tower-climbing hero-roster autobattler with independent tactical commands.

## Authority And Routing

- Player-facing rules live under `gameplay-design/`.
- Runtime ownership, scene contracts, and data flow live under `system-design/`.
- Long-form gameplay discussion lives under `design-discussion/`, separate from current authority. For these discussions, read `design-discussion/README.md`, `roadmap.md`, the current topic and relevant cross-topic issues before continuing; update the topic record and roadmap before handing off.
- Discussion confirmation does not authorize implementation or incremental authority edits. Keep conclusions in the discussion area until the agreed integration review and user-confirmed merge; retain rejected/deferred decisions and do not treat research examples as rules.
- Confirmed execution scope and resume state live under `work-items/active/`.
- Test cases and manual QA live under `docs/testcases/`.
- Keep this file limited to stable routing and hard constraints.

Before implementation, read the authority relevant to the changed behavior and an existing active work item when applicable. Small local changes do not require a new task document. Resolve intent from current evidence; material unresolved direction conflicts return to discussion.

## Stable Project Constraints

- Develop only on `main`; do not create local development branches or use worktrees/alternate checkouts to bypass this constraint.
- `D:\godot\rpg` is a read-only donor. Copy only explicitly selected assets or adapted patterns into this repository; never create runtime dependencies on external absolute paths.
- Every concrete hero, soldier, enemy, and item is an independently instantiable `.tscn` scene that can be opened and tuned in isolation.
- Static definitions may be referenced `.tres` resources. Mutable run or battle state must not be written into shared resources.
- Build behavior through focused component scenes, explicit typed dependencies for commands/queries, and typed signals for events. Content scenes must not depend on hidden nodes in a level, battle, UI, or autoload composition root.
- Prefer authored `.tscn`, `.tres`, `Theme`, and shader resources. Runtime code loads and binds them; it does not construct whole UI or content trees ad hoc.
- Player-visible text defaults to Chinese. Stable ids, class names, field names, and enum values remain ASCII/English.
- Use low-concurrency .NET builds (`-maxcpucount:2 -v:minimal`) and avoid unnecessary editor launches or repeated imports.

## Validation Responsibilities

- Agents own foundational correctness through change-proportionate compilation, scope checks, and key rule checks. Retain risk-based automated checks for core collision, determinism, rollback, and similarly critical behavior.
- Users own hands-on play, movement feel, interaction experience, and pacing or balance acceptance. Agents provide concise steps and observation points; real interaction checks must use real input paths, and pending manual checks must never be reported as passed.
- By default, skip multi-resolution screenshot sweeps, full-run automated progression, broad regression, and duplicate independent reruns. Expand only on request, clear risk, failure, or insufficient evidence; the main agent reviews scope and credible executor evidence, then reruns only gaps.
