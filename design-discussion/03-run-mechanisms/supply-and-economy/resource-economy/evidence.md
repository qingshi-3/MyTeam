# R09 资源经济：已有证据

讨论证据，不是现行权威。进程见 [总览](../../../roadmap.md)，完整编号比较见 [方案讨论](proposals.md)。未联网补研或改写研究原库。

后续用户已选择 FC01 共用金币、刷新限次及遗物减费方向，见 [R09-D01](decisions.md)。下文保留检索时的候选前提和研究限制，不把原推荐或原作例子作为当前用户选择。

Q02 后续用户已选择 SG01＋SG04＋SG05，见 [R09-D02](decisions.md)：普通留存不生息，特定内容可提供存款收益和复利。以下 Q02 研究时点、来源限制及原方案定义保留，认可内容方向不表示采用原作数值或配方。

## R09-Q01：有限刷新内的支付资源与机会成本

日期：2026-09-08。承接 [R08-D02](../shops-and-rewards/decisions.md)：普通商店连续购物、买后空位、主动刷新且次数有限；特殊资源门槛尚为候选。本轮仅检索刷新支付什么、与什么用途竞争、免费机会怎样改变支付。价格递增的具体曲线、收入／利息、奖励频率、完整生命模型与全部成长经济不在本题一次决定。

只读核对 [构筑权威](../../../../gameplay-design/combat-build-framework.md)与 [核心权威](../../../../gameplay-design/tower-autobattler-core.md)：招募经济仍有待设计，现行装备取得／免费调整规则没有闭合本题刷新支付体系。权威的战术指令金币成本不能当作已定搜索货币；G02 初期无介入的讨论差异仍由原 issue 处理。未读取实现来倒推新规则。

### 实际检索范围

- [深层证据库](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) `.records` 780 条；[发现层](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json) 106 条；[游戏档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/) 56 份；[来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)用于核对来源身份、版本与日期。
- 首筛以 `reroll|re-roll|refresh|刷新|重抽|重掷` 与 `gold|currency|currencies|resource|cost|pay|paid|price|shard|flame|token|charge|free|金币|货币|资源|消耗|费用|付费|支付|代币|次数|免费` 交叉。按 JSON 字段值搜索，深层全字段 90／四核心字段 48，发现层 2／0；档案同行交叉 39 份／104 行。核心字段为 `rule_support / practical_support / mechanism / engine`。
- 委派独立扩查加入 `reload|re.roll|free.roll|reroute|voucher|ticket|coupon|token|免费次数|免费重抽|免费刷新|重置候选|重置选项|重新加载|重新抽取|重摇|代币|券|票`，支付语境补充 `spend|life|health|mana|keen|carat|生命|材料`。该次完整查询深层 124／65、发现层 2／0，档案 45 份／154 行。它与首筛正则不同，数量不直接相加；扩词新增有用相邻材料为 Just King token 换特殊供给访问，其余新增主要是召唤 token、装备 token、武器 reload 等。
- 主责与只读 Agent 回读下列关联记录、档案段落和关键来源元数据，区分同构支付、相邻分账、内容优惠及不完整线索。未逐字重读全部宽命中；上述数量是筛查范围，不是完成率或所有游戏机制已穷尽的证明。

## S01：刷新与购买共用资源

**Guildrun**：`ev-guildrun-002-shard-shop-pity-economy`。Shards 同时用于购物与刷新，刷新起价后递增；玩家花在搜索上的钱不能同时买眼前商品。主要来源 `src-guildrun-wiki-economy-0-5-6`（*Guildrun Economy: Shops, Shards, Rank Ups and Team Size*，2026-08-12，Demo 0.5.6／build ff633149），另有 `src-guildrun-patch-0-5-2`、`src-guildrun-steam-guide-red-rift`、`src-guildrun-review-pivot-friction-2026-07-29`。记录范围 0.5.2–0.5.6，0.5.7 未宣布相关表改变，不由此声称全部版本规则一致。不迁移冻结、卖回比例或遗物出售限制。

**Magicbook AutoBattler: Contract**：`ev-mba-001-shared-offer-panel`；`src-mba-review-system-ownership-2025-03-30`、`src-mba-review-contract-economy-2025-03-29`、`src-mba-review-role-convergence-2025-03-31`、`src-mba-guide-endless-300`。2025 首发及攻略资料：英雄、装备和候选战斗共用面板，Gold 用于刷新与购买。只取同钱包竞争，不因此采用本项目英雄／物品共享刷新，更不引入付钱换敌人。

**Monster Train**：`ev-monster-train-007-upgrade-artifact-economy`；`src-mt-wiki-merchants`、`src-mt-wiki-upgrades`、`src-mt-wiki-artifacts`及记录所列两篇评测。2.x 商人强化、刷新、删牌消耗 Gold。搜索既与新内容竞争，也与已有内容强化竞争；本项目培养已确认材料结构，不能把原作金币强化直接替代 R02。

同构补充命中保留：`ev-tft-001-economy-tempo`（Set 17 指南及未标版本通用经济指南）、`ev-hsbg-001-use-it-or-lose-it-gold`（2019 官方基础规则＋Season 13 实践）、`ev-sap-001-shop-gold-replacement`（2026-06-04 更新指南）、`ev-dota-underlords-002-standard-economy-loop`（late Season One／New Blood 历史 Standard）、`ev-gods-vs-horrors-002-draft-economy-tempo`（1.0 指南＋1.1 观察）。共同点是搜索与其他用途分配同一货币；是否可跨轮保存、有没有利息、购买经验、三合一和 PvP 共池都是独立前提，不混成 FC01 必备规则。

对应 **FC01**。现有资料支持机会成本关系，不证明哪种费用能使本项目成型速度最好；已定次数上限不因原作有可持续付费而取消。

## S02：按操作领域分账

**Slotbound**：`ev-slot-003-three-resource-economy`；`src-slot-review-core-economy-2026-07-26`（*Core, infusion, class-promotion and dual-currency review*，2026-07-26，Demo 0.3.0）、`src-slot-review-cavalry-2026-08-15`、`src-slot-review-todaywegame-2026-07-17`、`src-slot-discussion-shop-rng-2026-08-31`。Demo 0.3.0–0.3.4 范围，部分 0.3.2 实践来自局外树已满个案。

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/slotbound.md)记录 spin 获取单位的 Gold 与购买 item、商店／Core reroll 的另一种货币分开，局外另有资源。搜索物品与扩充单位可以分账，**商店货币仍能买物品，不能称为纯刷新专币**。价格随使用增加、经过后续 wave 回落的实践未有完整官方表；不采用个体报告的具体公式，资源名称和跨店留存细则未闭合。

**ShapeHero Factory**：`ev-shf-008-route-research-shop-rng-economy`；`src-shf-official-shop`（*5 Days until 1.0 Launch! What's Changing? - Shop System*，2025-09-12）、`src-shf-official-1-0-0`（*Update v1.0.0*，2025-09-17）、`src-shf-discussion-starting-research`、`src-shf-review-asc9-rng-205283214`、`src-shf-review-recipe-footprint-204986339`。

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/shapehero-factory.md)第 82–85 行区分 Research reload 使用 Keen，商店购买及后续刷新使用 Carat。**1.0.10 商店刷新细节目前仅见档案，记录 source_ids 未单列该补丁，不能用 1.0.0 公告冒充其直接出处，故不以历史价格作为比较重点。** 官方商店引入说明提到消化闲置 Carat 及给 RNG 缓冲。社区所述开局消耗品允许无限重抽特定 Research 后形成固定 `Underground Utilization + Inserters` 开局，是特定版本玩家实践，不是所有玩家的收敛率。

对应 **FC05**；也是 FC02 的分账动机参照。不同操作各付一种钱，不等于同次刷新同时交两种钱。Research 并非普通物品商店，不能把两个入口直接映射为本项目已定的英雄／装备分币。

## S03：搜索与生命容错竞争

**The Last Flame**：`ev-tlf-001-run-resource-routing`；`src-tlf-gameplay-levels-2025`（*The Last Flame – All Level Tips*，2024-01-21／2025-01-12）、`src-tlf-steam-indepth-2026`（*In-depth Strategy Guide*，2025-02-22；访问时显示 2026-06-22 更新，作者多处仍按 initial 1.0）、`src-tlf-official-1-0`。

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/the-last-flame.md)第 40–42 行：Flame 是 run 生命，同时支付 reroll、campfire、Reborn；Gold 买物品，Trophy 用于英雄成长并参与 Reborn。支付搜索会损失生存余量，也会减少其他机会预算。来源混合 EA 与初始 1.0，不穷举每个 reroll 按钮的当前费用。

反例 **`ev-tlf-014-origin-reroll-underuse-buff`**：`src-tlf-official-1-0-2`（2025-03-16）及上述指南。官方因营地 Origin reroll 用得少，附加回复 8 Flame。证明恢复／纠偏价值可能互相压制；没有前后使用率，不证明普通店刷新因同样原因失败，也不把 8 Flame 当项目参数。

对应 **FC06**。生命作为搜索资源不是独立纯专币，也不自动授权用英雄当前 HP 付费。Reborn 的 Flame／Gold／Trophy 混合支付属于另一项操作，不能作为 FC03 刷新双支付的同构证据。

## S04：免费刷新机会的不同来源

**Hearthstone Battlegrounds**：`ev-hsbg-005-elemental-boost-build`。最直接来源 `src-hsbg-bgbuddy-refreshing-anomaly`（*Refreshing Anomaly Battlegrounds Minion Stats*），Refreshing Anomaly 提供两次免费 Refresh。动态页于 2026-09-02 读取，站点 Patch 250339 未与官方 36.x 建立精确映射；记录中的 `src-hsbg-blizzard-patch-36-2-2` 不自动证明两者同版本。组合站点的胜率不能拿来证明免费刷新效果。

**Setr’s Auto Battler**：`ev-sab-006-opening-protection-tempo`；`src-sab-official-130`（*1.3.0 - Balance changes, music, bugfixes*，2022-01-16，登记为 **1.3.0 beta**）、`src-sab-official-html5-client-130`（公开客户端自报 1.3.0）、`src-sab-review-kona-2021-12-29`。补丁给开局两次免费刷新，并将后续金币刷新从 2 降至 1；这些属于开局保护和收费调整，2021 评测不作为后来补丁的直接证明。

**Backpack Dungeon**：`ev-bpd-008-shop-craft-pivot`、`ev-bpd-015-reroll-item-convergence`；`src-bpd-patch-2-0-2`（*v2.0.2 Quality-of-Life and Fixes Update*，2026-07-17），另有 `src-bpd-patch-2-0-5`与记录所列实践来源。2.0.2 后 Relic Shop 至少一次免费刷新；“至少一次免费”不等于每店总共只准刷新一次。

对应 **FC04**：开局赠送、指定店赠送、具体内容产出有不同触发，可以合并为支付优惠机制，但保留各自供给差异。原作免费机会没有证明可跨店积存的实体券、可交易库存、手动任选金币／券支付，或豁免项目次数上限。

## S05：构筑本身产生搜索机会

**Epic Auto Towers**：`ev-eat-008-pack-reroll-and-first-boss-wall`；`src-eat-thread-difficulty-and-build-variety`、`src-eat-review-reroll-pack-analysis-2026-02-21`、`src-eat-review-early-boss-scaling-2025-11-27`、`src-eat-thread-inferno-builds`。

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/epic-auto-towers.md)第 46 行及关联记录为 0.9x–0.99 社区材料：特定 0.98 长评描述 Royal／Necropolis 可以持续 reroll，Inferno 依赖特定塔提供 reroll，Tropical 刷新可能提前结束但提高稀有度。它说明不同内容会改变搜索来源，尚无官方概率、费用及跨回合积存契约，不能补成完整券经济。

保留在 **FC04** 的内容方向。不能因此新增生产英雄系统，或认为所有战斗单位都应产出搜索资源。控制搜索次数仍须检查此类效果有无架空限制。

## S06：付成长资源取得供给访问，再用钱购买

**Just King**：`ev-jk-010-post-level-three-resource-sinks`；主要来源 `src-jk-thread-level-three-economy`（*Hitting level 3 on heroes early on?*，2025-04-19–22，1.3.x 时期社区说明），另有 `src-jk-guide-food-052`、`src-jk-official-2023-12-21`。

Merchant 先收 token 展示特殊物品，再用 Gold 购买；token 还用于培养、食物和精炼。支付进入供给机会会牺牲已有成长资源，买货还需另一笔钱。混合 0.5.2 Food、1.0 hotfix 与后续社区记录，不统一称为当前 patch 全套规则。

是 **FC03** 两层支出的相邻参照，也保留“非购物资源仍可能有别的高价值用途”这一差异。**没有证明展示动作是整店刷新，更没有证明刷新本身同时扣 token＋Gold。** 本项目培养材料已有独立讨论，不能借此默认合并其用途。

## 证据缺口、排除与方案边界

- FC01 有直接同钱包证据；FC04 有具体免费机会依据；FC05 有分领域资源依据；FC06 有搜索与生命共用资源依据。**FC02 的纯搜索专币／跨店保留／普通必付完整契约、FC03 的每次双支付均是项目方案。** 研究未找到不等于它们不可设计，也不等于其他游戏绝不存在。
- 可积存刷新券、金币或专币任选支付、所有领域共享同一次刷新、免费权自动消耗顺序、跨店恢复／兑换制度均未闭合。不能借免费次数或 R02-PY01 混合培养支付补出这些规则。
- “仅有节点固定免费次数、无其他付费刷新”缺完整直接案例，且不是 R08 已采用普通付资源刷新；不为理论排列扩充本轮默认候选。具体免费节点可留内容，而非重开 SH01／SH02。
- 价格递增、利息、金币清零、出售套利、人口等级／概率、风险奖励与职业材料掉落都可能影响经济，但本轮只取与支付竞争直接相关的部分；其他分别留经济供给、内容或原所属议题。召唤 token、武器 reload、字幕 ticket 等同词命中不构成刷新机制。
- 没有本项目实际成型率、搜索次数分布或使用率数据。推荐只比较机制带来的取舍，不声称体验已经验证。资源数量与上限需后续节奏设计，本轮不运行构建、游戏或自动试玩。

## R09-Q02：留钱是否产生额外经济收益

日期：2026-09-09（本轮跨日整理）。恢复核对 [R09-D01](decisions.md)、[G03-D02](../../../01-global-boundaries/run-rhythm/decisions.md)与 I21／I24／I30／I31 后，先对金币收入作全库筛查，再收窄本题。G03 已允许保留资源用打工单位、等关键节点转型，明确未据此采用利息；B09 经济位已归具体体系。因此不重开“能否存钱”，只比较存款或不购买行为是否生成额外收入，具体来源表及其他报酬规则后续讨论。

### Q02 实际检索范围

仍使用上述 780 条深层、106 条发现记录、56 份档案及来源索引，研究原库只读，未联网补研。JSON 查询匹配字段值，不以字段名如 `recruitment_economy_pivot` 制造命中；四核心字段沿用 `rule_support / practical_support / mechanism / engine`。

- 主责宽筛：`gold|coin|currency|shard|金币|金钱|货币` 与 `income|earn|reward|gain|payout|stash|save|interest|profit|收入|获取|利息|存钱|结算|赚|产金` 同时匹配。深层全字段 **109／核心 46**，发现层 **1／0**；档案同行交叉 **24 份／41 行**。宽命中包含出售、战斗掉落、服务器恢复资源、奖励跳过等，不能全部列为储蓄机制。
- 委派储蓄专题初筛：`interest|saving|\bbank\b|unspent|reserve|本金|利息|存钱|储蓄|清零|expire`。深层 **267／70**，发现层 **2／0**，档案 **26 份／80 行**。因 `interest` 命中 `interesting`、`reserve` 命中 `preserve`，随后改为 `\binterest\b|\bsavings?\b|\bbank\b|\bunspent\b|\breserves?\b|本金|利息|存钱|储蓄|清零|\bexpir(?:e|es|ing)\b`；深层收窄为 **85／31**，发现层 **2／0**，档案仍 **26／80**。仍有后备单位、预留装备等异义，数量不等于直接相关机制数。
- 扩查延迟投资与复利：`deposit|invest(?:ment|ing)?|principal|compound|double|debt|loan|matur|stash|piggy|balance|储蓄|本金|投资|复利|翻倍|借贷|贷款|存款|余额|兑现|到期` 与 `gold|money|cash|currency|essence|shard|income|earn|利息|金币|金钱|收入|收益|货币|资金` 交叉；深层 **84／12**，发现层 **3／1**。不同查询数量不相加。
- 回读下列规则、档案和反例，核对关键来源元数据；没有逐字重读所有宽命中，也不声称穷尽所有游戏。以下将普通基础、内容特例、模式差异、历史案例及项目定义分开。

### Q02-S01：有限预算与无通用利息是不同于不能存钱的设计

`ev-slay-the-spire-003-run-loop-economy` 与 `ev-monster-train-007-upgrade-artifact-economy`保留 Gold 在后续购物、强化或移除之间竞争的预算参照。Slay 的 `src-sts-wiki-gameplay`（2.x，2024-03-16 修订）、`src-sts-wiki-merchant`（2.x，2026-07-29 修订）及关联攻略支持机会成本，不从摘要没有写 interest 推断所有内容都不能生钱。

`ev-dota-underlords-005-knockout-separate-rules` 明确 Knockout 无可购 XP、interest 和 streak Gold；来源包括 `src-du-wiki-knockout`、`src-du-official-jull-tide`等记录关联来源。与 Standard 对照说明普遍利息是可拆卸的模式规则，不是自走棋天然必备。**本项目 SG01“可跨机会留钱、无默认余额／不购买奖励”仍是结合 G03 的项目方案，不将某模式的全部币制移植进来。**

无普通利息不会否定 B09 特定经济内容，也不说明基础收入该是固定、随进度增长或来自哪类奖励；这些不由“没有利息”反推。

### Q02-S02：余额分档、收益封顶的普通利息

**Dungeon 100**：`ev-d100-015-quick-mode-interest-nine-slot-economy`；最直接来源 `src-d100-official-2023-04-25`（*New mode update*，2023-04-25 官方 major update），另有 `src-d100-thread-boss-class-lock`、`src-d100-thread-shaman-pivot`。Quick Mode 每轮固定 5 Gold，另每 10 已存 Gold 得 1 interest、最多 5；明确存在积蓄增加后续收入、达顶后新增本金不再提高该项收益的结构。只属 Quick Mode，不能定义 Classic、项目轮数、卡槽或价格。

**TFT**：`ev-tft-001-economy-tempo`；`src-tft-bunnymuffins-leveling`、`src-tft-bamboo-economy`。Set 17 与未标版本通用攻略强调利息、眼前战力、升级、刷新及血量的竞争，低血或棋盘不足时应提前花钱。不能简化为“任何阵容先存满”；记录未闭合一套精确当前公式。

**Auto Chess**：`ev-auto-chess-004-historical-economy-tempo`；`src-ac-guide-battle-flow`、`src-ac-guide-economy`、`src-ac-guide-transitions`、`src-ac-official-new-player-goals`。2019 独立移动版攻略记录至 50 Gold 的利息档和转型节奏；不能作为 2025／2026 当前参数。连胜／连败另为收入来源，不从本题利息一起采用。

**Underlords Standard**：`ev-dota-underlords-002-standard-economy-loop`；`src-du-wiki-gold`（末修订 2021-03-25，rev19803）、`src-du-wiki-player-level`、`src-du-wiki-shop`、`src-du-official-jull-tide`。后期 Season One／New Blood 档案记录每 10 Gold 给 1 interest、cap 3，**战斗开始锁定利息，之后可花钱**。计息快照影响玩家真正需要保留钱的时段，不能默认按进入店、离开店或整段最低余额结算。将其作为 SG02 适配细节保留，不在本轮另开微观时序选择。

以上直接支持 **SG02**。利息收益上限不是钱包容量，PvP 对手节奏／连败收益／人口经验与共享池不一并迁移；本项目前期容错较大可能强化早期储蓄倾向，是 Agent 兼容性推论，未有本项目统计。

### Q02-S03：按不购买连续记录增加收入

**Setr’s Auto Battler**：`ev-sab-001-shop-economy`；`src-sab-official-html5-client-130`（公开 HTML5 自报 1.3.0，2026-09-03 访问）、`src-sab-official-130`（1.3.0 beta 官方补丁）、`src-sab-review-kona-2021-12-29`（早期实践，不能反证后来客户端公式）。

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/setrs-auto-battler.md)第 35／91 行记录：本轮没买单位，下一轮 interest 增一；购买会将该增长状态清零。客户端收入表达为 `6 + floor(round/3) + interest + win streak + lose streak`，本题只取不购买增长机制，不采用基础曲线和连胜／连败参数。

对应 **SG03**。本金大小不是这项奖励的判断依据，购买小件也会打断连续状态；不是原有金币清零，也不是花钱买 interest。原作买入触发范围、interest／streak 上限、UI 解释及当前服务状态没有完整证据。项目若有混合商品，不能将其默认为所有金币支出都清零；本轮不填此接口。

### Q02-S04：利息能力通过额外成长取得

**Slotbound**：`ev-slot-003-three-resource-economy`；`src-slot-review-core-economy-2026-07-26`、`src-slot-review-cavalry-2026-08-15`、`src-slot-review-todaywegame-2026-07-17`、`src-slot-discussion-shop-rng-2026-08-31`。总体为 Demo 0.3.0–0.3.4；关键 *Meta-economy and all-Cavalry hard-clear review* 为 2026-08-15、Demo 0.3.2、局外树已满并只验证 Stage 1 Hard 的个案。

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/slotbound.md)第 49–51 行记录局外树包括 interest／supply，玩家留 500 得到 50 interest，没有 interest 时使用另一补兵路径。支持经济能力可以由额外成长开放；**单点 500→50 不证明通用比例、最低本金、上限或当前新手体验。**

对应 **SG04** 的相邻参照。把利息能力改由本项目某件局内遗物提供属于项目设计，不冒充原作遗物规则，不借此批准局外必修经济树。不同货币用途的 FC05 未成为当前普通基础，此处也不重开分币。

### Q02-S05：内容驱动的余额倍增及复利失控个案

**Gods vs Horrors**：`ev-gods-vs-horrors-019-infinity-recursion-guards`；`src-gvh-official-update-1-1-2025-06-19`说明 Infinity 引入，`src-gvh-discussion-infinite-essence`（*bug: (almost) infinite essence*，2025-08-20）、`src-gvh-review-infinity-230758740`、`src-gvh-review-infinity-overflow-198199121`为不同日期、不同构筑的实践，不能合为一场。

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/gods-vs-horrors.md)第 130 行记录 Essence Preservation＋多个 Anu 使余额倍增并负数溢出，end-of-recruitment 复制放大收益等观察。原作内容让保留的钱与后续增长互相加强；乘数／重复结算与剩余轮数共同影响结果。

对应 **SG05**，只作特殊内容结构及失败边界。**公式来自玩家推导，官方未确认发生率、内部数值类型、精确当前上限或修复节点**，不能声称 bug 仍存在，也不能由无尽技术溢出证明主流程这种设计必定失衡。普通同名英雄唯一、后续复制例外和非初期重点无尽的项目边界保持，不移植多 Anu 配方。

### Q02-S06：回合金币清零属于不同模式前提

`ev-hsbg-001-use-it-or-lose-it-gold`：`src-hsbg-blizzard-intro-2019`官方基础说明＋`src-hsbg-fantasywarden-season13`实践，酒馆 Gold 无法留到下一轮。`ev-sap-001-shop-gold-replacement`：`src-sap-tag-mechanics-2026`、`src-sap-tag-consistency-2026`，2026-06-04 更新指南，通常每轮 10 Gold，不跨轮携带。

对应 **SG06**。临时预算促使本轮内将钱转为价值，与 G03 已允许的省钱等关键节点路线不兼容，所以仅展示模式对照，不提交重选。不能把原作可持续金币刷新、冻结／合并等一并带入 R09-D01 的限次商店。

### Q02-S07：已归内容的生产经济，以及未闭合的投资机制

- **经济位已有归属，不再作通用制度选择**：`ev-sab-003-alchemist-cycle-funding`，`src-sab-official-120`（*1.2.0 - New units, redirect targets*，2021-12-18，beta）、公开 1.3.0 客户端及 `src-sab-itch-buy-sell-build-2023-07-30`。Alchemist 在 shop end 支付 `2 × Level`，占稀缺队伍槽，收益延后；是内容产钱，不是余额生息。`ev-sab-002-buy-sell-growth-build`还说明反复买卖会牺牲不购买增长，产钱内容可以补其中一部分成本，不证明无限同店套利。
- **经济桥与退出**：`ev-eat-005-economy-bridge-removal`；`src-eat-guide-flower-power-061c`（2024-11-18／20，EA 0.61c）、`src-eat-guide-slime-insolent-bear-061`（2024-11-17／18，EA 0.60–0.61）、`src-eat-review-slime-challenge-2025-12-29`（0.96–0.97 个案）。Chest／Thief 等经济塔有前期收益、后期占空间及移除投入，不是所有阵容默认先铺经济位；Debt 名称不足以证明借贷、利率、锁本金或到期还款。G03-B09 已定位，R03 回退门槛不因这些原作移除方式改变。
- **金币奖励被撤回，但原因未知**：`ev-combat-alchemy-005-accumulation-guard-reworks`，`src-ca-itch-patch-boons-2023-12-02`与 `src-ca-itch-patch-v0-2-2023-12-15`。2023 Web 原型补丁删除 permanent +2 Gold 与 next-round +6 Gold 奖励。它保留跨回合收益可被调整的材料，缺乏完整旧结算、改动动机与效果统计，不能推成“购买固定收入升级”或证明永久收入一定过强。
- 扩查发现层 `e093`／`s061` Thronefall 是投资经济建筑、存活后周期产出；反复投资增加收入不同于未花的钱自行生息，只保留 G03-B09 的相邻线索，不由发现层摘要建立初期系统。
- `ev-gods-vs-horrors-009-marduk-tepeyollotl-build` 的余额转战斗成长、`ev-bpd-003-gold-crit-build` 的金币转爆伤归内容，不产生本题讨论的经济收入；物品名 Piggy Bank、任务 Collect 500 Gold、装备投资、武器 reload 和 save 存档词不能补成储蓄契约。

未找到足够完整的锁本金定期兑付、存够门槛一次奖励、普遍借贷、付钱直接升级 interest、遗物修改普通利息上限条文；不为凑选项将理论金融方式加成本题正式机制。检索清单保留来源去向，研究缺证不等于禁止未来设计。

### Q02 覆盖结论与限制

形成 [SG01–SG06](proposals.md)：SG01 是基于已有留钱前提的项目普通基础，SG02／SG03 有直接规则，SG04 有额外成长开放的相邻依据而遗物载体属迁移，SG05 为特定内容社区结构，SG06 为不兼容模式对照。未将不同游戏的数字或无尽错误拼成统一公式。

目前没有本项目存款、购买行为、成型率或经济内容使用率数据；默认利息与宽容早期的相互作用属于设计取舍，不能声称经过平衡验证。本轮仅研究与记录，R09-D01 不变，没有新增用户决定，未改权威／研究库／实现，也未运行或体验验收。

## R09-Q03 调研快照：不再需要的普通装备如何退出并转成后续资源

日期：2026-09-09。本节保存已执行的检索、来源边界与缺口；最初停在研究快照，随后用户澄清插入的火苗反馈为跨对话误发，要求恢复本题。已据此形成 [EX01–EX06 与 Q03-P01](proposals.md) 的完整比较；用户后续“同意”的确认范围归 [R09-D03](decisions.md)：普通 EX01 卖金币，EX04 保留特殊内容方向。机制证据不因方案确认而改变；记录确认时没有重跑全库或新增外部来源。

拟收窄的问题是：玩家不再需要一件普通装备时，能否将它出售、拆解或交换，以及这种退出所得如何影响后续购买与成长。出售权限发生在任意准备阶段还是特定节点，会改变资源何时可用，属于需核实的同题接口；现有证据尚不足以给出完整的跨游戏权限对照。

前提保持：

- [M05-D01–D03](../../../02-foundation-models/equipment-model/decisions.md) 的重复穿戴按词条控制、装备词条与数值固定但可升级品质不变。材料尽量通用、可与金币区分；高级 Boss 稳定产出特定材料仍为可考虑方向，不能因研究拆解就让它稳定产出该特材，也不能变成不升级便无法继续。具体材料构成、转化和配方尚未确认，原作同名合成与随机重铸不自动采用。
- [M06 遗物退出](../../../02-foundation-models/relic-model/decisions.md) 的常规保留、特殊机会付代价移除或交换，以及 [R03 英雄培养回退](../../roster-and-growth/replacement-recovery/decisions.md) 的门槛／全量回退各有归属，不能由装备卖回推定同样权限或退款。
- [R09-D01／D02](decisions.md) 保持；将装备换成金币不会自动增加每店刷新次数、恢复招募名额或替代培养材料。尚未使用本题材料选择新的普通出售、拆解或兑换规则。

### Q03 已执行的检索口径

研究原库只读，未联网补研；沿用 780 条深记录、106 条发现记录、56 份档案和来源索引。以下为委派装备退出专题的实际口径，与其他词表得到的数量不相加。JSON 比较的是字段值的文本，四核心字段为 `rule_support / practical_support / mechanism / engine`；档案按同一行交叉命中统计。

- 首筛退出词：`sell|sale|salvag|dismantl|recycl|disenchant|trade|scrap|donat|出售|拆解|回收|置换`；装备词：`equip|item|weapon|armor|armour|gear|trinket|artifact|装备|物品|道具|武器|防具|饰品`，均忽略大小写。两个词表同时命中，深记录为全字段 **76／四核心 19**，发现记录 **0／0**，档案 **16 份／38 行**。
- 为避免漏掉消费旧物品的机制，在退出词中扩加 `分解|熔|献祭|捐赠|销毁|卖|reforg|dissolv|smelt|convert|sacrific|consum`，装备词保持。深记录 **217／87**，发现 **0／0**，档案 **28 份／74 行**。扩查增加大量战斗资源消耗、护甲转换及牺牲单位的异义，不能把这些计为装备退出做法。
- 另以 `salvag|dismantl|recycl|disenchant|scrap|donat|拆解|分解|熔炼|捐赠|销毁|回收|置换` 独立检查深记录全字段值，共 **11 条**；包含卡牌回手、原料回收、建议字段和 `Scrappy` 等假命中。出现 `dismantle` 或 `salvage` 不等于已经记录拆装备的输入与产出。
- 回读 Guildrun、Backpack Battles、The Last Flame、Gladiator Guild Manager 相关正文；进一步核对 Backpack Hero、Astronarch、Magicbook AutoBattler 三份档案、相关证据和来源元数据。Backpack Hero 的局外物资转化来自此轮定点回读，不能因首轮关键词未命中而遗漏。

这些数量是筛查过程记录，既不代表逐字重读了所有命中，也不是“退出机制已研究完成”的比例。未重跑或改写前期研究资产。

### Q03 已有直接材料：普通物品卖回与旧装备转新装备

**Guildrun：物品折价卖回，承担过渡战力和小额储备。**

`ev-guildrun-002-shard-shop-pity-economy`，来源 `src-guildrun-wiki-economy-0-5-6`、`src-guildrun-patch-0-5-2`、`src-guildrun-steam-guide-red-rift`、`src-guildrun-review-pivot-friction-2026-07-29`。0.5.2–0.5.6 经济记录、未声明 0.5.7 改表：英雄与普通物品可按 66% 卖回，遗物不能卖。Red Rift 攻略将 common item 用作过渡战力和可部分变现的小银行；昂贵 Auction House 换线又提供退出成本与后续购入成本共同约束方向的实践。

这是装备转通用购物资源的直接参照，**不从 66% 自行补出价格基准、折扣套利、品质升级投入退款或回购制度**。商店上下文也不足以证明只有商人节点允许出售，不能拿它替本项目决定出售窗口。

**Backpack Battles：卖掉过渡物品，换向后续核心。**

[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/backpack-battles.md)的真实循环记录每轮商店购买、出售、刷新和保留；`ev-bpb-007-venomancer-snake-scythe-build` 及 `src-bpb-ign-reaper-2024` 给出以可出售普通武器过渡、之后转向 Death Scythe 等核心的路线。关联的 `src-bpb-dood-classes-2026`、`src-bpb-dood-mechanics-2026` 提供后来说明，不能把 March 2024 Early Access 构筑升级为 1.1.8 当前强度结论。

它支持“当前用过的物品仍可成为后续预算”，没有闭合本题所需的完整卖价、回购、升级投入返还或商店之外的出售权限。有限背包空间与本项目库存前提也不自动相同。

**Magicbook AutoBattler: Contract：消耗旧装备，随机获得另一件高品质装备。**

`ev-mba-003-equipment-merge-reforge-transaction`：三件不同、同品质装备可重铸为一件随机更高品质装备；三件同名同品质合成更高品质同件是另一种规则。主要规则来源 `src-mba-review-system-ownership-2025-03-30`，其他关联为 `src-mba-official-transaction-fixes-2025-03-31`、`src-mba-review-role-convergence-2025-03-31`、`src-mba-official-workshop-leaderboard-2025-06-27`，覆盖 Contract 首发、三月修复及六月更新。

这是“装备换装备”，不同于卖金币或拆升级材料。没有完整产出概率／回收表，不能推成稳定补指定部位、值得反复做的期望收益，或本项目已经采用普通随机重铸；同名合成也不因并列研究而获确认。

### Q03 相邻与特定内容：保留差异，不冒充普通装备制度

**Backpack Hero：地牢物品转换为局外城镇物资。**

`ev-backpack-hero-031-story-quick-game-layer`，`src-bh-official-town-economy`（官方 *Haversack Hill – The Basics #1*，2023-10-25）；[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/backpack-hero.md)的 Story／Quick Game 部分记录地牢物品可卖出或拆成 food／material／treasure，服务城镇研究与建设。

它提供“闲置物品资源化”的相邻结构，但资源归局外层；不能写成原作普通局内装备拆解为强化材料，更不能据此新增本项目局外建设。测试期与之后 Story／Quick Game 的模式差异保持，不把旧城镇条文当作所有当前模式的规则。

**Magicbook：在特定事件中将物品用作交换投入。**

`ev-mba-012-event-special-set-acquisition`；`src-mba-guide-endless-300`（更新至 2025-04-23）、`src-mba-thread-challenge-200`（2025-03-31）、`src-mba-review-role-convergence-2025-03-31`，以及六月更新 `src-mba-official-workshop-leaderboard-2025-06-27`。记录有 `offer two items`／保留两件 exchange inputs，并有 Blacksmith Karl 复制或补齐套装等不同服务。

事件交换不同于随时普通卖回；**现有登记没有完整逐事件表，不能断言“两件任意垃圾装备必换指定特殊件”**，也不能将复制、补齐等服务全部解释为消费旧装备。此处仅保存定向事件内容如何使旧物品具有另一种机会价值。

**Astronarch：任务道具可放弃挑战资格，换取 Gold。**

`ev-astro-012-key-items-primarch-route-contract`，`src-astro-official-1-6`（v1.6.0，2021-06-24）：三把各仅一次机会的钥匙组合后可开启 Primarch 最终 Boss，也可出售换大量 Gold。它是任务物品／挑战机会的经济出口，不是普通装备出售表，也不批准本项目隐藏 Boss 或三钥匙路线。

### Q03 负面与交易边界材料

- **误消耗与操作负担**：Magicbook 的 `ev-mba-003-equipment-merge-reforge-transaction`、`ev-mba-015-one-click-synthesis-ux-rework` 记录评测对反复卸装、合成、换装、重铸的负担，以及未识别套装件被误消耗的担忧。`src-mba-official-transaction-fixes-2025-03-31` 修复重铸拖拽丢装备；`src-mba-official-workshop-leaderboard-2025-06-27` 明确根据反馈加入一键合成。不能从便利功能推定已装备／特殊物品全部自动保护、存在撤销或回购，亦不把一键合成作为本项目新设计。
- **空合法目标导致交易无法退出的历史个例**：`ev-backpack-hero-012-tote-trader-softlock`，`src-bh-guide-tote-softlock`。2024-11-28 UTC Guide 复现 Tote 到 Pocket Trader 时无合法交易目标且无可见取消路径的卡死；同页 2025-03-06 UTC 评论称 Pocket Trader 本身可交易、不再卡死。后者不是官方修复条文，也无精确 build／全平台覆盖。未登记 Pocket Trader 完整目标、结果或回滚契约，因此不能用它充当完整“以旧换新”方案来源。
- **未来兑换定价反而鼓励推迟正常成长**：`ev-astro-015-interstellar-seller-waiting-rework`，`src-astro-official-1-3-5`（v1.3.5，2021-02-24）。Interstellar Seller 改为逐件 item 计价，并允许牺牲 ATK／SPD 或 HP／DEF；官方理由是减少为了等该事件而不升级的动机。**它不是买走旧装备的证据**，只能作为相邻的兑换定价反作用；没有改动前后采用率／胜率统计。

本轮没有找到可闭合为普通装备买卖套利的交易链。价格折损是否足以限制特殊买入／卖出触发，仍取决于具体内容；不得拿英雄买卖成长、未定义的 Rebate Token 或名字含 Trade 的效果补成装备套利事实。

### Q03 缺证、排除与恢复入口

尚未找到足够完整的直接条文来闭合：

- 局内普通装备拆成通用升级材料，以及品质／原投入与材料产出之间的关系；金币与材料供玩家二选一是否为原作常规权限。
- 以一件装备作为直接供体，转入另一件装备成长的完整规则；消耗同名合成或随机重铸不能代替这项证据。
- 普通确定以旧换新的完整投入、目标、费用和窗口；事件有交换服务不等于存在普遍确定兑换。
- 任何准备阶段出售与仅特定商人节点出售的精确权限对照、回购／撤销规则，以及普通装备完全不允许变现的可比完整制度。

已核对但不据此补规则：The Last Flame 的 `ev-tlf-002-item-ownership-and-pivot` 确实存在，关联 `src-tlf-steam-indepth-2026`、`src-tlf-gameplay-starter-2025`、`src-tlf-gameplay-levels-2025`；它只在限制字段提到 dismantle，已读档案未给拆解产出。Gladiator Guild Manager 的 Workshop／Crafting Tools 证明升级投入，不证明材料来自拆装备。ShapeHero Factory 是 Motif 原料回收；Auto Chess 卖英雄返装备是卸装；Setr’s Auto Battler 买卖成长是英雄事件，均不计作普通装备退出规则。

恢复时先读本节、[EX01–EX06 比较及用户回应](proposals.md)、[R09 已定结论](decisions.md)、[M05 已定边界](../../../02-foundation-models/equipment-model/decisions.md)及 [讨论总览](../../../roadmap.md)。普通卖回／随机重铸／特定事件交换有直接机制材料；拆通用升级材料仅有局外相邻参照，直接供体和普通不变现为项目对照，不补成原作事实。同名合成影响重复件价值但仍归 M05 升级配方，本题不自动采用；出售窗口与品质升级投入返还尚未决定，沿 I33 衔接。本节是证据记录，推荐另见 proposals.md；缺证不等于禁止未来设计，未改权威／研究库／实现，未运行或体验验收。

## R09-Q04：普通装备出售需要什么操作窗口

日期：2026-09-09。问题限定为：D03 已采用的普通装备卖金币，是在合法非战斗管理阶段普遍可用，还是需要到商人等指定交易节点。已有免费换装不能推出出售权限；本题不重开出售所得、装备拆解、英雄回退、战中介入或商店出现频率。研究对应 [SW01／SW02](proposals.md)；研究时尚未决定，后续用户“同意”采用 SW01 的范围归 [R09-D04](decisions.md)，记录确认时无新检索或来源。

### Q04 检索范围与方法

只读全部 780 条深记录、106 条发现记录和 56 份深档案，参考来源索引；原有 Q03 检索用于识别出售行为，本次重新按窗口筛查。JSON 用字段值文本，数组值以空格连接，不检索字段名；四核心字段为 `rule_support / practical_support / mechanism / engine`。

- 出售词：`\b(sell|sells|selling|sold|resell|resale|buyback)\b|出售|卖出|卖掉|卖回|售卖|变现|收购|回购`。深记录命中 **63**，发现记录 **1**。
- 窗口词：`shop|merchant|market|vendor|trader|inventory|prepar|phase|between|battle|combat|round|node|room|camp|anywhere|anytime|商店|商人|市场|背包|准备|阶段|战斗|回合|节点|房间|营地|随时`。与出售词同记录交叉命中深记录 **52**、四核心交叉 **16**；发现层 **1／0**。其余 11 条出售候选也列入去向检查，不因缺窗口词直接当作无关。
- 56 份档案按出售词筛查，**27 份／91 行**；进一步用前后 90 字符的出售与窗口词近邻定位具体段落。关键词命中不等于读到了“只能在此出售”的原作规则。
- 扩查加入 `cash\s*out|liquidat\w*` 与 `only|anywhere|anytime|outside|during|between|仅|只能|随时` 等许可词，双方距离至多 100 字符并忽略大小写／跨行，深记录 **20**、发现 **1**。回读新增候选发现多条 `cashout` 指盾／诅咒转伤，不是资产变现；Pochette 的售宠生命周期修复也不证明普通装备出售窗口。不同查询数量不相加，不作为完成率。

主责回读 Vivid Knight 的 Jeweler／路线段、Neon Auto Party 售出与 Offering 的两阶段、Tales & Tactics 卸装道具、Mirror Throne 历史商店买卖，以及近邻扩查的假命中。并行定点复核 Guildrun、Backpack Battles／Hero／Dungeon 和酒馆战棋、Super Auto Pets、Just King、TFT 的档案／关联记录。Balatro 在本次 deep 档案和证据中未找到，不能用记忆补成已有调研。

### Q04 直接装备参照：高频商店中的出售与同次周转

**Backpack Battles**：[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/backpack-battles.md)第 37 行明确：“每轮先在商店购买、出售、刷新和保留物品……战斗自动进行，胜负后进入下一轮商店”。因此它支持每场战斗之间都有的商店整理／出售流程，而不是需要绕路找到稀疏商人。

`ev-bpb-009-lantern-golden-pan-half-start` 的构筑复盘写 `Sell Pig, Gem Box or obsolete transitions when their gold and cells are needed to find or field Magic Orb.` 来源 `src-bpb-top-lantern-pan-2026`，2026-07-14 的视频 ASR 编辑复盘，具体补丁未标。它支持卖掉过渡物，为继续寻找、购入和摆放核心提供资金／格位；历史补充为 `ev-bpb-007-venomancer-snake-scythe-build`／`src-bpb-ign-reaper-2024`（2024-03-20 Early Access）。

**限制**：不能从“商店中卖”推成“商店外禁止卖”，也没有独立的到账事务时点、事件内售卖、商人收购数量／类型限制或回购条文。每战后商店的高频便利不能直接替本项目证明稀疏路线节点独占的体验。

**Guildrun**：`ev-guildrun-002-shard-shop-pity-economy` 在 Regular shop／Auction House 上下文中确认普通物品 66% 卖回；`src-guildrun-wiki-economy-0-5-6` 是 2026-08-12 维护 Wiki，Demo 0.5.6 build `ff633149`，不是官方手册。`ev-guildrun-006-red-rift-route`／`src-guildrun-steam-guide-red-rift`（2026-07-18）记录过渡装备的小银行价值与最后可用商店采购安排。

这些资料支持变现与购物有关，却没有闭合出售入口的排他性。`ev-guildrun-014-mode-objectives-antistall`／`src-guildrun-review-endless-resource-gap-2026-08-07` 的个人观察称 Endless 失去常规商店，**这仍不能推出无法出售装备**；购买供给和持有物出售权限必须分开。

### Q04 相邻参照：每轮准备与有路线成本的商人

**酒馆战棋、Super Auto Pets：每轮返回商店／招募阶段。** 酒馆战棋档案第 44 行记录“酒馆招募—排列七个单位—自动战斗—承伤—回到酒馆”及出售腾位回收金币；基础来源 `src-hsbg-blizzard-intro-2019`（2019-11-01 官方入门），关联 `ev-hsbg-001-use-it-or-lose-it-gold`。SAP 的 `ev-sap-001-shop-gold-replacement` 以 `Each shop phase` 为触发，并记录出售低阶投入换阵容的策略；来源 `src-sap-tag-mechanics-2026`、`src-sap-tag-consistency-2026`，同作者维护至 2026-06-04，不能算两份独立来源。

两者出售的主要是单位，金币也不跨回合，**仅借其高频准备窗口的周转结构**。资料没有闭合任意非战斗界面都能卖，也不能据“自动战斗”补写所有后台交易按钮的权限。本项目不采用倒计时、回合金币清零或为出售而增加每战商店。

**Vivid Knight：买卖发生于 Jeweler，前往与回访受 Mana／路线影响。** `ev-vivid-001-mana-route-jeweler-loop`、`ev-vivid-012-pair-holding-slot-reroll-economy` 及 [档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/vivid-knight.md)的“迷宫、Mana 与 Jeweler”段：移动、回到 Jeweler 买卖、超出储存与探索岔路都会消耗或压迫 Mana；攻略选择先积累多个对子、扩位置，在后段集中卖过渡单位再刷新。regular Jeweler 与按 symbol 筛选的 traveling Jeweler 又影响供给。

来源 `src-vivid-guide-easy-start`（v1.1.10，2021-06-06／06-13）、`src-vivid-guide-maze9`、`src-vivid-guide-witch-maze` 及 `src-vivid-official-1-1-5`，版本横跨 Witch Maze 8／Maze IX、v1.1.10–v1.2.3。它说明交易点的空间／机会成本会改变采购时机，**交易物是单位，不是本题普通装备；资料也不足以证明所有非 Jeweler 出售方式均不存在**。G03 已不采用回访旧商店的集中采购循环，因此本项目只可讨论顺路／分支交易机会，不能照搬原作往返或 Mana 成本。

**Just King 与 Mirror Throne：出售和其他购物集中处理的补充。** `ev-jk-008-hero-upgrade-token-sale` 明确 `Heroes are bought, moved and sold in the shop`，来源 `src-jk-guide-hero-030`、`src-jk-guide-achievements-110` 等，覆盖 0.3.0–1.1.0 的历史社区材料；不能推出装备也能卖或每战必有商店。`ev-mirror-throne-012-demo7-gambler-sale-trapper-run`／`src-mt-video-demo7-gameplay` 为 2024-08-21 Demo 7-era 单局 ASR，记录买入／部署／卖 Bard、再合并／出售等操作；`src-mt-demo5` 还修过 sold-in-shop 触发。只补高频商店周转，不延伸为正式版现行权限。

### Q04 未闭合、排除与项目适配

- Backpack Hero 的 `ev-backpack-hero-001-manual-inventory-loop` 仅把 loot／sale／discard／expansion 并列，未登记普通出售入口；`ev-backpack-hero-009-tote-thin-deck-economy` 是付钱删 Carving，Pocket Trader 是物物交易，城镇出售是局外层，都不能补成装备商人独占。Pochette 的售宠修复也不证明普通装备窗口。
- Backpack Dungeon 的 `ev-bpd-008-shop-craft-pivot` 闭合战后 loot→商店购买／reroll，`sell` 只出现在研究分析字段，不能算作“每战后卖装”的直接条文。Gladiator Guild Manager 的 `ev-ggm-008-item-upgrade-type-synergy-handoff` 同样不能由建议字段的 Sell 推出权限。
- TFT／Auto Chess 卖载体返装备是英雄与卸装规则；Neon Auto Party 的 `ev-neon-auto-party-003-offering-sale-economy` 只证明出售单位得到 proceeds／Offering 后再用 Offering，未说明出售可在哪个窗口操作；Lock 控件位置不证明阻止售卖。Astronarch 卖 Ability Orb／任务钥匙有资源价值，却缺本题装备操作窗口；discovery `e007` 是 Auto Chess 公共池归还分析，四核心未给出售窗口。
- **缺少明确完整的普通装备权限对照**：任意非战斗窗口开放、严格只有商人节点开放、付费事件中临时售卖、远程收购道具、标记后延期结算、限定收购类别／数量／预算。本轮不为增加条目补出这些原作机制，也不从缺证推出项目不能设计。
- 商店频率与出售权限是两个轴：每战必有商店时，商店出售可能已覆盖几乎所有准备；商人稀疏、节点之间有其他金币用途时，限制出售才明显改变筹资和路线。这个条件分析是项目设计推论，未有本项目体验数据。

据此形成两个直接待决的项目规则 **SW01 非战斗管理阶段普遍可卖、SW02 指定交易节点可卖**；把原作高频商店、路线交易的具体差异用于解释，而非伪造更多互斥套餐。D03 已认可 EX04 特殊交换可分别与两者组合，不重选。现有语料不足以关闭的窗口限制明确留缺口；本轮无联网补研、安装、运行或实现改动。

## R09-Q05：已升级装备退出时，过去投入的升级材料如何处理

日期：2026-09-09。D03／D04 已确认普通装备卖金币及合法非战斗管理窗口；本题只比较升级投入怎样退出。恢复核对 [M05-D02／D03](../../../02-foundation-models/equipment-model/decisions.md)、[R03-D01–D03](../../roster-and-growth/replacement-recovery/decisions.md)与 I21／I24／I33。品质配置固定、材料尽量通用、不升级不能成为推进硬门槛均保持；独立材料及高级 Boss 稳定产出特材仍含候选，不写成完整配方。英雄回退已定不等于装备自动采用同一回退方式。

本节保留研究时点与缺证结论。用户后续在核对投入前提后，已明确“不返还材料，直接折算金币”，见 [R09-D05](decisions.md)；具体计价／折损未定，原 IR02 推荐未采用。记录确认时未新增研究或改写原作证据。

### Q05 实际检索与扩查

只读 [780 条深记录](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、106 条发现记录、56 份深档案与 [来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。JSON 检索字段值，数组连接为文本，不匹配字段名；四核心字段为 `rule_support / practical_support / mechanism / engine`。

- 装备词：`equip|item|gear|weapon|armor|armour|trinket|装备|物品|武器|防具|护甲|饰品`；成长词：`upgrad|enhanc|level|rank|tier|star|enchant|refine|cultivat|强化|升级|升阶|品阶|品质|培养`；退出词：`\b(sell|sells|sold|selling|resale|refund\w*|reimburse\w*|reset|respec\w*)\b|recover|reclaim|return|inherit|transfer|salvag|dismantl|出售|卖出|卖回|退款|返还|回收|重置|回退|继承|转移|分解|拆解`。
- 成长＋退出命中深记录 **184**，再交叉装备词为 **103**，三组均落在四核心字段为 **19**；发现层对应 **3／1／0**。56 份档案同行三组交叉为 **16 份／25 行**。同行筛查会漏掉分段规则，随后按游戏与退款／继承行为回读，不把同行未命中当作没有机制。
- 为补“装备”字样之外的相邻退出规则，独立扩查 `\b(refund\w*|reimburse\w*|respec\w*|downgrad\w*|unenchant\w*)\b|退还|返还|退回|退款|重置|回退|降级|降阶|降品|强化转移|升级继承|培养继承`。深记录 **28／核心 11**，发现层 **1／核心 1**；逐项查看命中附近文字，再回读与本题有关的完整字段。包含商业退款、技能资源返还、版本降级和排行重置等假命中，不计作投资退出制度。
- 主责回读 M05 的升级材料证据、GGM／Magicbook／Dwarves／MPIG／Astronarch 等投入与负面材料、Neon 的装备转交及零投入退款报告。并行定点核验 Guildrun、GGM、The Last Flame、Magicbook、Backpack Battles，以及 Setr’s、Skull Horde、Slotbound、Siralim、Vivid Knight、Just King 与 R03 重置参照。Auto Chess／TFT／Mechabellum 的资料缺口一并核对。

不同查询不相加，命中数不是研究完成率。另一次档案退款词宽查输出只查看了部分命中，不宣称其全部正文已读；本节以完成的定点回读支持结论。未联网补充，未因原库没有条文而推定原作不退款。

### Q05-S01：直接装备资料能证明什么，尚不能证明什么

| 游戏与版本 | 已有明确过程／实践 | 本题的缺口与来源 |
| --- | --- | --- |
| Guildrun，Demo 0.5.2–0.5.6；Wiki 2026-08-12 对应 build `ff633149` | 普通物品 66% 卖回；攻略把 common item 当作可部分变现的过渡投入。 | `ev-guildrun-002-shard-shop-pity-economy`／`src-guildrun-wiki-economy-0-5-6`、`src-guildrun-patch-0-5-2`、`src-guildrun-steam-guide-red-rift`。缺少 66% 的计价基准、升级后售价、实际折扣与材料退款；不能将英雄升阶难回收的反馈套给装备。 |
| Gladiator Guild Manager，1.0 与 1.036 历史节点 | Universal Items 改为只出现基础版本，通过 Workshop／Crafting Tools 升级；兼容装备可交给后续英雄继续使用。 | `ev-ggm-008-item-upgrade-type-synergy-handoff`、`ev-ggm-012-trait-and-item-choice-reworks`／`src-ggm-official-1-0`、`src-ggm-official-1-036`、`src-ggm-steam-campaign-v1`。只证明升级投入与整件移交，不证明售出返工具、降级提取或装备间成长继承。 |
| The Last Flame，initial 1.0 自述攻略与 2025 实战 | 保留强底材供后续锻造，根据新英雄职责转移装备。 | `ev-tlf-002-item-ownership-and-pivot`／`src-tlf-steam-indepth-2026`、`src-tlf-gameplay-starter-2025`、`src-tlf-gameplay-levels-2025`。dismantle 只出现在限制字段，未给具体产出；2026 更新日期不证明整篇规则均已重新核对。 |
| Magicbook AutoBattler: Contract，2025-03 至 06 | 三件同名同品质升为同件高品质；不同名同品质随机重铸。五月规则另有橙色材料强化红装；六月改善反复合成操作。 | `ev-mba-003-equipment-merge-reforge-transaction`、`ev-mba-011-endless-growth-cap-rework`、`ev-mba-015-one-click-synthesis-ux-rework`／`src-mba-official-synergy-20-2025-05-18`、`src-mba-official-workshop-leaderboard-2025-06-27`。配方消耗不是退款；“高阶红装给更多材料”的档案表述缺触发与计算，不能补成出售退强化投入。 |
| Backpack Battles，2024 EA 与 2026-07 单局复盘 | 卖过渡武器或过时物品，取得找下一件核心需要的钱与格位。 | `ev-bpb-007-venomancer-snake-scythe-build`、`ev-bpb-009-lantern-golden-pan-half-start`／`src-bpb-ign-reaper-2024`、`src-bpb-top-lantern-pan-2026`。没有历史投入／配方成品售价表；`ev-bpb-002-stamina-cadence-budget` 的 refund 指 Stamina，不能作为材料退款。 |

M05 还保存了 Dwarves 的 Forge 配方材料（`ev-dwarves-glory-death-loot-016-forge-recruit-growth-economy`，v2.0／约 v2.0.10 资料）、Astronarch 用 Gold 升级（`ev-astro-010-route-morale-potion-swap-decisions`）和 Just King 的 Smith 使用 token 精炼（`ev-jk-010-post-level-three-resource-sinks`，2025 社区材料）。它们补足投入形式，不包含对应的历史退款闭环；配方做出新装备、老兵退休和外层成长不能自动解释为装备投资回收。

### Q05-S02：按成品当前状态估价，与历史退款不同

**Setr’s Auto Battler** 的 `ev-sab-001-shop-economy`：公开 HTML5 1.3.0 客户端按 `tier × level + 1` 定买价，售出按该值一半向下取整、最低 1。来源 `src-sab-official-html5-client-130`，2026-09-03 核验；`src-sab-official-130` 为 2022-01-16 官方历史补丁，早期评测只补周转实践。这里读取单位当前 tier／level，不逐笔累加副本、刷新和培养支出。**可类比装备按身份／当前品质估价，不能证明退还历史材料。** R03 旧证据正文曾误写 Super Auto Battle，本轮按库内 `setrs-auto-battler` 使用正确名称，不沿用误称，也未修改 R03。

**Skull Horde** 的 `ev-skull-horde-002-standard-roster-economy`／`src-skull-review-screenhype`（2026-04-10 正式版首发评测）明确替换一条单位类型线会失去累计进度、只得到部分退款。六条指单位类型线，不是装备或全部战斗实体；具体比例与按购入还是现状计价均缺证。它说明折损式退出确实影响转型，不证明“按原材料种类退实际投入”。

### Q05-S03：出售产成长资源，以及保留对象的独立重置

**Just King** 的 [档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/just-king.md)“招募、升级、装备与替换”明确出售英雄返回 token。对应 `ev-jk-008-hero-upgrade-token-sale` 与 `src-jk-guide-hero-030`、`src-jk-guide-achievements-110`、`src-jk-official-2023-06-13`，横跨 0.3.0–1.1.0；0.4.0 又调整 token 来源和成本。它证明售出对象可以产生成长资源，**没有完整数量／类型公式，不能叫按历史实付原样退款，更不能证明装备也采用该规则**。Kādomon 放生单位产 XP（`ev-kado-001-route-recruit-evolve-replace`）同样是退出产物，不自动等同历史投资退款。

**GGM 属性重置**：`ev-ggm-001-timeline-guild-resource-loop`／`src-ggm-official-1-0`、`src-ggm-steam-campaign-v1` 记录建筑开放六项主属性重置，英雄保留。它支持“重置可独立于出售，并由设施开放”，未给每次价格、Trait Blueprint 全部退回或装备退款公式。

**The Last Flame 被动重置**：`ev-tlf-001-run-resource-routing` 及 `src-tlf-steam-indepth-2026` 所属档案记录 Trophy 可用于升级英雄、重置被动及 Reborn。这里支持花资源重配，不能把重配当过去升级费用返还，也不能把 Act 1 Boss 后 Reborn 的一次机会套成每次被动重置的限制。

因此 [R03](../../roster-and-growth/replacement-recovery/decisions.md)按实付种类回收本就是已获用户确认的项目制度，并非原作退款表的照搬。本题若讨论装备退款，仍需明确出售是否失去装备、是否另需回收机会；不能只因用了“回退”两字继承英雄规则。

### Q05-S04：反作用证据与继承边界

- **不愿投入过渡装备**：`ev-mpig-014-enchant-synthesis-commitment`／`src-mpig-update-rewards-duration-2026-08-31`、`src-mpig-review-farm-equipment-233812945`、`src-mpig-review-economy-gear-gap-234241676`。稀有、绑定、毁装风险叠加，玩家报告不愿强化；官方把附魔失败毁装从 100% 降至 50%，合成另有累计十次失败后下一次保底。不能混成同一概率，也没有售装退款证据。它只提醒投资退出代价会影响“现在先用起来”的意愿。
- **未来交换促使玩家推迟升级**：`ev-astro-015-interstellar-seller-waiting-rework`／`src-astro-official-1-3-5`（2021-02-24）。官方明确调整 Interstellar Seller 的计价以减少等事件而不升级的动机；不是买走旧装备或返材料规则，亦无改动前后采用率统计。项目若让某种处置明显惩罚先升级，应检查是否与前期容错冲突。
- **零投入也增加退款的历史个案**：`ev-neon-auto-party-015-skill-tree-refund-exploit`／`src-nap-discussion-playtest-feedback-2024-2025`。一名玩家在 0.5.2 之前反馈，右键显示已投值为零的技能仍增加可用点；开发者仅称调查。`src-nap-playtest-update-1` 与 `src-nap-main-0-5-2` 未复现或确认修复，不宣称现行普遍问题。项目启示仅是区分“取得了成长”与“确实支付了可退资源”，不是拿技能树报告证明装备退款方案。
- **整件物品移交不等于提取成长**：GGM 已述；Neon 的 `ev-neon-auto-party-006-artifact-combine-transfer`／`src-nap-playtest-0-4-2-merging-balance` 明确合并源单位有 Artifact、目标没有时转交 Artifact。目标已有时结果未知，2025 Demo 也未完整演示该链。Auto Brawl Chess 的 `ev-auto-brawl-chess-009-equipment-transition-friction` 记录换队与 Forge 反复卸装的 2023 玩家诉求；两者均非把旧装备的强化提到另一件装备。
- **定向继承属于其他制度**：Slotbound 的 `ev-slot-004-absorb-imprint-ownership`（Demo 0.2.5–0.3.4）消耗单位传成长点与 Imprint，不保证搬走全部历史投入；Siralim 的 `ev-siralim-ultimate-004-fusion-inheritance`（0.12 历史说明与 2.0 维护资料）按双亲字段继承，不是金币／材料退款。Vivid Knight 的 `ev-vivid-003-three-copy-permanent-symbol-unlock` 卖银星单位后仍保留局内符号解锁，是队伍成果归属，不是装备品质或材料返还。这些不另列为普通卖装候选；装备供体方向已有 EX05，暂不作为初期通用。
- **脱装后保留精炼收益也不是退款**：Combat Alchemy 的 `ev-combat-alchemy-002-permanent-refinement-portfolio`／`src-ca-itch-post-8880794`，2023-11-14 历史 Web 原型的社区解释，支持某些装备 1／3 星奖励在未穿戴时仍归角色生效。未说明出售、销毁或消费后是否保留，也未证明跨局永久保留；官方十二月补丁仅证实节点效果调整。它留在 M05 升级收益归属，不新增 Q05 装备退款选项。
- Auto Chess／TFT 已读卖单位返穿戴物只能证明取回装备实例；Mechabellum `ev-mecha-003-upgrade-xp-bounty` 只有升级成本与死亡给对手 XP，不含出售回收表。Tiny Auto Knights `ev-tak-018-removed-item-save-migration` 的退款补偿是研究建议而非已发生原作规则；Monster Train 单位合成、ShapeHero Factory 建筑／Canvas 不继承和 Loot Loop 不可重置亦不补成装备退款机制。

### Q05 覆盖结论

现有材料可支持装备升级与过渡投入、按当前状态估价的相邻制度、售英雄产成长资源、独立重置机会，以及退出代价／退款边界的负面材料。**没有找到与本项目完全同构的已升级装备售出后按历史实付材料返还、历史材料统一折金币、单独退稀有材料或装备降级回收的完整原作条文**；也不能据此认定原作完全不返还。

据此形成 [IR01–IR05](proposals.md)：成品估价、实际材料返还、历史投入折金、受限回收服务，以及按材料区别处理的条件组合。均明确为项目比较，原作证据只支持上文所述部分。掉落成品与自行升级成品是否不同结算、稀有材料是否仅回流实际支出、材料退款与卖价如何避免重复补偿，是本题直接需要展示的差异；比例与价格表不在本轮硬定。D01–D04 保持，无新增用户决定、权威／实现或研究原库修改。

## R09 阶段归属核对：普通收入、表现收益与后续损耗

2026-09-09，D05 已确认后，按恢复流程检查是否还有需要独立表决的基础收入机制。以 `gold|coin|currency|payout|income|reward|loot|bounty|金币|金钱|货币|收入|奖励|掉落|战利品|赏金` 交叉 `streak|victor|win|lose|loss|defeat|perfect|flawless|speed|clear time|surviv|casualt|panic|damage taken|kill|击杀|连胜|连败|胜利|获胜|失败|战败|无伤|速度|用时|存活|阵亡|损伤|伤亡|评分` 及相邻拼写，深记录宽筛 **226／四核心 79**，发现层 **9／核心 0**，56 档案同行交叉 **37 份／110 行**。这只是筛查及指定段落回读，不声称全部宽命中逐篇精读。

回读结果与归属：

- Underlords Standard 的 `ev-dota-underlords-002-standard-economy-loop` 及档案、`src-du-wiki-gold`（Gold 页末修订 2021-03-25）明确胜利额外 1 Gold、连胜最高额外 4、连败最高额外 2，以及败给玩家后的不累积免费刷新。Setr’s `ev-sab-001-shop-economy`／`src-sab-official-html5-client-130` 的 1.3.0 公式也含 win／lose streak；TFT 与 Auto Chess 的历史经济材料支持经营连胜／连败，但未闭合全部当前阶梯。
- 本项目现行普通战败／超时结束整局，G01 未改这一条。因此败后收入与免费刷新不是无需前提的新普通选项；普通连续胜场也通常等于通过的战斗数，属于进程收入曲线，不包装成 PvP 两条经济路线。此判断不是新决定“永远禁止表现奖励”。
- 对同一场胜利，快慢／伤亡改变金币的完整普通公式，在本轮定点回读的 TLF、GGM、Astronarch、Dwarves 中未找到。TLF 死亡损失 Flame 是共享资源损耗，GGM 的复活是恢复成本，战报统计不是奖励公式；这些转 R11。The Last Spell／Crops 在本次指定候选、发现与来源索引按名称查找未找到登记，不用熟悉游戏印象补引用。
- `ev-slot-002-payline-nudge-economy` 的击杀补充转轴 Gold 属于战中操作预算；本项目初期无该输入循环。`ev-d100-015-quick-mode-interest-nine-slot-economy` 的固定轮收入为模式例子，不能直接指定本项目每战金额。经济英雄／遗物、风险节点与来源频率已有各自归属，不因此次宽筛再作整套全局选择。

D01–D05 足以让 R09 的基础讨论阶段收束，未新增 Q06 方案或 D06。基础奖励币种／供给、实际收入与价格曲线、装备升级支付／窗口、拒领结果和具体服务仍未全部完成，分别沿 I21、I24、I30–I33 与对应内容／经济问题衔接；有真实分歧再回看。当前转入 [R11-Q01](../../journey-and-pressure/attrition-and-recovery/proposals.md)，不替并行 R10 选择路线形式，不修改权威、研究原库或实现。
