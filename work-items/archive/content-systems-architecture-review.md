# Content Systems Architecture Review

Status: Completed

## Goal

Produce a complete, evidence-based Chinese HTML review of the game's content architecture before content expansion begins. The review must start from the content the game needs to author—heroes, soldiers, enemies, bosses, abilities, relics, statuses, summons, floor rules, encounters, rewards, and progression—then map that content model to configuration, composition, deterministic runtime systems, persistence, presentation, validation, and migration work.

The HTML is a decision and review artifact for the project manager. It must make omissions, coupling, proposed boundaries, tradeoffs, and optimization order easy to inspect before any architecture implementation is authorized.

## Confirmed Solution

- Replace the earlier code-directory-first audit with a content-production-first audit.
- Treat the current implementation honestly:
  - independent unit and item scenes, stable content ids, catalog validation, deterministic battle simulation, navigation, formation, presentation separation, save ids, and contract tests are useful foundations;
  - there is no complete ability system, relic system, status system, trigger model, upgrade model, or reusable effect kernel;
  - current unit behavior, hero rules, and item modifiers are expanding optional-field bags;
  - current items are primarily static modifier bundles and do not yet constitute a full relic system;
  - hero commands are independent authored scenes but usually require a dedicated runtime class;
  - new content counts are blocked by validator assertions fixed to 8 heroes, 24 soldiers, 13 enemies, and 45 portraits;
  - `GameRoot`, `RunApplication`, tower generation, reward selection, and mechanic-specific battle branches are major expansion pressure points.
- Define the target content taxonomy and system ownership for:
  - unit identity and loadouts;
  - hero army identity, automatic abilities, and bounded manual commands;
  - soldier, enemy, summon, and boss abilities;
  - relic acquisition, rarity, uniqueness, stacking, charges, counters, rolls, ownership, persistence, and multi-scope effects;
  - typed statuses, combat resources, summons/corpses, tags/synergies, ability upgrades, boss phases/timelines, encounters, loot/recruitment tables, and meta unlock pools.
- Make abilities, relics, floor rules, statuses, and encounters separate product systems that reuse a typed effect kernel. Do not merge them into one generic system.
- Model reusable effect composition as explicit authored bindings:
  - trigger;
  - conditions;
  - target query;
  - ordered effects;
  - rate/recursion/usage limits;
  - presentation and reporting metadata.
- Separate the effect pipeline into:
  - pre-resolution modifiers;
  - authoritative state-changing effects;
  - resolved typed domain events;
  - reactive triggers that enqueue further invocations.
- Show how multi-effect and cross-content chains work, including deterministic ordering, source attribution, chain ids, recursion depth, per-step budgets, per-source intervals, repeated-edge protection, and explicit interruption evidence.
- Keep battle, run, and meta effect scopes separate. Battle effects cannot directly mutate run saves; sanctioned transition results bridge scopes.
- Use typed resource subclasses and processors rather than a string action DSL, reflection-driven scripting system, or unrestricted global event bus.
- Preserve deterministic battle authority for ticks, attacks, damage, healing, death, occupancy, movement, outcome, and effect ordering.
- Preserve independently instantiable `.tscn` roots for concrete heroes, soldiers, enemies, bosses, summons where concrete, and relics/items. Use `.tres` resources for immutable definitions, trigger/condition/target/effect specs, statuses, tables, campaigns, and presentation profiles.
- Keep a typed custom extension point for genuinely unique mechanics. A new atomic mechanic may require one processor, validator, description renderer, and focused tests; ordinary recombination of existing mechanics must not require edits across simulation, setup, UI, and save code.
- Build the review as a standalone, self-contained local page at `web/system-architecture-review/index.html` with inline/local HTML, CSS, JavaScript, and no remote dependencies. Do not modify or embed it in `web/game-mechanics-atlas/`.
- The page should support fast review through navigation, filters, status/risk labels, collapsible evidence, decision controls (`认可` / `需调整` / `暂缓`), reviewer notes, local persistence when available, and JSON export of the review state. These controls must not mutate project authority.
- The visual design should be a professional systems design console, not a generic card wall: compact navigation, clear hierarchy, restrained fantasy/pixel accents, diagrams where relationships matter, readable Chinese typography, and responsive behavior at desktop and narrow widths.

### Confirmed Visual-First Revision

- The first implementation was rejected because it presented the audit as dense prose, large tables, and text-filled boxes. Its factual inventory remains useful, but its information design is superseded.
- Rebuild the main experience around diagrams rather than narrative. The main canvas must use five substantive visual explanations:
  - executive system landscape and expansion blockers;
  - content-production factory for heroes, soldiers, enemies/bosses, abilities, and relics;
  - ability/relic/trigger/effect interaction and deterministic causal chain;
  - current-to-target runtime/configuration/data-flow comparison;
  - dependency-aware migration roadmap.
- Use authored inline SVG and compact CSS-native visual components where they communicate topology, flow, ownership, state, or dependency better than prose. Static meaning must remain complete without animation or color.
- Main-view prose is limited to short conclusions, labels, legends, and decision prompts. Existing hero, relic, unit, floor-rule, source-file, and detailed system evidence moves into on-demand drawers/details and an appendix.
- Tables may remain only where exact many-row evidence genuinely requires them; they must not dominate the primary review path.
- The first viewport must answer within one glance: whether the project is ready for content expansion, which foundations are reusable, which blockers must be resolved first, and what target architecture is recommended.
- Preserve the existing factual corrections, review decisions/notes, local persistence, safe reset, and JSON export while changing the visual hierarchy.

### Confirmed Architecture-Only Supplement

- The project manager rejected using hero, soldier, relic, encounter, or build-content completeness as evidence of architecture maturity. Concrete content may appear only as an extension pressure-test sample; it must not affect the architecture verdict.
- Supplement the existing visual review rather than creating a second page. Keep the main review diagram-first and limit prose to labels, short conclusions, legends, and decision prompts.
- Add three substantive architecture diagrams:
  - a mature-architecture pattern comparison mapping this proposal to publicly observable or documented patterns such as separate content products plus an ordered action/effect queue, immutable definitions plus runtime instances, run/meta state separation, catalogs, stable ids, and narrow typed extension points; do not claim knowledge of proprietary internal implementations;
  - a runtime lifecycle and ownership map from authored definition through compiled specification, runtime instance, domain event, sanctioned transition result, and save DTO, with explicit creation, ownership, unsubscription, cleanup, scope crossing, and migration responsibilities;
  - an architecture hard-contract map covering deterministic timing, lifecycle/subscription cleanup, content compilation and dependency validation, schema/save migration, causal observability, and architecture conformance tests.
- Reframe any existing hero, soldier, enemy, relic, ability-chain, or floor-rule evidence as implementation evidence or architecture pressure tests only. The opening verdict and architecture conclusions must not imply that unfinished content design makes the foundation immature.
- Distinguish four architecture states consistently: observed current foundation, proposed but unimplemented target, missing hard contract, and deliberate non-goal. Explicit non-goals include networking synchronization, a universal scripting language, and an ECS/DI rewrite unless future requirements change.
- Preserve the five existing diagrams, default-closed evidence drawers, review state persistence, safe reset, JSON export, offline behavior, and responsive containment.

## Architecture And Authority Impact

- Subsystems inspected: Content Pipeline, Unit, Ability, Relic, Effect, Trigger, Status, Combat Resource, Summon/Corpse, Tags/Synergy, Upgrade, AI/Targeting, Boss/Encounter, Tower/Run, Reward/Economy, Save/Progression, Battle Simulation, Presentation/UI, Reporting, Validation/Testing, and Authoring Workflow.
- Existing accepted gameplay and architecture documents remain authoritative descriptions of current accepted behavior.
- This task produces a review artifact only. Proposed target architecture in the HTML is not accepted runtime authority until the user completes final review and explicitly confirms an optimization plan.
- No gameplay, Godot runtime, content resource, scene, catalog, save schema, or accepted authority document changes in this task.
- The existing game-mechanics atlas and its active verification task remain separate and untouched.

## Scope

- Read-only evidence synthesis from current code, scenes, resources, accepted documents, tests, and content inventory.
- Current-system map with status, evidence, strengths, limitations, expansion consequences, and target ownership.
- Content capability matrix covering the present 8 heroes, 24 soldiers, 13 enemies/bosses, 12 items/relic candidates, and 5 floor rules at a useful review granularity.
- Explicit decomposition of the eight current hero identities into army rule, automatic/passive effects, manual command, resources, targets, and required primitives.
- Explicit migration classification of the twelve current items into relic behaviors and the missing relic lifecycle capabilities.
- Ability/relic/effect/status/resource/trigger boundaries and reusable-effect matrix.
- Multi-effect linkage examples, deterministic resolution timeline, and loop-safety model.
- Current-to-target data flow and authored-resource shapes.
- `keep in code` / `move to resource` / `compose as scene or component` / `delegate to service` matrix.
- Target workflow and acceptance test for adding a hero, soldier, relic, ability, status, floor rule, encounter, and boss mechanic.
- Architecture hotspots including configuration blockers, large responsibility hubs, open vocabularies, raw event/cue strings, save stability, content-query gaps, and test-governance gaps.
- Prioritized migration phases with dependencies, risk, exit criteria, and explicit non-goals against over-abstraction.
- Review controls and JSON export contained only within the HTML page.

## Non-Goals

- Implementing or refactoring the proposed architecture.
- Modifying gameplay balance, content counts, unit scenes, item/relic data, hero kits, battle rules, saves, UI, tests, or accepted design documents.
- Choosing final product rules that have not yet been accepted, including ability slot counts, upgrade cadence, relic inventory caps, duplicate policy, rarity curve, or new combat resources. The HTML must mark these as decisions rather than silently resolve them.
- Renaming existing stable `item_*` ids or performing a save migration.
- Creating a generic scripting language, behavior tree editor, visual node editor, ECS rewrite, dependency-injection framework, or global event-bus architecture.
- Publishing, deploying, or changing the existing Sites project.
- Cleaning unrelated dirty-worktree changes or moving completed historical work items during this task.

## Constraints

- Work on `main`; do not create or switch branches.
- Preserve all existing user and prior-task changes in the dirty worktree. At audit start there were 176 status entries and `web/` was untracked.
- Do not modify `web/game-mechanics-atlas/` or its hosting configuration.
- The HTML must work without a build step or network connection and must not load remote fonts, scripts, icons, analytics, or images.
- Use semantic HTML, keyboard-operable controls, visible focus, sufficient contrast, reduced-motion-safe behavior, and no horizontal page scrolling at supported widths.
- Prefer diagrams, matrices, and compact evidence to repeated prose. Do not use decorative charts that imply unsupported quantitative precision.
- Clearly distinguish current accepted behavior, observed implementation, recommended target, optional future capability, and unresolved product decision.
- File references may be exposed as secondary collapsible evidence, while primary writing remains at project-manager altitude.

## Acceptance Criteria

- `web/system-architecture-review/index.html` exists as one self-contained local page and opens without a build or network dependency.
- The opening section states the content-first conclusion and does not present the current item modifier system as a complete relic system or the current hero command implementation as a complete ability system.
- The page contains a navigable system map covering all systems listed in Architecture And Authority Impact, with current state, target responsibility, expansion risk, and priority.
- Ability, relic, effect, trigger, condition, target, status, resource, summon, upgrade, and presentation responsibilities are separately defined.
- A central diagram shows separate abilities/relics/floor rules/Boss content converging on the reusable typed effect kernel without erasing their different ownership and lifecycles.
- The effect model visibly separates modifiers, effects, resolved domain events, and reactive triggers.
- At least two concrete cross-system chains are shown end to end, including an overheal/charge/shield/cooldown chain and a death/corpse/summon chain or equivalent current-game-relevant examples.
- Deterministic ordering and loop safety include chain/source/owner/depth/sequence attribution, per-step budgets, rate limits, repeated-edge or recursion protection, and explainable interruption.
- The page explains battle/run/meta scope boundaries and the sanctioned transition between them.
- All eight current heroes are mapped to the target content model without inventing new accepted mechanics.
- All twelve current items are classified as relic candidates with their currently implemented behavior and missing lifecycle dimensions; the page does not invent unconfirmed duplicate, cap, or upgrade rules.
- Current unit/enemy/floor mechanic primitives are inventoried sufficiently to show which are reusable, hardcoded, missing, or candidates for typed effects/statuses/targeting policies.
- The page shows the exact content-expansion blockers, including fixed validator counts and the optional-field bags in unit, hero, and item components.
- The target Godot authoring shapes respect independent concrete scenes and immutable resource definitions.
- The report states measurable extensibility tests: recombining existing primitives is data/scene-only; adding a new atomic primitive has one narrow implementation path; unique mechanics use a typed extension point.
- The migration roadmap is phased, dependency-aware, and protects deterministic gameplay and existing saves. Each phase has an exit criterion and avoids a big-bang rewrite.
- Unresolved product decisions are grouped separately for user review instead of being treated as implementation facts.
- Reviewer decisions and notes can be changed with keyboard and mouse, survive reload when browser storage permits, reset safely, and export as valid JSON without altering repository files.
- The page is visually inspected at a representative desktop width and a narrow width. Required navigation, tables/matrices, diagrams, controls, notes, and export remain readable and operable without horizontal page scrolling.
- Focused static validation confirms no remote dependencies, no references into the existing mechanism-atlas application, presence of required sections/content terms, and valid embedded review-state behavior.
- The primary review path contains at least five materially different diagrams; they are not repeated cards with arrows or prose placed inside boxes.
- The executive opening, content factory, effect chain, runtime/configuration comparison, and migration dependency map are understandable from titles, shapes, connectors, grouping, and concise labels before opening any detail surface.
- Full hero/relic/unit/floor/source evidence remains reachable on demand but is not expanded by default.
- At desktop width, the first screen is an executive architecture briefing rather than a document introduction. At narrow width, each diagram has an intentional alternate layout or contained pan/scroll surface rather than page-level clipping.
- Architecture maturity is judged only from ownership, lifecycle, dependency direction, determinism, state isolation, versioning, validation, observability, and extension cost. Content quantity, hero quality, build variety, or balance completeness do not affect the verdict.
- The supplement includes three materially different architecture diagrams for mature-pattern alignment, lifecycle/ownership, and missing hard contracts. Public game references identify transferable patterns without asserting undocumented proprietary source structure.
- The lifecycle diagram assigns creation, runtime ownership, cleanup/unsubscription, sanctioned scope transition, persistence projection, and migration responsibility without allowing shared immutable resources to hold mutable state.
- The hard-contract diagram defines simultaneous-resolution timing, reentrancy/queue semantics, dependency validation gates, schema migration boundaries, causal traces, and conformance tests strongly enough to guide later implementation.
- Existing content mappings are visibly labeled as pressure-test evidence or appendix material, never as architecture maturity criteria.

## Progress

- 2026-08-30: Initial read-only audit inventoried 99 C# files, 63 general scenes, 62 concrete content scenes, 165 content resources, 8 heroes, 24 soldiers, 13 enemies/bosses, 12 items, 5 floor rules, 3 tower regions, 57 catalog entries, and 45 portrait resources.
- 2026-08-30: Initial audit found strong content identity/lifecycle, deterministic simulation, presentation separation, formation, navigation, save-id, and contract-test foundations, plus centralization and configuration risks.
- 2026-08-30: User rejected the code-directory-first framing because it omitted first-class ability and relic systems and did not start from future game content.
- 2026-08-30: Discussion reclassified the current hero rule/command, unit behavior, and item modifier implementations as incomplete content systems rather than sufficient abstractions.
- 2026-08-30: User confirmed that the final review must include multi-effect interaction through a shared reusable effect layer with deterministic event chains and safety boundaries.
- 2026-08-30: User authorized production of the complete HTML for final review.
- 2026-08-30: User corrected the review scope: concrete hero/relic/content maturity is unrelated to foundation maturity except as an extensibility pressure test. The architecture-only supplement and its three required diagrams were confirmed for execution.
- 2026-08-30: Activity document created; implementation is ready for the named executor.
- 2026-08-30: Executor completed the read-only evidence pass across all 8 hero rules and command runtimes, all 12 item scenes and definitions, the 24-soldier and 13-enemy mechanic inventory, all 5 floor rules, battle/run/save pressure points, and current validation/test contracts.
- 2026-08-30: Implemented the complete offline review page at `web/system-architecture-review/index.html` with content-first navigation, system filtering, hero/relic/primitive matrices, typed effect-kernel and scope diagrams, authoring and workflow guidance, phased migration, grouped product decisions, per-section review controls, local persistence, safe reset, and JSON export.
- 2026-08-30: Focused static verification passed: the embedded script parses, no remote dependencies or existing mechanism-atlas references are present, required content and architecture terms are present, and the page contains 23 filterable system-map rows and 7 review sections.
- 2026-08-30: Browser visual QA was attempted through the required Browser workflow, but the runtime reported no available browser instances after the prescribed troubleshooting check. Desktop/narrow-width visual and interactive inspection remains for independent verification.
- 2026-08-30: Independent review corrections applied: all 12 item rarities now use the project's `普通 / 优良 / 稀有 / 传奇` vocabulary; the scope bridge uses a responsive class instead of an overriding inline grid; the soldier primitive inventory includes the missing execute-threshold and low-health-damage units with the corrected 8-special/16-foundational split; the current direct on-death summon is distinguished from the optional future corpse chain; and the current 3-mana/1-cost hero contract is no longer presented as an unconditional rule for every future hero.
- 2026-08-30: Independent completeness review added the missing project/run composition-root target: `GameProjectDefinition` owns project-level resource references, campaign and run rules become authored definitions/tables, `RunApplication` remains a facade over cohesive formation/node/battle-preparation/reward/progression/persistence services, and `GameRoot` narrows to bootstrap plus screen routing with screen-local view-model binding. The page also inventories the corresponding current hard-coded resource paths, 15-floor/3-region structure, capacities, economy/recovery/reward/node formulas, page copy, and card/node paths without introducing new gameplay values.
- 2026-08-30: User rejected the first HTML presentation as an essay-like engineering inventory. The page's long tables, exposed prose, and text-filled boxes did not communicate architecture at leadership-review altitude.
- 2026-08-30: User confirmed the replacement rule: draw substantially more, write substantially less. The visual-first revision above supersedes the first page layout while preserving the verified factual inventory.
- 2026-08-30: Rebuilt `web/system-architecture-review/index.html` around five materially different authored inline-SVG explanations: executive readiness landscape, dual-shape content factory, typed effect kernel with two causal-chain rails, current-versus-target runtime/configuration comparison, and dependency-aware migration map. Primary-view prose is now limited to concise conclusions, legends, captions, and decision prompts.
- 2026-08-30: Moved the 23-system inventory, all 8 heroes, all 12 corrected-rarity relic candidates, unit/enemy/floor primitives, responsibility glossary, authoring workflow, and source/hard-coding evidence into seven default-closed appendix details. Preserved section decisions, notes, safe reset, JSON export, system filtering, local storage, and automatic migration of prior v1 review state into v2.
- 2026-08-30: Visual-first static validation passed: five SVG roots and five unique diagram titles are present and balanced; all seven evidence details are closed by default; embedded JavaScript parses with no duplicate HTML ids; 23 filterable system rows, 8 hero records, and all 12 corrected rarity mappings remain present; remote dependencies and mechanism-atlas references are both zero. Diagram/table overflow is owned by contained scroll surfaces, and no body-level overflow clipping rule is used.
- 2026-08-30: Completed the confirmed architecture-only supplement in the same visual review page. Added three materially different diagrams: a public/documented mature-pattern alignment map with explicit proprietary-source disclaimer, a runtime lifecycle/ownership swimlane from authored definition through migration, and a six-gate architecture hard-contract map.
- 2026-08-30: Reframed the executive verdict to use only ownership, lifecycle, dependency direction, determinism, state isolation, versioning, validation, observability, and extension cost. Concrete heroes, relics, units, floor rules, and example effect chains are visibly labeled as pressure-test evidence and do not affect architecture maturity. The four states `observed foundation / proposed target / missing hard contract / deliberate non-goal` are explicit; networking synchronization, a universal scripting language, and an ECS/DI rewrite are deliberate non-goals.
- 2026-08-30: Architecture-only static validation passed: 8 SVG roots parse as XML and match 8 figures/unique titles; embedded JavaScript parses; 64 HTML ids are unique and every `aria-labelledby` reference resolves; 7 evidence details remain closed by default; 9 review sections, 23 filterable system rows, localStorage v2/reset/export, and responsive containment remain intact; remote dependencies and mechanism-atlas references are zero; `git diff --check` and no-index whitespace checks completed without whitespace errors.
- 2026-08-30: Project manager accepted the architecture-only review as the migration authority. The accepted boundaries were transferred into `system-design/tower-autobattler-architecture.md`; the review HTML remains unchanged as decision evidence and this task is complete.

## Final Approval

- Approved result: the diagram-first architecture review, including separate product systems over a typed deterministic effect kernel, explicit runtime ownership/lifecycle, scope transitions, content compilation, persistence boundaries, and phased migration.
- Authority transfer: long-lived runtime contracts now live in `system-design/tower-autobattler-architecture.md`; this work item is historical evidence rather than an active source of runtime truth.
- Visual limitation retained: live desktop/narrow browser QA was unavailable during production, but the project manager reviewed and accepted the artifact before migration authorization.

## Historical Verification Handoff — Architecture-Only Supplement

Superseded by the final approval above. The following pre-approval handoff is retained only as execution evidence; it is not the task's current status or resume instruction.

Implementation is complete and awaiting independent visual verification.

Changed files:

- `web/system-architecture-review/index.html`: supplemented the existing visual-first offline page with architecture-only verdicting and three additional diagrams.
- `work-items/active/content-systems-architecture-review.md`: latest progress, static evidence, verification resume point, and status updated.

Eight-diagram primary path:

1. Executive landscape now judges only architecture and explicitly states that content quantity/completeness is not a scoring dimension.
2. Mature-pattern alignment maps publicly documented Godot and Unreal GAS concepts plus publicly observable roguelike/action-resolution patterns to transferable rules and this project's current/target gaps. It explicitly disclaims knowledge of proprietary source implementations.
3. Content-production factory keeps independently instantiable `.tscn` roots, immutable `.tres` specs, compilation, runtime instances, and save projections; content types are labeled as pressure inputs rather than maturity criteria.
4. Effect-interaction diagram retains the typed pipeline, determinism gate, and two chains as architecture pressure tests only.
5. Lifecycle/ownership swimlane assigns creation, authoritative ownership, typed subscriptions, unsubscription/disposal across every battle termination path, event queue drain, immutable transition application exactly once, save projection, migration, validation, and publication.
6. Runtime/configuration comparison retains the current central knot versus project definitions, a `RunApplication` facade, cohesive services, `ScreenRouter`, and screen-local view models.
7. Hard-contract map defines six conjunctive release gates: deterministic simultaneous-resolution/queue semantics, lifecycle cleanup, dependency compilation, schema migration, causal observability, and architecture conformance tests.
8. Migration dependency graph retains phases 0–5, dependencies, exit criteria, priorities, and deterministic/save protections.

State and evidence boundaries:

- The page consistently distinguishes observed current foundation, proposed but unimplemented target, missing hard contract, and deliberate non-goal.
- Networking synchronization, a universal scripting language, and ECS/DI rewriting remain explicit non-goals unless future requirements change.
- All specific hero, relic, unit, enemy, floor-rule, and chain evidence is labeled as pressure-test evidence or kept in the seven default-closed appendix details. It does not participate in the architecture verdict.
- Nine review sections expose keyboard-operable decisions and notes. localStorage schema v2 with v1 migration, safe reset, JSON export, offline behavior, and narrow-width contained scrolling are preserved.

Static verification evidence:

- SVG/figure structure: 8 opening/closing SVG roots, 8 figures, 8 unique diagram title ids; all 8 SVG fragments parse as XML.
- HTML/script integrity: embedded JavaScript parses; 64 ids are unique; every `aria-labelledby` reference resolves.
- Evidence containment: 7 details, zero open by default; 23 filterable system rows remain.
- Architecture supplement terms: four state labels, public-source disclaimer, creation/owner/unsubscribe/cleanup/transition/migration responsibilities, simultaneous-resolution and reentrancy semantics, dependency graph validation, schema migration, causal trace, and conformance gates are all present.
- Dependency and containment scan: zero remote dependencies, zero `game-mechanics-atlas` references, no body-level overflow hiding, and contained horizontal scroll owners remain.
- Whitespace validation: `git diff --check` plus no-index checks for both untracked target files completed without whitespace errors.

Remaining verification and limitations:

- The required Browser workflow previously found no available browser instances. No claim is made that the architecture-only supplement has passed live desktop or narrow-width visual QA.
- Resume by opening the page at representative desktop and narrow widths. Verify all 8 diagrams are visually distinct, labels/connectors are legible, the first viewport does not use content completeness in its verdict, public-pattern disclaimers are clear, lifecycle ownership/cleanup paths and six hard contracts are understandable before opening details, horizontal containment exposes every edge without page-level clipping, and all existing review/reset/export/persistence interactions still operate.
- If defects are found, modify only this HTML and handoff. Do not change Godot runtime/resources, authority documents, the existing atlas, hosting, or external state.

## Historical Resume Condition — Before Approval

Superseded by final approval and archive. No work resumes from this completed review task; architecture implementation is routed through `work-items/active/content-platform-architecture-migration.md`.

Read this activity document, `AGENTS.md`, `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, current source/content inventory, current `git status`, and the Sites building instructions. Preserve unrelated changes. Resume at the first incomplete acceptance criterion without modifying the Godot runtime, accepted authority documents, existing mechanism atlas, or external state.

## Superseded Verification Record — Text-Heavy Revision

Implementation is complete and awaiting independent verification.

Changed files:

- `web/system-architecture-review/index.html`: new self-contained Chinese architecture review page with inline CSS and JavaScript only.
- `work-items/active/content-systems-architecture-review.md`: progress, evidence, remaining verification, and status updated.

Content coverage:

- Content-first conclusion and the hero/soldier product contract, including explicit authored solo-build support.
- Navigable and filterable map for all scoped content, battle, run, persistence, presentation, validation, and authoring systems.
- Complete mappings for 8 heroes and 12 item/relic candidates without inventing new accepted mechanics.
- Current mechanic inventory for all 24 soldiers, 13 enemies/bosses, and 5 floor rules at primitive and full-content-list granularity.
- Separate ability/relic/status/trigger/condition/target/resource/summon/upgrade/presentation ownership, plus a shared typed effect kernel.
- Concrete overheal-to-charge-to-shield-to-cooldown and death-to-corpse-to-summon chains with deterministic ordering, attribution, budgets, rate limits, repeated-edge/recursion protection, and interruption reasons.
- Battle/run/meta scope bridge, current-to-target data flow, Godot scene/resource shapes, ownership decision matrix, measurable content-authoring workflows, phased roadmap, risks, exit criteria, and grouped unresolved decisions.
- Project/run composition root showing top-level project, campaign, run-rule, semantic/presentation references; cohesive run services behind a facade; and bootstrap/router versus screen-controller boundaries, paired with the current hard-coding inventory.
- Keyboard-operable native controls, visible focus treatment, reduced-motion support, local-storage persistence with failure fallback, safe reset confirmation, and JSON export.

Static verification evidence:

- Embedded JavaScript parsed successfully with Node (`SCRIPT_PARSE_OK`, 5,187 script characters).
- Remote dependency scan: `REMOTE_MATCHES=0`.
- Existing atlas reference scan: `ATLAS_MATCHES=0`.
- Required hero, item, architecture, chain-safety, persistence, reset, and export terms: all present.
- Structural counts: 23 system rows, 7 review sections, one HTML root, one closing HTML root.
- Independent correction checks: item rarity labels match all 12 `ItemRarity` values; no obsolete `史诗 / 传说` labels remain; the scope bridge has no inline grid override and inherits the 1050/760 responsive rules; soldier primitive counts and current-versus-optional corpse semantics are explicit; future hero validation permits an authority-confirmed resource exception.
- Composition-root static check confirms the required project/campaign/run-rule/semantic-presentation references, cohesive service names, `ScreenRouter` boundary, and current 15-floor/3-region plus 6-deployed/3-reserve inventory are present.

Remaining verification and limitations:

- The required Browser workflow found no available browser instances, so no claim is made that desktop/narrow-width visual QA has passed.
- Resume by opening `web/system-architecture-review/index.html` at a representative desktop width and a narrow width. Verify sticky navigation, local table containment, diagrams, review buttons, notes persistence after reload, safe reset cancellation/confirmation, valid JSON download, visible keyboard focus, and absence of page-level horizontal scrolling.
- If visual or interaction defects are found, modify only the HTML and update this same handoff. Do not change runtime, authority documents, the mechanism atlas, hosting, or external state.
