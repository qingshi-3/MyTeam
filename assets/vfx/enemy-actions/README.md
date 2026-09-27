# 六项动作的部件素材

2026-09-25：按用户授权优化正式特效。本次两张 PNG 由内置 imagegen 生成，原 RGBA 字节复制入项目，无在线或生成目录运行依赖。它们仅是物理主体部件；运动、方向、残影、尘雾、命中和清理由共享 Godot 场景编排。

|文件|规格与用途|SHA-256|
|---|---|---|
|hook-claw-painted.png|1254×1254 RGBA；右向三爪钢铁钩头、铜铰链、少量青色嵌件，独立透明底，无链条/光效|`b502004e954e738e1506355b1a1118345045c24cc4e5b20a1231a37cb5b3e081`|
|return-blade-painted.png|1254×1254 RGBA；正视四向弯刃、钢铁刃面与铜色中心，旋转轴居中，透明底，无运动轨迹|`53d34e759024930d1a44fe7e09abc10d1e2cd79f881dbda331c401561b7cd594`|

生成要求摘要：手绘游戏部件，清楚的金属明暗与轮廓、正交视角、单独对象、足够透明留边，不附背景、文字、角色或完整特效。飞钩朝右且尾端留链条连接位；返刃四向平衡，适合自旋。原图已直接检查透明度和轮廓。

`chain-link.svg` 是本项目编写的可平铺金属链节矢量图，供 Line2D 沿真实两端点铺设；不来自外部包。已有 swap_rune 等资源保留原归属。石块复用 assets/vfx/library/rock-wedge-anime.png；烟尘、扰动复用 assets/vfx/dreams-circle/ 下已授权纹理，详见其 README。

动作依据与项目适配：design-discussion/02-foundation-models/combat-presentation/evidence.md P01-S53。

## 弧形岩盾单柱（2026-09-25）

`barrier-pillar-painted.png`：1254×1254 RGBA，内置imagegen，原字节复制，SHA-256 `9ed51084a4935c7de199a063b4b5389ffd2c442d1d522048ab20f8bc56b90a3a`。源文件 `$CODEX_HOME/generated_images/01a0d4b5-8fde-7702-9242-908444237e81/exec-f0bc7c05-8bc0-4161-9994-75f2b3ca164b.png`。alpha>128主体边界(374,25)–(907,1237)，着色器裁取源UV(0.29,0.012,0.44,0.977)，保留原alpha且弱化边缘杂像素。运行依赖为仓库相对路径。

最终提示词（内置工具，无CLI）：

> Create one production game sprite, not a scene: a SINGLE upright solid stone shield-pillar / barrier slab, on a truly transparent RGBA background. 1024x1024 square canvas, isolated centered object occupying roughly x=30%-70%, y=6%-94%. Tall slender slab, width-to-height about 0.43, bottom flat for embedding in ground, slightly irregular tapered sides, angular asymmetric chisel top, strong thickness visible on right side, broad clean polygonal stone faces with a few fracture planes. Hand-painted polished 2.5D anime fantasy strategy game style, three-quarter frontal view with modest elevated camera. Cool slate blue-gray stone, pale blue-gray lit front planes, charcoal blue shadow facets, subtle narrow pale cyan rim highlights at two prominent edges. OPAQUE heavy ROCK, matte mineral, never transparent ice or glass. Design intended to be repeated with separate transforms into a closely joined curved defensive wall: no detached shards, no ground, no smoke, no glow cloud, no magic circle, no other objects, no text, no border, no baked shadow outside the rock. Every pixel outside the single rock must be transparent. Preserve readable big faceted shapes at 80 pixels display height.

工具实际输出1254×1254；已使用原始输出。参考为用户提供布克棱岩盾墙图，来源/观察边界见P01-S54。单柱是新生成美术，墙形、起升、尘土与持墙均由共享场景编排。

## 哑光岩壁面材质（2026-09-25）

`barrier-stone-matte.png`，1254×1254，原字节复制，内置imagegen。SHA-256 `5612bf610c43a4fc8596ff3129d9199b998013b9d2b01dc6f2762379699fd4f3`。源文件 `$CODEX_HOME/generated_images/01a0d4b5-8fde-7702-9242-908444237e81/exec-38994443-96b2-4195-bfd5-08e65c991822.png`。这是一张不透明材质底色，映射到RockBarrierPillar独立立面/顶面；材质不含岩柱轮廓/光效/运动。导入启用mipmap，线性mipmap过滤用于缩小显示。旧barrier-pillar-painted.png保留，当前岩壁不再使用其亮边贴片。

最终提示词（内置工具，无CLI）：

> Production diffuse texture map for matte stone in a stylized fantasy strategy game. A flat SQUARE seamless slab-rock material swatch, fills entire image edge to edge, straight orthographic view, no object silhouette, no background, no transparency. Dark muted gray slate with a subtle warm earthy undertone. Very broad restrained painted tonal regions, a small number of broad angular geological planes, a few understated sparse dark hairline fractures, almost no fine grain, no busy mottled noise. Dry dense weighty basalt, low contrast, soft broad diffuse values; approximate palette #53575b to #737779, brightest pixels around #858988, shadows around #3f4347. No white, no silver, no specular highlights, no glossy veins, no glowing edges, no cyan glow, no crystal or ice appearance, no ambient occlusion baked as a border, no cast shadows. Clean painterly game material that reads as solid heavy rock at tiny display size. Engine will supply all geometry, directional face shading, exposed top faces and animation. Do not depict a wall, pillar, building, separate stone blocks or a scene. This is only an even rough stone surface material for mapping onto separate rock faces.
