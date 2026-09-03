# Mirror Throne: Auto Battler

## 身份、版本与研究深度

- `title_id`: `mirror-throne`
- Steam App `2520490`，开发者 Zackavelli，发行商 Cottage Street Interactive；Windows-only、英文、付费，2025-08-12 正式发行。
- 官方 2024-01-16 宣布 Public Demo；可读版本链为 Demo 2（2024-02-02）至 Demo 12（2025-05-21）。当前 App Details 不再暴露 `demos`，没有找到独立 Demo App id；不能断言 Demo 当时使用主 App、独立 App 或目前仍可下载。
- 正式版最后一个公开公告是 2025-08-29 的 Patch 1.1 汇总；公开 appinfo 的 current public branch build `19789034` 也更新于 2025-08-29，但没有更细的 patch-to-build 对照。
- 深度：12 篇官方规则 / 补丁公告、1 个官方成就页、1 个含开发者回复的 bug 主题、2 个玩家讨论、3 篇玩家评测与 1 段带时间戳的 Demo 7-era 自动字幕 gameplay，共 20 个 deep source、12 条 evidence。转录中的同一次 Gambler—Pistolier—Trapper run 闭合历史构筑语法；单 Saboteur 挑战与正式版五单位推荐均只保留为不完整实践线。
- disposition：`retained`，不是当前 meta 样本，也不是可直接照搬的阵容库。

## 来源包与证据边界

| 功能 | 数量 | 内容 |
| --- | ---: | --- |
| 官方补丁 | 12 | Demo 2–12、正式版 Patch 1.1；单位角色、经济 / 召唤 / 触发、模式与修订链 |
| 官方规则 | 1 | 24 个当前成就条件，确认 level、HP / Damage / Armor、party、run 与 cursed-trinket 边界 |
| 开发者回复 | 1 | 正式发行日 Map 4 胜利后软锁与 quick patch 承诺 |
| 玩家实践 | 5 | Demo 单单位挑战、正式版相对站位链、Corporal + Rebate Token、Arena meta / counter-slot 行为、教程学习成本 |
| 视频转录 | 1 | Demo 7-era 的 shop 经济、Priestess death-stack 与 Gambler 出售成长 / Pistolier 双击 / Trapper 转型实战 |

20 页中 14 页是一手官方材料，但官方公告以增量改动为主，不等于完整当前规则。5 个 Steam 社区来源与 1 段创作者 gameplay 都是单人样本：可以证明某次实际选择、观察或抱怨，不能建立总体胜率、强度或因果。自动字幕中的单位 / 物品名存在误识别风险；Steam 商店、appinfo、搜索摘要、聚合页与另外两段无字幕视频只用于身份和检索日志。

## 产品与版本地图

| 时期 | 可核验结构 | 不可外推 |
| --- | --- | --- |
| Demo 2–4 | Poisoner、Saboteur、Alchemist 等显式 counter；Bellman 买入 / 召唤 buff；Bannerman 开战召唤；Ahnk 改为作用于第一名死亡单位；两名 level 2 合为 level 3 的 EXP 语义 | Ahnk 的实际后果、完整单位技能 / 数值、正式版延续 |
| Demo 5–6 | VS 对玩家队伍，Hope 归零前取 10 胜；run 内解锁单位 / 物品 / trinket 并适应；Experimental 混合 faction；Angler / Smuggler / Freebooter 供给经济，Trapper 反召唤 | 精确 Hope 损失、商店概率、队伍格上限、当前 matchmaking / meta |
| Demo 7–9 | Shard Shop 赛前买 trinket；cursed trinket 以难度换高分；Demo 7-era gameplay 显示每轮 10 Gold、未花清零、1-Gold reroll、lock / buy / sell / merge，以及 Gambler 出售成长、Pistolier 双击和针对 Scout 召唤的 Trapper 转型；Sailor / Sneak / Sawbones 与 Brawler / Galleyman / Witchdoctor 暴露 HP、Armor counter、自伤、受伤传播、死亡触发 | 精确 gameplay build、所有物品名 / 公式、正式版是否保留、开发者描述中的 `strong` 是否经玩家验证 |
| Demo 10–12 | 首次 balance pass；Saboteur → Tier II、Poisoner → Tier III；多项触发和购买 bug 修订；Journal、range reader；Shield 移至 Tier I 并改进；Knives 支持 Melee | 新旧公式、Shield 具体行为、Knife holder、正式版当前数值 |
| 正式版 1.0–1.1 | 玩家提供一条五单位相对站位推荐、Armor Piercing 偏好、Arena dominant-combo / counter-slot 观察；官方汇总 Corporal、trinket、faction、Squire、Chew Toy 与多项交互修订 | 完整当前 build、Patch 1.1 是否针对某个帖子、当前 OP / counter 成功率 |

## 核心循环与真实决策

1. Demo 5 官方定义的 VS 循环是与玩家构建的队伍战斗，在 Hope 归零前取得 10 胜。run 过程中逐步解锁单位、物品和 trinket，因此不能在开局一次性锁死阵容；Demo 6 的 mixed-faction format 又改变可用池。
2. 招募 / 升级不只看单位名。Demo 2 规定更高等级单位合并会提供更多 EXP，单位 EXP 价值为 `level × 100`，两名 level 2 可合为 level 3；Demo 10 又修复 shop card 合并时的费用应用。成就确认正式版至少存在 level 4。
3. build 输入跨三层：run 内单位 / item / trinket 解锁，赛前 Shard Shop 的 trinket 播种，以及 faction progression unlock。Demo 7 的 cursed trinket 还把战斗难度与 score / shard 收益交换。
4. 空间成本是有限 party slots，而不是只看 counter 标签。一名长期 Demo 玩家认为特定 counter pieces 在有限 group spaces 中收益不足，实际选择是忽略它们并用 RNG 给出的最强协同队伍；这只是一名玩家行为，但明确暴露了 counter 的机会成本。
5. Arena 既是挑战模式，也是其他玩家 build 的观察面。两名正式版玩家分别把 Arena 描述为主要游玩模式和获取 team-combo ideas 的地方，但没有公开阵容数据库或统计抽样。
6. Demo 7-era gameplay 给出一条可执行 shop 账本：每轮 10 Gold，开局 offers 为 3 Gold，未花 Gold 回合末消失，reroll 1 Gold；玩家会 lock offer、先填 party slots、购买 / 出售、merge / upgrade。它是历史单次玩法记录，不外推为正式版当前经济。

## Demo 7-era 完整构筑：Gambler 出售成长、Pistolier 双击与 Trapper 转型

2024-08-21 gameplay 发布于 Demo 7 与 Demo 8 之间，精确 build 未知。以下只记录 `24:21–36:46` 同一次 run 中实际发生的操作，并用 Demo 5 的 sold-trigger 修复、Demo 6 的 Trapper 反召唤定位和 Demo 7 的时代边界交叉约束。

- **engine**：Gambler 在 party member 被出售时获得 `+1` 到随机 stat；Bard 被部署后立即出售，随后又在 merge 后出售，作为可重复但要支付商店 / 队伍操作的 fuel。
- **state/resource**：每轮 Gold、shop offers、lock、1-Gold reroll、party slots、购买 / 出售 / merge、已触发的 sold events、Gambler 随机 stats、Pistolier 是否存活并完成双攻击、Trapper 等级 / 食物投入与敌方 summon event。
- **trigger**：提交 party member sale 后由 Gambler 读取；Pistolier 在其攻击窗口执行两次射击；敌方单位被 summon 时，Trapper 文本声称对该 summoned unit 造成 3 damage。
- **payoff**：Gambler 把出售事件转成随机战斗属性；Pistolier 是明确的伤害 owner，玩家观察其总是两枪，并认为若成功出手可击杀两名敌人。转录没有证明属性跨 run 永久保留。
- **survival**：玩家在 `32:11–32:15` 明确尝试让核心“活久一点”，但转录不足以可靠识别具体 item 名与防御公式，因此只保留生存投资动作，不补写装备效果。
- **spatial condition**：Bard 必须先进入 party 再出售，party slot 的进入 / 退出本身是发动条件；Pistolier 与 Gambler 需要保持在场完成各自职责。视频没有证据支持额外 adjacency、前后排或二维几何。
- **payoff owner**：Gambler 拥有 sold-event reader 与随机 stat；Pistolier 拥有双攻击输出；Trapper 拥有 enemy-summon reader；Bard 是被部署 / 出售的资源件。
- **economy**：同一视频直接显示每轮 10 Gold、3-Gold offers、未花清零、1-Gold reroll、lock、buy / sell / merge。构筑必须在当回合花费、保留下回合 offer、追合并和出售 fuel 之间取舍。
- **pivot / counter**：玩家先读取 Trapper 的 `enemy unit summoned → summoned unit takes 3 damage`，稍后提到有两个 Scouts 且 Scouts 会 summon，之后 roll、找到第二个 Trapper、买入 / 强化并投入 food。这是同一 run 中可核验的反召唤投入序列，但 ASR 不能确定 Scouts 的阵营，也不能证明 roll 是被该观察主动触发。
- **failure explanation**：`36:31–36:46` 玩家认为 summoned target 似乎没有被扣 3 damage，随后出现失败 / 本段结束。只能记录一次 observed non-proc；没有逐帧、combat log 或官方确认，不能定性为 bug，也不能把 ASR 的 `you lose eight turns` 硬解释为 run 精确终止于第八回合。报告层应分别显示 summon source、summoned unit、Trapper owner、3-damage event 是否创建 / 抵消 / 失效，以及随后结果。
- **version context**：视频只可定位到 Demo 7-era；单位名和简短文本受自动字幕影响，Trapper 角色由 Demo 6 官方公告交叉支持。不能声称这套组合在 Demo 8、正式版或当前 build 仍然成立。

## Demo 7-era 第二构筑切片：五 Priestess death-stack

同一玩家在 `12:45–13:27` 回顾曾用五名 Priestess：每名 Priestess 死亡时 buff 随机另一名队友，一个未命名特殊 item 把该 buff 改为全队，因此连续死亡会层层叠加。它说明 death engine 可以借 item 改写目标范围，并由幸存 Priestess 接收累积 payoff；五个 party slots 是明确空间投入。由于没有同一 run 的 shop 路线、敌人、counter、最终结果、item 名或精确数值，这条线只作为第二个历史 build slice，不单独承担 `retained` 门槛。

## 历史 Demo 失败构筑：单 Saboteur + Ahnk 对 Poisoner

这是一名玩家在 2024-02 的真实挑战路线，不是通关攻略。它发生在 Demo 2–3 附近；Ahnk 的“作用于第一名死亡单位”改动直到 Demo 4 才被官方写明，因此只能把玩家选择与随后可核验的规则边界并列，不能宣称完全同版。

- **engine**：Saboteur 在开战时削减强力 ranged 单位的 Attack，较高状态可能把 ranged 改成 melee；挑战者只带 Saboteur，并配 Ahnk。Ahnk 的完整效果未知，只能确认随后版本把目标改为第一名死亡单位。
- **state/resource**：一个已占用的单位位、Saboteur 等级 / Attack / Armor、Ahnk trinket、敌方 range / melee 状态、第一名死亡单位与 Poisoner 等级。商店价格、reroll、Ahnk 数值和关卡未公开。
- **trigger**：Saboteur 在 battle start 读取 ranged 目标；Poisoner 在死亡时读取击杀者；Ahnk 在第一名单位死亡时选取对象，但后果未知。
- **payoff**：Saboteur 先压低 ranged output 或破坏其攻击形态，再靠自己的普通输出完成单单位挑战。玩家称这是最接近成功的组合，但没有通关。
- **survival**：玩家在反复被 Poisoner 击败后增加 Armor，并观察到能够越过此前阻点；仍未完成整个 run。不能据此说 Armor 抵消 Poisoner 的 Health removal。
- **spatial condition**：只占一个单位位，没有队友 adjacency / 前后链；Saboteur 的开战 counter 仍需要合法 ranged 目标。Ahnk / Poisoner 的 death owner 必须唯一可归因。
- **payoff owner**：Saboteur 拥有 Attack reduction / ranged-to-melee reader 与自身攻击；Poisoner 拥有死亡时从击杀者移除 Health 的反制；Ahnk 拥有第一死亡目标选择，但效果缺失。
- **economy / opportunity cost**：正常购买、升级、刷新、出售、Armor 获取与 Ahnk 获取时序均未知；主动空出其余 party slots 只是空间 / 协同机会成本，不能冒充已闭合的经济路线。这条失败记录不承担 `retained` 的 economy 门槛。
- **pivot / counter**：被 Poisoner 反复击败后转向 Armor，是可核验 pivot；官方当时把 Poisoner定义为 high-Health counter，最高等级可对击杀者移除至多 100% Health。Armor pivot 只延长了该玩家路线，没有解决 run 末端。
- **failure explanation**：应分别显示 Saboteur 开战 debuff 目标 / 数值、目标是否变 melee、Poisoner death trigger 的击杀者与 Health delta、Armor 吸收与 Ahnk 第一死亡触发；否则玩家只看到“加了 Armor 仍失败”。
- **version context**：Saboteur / Poisoner 规则来自 Demo 2；挑战帖发于 Demo 2–3 时期；Ahnk 目标规则来自 Demo 4；Demo 10 又改变两者 tier / base stats。它是跨相邻 Demo 节点的历史失败样本，不是正式版复现。

## 正式版不完整实践线：Priestess / Saboteur / Soldier / Archer / Oath Keeper

Patch 1.1 后一名玩家给出开局建议：front Priestess，后接 Saboteur、Soldier、Archer 与 Oath Keeper，并优先“important relics”和 Armor Piercing items。原句的 `behind her / him` 代词不能唯一还原二维队形，以下只保留相对链与缺口。

- **engine / state**：五个具名单位、相对前后关系、relic 选择和 Armor Piercing item。Saboteur 的历史 ranged counter 已知；其余四单位的当前能力、等级、tier 与 synergy 未公开。
- **payoff / owner**：玩家声称该开局在拿到关键 relic 后稳定获胜，并要求总拿 Armor Piercing；没有指定物品 holder、主要 damage owner 或 penetration 公式，不能擅自把 Archer 定为 carry。
- **survival / space**：Priestess 被放在 front，其他成员由多个 `behind` 关系连接；只能说前后顺序是构筑条件，不能从名称推断 heal、tank、body block 或唯一网格。
- **economy / pivot / counter**：同一玩家抱怨首个 elite 在第一 stage 的第 2–3 node 太难、coins 不跨 camp、退出丢开局 Jewels，并称敌方 max-level Poisoners / Arbalists 过强。它们提供压力点，但没有可执行 roll / replace / abort 时序。
- **证据结论**：这条线证明正式版有人实际推荐具名 party、相对站位、relic 和 item 类别；它不能单独满足完整当前 build 数据库，也不承担本档案的 retained 闭环。

## Counter 标签与队伍格机会成本

- 官方在多个 Demo 节点显式标记 counter：Poisoner 反 high Health，Saboteur 反 ranged，Alchemist 与 Sneak 反 strong Armor，Trapper 反 summoning。它们攻击的是不同 owner / state，不是一条通用“克制单位”轴。
- Demo 7-era run 证明专用 counter 并非只能停留在标签：玩家读过 Trapper 的反召唤文本，之后在同一 run 中 roll、买入 / 强化第二个 Trapper并投入 food。转录还提到 Scouts 会召唤，但不能确定阵营或断言这是 roll 的直接原因。随后战斗里 3 damage 疑似未生效，说明反制投入需要可读的实际贡献与未触发原因。
- 一名自报玩过 Demo 100+ 小时的玩家认为 meta unit + item（特别点名 Double Crossbow）太强，而 counter units 在有限 group spaces 中不足以完成职责；其实际行为是放弃 counter slot，追求 RNG 下的最高正面协同。
- 这个样本的设计意义不是证明所有 counter 数值不足，而是：专用 counter 同时消耗招募机会、party slot、升级和 item/relic 兼容性。若只在 tooltip 写“克制”，但不补偿其低适配 matchup，理性玩家会选择更广谱的主 build。
- 本项目可把 enemy counter package 在战前预览，并让 counter piece 同时具备基础角色、桥接 tag 或可回收成本；否则“针对一层”不够支付一个完整队伍位。

## Shop、召唤与触发网络

- Bellman 在买入和被召唤时 buff；Bannerman 在开战时 summon。Angler 向 shop 提供 fish，Smuggler 获取 free shop items，Freebooter 同时连接 shop 与 summon。这里的 build 不是“召唤越多越好”，而是购买、生成、上场、召唤、受伤、死亡和出售各自触发不同 owner。
- Galleyman 在受伤后传播 buffs，Witchdoctor 在死亡后扭转战局，Brawler 以伤害己方换 damage boost，Sawbones 以己方 debuff 换 high-risk payoff。这些设计把负面状态 / 伤亡做成发动条件，但要求终止条件、阵营归属和受益者明确。
- 官方修订链暴露真实风险：Ahnk / Bannerman 曾 summon 到错误队伍；只有一名单位时 sold-in-shop abilities 曾不触发；Brawler 后来禁止 team kill；Fatigue Poison 不再在 kill 后无意义触发；Essence Catcher、Blood Stone 和 Witchdoctor 交互多次修复。
- 可迁移的 runtime 约束是：每个 `bought / sold / summoned / damaged / killed / died` 事件都要携带 source、owner、target team、alive-state 与 terminal guard；复合触发只消费一次已提交事件，不能从 UI 动作或错误阵营读取。

## Shard Shop、cursed trinket 与 run 构筑播种

- Demo 7 把 Shard Shop 放到 campaign run 之前，玩家用 shards 预购 trinket，以提高达成 `dream combo` 的控制度；shards 来自游玩和高分。
- cursed trinket 提高难度并提高 score，当前成就又确认至少存在“两件 cursed trinket 通关”、Toll Chest / Blood Stone 特定通关等挑战。这是 run 外资源、run 内规则改写和挑战目标的三层绑定。
- 正式版玩家把开局资源称为 Jewels，并抱怨退出会失去它们；公开材料没有证明 Jewels 与 shards 完全同一资源或 Patch 1.1 后的退出语义，不能合并命名或补写退款规则。
- 本项目若使用赛前 relic 播种，应明确这是概率控制、规则改写还是挑战倍率，并给出退出 / 重开 / 失败时的资源结算，而不是把三个账本混成一个货币。

## Shield、Armor、Health 与输出边界

- Demo 12 只确认 Shields 被移至 Tier I 并“improved functionality”；没有护盾生成、吸收、破盾、叠加、持续、holder 或转伤公式。
- Demo 8 的 Sneak 反 high Armor，正式成就把 `15 HP`、`15 damage`、`15 armor` 分成三个开战状态，正式玩家优先 Armor Piercing items。这里能确认 Armor 有独立 counter / reader，不能确认 Armor、Health 或 Shield 自动产伤。
- Poisoner 读取击杀者并移除 Health，不等同于 Armor Piercing，也不能自动推断 bypass Shield。玩家加 Armor 后多撑一段只是一条实践观察。
- 没有任何 deep source 支持 Ice Shield、Earth Shield、元素反应、Defense / HP 转法强、全队防御转单核 Attack 或 shield-to-damage。若本项目要做这些路线，必须由装备、遗物、技能或英雄能力显式声明输入、recipient、snapshot、cap、self-feedback guard 和槽位成本。

## Readability、失败解释与生命周期

- Demo 2 已加入 enemy preview level、战中 tooltip、零 / 负数 popup、combine 表现和 softlock logging；Demo 8 澄清 Jester tooltip；Demo 9 对 0 个目标显示红色；Demo 11 增加 head icons 与 range reader。可读性不是一次完成，而是随新 trigger / range / owner 持续补齐。
- 一名 2026 新类型玩家玩 487 分钟后仍称教程不足，主要靠 trial and error 学会。它不能证明 UI 普遍失败，但说明 patch 中增加 reader 并不自动等于 onboarding 闭合。
- 软锁跨版本出现不同形态：Demo 3 ranged battle softlock、Demo 8 elite-node campaign softlock、正式发行日 Map 4 clone victory 后地图不可点击；开发者对最后一例承诺几分钟内 quick patch。Patch 1.1 又列 To Battle Button lock up fix。没有证据证明这些是同一根因或全部仍存在。
- 正式版社区还报告 draw、首 elite 压力、camp coins 不继承、退出丢 Jewels、Poisoner / Arbalist 敌队压力。都保留为单样本待验证问题，不从补丁标题推断修复因果。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

- Steam：News 13/13、Reviews 21/21、General Discussions 7/7 和全部回复均读完；Guides 无玩家条目，Shared Files 无更多内容。Demo 1 只证明公开 vertical slice，不登记为 deep source；其余 12 个规则 / 补丁节点均登记。
- 身份 / 外部：App Details、appinfo、开发者 / 发行商搜索、官方个人站、X 可读快照、Twitch 链接、Metacritic、Kotaku、IGDB、RAWG、Playin、GameHypes、GamerDB、GAZ、TrueSteamAchievements、cheat / walkthrough 索引、GitHub 与 itch 路线均检查。独立页面要么重复商店文案、为空壳 / 自动菜单、攻略尚未编写或与本游戏无关。
- 搜索引擎：Yahoo / Bing、Brave、Google、DuckDuckGo 与站点限定、单位 / 物品精确词均检索。429、403、challenge、登录和 JS 边界没有绕过。SteamDB、TrueSteamAchievements 与 Cheats.co 的 403 只记录访问失败。
- 视频：三个目标 gameplay 和一个同名音乐逐个核对。`faRVwkJIdLg` 有自动英文字幕，完整 38:14 转录已导出并按时间戳登记；另外两段 gameplay 无可读字幕，未从标题、description、缩略图或画面补机制。没有下载或运行客户端。
- 饱和判断：新增转录实质改变了历史经济、build owner、出售 engine、反召唤 pivot 与失败解释，故纳入而非沿用旧的停止结论。修正后其余路线已收敛为同一 Steam 语料、元数据镜像、无字幕 gameplay 或未完成攻略；继续检索未再补出 current meta、正式版完整第二套构筑或 Shield 转伤规则。

## 对本项目的可迁移与不可迁移信息

可迁移：

- counter piece 必须支付一个真实 party slot；因此需同时提供基础职责、桥接 tag、可回收经济或明确的高价值 enemy preview。
- 把 `bought / sold / summoned / damaged / killed / died` 设计成不同提交事件，并给足 owner、目标阵营、终止条件、幂等与 combat-report 来源。
- 赛前 relic 播种、run 内掉落和 challenge multiplier 应分账；退出 / 重开结算必须可预测。
- Shield、Armor、Health 先作为不同生存 state；要输出必须由显式 reader 转换，且声明 holder / recipient / cap / feedback guard。
- 阅读层要覆盖 range、level、目标为零、负数、combine、敌人 preview 与触发记录；教程还需用真实决策链而非只增加 tooltip。

不可迁移：

- 不复制任何具名单位、trinket、item、faction、数值或玩家阵容。
- 不把失败的单 Saboteur 挑战写成成功攻略，也不把跨 Demo 规则称为同版。
- 不把自动字幕里的疑似物品名、Pistolier / Trapper 单次表现或玩家的 non-proc 判断升级为正式规则、当前强度或确认 bug。
- 不把正式玩家的 `always win`、`OP`、`useless` 或自报 100+ 小时转为统计结论。
- 不把 Rebate Token 与 Trade Goods Trinket 合并，不声称 Patch 1.1 因社区帖子而改 Corporal。
- 不从名称推断 Priestess 治疗、Archer 主 carry、Ahnk 复活、Shield 元素、Armor Piercing holder 或 Poison bypass 顺序。

## 未决问题

- 当前 build `19789034` 的完整单位、item、trinket、faction、tier 和 ability 数据；Demo 规则保留 / 删除 / 改名清单。
- Priestess / Soldier / Archer / Oath Keeper 的当前能力与五单位推荐的唯一二维几何、主输出 owner、relic 清单和 item holder。
- Ahnk、Rebate Token、Double Crossbow、Shields、Knives、Essence Catcher、Blood Stone 与 Chew Toy 的完整规则。
- 正式版当前 party / bench / shop / reroll / upgrade / sell / camp coin / Jewel / shard 经济；Demo 7-era 的 10 Gold、3-Gold offers、1-Gold reroll 与回合末清零是否延续。
- Hope 损失、draw、timeout、Mirror-team 匹配、敌人 preview 与反制窗口。
- Patch 1.1 每项 rebalance 的旧 / 新值、动机、实际结果与具体 build id。

## 最终 disposition

`retained`，定位为“历史 Demo shop / sold-growth / counter-pivot / trigger-network / 失败归因”长尾样本。

保留依据是：Demo 7-era 同一次真实 run 闭合 Gambler sold-event engine、Gold / shop / lock / reroll / sell / merge state、Pistolier payoff owner、模糊但明确存在的生存投资、Bard 进出 party 的空间条件、Trapper 反召唤投入序列，以及 observed non-proc 后的失败 / 结束；官方 Demo 5–7 又交叉约束 sold trigger、Trapper counter 角色与版本时代。五 Priestess death-stack、单 Saboteur 失败挑战、正式版具名 party、Arena 行为和两年补丁链提供不同补充，但都不冒充主闭环。停止深挖的原因是修正字幕遗漏后，其余路线仍不能补上正式版当前 owner / item / economy / patch-level build；把历史组合提升为当前阵容权威会违反证据边界。
