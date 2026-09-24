# Tower Autobattler Core Design

Status: Accepted

Reading boundary (2026-09-06): existing exact rules remain the current contract until explicitly replaced. Sections labelled confirmed direction do not fix their pending parameters or authorize implementation. Architecture/compatibility details belong to `../system-design/`; implementation and acceptance evidence belong to `../work-items/`.

## Product Identity

The game is a single-player tower-climbing roguelite hero-roster autobattler. The player chooses two heroes from six random opening candidates, grows a changing population and build during the run, deploys a formation on the spatial battlefield, and watches it resolve through real-time automatic combat with limited intervention from an independent tactical-command loadout.

The main pleasure is seeing a deliberately constructed hero team operate across a readable spatial battlefield. It is not a traditional turn-by-turn tactics game, a PvP economy autobattler, a permanent troop-management game, or passive playback with no meaningful preparation or intervention.

## 伤害、公式与统一防御

2026-09-21 按 M04-D03 及用户“把现有的都修复掉”实施授权统一：伤害由具体公式产生；攻击、法强、生命、层数或固定值等只有被公式明确读取时才参与。它们是输入属性，伤害是计算结果，不另设物理／魔法攻击属性或双防。

所有普通伤害统一受到受击方防御减伤，技能外观和公式输入不改变防御类别。真实伤害保留为绕过防御的明确例外，仍遵守适用的公共伤害修饰、护盾吸收、来源归属与死亡规则。护盾在减伤之后吸收；战报只计实际消耗的护盾与生命，不计过量伤害。

本轮保留既有防御减伤曲线和技能系数。原先标为魔法伤害的技能现在也受到防御影响，不自动加系数抵消这项变化；具体强度待试玩调优。界面以攻击、法强表示公式因子，以伤害／真实伤害表示结果，防御只有一个词条。

## Large Enemy Bodies And Trample

The EE03 elite uses a real body wider than one deployment cell (current diameter 1.4 cells, calibrated to its artwork on 2026-09-21). Deployment cells are anchors; the complete body must fit the arena and terrain without overlapping another body. Size does not automatically grant control immunity or change population cost. Existing roster sizes remain unchanged.

Enemy kits are authored for their mechanic; they do not require the hero roster’s fixed active/passive pairing. EE03 has one mana-full active charge and a visible personal mana bar. It spends the full pool at charge start; ordinary refill pauses through windup, travel and recovery, and resumes when the action ends. Control or an invalid charge target preserves full mana; there is no additional periodic/hidden cooldown.

Its charge telegraphs a fixed direction, then moves through the line while pushing contacted bodies sideways. Each enemy can take damage once per charge; allies are moved without friendly damage. A blocked side path can stop the charger, and control or defeat cancels it. The charge ends in a recovery window. Its unit scene, skill definition and visual effects are independent resources; current tuning and encounter placement live in `../work-items/active/enemy-trample-charge.md`.

## Hero Roster Contract

- Every persistent recruitable player combat character is a hero-grade roster unit with the same recruitment, equipment, progression, defeat, and reserve contract.
- Ordinary recruitment excludes every currently owned hero content identity, including reserves, and never repeats that identity within one candidate batch. Acquisition rechecks ownership; having previously seen an unchosen hero does not itself make that hero ineligible. A short eligible pool offers fewer candidates; an exhausted opportunity may be skipped. Explicit special copies require their own content contract. Stars are not used; material cultivation remains a direction awaiting its own runtime implementation. See `../work-items/active/ordinary-recruitment-uniqueness.md` for the uniqueness baseline.
- The two selected opening heroes become ordinary roster members. Neither is an irreplaceable commander, owns run-wide rules merely because it was chosen first, or owns tactical-command resources. Legacy ordering remains a compatibility detail.
- Every persistent hero consumes exactly one population regardless of tier or rarity. Ordinary run progression grows current population toward a conventional endgame ceiling of `10`; the run does not begin at `10` by contract.
- Every recruitable hero has a legible acquisition stage, immediate use, replacement condition, and build value. A visual asset or simple body is not promoted to roster content without those properties, and not every hero is presented as a build core.
- Equipment remains attached to concrete roster heroes. Relics remain run-level authored content.
- A battle continues while an original non-temporary player roster hero is alive or has a separately authored pending same-identity return. Borrowed enemy allegiance does not create roster membership. Defeat of the starting hero alone is not terminal; pending return waits for a legal position and remains subject to the normal battle time limit.
- Individual defeat is terminal for that battle unless a separately authored resurrection says otherwise. Existing cross-floor health, recovery, replacement, and rest consequences remain; this contract does not add permadeath, wounds, manpower, or a casualty economy.
- The existing eighteen legal player cells are the physical deployment ceiling. Growth above the ordinary `10` ceiling becomes available only through explicit heroes, abilities, equipment, or relics that consume real build opportunity.

Hero role hierarchy, shared build grammar, population scaling, explicit count bases, and the `10 / 18 / 30` landmarks are authoritative in `combat-build-framework.md`.

## Opening And Recruitment Supply

The bounded 2026-09-16 execution replaces free choice of one hero plus a random companion with six unique random candidates and exactly two player selections. Selection can be cancelled; candidates and selections survive menu navigation and reload. Confirmation opens the first ordinary battle's deployment screen without granting a third companion. The current test campaign makes its opening pool available independently of the legacy two-hero account unlock list; it does not erase account history.

The run pool contains 14 heroes across four acquisition tiers (4/4/4/2), supporting poison, death/summon and frost attack builds plus shared functions. Excluded run-pool heroes remain published for laboratory use and existing rosters. Tier means acquisition stage, not stars or cultivation rank; this pass does not retune hero combat attributes. The 300-hero long-term library and five-tier budget remain future content planning.

| Opportunity (one-based floor) | Tier 1 | Tier 2 | Tier 3 | Tier 4 |
| --- | ---: | ---: | ---: | ---: |
| Opening | 60 | 40 | 0 | 0 |
| Floors 1–3 | 65 | 35 | 0 | 0 |
| Floors 4–6 | 35 | 45 | 20 | 0 |
| Floors 7–10 | 15 | 30 | 45 | 10 |
| Floors 11–15 | 0 | 15 | 50 | 35 |

These are initial tuning weights. Each slot draws an allowed nonempty tier first, then a uniformly random eligible hero within it, without replacement. Ownership and earlier slots change the remaining distribution. Empty tiers redistribute only among already allowed nonempty tiers; no forbidden-tier or duplicate filler is permitted. Ordinary recruitment still offers up to three heroes and grants one. Stage depends on absolute floor progress, independent of roster strength and wall time. This is stage supply, with no build recommendation, seen-card downweighting or guaranteed core acquisition. Exact roster, configuration and checks: `../work-items/active/recruitment-opening-and-tiers.md`.

## Hero Mana And Automatic Skills

所有有技能的单位都显示战斗技能状态，不按英雄、敌人或阵营过滤。头顶第二条优先显示主要技能的真实法力、怒劲或周期进度，空资源也保留；仅有条件／常驻被动的单位显示对应触发／生效状态，不虚构回蓝。颜色同时配简短文字标识，完整技能状态、触发条件及次数在点选详情中查看。满资源不保证立刻出手，控制、目标不合法或当前动作未完成时保留等待／排队信息。首领读取当前阶段技能；单位死亡隐藏头顶资源，返场恢复当前状态。显示本身不改变技能原有释放条件、数值或动作顺序。

The designed roster keeps HC01 连弩手 and HC03 铁甲卫 and adds HC04–HC30, one newly authored hero for each BC01–BC27 mechanism under the user's 2026-09-12 implementation authorization. Each new hero has a fixed mana skill and fixed passive; multiple event bindings may implement one passive. Three stationary, melee, and ranged laboratory dummies remain available on either side and are excluded from recruitment. HC02 and the old roster remain excluded. Names, numbers, exact submechanism choices, and full build supply are provisional; authoring does not certify balance or player acceptance. This batch's implementation and validation history is maintained in `../work-items/active/bc-hero-roster.md`; the current hero drafts are in `../design-discussion/04-content-validation/content-drafts.md`. Existing HC01/HC03 evidence remains separately recorded in `../work-items/active/designed-heroes-and-test-dummies.md`. Other G/M/R discussions have not been merged by this content pass.

2026-09-19局部实施增加HC37军团斗士、HC38怒拳斗士、HC39铁钩机兵。军团以短时双向决斗配合受普攻反击吸血；怒拳以近期生命承伤积怒劲，消费为短盾和锁向范围重拳；机兵以实体飞钩抓沿途首敌，低血每战一次护盾提供容错。保持固定一主动一被动、自动施放、战后清理；军团与机兵使用满蓝条件，怒拳于2026-09-20改为下述无蓝条件。不迁入原作全部机制。当前暂定机兵为招募二档，军团／怒拳为三档，均有实验室专用预设；具体数值由各自技能资源维护，平衡与观感仍待试玩。实施与验证归`../work-items/active/duel-grit-hook-heroes.md`。

HC38怒拳斗士无蓝：怒劲满、生命从50%及以上跌至50%以下、生命低于20%均能触发重拳。20%濒死每战一次，复活不重置；半血触发在生命恢复至50%及以上后可重置。每个单位独立保存条件技能请求，同一技能的待执行原因合并，等当前蓄力、释放和收势完整结束后再施放下一次；不覆盖正在执行的动作。施放时消费当前怒劲，零怒劲仍有基础伤害，无额外护盾。控制／缺少合法目标时等待，死亡清理队列，不把濒死阈值当作免死。

HC01 retains Battle-local uncapped attack-speed stacks, base target-switch reset, laboratory U01 retention, and a speed-linked three-arrow test volley without per-arrow basic mana. HC03 retains temporary nearby taunt and armor plus current-armor reflection of ordinary hits. New mechanics use explicit authored readers and payoffs: current shield, independent shield grants, actual health lost, actual healing, overhealing, current statuses, and Battle history are different inputs. The team's shared resonance is Battle-local content energy, separate from personal hero mana, tactical points, and Run currency; it has no benefit until an explicit ability spends it. No hero's ability counters, consumed bodies, controlled enemies, or accumulated battle attributes carry into the next battle.

- Each persistent roster hero owns its own automatic skill and, when required by that skill, Battle-local mana. HC38 uses explicit conditions and has no mana pool. Starting-hero identity never selects another hero's skill or grants a shared hero-mana budget. The existing independent tactical-command product is separate; it is not the hero skill interface.
- Mana recovers with fixed simulation time, once per basic attack action (including a healer's basic healing action), and from effective damage received. Splash and piercing do not multiply attack recovery. Damage recovery counts actual health plus shield removed relative to maximum health, with an authored per-hit cap; zero damage and defeated units grant no recovery.
- The first content pass authors one mana-full skill per player roster template. Base maximum mana equals its authored skill cost; sourced maximum-mana modifiers change the full threshold. Starting mana and all three recovery rates are authored independently. Every new battle resets current mana to the effective starting value clamped to its maximum.
- Full mana attempts automatic casting, not a manual button. Action control, cast recovery, or no legal target preserves mana. Offensive single-target skills use a living enemy within attack reach and line access, retaining the current target when legal; healing skills select the lowest-health-ratio wounded ally in reach and line access, including self. Self-targeted skills do not require an enemy target.
- A successful mana skill empties that hero's mana and enters its authored recovery window (at least one tick); ordinary attacks, movement and ordinary mana recovery pause in that window. Explicit authored post-cast mana-refund effects remain separate from ordinary recovery. Failed execution restores mana, recovery, effects and presentation facts together. Damage that kills a target skips later status/heal operations on that defeated target instead of recreating it.
- Reused temporary-unit templates do not inherit the persistent hero mana cycle. Authored non-mana Boss/passive behavior remains its own mechanism.
- Mana bars, ready markers, skill names and descriptions, equipment slots, shields, and status stacks/timing expose actual runtime facts. A cast flash is driven by a successful cast event, not inferred from a change in mana. Numerical tuning and player experience remain pending hands-on acceptance.

The 2026-09-12 displacement extension adds HC31–HC36 with fixed active/passive drafts for charge, leap, blink, knockback, pull and gathering. They use the same roster contract and laboratory; names, numbers and opening full mana are provisional. Scope and the explicit no-build/no-runtime-validation boundary are recorded in `../work-items/active/hero-displacement.md`.

## Temporary Units

- Temporary units are an optional authored combat primitive, not a persistent roster layer. A complete valid build may contain no temporary-unit source.
- They may be created only by an explicit hero ability, item/relic/status effect, tactical command, encounter, Boss phase, or floor rule.
- Temporary units do not consume persistent roster or reserve population, receive persistent equipment, persist individual save state, or introduce recruitment, replacement, replenishment, troop, detachment, or manpower flows. They still require an actual legal nonoverlapping battle-space position.
- Their source attribution, deterministic join/death facts, targeting, occupancy, cleanup, and report evidence remain explicit. A temporary unit cannot keep battle active after all player roster heroes are defeated and have no authored pending return.

Authored death effects distinguish consuming a temporary ally, using an eligible enemy corpse to create a fixed temporary product, and returning the same hero identity. Consumption produces a death for explicit death readers without pretending the enemy killed the unit or adding a large damage event. A corpse reserved for same-identity return cannot also be spent for a corpse product. Limited resurrection retains Battle history and used counts, does not replay opening abilities, and requires a legal body position. Temporary control preserves an enemy identity and ability loadout for its authored duration, then restores its team; it never grants post-battle recruitment or retained ownership. Exact eligibility and numbers remain specific hero content.

## Combat

- The player sees enemy composition and relevant floor rules before deployment.
- Formation, lane access, targeting, body blocking, range, area effects, deaths, explicitly authored temporary units, and environmental rules materially affect outcomes.
- Combat runs automatically in real time.
- The player may pause, change speed, inspect units, and spend shared tactical points through the run's independent tactical-command loadout.
- The displayed battle speeds remain `x1`, `x2`, and `x4`, while their real-time simulation scales are `0.8`, `1.6`, and `3.2`. This changes observation pace only: fixed simulation time, authored cooldowns, deterministic ordering, outcomes, digests, and saved speed choices remain unchanged.
- Every run equips exactly two tactical-command stable ids independently of its heroes. Every battle begins with exactly three shared tactical points; points are discrete, do not regenerate, are not saved between floors, and begin full again in the next battle.
- Authored tactical commands cost one to three tactical points and may be reused subject to their compiled cost, cooldown, use limit, target, and effect preflight. Failed activation consumes no tactical points, currency, cooldown, use count, or partial effect.
- Command selection is a run-level decision independent of starting-hero selection. The first playable slice may provide a deterministic starter loadout and may replace or improve commands only through already-authorized run rewards; there is no command deck, hand, draw pile, rarity economy, or third slot.
- 2026-09-06 confirmed direction: review player intervention as one topic covering pre-battle automatic-behavior customization and in-battle tactical input. Its future entry point need not be equipment or cards. No replacement of the current two-command/three-point contract has been decided; hero mana skills remain automatic. This is an open design topic, not authorization to add a new input system.
- Tactical commands primarily change targeting, position, tempo, protection, cleansing, devices, or explicit temporary reinforcement. A universally optimal raw-damage or healing button is not the intended baseline.
- The tactical HUD keeps both equipped commands, generated effects, current/maximum tactical points, costs, success, cooldown/use facts, targets, and localized failure reasons visible. Pause and speed controls remain observation controls and never consume tactical points.
- Recruitment presents gameplay-relevant faction and unit tags in Chinese. Multi-tag identity is retained (for example, an undead beast shows both traits), while technical catalog tags are not player-facing.
- Unit behavior must be readable. Each roster hero has one primary responsibility and a small number of clear mechanics; complexity comes from combinations. Player roster heroes share a clear team/hero identity rather than one gold commander plus subordinate bodies, and every combat unit exposes a near/ranged marker derived from authoritative attack range. Reach up to 3 is near and reach above 3 is ranged, so 2.2/2.3 remain near while 3.5 is ranged. Clicking a visible combat unit opens inspection with its precise responsibility, reach, health, persistent/temporary identity, and current source where applicable.
- Every living unit participates from the first simulation tick. A unit first takes any legal attack or heal available from its current continuous position; a newly attackable threat may interrupt a distant pursuit. Otherwise it chooses a reachable target by actual path cost to a legal engagement position, with role preferences and stable target hysteresis as secondary rules.
- A healer protects a wounded ally only when a legal healing plan exists: the ally must be reachable and the eventual heal must satisfy range and line access. It tries other wounded allies when the lowest-health ally is illegal, heals or waits through cooldown for a valid protected ally, and joins ordinary combat when no legal wounded-ally plan exists.
- Deployment is one unit per legal cell. At battle start each deployed cell center becomes the unit's initial continuous logical position; after that, units may move and stop anywhere inside legal battle space. Each living unit has an authored circular body radius; ordinary ground movement cannot pass through living ground bodies or blocked terrain. Explicit leap/blink exceptions follow the skill-displacement contract below. Strategic target identity and a reserved engagement position around that target are separate. Movement planning reads one tick-start snapshot and resolves deterministically without rigid-body bounce or push.
- A future engagement position is not a terrain wall. Multiple attackers reserve distinct reachable positions around a target when space permits. Local avoidance and swept nonpenetration prevent tunnelling and persistent overlap; temporarily blocked units retain their target for a bounded wait, then replan, stage, or choose another reachable target. Death immediately releases body occupancy and every dependent stale plan from navigation authority.
- Ordinary attack delivery is explicitly authored as Melee, Projectile, or Beam. Melee and Beam resolve immediately; Beam also displays a short connecting effect. Authored Projectile attacks draw before releasing a straight-moving entity: windup follows attack speed, release aligns with the authored animation progress, and collision alone resolves damage. Control cancels an unreleased shot; release also rechecks a living enemy target, reach and line access. Ordinary attack mana and attack count occur once at actual release, not at draw start or per collision. In-flight projectiles do not home or hit allies; bodies intercept by contact order, blocked terrain stops them, and lifetime bounds misses. A dead intended target does not redirect a released shot; a dead source does not cancel it while battle remains active. Terminal battle completion discards pending and in-flight shots. Existing piercing projectiles hit at most two distinct enemies along the flight path, with the secondary hit retaining 35% raw damage.
- Death is terminal unless a separately authored resurrection rule says otherwise. Ordinary healing and lifesteal cannot restore a defeated unit or return it to navigation and combat participation.
- Selecting a unit explains its current action in Chinese, including seeking/engaging, moving, route waiting, attacking, healing/casting, attack cooldown, disabled, and defeated. Player-facing cooldowns use seconds rather than simulation ticks.
- The battle simulation's continuous logical position is authoritative. Presentation projects that position into current board pixels and interpolates successive simulation positions without imposing cell stops, hops, or transform authority of its own. Initial deployment and newly created temporary units appear directly at their authoritative positions; ordinary health/idle refresh never snaps a moving unit to a newer simulation position.
- Movement remains visually continuous, tracks the actual route and target, freezes on pause, and does not snap or reverse on speed changes. Attack animation does not stop actual travel; defeated units cannot resume visible movement. Detailed sample/interpolation, facing and character-only motion contracts live in `../system-design/tower-autobattler-architecture.md`, not in a second gameplay specification.
- A normal encounter should resolve in roughly 60–120 seconds once tuning stabilizes.
- A terminal simulation tick stops future stepping immediately. The final battlefield remains briefly visible before the transition; confirm may fast-forward this presentation but never skip the battle report. Current hold/fade timings belong to the system presentation baseline.
- Every completed battle opens a dedicated report before rewards or run results. A fixed outcome banner shows outcome, encounter, deterministic duration, successful tactical-command uses, and player command-gold cost; a compact two-team comparison keeps survivors, casualties/kills, effective damage, effective healing, and remaining health understandable. Player-only command cost is metadata rather than a comparable enemy statistic.
- The report supports independent `战局总览`、`输出`、`生存`、`治疗` and `我方`/`敌方` switching. Unit identity remains fixed while the chosen dimension changes the primary fact and deterministic order: output uses effective damage/share/active-lifetime DPS/kills, survival uses effective damage taken/shield absorbed/final health/active lifetime, and healing uses effective healing/share/active-lifetime HPS/effective healing-event count. A side with no effective healing shows an intentional empty state; all unit facts remain reachable through overview.
- Roster heroes, enemies, bosses, and temporary units remain independent report entries with alive/defeated state, persistent/temporary identity, source attribution, and final facts. Temporary-unit rates use their actual join tick rather than whole-battle duration. Positive tied leaders may receive explainable highest-damage, highest-damage-taken, or highest-healing awards; an all-zero category grants none, and the report has no opaque composite rating or MVP formula.
- Report damage counts only health and shield actually removed; overkill is excluded. Report healing counts only health actually restored; overheal is excluded. Concrete lethal sources receive kills, temporary units own independent statistics, and floor/environment contributions remain unowned rather than being falsely credited.
- Each immutable unit result additionally records join tick, first terminal defeat tick, attack-action count, and effective healing-event count; the battle result records successful tactical-command uses. One attack action counts once even when splash or piercing affects several targets, one positive effective heal counts once for its credited source, and failed/zero-effective heals or commands count nothing. Active lifetime runs from join through first defeat or the result tick, clamps to at least one fixed simulation tick, and is the only divisor for report DPS/HPS. Positive target-side effective damage taken not reconciled to opposing credited unit damage may be labelled separately as environment damage but is never assigned to a unit.
- The report continues to rewards after an ordinary victory, to run success after the final victory, and to run failure after defeat or timeout.

### Explicit Skill Displacement

- Authored charge, knockback, pull and gathering move real continuous ground positions, stopping at bodies or terrain. A blocked path does not permit tunneling. Displacement that cannot produce a legal move is not cast and spends no mana.
- Leap temporarily exempts the mover from ground-body blocking and can cross intervening bodies and terrain; its reserved landing must remain inside clear terrain and outside living ground bodies. This grants no invulnerability. Blink changes directly to a legal reserved endpoint, without an attackable traversed path; existing projectiles do not gain fictitious hits along a teleport segment.
- The mover pauses normal attacks, new active skills and ordinary navigation during displacement; ongoing statuses still elapse. Stun interrupts the payoff of a self-directed charge/leap/blink. An interrupted living leap finishes its safe landing. A pushed or pulled victim's existing stun does not cancel that forced motion.
- Arrival damage and control resolve at the actual stopping position. A single-target charge or leap must still reach its original target. Ground gathering preserves body spacing; it never stacks all bodies at one point. HC34–HC36 explicitly exclude Boss targets from forced movement; this is not general immunity to control.
- Trajectories and reservations belong to the current battle. Death, allegiance changes and battle completion clean up the associated motion; no displacement or accumulated skill state transfers to another encounter.

## Roster Information And Deployment

- Equipment rewards and purchases enter the run's unequipped inventory as unique instances. All persistent heroes share the same three-slot rule. In preparation, players may equip, transfer or remove equipment without a fee; replacing a slot returns its previous item to inventory rather than destroying it. No sale, crafting or paid-removal economy is implied.
- Reward acquisition is shown before continuing, with immediate optional allocation. Deployment and the roster drawer share equipment controls; click/select and drag/drop express the same validated command. Invalid operations, cancellation and persistence failure do not consume or duplicate equipment. Equipment editing is locked while a battle is active.

- Route, recruitment/reward, shop, event, rest, and deployment decisions keep a compact roster summary available without leaving the current decision. A read-only overlay drawer provides each hero's health, deployment state, responsibility, reach, equipment, build facts, the two equipped tactical commands, and run-level relic effects.
- Formation changes are owned only by deployment. The deployment screen shows the same `10×6` logical floor, floor-rule previews, unchanged enemy starts, and all 18 candidate player cells in columns `0..2` used by battle setup. Floor rules decide which candidate cells are currently legal.
- Current persistent population limits how many roster heroes may deploy, while the eighteen legal cells remain the physical ceiling. Ordinary growth targets `10`; explicit population builds may exceed it and high-roll toward `18`. Every deployed hero occupies one unique legal cell.
- All roster heroes, including the starting hero, support the same drag/drop operations: reserve deployment into an empty cell, reserve replacement of an occupied hero, deployed movement, atomic position swap, and withdrawal into the reserve return area. Clicking a hero selects/inspects it; clicking another cell never commits a pending formation move. A reserve exists, but its exact capacity is not yet fixed.
- Every visible destination state and rejection message comes from the same non-mutating formation evaluation used by the committing command. At most one drag-hover destination exists, and moving between cells, cancelling, dropping, rebinding, or leaving deployment clears that transient state. Cancelled or illegal deployment input changes neither run state nor save data. Withdrawal obeys the eventual authored reserve limit. Each successful formation command commits and saves exactly once; a persistence failure rolls the complete ordered roster and formation back to its exact prior state.
- Concrete enemy starts use their independently animated idle portraits in deployment, with compact enemy/role/reach or boss redundancy. Hazards, objectives, and blocked terrain remain semantic markers.
- On the pre-battle board, concrete enemies stand on the right and their character art faces left toward the player deployment zone. This deployment-only mirror never flips badges, labels, tooltips, layout, or interaction geometry and does not change the independently owned battle-facing rules or neutral portrait consumers.

## Run Structure

### 已确认重点方向：地图与多战线（2026-09-06）

- 地图是玩法设计重点，不只是无障碍规则棋盘的皮肤。地形、通路、接敌和不同战线应使英雄分配、射程、机动、控制与支援具有实际意义。
- 地图上的节点性 Boss 应提前提供有助于准备的机制信息，给调整阵容/装备留下窗口，而非只在遭遇克制时才揭示。信息提前多久、精确程度与调整窗口待定；不承诺公开全部行动或数值。
- 障碍、多战线是方向；多层战场、守点、护送、目标物、分批接敌等只是候选实现，尚未选定地图方案或胜负条件。
- 现有 `10×6` 逻辑空间、玩家 `0..2` 列和 18 个候选部署位是当前实现/验收基线，不是所有未来地图必须长成同一矩形的设计目标。新的地图方案须显式处理部署容量、通行、视线、出生和胜负契约；本次讨论不修改已确认的 10/18 人口里程碑，也不授权突破现有边界。
- 尚未指定地图工作的实施顺序。不要将 Agent 的“先做最小样例”建议写成已获批准的任务。

### Current Run Baseline

The complete Alpha contains three themed tower regions. Each region has a route containing combat, elite, recruitment, shop, event, rest, and boss opportunities. A run ends in a final boss victory or when no living non-temporary player roster hero remains after a battle.

Floor rules are visible before combat and are data-driven. The initial content must demonstrate at least:

- constrained paths or blocking terrain;
- periodically dangerous or beneficial cells;
- a controllable objective or environmental device;
- a boss rule that changes normal targeting or deployment priorities.

### 首批贯穿特色兵（2026-09-16局部执行）

- 贯穿箭命中后继续沿锁定直线飞到地形／射程终点；当前2人伤害上限仅约束结算，不提前销毁箭。擦伤反馈在受击身体位置短促出现，与可见箭头接触对齐；不播放扩散水波。
- ES01贯阵弩手与ES04贯光术士均使用独立于普攻的超远技能距离。先显示方向、源端逐渐充能，再沿开始时锁定的方向释放；目标移动不会转向。蓄力中受控、死亡或施法者位移／换队取消未释放技能。
- 贯阵弩手发射有实际飞行时间的贯穿箭；贯光术士在释放瞬间造成一次直线伤害。当前共同初值为1.2战斗秒蓄力、最多2名目标，首个120%攻击力、第二个80%；这是专门技能配置，不覆盖普通贯穿弹的衰减规则。分散／错位站位和打断提供应对，不要求指定解题英雄。
- 机制类别与阶段数值分开；基础兵在后期继续存在，通过独立遭遇生命／攻击倍率调整战力。首版仅在有限遭遇替换一名首位敌人，每场最多一名新增贯穿特色兵，不提高该场原有总人数。
- 内容数值和出场配置为可调测试基线，详见 `work-items/active/enemy-piercing-skills.md`；本批不固定整局类别配额，也不代表整局成长、难度或体系平衡已通过试玩。

## 五名机制精英与首领（2026-09-19 局部实施）

- EE07 炉喉蜥卫：直径1.6格；锁向后喷吐三段扇形伤害，受控取消剩余吐息。
- EE08 返刃投手：自然挥臂离手，无额外蓄力／预警线；飞刃沿固定路线去回，每目标每程至多命中一次。返程回到原投掷点，已释放的飞刃不因投手移动／死亡而追踪或消失；接回前暂停普攻。
- EE09 换位妖使：标记最远合法敌人，按双方当时的位置整体交换。任一完整身体落点无效或施法中断则双方不换，冷却仍消耗；换位本身不造成伤害。
- EB01 岩垒督军：直径2.2格；普通近战→短时岩墙→窄线地裂→收招。墙有独立血量，阻挡身体和飞行物，普通自动攻击可拆除；地裂可以越过自己的墙。死亡／中断清墙，不永久封路。
- EB02 裂壳虫母：半血时一次裂壳，直径2.4→1.4格并沿合法地面退开；血量、护盾、状态保留，期间仍受伤。之后使用酸液并产卵；卵约3秒孵化，卵与幼虫存活合计最多2，总计最多两批／4枚卵。母死取消未孵化卵，已孵化幼虫留场参与胜负清理。
- 正式初始接入：首区精英在原贯阵弩手与EE07／EE08间选一名领队；末区精英在原贯光术士与EE09间选一名；中区EE03保持。首区／末区首领分别为EB01／EB02。原基础兵池与遭遇总人数保留；机制密度及数值为待试玩基线。
- 五项均有战斗实验室预设。实现恢复与验收边界见 `work-items/active/enemy-five-elite-bosses.md`；不代表整局成长和构筑平衡已验收。

## 霜羽冰冻（2026-09-20 修订）

- 寒意与冰冻合并，只保留无法行动的冰冻；移除寒意叠层、减速及按目标层数提高概率。
- 寒羽由本人普攻或技能箭实际命中触发，基础概率25%、基础时长0.8秒。沿用霜羽同盟现有2／4成员档：2–3名成员时35%，4名及以上50%，最终概率上限50%。同盟攻速加成保持；概率提升只作用于拥有寒羽的同盟成员，不因佩戴徽章自动授予寒羽。
- 原生资格与同一持有者的徽章不重复计数。控制抗性继续缩短适用时长，无新增冻结免疫期；高攻速和多目标仍增加触发机会，数值为当前试玩基线，未表示平衡验收。

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

- `战斗实验室` is a developer-only unit combat test tool. It bypasses tower progression, recruitment, shops, rewards, Meta progression, and the Active Run, and never becomes an alternate progression mode.
- One library exposes every currently published unit by default: heroes, normal units, elites, bosses, summons and test dummies. Classification is an optional browsing filter, never placement eligibility. Choose A or B as the destination test team; any unit can belong to either team, including duplicate copies and hero-versus-hero or boss-versus-boss setups.
- All configurations use free placement on the current `10×6` field, without population caps or left/right deployment regions. Bounds, terrain, distinct instance identity, unique anchors and complete-body non-overlap still apply. Units on either team can use the standard three Equipment slots. Explicitly configured team Relics currently belong to A, as labelled in the editor.
- There is no required primary hero or inherited primary-hero team bonus in the Lab. Each unit retains its own authored skills and team Trait contributions; every boss instance independently uses its published phase/loadout. Legacy primary-id, mode and population fields remain readable for preset compatibility; old formal presets become free configurations in memory without rewriting the saved originals.
- Every edit is validated before battle preparation and refreshes population, Equipment, Relics, Trait contribution/tiers, prepared key attributes, readiness, and concise Chinese rejection reasons. `BattleSimulation` clamping or nearest-free-cell repair is never placement authority.
- Starting freezes a deep configuration snapshot and creates fresh production battle runtime state. Returning restores the unchanged editable configuration; health, Statuses, cooldowns, counters, modifiers, and other battle mutation never copy back. Reset, replacement, exit, and re-entry rebuild or dispose every Battle-owned scope.
- Battle controls provide pause, continue, one fixed tick while paused, displayed x1/x2/x4 speed, reset, and return to configuration through the production Battle screen boundary. Equal canonical configuration plus seed must reproduce the same terminal result and deterministic event/digest projection.
- Built-in presets are authored data. User presets are versioned JSON under `user://battle_lab/` and contain stable ids and mutable Lab configuration only, never Resources or battle runtime state. Lab use must produce zero production Meta, Settings or Active Run writes, regardless of save-schema version.
- Built-in presets retain the default, HC01 growth/U01, and HC03 taunt examples and add 27 `BCxx · 英雄名 · 机制` configurations. These use designed heroes and test dummies, start as clearly labelled free experiments, and exercise production Battle behavior. A preset is a mechanism observation setup, not a final recommended team or proof of balance. Concrete content ids are preset data only and never runtime dispatch.

## Visual Information Hierarchy

- Player-facing state follows `space/shape/motion → icon/color → concise text → on-demand detail`. Persistent surfaces prioritize the current objective, key resources, primary action, and immediately decision-relevant facts; interaction exposes comparison state, while exact rules and values remain reachable through details or tooltips instead of being repeated across page copy, summaries, cards, and actions.
- Deployment is a direct-manipulation board. Empty candidate cells carry no persistent coordinate or deployment prose; terrain, boundary, unit imagery, stable semantic symbols, and distinct default/hover/focus/selected/drag/legal/illegal/swap/success/failure states communicate the deployment zone and action result without relying only on color. Exact names, coordinates, rules, and invalid reasons remain available on demand.
- Mouse and keyboard/gamepad focus support selection and inspection; unit placement and movement use drag/drop only, in both deployment and the laboratory. There is no select-then-cell placement mode. Other run decisions retain their explicit controls. A visible control is not accepted as operable until player-like input reaches the same typed action and state feedback without duplicate activation.
- Player-facing presentation uses one shared semantic palette: health green, attack/physical damage/critical/taunt red, true damage near-white, magical damage purple, tactical points blue, shield/reflected damage white, freeze blue, healing teal-green, player-hero identity gold, exact range light cyan, death/danger crimson, and risk amber. Neutral near-white and blue-gray remain authoritative for prose and secondary labels. Keyword icons, names and associated values share the keyword color.
- Color is never the only information carrier. Chinese labels and tintable icons continue to identify hero, near/ranged role, outcome, risk, rarity, resources, and status; long descriptions remain neutral and readable.
- Starting-hero selection uses a responsive master-detail hierarchy with one deterministic initial selection. Hover and focus only expose their ordinary visual states; mouse click or keyboard/gamepad `ui_accept` explicitly selects a compact animated hero-library tile and binds the detail panel. One fixed detail-panel action starts a run only for that explicitly selected unlocked hero by stable id. Responsibility, automatic contribution, build hook, gameplay tags, and core stats appear once in the detail panel; tactical commands are selected and explained independently.
- Tower routes, recruitment/rewards, and shop choices retain structured decision hierarchies with distinct icon or portrait, title, body, and footer/meta regions while preserving mouse and keyboard/gamepad selection.
- Desktop launch defaults to a maximized, resizable window with its title bar and normal close/restore controls. It does not enter fullscreen. The design viewport and restored-window baseline remain `1600×900`; `1280×720` remains the supported lower bound. Deployment keeps a persistent reserve bench below the battlefield, showing only undeployed heroes, reserve count and an empty state. Drag a bench hero onto the board to deploy/replace, or drag a deployed hero back to the bench to withdraw; clicks and keyboard activation only select/inspect. The complete roster and equipment remain in the separate hero/equipment view.
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
