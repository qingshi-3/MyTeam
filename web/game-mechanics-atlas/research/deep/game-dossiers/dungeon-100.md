# Dungeon 100

## 身份与证据边界

- `title_id`: `dungeon-100`
- 类型：单角色自动战斗、九卡技能组装、Boss-ladder roguelite / adjacent autobattler。
- 发行状态：Steam 正式版；可访问的最后一条实质平衡公告为 2023-08-18，之后状态不代表本文规则仍是 current meta。
- 观察时期：2022-12 历史攻略；2023-02–03 大重构/测试期；2023-04-25 后 live major update；2023-07–08 summon/form/Challenge updates；2023-09–11 社区实战。
- 核心证据限制：没有公开完整规则书、卡牌数据库、商店概率或统计站。官方补丁能核验具体卡、触发、羁绊、上限与版本变化；社区帖子能核验玩家实际九卡构筑、层数卡点和顺序经验，但不代表总体 meta。
- 设备/所有权边界：可访问资料证明的构筑载体是职业底盘、最多九张带星级/羁绊的技能卡和 Boss archive；没有证明独立装备/武器/护甲层。本文不把技能卡改称装备。

## 实质来源

| Source id | 页面 | 类型 / 质量 | 主要用途 |
| --- | --- | --- | --- |
| `src-d100-official-2022-12-06` | [2022/12/6_10:42](https://steamcommunity.com/games/1711610/announcements/detail/4979322040033344686) | official-patch / A | Curse Burst、hit/crit trigger、Loot Box–shield 无限环路防护 |
| `src-d100-official-2023-02-24` | [Note about recent updates](https://steamcommunity.com/games/1711610/announcements/detail/5059268543359096053) | official-dev / A | 大改 bug 太多后转 beta、延迟 live |
| `src-d100-official-2023-03-24-test` | [3.24 Test Server Update](https://steamcommunity.com/games/1711610/announcements/detail/5143713753730853754) | official-patch / A | 卡牌/羁绊大重构、删除、星级差、开发者稳定性警告 |
| `src-d100-official-2023-04-25` | [New mode update](https://steamcommunity.com/games/1711610/announcements/detail/5133583099813005693) | official-patch / A | Quick Mode、九槽、经济、完整羁绊、三标签、Trigger Sprite、右侧触发、召唤上限 |
| `src-d100-official-2023-04-27` | [4.27 Update](https://steamcommunity.com/games/1711610/announcements/detail/5133583642650635856) | official-patch / A | 加速伤害丢失警告、旧角色 Boss 防秒杀 |
| `src-d100-official-2023-05-06` | [2023/5/6_8:37 Update](https://steamcommunity.com/games/1711610/announcements/detail/5138087875086380124) | official-patch / A | Treant、Carnivorous Plant、Soul Strike、召唤死亡上限、自身 Boss 1 伤害重做 |
| `src-d100-official-2023-07-04` | [2023/7/4 Update log](https://steamcommunity.com/games/1711610/announcements/detail/5124582150148694284) | official-patch / A | Metamorphosis/Bless ownership、Loot Box 每秒上限、summon bond leak |
| `src-d100-official-2023-07-07` | [2023/7/7 Update log](https://steamcommunity.com/games/1711610/announcements/detail/5124582150158926296) | official-patch / A | 选角/战斗中查看当前 Boss decks |
| `src-d100-official-2023-08-10` | [2023/8/10 Update Log](https://steamcommunity.com/games/1711610/announcements/detail/5839532486212710731) | official-patch / A | Challenge completed-save、Chaos cap、Plant quantity guard、三合一说明 |
| `src-d100-official-2023-08-18` | [2023/8/18 Update Log](https://steamcommunity.com/games/1711610/announcements/detail/5127964289176078284) | official-patch / A | Dragon 无限叠层、form owner、Soul Strike 重复增伤/续命、寻路与 multi-release bugs |
| `src-d100-guide-strange-builds` | [奇奇怪怪的流派](https://steamcommunity.com/sharedfiles/filedetails/?id=2907283448) | strategy-guide / C | 2022-12 九类配方、召唤/近战/远程 counter triangle、性能与护盾 overflow |
| `src-d100-thread-cast-order` | [How to properly arrange spells](https://steamcommunity.com/app/1711610/discussions/0/3781372553309167260/) | community-analysis / D | 左到右施法、短 CD 饥饿、单主技能经验 |
| `src-d100-thread-golem-affordance` | [Feedback about the unintuitive interaction of golems](https://steamcommunity.com/app/1711610/discussions/0/3832043350351005091/) | community-analysis / D | 四魔像直觉与共享 cap 冲突、leftmost replacement、Fusion 失配 |
| `src-d100-thread-boss-class-lock` | [I'm great with the player becoming the enemy but.....](https://steamcommunity.com/app/1711610/discussions/0/3875970367304264881/) | community-analysis / D | class lock、Challenge loophole、post-Aug 九卡 Hunter 与乘算层 |
| `src-d100-thread-bear-druid` | [My first full Legendary build - Bear Form Druid](https://steamcommunity.com/app/1711610/discussions/0/5562542501387597326/) | community-analysis / D | pre-rework Bear/Shield/curse 构筑与 Rogue Mirror counter |
| `src-d100-thread-boss-ladder` | [I simply can't beat my save, any help?](https://steamcommunity.com/app/1711610/discussions/0/3879345097428793263/) | community-analysis / D | 15/30/45/60 层过去构筑 Boss、卡点与替代挑战 |
| `src-d100-thread-shaman-pivot` | [All the way down to the shaman...](https://steamcommunity.com/app/1711610/discussions/0/3810657379791564566/) | community-analysis / D | 2023-07 Shaman shield-trigger 与 curse 两套九卡，Lightning 过渡 |

## 真实循环：把自己过去的成功变成未来试卷

- Classic run 不是同时管理多人阵容，而是选择一个职业载体，在最多九个卡槽中组装主动、被动、trigger、form、aura、summon 和 bond。自动战斗按卡的 cooldown/cast/trigger 关系运行；玩家的主要决策在商店、三合一、星级、卡槽顺序和 Boss 前替换。
- 社区可读 save 把过去构筑列为 15/30/45/60 层 Boss；另一些线程明确讨论 75/90/100 层。玩家一边爬层，一边遇到自己先前保存的角色/卡组，因此一次“成功”会成为下一职业必须解开的持久 capability package。
- 官方 2023-07-07 允许在选角或战斗中查看当前 Boss decks；2023-08-10 又允许上传/挑战已通关 Classic saves。敌人的 exact cards 可见，所以失败可以被解释为具体 trigger、range、body count、damage layer 或 survival layer，而不是隐藏总战力。
- 社区解释 class lock 的设计目的，是阻止直接用同职业同卡组制造镜像 coin flip；但 Challenge Mode 对挑战者不锁创建 Boss 的职业，于是复制原构筑又成为可行 loophole。这里能确认的是玩家观察到的规则矛盾，不是官方胜率结论。

## 九张卡不是九件彼此独立的装备

### 左到右的主施法链

- 2023-02 社区规则是主动技能从左到右寻找可施放项。零/极短 cast-time 技能会持续占用施法机会，使右侧主动卡不发动；长 cooldown 放左、最终 spammer 放右，passive 位置不影响这一基本队列。
- Cyclone 等效果持续时间覆盖自身 cooldown 的卡，实战上接近零空档，也可能吞掉右侧技能。玩家由此总结“一个主伤害技能，其余卡围绕它”的可靠默认结构。
- 这不是完整官方 order specification。官方 2023-05-15 仍修过 Treant form 顺序，2023-08-18 又修 Moonbeam/Frost Nova/Combat Bond triggered multi-release，证明触发层和主动队列不能只靠一条口头顺序概括。

### Trigger card 形成有方向的程序

- 2023-04-25 官方明确：Critical Casting 可触发连接在右侧的所有 active skills；Hit Casting 始终触发右边技能，并且可触发 curse、buff、summon active。这里的左右不是装饰，是可视化依赖边。
- Trigger Sprite 会代表原角色从 sprite 所在位置发动 hit/crit/loot-box/shield-break 等 trigger，但伤害计算仍使用原角色属性；增加 sprite 数提高触发频率而不转移 payoff stats ownership。
- Treant Form 交替施放不同技能、相同技能重置 chain；多个 Treant form 的基础增伤明确为 additive，不允许每张 form 自乘。Dragon Form 则在攻击时释放所有 skills，形成另一条绕过普通队列的节奏。
- 因此卡槽既是容量，也是 directed graph。复制一个 main skill、增加 trigger proxy、增加 attack speed 或增加 multiplicative layer 是不同投资，不能统称“再加一张输出卡”。

## 具名构筑一：post-August Hunter Dragon—Blade 九卡终局

来源时期：2023-10/11，作者明确这是 2023-08 最后实质平衡公告后的 9-card Hunter 终局，且要两张 Legendary Coin 才闭环。

- 职业：Hunter，自带/额外 `+1 Hunter Bond`。
- 九卡：Dragon Form；Curse of Frailty；Blade Storm ×2；Power of Chaos；Attack Combo；Gambler；Coin ×2。
- 目标 bonds：Arcane 3、Druid 3、Combat 5、Warrior 6、Rogue 5、Hunter 5、Chaos 3。
- **driver**：Hunter attack speed + Dragon Form；Dragon 的每次攻击释放全部技能，两份 Blade Storm 相当于再增加一条与 attack speed 乘算的 main-attack copy。
- **state/resource**：九槽、两张 Legendary Coin 的 bond width、Combat bonus attacks、crit、Frailty、Chaos conversion，以及卡片星级。
- **payoff**：Blade Storm 是具体输出 owner；Warrior AOE、Druid attack、Rogue crit/Gambler、Frailty taken-damage、Chaos/general damage、Attack Combo 的 bonus-hit layer 分属不同乘算 prong。
- **survival**：配方没有独立恢复卡，依赖在自动接触前完成爆发；这使其对形成期和前置 Boss 更脆弱。
- **spatial condition**：Blade Storm 需要 AOE 接触；Dragon 在 attack cadence 中触发。不是队伍站位，但敌我距离、接触和 AOE coverage 仍决定首轮输出。
- **形成/转型风险**：作者称 9 卡前几乎没有 DPS，两 Coin 未成型时连 L75 Challenge Boss 也过不了；完全体才在 L90+ 强。它是闭环配方，也是“晚成核心没有过渡职责”的负例。
- **规则误读**：Attack Combo 的 consecutive attacks 指 Combat Bond 产生的 bonus attacks，不是普通攻击次数无限递增。作者把 translation 误导视为多数错误构筑的来源；这是单一专家解释，未升级为官方统计。
- 构筑语法：Dragon/Hunter cadence + bond/crit/curse/Chaos layers + double-Blade Storm payoff + burst-before-contact survival + AOE contact。Payoff owner 是 Blade Storm/角色原属性，不是 Coin 或 Dragon proxy。

## 具名构筑二：pre-rework Bear Druid Shield—basic attack

来源时期：2023-01，必须和 4 月大重构后的 live 规则分开。

- 职业：Druid。
- 九卡：Bear Form；Battle Fury；Lone Wolf；Hunter Aura；Nature Aura；Cursed Energy；Curse of Fragility；Shield Aura；Enhanced Shield。
- **driver**：Bear/basic attacks 与 Hunter Aura attack speed；Battle Fury 将 basic attack 扩到 cleave。
- **state/resource**：Nature 提高 Max Health/Shield，Shield Aura 每秒补 shield，Enhanced Shield 把 shield 比例读成 Attack；basic attack 又通过 Cursed Energy 写入 curse。
- **payoff**：角色 basic attack 是 damage owner，Shield 是防御 state 和攻强 conversion input，Fragility 放大目标承伤。
- **survival**：Max Health、Max Shield 和持续 Shield Aura 在同一 body 上闭环。
- **counter**：回复者用 Rogue + Mirror Image + basic-attack buffs 较容易击败它；四个镜像同时提供额外伤害和 control bodies。另一个回复指出同一九卡放 Rogue 能得到更强 Rogue bond，说明职业是 carrier modifier，不是配方所有权。
- **版本风险**：4 月更新重写 Aura、Nature、Shield 与 card bonds，不能把 2023-01 数值带入 post-April meta。
- 构筑语法：Bear/attack speed + Shield/curse + basic cleave payoff + self-shield/health + melee contact。它直接展示“盾体系如何打伤害”：明确 converter 和单一 payoff owner，而不是让全队盾自动变全队伤害。

## 具名构筑三：post-July Shaman 的两条闭环与过渡

来源时期：2023-07-06/08，位于 7 月 summon/lootbox patch 后、8 月 balance patch 前。

### Trigger—shield—Loot Box 九卡

- Timeshield；Coin；Spell Totem；Trigger Sprite；Bestow；Shield Aura；Shielding Battlecry；Loot Box；Shield Burst；全部 Legendary。
- Spell Totem/Trigger Sprite/Bestow 将 trigger 能力外移但保留原角色属性；Shield Aura/Battlecry 供应 Shield，破盾触发 Loot Box，Shield Burst 提供 damage outlet，Timeshield 为脆弱 trigger body 提供窗口。
- 7 月官方规则限制 Loot Box 每秒最多三次，并修正其错误触发 form；12 月官方已禁止 Loot Box 随机出 shield skills 以避免 shield→Loot Box→shield 的无限环。
- 构筑语法：proxy/Bestow + Shield loss/cooldown + Loot Box/Shield Burst payoff + Timeshield + proxy position。它能迁移的是 owner、rate limit 与 trigger graph，不是随机技能盲盒。

### Lightning 过渡到 Dragon—Curse 九卡

- 过渡：Dual Cast Lightning build 扛早期楼层。
- 终局：Dragon Form；Curse of Agony；Curse of Fragility；Curse of Elements；Curse Outbreak ×2；Enhanced Curse；Balance；Coin；全部 Legendary。
- Dragon 负责发动，多种 curse 是叠层/放大 state，两份 Curse Outbreak 是 cashout owner，Balance/Coin 修复星级与 bond。作者报告用约 80% remaining health 通过自己的 Boss，但这是单个 save，不是胜率。
- 这条路线的设计价值是公开 pivot：早期不是“残缺终局”，而是用独立的 Dual Cast Lightning 完成过渡，等 curse pieces/星级到齐后整体换引擎。

## 召唤、赋予与递归边界

### 2022-12 的开放式召唤链

- 历史攻略把 `pet + Bestow` 重复排列，理论上生成大量 pets；另一条 `Summon Skeleton/Resurrection + gold Bestow + ranged skill + Rat Army + Bestow + Loot Box + Shield Burst + Shield Aura` 让召唤物继续发动 trigger，依靠 body count 推挤近战 Boss、后排安全输出。
- 这不是无代价扩张。攻略直接指出大量 pets 会卡顿且被 AOE 清场；Infinite Lightning 的 projectile/bounce/AOE 又专门以密集召唤为 fuel，形成 summon > melee、AOE > summon、single-target melee > ranged 的作者 counter triangle。
- 本项目不能迁移“生成几千只宠物”。可迁移的是：召唤物是否继承技能、谁提供原始属性、占位/推挤是否合法、AOE 是能力 counter，以及视觉/性能预算必须和规则上限同设计。

### 官方逐层封闭递归

- 2023-04-25：四种 Golem 共用 summon limit；Warlock 只提高明确的 limit/count；Trigger Sprite 有数量上限且只改变触发位置。
- 2023-05-06：Carnivorous Plant 在 summon 死亡时生成，但自身死亡不再生成自身；Soul Strike Spirit 只攻击一次且最多 2；sprite death、golem death 对 trigger/limit 的异常被修复。
- 2023-07-04：Metamorphosis 只吞噬其自己所属的 summons，不再由 Legendary 品质吞全场；Best Friends 只随机强化一个 summon，且修复 summon 继承角色 bond 的 ownership leak。
- 2023-08-10：Carnivorous Plant 不再享受 Warlock summon-quantity bonus；Chaos per-synergy damage 也获得 hard cap。
- 2023-08-18：Soul Strike 不再重复吃 Boss Fight/Curse Boost，且其生存时间不再由 Summon Fusion 刷新；Dragon Form 无限叠加和未变身吃 pet bonus 被修复。
- 这些不是零散 bug：它们共同定义了 owner、child eligibility、shared cap、self-recursion、lifetime refresh、trigger frequency 和 cross-system scaling 六个必须显式的 guard。

### Golem affordance 失败

- Stonehenge 和四种 elemental Golem 的视觉/文本容易让玩家认为应同时组四种；但共享 cap、Warlock 文案和死亡后复制最左侧 Golem，使单一 Golem 反复重召更省槽位。
- Summon Fusion 按“合并而不增加数量”的直觉应允许继续 cast，实战却先被 cap 阻止。官方能确认 shared cap，玩家能确认呈现和组合预期冲突；不能据单帖断言总体强度。
- 设计教训：上限不仅要合法，还要与卡牌形状传达的 intended composition 一致。若“四种组成套”是视觉承诺，共享 cap 不能暗中把最优解压成一种。

## 羁绊、元素与跨职业组装

- 2023-04-25 后，一张卡最多带三个 synergies，同一 synergy 可以重复，不再限定为一职业 + 一辅助元素。职业、damage type、trigger family、summon family 和 card function 因而是可交叉的 axes。
- Physical/Fire/Frost/Lightning 混合卡会在造成一种伤害后追加另一种伤害，并触发相应 elemental bond。Frost 是被击后 taken-damage stacking；Fire 按 triggering skill 当前伤害产生有限频率 AOE；Lightning 加 bounce；Chaos 按其他 synergy width 增伤并在 8 月加 cap。
- Shield/Nature、Warrior AOE、Combat repeat、Arcane trigger frequency、Hunter attack speed、Shaman cast speed、Druid pet attack 和 Warlock summon count 各修改不同 channel。一个 card 的第二/第三标签可做 bridge，不必让“职业”和“元素”合并为同一体系。
- 历史 Bear 配方能从 Druid 移植到 Rogue；post-Aug Hunter 作者也把 Multiplicative layers 按 effect prong 而非职业分类。可迁移原则是职业提供倾向和 access cost，技能图决定实际引擎；但本项目仍以英雄固定技能/装备/遗物为主，不接受九张完全自由卡槽。

## Floors、商店、替换与转型

- Quick Mode 官方规则是 30 floors、全屏 swarm、较短 battle、每轮固定 5 Gold、每 10 Gold 产生 1 interest、最多 5，shop upgrade cheaper，开局解锁全部九卡槽。它是明确的短局经济变体，不能定义 Classic 的全部 shop curve。
- 三合一 reward 和 collection UI 到 2023-08 才能同时查看 synergy/card descriptions，说明升级选择不仅比较星级，还要看 card 在 directed trigger/bond graph 中的贡献。
- post-Aug Hunter 是反面过渡案例：两 Coin/九卡未齐前无输出。Shaman 线程则给出正面 pivot：Dual Cast Lightning 独立完成早期，再换 Curse engine。后续设计应要求终局卡至少能由一个可玩的 transition shell 接近，而非九件齐全才启动。
- Boss ladder 让替换带双重代价：新 build 要击败过去 build，又可能把更难的 capability 写进未来 save。社区甚至建议当玩家败给下一 Boss 时，用途中较强构筑回写较低层 Boss；这只是提议，不是当前规则。

## Past-character Boss、反制与失败解释

- 可读 save 的四个 Boss 已显示每层可考不同能力：L15 Fire/elemental trigger、L30 Frost projectile/Convolution、L45 Hunter summon/mirror/projectile、L60 Rogue crit-trigger/curse/boomerang。作者称 L30 常成 run ender、L45 易过、L60 未破，证明难度不随 floor/level 单调等价于实际 counter coverage。
- Bear Druid 被 Rogue Mirror Image + basic buffs 以更多 bodies/control 绕过；历史 summon 被 AOE/ranged 清除；近战 curse/one-hit 又被 body wall 阻挡。Boss 设计价值在保存“技能图与能力”，不是只保存总战力。
- 失败报告至少要显示：谁实际 cast、触发链被哪张短 CD 卡饿死、哪个 proxy 继承谁的属性、召唤 cap/child eligibility、damage layer 是 additive 还是 multiplicative、Boss deck 当前九卡与职业、战斗倍速是否丢事件。

## 失败、重做与明确负面案例

以下八组计入跨游戏明确负面/重做深度：

1. **Loot Box—Shield 无限环**：2022-12-06 官方禁止 Loot Box 随机施放 shield skills，明确理由是避免 infinite loops；2023-07-04 又限制 shield-loss Loot Box 每秒最多三次。
2. **召唤递归与归属外泄**：Golem 共用 cap、Plant 不自生且不吃 quantity、Spirit 最多 2/只打一击、Best Friends 不再泄漏角色 bond，多个版本连续修正 child eligibility、shared limit 和 owner。
3. **Dragon Form 跨层无限增伤/错误继承**：2023-08-18 修复无限叠加、未变身仍吃 pet bonus、Gold/Legend bonus、Deadly Rhythm/Time Shield trigger 等一组 owner/state bugs。
4. **Soul Strike 重复放大与续命**：同一 8 月补丁禁止重复获得 Boss Fight/Curse Boost，并禁止 Summon Fusion 刷新生存时间，封住 damage/lifetime 两条递归。
5. **旧角色 Boss 防秒杀变成 1 点伤害僵局**：4 月先提高双方 damage reduction，5 月改为 Max Life/Shield enhancement，官方明确为避免只能造成 1 damage；life/shield-based damage 同步下调以保持 normal damage consistency。
6. **倍速改变结果**：4 月官方给 Fast Mode acceleration 增加“可能丢失伤害”提示；更早补丁也因 4x damage loss 调整 projectile speed。这是模拟步进/投射物事件未保持结果不变的负例。
7. **大规模技能重构失稳**：3 月 test patch 删除多张卡、重写 aura/bonds/星级并承认改动未完全记录、bugs 多、difficulty 可能异常；2 月已因开发版不稳定把更新转入 beta 1–2 个月。规则迁移和兼容不能靠未版本化热改。
8. **四魔像套装暗示与共享 cap 冲突**：官方 shared cap 与社区实战共同显示，Stonehenge/四元素形状鼓励的“多种魔像”被最左单种复制压过；这是组合 affordance 与合法上限不一致，不是单纯数值弱。

另记录但不重复计数：Curse stacking removal、Area Increase/Enhance synergy 删除、Chaos width cap、Balance/Gambler/Hand of Fate 星级重做、Treant order、pathfinding 与 multi-release 修复。它们是重要 lifecycle，但公开材料未逐项给出 dominance 或失败原因。

## 对本项目可迁移

### 可迁移原则

- **构筑必须明确执行图**：driver、state、payoff、survival、spatial condition 之外，还要能画出谁触发谁、方向、cooldown、owner 和 rate limit。卡槽顺序不应靠玩家猜。
- **职业是载体修正，不是封闭技能包**：同一 Shield/basic recipe 在 Druid/Rogue 表现不同，说明 hero identity 可改变 access、倍率或过渡，而不要求每个体系只有一个固定英雄。
- **盾输出必须有 converter 与 owner**：Shield Aura/Nature 供应并储存；Enhanced Shield 读 Shield 变 Attack；basic/Bear/Shield Burst 是 payoff。不得把“全队有盾”直接等同“全队都高伤”。
- **召唤系统预先列六类 guard**：source owner、child eligibility、shared cap、self-recursion、lifetime refresh、trigger-rate。临时单位仍必须服从本项目 18 格物理占位和 persistent defeat rule。
- **旧构筑 Boss 保存 capability，不保存模糊总分**：预览 exact skills、触发关系、range/body/counter tags，让玩家能用不同职业/英雄体系解题。
- **过渡构筑必须独立可玩**：Shaman 的 Lightning→Curse 比九卡 Hunter“闭环前无 DPS”更健康。招募/替换界面要显示失去哪条 trigger/bond/prong。
- **倍速必须结果不变**：Dungeon 100 的 4x 丢伤害是明确反例；本项目接受的 0.8/1.6/3.2 simulation scales 必须保持 deterministic outcome 和事件归因。

### 不可直接迁移

- 本项目是多英雄 spatial roster，不是单角色九张自由技能卡；不能照搬 Classic/Quick shop、三合一、职业锁和每卡三羁绊。
- Dungeon 100 没有可核验的独立装备层；不能用它证明“装备应该有九槽”或把卡牌星级当装备稀有度。
- Challenge Mode、玩家 save 上传、PvP Battle Mode 与镜像 coin flip 依赖异步/多人生态，不是本项目 authored PvE 的默认规则。
- 具体卡名、羁绊名、星级、百分比、15/30/45/60/75/90/100 楼层和职业数值均不成为项目内容。
- 随机 Loot Box、无上限 Bestow chain 和跨卡任意 trigger 容易产生不可读递归，不适合作为本项目 limited tactical commands 的基础。

## 未决问题

- 2023-08 后没有实质平衡公告，无法确认论坛 10/11 月 post-Aug build 是否与所有线上 hotfix 完全一致。
- 社区能观察 left-to-right starvation，但没有官方完整说明 passive、normal cast、triggered cast、form、summon proxy 和 simultaneous resolution 的总顺序。
- Classic 的完整 shop odds、Gold income、slot unlock cadence、Boss 保存/替换精确规则和 class-lock 生命周期没有公开规则页。
- `Share your decks`、多份求助帖只给外链截图；本档不 OCR，因而无法复原成功 counter build。
- 召唤物是否真正占用碰撞/格位、push 的确定性和最多实体数没有正式 specification；历史攻略只证明玩家观察到 body wall 与性能压力。
- 没有可核验的 equipment/relic ownership；后续跨游戏综合不能把缺席的数据当“没有装备是成功设计”。

## Disposition

- `retained`
- 理由：17 个实质非商店来源覆盖官方规则/补丁/开发说明、历史策略 guide 和六个独立实战讨论；三套九卡终局、一个明确 Lightning→Curse pivot、召唤递归 guard、past-build Boss ladder 与八组 failure/rework 均能回到正文核验。
- 研究价值：它把“技能自由组装”具体化为 directed card graph，而不是商店口号；同时展示 Shield converter、跨职业 carrier、旧构筑 Boss、召唤 ownership/cap/recursion 和倍速结果漂移的正反案例。
- 置信度：官方 patch 结构高；2023-04 后 card/bond/guard 中到高；社区具体 build 与 counter 中；整体强度、当前 meta、商店概率、完整顺序和独立装备层低或未知。
