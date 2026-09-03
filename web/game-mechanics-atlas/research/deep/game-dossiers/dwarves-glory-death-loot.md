# Dwarves: Glory, Death and Loot

## 身份、版本与研究深度

- `title_id`: `dwarves-glory-death-loot`
- 当前 Steam 身份：开发者 `Hamma Studios`；发行商 `Sidekick Publishing`、`Gamersky Games`。官方 Steam News 的作者账号仍显示为 `ichbinhamma`，仅作为新闻署名与早期公开身份，不替代当前开发 / 发行字段。
- Steam App：`2205850`。
- Early Access：2023-08-17；正式版：2026-01-22，对应 `v2.0.0`。
- 当前可核实公开主版本：`v2.1.2`（2026-08-12）；本 dossier 只对 `v2.1` 系列的官方规则和失效节点作当前判断，没有找到 `v2.1.1` 修复后的合法完整构筑。
- 分类：long-tail；最多十人的逐步扩编 roster、装备驱动、带长线成长与 Boss 适配的单机 autobattler。
- 研究深度：adaptive-depth `retained`。资料足以闭合一套历史 `v2.0` Priest/Thorns 构筑，并记录一套在 `v2.1.1` 被作者明确判定不可执行的 Crow/Warlock 失效结构；Shield/Block/Thorns/元素状态的 owner 边界、经济、阵型、Boss 与生命周期仍可交叉。维护数据库未同步 `v2.1` 新 Rune，且修复后合法 Crow 招募成本与可达性没有实战支持，因此不升为 anchor，也不声称存在当前闭环。

必须把版本层分开：`v1.6` 用 Rune Circle 完全替换 Skill Tree，`v1.16` 又重做 Rune Circle，`v2.0` 扩充 Artifact/Rune/锻造，`v2.1` 再重做十个 Rune、多个 Artifact、技能与职业。旧攻略可证明结构和历史实践，不能直接证明 `v2.1` 强度。

## 检索日志与停止理由

- 官方：Steam News API 共 87 条，已全量扫标题并深读 Rune Circle 两次重做、战斗时长、正式版前 Artifact/Rune/锻造专题、正式版、`v2.0.10`、`v2.1` 与 `v2.1.1`。
- 数据库：读取官方论坛置顶的 Dwarves Companion 的 Classes、Stats、Formations、Runes、Items、Forge、Tavern 与 Enemies 页面。页面能交叉具体职业、状态、Artifact、锻造和 Boss，但缺少 `v2.1` 新 Rune 名称，按约 `v2.0.10` 快照使用；其 AI Innkeeper 的建议不计证据。
- Guides：10/10 Steam Guide 标题已扫；保留当前 Formations 2026、Season 1 Dark Magician、已明确过期的 Gem Farmers 历史路径、2026 俄文基础与 Boss 指南。旧 Guide 均显式标版本，不跨期宣称强度。
- Discussions：20 页、287/287 个公开标题已扫；保留十帖，覆盖 Season 5 上线即失效的 Crow 路线、当前 Endless、Forge Demon、Block、Thorns、INT/元素、Unique Rune、Holy/Dark resistance、Weight 与新手陷阱。另对 2026-08-11 后的 Crow/Warlock/Conquest/Veteran/Fear 结果和热修后 Reviews 作定向复查，未找到合法完整构筑。
- Reviews：Steam API 汇总 2702 条，分页获得 2698 个去重正文；筛出 201 篇长正文并保留七篇会改变机制、经济或失败理解的评测。四条未返回正文不补造。
- 视频：八个中英俄查询族得到 137 个去重候选，深查 17 个指南/构筑/Boss/Season 5 页面；页面虽暴露 caption track，但 timedtext/json3 均为空，故标题、缩略图、画面数值与无字幕构筑不计证据。
- 外部：保留 Savior Gaming 与 XPN Network 两篇可读长评。Game Critix 把本作误写成动作地牢并包含不存在机制，排除；What's It Like? SSL 失败且不绕过；Bing/DuckDuckGo 后续路线噪声或 shell 较多。

最终注册 43 个实质来源：11 个官方版本/机制节点、8 个维护数据库页面、5 篇 Steam Guide、10 个机制讨论、7 篇详细评测和 2 篇外部长评。继续检索主要返回重复商店简介、无字幕 run、旧版数值或无法核验的 AI/同名内容，已不再改变 owner、pivot、counter 或 lifecycle 理解，因此停止。

## 来源包

| 组别 | 数量 | 主要用途 | 关键限制 |
|---|---:|---|---|
| 官方机制 / 补丁 | 11 | 版本边界、Rune/Artifact/锻造、战斗时长、`v2.1` 重做 | 不提供完整 current 数值表或采用率 |
| Dwarves Companion | 8 | 职业、状态、Formation、Rune、Artifact、锻造、Tavern、敌人 | 主体约为 `v2.0.10`，不能覆盖 `v2.1` |
| Steam Guides | 5 | 阵型、历史 Endless/成长、2026 基础与 Boss 实践 | 多数早于 `v2.1`，结构可用、强度不可跨版 |
| Discussions | 10 | 失效 Crow 路线、Boss pivot、Thorns/Block/元素公式边界与认知问题 | Crow 原作者已撤回修复后可执行性 |
| Detailed reviews | 9 | 长线成长、商店稀释、Formation、Endless、负担与失败体验 | 体验样本无控制变量，不能单独证明因果 |

## 基础循环、成长与真正的选择

玩家逐步扩编并维护最多十名矮人，为每人选择装备、职业、Rune、Artifact 与 Formation；战斗自动执行，但战前编队、装备 owner、Boss 抗性与点名适配决定结果。金币、锻造材料、Potion、Veteran、退休后的 Gem 与 Artifact 成长跨战斗影响队伍。Tavern/商店的刷新、招募、替换与装备池使“保留现有成长者还是换新职业/更高潜力单位”成为主要机会成本。

成长不是同一个池：基础属性与 Veteran trait 属于具体矮人；Growth Potion/Blueberries/Kakapo Egg 等按各自条件积累；Rune Circle 是 clan 级元进度；Artifact 有品质、升级与佩戴 owner；锻造配方把装备、材料和目标品质转成新物品。`v2.1` 让所有 Artifact 都提供升级成长，官方直说是为减少升级前反复换装的操作负担，说明成长是否绑定当前装备会直接改变玩家行为。

Formation 不是纯队形皮肤。当前资料列出职业、元素或混合门槛形成的团队效果，战斗中还存在前后排、被点名目标、击退、Weight、Hold the Line 与 Boss 阶段阵型切换。职业阈值、元素状态、Shield、Block、Thorns、装备 owner 与最终输出 owner必须保持正交。

## 失效历史结构：Season 5 `v2.1.0` Crow / Warlock

来源时期：玩家在 2026-08-09 的 `v2.1.0` 上线窗口发布，随后在 2026-08-11 明确补充：Veteran 招募 bug 已在 Conquest V 修复，文中通关策略在 bug 关闭后无法执行。官方 `v2.1.1` 只笼统记录 Veteran pity fixes；没有后续合法招募成本、可达性或完整实战能重新闭合该路线。因此以下内容只保存失效前的 engine/status/space/Boss-package 结构与 lifecycle 教训，不是当前攻略，也不能把漏洞步骤删除后继续称为可执行构筑。

- **engine**：六名 Warlock 的 Crow/Fear 链；Warlock 基础攻击施加 Fear，Crow 穿透已 Fear 目标。Multishot、Fear Rune 与 Ultimate 后增伤/暴击扩大读取频率与终结能力。
- **state/resource**：Fear stacks、INT、mana、Rune/Artifact slot、Veteran trait、Growth Potion 与 Blueberry 战后成长。
- **payoff**：Crow 以穿透路径越过 Forge Demon 等前排，清理后排援军；Sharpness 等低血执行负责收口。
- **survival**：三名 Healer 与一名 Knight 提供治疗、Barrier/Bulwark、Cold healer 生成 Shield与 Burn cleanse；这些 owner 买启动时间，不自动拥有伤害。
- **spatial condition**：Hold the Line 稳定阵线；Boss 间在 `1/8/1`、`2/5/3`、`3/6/1` 等 Formation 间切换，Knight 承担 Hurricane/雷电点名，Crow 利用穿透攻击后排。
- **payoff owner**：Warlock/Crow 拥有输出；healer/Shield 拥有生存；Knight 拥有点名承伤与阵位；Rune/Artifact只在文本声明的事件上读取。
- **economy/pivot**：失效路线原本在阶段 49/74 切 Glacier，并在 75 后通过锁三张卡、刷新 50 次再解锁的 bug 廉价取得指定 Veterans，随后用 Growth Potion、Blueberry farm 与 Boss 换装建立门槛。这个非法招募步骤是整套成本/可达性闭环的必要部分；修复后没有来源给出替代经济线，故不能据剩余步骤生成当前建议。
- **counter/limit**：原路线记录 Spider 的 Shock/poison resistance、Fire Dragon 的多盾/复活、Ice/Thunder Dragon 的三 Knight 与 Forge Demon 后排援军访问；这些可以保留为 Boss-package 设计结构。但原作者已明确判定修复后整套路线不可执行，所有强度、招募窗口与最终到达概率均失效。

这套失效结构仍能说明一条清楚的供应链：Warlock 供应 Fear，Crow 读取 Fear 并改变穿透路径，Healer/Shield 保证 reader 启动，Boss 包迫使抗性、阵型和 equipment package 切换。但组件文本只能证明链条语义，不能证明修复后的经济可达性；它在 corpus 中属于 lifecycle / invalidated-build 证据。

## 历史闭环：`v2.0` Priest / Thorns Immortal

来源时期：2026-02 Forge Demon 实战讨论、Dwarves Companion `v2.0.10` 近似快照与旧成长 Guide。`v2.1` 已重做多个 Artifact/Rune，故只保留为历史可运行结构，不宣称当前强度。

- **engine**：十 Priest 的治疗/生存底盘，或 Boss 时转为四 Banner、五 Priest 与一名 Umbra electric Mage；Thornmail、mythic Thorns、Thornward、Crown of Thorns 与 Holy Burn 提供具名 reader。
- **state/resource**：实际治疗量、当前 Health、Thorns 数值、成功 Block、Shield、Artifact 品质与战后成长。
- **payoff**：普通 Thorns 回应近战；Thornward 让 Thorns 可回应魔法/投射物并按 Thorns 生成 Shield；Crown of Thorns 周期性把 Thorns 主动发射成范围伤害并施加 Bleed；Vengeance 则按成功 Block 抵消的伤害反击。
- **survival**：Priest、Kakapo Staff/Egg、Shield与高 Health 保持队伍在场；Heart of Iron 限制单次受伤比例。
- **spatial condition**：密集治疗/传播范围保护前排，Banner 与 Priest 阵位兼顾 aura；Forge Demon 援军/爆发会测试单纯前排反伤的覆盖。
- **payoff owner**：Thorns 值是被读取状态，Thornward/Crown/Vengeance 是 converter；触发者或 Artifact owner 产生最终伤害。Shield 本身不是输出 owner。
- **economy/pivot**：长线积累 Thorns/STA/Artifact 品质；Boss 前按援军、伤害类型与抗性换 Banner、Umbra 或 electric Mage，而非永久锁十 Priest。
- **counter/limit**：非近战伤害需要 Thornward 才进入回应范围；Block 有 90% cap；Frozen 时不能 block/dodge，会关闭 Vengeance 类链；`v2.1` 生命周期变更使旧配置需要重新验证。

这条历史链直接回答“盾体系怎么打伤害”：不是让 Shield 自动输出，而是由 Vengeance、Thornward 或 Crown of Thorns 明确读取 Block/Thorns/被击事件。生存状态、converter、最终伤害 owner 和可反制窗口都能单独解释。

## Shield、Block、治疗与 defense-to-output

- Paladin/相关 Formation、Barrier/Bulwark 与 Scarab Brooch 证明 Shield 是可供应、可读取的临时生存状态，不是元素标签。
- Scarab Brooch 在治疗后按概率产生等于实际回复量的 Shield，并让被 Shield 单位额外增伤；这是明确的 `heal → actual heal amount → Shield → damage reader`，不是“盾天然加伤”。
- Vengeance 只在成功 Block 后按被抵消伤害反击；90% Block cap、Frozen 禁止 Block/Dodge 与非 Block 伤害构成自然反制。
- Thornward 与 Crown of Thorns 读取 Thorns；Heart of Iron 读取最大生命比例限制单次伤害。Health、Shield、Block、Thorns 是不同状态，不能合成一个“坦度值”。
- 用户提出的“全队额外生命/防御转给一个射手”在本研究中仍是项目侧候选：必须声明读取范围、接收者、slot/Artifact owner、刷新时机、cap、来源 lineage 与反制；本作没有证据支持把任意全队防御无条件折算给任意 carry。

## 元素、状态与防御动作正交

INT 影响技能/元素倍率，但 Fire、Thunder、Ice/Holy/Dark 的状态、抗性与触发器仍分别拥有规则。Order of Frost、Icebreakers、Chilled/Frozen 和 `v2.1` Frostfire 的 shatter 形成一条冰状态切片：供应 Chilled/Frozen，reader 在 Frozen 上触发额外范围结果。Frozen 同时降低伤害且禁止 Block/Dodge，说明元素状态能改写防御动作，却不会变成一种“冰盾”。

Frost Armor、Hot Coal、Tesla Coil 都是在被近战命中后施加对应元素状态的防御触发 Artifact。Ring of Thunder 则让雷电法术优先攻击佩戴者、回复其 mana 并免疫 Shock，形成 target rewrite + resource gain + immunity。Arkenstone 把伤害转为 Holy，Ultimate 可触发 Disintegrate，但携带者死亡会降低品质并损失累计属性。元素、目标选择、资源、成长风险与输出 owner依旧是不同轴。

DoT 不能递归触发自身或其他效果，Inquisition 明确不能再次触发自身。这一 guard 对任何“状态传播→爆炸→再传播”项目方案都是硬边界：需写清 source lineage、每事件次数与递归预算。

## 阵型、Weight 与 Boss 适配

Formations 2026 把约二十种 Formation 作为职业/元素阈值层；失效 Crow 路线保留了按 Boss 切换阵型的历史战术 package，但不能证明该 package 在 `v2.1.1` 后仍经济可达。Weight 影响击退与站位稳定，Potion of Rebirth 会重置 Weight；开发者承认描述不清，说明角色重置交易必须列出哪些成长保留、哪些重置。

Boss 不是单一数值墙。Spider 测 poison/Shock resistance，Fire Dragon 测 Shield/复活，Ice/Thunder Dragon 测 Knight 点名承伤，Forge Demon 测能否越过 Boss 清后排援军。Resistance、targeting、formation、cleanse、revive 与输出路径各攻击构筑不同 link。Boss preview 应在换装/招募锁定前出现，并明确失败来自哪一环。

`v1.20` 取消普通战自动 Sudden Death，改为一分钟后由玩家命令停止治疗并冲锋；Boss 三分钟后每秒增伤。这把普通战的反龟缩决策交给玩家，把 Boss 的拖延终止留给规则层。两者不能合并成无提示的统一超时秒杀。

## 商店、锻造、成长与长期 run

商店与 Tavern 同时承担招募、替换、刷新与成长 owner；装备、Rune、Artifact、Potion、Veteran 和退休 Gem 是不同预算。正式版前锻造专题承诺更清晰的配方/材料流，Companion 列出按 biome 与配方组织的 Forge；这支持“装备目标可规划”，不支持复制其具体配方。

评论中反复出现两种冲突：一边是长线成长、退休与 Gem 让弱开局可逐步建立专精；另一边是池膨胀、reroll 成本、Artifact 换装和长时间 Endless 让正确动作趋向反复刷成长。`v2.1` 的 Artifact 普遍升级收益正是对换装负担的官方修正，但并未证明所有成长路线已同样有意义。

Arkenstone 死亡降品质、Blueberries/Kakapo Egg 条件成长、Growth Potion 锁定属性与 Potion of Rebirth 重置 Weight说明持久状态必须有清楚事务边界：来源、当前 owner、替换/死亡/退休时的保留、损失与报告都需要逐项声明。

## 生命周期与负案例

本 checkpoint 计十五个 materially distinct negative/reworked families：

1. `v1.6` Rune Circle 完全替代 Skill Tree，并建议旧 clan 退休，旧成长不能静默迁移。
2. `v1.16` 再次重做 Rune Circle，移除随机 Rune Shop/库存以解决限制性和难理解。
3. 正式版扩充 Artifact/Rune/锻造，旧内容池与成长路径不能跨 `v2.0`。
4. `v2.1` 重做十个 Rune、多个 Artifact、技能与职业，旧 tier/build 强度失效。
5. `v2.1` 让所有 Artifact 升级都提供成长，针对升级前反复换装负担。
6. `v2.1.1` 修复可借锁卡 50 次操纵 Veteran 招募的执行漏洞，旧经济步骤作废。
7. `v1.20` 用玩家可选停止治疗/冲锋替换普通战自动 Sudden Death；Boss 另有递增伤害。
8. Block 90% cap 及来源说明需要清楚展示，否则纯格挡线看似可达无敌。
9. Frozen 禁止 Block/Dodge，元素状态会关闭防御动作，不能只显示减伤。
10. DoT/Inquisition 禁止自递归，状态链需要来源与次数 guard。
11. Unique Rune 的唯一、slot 与可叠加边界引发认知问题。
12. Holy/Dark resistance 规则与可得来源曾形成明显信息缺口。
13. Potion of Rebirth 重置 Weight 的交易描述不清，玩家无法预判持久损失。
14. 商店池稀释、reroll 成本、退休/Gem 与成长 owner 造成长期 grind 和错误替换成本。
15. Endless 难度、成长溢出/龟缩与少数成长循环压制其他 build，是需分别验证的 late-run guard 问题。

一般价格、内容量、语言与纯审美意见未拆分凑数。Cumulative explicit negative/reworked cases：235。

## 对本项目可迁移

- Shield、Health、Armor/Defense、Block、Dodge、healing、Thorns 与 element/status 应保持正交；只有具名 reader/converter 才能把生存转成输出。
- “盾+射手”可成立，但至少要显示 supplier、team-state reader、carry recipient、slot/Artifact owner、cap/refresh 与反制，不能只写一条全队百分比。
- 元素不是颜色前缀。Frozen 可改写 Block/Dodge，Thunder 可改写 target/mana，Fire/Poison 可要求 cleanse/resistance；每条状态链都要有自己的 supply/read/counter。
- Formation threshold 与实际战场位置是两层：前者决定团队规则，后者决定目标、穿透、击退、aura 与后排保护。
- Boss package 应攻击不同 build link，并在招募/换装窗口关闭前给出 resistance、targeting 与援军信息。
- Artifact/Rune/装备不是无主被动；成长、死亡降级、换装、退休与重置必须保留 lineage，并在战报中归因。
- 长线成长需要 anti-stalemate、数值 cap、换装摩擦控制和替代 build 的可达性；不能用无上限堆叠代替决策。

## 不兼容与未决

- 不复制最多十人的人口上限/扩编规模、具体职业/Rune/Artifact、Formation 阈值、Boss 或数值。
- Dwarves Companion 缺 `v2.1` 新 Rune，按约 `v2.0.10` 使用；其 AI 建议不计证据。
- Crow Guide 的 Veteran 锁卡步骤已由 `v2.1.1` 修复，且作者明确写明整套 clear strategy 在 bug 关闭后无法执行。当前只保留 engine/status/space/Boss-package 与失效原因；没有合法 post-fix economy/pivot，也没有当前 Crow 闭环。
- Priest/Thorns 是 `v2.0` 历史实战，不能称 `v2.1` meta。
- 没有全量 current 公式、Rune/Artifact drop odds、真实 build 使用率、胜率、内部 target/resolver 顺序或 Endless 数值上限。
- 公开视频没有可读字幕；未从缩略图、画面或音轨猜数值，也未下载/解包客户端。
- “冰盾”“土盾”和“全队额外生命/防御转单 carry”仍是项目设计问题，本 dossier 只提供 owner/guard 约束，不授权首版方案。

## Disposition

`retained`。

保留理由：43 个实质来源以历史 `v2.0` Priest/Thorns 实战闭合 retained 所需的一套具体构筑；`v2.1.0` Crow/Fear 只作为被 `v2.1.1` 修复击断的 lifecycle 结构，补充 engine、space、Boss package 与“经济可达性也是构筑一部分”的负案例。heal→Shield→damage、Block/Thorns converter、元素状态、阵型/Weight、成长/锻造仍有多来源交叉。

停止理由：`v2.1` 刚完成大规模重做，维护数据库落后一个版本，原 Crow 作者撤回修复后可执行性，定向复查又没有找到合法 post-fix 构筑，视频无可用转录且无统计。继续枚举旧装备或无字幕 runs 不会补上当前招募经济，因此保留为 long-tail retained，并明确 current-data gap，不伪造 current tier list 或当前 Crow 路线。
