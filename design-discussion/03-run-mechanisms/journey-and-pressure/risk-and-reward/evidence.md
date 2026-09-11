# R13 自选风险收益：已有证据

本文件只记录本地调研的检索、事实与迁移边界，不是现行权威。方案见 [proposals.md](proposals.md)，进程见 [总览](../../../roadmap.md)。

## 本轮问题与既定边界

日期：2026-09-09。R13-Q01：**玩家通过什么方式为额外成长主动加码，加码影响当前一战还是后续进程？**

[构筑框架](../../../../gameplay-design/combat-build-framework.md)已明确要做通用自选风险收益，额外危险与所得在选择前应可理解；货币、量表、倍率、持久范围及是否独立系统未定。本轮不重问是否要做，也不因名为通用机制就预定每场都弹一次加码面板。

- [R12-D01](../threat-preview/decisions.md)：初期本场实际敌情可查、关键 Boss 提前到有机会准备；不包含试打或精确胜率预测。
- [R11-D01](../attrition-and-recovery/decisions.md)：普通胜利后全名册满血，倒地者下场可用；败／超时终局。不把跨战残血重新设为普通风险成本。
- [M05-D03](../../../02-foundation-models/equipment-model/decisions.md)及 I21：装备材料尽量通用；独立材料和高级 Boss 稳定产出某种特材是候选。拒绝可选挑战／不升级不能使主线必然无法推进，不等于保证通关全部可选高难。
- [G03-D02](../../../01-global-boundaries/run-rhythm/decisions.md)：前期低压探索，成型后留若干有挑战的关键战；无尽可后续考虑，非初期重点。R10 外层形式仍开放，不能先锁定固定楼层图或强制几场一段。
- 普通购买／刷新共用金币且刷新有限，英雄类别／通用材料混合支付、培养回退门槛与装备出售不返料保持；风险奖励不自动新增无限搜索或自由兑换权限。

相关性判据：必须直接改变玩家主动选择的额外危险、所得、接受时点或持续范围。普通递增难度、强制关卡反制、技能自身代价、赛前分数挑战、随机事件抽奖及技术事务故障按实际归属记录，不因包含 risk／reward 就展开成新制度。

## 实际检索范围

仅回查已有语料，未联网补研；原库只读：

- [深记录](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)：780 条。
- [发现记录](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)：106 条。
- [游戏档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/)：56 份；回读相关正文及 [来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。

首筛交叉匹配字段值，核心字段为 `rule_support / practical_support / mechanism / engine`，发现层按实际存在字段处理：

```text
危险／所得：risk|reward|trial|challenge|curse|corrupt|elite|wager|betting|gambl|pact|bargain|push.?your.?luck|extract|风险|收益|奖励|试炼|挑战|诅咒|腐化|精英|赌|契约|加码|撤离
主动选择：opt.?in|optional|choose|choice|select|accept|declin|take|route|path|toggle|enable|voluntar|额外|可选|选择|主动|自选|接受|拒绝|路线|开启
```

深记录全字段 225 条、核心 80 条；发现层全字段 24 条、核心 2 条。档案以 `trial|opt.?in|optional|curse|corrupt|wager|betting|gambl|push.?your.?luck|extract|风险|试炼|诅咒|腐化|加码|撤离` 命中 44 份／201 行。

独立核心扩查 `trial|pact.?shard|instability|omen|chaos|stress|sacrific|risk.?card|cursed.?trinket|primarch|retreat|cash.?out|extract|加码|试炼|混沌|不稳定|献祭|牺牲|安全撤|风险卡`：深 57 条、发现 0 条。各查询重叠，不相加；全字段中的项目建议只用于找线索，不作为原作事实。命中数不代表全部条文已闭合，更不代表穷尽游戏机制。

定点回读分为局部挑战／奖励、跨战代价／收益和现行基线三组；另回查候选中的赛前难度、训练风险、特定 Boss 反制与特殊模式，防止遗漏不同机制或误纳相邻问题。下文保留直接相关记录及排除去向。

## S01：选更危险的遭遇，胜利后取得成长

**Slay the Spire**：[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/slay-the-spire.md)，`ev-slay-the-spire-003-run-loop-economy`；来源 `src-sts-wiki-gameplay`、`src-sts-wiki-card-rewards`、`src-sts-guide-foundation`。PC 2.3.4 终点／2.2 主要平衡规则及 2023 更新指南；发现 `e081 / s055` 只有官方商店级概览。

玩家根据现有牌组、生命和补强需要决定走危险精英路线，档案明确精英遗物与其他节点机会的取舍。风险既来自当前精英，也来自所选路线放弃的商店／恢复机会。**本库没有独立的完整精英奖励表**，不补写必出项、金币数、稀有率或进入节点后可退出。迁移的是“主动挑更难的战斗争取成长”，跨战生命经营不迁入本项目。

**Magicbook AutoBattler: Contract**：`ev-mba-001-shared-offer-panel`；`src-mba-review-system-ownership-2025-03-30`、`src-mba-review-contract-economy-2025-03-29`、`src-mba-review-role-convergence-2025-03-31`、`src-mba-guide-endless-300`。2025 Contract 实践中，角色、装备和候选战斗共用三选面板；玩家会找较容易或收益更高的敌人，争夺当前成长与未来经济。具体出价／权重／奖励公式不全。**选择遭遇不一定依赖分叉地图**，但不据此导入共用刷新面板、无限刷新、锁定或原作生命容错。

对应 **RK01**。这种机制不必另有全局风险值；当前战斗选了更危险的配置即可。普通必经 Boss 的强度递增本身不构成自选风险。

## S02：同一场战斗之外再选挑战条件——现有证据只闭合入口

**Monster Train**：[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/monster-train.md)明确战前可看主要敌人与可选 Trial；相邻 `ev-monster-train-004-three-floor-battle-order` 覆盖 2.x 战序／预览，来源 `src-mt-wiki-battle`（2024-01-18 修订）、`src-mt-wiki-rings`（2021-09-19 修订）。

**本地切片没有普通 Trial 的完整惩罚、奖品及持续时间条文**。因此它只支持“战前另有可选择的挑战入口”；RK02 的“本战附加条件、胜后加奖、战后条件结束”明确是本项目方案，不写成已查实的原作整套规则。不用 Pact Shards 的跨战条文补普通 Trial，也不凭游戏熟悉程度填写数值。

## S03：危险挑战给定向成长机会，而非仅增加数量

**Vivid Knight**：`ev-vivid-004-accessory-effect-symbol-bridge`，辅助 `ev-vivid-001-mana-route-jeweler-loop`、`ev-vivid-010-gem-intent-skip-intervention-layer`、`ev-vivid-016-accessory-symbol-result-visibility`。主要 `src-vivid-guide-maze9`，wanio 的 *Amelie Witch’s Maze IX Guide (Ver1.2.3)*，2022-06-30 发布、2025-08-21 更新，仍明确为 v1.2.3；官方 `src-vivid-official-1-1-11` 支撑饰品 Symbol 结果信息。

Maze IX 的高风险 Monster Spot 获胜后，可以选择 Ring／Necklace／Earrings 部位，取得替换饰品的机会。攻略会避开打不过的 Spot，也会等关键 Gem 准备好再进入。饰品兼有具体效果和 Symbol，所以挑战价值可能在于补构筑缺口，而非总属性更多。

选择部位**不等于指定某装备或指定 Symbol**，资料也没证明挑战前预览最终随机掉落。本项目没有照搬这三个饰品槽；Gem 战中操作不迁入初期。RK03 适配为可理解的奖励方向，例如某类培养材料、某类装备候选，具体类别及额外数量未定；可与 RK01／RK02／RK08 组合，它是收益安排，不是另一种敌人增强方式。

## S04：先拿强力收益，再让累计负担强化后续敌人

**Monster Train / The Last Divinity**：`ev-monster-train-008-pact-shard-risk-budget`；`src-mt-wiki-pact-shards`（2022-06-25，rev 9932）、`src-mt-wiki-divine-temple`（2021-12-15，rev 9839）、`src-mt-official-tld-release`（2021-03-25），2.x DLC 标准 run。

玩家取得 Divine Temple 升级、Gold／Artifact 或 Unit Synthesis 等强力收益，**同时增加 Pact Shards**；累计量提高后续敌人强度，达到 100 解锁 Seraph 之后的 Ring 9。应理解为接受收益并增加风险，不是花掉已有 Shards 令风险下降。玩家需要安排接收时点，判断新增实力是否足以承担后续增强。

对应 **RK04**。与一战兑奖的区别是收益已经到手、代价延后兑现，多个选择共用一条累计负担。来源没有完整敌人强化表、风险减少／撤销流程，Threat 显示只是指示；同条记录的 Covenant 固定难度限制是另一制度，不打包迁移。项目若采用，也不能使普通培养、普通装备升级一律自动加风险。

## S05：削弱自己或接受负面内容，换取另一种价值

**Astronarch / Interstellar Seller**：`ev-astro-015-interstellar-seller-waiting-rework`；`src-astro-official-1-3-5`、`src-astro-guide-complete`（作者明示 v1.5.3）、`src-astro-guide-elements`（1.2.3–1.3.5）。v1.3.5 调整为按物品计算献祭代价，提供 ATK／SPD 或 HP／DEF 两类支付，交换事件收益。后者削弱耐久，前者削弱攻击／行动能力，不同阵容承担能力不同；本切片不补定完整奖品、人数、持续／移除细则。

**负面实例**：旧计价根据整体升级状态，诱使玩家为了一个可能出现的事件延后正常升级；开发者明确此次调整旨在减少这种等待理由。没有前后胜率／选择率统计，不声称已验证平衡成功。RK05 若有依已培养资产定价的交换，应检查是否形成同样的隐性等待激励。

**Backpack Hero**：`ev-backpack-hero-021-curse-enemy-lifecycle`、`ev-backpack-hero-034-curse-optional-owner`；`src-bh-official-enemy-rework`（2023-09-16 main）、`src-bh-official-curse-optional-reply`（2023-09-17 PDT 开发者）。重做移除敌人 Curse 攻击，开发者明确 Curse 为 entirely optional／avoidable，来自玩家选择的来源。

这只确证负面内容的接受归属。箱子／NPC／事件的逐项收益载体、接受预览、持续时间与移除成本没有闭合，**不能写成每次拿诅咒都会即时得到强奖**。4 月 testing 重做和 5 月争议／攻略是旧规则，敌人强塞诅咒不混入 9 月后机制。

对应 **RK05** 的两类内容方向：直接付出自身能力、持有带负担的内容。共同点是代价落在己方，不是所有后续敌人随公共数值一起变强；具体持续和解除应分别说明，不能为所有诅咒统一推定整局或可随时移除。R11 满血不自动返还被明确牺牲的最大生命或其他属性，但本项目尚未采用这种献祭。

## S06：为下一段过程选择挑战强度——结构证据与项目推演分开

**Siralim Ultimate**：`ev-siralim-ultimate-002-realm-progression-axes`；`src-su-official-patch-0-6`（2020-12-21 历史）、`src-su-wiki-realms`、`src-su-wiki-realm-depth`、`src-su-wiki-projects`（2026-09-04 查阅的维护规则）。档案以 2.0 stable family 为主，不把尚未证实交付的 3.0 公告变成当前规则。

玩家在城堡配置队伍、进入 realms，再决定调整 Realm Depth／Instability、构筑与解锁目标。Depth、Instability、Fortune、Favor 和 Projects 是不同轴，不是同一个万能风险钱包。资料支持可选择风险及解锁路线，修正项会影响命中、恢复、状态或行动等不同能力。

**缺口**：具体何时能改 Instability、何时锁定／重置、每档修正分布及收益率未完整登记。RK06 的“为接下来一段连续过程选强度、结束后重新决定，收益随该段获得”是明确的项目适配对照，不声称已核实原作完整段落合同。它与 RK04 的区别在于主动选择未来一段的考验，而不是每拿一个即时礼包就累积威胁；与 RK02 区别是持续覆盖多次机会。长期刷取／账号成长和回合制宏权限不迁入。

## S07：玩家布置危险，随后通过它获得成长

**Loop Hero**：仅发现 `e087 / s058`，[官方商店](https://store.steampowered.com/app/1282730/Loop_Hero/)级证据，无深档案。玩家放置敌人、建筑与地形，改变后续循环的风险并取得成长／战利品。

对应 **RK07**。玩家不只从预制危险选项中挑选，也参与决定危险的类型、位置或密度。项目适配可以是主动放入某种额外敌人／危险组合，不必照搬环形道路；具体效果是否持续、多次结算及可移除性必须另定。原库未闭合卡牌／地块组合、回报率和撤销规则，不能补成成熟的项目地图方案；R10 仍开放。

## S08：逐步取得可选特殊终点的进入资格

**Astronarch**：`ev-astro-012-key-items-primarch-route-contract`，`src-astro-official-1-6`，官方 *The Keys of Fate – Alternate Final Boss (v.1.6.0)*，2021-06-24。三种独特钥匙每局各有一次取得机会，集齐组合会把最终 Boss 换成 Primarch；组合物也可以出售取得大量 Gold。

对应 **RK08**。它改变的是终点与沿途规划，不等于每拿一把钥匙就让途中敌人增强。保留资格与出售兑现形成机会成本；出售允许窗口未完整登记，也没有特殊 Boss 胜后物品／资源奖励表。**本项目高级 Boss 给特殊升级材料，是 M05 候选的适配，不能写成 Primarch 原作奖励事实。** 当前必经层主也不等于此类可选高级 Boss。

相邻 `ev-bpd-010-hidden-boss-multiaxis-exam`／`src-bpd-guide-achievements-2-2-5`（Backpack Dungeon 2.2.5，2026-07-28—30）：四 Rune、30 层 Boss 后的 Core、在 Ancient Ruins 提交，支持额外终点入口。没有证据说明持有 Core 本身持续施加惩罚，不增列“带钥匙诅咒”的伪机制。

## S09：兑现离场，或把已有价值继续暴露在更深危险中

**Survivor Mercs**：`ev-survivor-mercs-002-operation-objective-extraction-loop`、`ev-survivor-mercs-006-priest-stim-deep-extraction-build`、`ev-survivor-mercs-016-extraction-economy-rework`。主要 `src-sm-guide-new-player`（官方，2026-04-30 更新至 1.0）、`src-sm-official-release-1-0`、`src-sm-discussion-extraction-economy`、`src-sm-official-operation-overhaul-0-9-8`、`src-sm-official-final-drill-0-15`。

Operations 串联战斗阶段和 Danger，目标提供 Mercs、XP／SP、Gear、战利品或撤离；玩家权衡继续成长与安全退出。2023 EA 的 Priest＋Stim 深探只为历史实践，不能断言当前最强。早期第一 Boss 就撤离曾成为高效刷取循环，开发者后续调整深层稀有度、地图特材及相关结算设施；不据旧分析推定当前最优撤离点。

对应 **RK09**。这是“已经带着价值，还要不要继续押”，与接受一场额外战斗后领奖不同。原作具体保留／损失范围曾多次变化，不编造全额清空。项目若采用，必须另定主动退出、通关与败局的关系，以及收益带到哪里；涉及 R11／R14 和其他模式。它直接相关但改变主流程终结方式，不能作为初期自选战斗风险的附带功能。

## 相邻命中、排除去向与不足

| 记录／来源 | 实际支持与本轮处理 |
| --- | --- |
| `ev-girls-of-the-tower-015-chaos-risk-economy`；`src-gott-official-chaos-launch`（2024-04-26）、`src-gott-official-chaos-1-0-1-5`（4 月 28 日） | Chaos 值连接难度、机会、混沌币、魔化装备、敌军招募和奴役 Boss。首发两天后降价格／中后期难度、增收益，支持“风险与购买能力须一起调”。但赛前或局内何时设值、何时能改及持续范围未知，不增列“可随时调节风险专币经济”的已证实机制。特殊内容访问权可参考 RK03／RK08，不默认新增货币。 |
| `ev-mirror-throne-005-shard-cursed-trinket-seeding`；`src-mt-demo7`（2024-08-09）、`src-mt-achievements`、`src-mt-review-party-204135472` | Demo 7 赛前 Shard Shop 选择 cursed trinket 增难换 score／局外目标，成就有携诅咒通关条件；不是局内多发金币／装备。另归赛前挑战与 R14，不能用玩家 Jewels 称呼推定与 Shards 同一资源。 |
| `ev-neon-auto-party-013-stress-rework` | 0.4.1 分层 Stress 解锁、敌方 Stress 技能和更高 unit XP；0.4.3 修解锁。赛前难度轴，经验归属／倍率缺口；不混作局内累计威胁。 |
| `ev-astro-013-c20-omen-axis-rework` | v1.5 将扭曲阵容的限制从 Corruption 难度梯度拆至可选 Omen。证明挑战可分轴，未证明额外局内成长收益。 |
| `ev-eat-009-ascension-and-malicious-reworks`；`src-eat-official-malicious-rework-077`、`src-eat-official-ascension-rework-092` | Epic Auto Towers 0.77 调整 Beggar／Cursed／Janitor 代价，0.90–0.92 调整混乱或不合理的 Ascension 并加高层奖励；不闭合另一套局内接受流程，无前后统计。作为负面代价应合理可读的版本反例，未补写 Malicious 个体规则。 |
| `ev-bpd-012-calamity-frequency-normalization`、`ev-bpd-014-endless-progression-reward-mismatch` | Backpack Dungeon Endless 三选 Calamity 叠加难度，未闭合可跳过条文，且档案有开发者明确该模式无额外奖励。不能包装成可选加码换奖；无尽属于后续模式。 |
| Tales & Tactics 档案 Road Tale／Risk Cards；`src-tnt-official-1-0-40` | 安全选项避免误入 Curse、风险卡高期望值只为档案线索；没有独立战斗作用切片。1.0.40P 预决定奖励主要为 PvP 经济，不能拼成 PvE 战斗加码。保留为可读选择／事件设计线索。 |
| Storybook Brawl 发现 `e078` | 完成任务或三合取得宝物，不证明有主动下注、额外失败成本或放弃奖励滚存。本轮未用它编造额外目标押注机制。 |
| `ev-pmm-008-training-risk-schedule` | 训练、伤病／行为／心情和日历成本的培养管理；不是接受额外战斗难度的制度。与培养／损耗题衔接，不重开初期英雄随机属性或普通伤势。 |
| `ev-vivid-009-maze9-dark-form-permanent-buff-trap`、TLF／KADO／STS／Siralim 的具体反制包 | Boss 读取已有增益／命中／行动等构筑特征，属于内容考验；没有每项普通成长都换额外奖励的独立接受动作。用于 R12／内容，不扩成“所有成长都要惩罚”。 |
| `ev-guildrun-006-red-rift-route`、SAP 难度重做、TFT／HSBG／Auto Brawl 连败及其他模式记录 | 模式规则或 PvP 生存经济不同，不作为本项目败即终局下的通用加码；难度选择、奖励节奏归相应模式／G03。 |
| 其他成长引擎、回收、服务器奖励／存档事务命中 | 只因 reward／risk／choose 等字段词命中；没有当前风险选择结构差异，分别归内容、经济或实现。未找到足够材料把战中额外目标押注／多波挑战结算另写为确证通用机制。 |

## 现行实现只读基线

本轮未构建、运行或体验验证。以下用于说明现状，不为新方案背书或授权修复：

- [TowerNodeDefinition](../../../../src/Project/TowerNodeDefinition.cs) 导出字段为 `Risk`，不是 `RiskLevel`；[编译器](../../../../src/Project/GameProjectCompiler.cs)仅校验非负，[生成器](../../../../src/Run/TowerGenerator.cs)传递，[TowerScreenController](../../../../src/UI/TowerScreenController.cs)显示“风险 N”。显示值不证明它驱动敌人或奖励倍率。
- 真实普通／精英敌阵来自 `EncounterDefinition` 配置与 `TowerGenerator`。火焰普通／精英示例基础敌数为 4／6；这是当前内容值，不是新规则。
- [RunRewardEconomyService](../../../../src/Run/RunRewardEconomyService.cs)按 `IsBoss / IsElite` 发金币，当前 [RunRulesDefinition](../../../../src/Project/RunRulesDefinition.cs)普通／精英／Boss 默认 7／12／18，不读取 `Risk`。默认普通胜利物品机会仍走统一 `CombatReward`，见 [结算入口](../../../../src/Run/RunNodeResolutionService.cs)和 [默认奖励](../../../../src/Project/RunOfferDefaults.cs)；精英说明中“更多金币”有逻辑对应，“更多战利品”未在这条默认路径体现额外数量／品质。
- Boss 层只给 Boss 选项，是必经层主，不是已经落地的自选材料 Boss。现有事件概率冒险另行执行，旧失败扣生命不成为 R13 新通用代价。

实现文件定位与接口差异留统一整合；本轮不改权威、调研原库或实现。上文形成 RK01–RK09 比较，提出时均为待讨论材料；后续用户确认范围见 [R13-D01](decisions.md)，不将整份研究转为已采用规则。

## Q01 研究提出时的静态核对

受影响六份讨论文档的 143 个本地链接有效；R13 引用的 27 个深证据 id、38 个显式来源 id 可解析，RK01–RK09 无重复编号，没有建立 R13 decisions.md。`git diff --check` 未发现空白错误；讨论区外纳入检查的 1,829 个文件清单／内容汇总与本轮写入前一致，roadmap 中 R10／M02／P01 三个并行入口及未合并决定清单原文保持。只证明引用和写入范围，不是玩法或体验验收。

## 2026-09-09 用户决定的后续说明

用户选择一般 RK01＋RK02，RK03–RK09 保留设计空间，详见 [R13-D01](decisions.md)。没有新增或改变原作证据；尤其普通 Trial 奖惩条文仍有缺口，已确认的是项目所描述的单场附加条件方向。原 RK01＋RK03 推荐不整体采用，后续初期内容与参数按决定边界展开，不根据研究示例补定。
