# P01 本地架构核对

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
