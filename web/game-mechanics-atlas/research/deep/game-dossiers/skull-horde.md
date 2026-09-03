# Skull Horde

## 身份、时期与资料密度

- `title_id`: `skull-horde`
- 精确对象：Steam App `3199360`，开发 / 发行均为 8BitSkull；Demo App `3762070` 于 2025-06-20 发布，正式版于 2026-04-10 发布。
- 访问日最后一个明确 main-branch 官方版本为 2026-06-16 的 `v1.032`。官方同时提供 experimental 分支，但其后续变化不与正式分支规则合并。
- 本 checkpoint 收录 34 个实质非商店来源：13 个官方发布 / 补丁 / 重做节点、2 篇完整 Steam Guide、9 个机制 / build 讨论、7 条实质 Steam 长评与 3 篇独立评测。
- 置信：中高。普通六线 roster、三合升级、双货币、threat clock、死亡 / respawn / life、The Many、Garg 与 Adam 三套构筑、尸体消费、Rally owner 和主要生命周期均可闭合；但没有维护 wiki / 当前数值数据库 / 使用率统计 / 完整召唤继承表，因此为 `retained`，非 anchor。

## Adaptive-depth 决定与路线审计

这款游戏的价值不是“骷髅题材”，而是把 roster 单位、临时生成物、敌尸复生、尸体资源、死亡货币、复活队列、全灭 life 和可控 Rally 分成不同 owner。它直接检验“尸体 / 召唤 / 人口”能否被写成真实规则，而不是把所有屏幕实体笼统称为亡灵群体。

研究覆盖 64/64 官方新闻标题并读取正式版关键正文、282/282 Steam Discussions 标题并打开约 20 个核心主题、4/4 Guides、1,088/1,088 Steam reviews，以及多组外部英文检索。两篇 Guide 可计：The Many 完整构筑与当前成就 Guide；Correct Doll 过窄，Full Game Walkthrough 正文为空且只有视频，因此不计。六个 YouTube 候选虽暴露自动字幕 track，但 timedtext 均返回空正文；标题、缩略图、描述和无字幕画面不作证据。GameBrief 把升级写成“两单位相邻合并”，与多源确认的三合一、非固定邻接规则冲突，作为排除路线而非来源。3rd Strike 返回 403，未绕过；未下载、安装或解包客户端。

34 个来源后，新增材料只重复角色解锁、泛化 item 推荐、尸海画面与早期 grind 评价，不再改变三套 build 的 engine / owner / pivot / counter，也无法补成官方召唤继承表，故按 adaptive-depth 停止。

## 来源包与版本边界

- 官方发布、Garg Update、`v1.008`、`v1.011`、`v1.013`、`v1.014`、`v1.015`、`v1.018`、`v1.020`、Dodge overhaul、`v1.026`、Blood Feast 与 `v1.032` 分别承担版本、尸体 / 实体 guard、Adam/Garg/The Many、Oath/可读性、Evasion、反伤修复、治疗/overheal 模块与当前分支边界。
- The Many Guide 于 2026-06-11 更新，但仍沿用 2026-05-01 已被 Evasion 替代的 `dodge scaling` 术语；其死亡经济、单位职责、banish 和早期脆弱结构可保留，精确 Dodge 数值与旧缩放建议降级。
- 2026-08 Achievement Guide 接近当前，支持 Adam unlock/Rot 复生与 Sharpshooter crit 模块；其中“每层 Rot 0.75% 自动复活”只作为 Guide 规则，不升级为官方公式。
- 讨论与评论支持实际 build、Rally/AI、pool dilution、失败与可读性；`OP`、`最强`、`无限`、具体 .7 秒 respawn、无上限 peon 和胜率均不当作正式规则。

## 基础循环、经济与控制边界

每层中，玩家移动 Skull、用 Rally 令普通单位向自己回拢，并在 cooldown 到点时使用 Skull 主动能力；单位自动移动、索敌与攻击，玩家没有逐单位编组 / 分散 / 点杀控制。探索、击杀与场景资源提供 Ducats 和 Corpuscles：前者用于招募 / reroll 单位，后者用于物品 / 相关 reroll。Peril 以持续 threat clock 迫使玩家在继续刷资源与敌人数值 / 数量增长间取舍；Explorer 改用逐层增长，不能把两种时间压力混成同一规则。

标准 Skull 同时维持六条单位类型线。商店展示三个候选，可 reroll；三个同名基础单位合成 Tier 2，再以三个 Tier 2 合成 Tier 3。高阶稀有度会扩充候选池并引入更专职单位，因而既提供新 build owner，也稀释已有单位线。更换整条单位类型会清掉其升级进度，只退部分费用；垂直升级、横向换线、扩池、banish 与 reroll 是同一经济问题。

单位死亡后进入有序 respawn，长短由单位基准和修正决定；公开证据只足以确认按死亡顺序返回与 cooldown 可见，不足以确认统一公式。普通 Skull 在全军死亡时失去一条 life，基础为三条。Rally 是普通 roster 的回拢输入，而不是所有友方实体的全局控制：`v1.013` 明确让 Adam 的 reanimated enemies 不响应 Rally，之后又修复按住 Rally 时它们卡住的问题。

## 三套人口语法，而非一种“骷髅群”

### 标准 roster

六个单位类型槽是可招募、升级和替换的持久 roster owner；同类三合提高 Tier，顶级单位参与 Oath / tag 层。单个类型线中可有多个单位，但“六线”不是所有当前存活实体的统一上限。

### The Many swarm

The Many 不能升级单位、不能获得 Oath，只使用 Tier-1 单位并通过更大的招募数量、批量复活能力和死亡经济建立密度。它改变的是升级 / Oath / 数量 / respawn 语法，不是简单给普通 roster 加几个槽。`v1.018` 为其补 Churl/Carver/Hurler 起手、把能力目标数从 3 提到 5，并调整 / 封顶 flat stat；`v1.020` 又加入提高 respawn rate、降低 max Health 的明确交换。

### Adam enemy reanimation

Adam 的额外前排来自敌尸，而不是商店 roster 复制。Carrion Strike + Rot / Psychoactive Fungus 让敌人死亡后被复生；这些单位不响应 Rally，具有独立 AI / path owner。正常 roster 先负责产生第一批尸体，复生敌人接管前排，随后原生单位转远程 / support。它必须与 Necromancer peons、Druid plants、Rat Nest rats 分开：四者生成源、生命周期和潜在人口 / 继承规则不同。

社区观察称上述多种生成物会显示 item / buff，但没有官方完整表说明哪些属性、tags、on-hit、on-death、Oath、装备或全队效果继承。研究只保留“可能继承某些效果”的中置信观察，不将它扩写成统一召唤物规则。

## 构筑一：The Many 死亡经济 + 快速复活 swarm

可核实的核心单位为 Necromancer、Druid、Petard、Hurler、Carver，Cleric 可作早期过渡。Petard 主动自爆；`Charon's Obol` / `Charon's Tongue` 在友军死亡时产生 Ducat / Corpuscle，cooldown 与 respawn 缩短把死亡转成可重复资源事件。Necromancer peons 和 Druid Root 提供额外数量 / 控制，Hurler 与 Carver分别承担远程读取与 speed bump。

- **engine**：高数量 Tier-1 roster、Petard 自爆、友军死亡事件与快速 respawn。
- **state/resource**：Ducats、Corpuscles、死亡队列、respawn cooldown、活体密度、ability target count、Burn/Plague/Rot/Root/Crit/Evasion 状态。
- **payoff**：Obol/Tongue 将死亡转为招募 / 物品经济；Parting Gift、Chilling Scream、Burn/Plague/Rot、on-crit/on-dodge/on-death 把大量事件转为伤害 / 控制。Necromancer/Druid/Hurler 或状态读者拥有战斗输出，不由“人多”抽象地统一拥有。
- **survival**：快速返回、Evasion / invulnerability、Bone Golem 或廉价前排持续占场；全灭仍会损失 life。
- **spatial condition**：玩家用 Rally / 自身移动维持普通单位在有效距离；聚团能保输出也会被 AOE / Crescendo 一次清场。
- **payoff owner**：Petard / dying ally 提供 death event；Charon items 拥有货币转换；状态 source/reader 拥有伤害；The Many ability 拥有批量返场。
- **economy / pivot**：先冲 recruitment 3 与 Necromancer/Druid/Petard，再找 Obol/Tongue；建立死亡经济后才扩 Burn/Cold/Crit/Rot。Banish 对 Tier-1 基础值收益差的百分比池，保留 flat 或稳定触发项。
- **counter / limit**：第一 elite/Boss 前引擎未齐最脆弱；AOE、Crescendo、单次大伤、全灭 life、招募递增成本和 flat-stat cap 抑制无脑扩张。Guide 的旧 Dodge 数值不作当前解法。

这套 build 的关键不是“死得越多越强”，而是死亡事件同时经过 respawn、货币、状态、场上占位和全灭风险五个 owner；任何一个环节缺失都可能把引擎变成纯损耗。

## 构筑二：Garg 链锁 Champion 质量流

Garg Update 把主动能力改为一次连接到最大链锁数，按单位类型调整 chain distance、提高上限并降低非链单位 debuff；最大链数节点被前移。`v1.032` 又修 Garg 招募价格 cap。当前 `v1.032` 后评测给出 Dreadmaul 前排，Fire/Ice/Plague Mage 与 Druid/Necromancer 后排 / 控制，基础优先 Four Leaf Clover、Whetstone、Pair of Dice，传奇 / 模块包括 Broadsword、Coup de Grace、Ash Husk、Mercy Kill、Conflagration、Death Feint、Desert Winds、Vulture、Last Stand、Grit、Sodden Gauze。

- **engine**：Garg chain 一次覆盖有限数量 Champion，把 buff / debuff 与角色主动集中到被链单位。
- **state/resource**：链锁名额、距离、被链 / 非链状态、Champion 招募价格、Crit/Armor/regen/element 状态与 item slots。
- **payoff**：可选择链住 crit gunners / explosive mages，让链锁远程拥有爆发；或链住 Dreadmaul / Bone Golem 前排，让其拥有生存与近战 payoff。
- **survival**：链前排时 Armor、HP regen、Last Stand/Grit/Sodden Gauze维持至少一个单位存活，为死亡队列争取返回时间；链后排时未链 beefy frontline 承伤。
- **spatial condition**：chain distance 与单位移动决定是否接入；玩家不能固定 formation，只能用 Rally / Skull 位置影响接近。
- **payoff owner**：Garg 拥有 chain 规则；被链 Champion 拥有 buff 与攻击；未链 support 提供治疗 / 控制 / 状态；item/relic 拥有 conversion / trigger。
- **economy / pivot**：前期 chain 重做消除逐个连接的慢启动；中后期 roster 扩大后，chain cap迫使玩家选“链输出”或“链前排”，其余位置转专职 support。
- **counter / limit**：前置爆发、链距断开、后排访问、AOE 和非链 debuff 会攻击不同环节；不能从社区 `Garg is insane` 推导通用最强。

Garg 与 The Many 是相反的投资语法：前者把有限名额集中为质量核心，后者把低阶死亡 / 返回扩成频率引擎。两者都需要显式 owner，而不是一个笼统的“亡灵加成”。

## 构筑三：Adam 敌尸复生前排 + 原生远程支援

当前成就 Guide 将 Adam 解锁与 Carrion Strike、Rot、Psychoactive Fungus 和三名敌人复生关联；社区实践补足了真正的启动顺序：先用自有 fighters 杀出第一批尸体，复生敌人接管前排，然后把普通 roster 转为 Herald + Jester 等远程 / support。`v1.013` 把起手改为 Bone Golem + Jester、移除 Bone Rat，并明确 reanimated enemies 不响应 Rally；`v1.026` 修按住 Rally 时其卡住。

- **engine**：原生单位制造敌尸；Adam ability / Rot 系统将合格敌尸转为 reanimated bodies。
- **state/resource**：敌尸、Rot stacks、复生机会 / ability target、reanimated live bodies、普通 roster、Rally 状态与 respawn/life。
- **payoff**：敌方单位转为前排占位与承伤，Herald/Jester/远程原生单位拥有持续输出 / support；复生体不是输出归因的默认 owner。
- **survival**：复生敌人吸收接触伤害；Bone Golem 帮助首批 corpse seed；全灭与高爆发仍可在循环启动前结束 run。
- **spatial condition**：需要敌人在可复生状态死亡；reanimated 采用独立 AI 且不受 Rally，玩家只能间接控制普通单位围绕 Adam。
- **payoff owner**：原生 fighter 拥有首次 kill；corpse 保留来源；Adam/Rot reader 拥有 reanimation；复生实体拥有后续承伤 / 攻击；普通远程拥有终局 payoff。
- **economy / pivot**：开局保留足以杀敌的 fighter，不可直接全换 support；尸体循环稳定后再向 ranged/support 倾斜。
- **counter / limit**：无尸体 / 不可复生目标、尸体消费竞争、AOE、错误路径、Rally 无权控制、复生继承不透明和实体 cap 都会断链。

Guide 的每层 Rot 0.75% 只能作为版本化作者说法。若没有当前官方公式，界面和报告必须区分“由主动能力保证复生”与“Rot 触发的概率复生”，不能把两者合并成一个尸体掉落率。

## 补充模块：Sharpshooter 与 Blood Feast

- 当前 Achievement Guide 的 Sharpshooter Boss module：高 Crit chance / damage、Sharpshooter、Whetstone、Hand Wraps、Poise、Balanced Hilt、Incisive + Spectacles、Spring 重置 attack cooldown，以及 Tier-3 Sharpshooter crit instant kill。它是结构完整的单体 / Boss 模块，但独立实践交叉较弱，不提升为第四套主 build。
- 2026-05-23 Blood Feast 是官方 authored module：Margrave heal-on-hit / transfusion，Dark Pact / Bloody Fangs 让全队攻击回血，Bat 提高所有治疗，Platelets 把 Corpuscles 转为治疗，Fatal Vigor 让满血增伤，Blood Golem 由持续 overheal 召唤。它说明 healing、满血窗口、overheal summon 与货币转换可连成一条链，但不证明玩家 meta 或强度。

## 尸体、Vulture、死亡触发与实体 guard

- Vulture 自动消费附近尸体换额外 loot；2026-04-13 官方因同时吃过多尸体加入 consumption-rate cap。它与 Adam 竞争同一 corpse state 时必须有确定消费顺序，不能一个尸体同时结算 loot 与 reanimation。
- `v1.011` 减少 concurrent corpse limit、改善高敌人数 / 聚团、增大对象上限并在资源多时扩大拾取范围。这是 runtime / readability guard，不等于改变 roster 人口。
- Yorick 在 `v1.014` 优先选择自带 death effect 的单位，证明 trigger selector 会影响 build，而不是随机事件都同权。
- Charon death currency、Petard 自爆、Blood Bond、Parting Gift / Chilling Scream 能形成 source→death→reader→payoff 链。需要 recursion guard、每事件结算次数、corpse state 与被动触发来源归因。

## Oath、扩池、Banish 与换线成本

普通角色通过 Tier-3 单位取得 Oath / tag build layer；The Many 明确没有这条轴，Garg则只招募 Champion，因而更容易追 Oath，但会受链数和 Champion 价格影响。提升 rarity 扩充单位池，既开放 Rat King / Mage 等专职结构，也降低已有线出现率。Banish 是修剪物品 / 单位候选的 correction tool，官方后续按 unlock pool 重新定价；它的价值取决于当前 pool dilution，而不是固定“越多越好”。

替换单位线会丢升级进度并只退部分成本，形成真正的 pivot window。敌方 threat、Boss 预告、当前 pairs、banish / reroll 成本与现有 Oath commitment 应共同决定是否换线；如果只显示新单位强度而不显示丢失的 Tier/Oath/价格，就会把战略成本隐藏成菜单陷阱。

## 反制、失败解释与可读性

- AI / AOE：玩家不能单独移动单位；自动单位可能分散被逐个击杀，或聚团被 cleave/AOE 一次清空。Rally 能回拢普通单位但不是“散开”命令，Adam reanimated又不服从它。
- 时间 / 资源：Peril 鼓励多刷 Ducats/Corpuscles，但每次 threat tick 提高敌人压力。失败可能来自贪资源、reroll 菜单耗时、引擎晚一层上线或 Boss counter 缺失，报告应标注时间花在战场还是菜单。
- survival / death：单个单位死亡可通过 respawn 恢复，全灭却消耗 life；玩家需要存活单位数、下一返场时间和“距全灭一击”的非颜色警告。
- attribution：首发长评明确批评缺少单位/技能/减伤报告；The Many 与 Adam 尤其需要把原生攻击、召唤、状态、death reader、尸体、货币和复生分开，否则无法判断 build 是伤害不足、占位不足还是引擎未启动。
- visual / tooltip：2026-08 长评仍报告有限色彩下 Skull、友军、敌军、投射物混淆；Armor tooltip 曾被开发者确认长期过时。标题、图标、颜色不能替代精确公式、owner 与当前版本说明。

## 生命周期与负案例

本 checkpoint 计十四个 materially distinct negative / reworked families：

1. **Garg 慢启动 / 误链体验重做**：一次能力改为连满可用链数，并调整距离、cap 与非链 debuff。
2. **The Many 多阶段重平衡**：1.008/1.018/1.020 依次处理起手、目标数、flat cap 和 respawn↔Health 交换。
3. **Dodge scaling → Evasion**：旧攻略术语 / 数值失效，生存轴整体重做。
4. **Armor tooltip 与真实结算不一致**：开发者确认过时；玩家将连续减伤误读为 500/1000 离散台阶。
5. **Vulture 同时吃太多尸体**：加入 consumption-rate cap，约束尸体经济吞吐。
6. **尸体 / 高实体 / 聚团表现上限**：1.011 调 concurrent corpse、对象与拾取范围，暴露实体 guard 需求。
7. **Adam owner / Rally / path 修复**：起手换队、reanimated 不响应 Rally、后续修 Rally 按住卡死。
8. **Oath progress / 即时触发 / tag change 可视化**：补丁持续补反馈，说明 trait owner 与触发时点不可只靠单位名。
9. **Banish 与 unlock-pool 稀释**：引入后按池重新定价，correction cost 必须随可抽池变化。
10. **Thorns / Riposte 完全不工作**：`v1.026` 修复；强度讨论前必须先验证触发有效。
11. **save corruption → 自动备份 / 恢复**：官方生命周期把 run/meta 存档视为事务而非单文件覆盖。
12. **Burn/Plague/Rot/Freeze/reanimated achievement 不结算**：多类来源归因问题被修复，不能把未记账误判为无效 build。
13. **Chilled/Frozen stack crash**：`v1.032` 修复状态叠层导致的崩溃，要求堆叠上限 / 转换 guard。
14. **高 stack 图标与战斗识别负荷**：官方提供关闭过多 stack icons 的选项，当前评论仍报告单位 / 投射物归属混淆。

Dynamic Curse 随使用 build 衰减与信息页、Blood Feast 新模块仍有研究价值，但前者当前因果 / 公式不足，后者是新增正向内容；不为追求数量拆入本次十四例。Cumulative explicit negative/reworked cases：194。

## 对本项目可迁移

- **人口至少要分四本账**：持久 roster slots、当前 live bodies、临时 summon/reanimated occupancy、全局实体 / 特效预算。标准六线、The Many 和 Adam 证明它们不能用一个 `unit_count` 代替。
- **尸体是竞争性状态**：复生、loot、on-death 与 corpse cleanup 都要声明读写顺序、消费 owner、过期与去重；同一 corpse 不能默默被多个 payoff 重复结算。
- **死亡引擎需要完整风险闭环**：death currency 只有在 respawn、全灭 life、event cap 与 return timing 同时存在时才是 build，而不是免费资源发生器。
- **盾 / Armor 是 uptime，不天然是伤害**：Garg 可把 chain 集中给前排，让独立 mage/gunner 输出；Armor 也可与 healing / Thorns 组合。若本项目让全队额外生命 / 防御转给 carry，必须由 relic/equipment 明确 read scope、recipient、slot cost、cap、refresh、snapshot 与 counter。
- **元素 / 状态与防御是正交轴**：Ice/Plague/Burn/Rot 可以依附不同 supplier / reader；“冰盾”和“土盾”可以都遵守 Shield 基础语义，但元素只改变生成、维持、break reader 或 counter，不能让名称自动拥有另一套人口 / damage 规则。
- **Rally 必须声明控制域**：普通 roster、召唤物、复生敌人、不可控 deployable 是否响应同一指令要逐类定义，并在不响应时给出非颜色反馈。
- **报告保留 lineage**：source、death/corpse、converter、status reader、summon、final hit、mitigation、currency 与 respawn 需分别归因，避免把所有收益算给英雄或 relic。
- **高密度 build 必须有 rate limit**：corpse cap、Vulture consumption、状态 stack、proc recursion、live entity 和 visual icon 都需要独立 guard；guard 触发必须可见且不能改变近战结果。

## 不兼容与未决问题

- 本项目普通人口目标为 10、物理候选格上限为 18，并有独立战术命令；不能复制 Skull Horde 的六单位类型线、连续随 Skull 移动、Peril 实时时钟或缺乏逐单位控制。
- 未核实标准 roster 的当前全部价格、rarity pool 概率、Oath 阈值、respawn 公式、live-body ceiling 或 corpse eviction 顺序。
- 未核实 Adam reanimated、Necromancer peons、Druid plants、Rat Nest rats 对 stats/items/tags/procs/Oath 的完整继承，也未核实它们是否占相同人口预算。
- 未核实 Vulture、Adam 和 corpse cleanup 在同一尸体上的确定优先级；官方只证明存在 consumption-rate cap 与并发尸体 guard。
- The Many 攻略跨过 Evasion 重做；旧 Dodge 百分比和社区 `.7s`、`100+ peons`、`无限资源` 都不是当前保证。
- Blood Feast 与 Sharpshooter 是模块证据，不提供采用率、胜率或唯一最优路线。
- 当前正式分支停在明确 `v1.032`；experimental 未读取为 main，之后若正式合并需重做版本审计。

## Disposition

`retained`。

保留理由：34 个实质来源跨官方规则 / 补丁、当前 Guide、实战讨论、长评与独立评测；The Many 死亡经济、Garg 链锁质量流、Adam 敌尸前排三套 build 均闭合 engine、state/resource、payoff、survival、spatial condition、owner、economy、pivot 与 counter。更重要的是，它提供了真实可验证的 roster / summon / corpse / reanimation / respawn / life / Rally 分权，而非仅有亡灵题材。

停止理由：继续搜索已不再改变三套结构；最大缺口是官方召唤继承表、精确人口 / 尸体 / respawn 公式和当前统计，现有社区说法不能补齐。若后续出现正式分支更新、规则数据库、带正文的当前 build Guide、完整召唤继承说明或可读视频转录，再重开版本审计。
