# Dynamic Multi-Dimension Battle Report

Status: Confirmed — Ready For Execution  
Confirmed: 2026-08-30

## Goal

Replace the spreadsheet-like battle report with a readable, game-like analysis screen that answers three player questions: why the battle was won or lost, which units contributed most, and where the formation or unit mix underperformed. The report must support live switching among overview, offense, survival, and healing dimensions while retaining exact immutable battle facts and unit identity.

## Confirmed Solution

### Information architecture

- Keep one fixed outcome banner with victory/defeat/timeout, encounter name, deterministic duration, and player command-gold cost.
- Add a compact two-team comparison area for survivors, casualties/kills, effective damage, effective healing, and remaining-health state. Player-only command cost is metadata, never an enemy-comparable value.
- Add primary dimension controls: `战局总览`, `输出`, `生存`, and `治疗`.
- Retain explicit `我方` and `敌方` controls. The selected allegiance determines the detail roster; the fixed comparison context continues to show both teams.
- Use authored unit cards in a responsive two-column scrollable roster at both supported resolutions. Identity, portrait, name, role/hero/summon tags, and alive/defeated state stay fixed while the metric region and default ordering change with the selected dimension.
- Switching dimension or allegiance replaces the complete derived view deterministically, preserves reachable mouse/keyboard/gamepad focus, and may use only short interruptible presentation feedback.

### Dimension content

- `战局总览`: complete selected-side roster, final health state, compact core contributions, and explainable category awards.
- `输出`: effective damage dealt, team damage share, active-lifetime damage per second, kills, and a proportional contribution bar; default order is descending effective damage with stable-id tie-breaking.
- `生存`: effective damage taken, shield absorbed as a subset of that damage, final health/maximum health, final-health ratio, alive/defeated state, and active lifetime or defeat time; default order is descending effective damage taken with stable-id tie-breaking.
- `治疗`: effective healing, team healing share, active-lifetime healing per second, and effective healing-event count; default order is descending effective healing with stable-id tie-breaking. When the selected side has no healing contribution, render a deliberate Chinese empty state while keeping all required unit facts reachable through overview.
- Explainable awards are limited to positive facts such as highest damage, highest damage taken, and highest healing. Ties remain valid; an all-zero category grants no award. Do not introduce an opaque composite rating or MVP formula.
- Temporary summons remain independent report entries and receive an explicit summon identity tag. Their contribution is never silently merged into the summoner.

### Statistic authority and minimal extension

- Preserve current effective-stat definitions: damage counts only shield and health actually removed, excludes overkill, and healing counts only health actually restored, excluding overheal.
- Continue to leave floor/environment contributions unowned. The overview may derive and label non-negative environment damage from target-side effective damage taken minus opposing credited unit damage so totals remain understandable; it must not falsely attribute that value to a unit.
- Existing immutable unit facts remain authoritative: final health/maximum health, final shield, alive state, damage dealt, damage taken, shield absorbed, healing done, kills, hero/role/team/temporary identity, and final cell.
- Add the minimum immutable aggregate facts needed for fair dynamic analysis:
  - unit spawn/join tick;
  - first terminal defeat tick, when defeated;
  - attack-action count;
  - effective healing-event count;
  - successful hero-command use count on the battle result.
- Active lifetime is derived from join tick to defeat tick or result tick, clamped to at least one simulation tick. DPS/HPS use active lifetime so late summons are not divided by the whole encounter duration.
- Record each counter exactly once at its authority boundary. Splash/piercing targets do not create extra attack actions; one effective `HealLiving` result creates one effective healing event for its credited source; failed/zero-effective healing creates none; failed hero commands create no use count.

## Authority Impact

- Update `gameplay-design/tower-autobattler-core.md` before production implementation with the dynamic dimension contract, comparison semantics, minimal aggregate facts, active-lifetime rate definition, independent summon treatment, and no opaque rating.
- Update `system-design/tower-autobattler-architecture.md` before production implementation with immutable result ownership, aggregate-counter boundaries, authored dynamic report/card scenes, typed view-model derivation, and responsive scroll/focus ownership.
- Update the relevant manual QA under `docs/testcases/` with dimension/allegiance switching, ordering, zero state, tied awards, environment reconciliation, summon lifetime, focus, and paired-resolution checks.
- The active semantic-icons/animated-portraits work item retains shared icon, palette, Theme, and independent portrait ownership but no longer owns report information architecture or tabular layout.

## Scope

- Immutable battle-result and per-unit aggregate-stat extensions listed above.
- Deterministic derived report view models for team comparisons, contribution shares, active-lifetime rates, rankings, positive category awards, and environment damage.
- Authored report composition, dimension/allegiance controls, comparison components, responsive unit-card template, empty state, and shared Theme/type variations.
- Replacement or retirement of the obsolete spreadsheet-style row layout after all references and tests are migrated.
- Focused gameplay/UI contracts, relevant regression, and paired visual evidence for player/enemy and victory/defeat states at 1280x720 and 1600x900.

## Non-Goals

- No time-series graphs, retained per-tick combat ledger, skill/source/target damage breakdown, damage-type taxonomy, crowd-control score, buff uptime, arbitrary combat rating, balance change, AI/navigation change, reward/economy change, save-schema change, or new art generation.
- No merging of summon statistics into heroes and no attribution of environmental facts to combat units.
- No redesign of battle simulation timing, digest authority, post-battle routing, reward navigation, other screens, or battlefield presentation.
- No runtime construction of a replacement UI tree in C# and no concrete hero/unit-id presentation dispatch.

## Hard Constraints

- Work only on the current `main` branch; create no branch or commit.
- Preserve the all-untracked workspace and unrelated user files. Do not edit generated `.godot/imported` files or control/close a user-open Godot editor/game.
- Read this work item, the project rules, relevant gameplay/system authority, and the active semantic-icons/animated-portraits boundary before editing.
- Synchronize the authority documents and manual-QA intent before production code, scene, resource, or test changes.
- `BattleResult` and unit report entries remain immutable value snapshots without live state, Nodes, textures, or mutable shared resources.
- Author layout and reusable structures in `.tscn`/`.tres`/Theme resources. C# owns typed binding, derivation, ordering, and interaction only.
- Reuse the current semantic icon catalog, palette, and independent `UnitPortrait`; do not duplicate their ownership or introduce screen-local icon maps.
- Required actions and primary facts remain reachable at 1280x720 and 1600x900. Long rosters scroll inside one explicit owner while the outcome, dimension/allegiance controls, and continue action remain reachable.
- Run Godot/build/import work serially, use low-concurrency .NET builds, and shut down only idle build servers that this task created.

## Acceptance Criteria

### Battle facts

- Seeded gameplay contracts prove initial units join at tick zero, temporary summons retain their actual join tick, terminal defeat tick is written exactly once, attack actions count actions rather than affected targets, effective healing events exclude zero/overheal, and only successful commands increment command uses.
- Existing effective damage/healing, kill, shield, environment-unowned, terminal-death, immutable-snapshot, fixed-tick, and digest contracts remain green.
- Derived percentages are zero-safe and use the selected team's credited total. Active-lifetime DPS/HPS use the confirmed tick interval and never divide by zero.
- Environment damage is shown separately only when positive and reconciles without mutating or falsely crediting a unit.

### Dynamic UI

- The report visibly reads as a battle analysis screen rather than a fixed equal-column spreadsheet.
- `战局总览`, `输出`, `生存`, and `治疗` switch the selected roster's primary metric, supporting facts, contribution bar, and deterministic ordering without losing unit identity.
- `我方` and `敌方` switch the detail side independently of the dimension. Header comparison remains meaningful, and player-only command cost is not presented as an enemy stat.
- Output shows damage/share/DPS/kills; survival shows taken/shield/final health/lifetime; healing shows healing/share/HPS/effective events or the deliberate zero-contribution state.
- Positive tied leaders receive the appropriate category award; zero categories do not. Heroes, soldiers, enemies, bosses, living/defeated units, and temporary summons remain distinguishable with icon-plus-Chinese-text semantics.
- The fixed continue action, scrolling, mouse, keyboard, gamepad, focus, hidden-screen behavior, and exactly-once post-report routing remain intact.

### Responsive presentation and verification

- At 1600x900 and 1280x720, two-column cards have no clipped labels/values, overlap, inaccessible controls, hidden continue action, or ambiguous selected dimension/allegiance.
- Fresh visual evidence covers at least player/enemy overview, output, survival, healing or its zero state, ordinary victory, defeat/timeout where applicable, and a roster containing a temporary summon when deterministic fixtures permit.
- Low-concurrency build plus focused Gameplay, semantic/visual hierarchy, UI, route, and clean-startup checks pass serially with only documented expected diagnostics.
- Final scope review confirms no time-series ledger, opaque score, balance/save/navigation/reward/battlefield drift, local icon duplication, generated-import editing, donor dependency, or unrelated cleanup.

## Progress

- 2026-08-30: User rejected the fixed spreadsheet-like report and requested reference to established game result patterns.
- 2026-08-30: Read-only review compared the stable information patterns used by MOBA scoreboards, role-oriented team shooters, army battle summaries, autobattler roster results, and ranked damage meters. The user added and confirmed live switching across different analysis dimensions.
- 2026-08-30: User confirmed `dynamic four-dimension report + allegiance switching + ranked contribution bars + minimal statistic extension`.
- 2026-08-30: Architecture preflight assigns immutable aggregate facts to Combat result construction and all comparison/percentage/rate/ranking presentation to UI view-model derivation. Save state, simulation timing, routing, and other screens remain unchanged.
- 2026-08-30: Active semantic-presentation documentation was corrected so its former tabular report constraint no longer conflicts; shared semantics and portrait contracts remain dependencies.
- 2026-08-30: This self-contained activity document was created. Production authority, code, scenes, tests, builds, imports, and captures have not yet been changed for this task.
- 2026-08-30: Execution began on `main` as the sole active writer after the overlapping semantic-visual-hierarchy session reached `Awaiting Verification` and its report/Theme files stabilized. The accepted baseline is its shared 38-key semantic catalog, semantic Theme palette/type variations, structured component vocabulary, independent `UnitPortrait`, red report-damage correction, and green visual-hierarchy/semantic/UI evidence; this task will migrate only report-owned assertions and consumers.
- 2026-08-30: Execution architecture preflight passed. `BattleSimulation` remains the sole authority for join, attack-action, effective-heal, terminal-defeat, summon, and successful-command aggregates; immutable additions flow through `BattleUnitReportSnapshot`/`BattleResult`. UI Presentation owns zero-safe comparison, environment reconciliation, active-lifetime rates, rankings, tied positive awards, dimension/allegiance state, authored cards, scroll, and focus. Digest, fixed-step timing, run/save/routing, semantic-catalog ownership, portraits, hero library, and every non-report screen remain unchanged. User editor PID 23260 remains open and outside executor control.
- 2026-08-30: Gameplay, system, and manual-QA authority synchronization completed before tests or production implementation. The contracts now cover independent dimension/allegiance switching, team comparison semantics, minimal immutable aggregates and exact counter boundaries, active-lifetime rates, tied positive awards/no opaque score, environment reconciliation, authored two-column cards/scroll/focus, zero-healing state, summons, dual resolutions, and exact-once routing.
- 2026-08-30: Focused source/runtime-surface RED authored as `DynamicBattleReportContractSmoke` without referencing absent production types. A low-concurrency build completed with 0 warnings/0 errors, then the focused scene emitted `DYNAMIC_BATTLE_REPORT_CONTRACT_FAILED` only for the intended missing immutable aggregate fields/counter boundaries, typed four-dimension derivation, authored team-summary/card resources, dimension/allegiance controls, empty state, and obsolete row ownership. This establishes a clean RED before production implementation.

## Current State And Resume Condition

Execution is in progress after a clean focused RED. Next implement the minimal immutable aggregates, typed derived report view models, authored team-summary/unit-card composition, dynamic controls, and necessary test/capture migrations. Stop and return to discussion if correct implementation requires a per-tick event ledger, skill/damage taxonomy, save migration, balance change, concrete-content dispatch, semantic-catalog duplication, or a change to another active task's non-report ownership.

## Verification Handoff

Execution evidence now includes `dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal` at 0 warnings/0 errors and the expected focused RED from `tests/DynamicBattleReportContractSmoke.tscn`. Authority changed only in report-owned sections. No production implementation, regression suite, import, or capture has run for this task yet. The next required handoff checkpoint is implementation GREEN and focused gameplay/UI evidence. Set status to `Awaiting Independent Verification` only after implementation and executor-side verification are complete; do not archive this task.
