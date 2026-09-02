# Mature Combat Build Systems Migration Test Cases

## Phase 0 — Governance And RED Contracts

Phase 0 establishes executable names for the missing product contracts without implementing production behavior or changing gameplay values. `MatureCombatBuildSystemsRedContractSmoke` must compile and exit non-zero with `MATURE_COMBAT_BUILD_RED_EXPECTED`; each missing capability must appear once as `MATURE_COMBAT_BUILD_RED_GAP [id]`. An unexpected crash, compile error, missing diagnostic, or an existing completed-platform gate turning red is not the intended RED state.

The RED source-evidence probes are phase-entry gates, not final behavioral acceptance. In the owning implementation phase, replace each satisfied token probe with direct state/ordering/rollback assertions through the production public boundary before marking it GREEN. Do not make the scene pass with aliases, dead declarations, fixture-only implementations, or concrete-id branches.

### Attribute rollback and source identity

- Create one unit attribute set from immutable base values and apply additive, multiplicative, and override contributions in deterministic order before the attribute clamp.
- Apply equal contributions from two distinct `CombatSourceRef` instances. Refresh/replace one source, remove it by its authoritative handle/source, and prove the other contribution remains exactly once.
- Prove two units and two Battles sharing authored definitions never share mutable modifier state; teardown restores zero handles without changing the definition fingerprint.
- Cover typed constant, source/target attribute, invocation value, population count, and Trait value magnitudes with explicit snapshot-versus-live behavior.

### Battle-local typed attack, Status, and death events

- Publish immutable battle started/completed, attack declared/landed, Status applied/stack-changed/removed, unit defeated, and killed facts from the authoritative mutation points.
- Distinguish defeat from credited kill and retain source/owner/target, Battle identity, tick, sequence, chain, and effective-value facts.
- Reverse subscription/enqueue order and prove deterministic event/commit order. A listener may enqueue later bounded work but cannot synchronously re-enter the resolving mutation.
- Victory, defeat, timeout, abort, replacement, exception, and disposal leave zero subscribers and pending events. No global Autoload EventBus participates.

### Mature Status stacking

- Cover aggregation by source, aggregation by target with multi-source attribution, and independent instances with separate timers.
- Cover stack cap/overflow, duration refresh, periodic reset, ordinary/strong/non-dispellable dispel, removal reason, death policy, granted state tags, and sourced Attribute rollback.
- Control resistance modifies authored Freeze duration through the documented compiled rule; action authority reads the disabled state tag rather than a concrete Status id.

### TFT-style Trait breakpoints

- Derive a stable team contribution snapshot using authored deployed/persistent/temporary/duplicate/extra-contribution policies.
- Select exactly one ordered breakpoint, remove the previous breakpoint source, and apply the next through ordinary shared primitives without a Trait-id switch.
- Cover values below the first tier, exact bounds, tier downgrade, Equipment/emblem contribution, and a value of `30` without requiring thirty bodies.

### Hero-owned Equipment

- A concrete persistent hero instance owns three authored slots; two heroes equipping the same definition retain independent item instance/source identity.
- Equip, save/load, Battle-project, remove, and replace one item without leaking contributions to another hero or mutating shared Resources.
- Failed equip/replace/save is atomic. Temporary units cannot own persistent Equipment.

### Reactive Relic counters

- Subscribe each Relic only to required typed events. Cover Battle and Run counter ownership, threshold, consumption, reset, duplicate-instance isolation, and deterministic registration order.
- Static and reactive sources use ordinary Attribute/Effect primitives. The fixed legacy modifier enum is migration-only and cannot be the final extension route.
- All Battle completion paths dispose subscriptions/counters; Run counters persist only when their authored reset policy permits it.

### Unified hero roster and population

- The starting hero and later recruits share one persistent roster-instance/formation/equipment contract and each cost one population.
- Cover current population below/at/above ordinary `10`, explicit above-cap sources, and hard physical ceiling `18`; every deployed unit occupies one legal cell.
- Compile the authored initial-population fact independently from initial roster grants. Production uses compatibility value `7`; compilation rejects values below the complete starting roster or above ordinary `10`, and new Runs publish `7` without inventing reward cadence.
- Temporary units consume free cells but no persistent population and cannot keep battle alive after all persistent player heroes are defeated.
- v2/v3 migration preserves every identity, health fact, item, exact cell, and at least the former seven-body deployable capability; a larger migrated roster raises current population only through `10`, while an over-`10` roster rejects only the active Run and preserves Meta and Settings.

### Independent tactical loadout

- Run state owns exactly two equipped command ids independent of every hero. Each Battle starts with exactly three shared tactical points.
- Cover authored costs one through three, cooldown, maximum use, target/effect preflight, optional gold, and reset on the next Battle.
- Any failure consumes no points, gold, cooldown, use, or partial mutation; no third slot, point regeneration, hero-owned command mana, or command child requirement remains authoritative.

### Frost / Freeze production vertical slice

- One hero-owned production Equipment gains AttackSpeed only from its owner's AttackLanded events.
- Attacks apply shared-target Frost with correct source attribution; the authored threshold consumes/changes Frost and applies timed Freeze.
- Freeze grants the disabled state, obeys control resistance, and removes/rolls back cleanly. One authored Trait breakpoint modifies the path through shared primitives.
- Immutable active/stack/removed presentation cues are visible. Same seed yields identical result, event order, trace, and cleanup; generic services and UI contain no concrete Frost/Equipment/Trait/Status id dispatch.

## Phase 1 — Attributes, Sources, And Combat Pipeline

The first two capability probes are now direct production behavior contracts. The scene remains intentionally non-zero until later phases finish; the Phase 1 handoff result is exactly `MATURE_COMBAT_BUILD_RED_EXPECTED missing=7/9`, with only mature Status, Trait, Equipment, reactive Relic, unified roster/population, independent tactics, and Frost/Freeze still reported.

Phase 1 behavior coverage includes:

- immutable authored/compiled attribute definitions; independent Battle-owned sets; add → multiply → override → clamp; same-source slot replacement; different-source stacking; handle/source rollback; snapshot/live typed magnitudes; non-finite/cycle rejection; completion snapshots and zero mutable state;
- every production initial/temporary unit and direct movement fixture receives an explicitly Battle/test-scope-owned `BattleAttributeSet`; `BattleUnitState` has no detached fallback path;
- same-seed, same-owner Battle scopes reject foreign modifier handles and live magnitude contexts through a runtime-only scope-instance identity;
- failed modifier insertion or replacement restores modifier/handle mappings plus handle/application sequences, and `SetBaseValue` restores its exact previous override when projected-value validation fails;
- Attribute compilation rejects invalid source/target attribute enums, team-count kinds, and team-count/Trait team values outside `0..1`;
- Battle-local immutable events, deterministic subscription/calculation order, cached registration snapshots, queued reaction waves, transaction commit/rollback, budgets/depth/trace, and synchronous re-entry rejection;
- a listener that enqueues and then fails discards only its event's new reactions; a failing reaction discards all unexecuted/new pending work; later `Complete()` cannot execute abandoned work;
- authoritative-resolution LIFO validation is non-destructive, so closing the inner resolution and retrying the outer resolution recovers cleanly after an ordering error;
- live `Events` and `Trace` surfaces are cached read-only views and reject mutation without exposing their backing lists;
- construction-time production bindings through `BattleCombatBindingRegistry`, with real `AttackLanded` and `UnitDefeated` listeners enqueueing later work and all subscriptions/reactions cleared on natural completion;
- typed `BattleStarted` is the first production combat fact, before configured summons, floor-start mutations, and battle-start Ability/Status facts, while the established legacy event/digest order remains unchanged;
- one movement resolution commits every accepted unit cell before publishing the complete legacy move/typed `UnitMoved` batch; queued reactions observe the full same-tick fact batch rather than a partial movement state;
- real automatic periodic Ability → timed add-stack Status production flow: tick 1 apply, tick 2 apply plus stack change, tick 3 expiry removal, with `AbilityResolved` on both successful commits;
- concrete runtime-unit compatibility damage emits `UnitKilled`; unknown hazard/system damage emits only `UnitDefeated` while retaining system source attribution;
- Battle identity on production facts and zero Attribute/pipeline state after victory, defeat, timeout, abort, replacement, exception, disposal, and completion-listener failure.

## Phase 2 — Mature Status / Modifier Lifecycle

The mature Status probe is now a direct production behavior contract. The migration scene remains intentionally non-zero for later phases; the Phase 2 handoff result is exactly `MATURE_COMBAT_BUILD_RED_EXPECTED missing=6/9`, with only Trait, Equipment, reactive Relic, unified roster/population, independent tactics, and the production Frost/Freeze slice reported.

Phase 2 behavior coverage includes:

- immutable authored/compiled instant, timed, and permanent definitions; aggregation by source, shared target with complete multi-source attribution, and independent instances with separate timers;
- both overflow branches (`RejectNewStacks`, `RefreshDuration`), stack consumption and canonical overflow transition, plus removal snapshots that retain every pre-consume source contribution;
- all duration refresh branches (`None`, `Reset`, `KeepLonger`, `Extend`) and both periodic schedule branches (`KeepSchedule`, `ResetOnApplication`), including deterministic periodic execution and `WhileActive` cues;
- ordinary, strong-only, and non-dispellable categories; exact definition/source dispel; typed `Neutral`/`Helpful`/`Harmful` owner purge; death remove/persist policy; typed removal reasons, state tags, and action-disabled authority;
- `ControlResistance` applies `max(1, ceil(authored_ticks * (1 - clamp(resistance, 0, 1))))` to compiled timed control without concrete Status-id dispatch;
- each stack captures snapshot magnitudes exactly once while live magnitudes retain a complete narrow context for source/target attributes, invocation value, team counts, and Trait values;
- stable modifier projection identity is `Status instance × source × stack application sequence × modifier index`; refresh, source removal, and overflow preserve unaffected handles and deterministic Override winners, while failure restores exact handles/application order;
- duplicate `(Attribute, SlotId)` projections within one Status fail compilation with a direct diagnostic; missing source attributes, non-finite magnitude results, lifecycle/effect sink failures, and overflow-transition failures restore the complete pre-mutation state;
- canonical Status dependency publication preserves resource paths, rejects missing targets/cycles/collisions atomically, and participates in the deep package fingerprint;
- active combat reactions cover `OwnerIsSource` and `OwnerIsTarget` filtering, deterministic `PrimaryContribution` attribution, immutable typed event context, `EffectiveValue` invocation magnitude, and frozen explicit counterpart targeting (`source → target`, `target → source`);
- a target-aggregated multi-source instance registers one subscription per binding, retains it across partial source removal, transfers primary-source attribution, and disposes it after final removal, failed application, natural completion, exception, and throwing cleanup wrappers without pseudo-rollback;
- natural Battle victory with a permanent Status carrying a Removed binding produces `ScopeCompleted` removal fact/cue but no terminal gameplay effect; Effect, Ability, Status, Attribute, and combat scopes all finish at zero. Lifecycle, cue, or unsubscribe failures still finish cleanup and publish an `Exception` Status transition.
- production Time Stop and permanent damage multipliers retain their accepted values and ordering. Phase 2 Frost/Freeze fixtures prove shared-target Frost, threshold consumption, timed Freeze, action-disabled tag, control resistance, canonical reachability, cues, and rollback; the Phase 5 production Equipment/Trait slice remains intentionally absent.

## Phase 3 — Batches 1–3

The unified-roster/population, hero-owned Equipment, and independent Tactical Command batches are direct production behavior contracts. The migration scene remains intentionally non-zero for later phases; after Batch 3 its exact result is `MATURE_COMBAT_BUILD_RED_EXPECTED missing=3/9`, with only Trait breakpoints, reactive Relic counters, and the production Frost/Freeze slice reported.

Batch 2 Equipment coverage includes:

- one canonical production Equipment definition and independent item scene, explicit Equipment/Relic classification, production-directory/reference symmetry, atomic package rejection, nested authored/compiled fingerprint sensitivity, and no inclusion in ordinary Relic reward/shop pools;
- authored three-slot capacity, empty Equipment state for new and legacy-migrated roster heroes, JSON clone/load preservation, and rejection of null entries, duplicate slot/instance identities, mismatched owner, fourth slot, unknown content, and Relic content;
- atomic equip, replacement, and removal through the Run application, including deterministic instance identity, same-definition instances on different heroes, save-failure rollback, and rejection of temporary/non-roster owners without persistence;
- Battle preparation includes only deployed persistent owners; reserve Equipment is omitted. Runtime projection changes only the matching owner Attribute set, preserves health ratio for MaxHealth changes, and removes every modifier handle on completion;
- the Army overview exposes authored Equipment names through the generic roster view model without concrete content-id dispatch;
- production source guards cover the Equipment stable-id family, and the production content, scalable-authoring, composition, formation/save, Relic, gameplay, full-tower, UI, input, and visual-hierarchy regressions remain green.

Batch 3 Tactical Command coverage includes:

- one canonical independent Tactical Command product with immutable definitions, independently instantiable scenes, canonical Ability references, deep fingerprints, focused-package path support, strict production-directory publication, and no concrete command-id or prefix dispatch;
- exactly two unique Run-owned command ids, an authored deterministic starter loadout, lossless v2/v3 hero-to-command compatibility mapping, and strict v4 rejection for missing, null, one-slot, three-slot, duplicate, or unknown ids without partial publication;
- exactly three Battle-owned tactical points, authored one-to-three-point costs, optional gold, cooldowns, maximum uses, explicit target/effect and summon-capacity preflight, and zero resource or partial mutation on every rejected preparation/commit path;
- a command effect anchor selected from living persistent player heroes independently of the starting hero. Starting-hero defeat does not disable commands while another persistent hero lives; zero living persistent heroes rejects without spending anything;
- idempotent zero-state cleanup on victory, defeat, timeout, abort, replacement, exception, and disposal, with the next Battle restoring exactly three points;
- an authored two-slot HUD, structured tactical-point/gold badges, current/maximum point segments, localized failure feedback, two distinct visible command identities, and a real viewport mouse path to the same typed activation boundary;
- hero selection and concrete hero scenes no longer present or own commands, hero `MaxMana` command authority is removed, Army overview and Battle report use independent Tactical Command terminology, and Ability-owned mana remains a separate permitted resource.

## Phase 4 — Trait Batch

Only the TFT-style Trait capability becomes a direct behavior contract in this batch. The expected handoff result is exactly `MATURE_COMBAT_BUILD_RED_EXPECTED missing=2/9`, with only reactive Relic counters and the production Frost/Freeze slice reported. Production Trait publication is intentionally an empty graph until separately authored player-facing names, breakpoints, and contributions are approved.

Trait behavior coverage includes:

- immutable authored/compiled identity, semantic presentation, counting policy, ordered inclusive breakpoint ranges, deterministic fingerprints, gap acceptance, overlap rejection, duplicate modifier-slot rejection, and missing Trait dependency diagnostics;
- canonical focused-package reachability plus authored/compiled deep fingerprints, strict production directory validation, atomic rejection, and a valid empty production Trait graph;
- the production source guard requires the data-driven `trait` family, rejects a quoted concrete `trait_*` id, and preserves prefix-only, incomplete-id, and established semantic allowlist near misses without hard-coded scanner branches;
- typed hero, Equipment/emblem, and explicit-extra contributions without tag, stable-id, or prefix inference; deterministic deployment-only, persistent, temporary-unit, duplicate-content, team, and explicit-extra counting policies;
- a recomputed Run snapshot that observes fixture Trait value `30` without thirty bodies and does not add mutable Trait state to schema v4;
- one Battle team scope selecting at most one tier, applying its Attribute loadout through a source identity containing scope/team/Trait/breakpoint/owner, and replacing or downgrading the exact previous source without disturbing another scope or source;
- failed tier application restores the exact Trait snapshot, tier/source handles, Attribute values, and deterministic handle/application sequences; a direct production Tactical/Ability world transaction summons a temporary unit with a fixture Trait contribution, observes the activated tier and sourced Attribute handles, then fails late and restores a byte-equal running Battle before a deterministic retry matching a clean Battle;
- victory, defeat, timeout, abort, replacement, exception, and disposal clear every Trait tier and modifier handle idempotently;
- immutable typed presentation facts retain authored semantic identity, value, active range, display style, and localized text, and convert to the existing `TraitBadge.Bind(SemanticFact)` boundary without UI product inspection.

## Phase 4 — Reactive Relic Batch

Only the Reactive Relic capability becomes a direct behavior contract in this batch. The expected handoff result is exactly `MATURE_COMBAT_BUILD_RED_EXPECTED missing=1/9`, with only the production Frost/Freeze slice reported. Existing production Relic classification and values remain unchanged.

Reactive Relic behavior coverage includes:

- immutable authored/compiled static Attribute bindings, Battle-start shield/summon actions, Victory outcomes, typed reactive counter declarations, manual Effect bindings, canonical publication, and deep authored/compiled fingerprints without concrete Relic-id or stable-id-prefix dispatch;
- compiled threshold-Effect fingerprints cover trigger kind/event, every typed condition field, complete target parameters, ordered steps, limits, and presentation with culture-invariant canonical values and explicit unknown-type failure. Null modifier/outcome collections and invalid counter-source enums return failed compile/batch reports instead of escaping as runtime exceptions;
- narrow Relic target policies preserve current army, hero, empty-slot, and formation-adjacent behavior while removing `RelicBattleModifierKind` from the final production extension route and preserving every current production value exactly;
- explicit Attribute stack and Battle-start repeat policies preserve ordinary per-stack shield/Add/Multiply/adjacency behavior and victory-gold scaling. Empty-slot linear bindings aggregate by Relic stable id plus binding id, use the stable first registered instance as their single source/handle owner, and scale Add as `base × empty slots × total stacks` or Multiply as `1 + empty slots × (base - 1) × total stacks`; once-per-Battle bindings use the same key/source rule and execute only once across stacks and duplicate instances. Direct coverage proves registration ordering, exact checkpoint handle identity, cleanup, and authored/compiled/package fingerprint sensitivity;
- each counter declares a stable counter id, Battle or Run ownership, one typed population/alive/attack/death source, positive threshold, threshold consumption, and reset policy; compilation rejects duplicate ids, incompatible scope/reset pairs, unsupported events, invalid thresholds/consumption, and non-manual threshold effects with path-indexed diagnostics;
- each Relic instance subscribes only to the combat event kinds required by its compiled counters. Registration and threshold execution are deterministic by Relic registration, counter declaration, source, and priority order; duplicate instances of the same definition retain independent source identity and counter values;
- population and alive counters observe authoritative deployed/persistent/temporary/alive facts through an immutable Battle query; attack and death counters filter the correct typed source/target team fact without inspecting content ids or presentation text;
- threshold effects enqueue through the bounded combat reaction queue and execute only after the publishing mutation. A late effect failure restores Relic counters, subscriptions, pending work, Effect state, combat history boundaries, and deterministic sequences so retry matches a clean Battle;
- Battle counters reset and disappear on every completion path. Run counters project only when their authored reset policy permits it, are authenticated by the Battle→Run transition, and persist in schema v4 as an explicit counter-id/value collection rather than overloading stacks, charges, or rolls;
- every legal Run/Battle counter value is canonical only in `0 <= value < Threshold`; direct Run activation, Battle preparation, current-v4 persistence, and transition authentication reject threshold-equal or extreme positive residues without overflow or partial publication;
- schema-v4 clone, JSON round-trip, validation, new-Run creation, reward acquisition, Battle preparation, successful settlement, failed-save rollback, and reload preserve valid Run counters exactly; null, duplicate, unknown, negative, Battle-owned, reset-incompatible, or definition-mismatched persisted counters reject without partial publication or schema bump;
- victory, defeat, timeout, abort, replacement, exception, and disposal dispose all Relic subscriptions, Battle counters, projections, and instances idempotently. Run counters do not advance on an unauthenticated, failed, non-victory, or unpersisted transition;
- current Aegis Standard, Blood Chalice, Clockwork Seed, Commander Map, Crimson Mail, Duelist Seal, Field Rations, Gilded Contract, Last Banner, Lone Crown, Soul Lantern, and War Drum retain their accepted shield, lifesteal, summon, adjacency, health, damage, empty-slot, and victory-gold behavior with no balance drift.

## Phase 5 — Production Frost / Freeze, Density, And Presentation

The final capability gate must be a direct production-package and real-`BattleSimulation` behavior contract; source-token presence is not acceptance. It becomes GREEN only when all nine mature capabilities pass and the production Frost slice is independently authored, reachable, deterministic, reversible, and readable.

Production vertical-slice coverage includes:

- one independently instantiable hero-owned production Equipment subscribes only to its concrete owner's typed `AttackLanded` facts. Every accepted owner attack applies one authored AttackSpeed-growth Status to that owner and one authored Frost Status to the immutable event target; another unit's attack cannot advance either path;
- a lethal `AttackLanded` fact preserves a surviving Equipment owner's momentum application but cannot apply Frost/Freeze, emit `StatusApplied`, or emit `OnActive` to the defeated event target after death cleanup. The generic eligibility query is evaluated when the deferred reaction executes rather than by concrete Status or Equipment id;
- two distinct Equipment owners attacking one target produce one target-aggregated Frost instance with deterministic multi-source contributions. The authored threshold removes/consumes the complete Frost snapshot and applies timed Freeze; Freeze grants `state.action_disabled`, drives ordinary action authority, and resolves duration with the target's compiled `ControlResistance`;
- one ordinary production TFT Trait reaches its breakpoint from typed contributions and modifies the same attack cadence through the existing sourced Attribute projection. The breakpoint applies and removes with no Trait-specific Frost branch or content-id dispatch;
- production package publication validates Equipment→Status and Equipment→Trait dependencies, authored paths, independent item-scene symmetry, deep fingerprints, and shared-Resource immutability. Missing/unregistered dependencies reject the complete package atomically;
- Equipment-instance source attribution rejects any direct or overflow-reachable Status modifier using a source-Attribute magnitude at compilation, while the identical Status graph compiles under Owner attribution. The forged compiled runtime batch remains covered separately for complete rollback;
- immutable applied/stack-changed/overflow-removed/Freeze-active/Freeze-removed lifecycle snapshots and cues retain source contributions, semantic icon, localized label, tick, reason, and deterministic order. Selected-unit/HUD/report consumers bind only immutable Status/Trait facts and do not inspect concrete product ids;
- `BattleFloatingCueContractSmoke` instantiates the real `BattleScreen`, authored overlay, and independently authored cue template under `RealmTheme`; real ticks must expose signed damage/healing numbers plus non-color Chinese active/stack/removed wording through five typed Theme variations. Real viewport board input must still open exact Freeze duration details. Active cue nodes/Tweens never exceed the authored controller cap, and replacement, terminal completion, and tree exit each leave zero cue nodes/Tweens;
- same seed and inputs yield the same attack order, combat facts, Frost contributions, Freeze duration, cue sequence, trace, digest, and terminal transitions. Failed status application or enclosing Battle transaction restores exact Attribute/Status/Trait/combat/presentation boundaries; victory, defeat, timeout, abort, replacement, exception, and disposal leave zero Equipment subscriptions/modifier handles and zero live Status/Trait state;
- a real viewport mouse path deploys/selects and opens details at ordinary `10` and physical `18` player units. Every occupied cell is legal and unique; status/trait/action facts remain visible without overlap or clipping at the supported desktop viewport, and critical state is not conveyed by color alone;
- a deterministic high-density Battle includes `18` player bodies, enemies, and temporary units up to the physical free-cell limit. Record setup/step/completion time, maximum living/total bodies, ticks, event/reaction/cue counts, and cleanup; measure before optimizing, and change no targeting/initiative fairness without a separate confirmed decision.

`Phase5BattleDensityContractSmoke` is the focused density gate. It fills the complete `10×6` physical board exactly once with `18` persistent player heroes in the authored deployment zone, `12` ordinary enemies, and `30` temporary units in every remaining cell. All persistent heroes carry the production Frost Equipment so the measurement exercises typed combat events, Equipment reactions, Status batches, Trait projection, and presentation cues rather than an empty simulation loop. Two runs with seed `0xD3517` must match outcome, ticks, body maxima, event/reaction/cue counts, immutable result/event/trace/cue fingerprint, and zero-state terminal transitions. The catastrophic-regression guard is setup `<= 5000 ms`, cumulative non-terminal stepping `<= 15000 ms`, and terminal-step cleanup `<= 5000 ms`; record observed timings in the active task before considering optimization.

Run serially with the Godot 4.7 Mono console executable:

```text
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
Godot --headless --path . tests/EffectKernelContractSmoke.tscn
Godot --headless --path . tests/AbilityStatusContractSmoke.tscn
Godot --headless --path . tests/RelicContractSmoke.tscn
Godot --headless --path . tests/ProjectCompositionContractSmoke.tscn
Godot --headless --path . tests/ScalableAuthoringContractSmoke.tscn
Godot --headless --path . tests/MatureCombatBuildSystemsRedContractSmoke.tscn
Godot --headless --path . tests/BattleFloatingCueContractSmoke.tscn
```

Phase 0 expects the first five completed-platform gates to exit `0` and the final RED scene to exit `1` with all nine named gaps. After implementation, the same scene must exit `0` only after its token probes have been replaced by focused behavioral assertions. Preserve the complete serial regression and isolated startup commands from `content-platform-architecture-migration.md`; run them before every phase handoff.
