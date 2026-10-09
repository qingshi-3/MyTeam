# 征程配置、动作与保存合同

本页是默认 Expedition 与 legacy Tower 的运行时边界。玩家规则归[随机大陆征程](../gameplay-design/expedition-progression.md)，实施和验证状态归[活动任务](../work-items/active/expedition-map-progression.md)。下文是必须满足的接口与失败语义，不表示集中验证已经完成。

## 2026-10-06 当前入口与存档身份

正常游戏恢复原 GrowthGameRoot 组合及主菜单→六选二→大陆地图→布阵战斗→战报／继续流程。主菜单“练习与预览”进入独立能力实验；GameRoot 托管预制 ContentRebuildRoot 测试场景，该场景不构造 Run 或 SaveService，退出返回原主菜单。

当前征程 profile 为 `eight_trait_expedition_v1`，SaveNamespace 为 `eight-trait-journey`，内容根为 `content/expedition/current/`。旧 `growth-journey` 活动存档保留但当前入口不读取或写回，不承诺兼容。运行合同已确认旧 profile 被拒绝时不覆盖原文件；新局保存失败保持原活动局，继续时精确恢复已保存状态。

花灵首批成长接入沿用该 profile／namespace，但技能、属性及成长资源参与内容指纹，旧内容指纹的活动局会明确拒绝且保留原文件，不做静默迁移。验证新内容应创建新征程；创建成功时仍按既有新局语义替换活动局。

以下旧 profile 与入口说明属于历史实现记录，不能当作当前默认入口或存档兼容承诺。

## 花灵固定双属性培养

`GrowthProductionMode.Cultivation=3` 追加于既有枚举，保留原值。`CompiledGrowthHero` 持有 `FixedAttackGain`、`FixedMaxHealthGain`，作者编译拒绝负数与非有限数，两个字段进入稳定内容指纹。当前仅U16开放该模式，固定4／40，百分比率为0。

分配与收益沿现有命令、冻结快照、工作副本和原子保存。生产者及受益者必须属于提交前常驻部署；一次有效非终局行动生成同来源、同生产者和同对象的攻击／生命两条已有账本项，而不是另建异步树种队列。两项属性与行动费用、库存及完成身份一次保存，失败整笔回滚；继续和重复来源不重发。未分配的初始状态不因下拉框选中首项而获得生产资格。

本批沿用既有非终局判定，治疗也不作为培养触发。培养只在行动结算遍历冻结分配，不增加逐帧统计；战中花灵治疗使用通用蓄势、目标查询、生命结算与表现事件，只有实际治疗量进入统计。森林复制体从干净模板构造，独立法力／冷却且不参与Run生产。

## 配置发布与入口选择

`CompiledGamePackage` 持有共享 `ContentRegistry`、`ProfilesById` 与 `DefaultNewRunProfileId`。每个 `CompiledRunProfile` 包含完整 `Project`、`GrowthRules`、稳定内容指纹、兼容版本及进程／奖励／身份协议版本。`Package.Project/GrowthRules` 仅是默认 profile 的只读兼容属性，继续旧局不能改默认新局指针。

历史 profile 配置记录（非当前入口）：成长入口曾一起发布 `growth_expedition_v2`（默认，`content/expedition/v2/expedition_project.tres`，招募三选一）、`growth_expedition_v1`（原地图配置，`content/expedition/expedition_project.tres`，保留一候选）与 `growth_tower_legacy_v10`（完整原成长配置，`content/growth/growth_project.tres`）。旧地图草稿与活动局仍按原 profile 继续，不改保存头、冻结库存或战斗签名；新征程使用 v2。未知指纹仍严格拒绝。该历史版本中 Alpha 保持单 profile。所有配置的内容、成长和 Boss timeline 能力依赖取并集，统一通过同一 ReadyGate，任一失败不发布部分注册表或 profile。组合根持有全部作者资源直至退出；稳定指纹来自实际配置与内容，不使用进程序号或 `GetHashCode()`。

`CompiledCampaign.Progression` 明确区分 Tower 与 Expedition。共用遭遇编译校验敌人、组成、站位、Boss 和能力；Tower 专属编译处理局部楼层范围、曲线与逐层覆盖。Expedition 使用明确的最终生命／攻击倍率及该 Encounter 自己的战场池，不能额外乘塔的楼层曲线或创建假 Tower 数据。服务按稳定 recipe ID 绑定，有序奖励组保留实际物品类别。

`RunSessionFactory.ResolveProfile` 只读一次活动存档，构造 `RunProfileHeader`，交给 `RunProfileResolver`。无 ID 的 v2–v10 先按入口命名空间选完整 legacy profile：`growth-journey` 对应成长旧塔，Alpha 原空命名空间对应 Alpha；测试 namespace 显式传映射。该步骤只路由，不证明旧结构合法。v11 必须匹配配置 ID、内容指纹与全部协议字段；未知版本、ID、指纹或读错返回诊断，保留文件，不回退默认配置。

选择成功后才构造对应全套 Run 服务。一次读取回放代理向持久化边界交付同一 DTO，避免路由和迁移之间重复读盘。原版本迁移、克隆、验证、跨版本备份及成功写回仍由持久化边界负责；原来无法证明领奖资格的旧档继续拒绝。v10 已冻结分类奖励保留原组，不重抽。未知／拒绝档不触发 Meta 初始化写入。

旧局再点新局时使用包的默认配置创建候选会话和六选二草稿；保存成功后替换整套服务与活动局，失败保留原会话。草稿成功已替换旧局，后续地图生成失败只保留本草稿。候选会话的 Meta 默认值只在内存准备，不能在新局提交前先写入。

## 状态归属与身份

schema v11 在 `ActiveRunDto` 保存 `ProfileId`、`ContentFingerprint`、四个协议字段、`RunId`、`Revision`、`NextActionSequence`、类型化 `Tower`／`Expedition`、可空 `Action` 与 `CompletedActionIds`。两种进程状态互斥；地图开局草稿的 Tower 与 Expedition 均为空，且具有 OpeningRecruitment。选队确认后必须具有完整合法地图和冻结库存。

| 状态 | 唯一拥有者 |
| --- | --- |
| 地形、迷雾、位置、阶段与剩余行动 | `ExpeditionStateDto` |
| 普通地点敌阵／战场／内容种子、服务库存、基础奖励、清除／处理标记 | `ExpeditionSiteDto` |
| 各阶段 Boss 战场与基础奖励库存 | 地图的 `StageBossFloorRuleIds`／`StageBossRewardGroups` |
| 本次服务、战斗计划、成长快照、战斗冻结与有序奖励游标 | `RunActionDto` |
| 战斗冻结证明 | `RunBattleFreezeDto` 的入场后 Gold、起战前 BattleNumber 和 InputsFingerprint |
| 动作来源 | 不可变 `RunActionSource(ActionId, Sequence, StageIndex, SiteId, LegacyFloorIndex)` |
| 成长已结算事实与永久增量来源 | Growth history 及其同一 `Source`／`ActionSource` |
| 旧塔层数、节点与待决项 | `TowerProgressionStateDto`，地图不写第二份 |

原根级 Floor／Node／offer 属性是 `JsonIgnore` 的兼容转发，旧 JSON 顶层字段由 deserialize-only `Legacy*Input` 读入 Tower；新 schema 只存对应状态。地图的 `PendingOffer` 只投影 `Action.CurrentOffer`，两者不另序列化成第二份队列；旧塔 `PendingRewardGroups` 归 Tower，`Growth.PendingNode` 只属于 legacy Tower。Growth 通过 `GetPending/SetPending` 访问地图 `Action.GrowthSnapshot`，不双写。地图事件以 ActionId／Sequence 区分；`FloorIndex` 不承担新地图的动作、成长或奖励身份。

`SamplingKey` 与机会身份分离并持久化：内容与事件结果采样仅依赖种子、生成版本及地点 ContentSeed 等稳定输入；RunId 只用于本局身份，不改变同生成输入的内容。

固定库存没有动作来源，`Source=null, FloorIndex=-1, BattleNumber=0`；激活机会保留固定库存身份，绑定当前 Action.Source、`FloorIndex=-1` 和激活时的已结算战斗计数。`RunDecisionValidation.ValidStockOffer` 与当前机会校验分开。全状态克隆／复制须覆盖 profile、Tower／地图、Action、Growth 来源和完成集合；可变 DTO 不得通过公开 UI API 暴露。

`BattleNumber` 是已接受结算次数。起战前为 b，本场冻结和战果身份引用 b；成功接受胜利、失败或超时才在同一战果事务变成 b+1。走路、查看、服务、领奖不计战；旧塔身份算法保留。计战遗物、利息与物品失效资格必须使用同一口径。

## 命令与唯一提交

`ExpeditionMapGenerator` 只生成／验证候选；`ExpeditionNavigation` 只处理可见、通行与路径；`ExpeditionMapProjection` 只公开已知信息。生成使用种子、生成版本和独立随机流，固定排序采样，有限尝试失败后保留草稿；恢复保存地图，不能重新生成代替校验。

`RunActionCoordinator` 组合地图命令，`RunChoiceEvaluator` 复用条件、支付和操作，`RunBattleResultProjector` 为两种进程共用战果投影。Tower 保留自己的节点适配器。Growth、遗物、战斗准备与模拟各自保持原职责，投影方法不得中途保存；一次业务提交最终只经 `RunProgressionPersistenceService.TryPublish` 校验、保存、复制发布。Revision 由该边界递增，失败不发布临时扣费、库存或冻结变化。

`InspectExpeditionSite`、敌情查看、服务报价和未提交取消仅持有界面预览上下文，不能分配 Action、递增序列或写盘。服务首次正式 choice 才在工作副本中创建来源、冻结原参与者与入场来源、计算效果和费用、更新库存、结算一次成长并保存。商店首次成功购买支付地点行动；后续购买与已处理商店回访仅执行商品交易，不重复成长。零行动时不得继续购买。

地图键盘由 `ExpeditionScreenController` 接收独立 InputMap 动作，通过 `StepRequested`／`InteractRequested` 交给 `GameFlowCoordinator`。协调器从当前只读位置计算四邻目标，再走原移动与地点命令；键盘目标与鼠标查看选择分离，不作为存档状态。页面在 GUI 焦点处理前消费地图按键，受阻、重复回车及锁定时不会落给首领按钮。`AppScreenHost` 通过军团 `OpenChanged` 显式管理独立的地图输入阻挡位，覆盖装备／成长页及关闭动画；隐藏地图、弹窗和文本编辑不接收地图命令。

未提交的战前属性和首次服务支付能力，在只读副本上计入同一入场遗物效果，使预览金额、临时生命和正式提交一致。战斗预览继续使用独立的 `preview-v1` 来源，不能作为正式战果；已冻结战斗直接读取提交后输入，不重复入场收益。

移动先捕获当前已知合法路径，在首次揭示新信息处截断；逐格克隆、保存并发布位置与迷雾，不根据新揭示信息延长已提交路径。中途保存失败保留已成功前缀，未走部分不发布；移动不产生行动来源或成长。

正式开战在同一工作副本中捕获原入场与成长资格、支付行动、处理入场效果与法术一次消费，再校验最终战斗输入并计算指纹。完整输入仍由已提交 Run、Action.Encounter 和唯一 GrowthSnapshot 持有，包括真实入场后金币、阵容及永久属性、装备／遗物、法术及目标、部署、敌阵／战场、种子和起战前计数。`RunBattleFreezeDto` 只保存 Gold、BattleNumber、InputsFingerprint，不复制整份 Run 或另存一份完整战斗输入 DTO。领域编辑锁保持原输入不变，输入指纹在恢复／结算校验时核对一致性；准备适配器读取这份唯一权威输入。保存后才启动模拟，启动失败继续原冻结战斗，不能重新支付或换配方。

战果核对动作来源、遭遇／组成／战场、冻结计数与输入，以及遗物 transition、永久增量和可支出金币。接受后统一结算生命、金币、战斗计数、合法永久收益和非终局成长；再发布奖励或结束该动作。已完成来源及当前阶段拒绝重复／串单。最后一点行动仍先处理当前奖励；空组按 Exhausted 前移游标，不调用主动 Skip 效果。Boss 奖励完成后才增加阶段并补足下一阶段额度一次。

## 编辑、验证与终局

编辑许可必须在命令边界执行，UI 忙碌锁只能更严格。未提交预览允许整备；已冻结 Battle 至结果提交期间禁止改变本场输入；领奖只处理当前机会及必要接收；已支付发现只接受固定候选；Boss 准备允许使用已有资源但禁止地图获取。切屏、继续或错误重试都不能解除领域冻结。

`ActiveRunConfigurationValidator` 先分 Tower／Expedition：地图不能访问 `TotalFloors`／`Regions`。地图校验拓扑、位置、迷雾、配额、地点与三阶段 Boss 固定库存、来源序号、动作阶段、奖励队列、冻结指纹和预算。预算与已结算来源对应，提前 Boss 允许明确放弃余量，免费商店回访不计费，终局无额外生产。成长永久增量仍逐条对账，不因接入地图简化。

终局先保存 `TerminalCompletionId` 与终局 Run。`Meta.HighestRegion` 取已到达阶段，采用 max；胜利按 `AppliedRunCompletionIds` 去重。无论胜败，Meta 高水位及应有胜利／解锁成功保存后才能删活动档，失败保留终局重试。游玩中的 Meta 副记录失败不反悔已成功的 Run 提交，终局删除前必须补齐。

## 界面边界

`ExpeditionScreen` 与格子使用预制场景，消费只读投影和类型化命令；迷雾后方不携带可查询的完整库存。地图显示阶段额度、Boss 预告、已知内容、固定库存及不可通行原因；颜色另有文字或形状辅助。整备、军团、装备、工坊、战斗和战报复用正式界面。地图格坐标与部署坐标、环境背景和天气展示各自独立。流程导航按已保存的继续目的地与终局事实决定，不凭胜利布尔值猜下一页。
