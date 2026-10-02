# 单位卡挂件素材

2026-09-29：依据用户上传的“D · 战利品挂件”参考，使用内置 imagegen 提取／精制两类独立托座。仅替换顶部技能和装备承载面，未重画已确认属性图标、角色或原灰石框。

- `skill-medallion.png`：空白圆形技能托座，图标由运行组件叠加；原件 `exec-214ee810-8c35-4c94-9f70-d80f2d6cc9d4`。
- `equipment-hanger.png`：皮革 U 形装备扣，中间透明，＋与装备图由真实控件绘制；原件 `exec-5bf1c463-99be-4ed3-bb6d-09ea431b45a4`。
- 两者通过 `content/ui/unit-card-icons/*-mount.tres` 非破坏裁边、等比缩放，不使用九宫格拉伸。保留原图 alpha；无运行时外部绝对路径依赖。
- 托座导入启用 mipmap，并仅对托座启用线性 mipmap 过滤：源图约 1200 像素，正式控件约 56～76 像素，避免大幅缩小时铜边和皮革细节变成闪烁噪点。已确认词条图标的图像与过滤不受此设置影响。
- 来源为用户提供的项目审稿 `codex-clipboard-e00264c6-4985-4fdf-8ab2-a96466a783a6.png`，未从第三方下载新素材。此轮生成托座和实机整体观感仍待用户验收。

## 完整生成提示

### equipment

```text
Use case: background-extraction. Input image 1 is the user's approved fantasy UI reference. Extract and refine ONE empty leather equipment hanger from the top row of its LEFT character card (the U-shaped leather buckle with the short top attachment strap and horizontal iron tabs), using the enlarged equipment slots on the right as material detail reference. Make a single production-ready cutout on actual transparent background, front-facing, centered, complete outer silhouette with 10% transparent padding. Preserve the reference's shape: short thick brown leather top strap, dark riveted iron horizontal bar, descending U-shaped leather loop with broad worn edges. The opening inside the loop is transparent, NOT beige paper. Remove the plus sign: code will place it. No weapon, item, numerals, letters, labels, character, stone card frame, panel, paper or scene. Matte hand-painted brown leather and aged dark iron, quiet broad light/shadow planes, restrained detail, no oily shine or glowing outlines. Must remain legible as a 62x76 pixel equipment slot. Output ONLY this ONE hanger, not an atlas or presentation sheet.
```

### skill

```text
Use case: background-extraction. Input image 1 is the user's approved fantasy UI reference. Extract and refine ONE round skill medallion MOUNT from the top-left character card (matching the two circular skill buttons). Output a single front-facing round blank mount, centered on actual transparent background with 10% padding. Outer ring is thick aged bronze with four restrained faceted attachment notches, a dark muted reddish-brown leather inner disc. Large quiet plain center occupies 72 percent of the diameter so the engine can place the existing skill icon. Remove ALL arrows, weapons, crosshairs, diamond gems, stars, symbols, text, numbers and highlights from the center; no identity symbol belongs in the mount itself. Keep broad beveled planes and soft contact shadows, hand-painted tactile fantasy material consistent with the reference. No square tile, rectangular background, stone outer card, paper, character, scene, glow, thin concentric ornamental rings or new decoration. It must read well at 64x64 pixels. Output ONLY ONE blank medallion mount.
```
