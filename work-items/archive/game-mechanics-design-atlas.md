# Game Mechanics Design Atlas Web

Status: Complete

## Goal

Create a highly detailed, Chinese-language interactive web design atlas for the tower-climbing army autobattler. The atlas maps the game's large gameplay possibility space into stable dimensions, mechanic ideas, cross-dimension combinations, counters, build archetypes, and boss tests so future discussion can explore broadly before accepted rules are narrowed.

The site must remember the user's exploration and decisions across refreshes and browser restarts. It is a durable local design workspace rather than a static article.

## Confirmed Solution

- Build an interactive web application, not a Markdown document embedded in a page.
- Organize the design space from dimensions to mechanic options, combinations, build archetypes, encounter tests, and concrete content examples.
- Use the current blank rectangular grid, automatic combat, pre-battle deployment, and limited hero commands as the baseline coordinate system.
- Keep special maps, terrain, manual movement intervention, and higher-operation variants visible as explicitly labelled expansion dependencies instead of mixing them into baseline requirements.
- Separate the read-only authored atlas catalog from user-owned workspace state.
- Give every dimension and mechanic a permanent stable ASCII id so catalog revisions do not discard saved choices.
- Persist device-local exploration state automatically. The accepted baseline is browser-local persistence plus named workspaces, named snapshots, and JSON export/import recovery; account, backend, and cross-device synchronization are not required.
- Support distinct decision states so an interesting idea is never mistaken for accepted gameplay authority.
- Present relationships through useful interactive views: dimension navigation, mechanic cards, filtering/search, synergy/counter/dependency relationships, build-engine examples, boss capability tests, and decision/progress summaries.
- Deliver and validate the completed site. Publish through Sites without a sign-in requirement; local persistence remains device/browser scoped even when hosted.
- Discourage search-engine indexing with a `noindex` directive while keeping direct unauthenticated access available to anyone with the URL.
- Refine the atlas from an always-expanded document layout into a calm design tool with explicit overview, focus, selection, and comparison layers.
- Use the build-engine view as the representative interaction pattern: compact engine summaries remain scannable, one focused engine owns the detailed explanation, and advanced information appears on demand.
- Keep the current catalog content, stable ids, workspace decisions, notes, snapshots, JSON recovery, and device-local persistence behavior unchanged while restructuring presentation.
- Establish a reusable semantic illustration language so spatial rules, targeting, timing, conversion loops, and counter relationships can be understood visually before reading the complete prose.
- Validate that language with twelve representative code-native diagram prototypes before extending it to the complete mechanic catalog. Precise rule diagrams use HTML/CSS shapes and motion rather than generated bitmap art or model-authored SVGs.

## Authority Impact

- The atlas is an exploratory design workspace and does not override `gameplay-design/tower-autobattler-core.md`.
- Accepted player-facing rules remain under `gameplay-design/`.
- Runtime ownership and implementation contracts remain under `system-design/`.
- An atlas entry may be `unreviewed`, `interested`, `candidate`, `confirmed`, `deferred`, or `excluded` inside a user workspace. Only a separately confirmed documentation change may promote an atlas idea into gameplay authority.
- The new site must include an explicit explanation of this boundary.

## Scope

### Design taxonomy

Cover at least the following top-level domains with deep, concrete subdimensions and idea branches:

1. Battlefield space: occupancy, body blocking, formation, adjacency, distance, engagement lines, guarding, charging, interception, displacement, range, attack shapes, targeting, aggro, clustering, and separation.
2. Combat systems: attacks, cooldowns, casting, health, armor, shield, healing, lifesteal, damage categories, armor/shield interaction, control, status effects, marks/stacks/charges, summons, corpses, death triggers, phases, enrage, and battle-time pressure.
3. Army construction: functional roles, formation families, full-roster/elite/swarm/solo shapes, tags and cross-faction packages, front/backline cooperation, reserves as a sideboard, replacement, upgrades, attrition recovery, and weakness patching.
4. Heroes, skills, and relics: fixed identity, army rule, automatic skill pool, bounded manual command, upgrade branches, stat/conditional/trigger/conversion/spatial/risk/core/loop relics, and rule-breaking legendary effects.
5. Enemies, bosses, and encounters: enemy jobs, authored encounter templates, elite modifiers, boss phases/timelines, telegraphs, capability checks, soft and hard counters, multiple solution families, advance warning, adaptation windows, and failure explanation.
6. Run and replay structure: recruitment, replacement, upgrading, recovery, currency/opportunity cost, reward pools and rarity, route information, build timing, pivots, casualties, difficulty, meta unlocks, and replay variation.
7. Player agency and future expansions: preparation-only answers, automatic triggers, bounded commands, optional movement intervention, special terrain/maps, and the dependency cost of each expansion.
8. Readability, balance, and simulation safety: causal feedback, mechanic vocabulary, counter fairness, deterministic triggers, same-tick recursion prevention, rate limits, stalemate prevention, and explainable reports.

### Mechanic record contract

Each mechanic idea must expose enough structured information to compare it rather than merely list it:

- stable id, Chinese title, domain, subdimension, concise premise, detailed rule idea;
- player decision and intended pleasure;
- spatial, temporal, resource, and build consequences;
- synergies, counters, dependencies, risks, readability needs, and implementation/simulation concerns;
- suitable heroes, armies, relics, enemies, bosses, or floor rules;
- baseline support versus special-map or movement-intervention dependency;
- agency type, complexity, depth type, and workspace decision state;
- related mechanic navigation.

The catalog should be broad and genuinely useful: substantial coverage across every domain, not a thin page containing a few examples. Quality and structured distinctions matter more than inflating a raw card count.

### Interactive views

- A product-specific overview that explains the atlas and immediately exposes the major design domains.
- Persistent domain/subdimension navigation.
- Full-text search and filters for domain, decision state, baseline/expansion dependency, agency type, complexity, and depth type.
- Mechanic cards/details with relationship navigation and editable user notes.
- Dimension tree or equivalent hierarchical exploration.
- Build-engine view showing multi-step trigger/conversion loops, including shield/casting and other representative engines with safety boundaries.
- Boss-test/capability matrix showing which army capabilities answer which pressures without implying a single mandatory solution.
- Decision dashboard summarizing evaluated, candidate, confirmed, deferred, and excluded ideas.
- Responsive layout with keyboard-accessible controls and readable mobile fallback, while prioritizing desktop knowledge-work use.

### UX hierarchy refinement

- Preserve the global navigation, then separate each page into page purpose, object overview, focused detail, and optional advanced information instead of rendering all levels at equal weight.
- On the build-engine page, replace the vertical stack of fully expanded engine cards with a desktop master-detail layout: a compact selectable engine list and one focused detail panel. Narrow layouts may use stacked summaries plus an accessible detail drawer or accordion.
- A compact engine summary exposes only the engine name, one-sentence premise, a small set of meaningful tags, and its current workspace selection state. Trigger order, components, breakers, safety boundaries, risks, and related mechanics belong to the focused detail.
- Make `查看详情`, `加入当前方案`, and `加入比较` explicit actions. Selected engines remain visible in a low-noise selection tray; comparison is a separate state and does not crowd ordinary browsing.
- Consolidate workspace creation, duplication, rename, and deletion under a labelled management menu. Keep automatic-save feedback reassuring but visually secondary.
- Establish hierarchy primarily through spacing, typography, surface grouping, and progressive disclosure. Reserve deep teal for the active focus, gold for selected/important state, and pale semantic surfaces for risk or guidance; reduce repeated dark bars, borders, micro-labels, and decorative English copy.
- Preserve keyboard focus, touch targets, semantic labels, responsive behavior, and reduced-motion support through the redesign.

### Semantic mechanic illustration prototypes

- Add one shared `MechanicDiagram` presentation component with a restrained tabletop-wargame grammar: small authoritative grid, teal allied units, crimson enemy units, steel shield/armor, gold focus or resource, readable paths/range cells, and shape plus motion rather than color alone.
- Render a compact diagram before prose on the twelve target mechanic cards and a larger explanatory stage at the top of their focused detail. Do not add illustration placeholders to unrelated mechanics during this prototype round.
- Cover these exact prototypes and stable ids:
  1. `battlefield.targeting.threat-lock`: taunt/threat redirection, with target lines converging from a vulnerable rear unit onto the threat holder.
  2. `battlefield.shape.cone`: area coverage expanding from the caster across a fan of grid cells.
  3. `battlefield.shape.line-pierce`: one projectile path crossing and striking several aligned units.
  4. `battlefield.engagement.intercept`: an enemy movement path stopped when it enters a guard threat cell.
  5. `battlefield.engagement.charge`: charge momentum crossing cells, then ending in a displaced front-line target.
  6. `combat.defense.shield`: damage removing a visible shield layer before health and exposing the break event.
  7. `combat.summon.corpse`: unit defeat creating one owned corpse token that is consumed into one summon.
  8. `combat.trigger.overheal`: healing fills health, excess flows into a bounded charge resource, then powers an effect.
  9. `army.role.aoe-casters`: a dense swarm entering an area template and losing multiple bodies, visually expressing the counter.
  10. `combat.damage.armor-break`: repeated hits remove armor segments before a heavy finisher, visually expressing the heavy-armor counter.
  11. `encounter.template.backline-hunter`: a hunter route toward the rear core intercepted or redirected by a guard/decoy answer.
  12. `encounter.boss.timeline`: telegraph, response window, command timing, and Boss resolution shown on a short timeline.
- Each prototype may use a restrained two-to-four-step CSS animation with a visible `播放演示` / `暂停演示` control in detail. The summary thumbnail remains understandable when motion is paused. `prefers-reduced-motion` disables automatic motion and keeps the final state readable.
- The diagrams contain no embedded prose. Adjacent Chinese captions and accessible descriptions explain the semantic meaning; visuals supplement rather than replace authoritative rules.
- Reduce reading load on illustrated mechanic cards by showing the diagram, title, premise, and compact tags first. The full decision prompt, rule, risks, implementation notes, and relationships remain in the focused detail and may use progressive disclosure without hiding workspace state or notes.
- Use the AOE-versus-swarm, armor-break-versus-armor, and backline-hunter-versus-guard scenes as the first counter-relationship visual prototypes. Their source/pressure/answer direction must remain explicit and clickable relationships must continue to open the target mechanic.

### Persistence and recovery

- Autosave all meaningful user mutations and show save status/last-saved feedback.
- Restore choices, notes, current workspace, filters, expanded navigation, and useful resume context after refresh and browser restart.
- Support multiple named workspaces whose selections and notes do not contaminate each other.
- Support workspace duplication, rename, and safe deletion.
- Support named snapshots/checkpoints.
- Support JSON export and import with validation and clear overwrite/merge behavior.
- Use catalog and user-state schema versions. Preserve state through catalog additions and display unmatched/orphaned saved records instead of silently deleting them.
- Require confirmation for reset, destructive import, workspace deletion, or other material data loss.
- Avoid same-device multi-tab silent overwrite where practical; at minimum detect newer external state and prompt or safely refresh.

## Non-Goals

- Implementing any atlas mechanic in the Godot game.
- Changing accepted hero mana, movement, map, combat, progression, or other gameplay contracts.
- Treating brainstormed ideas as scheduled development work.
- Account creation, authentication, backend persistence, collaborative editing, or automatic cross-device synchronization.
- Building a combat simulator or balance calculator in the first version.
- Generating new game art or changing existing game assets.
- Modifying the unrelated GitHub publication work item or other user changes.
- Redesigning or rewriting the authored mechanic catalog, build-engine rules, Boss tests, taxonomy, or accepted gameplay authority during the hierarchy refinement.
- Replacing browser-local persistence with backend storage, authentication, collaboration, or cross-device synchronization during this refinement.
- Generating atmospheric or character bitmap illustrations in this prototype round; their purpose, dimensions, quantity, and acceptance remain a later discussion after the semantic diagrams are reviewed.
- Claiming complete per-mechanic illustration coverage before the twelve prototype families are visually accepted and parameterized for the remaining catalog.

## Constraints

- Work on `main`; do not create or switch branches.
- Preserve all existing project changes.
- Keep the site in a focused, self-contained project surface and document how to run or publish it.
- Use Chinese for all player/designer-facing copy and stable English/ASCII ids for data contracts.
- No external runtime dependency on the donor repository.
- Do not edit `AGENTS.md` for task-specific routing or progress.
- Prefer structured data for atlas content so navigation, filters, relationships, persistence migration, and future expansion do not depend on parsing prose.
- Browser-local state is explicitly confirmed for the first version; JSON export/import is the required portability and disaster-recovery path.
- The visual design should feel like a serious game-systems workshop: dense but calm, legible, purposeful, and distinct from a generic admin dashboard.

## Acceptance Criteria

- The site presents all scoped design domains with substantial, structured mechanic coverage and clear cross-links.
- A user can understand the difference between dimension, mechanic, combination, build archetype, encounter test, and accepted authority.
- Search and all primary filters work together and can be cleared without corrupting results.
- Mechanic state and notes can be changed independently in at least two named workspaces.
- Refreshing or reopening restores the active workspace, mechanic decisions, notes, filters, and navigation context.
- Workspace create, duplicate, rename, switch, and delete flows behave predictably; destructive actions require confirmation.
- Named snapshots can be created and restored without corrupting the current workspace.
- Exported JSON can restore the complete design workspace after a local reset; malformed or incompatible imports fail safely with an understandable message.
- Stable ids and schema versions are explicit, and unknown saved mechanic ids are retained and surfaced rather than silently discarded.
- The build-engine view explains representative loops and their trigger-order/rate-limit safeguards.
- The boss-test view demonstrates multiple solution families for major pressures such as swarms, shields/armor, backline threats, sustain, burst windows, summons, and phase timers.
- The site is usable at desktop and narrow viewport widths and has accessible labels, visible keyboard focus, and no required mouse-only interaction.
- Production build passes. Persistence and key interaction behavior have focused automated checks where practical, plus a browser-level verification pass if the available environment supports it.
- The site contains no claim that exploratory selections automatically modify accepted gameplay authority.
- A published Sites URL is delivered when hosting succeeds; it opens without ChatGPT or other account authentication and exposes a `noindex` directive.
- The build-engine page no longer presents every engine as a fully expanded equal-weight block. All engines remain quickly scannable while only one engine's complete detail dominates the page at a time.
- The focused build-engine detail preserves every existing trigger step, component, breaker, safety boundary, risk, and related-mechanic link without duplicate presentation in the compact list.
- Engine selection and comparison actions are explicit, keyboard reachable, persisted in the existing device-local workspace model, and visible without relying on color alone.
- Workspace management actions are understandable without interpreting icon-only controls, while autosave status remains available at lower visual weight.
- At the desktop reference width, the page has one primary visual focus and no repeated full-width dark headers. At narrow widths, summaries and focused detail remain readable and operable without horizontal page scrolling.
- Existing persistence, import/export, workspace isolation, snapshots, catalog navigation, Boss matrix, decision dashboard, public access, and noindex behavior do not regress.
- All twelve target stable ids render their unique rule diagram in both the library summary and focused detail without changing the authored mechanic rule text or relationships.
- A user can distinguish the acting unit, target, affected cells/path, before/after state, and counter direction in each prototype without relying on color alone.
- Illustrated summary cards are materially quicker to scan: diagram, title, one-sentence premise, and compact tags form the default reading layer; complete prose remains reachable in detail.
- Detail animation can be played and paused with mouse, keyboard, and touch; reduced-motion users receive a clear static state with no required animation.
- Diagram components use no generated bitmap, model-authored SVG, canvas screenshot, remote asset, or new persistence field; they remain themeable code-native presentation.
- Desktop and narrow layouts keep diagrams legible without horizontal page scrolling, clipped labels, obscured actions, or unreadably small units.
- Focused automated checks assert exact prototype coverage, unique visual kinds, accessible descriptions/controls, reduced-motion handling, and unchanged persistence/import behavior.

## Progress

- 2026-08-30: Discussion established the need for a web-based design-space atlas rather than a fixed gameplay proposal or a long static document.
- 2026-08-30: User confirmed the dimension-first taxonomy, structured mechanic records, interactive relationship views, blank-grid baseline, and explicit expansion labels.
- 2026-08-30: User confirmed durable device-local persistence, including automatic recovery after refresh, named workspaces, snapshots, and JSON backup/restore.
- 2026-08-30: Execution authorized. Activity document created; implementation is ready for the execution agent.
- 2026-08-30: Execution started on `main`. Applicable project authority, repository status, Sites capability-path guidance, and device-local persistence guidance were reviewed. The site will use the explicitly confirmed browser-local model rather than accounts or platform storage.
- 2026-08-30: Core implementation milestone complete under `web/game-mechanics-atlas/`: eight design domains, 72 structured mechanic records with permanent ids, five build-engine chains, seven Boss capability tests, interactive dimension/library/engine/matrix/decision views, responsive styling, and an explicit exploratory-authority boundary.
- 2026-08-30: Persistence milestone complete: autosaved device-local state, named and independent workspaces, duplication/rename/safe deletion, notes and decisions, resume filters/navigation/scroll context, named snapshots with non-destructive restore, validated JSON merge/overwrite, schema migration, orphan retention, destructive confirmations, and newer multi-tab revision detection.
- 2026-08-30: Initial production build, focused persistence/import tests, server-render test, and lint completed successfully. In-app browser discovery was attempted for the requested browser-level pass, but the current environment exposed no browser instance; this does not block Sites publication under the hosting instructions.
- 2026-08-30: Final production build completed after binding the Sites project id. Four focused automated checks passed (migration/orphan preservation, malformed/future import rejection, collision-safe workspace merge, and rendered product shell), and ESLint passed with no findings.
- 2026-08-30: Sites version 1 was published successfully with verified owner-only access at `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`. Direct unauthenticated HTTP retrieval correctly reaches the private ChatGPT sign-in boundary; no public access was introduced.
- 2026-08-30: User requested removal of the private sign-in boundary and explicitly confirmed unauthenticated public access with `noindex`. Access-level correction resumed without changing atlas content or local workspace persistence.
- 2026-08-30: Integration review found that Godot had scanned starter SVGs inside the new website. Added `web/.gdignore` to exclude the complete web surface from Godot imports, removed all unused starter SVGs and their generated `.import` sidecars, and removed unused D1/Drizzle examples, configuration, runtime/dev dependencies, and migration-copy shell. The worker no longer declares an unused D1 binding.
- 2026-08-30: Post-cleanup production build, four focused tests, and lint all pass. The emitted client contains no starter SVGs and the build emits no Drizzle payload.
- 2026-08-30: The cleaned final source was pushed and saved as Sites version 2, then deployed successfully with verified owner-only access. The production URL remains `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`; the live deployment now matches the post-review source and artifact cleanup.
- 2026-08-30: UX hierarchy refinement execution started. Authority and integration constraints were reread; architecture is confirmed as a presentation plus device-local workspace-state change that reuses `AtlasApp` and the existing persistence layer. Work remains restricted to the site and this work item, with no catalog rewrite, Godot mutation, or publication.
- 2026-08-30: Added site-wide search directives through authoritative page metadata: `robots=noindex, nofollow, nocache` and `googlebot=noindex, nofollow, noimageindex`. Server-render verification now asserts both directives; production build, four focused tests, and lint all pass without changing atlas behavior or device-local persistence.
- 2026-08-30: The existing Sites project access policy was changed from owner-only to public per explicit user authorization. Cleaned source plus noindex metadata was saved as Sites version 3 and publicly deployed at `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`.
- 2026-08-30: Unauthenticated HTTP verification returned status 200, the expected atlas title, no sign-in boundary, `robots=noindex, nofollow, nocache`, and `googlebot=noindex, nofollow, noimageindex`.
- 2026-08-30: User reviewed the published build-engine page and rejected the current visual hierarchy as overly dense, visually fragmented, and insufficiently layered. Discussion identified the root cause as always-expanded equal-weight content rather than color alone.
- 2026-08-30: User confirmed a hierarchy refinement centered on compact summaries, one focused detail, explicit selection/comparison states, quieter workspace management, and progressive disclosure. Existing content and browser-local persistence remain authoritative and unchanged. Execution resumed on `main`.
- 2026-08-30: UX hierarchy refinement implementation completed without changing the five authored engine definitions or the mechanic catalog. The build-engine page now uses a compact selectable overview plus one dominant focused detail; it preserves every trigger step, required component, breaker, simulation safeguard, and related-mechanic link. Explicit per-workspace plan and comparison actions feed a low-noise selection tray.
- 2026-08-30: Workspace creation, duplication, rename, and safe deletion were consolidated under a labelled management menu. Autosave remains visible but secondary. Repeated decorative English micro-labels were reduced, focus-visible styling remains global, and the narrow layout now stacks engine summaries/details with an additional compact-phone topbar guard.
- 2026-08-30: Device-local schema advanced from v2 to v3 under the unchanged `tower-atlas.workspace.v2` storage key. Migration normalizes missing engine state to an empty record, preserves v3 engine selection/comparison in workspaces and snapshots, and persists focused-engine context. Existing mechanic decisions, notes, filters, snapshots, orphan records, JSON recovery, and workspace ids remain normalized through the same migration path.
- 2026-08-30: Final validation passed: production build completed; 8 focused tests passed covering v1/v2 migration, v3 engine persistence, workspace isolation, snapshot/JSON retention, malformed/future import safety, merge collisions, rendered metadata, and required master-detail actions; ESLint completed with zero findings. Browser connection discovery returned no available instance, so interaction screenshots and real viewport clicks remain for independent verification. No publication was performed in this refinement run.
- 2026-08-30: Independent review found that the comparison selection tray only focused one engine at a time and did not yet provide the confirmed multi-engine comparison layer. Execution resumed within the existing UX refinement scope to add a keyboard-accessible comparison panel without changing the persisted workspace data contract.
- 2026-08-30: The comparison gap is resolved. When at least two engines are marked for comparison, the tray exposes `打开比较（N）`; the independent comparison panel renders every selected engine's title, thesis, requirements, breakers, and simulation safeguards together. Each card can leave comparison or return to that engine's complete focused detail, and explicit top/bottom close actions restore keyboard focus to the comparison trigger. The panel is ephemeral UI, resets when changing workspaces, and does not expand schema v3.
- 2026-08-30: Comparison validation passed: production build completed, all 8 focused tests passed with the master-detail test extended to assert open/close, remove/focus, and all required comparison fields, and ESLint completed with zero findings. Responsive CSS uses a two-column comparison grid only at wide widths and a single-column card stack below 1120px, with card actions stacked on narrow screens. No publication was performed.
- 2026-08-30: Parent review accepted the implementation after the comparison gap was corrected. A fresh production build, all 8 focused tests, and ESLint passed independently. The in-app browser runtime exposed no connected browser, so visual click and screenshot QA remained unavailable and did not block Sites publication under the hosting contract.
- 2026-08-30: The exact validated source was saved as Sites version 4 and deployed successfully to the existing public production URL. Unauthenticated HTTP returned 200 with the expected atlas shell, no sign-in boundary, both noindex directives, and live JavaScript assets containing the new `构筑实验台`, `引擎概览`, `管理工作区`, `打开比较`, `断链风险`, and `模拟安全边界` UI.
- 2026-08-30: User requested visual explanations for individual mechanics such as taunt and area damage, plus visual counter relationships, because the text-dominant catalog remained tiring to read. Discussion separated precise semantic diagrams from decorative illustration.
- 2026-08-30: User confirmed a phased semantic-illustration approach. The first execution round covers twelve exact rule/counter prototypes with code-native grid scenes and restrained motion; generated atmospheric art and complete 72-mechanic coverage remain gated on prototype review. Execution resumed on `main`.
- 2026-08-30: Semantic diagram implementation started after rereading gameplay authority, runtime architecture, Sites guidance, and ImageGen's code-native-diagram exclusion. Architecture remains strictly Web presentation: one reusable semantic diagram component and stable-id mapping extend `MechanicLibrary` and `MechanicDetail`; catalog rules, relationships, persistence schema, and Godot remain untouched. No bitmap, SVG, remote asset, dependency, or publication is authorized in this execution pass.
- 2026-08-30: Implemented one reusable code-native `MechanicDiagram` grammar and a separate stable-id definition map for exactly the twelve confirmed prototypes. The map has twelve unique visual kinds plus explicit actor, target, affected cells/state, path, before/after, adjacent Chinese explanation, accessible description, and three-step reading sequence for each prototype. No unrelated mechanic receives a placeholder.
- 2026-08-30: Integrated compact paused diagrams into only the twelve mechanic-library cards and larger explanatory stages at the top of their focused details. Illustrated cards now default to diagram, title, premise, and compact tags, while unchanged detail content retains full rule, decision, pleasure, consequences, risks, implementation notes, workspace state/notes, and relationship navigation.
- 2026-08-30: Added a restrained HTML/CSS tabletop grammar: six-by-four grid, circle-versus-diamond unit silhouettes, role marks, striped affected cells, dashed original routes, solid/double answer routes, stop/impact/break/corpse/charge/armor/timeline shapes, and four-stage CSS motion. Detail provides native `播放演示` / `暂停演示` controls with `aria-pressed`; reduced-motion CSS disables animation and leaves the complete static state readable. Narrow details stack explanations, counter links, and touch-sized controls without a horizontal table.
- 2026-08-30: Counter prototypes explicitly show `密集敌群压力 → 范围清场答案`, `分段护甲压力 → 破甲叠层答案`, and `绕后猎杀压力 → 护卫 / 诱饵答案`. Their adjacent buttons navigate to real catalog targets (`encounter.template.swarm`, `combat.defense.armor`, and `battlefield.formation.guard-pocket`) without replacing or altering the existing relationship groups.
- 2026-08-30: Static semantic audit completed for all twelve diagrams after correcting target-line, intercept, incoming-damage, AOE-pressure, armor-finisher, and backline-counter arrow directions. Every prototype exposes non-empty actor, target, affected cells/state, path, and before/after evidence; shape, line style, position, Chinese explanation, and accessible description carry meaning in addition to color.
- 2026-08-30: Final validation passed: production build completed, all 13 tests passed, and ESLint reported zero findings. New tests assert the twelve exact ids, twelve unique visual kinds, catalog existence, full semantic audit fields, three counter directions and real targets, card/detail integration, accessible play/pause controls, code-native output with no SVG/canvas/image, reduced-motion CSS, the unchanged 72-mechanic catalog, and unchanged schema-v3/localStorage contract. No dependency, persistence, catalog, Godot, or publication change was made; live Sites version 4 remains unchanged.
- 2026-08-30: Publication-readiness review found one isolated geometry defect in the backline-counter answer path: the hunter-to-decoy segment used a single oversized percentage/angle that overshot the right-upper hunter in both supported diagram aspect ratios. The segment now starts at the decoy's exact 6×4 center `(58.333%, 87.5%)` and uses aspect-correct geometry per rendering mode: `55% / -52.7°` for the 12:7 summary and `60.1% / -56.3°` for the 3:2 detail. The existing `reverse` arrow still points toward the decoy. A focused regression assertion rejects the old `82% / -63°` geometry; all 13 tests, production build, and ESLint pass. No other visual grammar, catalog, persistence, or publication state changed.
- 2026-08-30: Parent independent verification accepted the semantic-diagram implementation and the corrected backline-counter geometry. The exact validated source was saved and deployed as Sites version 5 at the existing public URL `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`.
- 2026-08-30: Anonymous production verification returned HTTP 200 with no sign-in boundary. Rendered metadata preserved robots `noindex, nofollow, nocache` and Googlebot `noindex, nofollow, noimageindex`. Across the seven served JavaScript/CSS assets, verification found `data-mechanic-diagram`, the play/pause copy, exact prototype stable ids, the backline-counter wording, and the unchanged `tower-atlas.workspace.v2` device-local storage key.
- 2026-08-30: This activity is complete and intentionally closes at twelve validated semantic prototype families. Extending the diagram grammar to the remaining sixty catalog mechanics, or adding atmospheric/generated illustration, is outside this completed scope and requires a new explicit discussion and confirmation.

## Resume Condition

Read this work item, `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, current `git status`, and the Sites building/persistence instructions. Preserve unrelated user changes. Resume at the first incomplete acceptance criterion without redesigning the confirmed taxonomy or persistence model.

Current resume entry: none. The confirmed twelve-prototype round is implemented, independently verified, and publicly deployed as Sites version 5. Any remaining sixty-mechanic rollout or atmospheric-art work begins as a new confirmed activity rather than resuming this one.

## Verification Handoff

Sites version 5 is publicly deployed with the hierarchy refinement and the completed twelve-prototype semantic-illustration round. Automated/static validation and parent independent verification passed before deployment.

Verification evidence:

- Production build: passed (`vinext build`, Cloudflare-compatible output).
- Focused automated tests: 13 passed, 0 failed, including all prior persistence/master-detail coverage plus exact diagram ids, unique visual kinds, semantic audit completeness, real counter targets, accessible controls, reduced motion, catalog count, and unchanged persistence schema.
- Backline counter geometry: summary and detail use separately calculated 6×4 endpoint geometry; regression coverage asserts both mode-specific values and rejects the former overshooting segment.
- Static code quality check: passed with 0 findings.
- Hosting: Sites version 5 is the public production deployment at `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`.
- Public access: unauthenticated HTTP returned 200 with the atlas content and no ChatGPT sign-in boundary.
- Indexing policy: rendered metadata exposes `noindex, nofollow, nocache` plus Google-specific `noimageindex`; automated and live HTTP checks passed.
- Source scope: implementation is contained under `web/game-mechanics-atlas/`; no gameplay authority or Godot runtime file was changed by this task.
- Godot integration: `web/.gdignore` now prevents the engine from importing website source/assets; unused generated `.import` sidecars were removed.
- Post-review cleanup: production build passed again; 4 tests passed; lint reported 0 findings; no D1/Drizzle or starter public SVG artifacts remain in the source/build.

Independent smoke requested:

1. In workspace A, set two different mechanic states and notes, apply filters, expand a dimension, refresh, and confirm all context returns.
2. Duplicate to workspace B, change the same mechanics, switch between A/B, and confirm isolation.
3. Create and restore a snapshot, confirming restore creates a new workspace without overwriting the active one.
4. Export JSON, import with safe merge, and confirm the imported workspace is independent. Use a malformed JSON file and confirm current data remains untouched.
5. Inspect the mechanism drawer relationships, build-engine safety boundaries, Boss capability matrix, decision dashboard, orphan-state explanation, authority boundary, and narrow responsive layout.
6. Inspect all twelve illustrated cards and details while paused; confirm actor, target, affected grid/path, and before/after state remain readable without motion or color alone.
7. Play and pause several detail demonstrations by mouse, keyboard, and touch; enable reduced motion and confirm the static complete state remains readable.
8. For AOE, armor break, and backline hunter, confirm the adjacent pressure-to-answer direction and navigate through the provided real-target button, then confirm the original relationship groups still work.

Known verification limitation: no in-app or extension browser instance was available during implementation. Static responsive/keyboard review, automated checks, parent independent review, deployment status, unauthenticated HTTP, metadata, and production-asset content verification passed.

Publication note: Sites version 5 is the current public production deployment and contains the confirmed hierarchy refinement, twelve semantic mechanic prototypes, and site-wide noindex metadata. Anonymous HTTP returned 200 without a login boundary; both robots directives and the production asset markers were verified. The repository-only `web/.gdignore` remains outside the hosted site by design while protecting Godot from scanning the complete web surface locally.
