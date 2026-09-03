# Monster Train

## 身份、范围与研究深度

- `title_id`: `monster-train`
- 开发 / 发行：Shiny Shoe / Good Shepherd Entertainment；Steam App 1102190。
- 类型：三层部署、自动结算、牌组构筑 roguelite；本项目只把它当相邻类型的构筑与空间样本，不把牌组/抽牌层照搬成英雄招募系统。
- 公开时期：PC 正式发行 2020-05-21；Wild Mutations 2020-07-16；Friends & Foes 2020-09-03；Herzal's Workshop 2020-10-22；`1.4.1` 2021-01-04；The Last Divinity 测试 2021-01-08、正式 `2.0` 2021-03-25；`2.0.1`、`2.1`、`2.2` 为本研究的后续校准节点。
- 研究范围：单人标准 run，重点覆盖双氏族、三层布阵、Banner priority draw、升级/Artifact/Pact Shard/Unit Synthesis、死亡/Reform/Eaten、Wurmkin Echo/蛋、Emberdrain、四种 Seraph 和 The Last Divinity。Hell Rush、Daily Challenge、Community Challenge、mutator、mod 和续作不定义这里的标准规则。
- 深度：50 个实质来源、6 个功能分组、7 种 `source_type`；26 条标准化证据。来源足以闭合四套完整构筑、一套跨氏族模块和一条历史氏族路线，因此归为 `anchor-retained`。Gabriot 表内部含 311 条胜利与 27 条失败，但缺少代表性抽样框、记录选择规则和游戏总体分母，因此不输出总体胜率、采用率或氏族排名。

## 来源包

| 功能 | 数量 | 内容 |
| --- | ---: | --- |
| 官方发行 / 开发 / 补丁 | 10 | 正式发行、两次免费更新、Workshop、1.4.1、TLD 测试与正式版、2.0.1、2.1、2.2 |
| 维护 Wiki | 24 | 战斗/抽牌/Pyre、商店/升级/Artifact/Covenant、四种 Seraph、TLD、Pact Shards、Unit Synthesis、四条构筑的关键 owner、overstack |
| 完整攻略 | 4 | 通用构筑、Hellhorned、Melting Remnant、TLD-era 层与位移技巧 |
| 社区机制讨论 | 6 | Little Fade、Wurmkin、Emberdrain、层选择、C25 TLD 与氏族组合 |
| 逐局目录 | 1 | `Filter Gabriot Imported DLC Sheet`：338 条带胜负旗标、seed、双氏族与组件的已记录 TLD-era runs；311 胜、27 负 |
| 详细评论 | 5 | 层、容量、Banner、双氏族、升级、死亡与 DLC 控制面的长评 |

所有 source id、URL、版本边界、可访问性和限制见 `../source-index.md` 的 Monster Train 段。

## 检索日志与停止理由

- 筛完 Steam News API 的 87/87 条公开标题；读入十个会改变身份、语义、DLC、交易、反馈、排序或兼容性理解的官方节点。
- 读取 24 个 Fandom MediaWiki 页面及末修订 revision。Seraph 四变体、The Last Divinity 与 Floor Overstacking 单独深读，避免把“顶层最安全”“单层集中”“全体 buff”写成无条件规则。
- Steam Guides 发现 31 个 id，四篇完整正文已覆盖通用、Hellhorned、Melting Remnant 和 TLD-era 实战。其后 rate limit 下没有读到正文的三篇只记失败路线，不计来源。
- 读取六个完整 Steam Discussion；批量筛其余标题。Top 100 英文 Steam Review 中只保留五篇机制密度足够的长评；总评论数 14,035 只作检索范围，不作 sentiment 样本。
- 公开 Gabriot 表的 `gid=0` CSV 可解析 338 条已记录 TLD-era runs，其中 `victory=TRUE` 311 条、`victory=FALSE` 27 条；本 dossier 只用三个逐行核对为 TRUE 的 seed 闭合具体胜例。该表仍缺少代表性抽样框、记录选择规则和游戏总体分母，不能用表内 311/338 外推总体胜率。Google 429、Reddit/GameFAQs/Neoseeker 403、Bing 同名噪声、无 CUA browser、空字幕与未读 Guide 均未绕过或升级为证据。
- 停止原因：继续可访问结果主要重复“单层集中、乘法缩放、薄牌组、前坦后打、氏族组合皆可”等已知观点，不再改变 owner、构筑 pivot、空间成本、Boss counter 或生命周期理解。完整卡牌目录、所有 Champion path 和全部 Covenant 波次超出机制切片。

## 标准 run 的真实循环与决策

1. run 在八个 Rings 中选择分叉服务、战斗与奖励；TLD DLC 下达到 100 Pact Shards 才在 Seraph 后进入 Ring 9。
2. 战斗前可看到主要敌人与可选 Trial。下三层可部署单位，第四层是 Pyre；敌人每回合从底层进入，战斗中敌方先攻击，存活友军后攻击，漏怪向上移动。
3. Champion 固有进入首手；基础每回合抽五、手牌上限十。只要抽牌堆开始抽牌前仍有 Banner Unit，至少一张 Banner Unit 获 priority draw，但多个 Banner Unit 仍会竞争该保证。
4. 玩家用 Ember 打单位和法术；战后通过 Banner 奖励、Merchant of Steel/Magic、Artifact、purge、duplication 与 Champion path 调整牌组。普通单位/法术通常有两个升级槽，reroll、升级和 purge 争夺同一 Gold。
5. 每层 Capacity 是部署预算。Ascend/Descend 可把单位移进已满层形成 overstack，但该层仍有最多七单位 guard，而且以后可能无法再投放 Morsel、Imp、Tomb 或被 Reform 的单位。
6. TLD 下用 Pact Shards 换 Divine Temple spell upgrade、Gold/Artifact 或 Unit Synthesis。Shard 同时强化敌人；Unit Synthesis 会把 donor 连同升级从牌组删除，只把 essence 交给一个非 Champion recipient，且每 recipient 最多一项 essence。
7. Pyre 不是免费第四防线：漏怪立即与其交战，Pyre 虽会反击全体敌人，但伤害跨战斗保留，只能通过有限事件/效果恢复。

## 构筑一：Awoken Quick + Sweep 单层阵地

### 闭环

- **engine**：前排 Sentient / Thorned Hollow 或其他耐久单位承接首轮；后排 Shattered Shell 或 Husk Hermit 持有 Sweep。
- **state/resource**：单位升级槽、Quick、Multistrike、Regen/治疗、层 Capacity 与抽牌可靠性。
- **payoff**：Sweep 同时触碰整排敌人；Quick 把后排清场放在敌人攻击前，Multistrike 放大同一 payoff owner 的次数。
- **survival**：前排 Health/Spikes/Regen 买出成长时间；后排不因拥有 Sweep 自动获得生存。
- **spatial condition**：坦克在最前，Sweep owner 在后；选定一层集中 setup。顶层通常给更多抽牌/成长时间，底层则更早处理威胁。
- **payoff owner**：Husk Hermit / Shattered Shell 的攻击；前排只供应时间和可能的 Spikes，不把全队防御自动换成输出。
- **economy/pivot**：在 Merchant of Steel 寻找 Quick/Multistrike 与 survivability 时，必须在同一 Gold、reroll 和两升级槽预算里取舍。没有 Sweep/Quick 时先保留普通 backline clear，不能只等终局件。
- **counter/abort**：TLD 顶层 Sweep 会直接碰后排；Seraph Temperant 的 Sap 压输出；Chaste 减半 buff；Covenant 20 中层少 1 Capacity。缺 front tank、后排保护或升级槽时不能把“有 Sweep”误判为成型。

逐局表中的 2021-08-23 Husk Hermit 记录属于后文 Primordium 喂养构筑；它只说明 Sweep body 可成为单一 recipient，不额外证明 Quick 必然出现，也不把两种 Awoken shell 合成固定最优表。

## 构筑二：Fire Light Little Fade + Reform 死亡阵地

### 闭环

- **engine**：让 Little Fade 在可控位置死亡；Fire Light 在每次死亡时给同层友军增加 Attack/Health 并延长 Burnout，再用 Primitive Mold / Reform 把她从死亡池送回。
- **state/resource**：Little Fade 死亡次数、死亡/Consume 池、Burnout 回合、同层友军、手牌与 Reform 抽取。
- **payoff**：同层 Railbeater、Votivary 或其他攻击者累积本场战斗内跨死亡/Reform 保留的 stat change；Fire Light 自身是触发 owner，不必承担最终攻击。这种 combat-persistent growth 不跨 battle/run 永久累积。
- **survival**：Railbeater/其他前排保护脆弱后排；Reform 既是递归引擎也是死亡后的恢复入口。
- **spatial condition**：Fire Light 只强化同层；需要保留可死亡位置、前排位置与被 Reform 单位的 Capacity。盲目 overstack 会封死墓碑/回场单位。
- **payoff owner**：Little Fade 供应死亡事件和 floor buff；最终存活攻击者拥有伤害。Primitive Mold 只拥有回收。
- **economy/pivot**：早期用廉价 Reform/前排桥接；当死亡池被无关单位污染、Burnout 对不上或后排保护不足时，应转向少数高质量死亡对象，而不是继续加所有 Extinguish/Tomb。
- **counter/abort**：TLD 顶层 Sweep 直接打后排、底层 Trample 越过 disposable blocker；Patient 会攻击并施加 Melee Weakness；错误死亡顺序或抽不到 Reform 会让 engine 断链。

2021-09-27 的 Melting Remnant Exiled + Hellhorned 150 Shards 记录为 `victory=TRUE`，包含 Fire Light III、Railbeater、Votivary、Primitive Mold、Remnant Pact 与 Dripfall；seed `1323934214`。这证明该闭环曾在 TLD 胜例中成立，不代表总体胜率。每次 Dark Forge 的同一级升级选择互斥，但三个升级节点允许 Fire Light / Eternal Flame crosspath；本条记录的 Fire Light III 是三个节点全选 Fire Light 的纯路线，不能把 Eternal Flame 自身的死亡成长、Endless 或单体输出能力算作 Fire Light III 固有能力。

## 构筑三：Corruptor Spine Chief + Bog Chrysalis

### 闭环

- **engine**：Infused cards / Fracture 生成 Charged Echoes；Corruptor Spine Chief 读取同层 Echo 数，为同层单位加 Attack；Bog Chrysalis 通过 Shell 计时孵化。
- **state/resource**：Echo、Shell、蛋的 1 HP、生存回合、同层 Capacity、蛋上的升级/stat change。
- **payoff**：Bog Chrysalis 孵化两只 Bog Fly；它们继承蛋已记录的升级/stat change，Multistrike 会被两个结果同时放大。
- **survival**：蛋在孵化前需要护住；Champion 的三 Capacity 与前排会挤压同层空间。
- **spatial condition**：Corruptor 只给同层单位；蛋必须在其层内并有生存时间。Overstack 可集中多个 reader，却也封死继续投蛋和功能单位的空间。
- **payoff owner**：Spine Chief 拥有 floor attack bonus；Bog Chrysalis 拥有孵化事务；两个 Bog Fly 分别拥有攻击。升级继承不等于所有临时 buff、trigger、essence 或 report lineage 都无条件复制。
- **economy/pivot**：Divine Temple 可把一个 donor essence 压到单一 recipient，但付 25 Shards、删除 donor 与其升级，并提高后续敌人压力。没有可靠 Echo 供应或蛋孵化时间时，应转为现成 Banner carry，而不是继续为理论双 Fly 付 Shards。
- **counter/abort**：蛋被 Sweep/AOE 提前击杀、Diligent 污染关键 spell 周转、TLD 三层同时开战、Shard 强化过快和容量不足都会攻击不同 link。

2021-07-11 的 Wurmkin + Umbra 125 Shards 胜局记录 Corruptor III、Bog Chrysalis、Space Prism、Perils of Production、Void Binding、Accelerated Incubation、Fracture；seed `164305946`。这是完整组件样本，不是 Wurmkin 唯一答案。

## 构筑四：Aggressive Edible Primordium + Husk Hermit

### 闭环

- **engine**：把 Primordium 放在目标 carry 前方，让 Eaten 分批把 stats 交给前方单位；Aggressive Edible 提供更高 Attack 供应，其他路径可能改变 status/数量转移。
- **state/resource**：Primordium 的 remaining Buffet 次数、可转移 stat/status、carry 升级槽、层 Capacity、Emberdrain 债务与保护回合。
- **payoff**：Husk Hermit 的 Sweep 把转入的单体成长广播到整排敌人；Furnace Tap 的 Multistrike、Void Binding 的 Rage/Damage Shield 可继续乘同一攻击 owner。
- **survival**：Damage Shield、前排位置和控制买出进食/攻击时间；它们不自动拥有最终输出。
- **spatial condition**：Primordium 必须位于 eater 前方，且同层先后顺序正确；把 carry/食物放反会把 engine 交给错误 recipient。
- **payoff owner**：Primordium 是 supplier，Eaten 是转移事务，Husk Hermit 是最终 Sweep owner，Furnace Tap/Void Binding 是状态/规则模块。
- **economy/pivot**：先拿可独立工作的 Sweep body，再决定是否围绕 Primordium、Multistrike 与 Rage 加倍；如果 Banner 抽牌、容量或 Ember 解法未闭合，保留一个普通前排比继续上债更重要。
- **counter/abort**：Chaste 会持续减半 Rage/Damage Shield 等状态；Temperant 降 Attack；TLD 顶层 Sweep/中层 Multistrike/底层 Trample 分别要求后排保护、坦度和漏伤处理。

2021-08-23 的 Umbra Exiled + Awoken 130 Shards 胜局记录 Aggressive Edible III Primordium + Husk Hermit、Void Binding、Furnace Tap、Space Prism；seed `1910063764`。它说明“食物 supplier + 独立 Sweep carry”可闭合，不代表任意 Morsel/任意盾都能转成伤害。

## 构筑五：Emberdrain 跨氏族模块

Emberdrain 不是一条独立标签阵容，而是“当前回合超额收益、未来回合 Ember 债务”的可插拔模块：

- Furnace Tap 用未来 Ember 换 Multistrike；Void Binding 换 Rage/Damage Shield；Perils of Production 同时供应 Ember/Rage，并常通过 Holdover 重复承担债务管理。
- 0 费牌、直接 Ember 生成、让带债单位死亡/Reform、让其被 Eaten/despawn、Sap/Daze/Quick 或直接结束战斗，都是不同的还债/绕债路线。
- Covenant 10 会让顶层首个单位带 Emberdrain，Seraph Chaste 波次也带 Emberdrain 压力；这使该状态同时可能是玩家主动融资和敌方施加的行动税。
- 每个版本必须明确状态 recipient、施加时机、下回合扣减、死亡/Eaten 退出语义和最低可执行牌组。把“本回合很强”当作免费增益会让下一回合整手牌失效。

2021-08-23 Primordium/Husk 与 2021-07-11 Corruptor/Bog 两个胜局都携带了 Perils/Void Binding 中的部分模块，但最终 payoff owner 不同；这正说明 Emberdrain 是横向经济/节奏轴，而不是元素或盾体系本身。

## 历史 Hellhorned Brawler / Reaper 参考线

发行期攻略把 Brawler / Reaper Champion path 放在 Rage、Armor 与前排保护中讨论：Brawler 需要攻击次数与 Rage 放大，Reaper 需要 Slay 成长和安全击杀窗口。该线用于说明“同一氏族内也有 cadence reader 与击杀 reader 的分工”。2023 C25/TLD 讨论认为 Reaper 在 DLC 环境更难使用，因此这里不称其为当前强势，也不编造一个 post-2.2 合法最优清单。

## 空间、抽牌与容量不是装饰

- **层**：底层最早接敌，给输出的准备时间最少；顶层通常最晚接敌，却会受 Covenant 10 首单位 Emberdrain、TLD Sweep 和具体 floor artifact 影响。
- **单位顺序**：敌人先打，普通结构因此前坦后打；Harvest 可能希望特定死亡发生在 reader 前，Little Fade 可能要主动位于承伤位，不能把“坦克永远在最前”当成全局规则。
- **抽牌**：Champion 固有首手、Banner priority draw、五抽和十手牌上限共同决定 setup 可靠性。薄牌组不是抽象美德，而是让关键 floor 在漏怪前闭合。
- **容量**：单位本体、Champion、蛋、Tomb、Morsel 和 Reform 回场都争同一层 Capacity。Ascend/Descend 的 overstack 是用卡位/位移资源换超容，不会恢复继续部署权。
- **Pyre**：漏怪把当前层失败转成 run 级永久生命损失；战斗预览应帮助玩家判断是底层早杀、顶层成长还是多层兜底。

## Artifact、升级、Synthesis 的所有权与机会成本

- 单位/法术升级附着于具体卡，通常最多两个槽；Quick、Multistrike、cost reduction、Magic Power 等不能当作免费全队属性。
- Artifact 是 run 级规则 owner；Merchant/reroll/purge/duplication 是路径与 Gold 交易；Champion path 是 Champion 专属成长。它们必须和单位升级分层展示。
- Unit Synthesis 是 donor 删除 + essence 转移 + recipient 单项上限 + 25 Shards 风险的事务，不是两单位合体后保留所有内容。donor 的升级随 donor 离开，recipient 只得到定义过的 essence。
- Pact Shards 把获得构筑压缩力的收益与敌人强化、Ring 9 资格绑定。玩家不能只看“更强升级”，还要看何时跨 100、是否有能力处理 TLD 三层考题。

## 敌人包如何攻击不同 link

| 敌人包 | 主要攻击的 link | 适应窗口 |
| --- | --- | --- |
| Seraph the Temperant | 玩家单位入场时带 Sap 3；之后 Seraph 每回合向一个楼层的单位施加 Sap 3，压低攻击型 payoff | 预览后增加独立倍率、更多攻击者或减少对单一 Attack stack 的依赖 |
| Seraph the Chaste | 每回合把一层的友方 buff/敌方 debuff 减半，并有 Emberdrain 波次 | 提高施加频率、在 Relentless 再投入关键 buff，或改用 base stat/独立伤害 |
| Seraph the Diligent | 每回合首个 spell Consume，塞 Vengeful Shards，Purifier 继续污染 | 准备 filler/本来就 Consume 的牌、Consume 回收、后排清除和更薄牌组 |
| Seraph the Patient | 提前攻击、Melee Weakness，Rally/Incant 增攻 | Silence、Daze、Stealth、Damage Shield、位移或小型第二层诱饵；避免无意义触发 |
| The Last Divinity | 顶层 Sweep、中层 Multistrike、底层 Trample，三层同时 Relentless | 不允许所有防御依赖同一种前排；分别准备后排生存、正面坦度和越坦伤害处理 |

这些是不同 counter package，不应合成一个“终局 Boss 抗性”。失败说明应指出：输出被 Sap、状态被减半、关键 spell 被 Consume/污染、Patient 被 Rally/Incant 喂大、哪层被 TLD 的哪个关键词击穿。

## 死亡、Eaten、Reform、孵化与事件谱系

- Wild Mutations 把 Morsel 的 Eaten 从“死亡”改为 despawn，主动切断 Umbra Morsel → Melting Remnant Harvest 的强制联动，也避免敌方 Harvest 因玩家正常进食获利。
- Friends & Foes 把 Eaten pile 从 Consume/Reform pool 分离；临时 status 在死亡后不保留，stat change 则在本场战斗内跨死亡/Reform 保留，但不会因此跨 battle/run 永久累积。死亡、Eaten、Consume、Purge 和 Reform 因此是不同事件/容器；这也必须与 Little Icarus / Bounty Stalker 等明确跨战斗 `permanent` 的成长分开。
- Bog Chrysalis 孵化两个 Bog Fly，并把蛋记录的升级/stat change 交给两个结果；这是具名 spawn transaction，不是“所有召唤继承所有来源状态”。
- 2.2 尝试修正多单位同时死亡后的生成位置又因副作用回滚，说明 spawn order、原位置、空位仲裁、来源归属和取消原因必须作为 resolver 的显式契约。

## 生命周期、负案例与版本冲突

本 checkpoint 计入 14 个 materially distinct negative/reworked family：

1. Eaten 从死亡改为 despawn，拆除跨氏族 Harvest 强制环；
2. Eaten pile 与 Consume/Reform pool 分离；
3. 临时 status 死亡清除、stat change 在本场战斗内跨死亡保留且不跨 battle/run 的边界；
4. Divine Temple 连点导致多张单位被删的交易 bug；
5. Capacity 不足召唤缺少失败反馈；
6. For The Greater Good 因复杂 bug 整体重做；
7. Votive Key 限定“从手牌打出”以修正隐式触发；
8. Room/Floor 术语修正以避免空间归属混淆；
9. TLD Purify 文本与真实效果不一致；
10. 多单位死亡后的 spawn placement 错误；
11. placement 修复因副作用在同一 2.2 节点回滚；
12. 官方 Workshop 明示第三方 mod 可崩溃/卡死且不受支持；
13. TLD 测试明确旧 mod 可能不兼容；
14. 发行期 Brawler/Reaper 实践不能直接外推到高 Shard/TLD，当前社区指出环境考题已改变其可用性。

TLD 新增 Wurmkin、Pact Shards、Unit Synthesis 与三层 Boss 是版本扩展，但不单独填充 negative 计数。普通 bugfix、数值调整和玩家偏好也没有机械拆条。

## 对本项目可迁移

- 盾/防御与元素是正交轴。Monster Train 的 Armor、Damage Shield、Health、Regen、Stealth 与 Quick 都能买生存时间，但最终输出仍由 Sweep、Multistrike、Rage、Slay、Corruptor floor buff 或其他具名 reader 持有。
- “防御转输出”需要显式 converter 和唯一 payoff owner。可迁移的是 supplier → stored state → converter → recipient → final attack 的结构，不是复制 Monster Train 卡牌或把所有盾自动变伤害。
- 同一个防御 chassis 可以服务不同元素/氏族；同一个元素/氏族也能有多个输出 owner。Ice Shield/Earth Shield 若进入本项目，应共享盾的承伤/刷新/破裂语义，同时保留元素 supplier、反应、counter 与装备/遗物槽成本。
- 单层集中产生乘法协同，也产生明确风险：Capacity、后排暴露、TLD 三层考试、位移/超容后的部署锁。阵地强度必须与空间机会成本一起设计。
- 死亡、消耗、退场、召唤、继承和回场必须是不同事件；临时单位的 owner、占格、继承字段、触发资格、清理和报告谱系不能靠“召唤物”一个标签含混处理。
- 敌人包应分别攻击输出值、状态维持、牌/技能燃料、触发频率、后排生存和越坦伤害，而不是直接宣布某体系免疫/失效。

## 不兼容与未决

- 牌组、五抽、十手牌、Ember、Consume pile、每回合打牌与 Ring 路线不属于本项目已接受的英雄 roster + 两战术命令契约；只迁移可靠性、机会成本、时机和失败解释。
- 三条纵向楼层与本项目方格战场不同；可迁移的是“不同空间带来不同准备时间和敌人权限”，不能照搬上下楼。
- Gabriot 表有 311 条胜利和 27 条失败旗标，但仍缺少代表性抽样框、记录选择规则和游戏总体分母；Steam Guide/Discussion/Review 也不能补足这一统计口径。未决：不同组合的真实采用率/胜率、末版 C25/TLD 分布、完整 action resolver、所有继承矩阵与 mod 影响。
- 本 dossier 不确认任何首版英雄、盾/元素清单、遗物、装备或数值。若后续讨论团队额外生命/防御转给单一射手，仍需定义 supplier 集合、bonus/base 口径、converter owner、recipient、是否含自身、读取时点、cap、refresh、slot、来源谱系和 counter。

## Disposition

`anchor-retained`。

理由：50 个实质页面覆盖正式规则、四条具名构筑、跨氏族债务模块、空间/抽牌/经济、四种 Seraph、TLD、14 个生命周期/失败 family 和三个逐行核对为 `victory=TRUE` 的 seed 胜例；来源类型与版本密度达到 anchor 门槛。研究在 owner、pivot、counter、空间成本和生命周期已饱和，继续枚举卡牌不会改变本项目设计理解。所有结论仍是探索语料，跨游戏 synthesis 继续 `Withheld`。
