# Battle Report Core Matchups

Status: Completed  
Confirmed: 2026-08-30

## Goal

Replace the overview's low-value team-total bars and separate selected-side leader cards with a compact, statistics-first core-unit matchup. The overview should explain which unit carried output, absorbed pressure, and supplied healing for each side, using only existing authoritative battle facts and derived team contribution shares.

## Confirmed Diagnosis

- The current `BattleReportComparison` repeats remaining health, effective damage, and effective healing totals already visible in the fixed team summaries.
- Those totals are weak diagnostic measures: total damage is constrained by opposing health and battle length, while healing is often structurally zero. Side-by-side total bars therefore consume space without explaining why the result occurred.
- The comparison also repeats survivors, casualties, and kills already shown in the two fixed team summaries.
- Three separate leader cards identify only the currently selected side and repeat information that can be presented more meaningfully as direct player-versus-enemy matchups.

## Confirmed Solution

- The default overview keeps the result header, both fixed team summaries, roster strips, page/allegiance controls, positive environment-damage attribution, and exact-once continue routing.
- Remove the old remaining-health/effective-damage/effective-healing team-total bars and the duplicated survivor/casualty/kill sentence from the overview body.
- Replace the old comparison bars plus three leader cards with exactly three compact authored matchup rows:
  - `输出核心`: each side's positive effective-damage leader, authoritative value, and that unit's share of its own team's effective damage.
  - `承伤核心`: each side's positive effective-damage-taken leader, authoritative value, and that unit's share of its own team's total effective damage taken.
  - `治疗核心`: each side's positive effective-healing leader, authoritative value, and that unit's share of its own team's effective healing.
- Each row keeps player identity/value/share on the left, the category in the center, and enemy identity/value/share on the right. Tied positive leaders remain visible and deterministic; use concise joined names and an `各` share label where necessary.
- If both sides have zero healing, the healing row collapses to a compact `双方均无有效治疗` state rather than showing empty bars or inventing a leader. If only one side is zero, that side reads `无有效治疗` while the positive side remains visible.
- The overview is a fixed two-team comparison. The allegiance selection continues to determine which side opens in the detailed `输出`/`生存`/`治疗` rankings; the detailed rankings, row selection, single detail panel, and common-maximum contribution bars remain unchanged.

## Architecture Judgment And Authority Impact

- Ownership remains entirely in UI Presentation. Immutable `BattleResult` snapshots and combat-stat collection are sufficient; no battle simulation, save, routing, formation, movement, or gameplay rule changes are required.
- Update the report clause in `system-design/tower-autobattler-architecture.md` before production edits so the overview authority becomes three core-unit matchup rows rather than team-total scales plus separate leader summaries.
- Update the report cases in `docs/testcases/alpha-manual-qa.md` before production edits so three-second readability validates core identity/value/share matchups and compact zero-healing handling.
- Reuse existing `BattleReportUnitViewModel` shares and positive/tied leader derivation. Presentation-only pairing helpers may be added if they do not create a new scoring or gameplay statistic.

## Scope

- `BattleReportScreen` overview composition and binding.
- An authored reusable core-matchup row scene/component and the existing authored overview comparison container, or an equivalently focused authored scene arrangement.
- Removal of `BattleReportLeaderSummary` production resources only after every valid consumer and contract has migrated.
- Focused statistical, responsive, density, UI, hierarchy, and visual-capture contract updates needed to express the new authority.
- Fresh overview evidence at `1600x900` and `1280x720` when the environment permits safe capture.

## Non-Goals

- No composite MVP, rating, efficiency score, inferred tactical advice, timeline, skill breakdown, damage taxonomy, or unavailable statistic.
- No change to detailed leaderboard columns, ordering, common-maximum bars, unit detail contents, team switching semantics for detailed pages, or healing-page empty-state behavior.
- No changes to battle simulation, result collection, movement, formation/deployment, rewards, navigation, persistence, portraits, semantic catalog, RealmTheme, other screens, `web/`, or unrelated active work.
- No whole UI tree construction in C# and no raw `Control.Scale` workaround.

## Hard Constraints

- Work on current `main`; create no branch, commit, push, reset, clean, or history rewrite.
- Preserve the dirty worktree and concurrent unrelated changes. Only report-owned files and the two narrow authority/QA clauses may change.
- Repeated visual structure is authored in `.tscn`; C# only derives, pairs, binds, sorts, and handles interaction.
- Continue to inherit `res://content/ui/RealmTheme.tres`; do not edit the Theme for this task.
- Keep content within the existing report `ScrollContainer`. Header, controls, and continue stay fixed and reachable at both supported resolutions.
- Long Chinese names and large values must truncate or wrap inside their authored columns. Do not transform-scale controls or text.
- Avoid unnecessary Godot/editor launches and parallel build/import work. Use low-concurrency build only when safe.

## Acceptance Criteria

- The overview contains no remaining-health/effective-damage/effective-healing team-total comparison bars and no separate three-card leader row.
- Exactly three authored compact core matchup rows compare player and enemy leaders for output, damage taken, and healing.
- Every positive side displays leader identity, authoritative absolute value, and zero-safe team contribution percentage. Ties are retained deterministically and are not reduced to one arbitrary unit.
- Both-zero healing is one compact explicit Chinese state; one-sided zero healing remains understandable without allocating a fake leader.
- The old duplicated survivors/casualties/kills comparison sentence is absent; fixed team summaries remain the single owner of those totals.
- Positive environment damage remains labelled as a team-level fact without unit attribution.
- At `1600x900` and `1280x720`, all matchup identities, categories, values, percentages, roster strips, controls, and continue remain contained and readable; overview information moves upward rather than leaving a large blank replacement region.
- Output/survival/healing rankings, team switching, stable row selection, unit detail, zero-healing ranked-page state, and exactly-once continue routing still pass their focused contracts.
- No fabricated statistics or protected gameplay/simulation changes are introduced.

## Progress

- 2026-08-30: User identified the team-total comparison bars as low-value and confirmed replacing them with direct player-versus-enemy core-unit matchups containing identity, authoritative value, and team contribution share.
- 2026-08-30: Architecture preflight assigns the change to UI Presentation. Existing immutable unit facts, positive/tied leader derivation, and damage/healing/damage-taken shares already cover the required data; no combat ledger or gameplay authority change is needed.
- 2026-08-30: Activity task created on `main`. The dirty worktree from the accepted statistical-report work and unrelated active tasks is preserved. No production, authority, QA, test, build, or capture edit for this task has started.
- 2026-08-30: Execution began on `main` after rereading project rules, the confirmed task, report architecture authority, report manual-QA cases, the archived statistical-report boundary, and the required Godot UI/responsive/C#/testing/review skills. Architecture preflight keeps ownership in UI Presentation: immutable result collection, detailed rankings and selection/detail behavior, routing/save, RealmTheme, semantic catalog, portraits, combat, formation, movement, other screens, and `web/` remain frozen. The existing dirty worktree is preserved; no branch, commit, cleanup, reset, or unrelated edit was made.
- 2026-08-30: Before tests or production edits, the narrow architecture and manual-QA clauses were updated to make exactly three authored player-versus-enemy core-unit matchup rows authoritative. Fixed team summaries now solely own team totals; tied identity/value/share, explicit both-zero/one-sided-zero healing, positive environment attribution, detailed ranking semantics, responsive containment, and exactly-once routing remain explicit.
- 2026-08-30: Added and proved focused `StatisticalBattleReportContractSmoke` RED after a low-concurrency build completed with 0 warnings/0 errors. The unmodified production report exits non-zero only for the intended authority gaps: missing fixed two-team matchup derivation and authored row, retained team-total bars/duplicated outcome sentence/separate leader cards, and missing identity/value/share/zero-state fields. Existing detailed ranking and exactly-once coverage remains in the same contract.
- 2026-08-30: Production migration completed within report ownership. The overview comparison now instances exactly three authored `BattleReportCoreMatchupRow` components for output, damage taken, and healing; each pairs player/enemy positive and tied leaders with authoritative absolute values and zero-safe own-team shares. The old team-total bars, duplicated outcome-fact sentence, and separate `BattleReportLeaderSummary` production resources were removed. Both-zero healing collapses to `治疗核心 · 双方均无有效治疗`; one-sided zero remains explicit, and positive environment damage remains team-labelled without unit attribution.
- 2026-08-30: Focused GREEN passes after a 0-warning/0-error low-concurrency build. `StatisticalBattleReportContractSmoke` covers fixed ordering, both identities/value/share, deterministic positive ties, both-zero healing, one-sided-zero healing, detailed leaderboard selection/detail, common-maximum bars, and exactly-once continue. Dynamic, responsive, density, and UI contracts also pass; density still measures six aligned 62px rows and a 100% common-maximum leader bar at both supported resolutions. The existing `UiSmoke.cs:95` Army-drawer focus warning is outside report ownership.
- 2026-08-30: Final focused regression set passes: Statistical, Dynamic, Responsive, Density, UiSmoke, Semantic Presentation, Visual Hierarchy, RealmTheme, Window/Portrait, and Gameplay contracts. Final low-concurrency build is 0 warnings/0 errors; `git diff --check` is clean. Protected baselines remain unchanged: BattleSimulation `B38BE641C2988E1C5B8BC4022D68FFD232223B1FFE8F83050EE5859BFE59A0B0`, BattleModels `7B83446F188C4817DF62EFAE8D6512ED9345FC11DB7DC7F311A7E74ED83EDA22`, and gameplay core `3762EB29BDB69271356FC5408953D1EF7317E5C5222C184EC5607BAF40B8D149`. RealmTheme remains the inherited external hash `FBAB260391FF26EAC834DE935A0DFDE355FA3552C7977FCA0F0F0A433CC4A6A4` and was not edited by this task.
- 2026-08-30: Fresh non-headless dedicated captures were generated for ordinary player/enemy overview and all detailed dimensions, long-name/large-value stress pages, positive environment damage, tied positive leaders, and both-zero healing at 1600x900 and 1280x720. Manual review confirms the matchup identities, center categories, absolute values, percentages, joined ties, explicit zero states, roster strips, controls, scroll ownership, and fixed continue remain contained and readable. The overview moves the rosters upward and leaves no empty replacement block where the retired comparison/cards lived.
- 2026-08-30: Godot code review found no report-local critical or improvement findings. The new component scenes each retain one presentation responsibility; node references are cached in `_Ready`; no per-frame lookup/load, runtime-built control tree, raw scale, Theme mutation, signal lifecycle change, or unrelated refactor was introduced. MSBuild/Roslyn servers were shut down after verification.
- 2026-08-30: Main-Agent independent verification accepted the change. It repeated a 0-warning/0-error low-concurrency build; Statistical, Responsive, UiSmoke, Dynamic, and Density contracts; direct visual review of 1600x900 and 1280x720 ordinary, stress, tied, and both-zero-healing overview captures; `git diff --check`; and protected-file hash checks. The new matchup rows remain contained and the detailed ranking/selection/detail/continue behavior is unchanged. The only runtime warning remains the separately owned Army drawer focus warning at `UiSmoke.cs:95`.

## Current State And Resume Condition

Implementation and independent verification are complete. The task is archived as accepted; no resume action remains. Any future battle-report information-architecture change requires a new discussion and activity task.

## Verification Handoff

Accepted evidence: the focused statistical, dynamic, responsive, density, and UI contracts pass independently; executor regression evidence also covers semantic, hierarchy, RealmTheme, portrait/window, and gameplay contracts. Dedicated overview captures at both supported resolutions cover ordinary, long-name/large-value, environment-damage, tied-positive, and both-zero-healing states. Build, whitespace, scope, and protected-hash checks pass.
