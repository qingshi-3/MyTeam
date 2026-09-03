# Private Military Manager: Tactical Auto Battler

## 身份与证据边界

- `title_id`: `private-military-manager`
- 子类型：雇佣兵公司管理、四人小队、房间/区域计划与实时自动战斗。
- 状态：2024–2025 Playtest/Demo；2025-04-28 Early Access；2025-07-10 官方宣布团队解散并永久停止更新，成品保持未完成。
- 核心版本边界：world-map 方案、2024 Playtest、2025-01 Demo、2025-04 EA launch/hotfix 与停更后的最终版分开处理。
- 置信：中。官方开发日志和补丁能建立循环、五职业、训练、指令、失败/版本；多个长评与讨论给出具体四人编制、房间策略、经济和任务反制。但无完整手册、数据库、统计或正式攻略，实战高度集中在 Steam 社区。
- 商业表现不足与永久停更只建立生命周期事实，不能被反推为某一机制导致失败。

## 实质来源

本 checkpoint 登记 26 个非商店页：

- 6 个 `official-dev`：`src-pmm-official-loop-rebuild-2024-05-13`、`src-pmm-official-playtesting-2024-06-21`、`src-pmm-official-training-rework-2024-10-30`、`src-pmm-official-company-growth-2024-11-14`、`src-pmm-official-structure-2025-04-28`、`src-pmm-official-shutdown-2025-07-10`。
- 5 个 `official-patch`：`src-pmm-official-playtest-1-2024-07-02`、`src-pmm-official-playtest-2-2024-07-11`、`src-pmm-official-hotfix-1-2025-04-29`、`src-pmm-official-hotfix-2-2025-04-30`、`src-pmm-official-hotfix-3-2025-05-02`。
- 7 个 `detailed-review`：`src-pmm-review-cn-tactics-2025-04-29`、`src-pmm-review-kr-runlog-2026-02-19`、`src-pmm-review-en-time-mood-2025-05-30`、`src-pmm-review-de-training-2025-06-21`、`src-pmm-review-tw-roles-2025-07-18`、`src-pmm-review-en-cover-ai-2025-05-11`、`src-pmm-review-en-tier-promotion-2025-05-01`。
- 8 个 `community-analysis`：`src-pmm-thread-long-feedback`、`src-pmm-thread-timer-mission-counter`、`src-pmm-thread-freeplay-pivot`、`src-pmm-thread-medic-section-budget`、`src-pmm-thread-four-operators`、`src-pmm-thread-training-and-double-tank`、`src-pmm-thread-criticism-and-power-score`、`src-pmm-thread-grenade-activation`。

长评和讨论提供玩法样本，不代表总体统计；职业“必需”、数值“过强”和难度评价全部带时期/作者限制。

## 真实循环：观察、重排、重试，而不是直接操纵每一步

官方把核心拆成 `loadout + mission planning + combat`，外层是招募、训练、公司事件、装备、研究、派遣与资金/关系。玩家先观察同一任务的模拟战，再改装备、职业、站位和区域策略，直至实战。world-map 大系统被主动舍弃，开发者先验证“组队→训练→计划→反复试任务→通过”的核心闭环。

战斗不是纯录像，也不是 Door Kickers 式逐单位路线控制：

- 赛前按每个 section/room 允许技能、治疗政策、爆破和手雷，设置 Crossfire、Scatter Fire、Slice the Pie 等战术。
- 战中可手动选择手雷落点、在清完一区后重排四人位置，并改变观察速度。
- 开发者明确表示逐单位手动画路线与核心结构冲突；单位仍自动移动、选掩体、选择目标和开火。

这条边界很适合研究“玩家给意图与预算，AI 执行路径”的设计，但不能把 PMM 的房间政策表直接替换成本项目已确认的两个战术指令与三点资源。

## 四人队、位置与目标

EA 可见战斗队固定四人。编号 1/2 更常承伤，3/4 相对靠后；房间清理后可把失去 Armor、medkit 不足或濒危的前位换到后排，让健康成员接压。

武器与战术共同改变目标图：社区说明 SMG 在 fire-at-will 下优先 armored targets，shotgun 优先 unarmored，DMR 优先 strongest；Crossfire 让 1/3、2/4 分别集火，Scatter Fire 令每人分散目标，Slice the Pie 提供入室回避/减速。社区对具体数值有争议，但“装备类型＋计划政策决定 AI 的目标职责”由多帖交叉支持。

## 具名构筑一：盾 Vanguard—SMG Breacher—Pointman—Marksman

来源：`src-pmm-thread-four-operators` 的简表与 `src-pmm-thread-long-feedback` 的完整职业论证，2025 Demo/EA 历史结构。

- **driver/engine**：Vanguard 首位承压；Breacher 用 C4 开门并眩晕密集房间；Pointman 管手雷/AOE；Marksman 在后排远程单点。
- **state/resource**：Vanguard Shield/ballistic Armor、Breacher charges、Pointman grenades、弹药与房间策略。
- **payoff**：Marksman 远距击杀；Breacher/Pointman 清除或控制开门后的密集目标；SMG 处理 armored target。
- **survival**：盾 Vanguard；Breacher 另配 SMG 与 flak jacket；Slice the Pie 或开门眩晕降低第一轮压力。
- **spatial condition**：Vanguard/必要时 Breacher 在 1/2 位，Marksman 在后排；有 breach point 的房间才兑现 Breacher 的完整价值。
- **payoff owner**：Vanguard 拥有承伤，Breacher 拥有破门/shotgun 或 SMG 作用，Pointman 拥有手雷，Marksman 拥有远程终结。
- **pivot/counter**：无门房间可移除 Breacher；两个长距区可上双 Marksman；多个聚集房间可上双 Pointman。SMG factory 敌人会快速撕碎盾/Armor，迫使换位、不同防御或加速击杀。
- **version context**：只证明角色/装备/房间之间的责任图；“Vanguard、Breacher 必带”是单帖历史判断，不是正式版统计。

## 具名构筑二：双 Vanguard—Pointman—Medic

来源：`src-pmm-thread-training-and-double-tank` 的 `Searching for Freedom` Freeplay 样本。

- **driver/engine**：开局三 Vanguard/两 Medic 池中招 Pointman，形成 `2 Vanguard + 1 Pointman + 1 Medic`。
- **state/resource**：两面 riot shield/ballistic protection、medkits、Pointman 手雷、每周工资与 mood/discipline。
- **payoff**：Pointman 是主要主动/AOE 伤害；双 Vanguard 让同一伤害压力可在两名坦克间轮换。
- **survival**：两个盾前排加 Medic；玩家明确把它比作双坦＋治疗＋DPS。
- **spatial condition**：承压者位于 1/2，清房后可换位；Medic/Pointman 避免长期站最前。
- **payoff owner**：Vanguard 各自拥有 Armor/盾承伤，Medic 拥有恢复，Pointman 拥有伤害/手雷。
- **pivot/counter**：尽早让两 Vanguard 都获得 riot shield；若工资、负面事件或 mood 使双坦不可持续，应解雇/替换而不是无上限养备用员。
- **version context**：EA launch 单人样本，能证明双坦职责，不证明普遍最优。

## 具名任务反制：Vanguard—双 Medic—Sniper＋房间预算

来源：`src-pmm-thread-timer-mission-counter` 与 `src-pmm-thread-medic-section-budget`。

- **driver/engine**：Vanguard 首位，两个 Medic 提供治疗/section-start ballistic Armor，Sniper 后排终结；Slice the Pie＋Crossfire 控制入室和集火。
- **state/resource**：medkit charges、每个 section 是否允许 medkit/Armor buff、四人剩余 Armor/Health、房间数。
- **payoff**：Sniper/DMR 对 strongest target 单点；Crossfire 用 1/3、2/4 配对打硬目标。
- **survival**：双 Medic 和 Vanguard。Armor buff 每区消耗 medkit 且离开 section 后失效，不能全程无脑开启。
- **spatial condition**：前位持续换伤；早期只有一两敌人的 section 禁止 medkit，把次数留给后段小军团/Section 5。
- **payoff owner**：Medic 各自拥有 medkit 和 Armor/Heal；Vanguard 拥有前排承伤；Sniper 拥有远程伤害。
- **pivot/counter**：盾敌导致错误集火时，切换武器目标策略/Crossfire；Section 5 开场火力用 Slice the Pie。若是高密度无门房间，Pointman 手雷可能比第二 Medic 更合适。
- **version context**：Demo 任务样本；社区给出的 350–400 Armor、五/七次 medkit 和九 section 数只保留作历史预算示例。

## 敌情观察与赛前适应

完成 Freeplay 的玩家明确放弃“同四个 T5 从头打到尾”：

- 无门就不带 Breacher；两段长距上双 Marksman；多段聚集敌人上双 Pointman。
- 任务 modifier 决定能否准备、敌人类型、收益和关系成本。短任务可能直接进场、失去整周训练；多一周也意味着多付一周工资。
- Shotgun Factory 被高 Mental/Armor 前排克制；SMG Factory 反过来快速打碎盾/Armor。
- Cartel 关系可降低招募/工资，CIA 关系给武器/现金桥；强力 operative 太早拿会成为工资“白象”。

敌情预览只有在能映射到“房间→敌类→目标政策→装备/职业→资源次数”时才有意义。一个 combat power 总分无法替代这张图。

## 成长、装备与替换

训练日分 morning/afternoon/night 三段，可选择训练/休息；更高成长会增加 injury/behavior 风险，食物、药物和休息可对冲。轻伤是重大伤病前兆，止痛药可以换取眼前考核。训练预设能减少重复操作，但开发者仍承认旧训练决策浅到“三行 Python”，于是重做成低频、更深的选择。

角色有职业、traits/modifiers、tier、属性、等级 perk/Tac. Skill 与装备限制。低编号 tier 更强、更贵且成长上限更高；低级雇员不能直接升 tier，导致玩家必须重新招募和训练，并承担签约金、工资和旧员闲置/解雇成本。装备可以转交，但官方曾修复未续约装备消失、编队外人员仍可装备和 permadeath 被回滚等所有权问题。

评论给出的历史优化轴并不健康：Shooting 让击杀加速并削弱 reload/Tactics 的价值；Mental 同时提高 ballistic mitigation，压过 Physical/Health；部分 ammo-on-kill perk 又消除弹药/装填预算。这说明“职业＋武器＋属性＋perk”必须各自拥有不可替代的职责，否则五职业仍会收敛成固定答案。

## 失败复盘与可读性

官方从 Playtest 起就想让玩家知道“哪个选择/未考虑系统导致失败”，并用 weekly goals/Trust 让落后更早暴露。最终可玩版仍出现多个归因缺口：

- headshot damage 一度漏出战后统计；combat power 超过 100 或路径未显示；
- Power 被玩家认为偏向 raw DPS，忽略 accuracy/recoil/crit、持有者 Perception/Tactics 与敌方 cover；
- reload speed 高低方向、乘法 perk 和装备适配不清；
- 模拟战缺少前后结果对比；屋顶遮挡、无法自由移动镜头使敌人死在画外；
- 友军 AI 可能选择木箱/开阔地而敌军用混凝土掩体；编号 aggro 甚至让敌人追击远处 Vanguard。

战报不能只给总 Power 或胜负：至少要显示每区首轮伤害、Armor/Health 分层、目标政策命中、手雷/C4/medkit 使用与未使用原因、换位、掩体、击杀/伤害归属和资源剩余。

## 版本、失败与重做

1. **world-map 方案重构**：因 faction/world-map 难以持续生成有趣事件，开发者撤回大系统，先验证 squad/training/mission 核心。
2. **训练浅选择重做**：旧版被内部评价为“三行 Python”级决策；改成三时段、场地组合、轻伤预警和风险成本，但玩家又出现重复/信息过载分歧。
3. **负面事件与伪选择**：开发者承认负事件过多，`-1 Discipline/day` 等选项表面可选、实际不可用；Hotfix 又禁止开局极端负 modifier。
4. **Freeplay 后段难度墙**：官方根据玩家报告调整过度惩罚且不一致的后段 spike；社区仍记录第三任务和 hostage-rescue 的陡升。
5. **固定职业/属性收敛**：历史玩家认为 Shooting/Mental、Vanguard/Breacher 过度占优，Health/Tactics/Medic/shotgun 价值被挤压；这是社区 meta 诊断而非官方统计。
6. **Power/路径/战报误导**：combat power、超过 100 的显示、行动路径、headshot 统计均在首发热修；社区仍指出总分偏 raw DPS、公式/方向不清。
7. **手雷激活像 bug**：近门立刻接敌时不投、长房间接近前才投；开发者承认激活条件对玩家看起来像故障，另有 grenade＋gun mod/爆破冻结修复。
8. **AI 掩体与编号仇恨**：长时评测报告友军选弱掩体/开阔地；社区观察编号优先可能压过实际距离。这是低置信 AI 失败样本。
9. **装备/死亡状态所有权**：未续约装备丢失、编队外装备、permadeath 结果被回滚分别被官方修复，证明 roster/equipment/battle result 需要原子结算。
10. **十任务终点切断成长**：官方 Freeplay 只有十任务，计划中的结束后 sandbox 从未交付；玩家报告高 tier 刚拿到或未训完 run 就结束。
11. **永久停更**：官方将原因归于商业表现不足并退款；只记录产品生命周期，不计机械失败因果。

本 checkpoint 将第 1–10 项计为十个 materially distinct negative/reworked cases，累计 92；第 11 项作为商业生命周期单列，不加入机械失败数。

## 对本项目可迁移

### 可迁移原则

- **敌情预览必须映射到具体职责**：门、房间距离、聚集度、Armor、首轮火力和 section 数分别要求爆破、射程、AOE、破甲、开场保护和资源预算。
- **玩家给意图，AI 执行路径**：允许目标政策、房间技能许可、有限主动落点和阶段换位，不必开放逐单位微操路线。
- **战中资源跨区域预算**：medkit、grenade、breach charge 在早区省下才有后区答案；“允许使用”与“必须立即使用”要分开。
- **防御分层可创造转换**：Armor 独立于 Health，Medic 可消耗 medkit 生成临时 ballistic Armor，Vanguard 又可读取 Heal→Armor；但数值/次数必须有 section 和持有者边界。
- **功能位按敌情替换**：无门撤 Breacher、长距增 Marksman、密集房增 Pointman，比固定职业羁绊更接近可读的能力试卷。
- **成长时间与任务期限共同定价**：额外训练周也是工资成本；强力 recruit 太早拿可能成为白象。
- **复盘按来源和区域展开**：显示谁吸收 Armor、谁被 focus、哪个政策改变目标、哪个手动命令解决问题，而不是一个 Power 总分。

### 不可直接迁移

- 不移植 PMC 工资、CIA/Cartel 关系、派遣、道德事件、三时段个人训练或永久伤病/死亡骨架。
- 不把四人队上限、职业名称、枪械目标规则、房间串联地图或十任务 Freeplay 当成本项目权威。
- 不移植不限次数的房间政策表；本项目仍固定两条战术指令、三点共享资源和明确 cost/cooldown/use-limit。
- 不因 PMM 无逐路径控制，就削弱本项目已经确认的格子部署、导航和战中有限干预。
- 不复制历史 Shooting/Mental/Armor 数值收敛；属性、装备和职业必须各自保留反制与机会成本。
- 不从商业停更推断玩法价值或失败原因。

## 未决问题

- 五职业、武器、装备、perk/Tac. Skill、tier、训练和 mission modifier 的完整最终数据库未找到。
- 自动路径、掩体评分、编号仇恨、武器目标优先和 simultaneous kill/medkit 顺序无官方规范。
- 手雷/medkit 的最终主动/自动边界、取消/失败消耗与 cooldown 未公开。
- 现有构筑与数值主要来自 2025 单帖；无采用率、胜率或正式版 meta。
- 真实战斗复盘的完整字段和模拟战差异比较从未正式补齐。

## Disposition

`retained`

Private Military Manager 超过普通长尾门槛：官方规则/开发/补丁链、七份长评和八个完整讨论共同建立四人队、三套具体编制、房间级命令、任务期限、装备/成长/替换、敌情适应、失败归因和十个生命周期案例。它不升 anchor：游戏永久停更且未完成，实践证据集中于 Steam 单帖，缺完整规则库、统计、正式攻略与行动顺序。
