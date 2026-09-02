# Battle Report Responsive Card Repair

Status: Completed
Confirmed: 2026-08-30

## Goal

Eliminate internal clipping and overlap in dynamic battle-report cards when real content combines long Chinese unit names, responsibility labels, awards, large values, and two-column roster allocation. The report must adapt its authored layout to the actual allocated width instead of visually truncating fixed-size subpanels or applying a raw transform scale.

## Confirmed Diagnosis

- Project-level scaling is already correct for the supported desktop baseline: 1600×900 viewport with `canvas_items` and `expand`; the defect is inside the report card layout.
- `BattleReportScreen` currently forces two roster columns at every supported width.
- `BattleReportUnitCard` accumulates rigid minimums: a 94px identity column, 88px portrait, 142px primary metric, a fixed two-column support-fact grid, fixed gaps, and content-driven name/role/award minimums.
- Godot Containers calculate minimum sizes before distributing width. `Control.Scale` is visual-only and does not reduce those layout minimums, so scaling the whole card would preserve the collision while making Chinese text and pixel portraits blurry.
- Previous visual fixtures used shorter content combinations and did not cover the real long-name/multi-badge pressure case shown by the user.

## Confirmed Solution

### Responsive size classes

- Base adaptation on the card or roster's actual allocated width, not a hard-coded window-resolution assumption.
- Keep the two-column roster when every card can fit a measured compact-safe width.
- Use a wide card composition when allocation comfortably fits the full horizontal hierarchy.
- Switch cards to a compact authored composition when allocation is below the wide threshold: reduce only approved portrait/minimum-size/gap/font tokens, allow headers and facts to reflow, and increase card height as required.
- If the roster becomes narrower than the compact-safe width, switch the outer roster to one column. The layout must prefer a taller scrollable card over clipped or overlapping facts.
- Breakpoint constants must be named, explained by measured combined-minimum sizes, and tested. Resizing across a breakpoint must be deterministic and reversible without losing the selected dimension/allegiance or creating duplicate cards.

### Card reflow rules

- Keep portrait, name, identity, alive/defeated state, responsibility, awards, primary metric, all four dimension facts, and contribution bar available; do not solve the bug by deleting required information.
- Long names must remain fully readable through authored wrapping/reflow or a compact header arrangement. Role and award content may move to their own row instead of competing with the name.
- Replace rigid support-fact packing with an authored wrap-capable or size-class-controlled layout. Support facts may change from two columns to one/two responsive rows but may not overlap a neighboring card or the primary metric.
- Reduce the compact portrait no lower than 64px and compact semantic text no lower than 14px at the supported resolutions. Semantic icons and Chinese labels remain paired.
- Primary metric and identity surfaces may use smaller compact minimum widths, but their content must remain centered/readable and their border must fully contain it.
- Card height is content-driven. The roster `ScrollContainer` owns vertical overflow; the fixed dimension/allegiance controls and continue action remain reachable.
- Do not apply `Scale` to the card or its text hierarchy and do not alter global `content_scale_factor`, project stretch mode, or base resolution.

## Architecture Impact

- UI Presentation owns actual-width observation, named size-class selection, outer grid-column selection, authored compact/wide structure, and focus-safe reflow.
- Update `system-design/tower-autobattler-architecture.md` before production edits with the actual-width responsive-card/grid contract and the prohibition on raw transform scaling for Container-managed report layout.
- Update `docs/testcases/alpha-manual-qa.md` before production edits with long-name, multi-award, large-value, breakpoint resize, and internal-overlap checks.
- Gameplay authority, immutable battle facts, simulation, routing, saves, rewards, and the semantic icon/portrait resource ownership do not change; no gameplay-design update is required.

## Scope

- Responsive layout behavior in `BattleReportScreen` and `BattleReportUnitCard` scripts/scenes.
- Report-only Theme/type variations and authored compact/wide tokens where needed.
- Focused responsive layout contracts and migration of visual fixtures to include a worst-case card-content stress state.
- Fresh paired 1280×720 and 1600×900 evidence for overview/output/survival and the reported long-name case, plus resize-across-breakpoint verification.
- Retirement of any newly obsolete report-only fixed-minimum layout property after every consumer/test is migrated.

## Non-Goals

- No combat-statistic, battle-result, balance, AI/navigation, save, reward, routing, portrait-source, semantic-icon, or command change.
- No global Theme redesign, pixel-fantasy Theme rollout, new/generated art, or modification of the separate pixel-fantasy asset package.
- No redesign of team summaries, outcome hierarchy, four analysis dimensions, allegiance switching, awards, environment reconciliation, or report data semantics.
- No bulk responsive cleanup of unrelated screens and no modification of the separate `web/` mechanics atlas.
- No raw card transform scaling, font smaller than the confirmed compact minimum, icon-only facts, or content deletion to force fit.

## Hard Constraints

- Work only on current `main`; create no branch, commit, push, or history rewrite.
- Preserve the dirty report implementation and every unrelated modified/untracked file. In particular, do not modify `web/`, `work-items/active/game-mechanics-design-atlas.md`, `assets/ui/pixel_fantasy/`, `content/ui/pixel_fantasy/`, the separate pixel-fantasy Theme/preview/test surfaces, or their activity task.
- Read this work item, project rules, archived dynamic-report task, relevant system/QA authority, and applicable Godot UI/responsive/testing skills before editing.
- Synchronize system and manual-QA authority before production code, scenes, Theme, or tests.
- Author layout in `.tscn` and Theme resources; code may select named size classes, grid columns, and bind data but must not construct replacement UI trees.
- Reuse `UnitPortrait`, `SemanticChip`, semantic Theme colors, and current dynamic report view models without duplicating ownership.
- Do not edit generated `.godot/imported` files or close/control the user editor. Run build/Godot/import work serially and pause if another session starts a build/Godot process or writes a protected report/shared-Theme surface.

## Acceptance Criteria

### Layout behavior

- At 1600×900 and 1280×720, the longest current Chinese unit name, longest responsibility label, positive award text, large metric values, four support facts, and contribution bar remain inside their own card and surface borders.
- No name, portrait/identity surface, primary-metric surface, support fact, award, contribution label/bar, or card border overlaps a sibling card or is clipped by its own layout.
- Wide and compact cards select from actual allocated width. The roster uses two columns while safe and one column below the measured compact-safe width.
- Repeated resize across both breakpoints is reversible and stable: selected dimension/allegiance and continue behavior remain unchanged, focus stays reachable, card count remains exact, and no orphan/duplicate nodes appear.
- Long cards expand vertically and remain reachable through the existing scroll owner; partial visibility of the next complete card row at a scroll boundary is acceptable, but internal clipping is not.
- Compact text remains at least 14px, portrait at least 64px, semantic icon-plus-Chinese-label meaning remains intact, and no raw transform scale is applied.

### Verification

- A focused pre-change contract reproduces the rigid-minimum/two-column failure with the reported pressure combination and fails only for the intended missing responsive behavior.
- Focused GREEN proves named width classes, wrap/reflow behavior, safe column switching, no transform scale, and retained dynamic report facts.
- Runtime UI verification measures every stress card's allocated rect and relevant descendant rects after layout settles, rejecting internal or sibling overflow rather than relying only on screenshots.
- Existing DynamicBattleReport, UI, SemanticPresentation, VisualHierarchy, WindowPortrait, Gameplay, and routing contracts remain green.
- Fresh 1280×720 and 1600×900 captures include the long-name/multi-award stress case and are manually reviewed. No protected external task surface or unrelated screen is changed.

## Progress

- 2026-08-30: User supplied a real report screenshot showing multiple internal card surfaces visually clipped/overlapping and requested adaptive scaling behavior.
- 2026-08-30: Read-only diagnosis confirmed the global 1600×900 `canvas_items + expand` scaling contract is correct; the defect comes from forced two-column roster allocation plus accumulated fixed card minimum widths and non-wrapping content.
- 2026-08-30: User confirmed an actual-width responsive-card repair: authored wide/compact reflow, safe outer one/two-column switching, bounded compact typography/portrait sizes, content-driven height, and no raw transform scale.
- 2026-08-30: Architecture preflight assigns the repair to UI Presentation only. Battle facts, simulation, routing, shared semantic/portrait ownership, pixel-fantasy production, and the web mechanics atlas remain outside scope.
- 2026-08-30: This self-contained activity document was created. System/QA authority, production code/scenes/Theme, tests, builds, imports, and captures have not yet changed for this repair.
- 2026-08-30: Execution started on `main` at `62f13f466fea5d3c28625ec4b62391edf7555f10` with the dirty dynamic-report implementation preserved. Protected `web/`, game-mechanics atlas, `assets/ui/pixel_fantasy/`, the absent `content/ui/pixel_fantasy/`, pixel-fantasy activity surfaces, and unrelated work remain outside scope. Only user editor PID 23260 is active; no external Godot/build/compiler process is running. Report scripts/scenes and shared `game_theme.tres` were fingerprinted before edits so later external writes can be detected.
- 2026-08-30: Execution architecture preflight passed. UI Presentation owns actual-width roster/card observation, named wide/compact size classes, safe one/two-column selection, authored reflow, and focus-stable resize. Immutable facts, typed report derivation, dimension/allegiance semantics, routing, saves, rewards, global stretch/content scale, shared semantic catalog, portrait definitions/frames, and all pixel-fantasy ownership remain unchanged.
- 2026-08-30: System architecture and manual-QA authority synchronization completed before production edits. The contracts now require measured actual-width size classes, bounded compact tokens, content-driven height, one-column fallback below compact-safe width, descendant/sibling rect checks, long-name/multi-award/large-value stress, reversible breakpoint resize, full fact retention, and no raw transform scaling.
- 2026-08-30: Focused RED source authored as `BattleReportResponsiveContractSmoke` without referencing not-yet-created production types at compile time. Its stress result combines long Chinese names, every positive award, large health/damage/healing/rate/action values, four support facts, and four player cards. It statically requires actual-width named size classes/reflow and dynamically checks 1600/1280/1000 widths, two/one-column selection, descendant/primary-border containment, sibling separation, compact portrait/text floors, full wrapping, no raw scale, exact card count, focus/state retention, and repeated breakpoint reversibility.
- 2026-08-30: RED execution is intentionally paused before any build/Godot launch because a separate protected pixel-fantasy session began writing its isolated test/resource surfaces. It has not touched report or shared `game_theme.tres`, and only user editor PID 23260 is active. Report/Theme timestamps and fingerprints remain at the execution baseline; continue read-only monitoring until the external write set is stable, then build and run the RED serially.
- 2026-08-30: After external write stability and explicit serial-build clearance, the first low-concurrency build was blocked before responsive RED execution by the protected `tests/PixelFantasyUiContractSmoke.cs`: line 92 calls nonexistent Godot 4.7 C# `Theme.HasType` APIs, producing two CS1061 errors and one related nullable warning. This is unrelated to the responsive contract and cannot establish this task's RED. No protected pixel file, report production surface, or shared Theme was changed; build servers were shut down and report/Theme fingerprints remained identical. Resume only after the owning pixel-fantasy session fixes its compile surface and the main Agent reauthorizes serial build/Godot work.
- 2026-08-30: User explicitly authorized this responsive implementation to continue without modifying or waiting on the protected pixel-fantasy test. The unrelated compiler failure will be recorded as an external verification gap; a full-project build is not an implementation prerequisite, and no runtime GREEN will be claimed unless the project becomes buildable without touching that protected surface.
- 2026-08-30: Production repair completed without changing the shared Theme or report-screen scene. `BattleReportScreen` now observes the roster's allocated width, keeps two columns only above the measured `2 × 520px + 8px` compact-safe minimum, and uses a centered 640px compact single-column fallback below it. Column changes only reflow existing cards and preserve dimension, allegiance, focus, card count, and continue state.
- 2026-08-30: `BattleReportUnitCard` now owns explicit `Wide`/`Compact` size classes selected from actual allocated width at a measured 680px breakpoint. The authored scene separates name, role, and award rows; enables full wrapping; changes metrics to a size-class-controlled `GridContainer`; changes support facts between two and one columns; keeps compact portraits at 72px and semantic text at 14px; and grows vertically instead of applying transform scale or deleting facts.
- 2026-08-30: The focused responsive contract is GREEN in both headless CI mode and rendered desktop mode. It proves 1600×900 = two-column Wide, 1280×720 = two-column Compact, 1000×720 = one-column Compact, exact four-card retention, selected offense/player/focus retention, repeated breakpoint reversibility, descendant/primary-border containment, sibling separation, full wrapping, and the no-scale/token floors. Rendered capture is skipped only under the headless display server so CI never waits on an unavailable `FramePostDraw` signal.
- 2026-08-30: The obsolete `UiSmoke` assumption that every report allocation must use two columns was migrated to the same actual-width contract. The initial failure was diagnostic-only: report visibility, both team summaries, and all four cards were bound; a 497px not-yet-settled allocation correctly selected one column. After migration, `UiSmoke` is GREEN with its pre-existing deliberate modal-focus warning.
- 2026-08-30: Final verification is GREEN: low-concurrency build reports `0 warnings / 0 errors`; `BATTLE_REPORT_RESPONSIVE_CONTRACT_OK`; `DYNAMIC_BATTLE_REPORT_CONTRACT_OK`; `UI_SMOKE_OK`; `SEMANTIC_PRESENTATION_CONTRACT_OK`; `VISUAL_HIERARCHY_CONTRACT_OK`; `WINDOW_PORTRAIT_CONTRACT_OK`; and `GAMEPLAY_CONTRACT_OK`. `git diff --check` is clean, no debug marker remains, and Godot code review found no critical or improvement item in the task-scoped repair.
- 2026-08-30: Fresh rendered stress evidence was generated and manually reviewed for overview/output/survival at both resolutions: `.godot/qa/UI_1280x720_BattleReportResponsiveStress{Overview,Offense,Survival}.png` and `.godot/qa/UI_1600x900_BattleReportResponsiveStress{Overview,Offense,Survival}.png`. Long Chinese names, three simultaneous awards, large values, primary surfaces, all four facts, and contribution bars remain internally contained; 1280 cards become taller and use the existing scroll owner as intended.
- 2026-08-30: Scope audit passed. Task changes are limited to report-responsive production surfaces, their authority/QA, focused contract/captures, and the obsolete responsive assertion in `UiSmoke`. Baseline hashes for protected shared `content/ui/game_theme.tres` and `scenes/ui/BattleReportScreen.tscn` remain unchanged. No pixel-fantasy, `web/`, mechanics-atlas, gameplay/statistics, routing, reward, save, or generated import surface was modified by this task; user editor PID 23260 was not controlled or closed.
- 2026-08-30: Independent verification passed. The verifier reviewed the scoped static diff, focused pressure-contract logic, clean `git diff --check`, and all six rendered evidence files; manual review of 1280×720 and 1600×900 overview plus 1280×720 offense/survival confirmed that inner metric surfaces, long names, awards, large values, facts, and contribution bars remain contained. The apparent 1280 bottom cutoff is only the confirmed `ScrollContainer` viewport boundary, not internal card clipping. No blocker or corrective change remains.

## Current State And Resume Condition

Implementation, executor verification, and independent verification are complete. The named size classes, 1600/1280/1000 column behavior, retained report state/facts, containment measurements, rendered evidence, and protected-scope audit all passed. No known task-scoped blocker, verification gap, or follow-up implementation remains; this work item is complete and archived.

## Verification Handoff

Verification is closed. Independent review covered the scoped production/test diff, focused responsive contract, six pressure captures, protected-surface hashes, and clean diff checks; all acceptance criteria passed. Future regressions should resume from the named commands and evidence paths recorded above, but no further action is required for this completed work item.
