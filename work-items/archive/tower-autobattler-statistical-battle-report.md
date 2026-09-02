# Statistical Battle Report

Status: Completed  
Confirmed: 2026-08-30

## Goal

Replace the current battle-report card wall with a statistics-first result screen. The player must be able to understand the outcome, the three leading contributors, the main team difference, and the ordered unit comparison within a few seconds. Preserve the accepted immutable battle facts, responsive containment, dimension/allegiance switching, focus behavior, scroll ownership, and exactly-once continue routing.

## Confirmed Diagnosis

- The current report contains valid statistics but expands every unit into a large self-contained card. This gives each field presentation space without creating a shared comparison scale, so the screen reads as accumulated information rather than analysis.
- Repeating identity, award, primary metric, four supporting facts, and a contribution bar inside every card wastes vertical space and makes rank and relative gaps difficult to scan.
- The prior responsive and density repairs solved internal clipping and increased visible card count; those protections remain useful, but the card wall is no longer the report's primary information architecture.
- Existing immutable battle data and `BattleReportViewModels` already provide the required outcome, team totals, stable rankings, shares, DPS/HPS, action/event counts, final health, active lifetime, positive dimension leaders, and environment reconciliation. No new combat ledger is required.

## Confirmed Solution

### Fixed result header

- Keep victory/defeat/timeout, encounter, deterministic duration, successful hero-command count, and command gold cost.
- Keep only a small set of first-glance battle totals in the fixed header, including survivors and team damage/healing context. Do not repeat the same totals again in every following region.

### Overview page

- Show one compact player-versus-enemy comparison with common scales for remaining health, effective damage, and effective healing, plus survivors/casualties or kills where useful.
- Show exactly three explainable leader summaries: output leader, damage-taken leader, and healing leader. Use the existing positive awards and retain tied leaders where required; an all-zero category has no leader.
- Show a compact roster portrait strip for both sides and positive environment damage when present.
- Do not show a composite MVP, combat rating, invented score, timeline, skill breakdown, or damage taxonomy.

### Ranked dimension pages

- `输出` uses one fixed-column leaderboard: rank, unit, effective damage, team share, DPS, kills, and attack actions.
- `生存` uses one fixed-column leaderboard: rank, unit, effective damage taken, shield absorbed, final health, active lifetime, and remaining-health ratio.
- `治疗` uses one fixed-column leaderboard: rank, unit, effective healing, team share, HPS, and effective healing-event count. A zero-healing side keeps the deliberate Chinese empty state.
- Every row uses the same authored column widths/alignment for the active dimension. Primary contribution bars use one common scale within the selected side and dimension, so magnitude and gaps can be compared directly.
- Ranking remains deterministic and follows the existing typed view-model order. The selected allegiance changes the ranked side without changing the fixed two-team context.

### On-demand unit detail

- Each leaderboard row is an authored focusable/clickable template with portrait, identity, role/status, rank, dimension values, and a common-scale contribution bar.
- Selecting or focusing a row reveals one authored detail panel for that unit. The detail panel may expose the complete existing unit facts and awards, but those facts are not expanded for every unit by default.
- Selection survives normal dimension/allegiance refresh only when the same runtime identity remains valid; otherwise select the first available row. Mouse, keyboard, and gamepad must reach rows, dimension/allegiance controls, detail, and continue without stale focus.

## Authority Impact

- Update `system-design/tower-autobattler-architecture.md` before report production changes. Replace the card-grid ownership clause with a statistics-first overview/leaderboard/detail contract while preserving immutable data derivation, authored resources, focus, responsive containment, and continue routing.
- Update `docs/testcases/alpha-manual-qa.md` before report production changes. Replace card-count/density acceptance with three-second conclusion readability, fixed-column comparison, common-scale bars, dimension column switching, on-demand detail, and dual-resolution containment.
- Player-facing combat/statistic rules and immutable battle-result collection do not change. No gameplay-design update is required unless implementation proves a missing statistic authority, in which case stop and return to discussion.
- This work item owns battle-report information architecture. The active global UI/input task retains RealmTheme, global interaction language, deployment, other screens, and cross-screen lifecycle work; it must not be rewritten from this task.

## Scope

- Authored battle-report screen composition for fixed header, overview, ranked dimension pages, compact roster strips, row selection, and one on-demand unit detail region.
- New or adapted authored report components for team comparison, leader summary, leaderboard header/row, and unit detail.
- `BattleReportScreen` binding, page/allegiance switching, stable row selection, focus recovery, responsive breakpoint selection, and exact-once continue preservation.
- Reuse of existing report view models, with presentation-only helpers for overview leaders, row columns, and common-scale bar values when necessary.
- Migration of report-specific production/test consumers away from `BattleReportUnitCard`. Retire card-only resources only after every report consumer and contract has migrated.
- Focused RED/GREEN contracts, relevant regressions, and fresh 1280x720 / 1600x900 visual evidence.

## Non-Goals

- No new battle counters, per-tick timeline, damage source/type/skill breakdown, crowd-control score, economy analysis, balance change, or save-schema change.
- No opaque MVP/composite score, invented efficiency rating, or fabricated comparison.
- No change to battle simulation, movement, deployment/formation, rewards, navigation, persistence, portraits, semantic catalog, `web/`, or unrelated screens.
- No RealmTheme redesign, pixel UI, new raster/vector art, global scale change, or runtime-built replacement UI tree.
- No requirement to keep the old card wall, its two-column count targets, or every card-density implementation detail after all valid consumers migrate.

## Hard Constraints

- Work on current `main`; create no branch, commit, push, or history rewrite.
- Preserve the dirty worktree and every unrelated change. Do not reset, revert, clean, stop, or restart the user's Godot/editor/game processes.
- Read this work item, project rules, system authority, manual QA, the active semantic-portrait boundary, and the active global UI/input boundary before editing.
- Synchronize the narrow architecture and QA clauses before production scenes, scripts, or tests.
- Author repeated structure and layouts in `.tscn` resources. C# owns typed derivation, binding, ordering, selection, and interaction; it does not construct a replacement control tree.
- Reuse `res://content/ui/RealmTheme.tres`, `UnitPortrait`, semantic icons/chips, immutable result snapshots, and existing report derivation. Do not modify RealmTheme unless a missing report-only stable role is proven and returned to discussion.
- One explicit report content scroll owner may contain the overview or leaderboard body. Outcome, page/allegiance controls, and continue remain reachable at 1280x720 and 1600x900.
- Do not use raw `Control.Scale`, global content scaling, or window-resolution guesses to hide minimum-size conflicts. Use containers, actual allocated width, authored breakpoints, clipping/overrun policy, and local scroll ownership.
- Avoid concurrent Godot/import/build processes. The user has explicitly allowed implementation to continue without treating unrelated compilation contention as a design blocker; run verification only when safe and record any external gap rather than changing unrelated work.

## Acceptance Criteria

### Statistical readability

- Within three seconds at the default overview, a player can identify the outcome, major team difference, output leader, damage-taken leader, and healing leader without reading every unit's details.
- Overview uses shared comparison scales and compact leader/roster summaries rather than a wall of fully expanded unit cards.
- Output, survival, and healing each show one fixed-column leaderboard whose complete column set changes with the selected dimension.
- A representative six-unit side is directly comparable in a single aligned list: rank and unit identity stay fixed, numeric columns align, and the common-scale bars make the largest and smaller contributions visibly comparable.
- No composite MVP, rating, fake timeline, skill split, damage type, or other unavailable statistic appears.

### Interaction and detail

- Dimension and allegiance controls update the complete ranked dataset deterministically while preserving an unambiguous pressed state.
- Clicking or focusing a row selects one unit and reveals its authored detail panel; other rows remain compact. Selection/focus never targets a freed control and falls back predictably when the runtime id is absent after a page/team switch.
- Healing-zero state is deliberate and still allows overview access. Tied positive leaders remain explainable, and all-zero categories award nobody.
- Continue remains fixed/reachable and emits exactly once for ordinary victory, final victory, defeat, and timeout routes.

### Responsive containment

- At 1600x900 and 1280x720, header, comparison, page/allegiance controls, leaderboard header, selected-row indication, unit detail access, and continue do not clip or overlap.
- Long current Chinese names, large numeric values, hero/summon/status identity, semantic icons, and focus indicators stay contained through authored truncation/wrap/detail behavior. Horizontal comparison columns do not drift between rows.
- The content scroll owner handles additional rows or detail height; fixed controls do not disappear. Repeated resize is reversible and preserves dimension, allegiance, exact row count, and valid focus/selection.

### Verification

- Write a focused failing statistical-report contract before production edits. It must reject the card-wall primary surface and require authored overview/leaderboard/detail resources, fixed dimension columns, common-scale bars, stable row selection, and preserved continue routing.
- Update/migrate the prior dynamic, responsive, density, UI, semantic, hierarchy, and visual-capture contracts so they test the new authority rather than obsolete card counts or node paths.
- Verify view-model ranking/share/rate/leader/zero-state behavior without adding unavailable battle facts.
- Generate and manually inspect player/enemy overview plus output/survival/healing or zero-state captures at both supported resolutions. Record clipping, density, comparison alignment, selection/detail, and fixed-action results.
- Run a low-concurrency build and relevant focused regressions when the external build/import set is idle. Final scope audit confirms no Theme, gameplay, save, routing, battlefield, deployment, semantic-catalog, portrait, `web/`, or unrelated-task drift.

## Progress

- 2026-08-30: User rejected the card-wall report as information accumulation rather than statistics and explicitly referenced the conclusion/ranking comparison pattern used by established autobattlers.
- 2026-08-30: Read-only audit confirmed the existing immutable result and view-model layers already provide the required team totals, stable dimension rankings, shares, DPS/HPS, counts, leaders, and environment reconciliation. The presentation layer is the limiting factor.
- 2026-08-30: User confirmed `fixed result header + overview conclusions/leaders + dimension-specific fixed-column leaderboard + one on-demand unit detail panel`, with no fabricated statistics.
- 2026-08-30: Architecture preflight assigns the change to UI Presentation. Combat/result collection, routing, Theme, semantic catalog, portraits, formation, movement, and other screens remain authoritative and frozen.
- 2026-08-30: This self-contained activity task was created on `main` with the dirty worktree preserved. No authority, production, test, build, import, or capture change for this task has yet been made.
- 2026-08-30: Execution began on `main` at `62f13f466fea5d3c28625ec4b62391edf7555f10` after rereading project rules, system/manual-QA authority, semantic-portrait and global UI/input boundaries, and the required Godot UI/responsive/C#/testing skills. Architecture preflight keeps ownership in UI Presentation: immutable result collection, gameplay authority, simulation, deployment/formation, routing/save, RealmTheme, semantic catalog, portraits, other screens, and `web/` remain frozen. Protected baselines are RealmTheme `A5375C3C4295AB58D2F964067DC45362AA8E1ABA3EC38CEA1C272A1FED7DA7AB`, BattleSimulation `B38BE641C2988E1C5B8BC4022D68FFD232223B1FFE8F83050EE5859BFE59A0B0`, BattleModels `7B83446F188C4817DF62EFAE8D6512ED9345FC11DB7DC7F311A7E74ED83EDA22`, gameplay core `3762EB29BDB69271356FC5408953D1EF7317E5C5222C184EC5607BAF40B8D149`, and presentation-extensible BattleReportModels `7F73670C6559AF4A1566F6E4383564B49E3196F083D098E5F2817A2EC5BBCF5B`.
- 2026-08-30: Before production or test edits, system architecture and manual QA were narrow-merged to replace obsolete card-grid/card-count ownership with the confirmed fixed header, shared-scale overview, three positive leader summaries, compact two-side roster strips, dimension-specific fixed-column leaderboards, common-max bars, stable row selection, one on-demand detail panel, deliberate healing-zero state, responsive containment, and exact-once continue contracts. Gameplay authority was not modified.
- 2026-08-30: Added and proved focused `StatisticalBattleReportContractSmoke` RED after a low-concurrency build completed with 0 warnings/0 errors. The unmodified report exits non-zero only for the intended authority gaps: six missing authored statistical components, missing overview/leaderboard/detail surfaces, retained card-wall primary surface, missing fixed leaderboard columns/common-scale maximum/leader derivation, and missing stable runtime-id row selection. The RED also retains exact-once continue coverage and contains no test-harness or compilation failure.
- 2026-08-30: First production milestone authored the statistical screen skeleton and pure presentation derivation: shared-scale comparison, exactly three positive/tied leader groups, both roster strips, fixed-column header/row, selected-side common maximum, stable runtime-id selection, deliberate healing-zero state, and one detail panel. An incremental build then became externally blocked by a concurrent active-task edit in `ArmyOverviewModels.cs` missing its own `Texture2D` namespace; that file is outside this task and was not modified. Per confirmed execution authority, implementation continues while the external compile gap is recorded for later serial verification.
- 2026-08-30: Completed the authored production surface. `BattleReportScreen` now owns a fixed result/team header, statistics-first overview, output/survival/healing leaderboard pages, player/enemy switching, stable runtime-id selection, one selected-unit detail panel, deliberate zero-healing state, and exactly-once continue. New authored components are `BattleReportComparison`, `BattleReportLeaderSummary`, `BattleReportRosterStrip`/`BattleReportRosterPortrait`, `BattleReportLeaderboardHeader`/`BattleReportLeaderboardRow`, and `BattleReportUnitDetail`; C# only derives/binds runtime data and instantiates the authored repeated templates.
- 2026-08-30: Extended `BattleReportViewModels` only with presentation derivation required by the confirmed report: positive/tied output, damage-taken, and healing leaders; deterministic rosters; dimension-specific stable ranking; shares/rates; and selected-side common primary maximum. Combat snapshots and collection were not changed. All-zero healing awards nobody and exposes the Chinese empty state.
- 2026-08-30: Migrated valid report consumers and contracts away from the obsolete expanded-card surface and retired the old report card/row/metric resources after repository search found no valid runtime consumer. A separate active semantic-portrait work item still contains a historical prose reference to `BattleReportUnitRow`; it is outside this task's authority and is recorded for its owner rather than edited here.
- 2026-08-30: Focused GREEN is `STATISTICAL_BATTLE_REPORT_CONTRACT_OK overview=shared-scales leaders=3 leaderboard=fixed-columns bars=common-max selection=stable detail=single continue=once`. Dynamic, responsive, and density contracts also pass. Density evidence measures six deterministic rows at a uniform `62.0px` and a common-max leader bar of `100.0` at both 1600x900 and 1280x720.
- 2026-08-30: A final `UiSmoke` pass exposed one report-local stale flag: after enemy zero-healing state, switching to overview hid the page but left the empty-state node's own `Visible` property set. `BindOverview` now explicitly clears it. The rerun passes all report routes, active tabs, focus, explicit zero state, and exactly-once post-report routing. Its remaining focus warning is emitted by the separately owned Army drawer check at `UiSmoke.cs:95`, not by the report.
- 2026-08-30: Fresh non-headless fixture captures were generated and manually inspected for both 1600x900 and 1280x720. Evidence covers player/enemy overview, player/enemy output, survival, healing, enemy zero-healing, long Chinese names, large values, selected-row detail, common-scale bars, fixed columns, local scroll containment, and fixed continue. Files are under `.godot/qa/UI_<resolution>_BattleReportStatisticalOrdinary*.png` and `.godot/qa/UI_<resolution>_BattleReportStatisticalStress*.png`.
- 2026-08-30: Final low-concurrency build succeeds with 0 warnings/0 errors. Passing regression markers: `DynamicBattleReportContractSmoke`, `BattleReportResponsiveContractSmoke`, `BattleReportDensityContractSmoke`, `UiSmoke`, `SemanticPresentationContractSmoke`, `VisualHierarchyContractSmoke`, `RealmThemeContractSmoke`, `WindowPortraitContractSmoke`, and `GameplayContractSmoke`. `git diff --check` is clean for the owned surfaces.
- 2026-08-30: Godot code review found no report-local critical or improvement findings: component scenes retain one responsibility, node references are cached in `_Ready`, repeated authored nodes are disconnected and `QueueFree`d, and no per-frame lookup/load or runtime-built replacement tree was introduced. Visual review also confirms the old expanded-card wall is absent from the current statistical captures.
- 2026-08-30: Final frozen-surface audit confirms BattleSimulation `B38BE641C2988E1C5B8BC4022D68FFD232223B1FFE8F83050EE5859BFE59A0B0`, BattleModels `7B83446F188C4817DF62EFAE8D6512ED9345FC11DB7DC7F311A7E74ED83EDA22`, and gameplay core `3762EB29BDB69271356FC5408953D1EF7317E5C5222C184EC5607BAF40B8D149` match the execution baselines. `RealmTheme.tres` changed concurrently from baseline `A5375C3C4295AB58D2F964067DC45362AA8E1ABA3EC38CEA1C272A1FED7DA7AB` to `FBAB260391FF26EAC834DE935A0DFDE355FA3552C7977FCA0F0F0A433CC4A6A4` under the separate global UI task; this task did not edit the Theme, and its theme contract plus fresh report captures pass against the current external state.
- 2026-08-30: The monolithic `VisualCapture` run was stopped after several minutes without new QA output while traversing externally changing full-game UI state. No user editor/game process was controlled. This does not block the report handoff because the dedicated report fixtures generated the required current dual-resolution/player-enemy/dimension evidence; a future whole-game capture remains an external integration check.
- 2026-08-30: Main-Agent independent verification accepted the implementation. It independently repeated the 0-warning/0-error build; Statistical, Dynamic, Responsive, Density, UiSmoke, Semantic, Hierarchy, RealmTheme, WindowPortrait, and Gameplay contracts; non-headless Responsive capture refresh; and 1600x900/1280x720 visual review of overview, offense, survival, healing/zero state, long-name containment, fixed headers/columns, common-max bars, detail scrolling, and continue. Protected simulation/gameplay hashes still match, and independent Godot code review reports no critical or improvement findings.
- 2026-08-30: Final documentation governance replaced the remaining retired card-wall wording in battle-report and RealmTheme manual QA with the current leaderboard-row/summary/detail contract. No code, scene, test, Theme, or unrelated document changed during this completion step.

## Current State And Resume Condition

Implementation and independent verification are complete. The work item is archived as accepted; no resume action remains. Any future RealmTheme or report behavior change requires a new discussion/task rather than reopening this completed execution record.

## Verification Handoff

Changed-surface groups:

- Authority/QA: the report clauses in `system-design/tower-autobattler-architecture.md` and `docs/testcases/alpha-manual-qa.md`.
- Production: `BattleReportModels`, `BattleReportScreen`, the authored statistical component scripts/scenes listed in Progress, and retirement of the obsolete report card/row/metric-only resources.
- Verification: statistical RED/GREEN, migrated dynamic/responsive/density/UI/semantic/hierarchy contracts, and report capture routes.

Independent verification entry:

1. Build with `dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal`.
2. Run headless scenes `StatisticalBattleReportContractSmoke`, `DynamicBattleReportContractSmoke`, `BattleReportResponsiveContractSmoke`, `BattleReportDensityContractSmoke`, and `UiSmoke`, then the semantic/hierarchy/theme/window/gameplay regressions recorded above.
3. Run `BattleReportDensityContractSmoke.tscn` and `BattleReportResponsiveContractSmoke.tscn` non-headless to refresh `.godot/qa/UI_*_BattleReportStatistical*.png`.
4. Inspect both supported resolutions for three-second overview readability, three leader categories, both common-scale team bars, roster state, fixed column alignment, six-row/common-max behavior, long-name truncation, selected detail, enemy switching, zero healing, local scrolling, and fixed continue.
5. Confirm the three protected gameplay/simulation hashes above still match. Treat the recorded RealmTheme hash as external shared-state drift and revalidate visually if it changes again.

Accepted result: the dedicated statistical-report evidence and independent verification satisfy this task. The previously stalled monolithic whole-game `VisualCapture` remains an optional integration responsibility of its owning task and is not a remaining requirement of this completed work item.
