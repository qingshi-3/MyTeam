# Hadean Tactics

## 身份与研究时期

- `title_id`: `hadean-tactics`
- 类型：单人 roguelite deckbuilder / autobattler 混合体。单位实时自动寻敌、移动、普攻、积累 Mana 并施放技能；玩家以可暂停的卡牌回合、Energy、手动目标/移动、召唤与陷阱即时改变战局。
- 发行状态：2020 年进入 Early Access；2023-08-24 发布 1.0；2024-01-31 发布 1.1；2026-04-29 发布 2.0、Moonhunter DLC 与 Eternal Rift。本档案最后观察到 2026-07-20 的 2.1.18。
- 版本边界：2021 的 Inquisitor、Nightblade、Might Bomb 与无限施法均为 Early Access；2023 的 summon-multicast 位于 1.0 初期；2024–2025 构筑属于 1.1.x；Moon Phase、Moonmark、Eclipse、Hidden、Ascendant、Survivor 与 Eternal Rift 只属于 2.x。它们不被拼成一套可同时成立的当前构筑。
- 直接游玩：本批未购买或安装游戏；结论来自官方完整公告/补丁、Steam Discussion 正文与开发者回复。商店页和搜索摘要未进入 deep evidence。

## 检索日志

1. 通过 Steam 官方 news API 读取 1.0、1.1、1.1.04、1.1.11、1.1.15、2.0.60 与 2.1.18 全文，并把 gid 解析为可复查公告 URL。
2. 读取 Emberfish 官方 2.0 Markdown，逐项核验 Moon Phase、Moonmark、Eclipse、Hidden、Ascendant、Passive、卡牌/单位/遗物/Alliance、Eternal Rift、Survivor 与 base-game rework。
3. 读取 2021 `I draw - you die` 与 `Favorite Builds`，核验 Inquisitor 抽牌—盾闭环、Nightblade 3×3 陷阱队、Might Bomb 与 Parasite/Holy Water 无限施法的单位、卡牌、遗物、站位和操作时机。
4. 读取 2022 新手线程及 2021 Resume Turn 开发者回复，核验自动单位层、每 7 秒换牌/回能、自动暂停、End Turn、手动目标/移动次数与暂停时机的职责边界。
5. 读取 2023 `Most OP combo`、2024–2025 `Favorite builds?` 与成就构筑，核验召唤铺场、Recycle/Deplete、Shield Allies/Might-on-Shield、Toxicant/Catalysis、Afterimage 与 Energy/draw 递归。
6. Steam Guide `2978982244` 和 `2440214825` 均只返回泛化 Workshop 页面，无法读取指南正文；未绕过、未登记、未计证据。没有公开的 patch-level 构筑胜率或 matchup 数据库。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-hdt-official-1-0` | [Hadean Tactics is now out of Early Access!](https://steamcommunity.com/games/1260590/announcements/detail/5141475722327803783) | Emberfish Games，2023-08-24 | official-dev / A | 1.0 内容边界、单位 Banner、等级、遗物与 run 规模 |
| `src-hdt-official-1-1` | [The 1.1 update - Out Now!](https://steamcommunity.com/games/1260590/announcements/detail/5582843074935200816) | Emberfish Games，2024-01-31 | official-patch / A | Battle Log、Shield cap、Wanderer、Bounty、cards/relics/alliances、Knife 限频 |
| `src-hdt-official-1-1-04` | [Patch 1.1.04](https://steamcommunity.com/games/1260590/announcements/detail/5589599109272382976) | Emberfish Games，2024-02-10 | official-patch / A | Healing Balm 无限环与战报漏记修复 |
| `src-hdt-official-1-1-11` | [Patch 1.1.11](https://steamcommunity.com/games/1260590/announcements/detail/5759616966672690633) | Emberfish Games，2024-04-27 | official-patch / A | Summon card 重做、多次升级、Macaw Harpy 铺满 board 崩溃 |
| `src-hdt-official-1-1-15` | [Patch 1.1.15 + Hadean Tactics in 2025](https://steamcommunity.com/games/1260590/announcements/detail/1788311419085980) | Emberfish Games，2025-01-11 | official-patch / A | Afterimage Summon/Trap 扩展与成本、自动暂停、复活与顺序修复 |
| `src-hdt-official-2-0` | [Hadean Tactics 2.0 Update + Moonhunter DLC Release](https://emberfishgames.com/data/blog/hadean-tactics-2.0-moonhunter-dlc-release.md) | Emberfish Games，2026-04-29 | official-patch / A | Moonhunter 全规则、Eternal Rift、base game 重做、Echo/四叶草与空间修复 |
| `src-hdt-official-2-0-60` | [Patch 2.0.60](https://steamcommunity.com/games/1260590/announcements/detail/1831432155578025) | Emberfish Games，2026-05-04 | official-patch / A | Kindred Shield/Enchant 传播改为 50% 分摊 |
| `src-hdt-official-2-1-18` | [Patch 2.1.18](https://steamcommunity.com/games/1260590/announcements/detail/1838407329267696) | Emberfish Games，2026-07-20 | official-patch / A | Survivor 归因、旧战报残留、Alliance Stamp 文案与来源可见性 |
| `src-hdt-steam-draw-shield-2021` | [I draw - you die](https://steamcommunity.com/app/1260590/discussions/0/3202618375230179624/) | Kosmirion Epos 等，2021-11-24 | community-analysis / D | Inquisitor 抽牌—盾—Sentinel 具名队 |
| `src-hdt-steam-favorite-builds-2021` | [Favorite Builds](https://steamcommunity.com/app/1260590/discussions/0/4790130997107294030/) | jmotivator 等，2021-09-21 | community-analysis / D | Nightblade 3×3 陷阱、Might Bomb、Parasite/Holy Water |
| `src-hdt-steam-autobattler-pause-2022` | [never played autobattlers](https://steamcommunity.com/app/1260590/discussions/0/3725071921961841708/) | jujubee20062010 与回复者，2022-12-29 | community-analysis / D | 自动单位层、7 秒卡牌回合、自动暂停、两次手动命令 |
| `src-hdt-steam-resume-turn-2021` | [Resume Turn Bug](https://steamcommunity.com/app/1260590/discussions/0/3174449951063757267/) | Jay；开发者 Doug，2021-04-07 | community-analysis / D | End Turn、半回合警告、暂停微操与时序可读性 |
| `src-hdt-steam-op-combo-2023` | [Most OP combo you have run](https://steamcommunity.com/app/1260590/discussions/0/3826425264482770354/) | Wex 等，2023-09-01 | community-analysis / D | summon-multicast、零回合/无限牌、turn cooldown 压缩 |
| `src-hdt-steam-favorite-builds-2024` | [Favorite builds?](https://steamcommunity.com/app/1260590/discussions/0/7051043016322087595/) | hihatss 等，2024-11-18 至 2025-10 | community-analysis / D | 1.1.x Toxicant、召唤盾、Recycle、Shield Allies、性能观察 |
| `src-hdt-steam-achievement-builds-2025` | [Need some build suggestions for achievements](https://steamcommunity.com/app/1260590/discussions/0/594013434968860379/) | Mars 等，2025-01-28 | community-analysis / D | 小牌库 draw/Energy/Recycle/Deplete 递归 |

## 真实循环与玩家决策

### Run 层

- 1.0 的基础体量包括 3 位 Hero、6 个 Circle、9 个 Boss、30+ draftable units、260+ cards 和 70+ relics。单位 Banner、Hero Upgrade、事件、营地、符文与 relic reward 是不同投资入口；普通单位在第三 Circle 后最高到 level 5（`src-hdt-official-1-0`）。
- 1.1 允许跳过 Hero Upgrade 或 Banner 换取 50 Gold；Wanderer 可以移出 Party，但若留在 Bench 会在战后逃走；Bounty 让击杀目标转化为 Gold。招募不是只比较单位强度，还要判断临时 Wanderer、bench、升级、Gold 与当前牌组/Alliance 的配合（`src-hdt-official-1-1`）。
- 2.0 把 relic reward 改成二选一，并允许 custom deck 指定一张 starting card；Eternal Rift 用连续 wave、Survivor 护送与后续 run bonus 压缩准备周期（`src-hdt-official-2-0`）。

### 战斗层：自动执行与卡牌干预分工

- 单位自动寻敌、移动、攻击、积累 Mana 和施放技能。社区规则解释称，单位通常攻击最近目标；每场每个单位可有限次被手动指定优先目标或移动位置，某 relic 可放宽次数。该描述不是完整 AI 规格（`src-hdt-steam-autobattler-pause-2022`）。
- Early Access 观察中，每 7 秒自动暂停、弃掉当前手牌、抽 5 张并恢复 Energy。玩家可在暂停中不限思考时间地打出 Energy 允许的 cards，然后恢复实时战斗；中途能力产能时也可自动暂停（`src-hdt-steam-autobattler-pause-2022`）。
- `Resume Turn Bug` 实际暴露的是有意的 End Turn 门禁：只恢复战斗而不结束卡牌回合时，计时器到半程停止并警告；玩家可借此等敌人走入 AOE、等单位施法前后再打牌。开发者承诺补 tooltip，说明时序控制本身有价值，但表达不足会被误判为 bug（`src-hdt-steam-resume-turn-2021`）。
- 因而 Hadean 的高强度常来自“小 deck + draw + Energy + pause + trigger fanout”，不是某个单位独自完成。它也与本项目固定两条独立战术指令、每战 3 点共享资源的边界根本不同。

## 具名构筑 A：EA Inquisitor “I draw – you die”

时期：2021-11 Early Access；只证明历史结构，不证明 1.1/2.x 当前强度。

- Hero：Inquisitor，走 Draconic / Relentless。
- Units：同路线 Warg；至少一个 Sentinel 的 Marble Gargoyle；可选 Golden Warg + Sentinel、Minos。回复者给出两只 Marble Gargoyle + Golden Warg 的实战补充。
- Cards：random shield、Chainmail、blood shield card、Thinking Ahead、Shell、Justice Hammer、Holy Water、Maieutics，以及给 Gargoyle 的 Illusion/Clone；补充 Shield Throw 和消耗全队 Might 对全敌结算的 talent。
- Relics：Knife Tentacle、Pink Gem 为核心；Illusion Mirror、Ace of Spades、Blood Oleander 与 Boss 后增加 draw 的 relic 为增强。
- Engine：draw 与 shield 反复触发 Sentinel；Gorgon's Vengeance 生成的牌也算 draw，使 talents 继续触发 Sentinel。
- State/resource：hand、Energy、draw 次数、Shield、Might、Sentinel 触发次数。
- Payoff：Shield Throw、Might cashout 与 Knife Tentacle 把防御/抽牌 fanout 转为全场伤害。
- Survival：Marble Gargoyle、连续 Shield 与 Sentinel activation 承担生存。
- Spatial condition：Gargoyle/前排必须吸住第一接触；AOE 与全体结算降低精确 hex 依赖，但单位在首次 cast 前死亡会断引擎。
- Payoff owner：装配后的 Inquisitor/Golden Warg 与 Might cashout card；Gargoyle 是盾/触发引擎，不应把所有伤害记到它身上。
- Pivot/counter：缺少 draw 或 Knife/Pink Gem 时保持普通盾队；Silence、开局爆发、移除关键前排、打断 draw/turn cadence 会拆层。1.1 已把 Knife Tentacle 限制为每两次 damage 才触发以避免崩溃，因此旧版 turn-zero 结论不可外推。

## 具名构筑 B：EA Nightblade 3×3 Permatrap

时期：2021-09 Early Access；1.1 仍有 Permatrap，但卡牌数值与升级规则已变化。

- Hero：Nightblade / Nightshade 陷阱路线。
- Units：优先 ranged units 和 clone units；分身先吃第一轮伤害，远程单位进入攻击距离后尽量不离开保护区。
- Cards：尽可能取得 traps，核心为 decay、heal、shield traps；Summon Slime；Double Trap Range；后期 Permatrap，最好赋予 Crucial，并通过升级移除 trap 的 Deplete。
- Formation：己方前线排成 3×3，shield trap 在中心，heal/might/shield traps 持续填入该区；敌人站稳后暂停，在敌群中重叠 stun/decay/damage traps。
- Engine：每次 trigger 同时引爆保护区与敌区的多个 traps；Double Trap Range 扩大覆盖，Permatrap 让当战后续创建的 traps 跨战保留。
- State/resource：trap 位置、重叠数、trigger 次数、Decay、Shield、Might、跨战持久 trap 与空格。
- Payoff：敌区叠 Decay/伤害/控制，己方区同时 heal、shield、gain Might；后续 1.1.x Toxic Archer + 两名 swamp tanks 仍展示 ailments/trap damage → tanks gain Shield 的同构路线。
- Survival：3×3 保护区、远程不移动、clone/body saturation 与 heal/shield traps。
- Spatial condition：中心 trap、双倍范围、远程射程、敌人 settle 后的 overlap 和 free tiles 都是必要条件；不是“拿到陷阱标签就自动成立”。
- Payoff owner：敌区 trap/Decay 负责伤害，swamp/tank 或 ranged carry 分别拥有 Shield 与持续输出；Permatrap 只拥有持久化规则。
- Pivot/counter：缺 Permatrap 时只做单战 trap；敌人位移、反后排、分散阵型、occupied tile、开局快速压阵会破坏两个区域。2.0 修复 trap 与占用格重叠，说明空间合法性必须由规则保证。

## 补充构筑与反例

### Might Bomb

- 五人队至少一名高 HP tank，最好两名轮换。等待 `Sacrifice tank → 全队按 lost HP 得 Might`、`Double Might`、`移除全队 Might 并按总和伤害全敌` 同时到手；小 deck、draw 与 Energy 提高组合率。
- 作者例中先给四名幸存者各 1500 Might，再翻倍为 3000，最后对全敌 12000。另一线程也记录 Fragile + 全队 Might cashout 的 turn-zero 胜利。
- 它清楚展示“坦克生命是输入、全队 Might 是中间态、结算牌是 payoff owner”，也展示防御资源一键全屏转伤会压缩战斗阶段。数值只属于 2021 EA 单例。

### Summon / Deplete / Recycle

- 1.0 初期的 Mage + Mystic + Conjurer + Dragon Tail、multicast、AOE armor 与 Knife Tentacle 可在第三回合铺满 board；另一回复用 cooldown relic + 两张 upgraded Impatience 把 6 秒 turn cooldown 降到 0，进入无限牌。
- 1.1.x Mage 构筑以“deplete card 时全 allies +75 Shield”、Recycle 与 Energy generation 铺满 Wisps，并把 Shield 推到 2k+；玩家指出 Recycle 会把 depleted card 直接放回 hand，而不是 discard，因而是即时递归关键。
- 成就构筑用 Recharge + Blood Card，或 Recycle + Jolt + 一张从 discard 取回且自身 Deplete 的 frost spell，重复 draw、Energy 与 summon。
- 这些路线证明 cadence、hand destination、Deplete、summon occupancy 和 trigger budget 必须共同受控；不证明本项目需要牌库。

### Shield Allies / Might-on-Shield / Dragon Tails

- 1.1.x 社区构筑让三名 units 同时取得 Shield Allies 与 Might-on-Shield，再用 Dragon Tails 收尾；Shield 可叠到数千，Might 同步增长。
- 这是用户提出的“盾本身负责活、另一位输出者读取全队防御状态”的直接参照：供应者、读者与终结者可拆开，而不是所有盾都必须同一元素或自己反伤。
- 官方 2.0 的 Brute Force 也会移除目标一半 Shield 并转成额外 Might；Moonlit Mend 则把全队总治疗转成对随机敌人的伤害。两者说明防御→进攻可以是有消耗、明确归属的转换。

## 2.x Moonhunter：元素/状态引擎而非盾体系替代品

- Moon Phase 有 New / Half / Full；Moonmark 是可叠层 enemy debuff，在 Shatter 时按当前 phase 产生伤害/附加效果。Eclipse Charge 满后生成 Activate Eclipse，Eclipse 同时开启三相效果。
- Ascendant card 若仍在 draw pile，每回合开始自动抽一张，每回合最多一张；Passive 只要 card 在 deck 中就生效。两者把“牌库拥有”与“打出”分成不同所有权。
- Lunar Barrage 花光 Energy，按每点 Energy 对全敌伤害并加 Moonmark；Harvest Moon 把 Moonmark 转为 Eclipse Charge；Eclipse Overcharge 立即 Eclipse，但下一回合少 Energy、少抽牌。这里的强度来自相位、标记、能量、未来回合债务的闭环。
- Moon Armor 在 Moonmark applied 时给 Shield；Lunar Shield 在 phase change 时给 Shield；Riposte/Quick Parry 用 Shield/减伤反射 Precision Damage；Moonlit Mend 把全队治疗总量转成随机敌人伤害。盾只是相位体系中的一种生存或转换状态，并不等于“月元素”。
- Shadowstep 先选 unit 再选 tile，移动并赋 Hidden；Moonlit Knight teleport 到空 tile，落点邻敌 Moonmark + Stun；Moon Totem 在 phase change 时影响相邻敌人；Duel 选择两单位隔离交战且免疫外部伤害。target、tile、adjacency 与 occupancy 是 card 层的真实成本。

## 空间、目标与召唤后果

- 自动层负责单位 target、range、movement、Mana 和 skill；卡牌层可以暂停后 shield、target、teleport、summon、trap、phase change 或改变手牌/能量。卡牌不是单位的永久手动技能。
- 2.0 Hidden 明确禁止敌人选中且不重新找目标，攻击后解除；Assassin jump/invisibility 曾在目标失效后仍攻击而被修。目标资格与既有攻击必须在同一时序中失效。
- Moonlit Knight 必须选择 empty tile；2.0 还修复 traps 在 occupied/overlapping tiles 生成。Hadean 的召唤/陷阱强度与 board occupancy 直接相关。
- 1.0.08 专门优化大量 summons；1.1.10.5 修复 Macaw Harpy 填满 board 时崩溃；1.1.11 重做 Summon cards 为可多次升级。召唤数量、升级深度、性能与合法格不能作为四套互不相干的规则。

## 经济、招募、升级与转型

- Banner 直接 draft 单位，1.0 第四 Circle Banner 给 level 3；Bronze Man 使 Banner 单位再高一级。Alliance Stamp 给随机 ally 额外 Alliance，说明招募与 trait bridge 可由 run relic 改写。
- 1.1 跳过 Hero Upgrade/Banner 换 50 Gold，把“没看到合适组件”转成未来选择权；Wanderer 在 Bench 会逃走，阻止无成本长期囤积临时答案。
- 自定义 Hero/deck 允许预装闭环，社区的许多无限构筑因此不能当普通随机 run 的一致性证据。2.0 允许 custom deck 配一张 starting card，又进一步提高关键引擎稳定性。
- 2.0 relic reward 改为二选一，比随机单件更能支持 pivot；但没有公开统计证明它提高了构筑多样性。

## 反制、Boss 与适应窗口

- 1.1 减少 Boss reinforcement 数量，表明 summon pressure 可以由 Boss package 独立调节；社区对 Luciferos “未行动”或 turn-zero kill 只是个案，不能证明 Boss 普遍失效。
- 小 deck 无限 draw/Energy、turn cooldown 归零、全屏 Might cashout 和 trap fanout 都能在自动单位开始有效交互前结束战斗。它们的真正反制不是再加一个 Boss 特例，而是回合最短间隔、触发预算、资源结算顺序、summon cap/occupancy 和转换消耗。
- Eternal Rift 的 Survivor 要活到下一 Boss 才成为后续 run bonus；这把“保护特定弱单位”变成可预览的跨波目标。2.1.18 又修复 Survivor ailment damage 的 owner 名称/portrait，说明护送失败必须能追到伤害来源。
- Moonhunter 的空间/target cards 提供显式适应窗口：用 Shadowstep/Hidden 躲首轮 focus，用 Moonlit Knight 改位控制，用 Duel 隔离关键单位；其代价分别是 card、Energy、目标/空格和未来回合债务。

## 失败解释、生命周期与明确负面案例

以下六组计入跨游戏明确负面/重做深度；相邻数值调整不重复计数：

1. **Knife Tentacle 触发风暴**：1.1 改为每两次 damage 才触发，官方明确理由是避免 game crash。说明伤害 fanout 必须有 rate limit。
2. **Healing Balm 无限治疗环**：1.1.04 明确修复 infinite loop；同补丁还修复它治疗敌人。目标合法性与递归终止是不同 guard。
3. **召唤铺满 board 的性能/崩溃**：1.0.08 优化大量 summons；1.1.10.5 修复 Macaw Harpy 填满 board 崩溃；1.1.11 随后重做 Summon card 升级。社区仍报告极端铺场 FPS 下降。
4. **复制收益降幅**：2.0 将 Echo Amulet 从完整复制 ally-target effect 改为随机 ally 半效果，防止一张定向牌等价产生第二份完整收益。
5. **Kindred 传播分摊**：2.0.60 把相同 Kindred copies 的 Enchant/Shield 传播改成只分摊 50%，避免复制体按全值重复广播。
6. **战报归因与陈旧状态**：1.1 引入 Battle Log；1.1.04 修复首段伤害漏记；1.1.11 修复 Radiant Point 未出现；2.1.18 修复 Survivor ailment owner/portrait 与旧战斗条目残留。报告正确性是玩法可读性的一部分。

其他重要 guard：1.1 把 max Shield 设为 1M；Afterimage 在 1.1 获得 Deplete，1.1.15 扩展至 Summon/Trap 时成本从 1/1 调到 2/1，2.0 又进入 2/1→1/0 的新版本；Four-Leaf Clover 在 2.0 增加 1 秒 cooldown；Phoenix Down 与 Chalice of Burden 均经历顺序/数值修正。这些保留为生命周期，不额外宣称每项都曾统治环境。

## 社区观察边界

- `I draw - you die`、Nightblade trap、Might Bomb、summon shield、Toxicant/Catalysis 和 Shield Allies 都是具体玩家构筑，可证明交互存在和如何操作，不能证明总体胜率。
- “Luciferos 没行动”“turn zero kill”“只需两张牌”等是单次或少量回复的强度描述。它们用于识别前置结算和递归风险，不作为当前平衡结论。
- Vulture Eye、Toxicant/Catalysis 等只有单线程讨论且缺少当前正式数值交叉；不进入主 evidence，避免把抱怨或推荐当成规则。
- 没有可访问的 patch-level 构筑统计；官方补丁只能证明改了什么，只有在明确给出 crash、infinite loop、错误顺序或传播规则时才记录原因。

## 对本项目可迁移

### 可迁移原则

- 自动单位层与有限战术干预层必须分别说明：谁自动选目标、何时可暂停、指令改什么、失败是否消耗资源、目标/空格是否合法。
- 盾体系可有独立伤害读者：一人/一件装备读取全队额外生命、Shield 或 Defense，供应者负责活，carry 负责转伤；转换应有明确比例、消耗、上限、触发频率与归因。
- 元素/状态体系可以与盾正交。月相→Moonmark→Shatter/Eclipse 是状态引擎；其中的盾牌只是 survival/reader，不定义整个元素身份。冰盾、土盾可以共享盾规则，但其供应、控制、反应与 payoff 不同。
- 陷阱、召唤、teleport 和邻接必须把 free tile、occupied tile、source、target、trigger budget 与持续范围纳入构筑句子。
- 复制、传播、全队广播、伤害/治疗反复触发需要 hard cap、rate limit、衰减、一次性标记或不可回入规则；只靠稀有度防不住闭环。
- 战报必须区分原始来源、召唤/陷阱/状态 owner、Shield produced/absorbed、Might conversion、healing converted to damage，以及旧战斗状态清理。

### 不可直接迁移

- deck、hand、draw pile、discard、Deplete、Recycle、Crucial、Permatrap 与一回合 9+ cards 不属于已确认项目规则。
- 本项目每 run 固定装备两条独立 tactical-command id、每战共享 3 tactical points；不能把 Hadean 的整套牌库、每 7 秒回能和无限暂停链当成已接受设计。
- 自定义 Hero/deck、Boss relic 降 turn cooldown、starting card 与跨战 traps 极大提高闭环一致性，不可用来推断普通随机奖励的体验。
- Hadean 的 nearest-target 与两次手动命令来自社区规则解释，不足以替代本项目已有的确定性寻路/仲裁权威。

## 未决问题

- 1.1.x 社区构筑没有完整、公开、按 patch 切分的胜率或 encounter matchup 数据。
- 当前 2.1.18 的普通 card turn 基准是否仍完整沿用 EA 的 7 秒/5 张/回满 Energy，官方 2.x patch 未重新陈述，因此不把旧数值升级为当前规则。
- Summon cards 可多次升级后的具体上限、每类 summon 的硬数量上限与满 board 时选择失败语义未由可访问官方文档完整说明。
- `Favorite builds?` 的 Toxicant/Catalysis、Shield Allies 等缺少正式 card 数据库交叉，只保留社区构筑层级。
- 没有足够资料评估 Eternal Rift Survivor bonus 的选择池、放弃成本和各 Boss 的真实适应率。

## Disposition

- `anchor-retained`
- 理由：15 个实质非商店来源，覆盖 official developer/release、official patch/rules、具体社区构筑和开发者回复；两套 EA 具名构筑具有 exact hero/units/cards/relics、触发、3×3/overlap 空间、payoff owner 与 pivot/counter；另有 1.1.x 与 2.x 交叉、六组明确生命周期失败。
- 置信度：核心官方规则与明确 bug/rework 为高；历史构筑结构为中；单例强度、当前 meta 与未完整文档化 AI/turn 数值为低到中。
