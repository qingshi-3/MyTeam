# R08 商店与奖励：已有证据

本文件仅为讨论材料，不是现行权威。问题与项目比较见 [方案讨论](proposals.md)，进程见 [总览](../../../roadmap.md)。Q01／Q02 提案证据分节保存；后续用户确认范围见 [R08-D01／D02](decisions.md)，两次确认轮均未新增调研。

## R08-Q01：一次战后奖励中，哪些分别获得，哪些共享取舍机会

日期：2026-09-08。只比较一次奖励的领取结构：基础资源与构筑选择是否分开，不同类别是否竞争名额，一个选项能否包含配套内容，以及拒领后的结果。商店库存／购买／定价、路线选择、奖励频率、掉落概率与具体内容不在本轮展开。不同机制可以组合，不把所有遭遇统一成一种奖励模板。

### 已定前提与基线核对

- G03-D02 的前期容错、随机候选开局、随英雄／关键装备／遗物逐渐定型和适配推荐＋其他体系空间保持。
- R05-D01 已定部分战斗后可选英雄、节点招募控制增长，不代表每战必发英雄或英雄／装备／遗物必须共享一个名额。R07-D01／D02 的普通唯一和招募刷新保持，不推定免费战后奖励也可付费刷新。
- M05 装备可重复、按词条叠加／唯一；M06 遗物默认唯一、默认启用且无数量上限，特殊移除／交换另有门槛。跳过未领取奖励不等于出售已有资产或退款培养。
- [现行核心](../../../../gameplay-design/tower-autobattler-core.md)规定普通胜利先战报再奖励、装备取得后入未装备库存并展示取得结果，招募有跳过入口；没有定义本题完整的跨类别领取权利。现行三名招募候选 UI 不是本轮新数量决定。没有通过实现能力倒推方案，也未读取或修改实现代码。

## 检索记录

只读已有研究库，未联网补研或重新验证外部当前版本。主责全库筛查，复用只读研究 Agent 回读不同机制与证据缺口；没有把命中数当完成率或完整阅读量。

- [深层记录](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)：780 条 `.records`。
- [发现层记录](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)：106 条 `.records`。
- [游戏档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/)：56 份 Markdown；[来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)用于回读作者、版本和来源限制。
- 搜索 JSON 字段值，排除字段名本身的命中。首轮主题词 `reward|loot|奖励|战利品|掉落|宝箱`，与 `choos|choice|select|pick|skip|declin|bundle|pack|draft|multiple|separate|gold|选择|选取|跳过|放弃|礼包|金币|分别` 交叉。全字段深层 182、发现层 8；要求两组词在 `rule_support/practical_support/mechanism/engine` 合并值出现，深层 54、发现层 0。档案同行交叉 28 份／65 行。
- 扩查主题加入 `draft|treasure|banner|booster|prize|战后|三选|二选`，选择组加入 `budget` 并保留相关同义词，深层全字段 198／核心字段 66，发现层 9／0；档案 29 份／82 行。另用 `rg` 检查奖励、跳过、宝物和招募的相关上下文。
- 回读下列直接／相邻材料及负面案例；未逐字阅读全部宽命中。发现层 e001／e017／e018／e027／e044／e045／e066／e081／e097 主要是渠道、路线、节奏或相邻选禁，未补足一次奖励领取制度，不升格为独立规则证据。

## 相关证据与可迁移边界

### S01：同类别内给候选，玩家选其中一项

- **Hadean Tactics**：`ev-hdt-010-banner-wanderer-skip-economy`；`src-hdt-official-2-0` 为 2026-04-29 官方 2.0／Moonhunter 说明。遗物奖励从随机单件改为二选一。`src-hdt-official-1-0` 与同一记录还区分 Banner 招募、Hero Upgrade 和遗物入口。证明类别内选择及入口不同，**不证明同一场战斗同时给各类别独立选择**。没有效果统计证明此改动提升构筑多样性。
- **Auto Brawl Chess**：`ev-auto-brawl-chess-007-artifact-magic-item-layers`；`src-abc-help-dungeon-2022` 的 2022 起源历史 Dungeon 规则说明胜利后神器三选一，2026 外壳迁移不当最新玩法更新。`src-abc-video-artifact-offer-2023` 的 2023 可见画面有 Yin and Yang、Sorcerer's Ring、Ice Armor 三个候选，**仅证明候选展示，不证明最终选择或胜利，也不从名字推导特效**。
- 项目迁移：一次构筑奖励可在所属类别内部选择；同一结算支持多组各选属于 RW02 项目组织方式，不冒充上述游戏完整奖励流程。

### S02：拒领可以保护构筑，拒领补偿是另一条规则

- **Slay the Spire**：`ev-slay-the-spire-003-run-loop-economy`、`ev-slay-the-spire-020-card-offer-pivot-discipline`；`src-sts-wiki-card-rewards`（2.x，页面末修订 2023-02-28）、`src-sts-wiki-gameplay` 及适应构筑攻略。档案明确胜利后随机卡牌选一张或跳过；攻略建议拒绝稀释牌库的无用卡。**已有摘要没有记录普遍跳过补偿，也未完整记录金币／药水／卡牌的独立领取权限，不用常识补齐证据。** 本项目英雄不是抽牌库，不能直接沿用牌库稀释解释所有拒领。
- **Backpack Hero**：`ev-backpack-hero-025-relic-specificity-community`；`src-bh-wikigg-relics`、`src-bh-review-relic-downside-2022`、`src-bh-review-relic-specificity-2025a`、`src-bh-review-specificity-2025b`。三个跨年代个体反馈提到 Boss 遗物与构筑冲突／带无用副作用，最终跳过。说明稀有奖励也可能没有适合的接受项；**不是发生率、当前全部遗物规则或官方失败原因**。
- 项目迁移：保留拒领权有明确价值。RW07 的“不额外补偿”是项目对照；资料未写补偿，不等于通过原作证据证明任何情形都没有补偿。

### S03：放弃本次单位／升级或宝物，立即得到资源

- **Hadean Tactics**：仍为 `ev-hdt-010-banner-wanderer-skip-economy`。`src-hdt-official-1-1`（2024-01-31 官方 1.1）与档案明确跳过 **Hero Upgrade 或 Banner 给 50 Gold**。这是对应机会的替代价值；不扩成遗物、所有战后组或所有版本都如此。
- **Storybook Brawl**：`ev-storybook-brawl-016-treasure-slot-opportunity-cost`；`src-sbb-wiki-treasure`（EA 历史规则，末修订 2021-11-06）及相关实践。单位完成升级后获得同级 Treasure 选择，可跳过换 Gold，三件容量带来替换取舍。**触发是单位升级而非胜利；不导入三合一、三槽遗物或回合金币制度。**
- 项目迁移：RW08 可以给不想要的奖励组一个资源出口。是放弃本次机会，不是先拿物再卖；补偿物种、数额及适用组未定。

### S04：跨类别候选确实会让新增身体与已有强化竞争

- [Milky Way TD 档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/milky-way-td.md)第 32 行记录正式版韩文 Steam recommendation `175815013`：奖励条满后三选一，新单位、单位强化与技能强化混合。发评时 119 分钟、访问时 273 分钟。
- **低权重线索**：没有独立 ev／src id，档案整体为 `insufficient-evidence`；触发是奖励条而非战后，缺完整当前规则。只作为不同类型可竞争同一次选择的线索，不用它确立本项目制度。
- `ev-mba-001-shared-offer-panel` 的 **Magicbook AutoBattler: Contract** 用同一面板展示角色、装备与候选战斗，金币刷新／购买；来自 2025 评测与攻略。它证明共享展示和资源竞争，**不证明免费奖励互斥领取**。不把选敌人加入本轮奖励组。
- 项目 RW03 的“英雄／装备／遗物共享一个名额”是迁移比较，具体类别比例与候选数未定。

### S05：一个奖励选项附带让成长可用的配套内容

- **ShapeHero Factory**：`ev-shf-007-statue-synthesis-overflow-owner`；`src-shf-official-statues-tablets`、`src-shf-official-1-0-0` 与正式版实践。1.0 选择 Hero Upgrade 时一并得到相应 Statue；Statue 生产只能合成的复制素材，帮助高阶合成时保留原低阶战斗生产线。
- 它提供“选择成长，同时取得配套手段”的参照，**不证明原作多个完整礼包争一个名额，也不是赠送可战斗复制英雄**。
- RW04 把“英雄＋适量培养材料”作为项目套餐示意。附送是否已投入、未来回收是否有价值需衔接 R02／R03，不能推定赠送材料产生额外退款。

### S06：奖励可定向，但信息揭示顺序未闭合

- **Vivid Knight**：`ev-vivid-004-accessory-effect-symbol-bridge`；`src-vivid-guide-maze9` 等资料的 v1.1.11–v1.2.3 范围。Maze IX Monster Spot 可选择配件部位用于替换，配件同时影响 effect 和 symbol。
- 只支持奖励方向有局部选择。**不能证明先选部位再随机揭示物品的两阶段流程**。RW06 的“先决定装备／遗物类别，再生成具体候选”属于项目对照；若一开始全部物品已公开，仅切换标签页不构成新的机制。

### S07：全部兑换成资源很平稳，也会失去具体掉落的期待

- **Loot Loop**：`ev-loot-loop-018-gold-gems-no-equipment-layer`；`src-loot-loop-discussion-loot-boundary`、`src-loot-loop-review-missing-equipment-233616632` 与正式版公告。2026 正式版至 Patch 1.1 资料记录敌人／Boss 掉 Gold、gems，用于后续成长；一位完成玩家明确期待装备或更鲜明的掉落。
- 这支持资源掉落与后续支出的分离，以及个体的奖励期待落差。**不证明它有“基础资源＋装备候选”双层结算，也不代表普遍玩家评价**。
- RW01 仅取直接发基础收益的部分作项目方案。已有 G03 要求多种构筑信号，不据此改成所有奖励货币化。

## 缺口、排除与编号关系

- 没有找到可严格闭合“基础金币与构筑奖励同时分别领取”“同一场多个类别各选一次”“奖励面板按预算拿多件”的完整规则。RW01 的双层组织、RW02 的同场多组、RW04 的多包竞争、RW05、RW06 的完整流程均明确为项目对照。
- **候选不可用**：回读 `ev-shf-019-unproducible-reward-rng-negative`，正式版玩家报告奖励缺地图资源／配方前置／研究支持。仅增加选项或显示警告不保证可用；I27 的合法池与类别材料依赖仍需实际内容校核，不能靠本题补偿掩盖不合法供给。低暂时收益但存在未来用途，不等同非法奖励。
- `ev-combat-alchemy-005-accumulation-guard-reworks` 涉及 Life 资格、删除金币奖励与药剂容量，没有证明本题领取组织；容量／积累归 R09／内容，不推定官方改动动机。
- Buddy 进度、自动等级和人口里程碑归既有成长／节奏；商店 paid reroll、冻结与混合面板归 R07／R08 后续；逃生结算、无尽报酬归后续流程／风险；服务器和存档奖励修复归实施契约。它们不是本轮独立奖励选择机制。
- 稳定编号 RW01–RW08 的完整过程、得失与组合关系见 [方案讨论](proposals.md)。本轮不要求每项采用或只能选一项。没有研究原库写入、运行或体验验收。

## R08-Q02：同一家商店买完一项后，还能怎样继续购物

日期：2026-09-08。限定同一次访问中的非英雄商品／服务：购买后是否有剩余库存、怎样获得新货、是否存在独立于金币的总购买或分类额度。商品售价／刷新价格曲线、具体限次数、卖出、讨价还价、掉落类型和路线形式另归经济／内容，不扩成本轮待选机制。

R08-D01 的战后奖励结构保持。R05 已定英雄单次最多招募 x 人，刷新不增加名额；R07 已定英雄普通唯一、全刷＋已见软降权及 RF02–RF06 遗物分工，不能从此推定所有物品都已接受同样补货制度。G03 战前不限时、不回访旧店保持；M05 装备可以重复不等于某店无限售同款，M06 遗物唯一不等于一店只能买一件不同遗物。

只读核对 [核心权威](../../../../gameplay-design/tower-autobattler-core.md)：装备购买入未装备库存、选择时可查阵容及装备自由调整已有规则；未找到本题完整的物品购买总次数、售罄／刷新契约。不以已有界面或原子提交能力代替产品决定，未读取或修改实现代码。

### Q02 检索范围

- 沿用上述研究库路径，对 780 深记录、106 发现记录和 56 档案重新按本题筛查；主责做范围检索和基线核对，研究 Agent 独立回读具体购买权／补货差异，不写文件。未联网补研，外部版本以原记录为准。
- 首轮字段值主题词：`shop|merchant|store|商店|商人|商铺|货架`；交叉 `stock|restock|refill|replenish|sold.?out|purchas|buy|bought|reroll|refresh|service|库存|售罄|补货|购买|买入|买下|刷新|服务`。深层全字段 108／四核心字段 48，发现层 5／0；档案同行交叉 35 份／82 行。核心字段仍为 `rule_support/practical_support/mechanism/engine` 的合并值。
- 扩查加入 `vendor|market|市场` 与 `offer|quota|limit|once|限购|限次|一次`，深层 149／64，发现层 5／1；档案 38 份／97 行。另查 `courier|restock|replenish|sold.?out|补货|售罄|自动补|购买次数|购买上限|重复购买`，排除回盾、体力补充、名称含 Courier 和事务 bug 等噪声。
- 发现层 e004 只有酒馆法术与单位竞争货币／槽位的线索，e005／e007／e064／e079 为赛季、共享池、宽泛商店或停服信息，未补足库存契约。命中数不代表全文阅读量或完成率；定点回读如下材料，未逐字读完所有宽命中。

### S08：有限库存可以按某种商品规定

**Tales & Tactics**：`ev-tnt-003-equipment-transfer-and-role-ownership`，直接数量来源 `src-tnt-steam-army-size-2026`。2026-08-10–11 的 2.0 社区答复称，每店有两件卸装消耗品、每件 1 Gold，一次把某单位全部装备退回背包；[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/tales-and-tactics.md)第 53 行保留过程。

它支持特定常用品有明确每店供给数量，不只是玩家没钱；**不证明全店共用两次购买，也没有保存刷新会否恢复该商品库存的完整条文**。数量／价格只属于这份社区版本记录。本项目已允许战前免费换装，只取库存结构参考，不恢复卸装收费。

SH01 的“整店固定货单、买后空位、访问期间不刷新”是完整项目对照，不能用这一种商品的两件库存证明所有原作商品都如此。

### S09：物品／遗物供给可以刷新，但陈列数量不是总库存

- **Guildrun**：`ev-guildrun-002-shard-shop-pity-economy`；`src-guildrun-wiki-economy-0-5-6`（2026-08-12，Demo 0.5.6、build ff633149）及相关实践。普通店陈列三英雄、两物品、一遗物；付 Shard 刷新，价格递增，Auction House 刷新更贵。**六个陈列位不等于整个访问只能买六件，也不证明买一件后立刻补货或保持空位。** Freeze 带到下店不迁入本项目。
- **Backpack Dungeon**：`ev-bpd-008-shop-craft-pivot`、`ev-bpd-015-reroll-item-convergence`；`src-bpd-patch-2-0-2`（2026-07-17 官方正式版）确认遗物商店至少有一次免费刷新，评测说明购买／刷新组件的过程。**一次免费不等于只许刷新一次**，不从中推定费用曲线或库存总数。
- **Magicbook AutoBattler: Contract**：`ev-mba-001-shared-offer-panel`，2025 评测与攻略保留角色／装备／候选战斗共享付费刷新面板，以及想买多种东西但钱不足的实践。支持多类供给与花钱换候选，**不证明购买即补货**，也不将选敌人加入本项目物品商店。
- **Monster Train**：`ev-monster-train-007-upgrade-artifact-economy`；`src-mt-wiki-merchants`（2.x，2022-04-27 rev 9911）说明不同商人提供升级、reroll 与 purge，竞争 Gold。**单位通常两个升级槽不是每店最多买两次**；现存摘要未闭合商品各自库存、刷新限次和重复服务上限。

SH02 以这些物品刷新渠道为参照，具体“买后留空、其他货仍可买、付费整批换货”是项目合同式比较；不能把研究中的刷新存在直接补成无限刷新／无限购买。

### S10：类别购买额度与特殊补货是两条可分别改写的规则

**Storybook Brawl**：`ev-storybook-brawl-004-hero-shop-effect-owners`、`ev-storybook-brawl-005-peter-hatball-build`；`src-sbb-wiki-gameplay`（2021 EA，末修订 2021-11-08）、`src-sbb-wiki-crystal-ball`（2022-11 EA，末修订 2022-11-04）及时期攻略。

- 普通每轮 Shop 可以买角色，但只有通常的一次 spell 权。限制属于法术类别，**不是全店只能购买一次，也不是只陈列一张卡**。
- Crystal Ball 让符合条件的定向 spell 不占通常 spell 权，并补入新 spell，形成连续法术机会；[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/storybook-brawl.md)第 57 行明确记录。
- 条件商品、通常额度豁免、补入新候选需要分别理解。没有这项特定内容，不能把整家店都视为默认买一补一；非定向法术、缺钱和组件未到都能中断该构筑。`ev-storybook-brawl-006-hatball-reachability-lifecycle` 另保留历史可达性／兼容修复，不当当前 live meta 或胜率依据。

SH03 的普通购买即补货是项目扩展对照；本证据最适合支撑特殊遗物按条件提供补货。SH05 的分类限购有直接参照；R05 已确认英雄限额独立有效，不能因物品继续购物而追加英雄名额。

### S11：同次访问可重复使用服务，价格随本店使用次数变化

**Backpack Hero／Tote**：`ev-backpack-hero-009-tote-thin-deck-economy`；`src-bh-video-tote-1161` 为 Shlomi Arbeitman 于 2024-11-15 UTC 发布的 patch 1161 实战讲解。商店删牌价按 5／10／15……递增，每家店分别重置；[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/backpack-hero.md)第 75 行保留过程。

它证明服务可以在同店重复使用，不是第一次买完便永久售罄。**没有证明无限次数，价格仅属于观察补丁**。SH06 的常备材料／商品可持续购买是项目对照，不能把重复服务等同无限物品库存。若本项目培养回退采用服务窗口，仍须符合 R03 实际门槛；本题不额外开放免费回退、出售或卸装消费。

### S12：反复购买／刷新有自己的构筑价值与成本

- `ev-combat-alchemy-003-zero-money-purchase-exploit`，`src-ca-itch-patch-v0-2-2023-12-15` 的历史 Ninja 每轮四次购买产生永久 Crit；后续玩家报告失败购买也计数的漏洞。说明“购买次数”本身可能被内容读取，不能把补货当纯界面变化。**四次触发不是商店只可买四次，也不能借失败购买证明合法购买没有门槛。** 当前版本是否存在该 bug 未验证，技术原子性不是本轮新的玩法选择。
- `ev-bpd-015-reroll-item-convergence`：`src-bpd-review-balance-convergence-2026-07-17` 个体评测认为长流程、多刷新让少数强件可反复强求；`src-bpd-discussion-relic-reroll-2026-08-20` 的另一玩家报告花 180 万金币仍未见目标，开发者仅确认 Endless 特定遗物概率很低。两者分别提示过度可求与实际上不可求，**没有概率表或总体统计，不拼成同一版本的普遍结论**。
- `ev-slot-003-three-resource-economy` 的 Demo 实践说明刷新价随使用上涨、后续波次下降；`ev-ggm-012-trait-and-item-choice-reworks` 的官方历史重做针对随机特性反复刷新与低阶装备闲置。它们归费用／培养和供给可达性，不把递增价格或每波重置再列成新的库存制度，也不据此锁定项目公式。

### Q02 缺口与排除

- 未找到可可靠闭合的普通全店买一补一、全店共用 x 次购买、单格付费补货或明确无限库存常用品规则；相关完整项目流程不得冒充调研事实。SH01／SH02 的买后空位与是否刷新、SH03 普遍化、SH04、SH06 常用品部分均有项目定义。
- Slay the Spire 的 `ev-slay-the-spire-003-run-loop-economy`／`ev-slay-the-spire-020-card-offer-pivot-discipline` 与 `src-sts-wiki-merchant` 记录分类购买和移除机会成本，但没有保存 Courier 的补货契约；不靠常识添成此次已核验依据。
- 物品使用一次／两次不是商店限购；背包容量不是购买次数；商品从画面消失或保存修复不能证明补货时点。跨节点预约、重访、多人共享池、限时抢购和局外商店不进入本题。
- 局内物品刷新是否沿用 R07 的软降权与遗物策略须有明确适用范围；本轮建议可复用既定交互思路，不再逐项表决冻结／回看，但不能悄悄视为 D02 已覆盖所有物品。具体限次／价格交 R09，供给资格和已持有唯一继续独立生效。
- 完整 SH01–SH06 比较与原 Q02-P01 见 [方案讨论](proposals.md)。后续用户采用 SH02 并要求有限刷新、提出特殊资源门槛候选，见 [R08-D02](decisions.md)；这不新增原作证据，也不把其他机制一并采用。R08-D01 保持，未新建商店内容、运行或测试。
