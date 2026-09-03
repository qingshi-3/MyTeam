# Dungeon Tactics: Idle, Incremental, Autobattler

## 身份、范围与研究深度

- `title_id`: `dungeon-tactics-idle`
- 开发 / 发行：Rubén Arranz；Steam App `4194860`。
- 状态：Windows 单机版于 2026-07-27 正式发行；商店没有官网或 support URL，只提供 `defendersguildthegame@gmail.com`。
- 子类型：桌面常驻式 idle / incremental autobattler，以六人队伍、前后两排、战斗升级和技能树为公开产品框架。
- 时期：可核验机制主体来自 2025-12 至 2026-03 的公开 Demo。2026-07 正式版只有一条首周评测；作者发评时仅游玩 31 分钟，访问时账号累计时长为 46.4 小时。它没有提供当前构筑、完整规则、补丁或指南正文，后续累计时长也不能反向增强原评论。
- 深度：5 个实质非商店来源——一篇完整 Demo 阵容 / 站位分析、三个带 Steam `[developer]` 标记的规则 / 平衡回复页、一条正式版详细评测；8 条标准化 evidence。它足以闭合一套历史 Demo 构筑，但不足以描述正式版 meta，因此 disposition 为 `retained`，不是 anchor，也不是 current-build 样本。

## 来源包与独立性边界

| 功能 | 数量 | 内容 |
| --- | ---: | --- |
| 实践构筑 | 1 | Possessed 的完整反转前后排队伍、目标理由、替换判断和极端敌阵 |
| 开发者规则 / 生命周期 | 3 | base stat 放大；Demo 进度 / rank 规划与上线回复；僵持、退出按钮和 x4 稳定性 |
| 正式版详细评测 | 1 | 唯一公开 Steam 评测；作者发评时 31 分钟，报告英雄 / 技能树 / 等级 / 金币循环以及没有 loot/equipment |

三个开发者页都来自同一 Steam 社区、同一开发者账号，不能当成三家独立机构；实践构筑也只有一名玩家。来源包的价值在于功能异质，而不是观点数量。商店页只用于身份 / 发行边界，不进入 deep source 或 evidence。

## 产品与版本地图

| 节点 | 可用语义 | 不可外推 |
| --- | --- | --- |
| 2025-12 Demo | 玩家报告 10–15 分钟解锁 Demo 内容、约 5 小时后六名英雄 level 20 / 约 stage 81；开发者解释 Demo 约 40 分钟、升级曲线、rank 计划和后续内容计划 | 5 个 dungeon × 80 stages 是明确标注 `could change` 的发行前计划；rank 5 / 10 / 20 行为示例不等于已验证最终表 |
| 2025-12-06 Demo 更新 | 开发者在讨论中称已发布 XP ranks 与小型 QoL 更新，并说明右键入口；同页玩家确认至少数字缩写变化可见 | 没有补丁正文或客户端复核，不能证明帖子里每个计划行为均已实装 |
| 2026-03 Demo | 玩家给出一套明确标注 `DEMO version` 的六人队伍；另一页开发者确认 rank 增加 base stat，并经 stat / character upgrades 放大 | 该阵容不能跨越到 2026-07 正式版，`BIS` 只是单玩家判断 |
| 2026-07 正式版 | 唯一评测报告金币推进、英雄、技能树和等级成长，并明确称没有 loot/equipment；发评时 playtime 为 31 分钟 | 一条首周短时正评不能证明总体口碑、平衡、英雄强度、正式版阵容或未来内容；访问时累计 46.4 小时不改变原评论写作时的证据强度 |

没有公开 patch chain、Steam News、Steam Guide、官方规则书、wiki、数据库或版本化当前攻略。因此不把 Demo 页面与正式版评论拼成一套同时存在的规则。

## 核心循环与真实决策

1. Demo 实践显示玩家围绕六个位置安排前后两排，让不同耐久与目标能力的角色承担不同威胁。具体目标算法没有官方正文；只能采用帖子中观察到的敌危险后排攻击己方后排这一局部模式。
2. 角色在战斗中获得 XP / rank，另有购买式 character / stat upgrades。开发者说明加成读的是 base stat，不是当前 final stat；base 增量随后被其他升级放大。
3. 金币用于升级并推动关卡。一个玩家报告早期解锁过快、后期约 33k / 次升级和约一小时 200k 收入，但这是把 30–40 分钟 Demo 长时间挂机到设计边界之外的单样本。
4. rank 的特殊行为、更多角色、更高治疗比例和新攻击曾以开发者计划 / 示例出现；只有 2025-12-06 的“XP ranks 更新已发布”和 2026-03 的 base-stat 回复能确认公开 Demo 已有 rank 层，不能据此重建最终技能树。
5. 1-7 的 Orc + healer 可与 Cleric-front / Archer-back 形成极长战斗。开发者保留 `back to main` 作为退出路径，并称超过 x4 会让模拟失控，x4 本身已需要稳定性补丁；没有自动 overtime、强制平局或正式版超时规则证据。

## 历史构筑：Demo 反转前后排

来源作者把它称作自己的 `BIS team`，不是统计结论。

### 队伍与位置

- 后排：Warrior、Cleric、Sharpshooter。
- 前排：Twin-axe Healer、Archer、Basic Mage。

### 构筑语法闭环

- **engine**：观察敌方危险后排会攻击己方后排，于是反转传统职责，让耐久角色 / 较耐久输出承受该方向压力。
- **state/resource**：六个部署槽、前后排身份、每个角色的 HP / defense / attack cadence、可攻击敌方后排的能力、level / rank / upgrade 投入。
- **payoff**：Basic Mage、Sharpshooter、Archer 三名具备帖子所述敌后排伤害能力的成员负责先杀危险目标；最终伤害仍由具体攻击者拥有。
- **survival**：Warrior、Cleric 和 Sharpshooter 被放在后排吸收危险敌后排的攻击；Twin-axe Healer 因治疗价值更高而被作者放到前排避开主要压力。
- **spatial condition**：固定两排；敌我前后排目标关系决定单位价值，而不是“坦克必须在前排”的职业标签。
- **payoff owner**：Warrior / Cleric 的耐久与治疗为输出买时间；Basic Mage / Sharpshooter / Archer 拥有各自伤害。没有 defense、HP 或 shield 自动转换成攻击的规则。
- **economy / pivot**：六个位置意味着选入一个角色就排除另一个。作者用 Warrior 替换 Tank，理由是其速度、HP 与 rank-10 能力更好；Lance Mage 因主要打低威胁前排而被弃用。金币 / rank 只能说明长期强化，不能证明这套队伍的最优升级顺序。
- **counter / abort**：作者给出的极端敌阵是前排三 Healer、后排三 Thief；其 Demo 进度停在约 level 80。这个描述是一名玩家的假设 / 实战边界，不是可重复统计。
- **version context**：仅限 2026-03 Demo；不称为正式版强势阵容。

## 空间、目标与角色标签的设计含义

- 这个案例最有价值的不是六个角色名，而是“威胁方向可以反转职业默认站位”。如果危险技能从敌后排直接读己方后排，那么 `frontline` / `backline` 是空间坐标，不是 tank / DPS 的永久同义词。
- Warrior 优于 Tank、Lance Mage 失去价值都来自同一玩家对当时目标价值的判断。没有攻击范围、仇恨、随机性、速度公式或目标优先级官方正文，不能从结果反推完整 AI。
- 2025-12 的开发者曾举例说明 rank 可能给予同列减伤或后位增伤，但当时用词是 `The idea` / `for example`；它只显示位置可以成为升级读取器，不证明这些精确效果进入正式版。
- 1-7 僵持说明“谁能打到谁”和“治疗能否抵消输出”会改变实际完成时间。它不是盾体系，也没有元素、反应、召唤、装备或遗物链。

## 升级、经济与生命周期

- 开发者给出的 final stat 组成至少包括 base stat、character rank、character upgrade 和 stat upgrade。`+2 BASE attack` 可能在当时进度下成为 final `+20 / +40 / +80`，但没有完整公式、舍入、叠加顺序或上限。
- 这是一种“底层基数 × 多层放大”的增长结构。小 base 奖励的显示如果只写 `+2 attack`，玩家会误判价值；UI 至少要同时说明被修改层与预计 final 变化。
- Demo 设计时长与玩家长时间挂机之间存在明确错位：一页开发者称 30 分钟，另一页称 40 分钟；玩家分别报告约 3 小时后全满和 5 小时 / stage 81 后进入瓶颈。这里记录的是 Demo 范围失配，不是正式版内容不足结论。
- 正式版唯一评论在发评时只游玩 31 分钟，提到金币 / 等级推进与技能树，同时明确没有 loot/equipment；访问时累计 46.4 小时不能反向改变该短评的观察窗口。2026-03 玩家提出的“每人一件装备、由 level-10 chest 掉落”完整方案只是建议；不能登记为原游戏系统，更不能把它当成盾转伤或团队属性转核心的先例。

## 反制、失败解释与可读性

- **构筑失败**：输出单位被敌后排先杀；错误角色只打低威胁前排；治疗 / 低伤组合形成僵持；六槽内带入功能不匹配角色。
- **适应窗口**：战前交换前后排或换入能打敌后排的成员；继续解锁 / 升级角色；无法收束时返回主界面。来源没有证明战斗中换位或技能指令。
- **应报告的原因**：每名单位实际目标、攻击来源方向、可达排、治疗抵消量、战斗时长、退出而非胜负的结果。否则玩家只会看到“Tank 在前排却更差”。
- **速度边界**：显示 x4 不等于模拟可无限加速。开发者把更高倍率与稳定性风险直接关联，支持固定速度档和确定性验证，但不证明当前正式版仍有同一缺陷。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

### Steam 全量路线

- 商店页、App Details API、package 字段和精确商店搜索：确认 App 4194860、开发 / 发行者、Windows-only、2026-07-27、`coming_soon=false`、无官网 / support URL；App Details 没有 `demos` 字段，精确搜索只返回主 App。身份页不计 deep source。
- Steam News API 返回 0 条；Community `allnews` 显示没有更多内容。
- Steam Guides 页面没有任何条目。
- General Discussions 共 6 个主题，全部打开。四个与机制相关的完整主题进入来源包；`Any chance for allowing custom images?` 与机制无关，`Resolution/Window issues` 是两名玩家的超宽屏 / 强制吸附问题，不为凑数登记。
- 全语言 Reviews API 只有 recommendation `231693744` 一条，已完整读取：发评时 31 分钟，访问时累计 2784 分钟（46.4 小时）。它只用于该玩家首周短时内容边界，不代表总体评价，后续累计时长也不提高原评论强度。

### 外部、语言与历史路线

- DuckDuckGo、Bing RSS、Google 用精确标题、App ID、`guide / strategy / build / gameplay / review / patch / update`，以及西语 `guía / reseña / actualización` 组合检索；结果主要是 Steam、SteamDB、价格 / 玩家数聚合页或无关噪音。
- 精确检索 `Rubén Arranz`、`chikotrongames01`、`chikotron1` 和支持邮箱没有找到开发者规则站、博客、wiki、Reddit、itch.io 或独立攻略。Steam 开发者搜索及相关作品没有交叉公告。
- SteamDB app / info / patchnotes / history 返回 403，未绕过。Wayback / CDX、开发者与商店历史路线没有提供可读的新机制正文。
- YouTube 找到 Game Submarine 于 2026-08-17 上传、时长 4341 秒的视频 `mvqZj0LNdwg`，但没有 caption track，description 只有类型标签；不把标题或不可检索画面当证据。另一个相似名 `Idle Pixel Battler` 属于 App 4809070，已排除。
- 当前环境没有可用 CUA 浏览器；所有可访问 HTML / API 正文均通过只读请求检查。没有绕过登录、robots、403 或下载客户端 / Demo。

继续检索已只返回同一组 Steam 页面、无字幕视频或聚合元数据，未产生新的 owner、构筑、经济、空间、反制或版本信息，因此停止。低来源密度保留为档案限制，不用镜像或无关 UI 帖填数。

## 对本项目的可迁移与不可迁移信息

可迁移：

- 把前排 / 后排定义为空间条件而非职业身份；敌方威胁方向可以迫使玩家反转站位。
- 明确 base / bonus / final stat，所有转换器都要说清读取哪一层、何时快照、由谁放大。
- 防御本身先购买存活时间；若要变成伤害，必须由独立装备 / 遗物 / 英雄读取器显式桥接并保留最终 owner。
- 高倍率观察需要固定模拟步、确定性验证和可退出僵持，而不是无限提高时间缩放。

不可迁移：

- 不能把单玩家 Demo `BIS` 直接写成通用站位规则或正式版 meta。
- 不能采用帖主设想的装备 / 宝箱 / 稀有度，因为正式版唯一评论反而报告没有装备。
- 不能从这组资料发明盾、冰、土、元素反应、队伍 defense / HP 转单核攻击、完整 target AI 或超时规则。
- 本项目有 10×6 网格、18 个玩家候选格、持续移动与独立战术指令；这个两排桌面 idle 的固定六槽不能直接复制。

## 未决问题

- 正式版 16 名角色的技能、rank 树、升级公式与最终上限。
- 六个队伍位置的精确坐标、目标选择、速度、范围、攻击 / 治疗顺序和同时事件规则。
- 金币收入、升级成本、离线进度、解锁、替换和是否存在重置 / 转型机制。
- 正式版关卡 / dungeon 数量、enemy set、Boss、超时、失败与奖励。
- 当前版本是否仍存在 Demo 的反转站位优势、1-7 僵持或 x4 稳定性边界。
- 正式版是否未来加入 loot/equipment；当前只知道一名玩家报告没有。

## Disposition

`retained`

保留理由是：带 `[developer]` 标记的回复为 base-stat / rank / 速度与版本边界提供一手规则，独立玩家帖子则给出一套可回到原文核验的六人 Demo 构筑，满足功能不同的 rules + practical 最低包。保留对象严格是“2026-03 Demo 反转站位构筑与相关升级 / 僵持边界”，不是正式版阵容、成功样本或装备系统。资料稀疏使它只获得有限的 8 条 evidence；不升级为 anchor，也不制造第二套构筑。
