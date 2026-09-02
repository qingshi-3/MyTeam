# Battle Report Desktop Density

Status: Completed
Confirmed: 2026-08-30

## Goal

Raise the battle report's single-screen information density without reversing the completed clipping repair. At the 1600×900 desktop baseline, an ordinary six-unit side should show all three two-column rows completely. At 1280×720, an ordinary side should show at least two complete rows. Long names, multiple awards, large values, environment damage, and larger rosters may still increase authored content height and use the existing roster scroll owner.

## Confirmed Diagnosis

- The report is now containment-safe, but its vertical budget remains intentionally conservative after the responsive repair.
- Both the screen and `BattleReportTeamSummary` impose a 122px comparison minimum even when the facts occupy much less height.
- Wide cards impose a 174px minimum, while the card metrics also claim vertical expansion. Short ordinary content therefore creates empty space between the metrics and contribution row.
- Identity and alive/defeated labels remain stacked below the portrait, making portrait height plus two labels a card-height floor even though the content column has spare horizontal width.
- The standalone roster title repeats the selected team and dimension already expressed by the control row and consumes another vertical row.
- Shrinking the whole report, deleting facts, or lowering typography is unnecessary and would weaken the completed readability/containment contract.

## Confirmed Solution

### Dense desktop hierarchy

- Keep the outcome banner, team comparison, dimension/allegiance controls, roster scroll, and fixed continue action as the same authored regions and preserve their typed behavior.
- Make each team summary content-driven and approximately 70–80px for ordinary facts instead of enforcing 122px. Keep title, remaining-health value/bar, survivors, casualties, kills, damage, healing, and optional environment damage. Its flow may grow when the optional fact needs another line.
- Move the roster context label into the existing control row and shorten it to a concise team/dimension context. The pressed controls remain the primary state indicator; no separate title row remains.
- Preserve the current one/two-column roster selection and actual-width card size classes.

### Dense report cards

- In the authored card scene, place identity and alive/defeated state beside the unit name/responsibility rather than under the portrait. All identity, responsibility, award, state, portrait, primary metric, four support facts, and contribution information remain visible.
- Use a 72–80px portrait for desktop dense cards, never below the already accepted 64px floor.
- Remove the wide card's artificial fixed height and remove vertical expansion from its metrics region. Card height comes from current content.
- Keep the primary metric horizontally paired with a wrap-capable support-fact flow. Ordinary short facts should consume spare width; long values wrap into additional fact rows without overlap or deletion.
- Keep contribution immediately after metrics instead of pinning it to the bottom of unused card space.
- Preserve the compact typography floor of 14px, semantic icon-plus-Chinese-text meaning, no raw `Control.Scale`, and content-driven growth for pressure content.

## Architecture Impact

- UI Presentation continues to own report composition, actual-width size classes, content-driven minimum sizes, fact flow, and roster scroll ownership.
- Add a density clause to `system-design/tower-autobattler-architecture.md`: ordinary wide content uses available horizontal space before adding vertical height, while pressure content grows and scrolls.
- Add manual QA to `docs/testcases/alpha-manual-qa.md` for ordinary six-unit 1600×900 three-row visibility, ordinary 1280×720 two-row visibility, absence of spacer-like internal gaps, and retained worst-case containment.
- Gameplay facts, view-model derivation, ranking, awards, simulation, routing, saves, rewards, semantic catalog, portrait animation, global scaling, and RealmTheme authority do not change.

## Scope

- `BattleReportScreen` authored composition and concise roster-context binding.
- `BattleReportTeamSummary` authored minimum sizing and fact flow.
- `BattleReportUnitCard` authored hierarchy and size-class tokens needed for density.
- Focused density/containment contract updates, ordinary six-unit fixture, existing stress fixture preservation, and fresh 1600×900/1280×720 evidence.
- Small synchronized updates to report architecture and manual QA after merging any concurrent shared-document edits.

## Non-Goals

- No new statistics, charts, dimensions, awards, sorting, filters, gameplay facts, or result derivation.
- No RealmTheme redesign, pixel UI restoration, new art, portrait resampling, semantic icon changes, or global typography change.
- No removal of required report content and no guarantee that extreme long-name/multi-award or more-than-six-unit reports avoid scrolling.
- No changes to battle/deployment projection, formation logic, movement presentation, `web/`, or unrelated active work items.

## Hard Constraints

- Work on current `main`; create no branch, commit, push, or history rewrite.
- Preserve the dirty worktree and all unrelated changes. Do not modify the active flexible battlefield task, mechanics atlas, semantic portrait task, `web/`, or their isolated production/test surfaces.
- The currently active battlefield-layout session may write shared architecture/QA/tests and run a build. Fingerprint overlapping files, wait for stable writes, and never run Godot or `dotnet build` concurrently with it.
- Update shared architecture and manual QA before report production edits, but merge only the narrow report-density clauses into the latest file versions.
- Author hierarchy in `.tscn`; C# may bind values, select existing authored size classes, and update the concise context label, but may not build a replacement tree.
- Do not alter `res://content/ui/RealmTheme.tres` unless execution proves a report-only stable type variation is strictly required; return to discussion before any broader Theme change.
- Reuse the existing roster scroll, card scene, team summary scene, `UnitPortrait`, `SemanticChip`, semantic catalog, and report view models.

## Acceptance Criteria

### Density

- At 1600×900, a representative ordinary six-unit player side with realistic short/medium Chinese names, normal-size values, mixed alive/defeated states, and zero/one award per unit displays all six cards as three complete two-column rows without roster scrolling being required to see the sixth card.
- At 1280×720, the same ordinary fixture displays at least four cards as two complete two-column rows while banner, comparison, controls, and continue remain fully reachable.
- Ordinary two-team summaries are content-driven and no taller than approximately 82px per side; optional environment damage may grow the shared row only when it wraps.
- The standalone roster-title row is gone; concise team/dimension context remains readable in the control row.
- Ordinary cards have no artificial vertical spacer between metrics and contribution. Metrics and contribution remain adjacent according to authored separation tokens.

### Safety and behavior

- The completed responsive stress fixture remains containment-safe at 1600×900, 1280×720, and the one-column breakpoint. Long names, three awards, large values, all four facts, and contribution remain inside their card and may increase height/scroll extent.
- Portraits remain at least 64px, semantic text at least 14px, and no report/card text or root uses transform scale.
- Dimension, allegiance, focus, exact card count, ordering, empty healing state, scroll ownership, and exact-once continue routing remain unchanged through binding and repeated resize.
- Every team-summary and card fact remains present; density is achieved by layout and minimum-size corrections, not deletion.

### Verification

- Write a focused failing density contract before production edits. It must measure ordinary visible row/card rectangles and the gap between metrics and contribution rather than compare brittle pixels.
- Preserve or extend the responsive containment stress contract and existing dynamic report, RealmTheme, semantic, hierarchy, portrait/window, UI, gameplay, and routing regressions.
- Generate paired ordinary-density and pressure captures at 1600×900 and 1280×720. Manually inspect complete-row visibility, long-content growth, fixed controls, and absence of internal clipping.
- Run low-concurrency build/Godot work only after the external build/write set is idle. If unrelated work blocks verification, record the exact external gap without modifying it.

## Progress

- 2026-08-30: User supplied a 1600×900 live report showing only two complete card rows and substantial unused vertical space inside team summaries and cards.
- 2026-08-30: Read-only diagnosis identified duplicated 122px team-summary minimums, the wide 174px card floor, metrics vertical expansion, portrait-column identity stacking, and a redundant standalone roster-title row.
- 2026-08-30: User confirmed a desktop dense layout: content-driven summaries, merged roster context, dense authored card header, horizontal wrap-capable metrics, immediate contribution placement, unchanged compact safety floors, and ordinary six-unit three-row visibility at 1600×900.
- 2026-08-30: Architecture preflight assigns the work to UI Presentation only. RealmTheme visuals, immutable report facts, simulation, routing, semantic/portrait ownership, battlefield projection, and formation work remain outside scope.
- 2026-08-30: This self-contained activity task was created. No authority, production, Theme, test, build, import, or capture change has yet been made for this density task.
- 2026-08-30: Execution started on `main` at `62f13f466fea5d3c28625ec4b62391edf7555f10` with the dirty worktree preserved. Architecture/QA and report production surfaces remained stable through the read-only observation window. PID 8508 is an idle Roslyn `VBCSCompiler` service rather than an active build and will not be controlled; the user Godot editor PID 23260 and an unrelated editor for another workspace also remain untouched.
- 2026-08-30: Baseline fingerprints were recorded before density edits: architecture `75A513F1E43391E7D0013CEF4AA3546CDD331F302A210583E195334DD6321D05`; manual QA `D7E861203C00CDCCFA1329A3856A25466693D82F3A1DA8D87B3B2B59661B5F52`; RealmTheme `A5375C3C4295AB58D2F964067DC45362AA8E1ABA3EC38CEA1C272A1FED7DA7AB`; report screen scene `87AF1ED3A1C9CBF7A16CF4E217C139BDE756B3E5EE4294B3D06B1C04354FA37D`; summary scene/script `02DBD474A930F60841CD4BB612C0B3E3597121B5A6861B1183B3BE6798E60F5E` / `1949CDA76DAA3D9F547B7731B6777D2214DA28CDEF558975B2316D4890531E2B`; card scene/script `6E8FA5F7239BD4ECE9ECDD02D04C6E1E61D7BC0A1CC2550F670AA1068F0613E5` / `2C5CA71FC982A5315FE254D8D40CF0D3AC7916121E2408EA7CC47C76568EDC4F`; report screen script `7A838A57A00C5B546A4E0D81C349EAB05137E055351F091176B92360E12BB1BA`; responsive contract `3CD2A670BB5251CF3DA0EDDF639D6C8F162279AE89495633C0C6824FC986811C`.
- 2026-08-30: The concurrent flexible battlefield task owns battle/deployment, formation, projection, movement, its active task, and overlapping shared regressions. It recently updated `UiSmoke`, `GameplayContractSmoke`, `MovementPresentationContractSmoke`, and `VisualCapture`; those files are protected until their latest versions remain stable. Density execution will narrow-merge only report clauses into the latest architecture/QA and will not modify RealmTheme, battle/deployment production, semantic portraits, `web/`, or any unrelated activity task.
- 2026-08-30: Stable latest architecture/manual-QA were narrow-merged before production work. Architecture now states that ordinary wide report content consumes horizontal space before vertical height while pressure content still grows into the existing scroll owner; manual QA now covers ordinary six-unit three-row/two-row visibility, content-driven summaries, merged context, header identity/state, adjacent metrics/contribution, and retained pressure containment. No battlefield clause was changed.
- 2026-08-30: Focused `BattleReportDensityContractSmoke` RED is compiled and proven without a test-harness exception. At 1600×900 the current report exposes only four complete ordinary cards instead of six; at 1280×720 only two instead of four. Both summaries remain 122px; the standalone title remains; every wide card retains a 174px artificial minimum and 88px portrait; metrics still claims vertical expansion; identity/state remain below the portrait; and no wrap-capable support-fact flow or control-row context exists. The failing contract measures actual scroll/card rectangles and metrics-to-contribution adjacency rather than pixels.
- 2026-08-30: Implemented the confirmed dense desktop hierarchy in the authored report resources. Team summaries are content-driven; `RosterTitle` was removed and `RosterContext` now reports the selected allegiance/dimension inside the fixed control row; identity and alive/defeated state moved into the card header; the wide fixed card-height floor and metrics vertical expansion were removed; the wide portrait is 80px; support facts use an authored `HFlowContainer`; contribution follows metrics directly. Existing 680px card sizing and 520px compact-safe grid breakpoints remain unchanged, and no gameplay fact or selection behavior changed.
- 2026-08-30: Focused GREEN measurements are exact runtime rectangles, not screenshot estimates. At 1600×900 the roster viewport is 560.0px, both summaries are 82.0px, all 6/6 cards are complete, and ordinary card heights are `[187.0,187.0,187.0,187.0,162.0,162.0]` px. At 1280×720 the viewport is 380.0px, both summaries are 82.0px, 4/6 cards are complete as two full rows, and card heights are `[184.0,184.0,184.0,184.0,159.0,159.0]` px. Relative to RED, summaries changed from the enforced 122px floor to the measured 82px content height, the enforced 174px wide-card floor is gone, and the ordinary visibility target changed from 4→6 complete cards at 1600 and 2→4 at 1280. Content-bearing award cards may be taller than the old floor; short cards now shrink with their content instead of inheriting spacer height.
- 2026-08-30: Verification is GREEN. Low-concurrency `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` completed with 0 warnings and 0 errors. Focused marker: `BATTLE_REPORT_DENSITY_CONTRACT_OK ordinary=1600-three-rows,1280-two-rows summary=content-driven context=controls cards=no-spacer fact-flow=wrap`. Retained markers: `BATTLE_REPORT_RESPONSIVE_CONTRACT_OK`, `REALM_THEME_CONTRACT_OK`, `DYNAMIC_BATTLE_REPORT_CONTRACT_OK`, `SEMANTIC_PRESENTATION_CONTRACT_OK`, `VISUAL_HIERARCHY_CONTRACT_OK`, `WINDOW_PORTRAIT_CONTRACT_OK`, `UI_SMOKE_OK`, and `GAMEPLAY_CONTRACT_OK`. `UiSmoke` retains its known intentional focus warning and exits zero.
- 2026-08-30: Fresh ordinary captures are `.godot/qa/UI_1600x900_BattleReportOrdinaryDensityOverview.png` and `.godot/qa/UI_1280x720_BattleReportOrdinaryDensityOverview.png`. Fresh pressure captures are `.godot/qa/UI_1600x900_BattleReportResponsiveStress{Overview,Offense,Survival}.png` and `.godot/qa/UI_1280x720_BattleReportResponsiveStress{Overview,Offense,Survival}.png`. Manual inspection confirmed the required complete rows, contained long names/multiple awards/large values, readable fixed controls, reachable continue action, and no spacer-like card band or internal clipping across the ordinary captures and all pressure dimensions at both target sizes.
- 2026-08-30: Final scope audit passed. `git diff --check` is clean. `content/ui/RealmTheme.tres` remains byte-identical to the protected baseline at SHA-256 `A5375C3C4295AB58D2F964067DC45362AA8E1ABA3EC38CEA1C272A1FED7DA7AB`. This task changed only `BattleReportScreen`, `BattleReportTeamSummary`, `BattleReportUnitCard`, the focused density contract, and the narrow report clauses in architecture/manual QA. Existing dirty battle/deployment, formation, movement, portraits, shared regressions, other work items, and `web/` changes belong to their recorded owners and were not modified by this execution.
- 2026-08-30: Independent verification passed. The verifier checked the final authored hierarchy and bindings, focused exact measurements, clean `git diff --check`, and protected RealmTheme hash; manually inspected ordinary 1600×900/1280×720, pressure overview at both sizes, 1280 offense, and 1600 survival; and confirmed 1600 6/6 cards in three complete rows, 1280 4/6 cards in two complete rows, 82px summaries, merged control-row context, adjacent metrics/contribution without a spacer band, pressure containment without internal clipping, and compliant scope.

## Current State And Resume Condition

Implementation, executor verification, and independent verification are complete. The confirmed desktop-density result is accepted and no work remains on this task.

## Verification Handoff

Independent verification passed with the evidence recorded above. This task is complete and archived; future regressions should resume from the focused density contract and paired capture set rather than reopening the accepted layout direction.
