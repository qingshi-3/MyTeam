# 动态光影素材提示词

2026-09-30，内置 image_gen 工具。两次都以原 `courtyard.png` 为明确参考，生成后原样复制到本目录，未作后期图像修改。运行时只引用仓库内素材。

## 均匀受光底图 courtyard-albedo.png

```text
Edit target: the provided sunlit ruined courtyard game background. Create a production ALBEDO/DIFFUSE BASE version for real-time lighting in a 2D game.
Preserve invariants aggressively: same full 16:9 framing, same oblique horizontal top-down camera, same arch and stone wall positions, same four corner autumn trees, same ivy and yellow/orange foliage, same stone paths and irregular mossy cobblestones, same open empty battle floor, same pixel-art texture, colors and detail density. No new objects, no characters, no interface, no grids.
Single change: remove the large cast canopy shadows and irregular bright sun patches on the courtyard floor. Make the whole floor and vegetation illuminated by soft, EVEN, neutral overcast daylight. Keep intrinsic material colors, tiny crevices and contact occlusion around roots/walls/rocks, keep pixel details and solid forms. The central floor must have even diffuse values, no baked beam, no sun shafts, no diagonal lighting band, no dappled canopy silhouette. Do not flatten all material colors to gray. This neutral base will receive dynamic warm sunlight and animated canopy shadows in-engine. Do not change structure, crop, redraw composition or add fog.
```

## 树冠投影遮罩 canopy-cookie.png

```text
Use case: texture asset. Generate one grayscale light-cookie texture for dynamic dappled overhead tree canopy shadows in a 2D pixel-art courtyard. Use the referenced courtyard ONLY to choose broad projected canopy shadow shapes and distribution, not as an image to redraw. Output a full-frame 16:9 MONOCHROME shadow mask, flat orthographic texture, no scene content.
WHITE means open daylight. Dark gray/black means foliage occlusion. The mask should look like soft natural SHADOW SILHOUETTES of leafy tree canopies and thin irregular branching twigs projected diagonally from upper-left to lower-right across a rectangular ground plane. Large clustered canopies near left/right edges; a few broad broken branch and canopy shadows crossing center diagonally. Broad white openings of varied sizes between clusters, smaller ragged leafy holes and clustered lobes inside the darker crowns, slight soft penumbra, organic airy recognizable leafy silhouettes rather than granular noise blobs. The central ground has about 55 percent open white sunlight and 45 percent grouped gray shadow. Light from upper-left. Soft grayscale edges, no gradients simulating scene lighting, no arrows, no framing.
The entire image is ONLY a usable seamless-looking canopy SHADOW MASK; absolutely NO colored foliage, stones, masonry, textures, terrain, trees drawn in elevation, trunks drawn standing upright, user interface, rays, fog, particles, characters, labels, annotations. White background and black/gray canopy shadow shapes. Do not include the reference courtyard itself. It will be used as moving shadow projection in a shader, not as a game background.
```
