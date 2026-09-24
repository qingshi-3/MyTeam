# P01 本地架构核对

## 五名敌人动作制作依据（2026-09-19，实施中）

继承 S26 的喷火器窄根连续供给／下游膨开、S33 岩刺与传送壳层、S34 瞬移的双端分离、S24 碎片独立收尾。以上已有实际观察，不重复宣称本轮重新观看。新查 [Sivir 官方页](https://www.leagueoflegends.com/en-us/champions/sivir/) 的 Q 与媒体地址，文字明确去回两次伤害；浏览器超时、官方 MP4/WebM 返回403，本轮没有观看该片。缓存仅用于研究，未知原作精确速度曲线、接刃姿势及材质实现。

本轮已查看工程 authored SpriteFrames 的 idle/attack 阶段图（`.godot/enemy-five-review/assets-contact.png`）：firebreather 有口部出火；irondervish 有躯干起势与手臂上举挥出；spellthief 有手部法光；crystalbeetle 有硬壳和抬爪。素材事实与以下项目适配分开：

| 动作 | 中心、朝向与轨迹 | 项目阶段与分层／素材 |
| --- | --- | --- |
| 炉喉吐息 | 口部身体平面，脚点查询的固定70°/3格扇形；锁向后不追踪 | 1秒吸气、1.8秒连续供给、收势。喷口内焰、外焰卷动、边缘碎火分层；使用专用扇形流动shader，地面淡边界只表达查询，不扩成圆波。供给在控制取消时结束。 |
| 往返刃 | 挥臂离手点，刀体平面自旋；固定原点→端点→原点 | 无蓄力条。采用本地挥臂帧作离手依据，独立十字金属刀体SVG、窄亮刃、短旋转残痕；飞行插值只消费权威轨迹。匀速往返是已确认项目设计，非对Sivir视频的观察；金属刀形与旋转不从普通箭换色。 |
| 成对换位 | 两者脚点、地面双端刻印，准备期细链接跟随目标 | 1.1秒刻印、同一逻辑tick双端瞬移、0.8秒施法者收势；复用S34的离散端点语义。配对几何标记不是伤害范围，取消时同时退去。 |
| 岩垒／地裂 | 三段可攻击岩体组成约2.5格短墙；地裂沿独立锁定地面轴 | 岩块上升、短尘屑；随后窄裂纹预告、逐段尖石突起、碎块退去。复用S33岩石分面素材／由地面向上运动，不使用冰晶换色。墙实体寿命与血量由战斗层控制。 |
| 裂壳、卵与爪 | 身体外层硬壳向两侧分离，原身体真实缩小；卵在合法地面点 | 1.2秒裂纹、硬片脱落、缩体后沿地面退开；沿用S24破裂与惯性收尾关系，绘制独立甲壳片和有胎体的卵SVG，材质不冒充冰片。爪为短弧三条擦痕，酸液为独立黏液滴。 |

全部时间／尺寸是项目调试初值。预览与战斗使用同一VFX场景、共享暂停时钟；记录不等于已完成动态验收或用户认可。

## P01-S47：决斗、蓄怒重拳与实体飞钩（2026-09-19）

直接动作来源：[军团决斗官方片段](https://cdn.cloudflare.steamstatic.com/apps/dota2/videos/dota_react/abilities/legion_commander/legion_commander_duel.mp4)、[瑟提W](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0875/ability_0875_W1.mp4)、[机器人Q](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0053/ability_0053_Q1.mp4)、[机器人被动](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0053/ability_0053_P1.mp4)。已取得完整演示并查看每秒4帧的完整阶段序列，缓存归`.godot/duel-grit-hook/reference/`。这四项是实际演示观察，不是作者制作教程；未取得原工程材质拆解。

- 观察：军团在近战距离对攻，金色标记持续辨认决斗双方，普通攻击继续播放。项目适配为两人脚下的金色断环与交叉剑徽，跟随各自脚点，开始短亮、4秒持续、结束淡出；不画封闭牢笼、不迁入永久成长。
- 观察：瑟提W先画前方细范围边界、身体附近蓄力，随后沿原方向推出宽冲击和尘迹；高怒版本增强身体金光。项目适配为0.6秒锁向、3格长/1.6格宽的地面边界，释放层沿前向展开，边缘碎尘快速消失，护盾由真实护盾状态呈现。脚点定义地面命中带，胸前层只作冲击亮核，不扩大真实命中范围；此版全带物理伤害，不照搬原作中央真伤。
- 观察：机器人Q从身体射出手爪，连续绳链连接源头，命中后目标沿链向源头移动；空钩伸展后回收。项目使用独立金属爪几何、双线绳索与分段铰接，飞行/回收头位置跟随真实事件插值，抓住后跟随被拖者的可见身体；不是瞬移或能量球。0.3秒起手、每秒10格飞行、0.6秒回拖均为项目暂值。首个敌人挡钩、首领不被拖动由逻辑决定。
- 观察：低血屏障是贴近身体的闭合罩，在连续受击中保持，耗尽后消退。项目复用已存在的护盾视觉/护盾条，只在实际盾账本大于0时保持；不根据角色id额外演一层假盾。

制作依据：方向与阶段据上述真实演示拆分；金属爪/链节用独立场景中的Line2D/Polygon2D，拳击用256基准面shader的边界、纵向冲击与离散尘迹分层。共用VfxPlayer时钟、暂停及减少动态接口。角色动作查看供体攻击阶段图后选择风刃指挥官（近战刀击）、太阳之拳（近身拳击）、Invincibuddy（金属重臂）；放弃投石者和持斧钢魔像。供体只复制选定png与本地化frames资源。正常速度正式BattleScreen核对结果归[活动任务](../../../work-items/active/duel-grit-hook-heroes.md)。

## P01-S46：大型冲阵的身体推进与两侧让位（2026-09-19）

来源：[Riot塞恩官方页](https://www.leagueoflegends.com/en-us/champions/sion/)及从该页取得的[R技能片段](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0014/ability_0014_R1.mp4)。浏览器首次导航超时后读取官方页面媒体地址，提取完整片段每1/3秒阶段图并查看；原片与阶段图保存在`.godot/trample-review/reference/`。约0.7–1.0秒起势，1.3–8.0秒身体向前持续推进，暖色前端与向后的红色拉丝同向移动；约8.3秒接触目标出现短爆发，随后身体恢复普通动作。官方片段是持续冲锋／接触收势的动作依据，未据此证明原技能把所有身体向两侧推开，原作数值和控制免疫不迁入。

本项目适配由用户明确要求：大体型、贯穿行进、沿路两侧撞开。中心为巨兽脚点；锁定的地面方向为主轴，预警的左右边界对应真实身体半径，推动目标沿主轴法向离开。1.2秒预警逐渐增强，正式位移开始后预警消失、肩前压缩气流与向后短尘尾跟随可见身体；被撞者由共享位移呈现播放横向退开及短擦痕，收招1秒。箭形地面指示／肩前薄气流／稀疏尘尾分别为项目新适配，不复用水波圆环，也不照搬塞恩的环形终点爆炸。

素材规格：既有`f5_tank`独立动画资源用于大型精英场景，VisualScale=2.1、身体半径0.9；不修改基础蛮兽的尺寸。新预警／冲锋为独立256基准面的shader场景，按战场双轴投影和同一半径设置空间，运动仅使用共享播放时钟，减少动态时停止纹理流动。技能资源分别引用两项VFX，特效不反向驱动碰撞或伤害。

动作核对：正式BattleScreen隔离预设录制正常速度，首轮发现指示箭头反向，修正后再次录制；查看阶段序列并在本地浏览器以1倍速度播放，核对方向、连续推进和两侧落位。真实暂停按钮输入冻结逻辑及特效，结束清理通过。录制与未验收边界归[大型冲锋任务](../../../work-items/active/enemy-trample-charge.md)，这不是用户观感认可。

## P01-S41–S45：idle 施法的身体覆盖与爆发候选（2026-09-16）

用户明确排除现有库作为最终方案。本轮检索 Google `site:realtimevfx.com buff character aura effect breakdown` 与 RTVFX 公开搜索 `buff effect`；Google 正文要求 JS，浏览器导航/正文读取多次超时。转用论坛公开 JSON 读取以下作者原文和演示地址，未下载媒体或工程。**本轮新参考的动态观察未完成；下面作者说明与项目推论分开，不能据标题或链接声称已逐帧观看。** 原文缓存位于 `.godot/idle-cast-review/reference-*.json`。

| 证据 | 原始来源及作者说明 | 对本需求的价值和边界 |
| --- | --- | --- |
| P01-S41 | [SHA3DOW：Strength Buff FX](https://realtimevfx.com/t/strength-buff-fx/31395)。ARPG 力量增益，作者明确目标是有力度且保持角色可辨认，仍在迭代时序；第二帖说明地面破坏由 Houdini 模拟后输出 VAT。[原始动图](https://realtimevfx.com/uploads/default/original/3X/b/9/b964e6cb42bb0fe3c01ee539bf703dfe21dafa7c.gif)。 | 最贴近力量增益爆发的候选；不能把地面破坏当血鼓必需元素，不能据作者目标断言本项目大小下仍清晰。角色本身是否有施法动作尚未核验。 |
| P01-S42 | [Ilya_2021：Sketch #59 Light](https://realtimevfx.com/t/ilya-2021-sketch-59-light/24528/4)。第4帖明确贴近角色的能量聚集使用简单网格，叠加 masks、dissolve、Fresnel、distortion；第14帖补烟雾序列作为次层。第19–20帖讨论后期过强，作者认同早期版本更适合治疗/增益。[早期更新视频](https://www.youtube.com/watch?v=Oo9P8WYvVIc)，[后续柔和版本](https://www.youtube.com/watch?v=zYgCIK2GMho)，[最终版](https://www.youtube.com/watch?v=aPOgIABQQo0)。 | 提供贴身覆盖、聚集到释放及溶解分层的明确制作依据。2D 能量罩/前景层是项目适配，不是原作者公开了 Godot 或精灵方案。需要比较早期和最终版，不能照搬强伤害风格。 |
| P01-S43 | [Recklol：Magic buff，第8帖](https://realtimevfx.com/t/recklols-vfx-sketchbook/6419/8)。作者明确是 magic buff 并提供拆解图与[视频](https://youtu.be/j2-J04zzqC8)；[拆解图](https://realtimevfx.com/uploads/default/original/2X/e/e591fba12dffdca5ecab21abfce9b640a8639752.jpeg)。 | 有针对性的增益候选。图中文字和动态尚未实际查看，不臆称其中具体网格数、运动方向或角色姿势。 |
| P01-S44 | [角色材质覆盖与坐标讨论](https://realtimevfx.com/t/i-need-to-put-a-world-space-shader-to-a-character-like-a-buff-effect-but-following-the-uvs-is-a-bit-of-a-problem/19914)。提问者需要覆盖角色且图案跟随角色；回复建议 UE 的 pre-skinned local bounding box UV，作者确认解决。 | 支持身体覆盖层应跟随角色而非世界纹理滑动。对 Godot 2D 可探索按当前帧 alpha 限定的材质扫光；这属于本项目技术推论，无成品动作演示，也不能直接照搬 3D Fresnel。 |
| P01-S45 | [Hekaite：Need help with this buff effect](https://realtimevfx.com/t/need-help-with-this-buff-effect/30142)。作者区分 burst、duration、结束前3秒闪烁，仍求助爆发不足并计划改 spiral meshes；[视频](https://youtu.be/maUWdyGCCh4)。 | 说明增益的释放瞬间和持续阶段要分开；属于未完成习作，不作为已解决的最佳案例，不将末段闪烁或持续时间带进本项目。 |

此前 P01-S21 的手绘 aura 证据仍有效，可解释前景覆盖提升体积，但它是持续光环，且曾被适配成现有库；本轮不以重复它代替用户要求的新参考。尚无证据证明以上作品全部使用纯 idle，也未核验 DNF 的具体技能动作；此次推荐只针对效果覆盖和释放方法。

制作轮补充：用户随后要求实施。浏览器在 S41 原始 GIF 成功取得站立、张臂伴随红色脚下轮廓与向上细束等离散画面，但未完成有序全程动态观察；不据此编造时序参数。S42/S43 视频仍未完成观看。工程首版依据 S21 已观察的前景覆盖/向上分离与 S42 的作者分层拆解，采用原创短促薄片/上升柔带；2D帧轮廓、时长和强度为项目适配。具体动作、资源、前后正常速度录制和验证记录归现有 VFX 活动任务，不能将实现完成写成参考观看或用户审美通过。

## P01-S37–S39：贯阵弩手蓄力/发射的官方候选片段（2026-09-16）

承接P01-D09。已通过HTTP读取下面三个Riot官方英雄页面，核对技能名、说明和页面实际引用的独立MP4；三个视频HEAD均返回200/video/mp4。没有下载远程媒体。浏览器中文入口发生地区重定向，页面和独立视频的浏览器操作均超时；本轮**没有成功观看动态、逐帧分析或读取作者制作拆解**。下表只区分官方文字支持的技能类型及待观察问题，不把预期视觉写成实际观察。

| 编号 | 官方来源和片段 | 已核对的文字事实 | 供用户确认/后续观察的重点 |
| --- | --- | --- | --- |
| P01-S37 | [Lux官方页](https://www.leagueoflegends.com/en-us/champions/lux/)；[R Final Spark独立短片](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0099/ability_0099_R1.mp4) | 官方说明聚集能量后发射光束，对范围内目标造成伤害。 | 作为聚能后直线光束的候选；需实际观察方向预告、源端聚能、亮度峰值及光束出现/消退，不能据文字声称具体颜色/宽度/时长。 |
| P01-S38 | [Varus官方页](https://www.leagueoflegends.com/en-us/champions/varus/)；[Q Piercing Arrow独立短片](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0110/ability_0110_Q1.mp4) | 官方说明准备后射出强力一击，准备越久射程和伤害越高。 | 作为蓄力弓箭/贯穿投射物候选；重点确认用户是否要可辨识的飞行前端、尾迹和沿路命中。项目不因此采用原作的可变蓄力/射程/伤害规则。 |
| P01-S39 | [Xerath官方页](https://www.leagueoflegends.com/en-us/champions/xerath/)；[Q Arcanopulse独立短片](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0101/ability_0101_Q1.mp4) | 官方说明发射长距离能量光束，伤害命中的所有目标。 | 作为奥术能量束的候选，比较其源端动作和光束形体；当前页面短说明不提供预警/充能亮度的精确变化。 |

页面中的原作附加效果不进入ES01玩法。跨阶段亮度、发射物长度/速度、线宽、判定时刻及素材层次均待动态观察和项目适配。本轮没有原作纹理/shader/粒子实现证据，没有生产样板或审美验收。浏览器面板打开Varus片段的请求返回queued，不能据此称用户已看到或播放器已成功播放。

## P01-S34：英雄位移的身体动作与空间参考（2026-09-12）

用户要求把冲锋、跳跃、闪现、击退、拉拽和聚怪做进具体英雄。本轮先读取官方英雄页公开媒体，再查看六段视频各自每 0.25 秒的完整阶段图；这属于参考观察，不是本项目运行或审美验收。页面与精确媒体链接保存在 `.godot/displacement-reference/sources.json`，原视频、阶段图与完整制作说明同目录。

| 动作／来源 | 实际观察 | 本项目适配、分层与边界 |
|---|---|---|
| 冲锋：[Gragas E](https://www.leagueoflegends.com/en-us/champions/gragas/)／[官方片段](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0079/ability_0079_E1.mp4) | 约 0.75–1.0 秒身体朝目标快速前冲；1.25 秒接触处尘屑出现，目标头顶控制标记独立延续。 | 以移动者脚点为中心、沿起点到权威终点的地面线推进，现有跑动帧为主；接触或碰阻停止，实际命中与后续控制各自消费事实。可复用接触 `impact`，不以拖尾伪造额外路径。 |
| 跳跃：[Tristana W](https://www.leagueoflegends.com/en-us/champions/tristana/)／[官方片段](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0018/ability_0018_W1.mp4) | 约 1.25 秒起点地面亮、角色明显升离地面；1.5 秒在目标上方，1.75 秒落点产生径向冲击，角色之后留在落点旁。 | 地面根节点沿真实路径，只有角色 sprite 上升、越顶、下降，选择位置与血条留在地面；身体护盾／状态使用独立身体挂点跟随高度。`4p(1-p)×高度` 是项目解析弧，不是原作公式；实际落地可用既有 `burst`。不复制火箭燃焰、不声称有专门起跳骨骼帧。 |
| 闪现：[Katarina E](https://www.leagueoflegends.com/en-us/champions/katarina/)／[官方片段](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0055/ability_0055_E1.mp4) | 1.25 秒仍在远点，1.5 秒已在敌人近旁，两处变化分离；粗采样未见持续行走的中间身体，不能据此证明精确消隐帧数。 | 闪现语义来自当前用户要求：短前摇后完成事实当帧 snap 至合法终点，清除走路插值，途中没有身体扫过。原有 `teleport` 的前后壳层适用，必须按真实起手／Fired 事件播放，不能用它的自动 0.42 秒前摇延迟玩法换位。壳层制作依据另见 S33。 |
| 击退：[Tristana R](https://www.leagueoflegends.com/en-us/champions/tristana/)／[官方片段](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0018/ability_0018_R1.mp4) | 约 1.0 秒炮口亮，1.25 秒接触，1.5 秒目标沿远离施放者方向快速后退；接触碎屑留后散。 | 移动者是受击目标，方向为施法来源到受击者；地面权威推离线配持有的受击姿态，碰阻提前停下。复用真实命中 `impact`，不凭参考炮弹给当前近身盾击添加远程弹道。 |
| 拉拽：[Blitzcrank Q](https://www.leagueoflegends.com/en-us/champions/blitzcrank/)／[官方片段](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0053/ability_0053_Q1.mp4) | 0.75 秒端头和链向外伸，1.0 秒接触远处目标，1.25 秒目标回到施放者近侧、链缩短；控制标记随后独立保持。 | 目标脚点沿向内地面线移动，停在施放者附近合法空位，不能覆盖其身体；施法、牵引、到达／取消分开，受击者不翻成主动往回跑。当前未制作实体钩索素材，先以实际牵引／受击姿态表达，不把汲取光束换色冒充铁链。 |
| 聚怪：[Orianna R](https://www.leagueoflegends.com/en-us/champions/orianna/)／[官方片段](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0061/ability_0061_R1.mp4) | 约 0.75 秒以球体为中心出现提示，1.0 秒球体亮，1.25 秒不同方向目标同时向该中心移动，1.5 秒已聚在近处；施放者仍在右侧。 | 原作使用独立球体中心；HC36 当前稿明确固定在施放者施放时地面位置，施放者不动。目标分别沿自身径向向中心附近空位靠拢，保留身体间隔。既有 `wind_vortex` 的旋卷体分层可服务固定中心，特效本身不选择／搬运敌人，范围取同一玩法查询。 |

实现适配：`BattleDisplacementCue` 保留本次起点／终点、开始 tick、总 tick 与进度，事件位置为当前实际地面位置。一般移动仅在已收到的相邻权威采样间插值，完成采样也走完后清理；闪现及暂停单步直接采样权威位置。跳跃受控后的 `Cancelled` 只取消落地收益，`Finished` 前仍沿安全下降继续；死亡、离场和身份重建沿既有清理路径。真实 simulationSpeed 为界面倍率乘 0.8，暂停冻结；减少动态压低附加弧高，不改变逻辑位移。角色根缩放只应用一次，身体特效和地面特效分别注入挂点，地面阴影若内容本来拥有则不抬高；当前英雄没有新增阴影素材。

当前绑定：闪现实际接入共享 `teleport`，按起手事实开启，成功换位发送 Fired／Impact 进入双端收尾，取消直接移除；前摇倍率按其 authored CastDuration 与位移 tick 时长计算。冲锋、跳跃、击退成功结束后用共享 `impact` 显示局部接触，在非暂停状态等待末段插值的一个 tick，使光点随可见抵达；它不充当范围圈。聚怪实际接入共享 `wind_vortex`，以施放时冻结的 EffectCenter 和真实查询半径播放；同次施法多个移动者共用一实例，全部结束／取消后释放。没有复制特效场景或按英雄 id 特判。盾壳与持续身体状态逐渲染帧读取身体挂点，地面状态继续读取脚点。终局进入结果停留前统一清除位移插值、角色高度、旋风组和待播接触，不能因逻辑 scope 清账而让角色留在半空。

素材与未知：本轮没有生成新角色姿态、残影、钩索或全屏光效，只组合既有跑动／施法／受击帧与语义匹配的共享效果。原作内部 mesh、shader、精确缓动、位移时长和无敌／地形规则未由本次观察确定，项目数值不是原作事实。依用户“不用验证”的当前边界，未构建、启动 Godot、运行测试或录制当前战斗；代码实现和参考阶段图不等于观感通过。

## P01-S36：主动施法者短光（2026-09-16）

用户要求远程技能的施法者也应有简短反馈。复用 S33 已观察的 [Gangplank W 官方演示](https://www.leagueoflegends.com/en-us/champions/gangplank/)中“腰腹局部亮起、少量延后细尘”的身体端分层，以及现有 `arcane_missile.tscn` 源端独立亮点轨道的制作方式。参考本身是净化；这里只借鉴局部身体亮度与细尘层次，不声称其动作就是远程施法，也不复制上掠光弧或净化含义。

项目适配：中心为可见身体挂点，平面为朝向镜头的 sprite，无地面范围／攻击朝向；在成功主动施法事实当帧开始，约 0.03 秒起亮、0.32 秒主体结束，六枚光点在 0.035 秒后以短距离向外散开并轻微上浮，0.48 秒全部清理。局部浅蓝白柔光负责识别施法者，细光点负责短促释放；不伪造前摇或延迟技能结算。采用共享 `surface.tres`＋`element_sparkle.gdshader` 的柔光和 `ElementDust.tscn` 点粒子，不需新图，不用多边形线框，不混用目标治疗加号或向内聚集的回蓝效果。该时长、数量与色彩是项目适配值，不是对参考的逐帧测量。正式战斗与预览共用 `spell_cast` 资源与共享时钟。

## P01-S33：常用动作扩充与日漫造型复核（2026-09-12）

- **已观察的动作资料**：[岩刺](https://realtimevfx.com/t/vfx-spike-attack-riot-style-using-popcorn-fx/802) 为地表先行、依次爆刺、碎块收尾；[风卷分层](https://realtimevfx.com/t/thermal-amp-sketch-12/4700) 与 [制作说明](https://realtimevfx.com/t/veer-sketch-12/4776) 为窄根宽顶偏心风体、独立顶环/风裙/碎尘，作者说明 mesh、panning noise、alpha erosion、vertex offset。[束缚](https://realtimevfx.com/t/harry-halis-alisavakis-sketch-25-root/9683) 作者说明预弯曲圆柱UV生长与尖端收窄，实际片段为脚边根圈长出后固定。[2D传送](https://realtimevfx.com/t/2d-teleportation-unity/4170) 可见分块壳层、亮峰和独立余屑，未展示完整角色位移。
- **进攻细分**：[Athena突刺](https://realtimevfx.com/t/wip-help-athena-thrust-how-to-strengthen-the-motion/21161) 观察尖锋快速贯出后制动、尾形消退，作者/评论讨论后向光痕过长的问题；[连结电弧](https://realtimevfx.com/t/unity-vfx-chain-lightning-effect/27281) 观察真实端点连接、接点短亮、余电收尾。官方 [Katarina](https://www.leagueoflegends.com/en-us/champions/katarina/) R 片段观察来源中心持续短弧与独立范围；项目不引入参考的小刀目标或新命中。官方 [Fiddlesticks](https://www.leagueoflegends.com/en-us/champions/fiddlesticks/) W 观察保持的两端暗红链接和末端脉冲，无法从阶段图证明原作颗粒方向；项目 Target→Source 明确作为汲取语义适配。
- **辅助与状态**：官方 [Soraka](https://www.leagueoflegends.com/en-us/champions/soraka/) E 观察头顶独立封声符号；[Gangplank](https://www.leagueoflegends.com/en-us/champions/gangplank/) W 观察腰腹亮、侧向上掠光弧和延后细尘。取其小范围身体净化，不复制角色吃橘子或扩为大光柱。奥术抛射/治疗加号/持续冰封复用既有抛物线、用户认可的独立治疗粒子与 S24 生长/碎裂分层，持续冰封另设保持阶段，不复用爆裂到期。
- **日漫美术，只作静态依据**：实际查看 [《地狱乐》12话剧照](https://www.jigokuraku.com/story/) 的粗根细枝/负空间，[《鬼灭之刃》3话剧照](https://kimetsu.com/anime/risshihen/story/?story=3) 的岩体大明暗与局部低频纹理，[《钢之炼金术师》2003版官网](https://www.hagaren.jp/story/) 1–2话的硬面阴影与独立光层。没有看过对应整集或连续动画，不作时序证据；无法访问的其他日漫不列作已参考。由此修正岩柱晶体般长直剪影，改厚重、不对称断块；藤蔓采用粗细分面与独立弱光，拒绝写实密纹和固定雾团。
- **项目适配与证据边界**：16项的具体时长、数量、颜色是项目参数。连轰/雷暴是有固定落点的视觉节奏，只消费 Source/Target/Radius，不自动寻找/制造多个真实受击者。没有角色位移、回复、伤害或新状态逻辑。参考媒体只用于研究，不成为运行资产；运行纹理由内置生图提供原始RGBA。详细观察、阶段图和制作前动作说明保存在当前任务 visualizations 的 library-expansion/research-offense 与 research-controls（含 anime-direction.md）。实现、验证和恢复见活动任务。

## P01-S32：嘲讽范围与受控标记（2026-09-12）

- [Riot 官方加里奥页面](https://www.leagueoflegends.com/en-us/champions/galio/)与页面公开 [W 演示](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0003/ability_0003_W1.mp4)：已下载研究用视频并拆看约 7 秒的 14 个阶段。约 1–2.5 秒可见施放者为中心的圆形边界逐渐扩大；约 3 秒释放、范围波收尾，目标身上出现独立红橙色环状控制标记，目标靠近施放者后标记仍保留，约 5.5 秒已消失。只是所观察片段，不推定版本数值、内部 shader 或精确控制时长。
- 范围层采用该片的中心／地面圆形边界和瞬时释放波关系；持续负面状态采用“独立于范围圈、跟随受控目标”的关系。HC03 当前即时施放，不引入原作蓄力、伤害、角色动作或资产；将目标标记适配为头顶四瓣怒意符号，属于本项目造型选择，不能声称官方演示就是该符号。
- 制作分工复用已查证 P01-S12 的主次／范围准确／预备主体消退原则和 S31 小型符号独立运动拆解：本次新增独立怒意贴图，范围边界／声压回波分别用局部 shader，持续符号用共享时钟驱动；不将整套效果画成一张图。范围读取实际查询半径；瞬时效果冻结施放点，持续效果跟随权威状态和可见单位位置。具体阶段／数值见现有 VFX 活动任务。
- 研究材料在 `.godot/taunt-vfx/reference/galio-w.mp4`、`galio-*.png`，仅作参考，不进入运行时资源。浏览器搜索超时后读取官方公开页面及其直接媒体，未声称完成其他搜索结果的动态观察。

## 2026-09-11：整批素材修订的项目适配与渲染证据

- 本轮沿用 S17、S19、S21、S23–27、S29–31 已有动作拆解与此前用户反馈；没有新增商业视频观察，也不把新素材外观称为参考原作实现。生图提供独立组件，现有轨道、网格和生命周期承担运动。
- 本地渲染发现并修正：新刀光较亮的贴图被尾迹 UV 位移带到网格端部，出现矩形端盖，增补网格端部渐隐；中性烟团直接用于爆炸会读成白云，FireAsh 独立压暗暖化；强化新光羽在浅底加色叠加过白，主体改 alpha 混合，脚底光继续独立加色。眩晕减小/下移轨道，3 枚细卷纹与主星同速错相，避免异速追赶；尺寸、节奏幅度与材质阈值均为本项目选择。
- 10 张生图原件通过 RGBA/alpha/源文件哈希核对，来源和完整提示词归 assets/vfx/refresh/README.md。法阵和盾壳生图遇到连接/解码失败，额外法阵候选烘焙棋盘格而未接入；两者保留旧图调整 shader，不把失败请求计作新资源。
- 动态证据保存在当前任务 visualizations/vfx-refresh：before 与 after 为同一预览、1600×900、60 Hz 序列，review 内是正常速度左右对照；另有浅底减少动态与共用资源序列。已查看关键阶段及眩晕 12 时点位置变化；自动检查通过不代表用户审美认可，未做真实输入或正式战斗验收。

## 2026-09-08：强化与毒沼动作参考

- [P01-S21：Making hand-drawn auras for a fighting game](https://realtimevfx.com/t/making-hand-drawn-auras-for-a-fighting-game-feedback-appreciated/2576)，已读作者与反馈正文，取得第 6 楼公开 GIF（480×248、45 帧、2.99 秒），查看 0/0.40/0.73/1.07/1.47/1.87/2.20/2.53 秒画面。实际观察是脚边前景+身后主体形成归属，上方尖端不断脱离成小形体；作者说明是 2D 手绘动画并补了前景火焰改善体积。它支持分层归属、上升与分离，不能证明 DNF 强化具体动画，也不要求采用大面积持续火焰。本项目短促金色光簇来自用户提出的表达方向，使用独立纹理粒子是项目适配。
- [P01-S22：Ryan Dziurgot 的 VFX Journal，第 7–11 楼](https://realtimevfx.com/t/vfx-journal-ryan-dziurgot/3000/7)，已读正文并取得第 10 楼 GIF（500×500、172 帧、10.32 秒），查看 0–9 秒连续阶段。观察到地面绿色液体流动、较亮泡体出现后破裂、主体先收起而零星碎滴后消失。第 9 楼反馈明确要求泡体先“exploded”再 dissolve，作者随后更新并提到实现出生时序困难；不能由此推定作者具体 shader 或完整工程。本项目采用固定液面位置的鼓起→破裂碎滴→涟漪解析周期，未复制参考的空心圆环或作者资产。
- 检索入口为 Real-Time VFX 公开搜索 JSON 和原帖 JSON。buff effect breakdown/toxic puddle 查询返回 429，未持续重试；power up aura、poison pool 找到上述可直接查看的动图。8053 的 Jump Force 风格习作与 6419 的 magic buff 视频入口仅作候选，未据未观看视频写动作结论。
- 原帖 JSON、公开参考 GIF、阶段帧和链接记录留在当前 visualizations 的 status-study；不加入项目资产。制作前动作说明、实施调整和新旧引擎动态对照归现有活动任务。用户只评价此前椭圆横扫“勉强可以”，本次强化/毒沼尚未获审美认可。

## 2026-09-08：用户“试一下”后的横扫动态取证

- 本轮取得 P01-S17 [Venom Slash 原帖](https://realtimevfx.com/t/venom-slash-breakdown-of-the-effect-included/18903) 的公开 GIF，并逐帧查看首次挥动 0.03–0.69 秒：约 0.09 秒可见弧面，0.15–0.21 秒亮部沿圆周推进，约 0.27 秒主面退去，飞溅继续停留在路径附近。这些时间来自 GIF 采样，只是观察证据，不是作者公开参数。
- 原始 GIF 为 300×205、302 帧、约 9.15 秒，含多次挥动；来源正文/链接存于本地研究输出 cleave-study/source.json，原图及首次挥动关键帧分别为 reference.gif、reference-first-sweep.png。仅作研究，未作为项目运行时资产。
- 作者说明的静态环形网格、中心遮罩、滚动纹理、辉光与独立飞溅，与已观察到的圆周推进及主次层收尾对应。参考无角色，不能证明角色挂点/动作同步，也不能据此认定恰好 180°。本项目“施放者中心、指定前向 180°”来自用户；0.30 秒挥出/0.24 秒局部尾迹是项目样板选择。
- 据此先形成动作说明，再实施横扫空间载体与分层材质；实现/检查/同视角新旧动态输出见 work-items/active/combat-vfx-and-preview.md。本次没有补完 DNF 强化动画观察，也没有认可其他被否定样例。

## 2026-09-08：D05 逐项检索的前序进度与缺口

- 复核 P01-S17 作者正文：Venom Slash 明确使用静态环形网格、中心 alpha mask、两纹理相乘、其中一层滚动、色带/辉光及独立飞溅。它提供了可拆解的空间载体和材质运动依据；不证明本项目所需的施放者中心/前向 180° 已实现，也没有给出可直接抄用的时长。
- [DFO World Wiki：Overdrive](https://wiki.dfo.world/view/Overdrive) 正文可读，内容是武器强化/属性与施放时间等玩法说明，页面显示最后编辑于 2021-05-16。未取得有效的实际动画观察，不能从这些数据推导强化光簇的形状、方向和层次。
- [Recklol 的 magic buff 拆解帖子](https://realtimevfx.com/t/recklols-vfx-sketchbook/6419/8) 作者确实提供了 [视频](https://youtu.be/j2-J04zzqC8) 和拆解图；目前仅确认入口，尚未观看，不将其构成或运动写成已验证结论。
- 浏览器创建参考页再次超时；改用只读 HTTP 获取文字。Bing 的 `game vfx melee slash 180 degree mesh breakdown` 返回低相关结果，未采用；Real-Time VFX 站内搜索 `buff effect breakdown` 找到上述作者帖。猜测的 Diablo slash 帖子 URL 返回 404，不作为来源。本轮未下载参考美术或修改运行时，完整动作描述仍待动态证据补齐。

2026-09-06：仅静态读取本仓库，没有构建、运行、外部素材调研或体验验收。

| 当前入口 | 已有能力 | 缺口 |
| --- | --- | --- |
| `src/Presentation/RangedAttackLayer.cs`、`RangedAttackVisual.cs` | 3 个导出场景，字符串远程事件分派；Projectile/Beam/Impact，暂停倍速、清理 | 类型与具体节点结构耦合；没有通用目录／能力声明，绑定 BattleBoard，身体偏移固定为 24 像素 |
| `src/Battle/BattleProjectiles.cs` | 权威子弹实体及碰撞 | 属于玩法实体，不能搬进 VFX 生命周期或用粒子碰撞替代 |
| `src/Abilities/Authoring/AbilityPresentationSpec.cs` | SemanticIcon、Cue、ReportLabel | Cue 主要表达角色动画；没有技能分阶段特效绑定 |
| `src/Statuses/Authoring/StatusPresentationSpec.cs`、`BattleStatusScope.cs` | Executed／OnActive／WhileActive／Removed 提示及实例快照 | 已有可复用的生命周期，未连接持续特效播放器；不能每次 WhileActive 创建一个新节点 |
| `src/Effects/Authoring/EffectPresentationSpec.cs` | 名称、报表和 Cue | Effects 是玩法效果模块，不能与视觉系统混用职责 |
| `src/Battle/BattleCombatEventPipeline.cs`、`BattleSimulation.cs` | 类型化技能结算、伤害、治疗、护盾等事实；SubjectStableId、源目标及位置 | 需按具体操作核对施放关联、实际目标／区域、持续实例与结束原因；不能推定已经存在完整的施法开始／护盾破碎事件 |
| `tests/RangedAttackVisualPreview.cs` | 使用真实场景的隔离截图预览 | 硬编码三个事件，无目录选择／正式前台入口，不是预览室 |

参考现行系统权威的战斗与表现职责。上述结论不声称所有能力事件完整，也不把讨论区未合并的纯自动／空间方向提前套进实现。

## 公开资料对照（2026-09-06，用户授权专项搜集）

以下资料已实际获取正文。引擎文档证明引擎能力；游戏开发者文章证明公开工作方法；工具采用名单仅证明采用，不证明某个具体技能的内部实现。未运行外部软件、安装插件、制作素材或改项目实现。

### 来源与可迁移结论

| ID | 来源与版本边界 | 实际内容／原文线索 | 对本项目的意义与限制 |
| --- | --- | --- | --- |
| P01-S01 | [Epic：AGameplayCueNotify_Actor](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/GameplayAbilities/AGameplayCueNotify_Actor)，读取页面标示 UE 5.8 | 实例可维护状态并 tick；OnActive／OnExecute／WhileActive／OnRemove；按 instigator/source object 决定唯一实例；AutoDestroyDelay、Recycle、ReuseAfterRecycle。WhileActive 为“first seen as active”，不是每帧回调。 | 支持有状态持续效果、来源归属、延后清理与重建。借生命周期和归属思想，不移植 GAS 网络复制／预测或强制复刻类层级。 |
| P01-S02 | [Epic：UGameplayCueNotify_Static](https://dev.epicgames.com/documentation/en-us/unreal-engine/API/Plugins/GameplayAbilities/UGameplayCueNotify_Static)，读取页面标示 UE 5.8 | 非实例化 handler，适用于 one-off burst effects。 | 区分一次性触发与需持有状态的效果；这不意味着 Godot 的视觉场景不必实例化。 |
| P01-S03 | [Unity：Visual Effect Component](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/manual/VisualEffectComponent.html)，包 17.0.4 | 组件基于 Graph Asset 创建真实实例；Exposed Properties 与 Rendering Properties 按实例设置；播放控制包括 Stop、Play/Pause、Step、Restart、Rate、Event Tester。 | 预览直接控制真实实例；资产默认值、实例覆盖和暂停／重播各有职责。独立前台预览仍是本项目的选择，不声称 Unity 提供相同产品入口。 |
| P01-S04 | [Unity：Property Binders](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/manual/PropertyBinders.html)，包 17.0.4 | 将 scene/gameplay values 连接到实例公开属性；Sphere binder 同步位置／半径；IsValid 后才 UpdateBinding。 | 正式与预览都绑定来源、目标、半径等同类型上下文；参数须校验，不能只维护任意字符串到值的字典。 |
| P01-S05 | [Unity：Visual Effect Graph window](https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/manual/VisualEffectGraphWindow.html)，包 17.0.4 | 可将打开的 Graph 附着到真实 GameObject，控制对应实例；提供示例与 Output Event helpers。 | 支持共享资产和真实实例预览。输出事件可协调音效等表现，但本项目不能用它结算伤害。未实际运行 Unity 样例。 |
| P01-S06 | [Riot：Clarity in League](https://www.leagueoflegends.com/en-us/news/dev/clarity-in-league/)，2021-03-12 | 三原则：传达玩法、保持视觉层级、减少噪声；举 Zoe Q、Taric 晕眩、Kayle 大招；效果应匹配 hitbox，投射物静止时也应能看出方向；法术护盾保持既有形状语言。 | 高质量并非每个技能都加最亮最多粒子。预览应检查有效范围、朝向、重要性与叠加场景；该文不公开 Riot 的内部播放器架构。 |
| P01-S07 | [Riot：Behind the Scenes of VFX Updates](https://www.leagueoflegends.com/en-us/news/dev/dev-behind-the-scenes-of-vfx-updates/)，2022-04-25，Riot Sirhaian／Riot Riru | 先修错误／缺失命中范围，再改善辨识／噪声和视觉强度，再增强主题；先基础效果反复反馈，通过后推广皮肤；接受命中范围、可见性等具体反馈。 | 首批做高质量基础样板后扩展；验收用具体可观察结果。不可用“清晰”当借口交付用户拒绝的低质量线条。 |
| P01-S08 | [Effekseer 官网](https://effekseer.github.io/en/)与[采用名单](https://effekseer.github.io/en/adoption.html) | 专用开源粒子特效工具，可导出二维动画和实时效果；采用名单列出 THE KING OF FIGHTERS XV、SAMURAI SPIRITS、RPG Maker MZ 等。 | 有商业游戏采用的制作工具可作为候选；名单不证明所有效果均由它制作，也不保证我们的美术质量。 |
| P01-S09 | [Effekseer Recorder](https://effekseer.github.io/Help_Tool/en/ToolReference/record.html) | 导出 sprite sheet／独立图片／GIF／AVI，可选帧区间和频率；支持透明背景，以及分离 Blend+Add 输出以改善不同背景下的还原。 | 斩击、爆炸可离线烘焙，Godot 正式和预览消费同一序列帧；烘焙固定了部分视角／变化，不适合把可变护盾受击方向全部烘死。 |
| P01-S10 | [EffekseerForGodot4 仓库](https://github.com/effekseer/EffekseerForGodot4)、[使用说明](https://effekseer.github.io/Help_Godot/v4/en/how-to-use.html)、[限制](https://effekseer.github.io/Help_Godot/v4/en/introduction.html)、[1.80.5.1 release](https://github.com/effekseer/EffekseerForGodot4/releases/tag/1.80.5.1) | efkefc 导入为 Godot 资源；Emitter2D 持有 Effect，提供 Paused／Speed／Color，Inspector 能预览同一资源。Release 2026-07-10 明确修复 Godot 4.7 脚本错误。2D 文档列出 depth test／soft particle、normal／tangent 等限制。 | 可做专用运行时适配器，但需要 GDExtension 和平台验证。说明页还保留“Godot 4.0 is not supported yet”及 Godot3 图片，故不可据旧版本段落推断当前 4.7 不可用；release 也不证明所有功能兼容。项目当前 gl_compatibility 未验证，不能直接承诺接入。 |
| P01-S11 | [Godot：2D particle systems](https://docs.godotengine.org/en/stable/tutorials/2d/particle_systems_2d.html)、[官方 2D particles demo](https://github.com/godotengine/godot-demo-projects/tree/master/2d/particles) | 粒子支持单张纹理和 flipbook；官方说明可用 Blender／EmberGen 预渲染复杂烟火；使用 CanvasItemMaterial 配置动画帧。示例 README 使用 GPUParticles2D／ParticleProcessMaterial，渲染器 Mobile。 | 原生 Godot 路线同样能消费高质量烘焙素材，并非必须画几何线条。原始文档有 article_outdated 标记，demo 也不是本项目 Compatibility 后端，具体功能仍需项目验证；不是成熟商业 Godot 游戏源码证据。 |

### 这轮对草案的实质修正

1. 制作工具和运行时播放器分开决策：Godot 原生纹理／序列帧／粒子可作为基本消费方式；Effekseer 可以仅用于离线制作，也可以另接实时运行时，不要求二选一锁死整个系统。
2. 生命周期补上“恢复已有状态”的语义，不能只处理刚刚生效和结束；实例唯一键／多来源叠加政策明确配置。
3. 预览补上事件测试和单步，但不承诺 GPU 任意回退。实际资源、实例覆盖、运行中的实例必须明确区分。
4. 高质量烘焙要处理混合模式：透明 PNG 不自动保证 Add／Blend 效果在深浅背景一致。预览需要至少可切深浅背景及场景参照。
5. 样板验收同时看美术质量、范围／方向、视觉层级与叠加噪声。这里的具体验收组织是基于 Riot 公开方法的项目建议，不是用户已批准的完整清单。

### 检索及证据限制

- 通用搜索尝试了 Riot clarity/VFX、Unreal GameplayCue、Effekseer Godot 等关键词；Google 返回跳转提示，Bing RSS 返回低相关结果，不将其作为证据。随后直达官方文档、官方 GitHub 和 Riot 原文获取正文。
- Epic Gameplay Cue 总览候选页只返回目录壳；Niagara overview 返回 403，Unity 独立 EventTester 候选页返回 404。这些失败页不作为证据，采用可读的 API、Component 和 Graph 文档。
- tranek/GASDocumentation 可访问但明确自称非官方且对应 UE 5.3；本轮核心生命周期结论已有 Epic API 一手证据，不将该社区资料提升为官方依据。
- 尚未找到并完成审查的“成熟 Godot 商业游戏完整特效系统源码”；不把官方粒子 demo 冒充商业实战架构。也未观看并逐帧分析嵌入视频，Riot 和 Effekseer 结论限于公开正文。

## P01-S12–S20：特效制作方法与素材粒度补充（2026-09-07）

用户要求先学习专业动效知识，随后明确认可生图质量，问题在于生成粒度和系统配合。本轮仅研究和记录，没有修改运行时代码、场景或美术。

| id | 实际读取的来源 | 可核查的内容与适用边界 |
|---|---|---|
| P01-S12 | [Riot VFX Style Guide，37 页](https://nexus.leagueoflegends.com/wp-content/uploads/2017/10/VFX_Styleguide_final_public_hidpjqwx7lqyx0pjj3ss.pdf) | 已提取全文并渲染查看 p6/30/33/35。p6–7 主元素负责用途/焦点，次元素强化主题；p28–30 清晰轮廓、软硬形状搭配、方向性和拖影；p32–36 预备/主体/消退，收尾降低显著性，动态时序优于全程线性，避免无意义停留。p8–9 范围与命中表现准确。指南的手绘、饱和度等是 LoL 风格要求，不直接升级为本项目通用限制。PDF 静态图不是已观看原视频。 |
| P01-S13 | [Riot Art Education：Visual Effects](https://www.riotgames.com/en/artedu/visual-effects) | 正文明确平衡玩法准确、主题一致和吸引力，没有唯一构建方法；推荐 Joseph Gilland《Elemental Magic》、Jason Keyser 和 Real-Time VFX 社区。只读页面正文，没有声称看完其嵌入视频或书籍。 |
| P01-S14 | [VFX Apprentice：Artistic Principles](https://www.vfxapprentice.com/blog/five-artistic-principles-gaming-vfx) | 当前正文已扩充为六项：Gameplay、Shape、Value、Color、Timing、Composition。形状传达含义，明度控制注意，寿命影响噪声；明确参考 Riot 指南。不能把网页标题中的 five 当作当前只有五项。 |
| P01-S15 | [FX Design Principles](https://www.vfxapprentice.com/courses/fx-design-principles)、[FX Timing Principles](https://www.vfxapprentice.com/courses/fx-timing-principles) | 公开课程摘要说明意图、物理属性、构图、预先设计；时序包含关键帧/逐帧、缓入缓出、预备、Impulse/Rhythm、跟随、弧线、Stretch/Smear。仅阅读公开摘要，未购买或观看付费课。不能把摘要等同于完成系统训练。 |
| P01-S16 | [Mia Yang timing interview](https://www.vfxapprentice.com/blog/mia-yang-vfx-interview-timing) | 2026-09-04 公开访谈正文强调研究参考、分析其他效果，timing 不只是一组数学固定值。是方法建议，不是具体游戏内部实现证据。 |
| P01-S17 | [Venom Slash，Geri 制作拆解](https://realtimevfx.com/t/venom-slash-breakdown-of-the-effect-included/18903) | 已读取作者说明：静态环形 mesh + alpha mask；两纹理相乘，其中 Noise1 滚动驱动视觉运动；Color Ramp + Glow；四格 splashes atlas 随机选择，dissolve map 淡出，同组 splashes 复用在地面和刀光。作者使用 All In 1 Vfx Toolkit，并有 Bloom。是具体社区作品/工具案例，不冒充商业游戏源码；未下载付费资产或实际运行项目。 |
| P01-S18 | [Unity Renderer module](https://docs.unity3d.com/Manual/PartSysRendererModule.html) | 文档区分 billboard、速度拉伸 billboard、地面 billboard、mesh 和 trails-only；材质负责着色，渲染对齐/排序/轨迹有独立配置。支持“粒子不是一种固定美术样子，而是发射/运动/寿命系统，可搭配不同渲染载体”的技术分工。Unity 功能不等于已在 Godot 当前后端验证。 |
| P01-S19 | [Magic Shield breakdown](https://www.vfxapprentice.com/courses/magic-shield-breakdown)、[Unity Block-in VFX](https://www.vfxapprentice.com/courses/unity-block-in-vfx) | 公开摘要明确护盾由协同粒子系统组成；Unity 教学按命中时序/对比、治疗2D动画、爆炸mesh particles、导弹trails与Timeline组织。仅作为课程路线和组件组合证据，不声称读取付费课内部参数。 |
| P01-S20 | [Kevin Leroy：League fan VFX study project](https://realtimevfx.com/t/releasing-my-league-vfxs-fan-arts-for-study-purposes/1435) | 作者说明组合 shader/material/texture/particles/mesh/animation，多数效果由角色动画驱动。是 Unity 5.5 同人学习项目，含 Riot 角色资产，作者声明学习用途和复用限制；未下载、未接入，不当作开箱生产库。 |

### 本轮检索限制
- Google 搜索返回跳转页，Bing RSS 返回不相关内容，均未采用。改用 Riot 原文、其推荐社区索引、VFX Apprentice 目录与 Real-Time VFX 搜索 API 追踪实际文章。
- 已阅读专业指南正文和一份具体斩击构成拆解；没有声称已观看并逐帧分析商业游戏视频，也没有声称研究了付费课程完整项目。
- 当前可得结论足以纠正制作顺序与资产划分；每个样板的具体轨迹/时间/亮度仍须选参考并做动态对照，不能从文章直接宣称“已掌握高质量特效”。

## P01-S23–S24：雷击与冰爆动作参考（2026-09-08）

- P01-S23：[Discharge Strike 作者拆解](https://realtimevfx.com/t/discharge-strike-vfx-breakdown/10878)。读取作者正文并查看公开 GIF 阶段帧：作者以 Houdini 静态噪声分支网格、宽度渐变和 UV 电流组织电弧，较短放电与定向主放电分层，另配亮斑、火星和余波。动态可见聚集/分支爆发、定向放电和残留。补看 [Alex Huang Lightning Strike 第 2 楼](https://realtimevfx.com/t/aletsux-sketchbook/31237/2) 7.1 秒公开 GIF：下行接触、径向爆发、碎屑收尾。项目采用分支形体、主次电流及独立接触层；0.05 秒贯通、0.15 秒余电为项目参数，未复制参考长预备或地面破坏。没有声称看过链接中的 Vimeo 拆解或商业游戏源码。
- P01-S24：[Frost Grenade 作者作品与说明](https://realtimevfx.com/t/frost-grenade/6047)。已读作者说明：爆点/法线决定冻结方向遮罩，噪声挤出、切换可破坏体并施加冲量、蓄起与爆裂时序以及光照烟雾；查看 4.92 秒公开 GIF，观察形成/冻结→碎裂→独立冰片与冷雾分别收尾。项目仅采用生长、破裂、碎片惯性和低显著性冷雾的层次，不实现角色冻结或破坏玩法；7 根晶体、16 个碎片、具体时长为项目设计。
- 研究输出位于当前任务 visualizations/element-study：topic JSON、reference-sources.json、三份公开参考 GIF 及阶段图。参考只作研究，没有加入游戏资产或形成运行时外部依赖。
- 制作前动作说明和完成状态见 work-items/active/combat-vfx-and-preview.md。lightning-before-after.gif、frost-before-after.gif 是同视角/尺寸的正常速度引擎录制（左旧右新），已检查最终关键帧和浅底减少动态；隔离行为与构建通过不代表用户已认可视觉质量。

## P01-S25–S26：贴附燃烧与连续喷流（2026-09-09，首轮实施已被否定）

- P01-S25：[Ryan Zeng：Stylized Zelda camp fire breakdown](https://realtimevfx.com/t/stylized-zelda-camp-fire-breakdown/9613)、[作者双语博客](https://ryanzengvfx.blogspot.com/2019/06/zelda-stylize-camp-fire.html)。已读正文并查看 132 帧/8.87 秒公开 GIF 的阶段图。作者分开火星、火体、烟雾；RGBA 分工为内焰、外焰和两路扰动，内焰弱扰动/外焰强扰动，以纵向渐变稳定根部，烟雾依据自身明暗形成消散遮罩。图中火根稳定、外焰持续换形、烟和火星分别移动。是社区作者的 Zelda 风格作品，不是任天堂原作技术揭秘。项目采用根部稳定/上流/分层，并生成自己的单火舌；未复制作者贴图，未实现营火玩法。
- P01-S26：[Flamethrower (Unreal Engine 5)](https://realtimevfx.com/t/flamethrower-unreal-engine-5/30985)。已读作者与评论、查看作者第 8 楼最终 GIF（50 帧/5 秒）。作者自述由约 1600 粒子降至约 60 粒子加一个低模根部网格；可见窄根连续喷射、前端膨开/卷动、火烟分离。评论的指令数/FPS估计没有在本项目验证，不作为预算规则。作者指出 GIF 本身有跳帧，不把它当完整帧率基准。
- 连续喷流动作补证：[Dragon Breath 教程入口](https://realtimevfx.com/t/heres-how-to-create-a-dragon-breath-in-unity/19009)，已看公开 GIF（100 帧/5 秒）的启动、持续和停止后残焰阶段；正文确认 Shader Graph、VFX Graph 和 Krita 纹理，未观看 YouTube 教学。另读取 [Giovanni Chequi 第 8–9 楼](https://realtimevfx.com/t/giovanni-chequi-vfx-sketchbook/20382/8) 的锥体纹理/纵向遮罩说明和 0.97 秒公开 GIF；它是短促锥形爆发，仅作材质/遮罩辅证，不充当持续吐息演示。
- 项目适配：灼烧为 7 根贴附火舌、少量离体火焰/炭烟/余烬；吐息为连续流体与独立下游火舌组合，0.5 秒供给和 0.32 秒传播为项目参数，施法只改供给。素材粒度与动态对照记录在活动任务。fire-study 保存搜索/原文 JSON、作者博客文本、公开 GIF/阶段图和新旧引擎录制，研究图未进入游戏。新两项尚待用户审美验收。

## P01-S27–S28：火尖形变与统一焰体（2026-09-09，统一焰体实施已否定）

- P01-S27：[Ivan Boyko：2d animation “stylized fire” vfx](https://realtimevfx.com/t/2d-animation-stylized-fire-vfx/15248)。作者明确 Adobe Animate、30fps 逐帧手绘，无 shader；已查看四份公开 GIF 的各 12 个阶段，分别 54/64/80/30 帧。可见火尖相对火根侧弯、细颈拉伸后脱离，内外轮廓同时演变。采用的是这组动作关系，不将当前单图 shader 声称为手绘序列帧或已达到参考质量。
- P01-S28：[Shannon McSheehan：LoL FX + Knowledge Share，第 209 楼](https://realtimevfx.com/t/shannon-mcsheehan-lol-fx-knowledge-share/1133/209)。已读作者关于渐变圆、黑色负形遮罩分别上移并循环，导出 flipbook 的说明；未查看该楼构建截图或原商业游戏动画。作为多个形体先合成再着色的依据，不能把项目的密度场实现称为作者原管线。
- 结合 S26 的喷流阶段，项目移除“喷火背景＋横向直立火苗”，以重叠焰团的统一密度/颜色场表现前推、膨开、卷动、断流及残焰。灼烧裁去原图圆肚，加强根部至尖端的传播弯曲，独立小碎焰、暖光、火星。18 个活跃喷流采样、0.55 秒焰团寿命及形变参数均为项目适配，没有真实流体求解。
- 新检索/原文和 GIF 存 fire-study 的 motion_research.py、topic-15248.json、topic-1133-209.json、handdrawn-reference-sources.json、handdrawn-fire-*；只作研究。最新正常速度对照为 burn-curl-unified-compare.gif、flamethrower-curl-unified-compare.gif，左为用户已否定的 motion-fix 版，右为本次；已检查阶段渲染，审美尚待用户验收。


- 后续验收纠正：用户指出单火苗局部调整被擅自扩大为高火根/构图重做，形似触手；统一密度场形似胶状物。该实验已撤回，S27 仅用于原大小火苗的局部轻摆，S28 不再作为当前喷流实现说明。源码/录制恢复与验收入口见活动任务。

## P01-S29–S30：召唤阶段与剩余状态表现（2026-09-09）

- P01-S29：[Void/Star Summon FX](https://realtimevfx.com/t/void-star-summon-fx/19105)。查看作者 101 帧公开 GIF 的 12 阶段：地面能量和竖向形体相接，较亮峰值后主体先退，零碎光点另行结束。作者只说明练习作品，没有给技术拆解；不推断其 shader 参数或商用游戏归属。
- P01-S30：[Skeleton Mage Summon With Shaders and Textures](https://realtimevfx.com/t/skeleton-mage-summon-with-shaders-and-textures/10196)。已读作者说明顶点位移、世界空间渐隐/侵蚀、mesh ribbons、事件时序、独立纹理等分工，并查看首份 265 帧 GIF 的 12 阶段：能量/碎片出现后实体显现，余能随后退出。没有观看嵌入的 YouTube 教学，没有取用其角色、纹理或工程。
- 项目适配保留已有法阵，修正光柱悬空及阶段衔接；用局部 shader 光幕、独立短亮/碎光，不实现参考的实体材质变化或 3D 召唤。项目起升/短亮时长不冒充参考参数。检索、原文、两份公开 GIF 与阶段图在 remaining-study，未加入游戏资源。
- 眩晕本轮保留现有三颗星椭圆绕行，仅做清晰星形、近远明暗/大小与短尾光，复用已有护盾连续深度表达。检索到的 Bubble Stun 和 Ethereal AOE Stun 动作不适用，Google/DDG 未提供可用结果；没有声称看过商业游戏星环，未据不适用的参考重做动作。

## P01-S31：虚弱状态的符号与运动分工

- [Borniol: Sketch #25 Slow](https://realtimevfx.com/t/borniol-sketch-25-slow/9686)：已读作者说明，太阳/时钟意象代表热导致减速，列出 animated mesh；查看 Update 2 公开 GIF（79 帧）的 12 阶段，头上单个符号展开、自身转动、随目标移动并退出，未见粒子系统技术细节，作者清单也未将 particles 标为完成。可复用的是小型独立状态符号和主体角色分离，不是其玩法或热/时钟造型。
- 项目适配：weaken 原有运动即从头顶下沉，本次替换难辨能量滴为双下压符印，头肩两侧错峰下落、局部亮边经过、末段碎散，紫色细尘作为弱次层；不采用参考旋转太阳，不宣称原片有双箭头。符印是新生成的独立纹理，运动/材质/生命周期由共享轨道承担，移除召唤法阵复用。
- 已检索 weaken/curse/debuff/status effect，并查看 [Jukerlaw 第 10 楼](https://realtimevfx.com/t/jukerlaw-vfx-sketchbook/24801/10) 107 帧咒力 GIF，实际为旋转焰团，不适用下沉虚弱，未采用；时间牢笼、黏液减速、大型吸魂光束同样排除。不把搜索结果标题当制作依据。
- 原文、参考 GIF/阶段及前后录制在 remaining-study/weak-topic-*.json、slow-symbol-reference*、curse-reference*、weaken-*。未下载参考工程或复制第三方资源到运行时。

## P01-S35：高频战斗声音与听觉主次（2026-09-15）

用户担心增加受击音会造成密集噪声，询问成熟自走棋尤其云顶。已读取 Riot 官方 [The Frequencies of Folklore](https://www.leagueoflegends.com/en-us/news/dev/the-frequencies-of-folklore/)（2020-08-11）：瑞文设计师学习高频连招后强调声音起音部分，避免连招变成杂音；蛇女因低冷却高频施法，降低刺耳玻璃质感并重新平衡；提莫普攻与 Q 用不同响度及音高区分可读性。以上是《英雄联盟》灵魂莲华皮肤音效作者说明，不是 TFT 专项，未试听文章嵌入音频，不据此声称云顶采用某个并发数／压低比例／受击筛选算法。

TFT 专项检索包括 Teamfight Tactics sound design audio mixing combat Riot 及精确短语。Google 返回需要 JS、DDG 验证、Bing 返回无关内容、Audiokinetic 403，未获得可核实的 TFT 战斗混音实现资料。不得将常见游戏音频策略包装成云顶内部事实。

本项目静态证据：FeedbackAudio.tscn 的 melee_swing／melee_hit／arrow_release／arrow_hit 均为 -7 dB、同组最小间隔 0.08 秒／最多 2 声部；FeedbackAudio 为固定 12 声部及优先级抢占。按组限流不等于整场节奏／听觉层级；相同标称 dB 也不保证感知等响。现有命中反馈已经存在，不需要再叠一层全员通用受伤声。以上仅核查，没有修改运行时或重新进行听感验收。
# P01-S40：ES01 / ES04 实际动作观察与本项目拆层（2026-09-16）

同日P01-D13修正：用户要求完整贯穿飞行和简单擦伤，否定通用命中水波。复用本条已经观察的沿轴前进／目标独立短闪，以及P01-S12方向性、主次分层和快速收尾原则；项目动作设为受击身体中心的两道错位斜痕，约0.04秒划出，0.18–0.22秒消退，两枚碎光沿短路径分离，最迟0.28秒收净。采用独立scratch shader与authored SpriteTrack，不使用扩散环／地面范围，不生成整套效果图片。末段飞行和命中特效按可见弹道对齐，具体当前录制见敌人任务；数字／划痕形体均为用户要求下的项目适配，未将它们冒充原作内部实现。

同日P01-D12修正：用户否定下文首版“金属箭头”的素材适配，要求独立发光箭。沿用已观察的Varus Q聚光→尖锐前端飞行→依次命中动作依据，ES01改为独立shader的金白亮芯／发光尖头与渐淡尾迹；中心、平面、轨迹及阶段不变。正式场景已移除普通箭图集，正常速度前后录制见敌人任务最新段。下文金属箭身是被覆盖的首版选择，不是现行要求，也不是原作制作方式。

已通过本地参考播放器直接嵌入官方远端视频，正常速度循环观察并按 0.18–0.36 秒采样；未下载/复制原作素材。原先直接视频页的控件读取超时已绕开，P01-S37–S39 的“尚未观察”状态由本条补齐。

- [Lux R 官方视频](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0099/ability_0099_R1.mp4)：实际观察到施法者前方先出现细长方向线及平行边缘，源端聚光明显增强；随后同一轴线全长同时亮起粗白芯和外晕，快速收窄消失，目标端短闪保留更久。没有飞行箭头，不能用缓慢生长长度的光条代替释放。原作贴图、shader 和粒子内部实现未知。
- [Varus Q 官方视频](https://lol.dyn.riotcdn.net/x/videos/champion-abilities/0110/ability_0110_Q1.mp4)：实际观察到弓前聚光、箭头形体形成、角色持弓蓄力；释放后有独立尖锐前端沿一条轴线前进，先后穿过前后目标，各目标出现短闪，深色/紫色条带尾迹随后消散。该片段没有可确认的整条敌方地面危险条；本项目添加此条来自用户明确要求，不称其为原作机制。

项目适配（非原作参数）：源端取身体挂点，方向条取地面同一条逻辑射线；长度受战场边界/地形截断，宽度读取技能半径。1.2 秒先低亮方向预警，源端光核渐亮、少量光点向内汇聚；物理箭保持金属箭头+窄金色尾迹，魔法光束用全长白芯/青蓝外晕并在 0.35 秒内收尾。箭按实际碰撞播放命中，光束按单次释放事实播放命中。

分层素材与实现：既有箭头图集只用于金属箭身；256 方形载体+独立条带/径向 shader 分别表达地面指示、光核、光束，既有单光点素材用于内聚粒子。几何形状、长度、宽度和轨迹由共享播放器与注入投影计算；不将整套动作画成一张图，不复用横扫刀痕。暂停、倍速与减少动态使用共享时钟；后者保留渐亮和方向，减少粒子运动。
