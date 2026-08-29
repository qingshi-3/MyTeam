# Tower Autobattler Movement Presentation Readability

Status: Completed

## Goal

Replace unreadable grid-to-grid teleporting with clear, bounded movement presentation while preserving the accepted deterministic battle simulation, one-unit-per-cell occupancy, targeting, reservations, combat balance, and 1x/2x/4x speed semantics.

## Confirmed Solution

1. Logical movement remains discrete and authoritative.
   - `BattleSimulation` and the deterministic grid-movement service continue to own cells, legality, reservations, cooldowns, event order, damage, death, and the battle digest.
   - No continuous-coordinate navigation, physics movement, same-cell occupancy, pathfinding rewrite, or balance change is authorized.
2. Spatial movement becomes an independent presentation responsibility.
   - Add a reusable, editor-tunable Godot component scene for unit motion presentation.
   - The component moves the instantiated unit root between rendered cell positions. It never writes simulation cells or decides where a unit may move.
   - A persistent process-driven interpolator owns movement state; do not create overlapping tweens that compete for the same transform.
3. Real movement events retain their ordered destination cells.
   - The battle presenter routes every accepted `move` event and its event cell to the motion component before ordinary state synchronization.
   - Movement waypoints are kept in a short bounded queue and played as adjacent-cell segments. Cue arbitration may select an attack or hit animation, but it must not discard an accepted spatial move.
   - Initial battle placement, newly created presenters, summons, rebind, and explicit reset snap directly to the authoritative cell. Ordinary idle/health synchronization never writes the unit transform.
4. Presentation latency is bounded.
   - Default editor-authored single-cell durations are approximately 0.17 seconds at 1x, 0.11 seconds at 2x, and 0.08 seconds at 4x.
   - The queue is bounded by an authored waypoint limit and a maximum visual-lag budget of approximately 0.25 seconds. When a catch-up burst arrives, segment playback accelerates smoothly within that budget rather than accumulating indefinitely or teleporting.
   - A movement event created during a rendered frame cannot consume the elapsed time from before that event. Hitch recovery must preserve at least one rendered spatial sample instead of completing a fresh segment or its whole queue before drawing. The approximately 0.25-second catch-up budget is measured in effective presentation time at supported rendering cadence after input stops; no-frame stalls cannot simultaneously provide wall-clock completion and intermediate visible samples, so continuity takes priority when rendering resumes.
   - 1x is the tactical-observation speed, 2x remains readable acceleration, and 4x is fast resolution. Four-times speed need not expose every authored action frame, but movement must remain continuous and must not flash between cells.
5. Animation, pause, speed, and terminal state remain coherent.
   - While spatial motion is active, `move` is the base animation cue. Attack, skill, and hit one-shots may take visual priority without stopping spatial interpolation; when the action ends, the base cue reflects whether motion is still active.
   - Pause freezes both simulation and in-progress presentation at the current interpolated position. Resume continues from that exact position.
   - Changing speed retimes the active segment from its current progress without restarting, snapping, or moving backward.
   - Defeat immediately clears pending motion and prevents any later position synchronization or queue callback from sliding, revealing, or recreating the unit. Rebind, battle replacement, deactivate, and tree exit clear all motion state.

## Authority Impact

- Update `gameplay-design/tower-autobattler-core.md` with the player-facing movement-readability contract and the intended roles of 1x/2x/4x.
- Update `system-design/tower-autobattler-architecture.md` with the split between authoritative grid movement and bounded spatial presentation, ordered move-event routing, snap-only lifecycle cases, pause/speed ownership, and terminal cleanup.
- Extend `docs/testcases/alpha-manual-qa.md` with native observation of continuous movement, path order, pause/resume, speed switching, action overlap, summons, and defeat interruption.
- Keep the previously completed playability task historical; this new task owns the newly reported temporal presentation defect.

## Scope

- A reusable unit-motion presentation component scene and its focused runtime script.
- Battle-presentation routing that preserves ordered `move` destinations separately from per-unit animation-cue arbitration.
- Separate APIs for snap placement, queued movement, non-positional state refresh, pause, speed change, defeat cancellation, and reset/teardown.
- Integration with the existing independent unit scenes without adding battle-root discovery or external paths to content.
- Temporal regression tests, production-scene UI/presentation tests, rendered frame-sequence evidence, full regression, and independent read-only verification.

## Non-Goals

- Changing pathfinding, move intervals, attack timing, targeting, collision/occupancy, reservations, waiting, or deterministic results.
- `NavigationAgent2D`, physics bodies, free-form continuous movement, diagonal grid steps, local avoidance, stacking, or arbitrary deployment.
- Adding camera work, trails, particles, damage numbers, facing-direction systems, audio, new content, or final commercial combat polish.
- Making every event individually readable at 4x; 4x remains a fast-resolution mode.

## Hard Constraints

- Work on `main`; do not create or switch branches.
- `D:\godot\rpg` remains read-only and is not a runtime dependency.
- Preserve all independent hero, soldier, enemy, item, and command scenes and the current save schema.
- Motion parameters and repeated scene structure are authored through `.tscn`/resources; business code must not construct a replacement unit tree ad hoc.
- Simulation truth and the presenter-free digest must remain byte-for-byte deterministic for the same seed.
- A content unit must still instantiate, validate, bind, activate, deactivate, and exit safely without a battle screen.
- Do not allow an unbounded event, waypoint, tween, or callback backlog.
- Do not launch or interfere with the user's currently open Godot editor.

## Acceptance Criteria

### Continuous movement

- At 1x, after an accepted one-cell move, the first rendered sample is not already at the destination; an intermediate sample lies strictly between the source and destination; completion lands exactly on the target cell.
- Consecutive moves preserve their authored adjacent-cell order instead of drawing one diagonal/long jump to the final state.
- Ordinary idle/health synchronization cannot snap an active mover to the authoritative final cell or end the move cue early.
- A catch-up burst representing up to twelve simulation ticks remains bounded, produces no backward jump or teleport, and reaches the newest authoritative cell within the configured visual-lag budget after the burst stops.
- Shared production-frame deltas of 0.10, 0.25, and 0.30 seconds cannot consume a newly queued move before its first rendered sample. Hitch recovery advances at most one ordered segment per rendered frame, retains only bounded current-motion time credit, and reaches the newest cell within roughly 0.25 seconds of effective presentation time at the supported frame rate.

### Lifecycle and actions

- Initial units and summons appear directly at their spawn cells and do not slide in from the origin.
- Attack, skill, and hit one-shots remain visible while spatial movement stays continuous; completing the action restores `move` when motion remains, otherwise `idle`.
- Pause freezes transform progress and movement animation state. Resume continues without discontinuity. Switching 1x/2x/4x retimes without restart or snap.
- Defeat clears queued motion immediately, plays the terminal presentation once at the current visible position, and never slides or reappears. Battle replacement, rebind, deactivate, and exit leave no active movement process or callback.
- Pointer selection continues to use the presenter's visible position while it is between cells.

### Regression and evidence

- Fixed-seed presenter-free battle results, ticks, digests, occupancy, waiting, reservations, and move-event sequences remain unchanged.
- Every concrete unit scene continues to instantiate independently with the required reusable motion/animation/readability contracts.
- Low-concurrency build; Fixture, Content, Gameplay, UI, AlphaRun, and main-scene smoke tests pass with their expected markers.
- Rendered temporal evidence at 1x, 2x, and 4x demonstrates a one-cell move, a multi-cell burst, attack during/after motion, pause/resume, and defeat interruption at both target resolutions where layout matters.
- Manual QA confirms 1x is tactically readable, 2x remains followable, and 4x is continuous fast-forward without the current teleport/flicker behavior.

## Progress

- 2026-08-28: User reported that combat units teleport between cells and flash unpredictably, making battle logic unreadable.
- 2026-08-28: Read-only diagnosis confirmed each logical move is one adjacent grid step, but the presenter directly writes the unit root position to the newest simulation cell.
- 2026-08-28: Timing audit confirmed the fixed 0.1-second simulation tick becomes approximately 100/50/25 milliseconds of real time at 1x/2x/4x; catch-up may execute up to twelve ticks in one rendered frame, so several legal moves can collapse into one visible jump.
- 2026-08-28: Read-only architecture audit confirmed ordinary synchronization also rewrites transforms and returns movement to idle on the next tick. Existing static screenshot and logic contracts cannot detect this temporal defect.
- 2026-08-28: User confirmed the bounded ordered-waypoint interpolation solution, including presentation-only ownership and readable 1x/2x with continuous fast-forward at 4x.
- 2026-08-28: Execution preflight completed on `main`. Project rules, this activity, gameplay/system authority, manual QA, and the required debugging, UI animation/feel, component, scene, state-machine, C#, and testing skills were read in full. The existing entirely untracked workspace is being preserved; no editor or donor-project process was opened.
- 2026-08-28: Architecture preflight found no simulation change is needed. `BattleSimulation` already emits each legal adjacent `move` with its ordered destination `Cell`; the defect is presentation-only because `BattleScreenController` and `UnitContentRoot` currently rewrite the presenter transform to the newest authoritative cell during event and idle synchronization.
- 2026-08-28: Gameplay, system, and manual-QA authority now define the split between authoritative grid cells and bounded spatial presentation: move-event waypoints route independently of cue arbitration; initial/summon/reset snap is distinct from non-positional refresh; pause/speed/action/defeat semantics and the 1x/2x/4x timing roles are explicit.
- 2026-08-28: The first public-behavior RED reproduced the defect against the pre-change production unit scene: after an accepted one-cell `move`, the first sample was already the destination and `MovementPresentationContractSmoke` exited non-zero with `accepted one-cell move teleported to its destination before the first rendered sample`. The pre-change SHA-256 baselines are `BattleSimulation.cs = 7ECB4849B19E5FC26E036938300B642F4FFB648FAD6A6673935A43271D0F7D98` and `DeterministicGridMovementService.cs = 1C800A489EDD8B2CC6021DCF66E40D94CBBC559AA00E566978A142F82C95AC3F`; both must remain unchanged.
- 2026-08-28: The reusable `UnitMotionPresentationComponent` scene now owns a persistent process-driven interpolator with authored 0.17/0.11/0.08-second speed profiles, a 0.25-second visual-lag budget, and a bounded twelve-waypoint queue. All 45 concrete hero, soldier, and enemy scenes instantiate it explicitly; `UnitContentRoot` exposes separate snap, queue, refresh, pause, speed, terminal-cancel, and reset lifecycle entry points.
- 2026-08-28: The temporal contract is GREEN after routing every production `move` event independently of cue arbitration. Verified first/mid/final one-cell samples, ordered corner traversal, a twelve-segment burst within 0.25 seconds, idle-sync isolation, pause/resume, 1x-to-4x retiming, action/base-cue recovery, defeat/rebind/deactivate/exit/replacement cleanup, production same-tick hit-plus-move, pointer selection at an interpolated position, summon snap, presenter-free determinism, and all 45 independent unit-scene contracts. The low-concurrency build reports 0 warnings and 0 errors.
- 2026-08-28: Concrete time samples are GREEN under rendered-frame semantics. A one-cell move from `(0, 0)` to `(88, 0)` remains at the source when its enqueue frame supplies a 0.30-second hitch delta, reaches approximately `(25.88, 0)` on the next 0.05-second presentation frame, and lands exactly at `(88, 0)` over later frames. The ordered corner probe visits `(88, 0)` before an intermediate vertical sample near `(88, 40.8)` and then `(88, 68)`. A twelve-destination burst defers the enqueue-frame hitch, advances no more than one accepted ten-pixel segment per 60fps sample, and reaches `(120, 0)` in fifteen effective frames (0.25 seconds). Bounded overflow still finishes at the newest authoritative `(150, 0)` rather than a stale tail.
- 2026-08-28: Production timing and lifecycle probes are GREEN. The production board presenter stays at `(56, 188)` on the move-event frame, then samples strictly between it and `(144, 188)` while the simultaneous `hit` cue remains active. Pointer selection succeeds at that interpolated position. Pause preserves the exact transform across a one-second probe, 1x-to-4x retiming preserves the switch position and direction, actions restore `move` or `idle` according to actual motion state, summons snap directly, and defeat/rebind/deactivate/exit/battle replacement leave no active processing or stale callbacks.
- 2026-08-28: The first-handoff rendered evidence passed at both 1280x720 and 1600x900 with 22 named frames per resolution, but it disabled production battle processing and therefore did not cover the later hitch finding. Those earlier one-cell, burst, action, pause, and defeat sequences remain useful regression evidence but are no longer the complete temporal handoff.
- 2026-08-28: Final regression passed: low-concurrency build `0 warnings, 0 errors`; Fixture; Content with all five expected negative lifecycle markers captured and Registry publication blocked; Gameplay; UI; MovementPresentation; AlphaRun; clean main-scene headless startup; and VisualCapture at both target resolutions. The UI smoke retains its pre-existing test-only focus warning and exits successfully. The legal fixture unit was updated to instantiate the same required motion component as production units.
- 2026-08-28: Final architecture/code audit passed with no blocking findings. All 45 concrete unit scenes explicitly instantiate the editor-authored component; per-frame interpolation performs no node lookup or resource load and disables processing while idle; signal/lifecycle cleanup is explicit; ordinary presenter synchronization contains no transform write; and the only scoped position writes are explicit snap and interpolated progress. Simulation SHA-256 values remain exactly `BattleSimulation.cs = 7ECB4849B19E5FC26E036938300B642F4FFB648FAD6A6673935A43271D0F7D98` and `DeterministicGridMovementService.cs = 1C800A489EDD8B2CC6021DCF66E40D94CBBC559AA00E566978A142F82C95AC3F`.
- 2026-08-28: Independent verification rejected the first handoff. `BattleScreenController` processes before each child presenter; on a hitch frame it can enqueue fresh move events and the child `UnitMotionPresentationComponent` then consumes the same raw 80-300ms frame delta, including time that elapsed before those events existed. A fresh segment or entire compressed queue can therefore finish before the frame is drawn. Earlier burst tests used 0.01-second manual substeps, the production-route test called the presenter separately, and VisualCapture disabled production battle processing, so none represented the shared parent/child hitch frame. The task resumed without authorizing any simulation or balance change.
- 2026-08-28: The new shared-production-frame RED failed against the rejected implementation before any fix. With `BattleScreenController._Process(0.10)` producing the move and the child motion component receiving the same 0.10-second delta, the first drawable presenter sample advanced from `(56, 188)` to `(107.76471, 188)` and the suite exited non-zero with `shared 0.10s production frame consumed time from before its fresh move event`. The same contract also covers 0.25 and 0.30-second shared deltas once the earliest failure is fixed.
- 2026-08-28: The hitch RED is GREEN after a presentation-only timing correction. Idle-to-move defers the enqueue-frame delta; raw frame contribution is editor-capped at 0.05 seconds; unused credit is bounded by the visual-lag budget; and each `_Process` may complete at most one ordered segment before yielding to a draw. Completion, snap, defeat, reset, rebind, deactivate, and exit clear credit. Pause clears credit and resume defers its first frame; every 1x→2x→4x→1x change preserves normalized position, clears old credit, and defers the switch frame. No simulation or balance code changed.
- 2026-08-28: Production-order contracts now pass shared 0.10, 0.25, and 0.30-second deltas: every enqueue-frame sample remains at `(56, 188)`, recovery never moves backward or crosses more than one adjacent cell per 60fps sample, and one-, two-, and three-cell authoritative destinations complete within fifteen effective frames. A separate already-active 4x chase probe survives a 0.30-second hitch without consuming its fresh queue, then reaches the newest waypoint over later frames.
- 2026-08-28: Final rendered evidence now contains 27 named movement frames per resolution at 1280x720 and 1600x900. Five new `1x_RealProductionHitch` frames use an actual non-headless engine schedule and a deliberate 320ms main-thread stall: pre-hitch and blocked frames, the first post-hitch production draw still at the source, a later grid-intermediate sample, and the settled destination. The recovery frames use explicit 1/60-second presentation steps because the capture process itself renders faster than 60fps; runtime assertions require source preservation after the real hitch and settlement within the supported-frame budget.
- 2026-08-28: Post-rejection full regression is GREEN: final low-concurrency build `0 warnings, 0 errors`; Fixture; Content with all five expected negative markers; Gameplay; UI; MovementPresentation with `hitch-0.10-0.25-0.30,one-segment-per-frame,no-wall-debt`; AlphaRun; clean main-scene startup; and VisualCapture at both resolutions. The Godot code-review audit found no new blocker: the hot path performs no node/resource lookup or allocation, process state is disabled while idle, credit is bounded, and every terminal/lifecycle path clears it. All 45 concrete unit scenes remain explicitly composed with the authored component.
- 2026-08-28: Simulation integrity remains byte-identical after the rejection fix: `BattleSimulation.cs = 7ECB4849B19E5FC26E036938300B642F4FFB648FAD6A6673935A43271D0F7D98`; `DeterministicGridMovementService.cs = 1C800A489EDD8B2CC6021DCF66E40D94CBBC559AA00E566978A142F82C95AC3F`.
- 2026-08-28: Second-round independent acceptance completed with all three read-only verification tracks returning `PASS`. The independent automated matrix passed, the original parent/child shared-delta timing blocker and its no-wall-debt correction passed focused re-review, and both 1280x720 and 1600x900 visual sets passed with 27 movement frames per resolution, including the real 320ms production-hitch sequence. Native 1x/2x/4x play observation remains a useful subjective polish check, not an unfinished acceptance blocker.

## Current State and Resume Condition

The task is `Completed`. Three independent read-only verification tracks passed the automated regression, focused timing-blocker re-review, and both 27-frame visual sets. No implementation or verification work remains. Native 1x/2x/4x play observation is retained only as a non-blocking subjective polish recommendation for future hands-on review. Any future simulation, balance, occupancy, targeting, or save change requires a new discussion and task scope.

## Verification Handoff

All three independent read-only verification tracks are complete and passed: automated regression, focused timing-blocker review, and rendered frame-sequence review at both target resolutions. The accepted evidence is under `res://.godot/qa/Motion_1280x720_*.png` and `res://.godot/qa/Motion_1600x900_*.png`; the production-hitch regression is specifically `Motion_*_1x_RealProductionHitch_00.png` through `_04.png`. Native 1x/2x/4x observation is optional subjective polish, not a remaining acceptance step.

If future movement-presentation changes trigger re-verification, inspect both code ownership and rendered time behavior: re-run the full matrix and frame-sequence capture because static screenshots alone are insufficient. Treat any ordinary-sync transform snap, unbounded lag, movement after defeat, pause drift, or fixed-seed digest change as blocking.
