# R07 招募机制：已有证据

日期：2026-09-08。只检索已有本地研究，无新增联网研究，不修改原资产。当前问题是同名英雄取得／持有／上场及直接相连的普通羁绊贡献，编号 DR01–DR06 见 [方案](proposals.md)。本次检索时尚无 R07 用户决定；后续结论见 [R07-D01](decisions.md)。

## 实际检索范围

- 委派只读研究覆盖 [深证据](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) 780 条、[发现层](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json) 106 条及 [游戏档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers) 56 份；主责独立核对项目已定边界、现行权威和相关代码片段。来源链见 [source-index](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。
- 复制词正则：`duplicat|same.name|same.type|copies|\bcopy\b|already.owned|clone|unique|同名|重复|复制|唯一`；单位语境：`hero|unit|character|pet|roster|recruit|field|队|英雄|单位|招募|出战|上场`，均忽略大小写。
- 全部字段值同时匹配：深度 229 条、发现 12 条。复制词限定 rule_support／practical_support／mechanism／engine 四字段、单位语境仍在全记录匹配：深度 77、发现 3。档案同行同时命中 48 份／120 行。
- 已读直接相关记录、档案段落与来源索引元数据；不是 229 条直接证据或 56 份全文精读。大量噪声来自装备／特效复制、临时召唤、unique 泛义及旧项目建议。补查已有持有／唯一名册与复制对羁绊的关系，缺口如下。
- 未找到本库足够明确的 TFT／Auto Chess 普通羁绊同名去重条文；不能以常识补成已核实事实。未找到可靠的熟悉英雄自走棋“完整名册只许每英雄一份”规则，不将 Ancient 装备去重误作 Astronarch 英雄唯一。

## 直接记录

原库版本、事实／实践与限制保留如下；原作的复制、职业模板、普通招募和升级不能互换概念。

### Just King：`ev-jk-006-archer-berserker-kiting-team`

- 版本：2022-08 Early Access community build；原库置信度：medium。
- 规则摘要：Archer historically fires three arrows, Hot Hands supplies Haste, Frenzy supplies percent damage and movement speed, and Rusher adds party movement speed.
- 玩家实践：A named two-Archer/two-Berserker line upgrades one Archer, gives it flat-damage or flame-AOE equipment and relies on movement-speed kiting rather than a heavy tank.
- 局限：The author calls kiting easy, but no difficulty, exact equipment roll or win-rate sample is available and later patches changed items.
- 来源：`src-jk-thread-op-builds`、`src-jk-guide-hero-030`、`src-jk-guide-pacifist`。

### Just King：`ev-jk-012-pacifist-environment-owner`

- 版本：2022-10 Early Access achievement route；原库置信度：medium。
- 规则摘要：Deep hooks or Yokai gates can damage enemies independently of hero, item and King damage; Cultist tentacles require a commanded target to attack.
- 玩家实践：The route buys a matching hero to activate the trait, then keeps heroes from dealing damage while continuously luring enemies through environmental objects.
- 局限：It is a narrow achievement route and does not establish ordinary build strength; later comments mention additional methods without full rules.
- 来源：`src-jk-guide-pacifist`、`src-jk-guide-hero-030`、`src-jk-guide-achievements-110`。

### Gladiator Guild Manager：`ev-ggm-004-shaman-cluster-to-hypercarry-pivot`

- 版本：1.0 speedrun route explicitly marked nerfed after launch, with EA precursor；原库置信度：medium。
- 规则摘要：Extrovert and same-type scaling reward clustering/summons, while v1.034 caps Extrovert and raises Shaman totem Stamina after its behavior changed to remain back and summon.
- 玩家实践：The route starts Swift Rhino plus two to four Extrovert Shamans, adds Banshee, then converts item/shop signals into one or two Pyromancer/Archer hypercarries instead of scaling Shamans forever.
- 局限：The author calls the route nerfed since 1.0 and leaves it as historical cheese; EA Shaman behavior was different.
- 来源：`src-ggm-steam-campaign-v1`、`src-ggm-steam-tournament-guide`、`src-ggm-official-1-034`。

### Auto Chess：`ev-auto-chess-018-insectoid-duplicate-death-summon`

- 版本：2020 Insectoid rule with later copy/revive context；原库置信度：high。
- 规则摘要：Insectoid watches duplicate non-Insectoid allies; when one dies, it reads the highest cost among surviving duplicates and summons a random insect based on that rule.
- 玩家实践：Duplicate bodies are intentional fuel, but the trigger requires a dead member and a surviving same-name reference rather than any allied death.
- 局限：Civet battle-start copies and Cave graveyard returns are separate mechanisms and do not inherit Insectoid rules.
- 来源：`src-ac-official-insectoid`、`src-ac-official-civet-s13`、`src-ac-official-cave-prodigy`。

### Auto Chess：`ev-auto-chess-021-civet-copy-and-shield-break`

- 版本：S13 2021；原库置信度：high。
- 规则摘要：S13 Civet enables three-star copying and battle-start same-name pairs to create Golems; the same patch gives Evil Knight an AOE when its shield breaks and Scryer an enemy-synergy counter read.
- 玩家实践：Copy, summon, break payoff and reactive counter are four separate owners even when used in one board.
- 局限：Exact Golem inheritance and pool accounting beyond the patch text are not generalized; later versions may differ.
- 来源：`src-ac-official-civet-s13`、`src-ac-official-insectoid`、`src-ac-official-cave-prodigy`。

### Tales & Tactics：`ev-tnt-004-four-dragon-four-noble-party`

- 版本：Early-1.0 Ladder 15 route cross-referenced to 1.4.3 roster and trait rules；原库置信度：high。
- 规则摘要：The 1.4.3 guide identifies Lime and Micky as the two 1-cost Dragons and Brim and Kio as the two 3-cost Dragons; four Dragons use first-cast fire to tier up allies that contact it.
- 玩家实践：The Ladder 15 guide preserves Dragon during random banishment, takes both 1-costs, uses two unrestricted Clovers for both 3-costs, equips Brim with two Attack Speed and one recovery item, tanks Micky/Kio and adds Noble plus a banner.
- 局限：The build is historical and the Banner holder is not named. Mapping the guide's Chinese 'hammer Dragon' to Brim is roster-based and explicitly labelled.
- 来源：`src-tnt-steam-ladder15-2024`、`src-tnt-steam-traits-1-4-3`、`src-tnt-official-levelup-2024`。

### Setr's Auto Battler：`ev-sab-005-upgrade-slot-rework`

- 版本：1.2.0 launch feedback through 1.3.0 rework；原库置信度：high。
- 规则摘要：Version 1.3 reduced same-unit upgrade requirements from three copies to two; the client allows a full team to buy only when the purchase immediately creates a legal merge.
- 玩家实践：Early review and discussion report that three-copy upgrading occupied two or three of five positions, restricted unit variety and made buy-sell supports harder to operate; later feedback says the 1.3 change removed that exact pressure.
- 局限：The reports do not establish whether two copies became optimally tuned or whether reserve space would have solved other constraints.
- 来源：`src-sab-review-kona-2021-12-29`、`src-sab-steam-upgrade-feedback-2021-12`、`src-sab-official-130`、`src-sab-official-html5-client-130`。

### Hearthstone Battlegrounds：`ev-hsbg-002-triple-discover-timing`

- 版本：2019 triple rule and 2022 Buddy-era leveling guide；原库置信度：medium。
- 规则摘要：Three identical minions merge into a Golden minion retaining prior buffs and grant a discover from the next Tavern Tier when played.
- 玩家实践：The curve guide times upgrades and early triples to access specific higher-tier snowball units while balancing board width and tempo.
- 局限：The curve examples are Buddy-era history and not current 2026 leveling recommendations.
- 来源：`src-hsbg-blizzard-intro-2019`、`src-hsbg-hstd-curves-2022`。

### Super Auto Pets：`ev-sap-002-merge-food-ownership`

- 版本：Mechanics and strategy guides updated 2026-06-04；原库置信度：medium。
- 规则摘要：Merging identical pets combines experience and stats; the receiving pet keeps its Food while Food on the dragged pet is lost.
- 玩家实践：The food guide assigns durability or summon perks to pets expected to remain, so merge direction and future replacement determine whether the investment survives.
- 局限：Non-official same-author sources; exact combine math is not copied into the project.
- 来源：`src-sap-tag-mechanics-2026`、`src-sap-tag-consistency-2026`。

## 机制映射与迁移边界

- **独立重复身体**：Just King 2022-08 EA 的两 Archer＋两 Berserker，有优先培养／装备一 Archer、资源少时其他副本保持低级的实践。不能写成 Just King 没有任何同类供体培养；它另有羁绊／token 升级。GGM 1.0 的 Swift Rhino＋2–4 Extrovert Shamans 是同职业个体集群，后续削弱已被作者承认，并有 1.034 图腾／Extrovert 调整；不把个体可变职业模板等同我们的固定英雄身份。
- **重复参与羁绊的局部例**：Just King 2022-10 EA Pacifist 路线的 recruitment_economy_pivot 明写“Buy a duplicate to activate the required environmental trait or restart when the shop misses.”；只证明该路线的环境羁绊，不证明所有普通羁绊计数。Auto Chess 2020-04-14 Insectoid 读取同名非 Insectoid 死者与仍存活者，再按存活同名者最高 cost 决定虫召唤；不读取后备，也不是所有死亡无条件触发。
- **DR01／DR02**：唯一名册、重复持有但限一出战均为项目制度对照，缺完整同构原作依据。
- **DR03／DR04**：可重复上场有上述依据，普通原生贡献去重／逐份计数为项目方案；不借 Just King 环境配方或 Insectoid 特殊体系证明普通全局计数。
- **DR05**：T&T 的 Clone Capsule 是定向复制目标单位的取得工具，但现有攻略未闭合复制后品阶、加强、装备、强制合成或独立长期部署的规则。Auto Chess Civet 的三星复制和开战生成 Golems 亦不能证明所有产物永久进入名册。discovery e084／s056 的 Monster Train 地图复制卡／强化单位仅是相邻结构线索，不当作英雄完整克隆证据。常规唯一＋永久复制例外仍为项目设计，不默认全继承或初期实施。
- **DR06**：酒馆战棋 2019 基础＋2022 Buddy 资料支持三同名自动 Golden、保留之前增益、打出后发现高一 tier；SAP 2026 同作者攻略支持手动合并经验／属性，接收者 Food 留下、被拖入者 Food 丢失；Setr 1.2→1.3 三改二缓解同名材料占位。区分有意保留重复身体与被迫囤升级材料，R02 已选材料培养，不重新要求同名合成。

## 项目基线与只读代码观察

[M03-D01–D03](../../../02-foundation-models/trait-model/decisions.md)明确重复取得／部署及同名去重尚未决定，普通原生 1 点不等于副本重复计数；受益资格与凑档分开。M03 既有证据中的 CountEach／UniqueContent 是配置能力，不是用户玩法决定。

主责读取 [RunRewardEconomyService](../../../../src/Run/RunRewardEconomyService.cs)的 PickEntries／Recruit／AddRosterHero、[RunDecisionService](../../../../src/Run/RunDecisionService.cs)的 CreateOffer、[RunOfferDefinition](../../../../src/Project/RunOfferDefinition.cs)及相关持久化／编队片段：已读抽样和加入路径未看到以 ContentId 排除已拥有英雄，添加会生成新的 InstanceId；这些是局部静态事实。未完整验收全调用链、存档验证及全部生产配置，因此不宣称当前正式流程已支持任何新方案，也不以代码默认替代未定产品制度。当前权威没有补齐同名名册制度。

## 后续与排除项

- 同次候选重复、刷新后重复出现、已拥有排除、有限库存耗尽及退出后再招是不同问题；本轮不把它们合成一条概率规则。
- 装备／遗物重复、临时召唤、复活、敌尸复生、卡牌复制不能替代持久英雄名册规则。
- 若采用唯一或限一上场，需要衔接候选不足、后备中持有、变体识别和退出再获取；若采用重复，需衔接每份投入、名额与普通贡献去重，不能绕过 R03 整体回退门槛。尚未预定重复补偿或全局禁用同名技能叠加。
- 没有代码／资源修改、原型、运行验证或体验结论；只维护讨论材料。I16 与 I28 记录同名、贡献／受益及招募边界。

## R07-Q02：同节点刷新、候选保留与重现

日期：2026-09-08。仅使用已有本地语料，无联网补研。委派只读研究覆盖全部 780 深记录、106 发现记录及 56 档案；主责核对 G03／R05／R06／R07 已定边界和方案适用性。RF01–RF07 见 [方案](proposals.md)，本次检索时尚无本子问题用户决定；后续结论见 [R07-D02](decisions.md)，D01 不变。

### 检索与证据范围

- 刷新词：`reroll|refresh|freeze|frozen|lock|blacklist|no.repeat|retain|刷新|冻结|锁定|保留候选|不重复`。
- 招募语境：`shop|offer|candidate|recruit|tavern|商店|候选|招募|酒馆`，均忽略大小写。
- 两式匹配全记录字段值：深记录 204 条、发现层 4 条。刷新词限定 rule_support／practical_support／mechanism／engine 四字段，招募语境仍匹配全记录：深记录 117、发现 0。档案同行同时匹配 37 份／113 行。
- 发现命中 e004／e005／e021／e064 未新增本题直接规则；lock／freeze 的锁目标、控制状态与软锁误报不计为候选保留。补查 blacklist、no repeat、without replacement、unbought、unpurchase、reroll memory、单格、保留候选、无放回、回看。
- 直接记录、档案相关段落及 source-index 元数据已回读；不是 204 条直接证据或 56 份全文精读。未找到 RF04–RF06 的完整英雄节点规则，也没有补出 Underlords 排除期限或 SAP 冻结粒度。

### 直接与相邻证据

#### Dota Underlords：`ev-dota-underlords-002-standard-economy-loop`

- 版本：late Season One / New Blood Standard archive；原库置信度 high。
- 规则摘要：Gold funds heroes, 2-Gold rerolls and XP; interest locks at combat start, levels determine board cap and tier odds, and manual rerolls blacklist unbought offers while automatic refreshes do not.
- 实践：Reroll builds, fast-level builds, temporary holds and interest preservation compete for one budget and bench.
- 限制：Jull-tide's historical 7-Gold round rule and the archive's later 5-Gold base rule are not blended; exact figures are era-scoped.
- 来源：`src-du-wiki-gold`、`src-du-wiki-player-level`、`src-du-wiki-shop`、`src-du-official-jull-tide`。

#### Super Auto Pets：`ev-sap-001-shop-gold-replacement`

- 版本：Guides updated 2026-06-04；原库置信度 medium。
- 规则摘要：Each shop phase normally begins with 10 Gold; pets and most Food cost 3, rerolls cost 1, frozen offers persist and Gold does not carry over.
- 实践：The strategy guide recommends spending nearly all Gold unless a pet rewards leftovers and selling low-tier investments once they stop improving future win chances.
- 限制：Both pages share one author and are not official; specific prices can change by patch or ability.
- 来源：`src-sap-tag-mechanics-2026`、`src-sap-tag-consistency-2026`。

#### Magicbook AutoBattler: Contract：`ev-mba-001-shared-offer-panel`

- 版本：Contract launch practice through 2025 updates；原库置信度 high。
- 规则摘要：Characters, equipment and candidate battles share one three-choice panel; gold rerolls all three offer types, while an unaffordable result can be locked for later.
- 实践：Players use the panel to search for easier or higher-reward enemies, elite recruits and build pieces, and report early turns where several desired offer types appear but cannot all be purchased.
- 限制：Exact reroll prices, offer weights and current Book-level probability tables were not published.
- 来源：`src-mba-review-system-ownership-2025-03-30`、`src-mba-review-contract-economy-2025-03-29`、`src-mba-review-role-convergence-2025-03-31`、`src-mba-guide-endless-300`。

#### Hearthstone Battlegrounds：`ev-hsbg-001-use-it-or-lose-it-gold`

- 版本：2019 base rules, with Season 13 practical opportunity-cost cross-check；原库置信度 high。
- 规则摘要：Official base rules state that tavern Gold cannot be saved for future rounds and can be spent on recruits, refreshes, tier upgrades and other actions.
- 实践：The Season 13 guide evaluates expensive Trinkets against the number of buys/refreshes left in the same turn and warns against paying for a speculative payoff without an economy engine.
- 限制：Exact costs and seasonal purchase channels changed after 2019; the use-it-or-lose-it structure, not old numeric prices, is retained.
- 来源：`src-hsbg-blizzard-intro-2019`、`src-hsbg-fantasywarden-season13`。

#### Storybook Brawl：`ev-storybook-brawl-002-shop-brawl-upgrade-loop`

- 版本：2021 EA core loop with post-71.20 pool correction；原库置信度 high。
- 规则摘要：Historical rules document alternating Shop and Brawl phases, gold-funded purchases and rerolls, XP-driven levels, three-copy upgrades and same-level treasure choices with a three-treasure limit.
- 实践：The guide and long review describe locking offers, balancing levels against tempo, positioning seven characters and choosing when to pursue triples.
- 限制：Early shared-pool copy counts are obsolete after 71.20 and exact late-EA odds are not reconstructed.
- 来源：`src-sbb-wiki-gameplay`、`src-sbb-wiki-shop`、`src-sbb-wiki-treasure`、`src-sbb-steam-guide-beginners`、`src-sbb-review-deep-intro`。

#### Guildrun：`ev-guildrun-002-shard-shop-pity-economy`

- 版本：0.5.2–0.5.6 economy, with no declared 0.5.7 table change；原库置信度 high。
- 规则摘要：Regular shops offer three heroes, two items and one relic with one 25% discount; reroll starts at one Shard and rises, Freeze carries offers while affecting pity, heroes/items sell for 66%, relics cannot be sold, and Auction House rerolls start higher and cannot Freeze.
- 实践：Red Rift players use common items as bridge power/small banks and delay the Key until the last viable shop, while a long review describes pre-ranked purchases and expensive Auction pivots locking direction.
- 限制：Hidden weights come from the maintained data snapshot; player sources do not quantify optimal roll counts or prove the economy is universally too restrictive.
- 来源：`src-guildrun-wiki-economy-0-5-6`、`src-guildrun-patch-0-5-2`、`src-guildrun-steam-guide-red-rift`、`src-guildrun-review-pivot-friction-2026-07-29`。

#### Astronarch：`ev-astro-011-item-pool-ownership-weak-duplicate-protection`

- 版本：v1.0-v1.5.3；原库置信度 high。
- 规则摘要：Ancient items stop duplicating already owned Ancient items, and common/rare items reduce their reappearance chance each time seen; items remain equipped to individual heroes and can be upgraded.
- 实践：Guides prefer a two-star synergistic item over two one-stars, keep owner-specific boss swap kits and group available pieces into caster, DPS, tank and tempo packages rather than demand one exact recipe.
- 限制：Reduced reappearance is not a full no-repeat rule and no public drop-weight table exists.
- 来源：`src-astro-official-1-0`、`src-astro-official-1-4`、`src-astro-guide-elements`、`src-astro-guide-complete`。

### 本题的精确迁移边界

- **RF01 整批替换**：常规花钱 reroll 有广泛直接记录，但不能从“没写 blacklist”推定允许立即重现。旧批不可回看、没有其他屏蔽时可再出现，是项目制度对照，不声称完整复刻。
- **RF02 部分保留**：SAP 明确 frozen offers persist／Freeze high-value pairs，来源为两篇同作者、非官方攻略；摘要未完整说明逐格冻结、可冻数量或跨手动刷新事务。Magicbook 首发至 2025 年更新前实践说明三类候选共用面板、金币重抽、买不起的结果可 lock later；锁单格／整页和持续边界未闭合。只在本节点保留所选、刷新其余为项目具体化，不引入混合英雄／装备／敌人商店，也不把 later 解释为回访旧店。
- **RF03 刚拒绝项排除**：Underlords 资料明确 manual rerolls blacklist unbought offers while automatic refreshes do not。`src-du-wiki-shop` 为末修订 2020-10-15、rev 19760 的 Shop；档案亦有手动／自动区别，但没有保存“只持续紧邻下一次、随后清空”的期限条文。RF03 的一次刷新窗口明确是项目方案，未把短期记忆扩成整节点或全局禁用。
- **RF04／RF05／RF06**：分别是节点内无放回、付费看新旧再选、历史候选持续可回选，本库未找到完整原作直接依据。它们改变搜索信息与保留机会，作为项目对照列出，不靠相似 reroll 字样硬贴游戏名。
- **RF07 软降权**：Astronarch common／rare 物品每见一次降低重现概率，Ancient 已拥有排除；其记录 scope 是 Run pool and concrete hero owner，主责另核对为局内物品池，不是跨局永久惩罚。单节点英雄降权是相邻机制迁移，不能给出未经验证的权重表，也不与既定阵容适配推荐混同。

### 跨节点／跨回合参考的去向

- 酒馆战棋 [档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/hearthstone-battlegrounds.md)在真实循环和机会成本段明确冻结把整店留到下一回合、放弃自然刷新，直接来源 `src-hsbg-blizzard-intro-2019`（2019-11-01 官方）。当前同节点没有自动刷新时钟，整页“不动”已由玩家暂不操作实现，不另造锁店功能或倒计时。
- Storybook Brawl 2021 EA shop roll／lock 与 71.20 个人池需分开，`src-sbb-wiki-shop` 末修订 2022-09-03 仍含过时共享池语境。不能拿跨回合锁店证明单格刷新或历史回看。
- Guildrun 的 Regular shop Freeze 把当前供给带往下一家商店，Auction House 不支持；wiki 为 Demo 0.5.6、build ff633149、2026-08-12。它是跨节点预约方向，当前不作为同节点候选保留方案，也不恢复 G03 已排除的回访旧店采购。
- 价格递增、免费刷新次数、pity、类别过滤、永久 banish 和招募保底不在本子问题展开；R05 已定付资源刷新与名额不被重选。候选不足必须衔接 R06 合法池与 R07 已拥有排除，不能为了满足不重复再生成不合法英雄。

本轮只更新讨论文档；未修改权威、实现或研究原库，未进行概率／交互验证。不以某个界面少一次点击当作体验已证实。
