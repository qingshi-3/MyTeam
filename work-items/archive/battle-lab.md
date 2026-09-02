# Battle Lab

Status: Complete  
Confirmed: 2026-09-01

## Goal

Add a developer-facing Battle Lab that bypasses tower progression and lets a user assemble player heroes, player equipment, team relics, and published PvE enemies on the physical battlefield, then run the exact production battle pipeline with deterministic controls.

The Battle Lab is a PvE content/debugging tool. It is not a player-facing PvP mode and it must never own a parallel combat implementation.

## Confirmed Product Contract

### Entry and lifecycle

- Add a clear `战斗实验室` entry to the existing main menu and host the Lab as an independent screen inside the existing `AppScreenHost` flow.
- Entering the Lab must not traverse tower nodes, shops, rewards, recruitment, or an Active Run.
- Configuration and battle runtime are separate lifetimes. Starting a battle freezes/deep-copies the editable configuration and materializes fresh runtime state.
- Returning from battle restores the original placement, equipment, relics, mode, and seed without copying health, statuses, counters, or other battle mutations back into the editable configuration.
- Reset and subsequent starts must rebuild every battle, floor-rule, equipment, relic, trait, status, and simulation runtime scope from clean inputs.
- Leaving and re-entering the Lab must not retain subscriptions, Tweens, nodes, or mutable battle state.

### Published content libraries

- The player library exposes every published player hero permitted by formal catalog/compiled-content classification.
- The enemy library exposes every published PvE normal enemy, elite, Boss, and formally referenced summon that is legal to instantiate independently.
- Boss classification may use `UnitRole.Boss`. Elite and summon membership must be derived from authored/compiled relationships, never concrete ids, paths, or stable-id prefixes.
- The equipment and relic libraries expose all published definitions through the formal package/compiled graph.
- Search and filtering operate on typed published metadata. No Battle Lab-owned content list is authoritative.

### Placement and identity

- Drag player heroes and PvE units from their libraries onto the battlefield through real mouse input.
- Drag placed units to reposition them. Support swap, recall to the originating library, explicit delete, and clear-all.
- Every placed unit has a unique runtime/editor instance id even when several instances reference the same stable content id.
- One physical cell contains at most one unit. All first-version units occupy exactly one cell.
- Placement validation must reject invalid edits before battle preparation. It may not depend on `BattleSimulation` clamping or nearest-free-cell repair.
- Valid, invalid, dragging, swapping, success, and failure states require non-colour signals such as shape, icon, motion, cursor, and concise Chinese reasons in addition to colour.

### Placement modes

#### Formal rules mode

- Player placement uses the production 3×6 player deployment region, population, physical-cell, unique-occupancy, floor, equipment-slot, and other reachable production rules.
- The configured current population is explicit and defaults to the production initial population.
- PvE units use the production enemy region and formal physical restrictions, while their selected composition remains a Lab input.
- This mode proves that the player deployment is production-legal; it does not claim that an arbitrary Lab-authored enemy composition already exists as a campaign encounter.

#### Free experiment mode

- Both sides may occupy any otherwise legal cell of the 10×6 physical battlefield.
- Ordinary population and side-region restrictions are ignored so extreme configurations are possible.
- Bounds, forbidden cells, unique instance identity, one-cell occupancy, one-unit-per-cell, and `BattleSimulation` invariants remain mandatory.
- The screen must prominently and persistently display `自由实验配置`; colour alone is insufficient.

### Player equipment

- Equipment is attached to a concrete player-hero instance, never only to a stable hero definition.
- Each player hero obeys the production three-slot contract.
- Replacing and removing equipment are supported.
- Equipping the same definition on two heroes creates two distinct equipment instances and source identities.
- PvE units do not expose player equipment slots.
- Shared `EquipmentDefinition` resources remain read-only.

### Team relics

- Relics belong to the player team/Lab configuration, not an individual hero.
- Adding, removing, and configuring positive legal stack counts are supported using the current production relic legality. The Lab must not invent an authored maximum stack rule that production does not have.
- PvE units do not receive player relics.
- Shared `RelicDefinition` resources remain read-only.

### Derived information

Every configuration change refreshes:

- current player count, population use, and population legality;
- each hero's equipment instances;
- player-team relics and stack counts;
- trait contributions, current thresholds/tiers, and prepared values;
- final battle-readiness validity;
- concise Chinese reasons for every condition preventing battle start.

The selected-unit inspector shows at least:

- stable content identity plus unique Lab/runtime instance identity;
- prepared/final key attributes, including health, damage, attack speed/rate, reach, and control resistance where available;
- player equipment or an explicit not-applicable enemy state;
- trait contributions and active tier information;
- during battle, active status stacks, remaining duration, source id, and multi-source contribution information exposed by the formal runtime.

### Battle controls

- Accept an explicit fixed integer random seed and provide a deterministic default.
- Support start, pause, continue, one fixed-tick step while paused, x1/x2/x4 speed, reset, and return to configuration.
- One-step advancement is owned by `BattleScreen`: it advances the simulation and also drains/presents events, refreshes inspection, and handles terminal state. The Lab must not reach into private simulation state.
- The same canonical experiment configuration and seed must produce the same result and event/digest projection.
- Provide clear-all and restore-default actions.

### Presets

- Built-in presets are authored project resources/data. User presets are versioned JSON under an isolated `user://battle_lab/` namespace.
- Presets contain only stable content ids, unique instance configuration, cells, player equipment instances, player-team relic stacks, mode, population/rules inputs, and seed.
- Presets never serialize Godot `Resource` objects or battle runtime state.
- The Lab must not call or mutate the production Meta, Settings, Active Run, or schema-v4 save path.

### Built-in frost-system preset

Add one built-in `冰霜体系验证` preset using formal published content:

- at least two equipment-capable player units;
- two independent `霜痕战刃` equipment instances;
- an active `凛冬盟约` trait tier;
- one normal high-health PvE target;
- one published development PvE target with non-zero control resistance;
- a setup that makes attack-speed growth, multi-source frost stacks, three-stack freeze conversion, shortened freeze from control resistance, and status apply/stack/expire feedback observable.

Concrete ids required to reference these authored contents may appear only in preset data. No runtime dispatch, special case, or code branch may depend on them.

The current production authoring path gives published units zero base control resistance and only test fixtures provide a non-zero value. Execution therefore includes a generic base-control-resistance authoring path plus one independently instantiable, formally published development target that is excluded from campaign encounter pools. The Lab must not inject resistance by checking its id.

## Architecture Contract

```text
Published CompiledGamePackage
            ↓
BattleLabContentIndex
            ↓
BattleLabSession (editable, isolated mutable state)
            ↓
BattleLabPlacementPolicy
    ├── Formal rules mode
    └── Free experiment mode
            ↓
Shared BattlePreparationAssembler
    ├── RunBattlePreparationAdapter
    └── BattleLabPreparationAdapter
            ↓
Production BattleScreen → BattleSimulation → BattleReport
            ↓
Return to unchanged BattleLabSession
```

### Formal systems to reuse

- Atomic content publishing: `GamePackagePublisher.CreateReadyAsync`, `CompiledGamePackage`, `ContentRegistry`, `CompiledContentGraph`, and `CompiledGameProject`.
- Published catalogs and graph collections for units, encounters, equipment, relics, traits, statuses, tactical commands, abilities, behaviours, and summon references.
- Unit snapshot preparation: `BattleSetupFactory.Snapshot`.
- Population and formation: `RunPopulationPolicy`, `RunFormationPolicy`, and `RunFormationService`.
- Equipment: `RunEquipmentService` and `EquipmentBattlePreparationBuilder`.
- Relics: `RunRelicService.PrepareBattle`, `RelicRunScope.InitialRunCounters`, and formal relic runtime transitions.
- Traits: `RunTraitSnapshotBuilder`, `TraitSnapshotBuilder`, and `TraitBattlePreparationBuilder`.
- Battle: `BattleConfig`, `BattleSimulation`, `BattleScreenController.StartBattle`, `BattleScreen`, and `BattleReport`.
- UI atoms: `BattlefieldProjection`, `DeploymentCell`, `DeploymentUnitCard`, `UnitChoiceCard`, `ItemChoiceCard`, `UnitPortrait`, semantic facts/components, and `RealmTheme`.
- Real-input test patterns: `DeploymentInputHeroSelectionContractSmoke` and `GameUiInteractionReliabilityContractSmoke`, which use `Viewport.PushInput`.

### Minimum new ownership

- `BattleLabSession`: isolated edit model and immutable start snapshot.
- `BattleLabContentIndex`: compiled published-content projection and typed filtering.
- `BattleLabPlacementPolicy`: placement transactions and Chinese failure semantics.
- `BattleLabPreparationAdapter`: converts the Lab snapshot to the shared formal preparation request.
- `BattleLabPresetStore`: built-in data plus isolated versioned user JSON.
- `BattleLabScreen`/coordinator and focused authored component scenes for libraries, board controls, loadout editing, relic editing, inspection, and battle controls.
- `BattleLabUnitInspector`: configuration-stage prepared projection plus battle-stage read-only runtime projection.
- A small public `BattleScreen` control/read-model boundary for pause, continue, fixed-step, speed, terminal state, and selected-unit inspection.
- A generic authored base-control-resistance input and one development-only published target excluded from campaign pools.

### Required shared extractions

- Extract a production-neutral battle-preparation request/assembler from the current Run-oriented `ActiveRunDto + EncounterPlan` service. Existing Run preparation becomes an adapter and must retain identical behaviour.
- Extract a pure Active Run/configuration validator from persistence ownership so validation can run without loading or saving Meta/Run data.
- Neither extraction may add `if BattleLab` branches to `BattleSimulation` or general combat systems.

### Runtime and resource boundaries

- Shared definitions and published resources are immutable.
- Lab edit state, equipment instance state, relic instance/counter state, prepared battle state, and simulation state are separate objects with explicit lifetimes.
- Content scenes remain independently instantiable and do not depend on hidden Battle Lab nodes.
- UI structure is authored in `.tscn`; reusable static data/theme is authored in `.tres`. Runtime code binds and refreshes templates rather than constructing a whole interface tree.
- Child components emit typed signals upward; the Lab coordinator drives owned children through explicit methods. Every C# subscription must be disconnected on replacement/exit.

## Authority Impact

Before production implementation, update the existing authoritative documents rather than creating proposal copies:

- `gameplay-design/tower-autobattler-core.md`: record the developer-only Battle Lab contract, formal/free-mode semantics, and the rule that it exercises production PvE systems without becoming a Run/PvP mode.
- `system-design/tower-autobattler-architecture.md`: record Lab session ownership, published-content indexing, shared preparation assembler, save isolation, BattleScreen control/read boundary, and immutable-resource/runtime-state separation.
- `docs/testcases/`: add a focused Battle Lab manual/real-input QA entry covering the confirmed interaction and isolation matrix.

Do not expand root `AGENTS.md`; the task is not a new stable routing rule.

## Scope

- Main-menu entry and AppScreenHost routing.
- Authored Battle Lab configuration and battle screens.
- Full published player/PvE/equipment/relic libraries with typed search/filter projections.
- Dual-mode, two-sided, one-cell placement transactions.
- Player equipment and player-team relic configuration.
- Population, trait, final-stat, readiness, and Chinese failure projections.
- Production-neutral battle preparation shared by Run and Lab.
- Production BattleScreen/Simulation execution and deterministic controls.
- Isolated built-in and user presets.
- Frost-system validation preset and the minimum generic control-resistance authoring/content needed for it.
- Focused, real-input, save-isolation, determinism, lifecycle, source-guard, build, and regression verification.

## Non-Goals

- PvP, networking, replay synchronization, enemy hero construction, or symmetric player rules.
- Enemy equipment, enemy relics, or enemy tactical-command loadouts.
- Real 1×2, 2×2, or other multi-cell units; visual scaling must not pretend multi-cell occupancy exists.
- AI, targeting, movement, action-order, balance, encounter, reward, economy, or combat-number changes.
- A parallel battle simulator, Lab-specific status/ability/effect rules, or concrete-content runtime dispatch.
- A general Ability/Status/Effect editor or arbitrary base-stat override console.
- Tower map, shops, recruitment, rewards, normal Run progression, or campaign authoring changes beyond excluding the development target from campaign pools.
- Meta, Settings, Active Run, schema-v4, or production-save semantic changes.
- Batch simulation, balance analytics, tournaments, cloud preset sync, a complete UI redesign, or new art production.

## Hard Constraints

- Work on current `main`; create no branch, commit, reset, clean, or checkout-overwrite operation.
- Preserve all pre-existing dirty and untracked changes. Record the touched-path baseline and never claim unrelated changes.
- Use one writing executor at a time. Read-only reviewers may work in parallel but may not modify the repository.
- Never close, control, or otherwise interfere with the user's running Godot editor or game process.
- Use the formal content publication, compiled graph, battle preparation, stats, status, trait, equipment, relic, and `BattleSimulation` systems.
- No concrete unit, enemy, item, relic, status, trait, or ability id in runtime dispatch; no stable-id prefix inference.
- Do not mutate shared `EquipmentDefinition`, `RelicDefinition`, unit definitions, portrait data, or other shared resources.
- Do not add a Battle Lab mode branch inside `BattleSimulation` or generic combat behaviour.
- Do not change production battle numbers, target selection, action order, or balance.
- Keep one unit per physical cell and one cell per unit in version one.
- Use low-concurrency .NET builds (`-maxcpucount:2 -v:minimal`) and serial Godot tests. Avoid repeated editor/import launches.

## Implementation Stages And Exit Gates

### Stage 0 — baseline, authority, and RED contracts

- Record current relevant dirty paths, process state, and save-write baseline.
- Synchronize gameplay, system, and manual-QA authority.
- Add RED contracts for shared preparation parity, Lab data/placement, preset isolation, source guards, and scene/input surfaces.
- Exit only when failures are the intended missing capabilities and no existing regression is introduced.

### Stage 1 — production-neutral core

- Introduce the neutral preparation request/assembler and pure validator.
- Adapt existing Run preparation without changing its prepared `BattleConfig`/digest behaviour.
- Add Lab session DTOs, content index, placement policy, canonical configuration digest, and versioned preset schema/store boundary.
- Exit with focused core tests green, production Run preparation parity green, shared-resource fingerprints unchanged, and zero production-save writes.

### Stage 2 — authored two-sided configuration UI

- Add authored configuration screen and focused templates.
- Wire player/PvE libraries, search/filter, board placement, swap, move, recall, delete, clear, and formal/free modes.
- Add semantic non-colour feedback and Chinese rejection reasons.
- Exit with real mouse `Viewport.PushInput` contracts green for both libraries, movement, swap, recall, invalid cells, clearing, and mode switching.

### Stage 3 — build configuration and inspection

- Add hero-instance equipment editing and team relic editing.
- Refresh population, equipment, relic, traits, tiers, final stats, validity, and Chinese failure details atomically after every edit.
- Extend the selected-unit inspector for configuration and runtime source-aware status information.
- Exit with three-slot/replacement/removal/instance-isolation, relic team scope, trait derivation, and resource-immutability contracts green.

### Stage 4 — production battle lifecycle and controls

- Run the prepared configuration through real `BattleScreen`/`BattleSimulation`/`BattleReport`.
- Add public pause/continue/fixed-step/speed/read-only-inspection controls at BattleScreen ownership.
- Implement reset, terminal replacement, return-to-configuration, and full teardown.
- Exit with same-config/same-seed determinism, fixed-step correctness, preserved edit configuration, and zero residual runtime state/subscriptions/Tweens/nodes.

### Stage 5 — entry, presets, and frost validation content

- Add main-menu/AppScreenHost routing without recreating `RunApplication`.
- Add isolated built-in/user preset UX, default configuration, and `冰霜体系验证`.
- Add generic base-control-resistance authoring and the development target without placing it in campaign pools.
- Exit when frost mechanics are observable through formal content and the preset has no runtime content-id dispatch.

### Stage 6 — verification and handoff

- Run focused tests, real-input tests, the complete existing serial regression matrix, CleanStartup, build, source guards, and diff/process audits.
- Perform Godot-specific code review and fix only in-scope findings.
- Update this document with changed surfaces, evidence, remaining risks, and independent verification entry points.
- Move to `Awaiting Verification`; do not archive until independent verification is complete.

## Acceptance Criteria

### Core architecture

- Existing Run preparation produces equivalent output after the neutral assembler extraction.
- Lab start reaches the same formal preparation, stats, status, traits, equipment, relic, and `BattleSimulation` implementations as production.
- No duplicate combat implementation, `BattleSimulation` Lab branch, concrete-content runtime dispatch, or id-prefix inference exists.
- Shared definition/resource fingerprints remain unchanged through editing, battle, reset, terminal state, preset load/save, and exit.

### Configuration and interaction

- The Lab scene can be independently instantiated, exited, freed, and re-entered.
- Real mouse input can drag a published hero and published PvE unit from their libraries to legal cells.
- Real mouse input can reposition, swap, recall, delete, and clear units.
- Invalid cells, occupied conflicts, bounds, forbidden cells, population, and mode restrictions produce deterministic transaction failures plus non-colour Chinese feedback.
- Formal mode enforces production player rules. Free mode visibly identifies itself and permits extreme configurations while retaining physical invariants.
- Every placed copy has a unique instance id.

### Equipment, relics, and derived state

- Real mouse input attaches equipment to a concrete hero instance and supports replacement/removal.
- Three-slot limits are enforced. Equal equipment definitions on different heroes have distinct instance/source identities.
- Relics act on the player team with legal positive stacks and do not write to a Run.
- Player count/population, equipment, relics, trait contributions/tiers, prepared attributes, readiness, and Chinese failure reasons refresh after every configuration transaction.
- Enemy units expose no player equipment or relic controls.

### Battle and determinism

- Start, pause, continue, one fixed-tick step, x1/x2/x4 speed, reset, terminal handling, and return to configuration work through real UI input.
- Same canonical configuration and seed produce the same terminal result and deterministic event/digest projection.
- Returning to configuration preserves the edit snapshot and does not preserve runtime damage, statuses, counters, cooldowns, or modifiers.
- Replacement, reset, terminal state, exit, and re-entry leave no residual subscription, Tween, node, status, modifier, equipment, relic, trait, floor-rule, or simulation mutable state.

### Presets and isolation

- Preset JSON round-trips stable ids, instance configuration, cells, equipment, relics, mode, relevant rule inputs, and seed without serializing Resources.
- Opening, editing, running, resetting, loading, saving, and leaving the Lab cause zero incremental calls to the real save service after the pre-entry baseline.
- Meta, Settings, Active Run, schema-v4 files, and in-memory production progression remain byte/semantic equivalent.
- Built-in `冰霜体系验证` demonstrates the requested formal frost/status/control-resistance chain.

### Verification gates

- New focused contracts and `Viewport.PushInput` real-mouse scenarios pass serially.
- Existing mature-combat focused gates, completed content-platform serial regression, battle-report/theme/responsive suites, CleanStartup, and other directly affected tests pass serially.
- `dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal` finishes with 0 warnings and 0 errors.
- `git diff --check` passes.
- Source guards find no concrete-content runtime dispatch, stable-id-prefix dispatch, Lab combat clone, or production-save access.
- User-running Godot processes remain open and untouched.

## Progress

- 2026-09-01: Read-only architecture preflight completed after reading root/project authority, active work items, completed content-platform/combat-build migrations, and related test authority.
- 2026-09-01: Preflight confirmed the feature is feasible by sharing production systems, but requires a production-neutral battle-preparation assembler, pure validation boundary, two-sided placement policy, BattleScreen control/read interface, and isolated preset store.
- 2026-09-01: Preflight found the production frost chain is present but no published non-zero-control-resistance target exists; the generic authoring and development-only target above were included in confirmed scope.
- 2026-09-01: User confirmed the complete preflight plan and authorized execution.
- 2026-09-01: This activity document was created as the execution authority. No production code, resource, scene, authority document, test, build, Godot process, save, branch, or external state was changed before this entry.
- 2026-09-01: Stage 0 execution started on `main`. The pre-existing worktree is intentionally extensive and dirty from completed content-platform, mature-combat, UI, and documentation work; no cleanup, reset, branch change, commit, or ownership claim is authorized. The baseline includes existing modifications to both gameplay/system authority and `docs/testcases/alpha-manual-qa.md`, so Battle Lab authority is integrated additively.
- 2026-09-01: User Godot editor PID `23260` (`Godot_v4.7-stable_mono_win64`) is responsive and remains outside executor control. The production user-data directory `C:\Users\qs\AppData\Roaming\Godot\app_userdata\my-team` exists with no files at the Stage 0 baseline; production save-file hash set is therefore empty. The known isolated console executable is `C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe`.
- 2026-09-01: Gameplay, system, and focused Battle Lab testcase authority now record the confirmed developer-only PvE contract, formal/free placement modes, shared preparation ownership, BattleScreen controls, preset/save isolation, Frost validation, real-input matrix, exact serial regression order, and immutable Resource/runtime separation. Stage 0 RED implementation and execution remain next.
- 2026-09-01: Stage 0 RED source is authored as two compile-safe contract scenes. `BattleLabContractSmoke` names the shared preparation, session, compiled index, dual-mode placement, isolated preset, BattleScreen-control, Resource fingerprint, and production-save isolation gaps. `BattleLabInputContractSmoke` names the authored screen, main-menu/AppScreenHost routing, library/board/mode surfaces, and real-input controller gaps without introducing production stubs. Build and RED execution remain next.
- 2026-09-01: Stage 0 exit gate passed. Low-concurrency build is `0 warnings / 0 errors`. `BattleLabContractSmoke` exits the intended `1` with exactly six missing capability groups (shared preparation, session snapshot, compiled index, dual-mode placement, isolated presets, production Battle controls); its source/save-isolation guard is already present. `BattleLabInputContractSmoke` exits the intended `1` with exactly fifteen absent authored/routing/input tokens. Existing `ProjectCompositionContractSmoke` and `ScalableAuthoringContractSmoke` both exit `0` with their established GREEN markers. The RED failures are limited to missing Battle Lab capabilities, so Stage 1 is authorized.
- 2026-09-01: Independent Stage 1 audit found that the first implementation does not yet satisfy the exit gate: new-library placement can overlap an occupied cell, FloorRule resolution/edit validation is permissive, the assembler changes Run `requireLegalFormation=false` semantics, shared preparation remains DTO-level, FreeExperiment preparation truncates the nineteenth player through the 18-slot Run projection, and Restore/preset semantic validation is incomplete. UI/entry/preset expansion is paused while these core findings are corrected with direct behavior contracts.
- 2026-09-01: Stage 1 audit correction completed. `BattlePreparationAssembler` now owns production-neutral content snapshot/loadout preparation, behaviour and HeroRule summons, Equipment and Trait inputs, Relic/Tactical summons, Boss loadout suppression, exact-placement validation, and final `BattleConfig`; Run and Lab adapters only project owned source state. Run `requireLegalFormation=false` retains legacy duplicate-placement acceptance while `true` rejects it, and legal false/true configurations produce equivalent projections/results.
- 2026-09-01: Lab core now normalizes only an empty FloorRule to an explicit deterministic default, strictly rejects unknown rules, validates the active FloorRule on every placement edit, rejects occupied-cell library adds, validates both sides of swaps, and asserts global single-cell uniqueness after transactions. Session/Restore/preset validation covers enums, bounds, formal regions/population cap, content/side, Equipment slots/identity, Relic legality, global identity, cells, and all durable instance sequences.
- 2026-09-01: Lab snapshots now carry an explicit `PrimaryHeroInstanceId`; HeroRule selection no longer depends on mutable instance-id sort order. FreeExperiment no longer projects through the production 18-slot ActiveRun DTO: a direct 19-player regression proves the nineteenth unit, Equipment instance, and Trait contribution enter shared preparation.
- 2026-09-01: Corrected Stage 1 evidence is GREEN: low-concurrency build `0 warnings / 0 errors`; `BattleLabCoreContractSmoke`, `BattleLabContractSmoke`, `FormationDeploymentContractSmoke`, `ProjectCompositionContractSmoke`, `BattleLabPlacementInputContractSmoke`, and `BattleLabBattleLifecycleContractSmoke` all exit `0`. The core contract includes legacy production-builder projections, strict/permissive Run formation behavior, semantic preset distrust after digest recomputation, immutable Resource fingerprint equality, same-seed determinism, and zero production-save writes. `git diff --check` is clean for corrected surfaces; production user-data remains file-empty; user editor PID `23260` remains responsive and untouched.
- 2026-09-01: Stage 6 final verification resumed after Stage 2/3/5 integration. The final cached-code change is the `BattleLabContentIndex` precomputed `10×6` FloorRule occupancy projection plus the `UiSmoke` screen-count marker, so a fresh low-concurrency build and fresh focused/UI/startup executions are mandatory before handoff. The user editor PID `23260` is responsive and remains outside executor control.
- 2026-09-01: Stages 2 and 3 completed. The authored Lab screen now exposes compiled player/PvE libraries, a `10×6` board, real mouse drag/drop, move/swap/recall/delete/side-clear/all-clear, formal/free rules, hero-instance Equipment, player-team Relics, explicit primary hero, atomic derived population/Trait/prepared-stat/readiness facts, and selected-unit inspection. The real-input placement contract proves both libraries, movement, swapping, recall, invalid non-mutating Chinese feedback, mode switching, clearing, and teardown.
- 2026-09-01: Stages 4 and 5 completed. The main-menu entry routes through `AppScreenHost`; default and `冰霜体系验证` authored presets load through isolated Lab data; start/reset use fresh shared preparation and the production `BattleScreen`; terminal state opens the production `BattleReport`; returning restores the unchanged canonical configuration. Pause, fixed-step, x1/x2/x4, reset, report return, determinism, and runtime cleanup are covered by focused lifecycle/App-flow contracts.
- 2026-09-01: Stage 6 implementation review found no new in-scope critical issue. Node references are cached at `_Ready`, child signals are disconnected on exit/replacement, dynamic UI uses authored scenes and deferred freeing, Battle/Effect scopes are disposed through their production owners, shared Resources remain read-only, and the FloorRule occupancy cache removes repeated rule-scene instantiation from drag-hover evaluation.
- 2026-09-01: Final fresh verification is GREEN: low-concurrency build `0 warnings / 0 errors`; all six Battle Lab focused contracts exit `0`; `UiSmoke` exits `0` with `screens=14` and only its known focus warning; all seventeen existing serial regressions exit `0` in the mandated order; separate `CleanStartup` exits `0`; explicit source guards, `git diff --check`, production-save fingerprints, shared-Resource fingerprints, and process audit all pass. User editor PID `23260` remains the sole Godot process, responsive and untouched.
- 2026-09-02: Independent verification rejected the first handoff. The task returned to `In Progress`; the prior GREEN evidence is retained as history but is not acceptance evidence for the corrected implementation. One writing executor is correcting the findings below without redesign, branch changes, cleanup, or interference with user editor PID `23260`.
- 2026-09-02: Correction core milestone is GREEN. Derived inspection now ignores temporary summon states without authored Lab identity and still projects player-only/enemy-only unready configurations through the shared preparation and production Simulation path. Relic edit/preset state contains only identity/content/stacks and rebuilds zero charges/roll plus production initial counters; zero-player primary identity and Save→TryLoad→Restore are valid. Combat/Elite/Boss/Summon enemy flags are additive with an unreferenced non-Boss fallback, and same-cell movement is an explicit non-mutating rejection. Fresh low-concurrency build is `0 warnings / 0 errors`; expanded `BattleLabCoreContractSmoke` exits `0`, including Gear Architect plus summon-Relic temporary-source coverage.
- 2026-09-02: Production Battle inspection and failure-transaction correction is implemented. The generic runtime snapshot now carries content/source/runtime identities, final Damage/AttackSpeed/Reach/ControlResistance, Equipment instance identity, unit Trait contributions, team Trait presentations, and source-aware Status state for the authored SelectedUnit panel. Start, presenter creation/binding, fixed-step, and process paths clean simulation/config/content/scopes and even tree-attached unregistered presenters on failure; Lab flow catches defensively stop Battle. The expanded lifecycle contract covers throwing display, presenter, fixed-step, and process fixtures and remains GREEN; full correction verification is still pending UI/Frost/source-guard work.
- 2026-09-02: Responsive authored UI and the expanded real-input matrix are GREEN at `1280×720` and `1600×900`. Stable library recall, outside cancellation, same-cell rejection, persistent non-colour selection, explicit delete, three-slot choose-and-click Equipment plus fourth rejection/removal, PvE Equipment inapplicability, Relic editing, invalid seed, typed enemy filtering, side/all clear, preset load, fixed Start, scroll-reachable build/readiness, and the complete Battle HUD controls/Tactical HUD are covered by `BattleLabPlacementInputContractSmoke`.
- 2026-09-02: The formal `冰霜体系验证` behavior contract is GREEN after changing only the authored resistant-target preset cell. It proves two independent Rimebrand Equipment instances, active Winterbound AttackSpeed, both owners' momentum, two-source Frost, three-stack Freeze conversion, normal `6` versus `0.5`-resistance `3` ticks, and apply/stack/remove cues. A second fresh production `BattleScreen` run pauses and steps until two-source Frost, then uses real `Viewport.PushInput` selection and verifies the visible Status panel contains both Equipment source ids, stacks, and duration.
- 2026-09-02: Source and immutable-Resource correction gates are GREEN. `BattleLabContractSmoke` now inspects actual production sources through `ProductionSourceGuard`: no Lab branch in `BattleSimulation`, no concrete content ids in generic Lab/Battle runtime, no stable-id-prefix inference in typed indexing/preparation, and no production save API/file/schema access from Lab ownership. The core lifecycle fingerprint now includes every catalog definition plus `GD.Load<Resource>` roots for compiled Equipment, Relic, Status, and Trait authored paths. Focused visual evidence is rendered at both supported resolutions under `res://.godot/qa/battle-lab/` for Lab configuration/build and the production Battle HUD; only two pre-existing invalid-UID path-fallback warnings occurred during rendered capture. Fresh full correction verification and handoff remain next.
- 2026-09-02: Correction Stage 6 is complete and GREEN. A fresh low-concurrency build reports `0 warnings / 0 errors`; all seven focused Battle Lab contracts pass, including real source-aware Frost inspection; the complete seventeen-scene regression matrix passes in the mandated order after updating three stale Tactical HUD test/capture paths to the authored `ControlRow`; separate `CleanStartup --quit-after 8` exits `0`; actual source guards, expanded Resource fingerprint, production-save absence, `git diff --check`, rendered visual inspection, and process audit pass. The five `CONTENT_GATE_*` errors are that contract's expected failure probes, and the two invalid-UID warnings are pre-existing path fallbacks. User editor PID `23260` remains responsive and is the only Godot process. Status moved to `Awaiting Verification`; no archive or commit was performed.
- 2026-09-02: A fresh independent audit found finding #2 partially open: configuration inspection projected team `TraitPresentationSnapshot` values but did not expose the selected authored unit's formal `TraitContributionSnapshot` sources and values. Status returned to `In Progress`. The scoped correction is limited to DerivedProjection, configuration Inspector copy, and direct player-only/unready plus Equipment-contribution behavior contracts; prior broad GREEN evidence remains historical until focused correction verification passes.
- 2026-09-02: The focused inspection correction and follow-up evidence audit are GREEN. Configuration projection now carries each authored unit's non-temporary, team-matched formal `TraitContributionSnapshot`; the Inspector separates source-aware `单位贡献` from `团队档位` and keeps both visible in player-only/unready state. Core directly proves the published Equipment contribution instance/content/value and excludes temporary summon contributions. Placement real input proves `1000` Relic stacks with `MaxValue=int.MaxValue`, exactly three reachable UI Equipment slots plus domain rejection of `slotIndex=3`, and final attributes/Equipment/Trait source/team tier visibility after clearing the enemy. Long Inspector copy leaves ClearEnemy/ClearAll/Start inside both supported screen rects.
- 2026-09-02: Follow-up lifecycle/source evidence is also GREEN. `BattleLabInputContractSmoke` now accurately identifies itself as an authored source-surface contract rather than real input. The reusable stable-id inference guard covers terminal `Id`/`StableId`/`ContentId` property chains using StartsWith, Substring, slicing, Split, `IndexOf == 0`, and anchored/named Regex, with positive and negative sentinels while scanning the actual ContentIndex/Preparation paths. Real Pause→Continue resumes advancement; coordinator-owned Reset buttons rebuild clean running, paused, and terminal battles; and real Lab → main menu → Lab re-entry can start and return from a fresh battle without duplicate routing. The architecture document's `Persistence` heading now owns only its original persistence bullets.
- 2026-09-02: Final scoped verification is fresh and GREEN: low-concurrency build `0 warnings / 0 errors`; `BattleLabContractSmoke`, `BattleLabInputContractSmoke`, `BattleLabCoreContractSmoke`, `BattleLabPlacementInputContractSmoke`, `BattleLabBattleLifecycleContractSmoke`, and `BattleLabAppFlowContractSmoke` all exit `0`. Only the two previously recorded invalid-UID text-path fallback warnings appear. `git diff --check`, production-save absence, and user-process audit pass; PID `23260` remains responsive and untouched. Status returned to `Awaiting Verification` without archive or commit.
- 2026-09-02: Final independent verification accepted the implementation with no blocker. A fresh low-concurrency build reports `0 warnings / 0 errors`; all seven focused contracts (`BattleLabContractSmoke`, `BattleLabInputContractSmoke`, `BattleLabCoreContractSmoke`, `BattleLabPlacementInputContractSmoke`, `BattleLabBattleLifecycleContractSmoke`, `BattleLabAppFlowContractSmoke`, and `BattleLabFrostBehaviorContractSmoke`) run against the same fresh DLL and exit `0`; the specified seventeen-scene serial regression matrix exits `0`; and separate `CleanStartup --quit-after 8` exits `0`. The two pre-existing invalid-UID path-fallback warnings and Content's five expected failure probes are unchanged and accepted. Two independent reviewers accepted the result with no blocker. Final `git diff --check`, production-save absence, and PID `23260` sole-responsive-process audits also pass. The task is complete and archived without a commit, cleanup, reset, branch change, or control of the user's editor.

## Independent Verification Findings — 2026-09-02

1. Derived projection assumes every simulation unit maps to an authored Lab instance; HeroRule/Relic temporary summons can have an empty source instance and currently break projection. Ignore or explicitly represent temporary runtime units and add Gear Architect plus summon-Relic regression.
2. Configuration inspection is incorrectly gated by battle readiness. Single-side and other intermediate states must still project selected-unit base/prepared attributes, Equipment, Trait contributions/tiers, while retaining readiness failures through shared preparation/Trait capabilities.
3. Production battle inspection is incomplete. The `SelectedUnitPanel` read model must expose content, Lab/source, and runtime identities; final damage/attack-speed/reach/control-resistance; Equipment; unit Trait contributions plus team tiers; and source-aware Status stacks/duration/contributions without a Lab branch.
4. Selecting PvE must hide or disable the complete hero-Equipment editor and visibly say `不适用` before submission.
5. Relic stack input must not invent `999` as a business maximum; use the technical integer type ceiling while session legality remains positive-only.
6. Enemy flags are typed and additive: Normal derives from Combat encounter membership, Elite from Elite encounter membership, Boss and summon remain additive, and an otherwise unreferenced non-Boss PvE target falls back to Normal. Add an authored enemy filter and prove a non-empty Normal+Elite overlap from shared pools.
7. Preset/session Relics serialize only instance id, content id, and stacks. Battle charges, rolls, and counters rebuild from production initial state. Cleared-player/cleared-all unready configurations must Save→TryLoad→Restore, with primary hero optional only when no player exists.
8. The Frost built-in requires a true battle behavior contract proving two Equipment instances, active Winterbound attack-speed benefit, two-source Frost, three-stack Freeze conversion, 6-tick normal versus 3-tick 0.5-resistance Freeze, and apply/stack/remove cues; concrete ids remain preset/test data only.
9. Both Lab configuration and Battle HUD must be usable at `1280×720` and `1600×900`. Recompose authored containers/scroll ownership so libraries, board, inspector/build controls, start action, Status, five battle controls, and tactical HUD remain reachable; add rect/real-input and visual evidence.
10. `StartBattle`, presenter creation/binding, fixed-step, and process failures need transactional cleanup. A failure after simulation creation must remove the simulation, scopes, and even tree-attached presenters not yet registered; flow catch defensively stops battle; step/process failure must terminal-route once or reliably clean up. Add throwing fixtures.
11. Recall uses a stable panel/scroll drop zone even when search results are empty; invalid seed is a visible Chinese rejection; same-cell move is a non-mutating no-op/rejection; board selection has a persistent non-colour shape/state.
12. Real-input coverage must include explicit delete, cancellation/outside release, representative invalid feedback, three successful Equipment slots plus fourth rejection, enemy Equipment inapplicability, unready inspection refresh, and battle source-aware Status UI. Equipment remains choose-and-click; tests must not call it drag.
13. Replace self-referential token checks with production-path source guards that actually reject save APIs, concrete ids, prefix inference, and a Lab branch in simulation. Expand Resource fingerprints to catalog definitions and compiled Equipment/Relic/Status/Trait authored Resource paths rather than opaque scenes.

## Current State And Completion

Implementation and independent verification are complete. The authoritative final focused gate is the seven-scene set listed in the final Progress entry, including Frost behavior, run serially against one fresh DLL; the seventeen-scene regression matrix and separate CleanStartup gate also passed fresh. No verification or resume action remains.

The user's Godot editor PID `23260` remained responsive, was the only Godot process, and was never closed, foregrounded, attached to, or reused for tests.

## Completed Verification Record

The evidence below records the corrected 2026-09-02 handoff. Earlier rejected evidence remains only in the dated Progress history.

### Changed surfaces and ownership

- Authority: `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, and `docs/testcases/battle-lab.md` own the accepted product, runtime, isolation, and serial-QA contracts.
- Shared preparation: `src/Battle/BattlePreparationContracts.cs`, the Run preparation adapter/service, and pure Active Run validation retain production behavior while accepting neutral immutable preparation inputs.
- Lab core: `src/BattleLab/` owns compiled-content indexing, session/snapshot identity, formal/free placement transactions, derived projections, shared preparation adaptation, and isolated preset DTO/storage.
- Authored UI and routing: `scenes/ui/BattleLabScreen.tscn`, its authored card/cell templates, `src/UI/BattleLab*`, the main-menu entry, `AppScreenHost`, and `GameFlowCoordinator` own configuration, typed input, screen lifetime, production Battle/Report routing, and return to the unchanged session.
- Production Battle boundary: `BattleScreenController` and its authored scene expose pause/continue/fixed-step/speed/reset/return controls without adding a Lab branch to `BattleSimulation`.
- Authored validation content: `content/battle-lab/battle_lab_presets.tres` plus the generic non-zero-control-resistance development target and catalog/portrait entries provide the default/Frost scenarios without campaign-pool inclusion or concrete-id runtime dispatch.
- Contracts: seven focused Battle Lab scenes cover production-path guards, core/resource isolation, full UI input, placement input, lifecycle/determinism, app flow, and formal Frost behavior/source-aware UI; one focused rendered-capture scene provides visual evidence at both supported resolutions. Existing UI/content/combat regressions remain the broader compatibility gate.

### Execution evidence

- Initial RED evidence named six missing core capability groups and fifteen missing authored/routing/input tokens. Corrected GREEN markers are `BATTLE_LAB_CONTRACT_OK`, `BATTLE_LAB_INPUT_CONTRACT_OK`, `BATTLE_LAB_CORE_CONTRACT_OK`, `BATTLE_LAB_PLACEMENT_INPUT_CONTRACT_OK`, `BATTLE_LAB_BATTLE_LIFECYCLE_CONTRACT_OK`, `BATTLE_LAB_APP_FLOW_CONTRACT_OK`, and `BATTLE_LAB_FROST_BEHAVIOR_CONTRACT_OK`; every scene exits `0` on the final fresh DLL.
- `BATTLE_LAB_CORE_CONTRACT_OK` proves Run adapter parity, strict/permissive legacy formation semantics, compiled typed content, deep snapshots, explicit primary-HeroRule identity, exact placement, instance Equipment, team Relics, semantic preset distrust, same-seed result equality, nineteenth free-mode player preservation, a real save spy, and production user-data zero writes. Its immutable fingerprint roots include the authored project, catalog, every catalog definition, and loaded authored Resources behind all compiled Equipment, Relics, Statuses, and Traits.
- `BATTLE_LAB_PLACEMENT_INPUT_CONTRACT_OK` uses real `Viewport.PushInput` mouse/keyboard paths across the corrected interaction/responsive matrix. `BATTLE_LAB_INPUT_CONTRACT_OK` is deliberately limited to authored scene/routing/input-handler source surfaces and does not claim real input. Real input for configuration, lifecycle, entry/re-entry, and Frost Status inspection is owned by PlacementInput, BattleLifecycle, AppFlow, and FrostBehavior respectively. `BATTLE_LAB_BATTLE_LIFECYCLE_CONTRACT_OK` proves pause/continue/step/x1/x2/x4, coordinator running/paused/terminal Reset, return, terminal-once handling, unchanged configuration, determinism, runtime cleanup, and throwing start/presenter/step/process transactions. `BATTLE_LAB_APP_FLOW_CONTRACT_OK` proves main-menu → independent Lab → production Battle → production Report → unchanged Lab configuration plus Lab exit/re-entry and a fresh second battle. `BATTLE_LAB_FROST_BEHAVIOR_CONTRACT_OK` proves the complete formal Frost chain and real-click source-aware Status UI.
- Final fresh `dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal`: `0 warnings / 0 errors`.
- All seven focused contracts—Contract, Input, Core, Placement, Lifecycle, AppFlow, and Frost—ran serially against that same fresh DLL and exited `0`.
- Fresh existing serial regression: ScalableAuthoring, ProjectComposition, Relic, AbilityStatus, EffectKernel, Content, Fixture, FormationDeployment, Gameplay, MovementPresentation, AlphaRun, Ui, GameUiInteractionReliability, DeploymentInputHeroSelection, SemanticPresentation, GameUiVisualLanguage, and VisualHierarchy all exit `0` in the mandated order. Content's five engine-error lines are its expected failure probes. `UiSmoke` reports `screens=14`; responsive Tactical HUD path assertions now follow the authored `ControlRow`.
- Separate `CleanStartup --quit-after 8`: exit `0`. `git diff --check`: exit `0`.
- `BATTLE_LAB_VISUAL_CAPTURE_OK` produced and visually inspected six images under `res://.godot/qa/battle-lab/`: Lab configuration/build and production Battle HUD at `1280×720` and `1600×900`.
- Actual production-path source guards report no `BattleLab` branch in `BattleSimulation`, no production-save API/file/schema access from Lab ownership, no concrete content ids in generic Lab/Battle runtime code, and no stable-id prefix inference in content indexing/preparation. The broader Content contract reports `source-guard=21-families-data-driven`.
- Production `meta.json`, `settings.json`, and `active_run.json` were absent before and after final verification. The focused core test also fingerprints `.tmp` variants and proves no incremental production user-data change.
- User editor PID `23260` remained responsive with its original start time; it was the sole Godot process after tests and was never controlled or reused.

### Accepted risks and closure

- No known contract blocker remains. Two pre-existing invalid resource-UID warnings fall back to their valid text paths, and the Content contract's five named engine errors are expected failure probes; both known sets were unchanged in final verification.
- The repository is intentionally very dirty from multiple confirmed workstreams. Verify behavior and scoped paths; do not infer Battle Lab ownership for unrelated modified/untracked files.
- Two independent reviewers accepted the final implementation and evidence with no blocker. The task requires no further verification command sequence and is closed.
