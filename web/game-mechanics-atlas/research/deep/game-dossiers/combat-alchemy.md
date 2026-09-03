# Combat Alchemy: Autobattler / Battle Alchemy: Autobattler

## 身份、版本与研究深度

- `title_id`: `combat-alchemy`
- Steam App `2680510` 当前商店名为 `Combat Alchemy: Autobattler`，开发 / 发行均为 Fantasy Forge Games；Windows-only、免费、仍为 `Coming soon`。Steam Community 与公开 appinfo 仍显示旧名 `Battle Alchemy: Autobattler`。
- itch.io 页面使用 `Battle Alchemy: Autobattler`，由同一开发者发布并直接链接 Steam App `2680510`；描述、画面和玩法身份一致，足以确认它是同一项目的旧名 / 并存名，但不能据此推定精确改名日期。
- itch.io 当前为免费、`In development`、HTML5 / Unity 服务器多人原型；访问时嵌入构建标记 `productVersion=1.0.2`。可读机制材料来自 2023-11 至 2025-07 的旧版玩家帖子，以及 2023-12 的两个官方补丁节点。不能假定这些规则完整延续到 2026 的 1.0.2 Web 构建或未来 Steam 版。
- 深度：2 篇官方补丁、5 篇玩家帖子、5 条标准化 evidence。资料足以有边界地拼合一套单角色 `购买阈值暴击 + Thrust + dodge` 历史路线，并说明装备精炼、资源上限和一个跨年复现漏洞；不足以建立当前构筑、完整装备表、敌人反制或胜率。因此 disposition 为 `retained`，但只作为历史 Web 原型样本。

## 来源包与证据边界

| 功能 | 数量 | 内容 |
| --- | ---: | --- |
| 官方补丁 / 规则变化 | 2 | Boon / 技能 / 武器被动与经济奖励重做；v0.2 的 Dagger、Ninja、Thrust、potion 上限和护甲重做 |
| 实践与社区观察 | 5 | 未装备装备的永久升级收益；Ninja / Rogue / Assassin 购买计数漏洞；dodge + crit 实战方向；tooltip、对手信息与 UI 问题 |

两篇补丁同属开发者一手来源；五篇帖子来自 itch.io 社区，其中两名玩家分别在 2024 与 2025 年复现同一零金币购买漏洞。社区帖子可以支持观察到的路线、问题和复现，不代表总体口碑或当前版本状态。商店、Steam API、appinfo 和 WebGL shell 只用于身份 / 版本边界，不进入 deep source 或 evidence。

## 产品与版本地图

| 节点 | 可核验内容 | 不可外推 |
| --- | --- | --- |
| 2023-11 Web 原型 | 玩家说明 1-star / 3-star 装备升级奖励即使装备未穿戴也持续生效；装备树升级数受上限约束 | 不知道后续补丁是否保留全部奖励、数值和 UI |
| 2023-12-02 补丁 | 调整 Attack / AP / ASPD / Crit / Armor / Health / ES / Dodge 被动；强化 1-star 武器效果；加入 Boons、Assault / Counter 与技能；收紧 Life 奖励并移除两个 Gold 奖励；重做四件装备的 5-star 效果 | 公告没有给出完整技能 / Boon 表，也没有解释每项改动的原因或实际胜率影响 |
| 2023-12-15 v0.2 | 加入 Dagger tree、Ninja、Thrust 与四个 Thrust 技能；Ninja 每回合买 4 件物品永久获得 4% Crit；调整 Rare / Epic potion 上限、加入 Legendary potions 与 Rare-cap 奖励；重做两件护甲 | `Ninja` 是官方当时名称；玩家后来称 `Rogue` 或 `Assassin`，无法确认是正式改名、记忆差异还是 UI 版本差异 |
| 2024-01 至 2025-07 社区 | 两名玩家分别报告没钱时尝试购买仍会计数，使永久 Crit 可在首个商店堆至 100%；另一名玩家把实战路线概括为 Payday 2 式 dodge + crit | 无官方修复记录；不能声称漏洞仍存在于当前 1.0.2，也不能把一句构筑概括扩写为完整最优 loadout |
| 2026-09 访问快照 | Steam 仍未发行；itch Web shell 标记 1.0.2，CDN 文件时间晚于旧 devlog | 未实际游玩当前构建；shell 版本号不证明旧机制仍在、数值未变或与 Steam 同版 |

## 核心循环与真实决策

1. 商店公开框架是把基础元素合成为高级元素，再制作武器与护甲，让单一玩家角色自动与怪物或其他玩家角色战斗，并收集 Philosopher's Stone shards。此处只用来理解产品身份，不把商店文案当深证据。
2. 官方 v0.2 同时暴露三组历史系统切片，但没有资料证明它们属于同一套构筑：`Ninja` 的四次购买条件把 Gold 与商店操作转化为长期 Crit，`Thrust` 再读取攻击是否暴击并造成额外伤害；装备树另行承载精炼；potion 容量则是独立的资源积累边界。
3. 玩家说明装备的 1-star / 3-star 升级奖励在未装备时也永久存在，因此装备决策不只是“当前穿哪一件”，还包括是否投资多棵装备树以获取账户 / 角色层面的持续收益。`+3 Weapon/Armor Tree` 又把可持有的升级数量做成有限容量。
4. v0.2 的 Rare / Epic / Legendary potion 数量和分类上限，以及新增的 Rare-cap 奖励，说明资源池有显式容量而非无限累积。此前 Life 门槛和跨回合 Gold 奖励也被收紧 / 移除。
5. 玩家能否理解路线受到界面限制：一名玩家报告 AP、Thrust、Replenish、升级树、对手技能 / 装备缺少解释与 Water tooltip 损坏；另一名玩家报告界面缩小 / 消失。它们是局部报告，不建立总体可用性结论。

## 历史构筑：Ninja 购买阈值暴击 + Thrust + dodge

这不是某篇玩家帖子逐项列出的完整攻略，而是同一项目的跨日期历史资料重建：Ninja / Dagger / Thrust 来自 2023-12 官方补丁，dodge + crit 方向来自 2024-11 玩家概括；精确版本连续性未知，不能保证这些组件在同一版本同时成立。

- **engine**：Ninja 在一回合内购买 4 件物品后永久获得 4% Crit；重复满足阈值积累暴击率。官方 v0.2 同时加入 Dagger tree、Thrust 和四个相关技能。
- **state/resource**：Gold、当回合成功购买计数、永久 Crit、Dodge、Dagger / Thrust 与技能槽；商店概率、技能槽数量和 2024-11 时这些组件是否仍同时存在均未知。
- **trigger**：正常规则是当回合第 4 次购买完成时增加 Crit；Thrust 在一次攻击暴击时触发伤害。玩家发现的缺陷是无 Gold 的失败购买尝试仍被计为购买。
- **payoff**：更高 Crit 提高攻击暴击频率，并让 Thrust 的暴击触发伤害更频繁地发生。公开材料没有完整公式、递归规则或伤害上限。
- **survival**：一名后续玩家把可行思路概括为 Payday 2 式 dodge + crit；dodge 负责延长单一角色的输出时间，不是伤害转换本身。
- **spatial condition**：这是单角色自走棋，没有队伍阵型或多单位站位证据；只确认效果由同一战斗角色及其 Dagger / Thrust / 技能选择承载，具体槽位结构未知。商店操作发生在战斗外。
- **payoff owner**：玩家的单一战斗角色拥有 Crit、攻击和 Thrust 伤害；Ninja 被动拥有购买计数读取；dodge 只提供生存。没有 team Defense / HP 转单核攻击的规则。
- **economy / pivot**：若该跨日期组合确实连续存在，正常入口要求一回合完成四次合法购买，因此只能确认 Gold 与成功购买次数之间的机会成本；商店概率、购买物类型、成型时点和替代路线没有资料。装备精炼容量与 potion 上限属于后文独立切片，不用于闭合本路线。
- **counter / abort**：没有可靠敌人技能、抗暴、必中、破闪避或节奏反制资料。可确认的失败缺口只有 Gold 不足、当回合成功购买未达四次、Crit / Thrust 链未形成或 Dodge 生存不足；这些也没有对应敌方实例，不能冒充已证实反制。
- **version context**：Ninja / Dagger / Thrust 仅由 2023-12 v0.2 补丁确认，dodge + crit 仅由 2024-11 玩家概括确认；两者属于同一项目的历史资料，但是否同版并存未知。零金币购买属于漏洞复现，不属于合法经济路线；当前 1.0.2 是否仍能执行这套组合未知。

## 装备精炼与机会成本

- 1-star / 3-star 永久升级奖励即使装备未穿戴仍存在，使“做过的装备”可以成为长期属性投资。这类系统把物品从临时 holder 工具变成组合式成长节点。
- `+3 Weapon/Armor Tree` 增加可拥有的装备升级数量上限，说明永久收益并非完全免费。玩家需要在当前战斗装备、未来精炼价值和树容量之间做组合投资。
- 2023-12-02 补丁把 1-star weapon effects 统一加强 100%，并重做 Katana、Broad Sword、Plate Vest、Simple Robe 的 5-star 效果；2023-12-15 又重做 Gladiator Armor、Cuirass Armor。可以确认这些节点发生过，但没有足够正文建立具体完整装备路线或平衡原因。
- 本项目若借鉴，应明确永久奖励的真正 owner、装备卸下后是否保留、升级容量如何计算、替换是否退款，以及 UI 如何区分“当前穿戴效果”和“已解锁永久效果”。

## 资源上限、跨回合积累与生命周期

- 2023-12-02 补丁把 Life 奖励限制为低于 5 Life 才出现，并移除永久 `+2 Gold` 与下回合 `+6 Gold` 两个奖励。这显示开发者主动收紧可累积 / 跨回合经济入口，但公告没有给出原因，不能说它们必然过强。
- v0.2 把每类 Rare potion 限为最多 5、Rare 总上限 15，把 Epic 总上限从 3 提至 6，并加入两个 Legendary potions 与一个 `Rare potion 上限 +5` 奖励。总上限、分类上限与扩容奖励是三个不同规则层。
- 这些变更适合作为“积累系统需要显式 guard”的生命周期样本：限制器必须说明作用层、扩容方式和超额处理。不过缺少完整 UI / 结算文本，不能补写丢弃、替换或溢出优先级。

## 零金币购买漏洞

- 官方规则组件是“一回合购买 4 件物品，永久 +4% Crit”。2024-01 玩家报告即使没有足够 Gold，只要继续移动 / 尝试购买元素，仍会反复触发购买计数；2025-07 另一名玩家独立报告 Assassin 在首个商店即可这样堆到 100% Crit。
- 两次跨年复现提高了“历史 Web 版曾存在计数与支付提交脱钩”的可信度，但名称分别为 `Rogue`、`Assassin`，官方 v0.2 则称 `Ninja`。只确认三者描述的是同一“四次购买永久暴击”触发，不断言正式改名关系。
- 根本设计问题是触发监听了购买意图 / 交互，而不是成功扣费后的已提交交易。正确 guard 应只在扣费成功、物品实际进入合法容器后原子递增计数，并对失败购买、拖动、取消和重复事件保持幂等。
- 没有开发者修复公告，也没有当前 1.0.2 实测。因此该记录是历史 lifecycle / exploit 证据，不是当前漏洞披露或可推荐玩法。

## 可读性与失败解释

- 一名玩家无法理解 AP、Thrust、Replenish、升级树和敌方技能 / 装备，并指出 Water tooltip 损坏；另一个玩家报告 UI 会缩小 / 消失。这两份材料支持“关键 reader / owner / 对手信息没有稳定呈现”的局部风险。
- 单角色战斗若只显示最终 Crit 或结果，玩家无法区分合法购买积累、失败交互误触发、装备永久奖励、dodge 生存和 Thrust 额外伤害。报告至少应列出成功交易、购买阈值进度、永久属性来源、当前穿戴 / 未穿戴奖励、触发技能和敌方命中 / 闪避结果。
- 没有可靠的敌方反制、战斗报告、失败关卡或对局统计，不能编造“某敌人克制该构筑”。当前最重要的缺口不是再列更多效果，而是建立对手技能可检查性与来源归因。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

### Steam、身份与当前构建

- Steam Store / App Details / appinfo 确认 App 2680510、当前名、Windows-only、免费、`Coming soon`、开发 / 发行者与无官网；Steam Community / appinfo 仍显示 `Battle Alchemy: Autobattler`。这些页面仅作身份和预发行边界。
- Steam News API 为 0，Reviews API 为 0；Discussions 没有主题，Guides 没有玩家条目。没有可建立当前 Steam 规则或构筑的社区材料。
- itch 页面直接链接相同 Steam App，开发者、描述和画面一致。当前 WebGL iframe 可加载，shell 标记 Fantasy Forge / Battle Alchemy / 1.0.2；未进行实际游玩，且 CDN 时间晚于 2023 devlog，因此不把旧帖自动提升为当前规则。

### itch、外部与排除路线

- itch 页的 5/5 玩家评论和 2/2 官方 devlog 均完整读取；七页全部登记。两篇补丁提供规则 / 生命周期，两名玩家独立复现购买计数漏洞，一名玩家提供 dodge + crit 实战概括，两名玩家补充永久装备奖励与可读性 / UI 边界。
- YouTube 以当前名、旧名和开发者名精确检索，未找到目标游戏的实质视频。Bing 结果主要为同名炼金噪音；Google 需要 JavaScript；DuckDuckGo / Brave 触发反机器人验证后停止，没有绕过。
- Reddit JSON 返回 403，Wayback / CDX 连接超时，Discord 只暴露需登录频道，均未绕过。没有可读 wiki、独立攻略、统计、当前补丁链或完整技能数据库。
- 精确同名 GitHub 仓库 `BadestTrip/CombatAlchemy` 于 2026-05 创建，是 Godot 的黑暗奇幻实时探险 / 现场调药项目，与 Fantasy Forge 的 chibi PvP 单角色 autobattler 没有身份链接，已排除，不能用其代码反推 App 2680510。

继续检索已收敛到同一 itch 小型语料、商店身份页、登录 / 反机器人边界或无关同名项目，不再产生新的合法构筑、owner、敌方反制或当前版本信息。因此在一条历史构筑、装备精炼、积累 guard 和漏洞边界闭合后停止，不制造当前 meta 或第二套构筑。

## 对本项目的可迁移与不可迁移信息

可迁移：

- 盾 / 防御只是 survival supplier；若要伤害，必须另设装备、遗物、技能或英雄 reader，并声明输入属性、输出 owner、槽位成本、上限与自反馈规则。
- 永久装备奖励可鼓励横向精炼，但必须用装备树容量、经济成本和清晰的已穿戴 / 已解锁状态约束。
- 购买阈值只能读取成功提交的交易；扣费、入包、计数和奖励触发应原子化，失败交互不得产生构筑资源。
- 总容量、分类容量与扩容奖励要分层表达；跨回合资源尤其需要显式 cap、快照和溢出规则。
- 对手技能、装备、触发来源和 tooltip 必须可检查，否则玩家无法区分构筑失败、信息缺失与程序缺陷。

不可迁移：

- 不复制任何具体武器、护甲、potion、Ninja 数值、Philosopher's Stone 目标或漏洞利用路线。
- 不把 dodge + crit 一句话当成完整平衡样本，也不把旧 v0.2 组件称为当前最佳构筑。
- 不从单角色原型推导队伍站位、羁绊、团队 defense / HP 转单核攻击或冰盾 / 土盾的规则。
- 不把玩家命名差异当成正式职业改名，不推断官方已修复漏洞或为何移除 Gold 奖励。

## 未决问题

- 当前 1.0.2 Web 构建与 2023 v0.2 的技能、装备、被动、potion、职业和经济差异。
- Steam 版是否有公开 build、是否与 WebGL 同版、何时正式发行以及名称迁移历史。
- `Ninja`、`Rogue`、`Assassin` 的正式命名关系。
- Crit、Dodge、Thrust、AP、Replenish、ES 的完整公式、触发顺序、上限、递归和 UI 归因。
- 装备 1-star / 3-star 永久奖励的 owner、卸下 / 替换 / 重置规则与装备树容量算法。
- potion 满容量时的溢出 / 替换行为，以及 Rare 分类上限与总上限的结算顺序。
- 敌人 / PvP 对手的技能检查、抗暴 / 命中 / 闪避反制、失败条件与战斗报告。
- 购买计数漏洞是否在任何后续版本修复。

## Disposition

`retained`

保留对象严格是 2023–2025 itch Web 原型的历史机制：官方补丁给出购买阈值永久 Crit、Thrust、装备 / potion / 经济变更，玩家材料提供永久精炼实践、dodge + crit 方向和跨年漏洞复现，满足功能不同的 rules + practical 最低包。资料允许记录一条带版本连续性缺口的单角色历史组件路线，以及独立的装备精炼 pivot；敌方 counter 仍是明确缺口，因此只生成 5 条证据，不升级为 anchor，不支持当前 Steam 构筑、当前 meta、胜率或平衡结论。
