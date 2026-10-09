# 随机大陆探索：实施设计

2026-10-02。设计负责人：主 Agent。状态：**空上下文独立初审与两轮复核完成，ER01–ER08 在设计层关闭，尚未运行实施。** 本文件承接 R10-Q11／D11 和用户“流程没什么问题，补设计并以空上下文子 Agent review”的授权。是实施前契约，不是已经存在的运行系统。用户未逐项认可下面的参数；数值是 Agent 选择的首轮配置，不能宣称平衡通过。

## 1. 交付边界与体验闭环

默认成长征程的新局切换为随机大陆；继续旧局走其原进程，实验室和 Alpha 不被改成地图。一个完整新局包含开局六选二、随机地图与出生、迷雾、真实步行／回访、敌人绕路／受阻／清除、招募／商店／遗迹／营地、三阶段 Boss、正常胜败和持久化恢复。沿用正式英雄／装备／遗物／成长／法术及战斗模拟，不另写一套地图专用战斗或奖励引擎。

首轮配置选择：16×16，三个地域，三阶段各 6 行动，普通／精英／一次性地点处理消耗 1；阶段 Boss 不额外消耗探索行动。可主动放弃剩余行动提前迎战，无收益补偿。现有全局生命 100、普通败战 20、精英败战 30、营地恢复 25、Boss 失败／超时终局保持；每战英雄满血保持。新参数集中在作者资源，不能散落常量。

每地域配置 13 个内容地点：7 普通敌人、1 精英、2 招募、1 商店、1 营地、1 遗迹；总计 39 个，18 次阶段行动不要求全收。空格承担可读地貌和通路，不是事件也不收费。普通地点敌人强度在生成时固定，区域难度不因阶段或查看次数重抽。阶段 Boss 由阶段推进出现，不占地图通路。

这些数量用于实施与诊断：18 个有行动的处理＋2 个非终局 Boss 可产生最多 20 次常规成长，明显不同于旧 15 层的最多 14 次非终局成长。不能照搬旧 Boss 数值就宣称节奏平衡；首轮先保留每次成长语义与成本，以当前内容做自然供给诊断，再调地图预算和明确的遭遇强度配方。不得偷偷限制可生效的行动来伪装成旧 14 层。

## 2. 地图生成、可见与通行

### 2.1 确定的生成流程

1. `GenerationVersion + RunSeed` 派生拓扑、地域、地点、遭遇、库存各独立随机流；稳定排序后采样，不依赖字典遍历、帧时间或 `GetHashCode()`。
2. 先生成三个连通地域及连接走廊，整体约 55%–70% 可通行格；再加入局部环路、狭口和支路。图结构与地形呈现分开，不向每格独立抽事件。
3. 随机确定地域在大陆的位置／旋转和出生点；起点在低压力地域，周围安全且至少两个可探索方向。高压力地域可提前发现，不因阶段变化强化已经生成的敌人。
4. 按地域配额放内容；一格最多一个地点。敌人格在未击败时阻挡通行；其他地点格可通过且不自动交互。首版绕过仅通过真实四邻替代通路实现，不加潜行或免费穿怪按钮。
5. 地点引用完整遭遇／服务配方。开局选队成功时生成内容：起始身份从普通招募候选排除；地图全部地点及库存固定，后续升阶得到某身份只会使对应招募不合法，不会重抽该格。
6. 验证后整张地图与选队确认一次保存。沿用现有单活动槽：点击新征程，六选二草稿保存成功即替换上一局；保存失败仍保留上一局。随后选队确认／地图生成失败，保留的是本次草稿、种子与已选英雄，不承诺恢复已主动替换的上一局。最多 64 次确定性生成尝试，仍失败时显示可诊断错误，不扣费、不转成爬塔、不用隐藏固定图；继续仍回该草稿。

### 2.2 必须验证的性质

- 地形连通；所有内容都可在其守军清除后抵达，无墙内、出生重叠、孤岛和非法格。
- 初始安全连通区至少有 1 个合法招募、1 个可处理普通敌人及恢复／金币服务；营地满血仍可选择其军费选项。首场候选使用早期完整敌阵，不能用强敌挡住所有发育。
- 至少 2 处敌人可通过另一条真实通路绕过；至少 2 处局部守关确实控制其后支路，绕路检查按敌人格不可通行执行。不是每家店都要守关。
- 起点在将全部未清敌人格移除后的自由通行分量中，至多可直接访问 5 个服务地点，且不能直接走遍三个地域；至少一条向外扩展的边界是已配置的早期普通敌阵，不要求先赢精英。生成验证按实际格子连接重新计算，防止绘制地形时产生绕开全部守关的缝隙。阶段 Boss 不受这些地理门槛限制；局部环路仍允许绕过非关口敌人。
- 总内容量／类别配额满足配置；三个地域都有后续机会，阶段触发不要求固定地理次序。对行动数的保证是机会可用与容量，不冒充任意阵容都能获胜。
- 不依赖唯一英雄或唯一装备解锁地图。首版生成硬约束仅检查真实分阶池、起始身份排除、不同身份数量与合法接收机会；不引入未有证据的“治疗／控制职责覆盖”标签。阵容抓手与自然供给覆盖列入 §8 内容诊断，不能以 UnitRole／stable id 或未实现标签冒充能力保证。
- 长距离空路超过 4 格时压缩拓扑或补分叉／可观察地标，不通过自动传送省略未探索区域；只在已知合法路径上提供连续行走。

### 2.3 视野与交互

当前位置及八邻显示地貌／地点类别；只能四邻移动。已知敌人可看完整敌阵、强度、战场、胜负后果和可能奖励；远处不显示全地图收益清单。墙后视野首版不实现光线追踪，但可见不代表可通行。无数字推理，线索来自真实地点内容。

移动、揭示、查看与旧路回访 0 行动、0 成长；每个确认完成的格步由命令保存位置与揭示，再播放过渡。长路径在新前沿、敌人格前停止，可取消剩余路径。路径只使用已知合法格，不通过寻路结果泄漏迷雾。战前预览与取消不移动位置、不支付；胜利清除敌人格但队伍留在原交战位置，下一步由玩家决定。

## 3. 当前内容如何接入

### 3.1 内容与配方来源

| 职责 | 当前证据 | 新设计的用法 |
|---|---|---|
| 真实内容目录 | `content/growth/growth_catalog.tres`、`manifest.json`、`GrowthContentPackage` | 同一发布流程校验完整英雄、装备、遗物、能力与场景引用；不另建地图 ID 名单 |
| 地域遭遇 | `content/growth/encounters/encounter_{ember,gloam,crown}_{combat,elite,boss}.tres` | 复用完整敌阵、单位和战场资源；新地图遭遇配方显式引用原 `EncounterDefinition` 与一个组成稳定 ID |
| 首区早期敌阵 | `ember_combat_early_ranged`、`ember_combat_early_rush` | 起点压力候选，只作作者配置，不在生成器里判断具体字符串 |
| 三阶段 Boss | `encounter_ember_boss`、`encounter_gloam_boss`、`encounter_crown_boss` | 首轮按序复用 rampart、shadow、brood 的正式组成与 BossTimeline；阶段资源显式引用，不从楼层取模 |
| 招募与升阶 | `growth_recruitment_supply.tres`、`growth_rules.tres` | 复用分阶身份与成长能力；每招募地点展示一个已固定英雄和接受／离开，不强行三选一 |
| 装备与遗物 | `growth_item_pool.tres`、`RunOfferDefaults.ItemRewardGroups` | 内容自身类别、唯一性与效果保持；每种地点显式引用奖励配方，装备／遗物选择组仍有独立身份 |
| 营地／事件／交易 | `CompiledRunChoice`／`CompiledRunOperation` 与当前默认配方 | 编成新作者 `.tres` 配方并复用操作执行器，保留实际金币／恢复／概率效果；不写地图专用 if-else 发货 |
| 人口与后备 | `growth_run_rules.tres`、`RunPopulationPolicy` | 起始人口 10、普通上限 10，保持当前后备及物理上限约束；Boss 不凭空加人口，没有已实现的“城堡扩格”可直接照搬 |

遭遇配方携带 `EncounterId + CompositionId + AllowedFloorRuleIds + FinalHealthMultiplier + FinalDamageMultiplier + RewardRecipeId`。新地图不运行旧 `MinLocalFloor / MaxLocalFloor` 过滤，也不读取旧局内曲线；下面的**最终倍率**直接作用于单位基础值一次。旧数值只在制作作者配方时取样，运行时不能再乘 Encounter 的基础倍率或曲线。删除没有执行用途的 PressureBand 字段；地域通过显式候选配方和权重决定内容，界面显示实际倍率与阵容。

首版每个组成建立一个配方，表中并列 ID 表示各独立候选，不是临场拼装。允许战场从**该配方实际引用的 EncounterDefinition.FloorRulePool** 编译取值，不能统一套地域普通池：普通／精英用 `pool_{ember,gloam,crown}_floor_rules`，三 Boss 保持各自的 `pool_{ember,gloam,crown}_boss_rule`，分别为 narrow_lanes／hazard_pulse／boss_ward。地图地点生成时选定具体战场；阶段 Boss 从同一生成输入冻结其专属战场。预览与开战都验证完整敌阵站位，不根据当前位置或预览次数换战场。

| 遭遇来源 | 组成（省略与地域相同的前缀） | 最终生命／伤害倍率 | 胜利基础金币／组成额外收益 |
|---|---|---|---|
| ember combat 入口 | `combat_early_ranged`、`combat_early_rush` | 0.8／0.8 | 7；分别研究 2／材料 1 |
| ember combat | `combat_patrol`、`combat_screen`、`combat_rush` | 1.05／1.05 | 7；分别金币 4／金币 4／研究 2 |
| ember elite | `elite_pierce`、`elite_furnace`、`elite_blade` | 1.12／1.12 | 12；分别研究 4／材料 2／金币 6 |
| ember boss | `boss_rampart` | 1.25／1.5 | 18；无组成额外 |
| gloam combat | `combat_swarm`、`combat_beam` | 2.8／1.8998 | 7；分别研究 2／材料 1 |
| gloam elite | `elite_charge`、`elite_line` | 2.38／1.5042 | 12；分别研究 4／材料 2 |
| gloam boss | `boss_shadow` | 2.4／1.4375 | 18；无组成额外 |
| crown combat | `combat_guard`、`combat_line` | 4.35／1.8225 | 7；分别金币 4／研究 2 |
| crown elite | `elite_swap`、`elite_impact` | 4.35／1.8225 | 12；分别研究 4／材料 2 |
| crown boss | `boss_brood` | 3.3／1.82 | 终局，仅保留既有验签／终局结算，不生成可继续领取的奖励 |

当前对应曲线中部（下标 2）的最终乘积是初值依据，不是难度排序或平衡证明，尤其普通、精英与 Boss 的阵容和技能不同。`RewardGold/Research/Materials` 直接由选定的组成编译进入 `CompositionBonus`，不在地图配方手工再抄一份。基础金币由 RewardRecipe 定义，组成奖励及遗物收益各有独立账本项。材料／研究仍走已有成长投影；当前通用 operation 没有这两类，不能编造未支持的操作。

地图普通／精英地点一格只有一个具体敌阵；现有精英多选改为地图上不同敌人提供选择，预览与正式战斗使用同一个冻结计划，不在点开后再次抽候选。

普通敌人配方带金币和一组装备机会；精英带其既有组成额外及装备、遗物两组机会；阶段 Boss 带一次常规非终局成长及一组遗物机会，最终 Boss 直接通关。阶段定义只引用 Boss 遭遇配方，不再另发同一份奖励。遗物跳过奖励必须仍有合法选择组，不能用全自动发奖把现有遗物能力做成空效果。

### 3.2 新作者结构与发布

`CampaignDefinition` 继续拥有通用内容池／开局供给／选择配方；新增显式 `ProgressionDefinition`（`TowerProgressionDefinition` 或 `ExpeditionProgressionDefinition`）。把原 Regions/FloorsPerRegion/NodeTable 收入 Tower 定义。当前所有项目资源一次迁成明确的 Tower 或 Expedition 定义；不让新地图依赖一份假塔表。旧序列和已锁机会须由 Tower 编译路径保持。`CompiledCampaign` 保留共同内容，持有类型化进程变体，`TowerGenerator` 只接收 Tower 变体。

新增 `ExpeditionDefinition` 引用 `MapGenerationDefinition`、`BiomeDefinition[]`、`StageDefinition[]` 与 `SiteDefinition[]`；后者引用完整遭遇、招募供给档或 `RunOfferDefinition`。`SiteDefinition` 说明占格、类别、处理成本、一次性／库存规则及表现资源，不存本局可变状态。阶段明确 Boss 配方、行动额度、成长发现档；地域独立说明遭遇候选／招募档，不以 StageIndex 隐式索引某数组。

供给档复用现有 tier 成员，重新以明确档 ID 编译权重：开局／低压力 60/40/0/0，中压力 30/40/30/0，高压力 0/20/45/35；从已读的当前资源取初值，但新采样调用接收档对象，不传一个伪楼层。升阶发现在第一阶段使用当前 FirstDiscoveryPool，后两阶段使用 AdvancedDiscoveryPool，仍须恰好三个不同未拥有身份，否则不扣材料、不升阶。地图招募被冻结后不因升阶重抽。

编译／引用收集必须扩展 `GameProjectCompiler`、`ContentReferenceCollector`／项目能力引用收集、`GamePackagePublisher` 与 ReadyGate。同一次发布先验证地图、阶段、遭遇组成、地形、奖励、招募、成长和所有 BossTimeline loadout，再原子发布；缺任何正式引用则整包拒绝。`GrowthGameRoot` 持有整份 authored package 到退出，沿现有失败重试清理路径；不在游戏已启动后临时 GD.Load 漏掉的技能。

共用遭遇编译负责稳定身份、敌人场景、完整组成、站位／尺寸、Boss 唯一性、timeline 和能力引用；Tower placement 编译单独负责 Min/MaxLocalFloor、曲线长度和逐层覆盖。Expedition 只消费共用内容和自身最终倍率。共享资源按规范路径／对象身份 memoize，同一 StableId 指向同一资源可复用；不同资源声明同 ID 仍报错。一次 ReadyGate 内收集并编译所有 profile 的依赖联合，不因复用同一敌阵重复注册或漏查。

### 3.3 配方索引与首版服务

把现有按 `RunOfferKind` 唯一的 `RunOffers` 拆成 `OfferRecipesById` 与 Tower 专属的 `Kind → RecipeId` 绑定。通用配方编译仍验证已有 condition／operation，但不禁止同 Kind 不同 StableId；Tower 绑定仍要求每 Kind 一份，旧默认配方生成只在 Tower 适配层运行。Expedition 的 Site 直接持有已编译配方引用，运行时不按地点类型硬编码删选项。

`RewardRecipeDefinition` 指向基础金币和**有序奖励组**；每组有稳定 GroupId、类别、候选数及物品配方引用。类别由实际单件 GrantItem 的 ProductKind 编译验证，不能只信作者标签；普通物品组复用 `ItemRewardGroups` 的分类能力。组 ID 在配方内唯一；同一次 action 的奖励身份由 ActionId＋GroupId 构成。服务 offer 和奖励组用同一个 choice evaluator，不给地图另写授予器。

| 首版配方 | 明确绑定与候选 | 支付／收益 |
|---|---|---|
| `exp_reward_normal` | 当前成长物品池按 Equipment 分组，最多 3 个不同候选，取 1 或放弃 | 胜利基础 7 金币；加实际 CompositionBonus |
| `exp_reward_elite` | Equipment、Relic 两组，各最多 3 个不同候选，每组取 1 或放弃 | 基础 12；加实际 CompositionBonus |
| `exp_reward_stage_boss` | Relic 一组，最多 3 个不同候选，取 1 或放弃 | 基础 18；加非终局常规成长一次 |
| `exp_recruit_single` | 按地域 supply profile 从当前 26 身份分阶池选 1，排除开局两人；不是六选二，也不随机补人 | 接收花 1 行动、不花金币；暂留免费。后备／部署接收不合法先整备，无静默丢弃英雄 |
| `exp_shop_fixed` | 当前物品池最多 3 件不同库存，设备可与已拥有设备同 ID，唯一遗物不得重复取得 | 用 ItemDefinition.Price 冻结原价；首次成功购买 1 行动，其余按 §5.2 |
| `exp_camp` | 恢复、军费两项固定 choice | 1 行动；恢复 25 全局生命或得 8 金币，满血只可领军费 |
| `exp_ruin` | 安全、冒险两项固定 choice | 1 行动；安全得 6 金币，冒险 65% 得 18，否则 0；风险结果固定但处理前不暴露答案 |

物品池引用当前资源，不把本轮观察到的 6 equipment／33 item 写死成数组上限。候选不足时保留实际数量，不复制、不重抽、不补货；整个组失效的处理见 §5.4。全图普通奖励候选、商品与招募在生成时固定，战斗失败重试不重新生成奖励。成长、计数遗物等结算时才产生的额外收益由各自明确来源与 ActionId 派生，不与地点基础库存混为一份。

### 3.4 完整配置发布与会话选择

`CompiledGamePackage` 改为共享 `ContentRegistry`、`ProfilesById`、`DefaultNewRunProfileId` 与同一 PublicationVersion。`CompiledRunProfile` 含自身 ID／兼容版本／稳定内容指纹、CompiledGameProject（Campaign、RunRules、表现绑定）、CompiledGrowthRules 及进程／奖励／身份协议版本；每个 profile 只有一个进程变体，不是 Campaign 同时装两套进程。运行发布序号只用于当前进程观察，存档存稳定指纹，不拿重启后 PublicationVersion 匹配存档。

- 成长包注册 `growth_expedition_v1` 为新局默认，以及 `growth_tower_legacy_v10`。后者保留迁移前的完整 Tower、RunRules、分阶供给、默认 offer、成长与身份规则，不能只留五层塔表。相同定义可共享不可变引用；未来改动会影响旧局语义的定义必须版本化，不能原地覆盖 legacy 引用。Alpha 包维持自身 profile／命名空间。
- 发布前联合收集所有 profile 的正式依赖、旧 Boss timeline 和成长 loadout；全部通过才能发布。当前单 Project／GrowthRules 发布接口随此修改，不能以运行中切换 GD.Load 绕过原子门禁。
- 启动先只读保存 envelope（命名空间、schema、ProfileId、指纹），不迁移、不写 Meta／Run。没有 ProfileId 的已支持 v2–v10，按入口提供的固定命名空间映射：`growth-journey → growth_tower_legacy_v10`，Alpha 命名空间→Alpha legacy profile；测试命名空间由 fixture 显式传映射。再选完整 profile，执行该版本既有拒绝条件和迁移，不能因版本在范围内就接受非法结构。未知 schema／ID／指纹不降级成默认；拒绝并保留文件。
- 组合根通过 `RunSessionFactory` 为选中 profile 建立唯一服务会话，再读完整 DTO、迁移和验证，迁移成功写回后发布；原来 `RunApplication` 先绑定唯一 project 再 LoadActiveRun 的顺序必须调整。新 schema 同时写 ProfileId、协议版本和指纹。
- 继续旧局后点新征程，factory 用默认 Expedition profile 创建候选会话与草稿。候选校验／保存成功才替换整个当前会话、解绑旧 UI 回调；失败仍保留原会话。不能只换 DTO 却把 services 留在旧 profile。Meta／Settings 由共享存储协调，不因候选会话初始化而先写默认值；本轮不新建第二活动存档槽。

## 4. 身份、状态所有权与模块职责

### 4.1 身份不能复用楼层

| 身份／计数 | 唯一用途 |
|---|---|
| `RunId / Seed / ContentRevision / GenerationVersion` | 局身份、稳定随机与内容兼容 |
| `StageId / StageIndex / RemainingActions` | 阶段、Boss 与预算；不控制旧怪强度或库存重抽 |
| `CellId / SiteId` | 地理及地点身份；回访保持 |
| `ActionId / ActionSequence` | 一次正式处理或战斗尝试；重复请求／恢复沿用同 ID |
| `BattleNumber / BattleIdentity` | 已正式结算的战斗次数与本次战果归属；冻结快照沿用起战前计数，见 §5.4 |
| `OfferId / ChoiceId / RewardGroupId / TransactionId` | 固定内容、购买或领奖及幂等；同一商店不同商品不是同一交易 |
| `RunRevision` | 拒绝过期预览／重复输入；不是随机种子 |

`ActiveRunDto` 的新 schema（计划 v11，实施时核对当前版本）含显式进程种类及互斥 `TowerState`／`ExpeditionState`。不能同时保留两套可写的当前位置或待战标志。`ExpeditionState` 只保存生成地图、当前位置／可见格、地点状态／库存、阶段／预算。公共 roster、装备、物品、全局生命、成长仍归现有 Run 聚合。两种进程共用 `ActiveRun.Action`（`PendingRunAction`）保存当前不可逆处理，一次最多一项；地图不再另存 action、副本队列或完成账本。RunId 在新局草稿首次建立时生成并保存，不等同可重复使用的随机种子。

唯一序列化归属如下；服务只执行这些字段的工作副本操作，视图只做计算投影：

| 事实 | 新 schema 的唯一存储 | 旧字段迁移／使用约束 |
|---|---|---|
| 当前层、待选旧敌、旧兼容标志 | `TowerState` | 旧 FloorIndex／SelectedNode／SelectedOpponentOptionId／LegacyPendingOpponent；只适用于 Tower |
| 动作来源、开始阶段、阶段生命周期 | `Action.Source / Phase / StageAtStart` | 旧 PendingNode 转成对应 action；地图不保留 PendingNode bool；无 action 的 BossReady 由预算与无待办推导 |
| 战斗来源、配置身份、开战金币、入场后 roster／装备／遗物／法术输入 | `Action.BattleFreeze` | 入场与消费投影后生成最终快照，BattlePreparation 只消费这一份；法术消费记录留在 `Action.GrowthSnapshot`，freeze 只引用该记录 ID |
| 成长参与者、生产分配、已消费法术 | `Action.GrowthSnapshot` | `Growth.PendingNode` 迁入并移除旧写入路径；Tower 快照保留 LegacyFloor 来源语义 |
| 当前奖励与剩余队列 | `Action.Rewards.Groups + Cursor` | 旧 PendingOffer 和 PendingRewardGroups 合并成有序队列；CurrentOffer 是 cursor 投影，不再序列化一份重复 offer |
| 旧塔非战斗待办 | `Action.ServiceOffer`，与 rewards 队列互斥 | 兼容旧已进入服务；新地图查看商店只读 Site，不创建 action／PendingOffer |
| 已支付但待选的升阶发现 | `Growth.PendingDiscovery` | 保留唯一位置，附独立 TransactionId 和 StageAtPurchase／固定候选；不复制进 Action；不能与未完成 action 同时新建 |
| 生产／永久收益历史 | `Growth.History`，每条引用 ActionSource | 当前 action 的结算指针及历史去重按版本化来源；旧 LastSettledFloorIndex 迁成 Tower source watermark，不当新地图序号 |
| 已完成动作与交易收据、序列 | `ActiveRun.CompletedActions / Transactions / NextActionSequence` | 存本局有限付费动作、购买和领奖结果／请求摘要；不与 Growth.History 重复存授予值。相同 ID 不同请求拒绝 |

新地图一次性非战斗操作可在同次提交中建立、结算、清除 action 并写完成收据；不是为了统一状态强制持久化中间半成品。旧字段仅作为旧 schema 的反序列化输入，迁移成功清空；新格式拒绝旧／新双份状态。全字段 clone、copy、验证及失败回滚必须随归属表更新。

既有 `PendingRunOffer` 改为持有 `OfferSource`：旧塔来源包含旧层数／战斗号，新地图来源包含 SiteId／ActionId／组号和库存版本。校验按来源契约，不再比较新地图当前楼层。旧字段仅由显式迁移读取，不设用于通过验证的占位 0。

`BattleIdentity`、`RelicRunKey` 接收不可变 `BattleOriginKey`：局身份、ActionId、BattleNumber、具体遭遇／组成／战场与内容指纹。旧塔适配器保留旧哈希版本和旧身份字段来恢复旧冻结战斗；新地图有新的身份版本。战斗只认识不可变来源标识，不引用地图 DTO 或服务。实验室提供它自己的来源，不要求伪造地图 ActionId。

### 4.2 职责分配

| 模块 | 拥有 | 禁止 |
|---|---|---|
| `ExpeditionMapGenerator` | 纯输入生成候选与验证报告 | 存盘、读当前阵容来加强敌人、创建 Node |
| `ExpeditionNavigationService` | 可见／通行查询、合法步进、地点可达性 | 扣行动、发奖、触发成长 |
| `ExpeditionStagePolicy` | Boss 到期、待战锁、提前迎战和下一阶段投影 | UI 回调计时、直接发布半份 Run |
| `RunActionCoordinator` | 一次处理的校验、冻结、组合副作用和唯一提交，两种进程共用 | 复制战斗算法或每类地点另写授予逻辑 |
| `RunChoiceEvaluator`（从 Decision 拆） | 在工作副本上校验／执行既有 operation、生成变化 | 内部 Save、自动 FloorIndex++、导航 |
| `RunDecisionService` | 选择编排、来源校验、调用 evaluator；塔结束流程交给其进程策略 | 打开界面就提交机会，商品重访重抽 |
| `GrowthRunService` | 参与者冻结、成长／升阶／法术与永久收益 | `new TowerGenerator(...)` 验证地图战斗、以层数唯一结算 |
| `RunBattlePreparationService` | 来源明确的完整战斗计划到既有 assembler | 读取地图行走步数做敌军倍率、另写地图专用模拟 |
| `RunProgressionPersistenceService` | 整个 Run 校验、深复制、保存成功再发布、终局协议 | 把策略／内容查找／界面导航揉进去 |
| `RunApplication`／GameFlow | 用例门面、只读投影与页面编排 | 控制器写 DTO、模拟发奖 |

进程差异仅在组合根选择的 `IRunProgressionPolicy` 中处理（Tower / Expedition）；共有战斗／操作／成长／存储由统一组件消费明确上下文。接口至少提供继续目的地、当前遭遇来源、有效处理前置、普通结果后的进程投影、终战事实与元进度事实。命令需要的地图查询只在 Expedition 服务；不得把两个模式所有 API 堆进一个全可空超级接口。

现有 `RunNodeResolutionService.CompleteBattle` 拆为共有的 `RunBattleResultProjector`（战果验签、遗物转移、金币／生命／永久收益／成长／奖励投影，无内部保存）和 Tower 进程策略（旧楼层／旧节点语义）。`RunActionCoordinator` 唯一调用 projector 后保存；旧 service 的原提交路径随迁移移除，不保留两套战果处理。塔的 Options／SelectNode 归 Tower 用例服务。`RunApplication` 保留用例门面，UI 不直连 projector。

`RunEditPolicy` 统一给所有阵容、装备、成长、法术、领取与导航命令判定权限，不能靠页面是否打开：Exploring／未提交预览允许整备；BattleCommitted 至战果保存期间全部锁定；ClaimingRewards 仅允许当前组领取／放弃及其必要接收动作；PendingDiscovery 仅允许原发现选择；BossReady 允许已有资源整备但禁止地图获取。阶段领奖期间不新开升阶发现，先领完再阶段转换。原 `Growth.PendingNode`／界面 bool 锁替换为此领域判定；表现层的额外忙碌锁只能更严，不能解开已冻结输入。

## 5. 支付、冻结、结算：同一事务链

`Preview` 只给只读事实与报价；`Commit` 先查 TransactionId 和请求摘要是否已有成功收据，再使用 RunRevision、内容身份和实际状态重新校验未处理请求。所有工作投影经同一个 `TryPublish` 保存成功再可见；保存失败不发布副作用。重复成功命令返回同一结果／已完成信息，不再次发货；相同 ID 换 payload 拒绝。移动也不直接写公开视图，其旧 RunRevision 重放拒绝，不需要保存无限行走收据。

### 5.1 普通／精英开战

确认时按顺序：验证探索状态、已知且相邻、敌人未清及作者配置／阵型前置；复制 Run；分配 ActionId，捕获**入场触发来源和成长参与资格**；扣 1 行动；应用入场遗物；记录成长生产快照并消费已选法术；从该最终工作副本构建完整 `BattleFreeze` 和真实配置，进行最终完整校验；一次保存 PendingBattle；发布后启动模拟。

前面的资格捕获不是最终战斗数值冻结。`BattleFreeze` 必须包含入场后的 Gold、已生效全局／英雄属性、装备与遗物状态、真实已消费法术及目标、敌阵与战场、种子、起战前 BattleNumber、最终来源指纹；快照由现有 assembler 所需输入定义。RL02 入场加金币之后的值必须同时用于 RL09 金币转生命、战内可花金币、GoldSpent 上限和战果利息基准，不能一处读快照、一处读入场前金币。结果验签与扣除使用同一冻结基准，领域编辑锁保证直到战果接受前该基准不被外部变更。预览在只读克隆上运行同序投影，展示一致的正式开战预期，但不发布、不实际扣费。

`TryBeginGrowthBattle` 不再自己先保存一份成长状态；冻结改为投影操作，由外层一次提交。任何最终配置／保存失败都丢弃整份副本，可免费修正；发布成功但场景启动失败只能恢复原冻结战斗，不再次扣费或更换配方。阶段 Boss 使用同一顺序，只把探索行动费用配置为 0。

消费最后一点时状态是 `ResolvingAction`，不是立即 BossReady。先结算当前战果、永久收益、成长和待领奖队列；全部完成后检查终局／Boss。失败有生命则敌人保留、当前位置不变，既有失败成长可按一次正式尝试结算，不能发胜利奖励；重试新的 ActionId 且再付行动。

### 5.2 非战斗与商店

报价在克隆上模拟第一次有效入场遗物与折扣，再校验费用／资格和全部操作；用于告诉玩家最终可支付金额，不能发布预览收益。确认时重新计算。事务顺序：冻结已有遗物来源与原参与者 → 首次有效处理的入场效果 → 行动及金币等费用 → 购买触发 → 成功或失败 operation → 库存／完成状态 → 一次成长 → 阶段投影 → 保存。

新招募／新遗物不追溯参与它被授予之前的入场或本次生产。商店第一次成功交易花 1 行动并结算一次成长，以 SiteId 保存资格；后续商品各有 TransactionId，只执行真实购买费用／效果，不触发第二次入场和成长。离店不推进任何东西。第一次购物耗尽预算时只交付当前商品，立即进入最后一次结算／Boss 准备，不把整店余货视为同次免费处理。

一次性事件随机结果由地点、选项及生成随机流固定，回访／存读不重抽；试图反复点概率选项会被已完成身份拒绝。无效／满血服务在报价阶段禁用；如果有入场效果改变资格，用同一投影顺序算出实际合法性。

### 5.3 成长与遗物对应表

| 当前触发 | 新进程绑定 | 保留／修正 |
|---|---|---|
| 工坊生产、MaterialsPerNode | 已提交付行动处理的非终局完成；每 ActionId 一次 | 冻结参与者在授予新英雄前；失败可续行仍生产；空看不生产 |
| 非终局 Boss 成长 | 阶段动作完成一次 | 阶段配方不重复加同份 MaterialsPerNode；最终 Boss 不发可复用成长 |
| `LastSettledFloorIndex`、成长 History | 新进程以 ActionId/Sequence 约束 | gain、settlement、pending 和校验器全部迁移；不只改计数器字段 |
| 升阶发现池 | 当前阶段显式 DiscoveryProfile | 内容合法、三个不同未拥有者及扣料事务保持；待发现锁定下一处理 |
| 战前法术 | 同一次开战提交 | 准备可取消，冻结后一份只消费一次；同次恢复不换目标 |
| MX01 等战内永久收益 | 验签后的同一 BattleOriginKey | 只接实际允许的持久增量，不能另写地图数值回填 |
| `NodeGoldUntilPurchase` | 每个新有效动作（含 Boss）入场；同店后续购买不入场 | 入场金币可在同次报价中用于买货；购买再停用该遗物，顺序一致 |
| `ShopHealing` | 该店第一次成功处理的入场 | 查看／回访没有免费治疗；保持每个付行动商店机会一次的效果 |
| 商店折扣／购买后停止收益 | 每笔真实报价／付费交易 | 原价冻结，折扣按当前遗物只读计算，不多次乘折扣 |
| 跳过装备金币／放弃奖励加生命 | 实际奖励组显式放弃 | 离开商店／暂留敌人不是跳过战利品；领取和放弃只能一次 |
| 精英额外奖励／计数遗物奖励 | 原来源和实际胜败／CounterTransitions | 同一次战果内既有规则执行；新奖励遗物不奖励自身取得 |
| 计战利息 | 实际 BattleNumber | 不以走格／预览或补领增加计战次数 |

遗物 `ShopHealing` 等文字要与有效处理语义同步，不能让玩家看到“进入商店回血”却只在购买时触发而不说明。旧塔存档仍用旧触发协议，内容显示经进程规则投影，不能全局改坏 Alpha。

### 5.4 计战时点、过期内容与空组

`BattleNumber` 保留现有的**已结算次数**。起战前为 b，本次冻结身份记录 b（展示第 b+1 场），法术与遗物快照都引用这份身份；确认／启动失败／恢复不递增。胜利、失败或超时被接受后，仅 projector 在同次战果事务写成 b+1，随后计战遗物按该值判断；重复结果不得再加一。存钱罐 Limit=8 因此第 8 场仍可结利息，结算后不得再取得一个已经没有未来效果的存钱罐。普通败战后重试及阶段 Boss 各是一场；非战斗、领奖和走格不计战。旧 Tower 的来源 hash／种子运算保持，不能因为显示第几场而把旧输入统一加一。

固定候选指身份与基础内容不变，取得资格每次投影和提交仍按当前状态检验。已经拥有的唯一遗物、已经入队的招募身份或失去未来效果的物品显示具体原因并禁用；暂时金币不足／后备满显示可恢复的条件，不抹掉库存、不补抽。只读显示禁止隐含授予或保存。无任何合法服务选项时不消耗行动、不触发遗物或生产，玩家仍可回头或提前迎战 Boss。

地点基础奖励的候选虽在生成时固定，战胜时仍重新判定资格。按有序组依次领取，上一组可能改变下一组合法性；全组已无可取得内容时，由处理命令投影为 `Exhausted` 并推进 cursor，同次保存。**Exhausted 不是主动 Skip**，不触发放弃奖励加生命／装备跳过金币。只有当前至少一个可领取选项时，显式放弃才触发现有跳过收益一次。不因空组生成金币补偿、不重抽、不复用已领取组。当前 Tower 的历史 offer 继续按其版本协议，不能把新空组语义偷偷反写到旧局。

## 6. 阶段与终局恢复

阶段流程为 `Exploring → ResolvingAction → ClaimingRewards → BossReady → BossBattleCommitted → StageRewards → Exploring/Terminal`，无待领奖则可直接跳过 ClaimingRewards。预览是 UI 状态，不是已经提交的业务状态。所有 Boss 在阶段开始即预告；BossReady 禁止导航获取新内容和全部地图交易，但允许对已有英雄／材料／法术整备，已有付费发现先领完。提前迎战仅 Exploring 且无待处理时提交，剩余资源置零且不视为有效成长动作。

Boss 胜利：战果与永久收益验签、非终局一次成长、阶段奖励队列冻结同次保存；领完阶段奖励后才 StageIndex+1、替换 RemainingActions，保存一次。阶段奖励为零也走明确完成步骤。失败／超时或普通战耗尽全局生命：沿 TerminalCompletionId／Meta AppliedRunCompletionIds 既有两文件终局协议重试，不重奖／重计胜利。最高地区元进度改由当前**已到达阶段**提供（初始为 1，下一阶段正式提交后增加），不是仅计打赢的 Boss；保留既有 high-water mark 含义，不再 FloorIndex/5。Meta 同步失败不把已成功 Run 事务报成失败，恢复时重试高水位。

终局清理须补齐这一恢复保证：终局 Run 先持久化 `TerminalCompletionId` 和 `ReachedStage`，删除 active_run 前确认 Meta.HighestRegion 至少达到该值；未达到则与本次胜利计数／解锁（如有）合为一次 Meta 保存。普通败局不增加胜利或解锁，但同样须写成功高水位才删除终局 Run。保存失败保留终局重试；重复执行采用 max 和既有 completion id 去重。不能继续沿用“败局直接删除活动档”却承诺以后还能恢复高水位。游玩中的副记录失败仍不回滚已成功的 Run。

继续的优先顺序：终局待完成 → 未确认开局 → 已冻结普通／Boss 战 → 待奖励／付费发现 → BossReady → 地图。恢复同次战斗是按原冻结参数重启，不宣称逐帧战斗保存；原场景未成功启动也适用。保存失败、未来版本／未知内容／非法地图必须拒绝并保留文件，不能重新生成伪装成继续成功。

旧存档策略：使用现有 `growth-journey` 存档空间，新增进程种类；无配置 ID 的旧档先按 §3.4 的命名空间／schema 映射选定完整 legacy profile，再迁移、保留当前层、选择、成长历史和奖励，不补生成地图。迁移为 TowerState 后由 Tower 策略继续，默认新局才采用 Expedition。旧 Alpha 空间保持。写盘时保留既有跨版本备份，成功后才发布；禁止真实玩家文件做测试。旧版本无法证明奖励是否已经领取等原有拒绝条件继续执行，不扩大成“所有旧档保证可恢复”。

## 7. 界面与代码接入清单

新增预制 `ExpeditionScreen`，组合地图视口、格子层、迷雾层、队伍位置、选中详情、阶段栏与已知目标。格子实例来自一个预制场景，图标／地貌由定义绑定；不在代码里生成整套控制树。棋盘负责展示和输入，合法路径由服务给出。`RunView` 新增只读 Expedition 视图，公开已知投影；控件不能持有可变 DTO 或完整未知内容表。英雄／装备／法术界面复用正式入口。

`GameFlowCoordinator` 的 ShowTower/Continue/Back 改为读取恢复目的地，不用“战斗胜利 bool”猜页面。`AppScreenHost/ScreenRouter` 显式持有地图和旧 Tower 页面，兼容恢复才显示旧页。`RunBattleFlow._pendingFinalBoss` 改读具体阶段终战事实。`DeploymentScreen` 返回是取消未提交整备，不再依赖已选择即 PendingNode 的旧假设。战报继续由已保存状态导航。

地图外观沿用全局背景与已认可灰石信息框；地图地貌单独表达空间，不把背景庭院误作可通行地图。天气、云影、光线只消费展示配方，不拥有迷雾或资源。界面须显示剩余行动、Boss 预告、当前地点代价、不可走原因、固定库存与已完成状态；用文字／形状补颜色。地图控制与战斗镜头、战斗部署坐标隔离。

需要修改的现有位置（实施拆分清单，不表示已改）：

- Project / Content：Campaign / CompiledProjectModels / GameProjectCompiler.*、项目引用收集与包发布；新增作者地图／地点／阶段定义及验证。
- Run：RunModels / RunViews / FormationContracts（ActiveRunFormationSchema）/ ActiveRunConfigurationValidator / RunProgressionPersistenceService 的复制、迁移、恢复；新增地图与阶段服务、动作协调；Decision 拆操作执行器；NodeResolution 拆共有战果投影与 Tower 策略；Rewards / Health / Population 显式消费来源上下文。
- Growth：GrowthModels / GrowthRunService / GrowthRulesCompiler 与 PendingDiscovery 资格；所有 floor 历史读写改用版本化动作来源，保留旧历史可读。
- Battle 边界：BattleIdentity / RelicRunKey、RunBattlePreparationService / Adapter 与 Transition 验签；不增加地图依赖到 BattleSimulation / FloorRule / 单位内容。
- App / UI：GameFlowCoordinator / RunBattleFlow / AppScreenHost / ScreenRouter、Deployment 返回、选择页回访与地图预制组件。
- 作者数据：显式地图角色配方与共用引用；不复制英雄、Boss 或物品场景形成第二套平行内容，不把 66 项羁绊候选顺手实装。

## 8. 实施顺序与验收门槛

用户后续确认五阶段推进，并要求阶段内避免重验证、最后集中走流程。前四阶段做必要编译、配置／规则和关键风险的定向检查，优先复用已有证据；下列检查清单分配到对应改动，不能每阶段全量重复执行。存档迁移、幂等支付／领奖、战果验签与冻结等高风险边界仍即时验证；整合流程、正常速度录制和体验校准集中在第五阶段。只有失败、新风险或明确证据缺口才扩大复验，不默认增加多分辨率遍历、宽泛回归或再次整套独立审查。

1. 首先做类型化来源与进程配置拆分，旧 Tower 行为保持；迁移／验签／发布边界契约通过后进入地图。
2. 实现纯地图生成与状态、导航和只读投影；多种子确定性、环路、守关、起点与配额验证。
3. 接通一次处理、战斗／非战斗／奖励／成长／遗物、阶段与保存；按风险覆盖已知反例，复用已有契约检查，避免为每次局部接入新增一套重复测试。
4. 接预制地图与真实输入，最后切换默认新局；旧局仍可继续。不要先把菜单改名“地图”再塞临时逻辑。
5. 在隔离 960×540 用正式内容与真实输入集中走一遍代表流程：新局 → 发现与绕行／受阻 → 地点处理与成长 → 最后一行动 → 领奖 → Boss 准备与阶段继续 → 终局，并检查存档恢复。取消部署、胜利开路、败战保留、商店回访等分支通过相关定向检查补足，不为逐个分支重走整局。正常速度录制代表操作；不直接触发信号冒充点击，不把这一遍流程称为所有随机种子／构筑的平衡验收。

重点自动检查：同输入（种子、开局选择、生成版本、profile）地图相同／不同种子变化；全状态克隆与恢复；非法位移、跨墙或隔雾寻路拒绝；所有有效处理支付一次；普通失败／重试；地点确定性；商店入场金币／治疗与首次购买的时序；遗物折扣、唯一性、主动跳过与 Exhausted 区分；第 7／8／9 场计数与存钱罐资格；生产参与者；法术冻结恢复；动作与战果串单拒绝；阶段末当前奖励完整；Boss 奖励和资源补充一次；无 ID 旧配置解析及切换新局；发布重复资源与缺失引用拒绝；保存失败回滚和新草稿／选队生成失败边界。

平衡验证单列：当前真实供给下每阶段金币、人数、装备、材料／研究与选择压力；6/6/6 的额外成长量及现有 Boss 检验；26 个分阶身份和 7／9 个发现池的互相排除及三个候选自然耗尽；开局组合是否能遇到可靠的前排、输出及对应敌人抓手；免费探索是否过早看完所有收益。可以调整本配置数字，不能修改回滚／身份语义换取通关。审查设计通过不等于以上运行、平衡或观感已通过。

## 9. 审查清单与当前证据

请独立审查：每个新字段是否有唯一拥有者、每个事务有完整失败／恢复路径、当前资源是否真的能提供列出的内容、供给／人口／成长／遗物是否逐项接上、FloorIndex 是否还有新地图依赖、旧档配置是否完整保留、地图是否可重复刷收益、UI 是否越过只读边界。问题必须给设计段落和当前源码证据，标明设计阻断、可实施细节或平衡风险。

本轮只是代码与作者资源静态核对及文档设计，尚未实施、构建或运行。审查报告另存同目录 `expedition-design-review.md`，修订记录应说明处理了哪些发现和仍有什么限制。

初审修订记录：ER01 补 §3.4 完整 profile 与无 ID 旧档解析／会话切换；ER02 统一 §2.1 和 Q11 的新草稿替换时点；ER03 补 §4 唯一存储迁移表、共有 projector 与编辑权限；ER04 补 §3.3 StableId 配方索引、有序组与实际首版绑定；ER05 补 §5.4 已结算计数、失效库存与空组；ER06 补 §3.1 最终倍率和共用编译边界，移除未定义覆盖标签／PressureBand，职责可行性转入显式内容诊断。修订仍需 reviewer 复审，不能以主 Agent 自评关闭问题。

第一轮复审追加修订：ER07 修正 Boss 必须使用各自原 Encounter 的专属战场池；ER08 明确入场／法术消费后再冻结完整战斗输入，含金币与最终指纹。Meta 提醒补为终局删除前持久化已到达阶段，失败保留终局重试。等待最后定点复核。

最终状态：reviewer 定点复核已确认 ER01–ER08 及 Meta 提醒关闭，已审范围无剩余 P1／P2 设计阻断，详见[报告末节](expedition-design-review.md#最终定点复核2026-10-02)。上面的等待表述保留各次修订历史；后续按 §8 实施，仍须完成真实运行和体验验证。
