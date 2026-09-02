# Tower Autobattler Core Design

Status: Accepted

## Product Identity

The game is a single-player tower-climbing roguelite hero-roster autobattler. The player chooses a starting hero, grows a changing population and build during the run, deploys a formation on the spatial battlefield, and watches it resolve through real-time automatic combat with limited intervention from an independent tactical-command loadout.

The main pleasure is seeing a deliberately constructed hero team operate across a readable spatial battlefield. It is not a traditional turn-by-turn tactics game, a PvP economy autobattler, a permanent troop-management game, or passive playback with no meaningful preparation or intervention.

## Hero Roster Contract

- Every persistent recruitable player combat character is a hero-grade roster unit with the same recruitment, equipment, progression, defeat, and reserve contract.
- The selected starting hero becomes the first roster member. It may be a meta unlock and explicit starting choice, but it is not an irreplaceable commander category, does not own run-wide rules merely because it was chosen first, and does not own tactical-command resources.
- Every persistent hero consumes exactly one population regardless of tier or rarity. Ordinary run progression grows current population toward a conventional endgame ceiling of `10`; the run does not begin at `10` by contract.
- Every recruitable hero has a legible acquisition stage, immediate use, replacement condition, and build value. A visual asset or simple body is not promoted to roster content without those properties, and not every hero is presented as a build core.
- Equipment remains attached to concrete roster heroes. Relics remain run-level authored content.
- A battle continues while at least one non-temporary player roster hero remains alive. Defeat of the starting hero alone is not terminal.
- Individual defeat is terminal for that battle unless a separately authored resurrection says otherwise. Existing cross-floor health, recovery, replacement, and rest consequences remain; this contract does not add permadeath, wounds, manpower, or a casualty economy.
- The existing eighteen legal player cells are the physical deployment ceiling. Growth above the ordinary `10` ceiling becomes available only through explicit heroes, abilities, equipment, or relics that consume real build opportunity.

Hero role hierarchy, shared build grammar, population scaling, explicit count bases, and the `10 / 18 / 30` landmarks are authoritative in `combat-build-framework.md`.

## Temporary Units

- Temporary units are an optional authored combat primitive, not a persistent roster layer. A complete valid build may contain no temporary-unit source.
- They may be created only by an explicit hero ability, item/relic/status effect, tactical command, encounter, Boss phase, or floor rule.
- Temporary units do not consume persistent roster or reserve population, receive persistent equipment, persist individual save state, or introduce recruitment, replacement, replenishment, troop, detachment, or manpower flows. They still occupy actual free legal cells.
- Their source attribution, deterministic join/death facts, targeting, occupancy, cleanup, and report evidence remain explicit. A temporary unit cannot keep battle active after all player roster heroes are defeated.

## Combat

- The player sees enemy composition and relevant floor rules before deployment.
- Formation, lane access, targeting, body blocking, range, area effects, deaths, explicitly authored temporary units, and environmental rules materially affect outcomes.
- Combat runs automatically in real time.
- The player may pause, change speed, inspect units, and spend shared tactical points through the run's independent tactical-command loadout.
- The displayed battle speeds remain `x1`, `x2`, and `x4`, while their real-time simulation scales are `0.8`, `1.6`, and `3.2`. This changes observation pace only: fixed simulation time, authored cooldowns, deterministic ordering, outcomes, digests, and saved speed choices remain unchanged.
- Every run equips exactly two tactical-command stable ids independently of its heroes. Every battle begins with exactly three shared tactical points; points are discrete, do not regenerate, are not saved between floors, and begin full again in the next battle.
- Authored tactical commands cost one to three tactical points and may be reused subject to their compiled cost, cooldown, use limit, target, and effect preflight. Failed activation consumes no tactical points, currency, cooldown, use count, or partial effect.
- Command selection is a run-level decision independent of starting-hero selection. The first playable slice may provide a deterministic starter loadout and may replace or improve commands only through already-authorized run rewards; there is no command deck, hand, draw pile, rarity economy, or third slot.
- Tactical commands primarily change targeting, position, tempo, protection, cleansing, devices, or explicit temporary reinforcement. A universally optimal raw-damage or healing button is not the intended baseline.
- The tactical HUD keeps both equipped commands, generated effects, current/maximum tactical points, costs, success, cooldown/use facts, targets, and localized failure reasons visible. Pause and speed controls remain observation controls and never consume tactical points.
- Recruitment presents gameplay-relevant faction and unit tags in Chinese. Multi-tag identity is retained (for example, an undead beast shows both traits), while technical catalog tags are not player-facing.
- Unit behavior must be readable. Each roster hero has one primary responsibility and a small number of clear mechanics; complexity comes from combinations. Player roster heroes share a clear team/hero identity rather than one gold commander plus subordinate bodies, and every combat unit exposes a near/ranged marker derived from authoritative attack range. Reach up to 3 is near and reach above 3 is ranged, so 2.2/2.3 remain near while 3.5 is ranged. Clicking a visible combat unit opens inspection with its precise responsibility, reach, health, persistent/temporary identity, and current source where applicable.
- Every living unit participates from the first simulation tick. A unit first takes any legal attack or heal available from its current cell; a newly attackable threat may interrupt a distant pursuit. Otherwise it chooses a reachable target by actual path cost to a legal engagement position, with role preferences and stable target hysteresis as secondary rules.
- A healer protects a wounded ally only when a legal healing plan exists: the ally must be reachable and the eventual heal must satisfy range and line access. It tries other wounded allies when the lowest-health ally is illegal, heals or waits through cooldown for a valid protected ally, and joins ordinary combat when no legal wounded-ally plan exists.
- One living unit occupies one legal grid cell. Strategic target identity and the exclusive final engagement goal are separate. Every unit finishes its ordered choices from the same tick-start occupancy snapshot before a deterministic team-neutral arbitration jointly assigns scarce goals and adjacent moves; losing one contested choice must try a legal next-best choice. Friendly follow chains may advance into cells vacated in the same resolution; stacking, direct swaps, dependency cycles, and movement events for rejected intents remain illegal.
- A future engagement goal is not a terrain wall. Temporarily blocked units retain their target for a bounded wait, then replan or choose another reachable target; staging steps must improve path distance or line access rather than create pacing or fallback-cell oscillation. Death immediately removes the unit and every dependent stale plan from navigation authority.
- Death is terminal unless a separately authored resurrection rule says otherwise. Ordinary healing and lifesteal cannot restore a defeated unit or return it to navigation and combat participation.
- Selecting a unit explains its current action in Chinese, including seeking/engaging, moving, route waiting, attacking, healing/casting, attack cooldown, disabled, and defeated. Player-facing cooldowns use seconds rather than simulation ticks.
- Logical grid cells remain authoritative, and accepted adjacent-cell moves are presented as deliberate, ordered grid-march steps instead of teleports or free-form navigation. Every step follows only its source-to-destination segment with mild ease-in/ease-out, visibly respects each ordered intermediate cell center, and lands exactly on that center without overshoot, reversal, diagonal shortcut, or rounded corner. Initial placement and newly created temporary units appear directly at their authoritative cells; ordinary health/idle refresh never snaps a moving unit to the simulation's newest cell.
- Movement presentation keeps catch-up lag bounded to roughly 0.25 seconds of effective presentation time at the supported rendering cadence after movement input stops. Initial authored single-cell targets are about 0.24 seconds at 1x for readable board-game cadence, 0.14 seconds at 2x for followable acceleration, and 0.09 seconds at 4x for continuous fast resolution. A move created during a hitch cannot spend time from before that event or finish its fresh path before any frame shows travel; when rendering resumes, visible continuity takes priority over impossible wall-clock catch-up during frames that were never drawn. Pause freezes an in-progress segment exactly where it is, and speed changes retime from current progress without restart, reversal, or snap.
- A normal grid step may lift only the character art by roughly three pixels near mid-step, returning to exactly zero at both cell centers. Health, hero/role markers, pointer selection origin, and the unit root remain on the authoritative spatial segment. Repeated steps preserve a bounded presentation-only move phase where the authored clip permits it, so consecutive cells do not flash the same opening pose; action one-shots may still override the move clip without stopping root travel or the step lift.
- Attack, skill, and hit actions may visually override the move animation without stopping spatial travel; movement resumes as the base animation while travel remains. Defeat immediately cancels queued travel at the current visible position and prevents later synchronization from sliding or revealing the unit.
- Unit facing is presentation-only. Player units begin facing right and enemies left; horizontal movement faces the actual segment direction, while attacks and heals face their real target. Vertical movement retains the last direction, defeat locks it, and only the character sprite is mirrored so health and readability markers remain upright.
- A normal encounter should resolve in roughly 60–120 seconds once tuning stabilizes.
- A terminal simulation tick stops future stepping immediately. The final battlefield remains visible for about 1.1 real-time seconds, then a full-screen fade reaches black over about 0.45 seconds before navigation. Confirm input may fast-forward this presentation, but it never skips the battle report.
- Every completed battle opens a dedicated report before rewards or run results. A fixed outcome banner shows outcome, encounter, deterministic duration, successful tactical-command uses, and player command-gold cost; a compact two-team comparison keeps survivors, casualties/kills, effective damage, effective healing, and remaining health understandable. Player-only command cost is metadata rather than a comparable enemy statistic.
- The report supports independent `战局总览`、`输出`、`生存`、`治疗` and `我方`/`敌方` switching. Unit identity remains fixed while the chosen dimension changes the primary fact and deterministic order: output uses effective damage/share/active-lifetime DPS/kills, survival uses effective damage taken/shield absorbed/final health/active lifetime, and healing uses effective healing/share/active-lifetime HPS/effective healing-event count. A side with no effective healing shows an intentional empty state; all unit facts remain reachable through overview.
- Roster heroes, enemies, bosses, and temporary units remain independent report entries with alive/defeated state, persistent/temporary identity, source attribution, and final facts. Temporary-unit rates use their actual join tick rather than whole-battle duration. Positive tied leaders may receive explainable highest-damage, highest-damage-taken, or highest-healing awards; an all-zero category grants none, and the report has no opaque composite rating or MVP formula.
- Report damage counts only health and shield actually removed; overkill is excluded. Report healing counts only health actually restored; overheal is excluded. Concrete lethal sources receive kills, temporary units own independent statistics, and floor/environment contributions remain unowned rather than being falsely credited.
- Each immutable unit result additionally records join tick, first terminal defeat tick, attack-action count, and effective healing-event count; the battle result records successful tactical-command uses. One attack action counts once even when splash or piercing affects several targets, one positive effective heal counts once for its credited source, and failed/zero-effective heals or commands count nothing. Active lifetime runs from join through first defeat or the result tick, clamps to at least one fixed simulation tick, and is the only divisor for report DPS/HPS. Positive target-side effective damage taken not reconciled to opposing credited unit damage may be labelled separately as environment damage but is never assigned to a unit.
- The report continues to rewards after an ordinary victory, to run success after the final victory, and to run failure after defeat or timeout.

## Roster Information And Deployment

- Route, recruitment/reward, shop, event, rest, and deployment decisions keep a compact roster summary available without leaving the current decision. A read-only overlay drawer provides each hero's health, deployment state, responsibility, reach, equipment, build facts, the two equipped tactical commands, and run-level relic effects.
- Formation changes are owned only by deployment. The deployment screen shows the same `10×6` logical floor, floor-rule previews, unchanged enemy starts, and all 18 candidate player cells in columns `0..2` used by battle setup. Floor rules decide which candidate cells are currently legal.
- Current persistent population limits how many roster heroes may deploy, while the eighteen legal cells remain the physical ceiling. Ordinary growth targets `10`; explicit population builds may exceed it and high-roll toward `18`. Every deployed hero occupies one unique legal cell.
- All roster heroes, including the starting hero, support the same drag/drop and select-then-select operations: reserve deployment into an empty cell, reserve replacement of an occupied hero, deployed movement, atomic position swap, and withdrawal. A reserve exists, but its exact capacity is not yet fixed.
- Every visible destination state and rejection message comes from the same non-mutating formation evaluation used by the committing command. At most one drag-hover destination exists, and moving between cells, cancelling, dropping, rebinding, or leaving deployment clears that transient state. Cancelled or illegal deployment input changes neither run state nor save data. Withdrawal obeys the eventual authored reserve limit. Each successful formation command commits and saves exactly once; a persistence failure rolls the complete ordered roster and formation back to its exact prior state.
- Concrete enemy starts use their independently animated idle portraits in deployment, with compact enemy/role/reach or boss redundancy. Hazards, objectives, and blocked terrain remain semantic markers.
- On the pre-battle board, concrete enemies stand on the right and their character art faces left toward the player deployment zone. This deployment-only mirror never flips badges, labels, tooltips, layout, or interaction geometry and does not change the independently owned battle-facing rules or neutral portrait consumers.

## Run Structure

The complete Alpha contains three themed tower regions. Each region has a route containing combat, elite, recruitment, shop, event, rest, and boss opportunities. A run ends in a final boss victory or when no living non-temporary player roster hero remains after a battle.

Floor rules are visible before combat and are data-driven. The initial content must demonstrate at least:

- constrained paths or blocking terrain;
- periodically dangerous or beneficial cells;
- a controllable objective or environmental device;
- a boss rule that changes normal targeting or deployment priorities.

## Alpha Content

The target content set is:

- an initial hero-grade roster with materially different responsibilities, automatic contributions, and build hooks;
- enough recruitable heroes and build offers to make ordinary population growth toward `10` meaningful, while preserving a deliberate high-investment path toward the eighteen-cell physical ceiling;
- three tower regions with a complete final victory path;
- a useful set of independently authored items and relics covering economy, formation, offense, defense, and explicit temporary-unit support;
- an independently authored tactical-command pool with a deterministic two-command starter loadout and three shared tactical points per battle;
- meta unlocks for heroes or difficulty, settings persistence, and resumable run state.

Existing hero and unit content is reclassified pragmatically. Hero-grade roster content retains a distinct responsibility, automatic contribution, and build hook; simpler bodies may remain enemies or explicit temporary-unit templates. Existing useful command effects may become independent tactical commands, but no command remains selected or owned by a concrete hero id.

## Progression And Failure

- Hero recruitment, replacement, upgrades, equipment, relics, tactical-command loadout, events, and route risk form the run build.
- Defeated roster members and cross-floor health must matter without making the starting hero's defeat or one early casualty automatically invalidate a run.
- Victory, defeat, restart, unlock, save, and resume are part of the playable loop rather than debug-only flows.
- The Alpha is accepted as a complete playable product slice, not as commercial launch balance or final art polish.

## Developer Battle Lab

- `战斗实验室` is a developer-only PvE configuration tool. It bypasses tower progression, recruitment, shops, rewards, Meta progression, and the Active Run, and it never becomes a PvP or alternate combat mode.
- A Lab configuration consists of uniquely identified player-hero instances, one explicit primary-hero instance that supplies the team HeroRule, published PvE unit instances, exact `10×6` physical cells, per-player-hero equipment instances, player-team Relic stacks, placement mode, explicit population input, and one fixed integer seed. Shared authored definitions remain read-only; renaming or reordering instance ids cannot silently change the primary hero.
- Formal mode enforces the production player `3×6` deployment region, explicit current/effective population up to the production physical ceiling `18`, one-cell occupancy, floor legality, three Equipment slots, and the production enemy region. Free mode permits either side on any otherwise legal physical cell while retaining bounds, forbidden cells, unique instance identity, and one unit per cell; the screen must continuously label the state `自由实验配置` without relying on colour.
- Every edit is validated before battle preparation and refreshes population, Equipment, Relics, Trait contribution/tiers, prepared key attributes, readiness, and concise Chinese rejection reasons. `BattleSimulation` clamping or nearest-free-cell repair is never placement authority.
- Starting freezes a deep configuration snapshot and creates fresh production battle runtime state. Returning restores the unchanged editable configuration; health, Statuses, cooldowns, counters, modifiers, and other battle mutation never copy back. Reset, replacement, exit, and re-entry rebuild or dispose every Battle-owned scope.
- Battle controls provide pause, continue, one fixed tick while paused, displayed x1/x2/x4 speed, reset, and return to configuration through the production Battle screen boundary. Equal canonical configuration plus seed must reproduce the same terminal result and deterministic event/digest projection.
- Built-in presets are authored data. User presets are versioned JSON under `user://battle_lab/` and contain stable ids and mutable Lab configuration only, never Resources or battle runtime state. Lab use must produce zero production Meta, Settings, Active Run, or schema-v4 save writes.
- The built-in `冰霜体系验证` preset uses ordinary published Frost Equipment, Trait, Status, and control-resistance authoring to make multi-source Frost, threshold Freeze, attack-speed growth, shortened control duration, and Status lifecycle feedback observable. Concrete content ids are preset data only and never runtime dispatch.

## Visual Information Hierarchy

- Player-facing state follows `space/shape/motion → icon/color → concise text → on-demand detail`. Persistent surfaces prioritize the current objective, key resources, primary action, and immediately decision-relevant facts; interaction exposes comparison state, while exact rules and values remain reachable through details or tooltips instead of being repeated across page copy, summaries, cards, and actions.
- Deployment is a direct-manipulation board. Empty candidate cells carry no persistent coordinate or deployment prose; terrain, boundary, unit imagery, stable semantic symbols, and distinct default/hover/focus/selected/drag/legal/illegal/swap/success/failure states communicate the deployment zone and action result without relying only on color. Exact names, coordinates, rules, and invalid reasons remain available on demand.
- Mouse selection, select-then-cell, drag/drop, and keyboard/gamepad focus are equally supported player paths for formation and run decisions. A visible control is not accepted as operable until player-like input reaches the same typed action and state feedback without duplicate activation.
- Player-facing presentation uses one shared semantic palette: health green, attack/damage red, tactical points blue, shield steel gray, healing teal-green, player-hero identity gold, exact range light cyan, death/danger crimson, and risk amber. Neutral near-white and blue-gray remain authoritative for prose and secondary labels.
- Color is never the only information carrier. Chinese labels and tintable icons continue to identify hero, near/ranged role, outcome, risk, rarity, resources, and status; long descriptions remain neutral and readable.
- Starting-hero selection uses a responsive master-detail hierarchy with one deterministic initial selection. Hover and focus only expose their ordinary visual states; mouse click or keyboard/gamepad `ui_accept` explicitly selects a compact animated hero-library tile and binds the detail panel. One fixed detail-panel action starts a run only for that explicitly selected unlocked hero by stable id. Responsibility, automatic contribution, build hook, gameplay tags, and core stats appear once in the detail panel; tactical commands are selected and explained independently.
- Tower routes, recruitment/rewards, and shop choices retain structured decision hierarchies with distinct icon or portrait, title, body, and footer/meta regions while preserving mouse and keyboard/gamepad selection.
- The intended desktop presentation baseline is a normal, resizable, windowed `1600×900` launch. `1280×720` remains the supported lower bound; required actions and primary unit facts remain reachable at both sizes.
- Roster heroes, enemies, and concrete temporary units use independently authored crops of their existing animation art anywhere unit identity is chosen or reviewed. Starting selection, recruitment, deployment lists, roster details, and battle reports share the same per-unit portrait source while each context controls only display size.
- Hero selection and recruitment use a unit-specific card hierarchy with a readable portrait plus separate name, precise responsibility, localized gameplay tags, key attributes, and contextual metadata. Tower-route and item cards retain the general choice-card presentation.
- Recruitment presents its three hero choices in one vertical column. The choice list may scroll at the lower supported resolution, while the skip action remains fixed, visible, and usable; there is no universal soldier conversion or fallback economy.
- Hero details and recruitment use distinct reusable visual roles: prominent stat blocks for health/damage/reach and restrained badges for responsibility/faction identity. Tactical-command details use structured tactical-point/gold cost badges adjacent to the command name. Costs are never extracted or colored by parsing prose.
- Stable unit statistics, responsibilities, factions, traits, and tower-node identities share one semantic icon vocabulary across screens. Every icon remains paired with Chinese text; exact reach has its own meaning, faction-like gameplay tags reuse their faction icon, and route-node identity remains separate from its independently labelled risk.
- Unit portraits in selection and review contexts play the unit's existing authored idle animation at a calm UI pace. This playback belongs only to the visible UI portrait instance, pauses while hidden, and never controls or changes the battlefield animation instance or its state.

## Non-Goals

- Multiplayer, shared shop pools, PvP rounds, interest-economy imitation, and mandatory three-copy merging.
- A universal persistent soldier, troop, detachment, manpower, replenishment, or casualty-economy system.
- A command deck, hand, draw pile, rarity economy, third command slot, regenerating tactical points, or starting-hero ownership of commands.
- Seven hundred mechanically unique units in the first release.
- A strategic overworld, city builder, dialogue campaign, or live service.
- Commercial release certification or final licensed-asset packaging during the Alpha implementation.
