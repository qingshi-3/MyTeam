# R10 路线推进：证据与比较边界

## R10-Q01：保持战斗不变，重新理解“塔”和“爬”

日期：2026-09-08。本轮为既有资料检索与开放设计分析；没有联网补研、原型、试玩或正式玩法合并。

用户指出已有特色偏向内部的玩法品质，认可核心规则／内容联动需要做，但希望寻找一眼可辨的外层差异。明确要求战斗环节当前不变，拆解爬塔的底层逻辑，开放结合其他游戏形式。此为问题与讨论范围，不是选定某种新进程。

### 已读范围与搜索方法

- 现行玩法的 Run Structure，G03-D01／D02、R02-D02／D03，R10–R13 入口及跨题记录；当前战斗系统不因讨论而改动，初期无介入规划也不在本题重选。
- 读取历史十种完整进程的任务记录及现存完整数据：`web/game-mechanics-atlas/app/data/runProgression.ts`。它们是项目探索方案，不是外部游戏事实，也不是已采纳规则；本轮不修改该 Web 或图像资产。
- 对全部 780 条深证据的 domain、mechanism、rule_support、practical_support 筛查 explorat、expedition、excavat、calendar、overworld、territor、route、floor reveal、contract、weekly、mission deadline、map choice、探索／挖掘／领地／日历／航行／路线，得到 111 条宽候选。很多只是战内机制或技术措辞命中，不能算有效进程机制。
- 对全部 56 份既有档案做相近关键词扫描，定点回读 Vivid Knight、Skull Horde、Gladiator Guild Manager、Private Military Manager、ShapeHero Factory、Magicbook AutoBattler 的相关段落；对全部 106 条发现记录补查，回读 Loop Hero／Thronefall 线索。没有逐篇重读全部档案，也没有把发现层提升为深证据。
- 本轮重点解释进程对象、玩家操作、机会分布、推进压力、战斗反馈和终局；战内主动操作、生产临时兵替代英雄名册、普通技能联动等只作迁移边界，不扩大成本题设计。

### 可支持拆解的具体原作片段

以下是局部机制依据，不代表原作整套游戏可直接移植。深证据 id 位于 [证据库](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)，source_ids 对应 [来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。

| 证据 | 原作过程及本题启发 | 限制与迁移边界 |
| --- | --- | --- |
| `ev-vivid-001-mana-route-jeweler-loop`，Vivid Knight | 地图移动消耗 Mana，战斗获得 Keene 与单位；玩家比较继续探索、避开难以处理的敌点和何时集中购买。说明推进可以是空间探索与预算分配。 | v1.1.10–v1.2.3 指南期；精确公式未核定。本项目 G03 已不采用回访旧商店集中采购，不能借探索方案偷偷恢复它。 |
| `ev-ggm-001-timeline-guild-resource-loop`，Gladiator Guild Manager | 建筑开放招募／培养服务，商店、任务和锦标赛随日历到来，玩家围绕月度比赛安排投入。说明阶段可以由时间组织，而非楼层。 | 1.0 与后续可选 Glory 分开。项目只可讨论行动推进的日历，不引入现实准备倒计时或默认采用工资／复活制度。 |
| `ev-pmm-007-weekly-goal-and-mission-deadline`，Private Military Manager | 周目标及行动日期让训练、休息、采购等竞争有限准备日程，错过目标影响 Trust。 | 2024 Playtest–2025 EA，任务间日程不同。只比较机会期限，不采用原作战中权限、伤病或 Trust 失败规则。 |
| `ev-mba-001-shared-offer-panel`，Magicbook AutoBattler: Contract | 英雄、装备和候选战斗共用三选面板，金币刷新同时改变三类机会，可锁住买不起的选项。说明“下一战”也可以是供给选择中的一种商品，而不需要路线地图。 | Contract 发布及 2025 更新期资料；权重和现行价格缺口保留。“Contract”名称不证明它有下文委托网络，不能混淆。 |
| `ev-shf-008-route-research-shop-rng-economy`，ShapeHero Factory | 路线、配方、材料、研究和地图扩张共同决定生产链能否成立；商店提供有限纠偏。说明外层可有一个持续搭建的成长对象。 | v1.0 及同期实践，精确现行概率未知。其产线直接决定自动部署频率，不能原样搬入本项目固定英雄名册；O12 只推演战前成长供给。 |
| `ev-skull-horde-003-threat-resource-race`，Skull Horde | 留在区域获取资源也会推进 Peril 的敌方压力；Explorer 改为按楼层增长。说明资源获取与压力可以共享不同的推进尺度。 | 两模式不可混写。项目不采用其实时移动／技能权限，外层压力若使用应按明确行动推进。 |
| `ev-astro-012-key-items-primarch-route-contract`，Astronarch | 三个每局各一次机会的关键物品集齐后改变最终 Boss；合成物也可出售。说明终局可由中途承诺决定，不必只靠到达第几层。 | v1.6.0 更新拥有规则证据，实践指南更早，只支持原有准备循环，不证明新 Boss 强度／实战表现。 |

### 仅为发现线索

`e087／e088` 的 Loop Hero 指向放置地图内容、在循环中生成危险与成长；`e093` 的 Thronefall 指向建设与防御交替。现有记录主要是发现层，未完成本轮规则与策略补研。O05／O12 为本项目推演，不能宣称完整照搬、已核实当前原作细则或证明新颖性。

### 历史项目方案与本轮展开的关系

旧十方案包括领土、日程、委托、移动据点、多线防卫、赛季、基地、竞争队伍、挖掘、规则选秀。本轮 [O01–O14](proposals.md) 重新按可见对象与实际操作展开；O03、O06–O14 接续这些思路，O01／O02／O04 新增空间拼接、旋转、骰子分配的项目设想，O05 结合发现线索展开循环路线经营。未把旧编号改写或冒充新研究。

这是相关设计空间的开放展开，不是穷尽所有游戏形式，更不是“十四项都独创”。一眼区分度、实际策略差异与项目适配分别评价；尚未选择具体方案。

### 用户补充：跨游戏形式的共同决策结构

2026-09-08 用户举例：LoL 中卢锡安保留 E 会约束扎克直接 E 进场，双方闪现又构成后续应对；炉石中法师的 AOE 威胁使骑士逐步铺场，以平衡场面收益和被一次清除的损失。用户借此说明外观／操作形式相距很远的游戏，也可能具有相似底层资源置换。

以上是用户提供的说明情境，本轮未联网核验 LoL 版本、技能参数或炉石具体卡组；不将条件性例子写成所有对局的必然最优策略。炉石手牌通常不公开，对手对 AOE 的判断可能是推测；技能冷却也有观察和估计差异。共同结构及差异的 Agent 分析见 [B01–B05](proposals.md#用户例子后的进一步拆解b01b05)。

用户随后明确该例子仅用于说明不同游戏底层逻辑可能存在共同性，作参考。此补充未采用任何 O 方案、B 分析或固定研究方法，也不要求把资源置换、对手 AI、威慑或博弈搬进外层；爬塔底层逻辑与其他形式的连接继续开放探索。


## R10-Q02：收口前补齐常规主循环基线

日期：2026-09-11。沿用 Q01 已有研究并定向补检，不联网、不改调研原库、不运行原型。用户要求先定 R10，未选择外层形式。本轮比较原作如何在战斗之间组织机会与推进整局，排除战内移动、战场楼层、物品布局、构筑成长“路线”、普通升级和纯模式标签。

### 实际覆盖

- 对深证据库全部 **780 条／42 个游戏**的规则、实践、机制等字段筛查分叉／节点／路线／探索／日程／任务／目标／下一战等概念，得到 **139 条宽候选**，回读外层流程相关记录及 source-index。命中数不作完整性或质量指标。
- 对全部 **56 份深档案**作路线／地图／探索／分叉／日历等宽筛，命中 **29 份**；定点回读 16 份：Slay the Spire、Monster Train、Astronarch、The Last Flame、Vivid Knight、Magicbook、Gladiator Guild Manager、Private Military Manager、ShapeHero Factory、Auto GUI Battler、Just King、Backpack Hero、Backpack Dungeon、Mirror Throne、Combat Alchemy、Girls of the Tower。
- 全部 **106 条发现记录**按序列化字段宽筛得到 **18 条候选**，逐条检查并排除战内路线、配方、概率操纵等无关命中，读取 discovery synthesis／source-index 的相关命中。发现记录仍只是线索。
- 主责复读 R10 现有方案、历史 runProgression.ts 的方案结构、现行 Run Structure 及 G／R 跨题依赖。现行三主题区域和旧切片数量不作为本轮已定新范围。各查询有交叉，不相加为独立证据数。

### 本轮补充和核实

| 归属 | 已有证据／原作过程 | 可支持什么与限制 |
| --- | --- | --- |
| O15 分叉节点 | ev-astro-010-route-morale-potion-swap-decisions；ev-tlf-001-run-resource-routing；ev-slay-the-spire-003-run-loop-economy。Astronarch 按钱、队伍状态与敌包选战斗、精英、事件和商人；The Last Flame 与 Slay the Spire 也通过地图机会逐步修正构筑。 | Astronarch 为早期正式攻略至 v1.5 附近，路线优先级有作者差异；TLF 横跨 EA／早期 1.0，STS 为 2.x 与 2023 指南。可支持已有连接中的路线取舍，不复制生命／营火／价格。 |
| O16 固定阶段与服务分支 | [Monster Train 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/monster-train.md)第 36–41 行、src-mt-wiki-rings；固定 Rings 中选择分叉服务，商店类型、移除、复制与单位奖励补不同缺口。发现 e084 另述节点互斥。 | 2.x／TLD 内容与基础流程分清；档案支持阶段骨架与服务分叉，不足以断言每阶段一律两包或完整未来公开字段。相关深条目多聚焦战内层数，不能由它们反推外层地图。 |
| O17 共享机会面板 | ev-mba-001-shared-offer-panel；[Magicbook 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/magicbook-autobattler.md)第 42 行及来源表。候选混合冒险者、装备、敌人／关卡；花金币刷新、锁候选，阶段进入 Elite／Boss。 | Contract 发布及 2025 更新期资料。原作锁定与搜索规则不自动成为本项目的普通规则；已有条目与档案结合支持本次基线比较。 |
| O18 移动预算探索 | ev-vivid-001-mana-route-jeweler-loop；[Vivid Knight 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/vivid-knight.md)第 47–48 行。移动耗 Mana，探索战斗取得 Keene／单位，衡量继续走岔路或前往出口。 | v1.1.10–v1.2.3 攻略期。回 Jeweler 集中采购与 G03 冲突，不迁移；不证明 O03 的破壁／工具／坍塌。未核定完整视野、成本和掉落公式。 |
| O19 固定序列与自由准备 | ev-sap-012-daily-fixed-enemy-package：SAP Daily 2026-04-08 首发，所有人当天遇相同 14 支敌队；ev-slot-014-preparation-timer-removal：Slotbound Demo 0.2.0 取消准备倒计时，准备后主动开始。 | 两个原作的窄规则分别提供对照，不能拼成同一完整合同，也不把 Slotbound Demo 当成熟正式发行样本。SAP 预览／重试／奖励细则未知；不采用 14 的数量。Auto GUI 50 轮／五轮奖励仅不足证据，见其档案第 9–10、27 行，不用作质量背书。 |
| O08／O09 日程考核 | ev-ggm-001-timeline-guild-resource-loop、ev-ggm-014-glory-mode-failure-budget；ev-pmm-007-weekly-goal-and-mission-deadline、ev-pmm-008-training-risk-schedule。GGM 商店、任务、月度赛随时间到来；PMM 的准备、训练和任务期限相互占用。 | GGM 1.0 战役与 2025 可选 Glory、PMM 2024 Playtest→2025 EA 分清。原作时间流动不能冒充本项目行动日历；工资、伤病和永久经营不附带。 |
| X01 改变终点 | ev-astro-012-key-items-primarch-route-contract：三把钥匙各一局一次机会，合成后替换普通最终 Boss，合成物可卖金币；ev-bpd-010-hidden-boss-multiaxis-exam 为符文／核心／隐藏挑战相邻例。 | Astronarch v1.6.0 官方更新支持规则，旧攻略不证明新 Boss 实战。Backpack Dungeon 2.0.x–2.2.5 的数量与关卡前提不迁移。归终局附加层，不另作地图载体。 |
| X02 主动加码解锁额外终点 | ev-monster-train-008-pact-shard-risk-budget：Divine 收益、Pact Shards、敌方强化和额外 Ring 9。 | 2.x TLD；100 为原作门槛，不移植。与 X01 的物品收集不同；不由此要求本项目普通升级累积危险。 |
| O11 的相邻目标结构 | ev-siralim-ultimate-002-realm-progression-axes，Realm、Depth、Instability、Fortune、Favor、Projects 分担目的地、难度、收益与解锁。 | 0.6 历史和维护中 2.0 时代资料。没有完整外层行走／出入口／目标生成和有限整局合同，不能补成已验证委托网络。 |
| 外层成本反例 | ev-pmm-021-worldmap-to-core-loop-rework：2024 pre-Playtest 开发者因宏观事件难以持续有趣而取消世界地图／派系结构，回到整备—计划—战斗—调整。PMM 档案第 122 行另有任务流程结束时高阶单位刚获得、未充分培养发挥的观察。 | 支持检查外层是否贡献实际决策、成长是否有兑现时间，不证明所有地图／宏观机制应该取消，也不据此判定本项目 O01–O14 必然失败。 |

深证据 id 均回到 [mechanic-evidence.json](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) 与 [source-index](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。O01／O02／O07／O10／O14 的完整项目过程，本轮深库未找到可直接证明其全部规则与实践的记录；O05 的 Loop Hero 仍为 e087／e088 发现线索，O12 的生产迁移仍有固定英雄名册边界。候选可以讨论，但不能称这些推演均已经历原作验证。

本次补研结论限于补齐 O15–O19 常规基线，并将 X01／X02 归终点附加层，没有创新性、平衡、乐趣或运行验收结论。随后用户明确创新应服务整局非线性运营，见 [R10-D01](decisions.md)；Agent 已撤回当时对 O01 的优先建议。该修正来自用户反馈，本轮未新增原库或外部检索，既有证据与限制保持。


## R10-Q03：战外运营关系的定向回读

日期：2026-09-11。用户在 D01 后要求继续。本轮沿已有路线研究转向动作之间的经营关系，未联网、未改原库或执行运行验证。

### 覆盖与判据

- 对全部 **780 条／42 款游戏**的深证据，按经营、后续供给、资源用途、共享竞争、日程、重复兑现与终点关系筛出 **131 条宽候选**，与 Q02 的 139 条路线候选交叉回读。两次检索有重叠，不相加。
- 复用 Q02 的 **56 档案／106 发现记录**覆盖，本次重点回读 GGM、PMM、ShapeHero Factory、Skull Horde 与相关官方索引，复用 Astronarch 和 Loop Hero 已读内容；对全档案另补查竞争、领土、反攻、抢占与 world-map，不把关键词命中当直接机制依据。
- 主责核对 G03、R03、R06、R09、R13 的已定范围，避免把打工换核、受限回退、经济遗物与普通出售重新包装为外层新系统。额外核对下列深证据的完整 id、版本与 source_ids。
- 按“原作先做什么、改变何种资源／机会、之后为何会再作选择”组织；长期公会、工厂、放置和 PvP 均注明迁移前提。没有给全部关系贴已达成非线性或已证明好玩的标签。

### 来源映射

| 比较项 | 依据与具体支持 | 版本、反例及限制 |
| --- | --- | --- |
| NL01 成长渠道 | ev-ggm-001-timeline-guild-resource-loop；建筑开放招募、洗点、教学和装备升级。ev-tft-001-economy-tempo、ev-hsbg-001-use-it-or-lose-it-gold 作未来供给投入的相邻对照。 | GGM 1.0 战役与 2025 可选 Glory；TFT Set 17＋通用攻略、HSBG 2019 基础／S13 实践分清。长期公会不证明有限局内基地，不移植人口买级、工资、普通利息或金币过期。 |
| NL02 关系改变机会 | ev-ggm-001-timeline-guild-resource-loop、ev-ggm-010-task-size-loss-replacement-decisions；[GGM 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/gladiator-guild-manager.md)第 40–43 行，src-ggm-official-1-0。任务影响声望、后续单位／物品及敌对后果，可拒绝当前任务等待新项。 | 官方 1.0 2024-06-22 与攻略分层。没有完整声望阈值／任务图／概率，不证明已接任务可无损中断；关系后果不等于观察玩家的战略 AI。 |
| NL03 资产分流转用 | [ShapeHero 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/shapehero-factory.md)第 45–48、75–77、92 行；src-shf-official-statues-tablets，来源索引约第 6508 行。Statue／Tablet 持续产生只能供合成的 replica，玩家调整基础／高阶生产用途。 | 正式 1.0 规则与其后玩家实践。移除需 Pickaxe、拆除损在途物、研究不普遍退款；不据此允许项目自由精细回退或产线生成战斗英雄。 |
| NL04 闲置资源用途 | ev-shf-009-recycle-dead-resource-economy、ev-shf-008-route-research-shop-rng-economy；同档案第 78、82–85 行。Recycle Tech 与其他研究／空间投入竞争，闲置资源可转经验／经济／材料。 | v1.0 社区实践，10–16% 仅作者时期观察，不采用。缺 Motif／Ink、前置配方或奖励与 Research 不兼容仍可能失败，界面提示不等于提供转型答案。 |
| NL05 准备时机 | ev-pmm-007-weekly-goal-and-mission-deadline、ev-pmm-008-training-risk-schedule；[PMM 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/private-military-manager.md)第 82–95、113–115 行。任务期限影响训练与持有成本，接近目标后改变训练、休息与购买。 | 2024 Playtest／重做、2025 EA、个别后续实践分清。训练被批评可由极简脚本代做，重做后仍有重复劳动和信息负担分歧；未证明多个已接任务任意交错暂停。 |
| NL06 成长与推进 | ev-skull-horde-003-threat-resource-race；[Skull Horde 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/skull-horde.md)第 28、111、116 行。获取资源完善引擎与 Peril 压力竞争，玩家决定继续探索或推进。 | 正式版至 main v1.032、2026-06-16；普通实时与 Explorer 按楼层增长必须区分。贪资源、引擎上线晚、菜单耗时是反例，不移植现实准备压力。 |
| NL07 稳定区与前沿 | ev-mpig-006-stable-stage-farming-pivot：前沿受阻转向稳定清理，再培养推进，但当前区可能不产所需品级。ev-ggm-014-glory-mode-failure-budget 对照旧战役无限练兵与后续约束。 | MPIG 早期正式至 2026-08-31 自动化更新；GGM Glory 为 2025。原作放置／长期成长不等于本项目单局允许无限刷取；败后回刷不适用于当前败即终局。 |
| NL08 外部供给反馈 | ev-dota-underlords-003-shared-pool-contest-pivot、ev-dota-underlords-022-pool-resize-lifecycle。买入、卖出、淘汰改变共享池，观察争抢影响搜索与转型。 | 2020-04-30 至最终 Standard、2019 行为讨论分清；动态价格、囤牌有效程度未量化。PvP 多副本不能直接转入单人普通唯一；本库未找到完整单机战略竞逐过程。 |
| NL09 重复环境 | discovery e087／e088 Loop Hero；放置敌人、建筑、地形，在循环中形成危险与成长；e093 Thronefall 为建设与防御相邻线索。 | 仍为发现层，非完整深证据。自由拆除、回收、收益限制、对手反馈未验证；候选价值不能由地图画成环形推定。 |
| X01／X02 终点层 | ev-astro-012-key-items-primarch-route-contract（v1.6.0）；ev-monster-train-008-pact-shard-risk-budget（2.x TLD）。分别为收集后保留终点／出售和强收益伴随后续压力／附加终战。 | 旧实践不证明 Primarch 新 Boss 实战，不编钥匙装备占用或随时赎回；MT 原作 100 门槛不移植。两者不单独证明整局运营创新。 |

### 与“复杂不等于好玩”直接相关的反例

- ev-pmm-021-worldmap-to-core-loop-rework、src-pmm-official-loop-rebuild-2024-05-13：开发者因 world-map／faction 难以持续产生有趣事件，撤回宏观地图，先验证小队、训练、计划和任务循环。证明该版本的失败与修正，不证明所有地图或关系系统都无效。
- GGM 官方以 ev-ggm-014 说明旧规则可用无限时间磨过难度；引入可选 Glory 的跳过／失败预算是原作修正，不提供本项目必需次数。
- ShapeHero 与 PMM 的问题说明，可重新安排资源、更多目标或日程并不自动形成有价值选择。项目必须说明实际可行替代，不能只增加管理动作。

NL01–NL09 的项目适配及 Agent 推荐归 proposals。没有新增用户决定，没有把领土税收、局内基地或竞争 AI 的未验证完整流程补成原作事实。深证据回到 [证据库](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)及 [来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)，发现线索沿原库保留。
