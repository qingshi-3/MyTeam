# Battle Lab Test Cases

## 碰撞、普攻范围与巨兽通行（2026-09-21）

自动入口：`tests/BattleGeometrySmoke.tscn` 无窗口验证普攻边距阈值／地形阻挡、体型快照、确定性和1.56格通道（1.4格身体通过、旧1.8格身体被挡）；`-- --input` 使用有窗口的正式 BattleScreen 隔离实例，通过真实鼠标／键盘操作开关、点选、暂停、单步、重开和关闭，并检查实际接敌及冲锋期间的位置。冲锋旧契约入口为 `tests/EnemyTrampleContractSmoke.tscn`。

用户观察：开战后，在上方打开「碰撞」「普攻范围」，点击巨兽或其他单位。身体实线与脚下位置同步；黄色虚线触及目标身体后满足普攻距离，连线显示边缘间距与射程；技能距离独立。暂停与单步可精确看位置。诊断开启时按逻辑步显示位置，关闭后恢复平滑移动；没有常驻说明文字。检查巨兽通过可容纳1.4格身体的通道，窄于身体的缝隙仍被阻挡，不应挤穿或穿墙。单位详情仍从原按钮打开。

已检查截图 `.godot/ui-review/geometry-separated.png`、`geometry-contact.png`、`geometry-charge.png`；日志 `geometry-contract.log`、`geometry-input.log`、`geometry-trample.log` 同目录。程序检查不代替用户对体型、拥挤感与冲锋平衡的试玩验收。

## 大体型部署预览（2026-09-21）

`CurrentUiInputCapture.tscn -- --footprints` 使用隔离存档，通过真实输入放置EB02、检查重叠／边界拒绝、取消／移动／撤销、普通单位与共享正式棋盘。对应证据归 `work-items/active/battle-lab-ui.md`。`EnemyTrampleContractSmoke`保护完整身体放置契约，`FormationDragOnlyInputSmoke`保护正式部署的原生拖动命令。

玩家观察：拖入冲阵蛮兽／炉喉蜥卫／裂壳虫母时，覆盖格和身体轮廓随实际体型变化；放置后图像明显大于普通单位。移到边缘或另一身体旁，应能从红色范围和叉看出不可放置。取消／撤销后不得残留上一位置的轮廓。正式遭遇的大敌人预览与实际体型一致，可点击扩展范围查看；格子是锚点，部分覆盖不代表整格被改成方形碰撞。


## 当前：全技能状态条（2026-09-20）

基础检查入口：`tests/SkillProgressContractSmoke.tscn`（headless），日志`.godot/skill-progress-contract.log`；全发布单位两队、真实资源／周期／次数／阶段、场景绑定和读投影不修改战斗均已通过。怒拳队列、巨兽冲撞和五种精英／首领的既有规则回归也通过。未运行图形输入或视觉验收。

用户实验室观察项（待验收）：

1. 任意两队放普通法力英雄、HC38怒拳、ES01或ES04、EE03破阵巨兽与EE07–EE09。空蓝／空怒仍有条；法、怒、时文字与对应颜色一致；满资源等待合法目标／行动时有状态提示。
2. 怒拳依次跨过半血和20%生命：怒劲清空与挥拳对应，上一动作未结束时后续条件显示排队，濒死机会只消耗一次。点选详情与头顶读数一致，其他被动仍可查看。
3. EB01与EB02可放任一队：周期正常推进；虫母半血破壳后主条改为产卵，详情显示已产批次，达到上限显示已用尽。无技能假人不应出现资源条。
4. 暂停、单步、倍速、阵亡／返场、结束和重开：显示跟随实际战斗，不在暂停中自行积蓄；死亡隐藏、返场恢复当前状态、下一战重新初始化。观察大体型单位的条与文字是否清楚、是否遮挡状态。

## 当前：全单位自由测试（2026-09-20）

主菜单 → 战斗实验室 → 添加单位，默认是全部单位。搜索 **EE03 / 破阵巨兽**，选 A 队或 B 队后拖到棋盘；大型单位需给身体留空间。用同一库给另一队添加任意英雄／首领／假人；点击仅查看，拖动放置。两队均可配置装备，无人口与左右区域限制；开战仍需双方各有单位。遗物工具明确针对 A 队。

用户验收：切队保留搜索、相同单位能分别进两队；中间及两端均可放置／跨队交换；从任意队拖回库移除并撤销；首领对首领／英雄对英雄可开战；EE03 朝对面冲撞；B 队选中单位可正常拖放装备；载入旧预设仍为自由布置，战斗返回后保留配置。

自动检查：构建后运行 `Godot --headless --path . res://tests/BattleLabOpenUnitsContractSmoke.tscn`。本轮通过当前 63 种发布单位、41 个内置预设、双队装备准备／身份转移、自由放置与体型边界、旧预设校验与内存转换、多首领阶段和确定性、A 队冲撞及 UI 场景绑定。日志 `.godot/battle-lab-open-units.log`；无窗口、无截图、无存档写入。界面操作和观感待用户验收，不能以该检查代替。历史 `BattleLabPlacementInputContractSmoke` 使用冻结名册／旧双库与正式模式，不作为当前规则入口；旧日期记录仅保留当时证据。

## 属性图标与精简悬浮卡（2026-09-20）

最新词条扩展沿同一 `--tooltip-icons` 入口：HC13 的 120 护盾与盾图标均为白色；全部机制／属性词条图标可加载且能实际行内渲染，名称、图标、关联数值同色。检查“攻击 200 防御 40”、前置数值、层数、增减与生命阈值归属；“每 2 秒获得 20 护盾”的 2 秒及毒范围 1.5 格保持中性；BBCode 输入只能显示为字面文本，技能名“乘乱追击”不附加重置普攻的词条释义。新增 24 项图例明确标注为展示夹具，并实际搜索悬停 HC38 验证长技能范围；截图 `after-keyword-shield-white.png`、`after-keyword-icons-palette.png`、`after-keyword-long-skill.png` 与最终日志 `keyword-icons-input.log` 均在 `.godot/ui-review/`。最终检查通过并已查看渲染。

构建后使用带显示的 Godot：`--path . --windowed --resolution 1600x900 --rendering-method gl_compatibility res://tests/CurrentUiInputCapture.tscn -- --tooltip-icons`。此开关只运行本轮相关路径，不执行该入口历史的全界面采集；使用生产 GameRoot 和独立 tests 存档命名空间。

鼠标进入战斗实验室、打开单位库、搜索 HC08 并悬停；检查生命／攻击／防御／普攻间隔／射程／20/60 法力／+5/s 回蓝图标与数值，红框中的身份和操作教学不再出现。点击后 Tab／Shift-Tab 返回同一卡片也能读到同样信息；真实拖入棋盘后读取准备值；切到承伤假人无蓝量条目。检查图标加载、悬浮卡边界与实际渲染。日志和四张截图在 `.godot/ui-review/tooltip-icons-input.log`、`after-tooltip-icons-*.png`；本轮 1600×900 通过，用户图标辨识体验与配色选择尚待反馈。

## ES01／ES04贯穿特色兵（2026-09-16）

最新回归：ES01打满2人伤害名额后仍飞至预警线终点，第三人不重复增加伤害；最后一段有中间帧和终点帧，暂停／清场正常。命中改为受击者身体上的短擦痕，等可见箭头经过接触点再出现，不先在前方冒水波。规则入口新增完整飞行／伤害上限检查，渲染入口新增末段插值与命中时序检查；当前动态证据在活动任务最新段。

主菜单 → 战斗实验室 → 内置预设，搜索 **ES01** 或 **ES04**，分别载入“贯阵弩手／贯光术士 · 预警与贯穿”。观察超远起手、地面指示方向、源端渐亮和释放：箭逐段飞过并依次命中，光束全长亮起并只结算一次。共同上限2人，第二人衰减；第三个共线目标可被光束视觉覆盖但不再受伤。改变前后排共线／错位站位，加入控制检查蓄力取消，再看正常战斗压力是否适当。

本批 `EnemyPiercingContractSmoke.tscn` 与已有 `RangedAttackContractSmoke.tscn` 通过规则检查；`EnemyPiercingVisualSmoke.tscn` 使用正式BattleScreen和发布预设，通过实际视口鼠标暂停／恢复、动态渲染、减少动态与清理检查。运行方式为带显示Godot `--path . --scene res://tests/EnemyPiercingVisualSmoke.tscn --fixed-fps 60 -- --capture=<目录>`。录制夹具将我方快照设为静止；没有走主菜单手动载入路径，也没有整局推进。最终帧2048×1081及正常速度动图位于 `.godot/enemy-piercing-review/`，具体范围归[活动任务](../../work-items/active/enemy-piercing-skills.md)。玩家操作、观感与平衡不记成已验收。

## 场景职责拆分验证（2026-09-16）

`tests/PresentationBoundaryInputSmoke.tscn` 使用当前正式发布内容和独立内存会话；用带显示的 Godot、`--windowed --resolution 1600x900` 运行。覆盖独立飘字层的伤害聚合、状态去重、64 个节点／Tween 上限、重复清理及退树；预设搜索／空结果、Tab／Shift-Tab、Esc 焦点返回、载入／撤销、恢复默认和非法保存拒绝均通过真实引擎输入。另行实例化预设面板，验证它只发出保存意图、各实例输入状态互不污染，不写用户预设。正式 BattleScreen 检查只读快照、暂停单步、换战／停止清理及原实验室会话不变。

录制输出 `.godot/architecture-decoupling/preset-panel.png`、`battle-after-extraction.png`，需查看实际渲染；日志及本轮结果见 [架构整理记录](../../work-items/archive/architecture-decoupling.md)。`ProductionStartupContractSmoke` 验当前正式启动。旧 `BattleLabBattleLifecycleContractSmoke` 当前在冻结名册与现行项目池交叉引用处发布失败，不能视为有效通过；本轮没有运行全量回归或完整战斗推进。

## 当前UI修复：已执行的输入与渲染检查（2026-09-13）

运行 `dotnet build my-team.csproj -maxcpucount:2 -v:minimal` 后，以带显示的Godot运行 `res://tests/CurrentUiInputCapture.tscn`（不加`--headless`）。该入口使用生产GameRoot与隔离存档，通过引擎鼠标/键盘/滚轮输入完成选英雄、实验室双侧展开/折叠、单位属性技能、悬停、预设载入、团队页、战斗暂停/点选详情/完整词条滚动、统计展开/收起。当前1600×900通过并输出13张截图，主责已核对渲染；证据及限制见[活动任务当前段](../../work-items/active/battle-lab-ui.md)。下方旧批次“未验证”保留为其历史范围。

后续重点：切换长技能单位后正文应回到顶部；生命/攻击/防御与攻速含义清楚；正文能滚到最后一行；展开侧栏不挤掉顶栏或开战按钮；无指令的实验室战斗不显示两块空指令卡。真实用户预设与存档不得作为自动检查的写入目标。

## 首版三体系：当前体验入口（2026-09-13）

主菜单 → 战斗实验室 → 预设，搜索 **NE01／NE02／NE10**。当前为28名可用英雄、30个可选预设；下方旧批次数量及“不用验证”只对应原阶段。

1. **NE01 棘毒防守**：观察前排争取时间后，棘毒使持续施毒并增加已有毒量，敌人持续掉血；比较核心过早倒下与获得供能、防护后的差别。
2. **NE02 砂骨殉爆**：观察送葬人开场召唤砂骨仆，召唤物死亡后在尸体附近爆炸；送葬人存活时提供临时友军死亡监听，离场后不继续替后续召唤物触发。比较召唤物接敌位置和生存时间。
3. **NE10 霜羽连射**：观察连射在本次选中的最多三名敌人间分配，成功冰冻后获得护盾。寒羽基础概率 25%，霜羽同盟 2／4 名成员时为 35%／50%，基础时长 0.8 秒，受控制抗性影响；无寒意、减速或叠层。徽章提供成员资格与攻速，不直接授予寒羽。对照单人、2 人、4 人配置；同一单位原生与徽章不重复计数，超过 4 人仍不超过 50%。

已通过低并发构建、正式启动检查，以及[机制检查](../../tests/FirstContentSystemsContractSmoke.cs)和[实际预设战斗检查](../../tests/FirstContentPresetsContractSmoke.cs)。具体种子、输出及修复记录见[活动任务](../../work-items/active/bc-hero-roster.md)。这些是无画面的规则检查，不代表界面、箭矢／爆炸观感、节奏、平衡或整局随机供给已经验收；上述三项仍供用户实际体验。

## UI 与交互优化：用户观察入口（2026-09-12，待体验）

本轮界面与编辑交互实现已整合，按用户“不用验证”要求，未构建、未运行自动测试或真实交互、未启动 Godot 或渲染验证。以下供用户体验的观察点**不是已执行结果**；实际完成范围与恢复状态见 [活动任务](../../work-items/active/battle-lab-ui.md)。下方此前任务的命令、自动覆盖和历史回归安排保留其原日期／阶段范围，不自动成为本轮执行清单。当前名单还包括 HC04–HC30 与新增 27 个 BC 预设，旧 HC01／HC03 小批记录不代表全量现状。

1. 主菜单 → 战斗实验室：左侧单位库、中间 10 × 6 棋盘、右侧检查区清楚分开。滚动单位、装备或详情时，棋盘和底部开战／就绪状态仍可见。
2. 在我方／敌方库间切换和搜索；单击单位后可连续布置到格子，或直接从单位库拖放。短距离按下／松开只选择，达到 8 像素才进入拖动；头像、名称与阵营帮助辨认单位，正式模式的部署区域边框与提示在自由模式中不作限制展示。
3. 选中棋盘单位后点击空格移动，也可拖动交换或移除。Esc 取消当前布置／拖动；键盘按钮激活路径能选格和布置。Delete 删除选中单位，但在搜索、种子或预设名称输入框内编辑时只编辑文字。
4. 使用棋盘附近的清空与撤销：清空只移除单位、保留遗物；成功编辑和预设加载可在 32 步内撤销，失败操作不增加撤销步骤。撤销属于内存会话，不影响真实存档。
5. 右侧“单位”页显示所选单位；装备沿用既有 Session 资格，我方单位（包括假人）可以配置，敌方不适用。常用属性包括防御，HC01 技能介绍可看到叠速被动及当前换目标保层覆盖，不必打开内容 ID／追踪细节。
6. 切换“团队”页调整遗物，回到“单位”页仍保留原单位选择；团队遗物与单位装备的归属一眼可辨。
7. 打开预设浮层，用名字或 BC 编号搜索可见列表，查看候选摘要，双击或用键盘载入；Tab 焦点停留在浮层内，错误反馈也留在浮层内。内置／我的预设可区分，原 4 个与新增 27 个预设仍可访问，顶部显示当前配置名；保存拒绝与内置预设重名。
8. 在“设置”修改种子，再应用、保存或直接开战，三种路径使用同一输入值；非法输入有明确原因，不能悄悄按旧种子开战。
9. 底栏持续显示能否开战与首要阻塞原因。准备完双方单位后从底栏进入战斗，返回配置时布置、装备、遗物、种子与选择行为一致。
10. 打开与收起技术细节、反复切换标签／预设浮层，不产生重复条目、卡住的拖动或焦点丢失；界面信息密度和操作节奏仍待用户实际反馈。

## 设计英雄与假人（2026-09-12）

本批正式名单为 HC01 连弩手、HC03 铁甲卫，另有可放在双方的静止／近战／远程假人。旧英雄与冰霜预设相关回归已冻结为 `tests/fixtures/legacy-roster/`；下方历史冰霜用例不再是当前正式预设要求，也不因本次小批接入重跑历史整套验收。

当前针对性入口：先 `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`，再串行运行 `Godot --headless --path . res://tests/DesignedContentContractSmoke.tscn`、`DesignedContentInputSmoke.tscn`、`BattleLabAppFlowContractSmoke.tscn`（后两项使用同样的命令形式）。渲染检查使用 `DesignedContentInputSmoke.tscn` 去掉 `--headless`，检查 `.godot/designed-content-input.png`；不得将无渲染输入断言当作画面验收。具体已执行证据见 [活动任务](../../work-items/active/designed-heroes-and-test-dummies.md)。

用户体验路径：

1. 主菜单 → 战斗实验室 → “连弩手叠速”：观察普通箭、三箭连射、追猎层数与攻速增长；主动暂用半普攻间隔，判断箭数与施放占用是否合适。
2. 选择“连弩手升阶换目标”：选中弩手，切换“升阶测试：换目标保层”，对照换目标时层数清空／保留；返回配置再开战，叠层应从零开始。开关仅测试 U01，不表示已完成培养取得流程。
3. 选择“铁甲卫嘲讽反伤”：观察近处敌人转火、合法寻路、3 秒增防期间反伤提高、结束后回落；远程普攻也反伤，技能箭不触发，围攻仍可能击倒坦克。
4. 从双方单位库拖入静止、近战、远程假人，改变数量和距离；静止假人不行动，攻击假人只有基础普攻。旧英雄不再出现，假人不会出现在正式招募。

自动检查覆盖资格、归属、回滚和基本输入；数值均为测试暂值，玩家尚未验收射击节奏、强度和乐趣。

默认箭显示回归：`DesignedProjectileVisualSmoke.tscn` 使用正式 BattleScreen 与当前设计英雄／假人，不访问存档。以 `--fixed-fps 60` 运行，追加 `-- --capture=<输出目录> --stride=1` 可录制 60 fps 全帧；默认 stride=4 为每四帧一张，追加 `--speed=4` 检查加速。`trace.csv` 同步记录渲染帧、战斗步、角色动画帧和箭位置。检查普攻和连射的拉弓 → 松弦 → 飞行：当前 f1_ranged 应到零基第 12／15 帧附近才出生箭，增长后动作和出箭一起变快，满蓝转技能不应在刚放箭时重播拉弓。箭飞行应有两次战斗步之间的中间位置，暂停冻结、单步跳至权威位置，命中／结束不留悬空箭。自动检查另覆盖释放即完整显示、右／左／斜向、移动后朝向、减少动态及清理；行为入口 `DesignedContentContractSmoke` 检查前摇不提前发箭／伤害、释放时刻及控制／超距取消，`RangedAttackContractSmoke` 保留碰撞规则回归。不能用伤害断言代替实际可见或用户手感认可。可选 `--before` 仅用于 `.godot/arrow-fix/before-projectile.tscn` 旧能量弹外观快照；本次时序修正的旧版基线是 `.godot/arrow-fix/after.mp4`，证据见活动任务。

## 嘲讽范围与负面状态

HC03 嘲讽特效入口：`DesignedTauntVisualSmoke.tscn` 使用当前正式英雄／假人和 BattleScreen，私有配置将盾将初始法力调满、关闭后续回蓝以录制单次施放，不写真实存档。`Godot --path . --rendering-method gl_compatibility --fixed-fps 60 res://tests/DesignedTauntVisualSmoke.tscn -- --capture=<目录>` 输出 390 张帧图；`--before` 仅隐藏本轮两个特效供同场景对照，不是历史实现。自动检查覆盖同源半径、非等比斜投影、圈内／圈外、真实状态绑定、可见位置跟随、刷新、到期／死亡／来源失效、减少动态及实际视口暂停／恢复输入。玩家从“铁甲卫嘲讽反伤”预设观察：边界在施放时短留，圈退后受控者头顶侧边仍有红色怒意符号，未受控者没有，解除后消失；预览室两项与正式战斗共用资源。不要以截图／断言代替用户观感认可。

## Stage 0 And Core Contracts

- The initial RED scene must fail only for named missing Lab capabilities while the existing build and completed-platform regressions remain unchanged.
- Compare the pre-extraction Run preparation and the shared assembler adapter for fixed Run/Encounter fixtures: complete `BattleConfig`, spawn cells/identities/snapshots, floor rule, Equipment, Relics, Traits, commands, summons, Boss timeline, seed, identity, and deterministic battle digest/result must match.
- Publish the Lab index from `CompiledGamePackage`; every player hero, legal PvE unit, Equipment, and Relic must derive from typed published metadata. Elite/summon membership cannot use concrete ids, paths, or id prefixes.
- Fingerprint the complete authored/compiled Resource graph before edit, preset round-trip, battle, reset, and exit. Every fingerprint must remain unchanged.
- Use recoverable isolated production-save fixtures and save-service call counters, never migrate or overwrite actual player saves for this check. Opening, editing, starting, completing, resetting, preset saving/loading, exiting, and re-entering must add zero production-save calls and preserve fixture byte/semantic identity for the currently supported format (v6), independently of the Lab preset schema.

## Placement And Real Input

1. At both supported resolutions, enter `战斗实验室` through the real main-menu button. Drag one published player hero and one published PvE unit from their library cards to legal cells using `Viewport.PushInput` mouse motion/press/release events.
2. Reposition both pieces; swap occupied cells; recall each piece to its originating library; explicitly delete a piece; clear one side and clear all. Each successful edit changes the canonical configuration once and every copy retains a unique Lab instance id.
3. Attempt out-of-bounds, forbidden, occupied non-swap, wrong-side formal cells, population overflow, duplicate-cell, missing-content, and same-source drops. Every rejection is non-mutating, deterministic, and exposes a concise Chinese reason plus a non-colour shape/icon/motion/cursor signal.
4. In formal mode, verify player columns `0..2`, the production enemy region, current/effective population, floor legality, and one-unit-per-cell. In free mode, place either side at both board extremes while bounds/forbidden/occupancy remain enforced and `自由实验配置` stays visible.
5. Cancel a drag, release outside the board, switch mode during selection, reset, leave, and re-enter. No hover state, focus target, subscription, Tween, node, or session mutation may leak.

## Equipment, Relics, And Inspection

当前 UI 扩展的手动观察项（尚未执行，不表示验收通过）：

- 悬停或用 Tab 聚焦单位库与棋盘单位：前者显示基础属性，后者显示当前装备／羁绊准备后的数值；主动、被动、HC01 保层状态与对应实例一致。查看装备／遗物候选和已装槽位时，显示效果、对象与条件，而不是内容 ID。
- 分别收起左右栏并再次展开，搜索、选择、当前页与装备配置保留，棋盘使用释放的空间。F1／F2 不在输入框内抢按键；收起时焦点回到可见入口。点击棋盘单位可打开详情，常规刷新不得自行展开。
- 战斗侧栏展开／收起及切换伤害、承伤、治疗视图，不改变暂停、速度或战斗结果；统计排序和数值来自当前战斗事实，选中行打开对应单位。结束／重置后不得遗留旧统计或悬浮提示。
- 拖拽、打开预设浮层、隐藏侧栏、返回配置后，悬浮卡及时消失；卡片不遮挡点击与拖放，不把 hover／焦点当成选择或开战操作。

1. Equip three slots on one concrete player instance, replace and remove an item, and reject a fourth slot. Equip the same definition on a second hero and prove distinct Equipment/source instance identity. Enemy selection must show equipment as not applicable and expose no editing control.
2. Add, remove, and change positive player-team Relic stacks through production legality. Reject zero/negative stacks without inventing a maximum. Enemy instances receive no Relics.
3. After every edit, assert atomic refresh of player count/population, per-hero Equipment, team Relics, Trait contributions/tiers, prepared health/damage/attack speed/reach/control resistance, readiness, and all Chinese failure reasons.
4. During battle, inspect multi-source Status facts including definition, stacks, remaining duration, source ids, and source contributions through the formal read-only runtime snapshot.

## Battle Lifecycle And Determinism

1. Start through real UI input. Pause, continue, step exactly one fixed tick while paused, and select x1/x2/x4. A step must advance one simulation tick, consume/present its events, refresh inspection, and handle terminal state once.
2. Run the same canonical configuration and seed twice. Terminal outcome/tick, deterministic event projection, digest, movement order, Status facts, and report facts must match.
3. Return to configuration after partial and terminal battles. Placement, mode, population, seed, Equipment, and Relics remain identical; damage, Statuses, cooldowns, counters, and runtime modifiers are absent from the edit session.
4. Reset during running, paused, and terminal states; start again; leave and re-enter. Every Battle scope reports zero retained subscriptions/reactions/runtime entries/Tweens/Nodes.

## Presets And Frost Validation

- Round-trip versioned user JSON containing stable ids, unique instance configuration, cells, Equipment instances, Relic stacks, mode, population input, and seed. Reject unknown versions, invalid ids, duplicate instance ids/cells, illegal stacks, and malformed JSON without partial publication or Resource serialization.
- Load `冰霜体系验证`. It must contain two Equipment-capable player units with two independent `霜痕战刃` instances, an active `凛冬盟约` tier, a normal high-health target, and a published development-only target with non-zero authored control resistance that is absent from campaign pools.
- Observe attack-speed growth, two-source Frost attribution, three-stack conversion, shorter Freeze on the resistant target, and apply/stack/expire cues. Source guards must prove these content ids occur only in preset/authored data, never generic runtime dispatch.

## Serial Verification

### Animation Audio (2026-09-15)

Combat mix follow-up: `tests/CombatAudioMixSmoke.tscn` exercises cross-cue group budgets, higher-priority replacement, real player volumes under highlights, UI exclusion, pause/resume, capped-tail fade and rejection without deferred explosions. The fixture uses quiet synthetic tones; it verifies runtime mixing behavior, not subjective battle sound quality. Main-flow `AudioFeedbackSmoke` and the animation preview input check also cover the adjusted authored sounds. No extra damage/pain event audio is introduced.

`tests/AnimationFrameAudioSmoke.tscn` checks real sprite-frame emission, unequal frame durations, queued/restarted/timed actions, pause, cancellation, loops and all authored unit markers. `tests/AnimationAudioPreviewInputSmoke.tscn` uses rendered SubViewport GUI input to select a lab unit, open animation audio, edit its frame, replay, inspect silently and reopen. It checks the requested frame against the actual sprite frame, non-silent mixed PCM, cleanup and shared-resource isolation. The dedicated wide-action fixture checks clipping geometry. `tests/AudioFeedbackSmoke.tscn` covers legacy action suppression, victim-owned death markers, retained impacts, actual campaign battle output and pause/cleanup. These focused checks passed for this change; they are not a subjective listening acceptance.

Manual follow-up: choose a melee unit → 动画音效 → step to the weapon action → adjust 在第几帧播放 → 重播. Check the selected sound's attack onset against the pose. Close/reopen must restore the authored marker; permanently change the unit's FrameSounds resource in Godot. Compare normal battle and lab battle using the same unit. High-frequency identical sounds may be dropped by the bounded mixer, never queued late.

Run the focused Lab core and real-input scenes first, then the existing seventeen-scene completed-platform regression in this exact order: ScalableAuthoring, ProjectComposition, Relic, AbilityStatus, EffectKernel, Content, Fixture, FormationDeployment, Gameplay, MovementPresentation, AlphaRun, Ui, GameUiInteractionReliability, DeploymentInputHeroSelection, SemanticPresentation, GameUiVisualLanguage, VisualHierarchy. Run `CleanStartup` separately afterward, followed by the low-concurrency build, source guards, `git diff --check`, production-save fingerprint comparison, shared-Resource fingerprint comparison, and process audit confirming user Godot PID `23260` was not controlled.
