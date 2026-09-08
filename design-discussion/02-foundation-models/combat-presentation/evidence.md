# P01 本地架构核对

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

