# Growth holdout 诊断

未参与曲线选择的 seed `2718 / 4815`，四路线共 8 局，用时 29.19 秒。

| 路线/seed | 结果 | 败战 | 最终 Boss outcome/tick/我方存活 |
|---|---|---|---|
| frost-battle/2718 | 三Boss全胜 | 无 | PlayerVictory/369/10 |
| frost-battle/4815 | 三Boss全胜 | 无 | PlayerVictory/262/10 |
| construct-growth/2718 | 三Boss全胜 | 无 | PlayerVictory/511/9 |
| construct-growth/4815 | 三Boss全胜 | 第6层Combat | PlayerVictory/574/10 |
| construct-research/2718 | 三Boss全胜 | 无 | PlayerVictory/511/9 |
| construct-research/4815 | boss-defeat | 第6层Combat、第10层Boss | PlayerDefeat/484/0 |
| scattered-no-investment/2718 | 三Boss全胜 | 无 | PlayerVictory/1233/6 |
| scattered-no-investment/4815 | boss-defeat | 第6层Combat、第8层Combat、第10层Boss | PlayerDefeat/503/0 |

frost 与 growth 均 2/2；research 为 1/2，scatter 为 1/2。scatter 2718 虽通关但终局耗时 1233 tick、仅 6 人存活，属于低余裕反例；4815 在二区 Boss 止步。

> 遥测限制：本轮 JSON 的 `Player.Cell` 因 Godot `Vector2I` 直接序列化而显示为 `{}`；seed、策略、成员、胜负和其他统计有效，但不能据此声称保存了站位。后续精英复跑已改为显式 `X/Y`。
