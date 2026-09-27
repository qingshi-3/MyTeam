# Growth 正式混合曲线诊断

正式 growth 资源的一区倍率保持 alpha，二区普通/精英 ×1.25/1.15、Boss ×1.5/1.25；三区普通/精英 ×1.25/1.25、Boss ×1.10/1.40。7 路线 × 3 原 seed 共 21 局，用时 64.61 秒。

| 路线 | 三 Boss 全胜 | 失败位置 | 旧曲线终局 outcome/tick（1776,2026,9173） | 新曲线终局 outcome/tick |
|---|---:|---|---|---|
| frost-battle | 3/3 | 无 | Victory/392, Victory/245, Victory/533 | Victory/484, Victory/256, Victory/491 |
| construct-growth | 3/3 | 无 | Victory/753, Victory/259, Victory/520 | Victory/1058, Victory/280, Victory/586 |
| construct-research | 3/3 | 无 | Victory/753, Victory/259, Victory/525 | Victory/1072, Victory/345, Victory/591 |
| construct-no-growth | 3/3 | 无 | Victory/996, Victory/385, Victory/356 | Victory/1090, Victory/647, Victory/397 |
| construct-late-growth | 0/3 | 1776:第10层Boss；2026:第10层Boss；9173:第2层Combat；9173:第6层Combat；9173:第8层Combat；9173:第10层Boss | Victory/387, Victory/293, Defeat/397 | Defeat/502, Defeat/334, Defeat/208 |
| scattered-no-investment | 0/3 | 1776:第10层Boss；2026:第10层Boss；9173:第8层Combat；9173:第10层Boss | Victory/645, Victory/1141, Victory/1055 | Defeat/419, Defeat/296, Defeat/483 |
| construct-concentrated | 2/3 | 1776:第7层Combat；9173:第2层Combat；9173:第6层Combat；9173:第8层Combat；9173:第10层Boss | Victory/571, Victory/350, Defeat/297 | Victory/516, Victory/261, Defeat/217 |

结论：无生产者但依靠升阶与协同的 `construct-no-growth` 仍 3/3；无主动培养的散搭与前两区保留生产者的 late 路线均在二区 Boss 止步；集中路线为 2/3。最终 Boss 没有出现超时，成熟路线最长为 no-growth 1776 的 1090 tick，其次 research 1776 的 1072 tick。

护阵现为 180 tick：三个 checkpoint 的前排首次承伤分别为 124、93、14 tick，均发生在护盾到期前；对应总 tick 为 378、336、255，none 为 378、345、266。蓄势将法力核心首次施法从 46→34、45→33、54→32 tick。

## 遭遇基础倍率（HP / 伤害）

下表仍需逐层乘以各遭遇的 `LocalHealthMultipliers` / `LocalDamageMultipliers`；因此一区普通战的逐层实际倍率并非恒定 1。

| 区域 | 普通 | 精英 | Boss |
|---|---|---|---|
| 一区 | 1 / 1 | 1 / 1 | 1.25 / 1.5 |
| 二区 | 2.1875 / 1.61 | 2.125 / 1.38 | 2.4 / 1.4375 |
| 三区 | 3.75 / 1.6875 | 3.75 / 1.6875 | 3.3 / 1.82 |

这些是 growth 独立资源的最终有效倍率；alpha 资源未修改。

> 遥测限制：本轮 JSON 的 `Player.Cell` 因 Godot `Vector2I` 直接序列化而显示为 `{}`；seed、策略、成员、胜负和其他统计有效，但不能据此声称保存了站位。后续精英复跑已改为显式 `X/Y`。
