# 当前全部单位与效果表

2026-10-03 工作区快照。用于选择下一批内容重做范围；本轮不删除单位、不改技能逻辑，不改特效。保留现有特效资源，蓄怒重拳、冲锋、喷火作为用户点名的技能保留候选；其他技能仍待逐项选择。

**共129个独立单位定义，对应129个独立场景：91名英雄、21种战斗敌人、13种召唤体／场上造物、4种测试假人。** 91名英雄中，当前探索供给26名，其他矩阵英雄27名，旧HC英雄38名。21种敌人中19种进入当前探索遭遇，另2种旧Boss仍保留。表中每个单位只占一行；同一单位的运行变体另见末尾。

编号采用U001–U129，与羁绊参考库数字索引相互独立；MX／HC／GX为项目现有内容编号。★表示用户本轮明确点名的技能对应单位，尚不表示必须保留整个单位的所有数值与配套。单位名链接独立场景，具名技能链接能力资源；完整稳定ID、基础属性、绑定路径和文件哈希见[数据快照](unit-effects-20261003.json)。

## 用户点名的保留候选

| 对应单位 | 当前技能与效果 | 所在范围 |
| --- | --- | --- |
| U090 [怒拳斗士](../../../../content/heroes/hero_hc38_grit_brawler.tscn)（HC38） ★ | “蓄意轰拳”在工程中名为“蓄怒重拳”：生命承伤储怒，触发时消耗怒劲，获得等量护盾，并打出前方范围重拳。 | 旧HC库；未进入当前探索招募 |
| U098 [破阵巨兽](../../../../content/enemies/enemy_ee03_trample_brute.tscn) ★ | “蛮横冲阵”：锁向蓄力后冲锋，推开沿途单位，对每个敌人造成一次伤害，可被控制或堵路打断。 | 当前探索敌人 |
| U099 [炉喉蜥卫](../../../../content/enemies/enemy_ee07_furnace_lizard.tscn) ★ | 你说的“蝎子喷火”对应现有喷火样板EE07；资源名仍叫“炉喉蜥卫／炉喉吐息”：锁定扇形方向，连续三段火焰，可被控制打断。 | 当前探索敌人 |

## 当前探索可取得的英雄（26名）

按当前默认入口的成长基础能力覆盖整理，包含低阶招募和高阶供给。T1–T4是当前供给档位，不是升阶次数；开局只抽T1／T2。

生产单位在符合冻结参与资格的非终局有效结算时产出一次：攻击／生命培养分别永久增加目标原始攻击／生命的4%；研究模式为研究＋1。同次只选一种模式。升阶战斗增益已有7项、留白19项；留白仍保留原有能力以及升阶事务和发现三选一，不能解读为没有升阶系统。

| 编号与单位 | 羁绊／供给档位 | 主技能（含开场技能） | 被动、生产与其他效果 | 升阶新增／替换收益 |
| --- | --- | --- | --- | --- |
| U001 [冻原盾卫](../../../../content/validation_matrix/units/hero_mx01.tscn)（MX01） | 护卫／霜羽；T1 | 盾击当前敌人，造成140%攻击伤害；自身获得45点护盾，持续3秒。 | 受到普通攻击时，给攻击者施加1层寒意；同一效果冷却1.5秒；[冻土积层](../../../../content/growth/abilities/hero_mx01_permanent_frost_growth.tres)：每场首次成功冻结敌人后，永久获得16点最大生命；本场立即生效。 | [寒阵接力](../../../../content/growth/abilities/hero_mx01_ascend.tres)：本场首次施加控制后，为2.5格内至多2名友方单位（可包含自身）提供35点护盾，持续3秒。 |
| U002 [裂冰斗士](../../../../content/validation_matrix/units/hero_mx02.tscn)（MX02） | 斗士／霜羽；T2 | 重击当前敌人，造成170%攻击伤害；若其受控，再造成65%攻击伤害。 | 首次攻击每名被冻结的敌人时，自身获得24点护盾，持续3秒。 | **留白**（不新增战斗收益） |
| U003 [寒羽游侠](../../../../content/validation_matrix/units/hero_mx03.tscn)（MX03） | 射手／霜羽；T1 | 连续射出5箭，每箭造成55%攻击伤害，最多攻击3名敌人。 | 连续命中同一敌人3次时施加1层寒意；更换目标会清空次数。 | [寒羽游侠·升阶被动](../../../../content/growth/abilities/hero_mx03_ascended_p.tres)：每连续命中3次，额外施加1层寒意；更换目标不清零，仍受羁绊冻结冷却限制；[追猎寒羽](../../../../content/growth/abilities/hero_mx03_ascend.tres)：开战时获得15%攻速，持续3.5秒；连续命中次数不会因更换目标而清零。 |
| U004 [雪幕术士](../../../../content/validation_matrix/units/hero_mx04.tscn)（MX04） | 术士／霜羽；T3 | 在敌人最密集处降下3轮雪幕，每轮造成45%攻击伤害并施加1层寒意。 | 每次施法首次命中带寒意的敌人时，返还8点法力。 | **留白**（不新增战斗收益） |
| U005 [雪灯医者](../../../../content/validation_matrix/units/hero_mx05.tscn)（MX05） | 祝祷者／霜羽；T2 | 治疗生命比例最低的友方单位55点，并为其提供25点护盾，持续3秒。 | 3格内的敌人首次被冻结时，治疗生命比例最低的友方单位18点；冷却2.5秒。 | [雪灯回护](../../../../content/growth/abilities/hero_mx05_ascend.tres)：本场首次有友军施加控制后，为生命比例最低的至多2名友军提供28点护盾，持续3秒。 |
| U006 [药藤师](../../../../content/validation_matrix/units/hero_mx10.tscn)（MX10） | 祝祷者／棘毒；T2 | 治疗生命比例最低的友方单位60点；其3格内至多4名带棘毒敌人降低8点攻击，持续2.5秒。 | 敌人达到3层棘毒时，为正被其攻击的友方单位提供18点护盾；每名受益者独立冷却3秒。 <br>【生产】生命培养；有效结算规则见表前说明。 | **留白**（不新增战斗收益） |
| U007 [铆盾卫](../../../../content/validation_matrix/units/hero_mx21.tscn)（MX21） | 护卫／构装；T1 | 自身获得80点护盾，持续4.5秒。 | 护盾被敌人击破时，自身获得6点护甲，持续3秒；冷却3秒。 | [预铆护阵](../../../../content/growth/abilities/hero_mx21_ascend.tres)：开战时为2.5格内至多2名友方单位（含自身）提供36点护盾，持续3秒。 |
| U008 [碎盾锤](../../../../content/validation_matrix/units/hero_mx22.tscn)（MX22） | 斗士／构装；T2 | 消耗50%当前护盾，重击2格内至多5名敌人，造成160%攻击伤害，并追加所耗护盾80%的伤害。 | 获得其他友军提供的护盾时积累1层增幅，每层增加2点攻击，最多5层。 | [余盾回收](../../../../content/growth/abilities/hero_mx22_ascend.tres)：施法消耗护盾后，为自身返还24点护盾，持续2秒。 |
| U009 [机弩手](../../../../content/validation_matrix/units/hero_mx23.tscn)（MX23） | 射手／构装；T1 | 向当前敌人连续射出5箭，每箭造成55%攻击伤害；同一目标每次命中会额外增加伤害。 | 持盾时连续命中同一目标会提高攻速，每层2.5%，最多8层；失去护盾后每秒减少1层。 | [稳压供弹](../../../../content/growth/abilities/hero_mx23_ascend.tres)：本场首次被敌人击破护盾后，获得18%攻速，持续1.5秒。 |
| U010 [磁轨术士](../../../../content/validation_matrix/units/hero_mx24.tscn)（MX24） | 术士／构装；T3 | 发射贯穿磁轨，对路径上的敌人造成220%攻击伤害。 | 每次技能命中第2名敌人时，为最前方友军提供35点护盾，持续3秒。 | **留白**（不新增战斗收益） |
| U011 [工蜂监工](../../../../content/validation_matrix/units/hero_mx25.tscn)（MX25） | 统御者／构装；T2 | [开战工蜂](../../../../content/growth/abilities/hero_mx25_growth_drone.tres)：每场开战时召唤1只持续10秒的工蜂。 | 每只工蜂首次攻击命中时提供25点、持续3秒的护盾；【目标配置偏差】当前默认只选最近1名同队单位，未排除触发工蜂，正常会先选到工蜂自身。 <br>【生产】攻击／生命培养／研究三选一；有效结算规则见表前说明。 | [协同工单](../../../../content/growth/abilities/hero_mx25_ascend.tres)：开战时为2.5格内至多2名友方单位（含自身）提供30点护盾，持续3秒。 |
| U012 [血契祭司](../../../../content/validation_matrix/units/hero_mx30.tscn)（MX30） | 祝祷者／血誓；T2 | 消耗8%当前生命，治疗生命比例最低的至多2名友方单位各55点。 | 治疗低生命友方单位时，再为其提供20点护盾，持续3秒。 <br>【生产】攻击／生命培养二选一；有效结算规则见表前说明。 | **留白**（不新增战斗收益） |
| U013 [霜毒斥候](../../../../content/validation_matrix/units/hero_mx36.tscn)（MX36） | 突袭者／霜羽／棘毒；T3 | 突进至棘毒层数最高的敌人，造成150%攻击伤害并施加1层寒意。 | 普通攻击命中带寒意的敌人时，额外施加2层棘毒；冷却2秒。 | **留白**（不新增战斗收益） |
| U014 [冥铸守门人](../../../../content/validation_matrix/units/hero_mx39.tscn)（MX39） | 护卫／亡契／构装；T3 | 自身获得55点护盾；3格内至多3名临时友军各获得30点护盾，均持续3秒。 | 3格内每名不同单位首次阵亡时，自身获得20点护盾，持续2秒；冷却1秒。 | **留白**（不新增战斗收益） |
| U015 [赤炉破阵者](../../../../content/validation_matrix/units/hero_mx40.tscn)（MX40） | 斗士／构装／血誓；T3 | 消耗40%当前护盾和8%当前生命，重击2格内至多4名敌人，造成180%攻击伤害，并追加所耗护盾70%的伤害。 | 生命较低时，本场首次获得其他友军提供的护盾后，下一次普通攻击伤害提高45%。 | **留白**（不新增战斗收益） |
| U016 [极光唤灵者](../../../../content/validation_matrix/units/hero_mx42.tscn)（MX42） | 统御者／星辉／霜羽；T3 | 召唤1只持续10秒的极光灵；最多同时拥有3只。 | 极光灵攻击命中时施加1层寒意，同一敌人冷却1.5秒；自身施法时延长1只极光灵2秒寿命，每只最多延长3次。 | **留白**（不新增战斗收益） |
| U017 [构装卫师](../../../../content/validation_matrix/units/hero_mx43.tscn)（MX43） | 护卫／祝祷者／构装；T3 | 为3格内至多3名友方单位提供35点护盾，持续3秒。 | 护盾被敌人击破时，治疗最近的受伤友军35点；冷却3秒。 | **留白**（不新增战斗收益） |
| U018 [霜弦咏者](../../../../content/validation_matrix/units/hero_mx44.tscn)（MX44） | 射手／术士／霜羽；T4 | 向当前敌人连续射出4支法术箭，每箭造成70%攻击伤害；每层蓄箭再提高8%伤害并消耗全部蓄箭。 | 每次普通攻击命中获得1层蓄箭，最多4层。 | **留白**（不新增战斗收益） |
| U019 [幽刃斗将](../../../../content/validation_matrix/units/hero_mx45.tscn)（MX45） | 斗士／突袭者／亡契；T4 | 突进至生命比例最低的敌人，造成200%攻击伤害；每层复仇再增加8点伤害并消耗全部复仇。 | 受到敌人生命伤害或其他友军首次阵亡时获得1层复仇，最多4层；主动技能击杀敌人后获得30点护盾，持续3秒。 | **留白**（不新增战斗收益） |
| U020 [棘核寄生体](../../../../content/validation_matrix/units/hero_mx46.tscn)（MX46） | 棘毒；T4 | 从当前敌人转移至多3层棘毒给6格内另一名敌人，棘毒总层数不变。 | 自己施加的棘毒被转移时，自身获得25点护盾，持续3秒；冷却2.5秒。 | **留白**（不新增战斗收益） |
| U021 [星界观测者](../../../../content/validation_matrix/units/hero_mx47.tscn)（MX47） | 星辉；T4 | 使最近的另一名可强化施法者下一次技能效果提高50%，持续12秒。 | 其他友军消耗法力施法时，自身获得6点法力；冷却1.5秒。 | **留白**（不新增战斗收益） |
| U022 [铁弩佣兵](../../../../content/validation_matrix/units/hero_mx48.tscn)（MX48） | 射手；T4 | 自身获得12点攻击，持续4.5秒。 | 连续命中同一敌人时积累专注，最多5层；每层使下一次普通攻击伤害提高6%，更换目标会清空层数。 | **留白**（不新增战斗收益） |
| U023 [蔓疫织者](../../../../content/growth/units/gx01_vine_plague.tscn)（GX01） | 术士／棘毒；T4 | 【文案不符】当前为：对7格内棘毒层数最高的至多5名敌人，各施加向下取整的（6＋140%自身攻击）层棘毒；没有按已有棘毒比例放大的操作。基础12攻击时为22层，尚非平衡认可。 | 未配置独立被动 | **留白**（不新增战斗收益） |
| U024 [负毒行者](../../../../content/growth/units/gx02_venom_walker.tscn)（GX02） | 统御者／棘毒；T4 | 【文案不符】当前只召唤1只持续6秒的疫焰灵；召唤者与召唤物都未绑定攻击施毒反应，不能按“攻击会施毒”理解。 | 未配置独立被动 | **留白**（不新增战斗收益） |
| U025 [阵列炮师](../../../../content/growth/units/gx03_array_gunner.tscn)（GX03） | 射手／构装；T4 | 消耗35%当前护盾，沿前方8格直线攻击至多5名敌人；每名受到160%攻击＋所耗护盾60%的伤害。 | 未配置独立被动 | **留白**（不新增战斗收益） |
| U026 [稳相锚卫](../../../../content/growth/units/gx04_phase_anchor.tscn)（GX04） | 护卫／构装；T4 | 消耗30%当前护盾，为2.5格内至多3名友方单位（可包含自身）提供36点护盾，持续3秒。 | 未配置独立被动 | **留白**（不新增战斗收益） |

## 其余矩阵英雄（27名）

这些单位有独立场景和主动／被动配置，但没有进入当前26名探索供给，也没有接入该成长规则的升阶收益。

| 编号与单位 | 羁绊 | 主动技能 | 被动效果 |
| --- | --- | --- | --- |
| U027 [荆甲卫](../../../../content/validation_matrix/units/hero_mx06.tscn)（MX06） | 护卫／棘毒 | 嘲讽2.5格内至多5名敌人，持续3秒；自身获得10点护甲，持续3秒。 | 受到普通攻击时，给攻击者施加1层棘毒；每名攻击者独立冷却2秒。 |
| U028 [淬毒弩手](../../../../content/validation_matrix/units/hero_mx07.tscn)（MX07） | 射手／棘毒 | 向当前敌人射出3支毒箭，每箭造成75%攻击伤害。 | 普通攻击或技能命中时，给目标施加1层棘毒。 |
| U029 [萃毒猎手](../../../../content/validation_matrix/units/hero_mx08.tscn)（MX08） | 突袭者／棘毒 | 突进至棘毒层数最高的敌人，消耗至多3层棘毒；每层造成12点伤害。 | 亲自击杀带棘毒的敌人后，自身获得35点护盾，持续3秒。 |
| U030 [疫雾术士](../../../../content/validation_matrix/units/hero_mx09.tscn)（MX09） | 术士／棘毒 | 向敌人最密集处释放毒云，造成70%攻击伤害并给至多6名敌人施加2层棘毒；主目标现有棘毒增加40%。 | 技能命中带棘毒的敌人时，扩大下一次毒云范围，最多叠加4次。 |
| U031 [炉刃武士](../../../../content/validation_matrix/units/hero_mx11.tscn)（MX11） | 斗士／灰烬 | 挥出火刃，对前方2格内至多5名敌人造成160%攻击伤害。 | 每次受到生命伤害时，扩大下一次火刃的攻击范围，最多叠加4次。 |
| U032 [火羽炮手](../../../../content/validation_matrix/units/hero_mx12.tscn)（MX12） | 射手／灰烬 | 发射贯穿射线，对路径上的敌人造成200%攻击伤害。 | 普通攻击命中带余烬的敌人时，获得4点法力；冷却0.5秒。 |
| U033 [烬步客](../../../../content/validation_matrix/units/hero_mx13.tscn)（MX13） | 突袭者／灰烬 | 突进至余烬层数最高的敌人，造成200%攻击伤害。 | 亲自引爆余烬后，下一次普通攻击伤害提高35%。 |
| U034 [焚城术士](../../../../content/validation_matrix/units/hero_mx14.tscn)（MX14） | 术士／灰烬 | 延迟0.8秒轰击敌人最密集处，对2格内敌人造成240%攻击伤害。 | 每次技能命中第2名带余烬的敌人时，返还15点法力。 |
| U035 [薪火牧者](../../../../content/validation_matrix/units/hero_mx15.tscn)（MX15） | 统御者／灰烬 | 召唤1只持续10秒的火灵；最多同时拥有3只。 | 所属火灵阵亡时，给1.5格内至多5名敌人施加1层余烬。 |
| U036 [守冢骑士](../../../../content/validation_matrix/units/hero_mx16.tscn)（MX16） | 护卫／亡契 | 自身获得8点护甲，持续4秒；最近的另一名召唤者获得40点护盾，持续3秒。 | 3格内临时友军阵亡时，自身获得18点护盾，最多保有65点，冷却1秒；自身每场首次阵亡时，为最近的存活友军提供35点护盾，持续3.5秒。 |
| U037 [残誓武士](../../../../content/validation_matrix/units/hero_mx17.tscn)（MX17） | 斗士／亡契 | 横扫2格内至多5名敌人，造成160%攻击伤害；每层战意再增加7点伤害。 | 每名不同友军首次阵亡时获得1层战意，最多5层；临时单位只提供四分之一层。 |
| U038 [渡魂刃](../../../../content/validation_matrix/units/hero_mx18.tscn)（MX18） | 突袭者／亡契 | 突进至生命比例最低的敌人，造成190%攻击伤害。 | 其他友军阵亡时，下一次技能伤害提高35%，最多储存1次。 |
| U039 [引魂灯使](../../../../content/validation_matrix/units/hero_mx19.tscn)（MX19） | 祝祷者／亡契 | 为生命比例最低的至多2名友方单位提供40点护盾，持续3秒；并额外触发其中1名的“遗盾”或“遗灯”。每名亡语拥有者每场只能被额外触发1次。 | 自身每场首次阵亡时，为生命比例最低的存活友军提供35点护盾，持续3.5秒。该亡语每场可被主动技能额外触发1次。 |
| U040 [白骨司祭](../../../../content/validation_matrix/units/hero_mx20.tscn)（MX20） | 统御者／亡契 | 召唤2只持续10秒的骷髅；最多同时拥有3只。 | 临时友军阵亡时，对其1.3格内至多5名敌人造成死者最大生命20%的伤害。 |
| U041 [血誓狂战](../../../../content/validation_matrix/units/hero_mx26.tscn)（MX26） | 斗士／血誓 | 消耗10%当前生命，突进至最近的敌人并造成130%攻击伤害；自身获得10点攻击，持续4秒。 | 生命低于60%时，攻速提高25%；生命恢复后解除。 |
| U042 [血弦弓手](../../../../content/validation_matrix/units/hero_mx27.tscn)（MX27） | 射手／血誓 | 消耗8%当前生命，向当前敌人射出4箭，每箭造成80%攻击伤害。 | 生命低于60%且持有护盾时，攻速提高20%；任一条件不满足时解除。 |
| U043 [赤刃猎手](../../../../content/validation_matrix/units/hero_mx28.tscn)（MX28） | 突袭者／血誓 | 突进至生命比例最低的敌人，造成180%攻击伤害。 | 亲自击杀敌人后，恢复自身最大生命12%的生命，最多恢复45点。 |
| U044 [裂脉术士](../../../../content/validation_matrix/units/hero_mx29.tscn)（MX29） | 术士／血誓 | 消耗一半已储存的伤痛，对1.6格内敌人造成140%攻击伤害，并追加全部已储存伤痛30%的伤害。 | 储存敌人实际造成的生命损失，最多120点；死亡时清空。 |
| U045 [星幕守卫](../../../../content/validation_matrix/units/hero_mx31.tscn)（MX31） | 护卫／星辉 | 自身获得60点护盾；最近的另一名施法者获得30点护盾，均持续3秒。 | 3格内其他友军消耗法力施法时，自身获得18点护盾，持续3秒；冷却2秒。 |
| U046 [星轨射手](../../../../content/validation_matrix/units/hero_mx32.tscn)（MX32） | 射手／星辉 | 对当前敌人造成230%攻击伤害，并使下一次普通攻击伤害提高40%。 | 施法后的第一次普通攻击会额外射出1箭，造成55%攻击伤害。 |
| U047 [共鸣术士](../../../../content/validation_matrix/units/hero_mx33.tscn)（MX33） | 术士／星辉 | 对当前敌人造成230%攻击伤害。 | 3格内其他友军消耗法力施法时，自己的下一次技能伤害提高30%。 |
| U048 [星泉祈者](../../../../content/validation_matrix/units/hero_mx34.tscn)（MX34） | 祝祷者／星辉 | 治疗生命比例最低的至多2名友方单位各55点。 | 每次主动技能实际恢复生命时，为另一名法力比例最低的友军补充8点法力。 |
| U049 [星灵织者](../../../../content/validation_matrix/units/hero_mx35.tscn)（MX35） | 统御者／星辉 | 召唤1只持续10秒的星灵；最多同时拥有3只。 | 其他友军消耗法力施法时，使至多3只星灵各获得2点攻击，最多叠加5次；冷却0.5秒。 |
| U050 [疫焰使役者](../../../../content/validation_matrix/units/hero_mx37.tscn)（MX37） | 统御者／棘毒／灰烬 | 召唤1只持续10秒的疫焰灵；最多同时拥有3只。 | 疫焰灵攻击命中时施加1层棘毒；疫焰灵阵亡时，给1.5格内至多5名敌人施加1层余烬。 |
| U051 [余烬司魂者](../../../../content/validation_matrix/units/hero_mx38.tscn)（MX38） | 术士／灰烬／亡契 | 对敌人最密集处2格内的敌人造成210%攻击伤害并施加1层余烬；每2层亡者余烬再增加1层余烬。 | 每名不同友军首次阵亡时获得1层亡者余烬，最多4层；临时单位只提供四分之一层。 |
| U052 [祈血星使](../../../../content/validation_matrix/units/hero_mx41.tscn)（MX41） | 祝祷者／血誓／星辉 | 消耗6%当前生命，为法力比例最低的至多2名其他友军各补充15点法力并治疗25点。 | 受到其他友军的实际治疗后，下一次主动技能效果提高35%，最多储存1次。 |
| U053 [游历决斗士](../../../../content/validation_matrix/units/hero_mx49.tscn)（MX49） | 斗士 | [游历决斗士·决斗](../../../../content/validation_matrix/abilities/hero_mx49_a.tres)：与当前敌人互相锁定攻击4秒；若相距超过4格则提前结束。 | [游历决斗士·反击](../../../../content/validation_matrix/abilities/hero_mx49_p.tres)：被锁定目标攻击3次后反击，造成120%攻击伤害，并恢复该次伤害25%的生命。 |

## 旧HC英雄库（38名）

以下全部保留在工程中，但均不在当前探索招募池。标“保留库”的7名与旧Alpha供给进一步分开；其余31名在旧Alpha英雄目录中。这里按场景实际绑定列效果，不能把旧英雄目录误当作当前探索供给。HC02没有对应单位定义／场景，因此不补造一行。

| 编号与单位 | 内容归属／羁绊 | 主技能 | 被动与其他效果 |
| --- | --- | --- | --- |
| U054 [连弩手](../../../../content/heroes/hero_hc01_crossbow.tscn)（HC01） | 旧Alpha英雄目录；无已配置羁绊 | [疾风连射](../../../../content/abilities/automatic/ability_hc01_volley.tres)：连射 3 箭，每箭 65% 攻击力伤害。 箭间隔为当前普攻的 50%，随攻速变化。 | 连击加速：普攻和技能箭每次命中增加4%基础攻速，无层数上限；换目标清零、战后清零。独立组件还保留旧U01换目标保层升级开关，未接入当前成长英雄名单。 |
| U055 [铁甲卫](../../../../content/heroes/hero_hc03_iron_guard.tscn)（HC03） | 旧Alpha英雄目录；霜羽同盟 | [铁壁挑衅](../../../../content/abilities/automatic/ability_hc03_challenge.tres)：嘲讽周围 2.5 格敌人，持续 3 秒。 自身防御 +12，持续 3 秒。 | [荆棘甲](../../../../content/abilities/passive/ability_hc03_thorns.tres)：被普攻命中时，对攻击者造成当前防御 100% 的伤害。 |
| U056 [追刃客](../../../../content/heroes/hero_hc04_crit_duelist.tscn)（HC04） | 旧Alpha英雄目录；无已配置羁绊 | [追刃姿态](../../../../content/abilities/automatic/ability_hc04_a.tres)：暴击率与攻速提高 35%，持续 4 秒。 | [猎刃本能](../../../../content/abilities/passive/ability_hc04_p0.tres)：暴击率增加 15%；[猎刃本能](../../../../content/abilities/triggered/ability_hc04_p1.tres)：普攻暴击后立即追击，至少间隔 0.2 秒。 |
| U057 [回潮术士](../../../../content/heroes/hero_hc05_tide_caster.tscn)（HC05） | 旧Alpha英雄目录；无已配置羁绊 | [回潮冲击](../../../../content/abilities/automatic/ability_hc05_a.tres)：对目标及其 1.4 格内敌人造成 70 伤害。 | [潮汐回流](../../../../content/abilities/triggered/ability_hc05_p.tres)：主动施放完毕后返还 20 法力并获得 25 护盾。 |
| U058 [共鸣使](../../../../content/heroes/hero_hc06_surge_conductor.tscn)（HC06） | 旧Alpha英雄目录；无已配置羁绊 | [共鸣迸发](../../../../content/abilities/automatic/ability_hc06_a.tres)：满蓝且全队共鸣至少 5 点时，花费 5 点，以攻击范围内的敌方目标为中心，对其 4 格内敌人造成 100 伤害。 | 协奏供能：每次友军满蓝技能成功释放产生1点全队共鸣；自身每第3次普攻命中也产生1点。两个触发入口，不重复计算文案。 |
| U059 [传灯狐](../../../../content/heroes/hero_hc07_death_echo.tscn)（HC07） | 旧Alpha英雄目录；无已配置羁绊 | [护灯](../../../../content/abilities/automatic/ability_hc07_a.tres)：为自身和身后最近友军各提供 55 护盾。 | [借火](../../../../content/abilities/triggered/ability_hc07_p.tres)：每第三次普攻命中，触发身后 3 格内最近友军的可复制亡语；该友军不会因此死亡。 |
| U060 [棘毒使](../../../../content/heroes/hero_hc08_poison_keeper.tscn)（HC08） | 旧Alpha英雄目录；无已配置羁绊 | [毒云](../../../../content/abilities/automatic/ability_ne01_poison_cloud.tres)：对目标及周围 1.5 格敌人施加 3 层棘毒，随后主目标的棘毒层数增加 50%（向下取整）。 | [淬毒](../../../../content/abilities/triggered/ability_ne01_poison_hit.tres)：普攻命中施加 1 层棘毒：持续 12 秒，每层每秒造成 2 真实伤害。 |
| U061 [裂印使](../../../../content/heroes/hero_hc09_breach_scribe.tscn)（HC09） | 旧Alpha英雄目录；无已配置羁绊 | [刻下破绽](../../../../content/abilities/automatic/ability_hc09_a.tres)：对目标及周围 1.4 格敌人施加 6 秒破绽。 | [乘隙](../../../../content/abilities/triggered/ability_hc09_p.tres)：任何友军普攻或技能箭命中自己的破绽目标时，额外造成该攻击者攻击力 20% 的伤害。 |
| U062 [引烬师](../../../../content/heroes/hero_hc10_ember_cashout.tscn)（HC10） | 旧Alpha英雄目录；无已配置羁绊 | [收烬](../../../../content/abilities/automatic/ability_hc10_a.tres)：目标至少有 3 层余烬时，消耗全部余烬，立即造成其剩余伤害。 | [添薪](../../../../content/abilities/triggered/ability_hc10_p.tres)：普攻命中叠 1 层余烬，每层每秒 4 伤害，6 秒；队友同名余烬也可供给。 |
| U063 [三相使](../../../../content/heroes/hero_hc11_triad_weaver.tscn)（HC11） | 旧Alpha英雄目录；无已配置羁绊 | [三相印](../../../../content/abilities/automatic/ability_hc11_a.tres)：对目标周围 1.4 格敌人分别施加蚀毒、余烬和 3 秒霜缓。 | [异相共振](../../../../content/abilities/triggered/ability_hc11_p.tres)：普攻命中后，目标每有一种负面状态便额外受到 10 伤害；同种状态只计一次。 |
| U064 [缚钟人](../../../../content/heroes/hero_hc12_control_clock.tscn)（HC12） | 旧Alpha英雄目录；无已配置羁绊 | [停摆钟](../../../../content/abilities/automatic/ability_hc12_a.tres)：使目标周围 1.5 格敌人无法行动 1.5 秒，持续时间受控制抗性影响。 | [借隙](../../../../content/abilities/triggered/ability_hc12_p.tres)：自己每成功对一名敌人施加行动控制，获得 6 法力和 10 护盾。 |
| U065 [晶盾剑士](../../../../content/heroes/hero_hc13_shield_crit.tscn)（HC13） | 旧Alpha英雄目录；无已配置羁绊 | [结晶护壁](../../../../content/abilities/automatic/ability_hc13_a.tres)：获得 120 护盾。 | [晶锋](../../../../content/abilities/automatic/ability_hc13_p.tres)：每点当前护盾提供 0.15% 暴击率，最多增加 60%。 |
| U066 [磐岩拳师](../../../../content/heroes/hero_hc14_stone_fist.tscn)（HC14） | 旧Alpha英雄目录；无已配置羁绊 | [磐岩震](../../../../content/abilities/automatic/ability_hc14_a.tres)：对周围 1.5 格敌人造成自身最大生命 12% 加防御 2 倍的伤害。 | [厚积重拳](../../../../content/abilities/triggered/ability_hc14_p.tres)：每次普攻命中追加自身最大生命 2% 的伤害。 |
| U067 [接盾战士](../../../../content/heroes/hero_hc15_shield_grower.tscn)（HC15） | 旧Alpha英雄目录；无已配置羁绊 | [接力盾](../../../../content/abilities/automatic/ability_hc15_a.tres)：自身及最近一名友军各获得 35 护盾。 | [百锻](../../../../content/abilities/triggered/ability_hc15_p.tres)：每次独立获得正值新护盾，本战攻击增加 2；单纯状态刷新不计次。 |
| U068 [碎盾轰卫](../../../../content/heroes/hero_hc16_shield_bomber.tscn)（HC16） | 旧Alpha英雄目录；无已配置羁绊 | [碎盾轰击](../../../../content/abilities/automatic/ability_hc16_a.tres)：护盾至少为 30 时，消耗全部护盾，对周围 1.5 格敌人造成消耗量 80% 的伤害。 | [叠甲](../../../../content/abilities/automatic/ability_hc16_p.tres)：每 2 秒获得 20 护盾。 |
| U069 [蓄痛卫](../../../../content/heroes/hero_hc17_pain_vessel.tscn)（HC17） | 旧Alpha英雄目录；无已配置羁绊 | [还痛](../../../../content/abilities/automatic/ability_hc17_a.tres)：蓄痛至少为 30 时，消耗全部蓄痛，对周围 1.5 格敌人造成蓄痛值 80% 的伤害，并恢复蓄痛值 25% 的生命。 | [忍痛](../../../../content/abilities/triggered/ability_hc17_p.tres)：仅实际扣掉的生命计入蓄痛；护盾吸收和被防御减掉的伤害不计入。 |
| U070 [济世医师](../../../../content/heroes/hero_hc18_healing_reader.tscn)（HC18） | 旧Alpha英雄目录；无已配置羁绊 | [续命针](../../../../content/abilities/automatic/ability_hc18_a.tres)：为 4 格内生命比例最低的两名受伤友军各恢复 70 生命。 | [济世回响](../../../../content/abilities/triggered/ability_hc18_p.tres)：每次实际治疗后，对受益者附近最近敌人造成实际恢复量 60% 的伤害。 |
| U071 [溢光祭司](../../../../content/heroes/hero_hc19_overheal_priest.tscn)（HC19） | 旧Alpha英雄目录；无已配置羁绊 | [盈光](../../../../content/abilities/automatic/ability_hc19_a.tres)：为自己与 3 格内最多两名友军恢复 50 生命；满血也可施放。 | [光有余](../../../../content/abilities/triggered/ability_hc19_p.tres)：自己受到的过量治疗有 50% 转化为护盾。 |
| U072 [血契鼓手](../../../../content/heroes/hero_hc20_blood_drummer.tscn)（HC20） | 旧Alpha英雄目录；无已配置羁绊 | [血鼓](../../../../content/abilities/automatic/ability_hc20_a.tres)：对自己及 2 格内友军造成 6 真实伤害，使存活者的攻速提高 35%，持续 4 秒。 | [以痛催战](../../../../content/abilities/triggered/ability_hc20_p.tres)：自身实际失去生命时，对附近最近敌人造成 12 伤害，最多每 0.5 秒触发一次。 |
| U073 [背水狂徒](../../../../content/heroes/hero_hc21_last_stand.tscn)（HC21） | 旧Alpha英雄目录；无已配置羁绊 | [绝地斩](../../../../content/abilities/automatic/ability_hc21_a.tres)：对目标造成攻击力 200% 伤害，自身恢复 40 生命。 | [背水](../../../../content/abilities/triggered/ability_hc21_p.tres)：每战首次因受伤降到 35% 生命以下时，获得 90 护盾，并使攻速提高 75%、吸血提高 25%，持续 5 秒。 |
| U074 [风步刺客](../../../../content/heroes/hero_hc22_dodge_assassin.tscn)（HC22） | 旧Alpha英雄目录；无已配置羁绊 | [无影步](../../../../content/abilities/automatic/ability_hc22_a.tres)：4 秒内增加 40% 闪避和 20% 吸血。 | [风步本能](../../../../content/abilities/passive/ability_hc22_p0.tres)：常驻 15% 闪避；[风步本能](../../../../content/abilities/triggered/ability_hc22_p1.tres)：成功闪避恢复 20 生命；[风步本能](../../../../content/abilities/triggered/ability_hc22_p2.tres)：成功闪避，对 1.2 格内最近敌人造成攻击力 50% 的反击伤害。 |
| U075 [霜羽猎手](../../../../content/heroes/hero_hc23_frost_historian.tscn)（HC23） | 旧Alpha英雄目录；霜羽同盟 | [霜羽连射](../../../../content/abilities/automatic/ability_ne10_frost_volley.tres)：连续射出 6 箭，在最多 3 个目标间轮转；每箭造成 50% 攻击力伤害，间隔为当前普攻间隔的 60%。每箭命中均触发寒羽。 | [寒羽](../../../../content/abilities/triggered/ability_ne10_chill_hit.tres)：普攻和技能箭命中有 25% 概率冰冻 0.8 秒。霜羽同盟达到 2／4 名成员时，冰冻概率提高至 35%／50%，最高 50%。 |
| U076 [虫群牧者](../../../../content/heroes/hero_hc24_swarm_keeper.tscn)（HC24） | 旧Alpha英雄目录；无已配置羁绊 | [呼群](../../../../content/abilities/automatic/ability_hc24_a.tres)：在附近召唤 2 只虫卒，最多同时拥有 6 只。 | [群护](../../../../content/abilities/triggered/ability_hc24_p.tres)：自己召唤的虫卒每次普攻命中，为牧者增加 4 护盾。 |
| U077 [送葬人](../../../../content/heroes/hero_hc25_death_provider.tscn)（HC25） | 旧Alpha英雄目录；无已配置羁绊 | [砂骨先锋](../../../../content/abilities/automatic/ability_ne02_raise_skeletons.tres)：在附近召唤 2 名砂骨仆，自己最多同时维持 5 名。 | [殉爆](../../../../content/abilities/triggered/ability_ne02_temporary_death_blast.tres)：己方临时小兵死亡时触发殉爆，对死者周围 1.5 格敌人造成死者最大生命 25% 的伤害；[殉爆 · 亡语](../../../../content/abilities/triggered/ability_ne02_owner_death_blast.tres)：亡语：对周围 1.5 格敌人造成自身最大生命 25% 的伤害。此亡语可被代触发。 |
| U078 [灰烬行者](../../../../content/heroes/hero_hc26_ash_returner.tscn)（HC26） | 旧Alpha英雄目录；无已配置羁绊 | [余火踏](../../../../content/abilities/automatic/ability_hc26_a.tres)：对周围 1.5 格敌人造成 65 伤害，自身获得 35 护盾。 | [余温](../../../../content/abilities/triggered/ability_hc26_p_death.tres)：死亡时为周围 2.5 格友军提供 50 护盾，可被代触发；[归灰](../../../../content/abilities/triggered/ability_hc26_p_return.tres)：2 秒后以同一身份和 40% 生命返场，每战最多两次。 |
| U079 [饕餮](../../../../content/heroes/hero_hc27_ally_consumer.tscn)（HC27） | 旧Alpha英雄目录；无已配置羁绊 | [吞食](../../../../content/abilities/automatic/ability_hc27_a.tres)：吃掉 2 格内最近友方临时单位，获得其当前攻击 50% 与最大生命 30%，并恢复新增的生命。 | [备粮](../../../../content/abilities/automatic/ability_hc27_p_start.tres)：开战生成 2 个食饵；[备粮](../../../../content/abilities/automatic/ability_hc27_p_feed.tres)：每 8 秒生成 1 个食饵，最多同时拥有 2 个。 |
| U080 [拾骸者](../../../../content/heroes/hero_hc28_corpse_raiser.tscn)（HC28） | 保留库；无已配置羁绊 | [拾骸](../../../../content/abilities/automatic/ability_hc28_a.tres)：消耗 5 格内最近的非 Boss 敌人尸体，在附近召唤 1 名复生骸骨，最多同时拥有 4 名。 | [催命](../../../../content/abilities/triggered/ability_hc28_p.tres)：普攻命中生命不足 30% 的敌人时，追加攻击力 50% 的伤害。 |
| U081 [缚心者](../../../../content/heroes/hero_hc29_mind_binder.tscn)（HC29） | 旧Alpha英雄目录；无已配置羁绊 | [缚心](../../../../content/abilities/automatic/ability_hc29_a.tres)：锁定半血以下的非 Boss 敌人 2 秒，完成后令其为己方战斗 8 秒；缚心者死亡即解除。技能与身份保留，战后不带走。 | [心隙](../../../../content/abilities/triggered/ability_hc29_p.tres)：普攻命中施加 4 秒心隙：防御减少 3，移速降低 20%。 |
| U082 [山岳旗手](../../../../content/heroes/hero_hc30_armor_broadcaster.tscn)（HC30） | 旧Alpha英雄目录；无已配置羁绊 | [山岳立旗](../../../../content/abilities/automatic/ability_hc30_a.tres)：自身防御增加 12，持续 5 秒，并获得 50 护盾。 | [山岳同袍](../../../../content/abilities/automatic/ability_hc30_p.tres)：2.5 格内其他友军获得自身基础防御 35% 的防御；离开范围或自己倒下时失效。 |
| U083 [破阵骑兵](../../../../content/heroes/hero_hc31_breach_lancer.tscn)（HC31） | 保留库；无已配置羁绊 | [破阵突进](../../../../content/abilities/automatic/ability_hc31_a.tres)：开场满蓝。冲向 4 格内最近敌人，对接敌处周围 0.8 格敌人造成 160% 攻击力伤害并眩晕 0.6 秒。 | [冲阵甲](../../../../content/abilities/triggered/ability_hc31_p.tres)：每次发动满蓝主动时获得 50 护盾。 |
| U084 [腾空武僧](../../../../content/heroes/hero_hc32_sky_monk.tscn)（HC32） | 保留库；无已配置羁绊 | [踏空震落](../../../../content/abilities/automatic/ability_hc32_a.tres)：开场满蓝。跃至 5.5 格内最远敌人身后；落地时对 1 格内敌人造成 140% 攻击力伤害并眩晕 0.8 秒。身后没有空位时不会施放。 | [挨打蓄势](../../../../content/abilities/triggered/ability_hc32_p.tres)：被普通攻击命中时额外获得 3 法力，最多每秒触发一次。 |
| U085 [影隙刺客](../../../../content/heroes/hero_hc33_rift_assassin.tscn)（HC33） | 保留库；无已配置羁绊 | [影隙穿刺](../../../../content/abilities/automatic/ability_hc33_a.tres)：开场满蓝。闪现至 5 格内最远敌人身后，造成 180% 攻击力伤害。身后没有空位时不会施放。 | [跃影追猎](../../../../content/abilities/triggered/ability_hc33_p.tres)：每次发动满蓝主动时，攻速提高 25%、吸血提高 20%，持续 3 秒。 |
| U086 [震盾卫](../../../../content/heroes/hero_hc34_shockshield_guard.tscn)（HC34） | 保留库；无已配置羁绊 | [震盾驱敌](../../../../content/abilities/automatic/ability_hc34_a.tres)：开场满蓝。将 2.2 格内最近的至多 3 名非 Boss 敌人向外推开至多 1.6 格，各造成 40 加 80% 攻击力的伤害并眩晕 0.6 秒。 | [稳阵甲](../../../../content/abilities/triggered/ability_hc34_p.tres)：被普通攻击命中时获得 8 护盾，最多每秒触发一次。 |
| U087 [铁钩猎手](../../../../content/heroes/hero_hc35_hook_hunter.tscn)（HC35） | 保留库；无已配置羁绊 | [铁索拖拽](../../../../content/abilities/automatic/ability_hc35_a.tres)：开场满蓝。将 5 格内最远的非 Boss 敌人拉近至多 4 格，造成 20 加 80% 攻击力的伤害并眩晕 1 秒。 | [乘乱追击](../../../../content/abilities/triggered/ability_hc35_p.tres)：普攻命中带有至少一种负面状态的目标时，追加 25% 攻击力的伤害。 |
| U088 [风眼术士](../../../../content/heroes/hero_hc36_cyclone_weaver.tscn)（HC36） | 保留库；无已配置羁绊 | [风眼收束](../../../../content/abilities/automatic/ability_hc36_a.tres)：开场满蓝。将 3.6 格内最近的至多 5 名非 Boss 敌人拉近至多 2.4 格，各造成 30 加 50% 攻击力的伤害并眩晕 0.4 秒。 | [回旋护流](../../../../content/abilities/automatic/ability_hc36_p.tres)：每 3 秒为自己 3.6 格内最近的两名其他友军各提供 25 护盾。 |
| U089 [军团斗士](../../../../content/heroes/hero_hc37_legion_duelist.tscn)（HC37） | 旧Alpha英雄目录；无已配置羁绊 | [决斗](../../../../content/abilities/automatic/ability_hc37_a.tres)：与当前近战敌人决斗 4 秒，双方只能攻击彼此且不能发动主动；其他单位仍可攻击双方。任一方死亡或相距超过 4 格时结束。 | [勇毅反击](../../../../content/abilities/triggered/ability_hc37_p.tres)：每承受 3 次普攻命中，额外反击当前近战对手，造成攻击力 100% 的伤害，并回复实际生命伤害的 60%。反击不触发普攻命中效果。 |
| U090 [怒拳斗士](../../../../content/heroes/hero_hc38_grit_brawler.tscn)（HC38） ★ | 旧Alpha英雄目录；无已配置羁绊 | [蓄怒重拳](../../../../content/abilities/automatic/ability_hc38_a.tres)：怒劲满、生命跌破 50% 或低于 20% 时触发，濒死触发每战一次。消耗全部怒劲并获得等量的 3 秒护盾，蓄力后对前方 3 格、宽 1.6 格的敌人造成攻击力 100% 加怒劲 80% 的伤害。 | [怒劲](../../../../content/abilities/triggered/ability_hc38_p.tres)：实际损失的生命积为怒劲，上限为最大生命的 50%；战内不自然流失，施放重拳时消耗，死亡或战斗结束清空。护盾吸收、被防御减免的伤害不计入。 |
| U091 [铁钩机兵](../../../../content/heroes/hero_hc39_hook_machine.tscn)（HC39） | 旧Alpha英雄目录；无已配置羁绊 | [动力飞钩](../../../../content/abilities/automatic/ability_hc39_a.tres)：向 7 格内最远敌人射出飞钩，对沿途首名敌人造成攻击力 100% 的伤害并拖至身前空位。飞钩会被前排阻挡；首领只受伤害。 | [应急屏障](../../../../content/abilities/triggered/ability_hc39_p.tres)：每战首次因生命承伤降至最大生命的 30% 及以下时，获得最大生命 30% 的护盾；致命伤不会被此盾挽救。 |

## 战斗敌人与Boss（21种）

“当前遭遇”依据默认探索战役引用的具体阵容逐一核对，不按整个敌人目录推断。数值为单位基础配置，探索遭遇还会施加独立生命／伤害倍率。当前普通伤害统一受防御影响，箭／射线的外观不同不构成物理／魔法两类防御；表中的旧资源“护甲”指当前防御。

| 编号与单位 | 出场状态 | 当前效果 |
| --- | --- | --- |
| U092 [血潮掠夺者](../../../../content/enemies/enemy_blood_reaver.tscn) | 当前遭遇 | 近战普攻，带15%吸血。 |
| U093 [腐尸爬兽](../../../../content/enemies/enemy_carrion.tscn) | 当前遭遇 | 近战快攻，基础普攻间隔0.9秒；无独立技能。 |
| U094 [塔垛弩手](../../../../content/enemies/enemy_crossbow.tscn) | 当前遭遇 | 5格弹道远射；无独立技能。 |
| U095 [沙路割喉者](../../../../content/enemies/enemy_cutpurse.tscn) | 当前遭遇 | 近战普攻；对当前生命比例不高于20%的目标，伤害乘1.5。组件虽然叫“斩杀”，实际是增伤，不是直接处决。 |
| U096 [岩垒督军](../../../../content/enemies/enemy_eb01_rampart_warden.tscn) | 当前遭遇 | 筑垒裂地：升起短岩垒阻挡通路，再沿窄线打出160%攻击伤害的地裂；岩垒可攻击摧毁、默认存活3秒。普攻另带0.65格溅射。 |
| U097 [裂壳虫母](../../../../content/enemies/enemy_eb02_shell_matriarch.tscn) | 当前遭遇 | 裂壳育雏：半血时裂壳缩体、后退，改用远程酸液并产可破坏卵囊；约3秒孵化幼虫，全场至多2批。 |
| U098 [破阵巨兽](../../../../content/enemies/enemy_ee03_trample_brute.tscn) ★ | 当前遭遇 | 蛮横冲阵：满蓝后锁向蓄力1.2秒并冲锋，推开沿途单位，对每个敌人造成一次120%攻击伤害；受控或无处推开阻挡者时停止。 |
| U099 [炉喉蜥卫](../../../../content/enemies/enemy_ee07_furnace_lizard.tscn) ★ | 当前遭遇 | 炉喉吐息：前摇1秒，向锁定的3格／70°扇形喷吐3段火焰，段间隔0.6秒，每段65%攻击伤害；控制可打断后续吐息。 |
| U100 [返刃投手](../../../../content/enemies/enemy_ee08_return_blade.tscn) | 当前遭遇 | 往返刃：投出沿固定路线飞出再返回的旋刃，同一单位在去程和回程各可命中一次。 |
| U101 [换位妖使](../../../../content/enemies/enemy_ee09_swap_envoy.tscn) | 当前遭遇 | 易位印记：短暂标记后与最远敌人交换位置。 |
| U102 [贯阵弩手](../../../../content/enemies/enemy_es01_piercing_crossbow.tscn) | 当前遭遇 | 贯阵重箭：蓄力1.2秒后沿锁定方向贯穿至多2敌，依次造成120%／80%攻击的伤害；蓄力可被控制打断。 |
| U103 [贯光术士](../../../../content/enemies/enemy_es04_prism_caster.tscn) | 当前遭遇 | 贯光射线：蓄力1.2秒后沿锁定方向贯穿至多2敌，依次造成120%／80%攻击的伤害；蓄力可被控制打断。 |
| U104 [流沙咒师](../../../../content/enemies/enemy_hexer.tscn) | 当前遭遇 | 5格射线普攻；目标周围1.3格溅射，溅射为普攻原始伤害的45%。 |
| U105 [冰誓刃卫](../../../../content/enemies/enemy_ice_blade.tscn) | 当前遭遇 | 近战普攻；145生命、4护甲，无独立冰冻或寒意技能。 |
| U106 [冻原鹰手](../../../../content/enemies/enemy_ice_hawker.tscn) | 当前遭遇 | 4.8格弹道普攻；命中后给目标攻击冷却加0.2秒、移动冷却加0.4秒，不是常驻百分比减速。 |
| U107 [锈甲守卫](../../../../content/enemies/enemy_rust_guard.tscn) | 当前遭遇 | 近战普攻；155生命、6护甲的前排，无独立技能。 |
| U108 [青鳞蛮兽](../../../../content/enemies/enemy_scale_brute.tscn) | 当前遭遇 | 近战普攻；195生命、7护甲的前排，无独立技能。 |
| U109 [第二层主：暗影领主](../../../../content/enemies/enemy_shadow_boss.tscn) | 当前遭遇 | 暗影呼唤：每6秒尝试召唤1只腐尸爬兽，65%生命、70%攻击倍率，存活临时单位上限4；优先选择后排，普攻带1.5格、45%原始伤害的溅射。 |
| U110 [塔巢幼龙](../../../../content/enemies/enemy_wyrm.tscn) | 当前遭遇 | 3.5格弹道普攻；目标周围1格溅射，溅射为普攻原始伤害的45%。 |
| U111 [第一层主：北境重装](../../../../content/enemies/enemy_boreal_boss.tscn) | 旧Boss保留；当前探索无对应遭遇 | 寒甲再生：每5秒获得55护盾；近战普攻带1.2格、45%原始伤害的溅射。 |
| U112 [塔顶主宰：欺诈机神](../../../../content/enemies/enemy_clockwork_boss.tscn) | 旧Boss保留；当前探索无对应遭遇 | 4.5格弹道普攻，最多贯穿2个目标，并有1.8格、45%原始伤害的溅射。旧描述中的“先争夺信标”属于遭遇／战场规则，单位场景本身未配置这一技能。 |

## 召唤体与场上造物（13种）

此处把召唤物自身能力与召唤者赋予的联动分开，避免换一个召唤者后误以为仍有同样效果。

| 编号与单位 | 来源 | 自身行为与联动归属 |
| --- | --- | --- |
| U113 [虫母卵囊](../../../../content/enemies/enemy_brood_egg.tscn) | 裂壳虫母 | 不移动、不普攻；55生命、3护甲，约3秒后孵化裂壳幼虫，先摧毁则阻止孵化。 |
| U114 [裂壳幼虫](../../../../content/enemies/enemy_brood_larva.tscn) | 虫母卵囊孵化 | 80生命、13攻击的近战幼虫，无独立技能。 |
| U115 [短岩垒](../../../../content/enemies/enemy_rampart_segment.tscn) | 岩垒督军 | 不移动、不普攻；75生命、3护甲，阻挡通路与飞行物，可被击碎；寿命由筑垒技能管理。 |
| U116 [星灵](../../../../content/validation_matrix/units/matrix_spirit_astral.tscn) | 星灵织者 | 60生命、12攻击的远程召唤体；本体只普攻，施法后增攻来自星灵织者被动。 |
| U117 [极光灵](../../../../content/validation_matrix/units/matrix_spirit_aurora.tscn) | 极光唤灵者 | 70生命、8攻击的远程召唤体；本体只普攻，命中寒意与延寿来自极光唤灵者被动。 |
| U118 [短寿骷髅](../../../../content/validation_matrix/units/matrix_spirit_bone.tscn) | 白骨司祭 | 65生命、8攻击的近战召唤体；死亡伤害来自白骨司祭被动，不是骷髅自身亡语。 |
| U119 [护卫工蜂](../../../../content/validation_matrix/units/matrix_spirit_drone.tscn) | 工蜂监工 | 110生命、7攻击的近战召唤体；首次命中供盾来自监工被动，本体没有独立供盾能力；当前选目标会优先给触发工蜂自身。 |
| U120 [短寿火灵](../../../../content/validation_matrix/units/matrix_spirit_fire.tscn) | 薪火牧者 | 75生命、10攻击的近战召唤体；死亡施加余烬来自牧者被动。 |
| U121 [疫焰灵](../../../../content/validation_matrix/units/matrix_spirit_plague.tscn) | 疫焰使役者／负毒行者 | 65生命、8攻击的近战召唤体，本体只普攻。疫焰使役者提供攻击棘毒、死亡余烬；负毒行者当前未提供这两个被动。 |
| U122 [虫卒](../../../../content/soldiers/soldier_bc_insect.tscn) | 虫群牧者 | 110生命、13攻击的近战虫卒；命中为牧者供盾来自牧者被动。 |
| U123 [食饵](../../../../content/soldiers/soldier_bc_morsel.tscn) | 饕餮 | 120生命、14攻击，仍会近战普攻；可被饕餮吞食以增长攻击和最大生命，无独立技能。 |
| U124 [复生骸骨](../../../../content/soldiers/soldier_bc_raised.tscn) | 拾骸者 | 180生命、18攻击的近战骸骨；通过消耗敌尸召唤，无独立技能。 |
| U125 [砂骨仆](../../../../content/soldiers/soldier_bc_skeleton.tscn) | 送葬人 | 100生命、12攻击的近战仆从；死亡殉爆来自送葬人的己方临时小兵死亡触发。 |

## 测试假人（4种）

| 编号与单位 | 当前效果与用途 |
| --- | --- |
| U126 [抗性验证傀儡](../../../../content/enemies/enemy_resistance_dummy.tscn) | 1800生命、4护甲、50%基础控制抗性；仍会以2攻击进行近战普攻。用于验证，不在当前探索遭遇。 |
| U127 [近战小兵假人](../../../../content/soldiers/soldier_dummy_melee.tscn) | 800生命、12攻击，近战普攻，无独立技能。除训练外，构装机芯遗物仍引用它作临时召唤模板。 |
| U128 [远程小兵假人](../../../../content/soldiers/soldier_dummy_ranged.tscn) | 500生命、10攻击，5.2格弹道普攻，无独立技能。 |
| U129 [承伤小兵假人](../../../../content/soldiers/soldier_dummy_static.tscn) | 5000生命，不移动、不攻击，仅供承伤观察。 |

## 同一单位的保留变体

以下不额外计入129个单位。当前入口以主表为准；这些差异用于重做时避免误删仍有价值的旧能力。

| 单位 | 其他保留配置 |
| --- | --- |
| U001 冻原盾卫 | 独立矩阵场景基础配置：盾击当前敌人，造成140%攻击伤害；自身获得45点护盾，持续3秒；受到普通攻击时，给攻击者施加1层寒意；同一效果冷却1.5秒。当前探索使用成长覆盖；MX01额外加入永久生命，MX25改为开场召唤。 |
| U011 工蜂监工 | 独立矩阵场景基础配置：召唤1只持续10秒的护卫工蜂；最多同时拥有3只；每只工蜂首次攻击命中时提供25点、持续3秒的护盾；【目标配置偏差】当前默认只选最近1名同队单位，未排除触发工蜂，正常会先选到工蜂自身。当前探索使用成长覆盖；MX01额外加入永久生命，MX25改为开场召唤。 |
| U060 棘毒使 | 旧能力包仍保留，当前场景已换为NE版：[毒羽散布](../../../../content/abilities/automatic/ability_hc08_a.tres)：对目标周围 1.4 格敌人各施加 3 层蚀毒；[留毒](../../../../content/abilities/triggered/ability_hc08_p.tres)：普攻命中施加 1 层蚀毒；每层每秒造成 3 真实伤害，持续 8 秒，继续施加刷新持续。 |
| U075 霜羽猎手 | 旧能力包仍保留，当前场景已换为NE版：[历霜雪崩](../../../../content/abilities/automatic/ability_hc23_a.tres)：对目标周围 1.4 格敌人造成 25 加历霜层数 × 8 的伤害；[霜历](../../../../content/abilities/automatic/ability_hc23_p.tres)：每 2 秒对最近敌人施加霜缓，并使本战历霜增加 1 层。历霜不会随霜缓消失。 |
| U077 送葬人 | 旧能力包仍保留，当前场景已换为NE版：[白骨先锋](../../../../content/abilities/automatic/ability_hc25_a.tres)：召唤 2 名骨卒，最多同时拥有 5 名；[葬礼](../../../../content/abilities/triggered/ability_hc25_p_growth.tres)：其他友军每次死亡，本战攻击增加 3；自己死亡时补出 2 名骨卒，此补员亡语可被传灯狐代触发；[葬礼 · 补员](../../../../content/abilities/triggered/ability_hc25_p_death.tres)：死亡时补出 2 名骨卒；这是可复制亡语，维持上限仍为 5。 |

## 核对中发现的差异与待审点

这些是本轮静态盘点的发现，不代表已经完成全部单位的机制正确性审查。

| 单位／范围 | 已核对的事实 |
| --- | --- |
| MX25 工蜂监工 | 被动以命中工蜂为中心选择最近同队单位，默认仅1目标，未排除工蜂自身；正常会选中自身，不保证给其他队友。见BattleMatrixQueries与MatrixTargetingParameters，静态核对、未实战复现。 |
| GX01 蔓疫织者 | 文案写放大主目标已有棘毒，实际只有Poison操作，按6＋1.4×攻击计算新增层数，没有ScalePoison操作；主表列当前执行含义。 |
| GX02 负毒行者 | 文案写召唤物攻击施毒，实际召唤的疫焰灵本体只普攻，GX02没有MX37那种召唤物命中施毒反应。 |
| MX01／MX25 | 成长流程会替换场景基础能力包；只读矩阵场景会漏掉MX01永久生命，或把MX25误记成持续满蓝召唤。 |
| HC01 连弩手 | 连击加速来自AttackHitGrowthComponent，而非AbilityLoadout；只扫描技能列表会遗漏核心被动。 |
| 19项升阶收益 | 当前确实留空；7项仍有增益，但本轮不把已有增益判作设计合格，也不重新补占位效果。 |
| 旧Boss／敌人 | 单位名或简介不能代替逻辑：冰誓刃卫没有冰冻技能；割喉者的“斩杀”只是低血增伤；欺诈机神的信标规则不是单位自带技能。 |
| 羁绊身份 | MX矩阵羁绊与旧trait_ne10_winter不是同一ID；即便文字都涉及霜羽，也不能默认为互相补人数或共享冻结。 |
| 内容保留边界 | 保存VFX和保留某个技能，不要求沿用旧单位身份、羁绊、数值与升阶；具体重做清单尚未选定。本表不把调研索引转成实现任务。 |

## 来源与验证范围

- 单位全集：递归扫描content内所有引用UnitDefinition.cs的资源，并反查全部独立单位场景；129定义／129场景，没有缺场景或一份定义对应多场景的情况。非单位资源、地图地点、装备／遗物、战场规则不计入单位数。
- 供给：[默认主场景](../../../../scenes/app/GrowthGameRoot.tscn) → [探索V2战役](../../../../content/expedition/v2/expedition_campaign.tres)；英雄结合[26人英雄池](../../../../content/growth/project/growth_hero_pool.tres)、[分档供给](../../../../content/growth/project/growth_recruitment_supply.tres)和[成长规则](../../../../content/growth/growth_rules.tres)。敌人按各ExpeditionEncounter的CompositionId解析实际阵容。
- 机制补核：[矩阵操作执行](../../../../src/Battle/BattleMatrixAbilityScope.cs)、[矩阵选目标](../../../../src/Battle/BattleMatrixQueries.cs)、[基础攻击与溅射](../../../../src/Battle/BattleAttackScope.cs)、[低血伤害倍率](../../../../src/Battle/BattleVitalityScope.cs)、[喷火默认参数](../../../../src/Abilities/Authoring/ConeBreathAbilityOperationSpec.cs)以及单位行为组件。
- 其他技能条目根据当前绑定能力的配置说明整理；没有逐项做实战验证，也不据此声明无bug、平衡合理或有趣。已有明确文案偏差以上述配置／执行补核为准，原描述与配置路径均保留在JSON，便于继续审查。
- 本轮仅生成清单与讨论记录，未修改运行资源、代码、存档或VFX，未启动Godot／构建／整局回归。
