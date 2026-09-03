# Guildrun

## 身份、时期与资料密度

- `title_id`: `guildrun`
- 主 Steam App：`3669200`；开发 / 发行：Leyline；截至 2026-09-03 仍为 `To be announced`。
- 独立公开 Demo App：`4425970`，2026-07-16 发行。官方补丁从 `0.5.1.718`（2026-07-23）连续到访问日最新的 `0.5.7`（2026-09-01）。
- 维护型 `guildrun.wiki` 明确锁定 Demo `0.5.6`、build `ff633149`，由 Larsonix / The Wiki Guy 从 shipped game data 整理；它不是官方规则书，0.5.7 差异以官方补丁校正。
- 本 checkpoint：22 个实质非商店来源——8 个官方发布 / 补丁节点、5 个维护 wiki 页面、2 个完整实践攻略、4 个 Steam 实战讨论、3 个详细长评。
- 置信：高。经济、职业、目标选择、模式、连续补丁和多套构筑均能跨功能来源互证；但当前精确英雄 / 物品全集、完整内部胜率与 0.5.7 全量数据表未公开，因此是 `retained`，不提升为 anchor。

## Adaptive-depth 决定

Guildrun 的机制密度和本项目相关性都很高：英雄升级会改职业，后备席可通过 Backup 继续拥有战斗收益，Shield / Armor / Poison / Frost / Stealth / Shard 各有不同转换器，敌人又用 pull、teleport、furthest selector、random snipe 与控制检验站位。Red Rift 和 Endless 还使用不同胜利目标、商店窗口和成长压力。

因此本轮没有按普通长尾的最低一套 build 即停止，而是闭合四套 Endless 结构、两套实战 pivot 和一条 Red Rift 路线，并逐条用 0.5.1–0.5.7 校正。继续搜索已开始重复相同攻略、Steam 讨论和无可读字幕视频；全英雄、全 modifier、全 relic 目录不会再改变体系边界、owner、反制或生命周期判断，故按 diminishing-return test 停止扩展。

## 来源包

- `src-guildrun-launch-2026-07-15`：Demo 发布与 closed-alpha lineage。
- `src-guildrun-patch-0-5-1` 至 `src-guildrun-patch-0-5-7`：公开 Demo 从 0.5.1.718 到 0.5.7 的连续规则、平衡与修复链。
- `src-guildrun-wiki-about-0-5-6`：维护方法、shipped-data 边界、build `ff633149`。
- `src-guildrun-wiki-economy-0-5-6`：Regular / Auction House、Shards、reroll、Freeze、pity、卖回、升阶与队伍人数。
- `src-guildrun-wiki-targeting-0-5-6`：距离、职业、生命 / Defense 评分、GUID tie-break、Taunt、Stealth、路径和敌方特殊 selector。
- `src-guildrun-wiki-modes-0-5-6`：Endless、Red Rift、Challenge 的目标、缩放和限制。
- `src-guildrun-wiki-rank-modifiers-0-5-6`：职业池、Backup、Rush / Stall、Shield Power 和状态转换。
- `src-guildrun-steam-guide-red-rift`：首发时期完整 Red Rift 经济、职责、任务、Key 与 Boss 路线。
- `src-guildrun-grind-endless-guide-0-5-2`：0.5.2、约 35 小时测试的四套 Endless 构筑、弱点和失败例。
- `src-guildrun-discussion-mystics-2026-08-23`：Fiona、Grace、Gustav、Mana Regen、Backup 与高阶 Mystic 替换争议。
- `src-guildrun-discussion-tank-burst-2026-08-27`：Rowan 启动窗口、Yuuna / Dragomir 分支、Omnivamp、backline access 与 aggro 分担。
- `src-guildrun-discussion-poison-2026-07-18`：Poison rider、max-HP / on-hit 转换、Defense→Poison 与 Endless 衰减争议。
- `src-guildrun-discussion-positioning-2026-08-29`：反向站位、全前排、左右顺序、furthest / teleport / AOE 反制。
- `src-guildrun-review-pivot-friction-2026-07-29`：高难度预升阶锁定、两败退出、商店搜索和 Auction House 转型摩擦。
- `src-guildrun-review-position-backup-2026-07-19`：Red Rift 站位、backline access、stunlock、后备投资与遭遇重复。
- `src-guildrun-review-endless-resource-gap-2026-08-07`：Endless 失去常规商店、关键 relic 近似必需和 reroll agency。

## 真实循环、供给与职业所有权

准备期的 Regular shop 每次提供三名英雄、两件物品和一个 relic，其中一项随机获得 25% 折扣。reroll 从 1 Shard 起并逐次加一；Freeze 把当前供给带到下一家商店，但会影响 pity。英雄和物品可按 66% 卖回，relic 不能出售。Boss 后的 Auction House 使用更高的起始刷新成本，不能 Freeze，因此既是补强窗口，也是昂贵且不可延期的 pivot 检查点。

英雄从 C 升到 B、A、S。B 阶从三条固定 specialization 选一；A / S 从当前 active class pool 抽 modifier。dual-class 会拆分候选池，所以“转职业”同时改变后续成长的可达性，不只是加一个标签。战场初始三人、后续最多五人，roster 最多六人；第六人通常在 reserve，但带 `Backup` 的 modifier 能从后备席继续提供明确效果。后备因此不是免费仓库：它占招募、升阶、装备和机会成本，却可能拥有战斗 payoff。

这套结构把 owner 分成四层：英雄拥有基础技能和装备；职业 / specialization 决定 modifier 池；reserve hero 只在具有 Backup 时拥有远程收益；relic 属于团队规则层。它不支持把“全队都有盾 / 元素”自动理解为“全队都拥有同一伤害转换”。

## 构筑一：0.5.2 Kai 高频护盾正反馈

The Grind Reporter 的 0.5.2 Endless Guide 给出一条明确的 shield engine：Kai 每次收到一份独立 Shield 就永久获得 Attack；Warrior 的 `Shield Power` 再把当前 Shield 转成 Attack。Aria、Gustav、Pollen负责高频护盾，Ming可提供开场保护；若取得 Grace B-rank Backup，Mystic 的 heal / shield 还会给 Attack 与 Magic。

- **engine**：多个施放者以更高频率向 Kai 交付独立 Shield 实例；Mana Regen 提高事件次数，而不是只追求单次最大盾量。
- **state/resource**：Kai 的永久 Attack、当前 Shield、各 support 的 Mana / 施法频率、Mystic / Warrior modifier、场上与后备席位。
- **payoff**：Kai 是主要物理输出 owner；Shield Power 是当前盾量到 Attack 的额外转换；Grace Backup 另给 Attack / Magic，不把所有 support 变成 carry。
- **survival**：护盾本身吸收伤害；Ming保护启动期；高频小盾兼顾存活和 Kai 的叠层次数。
- **spatial condition**：Kai 需要避开开场集火和后排 selector，shield caster 必须在其施法 / 目标规则内存活；不是仅看总盾量。
- **economy / pivot**：早期可先用任意可靠 frontline 与盾源，确认 Kai、Mana Regen 与关键 modifier 后再把装备集中到 Kai；Grace 可以在 reserve 提供 Backup，释放一个战场席位。
- **counter / failure**：引擎启动前爆发、backline access、控制或击杀 shield caster 会截断正反馈。若只有大盾而事件频率低，Kai 的永久 Attack 增长也慢。
- **version**：0.5.2 将 Shield Power 从每 10 Shield 改为每 8 Shield；0.5.6 又修复其 AD bonus 未正确生效。旧指南能证明结构，不能证明 0.5.7 精确强度。

这是一条“盾体系打伤害”的实证路径，但关键不是盾天然输出，而是三个离散 owner：护盾源负责 survival，Kai 读取“收到盾的次数”成长，Shield Power 读取“当前盾量”转换。两条读数可以独立调节和被反制。

## 构筑二：Armor / Stall Endless

0.5.2 Guide 的防御路线以 Dragomir / Yuuna / Hoyoung 提供早期经济，以 Pimenta、Zuri、Tank Gustav 或 Skorn 扛住过渡；拿到 Rowan B-rank Backup 后，防线才进入持续成长。Skorn 把 Armor 变成伤害，Poison / Frost 的开场状态可触发防御 modifier，后期由 Riftbreaker + Storm 提供结束战斗的吞吐。

- **engine**：早期经济英雄扩大招募 / reroll 空间；Rowan Backup 与 Stall modifier把拖长战斗转成防御成长；Skorn读取 Armor 输出。
- **state/resource**：Shards、队伍阶级、Armor、战斗时长、Stall 层、开场 Poison / Frost 状态和后备席。
- **payoff**：Skorn 或后期 Riftbreaker / Storm 是 damage owner；Rowan 是 reserve scaling owner；Armor 自身仍首先是 survival state。
- **survival**：多名前排、Armor 与持续回复争取 Stall 启动时间。
- **spatial condition**：前排必须稳定分担 aggro，后排终结者避免被 teleport / furthest selector 点名；reserve Rowan 不承担格位风险。
- **economy / pivot**：经济单位是桥而非最终阵容；拿不到 Rowan、Skorn或终结者时必须及时转向普通 tank + DPS，而不是继续为未来 Stall 牺牲当前强度。
- **counter / failure**：Rowan 太晚、经济落后、启动前爆发或后排被点会让纯堆防御没有结束能力；这正说明 survival engine 和 payoff owner 必须分开验收。
- **version**：具体英雄 / modifier 数值来自 0.5.2；0.5.5 后 Stun Resistance 与其他改动改变了长战控制环境。

## 构筑三：Stealth Assassin Endless

Assassin 路线用职业行为产生 Shards，再把经济回流到 Stealth、Attack 与 Rush。Ming 延长敌人无法有效接触核心的窗口，使 Assassin 获得击杀和滚动资源的时间。

- **engine**：Assassin 产 Shards并购买 / 升阶下一轮的隐身、攻击和先手组件。
- **state/resource**：Shards、Stealth 时长、Rush、Attack、击杀节奏和 Ming 的控制 / 保护窗口。
- **payoff**：Assassin 是后排击杀和经济 owner；Ming 是时间窗口 owner，不拥有 Assassin 的伤害。
- **survival**：Stealth 通过暂时不可选中保护脆弱 carry，而非提供常驻减伤。
- **spatial condition**：需要进入后排并避开随机或已经锁定的 projectile；起手站位决定最初目标和路径。
- **economy / pivot**：前几轮击杀 / Shard 回报必须足以支付升阶与刷新；收益不成形时应转为已有高阶单位，而非继续追隐身件。
- **counter / failure**：Stealth 到期后阵容会迅速崩溃；Mushroom Archer 的随机 Snipe 可穿 Stealth，已发射 projectile 也会继续追踪进入 Stealth 的目标。

## 构筑四：Shard Damage 诱饵路线

0.5.2 Guide 测试 Nyx、Tilly、Irini、Ming、Rip、Fiona，利用 Golden Eye、高频 Shard 触发与 max-HP damage relic 构造大量事件。表面上资源频率很高，但实践报告发现对应伤害仍受 Defense / Armor，Endless 后期会衰减。

- **engine**：多英雄与 Golden Eye 提高 Shard 生成 / 消费事件频率。
- **state/resource**：Shards、触发次数、目标最大生命、Defense / Armor、relic 占位与商店机会。
- **payoff**：具体 max-HP / on-Shard 转换器拥有伤害；Shard 本身只是资源，不是 true-damage owner。
- **survival**：Fiona / Ming 提供护盾与控制窗口，但不能修复伤害被防御压缩的问题。
- **spatial condition**：高频 trigger 必须落到仍可攻击的目标；被点杀的转换 owner 会让资源无处兑现。
- **counter / failure**：高 Defense / Armor 直接压缩 payoff，进入 Endless 后敌人成长快于转换收益。它是“引擎看似在线、转换层却失效”的优秀失败样本。
- **readability**：若 tooltip 只强调最大生命百分比而不说明伤害类型和减伤顺序，玩家容易误读成 true damage。

## Red Rift 实际路线

首发 Guide 的优先级是 `生存 > 完成任务 > 后期构筑`。0.5.2 又把首店 Shards 调到 30，使“买两名英雄、尽早升到 B”成为更明确的开局。common item 是过渡件和可按比例卖回的小银行；阵容用 tank + DPS + sustain，或 tank + 2 DPS + teamwide shield / heal。Key 应在最后一个仍可用的商店购买，避免过早冻结本局战力。

Red Rift 的任务和死亡约束使“理论终盘阵容”不等于当前正确选择。玩家需要先用便宜前排 / 治疗过任务，再根据出现的高阶英雄和 modifier 移交装备。旧攻略曾建议 Frost 处理 Act 2 Frost Dragon；0.5.4 后该 Boss 获得 50% Frost resistance，官方又明确完整 Debuff 重做留到完整版，因此旧建议必须降级，不能作为当前固定答案。

## 两条实战 pivot

### Yuuna / Dragomir：谁先升阶，谁拥有路线

0.5.6 社区讨论给出一条不是固定卡表的分支：Yuuna 先升阶时，以 Yuuna 为 carry，Dragomir 转 Vanguard support；Dragomir 先升阶时，以 Dragomir Mage 负责经济 / 输出，Yuuna 暂时进入 reserve。Stealth、Omnivamp、Crit Defense、backline access 和分担 aggro 决定哪一侧能活到输出。

升阶顺序在这里就是真实的 path dependence：先出现的 specialization 改变 A / S modifier pool、装备 owner 和 reserve 成本。后续拿到另一名英雄，不代表可以无损交换已经升阶和装备的核心。

### Fiona / Mystic：盾持有者到后备辅助

Fiona 前期可用盾和 Vanguard 路线稳住战场，后期取得高阶 Mystic 后，已有 Mana Regen 装备可转给 Grace、Gustav等更强的施法 owner，Fiona再转 Backup。社区对 Fiona 本体强度存在争议，因此这里只保留“装备可迁移、职责可转型”的结构，不把她写成公认最强或最弱。

## Poison、元素与盾的正交关系

首发 Poison 讨论区分三件事：Poison 的直接伤害；在 Poison 目标上附加的 effect rider；读取 max HP、on-hit 或 Defense 的转换器。一个单位能用 Defense→Poison 建立防守资源到状态的桥，另一个 relic / modifier 再读取 Poison 输出；这不意味着 Poison、Defense 和 Shield 是一条单一体系。

Frost 同样是状态 / 控制轴。0.5.4 给 Red Rift Boss 临时加 Frost resistance，并明确把完整 Debuff 重做留给完整版，说明“给 Boss 单点抗性”是症状处理，不是元素体系已经闭合。盾则首先是 survival state；只有 Kai、Shield Power、Grace Backup 等明确 reader 出现时，盾才成为 Attack / Magic 的 engine。

对本项目更可迁移的表达是：

> 防御 chassis（Shield / Armor / Health） × 状态 axis（Frost / Poison 等） × reader / converter × 明确 payoff owner

冰盾和土盾可以共享“盾”的承伤语义，同时由元素决定状态、来源或敌人交互；但不能因为都叫盾就自动共享输出公式，也不能因为带元素就取消盾的防御职责。

## 目标选择、空间反制与可读性

0.5.6 wiki 给出的普通目标排序为：距离 → class priority → `log2(current HP) + Defense / 100` → GUID tie-break。目标选择和寻路是分离步骤；路径不可达可能触发 fallback，但不会把两者混成一个评分。projectile 锁定后，即使目标进入 Stealth，也可能继续命中。

敌人使用的空间反制不是单一“刺客切后排”，而是一组 selector：pull、teleport、furthest stun / poison、random snipe 与 AOE。玩家实践相应使用反向站位、全员前排、左右顺序、给后排 access、reserve swap 和分担 aggro。该公式适合作为解释和 QA 证据，不应要求普通玩家记忆 GUID；界面至少要显示“为何被选中、谁已锁定、移动后会否重选”。

## 失败、反制与生命周期

本 checkpoint 计九个 materially distinct negative / reworked families：

1. **Boss Token 稀缺**：0.5.2 改为每到 Boss 获得两个；官方仍称临时方案，说明元进度 / 进入成本尚未定型。
2. **Emergency Rewind 事务错误**：重充时错误累积 Shards，并存在 result-screen softlock；修复目标是回滚与奖励只结算一次。
3. **Frost 系统失衡的 Boss band-aid**：0.5.4 给 Red Rift Act 2 Boss 50% Frost resistance，同时把完整 Debuff 重做推迟到完整版。
4. **自动攻击递归**：多个“额外触发一次自动攻击”曾可相互递归，0.5.4 增加 guard；事件型复制必须有来源 / 深度 / 每事件上限。
5. **Endless stunlock**：0.5.5 增加可堆叠 Stun Resistance 和施法期免疫，处理长战中永久失去行动。
6. **Outbreak Banner 胜率异常**：Duelist 版本高出其他 banner 近 10% 胜率，0.5.5 重做；这是内部数据支持的局部失衡，不推广为所有模式结论。
7. **同帧死亡 / 复活 / aggro 完整性**：0.5.5 修复伤害分配、同帧死亡、复活状态与 aggro 异常，说明死亡队列和目标重验必须原子化。
8. **商店 / 延迟奖励 / tick 来源错误**：0.5.6 修复 frozen shop 重复、延迟奖励来源不明和 DoT 以 58/60 速率 tick 等问题；交易与战斗来源都需要稳定 identity。
9. **DoT / immunity / crit / on-damage 顺序**：0.5.7 继续修复结算顺序、伤害免疫与暴击互动、on-damage 触发和初选保存；规则链不能只靠 tooltip 局部正确。

Shard Damage 被误读为 true damage、Endless 失去常规商店导致关键 relic 锁死、reserve 投资难回收，是重要的策略 / 可读性负面证据，但不额外机械拆分计数，避免把同一资源 / 转型问题灌水。

## 对本项目可迁移

- **先定体系的 engine，再定输出 owner**：Shield 可以负责承伤；Kai、Skorn、射手或 relic 才负责把“收到盾次数、当前盾量、Armor、全队额外生命”等状态转成伤害。
- **元素与防御是正交轴**：冰盾、土盾共享 Shield 规则，同时让 Frost / 地系决定不同的状态、来源或反制；用稀缺 reader 连接，而不是为每个元素复制整套盾体系。
- **全队转单核必须收费**：若装备让持有者读取全队额外生命 / 防御转 Attack，应占一个核心装备位、明确读取范围、快照时点、上限和死亡 / reserve 边界，并允许敌人通过破盾、百分比伤害、后排 access 或禁疗攻击不同层。
- **高频与大数值是两个旋钮**：Kai 读取护盾实例次数，Shield Power 读取盾量。两者分开后，Mana Regen、高频小盾和单次大盾形成真实选择。
- **后备收益必须归属清楚**：Backup 可以释放格位，但仍消耗招募 / 升阶 / 装备；需要标注 reserve hero 是否可被攻击、是否能吃团队增益、死亡 / 替换后效果何时移除。
- **目标与寻路分开解释**：先显示选中理由和锁定状态，再显示路径；projectile、Stealth、teleport 与重选必须有确定顺序。
- **模式必须保留决策窗口**：Endless 若切走常规商店，关键 relic 不能成为进入前未提示的单点失败；应提供替代转换器、定向补件或明确的“最后采购”警告。
- **递归、死亡与来源是设计规则，不是纯技术细节**：on-attack、DoT、immunity、crit、同帧死亡、复活、延迟奖励都要有事件 id、深度 guard、稳定 source 和战报归因。

不可直接迁移：Guildrun 的具体英雄 / modifier / relic 数值、固定三至五人战场、六人 roster、Steam Demo 的 Red Rift 任务表、隐藏 pity 权重、GUID tie-break，以及任何旧攻略的强度排名。

## 未决问题

- 0.5.7 是否改变了 0.5.6 wiki 中未在补丁说明列出的隐藏权重、目标参数或 modifier 文本。
- 公开完整版本会如何重做 Frost / Debuff、Boss Token 和 Demo 期间的临时抗性。
- 当前完整英雄、物品和 relic 池的胜率 / 采用率；现有内部数据只披露个别差异。
- Shard Damage 的最终伤害类型、Armor / Defense 结算顺序是否会在 UI 中显式说明。
- Endless 后续是否恢复商店、提供定向 relic 兜底或保留新的适应窗口。
- reserve 的全部可见规则、投资回收和换位成本；当前资料足以确认 Backup，却不足以定义完整替换契约。
- 普通玩家界面是否能解释 class priority、锁定 projectile、Stealth 穿透和特殊敌人 selector，而无需查询数据 wiki。

## Disposition

`retained`

它满足并显著超过规则＋独立实践门槛：22 个功能不同的来源闭合了护盾、Armor、Stealth、Shard、Poison、经济、职业、reserve、站位、模式与连续版本史。多套构筑都有 engine、resource、payoff、survival、space、owner、pivot、counter 和版本限制；九个 lifecycle family 也有官方修复链。它仍不是 anchor，因为主游戏未发行、维护 wiki 停在 0.5.6、实践集中于 Demo 早期且缺少公开完整统计。
