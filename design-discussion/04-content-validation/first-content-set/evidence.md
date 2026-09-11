# V01 首批内容检验：依据与限制

这是候选样例的来源和纸面检查记录，不是现行权威，也不证明平衡、实现或体验已通过。样例见 [proposals.md](proposals.md)，进度见 [总览](../../roadmap.md)。

## V01-Q01：低费护盾前排的机制升阶

日期：2026-09-10。本轮不重选已定全局制度，使用少量具体内容检验“低费功能位可通过专属机制加强成为核心”。参考机制须分清读取对象、触发和实际代价，不能把反伤、破盾、受盾次数与盾量一概合并为防御转输出。

### 已确认边界与当前权威

- [M01-D01](../../02-foundation-models/unit-model/decisions.md)：固定主动＋被动、自动施放、无随机出生个体属性；后续 R02 已细化专属升阶选择空间，不把原无本体配置泛化为禁止加强原能力。
- [R01-D01](../../03-run-mechanisms/roster-and-growth/hero-tiers/decisions.md)允许不培养的羁绊单位，品阶主要改机制；[R02-D01–D03](../../03-run-mechanisms/roster-and-growth/cultivation/decisions.md)允许不同成长结构及通用／匹配类别材料混付。样例不确定全部初期结构、价格或材料窗口，复用不能牺牲特色。
- [M03-D01–D03](../../02-foundation-models/trait-model/decisions.md)：贡献、身份、受益分开，普通开战定档；[M04-D01／D02](../../02-foundation-models/attributes-and-statuses/decisions.md)将具体状态生命周期和控制反制留内容，不逐条审批一套全局算法。
- [M05-D01–D03](../../02-foundation-models/equipment-model/decisions.md)：装备同品质定义固定，重复可穿，属性与唯一特效分别处理，品质升级不等于随机词条；本轮不展开配方。
- 只读核对 [玩法框架](../../../gameplay-design/combat-build-framework.md)的职责、供给／状态／兑现／生存／空间关系，以及 [核心权威](../../../gameplay-design/tower-autobattler-core.md)的普通攻击与有效伤害回蓝、自动满蓝技能、失败回滚、有效护盾受损和战报事实。旧人口／玩家介入等权威差异继续按已确认讨论结论留统一整合，不回写。

V01 可以按已定模型子集检验，不依赖未定的 R10 外层形式。英雄、装备及升阶视为纸面已合法取得，不据此保证本局供给或认定初期正式制作。

### 检索范围

- 沿用并回读 [R02-Q01 全库研究](../../03-run-mechanisms/roster-and-growth/cultivation/evidence.md)：原 780 深记录四字段 99 候选／全字段扩查 247；56 档案命中 29 份／107 行；106 发现记录命中 e025／e029／e036／e051／e065／e084。本次没有重跑或把旧命中当新增。
- 在已有深记录与档案中按护盾、破盾、反击、格挡、实际承伤及防御转换展开定向材料，筛到 44 条，回读 15 条记录、5 份相关档案和 13 个关键来源索引块。完整库入口：[深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/)、[来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。
- 原作规则、攻略实践、补丁与项目推演分开。未联网或声称重读全部外部原文；数量不是完备性或内容质量指标。这里的样例选择不等于只保留支持它的盾机制，差异及未使用机制如下。

### 可区分的原作机制

| 标识 | 原作过程与构筑作用 | 来源与限制 |
|---|---|---|
| V01-RF01 破盾触发范围伤害 | Auto Chess 的 Evil Knight 有具名 shield-break AOE；防护消耗完的时点成为伤害出口。 | ev-auto-chess-015-knight-shield-lifecycle-drift；src-ac-official-civet-s13，2021-07-21 S13。仅此技能，不证明所有护盾破裂爆炸、到期／驱散／主动移除也触发，或伤害按盾量／吸收量计算。 |
| V01-RF02 按收到护盾的次数成长 | Guildrun Kai 每份独立 Shield receipt 增 Attack，Aria／Gustav／Pollen 频繁供盾，Mana Regen 加快施法。追求的是频繁收到盾，不单是更大一份盾。 | ev-guildrun-007-kai-shield-feedback-build；src-guildrun-grind-endless-guide-0-5-2、src-guildrun-wiki-rank-modifiers-0-5-6、src-guildrun-patch-0-5-2／0-5-6、src-guildrun-discussion-mystics-2026-08-23。0.5.6 修 AD 生效，不能声称当前强度；permanent Attack 不推成跨局成长。开场爆发／击杀供盾者／控制施法可打断。 |
| V01-RF03 读取当前防护量 | Guildrun Warrior Shield Power 当前盾量→Attack，与 Kai 的受盾次数分开；TLF Karina 当前盾→暴击率，可与施法、攻速及 Omnicrit 配合，盾被打掉会同时失去保护和输出；STS Body Slam 读取当前 Block 造成攻击，升级 1 费→0 费，记录没有消费 Block。 | ev-guildrun-007-kai-shield-feedback-build；ev-tlf-005-karina-shield-to-crit／src-tlf-steam-karina-2025、src-tlf-gameplay-starter-2025、src-tlf-steam-indepth-2026；ev-slay-the-spire-006-body-slam-build／src-sts-wiki-block、src-sts-wiki-entrench、src-sts-wiki-body-slam、src-sts-guide-modern-ironclad、src-sts-guide-ironclad-a20。转 Attack／暴击／一次攻击是不同出口；Shield Power 为职业 modifier，Karina 为单英雄挑战实践，STS 需手动出牌。 |
| V01-RF04 消耗防护换输出 | Siralim Ultimate 的 Holy Blast 供应最大 Barrier，Warp Reality 主动移除 Barrier 换法术伤害；兑现要付出失去保护的代价。 | ev-siralim-ultimate-024-barrier-explicit-converter；src-su-discussion-barrier-removal、src-su-discussion-barrier-health-damage、src-su-wiki-buffs、src-su-compendium-2-0。与 RF03 保留防护不同，不迁手动施法权限，不推所有护盾伤害都耗盾。 |
| V01-RF05 成功格挡读取抵消量 | Dwarves: Glory, Death and Loot 的 Vengeance 在成功格挡后按被抵消伤害反击，Block 上限与 Frozen 禁格挡／闪避限制触发；不可格挡攻击不供给这条链。 | ev-dwarves-glory-death-loot-008-block-vengeance-chain；src-dgdl-companion-stats、src-dgdl-companion-items、src-dgdl-discussion-block-cap、src-dgdl-discussion-thorns。v2.0.10 维护材料的 90% 上限不移植、不冒充 v2.1 数值；格挡抵消量不是护盾吸收量。 |
| V01-RF06 按实际受伤反击 | Siralim Retribution 读取实际攻击／法术承伤；Art of War 另加持有者最高属性项，Seraphim 防御转生命负责生存。间接伤害不供应该机制。 | ev-siralim-ultimate-013-paladin-defense-retribution-build；src-su-guide-bargain-bin-2-0、src-su-wiki-combat、src-su-guide-specializations-2-0-37、src-su-compendium-2-0。不将实际承伤说成减免量或护盾吸收，不能与 RF05 混成同一个指标。 |
| V01-RF07 受击次数定时点，防御定强度 | Girls of the Tower 历史九石化：被攻击叠 Guard，达到阈值按物理＋魔法防御对周围造成伤害。更多合格受击使触发更频繁；远程、低频攻击或绕后可削弱收益。 | ev-girls-of-the-tower-008-historical-nine-stone-build；src-gott-official-patch-1-0-0-9／1-0-1-0、src-gott-discussion-hidden-boss-builds、src-gott-review-three-builds。历史羁绊，次日修伤害类型、后续又改版；不是原作某低费英雄专属升级。 |
| V01-RF08 反伤的覆盖与主动兑现 | Dwarves 普通 Thorns 回应合格近身攻击；Thornward 扩到魔法／投射物并按 Thorns 生盾；Crown of Thorns 周期性将 Thorns 变范围伤害并附 Bleed。三者分别改变覆盖、保护和出手时点。 | ev-dwarves-glory-death-loot-010-thorns-reader-chain；src-dgdl-companion-items、src-dgdl-discussion-thorns、src-dgdl-discussion-forge-demon。低攻击频率／远程／不合格伤害削弱原反伤；不把各件作用当护盾默认能力。 |
| V01-RF09 失盾抽技能的循环及失败 | Dungeon 100 的 Shield Aura／Battlecry 供盾，失盾触发 Loot Box，Shield Burst 另作伤害出口。官方曾禁止 Loot Box 随机再出盾技能以断自循环，后续限制每秒次数。 | ev-d100-012-lootbox-shield-loop-rate-guard；src-d100-official-2022-12-06／2023-07-04、src-d100-guide-strange-builds、src-d100-thread-shaman-pivot。保留事件递归反例，不把随机技能或原作每秒 3 次照搬进英雄。 |
| V01-RF10 通过明确装备／能力桥接属性 | Astronarch 的 Brawler MaxHP 供盾／反击；Druid 受击成长与装备 ATK→盾相接；Paladin 防御→邻接盾同时支持 Burn。不同角色和载体承担转换步骤。 | ev-astro-006-defense-shield-heal-piercing-matrix；src-astro-guide-elements、src-astro-guide-high-corruption、src-astro-guide-classes、src-astro-official-1-4。不能推成护盾本体自带全部转换或全队属性自动汇总。 |

上述来源没有保存足够费用证据证明 Kai、Evil Knight 等就是“低费升阶成核心”的直接例子。R02 旧库的 Astronarch 主／被动预设升级与有限 Ability Orbs、Guildrun B 专精及 A／S 职业候选、TFT ev-tft-010-high-impact-augment-agency 提供成长结构参照，仍不等于本例具体能力已由原作验证。

### 从证据到 H01／U01 的项目适配

候选 H01 固定主动供盾、固定被动在有盾时普攻附伤；U01 将实际护盾吸收存入一份蓄势，在下一次原主动施法时转为近身范围伤害。它借鉴反击、明确读数和定时兑现的区别，**不是逐字移植任何单一原作**。本库没有保存完全相同的“低费护盾英雄＋该项专属升阶”合同。

选择实际吸收量，使队友提供的防护可以成为输出投入，也避免单纯刷新许多小盾便空转成长。等待下次原技能兑现保留施法和生存窗口；附近范围保留部署价值。这是样例选择，不否定 RF01–RF10 的其他内容空间，也不把所有机制塞给一名英雄。

### 纸面核对实际覆盖

- 已核对 M01／R01／R02 的接受范围：本体仍固定主动＋被动，原技能加强不自动成为第三技能；单条加强不确定全员固定升阶路线。
- 已逐项推演 proposals.md 的 C01–C06：无投入、自启动、外部补盾、低压力、远程／爆发、重复装备与盾移除。补清首次施法启动等待、旧蓄势消费后新吸收归下一轮、防护被移除不追溯清已有蓄势等候选语义。
- E01 开场盾给首次启动提供一种可替代帮助，A01 邻近供盾会因战场位置变化转移；不写成指定辅助保证永远给盾卫供盾。多来源盾不默认无条件叠加。
- 只在纸面上发现一条可解释的因果链；没有执行代码、配置正式资源、模拟战斗或试玩，没有证明强度、可玩性、供给可达或系统事件已支持。数值、实际节奏与更大组合覆盖仍需后续工作。

当前没有因此提出改动公共决定，依赖继续沿 I15／I18／I24／I27 和具体内容归属处理；仅更新讨论区。
