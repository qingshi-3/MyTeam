# 历史测试归档

2026-10-06 将下列 18 组入口及其 `.cs.uid`、`.tscn` 保留在 `retired-content/`。它们依赖已删除的旧项目、英雄、画像、预设或 `legacy-roster` 场景，属于历史实现与断言证据，不保证可以运行，也不作为当前通过门禁。

迁移保留 C# 源码和 UID；场景仅调整自身脚本路径。历史源码中的旧内容 ID、资源路径及成功标记不代表当前发布内容。恢复某组入口前，应按当前正式规则迁移夹具与断言，并重新取得与其风险相称的验证证据。

`tests/archive/.gdignore` 用于隔离 Godot 资源扫描；`my-team.csproj` 在所有配置下排除 `tests/archive/**/*.cs`。常规测试仅在 Debug 默认编译；其他配置需要显式传入 `-p:MyTeamIncludeTests=true` 才编译未归档测试，该参数不恢复归档代码，也不证明任何历史入口可运行。生产导出包的资源排除仍须由导出配置单独处理。

当前内容的实验室入口为 [CurrentBattleLabContractSmoke.tscn](../CurrentBattleLabContractSmoke.tscn) 和 [CurrentBattleLabInputSmoke.tscn](../CurrentBattleLabInputSmoke.tscn)；能力原型输入入口为 [AbilityPrototypeInputSmoke.tscn](../AbilityPrototypeInputSmoke.tscn)；征程恢复入口为 [JourneyRestorationContractSmoke.tscn](../JourneyRestorationContractSmoke.tscn) 与 [ExpeditionJourneyInputSmoke.tscn](../ExpeditionJourneyInputSmoke.tscn)。下表所列替代方向只说明当前入口位置，不声称完整覆盖每组历史断言或完成视觉验收。

| 历史入口源码 | 归档原因 | 当前替代方向或覆盖缺口 |
| --- | --- | --- |
| [VisualCapture.cs](retired-content/VisualCapture.cs) | 整套画面采集实例化已删除的 legacy-roster GameRoot，使用旧英雄与旧流程。 | 当前征程输入与实验室输入；旧整套截图采集未逐项迁移。 |
| [GameUiChangedScreensCapture.cs](retired-content/GameUiChangedScreensCapture.cs) | 加载已删除的 legacy-roster GameRoot，并选择旧英雄。 | 当前征程输入；旧指定画面采集未逐项迁移。 |
| [DeploymentInputHeroSelectionCapture.cs](retired-content/DeploymentInputHeroSelectionCapture.cs) | 加载旧 GameRoot、退役发布夹具和已删除的旧区域。 | 当前征程输入；旧部署与选英雄截图未逐项迁移。 |
| [BattleLabVisualCapture.cs](retired-content/BattleLabVisualCapture.cs) | 使用退役发布夹具、旧 BattleLabScreen 与旧预设目录。 | 当前实验室真实输入与契约入口。 |
| [ExperienceSliceInputSmoke.cs](retired-content/ExperienceSliceInputSmoke.cs) | 加载已删除的 legacy-roster ExperienceSlice 场景。 | 当前征程输入；旧切片专属流程未迁移。 |
| [BattleLabPlacementInputContractSmoke.cs](retired-content/BattleLabPlacementInputContractSmoke.cs) | 使用退役发布夹具与旧 BattleLabScreen，要求旧装备和羁绊。 | 当前实验室真实拖放入口；空物品池下不恢复旧装备断言。 |
| [DesignedContentInputSmoke.cs](retired-content/DesignedContentInputSmoke.cs) | 发布已删除的 alpha_project，选择旧 HC01，并要求旧升级控件与默认阵容。 | 当前实验室输入与能力原型输入；旧升级行为不作为现行规则。 |
| [DesignedProjectileVisualSmoke.cs](retired-content/DesignedProjectileVisualSmoke.cs) | 发布已删除的 alpha_project，使用旧 HC01 和历史对照投射物。 | 当前实验室或能力原型输入；旧箭效专属对照未迁移。 |
| [DesignedTauntVisualSmoke.cs](retired-content/DesignedTauntVisualSmoke.cs) | 发布已删除的 alpha_project，并以旧 HC03 编排嘲讽录像。 | 当前内容的实验室入口；旧嘲讽专属动态检查未迁移。 |
| [HealingReaderVisualSmoke.cs](retired-content/HealingReaderVisualSmoke.cs) | 发布已删除的 alpha_project，并加载已删除的 HC18 场景。 | 当前内容的实验室入口；旧治疗英雄的施法与治疗对照未迁移。 |
| [EnemyFiveVisualSmoke.cs](retired-content/EnemyFiveVisualSmoke.cs) | 发布已删除的 alpha_project，并按旧敌人前缀查询已退役预设。 | 当前实验室预设与战斗输入；旧五敌组合未迁移。 |
| [EnemyPiercingVisualSmoke.cs](retired-content/EnemyPiercingVisualSmoke.cs) | 发布已删除的 alpha_project，并要求旧穿透敌人的专属预设。 | 当前实验室入口；旧穿透表现与局部投射物断言保留为迁移资料。 |
| [DuelGritHookVisualSmoke.cs](retired-content/DuelGritHookVisualSmoke.cs) | 发布已删除的 alpha_project，并按前缀查询旧对决预设。 | 当前能力原型输入；旧对决录像未迁移。 |
| [MeleeContactVisualSmoke.cs](retired-content/MeleeContactVisualSmoke.cs) | 发布已删除的 alpha_project，并部署已删除的 HC03。 | 当前实验室入口；旧接敌录像未迁移，核心碰撞契约继续保留在常规测试。 |
| [RangedStyleVisualReview.cs](retired-content/RangedStyleVisualReview.cs) | 发布已删除的 alpha_project，远程样式分组选择多名已删除英雄。 | 当前实验室入口；旧分组材质录像未迁移。 |
| [UiPortraitPresentationSmoke.cs](retired-content/UiPortraitPresentationSmoke.cs) | 必须加载已删除的 HC03／HC01 画像及旧画像目录。 | 画像通用断言保留为迁移资料；没有声称存在等价的当前入口。 |
| [UiSmoke.cs](retired-content/UiSmoke.cs) | 加载已删除的 legacy-roster GameRoot，旧英雄、装备和爬塔流程贯穿主体。 | 当前征程及实验室输入入口；模态焦点、结构化卡片、战报零值／维度／路由和战术点禁用等通用断言尚未全部迁移。 |
| [CurrentUiInputCapture.cs](retired-content/CurrentUiInputCapture.cs) | 默认、词条和体型分支依赖已退役 HC／EB 单位及旧界面路径，并含桌面指针移动。 | 当前实验室输入入口；词条数字配色、中性数值、富文本转义和技能名称说明等专项仍待迁移，旧源码不能作为当前隔离输入入口。 |

本次归档前已静态检查上述类型与 UID 在 `tests/`、`src/`、`scenes/`、`content/`、`tools/` 中的引用，无组外代码或资源引用。核心规则、存档事务、碰撞、确定性检查及被现行入口调用的帮助类未纳入本名单。首轮 16 组归档后，Debug 与 ExportRelease 构建及当前实验室、花灵培养、内容引用检查通过。补充归档两组后，Debug 构建仍为 0 警告、0 错误，内容引用检查通过；编译清单为 165 个未归档测试 C#，归档代码为 0，正式配置仍按既有规则排除测试 C#。日志与编译摘要见 `outputs/archive-20261006/`。本次未复跑历史入口或进行界面体验验收，历史运行结论不沿用为迁移后的验证结论。

迁移映射保存在 `outputs/archive-20261006/test-moves.json`。完整编译清单与归档前文档快照收拢在同目录 `verification-details.zip`，解压字节与原件一致；摘要、日志、迁移映射及完整性记录单独保留。`tools/check_content_reset.py` 显式跳过本归档目录；其他自行递归扫描的工具不得将归档入口计入现行门禁。历史性能工具 `tools/run_architecture_profile.py` 显式启用未归档测试编译，但旧十英雄夹具已退役，当前不能直接运行；独立指纹与性能检查保留待迁移。
