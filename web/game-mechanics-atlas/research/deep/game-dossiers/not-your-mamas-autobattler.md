# Not Your Mama's Autobattler

## 身份与时期

- `title_id`: `not-your-mamas-autobattler`
- Steam App：`1993900`
- 开发 / 发行：Ancient Empire Games LLC.
- 状态：2022-10-11 已发行；2026-09-03 商店页仍可访问。没有可读官方新闻或补丁链，不能据此判断当前维护状态。
- 子类型公开承诺：多人异步 Survival 与十人 Tournament；购买英雄、武器、装备和技能后自动战斗。
- 证据结论：已发行，但无法满足“一条实质规则来源＋一条独立策略/构筑/实战来源”的 deep gate。

## 检索日志

访问日期统一为 2026-09-03。

### Steam 官方与社区

- 商店页和 App Details API：只确认身份、发行日、Survival 从十颗心开始、败局逐步增加生命损失，以及 Tournament 先到十分获胜等产品描述；按规则不计 deep source。
- Steam News API：零条新闻。Community `allnews` 无官方公告或同步新闻正文。
- Steam Guides：没有可见玩家攻略。
- Steam Discussions：逐一打开六个 General Discussions 主题：
  - `Feedback`：唯一含连续玩法细节的帖子；早期玩家与开发者讨论教程、20 gold 首轮、reroll 三英雄、物品拖换、状态反馈、Outlaw/lifesteal/siphon、Barbarian 减伤和 Practice/Survival 对手归属。
  - `Can't Drag Hero Into Hero Slot and Typos`：教程拖放与拼写修复；没有可复用构筑。
  - `Couldn't find players for the tournament`、`Looking for friends to play in the tournament`：只说明当时匹配困难。
  - `THIS GAME IS DOPE`、`I had the same idea`：宣传性意见与投资邀约；没有机制正文。
- Steam Reviews API：九条评测全部检查。正文只有概括性“多种 build strategies”、偏爱 Tower Agents、适合朋友游玩、价格或匹配人口意见；没有任何可按构筑语法核验的队伍。

### 外部检索

- Brave、Google、Bing 与 DuckDuckGo：用精确标题及 `guide`、`strategy`、`build`、`gameplay`、`review`、`tips`、`patch`、`update`、`gladiator`、`weapons`、`skills`、开发者名和 App ID 组合检索，并尝试 Steam/YouTube/Reddit/开发者域名 site 限定。
- 可访问结果只有 Steam、MobyGames、Metacritic、SteamDB、价格/相似游戏/玩家数聚合页；它们重复产品描述或元数据，没有规则、构筑、反制或补丁正文。
- YouTube 精确标题搜索只找到官方 reveal trailer 和一个 2022-10-11 新 Steam 游戏汇总候选。两个页面的字幕列表与 timedtext 均为空；不把无字幕画面、标题或缩略图当证据。
- 官方 Steam 支持字段没有网站 URL，只留下 `ancientempiregames.com` 邮箱域名。当前 HTTP 返回 502、HTTPS TLS 失败；Internet Archive CDX 没有该域名的成功快照。Steam 页历史快照仍是产品资料。

## 唯一可读的机制样本及其边界

`Feedback` 讨论是一条有价值但孤立的早期样本：

- 玩家称 Practice/Survival 首轮有 20 gold，常见做法是 reroll 到三名英雄，并提出“三英雄＋两件物品”才会产生更多开局差异；这不是完整商店概率、费用或最佳策略。
- 玩家尝试全 Outlaw，认为 lifesteal 套路可行且 siphon 很强；没有给出具体英雄、装备、位置、升级、对局或反制，不能构成一套可核验 build。
- 一个 tome 技能使敌人多承受 30% 伤害，但状态没有在战斗 UI 显示；开发者承认 conditions/effects 反馈不足。
- 开发者说明 Practice 对 bot，Survival 对其他玩家；同类物品应能在单位间拖动，熔炉/垃圾桶和跨单位拖换当时存在问题。
- Barbarian 的“50% physical damage”文本与实际减伤不一致。玩家用 10 Armor、30 bow damage 和 10 circlet protection 复测后，开发者定位问题并承诺下个 patch 修复；没有可读取补丁正文确认最终结果。

这些信息只能说明早期版本存在经济开局、职业/吸血主题、状态增伤、减伤和物品所有权等线索。因为全部集中在同一讨论，且没有第二份独立规则或实战资料，它们不进入 `mechanic-evidence.json`，也不被拆成多个负面/重做案例。

## 无法完成的构筑语法

- **engine**：只有“全 Outlaw＋lifesteal/siphon”标签，缺具体英雄与触发链。
- **state/resource**：可见 gold、英雄、物品和部分状态，但缺价格、商店概率、升级与持续时间。
- **payoff**：吸血与增伤可能是收益，缺伤害/治疗所有者和结算顺序。
- **survival**：Barbarian 减伤、Armor 与 circlet protection 出现在单次复测，缺完整防御关系。
- **spatial condition**：未找到站位、格子、相邻、前后排、目标或移动规则正文。
- **payoff owner**：物品可在同类槽位间转移，但技能、职业和装备的最终所有权契约不完整。
- **pivot/counter**：没有可核验的替换、坏对局、敌方反制或适应窗口。
- **version context**：讨论发生于 2022-10 发行前后；没有公开补丁链将这些观察映射到当前版本。

因此不能为了“至少一套构筑”而把三英雄、Outlaw、lifesteal、siphon、Barbarian 和增伤 tome 拼成一支来源中从未出现过的队伍。

## 模式、经济、空间与失败解释缺口

- 商店描述可确认 Survival 的十颗心和递增失败损失，以及 Tournament 的十人/十分目标，但没有完整配对、轮次、伤害、超时、积分和并列规则。
- 没有商店概率、英雄池、队伍上限、升级/出售、物品槽、技能池、刷新成本或经济利息表。
- 没有队形、站位、目标优先级、攻击/施法顺序、速度、范围、移动、召唤占位或战场对象规则。
- 没有可核验敌人/对手包、具体坏对局、侦察信息或 pivot 时点。
- 早期帖能证明条件、效果和减伤文本曾经不可读，但没有最终战报、伤害归因或修复后的规则。

## 生命周期与社区边界

- 发行前后讨论出现教程拖放、物品转移、状态显示和 Barbarian 减伤缺陷；开发者在帖子中回应或承诺修复。
- 没有官方补丁说明或后续实战来源确认哪些修复进入最终版，因此不将这些帖子升级为完整 lifecycle evidence。
- Tournament 匹配困难和 2026 单个评测者称排行榜只有自己，只能说明个别时点的体验；不能反推人口、留存、机制质量或商业因果。
- 九条评测的总体正负比例不用于机制判断。

## 对本项目的可用与不可用信息

可以保留为后续搜索提示、但尚未验证的方向：职业主题可横向连接英雄、装备和技能；状态增伤与吸血需要战斗中可见；物品转移必须明确槽位类型与所有权；首轮经济应允许不止一种合法开局。

不可作为设计依据：20 gold、三英雄、十颗心、十人 Tournament、Outlaw、Tower Agents、Barbarian、tome、siphon、lifesteal 强度、物理减伤公式和任何未读到正文的站位/技能推断。

## 未决问题

- 最终英雄、职业、装备、技能与状态数据库。
- 队伍上限、位置、目标和结算顺序。
- 商店、刷新、升级、出售、物品槽和经济规则。
- 一套具名、版本化、可复现的队伍及其反制。
- Practice、Survival、Tournament 的完整对手、积分和失败伤害规则。
- 2022-10 后是否有补丁、服务器或维护状态变化。

## Disposition

`insufficient-evidence`

它是已发行长尾游戏，不应降为仅未发行承诺的 `discovery-only`；但目前只有一条早期 Steam 反馈帖包含实质机制/实战片段，无法与独立规则源和另一套实践材料交叉。按既定门槛不登记 deep source，不生成 deep evidence，也不把资料缺失本身当成机制或失败案例。
