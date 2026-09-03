# Gladiator Guild Manager

## 身份与研究时期

- `title_id`: `gladiator-guild-manager`
- 类型：单人公会经营 / roster autobattler。玩家沿时间线经营建筑、商店、工资、声望和任务，培养并装备可长期留存的角斗士；每场先看敌人、选参战人数、站位和目标优先级，然后自动战斗。
- 发行状态：2021-10-13 Early Access；2024-06-22 发布 1.0；本档案观察 2021–2023 EA 指南、2024-02 的 0.941/0.942、2024-06/07 的 1.0–1.036，以及 2025-06 的 Glory Mode。
- 版本边界：EA Tournament 指南的无限等级敌人、旧 Shaman 前冲行为和固定 perk 获取方式不等于 1.0；1.0 已重做 Trait 与 Universal Item；v1.034 又修改 Taunt、AOE、Shaman、Extrovert 与商店单位等级；2025 Glory Mode 改变失败/跳过次数。所有构筑均带时期。
- 直接游玩：本批未购买或安装游戏；规则来自官方公告、四份可读 Steam Guides 和两条具体讨论。商店页只用于身份确认，未进入证据。

## 检索日志

1. 通过 Steam official news API 读取 0.941/0.942、1.0、1.034、1.036 与 2025 Glory Mode 正文，重点核验 Trait/Item 重做、Enemy Items、After Battle Stats、Priority Points、Taunt/AOE/Shaman/Extrovert 和时间失败条件。
2. 读取 `Campaign starter guide (game v1.0)` 全部章节，核验 Rhino—Archer—Pyromancer—Shaman—Banshee 队、属性/Traits/Items、目标优先级、视线/弹道、危险战斗、建筑与资源顺序、冠军赛人数、复活与换人。
3. 读取 EA Tournament guide 作为历史对照，核验三 Summoner / Rhino / Pyromancer / Priestess / Hookbot 组合、极端敌人、Stamina drain 与旧 Shaman 行为；不把它升级为 1.0 当前强度。
4. 读取 Attribute Guide，核验 STR/AGI/INT 通过具体能力公式生效、Health/Stamina/Movement 的职责、execution/cooldown cap、Knight Shield Block 和 Berserker range band。
5. 读取 2023–2025 Achievement Guide，核验攻击类型、装备/trait interaction、敌人带装备标记、弹道 body-block、任务/Boss 限制、permadeath、时间线与优先级操作。
6. 读取 Priority Points 与 team combos 讨论，核验 sliders 只能在开战前设置、模板与逐敌 1–10 权重、集火/分摊选择，以及 Rhino+summoner、Hookbot/Boom Bot/Necromancer、Warlock 与 Executioner 的具体空间组合。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-ggm-official-1-0` | [Full Release Now Available!](https://steamcommunity.com/games/1043260/announcements/detail/5759622039984099493) | Entertainment Forge，2024-06-22 | official-dev / A | 1.0 Trait/Item/Guild/Timeline/Reputation 重做与设计原因 |
| `src-ggm-official-0-942` | [After Battle Stats Out of Beta](https://steamcommunity.com/games/1043260/announcements/detail/5679673637659376536) | Entertainment Forge，2024-03-09 | official-dev / A | 战报、Enemy Items、trait redesign 方向 |
| `src-ggm-official-1-034` | [Priority Points QoL Improvements v1.034](https://steamcommunity.com/games/1043260/announcements/detail/5842939267353705854) | Entertainment Forge，2024-07-09 | official-patch / A | 多选优先级、人数/站位选项、Taunt/AOE/Shaman/Extrovert 与错误修复 |
| `src-ggm-official-1-036` | [V1.036 balancing adjustment](https://steamcommunity.com/games/1043260/announcements/detail/5842939267356363990) | Entertainment Forge，2024-07-10 | official-patch / A | Sage's Sacrificial Diadem 次日回滚 |
| `src-ggm-official-glory-2025` | [Glory Mode Feature Added](https://steamcommunity.com/games/1043260/announcements/detail/1801617199423014) | Entertainment Forge，2025-06-06 | official-patch / A | 无限时间/刷取问题与 1–7 次冠军赛失败/跳过限制 |
| `src-ggm-steam-campaign-v1` | [Campaign starter guide (game v1.0)](https://steamcommunity.com/sharedfiles/filedetails/?id=3276437022) | ink，2024-06-27 / 07-09 | strategy-guide / C | 完整 1.0 队伍、属性/trait/item/站位/优先级、资源与转型 |
| `src-ggm-steam-tournament-guide` | [How to rank in tournament mode](https://steamcommunity.com/sharedfiles/filedetails/?id=2841492609) | Bardamu，2022-07-31 / 2023-01-16 | strategy-guide / C | EA Summoner 队、极端敌人、Hookbot、Stamina drain 与旧行为 |
| `src-ggm-steam-attribute-guide` | [The Gladiator Attribute Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2629315717) | mahrgell，2021-10-16 | strategy-guide / C | 能力公式、cap、Stamina、range band、Shield Block |
| `src-ggm-steam-achievements-guide` | [100% Achievements Guide and Game Tips](https://steamcommunity.com/sharedfiles/filedetails/?id=2938948447) | Shindragan，2023-02-27 / 2025-02-20 | strategy-guide / C | 任务/Boss/难度、敌人装备、弹道、优先级、permadeath 与时间线 |
| `src-ggm-steam-priority-thread` | [Priority Points](https://steamcommunity.com/app/1043260/discussions/0/4408543140361664435/) | 玩家讨论，2024-07 | community-analysis / D | 开战前 sliders、模板/逐敌权重、集火与分散 |
| `src-ggm-steam-team-combos` | [Fun & strong gladiators / team combos?](https://steamcommunity.com/app/1043260/discussions/0/4408542785995967847/) | 玩家讨论，2024-07 起 | community-analysis / D | Rhino+summoner、Hookbot爆破、Warlock、Executioner 与 Spearman 单例 |

## 真实循环与玩家决策

### 时间线与公会经营

- 时间持续推进，商店、折扣、普通任务、月底 Championship 和 Ring of Death 以事件出现。1.0 可为重要事件配置 auto pause、是否自动打开面板，以及关闭面板后是否继续时间；这属于经营时间线控制，不是战斗中的技能指令（`src-ggm-official-1-0`）。
- Guild building 决定是否能招募某职业、重置六项主属性、教学/升级 Trait、升级 Universal Item 和扩大 roster。Trait Blueprint、Crafting Tools、Gold、Mana Crystals、木/石/铁与工资形成不同瓶颈（`src-ggm-official-1-0`, `src-ggm-steam-campaign-v1`）。
- Championship 由多场不同人数限制的战斗组成。1.0 指南记录从 3 人逐渐扩到 12 人的序列，并建议保留少量备份而不是平均投资所有人；最难和最易战斗给关键 Trait Blueprint，先后顺序因此是资源决策。
- 声望任务常同时提高一个 faction、降低另一个；高声望给 item 或带高质量 Traits 的 units，低声望会触发抢劫/追击。拒绝任务两天后再来一个，允许围绕派系路线和当前 roster 主动跳过（`src-ggm-official-1-0`）。

### 三层职责边界

1. **单位级行为配置**：开战前可选 ranged/support/frontline 模板，或给逐个敌人设置 1–10 priority；部分支援单位同时有 friendly/enemy sliders。它决定自动 AI 优先考虑谁，不是强制锁定，也不替代射程、最低射程、视线和移动合法性。
2. **赛前战术配置**：选择本场人数、从 roster 派谁、布置起始位置、装备/consumable、priority、是否接受预计复活成本，或查看敌方站位后换 fight。v1.034 可保存同人数阵型、清空队伍并多选单位批量调 priority。
3. **自动战斗本体**：开战后单位自行移动、选目标、支付 Stamina、执行 ability、block/dodge、召唤与重选目标。可访问来源明确说 individual sliders 只能在战前设置；没有证据支持一套战中可花资源的独立 tactical command。不能把赛前 priority UI 当成本项目的战中指令层。

## 具名 1.0 队伍：Rhino—Archer—Pyromancer—Shaman—Banshee

来源时期：2024-06/07 的 1.0 指南；v1.034 已在数周内继续削弱若干组件，因此是版本结构而非当前数值配方。

### 开局与招募

- 初始核心：Archer、Rhino、Shaman；作者建议重开直到初始建筑选择含 Pyromancer。
- 成型职责：Rhino 主坦/群体 Taunt；Archer 单体 alpha strike；Pyromancer（或 Dark Mage）处理群体；Shaman 兼顾治疗、召唤与伤害；Banshee 负责高威胁 stun。无 Banshee 时在约 3.5 月的有限窗口拿两名 Marauder；Cryomancer 是广域 freeze 备选。
- 资源早期只升四个核心职业建筑，mid/late 才拿 Trait Crucible、Banshee/Cryomancer；每个职责只重投一名，最多一名 backup，避免蓝图稀释。

### Rhino：生存与聚怪所有者

- 属性：足够 Stamina 释放 2–3 次 Taunt；必要时 Strength 扩大范围；其余 Health。第二 Rhino 可应对双敌群，偏纯 Health。
- Traits：Swift 防击退/位移；Revitalizing Echo 把所受伤害转为全队 Stamina；max Hold My Beer 跳入敌群并 Taunt；Absolute Guardian's Veil 在开局数秒吸收全队伤害。
- Items：Book of Invincible Shield 是核心；按死亡情况补 Health/Stamina/Strength，不为微小提升过早升级。
- 作用：把分散敌人聚到 AOE，保护 Archer/Shaman/Pyro 的第一轮；Rhino 是 target-control/survival owner，不是主 damage owner。

### Archer：远程高威胁删除

- 属性：满足不死的 Health、5–10 发所需 Stamina、少量 Intelligence 达 cooldown，余下 Agility；主张用装备和重置达到阈值而非平均加点。
- Traits：Serial Killer / Practical Learner 或 Extrovert；Critically Lucky；Greed is Good；Unyielding。装备优先 Swiftfoot Shackle、Hare's Gambit Greaves、Necklace of Recklessness 等 Agility/伤害倍率。
- Priority/站位：对最大 ranged/AOE threat 设 10；确保 line of sight、箭不会被其他敌人挡，也不要远到目标移动导致 projectile miss。普通 ranged/support priority 高，但 Pyro/tank 不照抄同模板。
- 作用：在敌方 Pyromancer/Archer 首轮前删除它；Archer 是单体 payoff owner。

### Pyromancer / Dark Mage：聚怪后的 AOE payoff

- Pyromancer 先到 50 Strength 扩范围，再投 Intelligence 让 burn 成长；Stamina 支撑 2–3 cast；之后才考虑 Agility 达 execution/cooldown cap。
- Traits 采用 Serial Killer/Extrovert、Greed is Good、Unyielding；items 用 Intelligence、Sage's Sacrificial Diadem，缺控时可临时带 Orb of Frost AOE。
- Rhino 聚怪后才有高覆盖。高等级敌人分散或 backline 先手时，Pyro 不能替代 Archer；敌人自己带强 AOE 时，Extrovert deathball 反而必须散开。

### Shaman / Banshee：续航、召唤与控制

- Shaman 以 Intelligence 放大 totem 等级/治疗/伤害，另配足够 Stamina；Extrovert、Ascending Summoner 和 Greed is Good 提供成长。v1.034 已因其改为后排持续召唤后过强而提高召唤 Stamina 成本。
- Banshee 补 Health/Stamina 和部分 Intelligence，以多次 instant ranged stun 压住一名超高等级威胁；Instructor 放在这种非主 carry 的常驻功能位，帮助核心升级。
- 多个大威胁时可从不同角度布置两名 disabler，或让 Archer 删除第三个；Boss/megaboss 可能免疫 stun，不能把 Banshee 当通用答案。

### 构筑语法

- Engine：Rhino Taunt 聚怪 + 开局保护；Archer/Pyro 的 priority/射线与 AOE；Shaman totem；Banshee stun。
- State/resource：Stamina、Health、ability cooldown/execution、main stat、Trait tiers、item multipliers、Gold/blueprints 与 enemy priority。
- Payoff：Archer 删除远程威胁，Pyromancer 清聚集敌人；二者为不同 payoff owners。
- Survival：Rhino/Absolute Guardian's Veil、Shaman healing/totems、Book of Invincible Shield 和必要 consumables。
- Spatial condition：Rhino 覆盖敌群；Archer 视线/弹道不被挡；Pyro 命中聚集目标；Shaman/Banshee 与 enemy AOE 保持合适距离。
- Pivot/counter：敌方单核用 Banshee/priority 10；多个远程威胁用 backup disablers/Archer；invisible 敌人用列阵让 projectile 穿过或 Taunt/AOE；1vX 尽量跳过；AOE 敌人迫使 Extrovert deathball 散开。

## 补充构筑与生命周期样本

### 1.0 后已削弱的 Shaman cluster speedrun

- 起始 Shaman/Rhino/Banshee，核心为 Swift Rhino + 2 名 Extrovert Shaman；Shaman 加够两次 pillar 的 Stamina、少量 Health、余下 Intelligence，后续加第三/第四 Shaman。
- Shaman 使用 Extrovert—Ascending Summoner—Better Together，Rhino 使用 Revitalizing Echo—Hold My Beer—Absolute Guardian's Veil；Banshee 在高等级敌人出现时补入。
- cluster 依赖相邻同类/友军，既放大召唤又把全队暴露给 enemy Pyro/Dark Mage AOE。指南明确标记策略自 1.0 后被 nerf，并建议最终按掉落的 %INT/%AGI items 转向 1–2 名 Pyromancer/Archer hypercarry，而不是永久横向加 Shaman。

### Hookbot / Boom Bot / Necromancer

- Hookbots 站 frontline，Boom Bots 放在旁边且与 Hookbot 设同一目标 priority；Hook 把 backline 拉入爆炸，Necromancer 复活 Boom Bots 或第一轮死亡单位，其他人收尾。
- 它依赖空间拉拽、目标一致和尸体/复活顺序；社区明确说对 Boss 或 Ancient Doors 无效，因而是 encounter-specific fun build，不是通用 meta。

### Executioner “double Hold My Beer”

- 一名 max Hold My Beer + Guardian Veil 的坦克跳进敌群并广域 Taunt；满 Intelligence Executioner 带低级 Hold My Beer 跳到同一位置，再以短射程 AOE 爆发。社区作者给出一次 final arena 35 万对比其他人约 1 千的报告。
- 它说明空间桥接可以把“短射程高倍率”变成 payoff，但 35 万是单例；Executioner 脆弱且不能杀 Boss，Guardian Veil/另一个 tank 仍是生存依赖。

## 属性、装备、Trait 与成长

- STR/AGI/INT 本身不提供统一属性，而是写入每个 ability 的具体 damage、execution、cooldown、range 或 duration 公式。Health、Stamina、Movement 是通用轴；攻击/技能消耗 Stamina，耗尽时单位会停滞（`src-ggm-steam-attribute-guide`）。
- 很多效果有 cap。Knight 的少量 INT 可把 Shield Block cooldown 从 1 秒降到 0.15 秒，使格挡频率约七倍；继续堆 INT 则无益。构筑必须显示“下一个阈值”而不是只比较总战力。
- 1.0 以前随机 Traits 迫使玩家反复刷新并可能花数千 Gold 找组合；1.0 改为 Mana Crystal + Trait Blueprint 教学和升级，每 6 level 解锁新 tier，也允许高 tier slot 学较低 tier Trait。shop 自带好 Traits 仍能节省蓝图。
- Universal Item 过去同时随机 green/blue/purple，导致 green 基本被忽略；1.0 改为 shop 只出 base item，再通过 Workshop 和 Crafting Tools 升级。普通 stat items 仍有 level，且可以在换人时传给新 owner。
- 装备效果与能力类型严格配对：AOE、direct、projectile、melee 会决定 burn/freeze/block 等触发。例：给会 Taunt 的 gladiator 教 AOE Taunt Trait，再装备“AOE 使敌人着火”的 item，Taunt 也触发该装备（`src-ggm-official-1-0`）。
- 1.0 指南反复建议“handoff”：早期纯 stat item 传给后续 hypercarry；临时 Orb 控制在 Banshee/Cryomancer 就位后换成倍率；shop 出高等级功能位可替换旧 backup，但已投入的 core 3 不因 level 差自动卖掉。

## 敌情观察、任务限制与换人

- 0.941/0.942 给 Championship/Tournament 敌人装备，并相应降低敌方 level 保持总强度。目的不是单纯加数值，而是让玩家识别带危险 item 的 owner 并优先消灭（`src-ggm-official-0-942`）。
- Achievement Guide 说明敌人 level 显示变绿代表至少一件 item；projectile 会被前方单位吸收，因此 squishy 应在 tank 后、Shaman totem 可作 Pyro 的 cover。观察、射线与 priority 必须一起解释失败。
- Championship/quest 会限制参战数并出现 1vX、many-v-1、Boss、invisible、weather 或 faction-exclusive 路线。Banshee 对普通单核有效但 Boss 可能免控；Mana Tower 固定 weather 会覆盖 martyr 构筑；某些任务在推进 arena 后可能错过。
- Permadeath 下阵亡是永久的；普通模式可花 Mana Crystal/等待复活。指南因此允许在小战给脆皮 HP potion、在大赛预计会掉人时准备 crystals，或者开战前 surrender/skip。这个项目没有自动继承 permadeath/工资/复活经济。
- 2025 Glory Mode 让玩家选只能失败/跳过 1–7 次 Championship；官方理由是旧规则即使最高难度也有无限时间，可持续 grind 穿过。它把“失败后无限刷回去”改成明确的 run loss budget。

## 战报与归因

- 0.941/0.942 After Battle Stats 显示 damage dealt/taken、healing 等，并把 summoned units 折叠到 summoner 名下；可展开每个 summon，区分 summoner 自身与召唤物贡献。
- 官方称该界面让玩家比较 item、识别最高 damage/tanking/healing 和每名 gladiator 的重要性。它直接服务于 roster replacement，而不是结算装饰。
- “Total Score” 把伤害、承伤、治疗相加容易混淆职责；本研究只采纳分项与 source hierarchy，不把总分作为单位价值真相。

## 失败、重做与明确负面案例

以下六组计入跨游戏明确负面/重做深度：

1. **随机 Trait 商店挫败**：官方称玩家可能花数千 Gold refresh 仍找不到理想组合；1.0 改成 Blueprint 教学/升级、每 6 level 开 tier，并优化 shop trait allocation。
2. **Universal Item 低稀有度死选项**：green 版本会被无视、等待 blue/purple，使 shop 出现 green 只像干扰；1.0 改为只出 base，再由 Workshop 主动升级。
3. **Taunt/AOE 统治与目标合法性**：ranged unit 被近身 Taunt 时因最低射程不攻击；v1.034 让其先移动到合法距离仍优先 Taunter，同时因 Taunt/AOE 被报告为最 OP 而削范围/时长/cooldown，并区分 Rhino 与 Drummer。
4. **Shaman 后排行为与 Summoner/Extrovert 放大**：Shaman 从前冲改成留后排召唤后强度大涨；v1.034 把 totem Stamina cost 50→80，并把 Extrovert cap 设为 125% 以防多 Summoner abuse。
5. **Sage's Sacrificial Diadem 次日回滚**：v1.034 将 50/120/200% 改到 50/75/100%；v1.036 次日承认 nerf 不合理，恢复为 100/150/200%。数值甚至与“original”表述存在不一致，证明热修不能被平均成稳定规则。
6. **无限时间消解最高难度**：官方称旧系统可无限 grind，无论最高难度最终都能推进；Glory Mode 引入 1–7 次 Championship lose/skip 预算，首次让时间/失败成为终局资源。

其他生命周期：After Battle Stats 从未规划的缺口发展到召唤分层归因；v1.034 商店单位等级更贴近现有 roster，明确为减少“不断买新单位”；position saving 和 random team sizes 被做成可选项。这些作为可读性/便利改进，不重复计为失败案例。

## 社区观察边界

- 1.0 guide 是单作者方法论；对 Archer、Pyro、Shaman、Banshee 的 S/F 评价不是统计。其价值在于组件、阈值、站位、换人和失败条件完整。
- EA Tournament guide 的 level 1000 boss、Energy Vampire lock 和 Summoner 强度是历史玩家观察；1.0 Traits、AI 和 balance 已重做，不用于当前强度。
- Hookbot/Boom Bot、Executioner、Spearman、Warlock 均为社区单例。保留真实发动方式和明确无效 encounter，不保留“最强”排名。
- 没有公开 patch-level class pick/win rate、trait adoption 或 Boss matchup 数据库；官方“reported OP”只按开发者平衡依据记录，不扩成玩家总体共识。

## 对本项目可迁移

### 可迁移原则

- **行为策略和战中指令必须是两种资产**：赛前 priority profile 可长期绑定英雄，战中 tactical command 仍消耗共享点数；不能让两者共用“指令”一词而隐藏成本。
- Priority 只改变候选排序，不保证穿墙、无视最低射程或弹道；选择 UI 应预演 line of sight、body block、合法射程和目标权重。
- 盾体系可以由 tank block、block→heal、block→AOE fire、team opening protection 与远程 carry 组成；盾供应、控制、伤害 reader 和 payoff owner 不必同一英雄。
- 属性成长应显示 ability-specific 阈值与浪费：Stamina 支撑几次 cast、INT 是否已到 cooldown cap、STR 是否只扩大 Taunt/AOE、AGI 是否影响命中/执行。
- 装备和 Trait 是可转移/可教学但有材料成本的塑职层。replacement 应区分 core sunk cost、临时功能位、可 handoff 装备和 shop 自带好 Trait 节省的资源。
- 敌人带关键装备、固定 weather、人数限制、免控 Boss 和 backline nuker 是可预览的 enemy package，不应靠隐藏总战力表达。
- 战报既要按职责分项，也要保留 summon→summoner hierarchy；不要用 damage+tanking+healing 的单一总分决定淘汰。
- 失败预算能阻止无限刷取抹平难度，但本项目应优先通过塔层推进、有限恢复和可解释的 run resources 实现，而非照搬工资/月份。

### 不可直接迁移

- 项目已确认战中有两条独立 tactical commands 和 3 points；GGM 的赛前 sliders 不能替代它们，也不授权加入逐敌 1–10 微调 UI。
- GGM 长期工资、建筑解锁、按月 Championship、无限或 Glory 时间线、faction 声望和 Mana Crystal 复活不是本项目既定系统。
- GGM 可拥有二十名以上 roster、12 人任务和部分 permadeath；本项目普通 endgame 是 10、物理上限 18，战败/复活契约不同。
- Extrovert、Greed is Good、Absolute Guardian's Veil、Hold My Beer 和具体百分比只是外部内容，不是候选数值表。

## 未决问题

- 官方没有给出完整 priority 算法：同分 tie、路径成本、当前目标黏性、重选时机以及 template 与逐敌 slider 的合并顺序仍不明。
- 可访问资料没有证实任何战中独立 tactical command；只确认赛前 priority、position、items/consumables 与开战后自动执行。
- v1.034 的 Diadem 旧值写作 50/120/200%，v1.036 又称恢复“original”100/150/200%；研究保留冲突，不选一个当历史真值。
- 2025 Glory Mode 的后续平衡、玩家完成率和最佳失败次数无统计资料。
- 1.0 指南很快遭多轮 50% 级平衡调整；结构可复用，确切 item/trait 数值不可当当前规则。

## Disposition

- `retained`
- 理由：11 个实质非商店来源覆盖官方设计/补丁、四份功能不同的攻略与两条实际讨论；一套完整 1.0 队伍满足职业、属性、Traits、Items、priority、站位、payoff owner、转型和 counter 要求，并有 EA 与 2025 生命周期对照。
- 研究价值：它把“羁绊/标签”之外的单位能力公式、赛前行为权重、任务人数、敌方装备、可传递装备和重做后的成长经济连成一条可操作构筑链，是 Steam 长尾样本的重要补充。
- 置信度：1.0/patch 规则和明确重做原因高；1.0 guide 的操作结构中；社区强度排名、EA tournament 极值与 Glory 后续结果低到中。
