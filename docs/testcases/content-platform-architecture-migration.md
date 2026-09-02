# Content Platform Architecture Migration Test Cases

## Phase 1 — Typed Effect Kernel And Lifecycle

Automated acceptance:

- Load real authored binding Resources and compile them into immutable runtime specifications.
- Resolve a damage → healing → shield reactive chain in deterministic waves with one immutable snapshot per wave.
- Prove reverse enqueue order cannot change stable commit order.
- Reject inline re-entry and dependency cycles; require a registered typed processor for every authored atomic effect.
- Interrupt invocation, step, event, depth, rate, usage, and repeated-edge overflow with typed causal evidence.
- Complete victory, defeat, timeout, abort, replacement, exception, and disposal idempotently with zero listeners, pending invocations, and live scoped runtime state.
- Compare the typed compatibility route against the legacy probe for floor damage, floor healing, and Blood Rush healing. Outcome, tick count, digest, command facts, and complete unit snapshots must be identical.
- Preserve existing floor-rule, death, movement, report, presentation, run, content, formation, and UI contracts.

Run serially with the Godot 4.7 Mono console executable:

```text
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
Godot --headless --path . tests/EffectKernelContractSmoke.tscn
Godot --headless --path . tests/ContentContractSmoke.tscn
Godot --headless --path . tests/FixtureContractSmoke.tscn
Godot --headless --path . tests/FormationDeploymentContractSmoke.tscn
Godot --headless --path . tests/GameplayContractSmoke.tscn
Godot --headless --path . tests/MovementPresentationContractSmoke.tscn
Godot --headless --path . tests/AlphaRunSmoke.tscn
Godot --headless --path . tests/UiSmoke.tscn
Godot --headless --path . --quit-after 5 tests/CleanStartup.tscn
git diff --check
```

Expected known output: `ContentContractSmoke` deliberately emits its structural/lifecycle negative-fixture errors before its success marker, and `UiSmoke` retains the documented focus-escape warning. Both processes must still exit `0` with their success markers.

## Phase 2 — Ability And Status Products

Additional automated acceptance:

- Compile all production command/automatic abilities, loadouts, and statuses as one immutable publication graph.
- Cover manual, battle-start/periodic automatic, triggered, and passive entry points; cooldown, maximum-use, and runtime-instance isolation.
- Prove insufficient mana/gold, defeated owner, missing summon template, unavailable summon cell, Effect condition/usage preflight failure, and commit failure leave mana, gold, use count, cooldown, events, units, and status state unchanged. Unsafe late, multi-kernel, multi-step, and damage/death-chain Ability effect shapes must be rejected until their batch rollback contract exists.
- Verify status source/owner attribution, timed and permanent duration, refresh-longer, stack cap, expiry, dispel, periodic binding, and immutable presentation snapshots.
- Prove Time Stop blocks exactly 18 owner actions while pausing cooldowns, then resumes on the next action; prove damage multipliers compose in application order, respect authored tag filters, and stop changing combat attributes at the authored stack cap. Timed or dispellable damage multipliers must be rejected until attribute rollback is authoritative.
- Reject duplicate stable ids, orphan loadouts/abilities/statuses, missing status dependencies, and unknown summon content ids before publishing any compiled batch.
- Compare all eight production hero commands and both periodic Boss abilities against the preserved adapter: complete gameplay state, event order, digest, mana, gold, uses, timing, summon limit, and landing cells remain equivalent.
- Complete every Battle termination path with zero Effect, Ability, and Status runtime state.

Add this focused scene before the existing serial regression list:

```text
Godot --headless --path . tests/AbilityStatusContractSmoke.tscn
```

## Phase 3 — Relic Lifecycle And Scope Bridge

Additional automated acceptance:

- Load every production Relic resource through its independent item scene and assert current ids, exact values, and absence of production compatibility modifier providers.
- Compile immutable definitions transactionally; reject stable-id collisions, invalid numeric shapes, unknown summons, orphan definitions, unregistered references, and incomplete publication.
- Prove two Run scopes and two Battle snapshots do not share mutable state; preserve registration/persistence order for duplicate content ids.
- Verify stacks 1/2/4 and duplicate instances compose multipliers with Pow, bonuses with Add, and summon availability with OR without merging by content id.
- Verify Gilded Contract contributes `+3 * stacks` only on victory and failure outcomes contribute zero.
- Reject wrong run, floor, battle, fingerprint, instance projection, or authored outcome evidence before mutation; reject repeated and stale transitions without mutation.
- Complete victory, defeat, timeout, abort, replacement, exception, and disposal idempotently with zero Run/Battle relic instances.
- Prove a failed `SaveActiveRun` leaves the original active Run object and all state unchanged, the same transition retries successfully once, successful copy-back preserves object identity, and an old transition cannot save or mutate again.
- Round-trip version-3 `InstanceId`, `ContentId`, `Stacks`, `Charges`, and `Roll` through JSON and disk; preserve them through v2→v3 migration and reject negative charges without adding schema v4.
- Source-scan Battle code to prohibit direct Run/Meta/Save authority references.

Run this focused scene before the complete serial regression list:

```text
Godot --headless --path . tests/RelicContractSmoke.tscn
```

## Phase 4 — Project Composition, Run Services, Pools, And Encounters

Additional automated acceptance:

- Load `alpha_project.tres` through Godot, compile its project/campaign/run-rules/presentation graph transactionally, and assert the existing 3×5 tower, capacities, choices, economy, recovery, event, reward, node-table, pool, encounter, and Boss-timeline values exactly.
- Compare authored generation against the preserved pre-migration algorithm for five fixed seeds across all fifteen floors, every option list, and Combat/Elite/Boss enemy, rule, title, and count projection.
- Reject the entire project publication for wrong pool category, unknown pool content, stable-id collision, missing required encounter, mismatched Boss timeline, or an out-of-campaign Boss floor; diagnostics must identify the authored boundary.
- Compose a test-only project/campaign/region/enemy pool/encounter from existing primitives, then pass it through `RunApplication`, `TowerGenerator`, battle preparation, and `BattleSimulation` without a concrete-content branch in any center.
- Require battle results to carry the immutable encounter/run/floor/battle identity created by battle preparation; a result from another encounter or Run cannot resolve the pending node.
- Source-scan `GameRoot` for central resource-path/value authority, `RunApplication` for concrete-content dispatch and missing cohesive services, `TowerGenerator` for authored definitions/content ids/switch descriptions, and `BattleSimulation` for project-composition ownership.
- Require `GameRoot` to remain a bootstrap-only root; screen node lookup, view-model construction, dynamic choice reconciliation, and screen input binding belong to authored screen-local controllers. `GameFlowCoordinator` owns routing and exactly-once user-flow coordination with no scene-tree lookup, while `RunApplication`/`RunNodeResolutionService` own the settlement transaction.
- Prove a compiled multi-phase Boss timeline reaches `BattleConfig`, installs its opening loadout, changes the authoritative loadout at a health threshold, and clears its mutable phase index on Battle-scope completion. Region resources must not retain a competing enemy/Boss/floor-rule surface.
- Source-scan the Domain, Project, Run, Battle, Formation, and Persistence boundaries for one-way dependencies: shared tower identity is Domain-owned, Project and Battle do not reference Run, and formation persistence depends only on a narrow port plus pure validation policy.
- Preserve formation transactions, rewards/economy, progression, schema-v3 saves, exactly-once Relic transitions, all three Alpha paths, UI navigation, and player-facing flows.

Run this focused scene before the complete serial regression list:

```text
Godot --headless --path . tests/ProjectCompositionContractSmoke.tscn
```

`CleanStartup.tscn` includes a test-only managed-finalizer drain before Godot 4.7's `--quit-after` teardown. This prevents the engine's native-container shutdown race and does not change production startup or gameplay behavior.

## Phase 5 — Scalable Authoring And Compatibility Removal

Additional automated acceptance:

- Publish a complete independently authored fixture package containing a hero, soldier, Boss, item, manual Ability, automatic Ability, Status, Relic, floor-rule scene, typed pools, three encounter categories, Boss timeline, region, campaign, run rules, project, and catalog without adding fixture-id behavior to a composition root.
- Instantiate each concrete scene in isolation through ready/process and bind/activate/deactivate lifecycles; compile the complete Ability/Status/Relic/project dependency graph before Registry publication.
- Publish Catalog, canonical Ability/Status/Relic products, and compiled `GameProject` through one composition transaction. Invalid Ability, invalid Project, and an unregistered Boss-phase loadout must each expose no Registry, Project, compiled graph, or non-zero version.
- Allow one authored `AbilityDefinition` Resource to be referenced by multiple distinct loadouts: compile it once, publish one canonical compiled record reused by every loadout, and keep runtime owner state isolated. Two distinct Ability Resources with the same stable id must still reject the complete package with zero publication; duplicates inside one loadout retain their existing rejection semantics.
- Require a real two-phase Boss fixture with distinct non-null loadouts and Abilities; after the health threshold, Battle must replace the opening loadout and execute the second effect.
- Prove two Runs and two Battles sharing the same authored Resources own independent mutable state, produce identical same-seed encounters/results/effect traces, and complete with zero Effect, Ability, Status, and Relic runtime state.
- Fingerprint the full stored authored Resource graph and a deep compiled package projection before and after runtime execution. Sensitivity probes must detect nested Ability operations/effects/targets, Status periodic/presentation data, Relic modifiers/outcomes, and Project pools/encounters/Boss phases/loadouts.
- Reject an invalid Ability package with resource-path plus `operation[0]` diagnostics and reject an Effect dependency cycle; neither failure may publish a partial Registry.
- Preserve the isolated v2→v3 formation/relic migration fixture and schema-v3 authority.
- Source-guard the production Registry's strict validator, zero concrete fixture-id dispatch across the complete production source tree, removal of the obsolete Hero-command runtime/fallback chain, and removal of the item modifier-provider compatibility chain.
- Source-guard `GameRoot` against split Content/Project publication and every `src/**/*.cs` file outside the two explicit compiler authorities against direct Ability/Status/Relic compilation. Ability/Status compilers cannot depend on `BattleSimulation`; fixed-tick conversion comes from the neutral Domain timing contract.
- Scan every production source file as a no-finding positive for concrete Effect/Ability/Status/Relic/content/project ids, donor-project absolute paths, and unrestricted root/group discovery. Content, Battle, and Components additionally reject parent or cross-sibling tree traversal. One immutable guarded-family list must construct the concrete-id matcher and directly generate an exact-diagnostic negative probe for every listed family, with required-family and uniqueness gates; prefix-only/incomplete-id near misses and the `hero_command` semantic allowlist are no-finding positives. Separate negatives cover both donor path separators, global discovery, and strict local traversal.
- Retain only compatibility boundaries with real consumers: authored Ability/Status and floor effects still use the typed Battle mutation bridge, while the attack/death recursion and current Boss `UnitBehavior` comparison remain outside this migration.

Run before the complete serial regression list:

```text
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
Godot --headless --path . tests/ScalableAuthoringContractSmoke.tscn
```

Expected success markers include `CONTENT_CONTRACT_OK ... source-guard=<count>-families-data-driven` and begin with `SCALABLE_AUTHORING_CONTRACT_OK`; then run every Phase 1–4 focused scene plus formation, gameplay, movement, Alpha, UI/input, semantic/visual, clean-startup, and `git diff --check` gates serially.

## Final v2 — Settlement Retry And Shared Ability Reuse

Additional automated acceptance:

- `RunApplication` exposes a typed `RunBattleResolution`; its bool completion facade remains compatibility-only. Accepted victory, defeat, and timeout are distinct from rejected settlement and persistence failure.
- Drive the authored GameRoot/AppScreenHost/BattleReport scene with a save double whose first battle-publication write fails and second succeeds. The first attempt must leave the original Run and transition retryable, must not set the coordinator committed flag or show `征程失败`, and must expose a visible `重试结算` action without automatic looping. Real button input retries the same result/encounter once; success commits once, preserves authoritative Run object identity, advances exactly one floor, and routes to reward. Accepted victory/defeat/timeout remain exactly-once.
- Publish a complete authored package in which two distinct loadouts reference the same Ability Resource. Both loadouts must reference the same canonical `CompiledAbilityDefinition`; two runtime owners execute with isolated mana/cooldown/use state. A distinct-Resource stable-id collision still yields no compiled batch and no package/version.
- Source guards keep `GameRoot` bootstrap-only, require Coordinator→typed Run resolution, prohibit Coordinator-owned save mutation, and reject the former batch-wide compiled-stable-id collision pattern.

Run the low-concurrency build, `AbilityStatusContractSmoke`, `ScalableAuthoringContractSmoke`, `RelicContractSmoke`, `UiSmoke`, `GameUiInteractionReliabilityContractSmoke`, `ProjectCompositionContractSmoke`, and `GameplayContractSmoke` before the complete seventeen-scene serial regression, isolated clean startup, source guards, and `git diff --check`.
