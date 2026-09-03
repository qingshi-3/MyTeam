# Monsters Auto Battler

## 身份、版本与研究深度

- `title_id`: `monsters-auto-battler`
- Steam App `3013370`；开发 / 发行：Soomka Games；Windows-only、英文、付费单机游戏，2024-12-21 正式发行，当前不是 Early Access。
- Steam App Details 没有 `demos` 字段，开发者目录也没有同名 Demo / Playtest。当前公开 package 只有主 App package；没有证据表明存在独立试玩或其他平台版本。
- 公开 appinfo 的 `store_asset_mtime` 与 public branch build 都停在 2024-06-13，早于 Steam 正式发行约六个月；它只能建立当前可见 manifest 的时间边界，不能证明该 build 当时公开销售、与发行版完全相同、发行后从未热修或当前客户端规则。
- Falcoware 的目标页和频道视频都使用同名产品与逐字相同的商店介绍，页面提供其自有分发入口；但页面没有标注 Soomka Games、授权关系、版本号或发布日期。Falcoware 224 秒视频发布于 2024-06-21，也早于 Steam 正式发行。它们最多是同名分发 / 宣传镜像，不能建立开发归属、二进制同一性或版本连续性。
- 深度结论：Steam 只有 1 条全语言评测，作者发评时游玩 12 分钟，正文仅称 Pyromancer 强势并催促在削弱前使用；没有技能、队伍、站位、经济、关卡、敌人或反制细节。News 为 0、General Discussions 为 0、没有玩家 Guide，外部也没有可读攻略 / wiki / 补丁 / 字幕视频。无法闭合一套真实构筑，故 disposition 为 `insufficient-evidence`，不登记 deep source 或 evidence。

## 身份与发行时间线

| 节点 | 可确认内容 | 不可外推 |
| --- | --- | --- |
| 2024-06-13 | Steam appinfo 的商店资产时间与 public branch build 时间；build id `14703878` | 不能证明这是公开 Demo、最终发行版、Falcoware 安装包或当前规则；manifest 时间不等于正式发布日期 |
| 2024-06-21 | Falcoware YouTube 频道发布 224 秒 `Monsters Auto Battler` 视频；无 caption track，description 复制商店介绍 | 无 Soomka 署名、版本号、授权说明或可检索解说；不把无字幕画面当机制正文 |
| 日期不明的 Falcoware 页面 | 页面标题同名，正文逐字重复 Steam 的完整产品介绍，并提供 Falcoware 自有下载 / torrent 入口 | 不下载、不运行；不据此认定官方站、相同 build、免费 Steam 版或跨平台版本 |
| 2024-12-21 | Steam 正式发行；`releasestate=released`、非免费、Windows-only、没有公开 Demo 字段 | 没有补丁 / branch 历史正文证明 2024-06 至正式发行或发行后的规则变化 |
| 2024-12-22 | 唯一 Steam 评测发布；发评时与累计游玩均为 12 分钟 | 不能建立 meta、Pyromancer 规则、OP 程度、削弱计划、胜率或长期体验 |
| 2026-09-04 访问快照 | Steam News 0、General Discussions 0、玩家 Guides 0、全语言 Reviews 1；五个 level achievement 仍可见 | 缺少来源不等于游戏没有更深规则，只说明公开可访问语料不足 |

## 发现层规则轮廓

以下内容只来自 Steam / Falcoware 共用的产品文案与商店资产，因此只用于说明检索对象，不进入 deep evidence：

- 玩家先在小型竞技场放置 mercenaries，随后只观察自动战斗；文案强调“谁放在哪里”比即时操作更重要。
- 敌人至少被宣传为近战、远程、接近士兵后爆炸、治疗或生成新敌人等类型。
- Steam 成就只显示 `Level 5 / 15 / 25 / 35 / 50`，描述为空。当前全局百分比接口能返回各成就百分比，但没有样本分母、版本或失败原因，不能反推出关卡难度、留存或构筑门槛。

这些词能提出 formation、射程、爆炸接近条件、治疗优先级、召唤占位等后续问题，却不能单独证明目标算法、单位属性、招募经济、升级、技能、战斗顺序或真实玩家打法。

## 最接近构筑的线索：12 分钟 Pyromancer 单句评论

唯一 Steam 评测 recommendation `183429184` 由 Maria Cogumela 于 2024-12-22 发布；API 与公开评测页均显示发评时 / 累计游玩 12 分钟。正文只有：`PYROMANCERES OP META ABUSE ANTES Q NERFEM`。

它只能作为搜索线索，不能满足构筑门槛：

- **engine**：未知。没有 Pyromancer 的技能、攻击、燃烧、元素或升级正文。
- **state/resource**：未知。没有 Mana、Gold、人口、冷却、层数、装备、购买或等级投入。
- **payoff**：只知道玩家主观称其 `OP`；伤害形式、数值、目标、owner 和触发均未知。
- **survival**：未知。没有前排、治疗、控制、护盾、闪避或替代承伤者。
- **spatial condition**：商店文案泛称站位重要，但评测没有给出 Pyromancer 的位置、射程、保护或目标关系，二者不能拼成具体阵容。
- **economy / pivot**：未知。没有如何获得、升级、替换或放弃 Pyromancer 的信息。
- **counter / failure**：未知。没有敌人包、失效场景或适应窗口。
- **version / lifecycle**：`antes q nerfem` 只是该玩家“趁削弱前”的催促，不是官方削弱公告。News、补丁和开发回复均为空，因此不能声称存在已计划 / 已执行 nerf 或确定的 meta。

保留这条线索只为了说明实际搜索没有漏掉唯一社区文本；它不登记为 `detailed-review`、`strategy-guide` 或 deep source，也不生成 Pyromancer evidence。

## 版本、平台与分发边界

- Steam 是唯一能够确认开发 / 发行者、正式发行日、操作系统和 release state 的一手身份节点。
- Falcoware 页面正文与 Steam 产品段落逐字一致，视频 description 也重复同一段。Falcoware 自称免费游戏分发站，但目标页没有 Soomka Games 或 Steam App 链接，也没有版本 / build 信息。因此不能把它当第二个独立规则来源，更不能把其 2024-06 视频日期称为 Steam 发售或 Demo 日期。
- appinfo 的 2024-06 public branch build 与 Falcoware 视频只在时间上接近；没有 hash、版本号、开发声明或跨站链接建立同一 binary。研究不下载或运行 Falcoware 安装包 / torrent，也不购买或启动 Steam 客户端补规则。
- Steam 开发者目录还列出 Chilljong、Defect detector、Hunting grounds、Torico 系列等多款作品，但没有找到这些作品中的交叉公告、规则文档或 Monsters Auto Battler 补丁。

## 机制与项目问题的证据缺口

- **阵型 / 目标**：不知道可部署人数、格子形状、交换规则、近远程射程、仇恨、碰撞、移动、爆炸半径或召唤占位。
- **构筑 / 经济**：不知道 mercenary 名单、招募方式、价格、商店、升级、合成、装备、技能或 run 结构。
- **收益 owner**：除名称 Pyromancer 外，没有任何伤害 / 生存效果可归因到具体单位、装备或团队规则。
- **防御与转换**：没有 Shield、Armor、Health scaling、Defense-to-Attack、team-stat transfer、retaliation 或 max-Health damage 证据。
- **元素**：Pyromancer 名称不能证明 Fire affinity、Burn、reaction、resistance、cleanse 或元素体系；更不能支持 Ice Shield / Earth Shield。
- **敌人包**：爆炸、治疗、生成敌人只存在于产品文案，未找到具体关卡、反制、优先级、提示或失败报告。
- **生命周期**：没有 News、patch notes、开发者回复或版本化 guide，不能写任何 buff / nerf / rework / removal 记录。

## 检索日志与停止理由

访问日期统一为 2026-09-04。

### Steam 全量路线

- Store / App Details / appinfo / package / developer search 确认 App 3013370、Soomka Games、Windows-only、英文、付费、2024-12-21、无官网 / support URL、支持邮箱 `SoomkaGames@gmail.com`、没有 `demos` 字段，且当前 public manifest 时间为 2024-06-13。商店和 appinfo 只作身份 / 版本发现。
- Steam News API 返回 0；Community `allnews` 显示 `No more content`。
- General Discussions 页面初始化数据明确 `total_count: 0`。
- Guides 页面没有目标游戏玩家条目；唯一出现的 shared-file 链接 `181142704` 是 Steam 通用的“如何创建 Guide”说明，不是本游戏攻略。
- 全语言 Reviews API 在完整日期范围、`purchase_type=all` 下仅返回 recommendation `183429184`。作者发评时 / 累计游玩均为 12 分钟，正文只有 Pyromancer 喊话；它不满足详细评测或策略来源标准。
- Screenshots / Videos 页面没有玩家机制条目，只落到 Steam 通用 shared-file 链接。商店的五个 level achievements 与全局百分比没有规则正文、版本 / 分母或构筑解释，未登记。

### Falcoware、视频与外部路线

- `https://falcoware.com/MonstersAutoBattler.php` 返回 HTTP 200。清理导航后，目标正文与 Steam 完整产品介绍逐字一致；页面没有 Soomka Games、Steam App、版本、日期、补丁、技能或构筑，只提供 Falcoware 自有分发链接，判定为宣传 / 分发镜像。
- Falcoware 搜索入口被单独请求，但在当前环境多次超过 30 秒且没有返回可用结果；直接目标页已经可读，未尝试绕过或下载。
- YouTube 以精确标题、App id、开发者名、`gameplay / guide / strategy / build / Pyromancer` 组合检索。唯一目标视频是 Falcoware 的 `FUbtxeS7vWU`：2024-06-21、224 秒、无 caption track，description 复制商店文案。其余结果属于 Mage & Monsters、Mechabellum、Backpack Battles、Knightica 等不同游戏，全部排除。
- Google、Brave、Bing RSS 与 DuckDuckGo 精确检索标题、App id、开发者和支持邮箱。可读结果收敛到 Steam、Steam Community、Falcoware、RAWG 元数据 / 相似游戏页与大量同名噪音；没有独立玩法介绍、wiki、攻略、补丁、论坛构筑或统计分析。Brave 后续触发 429 / challenge，DuckDuckGo 返回 202 challenge，均未绕过。
- Reddit 精确 JSON 搜索返回 403；itch.io 搜索返回 1000+ 个按词拆分的宽泛结果，但解析到的项目标题没有一个精确匹配；Wayback CDX 在 20 秒内超时。三条路线均未绕过，也没有用搜索数量或片段补证据。
- SteamDB、MobyGames 与 PCGamingWiki 路线返回 403，未绕过；RAWG 只有产品元数据 / 相似游戏，不提供实质机制。GitHub 精确标题搜索只返回 `424C/rust-monster-battler`，是不同作者的 Rust 文本项目，已排除。
- 相近名称 `Monster Battles` App 3141720、`Auto Dungeon Monsters` App 1839630 以及 Mage & Monsters 系列均为不同产品，未混入本档案。

继续检索已只返回同一商店段落、单句评测、无字幕宣传视频、聚合元数据或无关同名游戏，没有新增 engine、state、payoff owner、生存、空间、经济、pivot、counter 或版本节点。按照 Adaptive Depth Protocol 在此停止，不用宣传文案或无字幕画面制造构筑。

## 对本项目的可迁移与不可迁移信息

可迁移：

- 该审计本身再次证明：商店写“站位重要”“敌人会爆炸 / 治疗 / 召唤”只能生成研究问题，不能直接成为机制卡或系统结论。
- 极短评论里的 `OP` / `meta` / `nerf` 必须拆成待验证线索；至少需要实际规则、完整构筑和版本 / 平衡来源后才能进入设计证据。
- 分发镜像与官方身份必须分开；相同标题和逐字文案不足以证明开发关系、授权或版本连续性。

不可迁移：

- 不采用 Pyromancer、mercenary、敌人类型、关卡数字或任何未核验技能 / 数值。
- 不从宣传截图 / 无字幕视频反推网格、目标算法、单位数量、装备或召唤规则。
- 不从 level achievement 百分比推断难度、留存、平衡或玩家规模。
- 不主张这款游戏证明盾体系、元素体系、团队防御转单核输出或任何具体 counter package。

## 未决问题

- mercenary 名单、Pyromancer 的正式规则、单位属性、技能、升级与招募经济。
- 竞技场格子、单位上限、站位时点、移动、射程、目标和碰撞规则。
- 爆炸 / 治疗 / 生成敌人的具体 owner、触发、范围、上限、提示和反制。
- level 5–50 的关卡结构、奖励、失败、重试和难度曲线。
- 2024-06 manifest、Falcoware 分发与 2024-12 Steam 正式版之间的真实版本关系。
- 是否有未公开索引的补丁、客户端内教程、开发者社区或发行后热修。
- 该评论中的 Pyromancer 是否为正式单位名，是否真的强势，以及任何 nerf 是否发生。

## Disposition

`insufficient-evidence`

这是已正式发行的可识别游戏，不是 `discovery-only`；但唯一实践文本只有 12 分钟玩家的一句 Pyromancer 喊话，官方公开面又没有规则、补丁、开发回复或可读指南。Falcoware 页面 / 视频复制商店宣传且缺少身份与版本说明，不能补足 rules + practical 门槛。故本档案不登记 deep source、不生成 evidence、不报告构筑、meta、平衡、nerf 或项目可采机制。
