# 通用遗物参考库与可配置内容

> 后续状态（2026-10-01）：用户随后明确“直接接入”，再限定先逻辑、后确认效果风格、暂不制作图标。RL01–RL24 对应规则及击杀装备示例现已接入；原作事实与早期候选判断保留在下文，当前可执行参数以[效果清单](../../../../gameplay-design/generic-relics.md)和[活动任务](../../../../work-items/active/generic-relic-integration.md)为准。25 项逻辑中 24 项进入成长供给，专门入场能力重复暂不供给；数值与图标未获体验验收。


2026-10-01。按用户要求，遗物以通用效果为主体，优先借鉴成熟游戏的完整机制，尽量保留其有趣之处，只做项目需要的适配；不用当前英雄名单或已经实现的少量效果接口限制设计。用户进一步明确：效果已设计清楚时可以先完整制作，奖励对象等可变内容通过可配置池子连接，初始测试池可以只有一件已有装备。

本页首批核对 4 款游戏的 24 项物品／遗物机制，是持续扩充的参考库，**不是完整市场汇总，也不是已采用或已接入名单**。Brotato 与 Risk of Rain 2 的原载体是物品，迁为本项目全局遗物属于项目适配；原作名称仅用于追溯来源。机制尽量原样保留，参数、作用范围和取得时点按本项目核算。

## 设计顺序修正

先收集足够丰富的通用机制，再让单位、装备、羁绊与遗物形成组合。通用不要求每种阵容拿到后的价值完全相等：暴击治疗可以偏爱高暴击单位，但不需要写死某名英雄；获得金币时回血可以改变运营取舍，完全不依赖英雄技能。

遗物也可以提供新能力、改变普通规则或使另一件遗物增值，不只被动奖励某个已经完成的流派。保留简单而有分量的属性效果，同时避免用多件几乎相同的小数值加成填充主体。

本次修正此前“配套不完整就把整个效果判为未完成”的过宽标准。应分别判断：效果是否已经设计清楚、系统链路是否正确、池子是否可替换、正式内容是否足够丰富。效果完整＋单件测试池，是有效的开发状态；没有想清楚效果却填一个通用小盾，则仍应留白。

## 参考机制

“适配”是 Agent 判断，不代表已采用。“轻适配”只说明设计语义接近，不保证现有代码已支持。每行保留原作机制与本项目处理的区别，不照搬原作数值。

### 征程与经济

| 编号 | 原作与来源 | 原作核心效果 | 项目适配 |
| --- | --- | --- | --- |
| RL01 | Slay the Spire 1 [Membership Card](https://slay-the-spire.fandom.com/wiki/Membership_Card) | 商店商品半价 | 轻适配。作用于购买价格，给存钱与逛店新价值；折扣参数另核算。 |
| RL02 | Slay the Spire 1 [Maw Bank](https://slay-the-spire.fandom.com/wiki/Maw_Bank) | 每到一层获得金币，首次在商店花钱后失效 | 轻适配。保留“攒收益到何时花掉”的主动取舍；与正在重设计的进程结算点连接。 |
| RL03 | Slay the Spire 1 [Meal Ticket](https://slaythespire.wiki.gg/wiki/The_Merchant) | 进入商店回复生命 | 轻适配。回复征程生命而非所有英雄；同一次进入不能靠反复开面板重复触发。 |
| RL04 | Slay the Spire 1 [Bloody Idol](https://slaythespire.wiki.gg/wiki/Forgotten_Altar) | 获得金币时回复生命 | 轻适配。把经济收益接到征程续航，不依赖阵容。原作是事件遗物；本项目取得渠道另定。 |
| RL05 | Slay the Spire 1 [Black Star](https://slaythespire.wiki.gg/wiki/Relics_List?rarity=Boss) | 击败精英额外获得一件遗物 | 轻适配。把额外遗物指向可配置池，而非写死某件或复制当前全部供给。 |
| RL06 | Slay the Spire 1 [Preserved Insect](https://slaythespire.wiki.gg/wiki/Relics_List?rarity=Event) | 精英战中的敌人以更少生命进入战斗 | 轻适配。保留“更愿意挑战精英”的用途；明确影响初始敌人及后续召唤的边界。 |
| RL07 | Slay the Spire 1 [Singing Bowl](https://slaythespire.wiki.gg/wiki/Singing_Bowl) | 卡牌奖励可以换成最大生命提升 | 替换奖励载体后适配。可作为某类奖励的特殊放弃收益；不把它扩为所有人默认有的跳过补偿。 |
| RL08 | Brotato [Piggy Bank](https://brotato.wiki.spellsandguns.com/Piggy_Bank) | 按保留材料取得利息，有生效阶段限制 | 把原作通用消费货币对应到金币，保留存钱与即时购买之间的取舍，不误用本项目升阶材料。 |
| RL09 | Brotato [Padding](https://brotato.wiki.spellsandguns.com/Padding) | 当前持有材料越多，最大生命越高；花掉材料后相应收益消失 | 轻适配。金币暂时转成战斗耐久；是随库存变化的增益，不写成永久成长。 |
| RL10 | Brotato [Recycling Machine](https://brotato.wiki.spellsandguns.com/Recycling_Machine) | 提高回收武器或未领取物品的返还 | 需要回收渠道。可作为特殊遗物开放的交易接口候选，不能假装本项目已有普通出售权限，也不因接口未做而从参考库删除。 |

### 战斗与跨遗物组合

| 编号 | 原作与来源 | 原作核心效果 | 项目适配 |
| --- | --- | --- | --- |
| RL11 | Risk of Rain 2 [Crowbar](https://riskofrain2.wiki.gg/wiki/Early_Access_%27Skills_2.0%27_Content_Update) | 对高生命比例目标造成额外伤害 | 轻适配。形成开场爆发与后续收割的差异；按受击前血量判定。 |
| RL12 | Risk of Rain 2 [Focus Crystal](https://riskofrain2.wiki.gg/wiki/Focus_Crystal) | 对近距离敌人的伤害增加 | 轻适配。按攻击者与目标的实际距离，换成本项目空间单位。 |
| RL13 | Risk of Rain 2 [Armor-Piercing Rounds](https://riskofrain2.wiki.gg/wiki/Armor-Piercing_Rounds) | 对特定 Boss 敌人额外伤害 | 轻适配。明确本项目 Boss 身份范围，保留针对终局的选择价值。 |
| RL14 | Risk of Rain 2 [Gasoline](https://riskofrain2.wiki.gg/wiki/Module%3AItems/Data) | 击杀引燃附近敌人 | 轻适配。补给单体阵容清群能力，也能接其他燃烧效果；半径及归属另配。 |
| RL15 | Risk of Rain 2 [Harvester’s Scythe](https://riskofrain2.wiki.gg/wiki/Harvester%27s_Scythe) | 增加少量暴击率，暴击时治疗自己 | 轻适配。先决定全队成员各自获得该规则，或限定作用对象；不绑定英雄 ID。 |
| RL16 | Risk of Rain 2 [Aegis](https://riskofrain2.wiki.gg/wiki/Aegis) | 溢出治疗转成临时防护 | 轻适配。接本项目明确的临时护盾规则；治疗来源可以是英雄、装备或另一件遗物。 |
| RL17 | Risk of Rain 2 [Shattering Justice](https://riskofrain2.wiki.gg/wiki/Shattering_Justice) | 多次命中后削减目标护甲 | 轻适配。明确计数按攻击者还是全队共享；由命中事件驱动，不能把持续伤害每跳偷算为普攻。 |
| RL18 | Risk of Rain 2 [Death Mark](https://riskofrain2.wiki.gg/wiki/Death_Mark) | 多种不同减益齐备时提高目标承伤 | 轻适配。鼓励不同状态来源组合；数种减益与同一种的多层必须分清。 |
| RL19 | Risk of Rain 2 [Topaz Brooch](https://riskofrain2.wiki.gg/wiki/Early_Access_%27Skills_2.0%27_Content_Update) | 击杀获得临时防护 | 轻适配。提供收割续航，与开战送固定护盾有不同节奏。 |
| RL20 | Risk of Rain 2 [Predatory Instincts](https://riskofrain2.wiki.gg/wiki/Module%3AItems/Data) | 暴击增加可叠加但有上限的攻速 | 轻适配。保留触发、积累及上限；和暴击回血、命中破甲形成跨遗物联系。 |
| RL21 | Brotato [Retromation’s Hoodie](https://brotato.wiki.spellsandguns.com/Retromation%27s_Hoodie) | 闪避属性同时提供攻速，代价是射程下降 | 轻适配。保留属性转化与代价，明确近战／远程射程变化，不只抄正面增益。 |
| RL22 | Brotato [Riposte](https://brotato.wiki.spellsandguns.com/Riposte) | 成功闪避敌人攻击时对其造成反击伤害，附带近战伤害加成 | 轻适配。使用自动战斗中的闪避判定，完全不需要手动躲技能。 |
| RL23 | Monster Train [Ashes of the Fallen](https://monster-train.fandom.com/wiki/Ashes_of_the_Fallen) | Summon 入场能力额外触发一次 | 需要明确公共入场能力语义。不是“所有召唤技能额外造一个单位”，也不能把任意开战动作全部翻倍。 |
| RL24 | Monster Train [Waxer Snuffer](https://monster-train.fandom.com/wiki/Artifacts) | Extinguish 死亡能力额外触发一次 | 需要明确公共死亡能力语义。可供所有具有这类能力的单位使用；不要求绑定某名英雄或某一套羁绊。 |

这些设计已有形成组合的基础。例如，RL15 暴击治疗接 RL16 溢出治疗防护，RL20 又提高暴击路线的攻速价值；RL02 的逐步金币收益接 RL04 获金回血；RL06 降低精英战压力，RL05 提高精英战回报。这是项目组合推演，不声称整组原数值可以无条件移植到多人阵容。

## 效果完整与内容池独立

用户示例是“击杀 n 个单位后，战斗结束获得一个装备”。它说明可稳定实现的规则与可变供给应拆开，未指定 N、计数是否跨战、己方损失是否算击杀、达标后是否重复发奖等具体数值／规则。本轮不擅自把示例定为已采用的具名遗物。

| 部分 | 独立职责 | 初始测试做法 |
| --- | --- | --- |
| 事件与计数 | 接受符合条件的击杀事实、记录进度、判断是否达标 | 对明确的计数口径造真实战斗验证，不由某名英雄脚本直接发装备 |
| 奖励资格 | 达标后记录待兑现次数，不在战斗中改背包 | 单次或可重复领取按具体遗物配置；进度与待领次数是局内状态 |
| 战后结算 | 消费待兑现资格，从指定奖励池生成装备 | 复用正式结算和库存路径；发奖次数不由动画决定 |
| 装备池 | 列出可产生装备及必要抽取规则 | 可以只配现有 `equipment_field_focus`，不要求先设计新装备 |
| 展示 | 读取计数、阈值、待兑现与实际所得 | 后续换池不重写遗物效果或界面结算逻辑 |

配置关系示意，**不是声称这些字段已存在的运行资源**：

```text
击杀达标遗物
  计数条件与阈值 N：由该遗物规则配置
  兑现时点：战斗结束
  奖励数量：1 件装备
  装备池引用：测试装备池

测试装备池
  equipment_field_focus
```

只有一项时直接取得该装备；以后扩充池子，变化局限在供给数据及抽取配置。完成的效果不应因为池内项少而被判为“占位机制”；测试池也不冒充正式池的多样性和平衡已完成。

系统落地时还应保持几个既有事务边界：结果确定后冻结实际装备身份；进度消耗、领奖和存档一起提交；保存失败不丢进度、不发半份奖励；重载不重抽、不重复领取；更改池配置只影响未生成的新奖励。空池／非法物品应在内容校验阶段报错，不在领奖时悄悄吞掉收益。

只读核查：项目已有 `ContentPoolDefinition`，可提供物品身份列表；已有 `RelicReactiveCounterSpec` 的计数 authoring 字段。但 `RelicRunOutcomeKind` 当前仅有 `VictoryGold`，不能据此宣称“击杀达标战后发装备”已经能靠改配置完成。需要接好战斗结果、待兑状态、池抽取与原子发奖接口，不能把金币奖励临时冒充装备奖励。

## 下一步内容工作的标准

- 从原作有趣的完整机制出发，优先保留触发、回报、代价和相互关系；只因英雄名单未齐，不删掉通用候选。
- 对新奖励、招募、召唤内容，效果引用池／定义资源。测试可以用已有单项池或已完成单位；被明确需要的正式内容继续登记。
- 机制尚未想清楚的部分继续留白；原作机制已经清楚但实现缺接口，则列出接口工作，不退化成现有引擎最容易支持的小属性。
- 每条机制独立评估作用对象和触发频率。单人动作游戏的数值迁为全队规则要核算人数与事件量，但不为表现原创而改掉玩法主干。
- 继续扩充这个参考库，合并同机制重复项，保留来源差异。后续正式候选数与供给预算按完整流程设计，不拿当前 15 节点验证配方限制整个遗物库。

## 检索与证据边界

本轮定向检索 Slay the Spire 1、Risk of Rain 2、Brotato、Monster Train 的上述条目，核对触发、奖励、代价与适配。没有全量遍历四作，也未运行原作测试。版本按本次查阅的社区资料；没有将搜索中混入的 Slay the Spire 2 条目作为一代依据。

Brotato 四个单项页面已读取正文；其余资料包含 wiki 正文或搜索索引提供的正文摘录。若页面直接打开受限，只使用实际返回的索引内容，不声称已成功读取被拒绝页面，也不从搜索标题补造效果。RL11／RL19 使用 RoR2 版本页中展示的物品描述，RL14／RL20 使用公开物品数据表；数值没有直接采用。Monster Train 的入场／死亡术语保留其本义，避免召唤与入场混淆。来源均为社区维基资料，本轮不称为官方定案或实测。

只更新设计讨论与证据，不修改正式规则、游戏效果或原始研究库；具体条目待后续选择和实现。
