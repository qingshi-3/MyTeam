# Siralim Ultimate

## 身份、范围与研究深度

- `title_id`: `siralim-ultimate`
- 开发 / 发行：Thylacine Studios
- Steam App：1289810
- 类型：六单位回合制队伍构筑、怪物收集与长期地城推进；在本研究中是 `adjacent programmable-combat anchor`，不是 TFT 式免输入自走棋。
- 产品隔离：本 dossier 只研究《Siralim Ultimate》。Siralim 1、2、3 只进入谱系说明，不导入规则或数值；Trial of the Gods 是另一款 CCG，排除。
- 版本主面：以 2025-08-25 已上线的 `2.0` stable family 及能明确标注到 `2.0.x` 的实践为主。2026-01-23 官方宣布跳过 `2.1`、转做 `3.0`，当时只给出六月末 Steam beta 的目标；没有公开发布证据，所以本研究不推断 3.0 规则、数值或当前 meta。
- 深度结论：`anchor-retained`。45 个实质来源跨 `official-patch`、`official-dev`、`maintained-wiki`、`strategy-guide`、`video-transcript`、`community-analysis`、`detailed-review` 七种类型，覆盖七条构筑 / 系统结构、规则所有权、宏的权限边界、敌方反制、版本重做与可读性失败。

本档案采用机制切片，不复制完整生物、trait、spell、perk、artifact 或 relic 目录。研究目标是看清“谁供应状态、谁读取状态、谁拥有输出、哪些规则会截断链条”，而不是把原作内容表搬进项目。

## 来源包

| 组 | 数量 | 主要来源 id | 用途 | 边界 |
|---|---:|---|---|---|
| 官方版本 / 重做 | 11 | `src-su-official-patch-0-4` 至 `src-su-official-patch-3-0-announcement` | 建立 0.4→2.0→3.0 announced 的生命周期，核验 Calm、Macro Editor、Ultra、每个减伤 effect 的 80% 上限、Barrier 与行动上限修复 | 旧版只作 lifecycle；3.0 不 currentize |
| 维护规则 / 数据 | 13 | `src-su-wiki-combat` 至 `src-su-wiki-debuffs`、`src-su-compendium-2-0` | 战斗、融合、buff/debuff、realm、specialization、artifact、spell gem、relic、project、minion 的所有权 | Wiki API 可能出现 Cloudflare / 429 / 403 波动；compendium 是维护表而非官方规则 |
| 实战指南 | 4 | `src-su-guide-bargain-bin-2-0`、`src-su-guide-hellknight-story-lategame`、`src-su-guide-cleric-ri5-1-1-1`、`src-su-guide-specializations-2-0-37` | 闭合 Graveborn、Necromancer、Paladin、Shadowbringer、Cleric、Hellknight 与 Antiquarian 结构 | 单作者方案不等于采用率或唯一最优；历史 Cleric 只作 lifecycle |
| 视频实战 | 4 | `src-su-video-cleric-beginner-2-0`、`src-su-video-warlord-antiquarian-2-0`、`src-su-video-shadowbringer-setup-2-0`、`src-su-video-extreme-stat-gain-2-0` | 补入第二作者的 current Cleric 与 Shadowbringer 实测，闭合具名 Warlord Antiquarian / Relic 路线，并记录高 rank 异常增长 | 逐视频限定版本、测试对象与养成投入；标题、胜局和疑似 bug 都不外推为 meta 或稳定基线 |
| 社区规则 / 失败观察 | 11 | `src-su-discussion-necromancer-2-0` 至 `src-su-discussion-failure-attribution` | 第二作者 Necro、Shadow 独立失败、宏优先级 / 输入、Barrier、Undying softlock 与诊断缺口 | 逐帖限定为个案；不把回答当官方实现 |
| 详细评论 | 2 | `src-su-review-hidden-caps-2026`、`src-su-review-attribution-2025` | 隐藏上限、自动战斗误认、battle history 仍难归因 | 个人评论，不代表总体玩家分布 |

详细 URL、日期、作者、可访问性与限制见 `../source-index.md`。

## 谱系与版本隔离

- Ultimate 的 Early Access 与 0.x patch 只用来解释系统怎样形成：宏编辑、artifact / spell-gem 管理、realm 与 endgame 层在多个节点演化，不能把当时数值当作 2.0 当前规则。
- `1.1.1` 是历史稳定节点。Firlefanz 的 RI5 Cleric 方案明确写在这个时期，只能证明当时 actual healing、maximum-Health headroom、Abnegation 与行动上限的组合风险，不能替代 2.0 实践。
- 2024“恢复开发”公告提出 Calm 等仍处于 ideas phase 的方向；只有 2.0 正式 notes 与 live 公告能证明哪些内容最终落地。
- 2.0 full notes 是本档案的主要官方版本锚。它明确 Calm Mode 降低 attack、cast、resurrect 等行动上限，并把额外行动效果降到正常的 33%；Macro Editor 纳入所有可手动施放的 spell；Ultra 更快；每个 `take less damage` effect 除另有说明外分别封顶 80%，而不是 overall damage reduction 全局封顶 80%；更强的新 Barrier 有时未能覆盖较弱旧 Barrier 的缺陷被修复；由 Relic 产生的 buff、debuff、minion、stat gain/loss、damage taken 与 healing received 不再占用 creature 对应的每回合计数；pending resurrection 不再无条件阻止战斗结束。
- 3.0 只有公告：官方说跳过 2.1、目标曾是六月末 beta，但没有公开交付证据。本档案只登记“2.0→3.0 public evidence gap”。

## 真实循环与玩家权限

玩家在城堡选择 specialization、组装六个 creature、fusion、artifact、spell gem、relic 与 macro，进入逐步加深的 realms，完成项目、神祇 favor、资源与解锁，再决定提高 Realm Depth / Instability、换 specialization、换 creature 或改构筑。战斗按 timeline 逐个 creature 获得行动；玩家可手动 attack、cast、defend、provoke 等，也可在自己的行动点调用预先写好的 macro。

这不是原生 hands-off autobattler：

1. 官方 2.0 只证明 Macro Editor 能选择所有“可手动施放”的 spell，说明 macro 权限不能超出手动权限。
2. 社区规则帖描述宏由上到下测试；当前行无有效动作时才落到下一行，因此需要合法 catch-all fallback。另一帖说明 ethereal spell / target legality 可能让候选行无效。这些逐行、first-valid 与 fallback 语义是社区说明而非官方 specification。
3. 官方 2.0 只确认 Ultra 加速处理；“轮到 creature 时仍需选择 / 输入 Macro 或 default action”来自 2026 社区观察与个人 review，不是官方规则说明。社区参与者用控制器重绑或外部按键脚本减轻重复输入，属于 workaround，不是原生全自动权限。
4. 2026 Booze 帖报告“能手动施放但不在 macro spell list”，与官方 2.0“所有可手动施放 spell”以及历史修复方向冲突。没有复现与官方回应，故保留为低置信回归观察，不能反改官方规则。

对本项目最重要的不是照搬宏语法，而是把“配置权、触发权、动作合法性、fallback、最终 owner”分开。宏决定选择哪条可用动作，不创造新动作，也不应隐藏为什么所有行都失败。

## 构筑 / 结构一：Graveborn 主动死亡—复活—统计收割

`Bargain Bin Builds` 的 2.0 Graveborn 用主动自杀和复活制造重复死亡事件，再由 specialization / trait readers 把死亡次数转成 stats 与输出。

| 语法位置 | 具体结构 |
|---|---|
| engine | 可控的 ally death 与 resurrection 循环 |
| state/resource | 每只生物的存活状态、死亡 / 复活次数、行动预算、累计 stats |
| payoff | 死亡 / 复活 readers 提升团队属性，最终由攻击或 spell owner 结算伤害 |
| survival | 复活能力、减伤与至少一个不会同时断掉的恢复入口 |
| spatial condition | 六个 party slots 与固定开战顺序；关键 supplier / reader 死亡时会改变后续触发顺序 |
| payoff owner | reader 只产生 stats；实际 attack / spell 仍是伤害 owner |
| pivot / counter | Satyr Guardian 惩罚重复行动，Corrupted Phoenix 等 resurrection counter 直接断 engine；遇到时要减少循环依赖或改为一次性输出 |

它不是“死得越多必然越强”。每 creature 每 battle 最多 resurrect 10 次，attack / cast 等也有每 turn 15 次的上限；Calm 会进一步收紧。构筑成立依赖可控死亡、复活权限和 reader 同时存在，任一环被封锁都应在战斗报告中明确显示。

## 构筑 / 结构二：Necromancer minion snowball

2.0 `Bargain Bin` 与 2025 `Necromancer issues.` 提供两名作者、两种实践角度。前者用 start-of-battle / turn-based minion 供应、minion count 与 buff / stats readers 形成 snowball；讨论帖展示一个真实失败：minion spell 未按预期覆盖全队、hit 经常为零、Realm Instability 下 Speed 劣势导致 dodge，且过量投资 minion traits 挤占 stat / survivability slots。

- engine：summon minions / stacks，或随 attack、cast、damage、turn 继续补充。
- state/resource：各 creature 自己拥有的 minion types / stacks；它们不是额外棋盘单位，也不占 party slot。
- payoff：具体 minion 自己拥有 damage、buff、debuff 或 stat effect；specialization perks 可再读取“不同 minion 数”提供 damage / mitigation。
- survival：minion-driven healing、减伤与 creature 本体的 Defense / Health。2.0 将每个 `take less damage` effect 单独封顶 80%；维护 Combat 规则说明多个来源按剩余伤害乘算，例如两个 80% 来源合成 overall 96% 减伤。这里不存在 overall 80% 全局上限，也不能仅凭单一 effect 的上限断言多来源生存链已失效。
- counter / pivot：enemy dodge、低 Speed、高 Realm modifier、minion removal / denial、action cap 与慢清场都会断链。第二作者建议用不依赖命中的模块或修正 Speed / attack ownership，而不是继续堆同一种 minion trait。

这里的核心迁移价值是：召唤体系至少有“supplier、每个宿主的局部 ledger、minion action owner、汇总 reader”四层，不能只显示一个全队召唤数量。

## 构筑 / 结构三：Paladin Provoke / Defend → 承伤、Defense → Retribution / Health

`Bargain Bin` 的 Paladin 把 defensive actions 当 engine，而非把“盾”本身直接定义为伤害。Provoke / Defend 提供可预测的承伤和状态入口；Retribution 读取 attack / spell damage taken，Art of War 把最高 stat 加入 Retribution，Seraphim 则把 Defense 转成 Health。这里没有足够来源证明 prevented damage 会转成输出。

- supplier：artifact stats、fusion traits、specialization perks 与 Provoke / Defend 行动。
- state：attack / spell damage taken、最高 stat、Health、Defense、damage reduction 与 Barrier 是不同字段；Barrier 吸收不能自动等同 Health damage，也不能当作已证实的 Retribution 输入。
- payoff owner：Retribution 读取合格承伤，Art of War 添加最高 stat；Seraphim 只负责 Defense → Health，Defense 本身不直接造成伤害。
- opportunity cost：六个 creature slots、artifact trait slot、relic attunement 与 perk points 都在竞争；只堆 Defense 会失去速度、清场或 counter coverage。
- counter：忽略 Defense / indirect damage、buff removal、行动限制、极高 Defense 配极低 Health 的敌人以及阻断 Provoke / Defend 的规则会分别攻击不同环。

这正好回应项目的盾体系疑问：防御体系要输出，必须有显式 converter；converter 还要声明读取哪个防御字段、读取谁、何时 snapshot、由谁造成伤害以及是否允许自反馈。

## 构筑 / 结构四：Shadowbringer Fermented Hops + Blighted

2.0 `Bargain Bin` 的 Shadowbringer 以 Fermented Hops 给敌人施加基于其 maximum Health 的间接伤害，再由 Blighted 增幅与 Sea Shambler 模块补 sustain / damage reduction。输出不来自自己的 Attack，而来自 spell / Blighted damage event owner。

Ic0n Gaming 的 2.0 launch-era `Shadowbringer Setup` 始终在 RD493、maximum 593、RI0 的 gentle test 中搭出 attack → heal → Blighted 路线：前几个 encounter 可用，后续不同敌包 / 长战暴露输出偏低、特定武器 spell-slot proc 不稳定与敌方 respawn 拖延。它不是“进入更深层”的 scaling test，也不能把一个武器 proc 的问题扩大成全部 Blighted trigger 不稳。作者只要求补持续的 stats / damage growth；“另做 resurrection-safe finisher”只能是项目侧待验证方案，不是作者原话。该视频不与下述 realm-rule 讨论拼成一支队伍，也不借讨论帖的错误因果解释视频失败。

这条链的可读 counter 很具体：debuff immunity 直接阻止 Blighted，Nix Informer 等开场封法破坏 initiation，高 Defense / 低 Health 敌人让某些 stats-based补刀效率变差，Treasure Golem / Laughing Wisp 等包要求另备不依赖 debuff 的出口。2025 realm-rule 帖还提供独立失败样本：玩家把“less healing”与敌方高 Intelligence 同时出现误认为伤害异常倍增；回复指出关键是 Shadowbringer 读取敌方高速成长 stats，而不是那个 realm healing modifier 反向计算。该帖证明必须显示实际 reader 与 modifier 来源，不能从结果倒推因果。

## 构筑 / 结构五：Cleric actual healing → Abnegation；maximum Health / Defense 并行生存

1.1.1 历史 Cleric 指南闭合了“heal → actual Health recovered → Abnegation indirect damage”的旧路线；2.0 `Character Specialization Guide`、Most Insane Builds 的 `Beginner-Friendly Cleric` 与 2.0.38 Undying 帖则给出 current-era 边界。Abnegation 读取实际恢复量，并让随机敌人承受该恢复量 50% 的伤害；Dreams of Ice 的 heal → Defense 是并行生存支路，maximum Health 只扩大可恢复上限，二者都不直接被 Abnegation 转伤。视频几乎没有 Anointment / Nether Stone，并实测普通战，但它不是 fresh-account 或正常前期的独立复现：画面已有 Tier 50 boots、约 1.86–1.95M Health 与 artifact trait grind。RI5 只被作者称为 `might be possible`，少量未优化测试 `wonky`；Boss / False God 只有适配建议，没有视频实测。

2.0.38 Undying 帖是另一位作者、另一支导出队伍：start-of-turn healing、Mend、maximum-Health growth、timeline move 与 Abnegation 可能形成长循环；玩家只观察到 Undying 与 Carnal Genesis heal / timeline loop 共现，敌人表面全死后战斗仍继续。回复称 heal loop 可造成 softlock，但没有官方或内部 queue 诊断证明 pending resurrection 是该次阻塞状态。视频与帖子只在结构层互证，不能把两支队伍、各自组件或实测结果合并成同一导出。

- heal supplier、actual Health recovered、maximum-Health ceiling、Dreams of Ice Defense、timeline move 与 Abnegation 分属不同 owner。
- Abnegation 只读取实际 recovered Health；Defense 是并行 survival，maximum Health 只提供恢复 headroom。满血是否能触发取决于明确 perk，不可从“施放了治疗”直接推断。
- 2.0.38 个案只证明高频 healing / timeline movement 与表面全死不结束共现，回复者认为 heal loop 可 softlock；exact blocking state 未知。官方 2.0 的旧 pending-resurrection 修复是独立 lifecycle 事实，不能拿来解释该现行个案。
- historical guide 明确指出 action cap、indirect-damage immunity / reduction 会让表面完整的链失效。它只能说明结构生命周期，不能给 2.0 当前强度背书。

## 构筑 / 结构六：Hellknight story → lategame progression

mr.kitty 的 2024 指南不是单个终局阵容，而是一条从 story 到 postgame 的可执行迁移路径：按可获得性招募 / fusion，先用稳定 attack engine 通关，再逐步补 artifact、spell、boss counter 与更昂贵的 trait。它展示的是 recruitment / replacement decision：早期组件必须能独立工作，后期 reader 到手后再替换过渡位，而不是从第一层强塞终局清单。

Hellknight 的价值在于把普通 attack 变成可投资的发动器，并通过 specialization / trait 修正命中、额外行动与输出。counter 则包括反击 / on-attack punishment、限制额外 attack、Confused 反噬和 boss-specific immunity。2.0 的 Relic 豁免只让由 Relic 产生的 buff、debuff、minion、stat gain/loss、damage taken 与 healing received 不计入 creature 对应的每回合额度；attack / cast 不在该列表，更不代表全队 recursion 无上限。

## 构筑 / 结构七：Warlord Antiquarian + Relic / Reliquary

2.0.37 specialization 指南把 Antiquarian 定位为不能开局选择的 advanced specialization；Most Insane Builds 的 2.0-era `Warlord Antiqurian` 则补齐具名闭环。实战按视频 2.0 UI 写作 `Hanti & Jihi`；maintained wiki 正文写 `Hanti & Jiti`，但其图片文件名又写 Jihi，故保留拼写冲突。方案由一只 creature 装备 Hanti & Jihi，再由 Last of the Ancients 让其余五只 creature 各装备该 Relic 的一个副本；这不是复制 trait / effect。全队 Provoke 后，敌方攻击启动各 holder 的 Relic 反击、maximum-Health、healing 与累计承伤输出。作者口述可打 RI5，视频实际录制 Gate；Muse / Tune 分别在 Gate 中被判不可行，长战存在剪辑，所谓 `100% Gate win rate` 只是作者回忆而非统计。

| 语法位置 | 具体结构 |
|---|---|
| engine | 全队 Provoke，把敌方攻击变成 Relic 与承伤 readers 的统一启动信号 |
| state/resource | 每个 holder 的 Relic、maximum Health、healing、累计承伤与行动上限；Reliquary rank / Piety / attunement 是长期投入 |
| payoff | Relic 反击与累计承伤 reader 分别结算，而不是 Provoke 或 Health 本身自动造成伤害 |
| survival | maximum-Health growth、healing 与 Relic 提供的 holder 生存；慢速闭环需要承受多轮敌方行动 |
| spatial condition | 六个 creature slots 与各自 Relic holder 绑定；五个 Relic 副本不抹掉各 holder 的触发 owner、结算 owner 与固定开战顺序 |
| payoff owner | 每个 Relic / reader 的 holder；团队规则只供应触发与状态 |
| pivot / counter | Muse / Tune 在 Gate 中分别被判不可行；spell / indirect damage 不受 Rank 70 的 attack-damage reduction 覆盖。遇到阻断 Provoke、反击或恢复的包需换终止器，不能只继续堆 rank |

这仍不是正常养成下的无条件强度证明：视频中 Rank 70 是 bearer 承受的 attack damage 降低 50%，不覆盖 spell / indirect damage；大量 Reliquary 已是高 rank，作者也承认多层高 rank 投入混在一起，无法隔离 Antiquarian 本体与各 Relic 的正常 ROI。Wiki 与官方生命周期只互证系统边界：Relic 绑定具体 creature，不等于 ordinary artifact；Reliquary upgrades、Piety、attunement 与 rank 是长期 owner；Antiquarian perks 只改写这一层；2.0 只豁免由 Relic 产生的指定状态 / 计数事件，不能外推为 Relic attack / cast、普通 trait、spell 或整个队伍不受上限约束。

同作者 2026 `Extreme Stat Gainings` 在 5740-NG、Rank 100 画面中展示爆炸式增长到科学计数法；作者随后说 gain 变 flat，并在 7:49 / 8:12 口述怀疑 `suspected buggy`，因此没有建立数学指数公式。`1e205` 与多数战 one-shot 只是作者回忆 / 经验，不是当前画面或统计。Seed of Potentiality 还明确规定其产生的 stats 不再触发 on-stat-gain effects，证明至少存在显式 anti-retrigger guard；本样本的根因仍未知，不能声称已证实自反馈或无界递归。它只登记为高增益、数值范围与归因风险，不作为稳定规则、正常数值基线、采用率或 Warlord 构筑必需组件。

## 所有权层与机会成本

Siralim Ultimate 的复杂性来自多层叠加，而不是一个统一“羁绊值”：

1. creature native trait：主亲决定 race、personality 与 body；副亲决定 class，双方基础 stats 取平均。
2. fusion：融合后保留两条 trait，但视觉 / class / race 的归属不对称；它是永久 roster commitment，不是战斗内临时 buff。
3. specialization perks / Anointments：玩家角色层规则；可切 specialization，但 perk / Anointment 的可达性与 project / boss progression 有成本。
4. spell gems：绑定具体 creature 的可施放动作，受 charges、silence、seal、class、macro legality 等约束。
5. artifacts：绑定 creature，提供 stat / property / trait / spell 等插槽；它们不等于 Relic。
6. Relics：单独的神祇、attunement、Piety、rank 与特殊 action owner；Antiquarian 只改写这一层。
7. minions：附着于各自 master 的临时 buff-like entities，拥有自己的 trigger / effect / stack，而非棋盘人口。

每次替换 creature 都可能同时改变 native trait、fusion trait、class、artifact holder、spell access、relic holder、macro 与开战顺序。项目若借鉴，UI 必须给出 before / after，而不是只显示战力箭头。

## Barrier、Health 与伤害归因

维护规则把 Barrier 定义为施加时按最高 stat 计算、上限为该值 200% 的吸收状态；新的 Barrier 不叠加，只保留更强者。2.0 修复的是“更强的新 Barrier 有时未能覆盖较弱旧 Barrier”，说明历史实现曾拒绝应当发生的升级替换，而不是让弱 Barrier 覆盖强 Barrier。攻击只被 Barrier 吸收时通常不计为对 Health 造成伤害；该讨论中要求至少造成 1 点 Health damage 的 On-Attack / health-hit reader 因而不触发，但其他 On-Damaged 或 On-Damage（spell / indirect）trait 不能由此一概而论。

社区实践补两条边界：

- Holy Blast → 最大 Barrier → Warp Reality 移除 Barrier 并按其强度转成 spell damage，是一个显式“状态供应—移除—转伤”链；不是所有 Barrier 自动爆炸。
- `On The Rocks not triggering Hunter's Season?` 的回答明确指出 damage to Barrier 或 0 damage 不满足该 on-attack / Health-damage reader。这个差异需要战斗历史同时显示 absorbed、Health damage、trigger accepted / rejected。

所以 Barrier、Defense、damage reduction、maximum Health、healing、Dodge 与元素 / debuff 均是正交轴。原作没有 Ice Shield / Earth Shield 分类，也没有“全队额外生命 / 防御的 50% 自动转某个射手攻击”的通用规则；那仍是项目侧待设计的 converter。

## 行动预算、顺序、循环与失败日志

- 固定开战顺序与 timeline 决定哪位 creature 先提供状态。fusion 会改变 class 等归属，但不会替项目解决宏继承 / 重绑问题；原作的 macro 与具体 creature / 可用 action 联动。
- 维护 Combat 页列出的常态上限是：每 creature 每 turn 最多 attack 15 次、cast 15 次、receive buffs 15 次、receive debuffs 15 次、receive minions 15 次、gain stats 15 次、lose stats 15 次、take damage 15 次、receive healing 15 次；每 creature 每 battle resurrection 最多 10 次。Defend / Provoke 不在这份 15 次列表中。官方 2.0 以 attack、cast、resurrect 为示例说明 Calm 会进一步降低上限，并把 extra-action effect 降到正常的 33%。
- 2.0 把每个 `take less damage` effect 除另有说明外单独限制为最多 80%；不同来源仍按剩余伤害乘算，两个 80% 来源可得到 overall 96% 减伤。诊断时必须显示每个来源的独立 clamp 与乘算后的 residual damage，不能误报为全局 80% cap 或简单相加。
- Undying + heal / timeline loop 个案只说明表面全死与持续事件流可能共现，exact blocking state 未知；官方修过一个旧 pending-resurrection battle-end blocker，但没有证据把两者判为同一原因或回归。
- 2026 hidden-caps review 抱怨玩家只有在投入构筑后才发现 resurrection、stat gain、damage events 等上限；这是单人评论，但与官方上限存在性相符。问题是披露，不是证明上限本身错误。
- 2025 attribution review 指出 sudden wipe 常来自隐藏在子菜单里的 trait / spell / status 组合，读完 battle history 仍可能不知道谁造成巨额伤害。另一社区帖也说明界面不展示计算后的 modifier，history 缺少足够中间值。

因此“日志有上千条事件”不等于可诊断。有效报告要折叠重复事件，同时保留 source owner、target、读取值、cap 前后、Barrier absorbed、Health damage、被拒绝的 trigger、递归深度与终止原因。

## 敌方考题与可适应窗口

- anti-resurrection：直接截断 Graveborn / Cleric death loop；需要战前标识和替代伤害出口。
- anti-debuff / cleanse：截断 Shadowbringer / Blighted；不能只显示“伤害为 0”，要显示 debuff application 被拒绝。
- Dodge / Speed gap：让 attack/minion build 看似“数值不足”；应把 hit failure 与 damage-after-hit 分开。
- start-of-battle silence / spell seal / charge reduction：攻击 macro 的合法动作集合；fallback 必须有非 spell 行。
- high Defense / low Health、indirect-damage resistance、单个 damage-reduction effect 的 cap 与多来源乘算：分别考查 reader 选型，不是一个“更硬”标签。
- Satyr Guardian、Corrupted Phoenix、Nix Informer 等具名包说明好 counter 应针对链条的一环，并给玩家在出发前换 spell、trait、holder、macro 或 specialization 的窗口。

## 生命周期与去重 negative / reworked families

本 checkpoint 记 17 个去重案例：

1. Macro Editor 从分散可用列表改为覆盖所有可手动施放 spell；2026 Booze 仅作未复现回归观察。
2. Calm Mode 通过更低行动上限与 33% extra-action 效果压低触发洪水。
3. 官方 Ultra 只确认性能加速；社区 / 个人 review 仍观察到动作选择输入与重复按键负担。
4. 每个 `take less damage` effect 除另有说明外被标准化为最高 80%，但不同来源继续按剩余伤害乘算；这是单来源上限与组合规则的生命周期调整，不是 overall reduction 全局封顶或所有高减伤组合终结。
5. 更强的新 Barrier 有时未能覆盖较弱旧 Barrier 的历史问题被修复。
6. 由 Relic 产生的 buff / debuff / minion、stat gain/loss、damage taken 与 healing received 不再占用 creature 对应的每回合额度；attack / cast 未获该豁免，要求明确事件来源与计数 owner。
7. pending resurrection 无条件阻止 battle end 的终止问题被官方修复。
8. Graveborn 受 resurrection / action cap 与 anti-resurrection counter 约束。
9. Necromancer 过度投资 minion traits、Speed / dodge 与 slow clear 的实践失败。
10. Shadowbringer realm-rule false causality：结果变强被错误归因给 healing modifier。
11. historical Cleric actual-healing / Abnegation loop 受 indirect-damage immunity 与行动上限失效；maximum Health 只提供恢复 headroom。
12. macro top-to-bottom / no-valid-line 的可读性与 fallback 负担。
13. 隐藏 caps 在投入后才暴露的社区抱怨。
14. battle history 事件很多但 owner / modifier / rejection 不足，导致失败仍难归因。
15. 2.0.38 Undying 个案在旧终止修复之后仍出现敌人表面全死但战斗不结束，并与 heal / timeline 长事件流共现；exact blocker 未知，不与第 7 项合并，也不判为同一原因或回归。
16. Shadowbringer 在同一 RD493 / maximum 593 / RI0 设定中从前几个 encounter 可用到后续不同敌包 / 长战输出偏低、特定武器 spell-slot proc 不稳并被 respawn 拖长，暴露同设定 robustness 缺口。
17. Rank 100 Relic / stat-share 的爆炸式科学计数法增长被视频作者口述怀疑为 bug；作者也说 gain 后来变 flat，且存在 anti-retrigger guard，只作数值范围 / 归因风险而非数学指数或稳定平衡基线。

这些案例不证明商业成败，也不说明所有玩家都抱怨；它们只记录规则重做或可复核的失败链。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

- 官方：Steam News API / canonical event 页面覆盖 0.4、0.6、0.9、0.10、0.11、0.12、1.1.1、恢复开发、2.0 live 与 3.0 announcement；Thylacine 官方博客直接读取 2.0 full notes。EA announcement、1.0 release 与商店只作身份 / route audit，不登记为实质规则来源。
- 维护规则：逐页读取 Combat、Buffs、Creatures、Artifacts、Spell Gems、Realms、Realm Depth、Reliquary、Specializations、Projects、Minions、Debuffs 的真实 MediaWiki API 路由。Wiki.gg 当前可能受 Cloudflare、429 或 403 波动，未绕过；索引保留 API URL 与限制。维护 compendium 只用于 2.0 名称 / owner 交叉检查。
- 指南：完整读取四篇 Steam Guides。`Bargain Bin` 以 2.0、RI2、约两倍 enemy level 与低资源为边界；Hellknight 给出 story→postgame 路径；Cleric 明确为 1.1.1 历史；Specialization Guide 明确 current as of 2.0.37。Planner 只作为社区帖子里的导出载体，不登记。
- 视频：四段 YouTube 视频具备可导出 ASR 并登记为 `video-transcript`。它们分别提供具备高阶存档投入的 beginner-accessible Cleric recipe、实际 Gate 录制的 Warlord Antiquarian / Relic、固定 RD493 / maximum 593 / RI0 的 Shadowbringer robustness test，以及 5740-NG / Rank 100 stat-share 压力测试；每段都保留其未优化、高 rank 混杂、同设定长战失速或 suspected-bug 边界。`Death Horde` 页面只证明标题中的 RI5 / Patch 2.0、章节对象与作者“未优化、慢、should 可打”的自述；因 `captionTracks=0` 且没有人工听读 / 画面时间戳日志，不登记为 substantive source，也不据此声称实战胜负或构筑闭环。Siegemaster 同样未取得足以新增独立 `Spell Gems Copy` counter 的可读字幕证据，未登记；Planner 仍不登记。
- 社区：读取 Necromancer 第二作者意见、Shadowbringer realm-rule、五个 macro / input / Booze 主题、两条 Barrier 主题、Undying softlock 与 attribution 主题。弱 Paladin episode、3.0 转帖、无字幕视频与只有标题的路线不登记。
- reviews：通过 Steam Reviews API 读取固定 review id / author / date，保留 hidden-cap 与 post-2.0 attribution 两条详细文本；它们是社区体验，不是规则权威。
- 饱和判断：七条结构已覆盖死亡、召唤、防御转伤、debuff/max-Health、治疗/max-Health、进度替换、Relic 改写；新增视频补齐第二作者的 current 2.0 实测与具名 Relic-heavy 闭环，却没有改变宏的来源权威或 3.0 边界。剩余缺口集中在 3.0、宏目标绑定、完整 resolver order、正常 Reliquary 养成 ROI 与代表性统计，继续堆 catalog / 单 build 不会关闭这些缺口，故停止于 45 个实质来源。

## 可迁移与不可迁移

可迁移：

- 用 `supplier → state ledger → reader → payoff owner` 表达盾、生命、治疗、minion 与 debuff；防御只有经过显式 reader 才转成输出。
- equipment / relic 可以改写规则，但必须声明 holder、scope、snapshot、cap、slot cost、反馈环 guard 与 exception owner。
- 宏只在手动合法动作集内选动作；上到下、fallback、拒绝原因、目标绑定与玩家触发权都必须可见。
- 敌方 package 针对链条的一环，并在战前给换 holder、spell、站位 / 顺序或 specialization 的窗口。
- 战斗报告折叠重复事件，却保留 owner、读值、cap、吸收、拒绝与终止原因。
- 长期 progression layers 分账：fusion、specialization、artifact、spell gem、relic、project / favor 不能压成一个战力数字。

不可迁移：

- 不复制具体 creature、trait、spell、perk、artifact、relic、boss 名称和数值为第一版内容。
- 不把 Siralim 1/2/3 的规则或 1.1.1 构筑 currentize 到 2.0，更不推断未发布 3.0。
- 不把 macro / Ultra 称为原生 hands-off autobattle，不把外部按键脚本当产品能力。
- 不把 Barrier、Defense、Health、healing、Dodge、Blighted 或元素混成同一轴。
- 不把一篇 guide、一条帖子或一篇 review 解释为 meta、胜率、普遍玩家行为或因果统计。
- 不把 Antiquarian / Relic 结构直接等同本项目的遗物系统；它只提供 ownership 与 commitment 参考。

## 未决问题

- 3.0 是否已进入公开 beta、正式规则与 2.0 save / platform compatibility；截至本 dossier 没有公开交付证据。
- 2.0.x 各平台 exact binary，2.0.37 / 2.0.38 的完整公开 patch notes 与当前 build continuity。
- Macro 导入 / 导出格式、完整 target-legality / rejection 反馈，以及 Booze spell 缺失是否可复现及其修复状态。
- 已公开 cap / Relic 来源计数豁免在后续 2.0.x 与各平台 exact binary 中是否保持连续，以及这些事件的归因、计数与 resolver 细节；Calm 的完整数值表仍未决。
- Barrier 与 Health-damage readers 的完整 ordering、Fracture / removal 与其他 modifier 的结算顺序。
- minion、death、heal、timeline move 同时触发时的内部 queue、recursion depth、terminal guard 与日志折叠规则。
- Realm Instability 的当前分布、各 build 使用率、成功率与 counter 出现率；本来源包无代表性统计。

## 最终 disposition

`anchor-retained`

Siralim Ultimate 具备足够强的规则与实践互证：45 个来源闭合七条不同结构，能把 fusion、trait、specialization、spell gem、artifact、relic、minion 与 macro 分成独立 owner，并同时展示 defense-to-offense、healing/max-Health、death/resurrection、debuff、action cap、counter package 与失败归因。它对本项目“体系如何靠装备 / 遗物 reader 产生伤害”尤其有参考价值。

但它仍是相邻的 programmable-combat anchor，而不是 TFT 式自走棋。官方只确认 Ultra 加速；已读取的 2026 社区观察与个人 review 仍描述玩家选择 / 输入动作，宏只在合法动作内自动化决策。长期 realm / collection economy 也不同于短局 shop / bench / shared-pool。其结论只进入研究语料，不成为项目玩法权威，也不授权任何第一版英雄、装备、遗物、元素或数值。
