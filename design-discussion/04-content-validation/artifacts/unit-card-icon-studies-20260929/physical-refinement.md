# 旧图标实物精修

## 已获接入授权：保留原框的单位卡材质接入

用户对下节“原剑＋宽条拖尾”回复“可以了”，要求优化实际 UI，并明确原 UI 框不改。生产攻速直接复制该图；其他图标以 `physical-icon-critical-clean.png` 为输入提取透明图集，再由预制 AtlasTexture 切片，不重新设计语义。资源、完整提取提示与归属见 [生产目录](../../../../assets/ui/icons/unit-card/README.md)。外框、名称栏与立绘保持原资源，隐藏四项回蓝，法力合并为初始／上限。

已在真实招募、开局候选与英雄装备场景中查看透明合成和小尺寸图标：未见预览器中的大块白色空隙污染；这不是源图 alpha 全零声明。基础构建、隔离真实输入和录帧证据见 [活动任务](../../../../work-items/active/ui-material-overhaul.md)。下节“仅静态稿、未接入”是历史状态，最终游戏观感仍待用户确认。

## 当前要求：指定原剑＋宽条拖尾，禁止换剑

用户再次贴出[必须保留的原剑](feedback-attack-speed-original.png)，明确是“这个图＋指定的拖尾”。下节把黑白参考整枚着色为 `attack-speed-banded-trails.png` 的做法改错了主体，已被否定，不作为当前候选。黑白图只提供拖尾的宽条／平行间隔／与主体相连的表达，不能替代原剑、柄部或护手。

本轮内置 imagegen 用两张输入分别限定“原剑编辑目标”和“只借拖尾的参考”，生成[原剑加宽条拖尾稿](attack-speed-original-with-bands.png)。可见主体保留原剑方向、弯曲刃形、皮革握柄和黄铜护手，左侧空白处增加四道可见宽弧带；提示要求五道，实际输出为四道，不宣称数量精确满足提示，也不把用户参考推断为必须五道。随后仅做透明空隙与杂边清理，不再更换主体。

验证：已查看两次输出与 384 像素系统缩略渲染；最终 PNG 为 RGBA 1254×1254，项目副本与生成原件 SHA-256 一致。工具预览仍显示部分空隙白色斑点；抽样像素为 `(255,255,255,1)`（8 位 alpha 接近透明），不能据此报告 alpha 全部为零或彻底清理完成。还没有实际背景合成／24 或 32 像素游戏检查，当前仅为形状审稿，不标生产资产验收。未修改正式资源、其余图标或名称栏。

拖尾参考来源为 Lorc 的 [Blade drag](https://game-icons.net/1x1/lorc/blade-drag.html)，[CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)，本稿借用其宽弧带表达并适配于用户指定原剑，非原作者成品。

### 添加拖尾的完整提示（内置 imagegen）

```text
Use case: precise-object-edit / compositing.
ADD TRAILS TO IMAGE 1. DO NOT CHANGE ITS SWORD.
Image 1 is the edit target and the required finished weapon: retain this EXACT curved silver sword, its long curving blade including inner sliver, soft upper tip, the thick brown leather handle, brass guard and pommel, their position, scale, angles, shape, shading and texture.
Image 2 is ONLY a reference for the SHAPE OF THE TRAILS. DO NOT copy, recolor or substitute its sword, handle, guard, separated parts, blade shape or overall silhouette. The previous error was replacing image 1 with a colored version of image 2. That is explicitly forbidden.

Make a single additive edit in the EMPTY SPACE to the LEFT of image 1's curved blade: add FIVE clearly spaced, parallel, BROAD CURVED SWING BANDS like the upper-left bands in image 2. Follow image 2's substantial ribbon width, clean edges, blunt open trailing ends and evenly readable dark/transparent gaps. Each band curves from the left across the empty space and JOINs the existing blade at a different point along its inner/left side. The five arcs share one swing direction; arc sweep is adapted to the EXISTING sword, not vice versa. Keep the existing sword visible and in front of the bands; no trail crosses the hilt. Bands fill only some of the empty upper-left and center-left space, with clear margins, not a background vortex.
Use smooth muted steel-gray bands, moderately translucent and subordinate to the solid main blade, without tiny fibers. Their substantial widths and sharp clean spacing must read at small size. The bands are motion afterimages, NOT solid extra metal blades: no bevel frames, chrome rims, grooves or scratched surface on them. Do not turn the bands into wispy smoke, tapered hair, feather shapes or two thin swooshes. Keep the reference's BROAD GRAPHIC BAND construction.

Preserve transparent background and original square framing. Output only the edited image 1: the SAME sword PLUS the five specified bands. No text, no frame, no numbers, no other changes. Absolutely do not replace, reshape, disassemble, flatten, rescale, restyle or regenerate the sword body.
```

### 后续透明空隙清理提示（内置 imagegen）

```text
Use case: background-extraction. Alpha cleanup ONLY. Keep this exact sword and the FOUR broad curved silver-gray trail bands currently visible in the image, with absolutely no changes to their shape, width, arrangement, colors, material details or scale. Keep the original leather handle and gold guard unchanged. Do NOT add or remove any trail bands.

REMOVE ALL stray WHITE PATCHES, bright noise, colored blue/red/yellow fringes, debris and speckles floating in the EMPTY GAPS BETWEEN THE FOUR TRAIL BANDS and around the icon. These large irregular white areas are background contamination, NOT part of the icon. The entire background and ALL open gaps must be truly transparent with clean alpha=0. Especially erase the patchy white zone underneath the uppermost band and between the next three bands. Remove white edge halos without eroding the broad silver-gray bands. Preserve natural antialiasing at the actual object's boundary and existing smooth partial alpha within the motion bands.
Output a clean transparent PNG cutout of precisely the same sword-plus-four-bands composition. No added background, no white/black/checkerboard canvas baked in, no new effects, no redesign. Only background and edge decontamination.
```


## 当前攻速稿：用户指定的平行宽条拖影

用户明确否定下节 `attack-speed-swing-trails.png` 的烟雾／细丝状拖尾，重新贴出 [Blade drag 裁图](feedback-attack-speed-bands.png)说明要的是平行成束、间隔清楚、与主体直接相连的宽条运动图形。上一张仅作为失败记录，不再列为当前候选。该要求限定拖尾表达，不等于认可所有材质或正式替换整套符号。

本次使用内置 imagegen，以用户黑白图作为直接编辑输入，优先保留五道上部宽条、两道下部弧带及负空间间隔；对轮廓施加钢灰、旧黄铜和皮革的局部材质。[当前独立预览](attack-speed-banded-trails.png)为深底静态 PNG，已实际查看并核对项目副本与生成原件哈希一致。生成图不承诺逐像素几何一致；当前未做小尺寸／实机检查，未改其余图标、名称栏或生产资源，等待用户判断结构与材质。

来源归属仍为 Lorc 的 [Blade drag](https://game-icons.net/1x1/lorc/blade-drag.html)，[CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)；用户上传图与该研究参考一致。本图为着色／材质改编，不代表原作者认可。

### 当前宽条稿完整提示（内置 imagegen）

```text
Use case: style-transfer / precise-object-edit.
The supplied black-and-white image is the USER'S EXACT APPROVED GEOMETRY. Treat it as a fixed mask/template, not loose inspiration. Re-render this exact symbol as a tactile hand-painted fantasy game attack-speed icon. Preserve the image's arrangement, proportions, white silhouette, all black cutouts and the widths/spacing of all curved strips. Keep the canvas square and the icon fully visible. Do not invent a different sword.

CRITICAL GEOMETRY: the reference has a broad diagonally oriented blade mass on the upper right, with FIVE clearly separated, broad parallel curved motion bands extending LEFT from the blade, each retaining a substantial width and ending in the same BLUNT CUT ends seen at the left edge. These are STYLIZED GRAPHIC SWING AFTERIMAGES, not smoke or threads. Preserve these five bands individually and exactly their spacing, common arc and attachment to the blade; the dark channels between them taper into the blade mass as shown. Keep the TWO broad sweeping bands below the main diagonal blade/guard relationship exactly as in the reference as well. Keep the small lower-left pommel, narrow grip and rectangular crossguard. No new metal crescent shaped blade, no feather cutout, no long wispy tendrils. NO changes to the silhouette to make the weapon more anatomically realistic. It is an expressive in-motion symbol.

MATERIAL ONLY: map the white blade area to subdued silver-gray steel with a few broad restrained light/dark planes, the small guard and pommel to aged brass, and the thin grip to dark brown leather. Map the curved bands to SILVER-GRAY GRAPHIC AFTERIMAGES: smooth broad opaque strokes close to the blade, subtly flatter and slightly lower contrast toward their blunt trailing ends. The bands keep clean crisp boundaries and uniform substance; no motion-blur filter, no strand texture, no gradients to invisible points, no glow, no fuzzy or feathery edges. The user's strong broad-band graphic construction is more important than physical realism. Metal depth is subtle so the geometry is not crowded by bevels. Soft matte lighting, no oily shine, no sparkling rim light, no scratches/noise. Match a restrained tactile fantasy UI icon palette.

Replace black background with uniform very dark warm charcoal (#211f1a), preserving every negative-space channel. No text, labels, numbers, extra images, frames or props. One icon only. This is a colored/material rendering of THIS EXACT reference, not a redesign. In particular DO NOT draw the previous curved saber with two thin smoke arcs; follow only this attached mask.
```


## 最新攻速修订：挥动形变与拖尾同时使用

用户确认上轮调研方向，随后明确“也同样可以加上拖尾的效果”。这撤销 Agent 对所有独立拖尾的一刀切限制；保留剑身弯曲、向尖端渐虚、清晰柄部和旧图标材料体系。确认的是制作方向，不是对本轮新图片的验收。

本次按 imagegen 技能使用内置图片编辑，先单独提取攻速做大幅弧形轮廓，再局部修订上段虚实；用户补充后，为同一挥砍弧线增加两道长短不同、端部变细变淡的银灰拖尾。最终输出为[独立攻速局部稿](attack-speed-swing-trails.png)，深炭色背景便于查看淡出，不是透明生产贴图。其他十五项图标和名称栏原稿未改，未替换正式 SVG、目录资源或运行组件。

Agent 已实际查看最终生成图：柄／刃根保持清晰，上段弯曲并向尖端淡入背景，内侧可见两道弧形轨迹。原件与项目副本 SHA-256 相同，尺寸 1254×1254。本轮只有静态大图检查，尚未完成 24／32 像素可读性或实机验证，也未据此报告“最终质感已通过”；待用户评价这张局部稿。

造型研究参考 [Blade drag — Lorc](https://game-icons.net/1x1/lorc/blade-drag.html)，[CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)；本稿为另行生成的材质、形变和拖尾改编，不是原作者成品，也不代表原作者认可。调研边界见 Q25。

### 当前交付稿完整编辑提示（内置 imagegen）

输入为本轮独立攻速的中间修订图（此前以项目词条稿为材质参考、Blade drag 为挥动形状参考）；最终保留不透明研究背景，不当透明资产使用。

```text
Edit the supplied attack-speed icon. User correction: KEEP the curving blade AND ADD VISIBLE SWING TRAILS. These work together. Output a single revised icon on a plain dark warm charcoal background (#211f1a) so the fading motion is easy to judge. This is a local review image, not a complete UI or sprite sheet. One icon, no text, no label, no frame. Square canvas, icon occupies about 75 percent with safe margins.

Keep the existing brown leather grip, brass crossguard and short solid steel blade root. Keep the sweeping curved main blade, but its UPPER THIRD must become noticeably softer and fade toward the vanishing upper-left tip, rather than staying solid to a sharp tip.

Add TWO CLEAR, BROAD, CURVED SWING TRAILS to the INNER/LEFT side of the main blade, following the same sweeping arc and fanning back across the currently empty upper-left space. The motion is a broad sword swing centered near the hilt. The two trails sweep behind the blade in concentric arcs: strongest next to the moving blade, progressively thinner, softer and fainter toward their trailing ends. Make one longer and one shorter, visibly spaced enough to survive small-icon display. Curved tapered painted strokes of neutral silver-gray, not rigid rectangular gold bars. The trails should almost meet the blade mass at their thick end and dissipate freely at the other end. They must clearly read as motion trails, not extra metal blades. No full rings around the sword and no duplicated handles.
Simplify the pre-existing pointed inner metal sliver into the nearest soft sweep so the icon does not resemble a double-bladed scimitar. One recognizable weapon plus two kinetic trails.

Hand-painted fantasy game stat icon in the same material system. Make the dynamic shape clear and compact. Solid leather/metal root, softer stretching blade, airy swept trails. Restrained highlights, broad material planes, no hot glow, particles, lightning, feathers, white halos, oily gloss, or noisy brushed-metal texture. Preserve the weapon identity and existing grip design. Do not add any other UI assets or alternate versions.
```


## 最新暴击率修订：移除中央闪电

用户指出暴击率中央闪电缺少语义依据。此次局部去掉闪电形孔／暗纹，以原有金属表面补齐中心，保留爆发外轮廓，不用新符号补位；其他图标（包括主动技能原有闪电）、数值与排版不变。使用内置 imagegen 编辑静态稿，未替换运行资源。

当前输出：[暴击率去闪电修订稿](physical-icon-critical-clean.png)。Agent 已查看，暴击率中心已补齐、无替代符号，其他已定向修改保留；待用户评价，未接入正式游戏或进行小尺寸实机验收。

### 暴击率局部编辑提示词

```text
Precise single local edit of this existing fantasy UI review sheet. Modify ONLY the critical-chance icon labeled 暴击率, row 2 column 3, above its value 0%.
REMOVE the dark LIGHTNING-BOLT SHAPE from the CENTER of the gold impact burst. Fill that lightning-shaped cutout completely with the SAME continuous muted gold/brass surface as the surrounding burst, using quiet broad painted planes and the existing soft upper-left lighting. It should now be one solid, clean angular impact-burst silhouette with a plain center. Preserve its current outer points, shape, size, position, depth, bevels and palette. Do not leave a bolt-shaped shadow, groove, crack or highlight. Do not replace it with any other symbol: NO central star, gem, sword, diamond, circle, rune, plus, hole, or inset badge. The impact-burst shape itself is sufficient.
Keep label 暴击率 and value 0% identical. DO NOT modify 暴击伤害 next to it; its red burst and sword stay.
Everything else remains unchanged: the curved attack-speed sword with its tip gradually blurring/fading, axe, ribbon, name 连弩手, attack 22, heart 155, purple smooth spell energy, red fangs, green healing cross, solid blue mana drop with 20/60, all other icons and text, 4x4 arrangement and charcoal background. Do not reintroduce removed mana-regeneration entries. Do not change the separate active-skill lightning icon in the bottom row: only remove the bolt INSIDE 暴击率. No redesign, no new details, no layout change.
```

## 最新攻速修订：剑身弯曲，剑尖渐虚

用户明确要求剑身本身弯曲，并且越靠近顶端越虚，否定上一版直剑旁叠加独立拖尾的理解。此次只改攻速：柄／根部清晰，剑刃沿弧线弯曲，向剑尖渐进模糊并淡出；移除独立金色拖尾。其余图标、名称栏、法力 `20/60` 及回蓝展示收敛不变。使用内置 imagegen 局部编辑静态稿，未据此修改正式资源。

当前输出：[弯曲渐虚攻速修订稿](physical-icon-attack-speed-curved.png)。Agent 已查看：剑刃呈弧线、柄部清晰、上段与尖端逐渐虚化，旧独立金色拖尾已移除；其他条目与数值保持。此稿待用户评价，不视作运行时动画或正式资源接入验收。

### 攻速局部编辑提示词

```text
Use case: precise-object-edit. Edit ONLY the attack-speed icon labeled 攻击速度, row 2 column 1, above ×1 in this supplied existing sheet. Preserve the whole remainder of the sheet unchanged, including all 15 other icons, numbers, text, arrangement, name ribbon, purple non-crystal spell energy, red fangs, green healing cross, and solid blue mana drop with 20/60.

The USER'S EXACT DESIGN: THE SWORD ITSELF IS CURVED, AND IT BECOMES PROGRESSIVELY MORE BLURRED / FAINT TOWARD ITS TOP TIP. This is NOT a straight solid sword with decorative speed lines beside it.
Remove ALL THREE separate golden swoosh trails currently to the left of the sword. They are the rejected approach. Do not add any replacement detached trails, duplicate swords, ghost swords, wings, rings, lightning, sparks, or arrows.

Draw ONE sword with the same brown leather grip and restrained brass guard at lower-left. Its blade must visibly sweep along a smooth, pronounced curved arc toward upper-right, like a sword caught during a rapid swing and optically bending with motion. BOTH sides of the blade AND its center ridge follow this curve. The handle and lower blade/root are solid, focused, readable and materially painted. As the blade rises along its curve: lower 30% crisp steel; middle 35% increasingly softened, lower contrast and mildly translucent; upper 35% visibly motion-smeared ALONG the same arc and progressively fades into the charcoal background. The very tip must be the SOFTEST and FAINTEST part, a soft disappearing curved stroke, NOT a crisp bright pointed metal tip. Blade volume transitions continuously into its own motion blur, not a hard sword plus an attached glowing tail. Retain enough dark/light curved steel planes near the root to identify a sword at small size. Do NOT leave any straight rigid spine through the bend. Do NOT render a crisply focused curved scimitar all the way to its point; the progressive loss of focus and opacity toward the point is essential. A single continuous curved sword-swing silhouette, compact and legible.

Keep local icon size comparable to the current attack-speed icon and keep ×1 exactly below it. Hand-painted stylized fantasy UI, muted steel and existing warm guard/handle, controlled highlights. No oil shine or glow bloom. The background must show through the fading upper end. Do not modify attack AXE or critical damage's sword; only 攻击速度. No other edits, no restyling, no layout changes, no added before/after panels or explanatory text.
```

## 最新定向修订：配色、动势与法力展示

用户要求攻速加强动态感；吸血改红色；法强去掉水晶感；治疗强度改绿色；法力上限使用非镂空水滴。单位卡不展示每秒／攻击／受击回蓝及单次回蓝上限；初始法力不再单列，与上限合为 `n/m`，明确是“初始／上限”，本稿为 `20/60`，不是战中“当前／上限”。这不删除回蓝机制或属性数据。

修订稿将原 21 项审稿板收敛为 16 项：14 个属性条目与 2 个技能类别；4×4 仅用于审稿排版，不重开游戏卡内布局。未点到的图标与名称栏保留。

当前输出：[定向修订稿](physical-icon-mana-merged-final.png)。已查看：攻速带定向挥砍拖尾，红色牙齿、绿色十字保留原符号；法强二次局部修订为连续流动能量，法力为实心水滴且显示 `20/60`，五个独立法力／回蓝条目已移除。[首次定向稿](physical-icon-mana-merged.png)保留为法强切面未完全去除的中间记录。两次均使用内置 imagegen 编辑；仅静态美术制品，未接入生产资源或改动回蓝逻辑，实机与用户体验未验收。

### 定向编辑提示词

```text
Use case: precise-object-edit / fantasy game UI icon refinement.
Edit the supplied existing painted icon sheet. This is a focused revision, NOT a new icon system. Keep the crafted axe, name ribbon, existing identities and overall matte hand-painted physical aesthetic. Preserve the full top name ribbon exactly in structure and art: single bearded axe + 22 at left, 连弩手 centered, red heart + 155 at right. Leave all icons not listed below unchanged in design, paintwork, colors and detail.

USER CHANGES:
1. ATTACK SPEED 攻击速度: the static sword and two rectangular bars look motionless. Keep ONE diagonal sword, but make it clearly cutting fast toward upper-right. Use a swept gesture and 2 or 3 broad TAPERED CURVED motion streaks following one coherent swing from lower-left/left toward the blade. These should read as painted rapid-motion traces, not separate rigid gold bars. Preserve a legible blade, guard, and grip; strong directional silhouette and purposeful spacing. No second sword, no lightning substituted for the sword, no giant glowing aura or distracting particles. Value stays ×1.
2. LIFESTEAL 吸血: keep the same paired long downward FANGS with small inner notches. Change the ivory to clear deep crimson/red with lighter red upper planes and dark burgundy inner shadows. Keep their tactile rounded fang construction. Red overall, not just tiny red tips. No gore, dripping blood, bottles, hearts, or added symbols. Value 0%.
3. SPELL POWER 法强: existing purple comet/flame looks like a solid faceted crystal. Retain the same round-core and sweeping tapering-tail silhouette, but paint it as flowing purple magical energy. Smooth coherent curving flame ribbons and soft interior transitions, an opaque lavender core with darker violet flowing folds and restrained translucent tips. ZERO polygon facets, crystal shard planes, gemstone glints or mineral texture. It must feel fluid, not a purple rock. Keep a crisp recognizable outer contour, moderate brightness, NO big neon halo, glitter or cloud. Value 0.
4. HEALING POWER 治疗强度: retain the exact equal-armed solid cross, recolor the entire cross to fresh leafy GREEN with a softer pale-green upper plane, darker green side thickness and controlled matte highlights. Not a gem, not ivory or gold, no extra plate. Value 0.
5. MAX MANA 法力上限: keep the same BLUE WATERDROP outer silhouette, but make it COMPLETELY FILLED/SOLID. There must be NO central hole, no ring, no hollow teardrop. Paint a rounded, full blue drop with a quiet broad light plane and darker underside, not a flask, crystal, or hoop. Its text MUST be exactly 20/60. This means INITIAL MANA / MAXIMUM MANA, NOT current combat mana. Retain label 法力上限.
6. REMOVE the following FIVE entire entries from the sheet: 初始法力, 每秒回蓝, 攻击回蓝, 受击回蓝, 单次回蓝上限. Remove their icons, values, headings and grid slots. Initial mana is now represented ONLY by the numerator 20 in 20/60. Do not leave the obsolete numbers 5,10,50,20 floating where removed entries were. Do not add other regeneration icons or hide them in a legend.

LAYOUT after removal: exactly SIXTEEN entries in an evenly spaced FOUR COLUMN by FOUR ROW review grid below the unchanged top name ribbon. Each has one Chinese field heading, ONE large refined icon and the exact value beneath. No panels or badges behind each entry, no comparison arrows, no duplicate miniatures, no empty slots. This 4x4 layout is the design sheet layout ONLY, not a redesign of the in-game card.
Row1 left-to-right: 攻击 22 | 法强 0 | 生命 155 | 防御 1.
Row2: 攻击速度 ×1 | 攻击距离 5.2 | 暴击率 0% | 暴击伤害 150%.
Row3: 吸血 0% | 闪避 0% | 控制抗性 0% | 移动速度 ×1.
Row4: 法力上限 20/60 | 治疗强度 0 | 主动技能 主动 | 被动技能 被动.

Preserve unchanged: attack axe has steel blade, real socket and leather grip; health red heart; armor steel shield; range brass crosshair; critical chance gold burst with bolt; crit damage red impact and blade; dodge running relief; resistance broken chain; movement leather boot; active lightning and passive ring/star. No new ornament, plates, gems, oily gloss, noise texture or global glow. Do not accidentally turn the red fangs into a heart or change the cross shape. Spell power must NOT retain even faint crystalline facets.

Keep charcoal backdrop, clean Chinese typography, ample spacing and high illustration fidelity. Main title 旧图标 · 局部修订. Subtitle 原有造型延续 · 精简法力展示. A small review-only note may read 法力：初始 / 上限. Footer 静态美术预览 · 尚未替换游戏资源. Name ribbon has no changes. The only existing artwork changes are the five targeted icon refinements listed above; the only display changes are removing five entries and formatting mana as 20/60.
```

### 法强单项去切面修订提示

首次定向稿已完成其余变化，但法强仍残留闭合分块线，继续仅编辑这一项，不以改色冒充去水晶质感。

```text
SINGLE LOCAL EDIT ONLY. Edit the supplied finished game icon sheet; modify ONLY the purple spell-power icon labeled 法强 in row 1 column 2 (above its value 0). All other pixels/designs, labels, 4x4 grid, name ribbon, red lifesteal fangs, green healing cross, moving sword, full blue mana drop with 20/60 and all other values MUST stay as in input.
Problem: the purple icon STILL has a tessellated gemstone/crystal surface. Remove its ENTIRE polygon/cell-outline network and every faceted partition, sharp mineral plane, shard point and jewel glint. Do not merely soften or recolor the same facets.
Paint that one icon as a PURPLE MAGIC COMET / SMALL FLOWING ARCANE FLAME with the SAME overall diagonal direction and recognizable round upper-right core, swept trailing lower-left tails. Use a smooth unsegmented dark-violet outer flame, a smaller continuous lavender flame tongue curling inside it, and one soft pale-lavender luminous core blended into the body, like flowing hand-painted flame with broadly curved boundaries. Only 2-3 coherent curved flame ribbons, no tiny wisps, no internal straight lines or closed polygon shapes. Tail tips are flowing and slightly translucent. Keep it compact, legible, hand-painted, NOT a glass object, not a solid crystal, not a jellyfish, not a gem, not a mineral, not a leaf. No gemstone outline around the core. No hexagons. No faceted shell. Moderate luminance, no bright halo, no scattered sparks. This is energy, not a material object.
Important: unchanged fields stay unchanged. In particular keep mana exactly 20/60 with the full solid blue droplet, keep attack speed swoosh, life-steal red and healing green. Do NOT restore removed initial-mana/regen entries. Keep sixteen items. Do not change any Chinese text or re-layout the image.
```

## 上轮精修记录

用户明确否定上轮统一渐变／浮雕的执行质量，要求攻击及其他图标都认真细化，不能只是换色。旧语义轮廓与已认可名称栏继续保持；这不是重新选符号。上轮 21 项原路径稿保留为失败对照，不视作已采纳。

本轮使用内置 imagegen 做位图材质精绘：以旧图标对照板作为编辑对象，以 D 稿仅作实物材质参考。目标为逐项构造体积、部件接合、倒角与材料分区；不是继续批量套 SVG 滤镜。属于静态美术修订，未据此更改游戏运行资源。

## 交付与检查范围

- [整理后的精修稿](physical-icon-refinement-clean.png)：攻击细化为单刃战斧、刃口／斧身、装柄与皮革握持部；其余 20 项按原符号身份细化盾面、剑身、骨牙、锁链、皮靴与中空法力水滴等。
- [首张精修稿](physical-icon-refinement.png)保留为过程记录，因紧凑示例仍混入旧平面图标而不作为当前交付；整理稿已删除全部旧小图，不以旧图冒充缩小后的精修资产。
- Agent 已查看两稿，整理稿保留 7×3 项目、名称与数值，没有把回蓝系列重新变成药瓶，没有增加逐项底板。保留名称栏三段构图；生成式精绘不宣称原像素或 SVG 路径完全一致。正式接入时仍应复用已认可名称栏素材，只替换对应图标层，不能整图连文字烘入游戏。
- 当前是待用户评价的位图美术稿，尚未拆分透明单项资源、替换语义目录或验证实际小尺寸／真实输入；没有启动引擎、构建、提交或推送。小尺寸验证必须在真实资产接入后做，不能由此稿推定通过。

## 完整编辑提示词

```text
Use case: precise-object-edit / painted game UI icon material refinement.
Input image 1 is the EDIT TARGET: a contact sheet showing the project's EXISTING 21 symbol designs, plus its approved parchment-and-leather name ribbon. The tiny flat symbol at the left of every entry is the authoritative OLD SYMBOL. The larger rendered symbol to its right is the previous FAILED low-effort execution: it was merely tinted and bevelled. Input image 2 is MATERIAL QUALITY REFERENCE ONLY: tactile, deliberately painted miniature objects integrated with a thick gray-stone fantasy UI. NEVER import its invented potion/arrow/quiver semantics into the target.
USER REQUEST: do a genuinely crafted artistic refinement of all of image1's existing symbols, especially the attack axe, not a global color change, gradient, extrusion filter, or generic stamped metal token. Retain existing semantics, identities, directions and recognizable silhouette topology. More refined internal construction is welcome; a different symbol is forbidden.

OUTPUT: one polished landscape sheet around 2048x1600 in the same composition as image1: the approved name ribbon across the top; 7 columns x 3 rows of the SAME 21 entries below. Retain tiny flat original glyphs on the left of each entry for comparison, place the newly painted icon to their right, and an exact small version plus existing numeric value below each. Keep the source-sheet ordering, all Chinese field labels, numbers and zero values. No missing, merged, duplicated, or moved entries. Make the painted icons as visually deliberate and tactile as image2, while retaining image1's symbol designs. The little compact samples must be recognizably the same as their enlarged counterparts.

Most important ATTACK AXE: retain the specific existing diagonal SINGLE-BLADED BEARDED AXE, handle rising lower-left to upper-right, wide curved single cutting head on the right side of the shaft; the old pictogram is reference, not a spear or two crossed swords. Model the actual parts: a compact dark-brown wooden haft with a few broad leather grip wraps at its lower portion, a steel socket where the blade attaches, thick dull forged-steel head, distinctly sharpened curved cutting bevel, clear dark back/side face and overlap/contact shadow. Give the head real convex/planar volume with 3-4 carefully chosen painted planes. The handle must visibly pass into the head with a plausible attachment, not float detached. Keep the original strong axe silhouette and compact proportions; DO NOT add a second blade, spikes, jewels, shield, backdrop, hanging ring or ornament. No speckle/noise layer masquerading as texture. This should look like a tiny well-painted usable axe, not an extruded computer font. Apply the SAME refined axe to the left end of the approved name ribbon beside 22 and both attack samples in the grid.

NAME RIBBON LOCK: retain the entire existing parchment and brown leather ribbon structure, center Chinese name 连弩手, left attack number 22 and right heart with 155. Do not add badges or rearrange these three zones. Only refine the attack object in this ribbon. Preserve quiet matte leather and paper, not glossy bronze.

PER-ICON ART DIRECTION, preserving the flat originals on image1:
Row1:
1 attack: above.
2 spell power: keep the exact violet comet/flame with two tapering lower-left tails and rounded upper-right core; paint coherent curved/faceted violet mineral-like planes and carved lower edges, NO staff, wand, added stars, cloud or circle.
3 health: existing single red heart, carved warm red mineral with broad softly rounded volume, dark thickness at lower/right edges and very limited diffuse highlight; not a potion, shield or shiny inflated plastic.
4 armor: existing upright shield with squared shoulders, outer band, inner shield face and the open gap separating them. Thick iron rim and subtly bowed darker steel inner plate, visible bevel, matte under-plane. Preserve the gap. No new circle or wood buckler, no central jewel.
5 attack speed: existing diagonal sword and exactly its two horizontal speed streaks at upper-left. Actual steel blade with one readable ridge, muted leather grip, plain guard, streaks as clean tapered bone/metal slivers. Keep sword not arrows/quiver.
6 attack range: existing circular crosshair with inner ring and four axial marks. Restrained antique-brass machined ring with cylindrical thickness and clear short marks, unobstructed central hole. No jewel or compass star.
7 critical chance: existing ochre angular burst and its dark central lightning-shaped cut. Layered/chipped golden mineral planes and visible inset lightning shape; same outer starburst points, no arrow.
Row2:
8 critical damage: existing terracotta asymmetric impact burst behind ONE pale diagonal blade. Give the blade an edge/spine, the red impact fragment broad planes and overlap depth. Preserve this composition and central dark marking, not a spray of arrows.
9 lifesteal: existing two long downward fangs joined across their top, with the original central small notches. Smooth aged bone, pronounced tapered fang volume and inner shadows; no blood bottle or heart, no added droplets.
10 dodge: existing leaning/running stick-person silhouette and its three horizontal motion lines to left. Refined shaped warm-bronze relief with round joint volumes, limbs reading as connected sculpted forms, not a new boot, not a full human figure. Preserve pose and motion marks.
11 control resistance: existing pair of diagonal broken chain sections and FOUR disconnected break/motion notches in their existing positions. Actual chunky iron links with squared/beveled bar cross section, open holes and readable separated ends. No closed unbroken chain, no shield.
12 movement: existing boot silhouette with short leftward speed bars. Actual matte brown leather boot using restrained sole, upper seam and ankle thickness inside the original contour; just two broad crease planes, no buckles/rivets/furs/extra ornament. Preserve its speed bars.
13 max mana: existing open-center turquoise waterdrop silhouette, a hollow droplet shape. Thick ceramic/mineral-like curved body with blue inner wall and broad diffuse edge, empty center remains empty. NO flask, bottle, stopper or added backing.
14 starting mana: existing blue open droplet plus pale right-side triangle and vertical start bar. Paint as same droplet material plus compact clearly raised silver start mark, retaining overlap and gaps.
Row3:
15 mana regeneration: same open blue drop plus the existing lower-right small clock. Blue ceramic/mineral droplet, plain pale metal clock ring and readable two hands on a dark recessed face. Not a bottle.
16 on-attack mana: same open blue drop plus existing lower-right diagonal SWORD. Deliberately crafted miniature steel blade/guard/handle, readable overlap; do not substitute arrow or axe.
17 on-damage mana: same blue open drop plus the existing lower-right shield with central lightning shape. Matte silver shield rim, dark steel face and legible bolt, no other decorations.
18 per-hit mana cap: same drop plus the original silver horizontal cap/bar and upward stop marker. These are clearly separately layered structural marks, preserve EXACT identity, not a clock or shield.
19 healing power: existing equal-armed bone/cream cross, clean softly beveled warm ivory with quiet face, no bottle, no extra square plaque.
20 active skill: existing lightning bolt, solid carved pale-brass volume with 2-3 broad broken planes and visible thickness, no star, arrows or gemstones.
21 passive skill: existing circular ring containing the four-point sparkle shape, clear separation of ring/central element; sculpted antique brass, uncluttered and not a new sunburst.

UNIFIED CRAFT: 2D stylized hand-painted fantasy miniature icon illustrations, not photorealistic, not pixel art, not simplistic SVGs. Warm soft light from upper left; real part-specific forms, edge breadth, plane breaks, modest wear only at functional edges, shape-coherent brushwork. Large quiet surfaces, substantial silhouette, dark contact shadows, controlled asymmetry in painted surface only. Different real materials must read differently: cool forged steel, warm bone, matte leather, subdued brass, red stone and blue mineral. Structure and construction, not arbitrary ornament, supply richness. Make the difference from the failed flat bevel icons unmistakable, while keeping the old semantic system.
AVOID uniform global bevel, flat colored fills, plastic smooth gradients, shiny chrome, oily highlights, lens flare, halos/glow, gritty noise textures, dense scratches, tiny rivets, decorative gems, background plates behind each stat, crossed weapons, redesigning the meanings, changing text/numbers, extra inventory slots or UI. Do not repaint a hero or whole card. This is a local asset craftsmanship sheet, not a full-game layout redesign.
Title: 旧图标 · 实物精修. Small footer: 材质与结构细化 · 静态预览. Preserve the approved name ribbon and demonstrate the detailed axe at useful size.
```

## 局部整理提示词

首稿大图已完成逐项精修，但紧凑示例混入旧平面符号。后续整理删除重复的旧小图，只保留每项一个精修图标与数值，避免冒充实际小尺寸资产；同时修正“SVG 原路径”的过强标注，位图精绘不宣称像素级路径完全一致。

```text
Precise local layout cleanup of THIS existing painted icon refinement sheet. Preserve every one of the 21 LARGE newly painted icons, their crafted materials and exact designs, the whole top name ribbon with its new detailed axe, typography, and 7-column x 3-row order. Do not repaint the art or reimagine any icon.
The problem is repeated old compact glyphs misleadingly displayed below the large icons. Remove the tiny old white/flat source glyph and little comparison arrow from the left of EVERY cell. Remove the obsolete compact icon-and-number pair from the bottom of EVERY cell. In each cell, center its EXISTING LARGE PAINTED ICON, with ONLY its existing value directly below it, and retain its existing Chinese field label above. No second miniature icon at all. Each of the 21 entries now displays exactly ONE painted icon, its label and value; no before/after comparison. Values must remain: row1 22,0,155,1,×1,5.2,0%; row2 150%,0%,0%,0%,×1,60,20; row3 5,10,50,20,0,主动,被动. This prevents falsely presenting the old flat small glyphs as completed new assets.
Keep main title 旧图标 · 实物精修. Replace the subtitle with 沿用既有图标造型 · 分材质与结构精修. Replace the comparison-explanation line above grid with 原有语义不变，逐项细化部件与体积. No claim that generated paint is pixel-exact SVG path data. Footer 静态美术预览 · 尚未替换游戏资源.
Absolutely preserve the large painted axe design: existing single bearded blade, wood shaft, leather grip. The shiny red heart in top ribbon remains unchanged from this input; do NOT alter other approved ribbon details. The newly painted 21 large icons must not drift. Just clean up duplicate miniatures and recenter each existing large icon with its numeric value. No missing entries. Same restrained charcoal background. No panels behind each attribute. High-resolution polished sheet, not a new art direction.
```
