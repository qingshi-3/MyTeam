# Survivor Mercs

## 身份、状态与研究深度

- `title_id`: `survivor-mercs`
- 开发：Wolpertinger Games；发行：Wandering Wizard。
- Steam App：`2141520`。
- Early Access：2023-09-14；正式版：2026-04-30。
- 当前可核实主分支：`1.2.1`（2026-06-17）；`1.2` 发布于 2026-06-08。
- 分类：adjacent-long-tail；survivors-like / twin-stick Commander + 自动 Merc 小队 + extraction roguelite。
- 研究深度：adaptive-depth retained。游戏与本项目相邻的价值不在玩家持续瞄准，而在 Merc 独立 AI、间接小队控制、双轨成长、Gear/Trait owner、撤离风险和敌人空间包。

版本与模式必须分开：Standard Operations、解锁后的 Elite Operations、One-Man Army Trait 变体，以及最多五人的 hotseat / Remote Play co-op 不是同一战斗契约。co-op 中主玩家固定控制 Commander，其他玩家只能在相应 Merc 已招入队后接管；它不能证明单人模式存在逐 Merc 直接操作。正式版下架免费 Demo，但官方明确 Demo 与 EA save 可延续到 1.0。

## 检索日志与停止理由

- 官方：97/97 Steam News 标题已扫；深读 `0.9.8`、`0.9.9`、`0.9.11`、`0.10`、`0.11`、`0.15`、`1.0`、`1.2`、`1.2.1`。唯一公开 Guide 1/1 完整可读并于 1.0 日更新。
- 社区：111/111 公共 Steam Discussions 标题已抓取；深读 Shield、no-hit、Hail、Merc 控制、Gear pool、Survivor Bonus、extraction economy、Armor/grind 与 resource distribution 九个机制帖。
- Reviews：Steam API 176/176 全量抓取并按 Merc、Gear、Priest、Shield、specialization、grind、boss、aim、formation 等关键词筛查；保留五篇能改变规则、实践或生命周期理解的长短评。
- 视频：五组 YouTube 查询得到十个去重候选；六个页面显示 caption track，但 timedtext 均返回 HTTP 200 空体。无字幕构筑标题、缩略图、伤害数字和画面不作证据。
- 外部 written route：Google 返回 challenge shell；Bing 精确标题搜索被同名 Survivor 噪声占据；DuckDuckGo HTML 返回 202 无结果体；Brave 429。未绕过验证，也未把 search snippet、镜像、商店 copy 或同名结果登记为来源。
- 未购买、下载、反编译游戏或 mod；未从截图推断技能树、Gear 数值和 squad cap。

最终注册 24 个实质来源：十个官方规则/发布/补丁节点、九个实质机制讨论、五个详细玩家评测。继续搜索只重复“Merc 自动输出、Commander 主动走位/瞄准、长期 grind、AI/可读性”或提供无字幕 build 标题，无法补成正式版 Merc×Merc 配对表、逐 Gear owner 表或 current meta，因此按 Adaptive Depth Protocol 停止。

## 来源包

| source id | 类型 / 时期 | 主要用途 | 关键限制 |
|---|---|---|---|
| `src-sm-guide-new-player` | 官方 strategy guide / 更新至 1.0 | 当前循环、Trait/Merc/Gear、目标、撤离、Survivor Bonus、co-op | 无完整 synergy pair、Gear 数值与当前 meta |
| `src-sm-official-hotfix-1-2-1` | 官方 patch / 1.2.1 | 当前版本锚点 | 只作 hotfix 边界 |
| `src-sm-official-update-1-2` | 官方 patch / 1.2 | mini-boss、Merc facing、DoT、Gear/mission/retreat 可读性 | 无完整 composition |
| `src-sm-official-release-1-0` | 官方发布 / 1.0 | 正式版、Demo/save、新地图 | 发布介绍不证明强度 |
| `src-sm-official-final-drill-0-15` | 官方 patch / 0.15 | Merc synergy、四 Gear、双成长、地图/Complication | 1.0 前最后 EA 节点 |
| `src-sm-official-terminal-0-11` | 官方 patch / 0.11 | Base Attributes、auto-aim 边界、Hail、Riot Shield | 历史数值不可跨版 |
| `src-sm-official-strike-squad-0-10` | 官方 patch / 0.10 | Gear rarity、Boss phase UI、Complication | 部分 Gear 当时未完成 synergy 接线 |
| `src-sm-official-arsenal-0-9-11` | 官方 patch / 0.9.11 | Weapon/Trait/Dash owner、Mark、Shield、Armor decay | Airstrike/SMG 同文有已知 bug |
| `src-sm-official-evolution-0-9-9` | 官方 patch / 0.9.9 | Trait 分类、Armor/Piercing、旧 save 迁移 | 旧 Commander/Trait 被清除 |
| `src-sm-official-operation-overhaul-0-9-8` | 官方 patch / 0.9.8 | 敌人地图包、Hail/Priest、撤离/稀有 loot | 后续继续重做 |
| `src-sm-discussion-shield-op` | 社区+开发者 / 0.10 | Shield 默认化风险、重弹 knockback、信息负荷 | `OP` 非统计 |
| `src-sm-discussion-no-hit-shield` | 社区+开发者 / 0.9.9 | 角度绕盾、hit-invulnerability、一次命中盾 | 挑战与旧 hitbox |
| `src-sm-discussion-mercs-indirect-control` | 社区+开发者 / 0.9.7 | Merc 独立 AI 武器、间接控制、One-Man Army | synergy 当时未完整实现 |
| `src-sm-discussion-hail-rework` | 社区+开发者 / 首发 | Hail 后射与前进/拾取冲突 | 已被后续重做 |
| `src-sm-discussion-gear-pool-dilution` | 社区+开发者 / 首发 | 解锁稀释、排除与 reroll | 无当前概率/价格 |
| `src-sm-discussion-survivor-bonus` | 社区+开发者 / 首发 | 旧 10 level-ups 条件、死亡清空、说明缺口 | 当前改为专精 level 6 |
| `src-sm-discussion-extraction-economy` | 社区+开发者 / 首发 | Priest+Stim Pack、早撤最优、owner wording | 唯一完整 build 为历史版 |
| `src-sm-discussion-armor-grind` | 社区+开发者 / 0.9.9 | Armor/Piercing、脆弱与撤离意图 | 旧参考值非当前公式 |
| `src-sm-discussion-resource-distribution` | 社区+开发者 / 0.10 | BD 过剩、DNA/Steel 稀缺 | 无当前资源表 |
| `src-sm-review-current-merc-output-229541321` | 详细评测 / 1.2.1 后 | 当前 Merc 输出/Commander buff 结构 | 0.8h；Commander 概括过宽 |
| `src-sm-review-formal-miniboss-226023815` | 详细评测 / 1.0→1.2 | mini-boss sponginess 修复交叉 | 简评无 build |
| `src-sm-review-chinese-route-200387088` | 详细评测 / 0.10–0.11 | 分支路线、队伍继承、早招 Merc、手动开火 | 3.1h 历史结构 |
| `src-sm-review-current-agency-231203739` | 详细评测 / 1.2.1 后 | Trait 开局 agency 与 POI 重复 | 个案非统计 |
| `src-sm-review-specialization-bait-198414835` | 详细评测 / 0.10 | 专精 bait、Gear 窄池、grind/reroll 压力 | 未具名，后续已重做 |

## 当前基础循环与真实决策

玩家先选 Commander clone、地图和 Operation Type。Commander 由三个 Traits、主动 Weapon/能力与通常存在的 Dash 构成。进入 Stage 后先占 Beacon 招第一名 Merc；击杀敌人掉 Dogtags，Commander 获得 XP 并从常规升级池强化小队。地图目标随时间生成：Beacon 招 Merc，Supply Drop 给 XP/SP/治疗/磁铁，Loot Crate 与 Vault 给局外资源，Weapon Depot 给临时 powerup，Gear Crate 选 Gear，Extraction Point 决定是否安全带走收益。每过一 Stage，Danger 增加，Complication 或分支节点改变后续风险。

Merc 有各自攻击、索敌、移动模式。每名 Merc 同时存在普通 level-up 与 Stage 间消耗 SP 的 specialization 两条成长；specialization 路径互斥并通向独立 elite skill。`0.15` 才正式加入特定 Merc×Merc 组合的 synergy upgrade，并把 Gear slot 从三提高到四。当前 Guide 说每名 Merc 与三至四名其他 Merc 有可能出现 synergy，但公开文本没有 pair/effect 全表。

玩家真正做的是：先补战力还是跑资源目标；是否为某 Merc 投入普通升级或 SP 专精；选择哪条互斥专精；Gear 的 bonus/tradeoff 是否与 Trait/Weapon/Merc owner 相符；何时 reroll、何时排除池中内容；面对下一地图/mini-boss/Complication 是继续深跑、转 survival/counter，还是安全撤离。死亡会失去全部 Components 和大部分 DNA/BD；撤离或胜利保存 loot 与 Merc Survivor Bonus，因而“能继续打”与“应继续打”不是同一问题。

## Commander、Merc、Weapon、Trait 与 Gear owner

Commander 由玩家直接移动、Dash、瞄准和触发 Weapon。Merc 才是具有独立身体的自动攻击者：开发者将其比作“有自己的移动、targeting、attack 的武器”，玩家用 Commander 位置、移动方向、Mark 和少数主动能力间接引导。`0.9.11` 将 Dash、Trait、Weapon 从旧混合能力拆开；Weapon 的主要目的不是替代 Merc DPS，而是 Mark 集火、推开、聚怪、减 Armor、封路或阻挡。

Trait 也分 owner：Feature 主要改 Commander/core stats；Characteristic 主要改全队与环境，例如伤害类型、攻击模式、治疗；Talent 改 Commander 的主动/被动规则，甚至改变 squad composition。Gear 可给 Commander/全队 bonus/tradeoff，也可拥有独立规则如 Supply Drone；这不等于所有 Gear 都是全队 buff。没有逐件公开表时必须逐项保留 owner，而不能把“Gear 提供额外 bonus”泛化成装备者或全队都吃到。

`0.11` 明确可选 auto-fire/auto-aim，但官方提醒系统仍围绕 active twin-stick shooting 设计，且某些 Weapon 依赖主动时机和 squad control。玩家移动、准星技巧与 Dash 手感不迁移为本项目自走棋规则；可迁移的是 Mark、目标优先、保护形态、自动单位 owner 与“主动命令影响谁”的契约。

## 完整历史构筑：Priest + 可叠 Stim Pack 的深跑 / 撤离线

唯一能同时回到实战描述、规则与风险收益核验的完整构筑来自 EA 首发时期，必须标为历史构筑，不能称作 `1.2` meta。玩家把 runs 分成“前两次招募拿到 Priest”和“拿不到”：早拿 Priest 就深跑、拿更多 loot；拿不到则尽早 eject。开发者追问 Stim Pack 与 Priest 的比较，玩家明确回复 Stim Pack 表现接近且希望二者叠加。同期官方/开发者说明，未特别限定的 damage/crit bonus 作用于 Commander 与全队；当前 Guide 则继续确认 Merc/Gear、撤离与 Survivor Bonus 的结构。

- **engine**：尽早 Beacon 招 Priest；获得 Stim Pack 时继续叠加治疗；Commander 走位维持 Priest aura coverage 并获取目标/掉落。
- **state/resource**：Commander 当前 HP、持续治疗/治疗间隔、可选 Gear 槽、Merc 招募时点、XP/SP、Danger、loot carry、可安全 Retreat 的 Stage 条件、Merc Survivor Bonus。
- **payoff**：治疗把 HP 从不可逆倒计时变成可管理资源，允许多过 Stage、拿晚期稀有 Components/更多 loot，并让存活 Merc 带出下一局免费起始升级。战斗伤害仍由其他 Merc、Commander Weapon 和明确全队 damage/crit modifier 的接受者拥有；Priest/Stim Pack 不自动变成输出。
- **survival**：Priest aura + Stim Pack；后续 Armor、Shield 或 med-kit 可作为替代/补充模块，但原帖未证明固定组合。
- **spatial condition**：Priest 后于 `0.9.8` 围绕 Commander 移动以提供 aura coverage；玩家必须在接目标、避开敌群与保持治疗覆盖之间移动。离开有效 coverage、被包围或遭爆发都会让 sustain 失效。
- **payoff owner**：Priest/Stim Pack 分别拥有治疗来源；Commander HP 拥有生存状态；其他 Merc/Weapon 拥有伤害；Extraction/Survivor Bonus 系统拥有跨局结算。
- **economy / pivot**：前两次 Beacon 未出 Priest时，历史实践选择尽早撤离；可用 Stim Pack 或治疗目标降低依赖。池排除/reroll 允许减少不合适 Merc/Gear，但不能消除全部随机性。能安全撑住后，才值得承担更高 Danger 争取稀有掉落。
- **counter / limit**：爆发、Armor/Piercing不匹配、离开 aura、远程/包围敌人、限时 mini-boss 输出门槛和无法及时安全撤离分别攻击 sustain、space、DPS 与结算。1.2 明确“不保证每套 squad 自动击杀 mini-boss”，所以生存不等于过输出考试。
- **version limit**：Priest、Stim Pack、当时 general modifier wording 与早撤最优来自 2023 EA；0.9.8 后 Priest movement、0.9.9 Armor、0.15 specialization/synergy/Gear slot、1.0/1.2 资源和 Boss 平衡均变化。档案只保留结构，不宣称当前数值、采用率或唯一最优。

## 三个可验证模块，不冒充额外完整 build

### Hail 前方清路 / 360° Covering Fire

首发 Hail 朝 Commander 后方射击，迫使玩家在前往 POI、拾 XP 与让她命中之间反向移动；玩家甚至误认其损坏。`0.9.8` 改成默认沿 Commander 移动方向清路，或走 360° Covering Fire 专精；`0.11` 又让默认攻击优先敌群而不是 Commander；`1.2` 修 Merc 贴身、移动/aim facing。它是“玩家移动方向作为自动单位 formation selector”的完整 lifecycle，但没有 source-backed Gear/其他 Merc/economy，故只算模块。

### Commander Weapon Mark 集火

`0.9.11` 后 Marksman Rifle、Leafblower、Melter Beam、Singularity 等可施加 Mark；合格 Merc 在范围内改变个体 targeting，集中攻击被标记目标。Pistol 在 `0.15` 成为起始 Weapon并在 crit 时可无视 Armor。它提供 source→mark→Merc reader→target owner 链，可回答优先敌群、破 Armor和 mini-boss；但无 current build source 给出固定 squad/Gear，因此不拼装成一套阵容。

### Riot/Defender Shield survival

Riot/Defender Shield 阻挡 projectile/enemy 并提供清路/盾击。no-hit 讨论确认斜角投射物或敌人可绕到盾内，敌人被命中后的短暂无敌会在“推土铲”玩法中穿盾；Priest Crusader elite skill 当时提供一次命中 Shield。后续玩家发现可近乎全程挡 projectile，开发者拒绝增加一套 defense-point 计数负担，选择让重 projectile 产生 knockback，并希望 Shield 不成为默认，保留 tanking、kiting、crowd control 路线。该模块证明 Shield 是受角度、hit state 与位移反制的 uptime 工具，不是天然 damage converter。

## 敌人包、空间与反制

`0.9.8` 把地图 enemy package 分开：Desert 以强硬、重击、近身/包围为主；Arctic 偏远程、保持距离并受压重定位；Waste Land 以高速、编队与 swarm 为主。Mini-boss 借最终 Boss 的攻击模式作为预演，后续又增加 phase Shield UI、攻击充能提示、Armor/衰减图标。1.0 新图强调 toxic AOE，1.2 则重做 mini-boss HP/Armor并延长 30 秒。

因此同一 survival 模块面对不同包并不等价：Shield 对正面 projectile 强，但怕斜角、重弹 knockback 与包围；Priest aura 需要 coverage，但被 AOE/高速群逼迫移动；Mark 集火适合 Armor/mini-boss，却可能让周边 swarm失控；Hail 前向清路与 360 coverage是在移动方向和四周压力间互换。Boss/mini-boss 是输出/控制检查，不允许把“活得久”自动解释成能完成限时击杀。

## 招募、池稀释、双成长与跨局收益

Beacon 与 Gear Crate 分别承担 Merc/Gear 获取，BD 可在 Operation 内 reroll。解锁更多 Merc/Gear 会扩池；早期讨论推动 Bunker/Field Radio 排除机制，当前 Guide 确认可升级 reroll 与排除 Merc/upgrades/Gear，但随机性仍保留。每名 Merc 普通 level-up 与 SP specialization 分开；后者互斥、影响更强。`0.15` 增加 Merc×Merc synergy，并减少专精 tradeoff、简化部分树；这意味着 `0.10` 玩家所谓“每 Merc 只有一条明显好路径、少数 Gear 值得拿”是重做前的失败观察，不能当 current tier list。

Survivor Bonus 是真正跨局风险：2023 开发者说 Merc 存活撤离/胜利且该 run 投入至少十次 level-up，可在下次招入时得到一至三个免费起始升级；当前 2026 Guide 改为 specialization tree 至少 level 6 才得一枚 chevron，最多三枚。两者共同确认“成功撤离带来下局 Merc 起步优势、该 Merc 随队死亡会清空 Bonus”，但触发条件已经改变，绝不能平均成一个公式。

## 生命周期与负案例

本 checkpoint 计十二个 materially distinct negative / reworked families：

1. **Hail 空间契约重做**：后射与前进/拾取冲突 → 前方清路/360° → 优先敌群 → 1.2 facing/aim 修复。
2. **Shield 默认化与自然反制**：长时阻挡过强；开发者放弃额外 defense-point UI，采用重弹 knockback并保留角度/包围 counter。
3. **主动能力 owner 拆分**：Dash、Trait、Weapon 从混合系统拆开，Weapon 专注 Mark/space/control；旧 save 有重置/备份边界。
4. **Merc class → Base Attributes**：0.11 移除旧 class，改 Combat/Technology/Tactics/Resilience/Devastation/Luck；旧 build 数值失效。
5. **双成长与 synergy 重整**：普通 level-up、SP specialization、Merc×Merc synergy 分开；0.15 减 tradeoff、简树并提高影响。
6. **Gear 稀有度/升级/slot lifecycle**：0.9.8 level 1 无 tradeoff、升级扩大 bonus/tradeoff；0.10 改 rarity 且部分 synergy 未接；0.11 common 可 proc；0.15 又支持四槽与 level-up 升级。
7. **早撤最优经济修正**：固定 loot/carry 让第一 Boss 后撤成为重复最优；后续加入晚期稀有材料、地图差异、Vault、Grave、loot-run/boss-run 取舍。
8. **Survivor Bonus 说明与条件变化**：旧 UI 不解释十次升级规则；当前 Guide 改为专精 level 6，Retreat/Abort 直到 1.2 才明确分开视觉语义。
9. **mini-boss sponge / 限时门槛**：正式版玩家批评 sponge；1.2 重做 HP/Armor并延长 30 秒，长时玩家更新评测确认改善。
10. **mission/unlock/progression tracker 事务**：1.2 修 prerequisite wording、负进度、旧 tracker 与多项 unlock；说明内容可用性不能只靠隐藏条件。
11. **Merc combat resolution / attribution**：Spark 反向射击、Silverback movement/attack、DoT tick、Shredder shots-per-attack、lingering/AOE/Crit modifier 与 Gear modifier 显示均在 1.2 修复。
12. **资源分布与 build agency**：BD 过剩、DNA/Steel 稀缺和难度跳变推动资源/成本重平衡；正式版仍有玩家报告 Trait 开局 agency 与 POI 重复，现状保留为个案而非普遍结论。

Boss phase Shield UI 与早期 Mammoth invulnerability 讨论有价值，但公开链无法证明 bug、合法 phase gate 与后续显示三者的精确 current 关系，未另计 lifecycle。一般性能、设置和小型 UI 症状也不为追数量拆分。Cumulative explicit negative/reworked cases：206。

## 对本项目可迁移

- **Merc AI owner 可迁移，twin-stick 技巧不可迁移**：把每名英雄的 movement/targeting/attack 当独立契约；玩家战术命令可提供 Mark、regroup、guard 或 focus，而不是复制 Commander 持续移动/瞄准。
- **生存线必须回答输出 owner**：Priest/Stim Pack让队伍有时间继续作战，真正 damage 仍由具体 Merc/Weapon/reader承担。若盾体系没有显式 converter，就应通过保护射手/法师输出，而不是默认 Shield 自带伤害。
- **Shield 与元素正交**：Ice Shield、Earth Shield可以共享生成、吸收、break、refresh 的基础 Shield 语义；元素只应改变 supplier、reader、反应、抗性或 counter。名称不能自动改变人口或 damage owner。
- **防御转输出必须是有槽位的 converter**：若遗物读取全队额外 Health/Defense 并给某 carry 法强/攻击，必须声明 read scope、recipient、slot cost、snapshot/refresh、cap、source lineage 与 anti-heal/shield break/backline/dispelling 等 counter。
- **Merc×Merc synergy 不能只写 pair tag**：触发候选、出现窗口、是否占普通升级、recipient、失去成员后的状态与替换成本都要可见；否则 pool expansion 只会稀释可用 offer。
- **撤离把 survival 变成经济 payoff**：安全退与继续贪资源需要明确 Danger、稀有掉落、可撤条件、当前 carry 和永久损失预览，不能依赖玩家记忆隐藏规则。
- **敌人包比单个 Boss 更适合检验体系**：近身包围、远程重定位、高速 swarm、Armor/重 projectile/AOE 应分别击中 space、targeting、Shield、sustain、focus-fire 等不同链接。
- **报告保留 owner 和版本**：治疗者、Shield supplier、Mark source、Merc reader、final damage、Armor decay、撤离结算和 Survivor Bonus必须分别归因；规则变化后 UI/档案要显示版本，不让旧 Guide 数值复活。

## 不兼容与未决

- 本项目是固定格的自动战斗、有限独立战术命令；不复制持续自由移动、双摇杆准星、POI跑图、爆桶踢击或 Commander 不可替换的层级。
- 当前正式版确切 Merc 上限没有公开规则正文；co-op 最多五人只证明控制席位，不单独证明所有模式 squad cap。2024 review 的“四 Merc”也不能跨版。
- 公开资料未给当前 Merc×Merc 具体 pair/effect、完整 specialization tree、逐 Gear owner/数值、trait pool 概率、reroll/exclusion 价格和精确 Armor/Piercing 公式。
- Priest + Stim Pack 是历史 EA 构筑；当前结构仍存在 Priest、Gear、撤离与 Survivor Bonus，但不能宣称组合在 1.2 仍强。
- Hail、Mark 与 Shield 各自闭合模块/lifecycle，但没有同一实战来源把它们与具体 Merc/Gear/经济组成一套 current build；档案拒绝人工拼接。
- Standard、Elite、One-Man Army 与 co-op 强度、控制和掉落不可混写。
- 当前正式主分支停在明确 `1.2.1`；未来更新、可读 synergy 数据库、带正文的 1.2 build guide 或非空字幕出现时应重开 current-build 审计。

## Disposition

`retained`。

保留理由：24 个实质来源跨官方 current guide、正式发布、八个关键版本节点、九个开发者参与的机制讨论与五篇实战评测。历史 Priest + Stim Pack 深跑线闭合 engine、state/resource、payoff、survival、space、owner、economy、pivot 与 counter；Hail、Mark、Shield分别补足自动单位空间控制与自然反制。更重要的是，资料能严格分离玩家直接操作与 Merc 自动战斗，提供对本项目有用而不误迁移的 owner/formation/extraction/enemy-package 证据。

停止理由：current `1.2` 没有可读的具名完整 squad、Merc×Merc pair表或逐 Gear owner 数据。继续搜索只增加标题、无字幕画面和重复评论，不能诚实补齐。档案以版本化历史 build 达到 retained 门槛，同时明确 current-build gap，不把旧强度写成现状。
