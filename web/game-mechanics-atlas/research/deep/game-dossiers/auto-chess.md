# Auto Chess

## 身份、范围与研究深度

- `title_id`: `auto-chess`
- 开发 / 发行：Drodo Studio / Dragonest；独立版 Steam App 1530300。Dota 2 Workshop 的 `Dota Auto Chess` 是 2019 前身，不与独立版共用版本结论。
- 类型：八人共享棋子池、商店招募、合成升星、种族 / 职业阈值、装备与站位驱动的自走棋。
- 时期：2019 独立移动版；2020-04 Epic PC Early Access；2020-12 PlayStation Early Access；2022-12 Steam；2025-03 至 2025-05 为最后找到的完整英文大更新窗口；Steam News 另有 2026-03-26 维护节点。
- 范围：四套主构筑均严格标出历史版本。2019 BlueStacks 构筑用于研究独立移动版早期结构；2020 官方 `Divinity Water Shaman` 用于闭合装备、经济、站位和单核反制；2021–2025 官方节点用于生命周期与当前时代规则，不把它们拼成一套“当前最强阵容”。
- 深度：38 个实质来源，覆盖九个官方规则 / 数据页、七个官方开发 / 生命周期节点、九个官方补丁节点与十三篇完整实践攻略；26 条标准化 evidence。四套主构筑和两条补充线的 engine、状态、owner、经济、站位、pivot 与 counter 均闭合，因此 disposition 为 `anchor-retained`。

## 来源包

| 功能 | 数量 | 内容 |
| --- | ---: | --- |
| 官方规则 / 数据 | 9 | Game Goal、升星、Lineup & Synergies、Classic、Duo、新手规则、Knight、Water Spirit、Devastator |
| 官方开发 / 生命周期 | 7 | PC / PlayStation 平台节点、Item mechanism、Insectoid、Shaman 重做、Cave Prodigy、2026 Steam 维护 |
| 官方补丁 | 9 | PlayStation 修复、S13、S16、2.10、S20、2025-03 / 04 / 05 三个更新窗口 |
| 完整实践攻略 | 13 | 官方 Water Shaman；十一篇 2019 BlueStacks 规则 / 构筑 / 经济 / 站位；一篇 2023 Steam 历史构筑集 |

所有 source id、URL、日期、可访问性与限制见 `../source-index.md` 的 Auto Chess 段。十一篇 BlueStacks 页面的 JSON-LD `datePublished` 分别落在 2019-05-28、2019-06-27 或 2019-07-02，与正文中的早期移动版 roster / rules 一致；虽然十一页的 `dateModified` 都在 2026-07-30 06:07 左右被同分钟批量刷新，但没有正文证据表明构筑已更新到 2026 规则。因此它们仍只按 2019 historical practice 使用，批量 metadata modification 不建立当前版本性。未找到能同时给出版本、模式、样本框和阵容定义的代表性统计，因此不报告胜率、采用率或当前排名。

## 产品、平台与版本地图

| 节点 | 可用语义 | 不可跨界外推 |
| --- | --- | --- |
| 2019 Dota 2 Workshop 前身 | Drodo 原始模式的历史身份与类型起点 | 不验证独立版的棋子名、装备、数值、赛季或当前运营 |
| 2019 standalone mobile | BlueStacks 的 11 篇完整攻略共同描述早期经济、装备、站位和具名阵容 | 50 Gold 利息、六装备位、旧 Shaman / Knight / Goblin 数值不能当 2025/2026 当前规则 |
| 2020-04 PC Epic EA | 官方确认 PC Early Access 和移动端账号数据互通语境 | 不自动证明后来的 PlayStation / Steam 同服或完全同补丁 |
| 2020-12 至 2021-04 PlayStation | Founder's Pack EA 与专属在线修复均有独立官方节点 | 平台专属修复不能外推为所有平台的共同机制变化 |
| 2022-12 Steam / S20 | Steam 独立版存在；S20 同期引入 Soul Clan / Soul Spring 等规则 | S20 公告本身不证明 Steam 首发的全部平台细节，也不代表 2025 规则 |
| 2025-03 至 2025-05 | Relic、Race/Class blocking、Legendary 隐藏池、Ancestor、Divinity、召唤装备和 XP 曲线等当前时代节点 | 不能用旧官网静态数据库覆盖较新的补丁文本 |
| 2026-03 Steam 维护 | 至少存在一次明确日期的官方匹配维护公告 | 维护不等于活跃平衡、平台统一、人口健康或持续内容开发 |

### 模式不可混同

- **Classic**：官方模式页定义常规八人循环、共享池、自动战斗和升星；本 dossier 的历史阵容均以此类标准对局为语境。
- **Duo**：官方模式页是双人协作变体；队友协作、传递或共同胜负不能反推单人 Classic 的经济与 owner。
- **Quick / Fantasy**：仅记录其存在，不用它们建立普通对局的构筑结论。
- **Workshop mod、移动独立版、Epic、PlayStation、Steam**：平台 / 产品身份分别保留；公开页面可访问不代表所有平台共享一个版本。

## 核心循环与真实决策

1. 八名玩家从共享棋子池的商店购买棋子，部署后自动战斗；一般三个同星棋子合成更高星，种族 / 职业数量激活 synergy。
2. 招募、刷新、升级棋手等级、保留 bench 备件和追三星竞争同一 Gold 预算。2019 攻略给出 50 Gold 利息上限、连胜 / 连败经济与 round 11–18 的转型读盘，但这些精确数字只属于早期移动版。
3. 等级既增加上场人口，也改变高费用棋子的可达性。Goblin/Mech 的低等级追三星与后期升 9/10 找 Devastator 是同一构筑内相反的阶段性投入；找不到终盘件是明确失败原因。
4. synergy 是 supplier / amplifier，不必然拥有最终伤害。Knight 给耐久，Glacier 给攻速，Mage 降魔抗，Goblin/Mech 给生存；Lightblade Knight、Berserker、Tortola Elder、Water Spirit、Devastator 等具体棋子或装备拥有最终事件。
5. 装备附着具体 holder。2019 版本不能随意卸装，只能卖棋子回收；主动 / 被动技能、Mana 需求、攻击频率、范围和职责决定装备合法性。后来 Item Shop / chest / pass-block 规则发生重做，必须标版本。
6. 站位改变 Assassin 跳跃、前排承伤、Mana 获取、AOE 形状、Siren 面向、同名目标扩散、召唤落点和 creep 包处理。阵容名不包含这些空间条件时不是完整构筑。
7. 2025-05 的 Race/Class blocking、10 个随机可用 Legendary（其中 5 个初始隐藏）、首位发现奖励，以及 Item chest 的 pass/block 轮换，把“不可得”从纯随机改成可付费或可观察的池管理。

## 构筑一：Divinity Water Shaman（2020-12 官方实践）

### 闭环

- **engine**：4 Divinity + 4 Shaman + 4 Cave，以 Water Spirit 为单核；Divinity 缩短技能循环，Shaman 提供变形 / 干扰，Cave 提供生存底盘。
- **state/resource**：Water Spirit 星级、Mana、主动技能 cooldown、装备位、50 Gold 经济线、棋手等级与对手核心位置。
- **payoff**：Water Spirit 的技能拥有最终伤害；Pulse Staff、Holy Spirit Lance、Voodoo Staff、Orb of Refresh、Claw Wand、Monkey King Cane 等都集中给它，装备只改变该 holder 的资源、控制、抗性或命中。
- **survival**：Cave 的生命底盘、Claw Wand 的法术防护与 Shaman 干扰为 Water Spirit 买施法次数；耐久不自动变为攻击。
- **spatial condition**：Shaman 站位要侦察敌方核心；对 9 Mage 时用 Grand Herald 复制 Tortola Elder 的技能思路，说明敌方构型会改变摆位与工具位。
- **payoff owner**：Water Spirit 是伤害 owner；Divinity 是 cadence reader；Cave 是 survival supplier；每件装备保留 holder-bound owner。
- **economy/pivot**：round 1–15 不买 XP、存至 50；level 5 刷二费 Water Spirit；round 16–21 升 8；之后先追 Water Spirit 三星再升 9。
- **counter/abort**：Doom Arbiter 会针对最昂贵的单核；9 Mage 需要复制 Tortola Elder 的特定应对。攻略作者明确说阵容并非无敌，因此没有“凑齐标签即必胜”的结论。
- **version context**：只属于 2020-12 规则；2021 Shaman 重做后不可把旧 Hex / 变形语义当成同一机制。

## 构筑二：6 Knights + 4 Glacier（2019 mobile）

### 闭环

- **engine**：六 Knight 构成持续触发的防护层，四 Glacier 将攻速供应扩到全队。
- **state/resource**：Knight / Glacier 阈值、防护触发、攻速、关键棋子星级、Gold、bench 与八至十人口空间。
- **payoff**：Lightblade Knight 的弹射 / 范围普攻、Hell Knight、Berserker 的连续攻击，或 Dragon Knight 的变身输出是具名 owner；Glacier 只供 cadence。
- **survival**：Knight 的物理 / 魔法耐久和 Argali Knight / Evil Knight 的治疗或盾承担前排；它不是“盾值自动造成伤害”。
- **spatial condition**：Knight 前排保护远程 owner；Lightblade / Dragon Knight 的输出形状和 Assassin 后排威胁要求动态调整。
- **payoff owner**：Knight synergy 拥有防护；Glacier synergy 拥有攻速；每个 carry 拥有其攻击 / 技能伤害。
- **economy/pivot**：构筑费用高、bench 紧张；若核心 Knight / Glacier 不来，可横向转 Dragon/Human/Mage，或 Hunter/Egersis/Marine，而不是为六 Knight 牺牲全部功能位。
- **counter/abort**：历史攻略明确指出 DPS 可能不足；Mage / pure damage / 控制或抢占高费件攻击不同链路。
- **version context**：2019 BlueStacks 是规则主体；2023 Steam Guide 只证明这套壳仍被历史玩家整理，不证明其在 2023 更遑论 2025 的排名或精确数值。

## 构筑三：3/6 Mage + Warrior / Spirit / Cave frontline（2019 mobile）

### 闭环

- **engine**：由三 Mage 过渡到六 Mage，前期以 Warrior、Spirit 或 Cave 提供坦度；The Source 供应 Mana。
- **state/resource**：Mage 阈值、敌方魔抗、Mana、技能 cooldown、AOE 范围、前排存活时间、等级与转型 bench。
- **payoff**：Tortola Elder、Thunder Spirit、Shining Dragon 等具体施法者拥有 AOE；Mage 是魔抗 debuff / 放大器。
- **survival**：Warrior / Spirit / Cave 前排和角落保护为脆弱施法者争取首次施法。
- **spatial condition**：Tortola Elder 与 Shining Dragon 依赖角落或受保护的射线 / AOE 位置；敌方 Assassin 到场时需收拢后排、不给跳跃落点。
- **payoff owner**：The Source 供 Mana，Mage 降魔抗，具体 caster 拥有伤害。
- **economy/pivot**：六 Mage 在中期尚未凑齐时很脆；round 11–12 读取已到的高星三 / 四费，round 17–18 决定是否锁定，不应在无 caster / 前排时强追标签。
- **counter/abort**：Marine / 魔抗直接反制；Assassin 切后、Silence / 控制、分散站位或提前爆发都可阻止第一轮技能。
- **version context**：只作为 2019 移动版闭环；2025 Devastator / piece 数值补丁不能拼入这套旧 Mage 数值。

## 构筑四：Goblin/Mech gamble → Devastator + Mage bridge（2019 mobile）

### 闭环

- **engine**：前期三 Goblin + 两 Mech，以低费 Sky Breaker、Ripper 等在低等级 reroll 追三星；随后从 reroll 转为存钱、升级并寻找 Legendary Devastator。
- **state/resource**：低费拷贝、共享池、星级、HP regen / Armor、Gold、round、棋手等级和 Devastator 可达性。
- **payoff**：前期输出来自已升星 Goblin/Mech；终盘 Delayed Action Bomb 由 Devastator 拥有。三 Mage 只放大 Goblin/Mech 技能魔法伤害。
- **survival**：Goblin Armor / regen 与 Mech regen 让低费单位在前中期拖住，但攻略明确指出整体 DPS 不足。
- **spatial condition**：耐久单位前置聚怪，Devastator 需要存活到炸弹生效；对方分散、后排突入或控制会降低 AOE 收益。
- **payoff owner**：Goblin/Mech synergy 拥有生存；Devastator 拥有终盘爆发；Mage 拥有放大。
- **economy/pivot**：低等级刷卡只持续到大约 round 17；之后必须转存钱并升 9/10。还可接 3 Mage，或转 3 Assassin + 3 Dragon。
- **counter/abort**：Devastator 不来是明确失败 / 退出信号；继续低级刷会同时错过人口和 Legendary odds。高魔抗、控制与对方更快的后排输出也能破链。
- **version context**：2019 攻略提供完整经济时序；2023 Steam Guide 仅交叉验证“低费易三星但 Legendary 难找”的历史体验。

## 补充线一：6 Assassin + Feathered / Druid

Assassin 通过跳后排与暴击绕过前排；Feathered / Druid 或 Goblin / Mech 提供闪避、快速升星或早期生存。Phantom Queen 可故意放在坦克后方而不跳，使 Scream AOE 命中更多目标；level 8 主动花钱形成中期压制。历史攻略警告 round 25 后衰减，Armor、Knight、Warrior 与后排收拢会反制，因此终盘可减少 Assassin，补控制和高费棋子。这是空间 / 时机体系，不是单纯“六个同标签”。

## 补充线二：Druid 快速升星 → 横向转型

Druid 的历史规则不直接给战斗 buff，而是把升级所需拷贝从九份降为四份；Whisper Seer 与 Razorclaw 供应召唤。它用于早期快速三星，之后转 Beast/Warrior、Hunter/Egersis 或 Feathered。Druid 是升级经济 engine；召唤物和转入的 carry 各自拥有伤害。若把 Druid 当终盘战斗阈值而不转型，构筑会缺稳定输出与控制。

## 盾、防御、元素与显式转换

- **Knight shield / mitigation 与元素不是一层**：旧官网 Knight 静态页仍写早期 2 秒护盾与旧数值；2025-03 补丁将 Knight 描述为 60% 物理 / 魔法减伤加 HP Regain，且不减 pure。较新补丁优先，旧数据库仅作漂移证据。
- **盾破伤害必须具名**：S13 的 Evil Knight 在 shield 破裂时造成范围伤害；伤害来自明确的 break trigger，而不是所有盾默认爆炸。
- **元素 / 状态另走一条路**：Mage / Marine 是魔法放大与抗性；Demon / pure、Silence、Fear、Petrify、attack-speed reduction 与 Martialist 的 pure DOT 有不同状态和 counter。`Icearmor` 的名字也不等于通用“冰盾”规则。
- **团队统计定向供应有真实先例，但规则本体不是单 recipient**：S20 Soul Clan[2] 将两个格子变为 Soul Spring；放在任一 Soul Spring 上的棋子都获得场上存活 Soul Clan 总 Max HP 与 ATK 的 18%，因此最多两个 occupants 可同时获益。玩家可以策略性地把其中一格留给主 carry，但成员死亡时其对应 bonus share 会动态失效。它不是“任意全队 Defense 永久转 Attack”。
- **治疗转输出是 ally-local reader，不是已证实的团队总账本**：2025 Ancestor[4] 将效果赋予所有 allies；当某名 ally 的 `total healing received` 超过 100 时，对该 ally 附近敌人造成等于该值的 pure damage，单次 cap 300，并写有 5 秒 CD。原文未说明 healing received 如何累计 / 重置，也未说明 5 秒 CD 是 team-global 还是 per-unit，因此只保留“各 ally 本地读取→自身附近伤害”的确定语义。
- **项目启示仍非方案授权**：冰盾与土盾可以共享 shield acquire / hit / break / expire 生命周期，但 affinity、状态反应、抗性、converter、recipient 与最终伤害 owner 必须另行设计。

## 召唤、死亡、复制与占位守卫

- Insectoid 只读取重复的非 Insectoid ally；其中一个死亡时，以仍存活重复体中的最高 cost 决定随机虫召唤。duplicate、death、survivor read 与 summon owner 是分开的。
- 2021 Shaman 重做将原先近乎无反制、随机失控的 Hex 改为死亡后变成 cost +1/+2 随机单位；有 2.5 秒延迟，只能由死亡阻断，不继承 item / synergy，但继承当前 HP 百分比。Doom 按变形后的最高 cost 选目标。
- Cave Prodigy 从本回合 graveyard 召回最高 sale-cost 死者，星级随施法者，不带 item、不受 synergy；八个相邻格满或 graveyard 空时不施放。召回单位再死可再次入账，同名多尸体分别可用。
- Civet 三星复制与同名 piece 成对生成 Golem 的规则说明 same-name counting 要在 battle start 结算；不是普通共享池复制。S20 Siren 还按面向 / 视线石化，强调棋盘几何不是纯表现。
- 2025 Momora's Nest 明确受共享池剩余实际数量约束；数量不足则 nest level 不升。复制、升星与池扣减不能各自假装资源无限。
- War Horn 只增强 holder 的 summon 攻速与持续时间；召唤体、召唤 holder、synergy amplifier 和 player-damage / pool eligibility 不应混成一个 owner。

## 装备、遗物与机会成本

- 2019 装备通过 creep rounds 掉落并可合成；当时装备不可直接卸下，卖掉 holder 才回收。Frantic Mask 给无主动技能的 Ranger / Berserker 有价值，却会让需要施法的 Shaman 失效。
- Magicka Crystal / Orb of Regen / Orb of Refresh 改 Mana 与 cooldown；Blade Mail 在 Redaxe Chief taunt 上形成承伤→反伤闭环；Cappa / Voodoo Staff 通过降魔抗放大 Phantom Queen 等 caster。装备改变的是具体 holder 的链路。
- 2020 Item mechanism 将掉落、Item Shop 轮次 / 品质和 creep 敌方包显式化；狼群前后排反转、Wildwing 左侧、Glacier AOE 聚拢等要求玩家为 PVE 轮换站位。
- 2025 Relic 可以重写 encounter、Beast 夜行或 Chaos Contract；Race/Class blocking 和 Item chest pass/block 都有 Gold / 候选机会成本。Relic 是规则重写层，不应无条件叠在所有 build 上。
- Demon Gloves 把 holder 的所有伤害转为 pure；Twin Fangs 把 holder 伤害扩散到最多两个同名敌人。这些都是明确 holder / target selector，不是全队免费获得。

## 生命周期与负面 / 重做案例

1. **产品分叉**：Workshop 前身、独立移动、Epic PC、PlayStation 与 Steam 不能只因同名就共用版本事实。
2. **早期无教程反馈**：2019 实践来源明确记录新手难以理解流程；后续官方补充 Game Goal / Walkthrough，但不据此声称问题完全解决。
3. **装备锁定**：旧版不能卸装，错误 holder 可能直接废掉主动技能单位；后续 Item Shop / chest 机制已重做。
4. **Knight 文档漂移**：官网静态 Knight 页的旧 shield 数值与 2025 补丁不一致；不能把数据库更新时间当规则权威顺序。
5. **Shaman 无反制重做**：官方明确指出旧 Shaman[4] 针对单核近乎无反制且随机失控，改成 death→transform 并加入延迟 / 继承边界。
6. **变形继承限制**：新 Shaman 不继承 item / synergy，只继承 HP 百分比；避免随机生成单位携带整套隐式规则。
7. **Cave Prodigy 空状态**：无 graveyard 或无相邻空格则不施放；失败原因可解释而非静默生成。
8. **Martialist 非致死 DOT**：非 pure 即时伤害的一部分改为 5 秒 pure DOT，且 DOT 不致死；延迟伤害必须记录 lethal policy。
9. **Egersis 死后状态限制**：延命期间不可被普攻 / 友方技能选取，也不能施法；“仍在场”不等于完整行动资格。
10. **Kira's Wrath 自损**：item 给 holder Kira synergy，但每秒承受 3% Max HP pure damage且不致死；tag bridge 有明确债务与 lethal guard。
11. **Night Demon rate limit**：Fear 有 6 秒内置 CD，防止高频邻近判定连续锁死。
12. **传奇池 / Extended Pool 重做**：2025 移除 Extended Chess Pool，改为 Gold blocking、每局 10 Legendary、5 个隐藏与首次发现奖励；旧 shop 假设失效。
13. **召唤受池约束**：Momora's Nest 数量不足不升级；不能用复制 / 遗物绕过有限供给。
14. **运营因果边界**：2026 有维护节点，但没有版本统一、活跃人口或持续大更新的充分证据；不从单次维护推导健康度。

## 对本项目的可迁移结论

- **先定义体系轴，再填素材**：Knight、Glacier、Mage、Goblin/Mech、Shaman、Ancestor 分别展示 survival、cadence、amplification、economy、death rewrite 与 healing converter。它们能组合，但不应先把所有名为盾或元素的素材塞进同一套。
- **盾体系需要独立输出 owner**：可以是 protected carry、shield-break event、retaliation、ally-local healing-received reader 或明确的 team-stat converter。每种都必须写 supplier、reader、recipient、read time、cap、refresh、slot cost 与 counter。
- **元素体系不是换皮盾**：damage school、affinity tag、status、reaction、resistance、cleanse 与 DOT lethal policy 应分层；冰盾 / 土盾只在 shield 生命周期上共享。
- **构筑必须含经济时序**：Water Shaman 的 level-5 roll、Goblin/Mech 的低级 reroll→升 9/10、Knight 的高费 / bench 压力与 Mage 的中期脆弱，说明“什么时候停止追”与终盘棋子同等重要。
- **规则重写要占机会成本**：Hat / item、Relic、blocking、指定格与隐藏 Legendary 都是桥接工具，但应占 holder、格位、Gold、候选或 relic slot；否则纵向体系失去取舍。
- **报告必须能定位断链**：缺 supplier、reader、Mana、终盘件、合法格、holder、目标形状、共享池数量、站位或 rate-limit 都应产生不同失败解释。

## 不可直接迁移的假设

- 不把 2019 的 50 Gold、旧 Knight / Shaman、六装备位或 creep 表当当前规则。
- 不把 2023 Steam Guide 的阵容描述当当前 meta 或代表性统计。
- 不把 Soul Clan 18% 复制为项目数值；它只证明“具名供应集合→两个指定格、最多两个 recipients→死亡动态失效”的结构可行。把其中一格交给主 carry 是玩家策略，不是规则天生限制为单核。
- 不把 Ancestor 治疗转伤理解为所有治疗都自然造成伤害或共享一个团队累计池；确定语义是效果赋予每名 ally、该 ally 的 `total healing received` 超过 100、伤害作用于其附近敌人、单次 cap 300 与 5 秒 CD。累计 / 重置细节和 CD owner 未明确，项目必须另行定义。
- 不因 `Icearmor`、Glacier 或 Knight 同时出现就假设游戏存在统一冰盾 / 元素盾体系。
- 不把官方维护、可访问页面或 2026 piece 数据解释为所有平台同版、活跃人数健康或阵容仍最优。

## 置信度、缺口与停止理由

- **高置信**：官方补丁中明确的 Soul Clan、Ancestor、Shaman、Cave Prodigy、Civet、Martialist、blocking、rate-limit 与 holder 规则；2019 / 2020 攻略在各自历史版本内的具名构筑与时序。
- **中置信**：2023 Steam Guide 仅作历史交叉验证；BlueStacks 是单一出版方的系列实践，不代表全体玩家共识。十一页在 2026-07-30 同分钟批量更新的 `dateModified` 只视为页面 metadata 维护，不覆盖 2019 `datePublished`、正文 roster / rules 或本研究的 historical-practice 边界。
- **缺口**：没有当前英语完整阵容攻略、版本化代表统计或 PlayStation 当前状态官方说明；多数 2024–2025 官方攻略为图片，不能从不可读图片补全文本。
- **搜索范围**：筛查 Dragonest `announcement` 155 条、`news` 143 条、84 个 piece、22 个 race、12 个 class、56 个 equipment、4 个 mode 和 3 个 game-introduction 路由；另查 Steam News、Steam Guide、BlueStacks 系列、平台页、Fandom、搜索引擎与历史前身资料。
- **不可访问 / 淘汰**：Fandom 只有 17 页且内容薄；搜索结果、商店页、图片-only 攻略、无字幕视频、转载镜像和已移除页面的空壳不承担机制 claim。Steam Guide 2974734188 的完整正文仍可读，但 removed/incompatible 状态与 2025 评论均使其只能作历史材料。
- **停止理由**：新增检索已主要重复 2019 基础构筑或提供无正文的图片；现有 38 页已闭合四套主构筑、经济 / 站位 / 装备、盾与元素边界、三种显式 defense/heal/team-stat→output 桥、召唤 / 死亡 / 池守卫及十四类生命周期问题。继续堆叠页面不再改变 owner、pivot、counter 或版本理解。

## 最终处置

- `disposition`: `anchor-retained`
- 理由：Auto Chess 提供自走棋原型、独立版长期演化、完整历史构筑和当前时代官方重做的组合证据；尤其能直接回答“盾体系如何输出、元素体系是否独立、团队属性如何通过双指定格定向供应（并可策略性集中主力）、装备 / 遗物如何做 bridge”而不需要发明素材。
- Cross-title synthesis 仍为 `Withheld`；本 dossier 是探索证据，不授权第一版英雄、装备、遗物、元素、数值或实现方案。
