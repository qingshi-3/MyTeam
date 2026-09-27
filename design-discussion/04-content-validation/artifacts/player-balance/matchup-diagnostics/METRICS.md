# 采集口径

- 正式敌阵由 `TowerGenerator` 与 `RunBattlePreparationService` 生成，保留模板、地形、地区及层内倍率；实验室扩展只表示玩家英雄不在当前17人池，仍对同一正式敌阵。
- `DamageTaken` 是模拟器原始总承伤（生命损失与护盾吸收）；`HealthDamageTaken = DamageTaken - ShieldAbsorbed`。
- 有效治疗取 `HealingResolved.EffectiveValue`；溢出为 `AppliedValue - EffectiveValue`。护盾授予取 `ShieldResolved.EffectiveValue`，护盾吸收归承受者。
- 技能次数与首次释放取 `ManaSkillResolved`。怒拳次数取表现事件 `line_release`，仅用于该已知动作。
- `AbilityCasts` 只统计自动法力技能；`line_release` 只补记怒拳动作。它们不代表所有被动、触发能力或 `AbilityResolved` 动作总数。
- hard control 是 `DisableActions` 状态或公开 `DisabledTicks`；slow 是含移动或攻速减益的 harmful 状态。每次 `Step()` 后读取公开单位状态，按目标逐 tick 计数，因此同目标重叠不重复；tick 内施加并在同一 tick 消失的状态可能无法观察。来源侧 `HardControlGrantedTicks` 是应用时已受抗性修正的授予时长之和，不称为实际覆盖。
- 召唤父级由 `UnitSummoned` 的 source/target 建链，并追溯根召唤者；召唤物自身贡献保留独立单位行。非事件召唤若存在则无法补出父级。
- 对带公开 `SummonerRuntimeId` 的临时单位，采集时会核对事件父级；不一致会令诊断失败。
- 英雄记录实际征募品阶和 `UnitDefinition.RecruitCost`，后者当前未接征募经济，因此标为 `AuthoredRecruitCost`，实际 `RecruitmentPrice` 保持 null；装备与遗物读取正式 `ItemDefinition.Price`。
- 五固定种子是配置筛查，不代表自然胜率；失败与超时均保留。
- `winter-formed` 与 `winter-same-heroes-missing` 保持同英雄、同职责站位，只替换等价装备，是严格装备/羁绊对照。棘毒缺治疗与召唤缺核心均会把远程单位换成近战单位，并由同一职责站位算法重排；召唤替代队还把法力装备留给无蓝怒拳。两者是复合替补压力样本，不能解释成单一英雄或单一机制因果。

## 夹具修正

首次 `raw.json` 的默认近战落位在第4名近战时重复 `(2,4)`，因此 `lab-shield-chain` 与 `lab-crit-attack` 的末区各15场少一名英雄。这30场保留作无效夹具历史，不得用于强度判断；修正结果在 `raw-corrected.json`，用相同 case/floor/kind/seed 键替换。其余290场未触发重复格。修正后采集会在准备前、准备后及结算时分别核对完整永久英雄身份集合。
