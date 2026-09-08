# M06 遗物模型：已有证据

## M06-Q01：遗物的累积持有与启用容量

检索日期：2026-09-08。用户结论见 [M06-D01](decisions.md)，Q01 收束。原题比较新遗物到手后是否自然加入已有组合，还是需要腾出槽位／预算；不列遗物效果目录，不决定具体受益者，不重选初期自动战斗，不顺带批准出售、重复叠加或品质系统。以下比较材料完整保留。

### 现行基线

[玩法权威](../../../gameplay-design/tower-autobattler-core.md)已区分装备属于英雄、遗物属于本局。归属是本局不等于效果必定覆盖全队；[通用内容契约](../../../system-design/content-composition-foundation.md)允许遗物通过目标模型授予具体受益者状态。

只读 [RunRewardEconomyService](../../../src/Run/RunRewardEconomyService.cs)、[RunBattlePreparationService](../../../src/Run/RunBattlePreparationService.cs)、[RunRelicService](../../../src/Run/RunRelicService.cs)、[ActiveRunConfigurationValidator](../../../src/Run/ActiveRunConfigurationValidator.cs)、[RelicDefinition](../../../src/Relics/Authoring/RelicDefinition.cs)与 [RelicScopes](../../../src/Relics/RelicScopes.cs)：获得遗物追加到 run.Items，战前绑定全部 run.Items，服务逐一激活；实例校验与定义未见普通遗物槽位／容量／选择启用字段或总数上限。现行路径接近 L01，新增槽位不是补一个未定义空白。不据此声称无限数量可运行、所有效果永远生效或同名无限叠加；未运行验收。

### 已有语料检索

- 全部 780 深记录在 rule_support／mechanism／practical_support／engine 中交叉 relic／artifact／treasure／joker／accessory 与 slot／limit／hold／equip／swap／replace／remove／stack／capacity／acquire 等及中文，46 条初筛；扩展全部字段得到 196 条宽候选，其中大量是效果上限、装备所有权或 Agent 推论。
- 56 档案做全量同行交叉扫描，40 份／202 行命中，再收紧到槽位、替换、激活、库存与容量回读。重点核对 Storybook Brawl、Magicbook、Backpack Dungeon、Vivid Knight、Auto RiskRisk、Slotbound、Auto Brawl Chess、Slay the Spire、Monster Train 的对应段落；不重读全部全文或把命中数当覆盖质量。
- 106 发现记录同类检索命中 e017 The Last Flame 跨层构筑、e078 Storybook Brawl 宝物获取，共 2 条；它们不足以补精确槽位／替换制度。
- 关键深证据：`ev-storybook-brawl-016-treasure-slot-opportunity-cost`、`ev-mba-010-treasure-synergy-energy-budget`、`ev-bpd-009-relic-phase-ownership`、`ev-vivid-004-accessory-effect-symbol-bridge`、`ev-auto-brawl-chess-007-artifact-magic-item-layers`、`ev-monster-train-007-upgrade-artifact-economy`、`ev-gods-vs-horrors-013-relic-blessing-rule-rewrite`。Auto RiskRisk 的出售／免费槽说明在档案中，没有对应完整深记录，不虚构证据 id。
- 原库入口：[深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。本轮未联网补研；多款作品研究重点原为效果链而非遗物容量，因此无完整容量／移除规则时保留缺口。

### L01–L07：不同的持有与启用规则

**L01 获得后加入本局，普通遗物不另设启用槽位〔现行路径基线〕。**

新遗物与已有遗物同时进入规则集合，按各自触发条件发挥作用；获得新件不必先撤掉旧件。玩家主要在取得时比较它与其他奖励，以及获取所需的金币、路线风险或机会。

Slay the Spire 档案记录遗物通常不占手牌／每回合能量，但稀缺且难以定向获得；Monster Train 的 Artifact 属于本局规则层；Auto Brawl Chess Dungeon 有胜利后可叠加的 Artifact 奖励。它们说明获取机会本身可以构成代价，不过本轮已有摘要并非三款游戏所有遗物容量／移除规则的完整证明，尤其 ABC 的槽位与持久化关系仍有缺口。L01 的精确项目基线来自上面的只读路径。

优点是早期获得的能力不会因为后期腾槽被迫抛弃，可以逐步叠出组合；代价是后期规则数量和相互作用会增加，需通过具体内容、供给与可解释反馈处理。无总槽位不等于同名无限叠加，也不意味着每件遗物始终触发或可以无条件丢弃。

**L02 限制同时持有的总数，新件与旧组合争位置。**

Storybook Brawl 的历史规则最多持有三件 Treasure，升级角色获得同等级宝物选择，也可跳过换金币。Hat＋Ball 已占两个长期槽，其他核心宝物会与它们竞争，见 `ev-storybook-brawl-016-treasure-slot-opportunity-cost`、`src-sbb-wiki-treasure`。这让获得奖励不只是加法，也需要判断保留哪个组合；本轮不补完整替换界面、弃件回收或恢复旧宝物规则。

迁移后，玩家要在核心联动、即时战力及未来转型件之间取舍。好处是组合规模有限、每件选择鲜明；代价是早期经济／过渡遗物可能被后期淘汰，依赖多件配合的体系也被槽数限制。三槽是原作事实，不是项目推荐数值；不能从槽上限推定有备用库存。

**L03 全部保存在库存，只启用其中若干件〔项目对照〕。**

与 L02 的持有总量上限不同，未启用的遗物仍保留，准备阶段从库存中选择一套。例如把平时使用的经济遗物暂换成当前 Boss 的反制遗物，下场再换回来；这是说明，不是正式内容。

好处是可以准备不同对局，不因替换永久失去早期遗物；代价是新增一套全队配装管理，可能让每场换装成为例行操作。若购买时装备折扣、战斗前换伤害且保留全部收益，切换成本还可能被规避；具体经济阶段需要自洽。这是项目制度对照，本轮没有找到完整同构原作证据，不把 Slotbound 允许 discard Core 误写成有可恢复库存，也不从代码 Activate／Deactivate 推定玩家已获自由启停权限。

**L04 以总预算约束组合，而非每件占一个槽。**

Magicbook 的 Treasure Synergy 2.0 有可分配能量，起始 20 并随 Book 升级可增至 175，见 `ev-mba-010-treasure-synergy-energy-budget`、`src-mba-official-synergy-20-2025-05-18`。原作限制的是宝物联动配置，不直接等于每件遗物有不同费用，完整费用表也未公开。

项目迁移可以让不同遗物占用不同启用预算：一个较重的核心与多个较轻的辅助竞争总额度。好处是比固定件数更细地调整组合空间，也可允许容量成长；代价是玩家除看效果还要算预算，容易形成挑最优性价比组合的额外管理。具体预算、扩容方法和费用均未采用。

**L05 按类别分槽，各类分别选择。**

Vivid Knight 的 Ring、Necklace、Earrings 分属不同部位，饰品同时提供效果与 symbol，特定 Monster Spot 奖励可选择部位替换；攻略会为了补羁绊门槛保留单体效果较弱的饰品，见 `ev-vivid-004-accessory-effect-symbol-bridge`。它是饰品结构类比，不能把术语直接当成本项目遗物定义。

迁移方案可以按遗物类别分别限制数量，使不同用途各有空间；代价是人为分类会影响跨体系混搭，奖励扎堆同一类别时，即使另一个类别空着也未必能用。类型如何划分尚未定，不把攻击／防御／经济三类当成默认槽。

**L06 按战斗／非战斗阶段分别配置有效遗物。**

Backpack Dungeon 2.0.3 区分默认／非战斗页，2.0.5 加 Battle／Non-Combat 标签；Gold Dart 的玩家案例中，战中花金币触发必须放在战斗遗物栏，放非战斗栏则不响应。见 `ev-bpd-009-relic-phase-ownership`。不能把一个案例扩大成所有遗物的阶段或当前 UI 均会阻止误放。

它与 L05 的区别是限制按生效阶段划分，而非物品部位。好处是让经济与战斗配置分别管理；代价是合法持有也可能放错位置而失效，玩家必须理解“这个事件在哪个阶段发生”。若只是某遗物天然只响应战后奖励，可直接在内容中说明，不必为了这种时机差异引入所有遗物共有的两套栏位。

**L07 强制取得的特殊遗物附带槽位，但限制自由移除〔内容／获取特例〕。**

[Auto RiskRisk 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-riskrisk.md)记录 `Allow Us To Sell Artifacts?` 中开发者解释：强制取得的 artefact 附带免费槽，不挤占原本 regular item slots；普通 artefact 不允许随意出售，temporary artefact 另有规则。其意图是让玩家尝试所得遗物，同时控制槽数和组合复杂度。

这展示“赠送容量”与“接受规则约束”可以同时发生，不是获得新物必然挤掉旧物。但该作 artefact 与常规装备共享容量语境不同，完整构筑证据不足，不能移植其九／十二槽数值。

用于项目的取舍是保留接受特殊规则后的承诺，同时避免强制奖励挤掉原有配置；代价是可能被不适合当前体系的规则束缚。它是特殊获取／移除规则参考，不要求本轮采用强制遗物、负面遗物或禁止出售。

### 不混入本题的材料

- Siralim、Dwarves、Neon Auto Party 的具名 Relic／Artifact 有英雄持有层，术语相同不代表与本项目本局遗物同一归属；其装卸不直接证明 L03。
- 同名叠加、效果唯一、互斥、次数耗尽、一次性奖励与永久结果分别归具体规则／后续生命周期；不能将“有限次数”混成“有限遗物槽”。Gods vs Horrors 过滤不兼容 offer 也不等于所有遗物需要容量槽。
- 全队、本体、特定英雄、某类单位或某个事件的受益范围是内容问题，不重做全局单选。初期无战中介入继续有效。
- L01／L02／L03 的主要区别是累积、持有总量或启用子集；L04–L06 是容量组织方式，L07 是特殊获取承诺。可组合不等于全部需要，未提出遗物制作清单。

原建议及回应见 [Q01-P01](proposals.md)。用户已确认 [M06-D01](decisions.md)：无通用数量上限、不分栏、默认启用，特殊遗物可有自身启用机制；L02–L06 不作为普通管理制度，L07 未采用。研究证据及缺口不因决定改变；I22 保留特殊启用、移除权限与不同阶段收益的一致性检查。

## M06-Q02：已获得遗物的主动移除权限

检索日期：2026-09-08。状态：待讨论。本题讨论已经获得的遗物能否丢弃／出售／通过特定机会移除，以及这种权限如何影响转型和接受代价；不是为腾出容量，D01 无数量上限、不分栏、默认启用不变。重复获取另行核对，暂不设计具体负面遗物或移除服务。

### 现行核对

只读 [RunOperationDefinition](../../../src/Project/RunOperationDefinition.cs)、[RunDecisionService](../../../src/Run/RunDecisionService.cs)、[RunDecisionModels](../../../src/Run/RunDecisionModels.cs)和 [RunRewardEconomyService](../../../src/Run/RunRewardEconomyService.cs)：现行常规整局操作有 GrantItem，所读操作枚举没有遗物移除／出售，奖励路径追加遗物；没有发现已开放的玩家通用丢弃接口。此前 RelicScope 的 Deactivate 是运行生命周期，不等于玩家移除权；现行代码缺入口也不等于用户已决定永久禁止移除。

### 检索覆盖与证据缺口

- 780 深记录交叉 relic／artifact／treasure／core 与 sell／discard／remove／destroy／exchange／replace／sacrifice／ban 及中文，规则／机制／实践／engine 初筛 29 条，全字段扩查 149 条。多数涉及英雄替换、开发者删除目录内容、卡牌移除、效果结束或 Agent 推论，不是玩家移除遗物。
- 56 档案同行交叉扫描，30 份／77 行命中。定点回读 Guildrun、Slotbound、Auto RiskRisk 的真实出售／discard 文字，并以 Slay the Spire、TLF、Gods vs Horrors、Storybook Brawl、Backpack Dungeon 对应段落辨别卡牌移除、池过滤、版本删除、奖励跳过及 Workshop 权限。106 发现记录同类筛查无命中。
- 核心证据：`ev-guildrun-002-shard-shop-pity-economy`、`src-guildrun-wiki-economy-0-5-6`；`ev-slot-015-trait-core-overhaul` 的 discard 实践与 [Slotbound 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/slotbound.md)、`src-slot-review-core-economy-2026-07-26`；[Auto RiskRisk 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/auto-riskrisk.md)记录开发者对出售的解释，不虚构该段深证据 id。
- `ev-hdt-010-banner-wanderer-skip-economy`、`ev-storybook-brawl-016-treasure-slot-opportunity-cost` 的奖励选择／跳过不等于已取得后可卖；`ev-gods-vs-horrors-013-relic-blessing-rule-rewrite` 的不兼容 offer 过滤不等于移除当前遗物；`ev-tlf-012-content-dilution-soft-ban` 是供给控制；`ev-bpd-020-workshop-provenance-boundary` 属模组修改权限。它们不作为下面玩家移除制度的证明。
- 本轮没有联网新研究，也未重读所有档案全文。普通不出售有直接例证，discard 有跨系统例证；免费／返还、付费移除与交换的完整本局遗物规则不足，下面明确标注项目对照，不以常见游戏印象补事实。

### L08–L12：移除后失去什么、得到什么

**L08 常规没有主动移除入口，获得后保留〔出售限制有原作例证，完整禁移除是项目方向〕。**

Guildrun 允许英雄和普通物品按一定比例卖回，但明确 relic 不能出售；Auto RiskRisk 普通 artefact 也不能随意出售，开发者希望玩家尝试所获规则，临时 artefact 另有生命周期。不能把两者的不能出售扩大为任何事件、消耗或特殊效果都不可能移除。

项目若不提供常规丢弃／出售，玩家接受遗物时就需要考虑它与当前及未来构筑的关系。好处是保留取得时的承诺，不会每次转型都顺手清除旧约束；代价是旧遗物若与新体系冲突，可能压缩转型空间。它不要求所有遗物有负面效果，也不自动授权强制发放带代价遗物。特例能否移除仍可由明确内容规定。

**L09 战前主动丢弃，不返还资源〔discard 有类比，免费／无返还是项目设定〕。**

Slotbound 0.3.0 的玩家记录允许 discard Core，说明取得后仍有退出路径；Core 属该作独立规则系统，已有材料没有完整费用、返还及替换表，因此这里只证明可丢弃，不声称原作免费或零返还。

迁移方案是玩家放弃某遗物及其后续效果，换取摆脱旧规则的自由，不取得金币。好处是转型与修正选择容易；代价是若某遗物提供一次性收益并附持续代价，先拿收益再丢弃可能削弱其原定取舍。这个情形只是内容示意，具体哪些收益应保留或撤销需明示，不默认所有历史结果可回滚。丢弃不等于暂时停用后能再启用，不重开 D01 已排除的通用配装栏。

**L10 出售／回收遗物，取得资源〔项目对照〕。**

与 L09 仅解除规则不同，移除还会变成经济来源。过渡遗物先贡献一段收益，之后换钱补招募或其他资产；具体回收成金币还是材料尚未定。已有库没有足够直接的本局遗物出售规则，不把 Guildrun 英雄／普通物品的 66% 卖回比例移植到其明禁出售的 relic。

好处是降低转型沉没成本，让不再合适的遗物仍有价值；代价是可能让玩家把本不需要的遗物也当现金奖励，反复获取、出售的触发与价格需要一致。没有数量上限时，不需要为了清库存而强行增加出售系统。是否回收、按什么价值回收归本题方向及 R09，当前不定比例。

**L11 付出资源或消耗特定机会才能移除〔项目对照〕。**

没有随时可点的免费删除；在明确服务／事件出现时，付费或放弃另一项收益，解除指定遗物。它给转型留下出口，同时保留早期接受遗物的承诺。

代价是玩家可能已经想转型，却迟迟没有遇到移除机会；若所有带代价遗物都要靠稀有事件解锁退出，选择会变成等待和惩罚。资料里《杀戮尖塔》商店移除的是卡牌，不能用它证明普通遗物付费移除；这里只作项目制度对照，不预定新增商店／神庙或出现频率。

**L12 以旧遗物换取另一遗物或明确新效果〔项目对照〕。**

移除与获得新结果绑定。例如某次事件只允许用一件合格旧遗物换一件新遗物；玩家比较新旧组合，而非单纯付钱清除。不同于 L10：回收所得不是任意用途的通用货币；不同于 L11：移除后的奖励也是交易的核心。

好处是能制造明确的转型机会；代价是出口受交换资格及新结果影响，随机交换还可能从一个不适配换成另一个不适配。已有材料中的物品重铸／装备交换不等于本局遗物交换，没有精确同构例证，不假借原作事件名。确定交换还是随机交换、收益返还与资格均未定。

### 必须区分的相邻问题

拒绝尚未取得的奖励、移除当前遗物、停止持续效果与撤销已经发生的收益是不同动作；不从奖励可跳过推定已拥有可丢弃。次数耗尽、到期自毁、特殊启用／停用归具体遗物，不将它们强迫归入全局移除制度。

本题可以采用一个常规规则并保留少量内容特例，不要求五种制度齐备。获取是否可跳过及带代价规则如何预告与 R08／R12 衔接，不能靠未讨论的免费移除掩盖强制坏奖励。D01 和 G02 初期无介入不变，所有示意均不授权新遗物内容或服务实现。

原建议见 [Q02-P01](proposals.md)。用户已确认 [M06-D02](decisions.md)：L08 为常规保留规则，L11／L12 为特殊移除／交换方向，Q02 收束；L09／L10 不作为初期通用权限。原作证据及缺口保持，I22 继续记录特殊机会与当前效果／既有收益的生命周期。

## M06-Q03：同名遗物的重复获取与奖励结果

检索日期：2026-09-08。用户结论见 [M06-D03](decisions.md)，Q03 收束；以下保留原比较。本题只问普通奖励是否反复提供已持有的同名遗物，以及重复取得时带来什么。不同效果的叠加算法继续按内容定义，不再次提交统一加算／乘算选择；D01 无总数上限及 D02 常规保留、特殊移除／交换不变。

### 基线与证据范围

[系统契约](../../../system-design/tower-autobattler-architecture.md)明确遗物重复实例来源隔离，并有按层、跨实例汇总、每战同一绑定一次等效果政策。[RunRewardEconomyService](../../../src/Run/RunRewardEconomyService.cs)的 AddItem 以新实例 id 追加同一 ContentId、Stacks=1；[RunDecisionService](../../../src/Run/RunDecisionService.cs)的 CreateOffer 按稳定抽样取候选，未在该路径按已持有遗物统一去重。这是现行技术基线，不等于用户已确认所有遗物均可重复或未来供给政策已定；未运行验证。M05-D01 的装备重复穿戴不自动推广为遗物制度。

- 对全部 780 深记录，以 relic／artifact／artefact／treasure／core／遗物／神器／宝物，与 duplicate／stack／same／unique／copy／copies／upgrade／重复／叠加／同名／唯一／升级交叉检索；四个规则／机制／实践／引擎字段初筛 43 条，全字段 179 条。再扩展 joker／blessing／trinket／curio／祝福／小丑，四字段 44 条、全字段 184 条。大量候选只是英雄合成、跨效果联动或项目推论，不作同名遗物证据。
- 扫描 56 档案，首组同行交叉命中 25 份／81 行；进一步收紧叠加／重复关键词，回读 Auto Brawl Chess、Siralim Ultimate、Epic Auto Towers、Gods vs Horrors 等正文及来源索引。扫描 106 发现记录命中 e012／e053／e078／e085，分别为装备配方、装备合并、角色升级获宝物及背包空间类比，不证明遗物同名制度。输出编码修正后重读中文段落，未改原库。
- 直接相关：`ev-auto-brawl-chess-007-artifact-magic-item-layers`、`src-abc-help-dungeon-2022`：2022 起源、2026 迁移页面的历史 Dungeon 规则，胜后三选一、团队／阵营 Artifact 可叠加；2023 视频只证明候选展示，不证明实际选取或叠加。原库未完整列同名抽取资格、叠加公式及上限，不能把 stackable 扩为所有同名无限线性累积。
- 特殊复制：`ev-siralim-ultimate-021-warlord-antiquarian-relic-build`、`src-su-video-warlord-antiquarian-2-0`、`src-su-wiki-reliquary`：2.0 时期 Last of the Ancients 在只有一只生物装备遗物时，让其余五只各装备该遗物副本。原作是持有者绑定层，非本项目本局遗物；高 rank 的 Gate 实战不证明正常养成收益、RI5 通关或总体胜率。
- 相邻而不等同：`ev-gods-vs-horrors-013-relic-blessing-rule-rewrite`／`src-gvh-official-qol-2025-05-19`证明互不兼容候选过滤，不证明所有已持有同名遗物去重。`ev-eat-006-relic-rule-rewrite-ownership`只记录 duplication 缺陷修复且明确叠加上限缺口，不能用 bug 修复推定普通重复被允许或禁止。`ev-storybook-brawl-016-treasure-slot-opportunity-cost`是角色重复升级后获宝物，`ev-neon-auto-party-005-artifact-holder-bound-upgrade`是持有者绑定 Artifact 升级，均不证明本局遗物吃同名副本升级。
- 原库入口：[深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。本轮仅检索已有语料，未联网补研。库内多数研究重心为构筑效果链，缺少同名遗物供给的直接记录；以下项目对照据此明示，不用熟悉游戏的印象补证据。

### L13–L18：重复获取的不同处理

**L13 普通奖励排除当前已持有的同名遗物〔项目对照〕。**

拿到某遗物后，后续普通奖励主要提供尚未持有的遗物；玩家寻找新规则与已有组合连接，而不是反复追同一件。收益是奖励更有新鲜感、不会出现拿了没有新增作用的副本；代价是不能依靠继续拿同名遗物强化核心，随着可选池缩小也需处理候选不足。这里没有直接跨游戏同名去重证据，Gods vs Horrors 的互斥过滤仅说明资格过滤可作为供给环节，不能代证本条。特殊移除后能否再出、耗尽池如何兜底另定。

**L14 普通奖励允许重复，重复份数按遗物效果规则贡献〔现行模型方向＋累积类比〕。**

同一件遗物再次出现时，玩家比较“增强已有收益”和“拿一件新遗物”。例如项目示意中，每份增加一笔开场护盾可按份累积，而某个只开启一次的规则不因多一份就重复开启。Auto Brawl Chess 的历史 Dungeon 支持 Artifact 效果累积，现行项目模型支持重复来源与不同结算政策；不从两者推定同名抽取概率或统一倍增。

好处是已成型方向仍有可追求的重复收益，某次奖励无需一定扩展新机制；代价是奖励可能反复出现熟面孔，唯一效果的无新增收益副本尤其容易变成坏选项。通用允许重复与无效副本是否继续占候选位是两个层次，需与供给衔接；不在本题重选具体效果叠加算法。

**L15 区分普通唯一遗物与明确可重复遗物〔项目对照〕。**

某类遗物取得一次即退出普通候选，另一类明确标注可重复并显示下一份的收益。例如“改变招募规则”的遗物可以唯一，而按份提供资源／战斗收益的遗物可以允许再取；这只是示意，不硬性按数值效果／特殊效果分类。

好处是既保留新规则探索，也为部分体系提供持续投入对象；代价是玩家需要看懂可重复标识和实际增益，供给也要识别资格。可采用默认唯一、少数可重复，也可反过来默认可重复、少数唯一；这是默认值和内容权限的组合，不意味着已采纳任一具体遗物。现有库未提供完整同构混合政策，本条为项目方案。

**L16 同名再次获得时，提升已有遗物等级／品质〔项目对照〕。**

第二次取得用于升级原遗物，而不保留第二个独立副本；升级可以只是增强数值，也可以在某一档改变效果。好处是奖励既保持同一主题又产生清楚的成长节点，界面也可集中在一件遗物上；代价是会增加遗物养成制度和“差一份才成型”的追件压力。已有装备合并、持有者绑定 Artifact 升级不能作为本局遗物同名合并的直接证据。M05 装备品质可升级也不授权本条，当前不定份数、品质数或强化内容。

**L17 无法新增收益的重复奖励转换为其他回报〔项目对照〕。**

特殊固定奖励、无法避开的发放等渠道给到已持有的唯一遗物时，可转为资源或替代奖励，而非留下一份无效物品。区别于 L13：先允许发放结果撞重复，再处理替代收益；区别于 L16：不是强化该遗物。好处是避免重复奖励完全落空；代价是要平衡补偿价值，否则反而鼓励故意撞重复。它可只作为某些渠道的兜底，不必让普通可选奖励故意出现已知重复。补偿在取得时发生，与 D02 禁止普通出售不是同一种玩家权限，也不因此自动获批。

**L18 普通供给不重复，特定能力／事件才创造副本〔特殊复制类比〕。**

Siralim Ultimate 的 Last of the Ancients 让其他生物各装备一份指定遗物副本，说明“复制遗物”可以是专属规则，而非普通商店总会卖第二份。本项目可参考其获取权限区分：普通奖励保持唯一，只有明确机会才允许复制；不照搬每人装备遗物、六名额或原作养成。

好处是复制本身成为有价值的构筑机会，保留平常不重复的奖励体验；代价是必须说明可复制对象、是否复制计数／充能／既有成长、是否重发取得奖励以及副本后续如何移除。并非所有唯一规则都适合复制；可与 L13／L15 共存，是特殊内容方向，不是默认必须建设的系统。

### 本题边界与恢复

唯一持有、唯一效果、候选去重是不同约束；不同遗物碰巧产生相同效果也不等于同名。是否线性、乘算、递减、每战一次等结算继续归具体效果，关联 I18／I20。移除原件是否解锁普通候选、副本是否独立退出，与 D02／I22 衔接；不先用隐藏技术默认值决定。

原建议见 [Q03-P01](proposals.md)。用户已确认 [M06-D03](decisions.md)：L13 默认唯一＋L15 明确可重复内容，L18 保留特殊方向；L16 初期不增加通用制度，L17 按渠道需要再设计。证据及缺口不因决定改变；I23 继续记录供给、奖励结果及具体效果的衔接。
