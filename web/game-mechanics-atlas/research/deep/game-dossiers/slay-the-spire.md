# Slay the Spire

## 身份、范围与研究深度

- `title_id`: `slay-the-spire`
- 开发 / 发行：Mega Crit
- Steam App：646570
- 类型：单人牌组构筑 roguelike；本项目的 `anchor-adjacent` 样本，而非自走棋直接同类。
- 本 dossier 只覆盖 Steam/PC 原作标准 run。主机/移动移植、Daily、Custom、mods、《Slay the Spire 2》和桌游不混入规则或强度结论。
- 版本链：2017-11-15 Early Access → 2019-01-23 `1.0` → 2019-07-01 `1.1` → 2020-01-14 `2.0` Watcher → 2020-11-30 `2.2` 末次大型公开平衡 → 2022-02-27 `2.3` Steam Deck/input → 2022-12-20 `2.3.4` 非英语 Windows 启动修复。
- 当前可核 PC 端点是 `2.3.4`，但最后大型平衡节点是 `2.2`。两者不得互换。
- 深度结论：`anchor-retained`。59 个实质来源跨 `official-dev`、`official-patch`、`maintained-wiki`、`strategy-guide` 四种类型，五条构筑链均闭合，并有敌人 counter、时序、生命周期和失败解释。

研究采用机制切片，不穷举所有卡牌、遗物、事件、Boss 或 Ascension 数值。切片选择直接服务本项目的盾体系、元素/状态体系、遗物/装备规则改写、资源转换、循环 guard 和敌人考题设计。

## 来源包

| 组 | 数量 | 来源 id | 作用 | 主要限制 |
|---|---:|---|---|---|
| 官方身份 / 版本 / 补丁 | 13 | `src-sts-official-ea`, `src-sts-official-ascension`, `src-sts-official-defect`, `src-sts-official-custom`, `src-sts-official-poison-cap`, `src-sts-official-final-act`, `src-sts-official-weekly53`, `src-sts-official-1-0`, `src-sts-official-1-1`, `src-sts-official-2-0`, `src-sts-official-2-2`, `src-sts-official-2-3`, `src-sts-official-2-3-4` | 建立 EA→2.3.4、角色/模式/Final Act、历史规则和结算时序 | 不证明当前 build 采用率；Custom 只作模式隔离 |
| 基础循环 / 经济 | 5 | `src-sts-wiki-gameplay`, `src-sts-wiki-energy`, `src-sts-wiki-card-draw`, `src-sts-wiki-card-rewards`, `src-sts-wiki-merchant` | 地图、战斗、能量、抽牌、跳过、购买与移除 | 社区规则档案，无总体统计 |
| Block 转伤 | 5 | `src-sts-wiki-block`, `src-sts-wiki-barricade`, `src-sts-wiki-calipers`, `src-sts-wiki-entrench`, `src-sts-wiki-body-slam` | Block 生命周期、保留、倍增和具名输出 owner | Barricade/Calipers/Blur 不得合并；外部攻击 modifier 的精确交互未在本来源包中核实 |
| Exhaust 规则改写 | 5 | `src-sts-wiki-exhaust`, `src-sts-wiki-corruption`, `src-sts-wiki-feel-no-pain`, `src-sts-wiki-dark-embrace`, `src-sts-wiki-dead-branch` | 0 费、离场、Block、抽牌、随机生成的独立 owner | 有限 Skills、手牌上限与随机污染阻止“自动无限” |
| Defect Orb | 7 | `src-sts-wiki-orbs`, `src-sts-wiki-focus`, `src-sts-wiki-orb-slots`, `src-sts-wiki-loop`, `src-sts-wiki-capacitor`, `src-sts-wiki-blizzard`, `src-sts-wiki-electrodynamics` | Channel/Evoke/slot/order、Frost、Lightning、Focus 与累计账本 | Frost/Blizzard/Lightning/Dark/Plasma 不能压成同一元素轴 |
| Poison / debuff | 3 | `src-sts-wiki-poison`, `src-sts-wiki-catalyst`, `src-sts-wiki-artifact` | DOT、衰减、倍增、Exhaust、单次 debuff guard | Catalyst/Artifact 只按可核页面写，不猜实现中间步骤 |
| Watcher stance | 3 | `src-sts-wiki-stance`, `src-sts-wiki-rushdown`, `src-sts-wiki-mental-fortress` | Calm/Wrath、能量、抽牌、Block reader 与最小循环 | 薄牌组、费用、伤害出口、手牌上限均是 guard |
| 敌人考题 | 7 | `src-sts-wiki-corrupt-heart`, `src-sts-wiki-time-eater`, `src-sts-wiki-awakened-one`, `src-sts-wiki-gremlin-nob`, `src-sts-wiki-chosen`, `src-sts-wiki-shapes`, `src-sts-wiki-champ` | 每牌税、十二牌阈值、Power/Skill/非 Attack/多段/阶段清除 | 具体敌人攻击不同 link，不是全局禁止某流派 |
| 实战与失败方法 | 11 | `src-sts-guide-foundation`, `src-sts-guide-ironclad-a20`, `src-sts-guide-modern-ironclad`, `src-sts-guide-ironclad-streak`, `src-sts-guide-defect-builds`, `src-sts-guide-defect-lightning-standard`, `src-sts-guide-silent-style`, `src-sts-guide-poison-defense`, `src-sts-guide-watcher-infinite`, `src-sts-guide-watcher-archetypes`, `src-sts-guide-ten-ways` | 五条链的实践、转型、历史作业与失败模式 | EA/Ascension 15 指南只作时期实践；攻略不等于统计或唯一最优解 |

详细 URL、发布日期、可访问性、claim ids 与逐页限制见 `../source-index.md`。

## 检索日志与停止理由

- 筛选 277 条官方 Steam News 标题，打开与版本、角色、Final Act、Poison、Blizzard、Custom 和结算顺序相关的正文；13 个功能不同的节点进入索引。
- wiki.gg 当前返回 403，未绕过。Fandom MediaWiki API 可读取规则正文；多数页面使用完整 wikitext，Blizzard 改用展开后的 `prop=text`，因为其 wikitext 只有模板与历史、没有直接显示卡牌规则。从 Gameplay、关键词、卡牌、遗物和敌人页中保留 35 个直接改变 owner、counter、version 或 guard 的页面。
- Steam Guides 加 `&l=english` 后正文稳定可读。先筛高评价前 90 个标题，再针对 Defect 做纠错检索，最终保留 11 篇能补构筑、转型、敌人或历史失败的指南；`The Undefeatable Defect` 明确属于 Endless Mode，已排除并由 2025 发布、2026 更新且以普通 Act 1–3 为主路线的 `Defect Lightning Guide v1.1` 替换。后者两处 Endless 比较也被明确排除。
- Spirelogs 路由不稳定，正文/样本无法验证。官方 `2.2` 确认曾整理 7700 万局 run archive，但入口在 Discord 数据频道；本次未下载或计算，因此没有 `statistics` 来源和任何采用率/胜率结论。
- 视频没有出现既有可读 transcript、又能新增 owner/pivot/counter 的候选；标题、缩略图、无字幕 footage 不计来源。GitHub 搜索限流、Google challenge 与 Bing RSS 噪声均未绕过。
- 五条闭环、版本冲突、主要敌人包和生命周期已达到信息饱和；继续枚举卡牌评级或单局清单不再改变结构理解，故停止。

## 标准 run 的真实循环与决策

玩家在分叉地图上选择战斗、精英、休息点、商店、事件和 Boss 路径；战斗中每回合以有限 Energy 打出抽到的牌，敌人公开 Intent。胜利后从随机卡牌奖励中选一张或跳过，并通过商店购买卡牌/遗物/药水或移除卡牌。真正的构筑决策不是先选“流派标签”，而是同时管理：

- 当前 Act 的前置伤害、AOE、Block、持续战与 Boss 能力考试；
- 牌组厚度、抽牌顺序、Energy、升级、移除和手牌上限；
- 核心 reader 是否已有可靠 supplier，而不是只拥有孤立 payoff；
- Gold、路径生命、营火升级/回复、商店移除和精英遗物的机会成本；
- 面对已知敌人时，是补弱点、延后 greed、保留 Catalyst/Power，还是放弃理论终局组件。

`Slaying The Spire From The Ground Up: Building a Good Foundation`、以 EA/Ascension 15 为框架且 2023 更新的 `Slay the Spire Silent Style`，以及历史 `Ten Ways to Lose` 都支持“先解当前考题，再由实际 offer 决定方向”。因此下面五条链是跨期规则互证后的可核结构，不是开局应强塞的当前 meta recipe。

## 构筑一：Ironclad Block → Body Slam

### 闭环

| 语法位置 | 具体内容 |
|---|---|
| engine | Block 卡反复建立当回合防御；Barricade、Calipers 或 Blur 类效果改变跨回合保留，Entrench 读取当前 Block 再获得等量 Block |
| state/resource | 当前 Block，通常在玩家下回合开始失去；上限 999 |
| payoff | Body Slam 在结算时以当前 Block 给出基础攻击伤害；升级只把费用从 1 降为 0 |
| survival | Block 本身吸收伤害；Barricade 完全保留，Calipers 在回合开始损失 15，二者/Blur 的 retention 不是同一规则且可能冗余 |
| spatial condition | 无棋盘站位；等价的空间预算是抽牌顺序、Energy、手牌与在敌人攻击前建立 Block 的时序 |
| payoff owner | Body Slam 是具名最终攻击 owner；不是“所有 Block 自动造成伤害” |
| economy / pivot | 先拿可独立防守的 Block 与当前伤害，再在 retention/Entrench 已可达时提高 Body Slam 价值；缺攻击时不能等待完美 Barricade 套件 |
| counter | Gremlin Nob 对 Skill 增长、Chosen 向抽牌堆塞 Dazed、Heart 每打牌收费且限制单回合伤害；慢启动或抽不到 payoff 会把高 Block 变成无输出 |

### 精确边界

- 当前注册来源只直接证明 Body Slam 是 Attack，且其伤害等于结算时当前 Block；没有直接核实 Strength、Weak、Vulnerable 等外部 modifier 与它的精确交互或结算顺序。因此本 dossier 只保留“Block 提供 Body Slam 牌面/基础读取值，Body Slam 是最终伤害 owner”，不对未核外部 modifier 作确定结论。
- Entrench 的“获得等同当前 Block”是一层读数；只有能持续抽回、支付费用并保留状态时才可能复利。Block cap、牌堆和敌人输出限制增长。
- Barricade 不失去 Block；Calipers 是回合开始只失去 15；Blur 是另一种持续窗口。它们不能统称一条数值，也不能无成本叠加收益。

## 构筑二：Defect Frost / Focus / Orb-slot → Blizzard

### 闭环

| 语法位置 | 具体内容 |
|---|---|
| engine | Channel Frost；默认三个 Orb slots，填满后新 Orb 挤出 rightmost/active/next-to-evoke Orb并 Evoke；回合结束按 right-to-left 触发被动 |
| state/resource | 当前 Frost Orbs、Orb slot 容量、Focus，以及“本场战斗累计 Channel Frost 数”四个不同状态 |
| payoff | Frost passive/Evoke 提供 Block；Blizzard 读取累计 Channel Frost 次数造成全体伤害 |
| survival | Frost Block 由 Orb/Focus owner 产生，不受 Dexterity/Frail；Loop 可在回合开始额外触发 active Orb passive |
| spatial condition | Orb 队列位置就是顺序空间：active/rightmost 的被动、Evoke 和满槽挤出时机不同；Capacitor 改容量而非数值 |
| payoff owner | Frost owns Block；Focus 改 Lightning/Frost/Dark passive/Evoke；Blizzard 单独拥有累计账本→AOE 输出 |
| economy / pivot | 早期仍需即时伤害。只有 Frost 供应密度、抽牌与长战可达时才把 Blizzard 当 payoff；否则转 Lightning/Electrodynamics 或普通攻击 |
| counter | Gremlin Nob 惩罚 Skill-heavy 启动，Chosen 塞 Dazed；慢 Channel 在高前置伤害前会失败，低 Focus 或过多空槽也会延迟存活 |

### 不可合并的模块

- Focus 不影响 Plasma；也不直接放大 Blizzard，因为 Blizzard 读累计 Channel Frost 数，而非 Focus、当前 Frost 或当前 Block。
- Electrodynamics 把 Lightning 的被动目标改为全体，是另一输出 module；Dark 的蓄积/Evoke、Plasma 的 Energy 又是其他 owner。它们可以同牌组出现，但不能叫一个“元素数值”。
- 更多 slots 会减少被动触发的频繁 Evoke，也可能提高持续被动容量；容量不是单向伤害升级。

## 构筑三：Silent Poison → Catalyst

### 闭环

| 语法位置 | 具体内容 |
|---|---|
| engine | 反复向一个敌人施加 Poison，并用抽牌/保留效果等到合适的爆发回合 |
| state/resource | 敌人当前 Poison 层数；敌人回合开始损失等同层数的 HP，随后 Poison 减 1，且穿透 Block |
| payoff | Catalyst 消耗 1 Energy，把已有 Poison 增至两倍；升级后三倍；打出后 Exhaust |
| survival | Poison 的输出延后到敌人回合，Silent 必须以 Block、Weak、遗物或前置伤害买时间 |
| spatial condition | 无棋盘站位；关键是单目标选择、回合/阶段窗口、保留 Catalyst 和在清除前完成击杀 |
| payoff owner | Poison owns delayed HP loss；Catalyst 是具名倍率 reader；防御牌不自动拥有伤害 |
| economy / pivot | 没有稳定 supplier 时不应只拿 Catalyst；多目标、Artifact 或快速小怪要求补 AOE/前置攻击，Boss 前再提高单目标倍率 |
| counter | Artifact 每层阻止下一次 debuff application；Donu/Deca 带 Artifact。Time Eater/Awakened One 进二阶段清 debuff，The Champ 半血转阶段清 debuff并成长 |

### Catalyst / Artifact 精确交互

维护页明确把 Catalyst 解释为“额外施加当前 Poison 的 1 倍或 2 倍”，并明确写 `Artifact blocks Catalyst's Poison`。因此：

- 无论目标是否已有 Poison，Catalyst 的这次额外 Poison application 会被一层 Artifact 阻止；已有 Poison 本身不会因 Artifact 被清除。
- Artifact 是按 application 消耗的计数 guard，不是永久免疫。可先用较低价值 debuff 剥除，再在阶段窗口使用 Catalyst。
- 这里不推断内部 action queue、Snecko Skull 之外的所有修正顺序，也不把 The Champ 的阶段清除与 Artifact 混成同一机制。

## 构筑四：Watcher Calm / Wrath → Rushdown loop

### 最小闭环

| 语法位置 | 具体内容 |
|---|---|
| engine | 先进入 Calm，再以一张牌进入 Wrath；离开 Calm 获得 2 Energy，进入 Wrath 后 Rushdown 抽 2 |
| state/resource | 当前 Stance、Energy、牌堆/弃牌堆、十张手牌上限和本回合打牌数 |
| payoff | 进入 Wrath 的牌已先进入弃牌堆，Rushdown 才抽 2，使 Calm/Wrath 两张牌可重新回手；Wrath 使造成与承受伤害翻倍 |
| survival | Mental Fortress 每次 Stance change 提供 Block；退出 Wrath或在敌人行动前结束战斗仍是必要风险控制 |
| spatial condition | 牌堆必须薄到整副可握在手中；draw pile 为空、弃牌回洗和手牌上限共同决定能否稳定取回两张牌 |
| payoff owner | 带伤害的 stance 卡，或 Panache/Letter Opener 类独立伤害源；Rushdown 只 owns draw，Mental Fortress 只 owns Block |
| economy / pivot | 最小线需要 Rushdown、一张至多 1 费 Calm、一张至多 1 费 Wrath（或总费用不高于退出 Calm 回能）并删薄牌组；未闭合时仍要能打普通战斗 |
| counter | Time Eater 第 12 张牌结束回合并成长；Heart 每张牌造成 Beat of Death 伤害且有每回合伤害上限；Chosen 塞 Dazed，手牌/牌堆污染会断循环 |

### Guard 与敌人税

- 这是确定性循环的必要条件，不是“拿到 Rushdown 就无限”。缺任一 stance 牌、牌组太厚、费用总和超过回能、手牌被污染或无伤害出口都不闭合。
- Heart 与 Time Eater 不简单禁止循环。Heart 给每张牌生命成本并限制单回合结算输出；Weekly Patch 53 明确 Beat of Death 在卡牌动作完全结算后伤害。Time Eater 让第 12 张成为必须规划的 turn boundary，计数跨回合。
- Mental Fortress 能对冲每牌税，但它自身是 Power；Awakened One 在第一阶段每打 Power 增长 Strength。因此防御 reader 也支付敌人 matchup 成本。

## 构筑五：Corruption → Exhaust → Feel No Pain / Dark Embrace / Dead Branch

### 闭环

| 语法位置 | 具体内容 |
|---|---|
| engine | Corruption 把 Skills 改为 0 费且打出后 Exhaust；每次 Exhaust 触发多个独立 reader |
| state/resource | 手中/抽牌堆/弃牌堆的有限 Skills、Energy、Exhausted cards、手牌空位和随机生成池 |
| payoff | Dark Embrace 每次 Exhaust 抽牌；Dead Branch 每次 Exhaust 向手牌生成一张随机合法卡；输出仍由抽到/生成/原有攻击和其他 reader 拥有 |
| survival | Feel No Pain 每次 Exhaust 提供 Block；0 费 Skills 提供短期爆发与防御。当前登记的 FNP 页不直接证明其与 Dexterity/Frail 的边界，故不作该项断言 |
| spatial condition | 手里十张上限、抽牌与生成的先后、牌堆厚度和 Status/Curse 占位构成“牌区空间” |
| payoff owner | Corruption owns 费用/离场 rewrite；FNP owns Block；Dark Embrace owns draw；Dead Branch owns random generation；最终攻击仍有独立 owner |
| economy / pivot | Skills 是不可再生的战斗内燃料；应按战斗长度、Boss 阶段和抽牌速度决定何时打 Corruption，Dead Branch 则用随机广度换可预测性 |
| counter | Awakened One 对多 Power 增长；Chosen 的 Dazed 与敌人 Status 塞牌污染抽牌/手牌；过早烧光 Skills 会在长战失去防御，随机牌也可能挤占关键牌 |

### 资源所有权

- Exhaust 只把卡从本场战斗循环移除，战斗结束后回到永久牌组；不是永久删卡。
- 0 费、Exhaust、Block、draw、random generation 和最终 damage 是六个事件/owner。把它们压成“腐化无限”会隐藏实际 guard。
- Dead Branch 提供随机合法卡，不保证继续生成 Skill、继续 Exhaust、补充伤害或形成无限。它既能补燃料，也能制造 hand/deck pollution 与路线漂移。

## 经济、招募与转型对应关系

《杀戮尖塔》没有自走棋商店单位与 bench，但其卡牌/遗物/移除经济可直接映射为构筑机会成本：

- 卡牌奖励可以跳过；“更多同标签牌”会稀释抽到关键 reader 的概率。
- 商店 Gold 同时竞争买卡、买遗物、药水和移除，移除是提高关键闭环可达率的长期投资。
- 角色卡池限制不是体系标签保证；构筑方向取决于已拥有的 supplier、reader、当前 Act 考题与未来路径。
- 遗物通常不占手牌/每回合 Energy，但稀缺且难以定向获取；不能把理论遗物视为 build 默认组件。
- 转型发生在卡牌选择、升级与移除上。历史指南的精确清单若要求多张稀有牌/遗物而无过渡方案，只能作为上限或失败案例。

## 敌人包如何攻击不同 link

| 敌人 | 受压 link | 设计含义 |
|---|---|---|
| Gremlin Nob | 每打 Skill 增长 Strength | 攻击 Block/Poison/Orb/Exhaust 的 Skill-heavy 启动，不取消其后期价值 |
| Chosen | 每打非 Attack 向抽牌堆塞 Dazed | 攻击 draw、薄牌组和随机生成稳定性；输出不足时污染会滚雪球 |
| Spiker / Shapes | Thorns | 惩罚多段小攻击，迫使改用 DOT、Orb、一次大 hit 或先处理目标 |
| The Champ | 半血转阶段、清 debuff并增长 | 给慢 Poison 一个明确 Catalyst 爆发窗；清除不是 Artifact |
| Time Eater | 跨回合计数，第 12 张终止回合并增长；转阶段清 debuff | 攻击 Rushdown/Corruption 的每牌效率与 Poison 阶段管理，不是禁用低费牌 |
| Awakened One | 第一阶段每打 Power 增长，Strength 带入第二阶段；二阶段清 debuff | 攻击 Barricade/Rushdown/FNP/Dark Embrace 等 Power 套件的施放时机 |
| Corrupt Heart | Beat of Death、每回合 Invincible 上限、后续 Artifact/成长 | 同时测试每牌成本、循环防御、爆发跨回合与 debuff 入口；没有单一万能解 |

失败解释因此应显示哪条 link 断裂：supplier 未到、reader 未抽到、Energy 不足、手牌被污染、阶段清除、Power/Skill/每牌税、前置伤害或伤害上限，而不是只写“数值不够”。

## 生命周期、负案例与版本冲突

本 checkpoint 计十六个 materially distinct negative/reworked families：

1. EA、1.0、Watcher/2.0、2.2 大型平衡、2.3 input 与 2.3.4 兼容修复必须分离；最后版本不等于最后平衡。
2. Ascension 从 EA 加入并继续演变，历史层数/规则不能冒充末版完整难度表。
3. Custom Mode 的 modifier 不属于标准 run；模式变体不能反向污染核心规则。
4. 历史 Poison cap 变更说明成长上限本身会改变状态体系身份；不把旧 cap 写成当前值。
5. Weekly Patch 53 调整 Beat of Death 至卡牌动作完全结算后，证明“每打牌受伤”仍需确定结算点。
6. Barricade、Calipers 与 Blur 的 retention 重叠/冗余显示同一防御状态可有不同 rewrite owner，不能只比较数值。
7. Body Slam 以当前 Block 给出该 Attack 的伤害读取值；外部 modifier 未在本来源包中直接核实，不能擅自声称叠加、无关或转换。战报仍需将 Block 读取与其他已核修正分栏。
8. Orb slot、当前 Orb、Focus、Channel ledger 与 Evoke order 被混成“元素强度”时，容量、数值和时序取舍会消失。
9. Blizzard 的防御供应→累计账本→延迟 AOE 在前置伤害面前有真实成型窗；“Frost 等于输出”会隐藏 reader。
10. 精确 Power/Lightning 清单不等于可达构筑；强塞 archetype 会在 offer、前置伤害和 Boss counter 前失败。
11. Catalyst 被 Artifact 挡的是额外 Poison application；阶段清除、已有 Poison 与 Artifact guard 必须分别显示。
12. Rushdown 需要最小牌组、费用守恒、伤害出口和手牌/污染 guard；缺一项便不是循环。
13. Heart 与 Time Eater 给循环不同的每牌/阈值成本，而不是脚本式“禁止循环”；UI 应解释是哪一个 guard 生效。
14. Awakened One 使 Power 套件支付敌人成长税；Power 不是无条件部署的永久被动槽。
15. Corruption 将有限 Skills 变成战斗燃料；过早 Exhaust 完成会把后半战的生存资源烧空。
16. Dead Branch 的随机生成可能补燃料，也可能占满手牌、改变牌组可预测性；生成不等于递归保证。官方 archive 未验证则不能用大样本为这些结论背书。

一般评论热度、Steam 评分、速通/连胜个案和“无敌”标题没有拆分凑数。Cumulative explicit negative/reworked cases：280。

## 对本项目可迁移

- 盾体系与元素体系应继续作为正交主轴。Block/Frost 可以买时间；Poison/Blizzard/Body Slam 分别需要明确的状态 reader 才拥有输出。
- 用户提出的“攻击额外造成最大生命百分比”“全队额外生命/防御转某个射手攻击”在结构上可行，但必须由装备/遗物/Hero 明确拥有 converter，并声明 supplier 集合、读取时点、recipient、自身是否计入、cap、refresh、slot cost、来源 lineage 和 counter。
- Body Slam 是最干净的 defense-to-offense 模板：当前防御状态→具名 Attack 读取→最终伤害 owner。未核外部攻击 modifier 留作未决，不让原始 Shield 默认同时承担减伤与输出。
- Blizzard 展示“生存状态”和“输出账本”可以相关但不相同：当前 Frost/Block 负责活着，累计 Channel 次数负责 AOE。这样破盾/降 Focus/打断 Channel 可以攻击不同 link。
- Artifact、阶段清除和 counter 敌人应针对链路而非标签。剥离 debuff guard、加每牌税、插入污染牌、限制单回合伤害、惩罚 Power/Skill 是五种不同考题。
- Corruption 包说明遗物/装备重写一条规则后，必须保留有限燃料、手牌容量、随机污染、敌人阶段和最终伤害 owner；否则转换器会吞掉整个体系的决策。
- 战报需显示 supplier → state/ledger → reader/rewrite → recipient/final event，并标出 guard：上限、阶段清除、Artifact 消耗、十二牌阈值、每牌伤害、满手取消和当前 Block 读取。

## 不兼容与未决

- 不复制回合制卡牌操作、四角色卡池、三 Act 地图、Ascension 数值、具体卡牌/遗物内容或敌人名单到本项目。
- 项目是英雄 roster autobattler；抽牌顺序/手牌可类比时序和槽位预算，但不能冒充站位、目标或单位 AI 证据。
- 没有可核当前总体 build usage、胜率或死亡分布。官方 7700 万局 archive 未获得，Spirelogs 不稳定，故统计结论保持空白。
- Fandom 是社区维护档案；末修订日只说明页面历史，不证明官方持续维护或所有边缘交互无误。
- Body Slam 与 Strength/Weak/Vulnerable 等外部 Attack modifier 的精确交互未由本来源包直接建立；Catalyst/Artifact 与其他特殊 relic/debuff 的组合也只按本次明确来源限定，未核交互不扩写。
- 早期 EA Ironclad/Defect/Poison/Watcher 指南只作历史实践或结构互证，不能覆盖 2.2/2.3.4 的当前强度。
- 本 dossier 不授权第一版盾、元素、遗物、装备、英雄、敌人或数值；它只提供后续设计讨论可用的 ownership、guard、counter 与失败解释结构。

## Disposition

`anchor-retained`。

保留理由：59 个实质来源达到锚点多类型门槛；五条具名链均能回到规则页和实践指南，完整覆盖 engine、state/resource、payoff、survival、时序空间、payoff owner、经济/pivot、counter 和版本边界。尤其提供了 Block→Attack、Frost→独立累计账本、Poison→倍率/Artifact、Stance→能量/抽牌守恒、Exhaust→多 reader/rewrite 五种不同转换架构。

停止理由：官方版本链、相关规则页、主要实践指南和敌人考题已饱和；未获统计和视频 transcript 的路线已诚实记录。继续枚举全卡池、单局清单或无字幕视频不会改变本项目关心的 owner/pivot/counter/lifecycle 结论。
