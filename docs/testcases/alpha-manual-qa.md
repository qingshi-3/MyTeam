# Alpha Manual QA

## Complete Loop

1. Start from the main menu in Chinese and open settings once to verify volume and default battle speed persist and apply on the next launch/battle. The page must not expose an unsupported damage-number option.
2. Focus an unlocked hero, inspect its detail panel, and enter a seeded run through the single `以该英雄出征` action. Verify the detail identifies its army rule, named battlefield command, structured mana cost, and concrete effect. The merchant detail must additionally show a separate `5 金币` cost badge.
3. Choose tower nodes through all three regions. Exercise combat, elite, recruitment, shop, event, rest, and each boss.
4. Recruit soldiers and verify each card shows Chinese gameplay traits and role. In particular, `深渊爬兽` must show both `亡灵` and `野兽`, without exposing `soldier`/`undead`/`beast`. Move soldiers between reserve and the named back/front, upper/middle/lower deployment positions, take items, and verify currency/health/roster changes persist after returning to the menu and resuming.
5. Before each fight, verify enemy composition and the floor rule preview are visible. During combat verify pause, 1x/2x/4x speed, and that the limited hero-command button uses the selected hero's concrete command name. For `加急雇佣`, verify the HUD shows `5 金币/次` and remaining gold: at 5 gold a use changes MP 3/3→2/3, gold 5→0, and summons once; at 4 gold it reports `金币不足` without changing MP, gold, or summons.
6. Defeat the final boss and verify the victory result and meta unlock. Repeat a defeat path and verify defeat/restart.

## Architecture And Presentation

- Open a representative hero, soldier, enemy, and item scene independently; each must instantiate without the game root.
- Inject the one-shot structural test scene during the first catalog-validation instantiation, then inject one ready-pass test scene at a time that calls `GD.PushError` from `NotificationSceneInstantiated`, `_Ready`, its first `_Process`, and `_ExitTree`; every marker must appear exactly as authored in the validation report and prevent registry publication.
- Open each hero-command component scene directly and tune a non-default value. Its generated Chinese effect text and the next runtime execution must reflect that same value; zero/negative counts, scales, durations, divisors, tags, or paid-command costs must fail authoring validation.
- Verify constrained lanes, periodic hazard cells, an objective device, and a boss targeting rule visibly alter combat.
- Verify player formation armor never reduces damage received by an adjacent enemy formation; a lethal floor hazard emits a defeat attributed to `floor`; and the second-region boss summons at least one but no more than two temporary minions.
- At 1600×900 and 1280×720, verify route, roster, deployment, battle HUD, reward, and result text do not overlap.
- At 1280×720 with nine roster members, verify the deployment header, enemy/floor preview, status, and both bottom actions remain fixed and visible while the roster cards scroll independently.
- Verify player-facing cards and results use Chinese role, rarity, and battle-result labels; internal values such as `Ranged`, `Legendary`, or `PlayerDefeat` must not appear.
- Confirm no runtime error mentions `D:\godot\rpg`.

### Window Baseline And Unit Portraits

1. Clear any test-only window override and launch the game normally. Confirm it opens in a resizable `1600×900` window and is neither fullscreen nor maximized; resize it to `1280×720` and back without losing required actions or creating overlapping UI.
2. Inspect all 45 production portrait resources grouped as 8 heroes, 24 soldiers, and 13 enemies. Confirm each resource can be tuned independently, uses existing local animation art, removes distracting transparent padding, centers the readable body, and does not alter the corresponding battle sprite, animation, or facing.
3. At both `1600×900` and `1280×720`, inspect representative small, tall, wide, mounted, hero, soldier, and enemy portraits in hero selection, recruitment, the deployment roster, Army details, and battle-report rows. Confirm every context uses the same authored crop for a unit and varies only its display size.
4. Confirm hero selection uses compact animated library tiles plus one detail panel, while recruitment uses its unit-specific row. Both keep portrait, name, precise responsibility, localized gameplay tags, and key attributes separate from long Chinese descriptions.
5. In recruitment, confirm exactly three choices appear in one vertical column. At `1600×900`, each card is about 148–150 pixels tall with a 104–108 pixel portrait region. At `1280×720`, only the choice list scrolls; the skip or hero-authored conversion action remains fixed, visible, focusable, and clickable.
6. Traverse hero and recruitment choices using mouse, keyboard, and gamepad. Hero focus must change preview only; the fixed detail action and each recruitment row must produce exactly one stable-id activation. Verify disabled heroes, tooltips, focus order, and scrolling. Confirm multi-tag units retain every localized gameplay tag while technical tags remain hidden.
7. Recheck tower-route and item reward/shop cards at both resolutions. Their general `ChoiceCard` size, icon hierarchy, focus behavior, and selection semantics must remain unchanged.

### Hero Library And Semantic Hierarchy

1. At `1600×900`, confirm several compact animated hero tiles fit in multiple columns beside one focused detail panel. At `1280×720`, confirm the library reduces columns without hiding the detail action or return action.
2. Move mouse, keyboard, and gamepad focus across at least three heroes, including one locked hero. Confirm focus changes preview only, locked state is readable without color alone, and only `以该英雄出征` starts a run. One activation must submit one stable id exactly once.
3. Confirm the focused detail groups portrait/identity, health, damage, reach, army rule, command, cost, and availability before paragraph reading. Health and damage each appear exactly once.
4. Inspect the same command in hero detail and battle HUD. Mana must be a blue icon-labelled structured badge and optional gold a separate gold badge; neither cost may be embedded in or recovered from neutral effect prose.
5. Across touched screens, confirm health is green, attack/damage red, mana blue, shield steel gray, healing teal-green, gold/hero identity gold, range light cyan, death/danger crimson, and risk amber. Every colored value retains an icon and/or Chinese label.
6. Open `StatBlock.tscn`, `TraitBadge.tscn`, `ResourceCostBadge.tscn`, and `HeroAbilityPanel.tscn` independently. Each must bind without a level, battle, autoload, or concrete hero id.
7. At both resolutions, recruitment must show exactly three compact vertical choices with readable portrait, identity badges, separated health/damage/reach values, neutral description, and one clear recruit action while bottom actions remain fixed.

## Playability Polish

### Combat Animation And Readability

1. At 1x, 2x, and 4x battle speed, watch at least one melee attack, ranged attack, skill cast, hit, and defeat. Each attack/hit/skill cue must remain visible for its authored one-shot instead of returning to idle in the same rendered frame.
2. Confirm damage and defeat cues play only on their targets, while movement and attack cues play only on their sources. A defeated unit must enter defeat once, briefly hold, fade/hide once, and never reappear during later synchronization.
3. Confirm every player hero carries a high-contrast gold `★ 英雄` identity cue that is readable without color alone. Confirm each unit displays `近` or `远`, with `原兽母皇` classified as near and `骸骨摄政` classified as ranged.
4. Select representative heroes, soldiers, and enemies during battle. The selected-unit panel must show current health, precise role, and exact attack reach, and must refresh when selection changes or the selected unit is defeated.

### Army Summary And Drawer

1. On route, recruitment/reward, shop, event, rest, and deployment screens, verify the compact summary shows hero health, deployed/reserve counts, item count, and gold without displacing the current decision UI.
2. Open the Army drawer on each screen. Verify hero army rule and generated command effect/cost, soldier health/deployment/role/reach, and item effects match the current run. Formation controls must not appear in the drawer.
3. Repeat at 1280x720 and 1600x900. Scroll the drawer to its final row, confirm no clipping, click-through, or pointer leakage, close it, and verify focus/input returns to the underlying screen.
4. Recheck after recruitment, purchase, rest, battle casualty, deployment change, and item gain; both summary and drawer must refresh without leaving the current decision.

### Hero Command Mana And Transactions

1. Enter battle with each hero and confirm the HUD shows 3/3 blue mana segments, the concrete command name, generated effect, one-mana cost, optional gold cost, and an empty/current failure reason as appropriate.
2. Use a legal command three times and verify mana changes 3/3 → 2/3 → 1/3 → 0/3. A fourth attempt must fail with an explicit reason and leave mana and gold unchanged. Pause and 1x/2x/4x speed changes must not alter mana.
3. Start the next battle and confirm mana is restored to 3/3 without being stored in the run save.
4. Exercise insufficient mana, insufficient gold, dead hero, and missing/blocked summon failures. Each failure must preserve both mana and gold and must not create a partial summon or effect.
5. For `加急雇佣`, test 5 gold with an available summon (one summon, 3/3 → 2/3, 5 → 0) and 4 gold or no available summon (no summon and no mana/gold change).

### Battlefield Deployment

1. Confirm the deployment preview shows the real 10x6 floor, current floor rule, enemy start cells, fixed hero anchor, and the same six soldier anchors used by combat. The hero cannot be moved and arbitrary non-anchor cells cannot accept soldiers.
2. Test both drag/drop and select-unit-then-select-cell for reserve-to-empty placement, reserve-to-occupied replacement, deployed-to-empty movement, and deployed-to-occupied atomic swap.
3. Withdraw a deployed soldier to reserve, then reopen deployment and confirm the saved formation is preserved. Each successful operation should commit once; no intermediate duplicated or missing unit may appear.
4. Cancel a selection, drop outside a valid target, attempt an illegal cell, and attempt an operation with no reserve capacity. Run state and save data must remain unchanged.
5. Enter battle after each successful rearrangement and confirm every soldier and enemy spawns on the exact previewed cell with no duplicate living-cell occupancy.

### Movement Presentation Readability

1. At 1x, observe a unit accept one adjacent-cell move. Confirm it begins at the source, shows several intermediate samples with a mild start/landing ease, never overshoots or reverses, and finishes exactly on the destination center. An ordinary idle/health refresh during travel must not pull it to the destination early. The initial cadence should read as roughly 0.24 seconds per uncompressed cell rather than a short slide followed by an accidental-looking pause.
2. Observe a `right → down → left` path in a crowded fight. Confirm every turn reaches the intermediate cell center in event order and no segment becomes diagonal, curved, or corner-cut. Faster units may step more frequently only because they receive authoritative moves more frequently; the presentation must not predict a destination. Trigger a 100-300ms hitch/catch-up burst and confirm the first frame after the hitch still shows the unit at its prior position or between cells rather than already at the newest cell. Subsequent rendered frames may accelerate by ordered cells, never jump backward or teleport, and should reach the newest cell within roughly 0.25 seconds of effective presentation time after the burst stops at the supported frame rate. Do not interpret time with no rendered frames as available interpolation time.
3. Watch the character art, health bar, gold hero marker, near/ranged marker, and pointer selection origin during one normal step. Only the character art may lift by roughly three pixels near mid-step, and it must return exactly to its authored offset at both cell centers. The unit root and every readability marker remain on the straight grid segment without bobbing.
4. Follow at least four consecutive accepted steps at 1x, 2x, and 4x. The move clip must not visibly flash its same opening pose at every cell boundary. At 4x, require continuity and non-flashing behavior rather than every authored frame; action one-shots may override the move clip while root travel and character lift continue.
5. Pause midway through a visible segment and hold for at least one second. Root position, character lift, and movement animation must remain frozen. Resume and confirm travel continues from the same point. Switch 1x → 2x → 4x → 1x during a segment and confirm each change retimes continuously without restart, reversal, snap, or lift discontinuity.
6. Trigger attack, skill, and hit actions while a unit is traveling. The spatial path and character-only lift must continue underneath the one-shot; when the action ends, the base animation must return to `move` if travel remains or `idle` if it has completed.
7. Observe an initial deployment and at least one summon. Both must appear directly at their authored cells with zero decorative lift rather than sliding or bobbing in from the origin. Defeat a unit midway through travel and confirm it stops at its current visible position, clears pending movement, restores the decorative lift to zero, plays defeat once, fades/hides once, and never slides or reappears. Rebind/battle replacement must likewise begin cleanly.
8. Repeat representative one-cell, turn-route, burst, action-overlap, pause/resume, speed-switch, and defeat-interruption observations at 1280x720 and 1600x900. Treat 1x deliberate grid-march readability, 2x followability, and continuous non-flashing 4x fast-forward as the acceptance standard; static screenshots alone are insufficient.

### Engagement, Navigation, Facing, And Action State

1. At 1x and 2x, observe a crowded fight containing melee, ranged, healer, hero, enemy, and summon units. Both teams must begin participating immediately, advance coherently, and only queue briefly behind allies; no unit may pace between two cells, reverse repeatedly, or detour around a visually empty future engagement goal.
2. Select stationary living units during seeking, route obstruction, attack cooldown, healing cooldown, disable, and defeat. The existing selected-unit panel must show an understandable Chinese action reason, and cooldown values must be expressed as player-facing seconds. A dead unit must stop being selectable immediately even while its defeat animation remains visible.
3. Place a low-health unreachable ally and a higher-health reachable ally near a healer. The healer must choose the reachable legal heal, respect line access, retain that protected ally through cooldown, and join ordinary enemy engagement when no wounded ally has a legal plan. A non-healer and otherwise equivalent ranged healer with no wounded ally must both enter combat.
4. Observe units moving right, vertically, then left. Facing changes only when each horizontal segment actually begins; vertical motion retains the prior direction. Player units begin facing right and enemies left. Attacks and heals face their real targets even while visible movement lags behind simulation cells.
5. Confirm defeat locks the last facing and cancels travel. Rebinding the same content restores its team default. Character art may mirror, but health bars, hero identity, near/ranged markers, labels, and other readability UI never mirror.
6. Kill a unit after it has queued movement but before movement resolution. It must emit no move, remain defeated, free its cell for later use, and leave no follower waiting on its dead target or ghost engagement goal.
7. Let a low-health lifesteal unit kill an enemy whose death effect damages the attacker lethally. The attacker must remain at zero health and defeated, perform no later action, and leave its cell usable; ordinary healing or floor healing must not revive it.
8. In a layout where a unit has retained a legal target but its route is temporarily occupied by a stationary ally, observe several movement-ready decisions. The unit must wait on the same target for the bounded lease, then retarget to a reachable alternate at the lease boundary instead of switching immediately or waiting forever.
7. Put a stationary ally on a unit's shortest first step while leaving a longer empty side route. The mover must take the side route within the bounded wait lease rather than repeatedly selecting the blocked step; if that target remains sealed and another enemy is reachable, it must switch at the lease boundary. Also place an attack or heal target exactly two cells away behind one blocked middle cell: neither the range-2 attack nor the range-2 heal may pass through the wall.

### Battle Pace, End Sequence, And Report

1. Compare equal wall-clock intervals at displayed x1, x2, and x4. Verify x1 advances about eight 0.1-second simulation ticks per real second, with x2 and x4 remaining exact 2× and 4× display-mode multiples. Pause and mid-motion speed changes must remain continuous, and the same seeded setup must keep the same terminal tick, outcome, digest, movement-event order, and report facts at every display speed.
2. Observe a terminal attack at normal speed. No later simulation step may occur; commands, pause/speed controls, selection, and board input must lock immediately. Keep the final battlefield visible for about 1.1 real-time seconds, then reach fully opaque black over about 0.45 seconds before the report appears. Repeat with confirm during hold and fade: it may accelerate arrival at the same report, never skip it, duplicate resolution, or route twice.
3. Exercise ordinary victory, final-boss victory, player defeat, and timeout. Each must first show the dedicated report. Continue must route once to combat reward, run-success result, or run-failure result as appropriate; closing or abandoning the screen after terminal resolution must not lose or duplicate the authoritative save mutation.
4. Verify the fixed report banner shows outcome, encounter, deterministic duration, and player command-gold cost. The two-team comparison must keep survivors, casualties/kills, effective damage, effective healing, and remaining health readable; command gold must never appear as an enemy-comparable value.
5. Switch independently among `战局总览`、`输出`、`生存`、`治疗` and `我方`/`敌方`. Identity, portrait, name, hero/role/summon label, and alive/defeated state must remain recognizable while the roster reorders deterministically. Output must show damage/share/DPS/kills; survival taken/shield/final health/lifetime; healing healing/share/HPS/effective events. A side with zero healing must show a deliberate Chinese empty state and overview must still expose every unit.
6. Use tied positive fixtures and all-zero categories. Every tied leader must receive the matching explainable award, zero categories must award nobody, and no composite MVP/score may appear. Verify positive environment damage is labelled outside unit cards and reconciles target-side taken minus opposing credited damage without false unit attribution.
7. Build focused shield, overkill, splash, pierce, death-effect, hazard, heal, overheal, lifesteal, floor-heal, late-summon, and hero-command cases. Effective damage must equal actual health-plus-shield removed, effective healing actual health restored, kills only concrete lethal transitions, and summons independent cards. Initial join tick must be zero; a late summon must use its actual join tick for DPS/HPS; defeat tick writes once; one splash/pierce action counts once; zero/overheal creates no healing event; only successful commands increment command uses. Re-running prior deterministic fixtures must prove these counters do not alter health, outcome, digest, saves, or routing.
8. At 1280×720 and 1600×900, inspect player/enemy overview, output, survival, healing/zero state, ordinary victory, defeat/timeout, and a temporary summon when the fixture allows. Two-column cards must not clip or overlap. The roster alone scrolls; banner, both control groups, and continue remain reachable. Traverse all controls with mouse, keyboard, and gamepad, switch while a card/control is focused, hide/show the screen, and confirm focus remains valid and continue routes exactly once.

### Semantic Theme, Icons, And Choice Cards

1. Inspect hero/gold/legendary headings, mana/shield/player facts, healing/survival gains, enemy/casualty/defeat facts, and warning/timeout states across screens. Confirm their gold, blue, green, red, amber, and neutral treatments are consistent and centralized; every meaning also retains a Chinese word, icon, or symbol, and paragraphs remain neutral rather than fully colorized.
2. Verify health, damage, shield, healing, mana, gold, time, kills/deaths, hero, melee, ranged, risk, and loot icons remain sharp and tint correctly at 1280×720 and 1600×900. Missing optional unit/item artwork must fall back to a stable portrait or semantic role/loot icon without an empty or broken texture.
3. On hero selection, tower route, recruitment/reward, and shop screens, confirm every choice card exposes an independent icon/portrait, title, body, and footer/meta region. Long descriptions must not overwrite metadata. Mouse, keyboard/gamepad focus, activation, disabled state, and tooltip behavior must still select exactly one stable id.
4. Compare responsibility chips for 前卫、战士、远程、辅助、刺客、召唤、炮手、首领 and faction/trait chips for 秩序、沙海、亡灵、野兽、机械、霜寒、中立、敌军. Each meaning must retain Chinese text, use its differentiated catalog icon, and remain identical across hero selection, recruitment, deployment, Army details, reports, and selected-unit facts where it appears. `深渊爬兽` must show both 亡灵 and 野兽 with their matching faction icons.
5. Inspect combat, elite, recruitment, shop, event, rest, and boss tower choices at 1600×900 and 1280×720. All seven primary node icons must be visually distinguishable. Risk must remain a separate icon-plus-`风险` fact; a zero-risk route must keep its node identity rather than using the warning triangle as its main image.
6. Confirm unit cards show responsibility/faction or trait plus health, damage, and exact reach as icon-plus-Chinese-text facts. Hero health and damage must appear once, while army rule, battlefield command/cost, unlocked or locked state, tooltip, and typed activation remain readable and functional.
7. Watch representative multi-frame hero, soldier, and enemy portraits in all five portrait consumers. Each UI portrait must advance through its existing idle animation at a calm pace, pause when its card or ancestor is hidden, and resume when shown. One-frame portraits remain naturally static, and opening/hiding UI must not change any battlefield sprite, cue, frame, facing, or playback state.
8. Recheck dense fact groups, route cards, deployment roster, Army drawer, selected-unit panel, and battle-report rows at both accepted resolutions. No chip may clip, overlap prose, consume an action button, remove Chinese text, or break mouse, keyboard, or gamepad focus traversal.
