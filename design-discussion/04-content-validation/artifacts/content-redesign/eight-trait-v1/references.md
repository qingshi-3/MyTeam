# 资料依据与适配边界

2026-10-06 成长小批补证据：[完整设计第9节](growth-slice.md#9参考依据与取舍)记录酒馆20.2血宝石、29.4.2后续产出强化、21.2战斗授予／保留及29.2多人保留的官方依据；慎的普攻庇护采用Riot补丁与历史条目摘要交叉核对，历史条目正文访问失败，未宣称完整技能／动态资料已齐。检索围绕上述具体单位与机制进行，没有把本项目的触发时机、数值或胜利持久化条件归为原作规则，也未扩展八羁绊清单。

核对日期：2026-10-04。原66项初选、原编号和全部独有机制索引继续保存在[只读参考库](../../content-audit-20261001/trait-shortlist.md)，本稿不重新编号、不删除未选条目。当前选定为002、009、013、020、032、063、270、756，另选317/398作为两项单体设计提案。

本次查证足以支持机制和具体技能主干的首稿，但**尚未完成八条原羁绊的所有成员、每个历史补丁的完整数值、原作最佳装备/站位及动态特效证据**。项目提案不能冒充这项调研已经完成。单位表中的数值、站位与装备方向是项目设计；没有标成“市场已经验证的本项目平衡”。

## 项目命名与原作索引

2026-10-04 身份纠偏：原始 020“枪手”与 270“风暴龙”的名称、编号和来源保留；项目显示名分别改为“射手”和“元素”。火蝎保留蝎子身份与火焰技能，弓箭单位归远程武器职业；不得从原作标签推断项目物种。V1U04 最新工作名为“寒晶巨兽”，对应下文的冰晶四足素材候选；仅改称“破阵巨兽”的中间方案已撤回。瑟庄妮仅作职责参考，不代表移植其骑乘外形。此处记录名称修正，未新增资料查证。

## 本地素材与技能核对（2026-10-04）

查阅 `assets/donor-units/` 38 套素材的原 SpriteFrames，按其中 atlas 区域在本地查看器展示待机／移动／攻击抽帧；另看狐面双刃刺客和关键角色配套立绘。没有仅凭文件名指定物种，也没有把单帧查看算作实机动态验收。资源来源和 CC0 记录见[素材来源](../../../../../assets/provenance/README.md)。

V1U04 的替代候选为 [boss_borealjuggernaut](../../../../../assets/donor-units/boss_borealjuggernaut/frames.tres)：可见冰晶背刺与四足身体，move/run 同为 8 帧、8 FPS；attack 包含大旋风，不直接移植为普通撞击。进一步查看 f6_sister 完整帧表，其冰晶臂具与近身冰柱动作不足以证明弓炮射击，因此不为 V1U03 绑定。V1U05 可评估 f6_freeblade 的冰刃，V1U21 可评估 f5_drogon 的龙形；这些是素材适配建议，不是已经完成的新角色。

保留技能实际依据为 `content/abilities/automatic/ability_hc38_a.tres`、`ability_ee03_trample.tres`、`ability_ee07_furnace_lizard.tres` 及喷流 operation 默认值。重拳有 3 秒怒劲盾、冲锋是逐敌贯穿、喷火为三脉冲定时入口；本文档下方早期“无占位护盾”等描述是前稿不准确之处，当前以综合审查及原资源为准。

## 羁绊证据

| 编号 | 本次核对的事实与来源 | 版本风险 / 项目适配 |
| --- | --- | --- |
| 002 | [S1极地](https://tftset.com/set/1-beta-set/trait/glacial)：普攻概率眩晕，成员布隆、丽桑卓、艾希、沃利贝尔、瑟庄妮、艾尼维亚。 | 页面概率字段缺失；20/30/40%与0.7秒是项目首轮值，不能当成原作数值。 |
| 009 | [S1斗士](https://tftset.com/set/1-beta-set/trait/brawler)：生命方向，成员沃里克、雷克塞、布里茨、沃利贝尔、蔚、科加斯。 | 项目使用百分比生命配合长期培养，不声称原S1就是该公式。 |
| 013 | [S1换形](https://tftset.com/set/1-beta-set/trait/shapeshifter)：变形后增加生命，成员伊莉丝、奈德丽、杰斯、希瓦娜、纳尔、斯维因。 | 本版保留六种不同形态职责；生命层级、40/80%与每战只扩容一次由项目明确。 |
| 020 | [S1枪手](https://tftset.com/set/1-beta-set/trait/gunslinger)：普攻追加其他目标且带命中效果，成员格雷福斯、崔丝塔娜、卢锡安、普朗克、金克丝、厄运小姐。 | 页面部分概率/人数值为占位符；本稿概率、伤害比和递归边界均为项目参数，成员重组不等于原作枪手本来包含艾希。 |
| 032 | [Riot 10.2](https://teamfighttactics.leagueoflegends.com/en-au/news/game-updates/teamfight-tactics-patch-10-2-notes/)确认6森林复制全体；[森林成员记录](https://wiki.leagueoflegends.com/en-us/TFT%3AWoodland)检索返回艾翁、茂凯、乐芙兰、妮蔻、拉克丝。 | tftset森林页本次读取失败，Wiki此处仅检索摘要；不能声称已逐项看完。项目扩成六个自然可招募成员，不依赖拉克丝特殊计数；复制体属性与装备继承是项目选择。 |
| 063 | [S3圣盾使](https://tftset.com/set/3-galaxies/trait/protector)：施法4秒盾，页面含嘉文、洛、赵信、妮蔻、厄加特；[Riot 10.12](https://teamfighttactics.leagueoflegends.com/en-au/news/game-updates/teamfight-tactics-patch-10-12-notes/)记录6月12日把最高档55%改为50%。 | 同赛季内也有改动。项目采用2/4与20/32%，以5个成员提供搭配余地；不把10.12上线值、热修值混写成唯一原值。 |
| 270 | [Riot 12.12](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/teamfight-tactics-patch-12-12-notes/)明确风暴攻速从25/50/80/150%调整到20/50/100/200%。 | [tftset汇总页](https://tftset.com/set/7-dragonlands/trait/tempest)现混有增伤、10人档及格雷福斯，不能当纯S7首发。项目保留用户选择的八秒落雷后加攻速，并明确将落雷适配成有限普通伤害。 |
| 756 | 既有初选记录的多多自走棋刺客攻击暴击方向，加上用户本轮明确的开场跳后排。 | 原[社区来源](https://autochesscommunity.com/wiki/synergies/assassin-class)本次读取失败；本稿不引用其精确暴击参数。2/4档、全部刺客独立起跳资格、落点算法均为项目提案；原高档击杀追跳未纳入羁绊。 |

原作成员不是必须一一移植的名单。未采用的原成员仍在调研库：丽桑卓的自保、赵信的低蓝循环、洛的突入、茂凯的承伤回复等都有价值，但本版用角色职责和重叠度选择，不为收录完整而继续扩大24人。英雄名字与美术包装暂用工作名，不能由名称相似推断技能版本。

## 单位技能对应

以下本次已打开技能文本。TFTSet为第三方整理，若数值显示“?”或页面含多个补丁，保留未知；不把其中缺失系数自动补成记忆数值。

| 本版单位 | 对应来源 | 保留的核心 / 明确改编 |
| --- | --- | --- |
| V1U01 | [S1 Braum](https://tftset.com/set/1-beta-set/champion/braum) | 前方举盾；不做隔空全队投射物吸收。 |
| V1U02 | [S1 Volibear](https://tftset.com/set/1-beta-set/champion/volibear) | 普攻传导并施加命中效果；缩短持续、减少传导目标。 |
| V1U03 | [S1 Ashe](https://tftset.com/set/1-beta-set/champion/ashe) | 长箭命中首敌，控制与飞行距离相关。 |
| V1U04 | [S1 Sejuani](https://tftset.com/set/1-beta-set/champion/sejuani)及项目冲锋 | 原作仅提供前排群控职责参考；项目技能使用已认可冲锋，不宣称S1瑟庄妮也是该冲锋。 |
| V1U05 | [S1 KhaZix](https://tftset.com/set/1-beta-set/champion/khazix) | 孤立目标伤害提升，转为本项目攻击系数。 |
| V1U06 | [S1 Anivia](https://tftset.com/set/1-beta-set/champion/anivia) | 持续风暴与攻速压制，森林身份为重组。 |
| V1U07 | [当前正式重拳条件](../../../../../gameplay-design/tower-autobattler-core.md) | 既有HC38承伤积怒、条件请求、消耗怒劲后等量3秒盾和重拳；不采用旧稿删盾与随意改倍率。 |
| V1U08 | [S1 Gnar](https://tftset.com/set/1-beta-set/champion/gnar) | 跳入群体、变身与控制；技能自身不再单独叠一次原作生命。 |
| V1U11 | [S1 Nidalee](https://tftset.com/set/1-beta-set/champion/nidalee) | 自身及队友恢复、近战形态强化。 |
| V1U12 | [S1 Elise](https://tftset.com/set/1-beta-set/champion/elise) | 蜘蛛形态、吸血、小蛛；小蛛不继承原作恶魔标签。 |
| V1U13 | [S1 Shyvana](https://tftset.com/set/1-beta-set/champion/shyvana) | 退开、远程龙形与燃烧普攻。 |
| V1U14 | [S1 Jayce](https://tftset.com/set/1-beta-set/champion/jayce) | 锤形击退转炮形，短时高速射击。 |
| V1U15 | [S1 Swain](https://tftset.com/set/1-beta-set/champion/swain) | 限时变身、贴身吸取与结束爆发；盾来自本版圣盾羁绊。 |
| V1U09 | [S1 Blitzcrank](https://tftset.com/set/1-beta-set/champion/blitzcrank) | 牵引破阵，删去下一击击飞/自动全队索敌。 |
| V1U10 | [S7 Lee Sin](https://tftset.com/set/7-dragonlands/champion/lee-sin) | 击退目标撞击敌阵；汇总页有后续减魔抗，本版未采用。 |
| V1U16 | [S3 Neeko](https://tftset.com/set/3-galaxies/champion/neeko) | 身周伤害与控制，不将其误标为S2投种子的版本。 |
| V1U17 | [S1 Rengar](https://tftset.com/set/1-beta-set/champion/rengar) | 追击弱目标、命中攻击与提速。 |
| V1U18 | 项目已认可火蝎喷流；[表现要求](../../../../02-foundation-models/combat-presentation/decisions.md) | 口部供给、锁方向、持续伤害；没有套成S1希瓦娜技能。 |
| V1U19 | [S7 Ezreal](https://tftset.com/set/7-dragonlands/champion/ezreal) | 小技能叠攻速；项目重编为射手身份。 |
| V1U20 | [S7 Qiyana](https://tftset.com/set/7-dragonlands/champion/qiyana) | 调整位置后线形斩击与缴械。 |
| V1U21 | [S7 Ao Shin](https://tftset.com/set/7-dragonlands/champion/ao-shin) | 高蓝、长引导、全场雷雨；主动移除抽蓝。 |
| V1U22 | [S1 Lucian](https://tftset.com/set/1-beta-set/champion/lucian) | 滑步后的攻击与第二发技能弹；刺客身份为本版适配。 |
| V1U23 | [S1 Jinx](https://tftset.com/set/1-beta-set/champion/jinx) | 参与击杀后先提速再开启爆炸攻击，无占位主动。 |
| V1U24 | [S1 Tristana](https://tftset.com/set/1-beta-set/champion/tristana) | 普攻充能的定时炸弹，不换成S7小炮技能。 |

## 两个特殊原型与遗物

317的三个保护位置与地震来自[Riot S7.5介绍](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/uncharted-realms-gameplay-overview/)；398的可独立布置炮台及升级来自[Riot S9介绍](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/runeterra-reforged-gameplay-overview/)。将前者做特殊英雄、后者做多部件Boss，以及模块、属性、失效/重建条件，全是本稿适配。

海克斯基础见既有[来源映射](../references.md)所链接Riot介绍与补丁；本轮额外核对[12.12](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/teamfight-tactics-patch-12-12-notes/)中卓尔不群、复苏之风等变化，说明这些通用机制同样需要版本/数值分开。16件遗物不是宣布这16件同属原作同一套参数。战利品契约以本仓库[现行遗物规则](../../../../../gameplay-design/generic-relics.md)为机制依据。

## 后续证据缺口

- 以选定的原作补丁补齐全部成员数值与版本差异，特别是S7/S7.5混合页面、S3热修、756刺客资料恢复。
- 原作推荐装备、站位和它们成立的敌阵条件，需要独立实战材料；不能拿本稿建议反过来当调研证据。
- 所有新制作特效需先补实际演示与动作拆解。本稿只有动作语义与玩法时序，不替代特效证据；已有认可资源可在确认空间/时序匹配后复用。
- 本项目真实单位和固定库存接入后，检查第一轮技能是否来得及释放、森林复制是否可辨、射手冻结是否失去敌方行动窗口、六人档的取得成本是否值得。
