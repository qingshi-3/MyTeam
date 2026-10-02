# Alpha Manual QA

## 全局资源栏（2026-10-01，专项通过、体验待确认）

查看顶部右侧资源栏：手绘灰石横托应与卡框材质协调，角石没有被压扁；图标和数字应在同一基线，标签位于下方，生命条不挤压标签或石边。检查右侧“军团”是否容易辨认，鼠标按下后松开及键盘 Enter 均打开既有军团窗口，Esc 关闭后资源与遗物陈列恢复。生命低于等于 30% 时数字、图标和短生命条显示警示色，同时带“危急”文字；恢复后生命条回到普通样式。此前圆角扁平底板为用户否定的版本，不作为当前材质验收依据。

`RunRelicShelfInputSmoke.tscn -- --header-review-only` 已通过隔离 GUI 输入、基线／边距、长数值、警示样式恢复及不修改征程数据检查。低生命与长数值是只读展示夹具，未冒充自然获得。实机图、范围和日志见[证据](../../outputs/run-header-20261001/README.md)。文字大小、栏内留距和质感待用户确认。

## 路线页与共享 UI（2026-09-30，专项通过、体验待确认）

查看顶部紧凑资源、军团入口与遗物；悬停遗物提示应在陈列下方展开，不挡中心标题；从右端图标跨过空隙移向提示应继续保持，便于阅读和滚动，移出阅读区域再收起。三个节点同屏比较机会、风险和失败代价，整卡可点击；按下后移出并松开应取消，键盘 Enter 应提交一次，减弱动效应保留正常命中位置。单 Boss 卡居中且常驻显示终局代价。进入精英比较，候选正文／收益应在边界内，右侧敌军详情可完整阅读，返回不提前选择节点；事件／商店标题和说明应容易读。通过军团开关及进入实际战斗继续检查共享顶栏和提示恢复。

`TowerLayoutInputReview.tscn` 与 `RunRelicShelfInputSmoke.tscn` 最终隔离 GUI 输入通过。录制、实机图、测试夹具与退出告警的限制见[证据](../../outputs/ui-overhaul-20260930/README.md)。观感和按压重量仍待用户体验确认；没有把功能通过写成整套审美验收。


## 全局遗物陈列（2026-09-30，专项已检查、体验待确认）

进入拥有遗物的征程，查看顶部左侧陈列；悬停图标，检查名称、稀有度、持有层数、作用对象和完整效果。鼠标移开应收起；键盘聚焦可阅读，Esc 可关闭，点击不会消耗或改变遗物。同类遗物显示总层数，数量较多时横向滚动，键盘应能到达最后一项。打开／关闭军团或配装窗口，检查陈列被覆盖、旧提示清理及关闭后的恢复。

路线、机会、备战、正式战斗和继续征程的战报均应可查看；实验室及其战斗不混入征程遗物。切屏多次后顶部不持续下移，陈列、资源和页面操作不重叠。`tests/RunRelicShelfInputSmoke.tscn` 使用真实内容、受控拥有状态和隔离 GUI 输入，完整专项与 `--tooltip-copy-only` 补验已通过；覆盖及预览见[证据记录](../../outputs/run-relic-shelf-20260930/README.md)。没有把夹具当作自然获取或通关，最终图标密度、提示位置和阅读舒适度由用户确认。

## 默认成长主流程、独立奖励与战前法术（2026-09-30）

入口为默认主菜单“开始新征程／继续征程”；旧Alpha及旧档在“练习与预览→旧版征程（开发）”中保留。

1. 六选二后进入当前路线。普通战点击后直接整备，能查看实际敌情并进入法术工坊，不出现具体选敌面板；精英战才比较完整候选、压力、收益及失败代价，确认后进入整备。Boss当前固定必经。
2. 普通胜利基础收益直接取得；装备、遗物按实际类别独立选择或跳过。先领装备再继续读档，仍应只看到原剩余遗物组，不重发基础收益或重抽候选。领取一组不得消耗另一组名额。
3. 精英选敌返回／重进候选不重抽，确认与继续游戏恢复同一实际敌阵。旧v9已锁普通选敌亦应保留原敌阵与收益，新普通战不新增该机会。
4. 整备“战前法术”直达工坊。自然研究来源为研究英雄节点产出及部分精英胜利奖励，不能假定任意首战后都可施法。费用护阵／蓄势4、破阵5、压制3；预设或取消不消费，实际开战减一份库存，本场自动执行，无战中指令。
5. 以不同开局试玩，记录种子、所选敌阵、法术投入、胜负与资源；评价奖励类别／法术供给频率、信息是否好读及法术是否值得留存。新外层主循环仍待R10回接，本次不把旧三选节点循环当作节奏验收。

合同入口：`OpponentChoiceContractSmoke.tscn`、`GrowthRewardGroupsContractSmoke.tscn`、`GrowthRunContractSmoke.tscn`；法术真实效果沿`GrowthBattleGainSmoke.tscn`原证据。真实输入`GrowthEntryInputSmoke.tscn`从ProjectSettings的默认主场景启动，在960×540隔离视口完成新局入口与seed2合法开局档继续、首普通实战、独立领奖／中途读档、休整／事件自然生产3研究、精英选敌、兑换／取消／预设、第二场真实开战。无资源或战果注入，不移动桌面鼠标，不覆盖正式存档；seed2路线不是任意随机开局的可用法术保证。实际结果及剩余验收见[活动任务](../../work-items/active/opponent-choice-and-prebattle-spells.md)。

## 新征程重新抽取（2026-09-29）

1. 点击“新征程”，勾选一至两名候选，返回菜单，再点“新征程”：重新随机六名候选、勾选清空，确认按钮禁用；随机允许不同局出现相同英雄。
2. 勾选两名、返回菜单，再点“继续征程”：保留原候选和勾选；关闭后重进再继续也应保持，确认进入首战备战。
3. 保存失败夹具中，新建失败须保留原征程／候选／选择，确认失败须允许重试。

聚焦入口为 `RecruitmentOpeningContractSmoke.tscn -- --opening-only`；无头模式检查开局契约，带渲染模式额外通过隔离 `SubViewport` 执行点击和键盘输入，宿主固定 960×540，不移动桌面鼠标、不写玩家存档。本轮执行结果见[活动任务](../../work-items/active/recruitment-opening-and-tiers.md)。

开局样板接入另用 `UnitCardDisclosureInputSmoke.tscn -- --opening-only --growth-opening` 检查正式成长候选。六张卡应完整同屏；悬停石框与立绘同步放大，实际图标命中跟随画面；核心数值保留，边缘的属性、技能、成长及羁绊仍能逐项读取。选中通过高亮和底部头像席位表达，没有对号和常驻解释；点击席位取消，保存失败保留原选择。补查连点、Esc、键盘和运行中减弱动效。实录与当前验收状态见[开局接入证据](../../outputs/opening-adaptation-20261001/README.md)。

## 全页面视觉统一（2026-09-28，本轮已检查，观感待试玩）

保留认可灰石框与立绘，统一资源栏、标题／正文、机会卡、备战席、英雄属性、表单和工具窗口。`UiClarityReview.tscn -- --polish-after` 与 `UiAuxiliaryReview.tscn` 实际复核 41 个页面／展开状态，覆盖主流程、英雄装备／军团／工坊、战报四页签、实验室七个窗口、两战练习和特效预览工具；前后截图、输入范围和已知退出清理提示见 [UI 活动任务](../../work-items/active/ui-material-overhaul.md)。`RosterLoadoutInputSmoke`、`GrowthWorkbenchInputSmoke`、`RunHealthUiInputSmoke` 通过真实点击、键盘或拖放；普通单位拖放断言完成，大体型因宿主停发绘制帧改用聚焦入口补验通过。所有桌面宿主为 960×540，输入隔离，不移动桌面鼠标。夹具页面不能算自然全流程通过。

玩家复查：

1. 正常进入部署，辨认顶部资源、首领警告和空备战席；拖动普通与大体型单位，观察比例、跟手、落下与取消，原位淡影应保持。
2. 打开“英雄与装备”，切换英雄、展开技能说明并换装；检查全部 19 项属性、长技能和背包易读，法强／暴击不遗漏，背景不抢视线。
3. 查看机会卡、路线、战报、工坊及工具窗口，确认字号、间距、主操作层级统一。整体质感与弹性节奏仍由玩家确认，未记作审美验收。

## 独立成长征程闭环（2026-09-27，专项已执行、体验待试玩）

本项使用独立 `GrowthGameRoot`、成长内容包和存档命名空间，不改变 Alpha 存档。当前是 26 名代表内容的可玩包，不宣称 49 名矩阵英雄全部完成成长重构。玩家入口为主菜单“成长征程 · 工坊与升阶”，进入后从军团面板打开“成长工坊”。以下检查已实际执行：

- `tests/GrowthWorkbenchInputSmoke.tscn`：隔离存档及 `UiInputStage` 真实点击／键盘，检查生产分配、支付升阶后的持久三选一、研究法术预设及开战前不消费。
- `tests/GrowthEntryInputSmoke.tscn`：真实主菜单、开局六选二、首战部署、军团／工坊、Esc焦点返回及开始战斗，使用隔离存档；已查看实际渲染截图。
- `tests/GrowthRunContractSmoke.tscn` 与 `tests/GrowthBattleGainSmoke.tscn`：保存失败与重试、固定三选一、混合材料、开战冻结／一次消费、真实冻结永久收益、后续战斗投影、重复提交与终局、深拷贝和v7迁移。最近既有 `RunHealthContractSmoke` 也通过。
- `tests/GrowthJourneyDiagnostics.tscn`：从真实供给开始、逐节点推进和重载，运行 `BattleSimulation`；不注入金币、材料或成长，也不伪造胜利。包含生产、研究、战斗、晚来和缺投入对照；测试专用敌人 profile 与同存档反事实应和正式连续路线分开解读。具体参数、结果和复现见[运行证据](../../design-discussion/04-content-validation/artifacts/growth-route/runtime/README.md)。

执行时核对：攻击／生命培养按受益友军原始值 `4%` 固定累加，研究为 `+1`；每个非终局节点材料 `+1`，升阶花费 `4`，失败不扣料；固定三个发现候选保存后不重抽；研究 `4` 兑换法术，预览和取消不消费，真实开始战斗才消费且异常重试不二次扣除。检查 MX01 每战首次成功冻结只回存一次 `+16` 最大生命。普通／精英败战在全局生命未耗尽时继续，Boss 败战停止。

逐节点记录实际阵容、敌方、胜负、全局生命、资源和成长账本；战斗记录单位伤害、治疗、控制施加、承伤、护盾吸收与技能释放次数。现有遥测不能完整归因控制持续时间，结果必须注明。参数均为当前可调验证基线；自然供给强度、路线节奏与玩家取舍仍待结果和试玩，不能从入口存在推断验收通过。

## 阶段难度与构筑门槛（2026-09-26首轮）

自动入口：`EnemyDifficultyContractSmoke.tscn` 检查曲线、模板、预览／正式站位、Boss与生命结算；`EnemyDifficultyCalibration.tscn` 生成固定预算的真实模拟，不作为自然整局胜率；`EnemyDifficultyUiInputSmoke.tscn` 检查遭遇详情→键盘开战→鼠标暂停。数据和复现细节见[校准记录](../../design-discussion/04-content-validation/artifacts/difficulty/README.md)。本轮已运行相关专项，并查看实际预览／战斗截图。

玩家复查：以不同开局正常选牌，观察第6–9层零散投入是否逐渐难以维持，战报能否读出承伤／启动／输出不足；普通败战后能否利用余血和剩余节点补强；第二Boss是否检验了主配合；成型进入末区是否有明显收益。遇到第14层精英先按敌阵／地形调整站位或功能位，记录是否仍只有单一路径可胜。最后观察霜羽打最终Boss是否过长。需记录种子、实际取得的物品／英雄、层数和选择机会，不用实验室指定装备的胜利替代自然供给体验。

## 全局生命、败战续行与营火（2026-09-26）

专项入口：`tests/RunHealthContractSmoke.tscn`（headless）、`tests/RunHealthUiInputSmoke.tscn`（渲染，自动隔离视口与存档）。前者覆盖正常／精英／Boss胜败与超时、归零、指令金币、遗物计数器、回滚／重复／终局恢复、回血封顶和互斥、v6迁移备份与v7缺字段拒绝；后者用真实鼠标／键盘输入检查战报继续、下一场开战、营火错误重试／恢复／满血禁用／金币选择及Boss终局。受控战斗结果不代表自然通关或平衡通过。

玩家复查：初始100生命时故意输普通／精英战，分别剩80／70；无胜利奖励，但下一层仍可调整和满英雄生命开战。低血进入营火，选择回血实际增加至多25，不能同时领金币；满血时恢复禁用。Boss前应明确看到失败直接结束的提示，满全局生命败战仍终局。检查资源条、战报和继续游戏后的生命一致。重点试玩扣血初值、恢复机会与构筑补强窗口是否合适；这部分不据自动检查验收。

## 拖动物件动效 · 当前检查

战前部署直接拖动带单位素材的格子：素材格中心跟随鼠标，跨格／到边缘时不绕开落点跳位，棋盘不另画拖动区域；放置／交换／拒绝由素材格上的符号表达。普通单位以当前部署格宽高为基础，大体型按身体直径映射到棋盘投影，不再使用固定方形名字牌。拿起围绕中心等比弹大（100% → 118% → 112%），放下收回目标尺寸并轻微回弹；原位置保留淡影，取消返回、撤回后备均等比收缩。装备库存／槽位和实验室单位库／棋子继续使用已有拿起、跟手倾摆和落点反馈。玩家复查：宽高是否一起变化、放下是否自然收回，跨格是否连续跟手，交换与非法位置是否好认，Esc 是否只取消一次，快速连续拖动是否出现残影或来源持续变暗。开启“减弱界面动效”后，缩放和返回飞行应立即停止，拖放功能保持。大体型检查使用正式裂壳虫母的身体／立绘数据与内存 UI 夹具，不代表开放 Boss 招募或改变占位规则。

自动实输入入口：`FormationDragOnlyInputSmoke --isolated-pointer`、`RosterLoadoutInputSmoke --isolated-pointer`、`UiDragMotionInputSmoke`、`LabDragMotionInputSmoke`。后两者默认隔离，覆盖提交拒绝／非法目标／连续重拖／减弱动效／隐藏清理，以及实验室放置／交换／Esc／撤回／装备模板取用和移除。都使用隔离视口或内存数据。Godot 桌面窗口使用 960×540，`UiInputStage` 直接限制窗口模式和大小，内部视口可独立采样；不移动用户桌面鼠标。旧最大化窗口入口不作为本轮默认检查方式。实机动图和日志见 [UI 活动任务](../../work-items/active/ui-material-overhaul.md)；不等于玩家手感认可。

## C 英雄检视台 · 当前路径

备战“英雄与装备”及右上军团入口均为左侧头像列表／中间选中英雄／右侧技能属性与背包。背景暗化且输入为模态；军团总览从标题栏切换。右侧完整 19 项、卡面六项及暴击对都保留零值。点击属性／技能在右侧展开，返回恢复来源焦点，立绘保持可见。后备与无战场投影的军团显示自身及装备初始化值，明确排除其他开战条件；备战出战单位仍为完整准备值。

`RosterLoadoutInputSmoke --isolated-pointer` 已验证原生点击／键盘／拖放、只读查看、穿戴替换转交卸下、配装期间说明刷新、长列表、19 项不裁切、非零法强与暴击、模态关闭及全局共用入口。1600×900 可见立绘高度占 60.9%。日志和实机图见 [UI 活动任务](../../work-items/active/ui-material-overhaul.md)。

玩家复查重点：左侧识别与切换是否顺手，右侧长技能是否好读，暗化背景是否让信息主次清晰，物体材质与统一感是否符合批准稿。下方旧多卡／翻面路径是历史记录。本次未重跑全局通关或全分辨率测试。


## 每战满血与战外生命上限（2026-09-25）

自动入口 `tests/RunFullHealthContractSmoke.tscn`（headless）使用正式内容与内存存档，覆盖旧伤势、零生命、生命上限加成、阵亡后再部署与下一战、休息保存失败回滚／单次发奖、事件成败及旧默认待决项。只验证明确构造的结算服务输入，不宣称自然战斗获胜。模拟本体仍支持受伤起点和局内当前血量。

实输入入口：`RosterLoadoutInputSmoke.tscn -- --isolated-pointer`；`MainFlowUiInputCapture.tscn -- --offers-only --materials --health-rules --isolated-pointer`。复查整备与完整详情只有生命上限，后备不再显示伤势百分比；满血规则按最终上限初始化，不丢装备／遗物加成。原英雄伤势恢复已删除；现营火另有独立全局生命恢复与金币选项（见本页新检查），事件失败不扣血，实际确认仍只结算一次；加载旧待决项不得重抽或提前发奖。战斗实时血条与战报生命事实仍正常，正常全流程平衡未据此通过。

## 立绘接入与材质替换（2026-09-25）

石环与信息区复查：人物裁在环内，名称纸条和内圈不能盖脸。攻击与生命上限贴合姓名纸条两端，阶位／状态位于上角，不出现剩余值、百分比或血条；心形数值只表示生命上限，交叉武器／长椅和星标可通过悬停／键盘焦点识别状态与阶位；主被动各为一行居中图形＋技能名称，字号有主次，不套独立铭牌，点击／键盘仍进原详情。三个卡内装备位在共同底区集中排列，用浅轮廓提示位置，加号与已装备图标不能压住底部角石；槽位悬停、选中、焦点和拖入反馈仍应可辨，拖动预览保留物品名称，背包物品格保持独立。卡面不重复“生命上限”“主动／被动”“装备”等标签，槽位序号留在可访问名称中。心形点击打开详情，返回恢复其焦点。检查整卡一体感，而非逐字段增加装饰。`UiPortraitPresentationSmoke --review-cards` 可输出全部 28 张真实卡框内的取景页，文字为夹具占位；大图高度扣掉石环上缘与纸条，开局扣掉环的上下缘。

大立绘当前检查：招募正面、整备正面和开局候选的立绘可见高度至少占整卡 3/5，名称纸条遮挡不计入图高。招募技能／详情按钮打开完整正文，固定名称与确认按钮，Space 可返回；整备属性／技能打开同卡完整详情，更多属性可展开，名称和返回固定、装备槽保留，返回恢复原技能焦点。检查卡片尺寸不跳变、只读查看不改变征程；战斗继续像素动画。

用户使用桌面时，在下面两个实输入入口末尾加 `--isolated-pointer`，窗口启动加 `--position=-4000,-4000`。该模式用测试专用 SubViewport 渲染原生产场景并通过 `PushInput` 驱动点击、键盘、滚轮与原生拖放，不移动桌面鼠标；图与 tooltip 使用该视口坐标。它不代替操作系统窗口焦点／鼠标捕获的手动验收。只将普通窗口移出桌面仍可能受到系统鼠标事件干扰。

`tests/UiPortraitPresentationSmoke.tscn` 检查场景 opt-in、当前动画身份映射、缺图动画回退、默认动画播放、独立裁剪材质和重绑定清理；渲染窗口加 `-- --review` 输出全部已使用原图／裁剪对照。`MainFlowUiInputCapture.tscn -- --offers-only --materials` 检查实际菜单、开局六选二和独立 offer 夹具；`RosterLoadoutInputSmoke.tscn` 检查整备原生选人、穿戴／替换／转交／卸下、提示、滚动和模态焦点。最新日志、截图与品质未达项见 [UI 材质活动任务](../../work-items/active/ui-material-overhaul.md)。

玩家复查：在招募与“英雄与装备”查看角色面部、兽形头部和装备标志是否清楚；对照战斗，角色身份应一致且战斗继续像素动画。军团斗士、怒拳斗士、铁钩机兵目前缺立绘，保留原动态肖像。此专项通过不代表自然通关、全屏幕审美或正常速度动态对照已验收。

## 血鼓与医师身体覆盖施法（2026-09-16）

自动入口：`tests/SourceCastVfxSmoke.tscn` 检查正式技能事实、贴帧/翻转/变换、材质隔离及生命周期；`--reduced` 渲染浅底减少动态，`--battle` 使用正式 BattleScreen，`--preview` 以鼠标/键盘事件检查正式特效预览室选择、暂停、单步、倍速和重播。`HealingReaderVisualSmoke.tscn` 保留双目标不重复、普通治疗动作次数及暂停/恢复检查。日志和正常速度对照见 VFX 活动任务。

玩家复查：预览室搜索“血鼓”或“回春”；血鼓应出现贴近胸腹的短促红色能量释放，医师应出现绿白上升柔带，人物仍为原 idle。检查覆盖有无错位、体型是否仍清楚、释放是否可辨；正式战斗中双目标治疗只亮一次源端。源光强度、混战可读性与玩家观感待试玩。下节的旧通用短光已被这两项独立效果替换，其他主动技能仍保留通用回退。

## 济世医师满蓝与施法者反馈（2026-09-16）

自动入口：`HealingReaderContractSmoke.tscn`（正式 HC18 接近、目标／法力、控制、回滚）；`HealingReaderVisualSmoke.tscn`（正式 BattleScreen 私有战斗，真实暂停／加速／恢复输入，双目标治疗与身体短光）。相关移动及法力检查、录制和限制见 BC 英雄活动任务最新段。

玩家复查：让满蓝济世医师与受伤队友相距超过4格，确认靠近后释放续命针；全员满血应保留法力。施放时本人身体短亮，两个受治疗者各自显示治疗；暂停后短光停住，恢复后消退。其他满蓝主动施法也复用源端短光，特效预览可选“施法短光”。玩家观感、强度和整局节奏尚未验收。

动作复查：续命针采用中性姿态＋身体短光，随后16点普通治疗才播放原普通动作；不能再把两个动作当作相同skill_cast排队连播。`HealingReaderVisualSmoke --trace-animation`核对一次主动、两笔70治疗与各次16治疗的实际动作开头，包含真实暂停／倍速／恢复和死亡／重置清理。前后录制与旧移动夹具门禁失败记录见BC活动任务最新段。

## 英雄信息场景复用（2026-09-16）

`tests/UnitInformationInputSmoke.tscn` 用当前发布内容及独立 authored 场景检查紧凑卡片、开局详情、准备详情的共用信息：基础值／准备值分离、准备数值复制、有效技能／成长被动、实际生命／法力／护盾、换人清理。真实鼠标／键盘输入检查属性与技能说明、返回焦点、紧凑卡片键盘提示、页面出征意图及锁定状态。`tests/RosterLoadoutInputSmoke.tscn` 继续覆盖正式组合根中的卡片边界、选人、配装拖放、悬停仅一个提示、滚动和模态操作。两个入口均通过，截图已查看；结果见 UI 活动任务顶部。

玩家复查：在选英雄页、部署详情和“英雄与装备”中查看同一英雄，确认信息易读；准备加成应有明确上下文，升级后的被动按有效技能说明展示。战斗实时面板和招募候选仍有各自的展示布局。本次不代表全项目 UI 或玩家体验已验收。


## 默认最大化与底部备战席（2026-09-16）

`tests/FormationDragOnlyInputSmoke.tscn` 已改用正式默认启动模式，检查原生窗口最大化并保留边框；用真实指针事件／命中检测完成底部备战席上场、场上移动／交换、空备战席撤回、拖到已有备战英雄上撤回、替换、满员拒绝、敌区取消、Esc取消及Tab／Enter只检查不移动。使用正式GameRoot、隔离命名空间及内存征程，合法动作恰提交一次。恢复1280×720后检查备战席、卡片、开始按钮边界及肖像点选。结果与截图见[UI活动任务](../../work-items/active/battle-lab-ui.md)最新段。

玩家复查：正常启动应占满桌面可用区域，标题栏可直接关闭／还原；进入部署即看到下方备战席，无需右上角入口。上场后卡片离开备战席，下场后回来，空席仍可接收英雄。全体英雄与装备入口保留。固定尺寸的旧UI输入入口需加`--windowed`，避免把逻辑坐标误当成最大化后的窗口像素。

下方2026-09-15的非模态后备窗记录为历史，本节取代其入口与布局；实验室和全屏装备管理边界保持。

## 后备与仅拖动布置（2026-09-15）

`tests/FormationDragOnlyInputSmoke.tscn`：正式 GameRoot + 内存存档，实际打开后备名册、滚到新招英雄、拖肖像上场、场上移动／交换、拖进底部撤回区、后备替换占用格。连续点英雄和空／占用格、点击撤回区都不能改变阵型或保存；拖到敌区或按 Esc 取消不提交，合法拖放恰保存一次。专项已通过，名册卡片和滚动截图已查看。后备名册是非模态工具窗；英雄与装备管理仍全屏。

实验室在 `EquipmentDragInputSmoke` 中补查点模板再点空格不新增、点棋子再点空格不移动，实际拖动新增和移动仍可用；已通过。旧“先选后点格”操作记录被本节替代。验证只用隔离数据，不代表玩家体验确认。

## 当前全屏名册配装检查（2026-09-15）

入口：备战“英雄与装备”或右上资源条→军团默认页。`tests/RosterLoadoutInputSmoke.tscn` 使用真实 GameRoot、隔离命名空间与内存征程；检查整卡肖像选择、属性／技能／装备 hover、同屏穿戴／替换／跨英雄转交／拖回图标和空白卸下、滚到最后后备配装、关闭／Esc／Tab。装备库存和英雄列表各自滚动，根场景资源条不能从模态窗穿透点击，卡片子控件必须在卡内。

1600×900 专项已通过，截图及限制见 [UI 活动任务](../../work-items/active/battle-lab-ui.md)最新段。后备／全局军团明确为基础属性；备战出战单位为准备投影。未做全分辨率／完整征程。下方“正式装备工具窗可移动／窗外棋盘配装”和“军团小型居中窗”是被本轮替代的历史路径；实验室工具窗路径保持。

## 当前弹窗与战场检查（2026-09-15）

使用 `tests/ContextPopupInputSmoke.tscn` 检查部署／军团／战斗信息窗；`tests/EquipmentDragInputSmoke.tscn` 检查工具窗到棋盘的真实拖放及实验室信息入口。均使用隔离内存数据，不写真实存档。下方旧固定侧栏路径属于历史记录，当前恢复以本节和活动任务为准。

- 备战／实验室默认无展开的大侧栏；选择与移动棋子不会强开详情。战斗默认只有必要状态、紧凑指令和查看入口。
- 详情、规则、统计、团队设置按需弹出；关闭按钮、Esc、点击遮罩能退出。模态 Tab 留在窗口中，点击遮罩不穿透触发底层按钮；关闭后焦点回到入口。开关前后棋盘矩形不变。
- 装备／名册／单位库是工具窗，窗外可继续拖放；可拖动标题露出被遮住的格子。穿戴、转交、替换回包和卸下保持此前语义。
- 看属性、技能、装备、遗物等内容是否能清楚区分主次、滚到完整说明；军团是有边界的居中弹窗。此次不更改候选选择页与战斗结算报告的核心版式。

## 当前主流程 UI 检查（2026-09-13，信息层级修订）

入口：低并发编译后，用带显示的 Godot 运行 `res://tests/MainFlowUiInputCapture.tscn`。使用正式内容与唯一测试存档，不使用 Computer Use，不读写真实玩家存档。`-- --offers-only` 只补查独立候选/部署界面，不能报告为正常战斗或完整征程通过；执行记录与截图见[活动任务当前段](../../work-items/active/battle-lab-ui.md)。

- 当前候选界面契约：1600 宽并排三张，按可用宽度换行；卡体、属性与详情按钮仅查看，每卡确认按钮才领取／购买。卡内长文单独滚动，确认按钮固定；额外候选由外层滚动访问。已用独立合法征程 fixture 实际操作上述路径，并补查 900×900 两列、滚到最后候选打开说明、恢复三列。
- 军团／备战共用紧凑详情：首屏肖像、生命／法力、四项关键属性和主被动；检查属性 hover 的真实提示与点击／Space 内容一致，次要属性能展开，技能解释可访问，前后征程数据不变。
- 招募所得后备经真实名册点击及空格点击加入已保存阵型；商店合法购买扣款与列表刷新；奖励预览滚动与继续。预览未伪造胜利或领取。
- 战斗侧栏保持点选、属性／技能解释、正文滚动与统计折叠的真实输入路径；本次信息修订复查用实验室战斗，不冒充自然征程。上一轮新征程、路线、真实胜败报告与胜利奖励页的记录在活动任务历史段，本轮不重新声称全部自然随机分支已验收。
- 已查看最终渲染：修复新卡片肖像只露头的取景问题，确认物品折叠正式效果可读；属性提示不再留下整屏高度空白，并以真实悬停检查其高度。最终低并发编译0警告／0错误，两个入口退出0。未执行多分辨率扫测、满背包与完整通塔；历史套件未批量运行，玩家体验仍由用户确认。

## 当前使用边界（2026-09-06）

下方是历次 Alpha 的混合历史检查记录，不是现行完整验收清单。旧“英雄统领士兵”“英雄自带手动命令/共享 MP”和具体商人技能配方已不能作为产品要求；旧通过记录也不代表新版本通过。

当前玩家规则读 `../../gameplay-design/README.md`；英雄独立法力/装备检查读 `formal-equipment-and-hero-mana.md`，通用模型/保存检查读 `content-system-foundation.md`，远程行为读 `ranged-attacks.md`，连续移动读 `continuous-space-combat.md`。其他仍适用的历史检查只按本次变更风险挑选，不默认执行全局/多分辨率扫测，也不为满足旧断言恢复过期行为。

## 历史检查项

## Complete Loop

1. Start from the main menu in Chinese and open settings once to verify volume and default battle speed persist and apply on the next launch/battle. The page must not expose an unsupported damage-number option.
2. Focus an unlocked hero, inspect its detail panel, and enter a seeded run through the single `以该英雄出征` action. Verify the detail identifies its army rule, named battlefield command, structured mana cost, and concrete effect. The merchant detail must additionally show a separate `5 金币` cost badge.
3. Choose tower nodes through all three regions. Exercise combat, elite, recruitment, shop, event, rest, and each boss.
4. Recruit soldiers and verify each card shows Chinese gameplay traits and role. In particular, `深渊爬兽` must show both `亡灵` and `野兽`, without exposing `soldier`/`undead`/`beast`. Move the hero and soldiers among legal cells in the first three columns, move soldiers between deployment and reserve, take items, and verify identities, exact cells, currency, health, and roster changes persist after returning to the menu and resuming.
5. Before each fight, verify enemy composition and the floor rule preview are visible. During combat verify pause, 1x/2x/4x speed, and that the limited hero-command button uses the selected hero's concrete command name. For `加急雇佣`, verify the HUD shows `5 金币/次` and remaining gold: at 5 gold a use changes MP 3/3→2/3, gold 5→0, and summons once; at 4 gold it reports `金币不足` without changing MP, gold, or summons.
6. Defeat the final boss and verify the victory result and meta unlock. Repeat a defeat path and verify defeat/restart.

## Architecture And Presentation

- Open a representative hero, soldier, enemy, and item scene independently; each must instantiate without the game root.
- Inject the one-shot structural test scene during the first catalog-validation instantiation, then inject one ready-pass test scene at a time that calls `GD.PushError` from `NotificationSceneInstantiated`, `_Ready`, its first `_Process`, and `_ExitTree`; every marker must appear exactly as authored in the validation report and prevent registry publication.
- Open each hero-command component scene directly and tune a non-default value. Its generated Chinese effect text and the next runtime execution must reflect that same value; zero/negative counts, scales, durations, divisors, tags, or paid-command costs must fail authoring validation.
- Verify constrained lanes, periodic hazard cells, an objective device, and a boss targeting rule visibly alter combat.
- Verify player formation armor never reduces damage received by an adjacent enemy formation; a lethal floor hazard emits a defeat attributed to `floor`; and the second-region boss summons at least one but no more than two temporary minions.
- At 1600×900 and 1280×720, verify route, roster, deployment, battle HUD, reward, and result text do not overlap.
- At 1280×720 with nine roster members, verify the deployment header, enemy/floor preview, status, and both bottom actions remain fixed and visible while the roster cards scroll independently.
- Verify player-facing cards and results use Chinese role, rarity, and battle-result labels; internal values such as `Ranged`, `Legendary`, or `PlayerDefeat` must not appear.
- Confirm no runtime error mentions `D:\godot\rpg`.

### Visual Language And Real Input

1. On deployment at both supported resolutions, inspect the untouched board, a roster selection, a legal destination, an illegal destination, a swap target, an active drag, a completed placement, and a rejected placement. Empty cells must contain no persistent `可部署` or row/column text; zone, hero, soldier role/reach, enemy, hazard, blocked, objective, focus, and action legality must remain distinguishable without relying only on color. Exact identity, coordinates, rules, and one concise invalid reason must remain reachable through the detail/tooltip layer.
2. Drive real viewport mouse press/release events through a reserve roster card and then a legal cell. Selection and placement/swap must complete without `Object is locked`, invalid `free`, disposed-object, duplicate activation, or focus loss; one successful command saves exactly once. Repeat selection, deselection, withdrawal, back, start, drag cancellation, and illegal drop; rejected/cancelled actions mutate and save nothing.
3. Traverse every candidate cell and fixed deployment action with keyboard/gamepad focus. Confirm selected, legal, illegal, occupied/swap, disabled, success, and failure feedback remains visible and equivalent to mouse interaction, and that disabled/empty presentation never removes a legal focus target.
4. Activate route, recruitment, reward, and shop decisions through real mouse input, including a purchase that refreshes the available list. The active card must survive its event dispatch, refreshed cards must remain clickable/focusable, and no dynamic list may log locked-object, invalid-free, disposed-object, duplicate-action, or stale-focus errors.
5. Traverse route, hero selection, recruitment, reward, shop, event, rest, Army overview, deployment, battle, report, result, and settings at `1280×720` and `1600×900`. Confirm first glance exposes objective/resources/primary action, hover/focus exposes comparison state, precise rules remain on demand, and the same fact is not repeated in page prose, summary text, card labels, and action copy. Record every remaining instructional paragraph that cannot yet be replaced safely.

### Window Baseline And Unit Portraits

1. Clear any test-only window override and launch the game normally. Confirm it opens in a resizable `1600×900` window and is neither fullscreen nor maximized; resize it to `1280×720` and back without losing required actions or creating overlapping UI.
2. Inspect all 45 production portrait resources grouped as 8 heroes, 24 soldiers, and 13 enemies. Confirm each resource can be tuned independently, uses existing local animation art, removes distracting transparent padding, centers the readable body, and does not alter the corresponding battle sprite, animation, or facing.
3. At both `1600×900` and `1280×720`, inspect representative small, tall, wide, mounted, hero, soldier, and enemy portraits in hero selection, recruitment, the deployment roster, Army details, and battle-report rows. Confirm every context uses the same authored crop for a unit and varies only its display size.
4. Confirm hero selection uses compact animated library tiles plus one detail panel, while recruitment uses its unit-specific row. Both keep portrait, name, precise responsibility, localized gameplay tags, and key attributes separate from long Chinese descriptions.
5. In recruitment, confirm exactly three choices appear in one vertical column. At `1600×900`, each card is about 148–150 pixels tall with a 104–108 pixel portrait region. At `1280×720`, only the choice list scrolls; the skip or hero-authored conversion action remains fixed, visible, focusable, and clickable.
6. Traverse hero and recruitment choices using mouse, keyboard, and gamepad. Hero focus must change preview only; the fixed detail action and each recruitment row must produce exactly one stable-id activation. Verify disabled heroes, tooltips, focus order, and scrolling. Confirm multi-tag units retain every localized gameplay tag while technical tags remain hidden.
7. Recheck tower-route and item reward/shop cards at both resolutions. Their general `ChoiceCard` size, icon hierarchy, focus behavior, and selection semantics must remain unchanged.

### Hero Library And Semantic Hierarchy

1. At `1600×900`, confirm several compact animated hero tiles fit in multiple columns beside one focused detail panel. At `1280×720`, confirm the library reduces columns without hiding the detail action or return action.
2. Move mouse, keyboard, and gamepad focus across at least three heroes, including one locked hero. Confirm focus changes preview only, locked state is readable without color alone, and only `以该英雄出征` starts a run. One activation must submit one stable id exactly once.
3. Confirm the focused detail groups portrait/identity, health, damage, reach, army rule, command, cost, and availability before paragraph reading. Health and damage each appear exactly once.
4. Inspect the same command in hero detail and battle HUD. Mana must be a blue icon-labelled structured badge and optional gold a separate gold badge; neither cost may be embedded in or recovered from neutral effect prose.
5. Across touched screens, confirm health is green, attack/damage red, mana blue, shield steel gray, healing teal-green, gold/hero identity gold, range light cyan, death/danger crimson, and risk amber. Every colored value retains an icon and/or Chinese label.
6. Open `StatBlock.tscn`, `TraitBadge.tscn`, `ResourceCostBadge.tscn`, and `HeroAbilityPanel.tscn` independently. Each must bind without a level, battle, autoload, or concrete hero id.
7. At both resolutions, recruitment must show exactly three compact vertical choices with readable portrait, identity badges, separated health/damage/reach values, neutral description, and one clear recruit action while bottom actions remain fixed.

## Playability Polish

### Combat Animation And Readability

1. At 1x, 2x, and 4x battle speed, watch at least one melee attack, ranged attack, skill cast, hit, and defeat. Each attack/hit/skill cue must remain visible for its authored one-shot instead of returning to idle in the same rendered frame.
2. Confirm damage and defeat cues play only on their targets, while movement and attack cues play only on their sources. A defeated unit must enter defeat once, briefly hold, fade/hide once, and never reappear during later synchronization.
3. Confirm every player hero carries a high-contrast gold `★ 英雄` identity cue that is readable without color alone. Confirm each unit displays `近` or `远`, with `原兽母皇` classified as near and `骸骨摄政` classified as ranged.
4. Select representative heroes, soldiers, and enemies during battle. The selected-unit panel must show current health, precise role, and exact attack reach, and must refresh when selection changes or the selected unit is defeated.

### Army Summary And Drawer

1. On route, recruitment/reward, shop, event, rest, and deployment screens, verify the compact summary shows hero health, deployed/reserve counts, item count, and gold without displacing the current decision UI.
2. Open the Army drawer on each screen. Verify hero army rule and generated command effect/cost, soldier health/deployment/role/reach, and item effects match the current run. Formation controls must not appear in the drawer.
3. Repeat at 1280x720 and 1600x900. Scroll the drawer to its final row, confirm no clipping, click-through, or pointer leakage, close it, and verify focus/input returns to the underlying screen.
4. Recheck after recruitment, purchase, rest, battle casualty, deployment change, and item gain; both summary and drawer must refresh without leaving the current decision.

### Hero Command Mana And Transactions

1. Enter battle with each hero and confirm the HUD shows 3/3 blue mana segments, the concrete command name, generated effect, one-mana cost, optional gold cost, and an empty/current failure reason as appropriate.
2. Use a legal command three times and verify mana changes 3/3 → 2/3 → 1/3 → 0/3. A fourth attempt must fail with an explicit reason and leave mana and gold unchanged. Pause and 1x/2x/4x speed changes must not alter mana.
3. Start the next battle and confirm mana is restored to 3/3 without being stored in the run save.
4. Exercise insufficient mana, insufficient gold, dead hero, and missing/blocked summon failures. Each failure must preserve both mana and gold and must not create a partial summon or effect.
5. For `加急雇佣`, test 5 gold with an available summon (one summon, 3/3 → 2/3, 5 → 0) and 4 gold or no available summon (no summon and no mana/gold change).

### Battlefield Deployment

1. Confirm the deployment preview shows the real `10×6` logical floor, current floor rule, unchanged enemy start cells, and all 18 candidate player cells in columns `0..2`. Every concrete enemy must show its own independently advancing idle portrait plus compact enemy/role/reach or boss redundancy; hazards, objectives, and blocked terrain remain semantic markers. Verify legal cells, blocked cells, non-player cells, occupied cells, hero, soldiers, enemies, hazards, and objectives remain distinguishable without color alone.
2. Move the hero to every legal row/column extreme through mouse select-then-cell, then swap it with a deployed soldier. The hero must never leave columns `0..2`, enter a blocked cell, overlap a piece, withdraw, or be replaced by a reserve soldier.
3. In an isolated four-soldier state with three deployed and one reserve, test both real drag/drop and real select-unit-then-select-cell for reserve-to-empty placement; confirm the selected identity appears in the application run, persisted save, rebound roster card, and destination cell. Then cover reserve-to-occupied replacement, deployed-to-empty movement, and deployed-to-occupied atomic swap. Withdraw a deployed soldier to reserve; with three reserves, withdrawal must be rejected.
4. During one drag, traverse at least three candidate cells. Exactly one current destination may show drag-hover, every previous cell must clear immediately, and cancellation, outside-board release, drop, rebind, back, and teardown must leave no hover trail. Legal-target, selected-source, swap, illegal, success, and failure states must remain visually distinct without color alone.
5. Reopen deployment after every successful operation and confirm exact identities and cells persist. Each success saves exactly once. Cancellation, a drop outside the board, a blocked/non-player target, reserve-on-hero, same-cell action, and focus activation duplication save zero times and leave the formation unchanged. For every rejected command, the visible concise reason must match the authoritative non-mutating evaluation; no destination presented as legal may be rejected by unchanged application state.
6. Force one save failure for hero move, hero/soldier swap, soldier move, soldier swap, reserve replacement, and withdrawal. Verify the exact prior hero cell, six soldier identities/cells, reserve membership, and rebound selection-visible state are restored, with one failed save attempt and no partial formation.
7. Load an isolated representative version-2 JSON run and verify automatic version-3 migration uses hero `(0,3)` plus soldier cells `(1,1)`, `(1,2)`, `(1,3)`, `(2,1)`, `(2,2)`, `(2,3)` without losing hero, roster, health, items, gold, route, seed, battle count, or progression. Save/reload arbitrary legal version-3 cells. Duplicate, out-of-zone, out-of-bounds, wrong-count, and stale-identity version-3 formations must fail safely without touching the real active run.
8. Enter battle after each successful rearrangement and confirm the hero and every soldier spawn on the exact previewed cell with no fallback relocation or duplicate living-cell occupancy. Enemy spawns remain unchanged. Defeat one soldier and confirm only that identity leaves deployment while every other cell and the hero cell remain stable; empty-slot bonuses still count six minus deployed soldiers.
9. Traverse all 18 candidate cells and fixed actions using keyboard/gamepad focus, then repeat representative mouse and drag/drop operations. Focus must not escape, skip a legal cell, or submit one activation twice.
10. At `1600×900` and `1280×720`, confirm every concrete enemy preview on the right faces left toward the player zone while its enemy/role/reach badges, tooltip, cell, clipping, and interaction geometry remain upright and unchanged. Player and neutral portrait consumers retain their authored definition direction. Start battle and confirm the enemies' independently owned initial battle facing is still left rather than being double-flipped by deployment presentation.

### Responsive Battlefield Projection

1. At `1600×900`, verify battle uses the previously empty board area with all ten columns and six rows visible and cell pitch at least approximately `108×82`. Units, health/readability markers, hero/near/ranged cues, pointer hit targets, movement endpoints, and character lift must grow and remain aligned rather than leaving original-sized units in larger empty cells.
2. At `1280×720`, verify the complete board, inspector, status, pause, speed, command HUD, deployment fixed actions, and all required text remain visible without overlap or clipping. The projection may contract toward approximately `88×68`; it must not transform-scale text or the full UI tree.
3. Select living units at player/enemy corners and along the inspector-facing edge. The selected-unit inspector must never cover a playable cell or unit, and pointer selection must use the visible presenter position with projection-relative radius.
4. Resize repeatedly between the two accepted resolutions and intermediate widths. Cell centers, board drawing, deployment targets, hit testing, initial snaps, queued movement endpoints, markers, unit scale, and selection radius must update together and reversibly without stale offsets, teleporting, diagonal shortcuts, distorted text, or focus loss.

### Movement Presentation Readability

1. At 1x, observe a unit receive several consecutive continuous positions. Confirm the unit root advances linearly through intermediate positions, never overshoots or reverses, and does not pause, ease, or restart at each 0.1-second simulation sample. An ordinary idle/health refresh during travel must not pull it to the newest authority position early.
2. Observe a `right → down → left` route around blocked terrain. Confirm the route retains both turn vertices in order and never substitutes a diagonal or curved simulation path. At low rendering cadence one frame may advance across more than one queued segment, but the presenter must consume the ordered polyline internally and must not build an ever-growing x4 queue. Trigger a 100-300ms hitch and confirm a newly created move still exposes an intermediate travel frame; after input stops, the visible position catches up within roughly 0.25 seconds at a supported rendering rate.
3. Watch the character art, health bar, gold hero marker, near/ranged marker, and pointer selection origin during sustained travel. Only the character art may rise by roughly three pixels. Its travel weight must remain continuous across authority samples and return to the authored offset only when travel settles; the unit root and every readability marker remain on the projected path without bobbing.
4. Follow sustained movement at 1x, 2x, and 4x, including 4x near 30 FPS. The move clip and motion state must start once, remain active across consecutive samples, and stop once after travel settles. At 4x, require bounded visual lag and non-flashing behavior rather than every authored animation frame; action one-shots may override the move clip while root travel continues.
5. Pause midway through a visible segment and hold for at least one second. Root position, character lift, and movement animation must remain frozen. Resume and confirm travel continues from the same point. Switch 1x → 2x → 4x → 1x during a segment and confirm each change retimes continuously without restart, reversal, snap, or lift discontinuity.
6. Trigger attack, skill, and hit actions while a unit is traveling. The spatial path and character-only lift must continue underneath the one-shot; when the action ends, the base animation must return to `move` if travel remains or `idle` if it has completed.
7. Observe an initial deployment and at least one summon. Both must appear directly at their authored cells with zero decorative lift rather than sliding or bobbing in from the origin. Defeat a unit midway through travel and confirm it stops at its current visible position, clears pending movement, restores the decorative lift to zero, plays defeat once, fades/hides once, and never slides or reappears. Rebind/battle replacement must likewise begin cleanly.
8. Repeat representative sustained-route, turn-route, burst, action-overlap, pause/resume, speed-switch, and defeat-interruption observations at 1280x720 and 1600x900. Treat sample-cadence linear travel at 1x, followability at 2x, and bounded non-flashing 4x fast-forward as the acceptance standard; static screenshots alone are insufficient.

### Engagement, Navigation, Facing, And Action State

1. At 1x and 2x, observe a crowded fight containing melee, ranged, healer, hero, enemy, and summon units. Both teams must begin participating immediately, advance coherently, and only queue briefly behind allies; no unit may pace between two cells, reverse repeatedly, or detour around a visually empty future engagement goal.
2. Select stationary living units during seeking, route obstruction, attack cooldown, healing cooldown, disable, and defeat. The existing selected-unit panel must show an understandable Chinese action reason, and cooldown values must be expressed as player-facing seconds. A dead unit must stop being selectable immediately even while its defeat animation remains visible.
3. Place a low-health unreachable ally and a higher-health reachable ally near a healer. The healer must choose the reachable legal heal, respect line access, retain that protected ally through cooldown, and join ordinary enemy engagement when no wounded ally has a legal plan. A non-healer and otherwise equivalent ranged healer with no wounded ally must both enter combat.
4. Observe units moving right, vertically, then left. Facing changes only when each horizontal segment actually begins; vertical motion retains the prior direction. Player units begin facing right and enemies left. Attacks and heals face their real targets even while visible movement lags behind simulation cells.
5. Confirm defeat locks the last facing and cancels travel. Rebinding the same content restores its team default. Character art may mirror, but health bars, hero identity, near/ranged markers, labels, and other readability UI never mirror.
6. Kill a unit after it has queued movement but before movement resolution. It must emit no move, remain defeated, free its cell for later use, and leave no follower waiting on its dead target or ghost engagement goal.
7. Let a low-health lifesteal unit kill an enemy whose death effect damages the attacker lethally. The attacker must remain at zero health and defeated, perform no later action, and leave its cell usable; ordinary healing or floor healing must not revive it.
8. In a layout where a unit has retained a legal target but its route is temporarily occupied by a stationary ally, observe several movement-ready decisions. The unit must wait on the same target for the bounded lease, then retarget to a reachable alternate at the lease boundary instead of switching immediately or waiting forever.
7. Put a stationary ally on a unit's shortest first step while leaving a longer empty side route. The mover must take the side route within the bounded wait lease rather than repeatedly selecting the blocked step; if that target remains sealed and another enemy is reachable, it must switch at the lease boundary. Also place an attack or heal target exactly two cells away behind one blocked middle cell: neither the range-2 attack nor the range-2 heal may pass through the wall.

### Battle Pace, End Sequence, And Report

1. Compare equal wall-clock intervals at displayed x1, x2, and x4. Verify x1 advances about eight 0.1-second simulation ticks per real second, with x2 and x4 remaining exact 2× and 4× display-mode multiples. Pause and mid-motion speed changes must remain continuous, and the same seeded setup must keep the same terminal tick, outcome, digest, movement-event order, and report facts at every display speed.
2. Observe a terminal attack at normal speed. No later simulation step may occur; commands, pause/speed controls, selection, and board input must lock immediately. Keep the final battlefield visible for about 1.1 real-time seconds, then reach fully opaque black over about 0.45 seconds before the report appears. Repeat with confirm during hold and fade: it may accelerate arrival at the same report, never skip it, duplicate resolution, or route twice.
3. Exercise ordinary victory, final-boss victory, player defeat, and timeout. Each must first show the dedicated report. Continue must route once to combat reward, run-success result, or run-failure result as appropriate; closing or abandoning the screen after terminal resolution must not lose or duplicate the authoritative save mutation.
4. Verify the fixed report banner shows outcome, encounter, deterministic duration, and player command-gold cost. The two fixed team summaries must remain the single readable owner of survivors, casualties/kills, effective damage, effective healing, and remaining health; command gold must never appear as an enemy-comparable value.
5. Switch independently among `战局总览`、`输出`、`生存`、`治疗` and `我方`/`敌方`. Identity, portrait, name, hero/role/summon label, and alive/defeated state must remain recognizable while the leaderboard reorders deterministically. Output must show damage/share/DPS/kills; survival taken/shield/final health/lifetime; healing healing/share/HPS/effective events. A side with zero healing must show a deliberate Chinese empty state and overview must still expose every unit.
6. Use tied positive fixtures and all-zero categories. In each overview matchup, every tied positive leader must remain visible in deterministic order with its authoritative value and own-team contribution share; zero categories must invent no leader. Both-zero healing must collapse to `双方均无有效治疗`, while one-sided zero healing must label only that side `无有效治疗`. No composite MVP/score may appear. Verify positive environment damage is labelled as a team-level fact that belongs to no unit row or detail, and reconciles target-side taken minus opposing credited damage without false unit attribution.
7. Build focused shield, overkill, splash, pierce, death-effect, hazard, heal, overheal, lifesteal, floor-heal, late-summon, and hero-command cases. Effective damage must equal actual health-plus-shield removed, effective healing actual health restored, kills only concrete lethal transitions, and summons use independent leaderboard rows. Initial join tick must be zero; a late summon must use its actual join tick for DPS/HPS; defeat tick writes once; one splash/pierce action counts once; zero/overheal creates no healing event; only successful commands increment command uses. Re-running prior deterministic fixtures must prove these counters do not alter health, outcome, digest, saves, or routing.
8. At 1280×720 and 1600×900, inspect player/enemy overview, output, survival, healing/zero state, ordinary victory, defeat/timeout, and a temporary summon when available. The fixed header, dimension/allegiance controls, leaderboard header, selected-row indication, detail access, and continue must not clip or overlap; only the authored report-content owner scrolls. Continue must still route exactly once.
9. On the default overview, require a three-second read: identify outcome and directly compare the player-versus-enemy output, damage-taken, and healing core units without opening unit detail. Each positive side must expose identity, authoritative absolute value, and own-team contribution percentage; tied positive leaders remain explainable, zero healing is compact and explicit, both roster strips remain recognizable, and positive environment damage is labelled without unit attribution. The old team-total bars, duplicated survivor/casualty/kill sentence, and separate selected-side leader cards must be absent. No composite MVP, rating, fake timeline, skill split, or damage taxonomy may appear.
10. Switch among `输出`、`生存`、`治疗` for both allegiances. Each page must replace the complete fixed column set: output shows rank/unit/effective damage/share/DPS/kills/attack actions; survival shows rank/unit/effective damage taken/shield/final health/lifetime/remaining ratio; healing shows rank/unit/effective healing/share/HPS/effective events. For a representative six-unit side, every row uses identical column widths and numeric alignment, deterministic order, and a contribution bar normalized to the same selected-side maximum; zero healing uses the deliberate Chinese empty state.
11. Click and focus several leaderboard rows with mouse, keyboard, and gamepad. Exactly one compact row becomes selected and exactly one authored detail panel exposes that unit's complete existing facts and awards. Switch dimension/allegiance while a row is focused: retain selection only when the same runtime id remains valid, otherwise select the first available row, never target a freed control, and keep rows, controls, detail, and continue reachable.
12. Bind the longest current Chinese name/responsibility plus large values, tied core leaders, simultaneous positive awards, summons, and mixed alive/defeated states; then resize repeatedly between 1280×720 and 1600×900 and intermediate widths. Overview matchup identity/value/share columns and detailed ranking columns must stay contained; fixed columns must not drift between header and rows, names/identity must truncate or wrap into their authored regions without crossing numeric columns, common-scale ranking bars must remain comparable, detail content may grow only into the report scroll owner, and repeated resize must preserve dimension, allegiance, exact row count, and a valid selection/focus without transform-scaling text or the report tree.

### Full-Art Unit Cards And Disclosure (2026-09-30 修订)

2026-10-01立绘引用检查：从默认GrowthGameRoot的新局进入开局六选二，再选择、确认并打开英雄与装备页，核对使用低饱和配色背景的新版完整立绘。成长／matrix复用单位不能被旧CardIllustration透明稿覆盖；已有同Frames图片的敌人／士兵在选择立绘的静态信息面中也应使用新图，战场像素动画保持。`GrowthEntryInputSmoke.tscn -- --portraits-only` 提供这两个实际页面的隔离输入及Texture来源检查；`UiPortraitPresentationSmoke.tscn` 覆盖所有五个肖像目录的绑定、缺图回退和战斗动画隔离。最终观感仍由用户验收。

1. 在固定 960×540 隔离宿主中打开正式开局候选、招募单位卡和英雄／装备页。立绘占主体，保留灰石框与克制越框；底部姓名、攻击、法强、生命上限不被立绘淹没。招募按钮应常驻灰石卡框下方，留有间隔；属性项在正常、悬停和按压时均只有图标＋数字，没有独立底色框。单项提示可有自身阅读面板。现有未重画人物的画风不作为此轮 UI 验收成果。
2. 悬停卡片，检查左列向右、右列向左；真实技能（通常 1～2 个）从顶部向下渐显弹入；选人和招募卡不显示装备位，英雄／装备页显式启用三个装备位并保持同排技能，属性／技能／装备槽进入页面即常驻，没有“详情”按钮；移开指针、切换英雄与换装后均不隐藏。底部攻击靠左、法强居中、生命靠右，左右留白对称。最终停在灰石框内，没有整块资料框。底部只留姓名条收口，立绘铺到下沿，装备收起不留下预留空段。框宽保持，各项不覆盖邻卡、不移动原卡或确认按钮。招募卡点击“详情”固定，路过邻卡不抢走；“收起”／Esc 收起，旧焦点不能反复重开。开局六张卡横向单排居中，外围无大背板，人物展示高度增加；招募阶位以角石上的小号罗马数字刻字呈现，可悬停查看精确阶位，选中仅使卡框变为金色，取消后恢复，不额外显示勾选标记或选中文字。圆形立绘窗口与人物整体上移，姓名和核心数值位置稳定。开局右上角不显示“详情”；技能靠近卡框上沿，不覆盖角石阶位，血量等数字等高且基线一致；参考视口无需滚动，左右端卡的技能、属性和姓名完整可见。最右端卡悬停查看、明确点击选择及 Esc 收起均应可用；键盘 Tab 聚焦卡片可查看，移开后收起，提示选满或保存失败不得挤动卡片。
3. 招募卡核对常驻攻击／法强／生命与左右 11 项合计 14 个展示条目，零值保留；法力用实心蓝水滴与 `初始/上限`（如 `20/60`），不显示四项回蓝数值。开局紧凑卡和配装卡将法力拆成“初始法力”和“法力上限”，共 15 个属性条目；右列数值在左、图标在右，数值统一右对齐，两列的属性行距拉开。攻速为已确认原弯刀＋宽条拖尾，吸血红色、治疗绿色、法强为紫色流动能量、暴击率无中央闪电；材质图标不得再套单色调制。hover／键盘聚焦某项仅出现自己的解释，合并法力词条解释初始与上限两个数，开局与配装拆分条目则各自解释对应值。主动／被动、装备及两侧的成长／羁绊入口也各自只解释自身，成长／羁绊不充当技能槽。长说明可移入该提示滚动。配装页直接在常驻顶部槽位穿戴、替换、转交、卸下；同英雄刷新不收起，换人清理旧解释。两组顶部图标不能互相遮挡或被裁切；完整开局页选两名后仍能明确确认。
4. 开局卡没有详情按钮，悬停／键盘聚焦不选人，明确点卡或 Enter 才选择；招募仅明确确认才执行一次。招募检查 Enter 固定、Esc 返回；配装页检查一次 Esc 关闭整个窗口，重新打开时属性立即显示。检查快速反复开合、减弱动效中途开启、父界面隐藏和单位卡重绑为非单位机会。
5. 聚焦入口 `UnitCardDisclosureInputSmoke.tscn` 与既有 `RosterLoadoutInputSmoke.tscn -- --isolated-pointer` 提供实际输入检查；正常速度记录位于 `.godot/ui-review/unit-card-disclosure-motion/`。输入断言和 Agent 动态复核不替代用户对构图、幅度、时机与操作舒适度的最终验收。
6. 对照 D 战利品挂件左侧卡面：技能为圆形托座，装备为三个皮革扣，两组保持可辨间隔；左右属性同一起点、统一行距，数字字重低于常驻核心项。hover 展开时，黑色渐变在开窗边缘淡入，中心保持透明，不覆盖姓名／阶位／核心数值，也不能形成压住石框的硬矩形。英雄装备弹窗中也必须实际可见，不能因绝对 Z 层级落到弹窗下面。移开／Esc／切卡／隐藏父界面后无残留；快速反向与减弱动效不产生闪黑或拦截输入。顶部材质及渐变强度仍由用户作最终观感判断。

### Semantic Theme, Icons, And Choice Cards

1. Inspect hero/gold/legendary headings, mana/shield/player facts, healing/survival gains, enemy/casualty/defeat facts, and warning/timeout states across screens. Confirm their gold, blue, green, red, amber, and neutral treatments are consistent and centralized; every meaning also retains a Chinese word, icon, or symbol, and paragraphs remain neutral rather than fully colorized.
2. Verify health, damage, shield, healing, mana, gold, time, kills/deaths, hero, melee, ranged, risk, and loot icons remain sharp and tint correctly at 1280×720 and 1600×900. Missing optional unit/item artwork must fall back to a stable portrait or semantic role/loot icon without an empty or broken texture.
3. On hero selection, tower route, recruitment/reward, and shop screens, confirm every choice card exposes an independent icon/portrait, title, body, and footer/meta region. Long descriptions must not overwrite metadata. On hero selection specifically, move the mouse across several tiles and move keyboard/gamepad focus without activation: selected styling, detail content, selected stable id, and run state must remain unchanged. Mouse click or `ui_accept` must select exactly one hero and update detail once; only the fixed detail action may start the explicitly selected unlocked hero once.
4. Compare responsibility chips for 前卫、战士、远程、辅助、刺客、召唤、炮手、首领 and faction/trait chips for 秩序、沙海、亡灵、野兽、机械、霜寒、中立、敌军. Each meaning must retain Chinese text, use its differentiated catalog icon, and remain identical across hero selection, recruitment, deployment, Army details, reports, and selected-unit facts where it appears. `深渊爬兽` must show both 亡灵 and 野兽 with their matching faction icons.
5. Inspect combat, elite, recruitment, shop, event, rest, and boss tower choices at 1600×900 and 1280×720. All seven primary node icons must be visually distinguishable. Risk must remain a separate icon-plus-`风险` fact; a zero-risk route must keep its node identity rather than using the warning triangle as its main image.
6. Confirm unit cards show responsibility/faction or trait plus health, damage, and exact reach as icon-plus-Chinese-text facts. Hero health and damage must appear once, while army rule, battlefield command/cost, unlocked or locked state, tooltip, and typed activation remain readable and functional.
7. Watch representative multi-frame hero, soldier, and enemy portraits in all five portrait consumers. Each UI portrait must advance through its existing idle animation at a calm pace, pause when its card or ancestor is hidden, and resume when shown. One-frame portraits remain naturally static, and opening/hiding UI must not change any battlefield sprite, cue, frame, facing, or playback state.
8. Recheck dense fact groups, route cards, deployment roster, Army drawer, selected-unit panel, and battle-report rows at both accepted resolutions. No chip may clip, overlap prose, consume an action button, remove Chinese text, or break mouse, keyboard, or gamepad focus traversal.

### RealmTheme Full-Flow Presentation

1. Traverse main menu, hero selection, tower route, recruitment, deployment, battle HUD, battle report, reward, shop, event, rest, result, and settings in one run. Confirm every screen inherits the same project-local RealmTheme: dark flat surfaces, thin restrained borders, modest rounded corners, and gold emphasis. No pixel texture, ornamental rail, oversized corner hardware, missing style, or external donor dependency may appear.
2. On the main menu and every modal, confirm hierarchy uses one clear primary gold action, ordinary secondary actions, an explicit danger treatment where applicable, and compact controls for dense repeated actions. Normal, hover, keyboard/gamepad focus, pressed, selected, locked, cooldown, and disabled states must remain distinct without changing exact-once activation or typed command behavior.
3. At `1600×900` and `1280×720`, inspect every screen plus the Army drawer, hero detail/library tiles, recruitment cards, deployment roster/cells, selected-unit panel, command HUD, and all report dimensions/allegiances. No label, semantic icon, animated portrait, value, progress bar, tooltip, focus indicator, or fixed action may clip or overlap; only the already authored `ScrollContainer` owners may scroll.
4. Recheck health green, attack/damage red, mana blue, shield steel gray, healing teal, gold/hero gold, range cyan, danger crimson, risk amber, player/enemy distinction, secondary text, and neutral prose. Every meaning retains its icon and/or Chinese label, and the Theme—not per-screen path maps or paragraph parsing—owns its stable color role.
5. Open `StatBlock.tscn`, `TraitBadge.tscn`, `ResourceCostBadge.tscn`, `HeroAbilityPanel.tscn`, `HeroLibraryTile.tscn`, `DeploymentCell.tscn`, and representative report leaderboard-row, summary, and detail scenes independently beneath the local Theme. Confirm each retains its typed binding API and readable normal/selected/disabled state without requiring a level, autoload, concrete content id, runtime-built Theme, or runtime-built control tree.
6. Exercise settings sliders and every long scroll owner. Slider tracks/grabbers, scrollbar tracks/grabbers, report health/contribution bars, and keyboard/gamepad focus must remain visible in RealmTheme's flat language. Values, focus, and scroll position must behave as before.
7. Inspect the root composition and the local Theme resource. The background must be an authored flat dark `ColorRect`; the live Theme path must be `res://content/ui/RealmTheme.tres`; all styles must be `StyleBoxFlat`; and no live scene, source, test, system/QA authority, or Theme may reference a removed pixel path, old visual-role vocabulary, or an external absolute donor path.
8. Run the RealmTheme focused contract before the hierarchy, semantic, portrait/window, responsive/dynamic report, fixture, content, gameplay, movement, UI, alpha-run, and clean-startup checks. Generate and manually inspect the complete paired VisualCapture set at both supported resolutions, including fixed actions and selected/focused/disabled states.

## 2026-10-01：局内羁绊列表

1. 新开或继续征程，左侧应显示当前有关的羁绊，已激活的在前；当前贡献值、各档门槛、菱形当前档及激活／升档差值可辨认。
2. 悬停条目查看效果和计入成员，移入右侧说明面板并滚动至末尾；键盘聚焦可阅读，Esc 收起。后备成员应注明未计入（仅上阵计数的规则）。
3. 战前原生拖动成员下场／上场，跨过门槛时立即更新；原生穿戴／卸下羁绊徽章也要更新，同一成员的原生标签与徽章按当前去重规则处理。
4. 查看军团／装备后关闭，列表恢复；正式开战后仍显示开战阵容；实验室与实验室开战不显示无关征程数据。长列表滚轮和键盘都可到达末项。
5. Agent 已完成低并发构建与正式成长界面的隔离真实输入专项，证据与录像见 [羁绊展示记录](../../outputs/trait-tracker-20261001/README.md)。本地录像浏览器加载受限，未把抽帧记成原速播放观感验收；最后的密度、文字大小、遮挡与阅读舒适度仍待玩家确认。
