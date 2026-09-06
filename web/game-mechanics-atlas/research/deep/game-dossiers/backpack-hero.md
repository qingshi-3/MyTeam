# Backpack Hero

## 身份、范围与研究深度

- `title_id`: `backpack-hero`
- 类型：手动回合制 inventory-grid roguelike / deckbuilder-adjacent；不是自走棋，也不是自动战斗游戏。
- 发布与版本边界：Steam Early Access 规则、2023-11-14 的 1.0 发布、2024 官方更新以及 2026 社区观察分开记录。2024-12-20 `December Check-In!` 在当时明确是 PC testing-only change list；未决的是这些变化后来是否、何时、在哪个 branch / build / platform 交付。2026-06 两名不同平台持有者仍分别报告 `1.0.1187.0` 与 `1.0.1212` 的 Acorn Sap 为 10 Block，但不足以关闭该映射。
- 研究深度：`anchor-retained`。角色规则、空间语法、五类具名构筑、经济 / 转型、敌方考题、版本重做与社区失败链均有多来源支撑。它是“手动背包构筑与所有权”相邻锚点，不授予本项目任何战中手动点击或自动执行规则。
- 来源包：85 个实质非商店来源，覆盖 34 个官方公告 / 角色页 / 带开发者身份回复、12 篇完整 Steam Guide、8 个讨论 / 评论主题、9 个长评、2 段可读 ASR 视频、9 个固定 commit 官方 ModDocs schema / export 和 11 个 Wiki.gg API 页面。旧 Fandom 记录已由 Wiki.gg 与版本固定 Guide 完全替代。

## 来源路由

### 规则与版本

- 官方 1.0、enemy / relic / curse / Tote / Pochette / CR-8 / magic / Burn 更新与五个角色介绍：`src-bh-official-*`。
- 官方 ModDocs 固定 commit `69558c4826e8cff651a157d443c992ba53982e5b`：`src-bh-modd-*-677`。README 明示资料 WIP、incomplete and incorrect；只用于 2023-09 changeset 677 的历史数据模型，不 currentize。
- Wiki.gg 五个角色页与 Armor / Cursed Items / Accessories / Bestiary / Relics / Manastone 页面 `src-bh-wikigg-*`：用于交叉核对角色、物品族、Boss reward 与 Mana network 边界；维护页不等于官方 patch 或精确 current binary。

### 实践与失败

- Steam Guides：`src-bh-guide-*`，覆盖触发顺序、Purse 通用选择、Tote、CR-8、Curse、Disorder、Cleaver / Bow、Tote softlock，以及明确固定在 `v1.0.1187.0`、主要于 Endless Dream 开发 / 测试的 Tantatals 构筑包。
- 视频：`src-bh-video-tote-1161` 与 `src-bh-video-builds-live-2026`，分别明确 patch 1161 与 2026 live patch / testing-branch-excluded 边界。
- 讨论：`src-bh-discussion-*`，只证明相应玩家或开发者回答里的实践、失败或平台观察，不当采用率 / 共识。
- 长评：`src-bh-review-*`，用于历史 pivot lock、重复点击、reward specificity、UI / 保存状态等社区观察。评论之间的重复意见按 family 去重，不把票数或游玩时长当机制统计。

## 真实循环与玩家权限

探索、战斗、升级扩包、拾取 / 丢弃 / 出售、商店、锻造、事件与 Boss 奖励共同塑造一局。核心战斗不是“装备后自动解析”：玩家每回合手动选择物品、目标与使用顺序；Energy、Mana、Block、HP、Poison、Burn、Spikes、Charm、Sleep、curse / hazard 分属不同 ledger。

普通背包也不是一个笼统的“邻接系统”。资料至少区分：物品形状与精确 cells、相邻 / 对角、行 / 列、上下左右、connected network、empty-space path、Satchel pocket、Tote hand / backpack、CR-8 direction / rotation。2023 历史 schema 还把 trigger、area、value source、effect target、modifier 与 value changer 分开，说明“谁供应状态、谁读取、谁接收、在哪个空间谓词成立”是独立问题。

五名角色改变操作语法：

- Purse 是标准教学角色，围绕扩包、Energy 和普通物品布局。
- Satchel 每次升级取得 Tetris 形状；不连通格区形成 pockets，乐器施加 Charm 并允许非伤害胜利。官方开发史显示其经历 adaptive challenge、两代 sliding puzzle 等四次重做后才收敛到现行高层结构。这里的 Pocket 是角色空间分区，不等于通用的可装物 Pouch。
- Tote 使用 carving deck；每回合 draw，discard 进入弃牌堆，banish 只在本战移除，Toss 与 reorganize 有独立 Energy 成本，fixed items 不进 discard。
- CR-8 不按普通物品 Energy 逻辑行动；Core 产生 charge，按方向、旋转、线路和组件寿命自动激活路径物品。官方把它称为 auto-battler 型角色，但自动的是角色内部 execution graph，不是整个游戏变成无输入自动战斗。
- Pochette 的 Pets 是独立战斗者，有各自 pouches / gimmicks；Treat 是宠物专属资源，recall 与 resummon 需要另算能量和目标。

## 构筑一：Happy Buckler + Spiky Crown + Bashing Shield

这是明确的 Shield → damage 桥，而不是“护盾天然会输出”。

- **engine**：使用另一件 Shield 触发相邻 Spiky Crown；Crown 增加 Spikes 并对玩家造成 1 点 self-attack。该伤害先由 Block 吸收，Block 不足才扣 Health，但即使完全被 Block 吸收仍会触发 Happy Buckler 自己的 `when attacked` cost reduction，随后才能低费 / 0 费重复使用 Buckler。
- **state/resource**：Crown 拥有 Spikes 与 self-attack；Happy Buckler 拥有 Block 和自己的 Energy cost 变化。Block absorbed、可能溢出的 Health loss 与 Spikes 是三个独立结果，不能归到同一个 owner。
- **payoff**：Bashing Shield 显式读取当前 Block 造成伤害，并消费一部分 Block。伤害 owner 是 Bashing Shield，不是 Block ledger。
- **survival**：Happy Buckler / 其他 Armor 供应 Block；Crown self-attack 优先消费 Block，只有 Block 不足时才损失 Health；Bashing Shield 再消费 Block 换输出，是另一项防守代价。
- **spatial condition**：触发用 Shield 与 Crown 的 adjacency、Crown→self-damage→Buckler cost reader 的顺序、物品占格和可用顺序必须成立；不能简化成“队伍盾值”。
- **economy / pivot**：Happy Buckler + Crown 先闭合低费防守循环，再决定是否占空间加入 Bashing Shield。若转换器不到，保留为 Block / Spikes 防守；若 Poison 成为主要威胁，单加 Block 不能解决。
- **counter / failure**：Poison 绕过 Block；Crown self-attack 会先消耗 Block，并在不足时产生 Health 压力；Bashing Shield 自身仍需 Energy；任何一步 adjacency / owner / action order 失败都会断开对应环节。
- **version**：2026 live-patch 视频、Wiki.gg Armor / Cursed Items 与 `v1.0.1187.0` Tantatals Guide 交叉支持；后者多数构筑在 Endless Dream 开发 / 测试，视频则明确排除 testing branch。它们证明路线存在，不证明精确 binary 相同、掉率、胜率或唯一解。

对本项目的直接设计启示是：盾体系可以有转换装备 / 遗物，但必须保留 supplier、reader、damage owner 与消耗代价，不能默认所有冰盾、土盾或普通盾共享输出机制。

## 构筑二：Satchel disconnected pockets + Feather Shiv

- **engine**：保留初始 Feather Shiv；升级时制造多个断开的 pockets；用 Coral 复制 common Shiv。
- **state/resource**：pocket 数量、Shiv 所在 pocket、同 pocket / 不同 pocket 计数、Charm 与 Block 分开。
- **payoff**：先把 Shiv 分散到不同 pockets，利用 unique / another-pocket reader；有足够空间后才考虑同 pocket 叠加。
- **survival**：triangles / harps / drum 提供 Charm；gloves、helmets、Talon Boots 补 Block。它们是独立模块，不把 Charm、Block 和 Shiv damage 合成一个属性。
- **spatial condition**：断开连通区才形成 pocket；普通 adjacency、同一行或总占格数不能替代 pocket predicate。
- **payoff owner**：Shiv / 对应 pocket reader 拥有 damage；Coral 只拥有复制事务；装备分别拥有 Charm / Block 供应。
- **economy / pivot**：保留 common Shiv 是为了可复制与空间扩展；复制前先保证 pocket 结构，后续根据 poison encounter 把空间让给生存或先手输出。
- **counter / failure**：Poison 敌人优先处理；若 pocket 形状不闭合或复制品挤占防守空间，纸面数量不会变成有效输出。
- **version**：2025 讨论中的实际建议与官方 Satchel / 历史 area schema 互证；不是完整当前数值表。

这条路线证明“空间分区计数”可以成为输出 owner 的输入，但不能被泛化为邻接光环。

## 构筑三：Tote 薄 carving deck + 并行 Block / AOE 模块

### 薄 deck 稳定性

patch 1161 教学展示每回合抽 5 carvings；discard 进弃牌堆，banish 只在本战移除；reorganize 消耗 3 Energy，Toss 消耗 1 Energy；fixed items 不进入 discard。每增加一张 carving 都会稀释关键抽取。

商店删牌价格按 5 / 10 / 15……增长，并在每家商店分别重置。玩家可以暂拿某个 group 里不想长期保留的物品，影响后续 offer，再在商店支付删除成本；这是 loot-table / deck-quality / Gold 的三方交易，不是免费过滤。

### 共享机会成本的并行模块

- **engine**：相邻 conductive Manastones 形成 Mana network，Wiz Buckler 读取 Mana 供应 Block；Token / Violet Energy 另行供应 Energy；Spiral Lance 独立造成 5 AOE，并由 Piggy Bank / Gold 锻造增强。Spiral Lance 不读取 Mana 或 Block。
- **state/resource**：carving draw / discard / banish、Mana network、Block、Energy、Gold、锻造与 cells 分开。
- **payoff**：Wiz Buckler 是防守 payoff；Spiral Lance 是独立 AOE payoff。二者共享牌序、Energy / Gold 经济和背包空间，但不存在 `Mana → Block → Lance` 因果边。
- **survival**：Manastone→Wiz Buckler 模块供应 Block；AOE 模块缺失时防守仍可工作，Mana network 断裂时 Spiral Lance 也不会因此失去自己的伤害规则。
- **spatial condition**：Tote hand 与 backpack 不是同一区域；Mana connected network、Spiral Lance 占格与 carving deck 也不是同一空间语法。
- **payoff owner**：Manastones 拥有 Mana；Wiz Buckler 拥有 Block；Token / Violet Energy 拥有 Energy；Spiral Lance 独立拥有 AOE。
- **economy / pivot**：Piggy Bank / Gold 锻造提升独立 AOE 模块，删牌保证抽取稳定；锻造、购买和删除竞争同一 Gold，所有模块也竞争 cells。
- **counter / failure**：抽取稀释、过早扩 deck、Mana connectivity、Energy / Gold 短缺和 cells 被占会分别破坏不同模块。Pocket Trader softlock 只有一条 2024 Guide 正文直接复现，同页 2025-03-06 UTC 评论（PST 页面显示 3/5）则称 Pocket Trader 已可交易、不会再 softlock；后者是没有 exact build / 全平台覆盖的社区反例，不是官方修复说明。“交易前验证目标集合”是面向本项目的推论，不是已经由 Backpack Hero 正式规则 / 修复文档确认的结论。
- **version**：2024 patch 1161 视频提供当前快照，2023 Guide / official Tote rework 提供历史结构；数值和 exact pool 不跨版本合并。

## 构筑四：CR-8 charge network 与循环边界

历史 mage 线使用 Core / Battery 形成 compact loop，把 charge 转为 Mana，再由 Metallic Wand 输出。2026 live patch ASR 展示两类循环：Belt of Knives + Core + 两个 Reversers；或 Core + 两个 Coal + Reactive Cell + 0-Energy weapon。另一个 `v1.0.1187.0` Guide 与 2025 then-live 讨论只作各自版本 / 模式的书面旁证，不能合并为 2026 current 规则。

- **engine**：Core 产生 charge，组件改变方向 / 重复 / 反转 / 转换；旋转和线路是实际配置。
- **state/resource**：charge、component lifetime、alignment、Mana / Energy conversion 与最终 weapon use 分开。
- **payoff**：Metallic Wand、Belt of Knives 或明确的 0-Energy weapon 拥有最终伤害。
- **survival**：循环本身未必提供防守；长循环在高 HP 敌人前会增加执行与点击负担。
- **spatial condition**：网络方向、rotation、连接、blank energy 和终点必须全部合法；普通邻接不能替代线路解析。
- **economy / pivot**：Belt of Knives 不到就不能假设无成本转型；组件占格、替换和另一个终结器是实际机会成本。
- **counter / failure**：Overheat、charge 生命周期、线路过长、缺失 alignment、性能与高 HP 战斗会暴露循环代价；现有 current/live 来源不足以建立完整敌方反制矩阵。玩家还报告 CR-8 / Satchel bag fragment misalignment 与 post-battle overlay 状态问题，但单条评论不证明根因或当前普遍性。
- **lifecycle**：2023-09-06 官方把 CR-8 / Overheat 更新移入 main，明确保留复杂 combo、阻止部分简单 infinite。开发者回复补充：infinite 没被删除而是更难，charge 每穿过 / 使用 component 会增加 heat，历史 capacitor 示例在第三次经过后触发攻击。后来可工作循环不等于 Overheat 无效；它说明 guard 的分类和触发条件必须可见。

## 构筑五：Pochette Pet Bed → recall / resummon 控制

- **engine**：Legendary Pet Bed 在宠物召唤时让目标 Sleep；recall 后重新召唤可再次产生控制。
- **state/resource**：Pochette Energy、宠物存在 / 召回状态、pet pouch、Treat、Sleep 与 pet action 分开。
- **payoff**：Pet Bed 拥有 Sleep 施加；宠物拥有自己的攻击 / 技能。Big Dinky 提供 0-Energy summon，Lenny 可用 Mana / Treat 形成另一资源循环。
- **survival**：Sleep 延迟敌方行动；它不是 Block、Health 或无敌。
- **spatial / target condition**：单目标 Sleep 面对多敌人会漏控；Defender 可遮蔽后排目标，需要 Grapple 把目标拉出。
- **economy / pivot**：宠物、pet pouch、Treat 与普通背包空间竞争；控制不足时需换成多目标、生存或先处理 Defender 的工具。
- **counter / failure**：多敌人、目标被 Defender 保护、Energy 不足或 recall / resummon 时序断裂都会让循环失败。
- **lifecycle**：Pochette 从多 inventory 滚动切换到 pet pouches / Treats，2023-03-15 正式移入 main；召回、出售、治疗和 pouch 交互连续修复。宠物 owner、资源 owner 与最终伤害 owner 必须分层。

## Cleaver 与 Bow：补充空间语法

- Advanced ordering Guide 直接支持 King Cleaver 激活最多四个相邻普通 Cleaver，并以同一 event 内 source-target pair 不重复激活作为 guard。当前记录不加入没有直接来源的 Queen 路线，也不把这个精确 guard 泛化成 visited-item set；稀有核心不出现时完整构筑仍受 RNG 约束。
- 其他 Guides 支持 generic Cleaver chain，以及 four-long Bow / Arrow 对 open space 与 gem modifiers 的依赖；没有直接来源的 same-row Bow 规则已删除。Bow 是 launcher，Arrow 保留自己的贡献 owner。

二者作为空间语法补充，不与五条主构筑合并，也不证明对应 build 的当前强度。

## 所有权、目标与数值读取边界

固定 commit 的历史 ModDocs 将 TriggerType、Area、ValueSource、EffectTarget、Modifier 与 ValueChanger 分成独立 schema：

- trigger 包含 `onDiscard`、`onSummonPet`、`onPetDies`、`onReorganize`、`onUseUntilOverheat`、`onOverheat`、`onEnergyMove` 等不同事件；
- area 包含 adjacent、diagonal、row、column、connected、`toteHand`、`inThisPocket`、`inAnotherPocket` 等不同谓词；
- value source 区分 Player Block / HP / Max HP / statuses、target state、Mana power、pet count、carvings played / discarded 与 pocket / item / curse count；
- effect target 区分 player、enemy、all enemies、reactive enemy、frontmost / backmost 等 recipient；
- modifier 是间接增幅，area / distance / length / stackable 另算；ValueChanger 则表达 source × multiplier + base 的读取链。

这只能证明 2023-09 pre-1.0 数据模型。它不能证明 1.0.1187 / 1.0.1212 的完整枚举、UI、当前数值或内部执行顺序；但足以反驳把所有效果压成一个“邻接加成”或把 modifier 当作直接伤害 owner。

### 盾与元素是正交轴，不是二选一

同一 snapshot 的 `item.schema.json` 把物品拆成 `type[]`（是什么）、`group[]`（属于哪些构筑池）、`combat_effects[]`（做什么）和 `supported_characters[]`（谁能获得）四组数组；`group[]` 还明确影响 RNG，使背包已有同组物品时后续掉落偏向该组。因此项目里的“冰盾 / 土盾都是盾”应表达为多个正交标签，而不是互斥职业树：例如 `type=Shield`、`groups=[Shield, Ice]`、`effects=[Block, Freeze]`，再单独记录角色池。

BaseGameExports Version 3 的 2023-09 快照已有真实交叉：Swamp Buckler 是 Shield + Poison，Wiz Buckler 是 Shield / Carving + Magic，Tower Shield 是 Shield + Armored，Spiked Scale 是 Shield + Spikes，Rapier 同时落在 Melee / Armored / Shield group。Bashing Shield 读取 PlayerBlock 并在同次使用消费一部分 Block；Shield Spirit 保留 Block；Hercule Pavise 激活相邻 Shield；Spiked Scale 产 Spikes；Fox Rapier 用攻击成长相邻 / 斜邻 Shield。它们分别代表积累、保值、连锁、反伤、主 C 桥与兑现，不是六件必须捆绑的套装。

这些精确名称与数值只能标为 2023-09 changeset 677。可迁移的是八层契约：角色所有权、item type、多选玩法 group、effect / status、ValueSource→payoff 转换器、trigger、spatial selector、loop guard / 掉落偏置；不能把 snapshot 冒充 2026 live tuning。

## Curse、Charm、Burn 与敌方意图生命周期

- 2023-04 Curse 系统被全面重做；Cursed Shiv 随后从可无限使用修为每回合一次。这里的价值是“风险输入与 payoff 都要有次数预算”，不是保留旧数值。
- 2023-09 又从敌人攻击移除 Curse，把它转为玩家主动选择的风险；历史 Curse Guide 不能 currentize。
- 2023-04-25 testing-era `Curse Hotfix 2` 明确：Charmed enemies 不再产生 hazards / curse，且总在 uncharmed enemies 前行动。这条历史修复说明控制后的行动顺序与副产物权限必须有 owner，但不 currentize 为 1.0 / 2026 规则。
- 全 encounters 重做以给出更明确意图、反应窗口与 counter。早期评论仍可证明历史上玩家感到敌人不能迫使适配，但不能证明 1.0 后完全解决或没有新问题。
- 2024 Burn 改为在敌人失去 Shield 前结算，是状态 / 护盾顺序修复；Burn、Shield 与 Poison 仍是不同 ledger。
- Relic effects 曾全面重做，随后因反馈让部分旧 relic 返回。评论中 boss reward skip / specificity 是个体观察，不证明所有 relic、所有版本或官方动机。

四条反馈链有官方闭环：Relic 由全面重做→社区具体批评→官方问卷→旧效果部分回归；Curse / Enemy 由强制塞入破坏布局→完全可选风险，并让敌方意图更可读 / 可中断；CR-8 由历史简单 infinite / endless 风险→可见 Overheat 与更高组合门槛；Tote / Pochette 则通过移除 illusory spaces、多背包滚动和移动宠物扣能量来降低 UX 摩擦。每条只按一组方向性 family 计算，不按每个 hotfix 重复计数。

## Story、Quick Game 与局外空间

1.0 将 Story Mode 的任务、逐步解锁和 Haversack Hill 用作复杂度坡道；地牢物品可卖出或拆成 food / material / treasure，建筑研究解锁更强物品，建筑摆位还有自己的邻接效率。这个局外邻接不是战斗 item-grid adjacency，也不直接证明本项目应加入建城。

Quick Game 保留全解锁的经典运行方式。1.0 后官方又因玩家找不到经典模式而解释 `Start Quick Game` 并考虑改名，说明“渐进教学”和“直接沙盒”可以并存，但入口名称必须可发现。改名是否最终落地不在资料中。

## 2024-12 change list 与 2026 平台观察冲突

`December Check-In!` 明确把包括 Acorn Sap 10→12、42 项 rebalances 与 31 个新 Tote items 在内的变化列为 testing branch、尚未进入 main。对应 thread 于 2026-06-18 发起，关键 Epic / Steam 版本回复出现在 6/28–29：

- 一名 Epic owner 报版本 `1.0.1212`，Acorn Sap 仍为 10 Block；
- 一名 Steam owner 报 `1.0.1187.0`，同样仍为 10；
- 第三位只推测 Epic 也许对应 Steam 可选、且仅“slightly newer”的 beta branch。

这形成“testing change list 后续与可见平台版本如何映射仍不明”的社区冲突。不能据此断言这些变化后来进入 main、哪个 branch 等价、Steam / Epic 谁更新或实际根因。2025-11-30 视频只转述不可直接核验的 Discord 消息且无具体规则，不登记为 deep source。

## 负面 / 重做 family（去重 12）

1. **CR-8 简单无限 → Overheat**：官方保留复杂 combo，抑制部分简单 infinite；不是移除所有循环。
2. **CR-8 charge / alignment / 长线路**：blank energy、组件朝向、生命周期、执行时长与性能归为一族。
3. **Tote 多轮重做与 draw friction**：传统背包整理、hand / discard / banish 与抽取 RNG 冲突。
4. **Tote effect attribution 修复簇**：double-count、discard、Druid Staff 等按同一 owner / resolver family 去重。
5. **Tote + Pocket Trader softlock**：一条 2024 Guide 正文直接复现无合法交易目标时卡死；同页 2025-03-06 UTC 评论（PST 页面显示 3/5）称后来可以交易 Pocket Trader、因而不再 softlock。历史复现仍作为 lifecycle family，但后续评论只是版本未知的社区反例，不是官方修复说明，也不证明所有 current build / 平台；频率、根因和正式修复语义仍未知。“进入前验证目标集合 / 可回滚”仅作为本项目推论。
6. **Pochette inventory → pet pouch / Treat 重做**：召回、出售、治疗与 pouch 修复合为一族。
7. **Curse 强制干扰 → 主动风险**：2023-04 重做与 2023-09 从敌人攻击移除，作为一条方向性生命周期。
8. **Cursed Shiv 无限使用 guard**：修为每回合一次，独立于整个 Curse 产品方向。
9. **Charm 行动 / 副产物所有权**：被控制敌人的行动顺序与 hazard / curse 生成权限修复。
10. **Enemy encounter / intent 重做**：更明确意图、反应窗口和 counters；早期敌考题不足评论只作历史问题。
11. **Relic specificity / reward skip**：官方全面改版和部分回滚，与多名玩家的 build-specific downside / skip 观察同族记录；不当普遍统计或官方因果。
12. **UI / runtime-state integrity**：2024–2025 个体报告的 bag fragment misalignment、overlay / reward loss、reload softlock、controller overlap / misclick 合成一族；不把一条 bug 清单拆成许多机制 family，也不采信未证根因。

重复点击 / pivot lock 是相互关联但不同的两个设计观察：本 dossier 把前者作为手动权限的不兼容边界，把后者作为空间承诺 / offer quality 风险，不再额外抬高 lifecycle 数字。2024 Burn / Shield ordering 是另一条规则修复，但不为凑数重复计入。

累计 negative / reworked family 从 365 增至 377。

## 经济、转型与失败解释

背包空间不仅是容量，也是沉没成本：成形的 adjacency / pocket / network 一旦占满，换入新高稀有物品可能破坏多条连接。2022–2023 多名玩家描述 build 锁定后难 pivot、敌人不足以迫使适配，以及战斗退化成重复点击；2025 玩家又描述 item pool specificity / dilution 与 boss relic skip。它们是不同年代的社区观察，不是同一版本的实验证明，也不能推出 1.0 后完全没有有效转型。

可迁移的失败报告应显示：

- 哪个空间谓词断裂，释放 / 占用了哪些 cells；
- 哪个 supplier 未产出资源，哪个 reader 未满足，哪个 payoff owner 未行动；
- Block 吸收、Health damage、Poison / Burn tick 与 Spikes retaliation 的顺序；
- draw / discard / banish / fixed-item 归属，CR-8 charge 路线和 Overheat 原因；
- pet summon / recall、Sleep target、Defender 遮挡与 Grapple 位移；
- 交易目标集合、删除 / 复制 / 出售的原子结果和 reload 后的持久状态。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

- Steam News API 175 个标题全量筛查，读取了 1.0、角色、CR-8、Tote、Pochette、magic、relic、curse、enemy 与 2024 状态顺序等重点正文；另核验了 Tote redesign goal、Pochette main delivery 和带开发者身份的 testing / Overheat 回复。
- 17 个英文 Steam Guide listing 全筛，其中包括 `v1.0.1187.0` 的 Tantatals Guide；共 12 篇有足够完整正文并登记。Discussions 按 CR-8、Tote、Pochette、Satchel、curse、difficulty、版本差异筛查；8 个 discussion / comment threads 进入来源包。
- Steam Reviews API 中逐条读取并登记 9 个长评；它们只作带日期的个体观察，helpful / playtime 不当代表性统计。
- Wiki.gg API 的 Purse、Satchel、Tote、CR-8、Pochette、Armor、Cursed Items、Accessories、Bestiary、Relics 与 Manastone 十一页均逐页读取；旧 Fandom 深层记录已由这些维护页与版本固定 Guide 完全替代。
- 官方 GitHub ModDocs README 与相关 schema 已逐页读；最终登记 9 个固定 commit 记录：7 个 effect-model schema、1 个 item schema 与 1 个 BaseGameExports ZIP，并明确 WIP / historical 限制。
- 两段 ASR 全文逐句读取：2024-11-15 patch 1161 Tote 教学 486 秒；2026-01-17 live-patch 构筑视频 878 秒。另一段 2025-11-30 视频只转述 Discord 且没有可核验规则，不登记。
- RPS、PC Gamer、GamingOnLinux 文章已实际阅读；其内容主要重复 identity / high-level premise，低于现有官方 + Guide + 实战包的信息增益，不登记为独立机制证据。
- 停止理由：85 个来源已闭合五种角色操作语法、五条主构筑、两条补充空间结构、正交 type / group / effect / owner 模型、交易 / 转型、敌方考题、状态顺序、12 个 lifecycle family 和一个 2026 平台交付冲突；继续检索主要增加同类构筑、无字幕视频或重复评论，不能可靠补出 current item catalog、内部 resolver 或平台 build 映射。

## 可迁移与不可迁移

可迁移：

- 将“盾”拆成 supplier、Block ledger、显式 damage reader、payoff owner 与消费代价；元素标签是另一轴。
- 把 exact cells、邻接、行列、连通网络、pocket、hand、direction / rotation 作为不同空间谓词。
- 显示装备 / 遗物的 holder、recipient、value source、modifier、最终伤害 owner；全队 Defense / HP → carry 的转换必须由明确 reader 授权。
- 让薄 deck、删牌价格、商店重置与 offer manipulation 形成可解释的经济机会成本。
- 循环系统显示次数预算、Overheat / recursion guard、被拒绝事件和终结器；不把“理论循环”当“已完成伤害”。
- 交易、复制、出售、召回和 post-battle 奖励使用原子事务，并在目标集合为空时拒绝进入流程。
- 敌方包攻击一条构筑链的具体环节，并给玩家可读预告与可执行 pivot。

不可迁移：

- 不复制角色、物品、relic、pet、carving、敌人名称、数值或配方。
- 不把手动每回合用物品当成本项目应该拥有战中点击或新的 tactical command。
- 不把 2023 ModDocs、Early Access Guide、patch 1161、2026 live patch 和平台版本观察混成一个 current ruleset。
- 不把 Block、Poison、Burn、Spikes、Charm、Sleep、Mana、Energy 合并为一个元素 / 状态资源。
- 不把社区评论当采用率、胜率、普遍问题、根因或修复效果证明。
- 不从 Acorn Sap 冲突推定 branch 对应、testing change list 后来进入 main 或实际部署状态。

## 未决问题

- Steam `1.0.1187.0`、Epic `1.0.1212`、可选 testing / beta 与 2024-12 change list 的真实交付映射。
- 当前完整 item / relic / carving / pet / enemy catalog、掉率与 build frequency。
- 当前 resolver 对所有 trigger、modifier、target、pocket、network、Overheat 与状态结算的完整顺序。
- Happy Buckler / Crown / Bashing Shield、Satchel Shiv、Tote 并行 Block / AOE、CR-8 loops 与 Pochette Pet Bed 在不同难度 / boss 的胜率和失败分布。
- Poison、Defender、多敌人、Disorder 与各角色高难模式的完整 counter matrix；CR-8 live loop 的敌方 counter 仍无足够 current 来源。
- UI / reload / controller / reward-state 报告的复现、平台范围、修复版本和剩余影响。

## 最终 disposition

`anchor-retained`

85 个实质来源跨官方规则与版本、完整攻略、两段有版本边界的实践视频、讨论、长评、维护 wiki 与固定 commit schema / export，能够闭合 Shield→damage、Satchel pocket Shiv、Tote 薄 deck 下的并行 Block / AOE 模块、CR-8 charge loop、Pochette summon-control 五条 materially different structures，并解释 Cleaver / Bow 空间语法、正交体系标签、经济承诺、敌方 counter、owner attribution 与 12 个去重生命周期 family。

它对本项目最有价值的不是某个配方，而是证明“体系”必须分 supplier、state、reader、payoff owner、survival、space 与 pivot；冰盾 / 土盾可以同时属于 Shield 轴和 Element 轴，但只有显式转换器才把防御变成伤害。由于 Backpack Hero 的战斗是玩家手动逐回合使用物品，它只作为相邻构筑设计锚点，不作为本项目自动战斗、战中命令或数值权威。
