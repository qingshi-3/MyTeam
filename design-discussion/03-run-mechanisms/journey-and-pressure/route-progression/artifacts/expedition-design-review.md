# 探索实施设计独立审查

2026-10-02。审查者：空上下文子 Agent。审查对象：[expedition-implementation-design.md](expedition-implementation-design.md)，本轮读取的版本为 189 行。结论：**需要修订后复审；可以沿既定探索方向继续补齐设计，尚不能把本文作为接口已经闭合的实施合同。**

本报告只做静态设计审查。独立读取了根 AGENTS、讨论入口／R10-Q11／决定／跨题记录、正式玩法与系统契约、相关活动任务，以及下列当前源码和作者资源；没有把主设计的“已核对”当成证据。采用 `godot-code-review` 技能中适用的场景职责、显式通信、资源寿命与状态边界检查。没有构建、启动游戏、执行运行测试、操作玩家存档或验证观感。本轮只写本报告。

## P1：实施前必须关闭

### ER01：旧档配置选择尚不能由当前存档提供，且兼容配置的容器与解析时序缺失

- **设计证据：** §3.2 第 64 行让 Campaign 只持有一个类型化进程变体；§6 第 154 行又要求同一个成长包保留 `LegacyTowerProfile`，并“以保存的配置 ID”选择正确配置。没有定义 profile 注册表、其完整组成，或无配置 ID 的旧档如何绑定它。
- **当前证据：** `src/Run/RunModels.cs:44–94` 的 v10 `ActiveRunDto` 只有 Seed、楼层、战斗号及成长状态，没有 Campaign／Profile ID 或内容版本；`src/Growth/GrowthModels.cs:79` 的 RulesId 仅是成长规则 ID。`src/Composition/GamePackagePublisher.cs:12–18,66,110–128` 当前发布一个 Project 和一份 GrowthRules。`src/Run/RunApplication.cs:29–59` 在读档前已选定单一 project 并创建 persistence；`RunProgressionPersistenceService.cs:23–36,55–105,180–183` 用它迁移、校验旧档。不是已有一个运行时 profile resolver 可以直接复用。
- **触发与影响：** 默认 growth Campaign 切成 Expedition 后，继续一个没有配置 ID 的 v10 塔局，迁移还没获得旧规则便要做塔楼层、旧待战／奖励、人口和成长校验。仅保留旧塔表不够；旧 RunRules、旧供给／默认 offer 语义、成长来源身份和引用集合也必须一致。按“缺 ID 用默认配置”处理会拒绝合法旧档或用新规则解释它；临时在各服务判断 Tower 则会形成多处配置权威。
- **建议：** 明确发布包中的不可变配置注册表与默认新局配置 ID。每个条目绑定进程定义、RunRules、供给／offer 配方、GrowthRules 和兼容协议版本，内容目录可共用。先只读保存头并以“保存命名空间＋支持的 schema”将无 ID 旧档明确映射到固定 legacy ID，之后再选中规则、迁移并建立会话服务；新 schema 显式保存配置身份。定义未知 ID／版本拒绝路径和旧 profile 的完整引用收集。只读取 header 不发布或保存。该设计须说明继续旧局后再点新局时如何切回默认配置，而不是让构造器选一次后永远使用同一个 profile。

## P2：必须在相应模块实施前补齐

### ER02：生成失败保留哪一份“旧存档”与现有新局替换时点不一致

- **设计证据：** §2.1 第 23–24 行在选队成功时生成地图，生成失败不能丢弃旧存档；Q11“主流程与地图状态”另写先验证完整地图再发布新局，失败不替换旧局。§1 同时要求沿用现有开局。
- **当前证据：** `RunRewardEconomyService.cs:80–90` 在创建六选二草稿时就保存到唯一 active_run；`RunApplication.cs:86–98` 成功后立即替换 ActiveRun，确认选队是后续事务。`SaveService.cs:28–33,75–101` 只有一个活动槽，同 schema 普通替换也没有自动备份上一局。`work-items/active/recruitment-opening-and-tiers.md` 的 2026-09-29 修订明确新征程成功保存草稿即替换原征程。
- **触发与影响：** 正在探索／旧塔局 → 新征程保存草稿 → 选队后地图连续生成失败。此时原局早已被草稿覆盖，不能兑现“保留上一局直到地图成功”的解释。若临时补保存第二份，又会改变继续应恢复哪份状态。
- **建议：** 明确一个合同并统一 Q11 与实施设计：沿用当前新局即替换，则地图失败保留的是本次候选与已选草稿，并明确上一局在新局草稿提交时结束；若确实承诺地图成功才替换原局，需设计独立草稿槽／可恢复原局及其提交协议。不能让实现者根据“旧档”二字自行选择。

### ER03：PendingRunAction 与现有奖励／成长 pending 字段的唯一存储归属未定

- **设计证据：** §4.1 第 86 行让 `PendingRunAction` 持有成长参与者、战斗冻结／消费和待领奖队列；第 88 行继续改造 `PendingRunOffer`；§5.3 要迁移 growth pending/history；未明确现有字段是被移动、保留为唯一权威，还是仅保留只读投影。§4.2 也没有明确现有 `RunNodeResolutionService` 与新 `RunActionCoordinator` 的替换关系。
- **当前证据：** `RunModels.cs:64–75` 现有 PendingNode、PendingOffer、PendingRewardGroups；`GrowthModels.cs:86–107` 现有 PendingDiscovery、PendingNode、参与者与已消费法术；`GrowthRunService.cs:176–194,205–247,333–348` 按这些字段恢复、结算、校验。`RunNodeResolutionService.cs:106–214` 已拥有战果验收、遗物转移、成长、完整奖励队列和唯一保存。`RunApplication.cs:216–217` 的整备锁还直接依赖 Growth.PendingNode，而不是页面状态。
- **触发与影响：** 在战斗已保存但启动失败、商店第一次交易完成、阶段 Boss 领奖中断等边界，若新 action 保存一份队列／法术状态，旧字段再保存一份，恢复与校验会分别读不同事实；若只删旧字段但不改命令锁，则可在冻结后改阵容／装备。笼统规定“唯一提交”不能解决两份状态的序列化所有权。
- **建议：** 补一张逐字段迁移表，明确 battle freeze、生产快照、当前 offer／剩余队列、付费发现和已完成事务账本的唯一存储路径、旧字段只读迁移入口，以及正式命令如何统一查询编辑权限。声明 `RunNodeResolutionService` 是塔适配器、被协调器接管，还是共有战果投影器；共有战果／遗物／成长只能保留一条提交路径。建议新 Action 拥有来源与生命周期，成长子快照与奖励队列只存一份；具体放在哪层可由主设计选择。

### ER04：同类多配方缺少编译索引合同，不能直接复用当前按 Kind 唯一的 offer 表

- **设计证据：** §3.1 第 53、56、60 行要求地点独立配方，普通战装备、精英装备＋遗物、阶段 Boss 遗物；§3.2 第 64–66 行让 Campaign 继续拥有选择配方，Site 引用 `RunOfferDefinition`。没有规定新 recipe 注册表如何与当前 RunOffers 区分，或 RewardRecipeId 如何解析到多个奖励组。
- **当前证据：** `CompiledProjectModels.cs:96` 的 RunOffers 是 `ImmutableDictionary<RunOfferKind, CompiledRunOffer>`；`GameProjectCompiler.RunOffers.cs:11–15,64–67` 拒绝同 Kind 第二份配方。`RunDecisionService.cs:27–41` 所有战斗都读取唯一 CombatReward，再按类别拆分。`RunOfferDefinition.cs:9–19` 没有奖励组列表或 RewardCategory 作者字段；目前分类是 `RunOfferDefaults.ItemRewardGroups` 从单个实际物品操作推导。
- **触发与影响：** 将普通装备配方和 Boss 遗物配方都加入现有 Campaign.RunOffers，会直接因重复 CombatReward 拒绝发布；若只保留一份配方并在运行时按地点删类别，则会把内容规则再塞进协调器，违背本稿目标。
- **建议：** 明确以 StableId 编译可重用的 recipe 注册表或在 Site／Stage 编译结果中直接持有不可变 recipe 引用；旧 Tower 的 Kind→recipe 绑定保留为适配层。定义 RewardRecipeId 指向一个组还是一个有序组集合、组 ID 和类别由谁编译、空池／已拥有内容过滤后如何处理。列出首版普通／精英／Boss／三类非战斗配方的实际绑定及候选数，而不是只写“引用当前值”。共享执行器继续消费通用 operation。

### ER05：BattleNumber 的开始／完成口径与固定库存的时效资格需要一并写明

- **设计证据：** §4.1 第 82 行称 BattleNumber 为“实际提交的战斗次数”，§5.3 第 142 行要求保留计战利息；§2.1 第 23 行又冻结所有库存。尚未说明计数在开战还是结算时递增，以及固定内容在后续失去取得资格的显示／领取规则。
- **当前证据：** `RunBattlePreparationAdapter.cs:82–91` 以结算前 BattleNumber 生成战斗种子及身份；胜利在 `RunRewardEconomyService.cs:257`、败战在 `RunNodeResolutionService.cs:163` 各递增一次。`RunRelicRules.cs:97` 用递增后的 `<= Limit` 结利息；`RunItemSupplyPolicy.cs:15–27` 用递增后的 `< Limit` 排除再无未来效果的遗物。真实供给含 `item_rl08_piggy`，其 `content/relics/definitions/item_rl08_piggy.tres:8–11` 为 Limit=8。
- **触发与影响：** 第 8 场边界若把“提交次数”提前增一却沿用旧结算递增，会少算利息、改变战斗种子并使保存校验不一致。开局固定的商店或战利品候选若含存钱罐，玩家第 8 场后才处理，该物品目前已不合法；“固定库存”不能等同“永久可购买”，也不能靠回访重抽修补。
- **建议：** 明确 completed battle count 与本次 battle ordinal 的关系，例如 Begin 记录 ordinal=count+1，所有胜败正式结算仅一次写回 count；冻结／恢复不递增，普通新尝试与阶段 Boss 各计一场。为固定库存规定原始内容保持、资格在只读投影与提交时重验；失效项显示具体原因并禁用或过滤，但不补抽。奖励池全部失效时必须明确空组消退／跳过的规则，避免为了空组产生额外跳过遗物收益。覆盖第 7／8／9 场和存读、失败重试。

### ER06：首版内容映射仍缺可校验的职责覆盖与倍率含义，容易出现空字段或两次缩放

- **设计证据：** §2.2 第 32 行要求“基础职责覆盖标签”验证；§3.1 第 56 行同时引用带倍率的旧 EncounterDefinition 和新 HealthMultiplier／DamageMultiplier，并称编译结果保存最终乘积；§3.2 第 66 行还有地域 PressureBand。没有定义职责词表／覆盖范围、标签落在何种作者资源、PressureBand 的消费方，或新倍率字段表示局部因子还是最终值。
- **当前证据：** `UnitDefinition.cs:7,19,58` 有展示职责枚举与通用 Tags，但现有供给仅按 tier 采样；`growth_recruitment_supply.tres:5–28` 没有能力覆盖约束。旧遭遇本身已有基础倍率与每层曲线，例如 `encounter_gloam_combat.tres:41–44` 为 2.1875／1.61 与两条曲线；`RunBattlePreparationService.cs:72–80` 会再次相乘。`GameProjectCompiler.Encounters.cs:19–35,68–74` 当前还按 FloorsPerRegion 校验曲线与组成范围，不能直接作为无楼层地图的完整遭遇校验器。
- **触发与影响：** 实施者可能用 UnitRole=Support 当作真实治疗／控制供给保证，或加入实际上无人消费的 coverage／PressureBand 字段。中区配方若填入已乘曲线的最终倍率，准备服务再乘旧 encounter 基础倍率，就改变敌军强度；若为了绕过旧编译器填虚构五层，又恢复了本稿明确禁止的隐式塔依赖。
- **建议：** 将地图遭遇的编译输出定义为唯一最终倍率，明确旧值只在作者配置迁移时取样，运行准备不再相乘旧曲线。补选定组成、允许战场、最终倍率、基础与组成收益的首版绑定表。把共用敌阵结构校验与 Tower 专属楼层范围／曲线覆盖校验分离。职责覆盖应列出有限的真实能力标签、对应可供给身份和最低可达／合法机会条件；若首版不准备做这项验证，明确降为后续平衡检查，不能保留成一个声称已满足的生成硬约束。PressureBand 只留有明确消费规则的字段。

## 建议与已核实的可复用部分

- **S01：以现存资源为准，不依赖旧活动文档数量。** 当前招募 tier 数为 4／6／7／9，共 26 个；FirstDiscoveryPool 为 7 个，AdvancedDiscoveryPool 为 9 个。当前 `growth_item_pool.tres` 有 39 个身份，其中 6 个 equipment、33 个 item，旧活动记录中的“6 装备＋9 遗物”已不足以代表当前供给。三地域普通／精英／Boss 共 9 个 EncounterDefinition 均存在；Boss 分别显式引用 rampart、shadow、brood 组成及 timeline。上述是静态资源存在性，不是地图平衡证明。第一阶段之后全局按 AdvancedDiscoveryPool 供给，与可提前进入高地域的地图招募会相互排除；设计已规定不足三个拒绝升阶，应把这类自然耗尽列入供给诊断。
- **S02：保持本稿已经正确划定的边界。** 预制 ExpeditionScreen／格子组件、导航服务只管通行、天气只展示、正式 BattlePreparationAssembler 复用、先保存再反馈、非终局成长与最终 Boss 不发可用成长、地图位置不进入战斗模拟，这些方向与当前权威一致。`scenes/app/GrowthGameRoot.tscn` 已是预制页面组合；`AppScreenHost.cs:63–103` 显式登记页面与常驻栏，新增地图应一起进入这些集合。UI 的地图详情／商店浏览应读显式 Site 投影，不为了复用只读显示而先写 Run.PendingOffer。以上建议不代表新 UI 的节点路径、信号清理或输入行为已经验证。

## 审查覆盖与未知项

| 范围 | 本轮已覆盖 | 未作结论 |
|---|---|---|
| 配置／发布 | Campaign 与 compiled 类型、项目编译、loadout 引用收集、Growth authored package、统一发布／根寿命 | 新地图作者资源、legacy registry 尚不存在；没有运行 ReadyGate |
| 楼层解绑 | 对 src 中 FloorIndex／TowerGenerator／BattleNumber 做全量检索，并检查主要 Run、Growth、Battle adapter、Relic、UI 调用 | 没有以新 schema 实跑迁移；旧测试夹具仍须随正式接口适配 |
| 事务／恢复 | 开局、商店、选择、战果、成长、法术、Meta 两文件完成协议、保存及克隆入口 | 没有保存故障注入、重入或序列化验证 |
| 实际供给 | 当前招募分阶、发现池、物品池、遭遇组成、三 Boss timeline 引用与现有奖励路径 | 地图 39 地点的实际生成质量、自然构筑可行性与 6/6/6 平衡未知 |
| 场景／只读边界 | GrowthGameRoot、ScreenHost／Router、现有商店控制器、RunView 正式只读契约 | 地图新节点树、动态观感、960×540 实际输入和表现资源尚未制作 |

本轮没有发现必须另造地图战斗模拟、另复制英雄／Boss 场景或增加新英雄才能开始实施的证据。已存在的通用 operation 足以表达所列金币／恢复／招募／物品选项；它目前不包含通用材料或研究 operation，因此这些数值应继续由明确的战斗／成长收益投影负责，若要移入统一配方语言，须明确增加并校验相应 operation，不能把未支持字段写进 `.tres` 期待自动生效。

## 设计通过条件

1. ER01 关闭：补完整 profile 注册与旧档无 ID 映射、解析先后顺序和会话切换；发布门禁同时覆盖默认与 legacy 引用。
2. ER02–ER04 关闭：新局替换承诺统一；待处理／消费／奖励每项只有一份可写状态；多种同类别配方具有可编译的明确索引与执行接口。
3. ER05–ER06 关闭：计战时点、固定库存失效、空奖励组、遭遇最终倍率、职责覆盖和首版实际绑定可由合同测试明确判断，不依靠临时 if 分支。
4. 修订后的静态设计可再次审查。之后的运行通过条件仍是主设计 §8 的定向契约、真实内容与隔离输入、动态录制及用户体验验收；本报告通过也不替代这些验证。

本轮未修改主设计、正式规则、路线图或活动任务；主 Agent 在整合修订时负责同步讨论恢复入口。

## 第一轮复审（2026-10-02）

复审读取主设计 267 行修订稿，并重新核对 Q11 的新局替换说明、三 Boss／普通战场池、开战金币输入及遗物计算源码。**ER01–ER06 在设计层关闭；新增 ER07、ER08 两项 P2 尚需小范围修订。** 下表的“关闭”仅指初审要求已经形成明确合同，不表示新类型／资源／迁移已实现或测试通过。初审正文与原行号保留作为历史证据。

| 初审项 | 复审状态 | 修订依据与判断 |
|---|---|---|
| ER01 | 设计层关闭 | §3.4 第 112–118 行给出 ProfilesById／默认新局 ID、完整 legacy profile、依赖联合发布、无 ID 的命名空间＋schema 映射、header-first、session factory，以及旧局转新局时整份会话保存成功后替换。新保存指纹与临时 PublicationVersion 已分开，Meta 初始化不抢先写入。 |
| ER02 | 设计层关闭 | §2.1 第 24 行与 Q11 主流程文字一致：新草稿保存即替换上一局；选队／地图失败保留的是本草稿。没有引入第二活动槽，符合当前有效的新局语义。 |
| ER03 | 设计层关闭 | §4.1 第 134–150 行列明 Action、GrowthSnapshot、Rewards+Cursor、PendingDiscovery 与完成收据的唯一存储位置，拒绝新旧双份；§4.2 第 173–175 行明确共同 BattleResultProjector 无保存、Coordinator 唯一提交，以及覆盖所有正式命令的 RunEditPolicy。 |
| ER04 | 设计层关闭 | §3.3 第 94–108 行明确 StableId 配方表与旧 Tower Kind 绑定、有序奖励组、类别反查、各场景具体金币／候选／服务内容；组成材料和研究继续从正式成长投影接入，不假称现有 operation 已支持。 |
| ER05 | 设计层关闭 | §5.4 第 216–220 行保留起战前计数作为冻结输入、正式结算才 b+1；资格重验、固定库存禁用原因、不补抽与 Exhausted 非 Skip 已明确，第 7／8／9 场进入检查计划。 |
| ER06 | 设计层关闭 | §3.1 第 57–74 行列具体最终倍率与组成额外收益来源，运行只乘一次；§3.2 第 90 行拆共用内容／Tower placement 校验；第 33 行将职责保证明确降为内容诊断并删除 PressureBand。此次新列战场来源另有 ER07，不重开已经补齐的倍率与标签问题。 |

### ER07（P2）：三 Boss 不应被统一改用普通地域战场池

- **设计证据：** 修订稿 §3.1 第 59 行把允许战场统一写成 `pool_{ember,gloam,crown}_floor_rules.tres`，下面的同一表又包括三 Boss；没有 Boss 例外。
- **当前证据：** 三个 `content/growth/encounters/encounter_*_boss.tres:5,25` 均引用各自 `pool_*_boss_rule.tres`。这些专用池分别只含 `rule_narrow_lanes`、`rule_hazard_pulse`、`rule_boss_ward`；普通 ember 池还含 clear，普通 gloam 池还含 healing_beacon，普通 crown 池完全不含 boss_ward。`RunBattlePreparationService.cs:41–46` 从最终 FloorRuleId 实例化真实战场，所以这里不只是显示用元数据。
- **触发与影响：** 按新文字制作 Boss 配方时，第一／第二 Boss 可随机换到原来未使用的战场，最终 Boss 必然丢掉 boss_ward。于是“复用完整 Boss 遭遇”变成未声明的规则变化，冻结、预览虽然能一致，仍不是设计声称的原内容。
- **建议：** 每个地图配方以所引用 `EncounterDefinition.FloorRulePool` 的编译成员作为允许集；在首版表明确普通／精英与 Boss 的差异。编译时检查显式允许集属于该来源池；若有意覆盖战场，需作为独立作者配方明确说明，不能由统一路径模板产生。

### ER08（P2）：最终战斗冻结需要包含入场后金币，并在入场与法术投影之后校验

- **设计证据：** 修订稿 §4.1 第 142 行规定 BattlePreparation 只消费 Action.BattleFreeze；§5.1 第 183 行却先“冻结初始来源与预战阵容”，之后才应用入场遗物与法术消费，未明确最终 BattleFreeze 在哪个时点生成，也未列开战持币。§5.3 对 NodeGoldUntilPurchase 继续保留有效动作入场产金。
- **当前证据：** `RunBattlePreparationAdapter.cs:82–104` 把 `run.Gold` 作为正式 BattlePreparationRequest 输入；`RunRelicRules.cs:32–39` 入场可增加金币；`RelicDefinitionCompiler.cs:96–101` 将 GoldBattleHealth 编译为读取 `relic_gold` 的生命修饰；当前供给的 RL02 入场 +2 金币，RL09 每金币 +2 最大生命。`RunRelicRules.cs:91–98` 的计战利息还使用开战持币，`RunNodeResolutionService.cs:119` 用可用金币约束 GoldSpent。
- **触发与影响：** 同时持有 RL02、RL09 时，若完整快照在入场前冻结，模拟少得到这次 2 金币对应的生命，恢复或结算读取入场后的 Run 又有另一份金额；如果为修补而让 battle adapter 回读 Run.Gold，则违反“只消费冻结输入”，新边界失去完整性。战前法术也需要在最终校验中使用本次实际消费记录，而不是只验证消费前预设。
- **建议：** 区分“入场前来源／参与资格捕获”和“最终 BattleFreeze”。在工作副本完成入场效果、行动支付、法术消费投影后，构建包含入场后持币、最终 roster／装备／遗物输入、法术消费记录引用、来源及内容指纹的唯一 BattleFreeze，再经同一准备／assembler 路径完成完整校验并保存。战斗启动、同次恢复、利息基数与 GoldSpent 上限均消费这份冻结事实；保存失败不发布副作用。Boss 开战遵循同一顺序。初步合法性校验可以提前，但不能代替最终输入校验。

### 新增边界核对与非阻断提醒

- 第 31 行的免费连通分量检查具体可执行：未清敌格从通行图移除后计算起点分量，同时检查向外的早期普通敌人边界；它与局部可绕敌人、地图外阶段 Boss 没有逻辑冲突。最多 5 个服务、55%–70% 通行率与局部守关能否在 64 次尝试内稳定共同满足，仍待生成器测试，不能从文字证明。
- 第 226 行以已到达阶段记录 Meta 高水位，与现有高水位含义相符。实施时需注意 `RunProgressionPersistenceService.cs:150–173` 目前败局直接删除 active_run，只有胜利强制写 Meta；若新高水位同步失败后马上败局，删除前没有补同步便无法再靠“恢复时重试”找回。应在终局清理中明确高水位同步的持久化责任，或如实将其保留为 best-effort；这是收尾可靠性检查点，本轮不据此另建游戏进程阻断项。

关闭 ER07／ER08 后可完成本轮设计审查。复审仍未构建、运行或修改游戏资源，未知与运行验收责任沿初审记录保留。

## 最终定点复核（2026-10-02）

本次读取主设计 275 行版本，定点核对 §3.1、§4.1、§5.1 与 §6 的修订及相互引用。**ER01–ER08 均在设计层关闭；本轮已审范围没有剩余的 P1／P2 设计阻断。** 初审及第一轮复审保留，前文“尚需修订”描述对应当时版本，当前状态以本节为准。

| 复核项 | 当前状态 | 关闭依据 |
|---|---|---|
| ER07 | 设计层关闭 | §3.1 第 59 行改为从每个实际引用的 EncounterDefinition.FloorRulePool 编译允许战场，分别明列普通／精英地域池与三 Boss 专属池；narrow_lanes／hazard_pulse／boss_ward 与现有 Boss 资源一致。Boss 战场由生成输入冻结，预览与开战共用完整站位校验，消除了套用普通池改变 Boss 规则的问题。 |
| ER08 | 设计层关闭 | §4.1 第 142 行与 §5.1 第 183–187 行一致地区分入场资格捕获和最终 BattleFreeze；工作副本依次处理行动、入场遗物、成长／法术消费后，才构造完整冻结输入、完成最终校验并同次保存。入场后 Gold 明确成为 RL09、战内消费、GoldSpent 上限和利息的同一基准；装备／遗物、法术消费记录引用、敌阵、战场、种子、计数及指纹均纳入冻结。预览只读运行同序投影，Boss 走相同提交路径，失败与恢复不重新支付。 |
| Meta 终局高水位提醒 | 设计层关闭 | §6 第 230–232 行区分游玩中的副记录同步与终局清理：终局 Run 先持久化完成身份和已到达阶段，删除前必须确认／成功保存 Meta 高水位；失败保留终局，重试采用 max 与 completion id 去重。败局也受该规则约束，堵住当前旧路径直接删除活动档后无从补同步的窗口。 |

ER01–ER06 沿第一轮复审维持关闭；上述补充没有恢复双份可写快照、普通池替代 Boss 池或新局保存语义冲突。设计现可作为后续实施与定向验证的合同。实现时仍须用真实序列化、故障注入与内容配置验证这些承诺，特别是入场遗物／金币冻结、Boss 原战场、终局 Meta 保存失败的恢复；这些是主设计 §8 的运行通过条件，并非本次已验证结果。

本轮只追加本报告，未修改游戏代码／资源、主设计或其他文档，未构建、运行游戏或执行交互测试。地图生成可满足性、数值平衡与用户体验仍沿初审“未知项”保留，静态审查通过不代表其通过。
