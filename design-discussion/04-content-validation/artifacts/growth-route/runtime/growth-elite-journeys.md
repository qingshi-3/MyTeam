# Growth 精英优先诊断

seed 2718；选择规则为每区 local floor≥2 后，若 Elite 可用且本区尚未进入过 Elite，则优先一次；其余机会沿普通路线优先级。全程使用真实塔层选项。

| 路线 | 每区Elite可用次数 | 每区实际进入 | Elite战绩 | 停止位置 |
|---|---|---|---|---|
| frost-elite | [2, 2, 0] | [1, 1, 0] | 2/2胜 | 第10层二区Boss败 |
| growth-elite | [2, 2, 0] | [1, 1, 0] | 2/2胜 | 第10层二区Boss败 |

两条路线都真实进入 2 次 Elite：一区第4层、二区第8层，四场 Elite 全胜；随后均在二区 Boss 阵亡。第三区为 0 是因为征程已提前终止。

本文件由修正后的坐标遥测生成，`Player.Cell` 明确保存 `X/Y`。先前 authored/holdout 及零精英覆盖 JSON 的 `Cell` 被 System.Text.Json 写为 `{}`；其 seed、策略、阵容成员、胜负与其余遥测仍可复现，但不可用于还原站位。原先“Recruit 优先导致 Elite 进入 0 次”的两局保存在 `growth-elite-journeys-zero-coverage.*`。
