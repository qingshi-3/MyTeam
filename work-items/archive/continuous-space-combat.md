# Grid Deployment And Continuous-Space Combat

Status: Completed

## Goal And Confirmed Decision

The user confirmed on 2026-09-05: deployment snaps to unique grid cells; battle starts at those cell centers, then units move and stop freely between cells, including when attacking. Implement the previously discussed centralized-simulation approach. This is an authorized Combat/Navigation/Presentation spatial migration, not a deployment redesign.

## Authority And Architecture

Read `gameplay-design/tower-autobattler-core.md`, `gameplay-design/combat-build-framework.md`, and `system-design/tower-autobattler-architecture.md`. Before runtime edits, replace their contradictory grid-march battle contracts with the confirmed continuous-space contract. Keep deployment grid ownership and immutable authored resources. BattleSimulation remains the sole movement, targeting, damage and event authority; visual interpolation and Area2D overlap callbacks cannot decide hits. Update design routing if a focused spatial module authority is needed. Do not create proposal archives or expand AGENTS.md.

## Scope

- Preserve exact validated deployment, population, saved formations and Lab placement. Convert deployed cells to continuous battle positions at battle start.
- Introduce authored small circular body geometry, continuous positions and controlled speed. Keep terrain and boundaries meaningful. Units may stop anywhere, must not pass through living bodies or obstacles, and do not bounce or push as rigid bodies.
- Replace grid destination arbitration with deterministic continuous movement behind a focused service: stable strategic target, reachable engagement positions around its body, local avoidance/nonpenetration, bounded congestion recovery and target release on death. Grid or sampled geometry may assist path search but must not quantize authoritative motion or attack positions.
- Use consistent logical battle-space distance independent of UI scale. Separate body radius from attack reach. Define range convention explicitly, preserve practical authored balance where feasible, and use stopping margin/hysteresis without silently enlarging hit reach.
- Migrate range-dependent targeting/healing, splash, piercing, live auras, floor interactions, summon placement and spatial event/report projections. Filter legal targets and deduplicate area hits by runtime identity. Preserve deployment-time formation effects as deployment rules where authority says so.
- Preserve existing instantaneous attack timing in this spatial migration unless a minimal compatibility change is necessary; do not invent projectile combat or a new windup system. Document this boundary plainly. Any existing hit logic must read current authoritative positions, never delayed visuals.
- Preserve fixed combat timing (currently 0.1 s), cooldown/status duration, pause, x1/x2/x4, Lab single-step, seeded reproducibility, rollback and cleanup. Fixed bounded movement substeps may prevent tunneling without changing combat tick semantics.
- Update presentation to interpolate continuous movement without forced cell stops/hops or competing transform authority; maintain selection, facing, defeat, resize and bounded visual lag.
- Update meaningful obsolete grid tests and add continuous-space regression cases and `docs/testcases/` manual QA. Remove obsolete production grid code when replaced rather than maintaining two conflicting authorities.

## Non-Goals And Constraints

- No changes to unrelated `web/game-mechanics-atlas/` or its existing work items; they already have user changes.
- Develop on existing main; no branch creation/switching, no commits unless asked. Donor D:/godot/rpg stays read-only.
- No multiplayer, generalized physics engine, new abilities, balance overhaul, deployment UX redesign, attack windup/projectile feature, or direct engine physics authority.
- Resource-first authoring and existing component composition apply. Use low-concurrency builds; do not launch concurrent Godot/build processes or terminate user processes.

## Acceptance

1. Deployment remains one unit per legal cell; battle motion and attack stops demonstrably include non-cell-center positions.
2. Multiple attackers can approach distinct positions; dense allied traffic, opposite traffic and narrow terrain do not tunnel, overlap persistently, oscillate indefinitely or remain stuck when a reachable useful action exists.
3. Attack/heal ranges, area hits, piercing and live proximity effects use the same continuous space; death releases collision/target state immediately, summons find a legal nonoverlapping position or fail safely.
4. Pause/single-step and x1/x2/x4 preserve timing and deterministic final/event projections for equal configuration/seed. Rollback restores spatial and planning state.
5. Existing relevant production smoke/contract tests and new adversarial spatial cases pass. No obsolete grid assertions are silently skipped; document what each replacement verifies.
6. Visible movement is continuous, selection tracks the visible unit, defeat does not slide, and projection resizing does not change simulation outcomes. Actual mouse input must be used for interaction acceptance if UI interactions are changed; signal-only checks are insufficient.

## Progress And Recovery

- Discussion complete; user explicitly requested implementation.
- Baseline main contains unrelated web/research edits. Preserve them.
- 2026-09-05 execution started on `main`. Read project/user rules, all three required authority documents, and the GodotPrompter navigation, C#, testing, resource, and review guidance. Architecture decision: deployment/save remain cell-based; `BattleSimulation` owns continuous logical positions and deterministic circular-body movement; presentation remains a projection consumer; engine physics is not authority.
- Authority milestone complete: contradictory grid-march, cell-event, fixed cell-hop presentation, and temporary free-cell contracts were replaced before runtime edits.
- Runtime milestone complete: `BattleSimulation` owns continuous positions, authored circular bodies, continuous movement/range/area/event/report/rollback facts, continuous summon reservations, and position-driven presentation. The route resolver now checks the remaining reachable coarse nodes when none of the nearest twelve can see a legal continuous goal, avoiding false unreachable results around concave terrain. The unused terrain snapshot accessor was removed.
- Presentation milestone complete: motion now follows 0.125-second x1 authority samples with speed-scaled linear interpolation, ordered turn preservation, safe collinear overflow coalescing, bounded backlog compression, and one sustained travel/character-lift state. A 30 FPS, 96-sample run observed queue maxima `1 / 1 / 2` at x1/x2/x4 and settled in approximately 0.267 seconds after input stopped.
- Spatial regression milestone complete: focused cases cover non-center authoritative movement, swept terrain and bodies, dense and symmetric traffic, distinct engagement goals, splash, piercing, live auras, beacon control, blocked edge LOS, 0.1-second move cooldown, continuous rollback/replay, summon nonoverlap, digest/report positions, and x1/x2/x4 presentation throughput. Representative dense 9v9 resolution completes 36 ticks with 431 movement/attack actions and `3.798 / 3.8` progress in approximately 243 ms on this host.
- Alpha investigation complete: the carry path's final Boss defeat was caused by the obsolete row-major test formation concentrating the party along the top edge, outside the center beacon that removes Boss Ward. The end-to-end smoke now uses the public typed formation evaluation/commit path with an objective-centered legal priority; all original victory assertions remain intact, and commander/carry/solo again clear all 15 floors.
- Independent verification so far: `FormationDeploymentContractSmoke`, `BattleLabBattleLifecycleContractSmoke`, `AbilityStatusContractSmoke`, `RelicContractSmoke`, `ContentContractSmoke`, `Phase5BattleDensityContractSmoke`, `BattleLabFrostBehaviorContractSmoke`, `MovementPresentationContractSmoke`, and `AlphaRunSmoke` pass. The density check reached 60 living units for 30 ticks with deterministic replay around 210 ms total. `BattleFloatingCueContractSmoke` also passes after its real pointer input was moved from an obsolete fixed cell center to the concrete presenter's current visible position; it still verifies both controlled-time and exact Freeze-Status details.
- Focused manual QA lives in `docs/testcases/continuous-space-combat.md`, including an explicit map from former grid assumptions to continuous-space automated and manual replacements. `VisualCapture` no longer names grid-march assumptions and now proves three distinct attackers at non-cell-centered positions around one target. Complete 1280×720 and 1600×900 capture runs both returned `VISUAL_CAPTURE_OK`; `UI_1280x720_BattleContinuousSpaceSurround.png` and `UI_1600x900_BattleContinuousSpaceSurround.png` were visually inspected with no clipping or body overlap.
- Executor validation commands completed successfully: low-concurrency `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` (0 warnings, 0 errors); headless `GameplayContractSmoke`, `MovementPresentationContractSmoke`, `AlphaRunSmoke`, and `BattleFloatingCueContractSmoke`; graphical `VisualCapture` at 1280×720 and 1600×900; and `git diff --check` (line-ending notices only in pre-existing unrelated files). Godot consistently reports two known ability Resource UID text-path fallback warnings, with no associated load failure.
- Final code review removed the now-unused terrain snapshot dictionary after its accessor was deleted; `_blockedTerrainCells` remains the single immutable tick terrain representation used by route and sweep queries.
- Final independent verification on the completed source passed: low-concurrency build reported 0 warnings and 0 errors; `GameplayContractSmoke` passed all cases with dense 9v9 at 234 ms for 36 ticks and 431 actions; `BattleFloatingCueContractSmoke` passed. The primary verifier also confirmed the prior nine independent suites, inspected both clean 1280×720 and 1600×900 surround images, found no obsolete grid service/presentation symbol references in runtime, tests, authority, or QA, and reported a clean `git diff --check`. No review findings remain.
- Execution agent must maintain this same document at start, milestones and handoff with changed scope, commands/results, unresolved risks and resume entry points.
- If a material contradiction requires a new decision, stop affected edits and mark Needs Discussion; ordinary bounded implementation choices are authorized.

## Verification Handoff

Executor completes implementation and necessary focused tests, then marks Awaiting Independent Verification (do not archive). Primary agent reviews changes, reruns appropriate acceptance checks, resolves defects via executor, and records final evidence. Only completed verified work may be archived.
