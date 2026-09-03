# Vivid Knight

## 身份与研究时期

- `title_id`: `vivid-knight`
- 类型：单人 party-building roguelike / 轻量 autobattler。玩家在随机迷宫中移动并消耗 Mana，收集被宝石化的单位、扩充 active/reserve slots、三合一升级、在 Jeweler 买卖/reroll、装备饰品，并在自动回合战斗之间选择是否施放带次数与 cooldown 的 Gems。
- 发行状态：2021-05-27 Steam 发行，后有 Switch/mobile 版本；机制更新主要集中于 2021-05 至 2021-07 的 1.1.x，2021-10 为 1.2.3，2025 仅见语言、手柄和少量稳定性修补至 1.3.9。本档案不因后续维护版本号而假定核心规则改变。
- 版本边界：早期攻略横跨 1.1.5、1.1.10、1.1.25 与 Witch's Maze 8；最深入的 Witch's Maze IX 攻略明确以 1.2.3 为规则基线，页面虽更新到 2025，但没有把游戏版本提升为 1.3.x。单位数值、符号阈值和 Boss 构筑均保留版本标签。
- 直接游玩：本批未购买或安装游戏；规则来自 11 篇官方 patch 正文和 6 篇可读 Steam Guides。商店页、搜索摘要、视频标题和无正文页面未进入 deep evidence。

## 检索日志

1. 通过 Steam official news API 审查全部 72 条新闻，读取 1.1.2、1.1.5、1.1.6、1.1.7、1.1.10、1.1.11、1.1.18、1.1.23、1.1.31、1.1.34、1.1.35 正文，核验符号持久化说明、Jeweler、Boss 频率、Green、unit/tag 重排、accessory 信息、Maze IX 和动态符号 bug。
2. 读取 `Easy Start Guide v1.1.10` 的 Units/Map/Team Edit/Jeweler/Battle/Tips，核验双标签、技能发动类型、active/reserve、三合一、单位意图、Gem 分类、cooldown/uses 与 Mana 路线成本。
3. 读取双语 `Symbol & Character List v1.1.25`，核验 color 与 mark 的独立阈值、每个单位的交叉标签和技能，重点校对 Dancer Reno、King's Sword Rubela、Thunder Lion Topaz、Masked Jet、Eleschar、Sphene、Labla 等 build owners。
4. 读取 `Amelie Symbol Guide` 正文与同页具体构筑讨论，核验银星后永久持有 symbols、可出售 bridge、accessory bridge，以及 Rubela—Reno—Topaz 的 Fool/Shock 多段队。
5. 读取 `Witch Maze Guide`，核验 Amelie/Zeola 高难阵容、角色槽先于 reroll、Green/Orange 等早晚期转型、五枚 Gold Shield 与 accessories；作者与回复的 sustain/aggression 冲突按社区分歧保留。
6. 读取 1.2.3 `Witch's Maze IX` 全部 18 节，核验 ekonomi、pair holding、多面待牌、Gold unit bridge、Monster Spot 饰品、三种 Black Witch 形态和各自完全不同的 party/symbol/gem counter。
7. 读取 1.1.5 Boss guide，核验 gem unlock pool 污染、Burn/Shock 区别、恢复/盾的战中时机和 Boss failure；内嵌视频无 transcript，只采用可读正文，不把标题/播放数当证据。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-vivid-official-1-1-2` | [Update Version 1.1.2](https://steamcommunity.com/games/1569090/announcements/detail/4069544815363968644) | Asobism，2021-05-29 | official-patch / A | symbol persistence 提示、事件预览、Boss/Seal、color 可见性 |
| `src-vivid-official-1-1-5` | [Patch 1.1.5](https://steamcommunity.com/games/1569090/announcements/detail/4069544815374660027) | Asobism，2021-06-01 | official-patch / A | Witch Maze Jeweler reroll 成本降低 |
| `src-vivid-official-1-1-6` | [Patch 1.1.6](https://steamcommunity.com/games/1569090/announcements/detail/4093189347039280573) | Asobism，2021-06-02 | official-patch / A | 四 Boss 过频技能调整、symbol persistence 提示再强化 |
| `src-vivid-official-1-1-7` | [Patch 1.1.7](https://steamcommunity.com/games/1569090/announcements/detail/4093189347043341466) | Asobism，2021-06-03 | official-patch / A | maximally upgraded unit HP 上调、Green Lv2/3 下调 |
| `src-vivid-official-1-1-10` | [Patch 1.1.10](https://steamcommunity.com/games/1569090/announcements/detail/4093189347053864527) | Asobism，2021-06-06 | official-patch / A | 八个 gold-star skill 上调、Reno 下调、Forest 过滤修复 |
| `src-vivid-official-1-1-11` | [Patch 1.1.11](https://steamcommunity.com/games/1569090/announcements/detail/4093189347057412537) | Asobism，2021-06-07 | official-patch / A | result accessory symbol、sealed Gem 可见性、Boss arrival reward、rarity/事件 |
| `src-vivid-official-1-1-18` | [Patch 1.1.18](https://steamcommunity.com/games/1569090/announcements/detail/5019805792175378254) | Asobism，2021-06-14 | official-patch / A | Flint/King's Archer/Jaspa symbol 重分配、Megaphone cooldown |
| `src-vivid-official-1-1-23` | [Patch 1.1.23](https://steamcommunity.com/games/1569090/announcements/detail/4056035920061837237) | Asobism，2021-06-19 | official-patch / A | Green 10/20/30→10/15/25、Thorny Shield、Boss/skill bug |
| `src-vivid-official-1-1-31` | [Patch 1.1.31](https://steamcommunity.com/games/1569090/announcements/detail/4032392655074317166) | Asobism，2021-06-27 | official-patch / A | Ghost 3/4→2/4 与 Lv1 70%→50%、多项文本校正 |
| `src-vivid-official-1-1-34` | [Patch 1.1.34](https://steamcommunity.com/games/1569090/announcements/detail/4032392655092969726) | Asobism，2021-07-02 | official-patch / A | Witch's Maze IX、Magic Hammer、Black Witch+ |
| `src-vivid-official-1-1-35` | [Patch 1.1.35](https://steamcommunity.com/games/1569090/announcements/detail/4032392655103100599) | Asobism，2021-07-05 | official-patch / A | Jeweler 中新激活 symbol 不生效、potion/奖励停止与 Boss 名称 |
| `src-vivid-guide-maze9` | [Amelie Witch's Maze IX Guide v1.2.3](https://steamcommunity.com/sharedfiles/filedetails/?id=2708511302) | wanio，2022-06-30 / 2025-08-21 | strategy-guide / C | 完整高难经济、标签/单位评价、区域敌组、三 Boss 形态、队伍/Gems/饰品 |
| `src-vivid-guide-easy-start` | [Easy Start Guide v1.1.10](https://steamcommunity.com/sharedfiles/filedetails/?id=2509798595) | Aref，2021-06-06 / 06-13 | strategy-guide / C | UI/规则、color/mark、发动类型、Team/Jeweler/Battle、duplicate 与 Mana |
| `src-vivid-guide-symbol-list` | [Symbol & Character List Amelie v1.1.25](https://steamcommunity.com/sharedfiles/filedetails/?id=2506024143) | HAC，2021-06-03 / 07-13 | strategy-guide / C | 双语 symbol 阈值、单位交叉标签、技能与 upgrade stats |
| `src-vivid-guide-symbol-tips` | [Amelie Symbol Guide and General Tips](https://steamcommunity.com/sharedfiles/filedetails/?id=2506093128) | Zephro，2021-06-03 / 06-04 | strategy-guide / C | 银星永久 symbol、bridge/sell、Fool/Shock 具体组合、Boss counters 与观点冲突 |
| `src-vivid-guide-witch-maze` | [Witch Maze Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2515614635) | doemondo，2021-06-13 / 06-21 | strategy-guide / C | Witch Maze 4–8 Amelie/Zeola 终局队、饰品、5 shields、economy 与 upgrade limits |
| `src-vivid-guide-boss` | [Boss Strategy](https://steamcommunity.com/sharedfiles/filedetails/?id=2503200827) | lantastic79，2021-05-30 / 06-12 | strategy-guide / C | 1.1.5 gem pool、Burn/Shock、盾/恢复时机、Flame/Dragon/Witch 与失败观察 |

## 真实循环与玩家决策

### 迷宫、Mana 与 Jeweler

- 地图移动、回到 Jeweler 买卖、超出 storage/weight 和探索岔路都会消耗或压迫 Mana。敌人提供 Keene，地图会发现免费单位、Gems、恢复与事件；不打怪会缺钱，过度往返也会在到楼梯前耗尽 Mana。
- regular Jeweler 从完整 pool 抽单位，reroll 付费；traveling Jeweler 指定一类 symbol，适合收尾而非无条件追。Maze IX 攻略反对早期单点 reroll：先扩 active/reserve slots、同时持有多个 2-copy pairs，用多面待牌对抗宽 pool，再在 8F/最终 Boss 前集中卖 bridge、使用 1.1× Keene 的 Magic Hammers 后大规模刷新。
- 同一单位买入/卖出价值在攻略中按 30 Keene 计算；Gold unit 稀有、难升，但无星仍可作第六人的技能/标签 bridge，等中期落后时再卖。Gold-star 需要 9 copies，Maze IX 的低经济通常只做到 silver-star，说明 rarity、upgrade、skill 和 symbol contribution 不是同一纵向价值。

### 双标签、三合一与“永久桥”

- 每个单位通常同时带一条 **color** 和一条 **mark/symbol**，也可能出现 mark+mark、随机 symbol 等例外。Color 与 mark 分别计数、到独立 breakpoint 后施加全队 effect；单位技能、基础 stats 和 tags 是三套价值。
- 3 个同名单位合成 silver-star；3 个 silver-star 合成 gold-star。关键规则是：第一次升到 silver-star 后，该单位的 symbols 在本 run 永久获得，即使把单位卖掉或移出 active party仍保留。重复升级因此同时是纵向 stat/skill investment 与一次性横向 tag unlock。
- Accessory 既有 slot-specific effect 又携带 symbol，可临时补 breakpoint；Monster Spot 胜利允许选择 ring/necklace/earring 部位并替换。`Fool` 等高门槛可用一个饰品补齐，不必强求所有指定英雄。
- 这种结构允许开放桥接：便宜 Bronze 的 two-axis combination、silver 后出售、random-symbol unit、accessory、Gold no-star bridge 和 traveling Jeweler 都能替换配方中的一个具体名字。它不是“必须集齐固定六人”。
- 但持久化也制造不可逆风险。Maze IX Dark-form Black Witch 会按友军 buffs 超过 5 种后的数量增加伤害；许多 color/mark 永久给全队 buffs。玩家从 1F 就要避免 Justice、Chariot、White/Plum 等低收益 buff sources，说明“已获得 tag 永久留存”会压缩后期 counter-pivot。

## 具名构筑一：Fool—Shock Rubela 多段核心

来源时期：v1.1.10–1.1.25 规则与 `Amelie Symbol Guide` 同页实战讨论。

- **前置 bridge**：把 Black/Fool 的 Masked Jet 和 Purple/Fool 的 Eleschar 各收集到 silver-star后出售，使 Fool marks 永久留存；缺一份时由带 Fool 的 accessory 补。两名 bridge 的 color 还能同时推进 Black 或 Purple，而不是只为 Fool 占死位。
- **King's Sword Rubela**：Red/Fool Gold unit，放在 slot 1；战技对一个敌人进行 6 次 70% physical hit，是 Shock 的主 payoff owner。
- **Dancer Reno**：Orange/Fool Silver unit，置于 Rubela 后方；战技治疗前方友军并给它和自己 Truestrike，使 Rubela 更稳定发动多段 skill。Orange Lv1 在开局给全队 Truestrike，进一步保证首轮 skill。
- **Thunder Lion Topaz**：Yellow/Tower Gold unit，不要求升级；对一个敌人造成 physical damage，并给全体敌人 Shock。Yellow/Shock 在敌人每次被 hit 时结算 flat added damage，因此 Rubela 六连击把供应状态转成多次 payoff。
- **Fool mark**：按 Agility 给 Godspeed，触发时额外行动并清空；它同时提高 Topaz 供 Shock、Reno 续 Truestrike 和 Rubela cashout 的频率。评论记录一次四回合 final-boss clear，但该数字是单例，不是统计。
- **生存与补位**：剩余 slots 用 Green、Shield、Plum/White 或 defensive Gems 补开局生存；不能只追 Fool 4，因为其单位分散、强求会浪费未完成 symbols。多段也会更快消耗 Moonlight，并在反击/Spike Boss 前变成负面。
- 构筑语法：permanent/accessory Fool + Shock state + Rubela six-hit payoff + Green/Shield/Gem survival + Rubela slot 1 / Reno directly behind。Payoff owner 是 Rubela，Topaz 是状态 supplier，Reno 是发动 bridge。

## 具名构筑二：Maze IX Summon-form 范围/宝石队

来源时期：1.2.3 `Witch's Maze IX`；这是针对可在 run 开始看到的最终 Boss form 制定的终局队，而非所有形态通用最优。

- 成型六人：upgraded Sapphire 置于前方、Reno 置于其后；Forest Hunter Gem Silica、Appraiser Triffen、Peridot the Inventor、Labla the Astrologer 填满其余 slots。缺升级 Sapphire 时可用 upgraded Gem Silica 前置，unupgraded Sapphire 留后排作第二 AOE/cooldown body。
- 目标链：Reno 让前方 Sapphire 每回合稳定使用范围技能；Gem Silica/Triffen/Peridot/Sapphire 提供魔法 AOE；Labla 每次战技让所有 Gems cooldown -1。
- Symbols：Sun Lv2 放大 Gem damage，Pink 加速 Gems，Purple Lv2 放大单位 AOE magic，Forest 的 Magic Bullet 随机打到后排；Green/Magician 和其他防御轴只负责撑过没杀掉的爆炸。
- Gems：Dragon Arrow 为首选全体攻击，Cyclone 次之，Giant Dagger 打后排两体。玩家每回合优先进攻而非叠盾，必须在 Boss 第二形态召出的两只 back-row Bomb Mushrooms 下一回合自爆前杀掉。
- Counter/abort：只有前排单体攻击、依赖 Brown thorns 或纯 sustain 会被不断召唤和 Boss heal 拖死；缺 Sun/Purple 或 back-row Gems 时，应在早期就转离此方案，而不是到最终楼层才发现无法越过召唤墙。
- 构筑语法：Orange/Reno + Purple/Forest/Pink/Sun state + AOE/back-row Gems and units + Green/Magician emergency survival + front AOE owner with Reno behind。Payoff 分属 Sapphire/Gem Silica 与玩家 Gems，必须分别归因。

## 补充反构筑：Maze IX Spike-form 不攻击队

- Spike form 每回合获得 Thorns 与 Illusion，受攻击时可能对前排三人反击；第二形态 Defense 150、Magic Defense 40，并在每次被攻击后失去物防/获得破魔和攻击。普通多段物理会同时吃 Thorns 和多次触发反击。
- 高成本理想形态是 Mosco ×3 + Reno ×3，必要时将最前者换为 Chrome。Reno 让前方 hero 继续用非普通攻击或魔法 skill，Mosco/Forest Magic Bullet/Brown Thorns 负责不触发直接反击的伤害；Chrome 用高 Defense 承接固定先头三段并放大自身 Thorns。
- Gems 改为 Magic Armor、Heal/Healing Light、Iron/Charged/Thorn shields 和 Thunder；停止使用直接单发/多段 attack gems。Green、Magician、White/Plum、Blue、Shield、Brown、Star 与 Purple/Forest共同撑长战。
- 这是刻意“关闭自己通常的伤害发动机”来通过 capability exam，且资金高到不能每 run 达成。它证明 Boss counter 应允许可识别的保守 pivot，而不要求玩家永远维持一个封闭六人 recipe。

## Gems：有限战中干预，而非英雄技能

- Battle UI 展示双方 HP/buffs/debuffs 和角色下一动作意图；轮到玩家时可施放 Gem 或 skip。Gems 分为 offensive、defensive/heal 和 map-use，分别有 remaining uses 与 cooldown。
- Sun 放大 Gem damage，Pink 使 cooldown 在回合开始额外推进，Labla 用单位战技减少所有 Gem cooldown；Mirror Earrings 则把“skip”转成下一次 Gem cast twice。技能单位与玩家 Gem 因此是两套互相加速但 owner 不同的行动层。
- 高难攻略会在进 Monster Spot 前等关键 Gem cooldown、在普通弱敌留下最后一个低威胁目标以回转 cooldown、在 Gold Dragon/Black Witch 的危险意图出现时保留两面 shield。使用时机和 skip 都有真实机会成本。
- 这层与本项目 tactical command 只有“独立于英雄、看意图、目标/时机/cooldown、失败归因”可迁移。本项目固定两条 commands、每 battle 三点且没有 Gem inventory/map item/五面 Gold Shield；不能把 Vivid Knight 的回合制背包照搬进实时战斗。

## Accessories、角色槽与替换

- Ring/Necklace/Earrings 同时承载 effect 与一个 symbol。Aegis Earrings 改变 shield 的伤害代价，War God Earrings 被攻击时加 Attack；Haste Necklace 让前方 unit double action；Mirror Earrings 以 skip 为代价 double next Gem。
- 配件不是“多一点总战力”：更换会同时改变 effect、symbol breakpoint 和最终 Boss 的 buff count。Maze IX Monster Spot 因可指定饰品部位而成为风险较高但能修正 build 的节点。
- 先扩 party slots 会立即增加身体、symbol 和可持有 pairs 的数量，并提高下一次三合一命中面；最后第六 slot 昂贵，要与 Magic Hammer 复利和终局 reroll 时点比较。reserve capacity 也影响是否能保留多个 two-copy pairs。
- 升银后的 bridge 可卖 30 Keene，为真正 skill owners、pair waiting 和 Gold bridge 腾位置；但已解锁 symbols 不能卖回去。替换 UI 必须分别显示 active skill loss、permanent symbols retained、accessory threshold change 和 Keene/Mana cost。

## Color 与 mark 不是两张平行加成表

- **Color examples**：Yellow/Shock 是每 hit reader；Red/Burn 在施加和回合结束时伤害；Blue/Freeze 降低 physical damage；Green 按 active symbol types 提供 team Max HP；Pink/Sun 直接服务玩家 Gems；Orange 保障首轮 unit skill。
- **Mark examples**：Fool 增加行动次数；Moonlight 是会被 hit 消耗/衰减的 damage amplifier，适合单次大伤、不适合多段；Forest 在用 skill 时发随机 magic bullet；Shield 每回合积累 Guard；Star 加 team Defense 并同时放大 Brown Thorns。
- 双标签的价值来自交叉：Reno 的 Orange/Fool 同时保障 skill 与额外行动，Rubela 的 Red/Fool 把多段/再行动放进同一 carry，Topaz 的 Yellow/Tower 则只负责 Shock 而不必完成 Tower。某个 unit 的第二标签可以是 bridge，不要求两条轴都成为本构筑主线。
- `Green` 甚至读取“已激活 symbol 种类数”，成为宽度 payoff；v1.1.7/v1.1.23 下调它，同时上调 fully upgraded units 的 HP，把一部分全队横向价值还给具体纵向 owner。

## Boss、区域反制与失败解释

### Dark form：惩罚永久 buff 宽度

- 友军 buffs 达 5 种后，超出的每种让 Black Sword 大幅提高敌方伤害；第一形态还召唤两台量产 Golem，三回合后发射高额全体炮。目标是把 buff 控制在约 4 种、快速清掉 Golems，并在第二形态重复自增前击杀。
- 推荐 front Sly、Reno behind、Jaspa、Moon，再用 Sapphire/Peridot 或 Paparadscha 补 AOE/高质量；优先 Purple+Forest、Blue、Moon、Green/Sun/Star/Wheel 等敌方 debuff 或 player-command axes，避免低收益 Justice/Chariot/White/Plum 的额外 ally buffs。
- 因 symbols 在升银后永久，错误不是最终 Boss 前换两件装备即可修正。run 开始看到 Boss form 就必须将“是否激活这一 symbol”当成不可逆路线决定。

### Summon form：后排定时炸弹与召唤墙

- 第一形态不断在自身前方召 Boss并治疗；第二形态召前排墙和两只后排 Bomb Mushrooms，它们生成后一回合待机、下一回合全体自爆。AOE/backline Gem、Sun/Pink 和 unit magic AOE 是必要试卷。
- 战报要区分 Boss summon action、temporary blocker、one-turn fuse、自爆 owner 与被延误的 backline target；纯前排伤害应明确显示为何永远接触不到炸弹。

### Spike form：攻击次数成为负资源

- Thorns、Illusion、backfire 和第二形态的受击成长让普通攻击、multihit、直接 attack Gems 产生反作用；Rubela/Topaz/Fool 等正常高速引擎在此必须停用。
- 可迁移的不是“规定某六人”，而是敌人 capability 清楚标出：直接命中次数越多越危险、首个目标固定、Magic Bullet/Thorns 等间接伤害不触发同一反击。玩家才能从现有 tag/Accessory/Gem 库拼出替代答案。

### 区域与中 Boss

- 6–8F 的 Turtle 前排 + Thunder/Earth 后排以 Shock multihit 绕防，需要 back-row/AOE Gems；9F 后召唤、治疗和 magic 增多，Brown physical Thorns 从主输出降为风险，Green/Magician 和主动范围 damage 上升。
- Flame Emperor 的 Burn/magic 绕 Freeze，需要 burst、recovery 或 Forest Medicine；Gold Dragon/Black Witch 的开场与危险意图要求保留两面 shield。Boss skill 频率曾在 v1.1.6 官方下调，说明反制窗口不能被随机连发抹掉。

## 失败、重做与明确负面案例

以下七组计入跨游戏明确负面/重做深度：

1. **永久 symbol 获得方式难以理解**：v1.1.2 增加 symbol persistence tips/effects，v1.1.6 又强化 upgrade 时的提示；核心 run-state 连续两次补教学，说明“卖掉仍保留”不是可凭经验猜出的规则。
2. **Boss 技能连发抹掉反制窗口**：v1.1.6 同时调整 Sharkman、Shark Champion、Grey Sherbird、Death，使其技能“不再过于频繁”。玩家仍需 counter，但不应因连续触发而失去操作窗口。
3. **Green 横向生命压过纵向升级**：v1.1.7 提高所有 maximally upgraded units HP，同时降低 Green Lv2/3；v1.1.23 再把 Green 10/20/30 调为 10/15/25。全队 symbol-width payoff 与具体 unit upgrade 被重新分配。
4. **Accessory 的隐藏 symbol 影响结算却未显示**：v1.1.11 在 result screen 给每件 accessory 增加 symbol 信息，同时改善 sealed Gems 可见性。装备 effect 与 tag 必须在同一决策表面出现。
5. **单位交叉标签破坏/封闭 bridge 图**：v1.1.18 把 Flint 从 Sun→Chariot、King's Archer 从 Tower→Sun、Jaspa 从 Chariot→Tower。单位本体未消失，但多个 breakpoint 的低成本连接点同时移动；tag graph 是 balance 资产。
6. **Ghost 难凑但高概率，改为易凑低概率**：v1.1.31 将需求 3/4 改为 2/4，同时 Lv1 proc 70%→50%。它用 access 换单次 potency，避免稀有闭环必须靠固定名单。
7. **动态 Jeweler state 与战斗 state 脱节**：v1.1.35 修复在 Jeweler 新激活的 symbol effect 不会实际生效。永久/临时 tag 的 recompute 边界必须由统一权威状态驱动。

另有 v1.1.11 将 Boss defeat reward 改成 arrival reward，明确让未取胜玩家也能获得进展；v1.1.10 同时上调八个 gold-star skills、下调 Reno；v1.1.5 降低早期 Jeweler update cost。它们属于 progression/balance 调整，但缺充分原因或结果，不重复计入七组。

## 社区观察边界

- `Witch Maze Guide` 的五盾 sustain 与同页回复的两回合 Purple/Orange/Forest aggression 是两种个人路线；没有统计证明哪一种总体更优。它们共同说明同一双标签池允许相反的 survival/payoff 配比。
- v1.1.5 Boss guide 主张不解锁与当前路线冲突的 Gems，否则 reward pool 稀释、三合一困难；这是社区观察和 metaprogression 警报，不是官方承认的机制失败。
- Maze IX 攻略指出早期“堆盾+Shock/Thorns 慢磨”资料在 Maze IX 已过时，后期 summon/magic/Boss form 要求主动伤害；这是版本/难度环境变化，不代表旧构筑从未有效。
- Fool/Shock 四回合 final Boss、每次约 1000 Keene 大刷、Mosco×3/Reno×3 均为作者 run 或高成本理论线；只保留发动条件和反制，不转成 meta 频率。
- 没有公开 unit/symbol/Gem pick rate、Witch form matchup、Jeweler odds history 或 current 1.3.9 combat database。

## 对本项目可迁移

### 可迁移原则

- **双轴应交叉，不应绑定配方**：一名英雄可带 affinity + role/behavior tag，装备/遗物能补一轴，低阶 bridge 可退出 active formation；payoff 只需读取其中一轴或交叉条件，不要求六个固定名字。
- 重复升级必须明确区分 body、纵向 stats/skill 和横向 tag contribution。Vivid 的永久 symbol 是强力参考，同时也是不可逆污染的风险；本项目每个 persistent hero 仍消费 1 population，不能把 copies 伪装成额外 bodies。
- 角色、accessory、run-level symbol 与 player Gem 是四种 owner。招募替换界面应显示“失去哪项主动能力、保留/失去哪项 tag、谁持有装备、哪个 payoff 仍能启动”。
- Boss form 可按 capability counter：buff count、summon/backline timer、direct-hit count，而不是指定“必须用某元素”。这允许同一 counter 由多种 hero/equipment/relic bridges 完成。
- 战中操作最有价值的是 enemy intent、skip/commit timing、cooldown 和 source attribution；不需要照搬大量 consumable Gems 才能获得这层决策。
- Horizontal trait width 与 vertical hero investment 应互相制衡。Green 的连续下调和 gold-star HP 上调说明全队宽度收益过强时，应把部分价值还给明确 owner，而不是继续叠全局倍率。

### 不可直接迁移

- 本项目是 persistent hero roster；Vivid 把 hero 当作可频繁买卖、三合一并在卖出后留下永久符号的 collectibles。不能照搬 3/9 copies、固定 30 Keene、active/reserve mass holding 或收集即永久规则。
- 本项目普通 endgame population 为 10、物理上限 18；Vivid 的六人队、storage/weight、无星 Gold bridge 不改变既定 population contract。
- 本项目战中固定两条 tactical commands、每 battle 3 points；Vivid 的 Gem inventory、uses、multi-turn cooldown、map Gems、五面 Gold Shield 和回合 skip 不属于已确认系统。
- Color/mark 名称、阈值、unit、accessory、Black Witch forms 和数值都是外部内容，不成为第一版素材表。

## 未决问题

- 可访问资料没有完整说明 symbol persistence 是否能主动禁用；Maze IX Dark form 的预规划暗示至少不能方便移除，但本档不发明 toggle 规则。
- 双标签 guide 明确存在 color+mark 例外，但随机 symbol unit 在获取/升级后的永久落点和 seed 规则未完整公开。
- Unit intent、Quick/Preemptive/Battle/Heavy/Counter/Special 的完整 tie order 只有 guide 表格与评论修正，没有官方 deterministic specification。
- Switch/mobile 是否调整 Jeweler odds、触控 Gem timing 或单位 balance 未找到可比较补丁；只研究 Steam 1.1.x–1.2.3。
- 1.3.7–1.3.9 仅有输入/显示修复，不能证明 1.2.3 Maze IX 数值仍完全当前。

## Disposition

- `retained`
- 理由：17 个实质非商店来源覆盖官方 patch、规则教学、双语单位表、具体 symbol build、高难 party、经济与 Boss form；Fool/Shock 与 Maze IX Summon-form 两套队伍均能回到具体单位、双标签、站位、Gems、accessories/bridge 和 counter 核验。
- 研究价值：它提供了非常少见的“重复升级→永久横向 tag、角色可出售、active skill owner 可替换”结构，同时用 Dark-form Boss 暴露永久 tag 不可逆的代价；三种最终 Boss 又分别检验 buff width、后排定时召唤和攻击次数。
- 置信度：官方 patch/版本变化高；1.1.x/1.2.3 guide 的规则与具体发动链中到高；强度排名、经济极值、当前 1.3.9 meta 和跨平台一致性低到中。
