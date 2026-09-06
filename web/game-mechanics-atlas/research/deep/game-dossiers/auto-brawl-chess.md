# Auto Brawl Chess

## 身份、时期与研究深度

- `title_id`: `auto-brawl-chess`
- Steam App `951480`，开发 / 发行均为 Panoramik Games LTD，Windows 正式版于 2023-04-07 发布；同名移动版在更早时期已运营，iOS App id 为 `1495162677`。Steam 商店只用于身份，不进入 deep source。
- 可核验机制横跨 2020–2024：两篇 2020–2022 文字攻略、2021–2024 实战视频、九篇 2022-origin Help Center 正文、2022 iOS 评测、2023 社区讨论、2024 Seasonal PvP 补丁和一篇 2024 Steam 评测。不同平台与时期不得静默拼成当前规则。
- 九篇 Help Center 页面当前套着 2026-05-07 Zendesk 时间，但逐字历史比对只支持 `2022-origin / republished-or-migrated-2026 / historical`。这个标签适用于全部九页；2026 wrapper 绝不代表 2026 现行规则。
- 深度：28 个 substantive source，15 条 evidence，7 个去重 negative/reworked 案例。资料能闭合 Mage、Elemental、Guardian/Demon 与 Guardian/carry 四个历史实践结构，并覆盖局内经济、双轴羁绊、召唤空间、装备 / Artifact / Magic Item / Warlord 所有权和账户成长污染。
- disposition：`retained`，不是 anchor。完整构筑主要来自 2021–2022 移动版，2024 后没有完整 current meta、现行单位 / 装备 / 羁绊数据库或代表性统计。

## 来源包

| 功能 | 数量 | 来源 |
| --- | ---: | --- |
| 2022-origin 历史规则 | 9 | Gameplay、Factions and Classes、Warlords、Equipment、Armor/Magic Resistance、Journey、Dungeon、Time Rift、Chess Pass |
| 官方版本节点 | 1 | 2024-08-08 Seasonal PvP / Autofight 补丁 |
| 文字攻略 | 2 | Gamer Empire 历史综合攻略、Pocket Gamer 早期策略 |
| 视频实践 | 12 | Mage 2、Elemental 2、Demon 1、Artifact 1、2024 新手 1、局外指南 1、Guardian 1、Faction/Class 分解 2、六局 Ranked 日志 1 |
| 社区失败面 | 4 | 装备卸载建议、Journey 2-35、iOS v22–23 评测页、2024 Steam 匹配评测 |

全部 28 个 source 都由本档案 15 条 evidence 覆盖。视频源要么有可读字幕，要么像 `vitT8CLUD70` 一样只登记人工时间戳与画面可见动作；不从无字幕素材推断作者意图。

## 版本与模式边界

| 时期 | 可核验结构 | 禁止外推 |
| --- | --- | --- |
| 2020–2021 移动版 | 早期经济 / 站位建议；Mage 连败搜牌、Elemental 门槛、Demon 吸血、Guardian 防线与 carry 分工 | 不作为 2023 Steam、2024 Seasonal 或 2026 客户端当前数值 / meta |
| 2022 移动版 | Faction / Class 双轴；局外英雄 / 装备 / Warlord；Journey、Dungeon、Time Rift、Chess Pass；多条换人和六局 Ranked 记录 | Zendesk 2026 wrapper 不刷新规则日期；Plant/Elemental 不映射 Primal/Darkspawn |
| 2023 Steam / 社区 | Steam 正式发行；装备转型摩擦与 Journey 2-35 个案 | 不把账户不可迁移等当年状态写成当前跨平台结论 |
| 2024 | Seasonal PvP 每十四天六阵营池；普通 Ranked 保留；PvP Autofight 与免费 PvE Autofight；新手空间观察；账户战力匹配抱怨 | 最后可确认的新玩法节点不等于当前 meta，也不证明池治理已解决问题 |
| 2026 | Google Play listing/update metadata 显示 `versionName 39.0.7` 与 `Updated on 2026-02-26`；Zendesk 迁移外壳可见 | listing 不含 `versionCode`、binary artifact 或 hash；不证明精确 binary、2026 新机制、当前英雄表、数值、匹配规则或活跃 meta |

历史资料对最大星级、Armor/MR、Plant、Beast、Reptile、Undead、Soaring、Shaman、Shooter、Priest、Mage 与 Demon 数值存在漂移。它们只在对应来源时期出现，不裁决统一当前答案。

## 核心循环与真实决策

历史 Gameplay 规则把对局分为准备与自动战斗阶段。准备期的金币同时服务于购买、刷新、锁店、扩人口和合成；玩家还要观察对手、调整阵型并判断是否保留过渡英雄。Pocket Gamer、2024 新手视频和六局 Ranked 日志共同证明这些动作被实际使用，但利息、牌池、人口和合成最高星数不能当前化。

Faction 与 Class 是正交双轴。同一英雄能够同时满足两个门槛，也能在换人时作为桥；例如 2022 Class Breakdown 显示 Mage 常以 Marilyn、Zeus、Nero、Imar 为终局方向，缺 Zeus 时先用 Sadako，出现后再替换。Plant/Elemental 与后续名称没有证据支持一一改名。

局内构筑还受到局外账户状态影响。英雄等级、永久装备和 Warlord 提供对局前战力；装备需要 holder，Forge 又读取装备材料 / 强化状态。由此，同样的阵容标签并不等于同样的实际战力，局内搜牌失败、账户培养不足和匹配对手过强必须分别报告。

## 历史构筑一：Mage 连败、Round 8 roll-down

- **engine**：前七轮少买或空场承受失败，积累资源后在 Round 8 扩人口并集中搜索 Mage。
- **state/resource**：金币、早期生命损失、人口、商店刷新、Mage 门槛、Imar / Minerva 生存件与 Zeus / Nero / Thanatos 输出件。
- **trigger**：达到预定扩人口 / 搜牌窗口，或生命值已低到不能继续贪经济。
- **payoff**：Mage 充能与技能循环让 Zeus、Nero、Thanatos 等明确伤害 owner 获得施法窗口。
- **survival**：Imar / Minerva 提供站场时间；它们不是伤害转换器。
- **spatial condition**：生存件保护后排技能 owner；六局 Ranked 只证明玩家实际从低人口转至 9/9，不证明统一最佳站位。
- **payoff owner**：具体 Mage 输出英雄拥有伤害，前排只拥有生存窗口。
- **economy / pivot**：历史视频中的一局在 10 HP 才启动且没找到 Imar / Zeus，必须把贪经济、roll 随机和关键件缺失分开。另一视频含两次 Insane 尝试，不能把单次成功或失败写成通用最优。
- **counter / failure**：快速压血、抢占所需单位、Silence 关键 caster 或迫使提前花钱都能破坏路线。该失败计为一个 bounded negative case。
- **version context**：只适用于 2021–2022 历史移动版；精确回合、数值与英雄分类不进入当前处方。

## 历史构筑二：Elemental 门槛与宽羁绊转型

- **engine**：三 Elemental 门槛召唤 Golem，Drifter / Mage / Priest 等横向标签承担过渡与补强。
- **state/resource**：Elemental 计数、可用 hero、召唤空格、横向标签、金币 / 刷新和未完成的 2/3 或 1/3 门槛。
- **trigger**：部署第三个合格 Elemental，并为召唤物保留合法空位。
- **payoff**：Golem 是 summon owner；其他英雄保留自己的攻击、治疗或技能输出。
- **survival / space**：召唤者及其身后空格需要被保留。2024 新手视频只支持一个具体的“身后两格”观察，不能推广到所有召唤物 / 方向。
- **pivot**：一条实际路线长期停在 2/3，随后退至 1/3 并转 Demon / Undead / Mage，最终第二；这是门槛断裂后的转型样本，不是成功 Elemental 闭环。
- **counter / failure**：占满棋盘、搜不到第三个组件或在等待门槛时掉血，会使召唤收益无法兑现。第二名不能证明 Elemental 弱，也不能冒充三 Elemental 成功。
- **version context**：Drifter + Elemental 线来自 2022；另一门槛失败来自 2021。`JB1jeMairy4` 最终是 Shaman / Beast 宽羁绊且第二，明确排除为 Elemental 构筑。

Elemental 是召唤轴，不是 Shield、Armor 或元素反应系统。

## 历史构筑三：Guardian + Demon 吸血壳

- **engine**：Guardian 计数 / 协同向受影响友军提供 Armor；实践中 Guardian 单位站前排，历史四 Demon 门槛再把分布式攻击转为 55% lifesteal 续航。
- **state/resource**：Guardian / Demon 计数、受影响友军的 Armor / mitigation、前后排、攻击 owner 的伤害、吸血恢复和 Mary 的独立治疗。
- **trigger**：满足四 Demon 历史门槛并由各攻击者实际造成伤害。
- **payoff**：攻击者拥有伤害与由此产生的吸血；Guardian 协同只供应 Armor，受影响的前排 recipient 以额外站场时间延长攻击窗口。
- **survival / space**：Guardian 单位在该实践中占据前排，受影响友军持有收到的 Armor / mitigation，Demon 输出位保持攻击接触。Guardian 协同是 Armor supplier，Mary 治疗与 Demon 吸血另有 owner。
- **pivot / counter**：如果 Demon 门槛或合格输出不成形，可回退到 Guardian + 独立 carry；爆发、控制、Silence 或绕后会缩短吸血启动窗口。
- **version context**：55% 只属于 2021 视频快照，不写成当前值。

## 历史构筑四：四 Guardian 与 Maximus / Hanzo 输出分工

- **engine**：四 Guardian 条件向受影响友军提供 Armor；Guardian 单位在该局站前排，Maximus 后置后借 Slayer 行为切入后排，Hanzo 作为独立强单卡输出，Shaman 再放大技能侧。
- **state/resource**：四 Guardian 门槛、Armor supplier 与受影响 recipient、前排占位、Maximus / Hanzo 存活和攻击窗口、Shaman 增幅、敌方闪避 / 治疗 / 双坦克。
- **payoff**：Maximus 与 Hanzo 是输出 owner；Guardian 不读取 Armor 产伤。
- **survival / space**：Guardian 单位在该局承担正面接触，受影响友军持有收到的 Armor / mitigation；Maximus 的后置位置服务其切后排路径。Jaxy 只是条件桥接 / 计划，不写成已经完成的核心。
- **pivot / counter**：敌方闪避、治疗和双坦克造成平局压力；最终第一只证明该次路线闭合，不证明总体胜率。
- **version context**：2021 Guardian Defense 与同时期 Class / Armor 资料共同约束；不外推当前单位数值。

## Artifact、Magic Item、Equipment 与 Warlord

- Dungeon Artifact 是胜后团队 / 阵营三选一且可叠加的 run 规则层。
- Time Rift Magic Item 绑定指定英雄、增加主动或被动能力，可在 Heroes 页查看并由 Runes 强化；owner 是该英雄。该模式还明确出现 Memory Shards、Enchanted Ore、Runes 与 Gems，但不能据此断言每种资源都是玩家瓶颈。
- Equipment 是 holder-bound 物品，存在类别、稀有度与强化；官方 Equipment 页把 Magic items 列为 Equipment 类型，因此 Magic Item 可能是特殊 Equipment 子类。现有资料没有解析其与普通 Equipment 的 inventory、slot 与 persistence 关系。
- Warlord 是局外全队加成层，不是棋盘单位。
- `8I5nfPxhhek` 只显示最终阵容和 `Yin and Yang`、`Sorcerer’s Ring`、`Ice Armor` 三件候选 Artifact；不能声称夺冠、最终选择或把 `Ice Armor` 提升为元素盾体系。

Dungeon Artifact 与英雄绑定的 Magic Item 可以区分，Warlord 也明确是另一账户层；但 Magic Item 与 Equipment 不是已证实完全互斥的两层。对本项目的启发是明确 owner、获取阶段、taxonomy、inventory、slot、persistence、规则改写和替换成本，而不是先假定共享或分离。

## 英雄池、Seasonal 子池与转型成本

2022 iOS 固定评测描述商店出现 `locked`、未拥有或低属性英雄，并建议提供 deck filter；这些评测不支持“英雄池持续扩张”的说法。2024 官方补丁自己指出英雄过多、商店过于多样使三星更难，并让 Seasonal PvP 每期只显示六阵营、每十四天轮换，同时保留普通全阵营 Ranked。`shown`、`locked`、`owned`、`developed` 与 `usable` 必须分开。

这是一条“池污染 → 子池治理”的单一 lifecycle / negative case，不拆成投诉和补丁两个案例；也不声称官方由某条评论触发改造，或改造已提高具体追星率。

装备转型有独立摩擦：一名玩家在 2023 建议 `unequip all`，因为换阵容或进入 Forge 前需要逐件卸装。官方 Equipment 规则和 2022 局外视频证明装备 holder / Forge 层存在，但只有一帖证明这项操作抱怨，不能声称普遍性或已修复。

## Journey、Dungeon 与 Autofight

Journey 以限定可用 Faction、可预览固定 Boss 阵容、连续三胜和失败重试构成关卡式反制题。Journey 2-35 讨论只证明一关：玩家描述三秒无敌、隐身、Hellsing 输出与 Rat / Conrad / Norris 尝试；固定敌方开局、随机玩家池和账户成长的不对称不能推广为整个模式平衡。

Dungeon 从四 Faction 池组队，以生命石承受失败，并在胜利后三选一可叠 Artifact。Time Rift 的英雄 Magic Item 与 Dungeon Artifact 可以区分，但它又可能是 Equipment 的特殊子类；现有资料不足以裁定与普通 Equipment 共用还是分离 inventory / slot / persistence。

Autofight 是明确 lifecycle：2022 Chess Pass 页面将其限定为 Premium / PvE，2024 官方补丁给 PvP 增加 Autofight，并让 PvE Autofight 免费。两个时期互相替换，不可写成同一时点规则；自动功能也不证明自动布阵、额外承伤或改变空间条件。

## 七个去重 negative / reworked 案例

1. Mage 高风险 roll-down：低至 10 HP 才启动且缺 Imar / Zeus 后淘汰。
2. Elemental 门槛长期 2/3、后退 1/3 并转型；最终第二不是 Elemental 成功。
3. 英雄池污染与 Seasonal 六阵营子池治理合计一个生命周期案例。
4. 英雄等级、装备、Warlord 等账户战力污染匹配；单条 Steam 评测不证明总体 P2W 或算法。
5. 换阵容 / Forge 前逐件卸装造成装备转型摩擦。
6. Journey 2-35 固定敌方开局与玩家随机池 / 账户成长不对称，只限该关卡。
7. 固定 iOS 评测将 Power Essence、Elixir 与 coin 描述为养成瓶颈；官方 Time Rift 规则另证明 Memory Shards、Enchanted Ore、Runes、Gems 等资源多样性。两者合计只算一个聚合案例，但不把 Equipment / Forge、Gems 或每一种列出的货币都写成已被玩家证明的瓶颈。

Autofight 的版本变化只进入 lifecycle evidence，不加第八个 negative；Guardian 遭遇闪避 / 治疗 / 双坦克只作为 counter。

## Shield、Armor、元素与输出边界

历史资料将供应与持有拆开：Reptile 门槛向对应 Reptile recipient 提供 Shield，Guardian 计数 / 协同向受影响友军提供 Armor；这些 recipient 持有 Shield 或 Armor / mitigation。Elemental Golem 是 summon owner，Maximus / Hanzo 是 damage owner。Armor/MR Help 页提供的公式只属于历史规则；Reptile 数值也存在来源漂移。防御在可核验构筑里只延长输出窗口，没有自动转换为伤害。

没有证据支持 Ice Shield、Earth Shield、元素反应、最大生命值伤害、Defense / HP 转法强或全队生命 / 防御转单核 Attack。若本项目需要这些路线，必须由装备、遗物或英雄能力显式声明输入、recipient、snapshot、cap、self-feedback guard、刷新时机和槽位成本，不能归因于 Auto Brawl Chess。

## 检索日志与停止理由

访问日期统一为 2026-09-04。九篇 Help 页已与旧 article id / Wayback 文本比对；Gameplay、Factions / Classes、Warlord、Equipment、Armor/MR、Journey、Dungeon、Time Rift、Chess Pass 都保留历史迁移标签。另六篇已读 Help 页面 Hero Attributes、Ranked、Currency、Battles with clones、Equipment and Hero Summon、Forge 因重复而只进 route audit。

两篇文字攻略和十二个视频的正文 / 字幕 / 时间戳均按功能拆分。`vitT8CLUD70` 无字幕，只登记六局可见名次 `6/2/4/4/4/1` 与第二局早期低人口、资源积累、后续 9/9 / 第二名；不推断作者意图或最优性。`WozJkkrWf2M` 是一周新手，只作规则旁证。Gamer Empire、Pocket Gamer、Zathong 重复内容不冒充多方共识，Zathong 不登记。

社区路线固定到两帖、iOS page 5 的五个 review id/version 对和 Steam recommendation `176383868`。原 RSS 捕获保存版本与 `-07:00` 日期；当前登记 RSS URL 虽返回 HTTP 200，但 feed 已空。Apple 当前 reviews API 以 `platform=iphone&limit=10&l=en-US&sort=recent` 和 `offset=200/210/220` 仍可复核 5/5 正文、id 与 UTC 日期，却不返回 version，因此不能依赖会话缓存或用 recovery route 替代原始版本证据。泛化商业化抱怨不重复计数。2024 后的检索收敛为旧视频、重复攻略、商店信息和宽泛抱怨，继续增加来源不再改变 owner、counter、economy、version 或 failure 结论。

## 可迁移与不可迁移

可迁移：

- Faction / Class 双轴桥接，但必须让替换前后损失的门槛与 owner 可见。
- 召唤门槛同时读取 tag 和可用空间；显示 summon owner、保留格与失败原因。
- 将局外英雄等级、装备 / Forge、Warlord 与局内金币 / 商店分账，避免把账户战力误报为阵容质量。
- 区分 Dungeon Artifact、英雄绑定 Magic Item 与 Warlord；Magic Item 可能属于 Equipment 子类，须把尚未解析的 inventory / slot / persistence 关系明确标为未知。
- 对池污染使用显式资格过滤 / 周期子池，同时保留玩家选择完整池的模式边界。
- 报告失败时区分贪经济、搜牌随机、门槛断裂、站位、关键 owner 暴露、账户战力和关卡脚本。

不可迁移：

- 不复制历史英雄、Faction / Class 名称、协同阈值、Armor/MR 公式、55% Demon 吸血、能量值或账户成长数值。
- 不把移动版、Steam、Seasonal 与 2026 wrapper 合并为同一版本。
- 不把 Elemental 当 Shield，不把候选 Ice Armor 当已选 Artifact 或元素盾。
- 不把单个 P2W / Journey / 装备抱怨提升为总体统计、开发者承认或修复因果。
- 不把项目自创的盾、元素反应、最大生命伤害或防御转伤归因于原作。

## 未决问题

- 2024-08-08 之后的当前英雄、Faction / Class、装备、Warlord、Artifact 与 Magic Item 数据库。
- Magic Item 与普通 Equipment 是否共享 inventory / slot、是否同样永久，以及 Rune 强化的当前精确资源与重置规则。
- Steam、iOS、Android 当前是否同规则、同池、同账户与同匹配体系。
- 当前 shop、interest、人口、牌池、锁定、合成星级、出售与 Seasonal 六阵营概率规则。
- Reptile Shield 与 Guardian Armor 的当前 supplier / recipient、Armor/MR、summon vacancy 和 target / movement 的精确顺序。
- Journey 2-35 与其他关卡的可重复 counter、账户门槛和失败分布。
- Seasonal 子池是否改善搜牌 / 追星、Autofight 后续状态，以及 2024 后的实际高端 meta。

## 最终 disposition

`retained`

28 个来源跨规则、补丁、攻略、实战与失败讨论，能闭合历史 Mage、Elemental、Guardian/Demon 和 Guardian/carry 结构，并说明双轴羁绊、召唤空间、局内 / 局外经济、装备转型、模式 Artifact / Magic Item、账户战力与池治理。但完整构筑主要来自 2021–2022 移动版，Help Center 只是 2022-origin 历史正文的 2026 迁移，2024 后又缺少现行数据库、完整高端构筑与统计；因此保留为历史长尾机制样本，不升 anchor，不作为当前数值 / meta 权威。
