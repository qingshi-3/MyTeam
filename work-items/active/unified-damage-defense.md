# 统一伤害与防御结算

## 目标与授权

2026-09-21，用户在确认 M04-D03 后明确要求“那把现有的都修复掉呢？”，授权在当前 main 工作区同步实现、内容、界面、相关权威和必要验证。保留已有未提交工作，不提交／发布，不改真实玩家存档。

伤害由具体公式产生，攻击、法强、生命、层数等只是显式输入；普通伤害统一读取防御，真实伤害仅绕过防御，继续公共修饰、护盾、归属、死亡和有效伤害统计。保留防御曲线与技能系数，不自动补偿原魔法技能。

## 实施与验收

- 共同入口、普通／真实伤害类别、属性与内容编译、已编写技能资源一致；移除魔抗属性，保留其后属性的序列化数值身份。
- 清理现行技能／单位／词条／面板的双防说明；攻击、法强使用已选图标和配色，伤害为结果。
- 行为回归覆盖公式输入、统一防御、真实伤害、护盾和有效统计，以及实际贯穿箭／光束和状态事件筛选。
- 低并发编译、相关引擎检查、真实 UI 输入和渲染；平衡与主观观感留给用户试玩。

## 当前状态

已实现并通过针对性验证，平衡与主观观感待用户试玩。恢复入口：共同结算 `src/Battle/BattleSimulation.cs`，类型 `src/Effects/EffectContracts.cs`，属性 `src/Attributes/AttributeContracts.cs`，词条 `content/ui/combat_keywords.tres`。

设计依据：`design-discussion/02-foundation-models/attributes-and-statuses/decisions.md` 的 M04-D03。历史图标与调研资产保留。

## 已完成与证据

- `EffectDamageType` 只有 Normal=0、True=2；迁移15处原Magical=1资源。旧数值1明确拒绝，不误变成真实伤害。MagicResistance退出定义／快照／校验／指纹／面板，属性数值5留空，后续序列化身份保持。
- 普攻、技能、持续效果、反伤及共同伤害事件使用普通／真实类别。原曲线 `raw × 100 / (100 + defense × 7)`、正普通伤害最低1及现有技能系数保持。归属、共同修饰、盾量、生命、死亡和有效统计继续沿原公共链执行。
- 已编写技能、单位、组件说明和自动生成说明统一；词条为攻击、法强、伤害和真实伤害，防御不再声称只减物理。法强沿用紫色彗星、伤害沿用用户P2红色剑刃交击，图标及关联数值同色；原始图标资产与授权保留。
- `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` 成功，0警告／0错误。
- 引擎行为检查通过：`AbilityFoundationContractSmoke`（固定／攻击／法强／混合公式，公共修饰，统一防御，真实伤害，无盾／部分盾／全吸收，过量伤害及击杀归属）；`ContentPrimitiveContractSmoke`（输入快照／编译身份与退休配置拒绝）；`ContentCarrierGrantContractSmoke`（状态来源和普通／真实筛选）；`EnemyPiercingContractSmoke`（正式箭／光束、不同防御、时序／衰减／回滚）；`FirstContentSystemsContractSmoke`（现有状态／冰冻概率／召唤链）；`SurgeConductorContractSmoke`、`HealingReaderContractSmoke`（相关实际技能目标与触发）。日志在 `.godot/unified-defense/<入口名>.log`。
- 图形引擎 `CurrentUiInputCapture --tooltip-icons` 与 `UnitInformationInputSmoke` 通过真实鼠标、键盘、悬停、拖放及重绑。已人工查看词条图例、长技能和共享详情截图：法强／伤害／真实伤害与数值配色正确，无旧双防类别或裁切。日志 `.godot/unified-defense/ui-input.log`、`shared-ui-input.log`；图片 `.godot/ui-review/after-keyword-icons-palette.png`、`after-keyword-long-skill.png`、`.godot/unit-information/shared-views.png`。
- 扫描当前 `src/`、`content/`、`scenes/` 无旧物理／魔法结算代码或玩家侧双防文案。只保留图标历史文件名及无关空间语义的physical名称；历史讨论与调研不改写成新规则。

未运行整局自动推进或广泛旧测试；未操作真实玩家存档、提交、推送或发布。原法术技能面对防御现在会减伤，这是本次规则变更的预期结果，未用加系数抵消。用户可在实验室用同一光束技能对比低／高防御目标，观察实际伤害及强度是否需要后续调优。
