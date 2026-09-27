# 羁绊、单位成长与整局供给设计执行

## 2026-09-27 goal实施授权（已交付）

用户明确要求使用goal完成最初的可玩养成／运营目标，合理以Astra主责判断、gpt-5.6-sol承担明确执行。授权已从下方历史设计阶段扩展为运行实现、相关UI、保存、内容接入、必要构建与真实战斗／输入验证，及本次范围的权威同步。不恢复等级／同名合并，不把旧棘毒付费候选覆盖后续持续单位产出方向，不要求再次确认普通实施细节。

当前交付范围：可从真实应用进入、沿真实Run继续的成长征程；以构装工坊在定向永久培养与研究积累之间选择为主，霜羽作战斗对照，兼容培养来源作补充。接通单位产出、局内成长保存和下场投影；通用／类别材料一次机制升阶＋高阶三选一；研究取得与消耗法术；成长／运营能力和资源UI；连续取得、早晚／无源／集中／缺件对照及关键失败路径。原49横向内容可复用，新玩法不默认要求给每位角色追加运营能力，也不以现有297场代替新过程验证。

独立可玩项目配置和隔离存档已接入现有GameFlow／战斗核心，并使用独立地区与遭遇；Alpha征程及其数值未被本目标修改。主责已完成接口、复杂结算语义和最终集成收束。

本次工程验收已完成：可运行入口、真实保存／继续、普通失败继续与Boss终局、重复提交／保存失败不泄漏、后续战斗读取成长／升阶／法术、真实输入基本操作与成长路线统计均有证据。玩家手感和最终平衡接受仍由用户评估，代理验证不替代用户认可。

## 2026-09-27 实施与验证状态（本次 goal 已交付）

本批可玩闭环已经落地：独立 `GrowthGameRoot`、独立地区／遭遇与独立存档命名空间接入真实 `GameFlow`；成长内容包覆盖 26 名代表单位，不宣称原 49 名全部完成成长重构。运行时已接通节点生产、攻击／生命永久增量与研究、每节点材料、一次材料升阶及固定高阶三选一、研究法术的兑换与真实开战消费、MX01 首次冻结后的跨战生命收益、v8 保存／迁移与战斗投影。Alpha的内容池和遭遇数值保留，普通征程继续可用；共享schema按明确兼容范围升级，测试未覆盖真实玩家存档。

最终构建为 0 警告、0 错误；成长合同、战内永久收益、真实入口与工坊输入通过，已查看更新后的工坊截图。MX03 升阶后的换目标保层已由实际普攻路径验证；护阵为目标及近邻各获得相当于指定英雄本场生命上限15%的18秒护盾；v7节点续行与满员时拒绝装载 `PendingDiscovery` 的失败路径已修复并验证。

最终连续路线证据见 [`growth-authored-journeys.md`](../../design-discussion/04-content-validation/artifacts/growth-route/runtime/growth-authored-journeys.md)、[`growth-holdout-journeys.md`](../../design-discussion/04-content-validation/artifacts/growth-route/runtime/growth-holdout-journeys.md)、[`growth-elite-journeys.md`](../../design-discussion/04-content-validation/artifacts/growth-route/runtime/growth-elite-journeys.md) 与 [`final-validation.md`](../../design-discussion/04-content-validation/artifacts/growth-route/runtime/final-validation.md)。最终正式21局之外另保留8局未用seed与2局修正精英路线：原三seed中 frost／growth／research／no-growth 各3/3通关，late／scatter各0/3，concentrated为2/3；holdout中 frost 2/2、growth 2/2、research 1/2、scatter 1/2；两条精英路线共赢得4场真实Elite战，但都败于第2个Boss，第三地区Elite未覆盖。旧JSON的 `Cell={}` 限制和退出时2项资源告警按报告保留。当前证据不支持宣称完美平衡、完整站位回放或玩家已接受；玩家仍需手动验收工坊信息、选择节奏、法术预设及整体难度。

## 历史设计阶段的目标与授权（已由上方实施授权取代）

2026-09-27，用户在确定设计方向并请求规划路径后明确“执行”。本节记录当时先完成阶段1～5设计与算术／供给推演的边界；“不进入运行实现”已被顶端 goal 授权取代。原 49 横向矩阵仍保留，本批采用 26 名代表包而非全量重构。

固定方向：地图战斗＋职业／体系羁绊；有分量的单位持续成长与多种产出；无等级、无同名合并；常规一次材料机制升阶＋高阶三选一；法术介入运营／战斗但具体形式可按内容设计；不强制每单位一主动一被动，不以创作困难缩减规划。

## 恢复入口与分工

- 主稿：`design-discussion/04-content-validation/artifacts/growth-route/README.md`。
- 当前代码证据：同目录 `baseline.md`／`current-baseline.json`。
- 全貌：`content-map.md`／`content-map.json`，衔接原49位与两轴标签。
- 数值执行：`model.mjs`＋`model-config.json`生成模型报告及JSON；`supply-model.mjs`生成候选供给报告及JSON。
- 样例：`samples.md`；执行后的发现与方案修正由主责汇总。
- 主责Astra负责参数假设、内容方案、关键取舍与最终审查；roster_design_inputs及growth_content_map（gpt-5.6-sol）分别负责基线／算术模型、内容整理／供给抽样。原Astra协作者提供体系建议和独立反例审查。

当时“不启动 Godot／不构建／不改运行资源”只适用于首轮纸面推演，现为历史约束，不再禁止本 goal 已授权的实现与验证。仍只在现有 main 操作并保留文件归属。

## 验收与状态

首轮设计与推演已交付：14次节点成长候选预算、3%／4%／6%及叠加敏感性、七体系职责与49位映射、三种成套路线、连续资源样例及系统／UI流程草案。原49位保留43个战斗身份、提出6个发展重构；另外4个GX高阶位置仅参与供给补洞和纸面设计。

模型执行并通过对应自检：531,441条节点路径；原49／扩展53各70,000条固定选法供给样本。原49中21.47%样本的末段发现不足三项，补测后本批归零；仍有较多无属性源局，补高阶会重排各体系形成率。已回填设计，未把输出规模或0断言失败当作平衡结论。

主责审查完成：修正Boss战前快照、尚未取得目标／来源的倍率、所有供给事件自检范围，明确败战可成长指征程未终局，明确MX23升级新增窗口及MX47重构后的保留／删除能力。模型保留原始失败样本，不覆盖历史结果来掩盖缺口。

复现：在仓库执行 `D:\nodejs\node.exe design-discussion/04-content-validation/artifacts/growth-route/model.mjs`，以及同目录 `supply-model.mjs`。前者读取model-config.json；后者读取原矩阵plan.json并在脚本中显式添加四个供给占位，输出各自JSON／Markdown，不改变游戏资源。

该首轮结论之后，系统运行实现、真实连续路线、代表供给、法术／升阶操作和敌人门槛验证已经完成本次 goal 的工程交付。最终平衡与玩家体验仍须手动验收；49 名全量重构不属于本 goal，不能因 26 名代表包完成而宣称全量完成。
