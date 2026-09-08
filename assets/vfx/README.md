# 特效贴图来源

最新火球轰击：新增 `fireball-body.png`（内置 image_gen，原始输出 `exec-72446ea1-7215-4162-aea9-46fbafbf460b.png`）。它是无尾迹的单球焰体，由局部差速 UV 和微扰动驱动；本体保留原始颜色，避免重映射成整团黄。FireballWake 独立粒子复用该图，energy-mote 仅作短尾焰/触地火星。烟灰原图仍供 FireAsh 使用，但改为较大正立颗粒。焰体与烟灰采用 mipmap 过滤以减少缩小时的闪烁。

本次内置生图完整提示词：
> Use case: stylized-concept. Asset type: ONE isolated round fireball BODY material sprite for a 2D fantasy game, truly transparent RGBA background, square image. This is a component texture, NOT a complete spell illustration: no trail, no explosion, no ground ring, no sparks outside the body, no environment. Center a compact roughly spherical tumbling ball of flame occupying 70% of canvas with transparent margins. Rich volumetric layering of deep dark crimson folds/shadow pockets, saturated burnt orange turbulent flame lobes, thin bright orange/yellow internal seams and ONLY a few tiny pale-hot highlights (less than 5 percent of body area). Strong coarse readable sculpted turbulence, flowing curled flame tongues around silhouette, asymmetrical internal swirls. Painterly high-end realtime game VFX texture; must retain dark-red and orange structure when displayed at 55 pixels wide. No large white/yellow blob, no smooth uniform glowing circle, no rock, no lettering. Most of the sphere is mid-value orange and dark red, NOT yellow. No baked broad glow halo, renderer adds halo separately.

2026-09-08 火球坠击复用 `energy-mote.png`：本体用局部尾焰扰动和火焰色阶，拖尾与触地火星是独立粒子。地面火焰波复用 `burst.png` 的纹理细节，波前由径向材质控制。新增 `fire-ash-puff.png` 是单团带少量余烬的炭灰烟雾，内置 image_gen 原始输出 `exec-eff88a19-eaae-46d5-87ce-878ac5b5aab9.png`，保留 alpha；提示词要求独立烟团、透明留边、无完整爆炸或场景。烟团通过粒子分别移动、膨开和淡出。未增加第三方资产或运行时依赖。

2026-09-08 交叉撕裂改为 `rend-strip.png` 单条水平银白/绯红材质带，原始来源 `exec-31379b79-3244-4db8-9ee0-96464f1faaa8.png`（内置 image_gen，2172×724，保留 alpha）。提示词限定直条带、透明留边、无交叉/爆发/独立火星；它不包含完整动作。两条 authored Curve2D 生成带 UV 的 MeshInstance2D；shader 沿路径显锋、逐段衰退与顺势漂移，独立碎屑 Sprite2D 复用条带。局部接触亮光用短寿命材质生成。旧 rend.png 保留且仍供 beam 使用。本轮没有下载第三方资产或增加插件依赖。

当前范围冲击使用快速主波、三个更慢回波、短亮中心和独立径向粒子。burst.png 仅供主波后方的压力纹理层采样，角向保持固定，随波前外扩，不旋转或变成刀片；它也继续用于原有能量弹/命中。

`energy-mote.png` 由内置 image_gen 生成，原始输出 `exec-6e72c953-595b-4ad8-9be2-e1fc5a3ece56.png`，保留透明通道。提示词明确只制作一颗朝右的浅色能量滴与短羽状尾迹，留透明边、无完整技能/环/爆炸。`ShockMote.tscn` 将它作为独立径向飞散物；`OrbitBand.tscn` 将它作为轨道亮点。护盾轨道条带复用 rend-strip.png 的明度/透明细节，由材质重新着色为金/青/蓝，不修改原图。原 shield.png 作为稳定盾体，运动归三个独立轨道实例。

当前治疗使用本地 authored 矢量小图标 heal-plus.svg，由独立 Sprite2D 粒子实例播放。用户明确选择绿色加号表现；heal.png 是已弃用的光带试稿，仅保留来源，不再由治疗场景消费。

rend.png、burst.png、shield.png、heal.png 由内置 image_gen 工具生成，保留原始透明通道；无外部运行时依赖。原始输出留在 Codex generated_images，项目使用本目录副本。

提示词核心要求：
- rend：premium hand-painted fantasy game VFX，单条银白/绯红斜向挥砍，尖端收细、碎屑、透明背景，无角色/场景/文字。
- burst：premium hand-painted blue-white plasma radial shockwave，空心、湍流边缘与火花，透明背景。
- shield：translucent teal-gold magical shield shell，发光边缘、细碎能量面与清透中心，透明背景。
- heal：healing updraft，三条薄荷绿/象牙白上升弯曲光带、金色微粒，透明背景，无环/盾/人物。独立素材，不再复用护盾图。

运动由独立层组合并共用播放器时钟：当前斩击沿曲线路径逐段挥出；冲击光圈持续外扩；护盾外围环绕、内层稳定；治疗加号各自上浮。能量弹仍使用 energy_flow。每个场景可独立调资源参数；更换贴图时需要保持对应的 UV 方向与透明留边。
