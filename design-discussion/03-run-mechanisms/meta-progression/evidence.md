# R14 局外进度：已有证据

仅为讨论证据，不是现行权威。方案见 [proposals.md](proposals.md)，状态见 [总览](../../roadmap.md)。

## 本轮问题与边界

2026-09-09，R14-Q01：**一局结束后保留哪些成果，这些成果怎样改变下一局的内容、起步条件和挑战？** 先确定跨局改变什么；通关、里程碑、个人使用或局外资源等具体取得条件，随所选方向继续细化，不在首题强定价格／经验公式。

- [核心权威](../../../gameplay-design/tower-autobattler-core.md)将英雄或难度解锁纳入 Alpha，但没有规定完整解锁表、永久货币或永久战力。
- [G03-D02](../../01-global-boundaries/run-rhythm/decisions.md)已定随机候选中选择若干英雄起步，沿实际所得逐渐形成体系；[R06-D01](../supply-and-economy/run-content-pool/decisions.md)已定每局按内容组随机开放，另有公共功能英雄。账号已解锁集合、本局合法集合和本次候选是三层；其过滤关系尚未决定，解锁不等于直接取得或每局必定入池。
- [R13-D01](../journey-and-pressure/risk-and-reward/decisions.md)已足以基础阶段收束；一般 RK01＋RK02，其他机制认可保留，不重选。整局难度解锁与局内单场自选加码分开。
- [R11-D01](../journey-and-pressure/attrition-and-recovery/decisions.md)的胜后满血、倒地者下场可用及败／超时终局保持。跨局奖励不自动恢复败后续关，也不把战中倒地变为永久培养清零。
- R14 不引入永久军团经营、城建或未经讨论的永久数值成长；[构筑框架](../../../gameplay-design/combat-build-framework.md)中通用科技仍暂缓。相关成长对照可用于理解影响，不因此纳入默认方案；历史构筑变 Boss 已有保留意见，不重新作为待批准必做。

## 实际检索

仅检索已有本地语料，未联网补研、运行游戏或修改原库：

- [深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)780 条，[发现层](../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)106 条，[档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/)56 份；回读相关正文及 [来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。
- 首筛字段值交叉匹配：`meta.?progress|meta.?currency|persistent.?progress|account|permanent|unlock|prestige|carry.?over|mastery|跨局|局外|永久|解锁|继承|熟练|图鉴` 与 `run|campaign|start|new.game|account|profile|end|after|reset|restart|每局|下局|开局|通关|结算|失败|重开|新局`。深全字段 182／核心 66，发现全字段 4／核心 0。这里核心为实际存在的 `rule_support / practical_support / mechanism / engine`，发现层结构不同，随后补查其实际机制字段。
- 档案以 `meta.?progress|meta.?currency|permanent|unlock|prestige|carry.?over|跨局|局外|永久|解锁|继承|熟练|图鉴` 命中 51 份／288 行。带上下文的初次输出过大，改为上述命中行与定点正文回读，不把截断输出视为读完全文。
- 深核心独立扩查 `meta.?progress|prestige|ascension|covenant|unlock.all|run.start|carry.?over|out.of.run|achievement|compendium|collection|cosmetic|mastery|局外|跨局|继承|图鉴|成就|熟练`，42 条。发现层放宽为 `account|meta.?progress|meta.?currency|prestige|unlock.all|compendium|collection|cosmetic|mastery|局外|跨局|图鉴|熟练`，全字段及实际核心字段均为 2 条：e075 是直接横向解锁，e097 是赛前选禁的相邻材料。
- 再定点核对单位 run-end XP、Survivor Bonus、Arcane 开局工具、难度分组及原作模式，补查是否存在真正的新局资产继承。各查询重叠，不相加；记录字段中的项目建议不是原作事实，命中数不表示穷尽机制或规则全闭合。

## S01：长期开放内容，与开放完整内容入口

**The Last Flame**：`ev-tlf-012-content-dilution-soft-ban`；`src-tlf-official-1-0`（2025-01-09）、`src-tlf-gameplay-starter-2025`、`src-tlf-steam-indepth-2026`。1.0 回顾与档案明确大量 hybrid／ethereal 内容放至 Unlock 8，Reborn 系统放至 Unlock 8/8。因此进度既可扩大以后可见的内容，也可延后复杂局内机制。**没有完整八阶段表或 Unlock 按经验／胜局／任务增长的条文**，不补定触发方式。

官方明确收到 item dilution 负面反馈，也认为部分玩家没有充分使用廉价刷新；未量化两种原因占比。它支持“新增选项会改变可达性”，不证明 Soft Ban 与配方锁定已彻底解决问题。本项目有限刷新、遗物改变刷新策略已定，不能直接用原作廉价刷新作为补救承诺。

**Vivid Knight**：`ev-vivid-020-unlock-pool-dilution-community-failure`；`src-vivid-guide-boss`、`src-vivid-guide-easy-start`、`src-vivid-guide-symbol-tips`、`src-vivid-official-1-1-11`。v1.1.1–1.1.5 一篇 Boss 指南建议不解锁某些相互干扰的 Gem 家族，以免难凑计划中的工具；另一篇指南重视更广单位曝光。强烈观点且有读档操纵背景，无掉率表或总体统计，不能说所有玩家都因解锁变弱。对应 MG01 的池稀释风险。

**Gods vs Horrors**：发现 `e075 / s050`（官方商店）明确“局外只解锁选项，不直接增加数值”，是方向级资料；深记录 `ev-gods-vs-horrors-001-version-mode-boundary` 及 `src-gvh-review-ladder-194305497`（2025-05-06，正式首周 19 小时评测）进一步证实 optional unlock-all。**完整解锁奖励表、按钮流程、覆盖对象及可撤销性未知**。全开放不等于自行指定本局神系或初始阵容。

**Backpack Hero**：`ev-backpack-hero-031-story-quick-game-layer`；`src-bh-official-story-testing`（2023-10-17）、`src-bh-official-town-economy`（10-25）、`src-bh-official-town-layout`（11-08）、`src-bh-official-quick-game-naming`（11-29）。Story 通过任务、资源转换、研究和城镇逐步开放；Quick Game 保留经典全解锁直接 run。开发者因玩家找不到入口专门解释，拟议改名是否交付未知。城镇布局／经营是原作额外成本，不迁入本题；胜败能带回多少物品和完整转换表缺失，不证明整套背包继承。

对应 **MG01 分阶段开放**、**MG02 完整开放／允许跳过**。二者可按入口并存；解锁的取得机制与解锁后是否进每局池须分开决定。

## S02：永久提高战力，或永久提高经营与搜索能力

**Loot Loop**：`ev-loot-loop-008-skill-tree-economy-axes`、`ev-loot-loop-019-tree-completion-choice-collapse`、`ev-loot-loop-006-heal-only-aura-pruning-build`；`src-loot-loop-official-release-2026-04-13`、`src-loot-loop-higher-plain-review-2026-04-14`、`src-loot-loop-hakimodo-review-2026-07-19`、`src-loot-loop-discussion-healer-upgrades-break`。2026 正式版至 1.1，固定四人反复 run 后购买持久技能／perk，涉及攻击、攻速、暴击、防御与团队生命等。

普通 run 保留节点，Prestige 是另一层重置。评测认为多数节点最终买满，终局差异更多来自完成度和购买顺序；Healer 新 aura 与治疗争轮转槽，可能降低已投资的可靠性，无普通 respec，引出重开或只买治疗的实践。也有玩家支持速度／收益 aura，没有唯一最优共识；1.2 同时 aura 重做和 1.4 新成长仍为计划。完整树、价格及各结局钱包保留表缺失。对应 **MG03 局外购买持久战力**，不因此批准本项目通用科技树。

**Slotbound**：`ev-slot-003-three-resource-economy`、`ev-slot-013-early-rng-meta-pressure`、`ev-slot-020-golden-rarity-cap-lineage`。Demo 0.2.0–0.3.4，访问时主游戏未发行；`src-slot-review-core-economy-2026-07-26`、`src-slot-review-cavalry-2026-08-15`、`src-slot-discussion-golden-relic-2026-08-31` 等。Boss／Judgment 给局外资源，永久树购买 hearts、army size、reroll、advanced rarity、interest／supply。hearts 是本局承受失败的资源，**不是战斗英雄 HP**。开发者确认 golden-symbol 的 +1 rarity 不能越过账号已解锁稀有度。

0.3.2 满树作者留 500 得 50 interest／以 200 supply 买兵，只是特定时期路线。早期有空转、反复重开、先刷简单模式的报告，也有更新后认为 Core 和经济能救回开局的实践，无种子或前后统计。货币名、死亡扣失和完整价格未定。对应 **MG04 永久改变经营、搜索、容量和容错权限**，与单纯涨攻血不同；本项目普通人口默认开放、有限刷新、普通不生息不能由此重新上锁或取消。

**ShapeHero Factory**：`ev-shf-008-route-research-shop-rng-economy`；`src-shf-discussion-starting-research`（2025-09-20 后、1.0 首周）、`src-shf-official-1-0-0`（09-17）。档案明确 Arcane Knowledge 是跨 run 解锁，consumable 支持反复寻找开局 Research；玩家趋向 Underground Utilization＋Inserters 起手。档案称无限 reroll，但来源登记没有闭合工具获取、支付、消耗和重开恢复合同，也没有 1.1 重测。可支持 MG04 的开局随机修正方向，不能推成当前无限免费搜索规则。

**Survivor Mercs**：`ev-survivor-mercs-005-recruitment-specialization-pool`、`ev-survivor-mercs-015-gear-pool-tradeoff-lifecycle`、`ev-survivor-mercs-016-extraction-economy-rework`；`src-sm-guide-new-player`（2026-04-30 更新至 1.0）、`src-sm-official-final-drill-0-15`（2026-03-20）、`src-sm-discussion-gear-pool-dilution`（2023-09-14 历史）。带回资源供 Bunker 与解锁，服务能改善 reroll／排除候选；新增 Gear 也出现池稀释。具体排除数、期限和价格不全，不证明任意永久屏蔽。本题保留权限机制，基地经营不自动进入项目。

## S03：参战英雄的永久成长，与可以失去的老兵奖励

**Neon Auto Party**：`ev-neon-auto-party-017-failure-xp-growth-rework`，关联 `ev-neon-auto-party-013-stress-rework`。`src-nap-discussion-playtest-feedback-2024-2025` 中 2025-06-24 开发者回复明确：run end 按 cleared waves 给参战单位 XP，failed run 也给；`src-nap-main-0-5-2`（06-27）只调整 XP scaling，并预期单位 level-up 能帮助 next few runs，没有重述结算时点。当前完整经验、技能收益和重置规则不全，无更新后独立验证。

对应 **MG05 参战英雄专属永久成长**。不能与局内重复招募合并／Rank／Expertise／加卡混为一谈；视频里的同局扣 life 重试不证明永久 XP，也不导入项目败后续关。原资料不支持整套卡组、装备或棋子实例原样继承。

**Survivor Mercs**：`ev-survivor-mercs-011-survivor-bonus-risk-lifecycle`；`src-sm-discussion-survivor-bonus`（2023-09-20）、`src-sm-guide-new-player`、`src-sm-official-terminal-0-11`（2025-08-07）、`src-sm-official-update-1-2`（2026-06-08）。合资格 Merc 生还撤离／胜利后积累 chevron，后续 Operation 再次招募该 Merc 时获得 1–3 次免费起始升级；以后带它出征死亡会失去累计 Bonus。

2023 EA 资格为该局投入至少十次 level-up，1.0 Guide 改为 specialization tree 至少 level 6，**不能叠成同时要求**。对应 **MG07 可丢失的跨局起步优势**，与 MG05 永久经验不同。它不保证下局自动携带该 Merc，也没有完整保留上局专精、等级或 Gear。精确提交时点、Abort 结算及迁移资格有缺口；本项目若展开，战中倒地与整局失败的触发须另定，不自动追加全员永久损失。

## S04：局外积累换取开局规则

**Mirror Throne**：`ev-mirror-throne-005-shard-cursed-trinket-seeding`；`src-mt-demo7`（2024-08-09）、`src-mt-achievements`、`src-mt-review-party-204135472`（2025-09-12，1.1 后）。Demo 7 有赛前 Shard Shop，通过游玩／高分取得 Shards，购买／选择 trinket 改变开局规则；cursed trinket 加难换 score 和挑战目标。

对应 **MG06 局外积累提供开局配置能力**。实际购买与选择可提前影响体系，不是被动给所有英雄加面板。正式评测中的 Jewels 不等于已经证实同一种 Shards；价格、槽数、买断／每局消耗、退出退款等完整当前规则不足，不补写“无限免费换起手”或“每次退出全损”。项目若用，需要明确与随机起步的关系，不自动恢复开局指定阵营／核心队伍。

## S05：保存更高挑战资格，或让胜败改变长期段位

**Backpack Dungeon**：`ev-bpd-014-endless-progression-reward-mismatch`；`src-bpd-discussion-endless-reward-2026-08-26`（开发者）、`src-bpd-guide-achievements-2-2-5` 等，2.0.x–2.4.x 切片。Ascension 逐级开放，Endless 当前无额外奖励；普通构筑完成后，无尽的适应要求不一定对应更多局外进度。完整解锁条件／档表没有全部登记，不补为必定每赢一局升一级。

**Survivor Mercs**：`ev-survivor-mercs-001-current-version-modes`，`src-sm-guide-new-player`、`src-sm-official-release-1-0`：Standard Boss clear 开放 Elite Operations，是新增以后可选择的挑战，不等于所有新局自动升难。Neon Stress 分层解锁另见 S03，XP 接收者与倍率不扩成账号统一奖励。

**Tales & Tactics**：`ev-tnt-015-set2-difficulty-state-separation`；`src-tnt-official-2-0-14`（2026-08-26）。Set 2 使用独立 Challenge Climb，避免把旧内容组的熟练进度直接当成已掌握新内容；代价是可能重新爬阶。完整逐档规则缺失。对应 **MG08 难度／挑战资格记录**，按英雄还是内容组记进度属于后续归属问题，不自动按 R06 每次随机组合作永久难度档。

**Gods vs Horrors**：`ev-gods-vs-horrors-018-ladder-casual-failure-contract`；`src-gvh-discussion-ladder-casual`（2024 Demo 历史）、`src-gvh-official-qol-2025-05-19`（1.0 后、1.1 前）、`src-gvh-discussion-forced-pantheon`（2025-06-20，1.1 玩家）。Ladder 有胜负相关进度和掉级，Casual 难度从起点开放，服务实验与持续挑战的不同需要。下一组 mythology／Relic 在结果推进前固定仅为玩家观察，不能补成完整胜败／重开刷新算法。

对应 **MG09 胜败改变长期段位**，与 MG08 解锁高难后自由选择不同。**Super Auto Pets** `ev-sap-009-arena-difficulty-rework`／`src-sap-official-update44` 另展示从经验隐性影响敌强改为显式难度；不证明所有经验自适应都有害，但说明账号经验、难度选择和对手生成要分清。

## S06：收藏记录、重置进度与历史敌人

**Kādomon**：`ev-kado-018-misprint-collection-not-build-core`；`src-kado-official-faq-2024-02-20`、`src-kado-guide-misprint`（2024-03-28 至 2025-05-16 更新）、`src-kado-official-040`。Misprint 须带到 rest event 或通过 run 带出才永久解锁，之前失败会丢失。保留的是 alternate appearance，**战斗机制不变**；seed hunting／重开和保留不适合阵容的稀有色会竞争正常构筑目标。

`ev-kado-020-keeper-relic-and-variable-boss-package` 与 `src-kado-official-100`（2025-04-07）另确认 Ascension、Victory Ribbons 和 Kādo Guide 等新增；没有证据说 Ribbon 提供战力或解锁 Keeper。对应 **MG10 非战力收藏与完成记录**；项目图鉴／统计是该方向的适配，不照搬强制带出规则、异色制作量或全部卡多收集系统。

**Loot Loop**：`ev-loot-loop-011-single-prestige-final-map-gate`；`src-loot-loop-official-release-2026-04-13`、`src-loot-loop-higher-plain-review-2026-04-14`、`src-loot-loop-hakimodo-review-2026-07-19`、`src-loot-loop-review-prestige-flat-233080820`（2026-08-17，1.1）。Prestige 重置已推进进度、提高收入／强度／生存并开放最终地图；评测说当前仅一次，另一作者称强化后约十分钟重跑。有反馈快和缺新决策的不同意见，确切保留清单／倍率未知。对应 **MG11 重置换加速与新内容**，是短篇增量游戏的邻接结构，不把普通短 run、完整爬塔和版本清档混同。

**Dungeon 100**：`ev-d100-001-past-build-boss-ladder`、`ev-d100-016-boss-deck-preview-challenge-loophole`；`src-d100-official-2023-07-07`、`src-d100-official-2023-08-10`、`src-d100-thread-boss-ladder`、`src-d100-thread-boss-class-lock`。2023 Classic／Challenge 切片把成功职业和九卡／星级图保存为后来要面对的 Boss，玩家需准备对付自己过去的能力组合；精确保存替换、职业锁和 Challenge 复制边界不全。

对应 **MG12 历史构筑改变未来敌人**，保留的是挑战内容，不是新局己方战力。本项目此前已保留意见，不借 R14 重新要求采纳、开放上传或复制存档。

## 未扩成新机制的相关命中

- STS 深记录与官方 Ascension 发布、Monster Train Covenant、Astronarch Corruption／Omen 能证明模式或难度结构，**没有完整角色经验、氏族等级、金卡／奖章、通关解锁表**；不凭熟悉程度补充，不把版本新增角色／付费 DLC 算成账号游玩成果。
- Vivid Knight 永久 Symbol、Epic Auto Towers 保留加成、Combat Alchemy 的 permanent、Monster Train 合成、SAP 冻结等，多数是同局跨战／回合持久；与新局保留不同。研究用语 meta 也可能指主流策略，而非局外进度。
- GGM／PMM 的长期角色经营、Siralim 的长期收藏与装备档案、Auto Brawl Chess 的账号装备／Warlord 层属于不同 campaign／账号模式，不构成本项目已经允许整队带入新局的证据；不在本题另开永久军团和城镇建设选择。
- Backpack Hero Story 与 Survivor Mercs 带回资源可改变后续解锁／服务，但各结局的资源保存、丢失和回收比例未闭合。未找到足够证据再单列“上一局整套具体装备和培养原样带入下一局”；MG07 只保留已证实的 Bonus。
- Ananke 商店中的 permanent upgrade 作用域不明，仅有未发行产品承诺；不能推为跨局成长。发现层 e025／e044／e050／e080／e097 分别为长期培养、内容载体、同局永久或赛前选禁，本轮不扩成新的跨局制度。
- 设置、同局续玩、版本兼容／补偿、事务去重是系统职责，不是局外奖励机制；不把 debug Lab 的保存配置当玩家可携带资产。

## 现行实现静态基线

只读核对，无构建、运行或读取玩家实际存档：

| 范围 | 实现与设计边界 |
| --- | --- |
| 跨局数据 | [RunModels](../../../src/Run/RunModels.cs) 的 Meta 只有版本、UnlockedHeroIds、Victories、HighestRegion、AppliedRunCompletionIds。完成凭证用于去重，不是玩家资源；无永久属性、技能、装备、货币／材料字段。 |
| 初始与胜利解锁 | [RunProgressionPersistenceService](../../../src/Run/RunProgressionPersistenceService.cs) 在有效解锁表空时开放目录前若干英雄，[RunRulesDefinition](../../../src/Project/RunRulesDefinition.cs)默认 3；完整最终 Boss 通关后增加胜利数、解锁目录首个未解锁英雄。不是已确定的未来解锁顺序／频率。 |
| 失败与放弃 | 失败不发胜利解锁，但保留此前更新的历史最高区域；放弃结束活动征程。没有失败货币或英雄经验发放。 |
| 解锁消费 | [HeroSelectScreen](../../../src/UI/HeroSelectScreen.cs)与[开局校验](../../../src/Run/RunRewardEconomyService.cs)只限制所选起始英雄；初始追加及局内招募分别用 StarterPool／RecruitmentPool，[当前 campaign](../../../content/project/alpha_campaign.tres)同指向 pool_all_soldiers，不读取 Meta 解锁表。不能说账号新解锁已扩大局内池。 |
| 难度 | 所查生产路径没有 Difficulty 状态／选择／解锁；[胜利文案](../../../src/App/GameFlowCoordinator.cs)说“更高难度正在等待”不等于功能存在。 |
| 同局续玩 | ActiveRun 保存队伍／装备／金币／遗物等用于继续同一局，完成 Meta 提交后清除；新 run 重建状态，不能当跨局继承。保存／终局事务见 [系统契约](../../../system-design/content-composition-foundation.md)。 |

G03 随机选若干开局英雄与当前单一起始英雄入口的差异已在 I14；R06 组池与账号解锁关系沿 I27。R14 新增接口沿 I35 登记，不在本轮修实现或权威。以上形成 MG01–MG12 材料，研究提出时尚无 R14 用户决定；后续接受范围见 [R14-D01](decisions.md)。

## Q01 研究提出时的静态核对

新增证据中 28 个深记录 id、49 个显式来源 id 可解析；受影响六份讨论文档的 146 个本地链接有效，MG01–MG12 标识唯一，未建立 R14 decisions.md。定点复核补齐了 Survivor Bonus 必须成功撤离／胜利的前提，其余指定成长机制保留原作版本与缺口。`git diff --check` 未发现空白错误；讨论区外纳入检查的 1,829 个文件清单／内容汇总与写入前一致，roadmap 并行 R10／M02／P01 入口及未合并决定清单原文保持。检查仅证明引用与写入范围，不是玩法或体验验收。

## Q01 用户回应后的记录

2026-09-09，用户选择“先 1 8 10”，已建立 [R14-D01](decisions.md)，初期采用 MG01、MG08 与 MG10 轻量记录。此次无新调研，以上原作规则、版本、实现静态基线及证据缺口不变；具体解锁条件与组池关系仍待讨论，不用用户接受项目方向来填补原作未知细则。

## R14-Q02：新解锁内容怎样接入每局内容组

日期：2026-09-10。承接 [R14-D01](decisions.md) 和 [R06-D01](../supply-and-economy/run-content-pool/decisions.md)。本轮只问：**新内容以什么粒度获得普通入池资格，怎样避免账号进度把关键依赖拆散？** 解锁由通关、任务还是累计成果推动，未通关是否推进，留下一子问题；不重选 MG01／MG08／MG10 或 R06 的随机组池。

### Q02 检索范围

- 复用 Q01 与 R06 已有全库检索及来源，再对 780 条深记录、106 条发现记录、56 份档案全库补筛；仅本地只读，未联网。
- 深全字段交叉 `unlock|meta.?progress|account|permanent|owned|解锁|局外|账号|开放|拥有` 与 `pool|pack|content|hero|unit|item|relic|mytholog|clan|ban|eligible|roster|卡池|内容|候选|体系|英雄|装备|遗物|屏蔽|稀释`，226 条；四核心字段 rule_support／practical_support／mechanism／engine 同筛 85 条。去掉 permanent／owned／开放等宽泛词后核心 43 条，区分局内永久、版本解锁及账号内容。
- 发现层以解锁／账号词与内容／池词交叉，5 条：e021／e074 是当局招募档次，e040 是挂机长期成长，e060 是跨元素效果条件，e075 才直接涉及未来内容池；不能把发现层“长期”自动当作跨局。
- 档案同行双向交叉检索解锁与池／英雄／物品等词，28 份／62 行；按归属定点回读。另查全字段有解锁、核心未含解锁的记录 41 条，补出 Just King 物品编辑池等关联；各查询重叠，不相加，不把关键词命中数当精读或完备性。
- 回读分工覆盖 TLF／Vivid／Survivor Mercs 的扩池与纠偏，Gods vs Horrors／Monster Train／SAP 的分组和资格，以及 Just King、Tales & Tactics、Slotbound、Girls of The Tower 的边界。主责补查 Auto Brawl Chess、My Party Is Grinding、Dwarves、Mirror Throne、Backpack Hero，并排除无直接关系命中。

### Q02-A：解锁扩大普通可遇到范围

**The Last Flame**，`ev-tlf-012-content-dilution-soft-ban`；`src-tlf-official-1-0`（2025-01-09），辅以 `src-tlf-gameplay-starter-2025`、`src-tlf-steam-indepth-2026`。1.0 官方承认 item dilution 反馈，使用 Soft Ban weighting、recipe locking 和分阶段曝光；大量 hybrid／ethereal 内容延后到 Unlock 8，Reborn 延后至 8/8。它支持逐步增加可见内容、延后复杂机制；**未给出逐英雄／整组的完整解锁表，不能据此宣称账号按完整体系包解锁**。Soft Ban 是相关权重参照，没有任意自由选禁菜单或完整拒绝记忆算法的确证；便宜刷新也不能直接成为本项目有限刷新下的补救承诺。

**Vivid Knight**，`ev-vivid-020-unlock-pool-dilution-community-failure`；`src-vivid-guide-boss`（2021-05-30 发布、06-12 更新，v1.1.1–1.1.5），对照 `src-vivid-guide-easy-start`、`src-vivid-guide-symbol-tips` 和 `src-vivid-official-1-1-11`。账号解锁 Gem／单位后影响随机供给，一篇攻略主张少解锁与其路线冲突的 Gem，以免妨碍同名收集。此记录置信度 low，攻略含读档操作，其他实践强调更广曝光；只能证明明确的反向激励风险，不能说所有玩家少解锁更强。没有事后任意停用或解锁前试用的已核实规则；升银后 symbol 在当局保留另算。

**Survivor Mercs**，`ev-survivor-mercs-005-recruitment-specialization-pool`、`ev-survivor-mercs-015-gear-pool-tradeoff-lifecycle`；`src-sm-discussion-gear-pool-dilution`（2023-09-14 EA），`src-sm-guide-new-player`（2026-04-30 更新至 1.0），`src-sm-official-final-drill-0-15`（2026-03-20）。解锁更多 Merc／Gear 扩池，实际仍经 Beacon／Gear Crate 获取。Bunker／旧 Field Radio 提供可升级的 reroll 和 Merc／upgrade／Gear 排除，Operation 内可花 BD reroll。未闭合数量、费用、调整窗口及跨 Operation 保存；四 Gear 槽不证明可以开局预选全部配装带入。早期稀释反馈不作为重做后的当前强度表。

### Q02-B：分组限制当局来源，不等于已证明整组账号解锁

**Gods vs Horrors**，`ev-gods-vs-horrors-002-draft-economy-tempo`、`ev-gods-vs-horrors-004-mythology-trait-architecture`、`ev-gods-vs-horrors-017-pool-pivot-reachability`；`src-gvh-guide-strategy-3493071391`（2025-06-04，正式 1.0）、`src-gvh-official-qol-2025-05-19`、`src-gvh-official-update-1-1-2025-06-19`。档案写每局随机五种 mythology＋Neutral，具体 God 再经过当局供给取得。另有 `ev-gods-vs-horrors-001-version-mode-boundary`／`src-gvh-review-ladder-194305497` 的 optional unlock-all，以及发现 e075 的横向解锁方向。**这些没有闭合账号过滤先后、未解锁组补位、Neutral 初始名单或组内保障，不能拼成“原作先解锁整组再抽五组”的确定规则。**

**Monster Train**，`ev-monster-train-022-clan-combination-pivot`；`src-mt-wiki-synergy`、`src-mt-discussion-clan-combo`、`src-mt-winning-runs`。主氏族＋盟友氏族分别提供组件，可跨来源合作；2.2／TLD 实践没有闭合氏族账号等级如何过滤卡牌。主动选双氏族不同于本项目随机组，版本新增和付费 DLC 也不等于游玩解锁。此处只支持“组是供给来源，不必是固定阵容”。

**Super Auto Pets**，`ev-sap-008-custom-pack-super-limit`；`src-sap-official-update44`（2025-12-19）、`src-sap-tag-consistency-2026`（2026-06-04 维护快照）。预设／Weekly／Custom 等有不同包边界，Custom 跨已拥有内容组合；Standard Custom 最多五个 super pets，Wild 不受此限制。限制是包内特定内容类别数量，不是人口或抽取组数。owned packs 涉及商业拥有权，不是通关奖励；完整 Weekly 资格例外未知。它说明账号拥有、模式合法、这次使用的包不同，但不授权项目手编英雄池。

**Auto Brawl Chess**，`ev-auto-brawl-chess-011-seasonal-six-faction-pool`；`src-abc-patch-seasonal-pvp-2024`。2024 官方指出内容过多、商店过杂影响追三阶，引入六 Faction、每十四天轮换的 Seasonal，保留普通 Ranked。是模式／赛季筛池，不是账号解锁粒度或每局随机组的原作证明；无改后效果统计。

因此 [UP02／UP03](proposals.md) 的整组开放与组内可玩基础属于结合已定 R06 和上述风险的**项目推演**，不是多款原作已经验证同一套方案。

### Q02-C：解锁资格之外的纠偏与依赖

Just King 的 `ev-jk-009-equipment-slot-broadcast-pool` 记录 0.4.0 加入可编辑 item pool 与起始 favorites；`src-jk-official-2023-06-13`、`src-jk-guide-achievements-110`、`src-jk-thread-level-three-economy` 另覆盖 1.1.0 成就攻略及后续讨论中用改池寻找特定属性目标的实践。它提供“已解锁内容再由玩家决定允许出现哪些”的物品池参照，不能自动等同可任意编辑英雄池，也没有完整最小池／条目限制。与 Survivor Mercs 有预算／权限的排除、TLF 的相关权重分别对应 [UP06–UP08](proposals.md)，控制程度不同；本项目常规英雄手编池未采用，特殊遗物修池及适配推荐已有归属，不在此重新批准。

**Slotbound**，`ev-slot-020-golden-rarity-cap-lineage`；`src-slot-discussion-golden-relic-2026-08-31`。Demo 0.2–0.3.4 资料中，开发者确认 golden-symbol 的 +1 rarity 不能输出账号尚未解锁的 rarity。它是跨组限制成长档级，区别于增加新玩法条目；MG04 未纳入本次初期，不能从 MG01 重新上锁 R01／R02 的普通费用与品阶。

**Girls of The Tower**，`ev-girls-of-the-tower-017-pool-reachability-pivot`，`src-gott-review-pool-friction`（2024-06-29，62.9 小时）报告 locked exclusive 污染装备池、缺少排除及稀有遗物难遇；具体锁住对象和条件不明，不能断定是未解锁英雄或未购买 DLC；**My Party Is Grinding**，`ev-mpig-018-dead-reward-owner-facility-gap`，玩家拿到未解锁职业装备／设施还不能使用的材料。二者无完整频率数据，且各自的长期模式不同；用作“已出奖励却无合法使用者”的具体反例，不推定玩家当前阵容没有使用者就必须禁掉所有潜在转型物品。

**Gods vs Horrors**，`ev-gods-vs-horrors-013-relic-blessing-rule-rewrite`／`src-gvh-official-qol-2025-05-19`，改池 Relic 有不兼容选项修复；不证明自动补齐所有组合。项目应区分真正依赖未开放的专属物与可供其他英雄使用的通用物，具体筛法仍属 I27，不依据同主题标签整批删物品。

### Q02 相关命中去向与缺口

- Skull Horde 的 `ev-skull-horde-002-standard-roster-economy`、`ev-skull-horde-015-oath-pool-banish-economy`（`src-skull-discussion-rarity-pool`、`src-skull-review-screenhype`）为**局内**支付 rarity 扩池及 Banish 修剪；Mirror Throne `ev-mirror-throne-003-hope-arena-adaptation-loop` 是 Demo 5–6 的局内解锁与模式变化，不改写成账号组解锁。
- Tales & Tactics 的 `ev-tnt-004-four-dragon-four-noble-party`／`src-tnt-steam-ladder15-2024`（2024-09-23，约 1.0.50–1.0.53）描述只开放 Noble 作为额外 trait，但没有闭合账号故意不解锁、开局启用或局内取得的归属，不能当成选择性账号解锁的独立规则。SAP 回合 tier、Mechabellum 增援、Magicbook 等级属于当局供给；T&T Set 2 是版本内容及独立挑战进度。
- Dwarves `ev-dwarves-glory-death-loot-018-shop-pool-agency` 有商店稀释体验但原因未定；Auto Brawl Chess `ev-auto-brawl-chess-010-pool-pollution-search-failure` 的 locked／unowned／low-stat offers 各不相同，不能补成普通账号准入公式。其反例与上述依赖问题合并。
- Backpack Hero Story 的逐步内容开放与 Quick Game 全解锁沿 Q01／MG02 保留；本次不重新要求选择跳过解锁、城建、开局预设或完整沙盒。
- 新解锁何时影响已生成的局、重开是否换组属于生效时点／组状态问题。GVH 的 `ev-gods-vs-horrors-018-ladder-casual-failure-contract` 有 1.1 玩家观察，但缺完整生成合同；本题不据此引入锁下一局或反复重开算法。
- 没有足够本地证据单列“未解锁普通内容先试用再选择收录”或“每次新解锁有必遇试玩机会”。账号过滤顺序、组的最小可玩基础、条目权重与专属依赖仍需项目决定或后续内容校核；不凭熟悉作品补齐。

Q02 研究提出时无新增用户决定。仅补讨论证据、提案与恢复入口；现行实现基线沿 Q01 的已核对范围，未重新运行、构建或读取实际存档，未修改权威／实现／研究原库。

### Q02 用户回应后的记录

2026-09-10，用户同意 UP03 为日常基础、UP02 承接新玩法，已记为 [R14-D02](decisions.md)。本次无新调研；原作证据、版本和缺口保持，用户采纳的是项目方案，不能据此补成原作账号过滤算法。解锁粒度方向已定，取得条件和具体内容仍待后续讨论。

## R14-Q03：哪些成果推动内容解锁，未通关是否推进

日期：2026-09-10。承接 [R14-D01／D02](decisions.md)：本轮聚焦 MG01 的取得条件与失败结算。可抽组先有可玩基础成员，日常逐个扩展、需配套的新玩法按组开放；不重选解锁粒度。MG08 难度资格、MG10 记录奖励、R11 败后终局分别保持归属。

### Q03 检索与证据强度

- 复用 Q01／Q02 的全库覆盖，再筛 [深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json) 780 条、[发现层](../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json) 106 条及 [56 份档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/)；定点回读 [来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。只读本地，未联网。
- 解锁侧检索 `unlock|meta.?progress|account|persistent.?progress|progression|跨局|局外|解锁`，与成果侧 `experience|xp|score|achievement|mission|quest|challenge|task|milestone|victor|clear|wave|kill|defeat|fail|extract|resource|currency|shard|purchase|collect|discover|win|finish|complete|通关|击败|失败|任务|成就|积分|经验|资源|购买|带出|解救|发现|累计` 交叉：深全字段 165 条、四核心字段 65 条，发现层 7 条。核心字段为 rule_support／practical_support／mechanism／engine。
- 全字段命中而核心不满足交叉的 100 条按 id／关联归属继续分流，补出 Neon 失败 XP、Slotbound 三资源、Siralim Projects、Mirror Shards 等；不能只看标题是否含 unlock。
- 核心独立扩查 achievement／mission／quest／milestone／project／run end／failed run／each cleared／成就／里程碑／任务／通关／解锁条件，73 条。此为宽筛，projectile 等误命中已按语义排除，不称为 73 种相关机制。
- 档案对解锁与成果／结算词同行双向交叉，20 份／40 行；结合 Q01／Q02 原有广筛逐项回读。来源索引另查 unlock 与 quest／mission／achievement／experience、arrival reward、first clear、unlock condition。各查询重叠，不相加；截断的批量输出不计作完整阅读，定点另取字段和来源段。
- 发现层 e021／e074 是当局供给档次，e050 是当局跨回合保留，e060 是技能条件，e084 是地图服务；e040 是挂机长期成长，e075 只有横向内容解锁方向。没有补成七种账号取得制度。
- 原库对构筑规则的材料比账号取得表完整。本轮未找到覆盖全部“目标→奖励→失败／弃局保存→选择权”的原作合同。以下将直接规则、相邻奖励机制与项目推演分开；不凭熟悉 Slay the Spire／Monster Train 补角色经验和解锁表。

### Q03-A：完整成功与途中里程碑

**本项目现行静态基线**沿 Q01：完整最终 Boss 通关后追加目录首个未解锁起始英雄，失败不发这项胜利解锁；历史最高区域与同局续玩分别记录。这是 [UC01](proposals.md) 的明确项目对照，不是 D01／D02 已采用的取得条件，本轮未重新运行实现。

**Vivid Knight**，`src-vivid-official-1-1-11`（Asobism，2021-06-07），档案第 145 行登记从 **Boss defeat reward 改为 arrival reward**。可比较“到达节点即认可成果”与“必须击败”的差别。索引警示英文有重复／翻译措辞，仅采用明确 arrival。没有单独深记录 id；`ev-vivid-020-unlock-pool-dilution-community-failure` 引用该补丁，但没有闭合该奖励的具体类型、跨局性、首次限定和提交时点。不能直接说原作首到 Boss 就给永久解锁点，或退出也保留。

**Survivor Mercs**，`ev-survivor-mercs-001-current-version-modes`／`src-sm-guide-new-player`（2026-04-30 更新至 1.0），Standard Boss clear 开放 Elite Operations，是可确认的通关门槛，**奖励属于挑战资格 MG08**。移作新内容组的门槛是项目适配，不在此确认 MG08 的全套条件。

因此 UC02 将“首次达到／完成某个明确阶段即可开放内容”作为项目方案；到达还是击败、奖励对应什么阶段未定，不从原作缺口补一张固定关卡表。它可以与 UC03 的可重复累计并存。

### Q03-B：按实际推进累计，最终失败仍结算

**Neon Auto Party**，`ev-neon-auto-party-017-failure-xp-growth-rework`；`src-nap-discussion-playtest-feedback-2024-2025` 中 2025-06-24 开发者回复：**run end 按 cleared waves 给参战单位 XP，failed run 也给**。`src-nap-main-0-5-2`（2025-06-27）只调整 XP scaling 并预期帮助后续局次，没有重述失败结算或证明更新后效果。

接收者是参与单位，奖励是永久等级／战力，属于未纳入初期的 MG05。UC03 借用“已完成推进计入成果，最终失败不抹除”的结算结构，将奖励改为内容开放进度是项目适配；不能同时采用英雄永久经验。

`ev-neon-auto-party-013-stress-rework`／`src-nap-playtest-0-4-1-stress-machinist`（2025-02-08）另说高 Stress 使单位获得更多 XP，提供挑战影响收益速度的参照；倍率、取得资格与失败／弃局表未闭合。历史扣 life 后返回同局准备、merge XP 都不作为跨局奖励。

主动放弃／重开是否有 XP、途中加入或提前离开的单位资格仍未知；“failed run 也结算”不自动覆盖所有结束原因。

### Q03-C：完成具体玩法目标，而非只计算局数

**Skull Horde**，`ev-skull-horde-007-adam-corpse-build` 及档案第 82 行；`src-skull-guide-achievements`（2026-08-11，v1.032 后）将 Adam 解锁关联到 Carrion Strike、Rot、Psychoactive Fungus 和“三名敌人复生”。这是围绕复生行为的具名内容目标，但本库没有闭合三种方法是替代还是并列、“三名”是同时／同战／同局／累计，以及是否还需通关、何时永久提交。不能将摘要写成精确攻略。

**Just King**，`ev-jk-008-hero-upgrade-token-sale`；`src-jk-guide-achievements-110`（2024-08-07 更新，标注 1.1.0），相关历史 `src-jk-guide-hero-030`（2022，EA 0.3.0）。记录说指南使用 level-three milestones for unlocks and builds，可比较把某种局内培养做到里程碑的目标。没有完整“哪名英雄到三级→什么奖励”或败后保存表，不能说所有三级单位都给账号解锁。

`ev-jk-012-pacifist-environment-owner`／`src-jk-guide-pacifist`（2022-10-26）的无英雄伤害环境击杀，及 Mirror 指定 trinket 通关，都只确证特殊打法成就，不证明额外开放英雄／装备。本轮把条件型打法归 UC04，不因伤害、召唤、培养等主题不同拆成重复全局制度。

**失败观察**：`ev-skull-horde-017-status-stack-attribution-lifecycle`／`src-skull-official-v1-026`（2026-05-04）修复 Burn／Plague／Rot／Freeze／reanimated 成就归因。实际做到了却没记账与玩家没完成是不同问题；这不证明全部成就都有内容奖励或失败时必保存。

### Q03-D：通用解锁资源与目标研究

**Backpack Hero**，`ev-backpack-hero-031-story-quick-game-layer`；`src-bh-official-story-testing`（2023-10-17）、`src-bh-official-town-economy`（10-25）、`src-bh-official-town-layout`（11-08）。Story 以任务、地牢物品转 food／material／treasure 和建筑研究逐步开放物品。可支持任务与资源研究的结构；失败／退出是否保留任务、物品、研究资源，目标选择范围、先决条件和并行研究未记全，不引入城建。

**Siralim Ultimate**，`ev-siralim-ultimate-002-realm-progression-axes`、`ev-siralim-ultimate-003-specialization-player-layer`、`ev-siralim-ultimate-020-hellknight-progression-path`；`src-su-wiki-projects`、`src-su-wiki-realms`、`src-su-guide-specializations-2-0-37`。2.0 维护规则中 Projects／Favor／Realm Depth 各有归属，Projects 控制专精等取得。现有记录可证明目标项目与解锁机会成本，**没有完整接取／累计／切换／退款条文**。将其适配成“先选择一个英雄或内容组，再为它推进”属于 UC06 项目建议，不冒称原作可自由点名本项目式英雄并继承全部进度。

**My Party Is Grinding**，`ev-mpig-002-fixed-party-unlock-economy`、`ev-mpig-006-stable-stage-farming-pivot`；`src-mpigdb-class-unlocks`、`src-mpig-guide-n150-2026-08-28`、`src-mpig-guide-dscan-2026-09-02`、`src-mpig-update-rewards-duration-2026-08-31`。正式首周资料里，Coin 购买后续固定职业，Coin／EXP 在 stage clear 结算；攻略建议刷稳定旧关，不反复失败前沿。这是长期固定五人、挂机成长，不能把其人口解锁或刷取时长搬入项目，只用于说明“通用资源选择投入目标”与重复刷简单内容的激励。

**Slotbound**，`ev-slot-003-three-resource-economy`、`ev-slot-013-early-rng-meta-pressure`；`src-slot-review-core-economy-2026-07-26`（Demo 0.3.0）、`src-slot-review-cavalry-2026-08-15`（0.3.2 满树玩家）。Boss／Judgment 给局外资源，局外购买节点；到底到达还是击败、何时记账、败后损失和主动退出未闭合。用途多为 MG04 永久权限，不能从中引入本项目永久容量／稀有度上限。

UC05 比较“先获得可存留、可分配的解锁资源，之后支付给某项内容”；UC06 比较“事先指定推进目标”。两者可组合但机会成本不同，具体切换是否损失进度没有默认答案。原作带城镇、多货币或长期军团的结构不自动成为项目前提。

### Q03-E：发现后完成保存条件

**Kādomon**，`ev-kado-018-misprint-collection-not-build-core`；`src-kado-official-faq-2024-02-20`、`src-kado-guide-misprint`（2024-03-28 至 2025-05-16）、`src-kado-official-040`。Misprint 必须抵达 rest event 或通过 run 带出才永久解锁；此前失败会丢失。**不必一律整局胜利**，奖励为 alternate appearance，战斗机制不变。种子搜寻／重开是玩家收藏实践，不是已定的英雄解锁方式；UC07 将“找到并安全登记”迁为内容资格需另获采用，MG10 本来未附带强制带出收藏。

**Survivor Mercs**，`ev-survivor-mercs-016-extraction-economy-rework`、`ev-survivor-mercs-019-progression-agency-readability-lifecycle`；`src-sm-guide-new-player`、`src-sm-official-update-1-2`（2026-06-08）、`src-sm-discussion-resource-distribution`（2025-06-07 EA）。任务先决、资源门槛、unlock tracker 共同控制选项，1.2 修文字／负进度／旧 tracker／费用和解锁问题。当前指南记录死亡失去全部 Components、大部分 DNA／BD，成功撤离／胜利保存 loot；不能据此说全部失败资源归零，或反向断言所有任务计数永久保存。老兵 Survivor Bonus 是 MG07，另有资格和后续丢失，不混入 MG01。

项目普通失败终局不变；若采用带出类内容，仍需明确保存节点和所得归属，不能从 UC07 自动恢复任意撤离、败后续关或跨局配装继承。

### Q03 未扩成新机制的材料

- TLF 的 `ev-tlf-012-content-dilution-soft-ban`／`src-tlf-official-1-0` 只闭合 Unlock 8 的内容开放层次，不知道靠 XP、胜局还是任务；Reborn 的 Act 1 Boss 后局内支付与账号解锁分开。
- Gods vs Horrors 的 optional unlock-all 没有正常取得公式；Monster Train／Slay the Spire 的氏族／角色 XP、失败经验、账号首通前置未记全。版本／DLC 发布、T&T Set 2、新 Boss 模式、当局 Pact Shards／钥匙入口不补成 MG01。
- Mirror Throne `ev-mirror-throne-005-shard-cursed-trinket-seeding`／`src-mt-demo7`（2024-08-09）确证游玩／高分给 Shards、赛前购买 trinket，属 MG06 配置，买断／败局／退款不全；`src-mt-demo12`（2025-05-21）只有 faction progression unlock 方向，无具体推进条文。`src-mt-achievements` 是 Mirror 的成就，不归 Monster Train，也不证明每项都有内容奖励。
- Milky Way TD 档案有局外资源解锁星球／Hardcore的正式版评论，但没有当前支付、失败结算或解锁算法；其 Demo 通关叙述不替代正式规则，成就数值只是目标，不说明另外奖励内容。
- Guildrun `ev-guildrun-018-boss-token-rewind-transactions`／`src-guildrun-patch-0-5-2` 的到 Boss 给 Token 明说 temporary solution，缺本题内容资格用途；Loot Loop 的 Boss 分段掉钱也没有完整失败结算／内容解锁链，不再拆成独立 MG01 选项。
- 没有本题范围内足够证据确证“仅按现实游玩时长”“只要失败次数增加”“失败和主动放弃无差别”“按角色／阵营熟练度开放完整内容目录”为这些原作共同制度。Neon 给参与单位 XP 不是这种目录解锁的直接证明。
- 固定顺序发放、达标时选奖励、事先选研究目标各不相同；只有资源购买／研究等机制内的选择在本轮说明，普通奖励分配方式和完整界面不因进度来源自动确定。

Q03 研究提出时无新增用户决定；仅补讨论证据、方案和恢复入口，未修改权威／实现／研究原库，未联网、构建、运行或读取玩家存档。

### Q03 用户回应与证据边界

2026-09-10，用户要求按实际推进形成局外解锁进度，正常败局不能因未通关而成果归零，尤其打到 Boss 后惜败仍应有实际推进。已记为 [R14-D03](decisions.md)，对应 UC03 核心原则，不缩成历史记录或仅保留已经领取的奖励；UC02／UC04 及整套 Q03-P01 未随本次采用。

本次无新调研，原作证据、奖励归属和结算缺口不变；用户采用的是项目解锁原则，不反向补全 Neon、Vivid 等原作合同。计量／节奏／分配及主动放弃等结算未定，不从 Boss 惜败例子推出血量或伤害折算公式。未修改权威、研究原库或实现，未进行运行或体验验证。

## R14-Q04：解锁成果的分配与选择时点

日期：2026-09-10。沿 [R14-D01–D03](decisions.md)，本轮只问：**按实际推进积累足够后，下一项内容由系统安排还是玩家决定，选择发生在积累前还是积累后？** 普通失败仍有实际解锁推进、基础组先可玩、按需成套开放新机制保持。不重开永久战力，不讨论 Boss 血量换算或 MG08 难度解锁表。

### Q04 实际检索范围

全程只读已有语料，未联网，未修改原库或读取玩家存档。延续 Q01–Q03 已核对的现行基线，未重新核查或运行实现。

- [深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)全部 780 条。主查询字段值：unlock／meta.?progress／research project／research target／解锁／局外／研究目标／进度；全字段 85、核心字段 69。核心为 rule_support、practical_support、disagreement_or_limit、mechanism、engine、state_resource、trigger、scope、payoff。全字段独有 16 条回读后主要归项目推演、局内规则。
- 核心扩查 progress／experience／单词 XP／research 新增 109 条，筛读 ID／领域清单，排除局内经验、养成、一般体验和实现描述。另查 unlock 与 random／choos／select／order／sequen／purchas／buy／research／track／level 在前后 100 字符内邻近，40 条；拼接字段的邻近命中只用于召回。
- 对 16 条相关／邻近深记录定点回读规则、观察、限制、资源／触发／归属，解析出 57 个关联来源 ID；[来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)回读 13 个关键完整索引块。不是重读 57 份外部网页，也不是将所有关键词命中视为独立机制。
- [档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/)全部 56 份；解锁／unlock／局外／跨局／metaprogression／meta.progression／研究项目／研究.*(资源或解锁)／research.*(unlock或project) 命中 37 份、120 行。回读直接相关段落和来源；计数含 stunlock、局内升级和“未知”表述，不等于 37 款有本题机制。泛用“研究／project”会受档案固定标题干扰，未作为有效覆盖计数。
- [发现层](../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)全部 106 条，按字段值而非 JSON 键名筛 unlock／meta.?progress／meta.?currency／permanent／research／project／mastery／解锁／局外／跨局／研究／熟练，8 条；与 choos／choice／select／random／purchas／buy／spend／order／track／branch／target／level／tier／point／选择／随机／购买／顺序／分支／目标／等级／经验 交叉，5 条。核心 mechanism、input_state、trigger、scope、output_payoff、scaling_model、limits_safeguards、build_role 交叉也是 5 条。
- 发现层宽候选完整去向：e021／e074 是局内商店等级或奉献，e060 是局内元素组合，e097 是选禁／选手熟练；e040 是长期挂机且无分配合同；e039／e063 是不足证据的研究评估。交叉没有命中的 e075／s050 仍回读，只有 Gods vs Horrors 横向解锁承诺，无奖励分配流程。另由档案核对的 Milky Way TD e041／s022 商店结构不能补证其三选一。
- 回看两层 synthesis，没有额外可闭合普通解锁分配的规则。各查询重叠，不相加；筛查覆盖不等于穷尽所有机制，下面区分原作、相邻结构和项目对照。

### Q04-A：作者安排接触阶段，与固定／随机派发的缺口

**The Last Flame**，ev-tlf-012-content-dilution-soft-ban；src-tlf-official-1-0（2025-01-09）、src-tlf-steam-indepth-2026。官方 1.0 回顾与档案记录大量 hybrid／ethereal 内容放到 Unlock 8，Reborn 在 Unlock 8/8。能证明作者把复杂内容放到后段，不能证明普通英雄都按固定经验队列发放，也没有完整顺序、候选菜单或结算公式。UA01 的完整固定顺序是项目适配。

**Vivid Knight**，ev-vivid-020-unlock-pool-dilution-community-failure；src-vivid-guide-boss（v1.1.1–v1.1.5）、src-vivid-guide-easy-start、src-vivid-guide-symbol-tips。这里的 random 是开放后的局内奖励／商店，不是局外随机给下一项。某篇 Boss 攻略建议不开放妨碍目标组合的 Gem 家族，另一篇看重广泛接触；前者有读档操纵背景，无统计。可讨论自由选择下故意少开内容的动机，不能推为普遍行为或补全购买菜单。

原库没有“普通推进达标→随机永久新英雄”的完整成例，UA02 明确为项目制度对照。固定／随机决定下一份奖品，不改变 D03；TLF 阶段编号不能成为重新要求通关的理由。

### Q04-B：有限候选选择——当局成例与迁移边界

**Mechabellum**，ev-mecha-004-reinforcement-offer-pivot；src-mecha-wiki-reinforcements-2026（维护 Wiki，Last-Modified 2026-06-17，访问 2026-09-02，1.11 时期）、src-mecha-official-1-11、src-mecha-wiki-sledge-marksman-2026。指定补员窗口出现四个候选，选中单位及其后续购买资格在本局开放；可以跳过，补员 supply 随回合增加。候选受已有单位和某些专精影响，完整权重／例外未知。**全是当局援军，不是账号永久开放。**

**Milky Way TD 仅为线索**：[档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/milky-way-td.md)第 31／32 行正式版个人长评分别谈击杀填 XP 条升级三选一，以及奖励条满后在新单位／单位强化／技能强化中选择，部分卡限量。英文 recommendation 161900081、韩文 recommendation 175815013，App 2837330；档案访问 2026-09-04，评论日期和具体补丁未存。该游戏为 insufficient-evidence，明确没有登记深 source／evidence id；不能编 id 或用发现 e041／s022 商店介绍补齐三选一。

UA03 借有限候选保留选择、降低阅读负担的结构，将奖品改为永久内容资格；这是项目迁移，不宣称原库验证了“局外满条选一个英雄”。R08 的 RW01／RW02 是局内分类奖励，不自动赋予局外选择权或刷新权。

### Q04-C：成果存留后再分配——完整目录与资源购买

**My Party Is Grinding**，ev-mpig-002-fixed-party-unlock-economy；src-mpigdb-class-unlocks、src-mpigdb-heroes 与 src-mpig-guide-n150-2026-08-28／src-mpig-guide-ed-2026-08-31／src-mpig-guide-dscan-2026-09-02。正式首周：起始 Knight，Orb Circuit 职业节点有父节点和 Coin 成本，Warrior 1,500、Assassin 7,000、Archer 20,000、Mage 50,000；职业和小属性节点竞争预算，攻略安排推进路径及优先级。它是长期固定五人队、直接取得成员并混合永久战力，不是扩展随机招募资格。可借先存预算、后花给当前可达目标，不能写成无前置、可任意顺序购买全部英雄。

**Backpack Hero**，ev-backpack-hero-031-story-quick-game-layer；src-bh-official-story-testing（2023-10-17）、src-bh-official-town-economy（10-25）、src-bh-official-town-layout（11-08）。Story 用任务、地牢物品转换 food／material／treasure、研究逐步开放内容；完整目录、前置图、失败结算、接取和退款未闭合。Quick Game 全解锁是另一入口，不是普通进度满后任选菜单。项目不迁城镇或多材料树。

UA04 的“一次等额机会、从所有当前合资格内容中选一项”是简化的项目对照，没有直接原作流程。UA05“可储蓄预算、不同目标有不同价格”有上述相邻结构；二者同样由玩家面向完整合法目录选择，后者增加现在开小目标还是攒大目标的取舍。不自动共用 R09 局内金币，也未决定每次失败发多少资源。

ev-mba-005-character-experience-passive-investment 的共享经验后分配属局内培养；ev-mirror-throne-005-shard-cursed-trinket-seeding／src-mt-demo7 的 Shard Shop 是下局开局饰品配置；Slotbound 局外树多数为永久权限，Loot Loop 多数为永久数值节点。均不确证本项目普通内容的购买合同。

### Q04-D：事先研究目标，与所玩主题决定进度归属

**Siralim Ultimate**，ev-siralim-ultimate-002-realm-progression-axes、ev-siralim-ultimate-003-specialization-player-layer、ev-siralim-ultimate-020-hellknight-progression-path；src-su-wiki-projects、src-su-guide-specializations-2-0-37。Projects 控制部分专精等内容访问，Favor／Projects／Realm Depth 是不同账本，攻略随取得路径替换过渡构件。没有完整接取菜单、同时项目数、切换／暂停保存及普通推进是否只投给激活项目的条文。UA06“先选英雄目标，之后正常推进专门填它”是项目方案，不把任何阵容都可推进、切换全返写成原作事实。

**Neon Auto Party**，ev-neon-auto-party-017-failure-xp-growth-rework；src-nap-discussion-playtest-feedback-2024-2025 的 2025-06-24 开发者回复：run end 按 cleared waves 给参战单位 XP，包括 failed run。能说明本局参与者决定成果归属；奖励实际为永久单位成长，不证明 XP 开新英雄、主题奖励目录或分摊算法。

**Mirror Throne**，[档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/mirror-throne.md)与 src-mt-demo12（2025-05-21）只登记 faction progression unlocks，没有独立经验条、按所玩阵营归属或玩家选轨的具体合同。UA07 主题轨道是项目推演，不采用永久英雄属性；与 UA06 的差别是实际所玩内容决定进度去向。R06 随机组池会影响轨道可推进性，混合队／跨组／功能位的归属不能猜定。

### Q04 未展开为独立制度的相邻材料

- Skull Horde Adam 和 Just King 的特定玩法目标对应特定奖励，属 UC04，不因比较分配而进入初期。
- Backpack Dungeon Ascension、Neon Stress 逐级开放是 MG08 挑战资格，不作普通英雄队列证据。
- Slay the Spire、Monster Train 本库没有角色／氏族 XP 到内容奖励目录的完整表，未凭常识补固定顺序或分轨。
- Survivor Mercs 的前置、资源、tracker 与池稀释提醒可达性／信息成本，未重建菜单；ShapeHero Factory 起始 Research 的反复搜索是永久权限影响局内起手。
- 付费包、版本发布、已拥有目录编辑、Gods vs Horrors unlock-all、任意撤离／安全带出沿原归属，不扩成普通累计奖励方案。缺流程证据的发现层线索保持缺口。

Q04 研究提出时形成 [UA01–UA07／Q04-P01](proposals.md)，多数精确流程为项目对照或局内结构迁移，不因游戏名就升级为原作事实。提出时 decisions.md 保持 D01–D03；仅改讨论区，未改现行权威、研究原库或实现，未进行运行或体验验证。

### Q04 用户回应与证据边界

2026-09-10，用户同意 UA01＋UA03 分工，记录 [R14-D04](decisions.md)：新玩法组按作者安排次序开放，日常英雄扩展提供有限候选选择，取得仍遵守 D03 的实际推进与正常败局保留。初期不增加独立局外货币或游玩前研究管理；具体节奏和候选参数未定。没有新增调研，用户采用的是项目方案，不反向补齐 TLF、Mechabellum 等原作的永久解锁流程，Milky Way TD 仍为不足证据线索。完整机制及缺口保留，未修改权威、研究原库或实现。

## R14-Q05：更高挑战资格的取得、保留与归属

日期：2026-09-10。承接 [R14-D01–D04](decisions.md)，本轮讨论下一局可以选择哪些长期挑战，以及换英雄／内容组是否沿用资格。普通内容解锁的实际推进、正常败局积累与 UA01＋UA03 分配保持；不重开 MG01，不制定敌人词条表、收益倍率或排行榜。

### Q05 恢复核对与检索范围

[核心权威](../../../gameplay-design/tower-autobattler-core.md)将英雄或 difficulty 的 meta unlock 纳入完整 Alpha，但没有难度档表或资格流程；起始英雄仍是可替换的普通名册成员，不是不可替换的指挥官身份。[R13-D01](../journey-and-pressure/risk-and-reward/decisions.md)一般 RK01／RK02 属单场自选加码，本题为跨局可选挑战。R10 外层形式开放，不以现行三主题区域数确定新难度档数。沿 Q01 的实现静态基线，不重新检查代码或读取存档。

- [深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)全部 780 条。主查询 difficulty／ascension／covenant／corruption／stress／challenge／ladder／prestige／难度／挑战／资格／段位／高难，全字段值命中 120、核心 87。核心为 rule_support、practical_support、disagreement_or_limit、mechanism、engine、state_resource、trigger、scope、payoff。
- 全字段独有 33 条筛读，补回 ev-survivor-mercs-001-current-version-modes，其余多为构筑、单场机制、项目问题或版本背景。核心扩查 elite／cursed／brutal／omen／instability／endless／mode／rank，排除主查询后新增 133 条，筛读完整 ID／领域；大量为局内 rank、elite 节点、普通模式和攻击词汇。
- 定点回读 26 条直接／邻近深记录，检查规则、实践、限制、版本／来源、引擎／触发／作用域；[source-index](../../../web/game-mechanics-atlas/research/deep/source-index.md)回读 20 个关键索引块。不是重新读取全部外部原文，也不把关键词命中相加为精读数量。
- [56 份档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/)两次全量 rg -n/-l -i。宽筛“难度|爬阶|Ascension|Covenant|Corruption|Stress|Ladder|Hardcore|Nightmare|Prestige|Custom|Challenge|挑战|解锁|unlock”，46 份／306 行。精筛“逐级|逐档|(难度|挑战).*(解锁|保留|记忆|选择|归属|掉级)|((解锁|保留|记忆|选择|归属|掉级).*(难度|挑战))|Difficulty|Ascension|Covenant|Corruption [0-9]|Omen|Stress|Ladder|Hardcore|Custom Mode|Challenge Climb|Tournament|victory.ribbons|完成记录|奖章|成就”，29 份／137 行。回读后 BPD、T&T、GvH、Neon、SAP、Astronarch、STS 提供不同程度的直接结构，另核 Survivor Mercs、Kādomon、Milky Way TD、Monster Train 等归属缺口；原始命中含攻略标签、物品名和匹配模式，不能称每款都有完整资格合同。
- [发现层](../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)全部 106 条。字段值筛 difficulty／ascension／covenant／corruption／stress／challenge／ladder／rank／hardcore／endless／难度／挑战／爬阶／升阶／段位／无尽，4 条；与 unlock／progress／clear／win／loss／lose／fail／select／choos／account／profile／mode／解锁／进度／通关／失败／选择／账号／模式 交叉仍 4 条。核心 mechanism、input_state、trigger、scope、output_payoff、scaling_model、limits_safeguards、build_role 交叉仅 e057。完整去向：e009／s006 和 e095／s063 是供给池，e057／s035 是无尽敌群压力，e075／s050 只给横向解锁及难度应用建议；均无新长期挑战资格合同。
- 两层 synthesis 关联检索未增加具体开放／归属流程。检索范围和缺口如实保留，不宣称穷尽一切机制。全程未联网、修改原库、构建、运行或操作玩家数据。

### Q05-A：逐档、关键通关与直接选择

**Backpack Dungeon**，ev-bpd-014-endless-progression-reward-mismatch；src-bpd-discussion-endless-reward-2026-08-26（正式 2.4.x 时期开发者回复）。Ascension 仍逐级解锁，普通路线主要完成／Ascension 奖励与 Endless 分开；Endless 当前不增加该类奖励。mod debug 是快速开放的外部替代，不是普通权限。能说明逐级开放，不能补全一胜加一、失败保留／降档、账号／职业归属与低档选择合同。DG01 的“通过当前已开最高档开放下一档”须标项目规则。

**Survivor Mercs**，ev-survivor-mercs-001-current-version-modes；src-sm-guide-new-player（2026-04-30 更新至 1.0）。官方指南将 Standard Boss clear 与 Elite access 关联；Standard／Elite／One-Man Army 有不同模式边界。可确认基础通关后开放另一进阶入口，不扩大为所有区域全清、额外门票、每次进入重交资格或全模式统一条件。对应 DG02；普通内容的败局积累与该挑战条件分开。

**Gods vs Horrors**，ev-gods-vs-horrors-018-ladder-casual-failure-contract；src-gvh-official-qol-2025-05-19（1.0 后、1.1 前）确认 Casual 难度从开始开放。src-gvh-discussion-ladder-casual（2024-07-24 Demo 讨论及开发者回应）反映掉级抑制实验的动机，src-gvh-review-ladder-194305497（2025-05-06 正式首周评测）补充 Ladder 失利／掉级观察。固定每输降一档、保级点、重开等细则未保存。DG03 借直接选择，DG05 为不同的 MG09 长期段位比较，不将历史 Demo 与正式版细节混成一张规则表。

**Super Auto Pets**，ev-sap-009-arena-difficulty-rework；src-sap-official-update44（2025-12-19）。Arena 敌队强度从随经验隐式变化，改为明确的 Normal／Hard／Super Hard／Cursed 选择，目标是选择清楚且强度稳定。它支持“玩家选难度”与账号经验分开，不证明新存档所有档都已开放，也不证明全部自适应难度有害。

### Q05-B：进度开放与独立挑战规则

**Neon Auto Party**，ev-neon-auto-party-013-stress-rework；src-nap-playtest-0-4-1-stress-machinist（2025-02-08）从很多 sliders 改为 level-by-level unlocking，并有敌人 Stress skills；src-nap-playtest-0-4-3-demo-bridge 修复 second level 的 Stress levels 不解锁。可确认按关卡／层次逐步开放的设计，无法补成赢当前 Stress 开下一档、每关各有独立最高难档，或普通败局累计便开下一档。更高 Stress 的永久单位 XP 另属成长收益，不等于 MG01 内容进度。DG04 累计实际推进开放高难是项目对照，并非已证原作规则。

**Astronarch**，ev-astro-013-c20-omen-axis-rework；src-astro-official-1-5（2021-05-13）。v1.5 加入四个可独立于 Corruption 使用的 Omens，把旧 C20 队伍人数限制移入 Omen，并修改 C20。主难度与特殊考题可以分开选择，不必将某一限制强绑终档。Omen 首次开放条件、默认开放数、组合资格、胜利是否推进 Corruption 未记录完整；DG06 借两类挑战权限并列的结构，不补满具体词条或“自定义一律全开”。

**Slay the Spire**，ev-slay-the-spire-002-mode-difficulty-boundary；src-sts-official-ascension、src-sts-official-custom、src-sts-official-final-act 为 2018 EA 的不同模式／版本节点。可确认 Ascension、Custom 与 Final Act 应分清，资料未保存本题精确资格流程；不凭常识填角色通关数→钥匙→心脏、逐角色爬阶、Custom 是否可推进标准资格。

### Q05-C：资格归属与成绩记录不能互推

**Tales & Tactics**，ev-tnt-015-set2-difficulty-state-separation；src-tnt-official-2-0-14（2026-08-26 官方补丁）。Set 2 使用独立 Challenge Climb progression，提示旧 Set 1 熟练度不能直接当作新技能集的掌握；同时修复敌人此前错误地从所有难度抽取的问题，不能把全部新 Set 挫败归咎于玩家没学会。Tournament tier-up 使用固定难度，与 Challenge Climb 分开。可支持 DS02 按实质不同的内容 Set 分开，不推为每个英雄、羁绊或本局随机组组合分别爬阶，也不是未来任意版本更新均可清进度的授权。

GvH 档案另提 Casual 按敌人记忆难度，但没有闭合记的是上次选择还是最高解锁，以及所有敌人的资格关系；这是设置／记录线索，未据此另造按 Boss 分开解锁的制度。Neon 第二关 Stress 解锁修复同样不证明按关卡分别保存最高档。

**全账号共享 DS01、按英雄／领袖分别解锁 DS03**：目前没有足够完整的原作合同，均明示为项目对照。Monster Train 的 ev-monster-train-008-pact-shard-risk-budget、src-mt-wiki-covenant 只确认 Covenant 范围与部分考题；双氏族胜负表是成绩信息，不证明资格按主氏族、副氏族或组合分别锁定。STS 高难攻略同样不能补角色存档规则。

**Kādomon**，ev-kado-020-keeper-relic-and-variable-boss-package；src-kado-official-100（2025-04-07）确认 Ascension、Victory Ribbons、Kādo Guide 存在，不证明每只单位都必须各通一次才能开放高难。[Milky Way TD 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/milky-way-td.md)的各地图 Hardcore 成就也不是各地图独立爬阶证据，且该游戏没有 deep source／evidence id。

本项目可以共享可选难度，同时记录不同阵容的完成成果；这两层不矛盾。是否需要全英雄奖章目录未定，不能由 MG10 轻量记录自动扩为逐英雄资格门槛。D01 已保留普通难度可选，本轮不把强制高难或失败掉级当默认。

### Q05 相邻机制去向与限制

- **Loot Loop**，ev-loot-loop-011-single-prestige-final-map-gate：Prestige 重置并加快收入／成长后开放最终地图。一次性及约十分钟重玩是评测切片，精确保留清单不全。属于 MG11 的重置／永久成长结构，未纳入初期，不重新提交为必选挑战入口。
- **Dungeon 100**，ev-d100-001-past-build-boss-ladder、ev-d100-016-boss-deck-preview-challenge-loophole：已完成构筑或 Classic 存档形成后续可挑战对象。历史构筑 Boss 继续 MG12 旧保留；存档替换、职业锁与复制边界未补定。
- **同局无尽**：ev-gods-vs-horrors-001-version-mode-boundary／src-gvh-official-update-1-1-2025-06-19 的 Infinity 是 completed run 后 continuation，不说成首通永久开放新的 Infinity 开局；BPD Endless 同样不等于下局资格。项目 G03 已保留后续无尽，不在本题重选。
- **Siralim**，ev-siralim-ultimate-002-realm-progression-axes／src-su-wiki-realms／src-su-wiki-realm-depth：Depth、Instability、Fortune、Favor、Projects 是不同轴，未保存各项完整首次开放、败局回退与旧深度访问合同，不从名称补共享资格。
- Monster Train 的当局 Pact Shards 与 Ring 9、TLF 精英与路线资源、GGM Glory 单局失败预算、Guildrun 目标／红 Rift／临时 Token、Mirror 的开局诅咒饰品分别归 R13／流程／当局规则，不作为永久高难门票证据。
- T&T Brutal5 与 ShapeHero Factory Ascension9 的高难压力可供难度内容参考，不定义下一档解锁；SAP 每日固定敌队是挑战供给，资格／重试／奖励合同未闭合。PvP MMR／赛季段位、付费内容包、普通物品 Omen 命中不扩入本题。
- 20 个定点索引块覆盖 SAP Update44、TLF1.0、T&T2.0.14、Astronarch1.5、Guildrun modes0.5.6、BPD Endless回复、Survivor官方指南、GvH QoL／Infinity／Ladder讨论／首周及busywork评测、STS三个历史节点、MT Covenant、Neon两个Stress补丁、Siralim realms与realm-depth。索引读取不等于重新联网核实当前版本。

Q05 研究提出时形成 [DG01–DG06／DS01–DS03／Q05-P01](proposals.md)，暂荐 DG01 普通逐档＋DS01 资格共享，普通内容仍按 D03／D04 推进。提出时无新增用户决定，原作有限证据与项目建议分别记录。仅更新讨论区，未修改权威、研究原库或实现，未进行运行或体验验证。

### Q05 用户回应与证据边界

2026-09-10，用户明确选择“01 02 06 DS01”，记录 [R14-D05](decisions.md)：四项机制采用，包含 DG02 基础完成后开放进阶入口与 DG06 独立特殊规则，不再将它们只作后续参考。普通内容败局积累保持；具体模式、挑战条件／档数、规则组合及完成资格判定仍待设计。本次无新调研，用户采用的是项目机制，不补全 BPD、Survivor Mercs、Astronarch 等原作的全部资格合同。完整比较、被采用范围与原推荐差异均保留，未改现行权威、研究原库或实现。

## MG10 轻量图鉴／完成记录：归属复核

日期：2026-09-10。核查是否存在尚需用户决定的全局机制，避免将已采用的轻量记录拆成字段、UI 或成就清单逐项审批。本次仅复用和检索已有本地调研，未联网；以下归属判断不是新增用户决定。

### 本次检索范围

- 深证据全部 780 条，发现层全部 106 条，全部 56 份档案及相关来源索引。主查询为 achievement／bestiary／codex／encyclop／compendium／collection／collectible／misprint／completion／journal／图鉴／收集／收藏／成就／完成记录／异色／发现／研究。
- 深证据全字段值命中 101、核心规则／实践／限制／机制／引擎／资源／触发／范围／收益字段命中 61；全字段独有 40 条筛读，多为来源名与研究文字。发现层 3 条 e039／e046／e063 均为误召回，无新增图鉴规则。
- 档案广搜 201 行／52 份；移除收集／发现／研究等泛词后，精筛 57 行／24 份并读回命中。定向回读深记录 13 条、来源索引完整块 13 个。计数不相加，不代表精读全部原作或穷尽所有机制。

### 不同机制与实际归属

| 机制 | 原库依据及证据限制 | 本项目归属 |
|---|---|---|
| 完成标记与资料回顾 | Mirror Throne 的 src-mt-achievements 列 24 个公开成就，包括不同模式与特定饰品通关；src-mt-demo10 确认 Journal，ev-mirror-throne-005-shard-cursed-trinket-seeding 有部分饰品通关条件。没有完整奖励合同，不能说全部成就纯记录、绝无奖励。 | MG10 已采用轻量方向，具体记录和 UI 留内容／流程，不另立单选。 |
| 资料解释机制 | Dungeon 100 的 src-d100-official-2023-08-10 增补三合一奖励及 collection UI 的 synergy／card descriptions；Magicbook 的 ev-mba-014-combat-report-source-attribution／src-mba-review-readability-2025-04-05 是首发时期玩家对 Codex／HUD 规则缺失的反例，后续修复不能忽略。 | 信息完整性沿 R12，不把图鉴未点亮当作遮挡必要规则的理由。 |
| 反复遭遇后研究并逐层揭示信息 | 本次没有找到“遭遇／击杀次数→知识等级→敌情披露”的直接规则。Siralim Projects 与 Backpack Hero research 控内容访问，外部 Wiki 的 bestiary／compendium 也不是游戏内研究。 | 不凭相近词新增初期研究系统；故事／外观揭示属于具体资料内容。 |
| 稀有收藏要安全带出 | Kādomon 的 ev-kado-018-misprint-collection-not-build-core，src-kado-official-faq-2024-02-20／src-kado-guide-misprint：Misprint 到 rest event 或通关才永久开放，此前失败失去；战斗机制不变，攻略有种子与重开实践。 | D01 未批准的额外收藏玩法，保留参考，不改变 D03 普通内容败局积累。 |
| 特定玩法成就开内容 | Skull Horde 的 ev-skull-horde-007-adam-corpse-build／src-skull-guide-achievements 将 Adam 与复生相关行为关联，但并列／替代、同时／累计及通关条件不全；Just King 的 ev-jk-008-hero-upgrade-token-sale 只保存 Lv3 milestones 与 unlocks／builds，缺完整表。 | 归 MG01 的 UC04 候选，不由 MG10 自动采用，也不逐项审批成就。 |
| 完成阈值给永久能力 | My Party Is Grinding 的 ev-mpig-009-partywide-star-chart-allocation／src-mpigdb-star-chart：星座节点完成度 33／66／100% 给 keystone，并有全队属性／经济节点。它是成长树完成度，不能包装为图鉴收齐加属性。 | 永久成长不在 D01 初期范围，不重开或记成永久禁止。 |
| 已发生事实正确记账 | Skull Horde 的 ev-skull-horde-017-status-stack-attribution-lifecycle／src-skull-official-v1-026 修复状态与复生成就归因；补丁不能证明所有目标败后保留或都发内容奖励。 | 记录的正确性要求，不是需用户另选的玩法。 |

Kādomon 的 ev-kado-020-keeper-relic-and-variable-boss-package／src-kado-official-100 仍只证明 Ascension、Victory Ribbons、Kādo Guide 存在；D05 共享资格不能倒推逐英雄门槛。Milky Way TD 的地图成就、Monsters Auto Battler 无描述等级成就、Magicbook Workshop 排行榜边界，都没有补出普通图鉴奖励合同。

**归属结论**：MG10 已有基础方向，没有发现必须另开全局选择的缺口。字段、完成标记和界面留内容设计；新奖励、带出、全英雄目录、逐层研究均不能自动加入。此结论只针对现有授权与已存证据，不断言所有原作都没有复杂图鉴玩法。D01–D05 不改，R14 其他真实未决项见下列 Q06 及 [方案](proposals.md)。

## R14-Q06：普通内容进度的计量依据

日期：2026-09-10。承接 D03 正常败局仍有内容积累、D04 分配与 D05 挑战资格分工，只比较实际推进怎样换成本次积累。不是重新表决失败是否有成果，也不决定主动放弃／重开或具体公式。本轮沿 Q01 的实现静态基线；核对核心权威的 Alpha meta unlock 范围，没有重新读取玩家存档或执行游戏。

### Q06 全库检索范围

- [深库](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)780 条、[发现层](../../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)106 条逐记录扫描字段值。宽筛深全字段 159、事实相关核心 70；发现层 7，无新增可靠跨局计量合同。核心为 rule_support／practical_support／disagreement_or_limit／mechanism／engine，后两者仍检查事实支持，不把 project_question／transferability 等项目推论当原作。
- [56 份档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/)全量扫描，宽筛 48 份／194 行，聚焦结算与刷取 18 份／64 行，回读直接相关正文及 [来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。命中含战斗结算、Grind 名称、普通保存等噪声，不是机制数量。
- 主筛：run end／failed run／failure 与 XP、experience、progress、reward／cleared waves／arrival reward／partial boss progress／abandon／restart／save／extract／retreat／score／farming／grind／结算／到达奖励／实际推进／放弃／重开／刷取。
- 成果计量补筛核心 34 条：health、HP 与 segment、threshold、gold、coin、reward；reach boss／boss arrival、token；kill XP、coin、gold；survival time、minute、reward、progress；score／distance／time played／play time／completed 或 cleared wave、stage、floor。该轮补回 Loot Loop 血段金币和 Guildrun 到达 Token，各查询重叠，不能相加为精读量。
- 全部为现有语料检索，未联网重读外部原文。原库偏重构筑与战斗，许多计量、钱包及失败／退出合同未保存；没有把 60 多款初始游戏的商店宽搜当作 60 多套可靠计分规则。

### Q06-A：完成战斗与到达进程

**Neon Auto Party**：ev-neon-auto-party-017-failure-xp-growth-rework；src-nap-discussion-playtest-feedback-2024-2025 中 2025-06-24 开发者回复明确 at run end participating units gain XP for each cleared wave even on failed runs。src-nap-main-0-5-2（2025-06-27）调整 XP scaling，期待首局推进至某 level 后三分之一可让带入单位升级、帮助接下来数局，但不证明预期已实现。收益是永久单位等级，PG01 只迁结算结构为普通内容资格；主动结束、途中成员变化、重复波次和 Boss 残血贡献不全。历史失败视频只展示扣 life 后同局继续，不证明每败一次立即发永久 XP。

**Guildrun**：ev-guildrun-018-boss-token-rewind-transactions；src-guildrun-patch-0-5-2（2026-07-28 Demo 0.5.2 官方）明确 0.5.2 grants two Boss Tokens on reaching each Boss as a stated temporary solution。同记录另引 src-guildrun-patch-0-5-1、src-guildrun-wiki-modes-0-5-6。计量事件是抵达，不能补成击败；首次限定、钱包跨局性、败／退出／回溯重复领取合同未知。两个 Token 是原作临时版本值，不是项目数值。

**Vivid Knight**：src-vivid-official-1-1-11（2021-06-07 官方），档案 vivid-knight.md 的规则调整段记录 Boss defeat reward 改为 arrival reward。无独立深记录专门闭合此奖励；ev-vivid-020-unlock-pool-dilution-community-failure 虽引该补丁，也不能当完整奖励合同。奖励类型、数量、跨局性、首次条件仍缺。Guildrun 和 Vivid 支持“抵达可被认可”，PG02 的每局进程位置结算法为项目适配，不写成原作按最高层数发 XP。

### Q06-B：Boss 尚未击败，也可有中途贡献

**Loot Loop**：ev-loot-loop-015-boss-coin-boundary-rework，明确 Bosses were changed to retaliate and drop gold at Health segments。来源 src-loot-loop-official-demo-abilities-2026-01-19（历史 Demo 官方）、src-loot-loop-hakimodo-review-2026-07-19（正式版长评）、src-loot-loop-official-patch-1-1-2026-08-12（修 Boss 金币飞出屏幕）。这是实际 Boss 血段奖励的先例，不再笼统声称没有部分推进奖励。

金币／宝石参与永久技能树，不能迁成已证内容解锁点。血段数、阈值、金币量、回血后再次越段／复活／重复挑战去重、未拾金币及死亡／主动退出钱包均不全。PG03 将未赢战斗的有限完成度用于内容积累是项目方案；按血段／阶段或连续比例的取舍可以讨论，但没有原作证据证明按累计伤害连续线性折算败局内容进度。

相关 ev-loot-loop-014-death-win-settlement-rework；src-loot-loop-discussion-winlose-same-tick／src-loot-loop-discussion-final-stage-timing 曾报告死后弹道杀 Boss 双结果／双奖励，1.1 明确队伍死亡不再判胜。胜利资格与失败能否取得已有资源应分开，补丁没有说失败金币全部清零。

### Q06-C：行为数量与综合评价

**Milky Way TD**：[档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/milky-way-td.md)正式版英文 recommendation 161900081 记录击杀填 XP 条、升级三选一。是局内成长，仍为 insufficient-evidence，无 deep evidence/source id；不是每次击杀跨局记账的证据。PG04 迁移计量结构须明示，发现层商店来源不能补全其结算。

**Mirror Throne**：ev-mirror-throne-005-shard-cursed-trinket-seeding；src-mt-demo7（2024-08-09）登记游玩／高分取得 Shards、诅咒饰品提高难度和 score、赛前 Shard Shop；src-mt-achievements 另有具名通关目标。score 组成、只认历史最高增量还是本次总分、失败比例均不完整。src-mt-review-party-204135472（2025-09-12，Patch 1.1 后）抱怨退出丢开局 Jewels，但 Shards 与 Jewels 是否同物未证，不能当统一退款表。PG05 只借“综合表现影响成果”结构，原作具体速度／伤亡／伤害指标不猜填。

### Q06-D：奖励依据会改变刷取与深入的动机

**My Party Is Grinding**：ev-mpig-006-stable-stage-farming-pivot，Coin/EXP settle on stage clear；src-mpig-guide-n150-2026-08-28、src-mpig-guide-dscan-2026-09-02、src-mpig-review-farm-equipment-233812945、src-mpig-update-rewards-duration-2026-08-31。攻略让玩家停止反复失败的前沿挑战，刷稳定旧关；ev-mpig-015-automation-failure-guards 另有十连败自动退关并关闭挑战、Act Boss 箱满停止。它是长期挂机队伍成长，提供安全刷取动机的反例，不给本项目 Boss 惜败零进度背书；最优效率仍受时长、掉率和账号加成影响。

**Survivor Mercs**：ev-survivor-mercs-002-operation-objective-extraction-loop、ev-survivor-mercs-006-priest-stim-deep-extraction-build、ev-survivor-mercs-016-extraction-economy-rework、ev-survivor-mercs-019-progression-agency-readability-lifecycle。src-sm-discussion-extraction-economy（2023-10-12）玩家计算首 Boss 后撤效率较高，开发者认可并规划后段稀有收益；后续 src-sm-official-operation-overhaul-0-9-8、src-sm-official-final-drill-0-15、src-sm-official-release-1-0、src-sm-official-update-1-2 继续调整。旧版早撤动机可提醒本项目供给曲线，不能写成现版通用最优策略。

该作 src-sm-guide-new-player 更新至 1.0，档案记死亡损失全部 Components 和大部分 DNA／BD，撤离／胜利保留 loot；属于撤离资源博弈，不推定所有任务或普通内容失败都清零。Retreat／Abort 细表不全，本项目不因此添加主动撤离、败后续关或先用退出惩罚解决刷取。

### Q06 相邻机制归属与未闭合合同

- 酒馆战棋 ev-hsbg-006-buddy-progress-softens-loss／src-hsbg-blizzard-buddies-29-6 的赢平减 Buddy Button 成本 3、输减 2，是局内伙伴取得的输赢差异；不自动追加项目局外胜利倍率，不与 2022 伙伴实践拼成同一公式。
- Kādomon Misprint 安全带出仍归 MG10／UC07 旧参考，特色目标与首次里程碑归 UC04／UC02 候选；没有为本题自动采用。
- Tales & Tactics 的 ev-tnt-014-brutal5-restart-highroll／src-tnt-official-1-4 明确降低 Leader 血量加成以减少 Act 1 restart until you high roll；后续 src-tnt-steam-challenge-climb-2024／src-tnt-steam-brutal5-carry-2026 含重开、save/load 或首店操作实践。Slotbound ev-slot-013-early-rng-meta-pressure 也有重开／刷低难报告和不同意见。它们解释动机，不证明惩罚所有退出是正确方向。
- Guildrun 同条 Token 记录修 Emergency Rewind 重充错误累积 Shards 等问题，但没有完整合法重复领取合同。普通保存／续玩修复也不能证明关闭游戏等于放弃、保存可重复领奖或退出一律零成果。
- 仍无直接完整合同：Boss 累计伤害／最低血量按比例折普通内容进度、纯挂时长或失败次数自动解锁、主动放弃与战败全等价／全清零、首次突破与每局重复积累的通用原作规则、Boss 回血／复生／复战的完整计量去重。

研究提出时形成 [PG01–PG05 与 Q06-P01](proposals.md)：完成遭遇、每局抵达进程、未完成战斗的有限贡献、行为数量、综合表现是不同计量依据。原作对象／版本／缺口保留，建议以 PG01 为主要来源、PG03 有限补充；当时为 Agent 推荐，decisions.md 为 D01–D05；后续用户采纳见下。主动放弃／重开、特殊挑战资格判定及新解锁生效等仍未定。未修改权威、研究原库或实现，未进行运行或体验验收。

### Q06 用户回应与证据边界

2026-09-10，用户同意参考成熟机制即可，记录 [R14-D06](decisions.md)：PG01 基础积累＋PG03 有限完成度补充，具体公式及常规适配不再逐项讨论。本次无新调研；Neon 的永久单位 XP、Loot Loop 的战中血段金币等仍是不同奖励对象和部分合同，不因项目采用而成为已验证的普通内容解锁公式。原作退出、回血／复战计量等缺口保留，具体项目流程后续在已定方向内细化；未改权威、原库或实现。
