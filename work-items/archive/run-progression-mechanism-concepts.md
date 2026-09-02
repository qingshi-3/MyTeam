# Run Progression Mechanism Concepts

Status: Completed

## Goal

Create ten complete, materially different run-progression mechanism concepts for the existing single-player army autobattler. Each concept must carry one full roguelike run from randomized setup through player-directed growth, repeated deployment/battle payoff, escalating opportunity cost, and a terminal challenge. A traditional world map is only one possible presentation; the task designs progression systems rather than ten cosmetic maps.

## Confirmed Solution

- Treat the deliverable as ten self-contained run structures, not ten isolated pictures and not ten visual skins over a Slay-the-Spire route graph.
- Preserve the current product priority: construction and growth create the decisions; automatic combat validates the build and supplies the payoff. Narrative may provide concise optional context in `【】`, but narrative-roleplaying completeness is not a design objective.
- Each concept explicitly defines its run seed, player action economy, random content, directed growth opportunities, battle entry, battle-to-progression feedback, anti-exhaustion pressure, terminal trigger, and replay variation.
- Explore ten distinct progression families:
  1. territorial campaign;
  2. limited calendar and scheduling;
  3. multi-contract task network;
  4. mobile-base expedition;
  5. multi-front allocation and defense;
  6. tournament season and opponent elimination;
  7. organization/base phase development;
  8. competing adventuring parties;
  9. excavation and region discovery;
  10. chapter-by-chapter world-rule drafting.
- Randomness creates the run's problem and opportunity distribution; stable, inspectable rules let the player plan. Prefer fixed effects combined with randomized placement, ownership, timing, tags, opponents, task availability, and derived content.
- Produce generated raster visuals with the built-in image generation workflow. Do not use HTML, CSS, SVG, Canvas, or ordinary execution agents as a substitute for image generation.
- Give each concept as many generated visuals as it needs to explain the mechanism. Do not force one concept into exactly one image. Use a coherent pixel-strategy-game visual grammar, diagram-first compositions, minimal text, arrows, icons, state changes, and before/action/after sequences.
- Keep generated image text minimal because legibility matters. Put exact concise Chinese explanations in the accompanying design document rather than relying on dense in-image typography.
- Store the concept package and selected final images in a new isolated documentation surface. Do not modify the existing game-mechanics atlas, Godot content, runtime, accepted gameplay authority, or existing active work.

## Authority Impact

- This is exploratory design material only.
- It does not modify `gameplay-design/tower-autobattler-core.md` or promote any concept into accepted player-facing authority.
- It does not modify `system-design/tower-autobattler-architecture.md` or authorize implementation architecture.
- A later user selection requires a new discussion before any concept changes the game.

## Scope

- One concise visual design package containing ten full run-progression concepts.
- For every concept: core loop, randomized inputs, player verbs, construction/growth interaction, combat connection, progression pressure, final condition, and principal risk.
- Generated pixel-style raster diagrams and mechanism illustrations sufficient to understand the concepts visually.
- A comparison summary focused on gameplay differentiation, randomness, build agency, and implementation risk.
- Visual inspection of every selected generated image for subject, composition, mechanism readability, unintended text, and watermark/artifact issues.

## Non-Goals

- Selecting or accepting one concept as the final game direction.
- Modifying Godot scenes, resources, code, tests, saves, or existing gameplay authority.
- Building a website, HTML prototype, interactive mockup, production UI, or final game map.
- World lore, character endings, moral-choice simulation, dialogue writing, or narrative campaign completeness.
- Exact numerical balance, production content counts, final art direction, or implementation estimates beyond comparative risk.

## Constraints

- Work on `main`; do not create or switch branches.
- Preserve the dirty worktree and isolate all new artifacts from existing work.
- Use the image-generation skill and built-in image model for raster generation.
- Generate distinct assets with distinct prompts; do not request unrelated concepts as variants of one prompt.
- Do not overwrite existing assets.
- Keep diagrams readable without depending on color alone where practical; use position, shapes, arrows, icons, and panel order.
- Player-facing explanatory text is Chinese; stable concept ids and filenames remain ASCII/English.

## Acceptance Criteria

- Exactly ten run-progression concepts are delivered, and each is a complete run framework rather than a single map mechanic.
- The ten concepts differ in their primary progression engine, not merely theme, terrain, factions, or visual layout.
- Every concept explicitly shows how random generation changes planning while stable rules remain learnable.
- Every concept shows how the player intentionally pursues construction/growth and how battle both consumes and advances the progression state.
- Every concept includes an exhaustion/time/escalation mechanism and a concrete terminal trigger.
- Generated visuals are raster images produced by the built-in image model, use a coherent pixel-strategy visual language, and emphasize diagrams over prose.
- The concept package remains understandable if the generated images contain no embedded explanatory text.
- No existing runtime, authority document, website, or unrelated active work is changed.

## Progress

- 2026-08-30: Discussion separated gameplay-impacting systems from optional story background and identified construction/growth plus combat payoff as the product priority.
- 2026-08-30: Discussion confirmed that randomized composition should use stable logical rules and tags rather than requiring bespoke visual variants.
- 2026-08-30: The deliverable was corrected from ten HTML maps, then ten images, to ten complete and materially different run-progression mechanism concepts with generated raster visuals as supporting explanation.
- 2026-08-30: User confirmed the final execution framing. Activity document created; design and image generation are in progress.
- 2026-08-30: Ten complete run frameworks were authored under one isolated exploratory package: territorial campaign, limited calendar, contract network, mobile-base expedition, multi-front defense, tournament season, run headquarters, rival parties, excavation, and world-rule drafting.
- 2026-08-30: The built-in image model generated twenty selected raster assets: one complete-run overview and one key decision/combat/growth loop for every concept. All final visuals use the shared pixel-strategy diagram grammar, contain no explanatory prose, and are stored inside the package rather than left only in the model's default output location.
- 2026-08-30: One excavation-overview generation request failed from a transient connection error and was retried unchanged through the same built-in workflow; the retry succeeded. No CLI or alternate model was used.
- 2026-08-30: File verification found exactly twenty unique PNG files and twenty matching README references with no missing target. Overview assets are `1536×1024`; sequence assets use wide generated canvases between `1717×916` and `1823×863` as appropriate to their multi-stage flows.
- 2026-08-30: Primary visual inspection accepted all twenty selected images for mechanism focus, pixel-style consistency, icon/arrow readability, absence of embedded prose, and absence of visible watermark or logo artifacts.
- 2026-08-30: Independent package review returned PASS. It confirmed exactly ten materially distinct and complete progression frameworks, all required randomness/action/growth/combat/pressure/terminal fields, twenty unique resolved image references, a complete prompt inventory, and no delivery blocker.

## Resume Condition

The task is complete and independently reviewed. There is no normal resume action. Any request to revise a concept, generate additional variants, select a direction, or promote one concept into gameplay authority begins as a new discussion and must not silently change the accepted game.

## Verification Handoff

Independent verification passed:

1. The package contains exactly ten materially different run-progression concepts. Each states randomized inputs, player action economy, directed construction/growth, combat entry and feedback, anti-exhaustion pressure, terminal condition, replay variation, and principal risk.
2. `README.md` references exactly twenty unique selected PNGs—one overview and one decision-loop visual per concept—and every reference resolves.
3. `PROMPTS.md` records the shared visual grammar plus twenty asset-specific prompt requests. The built-in image workflow was used throughout.
4. All twenty images were inspected at original resolution. They are mechanism-first pixel-strategy diagrams with distinct compositions, no explanatory text or pseudo-text, and no visible logo, watermark, or blocking generation artifact.
5. The package changes no Godot runtime, accepted gameplay authority, system architecture, existing website, or unrelated active work.
6. Comparative conclusion: territorial campaign is the closest to a classic free-form strategic campaign; contract network most directly realizes player-directed acquisition of run-defining effects; multi-front defense and rival parties create the strongest unavoidable opportunity competition; excavation and world-rule drafting most directly turn the progression layer itself into a build object. Mobile-base and headquarters concepts carry the highest risk of overshadowing the existing army autobattler through auxiliary management.
