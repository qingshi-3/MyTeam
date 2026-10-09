# R12 敌情预告与准备：已有证据

讨论材料，不是现行权威。进程见 [总览](../../../roadmap.md)，完整机制与取舍见 [方案讨论](proposals.md)。2026-09-28的Q02包含新联网核查；后文Q01的“未联网”仅指2026-09-09历史研究。

## R12-Q03：纯自动战斗的项目适配核查（2026-09-28）

本轮不再扩张原作清单，复用Q02／Q01证据并回读项目基线；重新打开GvH策略指南与The Bazaar 13.0补丁存档，可读内容继续支持多路径自动应对／战前配装信息。未重新审计原作现版本平衡、完整GGM／PMM资料或运行游戏。AP01–AP10均是[项目方案](proposals.md)的推演标识，不是原作机制编号。

- 用户本轮再次明确纯自走棋、无战中抓手；与[G02-D01](../../../01-global-boundaries/player-agency/decisions.md)一致。Alpha权威中的历史战术指令不能覆盖当前讨论边界，普通暂停与变速只用于观察。
- [核心规则](../../../../gameplay-design/tower-autobattler-core.md)记录实际敵阵／空间、EE07锁向喷吐、EE09最远合法目标换位、EB01可自动攻击拆墙及EB02有限卵／幼虫。它们证明内容提供了相关行为，不证明任何建议的摆法／功能替换已有效；移动中目标与队形变化须另验证。
- [成长活动任务](../../../../work-items/archive/growth-and-composition-design.md)与[运行包](../../../04-content-validation/artifacts/growth-route/README.md)确认26名代表内容、永久成长、研究库存及开战法术已交付；不能推成全部49名单位、任意对策法术或同等成本替补均已具备。
- 本轮读取[GrowthRunService](../../../../src/Growth/GrowthRunService.cs)的`CraftSpell`、`EquipSpell`、`BeginBattle`与`Freeze`，以及[GameFlowCoordinator](../../../../src/App/GameFlowCoordinator.cs)的`StartBattle`：战前选择法术及已部署目标，实际开战冻结参与者并消费库存；非战斗走独立`FreezeNonCombat`。尚未使用的本节点产出不因此提前变成本场战力。
- [护阵资源](../../../../content/growth/abilities/spell_guard_formation.tres)为目标与2.5格内最多2名友军各给目标本场生命上限15%的18秒护盾；[蓄势资源](../../../../content/growth/abilities/spell_gather_momentum.tres)为目标开战加25法力。[战斗适配](../../../../src/Run/RunBattlePreparationAdapter.cs)在预览读预设、正式战斗读已消费快照。以上是静态规则核查，无本轮实际输入或观感验证，也不能证明能可靠打断特定敌人。
- 回读[R04](../../roster-and-growth/reserves/decisions.md)、[R10](../route-progression/decisions.md)、[R11-D02](../attrition-and-recovery/decisions.md)、[R13-D01](../risk-and-reward/decisions.md)：战前轮换方向保留；外层具体形式仍开放；普通／精英失败以全局生命容错、Boss失败终局；更难遭遇与自选单场条件已有一般方向，具体选敌菜单和条件内容仍待定。R13旧文的“一律败战终局”已由R11-D02后续决定取代，本轮不复活旧基础。

## R12-Q02：敌人考验、玩家投入与透题的成熟经验（2026-09-28）

### 问题与证据边界

用户强调D64／R12-D02只是玩家想法，要求在决定如何设计前研究成熟经验。两条已纠正为待验证假设；不以“抓手优先”或“准备越长越早透题”为必须得到的结论。核对实际信息、决策窗口、应对路径、代价及不兼容案例，区分开发者规则／设计意图、玩家实践和本项目推论。

复用Q01对780条深记录、106条发现记录及56份档案的筛查去向，并重新对两个JSON全库检索。深库在规则／实践／机制／引擎／反制／失败／限制字段中，将`preview|scout|telegraph|intent|forecast|预告|预览|侦察|意图|敌情`与`counter|adapt|prepar|position|resist|反制|克制|准备|站位|应对`交叉，初筛54条。发现层字段结构不同，改为全部字段值的信息关键词筛查，命中15条；两层命中不能相加为证据强度。回读相关记录及source-index定位来源，没有重新逐篇精读全部56份档案，也未改研究库。

联网查询覆盖开发者设计访谈／GDC、官方历史公告、已发布作品规则资料和相关玩家反馈：关键词包括`Slay the Spire intent Giovannetti design`、`Monster Train Friends and Foes preview bosses`、`Into the Breach final boss design`、`TFT opponent matchup tracker`、`Gods vs Horrors Strategy Guide`、`Backpack Hero enemy rework player agency`、`Darkest Dungeon scouting`及`Mechabellum counter anticipate`。下列主要结论聚焦稳定的已发布／历史机制，不混用尖塔1与2、怪物火车1与2，也不核定2026当前赛季强度。

### 来源索引

| 来源 | 实际核查与版本 | 能证明什么／限制 |
| --- | --- | --- |
| Q02-S01：[Mega Crit双创始人War Stories访谈](https://arstechnica.com/video/watch/war-stories-slay-the-spire-war-stories/?c=series) | 2019-05-02；本次检索返回完整相关访谈转写，直接open报错 | 初版Next Move的多敌人查看负担及意图图标改造；作者访谈，一手。不是意图系统提升胜率的统计 |
| Q02-S02：[Anthony Giovannetti访谈](https://hopeinsource.com/games/) | 2019历史访谈；本次打开并定位20:31～22:06及风险讨论 | 为即将面对的精英选择当下伤害牌、卡牌价值依上下文改变；不确定精确供给成功率 |
| Q02-S03：[Monster Train发行方Friends and Foes公告视频说明](https://www.youtube.com/watch?v=asbaBDnQ1pA) | Good Shepherd，2020-09-03；本次检索读取说明，视频open报错，未观看 | 明确新增开局预览第3／6环Boss；不从公告外推全部字段或所有波次公开。另见[历史版本记录](https://monster-train.fandom.com/wiki/Version_History) |
| Q02-S04：[Monster Train战斗规则](https://monster-train.fandom.com/wiki/Battle) | 本次检索可读，Fandom正文受robots限制；与已有`ev-monster-train-004`对照 | 战前主要敌人／Boss预览、部分普通敌人未展示、次要Boss战可选Trial换奖励；维护wiki，非开发者效果评估 |
| Q02-S05：[Into the Breach开发者GDC复盘](https://media.gdcvault.com/gdc2019/presentations/Into%20the%20Breach%20Postmortem%20Final.pdf) | 2019，49页，读取文本，重点第13／20／21／46页 | 玩家回合内确定性、公开攻击、非致命操作与难度超过可解阈值；未将全文当作自动战斗规则 |
| Q02-S06：[Subset双创始人访谈](https://www.gamedeveloper.com/business/how-subset-games-made-the-jump-from-i-ftl-i-to-i-into-the-breach-i-) | 2018-06-04；本次检索完整相关访谈，open间歇报错 | 终战为何不强制杀高血量Boss、结局高潮的代价，以及持续试玩／新手理解检查；一手访谈 |
| Q02-S07：[TFT设计支柱](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-design-pillars-of-tft/)及[12.4补丁](https://teamfighttactics.leagueoflegends.com/en-au/news/game-updates/teamfight-tactics-patch-12-4-notes/) | 官方2019设计稿与2022-02-15补丁，正文已读 | 观察对手影响招募及反制；潜在对手标记减少记忆负担。历史制度，不以其说明当前匹配算法 |
| Q02-S08：[Gods vs Horrors策略指南](https://steamcommunity.com/sharedfiles/filedetails/?id=3493071391) | uri.32／开发者与社区共创，2025-06-04，1.0；本次正文完整可读 | 开局已知终Boss、具体顺劈／毒／后排反制及跨神系借用；是攻略实践，不是供给与通关率证明 |
| Q02-S09：[背包英雄2023-09-16官方公告存档](https://steamdb.info/patchnotes/12202611/) | 已读SteamDB保存的开发者原公告；[Steam原址](https://store.steampowered.com/news/app/1970580/view/5561438382465842605)此次仅图片壳 | 更清楚意图、可打断攻击、移除敌方诅咒并改自选取得。补丁意图不能证明所有新内容已平衡 |
| Q02-S10：[Darkest Dungeon侦察规则](https://darkestdungeon.wiki.gg/wiki/Scouting)及[远征规则](https://darkestdungeon.wiki.gg/wiki/Expedition) | 本次检索可读维护wiki摘录，未核完整游戏客户端 | 入房间侦察邻接区域、饰品／光照／扎营等影响侦察，发现战斗后在路径允许时绕行；不保证具体敌阵全公开 |
| Q02-S11：[Mechabellum官方上线期说明](https://store.steampowered.com/news/posts/?enddate=1683817713&feed=steam_community_announcements) | 2023-05上线期官方文字，检索段落可读，过滤app后的open报错 | 看上一轮敌阵，招募／升级并预测下一轮；不是对手本轮所有新投入提前公开 |
| Q02-S12：[尖塔1地图生成](https://slaythespire.wiki.gg/wiki/Map_Generation)、[地图节点](https://slay-the-spire.fandom.com/wiki/Map_locations)、[精英规则](https://slay-the-spire.fandom.com/wiki/Elites) | 本次检索维护wiki，地图展示Boss图像、普通／精英类别；正文访问部分受限 | 不同信息粒度共存；不将老玩家认识图像等同新手自动知道完整规则 |
| Q02-S13：[The Bazaar怪物规则](https://thebazaar.wiki.gg/wiki/Monsters) | 本次检索可读完整相关规则，直接open为403；维护wiki | PvE怪物三选一，奖励含金币、经验及怪物物品／技能；不等于PvP对手可选，也不保证某件指定掉落 |
| Q02-S14：[The Bazaar 13.0公告存档](https://steamdb.info/patchnotes/22600660/)、[13.3修复](https://steamdb.info/patchnotes/22768103/)与[历史玩家比较](https://www.reddit.com/r/PlayTheBazaar/comments/1k4gslo/when_do_you_choose_not_to_take_the_rightmost/) | 2026-04-01公告含愚人节段与Real Version段，采用后者的右键查看怪物配装；4月13日修复再次涉及怪物检查。原Steam公告只返回图片壳；13.0检索可读，13.3与讨论正文已读 | 已发布版本补出战前配装查看，不能把2025年的[预览缺失反馈](https://steamcommunity.com/app/1617400/discussions/0/591780546279945440/)当作当前状态。历史玩家有优先稳定经验与追特定技能掉落的不同策略，只作为个案，不核当前最优选择 |
| Q02-S15：[Into the Breach任务规则](https://intothebreach.fandom.com/wiki/Missions) | 本次检索读取任务／奖励／策略段，维护wiki | 可选任务的目标与收益不同，低伤害队可能回避击杀数目标；岛内进程有限，不能推成每个敌人或全部未来波次均可挑选 |

用户续提“增加抓手，比如对人可选”，本轮按“对手可选”理解，尚非采用决定。补查`The Bazaar monsters choose / monster preview / monster inspection`、`Into the Breach choose island missions`和`Gladiator Guild Manager choose battles`，新增EP11／EP12。GGM商店的“choose gladiators”指选择己方单位，不能据此证明对手可选；2021年的Bazaar官方事件页主要嵌入视频，未看视频，不拿它证明已发布版本规则。

### EP01：公开下一动作，搭配即时改变结果的权限

原作事实：Into the Breach在玩家回合展示敌方攻击，攻击无命中／未命中随机，位移等非致命动作能改变后果（S05）。尖塔访谈以敌人强化时进攻、重击时防御说明意图如何改变当回合选择（S01）。玩家花的是本回合行动、能量与位置等资源。

项目推论：公开意图的价值依赖还能改变哪些结果；自动战斗若无战中介入，不能把同样的提示当作同样的解题权限。它可以服务战前理解或战后归因，但精确瞬时反应需另有抓手。代价是战术解题负担；S05还指出可解性越界后难度会陡变，不能靠完整信息保证总有解。

### EP02：关键远期考验早公开，影响沿途投入

原作事实：怪物火车1在Friends and Foes中加入开局第3／6环Boss预览（S03）；GvH指南明确从开局知道终Boss，建议据此发展阵容（S08）。这能在招募、升级和配套取得之前提供目标，不需每场战斗完全公开。

项目推论：支持重要构筑考验早告知的方向，但没有推导出统一提前层数。知道目标仍不等于抽到所需内容；本次不把GvH攻略的跨神系借用建议当作供给保证。既有个案`src-gvh-review-pivot-lock-221382512`报告缺件／转型困难，本次复查失败，只保留旧证据及个案性质，不冒称当前平衡定论。

### EP03：保留具体遭遇的不确定性，准备一类问题

原作事实：尖塔1地图区分普通／精英节点，Boss使用身份图像；不是逐个节点提前给出完整敌阵（S12）。Giovannetti解释为眼前精英优先拿伤害牌，即使较晚更想要另一张牌（S02）。准备会改变主构筑，但并不必须提前知道这一场的完整流程。

项目推论：成熟作品也通过部分未知考验覆盖面、资源余量及风险选择。不能把“存在未知”直接判为不公平；反过来，也不能用老玩家学会敌池证明新玩家已得到充分引导。有限牌池知识与本项目的教学／重复遭遇节奏要另评估。

### EP04：自动战斗用多条不同成本路径处理同一威胁

原作实践：GvH指南对顺劈建议弱单位承受邻位伤害、调整嘲讽位置；对毒列出顺劈、先手、护盾、小单位及直接伤害；对右侧威胁提供早杀来源或增加右侧承压两类办法（S08）。这些路径横跨部署、局部换人、成长及跨体系配套，并非逐Boss指定一个羁绊。

项目推论：这比只列“存在反制”更接近本项目；需要比较实际投入、取得与对主阵容的影响。攻略证明有这些打法，不证明它们同价、所有体系均可自然获得或整局胜率相同。该作是相近的已发布小型自动战斗参考，不能单独代表业界共识。

### EP05：给出可推断范围，保留对手下一步的不确定性

原作事实：TFT的12.4官方补丁标记潜在下一对手，并说明降低玩家记忆已交战对象的负担；设计支柱把观察对手与招募／反制相连（S07）。Mechabellum上线说明要求根据上一轮已见阵容新增单位／升级，同时预测下一轮投入（S11）。这是主动侦察与预测，不是敌情完全锁定。

项目推论：信息可以提供概率范围而不是唯一答案，玩家保留兼容多种局面的配置。代价是错误预测和持续侦察负担；PvP对手的策略互动并不能直接证明单机隐藏关键规则同样有价值。

### EP06：情报本身也是可投资的资源

原作事实：暗黑地牢1的侦察揭示邻接房间／走廊的战斗、陷阱等；饰品、光照、扎营技能等会影响侦察机会，已侦察战斗在路线允许时可绕过（S10）。玩家为信息付出装备或远征资源等机会成本。

项目推论：这是与免费全面预告不同的成熟做法；适用的是未知风险与资源管理本身构成乐趣的结构。可借鉴程度待定，不能为本项目强制机制增加“买提示”的税，也不能把侦察到战斗位置等同完整敌方技能公开。

### EP07：公开附加难题与回报，让玩家决定是否承担

原作事实：怪物火车1在战前预览主要敌人，次要Boss战允许开启Trial，增加挑战并换取奖励；部分普通敌人不在预览中（S04）。玩家先评估当前能力，再接受额外压力，接受与否本身是抓手。

项目推论：对于额外难度，可由是否参加承接阵容差异，未必要让每套阵容都事先补齐答案。它不等于普通主线可以任意硬卡；拒绝会放弃收益，若收益成为正常成长必需，该选择仍须重新评估。未将本项目任何新挑战制度视为已采用。

### EP08：保留机制特色，把破坏性惩罚从强制改为自选

原作事实：背包英雄2023敌人重做在增加意图／打断机会的同时，移除敌方诅咒攻击，把诅咒物品留给宝箱／NPC的自选风险（S09）。这是改压力来源，不能简化成“把诅咒写得更清楚”。

项目推论：若机制反复破坏已完成的构筑，可以改变其出现条件或风险归属，而不只是追加反制道具。公告给出明确改动与设计意图，没有提供改动后的体验或胜率统计，不能宣称问题已在所有内容中解决。

### EP09：终点检验已有能力，必要时删除不相容考题

原作作者说明：Subset解释Into the Breach没有采用必须击杀高血量最终怪物的要求，因为此前允许靠冻结／防守完成任务，终点突然改考杀伤会否定这些队伍；他们选择用熟悉机制和多阶段场景完成收束，也承认部分玩家觉得结局不够高潮（S06）。

项目推论：难题与已支持玩法冲突时，透题不是唯一修补方式。提前宣告一个必须放弃原玩法的结局仍有代价；需要评价战斗身份与终点考核是否相容。它不证明所有游戏都应取消Boss或保证所有偏科阵容通关。

### EP10：公开不等于可用，减少读取与记忆负担

原作作者说明：尖塔原Next Move需反复选中敌人阅读，多敌战斗难以形成整体判断，后改为常驻意图表达（S01）；TFT添加潜在对手标记也明确针对记忆成本（S07）。二者没有取消判断，而是让判断所需事实更容易取得。

项目推论：应验证玩家是否能发现、理解提示并用它作决定；仅在详情里存在长说明不够。不能据此要求所有数值同时铺满界面。作为补充，既有Neon Auto Party 0.5.2官方记录也因盲记Fate效果而增加预览，但属开发期案例，本轮未复核改动效果，不列为成熟成功证明。

### EP11：选择具体对手，让对局判断参与成长取舍

原作事实：The Bazaar的常规PvE怪物遭遇给出三个怪物供选择，获胜可取得金币、经验及怪物的物品／技能（S13）。13.0真实补丁段新增战前右键查看怪物配装，13.3继续修复该功能（S14）。选择既决定承受的机制，也决定奖励机会，不能与只选“普通／精英”混同；更不能外推PvP可选对手。

历史玩家实践：S14讨论中，有玩家倾向稳定经验，有玩家愿为适合构筑的技能掉落选较低档怪物。两者展示风险、成长节奏与掉落适配的竞争，也提示奖励过度集中可能压扁选择。2025年另有缺少预览、依赖站外查询的抱怨；需与2026补入配装检查分期记录，不能宣称当前仍无预览。未实测最新客户端、掉落概率或现版本候选平衡。

项目推论：玩家理解敌人后，可以通过挑选对局保护主构筑，而不必每次拆队。这仍然让敌人影响选择；但若能永久无代价避开全部短板，也可能退化为只吃优势对局。应比较选择前可见信息、放弃的收益、未来路线约束与必经考验，不自动规定每场三选一或敌人刷新。

### EP12：选择任务目标与战场，改变要解决的问题

原作规则及攻略实践：Into the Breach的地区任务包含不同额外目标和奖励，任务指南建议按队伍能力选择，例如低输出队较难完成击杀数目标（S15）。玩家选择的不仅是敌人，还可能包括保护对象、地形与任务条件；可选地区受岛内进程约束。

项目推论：与EP11挑具体对手、EP03选择未知遭遇类别、EP07开启附加难度分别保留。它允许队伍通过选择适合自己的目标发挥，但任务选择不能自动补成无限绕行或整条路线重抽。迁入本项目须先讨论胜负目标、路线承诺与奖励，尚未授权新增护送／守点规则。

### 扩查去向、反例与未完成部分

- Q01的PV04完整序列、PV05侦察工具、PV08模拟试打仍保留：SAP固定每日敌队不能证明提前全公开；Auto Battleships只见开发承诺；PMM可模拟后调整的既有证据有版本／次数限制。本轮没有找到足以把它们统一评为本项目成熟默认方案的新证据，分别留在原条目，不静默删除。
- Dungeon 100的Boss卡组预览属于EP02更细信息粒度；Gladiator Guild Manager的赛前敌方装备检查与阵容选择属于EP04；已沿旧证据保留，未逐个刷新所有规则。其余同词命中涉及合成、单位稀有度、显示修复等，不能据命中数充当相关设计证据。
- 没有实测原作，也没有统计各游戏的敌人比例、预告收益、自然反制获取率或胜率。开发者自述、发布说明、攻略与单人负评分别使用，不能由存在机制推导已经被证明最优。
- 本轮新增网络失败包括Steam部分公告／评论／评测页面、Fandom正文robots限制及若干直接open报错；已用可读的官方检索段落／公告存档补齐相应事实，并在来源中写明。没有把打不开的页面补成已读。
- 研究所得的本项目候选与待决点统一放在[Q02方案讨论](proposals.md)，D64／R12-D02继续未采用。没有修改玩法权威、代码、资源或运行游戏。

后续确认：2026-09-09 用户采用 PV02＋PV03 作为初期基础，见 [R12-D01](decisions.md)。下文保留研究时的原作证据、项目候选与实现基线；此次确认未新增调研，也不改变原作资料的缺口。

## R12-Q01：会改变准备方向的敌情，应在什么选择之前公开到哪一步

日期：2026-09-09。聚焦战前信息能支持的决定：已有资产调整、招募／培养投入及选择挑战。普通战前看敌阵已有基线，本题细化信息内容、提前量与不确定性；不合并战报、日志、局外图鉴、反制内容设计或失败续关。

### 前题收束与现行边界

[R11-D01](../attrition-and-recovery/decisions.md)已闭合普通胜后生命与倒地资格；法力、召唤有现行基线，具体持续效果归 M04／M05／M06，休息用途归 R10／R09。无需为完整度重选败即终局，R11 可基础阶段收束；实现差异仍沿 I08 等待统一整合。

- [核心权威](../../../../gameplay-design/tower-autobattler-core.md)第 45、82、86 行已有敌方组成、楼层规则、实际地形及敌人起点预览；第 94 行要求 Boss 提前提供能帮助准备的机制信息，但提前多久、精度与调整窗口未定。
- [G03-D01](../../../01-global-boundaries/run-rhythm/decisions.md)已允许不限时调整已有装备、站位与上阵／后备；[G02-D01](../../../01-global-boundaries/player-agency/decisions.md)已定初期无战中介入。不能靠战中提示补出临场换人或手动解控。
- [M04-D02](../../../02-foundation-models/attributes-and-statuses/decisions.md)与 I19 要求敌方控制、预告和可到达反制一起考虑。公开机制不等于保证掉落某件唯一答案；也不能用存在一件稀有反制就证明准备机会足够。
- R10 外层形式仍在并行开放讨论。信息提前量可按“选择挑战前／相关补强机会前／最终部署时”描述，不先规定几层或固定分叉地图。

**只读代码定位**：`src/UI/TowerScreenController.cs:38` 与 `src/Run/TowerGenerator.cs:13` 的路线卡主要提供标题、描述、风险；`src/App/GameFlowCoordinator.cs:270` 选择节点后，才在后续流程取得遭遇并显示部署。`src/UI/DeploymentScreenController.cs:112`、`:257` 与 `src/UI/EnemyDeploymentPreview.cs:27` 提供敌名／数量、起点、职责、近远程、射程和 Boss 标志；`src/UI/DeploymentBoard.cs:84`、`:104` 显示危险／目标／阻挡等地形预览。由此只能说当前这条路径的详细敌情主要支持已选节点后的整备，未见完整技能、属性、Boss 阶段与提前多节点公开契约；不宣称全仓所有入口均已审计，也不把 Battle Lab 当作征程试打权限。未运行或验证界面体验。

### 检索范围与判据

对 [780 条深记录](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[106 条发现记录](../../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)和 56 份深档案检索，回查 [深层来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)。JSON 搜字段值而非字段名；四核心字段是 `rule_support / practical_support / mechanism / engine`。发现层缺部分核心字段，命中数不能与深层质量直接相比。

- 首筛：`preview|scout|forecast|predict|telegraph|intent|预告|预览|侦察|预测|敌情|意图|情报`。深记录全字段 216／核心 48，发现层 15／核心 2；档案 49 份／156 行命中。
- 独立扩查核心字段：`inspect|visible|visibility|hidden|information|known|reveal|fog|intelligence|formation|查看|可见|公开|隐藏|已知|揭示|迷雾|阵型|提示` 与 `enemy|enemies|opponent|boss|wave|敌|对手|波次|关底` 交叉。深 52，发现 3。
- 模拟／知识扩查：`simulat|rehears|test.?fight|bestiary|encyclop|codex|monster.?book|observe.{0,30}repeat|模拟|试打|试战|演练|图鉴|敌人手册`。核心深 11，发现 1；捕获 PMM 反复模拟，以及与本题不同的词条图鉴／模拟一致性等材料。
- 回看 [G01-F23–F41／FL01](../../../01-global-boundaries/battlefield-experience/evidence.md#局部受挫与失败解释)，定点回读下列来源。查询彼此有重叠，不相加、不代表逐篇精读全部候选。`intentional`、Scout 职业名、隐身、同步模拟、配方预览、局外奖励与事后统计等误命中均分流。
- 纳入详细比较的条件：确实改变准备时知道什么、何时知道、信息是否稳定或怎样取得；仅说明某敌人有反制、某界面不清楚，不另列一个信息制度。

### S01：本场敌阵与能力可查，给已有资产调整提供依据

**Tiny Auto Knights**：`ev-tak-001-async-snapshot-and-live-mode-separation`，`src-tak-thread-async-pvp`（2025-01 至 05 Playtest／Demo）、`src-tak-official-demo-july-2025-07-23` 明确 Ranked 对手预览及上传队伍快照。可针对保存的 3×3 阵容准备；资料没有完整字段、锁定时刻或准备中重抽规则，也不证明所有模式都能预览。2026-02 好友实时模式是另一条模式，不拼接为相同生命周期。

**Auto Brawl Chess**：`ev-auto-brawl-chess-012-journey-fixed-enemy-adaptation`，`src-abc-help-journey-2022` 的历史 Journey 提供 Boss 队预览；页面 2026 wrapper 不代表规则重新核实。`src-abc-discussion-journey-2-35-2023` 的固定开局与免疫／隐身压力只支持单关观察，不能推成所有 Journey 波次固定。重试与 Faction 限制不迁入本题。

**Gladiator Guild Manager**：`ev-ggm-006-enemy-items-priority-adaptation`，`src-ggm-official-0-942`（2024-03-09）、`src-ggm-steam-achievements-guide`、`src-ggm-steam-campaign-v1`。装备敌人可识别，玩家检查危险远程／AOE 持有者再调整站位、控制或消耗品。选定敌人加入装备同时降低等级预算，说明相近总战力可以包含不同威胁；不照搬数值等价公式或手动优先级。

**Mirror Throne**：`ev-mirror-throne-009-reader-onboarding-evolution`、`src-mt-demo2`（2024-02-02）等记录加入等级相关敌情预览。能支持具体信息而非纯战力总分，但缺全字段与正式版延续表；新手仍有试错学习反馈，不能宣称补 tooltip 已解决理解问题。

**Monster Train**：[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/monster-train.md)标准循环与 `ev-monster-train-004-three-floor-battle-order`、`src-mt-wiki-battle`、`src-mt-review-layers`、`src-mt-review-capacity` 支持战前主要敌人／可选 Trial 预览。它不等于全部波次与数值公开。上述作品共同支持 PV02 的机制方向，项目提出的完整关键机制检查范围仍是建议。

### S02：远期关键敌人早告知，信息用于沿途投入

**Gods vs Horrors**：`ev-gods-vs-horrors-015-enemy-package-counter-grid`，`src-gvh-guide-strategy-3493071391`（开发者发布、社区共创，2025-06-04，1.0）明确最终 Boss 从开局已知。`src-gvh-discussion-scorpion-counter`（2025-05-16，1.0）另支持下一波预览；首次看到具体字段的准确窗口未知。`ev-gods-vs-horrors-003-deterministic-combat-explanation` 的确定性及解释困难不能补成全战预测功能。早知 Boss 不证明开局公开全部技能或普通波次。

该作攻略列出顺劈承压、右侧狙击、一次先手、护盾等针对性回答；评测仍报告确定构筑后缺关键反制。只能说早告知让沿途准备成为可能，不能证明供给已保证或总体平衡成立。

**Dungeon 100**：`ev-d100-016-boss-deck-preview-challenge-loophole`、`src-d100-official-2023-07-07` 明确选角与战斗中可看当前 Boss 卡组。它展示到具体构筑层，不只是身份；没有授权推成完整隐藏数值／AI／触发关系图。历史构筑变 Boss、Challenge 存档和镜像复制争论属于其他制度，用户此前保留意见仍不变。

**Monster Train**：`ev-monster-train-020-seraph-four-counter-packages`；`src-mt-wiki-seraph-temperant`、`src-mt-wiki-seraph-chaste`、`src-mt-wiki-seraph-diligent`、`src-mt-wiki-seraph-patient`（末修订 2021–2023），2.x Ring 8。可预览不同 Seraph，分别压攻击、削减状态、消耗法术并污染牌库、读取 Rally／Incant 成长，因而准备方向不同。资料未闭合精确首次公开时点，不能照搬“开局全部已知”或“提前固定 X 场”。

### S03：完整未来序列与只给粗略情报，是不同的项目政策

**Super Auto Pets**：`ev-sap-012-daily-fixed-enemy-package`、`src-sap-official-daily-2026`（2026-04-08）只直接确认每天所有玩家面对相同 14 队，**没有说明游戏内提前公开整份序列**。它可作为固定挑战结构参照，PV04“完整未来遭遇提前可查”是进一步的项目推演，不冒充原作功能，也不新增每日模式或重试。

PV01 的“远处只给类型／威胁摘要”可由项目当前路线卡基线具体说明；它与开战前完全不显示敌阵不是一回事。若坚持未知直到开战，便会削弱既定战前准备。此种更严格隐藏未获采用；不是将所有缺字段的原作归成有意隐藏。

### S04：信息本身需要取得，或已看到的对手未必就是最终目标

**Auto Battleships**：[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-battleships.md)、发现 `e054`／`s033`（官方商店）。截至既有 2026-09-03 研究只有产品页及 2026-04-14 公告，没有公开实践闭环。承诺战前用不同形状／重复次数的侦察物品部分揭示敌舰布局，再调整己方位置。价格、是否消耗、占槽、信息保存、反侦察、匹配与精确时点全部缺证。**PV05 的“消耗金币／机会”只能作为项目可选成本，不能归因于已证实原作付费。**

**TFT**：`ev-tft-004-scouting-positioning`，`src-tft-bamboo-scouting-positioning`、`src-tft-bamboo-beginner`，2026-09-02 访问的未标版本指南。备战可查看其他棋盘，躲钩／突袭／群控，调整主坦与核心位置；潜在对手不唯一，对方也能换边，故侦察支持概率性准备。PV06 比较的是敌情不完全确定，而非故意提供错误情报。单机若用候选敌人集合替代玩家对手，是项目适配；不自动创建能针对玩家临时变阵的 AI。

### S05：从说明规则，到预演局部后果或试打整场

**Vivid Knight**：`ev-vivid-010-gem-intent-skip-intervention-layer`，`src-vivid-guide-easy-start`、`src-vivid-guide-maze9`、`src-vivid-guide-boss`、`src-vivid-official-1-1-11`，v1.1.10–1.2.3。显示下一动作，玩家据危险意图保留两面护盾、施放或跳过 Gem。**Backpack Hero**：`ev-backpack-hero-023-readable-enemy-counter-packages`、`src-bh-official-enemy-rework` 等，2023 重做与 1.0 期指南支持更明确意图／可中断窗口。两者主要服务战中动作，不能变成本项目已有介入权限。

**Into the Breach**：发现 `e091`／`s060` 只有官方商店级“敌方攻击先预告，玩家用移动／位移／击杀改写”线索，没有补丁级完整精度或例外表。PV07 将这种局部因果可见适配为部署时的首个目标／起始覆盖辅助，是项目建议，不保证首个目标之后的整场路径和结果。

**PMM**：`ev-pmm-001-observe-refine-repeat-loop`，`src-pmm-official-loop-rebuild-2024-05-13`、`src-pmm-official-playtesting-2024-06-21` 与 `src-pmm-review-kr-runlog-2026-02-19`。官方循环及停更后最终版流程记录支持先模拟任务、改位置／装备／区域策略，再进入实际行动。`ev-pmm-014-section-report-attribution` 保留玩家对模拟之间缺直接结果比较的批评。Campaign／Freeplay／Prologue 日程不同，不能补定无限免费次数或完美预测。PV08 采用这种准备权限会改变本项目首次正式挑战的未知风险，是独立方向，不由“不限时思考”自动授权。

### 反例、相邻材料与缺口

- `ev-bpd-011-enemy-trait-scaling-preview`、`src-bpd-review-endless-scaling-traits-2026-08-04` 等：2.2.x 深层个案报告随机净化／恢复／多击组合与预览不足，使构筑被克制；无 seed／发生率。它提示要在仍能准备时披露改变胜负方向的能力，不证明所有随机敌人都不合理。
- `ev-neon-auto-party-018-actual-output-tooltip` 只证明 0.4.1 显示修正后伤害／治疗，未证实计入目标抗性、位置或 Barrier。不能拿“实际数值”补成精确对敌伤害或全战胜率预测。
- PMM 总战力不解释具体原因、Gods vs Horrors 无 RNG 仍有归因困难，都不证明游戏提供可靠胜负计算器。本轮没有足够来源把完整战局答案视为常见成熟基线。
- 已知针对件／Boss 攻略不自动证明原作何时公开；TFT 猜对手、TAK 已存快照、ABC 单关固定开局不能合并成一条“所有预览都锁死最终敌方”的规则。
- 日志、回放与事后统计留后续反馈问题；本局可用池查询沿 I27，局外图鉴／知识解锁另看 R14。当前扩查未取得足够证据把“初见必须失败才能解锁敌情”列作成熟通用制度。

据此形成 PV01–PV08。原作事实、开发中线索、项目政策与不同权限的迁移分别标注；研究提出时尚无用户决定，后续确认见 [R12-D01](decisions.md)。
