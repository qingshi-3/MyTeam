# 成长压力 profile 诊断

本轮运行 `gentle` 与 `standard`，每档 7 路线 × 3 seed，共 42 条连续征程；未运行 `pressure`。profile 只在测试用 `CompiledCampaign` 克隆中乘敌方生命与伤害，一区保持正式值。旧 baseline 与 counterexample 文件未覆盖。

- gentle：72.10s
- standard：74.46s
- research 定向复跑（仅修正 checkpoint 遥测后合并）：gentle 18.47s，standard 18.73s

## 连续征程结果

| profile | 路线 | 三 Boss 全胜 | 败战分布（一区/二区/三区） | Boss 中位：胜场/场次，存活，tick（一区→三区） | 实际投入范围：生产/升阶/法术 | Boss 激活羁绊数（一区→三区） |
|---|---|---:|---|---|---|---|
| gentle | frost-battle | 3/3 | 0/0/0 | 3/3，4，267 → 3/3，6，248 → 3/3，9，695 | 1–8/3–3/0–0 | 1–4 → 5–7 → 6–8 |
| gentle | construct-growth | 3/3 | 0/0/0 | 3/3，4，345 → 3/3，6，303 → 3/3，9，892 | 14–14/3–3/0–0 | 1–2 → 5–5 → 6–7 |
| gentle | construct-research | 3/3 | 0/0/0 | 3/3，4，347 → 3/3，6，304 → 3/3，8，1189 | 14–14/3–3/3–3 | 1–2 → 5–5 → 6–7 |
| gentle | construct-no-growth | 3/3 | 0/0/0 | 3/3，4，292 → 3/3，5，308 → 3/3，10，664 | 0–0/3–3/0–0 | 2–3 → 5–6 → 5–8 |
| gentle | construct-late-growth | 0/3 | 1/4/0 | 3/3，2，528 → 0/3，0，334 → — | 0–0/0–0/0–0 | 1–2 → 3–4 → — |
| gentle | scattered-no-investment | 0/3 | 0/3/0 | 3/3，3，288 → 0/3，0，296 → — | 0–0/0–0/0–0 | 2–3 → 3–5 → — |
| gentle | construct-concentrated | 2/3 | 1/3/0 | 3/3，3，337 → 2/3，6，238 → 2/2，10，576.5 | 9–32/2–3/0–0 | 1–2 → 4–6 → 5–6 |
| standard | frost-battle | 2/3 | 0/0/1 | 3/3，4，267 → 3/3，6，320 → 2/3，8，1019 | 1–8/3–3/0–0 | 1–4 → 5–7 → 6–8 |
| standard | construct-growth | 2/3 | 0/0/1 | 3/3，4，345 → 3/3，5，378 → 2/3，8，1474 | 14–14/3–3/0–0 | 1–2 → 5–5 → 6–7 |
| standard | construct-research | 1/3 | 0/0/2 | 3/3，4，347 → 3/3，5，376 → 1/3，6，1436 | 14–14/3–3/3–3 | 1–2 → 5–5 → 6–7 |
| standard | construct-no-growth | 2/3 | 0/1/0 | 3/3，4，292 → 2/3，4，392 → 2/2，7，937 | 0–0/2–3/0–0 | 2–3 → 5–6 → 5–5 |
| standard | construct-late-growth | 0/3 | 1/5/0 | 3/3，2，528 → 0/3，0，290 → — | 0–0/0–0/0–0 | 1–2 → 3–4 → — |
| standard | scattered-no-investment | 0/3 | 0/4/0 | 3/3，3，288 → 0/3，0，252 → — | 0–0/0–0/0–0 | 2–3 → 2–5 → — |
| standard | construct-concentrated | 2/3 | 1/4/0 | 3/3，3，337 → 2/3，5，312 → 2/2，8.5，914.5 | 9–32/2–3/0–0 | 1–2 → 4–6 → 5–6 |

判读：

- gentle 已形成目标中的区分：四条有主动培养或机制升阶的路线全部 3/3，低重叠无主动培养与前两区保留生产者的 late 路线均止于二区 Boss；无生产者的 `construct-no-growth` 仍 3/3，说明曲线没有强制工坊。集中路线仅 9173 止于二区 Boss。
- standard 明显更苛刻：成熟路线也在三区出现失败或超时，research 仅 1/3 三 Boss 全胜；`construct-no-growth` 9173 已止于二区。它能区分投入，但会把供应/阵容波动放大为成熟路线失败。
- 两档已能区分主责提出的“二区组合或投入、三区不可零投入轻松全过”，无需再跑 pressure。gentle 更接近“保留多种可行成长路线”，standard 更适合上界压力探测。

## 法术 checkpoint 边际对照

checkpoint 位于 research 首次达到 4 点研究、选中真实战斗节点且尚未消费时。gentle 与 standard 的一区保持相同倍率，因此两档结果完全一致，下表只列一次。三个分支从同一序列化存档恢复；guard/gather 均真实 Craft、Equip、Begin，none 不花研究。

| seed | none：目标/首法力技/tick | guard：前排/施盾-到期/首次承伤/是否先过期/tick | gather：核心/首法力技/tick |
|---:|---|---|---|
| 1776 | hero_mx23 / 46 / 378 | hero_mx21 / 0–40 / 124 / 是 / 378 | hero_mx23 / 34 / 389 |
| 2026 | hero_mx23 / 45 / 345 | hero_mx21 / 0–40 / 93 / 是 / 345 | hero_mx23 / 33 / 347 |
| 9173 | hero_mx22 / 54 / 266 | hero_mx21 / 0–40 / 14 / 否 / 255 | hero_mx22 / 32 / 256 |

- guard 在 1776、2026 中 tick 40 已过期，目标到 tick 124、93 才首次承伤，战斗 tick 与 none 完全相同；该用法没有可见边际收益。9173 在 tick 14 已接敌，护盾仍有效，战斗从 266 缩短到 255 tick。
- gather 把 MX23 的首次法力技从 46→34、45→33 tick，把 MX22 从 54→32 tick，目标选择已不再固定 MX25；但总战斗耗时分别变为 389、347、256，相对 none 为 +11、+2、-10 tick，当前样本不支持它稳定提速。
- 连续 research 路线只在 Boss 前消费 gather，每个 seed 实际消费 3 次，目标与能力事件保存在 JSON 的 `ConsumedSpellTargetInstanceId`、单位技能事件和 checkpoint 中。

完整逐节点结果、装备、真实羁绊阈值、成长账本、Boss 存活/tick 与 checkpoint digest 见 `growth-profile-journeys.json`。
