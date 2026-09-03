# Tales & Tactics

## 身份与研究时期

- `title_id`: `tales-and-tactics`
- 类型：单人 roguelite PvE autobattler；玩家在战斗之间处理单位商店、合并升级、装备、Level Perk、角色规则、事件与站位，战斗自动结算。另有 PvP 与 2.0 Knockout，但只作版本/失败案例对照。
- 发行状态：2023-08-10 Early Access；2024-08-15 发布 1.0；本档案分别观察 1.0 前夕、1.0.28–1.0.61、1.2、1.4/社区标记 1.4.3，以及 2026-08 的 2.0/2.0.14。
- 版本边界：2024 天梯 15 四龙攻略属于早期 1.0；2025 Brutal 5 五人队属于 1.2 后；韩文单位/trait 指南明确标 1.4.3；Set 2 单核、Boss 和难度问题属于 2.0–2.0.14。它们不被合并成一套同时存在的当前规则。
- 直接游玩：本批未购买或安装游戏；结论来自可读官方公告、Steam Guide、Steam Discussion 与带开发者标记的回复。

## 检索日志

1. 通过 Steam 官方 news API 读取 1.0、Road to 1.0 Week 5/7、1.0.28、1.0.40P、1.2、1.4、2.0 与 2.0.14 正文，并逐一解析、验证最终公告 URL。
2. 读取 2024 中文天梯 15 攻略，核验开局两只 1-cost Dragon、两枚无等级限制 Clover、两只 3-cost Dragon、四龙、Noble、装备方向与 Clone Capsule。
3. 读取 Ousters 的 1.4.3 韩文 trait 和 2-cost unit 指南，核验 Dragon roster/断点/火焰条件、Mythic 邻接、Engineer tier scaling、Aquatic/Chaser 空间，以及 Fritz/Flash/Deshret 的职责和属性投资。
4. 读取 2025 Brutal 5 完整队伍、rarity replacement 与职责优先构筑讨论，核验五名单位、combatant 规则、治疗/副坦/多目标职责、tier/装备/trait 的机会成本和 banner bridge。
5. 读取 2026 Set 2 四星三费单核、Army Size/卸装、Boss 难度、Zolton 与 2024 高难/rare-rush/Spirit Ring 讨论，分别与 2.0、2.0.14、1.4、1.0.40P、1.0.28 官方记录交叉。
6. 没有公开 patch-level 胜率数据库；官方 1.4 也说明 Metrics 样本已不足。所有作者自报成功、强弱和 “auto-loss” 均保留为单例或社区判断。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-tnt-official-1-0` | [Tales & Tactics 1.0 OUT NOW!](https://steamcommunity.com/games/1652250/announcements/detail/6909171012596238836) | Table 9 Studio，2024-08-15 | official-dev / A | 1.0 与 Early Access overhaul 边界 |
| `src-tnt-official-levelup-2024` | [Road to 1.0, Week 5 - Level Up!](https://steamcommunity.com/games/1652250/announcements/detail/5963413728331742803) | Table 9 Studio，2024-07-31 | official-patch / A | Level Perk、开局组件、Army Size、Mythic、事件选择 |
| `src-tnt-official-engineer-2024` | [Road to 1.0 Update 7](https://steamcommunity.com/games/1652250/announcements/detail/5963414363385569697) | Table 9 Studio，2024-08-07 | official-patch / A | Engineer 重做原因、tier scaling、Boss/bug |
| `src-tnt-official-1-0-28` | [1.0 Minor Update 1](https://steamcommunity.com/games/1652250/announcements/detail/5875595438683483426) | Table 9 Studio，2024-08-21 | official-patch / A | Spirit Ring 永久无敌环修复 |
| `src-tnt-official-1-0-40` | [1.0 Update 15](https://steamcommunity.com/games/1652250/announcements/detail/6240387642467407082) | Table 9 Studio，2024-09-02 | official-patch / A | PvP rarity cost、预决定奖励 |
| `src-tnt-official-1-2` | [Big Balance Patch 1.2](https://steamcommunity.com/games/1652250/announcements/detail/1806698490739422) | Table 9 Studio，2025-08-02 | official-patch / A | targeting、Fritz、Ignite、Engineer、total tier |
| `src-tnt-official-1-4` | [1.4 Balance & Bugfix Update](https://steamcommunity.com/games/1652250/announcements/detail/1821922921820428) | Table 9 Studio，2026-01-19 | official-patch / A | Brutal 5、Leader、DR cap、Deshret 与 trigger fixes |
| `src-tnt-official-2-0` | [Set 2: World Tour + Major Update](https://steamcommunity.com/games/1652250/announcements/detail/1840310314342593) | Table 9 Studio，2026-08-06 | official-dev / A | Set 2、Uber Items、单单位上限、Knockout |
| `src-tnt-official-2-0-14` | [T&T Update 2.0.14](https://steamcommunity.com/games/1652250/announcements/detail/1842212951297590) | Table 9 Studio，2026-08-26 | official-patch / A | Set 2 难度 bug、Boss、独立 Challenge Climb |
| `src-tnt-steam-ladder15-2024` | [无脑过天梯15方法](https://steamcommunity.com/sharedfiles/filedetails/?id=3336892386) | 提莫跑的贼快，2024-09-23 | strategy-guide / C | 四龙四贵族具名路线、Clover、装备、Clone Capsule |
| `src-tnt-steam-traits-1-4-3` | [[KOR] T&T Guide 1.4.3 - Traits (1)](https://steamcommunity.com/sharedfiles/filedetails/?id=3696321935) | Ousters，2026-03-31 / 04-09 | strategy-guide / C | Dragon/Mythic/Engineer/Aquatic/Chaser 规则与评价 |
| `src-tnt-steam-units-2cost-1-4-3` | [[KOR] T&T Guide 1.4.3 - Uncommon Units](https://steamcommunity.com/sharedfiles/filedetails/?id=3691420824) | Ousters，2026-03 | strategy-guide / C | Fritz/Flash/Deshret 职责与属性投资 |
| `src-tnt-steam-brutal5-team-2025` | [Brutal 5 winning team](https://steamcommunity.com/app/1652250/discussions/0/598536819815041740/) | Buzz Killington，2025-09-03 | community-analysis / D | 五人队、combatant、职责与 Lel'Thas 结果 |
| `src-tnt-steam-brutal5-carry-2026` | [Force a 4-star 3-cost carry](https://steamcommunity.com/app/1652250/discussions/0/590686232124783850/) | 真空中的球形企鹅，2026-08-09 | community-analysis / D | SL/RNG、高星单核、Eternal Scythe、人口压缩 |
| `src-tnt-steam-army-size-2026` | [Understanding the game](https://steamcommunity.com/app/1652250/discussions/0/590686501615970922/) | maaiiccoo / Pip，2026-08-10–11 | community-analysis / D | Boss 增人口、XP、卸装成本 |
| `src-tnt-steam-upgrade-replace-2025` | [Trade common for rarer unit?](https://steamcommunity.com/app/1652250/discussions/0/510701054096078657/) | matheod 等，2025-03-15–16 | community-analysis / D | upgrade/item/trait 投资与替换分歧 |
| `src-tnt-steam-comp-building-2025` | [General vibe of tactics](https://steamcommunity.com/app/1652250/discussions/0/598517805390773870/) | Tengen Toppa 等，2025-02-05–14 | community-analysis / D | 职责优先、装备塑职、banner/duplicator bridge |
| `src-tnt-steam-set2-boss-2026` | [Did Set 2 increase in difficulty?](https://steamcommunity.com/app/1652250/discussions/0/590686232124836582/) | Holy Fool Sehrael / Michael Mayhem 等，2026-08-10–14 | community-analysis / D | Boss bug、Boss Heart、tooltip、敌队反制 |
| `src-tnt-steam-zolton-2026` | [Zol-ton MK II set 2](https://steamcommunity.com/app/1652250/discussions/0/586183319262267454/) | Gentlest Giant / Michael Mayhem，2026-08-17 / 26 | community-analysis / D | dominance、Mana nerf、Ignite counter |
| `src-tnt-steam-challenge-climb-2024` | [Challenge Climb Difficulty](https://steamcommunity.com/app/1652250/discussions/0/4628106985313725929/) | The Nue 等，2024-11-25–2025-01-12 | community-analysis / D | 高难波动、生命缓冲、构筑/装备/站位三层 |
| `src-tnt-steam-rare-rush-2024` | [tactician rank](https://steamcommunity.com/app/1652250/discussions/0/4432192284001576599/) | Tako / Michael Mayhem 等，2024-08-27–31 | community-analysis / D | rare rush 与高段同质化、开发者响应 |
| `src-tnt-steam-ring-loop-2024` | [Found an actually broken combo](https://steamcommunity.com/app/1652250/discussions/0/4432191758099661566/) | Bishsume / Michael Mayhem，2024-08-20 | community-analysis / D | Spirit redirect + invulnerable ghost 无限环 |

## 真实循环、资源和 ownership

Classic Campaign 不是只选一个 trait 然后等结果。玩家在战斗后获得 Gold、Star Points、XP、单位、组件和事件机会，再在商店、Level Perk、Vault/duplicator、装备合成与站位之间分配。2.0 社区规则答复称每击败一名大 Boss，Army Size 增加 1；Grand Tournament 前有两名 Boss，所以正常终局达到 6 个部署位。XP 用于 level up 后选择 Perk。官方又提供 `For The People!` 这类额外人口选择，但同时给敌人 Damage Amp/Reduction，说明人口本身也是有反向成本的构筑资源。

单位强度由多层承诺叠加，而不是 rarity 单轴。2025 替换讨论把一名单位拆成最多三次升级、通常三件装备和 2–3 个 traits；一个 Tier 4 common 或正确装备的 caster 可能比新刷出的 rare tank 更难替换。Rare 数量更少，也更难在后段同时满足升级与 trait 组合。Marksman 可以继续从宽度获益；Dweller 读取单位总 tier，却因 rare 难追而产生垂直升级压力；Trickster/Royal 则被社区描述为可出售的前期 momentum/farming 层。Demon 是否应继续加人口存在社区分歧，本档案不抹平。

装备附着具体单位，不能免费任意转移。2.0 讨论称商店 potion tab 每店有两件 1 Gold 的卸装 consumable，一次把一名单位的全部装备退回 backpack。于是替换一个已经三装备、三升级的 carrier，不只比较新旧面板，还要支付卸装、重新装备、trait 断点和升级损失。Banner 可以把 trait 写到合适 owner，Duplicator/Power Roll/Clone Capsule 可以弥补招募，Skull of Hedra 等单位生成物可桥接人口；这些是“转换与补洞资源”，不是免费的完整体系。

## 具名队伍一：早期 1.0 天梯 15 四龙四贵族

核验来源：`src-tnt-steam-ladder15-2024` 给出完整开局路线、装备方向、Noble 和 Clone Capsule；`src-tnt-steam-traits-1-4-3` 用 1.4.3 roster 解析两只 1-cost 与两只 3-cost Dragon 的唯一组合，并补充 Dragon trait、专属装备和空间条件；`src-tnt-official-levelup-2024` 交叉证明开局方向、Army Size/Perk 与 Mythic/物品的机会成本。具体强度仍只属于早期 1.0。

- `roster`：Lime（1-cost Caster / Dragon, Shaman, Elite）、Micky（1-cost Defender / Dragon, Brawler, Bloodbond）、Brim（3-cost Fighter / Dragon, Engineer, Divine）、Kio（3-cost Defender / Dragon, Horror, Aquatic）。攻略称开局拿两只 1-cost Dragon，第一战后用两枚无等级限制 Lucky Clover 取得两只 3-cost Dragon；1.4.3 roster 使这四名身份可解析。
- `engine`：角色第四技能选择 Advanced Courses；随机 banish race 时必须保住 Dragon。两枚 Clover 把商店等级限制绕开，第一战后直接形成 4 Dragon；只开放 Noble 作为额外 trait，再以一个 Noble Banner 闭合 4 Noble。
- `state/resource`：四个 Dragon bodies、首次施法时机、接触 Dragon fire 的友军、单位 tier、Dragon Egg bench 格/三战进度、Clover、Banner、经验商店与 Clone Capsule。
- `payoff`：1.4.3 guide 称 4 Dragon 在 Dragon 首次施法时产生火焰，接触火焰的友军 Tier +1；真正收益 owner 是被火焰覆盖并成功升 tier 的单位，而不是 Dragon 标签本身。Dragon Egg 占一格 bench，三战后给 Dragon Tear 与随机专属装备，进一步把空间换成垂直投资。
- `survival`：Micky 与 Kio 是前排 Defender，按原攻略持 tank gear；攻略中的“重锤龙”与 roster 中的 Brim 对照，持两件 Attack Speed 与一件恢复装备，在前线同时承担 damage 与 sustain。Lime 留在较安全位置负责施法/触发。
- `spatial condition`：友军必须接触 Dragon 首次施法留下的火焰；缺氧 battlefield、开局前死亡或未接触火焰会让 tier payoff 失效。Dragon Egg 还占 reserve/bench 格。Banner holder 原攻略未指名，不能擅自分配给某一英雄。
- `equipment/trait ownership`：Brim 持 2 AS + 1 heal；Micky/Kio 持 tank items。1.4.3 专属 Dragon Claw 在施法后叠攻速，Dragon Shield 读最大生命施盾，Dragon Torch 给 SP/Mana Regen，Dragon Tooth 在友军死亡后叠 AD；这些是可选 owner 路线，不等于 2024 队伍必定拿到。
- `economy/pivot`：两枚 Clover 是第一战后的窄窗口；若只拿到一只 1-cost Dragon，攻略称仍可勉强继续。经验商店优先 Clone Capsule，帮助复制目标单位。若 Dragon 被 banish，或 Clover 没拿到所需 3-cost，应立即放弃“无脑四龙”假设，因为核心经济捷径已经失效。
- `counter/failure`：禁止 Dragon、火焰无法覆盖、bench 被 Egg 占满、首次施法前阵亡、缺 Mana Regen 或前排倒得太快，都能令这条路线失效。报告应显示每名 Dragon 首次施法、火焰覆盖对象、tier 前后、Egg 进度和 Banner 提供者。

## 具名队伍二：1.2 后 Brutal 5 Mountain Dwarf / Occultist

核验来源：`src-tnt-steam-brutal5-team-2025` 给出完整 combatant、五名单位、职责与三次击败 Lel'Thas 的作者记录；`src-tnt-steam-units-2cost-1-4-3` 在后续 1.4.3 分别解释 Fritz、Flash、Deshret 的技能/属性 ownership；`src-tnt-official-1-2` 与 `src-tnt-official-1-4` 给出 Fritz、Ignite、Deshret 与 Brutal/Leader 的版本变化。它不是逐槽装备/hex guide，这一限制不会被推测填平。

- `combatant`：Mountain Dwarf（-1 Army Size，换 25% Damage Amplification/Resistance）；Berserker class（favored weapon）；Find Love aspiration（开局两个 uncommon）；Occultist profession 只用 Forbidden Path，不激活 trait，换 damage/armor/mana regen。作者称 Forbidden Path 没出现就重开。
- `roster`：Fritz、Flash、Deshret、Gourmand、Noctus。
- `engine`：Fritz 与 Flash 用多目标伤害快速清场；Fritz 额外提供长 Shred，Noctus 以 Stifle 和多目标技能压制施法；Gourmand副坦并提供 penetration；Deshret用 heal/Death Ward 类保护把队伍拖过高难 Boss。
- `state/resource`：-1 Army Size 后的五人容量、Forbidden Path 是否出现、几乎满装备、Fritz/Flash 的 Mana/skill cadence、Shred/Stifle 持续、Deshret 的 Ward target 与 Gourmand 承伤。
- `payoff`：Fritz/Flash 是清场 damage owners；Noctus 是控制/副伤 owner；Gourmand 把前排承伤转成 penetration support；Deshret 是 survival owner。作者明确说先前用 Koda 坦能走很远，但最终胜利需要 healer，说明“更厚坦”不能替代该 party 的续航层。
- `survival`：Mountain Dwarf 的队伍 Amp/Resistance、Gourmand off-tank 和 Deshret healing/ward 共同承担。后续 1.4 官方修复 Deshret targeting/0-heal，并限制刚被 Death Ward 复活的目标再次获 Ward，证明该生存链的 target ownership 与防循环规则很重要。
- `spatial condition`：原帖只说明 Gourmand off-tank、Deshret healer 与前后职责，没有给精确 hex；因此本档案只要求让 Gourmand承接部分前线、保护 Deshret/Fritz/Flash/Noctus，不声称一个原作者未给出的固定阵型。
- `equipment/trait ownership`：原帖只说几乎所有单位将满装备、Berserker 提供 favored weapon，没有逐件命名。1.4.3 unit guide 建议 Fritz 用 Mana Regen/SP，Flash 更需要 Mana Regen 维持 Ignite 而非堆 SP，Deshret因高 Mana cost 优先 Mana Regen；这些是后续参考，不被冒充为原队伍实装清单。
- `economy/pivot`：Find Love 的 uncommon 只做开局桥接，若不匹配可以卖；Clovers帮助找到目标单位。Forbidden Path 没出现时作者选择重开，说明这套并非普适稳定线。替换 Koda 为 Deshret 是从纯坦度向 Boss 续航的关键职能转型。
- `counter/failure`：单核/无治疗在 Lel'Thas 多轮压力下不足；Deshret target bug 或防重复 Ward 规则会改变生存；Mountain Dwarf 少一人口使职责冗余更低。该作者的三次成功不等于总体胜率。

## 补充路线：2.0 第一商店四星三费单核

`src-tnt-steam-brutal5-carry-2026` 描述一条作者明确承认的 cheesy 路线：第一商店前尽量存 Gold，取得 `Rainbow Slime Bottle ×1 + Cosmic Medium ×3`，利用每次 reroll 会推进随机 transformation 结果的现象，通过 save/load + 不同 reroll 次数挑出 3/4-star 3-cost carry。之后全部 items、Perks、Banners 和 units 都围绕该 carry 定向。

作者给出的泛用结构是 Ultimate Eternal Scythe、两个 Legendary supports、一个高星 carry，并尽量堆 debuff/CC immunity；拖入 overtime 后 damage 与 lifesteal一起增长。最极端通关只有三人：默认终局 Army Size 6，Titan 减 2，另一件装备减 1，换取一个 4-star carry 与两名 support。2.0 官方同时引入 Uber Items，明确说重复合成后的装备会提高单单位上限并改变 late-game army building，因此“垂直投资压缩人口”是版本真实结构；但读档操纵 RNG 与作者所称“Brutal 5 过调”仍是单人观察，不是官方推荐。

这条路线的价值恰恰是负面边界：如果最稳定解来自保存/读取枚举随机结果，构筑系统的选择可能退化为先锁定一个超规格 owner，再把所有正常招募、trait 和人口规则变成附属物。它不替代两套具名 party，也不证明人口越少越强。

## Trait、装备与空间结构

### Vertical 与 horizontal

社区构筑建议普遍先确保 Tank/Fighter/Caster 或 Tank/Heal/DPS，再用 trait、item、event 和 Banner 连接，而不是为补最后一个 trait unit 烧完 reroll。Monty 可以通过 `FOECLEAVER + Attack Speed` 被装备塑成 payoff owner；Fester + Ghoul Banner 可以把 Shelly 横向接入；Craggus 的 HP/Armor 同时提高坦度与技能伤害，是防御属性直接服务输出的单 owner 例。

trait 并不共享同一种“越多越好”函数：Marksman 在 4 以上仍获益；Dweller 读取总 tier，因此高 rarity 多单位反而难升级；Trickster/Royal 被视为可出售的早期经济/节奏层；Demon 增员可能稀释 Demonlord buff 落到 carry 的概率，但另一玩家指出特定死亡 Perk 会反向奖励更多 Demon。设计上必须分别写清断点、持续宽度收益、随机受益者与可出售身份。

### Mythic、Engineer、Dragon 与 battlefield object

- Mythic 默认最多两名。1 Mythic 把 Valor 给自己和一个随机相邻 ally；两名 Mythic相邻时互相取得 Valor，并获得额外 Mana Regen。1.4.3 guide 建议把两者做成只彼此相邻的孤岛，否则随机 ally 会夺走关键 buff。官方重做理由还包括旧 Legend 难与 traits 协同、部分玩家觉得不有趣，以及 `Legend/Legendary` 术语会混淆。
- Engineer 为 2/4 断点，Turret damage 读取 Engineers 的总 tier；4 Engineer 使 Turret attack speed/AOE 质变。专属 hammer 改写 holder skill，适合给基础 Mana Regen 高的 Caster。官方重做前 Turret 固定低伤，面对 Tournament 数千 HP 仍无成长，开发者明确称玩家不爱 Engineer，因此加入 tier scaling 与开局 shield。
- Dragon 为 2/4 断点，首次施法火焰要求接触；专属装备分别把施法、最大生命、Mana/SP、友军死亡写给不同 owner。Dragon Egg用 bench空间换三战后的组件/专属件。这些机制说明 trait、装备、空间与 reserve 不是一个同义层。
- Aquatic fountain 给格子 Mana Regen，敌人踩到也可能获益，并能覆盖 pit/fire/mud；高级效果要求围绕 fountain站位。battlefield object 若同时改变地形和双方资源，就必须预览占领、覆盖和敌方受益。
- Chaser 在固定 PvE敌阵中可单方面切后排；1.4.3 作者认为强且提到近期 nerf。这个优势依赖敌方阵型不随玩家实时反部署，不能直接从 PvP 刺客平移到 authored PvE。

## Boss、反制与失败解释

Set 2 launch 暴露了几类必须分开的失败：Ki O'dini/River tournament entries 数值偏高；Boss Heart 要 nerf；Boss Minions 错误取得 Tiers/Runes，令 Xanatos/Monock 超出意图；Shred-o-matic 的 Boss Heart tooltip 声称 50% Damage Resistance，但开发者确认实际不生效并要删文案；玩家还报告 Lel'Thas Skeleton 满血复活到后排，可能绕过阵型。这些不能归成一个“Boss 太强”。

2.0.14 官方进一步确认 Set 2 Knockout 原本应按所选难度抽敌队，却错误从所有难度随机；Ki O'dini 的 Frisky + item 组合每 5 秒回 100% max HP；Boss 可同时生成两套 War Banner；Tournament tier-up 使用固定难度而非 Challenge Climb。官方把 Set 2 Challenge Climb 独立重置并增加提示，因为 Set 2 技能集不同，旧 Set 1 高难经验不能直接视为已掌握新 set。

Zolton 是更清楚的 counter package：开发者称其从 Set 2 发布后一直 dominant，2.0.14 当日显著提高 Mana cost；同时点名 Ignite 对高 HP 目标更强并削减 healing。反制因此不是泛用“堆更多伤害”，而是预览高 HP/高治疗 engine，提供 Ignite 或等价 anti-heal，再用 CC/cadence窗口延迟首个大招。若玩家没有机会取得此类 counter，社区所说的 “hardcounter or lose” 会变成 offer RNG，而不是有效适应。

战后解释至少需要：敌人实际难度包、Boss Heart 实际生效属性、Boss Minion tier/rune 来源、复活落点、最高治疗来源、Ignite uptime/healing prevented、Zolton 首次施法时刻，以及玩家是在信息不足还是没有可用 offer 时失败。

## 生命周期与负面/重做案例

1. **Engineer 固定炮台成长失败后重做**：官方称玩家不偏好 Engineer，原因之一是同样的 40 damage Turret 到 Tournament 仍面对数千 HP。Road to 1.0 加入总 tier scaling、开局 shield 和 4 Engineer AOE；1.2 又继续调整基础与每 tier 增长。计一项明确低采用/成长失配重做。
2. **Legend → Mythic 的身份与协同重做**：官方不只调数值，而是处理难嵌入 trait team、部分玩家不觉得有趣、Legend/Legendary 术语混淆，并用相邻 ally 与双 Mythic Mana Regen 建立空间玩法。计一项明确身份/可读性重做。
3. **Spirit Ring 永久无敌负反馈环**：Receiving Ring 放在被 Spirit 复制的 bench unit、Giving Ring 放在场上 Spirit 后，伤害被导向不可选且无敌 ghost，ghost 又因 Spirit 不死而不消失。玩家报告后，1.0.28 官方明确修复该永久不可击杀军团，并扩展到 Stasis 等同类状态。计一项确定循环 bug/anti-stalemate 案例。
4. **PvP rare-rush 令高段同质化**：开发者确认快速进入 rares 过于稳定，Kio/Noctus/Brim/Wolfgang 在高段占优，并立即测试降低一致性的改动；1.0.40P 又提高 PvP Rarity costs。计一项模式限定的经济/池同质化治理，不外推 Classic Campaign。
5. **Brutal 5 早期“重开等高 roll”**：1.4 官方将 PvE Leader bonus health 从 50% 降至 30%，明确说 Brutal 5 会加倍 Leader bonus，并希望 Act 1 少一些 “restart until you high roll”；同补丁加 90% Damage Resistance cap 防止 bug 导致完全无敌/softlock。计一项难度曲线与安全上限修复。
6. **Set 2 难度错配不是单一平衡问题**：2.0.14 官方确认 Knockout 敌队难度随机、Ki O'dini 100% max-HP heal、双 set War Banner 与 Tournament tier-up 四类 bug，并为新 set 独立重置 Challenge Climb。计一项发布后难度/教学/敌包校准案例；不把所有玩家失败都归因于数值。
7. **Zolton dominance + 明确 counter**：开发者确认从 Set 2 launch 起 dominant，随后提高 Mana cost，并指出 Ignite 的 high-HP 与 anti-heal价值。计一项单位统治与 counter-readability 案例；没有公开胜率，不量化影响。

本 checkpoint 将以上七项加入明确负面/重做案例。Road Tale 为避免一次误点进 Curse 增加安全选项、Risk Cards 保持高期望值，是额外的选择可读性案例，但未单独计数。

## 对本项目的迁移

可迁移：

- 把单位价值显示成“单位本体 + 升级 + 装备 + trait + 站位承诺”，招聘页不能只用 rarity 诱导替换。
- 盾/防御不是独立伤害答案，但可以由装备/遗物把 HP、Armor、Shield 或全队额外属性交给一个明确 payoff owner。Craggus、Dragon Shield、Mountain Dwarf 和 Uber Items 分别展示不同 ownership，不能合并成一个模糊“盾流”。
- 让 trait 同时拥有 vertical、horizontal、bridge 与可出售层：断点只决定启动，持续宽度、随机受益者、总 tier 与 Banner ownership 另行定义。
- 用敌方 package 而非纯血量测试体系：高 HP + 高治疗要求 anti-heal；Boss 禁用/绕后要求冗余；固定后排则让 Chaser 过强；battlefield object 可能同时帮助双方。
- 对 conversion、redirect、revive 与 invulnerability 建立非递归来源链、上限和 anti-stalemate；Spirit Ring 说明两个单独合理的组件可能闭合成无损耗环。
- 战报必须归因“谁供给、谁转换、谁受益、谁反制”：Dragon fire 生成/接触、Deshret Ward target、Fritz Shred、Zolton heal、Ignite prevented healing、Boss Minion非法 tier 都需独立记录。

不可直接迁移：

- 本项目普通人口目标 10、物理上限 18；T&T 的正常终局 6、Mountain Dwarf -1、Titan -2 与 `For The People!` 不是可复制数值。
- T&T 的 duplicate merge、Star Points、Clover、Banner、Vault、Uber Items、Combatant 四层和共享/异步 PvP对手池都不是既定系统。
- 4 Dragon 的 fire tier-up、Mythic random adjacent Valor、Aquatic fountain 与固定 PvE Chaser 是具体游戏实现，只能抽象为空间条件/受益 ownership。
- Save/load 枚举 transformation RNG 是应防范的可重复性漏洞，不是“让玩家稳定成型”的设计手段。

## 未决问题

- 没有可访问的官方完整 manual 或当前 2.0 unit/trait database；1.4.3 韩文 guide 与 2.0 新 set 必须分开。
- 第二套 Brutal 5 party 没有逐槽装备名与精确 hex；只能确认角色和职责，不能验证一个固定阵型。
- 2.0 Army Size、Boss 后增长和卸装 consumable 来自社区规则答复；官方公告只证明存在人口/perk/装备机会成本，没有完整当前教程正文。
- 无公开 patch-level 胜率/使用率；1.4 官方明确因样本不足停用 Metrics。社区强弱词不能当统计。
- Chaser、Wraith、Mystic、Insect 等 1.4.3 强弱判断主要来自单一作者；除有官方 rework/nerf 交叉者外，不计明确失败。
- Lel'Thas 复活落点与 Shred-o-matic 进一步平衡来自玩家案例；开发者只确认相邻的 tooltip/Boss Heart 事实。

## Disposition

- `disposition`: `anchor-retained`
- 置信度：官方生命周期、已确认 bug/rework 与 2.0.14 难度修复为高；2024/2025 具名构筑结构为中高；1.4.3 trait/单位细节为中；社区强度与当前 2.0 泛化为低到中。
- 锚点门槛：22 个实质非商店页面，覆盖 `official-dev`、`official-patch`、`strategy-guide`、`community-analysis` 四种类型；具名四龙四贵族满足单位、装备方向、空间/触发、payoff owner 与转型/反制 gate。Brutal 5 五人队和 Set 2 单核按各自证据限制保留。
