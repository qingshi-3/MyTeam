# 测试入口与维护边界

2026-10-06 整理：当前常规测试保留在本目录；18组依赖已退役内容的入口移入[历史归档](archive/README.md)。归档保留源码、场景和UID，不代表旧断言已被完整替代。具体执行证据与手工观察归[验收说明](../docs/testcases/README.md)。

## 先按问题选入口

| 检查范围 | 入口 | 说明 |
| --- | --- | --- |
| 当前征程发布／保存／恢复 | [JourneyRestorationContractSmoke](JourneyRestorationContractSmoke.tscn) | 当前profile、正常开局与恢复边界 |
| 正常流程实际输入 | [ExpeditionJourneyInputSmoke](ExpeditionJourneyInputSmoke.tscn) | 按现有局部参数选择；花灵用`--flower-only`，古树／U25用`--breeding-only`；非终局败战、战报继续与冷继续用`--v1-defeat-only`，建议通过`--journey-namespace=<隔离namespace>`和`--output=<独立目录>`分离本轮记录。同一V1 Run从前序Boss胜利后的待领奖阶段继续至终局用`--v1-journey-resume`，必须显式提供`--journey-namespace=<原namespace>`、`--prior-evidence=<前序证据目录>`和`--output=<新的独立输出目录>`；恢复输入只读核验前序证据，不覆盖原目录。两种输入用于B8里程碑／对应修复后验收，不默认遍历全流程 |
| 八羁绊、现行单位与古树庇护 | [AbilityPrototypeContractSmoke](AbilityPrototypeContractSmoke.tscn)、[实际输入](AbilityPrototypeInputSmoke.tscn) | 逻辑契约包含`ANCIENT_TREE_PROTECTION_OK`；界面输入分别验证 |
| 花灵治疗／固定培养与育种成长 | [FlowerHealingContractSmoke](FlowerHealingContractSmoke.tscn)、[FlowerCultivationContractSmoke](FlowerCultivationContractSmoke.tscn) | 目标、打断、回滚、冻结、双属性保存、古树点数与U25状态账本 |
| 当前战斗实验室 | [CurrentBattleLabContractSmoke](CurrentBattleLabContractSmoke.tscn)、[实际输入](CurrentBattleLabInputSmoke.tscn) | 当前目录、预设、正式拖放与统计 |
| 详细战况 | [BattleDetailedStatisticsContractSmoke](BattleDetailedStatisticsContractSmoke.tscn)、[BattleDetailedStatisticsIntegrationSmoke](BattleDetailedStatisticsIntegrationSmoke.tscn)、[实际输入](BattleStatisticsInputSmoke.tscn) | 按统计改动风险选取；性能证据有版本／规模边界 |
| 保留技能及内容清理 | [ContentResetContractSmoke](ContentResetContractSmoke.tscn) | 复用怒劲队列等核心检查；资源引用检查为`python3 tools/check_content_reset.py` |
| 当前V1内容／战力评估 | [ExpeditionPveCalibration](ExpeditionPveCalibration.tscn) | 六件正式装备指纹；`--v1-content --out <目录>`生成18个阶段checkpoint与36场主矩阵，`--v1-probes --v1-replay <目录>`执行效果／Boss探针，`--v1-sustain-probe --v1-replay <目录>`执行4格续航，`--v1-economy --v1-replay <checkpoint>`执行路线经济，`--v1-run-items --v1-replay <18快照目录>`执行经营遗物，`--v1-economy-gaps --v1-replay <18快照目录>`补R02正常路线与R03余额样本。指纹、条件样本与限制见[PvE校准说明](../docs/testcases/expedition-pve-calibration.md)；fullGUI通关与败战继续是本轮B8里程碑验收，不属于每次常规测试。A/B/C均是有条件选择的可达样本，不按此宣称未筛选自然胜率。 |
| 当前V1装备／遗物／事件／敌人合同 | [JourneyRestorationContractSmoke](JourneyRestorationContractSmoke.tscn)、[V1RunEventsContractSmoke](V1RunEventsContractSmoke.tscn)、[V1EquipmentContractSmoke](V1EquipmentContractSmoke.tscn)、[V1RelicCombatContractSmoke](V1RelicCombatContractSmoke.tscn)、[V1EnemyMechanicsContractSmoke](V1EnemyMechanicsContractSmoke.tscn) | 当前发布池为6件正式装备、11件遗物、15个B7敌人；R02 Boss入口修复后事件与征程供给合同通过。完整运行证据和剩余GUI验收见[八羁绊进度](../work-items/active/eight-trait-prototype.md)。 |

其他核心碰撞、确定性、检查点、事务及共享帮助类继续保留。表格不是每次都要运行的测试套件；仅补验改动风险与既有证据缺口。

## 编译与运行

- `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`：Debug默认编译未归档测试，保持现有Godot场景检查方式。
- `ExportDebug`、`ExportRelease`和普通`Release`默认不编译测试C#。诊断确有需要时显式传`-p:MyTeamIncludeTests=true`；它不会恢复archive中的源码。
- `tests/archive/**/*.cs`在全部配置排除；archive的`.gdignore`隔离Godot资源扫描。尚无导出预设，编译隔离不等于测试场景已从导出资源包排除。
- 逻辑检查：`Godot --headless --path . --scene tests/<入口>.tscn`。实际输入检查使用渲染驱动与现有960×540隔离宿主，不移动桌面鼠标。
- 需要显式成功标记，并核对失败日志；退出码或构建成功不能代替运行结果，截图也不能代替动态／交互验收。

## 历史与待迁移入口

`UiSmoke`、`CurrentUiInputCapture`已归档；其中通用界面断言仍保留在源码，尚未全部迁入当前入口，覆盖缺口见归档说明。其他混合旧内容的专项需逐项迁移，不要根据名称中的Current／Contract认定当前可运行。`ArchitectureBattleProfile`及`tools/run_architecture_profile.py`保留独立的优化配置、事件／状态指纹和性能测量边界，但旧十英雄夹具已退役，当前不能直接运行；需先迁移夹具，不列为当前通过门禁。

维护时先扩展现有职责相同的契约或输入入口，不为每次局部调整新增一套Smoke。只在存在独立、长期维护价值的边界时新建文件。归档前核对反向调用、共享帮助类、资源路径和UID；不可只按日期或文件名删除核心验证。归档后修正文档与工具引用，保留原失败／未验收限制。
