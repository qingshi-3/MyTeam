# Backpack Battles

## 身份与研究时期

- `title_id`: `backpack-battles`
- 子类型：异步 PvP 背包网格构筑、自走战斗；主要操作发生在商店、合成与物品摆放阶段。
- 状态：已于 2025-06-13 从 Early Access 进入 1.0，2026-03-27 推出 1.1；本批最新官方平衡公告为 2026-08-12 的 1.1.8。
- 观察时期：2024-03 Early Access 具名构筑、2025 1.0 开发回顾、2026 1.1–1.1.8 官方变更与 2026-07 实战复盘分开处理。
- 直接体验：本轮未安装或实玩；所有格位、资源、构筑与反制结论均限定在可读来源的时期。

## 检索日志

1. 打开 Dood 的机制页与职业页，提取 Mana、Poison、Spike、Stun、Heat、Stamina 和职业转型；因其术语/数值没有逐项版本映射，只作为二级路线材料。
2. 打开 IGN 的 Pyromancer 与 Reaper 专门构筑页，核验具体配方、核心件数量、星位/邻接、早中期过渡、收益所有者和反制。
3. 打开 TheGamer 的 Pyromancer Dragon build，取得 Round 8 Dragon Nest、Egg 商店、宝石所有权、无 Stamina 攻击与提前预留背包空间的独立路线。
4. 打开 BackpackBattles.top 的具体 Lantern Golden Pan 实战复盘。页面明确说明其为 Bilibili ASR 的编辑改写而非 transcript；只记录页面实际可读的运营判断，不假装是视频逐字证据。
5. 通过 Steam 官方 news feed 读取并映射 1.0、1.1、1.1.7、1.1.8 稳定公告 URL，核验版本、界面归因、网格编辑、资源和连续平衡改动。
6. `backpackbattles.wiki.gg` mechanics/recipe、Pro Game Guides、ESTNN 与 Reddit 返回 403/blocked；Steam Community Guide 列表可读但单篇正文为 SSR 壳。均未绕过，未计为实质来源。

## 来源表

| ID | 来源 | 发布者 / 日期 | 类型 / 质量 | 主要用途 |
|---|---|---|---|---|
| `src-bpb-dood-mechanics-2026` | [Mechanics Guide](https://dood.gg/en/backpack-battles/guides/mechanic-guide/) | dood GAME DB / 更新 2026-08-31 | strategy-guide / C | Mana、Poison、反伤、控制、Heat、Stamina 的二级机制解释 |
| `src-bpb-dood-classes-2026` | [Class Guide](https://dood.gg/en/backpack-battles/guides/class-guide/) | dood GAME DB / 更新 2026-08-31 | strategy-guide / C | 职业/子职业、组件、阶段路线、弱点与转型 |
| `src-bpb-ign-pyromancer-2024` | [Pyromancer Build Guide](https://www.ign.com/wikis/backpack-battles/Backpack_Battles_Pyromancer_Build_Guide) | Jen Rothery、Marloes Valentina Stella / 2024-03-18–19 | strategy-guide / C | Ashbringer Phoenix 的精确组件、格位、资源和过渡 |
| `src-bpb-ign-reaper-2024` | [Reaper Build Guide](https://www.ign.com/wikis/backpack-battles/Backpack_Battles_Reaper_Build_Guide) | Jen Rothery、Marloes Valentina Stella / 2024-03-20 | strategy-guide / C | Venomancer Snake and Scythe 的阈值、星位、生存与弱点 |
| `src-bpb-thegamer-pyromancer-2024` | [Pyromancer Build Guide](https://www.thegamer.com/backpack-battles-pyromancer-build-guide/) | Harry Alston / 2024-03-14 | detailed-review / C | Scalewarden Dragon 的 Round 8 转型、宝石与格位 |
| `src-bpb-top-lantern-pan-2026` | [Lantern Golden Pan Strategy Notes](https://www.backpackbattles.top/en/articles/backpack-battles-mocha-radio-lantern-golden-pan) | BackpackBattles.top 编辑改写 / 2026-07-14 | community-analysis / C | 当前实战中的半启动、卖件、格位、资源和针对性防御 |
| `src-bpb-official-1-0` | [Patch 1.0](https://steamcommunity.com/games/2427700/announcements/detail/520840848816998130) | PlayWithFurcifer / 2025-06-13 | official-dev / A | Early Access 回顾、内容池扩展、配方/归因 UI、触发来源收紧 |
| `src-bpb-official-1-1` | [Patch 1.1](https://steamcommunity.com/games/2427700/announcements/detail/515237350113018825) | PlayWithFurcifer / 2026-03-27 | official-patch / A | 物品/包独立编辑、grid storage、历史版本、战斗 UI 与新内容 |
| `src-bpb-official-1-1-7` | [Patch 1.1.7](https://steamcommunity.com/games/2427700/announcements/detail/712279347386390943) | PlayWithFurcifer / 2026-07-10 | official-patch / A | Stamina、冷却和资源上限的当前平衡 |
| `src-bpb-official-1-1-8` | [Patch 1.1.8](https://steamcommunity.com/games/2427700/announcements/detail/716786751176180649) | PlayWithFurcifer / 2026-08-12 | official-patch / A | 1.1.8 当前版本、连续 Stamina 上限回调和职业调整 |

## 真实循环与玩家决策

每轮先在商店购买、出售、刷新和保留物品，再通过相邻配方合成，并把有形状的物品放进有限背包格。包本身提供格位或局部增益；物品的星位、影响范围、相邻对象、朝向和是否完整落在加速包中，会改变发动对象与频率。战斗自动进行，胜负后进入下一轮商店；玩家的“操作”主要是让经济、配方、资源供给、格位和对手反制在开战前闭环。

1.0 增加配方关系固定提示、受影响物品高亮、物品伤害/治疗数字与全角色状态显示；1.1 又加入包与物品分离编辑、grid storage、无法移动/无法开始配方的反馈，以及按生命与 Block 总量移动的战斗状态图标。这些不是单纯收纳便利：构筑是否发动取决于哪个物品影响谁，因而编辑与归因界面属于规则可读性的一部分。

经济不是“看到终局件就买”。历史攻略和 2026 实战都反复出现三类机会成本：当前回合保血对留钱/刷新、过渡件对终局件、经济件对核心格位。Lantern Golden Pan 案例中 Golden Pan 很早完成，仍因 Magic Orb/Mana 与启动格缺失而处于“半启动”；中期必须卖 Pig、Gem Box 或过期小件，才有资金和空间寻找资源闭环。

## 构筑语法

Backpack Battles 的常见构筑结构可写为：

> 配方/局部激活 engine + Mana/Heat/Stamina/Poison/Block/格位 state + 武器、阈值或复活爆发 payoff + Block/治疗/净化/无敌 survival + 形状/星位/相邻/包内 condition

收益所有者必须拆开。例如 Snake 保护敌方 Poison 不被清除，但 Death Scythe/Poison 才兑现伤害；Moon Shield 用 Block 产 Mana，但 Glowing Crown 消费 Mana 兑现无敌；Fanny Pack 只是速度来源，Phoenix 或 Dragon 才是攻击收益者。只数“火物品”“毒物品”或“防具”会把发动机、资源和收益者混成同一维。

## 具名构筑一：2024 Early Access Ashbringer Phoenix Dark Lantern

核验来源：`src-bpb-ign-pyromancer-2024` 提供完整构筑表和摆放说明；`src-bpb-dood-classes-2026` 仅用于验证该子职业仍以 Dark Lantern/复活为识别结构；`src-bpb-official-1-1` 与 `src-bpb-official-1-1-8` 证明后续物品与数值仍持续变化，不把 2024 强度外推到 1.1.8。

- `engine`：Pyromancer 的 Flame/Fire Pit 建立最大生命和 Heat 起点；Burning Sword/Blade、Fire/Dark 物品围绕 Dark Lantern，Chili Goobert 由左右可激活物品累计触发。
- `state/resource`：最大生命、当前生命、Heat、Dark/Fire 受影响物品数、Goobert 邻接激活次数、Phoenix 触发频率、Stamina 与背包格位。
- `payoff`：Ashbringer 开战自损半血，Dark Lantern 在败亡前复活并按受影响 Fire/Dark 物品兑现爆发/减益；Phoenix 以生命资源换取输出并提供复活/治疗侧价值。主要伤害 owner 是 Phoenix 与 Lantern 复活事件，不是 Fire Pit。
- `survival`：Sun Armor 供 Block，Chili Goobert 治疗并清理自身负面；复活和短暂无敌给慢启动火系第二个行动窗口。
- `spatial condition`：Dark Lantern 必须被 Dark/Fire 物品包围；Sun Armor 周围只放能满足其 Fire 条件的物品；Phoenix 放入一层或多层 Fanny Pack 加速；Chili Goobert 左右必须有会激活的邻居。
- `equipment owner`：Draconic Orb 和宝石优先服务终局武器/Phoenix；Sun Armor 属于启动保护而非输出件。若采用双 Phoenix 单近战武器，可减少 Stamina 补件；两武器版本需 Banana/Topaz 等供给。
- `recruitment/economy/pivot`：前期用 Torch/Burning weapon 和打折防具保血，扩包并留 Chili Pepper、Goobert、Flame；至少有一把 Burning Sword 后再找 Draconic Orb。核心 Fire/Dark、治疗或格位迟到时不能只因 Flame 多就硬锁 Ashbringer。
- `counter`：Cold/减速或 Stun 延后关键激活；开局爆发可能在生存层闭环前压穿；防具/净化不足会让“先自损”变成真实风险。历史来源没有 1.1.8 胜率，不能称为当前最强。
- `version context`：2024-03 Early Access，确切补丁未标明；保留的是“自损→保护→复活爆发”的结构，不保留页面数值为当前规则。

## 具名构筑二：2024 Early Access Venomancer Snake and Scythe

核验来源：`src-bpb-ign-reaper-2024` 的具名构筑表、配方、数量和摆放；`src-bpb-dood-mechanics-2026` 与 `src-bpb-dood-classes-2026` 只交叉验证 Poison 不自然衰减、净化/抗性和 Venomancer 路线，不承担历史数值权威。

- `engine`：Storage Coffin 内物品激活有机会施加 Poison；Poison Goobert 在邻近物品多次激活后净化自身 Poison 并反施给敌人；Death Scythe 放大相邻施毒并等待阈值。
- `state/resource`：敌方 Poison 层数、Snake 的 Luck 与宠物星位、Coffin 内激活次数、Goobert 邻接次数、己方 Block、Mana、Glowing Crown 消费窗口和格位。
- `payoff`：历史 Death Scythe 在敌方约 35 Poison 时取得关键暴击收益，Poison 持续掉血；Scythe/Poison 是 damage owner，Snake 是防清除与生存发动机。
- `survival`：Moon Armor/Moon Shield 产生 Block 与 Mana，Glowing Crown 消耗 Mana 取得短暂无敌/净化；Emerald 放在护甲上可按己方施加 debuff 的次数补 Block。
- `spatial condition`：武器放进 Coffin 才利用其施毒机会；Snake 星位尽量覆盖 Pets 以换 Luck/最大生命；Death Scythe 邻近施毒物品；Poison Goobert 邻近足够多的可激活件；Moon 系列要连到 Magic/Mana 与 Crown 链。
- `equipment owner`：Corrupted Crystal/Emerald 等宝石优先放在武器或护甲上形成“施毒→Block”；Crown 是 Mana 的终端 owner，不能让不相关魔法件提前耗尽启动资源。
- `recruitment/economy/pivot`：Death Scythe 未出现前用可出售普通武器、Fly Agaric、Poison Dagger/Pandamonium 过渡；同时找 Goobert + 两个 Fly Agaric、Mana Orb 与护甲配方。Scythe、Poison Goobert、Moon Armor 长期不到时，继续追毒会把 RNG 缺口放大。
- `counter`：Poison cleanse、debuff resistance 与 Snake Luck 的保护直接对抗；Mana 断供会关闭 Crown 无敌；核心传奇武器缺失会导致层数有了但没有高效收益者。快速爆发也可能在阈值前结束战斗。
- `version context`：2024-03 Early Access；35 Poison 等精确阈值仅属于来源时期，1.1.8 当前数值未由该攻略证明。

## 补充具名构筑：2024 Scalewarden Dragon Nest

`src-bpb-thegamer-pyromancer-2024` 给出一条和 Phoenix 不同的 Fire payoff：前期 Flame + Fire Pit 堆最大生命，用 Torch→Burning Torch、Molten Dagger 过渡，购买 Box of Riches 积累宝石，并在 Round 8 先选 Dragon Nest 再刷新 Egg，因为 Nest 提高 Egg 出现并开放多种 Egg。Fanny Pack 与 Topaz 加速 Dragon，Ruby 提供吸血；Dragon 无 Stamina 成本，因此它把“武器耐力瓶颈”换成“Round 8 前必须预留大量背包空间和宝石”的格位/经济瓶颈。该路线仅作 2024 历史对照，不宣称是 1.1.8 meta。

## 当前实战样本：2026 Lantern Golden Pan 慢火循环

`src-bpb-top-lantern-pan-2026` 记录了更接近当前时期的一局复盘，但它是 ASR 编辑摘要而非逐字视频证据。

- `engine`：Golden Pan 与 Lantern 给出火线方向，Magic Orb/Mana 才让它持续发动；Chili Pepper 供火层，Rainbow Bird/Rainbow Orb 把速度分配给 Magic Orb、Horn、Nest 和 Chili Pepper。
- `state/resource`：Mana、启动秒数、Golden Pan/Lantern 是否真正入场、容器/半价格/Pig 的经济、背包空间、Stamina 与对方控制/Poison。
- `payoff`：Golden Pan/火线是伤害 owner；Horn 的 Stamina 削减是独立控制收益。早到的 Golden Pan 若没有 Magic Orb，只有方向没有闭环。
- `survival/spatial`：Stone Helmet、Yellow Gem、Shield 和防御宝石保护前几秒；加速范围必须覆盖真正缺资源的 Magic Orb/Chili Pepper，而不是随机铺给高稀有物品。
- `pivot`：容器与 Pig 可帮助早期经济，但 Magic Orb 久不出现时必须卖经济件和过期过渡件；先确保 Golden Pan、Lantern、Chili Pepper 与关键防御能放下，再追额外火件。
- `counter`：Stun 中断启动，快 Poison/爆毒跳过成长窗口，开局爆发压穿防御；因此抗眩晕、抗毒和护甲优先级可高于继续增加 Heat。

这一案例的重要结论不是“Golden Pan 强”，而是**核心件先到不等于构筑已成立**：资源供给、格位和第一轮行动保护缺一项，UI 应显示“缺 Mana/缺启动保护/关键件未入场”，而不是只显示已拥有火系标签。

## 资源、格位与顺序

### Mana 与 Stamina 不能混成统一速度

Dood 将 Mana 描述为被特定法术/药水消费的池，耗尽后相关物品停转；IGN Reaper 和 2026 Golden Pan 分别提供 Moon 系列产 Mana 与 Magic Orb 迟到造成半启动的实战例子。Mana 因此是明确的生产—消费闭环。

Stamina 主要限制需要它的武器节奏。IGN Pyromancer 明确在加入第二把武器时要求 Banana/Topaz，而 Dragon 路线因为攻击不耗 Stamina 能绕开该瓶颈。Dood 把 Stamina 概括为“整体战斗速度”，这一表述过宽；本 dossier 只保留已被多来源支持的窄结论：**纸面武器很多不等于有效 DPS，供给不足会让攻击排队/降频**。官方 1.1.7 与 1.1.8 连续调整 Stamina cost、reduction、maximum 和 refund，也证明它是独立且敏感的吞吐轴。

### Poison 是状态，阈值收益者是另一层

Poison 可被多件持续施加并由 cleanse/resistance 对抗；Venomancer 又加入 Snake 的防清除、Death Scythe 的阈值收益和 Crown 生存。因而“毒体系”至少包含产层、保层、阈值 payoff 和拖到阈值的 survival，不能只把所有施毒件归成同类。

### Block、反伤与转换需要记录方向和来源

Dood 的二级解释把 Block/护甲减伤与 Spike 反伤分开：一个降低来袭伤害，一个在受击时返回伤害。官方 1.0 又把 Smelly Wall 收紧为“只有特定星位产生的 Block 才触发 Poison”，说明防御事件转状态时必须保存来源。1.1 的 Mercury Elemental 写的是“Damage as Block”，方向是输出→防御；不能把它误读成 Block→Damage。对本项目而言，每条转换都要明确方向、owner、可递归性和一次事件是否能二次计数。

## 反制与失败归因

- **启动前爆发**：Phoenix、Venomancer、Golden Pan 都需要若干触发或阈值；首轮爆发可以在它们闭环前结束战斗。
- **Stun/Cold/节奏压制**：控制会延后核心物品；慢火实战把抗眩晕列为保护启动的优先补丁。
- **资源破坏**：缺 Mana 关闭 Crown/Golden Pan 链，缺 Stamina 让多武器纸面伤害落空；Horn 等也能从对手侧压制 Stamina。
- **净化与抗性**：直接削弱 Poison 的产层/保层；快毒对慢净化则可能先跨过成长窗口，双方都是时间竞赛。
- **格位压迫**：拿到组件却放不下、加速范围覆盖错对象、经济件挤占核心位，都是可单独归因的失败，不应统一写成“运气差”。
- **商店缺件**：可先由过渡武器/防具解释当前战力；当关键传奇、Mana Orb 或配方长期不到时，必须允许卖件转线，而不是让主题标签绑架后续购买。

## 生命周期与负面/重做案例

1. **早期终局武器过窄**：1.0 官方回顾明确说早期 late game 只能是 Bloodthorne 或 Lightsabers，因为当时只有这两类终局武器；开发期从约 50 件/2 职业扩展到 1.0 的 439 件/6 职业。它能证明“终局收益者不足导致路线收束”的历史约束，不能证明扩充后的全部组合都同样有效。
2. **Block 转 Poison 的来源范围被收紧**：1.0 将 Smelly Wall 改为只在星位产生 Block 时施毒，避免所有 Block 来源都无差别喂给同一个进攻触发。这是明确的触发作用域重写，可作为防止防御循环泛化的负面/重做案例。
3. **Hogus Bogus 的 Stamina 上限连续回调**：1.1.7 把最大 Stamina 使用从 6 提到 8、每 Stamina buffs 从 4 降到 3；1.1.8 又把最大使用从 8 降回 6并提高自眩晕。官方没有说明胜率或原因，因此只认定“收益上限/副作用在相邻补丁被快速再校准”，不声称旧版必然 OP。
4. **空间编辑与归因 UI 持续补强**：1.0 加受影响物品高亮、配方固定、伤害/治疗数字；1.1 加独立编辑模式、grid storage、配方失败动画、战斗生命+Block 指示和历史版本记录。资料能证明官方持续提高可读性，但没有用户研究证明这些改动已完全解决误摆或归因问题。

本 checkpoint 将前 3 项计为明确负面/重做案例；第 4 项只计生命周期/可读性演进，不把新增 UI 倒推成旧版“失败”。

## 对本项目的迁移

可迁移：

- 用“发动机 + 资源 + 收益者 + 生存 + 空间条件”判定体系是否成立；同标签数量只表示候选件，不表示闭环。
- 防御体系可以靠明确转换输出，但要像 Smelly Wall 一样限定**哪一种 Block/盾来源**可以喂给 Poison/伤害，避免所有护盾生成器无成本叠乘。
- 全队防御转给单一射手时，应像 Snake/Death Scythe 或 Moon Shield/Crown 那样分开“广播提供者”和“最终 owner”，并在报告中显示每次贡献来自谁。
- 慢启动构筑必须同时提供启动保护与反制窗口；抗控制、净化、爆发敌人和资源干扰应成为可预告的敌人包，而非暗中封杀。
- 格位/邻接的价值来自“谁影响谁”，不是把所有东西挤成俄罗斯方块。即使本项目使用战场站位而非物品背包，也应显示有效链接、遗漏链接和被错误占用的范围。
- 核心件早到但资源没闭环时，UI 应提示“半启动”和具体缺口；这比只给“火系 6/6”“盾 4/4”更能支持转型。

不可直接迁移：

- Backpack Battles 的异步 PvP、物品俄罗斯方块、无限准备时间、商店配方与具体职业包不是本项目默认结构。
- 2024 Early Access 构筑的数值、稀有度和当前强度不能成为第一版平衡目标。
- Dood 的整体速度/强度概括没有官方逐项校验；不能据此设计底层时序公式。
- 大量物品池和长期高频平衡允许更复杂的偶发组合；本项目第一版不应以 500+ 物品规模作为丰富度前提。

## 未决问题

- wiki.gg 规则与配方页不可访问，当前缺一份可公开读取、逐版本维护的第一方/高质量规则手册。
- 2024 IGN/TheGamer 构筑未写精确 Early Access 补丁号；所有具体数值只作历史，不与 1.1.8 合并。
- 2026 Lantern Golden Pan 是编辑过的 ASR 摘要，没有原视频 transcript 或公开对局统计；可支持一局运营链，不能代表整体 meta。
- 没有可访问的当前胜率、采用率或 matchup 数据；所有“强/弱”评价只保留为来源作者判断。
- Dood 对 Stamina 的“整体速度”描述过宽；后续如找到官方完整时序说明，应替换该二级概括。

## Disposition

- `disposition`: `anchor-retained`
- 置信度：官方版本、编辑/归因功能与明确数值变更为高；2024 两套具名构筑结构和格位为中高但仅限历史；2026 单局转型为中；当前 meta 强度为低。
- 判定：10 个实质非商店页面，覆盖 `official-dev`、`official-patch`、`strategy-guide`、`detailed-review`、`community-analysis` 五种来源类型；含两套满足完整字段的具名历史构筑、一套独立 Dragon 路线和一套 2026 当前实战闭环，并记录三项明确负面/重做案例，达到 anchor 门槛。
