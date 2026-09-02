# Flexible Three-Column Deployment And Battlefield Scale

Status: Completed  
Confirmed: 2026-08-30

## Goal

Make formation placement materially tactical and make combat readable at the supported desktop sizes without changing the established deterministic battle grid or army-size balance.

The logical battlefield remains `10×6`. In deployment, the player may place the hero and up to six soldiers on legal cells in the first three player-side columns rather than using one fixed hero anchor plus six fixed soldier anchors. In battle, the same logical cells render through a responsive projection that materially enlarges the board and its units at `1600×900` while remaining complete and usable at `1280×720`.

## Confirmed Product Rules

- The authoritative logical battlefield remains `10×6`; this task does not add rows or columns and does not rebalance reach, movement ticks, floor-rule coordinates, enemy starts, or expected encounter duration.
- The player deployment zone is every cell where `x = 0..2` and `y = 0..5`, for 18 candidate cells before floor-rule legality is applied.
- Formation capacity remains exactly one hero plus up to six soldiers. Eighteen cells are spatial choices, not eighteen deployment slots. Existing empty-deployment-slot hero/item bonuses continue to count unused soldier capacity only.
- The hero is an independently positioned formation piece. It occupies one cell, can be selected and moved within the same legal deployment zone, cannot be withdrawn, and cannot be replaced by a reserve soldier.
- Any living deployed piece must occupy a unique legal cell. Cells outside the first three columns and cells rejected by the current floor rule cannot accept a player formation piece.
- Moving a deployed piece onto another deployed piece performs one atomic position swap. Moving a reserve soldier onto an occupied soldier cell deploys the reserve soldier and returns the displaced soldier to reserve. Moving a reserve soldier onto the hero is illegal. Moving the hero onto a soldier swaps their positions.
- Deployment keeps both drag/drop and an equivalent select-piece-then-select-cell flow. Invalid drops, cancellation, and illegal targets do not mutate run state or save data.
- Battle setup uses the exact persisted previewed cells. It must not silently fall back to another spawn cell for a valid player formation. Duplicate or illegal player positions are rejected before battle starts.

## Confirmed Presentation Rules

- Logical battlefield coordinates and presentation pixels are separate responsibilities. Simulation, save data, formation commands, floor rules, and pathfinding use integer cells only.
- Battle and deployment each project the same `10×6` logical grid into their own allocated `Control` rectangle. They share logical cell identity and legality, not one fixed pixel `CellSize` or `Origin`.
- At `1600×900`, the combat board must use the previously empty width and height: target cell pitch is at least approximately `108×82` while keeping all ten columns, all six rows, units, markers, and borders visible.
- At `1280×720`, the full board and fixed actions remain visible without clipping. The projection may reduce toward the existing approximately `88×68` pitch but must not transform-scale text or the whole UI tree.
- Battlefield unit visuals, health/readability markers, pointer selection radius, motion endpoints, and character lift remain aligned with the responsive cell projection. Unit identity must become larger with the battle board rather than merely increasing empty spacing between unchanged sprites.
- The selected-unit inspector must not obscure playable cells. The authored battle layout reserves a compact inspector region or otherwise guarantees no living unit/cell is hidden under it at either supported resolution.
- Deployment clearly distinguishes the first-three-column placement zone, currently legal targets, occupied cells, the movable hero, soldiers, enemy previews, floor hazards/objectives, and blocked/non-player cells. Color is not the only carrier.

## Confirmed Run-State And Save Contract

- Run application state remains the sole owner of mutable formation truth. UI submits typed formation commands and never mutates formation collections directly.
- Upgrade the active-run save schema incrementally from version 2 to version 3. Preserve the existing six-entry soldier deployment identity list for capacity, reserve, empty-slot, casualty, and compatibility semantics; add explicit JSON-safe integer cell coordinates for each soldier capacity entry plus the hero cell.
- Version-2 saves migrate deterministically to the old authored positions: hero `(0,3)` and the six soldier cells `(1,1)`, `(1,2)`, `(1,3)`, `(2,1)`, `(2,2)`, `(2,3)`. Migration must preserve hero, roster, deployed/reserve identities, health, items, gold, route, seed, battle count, and progression.
- Version-3 validation requires one hero cell, exactly six soldier capacity entries/cell records, unique non-empty deployed identities, unique occupied formation cells, in-bounds first-three-column positions, and valid roster references. JSON stores coordinates as integer `x`/`y` fields rather than Godot vectors.
- A successful formation command mutates and saves exactly once. Save failure restores the exact previous identities and positions. Tests use isolated save namespaces and must not read, migrate, overwrite, or delete the player's real active run.

## Architecture Impact

This is a large cross-system behavior change spanning Map, Deployment, Runtime State, Save, UI/Input, Combat Setup, Presentation, and Navigation verification.

- Update `gameplay-design/tower-autobattler-core.md` before runtime changes: replace the fixed hero/six-anchor rule with the confirmed first-three-column deployment-zone and movable-hero rules while retaining one-hero/six-soldier capacity.
- Update `system-design/tower-autobattler-architecture.md` before runtime changes: separate logical battlefield geometry from responsive presentation projection; document versioned formation ownership/migration, typed formation commands, and exact deployment-to-battle spawn parity.
- Update `docs/testcases/alpha-manual-qa.md` before runtime changes: replace fixed-anchor deployment checks and add responsive board, movable hero, legal-zone, swap/replace, save migration/rollback, focus, and exact-spawn checks at both supported resolutions.
- Preserve the deterministic grid-movement service. Enlarging presentation or opening formation cells must not introduce `NavigationAgent2D`, continuous coordinates, stacking, path prediction, or presentation-owned movement truth.
- Prefer authored `.tscn` composition for the battle board/inspector layout and reusable deployment cell/piece presentation. Runtime code may calculate projection math, bind authored templates, and submit typed commands; it must not build whole UI trees, Themes, or StyleBoxes ad hoc.

## Scope

- Versioned active-run formation data and v2-to-v3 migration.
- Transactional hero and soldier formation commands with rollback.
- Shared logical deployment-zone/cell legality and exact battle spawn snapshots.
- Responsive battle/deployment board projection and cell hit-testing.
- Movable hero presentation and interaction using authored reusable scene ownership.
- Eighteen deployment-zone target cells, visual zone/legality states, soldier/hero occupancy, drag/drop, selection, swap, replace, and withdrawal compatibility.
- Battle layout adjustment so the larger board and selected-unit inspector coexist without hiding cells.
- Existing summaries and deployment state text updated away from fixed slot-number language where player-facing.
- Focused save, formation, battle setup, input, hierarchy, responsive, movement, gameplay, UI, alpha-run, startup, and visual-capture verification.
- Authority, manual QA, activity progress, evidence, independent verification handoff, and archive on acceptance.

## Non-Goals

- No change to logical `10×6` dimensions, enemy deployment zone/start patterns, unit capacity, reserve capacity, roster capacity, reach thresholds, cooldowns, movement ticks, target selection, engagement arbitration, floor-rule behavior, combat speed, or battle duration tuning.
- No same-cell stacking, diagonal movement rule change, continuous-coordinate navigation, RTS avoidance, camera system, zoom control, or user-controlled combat movement.
- No hero/soldier/item/content addition, stat rebalance, economy redesign, empty-slot bonus redesign, report algorithm change, or save-slot system redesign.
- No project-wide resolution/stretch/fullscreen change and no support below the accepted `1280×720` lower bound.
- No Theme replacement, pixel UI recreation, image generation, audio/VFX work, branch/commit/push, unrelated cleanup, or donor-project write.

## Hard Constraints

- Work only on `main`; do not create or switch branches.
- The worktree is intentionally dirty from prior accepted work. Record the pre-task porcelain and preserve every unrelated modified, deleted, and untracked surface; never reset, revert, or broadly clean it.
- `D:\godot\rpg` and `D:\godot\realm` are outside this task. Do not read from them unless a newly discovered blocker requires discussion, never edit them, and never create an absolute runtime dependency.
- Do not launch, close, focus, or terminate the user's existing Godot/editor/game processes. Use isolated headless contracts and the existing dedicated visual-capture flow only.
- Do not touch the player's real `user://` save. Migration and failure tests use isolated test save roots.
- Concrete heroes, soldiers, enemies, items, and commands remain independently instantiable scenes. Mutable formation state never enters shared `.tres` resources.
- Use low-concurrency build commands and serial Godot tests/captures. Shut down only idle build servers created by this task; never control Godot processes.
- If implementation requires changing `10×6`, soldier capacity six, reserve capacity three, movement/targeting behavior, floor-rule semantics, or the confirmed migration contract, stop and return this activity to `Needs Discussion` rather than guessing.

## Acceptance Criteria

### Formation rules and persistence

- All 18 first-three-column cells appear as candidate deployment targets; all other cells are visibly non-player cells and reject formation input.
- The hero can move to any legal empty deployment-zone cell and can atomically swap with a deployed soldier. It cannot leave the zone, enter a blocked cell, overlap another piece, or be withdrawn/replaced by a reserve soldier.
- Soldiers can deploy from reserve, move, swap, replace, and withdraw with the confirmed semantics. At most six soldiers are deployed and at most three are reserved.
- Every successful command saves once; every invalid/cancelled command saves zero times; a forced save failure restores exact hero cell, soldier identities, soldier cells, reserve membership, selection-visible state after rebind, and save-call contract.
- A representative v2 JSON save loads as v3 with the exact old hero/soldier anchors and no loss of run data. A subsequent save/reload preserves arbitrary legal hero/soldier cells. Invalid duplicate, out-of-zone, out-of-bounds, wrong-count, and stale-identity v3 formations fail safely.
- Casualty cleanup removes only the defeated soldier identity from deployment while preserving the other pieces' cells and hero position. Empty-slot bonuses still use six minus deployed-soldier count.

### Deployment and battle parity

- Deployment preview and battle use the same logical positions for hero and every deployed soldier. No valid player spawn is silently relocated by simulation setup.
- Blocked cells cannot be selected; hazard/objective cells retain floor-rule meaning and are deployable only when `CanOccupy` permits them.
- Enemy previews and enemy battle starts remain unchanged and never overlap a valid player formation.
- Mouse drag/drop, mouse select-then-cell, keyboard, and gamepad focus activation can reach every legal cell and fixed action without duplicate command submission or focus escape.

### Responsive presentation

- At `1600×900`, the complete combat grid uses materially more of the screen than the current fixed `88×68` projection, with target pitch at least approximately `108×82`; units and readability markers grow coherently with it.
- At `1280×720`, all ten columns and six rows, unit visuals, selection, inspector, status, pause, speed, command HUD, and fixed actions remain fully visible and usable without overlap or clipping.
- Responsive resize is reversible: cell centers, drawing, hit testing, unit snap/movement endpoints, deployment targets, markers, and selection radius update together without stale offsets, teleporting, diagonal shortcuts, or distorted text.
- The selected-unit inspector never hides a playable cell at either accepted resolution.

### Regression and evidence

- Low-concurrency .NET build reports zero warnings and zero errors; `git diff --check` passes.
- Focused contracts prove schema migration, formation validation/transactions, 18-cell zone, hero moves/swaps, exact battle starts, and responsive projection.
- Existing content, gameplay, movement presentation, UI, hierarchy, semantic, portrait/window, report, alpha-run, clean-startup, and RealmTheme contracts remain green after fixed-anchor expectations are deliberately replaced.
- Fresh complete native captures at `1600×900` and `1280×720` include deployment with movable-hero/zone states and active battle with selected unit. Manual review finds no clipping, hidden cell, undersized unit, lost marker, stale hit target, or fixed-action loss.

## Progress

- 2026-08-30: User requested a larger battle map, movable hero placement, and deployment across the first three columns instead of six fixed cells.
- 2026-08-30: Read-only architecture audit found one fixed `10×6` layout with pixel `CellSize = (88,68)`, hero cell `(0,3)`, six fixed soldier cells, a six-string version-2 deployment save, and shared fixed pixel mapping in battle and deployment. The battle screenshot confirms the grid occupies only the upper-left portion of the `1600×900` view while the selected-unit inspector consumes separate right-side space.
- 2026-08-30: Discussion separated visual enlargement from logical grid expansion. The user confirmed: keep `10×6`; responsively enlarge the battle presentation; expose all legal cells in columns `0..2`; let the hero move; retain one hero plus six-soldier capacity; persist exact positions with old-save compatibility.
- 2026-08-30: Execution preflight confirmed branch `main`, the existing intentionally dirty worktree, project authority, prior fixed-anchor contracts that this task supersedes, and the AI navigation, Godot UI, responsive UI, input, and save/load constraints. No implementation or authority file has changed yet beyond this confirmed activity record.
- 2026-08-30: Execution started on `main`. The exact pre-task porcelain was recorded before any authority/runtime mutation:

  ```text
   D content/ui/game_theme.tres
   M docs/testcases/alpha-manual-qa.md
   M scenes/app/GameRoot.tscn
   M scenes/ui/BattleReportScreen.tscn
   M scenes/ui/BattleScreen.tscn
   M scenes/ui/DeploymentScreen.tscn
   M scenes/ui/EventScreen.tscn
   M scenes/ui/HeroSelectScreen.tscn
   M scenes/ui/MainMenuScreen.tscn
   M scenes/ui/RecruitmentScreen.tscn
   M scenes/ui/RestScreen.tscn
   M scenes/ui/ResultScreen.tscn
   M scenes/ui/RewardScreen.tscn
   M scenes/ui/SettingsScreen.tscn
   M scenes/ui/ShopScreen.tscn
   M scenes/ui/TowerScreen.tscn
   M scenes/ui/components/ArmyOverview.tscn
   D scenes/ui/components/BattleReportMetric.tscn
   M scenes/ui/components/BattleReportUnitCard.tscn
   D scenes/ui/components/BattleReportUnitRow.tscn
   M scenes/ui/components/ChoiceCard.tscn
   M scenes/ui/components/DeploymentCell.tscn
   M scenes/ui/components/DeploymentUnitCard.tscn
   M scenes/ui/components/HeroAbilityPanel.tscn
   M scenes/ui/components/HeroCommandHud.tscn
   M scenes/ui/components/HeroDetailPanel.tscn
   M scenes/ui/components/HeroLibraryTile.tscn
   M scenes/ui/components/ResourceCostBadge.tscn
   M scenes/ui/components/SelectedUnitPanel.tscn
   M scenes/ui/components/StatBlock.tscn
   M scenes/ui/components/TraitBadge.tscn
   M scenes/ui/components/UnitChoiceCard.tscn
   D src/UI/BattleReportMetric.cs
   D src/UI/BattleReportMetric.cs.uid
   M src/UI/BattleReportScreen.cs
   M src/UI/BattleReportUnitCard.cs
   D src/UI/BattleReportUnitRow.cs
   D src/UI/BattleReportUnitRow.cs.uid
   M src/UI/HeroLibraryTile.cs
   M system-design/tower-autobattler-architecture.md
   M tests/GameplayContractSmoke.cs
   M tests/SemanticPresentationContractSmoke.cs
   M tests/UiSmoke.cs
   M tests/VisualCapture.cs
   M tests/VisualHierarchyContractSmoke.cs
   D work-items/active/tower-autobattler-dynamic-battle-report.md
   M work-items/active/tower-autobattler-semantic-icons-animated-portraits.md
  ?? content/ui/RealmTheme.tres
  ?? src/UI/BattleReportModels.cs.uid
  ?? src/UI/BattleReportTeamSummary.cs.uid
  ?? src/UI/BattleReportUnitCard.cs.uid
  ?? tests/BattleReportResponsiveContractSmoke.cs
  ?? tests/BattleReportResponsiveContractSmoke.cs.uid
  ?? tests/BattleReportResponsiveContractSmoke.tscn
  ?? tests/RealmThemeContractSmoke.cs
  ?? tests/RealmThemeContractSmoke.cs.uid
  ?? tests/RealmThemeContractSmoke.tscn
  ?? web/
  ?? work-items/active/flexible-three-column-deployment-and-battlefield-scale.md
  ?? work-items/active/game-mechanics-design-atlas.md
  ?? work-items/archive/realm-theme-migration-and-pixel-ui-removal.md
  ?? work-items/archive/tower-autobattler-battle-report-responsive-cards.md
  ?? work-items/archive/tower-autobattler-dynamic-battle-report.md
  ```

- 2026-08-30: Protected overlapping pre-task SHA-256 evidence was captured for prior accepted dirty work: `system-design/tower-autobattler-architecture.md` `E7A9012C…05AA`; `docs/testcases/alpha-manual-qa.md` `C1C3FA76…7900`; `scenes/ui/BattleScreen.tscn` `C1BEBF73…FACC`; `scenes/ui/DeploymentScreen.tscn` `1C6FFADC…9CE0`; `scenes/ui/components/DeploymentCell.tscn` `99121055…4949`; `tests/GameplayContractSmoke.cs` `9AEC416D…F6E7`; `tests/UiSmoke.cs` `28C6C785…5AA`; and `tests/VisualCapture.cs` `DBD8C83B…D3CA`. User Godot processes PID `23260` and `49048` were observed read-only and remain protected from control.
- 2026-08-30: Gameplay, system, and manual-QA authority now replace the fixed-anchor contract with the confirmed 18-cell player zone, movable hero, version-3 formation state, exact preview/spawn parity, transactional command boundary, and independent responsive board projections before runtime implementation began.
- 2026-08-30: Formation state milestone completed. Active-run version 3 adds JSON-safe `HeroCell` plus six `DeploymentCells` while preserving the six soldier identity entries. Version 2 migrates before validation to the exact legacy cells. Hero move/swap and soldier deploy/move/swap/replace/withdraw share one run-owned transaction with zero saves for rejection, one save for success, and complete identity/cell rollback after one failed save. Battle setup consumes exact persisted cells and rejects illegal/duplicate player positions instead of relocating them. Casualty cleanup preserves hero and unaffected cell records.
- 2026-08-30: Focused evidence is green: low-concurrency build `0 warnings / 0 errors`; `FormationDeploymentContractSmoke` emitted `FORMATION_DEPLOYMENT_CONTRACT_OK schema=v2-v3 invalid=duplicate,out-of-zone,out-of-bounds,wrong-count,stale-id commands=hero-move,hero-swap,soldier-move,soldier-swap,reserve-replace,withdraw rollback=exact save=once zone=18 spawn=exact casualty=stable projection=responsive-remap`. All save fixtures used `user://tests/formation-schema/` or in-memory fakes; the default active-run namespace was never read or written.
- 2026-08-30: Authored deployment and battle presentation completed. `DeploymentBoard` owns 18 authored `DeploymentCell` instances, floor/occupancy semantics, drag/drop, select-then-cell, focus neighbours, and an independent responsive projection. The hero is a normal movable formation piece in the deployment sidebar and board. `BattleBoard` owns a separate projection and inspector region; presenter scale, hit radius, snap points, queued/current motion coordinates, markers, drawing, and character presentation remap together on resize.
- 2026-08-30: Fresh native captures passed at both accepted resolutions. Screenshot-measured battle pitch is approximately `123×95` at `1600×900` and `98×76` at `1280×720`; deployment pitch is approximately `118×91` and `94×73`. Both show all `10×6` cells, the hero moved through authored input to column 3 / row 6, fixed actions, and an inspector outside the grid. Clicking the projected hero center opened `SelectedUnitDetails` at both sizes, providing production hit-test alignment evidence. No playable cell, fixed action, HUD, or required text was clipped; the only deployment overflow is the already-authored roster scroll owner.
- 2026-08-30: Final serial verification is green. Low-concurrency build reports `0 warnings / 0 errors`; RealmTheme, visual hierarchy, semantic presentation, window/portrait, responsive report, density report, dynamic report, fixture, content, gameplay, movement presentation, UI, Alpha run, formation deployment, isolated clean startup, and paired native VisualCapture all exit zero. `git diff --check` passes, and scans find no live old fixed-layout API, fixed-anchor copy, `NavigationAgent`/`NavigationServer`, or external donor runtime path.
- 2026-08-30: Transient unrelated writes to battle-report source/scenes between `15:28:17` and `15:30:14` invalidated one in-flight capture and briefly produced mismatched node-path/type errors. This task did not edit, revert, or format those surfaces. After their timestamps stabilized, the project was rebuilt and the complete 1280 and 1600 capture flows plus every report contract passed serially. The invalid transient run is not acceptance evidence.
- 2026-08-30: The main Agent independently verified the implementation and accepted the task. Independent build, focused formation, UI, movement-presentation, visual-hierarchy, isolated startup, static scans, original-resolution screenshot review, and Godot code review all passed. The activity is complete and archived without changing code, scenes, resources, other documents, branch, or protected processes during verification closeout.

## Resume Condition

Completed and independently accepted. Do not resume this activity. Any later change to formation, battlefield projection, save schema, or deployment interaction starts a new discussion and activity record.

## Verification Record

### Independent acceptance

- `dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal` independently completed with `0 warnings / 0 errors`.
- `FormationDeploymentContractSmoke` independently emitted the exact accepted marker: `FORMATION_DEPLOYMENT_CONTRACT_OK schema=v2-v3 invalid=duplicate,out-of-zone,out-of-bounds,wrong-count,stale-id commands=hero-move,hero-swap,soldier-move,soldier-swap,reserve-replace,withdraw rollback=exact save=once zone=18 spawn=exact casualty=stable projection=responsive-remap ui=authored-cells,hero-select,focus,inspector-reserved`.
- Independent regression markers include `UI_SMOKE_OK`, `MOVEMENT_PRESENTATION_CONTRACT_OK`, and `VISUAL_HIERARCHY_CONTRACT_OK`. `UiSmoke` retained only its established test-only `GrabFocus` warning.
- The correct isolated startup command, `Godot --headless --path . --quit-after 5 tests/CleanStartup.tscn`, exited `0` and did not use the player's default save namespace.
- `git diff --check` passed, and independent scans found no old fixed battlefield layout API or fixed-anchor copy.
- The main Agent inspected the six original `1600×900` / `1280×720` `DeploymentScreen`, `BattleScreen`, and `SelectedUnitDetails` images. The hero is at column 3 / row 6; the battlefield is materially enlarged; the inspector does not obscure playable cells; and fixed actions are not clipped.
- Independent Godot code review found no Critical issue and no required improvement. User Godot processes PID `23260` and `49048` remained responsive and untouched.

### Task-owned changes

- Authority and QA: `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, `docs/testcases/alpha-manual-qa.md`, and this activity record own the confirmed rules, architecture, manual checks, and evidence.
- Logical/state ownership: `BattlefieldLayout`, new `BattlefieldProjection`, new formation contracts, `ActiveRunDto`, `RunApplication`, and `GameRoot` own the 18-cell zone, v3 schema/migration, transactional commands, exact spawn cells, preview binding, and casualty stability.
- UI/presentation ownership: deployment models/controllers/board/cell/card, `BattleBoard`, `BattleScreenController`, `UnitMotionPresentationComponent`, and `UnitContentRoot` own typed input, hero presentation, independent projections, hit testing, scaling, and current/queued coordinate remapping. They do not alter navigation decisions or simulation cells.
- Authored scenes: battle/deployment screens plus deployment board/cell/card scenes own the independent inspector region, responsive containers, fixed actions, 18 reusable cells, and RealmTheme roles.
- Tests: new `FormationDeploymentContractSmoke` and isolated `CleanStartup.tscn`; focused updates to content/gameplay/movement/UI/visual hierarchy/VisualCapture replace old anchors, protect save namespaces, and prove the new paths. Battle-report implementation and its active density work item are explicitly outside this task.

### Persistence and transaction evidence

- The isolated version-2 JSON fixture migrates to hero `(0,3)` and soldier cells `(1,1)`, `(1,2)`, `(1,3)`, `(2,1)`, `(2,2)`, `(2,3)` while preserving the rest of the run DTO. Version-3 round-trip preserves arbitrary legal cells.
- Invalid duplicate, out-of-zone, out-of-bounds, wrong-count, and stale-identity v3 fixtures fail safely.
- Hero move/swap and soldier move/swap/reserve replacement/withdraw tests prove invalid commands save zero times, successful commands save once, and a forced save failure restores the exact hero cell, six identities/cells, and reserve membership after one failed call.
- Preview and battle configs contain the same hero/soldier logical cells; setup rejects illegal or duplicate player cells instead of relocating them. Casualty cleanup removes only the defeated identity and preserves every other cell.

### Verification evidence

- Build: `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` → `0 warnings / 0 errors`.
- Focused marker: `FORMATION_DEPLOYMENT_CONTRACT_OK schema=v2-v3 invalid=duplicate,out-of-zone,out-of-bounds,wrong-count,stale-id commands=hero-move,hero-swap,soldier-move,soldier-swap,reserve-replace,withdraw rollback=exact save=once zone=18 spawn=exact casualty=stable projection=responsive-remap ui=authored-cells,hero-select,focus,inspector-reserved`.
- Other green markers: `REALM_THEME_CONTRACT_OK`, `VISUAL_HIERARCHY_CONTRACT_OK`, `SEMANTIC_PRESENTATION_CONTRACT_OK`, `WINDOW_PORTRAIT_CONTRACT_OK`, `BATTLE_REPORT_RESPONSIVE_CONTRACT_OK`, `BATTLE_REPORT_DENSITY_CONTRACT_OK`, `DYNAMIC_BATTLE_REPORT_CONTRACT_OK`, `FIXTURE_CONTRACT_OK`, `CONTENT_CONTRACT_OK`, `GAMEPLAY_CONTRACT_OK`, `MOVEMENT_PRESENTATION_CONTRACT_OK`, `UI_SMOKE_OK`, and `ALPHA_RUN_OK`.
- Clean startup: `tests/CleanStartup.tscn` exits zero after five headless frames with `SaveNamespace = "tests/clean-startup"`; the default player save namespace is never loaded by this task's save/startup tests.
- Native capture: `VISUAL_CAPTURE_OK` at both `1280×720` and `1600×900`. Each resolution produced 27 UI frames plus 29 movement frames, 56 per size / 112 total under `res://.godot/qa`; later responsive/density report checks added 9 frames per size. Key evidence is `UI_<size>_DeploymentScreen.png`, `UI_<size>_BattleScreen.png`, and `UI_<size>_SelectedUnitDetails.png`.
- Static checks: `git diff --check` exits zero; old layout/fixed-anchor/navigation/external-path scans are empty.

### Review, warnings, and independent entry point

- Godot code review — Critical: none. Improvements required for acceptance: none. Positive: typed run-owned transactions, authored reusable controls, exact save rollback, logical/pixel separation, signal-up/method-down composition, and resize-safe ordered motion all match the project architecture. Reviewed against Godot 4.3+ practices and the project 4.7 constraints.
- Expected diagnostics: `ContentContractSmoke` deliberately emits its five lifecycle gate errors before its success marker. `UiSmoke` retains the established test-only `GrabFocus` warning and exits zero.
- Remaining subjective checks: an independent verifier should manually exercise continuous drag, gamepad traversal across cells and fixed actions, and repeated live resize during a real crowded moving battle. Static/native evidence found no clipping, stale hit target, inspector obstruction, or motion endpoint drift.
- The worktree remains intentionally dirty on `main`; unrelated pre-task and later battle-report changes are preserved. User Godot processes PID `23260` and `49048` remain running and untouched. No branch, commit, push, donor write, or real-save mutation occurred.
- Independent verifier entry: rebuild, run `tests/FormationDeploymentContractSmoke.tscn`, then run native `tests/VisualCapture.tscn` at `1600×900` and `1280×720` and compare the six key screenshots above before repeating the full serial matrix.

Independent acceptance is complete. This verification record is final.
