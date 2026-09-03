# Storybook Brawl

## 身份、范围与结论

- `title_id`: `storybook-brawl`
- 开发 / 发行：Good Luck Games, LLC
- Steam App：`1367020`
- 类型：历史 PvP 自走棋；八人对局；Shop / Brawl 交替；Hero、角色、法术、升级与 Treasure 多层构筑。
- 公开状态：2021-06-18 进入 Early Access。2022-04-18 路线图仍把 public launch 写为未来事项；未发现正式版发布节点，Steam 元数据也仍为 Early Access。
- 最后实质规则节点：2022-12-09 `74.13`；最后公开补丁：2022-12-16 `74.16`。
- 终止：2023-04-25 宣布服务器于 2023-05-01 关闭。官方只说没有可继续前进的路径，不能推断具体商业或机械因果。
- 未上线边界：`Spooky Forest` 只在 2022-10-19 宣布计划于 2023 春发布；未找到发布节点，不进入规则、构筑或内容计数。
- 观察窗口：所有规则和构筑都按 EA 补丁分期。关服后评论只作历史回忆，不作 live meta。
- 深度：历史样本自适应深挖。48 个实质来源、20 条规范化证据、四套有明确时期边界的构筑。
- 结论：`retained`。它对 Treasure/装备式规则改写、死亡代理、召唤 occupancy、有限空间与构筑生命周期有高价值，但没有当前客户端、正式版或可验证末期统计。

## 来源包与搜索审计

| 来源层 | 数量 | 主要用途 | 关键限制 |
|---|---:|---|---|
| 官方开发说明 | 4 | EA、路线图、未上线扩展、关服边界 | 计划不等于交付；关服不证明因果 |
| 官方补丁 | 13 | 构筑 nerf、Slay/召唤/池/时序/owner 生命周期 | 每篇只代表对应 EA 节点 |
| 历史 Fandom 档案 | 17 | 基础循环、Shop、Treasure、单位与触发文字 | canonical HTML 403；仅使用可读 API 页，并按末修订日期限定 |
| 策略指南 | 3 | 新手经济、Hero 作业、PVDDR 站位 | 早期指南的共享池规则在 `71.20` 后失效 |
| 社区机制讨论 | 6 | Mage/Copyboy/Summon/Slay 的争议、反制和失败窗口 | 无采用率、胜率或代表性保证 |
| 详细评测 | 5 | 循环、节奏、扩张、末期收敛和关服后回忆 | 个人观察不升级为官方规则或总体统计 |

来源清单：

- 官方开发：`src-sbb-official-ea`、`src-sbb-official-roadmap`、`src-sbb-official-expansion-plan`、`src-sbb-official-shutdown`。
- 官方补丁：`src-sbb-official-patch-61-1`、`src-sbb-official-patch-63-3`、`src-sbb-official-patch-64-2`、`src-sbb-official-patch-65-10`、`src-sbb-official-patch-67-4`、`src-sbb-official-patch-67-5`、`src-sbb-official-patch-68-9`、`src-sbb-official-patch-70-6`、`src-sbb-official-patch-71-20`、`src-sbb-official-patch-73-2`、`src-sbb-official-patch-73-5`、`src-sbb-official-patch-74-13`、`src-sbb-official-patch-74-16`。
- 历史规则/角色档案：`src-sbb-wiki-gameplay`、`src-sbb-wiki-shop`、`src-sbb-wiki-treasure`、`src-sbb-wiki-timing`、`src-sbb-wiki-summoning`、`src-sbb-wiki-slay`、`src-sbb-wiki-copycat`、`src-sbb-wiki-good-boy`、`src-sbb-wiki-peter-pants`、`src-sbb-wiki-merlins-hat`、`src-sbb-wiki-crystal-ball`、`src-sbb-wiki-spell-weaver`、`src-sbb-wiki-great-pumpkin`、`src-sbb-wiki-bearstain`、`src-sbb-wiki-summoning-portal`、`src-sbb-wiki-echowood`、`src-sbb-wiki-magic-sword`。
- 策略：`src-sbb-official-positioning`、`src-sbb-steam-guide-beginners`、`src-sbb-steam-guide-heroes`。
- 社区讨论：`src-sbb-discussion-strategy-tips`、`src-sbb-discussion-mage-balance`、`src-sbb-discussion-copycat`、`src-sbb-discussion-good-boy-change`、`src-sbb-discussion-summons`、`src-sbb-discussion-trophy-overflow`。
- 详细评测：`src-sbb-review-deep-intro`、`src-sbb-review-tech-and-pace`、`src-sbb-review-scaling-critique`、`src-sbb-review-late-meta-convergence`、`src-sbb-review-post-shutdown`。

搜索到达饱和后停止：91/91 条官方 News、3/3 份公开 Steam Guide、300/300 个公开 Discussion 标题和 3,568/3,568 条 Steam Review 正文已筛。全站 472 个 Fandom 标题已枚举，但只登记逐页可读 MediaWiki API，并将其视为历史社区档案。Reddit 返回 403、Google challenge、Bing 同名噪声、DarkTwinge 超时、Untapped 历史统计不可验证，均未绕过。葡语空 Guide、不含正文的视频/标题、商店 API 和搜索摘要不计深来源。未下载客户端、视频或历史包。

## 基础循环、资源与 owner

每轮先在 Shop 用 Gold 买角色、买一次法术、刷新或锁定商店，也可通过 XP 提升等级；Gold 不跨回合保存。随后进入自动 Brawl。队伍最多七名角色，常见结构为前四、后三；攻击先手随机，角色按槽位与存活情况进入攻击/触发顺序。三份同名角色升级后，从与角色等级相同的 Treasure 层中选择一件；最多持有三件 Treasure，也可跳过换 Gold。

这形成五类不能合并的 owner：

1. Hero 拥有改变整局路线的规则，例如 Peter 固定等级、Trophy Hunter 改写触发类型。
2. Shop 拥有 offer、roll、lock、等级可见范围和 `71.20` 后的个人无限池。
3. 角色拥有攻击、属性、Good/Evil alignment、Slay 或 Last Breath。
4. Treasure 拥有折扣、法术补充、槽位加成或其他持续规则改写。
5. Spell 拥有一次 Shop 阶段的价格、目标与效果；Crystal Ball 只改写满足条件的法术链。

Gold 同时竞争角色、法术、roll 与 XP；pair/triple 还竞争当前 tempo。升级奖励不能只按角色变强计算，因为 Treasure 可能是整条构筑的 engine。反过来，追逐 Hat/Ball、Magic Sword 或召唤 Treasure 时，当前棋盘和生命值就是机会成本。

## 构筑一：Peter Pants HatBall

适用时期：`61.1` 后到 2022 末期历史档案；不能称 `74.16` 唯一 meta。

- **engine**：Peter Pants 永远停在 Level 3；每积累 3 XP，不升级，而让 Shop 中的角色成长。Merlin's Hat 让 Shop spell 减少 2 Gold；Crystal Ball 使定向 spell 不占每回合通常的 spell 权并补入新 spell。
- **state/resource**：Gold、XP 三点阈值、Level 2/3 pairs、Treasure 槽、spell 是否定向、spell offer、永久成长的 Shop 角色与 Spell Weaver 攻击。
- **payoff**：Spell Weaver 每次施法永久获得攻击，通常作为后排 Ranged payoff；Peter 的 Shop buff 同时维持低等级角色的战力。
- **survival**：低等级快速 pairs/upgrades、成长的前排和即时 tempo 购买，必须撑到 Hat/Ball 与 carry 同时到位。
- **spatial condition**：Spell Weaver 需要后排保护；对方 Lightning Bolt、Disintegrate 或 Doombreath 可直接攻击该 link。
- **payoff owner**：Peter 只拥有 Shop 成长；Hat 和 Ball 拥有经济/法术规则；Spell Weaver 拥有永久攻击与最终普通攻击。Aon 是 Level 5 角色，不能作为 Peter 普通 Shop 核心。
- **economy/pivot**：早期用 Level 2/3 配对与升级追 Treasure，但 roll、买角色、买 spell 和 XP 仍竞争同一回合 Gold。Blind Mouse、Treasure Map、Forking Rod 只是提高可达性或上限的可选模块。
- **counter/failure**：前中期 tempo、Crystal Ball 未到、法术经济耗尽、非定向 spell 不兼容、后排 reader 被先处理，都会使链条断裂。

`61.1` 把 The End 从 Level 3 移到 Level 4，官方明确原因是 Peter HatBall 获得过量治疗。2022-08 社区则同时存在“Crystal Ball 太强”和“无 Ball 强行 Mage 会在 Level 5 左右暴毙”两种观点。它说明高上限与高可达性风险可同时成立，不能把争议写成统计结论。

## 构筑二：Copycat + Good Boy

适用时期：按 `67.4` 加入 “other” 后的 Good Boy 文本闭合；更早自我回灌只作生命周期负例。

- **engine**：Copycat 攻击时，触发位于其身后角色的 Last Breath；Good Boy 死亡时，把自身 Attack/Health 给其他 Good characters。
- **state/resource**：Copycat 的攻击机会、身后连接数、Good Boy 属性、Good recipient 数量、slot 和先手。
- **payoff**：提前或重复触发一次大额团队属性供应，Echowood 可作为读取团队增益的 recipient。
- **survival**：Copycat 必须先攻击；其他 Good recipients 必须活到接收并使用增益。Good Boy 反而通常希望尽早死亡。
- **spatial condition**：Copycat 在 Slot 1 更容易先攻，但只能读取一个后排；Slot 2 可读两个，却更容易先死。故意空出 Slot 1 也有真实 body/tempo 成本。
- **payoff owner**：Copycat 只拥有代理触发；Good Boy 拥有属性供应；其他 Good 角色各自拥有收到的属性。Echowood 是 reader/recipient，不是供应源。
- **economy/pivot**：玩家必须在直接 Good Boy 死亡、Copycat 代理包、Good recipients 和 Treasure 模块之间取舍。Magic Sword +100 只提高 Slot 1 攻击，是直接 Good Boy 方案；不能无成本与 Copycat 双后排读取合并。
- **counter/failure**：Medusa、Cupid、Pigomorph、Smite、Doombreath、先手与后排访问分别攻击 supplier、proxy 或 recipients。资料没有可靠通用“沉默/位移”规则，不自行添加。

`67.4` 的 “other” 明确阻止 Good Boy 通过 Copycat、Trophy Hunter、Phoenix Feather 自我回灌。补丁后一周社区认为组合显著削弱，也观察到赛事缺席；这不等于证明构筑彻底死亡。对本项目最有价值的是 no-self-feedback、slot 冲突和 recipient lineage，而不是具体卡名或数值。

## 构筑三：Trophy Hunter + Grim Soul 历史 Slay

适用时期：`65.10` 明确让 Grim Soul 回归，至 `70.6` 再次移除。`70.6` 后不补造合法替代作业。

- **engine**：Trophy Hunter 让 Last Breath 同时成为 Slay；Grim Soul 的 Last Breath 触发一名角色的 Slay；Baba Yaga 倍增 Slay。
- **state/resource**：Last Breath/Slay 清单、死亡队列、倍增次数、Grim 合法版本与能活到触发的 payoff owner。
- **payoff**：Good Boy、Wretched Mummy、Friendly Spirit 等历史组件把代理 Slay 变成属性、伤害或其他 Last Breath 输出。
- **survival**：该 Hero/组件组合前期弱，必须以普通 tempo 过渡到多件组件同时可达。
- **spatial condition**：Grim Soul 的死亡顺序与 Baba/核心 reader 的保护位置决定链条能否完成。
- **payoff owner**：Trophy Hunter 拥有规则改写；Grim Soul 拥有代理；Baba 拥有倍增；具体 Slay/Last Breath 角色拥有最终效果。
- **economy/pivot**：只有 Hero、Grim、倍增器和有价值 payoff 同时出现才构成完整链；否则应留在普通 Slay 或 Last Breath tempo。
- **counter/failure**：Medusa、Pigomorph、控制、先杀倍增器或在成型前压血都能攻击不同 link。

`64.2` 首次因 Slay 过强切除 Grim Soul；`65.10` 的 `The State of Slay` 明确让它回归；`70.6` 又将其移除。官方明确说“保证 Slay”消除了应有的风险和戏剧张力。这是重要的设计反例：support 可以提高条件引擎稳定性，但若直接删除“攻击并亲自击杀”的核心门槛，原引擎会退化为确定性递归。社区标题里的“13 million”只作战报可读性案例，不作伤害公式或典型强度。

## 构筑四：The Great Pumpkin 召唤

适用时期：用 `73.2` owner/事务修复与 `74.13–74.16` 末期规则闭合；旧讨论只支持结构、反制和争议。

- **engine**：Great Pumpkin 的 Last Breath 按此前死亡的 Evil 角色产生低一级的原生 Evil 角色，并优先较高等级。Summoning Portal 对依次进入的召唤物加成；`74.13` 将其调整到 Level 5 且改为 +2/+2。
- **state/resource**：死亡 Evil ledger、原始 level、可用空位、召唤顺序、token/owned 标记、Portal 增益、Animal eligibility。
- **payoff**：第一波 Evil 死亡后，Pumpkin 产生第二波单位；Bearstain 放大 Animal aura/中途召唤 Animal，Echowood 可读取召唤期间获得的 stats。
- **survival**：Pumpkin 通常放在后排 Slot 7，等待更多 Evil 死亡；Bearstain 等 reader 也需要后排保护。
- **spatial condition**：召唤先尝试目标格，再同排最近空位，再另一排；全满则取消。高等级召唤优先进入有限空位，因此 board occupancy 是产出上限。
- **payoff owner**：死去 Evil 只提供 ledger；Pumpkin 拥有召唤；Portal/Bearstain 拥有各自 modifier；召唤物拥有自己的攻击。token eligibility、永久 owned 状态与 buff lineage 不能混为一谈。
- **economy/pivot**：先用 Evil tempo shell 生存；有足够 eligible deaths、Pumpkin 与保护后再加入 Portal/Bearstain。组件多、过渡不稳，不能把随机召唤当成最终稳定 roster。
- **counter/failure**：Dracula's Saber、Dragons、Cupid、combat spell、后排击杀、占满棋盘和错误死亡顺序分别攻击 ledger reader、空间或 aura owner。

`67.4` 曾把 Pumpkin 降到 Level 5；`67.5` 因短局、长战斗和决赛快速收敛而回滚。这不能证明所有召唤都过强，但说明“战略阶段过短 + 单战斗动画/触发过长”可以同时出现。

## 空间、池、Treasure 与时序

七格把“多上一个 body”和“保留触发空间”变成对立选择。Copycat 的 slot、Good Boy 的死亡顺序、Pumpkin 的保护位、召唤 vacancy 和 Magic Sword 的 Slot 1 都说明：空间不是被动加成容器，而是触发访问权。故意空位有机会成本；满盘会取消召唤；后排并不天然安全。

`71.20` 从共享有限池改为每名玩家独立的无限池。此前关于 copy counts、卡对手和共享稀缺的指南判断失效；health、tempo、roll 与 pair 风险仍可保留。玩家仍能预览对手并用 tech/站位应对，但 preview 不再证明对方从全局池拿走组件。

Treasure 最多三件且可跳过换 Gold。Hat+Ball 已占两个长期槽；Magic Sword、Portal 或其他上限件会与之竞争。升级因此同时创造 body strength、Treasure reachability 和继续追 pair 的诱惑。项目若做“遗物让全队额外生命/防御转给一个射手”，同样要让 converter 付出真实 slot/reward 机会，而不是给防御体系免费补伤害。

触发层在 `61.1` 已加入 infinite-loop detection；`71.20` 又大规模修复同时死亡、召唤和空位仲裁，`73.2` 修 owned non-token/token、复制合并和 quest 事务，`74.16` 仍有召唤/顺序与界面修正。固定 RNG 或固定 slot 不能自动带来可读性；战报还必须保留 root event、source、owner、queue order、descendant、vacancy、canceled branch 与 recursion guard。

## 盾、元素与输出转换的启示

Storybook Brawl 本批资料不提供一套“盾体系”或“元素反应体系”，也没有证据支持 Ice Shield / Earth Shield。它的价值在于验证更底层的设计语法：

- 防御 shell 可以只负责 survival，最终输出由另一个后排 carry 拥有；Peter 的成长前排与 Spell Weaver 就是不同 owner。
- Treasure/Hero 可以充当具名 converter：Hat/Ball 改写法术经济，Trophy 改写触发种类，Copycat 代理 Last Breath。转换不是原始资源的默认属性。
- team-stat-to-carry 必须声明供应者、读者、接收者、自身是否排除、何时读取、是否读取 descendants、slot、cap、duration、refresh 和 counter。
- Good Boy 的 `other` 修复说明“供应者再次读取自己产生的增益”是高危递归；项目的全队额外生命/防御转换也应阻止自反馈或重复计算。
- Pumpkin/Portal 说明 summon 数量、occupancy、token eligibility、永久所有权和 buff lineage 至少是五个不同问题。
- Slay 生命周期说明支援组件应改善条件引擎，而不是删除引擎的定义性风险。

因此盾体系和元素体系仍应视为两条独立主轴。盾先回答如何生成、维持、破坏和换取生存；元素回答状态供应、读取、传播、抗性与净化。遗物、装备或 Hero 才能把它们桥接到伤害，而且桥接 owner 必须可被替换、克制和战报解释。

## 生命周期与负案例

本 checkpoint 计十四个 materially distinct negative/reworked families：

1. EA、未来 public launch、计划扩展、最后补丁与关服状态必须分离，计划内容不能冒充已上线。
2. `61.1` 把 The End 移出 Peter 可达层，阻止 HatBall 过量治疗，说明 Hero 等级边界也是平衡 guard。
3. Crystal Ball 同时出现“过强”和“无 Ball 强行 Mage 会暴毙”的社区意见，强度与可达性不可混成一个结论。
4. Good Boy `67.4` 加入 “other”，切断供应者自我回灌；递归 guard 改变 Copycat/Trophy/Phoenix Feather 多条线。
5. `67.4` Pumpkin Level 5 实验在 `67.5` 快速回滚，原因涉及短局、长战斗与决赛收敛。
6. Grim Soul 因保证 Slay 两度移除；support 删除定义性风险会压扁引擎身份。
7. `68.9` 切除 Dwarven Forge并提高 Tweedle 层级，升级加速同时支付经济、Treasure 与中期战力会过度捆绑。
8. `71.20` 从共享池改为个人无限池，使早期 copy-denial 与共享稀缺策略失效。
9. `71.20` 同时死亡/召唤 resolver 重构，说明确定战斗仍可能存在不可读时序与实现缺陷。
10. `73.2` owned non-token、token、复制合并、quest progress 和召唤落位修复，暴露持久 owner 与临时 body 的事务边界。
11. `73.5` everywhere buff/alignment/transform 规则澄清，tag eligibility、当前状态和来源 lineage 不能靠名称猜测。
12. Pumpkin/Portal 的组件负担、后排脆弱和满盘取消说明理论召唤上限不等于实际可达产出。
13. 大数值 Trophy 截图需要玩家手工重建 Grim/Baba/Last Breath 链，最终数字不能替代 source attribution。
14. 关服后评论的 meta/content 看法与官方“no path forward”不能拼成未经证据支持的商业或机械因果。

一般价格、审美、公司争议、评论分数或关服后情绪没有拆分凑数。Cumulative explicit negative/reworked cases：264。

## 对本项目可迁移

- 将体系写成事件链：supplier → state/ledger → reader/converter → recipient → final event，而不是“盾流/元素流/召唤流”标签。
- 遗物与装备适合拥有规则改写，但必须支付 slot、获取、替换和 reachability 成本。
- 团队防御转单核输出可行，前提是 converter 明确读取集合、排除自身反馈、声明 recipient 与刷新窗口，并保留前排生存和射手输出两个 owner。
- 触发代理应保留原条件的风险。Copycat/Grim Soul 表明“提前一次”“复制一次”“把条件变为无条件”是三种完全不同的强度预算。
- 召唤必须同时设计 formation vacancy、临时人口、token 权限、死亡计数资格、属性继承和超量取消。
- 敌人或 encounter counter 应攻击不同 link：先手压成型、后排访问、变形 supplier、占位限制、反召唤、spell interruption，而不是统一加生命。
- 复杂连锁的验收对象不仅是最终数值，还包括 event order、owner、source lineage、guard 与取消原因。

## 不兼容与未决

- 不复制具体 Hero、角色、Treasure、Spell、等级、槽位、Gold 数值或 PvP 八人结构。
- 不把 2021 共享池、旧 Timing 页、Pre-`67.4` Good Boy、`70.6` 后 Grim Soul 或计划扩展写成末版规则。
- 没有可验证末期 build usage、win rate、完整卡池/商店 odds、正式版或当前客户端。
- Fandom 是关服前历史档案；页面末修订日不是“仍维护当前数据库”的证明。
- 社区关于 Mage、Summon、Copyboy、Slay 与 meta 收敛的意见只证明存在争议或实践，不证明总体强度。
- 关服原因未知；不能从时间先后推断机械、商业、公司或内容节奏因果。
- 项目第一版盾/元素/遗物方案仍需后续用户确认；本 dossier 只提供结构、guard、counter 与 owner 证据，不授权内容或数值。

## Disposition

`retained`。

保留理由：48 个实质来源满足规则、实践、补丁、历史档案、社区和详细评测的多类型交叉；四套构筑都能闭合 engine、state/resource、payoff、survival、space、owner、economy/pivot 与 counter，并提供 Good Boy 自反馈、Grim Soul 条件删除、Pumpkin occupancy、Pool ownership 和 trigger readability 等少见生命周期证据。

停止理由：公开文字表面已被系统性筛完；继续枚举评测只会重复 Mage/Slay/Summon 争议，无法补出末期统计、正式发布、未上线扩展或内部 resolver。访问受限路线不绕过，也不下载客户端/视频/历史包。故作为 historical `retained` 收束，而不是 anchor 或当前 meta 数据集。
