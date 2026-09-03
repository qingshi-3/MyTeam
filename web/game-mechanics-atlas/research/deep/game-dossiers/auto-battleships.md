# Auto Battleships

## 身份与时期

- `title_id`: `auto-battleships`
- Steam App：`3379080`
- 开发 / 发行：Umlaut Games。
- 状态：2026-04-14 正式公布；截至 2026-09-03 仍为 `To be announced`，无公开 Demo、Steam Playtest、评测、攻略或社区讨论。
- 公开定位：PvP roguelike autobattler；一局内管理 armada、为舰船装备不同形状 / 尺寸 / 稀有度的 items、合并小船形成 battle cruiser，并在每阶段末与其他玩家战斗。
- 特色承诺：战前 scouting items 以不同 shapes 和 repetitions 揭示对手部分布局；发现 explosive 后可针对它；玩家再根据情报调整自己的舰队位置。
- 证据结论：只有商店页与一条官方 announcement，且公告明确让玩家等待未来 playtests / demo releases。没有公开实践闭环，属于 `discovery-only`。

## 检索日志

访问日期统一为 2026-09-03。

### Steam 官方与社区

- App Details API 与商店页：确认 TBA、Online PvP、选择 captain、run 内扩张舰队、装备物品、侦察、针对 explosive、舰船合并、物品形状 / 尺寸 / 稀有度、海上事件与交易等产品描述；按规则不计 deep source。
- Steam News API 只有 2026-04-14 `Auto Battleships Announcement!`。正文说明每阶段末对战其他玩家、战前侦察可看见对手部分 setup，并邀请关注未来 playtest / demo；它是开发公告而不是公开规则书或实战 build。
- App Details 不列 `demos`，Steam 商店搜索只返回主 App，没有单独 Demo / Playtest 条目。
- Steam Guides：无可见攻略。
- Steam Reviews API：零条评测。
- General Discussions：零个主题。Community Videos、Screenshots、Artwork 与 News 没有实质玩家玩法正文。
- 商店内的 GIF / video 展示属于无字幕视觉素材；不从画面推断格子、物品占位、舰船尺寸、攻击顺序或构筑。

### 开发者网站、频道与测试入口

- Umlaut Games 官网、sitemap、首页、`home-3`、Presskit 和公开 WordPress 文章全部检查。站点只展示既有作品 LIMBS / Leyline；截至访问日没有 Auto Battleships 专页、presskit、规则、开发日志或下载。
- 官网 sitemap 最后修改于 2026-04-07，但相关页面没有本游戏正文。WordPress REST 路径返回 404；没有隐藏文章被当作证据。
- 开发者 YouTube channel `UC5umFKw7OqgrfI5JO8s8L_Q` 的 RSS 只有 Leyline 视频，频道内 `battleships` 搜索没有相关公开视频；一般 YouTube 搜索也没有可归属 gameplay / guide / transcript。
- 官方 announcement 的 Discord 链接只用于未来 playtests / demo 通知。没有加入服务器、登录或绕过访问限制；私域测试招募不等于公开试玩。
- 官网社交链接仍指向旧 Leyline 账号，没有可读 Auto Battleships 规则或实战帖。

### 外部搜索

- 通过 Brave、Google、Bing、DuckDuckGo 与 YouTube 路径组合检索精确标题、App ID、Umlaut Games，以及 `demo`、`playtest`、`gameplay`、`guide`、`build`、`scout`、`shape`、`merge`、`explosive`、`fleet`、`items`、`rarity`、`counter`、Reddit 和 YouTube site 限定。
- 搜索结果只指向 Steam、开发者主页和自动元数据 / wishlist 聚合；没有新闻报道、试玩、攻略、wiki、Reddit 构筑或统计。
- GitHub 精确标题 / App ID / 开发者搜索无结果；没有公开代码、规则数据或测试笔记。
- SteamDB 和其他元数据页即使可发现，也只镜像 TBA / 标签 /商店字段，不满足 deep source。

## 公开承诺能说明什么

以下只保留为 discovery 假设：

- 一局内通过事件取得 ships 与 items，并在每阶段末对战其他玩家。
- scouting items 的覆盖形状和重复次数可能构成情报资源；玩家用部分揭示结果重新部署自己的舰队。
- 对方 explosive 可能是可瞄准的战场对象，击中后产生高价值连锁收益。
- 小型舰船可以保持分散 / 灵活，也可以 merge 为大型 battle cruiser，暗示宽度与集中投资的选择。
- items 有不同 shape、size、rarity，且可以 combine 变强；舰内 / 舰间摆放被描述为 synergy 来源。
- 海上随机事件可提供取得、交易和风险选择。

没有公开试玩或规则文本说明这些系统如何共同结算。

## 无法成立的构筑语法

- **engine**：只有买 / 取得 items、combine items、merge ships 和 scouting 的承诺，没有具名物品或船型链。
- **state/resource**：舰船数量、item 形状 / 稀有度、侦察 reveal、explosive、交易和 run 阶段可能是资源；没有价格、次数、概率、槽位或保存规则。
- **payoff**：大舰、synergy 与针对 explosive 被称为优势，没有伤害、射击、爆炸传播、buff 或收益所有者。
- **survival**：没有装甲、生命、修复、护盾、规避、沉船或舰队失败规则。
- **spatial condition**：只能确认“placement matters”和 scouting 有不同 shapes / repetitions；没有格子尺寸、旋转、相邻、遮挡、射程、目标或移动规则。
- **payoff owner**：无法确认 item 属于格子、舰船还是舰队；merge 后 item / 属性 / explosive 如何继承也未知。
- **pivot/counter**：可以概括“侦察到 explosive 后针对它”，但没有侦察物品、舰船、武器、位置、反侦察、错误情报或二次部署的具体例。
- **version context**：只有 2026-04 announcement / store promise，无 Demo 版本、patch chain 或玩家 build。

因此不能把“侦察形状＋发现爆炸物＋合并大舰＋异形物品”拼成来源从未展示的侦察爆破舰队，也不能假定它沿用传统 Battleship 的网格、命中或沉船规则。

## 经济、空间、目标与反制缺口

- 没有 captain 差异、舰队容量、船池、共享池、复制 / merge 数量、出售、交易价格和 item combine 规则。
- 没有 item shape 如何占位、能否旋转 / 移动 / 跨舰放置、rarity 如何影响属性或 synergy。
- 没有 scouting 的合法目标、覆盖形状、重复含义、revealed 信息持久性、假目标或机会成本。
- 没有 explosive 的所有者、触发条件、伤害范围、友伤、连锁、可拆除 / 转移或反制。
- 没有 battle target order、速度、射程、位置交换、沉船、战斗报告或失败伤害。
- PvP 对手、ghost / 异步 / 同步、匹配和侦察信息是否来自当前对手均未说明。
- 尚无 balance patch、开发复盘或玩家失效案例，不生成 lifecycle evidence。

## 对本项目的研究价值与限制

可保留为后续搜索提示的方向：把侦察覆盖形状做成有机会成本的情报资源；让敌方 explosive 成为可预览战场对象；用“多小单位 / 合并单核”制造宽度与集中投资选择；异形装备把槽位几何和稀有度分开；战前部分情报驱动部署适应。

当前不能作为设计依据：任何舰船、captain、item、形状、rarity、merge、explosive、侦察次数、PvP 结构或具体数值。本项目是单人 authored enemy package，也不能直接采用对手隐藏部署或公开 PvP 侦察循环。

## 未决问题

- 首个 Demo / Playtest 的公开时间、版本号与规则变化。
- PvP 是同步、异步快照还是其他模式；侦察和重新部署发生在何时。
- 舰船 / item 的格子、形状、旋转、合并、所有权和移除规则。
- scouting reveal 与 explosive 的目标、范围、持续、反制和错误解释。
- run 经济、事件、交易、舰队容量、失败和阶段结算。
- 至少一套版本化、具名、可复现的舰队 / item / scouting build 及其 counter。

## Disposition

`discovery-only`

该游戏在 2026-04 才公开，官方仍把 playtest / demo 写成未来事件。现阶段只有商店与 announcement 承诺，没有规则＋实践双功能来源。按门槛不登记 deep source、不生成 deep evidence，也不从 GIF、传统 Battleship 常识或宣传术语合成舰队 build。
