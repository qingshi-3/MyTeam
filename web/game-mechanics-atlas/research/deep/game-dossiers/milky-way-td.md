# Milky Way TD SURVIVORS AUTOBATTLER RTS

## 身份、版本与发行边界

- `title_id`: `milky-way-td`
- Steam 正式版 App：`2837330`；开发：MyDreamForever、Tazdraperm；发行：MyDreamForever；Windows-only；2024-03-29 正式发行，非 Early Access。
- Steam Earth 版 App：`2837340`；2024-03-22 发行的免费 Demo。商店正文明确限定为 1/4 地图、9/11 塔和 3/5 技能，不能用它代表正式版完整内容。
- Microsoft Store / Xbox 版由 Desert Water Games 发行，Xbox Series、Xbox One 和 Windows 商店路线于 2026-05-13上线。它们重复核心产品描述，但没有公开规则差异表或版本映射，因此不与 2024 Steam 版静默合并。
- 正式版 Steam 商店没有官网或 support URL，仅列 `tazdraperm.dev@gmail.com`。商店与平台页只用于身份和版本边界，不计 deep source。
- 证据结论：可读评论能够较完整说明循环，也出现一条 Demo 强势路线；但没有一套同时闭合经济、空间、生存、收益所有者与反制的版本化构筑，未达到 `retained` 门槛。

## 检索日志

访问日期统一为 2026-09-04。

### Steam 正式版 `2837330`

- App Details、商店页与 Steam 全局成就页确认身份、发行状态、Earth / Mars / Fantasy / Ice Giant 四张地图及各自 Hardcore 通关。成就还要求在胜利时达到至少 999 stone income、100 mana income 或 500 gold income；这些只是挑战阈值，不给出产能公式、时间窗口或最优经济线，故不进入 deep evidence。
- Steam News API 为零；Community `allnews` 没有可读官方公告；Steam Guides 没有玩家条目。
- General Discussions 共五个主题并全部读取：`controller support?` 只有开发者表示不计划手柄支持；`Full Game` 是购买识别问题；`Hotkeys?` 请求建造快捷键；`Questions for Dev's` 是内容创作者索取 key；`New Updates ?` 只有玩家希望增加塔和地图。没有一个主题给出规则闭环或构筑。
- Reviews API 以 `language=all`、`purchase_type=all` 和完整日期范围返回 23 条评测：20 正、3 负。23 条已逐条检查；比例只说明该 API 快照，不作为代表性统计或机制质量证据。

### Earth Demo `2837340`

- App Details 明确这是正式版的免费 Demo，且内容范围小于正式版；正式版页面没有 Demo 字段并不意味着该独立 App 不存在。
- Steam News API 为零；Guides 页面无条目；General Discussions 共三个主题并全部读取：一次三次通关/解锁完可用内容、字体建议，以及开发者确认成就会放在正式版。它们不提供构筑。
- Reviews API 返回 35 条评测：30 正、5 负。绝大多数是类型、难度、时长、字体或情绪性短评。最具体的实战样本是 recommendation `161344680`：玩家称取得 `thunder/star dragon` 后重复建造该单位和 gold mines 即可轻松通关，并认为 fireball dragon 相比其他单位没有用途。这是可用的 Demo 策略线索，但仍只是四十分钟评论中的单路线判断。

### 可读的规则与实践候选

- 正式版英文 recommendation `161900081`：发评时与访问时均为 117 分钟。它说明目标是保护星球并完成指定波数，资源建筑采集 stone / gold / mana，攻击或召唤单位自动作战，主动技能消耗 mana，击杀填充经验条，升级时三选一。它是较完整的循环介绍，但不是构筑攻略。
- 正式版韩文 recommendation `175815013`：发评时 119 分钟，访问时累计 273 分钟。它说明敌人按波进攻中央星球；建筑和单位受圆形合法范围约束，继续部署会逐步扩张范围；资源建筑还需靠近对应资源并定时生产；stone / gold 支付建造，mana stone 支付技能；连续建造提高下一次价格，而资源建筑价格会逐渐折扣；单位在射程内自动攻击；奖励条满后三选一，新单位、单位强化与技能强化混合，部分卡有数量限制；建筑/单位生命归零后移除，星球毁灭失败，清完最终波胜利；局外资源可解锁新星球与 Hardcore。作者也明确表示资源建筑合法位置难辨、没有奖励 reroll、内容有限，并未确认 9999/999 是否只是显示上限。
- 同一韩文评论列出苹果召唤群、火/水波/闪电龙、石头坦克、治疗树，以及 meteor、machine gun、rocket 等技能。这些是单位/技能例，不是一套共同上场的阵容。
- 正式版英文 recommendation `161813278` 以幽默方式说只建 Strawberry 防线导致 FPS 下降；`161841102` 是发评时及访问时均 134 分钟的单人中文体验，称理解规则后约两小时通关且数值不平衡；`190202934` 称单位难辨；`183176424` 称流程短、成长贫乏。它们只能作为低权重社区观察，不能代表总体玩家或独立闭合构筑。

### 外部、视频与移植版路线

- 以完整标题、两个 Steam App ID、开发者名及 `guide`、`strategy`、`build`、`gameplay`、`review`、`towers`、`upgrades`、`hardcore`、`Strawberry`、`Apple`、`Healing Tree`、`resource discount` 等组合检查 DuckDuckGo、Google、Bing 与 Brave 路线。可访问结果收敛到 Steam、平台商店、聚合元数据、视频和 2026 移植版发售稿；没有 wiki、文字攻略、构筑数据库、统计或玩家长帖。
- YouTube 找到正式版与 Earth Demo 的多条完整流程视频，包括 2024-03-22 的 Earth 20 波录像、2024-03-31 的近 50 分钟正式版录像、2024-04-13/16 的玩法录像，以及 2026 Xbox/Windows 的 100% / achievement walkthrough。页面可见的描述重复商店文案；视频没有人工时间戳规则说明。
- 多个页面暴露 `English (auto-generated)` caption track，但 YouTube signed timedtext、简化 timedtext、Jina 页面和公开 Invidious caption route 都返回空字幕体。未绕过 token、登录、Cloudflare 或机器人验证，也不把无字幕画面、标题、缩略图或产品描述计作 deep evidence。
- 2026 Xbox 官方商店只确认平台、发行商、5/13/2026 日期及重复产品文案。TheXboxHub 的 2026-05-13 发售稿仍是产品介绍，不是实际评测或构筑。TrueAchievements 搜索结果只暴露成就条件；正文/Walkthrough 路径受 Cloudflare 阻断，没有尝试绕过，也没有把搜索摘要当证据。
- 正式版与 Demo 均没有独立官网；Steam creator、support email、MobyGames、Metacritic、RAWG、Playin、SteamDB、Reddit 列表和其他聚合路线未产生新的可读机制或实践材料。

## 最接近构筑的 Demo 路线及缺口

Earth Demo recommendation `161344680` 与正式版两条长评可以组成研究假设，但不能升级为一套来源可核验的 build：

- **engine**：玩家反复建 gold mines，再重复部署其称为 `thunder/star dragon` 的单位。
- **state/resource**：gold 是可见状态；韩文正式版评论另称连续建造会涨价、资源建筑随时间折扣，但没有证据说明 Demo 与正式版数值/时序完全相同，也没有该玩家的等待或购买节奏。
- **payoff**：该玩家认为龙路线让 Demo 变得容易；韩文评论只说明某类龙会产生闪电。没有伤害、范围、升级卡、技能或波次门槛。
- **survival**：没有说明由召唤物、石头坦克、治疗树、塔生命还是纯击杀速度保护防线。正式版一般规则中的建筑/单位生命不能替代这条路线的生存模块。
- **spatial condition**：一般规则有圆形合法范围、资源邻近与射程，但该玩家没有提供金矿/龙的站位、扩圈方向、覆盖或敌人路径。
- **payoff owner**：最保守只能把输出归给被重复部署的龙；不能把正式版评论列出的其他单位或主动技能自动并入它。
- **pivot/counter**：唯一明确选择是放弃 fireball dragon；没有敌人类型、坏波次、资源压力、替换时点或对手反制。
- **version context**：策略来自 2024-03-22 Earth Demo，不能无差别代表 2024-03-29 正式四地图版或 2026 移植版。

这条路线有真实实践价值，但仍缺构筑语法中的 survival、spatial condition 和 counter/适应窗口，也缺第二份独立实践材料。把韩文评论的苹果、石头、治疗树、技能和随机升级补进来会制造来源中不存在的混合阵容。

## 能确认的系统轮廓与不能迁移的结论

- 规则轮廓显示三种资源职责分离：stone / gold 支付建设，mana 支付主动技能；经济建筑、直接攻击塔和召唤塔争夺时间与空间。
- 圆形部署边界会随建设扩张，资源建筑还受资源点邻近约束；这让“扩地”和“产能”可能同时成为防线暴露成本。
- 随机三选一同时容纳新单位、已有单位强化与技能强化，且部分卡有限量；但没有完整卡池、稀有度、权重、刷新、封顶或升级叠加规则。
- 召唤物可形成数量防线、石头单位可承担耐久、治疗树可提供恢复，这些只是角色线索。没有人口/占位上限、继承、归因、死亡结算或性能保护规则。
- 没有装备、遗物、队伍属性转单核、Shield、Armor、元素反应或 defense-to-offense 转换的可读证据。火、水波、闪电只是攻击表现/单位名线索，不能据此建立本项目的元素体系；石头坦克也不能据此建立土盾体系。
- 资源位置难辨、单位难辨和 Strawberry 数量导致 FPS 下降是值得后续对照的可读性/性能问题，但均是少量个人样本，没有补丁链、复现条件或开发者回应，故不登记 lifecycle evidence，也不增加 negative/reworked 计数。

## 未决问题

- 11 座塔、5 个技能、全部升级卡和 40+ 敌人的文字规则、数值、目标与版本差异。
- 建筑涨价、资源建筑折扣、资源节点邻近、扩圈、拆除/替换和机会成本的精确时序。
- 单位/召唤物的数量、占位、路径、目标优先级、碰撞、仇恨、死亡、治疗和伤害归因。
- 至少一套正式版具名、可复现的经济—塔—升级—技能—站位构筑及其失败波次、pivot 与 counter。
- Demo、2024 Steam 正式版和 2026 Xbox/Windows 移植版之间的规则/平衡差异。
- 任何正式补丁、开发者复盘、当前统计或具有时间戳/可读转录的实践视频。

## Disposition

`insufficient-evidence`

本作已经发行，且正式版长评与 Demo 评论提供了比商店文案更具体的规则和策略线索，因此不是 `discovery-only`。但目前没有一个来源包能闭合一套版本化构筑，也没有独立实践资料补足 Demo 龙路线的生存、空间和反制。按既定门槛不登记 deep source、不生成 deep evidence、不增加生命周期案例；保留完整检索与缺口，等待未来出现正式攻略、补丁或可读转录后复核。
