# Growth 路线最终验证记录

记录范围为最后一轮低并发构建、成长领域合同、Growth Workbench 真实输入检查和修正后的精英优先征程。以下命令均在 `C:\Users\qs\godot\my-team` 执行。本轮控制台输出没有另存为完整日志；`growth-run-contract-final.log` 是 14:03 的旧轮结果，不包含后来加入的满员 discovery 容量不变量，因此不作为本轮日志引用。

## 构建

命令：

```powershell
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
```

- 退出码：`0`
- 关键 stdout：`已成功生成。`、`0 个警告`、`0 个错误`
- 本轮未保存独立完整构建日志。

## GrowthRunContractSmoke

命令：

```powershell
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . --scene res://tests/GrowthRunContractSmoke.tscn
```

- 退出码：`0`
- 关键 stdout：`GROWTH_RUN_CONTRACT_OK commands rollback freeze retry settlement clone migration discovery materials`
- 本轮代码已包含满员 roster/discovery 容量不变量检查；该检查随上述合同场景整体通过。控制台成功标记是聚合标记，没有为这一子检查单独打印名称。
- 退出清理 stderr：`WARNING: 2 ObjectDB instances were leaked at exit`；`ERROR: 2 resources still in use at exit`。
- `growth-run-contract-final.log` 是旧轮日志，不能代替本轮证据。

## 修正后的精英优先征程

命令：

```powershell
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . --scene res://tests/GrowthJourneyDiagnostics.tscn -- --seeds=2718 --suite=frost-elite,growth-elite --output=growth-elite-journeys
```

- 退出码：`0`
- 关键 stdout：两条 `GROWTH_JOURNEY_PROGRESS profile=authored`；`GROWTH_PROFILE_COMPLETE profile=authored elapsed_seconds=4.74`；`GROWTH_JOURNEY_DIAGNOSTICS_OK profiles=1 strategies=2 journeys=2`
- 两条路线的 `AvailableEliteOptions` 都是 `[2,2,0]`，`EliteNodesByRegion` 都是 `[1,1,0]`。
- 实际进入 4 场 Elite：两条路线各在一区第 4 层和二区第 8 层进入一次，4 场全部胜利；两条征程随后都在第 10 层二区 Boss 失败。第三区为 0 是因为征程已终止。
- 退出清理 stderr：`WARNING: 2 ObjectDB instances were leaked at exit`；`ERROR: 2 resources still in use at exit`。
- 结果文件：[growth-elite-journeys.json](./growth-elite-journeys.json) 和 [growth-elite-journeys.md](./growth-elite-journeys.md)。原先 Recruit 优先造成 0 次 Elite 进入的历史结果保存在 `growth-elite-journeys-zero-coverage.*`，不计入最终统计。

## GrowthWorkbenchInputSmoke

命令：

```powershell
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --path . --scene res://tests/GrowthWorkbenchInputSmoke.tscn
```

- 退出码：`0`
- 关键 stdout：`GROWTH_WORKBENCH_INPUT_OK assignment ascension-discovery keyboard spell-preset`
- 使用现有真实输入夹具完成生产分配、升阶发现、键盘路径和法术预设检查，并更新同路径截图 [growth-workbench.png](./growth-workbench.png)。
- 渲染环境 stdout：`OpenGL API 3.3.0 NVIDIA 610.62 - Compatibility - Using Device: NVIDIA - NVIDIA GeForce RTX 4070`
- 退出清理 stderr：`WARNING: 2 ObjectDB instances were leaked at exit`；`ERROR: 2 resources still in use at exit`。

## 最终 31 局汇总

统计只读取以下最终文件：

- `growth-authored-journeys.json`：21 局
- `growth-holdout-journeys.json`：8 局
- `growth-elite-journeys.json`：修正后的 2 局

未计入 `growth-elite-journeys-zero-coverage.json` 的历史 2 局，也未重复计入 research checkpoint 遥测修正时定向复跑的 6 局。

| 指标 | 结果 |
|---|---:|
| 连续征程 | 31 |
| 真实战斗 | 261 |
| 战斗胜利 | 238 |
| 战斗失败 | 23 |
| 战斗超时 | 0 |
| 完成整段征程 / 三个 Boss 全胜 | 20 |
| Boss 失败终止 | 11 |
| 实际 Elite 战斗 | 4 |
| Elite 胜利 | 4 |

上述 31 局均走真实经济、节点选择、存档和战斗结算路径；没有注入 Elite 节点、资源或胜利结果。
