# R01 英雄分级：已有证据

## R01-Q01：英雄是否有固定档级，档级代表什么

检索日期：2026-09-08。状态：待讨论。这里的档级指英雄内容本身在招募体系中的固定层级，不是同一英雄的培养等级／升星，也不是攻略作者的强度榜。本轮先讨论采用与语义，暂不定档数、颜色、价格、概率或解锁节点。

### 已定边界与现行基线

[玩法框架](../../../../gameplay-design/combat-build-framework.md)明确英雄是否分级仍未定，已有内容标签不能代替用户决定；[核心规则](../../../../gameplay-design/tower-autobattler-core.md)规定每名永久英雄恰好占一人口，不因档级或稀有度改变。[M01-D01](../../../02-foundation-models/unit-model/decisions.md)固定主动＋被动、本体不配置、不随机个体属性；[G03-D02](../../../01-global-boundaries/run-rhythm/decisions.md)允许早期核心持续培养、过渡后换核、随关键收获成型，前期给探索容错。

因此不重新选择固定技能数量、每英雄一人口或前期必须追战力；高档可有更强／复杂的既定效果，但不能借本题增加本体天赋槽。档级、培养与后期替换的具体成本分别与 R02／R03／R07／R08 衔接。

### 全库检索与证据边界

- 全部 780 深记录在 rule_support／mechanism／practical_support／engine 交叉 hero／unit／character／creature／god／recruit／英雄／单位／角色／招募，与 tier／rarity／rare／legendary／cost／unlock／pool／分级／稀有／高费／低费／品质／解锁，初筛 139 条；全字段扩查 422 条。收窄到 tier／rarity／legendary／low-cost／high-cost／分级／稀有／高费／低费／品质后，规则四字段 58 条。大量宽命中是装备品质、种族池、商店经济或项目推论，不作为固定英雄档级证据。
- 56 档案同行交叉扫描，30 份／96 行命中。定点回读 Astronarch、Tales & Tactics、Slotbound、Mechabellum，以及下列深记录与来源索引。106 发现记录命中 e021 Tales & Tactics 层级投入、e036 Kādomon 满进化后的额外形态、e074 Gods vs Horrors 招募／升阶投入；e036 属培养及后续形态，不作本题初期方案。
- 核心材料：`ev-tft-005-reroll-archetype`、`ev-tft-006-fast-eight-archetype`、`ev-sap-005-fish-bison-build`、`ev-tnt-002-investment-over-rarity`、`ev-tnt-013-rare-rush-meta-response`、`ev-tak-009-legendary-core-needs-playable-bridge`、`ev-slot-007-mono-mage-build`、`ev-slot-010-role-imprint-single-owner`、`ev-pmm-009-tier-replacement-and-no-promotion`、`ev-mecha-014-tier-means-flexibility`、`ev-gods-vs-horrors-002-draft-economy-tempo`。Astronarch 职责招募参考档案及 `src-astro-guide-beginners`／`src-astro-guide-elements`，不虚构不存在的招募深记录。
- 反例及排除：`ev-shf-014-hero-family-rebalance-history`包含高阶过强和投入回报不足的历史反馈，但其工厂配方／合成不同于本项目，不能推出精确强度系数；`ev-guildrun-003-rank-class-bending`的 C→B→A→S 是同一英雄局内晋阶与特化选择，不作为四档先天英雄范本。Skull Horde 三份升级、Kādomon 进化、装备品质和单位 tier list 均与固定招募档级区分。
- 来源入口：[深记录](../../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[来源索引](../../../../web/game-mechanics-atlas/research/deep/source-index.md)、[档案](../../../../web/game-mechanics-atlas/research/deep/game-dossiers)。本轮未联网补研；历史版本与玩家实践不升级为当前全游戏规则，检索命中数不代表机制已穷尽。

### HT01–HT09：档级可能承担的不同作用

以下既包括分级制度，也包括采用分级后可共存的强度／功能安排；不是九选一。HT04、HT05 等不能被误写为新的招募货币或独立养成系统。

**HT01 不给英雄设固定档级，以技能职责与组合价值区分〔项目方案／职责招募类比〕。**

玩家拿英雄时主要看缺什么功能、谁能接上现有装备／遗物；进程可以仍然逐步开放候选，不必用白蓝紫或一至五费标明高低。Astronarch 的历史指南按 tank、healer、damage、control 等职责补有限名额，支持这种选择重心；它不构成“原作所有版本完全没有任何层级”的否定证明。来源为 `src-astro-guide-beginners`（早期 1.2.x）与 `src-astro-guide-elements`（1.2.3–1.3.5）。

好处是玩家不易把晚来者误认成自动上位替代，早期英雄的身份稳定；代价是缺少一个直观的奖励档次信号，后期收获感更依赖实际能力与内容变化。不分级不等于所有英雄一样强、开局全开放或等概率。

**HT02 档级首先管理获取阶段与机会成本〔多作规则／攻略参考〕。**

云顶通过等级改变各费用单位的商店概率；Gods vs Horrors 把当前购买、Pantheon 升阶与刷新放在同一资源取舍中；Super Auto Pets 按回合开放更高宠物 tier，升级宠物还会给高一阶商店候选。分别见 `ev-tft-006-fast-eight-archetype`、`ev-gods-vs-horrors-002-draft-economy-tempo`、`ev-sap-005-fish-bison-build`。一个靠主动投入改变供给，一个按进程自然开放，不在此提前二选一。

迁移后，档级让玩家理解哪些英雄容易在前期获得、哪些属于后续或较珍贵机会；不单凭档级保证替换必赚。好处是构筑供给有层次；代价是把关键能力放得太晚会让前期只有等待。具体采用进程、招募成长、概率或特殊渠道由 R07／R08 再定，不自动绑定人口升级。

**HT03 高档提供更高的即时战力，支持过渡后换核〔换核路线参考〕。**

云顶 Fast 8 路线用过渡阵容保血、保存经济，再寻找高费核心完成替换；见 `ev-tft-006-fast-eight-archetype` 与 `src-tft-bunnymuffins-leveling`（库内 Set 17 指南快照，攻略非官方定律）。项目可以让某些高档英雄在较少培养时就具备较完整的输出或生存能力，让晚来者值得转型。

收益是后期奖励容易产生可感知提升，符合已允许的打工卡换核；代价是若每次高档都无条件压倒旧人，探索与早期培养会被固定淘汰流程取代。`ev-tnt-013-rare-rush-meta-response`记录开发者承认 PvP 高段 rare rush 太稳定，随后提高 Rarity 费用，见 `src-tnt-official-1-0-40`；这是 1.0.33–1.0.40P PvP 反例，不外推其 PvE 价格。不是提出全英雄必须换高档的新规则。

**HT04 低档核心通过持续投入，可与高档换核路线并存〔构筑实践〕。**

云顶重抽路线停在适合低费单位的等级追副本，Fast 8 则把资源投入更高等级；低费更容易取得不等于成型总投入低。Tales & Tactics 的 2025 讨论建议：已有高培养 common、合适装备或羁绊组合，不必只因刷出 rare 就换掉。见 `ev-tft-005-reroll-archetype`、`ev-tnt-002-investment-over-rarity`，后者是有分歧的社区经验，没有统一价值公式。

迁移后，同一体系既可从早期核心持续培养，也可换后期核心。收益是保留玩家已有投入；代价是要让两条路线在所需投入、成型时间与后续回报上都成立，而非承诺任何低档满养都必胜最高档。不照搬追三、四星或原作装备转移制度，培养手段属于 R02。

**HT05 低档保留独有功能，高档不能自动替代〔历史功能互补实例〕。**

Slotbound 0.2.7 时期的两份实践描述：common Mage 可形成 healer，uncommon Mage 提供范围输出或坦克召唤，法师路线需要这些功能配合，而不是把所有 common 全换掉。见 `ev-slot-007-mono-mage-build`；属于 Core 重做前非统计实践，不证明当前概率或强度。

区别于 HT04：低档留下来可以因为其技能功能正合适，不必先付出巨额培养。收益是高档仍需围绕体系选人，功能位有长久价值；代价是关键功能若只有一个低档来源，后期供给又不再提供它，会出现想补却补不到的断点。只借鉴不同英雄功能互补，不采用原作局内职业分支。

**HT06 高档提供体系关键环节，取得后让已有组件形成更强联动〔后期发动机与反例〕。**

Tiny Auto Knights 的 Legendary 延后出现，Jester 在 1.1.0 被移入 Legendary；玩家反馈围绕六个 Legendary 规划的卡组很难在它们出现前安排，开发者建议适应实际候选，见 `ev-tak-009-legendary-core-needs-playable-bridge`／`src-tak-thread-legendary-availability`。Super Auto Pets 的 Fish→Tier-4 Bison 路线，则让早期 Fish 的等级投入成为后期 Bison 自成长的条件，见 `ev-sap-005-fish-bison-build`（2026-07-28 Turtle Pack 指南，无胜率样本）。

这与 HT03 纯粹换更强输出不同：新核心能连接已有投入。收益是获得关键英雄时有明显成型感；代价是若基础玩法完全依赖一张晚出英雄，前期只能空等，与 G03 宽容探索相冲突。建议区分“已有玩法被放大”和“此前根本不能玩”，不能用未来稀有奖励承诺当前阵容必成。

**HT07 高档更泛用，或在同一英雄内兼顾更多职责〔项目对照／评价方法类比〕。**

MechaMonarch 作者把其主观高 tier 解释为适用场景更多，而不是每次都该购买；低评级 Hacker 仍可在合适对局发挥核心作用，见 `ev-mecha-014-tier-means-flexibility`。必须强调这是攻略评级，不是原作内置稀有度。本条将“泛用度”作为项目可用的档级含义，而非声称 Mechabellum 用该方式设计固定档级。

一个高档英雄可以对更多场景有效，或在固定主动＋被动内同时覆盖两项职责；一个低档英雄仍可在擅长场景更合适。好处是晚期奖励有价值又不必全靠基础数值；代价是在每人都占一人口的前提下，职责压缩本身就很强，过多全能英雄会挤掉专职角色。现行人口框架已允许稀有终端以真实机会成本压缩人口体系职责，这是已有内容边界，不重新表决或推广成所有高档全能。

**HT08 档级决定成长上限，低档满养后需换高档才能继续突破〔跨局管理类比〕。**

Private Military Manager 的材料描述 operator tier 与成长上限；玩家报告低阶人员练满后需要招募高阶、重新训练和配置，未找到可靠的原员升档路径。见 `ev-pmm-009-tier-replacement-and-no-promotion`，2025 EA 至停止更新版语境；缺少完整上限／价格表，不能用未找到晋升说明证明绝对不存在。

这是档级规定长期潜力，不只是当前战力。好处是梯队更替与后期招募目标明确；代价是容易强制淘汰早期核心，而且原作是持续公司经营，与本项目单局塔爬不同。初期不推荐作为通用硬门槛；不等于所有英雄都要采用相同培养曲线或无限成长。

**HT09 高档解锁更多本体技能／被动容量〔与初期边界不兼容，保留参考〕。**

Slotbound 高 rarity 可容纳更多 passive，结合吸收继承形成更复杂的单体；见 `ev-slot-010-role-imprint-single-owner`，Demo 0.3.2–0.3.4 实践。收益是高档能承载更多内部联动；代价是成长同时增加配置复杂度，并强化追高档的压力。

这与 M01-D01 初期固定主动＋被动、无本体配置冲突，因此当前不作为待选初期方案。Guildrun C→B→A→S 的特化选择同样保留在培养／后续能力材料中，不借 R01 重开已定边界。

### 组合关系与待决范围

HT01 是不采用固定档级的整体对照；HT02 是采用后的供给作用。HT03–HT07 可共存，描述不同英雄在分级体系内的价值，不要求每档所有英雄共享一种角色。HT08 会改变培养与替换承诺；HT09 已受 M01 初期范围限制。确切档数、名称、开放方式和概率，以及同一英雄是否能升星，都不在本题顺带决定。

建议见 [Q01-P01](proposals.md)，尚无 R01 用户决定。I24 跟踪固定档级、早期投入、后期奖励与供给的衔接。
