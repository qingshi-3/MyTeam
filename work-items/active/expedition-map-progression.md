# 随机大陆探索替换简单爬塔

2026-10-03。状态：阶段 1–4 实现完成；阶段 5 的定向规则及代表流程接续至首领败战终局已取得通过证据，自然三阶段供给节奏与最终用户体验仍待试玩。代表流程接续至首领自然败战终局；没有完整三阶段真实 GUI 通关证据。

## 授权与当前工作点

用户认可 [R10-Q11](../../design-discussion/03-run-mechanisms/journey-and-pressure/route-progression/proposals.md#r10-q11随机大陆替换爬塔的细节流程规划2026-10-02) 流程，委托 Agent 自行补足设计并用空上下文子 Agent review，要求真实架构／内容匹配、顺滑集成、无临时逻辑，见 [D11](../../design-discussion/03-run-mechanisms/journey-and-pressure/route-progression/decisions.md)。随后明确要求执行落地并善用主／子 Agent，实施授权持续有效；不重复索要确认。具体数值仍区分作者配置与用户验收。

直接入口：[实施设计](../../design-discussion/03-run-mechanisms/journey-and-pressure/route-progression/artifacts/expedition-implementation-design.md)及[独立审查](../../design-discussion/03-run-mechanisms/journey-and-pressure/route-progression/artifacts/expedition-design-review.md)。

正式规则已同步到[随机大陆征程](../../gameplay-design/expedition-progression.md)，运行时合同归[进程与会话](../../system-design/run-progression-and-sessions.md)，执行观察点归[地图进程验收](../../docs/testcases/expedition-map-progression.md)。讨论中的计划类型名不作为当前已有 API 的证据。

## 范围与实施顺序

设计与独立审查已完成；实施按五阶段组织，当前 1–4 已完成，第 5 阶段按下方实际证据记录覆盖与缺口：

1. 配置与身份：完整 profile 发布／旧档选择、类型化动作来源、共用状态迁移与编辑权限；旧 Tower 基线保持。同步实施触及的正式玩法／架构合同，不能仅留讨论文档。
2. 地图核心：纯随机生成／揭示／通行、起点与地点配额、绕路／守关、只读地图投影及持久化。
3. 进程集成：阶段预算、共同战果投影、成长／法术／遗物和 choice evaluator；接通真实战斗与保存，形成完整业务闭环。
4. 预制界面：地图、内容预览、绕行／受阻提示、成本、阶段预告；沿用已认可材质与环境展示边界。
5. 集中验收与校准：960×540 隔离环境使用正式内容和真实输入，走一遍新局、探索、成长、阶段 Boss 至终局的代表流程，检查存档恢复；交付体验检查点和局限，按实际问题调整节奏。

## 验证投入约定

用户认可五阶段安排，并明确不需要每阶段重验证，最终集中走流程验收。前四阶段只做与本次改动相称的编译、配置／规则检查和关键风险定向验证，优先复用现有检查；存档迁移、事务幂等、战果验签／冻结等高风险边界仍须及时验证。阶段故障边界清单用于分配覆盖，不要求每阶段全量重跑。

正式整合后的流程验收集中在第五阶段。不按阶段反复跑整局、多分辨率截图、宽泛回归或重做完整独立审查；出现失败、新风险或关键证据缺口时才补相关检查。最终一遍流程不等同所有随机种子、分支和构筑平衡均已验收，未覆盖项如实记录。

## 当前进度

- 已核对配置／编译／ReadyGate、Run／Growth／Relic／BattlePreparation、导航 UI、当前作者供给和正式权威；不按旧活动文档的英雄／物品数量下结论。
- 用户已认可 Q11；补全首版地图、内容与供给、具体倍率、费用／收益、最后一次结算、Boss 与旧档合同。16×16、39 地点、三阶段各 6 行动、现有内容倍率是首轮作者参数，必须另做自然发育诊断。
- 独立 reviewer 使用 `fork_turns=none`，初审提出 ER01–ER06（1 P1、5 P2）；修订后复审另发现 ER07 Boss 专属战场池和 ER08 入场后完整战斗快照（2 P2）。两项及 Meta 终局高水位提醒修订后，最终定点复核确认 ER01–ER08 在设计层全部关闭。初审／两轮复核记录均保留；审查时确认可进入代码阶段，不能把静态审查写成运行通过。
- 阶段 1 已实现：类型化 Tower／Expedition 配置、所有 profile 联合依赖与 ReadyGate、默认地图／完整 legacy 发布、只读存档 header 选择、全套服务会话切换及 v11 身份。旧根属性为 Tower 兼容转发，旧 JSON 用 deserialize-only 输入迁移；地图草稿不伪造 Tower。旧局会话、v10 迁移及未知 profile 拒绝已有定向通过证据。
- 阶段 2 已实现：纯地图生成、连通／起点／绕路／守关校验、迷雾、已知路径和只读投影；移动捕获已知路径，在首次揭示前沿停止，逐格保存。地图合同专项通过；真实输入已检查走路揭雾且不扣行动。
- 阶段 3 已实现：动作协调、共用战果投影、通用 choice evaluator、成长来源、法术与遗物冻结、独立奖励及阶段／终局保存。服务 Inspect 为纯预览，首次正式选择才创建事务；固定库存及事件 SamplingKey 与 RunId 身份分离。受控结果验证了三阶段 Boss、终局 Meta 保存／删除失败恢复，不能代替自然发育供给与三场 Boss 实战。
- 阶段 4 已实现：预制地图及格子、主流程导航、敌情／服务预览、共享整备与战报已绑定。真实输入代表片段完成探索、营地、战前取消、x1 自然普通战胜利、装备奖励与继续恢复。首轮存在 SVG 未导入和地图初始定位可见问题，修复后另做地图局部输入检查，确认无需测试滚动便能看到队伍；用户观感尚未验收。
- 作者资源已加入 `content/expedition/`，复用成长目录、供给、规则和完整敌阵；包含三阶段 Boss 专属战场、营地与遗迹通用 operations、有序分类奖励。16×16／三阶段各6行动／39地点是首轮作者基线，尚未完成自然供给与节奏校准。
- 阶段 5 基础验收完成，体验校准待试玩：以下定向规则、真实输入片段及修复后的局部地图验证已有日志。预览遗物／取消小补丁后的最终编译、入场金币／利息边界及服务取消专项已通过；另仅补共享 choice／奖励迁移相关的旧塔奖励回归。自然三阶段流程、供给节奏与最终用户体验待试玩。旧塔历史通过结果不延伸为地图通过。

## 本轮执行证据与限制

| 检查 | 已取得证据 | 覆盖与限制 |
| --- | --- | --- |
| 最终编译 | [myteam-expedition-build-complete.log](../../outputs/expedition-verification-20261003/myteam-expedition-build-complete.log)：0 warning、0 error | 已包含预览遗物／取消小补丁；早一轮 [myteam-expedition-build-final.log](../../outputs/expedition-verification-20261003/myteam-expedition-build-final.log) 同样成功，保留为历史 |
| 地图合同 | [myteam-expedition-map.log](../../outputs/expedition-verification-20261003/myteam-expedition-map.log)：`EXPEDITION_MAP_CONTRACT_OK` | 同输入、可变拓扑、配额、守关／绕路、迷雾／导航、恢复／深复制、无效输入与来源；属于领域检查 |
| Run 合同 | [myteam-expedition-run-complete.log](../../outputs/expedition-verification-20261003/myteam-expedition-run-complete.log)：`EXPEDITION_RUN_CONTRACT_OK` | 开局／回滚、固定库存、服务一次性、战斗来源／回滚、末次行动、Boss冻结／奖励／补额、3 个受控 Boss 结果、终局 Meta 保存及删除失败恢复、legacy 会话／v10 迁移／未知 profile；较 [myteam-expedition-run-final.log](../../outputs/expedition-verification-20261003/myteam-expedition-run-final.log) 增补 `bank-padding-post-entry` 与 `interest-7-8-9`。不是自然三阶段流程 |
| 正式真实输入片段 | [myteam-expedition-input.log](../../outputs/expedition-verification-20261003/myteam-expedition-input.log)：`EXPEDITION_JOURNEY_INPUT_OK`；[输入记录](../../outputs/expedition-journey-20261003/evidence.json) | 新局六选二→探索揭雾→军团 Esc→营地选择→战前取消→x1 自然战斗 `PlayerVictory`→1 组 equipment 奖励→重建入口后 Continue 完整恢复。该次有 SVG 导入错误与初始地图定位问题，不能写成干净的最终整流程验收 |
| 修复后地图局部输入 | [myteam-expedition-map-input-final.log](../../outputs/expedition-verification-20261003/myteam-expedition-map-input-final.log)：`EXPEDITION_MAP_SCREENSHOT_OK`、`initial-party-visible-without-test-scrolling`；[记录](../../outputs/expedition-map-final-20261003/evidence.json) | 新局六选二到地图干净运行；测试未主动滚动便看到队伍。仅补验地图可见问题，没有重跑后续战斗流程 |
| 最终服务局部输入 | [myteam-expedition-service-input-final.log](../../outputs/expedition-verification-20261003/myteam-expedition-service-input-final.log)：`EXPEDITION_SERVICE_INPUT_OK`；[记录](../../outputs/expedition-service-final-20261003/evidence.json) | 预览取消保持完整保存不变、重开后确认一次、Continue 精确恢复；日志无 Godot 错误，主 Agent 核看了地图与取消入口截图。仅覆盖该服务片段 |
| 旧塔奖励定向回归 | [myteam-expedition-legacy-rewards.log](../../outputs/expedition-verification-20261003/myteam-expedition-legacy-rewards.log)：`GROWTH_REWARD_GROUPS_CONTRACT_OK` | 共享 choice／奖励及迁移边界：原子结算、分类组、领取一次、按组跳过、冻结恢复、legacy 保留、遗物唯一、商店消耗库存；不是宽泛旧塔回归 |

当前可用画面是[文字对比度修订后的地图](../../outputs/expedition-map-readable-20261003/map-overview.png)；[正常速度战斗 GIF](../../outputs/expedition-journey-20261003/battle-normal-speed.gif)含 45 帧、4029 ms，来自上述 x1 实际战斗片段，不是完整战斗或三阶段录像。隔离宿主为 960×540，记录中的内部视口为 1600×900；输入使用 SubViewport，不移动桌面鼠标。

上述最终补丁检查已凭实际日志更新；没有再以全流程自动推进或宽泛回归补成“整体通过”。服务局部截图另见[取消前预览](../../outputs/expedition-service-final-20261003/service-preview-before-cancel.png)和[恢复后地图](../../outputs/expedition-service-final-20261003/map-restored.png)。

## 实施必须覆盖的故障边界

无 ID 旧档先绑定完整 legacy profile；旧局再点新局须切整个服务会话；新草稿保存失败保留上一局，选队生成失败保留当前草稿；法术／入场遗物／金币／战斗身份一次冻结；奖励和生产只存一份；第 7／8／9 场利息与库存过期；空组不冒充主动放弃；Boss 奖励／阶段补充一次；Meta 高水位保存成功后才清理终局。详细检查点以实施设计 §8 为准。

下一步自然试玩三阶段，记录实际供给、队伍调整、阶段压力与战斗时长。16×16／3×6／39 以及余额、费用和倍率均为作者基线，测试断言或正常战斗胜利不构成用户对这些数值的认可。

## 恢复注意

只在 main；保留当前已有讨论改动、未跟踪 outputs、`.uid` 和 `.csproj.old`。不创建分支、工作树，不移动桌面鼠标。15 层以完整 legacy profile 保留，不锁定新地图大小和阶段预算。本批无须新英雄、复杂任务系统或重新制作天气；战斗规则和环境展示职责保持。主 Agent 统一编译与验证，子 Agent 不各自反复运行游戏或重做完整审查；最终小补丁结果只在核对实际日志后更新，不以本页完成代替体验验收。

- 末次可见修订：地图阶段、坐标、缩放、移动提示和图例增加暗色描边，避免浅色地面淹没文字。通过 `--map-only` 再核对真实开局与初始定位，日志见 [myteam-expedition-map-readable.log](../../outputs/expedition-verification-20261003/myteam-expedition-map-readable.log)，主 Agent 已检查输出截图；只修改预制场景文字样式，未更改规则。

### 首领至终局的真实输入补验

从上述自然普通战后的原隔离存档继续（种子 `1790965869291`、`hero_mx21`／`hero_mx03`、31 金币、1 场已结算战斗、行动 4/6），提前挑战第一阶段首领，x1 实战自然 `PlayerDefeat`。正式战报继续到“征程结束”，活动存档清理，Meta 保留阶段 1；实际点击返回菜单后 Continue 禁用。这个分支是提前迎战失败，不是充分发育后的难度评价，也未覆盖阶段胜利的 GUI 领奖／补额。

首次补验的测试断言错误要求败局增加胜利奖励去重 ID，停在终局页；产品规则仅胜利增加该 ID，败局保存阶段高水位。纠正断言后，将已自然产生的 `before-boss-run.json` 原样复制到另一隔离 namespace，资源／阵容／地图与种子均未修改，重新执行同一首领分支并完成菜单输入。失败日志保留为 [boss-input](../../outputs/expedition-verification-20261003/myteam-expedition-boss-input.log)；最终 [boss-replay](../../outputs/expedition-verification-20261003/myteam-expedition-boss-replay.log) 退出 0，标记 `EXPEDITION_BOSS_INPUT_OK outcomes=stage-1:PlayerDefeat terminal=defeat`，[复制来源](../../outputs/expedition-boss-replay-20261003/replay-source.json)与[输入记录](../../outputs/expedition-boss-replay-20261003/evidence.json)可核对。只补验本次终局缺口，不算第二次自然发育样本。

最终测试入口构建 [build-terminal-final](../../outputs/expedition-verification-20261003/myteam-expedition-build-terminal-final.log) 为 0 warning／0 error。当前已覆盖代表流程的探索、服务、普通胜战领奖、继续、首领败战终局；全部检查索引见 [verified-results.json](../../outputs/expedition-verification-20261003/verified-results.json)。自然三阶段发育／通关与节奏平衡仍待试玩。

## 2026-10-03 键盘地图操作

用户要求改为按键移动、回车交互。已接入独立 InputMap 动作：方向键／WASD 逐格移动，系统按住重复事件限速；Enter／小键盘 Enter 处理当前位置，或最近一次方向尝试所指的相邻守军。方向尝试不自动战斗，鼠标查看不改键盘交互目标。原移动、揭示、保存与地点事务入口复用，未修改费用或战斗规则。

地图先认领对应按键，防止无效交互落给焦点中的 Boss 按钮；军团 OpenChanged 的独立阻挡位覆盖装备／成长及关闭动画，隐藏页面／文本框／聚焦弹窗不接收地图输入。只读复核发现越界时可能保留旧目标，已重置至当前位置并刷新显示。

[最终构建](../../outputs/expedition-keyboard-20261003/myteam-expedition-keyboard-build-final.log) 0 warning／0 error。[局部真实输入](../../outputs/expedition-keyboard-20261003/myteam-expedition-keyboard-input.log) 退出 0，`EXPEDITION_KEYBOARD_INPUT_OK steps=4`：交替 WASD／方向键逐格行走、营地 Enter 后取消完整存档不变、军团阻挡及 Esc 后恢复、地形或边界阻挡、面向守军 KpEnter 进入正式整备后返回、服务／整备／主菜单不穿透、Tab＋Enter 继续。使用 960×540 隔离宿主与 1600×900 内部视口，未移动桌面鼠标。截图及实际按键见 [evidence.json](../../outputs/expedition-keyboard-20261003/evidence.json)，主 Agent 已检查守军选中截图和提示可读性。

本轮没有跑战斗或重复整局。越界清理旧目标的末次三行修订经过静态复核与最终编译，未单独复现“边界＋旧守军目标”的组合输入；按住移动的长时手感仍待用户体验。

## 招募候选与卡牌修复（2026-10-03）

- 用户截图中的单候选来自作者配方 `PoolChoiceCount=1`；UI 同时将单位卡 `ExpandFill` 撑满整行，且旧最小宽度导致三个候选不能同排。当前新征程改为三选一，候选卡使用稳定竖版比例与居中排列，攻击／法强／生命与姓名分行；补齐真实羁绊详情。开局六选二及军团卡资源保持原样。
- 默认新增 `growth_expedition_v2` 与五个独立作者资源，保留完整 v1 配置和旧指纹。已有 v1 草稿／征程继续原局，固定的一候选库存不补抽；未知指纹仍拒绝。只读核对真实用户存档头，未修改用户存档。
- [编译](../../outputs/recruitment-fix-20261003/build-verified.log)通过，0 warning／0 error。[候选与兼容合同](../../outputs/recruitment-fix-20261003/contract-final.log)得到 `EXPEDITION_RECRUITMENT_CONTRACT_OK`：六处新库存各有三个不同且排除开局身份的候选，旧 v1 草稿／库存恢复精确相同，未知指纹保留原档。
- [最终真实输入](../../outputs/recruitment-fix-20261003/input-verified.log)得到 `EXPEDITION_RECRUITMENT_INPUT_OK`；[记录](../../outputs/recruitment-verified-20261003/evidence.json)覆盖正式新局、键盘探索到招募点、回车打开三选一、取消和重进保持存档原文、详情展开、实际成长／羁绊／技能悬浮核对、确认只增加一英雄扣一行动、重新启动并继续精确恢复。960×540 隔离宿主，1600×900 内容视口，无桌面鼠标移动，最终日志无 Godot warning／error。
- 主 Agent 核看[正式三选一](../../outputs/recruitment-verified-20261003/recruitment-natural-three.png)、[展开详情](../../outputs/recruitment-verified-20261003/recruitment-details-expanded.png)与[单候选夹具](../../outputs/recruitment-verified-20261003/recruitment-fixture-1-candidate.png)。一／两候选布局在独立共享场景夹具中检查，未冒充自然库存，未改正式隔离征程存档。
- 首轮截图揭示核心数值与姓名重叠，随后修复；新增详情测试曾因键盘焦点提示不会随鼠标离开消失而超时，改用真实空白点击走原提示关闭交互后通过。中间日志／截图保留在 `outputs/recruitment-fix-20261003/` 与 `outputs/recruitment-fix-final-20261003/`，不作为最终通过图。
- 验证限于招募和相关兼容边界，没有重跑全地图战斗或多分辨率检查，最终观感仍由用户确认。
