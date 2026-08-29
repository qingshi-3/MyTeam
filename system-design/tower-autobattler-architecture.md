# Tower Autobattler Architecture

Status: Accepted

## Responsibility

This document owns the project layers, scene/resource contracts, runtime-state boundaries, donor-asset boundary, and acceptance architecture for the complete Alpha.

## Layer Ownership

```text
Content scenes and read-only definitions
        ↓
Run application state and commands
        ↓
Deterministic battle simulation
        ↓ semantic battle events
Presentation, animation, audio, and UI
```

- Content owns authored identity and configuration, never mutable run truth.
- Run application owns the current hero, roster, reserves, items, currency, route, rewards, unlock handoff, and save snapshot.
- Battle simulation owns combat legality, targeting, movement, timing, damage, effects, death, objectives, and outcome.
- Presentation consumes runtime facts and must not invent damage, victory, rewards, or progression.
- UI submits typed commands and renders view models; it does not mutate simulation collections directly.

## Independent Content Scene Contract

Every concrete hero, soldier, enemy, and item is a separate `PackedScene` under a focused content directory. Each scene must instantiate successfully in a minimal preview/test host without a battle level, tower screen, HUD, or autoload-specific parent.

### Unit Scene

Each unit scene has one unit root plus instanced component scenes, for example:

```text
UnitRoot
├── VisualRoot
│   └── AnimatedSprite2D
├── HealthComponent
├── CombatStatsComponent
├── TargetingComponent
├── MovementComponent
├── AttackComponent
├── AbilityComponent (optional)
└── UnitAnimationComponent
```

- A read-only `UnitDefinition` resource stores stable id, Chinese display name, tags, base stats, icon/visual references, and designer-facing descriptions. Scene ownership belongs only to its catalog entry.
- Runtime HP, cooldowns, targets, status effects, and position live on the instantiated nodes or battle state, never in the shared definition.
- Hero-specific behavior is supplied by focused components or abilities in the hero scene. Do not add a central switch over hero ids.
- Each independently instanced hero-command scene owns its display name, every tunable runtime parameter, optional currency cost, and runtime factory. Player-facing effect text is generated from those same authored parameters rather than stored as a second hand-written description. Command authoring validation rejects missing names/descriptions and invalid counts, scales, durations, divisors, tags, or costs before Registry publication; runtime algorithms do not carry fallback balance constants.
- Each hero scene explicitly authors maximum battle mana through its hero-rule component, and each concrete command scene explicitly authors its mana cost. Those same scene values feed selection, army overview, battle HUD, validation, and simulation snapshots.
- The common root API is narrow: bind runtime identity, expose definition and team, accept simulation/presentation events, and emit typed lifecycle signals.

### Item Scene

Each item is an independent scene with an `ItemContentRoot` and one or more focused effect-provider components. A read-only `ItemDefinition` resource stores identity, presentation, rarity, tags, and price. Scene ownership belongs only to its catalog entry.

- Items used only as run modifiers still remain scenes so they can be opened, composed, and tested independently.
- Item behavior contributes typed modifiers, hooks, or commands through explicit interfaces. It must not find the battle or run by absolute node path.
- No central item-id switch is authoritative. Adding an item means authoring a scene, definition, and catalog entry.
- Mutable stacks, ownership, charges, and per-run rolls belong to run state, not the shared scene definition.

### Components

- Reusable behaviors are separate component scenes with one responsibility.
- Children signal upward; composition roots call methods downward; unrelated systems communicate through typed application events.
- Components do not use parent-chain traversal or hidden sibling names. Required collaborators are explicit exported references or parent wiring.
- Content scenes may vary components without subclass explosions.

## Catalogs And Loading

- Stable ids exist only on definitions. A catalog entry owns exactly one `PackedScene` plus the same definition resource referenced by that scene root; it does not repeat the id or store a scene path in the definition.
- An id-to-entry registry is allowed for lookup. Concrete-id behavior dispatch is forbidden.
- Stable ids are ASCII and are the save-game and cross-reference keys.
- Human-facing scene/resource filenames and display names use Chinese where practical.
- Missing ids, scenes, definitions, or required components fail visibly with low-noise diagnostics. There is no silent placeholder for core content.
- Content discovery and validation happen before a run begins; combat hot paths do not scan directories.
- Catalog validation is bidirectional: every catalog entry must match one concrete content scene on disk and every concrete content scene must appear exactly once. Ids, scene references, and definition references are unique.
- Unit and item content roots expose authoring validation plus explicit bind/activate/deactivate lifecycles. A bare host can add any concrete scene to the tree and process a frame safely without a game root.
- Registry creation owns one scoped engine logger across the entire pipeline: it installs before catalog/structural validation performs its first scene instantiation, remains active through the batched hidden-host ready/process pass and all cleanup, and is removed only after captured errors are merged. First-pass structural instantiate/free plus ready-pass instantiate, attach, ready, process, detach, exit, and free errors all reject Registry publication, including errors that report without throwing or occur only once.
- Activation receives an explicit binding context for semantic battle events, seeded randomness, command submission, and modifier registration. Content must not discover `/root`, `CurrentScene`, groups, parent chains, or cross-root `NodePath`s.
- Item effects register and unregister symmetrically. Mutable ownership, stacks, charges, and rolls live in a separate `ItemInstanceState`, never the item definition or provider scene.
- Activation publication is transactional: failure restores the bound state. Deactivation clears local state before notifying collaborators, so cleanup remains stable even when an external sink fails.
- Validation returns a structured report. Any core error rejects run creation and makes headless validation exit non-zero; logging alone is not a gate.

## Battle Runtime

- Combat advances on a stable simulation cadence independent from animation frame rate.
- Units use a small explicit state model such as deploying, seeking, moving, attacking, casting, recovering, disabled, and defeated.
- Targeting, movement, attacks, abilities, floor rules, items, and hero rules communicate through typed commands/events and focused services.
- Simulation resolves truth before presentation plays the corresponding cue.
- Player hero rules and run-item formation modifiers apply only to team 0. Unit-authored adjacent auras remain local to the unit's own team.
- A seeded random source is owned by the run/battle session so saves and tests can reproduce content choices and combat setup.
- The first implementation may use grid steering instead of general-purpose navigation, but navigation ownership must remain replaceable behind a movement service.
- The deterministic grid-movement service separates stable strategic targets from engagement goals. At tick start it captures one living occupancy/terrain snapshot; every queued request completes ordered target/goal/next-step candidate generation from that snapshot before any reservation mutation. One central deterministic, battle-seeded and team-neutral arbitration then jointly assigns unique scarce goals and next cells, retries each request's next-best candidate after goal, destination, or dependency rejection, and commits the dependency-safe moves together. Actual path cost to a legal attack/heal position is the primary spatial cost, with authored role preference and target hysteresis secondary.
- Engagement goals are exclusive final ownership but are not global terrain walls. Current enemy cells remain impassable; friendly cells may be entered only through an accepted same-resolution follow chain. Same-cell commits, direct swaps, and dependency cycles are rejected. Blocked plans retain bounded leases before replan/retarget, and staging is allowed only when it improves distance or line access. Waiting age records real obstruction and resets after progress or completed action.
- `BattleSimulation` remains authoritative for immediate-action precedence, healing range/line access, cooldowns, damage, and death. Healers fall back to ordinary combat when no legal wounded-ally plan exists. Authoritative death is terminal without an explicit resurrection mechanic: ordinary healing, lifesteal, and subsequent status resolution cannot raise a defeated unit's health. Death releases the dead unit plus all dependent targets, goals, reservations, and queued intents in the same tick; next-tick, battle replacement, and disposal perform defensive stale-state cleanup.
- Hero-command activation is a simulation transaction. Legality, mana, gold, and summon capacity are validated before commit; failed activation publishes a typed reason and leaves all command resources unchanged. Mana is battle-local mutable state and never enters the run save DTO.
- Unit presentation owns animation lifecycle only. Each event batch is reduced per unit by `defeated > skill_cast/attack > hit > move/idle`; one bounded pending action prevents lower-priority cues from erasing attacks without creating an unbounded backlog. Attack, hit, and skill-cast cues are protected one-shots. Every authored frame is shown by fitting the full clip into a configurable action window rather than truncating its tail; defeat holds only after its full clip, then fades/hides idempotently. Presentation routes source cues and target cues explicitly and cannot recreate or re-defeat a hidden unit during synchronization.
- Spatial motion is a separate reusable presentation component with an explicitly bound unit-root target. `BattleSimulation` and `DeterministicGridMovementService` remain the sole owners of authoritative cells, legality, reservations, timing, move-event order, and digest; the motion component never writes simulation state or chooses a destination.
- `BattleScreenController` routes every accepted `move` event and its event `Cell` to the matching motion component in event order before per-unit cue arbitration. Cue priority may suppress a move animation cue for an action, but it cannot discard the spatial waypoint. Ordinary presenter synchronization refreshes health and animation facts without writing transforms.
- Initial presenters, summons, bind/reset, and explicit battle replacement use a snap API once. Normal movement uses a persistent process-driven interpolator with an editor-authored bounded waypoint queue, initial 1x/2x/4x segment durations of approximately `0.24 / 0.14 / 0.09` seconds, and an approximately 0.25-second maximum visual-lag budget in effective presentation time at supported frame cadence. Spatial progress applies one bounded monotonic ease-in/ease-out profile only along the current source-to-destination segment; it cannot overshoot, reverse, round a corner, skip an ordered cell center, or alter authoritative timing. Parent battle processing may enqueue events before child motion processing in the same frame, so fresh work defers pre-event frame delta. Per-frame delta and carried credit are bounded, at most one ordered segment completes per rendered frame, and completion/defeat/reset/teardown discard old credit. This preserves a visible sample after a hitch; it does not create competing transform Tweens or an unbounded callback backlog.
- `UnitContentRoot` explicitly coordinates motion and animation presentation. Motion publishes state, horizontal-segment, and normalized segment-progress facts upward; the composition root routes them downward to `UnitAnimationComponent`. The animation component owns only its character sprite's decorative step offset and bounded move phase, never the unit-root transform or readability children. The initial step lift target is approximately three pixels at mid-step and exactly zero at both endpoints. Snap, completion, defeat, bind, deactivate, reset, replacement, and tree exit clear that offset; pause freezes it with root progress. Components do not discover or call siblings through parent-tree traversal.
- The battle presenter propagates pause and speed changes to spatial motion and animation presentation. Pause freezes the current interpolation, character-only lift, and animation frame; resume continues from the same progress. Speed changes retime the active segment in place. Motion determines the `move`/`idle` base cue while action one-shots remain an independent priority layer. Re-entering the move base cue for consecutive segments retains or advances a bounded presentation-only phase when the resolved `SpriteFrames` animation supports it, avoiding an opening-pose restart without requiring a full clip at fast speed.
- Horizontal facing is presentation state and never enters the digest or action legality. `UnitAnimationComponent` owns the authored source-facing convention and mirrors only its `AnimatedSprite2D`; bind/reset supplies the team default. Motion updates facing when a real segment begins, while attack/heal presentation faces the event target from current visible positions. Vertical motion retains facing and defeat makes it terminal.
- The selected-unit panel renders the simulation's explicit action mode, target, and remaining cooldown as Chinese facts. It neither infers navigation decisions nor mutates simulation state.
- Defeat is terminal across both animation and motion presentation: it clears queued waypoints at the current visible position, restores the character-only decorative offset to zero, and then plays the defeat cue. Bind, deactivate, tree exit, presenter disposal, and battle replacement clear motion state, move phase, decorative offset, and processing so no stale waypoint or callback can slide, reveal, or recreate a unit. Pointer hit testing continues to use the visible presenter transform between cells.
- `BattleSimulation` owns per-runtime-id report statistics at the same mutation points that apply damage, shield loss, healing, attacks, terminal death, and summons. Effective damage is the capped health-plus-shield change, effective healing is the capped health restoration, damage dealt/taken mirror one authoritative fact, and kill credit requires the concrete source that caused the living-to-defeated transition. Initial units join at tick zero; a temporary summon captures the current join tick; the first terminal defeat writes its tick once. Attack actions increment once at the attack authority before splash/piercing targets, while a credited positive `HealLiving` result increments one effective healing event. Only a committed successful hero command increments the battle-level use count. Floor/environment sources remain unowned and statistics never enter the digest or decision path.
- `BattleResult` is an immutable post-battle boundary made from value snapshots with stable runtime/content/source ids and numeric facts, including join/defeat ticks, action/event counters, and successful command uses. It never exposes live `BattleUnitState`, Nodes, textures, or mutable shared resources. `RunApplication` consumes the snapshot's stable source id and final-health facts without changing the save schema.
- `BattleScreenController` owns fixed-step coordination and maps displayed `x1`/`x2`/`x4` modes to real-time simulation scales `0.8`/`1.6`/`3.2` while presentation receives the displayed mode. On the terminal tick it stops stepping, captures one result, locks battle interaction, preserves the final board for the authored hold, and owns one real-time fade Tween. Replacement, a new battle, or tree exit kills and resets that Tween; confirm may fast-forward the hold/fade but cannot bypass the report.
- `GameRoot` commits `CompleteBattle` and persistence exactly once when the terminal result arrives, stores an explicit pending post-report route, and does not change screens until the battle controller's fade completion signal. Repeated frames, input, callbacks, teardown, and report continue requests are idempotent. The report controller renders only its bound snapshot and emits one typed continue request.

## Tower And Floor Rules

- Tower regions, nodes, encounters, reward tables, and floor rules are authored resources or scenes and loaded through catalogs.
- Each floor-rule scene implements one lifecycle contract: validate setup, apply battle modifiers, react to fixed simulation ticks/events, expose readable preview text, and clean up.
- A started or partially started floor rule receives exactly one end callback on normal resolution, explicit battle replacement, scene teardown, or simulation failure. Battle simulation abort/dispose is idempotent.
- Encounters reference floor-rule scenes; the battle root does not switch on floor ids.

## UI And Composition Roots

- Main menu, hero selection, tower route, roster/deployment, battle HUD, reward/shop/event/rest, pause/settings, and result screens are authored scenes.
- Repeated cards, rows, slots, tooltips, and unit/item previews are reusable child scenes.
- Composition roots bind view models and route typed commands. They do not construct full control trees in C#.
- The desktop design viewport and default normal window are both `1600×900`; startup is explicitly windowed, resizable, non-fullscreen, and non-maximized. Godot `canvas_items` plus `expand` remains the stretch contract, and authored containers, anchors, size flags, and local scroll ownership keep `1280×720` usable without a second fixed layout.
- `UnitPortraitDefinition` is read-only presentation data owned one-per-concrete-unit and references existing local `SpriteFrames` plus an authored animation/frame, zoom, normalized offset, and optional horizontal flip. It does not own crop dimensions: each consumer-authored square `UnitPortrait` control supplies the visible clipping rect and display size. The catalog/presentation validator requires exactly 45 unique production portraits matching 8 heroes, 24 soldiers, and 13 enemies; missing or invalid production data fails visibly before publication.
- `UnitPortrait` is an authored reusable presentation scene. Each instance owns an independent UI `AnimatedSprite2D` bound to the definition's existing `SpriteFrames` and animation, starts from the authored frame, preserves zoom/offset/flip, plays at a calm UI speed, and pauses while not visible in the scene tree. It owns no stable-id lookup, selection, gameplay facts, mutable run state, battlefield sprite, battle animation state, shared-resource mutation, or content-id branching. Hero selection, recruitment, deployment lists, Army details, and battle-report rows bind the same definition through their existing view models and author only context-specific display size.
- `UnitChoiceCard` is the authored focusable recruitment template. It owns separate portrait, name, restrained responsibility/faction badges, prominent health/damage/reach stat blocks, neutral description, and contextual-meta regions and emits one typed stable-id choice signal. Recruitment uses its compact baseline. The general `ChoiceCard` remains authoritative for tower routes, items, shops, and other non-unit choices.
- Hero selection is an authored responsive master-detail composition. `HeroLibraryTile` owns only preview identity and emits a typed preview request; it never starts a run. `HeroDetailPanel` binds the focused hero, owns the single fixed primary action, and emits that stable id exactly once. The screen composition root instantiates only authored tile templates, changes grid density at the supported width boundary, and keeps preview focus separate from activation.
- `StatBlock`, `TraitBadge`, `ResourceCostBadge`, and `HeroAbilityPanel` are independently inspectable authored component scenes with typed binding APIs. Runtime code binds existing definition/command facts and may instantiate those templates; it must not build replacement trees, parse presentation prose, or dispatch on concrete hero ids.
- One read-only validated `SemanticIconCatalog` resource owns the mapping from stable presentation meanings to tintable source icons and optional Theme roles. `SemanticChip` is the authored icon-plus-Chinese-text component; fixed facts are authored scene instances and variable groups instantiate only that template. Controllers bind semantic keys and existing view-model values, while the catalog remains the sole icon resolver. Responsibility, faction, exact reach, stable combat facts, and tower-node type may be presentation categories; concrete content ids and per-screen path maps are forbidden.
- Hero selection, recruitment, deployment, Army details, battle reports, selected-unit facts, and tower-route choices reuse that catalog identity. Faction gameplay tags resolve to their matching faction semantic. Tower cards use their node-type icon as primary identity and bind risk as a separate semantic fact; the general choice-card size, focus, and typed selection contract remain unchanged.
- Recruitment owns `ScrollContainer > VBoxContainer` for exactly three unit cards and keeps its skip/conversion action outside that scroll owner. At the design baseline each card is approximately 148–150 pixels tall with an approximately 104–108 pixel portrait region; at `1280×720` only the choices scroll.
- A shared read-only army-overview view model is built from active-run state plus concrete content scenes. Route, reward/recruitment, shop, event, rest, and deployment bind the same overlay summary/drawer scene; the drawer never owns formation mutation. Its composition root supplies one focus scope: opening disables recursive focus below the modal, traps keyboard/gamepad focus inside the drawer, and closing restores the prior focus and scope state.
- Battle and deployment share one battlefield geometry/formation contract for board dimensions, hero anchor, six soldier anchors, and enemy start cells. Deployment-cell, unit-card, drawer-row, command-HUD, selected-unit, and readability widgets are authored reusable scenes instantiated by their owning presenter.
- Deployment submits atomic run commands for move, replace, swap, and withdraw. Only the run application mutates and saves the six-slot formation; it enforces the three-soldier reserve limit and rolls back the memory mutation when saving fails. Rejected or cancelled UI gestures never reach persistence.
- BattleBoard owns GUI hit testing for visible unit presenters and requests selection through their typed signal. Content units never discover global input or rely on `_UnhandledInput`; the selected-unit panel only renders the selected simulation state.
- `BattleReportScreen`, two-team comparison, responsive report unit card, empty state, icon-text/stat chip, and structured choice card are authored scenes using the shared Theme, semantic catalog, and independent `UnitPortrait`. One explicit `ScrollContainer` owns a two-column card grid at 1280×720 and 1600×900; the outcome, dimension/allegiance controls, and continue action remain outside it. Typed report view models derive zero-safe shares, remaining-health comparison, positive environment reconciliation, join-to-defeat/result active lifetime, DPS/HPS, stable rankings, and tied positive awards from the immutable result without mutating or falsely crediting facts. Dimension and allegiance controls replace the complete derived roster, preserve reachable mouse/keyboard/gamepad focus, and never alter post-report routing. Controllers instantiate authored templates and bind them; they do not construct replacement UI trees in code.
- The shared Theme owns semantic colors and type variations for health green, attack/damage red, mana blue, shield steel gray, healing teal-green, gold/hero identity gold, range light cyan, death/danger crimson, risk amber, body, and secondary text. Original monochrome tintable SVGs live under `assets/ui/icons/`; generated imports are never edited. Content definitions may supply optional authored icons, while unit presentation contracts and semantic role/loot icons provide stable fallbacks.
- Hero detail and battle-command HUD share `ResourceCostBadge`; mana and gold are structured fields from command content, never substring matches inside effect copy. Paragraph text remains neutral.
- `ChoiceCard` remains one focusable typed-choice control whose authored child hierarchy exposes icon, title, neutral body, and footer/meta independently. Tower route, item reward, shop, and other non-unit consumers retain that contract, including disabled state, tooltip, mouse, keyboard, and gamepad behavior; unit-choice presentation is intentionally separate.

## Persistence

- Save data is a versioned DTO written under `user://`; it contains stable ids and mutable values, not serialized live Nodes or shared Resources.
- Meta progress, settings, and an active run have separate ownership and validation.
- Loading validates schema version and referenced content before publishing state. Corrupt or incompatible active-run data fails safely without erasing valid meta progress.

## Donor Boundary

- `D:\godot\rpg` is read-only and never referenced at runtime.
- Copy only selected unit visual packages and required license/provenance material into this repository.
- Reuse `SpriteFrames` and the standard cue vocabulary (`idle`, `move`, `attack`, `skill_cast`, `hit`, `defeated`).
- Adapt a small animation presenter to the new unit API; do not transplant the donor's battle runtime, factory, logging stack, or tightly coupled animation mega-component.
- Units missing required cues are either excluded from the Alpha or receive an explicitly validated fallback.

## Verification Contracts

- Every cataloged unit and item scene loads and instantiates in isolation.
- No content behavior contains a central switch keyed by concrete hero, soldier, or item id.
- Shared definitions remain unchanged when two runtime instances mutate state independently.
- A deep definition fingerprint remains identical after two-instance mutation, battle resolution, and save round-trip.
- Fixed-tick seeded combat produces a stable digest and resolves without any presenter attached.
- Source guards reject concrete-id behavior branches and forbidden global scene-tree discovery.
- Headless UI smoke covers every authored screen, while three deterministic build paths complete the tower.
- A seeded headless run can progress from hero selection through the final result.
- Commander, carry/army, and solo builds each have at least one validated completion path.
- Low-concurrency .NET build, content validation, save round-trip, and Godot headless smoke tests pass before handoff.
