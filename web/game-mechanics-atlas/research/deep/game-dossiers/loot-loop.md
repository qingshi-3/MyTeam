# Loot Loop

## 身份、时期与资料密度

- `title_id`: `loot-loop`
- 精确对象：Steam App `3972320`，开发 / 发行均为 BitBrew；BitBrew 是单人开发者 Patryk 使用的名义。正式版于 2026-04-13 发布。
- 历史 Demo App `4207310` 于 2025-12-01 对外宣布，2026-01-19 获得主动技能、Boss 反击与早期自动攻击大更新；访问日 appdetails 对该 App 返回 `success:false`，商店 / 社区页落到通用 Welcome 页面。这里只记录历史节点，不猜测下架、合并或可玩状态。
- 访问日明确现行节点为 2026-08-12 的 Patch 1.1。开发者当日列出的 1.2 Healer 重做、1.3 手柄支持与 1.4 新地图 / Prestige / Boss / 机制都只是计划，不进入当前规则。
- 本 checkpoint：21 个实质非商店来源——3 个官方发布 / 补丁 / Demo 机制节点，2 篇独立详细评测，1 篇可读 Steam Guide，7 个规则 / 实战讨论与 8 条实质 Steam 评测。
- 置信：中高。固定四人、持续掉血、Power 主动时序、三个 aura、升级 / perk、一次 Prestige 和两套相反构筑已闭合；但没有正式手册、完整当前公式、技能树数据库、统计或可读视频转录，因此是 `retained`，不是 anchor。

## Adaptive-depth 决定

这款游戏的研究价值不在招募 / 换将，而在固定四人如何通过主动技能时序、持续衰减生命和不可退款的能力池扩充形成构筑。第一条路线以 Knight Vampirism、Healer aura、Archer 与 Mage 爆发错峰，把生存时间换成最终 Boss 输出；第二条路线主动拒绝 speed / gold aura，只保留 healing aura，证明“获得更多能力”可能因共享轮转槽而削弱可靠性。

检索覆盖 10/10 官方新闻、59/59 Steam 讨论主题标题并打开 22 个机制主题、1/1 Guide、1,669/1,669 多语言评测、10 组英 / 中 / 日 / 俄 YouTube 查询与 25 个主要视频。可见字幕轨均返回空 timedtext；标题、缩略图和未转录画面没有成为证据。SteamDB 被 JavaScript challenge 阻挡，Reddit / 搜索结果质量低，未下载或解包 Demo / 客户端。21 个登记来源后，新增材料主要重复 Healer 陷阱、尾王时序和内容短；已足以闭合 owner、pivot、counter 与 lifecycle，继续枚举评论或无字幕视频的边际信息低。

## 来源包

- `src-loot-loop-official-demo-abilities-2026-01-19`：历史 Demo 加入四角色主动技能、敌人 Power 掉落、Boss 反击 / 分段掉金币与五分钟内自动攻击。
- `src-loot-loop-official-release-2026-04-13`：正式版四角色、六地图、skill tree / perk、一次 Prestige 的产品边界。
- `src-loot-loop-official-patch-1-1-2026-08-12`：Mage attack levels、Boss coin、死亡判胜修复，以及尚未发布的 1.2–1.4 路线图边界。
- `src-loot-loop-hakimodo-review-2026-07-19`：持续掉血、四角色技能、三个 Healer aura、Power、skill tree、Prestige 与最终地图 gate。
- `src-loot-loop-higher-plain-review-2026-04-14`：四键主动、技能树最终买满、Boss cliff、Prestige 重跑与短 cooldown 的实践批评。
- `src-loot-loop-guide-3710038865`：Prestige 后 Balrog、healing aura 时使用 Healer skill、持续 Mage skill 的实践；页面虽有 removed / incompatible 警示但正文可读，约 50% 增幅只保留为低置信作者估计。
- 讨论补足最终 Boss 时序、healing-only 争议、Vampirism 充能、Power 可读性、同 tick 死亡判胜、手动重开与 loot 边界。
- 八条评测补足 aura 陷阱 / 无 respec、技能顺序、攻速与重复施放 RNG、一次 Prestige、现行 1.1 体验和缺少装备层。

## 真实循环、固定队伍与主动边界

四名固定角色自动向右推进并攻击；全队生命持续衰减，接触敌人会增加压力，因此每次 run 是一个可见时间预算。击杀掉落金币、宝石与 Power：金币 / 宝石进入 skill tree / perk 长期成长，Power 由玩家点击角色头像投入其主动技能。死亡后玩家必须选择立即再跑或回商店消费，不会自动无限重开。

正式版固定 Knight、Archer、Mage、Healer 四人同行，没有可核实的招募替换、bench、阵型或装备。Knight 以 Vampirism 回血，Archer 负责穿透 / 箭雨爆发，Mage 负责 meteor，Healer 在 healing、attack-speed、gold aura 间轮转并用主动延长当前 aura。玩家的主要构筑决策不是“带谁”，而是点哪些 tree 节点、是否扩充 Healer aura 池、何时投入 Power、哪些技能在路上错峰以及何时为 Boss 预充。

## 构筑一：最终 Boss 主动时序全队

核心实战讨论由开发者和玩家共同给出：开局在 Healer 的 attack-speed aura 时使用 Power，借相关 perk 获得移动速度尽快抵达 Boss；沿路错峰 Archer 与 Mage meteor，不把所有爆发同时浪费在同一批小怪；实际掉血后再开约 15 秒的 Vampirism；多次点击充能技能以预充，避免 Power 在满 gauge 上浪费；抵达 Boss 后转到 healing aura，使用 Healer power 获得 Armor，再集中四技能压伤害。

- **engine**：击杀掉 Power，四角色把 Power 转为各自可充能主动；skill tree / perk 改变充能、持续、生存和伤害。
- **state/resource**：持续衰减 HP、Power、各角色黄色 gauge / cooldown、当前 Healer aura、Boss 距离与血量分段。
- **payoff**：Archer 清较大目标 / 路线，Mage meteor 清队列并承担尾王爆发；Patch 1.1 追加 Mage attack upgrade levels 以缓解尾王伤害不足。
- **survival**：Knight Vampirism、healing aura、Healer power 关联的 Armor perk共同延长输出窗口。
- **spatial condition**：固定向右推进，敌人沿路径排队；attack-speed aura 的开局 movement-speed收益缩短到 Boss 的暴露时间。没有手动站位证据。
- **payoff owner**：各角色拥有自己的主动结果；Healer / Knight 提供 uptime，Archer / Mage 拥有主要输出，不把全队生存自动算成 Mage 伤害。
- **economy / pivot**：先以 HP / Armor / Boss-defense 与团队 HP 买到存活，再给 Mage attack 和攻击频率；尾王失败时调整技能错峰、预充与 aura 时点，而非只继续点已经封顶的无关节点。
- **counter / limit**：尾王伤害 cliff、持续 HP 衰减、aura 轮转、重复施放 RNG 与技能重叠浪费分别攻击生存、可靠性和 burst。讨论中玩家对“满树即可过”与“仍需 RNG / 时序”存在分歧，不能写成保证解。

## 构筑二：Heal-only aura pruning / 拒绝升级

Healer 初始只有 healing aura。解锁 speed / gold aura 后，三个 aura 竞争同一个随机轮转槽；由于没有 respec，新节点会降低治疗出现率和 uptime。多名玩家选择重开，或借唯一一次 Prestige 重新分配：第二轮只升级 healing aura，把 Mage 作为主要 damage owner，最终 Boss 更容易；也有玩家认为 speed aura 在 farming / push 时更强，因此这不是唯一最优解，而是一个“缩小能力池换可靠性”的完整反构筑。

- **engine**：不购买 speed / gold aura，使 Healer 的共享 aura 槽只产生 healing；主动延长当前 aura。
- **state/resource**：已解锁 aura 集、随机轮转、治疗 uptime、持续衰减 HP、不可退款的 tree investment。
- **payoff**：稳定 healing 把 run 时间交给 Mage / Archer 输出；Healer 不拥有最终伤害。
- **survival**：持续治疗避免 gold / speed aura 占用生存窗口；Knight Vampirism作第二层恢复。
- **spatial condition**：无阵型选择；价值随到 Boss 的行进时间和尾王停留时间增长。
- **payoff owner**：Healer 拥有 aura 供应，Mage / Archer 拥有输出，skill tree 节点拥有能力池扩充与机会成本。
- **economy / pivot**：在无 respec 前提下，最晚安全 pivot 是购买第二 aura 之前；误买后只能接受随机轮转、依赖主动锁定，或重开 / Prestige 后不再购买。
- **counter / limit**：只治疗会放弃 speed / gold 的 farming 效率；某些玩家明确报告 speed 更强。Patch 1.2 计划让多个 aura 同时存在，正说明当前共享槽是已知问题，但访问日尚未重做。

这条线的关键不是“治疗数值高”，而是能力池扩充改变了抽取分母。奖励树必须显示新节点会替换、稀释还是并行叠加；否则“升级”会成为不可逆陷阱。

## 升级、Prestige 与缺失的 loot 层

- skill tree 包含 Attack、Attack Speed、Critical、Armor、Boss Defense、团队 HP 等节点；Higher Plain Games 认为最终可全部买满，后期选择因此收敛为 completion，而非长期互斥 build。
- 一次 Prestige 重置进度并提高收入、强度和生存，最终地图锁在其后。部分玩家觉得十分钟左右即可重跑、反馈强但新选择少；另一些玩家接受其作为短游戏的第二圈。只能说评价分裂，不能从评论推导代表性。
- 当前所谓 loot 是 gold / gems，Boss 被打破血量段时掉币；没有独立武器、护甲或装备构筑层。玩家提出 weapon upgrades 后，开发者只确认 1.4 会加入某种 meta mechanic，尚未确定就是装备。
- 自动攻击在历史 Demo 中提前到五分钟内，但主动技能、拾取和每 run 后手动选择仍保留。它既不是纯挂机，也不是持续高输入动作游戏；系统必须明确哪些自动、哪些需要玩家在场。

## 敌人、失败解释与适应窗口

- 早期 Boss 从被动血条重做为会反击、按血量段掉金币，说明 Boss 需要对构筑施压，而不只是更厚的普通怪。
- 最终 Boss / Balrog 在 Prestige 后开放。玩家报告满树仍可能在接触后快速死亡、依赖 aura / Vampirism / repeat-cast 时序；Patch 1.1 又专门增加 Mage attack levels。这支持“输出与生存 cliff”存在，但没有失败率或精确 Boss 公式。
- Power 需要点击角色、部分技能需多次点击充能，黄色 gauge 是现有提示；玩家仍能通关后才知道 Power 用途，开发者承认需要更强视觉 cue。失败报告应拆开“没有 Power”“gauge 未满”“aura 不对”“技能重叠”“Boss 输出不足”。
- 历史版本允许队伍死亡后残留 projectile / meteor 杀 Boss，并同时显示胜负、发奖励；Patch 1.1 改为队伍死亡不再判胜。结算必须有确定优先级，不能让战斗事件和奖励分别提交。

## Demo→1.1 生命周期

本 checkpoint 计十二个 materially distinct negative / reworked families：

1. **只看数字上涨**：Demo 加四角色主动与 Power，让 run 中出现时序输入。
2. **自动攻击解锁过晚**：Demo 改为五分钟内解锁，把输入集中到拾取 / 技能。
3. **Boss 只是被动血条**：改为反击与分段掉币，增加阶段反馈与风险。
4. **新增 aura 稀释治疗**：共享随机槽使加法成长变成可靠性下降；1.2 并行 aura 仍是计划。
5. **尾王伤害 cliff**：1.1 给 Mage attack upgrade 增加等级，提供数值追赶出口。
6. **死亡后仍判胜**：projectile / meteor 与死亡同 tick 可同时结算；1.1 明确禁止队伍死亡后的 win。
7. **Boss coin 飞出屏幕**：掉落可超出拾取边界；1.1 修复。
8. **Power / charge 不可读**：玩家忽略整套主动系统，开发者确认视觉提示不足。
9. **一次强制 Prestige 选择弱**：最终地图 gate、快速重跑和纯数值放大令第二圈对部分玩家像门票而非新构筑。
10. **skill tree 最终全买满**：早期路径选择在后期收敛，缺少长期互斥 owner / slot cost。
11. **标题承诺与掉落层不匹配**：只有 gold / gems，缺少玩家预期的装备 / 独特掉落；未来 meta 机制未定。
12. **idle affordance 混合**：自动攻击存在，但技能、拾取与每 run 手动重开使离席边界不清。

一般内容短、画面设置、窗口尺寸等问题不拆成机制案例。Cumulative explicit negative/reworked cases：167。

## 对本项目可迁移

- **防御体系也要有明确输出出口**：可以用 healing / Armor / Vampirism买 uptime 给 Mage / Archer，也可以由遗物 / 装备显式读取 bonus HP / Defense / Shield 转给一个 carry；后者必须写明 owner、slot cost、刷新、cap 与 counter。
- **盾与元素是正交轴**：Ice Shield、Earth Shield 都可属于 Shield 生存语义，同时保留 element、supplier、reader / converter、payoff owner 与抗性 / 破盾 counter；命名不能自动合并体系。
- **能力池扩充不是天然升级**：共享随机槽里加能力会稀释关键结果。新 aura / relic / tactical command 必须说明是并行、替换、抽取还是互斥。
- **固定队伍也能形成构筑**：主动时序、技能树路径、perk、资源集中和拒绝升级可以形成差异，不必强行引入招募。
- **Boss 必须解释失败链**：展示到达 Boss 时 HP、各技能 gauge、当前 aura、吸血 / Armor uptime、Boss 分段和每个 owner 的 damage，避免只给“战力不足”。
- **结算要确定且原子化**：死亡、延迟 projectile、Boss death、掉落和胜利只由一个有序状态机提交。

## 不兼容与未决问题

- 本项目是塔层 hero-roster autobattler，不能直接复制 Loot Loop 的固定横向轨道、全队持续掉血或一次性 Prestige gate。
- 没有证据支持手动站位、招募替换、装备、真实元素反应、Shield、仇恨或抗性公式；这些只能作为本项目后续设计问题，不能回填原作。
- HAKIMODO 与多条实践来源把 Healer aura 写为 heal / speed / gold；Higher Plain Games 写成 speed / damage / health。来源冲突保留，以前者的多源闭合为当前较高置信映射，不把 damage aura 写成确认规则。
- Guide 作者对 healing-aura Healer skill 的“约 50%”用 `if I'm not mistaken` 限定，没有公式 / patch 对照，不能作为确定数值。
- 1.2–1.4 均为 2026-08-12 路线图，发布日期和最终设计未确认。Demo App 当前不可解析的原因未知。
- 未找到代表性 pick / win / clear 统计，所有“更强”“唯一解”“OP”只保留为具名玩家实践或争议。

## Disposition

`retained`。

保留理由：21 个实质来源跨官方机制 / 补丁、两篇独立详细评测、Guide、讨论和多语言实战评测，闭合两套互相对照的构筑，并覆盖 Power 时序、持续生命预算、固定 owner、不可退款 aura 池、一次 Prestige、Boss counter 和十二个生命周期案例。

停止理由：关键新增资料已连续重复 aura 陷阱、尾王时序与短流程；视频无可读字幕，缺少手册 / 数据库 / 统计，进一步搜索无法可靠补全公式或形成第三套 materially distinct build。若 Patch 1.2 发布、Demo 恢复、出现维护数据库 / 可读转录或正式装备层，再重开版本审计。
