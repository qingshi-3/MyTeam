# 特效库扩充素材（2026-09-12）

内置 image_gen 生成，原始 RGBA 文件直接复制。只生成独立部件；动作、网格、粒子与时钟由共享场景负责。启用 mipmap，无外部运行时文件依赖。

项目副本的 SHA-256 见 [hashes.json](hashes.json)。

用户否定首轮岩石/藤蔓写实纹理，要求多参考日漫。`rock-spike.png`、`thorn-strip.png` 保留历史，未被本轮最终场景引用。第一版 anime 岩柱实录仍像晶柱，故另生成 `rock-wedge-anime.png` 的厚重不规则断块；前版 `rock-spike-anime.png` 也不再使用。风卷仅生成水平流动纹理，不能将整张图当成完整龙卷。

## rock-wedge-anime.png

原件：`C:/Users/qs/.codex/generated_images/01a075a0-5a5d-76c1-96a0-b5ca390172ed/exec-0050f1f6-0203-4e1a-98ad-5dd044681720.png`。参考三部日漫官方静帧的体块、断面和独立光层分析后，修正过长针状轮廓；运行层另拉开石柱宽窄、高低与倾角，保持刚体顶出、独立碎岩和地尘。

完整生成提示词：

> Use case: stylized-concept. Asset: one individual fractured EARTH ROCK WEDGE sprite for a Japanese anime-inspired 2D action game's erupting stone attack. Truly transparent RGBA background. This is ONE rock component, no complete effect, no group.
> Shape correction is essential: a THICK, HEAVY, ASYMMETRIC mass of broken sedimentary rock with ONE broad chisel-shaped sloping peak, about 1:1.6 width to height. Side silhouette has 3 or 4 coarse broken stepped offsets, a missing chunk at one shoulder, tapering unevenly. NOT a slender needle, crystal, icicle, tower, cathedral, mountain, pyramid, trophy or round pedestal. Bottom is a ragged fractured cross-section with short uneven broken teeth disappearing into the ground, NOT a visible oval base. Keep body mostly upright; peak slightly left of center.
> Art direction informed by anime hard-surface cel animation: warm taupe/gray main face, muted umber-gray shadow, charcoal deep fracture, small warm stone highlight, 3 to 5 organized large angular value planes with crisp irregular ink contours. A few low-frequency chipped scars and thin fault seams concentrated at plane transitions. Flat-to-subtle painted shading, matte opaque EARTH material. No blue, icy translucency, crystal symmetry, metallic shine, polished bevels, noisy realistic mineral texture or all-over cracks. Strong weight and geological fracture, readable at 70x105 pixels.
> Centered single isolated sprite, occupy about 65% canvas width and 80% height, generous transparent padding. No ground, dust, sparks, glow, shadow on background, character, frame, text, symbols, scene. The engine will separately animate eruption, launch debris, and layer dust. Output transparent PNG.

## rock-spike-anime.png

原件：`C:/Users/qs/.codex/generated_images/01a075a0-5a5d-76c1-96a0-b5ca390172ed/exec-255dd9b8-264c-4d93-aa04-7c2891b84059.png`。

完整生成提示词：

> ONE production sprite component for a Japanese 2D TV anime battle effect. A SINGLE UPRIGHT EARTH SPIKE, large broad angular base and sharp asymmetric point at the top. Clean hand-drawn anime cel animation look: ONLY THREE LARGE FLAT COLOR PLANES, warm light gray face, medium blue-gray side, deep slate shadow. Crisp confident angular silhouette, 2 or 3 major rock splits, slightly stylized heroic proportions. NO photographic detail, NO stone grain, NO tiny cracks, NO brown muddy texture, NO realistic rendering, NO painterly noise, NO bevel highlight, NO glow or halo. Think a freshly erupted earth spear in a shonen anime action shot, but isolate just this one stone on GENUINELY TRANSPARENT RGBA background. Tall portrait canvas, sprite occupies central 60 percent width and 78 percent height, root at bottom tip at top, ample transparent margins. About 2.1 times tall as wide. No ground, no dust, no explosion, no particles, no complete scene, no lettering. Flat anime colors with deliberately large readable faceted shapes, designed to remain beautiful at 70 pixels high. The engine will animate rise and fracture; image contains no motion trail.

## thorn-strip-anime.png

原件：`C:/Users/qs/.codex/generated_images/01a075a0-5a5d-76c1-96a0-b5ca390172ed/exec-786a016c-d4a1-45bf-a79d-b899d09a2224.png`。

完整生成提示词：

> ONE VFX TEXTURE COMPONENT for Japanese shonen anime plant magic. A SINGLE straight-ish slender vertical GREEN VINE, root BOTTOM, tapered pointed growing shoot TOP, designed to be bent by the game engine along a curve. Beautiful clean 2D ANIME CEL ART: bold curving silhouette, rich jade green body, a broad pale mint green highlight on one side and deep forest-green shadow on the other. ONLY 3-4 large flat color areas, NO bark grain, NO realistic wood, NO photographic texture, NO painterly noise. Six small elegant pointed leaves attached alternately and five short ivory-green thorns, sparse enough that the stem stays clearly readable. Slight flowing S curve along the length, not a coil. Youthful living green plant magic, energetic and graceful. Portrait canvas, vine occupies the central 22% of width and 80% height, generous genuine transparent RGBA margin, no colored glow haze. No circle, no full binding cage, no character, no scenery, no ground, no text. This is one stem material for an animated rooted binding effect; at small size the vivid green smooth stem and leaf silhouette must remain clear.

## silence-mark.png

原件：`C:/Users/qs/.codex/generated_images/01a075a0-5a5d-76c1-96a0-b5ca390172ed/exec-ac34ab07-8beb-4aee-aaaf-dd58d9126ba6.png`。

完整生成提示词：

> Use case: stylized-concept. ONE isolated SILENCE status mark texture, genuine transparent RGBA background, square canvas. Compact hollow stylized speech-bubble rune with a strong diagonal sealing stroke across it, no letters inside. Rounded angular pale silver outline with a tiny speech notch at lower left, interior fully transparent, diagonal muted violet-magenta magical band clearly blocks the speech opening. Three tiny square notches integrated in the lower outline. Simple thick high-end hand-painted fantasy glyph, soft lavender edge highlights, deep plum underside for contrast, broad shapes recognizable at 30 pixels. No heavy shiny metal bevel, no big glow cloud. Mark occupies central 70 percent canvas with generous true transparent margins. Front facing. No circle behind, no ring, no full spell, no particles, no scenery, no character, no text, no background.

## wind-flow.png

原件：`C:/Users/qs/.codex/generated_images/01a075a0-5a5d-76c1-96a0-b5ca390172ed/exec-0f24cd17-25fe-4ea6-ad8f-af77526c4ee8.png`。

完整生成提示词：

> Production VFX material strip for Japanese 2D anime WIND animation. GENUINE transparent RGBA background. Horizontal wide canvas. This is only a flat flowing TEXTURE BAND to be wrapped around a curved tornado mesh by a game engine. Several broad sweeping wisps of pearl white and very pale blue-gray wind run from LEFT to RIGHT in slightly wavy parallel layers. Elegant tapered flowing brush shapes with clean anime negative-space cutouts, 3 broad tonal levels, softer transparent trailing fringes. Smooth airy streaming, no sharp sword blade tips, no ice shards, no crystalline facets, no noise grain. Upper and lower borders fade fully to transparency; left and right ends fade too. Light and graceful, not opaque fog. No whole tornado or funnel silhouette, no circles/rings, no glyphs, no ground, no particles, no text, no backdrop. Wider than tall, primary band at middle 50% height, generous transparent margins. Engine supplies actual rotation, perspective, convergence and changing shape.

## rock-spike.png

原件：`C:/Users/qs/.codex/generated_images/01a075a0-5a5d-76c1-96a0-b5ca390172ed/exec-38e23721-bfb8-4c4b-aacf-08a603a6e14a.png`。

完整生成提示词：

> Use case: stylized-concept. Production texture component for a hand-painted 2D fantasy game's ground spike spell. GENUINE transparent RGBA background, no backdrop or checkerboard. ONE isolated upright jagged ROCK SPIKE, squat broad rooted base at bottom, asymmetric sharp tip at top, about 2.0 times as tall as wide. Brown-gray basalt and warm slate rock, 4-5 broad angular stone planes, a few dark cracks and sparse dusty ochre edges. Strong heavy opaque earthy material, NOT ice or crystal, no translucent facets, no glowing lava. Painterly simple large shapes legible at 70 pixels high. Thin broken chipped silhouette near base. The single rock occupies central 70% of a portrait canvas, ample transparent margins, all tips fully inside canvas. No ground, no pile, no explosion, no dust clouds, no particles, no magic circle, no characters, no text. Engine will grow this individual stone from its base and spawn separate fragments.

## thorn-strip.png

原件：`C:/Users/qs/.codex/generated_images/01a075a0-5a5d-76c1-96a0-b5ca390172ed/exec-a965fca3-27c7-469b-88a1-ecc3f4ed0c2c.png`。

完整生成提示词：

> Use case: stylized-concept. ONE game VFX material component, a straight vertical thorny vine strip on a TRUE transparent RGBA background. Portrait canvas. Single slender woody vine extends from a slightly thicker ROOT at BOTTOM to a sharp tapered TIP at TOP. Mostly straight centreline, small natural bends only; an engine will bend the entire strip around a 3D-looking curve later. Light brown bark with large readable grooves, muted jade-green sap seams, sparse tiny leaf buds and short thorns attached to the sides. Width about 18 percent of canvas, length 78 percent, transparent padding everywhere. Rich but simple hand-painted fantasy game style, coarse readable structure at 15 pixels wide. No full coiled vine, no full spell, no circle, no branches extending far sideways, no flowers, no ground, no pot, no scene, no glow cloud, no sparkles, no text. Maintain real alpha all around the single material strip.
