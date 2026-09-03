# Kādomon: Hyper Auto Battlers

## 身份与证据边界

- `title_id`: `kadomon`
- 子类型：生物收集、线性队列自动战斗、类型协同与 roguelite 路线进化。
- 状态：2024-03-25 Early Access；2025-04-07 进入 1.0；1.0.12（2025-05-21）是本次能见的最后实质平衡/修复更新。
- 核心版本边界：2023 Demo、0.3.x Early Access、0.4–0.5 重做、1.0 launch 与 1.0.7–1.0.12 能量/内容迭代分开处理。
- 置信：中高。官方 FAQ 和版本链很完整，社区有具体队伍、进化配方和 Boss 反制；但无当前统计、完整官方数据库和全部行动/目标顺序规范。
- 本档不把商店页、收集数量或发布热度当成机制深度；不把 Demo 高 Ascension 构筑写成 1.0 current meta。

## 实质来源

本 checkpoint 登记 26 个非商店页：

- 1 个 `official-dev`：`src-kado-official-faq-2024-02-20`。
- 11 个 `official-patch`：`src-kado-official-030`、`src-kado-official-032`、`src-kado-official-040`、`src-kado-official-050`、`src-kado-official-100`、`src-kado-official-101`、`src-kado-official-107`、`src-kado-official-109`、`src-kado-official-1010`、`src-kado-official-1011`、`src-kado-official-1012`。
- 2 个 `strategy-guide`：`src-kado-guide-hyper-evolution`、`src-kado-guide-misprint`。
- 12 个 `community-analysis`：`src-kado-thread-beryl-poison`、`src-kado-thread-three-types`、`src-kado-thread-gnocking-wall`、`src-kado-thread-gnocking-teams`、`src-kado-thread-gnocking-diversity`、`src-kado-thread-old-final-boss`、`src-kado-thread-portal-one-hp`、`src-kado-thread-portal-counter`、`src-kado-thread-type-readability`、`src-kado-thread-speed-readability`、`src-kado-thread-wormonger`、`src-kado-thread-defense-observation`。

官方页面建立规则与版本；指南建立超进化/收集的实际条件；讨论只用来建立具体玩法、反制和有版本标签的玩家观察。没有任何单帖被写成整体统计。

## 真实循环：路线、四人队、经验与可替换 Box

玩家在分支区域里选择战斗、商店、事件、camp 与精英/Boss 路线。可见战斗队是四人线性顺序，「前方邻位」「前方所有 Kādomon」和作用于双方的文案需要区分。战斗主体自动解算；本次没有找到可核验的战中手动施法/目标切换层，因此不发明主动干预规则。

招募、升级和替换不是一条线：

- 战斗、事件和放生/释放队员可生产 XP；旧版 Lv2 单位可换 10 XP Candy，这是带特定时期标签的经济交易。
- Lv2/Lv3 进化改变实体与能力；Early Access 的 Lv3 上限在 1.0 被取消，可继续升级。
- Lv3 持有指定物品后还需再获得 XP 才触发 Hyper Evolution；配方在设计上把物品从「战斗装备」变成「进化钥匙」。
- 队伍之外的 Box 是替换窗口。Portal Boss 的开发者回复明确把「预备其他单位、后续换入」作为反制之一；但旧最终 Boss 又会把 Box 和本局放生过的单位召来当敌人，两个时期必须分开。

## 两条正交轴：类型门槛与战斗机制

类型不等于核心战斗机制。社区规则解释指向「同类型数量达到门槛后发 buff」，Physical 也是一种 type；指南则证明进化前达到 `Air 4`、`Ice 3`、`Earth 3`、`Physical 2 + Fire 2` 等条件会改写 Alternate Evolution 分支。

这意味着类型轴可以同时承担：

1. 水平组队门槛；
2. 进化分支开关；
3. 一个单位能否桥接多个队伍的标记。

但真正驱动战斗的轴另外存在：

- Poison/Decay：状态生产、传播、触发与结算；
- Shield/Armor/Thorns/Heal：生存、受击返击与防御转攻；
- Summon：额外身体、召唤时触发和占位；
- Speed/Energy：攻击频率、Super 频率与受击充能；
- front/ahead/target：谁先受压、哪个效果读取哪个目标。

因此一个 Poison 队可同时用 Water shield 保护状态付费主体、用 Bug summon 生产触发次数、用 Dark 提供 Last Wish/续航。「冰盾」可以同时属于冰类型桥和盾机制，但不应被迫合并为一条垂直系。

## 具名构筑一：Beryl + Snake + Water starter 的 Poison—Shield 线

队伍：`Beryl + Snake + Water starter + filler`（`src-kado-thread-beryl-poison`，2023 Demo/Ascension page 14 单个样本）。

- **driver/engine**：Snake 向群体上 Poison；Beryl 继续 AOE 上毒并触发目标的全部 Poison stacks。
- **state/resource**：敌人毒层，Beryl 的 Shield/Strength，队伍行动频率。
- **payoff**：Beryl 把已堆状态结算成 AOE/集中伤害；Snake 是状态供应者而非最终唯一伤害归属。
- **survival**：Water starter 给 Beryl shield 和 Strength；备选 unicorn/bear 提供 charge，bear 可放首位先承压并 buff Beryl。
- **spatial condition**：Beryl 默认前排；有 bear 时由 bear 站前、Beryl 站其后接 buff，改变受击和状态结算节奏。
- **payoff owner**：Beryl 拥有主结算，Snake 拥有 AOE Poison 来源，Water starter/bear 拥有对 Beryl 的防御/属性支持。
- **pivot/counter**：unicorn 与 bear 是可替换第四位；玩家还通过 `?` 节点提前拿 Beryl，避免后期直接打四 Beryl。Cleanse/减毒、快速爆发与反多次触发 Boss 会破坏这条线。
- **version context**：只作 Demo 历史构筑；1.0.7 之后能量获取规则已改，不保留原频率/数值。

## 具名构筑二：3 Fire / 3 Water / 3 Grass 多类型桥

路线：`Snose + Baleam + Shiftyke/Sewindler + primarily Water unit`（`src-kado-thread-three-types`，0.3.x 挑战）。

- **driver/engine**：Snose/Baleam/Shiftyke 先提供 `2 Grass / 3 Fire / 1 Water`；Shiftyke 进化为 Sewindler 后加主 Water 单位，变为 `3 Fire / 3 Water / 2 Grass`。
- **state/resource**：四个队伍槽位、每名单位的 innate type、进化后 type、物品/campfire 新增 type。
- **payoff**：三个类型门槛同时闭合；这是水平桥接/挑战目标，不代表一个单位同时拥有火、水、草的全部战斗责任。
- **survival**：来源没有给出完整生存公式；Water/Grass 门槛不能被自动写成 Shield/Heal，需看具体能力。
- **spatial condition**：四位线性队伍仍需要前排承压和邻位词义；类型门槛不会自动修复站位。
- **payoff owner**：type threshold 是队级规则；具体攻击、Super、Shield 仍归相应 Kādomon。
- **pivot/counter**：缺的第三个 Grass 可由 Snose 装 Spare Button、campfire 添加 Grass 或进化分支补齐。Shameleon 利用敌方首位 Grass 的一步含玩家假设，本档不将其当成已证规则。
- **version context**：Early Access 0.3.x；只证明「单位＋进化＋物品＋campfire」是四种标签桥接来源。

## 具名反制构筑：Poison + Summon + Poison-to-Shield 对 Gnocking

0.3.0 launch 的 Gnocking 会从玩家 buff 获利，而自走战斗不允许玩家像牌组构筑那样临时「不打 Power」。社区给出的具体反制包是：`Poison + minion summons + poison-on-summon items + a unit that gains Shield when Poison is applied`。

- **driver/engine**：召唤时上 Poison 的物品把新身体变成状态触发源。
- **state/resource**：Poison stacks、summon 数/触发次数、我方 Shield，Boss 已复制的 buff 包。
- **payoff**：Poison 结算 Boss，同时每次上毒转 Shield，形成进攻与生存共享一个状态生产器。
- **survival**：Poison-to-Shield 付费主体和 minion 墙拉长战斗；它不依赖大量可被 Boss 同等利用的我方 Strength buff。
- **spatial condition**：summon 需有可用位置，前排先受压；线性队伍的“ahead”效果仍需明确范围。
- **payoff owner**：每个 summon 是触发发生体，状态/物品源应保留 root owner，毒转盾单位拥有 Shield 收益。
- **pivot/counter**：可替换反制包包括 Snose 按 Poison 降 Attack、Nibbolt Shock、Rocorm 从 buff 获得更高自身价值、Grubuddy 自堆 Strength 同时堆 Slow，以及 `4 Earth + Thorn + Worm`。
- **version context**：0.3.2 官方随后将 Gnocking 改成只复制一次并移除 Crown，证明上述压力是被确认并降低的历史考题，不是当前必然强度。

## Boss 能力试卷与适应窗口

### Gnocking：自动 buff 构筑的反向镜像

这个考题最有价值的不是「禁用 buff」，而是它暴露了自走战斗的自由度差异：如果 buff 是自动被动，玩家无法在战中临时停止。所以反制必须出现在 Boss 预告之前，通过替换队员、物品或状态路线完成。玩家一方认为这是构筑试卷，另一方认为它压缩多样性；0.3.2 的官方削弱是生命周期结果。

### 旧最终 Boss：收集/Box 变成负资源

0.3.0 首日最终 Boss 会召唤本局放生过的单位和 Box 单位；Box 又能同步获得 XP。这导致社区反制是 World 2 后少拿新单位或 Boss 前清 Box。它不是健康的“备用阵容适应”，而是让收集和实验变成隐藏负债，不适合本项目。

### Portal Boss：多段伤害变成 Max Health 反击预算

Portal 每次被命中会反复降低随机队友 Max Health。开发者明确说这是 Boss gimmick 而非 bug，反制是 rush down 或准备 Box 替补；负数/显示异常才是 bug。玩家后续发现，小额多段和随机三连击会发生过多反击，第二阶段才显得全队突然变 1 HP。

可行的 1.0 后低样本量反制包括：高 HP/Armor、special spam、Lightning + Poison、精英奖励「每次使用 special 获得 HP」、或用 Strength 一击打破 portal。这说明「命中次数」本身是一种可被 Boss 反转的资源；如果不显示实时 Max HP 与反击源，失败就会看起来像无原因跳变。

## 能量、速度与确定性

1.0.7 移除被动获得 Energy，改为攻击获得 10，受伤获得伤害值 50% 的 Energy。1.0.9 又明确 triggered attacks 不给 Energy、致死伤害不给对方超额能量，一次获得足够 Energy 可连续放多次 Super。因此攻击、触发攻击、受击、自伤、致死伤害和 Super 消耗必须分开记账，否则很容易自循环。

0.3.2 已经把单战触发上限降到 50，并取消 faint 后已排队伤害。0.4.0 又修复 Tainee + Ice Club 无限循环。1.0.9–1.0.12 持续修复多次 Super、快速伤害、self-damage Energy、目标和所有权。这些不是一个「加上限」就解决的问题，至少需要 root owner、triggered-attack 标记、死亡重验证、共享预算和可读报告。

Speed 讨论则暴露表达问题：当时 Speed 与 Energy 都是「数值越低越好」，而玩家仍认为行动频率是核心构筑轴，并与 Air/Frost 类型交叉。建议包括 higher-is-better、行动时间线和行动 spotlight。本档不自行判定哪个界面方案正确，只保留“高频状态需要单一方向、预期下次行动与来源归因”的问题。

## 进化的价值与代价

Alternate Evolution 把 type threshold 变成了比 `+X%` 更强的决策：队伍宽度会改写单位的下一形态，不只是获得一个常驻 buff。可迁移的是「水平构筑状态解锁垂直分支」，但不必移植其隐藏成本：

- 不可逆选择会增加路线读档和规则变更负担；
- 隐藏配方要求外部表格/图鉴，会把理解变成搜索作业；
- Lv3＋指定物品＋再获 XP 是三重条件，如果全 roster 都这样做，内容量与版本迁移成本很高；
- 对本项目更合适的方式是让少数英雄/装备/遗物明示改写技能、付费主体或类型桥，而不是给每个单位一张秘密配方表。

## 版本、失败与重做

1. **Gnocking 自动 buff 反向复制**：0.3.0 玩家报告该 Boss 对被动 buff 构筑过度压迫；0.3.2 改为只复制一次并移除 Crown。
2. **50 次触发上限与死后队列**：0.3.2 降低每战 trigger 上限，并让 faint 单位已排队伤害不再播放；说明递归预算和死亡重验证是两个问题。
3. **Tainee + Ice Club 无限循环**：0.4.0 修复；用来证明局部组合仍能绕过一般触发规则。
4. **旧最终 Boss 惩罚收集/Box**：玩家为降低 Boss 强度而停止招募或清空 Box；这把备用资产变成隐藏负债。
5. **Portal 多段命中过度反击＋显示延迟**：多段队在不知情中把全队 Max HP 降到 1；机制与负数/未刷新显示 bug 必须分开。
6. **Speed/Energy 「越低越好」与战斗不可读**：开发者主动征求调整；社区保留该轴却要求方向一致/时间线/聚光。
7. **旧 Wormonger/Earth 前排负反馈**：被集火反而堆高 Thorns/Shield，使弱攻击和多段命中变成负资源；只记为 2023 单帖历史样本。
8. **存档初始化/分支风险**：0.5.0 时出现新存档未初始化导致无限加载，1.0 experimental 又警告分支切换可损坏 save。本 checkpoint 不单独登记该三个页面，所以它只作档案风险，不计入新负面 case 数。
9. **1.0 快速触发/能量/目标修复簇**：1.0.7–1.0.12 连续重写能量获取并修复 triggered attacks、self damage、lethal hit、multi-Super、目标和 enemy-Super 所有权；说明这些是同一调度系统下的多个边界。
10. **物品/队伍操作复制与软锁**：0.4.0 修复全员放生无法推进、战中 party/box 拖动卡死、商店物品复制和路线跳过；证明阵容变更要有原子性与阶段门禁。

本 checkpoint 将前 7 项和第 9 项计为 8 个新的 negative/reworked cases；第 8 项未登记实质来源，第 10 项是多个工具链修复的合并风险，不重复凑数。

## 对本项目可迁移

### 可迁移原则

- **类型是横向桥，机制是纵向责任**：冰盾、土盾都能进入 Shield 系；冰/土只决定另一层组队、反应或进化条件。
- **水平宽度可解锁垂直变体**：达成类型门槛可改写英雄 active/passive 或 payoff owner，比单纯 `+X%` 更有身份。
- **团队 engine 集中到可识别主体**：Poison、Shield、Summon、Speed/Energy 可交叉，但应明确谁产生状态、谁转盾、谁最终打伤害。
- **Boss intro 预告关键规则**：自动 buff 复制、多段反击、Max HP 削减和召唤物墙都必须在可替换窗口之前显示。
- **备用队员是反制窗口而非免费保险**：Box 替换展示了它的价值；本项目需要后续另行确认 reserve 容量、成本和 XP 规则。
- **触发链需六个独立守卫**：root owner、child/triggered eligibility、死亡重验证、shared cap、每时间频率和自循环标记；不要用一个全局 50 次上限遮住所有原因。
- **报告分开实际状态和显示状态**：Portal 案例证明 Max HP 在背景已改变而界面未刷新会把规则失败误读为 bug。

### 不可直接迁移

- 不移植捕捉、Misprint 稀有色、seed 刷取、图鉴收集和基于账号 RNG 的收集循环。
- 不移植「把稀有变体带到 camp/完成 run 才永久解锁，失败则丢失」；它是收集风险，不是战斗构筑决策。
- 不移植放生英雄换 XP Candy 作为普通进度骨架；它会把角色变成经验耗材，与本项目英雄级 roster 契约不一致。
- 不为全 roster 制作 Lv3＋隐藏物品＋再获 XP 的秘密超进化表。
- 不让备用队员与主战队免费同步获得全部 XP，也不让 Boss 强迫玩家卖掉/清空收藏。
- 本项目已确认两个独立战术指令与三点共享资源；不因 Kādomon 未见战中主动层而取消这一契约，也不把 Kādomon 的任何玩家技能库猜进来。

## 未决问题

- 1.0.12 后的完整当前 Kādomon/物品/被动/Super 数据库与选用/通关率未找到。
- 四人队的完整目标、同速并发、状态结算、summon 占位与 multi-Super 顺序没有公开规范。
- Hyper guide 的每条配方没有独立版本标签；页面 2025 更新不保证所有条目同时代。
- 2026 Portal 反制只有单条社区回复，能证明考题仍被遇到，不能证明最优解或普遍难度。

## Disposition

`retained`

Kādomon 超过普通长尾的两源门槛：官方 FAQ/版本链、超进化配方、两套具名队伍与一套 Boss 反制构筑、类型/状态/盾/召唤/速度/能量正交证据、替换/Box 和八个生命周期失败均可回到来源。不升为 anchor：实战证据高度集中在 Steam 社区，版本跨度大，缺当前统计、完整官方规则库和全顺序规范。
