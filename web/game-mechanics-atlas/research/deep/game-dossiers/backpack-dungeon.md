# Backpack Dungeon / 背包地牢

## 身份、时期与资料密度

- `title_id`: `backpack-dungeon`
- 精确对象：Steam 主 App `4478640`，英文名 `Backpack Dungeon`、中文名《背包地牢》，开发者夜喵，发行商 Shuffle Play Games。
- 独立 Demo App：`4524110`。商店元数据列 2026-03-27 发行，开发者于 2026-04-28 公告 Demo 页面开放；两者都是公开事实，但没有证据说明日期差异原因。
- 主游戏于 2026-07-14 正式发行。可见版本链为 Demo `1.5.5`、`1.5.9`、`1.5.10`，正式版 `2.0.0` 后连续更新，到访问日最新 `2.4.2`（2026-08-31）。
- 与 `Backpack Battles`、`Backpack Hero`、桌游 `Bag of Dungeon` 及普通背包商品通过 App id、开发者、完整标题和页面归属消歧；邻作的异步 PvP、疲劳、配方或网格规则均未导入。
- 本 checkpoint：29 个实质非商店来源——3 个官方开发 / 发行节点、11 个官方补丁、1 个完整 Steam Guide、7 个详细 Steam 长评、7 个规则 / 构筑 / 模式讨论。
- 置信：高。外周触发、三套具名组合、隐藏 Boss、Endless、relic phase、配方 / 商店和连续生命周期均有功能不同的来源；但没有维护 wiki、正式 manual、完整数据库、掉率 / 商店权重或统计，因此是 `retained` 而非 anchor。

## Adaptive-depth 决定

它不是“另一个把物品塞进格子”的弱差异样本。触发球沿选定连通包体的外周运行，使背包周长同时成为容量和全局攻速预算；主动物品必须把方向触点暴露在轨道上，被动物品则竞争内部邻接和 `★` reader。空间、节奏、触发顺序、合成、状态、relic、敌人计时和 Endless 疲劳都被同一几何结构连接。

因此本轮超过普通 adjacent-long-tail 的最低深度：闭合 Gold Crit、Spartan Helmet、Creature 三套结构，补充 Metronome 和 Ranger 两个可验证模块，并深挖隐藏 Boss 多轴反制、Endless Calamity、经济转型、战报、递归、动态 tag、SL 事务和投降护栏。

29 个来源后，新增结果主要重复同一批 Steam 评测 / 讨论、无字幕视频或 Workshop 自定义内容。继续枚举全物品和全 mod 不再改变周长—频率、owner、反制和生命周期理解，故按 diminishing-return test 停止。

## 来源包

- `src-bpd-demo-announcement-2026-04-28`、`src-bpd-launch-2026-07-10`：Demo / 主游戏身份、发行时期与产品规则边界。
- `src-bpd-patch-1-5-5`、`src-bpd-patch-1-5-9`、`src-bpd-patch-1-5-10`：Demo 后期的数据总览、Creature / Frost 与 Ranger 配方节点。
- `src-bpd-patch-2-0-1`、`src-bpd-patch-2-0-2`、`src-bpd-patch-2-0-3`、`src-bpd-patch-2-0-4`、`src-bpd-patch-2-0-5`、`src-bpd-patch-2-0-8`、`src-bpd-patch-2-0-9`、`src-bpd-patch-2-4-2`：正式版平衡、phase ownership、战报、Calamity、SL、递归、外周显示和投降修复链。
- `src-bpd-workshop-launch-2026-07-28`：自定义物品池 / 角色 / 起手与 authored balance 的 provenance 边界。
- `src-bpd-guide-achievements-2-2-5`：四 Rune + Dungeon Core 路线、隐藏 Boss 三免疫形态、1 HP 与事件成就方案。
- `src-bpd-review-spatial-combos-2026-07-16`：触发球、外 / 内空间分工、Spartan Helmet、Greed Blade + Focus Device 实例。
- `src-bpd-review-loop-combos-counter-2026-07-17`：周长节拍、多个 combo、敌人计时、免疫 / 疲劳压力与双层整理负担。
- `src-bpd-review-balance-convergence-2026-07-17`：刷新强求、少数强件、敌人 / potion 答案收敛。
- `src-bpd-review-hidden-boss-thorns-2026-07-22`：Thorns / interrupt 路线遭隐藏 Boss 完全反制与提前预警缺口。
- `src-bpd-review-mixed-damage-armor-2026-07-16`：Adventurer 隐藏 Boss 通关的物理 / 魔法 / Poison / crit / heal / Frost / Armor 结算。
- `src-bpd-review-calamity-dead-choice-2026-07-18`：2.0.4 前 Life Erosion 和一组三项致命 Calamity。
- `src-bpd-review-endless-scaling-traits-2026-08-04`：147 层 HP / summon 缩放、必需件与随机敌人 trait 压力。
- `src-bpd-discussion-creature-pivot-2026-08-17`、`src-bpd-discussion-metronome-2026-08-18`：Creature 入场延迟与 Medium / Metronome 高频 reader。
- `src-bpd-discussion-long-combat-2026-08-18`、`src-bpd-discussion-surrender-2026-08-28`：深层战斗时间、加速计算风险和投降窗口。
- `src-bpd-discussion-relic-reroll-2026-08-20`、`src-bpd-discussion-relic-phase-2026-08-19`：特定 relic 搜索成本与 combat / non-combat slot 归属。
- `src-bpd-discussion-endless-reward-2026-08-26`：Ascension 逐级解锁、Endless 无额外奖励与 mod debug 边界。

## 真实循环与空间规则

玩家先选一名角色，进入 30 层 PvE 地牢。战斗后获得 loot 并进入商店；途中事件、小游戏和特殊路线提供物品、背包、金币、Rune 或独有奖励。商店物品与背包内材料能组成配方，合成产物从待领取区取出。完成 floor 30 后可结算普通路线，或进入 Endless。

战斗准备不是“物品放进去就生效”，而是两层空间共同决定执行：

1. **包体层**：大小 / 形状不同的 backpack 自身也要铺在网格里。触发球沿它所在的连通包体外周循环；玩家能移动球的起点。扩包增加可装物品，却拉长一圈的轨道。
2. **物品层**：武器、medium、法术等主动件带方向触点，只有触点接触轨道上的球才触发。没有主动触点的 armor / reader 可放内部，以 `★` 邻接、每圈、受击、状态或其他物品触发为条件工作。

敌人主要按真实时间 / 自身节奏行动，玩家主动件则按触发球路程行动。因此包体每扩一段，不只是“多一格”，而是把所有依赖每圈或沿轨道的动作一起降频。紧凑包体触发快但 owner 少；大包能容纳更多 active / passive 连接，却可能让敌人行动更多次。玩家长评还明确把被动件放到不增加有效外周的区域，把强力武器触点放在起点附近争取开场爆发。

这和单位站位不是同一个空间，但可迁移的资源思想相同：格位、邻接、路径长度、启动顺序和 owner 数量共享一个空间预算。若未来把它借鉴到英雄自走棋，应迁移“几何会改变节拍”而不是复制旋转球。

## 构筑一：贪欲之刃 + 聚焦仪的金币必暴击

`src-bpd-review-spatial-combos-2026-07-16` 给出一局通关后的完整二件核心：

- **贪欲之刃**：暴击伤害加成读取当前金币。
- **聚焦仪**：其 `★` 位置上的武器触发时保证暴击。

玩家把两件正确邻接后，低自然暴击率不再是瓶颈；随后减少购买和刷新，把金币留在背包经济中，使贪欲之刃的暴伤继续增长。该局报告约 2000 gold 对应 2000% 暴伤。

- **engine**：聚焦仪把概率暴击改为位置保证；贪欲之刃读取金币。
- **state/resource**：金币、贪欲之刃当前暴伤、两件邻接、触发球频率和可购买机会。
- **payoff**：贪欲之刃是 damage owner；聚焦仪是确定性 selector / converter；金币是同时被商店和伤害争用的 state。
- **survival**：来源没有给出该局的完整防护件，故明确保留缺口；玩家必须用剩余内部空间解决 Armor / heal，而不能把暴伤当作生存。
- **spatial condition**：刀的触点在外周；聚焦仪的 `★` 必须覆盖它。聚焦仪本身可占内部，不必竞争球轨道。
- **economy / pivot**：每次刷新 / 购买都直接降低伤害 reader；未拿到保证暴击前，囤金收益不稳定，拿到后才值得停止消费。
- **counter / failure**：物理免疫阶段会完全封锁该 owner；过早囤金又会缺 Armor / heal。来源证明结构，不证明 2.4.2 仍为最佳路线。

这条组合特别适合回答“装备能否让非输出资源变成输出”：可以，但 conversion 必须有持有者、相邻条件和真实机会成本。金币没有被免费复制；它在战力和继续找件之间二选一。

## 构筑二：斯巴达战盔的四侧武器 Armor / Strength

同一实践来源给出由铁面具一路合成的传奇防具 `斯巴达战盔`：战斗开始给 Strength，并按四个侧面 `★` 是否连接武器获得额外 Strength；相邻武器触发时再给 Armor 与 Strength，并根据已损失生命提高收益。

- **engine**：四侧放置高频武器，每次触发反向喂给内部 Helmet。
- **state/resource**：四个 `★` 邻位、武器触发次数、Armor、Strength、已损生命比例、外周和内部面积。
- **payoff**：Strength 提升相邻武器伤害；具体武器仍是最终 damage owner，Helmet 拥有转换，不直接偷走所有伤害归属。
- **survival**：Armor 吸收敌方伤害；损血又提高后续增益，形成危险的 comeback feedback。
- **spatial condition**：四个武器既要邻接 Helmet，又要把自己的方向触点露到外周；Helmet 适合内部。它是明确的外周 active / 内部 reader 拼图。
- **economy / pivot**：从铁面具配方逐级投资；合成后旧效果可能改变，必须用 craft tree 预览，不能只看稀有度自动升级。
- **counter / failure**：物理免疫压制武器输出；清 positive status、封印 item 或过慢周长分别攻击 Strength、触发和频率。低血增益若没有即时 Armor / heal 也会在启动前死亡。

这是一条“防御体系如何打伤害”的不同答案：Armor 仍是 survival，Strength 仍给武器；Helmet 的价值来自把同一相邻触发同时喂防守与输出，而不是把 Armor 本身无条件按百分比转伤害。

## 构筑三：Hunter Creature 频率、重触发与每圈治疗

2.3.x 开发者讨论确认 Hunter 保证遇到 Dragon Tamer 事件；其他角色想保证 Creature 起手需使用 mod。玩家同时指出 dragon egg 孵化太慢，常在成形前已经被迫转入另一构筑。Demo 1.5.9 官方物品包提供可闭合的后期核心：

- `Beast Echo Totem`：Creature 有概率再次触发。
- `Beast Pact Seal`：统计 Creature 触发；达到十次后触发 `★` 处 Creature。
- `Heartnest of Beasts`：每圈按不同 Creature 数量回复生命。
- `Curse`：触发时移除正面状态，是外部反制示例。

- **engine**：多个 Creature 先正常触发，Echo 产生额外事件，Pact 计数到十后再触发指定 Creature。
- **state/resource**：Creature 数量 / 种类、触发计数、额外触发概率、每圈时间、egg 孵化进度、生命和正面状态。
- **payoff**：各 Creature 拥有自己的输出 / 效果；Pact 拥有计数与定向再触发；Echo 只拥有复制机会。
- **survival**：Heartnest 按独特 Creature 数量在每圈治疗，把横向品种转成 sustain。
- **spatial condition**：Pact 的 `★` 决定额外触发谁；active Creature 的触点仍需连接轨道；周长同时影响自然触发和每圈治疗。
- **economy / pivot**：Hunter 用保证事件降低入口随机，但 egg 的延迟让这条路线有真实“最晚等待点”。孵化前若另一套已稳定，继续持有蛋和 Creature support 会占空间 / 商店资源。
- **counter / failure**：Curse 移除正面状态；长孵化、封印、低触发频率或递归 guard 会降低连锁。隐藏 Boss 的异常免疫只克制其中的状态输出，不应被描述为完全克 Creature。
- **version**：核心物品来自 Demo 1.5.9，路线讨论来自正式版 2.3.x；没有补丁证明具体效果被移除，但当前数值和池概率未知。

## 高频模块：Metronome 与 Medium reader

2.3.x 开发者解释：`Medium` 是物品类型，Bow 也属于 Medium；Metronome 会按其 `★` 处 Medium 数量让自己额外触发，设计用途就是高频。玩家进一步提出把它接到“目标物品触发时施加效果”的 Omen Badge，或用 Strength 让其零基础伤害获得输出。

另一份独立长评描述同型组合：邻接物品每次触发给另一件加伤、多次触发件提高事件数、承载它们的 backpack 又按触发给金币。三者共同证明一种通用语法：

> cadence source → repeated neutral trigger → adjacent reader → damage / gold / status payoff

Metronome 不是伤害 owner，却可能是事件放大器。若战报只显示最终伤害，不显示“谁触发了谁”，玩家会把无伤物品误判为无用；若生成事件能反过来无条件触发 Metronome，又会形成递归风险。

## Ranger 配方与多轴覆盖

1.5.10 官方配方提供清晰的 specialization tree：

- `Woundbreaker Arrow = Feather Arrow + Heal Block Emblem`，施加 Heal Block。
- `Curtain Call Arrow = Poison Arrow + Woundbreaker Arrow + Balance Fang`，覆盖多个负面状态。
- `Sunpiercer Bow = Echo Bow + Split Bow`，以衰减方式触发 projectile 十次。
- `Daybreak Arrow = Frost Arrow + Explosive Arrow`，按次数施加 Burn 与 Frost。

这是 exact authored craft path，不是玩家胜率构筑。它的价值在于把多击 cadence、Heal Block、Poison、Burn、Frost 和 projectile 放在同一职业池，却仍保留不同 owner。禁止重复物品 Challenge 又会迫使横向配方，而非堆同一 Bow。

隐藏 Boss 三相提示了它的局限：physical、magic、status 会依次被完全免疫，单纯把更多状态压进一把武器仍可能整相归零。玩家的 Adventurer 通关结算同时包含物理、魔法、Poison、crit、healing、Frost 和大量 Armor，能证明混合轴确实可穿过考试，却没有完整布局，因此只作为 counter-validation，不伪装为可复现 Ranger build。

## 合成、商店、遗物与转型

- **战后供给**：战斗给 loot，随后商店允许购买和 reroll。事件 / 小游戏是独立来源；Relic Shop 在 2.0.2 后至少保证一次免费刷新。
- **合成不是无条件升级**：玩家长评指出合成后的高阶物品可能不继承父件功能，例如从 buff / debuff 工具变成清 enemy buff，导致当前 survival / damage reader 断链。2.0.5 增加 craft tree，正是为了在提交前展示路线。
- **角色池**：五名角色拥有不同起手 / 专属物品倾向；1.5.10 重排 Ranger pool，2.0.5 Training Mode 按所选角色展示物品。角色选择影响可达组件，而非传统单位 roster。
- **relic phase**：2.0.3 把默认 combat relic 与 non-combat relic 分页，2.0.5 加 tag。社区 Gold Dart 案例确认：要在战中响应 spend-gold，必须放 combat slot；放 non-combat slot会合法但不触发。
- **不可见池成本**：一名玩家在 Endless 称刷新 180 万 gold 仍未见 Thorn King Crown；开发者只确认特定 relic 概率极低并推荐 mod，没有公开 class restriction 或掉率。它不能证明精确概率，却证明长期 run 的“理论可刷”不等于实际 pivot window。

对本项目而言，最有价值的不是复制装备配方，而是：合成前预览 owner / tag / damage type / reader 是否改变；不同 supply channel 要显示池与兜底；relic 的战斗阶段、持有者和触发来源不能只靠玩家记住页面。

## 地牢、隐藏 Boss 与敌人反制

2.2.5 Guide 的隐藏路线要求在 floor 30 前收集四个 Rune，击败 floor 30 Boss 获得 Dungeon Core，再到 Ancient Ruins 一次性提交并召唤 Ancient Watcher。Boss 有连续三相：红相免疫物理、蓝相免疫魔法、绿相免疫异常；每相需要完整击败。

它是一个非常强的多轴终考，却产生真实争议：

- Thorns / interrupt 玩家到场才发现物理、Stun、interrupt 都被封，认为缺少提前转型提示。
- Poison 玩家报告 floor 30 Boss 的全清 Poison 使状态路线需要特定 relic；来源不足以确认当前所有细节，但能证明 hard counter 体验。
- 另一位玩家认为三相只在最终考试出现，可以接受；说明问题不只是“有没有免疫”，还包括是否预览、是否有替代 owner、是否保留最后采购窗口。
- 2.2.x 深层玩家又报告随机敌人可能同时取得清 buff、清 debuff、每秒回复和多击；若 trait 直到开战才组合，bag reposition 失去策略价值。

适合本项目的敌人包应把 Shield breaker、Healing Block、status cleanse、后排 access 分层教学，最终再组合。完全免疫可以用于少数明确形态，但必须在进入前显示，并保证至少两种可达转换器；否则“体系被克制”退化成“整条路线归零”。

## Endless、Calamity、疲劳与退出

普通路线 30 层后进入 Endless；玩家每隔约十层从三个 Calamity 中选一个，持续叠加难度。早期 `Life Erosion` 是每次物品触发扣 1 max HP，这会按 build 的事件频率惩罚 Metronome / Creature 等体系。2.0.4 改为每次 trigger ball 完成一圈扣 1 max HP，把惩罚从“物品数 × 触发数”归一到包体 loop。

一名 99 层玩家曾同时得到 magic immunity、按敌方 max HP 的死亡反伤和旧 Life Erosion，三项对其 build 都近似致命。它是单局，不证明整个池必死；但说明随机三选必须至少有一项对当前 build 可承担，或提供 reroll / cleanse /预览累计后果。

长战还有两道 anti-stall：达到一定 loop 后 fatigue 逐轮增加穿透 / 固定压力；退出依赖 surrender。150 层玩家报告单战 45 分钟，开发者拒绝简单 10x，因为更高速度会改变 trigger calculation，建议提高 damage 或投降。另一名使用 mod 的 1001 层玩家因双方无法结束且无投降入口求助，开发者接受建议；2.4.2 随后把 surrender 从 50 loops 提前到 20，并在 Blood Battle 永远显示。

这条链说明：模拟加速不是 anti-stalemate 的替代品。规则层要有 damage / heal / Shield / summon 的增长上限或疲劳，交互层还必须有确定可用的退出；modded run 不代表 authored balance，却能证明任何合法/自定义内容都不应把用户锁在无法结束的战斗中。

## 进度目标与后期决策密度

当前 meta / achievement 奖励主要围绕完成前 30 层和逐级 Ascension。开发者在 2.4.x 讨论明确确认 Endless 没有额外奖励，Ascension 仍逐级解锁；可用 mod debug 快速解锁，但这不改变 authored progression。

47 小时长评的核心冲突是：前 30 层容易在 20–25 层完成配方，后段只等待；真正要求持续改造的 Endless 却没有额外奖励。另有玩家报告高层 item rarity 使供给反而固定，想补 stamina 等低阶功能却找不到合适高阶件。与此同时，长流程与充足 reroll 让少数过强配方可以反复强求，敌人又常被一两种 cleanse / immunity 答案压缩。

这些不是一个问题：

- **目标错位**：奖励结束在 build 刚成形之处，后期模式缺乏外部目标。
- **供给错位**：稀有度上升可能把基础功能移出池，阻断小修正。
- **强求收敛**：刷新次数太多，使理论随机池变成固定强件路线。
- **决策耗尽**：bag 已完成后，剩余楼层只有自动执行和数值膨胀。

本项目若使用塔层，应让每个阶段继续提供横向 converter、敌人考试和有意义的替换，而不是只延长已完成构筑的播放时间。

## 战报、来源与递归

1.5.5 增加当前 run 的输出 / 状态总览、结算历史和拖拽可受益提示；2.0.5 增加 craft tree；2.0.8 增加 battle-log 搜索；2.0.9 显示 track perimeter。它们共同回答不同问题：这件能否连上、会合成什么、谁触发了谁、这圈有多长。

正式版初期仍暴露三类完整性风险：

1. **damage type / reporting**：Poison 没计入 Total Damage；Shadowkill trigger 不进 log；Burn 一度不受 Armor / physical immunity，尽管它被定义为 physical damage；player hit 信息缺失。
2. **save/load transaction**：战中 SL 恢复 gold / consumable，Rune / Core 丢失，事件资源丢失或奖励 / 属性可重复领取；同一 run event 缺稳定一次性提交点。
3. **event recursion / dynamic tag**：特定 backpack 配置 stack overflow；Beasttide chain loop；Living Doll Scroll 把 doll 改为 Creature 后，Pet Cage、Horn、Nest、Healing Angel 的 tag / trigger 归属多次失效。

高频 build 让这些问题成为设计约束：每个 trigger 需要 stable source id、parent event、phase、damage type、target、generated-event 标记和 recursion guard；tag 变化要重新评估订阅者，但不能把历史事件重复播放；save/load 要恢复快照而不是重放奖励。

## 失败、反制与生命周期

本 checkpoint 计十二个 materially distinct negative / reworked families：

1. **Thorns loop 过强**：2.0.1 将 Thornbark Treant 每圈 Thorns 3→1，并把 Wyvern 受击获取改为 30% 概率；社区仍有 Thorns 路线，故这是旋钮调整而非体系删除。
2. **空间 / trigger 不可读**：1.5.5 增 Data Overview、拖拽受益标识和 trigger-ball 教程；2.0.5 / 2.0.8 / 2.0.9 又补 craft tree、log search、perimeter display，说明两层布局需要专用解释工具。
3. **damage type / 统计归因错误**：Poison 总伤、Shadowkill log、Burn 的 physical / Armor / immunity、player hit 跨 2.0.2–2.0.3 修复；战报必须与结算共享权威类型。
4. **relic phase 归属不清**：2.0.3–2.0.5 增 combat / non-combat 页和 tag；2.3.x 玩家仍因 Gold Dart 放错页误判 bug，说明合法放置不等于有效触发。
5. **Calamity 对高频 build 过度惩罚**：Life Erosion 从每物品触发扣 max HP 改为每球一圈，归一不同 trigger density。
6. **save/load 破坏事务**：战中资源回滚、Rune / Core 丢失、事件资源丢失或重复购买 / 领取跨多补丁出现；奖励和消耗必须 idempotent。
7. **递归与 stack overflow**：特定背包触发栈溢出、Beasttide chain loop 被连续修复；高频 Medium / Creature 需要生成事件 guard。
8. **动态 tag 转换断链**：Doll→Creature 后 Pet Cage / Nest / Horn / Healing Angel 多次失效，表明运行时类型变化必须重绑 effect eligibility 和 owner。
9. **无法结束的长战**：社区 45 分钟单战和 modded 1001 层僵局；2.4.2 将 surrender 从 50 提前到 20 loops，并让 Blood Battle 常驻。
10. **隐藏 Boss hard immunity / 预警缺口**：三形态完整免物理 / 魔法 / 异常能检验多轴，却让单轴 Thorns / Poison / interrupt 玩家到场才整条归零；作为社区负面反制，不声称已修。
11. **Endless 目标 / Calamity 压力错位**：Ascension 与奖励停在 30 层，Endless 无额外奖励，却承担随机 Calamity 和数值膨胀；玩家将深层描述为有挑战但缺少回报 / 决策。
12. **刷新 / 池与少数强件收敛**：长流程多 reroll 允许反复强求少数强配方，特定 relic 又可能花极端资源仍不出现；无概率 / 统计，作为跨来源的供给负面观察。

另有 tutorial Stun Bomb、double-Boss revive softlock、floor 30 跳战、临时区重复 relic、战中计数残留、Workshop debug item 破坏 save 等具体修复。它们保留在来源，但不逐个增加计数，避免把同一布局 / 事务 / 持久化问题灌水。

## 对本项目可迁移

- **空间可以直接是节拍预算**：不必复制触发球；英雄格位、装备槽、连线长度或施法准备都能让“装更多”降低频率，从而抵抗无脑扩容。
- **active 与 reader 分区**：输出武器占危险 / 稀缺外周，Armor、转化、邻接 reader 占内部；等价到队伍就是 frontline、carry、support 与 reserve 各有不同空间成本。
- **防御转输出要读事件而非吞并职责**：Spartan Helmet 读取邻接武器触发，同时给 Armor 与 Strength；武器仍拥有伤害。Shield / Armor 可以供 survival，再由装备把事件或团队额外属性交给明确射手 owner。
- **团队总值转单核需要机会成本**：Greed Blade 读取金币时，消费就降低输出；如果本项目设计“射手读取全队额外生命 / 防御”，也应占核心装备、定义是否含 reserve、快照时点、上限和团队成员死亡后的更新。
- **高频、中量和每圈是不同 trigger**：Metronome 的事件数、Creature 的十次阈值、Heartnest 的每圈治疗与球周长互相连接。状态 / relic 必须声明读单次、次数、总量还是周期。
- **配方必须预览语义损失**：升级若改变 damage type、tag、target、trigger 或 owner，不能只用“更稀有”暗示更好。
- **Boss counter 先教学再组合**：physical immunity、magic immunity、status immunity、cleanse、item seal 各自先出现并给替代件，再进入最终多相；不要无预警整体系归零。
- **anti-stall 包含规则和退出**：疲劳 / resistance 阻止无限战斗，surrender 保证玩家永远能离开；加速只改变观看时间，不能修复无解状态。
- **事件 lineage 是内容系统前置**：trigger chain、状态、Shield、召唤、动态 tag、save/load 与战报必须共享 source / parent / phase / owner / once-token。
- **后期仍需可达的小修件**：稀有度提升不能把 stamina、cleanse、anti-heal 等基础功能全部挤出池；高阶商店要保留 bridge 或定向补件。

不可直接迁移：黄色 trigger ball、无限可移动包体、五个固定角色、30 层、精确物品 / Rune 配方、每十层 Calamity、完全免疫三相顺序、20-loop surrender、Steam Workshop JS / modloader 或任何 2.4.2 数值。

## 未决问题

- 球速、轨道每格时间、转向、多个分离 bag region 和 loop-speed cap 的当前完整公式。
- 商店 tier / rarity / character item pool / relic pool 概率、reroll 成本和 Endless 随层变化。
- 2.4.2 当前所有 Creature、Medium、Thorns、Frost、Burn 与 dynamic-tag 文本；无维护数据库可复核。
- 合成是否永久消耗父件、哪些 affix / buff 会继承，以及是否存在可靠的 downgrade / 拆解 / 锁定机制。
- 隐藏 Boss、floor 30 Boss 和随机 enemy trait 的完整 preview；玩家在提交 battle 前能看到哪些免疫 / cleanse / seal。
- fatigue 的当前起始圈、增长函数、damage type 和 meta unlock 边界。
- Workshop run 的 achievement、meta reward、history、seed 和 authored-balance 标记是否完全隔离。
- Endless 后续是否会补额外 reward、Calamity reroll / veto、基础功能补件或敌人 / summon 一致缩放。
- 公开统计、build adoption、item win rate 与难度分层数据不存在；社区收敛不能升级成客观最优解。

## Disposition

`retained`

它显著超过规则＋独立实践门槛，并闭合最具区别度的周长—频率系统、三套具名机制组合、两套补充模块、供给 / phase / hidden-Boss / Endless / lifecycle。它对“盾体系如何获得输出 owner”“元素与防御是否正交”“团队资源如何交给单核”都有高价值参照。它仍不升为 anchor，因为全部深层文字材料集中在 Steam，缺维护规则库、完整公式和统计，且版本在两个月内快速变化。
