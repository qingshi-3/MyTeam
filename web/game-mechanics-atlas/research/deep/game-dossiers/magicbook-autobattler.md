# Magicbook AutoBattler: Contract

## 身份、时期与资料密度

- `title_id`: `magicbook-autobattler`
- Steam App：`3521320`；开发 / 发行：JXGS；2025-03-28 正式发行。独立 Demo App `3535780` 于 2025-02-27 发布。
- 身份链：旧版公开名称为 `Magicbook AutoBattler` / `Spellbook Auto Chess`。开发者在 2025-03-29 为新 App 提供旧档自动 / 手动迁移；玩家与开发者回复把重发归因于原发行方问题。旧 App id 未从可访问的一手材料确认，故不猜测。
- 可见版本链没有语义化版本号，以补丁日期分期：2 月 Demo / 预发行修复；3 月 28–29 日 Contract 重发；4 月 25 日状态 / Contract / 宝物大改；5 月 18 日 Treasure Synergy 2.0 与成长上限重做；6 月 27 日一键合成、Workshop、排行榜和新武器 / 套装；最后一条公开补丁为 2025-07-08。
- 本 checkpoint：19 个实质非商店来源——10 个官方公告 / 补丁，1 个完整 Steam Guide，2 个完整 Steam 构筑讨论，6 个详细 Steam 评测。
- 置信：高。完整 Guide 与两个讨论提供三套不同阶段构筑，官方补丁能复核状态、成长、Workshop 与失败修复；但没有维护型 wiki / 数据库 / 统计、当前完整装备表和精确站位规则，因此是 `retained` 而非 anchor。

## Adaptive-depth 决定

这款游戏的公开表面远超普通独立长尾：18 条官方公告、264 条 Steam 评测、87 个公开讨论、1 份可完整读取的 300–405 层攻略、2 个 Workshop 样例和大量公开视频。其系统同时覆盖八人队、武器定职、装备供 Contract 点、角色被动、共享三选面板、Magicbook 升级、事件取件、Blessing、Treasure Synergy 能量、状态传播、护盾转输出、后期敌人考试和局外天赋，复杂度与项目相关性都高，适合深挖。

本轮完整检查 18 条公告、264 条评测、87 个讨论、Guides / Workshop、英文 / 中日文 YouTube 关键词、Reddit、GitHub、Bilibili、itch.io、PCGamingWiki、SteamDB 与普通搜索路线。YouTube 找到大量攻略 / 盾军团 / 法师 / 无尽视频，但抽查的自动字幕 timedtext 均返回空，其他目标无字幕；Bilibili 要求验证码，Reddit / PCGamingWiki / SteamDB 阻断，GitHub 与 itch.io 无相关结果。已有文字材料能闭合构筑、反制和生命周期，继续解析无字幕同类视频不再改变设计理解，故在 19 个功能不同的来源处停止。

## 来源包

- `src-mba-official-release-contract-2025-03-29`：Contract 正式发行内容；事件、宝物联动、角色经验 / 被动、挑战无尽、传奇套装与内容轴。
- `src-mba-official-legacy-migration-2025-03-29`：旧 `Spellbook Auto Chess` 存档迁移到 Contract 的官方流程。
- `src-mba-official-passive-fixes-2025-03-06`：被动提前生效、购买前后变化、升级属性未应用、跨存档残留等修复。
- `src-mba-official-transaction-fixes-2025-03-31`：高层事件不触发、重铸拖拽丢装备、Steam Synergy 条件、旧档迁移等。
- `src-mba-official-status-contract-rework-2025-04-25`：Burn / Poison / Weakness / Armor Melt / Frost / Ice Energy、Rogue Contract、宝物联动、武器重选、战中详情与 Endless Boss。
- `src-mba-official-report-fixes-2025-04-27`：属性与承伤结算显示、宝物事件、Boss Challenge 文本修复。
- `src-mba-official-synergy-20-2025-05-18`：Treasure Synergy 2.0 能量、角色 / 红装无限成长、红+1 商店、标准 Endless 每 90 层换武器。
- `src-mba-official-workshop-leaderboard-2025-06-27`：一键合成、Workshop、榜单隔离、新武器 / Boss 套装、阶段经验与防御溢出修复。
- `src-mba-official-passive-fix-2025-07-04`：角色被动描述 / 实际效果、Ranger Contract 额外触发与 Endless Boss 入口修复。
- `src-mba-official-destiny-fix-2025-07-08`：Destiny Start 初始特质失效与天赋文本修复。
- `src-mba-guide-endless-300`：完整 2025-03-30 / 04-23 Guide；300–405 层 Freeze + Bleed 玻璃炮、Book 4 路线、事件、Contract、宝物和 Blessing。
- `src-mba-thread-endless-setups`：多人持续更新的 Endless 构筑讨论；200+ 双法双战、162 层 Ice AOE、盾叠层、治疗失效、敌方 Ranger / 反伤 / 群怪考试。
- `src-mba-thread-challenge-200`：2025-03-31 Challenge Endless 200 层火墙 + 回能 + 双倍 + 电击枪控制、禁用件、事件取件与 Horus 免死。
- `src-mba-review-system-ownership-2025-03-30`：八人 / 六后备、六装备位、武器 / 防具 / Contract、三合 / 重铸、共享面板与手动经验。
- `src-mba-review-contract-economy-2025-03-29`：起始天赋、敌人风险收益、职业专属 / 全队 Contract 分支、Book 升级和局外点。
- `src-mba-review-readability-2025-04-05`：120 小时英文长评；无盾条 / 状态图标 / 来源、术语混乱、Codex 缺规则与多段攻击歧义。
- `src-mba-review-role-convergence-2025-03-31`：40 小时长评；共享面板经济、套装事件、近战吸血 / 盾替代 Guard、站位弱化、刷新与库存操作压力。
- `src-mba-review-endless-cap-2025-04-02`：旧上限时期 165 层；装备约 120 层封顶而怪物持续增长、反复合成 / 换装 / 刷新。
- `src-mba-review-late-convergence-2026-02-16`：补丁后 32 小时长评；重伤克制治疗、后期角色 / 装备成本与敌人倍率、路线收敛和爆发 / 消耗问题。

## 真实循环与系统所有权

一局由共享三选面板推进：同一个刷新池会出现冒险者、装备和下一场敌人 / 关卡。玩家可消耗金币反复刷新、右键锁定暂时买不起的项，也能选择收益更高但规则更危险的敌人。战斗自动执行；胜利与事件提供金币、材料、经验、装备、Blessing 或 Treasure，固定阶段进入 Elite / Boss。

核心不是经典棋子的三合升星，而是四层相互独立的成长：

1. **角色**：最多八人上场、六人后备。角色不三合，使用全局经验池手动升级，并在固定等级解锁个人被动；2025-05-18 后取消等级上限。
2. **武器 / 职业**：每人一件武器，武器决定职业倾向、普攻目标与技能。换武器等于换职业 / 技能包，不等于换角色被动。
3. **装备 / Contract**：每人另有防具、肩甲、项链和两个饰品。防具 + 肩甲组成套装并提供职业 Contract 点；同名同品质三件合成上一品质，同品质不同装备三件可随机重铸。Contract 既可强化本职业，也可用较低倍率广播全队。
4. **宝物 / Blessing / Treasure Synergy**：Boss 与事件提供独立规则层。5 月 18 日后 Treasure Synergy 使用 20 初始能量，Magicbook 升级可扩到 175，玩家在能量预算内自由组合；它不是 Contract 的另一个名字。

Magicbook 是本局的供给层：升级提高可刷出的角色 / 装备品质，增加 Grimoire 生存与 Blessing，并在后期扩 Treasure Synergy 能量。Guide 的核心节奏是只用四至五人过渡、尽快升 Book 4，再用带星 elite 替换早期占位者；另一个长评把 Book 升级写成解锁更高品质和更强被动角色的关键。角色、装备、敌人共用面板，因此一次刷新同时重掷三个不同需求，早期“想要的都出现但买不起”是实际经济压力。

## 构筑一：2025-04-23 Freeze + Bleed 玻璃炮（300–405 层）

这份 Guide 明确标注基于 2025-04-23 补丁，早于 4 月 25 日状态大改和 5 月 18 日 Synergy 2.0，必须作为历史构筑读取。

- **阵容 / 武器**：1 把 Necrotic Shield；4 个输出位，理想为 3 把 Feathered Soul Bow 加 1 把 Blood Demon Scythe 或 Moon Blade；3 个辅助位用 Blood Barrier、Steamer Basket、Zither，或 Bleeding Dagger + Scythe + Barrier。
- **engine**：Feathered Soul Bow 本身没有技能，配“自我 Silence、+100% 攻速”的饰品绕开施法损失，以多箭普攻触发 Freeze；Bleed、Deep Chill / Ice Blast 和 Ice Stone 补控制与异常。Contract 树优先 Ranger、近战和 Priest，提高伤害与覆盖。
- **state/resource**：Book 等级、elite 角色、武器品质、Contract 点、Freeze / Bleed、攻速、事件出现、宝物联动和 Blessing。
- **payoff**：三名 Bow carry 用高攻速、多箭与 Freeze 控制群体；Scythe 对前排和 Bleed 目标补百分比异常伤害。Guide 把十至十五秒结束战斗作为在线标志。
- **survival**：Necrotic Shield 放在高生命 / Tenacity 角色上，在其最大品质描述中让队友六秒内单次不承受超过自身生命 6% 的伤害；Steamer Basket 治疗，开场 Boss Blessing 控制七秒。
- **spatial condition**：Shield owner 是队伍的承伤 / 保护位；Bow 面向后期大量敌人，多目标优先于 Scythe 的少量前排；Guide 没给文字化精确格位，因此不从截图猜方格。
- **payoff owner**：三个 Bow 持有者分别拥有普攻 / Freeze 输出；Shield owner 拥有防护窗口；support 持有者拥有团队增益；Contract / Treasure 提供广播，不把全部伤害归给“队伍羁绊”。
- **economy / pivot**：开局只买一两名过渡角色，优先 Book 4；elite 出现后替换。Requiem Knight 套装不进普通商店，要通过事件取得首件，再由 Blacksmith Karl 复制或补齐；Mighty Hunter 可用两件换两件，事件因此是构筑必需的供给通道。
- **counter**：Guide 建议避开物理抗性 / 降伤敌人，并在后期 35 敌人场面考虑把 Moon Blade 换成第四把 Bow。该 build 依赖 4 月版本的 Contract 与 Treasure，不能直接声称在 5 月以后仍同强度。

## 构筑二：盾叠层 + 双法双战的 160–200+ 层适应线

多人讨论给出更完整的失败—调整链，而不是一次成功截图：

- **基础队形**：Guardian 装 Necrotic Shield + Purifying Shield；两名 Warrior 装 Dragon Soul Battle-Axe 与吸血项链；两名 Mage 装 Fire Dragon Staff 与开场施法项链；Thief 用 Curse Dagger；Priest 用 Cross Staff、盾项链和多目标 Treasure。另一版是 Guardian / Warrior / Thief / Ranger / 2 Mage / 2 Priest 的 Ice AOE。
- **engine**：双法的开场施法 + double-cast / energy refresh 快速打掉群体，双战靠吸血与攻速继续输出；Poison Thief 是前中期经济 / 状态桥，后期可换额外 Mage 或 cleanse owner。
- **state/resource**：开场施法次数、能量回复、Ice / Poison、盾层、吸血、cleanse、Book 15 左右的套装供给和事件套装。
- **payoff**：Mage 是前几秒 AOE owner，Warrior 是持续与残局 owner；不是全队平均输出。
- **survival**：Frog Boss Blessing 提供等于 100% Health 的开场盾，与 Priest 初始盾、盾项链和“有盾减伤”宝物叠加。报告称 HoT Priest 即使有多目标宝物、单战八百万治疗也无法阻止 Ranger 秒后排，盾是更有效的爆发答案。
- **spatial condition**：敌方 Ranger 会绕过通常前排压力击杀 healer；Guard 必须 Taunt / Cleanse / 限制单次伤害，后排不能只依赖治疗。玩家在 160–165 层遇到 mass-Ranger wall 后调整。
- **payoff owner**：Mage / Warrior 分别拥有爆发与续航，Guardian 拥有净化和保护，Priest 拥有开场盾 / 恢复，Frog Blessing 是战斗开始的团队来源。
- **pivot**：Thief 早期以经济和 Poison 占位，后期若 Curse / Poison 价值不足则换第三 Mage 或 cleanse / healer；装备套装从 T1 经事件换到 T2 / T3，再由 Blacksmith 补第二件。
- **counter / failure**：perma-freeze healer、mass Ranger、Skeleton Shield 反伤、35–40 敌人群和 Poison 免疫缺失分别考试净化、后排生存、停手 / 非反射伤害、多目标吞吐与异常防护。讨论最终总结“爆发直到被爆发”是 Endless 的收敛问题，而不是单一数值不足。

## 构筑三：历史 Challenge 200 层火墙永控

2025-03-31 Challenge Endless 讨论记录一套早期 Contract 版本：Fire Wall + 双回能 + double cast + Electric Gun，以 Blessing 或 Ghost Claw Shield 聚怪，Electric Gun 连续控制；Treasure Bag 与 Red Envelope 负责经济。若敌方取得间歇 control immunity，永控会被打断。

- **engine**：聚怪把分散敌人变成 Fire Wall 的统一目标，双回能 / double cast 提高技能节拍，Electric Gun 锁住行动。
- **state/resource**：能量、施法次数、控制持续、敌方免控窗口、火墙覆盖、经济宝物和 Challenge 随机 modifier。
- **payoff**：Fire Wall 拥有区域伤害；Electric Gun 拥有控制；能量与双倍组件只加速，不拥有最终伤害。
- **survival**：Horus 的等级 7 被动让角色在致命伤后继续存活十秒，作为被禁 Necrotic Shield / Moon Blade 等旧强件后的输出窗口。
- **spatial condition**：Ghost Claw Shield / Blessing 把敌人聚到火墙；敌方间歇免控明确打断空间 + 控制链。
- **economy / pivot**：事件的 Blacksmith 复制 Red+ 或补特殊套装，英雄事件给独有套装；普通重铸被玩家认为远弱于复制。装备品质与 Contract 点共同决定是否能从临时件转成特殊套装。
- **version / counter**：Challenge 当时直接禁用 Headlamp、Necrotic Shield、Moon Blade + Balance 等普通 Endless 强件，迫使换生存 / 控制方案。4 月 25 日与 5 月 18 日后规则大改，不代表当前强度。

## 元素、状态、护盾与 Contract 的正交关系

4 月 25 日官方规则把状态拆成不同资源：Weakness 最多十层并降低物伤、魔伤和治疗，满层附加 Healing Block；Poison 无限叠但属性削减封顶，Burn 无限叠且刷新持续，Armor Melt 最多十层提高承伤；Frost / Ice Energy 有独立累积与消费。Ice Power 要先通过每两次施法获得 Deep Freeze，再在 Ice Energy 20 层时消耗 Deep Freeze 和全部能量，按最大生命对七个随机敌人造成反射伤害。

同一补丁里的 Whisper of the Wind Necklace 读取 Frost → Poison → Wind Shield，Guardian of Fire Spirit 读取治疗 → 下次普攻 Burn，Rogue 高阶 Contract 再把 Burn / Poison 立即结算或翻倍。这里的设计价值是：元素 / 状态提供 state 和选择器，Shield 提供 survival，Contract 决定广播范围，具体武器 / 宝物仍是 payoff owner；它们能组合但不是一条“元素盾羁绊”。

6 月 27 日又增加 Magma Core：每十秒获得 `500% INT` 盾，盾存在时魔法伤害 +15%。这是清晰的防守转输出转换：INT 同时决定盾量，盾的存在是门槛，套装 owner 获得魔伤增幅。它也提示风险——若 INT、盾量、存活和伤害都在同一 owner 自闭环，必须用持续、破盾、重伤 / Weakness 或敌方节拍拆开。

## 供给、替换与机会成本

- **共享三选面板**：角色、装备和敌人争同一次刷新。玩家能刷更好打但低收益的敌人，也能锁住买不起的件；路线、商店和对手选择因此使用同一货币与注意力。
- **装备合成 / 重铸**：同名三合一保证纵向升级；同品质三件随机重铸提供清仓与赌高阶，但会吞掉可能的套装 / 特殊件。6 月 27 日的一键合成是基于玩家反馈的操作修复，不改变三合成本。
- **事件供给**：Blacksmith 复制 / 补套、Mighty Hunter 交换、Fate 起手套装、Boss 套装事件属于普通商店之外的定向来源。若特殊件不进商店，事件出现率就决定 build 可完成性。
- **Book 与低阶材料锁出**：旧评测报告 Book 太高后普通商店不再给白色材料，而晚到的白色特殊套装需要低阶复制 / 合成，迫使玩家找低阶掉落敌人。6 月更新让高阶红装给更多材料，但是否完全解决特殊套装追赶未知。
- **角色替换**：角色自带被动会锁定适配方向，后期角色越来越贵；武器能换职业，却不能无损替换已经分配的经验 / 被动。5 月 18 日为标准 Endless 加每 90 层换武器，是很晚的显式 pivot window。

## 失败、反制与生命周期

本 checkpoint 计八个 materially distinct negative / reworked cases：

1. **无限模式成长封顶错配**：早期玩家在 120–165 层报告装备 / 角色成长封顶而敌人继续增长；5 月 18 日取消角色等级和红装强化上限，并允许红+1 进入高 Book 商店。
2. **Treasure Synergy 2.0**：旧固定联动改为 20→175 能量预算与自由组合。它是构筑容量 / 选择权重做，不等于证明新系统已平衡。
3. **重复合成操作负担**：多条评测抱怨长局反复卸装、合成、换装；6 月 27 日官方明确因玩家反馈增加一键合成。
4. **角色被动状态不一致**：3 月 6 日修复被动一级提前生效、购买前后变化、升级属性未应用和天赋跨存档残留；7 月 4 日仍继续修复描述 / 实际效果并重做 Swift Coryet。
5. **装备事务 / 持久化缺陷**：3 月 31 日修复重铸拖拽时装备消失和旧档迁移冲突警告；装备在角色、背包、事件和重铸之间转移需要原子结算。
6. **战斗归因不可读**：120 小时评测列出无敌方血条 / 盾条、状态层 / 来源缺失、术语不一致和多段普攻触发歧义；官方随后补战中详情并修复承伤、属性和 Boss 文本，但没有证据证明问题完全消失。
7. **更新回归破坏起手 / 奖励**：2025-07-05 讨论报告 Destiny 起手特质和战斗装备奖励同时失效，开发者称已修；7 月 8 日官方补丁确认 Destiny 修复。一次更新同时污染起手契约和收益链。
8. **后期路线收敛**：历史讨论报告 mass Ranger、反伤、群怪迫使盾 / 净化 / 多目标；2026 长评又称重伤使治疗路线失去意义、敌人倍率与百分比减益让可行套路集中在早期少数装备。无统计，作为跨时期社区负面观察而非客观最优解。

另有 Challenge 防御值溢出、Thief 在特定 Contract 等级不进战斗、远程目标错误、状态层不叠、事件高层不触发等官方修复。它们已记录在来源 / dossier，但没有为每个单独增加累计数字，避免把短热修灌成设计结论。

## 对本项目可迁移

- **体系轴要正交但能连接**：角色被动、武器职业、Contract、套装、状态、宝物联动和 Blessing 分属不同 owner；稀缺连接器负责把 Frost → Poison → Shield 或 Healing → Burn 串起来。
- **全队广播是 Contract 的选择，不是默认**：职业专属高倍率与全队低倍率可以同节点二选一。盾体系可由少数 Guard / Priest 供 survival，再让明确 carry 读取“有盾”获得输出，不必人人都变法强。
- **防守转输出要有门槛和破口**：Magma Core 用“盾存在”门槛把 INT 盾接到魔伤；Weakness / Healing Block、破盾节拍、敌方免控和后排狙击分别攻击不同层。
- **Book 等级是本局供给曲线**：升级同时改善商店品质、生命、Blessing 和 Synergy 容量会形成过强单轴；应拆出可见阶段门槛、升级成本与错过低阶材料的补救。
- **事件可做定向完成器**：特殊套装先由事件给首件，再由 Blacksmith 复制 / 补套，比无限刷新更能解释“为什么这局能成”；但必须预览事件可能产物并提供缺席兜底。
- **敌人选择可与商店共用决策面，但不能共用盲区**：高收益禁技能、反伤、Ranger、免控等敌人应该在锁定 / 刷新前显示考试项和奖励，不应等战败才揭示。
- **报告必须显示来源与边界**：盾来源、状态层、伤害类型、攻击次数、是否触发 on-attack、Contract 广播、目标选择和免控窗口都需要可追溯。

不可直接迁移：八人 + 六后备的精确容量、角色 / 装备 / 敌人共用一个无上限刷新池、全局经验手动点八人、固定 15 层 Boss、无限红装强化、300–900 层长局、Workshop 自定义武器进入普通单机规则、旧版特定装备数字，以及用永久控制或十秒免死覆盖所有敌人考试。

## 未决问题

- 旧 `Magicbook AutoBattler` 的 App id、最终版本和 Contract 重发前的精确规则差异；官方只确认旧目录名与存档迁移。
- 2025-07-08 后是否存在未公告热更；2026 评测仍报告装备穿戴与后期收敛问题。
- 当前完整 Magicbook 升级价格、商店概率、Contract 阈值、Treasure Synergy 能量成本和装备池。
- 八人真实站位拓扑、近 / 远 / 最远目标的平手顺序、Taunt / knockback / re-target 的确定性。
- 6 月 27 日 Workshop 武器的本地 run、成就、存档和普通模式边界；公告只明确不得进入排行榜。
- Guide 的部分角色 / 套装信息依赖截图；本 dossier 只采用文字可读的武器、分工、事件与版本，不补猜图片内容。
- 5 月 18 日后旧 Freeze + Bleed / Challenge Fire Wall 构筑的强度，以及 2026 所称少数路线的采用率。

## Disposition

`retained`

它满足规则＋多份独立实践门槛，并闭合三个版本化构筑：历史 Freeze + Bleed 玻璃炮、盾叠层双法双战适应线、Challenge 火墙永控。每套都能说明 engine、state、payoff、survival、空间选择、owner、经济 / 替换与敌方反制；官方重做又能解释上限、宝物容量、操作与报告失败。资料虽多，但缺维护数据库、统计和当前精确规则表，不升为 anchor。
