# 特效贴图来源

2026-09-09 `flame-tongue.png` 来自内置 image_gen 原始输出 `exec-22915e18-e076-4c52-bfd9-eff7e677c87f.png`，RGBA 原件与 mipmap 保持。用户否定开放高火根如触手、统一焰团如胶状物后，恢复上一版完整单火苗与吐息配置。灼烧仅通过 BurnFlameParticle 的材质覆盖启用上半部小幅侧摆；吐息使用原 FlameTongueParticle，不继承该调整。素材未裁切、未重画，无新增外部依赖。

完整生成提示词（内置工具，未使用 CLI）：
> Use case: stylized-concept. Asset type: ONE isolated upright flame tongue sprite, a reusable texture component for a premium painterly 2D fantasy game's realtime fire effects. Transparent RGBA background. ONE coherent flame lobe with broad rounded root at BOTTOM, flowing tapered body, and two unequal curling split tips pointing UP. Flame occupies central 65 percent width and 78 percent height with generous transparent padding. Tall 1:1.6 silhouette. Strong readable coarse folds with deep crimson outer shadow cavities, rich vermilion and orange flame planes, small warm yellow inner tongue low in the body; no white blob. Several elongated negative spaces and thin ragged edges, painterly smooth gradients and crisp curl silhouettes, readable at 40 by 70 pixels. This is a SINGLE flame tongue, not a bonfire, ball, complete spell, particle system, explosion, plume, camp scene, ring or texture sheet. No ground, smoke, sparks outside, logs, rocks, character, lettering or broad baked glow. Engine separately anchors or emits multiple copies, advects and erodes them with shader, and adds independent light/smoke/embers. Do not include those additional layers in this asset.

雷击/冰爆点缀修订未新增图片：ElementGlint 与 ElementDust 使用单颗粒光学 shader，分别提供尖亮折射点与柔细微粒轮廓；各效果层赋予蓝紫或冰蓝/青白色，轨道控制独立出生、运动和淡出。冰晶密度调整复用 ice-prism.png；晶簇根部冷光是独立低亮柔光层。

本次冰爆重做新增 `ice-prism.png`：内置 image_gen 原始输出 `exec-221359f3-b247-4bc7-8ffa-9d54011c015c.png`，原始 RGBA 复制进项目并启用 mipmap。图片仅为一根直立棱柱；7 个独立晶体负责根部生长/局部裂解，较小实例负责飞散/翻转，冷雾使用另一个粒子层。旧 ice-shard.png 保留作历史资产，当前 frost 不再引用。雷击使用分支网格和材质，无新外部美术依赖。

本次单根棱柱完整生成提示词（内置工具，未使用 CLI）：
> Use case: stylized-concept. Asset type: ONE individual upright ice crystal prism sprite for a premium 2D fantasy game's VFX particles. Truly transparent RGBA background. A SINGLE elongated asymmetrical six-sided solid crystal, upright with sharp tip at TOP and flat narrow base at BOTTOM. It occupies middle 45 percent of width and 80 percent of height with transparent padding. No attached spikes or cluster: exactly one coherent prism. Strong large readable faceted planes: deep saturated glacier-blue side, translucent cyan main face, one thin icy white rim and a few subtle interior fractures. Crisp straight-edged silhouette, slightly unequal facet sizes, semi-transparent refraction but dark blue mass remains readable on a pale background. Painterly high-end game asset, low-frequency surface details, must read at 25 by 70 pixels. No snow cloud, no shards flying away, no ground, no halo, no ring, no spell composition, no character, no text, no cast shadow. The engine will instance the sprite separately, grow it from its base, then dissolve it and launch smaller crystal fragments.

本次强化重做新增 `rising-plume.png`：内置 image_gen 输出 `exec-2b643ad7-c70e-4e69-ab21-c1290d5a4ffa.png`，原始 RGBA 复制到项目并启用 mipmap。只包含单束朝上的金色能量纹理；VfxRiseTrack 按独立出生时间、加速位移、拉伸和局部溶解组合光簇。empower 不再引用 rend-strip 或斩击 shader。当前 poison 已改为 authored FastNoiseLite/NoiseTexture2D 驱动的分层液面材质和独立鼓泡/破裂/涟漪，旧 poison-liquid.png、PoisonBubble 与 poison_pool.gdshader 保留作历史资产，不再由 poison 场景消费。

本次强化完整生成提示词（内置工具，未使用 CLI）：
> Use case: stylized-concept. Asset type: ONE small reusable upright energy plume sprite, truly transparent RGBA background. For a premium 2D fantasy game's power-up particles, not a finished effect. One slender vertical luminous plume pointing UP, roughly 1:3 width-to-height content, centered with generous transparent margins. Short pointed bright ivory leading tip at top, golden core below it, several delicate parallel upward filaments, amber translucent feathered tail trailing downward. Organic irregular tapered silhouette, slight asymmetry but predominantly straight, subtle internal negative spaces. Coarse readable bright/dark structure at 25x90 pixels. Rich amber edges and tiny ivory core, moderate bloom contained inside transparent margins. No fire ball, no sword slash, no curved blade, no ring, no character, no background, no text, no other particles, no full aura, no explosion. This is a single streak of uplifting magical energy intended to be instanced independently with acceleration, stretching and local shader erosion.

前序毒沼修订新增 `poison-liquid.png`，内置 image_gen 原始输出 `exec-877add8c-3ddc-46b9-aacf-088496b94b16.png`。项目副本保留 RGBA 原始透明通道，启用 mipmap；当时承载薄液面纹理，poison_pool shader 控制透明度/局部流动，PoisonBubble 独立粒子控制稀疏气泡。旧烟灰材质不再用于毒沼。

完整生成提示词：
> Create ONE production-ready VFX texture component for a stylized high quality fantasy game, square 1024x1024 PNG with true transparent background. Subject: a very shallow pool of magical poisonous liquid seen DIRECTLY FROM ABOVE, circular but organically uneven perimeter, occupies 75% canvas with generous transparent margin. This is a flat thin LIQUID SURFACE decal, NOT a whole spell illustration. Elegant clear emerald/teal liquid with sparse lime-green luminous meniscus highlights, delicate branching fluid marbling, dark transparent-looking gaps, subtle small ripple fragments. Uneven narrow shoreline, only a few brighter concentrated patches. Low opacity airy interior, legible at 200 pixels. No solid opaque center, no brown, no yellow sludge, no mud, no rocks, no smoke, no clouds, no raised mound, no foam piles, no splashes, no airborne particles, no runes, no symbols, no text. No tilted perspective, no ground background, no cast shadow. Painterly hand-authored fluid detail with restrained emissive edge highlights. Components such as bubbles and animation will be separate engine layers. Save the generated texture.

常用特效扩充新增两个透明材质组件，均由内置 image_gen 生成并复制进本目录，无外部资源运行时依赖：`ice-shard.png`（原始 `exec-1765a21d-63ab-4877-bb5c-bc0699b5c5fe.png`）用于独立晶体粒子；`arcane-seal.png`（原始 `exec-a0bbbc4f-1093-4a92-9b44-bad48b00da8c.png`）用于符文分区显现/运动。两图启用 mipmap 和线性过滤。常用层继续复用 rend-strip、energy-mote、fireball-body、fire-ash-puff；眩晕星是独立小型填色符号材质，不以线框代替整套技能美术。

本次冰晶完整提示词：
> Use case stylized-concept. ONE isolated ice crystal shard sprite for a high-quality 2D fantasy game VFX particle, transparent RGBA background. Square canvas, single elongated shard points RIGHT, about 75% canvas width and 35% height, transparent padding all sides. Asymmetric sharp crystalline silhouette, icy cyan-blue translucent facets, dark teal interior planes, a few strong silver-white refracted edges, coarse readable shading and hairline cracks. No glow halo, no particles around it, no snowflakes, no explosion, no ground, no text. Individual material component to be emitted in many sizes and orientations by an engine. Rich painterly crystal structure that reads at 25-60 pixels. Not an entire skill effect.

本次法阵完整提示词：
> Use case stylized-concept. One isolated ARCANE SUMMONING SEAL material sprite for a premium 2D fantasy game, true transparent RGBA background, square image, top-down perfectly circular not perspective. An elegant silver-white engraved runic annulus: two concentric bands of broad angular glyphs and small diamond inlays around an empty transparent center. Three larger interlocking curved ornamental motifs at thirds of circle. Rich hand-painted luminous ivory-metal brushwork with subtle cool blue edge and dark silver inset relief; high contrast readable thick symbols at 200 pixel display, not hairline technical drawing. Transparent center and exterior margins, diameter about 80% of image. No floor, no environment, no light pillar, no sparks, no glow cloud, no Latin text. This is only the flat sigil texture: engine will separately animate radial revelation, inner/outer rotation, volume particles and light. Do not paint whole completed skill or explosion.

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
