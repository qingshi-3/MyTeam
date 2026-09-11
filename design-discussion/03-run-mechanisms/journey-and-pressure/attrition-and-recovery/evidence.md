# R11 跨战损耗与恢复：已有证据

讨论材料，不是现行权威。进程见 [总览](../../../roadmap.md)，完整编号与取舍见 [方案讨论](proposals.md)。本轮只读已有研究及必要的当前行为，不联网、不修改实现。

后续确认：2026-09-09 用户同意 AR01，见 [R11-D01](decisions.md)。以下保留研究时的证据、实现基线与缺口；用户确认不改变原作事实，也不表示现行代码已经改为满血恢复。

## R11-Q01：一场获胜后，英雄的残血与阵亡怎样影响下一场

日期：2026-09-09。R09-D01–D05 基础方向足以衔接后续，定价／材料供给仍保留；R10 在并行任务讨论，不替它选外层形式。R11 首题只处理**胜利后的损耗落点与英雄可用性**，不同时重选战败续关、重试、特殊胜负、英雄培养回退或整局失败后的局外成长。

### 本题前提与现行基线

- [G01-D04](../../../01-global-boundaries/battlefield-experience/decisions.md)明确常规歼灭胜利，未改变现行失败条件；[核心权威](../../../../gameplay-design/tower-autobattler-core.md)报告出口明确普通胜利去奖励、最终胜利去成功，战败／超时去整局失败。后备不因未参战而在战败后自动接替续关。
- 核心权威保留跨层生命、恢复与休息后果；“本战死亡不可普通治疗复起”不等于跨战永久死亡。每战法力回到配置起始值，临时召唤不进长期名册，本题不重开这些规则。
- [G03](../../../01-global-boundaries/run-rhythm/decisions.md)允许战前不限时调整已有装备／站位／部署，前期容错、随关键收获定型；免费调整不等于免费治疗。[R04](../../roster-and-growth/reserves/decisions.md)暂参考有限备战席与战前轮换，尚未确认伤病休养系统。

**代码静态核对发现，当前并非余血完全原样保留，也不是阵亡英雄保持零生命等复活。**

| 环节 | 当前实际行为与定位 | 解释边界 |
| --- | --- | --- |
| 获胜写回 | `src/Run/RunRewardEconomyService.cs:170` 的 `ApplyBattleVictory` 保存参战存活者的剩余生命比例并设最低值；阵亡者设一个部分生命比例、清除部署槽，再向整个名册追加胜利恢复。 | 长期名册、装备和培养未因本场阵亡删除。战报死亡事实与战后恢复并存，不是战中治疗复活。 |
| 后备与重部署 | 后备不承受本场伤害写回，但参加全名册集结恢复。`src/Run/RunModels.cs:7` 的名册 DTO 没有持久死亡／伤病字段；`src/Run/RunFormationService.cs:52` 没有死亡等待限制。 | 刚阵亡者可以重新部署，无须先休息；撤出部署是当前实现操作后果，不是本题待采用的必要步骤。 |
| 下一战生命 | `src/Run/RunBattlePreparationAdapter.cs:34` 传入 Run 的生命比例，`src/Battle/BattleSimulation.cs:160` 初始化并保底；初始属性授予后再按有效生命上限调整。 | 下一战按比例继承，不是无条件满血，也不是继承绝对生命数。 |
| 休息与失败 | `src/Project/RunOfferDefaults.cs:51` 提供恢复／金币默认选项，`src/Run/RunDecisionService.cs:125` 对目标名册加生命；`src/Run/RunNodeResolutionService.cs:107` 战败／超时终局，不应用胜利恢复。 | 默认休息不以活着筛选；不能把恢复能力推成败后续关。 |

当前 `alpha_run_rules.tres` 与 `RunRulesDefinition.cs` 还保留旧 `IsHero`／起始成员与其余内容的不同恢复档位：阵亡先设 25%，再追加 12%／15%，当前结果为 37%／40%；存活者有 15%／10% 最低比例后再追加恢复，默认休息分别加 35%／45%。**这些只是旧配置，不能跟随新“所有持久角色均为英雄”的讨论一起默认为已选参数或必须保留的身份差别。** 整合时核对；本轮未运行、构建或进行体验验收。

### 实际检索范围

检索 [780 条深记录](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、106 条发现记录和 56 份深档案，并回查 [来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。JSON 按字段值搜索、数组连接为文本，不匹配字段名；四核心字段为 `rule_support / practical_support / mechanism / engine`。

- 生命／损伤词：`health|\bHP\b|heal|reviv|resurrect|wound|injur|fatigue|casual|permadeath|permanent.?death|lives|life|flame|morale|血|生命|治疗|恢复|复活|重伤|伤病|疲劳|阵亡|死亡`。
- 跨战／恢复词：`between|after.{0,30}(?:battle|fight|combat)|next.{0,20}(?:battle|fight|combat)|cross.?battle|persist|reset|carry.?over|campfire|rest|reviv|resurrect|permadeath|injur|wound|跨战|战后|下.?场|营地|休息|继承|保留|复活|永久死亡`。
- 两组交叉：深层 **218／核心 65**，发现层 **4／核心 1**；56 份档案同行交叉 **44 份／171 行**。宽候选包含战中复活、临时护盾、状态名 Wound、反拖延 Fatigue、旧存档恢复等，不把它们当作跨战伤势规则。
- 独立扩查跨战近邻、`full health / HP reset / restored / destroyed / respawn / permanent death / injury / fatigue / 战后 / 满血 / 永久死亡 / 伤病` 等，四核心命中 **31**；补查 G01-F07–F17 与发现 `e050`，捕获第一组不含生命词的 Epic Auto Towers 战毁恢复。各查询不相加，也不表示逐篇精读了全部宽候选。
- 主责定点回读 Epic Auto Towers、PMM、Skull Horde、Tiny Auto Knights、Hadean Tactics 与发现层；并行回读 TLF／GGM／Astronarch／Dwarves 和 Monster Train／Vivid Knight／TFT／Auto Chess／SAP／Tales & Tactics。现行行为由独立只读代码核对补充，不将代码视为新用户决定。

### S01：本场被摧毁，下一轮恢复并保留长期投资

**Epic Auto Towers** 的 [档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/epic-auto-towers.md)“真实循环”明确战毁塔下回合恢复，跨回合永久增益保留。`ev-eat-001-limited-board-stack-loop`，来源 `src-eat-official-ea-release-060`、`src-eat-guide-flower-power-061c`、`src-eat-guide-slime-insolent-bear-061`，材料跨 EA 0.60–0.99；具体构筑以 0.60／0.61 历史攻略为主。发现层 `e050` 同时保存该机制，但来源 `s029` 为商店页，不另算一份独立实证。

直接支持的是**单场战毁不清空长期塔投资，下一轮可恢复使用**。既有摘录没有完整逐单位血量初始化表，不能将它补成“所有幸存塔与所有模式都严格满血”的条文。AR01 的全英雄满血开战是本项目候选；原作塔阵、叠高与经济触发不一并移植。

### S02：个人伤势延续，部分自动恢复是当前项目基线

当前代码闭合“剩余生命＋最低值＋胜利全名册回复，阵亡者部分恢复并可再部署”，见上文。这支持 AR02 的可解释具体过程，**只证明目前怎样工作，不证明体验已经接受或最佳**。

已读常见自走棋资料往往强调构筑循环，未保存完整胜后生命表。TFT／Auto Chess／SAP／Tales & Tactics 的现有切片不能补作全满重置证据；Astronarch 的 Morale 材料也不能证明幸存者每战满血。AR03 的“没有普通自动回复、仅靠特定恢复”作为与当前基线不同的项目政策保留，现有语料没有闭合可比的逐英雄普通流程，不用游戏印象补证。

### S03：长期损耗由共享资源承担

**The Last Flame** 的 [档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/the-last-flame.md)记录低级不匹配英雄死亡损失 Flame，可暂作过桥位。`ev-tlf-001-run-resource-routing`：Flame 同时承担整局生命容错及部分构筑消费；`src-tlf-gameplay-levels-2025` 为 EA 起稿、2025-01-12 更新，`src-tlf-steam-indepth-2026` 自述 initial 1.0、更新至 2026-06-22，`src-tlf-official-1-0` 为 2025-01-09。

能确认伤亡影响共享后续资源；**每次扣多少、同场复活如何影响、存活者／倒地者何时回多少生命没有完整条文**。`ev-tlf-014-origin-reroll-underuse-buff`／`src-tlf-official-1-0-2`（2025-03-16）让特定营火重抽 Origin 同时恢复 Flame，是具体服务补强，不是普通胜利回血。项目只比较损耗落点，不照搬 Flame 支付刷新而改写 R09-D01。

**Monster Train** 的 `ev-monster-train-006-pyre-persistent-run-cost`：敌人漏到第四层攻击 Pyre，Pyre 反击，其损血跨战保留，通过有限效果／事件恢复。来源 `src-mt-wiki-pyre`（末修订 2024-01-04）、`src-mt-wiki-battle`（2024-01-18）、`src-mt-wiki-rings`、`src-mt-review-capacity`，2.x 标准整局。

它清楚说明最终获胜也能消耗共享整局耐久，**扣血由漏怪实际攻击造成，并非按英雄阵亡计数**。迁入本项目不能因此增加 Pyre、串行三层或特殊护送胜负；AR04 若改为伤亡消耗共享资源，是项目适配。新增资源是否只服务损耗、耗尽后果与恢复机会仍须另定，不自动采用全灭后继续。

### S04：阵亡影响后续出战资格，普通复活与永久死亡分模式

**Gladiator Guild Manager** 的 [档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/gladiator-guild-manager.md)明确普通模式可花 Mana Crystal／等待复活，Permadeath 模式承担永久阵亡。`ev-ggm-010-task-size-loss-replacement-decisions`，来源 `src-ggm-steam-campaign-v1`（2024-06-27／07-09，1.0）、`src-ggm-steam-achievements-guide`（跨 EA／1.0，更新至 2025-02-20）、`src-ggm-official-1-0`。

资料支持“需要恢复后再用”和模式限定的永久损失，缺费用、等待时间、复活后生命、普通获胜是否另有自动处理、存活者生命结算。**不能补成付费立即与免费等待任意二选一，也不能把普通模式复活直接套给 Permadeath**。旧 G01-F13 的模式并列叙述不足以证明“永久死亡模式也普遍靠水晶恢复”，本题按具体模式分开。

`ev-ggm-014-glory-mode-failure-budget`／`src-ggm-official-glory-2025`（2025-06-06）另有限定锦标赛失败／跳过次数的模式；它不是单英雄伤亡数，也不等于 Permadeath，本题不重选。

### S05：伤势或已积累内容可以承担另一种长期损失

**PMM** 的 `ev-pmm-008-training-risk-schedule` 记录训练／休息、injury／behavior 风险、食物和药品相互制约；来源 `src-pmm-official-training-rework-2024-10-30`、`src-pmm-thread-training-and-double-tank`、`src-pmm-review-kr-runlog-2026-02-19`、`src-pmm-review-de-training-2025-06-21`。玩家对管理细节是否满意存在分歧。

它能说明生命条之外的伤病负担与恢复管理，但已登记触发主要来自**训练**，不是普通战斗倒地后必得某伤病的条文。因此 AR06 的“战斗损伤留下可治疗的持续减益”是项目候选，不能伪装原作照搬。`ev-pmm-013-equipment-roster-state-ownership` 的永久死亡回滚修复只证明死亡结果有长期归属，未给本题伤病公式。

**Dwarves: Glory, Death and Loot** 的 `ev-dwarves-glory-death-loot-017-persistent-state-lineage` 明确 Arkenstone 因持有者死亡降低自身品质、损失累计状态；来源 `src-dgdl-companion-items`（主体约 v2.0.10），Rebirth 重置 Weight、退休转 Gem 来自另外的状态层，不能混为倒地复活。

Arkenstone 是特定内容风险，**不代表所有英雄死亡都会掉品阶或装备破损**。对应 AR08 只保留具体遗物／装备的可参考方向，不把它变成全员普通惩罚；不因此修改 M05、R02 的已定成长或 R09 的出售结算。

### 排除与缺口

- **战败能继续**：Astronarch Morale、Gods vs Horrors shields、Neon lives、Setr’s 前期免扣生命及 PvP 玩家生命，主要回答整场失败容错，不与“赢了但有人倒下”合并讨论。
- **战中复活队列**：`ev-skull-horde-004-death-respawn-life` 是单位按死亡顺序在可见冷却后返场，全灭扣普通角色三条命之一；正式版至 v1.020 材料。它会改变本场终止／战中死亡规则，不拿来作无须其他改变的战后恢复方案。
- **资源不一定来自伤亡**：Vivid Knight `ev-vivid-001-mana-route-jeweler-loop` 支持移动／商店折返／超库存带来的 Mana 压力，没有胜后英雄生命或倒地扣 Mana 规则；不能用路线消耗代替伤亡结算。
- **永久与临时并非只看名称**：SAP 的 `ev-sap-002-merge-food-ownership` 与档案区分准备永久／战斗通常临时，但未闭合普通生命恢复；Tiny Auto Knights `ev-tak-008-warrior-shop-self-damage-conversion` 是商店伤害换永久攻击；Hadean Tactics Permatrap 是特定跨战陷阱，都不等于全队伤势制度。Monster Train `ev-monster-train-013-death-eaten-reform-boundary` 中属性可跨死亡／Reform保留仍限本场，不能推到下一场。
- Survivor Mercs 的 Survivor Bonus 关联撤离后再次招募与局外成长，去 R14；长战 Fatigue 是反拖延，不是胜利后身体疲劳；存档修复、Rebirth 名称及战报评分都不能填补普通恢复条文。

据此形成 AR01–AR08：战后重置、保留伤势并部分恢复、只靠恢复机会、共享损耗、暂时不可用、持续伤病、永久损失及特定内容损耗。原作实证、当前实现、项目候选和模式／内容参照分别标注；研究提出时尚未采用任何新制度或创建决定，后续用户确认另见 [R11-D01](decisions.md)。
