export type DecisionState = "unreviewed" | "interested" | "candidate" | "confirmed" | "deferred" | "excluded";
export type Agency = "prepare" | "automatic" | "command" | "movement";
export type Complexity = "low" | "medium" | "high";
export type Dependency = "baseline" | "special-map" | "movement-intervention";

export type Domain = {
  id: string;
  index: string;
  title: string;
  question: string;
  summary: string;
  color: string;
};

export type Mechanic = {
  id: string;
  title: string;
  domainId: string;
  subdimension: string;
  premise: string;
  rule: string;
  decision: string;
  pleasure: string;
  consequences: { space: string; time: string; resource: string; build: string };
  synergies: string[];
  counters: string[];
  dependencies: string[];
  risks: string;
  readability: string;
  implementation: string;
  suitable: string[];
  dependency: Dependency;
  agency: Agency;
  complexity: Complexity;
  depthTypes: Array<"space" | "time" | "resource" | "build" | "information">;
};

export const domains: Domain[] = [
  { id: "battlefield", index: "01", title: "战场空间", question: "单位在空白棋盘上如何互相塑造地形？", summary: "占位、战线、射程与攻击形状，让单位本身成为动态地图。", color: "#d39b59" },
  { id: "combat", index: "02", title: "战斗系统", question: "伤害、保护与资源如何形成节奏和循环？", summary: "从攻防结算到施法、控制、召唤和死亡引擎。", color: "#6fa9a5" },
  { id: "army", index: "03", title: "军团构筑", question: "一组单位为什么会成为一种战术，而不是属性之和？", summary: "角色、阵型、编制、标签、后备与转型共同定义军团。", color: "#a987c7" },
  { id: "hero-relic", index: "04", title: "英雄·技能·遗物", question: "哪些组件能把普通规则组装成一台战斗引擎？", summary: "固定身份、技能池、有限指令与规则改变型遗物。", color: "#d6b95f" },
  { id: "encounter", index: "05", title: "敌人与 Boss", question: "一场战斗在检验什么，又允许哪些不同答案？", summary: "遭遇模板、阶段时间线、能力试卷与公平反制。", color: "#d8786f" },
  { id: "run", index: "06", title: "单局与重玩", question: "战斗机制如何转化为一整局持续发生的取舍？", summary: "招募、经济、路线、成型节奏、转型与难度梯度。", color: "#72a8d4" },
  { id: "agency", index: "07", title: "玩家干预与扩展", question: "战前、自动与战中操作应如何分配责任？", summary: "以纯布阵为基线，清楚标记移动与特殊地图的扩展成本。", color: "#75b77b" },
  { id: "safety", index: "08", title: "可读性·平衡·模拟安全", question: "深度怎样保持可解释、公平且不会让模拟失控？", summary: "反馈词汇、反制公平、确定性、循环限流与战报解释。", color: "#8d9aae" },
];

type Seed = [
  id: string, title: string, domainId: string, subdimension: string,
  premise: string, rule: string, decision: string, pleasure: string,
  agency: Agency, complexity: Complexity, dependency: Dependency,
  depthTypes: Mechanic["depthTypes"]
];

const seeds: Seed[] = [
  ["battlefield.occupancy.body-block", "身体阻挡", "battlefield", "占位与通路", "每个活单位都是一格临时墙。", "移动路径不能穿过敌方；友军仅能通过同批次合法跟进链进入腾空格。", "用谁卡住接敌口，是否值得牺牲输出密度？", "看见阵型真的改变接敌顺序。", "prepare", "medium", "baseline", ["space", "information"]],
  ["battlefield.formation.guard-pocket", "护卫口袋", "battlefield", "队形与邻接", "盾兵围出一个安全输出口袋。", "被护卫的相邻远程受到近战锁定时，盾兵优先截击一次；口袋过密会放大范围伤害。", "保护核心还是主动展开以规避范围技？", "用几格站位换来清晰的保护关系。", "prepare", "medium", "baseline", ["space", "build"]],
  ["battlefield.engagement.charge", "冲阵", "battlefield", "接敌与战线", "用第一次接敌的速度和冲量破坏前后排。", "连续移动若干格后首次攻击附带推挤或短暂失衡；被截击会提前终止蓄势。", "直冲核心、拆侧翼，还是保存冲势？", "阵线在第一轮碰撞中被重新书写。", "automatic", "medium", "baseline", ["space", "time"]],
  ["battlefield.engagement.intercept", "截击", "battlefield", "接敌与战线", "守军把敌人的移动意图变成可防守窗口。", "敌人进入相邻威胁区时，满足条件的守卫消耗截击次数令其停步并成为临时目标。", "截击次数留给冲锋者还是杂兵？", "提前预判并拦住关键突破者。", "automatic", "high", "baseline", ["space", "resource"]],
  ["battlefield.range.minimum-range", "最小射程", "battlefield", "射程与距离", "强远程在贴身时失去最佳攻击条件。", "射手无法攻击小于最小距离的目标，或只能使用显著更弱的近身动作。", "用护卫维持射击走廊，还是接受近身短板？", "让贴脸与保距都成为真实战术。", "automatic", "low", "baseline", ["space", "build"]],
  ["battlefield.shape.line-pierce", "直线穿透", "battlefield", "攻击形状", "一条弹道同时奖励对齐与侧射。", "攻击沿目标方向穿过固定格数，依次命中同线单位并逐次衰减。", "从正面贯穿还是调整部署寻找纵列？", "一次正确角度击穿整条队列。", "prepare", "medium", "baseline", ["space", "build"]],
  ["battlefield.shape.cone", "扇形压制", "battlefield", "攻击形状", "近中距离展开面越宽，收益越高。", "以施法者朝向覆盖逐层扩张的格子；近端伤害高，远端控制范围大。", "把法师放在安全后排还是靠前换覆盖？", "把敌群聚集变成可利用的几何机会。", "automatic", "medium", "baseline", ["space", "time"]],
  ["battlefield.targeting.threat-lock", "威胁锁定", "battlefield", "目标与仇恨", "目标选择可被构筑，而非永远最近。", "造成爆发、治疗或护卫会积累威胁；单位在阈值差未超过滞回区时不频繁换目标。", "谁承担威胁，何时让核心暴露？", "用行为主动塑造敌方火力流向。", "automatic", "high", "baseline", ["information", "build"]],
  ["battlefield.displacement.swap", "战术换位", "battlefield", "位移", "位置能在关键时刻作为一次性资源。", "满足邻接条件的友军在受击或施法前交换格位；直接交换必须由明确效果授权。", "救核心、送前排，还是保持阵型增益？", "在自动战斗中出现可读的救援瞬间。", "command", "high", "baseline", ["space", "time", "resource"]],

  ["combat.defense.armor", "分段护甲", "combat", "防御结算", "护甲擅长处理多段小伤害，而非全局百分比减伤。", "每次物理命中先减固定护甲值，并设最低有效伤害；破甲削减的是当前护甲层。", "堆多段输出还是用重击跨过护甲？", "不同攻击频率自然形成克制差异。", "automatic", "low", "baseline", ["resource", "build"]],
  ["combat.defense.shield", "可刷新护盾", "combat", "防御结算", "护盾是一层可被生成、消耗和转化的战斗资源。", "伤害先扣护盾；来源、获得量、损失量和破裂事件分别记录，刷新不得复活。", "追求总盾量、刷新频率还是破盾触发？", "把防御做成看得见的引擎燃料。", "automatic", "medium", "baseline", ["resource", "time", "build"]],
  ["combat.defense.guard", "护卫分摊", "combat", "防御结算", "保护者替核心承担部分实际伤害。", "相邻受护卫单位受击时，按上限转移一部分未结算伤害；护卫者阵亡后链路终止。", "保护一个核心，还是分散护卫降低猝死？", "前排的牺牲直接转化为后排生存。", "automatic", "medium", "baseline", ["space", "resource"]],
  ["combat.damage.execute", "斩杀阈值", "combat", "伤害类型", "低生命目标进入必须处理的危险区。", "命中低于阈值的目标追加处决伤害；阈值按最大生命计算并明确预告。", "靠治疗跨出阈值，还是竞速击杀处刑者？", "制造紧张但清楚的终结窗口。", "automatic", "low", "baseline", ["time", "resource"]],
  ["combat.damage.armor-break", "破甲层数", "combat", "标记与层数", "持续压制逐步打开重甲目标。", "命中施加有上限且会衰减的破甲层，每层削减护甲；重击可一次消费层数爆发。", "持续维持易伤还是消费层数收尾？", "围绕一个目标建立明确的集火节奏。", "automatic", "medium", "baseline", ["time", "resource", "build"]],
  ["combat.casting.interrupt", "蓄力与打断", "combat", "施法与冷却", "强技能先暴露一个可反应的时间窗口。", "施法条结束才生效；位移、眩晕或专用打断会取消，并按规则返还部分冷却。", "投入控制打断，还是用护盾硬吃后反击？", "预告—应对—结果形成清楚节拍。", "command", "medium", "baseline", ["time", "information"]],
  ["combat.status.slow", "减速与节拍错位", "combat", "状态效果", "减速不仅降低数值，也能打散敌方同步攻击。", "分别影响移动或行动周期，不回滚已经完成的进度；叠加采用上限与递减。", "减速前排拖延接敌，还是减速后排延后齐射？", "通过时间控制让阵容逐个解决威胁。", "automatic", "medium", "baseline", ["time", "space"]],
  ["combat.summon.corpse", "尸体资源", "combat", "召唤与死亡", "死亡留下可以消费、争夺或污染的战场资源。", "终结单位生成带归属的尸体标记；复生、爆炸或吞噬会消费它，重复消费非法。", "保存尸体构筑后续收益，还是立刻爆破清场？", "让伤亡成为另一条资源曲线。", "automatic", "high", "baseline", ["space", "resource", "build"]],
  ["combat.trigger.overheal", "过量治疗转化", "combat", "治疗与转化", "满血时的治疗可以经专用规则转成有限收益。", "仅明确授权的效果可把过量治疗转为护盾、充能或伤害，转换率与单次上限可见。", "保持满血刷资源，还是把治疗留给真实伤势？", "把原本浪费的治疗变成构筑拼图。", "automatic", "high", "baseline", ["resource", "build"]],

  ["army.role.shield-line", "盾墙编组", "army", "功能角色", "多名防御单位通过相邻关系形成连续前线。", "相邻盾职共享部分护甲或截击，但断开连接会失去连锁增益。", "追求长墙覆盖，还是缩成保护核心的短墙？", "阵型轮廓直接表达军团意图。", "prepare", "medium", "baseline", ["space", "build"]],
  ["army.role.breakthrough", "突破队", "army", "功能角色", "冲锋、位移与追击组合成后排破坏包。", "至少一个开路单位制造缺口，后续单位优先沿同一接敌线追击被标记目标。", "集中一路迅速打穿，还是双翼分压？", "看见预先设计的突破链自动执行。", "prepare", "high", "baseline", ["space", "build", "time"]],
  ["army.role.artillery", "远程穿透阵", "army", "功能角色", "远程单位依赖走廊、对齐和前排稳固。", "穿透射手对纵列增益，标记者固定目标，护卫维持最小射程。", "围绕一条黄金射线布阵，还是分散以防突袭？", "几种单纯职责组合成精密火力线。", "prepare", "medium", "baseline", ["space", "build"]],
  ["army.role.aoe-casters", "法术清场阵", "army", "功能角色", "聚怪、蓄力与范围法术形成清场链。", "前排或召唤物聚拢敌军，法师在预告窗口完成圆形或扇形技能。", "用更多控场保证命中，还是堆伤害缩短施法次数？", "一次覆盖正确人群的爆发回报铺垫。", "prepare", "medium", "baseline", ["space", "time", "build"]],
  ["army.shape.elite", "少量精锐", "army", "编制形状", "空位与升级资源集中换取单体质量。", "仅由英雄或遗物授权时，空部署位提供精锐加成；普通英雄空位没有补偿。", "放弃功能覆盖换取主力强度是否值得？", "少数单位也能形成完整而不同的军团语言。", "prepare", "medium", "baseline", ["resource", "build"]],
  ["army.shape.swarm", "人海消耗", "army", "编制形状", "数量承担占位、承伤和死亡触发。", "低成本单位与临时召唤填充战线；永久编制上限和召唤容量分别管理。", "数量用于拖延、包围，还是主动献祭？", "战场秩序被自己组装的浪潮改变。", "prepare", "high", "baseline", ["space", "resource", "build"]],
  ["army.tags.cross-faction", "跨阵营小包", "army", "标签与协同", "标签提供拼图，不把阵容锁死为纯阵营。", "强协同由两到三件不同职责组成；纯阵营奖励只做方向提示而非硬门槛。", "为关键功能跨阵营，还是追求标签密度？", "发现出乎意料但逻辑清楚的混编组合。", "prepare", "medium", "baseline", ["build", "information"]],
  ["army.reserve.sideboard", "后备军备牌", "army", "后备与转型", "三个后备位是针对已知 Boss 的战术答案库。", "战斗前可把范围、破盾、护卫等专职单位换入六人部署，换出者保留成长与伤势。", "常备通用单位，还是押注区域终点的专用答案？", "预判 Boss 后通过换阵解决问题。", "prepare", "low", "baseline", ["build", "information"]],
  ["army.recovery.replacement", "带伤替换", "army", "伤亡与恢复", "损失会改变下一战阵容，但不立即判死整局。", "伤员可休整、替换或承担风险；永久阵亡的空缺通过后续招募补回。", "保护高成长伤员，还是让其继续上场维持体系？", "把战斗结果延伸成下一节点的真实决策。", "prepare", "medium", "baseline", ["resource", "build"]],

  ["hero-relic.identity.army-rule", "固定军团规则", "hero-relic", "英雄身份", "英雄首先改变军团构筑语法，而不是只多一个大招。", "每名英雄拥有一条始终公开的军团规则，负责授权空位、尸体、构装或经济等特殊转化。", "围绕身份走深，还是用通用单位补短板？", "同一内容池因英雄而产生不同价值。", "prepare", "medium", "baseline", ["build", "resource"]],
  ["hero-relic.skill.pool", "局内自动技能池", "hero-relic", "技能成长", "同一英雄每局可形成不同自动战斗引擎。", "技能池按主题分支出现，获得后由战斗条件自动触发；重复选择升级规则而非只加数值。", "强化已有循环，还是拿新触发扩大适配面？", "逐步组装属于本局的英雄版本。", "prepare", "high", "baseline", ["build", "time"]],
  ["hero-relic.command.bounded", "有限英雄指令", "hero-relic", "战场指令", "每战三次左右的指令是启动器和救场阀，而非持续微操。", "指令消耗不恢复的战斗法力；失败不扣资源，效果、成本与失败原因始终可见。", "提前启动引擎、抢 Boss 窗口，还是保留救场？", "少量点击产生高价值的时机判断。", "command", "medium", "baseline", ["time", "resource", "information"]],
  ["hero-relic.relic.trigger-rewrite", "触发器改写遗物", "hero-relic", "遗物类型", "遗物改变技能何时发生，而不是只加伤害。", "把“每隔若干秒”改为“破盾时”或“友军阵亡时”，并保留显式内置冷却。", "选择更高频但危险的触发，还是稳定周期？", "用一个组件重构整条战斗节奏。", "prepare", "high", "baseline", ["time", "build"]],
  ["hero-relic.relic.conversion", "资源转换遗物", "hero-relic", "遗物类型", "护盾、治疗、尸体、金币或层数能跨系统流动。", "以公开比例把一种已结算资源转换为另一种；转换事件携带来源与链路深度。", "提高转换效率还是先增加原料产量？", "像搭机器一样闭合资源循环。", "automatic", "high", "baseline", ["resource", "build"]],
  ["hero-relic.relic.spatial", "空间规则遗物", "hero-relic", "遗物类型", "遗物让阵型条件产生新的收益或代价。", "例如首列获得护甲、孤立单位加射程、同排技能串联；只读取权威格位。", "围绕遗物重排全军，是否暴露新的弱点？", "一件遗物让熟悉棋盘出现新解法。", "prepare", "medium", "baseline", ["space", "build"]],
  ["hero-relic.relic.risk", "风险收益遗物", "hero-relic", "遗物类型", "强大效果附带能被构筑管理的明确代价。", "例如开战损失生命换充能、施法增加易伤、金币越多治疗越弱。", "当前体系能否消化副作用？", "主动驯服危险而获得超规格回报。", "prepare", "medium", "baseline", ["resource", "build"]],
  ["hero-relic.relic.loop-core", "循环核心遗物", "hero-relic", "引擎组件", "传奇组件允许技能与资源形成近似无限循环。", "满足启动条件后，输出事件反馈为下一次触发资源；每链路有最小间隔、深度与单步次数边界。", "为完整循环牺牲多少即时战力？", "构筑成型后看见自己造出的机器运转。", "automatic", "high", "baseline", ["time", "resource", "build"]],
  ["hero-relic.upgrade.branch", "技能分支升级", "hero-relic", "技能成长", "升级改变用途而非线性加百分比。", "同一技能在范围、频率、资源回收或单体爆发中选择互斥分支，可在预览中比较。", "专精当前答案，还是保留对未知遭遇的泛用性？", "一次升级明确改变后续选牌价值。", "prepare", "medium", "baseline", ["build", "information"]],

  ["encounter.template.frontline-artillery", "重墙与炮列", "encounter", "遭遇模板", "厚前排为后排火力争取稳定输出时间。", "前卫占据接敌面，炮手锁定脆弱单位；模板内只随机具体单位和一处站位。", "穿透前排、突破后排，还是用 sustain 竞速？", "预览后能读出问题并选择多个答案。", "prepare", "medium", "baseline", ["space", "build", "information"]],
  ["encounter.template.swarm", "巢群增殖", "encounter", "遭遇模板", "小怪数量会随时间失控，迫使玩家处理规模。", "母体按固定波次召唤低生命单位；召唤容量、波次与超时结果提前公开。", "AOE 清场、穿透收割、死亡利用，还是斩首母体？", "不同流派以不同方式解决同一道题。", "prepare", "medium", "baseline", ["time", "space", "build"]],
  ["encounter.template.backline-hunter", "后排猎手", "encounter", "遭遇模板", "敌人绕过普通最近目标检验核心保护。", "猎手在预告后跃迁到最远或最低护甲目标附近，落点遵守占位并可被诱饵影响。", "护卫、诱饵、分散，还是提前击杀？", "让后排安全成为需要主动构筑的能力。", "prepare", "high", "baseline", ["space", "information"]],
  ["encounter.elite.modifier", "精英规则模块", "encounter", "精英强化", "精英通过一个改变解法的规则升级，而非单纯血攻倍率。", "从有限池附加如反伤层、周期护盾、死亡爆炸，并排除与模板硬锁死的组合。", "区域内是否已有针对该规则的补救？", "熟悉敌阵在单一变量下产生新判断。", "prepare", "medium", "baseline", ["build", "information"]],
  ["encounter.boss.timeline", "Boss 时间轴", "encounter", "Boss 阶段", "主要技能按可预判节拍出现，支持针对性指令时机。", "时间轴展示下一次大招、召唤与阶段变化；阶段可重排技能但不暗改规则。", "保指令打断大招，还是早用抢阶段？", "提前规划并在关键窗口执行成功。", "command", "medium", "baseline", ["time", "information"]],
  ["encounter.boss.adaptive-shield", "适应性护盾首领", "encounter", "能力试卷", "Boss 周期护盾检验破盾、持续输出或窗口爆发。", "护盾存在时积累一次反击能量；快速破盾、延后输出规避反击或穿盾斩首都可成立。", "用哪种资源曲线处理周期护盾？", "构筑能力在明确周期中被验证。", "prepare", "high", "baseline", ["time", "resource", "build"]],
  ["encounter.boss.sustain-check", "腐化医师", "encounter", "能力试卷", "治疗与复苏让无计划的平均输出失去进展。", "Boss 治疗最低生命友军，过量治疗生成可打断的复苏充能；处决、爆发、禁疗或控场皆可应对。", "优先打断治疗者还是集中处决目标？", "把“输出不足”拆成可理解的具体问题。", "prepare", "high", "baseline", ["time", "resource", "build"]],
  ["encounter.telegraph.answer-window", "区域答案窗口", "encounter", "预告与适配", "Boss 在区域开始就公开，路线中提供可识别的补洞机会。", "区域奖励至少多次出现针对核心压力的不同答案，但不保证指定单位或遗物。", "提前投资专用答案，还是赌现有构筑能硬解？", "失败来自判断而非终点随机处刑。", "prepare", "medium", "baseline", ["information", "build"]],
  ["encounter.failure.explanation", "失败归因", "encounter", "战报解释", "战报把失败落到可行动能力而非笼统战力。", "结合伤害、承伤、目标、技能时间轴和空间事件，提示可能缺口但不宣布唯一正确答案。", "下次应改阵容、站位、路线还是指令时机？", "失败能转化成下一局的知识。", "automatic", "high", "baseline", ["information"]],

  ["run.recruitment.opportunity-cost", "招募机会成本", "run", "招募与替换", "获得单位必须与金币、恢复或奖励机会竞争。", "节点选择、价格与有限库存共同约束招募；满员时可比较后替换而非直接失效。", "补功能、养核心，还是保经济？", "每次扩军都意味着放弃另一条成长线。", "prepare", "medium", "baseline", ["resource", "build"]],
  ["run.shop.finite-stock", "有限商店库存", "run", "经济", "商店是一次明确的资源分配，而非无限菜单。", "库存有数量、锁定与售罄状态；刷新价格递增，离开后结果固定保存。", "现在买关键组件，还是存钱等待高稀有度？", "金币真正形成可计算的机会成本。", "prepare", "low", "baseline", ["resource", "information"]],
  ["run.reward.weighted-pool", "构筑感知奖励池", "run", "奖励与稀有度", "奖励既能支持当前方向，也保留转型出口。", "区域、稀有度和已有标签调整权重，但设重复保护与异类候选位，永不直接保证核心。", "顺势走深，还是拿异类组件准备转型？", "随机性提供问题而非纯粹噪声。", "prepare", "high", "baseline", ["build", "information"]],
  ["run.route.foresight", "路线远见", "run", "路线信息", "玩家能看到足够远，才能为 Boss 形成计划。", "展示若干层节点类型、风险与终点压力；隐藏具体奖励但不隐藏决定性规则。", "安全补强还是冒险追关键组件？", "路线成为构筑规划而非盲选。", "prepare", "medium", "baseline", ["information", "resource"]],
  ["run.build.timing", "成型节奏", "run", "构筑阶段", "前期生存件、中期连接件、后期核心件价值不同。", "奖励池和难度按区域改变，循环核心不会在无启动组件时频繁出现。", "先拿即战力渡过前期，还是承担空窗押后期？", "一套构筑在整局中拥有成长叙事。", "prepare", "high", "baseline", ["time", "build"]],
  ["run.pivot.salvage", "转型回收", "run", "转型", "错误或被克制的方向可以付出代价后转向。", "拆解升级、出售遗物或替换核心返还部分资源；回收率限制无成本反复横跳。", "坚持未成型引擎，还是止损转向可通关答案？", "让中途判断比开局抽卡更重要。", "prepare", "medium", "baseline", ["resource", "build"]],
  ["run.casualty.recovery", "伤亡恢复节点", "run", "伤亡压力", "伤亡会消耗路线和经济，却不必自动判死。", "休息、医师、替补招募分别处理伤势、永久损失和阵容缺口，机会成本不同。", "修复旧体系还是借损失完成转型？", "把失败余波变成有张力的管理。", "prepare", "medium", "baseline", ["resource", "build"]],
  ["run.difficulty.modular", "模块化难度阶梯", "run", "难度与重玩", "高难度增加规则压力与信息题，不靠永久属性碾压。", "难度层依次修改经济、敌方模板、Boss 阶段或恢复资源；每层变化明确列出。", "当前构筑需要补哪种新短板？", "重复游玩要求重新理解系统。", "prepare", "medium", "baseline", ["build", "information"]],
  ["run.meta.horizontal", "横向局外解锁", "run", "局外成长", "解锁扩大选择空间，不用永久数值掩盖构筑问题。", "新英雄、单位、遗物与难度进入可控池；可查看其带来的新机制标签。", "扩大池子前是否理解已有机制？", "长期成长体现为更多可能性与挑战。", "prepare", "low", "baseline", ["build", "information"]],

  ["agency.baseline.prepare-only", "纯布阵基线", "agency", "操作层级", "所有主要战术问题原则上先提供战前答案。", "阵容、站位、后备与预览足以处理基础遭遇；战中不要求持续操作单位。", "用准备覆盖多少不确定性？", "看见计划自主运行并验证预判。", "prepare", "low", "baseline", ["space", "build"]],
  ["agency.command.window", "窗口型指令", "agency", "有限干预", "指令只在关键时点放大已有构筑。", "有限资源的指令用于启动、打断、保护或重置一次链路，不直接替代单位 AI。", "现在救场还是保留给 Boss 阶段？", "低频操作仍有清楚而高价值的掌控感。", "command", "medium", "baseline", ["time", "resource"]],
  ["agency.inspect.pause", "暂停与因果检查", "agency", "信息操作", "玩家能在不改变结果的前提下理解系统。", "暂停、倍速和单位检查不改变固定模拟步、确定性排序和冷却；显示当前行动原因。", "何时暂停学习，何时高速验证？", "复杂战斗仍然可以被读懂。", "command", "low", "baseline", ["information"]],
  ["agency.movement.nudge", "有限移动指令", "agency", "移动扩展", "未来可让玩家少量改变一个单位的目的地，而非全盘微操。", "消耗独立资源指定相邻或区域目标；路径、占位和攻击合法性仍由模拟决定。", "用稀缺移动修正哪一个局部失误？", "在不抛弃构筑核心的前提下增加临场表达。", "movement", "high", "movement-intervention", ["space", "time", "resource"]],
  ["agency.movement.rally", "集结点命令", "agency", "移动扩展", "对一组单位下达粗粒度意图，而非逐格控制。", "英雄放置短时集结区，符合标签的单位重新评估目标但不会无视即时攻击。", "重整战线还是追击残敌？", "以指挥官视角干预军团流向。", "movement", "high", "movement-intervention", ["space", "time"]],
  ["agency.map.chokepoint", "固定狭路地图", "agency", "特殊地图", "障碍物把战线宽度变成关卡参数。", "不可通行格与部署预览共同定义一到多个接敌口；敌我遵循同一规则。", "抢主路、守瓶颈还是绕侧翼？", "同一军团在不同几何下价值重排。", "prepare", "medium", "special-map", ["space", "build"]],
  ["agency.map.hazard-cell", "周期危险格", "agency", "特殊地图", "格子按预告节拍改变安全性。", "危险区在固定时间激活并明确标记；纯布阵版允许通过初始站位与阵容抗性应对。", "避开危险导致阵型破碎，还是硬吃换输出？", "静态部署也能与时间地图博弈。", "prepare", "high", "special-map", ["space", "time"]],
  ["agency.map.objective", "可控战场装置", "agency", "特殊地图", "占领或攻击装置能改变战斗规则。", "装置占据合法格，按控制进度提供团队效果；Boss 与玩家都有利用路径。", "分兵争夺，还是忽略装置全力斩首？", "地图目标制造第二条胜负轴。", "prepare", "high", "special-map", ["space", "resource", "build"]],
  ["agency.expansion.cost", "扩展依赖账本", "agency", "范围治理", "每个创意明确标记需要的操作、地图与 UI 成本。", "机制记录区分基线、特殊地图和移动干预；依赖未确认时不得悄悄进入基础规则。", "新增乐趣是否值得学习与实现成本？", "大设计空间仍能保持边界清楚。", "prepare", "low", "baseline", ["information"]],

  ["safety.readability.vocabulary", "统一机制词汇", "safety", "因果反馈", "同一机制在卡片、预览、战中与战报使用同一术语。", "护盾、破盾、截击、打断、穿透等拥有固定图文含义，不用相近措辞暗示不同规则。", "玩家能否只看关键词快速评估组合？", "知识可以跨单位和关卡复用。", "automatic", "low", "baseline", ["information"]],
  ["safety.readability.telegraph", "预告—窗口—结果", "safety", "因果反馈", "强机制必须有可识别的前兆和结果回执。", "Boss 技能、冲锋、斩杀和地图危险依次显示目标、倒计时、结算与来源。", "该用指令应对还是接受代价？", "失败时能指出自己错过了哪个窗口。", "automatic", "medium", "baseline", ["time", "information"]],
  ["safety.balance.soft-counter", "软克制优先", "safety", "反制公平", "受压不等于整套构筑失效。", "克制削弱效率、改变节奏或迫使换阵；只有提前充分预告且有补救时才使用硬禁用。", "补一个答案还是靠构筑强项硬过？", "保留表达空间与惊喜通关。", "prepare", "medium", "baseline", ["build", "information"]],
  ["safety.balance.multi-answer", "Boss 多解约束", "safety", "反制公平", "每个主要压力至少由三种机制家族回答。", "设计验收记录稳定解、风险解和绕题解；不得只检查某个内容 id。", "当前阵容用哪一类答案最划算？", "Boss 像试卷而不是钥匙孔。", "prepare", "medium", "baseline", ["build", "information"]],
  ["safety.simulation.order", "确定性触发顺序", "safety", "模拟安全", "同一时刻的效果有稳定、可测试的权威顺序。", "事件按模拟步、优先层、来源稳定 id 和运行时序号排序；表现不参与判定。", "复杂循环能否被复现和解释？", "相同准备得到可学习的相同结果。", "automatic", "high", "baseline", ["time", "information"]],
  ["safety.simulation.recursion", "同刻递归熔断", "safety", "模拟安全", "允许强循环，但不允许同一模拟步无限递归。", "事件携带链路 id 与深度；重复边、最大深度或单步次数超限时延后或终止并记战报。", "循环能否跨时间维持而不是卡死？", "无限构筑保留幻想，同时游戏永远可结算。", "automatic", "high", "baseline", ["time", "resource"]],
  ["safety.simulation.rate-limit", "触发频率上限", "safety", "模拟安全", "每个组件有可见的最小间隔或每步额度。", "频率限制属于机制数据并显示在详情；不靠隐藏全局冷却偷偷削弱组合。", "提高单次收益还是缩短触发间隔？", "循环强度可以理解、比较和调优。", "automatic", "medium", "baseline", ["time", "resource"]],
  ["safety.simulation.stalemate", "僵局终止", "safety", "胜负推进", "双重无限防御不能让战斗永久运行。", "超时前逐步公开狂暴规则，最终按明确目标或失败合同结算，不临时比较隐藏战力。", "构筑是否有实际获胜路径而非只不死？", "防御引擎仍需面对时间压力。", "automatic", "medium", "baseline", ["time", "build"]],
  ["safety.report.trigger-chain", "触发链战报", "safety", "战报解释", "玩家能从结果追溯关键资源循环。", "合并重复低价值事件，保留启动器、主要转换、被熔断点与最终贡献。", "真正的核心组件是哪一个？", "看到自己构筑为何成立或为何断链。", "automatic", "high", "baseline", ["information", "build"]],
];

const relationshipMap: Record<string, { synergies?: string[]; counters?: string[]; dependencies?: string[] }> = {
  "battlefield.formation.guard-pocket": { synergies: ["combat.defense.guard", "army.role.artillery"], counters: ["battlefield.shape.cone", "encounter.template.backline-hunter"] },
  "battlefield.engagement.charge": { synergies: ["army.role.breakthrough", "combat.status.slow"], counters: ["battlefield.engagement.intercept", "combat.casting.interrupt"] },
  "battlefield.shape.line-pierce": { synergies: ["army.role.artillery", "battlefield.targeting.threat-lock"], counters: ["army.role.shield-line"] },
  "combat.defense.shield": { synergies: ["hero-relic.relic.conversion", "hero-relic.relic.loop-core"], counters: ["encounter.boss.adaptive-shield", "combat.damage.execute"] },
  "combat.summon.corpse": { synergies: ["army.shape.swarm", "hero-relic.identity.army-rule", "hero-relic.relic.conversion"], counters: ["army.role.aoe-casters"] },
  "combat.trigger.overheal": { synergies: ["combat.defense.shield", "hero-relic.relic.conversion"], counters: ["encounter.boss.sustain-check"] },
  "army.role.shield-line": { synergies: ["battlefield.engagement.intercept", "combat.defense.guard", "army.role.artillery"], counters: ["battlefield.shape.line-pierce", "combat.damage.armor-break"] },
  "army.role.aoe-casters": { synergies: ["combat.casting.interrupt", "battlefield.shape.cone"], counters: ["encounter.template.swarm"] },
  "army.reserve.sideboard": { synergies: ["encounter.telegraph.answer-window", "run.route.foresight"], dependencies: ["run.recruitment.opportunity-cost"] },
  "hero-relic.relic.trigger-rewrite": { synergies: ["combat.defense.shield", "combat.summon.corpse", "hero-relic.relic.loop-core"], dependencies: ["safety.simulation.rate-limit"] },
  "hero-relic.relic.loop-core": { synergies: ["hero-relic.relic.conversion", "hero-relic.skill.pool"], dependencies: ["safety.simulation.order", "safety.simulation.recursion", "safety.report.trigger-chain"] },
  "encounter.template.swarm": { counters: ["army.role.aoe-casters", "battlefield.shape.line-pierce", "army.shape.swarm"] },
  "encounter.template.backline-hunter": { counters: ["battlefield.formation.guard-pocket", "combat.defense.guard", "army.role.breakthrough"] },
  "encounter.boss.adaptive-shield": { counters: ["combat.damage.armor-break", "army.role.artillery", "hero-relic.command.bounded"] },
  "run.reward.weighted-pool": { dependencies: ["hero-relic.identity.army-rule", "run.build.timing"], counters: ["run.pivot.salvage"] },
  "agency.movement.nudge": { dependencies: ["agency.expansion.cost", "safety.simulation.order"], synergies: ["agency.map.hazard-cell"] },
  "safety.balance.multi-answer": { dependencies: ["encounter.telegraph.answer-window", "encounter.failure.explanation"] },
  "safety.simulation.recursion": { dependencies: ["safety.simulation.order", "safety.simulation.rate-limit"], synergies: ["hero-relic.relic.loop-core"] },
};

const domainDefaults: Record<string, Pick<Mechanic, "risks" | "readability" | "implementation" | "suitable">> = {
  battlefield: { risks: "若目标、占位或朝向反馈不清，空间深度会退化为不可控噪声。", readability: "高亮影响格、目标线、接敌点与触发来源；颜色之外必须有形状或文字。", implementation: "只读取权威逻辑格与确定性移动结果，表现层不得反向修改战斗合法性。", suitable: ["士兵", "阵型", "敌人", "Boss"] },
  combat: { risks: "叠加顺序与边界若隐藏，会产生看似作弊的结果或指数膨胀。", readability: "数值变化需要来源、类型、层数、持续时间和关键阈值反馈。", implementation: "效果使用稳定事件顺序、来源 id、上限与同刻递归保护。", suitable: ["英雄", "士兵", "遗物", "Boss"] },
  army: { risks: "标签奖励过强会把开放构筑压成固定套装答案。", readability: "部署页同时展示职责、关键联动、保护关系与阵容能力缺口。", implementation: "组合由组件和标签贡献，不以具体单位 id 写中央分支。", suitable: ["英雄规则", "士兵", "后备军", "招募"] },
  "hero-relic": { risks: "强循环可能同质化英雄，或令最佳操作变成无脑尽早释放。", readability: "显示触发器、转换率、内置间隔、当前资源与链路预览。", implementation: "自动技能与手动指令分层；失败事务不消耗法力或金币。", suitable: ["英雄", "自动技能", "英雄指令", "遗物"] },
  encounter: { risks: "没有预告或只有单一答案的克制会变成随机处刑。", readability: "战前说明核心压力，战中给时间轴和目标预告，战后解释失败证据。", implementation: "遭遇引用模板与规则资源，不在战斗根节点按关卡 id 分支。", suitable: ["普通敌人", "精英", "Boss", "楼层规则"] },
  run: { risks: "奖励过度迎合会消灭随机性，惩罚过重则让早期事故直接判死。", readability: "在选择前展示价格、库存、风险、未来信息和放弃的机会。", implementation: "所有状态保存稳定 id 与可迁移 DTO，写入失败时不得部分提交。", suitable: ["路线", "商店", "奖励", "局外成长"] },
  agency: { risks: "新增操作可能让自动构筑让位于高频微操，并成倍增加 AI 与 UI 复杂度。", readability: "每项机制标记战前、自动、指令或移动干预及其地图依赖。", implementation: "基线功能不得暗含移动干预；扩展仍服从占位、路径与确定性。", suitable: ["部署", "英雄指令", "未来地图", "未来操作"] },
  safety: { risks: "隐藏限制会损害信任，过量战报又会淹没真正因果。", readability: "统一术语，突出关键触发链与限制，保留可展开的详细证据。", implementation: "安全边界是数据合同和自动测试对象，不是表现层临时补丁。", suitable: ["全系统", "战报", "Boss 验收", "自动测试"] },
};

export const mechanics: Mechanic[] = seeds.map((seed) => {
  const [id, title, domainId, subdimension, premise, rule, decision, pleasure, agency, complexity, dependency, depthTypes] = seed;
  const rel = relationshipMap[id] ?? {};
  const defaults = domainDefaults[domainId];
  return {
    id, title, domainId, subdimension, premise, rule, decision, pleasure,
    consequences: {
      space: depthTypes.includes("space") ? `空间：${premise}` : "空间：主要通过其他机制间接体现。",
      time: depthTypes.includes("time") ? `时间：${rule.split("；")[0]}。` : "时间：不改变基础节拍，影响发生在选择阶段。",
      resource: depthTypes.includes("resource") ? `资源：${decision}` : "资源：不直接新增资源，但可能改变机会成本。",
      build: depthTypes.includes("build") ? `构筑：${pleasure}` : "构筑：作为通用规则约束其他组件。",
    },
    synergies: rel.synergies ?? [], counters: rel.counters ?? [], dependencies: rel.dependencies ?? [],
    ...defaults, dependency, agency, complexity, depthTypes,
  };
});

export type Engine = {
  id: string; title: string; thesis: string; steps: string[]; requirements: string[];
  breaks: string[]; safeguards: string[]; related: string[];
};

export const engines: Engine[] = [
  { id: "engine.shield-cast-loop", title: "护盾—施法循环", thesis: "把防御产物变成施法频率，形成可持续但可熔断的正反馈。", steps: ["友军施法，为最低护盾单位生成护盾", "护盾被击破，触发器缩短自动技能冷却", "技能再次完成，重新补盾并推进循环", "敌方伤害既是压力，也是循环节拍器"], requirements: ["稳定施法起点", "护盾生成", "破盾触发", "冷却回收"], breaks: ["穿盾伤害", "长时间不破盾", "打断施法", "触发频率压制"], safeguards: ["破盾事件每来源有最小间隔", "冷却不得被回退到负值", "同一链路不能在同一模拟步重复边", "战报合并显示循环次数"], related: ["combat.defense.shield", "hero-relic.relic.trigger-rewrite", "hero-relic.relic.loop-core", "safety.simulation.recursion"] },
  { id: "engine.corpse-summon-loop", title: "死亡—尸体—召唤循环", thesis: "把可控伤亡转化为下一批战场单位和死亡收益。", steps: ["低成本单位阵亡留下尸体", "英雄或遗物消费尸体召唤临时单位", "临时单位承担占位并再次阵亡", "死亡层数强化核心或触发范围伤害"], requirements: ["可预测的消耗单位", "尸体所有权", "召唤容量", "死亡收益"], breaks: ["尸体清除", "召唤容量封锁", "敌方击杀成长", "范围清场"], safeguards: ["每具尸体只能消费一次", "召唤物与永久单位容量分离", "死亡来源和归属稳定", "无尸体时触发安全失败"], related: ["combat.summon.corpse", "army.shape.swarm", "hero-relic.relic.conversion", "safety.simulation.order"] },
  { id: "engine.overheal-battery", title: "治疗—过量—充能电池", thesis: "让维持满血与主动承伤之间形成一条资源选择。", steps: ["治疗把前排抬至满血", "被授权的过量治疗转化为技能充能", "充能释放保护或控制技能", "保护减少实际治疗需求，要求新的承伤窗口"], requirements: ["稳定治疗源", "过量转化", "充能技能", "承伤管理"], breaks: ["禁疗", "斩杀阈值", "爆发越过恢复窗口", "转换效率削弱"], safeguards: ["只转换真实过量量", "单次与每秒转换有上限", "不能通过零治疗触发", "来源在战报可追溯"], related: ["combat.trigger.overheal", "combat.defense.guard", "hero-relic.relic.conversion", "encounter.boss.sustain-check"] },
  { id: "engine.pierce-mark", title: "标记—对齐—穿透炮列", thesis: "通过目标选择与纵列几何，把单次远程攻击扩展为军团清线。", steps: ["标记者锁定高价值后排", "前排稳定敌军接敌线", "穿透射手选择经过标记目标的纵列", "破甲层让后续弹道逐步增效"], requirements: ["稳定战线", "目标标记", "直线穿透", "射击走廊"], breaks: ["后排猎手", "击退打散纵列", "最小射程压迫", "分散敌阵"], safeguards: ["弹道和命中顺序确定", "衰减规则公开", "遮挡与合法目标一致", "预览显示潜在线路"], related: ["battlefield.shape.line-pierce", "battlefield.targeting.threat-lock", "army.role.artillery", "combat.damage.armor-break"] },
  { id: "engine.command-burst", title: "指令—窗口爆发", thesis: "有限手动资源不取代构筑，而是在 Boss 的明确窗口把组件同步起来。", steps: ["Boss 时间轴预告护盾破裂或蓄力", "玩家消耗一点法力启动英雄指令", "自动技能冷却对齐，标记与破甲同时进入峰值", "窗口结束后回到自动运行"], requirements: ["公开时间轴", "有限法力", "可同步的自动技能", "窗口收益"], breaks: ["错误时机", "指令失败条件", "阶段提前转换", "控制打断"], safeguards: ["失败不扣资源", "每战法力不恢复", "成功效果由同一参数生成说明", "暂停不改变模拟结果"], related: ["hero-relic.command.bounded", "encounter.boss.timeline", "agency.command.window", "safety.readability.telegraph"] },
];

export const capabilityLabels = ["范围清场", "单体爆发", "破甲/破盾", "后排保护", "后排突破", "持续恢复", "控制打断", "召唤处理", "时间竞速"] as const;
export type Capability = typeof capabilityLabels[number];
export type BossTest = { id: string; title: string; pressure: string; primary: Capability[]; alternative: Capability[]; risky: Capability[]; warning: string; forbidden: string };
export const bossTests: BossTest[] = [
  { id: "boss.swarm-mother", title: "巢群母体", pressure: "召唤波次挤压棋盘并拖延斩首。", primary: ["范围清场", "召唤处理"], alternative: ["后排突破", "破甲/破盾"], risky: ["持续恢复", "时间竞速"], warning: "区域开始展示波次与母体位置。", forbidden: "不能只允许某个范围法师作为答案。" },
  { id: "boss.aegis-forge", title: "铸盾泰坦", pressure: "周期护盾与蓄力反击制造输出窗口。", primary: ["破甲/破盾", "单体爆发"], alternative: ["控制打断", "时间竞速"], risky: ["持续恢复"], warning: "护盾周期、反击能量和穿盾规则全部预告。", forbidden: "不能静默免疫全部护盾构筑。" },
  { id: "boss.backline-hunt", title: "影猎双生", pressure: "交替锁定最远单位，迫使阵型保护核心。", primary: ["后排保护", "控制打断"], alternative: ["后排突破", "召唤处理"], risky: ["单体爆发"], warning: "跳跃目标和落点提前高亮。", forbidden: "不能无视占位直接处决后排。" },
  { id: "boss.plague-doctor", title: "腐化医师", pressure: "治疗、复苏充能和禁疗窗口拉长战斗。", primary: ["单体爆发", "控制打断"], alternative: ["后排突破", "时间竞速"], risky: ["持续恢复"], warning: "治疗目标、复苏条和可打断阶段可见。", forbidden: "不能把所有恢复效果永久设为零。" },
  { id: "boss.executioner", title: "赤刃处刑者", pressure: "低血阈值斩杀放大分摊与恢复失误。", primary: ["持续恢复", "后排保护"], alternative: ["控制打断", "单体爆发"], risky: ["时间竞速"], warning: "斩杀阈值在生命条上常驻标记。", forbidden: "不能造成无预告的不可避免处决。" },
  { id: "boss.clock-lord", title: "时序领主", pressure: "固定节拍的大招和阶段加速检验时机管理。", primary: ["控制打断", "时间竞速"], alternative: ["持续恢复", "单体爆发"], risky: ["召唤处理"], warning: "完整时间轴在战前与战中可查。", forbidden: "不能因倍速或暂停改变结算。" },
  { id: "boss.iron-phalanx", title: "铁壁军团", pressure: "重甲前墙保护后排火力，形成双层目标。", primary: ["破甲/破盾", "后排突破"], alternative: ["范围清场", "单体爆发"], risky: ["持续恢复"], warning: "护甲层、护卫链和炮列目标公开。", forbidden: "不能只有堆更高总伤害一条路线。" },
];

export const catalogVersion = "1.0.0";
