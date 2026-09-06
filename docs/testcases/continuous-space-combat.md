# Continuous-Space Combat QA

## Contract

- Deployment, saved formations, Battle Lab placement, and authored spawn points remain unique integer cells.
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
| Presentation eases and restarts once per cell | Presentation follows ordered authority samples at their cadence, preserves terrain corners, bounds backlog, and owns one sustained travel state | `MovementPresentationContractSmoke` x1/x2/x4, 30 FPS, hitch, pause, turn, resize, and defeat cases |

## Manual Acceptance

1. Deploy several units through the normal deployment screen. Before starting, verify every piece occupies one legal cell and duplicate placement is rejected. Start battle and verify every unit initially appears at the exact previewed cell center.
2. Pause after several movement ticks and inspect at least three living units. At least one should be visibly between cell centers. The visible unit, selection hit target, selected-unit panel, event position, and reported derived cell must refer to the same authoritative unit state.
3. Run three melee attackers into one durable enemy. They should approach different points around its circular body, stop without overlap, and attack concurrently when space permits. They must not collapse into one point, push the target as a rigid body, or bounce.
4. Observe dense allied traffic and two equal-radius units approaching head-on. Bodies must not tunnel or remain overlapped. Short waits are allowed; a reachable useful action must eventually resume through side steering, staging, replanning, or target release.
5. Use blocked or concave terrain. Units may travel continuously inside open space but their swept body must not clip a blocked cell or battlefield edge. A ranged unit whose direct line is blocked should seek a reachable firing point; it must not attack through terrain.
6. Check an attacker, healer, live aura, and objective beacon near their exact range boundary. Moving a body slightly across the edge must change legality according to body-edge distance, independent of the derived cell. Cooldown recovery must never enlarge the reach.
7. Exercise splash and piercing with targets in different derived rows or outside the former cell-radius band but intersecting the continuous circle/capsule. Each runtime target is hit at most once per action, and an illegal or dead target is excluded immediately.
8. Kill a moving blocker and create a temporary summon in the same fight. Death must immediately release its body, target, and goal state. The summon must appear at a legal nonoverlapping continuous position or fail atomically.
9. Repeat the same seed at x1, x2, x4, pause/resume, and Battle Lab single-step. Outcome, terminal tick, event order, digest, report positions, and damage facts must match. At 4x near 30 FPS, visible lag remains bounded and movement does not restart or flash at each 0.1-second sample.
10. Resize between 1280×720 and 1600×900 while units move. The projection, selection, markers, character lift, and interpolation endpoints must resize together; the simulation result and logical positions must not change.

## Focused Commands

```powershell
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/MovementPresentationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/FormationDeploymentContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/BattleLabBattleLifecycleContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlphaRunSmoke.tscn
```
