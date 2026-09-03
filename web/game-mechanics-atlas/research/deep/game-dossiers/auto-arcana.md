# Auto-Arcana

## 身份与时期

- `title_id`: `auto-arcana`
- Steam 主 App：`4281360`；公开但尚未发行的 Demo App：`4695800`。
- 开发 / 发行：Broken Build Studio；工作室称由 Eric 与 Dani 两人组成，2025-11 成立于美国马里兰州巴尔的摩，本作为首作。
- 主 App 状态：截至 2026-09-03 为 `Coming soon`，Steam 仅显示 2027；没有公开 Playtest、公告、评测或社区实战。
- Demo 状态：Steam 显示计划于 2026-09-22 发布；在本次访问日仍为 `Coming soon`，因此不能把 Demo 的产品描述当成已经可玩的规则。
- 类型定位：单人 Strategy / Roguelike / Autobattler / Deckbuilding；官方 Google Factsheet 另写 Unity 2D。
- 证据结论：六槽顺序施法、元素状态、法术融合、runestone、确定性战斗与按敌人时点转型都高度相关，但只有官方商店和 press-kit 产品材料，没有独立实践源，故为 `discovery-only`。

## 自适应深度判断

- **系统复杂度**：公开文本同时涉及六槽容量、从左到右的施法序列、冷却时点、单元素专精、跨元素状态、法术融合、runestone 修改、draft / craft / loot 获取和敌人脚本反制，潜在复杂度高。
- **机制独特性**：把“技能栏顺序＋冷却”变成构筑空间，并以已知敌方回合脚本要求临战改序，是比普通数值羁绊更独特的结构。
- **项目相关性**：状态引擎、元素交叉、Shield / Feedback 防守应答、规则修改所有权、确定性结算、敌人 telegraph 与 pivot 窗口都直接对应本项目的研究问题。
- **资料密度**：官方产品材料密度中等，实践密度为零。主 App 与 Demo 均未发行；没有规则手册、开发日志、patch、玩家 guide、build、讨论、评测或可读字幕实战。
- **版本变化**：当前 Steam 与 Google Blurb 写 Demo 有 10,000+ 可制作法术，2026-09-02 更新的 Impress press-kit 页面写 15,000+；这只能证明营销文本发生差异，不能证明 Demo 内容增长或某个 playable build 的版本变化。
- **选择的深度**：对独特机制做比普通未发行标题更细的所有权 / 时序 / 反制缺口拆解，并穷尽公开 press-kit、Demo、开发者和外部实践路线。
- **停止原因**：第二种独立实践来源仍不存在。继续重复精确标题、法术名和平台限定检索只返回官方页面、同词游戏或无关内容，不再改变机制理解；按 diminishing-return test 停止，不把三段官方示例扩写成完整 build。

## 版本与发行地图

- Steam 主 App 4281360：2027，`Coming soon`；App Details 没有列出 `demos`。
- Steam Demo App 4695800：可由 Steam Store Search 独立发现，显示 2026-09-22，访问日尚未发行。主 App 没有关联 Demo 字段，不据此推断后续关联方式。
- Impress machine factsheet 使用 `2027-06-30` 且标注 `dateType: quarter`；Google Factsheet 写 `2027 (window unannounced)`，Steam 只写 2027。因此 6 月 30 日不是精确发布日期，本档案也不把 Q2 当成已确认窗口。
- Impress 游戏 press kit 的最近更新时间为 2026-09-02；它说明当前媒体文本状态，不是 playable version、patch 或平衡版本。
- 当前 Steam / Google Blurb 的 10,000+ 与 Impress 页面 15,000+ 彼此冲突。Demo 尚未公开，无法判断是宣传修订、统计口径、目标内容还是实际内容变化。

## 检索日志

访问日期统一为 2026-09-03。

### Steam 官方与社区

- 英文主 App Details、商店正文、开发者 / 发行商页均已读取。它们确认 2027、单人、六槽、顺序施法、元素专精 / 混合、fusion、runestone 和确定性战斗，但按门槛只作 identity / discovery，不计 deep source。
- Demo App 4695800 的 App Details 显示 2026-09-22 且仍 `Coming soon`；主 App App Details 未列 Demo。没有尝试通过 depot、清单或非公开分支获取未发行内容。
- 主 App 与 Demo 的 Steam News API 都是零条。主 App Guides、Discussions、Reviews、Community Videos / Images / News 没有玩家规则或实战正文；Demo 尚未发行，评测接口也没有可用样本。
- 商店有一个 `Auto-Arcana Gameplay Trailer`。没有字幕、转录或附带规则说明；不从画面推断法术数值、槽位重触发、目标、冷却结算或敌人行动顺序。
- Steam developer 页只有本作，没有第二款作品或可读开发日志可交叉定位团队机制惯例。

### 官网、Impress 与 Google Drive press kit

- `brokenbuildstudio.com` 及其任意常见子路径均重定向到 Impress 的 Auto-Arcana press kit；没有独立博客、sitemap、patch、devlog 或 manual。
- Impress 公开 API 与页面提供工作室身份、2027 计划、产品描述、八张截图、一个 Steam trailer 和公开 Discord。它是官方 press / product material，不满足独立实践功能。
- Google Drive 的公开 `Auto-Arcana Presskit` 根目录列出 Background Art、Broken Build Studio Info/Logos、Character Art、Concept Art、Factsheet/Descriptions、Gifs、In-Game Sprite Art、Key Art、Logos、Screenshots 与 Video。
- `Factsheet/Descriptions` 中两份公开 Google Docs 可以正常导出文本：`Auto-Arcana Factsheet` 只给身份、平台、引擎、类型和 2027 未定窗口；`Auto-Arcana Blurb/Descriptions` 重复商店机制示例与 10,000+ 法术说法。它们没有回合规则表、法术数据、玩家 run、构筑路线或反制结果。
- 没有下载 14–15 MB 图片包，也没有用概念图、截图、GIF、sprite 或无字幕视频补足规则 / 实践证据。
- Discord 邀请可公开看到，但属于私域入口；没有加入、登录、索取测试资格或把服务器存在当成公开 playtest 证据。

### 外部规则与实践路线

- 精确检索 `Auto-Arcana`、`Auto Arcana Broken Build Studio`、App / Demo ID，以及 `gameplay`、`demo`、`playtest`、`guide`、`build`、`strategy`、`Freeze`、`Shatter`、`Feedback`、`runestone`、`spell fusion`、`Discord` 和元素 / 冷却组合。
- Bing RSS 对精确标题持续返回日本汽车商家等错误分词噪声；Google 在限流前没有提取到本作外链，后续返回 429；DuckDuckGo 无结果，Brave 已限流。搜索摘要不计来源。
- YouTube 精确标题只出现 2020 年的 `Auto Arcana with 9 Units`，早于工作室成立且属于无关内容；带 Broken Build / BrokenBuildDev 的组合没有本作视频。Steam trailer 不因标题含 gameplay 而升级为实战源。
- itch.io 精确标题与工作室搜索没有本作；`brokenbuildstudio.itch.io` 与 `brokenbuilddev.itch.io` 均为 404。
- GitHub repository 搜索对工作室、App ID 和 Demo ID均为零。三个名为 `Auto-Arcana-Wars` 的 2024 项目早于本工作室成立、作者不同且内容无关联，已排除。
- Reddit 公共 JSON 返回 403；old.reddit 路径只显示登录页，其他公开搜索路线没有可归属帖子。没有绕过访问限制。
- Internet Archive 对官网、主 App 和 Demo App 的 CDX 请求均超时；没有把不可读快照或搜索缓存当作历史规则。

## 官方产品材料中的机制线索

以下都是待未来 Demo / 实战验证的 discovery leads，不是深证据：

- 玩家只有六个 skillbar slots，法术从左到右触发，并可能影响后续法术。
- 单元素专精会强化最强法术；混合元素可访问 cross-element effects，但没有元素表、阈值、抗性或状态优先级。
- 法术可通过 draft、craft、loot 获得；不同法术可 fusion 成自定义法术，再由 runestone 做 transformative modification。
- 战斗声称没有随机性；胜负取决于 spell rotation 与根据敌人调整构筑。该句没有给出 draft、loot、敌人选择或数值生成是否随机的边界。
- 示例一：先 Freeze，再用 Shatter 造成大量伤害。
- 示例二：先叠 Burning 与 Frozen Solid，再使用放大 debuff 的 curse。
- 示例三：面对第三回合多次攻击的 Elite，调整 Shielding spell 的 cooldown 使其在 barrage 前触发，或制作 Feedback spell 反射伤害。
- 终局承诺还包括永久冻结、无伤迫使敌人逃跑，以及连锁 1-cooldown lightning bolts 在敌方首次行动前结束战斗。

这些例子最多证明开发者想让顺序、状态、冷却和敌方时点成为设计轴；它们没有公布具体 spell body、fusion 配方、runestone、取得成本、失败样本或实际 counter。

## 无法闭合的构筑语法

### Freeze → Shatter 线索

- **engine**：官方只说 Ice spell 先 Freeze；不知道命中、层数、持续、抵抗、重复施加或冷却。
- **state/resource**：Frozen / Frozen Solid 是否同一状态、升级状态或不同来源未知。
- **payoff**：Shatter 是消费冻结、读取冻结、只需目标被冻结，还是按层数放大未知。
- **survival**：没有防守法术、回复、护盾或控场时长与这条线的关系。
- **spatial condition**：只有 skillbar 左右顺序；没有棋盘、射程、目标、lane 或单位空间。
- **payoff owner**：不知道收益属于 Shatter、融合后的法术、runestone、slot 还是 Arcanist。
- **economy / pivot / counter**：不知道何时能取得组件、融合能否回退、敌人是否有免疫 / cleanse / resist，也没有 Shatter 不来时的过渡线。

### Burning + Frozen Solid → Curse 线索

- 官方例子证明跨元素可形成前态，再由第三个法术读取，但不知道 Burning 与 Frozen Solid 是并存、反应、互斥还是只作 curse 标签。
- 没有 stack cap、DOT tick、控制持续、触发顺序、消耗语义、伤害归属或 cleanse 规则。
- 因此不能据此设计“火冰诅咒流”的单位、装备、遗物或数值，也不能判断垂直单元素与水平混元素的真实机会成本。

### Shield timing / Feedback 线索

- Shielding 例子是“把冷却对齐第三回合 barrage”的生存应答；Feedback 是另一个反射伤害的候选应答。官方没有说二者可共存、融合或互相转换。
- 这说明 Shield 系统与 element 系统可以是正交轴：Shielding 处理伤害时点，元素处理状态 / 反应；但没有证据说明 Shield 具有冰、土或其他 affinity，也没有 team-stat-to-carry 转换。
- Feedback 只能作为“防守事件可有独立输出 payoff”的发现线索。它不支持最大生命 5% 伤害、防御转法强或全队生命转射手攻击等本项目候选公式。
- barrage 的攻击次数、伤害类型、Shield 吸收 / 过期 / 破盾、Feedback 按吸收量还是原伤害计算、重入与反射循环保护均未知。

## 顺序、所有权、经济与反制缺口

- 六槽是固定顺序、循环队列、各自独立 cooldown 还是轮到时跳过未就绪法术未知。
- 左到右只说明初始方向；不知道多回合回到槽一、同冷却并发、融合后的多段触发、插入 / 延迟 / 重置或 1-cooldown 连锁如何排序。
- “无 combat RNG”不等于整局确定性。draft / loot / spell generation、敌人选择、数值范围和 tie-break 均未说明。
- fusion 不知道是两法术合并文本、继承 tag / cooldown / 数值、占一槽还是保持多个来源；也不知道拆分、覆盖或 replacement cost。
- runestone 不知道属于基础法术、融合结果、skillbar slot、Arcanist 还是本局；移动、叠加、互斥、销毁与替换规则均未公布。
- draft / craft / loot 的货币、候选数、刷新、锁定、稀有度、背包 / spellbook 容量和机会成本未知。
- 没有具名普通敌人、Elite 除第三回合 barrage 外的完整脚本、Boss、抗性、cleanse、反射免疫或对玩家的反构筑。
- 没有失败报告、时间线预览、来源归因、下一次触发预览或“为什么第几槽没发动”的 UI 证据。
- 单一 Arcanist 与法术栏是本作公开载体；没有英雄编队、人口、装备持有者、单位空间或战中两条独立战术指令，不能直接替代本项目的 roster / formation 结构。

## 对本项目的研究价值与限制

可保留为未来设计讨论的发现线索：

- 将有限技能槽当作真实机会成本，让 engine、survival 与 payoff 争夺相同容量。
- 将一维顺序视作空间条件：前槽制造状态，后槽读取 / 消费；敌人时点迫使玩家改 cooldown 或替换应答。
- 将 Shield、防御反射、元素状态和收益转换拆成可正交组合的系统，而不是把“冰盾”强制等同于一套完整流派。
- fusion 与 runestone 若分别拥有组合和规则改写，应显示原始来源、最终 owner、cooldown、状态消费与替换代价。
- 确定性战斗只有在敌方行动表、己方触发序列、失败原因和调整窗口可读时才构成策略承诺。

当前不能作为设计依据：六槽是否适合本项目、任一元素 / 状态 / 法术 / runestone 清单、具体 cooldown、法术数量、融合公式、反射公式、经济、Boss、确定性实现或平衡结果。尤其不能从营销中的“break the game”推导出无限递归应被允许。

## 复查条件

- 最早在 2026-09-22 之后重新检查 Demo App 4695800；只有它实际变为可下载 / 已发行，才进入新一轮规则与实践审计。
- 复查时先确认主 App 是否正式关联 Demo、Demo build / 公告 /版本号、Steam Guides / Discussions / Reviews 与独立长实战是否出现。
- 保留门槛不变：至少一条非商店规则 / 机制源和一条独立实践 / build / strategy 源；官方 Blurb、Factsheet、截图、GIF、trailer 与媒体报道转述不能共同冒充玩家实践。
- 优先验证六槽循环与 cooldown 结算、fusion / runestone owner、Freeze / Shatter 和 Burning / Frozen Solid 的状态语义、Shield / Feedback 的伤害归属、Elite telegraph、资源 / 替换成本与失败报告。
- 若 2026-09-22 到期但 Demo 延期、不可下载或仍没有第二实践源，继续保持 `discovery-only`，只更新身份 / 版本路线，不因日期经过而自动保留。

## 未决问题

- Demo 是否如期公开，主 App 与 Demo 的关联和版本标记是什么。
- 六槽的循环、跳过、并发、cooldown、fusion、多段、反射和 1-cooldown 连锁顺序。
- 元素列表、单元素强化、cross-element effects、状态叠加 / 消费 / cleanse / resist 的完整规则。
- draft / craft / loot / fusion / runestone 的资源、容量、所有权、替换和机会成本。
- 至少一套玩家实际完成的版本化 build：包括取得路线、六槽顺序、survival、payoff owner、敌人 counter 与失败解释。
- 10,000+ / 15,000+ 法术差异的口径，以及生成组合是否代表实质不同的玩法选择。

## Disposition

`discovery-only`

本作比一般未发行候选更有研究价值，但高价值不等于高置信。官方材料只建立了六槽顺序、元素 / 状态、法术融合、runestone、确定性战斗和 Elite 时点反制的产品意图；主 App 与 Demo 均未发行，第二实践源不存在。按自适应深度与 diminishing-return test，本次不登记 deep source、不生成 deep evidence，也不把 Freeze—Shatter、Burning—Frozen Solid—Curse 或 Shielding / Feedback 示例升级为已验证构筑。下一次有意义的恢复点是 Demo 实际公开且出现可读规则与独立实践材料之后。
