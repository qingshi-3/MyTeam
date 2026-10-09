# 八羁绊能力原型验收

2026-10-06 最新内容接入为[花灵治疗与定向培养首步](flower-cultivation.md)，其中记录当前构建、专项、正常征程实际输入与独立审查。以下较早的空生产内容描述属于对应检查时的版本，不代表当前花灵仍无培养。

## 2026-10-06 详细战斗统计

入口：「练习与预览 → 能力实验」开战后点右上角「战况」。左侧切换阵营和指标，点击单位后在右侧滚动查看控制来源、输出／生存、治疗护盾和伤害组成；实验终局自动保留冻结统计。「导出本场JSON」只按需写诊断文件，不建 Run。正式规则和统计边界见[能力实验](../../gameplay-design/ability-prototype.md#战斗统计与分析)。

证据目录：`outputs/battle-statistics-20261006/`。

- `build.log`：低并发.NET构建0警告／0错误。
- `contracts.log` 的 `BATTLE_DETAILED_STATISTICS_OK`：实际扣血／护盾／溢出、有效／过量治疗、控制重叠／来源／自身动作锁定、复活、同tick加入、终局冻结、嵌套回滚、64来源＋溢出、采集关闭。统计专用归因覆盖不改变伤害事实来源／类别／行动ID。
- `integration.log` 的 `BATTLE_DETAILED_STATISTICS_INTEGRATION_OK`：实际 `BattleSimulation.Step` 验证50%控制抗性、刷新、重叠去重、到期、驱散及周期伤害来源。四组真实预设在相同种子下，开启／关闭采集的全部战斗事件、结果指纹和原有单位统计相同；并检查射手追加、雷爪连锁和林牙扑击的精确统计来源。每组预热后5对交替样本，仅测模拟采集与结果生成，不测渲染。
- `prototype-regression.log` 的 `ABILITY_PROTOTYPE_CONTRACT_OK`：八羁绊、全部十二人施法、复制独立状态、换形后普攻、同源护盾刷新、回滚、一次落雷、刺客落点和追加攻击有界等既有规则回归。
- `input.log` 的 `BATTLE_STATISTICS_INPUT_OK`：960×540宿主／1600×900隔离SubViewport，真实鼠标／键盘输入覆盖打开、阵营／指标切换、单位明细、滚动、导出、单位资料、暂停、变速、自然终局、终局继续查看／导出、重置与返回。关闭面板期间统计快照为0，打开后采样实测4次／秒，暂停后不重复生成快照。未移动桌面鼠标。
- `input/control-detail.png` 能看到实际冻结累计和来源；`input/damage-breakdown.png` 覆盖滚动后的伤害表；两份JSON为暂停／终局真实导出。`baseline-comparison.log` 确认追加统计归因前后同配置自然终局的种子、时间、胜负和指纹不变。

本机Debug样本开销（不是帧率或所有阵容规模保证）：四组开启采集后的累计托管分配相对关闭增加约0.74%–1.07%；每组5次中位耗时差为约−0.21%–1.94%。首组首个样本明显偏慢，按均值统计差异约13.8%，故保留全部原始样本，不把中位数表述为稳定提升或全场景上限。后两组分别225／242模拟步自然结束，前两组截取300步。详见 `performance.json` 和 `integration.log`。面板开销以真实输入限频证据单列；常规征程不开详细采集。

首次两处专项失败来自测试夹具：手写属性缺回蓝属性、假人没有英雄资格导致首步结束；均改为生产属性工厂／正确英雄身份后通过，没有通过修改战斗规则绕过。一次构建遇到共享工作区其他改动的缺失导入，重读时该文件已自行修复；本任务未覆盖其改动，随后构建通过。

Astra分别审查累计／回滚／性能边界和统计专用来源传递，两轮无阻塞发现。尚未把减速／嘲讽折算为硬控，不估算属性加成的假设伤害贡献；统计来源覆盖可重叠，召唤物独立，不宣称单场数据即可代表阵容平衡或趣味。

## 2026-10-06 恢复入口与验收

能力实验从主菜单“练习与预览”单独进入；正常游戏保留原 GrowthGameRoot 主流程。GameRoot 托管预制 ContentRebuildRoot 测试场景，不创建 Run／存档，退出后返回原主菜单。八羁绊能力实验细节见[正式说明](../../gameplay-design/ability-prototype.md)。

当前运行证据：`outputs/ui-flow-restoration-20261006/build.log` 为 0 warnings／errors；`contracts.log` 的 `JOURNEY_RESTORATION_CONTRACT_OK` 已确认内容发布、12 英雄、空物品／升阶、开局／地图／招募／空商店、保存失败原子性、继续精确恢复及旧 profile 拒绝且不覆盖。最终 `input.log` 的 `JOURNEY_RESTORATION_INPUT_OK` 通过：实际输入覆盖设置页进出；无初始存档时实验开战、返回且不建档；六选二→地图→军团开关；真实敌阵布阵预览后取消且存档完全不变；x1 自然普通战以 `PlayerVictory` 结束；战报返回地图；重新实例化正式根后 Continue 的完整 JSON 相同；已有存档时再次进入实验并返回，Continue 仍完全相同。运行于 960×540 宿主／1600×900 SubViewport，无桌面指针；截图和 4 秒原始战斗帧见 `outputs/ui-flow-restoration-20261006/input/`。首次尝试因测试驱动在菜单入场转场屏蔽期点击设置而失败；驱动现等待 `Screens.IsTransitioning` 结束后通过，未改生产转场。Astra 只读审查未发现阻止交付的问题。该证据不覆盖三阶段自然通关、征程平衡或用户体验验收。

能力实验本身包含8条新羁绊与12个能力原型；独立内容和输入范围见 2026-10-06 恢复验收节。旧清理样板为该活动历史检查范围，不代表当前征程供给。正式规则见 [能力实验说明](../../gameplay-design/ability-prototype.md)。

转场结束后的最终主菜单、布阵和实验入口截图位于 `outputs/ui-flow-restoration-20261006/settled/`；`settled.log` 为 `JOURNEY_RESTORATION_CAPTURES_OK`。这次只接续已验证的隔离存档补看静止画面，没有重复开局或重跑战斗。

## 已执行的正确性检查

- `python3 tools/check_content_reset.py`：活动资源引用完整，仅选定的8条新羁绊与12个原型进入名单，没有恢复旧物品和供给。
- `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`：0警告、0错误。
- `tests/AbilityPrototypeContractSmoke.tscn`：正式内容发布门禁；四套真实预设的相同种子结果一致；全部12人实际施法。附带定向检查复制体独立属性／法力、不增加人数、斗士与换形同层生命、一次换形、变形后继续普攻、雷爪强化期继续攻击并传导及结束后恢复法力、施法盾在蓄势前到账、同源刷新和到期、故障注入回滚、八秒落雷一次、单刺客无激活档仍完成后排跳跃及落点不重叠、追加攻击数量有界且不递归。
- `tests/ArchitectureModuleOwnershipContractSmoke.tscn`：相关独立模块的状态归属、盾过期、延迟法力、检查点和回滚等契约通过。
- `tests/ContentResetContractSmoke.tscn`：保留四个样板仍实际施法且结果确定；原重拳队列、打断、独立实例与回滚检查仍通过。

命令使用 `/Users/qingshi/Downloads/Godot_mono.app/Contents/MacOS/Godot`，设置 `DOTNET_ROOT=/Users/qingshi/.dotnet`；逻辑入口可用 `--headless --path . --scene tests/AbilityPrototypeContractSmoke.tscn`。未跑依赖旧名单的宽泛历史回归，不宣称全仓库用例通过。

## 实机输入与观察

`tests/AbilityPrototypeInputSmoke.tscn` 使用960×540宿主、1600×900隔离SubViewport，真实鼠标和键盘事件，不移动桌面指针。检查主菜单进入、预设选择、单个位置留空、恢复阵容、正常速度战斗推进、暂停冻结、重开、返回配置与菜单，以及配置面板和12个选择控件处于可见区域。选择器用实际焦点与上下键操作，不直接调用选择信号。

日志和同视角原始帧位于 [输出目录](../../outputs/ability-prototype-20261004/)。`ability-prototype.mp4` 按帧实际采集时间编码，保持原来的1倍播放节奏；这是原型实机记录，不是最终特效交付或用户观感认可。

## 用户体验观察点

1. 启动后从「练习与预览 → 能力实验 → 八羁绊 · 能力实验」进入，先保持默认极地射手对森林换形；正常征程仍从主菜单「开始新征程」进入。
2. 比较换形前后、森林多出的身体，以及第8秒落雷对局势的影响；换形单位应继续移动／攻击，强化雷爪期间不能发呆。
3. 切换斗士圣盾或森林刺客，调整后排单位的上中下位置；检查跳后排是否使站位有意义，控制和护盾是否压缩了对手的行动空间。
4. 重开同配置比较行为；若组合没有期待感或数值悬殊，按这套能力原型调整，再确定最终素材与表现。

最终趣味、可读性、节奏和平衡仍待用户验收；临时素材并非最终身份和形态设计。
