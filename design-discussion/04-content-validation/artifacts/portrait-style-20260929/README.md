# 立绘画风与满幅卡面小样 · 2026-09-29

2026-10-06 参考范围已扩充：用户从统一候选表选择十项立绘参考，重点评价昆特 Geralt: Igni 的动态感与视觉冲击，并新增覆盖全部美术类别的 DNF 参考，当前选择见 [D77](../../decisions.md#portrait-reference-selection)，提炼建议见[当前讨论](../../content-outline.md#portrait-style-guidance)。本目录保留已认可画法、济世医师与逐张迭代的具体历史证据；参考扩充不自动否定这些认可，也不表示新增样稿或运行替换已完成。

2026-09-29已清理6张早期失败草稿、11个完全重复的图片副本，以及失效预览导入旁文件和本轮临时检查文件；保留采纳稿、提示词、参考图及每份回退原图的一份实体副本。旧引用已指向保留文件，清单见[清理记录](cleanup-20260929.json)。

## 当前逐张优化状态

用户已明确采纳[济世医师比例修正版](healer-pilot/after-proportions-v2.png)，要求“进行下一张，一张一张来”。第二张[共鸣使](surge-conductor/README.md)已交付，待本张反馈；不自动继续第三张。逐图状态见[清单](sequential-portrait-status.json)。美术采纳与运行资源接入分开记录，当前没有替换运行素材。以下为此前探索历史。

## 最新：绘法认可后的济世医师单张试改

用户已认可加强外轮廓后的整套绘法，并授权先优化一张现有立绘。[济世医师试改](healer-pilot/README.md)包含前后对照、两步完整提示词与来源，当前新稿待反馈，原运行素材未替换。下方保留此前独立小样的历史记录，其待反馈状态不覆盖本次绘法认可。

当前为选风格小样，均由内置 `image_gen` 制作，不是运行 UI，不替换 `assets/portraits/illustrations/`。人物沿用红发金蓝甲统帅、金发白紫金枪骑的识别特征；允许样稿重新组织服装、表情和体态。正式身份与素材接入仍待确认。

## 最新：按用户琴立绘定向收敛（待反馈）

[定向修订双卡](jean-restrained-direction.png)以用户提供的[琴立绘](user-jean-style-reference.png)为主要画法参考，上一轮双卡仅提供角色和构图。收敛金属亮斑、发丝与装饰密度，保持满框及局部出框；不通过滤镜后处理。由内置 image_gen 重绘，完整提示与输入／输出路径见 [jean-restrained-prompt.json](jean-restrained-prompt.json)。

已查看结果：大块甲面高光减弱，女性长发成束，男性仍有少量上挑轮廓。PNG 为 1536×1024 RGB，复制哈希与生成原件一致；未替换游戏素材、未启动引擎。尚未获用户认可，不能记为风格定稿；武器仍触及画布边界，也不是可直接接入的分层资源。

## 上一轮两张（已被要求返修）

- [原神参考方向](genshin-direction.png)
- [鸣潮参考方向](wuthering-direction.png)

两张均为 1536×1024 RGB，已查看生成结果，复制后哈希与原件相同。完整提示词、输入角色、来源与生成原件路径在 [prompts.json](prompts.json)。没有通过后处理滤镜换风格。用户随后明确批评油光发亮、头发杂乱，提供[琴立绘](user-jean-style-reference.png)要求收敛，以此覆盖上轮笼统风格描述；两张保留为返修历史。部分出框武器延伸到画布边界，这些小样不能直接作可复用裁切资源或出框交互验收。

## 参考与范围

- [HoYoLAB 社区对照图](https://www.hoyolab.com/article/29994301)：实际查看的是娜维娅和钟离立绘拼图，帖子属于社区，不标作官方作者发布；搜索摘要曾误称琴，以实际图片为准。
- [鸣潮官方角色页](https://wutheringwaves.kurogames.com/en/main/)：从页面公开资源映射取得并查看[忌炎](https://wutheringwaves.kurogames.com/static4.0/assets/jiyan-53f0b628.png)与[今汐](https://wutheringwaves.kurogames.com/static4.0/assets/jinxi-422f1400.png)。

参考只用于绘法分析与生成输入，不把原作角色或其素材接入游戏。临时参考缓存位于 `.godot/ui-review/portrait-style-refs/`；联网图片检索、网页读取及实际原图查看均已执行，官方页浏览器加载曾超时，未冒充成功浏览。

## 已否定与已认可部分

首轮四种厚涂／油画／日式厚绘／雕塑感方案被用户否定：写实过强，动漫感太弱。第二轮三种线稿／日系游戏／手绘动画方案仍未获认可，用户要求明确参照《原神》《鸣潮》。两轮提示与生成原件路径分别保留在 `rejected-first-pass.json`、`rejected-second-pass.json`，不作为后续认可基准。

用户单独认可[满幅与局部出框构图](user-approved-composition.png)，不等于认可该图画风。由此提出单位 UI 倾向：立绘占满，基础核心信息常驻，其余通过 hover／点击，配合向上下左右渐显＋弹出。具体字段、触发、方向与时序未定；未修改正式 UI 或制作交互原型，讨论归 Q25 / D53。
