# Combat Build And Population Framework

Status: Accepted

## Responsibility

This document owns the long-term player-facing grammar for hero roles, build composition, population growth, population-based builds, and the `10 / 18 / 30` landmarks. The core game loop, combat rules, tactical commands, failure, and presentation contracts remain in `tower-autobattler-core.md`.

Exact starting population, growth cadence, hero-tier names and odds, reserve capacity, recruitment economy, trait breakpoints, and specific elemental/status families remain future design work unless this document explicitly fixes them.

## Product Synthesis

The game is a tower-climbing roguelite hero-roster autobattler built from three mutually supporting layers:

- readable autobattler population, roles, traits, and formation composition;
- `The Last Flame`-like ability, status, equipment, and relic build grammar;
- run-based tower decisions that control when the player recruits, strengthens, pivots, accepts risk, or spends build opportunities.

External games are references for design qualities, never implementation authority. The target is not a collage of their feature lists. Every system must reinforce the same promise: assemble a legible hero formation, make build choices with real opportunity cost, and watch those choices resolve on a spatial battlefield.

A central content failure to avoid is pretending every hero is a build core. If most offers claim to be a core, actual cores become hard to identify and most rejected offers feel like junk rather than situational tools.

## Population And Formation

- A run starts below its mature formation size and grows its current population through ordinary run progression. The production compatibility baseline currently authors initial population `7` while granting four initial roster heroes; this preserves the former starting-hero-plus-six deployment capability without inventing a population reward cadence. The value remains authored balance data rather than a permanent landmark, and the later growth curve is not yet fixed.
- Every persistent hero consumes exactly `1` population, regardless of hero tier or rarity.
- Hero tier or rarity controls availability stage, role and mechanic complexity, and expected build responsibility. It never changes population cost.
- `10` is the conventional ordinary endgame formation ceiling. It is a growth destination, not the starting formation size and not a guaranteed body count in every run.
- The existing eighteen legal player candidate cells are the physical full-deployment ceiling. One living unit still occupies one legal cell.
- Explicit heroes, abilities, equipment, or relics may spend real build opportunity to raise effective persistent population above the ordinary `10` ceiling. A high-roll population build may approach or fill all `18` legal cells.
- A reserve may exist as a roster state, but its exact capacity, interaction with current population, and relationship to recruitment flow remain unresolved; no current rule fixes a reserve number or population exception.
- Temporary units come only from explicit authored sources, occupy actual free legal cells, and do not consume persistent roster or reserve population. They cannot exceed physical occupancy, stack units, or keep battle alive after all persistent roster heroes are defeated.

## Hero Role Hierarchy

A formation should expose a readable responsibility hierarchy rather than presenting every recruit as the centerpiece:

- **Core / payoff:** the main destination that converts an assembled engine, state, resource, trait value, or spatial condition into decisive power.
- **Engine / enabler:** creates, sustains, spreads, accelerates, or converts the state or resource that a payoff needs.
- **Functional / bridge hero:** supplies survival, control, targeting, positioning, coverage, transition value, trait bridging, or a temporary solution while the build changes.

Not every hero is marketed as a core. Every recruitable hero must nevertheless have a legible acquisition stage, immediate use, replacement condition, and build value. A hero may be intentionally transitional or functional, but it cannot be an offer whose only explanation is that a better hero was unavailable.

## Build Grammar

A formed archetype can be evaluated through one shared sentence:

> driver or engine + state or resource + payoff + survival + spatial condition

- The **driver/engine** answers how the build starts and repeats.
- The **state/resource** answers what accumulates, circulates, or is maintained.
- The **payoff** answers how investment becomes victory.
- **Survival** answers how the formation stays alive long enough to operate.
- The **spatial condition** answers where units, targets, lanes, adjacency, range, or terrain must align.

Abilities, statuses, equipment, relics, hero roles, and tower rewards should speak this shared grammar. Candidate families such as frost, burn, shock, death, and barrier are useful design spaces, but their detailed reactions, stacks, thresholds, and content lists are not fixed here.

## Population As A Cross-Archetype Chassis

Population is an army/force chassis that can combine with frost, burn, shock, death, barrier, and future archetypes. It is not merely a reward for placing more bodies.

Population content separates three responsibilities:

1. **Expansion sources** raise current population, create persistent recruitment capacity, or enable above-`10` deployment.
2. **Headcount-to-power scaling** converts a precisely named population fact into strength.
3. **Payoff / settlement sources** consume, cash out, or decisively exploit the accumulated formation, shared meter, attacks, deaths, or trait value.

Ordinary content should not solve all three responsibilities at once. A deliberately rare capstone may do so when its rarity and opportunity cost make that compression the point of the reward.

Population scaling may:

- empower one identifiable core;
- grant bounded threshold bonuses;
- strengthen only newly added or qualifying units;
- accelerate a shared meter or trigger cadence;
- pay off allied attacks, defeats, deaths, or another explicitly named event.

Every population-sensitive effect must state exactly what it counts:

- persistent roster heroes deployed at battle start;
- current living friendly units;
- temporary units;
- or an authored trait value.

These count bases are not interchangeable. Effects may include more than one only when the wording and balance explicitly say so. Avoid unbounded all-team offense or health per head: multiplying both team size and every member's full offensive or defensive output creates quadratic growth, obscures contribution, and makes ordinary balance collapse around one chassis.

## Landmarks

- `10` — the ordinary endgame formation ceiling reached through normal run growth.
- `18` — the physical full-board goal for an explicit high-investment population build.
- `30` — a possible hidden achievement for one trait value, not thirty simultaneous physical bodies and not a routine combat breakpoint.

Trait value may exceed physical body count only through explicit authored contributions such as emblems or equipment, relic multipliers, temporary-unit inheritance, or heroes that deliberately contribute more than one point. Normal breakpoints, the hidden achievement's name and reward, and whether selected traits receive secret transformations remain unresolved.

## Opportunity Cost And Acceptance Risks

- Growth above `10` consumes real hero, ability, equipment, or relic opportunity. It cannot be a free background upgrade that every build receives.
- Population offers must still support pivoting and functional needs; a player who does not pursue population should not see the majority of recruitment or reward choices become dead offers.
- Dense formations must preserve role silhouettes, targeting explanations, hit/death feedback, damage/healing readability, selection, and report attribution.
- High-population and temporary-unit combinations must remain within the accepted deterministic occupancy and defeat rules.
- Performance, visual density, path contention, UI capacity, and report density at the `18`-cell ceiling are future implementation acceptance risks, not reasons to lower the confirmed physical ceiling silently.

## Deferred Design Decisions

- future retuning of the authored initial-population baseline and the ordinary growth curve;
- hero-tier names, availability stages, and offer odds;
- reserve capacity and reserve/deployment exchange rules;
- recruitment prices, replacement value, and population-growth economy;
- detailed elemental/status families and their reactions;
- normal trait breakpoints and which traits can exceed body count;
- hidden `30`-value achievement name, reward, discoverability, and secret transformations;
- exact above-`10` source count, rarity, tuning, performance budget, and visual-density budget.
