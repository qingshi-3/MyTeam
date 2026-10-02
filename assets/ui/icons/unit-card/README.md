# 单位卡物件图标 · 2026-09-29

仅用于单位卡，保留既有语义；未替换全局单色 HUD 图标，未改外框和名称栏。

- `material-atlas.png`：内置 imagegen 从已审阅的 `design-discussion/04-content-validation/artifacts/unit-card-icon-studies-20260929/physical-icon-critical-clean.png` 提取透明图集；完整提示见 `generation-prompt.txt`。生成原件 ID：`exec-4f9adaf2-f898-4f62-8bc7-d9e2c850d64b`。
- `attack-speed.png`：直接复制用户最新确认的 `attack-speed-original-with-bands.png`，未重绘或改色。保留原弯刀、柄和宽条拖尾；图集中的旧攻速格不引用。
- 切片、边界与光学尺寸由 `content/ui/unit-card-icons/*.tres` 的 AtlasTexture 声明，不破坏原图。保留透明间隙，使用线性过滤与白色调制，不套语义色。
- 单位卡共 14 个展示条目：3 核心＋11 展开项，法力是初始／上限；四项回蓝不展示，战斗数据和机制不变。

## 来源与归属

原符号体系、精修过程和逐项依据见 `design-discussion/04-content-validation/artifacts/unit-card-icon-studies-20260929/physical-refinement.md` 及该目录历史稿。既有外部 SVG 候选的来源／授权保留在 `assets/ui/icons/candidates/game-icons/README.md` 与 `LICENSE.txt`，本次不删除或覆盖。

攻速拖尾构形参考 Lorc 的 [Blade drag](https://game-icons.net/1x1/lorc/blade-drag.html)，许可 [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/)。本项目以用户原弯刀为主体、加上宽条拖尾并作材质化改编，不代表原作者认可。生成与修改提示、用户参考图保留在上述讨论目录。

用户认可的是最新攻速方向并授权 UI 接入；本次图集提取及游戏中最终观感仍由用户验收。
