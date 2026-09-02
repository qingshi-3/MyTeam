# Combat Build And Population Framework Discussion

Status: Documentation Confirmed — Implementation Deferred

## Goal

Record one self-contained long-term player-facing framework that connects hero-role hierarchy, build grammar, population progression, population-based builds, and the `10 / 18 / 30` landmarks without starting system design or implementation.

The resulting authority must prevent two design failures: treating every hero as a supposed core, and treating population as free bodies plus unrestricted team-wide scaling.

## Confirmed Rules

- The product combines readable autobattler population/trait composition, `The Last Flame`-like ability/status/equipment/relic build grammar, and tower-run decisions. References inspire qualities and are never implementation authority.
- A run grows current population toward an ordinary endgame ceiling of `10`; it does not start at `10` by contract.
- Every persistent hero consumes exactly `1` population. Tier/rarity changes availability stage, role/complexity, and build responsibility, never population cost.
- The eighteen legal player candidate cells are the physical ceiling. Explicit build sources may raise persistent population above `10` and may high-roll toward all `18` cells.
- Temporary units require explicit authored sources, occupy actual free cells, consume no persistent roster/reserve population, and cannot break occupancy or defeat rules.
- Formations contain readable cores/payoffs, engines/enablers, and functional/bridge heroes. Every recruitable hero still needs legible acquisition-stage, immediate-use, replacement, and build value.
- A formed archetype contains driver/engine, state/resource, payoff, survival, and spatial condition.
- Population is a cross-archetype army/force chassis. Expansion, headcount-to-power scaling, and payoff/settlement are separate ordinary responsibilities; only a deliberate rare capstone may compress all three.
- Every population effect names whether it counts starting deployed roster heroes, current living friendlies, temporary units, or trait value. Unbounded all-team offense/health-per-head scaling is excluded.
- `10` is the ordinary endgame formation ceiling; `18` is the full-board population-build goal; `30` is a possible hidden single-trait-value achievement rather than a physical body target or routine breakpoint.
- Above-`10` growth consumes real hero, ability, equipment, or relic opportunity. Dense-combat readability and performance remain future acceptance risks.

## Authority Impact

- Create `gameplay-design/combat-build-framework.md` as the focused authority for population, hero role hierarchy, build grammar, and landmarks.
- Route the module from `gameplay-design/README.md`.
- Update `gameplay-design/tower-autobattler-core.md` only enough to remove obsolete `5 deployed + 3 reserve` authority, state the `10 / 18 / current population` relationship, and route detailed build rules to the focused module.
- Update `work-items/active/hero-roster-independent-tactics-redesign.md` so its deferred goal, phases, constraints, acceptance, progress, and handoff no longer claim `5+3`.
- Do not modify `AGENTS.md`, `system-design/`, implementation files, resources, persistence, tests, QA, or the separately owned content-platform task.

## Scope

- Long-term player-facing population and build grammar.
- Relationship between persistent hero population, legal cells, temporary units, and trait value.
- Hero content responsibility hierarchy and anti-junk-offer requirement.
- Documentation routing and removal of contradictory current capacity authority.
- A future implementation handoff that remains explicitly deferred.

## Non-Goals

- No code, resources, save schema, system ownership, UI, balancing tables, tests, or migration.
- No exact starting population, growth curve, tier taxonomy, tier odds, reserve capacity, or recruitment economy.
- No detailed frost, burn, shock, death, barrier, reaction, resistance, or status-stack design.
- No normal trait breakpoint table, final achievement name/reward, or confirmed secret transformation.
- No claim that every build must use population, temporary units, or the `18`-cell ceiling.
- No thirty-body battle target and no routine balance breakpoint at trait value `30`.

## Unresolved Numbers And Rules

- starting population and per-floor/per-region growth cadence;
- hero tier names, unlock stages, offer odds, and role-distribution rules;
- reserve capacity and reserve/deployment exchange behavior;
- recruitment price, replacement value, and how expansion opportunities enter reward pools;
- ordinary population/trait breakpoints below the `18` physical ceiling;
- how many above-`10` sources exist and how much opportunity each consumes;
- which traits can receive multi-contribution, inheritance, multiplier, or hidden-transformation content;
- `30`-value achievement naming, reward, signaling, and persistence;
- dense-combat readability, path contention, report density, and performance budgets at high population.

## Progress

- 2026-08-31: User rejected the fixed `5 deployed + 3 reserve` direction in favor of run-grown population with an ordinary endgame ceiling of `10`, an explicit-build physical ceiling of `18`, and a possible hidden trait-value landmark of `30`.
- 2026-08-31: User fixed every persistent hero at one population regardless of tier and left the starting curve, tier taxonomy/odds, reserve size, and recruitment economy unresolved.
- 2026-08-31: User confirmed the core/engine/function hierarchy, the five-part archetype grammar, and population as a cross-archetype chassis with explicit count bases and anti-quadratic safeguards.
- 2026-08-31: Documentation-only synchronization authorized. No architecture, code, resource, persistence, UI, test, QA, build, Godot, or migration work is part of this turn.
- 2026-08-31: Documentation synchronization completed across `gameplay-design/README.md`, `gameplay-design/tower-autobattler-core.md`, new authority `gameplay-design/combat-build-framework.md`, the deferred hero-roster task, and this discussion task. No forbidden or separately owned surface was edited.
- 2026-08-31: Documentation verification passed: both gameplay authority routes exist; current accepted sections contain no obsolete five-deployed/three-reserve claim; `10 / 18 / 30`, one-population-per-hero, and unresolved reserve/curve/tier/economy markers are present; touched documents have no trailing whitespace; repository `git diff --check` exits clean. No build or Godot command was run.

## Resume Condition

Implementation remains deferred. Resume design discussion by resolving one bounded topic at a time, starting with the ordinary starting/growth curve, tier availability, reserve contract, or recruitment economy. Resume implementation only after the separately owned architecture work completes or explicitly hands off, system authority is reconciled, and the user confirms the remaining capacity/economy rules needed by that implementation slice.

## Verification Handoff

- Confirm `gameplay-design/combat-build-framework.md` contains all confirmed rules and marks every unresolved number as deferred.
- Confirm `gameplay-design/README.md` routes both core and build authority.
- Confirm current gameplay authority and the hero-roster deferred task no longer state five deployed heroes or three reserves as accepted capacity.
- Confirm `10` means ordinary endgame formation ceiling, `18` means physical full-board population-build ceiling, and `30` means possible hidden trait value rather than body count.
- Confirm no implementation or separately owned task was modified.
- Verification for this turn is documentation scans plus `git diff --check`; no build or Godot evidence is expected.
