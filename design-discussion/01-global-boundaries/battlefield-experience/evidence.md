# G01 战场体验：已有证据

检索日期：2026-09-06。用途：讨论依据；不是现行权威、用户决定或实现验收。方案比较见 [proposals.md](proposals.md)。下文按子问题保留检索范围；Q01 为体验主轴，Q02 为战线关联与支援代价。

## G01-Q01：体验主轴检索

**地图应让玩家在兵力集中、分路应对与接敌先后之间做什么取舍？** 本轮比较这些选择的体验价值、成立条件与代价；不决定地图形状、坐标、战线数量、胜负目标、英雄技能或指令规格。

这里的“战线”暂指一场战斗内相对独立的接敌关系，不预设固定道路、多层、分屏或必须各放一支完整队伍。“整局路线分支”“并行地区经营”“背包格子”分别核查，不能因为同样出现空间或多线词汇就归成战场机制。

## 实际检索范围与方法

已按 [恢复流程](../../README.md) 读取 roadmap、G01 README、记录约定、跨议题问题，以及有关地图、人口、主动干预的现行玩法正文；另读 G02、G03、M02、R12 入口核对归属。

本轮对整个相关本地语料先检索，再选择正文切片；没有重新联网核验外站，没有续做深研任务的 Checkpoint 57，没有运行游戏或原型。

| 语料 | 实际处理 | 证据边界 |
| --- | --- | --- |
| [研究路由](../../../web/game-mechanics-atlas/research/README.md)、deep/schema.md | 读取分层与证据门槛 | 原库只读；本文不修改游戏的入库判定 |
| [候选清单](../../../web/game-mechanics-atlas/research/deep/candidate-roster.json) | 核对全部 66 项与实际 dossier 文件 | 56 份档案存在；14 anchor-retained、28 retained、8 discovery-only、6 insufficient-evidence；另有 10 pending 尚无档案 |
| [深证据](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) | 对全部 780 条记录检索 domain、mechanism 及记录全文；扩展到规则、实战、失败与空间字段 | 关键词命中不等于已读懂或适用；重点采用的切片及 id 见下文 |
| [逐游戏档案目录](../../../web/game-mechanics-atlas/research/deep/game-dossiers) | 全部现存 56 份正文参与全文检索；回读重点机制段、版本段与负面案例；读取 14 份未达深证据门槛档案的判定段 | 未逐字重读每份无关构筑；也不声称绝对无遗漏 |
| [深来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)、coverage-report.md、deep/synthesis.md | 核对证据来源路由和库的阶段状态 | deep/synthesis.md 仍是暂缓综合的占位文件，不能当作已完成跨游戏综合 |
| [discovery/source-index.md](../../../web/game-mechanics-atlas/research/discovery/source-index.md)、mechanic-evidence.json、synthesis.md | 全库检索候选和相关机制线索；核对 65 游戏、70 来源、106 条发现记录的分层 | 商店页及发现层建议不作为机制成立或优劣证据 |
| [旧机制图谱](../../../web/game-mechanics-atlas/app/data/atlas.ts)、[整局概念](../../../web/game-mechanics-atlas/app/data/runProgression.ts)及对应归档任务 | 检索并阅读战场空间、特殊地图、multi-front-defense 等命中段 | 属于历史设计素材；不是外部调研事实、用户当前决定或浏览器草案确认 |
| 仓库内其余 Markdown | 按地图、战线、护送、守点、接敌及英文同义词寻找遗漏入口；区分设计来源与工程记录 | 导航、碰撞、部署和战报实施记录不作为“玩法已获确认”的证据 |

检索词分两轮：

- 第一轮：`地图 / 战场 / 多战线 / 战线 / 地形 / 通路 / 侧翼 / 阵型 / 站位 / 空间`，以及 `battlefield / terrain / flank / formation / positioning / lane`。
- 关联扩展：`分兵 / 接敌 / 最近 / 最远 / 邻接 / 视线 / 射线 / 寻路 / 掩体 / 漏怪 / 守点 / 护送 / 夹击`，以及 `choke / body block / line of sight / pathing / pathfinding / target lock / nearest / furthest / farthest / adjacency / floor choice / leak / cover / escort / objective / split tank`。
- 进一步沿命中机制核对：`Chaser / fountain / Mythic / Hidden / trap / Taunt / The Hands / Corruptors / Vortex / Pyre / overstack / Rally`，并检查 AOE、召唤空位、移动方向、自动目标失控与版本重做。
- 检索中排除同词异义：版本“地图”、工厂物流 lane、背包 adjacency、属性面板位置、整局路线、来源文本中的普通 cover 等。没有把命中数量作为完成率。

## 当前基线与本轮不能偷换的前提

来自 [核心规则](../../../gameplay-design/tower-autobattler-core.md) 与 [构筑框架](../../../gameplay-design/combat-build-framework.md)：

- 地图与多战线已是重点方向，地形、通路、接敌应使英雄分配、射程、机动、控制与支援有意义；多层、守点、护送、目标物及分批接敌尚未选定。
- 游戏是单人爬塔、英雄名册、实时自动战斗与有限独立战术介入。英雄法力技能自动释放；现行两指令／三战术点仍有效。
- 普通终局人口目标 10、明确投入后可到 18 的契约继续有效。当前 10×6、18 候选部署位是实现基线，不能用 G01 讨论静默改容量。
- 当前战斗是连续空间、实体占位、合法射程与通路；全体存活单位从首 tick 参与自动行为。本文所说“拖延／牵制”不默认增加守线待机或手动画路线权限。
- 不重新表决核心／功能位分工；“某战线要有独立承压能力”也不等于给全员新增可指定职责系统。

## 七类做法及真正的差异

下表“原作做法”是已有材料支持的描述；“迁移与代价”是本轮设计分析，尚未验证于本项目。

| 做法 | 原作中的玩家选择与得失 | 对本项目有用的部分 | 不能直接照搬的前提／代价 |
| --- | --- | --- | --- |
| **1. 以站位分配威胁** | TFT、Auto Chess、Guildrun 根据跳后排、最远目标、AOE 等调整侧别、前后和诱饵；The Last Flame 的 The Hands 要分配两侧承压者与远距离承伤者 | 同一队伍换位即可改变谁承伤、谁有输出时间；功能英雄不只是凑数 | 这是对位深度，不自动证明地图有多战线；若每个敌包只剩固定摆法，会成为背答案 |
| **2. 用阻挡和射线组织火力** | Gladiator Guild Manager 用前排／图腾挡弹道，为射手寻找无遮挡角度；优先目标不保证实际命中该目标 | 正面保护、侧向射击、穿透与范围攻击能有不同几何价值 | 该游戏存在弹道例外；不能从攻略推定完整碰撞规则。若自动移动违背可见路线，玩家难以判断是部署错误还是 AI 问题 |
| **3. 集中换协同，分散换安全与覆盖** | Astronarch 的邻接治疗／承伤有范围；Tales & Tactics 的 Mythic 可组成互相独占增益的小组；Hadean Tactics 围绕陷阱区域布置；集中也会受到 AOE 和空位压力 | 支援范围让分兵有真实机会成本；核心组合可以形成局部阵地 | 不应自动要求每路复制坦克＋治疗＋输出；广域／全局支援与局部支援的比例未定。过强集中收益会使多路形同虚设 |
| **4. 侧翼用即时兵力换目标与时间** | Mechabellum 延迟侧翼可牵走敌方目标、威胁塔；防守者可在外侧用较少单位拖住；正面投入因而减少 | 路程、首个目标和先后抵达能改变主战线，不必把深度全放在手动操作上 | 它有 PvP 对手承诺、塔 debuff、分轮补兵和特别出生规则；本项目不继承这些。侧翼若无可防范路径，会变成免费跳过前线 |
| **5. 纵深用早接敌换后续压力下降** | Monster Train 底层先处理敌人，顶层多准备时间；漏怪逐层上行，Pyre 承担跨战损失；集中同层能吃增益，却可能封死功能单位入场 | 空间可以改变发动时间、威胁到达顺序与后备余量 | 这是串行拦截层，不等于同时分兵；依赖回合、抽牌、楼层容量、漏怪及 Pyre。多层本身不能保证分散更有价值 |
| **6. 局部对象改变值得争取的位置** | Tales & Tactics 的 fountain 格可给双方资源、覆盖地面效果；Hadean 的 trap 依赖区域与落点；Survivor Mercs 的招募／补给／撤离目标驱动移动 | 让“去哪里”有可见理由；对象可以影响火力、支援或风险 | 双方受益格、伤害区域、战斗胜负目标、整局奖励节点是四种不同对象。现有材料不足以替本项目选定守点／护送及失败代价 |
| **7. 持续移动与旋转制造实时选择** | Just King 移动／旋转整个十字队伍，改变接触、射程和环境重叠；Survivor Mercs 围绕地图目标移动 | 证明移动方向与攻击模式必须服务同一个玩家意图 | 乐趣依赖持续操控。不能把它当作有限指令自动战斗的同成本方案；Skull Horde 的自动散开／扎堆投诉提示执行责任不能悬空 |

### 主要证据入口

`ev-*` 均指向深证据 JSON 的原记录，`src-*` 均可在深来源索引查到原 URL、来源类型、日期与限制。下面保留足以继续核查的主链；同一 Wiki 或作者不计作多个独立观点。

| 来源正文 | 本轮使用的证据 id | 主要原来源与版本限制 |
| --- | --- | --- |
| [Mechabellum](../../../web/game-mechanics-atlas/research/deep/game-dossiers/mechabellum.md)：目标链、两套防守军阵、侧翼、Vortex | `ev-mecha-001-target-lock-chain`；`ev-mecha-002-flank-spawn-retarget`；`ev-mecha-007-chaff-clear-payoff-order`；`ev-mecha-009-vortex-link-shield-rework` | `src-mecha-wiki-targeting-2026`、`src-mecha-wiki-flanks-2026`、`src-mecha-wiki-sledge-marksman-2026`、`src-mecha-wiki-arclight-hacker-2026`；Vortex 对照 `src-mecha-official-1-10`／`src-mecha-official-1-11`。主要是 1.11 时期实践，不外推 2.0 测试服 |
| [Monster Train](../../../web/game-mechanics-atlas/research/deep/game-dossiers/monster-train.md)：标准循环、各层、容量与敌人包 | `ev-monster-train-004-three-floor-battle-order`；`ev-monster-train-010-overstack-capacity-trade`；`ev-monster-train-019-floor-choice-is-contextual`；`ev-monster-train-021-last-divinity-three-floor-exam` | `src-mt-wiki-battle`、`src-mt-wiki-pyre`、`src-mt-wiki-floor-overstacking`、`src-mt-discussion-floors`、`src-mt-guide-useful-tactics`。一代 2.x／TLD 与跨时期实践；顶／底层建议按上下文分歧，不选一个当通用最优 |
| [The Last Flame](../../../web/game-mechanics-atlas/research/deep/game-dossiers/the-last-flame.md)：Boss、空间与失败解释 | `ev-tlf-009-act4-boss-counter-package` | `src-tlf-steam-indepth-2026`、`src-tlf-gameplay-levels-2025`、`src-tlf-official-1-0`。初始 1.0 战术；详细分工主要出自单一专家攻略，不能解释为普遍胜率 |
| [Tales & Tactics](../../../web/game-mechanics-atlas/research/deep/game-dossiers/tales-and-tactics.md)：Mythic、fountain、Chaser | `ev-tnt-008-mythic-adjacency-islands`；`ev-tnt-010-fixed-pve-targeting-and-objects` | `src-tnt-official-levelup-2024`、`src-tnt-official-1-2`、`src-tnt-steam-traits-1-4-3`、`src-tnt-steam-challenge-climb-2024`。1.0.61 观察、1.2 嘲讽与 1.4.3 指南分开；不冒充当前 Set 2 完整规则 |
| [Gladiator Guild Manager](../../../web/game-mechanics-atlas/research/deep/game-dossiers/gladiator-guild-manager.md)：弹道、目标优先、聚怪组合及削弱 | `ev-ggm-009-line-of-sight-bodyblock-formation`；`ev-ggm-013-taunt-aoe-shaman-balance-chain` | `src-ggm-steam-campaign-v1`、`src-ggm-steam-achievements-guide`、`src-ggm-steam-priority-thread`、`src-ggm-official-1-034`。1.0 实践与 1.034 更新；拦截存在例外，缺完整碰撞规范 |
| [Guildrun](../../../web/game-mechanics-atlas/research/deep/game-dossiers/guildrun.md)：目标选择、反向站位与特殊敌人 | `ev-guildrun-005-targeting-reposition-counterpack` | `src-guildrun-wiki-targeting-0-5-6`、`src-guildrun-discussion-positioning-2026-08-29`、`src-guildrun-patch-0-5-6`、`src-guildrun-patch-0-5-7`。Demo 0.5.6–0.5.7；提取的目标公式不是官方面向玩家的完整契约 |
| [Hadean Tactics](../../../web/game-mechanics-atlas/research/deep/game-dossiers/hadean-tactics.md)：区域陷阱、Hidden、位移和召唤空位 | `ev-hdt-003-ea-nightblade-three-by-three-traps`；`ev-hdt-005-summon-multicast-board-saturation`；`ev-hdt-009-target-tile-hidden-spatial-cards` | `src-hdt-steam-favorite-builds-2021`／`src-hdt-steam-favorite-builds-2024`、`src-hdt-official-1-1`、`src-hdt-official-2-0`、`src-hdt-steam-autobattler-pause-2022`。历史 EA 组合与 2.0 规则不能拼成同一套当前卡组 |
| [Private Military Manager](../../../web/game-mechanics-atlas/research/deep/game-dossiers/private-military-manager.md)：房间政策与掩体失败 | `ev-pmm-005-section-target-policy-graph`；`ev-pmm-017-cover-and-position-ai-failure` | `src-pmm-thread-timer-mission-counter`、`src-pmm-thread-long-feedback`、`src-pmm-official-hotfix-2-2025-04-30`、`src-pmm-review-en-cover-ai-2025-05-11`。2025 Demo／EA 社区战术；路径显示修复有官方记录，掩体／编号仇恨仅低置信个案 |

### 同构做法与长尾差异

这些记录用于交叉比较或反例，不因不是主例而省略。

| 游戏／原档案 id | 相关证据 id | 本轮保留的差异与限制 |
| --- | --- | --- |
| Teamfight Tactics／`teamfight-tactics` | `ev-tft-004-scouting-positioning`；`ev-tft-009-backline-access-frustration` | 战前换侧与保护；Set 8 官方回顾批评快速直达后排。PvP 双方反部署和不确定对手不能当 PvE 前提 |
| Auto Chess／`auto-chess` | `ev-auto-chess-013-formation-and-pve-packages` | 狼等 PvE 方向差异会反转传统前后排；主要为 2019 实践与 2020 敌包，不能当新版本单位清单 |
| Dota Underlords／`dota-underlords` | `ev-dota-underlords-009-attacks-mana-targeting` | 站位还改变受击回蓝与首次施法；早期目标档案不证明后期寻路算法 |
| Astronarch／`astronarch` | `ev-astro-003-two-grid-target-adjacency`；`ev-astro-009-fallen-adjacency-intercept` | 两块友方 2×2 区域也能分主副承压与邻接支援；小棋盘深度不等于可通行多路地图；1.2.x–1.5.x 材料分开 |
| Tiny Auto Knights／`tiny-auto-knights` | `ev-tak-004-nine-grid-selector-grammar`；`ev-tak-012-burn-battlefield-status`；`ev-tak-017-row-column-localization-failure` | 行、列、前后、邻接与格子燃烧属于不同范围；日语行列翻译错误获开发者承认。3.9.6 地面燃烧不用于推定完整接触时序 |
| Hearthstone Battlegrounds／`hearthstone-battlegrounds` | `ev-hsbg-003-left-to-right-order` | 左到右出手、嘲讽与溅射邻位形成顺序题；不是走位／多路；2019 基础规则与 Season 13 实践分层 |
| Super Auto Pets／`super-auto-pets` | `ev-sap-003-trigger-order-disagreement` | 同触发顺序及死亡召唤可读性；等攻击如何破平的来源仍冲突，不能由本文裁定 |
| Storybook Brawl／`storybook-brawl` | `ev-storybook-brawl-003-seven-slot-ordering` | 七槽中的前后、首攻与预留空位；历史服务与 71.20 前后规则不混用 |
| Setr's Auto Battler／`setrs-auto-battler` | `ev-sab-009-front-redirection-formation` | 线性队列也能定义前方保护与队首召唤；1.2／1.3 客户端及补丁支持规则，但没有完整空间构筑攻略 |
| Dungeon Tactics: Idle／`dungeon-tactics-idle` | `ev-dungeon-tactics-idle-003-reverse-placement-build`；`ev-dungeon-tactics-idle-004-position-reader-role-inversion` | 六位置的反向部署实践；开发者的同列减伤／后位增伤只是设计示例，不能升级成已实现规则 |
| Magicbook AutoBattler／`magicbook-autobattler` | `ev-mba-013-formation-targeting-counter-packages` | 最近／最远／随机／周围、击退及控制改变目标；精确拓扑不明；有评测认为除射程外站位价值弱，不能仅凭选择器数量认定空间深度 |
| Slotbound／`slotbound` | `ev-slot-012-three-layer-geometry-counter` | 招募九宫格、人口槽与战场分开；骑兵前冲可能被围，后一张地图攻击最后方；仅 Demo 0.2.x–0.3.4 |
| Girls of The Tower／`girls-of-the-tower` | `ev-girls-of-the-tower-011-targeting-and-backline-space` | 射程内优先级、后排诱饵与召唤占位；v1.0.0.2–.7 补丁及历史实战，缺完整目标／碰撞契约 |
| Dwarves: Glory, Death and Loot／`dwarves-glory-death-loot` | `ev-dwarves-glory-death-loot-013-formation-weight-space` | 阵型标签门槛与身体重量／位移是两种空间层；Crow 路线在 v2.1.1 后失效，不能复刻旧构筑 |
| Auto Brawl Chess／`auto-brawl-chess` | `ev-auto-brawl-chess-014-summon-space-reservation` | 召唤构筑需空位；保留身后两格只是单一新手观察，不是通用容量要求 |
| Neon Auto Party／`neon-auto-party` | `ev-neon-auto-party-012-ranged-backline-auto-failure` | 早期测试中前线未保护后排且玩家报告无有效集火；后来一次通关不能证明该问题已修复，不能断言所有版本没有集火 |
| Just King／`just-king` | `ev-jk-001-realtime-cross-formation`；`ev-jk-012-pacifist-environment-owner` | 实时移动旋转与环境击杀；零英雄伤害是历史成就特例，不证明一般打法强度 |
| Survivor Mercs／`survivor-mercs` | `ev-survivor-mercs-002-operation-objective-extraction-loop`；`ev-survivor-mercs-007-hail-spatial-lifecycle`；`ev-survivor-mercs-012-enemy-map-package-counter` | 地图目标驱动持续移动；Hail 后向射击与向前拿资源的目标冲突后多次重做。EA→1.2 生命周期不合成一个版本 |
| Skull Horde／`skull-horde` | `ev-skull-horde-018-ai-aoe-readability-failure` | 多份评测报告自动单位散开送死或扎堆吃 AOE、难辨角色；可证明投诉存在，不能声称测得普遍失败率 |

## 负面证据与设计反例

### 有来源的失败／修订

- **保护失效**：TFT 官方 Set 8 回顾中的后排直达问题，支持“绕过前线需要实质反制”，不证明所有刺客设计都失败。来源 `src-tft-riot-monsters-learnings`。
- **自动执行不可信**：PMM 掩体个案、Neon Auto Party 的历史失败和 Skull Horde 评测从不同控制结构暴露类似风险；它们不是同一 AI 错误，也没有统一发生率。
- **聚集同时放大多条收益**：GGM 1.034 同时调整嘲讽、AOE、召唤与 Extrovert；Mechabellum 1.11 改写 Vortex 连线／重复盾。前者有反滥用说明，后者只证明范围与成本重写，不能虚构统治率。
- **自动攻击背离移动目标**：Survivor Mercs 的 Hail 从后向射击改为行进方向清路／环射，之后继续修目标与朝向；这里有明确生命周期证据，但不支持把本项目改成持续移动玩法。
- **范围表达错误就是决策错误**：Tiny Auto Knights 的行列翻译，以及 Monster Train 层容量／入场反馈问题，说明只显示角色面板不足以解释空间选择；不据此预定本项目 UI 实现。

### 本轮提出的反例，尚未实测

1. 地图画了几条路，但所有单位立即追最近敌人汇成一团：战线只改变开场动画，没有持续的兵力取舍。
2. 每条路永远需要同样的坦克、治疗、输出：只是把一队拆成几个重复小队，并使阵容不足时出现硬门槛。
3. 支援可以无代价覆盖全图，或胜出一路能无时间成本赶到另一侧：集中／分散的代价被抹平。
4. 绕远路永远更慢且没有目标、射线或牵引收益：路线选择是假选择；反过来，无条件瞬达后排也是假选择。
5. 把“守另一侧”交给玩家，却不给可用部署位置、可预测自动行为或适当介入：失败责任落在玩家无法控制的环节。
6. 任一侧失守即全败、敌情又不足以准备：把多线变成强制全面达标；若所有侧线又都可无代价忽略，则任务失去意义。

这些是后续比较方案的检查问题，不是当前游戏已存在的缺陷，也不构成代码任务。

## 未覆盖、证据不足与排除项

### 尚未深研的十个候选

原 roster 均为 pending，当前没有 dossier；没有因为本轮检索而改成已研究。

| 候选 | 现有入口与本轮处理 |
| --- | --- |
| Loop Hero | discovery `s058`：路线塑造线索，偏 R10；没有深证据支持用于 G01 |
| Brotato | discovery 候选；持续操控／构筑相邻样本，本轮无深证据 |
| Into the Breach | discovery `s060`：预告、保护与位移线索，可能帮助比较目标压力；未补规则与实践 |
| Thronefall | discovery `s061`：昼建夜守与兵力分配线索；不能据此推出有效守点规则 |
| Necrosmith | discovery `s062`：自动军团／基地线索；无深研来验证多队执行与基地压力 |
| Legion TD 2 | discovery `s063`：波次与漏怪线索；不能据此给本项目规定波次、路数或漏怪惩罚 |
| Teamfight Manager | discovery 候选；无深档案，本轮不拿其职责或战术当证据 |
| Symphony of War | discovery `s065`：编队、地形与士气线索；回合制大地图尚未核对 |
| Minion Masters | discovery `s066`：桥线／实时召唤线索；尚不能确认区域收益与资源节拍如何闭环 |
| League of Legends | discovery 已有官方角色属性转换数据，但不支持战线论断；不能用 MOBA 常识补成本轮调研 |

因此：**现有语料足以比较站位、目标、接敌、集中／分散与有限介入的边界；不足以给守点／护送／动态占区在本项目中的优劣下结论。** 若用户把任务目标压力选为主轴，下一次优先补与该方向直接相关的机制切片，不必机械续查所有 pending 游戏。

### 已有档案但未达深证据门槛

- insufficient-evidence：Not Your Mama's Autobattler、Milky Way TD、Monsters Auto Battler、Auto GUI Battler、Auto RiskRisk、Auto Jurassic Knights。已读各自判定：分别存在实践过少、构筑缺环、版本失效等问题；有规则片段不等于有效多来源机制证据。尤其不能因为 Milky Way TD 名称含 RTS／TD 就证明多路防守可行。
- discovery-only：Ultra Auto Gotchimon、Auto Dungeon Monsters、Auto Battleships、Auto Immune、Auto Legion、Auto-Arcana、ANANKE、Auto Grind: Dungeons。公开材料不足以闭合规则与实践，有些仍在未发行／测试前阶段；不将宣传、截图或未交付移动权限当作已验证玩法。

### 深库中本轮不继续展开的十五个标题

下列档案与记录参加了全库检索，但仅筛查相关性，未在本轮逐份重读全部构筑，也未用它们证明多战线成立：Backpack Battles、Backpack Dungeon、Backpack Hero（物品／执行空间）；ShapeHero Factory（主要可控空间为生产物流，自动部署不等于可布阵）；Vivid Knight、Slay the Spire（整局路线与资源）；Siralim Ultimate（目标／时间线／宏，非行走战场）；Dungeon 100（技能链与召唤，地图分配证据不足）；Kādomon、Combat Alchemy、Mirror Throne、Epic Auto Towers、My Party Is Grinding、Loot Loop、Gods vs Horrors（类型／牌序／成长／范围等命中，未改变本轮主比较）。这不是否定其对其他议题的价值。

现存 42 个 retained／anchor-retained 标题在上述两张采用表中覆盖 27 个，另外 15 个明确列出筛查边界；另 14 个未达门槛与 10 个 pending 均保留去向，共对应 66 个候选。

### 历史本地设计素材的限制

- 旧图谱 `battlefield.occupancy.body-block`、`battlefield.formation.guard-pocket`、`battlefield.engagement.intercept`、`battlefield.shape.line-pierce` 等提出身体阻挡、保护口袋、截击与射线想法。这些是设计种子，其中“活单位是一格墙”等旧表述与当前连续空间基线不同；不能将其当作实现依据或已采纳规则。
- `runProgression.ts` 的 `multi-front-defense` 是整局概念：“每回合亲赴一线、支援其他线、无法全救、保留成长渠道”。它讨论地区进程与终局收益，不是 G01 一场战斗里的同时接敌。本文只保留引用，不导入“必须失去地区”或“中央预备队”规则。
- 未打开或修改用户浏览器保存的草案；仓库里的 authored 概念不能证明用户选择了它。

## 对方案比较的支持程度

较可靠的共同结构是：**位置改变目标与时间，支援范围产生集中成本，敌人机制让阵型需要适应。** “地图需要可辨认的战线关系”是结合项目既定方向的 Agent 推论；证据没有替用户选定多路、多层、守点、胜负或操作频率。

G01-Q01 的体验主轴比较见 [proposals.md](proposals.md)；后续用户确认独立记录于 [decisions.md](decisions.md)。本文保留检索当时的证据与限制，设计确认不改变原证据的置信度或版本范围。

## G01-Q02：战线关联与支援代价

### 问题与检索增量

2026-09-06，用户在 G01-D01 后要求继续。本次只回答：**各战线应怎样相互影响，使集中与分散都有真实代价，又不退化为独立小队重复或无成本汇团？** 不重新选择 Q01 主轴，不决定具体地图、战线数量、胜负、控制入口或目标算法。

- 复核 G01-D01、I04–I07 与核心规则有关全员自动参与、合法目标、射程／视线、治疗与占位的段落。现行规则包含这些机制，不代表它们已经足以承载 Q02 提案；本次未检查代码或运行行为。
- 对全部 780 条深证据做 domain／mechanism 初筛及记录全文扩查，对全部 56 份现存档案做全文检索。扩展词包括 `互援 / 支援 / 牵引 / 战线 / 接敌 / 转场 / 牵制 / 射线 / 集火 / 分散`，以及 `retarget / intercept / reposition / flank / crossfire / target lock / adjacency / range / overstack / knockback / stunlock / Mark`。
- 回读下面采用的规则、实践、空间条件、反制与版本限制；补读相关 dossier 的目标链、支援范围、操作边界及控制重做段。来源原文的 URL／类型／版本继续通过 source-index 回溯，没有把本轮本地阅读说成重新访问外站。
- 重扫 discovery 的 106 条记录及旧 atlas 的威胁锁定、护卫、截击、最小射程和 multi-front-defense 线索。仍只用于发现；没有导入旧图谱的仇恨数值、截击次数或整局地区经营规则。
- Q01 的全候选覆盖与不足判定继续保留。本次新展开 ShapeHero Factory 的击退生命周期，仅用于“牵制不是无限封锁”的反例；其生产物流仍不能证明战场多线。未补 10 个 pending 游戏的外部调研，未修改研究库。

### 关系机制：支援不只有整队移动

下表“原作依据”来自现有研究；“本题推论”是 Agent 的迁移分析。各机制可以组合，材料没有证明某种组合在本项目中的最佳强度。

| 关系机制 | 原作依据与原证据 id | 对 Q02 的意义与限制 |
| --- | --- | --- |
| **牵动敌方目标** | Mechabellum 的近侧目标与延迟侧翼会牵走部分敌军；防守可用外侧单位拖住侧翼。`ev-mecha-001-target-lock-chain`、`ev-mecha-002-flank-spawn-retarget` | 一侧可在未获胜时改变另一侧承压；投入与抵达时间本身是代价。不能继承塔 debuff、特殊出生时间、PvP 经济承诺或“必定牵走谁” |
| **先后到达与目标接力** | Mechabellum 两套军阵先吸引火力、再清杂，让单体输出／控制接到适合的目标；侧别、射程与到达顺序影响兑现。`ev-mecha-005-arclight-hacker-defense-build`、`ev-mecha-006-sledge-marksman-defense-build`、`ev-mecha-007-chaff-clear-payoff-order` | 支援价值在于让特定角色及时工作，不能只看援军人数。007 本身是基于攻略的设计推论，不是官方“支援系统” |
| **火力在两处压力间分配** | GGM 的射手需射线、距离与实际弹道；The Last Flame 的 The Hands 攻略安排远程在一只手死后转向另一只。`ev-ggm-009-line-of-sight-bodyblock-formation`、`ev-tlf-009-act4-boss-counter-package` | 减少局部压力后，已有火力可以转用于别处；并非所有支援都要整队搬家。Hands 是特定 Boss 实践，不证明通用地图已有跨线规则 |
| **保护与增益有局部受益者** | Astronarch 邻接决定治疗／截伤覆盖，Fallen 的非邻接队友还承担另一类成本；T&T 两名 Mythic 相邻孤岛用于保证增益给对人。`ev-astro-003-two-grid-target-adjacency`、`ev-astro-009-fallen-adjacency-intercept`、`ev-tnt-008-mythic-adjacency-islands` | 分散可能拆开有效组合；小组可以互补而非复制全职责。只能迁移“覆盖与受益者”关系，不复制邻接格数、吸血成本或双人上限 |
| **目标偏好不等于跨空间执行许可** | GGM 战前 Priority Points 仍受射程／最低射程／视线约束；Survivor Mercs 的 Mark 只让符合条件且目标进入射程的 Merc 集火。`ev-ggm-002-prebattle-priority-behavior-layer`、`ev-survivor-mercs-008-mark-focus-fire-module` | “支援某处”与“全队无视距离去追一个目标”应分开分析。不能因此给本项目新增滑条、Mark 或指令，也不能推断普通单位永不换目标 |
| **承压可以发生交接** | TFT 的特定 Briar 构筑让核心先承压，再通过脱离仇恨与附近坦克接力。`ev-tft-014-primordian-briar-build` | 功能单位的价值可以是买出关键时间，而不是独立消灭所有敌人。它依赖 Set 17 的具名机制，不支持给所有单位默认脱战或仇恨移交 |
| **牵制需要仍有结束压力** | Guildrun 0.5.5 增加叠加眩晕抗性／施法免疫以处理持续控制；ShapeHero Factory 1.0 的重复击退逐渐获得抗性直至免疫。`ev-guildrun-016-stun-resistance-rework`、`ev-shf-012-knockback-progressive-resistance` | 若薄弱侧能低投入无限封锁，集中就没有时间风险；这支持检验有限窗口，不预定本项目采用递减抗性、Boss 免疫或固定控制上限 |
| **集中会同时放大多种收益** | GGM 1.034 同时处理嘲讽、范围、召唤与 Extrovert；Mechabellum 1.11 改写 Vortex 连线／产盾。`ev-ggm-013-taunt-aoe-shaman-balance-chain`、`ev-mecha-009-vortex-link-shield-rework` | 不能只按伤害平衡“集中”；保护、聚怪和增益覆盖也会叠加。前者有反滥用依据，后者仅证明重写，不推断原版普遍无敌 |
| **移动介入由产品控制边界决定** | PMM 保留自动路线，允许阶段政策及部分主动操作；Hadean 使用卡牌／有限操作改变目标资格和位置。`ev-pmm-006-auto-path-manual-intent-boundary`、`ev-hdt-009-target-tile-hidden-spatial-cards` | 比较体验时必须标明战前选择、自动结果与主动操作，不能先假设可调配互援再将权限留待后议；房间重排或卡牌瞬移也不能直接当作本项目战术指令 |
| **稳定局面与可预测自动执行不同于锁死目标** | Guildrun 把目标选择与寻路分开，特殊目标规则仍使反向站位有意义；PMM 与 Skull Horde 出现自动移动违背玩家预期的个案。`ev-guildrun-005-targeting-reposition-counterpack`、`ev-pmm-017-cover-and-position-ai-failure`、`ev-skull-horde-018-ai-aoe-readability-failure` | 战线要能被理解，但不是永远禁止重选或转场。AI 投诉不证明一切换目标都有害，也没有统一发生率 |
| **串行纵深是另一种关系** | Monster Train 的敌人逐层上行，同层集中消耗容量与未来入场机会。`ev-monster-train-004-three-floor-battle-order`、`ev-monster-train-010-overstack-capacity-trade`、`ev-monster-train-019-floor-choice-is-contextual` | 可以参考“早处理降低后续压力”“集中有机会成本”；不能由它证明“必须先清完一路才能跨线”，也不导入漏怪／Pyre／七单位容量 |

### 新展开来源的版本与质量

上一节重复采用的来源包与版本限制沿用 Q01 的表格，不再复制整套目录。本次新增到讨论分析的重点切片：

| 原档案与记录 | 本轮重点核对的原来源 | 使用边界 |
| --- | --- | --- |
| GGM 的 Priority Points／`ev-ggm-002-prebattle-priority-behavior-layer` | `src-ggm-official-1-034`、`src-ggm-steam-priority-thread`、`src-ggm-steam-campaign-v1` | 1.034 官方补丁与 1.0 附近实践；逐敌滑条是战前权限，社区明确只“mostly sure”，不是完全控制 |
| Survivor Mercs 的 Mark／`ev-survivor-mercs-008-mark-focus-fire-module` | `src-sm-official-arsenal-0-9-11`、`src-sm-official-final-drill-0-15`、`src-sm-guide-new-player` | EA 0.9.11／0.15 与更新至 1.0 的官方指南；没有同版本完整 Mark 小队实战或精确优先级／持续时间，故只取范围内响应边界 |
| PMM 自动路径边界／`ev-pmm-006-auto-path-manual-intent-boundary` | `src-pmm-official-playtest-1-2024-07-02`、`src-pmm-official-playtest-2-2024-07-11`、`src-pmm-thread-criticism-and-power-score`、`src-pmm-review-kr-runlog-2026-02-19` | Playtest／EA 历史控制边界；讨论中的开发者回复与其他玩家意见分开，最终冷却／失败语义未公开 |
| Guildrun 控制重做／`ev-guildrun-016-stun-resistance-rework` | `src-guildrun-patch-0-5-5`、`src-guildrun-review-position-backup-2026-07-19`、`src-guildrun-wiki-rank-modifiers-0-5-6`、`src-guildrun-patch-0-5-6` | Demo 0.5.5–0.5.6；抱怨早于修复，后续还在纠正文案与交互，没有修后控制覆盖率数据 |
| ShapeHero Factory 击退重做／`ev-shf-012-knockback-progressive-resistance` | `src-shf-official-1-0-0`、`src-shf-discussion-ascension-9-build`、`src-shf-guide-heroes-comment-3363095131` | 1.0 官方重做与骑兵实践；没有抗性阶段、衰减或 Boss 例外的完整数值，不能照抄一套公式 |

另外回读了 `ev-slot-008-deadeye-formation-build` 与 `ev-mba-008-firewall-control-challenge-build`：前者是 Demo 中一个玩家稳定使用的前后排构成，后者是改版前聚怪／控制历史胜例。它们补充“保护与控制给主输出创造时间”，但不提供清线转场或跨线互援的直接规则证据；不把其中的人数比例、特定装备或通关层数带入方案。

### 需要分开的三种代价

以下是基于上述材料与现行空间基线形成的 Agent 分析，不是原作统一规则：

1. **兵力与行动分配**：一名英雄转去处理别处，原先的伤害、控制或保护可能减少；应比较净后果，而不是只数“派了几人”。合法 AOE／覆盖能同时帮助两处时，其效率应当被承认为布阵或构筑收益。
2. **支援生效延迟**：需要移动时有路程与通行，远程支援有射程／射线／目标与出手时机；到场不等于已经救到关键对象。这些可以不同，不要求所有援助都先走路或都缴同一费用。
3. **协同和暴露**：展开可以减少同区拥挤与范围伤害，却可能拆开局部增益；靠近两处压力可以扩大覆盖，也可能更易受攻击。全局增益、传送、共享效果未来仍可存在，是否抵消空间代价要按具体机会成本检验，不在 G01 一概禁止。

特别区分合理支援优势和假选择：正确站位同时照顾两侧是可能的策略收益；若任何敌情、任何阵容都只需占一个安全位置就能完整处理全场，才说明空间分配缺少实质变化。

### 反例与尚未得到的证据

- “清线后全员支援”若太快，会使最快清线阵容稳定滚动吞掉各处压力；若太慢，则薄弱侧每次都必须自己打赢。两端都可能削弱 G01-D01，但当前没有本项目数据判断阈值。
- “交战中互援”若变成全队频繁追随同一个新目标，会消除局部承压；反过来，永久锁区／锁敌又会损害有效转场。现有源码未核查，不能把此风险报告成已知 bug。
- “只派功能位拖延”不是无条件答案。若敌情不接受其控制／阻挡，或者支援已经太晚，它可能失败；本轮不决定失败如何影响整局，也不要求每种阵容每局面都可低投入拖延。
- 瞬移、侧翼出生、跨区域全局效果可能绕过某项代价，但不等于天然错误；需要在 M01／M03／M04／M06 的具体投入与规则里核查。没有据此设定稀有度或成本。
- 现有材料没有提供一套与本项目“10／18 人口、实时自动、有限独立指令”完全一致的多线对照实验；也没有支援时间、转场胜率或交互负担的可直接迁移数据。
- 路程成本只能解释“已经分开之后，支援为什么不免费”，不能单独解释“为什么一开始值得分散”。GGM 的多角射线／AOE、The Last Flame 的不同空间压力和范围覆盖案例提示可检验展开的正面收益；不能据此声称只要加障碍就能形成多线均衡。

证据足以区分各类支援与提出体验方案，但不证明这些行为在本项目中都可由玩家安排。原“战中有限互援、局部突破后扩大支援”推荐已因下述操作边界问题收窄；比较过程见 [proposals.md](proposals.md) 的 G01-Q02 部分。整理时方案未确认，后续用户只确认 [G01-D02](decisions.md) 的主要关联方式，不改变原证据置信度，也不表示已实现或试玩。

### 对用户自走战斗疑问的校正（2026-09-06）

用户质疑除了开局部署，战中并不能调整互援策略。本次回读讨论与[现行核心规则](../../../gameplay-design/tower-autobattler-core.md)的 Combat／Roster Information And Deployment 段，校正证据迁移；未新增全库检索、外部来源、代码检查或运行验证。

- **决定时点与后果时点不同。** Mechabellum 的部署、侧翼与目标链属于准备后自动兑现；Astronarch 的邻接保护来自布阵与角色规则；GGM 的 Priority Points 是战前配置。本题已引用的这些记录不提供战中自由调配权限。The Last Flame 的 The Hands 例子也是通过布置远程，使一只手死亡后能处理另一只，不是通用主动救援策略。
- **需要主动输入的案例单独保留限制。** Survivor Mercs 的 Mark、Hadean 的卡牌介入与 Just King 的整队移动，不可填补本项目在普通自动行为中尚未定义的跨线调配能力。
- **当前基线只支持有限的推论。** 阵型变化属于部署；战中单位优先执行当前位置的合法攻击／治疗，否则按到合法接敌点的实际路径成本等规则选目标。单位可能在目标消失后转向别处，也可能因新近身威胁改变行动；它们没有据此获得“救最危险战线”、守住指定路口或准时交接承压的通用策略。
- **可达、可覆盖不等于会被选择。** 射程／射线是合法行动条件，不能单独证明射手会放弃现目标支援另一侧。范围效果能否连接两处，也需具体自动触发、目标与实际位置条件成立。

据此，Agent 将推荐收窄为局部突破后按规则自动转战为主，有明确自动依据的跨侧覆盖／目标牵动为补充。这是对原提案前提的修正，不是新研究发现或用户决定；现行有限战术指令也未被取消。支援的实际去向、时机与可预测程度仍是待检验缺口，不能仅把“手动互援”改名“自动互援”就视为问题已解决。


<a id="相关机制清单"></a>

## G01-Q02：相关机制展开清单（2026-09-06）

### 阅读边界与本次覆盖

用户已采用的主要关联方式见 [G01-D02](decisions.md)。本清单按用户随后要求补全可见的机制差异，**全部是讨论材料，条目编号只是阅读定位，不是待整包批准的方案**。本次问题仍是：什么改变接敌对象、局部交战持续时间、突破后的转战，以及跨处作用的方式与集中／分散代价。与这些有关的不同权限、模式和历史做法也保留；招募概率、纯装备经济、无战场对应的背包摆放等转回所属议题，不因为同样使用“空间”一词而混入。

**引用方式：**当前议题直接说 K14、K16 或 L09 即可；跨议题可写 G01-Q02-K14。K01–K78 是机制比较项，L01–L12 是待补证据线索，编号与聊天索引一一对应。后续新增采用追加编号，重排或分组不会改变编号，也不复用已移除条目的编号。

**后续决定与当前读法：**[G01-D03](decisions.md) 已采用 K01 的距离原则作为默认接战基础；特殊机制由具体英雄技能、装备或遗物提供，供后续内容设计参考。下表保留原作事实与全部编号，不因归属确认而删减，也不代表其他条目均已采用或均被否定。射程／阻挡等基础约束、自然战术、地图模式和玩家输入差异另按其性质理解，不一概视作技能；不再将整表作为全局 AI 规则的逐项选择题。

- 沿用 Q01／Q02 已有的 66 候选、56 份现存档案、42 份 retained／anchor-retained 档案及未完成去向，不将所有候选都说成同等深研完成。
- 本轮重新扫描全部 780 条深记录。domain／mechanism 的空间、位置、目标、路径、移动、邻接、侧翼、楼层、击退、嘲讽、拦截、控制、范围与出手顺序词得到 176 条候选；在其余记录的 rule_support／practical_support／spatial_condition 中扩查前后排、射线、弹道、召唤空位、陷阱、特殊格等，再得 127 条候选。逐项区分本题机制、同构例证、其他模型及技术故障，命中数不是完备性或质量证明。
- 按相互作用展开原记录，重点回读 Mechabellum、The Last Flame 等具名实践及新出现的跨单位传播、临时身体和特殊地面机制。同步检查 14 份未达深门槛档案的空间与自动行为段，将有区别的线索保留在末表。不是重新联网研究，也未改变任何原库判定。
- 前几轮只概括为“支援／范围／转场”的内容在此拆开。游戏名不同不必另造一条；能改变玩家判断的作用对象、触发、时间或权限不同则分别显示。仍不能宣称穷尽每款游戏所有机制。
- 原 `ev-*` 可在[深证据库](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)逐条查询，其 source_ids 连接[来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)的原 URL、版本与类型。正文中“官方”“攻略”“玩家报告”保留各自证据性质，推论不升级为原作规则。

### 1. 目标怎样被选中，以及局部目标怎样转换

| 编号／机制 | 原作具体做法与操作时点 | 差异、得失及使用边界 | 原证据 |
| --- | --- | --- | --- |
| K01 距离决定首个目标 | Mechabellum 的部署距离、朝向影响最近目标；玩家战前摆诱饵、前线与核心。 | 较低投入单位可先吃锁定；距离变化也可能牵走原本预期的攻击。未公开完整并列规则。 | `ev-mecha-001-target-lock-chain` |
| K02 先判区域，再判前排与距离 | Astronarch 攻略描述先看正前方区域，再看前排和远近，可分配主副坦承压。 | 与全场找最近不同；区域关系会先限制可接到的威胁。小棋盘规则不能直接推成通行战线。 | `ev-astro-003-two-grid-target-adjacency` |
| K03 最远目标引导危险 | The Last Flame 的 Lefty 球与 Guildrun 的远端拉拽／控制，可通过战前最远位置安排承受者。 | 后排不天然安全；最远诱饵还要避免溅射队友。Hands 是特定 Boss 攻略，Guildrun 为 Demo。 | `ev-tlf-009-act4-boss-counter-package`；`ev-guildrun-005-targeting-reposition-counterpack` |
| K04 射程内追打低血量目标 | Girls of The Tower 官方补丁改为近战优先选范围内低血量目标。 | 能收掉残血，却可能改变原先分担压力的关系；先满足范围，不能等同全场精准补刀。 | `ev-girls-of-the-tower-011-targeting-and-backline-space` |
| K05 按输出贡献压制核心 | The Last Flame 的 Vizera 会控制最高 DPS，并持续侵蚀全队。 | 集中投资单一输出会增加停摆风险；多输出承担者才有替代贡献。来自 1.0 特定 Boss，非通用 AI。 | `ev-tlf-009-act4-boss-counter-package` |
| K06 随机或特殊狙击绕过普通选敌 | Guildrun 的随机 Snipe 与隐身有特殊交互，不能只靠距离或坦克挡住。 | 打破固定安全位，但降低布阵可控性；应区分已锁弹体、普通选敌与特殊选择器，版本仍有修正。 | `ev-guildrun-009-stealth-assassin-build` |
| K07 战前设目标权重 | GGM 的 Priority Points 在准备阶段改变目标排序，战中自动执行。 | 比单靠站位表达更明确的偏好；仍受合法射程／射线限制，社区材料也不承诺完全控制。属于 G02 候选权限。 | `ev-ggm-002-prebattle-priority-behavior-layer` |
| K08 装备类别与阶段政策一起分配目标 | PMM 的武器偏好及 Crossfire、Scatter Fire、Slice the Pie 等阶段策略影响自动接敌。 | 能提前分配装甲／轻甲等威胁处理职责；依赖准备政策系统和分段结构，官方完整优先级表缺失。 | `ev-pmm-005-section-target-policy-graph` |
| K09 嘲讽强制改写优先级 | Tales & Tactics 1.2 明确嘲讽可覆盖具特殊筛选条件的技能；GGM 用嘲讽聚敌。 | 可把压力汇到指定承受者，也可能放大范围输出；与普通偏好不同，必须明确覆盖哪些技能。 | `ev-tnt-010-fixed-pve-targeting-and-objects`；`ev-ggm-013-taunt-aoe-shaman-balance-chain` |
| K10 隐身改变目标资格 | Hadean 的 Hidden 到攻击前阻止普通选敌；Astronarch／Guildrun 也有资格与范围伤害的区别。 | 可争取第一轮行动或接近后排；不等于免受 AOE，也不一定取消已发射攻击。原作版本边界各自保留。 | `ev-hdt-009-target-tile-hidden-spatial-cards`；`ev-astro-003-two-grid-target-adjacency`；`ev-guildrun-009-stealth-assassin-build` |
| K11 脱离仇恨后交接承压 | TFT 历史 Briar 构筑先让核心承压，再靠 Rogue 脱仇恨交给附近坦克。 | 承担伤害的角色可以阶段性交替；依赖具名脱仇恨能力，不能赋给所有单位。Set 17 历史攻略。 | `ev-tft-014-primordian-briar-build` |
| K12 目标消失后远程改打另一处 | The Last Flame 的 Hands 攻略把远程安排到一只手死后可处理另一只的位置。 | 火力转移不必等身体跨场移动；但目标可选、射程／射线和自动选择都要成立。不是按需救援系统。 | `ev-tlf-009-act4-boss-counter-package` |

### 2. 接敌先后、拖延窗口与身体转场

| 编号／机制 | 原作具体做法与操作时点 | 差异、得失及使用边界 | 原证据 |
| --- | --- | --- | --- |
| K13 低价值身体消耗首轮攻击 | Mechabellum 用 chaff 先吃锁定；Sledge 防守例还把 Crawler 放后方，延后承担压力。 | 用便宜单位的时间价值保护后续核心，前后放置会改变何时入场；不保证任何杂兵都能拖住所有敌人。 | `ev-mecha-006-sledge-marksman-defense-build`；`ev-mecha-007-chaff-clear-payoff-order` |
| K14 专门清杂为单体核心打开目标 | Mechabellum 先由 Arclight 等清杂，再让 Marksman 或 Hacker 接到高价值敌人。 | 突破速度受清杂能力与目标交付影响，加单体伤害未必有用；这是由具名攻略支持的角色接力推论。 | `ev-mecha-005-arclight-hacker-defense-build`；`ev-mecha-007-chaff-clear-payoff-order` |
| K15 每次获取新目标都要准备 | Mechabellum 1.11 给 Hacker 新目标准备时间，专门限制对大量小单位的效率。 | 换目标次数本身成为时间成本，少量高价值目标与多小目标得到不同结果；不泛化成统一转场费。 | `ev-mecha-011-hacker-target-class-tuning` |
| K16 延迟侧翼入场牵动正面 | Mechabellum 首次侧翼部署延后出现；玩家战前投入侧翼，防守方在外侧布置防线。 | 牺牲早期正面人数换后续目标牵动；依赖特别出生规则与塔目标，不能只搬一张多路地图。 | `ev-mecha-002-flank-spawn-retarget` |
| K17 跳跃或绕到后排 | Auto Chess 的刺客落点、Tales & Tactics 的 Chaser 后排接近改变首轮目标。 | 能绕过正面承压，但需落点与反部署空间；旧刺客攻略甚至让部分单位不跳以扩大有效作用。 | `ev-auto-chess-011-build-assassin-spatial-tempo`；`ev-tnt-010-fixed-pve-targeting-and-objects` |
| K18 冲锋缩短到场时间 | Slotbound 的骑兵快速接敌，分支可结合暴击或眩晕。 | 更快到场也可能脱离保护、被包围；同版本段既有通关例又有过伸失败，不能只保留正面结论。 | `ev-slot-009-all-cavalry-build` |
| K19 击退改变距离与覆盖 | Astronarch 中位移会拆开治疗／保护邻接；ShapeHero 的击退还受战场边缘影响。 | 能延迟敌人接近，也可能把目标推出己方有效范围或拆散保护；效果要按方向和空间解释。 | `ev-astro-003-two-grid-target-adjacency`；`ev-shf-012-knockback-progressive-resistance` |
| K20 拉拽把目标带离原位置 | Guildrun 的最远拉拽使后置角色受到不同压力，玩家会反向或全前排部署。 | 与推远争取时间不同，拉近会直接改写保护对象和局部密度；确切选择与移动契约未完整公开。 | `ev-guildrun-005-targeting-reposition-counterpack` |
| K21 重量影响阵地稳定性 | Dwarves 的 Weight 与位移有关，和队伍 Formation 的职业／元素门槛是两层规则。 | 耐久与不易被推离不是同一价值；旧 Crow 路线已失效，当前完整重量公式缺失，只保留机制区别。 | `ev-dwarves-glory-death-loot-013-formation-weight-space` |
| K22 限制敌人行动来替代部分治疗 | The Last Flame 的历史 No Healer 队以双坦、小范围控制和单体控制争取输出窗口。 | 少受攻击可替代部分续航投入；对免疫、漏控和长战斗脆弱，不能说所有侧线都可无治疗无限拖延。 | `ev-tlf-004-no-healer-shock-stun-build` |
| K23 重复控制产生抗性 | Guildrun 采用叠加眩晕抗性及施法期免疫；ShapeHero 重复击退后逐步抗到免疫。 | 保留开场控制价值，同时限制永久封锁；两种保护的是不同动作，衰减／上限与修后效果均不能照搬。 | `ev-guildrun-016-stun-resistance-rework`；`ev-shf-012-knockback-progressive-resistance` |
| K24 承伤和攻击加快自动施法 | Dota Underlords 的攻击／受伤回蓝，使站在接敌处影响首次施法时间。 | 承压有可能加快技能兑现，也增加先死风险；这与只提高生命不同，早期规则不证明后期寻路。 | `ev-dota-underlords-009-attacks-mana-targeting` |

### 3. 射程、遮挡、弹道与伤害形状

| 编号／机制 | 原作具体做法与操作时点 | 差异、得失及使用边界 | 原证据 |
| --- | --- | --- | --- |
| K25 每个技能各自检查有效射程 | My Party Is Grinding 曾修复技能沿用旧目标、未按自身范围检查的问题；GGM 也把目标偏好与射程分开。 | 人在能射到两处的位置不代表每项技能都能支援；范围投资可能扩大作用，但不保证选中另一侧。 | `ev-mpig-011-range-target-spatial-boundary`；`ev-ggm-009-line-of-sight-bodyblock-formation` |
| K26 最低射程迫使后撤 | GGM 1.034 修复远程受近距离嘲讽时移到合法射程再行动。 | 贴近目标可能反而损失输出时间；提高攻击上限距离无法替代最低射程条件。 | `ev-ggm-013-taunt-aoe-shaman-balance-chain` |
| K27 身体阻挡近战接近 | Dungeon 100 历史召唤链形成身体墙，让部分近战危险难以接近施法者。 | 临时身体买到接触时间，但密度／AOE与后续数量上限会改变收益；旧海量召唤不视为当前可用。 | `ev-d100-008-bestow-summon-chain-body-wall` |
| K28 前方身体截住投射物 | GGM 攻略把坦克或图腾对齐来吃弹道，并为射手避开阻挡线。 | 保护取决于射线，和近战堵路不同；穿透、侧角及 AOE 能突破，原作有未完整列明的拦截例外。 | `ev-ggm-009-line-of-sight-bodyblock-formation` |
| K29 拦截来袭导弹 | Mechabellum 的反导可处理 Stormcaller，Heavy Missile 又以发射间隔换导弹生存。 | 保护的是飞行攻击，不必由单位直接吃伤害；保护覆盖、弹路与不同目标类型形成反制链。 | `ev-mecha-008-shield-missile-counter-loop` |
| K30 屏障阻挡伤害之外的控制 | Mechabellum 的 Barrier 可挡 Hacker 与战场能力，迫使先破盾再完成控制。 | 屏障既是耐久也能阻断机制入口，破盾者可能决定局部突破；不能默认本项目所有盾都有此语义。 | `ev-mecha-008-shield-missile-counter-loop` |
| K31 朝向决定盾的保护面 | Survivor Mercs 的历史 Riot／Defender Shield 受角度、空隙、击退与重弹影响。 | 正面保护可被侧向接近或位移打破；原作依赖主动移动，当前强度与完整碰撞数据未确认。 | `ev-survivor-mercs-009-shield-natural-counter-lifecycle` |
| K32 聚敌让范围输出一次作用多目标 | GGM 嘲讽配 Pyromancer 等范围输出；The Last Flame 的 Corruptors 聚集后更适合 AOE。 | 集中敌人提高每次输出效率，分散敌人则拖慢清理；需要区分敌方聚集与己方扎堆，避免同一词混用。 | `ev-ggm-003-v1-rhino-archer-pyro-shaman-banshee`；`ev-tlf-009-act4-boss-counter-package` |
| K33 多单位共同分摊一次伤害 | The Last Flame 的 Righty cleave 可由多人分摊，也可由特别耐打的单体承接。 | 这是集中减轻单人压力，与一般 AOE 下分散避伤的要求相反；只在该 Boss 具体规则中得到支持。 | `ev-tlf-009-act4-boss-counter-package` |
| K34 部署顺序决定谁先出手 | 酒馆战棋友方通常从左向右攻击；Storybook Brawl 也用槽位、前后排和首攻关系安排角色。 | 位置可以改变先手和触发顺序，未必涉及移动距离；两作的具体结算顺序不相同，保留历史版本限制。 | `ev-hsbg-003-left-to-right-order`；`ev-storybook-brawl-003-seven-slot-ordering` |
| K35 围绕主目标溅射邻位 | 酒馆战棋实践会调整嘲讽旁的单位，降低敌方 cleave 一次打到多个高价值角色的收益。 | 被选中的单位与邻接受害者要分别考虑，单看谁吃第一击会漏掉后排风险。不是所有范围攻击都按此结算。 | `ev-hsbg-003-left-to-right-order` |
| K36 贯穿同一射线上的身体 | GGM 材料将穿透射击列为单一挡弹线的反制。 | 多角度和错位可降低一条线串穿的风险；具体能穿多少身体、碰撞例外尚不完整，不补数值。 | `ev-ggm-009-line-of-sight-bodyblock-formation` |

### 4. 支援怎样跨单位传播，分散会拆掉什么

| 编号／机制 | 原作具体做法与操作时点 | 差异、得失及使用边界 | 原证据 |
| --- | --- | --- | --- |
| K37 邻接治疗或护盾覆盖 | Astronarch 的 Druid／Cleric／Paladin 等按邻接提供治疗或盾，位移后可能失去受益资格。 | 前置支持者还是贴住核心是不同投入；邻接关系不能直接换算为本项目若干格。 | `ev-astro-003-two-grid-target-adjacency` |
| K38 把伤害转移给保护者 | Astronarch 的 Fallen 截取相邻伤害、从不相邻队友慢慢取血；Setr 的 Magician 在开场保护前一位。 | 改变伤害由谁承担，未必增加全队总耐久；Fallen 的近邻获益与远处付费须一起看，不能只摘保护部分。 | `ev-astro-009-fallen-adjacency-intercept`；`ev-sab-009-front-redirection-formation` |
| K39 同行作用范围 | Tiny Auto Knights 区分同行与其他范围，行内角色排列影响来源、受益者与自伤承担者。 | 可以连接横向分布角色，也使同一行共同暴露；不是邻接半径。日语曾混淆行列，完整结算表缺失。 | `ev-tak-004-nine-grid-selector-grammar`；`ev-tak-017-row-column-localization-failure` |
| K40 同列作用范围 | Tiny Auto Knights 把 column 与 row 分开；列可把前后位置串为同一作用组。 | 与同行可能得到完全不同的受益者，不能因棋盘小就合并为同一种范围。 | `ev-tak-004-nine-grid-selector-grammar`；`ev-tak-017-row-column-localization-failure` |
| K41 定向作用于前位、后位或前排 | Tiny Auto Knights 有 ahead／behind／front-row；Setr 的 Paladin 每回合治疗前一位。 | 需要区分一个相对位置与整排受益，移动／阵亡／插入新单位都可能改变关系；线性队列不等于二维路径。 | `ev-tak-004-nine-grid-selector-grammar`；`ev-sab-009-front-redirection-formation` |
| K42 控制随机邻接增益的候选集合 | Tales & Tactics 的 Mythic 给随机相邻盟友 Valor；攻略将两名 Mythic 摆成只彼此邻接的孤岛。 | 额外邻居可能稀释关键增益，所以越抱团不一定越好；依赖随机受益及双 Mythic 规则，1.4.3 实践。 | `ev-tnt-008-mythic-adjacency-islands` |
| K43 距离连线同时给输出和局部盾 | Mechabellum 的 Vortex 近距离连接提高攻击，并以攻击次数产生区域盾。 | 靠近同时叠加进攻与保护，拆队损失不止一种收益；1.11 改了上限与后续产盾成本，未测得通解强度。 | `ev-mecha-009-vortex-link-shield-rework` |
| K44 有限连线名额选择强化对象 | Skull Horde 的 Garg 有数量上限与单位类别距离，将力量给选中的 Champion，其他单位仍可辅助。 | 决定稀缺强化落在主力还是耐久侧；与所有近邻自动全受益不同，原作激活与移动权限不直接移植。 | `ev-skull-horde-010-garg-chain-build` |
| K45 不按局部位置选择全体 | Tiny Auto Knights 开发者澄清 Archer 是作用于 all heroes，而非随机一个。 | 全场范围可能降低空间分配限制；目标阵营资格、数值和当前强度未知，不能从 all 推断敌我规则。 | `ev-tak-015-archer-global-scope-clarification` |
| K46 向随机盟友复制部分效果 | Hadean 2.0 的 Echo Amulet 从全效复制改为向随机盟友施加半效。 | 一次行动可以影响第二个对象，但收益大小和对象不完全可控；不能当成指定跨线支援。 | `ev-hdt-014-copy-propagation-diminishing-value` |
| K47 按同类身份分摊效果 | Hadean 2.0.60 的 Kindred 在匹配副本间分配部分 Shield／Enchant。 | 远近之外还有身份集合约束；多个副本分享一份价值，与各自获得完整效果不同，空间前提不能凭空补充。 | `ev-hdt-014-copy-propagation-diminishing-value` |
| K48 由队伍共享资源连接供给者与核心 | The Last Flame 的 Surge 是共享战斗资源；攻略把生成来源放辅助，把升级触发收益放输出核心。 | 不靠赶路也能让一处角色支持另一处；是否生成足够快与 Slowdown 成本决定价值，资源升级本身没有收益。 | `ev-tlf-007-surge-team-battery` |
| K49 远处代理负责效果发出位置 | Dungeon 100 的 Trigger Sprite 可改变触发位置，计算仍取原角色属性。 | 效果发出点、属性来源和伤害承担者可以分开，可能减少主核心暴露；原作技能链并不提供小队布阵，属跨模型类比。 | `ev-d100-004-trigger-adjacency-proxy-owner` |
| K50 承压后向随机敌人反击 | Setr 1.3 的 Thorns 受击后以自身 Attack 反击若干随机敌人。 | 前线承压能影响非当前接触目标；与 1.2 固定伤害规则不同，不能把承伤或防御值本身算成伤害。 | `ev-sab-007-thorns-defense-to-offense` |
| K51 承压累积后只反击附近敌人 | Girls of The Tower 历史 Stone 在被击时积累 Guard，到门槛后以防御产生附近伤害。 | 给承压侧局部清理能力，无法据此帮助任意远处；历史重做阻止直接外推强度。 | `ev-girls-of-the-tower-008-historical-nine-stone-build` |
| K52 位置直接改变角色形态或职责 | Just King 历史 Druid 前位变龟、侧位变狼、后位治疗，部分装备也读取相对槽位。 | 角色摆在哪会改变它做什么，而非只改变对手；确切形态／装备数值有版本变化，不要求本项目引入通用职责切换。 | `ev-jk-002-position-forms-and-items` |
| K53 多人供给同一敌方状态，再由另一人兑现 | Astronarch 多个角色施加 Burn，Pyromancer 的 Detonate 消费剩余 Burn 造成即时伤害。 | 单位即使职责分开，也可通过同一目标状态协作；必须各自实际影响到该目标，不能误写成全场状态自动共享。 | `ev-astro-004-burn-detonation-five-hero-build` |
| K54 全局阶段统一改变各处效果 | Hadean 的 Moon Phase 改变 Moonmark 的结算，Eclipse 临时同时启用多个阶段效果。 | 共享时点可连接不同作用来源；原作含卡牌和阶段操纵，具体效果仍有目标与空间条件，不证明本项目要加月相。 | `ev-hdt-008-moon-phase-mark-eclipse-engine` |
| K55 把一名角色的防御投入广播给团队 | The Last Flame 研究区分坦克属性广播、维持生存转团队输出，以及个人护盾转暴击等归属。 | 辅助在局部承压也可向团队供给价值，但每一种都需具名转换和受益者；不应把所有防御成长视为全队输出成长。 | `ev-tlf-006-defense-broadcast-to-payoff` |

### 5. 局部胜利怎样产生或释放新的力量

| 编号／机制 | 原作具体做法与操作时点 | 差异、得失及使用边界 | 原证据 |
| --- | --- | --- | --- |
| K56 把活敌人变成己方前线 | Mechabellum 的 Hacker 控制被暴露的中型目标，转换后的单位提供身体并牵动敌军目标。 | 局部突破未必来自击杀；友方过高单体伤害可能先杀掉待控制目标，反而让控制空转。 | `ev-mecha-005-arclight-hacker-defense-build` |
| K57 击杀后利用尸体补充前线 | Skull Horde 的 Adam 先用正常单位制造尸体，再让重生敌人顶在前面。 | 局部击杀可能滚成额外身体，但需要合格尸体；重生单位另有 AI 和 Rally 资格，不能假设能随队调度。 | `ev-skull-horde-007-adam-corpse-build`；`ev-skull-horde-008-reanimated-rally-ownership` |
| K58 召唤插到队首改变下次承压者 | Setr 的 Lost Soul／Necromancer 召唤进入队伍前方，而攻击选择首个活单位。 | 无需让原承压者撤离，也能改变后续目标顺序；队列规则不等于自由战场里必定能插进敌我之间。 | `ev-sab-009-front-redirection-formation` |
| K59 为普通召唤预留合法空间 | Hadean 会填满棋盘；Auto Brawl Chess 的新手实践会给召唤者后方留空位。 | 多身体与腾空之间存在成本，合法人口不保证可生成；后留两格只是单个例子，不推广成统一要求。 | `ev-hdt-005-summon-multicast-board-saturation`；`ev-auto-brawl-chess-014-summon-space-reservation` |
| K60 在附近空位复活已阵亡单位 | Auto Chess 的 Cave Prodigy 从当轮墓地选单位，周围八格全满会取消施放。 | 先前损失、复活者存活和落点空间共同决定回场；继承与目标选择有专门规则，不等于普通召唤或免费补兵。 | `ev-auto-chess-020-cave-prodigy-graveyard-resolver` |
| K61 先保护孵化，再得到多个输出者 | Monster Train 的 Bog Chrysalis 要完成 Shell，之后孵出两只 Bog Fly。 | 承压时间可以换未来力量，不只等另一处清完；受楼层容量和回合机制约束，不能据此宣布实时战场已有孵化系统。 | `ev-monster-train-014-corruptor-bog-chrysalis-build` |
| K62 消耗一个供应者强化指定前位 | Monster Train 的 Primordium 经 Eaten 把属性／状态给前方单位，由后者负责 Sweep。 | 暂时保留额外身体与集中养大一个核心不同；Eaten 不等于死亡，属于相邻位置与阶段供应的模式对照。 | `ev-monster-train-016-primordium-husk-build` |
| K63 局部阵亡转成同区域成长 | Monster Train 的 Fire Light 在 Little Fade 阵亡时增益同层盟友，再用 Reform 循环。 | 损失本身可以是已知收益来源，受益局限同层；这是特殊自我牺牲构筑，不应默认为本项目允许或鼓励损兵。 | `ev-monster-train-012-fire-light-reform-build` |

### 6. 地面、阵地与不同遭遇结构

| 编号／机制 | 原作具体做法与操作时点 | 差异、得失及使用边界 | 原证据 |
| --- | --- | --- | --- |
| K64 地面陷阱分别支援友军和限制敌军 | Hadean 历史 Nightblade 布置盾／治疗／Might 区域，再等敌人聚拢后放控制与伤害陷阱。 | 作用于所在区域，可连接一批角色；原实践依赖卡牌时点与有限指令，不能当纯自动布阵案例。 | `ev-hdt-003-ea-nightblade-three-by-three-traps` |
| K65 地面状态持续影响后来接触者 | Tiny Auto Knights 的 Burn 在战场而非角色身上，3.9.6 每次触发后衰减。 | 单位更换不必清除地面危险，站位同时关系未来受害者；接触时序／叠加／移动尚未完整公开。 | `ev-tak-012-burn-battlefield-status` |
| K66 敌我都可能受益的特殊格 | Tales & Tactics 的 Aquatic fountain 可让敌方受益，并覆盖 pit／fire／mud 等地面效果。 | 争取位置可能同时给对方便利，摆放还改变地面状态；不同于单纯友方光环，也未等同占点胜负。 | `ev-tnt-010-fixed-pve-targeting-and-objects` |
| K67 接触特定地面才兑现强化 | Tales & Tactics 的四 Dragon 首次施法火焰让接触盟友升阶。 | 队伍必须实际接触，开始就有羁绊不等于全员拿到增益；相关地形和首次行动条件会破坏兑现。历史构筑。 | `ev-tnt-004-four-dragon-four-noble-party` |
| K68 有限指定位置给驻留者加成 | Auto Chess 的 Soul Spring 提供两个指定格供部署单位获取加成。 | 把收益限定给少数位置，站位与受益者数量绑定；不能从中推断本项目地块价格或固定双核心。 | `ev-auto-chess-006-trait-threshold-and-bridge-structure` |
| K69 串行纵深让漏过的敌人进入下一层 | Monster Train 低层先交战，未解决敌人上行，顶层得到更多准备时间。 | 提前削弱与延迟成型交换时间；这传递的是敌方压力，和同时两线派己方援军不同，伴随 Pyre 损耗体系。 | `ev-monster-train-004-three-floor-battle-order`；`ev-monster-train-019-floor-choice-is-contextual` |
| K70 移动集中可越常规容量，但挤掉未来入场 | Monster Train 用 Ascend／Descend 叠层，受七单位保护限制，普通部署仍按容量。 | 集中同层协同会压缩后续 Morsel／Imp／复活体入场机会；不能只摘取越容量好处，属于卡牌行动模式对照。 | `ev-monster-train-010-overstack-capacity-trade` |
| K71 不同区域同时施加不同伤害形状 | Monster Train 的 Last Divinity 顶层 Sweep、中层 Multistrike、底层 Trample，并同时进入 Relentless。 | 同一套防守配置不一定覆盖所有区域；这是 DLC 终局特例，不是一般波次，也不直接批准多层地图。 | `ev-monster-train-021-last-divinity-three-floor-exam` |
| K72 保护临时对象到检查点换后续收益 | Hadean 的 Eternal Rift 要让救出的 Survivor 活到下个 Boss 才获得后续奖励。 | 部署保护的收益跨越当前局部交战，可能与清敌效率竞争；属于目标模式候选，奖励细节不完整。 | `ev-hdt-016-survivor-boss-adaptation-objective` |

### 7. 需要另外讨论操作权限的关联方式

| 编号／机制 | 原作具体做法与操作时点 | 差异、得失及使用边界 | 原证据 |
| --- | --- | --- | --- |
| K73 标记后让范围内合格单位集火 | Survivor Mercs 的 Commander 武器标记目标，Merc 在标记目标进入射程后响应。 | 指向明确又保留空间资格，但依赖主动标记；不能从已有有限指令概念推出本项目已有此能力。 | `ev-survivor-mercs-008-mark-focus-fire-module` |
| K74 用卡牌选单位与落点换位 | Hadean 的 Shadowstep 选单位再选位置；部分相关效果需要空格。 | 能直接改变接敌与邻接，代价来自卡牌／行动和落点；属于主动介入，和自动冲锋或目标重选分开。 | `ev-hdt-009-target-tile-hidden-spatial-cards` |
| K75 领队位置间接牵动自动队友 | Survivor Mercs 直接操控 Commander，而 Merc 独立移动、选敌、开火并受领队位置影响。 | 不逐个操作也能持续改变全队位置，但需要持续领队输入；本项目没有此常规权限。 | `ev-survivor-mercs-003-commander-merc-control-boundary` |
| K76 集合命令只作用于合格单位 | Skull Horde 的 Rally 不作用于重生敌人，后续还修复了它们在集合时冻结的问题。 | 可将常规队伍与自主临时前线区分，但权限集合必须清楚；不能假定任意召唤都服从同一命令。 | `ev-skull-horde-008-reanimated-rally-ownership` |
| K77 整体平移与旋转固定阵型 | Just King 直接移动、旋转十字队伍，改变接触、射程和环境重叠。 | 内部相对位置保持、外部接敌持续改变；玩法成本是持续操作，不能与纯战前布阵按同成本比较。 | `ev-jk-001-realtime-cross-formation` |
| K78 分段之间重新排列队伍 | PMM 在阶段政策和选定介入之外保留队伍重排，而具体路径自动执行。 | 额外准备窗口可以修正下一段分工，但不等于同一局部战斗随时调兵；完整时点与费用仍缺规则表。 | `ev-pmm-006-auto-path-manual-intent-boundary` |

### 8. 有区别但证据不足的线索，也保留可见

以下不进入原深证据库，也不伪造 `ev-*`。它们的产品承诺、局部补丁或单一观察值得保留为讨论线索，尚不足以证明完整玩法或当前效果；不因列入就授权补研、采用或实现。

| 编号／线索 | 已有材料实际说了什么 | 与本题的关系和缺口 | 原档案 |
| --- | --- | --- | --- |
| L01 部署会扩大可建设范围 | Milky Way TD 正式版长评描述圆形合法范围随继续建设扩大。 | 扩张可能改变需要照看的边界；没有完整敌人路径和布阵实践，不能推定自动形成多线。 | [milky-way-td](../../../web/game-mechanics-atlas/research/deep/game-dossiers/milky-way-td.md) |
| L02 资源点邻近与核心防守争空间 | 同作资源建筑必须靠近资源，敌人按波攻击中央星球。 | 产能位置与防守位置未必一致；属于基地／资源模式差异，没有足够构筑证据选作本项目规则。 | [milky-way-td](../../../web/game-mechanics-atlas/research/deep/game-dossiers/milky-way-td.md) |
| L03 部分区域侦察再调整部署 | Auto Battleships 产品材料描述不同形状／重复次数的 scouting 与重新部署。 | 信息也是投入；没有可复现侦察流程、成本或匹配规则，只保留产品承诺。 | [auto-battleships](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-battleships.md) |
| L04 瞄准爆炸物带来连锁收益 | Auto Battleships 宣传提及敌方 explosive 可成为攻击对象。 | 突破可以经场上对象扩散，区别于直接打单位；触发、传播、友伤与拆除规则全部待证。 | [auto-battleships](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-battleships.md) |
| L05 磁吸改变敌人接近路径 | Auto Immune 2023-12 公告移除按颜色磁吸，引入 Magnet，使特定吸引影响所有 virus。 | 是吸引空间轨迹的规则线索，不能与单纯改攻击目标的嘲讽混为一谈；范围、并列与路径未完整公开。 | [auto-immune](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-immune.md) |
| L06 最近邻连接在后续准备时交换生命 | Auto Immune 的 Swapper Bond 在下一 shop round 与最近的已连接 cell 交换 Health。 | 位置连接能把当前局部状态带进后续准备；不是战中治疗或实时互援，距离重算／断链规则缺失。 | [auto-immune](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-immune.md) |
| L07 可移动防守锚点与多条攻击线 | Auto Immune 公告确认 Red Blood Cells 可移动；商店还描述旋转及最多三条攻击 lane。 | 可以提出“保护对象是否可搬移”的区别，但移动时点、lane 开放和战中权限未证实，不能拼成完整移动基地玩法。 | [auto-immune](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-immune.md) |
| L08 前排跳回后排 | Auto Jurassic Knights 补丁涉及前排 jump back，以及位移后重算命中／目标。 | 与向敌后跳跃相反，是退出原承压位置；何时触发、代价与完整队形未知。 | [auto-jurassic-knights](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-jurassic-knights.md) |
| L09 目标死亡后的剩余伤害转给下一个 | 同作 Bomber Fern 在前排目标死亡后把剩余伤害转给 next unit。 | 局部突破可直接传递本次攻击的余量，区别于重新选敌等下一次攻击；范围与完整技能强度未证实。 | [auto-jurassic-knights](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-jurassic-knights.md) |
| L10 前位攻击触发后位或中介行动 | 同作 Commando Carno／Echo Caller Vi 修复链涉及 ally ahead attacks 与 on-hit intermediary。 | 位置可传递攻击事件而非只传递数值；没有完整编队和实践，不能补成无限连击。 | [auto-jurassic-knights](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-jurassic-knights.md) |
| L11 条件自动施法与可选自动移动 | Auto Grind: Dungeons 产品承诺按 cooldown／condition 自动用技能，并可选择移动自动化。 | 尚不知条件由玩家编写还是系统固定，也不知关掉自动移动后如何操作；列为控制边界线索。 | [auto-grind-dungeons](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-grind-dungeons.md) |
| L12 位置条件直接改变减伤或伤害 | Dungeon Tactics 开发者举过同列减伤、后位增伤；其实际反向布阵另有玩家案例。 | 这些加成是设计示例，不是已实现规则。保留可讨论的区别，不与已验证的反向站位混用。 | [dungeon-tactics-idle](../../../web/game-mechanics-atlas/research/deep/game-dossiers/dungeon-tactics-idle.md) |

### 不同机制的组合与冲突（保留分析，采用范围见决定）

- 目标链、清杂、换目标准备、路程和身体遮挡可以共同决定一次自动转战，不是互斥套餐。目标可达、能够命中、选择它、来得及出手是四个不同条件。
- 同行、同列、邻接、相对前后、身份集合、距离连线和全场作用不能压成一个“支援范围”。近邻随机增益可能奖励双人孤岛；局部护盾连线则可能奖励更多聚集；需要同时展示相反取舍。
- 距离／随机／最高输出等目标规则，和嘲讽／隐身／脱仇恨的覆盖关系会决定布阵可预测性。G01-D03 已明确距离为默认基础、特殊规则由具体内容提供；未选定具体特例及冲突优先级，不把各游戏拼成一套通用 AI。
- 更快接敌、更多身体、更高集中和更早突破都可能有反面：骑兵过伸、召唤堵住落点、AOE 共同暴露、失去准备时间。反过来，过度拖延也可能让战斗没有终结：Setr 的互相治疗僵局属于有报告的失败例，不能只摘取无限续航收益（`ev-sab-011-healing-stalemate`）。
- 行走支援、远程换目标、转换敌人、尸体补兵、剩余伤害传递、共享资源、身份传播等是不同的优势传递通道；G01-D02 只确认主要体验，没有批准这些具体通道。
- 敌人如何施压也不是一套：最远球、最高 DPS 控制、群体伤害、伤害分摊、背后突袭与不同区域伤害形状各自留下不同答案。依赖已知敌情才有意义的部分交 R12 校核，不默认预告所有数值或未来行动。
- 主动标记、卡牌位移、领队移动、整体旋转、阶段重排和保护对象模式都保留为对照；若用户想深入，再明确它们会影响 G02、M02 或 I07 的哪些边界，而不是由 Agent 先删掉。

### 后续讨论入口

Q02 的默认基础与内容特例归属已由 G01-D03 收束，无需逐项表决清单或先局测才能确认距离原则。用户仍可按稳定编号深入任何素材；继续 G01 时，展开“合理局部受挫、失败可解释性”的不同原作做法，将延缓手段作为相关内容证据，不重新表决 G01-D01／D02／D03，也不直接缩成新的 ABC。未定参数、技术可行性与用户偏好保持分开。


<a id="局部受挫与失败解释"></a>

## G01-Q03：局部受挫与失败解释（2026-09-06，范围过宽，保留材料）

**当前读法：**用户指出下列清单混入胜负／关卡、死亡结算、羁绊、跨战与反馈规则，超出了“一处没守住但全队获胜”的问题。Agent 接受纠正并撤回 Q03-P01；[G01-D04](decisions.md) 仅记录常规歼灭目标与特殊关卡归属。以下问题表述、检索范围和综合是当时的研究过程，不再作为当前待决清单或证明范围恰当的依据。F／FL 编号和原作证据保留，后续到所属议题再引用；相关性须能直接改变当前答案，不能只凭关键词或间接影响。

**本轮可闭合子问题：在距离接战、突破后自动转战的前提下，能否接受“一处没守住，但完成了作用，全队最终获胜”；要让玩家怎样辨认这类代价与真正的布阵失误？**

D01–D03 继续有效。局部接敌关系不是必须单独结算的固定道路；本题不新增局部胜负计分，也不确定伤亡恢复、扣命、永久死亡、重试、地图目标或战报界面方案。以下把不同机制完整保留，同时标明基础／自然战术、内容特例和其他议题归属，不让用户逐项批准全部素材。

### 本轮检索与证据边界

- 沿用前两题核验的候选覆盖：66 候选、56 份档案，42 份 retained／anchor-retained、14 份未达该门槛，10 候选无深档案。原库只读；没有新增联网研究。
- 重新检索全部 780 条深记录的 mechanism、rule_support、practical_support、failure_explanation。首轮以 `casualt / defeat / death / sacrifice / leak / pyre / stalemate / timeout / attrition / revive / resurrect / combat log / battle report / replay / telegraph / preview / forecast` 及中文同义词筛出 162 条；沿信息归因补 `report / attribution / intent / readability / fatigue / enrage / loss / retry`，扩展命中 484 条。后者包括大量工程归属和非战场损失，不能把命中数当作有效机制数或覆盖率。
- 全部 56 份档案参与死亡、伤亡、战报、复盘、预告、僵局与退出等正文检索；重点回读本题的损失循环、版本和负面案例。用深记录的原作规则、实战、限制及 source_ids 交叉阅读，未逐字重读所有无关构筑。
- 发现层全部 106 条记录再次筛查机制与结果字段，得到 15 条候选。队列站位、模拟调整、塔恢复等由已有深证据承接；敌人进化、技能槽、身体部件、属性转换本身不能证明本题的损失／归因机制，未另作条目。完整意图、保护设施与无尽压力保留为 FL 线索。
- F01–F41 引用 53 条深证据、涉及 29 个游戏；编号在 Q03 内稳定，后续追加不重排。F 是本题比较条目，FL 是未达深证据门槛的线索；既有 K／L 编号不改。引用同一记录的不同条目只在作用、时点或归属确有差异时拆开，不按游戏数凑项。
- 不把 dossier 中“战报应显示”“本项目可迁移”当作原游戏已有功能。例如 Mechabellum 的目标／时点材料证明战术因果，不证明原游戏已提供完整因果时间线；Siralim 的日志投诉也不证明某个替代 UI 已获验证。
- 归属排除：招募交易失败、存档回滚、奖励重复、无关装备供给细节交所属模型或工程；具体控制、位移、代伤等内容沿用 Q02 K 清单，不重造默认 AI；模式／损失比较仅说明代价如何改变战场选择，具体契约归 G02／G03／R11／R12／R14。
- 现行权威只读基线：已有“早期单次伤亡不应自动毁掉整局”的要求、选中单位动作解释与分维度战报；本轮讨论的是体验标准及证据，不声称这些已完整解释因果，不修改权威或实现。

### 机制与差异

**局部损失怎样仍然完成作用**

| 编号／机制归属 | 原作做法、玩家选择、得失与证据限制 | 深证据 |
| --- | --- | --- |
| F01 承压后阵亡仍可能完成任务〔自然战术；K01／K13〕 | Mechabellum 的攻略先放可消耗单位或坦克接住最近目标，再清杂、保护单体输出。玩家战前决定谁先接敌；前排没活到最后，也可能已换到有效输出时间。代价是保护消失后核心会暴露，不能仅凭阵亡或低伤害判它无用。攻略支持这一战术，未给出通用“拖够几秒”标准。 | `ev-mecha-001-target-lock-chain` |
| F02 消耗廉价身体换掉敌方危险效果〔具体内容／阵容；K58〕 | Gods vs Horrors 的埃及召唤队用可消耗召唤物承受 Moiralith 毒／反伤前排的致命交换，同时让死亡触发者成长。玩家选阵容与排列，战中自动执行；收益是把昂贵主力的损失转给临时单位。该攻略也警告召唤路线对 Azathoth 不佳，不能将人海视作万能答案；证据为 1.0 指南及个别实践。 | `ev-gods-vs-horrors-007-egypt-summon-poison-build` |
| F03 死亡接续新身体，后排维持增益〔具体内容；K58／K63〕 | Super Auto Pets 以 Cricket／Sheep 等阵亡生成替代身体，Horse／Turkey 增益新召唤，Fly 继续补兵。前排死亡是计划中的触发，后排发动者才需保住；空位、狙击发动者、溅射与连续清杂会破坏链。2026 路线与 2022 反制仅支持结构，不合成当前强度结论。 | `ev-sap-006-horse-turkey-fly-summon-build` |
| F04 反复死亡／复活换同区成长〔具体内容，主动卡牌差异〕 | Monster Train 的 Fire Light Little Fade 死亡时强化同层友军并增加 Burnout，玩家再用 Reform 带回，持续供给受保护的输出层。代价是复活牌、行动资源与布置时机；强化保留限定在该场战斗。纯 Fire Light 并非天生 Endless，原作战中出牌权限也不能直接搬进本项目。 | `ev-monster-train-012-fire-light-reform-build` |
| F05 保住最后存活者，等待队友依次归来〔全局生命循环，模式差异〕 | Skull Horde 的死者按死亡顺序等待可见冷却后返回；普通角色整队同时倒下才消耗三条命之一。玩家围绕坚韧单位跨过复活空窗；局部阵亡暂时降低战力，连锁全灭则有更大代价。完整冷却公式未公开，角色还存在例外；这与本项目当前默认死亡不自动复活不同。 | `ev-skull-horde-004-death-respawn-life` |
| F06 首次死亡援护附近队友，满条件再有一次复活〔具体羁绊〕 | Dota Underlords 后期 Fallen 的首次死亡效果治疗／强化附近友军，满羁绊带来一次战斗复活。战前投资羁绊与站位，死亡变成有限保险及传递收益；需要满足人数条件，不能等同全员无限复活。来源属于 2020 后期版本，不能作为现行游戏或本项目默认规则。 | `ev-dota-underlords-013-build-hunter-fallen-heartless` |
| F07 本场被摧毁，下一轮恢复且保留长期增益〔跨战规则〕 | Epic Auto Towers 战毁塔在下一回合恢复，跨回合永久加成保留。玩家仍要处理当前波次，却可把身体损失与长期投资清零分开；好处是敢于使用前排，代价是单体保全本身的长期压力较小。原作是塔阵与回合循环，不能据此承诺我们的英雄自动复活。 | `ev-eat-001-limited-board-stack-loop` |
| F08 高投入单位死亡还会强化敌人〔死亡结算／成长规则〕 | Mechabellum 高等级单位死亡会给敌人更多经验，攻略因此提醒不要盲目升级炮灰；一级 Sledge 有时已能完成承压／清杂职责。升级能延长当前存活，但阵亡后可能加速敌方清杂成长。这是在死亡数量之外增加投资风险，具体公式来自维护 Wiki，且有 PvP 成长前提。 | `ev-mecha-003-upgrade-xp-bounty` |

**损失怎样带到后续：关联比较，具体选择归 R11 等议题**

| 编号／机制归属 | 原作做法、玩家选择、得失与证据限制 | 深证据 |
| --- | --- | --- |
| F09 伤亡消耗与构筑共用的整局生命资源〔R09／R11〕 | The Last Flame 档案记载英雄死亡损失 Flame；Flame 也可支付重抽、营火和 Reborn。允许拿生命容错换当前／未来构筑机会，但一次伤亡也减少后续消费空间。攻略建议用这项资源解决当前幕而非只保满；旧 EA 与 1.0 数值有差异，本轮不推定扣费公式或其他跨战细节。 | `ev-tlf-001-run-resource-routing` |
| F10 局部漏怪逐层兜底，最后损失核心生命〔地图模式；K69〕 | Monster Train 敌人从下层逐步上行，当前层没杀掉仍可由上层处理；抵达 Pyre 后立即交战，Pyre 会反击，但受到的伤害跨战保留。可以交换底层防守和上层成长时间，代价是漏得过多消耗整局生命。它需要串行楼层和核心目标，不是距离自走规则自动带来的机制。 | `ev-monster-train-004-three-floor-battle-order`；`ev-monster-train-006-pyre-persistent-run-cost` |
| F11 整场失利消耗有限容错，仍可继续准备〔R11〕 | Gods vs Horrors 用整局 shields 承受失利，Astronarch 攻略允许通过 Morale 承担有限失败；Mirror Throne 的历史 VS 是在 Hope 耗尽前取得十胜，Neon Auto Party 历史录像展示扣一条命后回到同局准备。共同点是失利不立即终局；差异在资源来源、扣除和下一次遭遇，现有语料不足以统一这些细则。 | `ev-gods-vs-horrors-012-run-shield-versus-combat-defense`；`ev-astro-010-route-morale-potion-swap-decisions`；`ev-mirror-throne-003-hope-arena-adaptation-loop`；`ev-neon-auto-party-017-failure-xp-growth-rework` |
| F12 开局保留胜负，但暂免失败扣生命〔G03／R11〕 | Setr’s Auto Battler 1.3 让前三轮失败不扣生命，同时降低重抽成本、给首轮免费重抽。玩家有更宽的启动窗口，失败仍发生且可能影响胜场或收入；代价是早期求稳的激励变弱。它是一次成组节奏改动，不能从补丁单独证明保护期改善了留存或平衡。 | `ev-sab-006-opening-protection-tempo` |
| F13 永久伤亡由复活、投降和替补缓冲〔R04／R11；权限差异〕 | Gladiator Guild Manager 在永久死亡模式下，攻略建议准备复活水晶、致命前投降、用早期竞技场培养替补；普通模式和永久死亡模式必须分开。每名单位的生还更有分量，但恢复、名册和退出机制承担配套成本；本项目不能只搬永久损失而忽略整套循环。 | `ev-ggm-010-task-size-loss-replacement-decisions` |
| F14 可长期重建，或限定能承受的失败／跳过次数〔G03／R11〕 | Gladiator Guild Manager 普通战役攻略利用下月重试、旧场训练和等待复活重建；2025 可选 Glory 模式把锦标赛失利／跳过限制在一至七次，耗尽终局。前者适合反复修正，后者保留整局压力；官方指出无限时间可磨过高难，但没有数据证明哪种容错最好。 | `ev-ggm-014-glory-mode-failure-budget` |
| F15 幸存积累的是下次优势，死亡丢掉这份积累〔R11／R14〕 | Survivor Mercs 成功撤出的佣兵可在以后招募时携带最多三次免费初始升级，后续出战被杀会丢掉。保住老兵有回报，再派上场则冒风险；不是阵亡就删除角色本体的同义词。早 EA 的十次升级条件与当前指南的专精六级是不同版本，不能叠加。 | `ev-survivor-mercs-011-survivor-bonus-risk-lifecycle` |
| F16 整局失败仍按已完成进度给予成长〔R14〕 | Neon Auto Party 开发者回复称整局结束时，参与单位按清过的波次获得经验，失败局也有；这与 F11 的扣命后同局继续是两层机制。它减少整局白打感，也可能把学习与刷成长混在一起。后续 0.5.2 调整经验缩放，但没有实战证明新曲线，也不是每扣一条命立刻获得永久经验。 | `ev-neon-auto-party-017-failure-xp-growth-rework` |
| F17 连败及保留经济使当前输局成为投资〔R09／R11；PvP 对照〕 | TFT 的连败收入、利息与升级／重抽机会让玩家权衡当前生命和未来阵容；血量或对手节奏逼近危险时，攻略建议提前花钱稳住。好处是输局也有经营选择，风险是故意输压过正常求胜。具体连败档位和多人节奏是赛季条件，不直接转成单机塔爬奖励。 | `ev-tft-001-economy-tempo` |

**争取时间与无限拖延的区别：节奏机制比较**

| 编号／机制归属 | 原作做法、玩家选择、得失与证据限制 | 深证据 |
| --- | --- | --- |
| F18 超时后逐步增加公共伤害〔G03／M04〕 | Tiny Auto Knights 用终局 Poison 结束治疗僵局；玩家认为第五轮后开始太早，开发者移到第九轮后，之后又加快增长。它给慢启动／治疗留时间，同时要求最终完成输出；触发太早会把正常防御构筑一并压掉。没有完整历史曲线，不能照搬回合数。 | `ev-tak-011-overtime-poison-stalemate-budget` |
| F19 先修造成僵局的防御消耗规则〔M04〕 | Tiny Auto Knights 将旧 Armor 每次受击只减一点改为按伤害消耗，并加 999 上限；此前有 2500 以上护甲的镜像长战报告。这是直接改变高护甲面对小伤害的耐久，而非单纯到时惩罚；也会改变正常护甲价值，不能当作无代价修复。报告不代表普遍发生率。 | `ev-tak-006-armor-decay-and-stalemate-rewrite` |
| F20 由敌人的狂暴压缩拖延窗口〔敌人内容〕 | The Last Flame 的 Boss 包含 Enrage；Dwarves v1.20 则明确 Boss 三分钟后提高伤害。前排、治疗和控制是在期限前争取输出机会，不保证能无限撑住。收益是压力有明确敌方来源，代价是仍偏向输出门槛；两作的计时与增幅不能混成一个标准。 | `ev-tlf-009-act4-boss-counter-package`；`ev-dwarves-glory-death-loot-015-anti-stalemate-contract` |
| F21 让玩家选择停止治疗、全队冲锋〔G02 权限对照〕 | Dwarves v1.20 用一分钟后可发出的停疗冲锋命令替代普通战斗的自动 Sudden Death。玩家选择何时放弃续航赌决胜，保留较慢阵容的主动权；代价是新增战中操作和最后一搏的责任。它不能因好用就默认加入本项目指令。 | `ev-dwarves-glory-death-loot-015-anti-stalemate-contract` |
| F22 提供退出路径，但退出不等于自动解决僵局〔G02／G03〕 | Backpack Dungeon 在疲劳之外把投降出现时点从五十循环提前到二十，Blood Battle 始终可用；Dungeon Tactics Demo 开发者让玩家回主菜单退出长战。前者有规则压力加退出，后者只证实退出建议；都不能用加速代替终止。Setr 有两份治疗超过伤害而无法结束的报告，是缺少可见终止规则的反例，隐藏超时是否存在仍未知。 | `ev-bpd-013-fatigue-surrender-antistall`；`ev-dungeon-tactics-idle-006-stalemate-speed-guard`；`ev-sab-011-healing-stalemate` |

**玩家怎样理解这次失败：原作实际反馈与局限**

| 编号／机制归属 | 原作做法、玩家选择、得失与证据限制 | 深证据 |
| --- | --- | --- |
| F23 提前公开关键敌人的机制，保留准备窗口〔R12〕 | Gods vs Horrors 从开局就知道最终 Boss；Monster Train 的不同 Seraph 变体分别针对攻击、状态、法术资源或触发频率。玩家有机会沿途调整，代价是路线可能围绕最终考题收束。前者玩家仍报告锁定路线后缺关键反制：知道考什么不等于拿得到答案。 | `ev-gods-vs-horrors-015-enemy-package-counter-grid`；`ev-monster-train-020-seraph-four-counter-packages` |
| F24 显示即将行动的意图，并给当前应对窗口〔R12／G02 对照〕 | Vivid Knight 显示单位意图，玩家可留两枚护盾应对危险 Boss 行动、选择使用或跳过 Gem；Backpack Hero 重做敌人行动提示及可打断意图。信息直接服务本回合动作，代价是阅读与反应负担。两者有战中选牌／用物权限，不能从意图可见推导本项目也能临场改部署。 | `ev-vivid-010-gem-intent-skip-intervention-layer`；`ev-backpack-hero-023-readable-enemy-counter-packages` |
| F25 准备时显示效果实际连接谁〔空间反馈；K37–K44〕 | Backpack Battles 官方更新加入受影响物品高亮与配方失败反馈，帮助辨认发动者是否连到正确受益者。可减少看似完整实际没连上的误判；但物品格连接是静态关系，我们的单位会移动，部署覆盖不能冒充整场保证。更新没有可用性研究证明误解全被解决。 | `ev-bpb-013-build-attribution-ui` |
| F26 显示修正后的实际数值〔数值反馈〕 | Neon Auto Party 0.4.1 的提示显示修正后的伤害／治疗，而非仅基础值。玩家更容易判断投入有没有生效；现有来源没有说明是否计算目标抗性、位置或 Barrier，不能据此声称提供了准确的最终伤害预演。 | `ev-neon-auto-party-018-actual-output-tooltip` |
| F27 战中实时数值与战后统计相互参照〔贡献反馈〕 | Slotbound 0.3.2 修复结算不显示伤害，0.3.4 加入战中实时伤害更新；Backpack Battles 也有物品伤害／治疗值及生命加 Block 显示。能观察主要输出何时开始发挥，代价是实时读数争夺注意力；文献未说明过量、召唤等计数边界，且总量仍不能解释为什么前排早倒。 | `ev-slot-018-damage-readability-chain`；`ev-bpb-013-build-attribution-ui` |
| F28 把普攻与技能贡献拆开〔战报反馈〕 | The Last Flame 的攻略通过上一战回顾辨认谁做了什么，资料区分攻击和技能伤害；My Party Is Grinding 按英雄及攻击查看伤害。玩家据此判断该补普攻、施法还是换装备。它有助于找到收益端，仍不自动解释施法为何没启动或辅助为何低伤害。 | `ev-tlf-011-combat-recap-attribution`；`ev-mpig-010-damage-meter-build-attribution` |
| F29 把共享战斗资源也放入回顾〔构筑反馈〕 | The Last Flame 1.0 将 Surge Gauge 加入上一战回顾，使玩家不只看最终伤害，还能查看共享资源这一环。适合区分没有启动和启动后收益不足；现有正文未完整列出 Gauge 形态或全部来源字段，不能声称已能逐条追踪每名供能者。 | `ev-tlf-011-combat-recap-attribution` |
| F30 召唤物单列，同时保留召唤者层级〔战报反馈〕 | Gladiator Guild Manager 的战后统计把召唤物放在召唤者下面，分别呈现双方贡献并可折叠，另列伤害、承伤和治疗。既能知道本体贡献，也能看见召唤收益；合计和子项若混加会误读。官方曾提出综合分，但不能用一个分数代替不同职责价值。 | `ev-ggm-011-battle-stats-summon-attribution` |
| F31 按事件查看伤害、治疗、状态与来源〔日志反馈〕 | Hadean Tactics 1.1 加入三类日志，后续修过首个伤害缺失、特效遗漏、异常状态错误头像／所有者和上一战残留。事件层比总数更接近原因，但前提是来源、顺序及所属战斗正确；补丁证明修过这些点，未公开完整归因模型。 | `ev-hdt-011-battle-log-source-attribution-lifecycle` |
| F32 零值、负值、召唤与射程也给可见反馈〔状态反馈〕 | Mirror Throne 的历史更新加入零／负数跳字、战中提示、等级相关敌情、召唤强化反馈及正确远程范围图标。让“没有正伤害”也有可观察结果；代价是更多符号与读数。仍有新玩家认为主要靠试错学习，说明功能存在不等于因果已经讲通。 | `ev-mirror-throne-009-reader-onboarding-evolution` |
| F33 相似外形对应一致的命中预期〔规则与表现一致性；K28〕 | Mechabellum 1.11 把 Abyss 每次对 Sledgehammer 的命中上限由一次改为两次，官方指出旧表现与相近体型单位差异过大、令玩家困惑。它通过修规则减少反直觉，而非只加一段解释。没有完整碰撞规范；启发是可见体型不能持续误导玩家。 | `ev-mecha-012-abyss-sledge-readability-fix` |
| F34 日志很多、结果确定，也可能仍找不到原因〔负面对照〕 | Siralim Ultimate 已有战斗历史，玩家仍报告看不出突然团灭的来源或计算修正；Gods vs Horrors 宣传战斗无 RNG，也有链式伤害／死亡难以诊断的讨论。确定性和大量文字各自不足以建立因果；这些是有限个案，不能据此断定整款游戏普遍不可读，也没有证据证明某种替代界面必然有效。 | `ev-siralim-ultimate-029-battle-history-attribution`；`ev-gods-vs-horrors-003-deterministic-combat-explanation` |
| F35 影响构筑的减速、触发上限必须能查明〔契约反馈〕 | The Last Flame 七件物品曾带未写出的 Surge Slowdown，1.0.1 将其移除；Siralim 的一名长期玩家很晚才发现复活／事件上限，规则来源能证实有上限但不能证实人人都被隐藏。前者是未披露成本，后者是发现路径问题；不能把二者都解释为玩家不会配装。 | `ev-tlf-013-hidden-surge-slowdown-removal`；`ev-siralim-ultimate-031-hidden-cap-disclosure` |
| F36 允许反复模拟同一任务、调整后再执行〔G02 权限对照〕 | Private Military Manager 把观察自动模拟、改装备／位置／区域策略、再模拟作为核心循环，然后进入实际行动。能用改动比较学习原因，代价是重复试解可能改变未知遭遇的风险体验；玩家也指出模拟之间缺直接结果比较。不同模式日程不同，本项目不因此取得重试或撤回战败权限。 | `ev-pmm-001-observe-refine-repeat-loop`；`ev-pmm-014-section-report-attribution` |

**哪些失败不能轻易归为玩家布阵错误**

| 编号／机制归属 | 原作做法、玩家选择、得失与证据限制 | 深证据 |
| --- | --- | --- |
| F37 反制存在，但来不及知道或拿不到〔敌情与供给边界〕 | Just King 有玩家抱怨初见即死，其他玩家列出防即死、生命、防甲、复活和治疗答案；My Party Is Grinding 的 Nightmare 反馈则指出元素抗性数值或供给不足、依赖闪避吸血。应区分不知道、没有准备窗口、答案不可达和确实选错。资料有分歧、缺发生率，不把投诉写成普遍结论。 | `ev-jk-014-boss-telegraph-disagreement`；`ev-mpig-012-nightmare-element-response-gap` |
| F38 敌人反制自动发动的能力，玩家无法临时停用〔内容与权限边界〕 | Kādomon 的 Gnocking 复制／利用玩家增益，自动增益队无法简单选择战中不发动；0.3.2 将复制改成一次并移除 Crown。它可以考验阵容适应，也可能让长期构筑直接失效；社区同时存在通关方案与多样性受限批评。应靠战前信息、可达替代或内容强度讨论，不能假设玩家临场停机。 | `ev-kado-006-gnocking-automatic-buff-counterexam` |
| F39 自动站位让单位送死，且反馈与权限不足〔自动行为边界〕 | Skull Horde 有玩家报告队员自行分散落单或挤进 AOE，同时难以区分单位、弹道和即将全灭；Neon Auto Party 一段历史试玩也报告后排遭自动绕入且找不到集火办法。它们是特定版本个案，不能证明普遍 AI 缺陷；但分析时必须分开战前选择的风险和玩家无法纠正的自动执行问题。 | `ev-skull-horde-018-ai-aoe-readability-failure`；`ev-neon-auto-party-012-ranged-backline-auto-failure` |
| F40 反制本来可行，连续触发却挤掉响应时间〔敌人节奏边界〕 | Vivid Knight v1.1.6 调低数个敌人技能过频的问题；攻略要求保存两次防护或治疗来接危险序列。区别于完全没带反制，玩家可能带了答案但连续触发不给使用窗口。具体旧／新频率未公布；本项目自动技能还要考虑其自然触发时机，不能直接套原作 Gem 操作。 | `ev-vivid-015-boss-trigger-frequency-window-rework` |
| F41 完整免疫让专精失效，混合能力则能通过〔敌人内容边界〕 | Backpack Dungeon 隐藏终点有分别免疫物理、魔法、状态的三个阶段；有反伤／打断玩家长时间疲劳后失败，也有混合多轴构筑通关。它确实考验覆盖，但可能要求已专精队伍补齐此前不需要的能力；提前信息与转型机会证据不完整，不能把一例通关当作人人可达，也不默认禁止所有免疫。 | `ev-bpd-010-hidden-boss-multiaxis-exam` |

**低置信与相邻类型线索：仍保留可见，不升级为规则证据**

| 编号／线索 | 实际支持与缺口 | 来源 |
| --- | --- | --- |
| FL01 完整敌方行动预告〔相邻类型线索〕 | Into the Breach 的发现层记录把敌方攻击预告与推离、拦截、击杀相连。可借鉴“看到后果再安排应对”的方向；本库只有发现层／商店来源，未深核完整精度与例外，不能把完整预演当成本项目已选功能。 | 发现层 e091／s060 |
| FL02 保护设施与杀敌是不同的成败轴〔地图模式线索〕 | Into the Breach 的发现层将保护建筑与消灭敌人并列：保住目标可能比追求击杀更重要。它会改变何谓局部成功，但需要专门目标与损失规则；本轮仍只是守点／目标候选的对照。 | 发现层 e092／s060 |
| FL03 无尽敌群持续变强，拖延最终耗尽生存〔模式线索〕 | Auto Legion 的发现层／商店信息描述持续增强的无尽敌群。区别于固定超时，是压力随波次增长；没有深档案、增长曲线或足够实战，不能证明其能妥善结束所有续航僵局。 | 发现层 e057／s035 |
| FL04 作用中状态必须能看见，文本必须对应实际〔历史反馈缺口〕 | Not Your Mama’s Autobattler 的 2022 早期讨论中，增伤 tome 的状态没在战斗 UI 显示，开发者承认反馈不足；另有减伤文本与复测不符并承诺修复。没有补丁正文闭合最终结果，不能说当前仍有缺陷；完整规则与构筑未达深证据门槛。 | [原档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/not-your-mamas-autobattler.md) |

所有 `ev-*` 回查[深证据库](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)，其 source_ids 对应[来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)；发现层 id 回查[发现证据](../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)。本轮 53 个深证据 id 与关联 source_ids 均已静态核对存在；证据存在不代表迁移结论或体验已验证。

### 本轮综合的范围

最接近当前主轴的是 F01 的承压机会成本，而非必须使用 F03–F06 的死亡构筑。F09–F17 说明“同样死一个单位”可能具有完全不同的长期代价，故 G01 不抢先决定恢复模型。F18–F22 展示防僵局的不同代价，不从“争取时间有价值”直接推出某个计时器。F23–F36 显示信息有不同粒度；总伤害、详细日志、精确预演各不能替代对因果的辨认。F37–F41 保留反例，防止把信息不足、无调整窗口或执行失控称为合理难度。迁移建议及本轮待决体验句见 [方案](proposals.md#q03讨论建议)。
