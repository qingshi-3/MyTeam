# Neon Auto Party

## 身份、时期与研究深度

- `title_id`: `neon-auto-party`
- 开发 / 发行者为 Pew Times Three。Steam main App `2679840` 仍是 Coming Soon / 2026；Demo App `3394090` 标记 2025-03-25；Playtest App `2755360` 标记 2024-08-29。商店与 App Details 只用于身份，不进入 deep source。
- 可核验机制限定在 2024 Playtest 至 2025 Demo / 开发版：7 篇 Playtest 官方补丁、1 篇 main-App 0.5.2 开发公告、2 段完整历史实战与 1 个反馈主题。未发行 main App 的未来正式规则、数值和 meta 均未知。
- itch first-party `0.4.4` devlog dated 2025-04-02 与 Steam Playtest `0.4.3` 大量同文，只作发布路线和版本桥审计；不登记第二 source、不配成双源，也不证明 binary 相同。
- 深度：11 个 substantive source、20 条 evidence、7 个去重 negative/reworked 案例。资料闭合一条 2025-04 Demo 完整五单位通关记录与部分可读 build / owner chain，同时保留一次 2024 first-Playtest 自动目标失败，以及 Rank / deck、Offering、Fate、Stress、Artifact、失败成长与可读性演化。
- disposition：`retained`，不是 anchor。它是 2024 Playtest → 2025 Demo / 开发版的历史长尾样本，不是未发行 main App 的 current rules/meta。

## 来源包

| 功能 | 数量 | 来源 |
| --- | ---: | --- |
| 官方版本 / 规则节点 | 8 | Update 1、0.2.0、Small Fixes、0.3.1、0.4.1、0.4.2、0.4.3、main-App 0.5.2 |
| 完整实战 | 2 | 2025-04-03 Demo clear 45:41；2024-10 first Playtest failure 56:14 |
| 社区 / 开发者反馈 | 1 | mail2ajr 主题、两名独立玩家与开发者回复 |

全部 11 个来源均被本档案 20 条 evidence 使用。Steam external-post gid 与 canonical event gid 是两套编号；本库只使用 source index 登记的 canonical URL。视频只使用完整观看后可读的解说、界面和人工时间戳动作链，不从其余短片、无字幕视频或缩略图补机制。

## 版本地图与禁止外推

| 时期 | 可核验变化 | 禁止外推 |
| --- | --- | --- |
| 2024-10 first Playtest / Update 1 | 自动战斗边界、重复单位升 Expertise / Rank 并加卡、每次升级所需单位数封顶为 3、使 Rank 4 更可达、item Expertise、end-turn 分单位处理、tooltip / skill-tree 反馈 | 不是 Rank 上限 3；不作为 2025 Demo 或未发行 main App 的 current formula |
| Playtest 0.2.0–0.3.1 | Offering、Fate viewer、Stress 显示边界、buff tooltip / Fate save、一次性 tutorial 与 performance | viewer 不等于选择前完全预览；普通 tutorial / QoL 不单独计负面案例 |
| Playtest 0.4.1 | Machinist 在战斗中牺牲自己的 arm，并提供一个以未说明方式帮助队伍的 companion；Stress 从多 slider 改为逐 level 解锁并加入敌方 Stress skill，高 Stress 使单位获得更多 XP；actual damage/heal tooltip | companion 的表现、动作 owner、目标、占位、持续、持久性、激活与空间合法性均未知；社区缺技能报告不是成功执行证据 |
| Playtest 0.4.2 | Level 2 下调；Offering 按售出 Rank 缩放；6x / concede；Stun-card 战斗锁修复；合并按 Rank 给 XP；带 Artifact 的 unit 被 combined into 无 Artifact 的 target 时转移给 target | target 已有 Artifact 时来源 Artifact 去向未知；速度 / 认输不构成战中策略命令 |
| Playtest 0.4.3 → Demo | Fate contrast、Buff/Debuff 分行、Rank 轮廓、move transparency、unit lock；Stress 项只精确修复 second level 的 Stress levels 不会 unlock；官方称下周 Demo 基本同当时 Playtest | “基本相同”不证明 exact binary 相同；itch 0.4.4 同文也不能组成独立互证；其他 display 改动不归入 Stress 修复 |
| 2025-04-03 Demo 实战 | 一条完整五单位 run、两次失利后恢复、50 Gold 历史扩格、部分 Artifact / card owner chain、最终 Boss 与通关 | 一次胜利不是胜率 / meta；不证明五个完整职责或第五格必要；数值、卡表与 exact build 不当前化 |
| 2025-06-24 反馈帖开发者回复 | run end 按 cleared waves 给参战单位 XP，包括 failed run | 这是 0.5.2 之前的结算说明；不证明三天后的 scaling 调整效果，也不等于每次 encounter loss 当场发永久 XP |
| 2025-06-27 0.5.2 开发版 | 全单位 skill trees 完整；Demo 前五级 / 首层三技能；Fate 三选一前显示 effects；Rat / Rat King / Rat Catcher offensive stats、Cleave scaling、Rend 的 Bleed / hit damage 与首层若干 waves 下调；XP scaling 调整，并说明若首 run 抵达一个 level 的 latter third，带入的 units 预计获得 level up、帮助 next few runs | 不建立 level-up 的 run-end 发放时点，也不建立 cleared-wave / participating-unit / failed-run 结算公式；敌方 population 未记录为改动项；无 post-0.5.2 实战；不是 Machinist 或 skill refund 专项修复，也不证明调整效果或 main release 规则 |

## 核心循环与真实决策

历史实战与补丁共同显示：玩家在战斗间购买 / 合并单位、提高 Rank / Expertise、配置个人 card deck 与 holder-bound Artifact、调整队伍顺序、使用有限资源或 consumable，并决定是否扩充容量。每个被检查的 holder 显示一个 Artifact，但每单位或全局最大槽数未知。两段录像中的战斗自动结算，未观察到或未暴露战中选技、retarget、换位；0.4.2 只明确记录速度 / 认输控件。first-Playtest 玩家另在该历史样本中报告当时无法 focus，这些观察都不能升级成所有版本 / mode 的 categorical lock。

匹配单位先增加升级进度；取得足够单位并完成 merge / upgrade threshold 后，resulting unit 才获得观察到的 Rank / Expertise / card change。Update 1 把“每次升级所需单位数”封顶为 3，使 Rank 4 更可达；它没有把 Rank 上限设为 3。0.4.2 又让合并按 Rank 给 XP。单次购买不等于立即升级或加卡，只有完成阈值才改变 resulting unit 的属性 / 动作集合；继续买匹配单位仍与其他角色、Artifact、Offering 或容量投资竞争 Gold。

失败必须分成两层。第一层是 encounter loss：扣一条 life，并在同一 run 返回 preparation；两段视频分别展示 first Playtest 重试仍败，以及 Demo 在 `09:40`、`12:40` 两次失利后继续经营并最终通关。第二层只在 run 结束结算：2025-06-24 反馈帖的开发者回复说明按 cleared waves 给参战单位 XP，即使 failed run 也会获得；三天后的 0.5.2 只说明 XP scaling 已调整，并说明若首 run 抵达一个 level 的 latter third，带入的 units 预计获得 level up、帮助 `next few runs`，但没有重述 run-end 发放时点。这不等于每次扣 life 当场发永久 XP，也不证明新曲线有效或故意失败有利。

## 2025-04 Demo 完整实战记录：五单位容量与部分 owner chain

视频发布于 2025-04-03，晚于公开 Demo、早于 0.5.2；精确 binary 未知。以下只记录同一 run 中可核验的行动和界面。

- **opening / engine**：开局为两名 Tactician 加 Pyromaniac。玩家合并 Tactician，提高 Rank / Expertise 并加卡，随后加入 Super Soldier 与 Nano Medic。Tactician 持 Enchanted Blades，承担可见物理 / 暴击输出，但最终 card table 未闭合。
- **state/resource**：Gold、剩余生命、匹配单位 / upgrade threshold、Rank / Expertise、每名单位的 card deck、可观察的多个 Artifact–holder bindings、队伍顺序、容量与 upcoming waves。`09:40` 和 `12:40` 两次失败各消耗生命，但 run 继续；槽数上限未知。
- **capacity pivot**：玩家花费历史快照中的 50 Gold 扩第五格并招募 Enforced Zealot。这个动作把 Gold 从现有单位升级转向容量与新单位；50 Gold 只属于该视频，不写成 current cost，也不证明第五格或 Zealot 是通关必要原因。
- **final party**：Pyromaniac / Super Soldier / Tactician / Enforced Zealot / Nano Medic。`45:12` 击杀 Baron of Sewage，`45:27` 出现通关。一次胜利只闭合这条 run，不形成胜率或 meta。
- **survival**：Vest 向 Enforced Zealot 供应 Dodge，Zealot card 显式读取 Dodge 为 healing / From the Shadows；Boss 段约 `300 Barrier` 只是一张 holder 状态快照，来源未闭合。Personal Barrier 向 Nano Medic 施加开战 Barrier，Medic card 另行拥有 healing / Refresh。Super Soldier 的完整 card / item 职责没有被可靠读出，不补写。
- **spatial condition**：第五容量是硬空间条件；队伍顺序还会影响“其后两人”等效果和治疗 / 目标行为。Celestial Strength 可见地影响其后两人，玩家为此调位，但 ASR 不足以唯一确认施放者，因此只保留效果范围与调整动作。
- **payoff owner**：Pyromaniac / Burn card 拥有魔法与 DoT damage；Lifesteal effect 读取 damage 并产生转换，holder 接收 healing。Tactician / card 的部分物理 / 暴击输出可读，但最终 deck 未闭合；Vest 只向 Zealot 供应 Dodge，Zealot card 才拥有 Dodge→heal→From the Shadows；Personal Barrier 向 Medic 施加开战 Barrier，Medic card 拥有治疗 / Refresh。
- **counter / failure**：本次通关没有闭合某个针对最终五单位的敌方 counter，也没有比较不买第五格的结果。项目侧失败报告可把 life loss、容量 / 招募时点、holder 死亡、自动目标、卡牌未发动和 Boss resolution 分开，而不能把这些建议反写成原作因果。
- **version context**：这是一条 pre-0.5.2 Demo 构筑；main App 未发行，没有 current card database 或 patch-level build continuity。

## Pyromaniac：Shard 供应属性，Burn 拥有伤害

Pyromaniac 持 Shard of N'Kai。视频中的历史 Epic 快照显示 `+14 Magic Damage / +18 Expertise / +10 Speed`；玩家把 Expertise 读作使 DoT / HoT 约为 134%，并观察 Burn damage 可通过 Lifesteal 回血。

- Shard 是 holder stat supplier，不是 Burn damage event owner。
- Pyromaniac 与具体 card 负责施加 Burn 并拥有 tick damage；独立的 Lifesteal effect 读取该 damage 并产生转换，同一 holder 接收 healing。不能把 Lifesteal healing 也归给 Pyromaniac / Burn card。
- 这是一条元素 / 状态输出路线，不是 Shield、Barrier 或 Dodge 体系。原作没有在该 run 实测对应 counter；清除、爆发、后排访问或缩短战斗只是在项目存在相应规则时可考虑的候选 counter。
- 历史数值、134% 读法和当前 item/card continuity 均未知，不能复制为项目公式或 current recommendation。

## Enforced Zealot：Vest 的 Dodge 通过显式 card 桥成生存与输出

Enforced Zealot 持 Epic Plane Shifting Vest。Vest 向 holder 供应 Dodge；Zealot / card 再读取成功 Dodge，触发 healing 与 From the Shadows。Boss 段约 `300 Barrier / +80% damage` 只是一局的瞬时 holder 界面快照，其中 Barrier 的来源不能从现有证据强判。

- **supplier**：Plane Shifting Vest → holder Dodge。
- **reader / recipient**：Enforced Zealot 的 card 读取 Dodge；heal 与 From the Shadows 归 Zealot / 对应 card，而不是 Vest 全队广播。约 300 Barrier 只记录为 holder 状态，来源未决。
- **space**：录像中 Zealot 处于可观察的 exposure / order，但 required role / position 与自动 targeting 公式均未知。
- **counter**：原作没有实测这条 owner chain 的 counter；命中修正、非攻击伤害、启动前爆发和后排访问都只是项目侧、仅当对应规则存在时的候选假设。
- **边界**：这说明装备可以把 Dodge 输入桥成 self-heal / output amplification，但只因为有显式 reader；不能外推为所有防御自动转伤，也不能由约 300 Barrier 快照补写来源。

## Nano Medic：Personal Barrier 与团队治疗分账

Nano Medic 持 Personal Barrier，历史文本显示它在开战时向 holder 施加 `+100 Barrier`，Medic 是该状态的接收者；Medic 的 healing / Refresh 由自己的 card 负责。first Playtest 的另一段历史实践观察到治疗倾向第一位置，但它不是同一 Medic build，也不足以确定通用 target priority。

设计上必须分别报告开战 Barrier 的来源 / holder、治疗卡来源、实际 target 与 Refresh；否则玩家无法判断是 Artifact 没生效、Medic 被绕后、治疗选错目标还是 card 未执行。`+100` 只属于历史 Demo 快照。

## Fate、The Chariot / The Empress 与选择信息

first Playtest 只观察到一个历史 Fate 交换整套 card library，不能据此推成所有 Fate 的通则。0.2.0 加入 viewer，却仍保留记忆负担；一名社区玩家批评盲选和正负效果绑定。0.5.2 明确在三选一之前显示各选项 effects，并由官方说明纯记忆练习不有趣 / 不公平。

Demo run 中 The Chariot 提供全场 `+40 Haste`；The Empress 按 Rank 提供 Celestial 或 Bleed。玩家计算生命增幅后仍选择升 Rank，证明这是一次历史 Fate / Rank 取舍，而不是数值最优定律。是否能 decline、是否能保留当前 deck、正负效果是否仍绑定、Fate 结果影响哪个 owner 范围都未闭合；没有 post-0.5.2 实战证明 preview 已解决决策质量。

## Offering：出售与后续一波效果是两个阶段

0.2.0 的顺序是：先出售 unit，获得 sale proceeds 和一个 Offering；然后再独立使用 Offering，削弱指定 wave。0.4.2 才增加“Offering 强度按售出 Rank 缩放”。出售会移除该 unit / body 与个人 deck ownership，但来源没有说明它当时是否部署或占位，也完全没有说明所持 Artifact 的销毁、转移、返还或是否参与强度。

登记的 Demo 视频只提供一般 economy context，没有实际展示 Offering。因而“预判难关后牺牲长期资产”的玩法属于项目设计推论，不是已观察到的原作 strategy / meta。报告应把 sale proceeds、被移除的 unit / Rank / deck、Offering 的创建、后续 target wave / effect 分开；Artifact 去向必须保持 unknown。

## Artifact、合并与 consumable

- 已登记视频把 Artifact 绑定到具体 holder；每个被检查的 holder 显示一个 Artifact，但每单位 / 全局最大槽数未知。不同 Artifact 分别支撑 Pyromaniac、Zealot、Medic 等接收者，而不是匿名全队战力；holder 的位置只影响其暴露 / 职责，不证明 Artifact reach rule。
- 0.4.2 的精确条件是：一个带 Artifact 的 unit 被 combined into 另一个没有 Artifact 的 target unit，Artifact 转给 target。target 已有 Artifact 时来源 Artifact 去向未知，不能泛化成“result empty slot”或任意参与者，也不能假设可提前移动或已有 conflict UI。
- Demo 同时存在 merge 和 holder，但视频没有实测 Artifact inheritance；实践只提供 transaction context，官方公告才是继承规则来源。
- 社区玩家描述的最低难度路线包含购买、升级、排序、少量 reroll 与使用 consumable，但没有可核验名称 / 公式 / 最终 build；只证明 consumable 是一次准备决策，不补写具体效果。

## Stress：从密集配置改为渐进挑战

Stress 的版本链是一族重做：早期限制显示，随后从多个 slider 改为按 level 解锁，加入 enemy Stress skill，并让单位在更高 Stress 下获得更多 XP；0.4.3 的 Stress 项只精确修复 second level 的 Stress levels 不会 unlock。其他 display 变化属于独立 UI cluster。XP 接收者是单位，来源没有给 reward multiplier，也没有把它归给 account / run。

没有独立实战证明重做成功；后续 unlock 缺陷也并入同一 Stress family，不重复计数。若迁移到项目，应逐步展示 enemy modifier、奖励和可达反制，不能只给一个模糊难度数字。

## 自动目标、队形与 first-Playtest 失败

first Playtest played 2024-10-19、published 2024-10-24，exact build unknown。玩家能在战前买人、完成合并 / 加卡、交换 Fate、调整排序；encounter loss 后回到同一 run preparation 继续调整。该视频不证明永久 XP 的 run-end 结算。录像战斗自动结算，未观察到或未暴露战中选技、retarget 或换位；玩家只在这一历史样本中报告当时没有可用的 focus option。

一组敌方 ranged unit 绕过 / 穿过预期前排攻击后排，玩家无法 focus fire，重试后仍败。这个样本只证明一次 bounded targeting / formation failure：前排耐久并未保护被 resolver 绕过的 owner。它不证明 current targeting、普遍难度或后来 Demo 五单位 party 解决了同一敌人。

失败解释需要给出敌方 target acquisition、路径、被击中的 holder、玩家在该样本中为何没有可用的 focus option，以及重试前后排序 / 升级差异。对于本项目，自动战斗不应让“前排为何没挡住”成为不可追溯结果。

## 七个去重 negative / reworked 案例

1. Rank upgrade-cost compression：每次升级所需单位数封顶为 3，使 Rank 4 更可达；不是 Rank 上限 3。
2. Fate blind-choice / memory：viewer、保存 / contrast 与最终三选一前显示 effects 合为一族；正负绑定未证实解决。
3. Stress accessibility / configuration：延后显示、多 slider 改逐关解锁、敌方 Stress skill / 单位 XP 和 unlock bug 合为一族。
4. Actual-output / UI readability：actual damage / heal tooltip 是锚；Buff / Debuff 分行、Rank 轮廓、move 与 Lock control 迁移只作同族 UI 迭代，Lock 对象 / 持续 / 阻止操作未知。
5. Early difficulty tuning：Level 2 与后来 Level 1 的 Rat、Cleave、Rend、waves 压力下调合为一族，不补能力不足或稳定 Rank 不可达的因果。
6. Failed-run meta-growth feedback：encounter loss 扣 life 并回同局 preparation；2025-06-24 反馈帖开发者回复说明 run end 才按 cleared waves 给参战单位 XP，包括 failed run；0.5.2 只补充调整后的 scaling 预期。
7. Community-reported skill-tree zero-value refund / integrity exploit：一名玩家报告右键点击显示投资值为 0 的技能仍增加点数；只与 eligibility / lower-bound failure 相符，不证明实现根因、复现、修复或 current persistence。

累计 negative/reworked 从 341 增至 348。Offering 不是 negative；end-turn freeze、Stun lock、Artifact transfer、普通 tutorial / QoL、ranged targeting failure、Machinist、容量购买和 Boss 都是独立证据 / 边界，不额外计数。

## 社区观察与官方边界

- Johnny Thunder 报告 Machinist 没有技能、右键点击显示投资值为 0 的 skill 仍会增加点数、Sighted 最终只显示 40；开发者只说调查。Machinist 报告是缺陷 / 版本反证，不证明 companion effect 成功；skill refund 只是一名玩家报告，没有官方复现、根因、修复或 current persistence 证据。
- megazver 报告多次最低难度失败，重装后到 12/12，并称首次升到 2 级后进展明显；可见动作只包括买人、升级、排序、少量 reroll 和 consumable。它不是完整 build、最优路线、普遍难度或重装因果证据。
- Fate 的盲选 / 正负绑定批评只是一名玩家观察。官方后续只证明三选一前显示 effects，不证明可 decline、保留 current deck、每种绑定被移除或 effect owner 范围。
- 一帖共六条回复只包含两名独立玩家加开发者，不能把回复数当样本数或统计。

## Shield、Barrier、Dodge、Burn 与输出边界

- Burn 是 Pyromaniac / card 拥有的 DoT；Shard 只向 holder 供应 Magic Damage、Expertise 与 Speed。
- Barrier 是 holder 生存状态：Personal Barrier 在开战向 Medic 施加 Barrier；Zealot 的约 300 Barrier 只是一张 holder 快照，现有证据不闭合其来源。
- Dodge 由 Plane Shifting Vest 供应给 Zealot，再由 Zealot card 显式读取为 healing / damage amplification。
- Medic healing、Lifesteal effect 产生且由 Pyromaniac holder 接收的 healing、Zealot self-heal 和 Barrier 不是同一恢复事件，也不与 Burn 组成一条元素盾反应。
- 没有 max-Health damage、Defense / HP-to-offense、team-stat-to-carry、Ice / Earth Shield 或 current card database。Celestial Strength 影响其后两人，但施放者不能凭 ASR 强判，更不能升级为全队属性转单核公式。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

- Steam identity：main `2679840`、Demo `3394090`、Playtest `2755360` 均核验；Reviews 各为 0。商店 / App Details 只作身份。
- Steam content：官方版本节点按 canonical event URL 登记。General Discussions 共三帖，只有一个 gameplay feedback；另两帖是 Steam Deck 技术问题。Guides、Curators 与用户媒体为 0。
- itch：first-party 0.4.4 devlog 与 Steam 0.4.3 大量同文；comments 为 0。为避免同文重复，不登记第二 source。
- 视频：十段候选逐一审核，仅两段长视频具有足够可读解说 / 界面和完整时间戳动作链；其余过短、无字幕或版本不明，不从标题 / 缩略图补规则。
- 社区：未进入 Discord，未绕过 Reddit 或其他访问限制。没有用 inaccessible post、搜索摘要、商店页或无字幕片段凑双源。
- 饱和判断：现有包已闭合一条完整五单位通关记录与部分 build / owner chain、一次自动目标失败、三个 meta-system 和七个 negative / reworked family；继续路线仍不能补出 post-0.5.2 practice、完整 current card / unit / Artifact database、main release rules 或统计，因此停止于 retained、非 anchor。

## 可迁移与不可迁移

可迁移：

- 将重复单位同时视为纵向 Rank 与横向 card-access 成长，并显示合并后失去的阵容灵活性。
- 原作只提供“出售→proceeds + Offering→后续独立指定 wave”的规则；把它设计成可预判的塔层反制属于项目侧待验证方案，必须显示两个 trigger、目标、持续和替换成本。
- Fate / 整包规则改写必须在确认前显示完整 before / after，而不是考玩家记忆。
- Artifact、holder、card reader 和最终 output 各自保留 owner；merge 需要原子化 item-lineage / conflict 规则。
- Barrier、Dodge、healing、Burn、Lifesteal 分账；只有显式 Vest→Dodge→Zealot reader 才能桥接防御到自疗 / 输出。
- 自动 targeting、失败 XP 与 Stress 都要把原因和下一次可做的新决策暴露出来。
- 原作只证明 end-turn effects 改为逐 unit 触发 / resolution order 以尝试解决同时触发冻结，以及 Stun-card bug 会锁战斗使单位无法继续；内部 queue 未知。项目若采用 queue，才需要另行设计 queue、terminal guard、state machine 与 frame budget。

不可迁移：

- 不复制单位、cards、Fates、Artifacts、Boss、Stress 名称或任何历史数值。
- 不把 2024 Playtest、2025 Demo、itch 0.4.4、0.5.2 开发版和未来 main release 混成同一版本。
- 不把一次通关写成胜率 / meta，不把一次后排失败写成当前普遍 targeting 规则。
- 不把同文 itch / Steam 公告当独立互证，不从无字幕视频、商店文案或社区回复数构造共识。
- 不声称 Artifact 双持冲突、skill refund、Machinist、Fate 绑定或 0.5.2 调整已有确定 current 结果。
- 不把 Burn、Barrier、Dodge、healing 合并为元素盾，也不把项目自创 defense-to-offense / team-stat-to-carry 归因于原作。

## 未决问题

- main App 的正式发布日期、release build、完整 unit / card / Fate / Artifact / consumable database 与 current rules。
- Demo `3394090`、Playtest `2755360`、Steam 0.4.3 与 itch 0.4.4 的 exact binary / content correspondence。
- 当前 unit Rank、Expertise、card addition、merge XP、sale、Offering、capacity 和 failure XP 数值。
- Artifact 每单位 / 全局槽数、upgrade / inventory，以及带 Artifact source combined into 已有 Artifact target 时的原子处理。
- Pyromaniac、Tactician、Super Soldier、Nano Medic、Enforced Zealot 的完整卡表、targeting、施放顺序与当前 ownership。
- Celestial Strength 的可靠施放者、The Chariot / Empress 当前 Fate 值，以及正负效果绑定是否仍存在。
- Machinist companion 的表现形态、动作 owner、targeting、occupancy、duration、persistence、activation 与 spatial legality；community skill/refund/Sighted reports 的复现与修复状态。
- Stress 重做、Fate preview、early-wave tuning 和 failure XP 在 post-0.5.2 实战中的效果。
- 自动 target / path / healing priority、战斗报告、Stun / end-turn resolution order；内部 queue 结构未知。

## 最终 disposition

`retained`

11 个来源跨八个官方版本节点、两段完整实战和一个社区 / 开发者主题，能闭合一条 pre-0.5.2 Demo 完整五单位 run 与部分 build / owner chain：Tactician 合并 / deck 成长、Pyromaniac + Shard 的 Burn owner、Zealot + Vest 的显式 Dodge reader、Medic + Personal Barrier 的个人生存，以及两次失利后用历史 50 Gold 扩第五格并击败 Baron of Sewage；但不证明五个完整职责或第五格 / Zealot 是胜利必要原因。Offering、Fate、Stress、Artifact transfer、两层失败成长、可读性和 liveness 又提供可迁移的系统边界。

但完整实践只覆盖 first Playtest 和 2025-04 Demo；0.5.2 没有后续实战，main App 仍未发行，current unit/card/item 数据库、target order、数值与统计均缺失。因此保留为 2024 Playtest → 2025 Demo / 开发版历史长尾样本，不升 anchor，不作为 current rules/meta 权威。
