# Tower Autobattler Grid-March Presentation

Status: Completed

## Goal

Turn the battle's accepted cell-by-cell movement into a deliberate, readable "grid march" rather than a short linear slide followed by a mechanical pause. Preserve the tactical value of visible cells, body blocking, speed cadence, ordered turns, and deterministic combat while making each accepted step feel authored.

## Confirmed Solution

1. Preserve grid authority and movement decisions.
   - `BattleSimulation` and `DeterministicGridMovementService` remain the only owners of cells, occupancy, path choice, reservations, movement cooldowns, event order, and battle digest.
   - Do not introduce continuous-coordinate navigation, `NavigationAgent2D`, physics movement, diagonal shortcuts, corner cutting, same-cell stacking, path merging, or balance changes.
2. Present every accepted adjacent move as one intentional grid step.
   - Continue consuming the ordered destination cell from each real `move` event.
   - The unit root must travel only along the authored source-to-destination segment and land exactly at the destination cell center.
   - Apply a mild bounded ease-in/ease-out profile to spatial progress. The profile must not overshoot, reverse, or leave the axis-aligned segment.
   - Consecutive orthogonal moves still pass through and visibly respect the intermediate cell center; no rounded diagonal corner is authorized.
3. Tune the step cadence as a board-game presentation language.
   - Change the editor-authored base cell durations from the current `0.17 / 0.11 / 0.08` seconds to initial targets of approximately `0.24 / 0.14 / 0.09` seconds at displayed `1x / 2x / 4x`.
   - Slow and fast unit identity continues to come from the authoritative frequency of accepted moves. The presentation component does not predict a future destination or alter `MoveInterval`.
   - Existing bounded catch-up, fresh-event delta deferral, one-segment-per-rendered-frame limit, pause behavior, and in-place speed retiming remain mandatory. Multi-waypoint backlog may compress within the existing visual-lag budget rather than accumulating.
4. Add restrained character-only step weight.
   - During a normal segment, the character sprite may receive a small editor-authored vertical lift with zero offset at both endpoints and a maximum initial target of roughly `3` pixels near mid-step.
   - Health bars, hero/role/readability markers, selection position, and the authoritative unit-root transform must not receive this decorative lift.
   - `UnitContentRoot` explicitly coordinates the existing motion and animation components. A component must not discover or call a sibling through parent-tree traversal.
   - Pause freezes the lift; snap, completion, defeat, rebind, deactivate, reset, replacement, and tree exit restore the decorative offset safely.
5. Make the authored move clip support repeated steps instead of flashing its opening pose.
   - Movement remains the base animation while spatial travel is active and action one-shots may still override it without stopping travel.
   - Repeated grid steps should not visibly restart the same opening move pose on every cell. Retain or advance a bounded presentation-only move phase when the available `SpriteFrames` contract permits it.
   - Do not rewrite donor sprite sheets, create per-unit bespoke animations, or require all authored frames to complete during one fast step. At `4x`, continuity and non-flashing presentation take priority over showing every move frame.

## Architecture Judgment

This is a battle-presentation and animation-component change, not a navigation or combat-simulation change. Reuse `UnitMotionPresentationComponent`, `UnitAnimationComponent`, and the existing explicit coordination in `UnitContentRoot`. Keep the persistent process-driven interpolator; do not replace it with overlapping position Tweens. Editor-tunable durations and lift amplitude remain authored in the reusable component scene.

## Authority Impact

- Update `gameplay-design/tower-autobattler-core.md` before runtime code so the player-facing movement contract describes deliberate grid-march steps, revised initial timings, exact cell-center landings, and character-only lift.
- Update `system-design/tower-autobattler-architecture.md` before runtime code with the eased spatial-progress boundary, explicit motion/animation coordination, decorative-offset ownership and cleanup, and the unchanged deterministic simulation boundary.
- Update `docs/testcases/alpha-manual-qa.md` with native crowded-battle checks for step cadence, turn order, marker stability, action overlap, pause/speed changes, hitch recovery, and terminal cleanup.
- Move the previously completed `work-items/active/tower-autobattler-movement-presentation.md` to `work-items/archive/` as historical work; this activity owns the new experiential tuning.

## Scope

- The reusable motion-presentation component script and scene-authored tuning.
- The narrow animation-component API/state needed for character-only lift and non-restarting repeated-step phase.
- Explicit parent coordination in the common unit root.
- Focused movement-presentation contracts, production-route coverage, rendered temporal evidence where useful, related authority/manual QA, and regression verification.
- Progress, evidence, remaining work, and resume state maintained in this file by the execution Agent.

## Non-Goals

- Any change to pathfinding, target selection, occupancy, reservations, move cooldowns, `MoveInterval`, combat balance, tick rate, or deterministic event order.
- Free-form continuous motion, diagonal path smoothing, local avoidance, physics collision movement, root overshoot, elastic/bounce translation, camera work, particles, trails, audio, or broad combat VFX.
- Reauthoring the 45 concrete unit scenes or donor `SpriteFrames` unless a shared scene contract cannot supply the confirmed effect; such a conflict returns to discussion.
- Unrelated refactoring, new content, save-schema changes, or changes to the end-of-battle sequence.

## Hard Constraints

- Work on `main`; do not create or switch branches.
- Preserve the entirely untracked pre-existing workspace and unrelated user changes.
- Do not open or interfere with the user's Godot editor. Use static checks first and the known console executable only for focused headless verification.
- Use low-concurrency builds: `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`.
- `D:\godot\rpg` remains read-only and is not a runtime dependency.
- Hot per-frame paths perform no scene-tree lookup, resource load, unbounded allocation, Tween creation, or unbounded queue/callback growth.
- Initial placement and summons still snap; ordinary synchronization never writes the moving unit root transform.

## Acceptance Criteria

### Grid-march feel and geometry

- At `1x`, a normal one-cell move starts at the source, shows multiple intermediate rendered samples, follows a mild non-linear progress profile, and lands exactly on the destination center without overshoot or reversal.
- The reusable scene authors approximately `0.24 / 0.14 / 0.09` second base durations for `1x / 2x / 4x`; backlog compression remains bounded by the existing catch-up contract.
- A `right -> down -> left` route visibly visits each intermediate cell center in event order and never draws a diagonal or rounded shortcut.
- Fast units step more frequently and slow units pause longer solely because of authoritative move-event cadence; presentation does not predict or invent movement.

### Character presentation

- Mid-step character-only lift is visible but restrained, returns to exactly zero at segment endpoints, and never moves the health bar, hero marker, reach marker, pointer selection origin, or unit-root path.
- Repeated cells do not visibly flash the same opening move pose each time. Action one-shots still override the move clip while spatial travel and lift continue, then restore `move` or `idle` according to actual motion state.
- Pause freezes root progress, character lift, and animation. Resume and `1x -> 2x -> 4x -> 1x` changes preserve direction and normalized progress without restart or snap.
- Defeat and every reset/teardown path clear queued travel and restore decorative offset without later sliding, revealing, callback activity, or stale processing.

### Regression and evidence

- Existing hitch recovery, fresh-event deferral, one-segment-per-frame, maximum-lag, summon snap, pointer selection, facing, action overlap, and deterministic presenter-free contracts remain green.
- `BattleSimulation.cs` and `DeterministicGridMovementService.cs` remain unchanged; their pre-task SHA-256 values are recorded before implementation and verified afterward.
- Low-concurrency build completes with zero warnings and errors. Fixture, Content, Gameplay, MovementPresentation, UI, AlphaRun, and clean headless main-scene startup pass with their expected markers.
- Native temporal evidence or an equivalent deterministic rendered sequence demonstrates the revised one-cell step and a multi-turn route at `1x`, plus continuous non-flashing behavior at `2x` and `4x`.
- Final Godot code review finds no component-boundary, lifecycle, hot-path allocation, node-lookup, or cleanup blocker.

## Progress

- 2026-08-30: User reported that combat still reads as one-cell-at-a-time rather than conventionally smooth movement and asked whether that could become a distinctive effect.
- 2026-08-30: Read-only audit confirmed root motion already interpolates between adjacent cells, but uses linear progress and fixed `0.17 / 0.11 / 0.08` durations. Across all 45 definitions, authoritative `MoveInterval` ranges from `0.28` to `0.60` seconds and averages `0.45`; at `1x`, the current root is moving for only about 30% of the average wall-clock move interval.
- 2026-08-30: Read-only animation audit confirmed shared move clips are generally authored near 8 FPS, so the current per-cell windows expose only about `1.36 / 0.88 / 0.64` authored frames at `1x / 2x / 4x`, reinforcing short slide/opening-pose repetition.
- 2026-08-30: User confirmed the recommended deliberate grid-march direction: keep cell authority and ordered centers, lengthen and ease each step, add restrained character-only weight, and avoid RTS-style continuous navigation.
- 2026-08-30: Execution preflight completed on `main`. The user/project rules, activity, gameplay/system authority, prior completed movement activity, manual QA, and required Godot component, C#, animation/feel, and testing skills were read. The entirely untracked pre-existing workspace is being preserved; no editor or donor project was opened.
- 2026-08-30: Pre-change SHA-256 baselines recorded: `BattleSimulation.cs = 8E2BC68B9DF2AD58884663577D240C55C8E23EEAE27F1CA1F067CB7AFE0099D8`; `DeterministicGridMovementService.cs = DAE014E27BB044EB52F9E6355110F11D4EA83B6886869225E093D0234AFA1`. Both files are frozen for this task.
- 2026-08-30: Architecture preflight confirmed the change belongs entirely to battle presentation. The existing persistent motion interpolator remains the root-position owner; `UnitContentRoot` will explicitly route normalized step progress into character-only animation decoration. No simulation, navigation, occupancy, balance, donor-sheet, or per-unit scene reauthoring is required.
- 2026-08-30: Gameplay, system, and manual-QA authority now define the deliberate grid-march cadence, monotonic segment easing, exact ordered cell centers, character-only lift, bounded repeated-step animation phase, and terminal cleanup before runtime implementation begins.
- 2026-08-30: Focused test-first RED was captured after adding the new public behavior contracts: low-concurrency compilation failed only because the pre-change animation component had no authored `StepLiftPixels` or bounded retained move-phase API.
- 2026-08-30: The reusable presentation implementation is now GREEN in the focused suite. Motion uses endpoint-exact monotonic smoothstep along each existing ordered segment; the shared scene authors `0.24 / 0.14 / 0.09`; `UnitContentRoot` explicitly routes normalized progress upward/downward; and only `AnimatedSprite2D` receives a sine-shaped three-pixel lift. The animation component retains one normalized move-loop phase across ordinary cell boundaries and action overrides, while every snap/defeat/rebind/deactivate/exit path restores zero decoration.
- 2026-08-30: Focused native contract passes with exact evidence `one-cell x = 0, 15.625, 50, 84.375, 100` and character lift `0, 2.121, 3, 2.121, 0` over the authored 0.24-second 1x step. Readability and health nodes remain unchanged; right/down/left ordered centers, pause/speed continuity, action overlap, repeated-step phase, hitch recovery, defeat, and production routing are green.
- 2026-08-30: Complete serial regression is GREEN: low-concurrency build reports `0 warnings / 0 errors`; Fixture, Content, Gameplay, MovementPresentation, UI, and AlphaRun exit zero with expected markers; clean five-frame headless main-scene startup exits zero. Content intentionally emits and captures its five structural/instantiate/ready/process/exit negative markers, and UI retains its existing test-only focus rejection warning.
- 2026-08-30: Final simulation integrity matches the recorded baseline byte-for-byte: `BattleSimulation.cs = 8E2BC68B9DF2AD58884663577D240C55C8E23EEAE27F1CA1F067CB7AFE0099D8`; `DeterministicGridMovementService.cs = DAE014E27BB044EB52F9E6355110F11D4EA83B6886869225E093D0234AFA1`.
- 2026-08-30: Final `godot-code-review` found no blocker. Component signals travel upward and the common root calls downward; components perform no sibling/parent discovery; the per-frame path performs no node/resource lookup or Tween creation; queue, credit, and move phase are bounded; processing disables while idle; and snap, completion, pause, speed change, defeat, rebind, deactivate, replacement, and exit have explicit cleanup coverage. The existing user editor process remained open and untouched.
- 2026-08-30: Main-Agent independent read-only verification passed on `main`. A fresh low-concurrency build completed with `0 warnings / 0 errors`; MovementPresentation reproduced the exact `GRID_MARCH_TEMPORAL_EVIDENCE one-cell=0,15.625,50,84.375,100 lift=0,2.121,3,2.121,0 duration=0.24 easing=smoothstep markers=stable` marker and its full success marker; Content reproduced `CONTENT_CONTRACT_OK entries=57 floors=5 events=90 portraits=45(8,24,13)` with the five expected structural/instantiate/ready/process/exit negative fixtures; and Gameplay reproduced its full `GAMEPLAY_CONTRACT_OK` marker.
- 2026-08-30: Independent static review confirmed smoothstep remains monotonic and endpoint-exact, lift writes only `AnimatedSprite2D`, the composition root explicitly coordinates signal-up/call-down, retained move phase is bounded, every lifecycle path clears presentation state, and the hot path performs no lookup, load, Tween creation, or unbounded backlog growth. Independent hashes still match `BattleSimulation.cs = 8E2BC68B9DF2AD58884663577D240C55C8E23EEAE27F1CA1F067CB7AFE0099D8` and `DeterministicGridMovementService.cs = DAE014E27BB044EB52F9E6355110F11D4EA83B6886869225E093D0234AFA1`. User Godot editor PID 23260 remained open and undisturbed.

## Current State and Resume Condition

The task is completed. Implementation, execution verification, and independent read-only verification all passed; no required work or recovery entry remains. Future subjective cadence tuning or any requested change to lift amplitude, easing shape, simulation, donor sheets, navigation, or balance requires a new discussion and activity rather than reopening this completed task.

## Verification Handoff

Status: Independent verification passed; ready for completed-task archive.

Commands executed from the project root:

```powershell
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/FixtureContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/ContentContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/MovementPresentationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/UiSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlphaRunSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . --quit-after 5
```

Success evidence:

- Build: `0 warnings / 0 errors`.
- `FIXTURE_CONTRACT_OK`.
- `CONTENT_CONTRACT_OK entries=57 floors=5 events=90 portraits=45(8,24,13)` after the five expected negative lifecycle markers.
- `GAMEPLAY_CONTRACT_OK ... navigation,two-phase,... death-terminal ... pace=0.8-1.6-3.2 ...`.
- `GRID_MARCH_TEMPORAL_EVIDENCE one-cell=0,15.625,50,84.375,100 lift=0,2.121,3,2.121,0 duration=0.24 easing=smoothstep markers=stable`.
- `MOVEMENT_PRESENTATION_CONTRACT_OK timing=grid-march-0.24-0.14-0.09,eased-first-mid-final,ordered-centers,... pause=speed-lift-continuity,... actions=move-base,phase-continuity ... lifecycle=snap,defeat,rebind,deactivate,exit,replacement-planning ... simulation=unchanged`.
- `UI_SMOKE_OK screens=13 ... interaction=modal-focus,battle-selection,zero-mana`; its focus warning is the existing deliberate rejection probe.
- `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`.
- Clean headless main-scene startup exited zero with no runtime error.

Scoped files:

- Authority and QA: `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, `docs/testcases/alpha-manual-qa.md`.
- Runtime/resources: `src/Components/UnitMotionPresentationComponent.cs`, `scenes/components/UnitMotionPresentationComponent.tscn`, `src/Components/UnitAnimationComponent.cs`, `scenes/components/UnitAnimationComponent.tscn`, `src/Content/UnitContentRoot.cs`.
- Evidence: `tests/MovementPresentationContractSmoke.cs`, `tests/VisualCapture.cs`.
- Task routing: this activity and the move of completed `tower-autobattler-movement-presentation.md` from `work-items/active/` to `work-items/archive/`.

Simulation hashes before and after are identical:

- `BattleSimulation.cs`: `8E2BC68B9DF2AD58884663577D240C55C8E23EEAE27F1CA1F067CB7AFE0099D8`.
- `DeterministicGridMovementService.cs`: `DAE014E27BB044EB52F9E6355110F11D4EA83B6886869225E093D0234AFA1`.

Remaining risk is subjective tuning only: the native deterministic evidence proves geometry, timing, lift isolation, phase continuity, and lifecycle contracts, but a human should still judge the 1x cadence in a crowded fight. The visual-capture harness now authors a six-frame 1x right/down/left sequence and reports 29 movement frames, but it was not launched in a visible rendering window while the user's editor was open; this avoids interfering with the editor and does not weaken the native temporal contract. No branch or commit was created.

Independent verification repeated the low-concurrency build plus MovementPresentation, Content, and Gameplay contracts and found no blocker. The exact temporal, content, gameplay, static-boundary, lifecycle, hot-path, hash, branch, and editor-process evidence is recorded in the final Progress entries above. This activity is complete and archived; subjective native feel remains a future tuning input rather than unfinished acceptance work.
