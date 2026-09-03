# ShapeHero Factory

## 身份、时期与资料密度

- `title_id`: `shapehero-factory`
- 精确对象：Steam App `2389040`，开发 / 发行均为 Asobism.Co.,Ltd。2024-11-06 进入 Early Access，2025-09-17 发布正式版 `v1.0.0`；访问日最后一个明确官方版本为 2026-07-30 的 `v1.1.4`。
- `v1.0.0` 不兼容此前进行中的 run save，并全面调整 Hero 参数、Burn、knockback、Research、Relic、事件、地图与高难度。所有 EA 攻略只能说明生产 / owner / 空间结构，不能直接证明当前强度。
- 2026-01-29 Major Update Vol. 3 后次日补丁为 `v1.1.1`，因此该大更属于 1.1 线；它加入 Box Select、Dullahan 与 Minions' Dan Dojo。访问日 1.1.4 仍在修 Ink flow、Ink Coating 的 battle-deploy-speed 显示与 Dojo 计分。
- 本 checkpoint：29 个实质非商店来源——9 个官方版本 / 系统重做节点，1 篇 PC Gamer 实玩介绍，3 篇可读 Steam Guide，8 个规则 / build / economy 讨论与 8 条 1.0–1.1 长评。
- 置信：中高。工厂→部署频率、两套 1.0 后构筑、生产空间、资源 / recipe / research / route economy、Statue 合成 owner、Ascension counter 和主要生命周期均闭合；但没有维护 wiki、当前公式数据库、统计或完整可读视频转录，故为 `retained`，非 anchor。

## Adaptive-depth 决定

这款游戏与本项目的相关性不来自“都自动战斗”，而来自一条不同的 build grammar：玩家不直接招募固定数量的单位，而是用 Motif / Ink、Canvas、Conveyor、Divider、Pipe 和 Portal 构建单位生产函数；某 Hero 在生产期被制作 / 送入 Portal 的数量，决定战斗期的部署间隔与能力阈值。空间不是战场站位，而是生产线 footprint、交叉、输入口、吞吐与重构损失。

研究覆盖 120/120 官方新闻标题及关键版本正文、287/287 Steam 讨论主题标题并打开 20 个机制相关主题、6/6 Guides、1,012/1,012 多语言评测、外部英文 / 中 / 日检索与 PC Gamer 原文。YouTube 搜索页在当前网络不返回可解析候选，Codex 浏览器入口也不可用；没有把视频标题、缩略图或 Steam screenshot 当证据。29 个来源后，新增材料继续重复低阶量产、高阶转型、Ascension 9 RNG 与空间 QoL，已不改变两套构筑的 owner / pivot / counter；按 adaptive-depth 停止，不枚举全 Hero / recipe / relic。

## 来源包

- `src-shf-official-1-0-0`：正式版内容、版本断点、Burn / knockback、Research / Relic、报告与性能重做总表。
- `src-shf-official-statues-tablets`、`src-shf-official-shop`：高阶生产转型与 Carat / RNG 缓冲的设计动机。
- `src-shf-official-major-update-3`、`src-shf-official-1-1-4`：当前 1.1 线的复制粘贴、Dullahan、Dojo、Ink flow 与部署速度显示边界。
- `src-shf-official-naga-0-10-2`、`src-shf-official-ink-overhaul-0-9-0`、`src-shf-official-warrior-rebalance-0-8-7`、`src-shf-official-mythic-rebalance-0-8-10`：EA→1.0 前的 Boss、Ink、低阶 / Mythic 强度生命周期，只作历史原因证据。
- `src-shf-pcgamer-demo-2024-11-24`：可读 Demo 实玩，明确 produced count→deployment frequency、Shield wall、Mage / Archer 临时输出与 Champion 门槛。
- 三篇 Guides 分别提供 Hero 角色判断、4/s bridge bottleneck / footprint、Tier 1→2 与 Mage→Warlock / Unicorn→Pegasus / Rock Fall→Meteor 配方链。图片本身不被解读。
- 八个讨论闭合正式 1.0 后 Ascension 9 骑兵链与 2026 Bleed swarm，并补足低 / 高阶争议、Mage + Shield wall、Statue priority/overflow、吞吐计算、起始 Research 与 Recycle economy。
- 八条正式版长评补足当前工厂循环、部署阈值、Free Control / Focus Time、随机资源 / recipe / research、低阶偏置、Spellmaster 与 1.1 编辑 / 显示问题。

## 真实循环：生产函数先于战斗队伍

每个 run 在路线图节点之间交替：生产阶段用有限时间 / Mana 放置工厂，把随机地图上的 Motif、Ink 和中间 Hero / Spell 经 Canvas / Conveyor / Pipe 送进 Portal；战斗阶段这些 Hero 自动部署，守住中心 portal / crystal。生产与战斗提供 EXP，升级和路线节点给 Mana、资源速度、地图扩张、Hero recipe / upgrade、Research、Relic、Carat、Potion 或事件选择；Boss 后继续下一段，正式版有 EX Stage、Ascension 0–9 与 Endless。

这里的“单位数量”不是库存里有 500 个独立棋子：生产 / export 的数量会缩短该 Hero 在战斗中的 deployment interval，并触发以累计产量为门槛的星级 / ability。生产 1 个、100 个与 500 个同类，代表同一 deploy owner 的 cadence / ability state。Ink Coating 又可让一个产物在能力门槛计数中代表更多点，但访问日 1.1.4 仍修其 UI 对 battle deploy speed 的错误显示，计数倍率与出兵速度必须分开报告。

## 构筑一：Ascension 9 Humans 骑兵进化链

正式版 1.0 后 Ascension 9 讨论提供完整路线：`Pikemen → Horses → Light Cavalry → Knight`，可加入 `Shieldbearers → Heavy Infantry`，Archer 只在资源 / Research 顺路时补。作者第一次失败、第三次完成；Pikemen 负责前期性价比与较少浪费，Knight 数量上线后把敌人压在地图边缘并清到最终 Boss，进入 Endless 后才遇到陡增压力。另一位完成者把同类结构概括为 `Mages → Shields → Horses`：Horse 负责 Boss，Mage 处理其他目标。

- **engine**：按路线拿到连续 Hero recipes，用多个 Canvas / Divider / Conveyor 保留前级产出同时供给后级合成；产量转为各 Hero 的 deploy cadence / ability threshold。
- **state/resource**：Pikemen / Horse / Light Cavalry / Knight 产量、Portal export、Mana、Motif / Ink、Canvas throughput、recipe 顺序、Research、factory cells 与重构时间。
- **payoff**：Knight / cavalry 拥有主要 Boss damage、接敌与压边结果；Pikemen 是低成本早期 owner。
- **survival**：Shieldbearer / Heavy Infantry 提供防线；Knight 自身存场与压制让远程 / 后续出兵少受 portal 压力。
- **spatial condition**：关键空间在工厂：多级 recipe 需要为前级分流、后级输入和 Portal overflow 留 lane；战斗单位自动部署，没有手动 formation 证据。
- **payoff owner**：每条 Hero 生产线拥有自己的 produced-count / deploy cadence；高阶 Canvas 不继承低阶升级，Statue replica 只能参与 synthesis。Knight 拥有终局战斗收益，而不是整座工厂无差别加伤。
- **economy / pivot**：先用 Pikemen / basic line 过第一图；Horse / Light Cavalry recipe 不齐时继续保留低阶产能，不能拆掉唯一生存线赌博。拿到 Statue 时可优先把 replica 送入后阶 combiner，让正常前级 overflow 继续进 Portal。
- **counter / limit**：Ascension 9 的 Naga、早期 Boss、随机 Motif 大小 / 颜色、缺 blueprint / Research 与 footprint 会在链完成前结束 run；1.0 后敌人受反复 knockback 会逐渐抗性直至免疫，不能把永久推边当无限控制。

这是“纵向进化链 + 横向保底线”。高阶不是纯替换：拆低阶线会同时失去早期出兵、门槛累计、合成输入与生存，转型必须显式处理 bridge / overflow。

## 构筑二：2026 Minion Master Bleed swarm

访问日最近的完整实战建议来自 2026-04 的 Ascension 9 求助：以 Thief 或 Ninja 施加 Bleed，配大量 basic Units 或 Horses。作者称 Bleed 会让其他单位攻击获得显著额外伤害并绕过敌方 Shield；basic Units 在第一图充当 buffer，避免过早把全部 footprint / recipe 押给单一路线。该帖明确说资源和 layout 应改变策略，没有 foolproof build。

- **engine**：低复杂度高吞吐 basic / Horse line 保持出兵频率，Thief / Ninja line提供 Bleed 状态；大量后续攻击读取该状态。
- **state/resource**：各 line 的产量 / deploy interval、Bleed 状态、敌方 Shield、可用 Cube / Motif、Mana、factory footprint 与 route recipe。
- **payoff**：Thief / Ninja 是 status supplier；命中带 Bleed 目标的 swarm 拥有后续 damage payoff。当前没有公式证明由谁结算每次附加伤害，报告必须保留 supplier 与 hitter 双 lineage。
- **survival**：basic Units / Horses 形成数量 buffer，为 Bleed owner 与后续攻击争取时间；不是由 Bleed 自身承伤。
- **spatial condition**：便宜、短 recipe line 在第一图占较少空间并更快上线；自动部署后没有可控站位。
- **payoff owner**：Bleed source、状态、每次读取它的攻击者与最终 damage event分开；生产 line 拥有 cadence，而不是把状态收益算给 factory 全局。
- **economy / pivot**：先保留多条基础 line；拿到适配的 Thief / Ninja recipe 与 Motif 后才扩 Bleed，缺资源时可保持 Horse / basic throughput，而非重开唯一通路。
- **counter / limit**：资源 / layout / blueprint 不支持时该线无法启动；敌方 burst 可在 swarm 建立前穿 portal，状态免疫 / cleanse 的当前完整规则未知。Bleed 绕 Shield 只来自一条 2026 实战讨论，没有官方数值或统计，保留为中置信。

它说明状态体系不等于单位标签：真正闭环是 supplier→state→多次 reader→damage owner；防线只负责让读取次数发生。

## 历史 Mage + Shield wall 模块

Demo 实玩与 EA 讨论反复出现 `Mage + Shieldbearer`：Shieldbearer 大量生产后形成更完整的 ring，Mage 短暂出现并放范围雷电；EA 玩家称 Mage 单独脆弱、需要 shield wall。正式版 Ascension 9 玩家仍使用 `Mages → Shields → Horses`，但 1.0 已全面重平衡 Hero，故这里只保留结构，不宣称当前 Mage / Shieldbearer 强度。

- Shield line 通过生产量提高部署连续性，购买的是 frontline uptime。
- Mage / Horse 分别拥有清群与 Boss payoff，Shield 不自动转化为法强 / 攻击。
- 若本项目设计“全队额外生命 / 防御的 50% 转给射手 / 法师”，必须由 Equipment / Relic 显式拥有 converter，并声明 read scope、recipient、slot cost、refresh、cap 与 counter；原作不提供该转换证据。

## Statue / Tablet、Recycle 与生产 owner

- `v1.0` 的 Hero Statue / Spell Tablet在选择 Upgrade reward 时一并获得，放置后持续生成 replica；replica 只能用于 synthesis，不能进入战斗，速度读取对应基础 Hero 的 production speed，且移除需 Pickaxe。官方目的正是让玩家升级高阶 Hero 时不必牺牲低阶能力或拆原生产线。
- 玩家实践把 Statue output 接在 priority combiner 前：Statue replica 优先进入后阶，普通前级 unit overflow 送 Portal。这样同一 Hero 的“合成输入 owner”和“战斗 deployment owner”不会互相吞噬。
- Statue 早期可帮助 A-rank，但随着普通 line research 加速会变得低效；Shop 出现时间与 Pickaxe 供给又限制重放。它是转型桥，不是免费产能。
- Recycle Tech 可把不用的地图资源送入回收链，以 XP / economy 与约 10–16% motif reclaim 换 footprint / equipment；讨论认为其强在早期升级 / 清理死资源，但也与 Ink Engine、Minion Environment 的生产加速争 Research / space。数值来自玩家时期实践，不写成当前固定公式。

## 路线、Research、Shop 与随机经济

- 正式版每次 Hero / upgrade 常在多个候选中选择，Research 候选更少，可花资源 reroll。`v1.0` 让 Research Tree 可用 20 Keen reload、升级给 1 Keen，Relic 从两个候选选一个；`v1.0.10` 又给 Shop 加 20 Carat reroll。
- Shop 是 Incursion Route 事件，用 Carat 买 item；官方明确它为“Carat 用途不足”和“纯 RNG 无缓冲”而设计。路线因此同时决定 Boss / Named 风险、recipe、资源、Research、Shop 与高阶链完成时间。
- 1.0 后玩家仍报告拿到地图没有对应 Motif / Ink 的 Hero / Spell、拿到无前置 recipe 的升级、或拿到与 Research 不相容的奖励。界面会以小字警告不可生产，但警告不等于可 pivot。
- Arcane Knowledge 是跨 run 解锁；有 consumable 能在开局无限 reroll 特定 Research。社区随后把 `Underground Utilization + Inserters` 视为稳定起手，说明“反 RNG 元成长”也可能收敛成固定开局。

## 空间、吞吐与时间契约

- Guide 的基础布局明确一个 bridge / tunnel 可形成 4/s 内部瓶颈，而输入 / 输出为 2/s；八 Canvas 需要 4/s motif，错误交叉会让末端 Canvas 饥饿。图片不作证据，但文字足以说明局部设备速度、总需求和 lane 交叉必须可诊断。
- Schematics Guide把 Mage→Warlock、Unicorn→Pegasus、Rock Fall→Meteor分成需要平衡上下游 cadence 的链；同一模板可选择更大、更高效或更紧凑、受 bridge 限速的版本。空间与吞吐是可交换预算。
- build phase 的时间、Mana 与 Free Control / Focus Time存在规则冲突。1.1 长评称 Focus Time 中出货不计数、Mana不增长，玩家可先断 belt 积货再恢复；Free Control 又允许暂停时无代价建造。这里不把该技巧写成设计意图，而是时间权威不一致的负案例。
- 破坏带有在途单位 / motif 的设备会损失内容，低成本研究又不退款；copy/paste 直到 1.1 才加入。重构成本同时是空间决策与不可逆经济，不是单纯 QoL。

## 敌人、反制与失败解释

- Ascension 9叠加更多普通敌人、更多 Boss 类型、敌方 HP / speed、强化 mini-boss、Boss 战附带 mini-boss 与强敌提前出现。玩家报告失败常呈“第一 Boss 碾压，否则一路到 EX”的二元曲线，不能从论坛估计胜率。
- EA Naga 曾除特定 Hero 外显著更难，`v0.10.2` 因此削弱；正式 1.0 又重排 Ascension / Boss。Boss 预览必须告诉玩家是需要单体追踪、AoE、Shield bypass、持续 frontline 还是控制，而不是只显示更高战力。
- 1.0 把 knockback 改成敌人每次被推后逐渐获得抗性，最终免疫；这是一条清晰的 anti-lock rate limit。应显示当前 resist stage，而不是让 Knight / cavalry 线突然失效。
- battle report 在 1.0 才加入 Minion Master direct attack；同时修复攻击范围、偶发 miss、Spell/Relic 影响范围文案。Factory report 还必须连接 produced→exported→threshold credit→deployment interval→battle actions，才能定位失败在生产还是战斗。

## EA→1.1 生命周期

本 checkpoint 计十三个 materially distinct negative / reworked families：

1. **低阶 Hero 能力失衡**：0.8.7 重做 D/C Warrior、削弱 Human、加强 Archer 与生产速度。
2. **Mythic 整体强于 Warrior / 异常技能**：0.8.10 以 nerf 为主重做 D/C Mythic、修错误过强 Frost Laser、下调 Burn 来源。
3. **Naga 特定解锁死**：0.10.2 因除特定 Hero 外显著过强而削弱。
4. **高阶合成破坏低阶线**：1.0 加 Statue / Tablet replica，保留低阶 deployment 与 ability。
5. **Carat 无用途 / RNG 无缓冲**：1.0 加 route Shop，1.0.10 加 20 Carat reroll。
6. **弱 / 难用 Research 与 Relic 池**：1.0 重做 11 tree、移除 1；新增 29 Relic、移除 3、改为二选一。
7. **无限 knockback lock**：1.0 改成重复击退逐步抗性至免疫，同时扩大初始可击退对象。
8. **Burn 语义薄弱 / 不一致**：1.0 改为可叠层、按层数 DOT；旧 Dragon / Meteor 强度不延续。
9. **Factory throughput 随速度 / Ink 状态失真**：0.9.3、1.0.4、1.1.4 连续修倍速产量、输入输出延迟、Mixer / Albedo flow 与 deploy-speed UI。
10. **高产量导致性能与过量部署**：1.0 优化 battle / ink，并在战斗已压倒性时限制部分 Hero / Spell 过度生成；这是表现 rate limit，不等于削产量公式。
11. **重构缺少批量编辑**：1.0 先做 bulk Conveyor replace，1.1 再加 Box Select copy/paste；move / rotate / undo 仍被当前长评报告为缺口。
12. **战斗归因缺失**：1.0 加 direct attack damage，修范围 / miss、Spell/Relic 文案；1.1.4 又修 Ink Coating 的 deploy-speed 显示。
13. **Focus / Free Control 时间权威不一致**：Focus 中出货 / Mana / timer 与 Free Control pause 形成可囤货、频繁暂停或完全规避压力的分叉；当前版本长评仍将其视为规则冲突。

一般画面、音乐、成就与单次菜单问题不拆成机制案例。Cumulative explicit negative/reworked cases：180。

## 对本项目可迁移

- **生成率可以是 roster 数量的替代维度**：同一 Hero 的生产量改变 deployment cadence / ability threshold；若迁移，必须避免把“500 个单位”误读为 500 个同时占人口的实体。
- **空间体系不限于战场**：合成格、背包、工厂线和装备槽都可作为 build space；但必须明确空间 owner、吞吐、合法连接、重构损失和战斗映射。
- **纵向升级需要 bridge**：高阶配方若吞掉低阶 owner，会同时破坏 survival 与 cadence。Statue replica / overflow 展示了“合成专用副本 + 原线继续战斗”的桥接方案。
- **盾体系通过 uptime 给独立 carry 输出**：Shieldbearer / basic swarm可以买 Mage / Horse / Bleed readers的攻击次数；Defense 不自动成为 damage。直接转换需要显式 relic/equipment owner。
- **状态收益要保留双 owner**：Bleed supplier 与读取它的攻击者不能都被笼统记为“状态队伤害”；report 要能分摊 source / reader / final event。
- **反 RNG 工具也会塑造 meta**：Shop、reroll、route preview、Arcane consumable 与开局无限 reroll都必须有机会成本，否则随机系统会收敛为重开 / 固定 Research。
- **控制需要递减抗性**：反复 knockback逐级免疫比二元免疫更可读，但必须显示层级、衰减和 Boss 例外。

## 不兼容与未决问题

- 本项目不是工厂建造游戏，不应复制实时 conveyor / pipe 编辑、数百次生产计数或两小时 factory run。可迁移的是“生成率 / bridge / space / owner”抽象，不是操作负担。
- ShapeHero Factory没有可核实的手动 battlefield formation；生产空间不能冒充本项目站位系统。
- 1.0 前 Guide、PC Gamer Demo 和 EA 讨论不支持当前英雄强度、精确 cadence 或 tier list；只保留仍被 1.0 后实践交叉的结构。
- Bleed 绕 Shield、附加伤害读取方式来自单条 2026 玩家建议；没有官方公式、stack / cleanse / immunity 文档。
- 1.1.4 只明确修复四项；没有公开 1.1.0 完整数值表、维护数据库或统计。Dullahan recipe 来自 Surprise Event，但没有可读实践足以闭合第三套 build。
- 讨论和评论中的 Ascension 9通过次数、25 次尝试、玩家自算概率都不是胜率统计，不进入机制公式。
- 未核实每个 Hero produced-count 到 deployment interval 的精确函数、战场同时存在上限、Ink Coating 对各阈值的当前倍率或 suppress-excess-spawn 的具体触发。

## Disposition

`retained`。

保留理由：29 个实质来源跨正式版本 / 重做、当前长评、文字 Guide、实战讨论与独立介绍；两套 1.0 后构筑均闭合 engine、state、payoff、survival、space、owner、economy、pivot / counter，且 Statue bridge、production cadence 与 knockback resistance 为本项目提供独特的可迁移结构。

停止理由：关键新增材料已连续重复低阶量产、高阶 recipe/RNG、空间重构和 Ascension 9 cliff；没有维护规则库、统计或可读视频转录，继续搜索无法把 Dullahan / Spellmaster / 全 Hero tier补成第三套 version-safe build。若后续出现 v1.2、大型 balance note、官方字段手册、维护数据库或带正文的 current build guide，再重开版本审计。
