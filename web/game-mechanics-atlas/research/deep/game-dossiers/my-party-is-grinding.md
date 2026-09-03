# My Party Is Grinding

## 身份、时期与资料密度

- `title_id`: `my-party-is-grinding`
- 精确对象：Steam App `4952980`，开发 / 发行均为 SUPERLINK Inc.；在线、服务器权威的桌面挂机 RPG。
- 公开 Playtest 于 2026-08-18 开放、8 月 21 日结束，进度清空且不继承；完整免费版于 2026-08-27 发布。访问日公开难度只有 Normal、Hard、Nightmare。
- 可确认官方版本节点为 2026-08-28 的 `v1.0.13`；MPIGDB 的 progression-walls 页另称其 200 行 campaign 数据已按 `v1.0.15` 客户端复核。后者是维护站的数据快照，不被冒充成官方补丁号。
- 本 checkpoint：33 个实质非商店来源——6 个官方发布 / 补丁 / 开发者节点，11 个维护数据库页面，3 篇独立实战攻略，1 个带具体 Nightmare 1-9 配装说明的视频 description，8 条实质 Steam 评测与 4 个规则 / bug 讨论。
- 置信：中高。五职业、技能、成长、装备、刷取概率链和两个构筑已闭合；但游戏仅上线一周、服务器规则仍变化，没有完整当前战斗公式、阵型控制说明或胜率数据，因此是 `retained` 而非 anchor。

## Adaptive-depth 决定

这款游戏的价值不在五职业本身，而在“固定全队 + 多层永久成长 + 职业独立技能 / 装备”如何产生所有权冲突。Knight 同时拥有全队恢复与 Shield，Warrior 提供全队 Attack，Assassin 提供全队 Attack/Movement Speed；输出可以由 Archer / Mage 的远程元素技能承接，也可以把攻、防、暴击、闪避与 Life Steal 集中给 Assassin，让它兼任承伤、恢复和伤害 owner。

研究追到 440/440 Steam 评测、23/23 公开 bug 主题、两份社区数据库、三篇独立文字攻略、七组 YouTube 查询与五个字幕轨、Reddit / 搜索引擎 / archive 路由。33 个来源后，新增结果主要重复 Nightmare 卡关、服务器不稳或 Taskbar Hero 比较；当前材料已改变 owner、economy、counter 与 lifecycle 理解，继续枚举宠物 / 外观 / 全部装备的边际信息低，按 adaptive-depth 停止。

## 来源包

- `src-mpig-release-2026-08-27`、`src-mpig-patch-1-0-13`：正式发布、当前公开难度、早期 Coin / UI / stability 调整与官方版本边界。
- `src-mpig-update-rewards-duration-2026-08-31`、`src-mpig-market-higher-difficulty-2026-09-01`、`src-mpig-stage-adjustment-2026-09-02`：奖励、Automation、装备、range / target、buff-duration 修复，以及由修复触发的 Nightmare 难度回调。
- `src-mpig-bug-reporting-thread`：开发者明确该游戏是 server-based，bug 需按时间追服务器日志。
- `src-mpigdb-heroes`、`src-mpigdb-skills`、`src-mpigdb-passive-lifesteal`、`src-mpigdb-passive-dodge`：五个固定 class slot、30 个 active、技能 owner 与 Life Steal / Dodge 的职业 / cap。
- `src-mpigdb-class-unlocks`、`src-mpigdb-campaign-farming`、`src-mpigdb-progression-walls`：解锁成本、三段掉落概率链、mini-boss 倍率、zero-weight grade wall 与 v1.0.15 数据边界。
- `src-mpigdb-equipment`、`src-mpigdb-gems`、`src-mpigdb-star-chart`、`src-mpigdb-loadout`：12 装备位、grade 容量、socket context、全队星盘和 loadout 计算边界。
- `src-mpig-guide-dscan-2026-09-02`：冷系远程双核、Assassin 装备优先、damage meter、解锁 / 周回路线与装备风险。
- `src-mpig-guide-n150-2026-08-28`、`src-mpig-guide-ed-2026-08-31`：Knight 生存、Warrior / Assassin 解锁、稳定旧关刷取、九件合成与 Hard 4 的全队 Attack / Speed / Defense 路线。
- `src-mpig-video-nightmare-1-9-description`：五英雄 Nightmare 1-9 配置中，把 Knight 的防御装备转给 Assassin 以提高 sustain；只使用公开视频文字 description，不从画面补猜。
- 八条已登记 Steam 评测补足 early-system、金币 / 装备 / 附魔错位、角色 owner、Dodge / Life Steal 过墙与元素反制不足；四个讨论补足 stage wall、Drained 不施放、wave 太快导致 Boss 不生成及星盘回本问题。

## 真实循环、固定队伍与所有权

玩家从 Knight 开始自动清一关中的普通怪、stage boss 与 act boss；完整过关后结算 Coin / EXP，宝箱进入库存。Coin 支付 Orb Circuit 的职业、技能槽、全队属性、自动开箱、离线收益与库存节点，也支付 Blacksmith、Mage Tower、Star Chart 和宠物等长期成长。装备通过宝箱、合成、制作、刻印、附魔与 gem 继续强化，再选择挑战下一关或回到稳定关重复刷取。

队伍不是自走棋式换将：数据库显示五个固定 class slot，Knight、Warrior、Assassin、Archer、Mage 各占一个，解锁后组成五人队。实际决策在于谁先解锁、每职业最多四个 active slot 放什么、passive / stat point 如何集中、12 件装备与强化给谁，以及全队永久节点何时优先于单体成长。

owner 链必须分开：Knight 的 HP 决定其 Shield 数值；Warrior / Assassin 的 buff 归 buff source；Archer / Mage 归属各自技能伤害；gem 的效果由 Weapon / Armor / Accessory socket context 决定；Star Chart / Orb 才拥有全队增益。外观 hero 记录不等于额外上阵单位，MPIGDB 还明确外观 record 的 ATK / HP 不进入实际战斗路径。

## 构筑一：Archer + Mage 冷系远程双核

DS-CAN 的 2026-09-02 实战建议在五人到齐、装备填满后，让 Archer 最大化 `Summon Ice Dragon + Elemental Damage + Skill Range`，让 Mage 最大化 `Frozen Anger + Elemental Damage + Skill Range + Skill Area`。数据库确认两招均为 Cold：Ice Dragon 为 7.2 秒、最多三目标，Frozen Anger 为 10.8 秒、最多三目标；Mage 的 `Ice Lake` 还能对最多五个目标减 Defense 并限制移动。

- **engine**：先解锁五职业与技能槽，再把 Archer / Mage 的 elemental、range、area passive 与装备孔位补齐。
- **state/resource**：两名后排的 active levels、Elemental Damage、Skill Range / Area、装备 / gem socket 与全队 buff uptime。
- **payoff**：Archer 与 Mage 分别拥有 Cold damage；Warrior 的 Reinforce 和 Assassin 的 Fast 只提供团队增益，不夺取最终伤害 owner。
- **survival**：Knight 的 Recovery 与按自身 HP 生成的全队 Shield；Archer 的 Drained 降低敌方 Attack Speed。
- **spatial condition**：没有找到可核实的手动阵型操作；可验证的空间预算是英雄 / 技能各自 range、AOE、最大目标数与移动限制。官方 8 月 31 日还修复技能沿用过期目标、不按自身 range 重选的问题。
- **economy / pivot**：前期优先 Warrior、Assassin、skill slot，再逐步解锁 20,000 Coin 的 Archer 与 50,000 Coin 的 Mage；若当前关不稳，关闭重复挑战并刷稳定旧关，而不是把失败时间继续投入前线。
- **counter / limit**：Elemental Damage 放大 cold 输出，但不自动处理 Cold resistance；物理 / 火 / 雷 / 混沌敌人和 exact resistance formula 仍缺。Nightmare 元素墙要求另配 armor-context resistance，不能把“冷系输出”误当全队防御体系。

它直接说明“元素体系”可以是一条伤害 / 控制轴，但 Knight 的 Shield 是另一条 survival 轴；二者能同队出现，不因此产生“冰盾”或防御转伤。

## 构筑二：Assassin Life Steal / Dodge 承伤输出核

DS-CAN 建议把强进攻装备优先给 Assassin，并把 Warrior / Assassin 的 Life Steal 作为早期关键 passive。MPIGDB 确认 Assassin 有 Dodge 与 Life Steal：Dodge 每级 1.5%、最终 cap 9%，Life Steal 每级 1%、最终 cap 9%（当前可达 cap 随 class hero level 增长）。一条 Nightmare 1-9 视频 description 明确将 Knight 的防御装备转给 Assassin以提高 sustain；一条社区评测也把“Assassin Dodge + Life Steal 拉满”描述为过 1-9 的做法，但承认依赖运气。

- **engine**：把 Attack / crit / active damage 与防御装备集中给 Assassin，并补 Dodge、Life Steal；不是五人平均分配。
- **state/resource**：Assassin 攻击频率、造成伤害、Dodge roll、Life Steal、HP / Defense、装备与 skill points。
- **payoff**：Assassin 同时拥有 damage、以 damage 回血的 sustain 和 Dodge 承伤；Knight 仍可拥有 Recovery / Shield，但不再必然持有最好的防具。
- **survival**：Dodge 避免部分命中，Life Steal 把实际输出回流为生命，外加转移来的防具；任一环断裂都会让 carry 先死。
- **spatial condition**：Assassin 是近战 / 高移动 owner；没有证据证明玩家能手动改站位，只能通过 Mobility、range、目标行为和装备改变接敌结果。
- **economy / pivot**：先用 7,000 Coin 解锁 Assassin；damage meter 验证其贡献后再绑定 / 强化装备。若 Assassin 无法稳定命中或受元素爆发秒杀，应把资源回移到 Knight survival、全队 Defense / Resistance 或远程双核，而不是继续赌 Dodge。
- **counter / limit**：高 burst、元素伤害、命中 / 控制与无法持续攻击会同时压制 Dodge / Life Steal。当前没有命中率公式、Dodge 独立性、Life Steal 对技能 / DOT 的完整规则，也没有通关率；这是一条社区验证路线，不是保证解。

这条线是明确的“单核承担多职责”，但它并未把 Defense 直接换成 Attack。若本项目要做“全队额外生命 / 防御转给射手攻击”，必须另有遗物 / 装备作为显式 converter，说明读取范围、owner、slot cost、刷新时点、cap 与反制。

## 解锁、刷取与装备经济

- 职业节点精确成本是 Warrior 1,500、Assassin 7,000、Archer 20,000、Mage 50,000 Coin。三篇独立攻略一致把职业与 skill slot 放在小数值节点之前；不是因为小节点永远弱，而是新增 body / active 在早期产生离散战力。
- Campaign 掉落不能只读一个百分比：field monster 先判 chest acquisition，再按 reward-category weight，再进 subpool。早期 chest 对高 grade 可为 zero weight，挂机时长无法克服 0 概率。
- 装备有 12 位；grade 同时决定 random options、engraving、enchant 与 gem 容量。gem 又按部位改效果，例如 Ruby 在 Weapon 提供 Elemental Damage、Armor 提供 Fire Resistance、Accessory 提供 Skill Range；Jade 的 Armor context 提供 Dodge。元素、承伤和输出因此竞争同一个 socket / gear owner。
- 九件同 level-band / rarity 的装备可向上合成；装备后变为不可交易。附魔失败的 destroy chance 从 100% 降至 50%，而合成在累计十次失败后下一次保底成功。高价值装备同时受绑定、失败损毁和稀有掉落约束，令“先穿、先强化还是保留”成为不可逆经济决策。
- Star Chart 有独立全队 Attack / Armor / Speed / economy 星座。Nightmare 解锁六组；Hell / Inferno 条目虽存在客户端数据，但访问日尚未公开可选，不能用未来数据填当前 meta。

## 敌人、失败解释与适应窗口

- 多语言评测和公开讨论集中描述 Nightmare `1-9`、`2-9` 及 act-end 突刺；只可说它们反复被报告为墙，不能从评论数量推导失败率。
- 1-9 报告的核心不是“数值高”一句话，而是 Knight / tank 在元素 / 远程爆发下先倒、Archer Drained 一度不施放、Resistance gem 数值低、当前关奖励又不足以让角色 / 装备同步成长。不同环节应分别显示：承伤类型、未施放技能、有效 Resistance、装备等级差和失败后可刷取关。
- 官方承认：数个 buff / debuff 曾在显示时长结束后仍持续；修复后，原本按 bug 状态平衡的部分 Nightmare 关实际变难。它不是 hero stat 的直接 nerf，却会让玩家感到战力下降；下一补丁才计划回调关卡。
- Automatic Challenge 连败十次会退一关并关闭；Act Boss chest 满也会停止并回退。这是挂机安全阀，但如果只告诉“停止”而不展示十败、库存或最后有效结算，玩家仍难区分 build 失败与系统暂停。
- 游戏由服务器裁定。断网、维护或 session 失败时，画面可继续播放但奖励不一定提交；bug thread 要求上报时间以查 server log。战斗、奖励、宝箱、装备 / 附魔应共享一次可追踪 transaction id。

## 1.0.13→访问日生命周期

本 checkpoint 计十个 materially distinct negative / reworked families：

1. **持续时间修复反向抬难度**：Shield、Reinforce、Reflect、Fast、Drained、Ice Lake 与敌方效果曾过期不消失；修复后 Nightmare 清关时间和实际难度超预期，开发者承诺回调。
2. **target / range 使用过期目标**：技能曾不在自身 range 内重新选目标；官方同时补充技能 range 描述，说明空间规则与可读性一并缺失。
3. **高风险附魔破坏成长闭环**：失败原为 100% 毁装备，后降到 50%；绑定装备与稀有掉落让损失不可轻易 pivot。
4. **合成保底文案不清**：官方澄清累计十败后“下一次”才必成，失败计数 / 保底时点必须可见。
5. **自动挑战无限失败**：新增十连败退关并关闭，避免挂机持续消耗在不可过关卡。
6. **宝箱容量使自动流程停摆**：Act Boss chest 满会停止挑战并回退，capacity 不是纯仓库 QoL，而是 progression guard。
7. **Coin 节点从固定加值改读 base reward**：早期小固定值在后期几乎无效；1.0.13 和后续补丁连续重做 Coin 与 boss reward scaling。
8. **掉落 / 制作 / 装备等级错位**：玩家可拿到未解锁职业装备、制作材料和低于角色进度的 gear，形成理论有升级、当前却没有合法 owner / facility level 的 dead reward。
9. **元素墙缺少可靠反制闭环**：Nightmare 玩家知道要补 Resistance，却报告 socket 数值与爆发不匹配，只能以 Dodge / Life Steal 赌过；官方未提供当前敌方伤害 / 抗性预览。
10. **server transaction 与可见挂机分离**：连接、session、宝箱计时和本地设置问题会让画面在动但奖励未提交；离线 Coin / EXP 又改为 online base 的 85%，状态切换需要明确结算边界。

快速清波后 Boss 不生成、Drained 不进入 cooldown、星盘 Coin 节点回本慢和一般 UI / launch 问题保留在来源，但分别并入 ordering、duration、economy 与 transaction 家族，不额外灌水。Cumulative explicit negative/reworked cases：155。

## 对本项目可迁移

- **盾、元素与输出所有权正交**：Shield 的 supplier / HP reader、Cold 的 damage owner、全队 buff 与最终 carry 分开记录；名字相同或同队出现都不自动合并体系。
- **防御体系至少给两条输出出口**：一条是买 uptime 给另一个远程 owner；另一条是显式 converter 读取全队 bonus HP / Defense 给单核。后者必须占遗物 / 装备位并有 cap、刷新、失效与敌方 counter。
- **固定角色也能有构筑**：差异来自 skill slots、装备 / socket context、全队永久节点与资源集中，而不是一定要靠替换单位。
- **0 概率必须明示**：当 chest grade weight 为零，界面应告诉玩家“这里不会掉”，不要让挂机时长伪装成可达性。
- **damage meter 要带 source lineage**：至少显示技能 / buff / summon / gear / element、raw→mitigated→final、Shield absorbed、healing / overheal 与 downtime，才能判断给 Assassin 还是双远程继续投资。
- **自动化要解释停止原因**：十连败、箱满、断线、服务器拒绝和关卡回退是不同状态，不能共用一个“停止挂机”。

## 不兼容与未决问题

- 本项目是塔层单机 hero-roster autobattler，不采用 My Party Is Grinding 的 always-online、Steam Market、付费自动化或外观收集经济。
- 原作固定五职业、长期挂机和跨日 Coin 成本与本项目的有限 run / 招募替换节奏不等价。
- 未找到可核实的手动阵型 / 站位操作。只能确认 range、AOE、最大目标、Mobility 与技能目标复核；不能把“未发现”写成“原作绝对没有”。
- v1.0.15 只来自 MPIGDB 的数据复核说明；官方 1.0.13 之后未给下一明确版本号。两者不可无证合并。
- Assassin Dodge / Life Steal 对哪些攻击、技能、DOT 生效，元素 Resistance 公式、aggro 顺序、Shield stacking / refresh 与最终伤害归属仍缺。
- 无公开胜率、build usage、stage failure rate 或完整 combat log schema；440 条评论只用于筛选可复核的结构 / 抱怨，不构成统计。

## Disposition

`retained`。

理由：维护数据库提供规则 / owner，三篇独立实战攻略和一个具体视频 description 提供 practice，两条完整构筑覆盖 engine、state、payoff、survival、space、economy、pivot / counter；官方更新又给出 duration→difficulty、targeting、enchant、automation、reward 与 server transaction 的生命周期。它的固定队伍、元素 / 盾正交、单核资源集中和挂机失败解释对项目有价值。

不是 anchor：上线时间过短、官方版本号与维护数据快照不完全对齐，实践仍集中在早期 Nightmare，手动空间控制、敌方公式和统计缺失。

## 搜索停止与未计路线

- Steam 当前 440 条评测已按 API 分五页完整筛查；语言分布为简中 105、英文 83、日文 58、俄文 50、繁中 32、西语 31、巴葡 28、韩文 17，其余 36。只登记八条能改变 build / economy / counter / lifecycle 理解的评测；不把相似骂评累计成统计结论。
- 公开 Bug Reports 共 23 题、两页，全部打开；只登记四个有具体机制 / reproduction 的主题。两个 Steam Guide 中，一个只有 class unlock 图但图片不可读，另一个实际导向 Taskbar Hero，均不计。
- 七组 YouTube 查询得到六个相关视频；五个可见自动字幕轨的 timedtext 均返回空正文。只登记 `Ze8K6I7jiQc` 的公开 description，因为它明确写出五英雄、Nightmare 1-9、把 Knight 防装转给 Assassin 和 sustain 目的；其余标题、章节名、缩略图与未转录画面不作证据。
- Reddit JSON 返回 403，old Reddit 进入登录页；未绕过。Bing 日 / 英 / 中精确查询主要返回三篇已计攻略、两个社区站与重复 Steam / YouTube。Wayback CDX 不可访问，未绕过。
- `mypartyisgrinding.wiki` 是较早的单人汇总站，主要重述 Steam、成就 API 与旧讨论，并含明显模板残留；只用于路线核对，不与原始 Steam / MPIGDB 重复计数。Fandom 返回 403。
- 未加入私有 Discord，未购买 DLC，未下载 / 解包客户端，未从 screenshots、Steam Inventory 或 marketplace 反推战斗公式。继续枚举 2,802 个 MPIGDB 本地化 / entity 页面不会改变这两条 build 或当前 owner / counter 结论。
