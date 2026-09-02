# Hero Roster And Independent Tactics Redesign

Status: Documentation Confirmed — Implementation Deferred

## Goal

Replace the accepted persistent `one irreplaceable hero + six soldiers + three soldier reserves + hero-owned command mana` product contract with one coherent hero-roster autobattler contract:

- every persistent recruitable player combat character is a hero-grade roster unit;
- a run begins with one starting hero who is the first team member rather than an irreplaceable commander category;
- ordinary run progression grows current population toward an endgame ceiling of ten, while explicit build sources may exceed ten up to the eighteen-cell physical ceiling; every persistent hero costs one population and exact reserve capacity remains unresolved;
- limited battle intervention is an independent tactical-command loadout with two equipped command slots and three shared tactical points at the start of every battle;
- temporary units are optional authored outputs of particular heroes, abilities, relics, items, tactical commands, encounters, or floor rules and are not a universal persistent soldier roster, manpower, replenishment, or recruitment system.

The resulting product remains a Godot 4.7 .NET single-player tower-climbing real-time autobattler. Its differentiation is a readable `10x6` spatial battlefield with pathing, body blocking, enemy/floor preview, formation, data-driven floor rules, and limited player-authored tactical intervention rather than a permanent hero-led army hierarchy.

## Confirmed Product Contract

### Persistent Roster

- Player-facing persistent roster units use one hero identity and one recruitment/equipment/progression contract. No player-facing `soldier`, `troop roster`, `manpower`, `detachment`, or `replenishment` category remains.
- The selected starting hero occupies the first roster entry. It may remain an explicit starting choice and meta unlock target, but it does not own run-wide rules merely because it was selected first, does not own the tactical command resource, and its defeat alone does not terminate a battle.
- Every persistent hero costs exactly one population regardless of tier or rarity. Current population grows during a run toward an ordinary endgame ceiling of `10`; the exact starting curve, tier names/odds, reserve capacity, and recruitment economy remain unresolved.
- The existing eighteen legal player cells are the physical full-deployment ceiling. Explicit heroes, abilities, equipment, or relics may spend real build opportunity to exceed the ordinary `10` ceiling and high-roll toward `18` deployed persistent heroes.
- Recruitment presents hero-grade candidates with a legible acquisition stage, immediate use, replacement condition, and build value. A formation distinguishes cores/payoffs, engines/enablers, and functional/bridge heroes; asset count alone is not a reason to promote a simple body, and not every hero is marketed as a core.
- Equipment remains attached to concrete roster heroes. Relics remain run-level authored content. Shared immutable definitions and mutable instance state retain their existing ownership boundary.
- A battle is lost when no living non-temporary player roster hero remains, not when the initially selected hero is defeated. Temporary units cannot keep a battle alive after the complete roster team is defeated.
- Individual defeat is terminal for that battle unless separately authored resurrection says otherwise. Cross-floor health, defeat, replacement, rest, and recovery should preserve the current soldier-like roster consequences where they remain coherent; this redesign does not invent permadeath, wounds, or a new casualty economy.

### Temporary Units

- Temporary units are a supported runtime/content primitive, not a mandatory composition layer.
- A valid run may contain no temporary-unit source and must remain fully playable.
- Temporary units may be produced only by an explicit authored source such as a hero ability, status/relic/item effect, tactical command, encounter, Boss phase, or floor rule.
- Temporary units do not consume persistent roster or reserve population, do not receive individual persistent equipment or save state, and do not introduce a general recruit/replace/replenish flow. They occupy actual free legal cells and cannot exceed physical occupancy.
- Their source attribution, deterministic join/death facts, targeting, occupancy, cleanup, report evidence, and lifecycle remain explicit. Existing independent concrete summon scenes remain reusable.

### Independent Tactical Commands

- Tactical commands are independently authored content and are never discovered through, owned by, or validated as a component of a concrete hero scene.
- Each run carries a tactical loadout of two equipped command stable ids. Every battle starts with three shared tactical points; authored commands may cost one to three points and may be used repeatedly subject to cost, cooldown, usage limit, target, and effect preflight.
- Failed activation consumes no tactical points, currency, cooldown, or usage count and commits no partial effect.
- Command selection is a run-level decision independent of starting-hero selection. The first playable slice may bind a deterministic starter loadout and expose replacement/upgrade only through already-authorized run rewards; it must not invent a command deck, hand, draw pile, rarity economy, or third command slot.
- Command content should primarily change targeting, position, tempo, protection, cleansing, devices, or authored temporary reinforcement. A universal best raw-damage/heal button is not an acceptance target.
- Player-facing terminology changes from hero mana/hero command to tactical points/tactical commands. Pause and battle-speed controls remain observation controls and do not consume tactical points.

### Spatial And Presentation Contract

- The existing deterministic `10x6` logical grid, eighteen legal player cells, one-unit-per-cell occupancy, path cost, body blocking, engagement goals, floor-rule legality, enemy preview, deployment input, and movement presentation remain authoritative unless a direct contradiction is documented in this work item. Eighteen is now the confirmed physical population-build ceiling rather than merely excess spatial choice.
- The battlefield now communicates a hero team rather than one gold commander plus subordinate soldiers. Selection, near/ranged markers, responsibilities, health, statuses, targeting explanations, animated portraits, semantic palette, and battle-report attribution remain readable for every roster hero and temporary unit.
- `Despot's Game` remains a reference for small-pixel silhouettes, dense-combat readability, impact/VFX hierarchy, and damage/healing feedback; it is not authority for anonymous disposable roster units.
- `The Last Flame` remains a reference for hero-roster build grammar; `Commander Quest` for battlefield topology; and `Thronefall` for immediate visual promise and preparation-to-resolution cadence. No external game is implementation authority and no external asset/runtime dependency is allowed.
- The long-term role, archetype, population-chassis, count-basis, opportunity-cost, and `10 / 18 / 30` rules live in `gameplay-design/combat-build-framework.md` rather than being duplicated here.

## Authority Impact

- Update `AGENTS.md` product identity only as a short stable top-level description; do not add implementation progress or content inventories.
- Rewrite the player-facing hero/soldier, deployment capacity, recruitment, failure, temporary-unit, tactical-command, UI terminology, Alpha-content, solo, conversion, and non-goal clauses in `gameplay-design/tower-autobattler-core.md`. Remove obsolete contracts rather than layering deprecation prose.
- Route and maintain the focused combat-build/population authority through `gameplay-design/README.md` and `gameplay-design/combat-build-framework.md`.
- This documentation turn does not modify `system-design/`, runtime ownership, code comments, resources, persistence, UI implementation, or QA. Those surfaces require a later handoff after the separately owned content-platform architecture work completes or explicitly yields its implementation boundary.
- `work-items/active/content-platform-architecture-migration.md` remains an active, separately owned architecture task at `Phase 5 Verification Corrections`. This redesign neither cancels, supersedes, absorbs, nor changes that task's ownership or correction scope.

## Deferred System Design Handoff

The confirmed player rules imply a future Run/Content/Battle/UI/Persistence contract migration rather than a local roster rename. The notes below are handoff topics, not synchronized system authority and not implementation authorization in this documentation turn.

- Content owns independently instantiable roster-hero, enemy, temporary-unit, item, relic, command-adapter where still required, and other concrete scenes plus immutable definitions.
- Project composition publishes the complete validated Catalog plus compiled Ability/Status/Relic/Tactical Command/Project graph atomically. No runtime subsystem recompiles a second authority.
- Run owns ordered persistent hero instances, current population, deployed cells, an eventually authored reserve contract, equipped tactical-command ids, tactical loadout validation, recruitment/replacement, rewards, recovery, and versioned save projection.
- Battle receives an immutable snapshot of the current legal deployed formation up to the eighteen-cell physical ceiling, optional authored temporary-unit templates/sources, the two-command compiled loadout, and the shared maximum tactical points. Battle alone owns mutable per-battle health/action/status/cooldown/usage/tactical-point state and returns immutable results/transitions.
- UI binds typed roster, deployment, command, report, and failure view models and emits typed intent. It does not infer hero/soldier identity from stable-id prefixes, prose, resource paths, scene-tree discovery, or concrete content switches.
- Existing deterministic effect ordering, fixed-tick simulation, lifecycle cleanup, resource immutability, exact-once Run transitions, authored scene composition, and responsive input contracts remain mandatory.

## Deferred Implementation Outline

All phases below are intentionally deferred. They may begin only after the active content-platform architecture task completes or provides an explicit non-overlapping handoff, and after `system-design/tower-autobattler-architecture.md` is reconciled by the future implementation owner.

### Phase 0 — Governance, Atomic Publication, And Baseline

- Synchronize system authority with this confirmed player contract only after the active content-platform task completes or hands off.
- Preserve the current dirty worktree and integrate with the completed architecture result without rollback, cancellation, scope transfer, or competing edits.
- Treat the content-platform task's atomic publication, Boss timeline, deep fingerprint, neutral timing, and invalid-package gates as its own prerequisite evidence rather than work transferred into this redesign.
- Capture a current low-concurrency build and focused baseline. Tests that assert the obsolete product contract become expected RED only after the new authority-specific contract test names the intended change.

Exit 0: authority is internally consistent, the complete content/project package publishes atomically with no duplicate runtime compilation authority, outstanding final-audit findings are closed, and the unchanged foundation is green before product migration.

### Phase 1 — Unified Hero Content And Run Schema

- Introduce an explicit typed player roster-hero category/contract that does not overload old `IsHero` leader-death semantics. Reclassify current recruitable player content without concrete-id dispatch.
- Separate starting-hero eligibility from general recruitable-hero eligibility.
- Replace separate selected-hero state plus soldier roster with one ordered hero-instance roster, explicit current population, deployed formation entries/cells, and a reserve contract whose exact capacity must be confirmed before implementation.
- Introduce versioned active-run schema v4 or the next valid version. Migrate only when lossless and unambiguous; otherwise reject the incompatible active run safely while preserving valid Meta and Settings. Never silently discard roster members or invent a reserve/population mapping before those unresolved rules are confirmed.
- Preserve stable content ids where possible. Legacy `hero_`/`soldier_` prefixes or source paths may remain only as compatibility identity and may not decide runtime category, behavior, presentation, or recruitment.

Exit 1: content validation and run creation/recruitment/save round-trip operate on one hero roster; capacity and migration failure semantics are explicit; shared resources remain immutable.

### Phase 2 — Formation, Battle Setup, And Defeat Semantics

- Generalize formation evaluation/commit from one hero plus six soldier slots to a current-population-limited roster formation with an ordinary endgame ceiling of `10` and a physical ceiling of `18`, while retaining atomic move/deploy/replace/swap/withdraw, exact rollback, and one-save semantics. Reserve-dependent operations wait for the reserve contract.
- Update deployment, battle snapshot preparation, setup validation, simulation team-termination, reports, transition application, and deterministic tests so the first-selected hero has no special survival authority.
- Keep temporary units outside persistent formation/save capacity and ensure their presence cannot postpone defeat after all roster heroes are dead.

Exit 2: the complete current-population formation is previewed and consumed exactly up to the physical ceiling; battle outcome depends on living roster heroes as a group; deterministic movement/report/transition contracts remain green.

### Phase 3 — Independent Tactical Command Product

- Move command loadout ownership out of concrete hero scenes and hero rules into independently authored, compiled, validated Tactical Command content referenced by Run state/project composition.
- Replace hero mana naming and ownership with two equipped command ids and three shared tactical points per battle. Preserve typed preflight, atomic failure, cooldown/use/gold/effect semantics and deterministic tracing.
- Remove hero-component command requirements and concrete hero command discovery. Existing command scenes/resources may be adapted as independent authored command assets when their effects remain useful; no command effect remains implicitly selected by hero id.
- Update HUD, details, selection/start flow, battle events, report metadata, localized failures, semantic icons, and tests to use tactical-command terminology and ownership.

Exit 3: any valid hero roster can equip any valid independent command loadout; two slots and three points work without hero ownership; invalid references reject publication/run load; failures remain atomic.

### Phase 4 — Recruitment, UI, Content Reclassification, And Temporary Sources

- Make hero selection, recruitment, army overview, deployment, rewards, shop/event/rest, selected-unit facts, and battle reports present one persistent hero category with no player-facing soldier hierarchy.
- Preserve authored scenes, animated portraits, semantic facts, real mouse/keyboard/gamepad paths, supported resolutions, and exact typed activation.
- Retain temporary-unit creation only for explicit existing or deliberately adapted authored sources. Remove any universal soldier fallback/conversion/capacity/replenishment copy and tests.
- Reconcile the initial content pool pragmatically: hero-grade content needs a real build hook; simple bodies may remain enemies or temporary-unit templates. This task does not require every available visual asset to become a recruitable hero.

Exit 4: the full run flow recruits, manages, deploys, inspects, and reports heroes consistently; a no-temporary-unit build completes; explicit temporary sources remain deterministic and lifecycle-safe.

### Phase 5 — Integrated Verification And Handoff

- Update automated/manual QA and source guards for obsolete hero/soldier, hero-command, hero-mana, special-leader-death, capacity, save, and UI assumptions.
- Run static checks, low-concurrency build, focused content/run/battle/command/persistence contracts, complete serial headless regression, isolated startup, and only the minimum required visual/input capture.
- Perform Godot-specific code review and independent read-only architecture/scope verification. Correct findings within confirmed scope and re-run affected gates.
- Update this document with changed surfaces, evidence, remaining risks, and exact resume/verification commands. Archive only after all acceptance criteria pass.

## Deferred Implementation Scope

- Accepted gameplay authority synchronization; system authority remains deferred.
- Content category and validation changes required for one persistent hero roster.
- Run state, schema, migration/failure semantics, current population, recruitment, eventually confirmed reserve rules, formation, reward/recovery, and save changes.
- Battle setup, termination, optional temporary-unit lifecycle, result, report, and transition changes.
- Independent Tactical Command authoring, compilation, loadout, resources, HUD, effects, targeting, failure, and terminology.
- Existing UI/view-model/test adaptation required to make the confirmed flow playable and verifiable.
- Compatibility with the separately completed content-platform architecture result after its explicit handoff.

## Non-Goals

- No universal persistent soldier, troop, detachment, manpower, replenishment, or casualty-economy system.
- No requirement that every hero, item, relic, command, or run use temporary units.
- No command deck, draw pile, hand, command rarity economy, third command slot, regenerating tactical points, or unconfirmed tactical-point meta progression.
- No new status family, resistance model, elemental reaction set, ability-slot progression, hero promotion tree, item redesign, relic redesign, map-region redesign, or final balance pass.
- No direct-control action combat, city building, PvP/shared shop, mandatory duplicate merging, or multiplayer.
- No broad visual redesign, new asset generation, donor-runtime transplant, runtime external path, or change to the accepted RealmTheme/portrait/input/resolution foundations beyond terminology and roster needs.
- No cleanup, rollback, commit, deletion, stable-id rename, or formatting of unrelated dirty-worktree changes.

## Deferred Execution Constraints

- Remain on `main`; do not create or switch local development branches.
- No implementation executor is active for this redesign. A future write-capable execution owner must be assigned only after the content-platform task completes or hands off because both tasks overlap content identity, persistence, deterministic battle termination, command ownership, and system authority.
- The future executor reads this complete work item, `AGENTS.md`, the then-current gameplay/system authority, the completed architecture task's final handoff, and matching GodotPrompter architecture/resource/component/C#/save/testing skills before writing.
- Inspect current diffs before every overlapping edit. Preserve all completed content-platform, report, theme, portrait, deployment, input, and UI work unless this confirmed contract directly requires adaptation.
- Prefer authored `.tscn`/`.tres` content and focused typed runtime services. Do not construct whole UI/content trees in code or restore concrete-id switches.
- Use static inspection before builds, low-concurrency `dotnet build -maxcpucount:2 -v:minimal`, serial Godot/headless tests, and no unnecessary editor/import launches.
- Never terminate or manipulate user Godot/editor/game processes. Idle build-server shutdown is allowed after verification.
- Invalid content/run/save data fails before publication or mutation with structured evidence. Do not relax validation merely to preserve obsolete fixtures.
- If implementation requires changing one-population-per-hero, the ordinary `10` ceiling, the physical `18` ceiling, two command slots, three tactical points, the non-universal temporary-unit contract, or inventing an unresolved starting curve, reserve, tier, recruitment, trait-breakpoint, achievement, or economy rule, stop, mark this task `Needs Discussion`, and return to the user.

## Deferred Implementation Acceptance Criteria

- Player-facing authority contains one coherent hero-roster/independent-tactics contract. System authority is reconciled only during the later non-overlapping implementation handoff.
- A new run starts below its mature formation size with one chosen hero in one unified roster, recruits hero-grade units at one population each, grows toward the ordinary `10` ceiling, and supports explicitly built above-`10` formations without exceeding eighteen legal cells. Reserve acceptance waits for its confirmed capacity and economy.
- The starting hero has no special battle-survival or tactical-resource ownership. Any surviving roster hero can keep battle active; no temporary unit can keep it active after the roster team is defeated.
- Tactical commands are independently authored/published and two equipped commands share exactly three full tactical points at battle start. Valid repeated use and every failure path are deterministic and atomic.
- A complete no-temporary-unit build and at least one explicit temporary-unit build both complete representative deterministic paths.
- Formation legality/evaluation/commit/save rollback, current-population limits, the eighteen-cell physical ceiling, battle setup, grid occupancy, pathing, targeting, deaths, temporary cleanup, reports, rewards, exact-once transitions, and run termination pass focused contracts.
- Content/project publication is one atomic dependency-aware transaction retaining compiled products for runtime consumers; invalid complete packages publish nothing; deep immutability and lifecycle gates pass.
- Active-run incompatibility never erases valid Meta or Settings. Any v3 migration is lossless or rejected explicitly; no roster member is silently discarded.
- Recruitment, hero selection, Army overview, deployment, HUD, selected-unit panel, rewards, report, and relevant tooltips use Chinese hero/tactical-command terminology and remain operable by real mouse plus keyboard/gamepad paths at `1600x900` and `1280x720`.
- Low-concurrency build, focused tests, full serial headless regression, clean startup, source/diff guards, Godot code review, and independent read-only verification pass with evidence recorded here.

## Progress

- 2026-08-31: User reviewed the proposed comparison set and identified `The Last Flame` as the build-depth reference, `Despot's Game` as the primary pixel-art/mid-scale-combat reference, `Commander Quest` as a battlefield-map reference, and `Thronefall` as an attraction/cadence inspiration rather than a directly compatible mode.
- 2026-08-31: User rejected a forced universal small-soldier layer and confirmed that persistent recruitable player characters should be heroes, while temporary units remain optional authored content.
- 2026-08-31: User confirmed tactical commands and their repeat-use resource must be independent of every hero.
- 2026-08-31: The earlier `five deployed + three reserve` capacity baseline was superseded by run-grown population: every persistent hero costs one population, ordinary endgame growth targets `10`, and explicit population builds may high-roll toward the eighteen-cell physical ceiling. Exact reserve capacity is reopened and unresolved. The two independent command slots, three shared tactical points, and non-mandatory temporary-unit rules remain confirmed.
- 2026-08-31: User confirmed a role hierarchy of cores/payoffs, engines/enablers, and functional/bridge heroes; a five-part archetype grammar; and population as a cross-archetype chassis with separate expansion, scaling, and settlement responsibilities. Detailed elemental/status families remain future design.
- 2026-08-31: User established `30` only as a possible hidden single-trait-value achievement. It is neither thirty physical bodies nor a routine balance breakpoint; exact breakpoints, achievement details, and secret transformations remain deferred.
- 2026-08-31: Documentation synchronized through `gameplay-design/combat-build-framework.md`, the gameplay routing index, the core population/capacity clauses, and `work-items/active/combat-build-population-framework.md`. No system-design, implementation, resource, persistence, UI, test, QA, or separately owned architecture-task surface was changed.
- 2026-08-31: Main Agent audited `main` and found a large overlapping uncommitted content-platform migration. The user retained that task's separate ownership and narrowed this turn to non-overlapping documentation governance only.
- 2026-08-31: Documentation synchronization completed only for the stable project identity in `AGENTS.md` and the confirmed long-term player rules in `gameplay-design/tower-autobattler-core.md`. No `system-design`, code, resource, save, UI, QA, test, asset, build, Godot, or migration work is authorized or claimed by this redesign turn.
- 2026-08-31: Changed surfaces for this turn are `AGENTS.md`, `gameplay-design/tower-autobattler-core.md`, this activity task, and restoration of the separately owned content-platform task's status/progress wording. The temporary overlapping edits made to `system-design/tower-autobattler-architecture.md` were precisely removed without reverting the architecture executor's existing work.

## Resume Condition

Implementation remains deferred. Continue unresolved population/build discussion through `work-items/active/combat-build-population-framework.md`. Resume implementation only after `work-items/active/content-platform-architecture-migration.md` reaches a completed handoff or its owner explicitly yields a non-overlapping boundary, and after the starting curve, reserve contract, and recruitment rules required by the chosen implementation slice are confirmed. The future owner must first inspect that final architecture result and current dirty diff, then reconcile `system-design/tower-autobattler-architecture.md` with this confirmed player contract before changing code, resources, active-run schema, formation, battle termination, tactical commands, UI, or tests. Do not infer that the architecture task was cancelled or that its remaining findings transferred here.

## Verification Handoff

Documentation handoff only:

- Confirmed now: one persistent hero roster; starting hero is the first team member rather than a special survival/resource owner; one population per persistent hero; ordinary growth toward `10`; explicit-build physical ceiling `18`; reserve capacity unresolved; exactly two independent tactical-command slots with three shared tactical points per battle; optional temporary units only from explicit authored sources and constrained by actual free cells.
- Changed now: focused combat-build/population authority, gameplay routing, and the core player-facing population/capacity clauses; no system or implementation surface.
- Remaining handoff: starting population/growth curve; hero tier taxonomy/odds; reserve capacity; recruitment economy; normal trait breakpoints; system ownership and content categories; project publication integration; hero eligibility/content reclassification; active-run schema and safe migration/rejection; population/formation; battle setup/termination; independent command authoring/runtime/HUD; recruitment and all roster UI; high-density readability/performance; report terminology; temporary-unit source/lifecycle evidence; automated/manual QA and full verification.
- Verification for this turn is limited to documentation source scans and `git diff --check`. No build or Godot result belongs to this documentation-only handoff.
