# Deep Research Schema

## Title disposition

- `pending`：候选已锁定，尚未完成深检索。
- `anchor-retained`：锚点档案；至少 6 个实质非商店页面、至少 3 种来源类型，包含规则与策略，存在时还要有版本/社区材料。
- `retained`：至少 2 个功能不同的实质非商店来源，能够解释一个真实构筑及其反制。
- `discovery-only`：未发行或只能确认公开承诺，不产生深证据。
- `insufficient-evidence`：已发行但可访问材料不足；记录查询和失败路径。
- `rejected`：有足够资料，但机制不适用或研究价值不足；仍保留 dossier 和理由。

## Source record

每条 deep source 必须包含：

- `id`、`title_id`、`title`、`url`、`author_or_publisher`、`published_or_updated`、`accessed`；
- `source_type`：`official-rules`、`official-patch`、`official-dev`、`maintained-wiki`、`strategy-guide`、`video-transcript`、`detailed-review`、`statistics`、`community-analysis`、`historical-archive`；
- `quality`：`A-primary`、`B-maintained`、`C-practical`、`D-community`；
- `accessibility`、`version_scope`、`supports_claims`、`limitations`。

URL 必须唯一。搜索结果页不进入索引。视频只有在可读取字幕/转录或记录时间戳时计入。

## Dossier contract

每份 dossier 包含：身份与时期、检索日志、来源表、真实循环与玩家决策、构筑例、构筑语法分解、经济/招募/转型、空间后果、反制/坏对局/适应窗口、失败解释、版本生命周期、社区观察、项目迁移、未决问题和 disposition。

构筑例必须至少说明：

> engine + state/resource + payoff + survival + spatial condition + payoff owner + pivot/counter + version context

每个 `anchor-retained` dossier 至少有一套可回到具体来源核验的具名构筑；必须记录明确版本/时期、关键单位或等价组件、核心装备/遗物、站位或发动条件、收益所有者和反制。`Fast 8`、`reroll`、`召唤流`等宏观经济线或流派标签可以作为额外 archetype，但不能单独满足具名构筑要求。若来源只显示阵容图而正文无法读取关键字段，该构筑不计入此门槛。

## Deep evidence record

`deep/mechanic-evidence.json` 的记录必须具有：

- identity：`id`、`title_id`、`game`、`claim_type`、`confidence`、`version_scope`；
- evidence：`source_ids`（至少 2 个，且无 store-only）、`rule_support`、`practical_support`、`disagreement_or_limit`；
- mechanism：`domain`、`mechanism`、`engine`、`state_resource`、`trigger`、`scope`、`payoff`、`survival`、`spatial_condition`、`payoff_owner`；
- decisions：`recruitment_economy_pivot`、`counterplay`、`failure_explanation`；
- transfer：`transferability`、`incompatible_assumptions`、`project_question`。

`claim_type` 只允许 `rule`、`strategy`、`lifecycle`、`community-observation`、`design-inference`；`confidence` 只允许 `high`、`medium`、`low`。设计推论必须和来源事实分栏，不得伪装成原游戏规则。

## Validation gates

- 所有 65 个 roster id 有且仅有一个 dossier 文件。
- retained 的来源功能类型满足门槛；anchor 的数量和来源类型满足锚点门槛。
- 每个 anchor 至少有一套来源可核验的具名版本构筑；宏观经济线不冒充具体构筑。
- evidence 的每个 source id 可解析且不属于 store/product/search。
- claim 类型、置信度、版本范围、限制均非空。
- source、dossier、evidence id 唯一；URL 唯一。
- coverage、source index、roster 与 JSON 统计一致。
