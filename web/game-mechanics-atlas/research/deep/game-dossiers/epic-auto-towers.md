# Epic Auto Towers

## 身份与证据边界
- `title_id`: `epic-auto-towers`
- 子类型：单人、回合制塔阵 autobattler＋deckbuilder/roguelike。
- 状态：2024-11-14 EA；资料跨度 Demo、0.60–0.79、0.9x 与 0.99.6 beta，不能混成同一强度表。
- 本 checkpoint：21 个非商店实质来源——11 official、2 完整文字攻略、4 社区分析、4 长评。
- 置信：中高。规则/生命周期官方链很强，三套具体阵容与 Boss/pivot 可回到全文；但实践资料集中 Steam、完整塔库/商店概率/结算顺序缺失，因此 `retained` 而非 anchor。

## 真实循环与空间语法
每回合从随机商店买塔、在有限槽位部署或把同类塔叠高，结束回合后自动迎击怪物。战毁塔下回合恢复，跨回合获得的永久增益保留。核心决策不是单纯凑标签，而是：占哪个槽、哪座塔先触发、临时增益由谁永久化、经济塔何时移除、是否用稀有牌污染当前路线、Boss 前把谁升到最低安全等级。

公开攻略显示早期约十个横向槽，Act 3 在 Royal Tower 后增加三个槽。邻接、左右、前方、全局和移除目标是不同选择器；所有 buff 效果先于 Honey/Insolent 等合并效果的历史观察尤其关键。塔叠高同时提高属性与独特技能，但不同塔是线性、倍增或保留百分比成长，不能只看 level。

## 构筑一：0.61c Flower Power
精确顺序：`Royal—Chest—Thief—Obsidian—Flower—空位(Honey)—Forge—Bee—Guardian—Guardian`。

- **engine**：Flower 先用自身属性的 20% buff 右侧 Honey，再吸收 Honey；下一回合把成长分享给左侧 Obsidian。
- **state/resource**：gold、reroll、Honey、Forge 费用、塔等级、临时/永久属性、removal/nullification token。
- **payoff**：Obsidian 保留临时 buff，Flower 经 Honey 每回合自我滚动；后期第二组 `Obsidian—Flower—Honey` 复制增长。
- **survival**：两 Guardian 是 Act 1 桥；后期 Bear 继承被移除塔的攻血并接替前线。
- **spatial condition**：Flower 必须邻接正确 Honey/Obsidian；空位用于每回合 Honey；历史触发顺序允许双 Flower 先 buff 中间 Honey 再合并。
- **owner**：Flower 拥有吸收成长，Obsidian 拥有保留，Bee/Chest/Thief/Forge 拥有供给，Bear 拥有被移除塔的继承。
- **pivot/counter**：优先 Honey 与 Bee，不追无关 rare；经济不足升 Chest/Thief，战力不足补 Flower/Obsidian。第三 Boss 全塔降一级并删除 level 1，Boss 前停止过度升高塔，先把所有低塔升到 2。之后移除 Guardian、Forge、Thief、Chest换 Bear/第二套引擎。
- **版本**：0.61c 历史强势结构；不能声称 0.99 仍同强度。

## 构筑二：0.60–0.61 Slime＋Insolent＋Bear
- 早期 `Chest＋Thief` 供经济，`Milk＋Cat` 成长，`Berserk＋Milk` 把 HP 转攻击；Iron level 3 的生命作为过渡饲料。
- 拿到 Bear 后移除 Iron/吃 Cow，把移除塔的攻血转给 Bear。Malicious Berserk 在场时 Jester 不耗 crowns。
- Debt Tower 在能活过短期时换取后期高额金；第二 Boss 后空出一格给 Insolent，再放 Slime 反复吞噬成长后的 Insolent。
- 第三 Boss 前所有塔至少 level 2。过 Boss 后继续升 Slime/Insolent/Bear；Chest/Thief 完成经济职责后退出。
- **所有权**：Milk/Iron/Cow 是状态供给，Berserk/Bear 是不同转换者，Slime 是吞噬者，Insolent 是可重复增长载体；不是一个“吃塔”标签自动全队收益。

## 构筑三：0.97–0.99 Inferno Mana / Mole
论坛提供两条可交叉的实战线：

- `Cerberus—Tower of Promises—Cerberus` 是第一 Boss 的前排桥；后排高等级 Well＋低等级 Well＋reroll tower 供 mana/刷新。Prism 每回合 infusion，但不要超过早期生存和 Well/Promises 升级优先级。
- 更稳定的 Mole 线以两 Well＋Chaos 开局，把 mana 先灌 Chaos；找到 Mole 后放左起第四格，最右 Root 与 Mole 左两格另一 Root，Root infusion 升 Mole，Drover 把 Mole 快速升到 2。Root 到 3 后边际下降，后续资源回到经济或前排。
- **counter/pivot**：十回合内没有 Prism 可重开或转 Mole/Succubus/Genie；Gargoyle 是中期坦克但可能迟到。早期防守塔会拖慢真正增长，引出“活过第一 Boss”与“第二幕仍能缩放”的双重约束。

## 经济、遗物与替换
Chest/Thief 是常见早期经济桥；Bee 影响 Honey/Flower 供给，Forge 消耗 1 gold 强化当回合材料；Debt 先承担风险后回报。移除 token 是后期构筑资源，因为已放塔并非随时免费撤回。

Flower 攻略的遗物选择展示规则改写层：Hourglass 增强临时 buff；Stairway to Heaven 解除等级上限；Stone of Simplicity/Refinement 改商店与 max level；Nullification 缩小牌池；Right of Ownership 增 Bear；Memory token 把临时增益保留权转给任意塔。遗物必须说明改的是 buff 倍率、等级、牌池、所有权还是槽位，不能只写“增强构筑”。

不同 pack 的 reroll 经济并不相同。长评描述 Royal/Necropolis 可持续刷新，Inferno 依赖特定塔产生 reroll，Tropical 的刷新可能提前终止但提高稀有度。它解释了为什么同一四塔 synergy 在不同 pack 有不同可达性，但属于 0.98 单人观察，没有官方完整概率表。

## Boss、波次与失败解释
- 第一 Boss 是早期 DPS/生存门槛；社区反复指出需要短时间闭合 2–4 塔 synergy，filler 又会占格和拖慢缩放。争议保留为社区诊断，不冒充胜率。
- 第三 Boss 全塔降一级并删除 level 1，形成明确的“宽升安全线”而非继续把单核叠高。
- 0.73 重做 Endless final Boss 攻血成长；0.99 社区仍报告塔达到数值上限而敌人继续成长。Insolent—Shark—Hook—Captain 通过移动重复喂养多个 Shark，可到 100+ 波，但最终仍受 cap/敌方曲线约束。
- 0.97 的 32–34 波没有怪，导致依赖 monster spawn 的 Honey/Slime 不触发；补 invisible monsters 又引发统计页崩溃，官方回滚。0.98 把这些塔在该区间的 start-of-battle 效果迁到 end-of-turn。这证明“无敌人波次”仍必须有明确触发语义，伪造目标不是安全修复。
- 商店中由塔生成、nullified、rarity-upgrade 的牌曾缺来源解释；0.98d 增加生成/禁用标记。论坛中 Pirate coin-toss reroll 与主塔升级奖励被混淆，进一步说明随机修改必须显示 source。

## 版本生命周期与负面案例
1. Royal/Inferno 上线后多塔成长不足，官方连续 buff，并计划彻底重做部分 Inferno 塔。
2. Beggar/Cursed/Janitor 的惩罚“不合理”，0.77 重做为较可承受成本。
3. Ascension 中难度不当或规则令人困惑，0.90–0.92 重新评估并增加高层奖励。
4. 32–34 空波次令怪物触发塔失效；invisible-monster 修复造成崩溃并回滚，随后改触发时点。
5. 玩家反复报告 profile progress 丢失，0.73 与 0.98 两次重做 save system。
6. Spirit 生成过少、Honey/Bee/Flower 超 100% 与 Moon/Honey 交互、Memory 冻结和 relic duplication 被官方修复。
7. 社区报告早期 Boss＋商店 RNG 把许多 pack 收敛到少数构筑；官方补丁证明持续调整，但无统计可确认总体程度。
8. Endless 中玩家塔数值封顶而 Boss 继续成长，形成硬终点；高波次样本支持现象但不建立精确 cap 规则。

本 checkpoint 将以上八项计为 materially distinct negative/reworked cases；数值 buff 不再逐塔重复计数。

## 对本项目可迁移
- 有限格应让 engine、供给、过渡、防守与经济争夺同一空间；移除/替换必须是可规划资源。
- 邻接与触发阶段要可预览；“先 buff、后合并”必须是确定规则而非攻略考古。
- 临时转永久、生命转攻击、被移除单位属性继承等转换必须指定稀有转换者和收益所有者。
- Boss 可考“全员最低等级”“无目标波次”“早期 DPS”“Endless cap”，但要提前给适应窗口与多种答案。
- 商店生成、rarity、nullification、自动购买与冻结需要来源标记和原子结算。

不可直接迁移：横向塔墙、Royal/crowns、精确塔名与 pack、每回合永久无限成长、无偿重开、100+ 波 Endless、同塔垂直叠高和现有随机刷新经济。

## 未决问题
- 完整当前塔/遗物/token/商店概率数据库与确定行动顺序。
- 0.99.6 beta 哪些改动已进入 0.99.8 main。
- 各 pack 真实完成率、构筑采用率和第一 Boss 失败分布。
- 最大属性、level cap、Annoyance 与最终 Boss 的精确规则。

## Disposition
`retained`

三套具体塔阵、经济替换、遗物所有权、Boss 反制和八个生命周期案例均有多源正文支撑。不上 anchor 的原因是实践证据仍集中 Steam、两份最完整攻略属于 0.61 历史版本，且缺当前完整规则库、统计和正式行动顺序。
