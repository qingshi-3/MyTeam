# 透光遗迹庭院样板

2026-09-30按用户“你做一个看看”及进入游戏满屏战斗场地的要求制作。后补实际动态光影，2026-10-01用户要求云影及光束联动，继而否定光束像撕裂痕迹，要求从画面外照入的平行光；当前版本如下。

## 当前动态版本

- `courtyard-albedo.png` 是原庭院的均匀受光版本，清除大面积固定树影和太阳光斑，保留石路、拱墙、树冠和像素材质。`canopy-cookie.png` 是历史灰度树冠投影遮罩，已按D34从正式场景、地面／空气／角色材质绑定中移除，仅保留来源记录。两张均为内置 image_gen 生成的1672×941原始PNG；完整提示词在[dynamic-prompts.md](dynamic-prompts.md)，尺寸／哈希／生成方式在[generation-dynamic.json](generation-dynamic.json)。未复制参考游戏素材，没有额外像素编辑。
- `courtyard_surface.gdshader` 在均匀底图上实时计算冷色环境光与暖色日照。D37将直射RGB乘数上限从(1.36,1.23,0.96)降至(1.10,1.025,0.84)，环境光端(0.64,0.73,0.75)保持，为空气光束留出亮度层次。不再叠加固定树叶投影；程序云场持续向右下方平移，没有正弦折返或时间循环跳变。当前云层最多削弱84%的直射日照，冷色环境光保留；960×540正式x1战斗主云层约横向28.8、纵向9.6px/s，较小云层约32.9、11.0px/s；云团四个尺度约300／148／73／37px，权重38%／30%／22%／10%，较小两层旋转采样以弱化轴对齐形状，同向差速使轮廓和云缝持续变化；最细层仍为柔软云边，不生成像素噪点。地板本身不移动，建筑接触暗部仍保留在底图；此为2D投影适配，没有建筑深度遮挡或可破坏环境。
- `courtyard_lighting.gdshaderinc` 由地面和角色共用。角色按实际脚点映射到同一程序云场与时钟，减弱角色明暗反差，保留图集边界、描边和命中提亮优先级；不会根据地面石缝颜色猜测光照，不修改姿态或战斗属性。
- 空气层是独立 `courtyard_air.gdshader`：4条同方向、各自沿程等宽的斜向光带从画面外照向右下方，主要透光区域位于中央战斗场地。主体由宽柔中心／中层肩部／淡外晕连续叠加，三层横截面尺度0.58／1.02／1.55，权重52%／32%／16%；共用该轮廓的地面足迹与空气仍一起受云遮挡。弱纵向纤维／雾丝独立流动；没有场内尖头起止、锥形扩散或窄亮芯。四束的顶部开口／宽度／落地深度由共享courtyard_beam定义，三束分别在y=0.72／0.58／0.46以宽柔接触面落地，一束y=1.16延伸到画面外。共享courtyard_ground_pool定义由当前位置云透光控制的二维受光区，横向软边与平行光同轴，纵向深度0.05；空气沿太阳方向投回该区域、积分中心及±0.035／0.070五个纵深样本，归一化0.46，落地结束同样由这些样本决定。透光阈值0.55–0.90，阴影足迹不生成残余光路；不再使用额外cloud_gap或单行定深度取样，让地面亮区与空气一起随云移动／受遮挡；几何方向／沿程宽度固定，雾丝以78%–100%幅度沿束流动；没有独立亮度周期或逐像素地表遮光对空气的再切割。暖色alpha混合表现散射并降低束内局部对比，默认intensity=0.86，透明度采用1-exp(-光学厚度)平滑趋近，替代旧版0.34硬截顶，避免中心／重叠处被截成同亮度平面。有限数量细尘在各自局部区域上浮及小幅单向侧移，7–12秒渐入／渐出，只有进入光带才显亮；按屏幕像素控制大小。落地受光区横宽纵浅、柔边，直接修正地面／角色的共享照明（补光权重0.65），保留地砖纹理，与空气共用同一个实际足迹函数。落点遮光联动是二维美术近似，不含云体高度或真实体积积分。
- Ground／SunShafts各有私有ShaderMaterial，入口与战斗场景不共享可变时钟。BattleScreen注入同一表现时间驱动地表、角色与空气；真实暂停、单步、倍速同步。减少动态冻结当前光影、移除浮尘，不跳回初始光照。空气位于单位上方、危险提示／投射物／浮字与HUD下方，鼠标输入穿透。D37实机补齐常驻军团资源条／遗物栏／羁绊面板与页眉压暗Z=10，高于空气Z=2，弹窗原Z=19／20保持。
- 云影原理参考：[GDQuest clouds2D.gdshader](https://github.com/gdquest-demos/godot-shaders/blob/main/godot/Shaders/clouds2D.gdshader)的固定方向乘时间、噪声覆盖与柔和场景遮光，MIT仓库代码；本项目自写程序场，未导入其纹理或整段shader。光束软边／分层沿用[pend00 God rays](https://godotshaders.com/shader/god-rays/)的作者说明与CC0代码；当前平行形状依据[Godot方向光文档](https://docs.godotengine.org/en/stable/tutorials/2d/2d_lights_and_shadows.html#directional-light)及用户D31要求。联动参考[NVIDIA GPU Gems 3第13章](https://developer.nvidia.com/gpugems/gpugems3/part-ii-light-and-shadows/chapter-13-volumetric-light-scattering-post-process)的光源遮挡影响散射原则，地面落点映射为本项目适配，不照搬三维方法。没有使用来源网站图片或视频，未记为观看过完整作者动画；项目动态通过自己的正式资源录制核查。
- 当前D37地表亮度与光束三层渐变见[亮度层次记录](../../../outputs/courtyard-light-layers-20261001/README.md)。此前空间联动修订、正式10秒及无遮挡约16秒原速录制见[光路配合](../../../outputs/courtyard-light-path-20261001/README.md)，效果待用户评价。此前去掉固定叶影及四尺度云团细化见[云影细化](../../../outputs/battle-cloud-detail-20261001/README.md)。此前同尺寸、10秒正常速度前后正式Run录制、带羁绊栏的真实输入与独立落点形状检查见[部分光束落地](../../../outputs/battle-grounded-sunlight-20261001/README.md)。[云影动态记录](../../../outputs/battle-weather-motion-20261001/README.md)保留，其流动方向继续沿用，双层云场已由四层替代。[平行光形状记录](../../../outputs/battle-parallel-sunlight-20261001/README.md)保留为D32否定动态不足的历史。[上一轮联动记录](../../../outputs/battle-light-coherence-20261001/README.md)保留为D31否定光束形状的历史；[首轮云影记录](../../../outputs/battle-environment-motion-20261001/README.md)保留为D30否定静态感的历史，其中before的整体树影摆动亦已被用户否定。D34云影细化版本已获用户D35“这个效果可以”的认可；本轮全局页面接入的具体观感仍由用户评价。

## 全局背景接入

`CourtyardBackdrop.tscn` 直接实例化正式 `SunlitCourtyard.tscn`，叠加与战斗同参数的顶部／底部柔暗边缘；背景与光影在所有UI之后，不拦截输入。GrowthGameRoot、GameRoot、ValidationMatrix、ExperienceSlice和VfxPreview五个入口统一引用。路线／奖励／招募／商店不再加局部整页重暗罩。非战斗页面共用根背景和连续时钟；正式与玩法试验进入战斗后隐藏根背景，由战斗实例管理暂停和速度。工具内用于观察特效的专门舞台保留原用途。源码、截图与验证边界见[全局接入记录](../../../outputs/global-courtyard-20261001/README.md)。

## 首版静态画稿记录

- `courtyard.png`：内置 image_gen 工具生成的新环境素材，1672×941、不透明PNG；参考用户图片的像素层次、草石过渡及树冠透光，没有复制其角色／UI／商店。原稿未缩放、裁切或另作图像编辑，节点用Nearest采样及等比覆盖。它是首个可调整样板，不代表用户最终认可。
- 首版曾将建筑、草木和大面积地面阴影一起烘在环境画稿中，角色用脚点附近5点地表亮度近似明暗，空气层运动极弱。此方案被用户指出缺少呼吸感；原图和首版录制保留作前后依据，正式场景已使用上面的动态分层版本。
- 作者原理来源：[pend00 God rays](https://godotshaders.com/shader/god-rays/)（原作者文字／代码，CC0）：分层光束、方向、扩散、软边和下游衰减。未复用网站图片／视频。当前shader为针对固定开口和有限浮尘的新实现，尚未宣称复刻作者动画。Godot实现能力见[2D灯光文档](https://docs.godotengine.org/en/stable/tutorials/2d/2d_lights_and_shadows.html)。

## 生成提示词

```
+Use case: stylized-concept.
Asset type: production 2D pixel art battle environment background for a Godot auto-battler, wide 16:9 composition, 1920x1080 output.
Primary request: a beautiful sunlit ruined stone courtyard inside an overgrown ancient tower, visually integrated like a premium pixel-art fantasy strategy game. Use the user's attached pixel village picture only as reference for crisp pixel clusters, grassy paving transitions, rich foliage and broad soft tree shadows with warm sunlight patches. Generate a NEW courtyard scene, not a copy of the village.
Camera: orthographic elevated oblique side/top view, horizontal ground axes with no isometric diamond; upper walls show their front faces. No sky, horizon or perspective vanishing point. Ground fills the entire frame.
Composition: generous unobstructed central combat floor covering x=8%..92%, y=21%..80%. This floor is continuous pale weathered small stone paving, irregular natural joints and moss cracks fading into grass at edges, no isolated board or grid. Keep the center moderately subdued to make small game sprites readable. Broken stone archway and ivy-covered low ruined walls run along only the top 15% of frame. Sparse autumn gold/olive tree canopies, trunks, roots and low stone fragments only at the far side edges and bottom corners. Bottom middle stays continuous paving. Ground must extend naturally behind future game HUD.
Materials: individually readable chunky stone blocks, simple broad clusters of pixels for leaves, patches of grass, worn masonry; restrained texture, polished authored pixel art, deliberate 2-3 screen-pixel clusters when displayed at 960x540.
Lighting: warm afternoon sunlight entering from upper left, unified soft broad canopy shadows toward lower right; dappled warm pools of sunlight on paving. Baked ground illumination only. Strong enough light-dark separation for depth, no whiteout. Foreground foliage slightly darker. No rendered floating atmospheric rays or dust; those are separate runtime layers.
Constraints: empty stage, NO characters, enemies, silhouettes, units, shops, water/river, bridges, weapons, furniture at center, text, UI, frames, borders, grids, hexagons, tile-square highlights or checkerboard. No cinematic painterly blur, 3D plastic rendering, photorealism, lens flare or glowing circles. The result must be a usable complete full-screen game background with a clean playable center.
```
