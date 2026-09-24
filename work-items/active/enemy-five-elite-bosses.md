# 五名精英与首领落地

## 目标与授权

用户已明确要求「把这五个做了」：D51 / Q23 的 EE07 炉喉蜥卫、EE08 返刃投手、EE09 换位妖使、EB01 岩垒督军、EB02 裂壳虫母。D52 覆盖返刃起手：自然投掷动作直接离手，无额外蓄力和预警线。此前 EE04—06 不属于本批。

在 main 当前工作区实现，保留已有及并行英雄改动；不提交、推送、切换分支或创建 worktree。单位、能力与表现分别使用独立资源；战斗状态由实例持有。

## 行为验收

- 吐息锁方向，扇形三段魔法伤害，控制中断余段。
- 返刃每段每目标至多命中一次，地形截断去程；回到离手原点，施法者移动/死亡不改变轨迹。
- 换位取最远合法敌人，准备后按双方当前位置原子交换；任一完整体型落点无效、死亡或受控则双方不交换并消耗冷却。
- 岩墙短时、可破坏、挡移动和适用弹道；自动单位能绕行或普攻拆墙；地裂越过自身墙；首领死亡清墙。
- 虫母半血一次裂壳，继续可受伤，同一血量/状态，真实缩体并沿合法地面后退；卵可打掉，3秒孵化；卵+幼虫存活合计<=2，总卵<=4/两批；母死清未孵化卵，已生幼虫留场。
- 五个独立实验室入口和正式可达内容；确定性、取消、清理及回滚覆盖；用户验收节奏/手感与平衡。

## 当前状态与恢复入口

2026-09-19：五名及其独立技能／表现／产物已实现，接入正式发布与实验室；等待用户试玩反馈。用户最新要求由自己在战斗实验室验收，Agent停止继续开游戏、录屏与表现检查，只完成实现及必要编译。没有用户体验认可记录。

### 已实现

- 五个独立单位：`enemy_ee07_furnace_lizard`、`enemy_ee08_return_blade`、`enemy_ee09_swap_envoy`、`enemy_eb01_rampart_warden`、`enemy_eb02_shell_matriarch`。每项都有场景、单位定义、肖像、目录入口、技能与技能组资源。
- 三个独立产物：`enemy_rampart_segment`、`enemy_brood_egg`、`enemy_brood_larva`。岩墙由三段普通可攻击身体组成；卵孵化和墙到期记录离场，避免变成可重复消费的尸体。
- `EnemyActionCompiler`／五个独立OperationSpec进入发布校验、能力指纹和召唤依赖解析；`BattleEnemyActions`持有不可变施法、往返命中、阶段及产物记录，世界检查点负责回滚。虫母的实例体型／攻击方式／弹体覆盖同样纳入回滚。
- 返刃0.4秒自然挥臂离手，无额外蓄力、警示线或固定长收招；独立旋转刀体走固定去回路线，飞出后不因投手死亡／移动中断；飞行未完时普攻仍占用。
- 新增八项独立VFX：吐息、返刃、换位、地裂、岩体升起、裂壳、爪痕、酸液；形状、方向、时序来自权威事实，暂停与清理共用正式播放器。岩墙素材尺寸及吐息遮挡问题已在最后收尾修正，按用户要求没有重开引擎复验。
- 正式遭遇：首区精英领队从原ES01与EE07／EE08中选一；末区从原ES04与EE09中选一；中区EE03保持。首区／末区Boss分别接入EB01／EB02的独立timeline；基础兵池和总数保持。旧Boss资源保留，整体强度未调平衡。
- 战斗实验室预设：`EE07 · 炉喉蜥卫 · 机制试战`、`EE08 · 返刃投手 · 机制试战`、`EE09 · 换位妖使 · 机制试战`、`EB01 · 岩垒督军 · 机制试战`、`EB02 · 裂壳虫母 · 机制试战`。

- 设计：`design-discussion/04-content-validation/content-drafts.md` Q23；`decisions.md` D51、D52（D50 为并行英雄工作）。
- 参考与表现：`design-discussion/02-foundation-models/combat-presentation/`、`work-items/active/combat-vfx-and-preview.md`。
- 战斗接入：`BattleSimulation` partial、`BattleChargedLines`、`BattleProjectiles`、`BattleDisplacements`；保留并行 `BattleCombatTechniques` 接入。
- 内容及验证参考：`enemy_ee03_trample_brute`、`EnemyTrampleContractSmoke` / `EnemyTrampleVisualSmoke`。
- 编译：`dotnet build my-team.csproj -maxcpucount:2 -v:minimal`；Godot 4.7 .NET 位于 `C:/Users/qs/Desktop/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe`。

## 验证记录

- 用户停止频繁验证前，`EnemyFiveContractSmoke.tscn`通过：正式包发布、三段吐息及中断、返刃两程去重／死亡与移动后固定回程、最远目标原子换位及失败冷却、自动普攻拆墙、地裂跨自身墙、阶段保留血量／盾与缩体后退、虫卵孵化／存活上限／批次与母死清理、失败提交回滚、预设确定性及正式遭遇完整身体落位。
- 已完成的渲染记录只有EE07／EE08／EE09／EB01正式画面、真实暂停按钮输入和清理检查；EB02有部分录制，无完整成功标记。初次渲染发现墙尺寸及吐息遮挡问题，已经修改。**这些记录不等于最后收尾版本的画面验收，更不是用户认可。**
- 用户随后明确自己验收，停止后续引擎运行。最终收尾的低并发C#编译已通过，0警告、0错误；不复跑视觉或全局回归。
- 原始日志与帧：`.godot/enemy-five-contract.log`、`.godot/enemy-five-visual.log`、`.godot/enemy-five-review/`。不操作玩家存档；无提交／推送。

## 用户试玩入口与观察点

主菜单 → 战斗实验室 → 加载对应编号的预设。可自由修改阵容与站位：吐息看固定朝向与打断；返刃看两程和原点回收；换位看两端保护关系；督军看自动拆墙与地裂后空档；虫母打至半血后看缩体、虫卵可破坏及幼虫清理。收到反馈后直接修改对应机制或资源，不默认重启录制流程。
