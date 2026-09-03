# 自走棋与相邻构筑机制研究层

本目录是图谱的外部证据层，不是玩法权威，也不会改变现有 72 张原创机制卡。正式构筑语法仍以 `gameplay-design/combat-build-framework.md` 为准，核心规则仍以 `gameplay-design/tower-autobattler-core.md` 为准。

## 研究方法

- 检索日期：2026-09-02（Asia/Shanghai）。
- 先宽搜，再按机制新颖度、证据质量和项目适配度归纳；低适配、已停运和未发行样本不会因“不好用”而从语料中消失。
- Steam 商店材料通过 Valve 官方 `appdetails` 接口读取完整 `about_the_game` / `detailed_description`，索引同时保留玩家可访问的商店页与实际读取接口。搜索结果只用于发现候选，不作为机制证据。
- 官方更新、官方商店/产品页标为 `primary`；维护型 Wiki 或社区攻略只能标为 `secondary/community`。本轮结构化记录没有把社区推测冒充官方规则。
- 同一游戏可以贡献多个记录，但只在输入、触发、结算、空间约束、成长模型或反制至少一项实质不同的情况下计作不同机制。

## 数据结构

`mechanic-evidence.json` 包含 `sources` 与 `records`。每条记录通过 `source_id` 解析来源的 URL、类型、质量与访问日期；记录本身必须包含：

- `id`、`game`、`subgenre`、`content_type`、`mechanism_type`、`domain`；
- `mechanism`、`input_state`、`trigger`、`scope`、`output_payoff`；
- `scaling_model`、`limits_safeguards`、`build_role`、`spatial_condition`；
- `counterplay`、`known_risk_failure`、`transferability`、`project_note`。

`transferability` 只允许 `high`、`conditional`、`low`、`reject`。它表示对本项目的可迁移性，不表示原游戏品质或商业成败。

`content_type` 是便于统计的五类证据分组：`observed-structure`、`steam-long-tail`、`adjacent-transfer`、`historical-or-reworked`、`negative-evidence`；更细的机制来源形态保留在 `mechanism_type`。

## 来源质量

- `A-primary-rules`：官方规则、官方更新或开发者明确机制说明。
- `B-primary-store`：开发者/发行商提交的官方商店完整说明；适合确认产品结构与显式特性，不足以证明精确数值或隐藏结算。
- `C-secondary-maintained`：维护中的 Wiki/高质量机制资料；必须和官方陈述区分。
- `D-community`：社区观察，只能作为待复核线索。本轮未用 D 级材料支撑确定性结论。

## 更新方法

1. 先在 `source-index.md` 增加来源，记录版本、日期与可访问性。
2. 为新记录分配唯一 ASCII id；不要复制原文效果名、数字、叙事或完整套装。
3. 用 `source_id` 关联来源，并明确不确定性、版本依赖和 PvP/共享商店等不可迁移假设。
4. 运行 JSON 解析、唯一 id、来源引用、必填字段与覆盖统计检查。
5. 只有经过新的用户确认，研究结论才可进入正式玩法文档。

## 已知边界

商店页擅长揭示产品级循环、布阵、装备、招募与构筑结构，但通常不公开确定性顺序、递归保护、精确伤害公式或失败原因。此类细节在记录中标为风险/待验证，而不是自行补全。部分 2026 年后发售或仍未发行的长尾样本只证明其公开设计承诺，不能当作已验证的实战表现。
