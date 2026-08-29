# Tower Autobattler Core Design

Status: Accepted

## Product Identity

The game is a single-player, hero-led tower-climbing army autobattler. The player chooses one mechanically distinct hero, builds a changing army during the run, deploys it before combat, and watches the build resolve through real-time automatic combat with limited hero-command intervention.

The main pleasure is seeing a deliberately constructed army operate. It is not a traditional turn-by-turn tactics game, a PvP economy autobattler, or passive playback with no meaningful preparation or intervention.

## Hero And Soldier Contract

- The hero is the irreplaceable run identity. It changes recruitment, army rules, progression, or battle commands.
- Soldiers are replaceable run-build pieces. They provide bodies, formation geometry, targeting behavior, functions, tags, and combination value.
- A normal deployment contains one hero and up to six soldiers, with up to three reserve soldiers.
- Commander, carry, summoner, hybrid, and solo army shapes are all valid.
- Solo is an explicit hero or build contract, not a baseline property shared by every hero.
- A solo path must still consume the recruitment and reward economy by converting recruits into retainers, traits, skills, materials, or another explicit benefit.
- Recruit conversion is available only when the selected hero authors a conversion rule; ordinary heroes may skip a full-roster recruitment but receive no fallback currency.
- Empty deployment capacity may become power only through an authored hero, item, or upgrade rule.

## Combat

- The player sees enemy composition and relevant floor rules before deployment.
- Formation, lane access, targeting, body blocking, range, area effects, deaths, summons, and environmental rules materially affect outcomes.
- Combat runs automatically in real time.
- The player may pause, change speed, inspect units, and spend a limited command resource on the selected hero's battle commands.
- The displayed battle speeds remain `x1`, `x2`, and `x4`, while their real-time simulation scales are `0.8`, `1.6`, and `3.2`. This changes observation pace only: fixed simulation time, authored cooldowns, deterministic ordering, outcomes, digests, and saved speed choices remain unchanged.
- Every battle begins with the selected hero at its authored maximum mana. The initial heroes author three maximum mana and their commands cost one mana, preserving three successful command uses. Mana is discrete, does not regenerate, is not saved between floors, and begins full again in the next battle.
- Hero selection discloses each hero's army rule, concrete battlefield-command effect, mana cost, and any per-use currency cost before the run starts. During battle, the command HUD keeps the generated effect, current/maximum mana, costs, success, and failure reason visible. A failed command consumes neither mana nor currency.
- Recruitment presents gameplay-relevant faction and unit tags in Chinese. Multi-tag identity is retained (for example, an undead beast shows both traits), while technical catalog tags are not player-facing.
- Unit behavior must be readable. Each soldier starts with one primary job and a small number of clear mechanics; complexity comes from combinations. Heroes carry a gold identity marker plus a text/symbol cue, and every combat unit exposes a near/ranged marker derived from authoritative attack range. Reach up to 3 is near and reach above 3 is ranged, so 2.2/2.3 remain near while 3.5 is ranged. Clicking a visible combat unit opens inspection with its precise role, reach, and health.
- Every living unit participates from the first simulation tick. A unit first takes any legal attack or heal available from its current cell; a newly attackable threat may interrupt a distant pursuit. Otherwise it chooses a reachable target by actual path cost to a legal engagement position, with role preferences and stable target hysteresis as secondary rules.
- A healer protects a wounded ally only when a legal healing plan exists: the ally must be reachable and the eventual heal must satisfy range and line access. It tries other wounded allies when the lowest-health ally is illegal, heals or waits through cooldown for a valid protected ally, and joins ordinary combat when no legal wounded-ally plan exists.
- One living unit occupies one legal grid cell. Strategic target identity and the exclusive final engagement goal are separate. Every unit finishes its ordered choices from the same tick-start occupancy snapshot before a deterministic team-neutral arbitration jointly assigns scarce goals and adjacent moves; losing one contested choice must try a legal next-best choice. Friendly follow chains may advance into cells vacated in the same resolution; stacking, direct swaps, dependency cycles, and movement events for rejected intents remain illegal.
- A future engagement goal is not a terrain wall. Temporarily blocked units retain their target for a bounded wait, then replan or choose another reachable target; staging steps must improve path distance or line access rather than create pacing or fallback-cell oscillation. Death immediately removes the unit and every dependent stale plan from navigation authority.
- Death is terminal unless a separately authored resurrection rule says otherwise. Ordinary healing and lifesteal cannot restore a defeated unit or return it to navigation and combat participation.
- Selecting a unit explains its current action in Chinese, including seeking/engaging, moving, route waiting, attacking, healing/casting, attack cooldown, disabled, and defeated. Player-facing cooldowns use seconds rather than simulation ticks.
- Logical grid cells remain authoritative, and accepted adjacent-cell moves are presented as deliberate, ordered grid-march steps instead of teleports or free-form navigation. Every step follows only its source-to-destination segment with mild ease-in/ease-out, visibly respects each ordered intermediate cell center, and lands exactly on that center without overshoot, reversal, diagonal shortcut, or rounded corner. Initial placement and newly summoned units appear directly at their authoritative cells; ordinary health/idle refresh never snaps a moving unit to the simulation's newest cell.
- Movement presentation keeps catch-up lag bounded to roughly 0.25 seconds of effective presentation time at the supported rendering cadence after movement input stops. Initial authored single-cell targets are about 0.24 seconds at 1x for readable board-game cadence, 0.14 seconds at 2x for followable acceleration, and 0.09 seconds at 4x for continuous fast resolution. A move created during a hitch cannot spend time from before that event or finish its fresh path before any frame shows travel; when rendering resumes, visible continuity takes priority over impossible wall-clock catch-up during frames that were never drawn. Pause freezes an in-progress segment exactly where it is, and speed changes retime from current progress without restart, reversal, or snap.
- A normal grid step may lift only the character art by roughly three pixels near mid-step, returning to exactly zero at both cell centers. Health, hero/role markers, pointer selection origin, and the unit root remain on the authoritative spatial segment. Repeated steps preserve a bounded presentation-only move phase where the authored clip permits it, so consecutive cells do not flash the same opening pose; action one-shots may still override the move clip without stopping root travel or the step lift.
- Attack, skill, and hit actions may visually override the move animation without stopping spatial travel; movement resumes as the base animation while travel remains. Defeat immediately cancels queued travel at the current visible position and prevents later synchronization from sliding or revealing the unit.
- Unit facing is presentation-only. Player units begin facing right and enemies left; horizontal movement faces the actual segment direction, while attacks and heals face their real target. Vertical movement retains the last direction, defeat locks it, and only the character sprite is mirrored so health and readability markers remain upright.
- A normal encounter should resolve in roughly 60–120 seconds once tuning stabilizes.
- A terminal simulation tick stops future stepping immediately. The final battlefield remains visible for about 1.1 real-time seconds, then a full-screen fade reaches black over about 0.45 seconds before navigation. Confirm input may fast-forward this presentation, but it never skips the battle report.
- Every completed battle opens a dedicated report before rewards or run results. A fixed outcome banner shows outcome, encounter, deterministic duration, and player command-gold cost; a compact two-team comparison keeps survivors, casualties/kills, effective damage, effective healing, and remaining health understandable. Player-only command cost is metadata rather than a comparable enemy statistic.
- The report supports independent `战局总览`、`输出`、`生存`、`治疗` and `我方`/`敌方` switching. Unit identity remains fixed while the chosen dimension changes the primary fact and deterministic order: output uses effective damage/share/active-lifetime DPS/kills, survival uses effective damage taken/shield absorbed/final health/active lifetime, and healing uses effective healing/share/active-lifetime HPS/effective healing-event count. A side with no effective healing shows an intentional empty state; all unit facts remain reachable through overview.
- Heroes, soldiers, enemies, bosses, and temporary summons remain independent report entries with alive/defeated state and final facts. Summon rates use their actual join tick rather than whole-battle duration. Positive tied leaders may receive explainable highest-damage, highest-damage-taken, or highest-healing awards; an all-zero category grants none, and the report has no opaque composite rating or MVP formula.
- Report damage counts only health and shield actually removed; overkill is excluded. Report healing counts only health actually restored; overheal is excluded. Concrete lethal sources receive kills, temporary summons own independent statistics, and floor/environment contributions remain unowned rather than being falsely credited.
- Each immutable unit result additionally records join tick, first terminal defeat tick, attack-action count, and effective healing-event count; the battle result records successful hero-command uses. One attack action counts once even when splash or piercing affects several targets, one positive effective heal counts once for its credited source, and failed/zero-effective heals or commands count nothing. Active lifetime runs from join through first defeat or the result tick, clamps to at least one fixed simulation tick, and is the only divisor for report DPS/HPS. Positive target-side effective damage taken not reconciled to opposing credited unit damage may be labelled separately as environment damage but is never assigned to a unit.
- The report continues to rewards after an ordinary victory, to run success after the final victory, and to run failure after defeat or timeout.

## Army Information And Deployment

- Route, recruitment/reward, shop, event, rest, and deployment decisions keep a compact army summary available without leaving the current decision. A read-only overlay drawer provides the hero rule and command, soldier health/deployment/role/reach, and item effects.
- Formation changes are owned only by deployment. The deployment screen shows the same 10×6 floor cells, floor rule previews, enemy starts, fixed hero anchor, and six soldier anchors used by battle setup.
- The hero anchor and six-soldier capacity are fixed for the current product slice. Soldiers support drag/drop and an equivalent select-then-select flow: reserve deployment, occupied-slot replacement, deployed movement, atomic swap, and withdrawal.
- Cancelled or illegal deployment input changes neither run state nor save data. With three soldiers already in reserve, withdrawal is illegal. Each successful formation command commits and saves exactly once; a persistence failure rolls the in-memory formation back to its exact prior state.

## Run Structure

The complete Alpha contains three themed tower regions. Each region has a route containing combat, elite, recruitment, shop, event, rest, and boss opportunities. A run ends in a final boss victory or hero defeat.

Floor rules are visible before combat and are data-driven. The initial content must demonstrate at least:

- constrained paths or blocking terrain;
- periodically dangerous or beneficial cells;
- a controllable objective or environmental device;
- a boss rule that changes normal targeting or deployment priorities.

## Alpha Content

The target content set is:

- eight heroes with materially different rules;
- at least twenty-four recruitable soldier scenes;
- three tower regions with a complete final victory path;
- a useful set of independently authored items covering economy, formation, offense, defense, summons, and solo support;
- meta unlocks for heroes or difficulty, settings persistence, and resumable run state.

The first eight hero identities cover formation command, death/undead economy, beast evolution, engineering constructs, vampiric carry/solo, pure solo dueling, time-command manipulation, and mercenary economy.

## Progression And Failure

- Recruitment, replacement, upgrades, items, events, and route risk form the run build.
- Losses must matter without making one early casualty automatically invalidate a run.
- Victory, defeat, restart, unlock, save, and resume are part of the playable loop rather than debug-only flows.
- The Alpha is accepted as a complete playable product slice, not as commercial launch balance or final art polish.

## Visual Information Hierarchy

- Player-facing presentation uses one shared semantic palette: health green, attack/damage red, mana blue, shield steel gray, healing teal-green, gold/hero identity gold, exact range light cyan, death/danger crimson, and risk amber. Neutral near-white and blue-gray remain authoritative for prose and secondary labels.
- Color is never the only information carrier. Chinese labels and tintable icons continue to identify hero, near/ranged role, outcome, risk, rarity, resources, and status; long descriptions remain neutral and readable.
- Hero selection uses a responsive master-detail hierarchy: compact animated hero-library tiles change only the focused preview, while one fixed detail-panel action confirms the previewed hero by stable id. Rules, commands, costs, and core stats appear once in the detail panel rather than being repeated in every library tile.
- Tower routes, recruitment/rewards, and shop choices retain structured decision hierarchies with distinct icon or portrait, title, body, and footer/meta regions while preserving mouse and keyboard/gamepad selection.
- The intended desktop presentation baseline is a normal, resizable, windowed `1600×900` launch. `1280×720` remains the supported lower bound; required actions and primary unit facts remain reachable at both sizes.
- Heroes, soldiers, and enemies use independently authored crops of their existing animation art anywhere unit identity is chosen or reviewed. Hero selection, recruitment, deployment lists, Army details, and battle reports share the same per-unit portrait source while each context controls only display size.
- Hero selection and recruitment use a unit-specific card hierarchy with a readable portrait plus separate name, precise responsibility, localized gameplay tags, key attributes, and contextual metadata. Tower-route and item cards retain the general choice-card presentation.
- Recruitment presents its three unit choices in one vertical column. The choice list may scroll at the lower supported resolution, while the skip or hero-authored conversion action remains fixed, visible, and usable.
- Hero details and recruitment use distinct reusable visual roles: prominent stat blocks for health/damage/reach, restrained badges for responsibility/faction identity, and structured mana/gold cost badges adjacent to the command name. Costs are never extracted or colored by parsing prose.
- Stable unit statistics, responsibilities, factions, traits, and tower-node identities share one semantic icon vocabulary across screens. Every icon remains paired with Chinese text; exact reach has its own meaning, faction-like gameplay tags reuse their faction icon, and route-node identity remains separate from its independently labelled risk.
- Unit portraits in selection and review contexts play the unit's existing authored idle animation at a calm UI pace. This playback belongs only to the visible UI portrait instance, pauses while hidden, and never controls or changes the battlefield animation instance or its state.

## Non-Goals

- Multiplayer, shared shop pools, PvP rounds, interest-economy imitation, and mandatory three-copy merging.
- Seven hundred mechanically unique units in the first release.
- A strategic overworld, city builder, dialogue campaign, or live service.
- Commercial release certification or final licensed-asset packaging during the Alpha implementation.
