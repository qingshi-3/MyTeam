# Auto GUI Battler

## 身份、版本与研究深度

- `title_id`: `auto-gui-battler`
- Steam 主 App `993210`，Demo App `3416430`；当前主 App / Demo 都显示开发与发行 `L6161`，Windows-only、英文 / 简体中文、单人 PvE，主 App 为付费正式版。
- Demo 的当前 Steam 日期为 2024-12-28；八篇官方公告发布于 2024-12-30 至 2025-01-09。主 App 于 2025-04-21 正式发行，因此八篇公告都属于发售前 Demo 时期，且没有公告到具体 binary / build 的映射。
- Wayback 中同一 App 的商店展示署名依次出现 `Striga246`、`LI62`、`L6161`。这只能证明同一 App 页面展示名称发生变化，不能据此断言三个名字的人员、组织或账号身份完全相同；当前 support email 仍使用 `striga246` 字样也不能补足身份链。
- Steam 当前公开面有 0 条评测、1 个只有“通关了 / 简单”的讨论且 0 回复、0 个玩家 Guide。可定位的真正目标 gameplay 只有约 129 秒，无法闭合一套可复现构筑。
- disposition：`insufficient-evidence`。本档案不登记 deep source、不生成 evidence，也不增加 negative / reworked 计数。

## 产品与版本地图

| 节点 | 可确认内容 | 不可外推 |
| --- | --- | --- |
| 当前主 App `993210` | 2025-04-21 正式发行；单人 PvE；Windows；当前开发 / 发行显示 `L6161`；主 App直接关联 Demo `3416430` | 当前商店正文不能证明每项规则从何时生效，也不能把署名变化解释为人员 / 团队交接 |
| Demo `3416430` | 当前页面日期 2024-12-28；关联主 App；页面称 Demo 止于第 36 轮 | 页面更新时间与发布 binary 未精确映射；不能假定 2025-02 视频与任一公告完全同 build |
| 2024-12-30 至 2025-01-09 | 八篇主 App 官方公告均早于正式发行，涉及魔法 / 流血、商店、合并、Boss、状态、UI 和缺陷修订 | 不代表正式版当前值、完整规则、玩家验证结果或一个连续可复现 build |
| 2025-02-23 北京时间 | Bilibili `BV16ZA5e3Ey5`，129 秒 Demo / Next Fest 试玩；画面只覆盖首轮战斗与第二轮购买 | 无站内字幕 / 硬字幕；低置信离线 ASR 不作逐句证据；未进入第二场战斗 |
| 当前社区快照 | 0 Reviews；1 个“通关了 / 简单”主题、0 回复；0 玩家 Guide | “简单”没有难度、模式、阵容、物品、回合或失败上下文，不能作为平衡 / 构筑证据 |

## 发现层规则轮廓

以下结构来自当前 Steam 产品正文，只用于界定检索对象，不进入 deep evidence：

- 纯 GUI 的单人 PvE 自动战斗；玩家保留或重抽发到的单位卡，管理单位、合并升级并升级物品。
- 正式版目标为存活 50 个随机回合，每回合 50 秒；Demo 止于第 36 回合。每 5 回合自动保存并发放物品。
- 三个同名同级单位合为更高等级，最高 level 3；当前产品正文明确没有 synergy system、其他地图 / campaign 或 meta-progression。
- 产品量级声称约 90+ 单位、60+ 敌人、约 30 件物品，并有技能、状态、随机敌阵、instant-death / nightmare 难度和 3× / 5×快进。

这些信息能提出 shop、合成、物品、状态、敌阵和无羁绊构筑问题，但商店页本身不能证明真实玩家如何组成、转型或反制一套阵容。

## 八篇发售前官方公告能确认什么

八篇公告全部可读，但它们是增量修订，不是完整规则书，也没有合格实践来源交叉闭合：

- 2024-12-30：提高所有单位与敌人的受击回 Mana，并调整一名提供 range immunity 的 fishman，官方目标是让魔法施法单位更可用。
- 2025-01-02：将卡牌 UI 移到顶部并修订本地化；另一篇修复 floating text 闪烁，并修复 `Necklace of Silence` 意外赋予普通攻击 AOE。
- 2025-01-03：商店开始显示购买 shop EXP 的费用。
- 2025-01-04：修复部分敌人随机获得 mana-draining attacks、明显影响游玩的问题。
- 2025-01-05：缩短并清理攻击音效。
- 2025-01-06：从商店买入后，bench 上满足条件的单位自动合并；简化逻辑只读取 bench，不读取 board。公告同时降低 Boss damage，并把 `UNDEAD LORD` 改为 melee、允许其受到带被动的攻击施加 stun / phase out 等负面状态；此前该 Boss 对这类 physical attacks 免疫。
- 2025-01-09：大幅增强多数 bleed-related skills，官方称 wyverns 应更可用；加入牺牲 level 2 或 3 单位、按 level 10 shop 重抽卡牌的选项；再次收紧卡牌 UI 排列。

公告能证明 Mana、Bleed、range immunity、AOE、shop EXP、bench / board、自动合并、Boss、负面状态、牺牲和高等级商店重抽都曾存在于发售前规则面。它们不能证明正式版保留、具体 holder、数值顺序、最优组合、玩家采用或任何改动实际解决了平衡问题；因此本检查点不把这些修订拆成 lifecycle / negative records。

## 唯一目标 gameplay：首轮购买、布阵与一次状态触发

Bilibili `BV16ZA5e3Ey5` 标题为“[新游]单机 PvE 自走棋游戏-STEAM新品节-试玩-独立-图标型自动战斗棋”，作者阿糕欧糕，发布时间为北京时间 2025-02-23 00:30，时长 129 秒，0 评论、0 弹幕。页面没有站内字幕，视频也没有硬字幕；音轨有连续男性中文解说，但离线 ASR 错字严重、置信度低，不用具体句子补规则。

画面只支持以下低权重实践观察：

- 开局显示 10 Gold、Interest 1、shop level 3 / EXP 0/26、capacity 0/3，发牌 / 刷新费用显示为 3；首轮五张牌都显示为成本 / 层级 1，买入过程可见 Gold 10→9→7→5。
- 玩家首轮买下五张一费单位，再把其中三名拖入容量 3 的战场；`妖精·束缚者` 的面板被动写明“30/40/50% 概率将敌人虚化 3 秒”，第一场战斗的浮字也实际显示“虚化”。
- 第一轮胜利后，第二轮显示 14 Gold / Interest 1；玩家再次买下五张一费单位，Gold 降至 9。
- 视频没有进入第二场战斗；最后打开 1× / 3× / 5×速度设置后结束。

它只能闭合“首轮买下五张一费单位 → 部署其中三名 → 一次被动状态触发 → 首轮结算 → 第二轮再次买下五张一费单位”的局部操作。第二轮五张一费单位没有被部署、合并或投入下一战，首轮五张也只有三名实际参战；这些购买不能命名为某种体系、经济策略或 pivot。视频也没有物品、升级完成、坏对局、反制、替换或最终失败 / 通关路径。按 schema，未达到可登记 `video-transcript` 或 practical build source 的门槛。

## 无法完成的构筑语法

- **engine**：只能看到 `妖精·束缚者` 的一次“虚化”被动触发和三单位首战；没有多个组件互相供给的构筑发动机。
- **state/resource**：画面显示 Gold、Interest、shop level / EXP、capacity、3-cost 发牌 / 刷新与一费卡；不知道利息公式、出售值、牌池、概率、锁牌、跨回合规则或正式版延续。
- **payoff**：面板给出 30/40/50% 概率和 3 秒持续时间，战斗浮字确认“虚化”曾触发；但没有实际目标选择、抗性、伤害交互、冷却、状态叠加或队伍收益数据。
- **survival**：三单位完成首轮，但没有前排职责、治疗、护盾、Armor、闪避、承伤归因或后续压力。
- **spatial condition**：玩家确实把三单位拖入容量 3 的场地；没有合法格、相对站位、距离、射程、body block 或为何这样排列的解释。
- **payoff owner**：只能把一次“虚化”观察归到当时 UI 标识的 `妖精·束缚者`；不能证明它在其他 build / 版本的正式规则。
- **economy / upgrade**：首轮与第二轮各买下五张一费单位，但只展示首轮其中三名上场；未展示合成、卖出、shop EXP、牺牲重抽或物品选择，无法说明这些购买是囤对子、追三星、扩容还是随手试买。
- **pivot / counter**：没有敌情预览后的替换、反制单位、坏 matchup、失败原因或下一战结果。
- **version context**：视频位于 Demo / Next Fest 时期，八篇公告之后、正式版之前；没有 build id，不能与某篇公告或正式版静默合并。

## Facehand 污染排除

多条中文 / 视频搜索结果实际属于独立游戏 `Facehand`，不是 Auto GUI Battler：

- Bilibili `BV1am421V71Y` 标题直接写 `FaceHand` 与增益投掷流；YouTube `rlSDQuLflDc` 和一条抖音内容是同一产品路线。
- `Facehand` 是 Steam App `2605600`，开发 / 发行显示 `foolsroom`，产品规则以 25 轮与协同为核心。
- Auto GUI Battler 是 App `993210` / Demo `3416430`，当前产品正文写 50 轮且明确没有 synergy system。

标题中的 emoji / 表情符号、自走棋、GUI 相似性不能覆盖 App id、开发者、轮数与 synergy 规则冲突。Facehand 的通关阵容、投掷增益、协同或视频解说均不进入本档案。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

- Steam：主 App / Demo 的 Store、App Details、appinfo 与相互关联已核对；主 App正式发行日为 2025-04-21，当前 public build 只作版本路由，不与八篇公告逐项映射。
- News：主 App News API 共八篇，日期为 2024-12-30 至 2025-01-09，全部早于正式发行并逐篇读取。Demo News 无独立公告。
- Community：全语言 Reviews API 为 0；General Discussions 只有 2025-01-14 的“通关了 / 简单”主题且 0 回复；Guides 无玩家条目。该单句不提供 build grammar 字段。
- 署名：当前 App Details / appinfo 显示 `L6161`；Wayback 的同 App 快照依次显示 `Striga246`、`LI62`、`L6161`。只记录展示署名变化，不合并为一个可证明身份。
- 视频：真正目标 `BV16ZA5e3Ey5` 的 API、页面、音轨与画面已核对；无站内 / 硬字幕，低置信 ASR 不作逐句证据。129 秒只覆盖首轮与第二轮购买，作为 partial practical observation 留在 route audit，不登记 deep source。
- 污染：`BV1am421V71Y`、YouTube `rlSDQuLflDc` 与抖音同内容均按 `Facehand` / App `2605600` / `foolsroom` 排除，未借用其 25 轮、synergy 或完整阵容。
- 外部精确标题、App、Demo、署名、单位、build、guide、strategy、gameplay 与视频路线没有找到 wiki、文字攻略、可复现玩家构筑、统计或完整字幕实战。继续结果只重复商店正文、八篇官方增量公告、极短试玩、单句讨论或 Facehand 污染，不再提高 build closure，故停止。

## 对本项目的研究价值与限制

可以保留为后续问题、但不能由本游戏单独证明为设计结论：

- “无 trait / synergy”不等于没有构筑深度；若深度由单位技能、状态、物品、shop 与敌阵承担，仍需真实构筑验证，而不能只凭产品承诺。
- bench-only 自动合并说明 board / bench 是不同状态容器，合并 reader 必须明确读取范围；牺牲高等级单位换高等级商店重抽则会把已投入升级转成搜索资源。
- 50 秒战斗、每 5 轮 checkpoint / item 与 3× / 5×快进提出节奏和可读性问题，但公开实践没有证明这些设置的实际质量。

不能作为本项目方案依据：

- 不复制任何单位、Boss、物品、状态、回合数、容量、价格、商店等级、利息、合成或重抽数值。
- 不把首轮五张一费单位中的三名上场者与第二轮五张一费单位拼成完整阵容，不从一次“虚化”触发推导控制体系、元素体系或 meta。
- 不把发售前 bug / buff 公告写成正式版 lifecycle 结论，也不从 `no synergy system` 直接证明项目应该移除 trait。
- 不借用 Facehand 的 25 轮、协同、投掷流或通关阵容。

## 未决问题

- 正式版当前单位、物品、技能、状态与敌人数据库；发售前公告到正式 build 的保留 / 改名 / 删除映射。
- Gold、Interest、shop level / EXP、发牌 / 刷新、容量、bench、board、出售、锁牌与牌池概率的完整规则。
- 三同名合并、自动 bench 合并、牺牲单位重抽与物品升级的精确选择顺序和机会成本。
- `妖精·束缚者` 在正式版中的文本、target、具体“虚化”机制、抗性、owner 和适用版本。
- 至少一套具名、版本化、可复现的单位 / 物品 / 布阵构筑，以及其经济、升级、转型、反制和失败样本。
- `Striga246`、`LI62`、`L6161` 展示署名变化的官方解释；八篇公告对应的实际 Demo builds。

## Disposition

`insufficient-evidence`

这是已正式发行、具有独立 Demo、可读产品规则和八篇官方增量公告的可识别长尾游戏，不是 `discovery-only`。但实践面只有一段 129 秒、无平台字幕的局部试玩和一句“通关了 / 简单”；它们最多证明一次首轮购买 / 布阵 / “虚化”触发 / 结算与第二轮囤牌，不能闭合 engine、完整 state、payoff、survival、space、owner、economy、pivot / counter 与版本连续性。故不登记 deep source、不生成 evidence、不增加 negative / reworked 计数，避免以官方资料数量或另一款 Facehand 的完整视频代替真实 Auto GUI Battler 构筑。
