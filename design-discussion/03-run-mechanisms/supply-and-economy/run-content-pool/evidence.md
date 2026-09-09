# R06 每局内容池：已有证据

日期：2026-09-08。仅使用已有本地调研，不联网补核，不改研究原资产。问题、稳定机制编号 CP01–CP08 及项目判断见 [方案讨论](proposals.md)。本次检索时尚无用户决定；后续确认范围见 [R06-D01](decisions.md)。

## 检索范围与实际覆盖

- [深证据](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) 780 条、[发现层](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json) 106 条、[档案目录](../../../../web/game-mechanics-atlas/research/deep/game-dossiers) 56 份参与筛查；来源链见 [source-index](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。
- 初查 pool／pack／banish／available race-tribe-myth-clan／内容池／卡池／种族轮换／禁用／屏蔽／排除／部族／阵营组合，交叉 run／match／select／random／remove／exclude／unlock／rotate／class／tribe／myth／clan／每局／随机／选择／解锁／移除／筛／羁绊。深层字段值 269 条，四字段 86；发现层 5；档案同行 23 份／38 行。
- 补查 Jail、tribe、mythology、clan、神系／阵营／种族／羁绊与随机／选择／筛／解锁／排除／屏蔽／轮换／开放／可用，以及 eligibility／block 等英文表达。深层字段值仍 269 条（集合不必相同），四字段 76；发现层 9；档案 25 份／59 行。四字段是 rule_support、mechanism、practical_support、engine。
- 重点回读 Gods vs Horrors 神系池、Jail 修正、SAP Pack／Custom、Monster Train 双阵营、Auto Chess 传奇池与 blocking、Auto Brawl Chess Seasonal、Skull Horde 扩池、TLF Soft Ban 和 Vivid 解锁稀释。宽输出的截断不算全文覆盖，关键字段另按 id 回读。命中数不代表有效机制或全文精读数量。
- 发现层 e009 对固定／每周／自定义包提供线索，e083 对主副阵营选择提供线索；与深证据和档案交叉。发现记录的旧 project_note 不作为当前项目范围决定。没有以炉石当前具体部族数等惯常印象补缺。

## 直接回读记录

下列保留原记录的版本、规则、实践、局限与来源；项目推演不因此成为原作事实。

### Gods vs Horrors：`ev-gods-vs-horrors-002-draft-economy-tempo`

- 版本：formal 1.0 Guide and launch practice with 1.1 offer-state observation；原库置信度 high。
- 规则摘要：Divine Essence is allocated among Gods, Pantheon tiers and rerolls; triples upgrade a God and discover from a tier-sensitive reward pool, while only a subset of mythologies is available per run.
- 实践：The Guide recommends immediate tempo, frequent tiering, limited early rerolls and delaying triples until after tiering; reviewers frame the central choice as present board strength versus future economy.
- 限制：Exact costs, pool odds and the Ladder offer-generation algorithm are not documented; a player-observed locked next set is medium-confidence mode evidence.
- 来源：`src-gvh-official-announce-2024-06-05`、`src-gvh-guide-strategy-3493071391`、`src-gvh-discussion-forced-pantheon`、`src-gvh-review-economy-194729657`、`src-gvh-review-busywork-218302641`。

### Dota Underlords：`ev-dota-underlords-007-jail-variance-and-inequity`

- 版本：Big Update Jail through 2019-11 algorithm correction and Jull-tide removal；原库置信度 high。
- 规则摘要：Jail removed heroes every 24 hours; Valve reported Scrappy/Inventor were over-punished while Assassin was largely unaffected and changed bans to 8-12 with tier/alliance limits and one Ace.
- 实践：Players described both forced daily-meta competition and, after removal, lost daily variation; the opinions conflict.
- 限制：Community threads are not a representative sentiment sample, and official notes do not quantify win-rate impact.
- 来源：`src-du-official-big-changelog`、`src-du-official-grow-together`、`src-du-community-jail-meta`、`src-du-community-jull-reaction`。

### Super Auto Pets：`ev-sap-008-custom-pack-super-limit`

- 版本：Official Update 44；原库置信度 high。
- 规则摘要：Standard Custom Packs were limited to five super pets while Wild Custom Packs retained no limit.
- 实践：The official reason is that balancing pets for both standard and custom packs is difficult/time-consuming and unrestricted staples punish players who do not own all packs; the guide confirms custom packs combine owned content across normal pack boundaries.
- 限制：No before/after win-rate evidence is available; the fairness and workload rationale is official intent.
- 来源：`src-sap-official-update44`、`src-sap-tag-consistency-2026`。

### Auto Brawl Chess：`ev-auto-brawl-chess-011-seasonal-six-faction-pool`

- 版本：Fixed 2022 iOS offer complaints and official 2024-08 Seasonal PvP roster response；原库置信度 high。
- 规则摘要：The 2024 official update says too many heroes and overly diverse shops made third-star upgrades harder, then adds a Seasonal PvP mode showing heroes from six Factions that rotate every fourteen days while preserving ordinary Ranked.
- 实践：Earlier guide/rule snapshots establish a broad Faction/Class roster; fixed iOS reviews separately report locked, unowned or low-stat offers and request deck filtering, not roster expansion.
- 限制：The reviews do not establish the official pool-size diagnosis or cause the change, and no source proves the mode solved search friction or changed a measured upgrade rate.
- 来源：`src-abc-patch-seasonal-pvp-2024`、`src-abc-ios-review-feed-page-5-2022`、`src-abc-guide-gamer-empire-2022`、`src-abc-help-factions-classes-2022`。

### Monster Train：`ev-monster-train-022-clan-combination-pivot`

- 版本：2.2 community plus logged runs dated 2021-03-25 to 2021-10-28；原库置信度 medium。
- 规则摘要：A run combines a primary and allied clan while cards, Champion and upgrades retain separate owners.
- 实践：Discussions report many viable combinations but disagree on ease; selected logged TRUE rows show Fire Light, Corruptor and Primordium shells.
- 限制：The catalog has 311 TRUE and 27 FALSE rows but lacks a representative sampling frame, record-selection rule and population denominator, so no tier list or all-combinations-win claim is made.
- 来源：`src-mt-wiki-synergy`、`src-mt-discussion-c25-tld`、`src-mt-discussion-clan-combo`、`src-mt-winning-runs`、`src-mt-review-upgrades`。

### Auto Chess：`ev-auto-chess-023-pool-blocking-and-legendary-discovery`

- 版本：S20 pool split to 2025-05 blocking rewrite；原库置信度 high。
- 规则摘要：S20 separated Extended/Public pools; 2025 removed Extended Pool, added paid one-at-a-time Race/Class blocking, ten per-match Legendary with five hidden, first-discovery Gold and item pass/block rotation; Momora's Nest remains bound by actual pool quantity.
- 实践：Old advice to chase Devastator/Gyrocopter or deny cheap pieces changes when the eligible Legendary set and blocking tools change.
- 限制：Historical guides do not establish 2025 odds, and the patch does not prove the new system achieved more diversity.
- 来源：`src-ac-official-s20`、`src-ac-official-2025-05-29`、`src-ac-guide-economy`、`src-ac-guide-steam-2023`。

### Skull Horde：`ev-skull-horde-015-oath-pool-banish-economy`

- 版本：formal release through v1.015+ economy/readability period；原库置信度 high。
- 规则摘要：Top-tier unit progress feeds Oaths/tags; rarity expansion adds specialized units to the offer pool, while The Many cannot use upgrades/Oaths and Garg uses a distinct Champion rule.
- 实践：Players identify pool dilution and reroll cost as the price of opening higher-rarity builds; Banish trims unusable percentage/stat options or unwanted branches.
- 限制：No current odds table, complete Banish pricing function or universal best rarity timing is public.
- 来源：`src-skull-discussion-rarity-pool`、`src-skull-review-screenhype`、`src-skull-official-v1-015`、`src-skull-discussion-many-engine`。

### Gods vs Horrors：`ev-gods-vs-horrors-013-relic-blessing-rule-rewrite`

- 版本：Next Fest Blessing introduction through 1.0 compatibility fixes and 1.1 Relic/Blessing rebalance；原库置信度 high。
- 规则摘要：Blessings add tiered run modifiers; Relics can alter mythology/pool/economy or grant standalone rules, and official updates remove incompatible offers, add eight Relics and rebalance multiple tiers/effects.
- 实践：Players distinguish pool-correction, event-doubling and economy Relics from weak flat bonuses, and report buy/sell busywork when a rule rewards every recruitment action.
- 限制：No complete current Relic/Blessing table or offer odds exist; forum rankings do not establish universal value.
- 来源：`src-gvh-official-demo-next-fest-2025-02-24`、`src-gvh-official-qol-2025-05-19`、`src-gvh-official-update-1-1-2025-06-19`、`src-gvh-discussion-balance`、`src-gvh-review-busywork-218302641`。

### The Last Flame：`ev-tlf-012-content-dilution-soft-ban`

- 版本：Official 1.0 Early Access retrospective and contemporary guides；原库置信度 high。
- 规则摘要：By 1.0 the game had 65 heroes, 325 items, 150 relics and 60 Origins, while recipe locking, Soft Ban and staged unlocks changed what players were likely to see.
- 实践：The developer explicitly reports negative feedback about item dilution; independent guides describe daunting option density, no trait/set-bonus starting reference and the need to preserve AD/SP coverage to avoid excessive rerolls.
- 限制：The developer also attributes some complaints to underusing cheap rerolls; the sources do not quantify how much each cause contributed.
- 来源：`src-tlf-official-1-0`、`src-tlf-gameplay-starter-2025`、`src-tlf-steam-indepth-2026`。

### Vivid Knight：`ev-vivid-020-unlock-pool-dilution-community-failure`

- 版本：v1.1.1-v1.1.5 launch guide；原库置信度 low。
- 规则摘要：Meta progression unlocks more Gems/units while random rewards and shops draw from available content; official patches adjusted rarity and reward access but do not confirm a dilution fix.
- 实践：One detailed Boss guide recommends not unlocking mutually exclusive Burn/Shock, heal and attack Gem families because unwanted unlocks obstruct collecting three copies of the planned tools; another guide values broad unit exposure, showing the tension is pool-specific rather than universally agreed.
- 限制：This is one strongly opinionated guide that also uses reload manipulation; no drop table or official acknowledgement establishes population-wide behavior.
- 来源：`src-vivid-guide-boss`、`src-vivid-guide-easy-start`、`src-vivid-guide-symbol-tips`、`src-vivid-official-1-1-11`。

## 机制映射与证据缺口

- CP01：Gods vs Horrors 档案“真实循环”记录随机五 mythology＋Neutral；Ladder 下一组与结果推进、Casual Star Chart 调整的权限分开。具体池算法、每组资格表不完整，不能抄成我们固定五组或所有标签互斥。
- CP02：Jail 是历史每日禁用，项目逐局版本属于迁移；官方 tier／alliance 约束修正与用户意见分歧同时保留。不能声称受约束抽样已经解决所有组合失衡。
- CP03：SAP 固定／Weekly 池结构与 Auto Brawl Chess 十四天六阵营是原作；每局从人工校核池抽一套是项目方案。周／赛季／版本轮换不自动等于逐局变化。
- CP04／CP05：主副阵营或自定义池会预先收窄构筑；与 G03 当前常规开局方向冲突，仍展示不同机制，不借比较请求重新批准。原作主英雄与商业拥有权不迁移。
- CP06：Auto Chess 随机 Legendary 的局级集合、五名隐藏与发现奖励是分开的机制。隐藏不等于未解锁，也不等于全局没有资格；没有拿这条资料证明全部普通池固定。
- CP07：Skull Horde 的稀有度增加候选有直接记录；SAP 档案“真实循环”记录 tier 分阶段加入。阶段资格与整局筛选分开，既定 R05 无人口升级保持。
- CP08：付费 Race／Class blocking 与 Relic 改神系／池是局部证据；不推定全部遗物的生效时点、删除已持有英雄或完整原作禁用规则。G03 的遗物屏蔽候选保持原采用程度。
- TLF 官方承认内容稀释并采用 Soft Ban、配方锁定与分层解锁；无权重细节和成因占比。Vivid 有攻略建议避免解锁不想要的 Gem 家族，但另一攻略重视广泛单位曝光，不能写成“解锁越多必然越差”的共识。这些支持池规模与推荐需衔接，不提供本项目最优算法。

## 排除与后续归属

- 共享有限牌池耗尽、出售归池、单次候选去重、刷新保留等归 R07，不与英雄本局资格混成一项；Storybook Brawl 有个人有效无限池历史，但改变库存竞争不等于每局筛选内容。
- 战斗内抽牌／消耗区、法力 pool、召唤生成池、敌人每日包与对手匹配不作为本题英雄集合案例。
- 局外解锁扩大总内容库归 R14，可能影响稀释但不是每局筛选方法。TFT／炉石赛季轮换有版本证据，本库相关部分不足以单独证明某种当前逐局英雄筛选算法。
- 单次招募倾向契合阵容同时保留其他方向已在 G03 确认，不重新表决。具体可达性校核、多标签英雄、跨羁绊功能连接，以及装备／遗物只适用于已排除英雄时怎样供给留后续必要子问题。
- 只有英雄池进入本轮主要比较；不能从某神系缺席推定所有同主题装备、通用状态或遗物也删除。没有已实现筛选、概率模拟、体验验证或普遍成型率证据。
