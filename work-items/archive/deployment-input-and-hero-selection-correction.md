# Deployment Input And Hero Selection Correction

Status: Completed

## Goal

Restore deployment as a truthful direct-manipulation flow and correct hero-library selection semantics. A reserve soldier must actually join the formation through click or drag, the board must never leave several cells looking selected, invalid feedback must agree with the authoritative formation rules, enemy deployment previews must show their real idle animation, and merely moving the mouse across a hero tile must not change the selected hero.

## Confirmed Solution

1. Replace presentation-only target guessing with one authoritative formation evaluation.
   - `RunApplication` owns a non-mutating evaluation result for a concrete `FormationMoveCommand`, including validity, operation kind, and a concise rejection reason.
   - The same evaluation gates the committing command and drives deployment target states. UI may not mark a destination legal when the application will reject the same command.
   - Preserve the existing six-soldier capacity, three-reserve capacity, movable hero, first-three-column zone, swaps, replacement, withdrawal, save-once, and rollback rules.
2. Make deployment selection and drag state singular and explicit.
   - Exactly one piece may be selected.
   - Legal destinations use a restrained target cue that cannot be confused with selection.
   - The board owns at most one current drag-hover destination; entering another cell clears the previous cell immediately. Drag completion, cancellation, screen rebind, and teardown clear every transient drag state.
   - A rejected click/drop leaves formation and persistence unchanged and shows the authoritative short reason. A valid click/drop updates the formation, refreshes the board, and shows success rather than a contradictory red flash.
3. Verify the complete player path instead of stopping at emitted signals.
   - Use a real `GameRoot` with an isolated save namespace and real viewport mouse/focus input.
   - Recruit or construct a reserve soldier, then select/drag it to a genuinely empty legal cell. Assert the application roster/deployment identity, persisted state, rebound card/cell, exact save count, and visible result.
   - Cover reserve-to-empty, reserve-to-occupied replacement, deployed move/swap, hero move/swap, same-cell, reserve-to-hero, full formation, invalid target, cancelled drag, and repeated operations.
4. Show enemy units as units in deployment preview.
   - Add an authored reusable enemy-preview scene that binds the concrete enemy `UnitPortraitDefinition` and plays the existing calm idle animation through `UnitPortrait`.
   - Role/reach and boss identity remain compact semantic overlays or tooltip detail. Hazard, objective, and blocked terrain remain semantic markers rather than pretending to be units.
   - Resolve enemy portrait data in the composition/presentation boundary; do not put UI resources into combat simulation snapshots or make the board query global content.
5. Make hero selection click/accept driven.
   - Hover provides only the ordinary Theme hover response. `MouseEntered` must not change selected hero, selected styling, detail binding, or run state.
   - Focus movement provides focus visibility only and must not silently change the selected hero. Mouse click or keyboard/gamepad `ui_accept` on a tile selects that hero and binds the detail panel.
   - The detail panel's primary action starts the run for the explicitly selected unlocked hero exactly once. Initial entry may author one deterministic default selection so the page is never empty.

## Authority Impact

- Update `gameplay-design/tower-autobattler-core.md` with truthful deployment-target feedback, single drag-target behavior, animated enemy preview, and click/accept hero-selection semantics.
- Update `system-design/tower-autobattler-architecture.md` with the shared formation-evaluation boundary, board-owned transient drag state, composition-owned enemy portrait view models, and separation of hover/focus from committed hero selection.
- Update `docs/testcases/alpha-manual-qa.md` with reserve-to-empty end-to-end input, drag-trail cleanup, invalid-reason parity, enemy idle preview, and hover-versus-click hero-selection cases.
- Do not alter the global `AGENTS.md`; its existing visual-language and real-input rule already covers this correction.

## Scope

- Formation evaluation/result contract and its use by application commit plus deployment presentation.
- Deployment roster/card, screen, board, cell, click, focus, drag/drop, transient feedback, and exact save integration.
- Authored enemy idle-preview component and deployment binding view model.
- Hero library tile/screen input semantics and selected/detail state.
- Focused RED/GREEN contracts, real-input integration, relevant regression, and paired 1280x720/1600x900 captures.
- Authority and QA synchronization listed above.

## Non-Goals

- No change to formation capacity, reserve capacity, deployment zone, floor legality, combat spawning, combat balance, recruitment limits, save schema, tower generation, or pathfinding.
- No new units, animations, portraits, semantic icons, items, hero mechanics, or generated art.
- No redesign of the full hero-selection layout or deployment geometry beyond the confirmed interaction/readability correction.
- No modification of any `BattleReport*` source, scene, model, test, or active workstream; that surface remains externally owned.
- No donor dependency and no modification of `D:\godot\rpg` or `D:\godot\realm`.

## Hard Constraints

- Work on `main`; create or switch no branch.
- Preserve the dirty worktree and every unrelated user/external-writer change. Never reset, revert, clean, or overwrite broad paths.
- Do not control, stop, restart, or reuse the user's Godot/editor/game processes.
- Keep `RealmTheme` authoritative and repeated visual structures scene-authored. Runtime code binds data/state; it does not construct replacement UI trees or Themes.
- Keep every concrete hero, soldier, enemy, and item independently instantiable as its own `.tscn` scene.
- Simulation/application remain authoritative for legality and persistence. Presentation owns only input interpretation, semantic states, portrait playback, and typed requests.
- Use isolated test save namespaces and low-concurrency builds. Never read from or write to the player's default save during automated verification.

## Acceptance Criteria

### Deployment Function

- With the observed representative state of four soldiers, three deployed soldiers, and three empty formation slots, the reserve soldier can be added to an empty legal cell through both real click-then-cell and real drag/drop.
- Each successful operation changes the expected application identity/cell, persists exactly once, rebinds the same stable controls safely, and displays the unit on the destination cell.
- Reserve replacement, deployed movement/swap, hero movement/swap, and withdrawal retain their accepted atomic behavior.
- Same-cell, reserve-to-hero, floor-illegal, out-of-zone, full-capacity, cancelled, and outside-board operations change and save nothing and expose the authoritative concise reason.

### Deployment Visual State

- At most one roster/board piece is selected and at most one cell is the active drag-hover target.
- Traversing several cells during one drag leaves no highlight trail. Drop, cancellation, rebind, back, and teardown clear all transient hover/drag state.
- Legal target cues, selected source, current hover, occupied swap, invalid target, success, and failure are visibly distinct without relying only on color. Legal-target presentation cannot be mistaken for several selected cells.
- No destination presented as legal for the selected piece is rejected by unchanged application state; no valid reserve-to-empty operation flashes failure red.

### Enemy Preview

- Every concrete enemy spawn on the deployment board shows its real independently playing idle portrait rather than a generic role icon.
- Enemy identity remains distinguishable from player units and keeps concise role/reach or boss redundancy without hiding the animation.
- Hidden/removed previews pause or free their independent playback cleanly and never affect battle animation instances or shared portrait resources.

### Hero Selection

- Moving the mouse across any hero tile changes neither selected hero id, selected styling, detail content, nor active run state.
- Moving keyboard/gamepad focus alone changes focus styling only. Activating a tile by mouse click or `ui_accept` selects it and updates the detail panel once.
- The primary deploy action starts a run only for the explicitly selected unlocked hero and emits once. Locked heroes remain previewable/selectable only to the extent explicitly shown by the existing detail contract, but cannot start a run.

### Verification

- Focused contracts demonstrate RED on the current code for drag-hover trails, reserve-to-empty end-to-end mutation, enemy icon-only preview, and hover-driven hero selection before production fixes.
- Final focused evidence uses real viewport mouse/focus input and asserts application plus persisted state; a typed signal alone is not acceptance evidence.
- Low-concurrency .NET build finishes with zero warnings/errors. Relevant formation, gameplay, content, UI, Theme, semantic, portrait/window, full-run, and clean-startup regressions remain green with only documented deliberate negative fixtures.
- Fresh paired captures at 1280x720 and 1600x900 show reserve selection, single legal/hover target, successful addition, cancelled drag cleanup, invalid feedback, animated enemy previews, hero default selection, hover-without-selection, and click selection. Manual review records any remaining subjective risk.
- Independent verification reviews the complete player paths and scope before completion; execution evidence alone may leave the task only at `Awaiting Verification`.

## Progress

- 2026-08-30: User reported that dragging across deployment leaves several cells looking selected, click-then-cell flashes red, reserve soldiers cannot be added, and enemy deployment previews show only icons.
- 2026-08-30: Read-only diagnosis confirmed every visited cell sets `_dropHover` during `_CanDropData` and clears only at drag end, while selection paints all floor-legal cells with a strong legal state. `IsLegalTarget` represents only floor occupancy and does not evaluate the selected command, so presentation and application rejection can disagree.
- 2026-08-30: The player's default save was inspected read-only and contains four soldiers, three deployed identities, one reserve, and three empty formation slots. Reserve addition should therefore be legal and the failure is not caused by a full formation.
- 2026-08-30: Existing interaction evidence was found insufficient: it selects/drags the first hero card, asserts only one `MoveRequested`, never wires the request to `RunApplication`, and never checks mutation, persistence, or rebound destination. Formation tests cover reserve replacement but not real-input reserve-to-empty integration.
- 2026-08-30: Enemy markers were confirmed to instantiate only `DeploymentMarker` with a semantic role icon; no enemy portrait definition or `UnitPortrait` is bound.
- 2026-08-30: User added and confirmed the hero-selection correction. `HeroLibraryTile` currently routes `MouseEntered`, `FocusEntered`, and `Pressed` to the same `PreviewRequested` signal, and the screen renders that preview through selected styling and detail replacement.
- 2026-08-30: User confirmed the combined correction scope, authority synchronization, new end-to-end acceptance boundary, and execution.
- 2026-08-30: Execution began as the sole project writer after fully reading this task, both Agent-rule levels, gameplay/system/manual-QA authority, the active semantic-icon/animated-portrait interaction contract, and the required Godot UI, input, debugging, testing, responsive, C#, and signal skills. Architecture ownership is split deliberately: `RunApplication` owns non-mutating formation evaluation plus transactional commit/persistence parity; deployment presentation owns only view models, one selection, one drag-hover owner, authored visual states, and typed requests; hero selection owns explicit tile activation; content definitions remain immutable portrait sources.
- 2026-08-30: Protected baseline is `main` with 165 porcelain entries spanning accepted UI/formation/report work and unrelated `web/` state. BattleReport sources/scenes/models/tests remain an external read-only boundary. Baseline SHA-256 values: `project.godot` `F48C02422EDF9E55F50FCE8237B98856DD11E5673D9837F6DB19A5E48B401EB2`; `RealmTheme.tres` `FBAB260391FF26EAC834DE935A0DFDE355FA3552C7977FCA0F0F0A433CC4A6A4`; `RunApplication.cs` `0D28FF9B3191CC5BC9831294A7D8F31CFDD6ACB1DB3C8AFD2924ED5FF27F4515`; `FormationContracts.cs` `EFF9E36C4DB31B744EDCC7B8DC278BBAD35C2360EB6C16CDBB054A2958F61B83`; `GameRoot.cs` `573FF484D161EE633C47858F3B9A68A451A95FAD6B976493B9324C4F4F018AC9`; `DeploymentBoard.cs` `A689E98A0914E4791B555E1AD5A589345AFEA2179C3E1BAFA5AED31C695FC16A`; `DeploymentCell.cs` `50A102BD31096ABCC348D96A6FFC60C32B07C183ED83D030C4E14C2AE5FAB481`; `DeploymentScreenController.cs` `743B10E37AB4B7388AB5D35DC9B5CDF28626362C8EFD3C2A60DBDA7422FA427E`; `HeroLibraryTile.cs` `21288266A5FAD5F59BA69B72352372E27855E272EAE10042456072239F5559EE`; `HeroSelectScreen.cs` `A835E209BBAFA3DEB8539956E5C0F91F9E6AA177C0F02184AD30CF8A3EE8265A`. Existing user Godot PIDs `23260` and `49048` are responsive and remain outside executor control; donor paths remain read-only and out of runtime scope.
- 2026-08-30: Focused RED established after two harness-only compile/path corrections. `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` is clean, while the real-input Godot contract reports `DEPLOYMENT_INPUT_HERO_SELECTION_CONTRACT_FAILED`: no shared evaluation boundary, zero animated enemy previews where four are expected, reserve-to-empty click does not mutate, persist, rebind, or show success, and the drag fixture cannot proceed because its setup withdrawal is rejected. This is behavioral RED against the current application/composition path, not a typed-signal-only failure.
- 2026-08-30: Before production behavior, gameplay, system, and manual-QA authority were synchronized with evaluation/commit parity, board-owned singular drag-hover state, composition-resolved authored enemy portrait previews, and click/`ui_accept`-only hero selection.
- 2026-08-30: Implemented `FormationEvaluation` plus explicit operation kinds. `RunApplication.ApplyFormationCommand` now commits only the result of `EvaluateFormationCommand`; the shared evaluation also exposed the concrete reserve-to-empty root cause: the legacy comparison treated both an undeployed source slot and an empty target occupant as `-1`, falsely reporting same-cell. The corrected comparison requires a deployed source before same-cell rejection, so reserve deployment now takes the first empty formation-capacity slot and saves once.
- 2026-08-30: Deployment composition now supplies an evaluation map per hero/soldier and concrete enemy portrait view models. The authored `EnemyDeploymentPreview.tscn` binds one independent `UnitPortrait` plus enemy/role/reach redundancy. `DeploymentBoard` owns one nullable current drag-hover cell, clears transitions and every terminal/rebind path, and keeps application rejection reasons visible for both click and handled invalid drop without persisting.
- 2026-08-30: Hero library tiles now emit selection only through `Pressed`, which covers mouse click and focused `ui_accept`; hover/focus remain Theme-only visual states. The screen retains one deterministic initial selection and the detail action remains the sole run-start boundary.
- 2026-08-30: Focused GREEN marker is `DEPLOYMENT_INPUT_HERO_SELECTION_CONTRACT_OK formation=evaluate+commit save=isolated-once input=reserve-click+drag drag-hover=single+cleanup enemies=animated-portraits hero=click-accept-only`. It uses a real `GameRoot`, isolated persisted saves, viewport click/drag/focus/`ui_accept`, invalid-drop reason/no-save assertions, exact save count, stable-control rebound, board-owned hover state, concrete enemy portrait identity and active playback, and explicit hero activation.
- 2026-08-30: Relevant serial regressions are GREEN: `FORMATION_DEPLOYMENT_CONTRACT_OK`, `GAME_UI_INTERACTION_RELIABILITY_CONTRACT_OK`, `GAME_UI_VISUAL_LANGUAGE_CONTRACT_OK`, `VISUAL_HIERARCHY_CONTRACT_OK`, `WINDOW_PORTRAIT_CONTRACT_OK`, `CONTENT_CONTRACT_OK`, `GAMEPLAY_CONTRACT_OK`, `ALPHA_RUN_OK`, `UI_SMOKE_OK`, `REALM_THEME_CONTRACT_OK`, `SEMANTIC_PRESENTATION_CONTRACT_OK`, `FIXTURE_CONTRACT_OK`, and `MOVEMENT_PRESENTATION_CONTRACT_OK`. `CleanStartup.tscn` ran for five frames with no diagnostic. Content-gate error stacks are deliberate negative fixtures that conclude with `CONTENT_CONTRACT_OK`; the existing `UiSmoke` focus warning comes from its direct `GrabFocus` probe and does not fail its contract.
- 2026-08-30: Fresh production-render captures passed at both accepted sizes with marker `DEPLOYMENT_INPUT_HERO_SELECTION_CAPTURE_OK ... captures=8`: default hero selection, hover without selection, click selection, reserve selection with concrete enemy previews, authoritative invalid reason, one drag-hover target, cancelled cleanup, and successful reserve addition. Manual montage review found no clipping/overlap at either size; selected, hover, invalid, drag, cancelled, and success states remain distinguishable, and the final deployment count changes from `3/6` to `4/6`.
- 2026-08-30: Final Godot review found no critical issues. The new preview is scene-authored and single-purpose; node lookups and scenes are cached outside per-frame work; child requests travel upward while composition binding travels downward; signal teardown is symmetric; mutable run state stays outside resources; enemy previews own independent portrait nodes and queue-free cleanly. No `BattleReport*`, donor path, save schema, Theme authority, unit/item scene, combat rule, capacity, balance, recruitment, or tower behavior was modified by this workstream.
- 2026-08-30: Independent verification passed on `main`. `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` completed with zero warnings and zero errors. The focused test produced the exact marker `DEPLOYMENT_INPUT_HERO_SELECTION_CONTRACT_OK formation=evaluate+commit save=isolated-once input=reserve-click+drag drag-hover=single+cleanup enemies=animated-portraits hero=click-accept-only`. All 13 serial regressions exited `0`; `ContentContractSmoke` emitted only its five established negative fixtures and `UiSmoke` emitted only its established focus warning. `CleanStartup` exited `0` after five frames. The main Agent inspected both resolution montages and the latest `1280×720` source frames, confirming `3/6 → 4/6`, one drag-hover target, cancelled-drag cleanup, concrete enemy portraits, unchanged hero selection on hover, click-driven hero selection, and no clipping or overlap. Task-scoped `git diff --check` passed. Code and scope review found no critical issue; the external `BattleReport*` boundary, donor isolation, `RealmTheme` authority, and independently instantiable hero/soldier/enemy/item scenes remain intact.

## Changed Surfaces

- Formation contract/application: `src/Run/FormationContracts.cs`, `src/Run/RunApplication.cs`.
- Deployment composition and UI: `src/App/GameRoot.cs`, `src/UI/DeploymentModels.cs`, `src/UI/DeploymentScreenController.cs`, `src/UI/DeploymentBoard.cs`, `src/UI/DeploymentCell.cs`.
- Authored enemy preview: `scenes/ui/components/EnemyDeploymentPreview.tscn`, `src/UI/EnemyDeploymentPreview.cs`, and its generated script UID.
- Hero selection: `src/UI/HeroLibraryTile.cs`, `src/UI/HeroSelectScreen.cs`.
- Authority/QA: `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, `docs/testcases/alpha-manual-qa.md`.
- Focused evidence: `tests/DeploymentInputHeroSelectionContractSmoke.cs/.tscn`, `tests/DeploymentInputHeroSelectionCapture.cs/.tscn`, their generated script UIDs, and ignored capture output under `.godot/qa/deployment-input-hero-selection/`.

## Resume Condition

Completed and independently verified; no recovery or continuation is required. Any future change starts from a new active task.

## Verification Handoff

Independent read-only verification is complete and accepted. No further handoff remains for this task.
