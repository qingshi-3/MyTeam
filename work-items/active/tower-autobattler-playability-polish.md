# Tower Autobattler Playability Polish

Status: Completed

## Goal

Resolve the seven playtest findings from the first completed run without changing the accepted product identity: correct combat animation lifecycles, expose army and hero-command information, make unit roles readable, eliminate blocked-unit oscillation while retaining tactical grid occupancy, and replace the abstract sequential deployment interaction with a real battlefield preview and direct rearrangement.

## Confirmed Solution

1. Combat animation becomes stateful presentation rather than repeated cue playback.
   - Attack, hit, and skill cues are one-shot actions that cannot be overwritten by same-frame idle synchronization.
   - Damage and defeat cues apply to the target; attack and movement cues apply to the source.
   - Defeat is an idempotent terminal presentation state: play once, briefly hold, then fade/hide. Do not leave a permanent corpse that appears to block a cell.
2. Every non-combat run screen exposes current army context.
   - A compact persistent summary shows hero health, deployed/reserve counts, item count, and gold.
   - A non-layout-shifting overlay drawer shows hero rule/command, soldier health and deployment state, role/reach, and item effects.
   - The drawer is read-only; formation changes remain owned by deployment.
3. Combat readability is explicit and does not depend only on sprite art or color.
   - Heroes receive a high-contrast gold identity marker with a non-color symbol/text cue.
   - Units receive a compact near/ranged marker derived from the authoritative attack range through one centralized classification rule; selected-unit details show the exact role and range.
4. One living unit continues to occupy one grid cell.
   - Replace forced fallback movement with stable targeting, reservable engagement positions, deterministic move-intent arbitration, and an explicit waiting state.
   - A blocked unit waits instead of moving to a non-improving cell. Waiting does not emit a move event or play movement animation.
   - Do not introduce same-cell crowding, continuous-coordinate navigation, or a global melee-diagonal balance change in this task.
5. Hero commands use real, discrete battle mana while preserving the current three-use balance.
   - Each hero authors `MaxMana = 3`; each concrete command scene explicitly authors `ManaCost = 1`.
   - Battles start at full mana, mana does not regenerate during battle, and the next battle starts full. Mana is not persisted across floors.
   - The battle HUD permanently shows a blue three-segment meter, current/max values, command name, generated effect description, mana cost, optional gold cost, and the current failure reason.
   - Resource consumption is transactional: a failed command consumes neither mana nor gold. Paid reinforcement must not spend gold before confirming it can succeed.
6. Deployment presents the real 10x6 battlefield, floor cells, and enemy start positions.
   - The hero remains visible at the existing fixed anchor for this task.
   - The existing six soldier anchors remain the authoritative formation capacity and save contract, but are shown on the real map and can be directly rearranged.
   - Support drag/drop and an equivalent select-unit-then-select-cell flow.
   - Support reserve-to-empty deployment, reserve-to-occupied replacement, deployed-to-empty movement, deployed-to-occupied atomic swap, and withdrawal to reserve.
   - Invalid drops/cancellation do not mutate run state or save data. Each successful operation saves once.

## Authority Impact

- Update `gameplay-design/tower-autobattler-core.md` with the confirmed battle-mana semantics, combat readability contract, blocked-unit waiting behavior, and concrete deployment interaction.
- Update `system-design/tower-autobattler-architecture.md` with presentation state ownership, the replaceable deterministic grid-movement service, shared army-overview view-model boundary, transactional hero-command resource semantics, and shared deployment/battle coordinate contract.
- Add or update focused manual QA under `docs/testcases/` for animation lifecycle, unit readability, army drawer, mana failure semantics, and deployment interactions.
- Do not put task progress or temporary implementation detail in `AGENTS.md`.

## Scope

- Animation event routing, one-shot/terminal animation state, death fade/hide, and cue fallback compatibility.
- Authored reusable unit-readability, selected-unit detail, hero-command HUD, army-summary/drawer, drawer-row, deployment-cell/card, and related UI scenes/resources.
- Combat hero and near/ranged identifiers and exact selected-unit details.
- Run-level read-only army overview binding on route, recruitment/reward, shop, event, rest, and deployment screens.
- Battle-only mana state, scene-authored maximum/cost metadata, transactional command execution results, and visible failure reasons.
- Deterministic grid movement service, target/engagement reservation state, intent resolution, waiting/fairness behavior, and cleanup on death/summon/target invalidation.
- Real-map deployment preview using the existing six soldier anchors and fixed hero anchor, with drag/drop, click selection, swap/replace/withdraw commands, and consistent battle spawn coordinates.
- Automated contracts, visual capture, and independent read-only verification proportional to each changed subsystem.

## Non-Goals

- Moving the hero during deployment, opening arbitrary player-side cells, changing formation capacity, or migrating the active-run save schema.
- Same-cell unit stacking, continuous-coordinate movement, `NavigationAgent2D`, or general RTS-style local avoidance.
- Natural/attack/hit mana regeneration, command cooldown redesign, buff-duration redesign, or balance changes beyond preserving the existing three successful uses.
- New heroes, soldiers, items, floors, audio, final VFX, or commercial art polish.
- Replacing the independent concrete unit/item/command scene contracts.

## Hard Constraints

- Work on `main`; do not create or switch branches.
- Preserve all existing user changes and the completed Alpha implementation.
- Every concrete hero, soldier, enemy, item, and hero command remains independently instantiable and editor-tunable through its `.tscn` scene.
- Player-facing effects, mana cost, and gold cost come from the same concrete command scene used by runtime behavior; UI must not duplicate balance values.
- Simulation owns combat truth, mana, command legality, occupancy, reservations, and outcome. Presentation only renders facts and animation state.
- Deployment and battle use one shared coordinate/formation contract rather than duplicated arrays.
- Repeated UI structures are authored template scenes and instantiated; do not construct whole interfaces ad hoc in C#.
- Keep deterministic, presenter-free battle execution and current content validation guarantees.

## Acceptance Criteria

### Animation and readability

- At 1x, 2x, and 4x, attacks visibly play and are not replaced by idle in the same rendered frame.
- Attackers never receive the target's hit/defeat cue.
- Every defeated unit enters defeat once, fades/hides once, and is not recreated by later synchronization.
- Player heroes are immediately identifiable without relying only on color; near/ranged classification matches authoritative range values, including near Brood Matriarch and ranged Bone Regent.
- All concrete unit scenes continue to load and instantiate independently.

### Movement

- A fully occupied engagement ring makes additional units wait for at least 30 ticks without producing an `A -> B -> A` oscillation.
- When an engagement position opens, a waiting unit resumes movement and can attack.
- Units prefer another reachable enemy with an available engagement position over oscillating around a surrounded target.
- Occupied cells remain unique and legal; reservations are cleared on death, summon, target invalidation, battle replacement, and disposal.
- Waiting emits no move event. Fixed-seed repeated runs retain identical results and digests.

### Mana and commands

- All eight heroes begin each battle at 3/3 mana; three successful uses produce 2/3, 1/3, and 0/3; a fourth use fails.
- A new battle starts at 3/3. Pause and speed do not alter mana.
- Insufficient mana, insufficient gold, dead hero, missing/blocked summon, or any other failed command changes neither mana nor gold.
- Existing three-use shield, multiplier, summon, disable, and paid-command outcomes remain behaviorally equivalent.
- Hero selection, army drawer, and battle HUD show the same generated command effect and authored mana/gold cost.

### Army information and deployment

- Route, recruitment/reward, shop, event, rest, and deployment screens show the compact run summary and can open the army drawer without leaving the current decision.
- Drawer content refreshes after recruitment, purchase, rest, battle casualty, deployment change, and item gain.
- At 1280x720 and 1600x900 the overlay does not clip content or leak pointer input; closing it restores usable focus.
- Deployment floor cells, enemy previews, hero anchor, and six soldier anchors match the actual battle setup.
- Any two deployed soldiers can swap in one atomic command; replacement, movement, withdrawal, cancellation, and invalid-drop semantics match the confirmed solution.
- Refreshing/reopening deployment preserves the formation through the existing save contract.

### Regression

- Low-concurrency .NET build passes with zero warnings/errors.
- Content/fixture/gameplay/UI/full-run smoke suites pass, including new focused counterexamples.
- Main-scene headless startup remains clean apart from intentional negative fixtures.
- Commander, carry, and solo deterministic completion paths still complete the tower.
- Refreshed visual evidence covers deployment, normal battle, death/attack state, army drawer on multiple run screens, command mana/effect state, and hero/role markers.

## Progress

- 2026-08-28: User completed a full run and reported seven playability findings.
- 2026-08-28: Read-only audit confirmed attack cues are overwritten by same-frame idle sync, defeated cues are repeatedly restarted, and damage/defeat cues are incorrectly routed to attackers.
- 2026-08-28: Read-only movement audit reproduced deterministic `A -> B -> A` oscillation with a static surrounded target because the current path fallback excludes waiting in the current cell.
- 2026-08-28: Read-only UI audit confirmed route-only name summaries, tooltip-only command descriptions, no mana model, sequential first-empty deployment, and abstract text slots.
- 2026-08-28: User confirmed the complete recommended repair scope, including a fixed hero anchor and directly rearrangeable six-soldier formation on the real battlefield.
- 2026-08-28: Execution preflight completed on `main`. The active task, both authority documents, project rules, and matching Godot skills were read; the existing untracked worktree state is being preserved.
- 2026-08-28: Architecture audit assigned animation lifecycle to `UnitAnimationComponent`, deterministic reservations/intents to a replaceable grid-movement service, battle-local mana and command commit to simulation, the army drawer to a shared read-only view model, and deployment mutation to atomic `RunApplication` commands over one shared battlefield layout.
- 2026-08-28: Gameplay and system authority documents were updated before runtime changes. Milestone A is now the active implementation slice.
- 2026-08-28: Milestone A completed. Attack/hit/skill-cast now use protected one-shot presentation states; defeat is an idempotent terminal hold/fade/hide; source and target event routing is explicit. Centralized reach classification and combat markers identify heroes and near/ranged units, while selection exposes exact role, reach, and health.
- 2026-08-28: Milestone B completed. All eight heroes explicitly author 3 maximum mana and all eight command scenes author a one-mana cost. Battle-local mana, command failure reasons, the authored-effect HUD, and atomic mana/gold/summon validation preserve three successful uses without charging failed commands.
- 2026-08-28: Milestone C completed. `DeterministicGridMovementService` owns stable targets, engagement reservations, waiting, and deterministic move-intent arbitration. `BattlefieldLayout` now owns the shared 10x6 geometry and formation anchors; deployment supports reserve placement/replacement, deployed movement/swap, withdrawal, cancellation, drag/drop, and click selection through atomic run commands.
- 2026-08-28: The shared read-only Army Overview summary/drawer is bound on route, reward/recruitment, shop, event, rest, and deployment screens. The drawer exposes hero rule/command, soldier deployment/role/reach, and item effects without taking formation ownership.
- 2026-08-28: Focused evidence passed before final matrix: low-concurrency build with zero warnings/errors; gameplay reservations/waiting/recovery/unique-cell, battle-mana/transaction, deployment/save, and animation lifecycle contracts; UI summary/drawer/battlefield/mana contracts; and commander/carry/solo fifteen-floor deterministic paths.
- 2026-08-28: Manual QA authority was extended with 1x/2x/4x animation lifecycle, hero/reach readability, multi-screen Army drawer and dual-resolution checks, mana failure transactions, and drag/click deployment swap/replace/withdraw/cancel cases.
- 2026-08-28: Visual capture now names explicit attack and defeated presentation states in addition to deployment, normal/hazard/boss battle, command mana, and Army drawers. Final rendered sets contain 21 PNGs at each of 1280x720 and 1600x900 under `.godot/qa/1280x720/` and `.godot/qa/1600x900/`; the inspected key layouts have no clipping or overlap.
- 2026-08-28: Final Godot code/range audit found no blocking production issue. Content remains independent scene-authored, simulation/application/presentation ownership remains intact, repeated UI is template-instanced, signals are paired with teardown, no hot-path scene-tree lookup was introduced, and no runtime donor-path dependency exists.
- 2026-08-28: Final ordered matrix passed: build `0 warnings / 0 errors`; `FIXTURE_CONTRACT_OK`; `CONTENT_CONTRACT_OK entries=57 floors=5 events=90` with all five expected negative lifecycle markers; `GAMEPLAY_CONTRACT_OK ... reservations,waiting ... mana,transactional-economy ... deployment`; `UI_SMOKE_OK`; `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`; clean five-second main-scene startup; and `VISUAL_CAPTURE_OK screens=11 extras=10 ... states=attack,defeated` at both target resolutions.
- 2026-08-28: Independent verification returned FAIL and reopened execution. Confirmed blockers are: same-tick hit overriding attack before render; action clips capped at 0.8 seconds and therefore truncating 34/35 attack plus 35/35 defeated assets; missing `cast` fallback; incorrect near/ranged boundary; full-reserve withdrawal mutation; missing Army drawer keyboard/gamepad modal focus trap; disabled 0-MP command swallowing the authored failure reason; stale manual-QA wording/names; and deployment mutations not rolling back when persistence fails.
- 2026-08-28: Independent UI verification added one in-scope blocker: unit selection relied on `UnitContentRoot._UnhandledInput`, but full-screen Control ancestors consume the mouse before that route. Selection hit testing moves to the production BattleBoard GUI-input boundary, which requests selection through the unit's typed signal; global unhandled input is removed from content units. A production-scene mouse contract and `SelectedUnitDetails` visual evidence are required.
- 2026-08-28: Reopened architecture audit keeps the correction within accepted ownership. Presentation arbitrates bounded cue priority and playback; animation presentation completes every authored frame within configurable action windows; `UnitRangeClassifier` remains the only reach boundary; `RunApplication` remains authoritative for reserve capacity and persistence rollback; reusable Army/HUD components own modal focus and failure display. No simulation rules, content counts, save schema, formation capacity, or donor boundary change is authorized.
- 2026-08-28: Reopened RED contracts now cover same-tick cue arbitration, every unique authored `SpriteFrames` attack/defeat sequence, `skill_cast -> cast` fallback, the 2.2/2.3/3.5 reach boundary, all deployment save-failure rollback shapes, full-reserve withdrawal rejection, modal focus scope, a fourth 0-MP command attempt, and production BattleBoard pointer selection.
- 2026-08-28: The correction slice is implemented. Per-unit event batches choose `defeated > skill_cast/attack > hit > move/idle`; one pending action is bounded to a single priority slot; complete authored clips are compressed into configurable action windows; defeat waits for its full playback before hold/fade; and cast fallback, reach classification, reserve capacity, save rollback, modal focus restoration, zero-mana feedback, and board-owned selection now match the authority contracts.
- 2026-08-28: The injected production-pointer test initially failed because `Viewport.PushInput` was asked to reinterpret an already-local Viewport coordinate. Using `inLocalCoords=true` reaches the real BattleBoard `GuiInput` path and opens selected-unit details; no direct signal or reflected selection shortcut is used. Unit content no longer owns global unhandled pointer input.
- 2026-08-28: Final ordered verification passed after the corrections: build `0 warnings / 0 errors`; `FIXTURE_CONTRACT_OK`; `CONTENT_CONTRACT_OK entries=57 floors=5 events=90` with the five expected structural/instantiate/ready/process/exit negative markers; `GAMEPLAY_CONTRACT_OK ... presentation=cue-priority,full-frames,cast-fallback ... run=conversion,deployment,rollback,settings`; `UI_SMOKE_OK ... interaction=modal-focus,battle-selection,zero-mana`; `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`; and clean five-second main-scene headless startup. UI smoke deliberately attempts an illegal underlying `GrabFocus` and Godot logs the expected rejection warning while the suite exits zero.
- 2026-08-28: Visual evidence was regenerated at both target resolutions. `.godot/qa/1280x720/` and `.godot/qa/1600x900/` each contain 23 flow/state PNGs, including `SelectedUnitDetails.png` and `BattleCommandManaEmpty.png`; inspected unit-detail, zero-mana feedback, attack, defeat, Army drawer, and deployment frames show no clipping or overlap. Both capture runs report `VISUAL_CAPTURE_OK screens=11 extras=12 ... states=attack,defeated information=selected-unit,zero-mana`.
- 2026-08-28: Final scope/code audit passed. Simulation/application/presentation ownership remains intact; repeated UI remains template-instanced; concrete heroes, soldiers, enemies, items, and commands remain independent scenes; no runtime donor path exists; and no hero movement, arbitrary deployment, save migration, stacking, continuous navigation, mana regeneration, or new content was introduced.
- 2026-08-28: Second-round independent acceptance passed with no blocker. The battle reviewer independently ran the low-concurrency build with `0 warnings / 0 errors` and confirmed Gameplay and Content both exited zero with the correct success markers and expected negative content-gate evidence. The UI reviewer statically reviewed the interaction paths and inspected all 23 images at both 1280x720 and 1600x900, including selected-unit, zero-mana, attack, defeat, Army drawer, and deployment evidence. The scope reviewer confirmed the implementation stayed within the accepted product scope and preserved simulation/application/presentation ownership plus independent scene-authored content. The primary reviewer also inspected the key implementations and four representative screenshot classes and found no blocking issue. All acceptance criteria are satisfied; no task-scoped implementation, test, documentation, or verification work remains.

## Current State and Resume Condition

The task is complete and independently accepted. There is no remaining task-scoped work and no normal resume action. Any future defect or requested change should begin as a new discussion/work item and preserve the accepted ownership boundaries unless the user explicitly confirms a direction change.

## Verification Handoff

Independent verification is complete:

1. Battle verification passed the low-concurrency build (`0 warnings / 0 errors`) and the Gameplay and Content suites with zero exits, correct success markers, and all intentional negative content-gate markers accounted for.
2. UI verification passed static interaction review and inspection of both complete 23-image sets at 1280x720 and 1600x900. Selected-unit details, zero-mana failure feedback, attack/defeat presentation, Army drawer, and deployment evidence were accepted.
3. Scope verification passed. No hero movement, arbitrary deployment, save-schema migration, stacking, continuous navigation, mana regeneration, new content, donor absolute-path dependency, or content-scene coupling was introduced.
4. Primary review of the key implementations and representative screenshots found no additional blocker.

The remaining observations are non-blocking experiential risks, not unfinished code: native pointer drag feel should still be judged during ordinary playtesting; real keyboard/gamepad focus traversal should be sampled on target hardware; and 1x/2x/4x animation readability remains a subjective tuning check. Any follow-up from those checks belongs to a new work item.
