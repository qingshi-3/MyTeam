export type DomainStatus = "accepted" | "derived" | "open" | "guardrail";
export type DomainGroup = "loop" | "decision" | "simulation" | "composition" | "feedback" | "guardrail";
export type RelationKind = "feeds" | "constrains" | "feedback";

export type EvidenceSignal = {
  id: string;
  game: string;
  domain: string;
};

export type FoundationNode = {
  id: string;
  code: string;
  label: string;
  group: DomainGroup;
  status: DomainStatus;
  x: number;
  y: number;
  summary: string;
  responsibility: string;
  inputs: string[];
  outputs: string[];
  channels: string[];
  openQuestion: string;
  evidenceSummary: string;
  evidence: EvidenceSignal[];
  custom?: boolean;
};

export type FoundationEdge = {
  id: string;
  from: string;
  to: string;
  relation: RelationKind;
  label: string;
};

export type FoundationZone = {
  id: string;
  label: string;
  summary: string;
  group: DomainGroup;
  x: number;
  y: number;
  width: number;
  height: number;
};

export type FoundationWorkspace = {
  nodes: FoundationNode[];
  edges: FoundationEdge[];
  zones: FoundationZone[];
  decisions?: Record<string, FoundationDecision>;
};

/** A scoped design decision, not acceptance of an entire foundation domain. */
export type FoundationDecision = {
  scenarioId: string;
  optionId: string;
  status: "tentative" | "confirmed" | "deferred";
  note: string;
  basis: string;
  updatedAt: string;
};

export type Diagnostic = {
  id: string;
  severity: "error" | "warning" | "info";
  title: string;
  detail: string;
  nodeId?: string;
};

export const STATUS_LABELS: Record<DomainStatus, string> = {
  accepted: "已确认",
  derived: "待确认结构",
  open: "尚未决定",
  guardrail: "工程护栏",
};

export const GROUP_LABELS: Record<DomainGroup, string> = {
  loop: "循环与终局",
  decision: "决策条件",
  simulation: "战斗规则",
  composition: "构筑与对抗",
  feedback: "反馈闭环",
  guardrail: "工程护栏",
};

export const RELATION_LABELS: Record<RelationKind, string> = {
  feeds: "影响",
  constrains: "约束",
  feedback: "反馈",
};

const ev = (id: string, game: string, domain: string): EvidenceSignal => ({ id, game, domain });

const node = (value: FoundationNode) => value;

const authoredNodes: FoundationNode[] = [
  node({
    id: "f01", code: "F01", label: "产品与局外边界", group: "loop", status: "derived", x: 130, y: 130,
    summary: "规定游戏承诺、入口、重开、解锁与开发工具的边界。",
    responsibility: "界定单人爬塔产品的入口与外层循环，避免教程、沙盒、局外成长和正式局内规则互相污染。",
    inputs: ["起始选择", "局外解锁", "设置", "已保存局内状态"],
    outputs: ["新开或恢复一局", "失败与重开交接", "开发工具隔离入口"],
    channels: ["起始选择", "难度", "设置", "保存与恢复", "战斗实验室"],
    openQuestion: "完整局外成长、解锁经济与难度结构尚未定义。",
    evidenceSummary: "不同入口、失败契约和恢复能力应是明确边界，而不是复制多个互相漂移的模式。",
    evidence: [
      ev("ev-backpack-hero-031-story-quick-game-layer", "Backpack Hero", "onboarding-sandbox-mode-layer"),
      ev("ev-gods-vs-horrors-018-ladder-casual-failure-contract", "Gods vs Horrors", "mode-failure-experimentation"),
      ev("ev-slot-017-run-persistence-performance", "Slotbound", "run-save-performance-stability-lifecycle"),
    ],
  }),
  node({
    id: "f02", code: "F02", label: "局内路线、节奏与终止", group: "loop", status: "accepted", x: 190, y: 250,
    summary: "把有限的爬塔路程变成连续、有代价的适应过程。",
    responsibility: "规定玩家如何经过路线节点、结算每次选择，并在有限适应窗口内到达最终成功或确定失败。",
    inputs: ["路线状态", "节点选项", "队伍与构筑", "战斗结算", "局内风险资源"],
    outputs: ["下一决策", "改变后的局内状态", "成功或失败"],
    channels: ["路线", "战斗与精英", "招募", "商店", "事件", "休整", "Boss"],
    openQuestion: "路线拓扑、节点节奏、风险曲线和重试规则仍待设计。",
    evidenceSummary: "局长应匹配仍有构筑决策的时间范围，风险、继续深入和回退结算需要明确。",
    evidence: [
      ev("ev-loot-loop-002-decaying-health-time-budget", "Loot Loop", "run-health-clock-active-extension"),
      ev("ev-survivor-mercs-016-extraction-economy-rework", "Survivor Mercs", "risk-reward-extraction-lifecycle"),
      ev("ev-guildrun-018-boss-token-rewind-transactions", "Guildrun", "meta-resource-rewind-transaction-integrity"),
    ],
  }),
  node({
    id: "f03", code: "F03", label: "战斗循环与胜负", group: "loop", status: "accepted", x: 250, y: 365,
    summary: "预览、部署、自动战斗、有限干预、战报和单次结算。",
    responsibility: "让一场战斗只产生一个可解释的终局事实，并把不可变结果交给局内循环。",
    inputs: ["阵型", "敌人与环境", "载入配置", "随机种子", "战术指令"],
    outputs: ["胜负或超时", "不可变贡献事实", "局内结算输入"],
    channels: ["单位", "能力", "装备", "遗物", "战术指令", "遭遇", "战报"],
    openQuestion: "超时阈值以及同时胜负、平局的优先级尚未确定。",
    evidenceSummary: "单位死亡、战斗终止和防僵局是三条独立规则，不能由一个模糊的死亡事件顺带决定。",
    evidence: [
      ev("ev-skull-horde-004-death-respawn-life", "Skull Horde", "death-respawn-defeat-state"),
      ev("ev-loot-loop-014-death-win-settlement-rework", "Loot Loop", "death-boss-kill-deterministic-settlement"),
      ev("ev-dungeon-tactics-idle-006-stalemate-speed-guard", "Dungeon Tactics", "anti-stalemate-and-speed-stability"),
    ],
  }),
  node({
    id: "f04", code: "F04", label: "玩家权限与决策窗口", group: "decision", status: "accepted", x: 520, y: 325,
    summary: "明确玩家何时能决定，何时只能观察，哪些行为由自动系统拥有。",
    responsibility: "区分战前部署、局内选择、战斗观察和战术干预的权限，拒绝模糊的半自动微操。",
    inputs: ["当前阶段", "部署状态", "暂停与速度", "指令费用与次数"],
    outputs: ["合法输入窗口", "提交意图", "有原因的拒绝"],
    channels: ["路线选择", "部署", "检查", "暂停与速度", "战术指令", "战报继续"],
    openQuestion: "未来若扩展战斗内权限，需要单独确认；当前只接受两条独立战术指令。",
    evidenceSummary: "自动单位、玩家指令和阶段性注意力窗口需要明确归属，不能混成持续微操。",
    evidence: [
      ev("ev-survivor-mercs-003-commander-merc-control-boundary", "Survivor Mercs", "automatic-combat-command-boundary"),
      ev("ev-girls-of-the-tower-004-active-card-ownership", "Girls of The Tower", "active-command-card-owner"),
      ev("ev-loot-loop-010-automation-manual-rerun-boundary", "Loot Loop", "automation-attention-contract"),
    ],
  }),
  node({
    id: "f05", code: "F05", label: "信息、预告与隐藏状态", group: "decision", status: "derived", x: 440, y: 130,
    summary: "区分已知、未知、低概率、不可用和真正隐藏的事实。",
    responsibility: "规定玩家在承诺前、结算中和按需查看时能知道什么，使信息成为策略资源而不是界面补丁。",
    inputs: ["敌人与楼层规则", "选项合法性", "目标与行动状态", "概率或资格"],
    outputs: ["预览", "意图", "不确定性标记", "可行动的失败链"],
    channels: ["路线", "商店与奖励", "部署", "敌人预览", "检查器", "HUD"],
    openQuestion: "概率公开程度、供给池可见性、敌人意图时长与合法隐藏仍待定义。",
    evidenceSummary: "信息必须留下可执行的响应窗口，同时要把不可能、不可用、未知和随机区分开。",
    evidence: [
      ev("ev-tft-004-scouting-positioning", "Teamfight Tactics", "formation-targeting"),
      ev("ev-backpack-hero-023-readable-enemy-counter-packages", "Backpack Hero", "enemy-package-adaptation"),
      ev("ev-auto-chess-023-pool-blocking-and-legendary-discovery", "Auto Chess", "recruitment-pool-lifecycle"),
    ],
  }),
  node({
    id: "f06", code: "F06", label: "随机生成、供给池与可达性", group: "decision", status: "open", x: 610, y: 145,
    summary: "判断一个选择是不可能、低概率，还是仍可在剩余路程中抵达。",
    responsibility: "管理选项生成、资格、权重、纠偏与构筑可达性，让随机性产生适应而不是死局。",
    inputs: ["内容池", "解锁与资格", "权重", "随机种子", "当前构筑", "剩余路程"],
    outputs: ["带来源的选项", "可达性信号", "纠偏结果", "明确不可用"],
    channels: ["招募", "装备与遗物奖励", "指令", "商店", "事件", "遭遇"],
    openQuestion: "供给池、权重、保底、重复保护和最低可达性均未确定。",
    evidenceSummary: "理论内容量不等于实际多样性；零资格、死选项、修复路径和剩余适应时间必须进入模型。",
    evidence: [
      ev("ev-gods-vs-horrors-017-pool-pivot-reachability", "Gods vs Horrors", "pool-agency-pivot-window"),
      ev("ev-mpig-007-chest-weight-grade-wall", "My Party Is Grinding", "multi-stage-loot-probability-zero-weight-wall"),
      ev("ev-shf-019-unproducible-reward-rng-negative", "ShapeHero Factory", "offered-but-currently-unproducible-reward"),
    ],
  }),
  node({
    id: "f07", code: "F07", label: "实体、归属与生命周期", group: "simulation", status: "accepted", x: 455, y: 535,
    summary: "区分角色、队伍、载体、来源、拥有者、临时实体与清理边界。",
    responsibility: "保证每个运行时对象和效果都有唯一身份、归属、加入、转移、死亡和清理语义。",
    inputs: ["内容定义", "实例身份", "队伍", "来源", "拥有者", "生成与死亡事实"],
    outputs: ["合法实体", "来源链", "有效生命周期", "转移与清理结果"],
    channels: ["英雄", "敌人", "临时单位", "装备", "遗物", "召唤", "战报"],
    openQuestion: "复活只有在单独创作时存在，其通用合同尚未定义。",
    evidenceSummary: "生成意图、占位、持续归属、来源死亡和装备转移是不同的生命周期问题。",
    evidence: [
      ev("ev-storybook-brawl-012-summon-occupancy-ownership", "Storybook Brawl", "summon-placement-token-lineage"),
      ev("ev-astro-017-summon-source-death-cleanup", "Astronarch", "summon-lifetime-source-attribution-lifecycle"),
      ev("ev-tft-002-item-holder", "Teamfight Tactics", "equipment-transition"),
    ],
  }),
  node({
    id: "f08", code: "F08", label: "时间、触发与行动顺序", group: "simulation", status: "accepted", x: 650, y: 535,
    summary: "规定同时发生、延迟发生和连锁发生的事件如何排队并终止。",
    responsibility: "统一时钟、冷却、持续时间、快照、同时意图、触发祖先、优先级和因果深度。",
    inputs: ["模拟时间", "行动意图", "不可变事件", "触发", "优先级", "限制"],
    outputs: ["有序行动", "反应波", "有限连锁", "可读顺序"],
    channels: ["攻击", "能力", "状态", "装备", "遗物", "召唤", "楼层规则"],
    openQuestion: "完整的玩家可见同帧优先级表尚未确定。",
    evidenceSummary: "嵌套触发、同时死亡与生成以及活性故障要求显式顺序、事件祖先和终止边界。",
    evidence: [
      ev("ev-storybook-brawl-018-trigger-readability-recursion", "Storybook Brawl", "event-order-recursion-reporting"),
      ev("ev-monster-train-025-spawn-order-rollback", "Monster Train", "deterministic-spawn-resolver"),
      ev("ev-neon-auto-party-020-effect-resolution-liveness-fixes", "Neon Auto Party", "effect-resolution-liveness-fixes"),
    ],
  }),
  node({
    id: "f09", code: "F09", label: "空间、目标与占用", group: "simulation", status: "accepted", x: 845, y: 535,
    summary: "让站位、射程、路线、阻挡、位移和目标变化成为真实规则。",
    responsibility: "管理棋盘拓扑、合法格、占用、射程、目标、交战点、寻路、仲裁与空间预览。",
    inputs: ["逻辑棋盘", "地形合法性", "占用快照", "队伍", "射程", "移动意图"],
    outputs: ["合法目标与路径", "移动或位移", "改变后的空间访问"],
    channels: ["部署", "单位AI", "攻击与治疗", "能力", "指令", "环境"],
    openQuestion: "职责偏好权重、目标保留参数与通用位移政策仍待定义。",
    evidenceSummary: "选择器形状、可达性、目标规则和多区域压力应成为可检查的独立空间规则。",
    evidence: [
      ev("ev-guildrun-005-targeting-reposition-counterpack", "Guildrun", "targeting-pathing-projectile-positioning"),
      ev("ev-tak-017-row-column-localization-failure", "Tiny Auto Knights", "localization-spatial-affordance"),
      ev("ev-monster-train-021-last-divinity-three-floor-exam", "Monster Train", "multi-lane-final-exam"),
    ],
  }),
  node({
    id: "f10", code: "F10", label: "行动、费用与合法性", group: "simulation", status: "derived", x: 1035, y: 535,
    summary: "所有自动或主动行为都先完整预检，再一次性提交或明确拒绝。",
    responsibility: "把权限、目标、条件、费用、冷却、次数、容量和效果计划收束成一个原子行动。",
    inputs: ["行动状态", "决策窗口", "目标与空间", "资源", "冷却与次数", "效果计划"],
    outputs: ["已接受意图", "原子费用与效果", "无副作用拒绝"],
    channels: ["单位行为", "能力", "战术指令", "装备或状态授予行动"],
    openQuestion: "法力、施法、引导、打断和退款尚未形成通用合同。",
    evidenceSummary: "目标、费用、容量、来源和执行路径都应预检，失败不能只做到一半。",
    evidence: [
      ev("ev-monster-train-024-transaction-feedback-rework", "Monster Train", "transaction-and-affordance-failure"),
      ev("ev-loot-loop-004-power-active-resource", "Loot Loop", "kill-drop-active-cast-resource"),
      ev("ev-backpack-hero-013-cr8-execution-graph", "Backpack Hero", "directional-execution-graph"),
    ],
  }),
  node({
    id: "f11", code: "F11", label: "状态、资源与属性", group: "simulation", status: "derived", x: 550, y: 720,
    summary: "每个可变事实都有作用域、拥有者、读法、上限、重置与当前投影。",
    responsibility: "定义生命、保护、人口、战术点、货币、计数器和状态等不同事实如何存在和被读取。",
    inputs: ["基础定义", "有来源的贡献", "局内选择", "运行时事件", "快照或实时读取"],
    outputs: ["当前值", "资源变化", "状态与标签", "可撤销属性投影"],
    channels: ["属性", "状态", "特质", "装备", "遗物", "指令", "人口", "经济"],
    openQuestion: "通用资源分类，以及哪些资源能共享接口，尚未确定。",
    evidenceSummary: "共享资源读取、基础与最终值捕获、状态风险和退出条件不能压成同一种仪表。",
    evidence: [
      ev("ev-gods-vs-horrors-011-japanese-sp-lifecycle", "Gods vs Horrors", "faction-resource-balance-lifecycle"),
      ev("ev-dungeon-tactics-idle-002-base-stat-layering", "Dungeon Tactics", "base-bonus-final-stat-layering"),
      ev("ev-slay-the-spire-014-stance-resource-rules", "Slay the Spire", "stance-energy-risk"),
    ],
  }),
  node({
    id: "f12", code: "F12", label: "效果、伤害、治疗、控制与死亡", group: "simulation", status: "accepted", x: 970, y: 720,
    summary: "把一次合法效果解析成尝试值、有效值、状态变化和最终归属。",
    responsibility: "统一减伤、保护、生命、治疗、控制、致死、击杀、清理和后续反应的权威结算。",
    inputs: ["来源与拥有者", "目标", "效果类型", "量值", "当前状态", "抗性与顺序"],
    outputs: ["有效结果", "状态变化", "控制生命周期", "死亡与击杀", "战报贡献"],
    channels: ["攻击", "能力", "状态", "装备", "遗物", "指令", "环境"],
    openQuestion: "完整公式层次、保护溢出、转换顺序和同帧致死仍待定义。",
    evidenceSummary: "控制需要界限，循环需要上限和反制，有效结算必须与画面和战报一致。",
    evidence: [
      ev("ev-shf-012-knockback-progressive-resistance", "ShapeHero Factory", "repeated-displacement-diminishing-return"),
      ev("ev-siralim-ultimate-019-historical-cleric-cap-counter", "Siralim Ultimate", "healing-loop-lifecycle"),
      ev("ev-survivor-mercs-018-merc-resolution-attribution-lifecycle", "Survivor Mercs", "automatic-combat-resolution-attribution"),
    ],
  }),
  node({
    id: "f13", code: "F13", label: "叠加、冲突、转换与循环限制", group: "simulation", status: "derived", x: 760, y: 720,
    summary: "决定多个规则同时修改同一事实时如何合并、替换、消耗并停止。",
    responsibility: "管理来源隔离、加乘覆盖顺序、刷新、溢出、驱散、消耗、递归、数值上限和防僵局。",
    inputs: ["多个贡献", "来源身份", "优先级", "聚合、读取与消耗策略", "事件祖先"],
    outputs: ["唯一投影或冲突", "有限后续工作", "可见上限", "可撤销句柄"],
    channels: ["属性", "状态", "特质", "装备", "遗物", "转换", "人口缩放"],
    openQuestion: "跨系统的统一冲突、递归和溢出政策仍待设计。",
    evidenceSummary: "事件祖先、数值预算、有限读取者和可见上限能保留循环乐趣，同时阻止无限结算。",
    evidence: [
      ev("ev-storybook-brawl-018-trigger-readability-recursion", "Storybook Brawl", "event-order-recursion-reporting"),
      ev("ev-gods-vs-horrors-019-infinity-recursion-guards", "Gods vs Horrors", "endless-scaling-overflow"),
      ev("ev-slay-the-spire-011-poison-catalyst-build", "Slay the Spire", "dot-multiplier"),
      ev("ev-backpack-hero-016-overheat-lifecycle", "Backpack Hero", "recursion-guard-lifecycle"),
    ],
  }),
  node({
    id: "f14", code: "F14", label: "构筑结构、职责与内容接入", group: "composition", status: "derived", x: 940, y: 245,
    summary: "把跨载体的供应、读取、转换、生存、空间与兑现组织成可读构筑。",
    responsibility: "定义内容如何组合、谁负责什么、缺少哪条链，以及桥接件和机会成本如何存在。",
    inputs: ["内容渠道", "归属图", "状态与资源", "空间要求", "当前队伍"],
    outputs: ["构筑链", "职责投影", "缺环与冲突诊断", "内容挂点"],
    channels: ["英雄", "能力", "状态", "特质", "装备", "遗物", "指令", "临时单位"],
    openQuestion: "跨内容渠道的低层规则语法仍需升格为正式玩法权威。",
    evidenceSummary: "供应、读取、兑现、桥接、稀有度、职责和投资是互相独立的构筑轴。",
    evidence: [
      ev("ev-survivor-mercs-004-trait-weapon-gear-owner-split", "Survivor Mercs", "effect-payoff-ownership"),
      ev("ev-gods-vs-horrors-004-mythology-trait-architecture", "Gods vs Horrors", "trait-faction-build-architecture"),
      ev("ev-slot-005-promotion-rarity-role", "Slotbound", "unit-promotion-class-role-rarity"),
      ev("ev-loot-loop-003-fixed-four-role-ownership", "Loot Loop", "fixed-roster-role-payoff-ownership"),
    ],
  }),
  node({
    id: "f15", code: "F15", label: "经济、招募、奖励与转型", group: "composition", status: "open", x: 805, y: 125,
    summary: "用有限机会在即时强度、未来组合、替换和横纵成长之间定价。",
    responsibility: "规定内容如何来到玩家手里，以及失败构筑如何在剩余路程内修复或转型。",
    inputs: ["货币与账本", "选项池", "队伍与容量", "当前构筑", "剩余路线", "风险压力"],
    outputs: ["获取或替换交易", "改变后的构筑", "转型窗口", "机会成本"],
    channels: ["招募", "商店", "奖励", "事件与休整", "人口", "装备与遗物"],
    openQuestion: "货币、价格、层级概率、储备区、替换价值和纠偏预算均未确定。",
    evidenceSummary: "储蓄与花费、横向与纵向投资、替换和多种账本都需要显式代价。",
    evidence: [
      ev("ev-tft-001-economy-tempo", "Teamfight Tactics", "economy-recruitment"),
      ev("ev-skull-horde-002-standard-roster-economy", "Skull Horde", "recruitment-upgrade-replacement-economy"),
      ev("ev-dwarves-glory-death-loot-016-forge-recruit-growth-economy", "Dwarves: Glory, Death and Loot", "multi-budget-roster-progression"),
    ],
  }),
  node({
    id: "f16", code: "F16", label: "敌人、Boss、环境与反制契约", group: "composition", status: "derived", x: 1105, y: 125,
    summary: "让敌人与环境检验构筑链，同时保留预告、答案宽度和适应窗口。",
    responsibility: "规定对抗内容能挑战哪些构筑环节、如何预告、可用答案有多宽，以及失败如何归因。",
    inputs: ["局内阶段", "遭遇包", "环境规则", "玩家构筑", "可达答案", "预告政策"],
    outputs: ["威胁与反制合同", "遭遇设置", "适应要求", "公平失败解释"],
    channels: ["敌人", "精英", "Boss阶段", "危险格", "目标与装置", "路线预览"],
    openQuestion: "威胁分类、反制宽度、答案保障与Boss升级曲线仍待定义。",
    evidenceSummary: "对抗应检验不同链路、展示可行动意图，并避免只有唯一答案的封锁。",
    evidence: [
      ev("ev-dwarves-glory-death-loot-014-boss-counter-packages", "Dwarves: Glory, Death and Loot", "enemy-package-adaptation"),
      ev("ev-shf-015-naga-specific-answer-nerf", "ShapeHero Factory", "boss-specific-answer-lockout-rework"),
      ev("ev-backpack-hero-023-readable-enemy-counter-packages", "Backpack Hero", "enemy-package-adaptation"),
      ev("ev-monster-train-021-last-divinity-three-floor-exam", "Monster Train", "multi-lane-final-exam"),
    ],
  }),
  node({
    id: "f17", code: "F17", label: "归因、解释与反馈", group: "feedback", status: "accepted", x: 1335, y: 620,
    summary: "让选择、传播、有效结果和失败原因在所有界面上讲同一条因果链。",
    responsibility: "从来源、拥有者、目标、有效变化一路解释到兑现者、反制缺口和终局结算。",
    inputs: ["合法性评估", "不可变事件与结果", "因果链", "预览事实", "当前选择"],
    outputs: ["直接与间接影响", "中文原因", "贡献事实", "成功失败反馈", "下一问题"],
    channels: ["所有玩家界面", "战斗表现与音效", "战报", "玩法模型实验室"],
    openQuestion: "设计期因果解释的词汇和粒度仍需通过Web原型检验。",
    evidenceSummary: "自动战斗只有在准备、目标、节奏、事件祖先、表现和战报一致时，才会形成可行动反馈。",
    evidence: [
      ev("ev-pmm-001-observe-refine-repeat-loop", "Private Military Manager", "management-combat-feedback-loop"),
      ev("ev-survivor-mercs-018-merc-resolution-attribution-lifecycle", "Survivor Mercs", "automatic-combat-resolution-attribution"),
      ev("ev-storybook-brawl-018-trigger-readability-recursion", "Storybook Brawl", "event-order-recursion-reporting"),
    ],
  }),
  node({
    id: "g01", code: "G01", label: "确定性执行", group: "guardrail", status: "guardrail", x: 250, y: 910,
    summary: "固定步进、稳定顺序、不可变结果与可复现摘要。",
    responsibility: "保证已经选择的玩法规则可以稳定重放和验证。",
    inputs: ["种子", "规则图", "行动序列"], outputs: ["可复现结果"], channels: ["战斗", "随机生成", "结算"],
    openQuestion: "这是工程可信度边界，不作为玩家构筑规则。", evidenceSummary: "由现有系统权威直接约束。", evidence: [],
  }),
  node({
    id: "g02", code: "G02", label: "数据校验", group: "guardrail", status: "guardrail", x: 585, y: 910,
    summary: "在进入游戏前拒绝断链、缺引用、冲突、非法循环和不完整内容。",
    responsibility: "保护内容完整性，而不是在运行时猜测修复。",
    inputs: ["内容定义", "关系图", "目录"], outputs: ["已发布包或结构化拒绝"], channels: ["全部内容渠道"],
    openQuestion: "校验规则随正式模型确认后落地。", evidenceSummary: "由现有系统权威直接约束。", evidence: [],
  }),
  node({
    id: "g03", code: "G03", label: "持久化与迁移", group: "guardrail", status: "guardrail", x: 920, y: 910,
    summary: "稳定ID、一次结算、失败回滚以及无损迁移或明确拒绝。",
    responsibility: "保护局内状态跨页面和版本变化后的完整性。",
    inputs: ["状态DTO", "版本", "结算"], outputs: ["已验证保存或回滚"], channels: ["局内循环", "经济交易", "设置"],
    openQuestion: "这是工程表达，不定义玩家看到的经济。", evidenceSummary: "由现有系统权威直接约束。", evidence: [],
  }),
  node({
    id: "g04", code: "G04", label: "作用域隔离与清理", group: "guardrail", status: "guardrail", x: 1255, y: 910,
    summary: "共享定义不可变，局内与战斗状态不串写，结束后幂等清理。",
    responsibility: "阻止跨作用域泄漏和被污染的战斗结果。",
    inputs: ["作用域", "拥有者", "生命周期"], outputs: ["独立状态与完整清理"], channels: ["所有运行时内容"],
    openQuestion: "这是工程边界，不新增玩家机制。", evidenceSummary: "由现有系统权威直接约束。", evidence: [],
  }),
];

const gameplayEdges: Array<[string, string, RelationKind, string]> = [
  ["f01", "f02", "feeds", "进入一局"], ["f02", "f05", "feeds", "暴露下一决策"], ["f02", "f06", "feeds", "限定剩余可达性"],
  ["f06", "f15", "feeds", "产生可获取选项"], ["f15", "f14", "feeds", "改变构筑材料"], ["f14", "f03", "feeds", "形成参战配置"],
  ["f16", "f05", "feeds", "提供威胁预告"], ["f16", "f03", "feeds", "形成遭遇规则"], ["f05", "f04", "feeds", "开启有信息的决策"],
  ["f04", "f10", "feeds", "提交行动"], ["f07", "f10", "feeds", "提供行动主体"], ["f08", "f10", "feeds", "开放触发时机"],
  ["f09", "f10", "feeds", "提供合法目标"], ["f11", "f10", "feeds", "提供费用与条件"], ["f10", "f12", "feeds", "提交效果计划"],
  ["f11", "f12", "feeds", "提供当前状态"], ["f13", "f11", "feeds", "决定聚合投影"], ["f13", "f12", "feeds", "约束转换与连锁"],
  ["f12", "f03", "feeds", "形成胜负事实"], ["f03", "f17", "feeds", "输出战斗结果"], ["f05", "f17", "feeds", "输出已知与未知"],
  ["f14", "f17", "feeds", "输出构筑链"], ["f15", "f17", "feeds", "输出机会成本"], ["f17", "f02", "feedback", "解释后续决策"],
  ["f17", "f04", "feedback", "修正准备行动"],
];

const guardrailTargets: Record<string, string[]> = {
  g01: ["f03", "f06", "f08", "f12", "f13", "f17"],
  g02: ["f06", "f07", "f10", "f11", "f12", "f13", "f14", "f15", "f16"],
  g03: ["f01", "f02", "f04", "f10", "f15"],
  g04: ["f03", "f07", "f08", "f11", "f12", "f13", "f17"],
};

const authoredEdges: FoundationEdge[] = [
  ...gameplayEdges.map(([from, to, relation, label], index) => ({ id: `edge-${index + 1}`, from, to, relation, label })),
  ...Object.entries(guardrailTargets).flatMap(([from, targets]) => targets.map((to) => ({ id: `guard-${from}-${to}`, from, to, relation: "constrains" as const, label: "保护" }))),
];

const authoredZones: FoundationZone[] = [
  { id: "zone-loop", label: "循环与终局", summary: "从进入一局到战斗结算", group: "loop", x: 25, y: 45, width: 315, height: 390 },
  { id: "zone-decision", label: "决策条件", summary: "权限、信息与可达选择", group: "decision", x: 365, y: 45, width: 350, height: 390 },
  { id: "zone-composition", label: "构筑与对抗", summary: "材料如何获得、组合并接受检验", group: "composition", x: 740, y: 45, width: 455, height: 390 },
  { id: "zone-simulation", label: "战斗规则", summary: "实体、时间、空间、行动与结算", group: "simulation", x: 365, y: 460, width: 830, height: 390 },
  { id: "zone-feedback", label: "反馈闭环", summary: "把结果变成下一次可行动信息", group: "feedback", x: 1220, y: 460, width: 250, height: 390 },
  { id: "zone-guardrail", label: "外围工程护栏", summary: "保证玩法规则可信，但不冒充玩法", group: "guardrail", x: 25, y: 870, width: 1445, height: 85 },
];

export const GRAMMAR_TERMS = [
  ["作用域", "规则在哪段生命周期有效，不能无过渡跨层写入。"],
  ["载体", "规则通过哪个内容渠道进入游戏，不自动等于运行时拥有者。"],
  ["归属者 / 权限", "谁拥有可变状态，谁有权选择或提交。"],
  ["可见性", "承诺前哪些事实可预览、可检查、不确定或隐藏。"],
  ["触发 / 决策窗口", "什么事实启动结算，什么阶段允许玩家输入。"],
  ["条件", "合法性、资格、时机、空间与状态谓词。"],
  ["目标", "实体、队伍、格子、状态、选项或集合。"],
  ["状态 / 资源", "被积累、维持、读取或改变的具名事实。"],
  ["读取者 / 读取模式", "读取哪个来源、快照或最终值；读取不代表消耗。"],
  ["操作 / 量值", "成功后做什么，以及采用何种数值和计数口径。"],
  ["消耗规则 / 限制", "何时消耗多少，以及费用、冷却、次数、上限和因果深度。"],
  ["生命周期 / 重置", "激活、持续、刷新、移除、死亡、结算与清理。"],
  ["归因", "来源、拥有者、目标与有效结果的可追溯链。"],
  ["兑现 / 兑现者", "最终胜利相关结果，以及真正拥有该结果的对象。"],
] as const;

export const CONTENT_CHANNELS = [
  ["局外与局内配置", "入口、解锁、难度、起始规则、路线、内容池与局内上限。", ["f01", "f02", "f05", "f06", "f15"]],
  ["路线与奖励", "招募、商店、奖励、事件与休整中的有限决策。", ["f02", "f05", "f06", "f14", "f15", "f17"]],
  ["角色与角色规则", "持续身份投影为战斗实体、属性、行为、能力和特质。", ["f07", "f08", "f09", "f10", "f11", "f12", "f14", "f17"]],
  ["能力与自动行动", "带触发、目标、费用、限制、效果和归因的行动。", ["f04", "f08", "f09", "f10", "f11", "f12", "f13", "f17"]],
  ["状态、修正与特质", "有来源的状态、属性投影、标签、聚合与生命周期。", ["f07", "f08", "f11", "f12", "f13", "f17"]],
  ["装备", "附着于一名持续英雄并只通过该拥有者投影。", ["f07", "f10", "f11", "f12", "f13", "f14", "f15", "f17"]],
  ["遗物", "局内级实例投影到队伍或订阅战斗事实。", ["f07", "f08", "f11", "f12", "f13", "f14", "f15", "f17"]],
  ["战术指令", "玩家拥有的局内配置投影为战斗本地资源和事务行动。", ["f04", "f05", "f08", "f09", "f10", "f11", "f12", "f13", "f17"]],
  ["敌人与楼层规则", "可预览的敌人、Boss、环境、目标、阶段与反制包。", ["f03", "f05", "f06", "f09", "f12", "f16", "f17"]],
  ["临时单位来源", "由明确载体创建的可选战斗实体。", ["f03", "f07", "f08", "f09", "f10", "f11", "f12", "f13", "f17"]],
] as const;

export function createFoundationWorkspace(): FoundationWorkspace {
  return structuredClone({ nodes: authoredNodes, edges: authoredEdges, zones: authoredZones });
}

export function createCustomNode(sequence: number, x = 760, y = 320): FoundationNode {
  return {
    id: `custom-${Date.now()}-${sequence}`,
    code: `N${String(sequence).padStart(2, "0")}`,
    label: "新的基础规则",
    group: "decision",
    status: "open",
    x,
    y,
    summary: "说明它解决哪个全局玩法问题。",
    responsibility: "定义这一规则域唯一负责的事情。",
    inputs: ["待定义输入"],
    outputs: ["待定义输出"],
    channels: ["待确认内容渠道"],
    openQuestion: "为什么它不能被已有基础域承担？",
    evidenceSummary: "尚未绑定调研证据。",
    evidence: [],
    custom: true,
  };
}

export function deriveDiagnostics(workspace: FoundationWorkspace): Diagnostic[] {
  const diagnostics: Diagnostic[] = [];
  const ids = new Set(workspace.nodes.map((item) => item.id));
  const required = Array.from({ length: 17 }, (_, index) => `f${String(index + 1).padStart(2, "0")}`);
  const missing = required.filter((id) => !ids.has(id));
  if (missing.length) diagnostics.push({ id: "missing-foundation", severity: "error", title: "基础规则缺失", detail: `缺少 ${missing.map((id) => id.toUpperCase()).join("、")}；影响链不再代表完整游戏。` });

  const brokenEdges = workspace.edges.filter((edge) => !ids.has(edge.from) || !ids.has(edge.to) || edge.from === edge.to);
  if (brokenEdges.length) diagnostics.push({ id: "broken-edges", severity: "error", title: "关系无法解析", detail: `${brokenEdges.length} 条关系指向不存在节点或自身。` });

  const duplicates = workspace.edges.filter((edge, index, all) => all.findIndex((item) => item.from === edge.from && item.to === edge.to && item.relation === edge.relation) !== index);
  if (duplicates.length) diagnostics.push({ id: "duplicate-edges", severity: "warning", title: "重复关系", detail: `${duplicates.length} 条关系重复表达同一方向。` });

  const gameplayNodes = workspace.nodes.filter((item) => item.group !== "guardrail");
  const disconnected = gameplayNodes.filter((item) => !workspace.edges.some((edge) => edge.from === item.id || edge.to === item.id));
  if (disconnected.length) diagnostics.push({ id: "disconnected", severity: "warning", title: "孤立基础域", detail: `${disconnected.map((item) => item.code).join("、")} 尚未进入任何因果链。`, nodeId: disconnected[0].id });

  const ungrounded = gameplayNodes.filter((item) => item.evidence.length < 2);
  if (ungrounded.length) diagnostics.push({ id: "ungrounded", severity: "warning", title: "证据覆盖不足", detail: `${ungrounded.map((item) => item.code).join("、")} 少于两条跨游戏证据。`, nodeId: ungrounded[0].id });

  const badGuardrails = workspace.edges.filter((edge) => workspace.nodes.find((item) => item.id === edge.from)?.group === "guardrail" && edge.relation !== "constrains");
  if (badGuardrails.length) diagnostics.push({ id: "guardrail-leak", severity: "error", title: "工程护栏混入玩法", detail: "工程护栏只能约束玩法，不能成为奖励、资源或构筑输出。" });

  if (!diagnostics.length) diagnostics.push({ id: "complete", severity: "info", title: "V2 基础图完整", detail: "17 个玩法域、4 个工程护栏与证据链均已进入关系网。" });
  return diagnostics;
}
