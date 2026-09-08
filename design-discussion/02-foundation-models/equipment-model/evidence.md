# M05 装备模型：已有证据

## M05-Q01：同名装备的重复使用与限制作用范围

检索日期：2026-09-07。2026-09-08 用户结论见 [M05-D01](decisions.md)，原研究材料保留。现行三槽、英雄归属、战前免费转移作为基线，不重新选择装备槽数量，也不列装备技能目录。

### 本题边界与现行核对

本题关注第二件同名装备能否继续分配给同一英雄，以及限制应落在穿戴、某项效果还是队伍范围。拿到两件与穿戴两件、两件参与结算与所有收益翻倍不是同一个事实。M04-D01 已明确具体状态结算方式归内容，本题不重新选状态叠加公式。

[玩法权威](../../../gameplay-design/tower-autobattler-core.md)已明确普通英雄三槽、装备属于具体英雄、战前免费装卸转移、替换旧件回库存，战中锁定编辑；[构筑权威](../../../gameplay-design/combat-build-framework.md)已允许装备通过状态与事件联动。没有发现需要重选这些基线的冲突。

只读 [EquipmentDefinition](../../../src/Equipment/Authoring/EquipmentDefinition.cs)、[RunEquipmentService](../../../src/Equipment/RunEquipmentService.cs)、[ActiveRunConfigurationValidator](../../../src/Run/ActiveRunConfigurationValidator.cs)、[EquipmentScopes](../../../src/Equipment/EquipmentScopes.cs)：现行普通持有实例转移路径与校验区分 InstanceId、槽和拥有者，不按相同 ContentId 禁止穿戴；战斗按每个实例建立来源并投影属性。代码支持普通同名实例分别存在，是当前基线，不等于每个状态／被动均独立叠加或所有效果都会翻倍。特殊“英雄唯一／队伍唯一／同组互斥”的产品字段不见于所读装备定义，不声称其他限制已经实现或绝无其他路径。未运行游戏。

因此本题是核对是否需要在基线上新增同名限制，以及特例的作用范围；不是假装现行完全没有重复穿戴行为，再要求用户从零批准。装备成长、合成、随机词条、重复掉落权重、出售／分解归后续所属问题，本题不增加这些系统。

### 已有语料检索

- 全部 780 深记录：在规则／机制／实践中搜索 duplicate、multiple copies、same item/equipment、unique、exclusive、stack、重复、同名、唯一、互斥，并与装备／item／artifact／accessory 交叉，20 条初筛候选。
- 扩展全记录字段及 engine，加入 copies／叠加等得到 56 条宽候选，其中有重复英雄、重复奖励、合成、装备供给、法术宝石和属性循环等噪声，不能等同 56 条同名装备规则。
- 全部 56 档案做同段落行交叉扫描，25 份／65 行命中；对 Astronarch、TFT、Auto Chess、Underlords、Backpack Dungeon 定点补搜并回读相关段落。初筛外围材料也查看了 My Party Is Grinding、Survivor Mercs、Siralim、Auto RiskRisk、Combat Alchemy 的装备边界，不把插槽、随机供给或培养层误当重复穿戴规则。
- 发现层全部 106 条参与同类检索，命中 e053、e064、e077、e078、e084、e085、e103，共 7 条，主要属于升级、供给、元素或非递归转换，未用于证明精确同名互斥。
- 直接相关原作证据集中在 Astronarch 多 copy 结算修复、Backpack Dungeon 挑战禁重，以及 Astronarch 供给端去重。其余游戏没有足够精确的同名穿戴／被动唯一范围说明；不以熟悉游戏印象补规则。
- 以下 E02–E05 是项目侧规则对照，没有在本轮已有库中找到足够直接的原作完整例证；E06 的全队迁移同样是项目推论。它们用于说明不同限制的真实后果，不当作已经调研证实的行业惯例。已有 66 个候选／56 份档案深度不同，本轮无联网新研究，未重读所有档案全文。

### E01–E06：重复装备的不同处理层

**E01 同一英雄可以穿多件同名装备，各自占槽、按具体效果结算。**
这是现行实例／槽校验容许的基线。玩家可以把第二件交给同一核心，强化相同能力，也可以分给另一个英雄。代价是占用另一个槽，少带一件生存、功能或其他联动装备；它不保证所有输出恰好翻倍。
Astronarch 1.4 修复过多个 Tower Shield 装备时效果被过度削减的问题，攻略也会比较多 copy、升级和不同持有者。证据确实支持“多 copy 的结算需要有明确规则”，但公告未穷尽每一 copy 的持有者、完整公式和所有物品规则，不能据此宣称该游戏全部装备都可在同英雄上无限同效叠加。证据：`ev-astro-016-multi-owner-start-order-and-copy-fix`、`src-astro-official-1-4`。

**E02 同一英雄同名装备最多一件，第二件可以给别人〔项目对照〕。**
限制发生在穿戴时，避免玩家在该英雄身上花槽却拿不到预期第二份效果。第二件仍有分给副核心、坦克或替补的价值。
好处是鼓励单英雄组合不同装备，限制同一强件堆满；代价是可能失去明确可理解的重复构筑，且当只有一个合适持有者时，第二件变成闲置。若同名不同品质是否也互斥，必须随内容身份一起说明。本轮没有精确原作规则证明，不从 Underlords 每人一槽推导出额外的同名唯一政策。

**E03 允许穿戴重复件，普通属性分别提供，但指定特殊效果只生效一份〔项目对照〕。**
示意：两件都提供各自生命属性，但“获得某种特殊保护”的具名部分只保留一份。第二件并非全无价值，只是主要机制不能复制。
好处是可以限制过强的规则效果而不删除普通属性投入；代价是两件外观相同却不完全叠加，若说明不清很容易形成装备陷阱。应明确究竟哪个部分唯一、作用于哪个英雄／受益者、不同强度怎样选择。这里只比较装备限制层，具体状态去重或保留较强者仍由 M04 内容规则决定，不设所有装备共有的“属性叠加、被动唯一”。

**E04 多件都能参与，但某个触发共享冷却／次数上限〔项目对照〕。**
与 E03 永远只保留一份效果不同，每件可以产生合格触发，但某一时间窗口或次数额度共享；第二件可能提高触发可靠性，却未必提高最高频率。
好处是能限制高频触发链而保留多个来源；代价是不同件可能互相抢机会，提升攻速／加第二件的收益不直观。必须明确额度归同一英雄、目标还是整个队伍，不能把一件装备原本的独立冷却误写成全队共享。现行共同效果次数隔离与研究中羁绊共享触发并不直接证明已有装备共享冷却；本轮不声称该形式已实现。

**E05 不同名字但属于同一互斥组，同一英雄只能选其中一种〔项目对照〕。**
示意：两个不同装备都用于改写同一种攻击模式，设计者可要求该英雄只能装备其中之一。它防止玩家通过换物品名绕过原本的组合约束，不需要禁止所有重复装备。
好处是解决真正相冲突的规则组合；代价是装备限制从看名字扩大到看组别，限制过多会削弱跨体系连接。互斥组应来自明确的内容冲突，不因为两件都提供护盾、伤害或同类属性就自动判互斥。不新增武器／防具／饰品类型槽，也不默认每个英雄只允许一种输出路线。

**E06 把禁重范围扩到整套配置／队伍〔挑战有原作例证，全队迁移为对照〕。**
Backpack Dungeon 有禁止背包内重复物品的 Challenge，迫使玩家使用不同组件，而非反复堆同一件弓。这是特殊挑战规则，不是原作所有普通局都禁重；游戏的单背包也不能直接等同本项目多英雄队伍。
若迁移为“全队只能穿一件某装备”，第二件即使分给别人也不能上场，约束显著强于 E02；若仅某个光环全队只生效一次，则应归 E03 的效果范围，不能混成穿戴禁令。好处是可作为特殊限制或稀有物的明确定位，代价是更容易产生多余奖励、强制配置变化，需要与供给和预告衔接。证据：`ev-bpd-007-ranger-craft-status-module`、backpack-dungeon 档案和 `src-bpd-guide-achievements-2-2-5`；不采用该 Challenge 或默认队伍唯一。

### 相关但不作为本题选项

- Astronarch Ancient 不再重复提供已拥有件，common／rare 越见过再出现权重越低，见 `ev-astro-011-item-pool-ownership-weak-duplicate-protection`。这是供给端去重，不证明库存／穿戴必须唯一；归 R06／R08。没有重复掉落也不等于永远没有复制或其他来源，不能反向推全局限制。
- 同名装备合成升级是消耗／成长规则；Magicbook 同名同品质三合一、不同件重铸，以及 Auto RiskRisk 的装备合成，归装备成长／奖励经济，不用它们回答“当前两件能否同时穿”。
- TLF 重复英雄被动、Siralim 组件继承、LoL 双向属性不自我递归，各自不是同名装备限制，不能拿来充当行业例证。
- “装备实例唯一”防同一件物品被同时放两处，与“同名只准一件”不同。换装免费不等于免费复制，复制一个状态也不等于生成第二件永久装备。

### 提案与剩余证据

见 [Q01 建议及回应](proposals.md)。2026-09-08 用户确认 [M05-D01](decisions.md)：装备可重复穿戴，收益依靠词条控制，例中爆伤叠加而暴击额外攻击词条唯一。该结论不改变原作证据的精度和缺口；其他限制仍为参考。后续按具体词条核对身份、唯一范围及技术表达，不从代码能力倒推玩法。当前未作任何实现或运行验收。

## M05-Q02：同一种装备获取时的固定定义与随机个体差异

检索日期：2026-09-08。用户结论见 [M05-D02](decisions.md)：词条／数值固定，可以升级品质，Q02 收束。以下保留原检索边界：本题核对装备拿到手时是否另行随机生成词条／数值；强化、合成、镶嵌、洗练的操作制度和费用不在本题确定。

### 基线与本题必要性

M05-D01 已确定重复穿戴与词条级唯一，但没有确定词条如何产生。M01-D01 的英雄个体固定也不自动禁止装备随机。G03-D02 希望通过关键装备／遗物逐渐定向，因此“找到装备就能确认它的作用”与“还要看这件装备随机到什么”会影响构筑可达性。

只读现行 [装备定义](../../../src/Equipment/Authoring/EquipmentDefinition.cs)、[装备实例](../../../src/Equipment/CompiledEquipmentModels.cs)与 [战前装配](../../../src/Equipment/EquipmentScopes.cs)：实例记录身份、内容 id、拥有者与槽位，装配按内容 id 读取同一固定定义，未见实例级随机词条／数值字段。固定成品是所读路径的现行基线，不代表用户已否定未来随机化或装备成长；未运行验收。

### 检索覆盖与证据限制

- 全部 780 深记录用装备／物品与 affix、enchant、procedural、random、roll、socket、rarity、quality、词缀、随机属性等交叉筛查，140 条宽候选。宽检索包含大量招募、供给、品质与回滚噪声，不等同 140 条装备随机规则。
- 在 rule_support／mechanism／practical_support 中收紧到词条、属性生成、附魔／插孔，得到 10 条候选：`ev-tnt-007-trait-width-tier-and-bridges`、`ev-astro-005-old-c20-ramp-party-boss-kit`、`ev-mba-003-equipment-merge-reforge-transaction`、`ev-mpig-004-cold-ranged-dual-core`、`ev-mpig-008-gear-grade-socket-context`、`ev-mpig-014-enchant-synthesis-commitment`、`ev-mpig-018-dead-reward-owner-facility-gap`、`ev-survivor-mercs-015-gear-pool-tradeoff-lifecycle`、`ev-girls-of-the-tower-016-item-relic-card-ownership`、`ev-auto-brawl-chess-008-account-power-match-confound`。其中人名／物品名 Enchanter、宝石、重铸及卡牌插孔不作为装备随机词条证据。
- 56 份档案全文行扫描，13 份／46 行命中。另定点回读 Survivor Mercs 的 Gear 历史、MPIG 的 random options／grade、Astronarch 的具名装备／升级，以及 TLF、Tales & Tactics、Gladiator Guild Manager 的对应装备段落，避免仅依赖英文关键词。
- 106 发现记录同类宽检索命中 e036、e053、e064、e068、e084、e088，共 6 条，属于进化、合成、供给、槽机或持续替换，不证明精确个体词条生成。
- 进一步搜索 random options／random stats／random attributes／random properties／affixes／rarity-based 等，深记录精确词组候选仍混入随机英雄属性、rolled back 等噪声。直接“random options”信息来自 MPIG 档案，不能因为结构化记录没有写全就遗漏。
- 关键原库入口：[MPIG 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/my-party-is-grinding.md)、[Survivor Mercs 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/survivor-mercs.md)、[GGManager 档案](../../../web/game-mechanics-atlas/research/deep/game-dossiers/gladiator-guild-manager.md)、[深记录](../../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)。本轮未联网刷新，未重读所有档案全文，也未取得完整逐件生成概率表。

### E07–E11：随机发生在哪一层

以下沿用本议题连续编号。E07–E10 比较固定与随机层；E11 是可以与前述组合的品质维度，不是第五种互斥套餐。示例装备与数值只用于解释，不是新增正式内容。

**E07 固定成品：同一明确版本的装备，词条和数值固定〔现行基线〕。**

随机的是奖励里出现哪件装备。一旦拿到指定装备，就能知道它增加哪些属性、具有什么特效；同定义的第二件无需重新鉴别好坏。若未来有品质／升级档位，比较时应控制在同档位，不将固定定义解释成永远不能成长。

优点是关键件能成为稳定的构筑信号，注意力集中在持有者、搭配、槽位与取舍；缺点是重复拾取的个体惊喜较少。同名重复仍可按 D01 分配或叠加，固定不等于没有价值。

Astronarch 攻略会围绕具名装备组合、升级优先级及备用套装规划，见 `ev-astro-011-item-pool-ownership-weak-duplicate-protection`、`src-astro-guide-complete`。它支持“可识别的装备作用能支撑规划”，但不是证明全游戏所有物品无任何随机数值的完备规则表；E07 的精确固定定义基线依据本项目所读路径。

**E08 词条种类固定，仅数值在范围内随机〔项目对照〕。**

示意：两件都固定增加爆伤并带同一种唯一特效，但一件增加 20%，另一件增加 30%；数值仅为示意。玩家仍知道这件装备属于暴击方向，重复掉落却可能出现更高数值版本。

优点是保留装备身份，同时让重复件有追求上限的空间；代价是增加比较、替换和等待高数值的负担。若随机幅度跨越关键触发或生存阈值，“只是数值差异”也会改变构筑是否成立。完整语料没有给出可直接引用的同名装备纯数值区间算法；不能拿 Survivor Mercs 的 rarity-based rolls 硬补这一事实。

**E09 随机词条种类：同种底材可以生成不同方向〔有相关原作结构，精确生成表缺失〕。**

MPIG 档案明确装备具有 random options，grade 决定相应容量，并存在刻印、附魔与宝石等其他层；来源 `src-mpigdb-equipment`，对应 `ev-mpig-008-gear-grade-socket-context`。因此不能只看装备部位／品质就假定得到同一收益；但原库不提供完整 server acquisition roll，也未给出本题所需的全部词条池／互斥概率。

项目示意：同类护符一件获得爆伤和攻速，另一件获得生命和治疗相关增益；这组具体搭配不是 MPIG 原物品。它让玩家根据实际掉落发现组合，也让同种底材有不同持有者。但玩家可能已经拿到正确装备类型，却仍没有得到需要的词条；如果关键机制也随机，体系成型会多一道门槛。MPIG 是长期重复刷取与多层成长，不能直接把其刷取耐受度搬到本项目有限流程。

**E10 固定核心机制，附加词条随机〔项目组合对照〕。**

示意：某件装备必定具有“暴击额外攻击一次〔唯一〕”，附加属性再随机生成爆伤、攻速或生存属性。玩家拿到它时已获得构筑关键机制，随机部分决定更偏输出还是更偏生存。

这保留发现好版本的惊喜，又减少关键机制完全不可达的问题；代价是仍需比较个体，而且副词条若占据过大强度预算，所谓固定核心仍可能只有少数随机组合真正好用。唯一特效和随机属性可以在同一装备共存，不改变 D01。

这是由固定机制与随机附加层组合出的项目方案，本轮已有材料没有足够精确的同构原作例证，不冠以某款游戏的通用掉落规则。

**E11 品质决定固定强度或词条容量〔独立维度，可与上述组合〕。**

品质至少有两种不同后果：其一是同一作用的明确增强版本，玩家主要比较投入与收益；其二是增加可生成的词条／配置容量，高品质因此获得更多组合机会。Tales & Tactics 2.0 的 Uber Items 会加倍核心属性并增强效果，见 `ev-tnt-006-high-star-population-compression`、`src-tnt-official-2-0`；MPIG 的 grade 容量见 E09。两者不能合并成“品质高就只加面板”。

Survivor Mercs 的 0.9.8 升级曾同时扩大 bonus／tradeoff，0.10 改为稀有度规则，0.11 又让 common 可以触发 proc，0.15 增加四槽与升级；见 `ev-survivor-mercs-015-gear-pool-tradeoff-lifecycle`。这些是历史改动，不能拼成同一版本，也不能据此断言目前低品质必定没有特效。

好处是给重复获得和阶段奖励清楚的层级；代价是低档装备可能变成纯过渡，或关键机制被高品质门槛锁住。GGManager 开发者曾指出玩家忽略低稀有度 Universal Items、等待蓝／紫版，1.0 改为基础装备加 Workshop 升级，见 `ev-ggm-012-trait-and-item-choice-reworks`、`src-ggm-official-1-0`。这是装备品质的实际失败／重做案例，不把同记录中的随机英雄 Traits 误写成装备词条。

本题只区分品质与随机的关系，不在此决定是否设品质、品质数量、掉率或升级办法。

### 相邻机制的去向

- 镶嵌、刻印、附魔、Siralim 的 stat／property／trait／spell 插槽，是玩家取得装备后再配置的层；可支持成长问题，不等于掉落时随机生成。Auto RiskRisk 商店页列 enchantments，但没有完整规则，不能补成洗练系统。
- Magicbook 三件不同同品质物品随机重铸，随机的是产出哪件；同名三合一的确定升级、TLF 锻造、GGManager Workshop 也不直接证明随机词条。保留至装备成长／供给经济。
- MPIG 的绑定、毁装风险、长期刷取与设施门槛不随随机词条自动采用。不同随机层均可配合后续供给设计，但不以尚未讨论的保底／洗练承诺掩盖可达性代价。

原建议及回应见 [Q02-P01](proposals.md)。用户随后确认 [M05-D02](decisions.md)：装备固定且可升级品质；E08–E10 的个体随机不采用，E11 具体增强及升级制度未定。D01 不变，研究证据边界不因决定改变；I21 继续记录固定品质配置与供给／成长的衔接。

## M05-Q03：品质升级的投入来源与机会成本

检索日期：2026-09-08。用户结论及候选见 [M05-D03](decisions.md)，材料与风险方向阶段收束。原题聚焦升级付出什么、是否必须再拿同名装备；不重选能否升级，不同时确定每档数值、品质名称／数量、价格或升级时间表。以下材料、机会与失败风险保留比较，不包装成互斥套餐或逐项采用。

### 基线及检索

D01 允许重复穿戴，D02 固定配置并可升级品质。现行 [玩法基线](../../../gameplay-design/tower-autobattler-core.md)明确三槽、免费战前装卸与库存归属，不隐含出售／制作经济；本轮搜索 src/Equipment 的升级、品质、合成／锻造字段，没有发现已建立的升级服务。不能把免费装卸解释成免费升级，也不把现行未实现当成拒绝品质成长。

- 780 深记录，在 rule_support／mechanism／practical_support／engine 中交叉搜索 equip／gear／item／artifact 与 upgrade／craft／merge／synthesis／forge／refine／quality 及中文近义词，58 条候选；扩展到全部字段得到 152 条宽候选，包含大量英雄升级、供给及研究推论噪声。
- 56 档案全文行扫描，28 份／106 行命中。定点回读 Astronarch、Magicbook、GGManager、MPIG、Dwarves、Girls of The Tower、Just King、Survivor Mercs、Auto RiskRisk 的对应段落；其他命中按对象归属排除，不把 Epic Auto Towers 强化塔、Super Auto Pets 合宠或 Monster Train 卡牌升级当装备升品质。
- 106 发现记录同类检索命中 e012 Backpack Battles 配方合成、e031 Dungeon 100 卡牌组合、e053 Auto RiskRisk 重复装备合成。Auto RiskRisk 当前 Demo 支持同名装备合成，但旧 Concept 的三同名角色规则不能用来补当前装备合成数量；只作受限例证。
- 主要采用深记录：`ev-mba-003-equipment-merge-reforge-transaction`、`ev-mba-011-endless-growth-cap-rework`、`ev-mba-012-event-special-set-acquisition`、`ev-astro-010-route-morale-potion-swap-decisions`、`ev-astro-011-item-pool-ownership-weak-duplicate-protection`、`ev-astro-015-interstellar-seller-waiting-rework`、`ev-ggm-008-item-upgrade-type-synergy-handoff`、`ev-ggm-012-trait-and-item-choice-reworks`、`ev-mpig-014-enchant-synthesis-commitment`、`ev-dwarves-glory-death-loot-016-forge-recruit-growth-economy`、`ev-jk-010-post-level-three-resource-sinks`、`ev-survivor-mercs-015-gear-pool-tradeoff-lifecycle`、`ev-girls-of-the-tower-016-item-relic-card-ownership`。来源 id 均在原 [来源索引](../../../web/game-mechanics-atlas/research/deep/source-index.md)，不改研究原库。
- 原库多数记录的是 item upgrade／refine，不都等于本项目的品质档位；下文明确迁移关系。未联网新增研究，未重读全部档案全文，无当前完整材料／价格／成功率表。

### E12–E19：投入与机会

**E12 同名副本合成：多件同名装备换一件高品质。**

Magicbook 的同名同品质三件合成上一品质，见 `ev-mba-003-equipment-merge-reforge-transaction`；Auto RiskRisk 当前 Demo 也有同名装备合成，但精确数量不足。玩家先收集重复件，再把当前多个可用物品压成一件更强物品。与 D01 结合，真实取舍是给多个英雄穿、给一个英雄重复穿，还是投入合成。

好处是重复件价值清楚，升级目标可追踪；代价是核心装备成长依赖再次命中同名，高品质还可能逐级放大需求，库存会存半成品。Magicbook 玩家报告反复卸装／合成／重铸的操作负担，后加一键合成；这不证明玩家应被自动合并。项目若采用，建议由玩家主动提交，不因集齐就移走正在使用的重复件。三件只是原作规则，不是本项目已定数量。

**E13 放宽材料身份：同类／同品质的其他装备也能投入。**

Girls of The Tower 的当前玩家记录描述低阶装备按攻击／防御／辅助同类合成高阶，见其档案与 `src-gott-review-current-rules`；MPIG 有九件同 level-band／rarity 合成，见其档案及 `src-mpigdb-equipment`。这些支持按类别／档位收集材料，不证明产出必定保留指定主件身份。

迁移方案是指定一件待升装备，其他合格装备充当材料；这部分属于项目对照，需另明确输出。好处是不必死等同名，暂时用不上的奖励也有用途；代价是会消耗备用反制件、未来转型件，玩家可能把奖励选择简化成收材料。Magicbook 三件不同同品质的随机重铸产出另一件高品质物品，是相邻回收制度，不等于定向升级当前装备，也不因本题自动采用。

**E14 花金币等通用货币升级已有装备。**

Astronarch 档案明确升级 Gold 与路线、装备槽和能力投入并存，攻略规划装备升级顺序及备用套装，见 `src-astro-guide-complete`、`ev-astro-010-route-morale-potion-swap-decisions`。本题借鉴“花通用资源强化指定装备”，不移植精确价格、星级或服务地点。

优点是找到核心后能主动存钱培养，不必再次抽中同名；代价是升级与购买新装备、招募等可能争同一预算，容易出现囤钱、单核集中或通用货币用途过多。项目最终是否共用一枚货币、何时可升级，需与 R08／R09 衔接；不能把战前免费调整改成收费。

**E15 使用专门的通用升级材料／工具。**

GGManager 1.0 将 Universal Item 改为只出现基础版本，再通过 Workshop 与 Crafting Tools 升级；这是针对低品质被忽略的实际重做，见 `ev-ggm-008-item-upgrade-type-synergy-handoff`、`src-ggm-official-1-0`。不据此认定所有装备类型共用完全相同费用或只有一种成本。

迁移到项目，可由玩家将有限升级材料投入已拥有的任意合格装备。收益是把“得到核心”与“培养核心”分开，且可单独控制装备成长节奏，减少同名供给门槛；代价是增加一类资源，若材料稀少或使用范围太窄，仍会囤积等待终局件。来源、可用范围及价格未定，不附带材料商店或完整工坊建设系统。

**E16 指定配方材料：升级目标决定要寻找什么材料。**

Dwarves 的 Forge 按 biome／recipe 组织材料与品质升级，见 `ev-dwarves-glory-death-loot-016-forge-recruit-growth-economy`、`src-dgdl-companion-forge`。资料主要约 v2.0.10，不移植当前最优配方；部分配方制造新物品，也不能全部说成原件升阶。

与 E15 通用工具不同，材料可能只服务部分升级目标。它让路线、遭遇奖励和装备计划建立联系；代价是关键材料缺席会卡住已获得装备，且容易引入多种碎片和库存管理。用于本项目时需考虑有限单局的可达性，不能借长线重复刷取替代回答；具体材料种类与地图分布本题不定。

**E17 用一次成长选择／共享培养资源换装备强化。**

Just King 的社区资料描述 Smith 用 token 精炼装备，Cook／Merchant 等也会消费 token，见 `ev-jk-010-post-level-three-resource-sinks`、`src-jk-thread-level-three-economy`；这属于 2025 历史玩家解释，没有完整精炼结果表。Survivor Mercs 0.15 的 Gear 支持 level-up 升级，见 `ev-survivor-mercs-015-gear-pool-tradeoff-lifecycle`，不推定当前每次升级都能任意指定装备。

迁移方案是奖励选择时，“升级一件已有装备”占一个选项，或消耗与其他培养共用的额度。好处是升级与战斗进程同步，不需另追重复件；代价是放弃另一个成长机会，或核心成长依赖该选项出现。本项目不因此引入英雄可配置技能、token 商店或固定轮次奖励。

**E18 特殊事件提供复制／补齐机会，作为主渠道的补充。**

Magicbook 的 Blacksmith Karl 可复制已有特殊装备或补套装，玩家先通过事件取得首件再补齐，见 `ev-mba-012-event-special-set-acquisition`。复制可与其重复件合成共同服务成长，但“复制物品”和“直接升品质”仍是两个动作。事件直接指定装备升阶是项目对照，不伪称该事件原规则。

收益是提供稀有加速机会，让难得的核心有额外成长路线；代价是如果普通升级必须等该事件，容易转成等待或反复刷新。Astronarch 曾调整 Interstellar Seller 的代价，使玩家不必为等待事件而普遍延迟升级，见 `ev-astro-015-interstellar-seller-waiting-rework`；这里借鉴等待行为的风险，不将该事件说成免费升阶。频率、上限、复制范围尚未定。

**E19 投入后确定成功，或附加失败／损失／保底〔风险维度，可叠在投入方式上〕。**

MPIG 资料将两种风险分别记录：附魔失败的毁装概率从 100% 降至 50%；合成累计十次失败后下一次保底。见 `ev-mpig-014-enchant-synthesis-commitment`；不能把附魔毁装概率误写成合成成功率。玩家报告稀有、绑定及损失叠加后不愿投入，但没有完整统计。

迁移到品质升级，确定成功让玩家能计划投入后的战力；风险升级可增加赌性和资源消耗，却可能让已成型核心倒退、投入落空，尤其不适合直接照搬长期刷取容错。确定成功不等于免费，固定词条也不自动决定成功率。此处建议初期采用确定结果，不做失败、掉级或毁装；尚待用户回应。

### 对照边界与归属

装备使用次数／击杀自动成长未找到足够完整的“升级品质成本”证据；Dwarves Artifact 给佩戴者的升级成长不能误当装备自己的经验条。Combat Alchemy 玩家报告精炼后某些奖励脱装仍保留，见 `ev-combat-alchemy-002-permanent-refinement-portfolio`，这是收益归属问题，留到升级效果而不混成成本选项。

配方把不同组件合为另一件成品、出售／分解及全局经济回收均属相邻制度，不由本题自动批准。E12–E18 可多渠道并存，E19 为风险维度；不要把所有比较项变成初期功能清单。建议见 Q03-P01，I21 继续跟踪升级与供给／经济，D01／D02 不变。

2026-09-08 用户回应另记 M05-D03：材料尽量通用，升级不是推进硬门槛；可考虑独立于金币的材料及高级 Boss 稳定产出特定材料。该回应不新增原作证据，不确认 E19 确定成功或其他渠道；Boss 材料想法来自用户，不能写成已有调研结论。
