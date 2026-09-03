# Auto Grind: Dungeons

## 身份与时期

- `title_id`: `auto-grind-dungeons`
- Steam App：`3869250`；开发 / 发行：Masked Meerkat。
- 截至 2026-09-03，Steam 与公开 App metadata 均标记 `Coming soon` / `prerelease`，没有发布日期。
- Steam App Details 没有 Demo 关联；Store Search 对精确标题只返回主 App。公开 metadata 也没有可见 depot，不能据此否定私有测试 build，只能确认没有公开可下载版本。
- 商店称 Discord 可参与 Beta，但公开邀请只能确认服务器入口，不能确认测试资格、版本、开放范围或当前可玩状态。封闭 / 邀请制 Beta 不等于公开 Demo、Playtest 或已发行版本。
- 证据结论：条件式自动施法、可选自动移动、五人跨职责塑形与 Combinator 装备目标都高度相关，但当前只有官方产品承诺，没有实质非商店规则源或独立实践源，故为 `discovery-only`。

## 自适应深度判断

- **系统复杂度**：公开文本涉及最多五人队、三种职业任意组合、攻击 / 法术 / buff / aura / passive、cooldown、condition、自动拾取、可选自动移动、装备合并 / 精炼与角色级 BIS 目标；潜在复杂度中高。
- **机制独特性**：真正值得追踪的是“玩家先写条件与冷却构筑，战斗再自动执行”的间接控制，以及移动自动化可以选择开启或关闭。Combinator 还可能把随机掉落转成定向装备成长，但规则未公开。
- **项目相关性**：技能触发权、AI 行为边界、装备 owner、五人角色职责、自动战斗可读性、掉落清理、长期 grind 与反复挑战都直接对应本项目的 build / automation / failure-explanation 问题。
- **资料密度**：产品材料密度低，规则与实践密度为零。没有公告、手册、开发日志、公开 Demo、Guide、讨论、评测、社区视频或可读实战。
- **版本变化**：没有公开版本号、patch、devlog 或 release window，无法建立可玩版本链。官网与商店当前文本只能视为一个未发行产品快照。
- **选择的深度**：完整拆解 automation、Combinator、职业 / 角色 ownership、dungeon loop 与失败解释的证据缺口，同时审计公开商店、官网、社区、视频、代码托管、独立平台、搜索与存档路线。
- **停止原因**：重复精确标题、App ID、工作室、gameplay、guide、build、Beta、Combinator 与 conditional skill 搜索只回到商店、同源官网、私有 Discord 入口或无关同词结果，不再改变理解。按 diminishing-return test 停止，不用营销词合成一套虚构队伍。

## 版本与发行地图

- Steam App 3869250：Windows 单人游戏；英文与巴西葡萄牙文界面；`Coming soon`，没有精确日期。
- App Details 的 `demos` 为空；精确标题 Store Search 只有 App 3869250，没有独立 Demo / Playtest 商店条目。
- Steam News API 为零条；Community 的 News、Guides、Discussions、Reviews、Videos、Screenshots 与 Workshop 都没有玩家或开发者正文。Reviews API 为零评测。
- Steam 的当前产品页邀请玩家到 Discord 参加 Beta。邀请可解析到名为 `Auto Grind: Dungeons` 的服务器，但服务器消息与测试 build 不公开；没有登录、加入、索取权限或读取私域内容。
- Masked Meerkat 官网根页仍嵌入 App 3869250 的 Steam widget；商店所链 `/auto-grind-dungeons` 当前为 404，sitemap 只列根页。官网没有公开版本、博客、规则、manual 或 devlog。
- 无法确认首次测试日期、当前 Beta 版本、测试是否仍招募、私有分支与主 App 的对应关系，也不能把商店素材更新时间当成游戏版本。

## 检索日志

访问日期统一为 2026-09-03。

### Steam 官方与社区

- 读取英文与巴西葡萄牙文 App Details、商店正文、Store Search、公开 app metadata 和 Steam widget。葡萄牙文是同一产品说明的本地化，不构成第二来源。
- 审计 News API / All News、Guides、Discussions、Reviews API / Reviews、Videos、Screenshots、Workshop 和 Community Hub；没有公告、规则、构筑、Beta 记录或公开实战。
- 商店没有关联 Demo，也没有公开 trailer / Community video 可提供字幕。官方截图不用于推断角色技能、装备属性、UI 字段、站位或地牢结构。
- 第三方 public app-info 路线只确认 `prerelease` 与无公开 depot metadata；SteamDB 403，SteamPeek 只重复商店标签 / 相似游戏，均不计 deep source。

### 开发者官网与封闭 Beta

- `maskedmeerkat.com/auto-grind-dungeons` 当前 404；根页是单页工作室介绍、联系方式和本作 Steam widget。`sitemap.xml` 仅列根页，`robots.txt` 没有额外公开内容路由。
- 官网静态 bundle 只包含同一 Steam widget、工作室地点 / 服务和联系方式，没有隐藏的游戏数据、blog、patch、manual、press kit、社媒链接或第二作品。
- Discord 邀请公开 API 能确认服务器名称和入口，但 widget / 公共消息不可读。按边界不加入服务器、不请求 Beta、不把服务器人数、规则频道名称或 Beta 宣传当成机制 / 实战证据。

### 外部规则与实践路线

- YouTube 以精确标题、去标点标题与工作室名检索，没有可归属本作的视频；大量同词 grind / dungeon / meerkat 结果均排除。不存在可读取字幕的本作实战。
- itch.io 精确标题与 Masked Meerkat 搜索没有本作条目；GitHub repository API 对精确标题和工作室均为零。
- Reddit 公共搜索返回 403；没有绕过登录或访问限制。Bluesky 公共搜索同样不可访问。
- Google 与 Brave 返回 429；DuckDuckGo 返回 challenge；Yandex 要求验证码；Mojeek 403；Yahoo 报搜索故障；Baidu 无有效正文。Naver 精确标题只返回 Steam 商店；Bing RSS 被 `auto` / `grind` 严重误分词并返回无关结果。搜索摘要不计来源。
- Internet Archive CDX 请求超时；Common Crawl 对工作室域名没有捕获。没有把不可读快照、缓存或索引元数据升级为规则。
- SteamDB、IndieDB 与 ModDB 路线被阻断；SteamPeek 与其他聚合页只重复商店身份 / 标签。没有找到 press release、采访、评测、wiki、GameFAQs、论坛帖子或公开测试记录。

## 官方产品材料中的机制线索

以下只作为未来复查的 discovery leads，不登记 deep source，也不生成 evidence：

- 单人 dungeon grind；玩家管理最多五名角色。
- Warrior、Archer、Mage 三种职业可任意组合，并声称每个角色的 build 能塑造成任意职责。
- 技能类别包括攻击、法术、buff、aura 与 passive；所有效果按 cooldown 与 condition 自动触发。
- 技能使用与 loot pickup 自动化；movement 可选自动化，说明移动权可能在玩家和 AI 之间切换。
- Combinator crafting 用于 merge / refine items，并朝每名角色的 build BIS 目标推进。
- 产品目标是让玩家把注意力放在 theorycraft、角色成长、build synergy 与 strategy，而不是持续主动操作。

这些句子没有公布任何具名技能、装备、职业被动、trigger condition、cooldown、目标选择、队形、地牢、敌人、资源、掉落表、配方、失败条件或实际玩家 build。

## 自动化边界与关键缺口

- **技能触发权**：不知道 condition 是系统固定、装备生成、玩家可编辑，还是只表示内部 AI 条件；也不知道优先级、同帧顺序、无合法目标、冷却重置、资源不足和重复触发如何处理。
- **角色行为权**：自动 use skill 不说明普攻、选敌、走位、追击、撤退、复活或队友保护的决策树。职业可塑造成任意 role 也没有行为模板或仇恨规则支撑。
- **移动切换**：optional auto move 不说明关闭后是直接控制全队、单一领队、点击目的地、暂停指令，还是角色保持原位；不能推导成本、战中微操量或与本项目两条战术指令的兼容性。
- **时间模型**：cooldown 不说明是实时秒数、行动回合、攻速缩放、共享 GCD、charge 或条件等待。没有技能节拍、施法打断、前后摇和 Boss 时点。
- **失败解释**：自动系统若不公开 condition 判定、目标原因、冷却、移动意图、伤害 / buff 来源和未触发原因，玩家无法区分错误 build 与错误 AI；目前没有 HUD、战报或 timeline 的文字证据。

## Combinator、装备与成长缺口

- merge / refine 不说明输入数量、同名 / 同槽 / 同稀有度要求、随机与确定性边界、数值继承、词缀保留、失败、回退、锁定或材料成本。
- `BIS goals` 只说明设计目标，不证明系统提供定向配方、pity、目标筛选、自动分解或可预测终点。
- 不知道装备属于角色、职业、slot、账号、run 或 dungeon；也不知道角色替换、职业转换、死亡和重组时能否转移。
- 自动拾取不说明背包容量、过滤器、稀有度、自动装备 / 分解、战斗中结算或重复 grind 的库存压力。
- 没有经验、等级、技能点、装备位、品质、角色招募 / 重置 / 重生、局内 / 局外成长边界，无法判断 grind 是策略进度还是纯数值累积。
- 没有经济 / 掉落 / crafting 实战，不能判断 Combinator 是减少随机性的定向工具，还是把大量垃圾掉落压缩为另一次随机抽取。

## 无法闭合的构筑语法

官方材料连一套最小可核验 build 都无法闭合：

- **engine**：只能说技能会按 cooldown 与 condition 自动触发；没有一个具名 supplier、trigger 或循环。
- **state / resource**：没有 Mana、Energy、Rage、buff stack、ammo、charge、Health threshold 或 cooldown 公式。
- **payoff**：没有具名攻击 / 法术、伤害类型、范围、状态消费或收益转换。
- **survival**：没有坦克、治疗、盾、护甲、控制、仇恨或复活规则；职业名称不能替代生存机制。
- **spatial condition**：没有站位格、lane、距离、射程、body blocking、目标优先级或手动 / 自动移动规则。
- **payoff owner**：不知道收益属于角色、技能、装备、aura source、全队还是 Combinator 结果。
- **economy / pivot**：没有角色取得、技能选择、掉落、装备配方、替换窗口或重练成本。
- **counter**：没有具名敌人、Boss、抗性、打断、驱散、后排访问、AOE、时间限制或失败样本。

因此不能从 Warrior / Archer / Mage 三个标签合成“战士坦克 + 射手输出 + 法师 buff”的队伍；“任意 role”反而要求未来证据证明职业、技能、装备与 AI 行为如何分权。

## 对本项目的研究价值与限制

值得保留为未来设计讨论的发现方向：

- 将自动战斗的策略焦点放在可见 condition、cooldown、target policy 与 owner，而不是把 AI 行为隐藏在职业标签后。
- 把自动技能、自动拾取和自动移动分成不同权限开关；每种自动化都应减少重复操作，但保留能改变结果的决策。
- 允许职业与职责正交：职业提供技能 / 属性候选，装备、技能与行为模板共同决定 tank、support 或 carry，而非名字直接锁死职责。
- Combinator 如果承担定向成长，应区分确定合并、随机精炼、锁词缀、目标追踪和垃圾清理，避免长期 grind 只制造库存劳动。
- 反复地牢需要清楚的“为何失败—需要哪类能力—在哪里能获得”的闭环，否则自动 grind 只会放大数值墙。

当前不能作为设计依据：五人容量、三个职业、任一技能 / aura / passive、自动移动形式、装备配方、BIS 路线、地牢结构、长期 progression、敌人 counter、离线收益或任何平衡结论。产品页也没有宣称离线进度，不能因 `Idler` 用户标签而加入离线挂机规则。

## 复查条件

- 若 Steam 出现公开 Demo / Playtest、正式发布日期、News / patch、可下载 build 或开发者公开规则，立即重审。
- 若没有状态变化，最早在 2026-12-01 之后做一次低成本复查：主 App、Store Search、News、Guides、Discussions、Reviews、Videos、官网 sitemap 与公开搜索。
- 保留门槛不变：至少一条实质非商店规则 / 机制源，加一条独立实践 / build / strategy 源。Steam 商店、官网 widget、Discord 邀请、截图和聚合页不能互相拼成两类来源。
- 优先验证：condition 是否可配置、技能 / 移动 AI 的权限、cooldown / target / order、五人职责塑形、Combinator 配方 / 继承 / owner、loot cleanup、dungeon failure loop、Boss counter 和战报归因。
- 封闭 Beta 只有在开发者公开发布可核验规则 / patch，或玩家在无需加入私域的公开页面给出完整实战时，才可能贡献 deep source；服务器存在本身不改变 disposition。

## 未决问题

- 是否存在独立的 Steam Playtest App 或仅有私发 Beta build；当前测试版本和公开计划是什么。
- condition 是可编程规则、预设 AI 条件、技能固有条件还是装备词缀，玩家能控制到什么粒度。
- optional auto move 的手动模式、控制对象、战斗暂停 / 指令成本和空间后果。
- Warrior / Archer / Mage 与 tank / healer / buffer / carry 的关系，技能、装备、属性和行为谁拥有职责。
- Combinator 的输入、输出、确定性、词缀继承、材料、失败、锁定、回退、目标追踪和库存处理。
- dungeon 的长度、重复规则、撤退 / 失败代价、Boss、敌人预览、掉落、经验和局内 / 局外 progression。
- 至少一套玩家实际使用的版本化五人或少人 build，包括 engine、state、payoff、survival、space、owner、取得路线、pivot 与 counter。

## Disposition

`discovery-only`

本作的高相关性来自明确但未展开的条件式自动化、移动权限切换、跨职责角色塑形和 Combinator 定向成长目标。当前主 App 未发行，没有公开 Demo / Playtest、版本说明、规则正文或独立实战；私域 Beta 入口不能替代可复核证据。按 Adaptive Depth Protocol 与 diminishing-return test，本 checkpoint 不登记 deep source、不生成 deep evidence、不编造职业队伍或装备路线。下一次有意义的恢复点是公开 playable build 或同时出现可读规则与独立实践材料之后。
