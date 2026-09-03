# Just King

## 身份与证据边界

- `title_id`: `just-king`
- 子类型：围绕 King 的四英雄环形队伍、实时移动战斗与 roguelite 路线/商店构筑。
- 状态：2023-12-19 脱离 Early Access；2026-04-02 的 1.4.1 是本次能见的最新官方更新。
- 核心版本边界：0.3.0 指南、2022 构筑、0.4.x 系统重做、1.0.x campaign、1.1+ Duels 和 1.4 UI/input 变更分开处理。
- 置信：中高。官方版本链很完整，历史规则与构筑文本详细，但缺少 campaign 胜率/选用率、当前完整数据库与公开目标顺序规范。
- 本档不使用商店页或视频标题来推断深度；不把 Duels 的全自动数值/meta 写成 campaign 规则。

## 实质来源

本 checkpoint 登记 29 个非商店页：

- 14 个 `official-patch`：`src-jk-official-2023-06-13`、`src-jk-official-2023-08-24`、`src-jk-official-2023-12-19`、`src-jk-official-2023-12-21`、`src-jk-official-2024-01-11`、`src-jk-official-2024-03-11`、`src-jk-official-2024-07-05`、`src-jk-official-2024-08-01`、`src-jk-official-2024-09-03`、`src-jk-official-2024-10-29`、`src-jk-official-2024-11-01`、`src-jk-official-2025-02-15`、`src-jk-official-2025-12-05`、`src-jk-official-2026-04-02`。
- 1 个 `official-dev`：`src-jk-official-2026-01-21-ui`，开发者明确承认 1.4 UI 反馈未达预期并提供 legacy 分支。
- 4 个 `strategy-guide`：`src-jk-guide-hero-030`、`src-jk-guide-achievements-110`、`src-jk-guide-pacifist`、`src-jk-guide-food-052`。
- 10 个 `community-analysis`：`src-jk-thread-favorite-team`、`src-jk-thread-op-builds`、`src-jk-thread-three-bards`、`src-jk-thread-formations`、`src-jk-thread-druid-cleric`、`src-jk-thread-zone3-boss`、`src-jk-thread-zone4-boss`、`src-jk-thread-boss-disagreement`、`src-jk-thread-level-three-economy`、`src-jk-thread-heal-owner`。

四个指南都显示 Steam community removal notice，但页面仍直接公开完整正文、日期与作者；本档记录可访问性异常而未绕过权限。所有社区“OP”、“太难”或“太弱”都是有版本的观察，不是人群统计。

## 真实循环：固定十字跟随 King 实时移动

campaign 不是战前摆好后的纯观战。队伍有固定 `front / back / left / right` 四槽，King 是中心；玩家在战斗中持续移动乃至旋转整个十字。`src-jk-thread-formations` 证明“其他 formation”当时只是提议，`src-jk-guide-hero-030` 则记录了英雄换槽与道具放入槽位的操作。

相对槽位是规则而非装饰：

- Druid 在 front 变 Turtle tank，left/right 变 Wolf damage，back 变 healer；狼形 Lv3 还把 haste 给对面槽的英雄。
- 历史 Wizard 在 front/back 和 left/right 进入不同元素形态。
- 多件道具同时有“持有者/槽位加成”与“指定 synergy 全队广播”，King 上的道具也可给全队广播。
- 1.0.1 官方重新加入 WASD+方向键、鼠标与 tank controls，证明移动/旋转是玩家实时输入层。

所以“移动速度”同时可能是输出速率、接触率、回避资源和地图互动条件，不是普通的面板数值。`src-jk-guide-pacifist` 的零英雄伤害路线要求玩家绕圈、拉敌踩 hook/门、保持距离；Cultist 触手没有可命令目标时不会自动攻击。这是很强的“玩家移动→整队目标/范围/地图对象→结算”证据。

## 属性、synergy 与所有权

每名英雄在早期规则中带两个 synergy 标签；两名同类英雄启动第一档、四名启动更高档。Haste 同时影响攻击、治疗与带沙漏的技能冷却，因而一个 haste 来源能够加速不同的团队职责。

0.4.0 将通用 damage 改为 Strength 与 Intellect，重做道具、经济、Heavenly 和 hero-token 来源。这使 0.3.0 的具体数值失效，但不改变可迁移的设计问题：一个效果究竟由谁产生、作用到谁、根据谁的数值缩放，必须写清。

`src-jk-thread-heal-owner` 的三条回复一致把 healing bonus 解释为“持有者造成的治疗”，包括 self-heal；King 的回合间治疗也读 King 上的治疗道具。这是所有权语义的社区证据，不是当前数值公式。

## 具名构筑一：Ronin 移速→多段伤害引擎

队伍：`Ronin + Huntress + Knight + Bard`（`src-jk-thread-favorite-team`，2025-10 至 2026-06 社区样本）。

- **driver/engine**：Huntress 提供 movement speed，Bard 提供 haste，Knight 以 stun 创造输出窗口。
- **state/resource**：队伍移动速度、Ronin 攻击次数和 Lv3 Steel Storm 触发进度。
- **payoff**：Ronin 在高移速下更频繁触发 Lv3 多段斜切；2023-08 官方还一度让 Ronin Lv3 给全队移速，2024-03 将其 Lv3 party mSpeed 从 50% 降到 30%，说明该联动是真实平衡轴。
- **survival**：Knight 承受接触并提供 stun，Bard 提供治疗/节奏支持；替代版本用 Ranger 换 Huntress，以狼削护甲并分担伤害。
- **spatial condition**：玩家必须持续移动整个十字，让近战的 Ronin/Knight 有接触时间，同时不把 Bard/辅助送入危险区。
- **payoff owner**：Ronin 拥有多段伤害；Huntress/Bard 只是速度供应者，Knight 拥有 control window。
- **pivot/counter**：Huntress 可以被 Ranger 替代，前提是 Ronin 已有足够移速；slow、重复冲刺使近战无法接触、即死 telegraph 和狭窄安全区会直接破坏引擎。

这套构筑不是“四个同类标签”，而是控制、移速、haste 和一个付费主体组成的横向系统。

## 具名构筑二：Knight 前排＋双治疗＋Imp 后排

队伍：`Knight front + Cleric/Paladin on both sides + Imp back`（`src-jk-thread-op-builds`，2022-08 Early Access）。

- **driver/engine**：双治疗把 Heavenly/治疗循环维持起来；Knight 用坚硬与 AOE stun 保持敌人聚集；haste 缩短 Imp 攻击/燃烧链。
- **state/resource**：Knight 的 HP/Armor/Block，队伍治疗节奏，Imp 在单体上的 Burn 层数。
- **payoff**：Imp 用 flat damage 与 haste 进行单体叠烧，Lv3 在燃烧敌人死亡时向群体扩散；Knight 的 AOE 是副输出。
- **survival**：Knight 拥有 front 槽和防御道具，两名 healer 互相弥补“Cleric 不能治自己”或让 Paladin 自愈并补前排。
- **spatial condition**：front 主动接敌，left/right 保持对前后的治疗覆盖，back 保护主输出；玩家移动 King 决定谁先接触与 AOE 覆盖。
- **payoff owner**：Imp 拥有 Burn/爆发伤害，Knight 拥有 AOE/control，各 healer 拥有自己造成的治疗；全队 aura 道具不应把伤害归到 King。
- **pivot/counter**：Cleric 适合 burst damage/需要 resurrection 的考卷，Paladin 适合自愈和前排压力；如果敌人迫使全队移动或直接切后排，槽位带来的安全假设就失效。

同帖还给出 `2 Archers + 2 Berserkers`：Hot Hands 提供 haste，Frenzy 提供百分比伤害/移速，一名 Lv3 Archer 持 flat-damage 或 flame-AOE 道具，通过 kiting 而非厚坦生存。它是另一个“全队状态→单一射手结算”的历史样本，但本档不将 2022 数值当成 current meta。

## 具名构筑三：3 Bard＋Lv3 Captain 的经济召唤线

队伍：`3 Bards + Lv3 Captain`，Area 3 Hard 单次通关记录（`src-jk-thread-three-bards`，2022-08）。

- **driver/engine**：Bard 额外金币让玩家留钱买 Wrath Crown，haste items 同时加速支持和 Captain 的队伍节奏。
- **state/resource**：金币、Bard 等级、Captain 等级、Discord 效果和 Forgotten Heroes 临时人数。
- **payoff**：两名 Lv3 Bard 的 Discord 与 Captain 的幽灵军团过前两波，全队 haste 保持 Boss 战存活；一名 Bard 因 reroll 未中而仍是 Lv1。
- **survival**：开局 Chainmail，早期 Frost Witch 控场桥，最终 slowing item 和 haste 道具。
- **spatial condition**：Captain 与召唤物需要前排接触，Bards 需要在十字的相对安全槽维持 Discord/heal；地图 AOE 或不利移动会破坏这个静态角色分工。
- **payoff owner**：Captain 拥有 Forgotten Heroes 召唤，每个 Bard 拥有自己的 Discord/支持，Wrath Crown 只是 King 槽上的全队 haste 供应者。
- **pivot/counter**：Frost Witch 只是早期桥，队伍在完整时替换她；玩家接受一名 Lv1 Bard 而不追到全员 Lv3。突发精英爆发和未买到 Wrath Crown 是路线风险。

`src-jk-thread-druid-cleric` 补出更一般的桥接逻辑：2 Bards 可以早期赚钱，后来合成 1 名 Lv3 Bard 释放其他三槽，或卖出为 elite 战替换职能英雄。这是可识别的经济桥，不是“未成型的垃圾英雄”。

## 招募、升级、装备与替换

- 商店中拖拽英雄/道具，可换槽、锁店、reroll；早期为三格商店的社区抱怨不用于推导当前概率。
- 同 synergy 英雄可拖拽合并升至 Lv3；售出英雄会返回 token，event 也可用职业 token 升级。
- 道具同时占据“持有者”、“相对槽位”和“synergy/全队广播”中一个或多个所有权轴；0.4.0 的 STR/INT 和 effect-item 重做说明装备系统会改变构筑责任，不只是数值增量。
- 0.4.0 加入可编辑 item pool 和起始物品 favorites；1.1.0 成就攻略也建议为特定属性目标修改 pool。这降低解锁造成的池子稀释，但不证明所有玩家都这样使用。
- 进一步资源 sink 在后续区域展开：Cook 以 token 换永久 STR/INT/Haste/HP 食物，Smith 以 token refine equipment，Merchant 先收 token 展示特殊道具再收 gold。这让全队 Lv3 后的资源仍有去处。
- 无独立 reserve/bench 证据。社区明确抱怨 Bard 要为 elite 战换人必须卖掉，这个“无长期备用槽”仅作为历史机会成本，不外推当前界面。

## 区域与 Boss：操作性能力试卷

### Zone 3：Oxygen、角落与 slow

`src-jk-thread-zone3-boss` 给出具体动作链：持续维持 O2，只在 Boss 刚召三根触手后进角落补氧，快速清 summon，躲开会 slow 的红色投射物。另有玩家说 phase-2 minions 也会 slow，但该点保留为不确定。近战 Deep 英雄的解锁线显著更难，因为补氧和输出接触争夺同一段移动时间。

### Zone 4：接触率与自己构筑的反向影子

`src-jk-thread-zone4-boss` 报告 Boss 连续横冲，使近战新英雄长时间打不到它；带 Berserker/Bard 时，影子复制反而使战斗更难。这是两名玩家的负面样本，但它精确暴露两个设计问题：近战伤害面板不等于实际 uptime；“复制玩家单位”会将原本的强力 synergy 转成 Boss 放大器。

### 初见 Boss 的归因分歧

2024 帖子中，一方认为初见即死、freeze-safe-zone 和 movement-speed 要求浪费整跑时间；另一方列出“即死变 1 damage”的道具、200–300 HP、15 armor、resurrection item/Cleric、观察 telegraph 和 Boss 前尽量全 Lv3 等反制。这证明反制存在，也证明反制的预告/可读性仍可以失败；不能从该帖子推导“Boss 普遍太难”或“都很公平”。

### Zone 5：路径站位也是选边

1.0.0 的 Dreek Coliseum 让左/右/中站位决定击杀归属和 trophy；各 God 的祝福同时影响玩家与敌人。Koi Spirit 让队伍在移动中逐步加速，但移得过快会提高受伤；Abs God 极端强化 front hero 却削弱其他人。这两者把“全队移动”和“单前排集中”变成明确交易，而非无条件增益。

## 版本、失败与重做

1. **道具自增益无限叠层**：1.0.0 hotfix 修复 Sigil of Bravery damage amplify 无限叠层。可迁移的不是该数值，而是每个“伤害增幅触发伤害”的效果都需要 source/root owner、可重入标记与叠层上限。
2. **加强后明显超标**：1.1.5 先加强 Sharkmancer，1.1.13 官方随后说其“overperforming ... by A LOT”并降低攻速/伤害。这是 Duels 定性案例，不外推 campaign；但它证明“召唤物＋自愈＋远程 hook”的角色压缩了多个职责。
3. **死亡对象仍在结算伤害**：1.2.0 hotfix 修复死亡 Bard 从 afterlife 秒杀敌人。死亡必须立即撤销行动资格、订阅/待结算触发与来源信用。
4. **存档转换风险**：2025-02-15 因 save conversion 问题整体 rollback，并将新版留在 open beta，以保护进度。这是正确的“不带病向前迁移”案例，但官方未公布技术根因。
5. **UI 重做没有达到预期**：1.4.0 为 mobile/Steam Deck/信息密度重做界面和输入；官方后来明确说新 UI “wasn't received as we expected”，保留 legacy，1.4.1 又重写导航并加入 reduced UI toggle。这说明高密度构筑信息不能只为触屏放大，还需平台特异的视野和操作路径。
6. **campaign 与 Duels 版本混用风险**：Duels 明确无 King 上场、开战后全自动、英雄/道具另调数值且有 mode-only item。任何不标模式的 tier/build 数据都不能直接支撑 campaign 结论。

本 checkpoint 把前五项计为 5 个明确 negative/reworked cases；第六项是证据治理边界，不重复计数。Ronin 移速下调、Druid 形态多次修正和道具槽位变更只作版本依赖证据，不在不能证明失败原因时凑负面数。

## 对本项目可迁移

### 可迁移原则

- **相对槽位 ownership**：单位可以因 front/back/side 承担不同职责，道具也可读相对槽位；但要在战前就预览形态、范围和所有权。
- **通用骨架，不同付费主体**：移速/haste、治疗、护甲和 summon 可以是全队 engine，但应让 Ronin、Imp、Archer 或 Captain 这样的单一主体拥有结算。
- **防御、元素与空间是正交轴**：Knight/Paladin/Cleric 的护甲治疗可以支撑 Imp Burn 或 Archer 多段；“冰盾/土盾”可同时属于防御与元素通道，不需被强制归为同一套垂直羁绊。
- **Boss 是能力试卷**：Oxygen/角落、slow/projectile、近战接触、召唤清理、即死保护和复制反向 synergy 可以分成不同预告的能力试卷，不应用一个“总战力”表示。
- **桥接与替换条件**：Frost Witch 的早期控场、Bard 的经济、Ranger 的狼分担，都不必被设计成后期核心；要显式告诉玩家何时它的职责完成。
- **来源归因**：全队 aura、持有者治疗、召唤物、死后残留触发和多段伤害必须分开记账，否则无法解释谁导致胜败。

### 不可直接迁移

- 本项目是战前空间部署＋实时自动战斗＋两个独立战术指令/三点共享资源；不是 twin-stick/arena-shooter 式持续控制 King。
- 不移植长时间绕圈 kiting、为补氧跑角落、持续诱敌踩 hook/门、拾取 Hot Hands 物件或旋转整个十字来对准范围。
- 可接受的转换只是：战前相对槽/网格、敌方预告后的局部重定位、范围/接触/地图对象的能力试卷，以及可由有限战术指令表达的一次性移位。
- 不移植 King 死亡立即失败；本项目已确认任一非临时 roster hero 存活就继续战斗，起始英雄不是特权指挥官。
- 不移植 Duels 的十回合 PvP 经济、排位、模式数值或开战后纯观战前提。

## 未决问题

- campaign 1.4.1 的完整英雄/道具数值、商店概率、目标顺序与格式化 combat log 未找到公开规范。
- 环形队伍的精确碰撞、个体脱队/归队、攻击目标重选和旋转后范围更新顺序未被官方完整公开。
- 2025/2026 社区 Ronin team 没有记录难度、全道具和胜率；只能证明实际玩法链，不能证明当前最优。
- 指南页面的 removal notice 原因不明；尽管正文可读，仍应保留可访问性风险。

## Disposition

`retained`

Just King 超过普通长尾的两源门槛：官方版本链、完整历史规则指南、多套具名队伍、招募/升级/装备/替换、四个区域/Boss 能力试卷和五个明确 lifecycle failure 均有证据。不升为 anchor：实战证据仍以 Steam Guides/discussions 为主，campaign 当前统计、完整官方规则和行为顺序不可得，且 2022 详细构筑必须与后续数值重做分离。
