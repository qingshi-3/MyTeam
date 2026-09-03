# Auto Immune

## 身份与时期

- `title_id`: `auto-immune`
- Steam 主 App：`2139330`；公开 Demo App：`2471360`。
- 开发 / 发行：Distant Signal Games。
- 状态：主 App 截至 2026-09-03 仍为 `Coming soon`；Demo 于 2023-08-28 上线且当前仍可在 Steam 取得。主 App 无评测，Demo 无独立评测区。
- 版本边界：2023 浏览器 / Strategy Fest Demo、2023-12 M-Cell 重做、2024-02 Next Fest Demo、2024-07 Tower Defense Festival Demo 与 2025–2026 开发中短视频不是同一规则快照。
- 证据结论：公开 Demo 和实质官方改动记录确实存在，但独立实践材料无法闭合一个版本化 build。唯一具名组合只有 `Tag Sprayer + Tag Seeker Splitter` 一句话，缺少状态、收益所有者、经济、空间条件、失败与反制；因此仍为 `discovery-only`。

## 检索日志

访问日期统一为 2026-09-03。

### Steam 官方与社区

- App Details API 与主 / Demo 商店页：确认主 App `Coming soon`、Demo App 可用、单人 / 多人标签与当前产品说明。当前主商店正文还给出 Carbs、同名合并、Elite / Reward、旋转、可移动 Blood Cells 和攻击 lanes 等提示；商店材料按门槛只作 discovery 路由，不计 deep source。
- Steam News API 共八条公告，逐条读取全文。2023-08 Demo 公告称有 43 个 Immune Cells、5 级进程、9 种 Virus，以及把三个普通 cell 合成后取得 Reward cell；2023-12 公告记录颜色系统移除、M-Cells / Magnet Cells 引入、Blood Cells 可移动、若干 cell 移除 / 新增及 bombs / mortars 小幅削弱；2024-02 Next Fest 公告记录 Electricity、5 个新 cell、Tag Sprayer、12 rounds、较缓难度曲线与 cell 描述重写；2024-07 公告只概括新 tutorial、dynamic camera、QoL 与 bug fixes。
- Steam Guides：无可见攻略。
- Steam Reviews：主 App 为零条，不能取得版本化玩家评测样本。
- General Discussions 共十五个主题，全部检查。大多数是 bug、功能请求、联系方式、语言与商店合规；只有 2024-09-08 `how to make a army` 给出一句具体组合：取得 Tag Sprayer，把它与 Tag Seeker Splitter bond，然后看着 army 展开。
- 2025-05-30 `Galery and map border` 只报告一个会躲避 cells 的 virus 被 seeker cell 追出地图边界；没有开发者回复、阵容、重现条件或修复结果。
- 2023-09 `portable base` 的开发者回复称最新 Demo 已允许移动 Red Blood Cells；该变化与 2023-12 官方公告一致，但仍不提供完整空间 / 目标规则。
- 2023-09 `How to play feedback` 报告教程视频被游戏音乐覆盖，玩家不理解颜色、资源和 cell behavior；开发者计划在 2024-02 大更新移除视频并改进游戏内说明。2024-02 官方公告确认描述重写，2024-07 又称有新 tutorial，但没有后续可用性研究或广泛玩家样本。
- 2024-01 `There is a bug` 报告尚有多个 Red Blood Cells 时被错误判负。开发者只说新 Demo 改动很多、希望不再发生；这不是修复确认，旧 Demo 失败也不能外推到后续版本。
- 2026-08 `cant rotate?` 只有一名玩家不知道如何旋转、且无回复。它与商店的黑箭头旋转提示并存，只能记录为当前可发现性疑问，不能证明旋转已移除或失效。
- 2025-11 起 `is the game dead?` 只有玩家猜测。官方 YouTube 在 2026-08 仍持续发布开发内容，因此不把社区猜测升级为取消、停更或商业失败结论。

### 官方站点、视频与公开测试路线

- 旧 web Demo 地址 `play.autoimmunegame.com` 来自 2023-08 黑屏讨论中的开发者回复；当前主域名、`www` 与 `play` 子域均发生 TLS / 网关失败，无法读取网页规则。没有启动 Demo、安装客户端或绕过访问问题。
- Internet Archive 对主域与 `play` 子域的 CDX 请求持续超时，没有把搜索摘要或缺失快照当正文。
- 官方 YouTube 频道 `UCsFFr4yoY_SOPoqfJu9demw` 的初始页面列出 6 个长视频和 48 个 Shorts；RSS 可见的 2025–2026 近期标题涉及 Antibody、Stem Cells、Magnet、Bomb 延迟、复制 generator、wall 受击成长、chain / turret 与跨 lane 移动等开发中演示。
- 这些短视频标题和描述是开发者展示线索，不提供完整数值、购买路线、阵容、结算顺序或反制。官方 2023 `Seven Turns of Auto Immune Gameplay (Pre-alpha)` 暴露自动字幕轨，但 timedtext 在 `exp=xpe` / PO-token 条件下返回空；没有绕过，也没有从无字幕画面推断规则。
- Steam 的两条 2024 developer livestream 公告只有活动简介，没有可读回放正文或字幕；不把“展示可能性”当作可复现 build。
- 官方 TikTok oEmbed 可读到一条“Bomb 的 positioning 会造成很大差异”的标题，但视频正文没有可引用的规则文本；它只保留为空间机制线索。

### 外部实践、检索与排除路线

- YouTube 精确搜索找到两支独立长 gameplay：Dead3y3 的 `Fight Viruses in this New Tower Defense Masterpiece! | Auto Immune`（2024-03-30，91:36）与 Olexa 的 `Finally, We Have the Covid-19 Autobattler Tower Defense Game`（2024-02-17，35:31）。两者描述只重复商店概述；自动字幕轨存在，但 timedtext 同样在 `exp=xpe` / PO-token 条件下返回空，因此不计规则或 strategy source。
- 继续检索官方 pre-alpha / Demo trailers、独立 gameplay、`Tag Sprayer`、`Tag Seeker Splitter`、`Reward Cells`、`M-Cells`、`Resonant Wall`、`Magnet Cell`、`Electricity`、`Antibody`、`build`、`strategy`、`counter`、`Reddit`、`guide`、App ID 与 Demo ID。DuckDuckGo、YouTube、Google / Bing 可达路径没有产生第二份具体 build、wiki、攻略或玩家反制正文。
- Reddit 公共搜索和站点限定没有发现可归属的构筑讨论；Steam 社区的一句话 Tag build 不能被搜索摘要重复计为第二来源。
- GitHub 精确标题只返回另一个同名的 isometric turn-based 项目；App ID 搜索没有可用规则数据。SteamDB、元数据页与商店镜像只用于确认条目存在，不计 deep source。
- Discord 是官方反馈 / 私测入口；没有加入服务器、登录、读取私域内容或把私测邀请视为公开规则。

## 可确认的官方版本变化

以下只描述已读公告，不代表当前完整规则：

- 2023-08 Demo 把三个普通 cell 的合成与 Reward cell 选择连接起来；具体普通 cell、Reward pool、Elite 强度、经济和出售规则没有在公告展开。
- 2023-12 移除了 colors：cells / viruses 不再有颜色，immune cells 不再因颜色磁吸 viruses。M-Cells 接管 walls、bonds、wildcards 和多数静止功能；原来读取 matching colors 的 Resonant Wall、Resonant Cannon、Resonant Buffer、Resonance Amplifier 改读 M-Cells。
- 同一更新加入 Magnet Cell，并让 Kinetic Magnet Buffer 的吸引影响所有 viruses；这证明“吸引 / 目标路径”是独立空间机制，而不是元素颜色本身。
- Swapper Bond 会连接一个邻近 immune cell，并在下一 shop round 与最近的已连接 cell 交换 Health；Proximity Buffer 放置时给最近 M-Cell `+3 Health`。公告没有说明并列距离、旋转、链接断裂、死亡或移动后的重算顺序。
- 2024-02 Next Fest Demo 加入 Electricity、5 个相关 cells 和 Tag Sprayer，并把关卡扩到 12 rounds、放缓难度曲线。没有可读来源解释 Electricity 状态、传播、清除、抗性或 payoff owner。
- 当前商店正文一处写 10 turns，后文又写单人目标为 turn 12；2024-02 公告也写 12 rounds。该冲突不被擅自解析为当前终局规则。

## 唯一具体组合为何仍不构成 Build

社区样本：`Tag Sprayer + Tag Seeker Splitter`，2024-09-08。

- **engine**：玩家明确要求取得 Tag Sprayer，并把它与 Tag Seeker Splitter bond。
- **state/resource**：名称暗示 Tag，但帖子没有解释 Tag 如何产生、保存、传播或消费；也没有 Carbs、复制、Elite 或 Reward 路线。
- **payoff**：作者只说“army unfold”，没有说明生成什么单位、数量、频率、持续时间或伤害方式。
- **survival**：没有 Blood Cells、wall、Health、heal、吸引、body blocking 或防线层。
- **spatial condition**：只能确认两者需要 bond；没有距离、朝向、相邻、lane、部署方向或与 Blood Cell 的相对位置。
- **payoff owner**：无法判断 army 由 Sprayer、Splitter、Tag 目标还是 bond 创建，也无法归属击杀 / 伤害。
- **pivot/counter**：没有缺牌替代、投入时机、坏对局、virus 类型、lane 调整、拆链或克制。
- **version context**：帖子在 2024-07 Demo 公告之后，但没有 build 号；不能与 2023 colors 版本或 2025–2026 开发视频混用。

因此不能把这一句话与官方 `Reward cell`、M-Cell、Electricity、Magnet、Bomb 或 2026 Shorts 拼成来源从未展示的召唤 / Tag 完整阵容。

## 经济、空间、敌人与反制缺口

- 商店提示声称 Carbs 每回合 use-it-or-lose-it、同名两个 / 三个合并和 Reward 选择，但无独立实践来源说明何时 reroll、锁定、持有 pair、卖出、跳过或为三合一牺牲版面。
- 没有 cell pool、商店槽位、刷新价格、概率、reserve、人口 / 占地、Elite 继承、Reward pool 或替换成本。
- 当前商店提示能发现旋转、Blood Cell 移动和最多三条攻击 lane，但没有完整地图边界、lane 开放时机、virus 目标优先级、碰撞、吸引覆盖、body blocking、越界或并列距离规则。
- “constantly evolving enemy”仍是产品概述。公告只给出 9 种 Virus 和较缓难度曲线；没有具名进化、波次增殖公式、Elite / Boss、telegraph、适应窗口或阵容 counter。
- Seeker 追逐逃逸 virus 的单例说明物理场可能产生边界问题，但没有重现矩阵、修复或玩家可用 counter。
- Bomb positioning、Antibody、Stem、wall 成长等近期标题没有可读规则；不把标题合成为当前 build，也不从宣传语判定强度或 `broken`。
- 2023 教程、错误判负与 2025 边界报告都是真实反馈线索，但缺少稳定复现和修复闭环，不生成 deep lifecycle evidence。

## 对本项目的研究价值与限制

可保留为后续搜索提示的方向：把“标签供应者—复制 / 分裂消费者—临时 army”作为召唤链候选；让 bond / 最近目标 / lane / 吸引范围成为可视化空间条件；三合一同时承担纵向强化和 Reward 选择；用可移动保护目标改变部署空间；把颜色属性与 Magnet / Resonance / Electricity 等机制轴分开；对基于物理追逐的目标、边界和归属建立明确 guard。

当前不能作为设计依据：任何 Tag、Electricity、M-Cell、Magnet、Bomb、Antibody、Stem、Reward、Elite、Carbs、lane、virus 进化、合并强度、具体数值或阵容。尤其不能把开发者短视频标题、商店提示和单条玩家组合横向拼接成一套已验证体系。

## 未决问题

- 当前 Steam Demo 对应 2024-02、2024-07 还是后续规则；2025–2026 Shorts 中的 cells 是否已经进入公开 Demo。
- Carbs、商店、刷新、reserve、cell pool、合并、Elite、Reward 和替换的完整经济规则。
- Tag Sprayer / Tag Seeker Splitter 的准确描述、触发顺序、army 类型、占位、上限、归属和反制。
- M-Cell、Magnet、Resonance、Electricity、Bomb、Antibody、Stem 与 wall 的当前版本文本及相互作用。
- Blood Cells、lane、旋转、bond、最近目标、物理碰撞、边界和 seeker 的确定性规则。
- Virus 的增殖 / 进化、波次、目标、telegraph、失败结算和针对性 counter。
- 至少一套版本化、具名、可复现的 build：包含购买路线、合并 / Reward 决策、布局、payoff owner、survival、pivot 和 enemy counter。

## Disposition

`discovery-only`

Auto Immune 比前几个未发行候选拥有更强的公开规则线索：可下载 Demo、实质版本公告和一条具名玩家组合都存在。但它仍缺少规则＋独立实践双功能闭环；公开视频字幕因 PO-token 路径不可读，且不能用无字幕画面补齐。按门槛不登记 deep source、不生成 deep evidence，也不把教程 / 边界 / 错误判负单例计入生命周期总数。
