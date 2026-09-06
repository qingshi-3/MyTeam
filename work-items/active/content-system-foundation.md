# 初版内容承载能力补齐

Status: Implemented — Compilation Passed; Behavioral Execution And Player Acceptance Pending

## 目标与授权

2026-09-06 用户确认：补齐 1 通用结算链路与 2.1 配置内容的通用模型/接入链路，然后再填初版英雄技能、羁绊、装备、遗物。本任务不扩充正式内容，不修改受保护调研与 Web 模型。独立于具体内容 id，不追求无限表达的脚本引擎。

## 约束与验证

- 只在 main 开发，保留现有大量未提交工作及外部远程攻击改动。无提交、发布、真实存档修改。
- 沿用 authored Resource → immutable compiled spec → scope-owned runtime。BattleSimulation 保持战斗 mutation authority。
- 主责串行编译，低并发；不启动自动战斗/整局试玩。新增关键行为回归可以编写；未运行必须明确记录。实际玩家体验待用户验收。
- 不自动引入升星、合成、经济曲线等未定玩法。支持的组合可执行，不支持的组合在发布时拒绝。

## 工作顺序与文件归属

1. 技能与战斗接入（hero_mana_content）：src/Abilities/**、src/Battle/BattleSimulation.cs、新 Battle 局部类、BattleCombatEventPipeline/CompatibilityAdapter；接通 Triggered/Passive、自动目标与满蓝解耦、无效目标结果、效果来源。主责不同时修改这些文件。
2. 征程决策与持久化（formal_equipment）：src/Run/**、src/Project/**、GameFlowCoordinator、Reward/Shop/Recruit/Event/Rest UI；一次性待决选择与原子提交、失败恢复、终局提交、初始布局配置契约。独立新增对应测试，不改战斗。
3. 内容载体（combat_feedback）：src/Traits/**、src/Equipment/**（RunEquipmentService 除外）、src/Relics/**、src/Statuses/**；可选成员/全队目标、可撤销状态授予、装备事件关系、共享接入与说明。不改 BattleSimulation，共享注入向技能负责人请求。
4. 主责：src/Effects/**、src/Attributes/**、ContentValidator/发布整合、共享模型、权威/任务文档、串行编译与缺口检查。数值表达提供有限 typed 组合；目标/条件扩展复用 Effect 内核，不做字符串 DSL。

## 完成标准

- 已声明的能力入口和上下文均有消费者，或有清晰的发布拒绝；不允许静默零值/永不触发。
- 通用选择、条件、数值和效果原语可被内容载体复用；授予/撤销、死亡、档位切换、结束/回滚有明确归属。
- 节点效果/资格消费/推进一次提交，待领奖可恢复，终局 Meta 保存失败不删除 Run。
- 最小独立配置覆盖上述边界，不依赖任何具体英雄/装备 id。编译通过与实际执行/体验验收分别报告。

## 已实现与整合结果

- 共享原语：有界加/乘/min/max属性表达式，快照/实时根捕获；效果步骤复用公式；阵营/标签/空间/排序/数量查询及生命比例/标签条件。来源、目标、上下文、计数和羁绊依赖明确校验，缺失上下文不再静默回零。法强/魔抗可由UnitDefinition编写并进入快照与指纹。
- 技能：AttackHit/OwnerDefeated正式事件桥，独立有界延后执行和事务；Passive常驻Status授予/撤销；自动目标策略与ManaFull解耦。来源与物理/魔法/真实伤害类别贯穿共同结算。死亡目标返回明确Skipped。
- 载体：Trait全队/成员选择与同档成员刷新；Equipment常驻授予及受击事件关系；Relic常驻授予复用Status事件/周期模型。GrantId隔离、原子替换与撤销，按来源/伤害类别筛选状态响应。状态说明由实际编译规则生成。
- 初始化：先投影全部静态/常驻属性，再一次性初始化输入生命比例与法力，最后执行授予玩法效果；避免伤害/治疗被归一覆盖。局中召唤不再重写全队生命。
- 额度与身份：Effect额度键包含具体Origin实例；独立grant不串用次数。普通UsageLimit/RateLimited正常跳过。状态精确指纹覆盖嵌套公式/效果/过滤/溢出，全部载体与战术命令引用完整指纹，不依赖舍入说明。
- Run：Campaign可配置Offer/Choice/条件/成本/成功及失败操作；默认节点适配同一模型。ResolveOffer在副本中完成全部效果与资格消费/推进，一次保存后发布；v6 PendingOffer恢复战后/节点待决选择。终局Run/Meta凭证幂等，Meta失败不删Run。旧非战斗待决格式存在资格歧义时拒绝迁移保留原件。新建征程布局不再受旧六格数组限制。
- 正式呈现：继续使用既有人物动画肖像/物品卡，支持混合选项；RunDecisionText生成完整条件/代价/概率/失败说明，悬停/键盘聚焦使用现有详情区域。旧ExperienceSlice显式消费正式奖励机会以维持隔离，不生成双份奖励。
- 未改变正式内容、调研语料、Web模型、main分支或真实存档；没有提交、推送、发布或启动试玩。

## 验证证据

- 第一轮统一低并发构建发现3个类型错误（测试委托适配、测试Spawns容器、ImmutableDictionary.Builder插入API），主责已修复。
- 最终 `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` 成功：0警告/0错误（2026-09-06）。没有无理由重复完整构建。
- 已编写 `ContentPrimitiveContractSmoke`、`AbilityFoundationContractSmoke`、`ContentCarrierGrantContractSmoke`、`RunDecisionContractSmoke` 独立入口，并修正旧AbilityStatusContractSmoke的被动样例；全部随项目编译，**未执行**。
- 主责/代理静态检查覆盖来源丢失、授予初始化、同源额度隔离、普通限流、精确指纹、保存失败、重复结算和迁移歧义。不以代码审查替代真实行为证据。
- 没有启动Godot、自动推进战斗、进行截图/键鼠验收或读写真实玩家存档；UI/战斗/持久化故障用例仍待在隔离环境执行。不能宣称上述回归或玩家体验已通过。

## 支持边界与下一步

- 不新增升星/合成、成长曲线、经济策略或全新正式内容；没有把所有未来玩法塞进通用模型。
- 常驻grant只支持永久/BySource/单层/不可驱散/死亡移除/无溢出状态；普通临时可叠层状态仍走原ApplyStatus管线。Relic受益者选择保留已有四类目标模型；其他新目标属于后续显式原语扩展。
- 条件为绑定级AND，不是任意布尔脚本或每个查询结果的独立条件；范围为中心距离。TeamCount是文档指定战斗口径，不是Run后备名册总数；未新增跨队属性聚合表达式。
- 恢复入口：先读 `system-design/content-composition-foundation.md` 和 `docs/testcases/content-system-foundation.md`。在用户允许执行时进行上述针对性隔离契约检查；实际操作与平衡验收由用户负责。
- 初版内容设计可以基于这套明确能力开始，但大量填充前仍需完成关键运行验收，再做一小批英雄/羁绊/装备/遗物配套内容。不要把本状态误读为所有想象中的机制或体验已完成。
