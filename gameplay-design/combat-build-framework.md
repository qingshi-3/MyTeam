# Combat Build And Population Framework

Status: Accepted Rules And Confirmed Directions — See Per-Section Boundaries

## Responsibility

This document owns the long-term player-facing grammar for hero roles, build composition, population growth, population-based builds, and the `10 / 18 / 30` landmarks. The core game loop, combat rules, tactical commands, failure, and presentation contracts remain in `tower-autobattler-core.md`.

Exact starting population, growth cadence, whether heroes have a tier/rarity progression model and its names/odds, reserve capacity, recruitment economy, trait breakpoints, and specific elemental/status families remain future design work unless this document explicitly fixes them. The 2026-09-06 directions below record product intent, not implementation completion.

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
- Whether heroes use tier/rarity progression remains undecided. If adopted, its availability, complexity and responsibility implications need explicit design; it never changes the accepted one-population cost. Existing content labels do not settle that choice.
- `10` is the conventional ordinary endgame formation ceiling. It is a growth destination, not the starting formation size and not a guaranteed body count in every run.
- The existing eighteen legal player candidate cells are the physical full-deployment ceiling. Deployment keeps one unit per legal cell; battle converts those cells to continuous starting positions and then enforces circular body occupancy.
- Explicit heroes, abilities, equipment, or relics may spend real build opportunity to raise effective persistent population above the ordinary `10` ceiling. A high-roll population build may approach or fill all `18` legal cells.
- A reserve exists and is retained for situational replacement, including functional heroes that are not heavily invested cores. Exact capacity and recruitment/exchange economics remain unresolved; no rule fixes a reserve number or grants a deployment-population exception.
- Temporary units come only from explicit authored sources, require actual legal nonoverlapping battle positions, and do not consume persistent roster or reserve population. They cannot exceed the authored temporary-unit capacity, overlap bodies, or keep battle alive after all persistent roster heroes are defeated.

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

## 2026-09-06：培养、构筑与内容定位

本节是用户逐项评议调研玩法后的**已确认方向**，不是完整配置规格或实现完成声明。编号对应讨论中的 1–18 项；研究原文仍是只读证据，不因本节迁移建议而改变。地图与主动干预的正式归属在 `tower-autobattler-core.md`。

### 常规能力与设计原则

| 讨论项 | 已确认的定位 | 尚未决定／不得推断 |
| --- | --- | --- |
| 2 培养回收 | 作为常规减负方向，降低替换英雄时培养投入的损失。 | 回收哪些投入、返还形式/比例、触发窗口与防循环规则；不默认全部无损，不等于新增装备出售。 |
| 5 核心与功能位 | 资源有限，重点培养少数核心；其他英雄可主要提供效果，不必全员深养。 | 不规定固定核心人数或强制职业配额。 |
| 6 备用阵容 | 保留若干功能性替补，按敌人和羁绊需要轮换。 | 备用人数、培养追赶和交换成本；不要求第二套完整高培养队伍。 |
| 7 非羁绊构筑 | 经济/买卖/成长等行为也能成为收益来源，不能把所有流派都收束成凑羁绊人数；关注不同路线强度。 | 不表示每一种行为引擎都纳入初版，也不照搬某游戏经济公式。 |
| 8 装备效果联动 | 装备通过通用状态、减益、增益、事件或资源与英雄及羁绊配合，不只加面板，不依赖固定英雄名单。 | 不引入背包形状/拼图系统；既有战前免费转移装备规则不变。 |
| 12 跨英雄供给与兑现 | 产生、维持、读取、转换和兑现状态可以分属不同英雄或内容载体；收益归属与消耗规则明确。 | 不要求每个组合具有全部角色，也不自动授予跨队属性汇总等尚未支持的技术原语。 |
| 14 每局内容池变化 | 要做；通过每局可用内容的变化提供随机性，并缓解大池下的成型困难。 | 按何种类别筛选、何时确定、是否公开、保底与禁选方式；不能把互相依赖的组件随意拆散。 |
| 16 风险换收益 | 要做通用的自选风险收益机制；额外危险与所得应在选择前可理解。 | 货币、难度量表、奖励倍率、持久范围及是否独立成系统。 |

### 特殊内容，不是全员必经的培养流程

| 讨论项 | 已确认的定位 | 限制 |
| --- | --- | --- |
| 1 英雄蒸馏 | 通过稀有遗物或特殊遭遇等低频机会，移除英雄占位，保留指定特性或羁绊贡献。 | 不是普遍退役流程；移除是离场还是离开名册、保留范围/时长/上限、装备归还和损失均待定，不能默认为复制全部收益。 |
| 3 融合继承 | 可作为某个羁绊或特殊内容的效果。 | 不做通用合成养成；供体去向、继承内容和叠加上限待定。 |
| 4 分支进化 | 可作为特殊多形态单位或某个羁绊的机制。 | 不做全员进化树；分支、可逆性与投入待定。 |
| 11 站位与职责 | 优先由特殊英雄表达站位切形态，或由位置影响辅助目标。 | 暂不新增全员职责指定系统；不是所有前后排英雄都会变形。 |

特殊获得机会仍应接入通用的条件、目标、授予、效果和生命周期。这里没有承诺当前原语足以表达全部特殊内容；缺少能力时明确扩展模型，不能写具体内容 id 分支绕过契约。

### 待讨论、暂缓与保留意见

- **9＋13 玩家介入**：合并研究，区别战前修改自动行为与战中战术操作；入口、频率和载体待定，不限定装备/卡牌。英雄自动法力施法不因此改回手动。现有两指令/三战术点仍是当前基线，修改前需明确决策。
- **10 科技**：暂缓，不新增全队科技树或普遍单位科技维度；提及“原始祝福”只作类比，不等于批准对应系统。
- **15 Boss 预告、17 地图与多战线**：见核心文档的地图方向；17 是重点方向，但多层、守点等候选实现尚未确定。
- **18 过去构筑成为 Boss**：用户保留意见，不列为当前必做。
- “先实现最小地图样例”是 Agent 提议，用户尚未确认具体下一执行任务；不能从本节推导实现顺序。

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
- whether hero tier/rarity progression is adopted, then its names, availability stages, and offer odds;
- reserve capacity and reserve/deployment exchange rules;
- recruitment prices, replacement value, and population-growth economy;
- detailed elemental/status families and their reactions;
- normal trait breakpoints and which traits can exceed body count;
- hidden `30`-value achievement name, reward, discoverability, and secret transformations;
- exact above-`10` source count, rarity, tuning, performance budget, and visual-density budget.
- cultivation recovery, distillation, inheritance/evolution, run pool selection and risk/reward details listed above; their confirmed direction does not supply missing parameters.
