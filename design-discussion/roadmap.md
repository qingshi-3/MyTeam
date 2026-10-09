# 讨论总览

2026-10-06 整理：本页保留当前恢复点、未解决事项和专题导航。逐轮记录完整保存在[历史总览](archive/roadmap-through-20261006.md)；具体决定归专题 `decisions.md`，执行状态归[活动任务](../work-items/README.md)。归档不改变采用范围或验收结果。

## 当前恢复点

| 工作线 | 当前状态与下一步 |
| --- | --- |
| 正常征程／独立实验室 | 原主菜单、六选二、地图、战斗及继续已恢复；能力实验和战斗实验室仍是独立入口。事实归[征程规则](../gameplay-design/expedition-progression.md)、[会话合同](../system-design/run-progression-and-sessions.md)、[实验室任务](../work-items/active/battle-lab-ui.md)。 |
| 第一版收口范围 | 用户2026-10-07已授权Goal执行[Astra完整计划](04-content-validation/artifacts/content-redesign/eight-trait-v1/implementation-plan.md) B0–B8，含§5.1常用组合、阶段养成和物品评估；现从预算和部署修复开始。18–20人及物品数量为预算，不补占位；逐批状态归[活动任务](../work-items/active/eight-trait-prototype.md#2026-10-07-首版-goal-执行)，未执行项不记为通过。 |
| 八羁绊与成长 | 现行12人／8羁绊，花灵治疗与固定4攻击／40生命定向培养已接入，见[任务](../work-items/active/eight-trait-prototype.md)和[验证](../docs/testcases/flower-cultivation.md)。[13人候选](04-content-validation/artifacts/content-redesign/eight-trait-v1/growth-slice.md)中的古树、铭记和新供给尚未接入。 |
| PvE与自然供给 | 固定人数校准工具可用；三份大型敌阵部署缺口仍在，第三Boss事务夹具不证明正式Boss可战。自然取得、三阶段通关和成长强度待验证，见[PvE记录](../docs/testcases/expedition-pve-calibration.md)。 |
| 培养／装备设计 | 保留长期核心和晚来副核，不以统一属性硬顶强制转投；装备通用能力保留，小批内容按实测迭代。见[培养决定](03-run-mechanisms/roster-and-growth/cultivation/decisions.md)与[装备决定](02-foundation-models/equipment-model/decisions.md)。装备／遗物池、额外升阶收益留空，未采用固定战力百分比分账。 |
| 种族／职业双轴 | 仍是[发散讨论](04-content-validation/content-outline.md)，未选定新分类或替换八羁绊；不恢复旧七乘七矩阵。 |
| UI与战斗表现 | UI16做到第3步普通招募，后续商店／军团手册等未完成；进度与观感归[UI任务](../work-items/active/ui-material-overhaul.md)、[表现任务](../work-items/active/combat-vfx-and-preview.md)。保留已认可的开局选人及动作样板。 |
| 立绘 | 按 v15 两处衣装标注与新增环境要求生成 v16 单张，加入胸前裁片金纹、斜地面与滑行扬尘；本轮环境要求覆盖透明底。衣片衔接、背景及扬尘密度待用户审核，出图后停止，未替换运行素材。见[参考决定](04-content-validation/decisions.md#portrait-reference-selection)和[单张记录](04-content-validation/artifacts/archer-style-comparison-20261006/sdorica-single-review/README.md)。 |

研究资产保持原路径：用户初筛66项、单单位独有条目及原始序号见[羁绊索引](04-content-validation/artifacts/content-audit-20261001/trait-shortlist.md)。它们是联合设计单位／羁绊的依据，不是全部实施清单。

## 仍需衔接的问题

- 服务前临时换入成长者的实际成本，要与持续参战、即时战力及招募机会比较，不能仅凭“占一格”宣称已平衡。
- 13人候选中的5人进阶发现池不足以支持全员三选一升阶，候选不足与升阶资格的耦合尚未解决。
- 已实施、正确性验证、实际输入、用户观感与平衡验收分别记录。任务归档不自动关闭体验或兼容问题。
- [跨专题问题表](05-integration/cross-topic-issues.md)继续承接未决依赖，包括旧对齐任务留下的I01–I04；不在总览维护另一套结论。

## 讨论路径

全局边界 → 基础模型 → 整局机制 → 小批内容检验 → 整合评审。可按依赖回看。讨论结论不自动授权实现，已有明确实施授权持续有效；正式规则见[玩法目录](../gameplay-design/README.md)与[系统目录](../system-design/README.md)。

## 议题状态

下表为稳定导航。具体采用、暂缓、撤回和参数状态以专题决定为准；基础讨论收束不等于实现或验收完成。

| 编号 | 专题入口 |
| --- | --- |
| G01 | [战场体验](01-global-boundaries/battlefield-experience/README.md) |
| G02 | [玩家权限](01-global-boundaries/player-agency/README.md) |
| G03 | [整局节奏](01-global-boundaries/run-rhythm/README.md) |
| M01 | [单位／英雄模型](02-foundation-models/unit-model/README.md) |
| M02 | [空间与遭遇模型](02-foundation-models/space-and-encounter/README.md) |
| M03 | [羁绊模型](02-foundation-models/trait-model/README.md) |
| M04 | [属性、状态与效果关系](02-foundation-models/attributes-and-statuses/README.md) |
| M05 | [装备模型](02-foundation-models/equipment-model/README.md) |
| M06 | [遗物模型](02-foundation-models/relic-model/README.md) |
| R01 | [英雄分级](03-run-mechanisms/roster-and-growth/hero-tiers/README.md) |
| R02 | [英雄培养](03-run-mechanisms/roster-and-growth/cultivation/README.md) |
| R03 | [替换与培养回收](03-run-mechanisms/roster-and-growth/replacement-recovery/README.md) |
| R04 | [后备与轮换](03-run-mechanisms/roster-and-growth/reserves/README.md) |
| R05 | [人口成长](03-run-mechanisms/roster-and-growth/population/README.md) |
| R06 | [每局内容池](03-run-mechanisms/supply-and-economy/run-content-pool/README.md) |
| R07 | [招募机制](03-run-mechanisms/supply-and-economy/recruitment/README.md) |
| R08 | [商店与奖励](03-run-mechanisms/supply-and-economy/shops-and-rewards/README.md) |
| R09 | [资源经济](03-run-mechanisms/supply-and-economy/resource-economy/README.md) |
| R10 | [路线推进](03-run-mechanisms/journey-and-pressure/route-progression/README.md) |
| R11 | [跨战损耗与恢复](03-run-mechanisms/journey-and-pressure/attrition-and-recovery/README.md) |
| R12 | [敌情预告与准备](03-run-mechanisms/journey-and-pressure/threat-preview/README.md) |
| R13 | [自选风险收益](03-run-mechanisms/journey-and-pressure/risk-and-reward/README.md) |
| R14 | [局外进度](03-run-mechanisms/meta-progression/README.md) |
| V01 | [首批内容检验](04-content-validation/first-content-set/README.md) |

## 更新记录

逐轮记录见[归档快照](archive/roadmap-through-20261006.md#更新记录)。后续替换上方受影响的当前行；执行证据写入原活动任务／测试说明，不再把每轮全文追加到总览开头。
