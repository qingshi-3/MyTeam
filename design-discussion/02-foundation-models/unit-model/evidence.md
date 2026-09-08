# M01 单位／英雄模型：已有证据

## M01-Q01：英雄自带哪些能力，哪些部分可以局内配置

检索日期：2026-09-06。原始研究之后，用户已确认 [M01-D01](decisions.md)，未合并。来源游戏中的 active 指会执行的技能动作，不等于玩家手动操作；项目初期仍按 G02-D01 自动战斗。

### 用户回应后的读法

初期采用 H03 的固定主动＋固定被动；英雄本身不提供局内配置，暂不做随机个体属性／特性。其他能力结构与配置思路保留后续参考，不纳入初期，也不采用原 P01 的可变技能数量与 H09 本体扩展建议。随机候选英雄和装备／遗物配置仍按已有决定继续；固定本体不等于取消培养或外部效果联动。下面原作过程、编号与限制完整保留，只是比较证据，不能用历史推荐或特殊内容保留意见反向扩大初期范围。

### 问题边界与基线

本题决定英雄原生能力与可配置部分的关系：招募时能识别什么能力，是否人人使用相同能力组合，局内能新增或替换哪些部分。只在改变这个答案时拆分条目；同一技能使用法力、冷却或次数，不在本题继续拆成资源制度。行动资源与多个动作的调度留待 M01 后续子问题，状态通则归 M04，装备槽与重复限制归 M05，培养成本／供给归 R02／R07／R08。不设计具名英雄或完整技能表。

- [现行核心规则](../../../gameplay-design/tower-autobattler-core.md)规定每名持续英雄拥有自己的自动技能与战斗内法力；首批模板每人一个满蓝技能。普通动作也可为治疗动作。这里是现行基线，不将首批内容数量当成最终英雄模型。
- [构筑框架](../../../gameplay-design/combat-build-framework.md)已定核心／供给者／功能位分工、少数核心深养、装备效果联动、具体内容可跨英雄供给与兑现。不能要求每名英雄自带完整输出、生存和供给闭环。
- G01-D03 已定距离接战，特殊目标机制通过英雄／装备／遗物表达；G02-D01 已定初期无玩家战中介入。G03-D02 已定随机候选开局、前期可探索体系、供给适配且保留其他方向；不能借能力模型恢复特殊主英雄或锁死后续供给。
- 全员进化、常规融合和普遍职责指定已有不采用／特殊内容边界。下面仍保留它们影响能力构成的实例，但不作为重新引入通用系统的表决。

### 检索覆盖与限制

- 沿用研究库 66 候选、56 份档案，其中 42 retained／anchor-retained、14 未达门槛、10 无档案的覆盖边界；未联网补研，未修改研究原库。
- 全部 780 条深记录按 passive、active skill/ability、skill slot、loadout、specialization、class change/equipment、技能槽／被动／天赋／转职／形态检索机制、规则、实践和引擎字段，首轮 75 条候选。
- 扩查 innate、built-in、abilities、skill tree/gem、talent、weapon/class、form、teaching、learn 与 trait 等规则和实践字段，244 条候选；其中许多只涉及某技能效果、状态变化、Boss 阶段、自动目标或供给概率，不作为能力结构详细条目。
- 对全部 56 份档案检索主动／被动／技能槽／技能树／职业／天赋／trait／active／spell gem 等，47 份命中。回读选定的 Astronarch、My Party Is Grinding、Dungeon 100、Gladiator Guild Manager、Magicbook AutoBattler、Siralim Ultimate、Slotbound、Just King 等的能力、配置与版本段，并核对 The Last Flame 的变体、少女塔的移植记录。没有逐篇全文重读全部档案。
- 发现层全部 106 条按同类字段筛出 19 条；回读 synthesis 的动作／配置相关段及 Auto-Arcana、Auto RiskRisk 的深档案入口。部分发现层 JSON 中文字段已有乱码，不猜译成规则；可用线索返回可读档案。Auto-Arcana 仅作 HL01；Auto RiskRisk 当前主要确证装备承载 set，未闭合个体动作结构，转 M03／M05，不把它强称为换装备就换完整技能。
- 检索到 Dwarves 的职业、装备与自动战斗记录，但本轮可读段不足以单独确认“换武器就换整套技能”的完整规则；这一比较采用 Magicbook 的直接证据，不补写外推。命中数与列项数不代表穷尽或质量。

### H01–H12：不同的能力构成与配置方式

**H01 固定招牌施放技能，配置围绕它发挥作用。** Auto Chess 的历史 Water Spirit 路线把其原生施放技能作为主要输出，装备提供法力、冷却、保护等帮助，玩家围绕这个可识别的技能配阵容，而不是给它从通用技能库换一套技能。收益是招募价值直观、敌我角色容易识别；代价是变化主要由装备、队友和使用条件提供。该例不证明所有单位都只有一个效果或没有被动；材料为 2020 攻略，随后 Shaman 重做，不能当当前强度。证据：`ev-auto-chess-007-build-divinity-water-shaman`。

**H02 由事件触发或持续特性承担招牌能力，不必另有蓄满后施放的技能。** Super Auto Pets 的 Horse／Turkey 在友军召唤时强化新单位，Sheep／Deer 等在倒下后生成替代身体；这些单位的价值在明确的事件与效果关系上，不能只按一次大招衡量。收益是辅助、经济或触发型角色也可简洁地成立；代价是离开配套事件就可能没有作用，需要辨认效果由谁触发、谁受益。其战斗有自动攻击次序，不能移植为我们的实时移动规则。2026 Turtle Pack 指南与 2022 独立资料只互证持续存在的结构，不互证当前数值。证据：`ev-sap-006-horse-turkey-fly-summon-build`。

**H03 固有主动＋固有被动，各有独立作用和培养价值。** Astronarch 每名英雄有一个 active 和一个 passive，两侧各可升级两次。攻略让 Druid、Pyromancer 等核心在两侧投入，也让某些 Cleric／Assassin 在基础能力已能履职时少投入。主动给可观察的施放，固有被动给持续规则或触发特色；其优点是单位容易解释，又能有两条配装／培养关注点。代价是若强迫每人填满同样模板，容易制造只为占位的被动或让功能位承担不需要的成长负担。这是对迁移的取舍分析；原作规则与供给估计来自 1.2.x–1.5.3 攻略，精确 Orbs 数不照搬。证据：`ev-astro-002-ability-orb-core-functional-economy`、`ev-astro-008-attack-active-cadence-separation`。

**H04 固有能力分阶段开放，招募时并非完整形态。** Magicbook AutoBattler: Contract 用共享经验手动培养角色，在固定等级解锁角色自己的被动。玩家除了看当前面板，也看以后能开放什么，再决定投入；这些是角色自有能力，换武器不等于把它们换掉。收益是同一英雄能逐步显露新用途；代价是晚到英雄或少投入功能位可能长期拿不到关键能力，必须和 G03 的前期容错／可选换核相容。当前精确里程碑和回收规则未公开，不据此设我们的等级表。证据：`ev-mba-005-character-experience-passive-investment`；所属层区分另见 `ev-mba-002-weapon-class-contract-ownership`。

**H05 一个职业提供多个可配置施放技能。** My Party Is Grinding 的五个固定职业各自可开放最多四个 active slot，从本职业技能中组合；例如 Knight 的恢复与盾、Mage 的伤害与削防／定身分属不同技能。玩家给同一英雄配多个动作，可以同时承担多个功能；代价是阅读、配装、自动选技能与表现解释都更复杂。多个技能不等于同时执行，也不等于它们都能有效覆盖全队。固定五职业、长期挂机成长不是项目默认结构；资料为 2026-09-03 的早期上线期数据库与攻略，个别技能目标／持续时间仍有官方修复。槽数来自 [该档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/my-party-is-grinding.md)的真实循环段，证据：`ev-mpig-002-fixed-party-unlock-economy`、`ev-mpig-003-class-skill-owner-map`、`ev-mpig-004-cold-ranged-dual-core`。

**H06 本体特性与可装配法术分开，额外动作不必绑定原生身份。** Siralim Ultimate 分开管理生物特性、Artifact、Spell Gem 与 Relic；法术宝石提供生物可使用的法术，原有特性与其他佩戴层仍分别存在。玩家选本体时看它的规则特色，再为它配置法术；好处是不用换掉整个单位就能补动作，代价是同一外观可能装有不同施法内容，必须能辨认实际技能与使用权限。原作是按时间线行动、玩家选择动作或调用宏的 RPG，并非原生全自动自走棋；这里只比较配置层，不采用手动／宏权限，也不声称任何生物都能无限装备所有法术。资料为 2.0-era 维护页，完整槽数／资格例外未在本轮展开。证据：`ev-siralim-ultimate-005-equipment-ownership-layers`、`ev-siralim-ultimate-006-macro-manual-authority`，生物特性层参考 `ev-siralim-ultimate-004-fusion-inheritance`。

**H07 大部分打法来自可自由组合的技能卡，英雄主要提供基础差异。** Dungeon 100 的技能卡可跨职业组合，Quick Mode 在 2023 大改后开局提供九槽；玩家曾在 Druid／Rogue 上复用 Bear／Shield 结构。与 H06 的本体特色＋额外法术相比，它把更多玩法构成放在技能配置本身，换卡可能重建整套运作方式。收益是组合自由度高；代价是原生英雄差异可能被配置淹没，学习与排序负担大。玩家报告从左到右的施放次序会让短施放技能占住循环、右侧技能轮不到，说明更多槽不等于更多有效能力。具体调度不是完整现行官方规格，不能照搬九槽、所有卡等概率或简单排序算法。证据：`ev-d100-002-cross-class-nine-card-assembly`、`ev-d100-003-left-to-right-cast-starvation`。

**H08 招募对象自带随机个体特性，同职业也可能不同。** Gladiator Guild Manager 的旧随机 Trait 招募使角色作为“职业＋随机特性组合”出现；好特性提高某个个体的价值，玩家可比较同职业的不同候选。优点是重复招募仍有新意，代价是额外增加阅读和搜索要求：官方明确描述过花数千 Gold 刷新仍凑不出想要组合的挫败。这里的随机特性与 G03 的随机候选英雄是两层，后者已确认，不自动批准前者。旧随机组合问题随后引出 1.0 的可教学方案，不能将旧版限制写成现行事实。证据：`ev-ggm-012-trait-and-item-choice-reworks`。

**H09 保留原生动作，通过可教学的特性改变具体用法。** Gladiator Guild Manager 1.0 用 Mana Crystal 与 Blueprint 教学／升级 Trait，较高层级的槽也可容纳较低层级 Trait；例如给会嘲讽的单位加 AOE Taunt，再与读取 AOE 的装备配合。它不像 H07 重装整套技能，而是在已有行为上增加或改写效果。收益是未抽到完美个体仍能补出需要的能力，同职业可有不同配置；代价是新增特性选择与资源竞争，通用最强特性还可能使配置趋同。官方重做说明动机与规则，未公布前后多样性统计；层级、数量和价格不是项目方案。证据：`ev-ggm-007-trait-teaching-recruitment-replacement`、`ev-ggm-012-trait-and-item-choice-reworks`；AOE Taunt 的具体联动见 [1.0 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/gladiator-guild-manager.md)的属性／装备／Trait 段。

**H10 换武器就换职业倾向、基础动作和技能包，角色被动另行保留。** Magicbook AutoBattler: Contract 将这几层拆开：武器决定职业倾向、攻击方式与技能，角色等级解锁个人被动，另一些装备提供 Contract 点。玩家无需换人就能换动作组合，再寻找适合该角色被动的武器。收益是角色投入可以配合多种用途；代价是看英雄本身不足以判断它怎么战斗，配装会承担更多身份定义。装备效果联动已获确认，但这种全面换动作的权限还没有获确认；本题比较权限边界，M05 再定槽与重复限制。资料跨 Contract 首发至 2025-07，完整武器表／阈值未公开。证据：`ev-mba-002-weapon-class-contract-ownership`、`ev-mba-005-character-experience-passive-investment`。

**H11 特定英雄能切换或改写自己的原生能力组合。** Just King 的历史 Druid 随前／侧／后位置变为 Turtle、Wolf、治疗形态；Slotbound 的晋升分支会改变自动技能、成长与战斗职责；TLF 的 Reborn 则可能用固定变体替换原技能的一部分。它们分别是位置条件、成长选择和特殊变体，不能混成同一种触发。共同之处是改变本体能力，而非只增加属性。收益是同一角色有有意义的变化；代价是变化后旧配装／用途可能失效、识别更难。**本项目已将形态／进化限定为特殊英雄或内容，不重新提通用进化树或全员职责选择**；这几例只解释模型需要容纳的已有特殊范围。证据：`ev-jk-002-position-forms-and-items`（0.3.0 历史形态）、`ev-slot-005-promotion-rarity-role`（0.2.7–0.3.4 Demo，分支表不完整）、`ev-tlf-010-reborn-static-combo-piece`（1.0 变体，后续调过平衡）。

**H12 某个英雄的能力能通过明确机会转交给另一个英雄。** 《少女塔》佣兵 Training 允许每个角色移植一次被动、之后不能更改，并在升星后保留；Slotbound 吸收牺牲单位以获得来源相关的 Imprint；Siralim 融合则同时保留两方特性、其他字段按父本分别继承。三者有不同的次数、供体代价和保留规则，不能都简称“继承全部技能”。对模型的共同意义是固有能力可能有明确的外部副本来源；收益是跨英雄组合，代价是英雄身份／机会成本可能被削弱，来源与叠加必须清楚。**本项目已有特殊融合／继承与低频蒸馏的边界，不重选常规合成系统**；上述具体方式也未被采用。证据：`ev-girls-of-the-tower-010-mercenary-transplant-economy`（2024 佣兵上线及后续实践）、`ev-slot-004-absorb-imprint-ownership`（Demo、转移池不完整）、`ev-siralim-ultimate-004-fusion-inheritance`（0.12 生命周期与 2.0-era 数据）。

### HL01：六槽法术序列的低证据线索

[Auto-Arcana](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-arcana.md)的官方产品资料描述六槽从左到右施法、法术融合与 runestone。它补充 H07 的序列配置方向，但研究时主 App／Demo 均未发行、没有独立实践；循环、跳过、并发与融合的完整规则未实证。不另立“六槽比九槽更好”的方案，也不把数千种法术的宣传数量当作已验证的组合深度。

### 归属与后续

- H01–H03 比较固有能力构成；H04 比较何时开放固有能力；H05–H07 比较动作配置范围；H08／H09 比较随机特性与可教学修正；H10 比较武器和本体的分工；H11／H12 只承接已有特殊内容范围。没有把它们全部提交为必做系统。
- 玩家战中指令、技能卡属于玩家而非英雄的 Hadean／Neon／少女塔牌库，以及宏操作，回 G02 的后续候选，不在初期增加玩家介入。
- 攻击／施法周期、共享资源、多个技能冲突与禁用条件留 M01 后续问题；属性／效果语义归 M04，羁绊身份归 M03，装备／遗物接口归 M05／M06。不得据本题决定回复率、施法优先级、冷却或状态公式。
- I01 仍是 Lab 主英雄字段的技术核对，I14 是已确认开局目标待整合；它们不需要用户在本题再次选择英雄身份，不因展开 M01 就顺手修改代码。

引用均可在 [深证据库](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)按 ev id 查到，关联来源见 [深来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。这里只证明已有语料支持哪些结构和限制，未验证本项目的体验或实现可行性。
