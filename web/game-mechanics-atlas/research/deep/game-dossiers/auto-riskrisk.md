# Auto RiskRisk

## 身份与时期

- `title_id`: `auto-riskrisk`
- Steam App：`2259990`
- 开发 / 发行：Parrexion Games。
- 状态：2024-04-03 已发行。官方 Steam 公告覆盖 1.01–1.04；2025 年仍有开发者处理 Unity 版本升级缺陷。Google Play 商店在 2026-07-18 更新并声明移除多人模式，但商店页只能确认产品状态，不能充当 deep source。
- 历史谱系：旧 `Auto Risk Risk - Concept` 以角色购买、三同名合成和角色 type synergy 为核心；当前 `Auto RiskRisk` 已改为装备决定 synergy，并更强调 equipment 与 placement。两个时期不能混用。
- 证据结论：当前规则、官方补丁和开发者设计解释较丰富，但没有可访问来源给出当前版可复现的具体队伍 / 装备 / 站位 build，因此无法满足 deep gate。

## 检索日志

访问日期统一为 2026-09-03。

### 官方与开发者页面

- 当前官方 itch.io Demo 页面：完整读取 `How to play`。可确认每轮用 gold 买 equipment、同名物品合成、每件物品同时属于两个 sets、同 set 达量激活 bonus、调整 fighter 与 equipment 位置、自动战斗和生命淘汰；还列出 60 items、11 classes、15 enchantments 与 70+ artefacts。页面明确是受限 Demo，不能据此推断完整游戏的全部 artefact、解锁和难度。
- 历史官方 itch.io Concept 页面及全部公开评论：可确认旧版购买角色、队伍大小等于 level、三同名角色合成、相同 type 的不同角色互相增强和原价出售。开发者明确说明新版有更大的 gameplay changes，并更关注 equipment 与 placement。
- 开发者官网、Games 页和 Press Kits 页：正文只重复 equipment-focused 定位。公开 Google Drive press kit 含 `All Items.png`、`AllArtefacts.png`、`AllSpells.png` 与图标 / UI / synergy 素材文件夹；属于图片资产，不把图像目录、卡图或 OCR 推断当规则或构筑来源。
- CrazyGames、Google Play、已下架的 App Store 路径和其他网页试玩 / 聚合页面均检查；它们重复官方产品简介或只提供平台状态，不形成独立实践证据。

### Steam 官方与社区

- Steam News API 共八条公告。逐条读取六条有实质内容的公告：`Finally released!`、1.01、`First Balance Patch`、1.03、1.04 和移动版上线；两条 Next Fest 播放 / Q&A 邀请没有可读规则正文。
- `First Balance Patch` 可确认：避免连续匹配同一对手；治疗在 25–50 次后减半、50–100 次后停止以终止无限战斗；单一 Debuff Immunity 被拆为 Purity Shield 与 Protected Body，避免一个效果反制所有 debuff；Rogue / Precision 与多件 artefact 被调整；HP regeneration 与 lifesteal 增加数值反馈。
- 1.01–1.04 还记录 rage 只在一名 fighter 出现、Morph 隐藏、online item stats、match history、Health 不可降到 100 以下、AI difficulty 只改变 AI 强度，以及 prophecy / artefact 同轮结算和战后继续流程修复。缺少实践材料时，这些不能单独闭合构筑。
- Steam Guides 为空；General Discussions 共八个主题并全部检查。
- `Allow Us To Sell Artifacts?` 中，开发者解释被强制取得的 artefact 会附带免费槽，level 9 仍保留九个 regular item slots；普通 artefact 不允许随意出售，Priest 等 temporary artefact 另有规则。其理由是保留尝试 artefact 的压力，并避免过多 slots / synergies 造成复杂度和超出十二件装备的既有平衡范围。
- `Online Play` 中，开发者确认 online 原本是 asynchronous multiplayer，至少两个真人后由 AI 填满；没有采用 realtime 是因为预期玩家量有限。开发者曾考虑 Super Auto Pets 式异步单人，但认为本作很依赖针对当前对手策略进行适应，随机 ghost 对手会使这种适应过于随机。Google Play 后来的“Removed multiplayer mode”只能说明产品状态变化，不能据此建立原因。
- 其余讨论是 DLC 外观、服务器短时不可用、战后卡死、旧存档无法继续、成就拼写和 Unity 2021→6 序列化版本不匹配。没有具体 build；孤立缺陷也不被拆成 lifecycle evidence。
- Steam Reviews API 的十五条评测全部检查。三条较长评测分别说明旧版 unit synergy 到当前 item synergy 的变化、玩家可带着战略目标追求 `two-combo synergy build`，以及 prophecy / artefact 会改变或破坏当前构筑。其他评论只泛称组合很多、tradeoff 明显或 AI 难度，均未列出具体 item、fighter、位置或对局。

### 外部搜索与视频

- 通过 Google、Bing、DuckDuckGo 与 Brave 路径组合检索精确标题、App ID、包名、开发者名，以及 `build`、`guide`、`strategy`、`synergy`、`gameplay`、`item`、`set`、`artefact`、`prophecy`、`Priest`、`Morph`、`Purity Shield`、`Protected Body`、`Rogue`、`Precision`、`lifesteal`、Reddit 和 YouTube site 限定。
- 可访问结果仍收敛到 Steam、官方 itch.io、开发者官网、Google Play、CrazyGames、价格 / 玩家数 / 相似游戏聚合和一个网页试玩入口；没有 wiki、文字攻略、构筑数据库、统计或 Reddit 实战帖。
- YouTube 找到三条官方 gameplay / release trailer、2024-04-10 的 37 分钟 `First Look: Auto RiskRisk` 和一条 Demo gameplay。两个页面暴露自动字幕轨，但 timedtext 因 `exp=xpe` / PO-token 要求返回空内容；其余视频没有字幕轨。没有绕过 token、登录或反机器人要求，也不把无字幕画面、标题或缩略图当 deep evidence。
- 官方 Discord 邀请存在，但没有无需登录即可读取的公开规则 / 构筑正文；未登录或绕过访问限制。

## 当前规则包能确认什么

- **经济与升级**：每轮 gold 买 equipment；同名物品多件合成更高等级。公开页面没有价格、刷新成本、出售值、概率或完整升级曲线。
- **横向体系**：每件 item 同时属于两个 sets，多件同 set 触发 set bonus。这使单件装备可以充当两个体系的桥，但没有公开的 set 名单、阈值、bonus 或当前强度表。
- **所有权**：equipment 归具体 fighter；强制 artefact 自带独立免费槽，因此不会直接吞掉该等级的 regular item slot。普通 artefact 不可自由出售，temporary artefact 另有生命周期。
- **空间**：官方明确 fighter 和 equipment 的位置会影响战斗，但没有可读来源说明格子、前后排、距离、目标、移动、相邻或任何一套精确站位。
- **对局适应**：开发者把针对当前对手的调整视为核心，并认为随机 ghost 会削弱这种适应。公开资料没有给出某套 set 对另一套 set 的具体反制图。
- **反无限与反全能**：治疗按累计次数分段衰减直至停止；Debuff Immunity 被拆成两个较窄效果。它们是有价值的规则线索，但缺实践来源来说明由哪些 item / fighter 持有、如何进入 build 或如何被对手利用。

## 无法成立的当前版构筑

当前版最具体的玩家表述只有 `two-combo synergy build`：玩家说可以带着目标进入一局，并在完成两个 synergy 组合时获得满足感。它没有给出两个 set 的名字、装备名称、fighter 持有者、站位、升级 / 替换、收益所有者、坏对局或反制，不能算一套 build。

旧 Concept 评论有一条具体阵容：`6 green, 2 lord, 2 fang, 1 teacher`。但它属于角色本身提供 type、三同名角色合成的旧规则。当前版明确把 synergy 所有权改到 equipment，并增强 placement；把旧阵容映射为当前 build 会混淆时代与系统。

因此也不能把补丁中零散出现的 Rogue、Precision、Purity Shield、Protected Body、lifesteal、Priest、Morph 和 artefact 拼成一套来源中从未存在的队伍。

## 无法完成的构筑语法

- **engine**：可确认同物合成与双 set 累积，但不知道任何一套当前 item / set 链。
- **state/resource**：gold、装备副本、set count、slot 和 artefact 可见；价格、概率、刷新、出售与阈值缺失。
- **payoff**：set bonus 和 artefact 会改变 build，但没有具体伤害 / 治疗 / 控制结果与结算顺序。
- **survival**：补丁出现 Health、healing、HP regeneration、lifesteal 和两个免疫效果，缺具体装备者与队伍关系。
- **spatial condition**：只有“位置重要”的官方总述，没有当前版合法格、选择器或站位例。
- **payoff owner**：装备归 fighter、artefact 有独立槽可以确认；双 set bonus 究竟作用于持有者、全队或其他目标无法逐项确认。
- **pivot/counter**：只确认应根据当前对手适应，没有任何具名替换、坏对局、侦察信号或反制窗口。
- **version context**：1.01–1.04 可定位规则变化；旧 Concept 阵容不能升级成 1.x 构筑，2026 移除多人也不能倒推 2024 的平衡。

## 生命周期边界

- 旧角色 / type synergy 到当前 equipment / dual-set synergy 是有开发者与长评交叉的重大方向变化。
- 官方补丁明确处理无限治疗、全能 Debuff Immunity、连续同对手、同轮 prophecy / artefact 结算、数值反馈和运行时卡死。
- 2025 年 Unity 2021→6 升级一度造成序列化版本不匹配，开发者在讨论中修复；这是发布管线缺陷，不是构筑机制失败。
- 2026 Google Play 移除 multiplayer 是商店状态事实。没有开发者复盘把它归因于人口、异步设计或机制质量，不能推断商业 / 设计因果。
- 因为规则包没有独立实践 build 闭环，本 checkpoint 不把上述条目登记为 deep evidence，也不增加 negative / reworked 计数。资料多不等于已满足功能门槛。

## 对本项目的研究价值与限制

可以保留为后续研究提示、但本游戏尚未独立验证成设计结论的方向：双标签装备作为体系桥；强制遗物用免费槽维持尝试压力；槽位上限抑制组合复杂度；治疗反无限使用分段衰减；把“一项免疫全部”拆成窄反制；对手预览应提供可执行的适应窗口。

不能作为本项目方案依据：任何具体 set、item、artefact、fighter、prophecy、职业、数值、九 / 十二槽平衡、当前多人模式、旧 `green/lord/fang/teacher` 阵容，以及根据零散补丁名词拼出的盾、元素、Rogue 或 Precision 体系。

## 未决问题

- 当前 60 items、11 classes、15 enchantments、70+ artefacts 的完整文字数据库。
- 每个双 set 的阈值、效果、作用域、持有者和 bridge 关系。
- fighter 数量、位置、目标、行动顺序、装备位置和 slot 增长的完整规则。
- 商店、刷新、合成、出售、经济与 prophecy 选择规则。
- 至少一套具名、版本化、可复现的当前队伍 / 装备 / 站位构筑及其 pivot 与 counter。
- 1.04 之后的平衡历史、2026 单人版规则与移除 multiplayer 的官方原因。

## Disposition

`insufficient-evidence`

这是已发行且公开规则丰富的长尾游戏，不是只有营销承诺的 `discovery-only`。但所有可访问当前资料都停在规则、补丁、开发者理由或泛化体验；唯一具体阵容属于已被重做的旧 Concept。按既定门槛不登记 deep source、不生成 deep evidence、不增加生命周期案例，避免以“资料很多”替代“当前构筑闭环存在”。
