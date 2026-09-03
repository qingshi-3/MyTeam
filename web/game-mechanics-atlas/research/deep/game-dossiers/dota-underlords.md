# Dota Underlords

## 身份、范围与研究深度

- `title_id`: `dota-underlords`
- 开发 / 发行：Valve；Steam App 1046930。
- 类型：共享英雄池、自动战斗、联盟阈值与站位的多人自走棋；这里作为历史锚点，不把仍可访问的商店/社区页面解释为仍有活跃平衡。
- 时期：2019-06 公开 Early Access；2019-10-24 Big Update；2019-12-18 Jull-tide 架构重做；2020-02-25 Season One 正式版；2020-08-27 New Blood；2020-11-19 为 199 条公开 News 中找到的最后实质平衡节点。
- 范围：以 New Blood 后、最后平衡期 Standard 为四套主构筑的共同语境；Big Update / Jail / Ace 仅作历史生命周期。Duos、Knockout、Classic、Freestyle、bot/offline 单独标记，不与 Standard 共用 meta 结论。
- 深度：47 个实质来源，覆盖官方补丁/开发说明、历史 Wiki、完整构筑攻略与社区反应；26 条标准化 evidence。四套末版构筑和一套补充线的 engine、资源、owner、经济、站位和 counter 均可闭合，因此 disposition 为 `anchor-retained`。

## 来源包

| 功能 | 数量 | 内容 |
| --- | ---: | --- |
| 官方版本 / 补丁 | 13 | Ace、Big Update、Jail 校正、Jull-tide、Season One、New Blood、共享池、rate limit、召唤裁决与最后平衡节点 |
| 历史 Wiki 规则 | 27 | Gold/Level/Shop/Pool，Standard/Duos/Knockout，Underlord/Item，攻击/Mana/状态/防御轴，十二个关键联盟 |
| 完整构筑攻略 | 2 | 2020-12 末版十套构筑；2019-08 Pre-Big Update 历史分支 |
| 社区机制 / 生命周期讨论 | 5 | Jail 日常 meta、Jull-tide 删除反应、共享池操作、Brawny 成长经验、后续更新空白 |

所有 source id、URL、版本限制与可访问性见 `../source-index.md` 的 Dota Underlords 段。未找到版本、模式与抽样框都明确的代表性胜率/采用率统计，因此不给构筑排名。

## 版本和模式地图

| 节点 | 可用语义 | 不可跨界外推 |
| --- | --- | --- |
| 2019-08 Ace / Pre-Big Update | Ace 封顶、Contraption 不占 unit cap、旧联盟分支 | 旧 Scrappy/Inventor/Elusive/Primordial/Alliance item 不是 Season One 末版内容 |
| 2019-10 Big Update | 赛前选 Underlord，Underlord 作为战斗单位，有 Hype/Talent/成长；Duos/Freestyle/Jail 进入 | Jull-tide 后 Underlord 获取和资源架构已替换 |
| 2019-12 Jull-tide | HP 从 Big Update 的 2x 回调至原始值 1.5x；Standard/Duos 流程、物品、Underlord 时机重做 | 当时 7 Gold/回合和删 creep round 是历史节点，不覆盖 Wiki 最终快照 |
| 2020-02 Season One | 离开 EA；Scrappy/Inventor 离场；Summoner/Vigilant/Void/Hat 进入 | Beta Guide 不能证明 Season One 强度 |
| 2020-08 New Blood | roster/alliance/item 大规模重排；Classic 恢复 creep waves 并移除 Underlord；旧新客户端分流匹配 | Classic 不是同期普通 Standard；旧客户端不可与新 roster 混玩 |
| 2020-11-19 后 | 末版 Brute/Scaled/Troll 与英雄数值；2020-12 Guide 的具名构筑 | 最后实质节点不等于官方声明停更原因或服务结束 |

### 模式不可混同

- **Standard**：后期规则下使用共享池、五格 Shop、购买 XP、interest/streak、九份合三星与 round 10 Underlord；本 dossier 的四套末版构筑均在此语境。
- **Knockout**：三套五人开局选一，可刷一次；2+2 即成三星；4 Health、每败固定 -1；自动升级，无购 XP、interest 和 streak Gold；奇数轮给 item；第一回合后选 Underlord。
- **Duos**：八个二人队；共享 100 Health 与 Level，各自招募，可传 hero / 1 Gold，bench 满时不能送 hero；双胜合并伤害，一胜一负按差值；池翻倍，streak 按队伍是否掉血裁决。
- **Classic**：New Blood 后的 Standard Casual 可选变体，恢复 creep waves 且不使用 Underlord。因此“有 Loot Rounds”和“普通 Standard 直接发物品”可同时为真，但属于不同模式。
- **Freestyle / bot/offline**：用于自定义战场验证或非 PvP 体验；不产生排位 meta 证据。

## 末版 Standard 的真实循环与决策

1. 准备阶段用 Gold 在五格 Shop 买 hero、以 2 Gold reroll 或买 XP；三个同星 hero 合成下一星，Standard 三星需九份基础拷贝。
2. 后期档案中开局 5 Gold、每回合基础 5 Gold；每 10 Gold 给 1 interest，cap 3。战斗开始时锁定 interest，之后可花钱；胜利 +1，win streak 最高 +4，loss streak 最高 +2。Reroll、XP、升星库存和临时握牌竞争同一预算。
3. Level 1–10 同时决定 board cap 和 shop tier odds。例如 L5 为 35/40/25，L8 为 18/24/35/20/3，L10 为 12/18/28/32/10。因此 level-5 reroll Brawny 和 rush-10 Shaman 是相反的 Gold/概率时间表。
4. 手动 reroll 会 blacklist 未购买的本次 Shop hero，自动刷新不会；输给其他玩家后可得一次不累积的免费 reroll。
5. 共享池末版为每名 hero 分别 T1 30、T2 20、T3 18、T4 12、T5 10；卖出和玩家淘汰后归池。追三星不只是自己的概率，也是 bench 和同局 contest 的机会成本。
6. 玩家组合 alliance 纵向阈值与横向桥接，再给具体 holder 装 item。Offensive/Defensive/Support/Hat 附着 hero，Underlord 不装；Contraption 独立放场上且不占 unit cap。Hat 可给 holder 增加 Alliance，是占据物品位的阈值桥，不是免费 tag。
7. Round 10 从四名 Underlord 中选择，每名带随机两种 Fight Style 之一；有 Passive、Active、Ultimate，不使用 Mana，按 cooldown 施放，每五轮升 rank，round 40 达 rank 6。Underlord 选择是中局适配，不是 Big Update 时的赛前一次性选择。
8. 战斗中近战优先四正交相邻目标再考虑对角，远程强烈偏好近目标但非绝对；Attack Range 1 是 melee。攻击和受伤供应 Mana，所以前排承伤、攻速和控制同时改变技能时序。
9. 回合末 player damage 受存活单位的 Tier/星级 power 与历史 round damage 规则影响；具体形式曾重做，不用一个早期公式覆盖全生命周期。

## 构筑一：3 Star Brawny（耐久/成长 → 独立 AOE owner）

### 闭环

- **engine**：早期上 Brawny，让该队 Brawny kill ledger 累积；在 level 5 围绕三名 2-cost Brawny 追三星。
- **state/resource**：Brawny 队伍击杀数、星级倍率、最大生命、Gold、bench、共享池与同局 contest；召唤物 kill 只计 0.25。
- **payoff**：成长后的高 HP 让成员活到多次施放；攻略指定 Beastmaster / Bristleback 携带 Octarine Essence 压技能节奏，由它们的 AOE 技能拥有最终伤害。
- **survival**：Brawny 最大生命、Armor 与 tank item 构成生存底盘。高 HP 不会自动变成 Attack。
- **spatial condition**：Brawny 主体前置承伤/聚怪，具体技能 owner 需在能触发 AOE 且不被提前秒杀的位置；历史 Brawny+Warrior 经验也记录前置与 Disruptor 后置的区分。
- **payoff owner**：Brawny Alliance 拥有 HP 成长读取；Beastmaster/Bristleback 拥有最终 AOE；Octarine 是 holder-bound cooldown 模块。
- **economy/pivot**：level 5 停级 reroll 与买 XP 直接冲突。若另一玩家同追 Brawny，攻略明确警告两人会互卡池；此时应将三星预算转为升级/横向前排或放弃该线，而不是无限刷。
- **counter/abort**：早期 DPS 低、没拿到 Brawny 时 kill ledger 起步慢；共享池 contest、高爆发/百分比 HP 伤害、Break 针对具名 passive，都可攻击不同链路。
- **version context**：主构筑只指 2020-11-19 后 Guide；2019 Brawny+Warrior 只用于说明早弱、累积和站位，不导入旧数值。

## 构筑二：6 Mages + Spirit / Knight variation（法术/资源/状态）

### 闭环

- **engine**：用 3 Mage 过渡至 6 Mage；若出现三星 Storm Spirit + Earth Spirit 转 Spirit 桥，若出现二星 Dragon Knight / Keeper of the Light 则用 Knight 或相关前排桥。
- **state/resource**：Mage 阈值、hero Mana、攻击/受伤产 Mana、技能 cooldown、星级、升级/追三星预算和 item 位。
- **payoff**：6 Mage 让敌人承受更多 magical damage，但该联盟是放大器；最终伤害由 Keeper、Storm、Dragon Knight 等具体技能与 AOE Hobgen 拥有。
- **survival**：Barricade / Knight 前排为后排充 Mana 与施放买时间；Mana Boots 改资源时序，不直接拥有技能伤害。
- **spatial condition**：用 Barricade/前排阻止近战与 Assassin 提前触及核心；AOE 的价值依赖敌方聚集与施法者存活。
- **payoff owner**：Mage 拥有 magical amplification；每个施法 hero / Hobgen 拥有具体 spell event；Mana Boots 拥有资源供应。
- **economy/pivot**：无同类 contest 时可在追三星和快速升级之间选择；Spirit 或 Knight 分支应读已到的三星/高费 hero，不是先固定终盘再强追。
- **counter/abort**：Scaled 给 Magic Resistance，BKB/Pipe/Hood 对抗 spell；构筑对单个三星 carry 的集火较弱，Silence/Stun/后排突入可切断施法。若缺 AOE owner 或充 Mana 时间，只有 6 Mage tag 不等于成型。
- **version context**：末版 Guide 后于 2020-11-19 补丁；2019 Beta 的 Mage+Primordial/Dragon/Heartless/Shaman 只作历史桥接样本。

## 构筑三：Hunters + Fallen + Heartless（多联盟桥接）

### 闭环

- **engine**：Hunter 额外攻击供应高频普攻，Heartless 降 Armor，Fallen 用首次死亡的近邻 heal/+damage 与一次复活稳住战线。
- **state/resource**：Hunter/Fallen/Heartless/Vigilant 阈值、Terrorblade 三星、Wraith King 一星可达性、Armor debuff、额外攻击触发、复活次数和 item slot。
- **payoff**：Terrorblade/Luna/其他 Hunter 的物理攻击拥有最终伤害；Hunter 额外攻击可再触发 Maelstrom/Skull Basher 类 holder effect，Heartless 只降甲放大。
- **survival**：Fallen 首次死亡 heal/buff、每战一次复活，加上 Brute/tank 前排或 Barricade；item cooldown 不因 revive 刷新。
- **spatial condition**：远程 carry 依赖前排与 Barricade；Fallen 的 heal/+damage 只影响死者 2 格内 ally；对 Assassin 必须改后排/角落，不是固定站位图。
- **payoff owner**：Heartless 拥有 Armor debuff，Hunter 拥有 extra-attack rule，Fallen 拥有死亡/复活事务，具体 carry 拥有伤害。Desolator 是 holder-bound 降甲模块，Enthrall Anessix 是 Underlord 控制/单位转化辅助。
- **economy/pivot**：目标可从 Hunter+Heartless+Vigilant+任一可用 tank 稳住，再追 6 Fallen Hunter；若 Terrorblade 三星或 Wraith King 不可达，应停在横向混搭，不让高费桥将全部 Gold 锁死。
- **counter/abort**：Shaman/Summoner 用数量与占位稀释单点，Assassin 跳后排，Brute 降伤并扫近战；对方多三星时强度门槛也可压过联盟闭环。
- **version context**：只用 2020-12 末版 Guide 定义构筑；Fallen 是 New Blood 联盟，不得与 Season One 首发时的无 Fallen roster 混写。

## 构筑四：Rush Level Shamans（召唤/高频/成长）

### 闭环

- **engine**：组 4–6 Shaman，战斗开始召唤 Wolf/Wildwing/Black Dragon；再接 4–6 Savage 的攻击次数成长和对 summon/全队的层级作用。Lone Druid 是关键高费桥。
- **state/resource**：Shaman/Savage/Summoner 阈值、召唤物类型、召唤 owner 裁决、攻击次数成长、board cells、Level 10 时间、Gold 和高费 shop odds。
- **payoff**：Shaman 召唤物与 Lone Druid/其他 hero 分别拥有攻击；Savage 是随攻击增长的放大层，Summoner 只放大 summon damage。Necronomicon/Horn of the Alpha 额外供应 zoo 单位，不把所有召唤合并为一个 owner。
- **survival**：高人数与召唤物分担目标，前排 hero 保护后排 Shaman；召唤不等于免费生存，仍受 AOE、占位和回合后伤害守卫。
- **spatial condition**：召唤需要合法格；Shaman owner 按星级 → draft tier → position（偏好后排）→ 购买/合成时间决胜。占满棋盘可让召唤失去位置，不能只看 alliance pip。
- **payoff owner**：裁决出的 Shaman 拥有开场 summon transaction；召唤物各自拥有攻击；Savage/Summoner 是 reader/amplifier；item 拥有额外召唤。
- **economy/pivot**：攻略不要求大量三星，而是尽快升 10 找 Lone Druid 与高费 Shaman；这与 Brawny 停 level 5 reroll 是两条相反的经济路线。若升级时掉血过快，应先上过渡前排/召唤 item，而不是空等高费。
- **counter/abort**：Guide 指出 round 30 后较弱；Heartless/Vigilant 类高频物理可快速清 summon，AOE/control 可同时打击后排与召唤物，无空位则 engine 未发生。
- **version context**：只用 New Blood 后 Shaman 版本；旧 Shaman Hex/Ace 与 Season One 早期 Summoner 不混入。

## 补充线：4 Brute 在 level 7 reroll

末版 Guide 的 4 Brute 线是一个更直接的“耐久 + 独立 damage event”样本：3 Brute + Brute Cap、4 Brute + Doom，或 Brute Cap + Hunter Crown 在 round 12/16/20 找桥；前排装 tank item。Brute 优先攻击尚未带该 debuff 的敌人，首次施加减伤时触发独立 damage-on-apply；最后补丁为 30/50% 减伤与 80/120 伤害。它对 Knights/Warriors/Assassins 等近战较强，被 Hunters、Mages 和更强 Brute 镜像针对。这是具名 trigger 产生伤害，仍不是“防御数值直接转攻击”。

## 联盟阈值、桥接和 payoff owner

- **纵向**：6 Mage、6 Fallen、6 Shaman 等高阈值换取强规则，但会挤压前排、控制、主 carry 与 counter slot。
- **横向**：Troll 同时给 Troll 和其他 ally 不同幅度攻速；Heartless 为 Hunter 降甲；Vigilant 在敌人施法后换目标并给首击；Hat 以 item slot 添 Alliance；这些是占人口/物品位的 bridge。
- **放大器不等于 owner**：Mage、Heartless、Summoner、Savage 改变伤害条件，但最终 event 仍归具体 hero/summon/item。Hunter 额外攻击也应把触发记录回持有者，不记为“联盟自动打伤害”。
- **Ace 历史样本**：Ace 是 T5 联盟封顶，激活相应 Alliance 时提供 Ace effect 且增加 Shop 出现指定 Ace 的概率。它把纵向 tag、高费卡与 shop odds 捆绑，也说明“终盘件”可同时改变概率和战斗规则；但 Ace 不是末版四套构筑的通用部件。

## 攻击、Mana、站位与状态反制

- **目标/距离**：近战的四正交优先、远程的近目标偏好、Assassin 跳跃和 Vigilant 施法后换目标，使“前坦后打”只是基本形，不是绝对保护。
- **Mana**：普攻伤害和受伤产 Mana；高攻击超过自身产 Mana cap 后仍会让敌人因受伤获得更多 Mana。Human、Arcane Boots、Eul、Refresher 等是不同资源/重置 owner。Underlord 后期则不用 Mana，不能共用 hero 公式。
- **Break**：禁 hero passive 和 item，但不禁 Alliance，也不禁 Underlord passive；**Disarm** 禁普攻但可施法；**Root** 禁移动但可对合法范围内目标攻击/施法；**Silence** 禁施法；**Stun** 禁移动/攻击/施法并打断。
- **Poison/Fire**：Poison 每层每秒 15 physical damage、-15% healing，5 秒、最多 5 层；Fire 每秒造成 2.5% Max Health 伤害，友方施加时不致死。两者都不是“元素盾”。
- **防御分轴**：Armor 只处理 physical，负甲放大物理；Magic Resistance 只处理 magical 且多来源乘法；pure 不受两者影响。Knight 还要求面向与相邻条件，Scaled 是 Mage 的具名反制。

## 召唤、死亡、复活与确定性

- Shaman 开场召唤有显式 owner 排序：星级 → draft tier → 位置（偏后排）→ 购买/合成先后。这是可验证的 resolver，不是随机把 summon 归给任一 Shaman。
- 2019-11 官方将 Spiderling 改为受任何伤害即死，并不再继承大多数 Alliance buff（Troll 例外），说明 token 继承要 whitelist。
- 2020-09 修正 Shaman alliance summon 回合末造成 player damage 和 summon owner 不稳定；Horn of the Alpha 也被修正为不造成回合末伤害。战斗内单位与回合末 player-damage roster 必须分开。
- Fallen 的第一次死亡 heal/buff 与每战一次 revive 是有限事务；官方还修正 Wraith King 不从正在复活的 Fallen hero 再生 zombie。复活不重置 item cooldown，不应产生隐式无限递归。
- Void 让全体 ally 有概率附加目标 Max HP 的 pure damage，但 2020-03-19 加全局 0.5 秒 proc cooldown；高频攻击是触发候选，不是无限每击倍增。

## 生命周期与负面/重做案例

1. **Ace 终盘锁定**：把 Alliance、T5 卡与 shop bias 捆绑；后续 roster 重做后不可直接迁移。
2. **Jail 初版不均**：官方明确记录 Scrappy/Inventor 被过度惩罚而 Assassin 基本不受影响，改为每日 8–12 个、按 Tier/Alliance 限制并每日一 Ace。
3. **Jail 日常 meta 分歧**：有玩家抱怨每日只剩少数最优组合，也有玩家在移除后认为它曾提供每日变化；这是社区分歧，不是统计共识。
4. **Big Update HP 过长**：官方认为 2x HP 过多，Jull-tide 回调为原始值 1.5x；这是明确的战斗时长/生存节奏校正。
5. **Underlord 架构替换**：赛前选 + Talent + Hype 被 round 10/2 选 + Fight Style + cooldown 取代；不是参数微调。
6. **精简的玩家成本**：Jull-tide 移除 Talent、Hype、creep rounds 后，单一社区主题认为策略与节奏被削弱；这是个人体验，不支持“为了新手”的因果。
7. **中立轮语义反复**：普通 Standard/Duos 删 creep rounds 并定期直接发 item；Classic 后来恢复 creep waves。两条规则不可混为一个时线。
8. **物品/联盟/roster 轮换**：Alliance items、Summoning Stone、Scrappy/Inventor 和多个英雄在不同节点移除/回归；Season One Hats 和 New Blood 新联盟使旧 Guide 大量失效。
9. **共享池缩放**：2020-04-30 全 Tier 缩池，官方理由是提高未 contest hero 的升星机会并鼓励多样 crew；2020-06-12 又把 T3 由 15 增至 18。
10. **Void rate limit**：全队百分比 HP pure proc 后加 0.5 秒全局冷却，表明共享触发必须有高频护栏。
11. **token 继承缩窄**：Spiderling 失去大多数 Alliance buff；召唤物不应自动继承队伍所有规则。
12. **summon owner / player damage 修正**：Shaman 召唤 owner 被规则化，召唤物/Horn 回合末伤害被禁；解析器与战绩归属是玩法的一部分。
13. **Fallen 复活重入修正**：正在 revive 的 Fallen 不再额外生 zombie，说明 death、reviving、revived 必须是可区分的状态。
14. **兼容性断层与停更因果边界**：New Blood 时旧新客户端只同版匹配；2020-11-19 后未找到新的实质平衡节点，但也未找到 Valve 对停止实质更新原因的正式声明。不从玩家数、商店可访问或社区“dead”提问推导原因。

## 对本项目的可迁移结论

- **盾/防御体系与元素/状态体系是正交轴**：Knight/Armor/Magic Resistance/Brawny HP 解决存活，Poison/Fire/Silence/Break 解决状态与反制。“冰盾”和“土盾”可共享盾的受击/破盾语义，但元素 tag、状态反应与抗性仍应分层。
- **防御到输出必须有具名 converter**：Brawny 高 HP 买技能施放时间，Brute 是“施加减伤时产生独立伤害”，二者都不证明任意 Defense 自动变 Attack。项目若设计“全队额外生命/防御 → 一名射手”，必须明确 supplier set、只读 bonus 还是 total、recipient、是否排除自身、读取时点、cap、刷新、item/relic slot、来源归属与 counter。
- **同一耐久底盘可支撑不同输出 owner**：Brawny 可以让 Beastmaster/Bristleback 多次 AOE，Brute 可通过具名 apply event 输出，Knight 则可单纯保护后排 carry。这三种闭环在报告中必须显示不同 owner。
- **高频与全队 proc 要限频**：Hunter 额外攻击可再触发 holder item，Void 却需要 0.5 秒全局 cooldown。项目应在 trigger 上标记 per-hit / per-cast / per-source / team-global 和 cooldown owner。
- **召唤需要 owner、格位、继承与回合末资格**：Shaman 排序、Spiderling whitelist、Fallen revive 与 summon player-damage 修正共同证明，“生成了什么”不够；还要记谁生成、占哪格、继承哪些 tag/stat、死亡/复活时是否再触发，以及是否计人口/player damage。
- **构筑不能只有联盟名**：同样是 6 Mage，Spirit 和 Knight 分支的经济、生存和站位都不同；同样是 Hunter，Fallen/Heartless 供应的是死亡韧性和降甲。“体系”必须闭合 engine + state + payoff + survival + space + owner + pivot/counter。

## 不兼容假设与未决问题

- 不将 Dota Underlords 的共享池、PvP 连胜/连败、对手站位侦查或八人淘汰直接搬到本项目的单人爬塔结构。
- 不把 Underlord 中局选择直接等于本项目英雄指令系统；前者是一个额外战斗单位/技能包。
- 未决：项目的队伍防御转单核是 item holder 规则、relic 全局规则，还是英雄 passive？三者的可达性、占位和可替换性不同。
- 未决：若同时存在 Ice Shield 和 Earth Shield，破盾、净化、元素抗性和反应是作用于共享 Shield state，还是作用于各自 supplier？
- 未决：如何在战报中分别展示联盟放大、item/relic converter、伤害 owner、summon owner 与限频拒绝？

## 检索日志与停止理由

- 筛完 Steam News API 全部 199 条标题，深读 13 个版本/规则节点；PC Gamer/RPS 转载、促销、赛事和重复 hotfix 不为深证据充数。
- 通过 Fandom MediaWiki API 列全站页名并读取 27 个相关页的完整 HTML/revision；具体 hero 目录在 alliance、build owner 与官方补丁已能闭合后停止枚举。
- Steam Guide 2317227825 的传统 HTML 已读到十个 build 段、roll level、item、strength/weakness；1834723571 仅作 Pre-Big Update 历史分支。只有视频链接/图而无可读解释的 Guide 不升级为 evidence。
- 筛查 Jail、Underlord/Hype、shared pool、Brawny/Shaman/meta、last update/dead game 等 Discussion 查询，只保留五个完整主题。标题、搜索摘要、无字幕视频不计来源。
- 未找到可验证的代表性统计；不报总体胜率、采用率或排名。未找到 Valve 对停止实质更新的正式因果说明；不从更新空白、当前页面可访问或单一玩家评论推断原因。
- 停止原因：47 个来源已闭合版本/模式地图、四套末版构筑、一套耐久补充线、全部经济/站位/状态/召唤关键轴与十四个生命周期家族。新结果已主要重复 owner、counter 和版本风险，继续搜索的预期信息增益已低。

## 最终 disposition

`anchor-retained`。Dota Underlords 以历史锚点保留：它展示了共享池、纵向/横向 Alliance、Underlord 系统级重做、召唤 owner 裁决、高频 rate limit 和防御到独立输出 owner 的多种闭环。这是研究证据，不是对本项目第一版体系、英雄、装备、遗物或数值的授权。跨游戏 synthesis 继续 `Withheld`。
