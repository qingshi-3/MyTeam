# Teamfight Tactics

## 身份与研究时期

- `title_id`: `teamfight-tactics`
- 子类型：多人共享牌池、回合制 PvP 自走棋。
- 状态：持续运营；规则、英雄池、羁绊、强化符文和装备随 set / patch 大幅轮换。
- 观察时期：历史复盘覆盖 Set 7/8、Set 10 与 2026 年两个赛季；实战节奏以 Set 17 指南和 2026-09-02 可访问通用指南为主。
- 直接体验：本轮未安装或实玩；结论来自官方复盘、补丁正文和可读实战指南的交叉验证。

## 检索日志

1. 先检索官方规则、补丁和赛季复盘，打开 Riot 的 Patch 14.1、Monsters Attack!、Space Gods、Lore & Legends 四页。
2. 检索升级、刷新、经济、装备、阵容选择、侦察、站位和强化符文攻略，打开 BunnyMuffins 一页与 Bamboo Gaming 六页。
3. 主审校准发现宏观经济线不足以替代具体构筑后，补查 Set 17 的具名阵容、独立重抽概率说明和独立站位指南；打开 TFT Flow、TFTEmblem 与 TFT School 三页，并只采用正文可核验字段。
4. 尝试访问 Mobalytics 指南与 League Wiki，均返回 HTTP 403；没有绕过限制，也没有将搜索摘要当作证据。
5. DuckDuckGo HTML 仅用于发现底层页面；Bing 结果相关性不足，两者均不计入 deep source。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-tft-riot-patch-14-1` | [Patch 14.1](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/teamfight-tactics-patch-14-1-notes/) | Riot Games / 2024-01-09 | official-patch / A | Heartsteel、连胜经济及多轴平衡快照 |
| `src-tft-riot-monsters-learnings` | [Monsters Attack! Learnings](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-teamfight-tactics-monsters-attack-learnings/) | Riot TFT Team / 2023-05-22 | official-dev / A | Dragons、Threats、后排访问、经济特质、Hero Augments 复盘 |
| `src-tft-riot-space-gods-reviewed` | [SPACE GODS Reviewed](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-tft-space-gods-reviewed) | Riot TFT Team / 2026-08-21 | official-dev / A | 系统范围、横向羁绊、低费重抽上限 |
| `src-tft-riot-lore-legends-reviewed` | [Lore & Legends Reviewed](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-tft-lore-legends-reviewed) | Riot TFT Team / 2026-03-18 | official-dev / A | roster 成本、Unlock、双核心交接、Artifact 锐度 |
| `src-tft-bunnymuffins-leveling` | [Leveling Guide Set 17](https://bunnymuffins.lol/tft-leveling-guide/) | BunnyMuffins / 2026-02-23 | strategy-guide / C | Fast 8/9、重抽、连胜/连败和牌池概率 |
| `src-tft-bamboo-economy` | [Economy Guide](https://www.bamboogaming.net/tft/economy) | Bamboo Gaming / 未标注 | strategy-guide / C | 利息、升级、刷新、稳血与节奏机会成本 |
| `src-tft-bamboo-items` | [Items Guide](https://www.bamboogaming.net/tft/items) | Bamboo Gaming / 未标注 | strategy-guide / C | 立即合装、装备载体及输出/前排/功能分配 |
| `src-tft-bamboo-comp-selection` | [Comp Selection Guide](https://www.bamboogaming.net/tft/comp-selection) | Bamboo Gaming / 未标注 | strategy-guide / C | 多信号选阵、同行、锁阵与转型成本 |
| `src-tft-bamboo-scouting-positioning` | [Scouting and Positioning](https://www.bamboogaming.net/tft/scouting-positioning) | Bamboo Gaming / 未标注 | strategy-guide / C | 侦察、换边、主坦和后排威胁 |
| `src-tft-bamboo-augments` | [Augments Guide](https://www.bamboogaming.net/tft/augments) | Bamboo Gaming / 未标注 | strategy-guide / C | 强化符文与当前棋盘/经济/装备的匹配 |
| `src-tft-bamboo-beginner` | [Beginner Guide](https://www.bamboogaming.net/tft/beginner-guide) | Bamboo Gaming / 未标注 | strategy-guide / C | 商店、备战区、上阵与基础站位循环 |
| `src-tft-flow-primordian-reroll` | [Primordian Reroll](https://tftflow.com/composition/set17/primordian-reroll) | TFT Flow / 2026-04-11，更新 2026-07-29 | strategy-guide / C | 具名阵容、Briar 核心、装备优先级、承伤站位与阶段节奏 |
| `src-tft-emblem-reroll-set17` | [Set 17 Reroll Strategy](https://tftemblem.com/guides/reroll-strategy-guide/) | TFTEmblem Team / 2026-06-03 | strategy-guide / C | 费用对应等级、重抽方法、同行与低血 roll-down |
| `src-tft-school-positioning-set17` | [Set 17 Positioning](https://tftschool.com/blog/tft-positioning-guide) | TFT School / Set 17 Patch 17.1 | strategy-guide / C | carry 保护、诱饵、AOE 分散和当轮侦察 |

来源集中度限制：Bamboo Gaming 的六页按不同功能页面登记，但共享同一发布者，只代表一个实战编辑视角；因此本 dossier 不把它们伪装成六份独立社区共识。补充的 TFT Flow、TFTEmblem 与 TFT School 提供了三个独立实战视角，但仍缺少可访问的代表性统计样本。锚点门槛的类型多样性来自官方补丁、官方开发复盘与策略指南，而非用同站页数充当独立观点数。

## 真实循环与玩家决策

玩家在每个备战回合同时管理金币、等级、商店、备战区、上阵人数、装备、强化符文与站位，然后观看自动战斗并以剩余生命承受失败代价。决策并非“先攒到 50 金再说”：保连胜、止连败、升级增加人口、在特定等级重抽低费牌、为高费牌保经济、低血时提前 roll down，都在争夺同一笔金币。

阵容方向也不是单由羁绊名称决定。可见商店、已持装备、强化符文、经济、血量、同行数量和当前最强棋盘共同限定选择。立即合成泛用装备能保血，却可能牺牲未来最优装备；等完美散件则承担当下战败成本。临时装备载体让玩家把战力从中期单位转交最终核心，是降低转型摩擦的重要桥接结构。

侦察将空间决策接入经济与构筑：玩家观察潜在对手、后排威胁和目标位置，在有限备战时间内换边、藏核心、移动主坦或接受对局风险。站位不是构筑完成后的装饰，而是构筑能否兑现的条件。

## 宏观路线一：低费重抽核心

- `engine`：停留在适合目标费用的等级，用商店概率、共享牌池和连续购买累积同名低费单位。
- `state/resource`：金币、利息、玩家生命、目标单位副本数、等级、备战区空间与同行占用的牌池。
- `payoff`：把一名低费单位升至三星，使其在中期形成显著战力或继续追求终局上限。
- `survival`：在慢抽经济与主动 roll down 之间切换；前排装备和可立即合成的泛用装备负责减少成型前掉血。
- `spatial condition`：后排核心需避开突进、钩取、眩晕和爆发；主坦位置影响敌方首个目标与移动路径。
- `payoff owner`：三星低费 carry；前排和控制单位是生存/兑现支持者，而非共享全部收益。
- `pivot/counter`：同行过多会压缩牌池；血量过低或副本来得太慢时，必须停止贪三星，花钱稳定或转入更便宜的现成战力。对手可通过后排访问、站位针对和更快节奏缩短其成型窗口。
- `version context`：具体等级、概率和哪张低费牌可作为终局核心高度依赖 set；官方也持续讨论 Champion Augment / 低费重抽是否应拥有吃鸡上限。

## 宏观路线二：Fast 8 的灵活高费核心

- `engine`：连胜或稳健经济提供升级资金，中期用 strongest board 与临时装备载体过渡，较早到 8 级搜索高费核心和可插拔功能单位。
- `state/resource`：金币、经验、生命、可转移装备、装备载体、商店中的高费牌与剩余 roll-down 时点。
- `payoff`：高费 carry 获得合适输出装备，搭配明确主坦和控制/辅助单位形成终局棋盘。
- `survival`：中期不空等“最终阵容”，而用已升星单位、前排装备、即时合装和灵活羁绊保血。
- `spatial condition`：主坦引导第一波火力；carry 换边规避后排威胁；控制与功能单位按潜在对手调整位置。
- `payoff owner`：一名主要高费 carry；主坦承担防御装备，功能单位提供控制/破防/增益。
- `pivot/counter`：血量或经济不足时不能硬上 Fast 9；高费核心被同行争抢、装备不匹配或 roll down 落空时，应转向可用的次级 carry 或便宜稳定阵容。对手通过侦察换位、后排访问和针对主坦影响其兑现。
- `version context`：高费单位、羁绊桥和推荐升级节点随 set 改变；结构性价值在于“可转装备 + 中期载体 + 到点搜索”，不是照搬某套英雄名单。

## 具名构筑：Set 17 Primordian Reroll

核验来源：`src-tft-flow-primordian-reroll` 提供具名构筑、阶段节奏、单位/装备与站位正文；`src-tft-emblem-reroll-set17` 独立支持 Set 17 的费用—等级概率、慢抽/硬抽和同行转型原则。

- `engine`：在适合 1/2 费单位的等级慢抽 Primordian 低费核心；第三阶段保持 50 金以上抽取，未成型时在规定节点硬抽，成型后再补等级。
- `state/resource`：金币、Briar / Rek'Sai / Cho'Gath 等核心副本、Rek'Sai 的成长层数、备战区空间、生命和同行占用的共享牌池。
- `payoff`：Briar 是正文明确的首要 carry；其被动利用低生命获得更高攻速，阵容通过受控承伤让她尽早进入输出状态。Kai'Sa / Jhin、Bel'Veth 等承担后续或远程补伤害，具体留用随商店和装备变化。
- `equipment`：先保障 Briar 装备与 Rek'Sai 的 Evenshroud；正文明确认为该阵容的 Evenshroud 优先于 Sunfire。Sterak's、Titan's 与 Evenshroud 会争用剑、甲、腰带等组件，形成真实的散件机会成本；剩余装备再分给主坦或 Bel'Veth。若 Cho'Gath 难以三星或成长不足，防装转给 Rek'Sai。
- `survival`：Rek'Sai / Cho'Gath 等前排在 Briar 完成承伤触发后接管仇恨；装备分配必须兼顾 Briar 的输出窗口和前排续航，不能只追垂直 N.O.V.A. 数量而塞入低质量单位。
- `spatial condition`：Briar 可单独或优先放在前排，使其开局承受主要仇恨并压低生命；坦克靠近 Briar，等待她通过 Rogue 仇恨脱离后接走攻击。这里“carry 站前排”不是通用摆法，而是被动收益和仇恨转移共同要求的发动条件。
- `payoff owner`：Briar；坦克和 Evenshroud 是破防/生存支持，Kai'Sa、Jhin 或 Bel'Veth 是补充输出，不应模糊为全队平均吃收益。
- `pivot/counter`：同行会同时降低低费三星完成率，TFTEmblem 建议在被争抢时提前硬抽或转升级路线。TFT Flow 还指出，若特定单位无法三星、成长层数不足或散件不匹配，应更换防装所有者和次级 carry。对手可用爆发在坦克接仇恨前击杀 Briar、用重伤/控制压缩她的低血输出窗，或利用站位让仇恨无法顺利转交；后两项是由机制结构推导的反制问题，不能冒充原攻略的逐字推荐。
- `version context`：Set 17 历史构筑页发布于 2026-04-11、更新于 2026-07-29；访问时网站已进入 Patch 18.1b。页面保留了多个 Set 17 patch 段落，因此本 dossier 只提取其稳定构筑关系，不把评论区或不同补丁建议混成单一最优名单。

## 构筑语法与机会成本

TFT 的通用语法可归一为：

> 经济/商店概率或连胜引擎 + 金币/等级/生命/副本状态 + 三星低费或高费 carry 收益 + 中期 strongest board 与前排生存 + 侦察驱动的站位条件

关键机会成本包括：利息对即时战力、升级对刷新、完美装备对立即合装、追三星对转型、垂直羁绊对横向功能单位、终局上限对当前血量。强化符文会改写其中一条规则或资源曲线，但实战指南明确反对脱离当前棋盘、装备、经济和血量盲选“最高胜率”选项。

## 空间后果、反制与适应窗口

- 后排核心拥有高价值但低容错的空间条件；突进、钩取、眩晕和爆发是显式反制。
- 主坦位置改变敌方首要目标与移动，可为后排争取输出时间，也可能把敌人引向核心。
- 侦察提供适应窗口，但潜在对手不唯一，因此换边是风险管理而非确定解。
- 同行争抢是构筑层反制：它通过共享牌池降低升星概率，迫使玩家转型或接受更高刷新成本。
- 玩家能把失败解释到至少四层：经济节奏、阵容信号、装备分配、站位/目标；但随机商店和多潜在对手也会降低单回合归因确定性。

## 版本生命周期与负面案例

1. **Dragons 的过度捆绑**：官方复盘认为双槽、高费用且贡献大量羁绊的单位压缩阵容多样性和灵活性。问题不是“巨龙太强”这一句，而是单个招募决定同时占用人口、经济和羁绊预算。
2. **Threats 的可插拔成功点**：无羁绊锁定的功能单位允许按棋盘缺口补控制、前排或伤害，是与过度捆绑相反的桥接方式。
3. **Hacker/Assassin 式后排访问**：官方将快速删除后排 carry 的体验描述为过于挫败，并倾向让后排移除更困难。反制如果绕过前排过于直接，会让玩家感觉整个生存层无效。
4. **Hero Augments 的控制权问题**：高影响、定向到单英雄的系统放大了分发与重抽控制问题；当强化直接锁定 payoff owner 时，玩家需要足够改选空间。
5. **Realm mechanic 范围过大**：同时改变强化分配、核心装备等多个轴，增加 live balance 与开发周期负担。一个主题系统改写越多基础规则，越难定位失败来源。
6. **双 carry 强制交接**：先要求玩家把资源集中给第一个核心，再要求转给第二个核心，会与装备经济和玩家直觉冲突。明确的坦克/输出职责配对比二次换核更易读。
7. **Artifact 泛化的代价**：把尖锐、专属、高峰值的物品改为人人可用的安全效果，能降低死选，却会损失构筑身份；可用性和独特性必须分别治理。

这些均为官方生命周期材料支持的具体设计案例；本 dossier 不以游戏热度、评分或商业结果代替机械成败判断。

## 社区与策略观察

本批未获得足够独立的统计或大规模社区样本，因此不声称存在统一“玩家共识”。TFT Flow、TFTEmblem、TFT School、BunnyMuffins 和 Bamboo Gaming 已构成五个攻略发布者视角；它们共同支持经济、血量、装备、同行和站位的联动，但并不自动代表总体玩家。可支持的较窄观察是：多个实战作者反对“永远攒 50”“开局锁死阵容”或脱离敌情的固定摆位；具体节点仍受作者、段位与版本限制。

## 对本项目的迁移

可迁移：

- 防御者、输出核心和功能单位应有清楚的收益所有者与桥接关系；“全队提供生存、单核负责兑现”可以成立，但不能让防御投资天然免费变成全队同额输出。
- 装备载体说明临时单位可以承接当前战力，并在明确节点把资源交给最终核心；这比要求玩家长期空等完整套件更健康。
- 横向功能单位和不锁方向的桥接选项能缓解垂直体系缺口，尤其适合补破盾、控制、净化或对空，而不必把每个反制都塞进同一套羁绊。
- 侦察—调整—观看结果形成清晰适应窗口；本项目可把敌人预告、阵型调整和结算报告做成单人版本，而不是复制 PvP 猜边。
- 高影响遗物/装备若改写收益所有者、资源曲线或输出转化，需要显式控制权、放弃成本和失败归因。

不可直接迁移：

- 共享牌池、八人淘汰赛、玩家生命连败容错、赛季轮换 roster 和多潜在对手猜位是 TFT 特有假设。
- 持续运营允许用补丁修复极端组合；本项目第一版不能把可读性和反递归保障留给线上热修。
- TFT 的羁绊阈值、抽卡概率和英雄费用不能成为本项目的默认数值模板。

## 未决问题

- 缺少可访问统计来源，尚不能量化不同经济线、强化或阵容的实际采用率与胜率。
- 虽已补充三个独立攻略发布者，后续饱和批次仍应寻找可访问的比赛或统计数据，而不是用更多相似攻略代替采用率/胜率证据。
- 官方复盘跨多个 set；任何细节迁移前都需区分持续结构与已移除规则。

## Disposition

- `disposition`: `anchor-retained`
- 置信度：核心经济/转型/站位结构为高；具体节奏与 set 内数值为中；玩家代表性为低至中。
- 判定：14 个实质非商店页面，覆盖 `official-patch`、`official-dev`、`strategy-guide` 三种来源类型和五个攻略发布者，并同时提供规则/版本、具名版本构筑与实战决策材料；达到 anchor 门槛。宏观重抽/Fast 8 路线不计作具名构筑，Set 17 Primordian Reroll 才满足具体构筑校准要求。
