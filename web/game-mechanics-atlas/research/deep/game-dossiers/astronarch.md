# Astronarch

## 身份与研究时期

- `title_id`: `astronarch`
- 类型：单人路线型 roguelite / 小队 autobattler。玩家在三幕路线中选择战斗、精英、事件与商人，为固定小队分配英雄、能力升级、装备和药水；开战前配置两个 2×2 区域内的站位，战斗开始后由英雄自动攻击和施放能力。
- 发行状态：2020-12-14 Early Access，2021-01-22 发布 1.0；本档案观察 2021-01 至 2021-06 的 1.0–1.6 官方更新、2021 年完整攻略、更新至 2024 的通关路线，以及一份 2025 玩家队伍记录。
- 版本边界：2021-02 的旧 Corruption 20 只允许以 2 人开局、4 人结束；v1.5 将该旧效果移为可选 Omen，并给 C20 新效果。v1.3.3 重做 Frost 上限，v1.4 加入 Fallen 和 30 件物品，v1.6 加入 Primarch。旧构筑只证明当时可用的责任链，不代表 1.6 当前数值。
- 直接游玩：本批未购买或安装游戏；规则来自 7 篇官方公告和 7 篇正文可读的 Steam Guides。商店页与搜索摘要未进入 deep evidence。

## 检索日志

1. 通过 Steam official news API 读取 v1.0、1.2.3、1.3.3、1.3.5、1.4、1.5、1.6 完整正文，核验 Corruption/Omen、Frost、Fallen、Primarch、物品池、事件、执行顺序、显示上限和召唤清理。
2. 读取 `High Corruption Level Build` 全部 24 节，核验 Druid—Paladin—Cleric—Pyromancer—Assassin 的技能、逐件装备、升级顺序、Burn 供应与 Detonate 所有权，以及旧 C20 的四人变体。
3. 读取 `Elements of Strategy`，核验 HP/Defense/Shield/Healing/Piercing 关系、速度与触发频率、MP/主动技能节奏、两套 tempo 队和旧 C20 Druid—Ronin—Frostmancer—Berserker 通关队。
4. 读取 beginners/classes guides，核验 3→5 人招募、Ability Orb 稀缺与出售、两个 2×2 区域、目标顺序、Hidden、adjacency、击退导致支援断链和多种坦克/伤害/治疗改职。
5. 读取 `Easy Corruption 1–20` 与 `Complete Guide`，核验跨幕招募、路线/精英/商店/升级/药水取舍、1.5.3 的通用装备集合和战斗间 swap kit。
6. 读取 2025 HP/Frost 队记录作时间较新的单例对照；其未注明游戏版本且混有与基础循环冲突的描述，因此只保留具体三人装备，不用它定义通用规则或当前强度。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-astro-official-1-0` | [Full Release Patch v1.0.0](https://steamcommunity.com/games/1234940/announcements/detail/4043637702552281782) | Dale Turner，2021-01-22 | official-dev / A | C11–20、Ancient 去重、进度提示、Warlock/Pyro/Fiend 与 Protected |
| `src-astro-official-1-2-3` | [Animation Caps & Bug Fixes v1.2.3](https://steamcommunity.com/games/1234940/announcements/detail/4053772703629502750) | Dale Turner，2021-02-10 | official-patch / A | 动画计时、速度显示、Frail 说明、反拖延敌人、死亡后召唤清理 |
| `src-astro-official-1-3-3` | [Frost & Item Changes v1.3.3](https://steamcommunity.com/games/1234940/announcements/detail/4059402835316683813) | Dale Turner，2021-02-20 | official-patch / A | Frost 5→10、Frostmancer、Avalanche、Talisman、Negate 与 exploit/defeat 修复 |
| `src-astro-official-1-3-5` | [Balances and Bugs v1.3.5](https://steamcommunity.com/games/1234940/announcements/detail/4060529371697574322) | Dale Turner，2021-02-24 | official-patch / A | Interstellar Seller/merchant 重做、战斗 tooltip 与开战物品顺序 |
| `src-astro-official-1-4` | [The Fallen & 30 New Items v1.4.0](https://steamcommunity.com/games/1234940/announcements/detail/5556851879140854895) | Dale Turner，2021-03-13 | official-dev / A | Fallen 邻接截伤/非邻接吸血、物品池递减、术语、Tower Shield 叠加修复 |
| `src-astro-official-1-5` | [Omens v1.5.0](https://steamcommunity.com/games/1234940/announcements/detail/4767601588109435962) | Dale Turner，2021-05-13 | official-patch / A | Omen/C20 分离、预览与目标 UI、Gladiator 状态收益、Pylon、速度实际值 |
| `src-astro-official-1-6` | [The Keys of Fate v1.6.0](https://steamcommunity.com/games/1234940/announcements/detail/4032392655063084658) | Dale Turner，2021-06-24 | official-dev / A | 三把一次性钥匙、合成/售卖、Primarch 能力切换与反拖延要求 |
| `src-astro-guide-high-corruption` | [High Corruption Level Build](https://steamcommunity.com/sharedfiles/filedetails/?id=2412671082) | Artemitch，2021-03-02 | strategy-guide / C | 五人 Burn/Detonate 队、逐英雄物品/升级/职责与旧 C20 变体 |
| `src-astro-guide-elements` | [Astronarch Elements of Strategy](https://steamcommunity.com/sharedfiles/filedetails/?id=2395485613) | Sellardohr，2021-02-14 / 02-26 | strategy-guide / C | 防御矩阵、攻速/MP、tempo 队、Boss Revenge、C20 四人通关与 swap kit |
| `src-astro-guide-beginners` | [Astronarch beginners guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2395108692) | Rillzor，2021-02-14 / 02-16 | strategy-guide / C | 自动战斗边界、招募、Orb 经济、2×2 站位、目标/邻接/击退 |
| `src-astro-guide-classes` | [Classes guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2397475989) | Rillzor，2021-02-16 / 02-19 | strategy-guide / C | 角色改职、Frost/Burn/Poison、Shield/反伤、召唤、控制与 Boss 免疫 |
| `src-astro-guide-easy-c20` | [Astronarch Easy Corruption 1–20](https://steamcommunity.com/sharedfiles/filedetails/?id=2426329160) | Audax，2021-03-28 / 2024-11-29 | strategy-guide / C | 3/4/5 人顺序、三幕路线、morale、装备优先与 C20 角色压缩 |
| `src-astro-guide-complete` | [Astronarch Complete Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2490531567) | doemondo，2021-05-17 / 2023-03-23 | strategy-guide / C | 标注 v1.5.3 的角色/装备集合、商店阈值、事件与多套 owner 配置 |
| `src-astro-guide-hp-2025` | [HP Heroes & Gear Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3451094361) | limonik26263，2025-03-24 | strategy-guide / C | Frostmancer—Assassin—Juggernaut 三人装备单例 |

## 真实循环与玩家决策

### 路线、招募与成长

- 普通旧版 run 以 3 名英雄开始，每幕 Boss 后在 tavern 增加 1 人，最终 5 人；Corruption 20 的早期版本压缩为 2→4 人。招募不是随机商店滚同名升星，而是用有限英雄位补齐当前的 tank、off-tank、healer、damage、control 或 backline access（`src-astro-guide-beginners`, `src-astro-guide-elements`）。
- 每名英雄只有基础属性、一个 passive、一个 active；两种能力各可升级两次。每 run 约 8–12 枚 Ability Orbs，不足以喂满全队；未投入的 Orb 可卖 Gold，形成“核心同时升 passive+active、功能位只升一侧、无须升级的第五人释放经济”的明确差别。
- 三幕地图让玩家在普通战、精英、事件、商人和不同路线间取舍。低 Gold/Morale 时战斗通常比事件稳定；高难度会主动避开某些末段路线、跳过打不过的早期精英，用 morale 容纳有限失败，并把药水留给精英/Boss。
- 装备属于具体英雄，可在战斗间替换。升级后的单件通常比两件未升级同名物更有乘法价值；但对 Boss、DOT、开场爆发和长时成长所需的 kit 不同，因此攻略明确建议保留 swap kit，而不是把所有拾取永久焊死在当前 owner。

### 战前与战中职责边界

1. **战前**：选本场装备/药水、能力升级、英雄位与两个 2×2 区域内的站位；站位决定正前方区域优先于对角区域、前排优先于后排、再按最近目标，Hidden 通常最后被选。
2. **邻接与目标链**：Druid/Cleric 的治疗、Paladin 的群盾、Fallen 的截伤以及大量 AOE 只在指定相邻关系生效。击退/换位会把 tank 推出 healer 覆盖，或让 healer 只能治疗自己，所以阵型不是装饰。
3. **自动战斗**：开始后不能移动英雄；普通攻击、MP 累积、active 施放、DOT、控制和召唤自动执行。药水是有限的战斗中 tempo 改变，但没有证据表明存在可配两条技能并花共享点数的独立 command system；不能拿它替代本项目已确认的 tactical commands。

## 具名构筑一：early-full-release Burn Detonation 五人队

来源时期：2021-03-02，Frost v1.3.3 后、Fallen v1.4 前。攻略用于高 Corruption 但五人版不适用于当时只允许四人的 C20。

### Druid：主坦与受击成长 owner

- Barkskin 在开场给 Frail Defense；受攻击时由 Helm of the Mad King 累积 ATK/SPD。
- War Mage's Greatstaff 在开战和普攻时按 ATK 产盾；Neb's Crystal Weaver 在开战和 active 时按 ATK 产盾并加 DEF。Druid 自己的低 MP heal 继续延长 frontline 存活。
- 升级优先为 Helm→Staff→Helm→Neb→Staff→Neb，Ability Orbs 先把 Druid passive/active 升满。它不是单纯“堆防”，而是被打→攻击/速度→普攻/施法→盾的循环。

### Paladin、Cleric：状态供应和续航

- Paladin 以 Zephyr Pendant 提高长期攻速，用 Searing Edge / passive 给目标与邻接敌人叠 Burn；Champion's Guard 随攻击叠 DEF。Aegis 给自己与相邻英雄基于 Defense 的 Shield，主坦倒下后短暂接管。
- Cleric 以 Tundra Talisman 周期性给全敌 Frost，降低敌方 tempo；Brain Sage 与 Maiden's Mirror 放大和分流治疗，Bless 治疗最弱相邻英雄并移除一类负面状态。
- 二者不是主伤害 owner：Paladin 提供 Burn 与第二道墙，Cleric 提供 Frost、治疗和 cleanse，使 Pyromancer 能等到结算窗口。

### Pyromancer、Assassin：结算与后排入口

- Pyromancer 用 Zephyr Pendant、Searing Edge/Bound Elements 和 Lich Wraps/Symbiote 叠 Burn、降低 active 周期；Detonate 移除目标 Burn，把剩余 DOT 倍率转成即时 piercing damage。Pyromancer 是主 payoff owner。
- Assassin 的 Hidden 使其通常最后被攻击；位于前排时优先敌方后排。它用高速度和 Searing Edge/Bound Elements 补 Burn/Poison，Garrote Silence 后排，并可在 Pyro 失败时改用 Headdress/Mask 成为第二成长 owner。
- 构筑语法：受击成长 Druid + 多人普攻 Burn/Frost state + Pyromancer Detonate + Druid/Paladin/Cleric survival + 两个 2×2 内的治疗/群盾和 Assassin 前排后排索敌。
- 失败与转型：敌人打散邻接、持续 piercing/DOT、后排 AOE、开场杀死 Pyro，都会断开链路；缺 Detonate 物品时先让 Assassin/Paladin 做普通 proc owner，缺坦装时优先 Druid，后期才把额外升级给 Pyro/Assassin。

## 具名构筑二：旧 C20 Druid—Ronin—Frostmancer—Berserker

来源时期：2021-02-26 前，旧 C20 限制 2 人开局、4 人结束；v1.5 后该限制属于 Omen，不可当作当前 C20 固定规则。

- 开局 Druid 自坦、自疗，为 Ronin 的 AOE 成长和 Disarm 买时间；第三人 Frostmancer 提供 Frost、stun 和群体控制；第四人 Berserker 持续获得攻击成长并以 lifesteal 收割。
- 最终 Boss 准备：Berserker 持 Realm Tooth + Whirlwind Axes；Ronin 以 Symbiote + Enchanter's Rod 取得 Revenge immunity。目标不是用治疗硬顶无限反伤，而是保护真正的 damage owners 不被其自身输出反杀。
- Engine：Druid 延时 + Ronin AOE/Disarm + Frostmancer 控速；State：Frost、战斗时间、Ronin/Berserker 的成长与 active 次数；Payoff：Berserker 单体成长和 Ronin AOE；Survival：Druid、Disarm、Frost、lifesteal、Revenge immunity；Spatial：Druid 承接第一目标，后排保持邻接支援而不让同一爆发覆盖全队。
- 适应窗口：开局先看本幕 Boss；早期精英不可能时跳过，路线落后过大时重开。Disarm/Stun 不能替代对 Boss active 的准备，过多续航也会在 v1.6 Primarch 面前被持续成长反超。

## 补充样本：2025 Frost 三人队

- Assassin：Fenrir's Bane、Tundra Talisman、Zephyr Pendant，以速度和对 Frost 目标的奖励做精确删除。
- Frostmancer：Starbird、Mage's Chainmail、Mark of the Archmage，负责 Frost/控制与铺路。
- Juggernaut：High Elf Claymore、Saint's Vow、Frozen Carapace，承担攻击者、反击/恢复与 frontline。
- 这是未注明 patch 的个人通关单例。它只说明 2025 仍有人把“Frost supplier/controller—Assassin payoff—Juggernaut survival”作为三人机器；不证明该队是 1.6 meta，也不采纳同文与基础招募循环冲突的泛化描述。

## 防御、盾与伤害转换

- 指南按 loading tooltip 将 Defense 理解为有效生命倍率；Shield 像预先治疗，同样受 Defense 放大，但未受击时可能浪费。Heal 又可能按 recipient Max HP、caster Max HP 或 damage dealt 缩放，不能用一个“治疗量”公式覆盖所有 owner。
- Bleed、Burn、Poison 和 Piercing 均被攻略描述为绕过 Defense 与 Shield，因此高 Defense/低 HP 坦克仍需要 HP、cleanse、immunity 或换路。Enchanter's Chainmail 被作为针对 DOT/piercing 路线的备用件，而非通用最优坦装。
- 防御并不自动产生输出，但装备和英雄可充当 converter：War Mage's Greatstaff/Neb 把 ATK 变 Shield；Helm 把 attackers 变 ATK/SPD；Bone-Spike Armor 可由 Fallen 截取相邻伤害触发；Brawler 以 Max HP 产 Shield/反击；Gladiator 可用 Giant's Club 按 HP 造成伤害。
- 这支持把“盾/防御 chassis”和“元素/status chassis”正交设计：Paladin 可同时是 Shield supplier 与 Burn supplier，Frostmancer 可以用 Frost 保护队伍，真正 payoff 则由 Pyro、Assassin、Berserker 或装备 reader 持有。

## Frost、Burn、Poison 与节奏

- v1.3.3 将 Frost 上限从 5 提到 10、每层效果减半，最大效果不变。官方原因是 5 层后期过快封顶，难以围绕 Frost 继续做物品；Frostmancer 也能过低投入清群。
- Frostmancer active 先由 passive 添加 Frost，再计算伤害与 stun；Avalanche 从“每次攻击消耗 1 层触发邻接伤害”改为“目标满 10 层时全部消耗，造成大额目标+邻接伤害”。这把 status 从线性常驻 debuff 变成可读阈值/结算窗口。
- 普攻 proc 主要吃攻击间隔，active 主要吃 ATK 系数、MP cost 和每秒 MP。Scourge、Searing Edge、Bound Elements 需要速度；高倍率 active 则更偏 ATK 与 Ability Orb。一个“攻速”属性不能暗中同时缩短 active 周期。
- Pyro 消耗 Burn，Alchemist 复制 Poison，Frost/Avalanche 可到阈值后消耗；同为元素状态却分别是 supplier→detonator、随机复制和 cap cashout，不能只按颜色列阵营加成。

## 装备、事件与机会成本

- common/rare item 在 v1.4 起每次见到后降低再次出现概率，Ancient 在 v1.0 起不会重复已有件；这是弱去重，不是保证完成指定配方。三物品槽、升级 Gold、路线和 ability orbs 同时竞争投入。
- v1.5.3 攻略将装备按 caster、DPS、tank、Reverie Totem 分组，并明确列出“能运转但建设成本过高或回报不足”的组合。这比英雄 tier list 更有研究价值：同一英雄能因物品成为 tank、off-tank、buffer 或 payoff。
- v1.3.5 重做 Interstellar Seller：过去玩家会为了等事件而故意不升级；改为按每件 item 计算代价，并允许牺牲 ATK/SPD 或 HP/DEF。事件奖励必须避免把“延迟正常成长”变成无脑正确答案。
- v1.6 三个 key item 各只出现一次，漏拿本 run 不再出现；集齐后合成物可替换普通 final boss 为 Primarch，也可卖成大量 Gold。它把隐藏 Boss、路线承诺和立即经济回报放在同一可见决策，而不是免费附加内容。

## Boss、敌方能力包与失败解释

- 普通 final boss 给全队 Revenge，使英雄承受自身输出的一部分；攻略要求 damage owner 携带 Negate、免疫、cleanse 或足量 shield/heal，并建议保留 Cure All/Divine 药水。高伤害本身可能成为失败源。
- Primarch 在 5 套能力间切换，C20 为 6 套；官方明确建议比普通 final boss 更 aggressive，过多 healing、伤害不足会让其成长失控，但它不移除 buffs。因此同一 swap kit 面对两个终 Boss 得出相反答案。
- Barba 随时间把攻击间隔压低，迫使玩家爆发击杀或用 summon/illusion 分段买时间。Sloshed Simian 的 active 被加上 ATK，官方理由就是防止 drawn-out battle；反拖延可以是具体敌人能力，而非隐藏硬时限。
- DOT/piercing 敌人、Disarm/Stun、强 active、全体/邻接 AOE、击退和 Boss resistance 分别攻击 Defense、攻击频率、施法、阵型覆盖和控制依赖。失败报告至少应显示敌方 owner、被绕过的防御层、状态/免疫、首个阵型断点与 damage owner 存活时间。

## 失败、重做与明确负面案例

以下六组计入跨游戏明确负面/重做深度：

1. **Frost 过快封顶并挤压物品设计**：5 层后期太容易到 cap，Frostmancer 低投入清群；v1.3.3 改为 10 层、单层减半，并重做 Avalanche/Talisman 等 reader。
2. **Corruption 20 把队伍压缩和一般难度绑死**：v1.5 为 C20 换新效果，把旧效果移为可独立组合的 Omen；高难度轴从单一路径拆成 Corruption × Omen。
3. **SPD 展示与实际执行两次不一致**：v1.2.3 因动画提前 0.2 秒把 UI cap 改到 0.3；v1.5 又确认界面 0.3、实际 0.5，最终让 tooltip 停在 0.5。显示公式必须由实际 cadence 反推并回归验证。
4. **Interstellar Seller 奖励等待而非成长**：旧事件让玩家压住正常升级等待更优兑换；v1.3.5 改成逐件计价和两类属性牺牲，官方明确说要减少该动机。
5. **多 owner / 多 copy 的执行顺序错误**：Tower Shield 多件装备时受到过量削减；Warlord's Burden 与另一英雄的 Life-giving Chamber 交互时伤害错误，随后统一修正开战物品顺序。装备规则必须有 owner、phase 与 stable order。
6. **来源死亡后临时单位仍继续生成**：defeated Mother Rat 曾在场上仍有 rats 时继续生成；v1.2.3 修复。召唤源死亡、已有临时单位、继续生成和 battle defeat 必须是四个显式状态。

另有 Puzzling Box 八次失败保底、reward-window Gold exploit、Hydra 未清所有 heads 却提前 defeated、Divine 穿透和 Frail 即时刷新等改动，均记录为 guard/bug，但不再重复计为六个独立设计失败。

## 社区观察边界

- 2021 guides 是个人通关和理论推演，不是 build pick/win-rate。`Elements of Strategy` 明确把部分公式写成推导；只有与官方 patch 或其他指南一致的结构进入高置信规则。
- Druid 在不同指南中同时被评为顶级坦/疗和较不稳定治疗；Paladin、Cleric、Frostmancer 也出现相反强度评价。研究保留职责和发动条件，不合并成平均 tier。
- `Easy C1–20` 虽更新到 2024，但物品主要是图片且正文没有标明每次平衡变化；只用其可读的队伍/路线/优先级，不猜图片内容。
- 2025 HP guide 的具体三人装备可读，但缺 patch、含与已核验基础循环冲突的泛化句；不据此改写 1.6 招募规则或当前 meta。
- 没有公开 patch-level hero/item pick rate、Boss matchup 或当前完整 AI 顺序数据库；“最快”“最强”“必拿”只按作者建议记录。

## 对本项目可迁移

### 可迁移原则

- **盾、生命、防御、治疗和伤害 owner 必须分栏**：盾可被 Defense 放大、由 ATK/HP/Defense 产生、被 DOT/piercing 绕过，再由另一个英雄/装备读取；不能把“坦克体系”误写成没有输出的纯生存标签。
- 元素需要 supplier、state、阈值/持续时间、consumer 与 counter。Burn Detonate、Frost 10 层 cashout 和 Poison replication 是三种不同 engine，不是换色同模。
- 招募位、Ability Orb、装备槽、升级 Gold 和路线机会应产生角色压缩：旧 C20 的 Druid 同时坦/疗、Assassin 无 Orb 仍做第五人，说明功能位的价值可来自节省核心资源。
- 两个相反 Boss 能检验 swap kit：Revenge 惩罚无保护的高输出，Primarch 惩罚过度治疗/低伤害。Boss 题目应改写 build 的薄弱环节，而不是只抬数值。
- 邻接、正面/对角、前后排、Hidden、击退和 AOE 共同决定支援链；战报应说明谁离开覆盖、哪次控制/伤害绕过盾、哪个 payoff owner 先倒。
- 物品池递减、Ancient 去重、事件逐件计价和 key 一次性提示都是“降低无意义随机，保留路线承诺”的不同工具；不能用硬保底替代所有随机。

### 不可直接迁移

- Astronarch 的固定 3→5 人、两个 2×2 区域和旧 C20 2→4 人不适用于本项目 10/18 population 与更大空间战场。
- 药水是临时战中使用物，不是本项目两条 tactical commands / 3 points 的替代方案；也不授权加入额外 command deck。
- 具体 hero、item、Omen、Corruption、Morale、三把 key 与百分比均是外部内容，不成为第一版内容表。
- Astronarch 英雄无法在战中移动且目标规则较小；本项目已有持续移动、路径、body blocking 和 engagement arbitration，不能照搬其静态优先级。

## 未决问题

- 官方没有公开完整 1.6 hero/item database；许多精确能力数值来自 2021 guide，不能保证与最终 1.6 一致。
- 普通攻击、active、start-of-combat effects 在所有 tie 下的完整 deterministic order 未公开；只确认两次与显示/顺序相关的修复。
- Fallen 的截伤比例、非邻接吸血与多 Fallen/Tower Shield 最终叠加规则在可访问公告中没有完整展开。
- 2025 Frost 三人队没有难度、Omen、版本和阵型截图正文，不能与 2021 C20 构筑做强弱比较。
- v1.5 后新 C20 的具体效果在公告正文未说明；本档只确认旧效果转为 Omen，不猜新效果内容。

## Disposition

- `retained`
- 理由：14 个实质非商店来源横跨 7 篇官方设计/补丁与 7 篇完整攻略；两套早期 full-release 队伍可以回到具体英雄、装备、站位、资源、Boss counter 和转型核验，并有 v1.3–1.6 的官方生命周期说明。
- 研究价值：它证明小队 autobattler 不依赖羁绊也能形成很深的 engine/state/payoff：装备可把受击、防御、HP、攻击、速度与盾互相转换，元素又以叠层、持续、复制和消耗形成独立路线；路线、事件、Boss 与 swap kit 决定何时换 owner。
- 置信度：官方重做/bug/难度规则高；2021 构筑结构与站位中；具体物品数值、2025 单例、当前 hero tier 与完整 AI 顺序低到中。
