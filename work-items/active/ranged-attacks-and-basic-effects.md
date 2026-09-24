# 远程攻击实体与基础特效

## 当前修复：第一层 Boss 战远程停手（2026-09-13，已复现并修复）

用户报告第一层 Boss 打到中途远程全部停住。使用当前征程数据的只读副本建立 `tests/fixtures/first-boss-ranged-stall.json`，由内存 SaveService 加载正式内容与遭遇，不写真实存档。原始重放中共鸣使、裂印使、连弩手在杂兵死后长时间 Waiting、目标为空、无射击蓄力或连射；Boss 在中央四格障碍另一侧被饕餮牵制，直到前排死亡、Boss 移到左侧才恢复。

根因：交战点快速采样主要在最大射程外圈，少量内圈点只朝向自己；中央墙与地图边界使这些采样都不可用，`ScoreTarget` 错把有绕路射击点的 Boss 丢弃。现保留快采样优先，失败时以逻辑网格合法站点兜底，目标评分与目的地选择使用相同兜底，保留地形、身体、落点预约和连续移动检查，不改变实际位置为格点。极小回归还定位到零宽视线掠过墙角、实体箭撞墙的问题：敌对投射攻击的行动与站位检查加入实际子弹半径；同队治疗、近战和光束沿用原规则。

验证：低并发编译0警告／0错误。`FirstBossRangedStallSmoke` 重放正式存档副本，最终214tick结束，无目标等待最大0；固定目标窄路用例14tick实际命中，重复轨迹一致；整面封墙120tick不得穿墙发射。额外通过 `GameplayContractSmoke --movement-only`（地形、身体、拥堵、回滚、治疗、死亡清理）、`AlliedBodyNavigationContractSmoke` 和 `RangedAttackContractSmoke`。日志在 `.godot/ui-review/boss-stall-before.log`、`boss-stall-after.log` 与 `boss-*-regression.log`。原样本595tick才结束，不能把时长变化作为平衡结论。以上为隔离模拟行为验证，未进行窗口手动游玩／新动效验收；没有改数值、素材或玩家存档，没有提交／推送。

恢复入口：本节、`DeterministicContinuousMovementService.ScoreTarget/SelectGoal/HasActionLineAccess`、`BattleSimulation.HasLineAccess`、`tests/FirstBossRangedStallSmoke.tscn`。本次bug复现与必要验证按用户当前故障处理授权执行，下方早期“不推进战斗”仅为当时批次边界。

## 目标与授权

- 2026-09-06：用户要求实现远程攻击逻辑与简单特效，以及无 cast 时使用 attack 的动画兜底。
- 抛射型发射子弹，碰撞后才结算；激光型沿用即时结算。当前不扩展全局战术、治疗目标特效或完整技能特效。
- 仅 main；保留工作区已有装备、法力、连续空间等改动，不提交、不操作真实存档。
- 遵守既有不自动推进战斗的边界：可编译、静态检查及无模拟器的隔离特效预览；战斗回归入口已编写但本轮不运行。

## 实现契约

- UnitDefinition 显式配置 Melee / Projectile / Beam；速度、半径、寿命使用逻辑空间和模拟秒，不以实时距离猜测类型。
- 子弹拥有独立 Battle 实体状态和独立可预览场景节点。BattleSimulation 以固定步长驱动，扫描整段飞行路径与敌方圆形身体及地形；节点不通过引擎物理回调扣血。
- 第一版直线发射，不追踪、不友伤。按碰撞时间、runtime id 稳定排序；地形优先于同位置单位。尸体不阻挡；发射者死亡不取消已发射子弹，但战斗终局立即清空剩余子弹。
- 发射捕获基础输出，目标防御等沿用命中时既有伤害管线；攻击声明和攻击回蓝在发射时一次，AttackLanded / 吸血 / 溅射 / 减速在首次命中时发生。既有 PiercingLine 子弹最多碰撞两个不同敌人，第二次沿用 35% 原始伤害，不瞬间攻击射线后的单位。
- 子弹状态和序号纳入世界事务快照，生命周期事件进入战斗摘要；战斗替换、失败、终局、销毁统一清理。
- 特效采用 authored .tscn 的 Polygon2D / Line2D，无外部素材。表现层处理暂停、倍速、投影变化、瞬时命中闪光和清理；暂停单步对子弹使用快照位置。
- 治疗动画回退为 skill_cast → cast → attack → idle。

## 进度与恢复

- 已实现模拟、场景、表现路由、21 个现有远程定义的显式类型、内容校验及指纹。
- `tests/RangedAttackContractSmoke.tscn`：延迟伤害、即时激光、高速扫描、拦截、穿透、地形、寿命、失效目标、源死亡、确定性与清理；未运行。
- `tests/RangedAttackVisualPreview.tscn`：不创建 BattleSimulation 的隔离渲染及治疗攻击兜底检查。
- 已完成低并发编译（0 警告／错误）、隔离预览运行与截图检查；未运行战斗回归／真实交互。实现已交付，战斗体验待用户验收。
- 恢复入口：本文件、`src/Battle/BattleProjectiles.cs`、`src/Presentation/RangedAttackLayer.cs` 和对应测试入口。测试实际结果见 `docs/testcases/ranged-attacks.md`。
