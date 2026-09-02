# Game UI Visual Language And Input Reliability

Status: Completed

## Goal

Repair deployment so it is operable through real player input, audit and correct the same interaction-lifecycle risk across the run flow, and reshape the game's interface around reusable visual language instead of persistent explanatory prose. The result must let players understand basic state, affordance, legal targets, consequences, and hierarchy primarily through layout, unit imagery, icons, color, shape, and motion while preserving precise details on demand.

## Confirmed Solution

1. Treat the current deployment failure as an event-source lifetime bug, not an input-discovery problem.
   - Selecting a roster card must not synchronously destroy the card that is still emitting `Pressed`.
   - Reuse bound roster cards for selection/state refresh; only reconcile children when roster composition changes, and defer any necessary destruction safely.
   - Audit every choice/list refresh reached from a click callback, including shop purchase refresh, for the same lifetime hazard.
2. Replace deployment-board prose with direct manipulation and visual states.
   - Empty deployment cells show no persistent labels or coordinates.
   - The first three columns are communicated by terrain tint, boundary, and authored cell state rather than a sentence on every cell.
   - Selection, hover/focus, legal destination, illegal destination, occupied swap, drag source, success, and failure each have a stable visual state.
   - Deployed units use their animated portrait/figure. Hero identity uses a gold, non-color-only marker; soldier responsibility/reach uses compact reusable symbols. Enemy, hazard, objective, and blocked markers use differentiated icons or textures instead of embedded explanatory copy.
   - Detailed names, coordinates, rules, and exact values remain available in a side detail or tooltip. Invalid actions produce one short transient response.
3. Apply one information hierarchy across the complete player flow.
   - Persistent surfaces show only the current objective, key resources, primary action, and immediately decision-relevant facts.
   - Hover/focus/selection reveals comparison information; complex rules and exact explanations live in detail panels, tooltips, logs, or result views.
   - Do not repeat the same fact in page copy, summary prose, card labels, and button text.
4. Reuse the existing `RealmTheme`, semantic icon catalog, animated unit portraits, and scene-authored component approach.
   - Extend the catalog or authored reusable component scenes only where a visual meaning is missing.
   - Do not restore the discarded ornate/pixel frame pack and do not generate new raster UI art in this task.
   - Colors retain stable semantics: health green, damage red, mana blue, shield steel gray, healing teal-green, gold/hero gold, reach cyan, danger crimson, and risk amber. No essential state may rely on color alone.
5. Correct the broader screen audit without changing gameplay rules.
   - Route: replace duplicated army prose with a compact icon/resource strip and make route-node type, risk, and reward identity visually dominant.
   - Hero/recruitment: keep the animated master-detail foundation, enlarge decision imagery where useful, reduce repeated field labels, and move extended descriptions to the detail layer.
   - Shop/reward: stop presenting unlike items as identical chest rows; show item/category identity, rarity, price, and effect hierarchy through distinct reusable visuals.
   - Event/rest/result/settings: reduce empty text panels; use semantic outcome clusters and concise actions while retaining exact consequences where the design intentionally exposes them.
   - Army overview: replace the sentence-style summary with a compact semantic strip; show portraits, bars, role/reach, and deployment status before prose, with details on selection.
   - Battle: remove persistent `英雄`/`近`/`远` words from the board where rings, glyphs, bars, and outlines can express them; keep precise unit state in selected detail.
   - Battle report: preserve its stronger color/section hierarchy, but remove repeated labels and obvious placeholder identity where a semantic visual already exists.
6. Make input acceptance reflect player behavior.
   - Real mouse input must cover roster selection, cell selection, board-unit selection, drag/drop, route choice, recruitment/reward choice, and shop purchase.
   - Keyboard/gamepad focus must reach equivalent actions and expose selected/legal/invalid states.
   - Direct `EmitSignal(Pressed)` remains acceptable for isolated logic tests but cannot be the evidence for input operability.

## Global Rule Change

Add one concise, durable section to `C:\Users\qs\.codex\AGENTS.md` under the game-development guidance. It must establish these cross-project rules without copying this task's project-specific details:

- Express game state and interaction in the order `space/shape/motion -> icon/color -> concise text -> detail`.
- Separate first-glance information, interaction-time information, and on-demand detail; do not repeat facts across surfaces.
- Require clear default, hover, focus, selected, drag, legal, illegal, success, and failure states where applicable.
- Keep icons/colors semantically consistent and provide non-color redundancy plus precise/accessibility detail.
- Accept operability through real mouse/touch/controller paths; direct signal emission is not interaction acceptance.

Do not duplicate this global principle in the project `AGENTS.md`; the global document already applies and the project file remains limited to project routing and hard constraints.

## Authority Impact

- Update `gameplay-design/tower-autobattler-core.md` with the long-lived player-facing visual-language and deployment-readability contract, without recording implementation detail.
- Update `system-design/tower-autobattler-architecture.md` with reusable presentation-state ownership, safe dynamic-list reconciliation, and the real-input acceptance boundary.
- Update `docs/testcases/alpha-manual-qa.md` with representative visual hierarchy, deployment, choice, hover/focus, drag/drop, and dual-resolution cases.
- Create no proposal archive. This document is the single task-level scope, progress, recovery, and verification record.

## Scope

- Global Codex game-UI rule in the user-level `AGENTS.md`.
- Deployment roster/card lifecycle, board cells and markers, direct selection, drag/drop, focus, feedback, and visual cleanup.
- Audit and correction of dynamic list refreshes triggered from active input callbacks.
- Reusable scene-authored resource/status strips, decision/outcome presentation, board state indicators, tooltips/details, and semantic Theme variations required by the confirmed screen audit.
- Integration across main route decisions, hero selection, recruitment, reward, shop, event, rest, Army overview, battle HUD/board, battle report, result, and settings where the current persistent prose or missing feedback violates the confirmed hierarchy.
- Focused contracts, real-input regression, paired visual captures at 1280x720 and 1600x900, and manual visual review.

## Non-Goals

- No combat, balance, economy, tower-generation, formation-capacity, save-schema, hero-command, or content-definition rule changes.
- No new heroes, soldiers, enemies, items, floors, events, or rewards.
- No replacement of `RealmTheme`, no return to discarded decorative UI frames, and no image generation.
- No dependency on `D:\godot\rpg` or `D:\godot\realm`; both remain outside runtime and modification scope.
- No full accessibility system, localization system, audio suite, or game-wide cinematic/VFX pass. Preserve current keyboard/gamepad accessibility and provide non-color/text alternatives proportionate to changed controls.
- No unrelated refactor or cleanup of existing user worktree changes.

## Hard Constraints

- Work on `main`; do not create or switch branches.
- Preserve the existing dirty worktree and all unrelated user changes. Never reset or revert the repository.
- Keep every concrete hero, soldier, enemy, and item independently instantiable as its own `.tscn` scene.
- Repeated and visually important UI remains authored as `.tscn`/`.tres`/Theme resources. Runtime code binds data and state; it does not construct whole screens ad hoc.
- Simulation/application remain authoritative for gameplay legality and persistence. Presentation renders state and requests typed actions.
- Do not synchronously free an object while it or one of its descendants is dispatching the input event that triggered the refresh.
- Do not stop, restart, or otherwise control the user's Godot/editor/game processes.
- Use low-concurrency builds and avoid unnecessary editor launches/import cycles.

## Acceptance Criteria

### Deployment And Interaction Reliability

- A real mouse click on a roster unit selects it without `Object is locked`, invalid `free`, or any other engine error.
- A subsequent real click on a legal cell moves/places/swaps the selected hero or soldier as allowed by the existing formation contract and saves exactly once.
- Real drag/drop supports reserve-to-cell and board-to-cell movement/swap; cancellation and illegal targets neither mutate nor save.
- Repeated selection, deselection, refresh, withdrawal, back, and start actions remain operable. Necessary child removal occurs only outside the active event-source lifetime.
- Equivalent keyboard/gamepad focus can select a unit and destination; focus is never lost behind overlays or disabled empty cells.
- Shop refresh and every audited dynamic decision list complete through real input without destroying the active event source.

### Visual Language

- Empty deployment cells and deployment-zone cells contain no persistent `可部署`, row/column coordinates, or instructional prose.
- A player can identify deployment zone, selected unit, legal/illegal destinations, swap target, hero, soldier reach/responsibility, enemy, hazard, blocked cell, and objective without reading a tutorial sentence and without relying only on color.
- Route, recruitment, reward, shop, event, rest, Army overview, and battle surfaces follow the confirmed first-glance / interaction / detail hierarchy; duplicated prose summaries and identical unrelated decision rows are removed.
- Exact stats, complex rules, and action consequences remain reachable in detail or tooltip surfaces. Reducing copy must not remove decision-critical precision or accessibility names.
- `RealmTheme` and the semantic palette remain authoritative; no discarded pixel frame asset is reintroduced.
- At 1280x720 and 1600x900, primary actions remain visible, cards and portraits do not clip, board content does not overlap HUD, and detail surfaces stay readable.

### Verification

- Focused contracts fail before the fixes for the real deployment click/lifetime and prohibited cell prose, then pass after implementation.
- Automated evidence uses actual viewport input for the confirmed interaction paths; direct signal-only tests are not cited as operability proof.
- Low-concurrency .NET build completes with zero warnings/errors.
- Existing content, gameplay, movement/presentation, UI, full-run, Theme, semantic, hierarchy, formation, battle-report, and clean-startup regressions remain green except for documented deliberate negative fixtures.
- Fresh paired captures cover every changed screen plus deployment default, selected, legal-target, illegal-target, drag/swap, Army detail, battle unit identity, and representative hover/focus states. Manual inspection records remaining subjective risks.
- Independent read-only verification reviews scope, real-input evidence, engine logs, both resolutions, and preservation of gameplay/scene contracts before the task is marked completed and archived.

## Progress

- 2026-08-30: User reported that deployment is not operable and rejected persistent deployment-cell prose in favor of visual language.
- 2026-08-30: Read-only diagnosis confirmed `DeploymentUnitCard.OnPressed -> DeploymentScreenController.OnPieceSelected -> Refresh` synchronously frees the active card. Godot logs repeated `Object is locked and can't be freed` and invalid `free` errors.
- 2026-08-30: Read-only full-flow visual audit covered deployment, route, hero selection, recruitment, reward, shop, event, rest, Army overview, battle, selected-unit detail, battle report, result, and settings at 1280x720, with paired 1600x900 evidence available.
- 2026-08-30: The audit found a project-wide hierarchy problem: semantic icons were added beside prose without replacing persistent explanation, decision types share overly similar row structures, state feedback is inconsistent, and signal-emission tests overstate actual operability. Shop refresh contains the same active-list destruction risk pattern and requires real-input verification.
- 2026-08-30: User expanded the scope to the other game surfaces and requested the visual-language principle as a global Codex game-development rule.
- 2026-08-30: User confirmed the complete repair, visual audit, real-input acceptance, authority synchronization, and global-rule scope. This activity task was created as the execution authority.
- 2026-08-30: Execution began as the sole writer on `main` after fully reading this task, both Agent-rule levels, gameplay/system/manual-QA authority, and the required Godot UI, input, responsive, resource, scene/component, C#, signal, testing, and review skills. Architecture ownership remains UI Presentation and Input lifecycle: the run application stays authoritative for legality and exactly-once persistence; authored controls emit typed requests; list owners reuse/reconcile children without destroying an active input source.
- 2026-08-30: Protected worktree baseline contains 94 porcelain entries spanning accepted RealmTheme, formation, battlefield projection, semantic portrait, report, visual-capture, test, authority, archived work-item, and unrelated `web/` changes. Global Agent rules hash is `121014A3CE92D3E17613F5EEC171D0664D054B07FC2CED24BA1C56307A68B466`, RealmTheme is `A5375C3C4295AB58D2F964067DC45362AA8E1ABA3EC38CEA1C272A1FED7DA7AB`, and `project.godot` is `F48C02422EDF9E55F50FCE8237B98856DD11E5673D9837F6DB19A5E48B401EB2`. Existing Godot editor PIDs `23260` and `49048` remain outside executor control. No baseline deletion, untracked file, unrelated change, editor, or player process may be reset, reverted, cleaned, stopped, or restarted.
- 2026-08-30: Authority synchronization completed before production or test implementation. The global game-development rule now establishes visual-language order, three information layers, complete interaction states, semantic/non-color redundancy, and real-input acceptance without project-specific detail. Gameplay authority defines direct-manipulation deployment and equivalent mouse/drag/focus paths; system authority owns authored presentation states, stable-id list reconciliation outside active event dispatch, symmetric signal cleanup, and viewport-input acceptance; manual QA now covers prohibited cell prose, real roster/cell/drag/shop input, lifecycle errors, exact-once saves, focus equivalence, both resolutions, and full-flow information hierarchy.
- 2026-08-30: Focused RED established as `GameUiInteractionReliabilityContractSmoke` after a zero-warning/zero-error low-concurrency build. It drives actual viewport mouse motion plus press/release through a deployment roster card and cell, records engine errors, checks event-source instance reuse, and rejects authored/runtime text on empty cells. The unmodified production path exits `1` for exactly the confirmed defects: `Object is locked and can't be freed`, invalid `free` on `DeploymentUnitCard`, active card replacement, authored `可部署\n1-1`, and runtime `可部署/2-3`. The typed roster-then-cell request still fires once, isolating the failure to presentation lifetime rather than formation-command discovery or legality.
- 2026-08-30: Interaction-lifecycle GREEN now covers both confirmed active-event refresh paths through actual viewport mouse motion and press/release. Deployment reuses the selected roster card, then emits exactly one typed legal-cell move; shop purchase reuses the same stable-id `ChoiceCard`, grants exactly one bound item, remains operable, and logs no locked/free/disposed engine error. Marker: `GAME_UI_INTERACTION_RELIABILITY_CONTRACT_OK input=viewport-mouse roster=reused cell=selected typed-move=once shop=reused empty-cell-copy=none engine-errors=none`; isolated Godot 4.7 console exit `0`, low-concurrency build `0` warnings / `0` errors.
- 2026-08-30: A separate writer updated/created the `BattleReportLeaderboard` / detail / roster / models file group during this execution. Those changes are not owned by this task and are now a protected external-concurrency boundary: this executor will not edit any `BattleReport*` source, scene, model, or test file. Battle-report requirements in this task are limited to read-only regression verification so the independent dynamic/statistical report workstream is not overwritten.
- 2026-08-30: The authored visual-language integration now covers deployment cells and markers, battleboard identity markers, route hierarchy, Army resource summary/detail, reward/shop item identity, battle status resources, and event/rest semantic outcome clusters. Deployment cells render animated portraits plus non-color hero, responsibility, and reach symbols; empty cells remain copy-free; the shared Theme owns default/hover/focus/selected/legal/illegal/swap/drag/success/failure roles. Route removes duplicate army/gold/hero prose in favor of the always-available Army strip. Reward/shop use a distinct item card with icon, category, rarity rail, effect, and semantic price/rarity. Battle status is a time/allies/enemies/gold strip, and in-world hero/reach labels are semantic textures. Event/rest use the reusable `OutcomeActionButton` with risk/health/healing/gold facts and tooltip precision; hero selection no longer repeats its master-detail content as a generic hint.
- 2026-08-30: New focused visual marker is GREEN: `GAME_UI_VISUAL_LANGUAGE_CONTRACT_OK deployment=portrait+semantic states=9 board-markers=icons army=resource-strip items=distinct battle=status-strip prose=layered`, Godot 4.7 console exit `0`. Expanded real-input marker is GREEN through viewport mouse plus focus action: route chosen, recruitment chosen, reward chosen, roster card reused, board unit selected, drag emitted one typed request, shop card reused, no engine lifetime errors; exit `0`. RealmTheme, semantic presentation, visual hierarchy, and formation deployment contracts also exit `0`.
- 2026-08-30: A temporary diagnostic build excluding only the externally changing `tests/BattleReportDensityContractSmoke.cs` proves this task's source compiles with `0` warnings / `0` errors. The unfiltered build remains outside this executor's repair authority while the protected BattleReport writer is active: its latest failure is the external density test missing the `UnitRole` namespace. `UiSmoke` proceeds through this task's changed route/deployment/reward/Army surfaces, then fails inside the externally changing enemy report tab; no BattleReport file is modified here.
- 2026-08-30: The protected BattleReport workstream stabilized. A fresh unfiltered low-concurrency build now completes with `0` warnings / `0` errors. Focused post-item-icon verification is GREEN for `GameUiInteractionReliabilityContractSmoke`, `GameUiVisualLanguageContractSmoke`, `ContentContractSmoke`, and `UiSmoke`; the content gate's logged errors are its deliberate negative fixtures, and the UI smoke's one focus warning is the deliberate attempt to focus a disabled underlying modal control before confirming focus did not escape.
- 2026-08-30: Final serial regression is GREEN for RealmTheme, semantic presentation, visual hierarchy, window/portraits, formation/deployment, fixtures, gameplay, movement presentation, complete Alpha run, battle-report density/responsiveness/dynamic/statistical views, and isolated clean startup. Every process exited `0` and emitted its contract marker where applicable.
- 2026-08-30: Fresh task-focused captures under `.godot/qa/` contain 16 changed-state frames at each accepted resolution (`1280x720` and `1600x900`), including deployment default/selected/legal/swap/focus/illegal/drag/failure, Army detail, item reward/shop identity, event/rest outcomes, and battle semantic status. Manual inspection after the item correction found no blocking clipping, overlap, lost primary action, or ambiguous deployment state. The battle status capture demonstrates icon-based unit identity and resources; the focused frame does not visibly open the selected-unit detail panel, whose interaction/detail contract remains covered by `UiSmoke` and the existing paired `SelectedUnitDetails` captures.
- 2026-08-30: Final static and Godot review is clean for the task-owned surface: `git diff --check` has no findings; runtime source constructs no `Theme`, `StyleBox`, or replacement control tree; all added C# event connections disconnect symmetrically; stable-id reconciliation defers stale event-source deletion; 12/12 item definitions have non-null, unique semantic icon paths; 12/12 concrete items remain independent `.tscn` scenes; content validation publishes all 57 entries including 45 independently validated unit scenes; no concrete item-id presentation dispatch or runtime donor path was introduced.

## Resume Condition

Completed and independently verified. Do not resume this task. Any future regression or materially new UI direction requires a new activity task linked to the accepted gameplay, architecture, and QA authority.

## Verification Record

### Independent Verification

- 2026-08-30: Independent read-only verification confirmed the branch is `main` and the unfiltered low-concurrency build succeeds with `0` warnings / `0` errors.
- Fresh Godot 4.7 console runs exited `0` with complete markers for `GameUiInteractionReliabilityContractSmoke`, `GameUiVisualLanguageContractSmoke`, `ContentContractSmoke`, and `UiSmoke`. The four content-gate errors and the one modal `GrabFocus` warning match the documented deliberate negative fixtures rather than production failures.
- Representative `1280x720` and `1600x900` captures were manually inspected with no blocking layout, hierarchy, item-identity, deployment-state, or readability issue.
- The global Agent UI rule remains concise and project-agnostic. Runtime source contains no `D:\godot\rpg` or `D:\godot\realm` dependency and no runtime `Theme`, `StyleBox`, or replacement control-tree construction.
- All 45 concrete unit scenes remain independent (`8` heroes, `24` soldiers, `13` enemies); all 12 concrete item scenes remain independent; all 12 item icon paths are non-null and unique. `git diff --check` is clean.
- `BattleReport*` files remained an external workstream boundary and were excluded from this task's implementation ownership. Independent verification accepted the task with no remaining blocker.

### Changed Surface Groups

- Interaction lifetime: deployment roster/card reconciliation, board selection and drag/drop, shop/choice reconciliation, focus preservation, typed formation requests, and concise success/failure feedback.
- Authored visual language: deployment cells/markers, Army resource strip/detail rows, distinct item cards, battle status strip, battleboard identity glyphs, and event/rest outcome buttons, all using `RealmTheme`, semantic icons, and independent portrait/content scenes.
- Flow integration: tower route, hero selection, recruitment, reward, shop, event, rest, Army overview, deployment, battle, result, and settings copy/hierarchy without gameplay, balance, save-schema, or content-rule changes.
- Authority and acceptance: global game-UI rule, player-facing visual/deployment contract, presentation/list-lifetime architecture, manual QA, focused real-input and visual contracts, and paired changed-screen capture scene.

### RED And GREEN Evidence

- RED reproduced the original real viewport deployment click failure with `Object is locked and can't be freed`, invalid `free`, active-card replacement, and persistent deployment-cell copy while still proving the typed move request was discoverable.
- Final GREEN interaction marker: `GAME_UI_INTERACTION_RELIABILITY_CONTRACT_OK input=viewport mouse+focus route=chosen recruitment=chosen reward=chosen roster=reused board=selected drag=typed shop=reused empty-cell-copy=none engine-errors=none`.
- Final GREEN visual marker: `GAME_UI_VISUAL_LANGUAGE_CONTRACT_OK deployment=portrait+semantic states=9 board-markers=icons army=resource-strip items=distinct battle=status-strip prose=layered`.
- Real-input coverage drives viewport mouse motion/press/release for route, recruitment, reward, roster, board-unit, deployment-cell, drag/drop, and shop purchase, plus focus and `ui_accept` equivalence. Direct signal emission is not used as operability evidence.
- Focused interaction logs contain no locked-object, invalid-free, disposed-object, or duplicate-action engine error. `ContentContractSmoke` deliberately injects structural/ready/process/exit failures and then confirms they reject publication before the production catalog publishes successfully. `UiSmoke` deliberately calls `GrabFocus` on a modal-disabled underlying control, producing one warning before proving focus cannot escape.

### Build And Regression Results

- `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`: succeeded, `0` warnings, `0` errors.
- GREEN markers: `REALM_THEME_CONTRACT_OK`, `SEMANTIC_PRESENTATION_CONTRACT_OK`, `VISUAL_HIERARCHY_CONTRACT_OK`, `WINDOW_PORTRAIT_CONTRACT_OK`, `FORMATION_DEPLOYMENT_CONTRACT_OK`, `FIXTURE_CONTRACT_OK`, `CONTENT_CONTRACT_OK`, `GAMEPLAY_CONTRACT_OK`, `MOVEMENT_PRESENTATION_CONTRACT_OK`, `UI_SMOKE_OK`, `ALPHA_RUN_OK`, `BATTLE_REPORT_DENSITY_CONTRACT_OK`, `BATTLE_REPORT_RESPONSIVE_CONTRACT_OK`, `DYNAMIC_BATTLE_REPORT_CONTRACT_OK`, and `STATISTICAL_BATTLE_REPORT_CONTRACT_OK`.
- Isolated `CleanStartup.tscn` with `--quit-after 5` exits `0`. No user editor/game process was stopped, restarted, or otherwise controlled.

### Paired Capture And Manual Review

- Review `.godot/qa/UI_1280x720_<state>.png` and `.godot/qa/UI_1600x900_<state>.png` for the 16 task-specific states authored by `GameUiChangedScreensCapture.tscn`.
- Manual inspection confirms the deployment states are distinct without cell prose, item icons and rarity rails differentiate reward/shop choices, Army and outcome clusters fit both resolutions, and battle semantic status remains readable. No blocking issue remains.
- Subjective residual risk: the focused `BattleSemanticStatus` frame proves the board/status visual layer but does not visibly show the selected-unit detail panel. Existing paired `SelectedUnitDetails` frames plus the real board-selection UI smoke cover the on-demand detail path; an independent verifier may recapture that one state if desired without changing production code.

### Independent Verification Entry Point

1. Read this task, the global/project Agent rules, `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, and `docs/testcases/alpha-manual-qa.md` read-only.
2. Review task-owned diffs while preserving the dirty worktree and treating every `BattleReport*` file as externally owned. Confirm concrete units/items remain independent scenes and no runtime dependency points to `D:\godot\rpg` or `D:\godot\realm`.
3. Run the low-concurrency build, the two focused game-UI contracts, `ContentContractSmoke`, `UiSmoke`, then the serial regression marker list above and isolated clean startup. Interpret only the documented content negative-fixture errors and modal focus warning as expected.
4. Inspect all 32 task-specific paired captures, prioritizing deployment default/selected/legal/swap/focus/illegal/drag/failure, Army detail, reward/shop identity, event/rest outcomes, and battle semantic status.
5. If implementation scope, engine logs, real-input evidence, both resolutions, and scene/content contracts pass independently, mark the task complete and archive it. Otherwise return the same task to `Needs Discussion` or execution with the exact failing evidence.
