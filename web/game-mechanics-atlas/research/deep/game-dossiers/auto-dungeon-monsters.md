# Auto Dungeon Monsters

## 身份与时期

- `title_id`: `auto-dungeon-monsters`
- Steam App：`1839630`
- 开发 / 发行：Turtle Charged Games。
- 状态：截至 2026-09-03 仍为 `Coming soon`，无公开 Demo、Steam Playtest、新闻、评测或攻略。
- 历史日期：2022 年缓存 App Details 曾列 `1 Nov, 2022`；第三方元数据也曾记录 2022 年预期窗口。当前官方页面已退回无日期状态，不能据此宣称曾发行、延期原因或取消。
- 公开定位：在地牢中取得阻挡玩家的怪物，用同类怪物牺牲升级，组合成自动战斗队伍；Boss 后取得可用于未来 delve 的 treasure。
- 证据结论：只有当前 / 历史商店描述、2021 年开发者官网快照和一个无规则论坛请求，没有公开试玩规则或实践 build，属于 `discovery-only`。

## 检索日志

访问日期统一为 2026-09-03。

### Steam 官方与社区

- App Details API 与商店页：当前只确认 TBA / Coming soon、单人、地牢取得怪物、同类牺牲升级、怪物互动、三阶段、Boss、跨局 treasure 和 hand-crafted AI parties 等产品承诺；按规则不计 deep source。
- 历史公开 App Details 缓存显示 2022-07 时页面写 `1 Nov, 2022`，背景 / 截图时间戳为 2022-07-25 左右；缓存正文与当前商店描述基本相同，没有额外规则或试玩状态。
- Steam News API：零条新闻。Community `allnews` 没有官方公告或补丁正文。
- Steam Guides：无可见攻略。
- Steam Reviews API：零条评测。
- General Discussions 只有 2023-02-12 的 `pls add mode Endless pls`，无回复。它只证明一名用户提出功能请求；不能确认其玩过公开 Demo、不能建立已有模式规则，也不能推断项目完成度。
- Steam Videos、Screenshots、Artwork 和 Community News 没有实质文字玩法材料。商店搜索只返回主 App，App Details 不列 `demos`。

### 开发者官网、社交与公开试玩

- 2021-12-09 Internet Archive 官网快照可读。首页称开发者位于 Tasmania，正在制作 `Auto Dungeon Monsters`；游戏摘要仍只有敌人转队友、组队、升级和组合，没有下载、版本、配方、具体怪物或战斗规则。
- 快照中的 `Auto-Dungeon-Monsters.html` 没有可用存档；CDX 不返回成功页面。当前 `turtlecharged.games` HTTPS 失败、HTTP 返回 502，旧 support 域名不可读。
- 官网快照链接到 `@TurtleChargedG` 和 YouTube channel `UC8_E1myp8xuH8sIubfhPZMQ`。当前 X / Twitter 账号返回 404，公开 syndication 为空；未从登录或缓存墙绕过。
- 开发者 YouTube 频道 RSS 无公开视频，频道 Videos 与 `dungeon` 搜索不返回可归属视频。没有官方 devlog、Demo 录像或字幕。
- 没有发现开发者 itch.io、Game Jolt、独立下载、浏览器版、Steam Playtest App 或公开测试注册正文。

### 外部搜索

- 通过 Brave、Google、Bing、DuckDuckGo 与 YouTube 路径组合检索精确标题、App ID、开发者 / 域名，以及 `demo`、`playtest`、`gameplay`、`guide`、`build`、`strategy`、`monster`、`sacrifice`、`upgrade`、`boss`、`treasure`、`endless`、Reddit、itch.io 与 YouTube site 限定。
- Brave 可访问结果只有 Steam、SteamDB、Metacritic、GamersGlobal、MMO13 和多语言商店页。GamersGlobal 只有数据库档案与五张图片；MMO13 重复商店介绍和旧预期日期。SteamDB 返回 403；不绕过。
- YouTube 精确标题没有正确结果；无引号搜索返回 Terraria、Idle Pixel Battle 等同词噪声，逐个核对后排除。
- GitHub 精确标题 / App ID 搜索只找到 Steam 元数据镜像和通用游戏列表，没有规则、源码或玩家资料。
- Internet Archive 对开发者域名仅有 2021 首页及静态文件快照；Steam CDX 路径超时，没有可读的规则 / 实践增量。搜索结果和元数据镜像不计 deep source。

## 公开承诺能说明什么

以下只作为 discovery 假设：

- 玩家可能在推进地牢时把遇到的敌方怪物纳入自己的 party。
- 同类怪物可以被牺牲以提升目标怪物；每种怪物被描述为有三个阶段。
- 不同怪物之间存在“interact with each other to boost their power”的组合关系。
- 地牢末端 Boss 可能提供用于未来 delve 的 treasure，形成跨局成长。
- 敌方队伍被描述为 hand-crafted AI parties，而非随机 ghost。

这些句子没有公开可执行细节：不知道何时取得、是否必得、同类数量、牺牲对象、阶段变化、队伍容量、treasure 所有权或 Boss 反制。

## 无法成立的构筑语法

- **engine**：只有“怪物互动”和“同类牺牲升级”的承诺，没有任何具名 supplier / carry / support 链。
- **state/resource**：怪物副本、阶段、地牢深度和 treasure 可能是资源；没有概率、数量、价格、槽位或持续边界。
- **payoff**：升级和组合被称为增强强度，但无技能、属性、状态、触发或收益所有者。
- **survival**：没有坦克、治疗、护盾、控制、死亡、复活或跨战恢复规则。
- **spatial condition**：截图不能代替正文；没有编队、前后排、格子、目标、距离、移动或 body-blocking 规则。
- **payoff owner**：同类牺牲可能让一个怪物升级，但不知道永久 / 单局、被牺牲对象、阶段继承和 party / reserve 归属。
- **pivot/counter**：没有招募替换、bench、出售、路线选择、坏对局、Boss 能力或反制窗口。
- **version context**：没有 Demo / Playtest 版本、patch chain 或公开玩家 build；2022 旧日期不能当可玩版本号。

不能把“取得敌人”“同类牺牲”“三阶段”“hand-crafted AI party”自动拼成某种死亡流、捕获流、三星合成或 Boss counter；来源没有建立这些规则。

## 经济、替换、空间与生命周期缺口

- 没有怪物取得条件、队伍上限、reserve、重复持有、替换、出售或机会成本。
- 没有牺牲所需副本数、阶段属性、技能继承、返还、失败与撤销规则。
- 没有 treasure 的池、稀有度、跨局解锁、装备者或污染后续掉落的规则。
- 没有 Boss 名称、能力、telegraph、队伍检查、反制或失败报告。
- 历史目标日期消失和官网失效只能证明公开状态变化，不能推断延期 / 停止开发的原因，更不能计为机械失败。
- 唯一 Endless 请求没有开发者回复或模式正文，不生成 lifecycle evidence。

## 对本项目的研究价值与限制

可保留为后续搜索提示的方向：敌人包同时作为招募候选；同类副本作为垂直升级成本；三阶段进化与横向队伍组合分轴；Boss treasure 作为跨局而非战内收益；hand-crafted enemy party 提供可设计的能力考试。

当前不能作为设计依据：任何怪物、阶段、牺牲数量、队伍组合、Boss、treasure、招募概率、编队、经济或 Endless 模式。项目已有英雄级 roster 和替换契约，也不能从一句“use monsters”直接引入捕获、献祭或敌人永久收编。

## 未决问题

- 项目是否仍在开发、已暂停、取消或更名。
- 是否曾存在公开 Demo / Playtest；2023 Endless 请求者的访问来源是什么。
- 怪物取得、party / reserve、牺牲升级、三阶段和替换的完整规则。
- 怪物互动的具名技能、状态、位置、收益所有者与组合例。
- Boss、treasure、跨局进度和失败重置契约。
- 至少一套版本化、具名、可复现的队伍 build 及其经济路线、pivot 和 counter。

## Disposition

`discovery-only`

该标题从 2022 目标日期退回 `Coming soon`，但没有证据表明正式发行。当前和历史公开材料都停在产品承诺，唯一社区主题不含玩法正文。按门槛不登记 deep source、不生成 deep evidence，也不把日期变化、官网失效或 Endless 请求计为机制 / 商业失败。
