# Tiny Auto Knights

## 身份与证据边界

- `title_id`: `tiny-auto-knights`
- 子类型：九宫格小队、商店阶段永久成长、异步快照 PvP 自动战斗。
- 状态：2025 年 Playtest/Demo；2025-11 正式版；2026-03 的 3.9.6 是本次能见的最后实质战斗补丁，3.9.7 只含控制器与排行榜性能修复。
- 核心版本边界：2025-03 Playtest、2025-05 至 10 月 Demo、1.0 launch、1.1–1.2 与 Fire/3.9.6 分开处理。
- 置信：中。官方更新对护甲、商店、触发链、召唤和模式演变很具体；但没有完整当前英雄/道具数据库、概率表、统计站或正式版长篇构筑攻略，实战结构主要来自版本化 Steam 单帖。
- 本档不把商店页、视频标题或搜索摘要当作深证据，也不把历史 999 属性、2500 护甲或个人“保证十胜”描述写成 current meta。

## 实质来源

本 checkpoint 登记 24 个非商店页：

- 1 个 `official-dev`：`src-tak-official-demo-meta-2025-05-22`。
- 9 个 `official-patch`：`src-tak-official-playtest-2025-03-05`、`src-tak-official-demo-july-2025-07-23`、`src-tak-official-demo-01414`、`src-tak-official-110`、`src-tak-official-120`、`src-tak-official-121`、`src-tak-official-fire-2025-12-18`、`src-tak-official-friends-2026-02-09`、`src-tak-official-396`。
- 14 个 `community-analysis`：`src-tak-thread-async-pvp`、`src-tak-thread-thief-armor`、`src-tak-thread-drummer-loop`、`src-tak-thread-self-damage`、`src-tak-thread-buff-persistence`、`src-tak-thread-maiden-cleanse`、`src-tak-thread-build-variance`、`src-tak-thread-row-column`、`src-tak-thread-necromancer-space`、`src-tak-thread-overtime-poison`、`src-tak-thread-legendary-availability`、`src-tak-thread-cyclops-repetition`、`src-tak-thread-monk-bless`、`src-tak-thread-archer-global`。

官方页面建立规则与版本结果；讨论建立具体摆位、投资路径和玩家遇到的失败。开发者在讨论中的回复可支持局部规则，但帖子整体仍按社区来源处理。没有把 14 个帖子当作 14 份独立统计样本。

## 真实循环：九格、商店永久成长与快照对手

玩家在商店阶段购买、出售、合并升级、给单位施加物品效果并调整九宫格与 bench。0.14.14 明确了不同稀有度售卖值、出售触发 `Coins gained`、XP 奖励、免费 reroll、bench 与召唤落点修复；1.1.0 又明确获得 0 Coin 不触发 `Coins gained`。因此经济触发必须依据实际获得量，而不是只看“发生了出售”。

状态持续期是构筑决策的一部分：开发者说明商店阶段 buff 默认永久，战斗中获得的 buff 默认只持续当场，具体文案可另设例外。永久自伤、永久攻击、合并等级、稀有度与战斗临时 Armor 不能混成一个总强度数字。

常规 Casual/Ranked 使用上传的玩家队伍快照：按回合、胜负等维度取对手，Ranked 另含 MMR。2026 年新增的好友 lobby 是实时多人实验模式，最长存活者获胜；它不是原异步池的规则更新。本项目可参考“已保存敌方构筑成为能力试卷”，但不能把 PvP 人口、MMR 或重复快照当成单人塔层内容来源。

## 空间语法：3×3 不是九个无关槽位

来源至少建立了 `row`、`column`、`ahead`、`behind`、`adjacent` 与 `front row` 六种选择器。Adventurer 在回合开始给同一 row 的盟友生命，Harpy 读取 column；Archer 的伤害作用于全场所有英雄，并非随机目标。日语本地化曾把 row 与 column 都翻成同一“列”语义，证明空间词若不配格子高亮，会直接破坏购买和摆位预测。

召唤也消耗真实格位。Necromancer 召的是 Skeleton，不是复活原盟友，而且需要空格；Playtest 中 Skeleton death 还能触发新的 Skeleton，形成无限链，后被修复。召唤位置、空格不足、生成失败和 root owner 必须进入战报，不能只显示“未触发”。

## 具名历史构筑一：Jack → Thief 经济 → Drummer 前排放大

时期：2025-08 Demo、0.14.14 与 1.1.0 之前的单个高 MMR 社区样本（`src-tak-thread-thief-armor`）。

- **driver/engine**：Jack 先在 Thief 上叠经济/成长，随后把主要投资转到 Drummer。
- **state/resource**：实际获得的 Coins、永久属性、前排属性与旧版 Armor。
- **payoff**：Drummer/前排把经济期积累转成接近 999/999 的可见战斗属性。
- **survival**：历史 Armor 每次受击只减 1，帖子报告一度超过 2500，使相似阵容陷入极长镜像战。
- **spatial condition**：主要承压和属性显示集中在 front row；后续单位围绕前排收益摆位。
- **payoff owner**：Thief 拥有早期经济触发，Drummer/前排拥有最终战斗属性；Jack 是成长发动者，不等于最终伤害所有者。
- **pivot/counter**：先以 Thief 过渡，完成经济职责后转投 Drummer；对手需绕过前排、使用更大单次伤害或在护甲改版后提高实际伤害，而不是继续堆小额 hit。
- **version context**：只保留构筑生命周期。0.14.14 将 Armor 改为按 incoming damage 的一半削减并把属性/Armor 限到 999；1.1.0 又给 Thief 三次 Charges。旧数值不可外推。

这套样本说明防御体系并不自动缺少伤害：经济/全队成长可以喂给明确的前排或射手 payoff owner。但如果同一永久经济引擎同时无限制造攻击、生命和近乎不衰减的护甲，就会压缩替换与反制空间。

## 具名历史构筑二：双 Drummer—Jester 循环阵

时期：2025-10 Demo 末期、0.14.14 附近的单帖（`src-tak-thread-drummer-loop`）。

- **driver/engine**：两个 Drummer 分别放在 top-middle 与 bottom-middle，Jester 位于其后，形成反复读取/反馈的属性增长链。
- **state/resource**：链内 `X gained` 事件与不断上升的 Attack/Health。
- **payoff**：Jester 与前排达到约 300–500+ 属性，循环把支持单位和承压单位都变成结算者。
- **survival**：前排承受第一轮伤害并获得循环增益；帖子称其疑似 Guardians，但报告者不确定，本档不固定英雄身份或等级。
- **spatial condition**：两名 Drummer 的上下中位与身后 Jester 是可核验结构；前排身份不是。
- **payoff owner**：每次增益的原始来源、被增益者和后续触发者必须分别保留；不能把整条链归给最后显示高属性的单位。
- **pivot/counter**：打断其中一个中位节点、绕后击杀 Jester、限制单次增益或缩短战斗都能攻击不同环节。
- **version context**：0.14.14 为 Drummer 增加单次触发最多 100，并规定同一 hero/同一 effect chain 的 `X gained` 只触发一次；1.1.0 将 Jester 政策性移到 Legendary。它是被守卫约束的历史循环，不是正式版推荐阵。

该构筑仅中低置信：站位和数值现象可读，但前排身份、Jester 等级与当时是否属于 bug 都不确定。

## 具名构筑三：Tier-3 Warrior 商店自伤转永久攻击

时期：1.0 launch 附近、1.2.0 伤害物品池收缩前后（`src-tak-thread-self-damage`）。

- **driver/engine**：商店阶段把 `1×4` 伤害物品施加在 Tier-3 Warrior 前方的友军。
- **state/resource**：目标永久损失 4 Health；Warrior 的 Hurt/Death 相关能力把该事件转成永久 Attack。
- **payoff**：社区样本给出一次操作永久获得 20 Attack；数值只属于该时期样本。
- **survival**：前方单位承担真实、永久生命成本，必须仍能活过开场；不能把自伤当成免费触发按钮。
- **spatial condition**：受伤者必须在 Warrior 前方，`ahead` 选择器决定成本落到谁身上。
- **payoff owner**：受伤友军拥有生命损失，Warrior 拥有永久攻击收益，商店物品拥有触发来源。
- **pivot/counter**：只有当剩余生命、前排替换或治疗计划可承担成本时才投资；否则保留伤害物品或换成不牺牲主坦的摆位。
- **version context**：1.2.0 将商店伤害物品池收缩为 common 单次与 legendary 两次；1.2.1 证明删除旧物品 id 若无迁移会让旧 run 启动崩溃。

这是清晰的“付出永久防御换单一伤害核心”结构。它比无条件把全队生命等额变攻击更健康，因为成本、收益主体、摆位与停止投资条件都可见。

## 其他机制闭环

### Cleanse、Armor 与替代路线

1.2.0 对 Monk 的 `Cleanse → Armor` 公式做过削弱，社区也建议用 Lv3 Monk、Bless/Holy 和围绕支持来构筑。另一帖提到疑似 Maiden 按自身 Cleanse 给邻接单位生命，并购买 Cleanse buff；玩家对英雄名使用问号，因此只保留为低置信邻接结构，不计一套完整具名构筑。Poison、Thorns 与 Cyclops fling 是社区给出的替代线，不代表当期全局最优。

这些例子共同证明：Cleanse 可以是状态防御，也可以被专门单位读作 Armor 或邻接 Health；Armor/Health 仍需另一个明确 payoff owner 才能转成伤害。盾体系与元素/状态体系因此是可交叉的不同轴。

### Burn 是战场状态，不是英雄身上的普通 DOT

Fire Update 把 Burn 放在 battlefield 上，并伤害所有 touching heroes。3.9.6 又让 Burn 每次 activation 后减少 50%。因此 tile/status owner、接触单位、激活次数和衰减后的余量必须分开。九宫格空间不只承载单位，也可能承载持续危险区域。

### 终局 Poison 是防僵局预算

Demo 社区认为第五回合开始的终局 Poison 太早；开发者将它后移到第九回合，并说明其目的是阻止 healing 无限僵局，后续又加快增长。应把“何时开始”“每轮增幅”“伤害来源”和“为何触发”显示出来，不能用突然放大的隐藏伤害结束战斗。

## 招募、升级、替换与早期随机性

- 重复英雄合并升级，rarity 决定取得窗口；普通 Rabbit/Warrior 的建议是后期被更高 rarity 功能体替换，而不是永久占位。
- 0.14.14 的 sell values、XP 奖励、free rerolls 和 bench 构成保留对子、卖出触发与转型机会成本；0 Coin 不触发 `Coins gained` 防止空事件自循环。
- 2025-07 的 round-3 health kit、weekly decks 和 Ranked 对手预览属于早期随机/信息缓冲；不能据此发明固定概率。
- 社区指出 Legendary 核心在前半程拿不到，六单位 deck 池会令转型依赖 RNG；开发者明确主张根据拿到的内容调整。因此 Legendary payoff 需要早期可玩的桥，而不是让玩家空等终局组件。
- 开发者在 Demo 观察到新手常玩 summon，高排位样本常把资源集中到一两个超强化核心。这是定性观察，不是胜率统计；它支持“队伍引擎 → 少数 payoff owner”的可读结构，不支持具体数值照搬。

## 版本、失败与重做

1. **Necromancer 无限 Skeleton**：早期 Skeleton death 可再次召 Skeleton；官方 Playtest 更新修复，后来社区又发现空格满足却不触发的独立 bug。
2. **Drummer/Jester `X gained` 循环**：属性反馈链出现 300–500+ 样本；0.14.14 加同链单次触发与 Drummer 100 上限，1.1.0 将 Jester 调整为 Legendary。
3. **Thief/旧 Armor 支配与镜像僵局**：单帖报告 999/999 前排与 2500+ Armor；护甲从“每 hit -1”改为按伤害削减并加 999 上限，Thief 后获三 Charges。
4. **终局 Poison 过早**：第五回合起的防僵局机制被玩家认为压缩构筑时间，开发者改到第九回合。
5. **删除伤害物品导致旧存档崩溃**：1.2.1 修复 1.2.0 删除物品后旧 run 无法启动，证明内容删除必须有稳定 id 迁移。
6. **row/column 本地化失去空间语义**：日语将两者混同，开发者转交修正；格子高亮比单一文本更可靠。
7. **相同 Cyclops/fling 快照重复**：单个玩家在奖励回合连续五次遇到相同摆位/组合；只证明异步池可能暴露重复感，不证明人口、匹配概率或流失因果。
8. **死亡目标与召唤触发边界**：3.9.6 修复 Nun 选择已死亡目标，Necromancer 空格/触发也曾失效；目标合法性需在结算时重验证。

本 checkpoint 将以上八项计为 8 个新的 negative/reworked cases，累计 82。30+ 英雄重做、Minotaur 随机伤害重做、Zombie Poison spread 削弱与 deck mechanic filter 记录为版本证据，不再拆分凑数。

## 对本项目可迁移

### 可迁移原则

- **九宫格选择器必须可视化**：row、column、ahead、behind、adjacent、front row 与全场范围应在购买、摆位和战报使用同一语法。
- **永久成长要有明确成本与收益主体**：Warrior 自伤转攻说明永久 Health 成本、Attack 收益、空间关系和停止条件可以同时成立。
- **防御轴与伤害轴正交但可被稀有转换器连接**：Armor、Health、Shield、Cleanse 负责存活；遗物/装备/英雄读取其中一个明示状态，把它集中给一个射手或法术核心，而不是全队免费双向缩放。
- **循环守卫不能只有总上限**：同一 hero/同一 chain 一次、单次增益 cap、0 值不触发、召唤空格、死亡重验证与终局防僵局各解决不同问题。
- **商店永久、战斗临时必须首眼可辨**：同一个数值如果跨战保留，其价格、风险和替换成本完全不同。
- **敌方快照可成为能力试卷，但需去重和版本标签**：重复 Cyclops 样本说明对手池需要相似度控制与来源解释。
- **tile status 有独立 owner**：Burn 证明战场对象可以积累、触发、衰减并伤害接触者，不应伪装成某一英雄身上的 DOT。

### 不可直接迁移

- 不移植公开异步 PvP、MMR、玩家快照上传或“最长存活者获胜”的实时好友 lobby；本项目是单人塔层。
- 不把九格版的具体售卖价、免费 reroll、rarity 池和 weekly decks 当作已确认经济规则。
- 不复制历史 999 cap、2500 Armor、Warrior +20 Attack 或第九回合终局 Poison 数值。
- 不让永久 shop buff 绕过本项目装备附着、英雄替换和共享资源权威。
- 不把每个防御英雄都做成自动伤害核心；转换应是稀有、可归因、付出真实槽位/遗物/装备机会的内容。
- 本项目已确认两个独立战术指令和三点共享资源；Tiny Auto Knights 未见同类战中主动层，不改变该契约。

## 未决问题

- 当前完整英雄、装备、rarity 概率、合并成本、bench 容量和正式版 sell table 未找到。
- 九宫格召唤选格、同时触发总顺序、同速规则和全部 row/column/adjacent 选择器没有官方完整规范。
- 三套具名构筑都依赖单帖历史样本；没有当前统计或跨作者攻略可比较胜率与采用率。
- Burn 的 tile 生命周期、叠加、移动接触与所有结算顺序仅有概要。
- 正式版异步池的去重、快照过期和相似度控制没有公开规则。

## Disposition

`retained`

Tiny Auto Knights 达到普通长尾门槛：九个官方补丁与一个开发者观察给出规则/生命周期，十四个可读讨论给出三套版本化构筑、具体九格选择器、经济/替换、召唤占位、终局防僵局和失败样本。它不升 anchor：实战主要依赖单个 Steam 历史帖子，部分英雄身份与强度不确定，缺当前统计、完整规则库、正式指南与全行动顺序。
