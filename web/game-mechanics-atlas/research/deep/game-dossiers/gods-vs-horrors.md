# Gods vs Horrors

## 身份、版本与研究深度

- `title_id`: `gods-vs-horrors`
- 开发 / 发行：Oriol Cosp Games。
- Steam App：`2994240`；Demo App：`3079080`。
- Demo：2024-07-19 首次公开，2025-02-24 Next Fest 更新；正式版：2025-05-05。
- 当前可核实的最后一个公开 Steam 主版本节点：`1.1`（2025-06-19）。2026 年评测仍在讨论同一结构，但不能据此断言内部 build number 或没有未公告热修。
- 分类：long-tail；单机 drafting roguelite / deterministic autobattler。
- 研究深度：adaptive-depth `retained`。资料足以闭合三套具名构筑、阵营与跨阵营桥接、经济 / 阵型 / 敌人反制、遗物 / Blessing owner 及多个生命周期问题；没有维护型数据库、使用率统计或 1.1 后完整单位表，因此不升为 anchor。

必须分开 Demo、正式版 `1.0`、`1.1`、Casual、Ladder 与通关后 Infinity。标题中的 Gods 与 Horrors 不是两套可对称招募的阵营：玩家招募的 Gods 具有十种 mythology tag 与 Neutral 支援；Horrors 是按 campaign/Boss 组织的敌人包。公开资料没有全局 faith、corruption 或 sacrifice meter；死亡、召唤、牺牲只在具体 God、Blessing 或敌人规则中存在，不能从主题名称扩写成系统。

## 检索日志与停止理由

- 官方：10/10 Steam News 标题已扫；深读首次玩法公告、Next Fest Blessing 节点、正式版后 QoL/bugfix 与 `1.1`。正式发布公告只有庆祝性文字，未计为实质机制来源。
- Guide：官方 2025-06-05 公告链接的 `Strategy Guide` 全文 14 节已读。Guide 由开发者 `uri.32` 发布并说明与 Discord 社区共同整理，覆盖一般经济、十种 mythology、跨阵营构筑和七个敌人/Boss 包；公告本身只用于确认 provenance，不单独计数。
- Discussions：76/76 个公开标题已扫；深读十一帖，覆盖跨神系组合、Yoruba bench、poison/summon、Skorpigoth、Support owner、Ladder/Casual、平衡、触发顺序、Essence 溢出、Demo Egypt 与下一局 Pantheon/Relic 锁定。开发者回复与普通玩家观察分开标注。
- Reviews：API 汇总显示 278 条，当前 cursor 分页实际返回 275 条去重正文；按 mythology、poison、shield、relic、bench、pivot、ladder、infinity、sacrifice、order 等筛查，保留十篇会改变机制或失败理解的详细评测。未把三条未返回正文补成证据。
- 视频：六个中英日查询族得到 63 个去重候选，其中至少两个为同名噪声；抽查十二个高位候选，十个与本游戏相关。十二个页面均暴露 caption track，但 timedtext 均为空，因此标题、缩略图、画面数值与无字幕构筑不计证据。
- 外部 written route：开发者官网只重复产品简介；Bing 精确查询被 `vs`/同名词噪声污染；DuckDuckGo 返回 202 shell；Reddit JSON 403；Google browser route 不可用。均未绕过。未购买、下载、解包或从截图读取牌表。

最终注册 26 个实质来源：四个官方机制/版本节点、一个开发者发布且社区共创的 Strategy Guide、十一篇机制讨论、十篇详细评测。继续搜索会重复 Guide 的阵营说明或给出无字幕 run，不能补出 1.1 后全卡数据库、真实采用率、精确 pool odds 或 Infinity 数值上限，因此停止。

## 来源包

| 组别 | 数量 | 主要用途 | 关键限制 |
|---|---:|---|---|
| 官方玩法 / 补丁 | 4 | drafting/无战斗 RNG、Blessing、Casual/QoL、`1.1` 平衡与 Infinity | 不提供完整 current 数据表 |
| Strategy Guide | 1 | 三套闭合构筑、十神系、经济/阵型、七种敌人包 | 写于 `1.1` 前十五天；是推荐而非采用率 |
| Discussions | 11 | 开发者组合例、bench owner、Boss counter、顺序/溢出/模式失败 | 论坛样本不能代表总体 |
| Detailed reviews | 10 | 经济与 pivot 实践、平衡冲突、run shield、Infinity 与忙操作 | 跨 2025–2026，不提供内部版本号 |

## 基础循环、经济与真正的选择

每个 run 只开放随机五种 mythology，加上 Neutral；Ladder 的下一组 mythology 与 Relic 由结果推进，Casual 可借 Star Chart 调整。招募阶段用 Divine Essence 在 Pantheon 购买 Gods、升级 Pantheon tier 或 reroll。三张同名 God 合成升级，并从更高 tier 的奖励中 discover；Guide 因而建议先升 tier 再 triple，以提高奖励质量。玩家还要决定是否 freeze、dismiss/cycle、留 Essence 给 Mesopotamian reader、把单位留在 bench，还是投入即时 tempo。

经济不是单纯攒钱：前期要在三名低阶 Gods、一次 reroll、尽早 tier-up 与 run shield 容错之间权衡；中期围绕一个能持续增长的 core 决定是否 commit；后期根据开局可知的最终 Boss 激进寻找特定 counter。Guide 与高时长评测共同强调不要强行指定阵容，但玩家也报告高难时关键牌断供发生在已经无法 pivot 之后。随机性主要在 offer、可用神系、Relic/Blessing；战斗本身宣称无 RNG。

`acquired`、`recruited`、`dismissed`、triple/upgrade、combat start、attack、damage、death 与 end of recruitment 是不同事件。`1.1` 专门把部分旧文本从 recruited 改为 acquired，并让其在其他取得路径上触发，证明事件 lineage 会直接改变构筑。Relic 与 Blessing 可改 mythology、可用 pool、stats、触发次数、tier 或 Essence；它们不是所有 God 都自动读取的无主全队加成。

## mythology 是真实体系，不是单位主题

- **Aztec**：多 Aztec 互相增益；既可全阵营横铺，也可把 Tepeyollotl 做永久化单核，或让 Mictlantecuhtli/Itzpapalotl经营 summon bodies。
- **Celtic**：reroll 与 dismiss 是 engine；Blodeuwedd、Macha、Morrigan 支撑循环，免费/多身体/附加 stats 的 God 是燃料。
- **Chinese**：Spellcast 以效果替代普通攻击，taunt 与站位保证读条；Nezha 增加触发，Nian 可成为少人数 spellcast carry。
- **Egyptian**：death/summon 是核心；Anubis/Sekhmet读取死亡体，或用 Isis/Thoth/Ra 把死亡 buff 滚给终局单位，Osiris/Ptah负责重复触发。
- **Hindu**：招募 Hindu 触发增长，Vishnu 把自身 buff 扩到全 Hindu；Ganesha/Parvati等供给循环对象。
- **Japanese**：Spiritual Power 是阵营资源，供应者提高其他 Japanese ability；高阶核心与 SP 循环构成后期。`1.1` 因其“远强且最稳定”而削弱并移动多个 tier。
- **Mesopotamian**：未花 Essence 是共享状态，多个 Gods 读取同一余额；Nabu/Ninhursag提供经济/升阶，Marduk/Enki/Enlil/Anu将余额或回合结束放大。
- **Norse**：受伤后增益；后期主动用 Heimdall/Baldur/Loki伤害友军，Odin等读取 damage。`1.1` 让 Heimdall只伤 allies，明确为改善对 Cthulhu 的坏 matchup。
- **Olympian**：attack/kill 等 heroic quest 供给永久成长，Hermes/Fury/First Strike改变完成速度；Hera放大 buff，Zeus/Hades/Athena是后期 owner。
- **Yoruba**：army/bench 互相供给、从 bench summon、复制 bench stats；Olodumare、Obaluaye、Shango/Oya形成 owner chain，且天然适合外援。
- **Neutral**：大多是 bridge/support，不是默认可独立成型的第十一阵营；Guide仅给低 tier、Protector of the Weak 与 Resistance Amplifier少数例外。

因此“神系”确实是 gameplay tag/engine，但垂直与横向投入并存。Guide 给出大量跨神系桥接，玩家评测则指出真实 offer 下很多神系仍高度自锁、support pieces 随机且高 tier 核心到得太晚。两者不是互相取消：前者证明规则允许 horizontal build，后者质疑它的可达率和容错。

## 三套完整构筑

### Egyptian summon/death 对 Moiralith poison

- **engine**：Set/Hathor 等死亡或 summon owner 产生低 Health bodies；Sekhmet 与 Anubis 读取死亡并强化后续单位/攻击。
- **state/resource**：可召唤 body、死亡次数、Anubis attack、阵列顺序、Essence/tier、run shield 与 Boss 已知信息。
- **payoff**：大量 expendable summon 清掉前排 poison/reflect 小怪，Anubis/Sekhmet把死亡转成持续输出；终局仍需保留 owner 处理 Boss 清场后的大 summon。
- **survival**：小召唤承担 poison 与一换一，不把大 carry 暴露给即时 death；shield/taunt可补充，但不自动属于此 build。
- **space**：Set(s)、Hathor(s)、Sekhmet、Anubis依攻击/死亡顺序排布；大单位留后处理 Boss summon。
- **owner**：summoner拥有 body；死亡单位拥有 death event；Sekhmet/Anubis拥有读取与攻击增长；具体攻击者拥有最终伤害。
- **economy/pivot**：低阶 Egypt 可过渡并在高阶替换；若 poison 包已知，优先补 cleave/direct damage/First Strike 或跨神系 counter，而非只堆一个大数值单位。
- **counter/limit**：Azathoth 单体在战斗中增长，Guide明确 summon/Spellcast通常较差；Cthulhu七次受击阈值也会惩罚多段召唤链。构筑是 2025-06 Guide/实战闭环，不宣称 1.1 采用率。

### Nezha + Osiris + Ra/Ptah 的开战多重 death-trigger

- **engine**：Nezha 在 combat start 触发 Osiris；Osiris/Ptah重放或扩大 death effects；Ra/Thoth/Isis等供应 buff。
- **state/resource**：具名 Gods、重复 copies、death-effect顺序、taunt/首位位置、升级与高 tier discover。
- **payoff**：开战即多次触发 Egypt death buff，使全体 Egypt 或终局 carry 在正式互撞前获得滚雪球 stats。
- **survival**：taunt 与起始顺序保护重复触发链；不能把复制次数当无限或忽略 enemy start-of-combat damage。
- **space**：Guide/开发者建议把 Osiris 放首位并配置 taunt，让同一 death chain可能再次发生。
- **owner**：Nezha拥有开战触发；Osiris/Ptah拥有重放；Ra等拥有 buff；被 buff God拥有后续攻击。
- **economy/pivot**：先用 Egypt低阶 death pieces过渡，看到 Nezha才转跨阵营；多 copies提高收益但增加 bench/Essence机会成本。
- **counter/limit**：Cthulhu按 damage proc计数、Arachnomir backline/sniper 与直接伤害会拆触发者；QOL 曾修 Fury/First Strike与 Primordial Echo 顺序，说明顺序必须可见。

### Marduk + Tepeyollotl 的未花 Essence 永久化单核

- **engine**：Mesopotamian余额 reader Marduk按保留 Essence提供 combat buff；Aztec Tepeyollotl把自身 combat buff永久保留。
- **state/resource**：未花 Essence、Marduk倍率、Tepeyollotl永久 stats、回合数、Pantheon tier与当前 survival margin。
- **payoff**：牺牲当回合购买/升阶，把同一余额转成可跨回合复利的单 carry；开发者称装配足够早时可 solo 多个 Boss。
- **survival**：Tepeyollotl仍需活到读取/永久化完成；taunt、run shield或其他防护只买时间，不自动成为输出。
- **space**：保护 carry并按 cleave/First Strike/poison包放置，不能只看面板值。
- **owner**：Essence economy拥有余额；Marduk拥有临时 combat buff；Tepeyollotl拥有永久化；Tepeyollotl攻击拥有最终输出。
- **economy/pivot**：每回合比较花 Essence补强板面与保留余额复利；太晚拿到任一件应放弃 combo，转用已有核心。
- **counter/limit**：Moiralith poison/reflect、Hemithar削最高 Attack、Cthulhu proc cap与backline access都能越过纯大数值；Essence Preservation+Anu在 Infinity 出现过溢出，不能把无上限乘法当健康目标。

## 辅助切片：Norse、Japanese 与 Yoruba

Norse 是最接近“防御/受伤转输出”的真实链：友军伤害 supplier → damage event → Odin/其他 reader → team stats或攻击；Thor可针对单个大敌。它不是 Shield 天然转伤害，且 `1.1` 为 Cthulhu matchup定向改 Heimdall，说明 reader次数与敌人七次受击阈值必须一起算。Loki/Odin讨论还暴露了循环 stack 不透明：玩家无法确认小循环为何先把全队清空，开发者需要 composition/order 才能复现。

Japanese 的 SP 是元素/阵营外的独立资源轴，强度与一致性在 `1.1` 被官方确认过高。削弱包含 tier移动与 SP/attack范围收缩，说明“同阵营资源越多越强”需要供给、读取、tier窗口与 cap，而不是只给一个 tag breakpoint。

Yoruba 证明 bench 不是被动仓库。招募期 army+bench 可触发；战斗期只有 army 与带 Support 的 bench单位触发。Iroko可在 bench提供高基础 stats给 Olodumare而规避 combat penalty；Obaluaye从其下方 bench slot召唤。owner/phase/slot语义若不写清，玩家会把“behind”理解为战场左右相邻。

## Run Shield、Ethereal Shield 与防御输出边界

Run shield 是 campaign 容错/生命：Guide说 shield充足时可更贪地升 tier，开发者明确拒绝增加过多 shield以避免无脑 power-level。Ethereal Shield、Aegis Shield/Sacred Barrier相关显示则是具体战斗或遗物保护；QOL修过第三层 shield不显示。二者不能合成一个通用 `Shield` 数值。

防御在本作可能服务三种 payoff：保护 spellcast/death reader完成动作；作为 Chaostral direct-damage包的生存阈值；通过明确受伤 reader（Norse）转成 stats/hand/output。没有 reader时，Defense、healing、run shield只增加 uptime/容错。Ice Shield、Earth Shield和全队 bonus-Health/Defense转 carry仍是项目侧问题，不是 Gods vs Horrors 的既有规则。

## 阵型、确定性与失败说明

Guide建议一般按 Attack由高到低排，taunt放最左/最右避免 Cleave溅射，弱单位吸 First Strike，right-most God应针对 Skorpigoth死亡反击；Chinese Spellcast需要 taunt保证施法。敌人预览与开局已知 final Boss让 counter规划成为主决策。

“无战斗 RNG”不等于自动可解释。QOL修过 Primordial Echo攻击顺序、Fury+First Strike插入敌方攻击、Cthulhu counter reset与多个触发/显示 bug；Loki/Odin玩家明确要求显示 stack方向。开发者早期还选择不显示敌方精确数值，以避免纯算式最优。项目可迁移的是固定顺序与速度无关的 resolver、来源追踪和敌人机制预览；不应复制“隐藏必要状态”本身。

## 敌人包与反制

- **Shoggoth**：高 Attack+Cleave；用弱单位承担溅射，优先 Attack而非无意义堆 Defense。
- **Azathoth**：战斗内持续增长且单体；weakening与强者在前有效，summon/Spellcast通常吃亏。
- **Arachnomir**：death/summon蜘蛛链，低 Defense且关键件在后排；Cleave、First Strike、direct damage和right-side snipe有效。Skorpigoth按死亡伤害打 right-most，形成明确阵位 counter。
- **Moiralith**：poison前排加清场后的大 summon；小召唤、Ethereal Shield、First Strike、Cleave、direct damage分别回答即时死亡和多目标。
- **Chaostral**：大量 direct damage扫低 Defense后排；需要真正 Defense或用最小单位承受环境效果。
- **Cthulhu**：存活受伤会回复，敌人减 Attack；Boss受伤七次后清全队，要求少而重的 hit、战斗内增攻或缩小上阵人数。`1.1` Norse改动就是具名 matchup修复。
- **Unknown Calamity**：混合其他普通战，final Boss开局可知；要求横向 bridge与临场 pivot，而不是背一套固定终局。

这说明 Horrors 是 enemy-package/counter体系，不是“恐怖阵营羁绊”。玩家争议集中在 counter是否过硬：官方 Guide认为各 mythology都能借跨阵营解法，部分高时长评测认为 poison/reflect/右侧点名迫使特定远程或机制牌，导致非命中即输。没有胜率统计时只记录冲突，不判定哪方代表总体。

## Relic、Blessing、Infinity 与生命周期

Relic在 run开始重写 pool、mythology、economy或规则；Blessing在关键阶段按 tier提供 stats、复制、升阶、事件加倍、Essence保留等。QOL主动排除了 Star Chart首个强迫选神系，以及 Mortal Uprising与Divine Purge互斥组合；`1.1`新增八 Relics并调整 Blessing tier/数值。这证明规则重写必须带 eligibility与冲突过滤，不能让随机池出现逻辑死项。

Infinity在 `1.1`正式加入，通关后持续提高难度直到失败。社区随后记录 Essence Preservation+多个Anu使余额每回合倍增并溢出负数、end-of-recruitment复制把 Essence推到六十万，以及另一套 Anu+Yoruba+Shield召唤线把 stats推到显示上限后回绕。另有评测认为 Infinity难度在两三轮内陡升，build尚未展开即结束。公开 patch没有后续修复节点，所以只能标为 1.1 后社区观察，不能给当前 cap或宣称 bug仍存在。

## 生命周期与负案例

本 checkpoint 计十四个 materially distinct negative/reworked families：

1. Demo五/六神系与 Blessing增量，不能与正式十神系直接合并。
2. Ladder丢级惩罚实验，开发者后拆出 Casual并按敌人记忆难度。
3. `1.1`确认 Japanese远强且最稳定，定向削弱并扶持弱神系。
4. `recruited`→`acquired`事件语义修正，扩大合法触发来源。
5. Star Chart首选与 Divine Purge/Mortal Uprising等无效/强迫组合被排除。
6. run shield容错与 power-level贪心冲突；开发者拒绝无限加盾。
7. Primordial Echo、Fury+First Strike攻击顺序修复。
8. Anu/Tezcatlipoca与 Relic/Blessing未接线修复。
9. Sacrificial Shade偶发丢失转移对象/升级失效，玩家提供重复条件，官方此前已有一次修复。
10. bench/Support phase owner不清，开发者明确招募期与战斗期规则。
11. Cthulhu counter未 reset及缺动画修复。
12. Skorpigoth/poison/reflect被报告为突然的硬 counter，敌人预览成为必要补偿。
13. Loki/Odin反馈循环缺 resolver/stack方向解释，难区分合法 start-of-combat伤害与顺序 bug。
14. Infinity难度墙与 Essence/stat整数溢出是两端失控的 1.1 后社区观察；尚无后续公开修复。

一般价格、内容量、翻译、显示分辨率和小型成就问题未为凑数拆分。Cumulative explicit negative/reworked cases：220。

## 对本项目可迁移

- 阵营不是标签本身，而是 supplier/resource/reader/payoff链；同一标签可有垂直核心、横向 bridge与 Neutral替代。
- 体系应为至少两种敌人包预留可得 counter。预览 Boss不够，还要让关键 counter在 commit窗口前出现。
- board与bench必须有 phase owner；Support、召唤来源、被召单位的 stats/tags/proc权分别声明。
- 事件词要区分 obtain/recruit/dismiss/upgrade/death/summon，避免同义文本悄悄改变 proc范围。
- 防御只在具名 reader存在时转输出。Norse受伤链比“Shield自动反伤”更清楚，因为 supplier、damage event、reader与最终攻击可分别归因。
- Relic/Blessing规则重写要有互斥过滤、slot/offer成本、cap和循环 guard；Infinity必须有数值上限与overflow策略。
- 确定性需要 resolver顺序、速度独立、触发栈与战报，不只是删随机数。
- 敌人包可以直接攻击 build link：Cleave打阵位、poison打单核、direct damage打低防后排、七次阈值打多段proc、战斗增长打慢速召唤。

## 不兼容与未决

- 不复制隐藏敌方必要数值、只有一两次失败容错、掉 Ladder level或拖拽循环忙操作。
- Guide写于 2025-06-04；`1.1`在 2025-06-19 调整若干 God/Relic/Blessing。三套主 build的关键件未在公开列表中被直接改写，但仍不能称 current meta。
- 没有维护型 wiki/database、逐 God current文本、pool odds、Relic/Blessing完整表、使用率/胜率、内部触发顺序或 Infinity cap。
- `Sacrificial Shade`只证明一个具体牺牲/转移效果及 bug，不证明存在全局 sacrifice体系；未发现 faith/corruption meter。
- Gods的 mythology是玩家阵营轴；Horrors是敌人包，不能做对称 trait breakpoint比较。
- Ice/Earth Shield与全队防御转 carry仍待项目设计讨论；本档案不授权任何具体遗物、公式或首版内容。

## Disposition

`retained`。

保留理由：26 个实质来源足以证明十种 mythology拥有不同 engine/resource/reader，且官方/社区 Guide与开发者讨论共同闭合 Egyptian summon、Nezha-Egypt death chain、Marduk-Tepeyollotl Essence conversion三套构筑；经济、阵型、bench owner、Relic/Blessing、七个敌人包和十四个 lifecycle family也能相互核验。

停止理由：最后公开主节点只有 `1.1`，缺少维护型数据表和统计；大量 2026 评测只反映体验延续，不能证明精确 current数值。继续抓无字幕 runs或重复评论不会改变 owner/economy/space/counter/lifecycle理解，因此保持 long-tail retained，不伪造 current tier list。
