# Continuous-Space Combat QA

## First Boss ranged-stall regression (2026-09-13)

`FirstBossRangedStallSmoke.tscn` loads a checked-in reproduction through an in-memory save service and runs the production first-Boss encounter. It rejects sustained targetless ranged waiting in the reachable narrow-lane arena. A smaller stationary-target case checks actual projectile damage after detouring, legal swept movement, body clearance, projectile-width line access and identical repeated movement traces; a full-height wall must remain unreachable. No test accesses or writes live player saves.

Passed with the production fix, together with `GameplayContractSmoke --movement-only`, `AlliedBodyNavigationContractSmoke` and `RangedAttackContractSmoke`. The full encounter no longer loses its ranged targets; the minimal detour hits in 14 ticks. This is simulation evidence, not a claim of user acceptance or a full campaign test. Recovery and before/after trace paths are recorded in `work-items/active/ranged-attacks-and-basic-effects.md`.

## Contract

- Deployment, saved formations, Battle Lab placement, and authored spawn points remain unique integer-cell anchors. Full circular bodies must also fit the terrain and avoid overlaps; one anchor does not restrict a body to one cell.
- Battle setup converts each cell to its logical center. From the first battle tick onward, authoritative positions are continuous `Vector2` values and may stop between cell centers.
- `BattleSimulation` and its deterministic movement service own targeting, range, terrain clearance, body nonpenetration, hit legality, summons, events, reports, and rollback. Presenter transforms and Godot physics overlap callbacks are projections only.
- A unit's circular body radius is separate from attack reach. Attack, healing, aura, beacon, splash, and piercing checks use the current continuous positions and the documented body-edge convention.

## Replaced Regression Map

| Former grid assumption | Continuous-space replacement | Automated evidence |
| --- | --- | --- |
| Every combat position is a cell and every move ends on a cell center | Deployment starts on centers, then authoritative move events and the digest retain non-center positions | `GameplayContractSmoke` continuous-position and digest cases |
| One destination cell can have one winner | Living circular bodies cannot overlap; distinct engagement points allow multiple attackers around one target | `GameplayContractSmoke` swept-bodies, dense-traffic, and distinct-engagement cases |
| Opposing movers wait for a cell to clear | Pair-stable side steering and bounded replanning let symmetric traffic pass without tunnelling or permanent deadlock | `GameplayContractSmoke` symmetric-traffic case |
| Range and proximity use cell or Manhattan distance | Body-edge distance and current line access decide attack, heal, aura, beacon, and area effects | `GameplayContractSmoke` splash, pierce, live-aura, beacon, healing, and edge-LOS cases |
| A summon needs an empty cell | A summon needs a legal continuous position with terrain and body clearance, or fails without partial state | `GameplayContractSmoke` summon-nonoverlap and death-cleanup cases |
| Rollback restores cells and a cell-route request | Rollback restores continuous positions, goals, targets, requests, wait state, digest, and deterministic replay | `GameplayContractSmoke` spatial-rollback case |
| Nearby side steering alone can clear a stationary allied formation | Route around an allied column, a crowded front and a column against the arena edge; preserve body clearance and attack the reachable target without pushing allies | `AlliedBodyNavigationContractSmoke`; `MeleeContactVisualSmoke --allied-blockers` for production rendering |
| Presentation eases and restarts once per cell | Presentation follows ordered authority samples at their cadence, preserves terrain corners, bounds backlog, and owns one sustained travel state | `MovementPresentationContractSmoke` x1/x2/x4, 30 FPS, hitch, pause, turn, resize, and defeat cases |

## Manual Acceptance

1. Deploy several units through the normal deployment screen. Before starting, verify every piece has a unique legal anchor and its full body fits without overlap. Start battle and verify every unit initially appears at the exact previewed cell center.
2. Pause after several movement ticks and inspect at least three living units. At least one should be visibly between cell centers. The visible unit, selection hit target, selected-unit panel, event position, and reported derived cell must refer to the same authoritative unit state.
3. Run three melee attackers into one durable enemy. They should approach different points around its circular body, stop without overlap, and attack concurrently when space permits. They must not collapse into one point, push the target as a rigid body, or bounce.
4. Observe dense allied traffic and two equal-radius units approaching head-on. Bodies must not tunnel or remain overlapped. Short waits are allowed; a reachable useful action must eventually resume through side steering, staging, replanning, or target release.
   For the stationary-ally regression, use free placement: Iron Guard at (1,2), allied static dummies at (3,1), (3,2), (3,3), and an enemy static dummy at (7,2). Coordinates are zero-based. The hero should go around an end of the column and attack; the allied dummies stay in place. The automated rendering fixture disables the hero's loadout to isolate ordinary navigation.
5. Use blocked or concave terrain. Units may travel continuously inside open space but their swept body must not clip a blocked cell or battlefield edge. A ranged unit whose direct line is blocked should seek a reachable firing point; it must not attack through terrain.
6. Check an attacker, healer, live aura, and objective beacon near their exact range boundary. Moving a body slightly across the edge must change legality according to body-edge distance, independent of the derived cell. Cooldown recovery must never enlarge the reach.
7. Exercise splash and piercing with targets in different derived rows or outside the former cell-radius band but intersecting the continuous circle/capsule. Each runtime target is hit at most once per action, and an illegal or dead target is excluded immediately.
8. Kill a moving blocker and create a temporary summon in the same fight. Death must immediately release its body, target, and goal state. The summon must appear at a legal nonoverlapping continuous position or fail atomically.
9. Repeat the same seed at x1, x2, x4, pause/resume, and Battle Lab single-step. Outcome, terminal tick, event order, digest, report positions, and damage facts must match. At 4x near 30 FPS, visible lag remains bounded and movement does not restart or flash at each 0.1-second sample.
10. Resize between 1280×720 and 1600×900 while units move. The projection, selection, markers, character lift, and interpolation endpoints must resize together; the simulation result and logical positions must not change.

## EE03 大体型分边冲锋（2026-09-19）

- 2026-09-20当前资源机制：EE03仅一个满蓝主动，初始90／100、每秒12.5，普攻／受伤不回蓝。满蓝开始蓄力即清空，蓄力、冲锋、收招期间不回蓝，结束后恢复；无合法目标或受控保留满蓝。原专项新增非英雄法力池、资源条数值／可见绑定、主动分类、未满不放、完整动作锁、下一满蓝无隐藏冷却、缺目标／受控与资源回滚检查，无窗口通过，日志 `.godot/enemy-trample-mana.log`。资源条画面、节奏和操作待用户验收；下列视觉记录只证明之前的冲锋动作。

- `EnemyTrampleContractSmoke`使用正式内容包，覆盖蓄力、锁向、连续扫掠、多目标分边／友军无伤／伤害去重、控制和阵营取消、墙边无穿透、出生与实验室完整身体合法性、预览和正式开战位置一致、确定性、提交失败回滚与清理。
- `EnemyTrampleVisualSmoke`使用正式BattleScreen与隔离预设，关闭对照单位移动和普攻以看清动作；验证预警、冲锋、真实鼠标暂停输入及表现时钟／清理。已查看正常速度录制，证据与限制见`work-items/active/enemy-trample-charge.md`。
- 上述专项、友军绕行和贯穿兵回归通过。旧`BattleLabCoreContractSmoke`因legacy内容目录与当前池不匹配而在发布阶段失败，未执行断言；不记为本次通过项。
- 人工：主菜单→战斗实验室→搜索EE03，播放「破阵巨兽」预设。检查大身体、蓄力方向与冲锋方向相同、沿途单位向两侧分离、每敌人一次伤害、结束收招；再用自由布阵把巨兽靠近墙与密集队列，确认不重叠穿墙。玩家确认混战可读性和第二区域精英压力是否适当。

```powershell
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/EnemyTrampleContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --path . tests/EnemyTrampleVisualSmoke.tscn
```

## Focused Commands

### 怒拳无蓝条件与单位技能队列（2026-09-20）

- 无窗口专项：`tests/GritActionQueueContractSmoke.tscn`。正式包加载后检查满怒／半血／20%边界、合并与依次施放、多个实例独立、濒死每战一次且复活不重置、取消／资源过期／盾清理／失败回滚与确定性。通过记录在`.godot/grit-action-queue.log`，不等于画面或手感验收。
- 手动入口：实验室HC38。增加敌方输出，让第一次重拳开始后生命继续跌到20%以下；观察先完成第一拳和收势，再开始第二拳，持续低血量不会反复空放。另验证治疗回半血后重新跌破，以及同场两个怒拳各自施放。
- 本轮不打开窗口或录屏，不自动扩展到全塔或其他英雄回归。

### 五名精英／首领（2026-09-19）

当前用户要求由自己在战斗实验室验收；下面入口保留供明确需要时使用，不自动反复启动引擎。

- 玩法与碰撞专项：`tests/EnemyFiveContractSmoke.tscn`。覆盖发布资源、命中次数与取消、原子换位、可攻击岩墙、缩体／产卵限额及正式落位；不代表平衡／画面通过。
- 表现夹具：`tests/EnemyFiveVisualSmoke.tscn`，可用`--case=EE07`等单项及`--capture=...`。夹具静止双方以隔离动作，不能作为实际阵容胜率依据；用户要求停止后未再运行。
- 手工：战斗实验室加载EE07／EE08／EE09／EB01／EB02“机制试战”。分别观察扇形锁向、返刃往返、双端换位、自动拆墙与地裂、半血裂壳／虫卵。实现和实际覆盖边界归`work-items/active/enemy-five-elite-bosses.md`。

```powershell
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlliedBodyNavigationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn -- --movement-only
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/MovementPresentationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/FormationDeploymentContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/BattleLabBattleLifecycleContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlphaRunSmoke.tscn
```
