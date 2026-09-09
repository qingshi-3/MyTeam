# R05 人口成长：已有证据

日期：2026-09-08。仅检索已有本地研究，不联网补核、不修改研究原资产。当前问题为普通上场人口增长的来源与代价；编号 PG01–PG07 的过程、差异和迁移限制见 [方案讨论](proposals.md)。

## 实际范围

- [深证据](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) 780 条、[发现层](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json) 106 条与 [56 份档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers)参与检索。来源 id 由 [source-index](../../../../web/game-mechanics-atlas/research/deep/source-index.md)追溯。
- 初筛人口同义词：population、team size、army size、party slot、active slot、deployment cap／size、unit cap、人口、上场人数、扩编／扩军、队伍容量、出战人数。深层全部字段值 92 条、四字段 14 条；发现层 10 条；档案 32 份／98 行命中。
- 扩查 population／army／team／party／deploy／人口／队伍／上场／扩编 与 level／experience／XP／cap／slot／size／expand／growth／等级／经验／上限／格位／扩／规模 的字段值交叉：深层 410 条、四字段 100 条；发现层 20 条。四字段为 rule_support、mechanism、practical_support、engine。宽命中含大量战斗技能等级、研究项目建议和召唤上限，不代表都相关或已全文读完。
- 定点回读 TFT 经济／Fast 8、Underlords Standard／Knockout、Astronarch 章节加人、T&T Army Size／Level Perk／人口压缩、Vivid 买位、Slotbound 局外及扩编样本的记录或档案段落。几次宽输出截断，不计为完整阅读；关键事实随后按 id／字段回读，引用仅取已读片段。
- 发现层 e019 提供 T&T 人口独立成长线索，e076 提供 Dwarves 从两人至十人的收集扩编线索，均优先以上层深记录和版本限制约束；不据发现摘要推定扩编价格。其他 e021／e074 解锁招募层级不证明增加出战人口；e040 放置经验解锁也不足以证明具体人口规则。

## 直接证据与来源链

下列原文摘要保存事实、玩家实践及限制；原库的置信度是该记录范围内评估，不等于原作全部规则完整或当前版本已核验。

### Teamfight Tactics：`ev-tft-001-economy-tempo`

- 版本：Set 17 guide plus version-unspecified general economy guide, accessed 2026-09-02；原库置信度 high。
- 规则：Gold can be allocated between interest, leveling and rerolling; shop odds and unit pools vary with level.
- 实践：Both guides distinguish win/loss-streak, reroll and Fast 8/9 lines and recommend spending early when health or board strength requires stabilization.
- 限制：Exact thresholds and timings are set- and lobby-dependent; this record does not preserve a numeric leveling recipe.
- 来源：`src-tft-bunnymuffins-leveling`、`src-tft-bamboo-economy`。

### Teamfight Tactics：`ev-tft-006-fast-eight-archetype`

- 版本：Set 17 leveling guide cross-checked with general economy, item and comp guides；原库置信度 high。
- 规则：Higher levels improve access to expensive units and increase board capacity, at the opportunity cost of reroll gold.
- 实践：Fast 8 uses economy and a strong transitional board to reach high-cost carries, with flexible items and holders preserving health before the roll-down.
- 限制：Timing and exact carries change by set; the guides do not guarantee success when a roll-down misses.
- 来源：`src-tft-bunnymuffins-leveling`、`src-tft-bamboo-economy`、`src-tft-bamboo-items`、`src-tft-bamboo-comp-selection`。

### Dota Underlords：`ev-dota-underlords-002-standard-economy-loop`

- 版本：late Season One / New Blood Standard archive；原库置信度 high。
- 规则：Gold funds heroes, 2-Gold rerolls and XP; interest locks at combat start, levels determine board cap and tier odds, and manual rerolls blacklist unbought offers while automatic refreshes do not.
- 实践：Reroll builds, fast-level builds, temporary holds and interest preservation compete for one budget and bench.
- 限制：Jull-tide's historical 7-Gold round rule and the archive's later 5-Gold base rule are not blended; exact figures are era-scoped.
- 来源：`src-du-wiki-gold`、`src-du-wiki-player-level`、`src-du-wiki-shop`、`src-du-official-jull-tide`。

### Dota Underlords：`ev-dota-underlords-005-knockout-separate-rules`

- 版本：Season One Knockout；原库置信度 high。
- 规则：Knockout starts from one of three five-hero teams, uses four copies for a three-star, four Health with fixed one-loss damage, automatic levels, no purchasable XP/interest/streak Gold, odd-round items and an early Underlord pick.
- 实践：Its accelerated upgrades and fixed-loss clock reward different risk, roll and item timing than Standard.
- 限制：Knockout shop odds and growth multipliers are mode-specific; no Standard build ranking is inferred.
- 来源：`src-du-wiki-knockout`、`src-du-official-jull-tide`、`src-du-wiki-items`、`src-du-wiki-underlords`。

### Astronarch：`ev-astro-001-auto-combat-recruitment-boundary`

- 版本：1.2.x early full release; old Corruption 20 separated；原库置信度 medium。
- 规则：A normal early-full-release run selects three heroes, adds one after each act boss to reach five, configures the party before combat and then resolves combat automatically; old Corruption 20 instead began at two and ended at four.
- 实践：Guides assign the compressed slots to role-combining heroes such as Druid tank/healer, then add a main tank, off-tank or payoff only when the next act makes that role affordable.
- 限制：These are guide-documented 2021 rules, not a current official manual; v1.5 moved the old C20 effect into an Omen, so the 2-to-4 line is historical.
- 来源：`src-astro-guide-beginners`、`src-astro-guide-easy-c20`、`src-astro-guide-elements`、`src-astro-official-1-5`。

### Tales & Tactics：`ev-tnt-001-army-growth-and-level-perks`

- 版本：Road-to-1.0 official perk design plus 2.0 community rules answer；原库置信度 medium。
- 规则：The 2.0 answer states that each major boss grants an Army Size increase and two pre-tournament bosses lead to six normal slots; XP level-ups grant perk choices. The official pre-1.0 patch separately documents Army Size perks and their tradeoffs.
- 实践：Army growth arrives at authored milestones while perks can alter it earlier or beyond baseline, so players price immediate composition against future capacity.
- 限制：The exact current campaign tutorial is not publicly readable; the six-slot and boss-growth wording is a community answer and is version-scoped to 2.0.
- 来源：`src-tnt-official-levelup-2024`、`src-tnt-steam-army-size-2026`。

### Vivid Knight：`ev-vivid-001-mana-route-jeweler-loop`

- 版本：v1.1.10-v1.2.3 Steam；原库置信度 medium。
- 规则：Dungeon movement consumes Mana while fights provide Keene and free units; Jewelers buy, sell and reroll units, party/storage slots expand for Keene, and Witch's Maze IX adds harder routes and Magic Hammer rewards.
- 实践：The Maze IX route clears most profitable fights, avoids impossible Monster Spots, carries Mana recovery, delays ordinary rerolls and returns to shops only when enough pairs and Keene make a broad roll-down efficient.
- 限制：Exact movement/over-capacity Mana formulas and reroll costs are guide-period rules, not a current official table.
- 来源：`src-vivid-guide-easy-start`、`src-vivid-guide-maze9`、`src-vivid-official-1-1-5`、`src-vivid-official-1-1-34`。

### Vivid Knight：`ev-vivid-012-pair-holding-slot-reroll-economy`

- 版本：Witch Maze 8 to Maze IX v1.2.3；原库置信度 medium。
- 规则：Party/storage slots, paid rerolls, equal unit prices and three-copy upgrades create a finite holding problem; the official patch lowered Jeweler refresh cost in early mazes.
- 实践：Guides buy character slots before rerolling, hold many pairs, stop most bridges at silver, sell obsolete upgrades before a large roll-down and use traveling symbol filters only when they match several live outs.
- 限制：Keene values and optimal floor timing are author routes, not invariant rules; one guide is Witch Maze 8 and another Maze IX.
- 来源：`src-vivid-guide-maze9`、`src-vivid-guide-witch-maze`、`src-vivid-guide-easy-start`、`src-vivid-official-1-1-5`。

### Auto Brawl Chess：`ev-auto-brawl-chess-001-shop-scout-merge-loop`

- 版本：2020–2024 historical mobile/Steam observations; exact current economy values excluded；原库置信度 high。
- 规则：The historical Gameplay page separates preparation and battle and exposes shop purchase, refresh, lock, population expansion, merging and opponent scouting.
- 实践：Early strategy, a 2024 beginner session and a timestamped six-match log show players choosing between low population, saving, buying, expanding, merging, positioning and observing opponents.
- 限制：Maximum star, interest, pool, price and population formulas differ or remain unavailable across periods; no value is currentized.
- 来源：`src-abc-help-gameplay-2022`、`src-abc-guide-pocket-gamer-strategy-2020`、`src-abc-video-beginner-2024`、`src-abc-video-ranked-six-match-log-2022`。

### Slotbound：`ev-slot-013-early-rng-meta-pressure`

- 版本：Demo 0.2.0 through 0.3.4 player reports；原库置信度 high。
- 规则：Runs allow repeated spins, limited nudge/Core correction, two losses and permanent upgrades that improve hearts, army size, rerolls and rarity access.
- 实践：Multiple players report early waves with no units and repeated restarts or grinding easy mode, while a post-overhaul player says economy/Core planning usually salvages bad starts.
- 限制：Sentiment and frequency conflict; no seed-level failure rate or before/after statistics establish how often control succeeds.
- 来源：`src-slot-review-core-critique-2026-07-14`、`src-slot-review-mage-cavalry-2026-07-17`、`src-slot-review-deadeye-2026-07-16`、`src-slot-review-core-economy-2026-07-26`、`src-slot-discussion-shop-rng-2026-08-31`。

### Dwarves: Glory, Death and Loot：`ev-dwarves-glory-death-loot-002-roster-equipment-loop`

- 版本：formal v2.0 loop cross-checked by maintained pages and 2026 reviews；原库置信度 high。
- 规则：The game manages an expanding roster of up to ten independently equipped dwarves whose professions, equipment, Runes, Artifacts and Formation shape automatic combat.
- 实践：Independent reviews describe repeated roster/equipment decisions and preparation as the primary agency rather than direct attack control.
- 限制：Current offer odds, full prices, target logic and exact expansion schedule and scaling are not reproduced here.
- 来源：`src-dgdl-official-full-release-2-0`、`src-dgdl-companion-classes`、`src-dgdl-companion-tavern`、`src-dgdl-review-savior-2026-02-03`、`src-dgdl-review-xpn-2026-04-10`。

## 各机制的证据边界

- PG01：Astronarch 1.2.x／旧版攻略普通三至五人；旧 Corruption 20 两至四人不混入。T&T 2.0 六人及 Boss 加人来自社区答复，官方旧公告直接支持的是 Level Perk 人口内容；不能把两个时间点视为一份当前教程。新位置与招募同时发放还是分开发放，在项目仍未定。
- PG02：Underlords 历史 Knockout 自动等级、无购 XP 有直接依据；项目按战斗 XP、路线改变速度是设计变体，无精确分配公式。
- PG03：TFT 与 Underlords Standard 支持买等级、容量及高费可达性联动；Auto Brawl Chess 历史资料补充购买／扩编／刷新竞争同一预算。精确自然经验、收费、等级概率和当前数值未重新核验，不照搬固定升级口诀。
- PG04：Vivid 买 party slot 直接增加身体，攻略有先买位与后期昂贵格位的取舍；项目人口与招募概率解耦是设计主张，不由原作摘要反证其所有隐藏概率规则。后备扩容已属 R04，不将两种槽位混同。
- PG05／PG06：T&T 官方 2024-07-31 Level Up 公告支持 For The People! 人口与敌方 Damage Amp／Reduction 代价；档案中 Mountain Dwarf 历史少一 Army Size 换伤害／抗性、Set 2 Titan 减人口的单核案例，是反向压缩的内容参考。具体版本、单人成功或读档实践不证明普遍平衡性。细节及来源见 [T&T 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/tales-and-tactics.md)；人口奖励放在项目可预告支路是推演。
- PG07：Slotbound 永久军队规模升级与早期负面反馈来自多个 Demo 观察；存在观点分歧、没有种子失败率，不把“人口永久升级”写成早期失败的单一已证实原因。

## 排除与未完成部分

- Guildrun 三至五人上场只证明容量改变，已读材料未闭合增长触发，不能猜成某 Boss／等级奖励。
- Dwarves 深记录确认扩编至十名，未给精确扩编日程与收费；因此保留为“扩编后每人装备投入”的参照，不据此新增独立“无人口门槛、买人即扩编”机制。
- TFT 历史双人口 Dragon、Slotbound 社区不计入显示 cap 的单位特例，涉及每英雄人口消耗例外；本项目已固定每名英雄 1 人口，本题不重开。固定小队整局不扩编与普通进程增长前提冲突，不重新作为主选项。
- 临时召唤的数量与性能上限、物品容量、备战席扩容、羁绊计数、永久角色图鉴不等于普通上场人口成长；未在此展开其战斗效果。人口特殊效果属于内容，不作全体玩家通用模式单选。
- 本轮没有精确开局数量、每级经验表、价格、供给频率或体验结论。现行初始 7 人口／4 名册只用于识别兼容基线；R04 暂参考云顶不授权自动采用 PG03。当前无新玩法决定。
