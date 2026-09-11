# 2026-09-11 整批特效素材候选

用户已授权优化此前评估的整批素材。本目录收录 10 张内置 `image_gen` 的原始 RGBA 输出；没有使用 CLI/API 回退，没有程序重绘或重采样 PNG。项目内副本与生成原件 SHA-256 一致。所有贴图启用 mipmap，保留真实透明通道。

新素材用于现有共享场景与播放器。旧版 PNG 留在上级目录，未覆盖。以下记录为生产来源，不表示用户已认可本版观感。

法阵与护盾生图请求遇到连接/响应解码失败；额外法阵候选带烘焙棋盘格且无合格 alpha，未接入。法阵、盾壳保留旧图，用现有 shader 分别收紧符文亮线与减轻壳体遮挡。共享 energy-mote 保留旧图，未将失败请求计为新素材。

| 素材 | 原图尺寸 | 用途 |
| --- | --- | --- |
| [stun-star.png](stun-star.png) | 1254×1254 | 头顶主星，三枚有深度明暗的独立轨道实例。 |
| [stun-scroll.png](stun-scroll.png) | 1254×1254 | 三枚细卷纹，与主星同速错相；防止异速追赶造成周期重叠。 |
| [flame-tongue.png](flame-tongue.png) | 1024×1536 | 直立灼烧火舌；顶部局部形变，alpha 幂压低背景光晕。 |
| [jet-flame.png](jet-flame.png) | 1536×1024 | 专用于吐息的横向残焰，热端朝 +X、尾部向左；局部尾部形变。 |
| [rising-plume.png](rising-plume.png) | 1024×1536 | 强化上冲光羽；宽纹理主体与较少的尾端溶解分区。 |
| [weaken-rune.png](weaken-rune.png) | 1254×1254 | 轻薄的双下压符印；沿用错峰下沉、末段碎散。 |
| [fireball-body.png](fireball-body.png) | 1254×1254 | 火球与历史轨迹余焰，宽明暗面替代密集小卷火。 |
| [smoke-puff.png](smoke-puff.png) | 1254×1254 | 中性独立烟团；冷雾由发射层调色，爆炸烟灰额外用 self_modulate 压暗暖化。 |
| [ice-prism.png](ice-prism.png) | 1024×1536 | 独立晶柱及碎晶，生长层 alpha 幂收紧外部光晕。 |
| [rend-strip.png](rend-strip.png) | 2172×724 | 刀光网格的横向纹理，共用于撕裂/横扫/光束/护盾带；撕裂端部另有网格 UV 收口。 |

## 完整提示词与原件

方式：内置生图工具，独立单组件生成。下列源文件名位于当前任务的 Codex generated_images 目录，仅作溯源；运行时只引用本项目 `res://` 路径。

### stun-star.png

原件：`exec-ae2a5925-96ea-49d3-8d91-403fb1d906a8.png`；1254×1254；SHA-256：`23afc9eba32a4c791c475ac7d64b9e92c7e4dccabbf1d6728fc0863ccd7b88be`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE compact five-point magical STAR for a cartoon-fantasy dizziness effect. A slightly playful hand-painted silhouette with softly curved shoulders and crisp tapered five tips. Warm amber at the lower edge, honey gold body, small pale lemon highlight on the upper left, luminous creamy center. Only 3-4 broad tonal planes, almost flat with a little soft thickness, no metallic bevel. Front-facing, one point straight UP. The actual star occupies 82 percent of a square canvas. Recognizable at 18 pixels. No swirl, no secondary sparkles, no orbit, no badge frame.

### stun-scroll.png

原件：`exec-d7496cfb-64a4-421d-badf-bd8b19f707e3.png`；1254×1254；SHA-256：`cf868efe4c751c34f360b91856de97a4ae02a272bdaf52674bee12ec75f0493c`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE delicate magical curling flourish for a cartoon-fantasy dizziness effect, matched to a honey-gold five-point star. A SINGLE clean open spiral brushstroke, curling inward about one turn, ending in a small tapered inner tip, with a long slender taper sweeping to the LEFT. Open negative space, flowing asymmetrical stroke taper, not a solid coil. Warm amber lower edge, honey gold body and a narrow pale lemon highlight. Only 3 broad tonal planes, nearly flat illuminated ink, minimal depth. Occupies 82 percent of a square canvas. Reads at 15-20 pixels. No stars, no extra particles, no ring, no metallic jewelry, no cinnamon roll.

### flame-tongue.png

原件：`exec-201288da-a7ad-4ccc-b74b-6980786a6ecf.png`；1024×1536；SHA-256：`b991e2602f11ad79e4d57549e5ccd99daa12d98ce827b32513b697f261363ec8`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE upright small fire tongue particle. Portrait canvas 2:3. Broad softly tapering root at BOTTOM, one flowing middle and TWO unequal short tapered tips at TOP. The root occupies the bottom fifth; flame rises mostly straight, slight leaning curve, no long snaking S, no ball-shaped bulb. 4 broad flowing shape layers: dark crimson outer folds, vermilion body, orange inner flame and a small yellow hot patch low inside. Two clean deep notches in the silhouette separate the tips. Thin translucent flame edges and a clear solid midtone body. Body occupies 78 percent width and 90 percent height. Strong design readable at 30-60 pixels. NO whole bonfire, NO sparks, NO smoke, NO complete spell, NO black outline.

### jet-flame.png

原件：`exec-69957f36-93c5-4d2e-b535-a74809763363.png`；1536×1024；SHA-256：`bee76e8ccf398d11b09f921ce8eb7da690ad2eddb4ee9fec6de90669b877fb6e`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE elongated horizontal rushing fire lobe particle for a forward flamethrower. Landscape canvas 3:2. The direction of travel is RIGHT: broad bright rounded leading lobe on the RIGHT, narrowing into two unequal curved wispy tails flowing LEFT. Dominant flow horizontal, no upright flame tongues. Coherent orange core, dark crimson hollows, a restrained yellow strip within the leading third. 4 broad smooth stylized planes, transparent notches between curling tail strips. Occupies 88 percent width and 65 percent height. Readable at 30-50 pixels wide. Not a fireball, no whole flamethrower plume, no spray, no sparks, no smoke, no background.

### rising-plume.png

原件：`exec-eb250265-4738-4518-9005-c55db893d7d1.png`；1024×1536；SHA-256：`babfd9a2f59d11c6a33b9526cb9b403d199602f7a50341284ac864e2c17c981f`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE small upright golden POWER-UP ENERGY PLUME. Portrait canvas 2:3. The TOP is the leading direction: one compact sharp pale-gold point, a broader luminous gold shoulder immediately beneath, and a smooth tapered amber translucent tail stretching DOWN. Silhouette like one swift upward brush flick, with two slim slits giving air within the tail. Strong mostly straight vertical upward shape; no curling lightning lines. Three broad color bands with a very small ivory tip, moderate gold midtone. Content is slim, 40 percent canvas width and 90 percent height. Reads as rising power at 18x70 pixels. NO sparks or stars, NO electric bolts, NO smoke, NO full aura.

### weaken-rune.png

原件：`exec-0da843e9-d125-4413-a6ca-d87d6ac493e0.png`；1254×1254；SHA-256：`4e956a5ac4205be3797bee82d9b5552a40259eeb46b9130d145252f816c2aaae`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE pair of downward chevrons forming a WEAKENING CURSE SIGIL. Two separated, broad angular V-shaped strokes, both points DOWN, upper V slightly larger. Each stroke tapers at its outer tips, and its edge has one or two controlled worn breaks. Bright muted orchid-violet upper edge, readable amethyst body, thin dark plum lower edge. A translucent magical ink mark, only 3 broad tonal planes. NOT solid rock, NOT metal, NOT three-dimensional carved object, no cracks. Large open transparent gap between the two Vs. Content occupies 85 percent of square canvas. Reads instantly at 25 pixels on both dark navy and pale grey. No smoke, particles, circle, full effect or background.

### fireball-body.png

原件：`exec-eb7d4de3-65f2-4813-9db7-efbbda0158d6.png`；1254×1254；SHA-256：`acf623b9d3a5c6eadb45e39ca34a7fa4617e36fdf9fcd17564e7e8ce6d742110`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE compact spherical rolling FIREBALL BODY. Square canvas, fireball occupies 88 percent with clean transparent margins. Three to five large interlocking flame lobes form a round but irregular silhouette. Clear deep crimson folds, rich vermilion-orange masses, three narrow yellow hot seams, only two tiny ivory highlights. Broad smooth painterly volumes, sparse large curves, no dense microtexture. A few curled torn edges around the silhouette, no spikes everywhere. It must read as hot rolling FIRE and retain orange/dark red structure at 55 pixels. NO rock or lava surface, NO tail, NO smoke, NO separate sparks, NO explosion, NO broad halo.

### smoke-puff.png

原件：`exec-151a45ce-4e30-42f2-b7c0-34167ccc0699.png`；1254×1254；SHA-256：`fdb0af5040afb67e5258b79beaa7ee7ab09d8b5293d32a34705e0a51c7783e75`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE isolated small stylized SMOKE PUFF for fantasy game particles. Square canvas. An asymmetric cluster of FOUR to SIX softly billowing lobes, larger upper-left lobe and smaller trailing lower-right wisps. Neutral muted blue-grey to charcoal shadow, broad soft planes with restrained pale-grey edge lights. Partly transparent edges, three generous soft transparent notches to avoid a solid cotton ball. Content occupies 84 percent canvas. Must read at 30-70 pixels with simple mass and airy edges. Hand-painted stylized smoke, not photographic granular smoke. NO fire, NO embers, NO sparks, NO ground, NO complete explosion.

### ice-prism.png

原件：`exec-3f090ad7-0944-4588-bf5a-96108b9c101c.png`；1024×1536；SHA-256：`0c328d51ac6e0d0c87b506774979093a60e808eee86f25825c6ecfd661d377d7`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE upright ICE CRYSTAL PRISM, sharp pointed TOP and a flat narrow base at BOTTOM. Portrait canvas 2:3. A single asymmetrical six-sided solid crystal, three to five large clear planar faces: deep cobalt-blue shadow face, saturated glacier cyan main face, pale icy-blue narrow light face. A few elegant broad internal refraction shapes, one sparse fracture near the base, crisp silhouette. Top shoulder bends asymmetrically, no symmetric diamond gemstone. Occupy about 62 percent width and 90 percent height. Strong readable volumes at 25x70 pixels. Preserve the cool blue weight rather than making a white blob. NO cluster, NO extra floating shards, NO glow halo, NO ground or complete spell.

### rend-strip.png

原件：`exec-99931bd0-1e2b-49d7-ab30-09f408998d1a.png`；2172×724；SHA-256：`fd124c084e47a647f3ce41672859ef9e70b1126b13e4be60dafc993cb5ce0414`。

> Use case: stylized-concept. Production texture component for our hand-painted 2D fantasy game's real-time VFX. GENUINE TRANSPARENT RGBA background. Render just the isolated component, without checkerboard or backdrop. Clean silhouette, low-frequency shapes, deliberate tonal hierarchy, restrained glow, no text or watermark. ONE straight HORIZONTAL SLASH MATERIAL STRIP for UV mapping onto a curved ribbon mesh. Landscape canvas 3:1. Long narrow sword-energy stroke extends LEFT to RIGHT, fully straight central axis at y=50 percent. A clean ivory-white cutting core with ONE dominant tapered bright strand, restrained silver midtone and crimson-red translucent trailing fringe underneath. Long sparse directional fibers, several deliberate thin transparent slits, very little noisy grain. Pointed ends and a broad gentle thickness maximum in the middle. Content occupies 92 percent width and only 35 percent canvas height. Must work when bent by a mesh and shown as a 12-pixel-thick stroke. NO diagonal slash, NO cross, NO arc, NO isolated sparks, NO explosion, NO full spell.
