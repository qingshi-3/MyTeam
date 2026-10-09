# 随机大陆进程验收

2026-10-03：阶段 1–4 实现完成，阶段 5 的定向规则及代表流程接续至首领败战终局已取得通过证据；自然三阶段供给节奏与最终用户体验待试玩。下方清单定义验收目标，实际覆盖以末尾结果记录和[活动任务](../../work-items/active/expedition-map-progression.md)为准，不将清单整体视为已通过。规则见[玩家合同](../../gameplay-design/expedition-progression.md)与[系统合同](../../system-design/run-progression-and-sessions.md)。

## 验证范围与投入

2026-10-06 地图表现局部复验：`ExpeditionJourneyInputSmoke.tscn -- --map-light-only` 使用正式 `GrowthGameRoot` 和真实随机新局，在隔离 960×540 宿主内发送按键／Control 输入。`MAP_LIGHT_INPUT_OK` 已通过：行走时详情框持续零偏移和完整透明度、新格揭示、先查看远格再 Enter 打开当前营地、服务期间移动输入隔离、返回前后完整保存和镜头一致、减弱动效无新拖尾、离页清理。该分支未运行战斗或领取奖励；不沿用默认报告中通用的“一场普通战”范围描述。录像、日志及固定场景前后对照见[本轮证据](../../outputs/map-light-20261006/README.md)。

前四阶段仅做必要编译、配置／规则检查及高风险定向检查；第五阶段集中使用正式场景、真实内容和隔离真实输入走一条代表流程。复用已有保存故障、成长、遗物和战果验证夹具，只补新边界；不按阶段反复跑整局、完整回归或多分辨率遍历。发生失败、新风险或证据缺口时再扩大。

构建使用 `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`。定向入口为 `tests/ExpeditionMapContractSmoke.tscn`、`tests/ExpeditionRunContractSmoke.tscn`，真实输入入口为 `tests/ExpeditionJourneyInputSmoke.tscn`；实际覆盖以测试代码和本轮日志为准，不能把地图领域检查写成 UI 或自然战斗通过。旧 Tower 测试须显式选 legacy profile；隔离 namespace 恢复无 ID 旧档时显式传 legacy 映射。所有测试使用隔离存档或内存保存服务，禁止修改真实 `growth-journey` 与 Alpha 存档。

## 定向正确性检查

| 边界 | 关键观察与失败预期 |
| --- | --- |
| 整包与配置恢复 | v2 地图、v1 地图和 legacy Tower 三 profile 联合依赖一次发布；任一缺失、重复稳定 ID、非法组成或战场池拒绝整包。当前 header 匹配完整配置；未知版本、ID、指纹保留原档且不写 Meta；v2–v10 仍执行各自原迁移拒绝条件 |
| 同输入与状态分支 | 种子、开局选择、生成版本与 profile 相同，地图及基础库存相同；新 RunId 只改变身份。不同种子有变化。新 schema 不重复保存根级旧字段／当前机会；地图草稿无 Tower、无地图，确认后只有 Expedition |
| 地图与只读查询 | 配额、Boss 专属战场、真实连通、可绕敌人与守关满足作者约束；未知格不泄漏库存。隔雾、跨墙、跳格无效；Inspect、报价与取消不写盘、分配 Action 或递增序列 |
| 行走保存 | 点击已知远格只走预先捕获路径，首次揭示前沿停止；逐格保存。第 k 步保存失败保留第 k−1 步，不回滚成功前缀、不继续后段；不消耗行动或生产 |
| 开战、编辑与恢复 | 入场遗物后的 Gold 与属性进入最终冻结，法术一次消费；配置或保存失败无扣费。存读／场景启动重试用同一 Action、敌阵、战场、法术和指纹；冻结后各编辑入口都拒绝修改 |
| 服务与商店 | 首次正式有效选择同次支付／发货／成长；预览、满血无效选择和取消无副作用。新英雄／遗物不追溯参与；首次购、后续购、离店、免费回访分别核对；最后一次行动只交付当前商品，余货不得继续购买 |
| 固定库存与独立奖励 | 失败、回访与重载不补抽；取得同身份后原候选失效。装备／遗物按原组与 cursor 恢复；临时接收困难不作永久耗尽；Exhausted 不触发 Skip 收益。基础收益与成长不因领取／重试重复 |
| 身份与计数 | ActionId／Sequence、阶段、具体来源与完整战果匹配；错误组成、来源、指纹、重复结果拒绝。BattleNumber 只在接受结算时加一，覆盖第7／8／9场利息及已过期物品取得资格 |
| 阶段及成长 | 普通失败存活保留敌人／位置，再战重新计费；有效处理生产一次，免费回访不产出；早 Boss 放弃余额无补偿；最后行动先完成全部奖励，Boss奖励与下一阶段额度只提交一次；升阶不足三个合法身份时不扣料 |
| 终局与 Meta | 胜败先保存终局 Run，再保存已到达阶段高水位及应有胜利／解锁，最后删除；Meta 或删除失败可恢复重试，max／完成 ID 不重复计数，不留下无对应结算来源的永久增量 |
| 新局替换与旧塔 | 继续旧塔后新局切换整套服务；草稿保存失败保留旧局，草稿成功后地图生成失败保留本草稿。v10分类奖励及旧已锁对手原样保留，不能补发或转地图 |

故障注入须同时比较磁盘快照与公开只读视图；新字段的深复制不能把失败投影泄漏回权威对象。检查接通后仍须由主 Agent 审核可信执行证据，不能仅以存在测试文件结案。

## 第五阶段代表流程

在 960×540 隔离视口通过正式 GUI 输入，记录种子、起始英雄、实际所得与资源；不移动用户桌面鼠标。优先一条自然代表流程，不注入资源／胜利来替代自然供给证据；必要故障夹具单列。

1. 新征程六选二，返回后继续确认同一草稿；进入大陆查看当前位置、迷雾、已知地点、行动额度和 Boss 预告。
2. 查看敌人及服务后取消，检查队伍、行动和资源不变；走已知路径，观察前沿停步、受阻原因与绕路是否清楚。
3. 从实际供给完成一次服务与一场普通战，确认入场／成长、战前法术以及胜负后果。查看军团、装备和工坊，核对返回目的地及冻结提示。
4. 到商店比较首购、后续购买与离店回访；在独立奖励组中途继续存档，确认候选、游标、基础收益和库存不重复。
5. 将当前阶段推进到最后行动，先处理其奖励再准备 Boss；非终局 Boss 奖励完成后进入下一阶段，额度只补一次。继续至一个真实终局并检查菜单、继续和 Meta；提前 Boss 与保存失败等未自然发生分支由定向检查补证据。

需要观察移动或界面反馈时，保留同尺寸、正常速度录制。用户最终评价迷雾信息、绕路取舍、地点密度、整备与领奖手感、自然成长供给、阶段压力和战斗时长。16×16、三阶段各6行动、39地点及当前倍率仍是待校准作者基线；一条流程不代表全部种子、阵容、分支或平衡通过。

## 结果记录

结果必须分开写：编译／配置检查、定向自动规则、实际 GUI 输入、正常速度可见结果、用户体验确认，以及失败和未覆盖项。

| 本轮证据 | 实际覆盖 | 不能据此宣称 |
| --- | --- | --- |
| [myteam-expedition-build-complete.log](../../outputs/expedition-verification-20261003/myteam-expedition-build-complete.log)：构建成功，0 warning／0 error | 包含最终预览遗物／取消补丁的代码可编译；较早 build-final 日志作为历史保留 | 运行、观感或平衡已全面验收 |
| [myteam-expedition-map.log](../../outputs/expedition-verification-20261003/myteam-expedition-map.log)：`EXPEDITION_MAP_CONTRACT_OK` | 确定性、拓扑变化、配额、守关／绕路、迷雾／导航、恢复／复制与来源校验 | 地点密度、自然供给或地图观感已验收 |
| [myteam-expedition-run-complete.log](../../outputs/expedition-verification-20261003/myteam-expedition-run-complete.log)：`EXPEDITION_RUN_CONTRACT_OK` | 正式包、固定库存、事务回滚、服务一次性、来源绑定、末次行动、阶段奖励补额；3 个受控 Boss 结果、终局 Meta 保存／删除失败恢复、v10 迁移和配置路由；在 run-final 基础上增补入场后垫底金币及第 7／8／9 场利息边界 | 三阶段自然养成、Boss 实战胜利或完整 GUI 通关 |
| [myteam-expedition-input.log](../../outputs/expedition-verification-20261003/myteam-expedition-input.log)：`EXPEDITION_JOURNEY_INPUT_OK`；[详细输入](../../outputs/expedition-journey-20261003/evidence.json) | 真实新局六选二、探索、军团 Esc、营地、战前取消、x1 自然普通战 `PlayerVictory`、equipment 奖励与 Continue 完整恢复 | 干净的最终整流程：该次存在 SVG 未导入及地图初始定位可见问题，随后修复 |
| [myteam-expedition-map-input-final.log](../../outputs/expedition-verification-20261003/myteam-expedition-map-input-final.log)：`EXPEDITION_MAP_SCREENSHOT_OK`，并记录 `initial-party-visible-without-test-scrolling`；[详细输入](../../outputs/expedition-map-final-20261003/evidence.json) | 修复后新局六选二到地图干净运行，队伍初始可见，测试未滚动代偿 | 修复后又完整跑过服务／战斗／终局 |
| [myteam-expedition-service-input-final.log](../../outputs/expedition-verification-20261003/myteam-expedition-service-input-final.log)：`EXPEDITION_SERVICE_INPUT_OK`；[详细输入](../../outputs/expedition-service-final-20261003/evidence.json) | 服务预览取消保持完整保存不变，重开确认仅结算一次，Continue 精确恢复；无 Godot 错误，主 Agent 已核看地图与取消入口截图 | 全部服务、商店所有分支或三阶段实际输入通过 |
| [myteam-expedition-legacy-rewards.log](../../outputs/expedition-verification-20261003/myteam-expedition-legacy-rewards.log)：`GROWTH_REWARD_GROUPS_CONTRACT_OK` | 仅补共享 choice／奖励迁移受影响的旧塔回归缺口，包含组独立、领取一次、冻结恢复、唯一性与库存消耗 | 全部旧塔专项或宽泛回归通过 |

隔离宿主 960×540，内部视口 1600×900；通过 SubViewport 发送真实输入，不移动桌面鼠标。当前地图画面见[文字对比度修订后的截图](../../outputs/expedition-map-readable-20261003/map-overview.png)，正常速度动态证据见[战斗 GIF](../../outputs/expedition-journey-20261003/battle-normal-speed.gif)：45 帧、4029 ms，保留 x1 实际战斗片段。录像用于观察局部可见结果，不代表完整战斗时长或用户审美确认。

最终补丁构建、入场金币／利息和服务取消专项均已核对实际日志。首轮真实输入片段与其可见问题仍保留，不以补验覆盖未重跑的后续流程；服务局部画面见[取消前预览](../../outputs/expedition-service-final-20261003/service-preview-before-cancel.png)与[恢复后地图](../../outputs/expedition-service-final-20261003/map-restored.png)。

仍待自然三阶段试玩和最终用户确认：供给是否支撑队伍成长与调整，绕路／守关是否清楚，最后行动至 Boss 的节奏，普通战与 Boss 时长，整备及领奖手感。未做多分辨率遍历、全部随机种子与分支验收；16×16／三阶段各 6 行动／39 地点以及本轮余额、费用和倍率都不是已获用户认可的平衡结论。

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

## 招募专项结果（2026-10-03）

`ExpeditionRunContractSmoke.tscn -- --recruitment-only` 和 `ExpeditionJourneyInputSmoke.tscn -- --recruitment-only` 已通过；完整日志、截图与限制见[活动任务招募修复](../../work-items/active/expedition-map-progression.md#招募候选与卡牌修复2026-10-03)。覆盖新征程三选一、原 v1 草稿与库存兼容、键盘进入、取消／重进、详情及提示、招募单次扣费与继续恢复；一／两候选尺寸另用不写存档的共享场景夹具核对。编译 0 warning／0 error，最终输入日志无 Godot warning／error。

试玩检查：重新运行游戏，新征程探索到旅途伙伴，确认三张竖卡并排、姓名和三种核心数值分行；点详情与悬浮条目读取属性、技能、成长与羁绊；选择一人后返回地图。继续旧 v1 征程仍显示原冻结库存数量，但不应横向拉伸。最终观感、阅读舒适度与供给节奏待用户验收。


招募卡角标局部修订后，可用 `ExpeditionJourneyInputSmoke.tscn -- --recruitment-style-only` 单独检查正式卡面、无按钮悬浮／键盘阅读和一／两候选布局，避免重复支付／继续流程。该入口已通过，证据与范围见[界面活动任务](../../work-items/active/ui-material-overhaul.md#招募卡去详情与角标修订2026-10-03)。单位卡已移除“详情”按钮，原招募专项中的详情操作现改为悬浮或 Tab 聚焦；物品与机会的正文详情入口保留。
