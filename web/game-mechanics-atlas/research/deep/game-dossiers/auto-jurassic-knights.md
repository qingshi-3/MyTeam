# Auto Jurassic Knights

## 身份、发行与时期

- `title_id`: `auto-jurassic-knights`
- Steam 主 App：`3882680`；开发 / 发行：Proelium Games；截至 2026-09-03 仍为 `Coming soon`，没有正式发行日期。
- Steam Demo App：`4038830`；官方关联主 App 3882680；2025-09-26 发行，当前仍可安装。itch.io 另有标为 alpha 的 2025-09-22 Windows 包。
- 公开版本链：2025-09 alpha / Steam Demo；2025-10-08 汇总 `v0.1.5–v0.1.7`；2025-11-11 汇总 `v0.1.8–v0.2.0`；2025-11-21 增加 34 种本地化。公共 Demo branch metadata 显示 2026-05-30 仍有更新，但没有公开 patch / 版本号映射，不能称其为某个已知版本。
- 主 App 与 Demo 商店均描述异步 PvP、逐回合招募 / 三合、前置编队、shop tier、class / species synergy、装备和 run-based 重开。Demo 当前有 20 条 Steam 评测；主 App 无评测。
- 最终结论：公开 Demo 与补丁足以证明许多规则和生命周期问题，但没有一条可公开核验的实践材料闭合完整 build、取得路线、pivot 与 counter。按门槛为 `insufficient-evidence`，不登记 deep source，也不生成 deep evidence。

## Adaptive-depth 判断

- **系统复杂度**：公开补丁覆盖商店、金币、三合、前后排、相对位置 trigger、jump-back、召唤、供应品 / 装备、异步队伍快照、匹配、难度缓冲、战斗优先级、draw guard 与 UI 归因，复杂度中高。
- **机制独特性**：恐龙、骑士和枪械主要是主题。更有研究价值的是前后排相对触发、jump-back 动态重定向、供应品加属性 / 装备给能力、召唤与同帧事件队列、异步队伍快照和 tier clock。
- **项目相关性**：formation selector、触发顺序、召唤占位、装备所有权、来源归因、反无限循环、敌人匹配与新手缓冲都高度相关。
- **资料密度**：官方规则 / patch 密度高；实践密度低。20 条评测中多数是短促主题 / 潜力评价，4 个讨论没有阵容，Steam Guide / Curator 为零；18 个公开视频只有 4 个自动字幕轨，正文请求为空，其余无字幕。
- **版本变化**：v0.1.5–v0.2.0 的修复跨度大，且 2026-05 公共 branch 又更新但无说明，必须把 2025 alpha、Demo 首发、v0.2 与未知当前 build 分开。
- **选择的深度**：保留高价值规则、触发顺序、生命周期与主题差异化分析；做最后一次 build gap matrix；不下载约 1–2 GB build、不安装游戏，也不从视频标题、缩略图或无字幕画面拼装阵容。
- **停止原因**：指南、评测、讨论、itch、YouTube、社媒、搜索与存档路线已达到递减收益。继续同类搜索只增加泛化观感或无字幕视频，不会补齐取得路线、完整 owner、pivot 和 counter。

## 可读但不登记的来源包

本标题没有满足 retained 门槛，以下页面只在排除 dossier 中保存审计，不进入 `deep/source-index.md`：

- Steam Demo 商店 / App Details：Demo 4038830 的发行、20 条评测、主 App 关联、产品规则与当前可安装状态。
- itch.io alpha 页面：公开 2025-09-22 下载、in-development 状态、半小时平均 session 和同一产品规则。
- itch `Patch V0.1.5!`：完整战斗 / UI / VFX / system 修复，且表明为 Steam Next Fest 准备。
- Steam `Patch Notes V0.1.5 & V0.1.6 & V0.1.7`：异步 PvP、事件优先级、召唤、target、formation、draw 与 team export。
- Steam `V0.1.8, V0.1.9, V0.2.0`：教程、merge / swap、召唤队列、装备触发、匹配、首三局难度缓冲与 stale-run 清理。
- Steam `Localization support for 34 languages!`：本地化范围，不提供新机制。
- Demo 20 条评测：只有一条繁中评测给出可辨认的路线族，一条 574 分钟评测提到会破坏平衡的组合，一条 11 分钟差评提到没有储备购买和敌方过强；均无可核验阵容。
- Steam 四个讨论：两条 Super Auto Pets 相似性讨论、一个流程停滞报告和一个美术讨论。开发者回复承认将为新 packs 走不同路线并制作单人 campaign，但没有规则计划或版本承诺。
- itch 页面唯一评论只评价早期无 SFX / VFX 到新版表现的变化，不含机制或构筑。

## 版本与模式边界

1. **2025-09 alpha / itch 包**：文件名为 `Auto Jurassic Knights 09-22-2025`，页面明确称 alpha with bugs。v0.1.5 itch devlog 与该包同日，不能假设它包含后续异步 PvP或 v0.2 教程。
2. **2025-09-26 Steam Demo 首发**：二十条评测从 9 月 27 日开始；首发玩家提到没有教程、单姿势和容易理解，不能用于描述 v0.2 的 tutorial / attack-pose 版本。
3. **v0.1.5–v0.1.7**：加入 / 修复 ability priority、召唤队列、draw guard、任意战斗阶段重组、match history、异步队伍上传和断网恢复。
4. **v0.1.8–v0.2.0**：增加教程、匹配简化、新手前三局按 round 降低敌方属性、merge / full-team swap 修复、召唤 health / attack UI、on-hit 链与装备触发修复。
5. **未知 2026 当前 Demo build**：公共 branch 在 2026-05-30 更新；没有公告说明版本或规则。2026-08 的“End Game 后下一轮不动”讨论只能标记为单一当前期故障报告，不能推导全体玩家状态。
6. **主 App / future campaign**：主 App 尚未发行。异步 PvP 是 Demo 规则；开发者 2025-10 说在做 single-player campaign，不能把未来 campaign 当已公开模式。

## 已确认的规则与所有权

### 商店、经济与升级

- 每轮用金币招募 knights / dinosaurs，滚动商店，能够 lock、sell；v0.2 教程强制一次 1-gold roll，并在 lock 教学前让玩家保持 10 gold。这是 onboarding 样例，不足以推出常规回合收入或利息。
- 三个同类单位合并为更强版本；shop tier 随更高 round 解锁更强单位 / synergy。没有公开 tier odds、单位池共享、售出返还、合并属性继承或最大等级。
- `Quartermaster Drell` 在出售时给属性；`Bounty Hunter K` 有六击杀计数；这些证明买卖 / 战斗计数可成为成长 trigger，但没有实践路线说明它们如何形成经济 build。
- 一个 11 分钟差评说不能购买单位放入 storage。它支持“储备缺失是玩家感知问题”，但不是完整 bench 规则，不能推断队伍容量与所有购买限制。

### 队形、目标与事件顺序

- 文本明确存在 frontline、backline、ahead、behind；玩家能在每个 battle stage 重组。满队交换两个相同单位曾失败，v0.2 后允许无空位 swap。
- 前排单位可以 jump back；近战 / 远程距离、hitbox、tooltip、投射物、mortar 和 target 坐标必须在位移后重算。`Mordar` 的 mortar 与 rifle / sniper 都曾命中旧位置。
- `Commando Carno` 的额外攻击应触发“when ally ahead attacks”与经 on-hit intermediary 到 `Echo Caller Vi` 的链。它证明 origin、intermediate、recipient 与相对位置各自有 owner，但没有公开完整编队。
- `Quarter Witch Briar` 被击时先触发 On Hurt 再 push；`Paleclaw Finisher` 的 battle-start 优先于其他效果；系统后来支持带编号的 ability priority tag。
- `Bomber Fern` 的前排目标死亡后，剩余伤害转给 next unit；`Finisher` 多弹不是同帧堆到同一目标，而要随时间分配。这些是有类型的 target revalidation / multihit 规则。

### 供应品、装备与状态

- v0.2 教程把 `Supplies give stats` 与 `Equipment give abilities` 合并讲解；装备面板统一写 `Equip Ally`。因此 supply 与 equipment 是不同功能层，效果仍落在具体 ally。
- 多目标 supply 对 `Nyra` 每回合只触发一次，而不是每个受影响单位一次；多个 supply buff 与 ally buff 同时应用曾出现 stack 不一致。
- `Steel Helmet` 应在合法 trigger 后爆炸并消耗；Burn 曾错误触发它，v0.2 后排除。`Doomshell` 也必须在正确位置 / trigger 爆炸并清理。
- `Sister Bastion Veil` 在非 frontline 时也可对自身生效，并给予包含 Burn / Scorch 在内的完整免疫。公开材料没有说明状态列表、持续、stack、dispel 或穿透规则。

### 召唤、死亡与终止

- ability summon 先于 distress-gun summon；`Skullcradle Remnant` 是官方例子。大量同帧召唤曾覆盖最后一个 frontline slot、重叠、停止移动或 soft-lock。
- v0.1.7 与 v0.2 分别加强 spawn queue、同帧 death + summon 的 placement / movement，并给 summons 显示 Health / Attack。召唤来源、身体、占位和可视属性是不同事实。
- `Witch Briar` 的 faint / summon 链曾被 pushback 破坏；`Flare Marshall Wyatt` 后更名 / 引用修正为 Watson，并有 Holmes summon token。
- 早期无限战斗通过内部 turn timer / draw condition 终止。没有公布回合数、判定先后、draw 对 run Health / reward / matchmaking 的影响。

### 异步对手与难度缓冲

- v0.1.6 把玩家 team data 上传服务器，再从已上传队伍抽取异步对手；不完整 / 过时 / 空队伍会被过滤，end-turn 断网不再冻结。
- v0.2 优先同 turn、同 Health 对手，找不到时退到 Health -1。它是匹配规则，不代表两队强度相等，也没有公开候选池 / 重复保护。
- 玩家头三次 completed runs 在 rounds 1–4 / 5–8 / 9–12 分别获得敌队属性 -40% / -30% / -20%，13+ 不减。这是显式新手保护，不能当正常平衡表。
- 旧 run 在首次启动新 patch 时会自动删除，以避免不兼容状态阻塞。它解决迁移，但也显示持久队伍 / schema 仍不稳定。

## 实践来源 gap matrix

| 闭环字段 | 当前材料 | 结论 |
| --- | --- | --- |
| engine | 补丁给出 Carno / Echo、召唤、sale / kill trigger | 能证明组件互动，不能证明玩家实际构筑 |
| state / resource | Gold、round、tier、Health、Attack、kill count、Burn / Scorch | 缺少完整规则、成本与 build 状态 |
| payoff | 多段攻击、summon、开场爆炸 / 死亡反伤路线族 | 独立评测不提供具名 payoff owner |
| survival | Veil immunity、Health buffs、front/back | 没有实践阵容的生存层与失败对局 |
| space | frontline / backline / ahead / behind / jump-back | 可证规则；没有固定编队与换位决策 |
| owner | unit、supply、equipment、summon source 可区分 | 完整 build 的 owner 链仍缺 |
| economy / pivot | buy / sell / roll / lock / triple / tier | 没有 round-by-round 取得、桥接或转型 |
| counter | 没有具名 enemy package、bad matchup 或换件答案 | 完全未闭合 |
| version | v0.1.5–v0.2.0 有说明，当前 2026 build 未映射 | 历史规则不能假定为当前平衡 |

## 为什么不能从补丁拼装构筑

- `Commando Carno → on-hit intermediary → Echo Caller Vi` 是开发者修复的触发路径，不等于玩家选择的完整队伍；没有单位等级、商店 tier、设备、survival、其他槽位、取得顺序或 counter。
- `Skullcradle Remnant`、distress gun、Witch Briar、Watson / Holmes 能证明多类召唤与占位压力，但没有一条实践源列出谁供应、谁受益、何时 pivot 或何种敌人克制。
- 繁中评测把强路线概括为“堆数值”“开场乱炸”“死亡反伤”。这可用于记录玩家感知的路线收敛，但没有单位 / 装备名，不能用 Paleclaw、Steel Helmet 或任一 death unit 擅自填空。
- `Crooked Loinlords`、`Summon Frenzy` 只是公开视频标题；页面无构筑正文。自动字幕轨请求返回 HTTP 200 空正文，其余视频无字幕，因此不能把标题、缩略图或无字幕画面当作具名 build。
- 574 分钟评测只说有可能 break the game 的 combo，579 分钟评测只说潜在 broken combos；游戏时长不能补足缺失的组件和反制。

## 失败、生命周期与主题差异化

以下都是 dossier 中的生命周期候选，不计入 corpus 的 115 个 negative / reworked cases，因为本标题没有达到 retained source package：

1. 召唤同帧 overlap、覆盖最后 frontline slot 和 soft-lock，后来加入 spawn queue / placement revalidation。
2. 无限战斗需要内部 turn timer / draw guard。
3. jump-back 后投射物、mortar、hitbox 和 tooltip 指向旧位置，需要统一重算。
4. 同帧 buff stacking、多目标 supply 与 extra-attack trigger 重复 / 漏触发，暴露 event chain ownership 风险。
5. Steel Helmet 被 Burn 错触发或未正确消耗，说明装备 trigger、状态 damage 与 transaction cleanup 必须分型。
6. v0.2 首三局分段降低敌人属性并重做匹配，显示初始难度 / 异步候选曾需要显式缓冲。
7. stale run 首次启动自动删除，显示 patch 与持久状态缺少兼容迁移。
8. 2026-08 单一玩家报告 End Game 后下一轮不动 / 无 movement；没有版本与复现，不能升级为普遍当前故障。

题材没有自动变成系统差异。两条社区讨论和若干评测把它描述为 Super Auto Pets 重皮；开发者回复承认相似，并说新 packs 会走不同路线、另做 single-player campaign。该回复只能证明团队意识到差异化问题，不能证明后来已经完成独特机制。当前可读的前后排、jump-back、equipment ability、异步快照与 tier clock 才是候选差异轴；class / species synergy 没有阈值 / 名单，恐龙 / 骑士 / 枪仍主要是内容与视觉包装。

## 检索日志

访问日期统一为 2026-09-03。

### Steam、itch 与官网

- 读取主 App / Demo App 多语言 App Details、商店、Store Search、Demo review API、三个主 App News 节点和 public app metadata。
- Demo Community 路由重定向到 Store；主 App Guides / Curator 为零，Discussions 共四个，Reviews 为零。Demo 有 20 条评测，已逐条检查；只有一条路线族评论，没有完整 build。
- itch alpha 页面、两个 devlog、RSS 和唯一评论均已读取。v0.1.5 itch 与 Steam patch 重复，不能作为独立规则视角。
- `jurassic-knights.com` 当前只公开 access-code / subscribe 门，sitemap 返回 503；没有尝试输入访问码、注册或绕过。公开受限站点不补足规则。

### 视频、社媒与外部实践

- YouTube 精确标题、gameplay、build、strategy、tier list、Maxim 和具名单位搜索共识别约 18 个相关视频，其中有官方 alpha / Demo / gameplay、独立 13–72 分钟实战和德语 / 波兰语评述。
- 仅四个长视频有自动字幕轨；timedtext 返回 HTTP 200 空正文，公开 transcript endpoint 返回 precondition failure。其余无字幕。没有绕过 PO-token / 登录条件，也没有从无字幕画面推断数值或阵容。
- 视频描述大多重复商店文案；官方 Demo Gameplay 只补充“有限金币、push-your-luck、all-in quick power 或 carefully evolving”的产品目标，不提供完整 build。
- Steam 页面列出 X、Instagram、Facebook、YouTube 与 Discord；当前公开网页 / 视频没有规则文档或实践构筑。Discord 不作为私域补证路线。
- Reddit 公共 JSON 被阻断；普通搜索对精确标题、App ID、开发者、具名单位、build / guide / strategy 只返回 Steam、itch、YouTube、聚合页或同词噪声。未找到 wiki、GameFAQs、文字攻略、统计、press analysis 或独立论坛构筑。
- 没有下载 / 安装 Steam Demo 或 965 MB itch alpha。任务不要求直接试玩，也不通过数据挖掘或资源解包代替公开实践证据。

## 对本项目的研究价值与限制

可保留为以后设计讨论的规则观察：

- 相对位置 selector 不只影响数值：ahead attack、behind、front / back 与 jump-back 会改变 trigger、target、hitbox 和弹道。
- 同帧事件需要 typed priority、root source、target revalidation、one-per-turn guard、spawn queue、slot reservation 与 draw controller。
- Supplies 加属性、Equipment 给能力可以把数值成长和规则改写分开；二者仍需显示 holder、trigger、duration、consumption 与转移。
- 异步 opponent snapshot 需要版本、完整性、队伍来源、匹配范围和 stale-data 过滤，不应把相同 Health / turn 当成等强度证明。
- 新手缓冲应与常规数值分层记录，避免玩家在脱离前三局后误读难度突变。
- 主题组合必须映射到新的决策轴才算差异化；把 SAP 单位能力换成恐龙 / 骑士名称不会自动产生新体系。

当前不能作为设计依据：任何单位 / 装备清单、完整召唤队、开场爆炸队、死亡反伤队、三合概率、shop tier 表、synergy 阈值、队伍 / bench 容量、经济曲线、异步匹配质量、当前 v0.2 强度或单人 campaign 规则。

## 复查条件与未决问题

- 主 App 正式发行，或 Steam Demo 出现新的公开 patch / version mapping 时重查。
- 优先检查是否出现 Guide、完整文字 review、公开 build sheet、可读视频字幕 / transcript 或开发者 bestiary / item database。
- 保留门槛不变：至少一条实质规则源与一条独立实践源，并闭合一套具名、版本化 build 的 engine、state、payoff、survival、space、owner、economy、pivot 和 counter。
- 复查当前 Demo branch 与 v0.2 的对应关系、团队 / bench 容量、tier odds、三合继承、supply / equipment 获取与转移、class / species synergy、draw / loss 结算、对手预览和 single-player campaign 状态。
- 若仍只有补丁交互、泛化评测与无字幕视频，保持 `insufficient-evidence`；不要把更多 unit names 当作构筑深度。

## Disposition

`insufficient-evidence`

本作不是低信息未发行样品：公开 Demo、三批公告、20 条评测和多个长实战使身份、版本、formation、trigger、summon、equipment 与异步匹配规则相当丰富。但深研究门槛要求的不只是“知道很多单位互动”，还要求一套玩家实际使用、能复核取得 / pivot / counter 的完整构筑。当前实践材料只给出“堆数值、开场乱炸、死亡反伤”、无 storage、难度 / 卡死和 SAP 相似性等零散观察；补丁里的 Carno / Echo 或召唤名字不能替玩家填空。因此本 checkpoint 保留高价值规则和生命周期审计，但不登记 deep source、evidence 或 negative 计数。
