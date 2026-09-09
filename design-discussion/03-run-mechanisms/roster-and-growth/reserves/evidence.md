# R04 后备与轮换：已有证据

日期：2026-09-08。仅检索既有本地调研，不修改研究原库、不联网重核版本。问题及稳定编号 BR01–BR10 见 [方案](proposals.md)，这些编号代表机制比较，不代表已采用。

## 检索范围与边界

- 全库字段值检索：[深证据](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) 780 条、[发现层](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json) 106 条；[游戏档案目录](../../../../web/game-mechanics-atlas/research/deep/game-dossiers) 56 份。来源链见 [source-index](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。
- 主查询：`bench|reserve|roster|storage|Box|hand size|holding|后备|替补|名册|仓库|储存|暂存` 与 `capacity|cap|limit|slot|full|expand|replace|overflow|容量|上限|格位|满|扩|替换` 交叉（实际正则用词干）。全部字段值命中深层 291 条；收窄到 rule_support／mechanism／practical_support／engine 四字段 57 条；发现层 0 条；档案同行交叉 28 份／64 行。
- 扩查备战席、手牌上限、满员、无限名册、bench full、storage／weight、Box 等；定点补回 Tiny 的后备、SAP 冻结与 Underlords Duos 满后备禁止传送。发现层无命中不表示所有游戏无此机制；部分旧中文字段存在编码质量限制。
- 全库参与筛查，不表示全部候选全文精读。读取直接相关完整记录及 Guildrun、Vivid Knight、Kādomon、Setr、GGM、Tiny、TFT、Underlords 等档案相关段落。部分宽查询输出截断，未把截断内容计为已读；关键直接证据单独回读。
- 不把上场人数增长、临时召唤上限、全游戏角色图鉴、物品背包、赛季池大小或预组包限制当作后备容量。Slotbound／Dwarves 的人数扩张等没有直接证明后备扩张，排除出本题主比较。Gods vs Horrors／Guildrun 的特定后备收益留后续内容边界，不由容量推定。
- 未找到足够依据证明 Kādomon Box 无上限，亦未核实常被口述的 TFT 九格、炉石十手牌及其完整超容细则，不填入惯常印象。BR04 无限持有、BR07 一次替换交易、BR08 临时超容均明确为项目方案；BR01 固定后备制度只有原作局部参照。

## 直接回读依据

以下保留原研究的事实／玩家实践／版本限制与来源 id；机制迁移判断以 proposals 为准，不以研究里的旧 project_question 充当现行规则。

### Guildrun：`ev-guildrun-004-reserve-backup-ownership`

- 版本：Demo 0.5.5–0.5.6 active-board/reserve rules；原库置信度：high。
- 规则摘要：The active team expands from three to five while roster capacity reaches six; specific Backup modifiers allow the reserve hero to provide a defined effect from outside the active board.
- 实践记录：Mystic players move Fiona or Grace into Backup to retain shield/stat utility while fielding a higher-rank active hero; another player reports that reserve investment can be difficult to recover.
- 限制：Sources do not publish every reserve interaction, buff eligibility or replacement edge case, and Fiona's strength is disputed.
- 来源：`src-guildrun-wiki-economy-0-5-6`、`src-guildrun-wiki-rank-modifiers-0-5-6`、`src-guildrun-discussion-mystics-2026-08-23`、`src-guildrun-review-position-backup-2026-07-19`。

### Vivid Knight：`ev-vivid-001-mana-route-jeweler-loop`

- 版本：v1.1.10-v1.2.3 Steam；原库置信度：medium。
- 规则摘要：Dungeon movement consumes Mana while fights provide Keene and free units; Jewelers buy, sell and reroll units, party/storage slots expand for Keene, and Witch's Maze IX adds harder routes and Magic Hammer rewards.
- 实践记录：The Maze IX route clears most profitable fights, avoids impossible Monster Spots, carries Mana recovery, delays ordinary rerolls and returns to shops only when enough pairs and Keene make a broad roll-down efficient.
- 限制：Exact movement/over-capacity Mana formulas and reroll costs are guide-period rules, not a current official table.
- 来源：`src-vivid-guide-easy-start`、`src-vivid-guide-maze9`、`src-vivid-official-1-1-5`、`src-vivid-official-1-1-34`。

### Vivid Knight：`ev-vivid-012-pair-holding-slot-reroll-economy`

- 版本：Witch Maze 8 to Maze IX v1.2.3；原库置信度：medium。
- 规则摘要：Party/storage slots, paid rerolls, equal unit prices and three-copy upgrades create a finite holding problem; the official patch lowered Jeweler refresh cost in early mazes.
- 实践记录：Guides buy character slots before rerolling, hold many pairs, stop most bridges at silver, sell obsolete upgrades before a large roll-down and use traveling symbol filters only when they match several live outs.
- 限制：Keene values and optimal floor timing are author routes, not invariant rules; one guide is Witch Maze 8 and another Maze IX.
- 来源：`src-vivid-guide-maze9`、`src-vivid-guide-witch-maze`、`src-vivid-guide-easy-start`、`src-vivid-official-1-1-5`。

### Gladiator Guild Manager：`ev-ggm-001-timeline-guild-resource-loop`

- 版本：1.0 campaign with 2025 optional Glory constraint；原库置信度：high。
- 规则摘要：Time advances through shops, discounts, quests and championships; buildings unlock recruitment, respec, Trait teaching and item upgrading, while reputation changes faction rewards and sabotage.
- 实践记录：The campaign guide sequences four core unit buildings, respec, Trait Crucible, roster capacity and resource conversion around monthly championships and salary/revival needs.
- 限制：Exact monthly prices and event probabilities are guide observations; Glory is optional and later than the 1.0 route.
- 来源：`src-ggm-official-1-0`、`src-ggm-steam-campaign-v1`、`src-ggm-steam-achievements-guide`、`src-ggm-official-glory-2025`。

### Kādomon: Hyper Auto Battlers：`ev-kado-001-route-recruit-evolve-replace`

- 版本：Official FAQ through 1.0.12, with Early Access and 1.0 progression separated；原库置信度：high。
- 规则摘要：Battles, events and releasing creatures provide experience; level-two and level-three evolution changes the creature; the level-three cap was removed at 1.0, while party and Box management provide recruitment and replacement surfaces.
- 实践记录：Players route through encounters, events, shops and camps while deciding whether to level an existing body, recruit a bridge or preserve a Box replacement.
- 限制：The public material does not provide a complete current shop-odds table, Box capacity history or one official end-to-end manual.
- 来源：`src-kado-official-faq-2024-02-20`、`src-kado-official-040`、`src-kado-official-100`。

### Tiny Auto Knights：`ev-tak-003-sell-reroll-bench-economy`

- 版本：Demo 0.14.14 with 1.1.0 zero-Coin guard；原库置信度：high。
- 规则摘要：0.14.14 specifies sell values, sell-triggered Coins gained, XP rewards, free rerolls and bench behavior; 1.1.0 makes zero Coin non-triggering.
- 实践记录：Holding a pair or late Legendary competes with selling, rerolling and adapting to offered units.
- 限制：Current odds, bench capacity and full sell table are unavailable.
- 来源：`src-tak-official-demo-01414`、`src-tak-official-110`、`src-tak-thread-legendary-availability`。

### Dota Underlords：`ev-dota-underlords-004-duos-separate-economy`

- 版本：Big Update launch through Season One Duos；原库置信度：high。
- 规则摘要：Eight teams of two share 100 Health and Level, recruit separately, can send heroes and 1 Gold, use a doubled pool, and combine or difference the two boards' round damage.
- 实践记录：A full teammate bench blocks hero transfer, and team health/streak outcomes make support timing different from Standard.
- 限制：Duos level, pool, damage and streak logic cannot validate Standard build economy.
- 来源：`src-du-official-big-update`、`src-du-wiki-duos`、`src-du-wiki-heroes-pool`、`src-du-official-jull-tide`。

### Skull Horde：`ev-skull-horde-002-standard-roster-economy`

- 版本：formal release structure, corroborated through 2026-04 reviews；原库置信度：high。
- 规则摘要：Independent reviews agree on a three-offer recruitment/reroll loop, six unit-type lines and three-copy upgrades from base to higher tiers.
- 实践记录：Replacing a unit line loses its accumulated progress for only partial refund, while rarity expansion adds specialized units but dilutes current lines.
- 限制：Six refers to persistent unit-type lines, not a universal cap on live summons or reanimated bodies; exact current shop odds are unavailable.
- 来源：`src-skull-review-screenhype`、`src-skull-review-mkau`、`src-skull-review-gamerz-theory`、`src-skull-review-three-population-223778112`。

### Super Auto Pets：`ev-sap-001-shop-gold-replacement`

- 版本：Guides updated 2026-06-04；原库置信度：medium。
- 规则摘要：Each shop phase normally begins with 10 Gold; pets and most Food cost 3, rerolls cost 1, frozen offers persist and Gold does not carry over.
- 实践记录：The strategy guide recommends spending nearly all Gold unless a pet rewards leftovers and selling low-tier investments once they stop improving future win chances.
- 限制：Both pages share one author and are not official; specific prices can change by patch or ability.
- 来源：`src-sap-tag-mechanics-2026`、`src-sap-tag-consistency-2026`。

### Setr's Auto Battler：`ev-sab-005-upgrade-slot-rework`

- 版本：1.2.0 launch feedback through 1.3.0 rework；原库置信度：high。
- 规则摘要：Version 1.3 reduced same-unit upgrade requirements from three copies to two; the client allows a full team to buy only when the purchase immediately creates a legal merge.
- 实践记录：Early review and discussion report that three-copy upgrading occupied two or three of five positions, restricted unit variety and made buy-sell supports harder to operate; later feedback says the 1.3 change removed that exact pressure.
- 限制：The reports do not establish whether two copies became optimally tuned or whether reserve space would have solved other constraints.
- 来源：`src-sab-review-kona-2021-12-29`、`src-sab-steam-upgrade-feedback-2021-12`、`src-sab-official-130`、`src-sab-official-html5-client-130`。

## 机制映射、反例与缺口

- BR01：Vivid／Tiny 支持场上与储存区分，固定数量为项目对照；撤下场上英雄也可能挤满后备，与 BR02 的总量规则不同。
- BR02：Guildrun 六人总名册与三至五人上场；不得推定开局已满六容量或复制 Backup。
- BR03：Vivid 的付费扩容有直接记录，GGM 经营规划补充扩容与其他投资竞争；进程免费扩容为项目变体，不声称两作都采用。
- BR04：Kādomon 支持 Box 持有替换，不能证明无限。无上限是否造成全反制收藏取决于未定供给，是设计风险推论。
- BR05：Vivid 档案“迷宫、Mana 与 Jeweler”明确超 storage／weight 压迫 Mana；精确公式不可补写，也不由此假设我们已采用移动消耗。
- BR06：Underlords Duos 满 bench 阻止传入，有模式边界；不概括 Standard 所有购买。
- BR07：Skull Horde 整线替换及进度损失支持替换成本比较，不足以证明本项目所提的一次操作或精确退款交互。
- BR08：本库缺直接英雄例证，明确项目设计；不是运行验证结果。
- BR09：SAP 冻结是未来候选保留，非已拥有后备；具体商店制度归 R07／R08。
- BR10：Setr 1.3 的满员即时合成与三改二，是直接的容量压力修复；该压力不能移植为材料培养必须需要囤同名英雄。
- Kādomon 历史最终 Boss 将 Box／放生史变成敌人，是特定关卡风险，已在档案保留；不扩入本轮容量机制或推荐为通用持有惩罚。后备能否追赶培养另论。

现阶段没有体验验证、容量最优值或“无上限必然更好”的证据。R03 回收机会与名册退出需独立校核，不能把原作售卖收入套成培养材料退款。
