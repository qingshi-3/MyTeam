# R12 敌情预告与准备：已有证据

讨论材料，不是现行权威。进程见 [总览](../../../roadmap.md)，完整机制与取舍见 [方案讨论](proposals.md)。本轮只读已有调研与现行基线，未联网。

后续确认：2026-09-09 用户采用 PV02＋PV03 作为初期基础，见 [R12-D01](decisions.md)。下文保留研究时的原作证据、项目候选与实现基线；此次确认未新增调研，也不改变原作资料的缺口。

## R12-Q01：会改变准备方向的敌情，应在什么选择之前公开到哪一步

日期：2026-09-09。聚焦战前信息能支持的决定：已有资产调整、招募／培养投入及选择挑战。普通战前看敌阵已有基线，本题细化信息内容、提前量与不确定性；不合并战报、日志、局外图鉴、反制内容设计或失败续关。

### 前题收束与现行边界

[R11-D01](../attrition-and-recovery/decisions.md)已闭合普通胜后生命与倒地资格；法力、召唤有现行基线，具体持续效果归 M04／M05／M06，休息用途归 R10／R09。无需为完整度重选败即终局，R11 可基础阶段收束；实现差异仍沿 I08 等待统一整合。

- [核心权威](../../../../gameplay-design/tower-autobattler-core.md)第 45、82、86 行已有敌方组成、楼层规则、实际地形及敌人起点预览；第 94 行要求 Boss 提前提供能帮助准备的机制信息，但提前多久、精度与调整窗口未定。
- [G03-D01](../../../01-global-boundaries/run-rhythm/decisions.md)已允许不限时调整已有装备、站位与上阵／后备；[G02-D01](../../../01-global-boundaries/player-agency/decisions.md)已定初期无战中介入。不能靠战中提示补出临场换人或手动解控。
- [M04-D02](../../../02-foundation-models/attributes-and-statuses/decisions.md)与 I19 要求敌方控制、预告和可到达反制一起考虑。公开机制不等于保证掉落某件唯一答案；也不能用存在一件稀有反制就证明准备机会足够。
- R10 外层形式仍在并行开放讨论。信息提前量可按“选择挑战前／相关补强机会前／最终部署时”描述，不先规定几层或固定分叉地图。

**只读代码定位**：`src/UI/TowerScreenController.cs:38` 与 `src/Run/TowerGenerator.cs:13` 的路线卡主要提供标题、描述、风险；`src/App/GameFlowCoordinator.cs:270` 选择节点后，才在后续流程取得遭遇并显示部署。`src/UI/DeploymentScreenController.cs:112`、`:257` 与 `src/UI/EnemyDeploymentPreview.cs:27` 提供敌名／数量、起点、职责、近远程、射程和 Boss 标志；`src/UI/DeploymentBoard.cs:84`、`:104` 显示危险／目标／阻挡等地形预览。由此只能说当前这条路径的详细敌情主要支持已选节点后的整备，未见完整技能、属性、Boss 阶段与提前多节点公开契约；不宣称全仓所有入口均已审计，也不把 Battle Lab 当作征程试打权限。未运行或验证界面体验。

### 检索范围与判据

对 [780 条深记录](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[106 条发现记录](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)和 56 份深档案检索，回查 [深层来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。JSON 搜字段值而非字段名；四核心字段是 `rule_support / practical_support / mechanism / engine`。发现层缺部分核心字段，命中数不能与深层质量直接相比。

- 首筛：`preview|scout|forecast|predict|telegraph|intent|预告|预览|侦察|预测|敌情|意图|情报`。深记录全字段 216／核心 48，发现层 15／核心 2；档案 49 份／156 行命中。
- 独立扩查核心字段：`inspect|visible|visibility|hidden|information|known|reveal|fog|intelligence|formation|查看|可见|公开|隐藏|已知|揭示|迷雾|阵型|提示` 与 `enemy|enemies|opponent|boss|wave|敌|对手|波次|关底` 交叉。深 52，发现 3。
- 模拟／知识扩查：`simulat|rehears|test.?fight|bestiary|encyclop|codex|monster.?book|observe.{0,30}repeat|模拟|试打|试战|演练|图鉴|敌人手册`。核心深 11，发现 1；捕获 PMM 反复模拟，以及与本题不同的词条图鉴／模拟一致性等材料。
- 回看 [G01-F23–F41／FL01](../../../01-global-boundaries/battlefield-experience/evidence.md#局部受挫与失败解释)，定点回读下列来源。查询彼此有重叠，不相加、不代表逐篇精读全部候选。`intentional`、Scout 职业名、隐身、同步模拟、配方预览、局外奖励与事后统计等误命中均分流。
- 纳入详细比较的条件：确实改变准备时知道什么、何时知道、信息是否稳定或怎样取得；仅说明某敌人有反制、某界面不清楚，不另列一个信息制度。

### S01：本场敌阵与能力可查，给已有资产调整提供依据

**Tiny Auto Knights**：`ev-tak-001-async-snapshot-and-live-mode-separation`，`src-tak-thread-async-pvp`（2025-01 至 05 Playtest／Demo）、`src-tak-official-demo-july-2025-07-23` 明确 Ranked 对手预览及上传队伍快照。可针对保存的 3×3 阵容准备；资料没有完整字段、锁定时刻或准备中重抽规则，也不证明所有模式都能预览。2026-02 好友实时模式是另一条模式，不拼接为相同生命周期。

**Auto Brawl Chess**：`ev-auto-brawl-chess-012-journey-fixed-enemy-adaptation`，`src-abc-help-journey-2022` 的历史 Journey 提供 Boss 队预览；页面 2026 wrapper 不代表规则重新核实。`src-abc-discussion-journey-2-35-2023` 的固定开局与免疫／隐身压力只支持单关观察，不能推成所有 Journey 波次固定。重试与 Faction 限制不迁入本题。

**Gladiator Guild Manager**：`ev-ggm-006-enemy-items-priority-adaptation`，`src-ggm-official-0-942`（2024-03-09）、`src-ggm-steam-achievements-guide`、`src-ggm-steam-campaign-v1`。装备敌人可识别，玩家检查危险远程／AOE 持有者再调整站位、控制或消耗品。选定敌人加入装备同时降低等级预算，说明相近总战力可以包含不同威胁；不照搬数值等价公式或手动优先级。

**Mirror Throne**：`ev-mirror-throne-009-reader-onboarding-evolution`、`src-mt-demo2`（2024-02-02）等记录加入等级相关敌情预览。能支持具体信息而非纯战力总分，但缺全字段与正式版延续表；新手仍有试错学习反馈，不能宣称补 tooltip 已解决理解问题。

**Monster Train**：[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/monster-train.md)标准循环与 `ev-monster-train-004-three-floor-battle-order`、`src-mt-wiki-battle`、`src-mt-review-layers`、`src-mt-review-capacity` 支持战前主要敌人／可选 Trial 预览。它不等于全部波次与数值公开。上述作品共同支持 PV02 的机制方向，项目提出的完整关键机制检查范围仍是建议。

### S02：远期关键敌人早告知，信息用于沿途投入

**Gods vs Horrors**：`ev-gods-vs-horrors-015-enemy-package-counter-grid`，`src-gvh-guide-strategy-3493071391`（开发者发布、社区共创，2025-06-04，1.0）明确最终 Boss 从开局已知。`src-gvh-discussion-scorpion-counter`（2025-05-16，1.0）另支持下一波预览；首次看到具体字段的准确窗口未知。`ev-gods-vs-horrors-003-deterministic-combat-explanation` 的确定性及解释困难不能补成全战预测功能。早知 Boss 不证明开局公开全部技能或普通波次。

该作攻略列出顺劈承压、右侧狙击、一次先手、护盾等针对性回答；评测仍报告确定构筑后缺关键反制。只能说早告知让沿途准备成为可能，不能证明供给已保证或总体平衡成立。

**Dungeon 100**：`ev-d100-016-boss-deck-preview-challenge-loophole`、`src-d100-official-2023-07-07` 明确选角与战斗中可看当前 Boss 卡组。它展示到具体构筑层，不只是身份；没有授权推成完整隐藏数值／AI／触发关系图。历史构筑变 Boss、Challenge 存档和镜像复制争论属于其他制度，用户此前保留意见仍不变。

**Monster Train**：`ev-monster-train-020-seraph-four-counter-packages`；`src-mt-wiki-seraph-temperant`、`src-mt-wiki-seraph-chaste`、`src-mt-wiki-seraph-diligent`、`src-mt-wiki-seraph-patient`（末修订 2021–2023），2.x Ring 8。可预览不同 Seraph，分别压攻击、削减状态、消耗法术并污染牌库、读取 Rally／Incant 成长，因而准备方向不同。资料未闭合精确首次公开时点，不能照搬“开局全部已知”或“提前固定 X 场”。

### S03：完整未来序列与只给粗略情报，是不同的项目政策

**Super Auto Pets**：`ev-sap-012-daily-fixed-enemy-package`、`src-sap-official-daily-2026`（2026-04-08）只直接确认每天所有玩家面对相同 14 队，**没有说明游戏内提前公开整份序列**。它可作为固定挑战结构参照，PV04“完整未来遭遇提前可查”是进一步的项目推演，不冒充原作功能，也不新增每日模式或重试。

PV01 的“远处只给类型／威胁摘要”可由项目当前路线卡基线具体说明；它与开战前完全不显示敌阵不是一回事。若坚持未知直到开战，便会削弱既定战前准备。此种更严格隐藏未获采用；不是将所有缺字段的原作归成有意隐藏。

### S04：信息本身需要取得，或已看到的对手未必就是最终目标

**Auto Battleships**：[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-battleships.md)、发现 `e054`／`s033`（官方商店）。截至既有 2026-09-03 研究只有产品页及 2026-04-14 公告，没有公开实践闭环。承诺战前用不同形状／重复次数的侦察物品部分揭示敌舰布局，再调整己方位置。价格、是否消耗、占槽、信息保存、反侦察、匹配与精确时点全部缺证。**PV05 的“消耗金币／机会”只能作为项目可选成本，不能归因于已证实原作付费。**

**TFT**：`ev-tft-004-scouting-positioning`，`src-tft-bamboo-scouting-positioning`、`src-tft-bamboo-beginner`，2026-09-02 访问的未标版本指南。备战可查看其他棋盘，躲钩／突袭／群控，调整主坦与核心位置；潜在对手不唯一，对方也能换边，故侦察支持概率性准备。PV06 比较的是敌情不完全确定，而非故意提供错误情报。单机若用候选敌人集合替代玩家对手，是项目适配；不自动创建能针对玩家临时变阵的 AI。

### S05：从说明规则，到预演局部后果或试打整场

**Vivid Knight**：`ev-vivid-010-gem-intent-skip-intervention-layer`，`src-vivid-guide-easy-start`、`src-vivid-guide-maze9`、`src-vivid-guide-boss`、`src-vivid-official-1-1-11`，v1.1.10–1.2.3。显示下一动作，玩家据危险意图保留两面护盾、施放或跳过 Gem。**Backpack Hero**：`ev-backpack-hero-023-readable-enemy-counter-packages`、`src-bh-official-enemy-rework` 等，2023 重做与 1.0 期指南支持更明确意图／可中断窗口。两者主要服务战中动作，不能变成本项目已有介入权限。

**Into the Breach**：发现 `e091`／`s060` 只有官方商店级“敌方攻击先预告，玩家用移动／位移／击杀改写”线索，没有补丁级完整精度或例外表。PV07 将这种局部因果可见适配为部署时的首个目标／起始覆盖辅助，是项目建议，不保证首个目标之后的整场路径和结果。

**PMM**：`ev-pmm-001-observe-refine-repeat-loop`，`src-pmm-official-loop-rebuild-2024-05-13`、`src-pmm-official-playtesting-2024-06-21` 与 `src-pmm-review-kr-runlog-2026-02-19`。官方循环及停更后最终版流程记录支持先模拟任务、改位置／装备／区域策略，再进入实际行动。`ev-pmm-014-section-report-attribution` 保留玩家对模拟之间缺直接结果比较的批评。Campaign／Freeplay／Prologue 日程不同，不能补定无限免费次数或完美预测。PV08 采用这种准备权限会改变本项目首次正式挑战的未知风险，是独立方向，不由“不限时思考”自动授权。

### 反例、相邻材料与缺口

- `ev-bpd-011-enemy-trait-scaling-preview`、`src-bpd-review-endless-scaling-traits-2026-08-04` 等：2.2.x 深层个案报告随机净化／恢复／多击组合与预览不足，使构筑被克制；无 seed／发生率。它提示要在仍能准备时披露改变胜负方向的能力，不证明所有随机敌人都不合理。
- `ev-neon-auto-party-018-actual-output-tooltip` 只证明 0.4.1 显示修正后伤害／治疗，未证实计入目标抗性、位置或 Barrier。不能拿“实际数值”补成精确对敌伤害或全战胜率预测。
- PMM 总战力不解释具体原因、Gods vs Horrors 无 RNG 仍有归因困难，都不证明游戏提供可靠胜负计算器。本轮没有足够来源把完整战局答案视为常见成熟基线。
- 已知针对件／Boss 攻略不自动证明原作何时公开；TFT 猜对手、TAK 已存快照、ABC 单关固定开局不能合并成一条“所有预览都锁死最终敌方”的规则。
- 日志、回放与事后统计留后续反馈问题；本局可用池查询沿 I27，局外图鉴／知识解锁另看 R14。当前扩查未取得足够证据把“初见必须失败才能解锁敌情”列作成熟通用制度。

据此形成 PV01–PV08。原作事实、开发中线索、项目政策与不同权限的迁移分别标注；研究提出时尚无用户决定，后续确认见 [R12-D01](decisions.md)。
