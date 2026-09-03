# Super Auto Pets

## 身份与研究时期

- `title_id`: `super-auto-pets`
- 子类型：五单位线性站位、异步 Arena / 同步 Versus 自走棋；另有固定日更敌队的 Daily PvE。
- 状态：持续更新；预设 Pack、Weekly、Custom Pack、Wild Custom Pack 和 Daily 的合法池及平衡前提不同。
- 观察时期：2022 Turtle Pack 历史反制、2024 Star Pack 重做、2025 顺序资料与 Update 44，以及 2026 Update 48 和 2026-06/07 维护型攻略快照。
- 直接体验：本轮未安装或实玩。官方公告、机制攻略、独立顺序测试与社区失败案例分栏记录。

## 检索日志

1. 检索官方玩法页；Team Wood Games 官网只返回客户端应用壳，无法读取新闻正文。通过 Steam 官方 news feed 找到并打开 Team Wood Games 的 Update 34、44、48 与 Daily Mode 公告。
2. wiki.gg、Fandom、NamuWiki 和 SteamDB 均返回 403，Reddit JSON 被阻止；没有绕过。Level Winner 出现 SSL 失败。
3. 打开 Two Average Gamers 的机制、构筑和综合策略三页；三页同作者，只按三个功能页面而非三个独立观点计权。
4. 打开 Grounded SAP 的专门顺序测试页，发现与综合攻略对“同攻击力如何破平”存在明确分歧；不强行统一。
5. 打开 Screen Rant 的 2022 队伍/反制文章，作为独立历史交叉验证；打开 Steam 2022 顺序讨论作为“玩家无法解释同时死亡/召唤”的可读性案例，而不是规则权威。
6. Zathong 与 Gamers Decide 可访问，但没有新增可核验版本、反制或生命周期字段，本批未为数量登记。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-sap-tag-mechanics-2026` | [Game Mechanics](https://www.twoaveragegamers.com/ultimate-guide-to-super-auto-pets-game-mechanics/) | Fred / 更新 2026-06-04 | strategy-guide / C | 商店、金币、合并、Tier、召唤、Food 和顺序 |
| `src-sap-tag-builds-2026` | [Best Builds](https://www.twoaveragegamers.com/the-best-super-auto-pets-builds/) | Fred / 更新 2026-07-28 | strategy-guide / C | Fish+Bison、召唤、自伤的组件、转型和反制 |
| `src-sap-tag-consistency-2026` | [Win More Consistently](https://www.twoaveragegamers.com/super-auto-pets-win-more-consistently-with-this-ultimate-guide/) | Fred / 更新 2026-06-04 | strategy-guide / C | Arena/Versus、Pack、站位、Food、Toy 与替换 |
| `src-sap-grounded-order-2025` | [Order of Operations](https://groundedsap.co.uk/Article.aspx?ID=11) | Freetz / 2025-02-19 | community-analysis / B | 分阶段触发序列、同触发器排序与消失/召唤时点 |
| `src-sap-screenrant-teams-2022` | [Best Teams & Counters](https://screenrant.com/super-auto-pets-best-teams-guide/) | Screen Rant / 2022-03-12 | detailed-review / C | 召唤、堆属性、狙击、自伤及反制 |
| `src-sap-official-update48` | [Update 48](https://steamcommunity.com/games/1714040/announcements/detail/686380477674684732) | Team Wood Games / 2026-05-21 | official-patch / A | 相邻发动重做、复制/变形范围、触发上限与时序 bugfix |
| `src-sap-official-update44` | [Update 44](https://steamcommunity.com/games/1714040/announcements/detail/501718945006355542) | Team Wood Games / 2025-12-19 | official-dev / A | Arena 难度重做、Custom Pack super-pet 上限及原因 |
| `src-sap-official-update34` | [Update 34](https://steamcommunity.com/games/1714040/announcements/detail/4178856034940690765) | Team Wood Games / 2024-05-28 | official-patch / A | Star Pack 重做、archetype 成熟度与视觉语言 |
| `src-sap-official-daily-2026` | [Daily Mode](https://steamcommunity.com/games/1714040/announcements/detail/490469454386301124) | Team Wood Games / 2026-04-08 | official-rules / A | 14 支全员相同的日更敌队 |
| `src-sap-steam-order-thread-2022` | [Order of Attack thread](https://steamcommunity.com/app/1714040/discussions/0/3198118348349750330/) | Attackturkey 等 / 2022-01-05 | community-analysis / D | 同时死亡、Badger/Cricket/Honey 的归因困惑 |

## 真实循环与玩家决策

Arena 每回合先进入商店，用 10 金在五宠阵容、Food/Perk、合并升级、1 金刷新和冻结之间分配，然后进入不可直接操控的线性战斗。多数宠物和 Food 为 3 金，金币不跨回合；但 Puppy、T-Rex 等“回合结束保留金币”效果会让 0 金通常最优出现例外。Tier 每两回合扩充一次池；池越大，寻找旧低阶副本越难，因此升级时点和是否冻结对子具有明确机会成本。

合并同宠叠加经验与部分属性；升到 2/3 级提高技能，并在升级时向商店加入下一 Tier 的奖励宠物。接收合并的一方保留自身 Food，拖入方的 Food 会丢失，这使 perk owner 与合并方向成为真实选择。商店阶段的增益通常永久，战斗阶段增益通常临时，但文本会标出例外。

战斗由两队接敌端宠物互撞，阵亡后队列前移。站位同时决定谁先承伤、亡语/受伤/攻击触发何时发生、召唤是否有空格、后排发动机是否能存活。召唤并非“无限增员”：只有空槽能生成新宠，五格队列与阵亡后空位的出现时点共同构成容量约束。

## 具名构筑一：2026 Turtle Pack Fish → Bison 成长队

核验来源：`src-sap-tag-builds-2026` 的 2026-07-28 构筑正文；`src-sap-tag-mechanics-2026` 与 `src-sap-tag-consistency-2026` 互证 Tier、合并、Food、替换和线性站位。Pets 均属于经典 Turtle Pack 语境。

- `engine`：从第一回合持续收集 Fish，在早期小池内升至 3 级；Tier 4 解锁后招募 Bison，利用场上 3 级友军让 Bison持续自我成长。
- `state/resource`：Fish 副本/经验、回合数、Tier 4 Bison 的出现、刷新/冻结金币、Bison 已积累属性、五个队位和剩余生命。
- `payoff`：Bison 是主要堆属性 carry；Fish 是启动条件与已投入的中期单位。Caterpillar 可复制大 Bison，Monkey继续单体增长，Penguin 为多名升级宠提供横向成长。
- `survival`：Turtle / Melon 为大单位提供一次高额伤害缓冲；Skunk 压低敌方最大生命，Dolphin 可先清低生命发动机。Food 应优先给可持续到终局的 Bison，而不是即将替换的临时宠。
- `spatial condition`：高身材 Bison 通常靠接敌端换掉多个单位；关键辅助放其后避免先死。若以 Turtle 现场亡语给 Melon，必须让 Turtle 先于目标死亡且目标仍在；也可在商店用 Pill 提前把 Melon 永久交给 Bison，释放阵容槽位。
- `equipment/food owner`：Melon、Garlic 或通用永久属性 Food 的主要所有者是 Bison；合并时由接收者保留 Food，方向错误会损失 perk。
- `pivot/counter`：Fish 到 Tier 4 前仍未接近 3 级，或 Bison迟迟不来，就不应继续为已投入的低阶宠刷新。高等级 Skunk 可直接削弱单核，前置 Scorpion 可一换一，成长更快的宽阵容可越过单体上限。构筑页还列 Penguin 体系为竞争性更强的横向成长对手。
- `version context`：构筑页标称更新至 2026-07-28；具体 Fish/Bison 数值未在本 dossier 固化，后续补丁变动不会改变“低阶 3 级条件 → Tier 4 单体自成长”的结构证据。

## 具名构筑二：2026 Turtle Pack Horse/Cricket → Turkey/Fly 召唤队

核验来源：`src-sap-tag-builds-2026` 的阶段转型；`src-sap-tag-consistency-2026` 的召唤格位与后排 buff 站位；`src-sap-screenrant-teams-2022` 独立支持历史组件及 Sniper/Hippo/Rhino 反制。

- `engine`：前期 Horse 为每个新召唤提供临时攻击，Cricket 阵亡生成 Zombie Cricket；中期换入 Spider 与 Sheep 等更高质量多体召唤；后期用 Turkey 替代 Horse放大每个战斗召唤，并尽早加入 Fly 提供连续衍生物。
- `state/resource`：当前召唤次数、空格、Horse/Turkey 等召唤 buff 等级、Cricket/Spider/Sheep/Fly 的副本与 Tier、Food、五宠上限和本局已获胜场。
- `payoff`：通过多轮亡语和 Fly 重生让对手在五个原始单位之外再进行多次攻击；Turkey 是后期广播型收益所有者，实际伤害由被 buff 的每个衍生物分散兑现。
- `survival`：召唤物吸收攻击就是主要防御；Turkey/Fly 等发动机藏在队尾。Deer配 Mushroom 可在死后再次提供召唤价值，早期连胜为高 Tier 发动机争取生命窗口。
- `spatial condition`：Cricket、Spider、Sheep、Deer 等放在接敌侧依次死亡；Horse/Turkey 位于其后。必须预留召唤空格：例如五宠满队且 Level 2 Rooster 过早死亡时，两个 Chick 可能因仅出现一个空位而损失其一。队尾发动机又会受到 Crocodile 等定点狙击威胁。
- `equipment/food owner`：Mushroom 优先给高价值召唤源（如 Deer）以追加一次本体/亡语链；不应把防御 Food 浪费在预定立即死亡且不需要存活的廉价衍生物上。
- `pivot/counter`：前期 Horse/Cricket 只负责抢节奏，构筑页明确要求中期换成 Spider/Sheep、后期转 Turkey/Fly；若 Cricket 很早到 3 级，可转接 Fish+Bison 类 3 级条件路线。Dolphin/Crocodile/Snake 等狙击可先杀 Turkey/Fly，Hippo/Rhino 从连续击杀弱召唤获得收益，Deer 式溅射可一次处理多个身体。
- `version context`：2026 维护型构筑页提供阶段路线；Screen Rant 是 2022 历史交叉验证，只证明召唤体系的长期结构与反制类型，不证明 2026 数值强度。

## 补充构筑：Hedgehog + Pufferfish 自伤链

- `engine`：Hedgehog、Elephant 或 Whale 主动伤害己方，触发 Pufferfish 的 Hurt 反击；Garlic 降低 Pufferfish 接受的每次伤害，使链条在自毁前多次发动。
- `payoff owner`：Pufferfish 是反击伤害所有者；Hedgehog/Elephant 是触发器，不能把触发次数算成其直接伤害。
- `spatial condition`：Elephant 必须在 Pufferfish 前方，使攻击后的向后伤害命中它；Garlic 维持触发次数。
- `counter`：高生命或 Garlic 阵容可耗过低倍率反击；Hippo 从连杀中成长；Weakness/失去 Garlic 会放大自伤并摧毁续航。Screen Rant 的 2022 文章独立指出该体系缩放较差，需要 Skunk/Scorpion/T-Rex 等后备终局方案。
- `version context`：Two Average 构筑页更新 2026，但独立反制来源来自 2022；因此只作为机制补充，不计入当前强度结论。

## 构筑语法、经济与转型

SAP 的可迁移构筑语法是：

> 商店/升级或战斗触发 engine + 副本/经验/金币/格位/触发次数 state + 单核属性、召唤广播或 Hurt 反击 payoff + Food/perk 与替补身体 survival + 五格顺序和空位 condition

关键机会成本包括：买宠对刷新、当前节奏对追副本、Food 给谁、合并方向与 perk 保留、五宠满编对循环/召唤空位、临时低阶宠对高 Tier 发动机、固定 Pack 一致性对 Custom Pack 跨包强组合。维护攻略明确反对因早期升过级就依恋低价值 Tier 1/2 宠；已投入经验不是终局保留理由。

## 顺序、格位与规则分歧

Grounded SAP 将战斗拆为 before battle、start of battle、before attack、after attack，再进入 Hurt、summoned、faint、after faint、friend/enemy faints、knock out、transform、counter、宠物消失与空前位等普通队列。相同 trigger type 按攻击力从高到低，攻击力相同则随机。

Two Average 的综合攻略同样说攻击力高者先触发，却在 FAQ 写同攻击力时以生命破平。两者冲突，官方 Update 48 没有提供总规则裁决。本 dossier 采用 `disagreement`：不把任一破平方式提升为项目参考规则，只确认“攻击力会影响同类触发顺序”。

2022 Steam 讨论展示了规则不可见的后果：Badger 与带 Honey 的 Cricket 同时死亡时，玩家无法判断 Badger 亡语和 Cricket 召唤/蜜蜂出现的先后，回复者也明确承认只知道部分高攻击优先，无法解释全部情况。Grounded 后来的“faint → after faint → friend/enemy faint → fainted pets disappear → empty front space”分解正是这种报告需求的答案，但仍为社区测试资料。

## 生命周期与负面/重做案例

1. **Custom Pack 跨包强件上限**：Update 44 为标准 Custom Pack 增加最多 5 个 super pets 的限制，官方直接说明同时平衡标准 Pack 与 Custom Pack 极其困难且耗时，并希望减少未购买全部 Pack 的玩家受到的惩罚；Wild Custom Pack 保持无限制。把“设计沙盒”和“有公平预期的标准环境”拆开，是明确的作用域治理。
2. **Arena 难度从隐式经验匹配改为显式选择**：Update 44 将随玩家经验变化的 Arena 敌队强度改成 Normal/Hard/Super Hard/Cursed，由玩家主动选且保持稳定。官方目标是让难度一致、更像通常的游戏设置，改善失败归因。
3. **Star Pack 整体重做**：Update 34 同时重做视觉和机制，官方希望得到“更成熟的 archetypes”，并让 Perk 与 Ability 使用同一视觉语言。资料能证明旧包的体系成熟度需要提升，但不足以给每个旧单位判失败原因。
4. **确定性 bug 影响异步公平**：Update 40/44/48 连续修复 before-attack、jump-attack、fainted friend、额外 Tiger trigger 等问题；Update 40 特别指出多个 before-attack 触发器曾令 Versus 双方得到不同战斗结果。这不是小表现问题，而是自动战斗回放一致性问题。
5. **触发上限与条件重写**：Update 48 把 Takin 从“前方友军受伤、每回合有限临时增益”改为“相邻友军攻击、每第三次三倍”的永久节奏，并调整多个 works-per-turn/次数条件。规则事实明确，但官方未给出胜率或设计原因，所以不声称旧版本失败。

## Daily Mode 与敌人包

Daily Mode 让所有玩家每天面对同样的 14 支敌队。其研究价值不是“每日内容”本身，而是把敌人包从隐藏异步抽样转成可比较的固定序列：玩家可把同一失败归因到构筑、站位或资源路线。公告没有提供敌队预告程度、奖励或重试细则，这些保持未决。

## 对本项目的迁移

可迁移：

- 召唤必须受显式格位和队列约束；“能召多少”由死亡腾位、召唤时点和保留后排发动机共同决定，不能只写倍率。
- 同一属性既可决定战斗数值，也可决定触发顺序，但破平规则必须唯一、官方化并进入战斗报告；社区长期猜测不是可接受状态。
- Fish+Bison 展示“低阶 3 级单位是条件，Tier 4 单核是收益者”；盾体系也可让全队防御成为发动条件、单一射手成为收益者，但 owner、倍率、上限和离场语义必须明确。
- 自伤/Hurt 链需要减伤、触发上限和递归顺序三重护栏；只做“受伤就反击”会产生无限链或自杀式不可读结果。
- 标准模式与 Wild 沙盒可以采用不同合法组合规则，避免为了无限组合牺牲主模式公平和可读性。
- 固定敌人序列比隐藏异步对手更适合单人塔爬，可结合预告和复盘验证 counter package。

不可直接迁移：

- SAP 的五格相撞队列、每回合 10 金清零、十胜/生命制、Pack 购买边界和异步匹配不是本项目默认规则。
- Custom Pack 的商业拥有权问题不适用于项目，但“跨模块组合让平衡成本爆炸”的设计问题适用。
- Arena 的无限商店思考时间和 Versus 倒计时不可直接决定本项目独立战术指令时限。
- 当前构筑攻略缺少公开胜率样本，不应把列出的队伍称为统计最优。

## 未决问题

- 官方没有公开完整 order-of-operations；Grounded 与综合攻略的同攻击力破平规则冲突。
- Team Wood 官网新闻正文无法服务端读取；Steam 官方公告正文可核验，但部分 Update 48 数值改动没有设计原因。
- 当前两套构筑都以 Turtle Pack 经典单位为主；不能外推到 Weekly、Star、Golden、Danger 或 Custom Pack。
- 缺少可访问的当前胜率/采用率数据库；“强构筑”只能解释结构与反制，不能量化强度。

## Disposition

- `disposition`: `anchor-retained`
- 置信度：商店/合并/召唤格位和官方生命周期为高；2026 两套构筑结构为中高；触发破平为低且有明确分歧；当前强度为低。
- 判定：10 个实质非商店页面，覆盖 `official-rules`、`official-patch`、`official-dev`、`strategy-guide`、`detailed-review`、`community-analysis` 六种来源类型；含两套具名版本队伍、明确召唤格位/攻击顺序材料、独立反制视角和五个生命周期/重做案例，达到 anchor 门槛。
