# Slotbound

## 身份、时期与资料密度

- `title_id`: `slotbound`
- 精确对象：Steam 主 App `4459590`，开发 / 发行均为 Optima Arch；单人 Roguelite / Auto-Battler / Defense。
- 主 App 在 2026-09-03 仍为 `Coming soon`，公开发行窗口是 `November 2026`。独立 Demo App `4906570` 于 2026-07-10 发行，不能把 Demo 评测当正式版强度。
- 可见版本链：Demo 首发、`0.2.0`、`0.2.5`、`0.2.7`、`0.3.0`、`0.3.1`、`0.3.2`、`0.3.3`、访问日最新 `0.3.4`（2026-08-30）。
- 与同名 GitHub 项目、玩家脚本、`Lootbound`、`Backpack Battles` 及一般 slot roguelike 通过 App id、开发者和 Demo 关系消歧。
- 本 checkpoint：25 个实质非商店来源——1 个官方 Demo 节点、8 个官方补丁、1 个独立详细试玩、10 条详细 Steam 长评、4 个规则 / bug / 经济讨论、1 个带具体构筑时间戳的公开视频 description。
- 置信：中高。3×3 payline、nudge、spin 经济、吸收 / 晋升、五个实践结构、三层几何、敌人反制和 0.2→0.3.4 lifecycle 已闭合；但没有 manual、wiki、数据库、统计或正式版规则，因此是 `retained` 而非 anchor。

## Adaptive-depth 决定

Slotbound 的区别不只在“用老虎机代替商店”。一次招募同时经过三层约束：3×3 图案能否形成 payline、有限 roster 是否愿意通过吸收压缩、生成单位在战场前后位置与路径上能否履职。其随机结果随后被 Core、nudge、item、Imprint、class promotion、局内 / 局外经济再次改写。

因此本轮闭合五个具名或可复核结构，并追到 Core 几乎全量重做、准备计时移除、战场扩容、吸收锁、存档、伤害可读性与当前 0.3.4。25 个来源以后，新结果主要重复“太随机 / 很上头”、未转录视频或尚未公开的正式版内容；继续枚举全部单位、Core 和 item 不再改变 owner、pivot、counter 或 lifecycle 判断，按 diminishing-return test 停止。

## 来源包

- `src-slot-demo-launch-2026-07-10`：Demo 身份与基础 spin→summon→absorb→wave 循环。
- `src-slot-patch-0-2-0`、`src-slot-patch-0-2-5`、`src-slot-patch-0-2-7`：准备计时移除、战场 / trait 重做、Core capacity / activation 问题与重建动机。
- `src-slot-patch-0-3-0`、`src-slot-patch-0-3-1`、`src-slot-patch-0-3-2`、`src-slot-patch-0-3-3`、`src-slot-patch-0-3-4`：Core overhaul、吸收锁、中途存档、生成位置、难度 / Engraving、稳定性与伤害可读性链。
- `src-slot-review-todaywegame-2026-07-17`：payline、nudge、spin / kill 经济、五人口、level-31 Warrior 前墙和第二地图后排猎杀。
- `src-slot-review-core-economy-2026-07-26`：Core active/passive、discard、双货币、trait inheritance、promotion 与 meta tree。
- `src-slot-review-mage-cavalry-2026-07-17`、`src-slot-review-mono-mage-2026-07-17`：Mage healer / summon 与 Cavalry pathing 的早期实践。
- `src-slot-review-deadeye-2026-07-16`、`src-slot-review-cavalry-2026-08-15`：Deadeye 前后排和全 Cavalry 两条版本化路线。
- `src-slot-review-imprint-tradeoff-2026-08-07`、`src-slot-review-current-shop-2026-09-01`：高阶单位 / Engraving 牺牲、前后排 trait 组合、当前 shop / rarity / absorb 时机。
- `src-slot-review-core-critique-2026-07-14`、`src-slot-review-absorb-ui-2026-07-16`、`src-slot-review-item-evolution-2026-07-12`：重做前 Core identity、吸收 / nudge 输入风险和 evolution 后 item eligibility 断裂。
- `src-slot-discussion-population-exception-2026-09-02`、`src-slot-discussion-base-class-2026-09-01`、`src-slot-discussion-golden-relic-2026-08-31`、`src-slot-discussion-shop-rng-2026-08-31`：人口例外、class lineage、rarity cap / nudge bug 与当前经济 / dead offer 观察。
- `src-slot-video-single-unit-description-2026-08-30`：六段文字时间戳的单单位 Lifesteal / Transfer / Buildup challenge；不使用未转录画面。

## 真实循环与三层几何

每波准备期先拉动 3×3 slot。横、竖或斜线形成三个相同 symbol 时召唤对应单位；每波有一次 nudge 用来把接近命中的结果推向一条 win line。玩家继续 spin、选择 Core、购买 item、移动单位、吸收材料和决定晋升分支，然后开始自动战斗。敌人越过防线会扣 hearts；失败两次会结束 run。Boss / Judgment wave 给局外成长资源，完成 run 后继续购买永久节点。

这套系统有三层互相连接、但不能混成一种“格子”：

1. **结果线几何**：3×3 位置只决定横 / 竖 / 斜 payline 和 nudge 是否能救回一次结果。它不是装备背包，也不持续占用战场格。
2. **Roster 容量几何**：独立试玩从五个上阵位开始；多余单位必须被保留、吸收或替换。当前社区还观察到某 trait 可不计 population，但没有正式规则表，只保留为低置信例外。
3. **战场几何**：单位出生 / 玩家摆放形成前后排与接敌路线。Cavalry 会快速前冲，Deadeye 依赖后排输出，第二地图有绕过前墙去打最后方单位的敌人；0.2.5 还修复 knockback 把单位推出战场。

因此“老虎机出了什么”只是 supply；真正构筑要同时回答 line 命中概率、谁占人口、谁被牺牲、谁在前排承伤、谁被后排猎杀和谁拥有最终 damage。

## Spin、nudge 与三套经济

- **战斗招募货币**：同一波连续 spin 的成本逐次增加；击杀敌人回金，决定下一波还能拉几次。一次 nudge 是低频纠偏，不是指定单位，也不能保证每波都有有效移动。
- **shop / Core 决策货币**：0.3.0 长评区分 slot spin 货币与另一种 shop currency；后者同时支付 item、shop reroll 和 slot-modifier / Core reroll。shop reroll 成本会上升，又随波次推进回落，形成“现在找件”与“等自动降价”的窗口。
- **局外资源**：永久树购买 hearts、army size、reroll、advanced rarity、interest / supply 等。它让高难度更可达，却也会把开局 RNG 问题延迟到多次失败后的数值解锁。

一条 0.3.2 满树玩家路线会留 500 取得 50 interest；没有 interest 时，等常规拉兵成本到 200 后使用 `出征补给` 以 200 直接买兵。它证明局外节点可以成为招募兜底，但不代表当前所有玩家共享该公式。

早期 0.2.0 玩家反复报告前几波连续空转和重开；0.3.0 后玩家认为 Core / modifier 能显著纠偏，也有人在 0.3.4 仍认为整局可抽不到足够兵。这里没有“RNG 已解决”的共识，只有可审计的控制层：nudge、Core、reroll、永久 rarity / supply 和一次最终保底单位。

## Absorb、Imprint 与 promotion 的所有权

吸收不是普通三合一。实践来源描述：

- 牺牲单位决定可继承的 Imprint / passive，例如 Pierce、Bash、Poison、AOE、multi-strike、ramping attack speed、stun、blind、knockback 或额外资源。
- survivor 接收成长点；点数受材料 rarity / type 等影响。达到阈值后 survivor 晋升，并选择 class branch，进而改变自动 active skill、stat growth 与 defensive / balanced / offensive role。
- 高 rarity 单位可容纳更多 passive。随机 trait 还可能使材料在被吸收时给 currency / experience，或让单位不计 population。
- 因此高阶单位有两个互斥价值：留下成为独立战斗 owner，或牺牲取得高阶 Engraving / trait。0.3.2 玩家明确把它视作当前 roster 选择，而不是自动吞掉低战力单位。
- 0.3.0 加入 manual lock，防止选中单位被当作吸收材料；这让 irreversible sacrifice 成为正式交互边界。

owner 必须分开：材料拥有可转移 trait，survivor 拥有继承后的被动 / 等级 / class，Core 只拥有招募或规则改写，item 拥有装备效果，局外树拥有 unlock cap。Golden-symbol relic 的 +1 rarity 仍受“已解锁 rarity”限制，证明 run 内 jackpot 不能越过 meta authority。

## 构筑一：level-31 Warrior 前墙与后排输出

TodayWeGame 的 0.2.x 完整试玩把一个 Warrior 叠到约 level 31，令其成为高 Health / Defense 的前墙，其他单位在后排输出。shop item 让“第一个被攻击的单位”七秒不受伤，进一步稳定初次集火。

- **engine**：反复吸收材料，把 growth / passive 集中到一个 Warrior。
- **state/resource**：Warrior level、Health / Defense、五个初始上阵位、材料单位和 spin / shop 货币。
- **payoff**：后排单位拥有 damage；Warrior 拥有承伤和集中成长，不被误称为全队 carry。
- **survival**：前墙 stats + 首个被攻击者短时免伤。
- **spatial condition**：Warrior 先接敌，damage owners 在其后。
- **economy / pivot**：少留几个平均单位，持续把材料喂给前墙；若拿不到后排伤害则单纯堆防御会超时 / 漏怪。
- **counter**：第二地图出现直接攻击最后方单位的敌人，能绕过这个单点防线；必须换后排位置、增加第二承伤者或牺牲部分集中成长。

这条结构直接回应“盾 / 防御体系怎么输出”：前墙并未把 Defense 自动转伤害，而是用生存时间授权后排 damage owner。需要另一个明确 converter 才能把防御数值交给单核。

## 构筑二：common healer + uncommon summon 的 mono Mage

同一 0.2.7 前后，两份独立长评都报告 Mage 路线强势。一份把全 Mage 描述为能清 Demo，二阶 Mage 的 summon 解决前排；另一份指出 common Mage 可晋升 healer，而 uncommon Mage 的可见分支是 AOE 或 tank summon，故 mono route 反而需要保留一个低 rarity healer。

- **engine**：定向 Mage symbol / Core 后持续获得同 type 材料并吸收晋升。
- **state/resource**：common healer、uncommon AOE / summon、rarity、passive slots 与 Mage-specific item。
- **payoff**：后排 AOE Mage 拥有伤害；summoner 拥有临时 tank；healer 拥有 sustain。
- **survival**：召唤物接敌，common healer 维持 tanks；不能只用高 rarity 替换低 rarity。
- **spatial condition**：召唤 / tank 在前，healer 与 AOE 在后；单位槽需同时容纳三种功能。
- **pivot**：若只来一个 Mage，可将其作为 healer support 配 Warrior / Spearman；只有有足够 Mage 与定向 supply 时才转 mono。
- **counter / limit**：后排猎杀绕过召唤墙；版本来源早于 0.3.0 Core 重做，当前强度未知。

这是一条 rarity 与 role 正交的实例：更稀有不是同一路线严格上位；低阶 healer 是 function bridge。

## 构筑三：4–5 前排 + 3–4 后排 Deadeye

约 20 小时的日文滚动评测在 Stage 1 Hard 总结稳定线：4–5 名前排、3–4 名后排，后排固定包含 `Scout → Ranger → Deadeye`。Deadeye 是该作者显著最高 DPS owner；选择 slot-adjustment relic 后，开局 1–3 波坏运导致 7–9 波团灭的概率降低。

- **engine**：前排数量换取 Deadeye 持续输出时间，slot-adjust relic 提高可达性。
- **state/resource**：7–9 个 roster slots、Scout lineage、Deadeye level / passive、前排 Health 与 slot controls。
- **payoff**：Deadeye 拥有单核后排 damage。
- **survival**：多前排分摊接敌；不是一个 tank 独占所有材料。
- **spatial condition**：前后排比例和最后方位置决定是否被第二地图 enemy 优先选中。
- **economy / pivot**：早期先拿能活的前排，Deadeye lineage 到达后才把 item / absorption 集中给它。
- **counter / limit**：后排猎杀、坏开局与滚动版本混合；无当前 0.3.4 DPS 数据。

## 构筑四：全 Cavalry charge / crit / stun

0.3.2 满树玩家在 Stage 1 Hard 先验证“全后排太脆”，再尝试全 Cavalry。Cavalry 的快速 charge / high crit 形成开场爆发，晋升后带 stun 的分支与另一分支各占约一半，令 Boss 在反击前被压制。

- **engine**：满树人口 / supply + 定向培养 Cavalry，先把一个 carry 升满而非平均分配。
- **state/resource**：Cavalry 数量、两个 class branches、crit / charge、stun、population 与 interest / supply。
- **payoff**：多名 Cavalry 各自拥有 burst；stun branch 拥有控制。
- **survival**：通过先手 burst / stun 减少受击，而不是传统 healer / Armor。
- **spatial condition**：全前排同时冲锋，快速压缩接敌距离。
- **economy / pivot**：保留 500 interest 或用 200 supply 补兵；优先培养一个核心，再横向补 stun / damage 分支。
- **counter / disagreement**：同阶段另一长评认为 Cavalry 会脱离队伍、冲进敌群被包围。这不是谁“正确”的统计问题，而是 enemy composition、数量、meta tree 与路径条件的差异；项目必须预览冲锋落点和脱队风险。

## 构筑五：前排锁血 + 后排狂暴与单核模块

0.3.2 玩家把 `前排 + 锁血 / 无敌` 与 `后排 + 狂暴（增伤也更易受伤）` 作为最容易判断的 Imprint 组合：survival trait 给接敌者，risk/reward damage trait 给受保护后排。这是 role-to-trait ownership，而不是把随机特性塞给当前最高 rarity。

另一个 0.3.3–0.3.4 边界的视频 description 给出六段可定位 challenge：General + level-1 Lifesteal、Arch-Shaman + level-3 Lifesteal、Bagg + max Buildup / Transfer、Deadeye + Transfer / Lifesteal、Elf Sage + max Transfer / Lifesteal 2、Deadeye + max Buildup / Transfer。它只证明 Lifesteal / Transfer / Buildup 能集中到一个 owner 并参与单单位挑战；没有字幕，故不从画面补写公式、胜率、站位或敌人。

这两类结构共同说明：吸收系统真正的 build 是“谁继承什么”，不是 unit count 本身。若所有 Imprint 都能无成本搬到单核，population 会坍缩；需要 passive slot、材料 rarity、不可逆 sacrifice 和 enemy targeting 同时制约。

## Item、Core 与演化后的失效风险

Core 同时存在 active / passive，能改变 slot odds、添加 joker / golden symbol、给首 spin 未命中时免费 reroll，或改变单位 / 战斗规则。0.3.0 玩家可以 discard Core，说明 Core slot / offer 不是永久堆叠；但完整 capacity 与 replacement 规则未公开。

早期一条波兰语长评指出，shop item 按 Warrior、Spearman、Duelist 等具体 class 生效，而不是广义 melee / ranged / magic；当单位 evolution 后 class id 改变，旧 item 可能失效。0.3.4 讨论仍有人看不清 Spearman / Soldier 进化后的基础 lineage，只能依赖 tooltip icon。

这会产生三种 dead offer：当前 roster 没有目标 class、目标已进化出 eligibility、目标 rarity 尚未由 meta tree 解锁。Golden relic 又证明 jackpot 输出受 meta unlock cap；因此 shop、Core 和 evolution UI 都要显示 target predicate、当前 eligible owners、进化后保留 / 失去和未解锁原因。

当前 0.3.4 长评进一步报告 common +5% Attack/HP item 因便宜、常见、易 max 反而优于专门 item，甚至无 shop run 也没有明显差距；Mage / Champion 稀有且吸收高阶单位通常要到 round 20+ 队伍已定型才合理。没有统计，不能称为全局 meta，却说明随机 item、unit rarity 与 absorb window 可能在时间上错位。

## 敌人、失败解释与适应窗口

- 第一地图可用前墙 + 后排解决；第二地图加入无视前排、攻击最后方单位的敌人，迫使重新摆位或增加冗余承伤。
- Cavalry 的快速接敌既能用 burst / stun 压 Boss，也可能前冲后被包围；预览 enemy formation 和 charge destination 才能把冲锋从随机优劣变成决策。
- 玩家指出失败 wave 后 round counter 仍推进，敌人继续增强；若 spin/shop 又没提供补强，第二条命的适应窗口可能比第一次更窄。
- Boss / Judgment 给局外资源，但完整 telegraph、skill、targeting、damage type 和 anti-heal / anti-control 包未公开。不能从视频标题拼装 counter table。
- 结果伤害在 0.3.2 修复，0.3.4 又改为战中实时更新。它能回答“谁打了多少”的一部分，却仍缺 absorb lineage、Core proc、summon owner、blocked / overkill 和 enemy target reason。

本项目可迁移“分层 enemy exam”：先用绕后怪检验单前墙，再用冲锋惩罚 / zone 检验脱队，最后才组合 Boss。完全不可预览的后排猎杀只会把摆位变成事后知识。

## 0.2→0.3.4 生命周期

本 checkpoint 计九个 materially distinct negative / reworked families：

1. **准备计时与决策密度冲突**：首发玩家来不及读随机 trait / 摆位；0.2.0 直接移除 timer，让 PvE 规划不限时。
2. **战场容量 / 边界不稳**：0.2.5 扩大战场并修复 knockback 把单位推出区域；0.3.0 又改善自然 spawn，说明位置系统需要明确合法边界。
3. **随机 trait options 缺少清晰选择**：0.2.0 宣布完全重做，0.2.5 实装 major overhaul；不是一次数值 nerf。
4. **Core identity 与 activation 失败**：早期玩家称 Core 对 slot 改变不足；0.2.7 修 activation 并宣布重建，0.3.0 几乎全部重做 / 替换，0.3.1 再修 activation。
5. **不可逆吸收 / Core 选择缺保护**：超过八个 Core 令选择页无响应；拖拽吸收易误操作；0.3.0 加 manual lock，分别修 capacity 与 sacrifice safety。
6. **存档 / freeze / performance 破坏 run**：0.2.5 修无法建存档、黑屏 / crash / blue screen，0.3.0 加 mid-run save，0.3.1–0.3.3 连续修 frame drop、memory leak、black screen、freeze / FPS。作为一个 run-integrity 家族，不逐条灌水。
7. **伤害结果不可验证**：0.3.2 修结算伤害不显示，0.3.4 改战中实时更新；显示时点是 attribution 契约的一部分。
8. **早期空转与局外强制感**：多个早期来源报告连续数波无单位、开局重开或先刷 meta tree 才能稳定；Core / rarity / supply / interest 提供缓解但未形成当前统计共识。
9. **item / class / rarity / absorb 时间错位**：早期 class-specific item 随 evolution 失效，当前玩家又报告 dead class offers、常见小 stat item 优于专门件和高阶单位直到 late game 才适合牺牲；尚无官方完整修复。

另有 defeat transition、Credits、audio device、battle auto-start、Engraving 数值和一般 stage balance 修复。它们保留在来源，但不分别计数，避免把同一 UI / performance / tuning 链灌水。Cumulative explicit negative/reworked cases：145。

## 对本项目可迁移

- **把三种空间拆开显示**：招募结果线、roster slots、battlefield positions 分别回答“能否得到”“能否带上”“能否履职”；不要用一个笼统的“格子系统”隐藏三种成本。
- **随机必须有分层控制**：nudge 纠正一次近命中，Core 改分布 / 规则，direct supply 兜底，meta unlock 改长期上限；每层要显示它不能解决什么。
- **吸收是所有权迁移**：材料 trait、survivor passive slots、promotion branch、item eligibility 和 source lineage 必须在确认前预览；manual lock 是最低安全线。
- **低 rarity 可拥有稀缺功能**：common Mage healer 不应被高 rarity 自动淘汰。桥接单位 / 装备的功能和 rarity 是正交轴。
- **防御支持输出不等于防御转伤害**：Warrior 前墙用生存时间授权 Deadeye / Mage；只有明确 relic / equipment reader 才能把 team Health / Defense 交给一个 shooter。
- **单核需要人口与敌人双重反制**：passive slots、不可逆材料、backline hunter、control / burst 和多目标压力共同限制“把一切喂给一人”。
- **冲锋要显示落点**：Cavalry 的相反实践说明 path / target / spacing 是体系组成，不是动画细节。
- **局内 run 不应靠失败刷树才开始**：局外成长可以开新选择和保险，但基础难度需用初始 supply / pity / preview 自洽。
- **报告要解释 chain**：slot result→spawn→absorb source→passive→promotion→Core proc→damage / survival owner，才能让玩家知道 build 为什么成功。

不可直接迁移：3×3 slot 皮肤、一次 nudge、五个初始 slots、两条命、500/50 interest、200 `出征补给`、具体 Warrior / Mage / Cavalry / Deadeye 进化树、Golden symbol、November 2026 内容承诺或任何 Demo 数值。

## 未决问题

- 当前 0.3.4 每种 symbol / payline / diagonal / multi-line jackpot / nudge 的精确结算顺序、权重和 pity。
- 全部 Core capacity、active/passive phase、discard / replacement、互斥与生成事件 recursion 规则。
- 所有 unit rarity、base class、promotion branches、Imprint / Engraving slots、材料成长点和 evolution 后 item eligibility。
- population 的当前 cap、越界处理、不计人口 trait 与 bench / reserve 是否存在。
- battlefield grid、生成位置、target / aggro / charge / knockback、summon occupancy 和 backline hunter 的完整 preview。
- 三种货币、spin cost、kill gold、shop reroll 回落、interest / supply 与局外树的当前公式。
- 失败后 wave 继续推进是否为正式规则，第二条命如何获得有效适应窗口。
- 0.3.4 之后 Golden nudge bug 是否修复；damage view 是否包含 summon / trait / Core / mitigation source。
- 主游戏 2026 年 11 月是否按期发行、Demo 存档 / meta 是否继承、正式版规则与当前五个结构保留多少。
- 没有公开 build adoption、unit/item/Core win rate、difficulty 分层或 enemy matchup 统计；社区强弱冲突不能升级成结论。

## Disposition

`retained`

公开 Demo 和 25-source 包显著超过规则＋独立实践门槛。它闭合了区别度很高的 outcome-line / roster-capacity / battlefield-position 三层几何、吸收式 trait / promotion ownership、三套经济、四条完整 party / formation 路线和一组单核模块，并有从 0.2 到 0.3.4 的连续重做 / 失败证据。它仍不是 anchor：主游戏尚未发行，全部规则快速变动，缺 manual / database / statistics，且公开视频字幕不可读。
