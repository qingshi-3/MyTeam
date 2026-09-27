# 成长闭环实现交接契约

2026-09-27，隶属[主活动任务](growth-and-composition-design.md)，为并行实现接口记录，不是独立产品提案。用户goal授权实施。主责拥有本契约及共享Battle投影/战斗/应用入口集成；所有构建与Godot串行协调。

## 本批可玩规则

- 独立GrowthGameRoot/项目配置/存档命名空间，真实RunApplication和GameFlow共用。普通部署容量初始10，开局6选2，15层，复用敌人单位、敌阵和生命规则；难度参数由独立growth地区／遭遇资源拥有，不能借成长调优改写alpha曲线。原alpha与matrix资源不改。
- 工坊生产每完成一个非终局节点发生一次；攻击或生命给一个指定已参与友军原始值4%，或改为研究+1（同一机会三选一，不同时得到）。生产者和受益者出发资格冻结，所得留在实例上；本场英雄阵亡不丢资格。后备默认无收益，新招募不追溯。
- 非战斗节点于选择节点时冻结；战斗于真正点击开始时冻结（部署页此前可调整），确认开局的自动首战也用同一入口。结算与推进同次保存，普通败战但征程继续仍产出；终局不再计可用收益。
- 每次非终局推进给通用材料1，类别材料支持替代混付，升阶cost4；每英雄最多一次，必须有明确内容配置及机制loadout。原hero ContentId不变。升阶扣料+标记+固定3名候选的PendingDiscovery同次提交；三个不同未拥有身份不足时整次拒绝不扣料，不能变成二选一。可在整备主动操作，不自动卡固定楼层。
- 首区发现池为T3及其兼容候选，后两区高阶池为明确覆盖的T4内容；配置必须足以真实给出三选一。发现独立PendingDiscovery，不覆盖普通PendingOffer；待领取时不开始下一节点/战斗，不开放放弃已支付奖励。保存后恢复同一候选，不重抽。塔图与战斗真正开始前的部署均可升阶；节点已冻结、奖励未完成或战斗已开始时不能升阶。
- 研究4点兑换一份法术，物品身份与库存归Run；首批护阵/蓄势两种开战自动效果，预选一份、一名合法目标。RunApplication先验证完整战斗配置，再在真正开始战斗的保存事务中消费并冻结；预览/取消整备不消耗。消费后清空下一场预设，重载已开始的战斗仍复用PendingNode里的已消费法术和目标，不再扣除；准备后战斗启动异常可以重试，不能免费改配方。一个来源的具体触发限制由内容规定。
- 常规产出不每次支付材料；无等级、无同名合并。第一次片段不宣称49位都做完发展重构，使用明确代表池/能力清单；高阶新单位必须提供可用能力。

## 共享接口名称

命名空间 `TowerAutobattler.Growth`。领域Agent拥有下列定义与DTO；内容Agent消费其构造签名；不互相编辑。

```csharp
enum GrowthProductionMode { Attack, Vitality, Research }
record CompiledGrowthHero(string ContentId,
 ImmutableArray<GrowthProductionMode> ProductionModes, float GrowthRate, int ResearchYield,
 string AscensionId, string AscensionName, string AscensionDescription,
 CompiledAbilityLoadout? AscendedLoadout = null, CompiledAbilityLoadout? BaseLoadout = null,
 string MaterialCategory = "");
record CompiledGrowthSpell(string StableId, string DisplayName, string Description,
 int ResearchCost, CompiledAbilityLoadout BattleLoadout);
record CompiledGrowthRules(string StableId,
 ImmutableDictionary<string,CompiledGrowthHero> Heroes,
 ImmutableArray<string> FirstDiscoveryPool, ImmutableArray<string> AdvancedDiscoveryPool,
 ImmutableDictionary<string,CompiledGrowthSpell> Spells,
 int MaterialsPerNode = 1, int AscensionCost = 4);
record GrowthCommandResult(bool Succeeded, string Message);
```

RunApplication构造器末尾新增可选 `CompiledGrowthRules? growthRules = null`。`GrowthRules`只读属性。命令：`SetGrowthAssignment(producerInstanceId,targetInstanceId,mode)`、`AscendHero(heroInstanceId)`、`ChooseGrowthHero(candidateContentId)`、`CraftGrowthSpell(spellId)`、`EquipGrowthSpell(spellId,targetHeroInstanceId)`（空spellId卸下）、`TryBeginGrowthBattle(EncounterPlan encounter)`，返回GrowthCommandResult。`CheckGrowthAction()`返回通用整备可编辑结果，细项仍在命令中校验。UI命令只经门面，失败保留原值并显示Message。

`RosterHeroInstanceDto.Growth`为非空 `HeroGrowthDto`，最少含 `AddedAttack`、`AddedMaxHealth`、`AscensionId`、`ProductionMode`、`ProductionTargetInstanceId`、`List<GrowthGainDto> History`。Added值是绝对固定增量，来源按内容原始定义，非当前面板。

`ActiveRunDto.Growth`为可空 `GrowthRunDto`，最少含 `RulesId`、`Materials`、`Dictionary<string,int> CategoryMaterials`、`Research`、`Dictionary<string,int> SpellInventory`、`EquippedSpellId`、`SpellTargetInstanceId`、`GrowthDiscoveryDto? PendingDiscovery`、`GrowthNodeSnapshotDto? PendingNode`、`LastSettledFloorIndex`、`List<GrowthSettlementDto> History`。PendingDiscovery包含 `OfferId`、`UpgradedHeroInstanceId`、`List<string> CandidateIds`。PendingNode冻结 `FloorIndex`、`BattleNumber`、`IsBattle`、参与者及分配副本、实际已消费法术/目标。必须深拷贝所有可变子项。

领域Agent可扩充DTO满足验证/账本，但以上UI使用名保持。Battle层不得引用Run DTO；主责定义不可变成长投影，由Run adapter及Roster预览共同消费。

## 集成与所有权

- 领域执行者：`src/Growth/GrowthModels.cs`及新领域service/state文件；`src/Run/**`（排除RunBattlePreparationAdapter.cs和RunBattlePreparationService.cs，由主责）；schema/clone/validator/RunApplication/节点/决策；新领域合同测试。
- 内容执行者：`content/growth/**`、`src/Growth/GrowthContentPackage.cs`、Growth规则Resource/Compiler（不得重定义上述compiled types）、新高阶独立场景及ascension loadouts。公开 `GrowthContentPackage.CreateReadyAsync(Node)`和`LoadRules(ContentRegistry)`；项目路径`res://content/growth/growth_project.tres`。
- UI执行者：新GrowthWorkbenchPanel.tscn/.cs与附属组件，ArmyOverview挂载绑定/刷新，动态单位能力展示；GameFlow/GameRoot/MainMenu/原RosterEquipmentPreview由主责集成。绑定 `Bind(RunApplication)`；公开`[Signal] ChangedEventHandler()`，命令成功后触发，主责负责刷新当前screen。
- 主责：Battle层永久值/升阶/法术投影，Run准备适配与预览、GameRoot/GrowthGameRoot/main menu/GameFlow真实入口；最后整合、反例审查和验证调度。

schema v8：v7只补确定的空状态；成长配置启用时新局初始化Growth，原alpha保持null。v7停在已选非战斗节点时补空参与者／空分配快照，完成原有选择仅结算当前固定材料一次，不补历史生产。禁止凭空重建历史产量。成长存档采用独立namespace `growth-journey`，未支持/非法文件保留并拒绝。保存失败必须完全不发布。

Battle准备：英雄的原始ContentId/实例身份/标签保持，绝对成长更新实例snapshot的MaxHealth/Damage及对应compiled attributes；BaseLoadout/AscendedLoadout只覆盖该次snapshot。装备、羁绊后续按既有顺序作用。已预设法术loadout只挂到指定实例的本场能力，不落入英雄永久loadout。完成后库存消耗保留、战斗能力清理。

战内永久收益已通过BattleResult.PermanentGains接通。Matrix PermanentAttribute仅支持本人、固定正数攻击或最大生命；来源实例／能力／operation slot每战去重，并进入回滚快照。领域层核对冻结参与者、持续英雄身份、实际基础／升阶能力和精确数值；不能把临时能力层数整体回存。首批MX01首次成功冻结获得固定16最大生命，当场生效，普通战斗结算后下一战读取；终局仅验证，不授予可复用收益。真实冻结、无冻结、保存失败重试、重复提交、后续投影均有定向验证入口。
