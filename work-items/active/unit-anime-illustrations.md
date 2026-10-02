# 单位动漫立绘

## 目标与授权

2026-09-12 用户要求依据每个单位的实际动画帧各生成一张精致动漫立绘，提供三张插画作为风格参考。本轮按 `assets/donor-units/` 的 35 套不同外形生成 35 张独立图片；当前不同游戏单位可共用一套帧。参考图只用于风格，单位外形以源帧为准。

## 范围与交付

- 使用内置 image_gen，每套素材单独调用；完整角色及标志性武器，竖版，保留人形／兽形及主要服装配色。用户后续明确要求极简低饱和纯色／轻渐变背景，不要建筑、树叶、星月、漂浮粒子和背景噪点。此前 10 张复杂背景版本全部返修，不作最终成品。
- 人形面部以用户新增参考图1（金轮女使，`codex-clipboard-442019f1-623b-4aa0-a8d0-0667386dd62d.png`）的精致日漫五官为准；明确拒绝参考图2（金甲男统帅）偏欧美／硬朗写实的脸。保持角色原性别与物种，不把兽类拟人化。
- 图片保存到 `assets/portraits/illustrations/<animation-key>.png`。该键可供当前和后续英雄复用，不绑定尚未完成的新英雄编号。
- 本次仅生成可审查美术资源及对应目录／提示词，不替换现有 UI 动态肖像，也不改战斗动画或玩法。
- 只在 main，保留既有未提交工作，不提交／推送。无需启动 Godot、导入或构建；仅检查生成图外形、构图、文件完整性和透明度。

## 进度与恢复

2026-10-01补齐游戏引用：用户反馈开局选人及多个页面仍显示旧图。确认上一轮只处理了旧英雄目录，默认GrowthGameRoot所复用的validation_matrix与growth目录仍有57份CardIllustration指向旧透明稿；已全部移除该覆盖，统一使用正式新PNG。另补齐已有同身份新图的敌人／士兵肖像引用，未提供对应新图的继续动画回退，战场动画不变。既有肖像检查扩展到全部运行肖像目录，避免再遗漏复用目录；真实默认主菜单的新局、六名候选选择／确认及英雄与装备入口已通过隔离输入核对，实际Texture读取新Illustration。旧32张画稿不重绘，完整路径、修前资源文本、取景同步、实机图与检查限制见[运行引用修正记录](../../design-discussion/04-content-validation/artifacts/portrait-rollout-20260930/runtime-reference-fix-20261001.json)。下方9月30日的完成记录仅覆盖当时旧英雄目录，不能据此认定默认成长模式已完整接入。

2026-09-30剩余32张优化已接入：用户明确要求继续优化所有剩余立绘，沿用此前确认的强外轮廓、整齐发束、简洁衣褶与阴影、合理比例及按单位配色的低饱和柔光底。以内置 imagegen 逐张编辑，保留物种、性别、主色、标志装备和源图朝向；部分画稿补做描边、材质或比例修订。32张1024×1536完整PNG已覆盖正式路径，32份英雄肖像资源移除旧CardIllustration引用，复用既有Illustration回退；原先三张已采纳角色的图片哈希保持不变，战斗动画和运行代码未改。[前后对照、完整提示词与正式卡面](../../design-discussion/04-content-validation/artifacts/portrait-rollout-20260930/index.html)以及输入副本、来源记录均已保存。低并发构建0警告0错误，单次批量导入与既有肖像检查通过；25种本轮有英雄绑定的外形在960×540隔离视口完成正式大卡／小肖像渲染及一次实际点击，另7张当前无英雄绑定。发现并定向调整沙丘施法者和灯笼狐的大卡取景，小肖像参数保持。检查范围与替换哈希见[接入记录](../../design-discussion/04-content-validation/artifacts/portrait-rollout-20260930/replacement.json)；未执行完整游戏流程，本轮新画稿最终审美仍待用户反馈。

2026-09-29三张配色背景已接入：用户在蓝灰骑士试稿后确认不同单位可用不同配色，并要求实施。骑士复用蓝灰稿，共鸣使、济世医师以内置 imagegen 分别改为灰紫、灰绿柔光底，保留已采纳角色造型与描边。三张1024×1536完整PNG已覆盖正式路径，继续由原Illustration引用读取，不改取景参数或运行时代码；旧白底定稿仍在outline-pass。完成文件哈希、导入、960×540正式大卡与小肖像渲染及读取路径检查；本轮仅背景替换，没有重跑交互或完整流程。[三张成图、提示词和实机卡面](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/background-study/index.html)已保存，新背景最终观感待用户反馈。

2026-09-29背景呈现试稿：用户认为透明抠图和白底均不好看，要求其他方案。以内置 imagegen 对已采纳骑士试做低饱和蓝灰柔光背景；使用相同正式卡片／小肖像组件及相同取景参数，在960×540隔离视口生成[白底与色底对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/background-study/index.html)。当前仅为背景方案预览，正式三张图片与资源引用未变，未记为用户认可或完整交互验收。

2026-09-29三张定稿接入：用户明确“可以 就这三个了”，允许不抠图直接读取。山岳旗手使用身体比例修订稿，共鸣使与济世医师使用描边加强稿；三张正式PNG已替换，对应肖像资源撤去旧CardIllustration引用，复用现有Illustration回退，不新增运行逻辑。低并发构建0警告0错误，既有肖像检查通过，三张在960×540隔离视口完成真实大卡／小肖像渲染和各一次点击，实际读取原PNG。保留浅色背景与原有窗口裁剪，不等同完整游戏流程验收；[接入记录](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/outline-pass/runtime-replacement.json)及运行截图已保存。

2026-09-29骑士身体比例修订：用户继续指出身体比例。以当前偏东亚面部稿为输入，内置 imagegen 调整头身、头肩与腿部关系，保留红发、金甲蓝衣及强描边；[同尺寸前后对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/outline-pass/knight-proportions-comparison.html)及提示词已保存。新稿1024×1536，已查看、复制哈希一致；提示词比例数值仅为目标，未记作实测。当前待反馈，未替换正式素材。

2026-09-29骑士面部局部修订：用户要求脸更亚洲化。以指定红发金甲版本的描边稿为输入，内置 imagegen 局部调整眉眼、鼻梁与下颌，保留原造型、姿态和强轮廓；[脸部放大对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/outline-pass/knight-face-comparison.html)、完整提示词已保存。新稿1024×1536，已查看且复制哈希一致；待反馈，女性描边稿及正式素材未修改。

2026-09-29三张描边加强：用户指定上传的红发金甲骑士版本为当前造型基准，暂停骑士其他设计方向；同时要求共鸣使和济世医师加深描边。以内置 imagegen 分别完成三张仅描边修订，保留造型与原有上色；[三张前后对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/outline-pass/index.html)、输入副本和完整提示词已保存。三张均1024×1536，已查看且复制哈希一致；本次描边强度待反馈，正式素材尚未替换，女性旧版认可／替换状态保留。

2026-09-29山岳旗手画法回归：用户否定金甲回色稿，指出与已采纳女性画风不一致。以内置 imagegen 直接参考济世医师／共鸣使绘法，重做[金甲战袍骑士](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/mountain-standard/index.html)，保留此前认可的成熟头脸、黄甲蓝衣，调整衣甲轮廓与站姿，另收敛碎高光。1024×1536，已查看、复制哈希一致；并排展示女性参考，本稿审美与画风统一程度待用户判断，未替换正式素材。

2026-09-29山岳旗手配色回调：用户认可上一版头脸，明确原单位黄色盔甲必须保留。内置 imagegen 以认可头脸所在图局部编辑，生成[金甲修订稿](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/mountain-standard/veteran-gold-v2.png)，保持简洁甲面与深色轮廓，恢复金黄甲和蓝披风。1024×1536，已查看且复制哈希一致；仅头脸此前获认可，本整图待反馈，未替换正式素材。

2026-09-29山岳旗手整体重做：用户否定前两版骑士风格及描边，旧面部修订不再待选。本轮以内置 imagegen 生成[成熟钢甲骑士新稿](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/mountain-standard/index.html)，改为银灰重甲、深蓝披风、整齐短发、稳重拄剑姿态并加强外轮廓；参考及完整提示词随图保存。已查看1024×1536成图，复制哈希一致；当前待反馈，未替换正式素材。

2026-09-29山岳旗手返修：用户否定此前油腻的面部与发型。已实际查看珀西瓦尔官方立绘，以其成年男性动漫五官作局部参考，内置 imagegen 重画为清爽青年骑士的脸与短发，保留身体、金甲和大剑；[修前／修后对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/mountain-standard/face-comparison.html)。新稿1024×1536，已查看、复制哈希一致，待反馈，正式素材未替换。此前“胡茬成熟脸”的处理已被否定，不沿用。

2026-09-29男性试稿：按用户要求选择山岳旗手（`f1_general`），以原图及已采纳共鸣使为参考，内置 imagegen 生成一张[优化稿与对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/mountain-standard/index.html)。重点减少碎甲、饰件与杂乱披风，保留成熟男性形象；1024×1536，已查看、复制哈希一致，原图未变。当前待本张反馈，未去背景、替换或运行引擎。

### 2026-09-29：济世医师采纳，逐张推进到共鸣使

共鸣使已正式替换：用户明确“接受替换掉”，`after-outfit-v2.png` 已替换正式 `f6_sister.png`，内置 imagegen 重新生成并替换 `cutouts/f6_sister.png`；引用与导入设置保持。已备份旧原图／透明图，其他角色图片哈希未变，清单和总览已更新。两张资源实际重新导入后，正式卡片／小肖像定向渲染与隔离视口实际点击检查完成，证据及限制见[替换记录](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/surge-conductor/replacement.json)。不把组件取景检查写成完整流程验收；源代码未改，未重建.NET。此状态覆盖下方待反馈记录，本轮未开始第三张。

共鸣使最新返修：用户在浏览器批注指出服装缺乏穿搭逻辑。已将上衣与下摆归并为完整蓝色无袖长战衣，黑裤作为内层，红色腰封束腰、白毛披风外披，删除挂环与无来源的碎片；[新稿与修前对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/surge-conductor/outfit-comparison.html)已保存。内置 imagegen 输出1024×1536，已查看，复制哈希一致，源35张未变。当前仍等待共鸣使反馈，未进入第三张或修改运行素材。

用户明确“可以了 这张采纳了。进行下一张，一张一张来”，采纳[济世医师比例修正版](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/healer-pilot/after-proportions-v2.png)。本轮以其为画法与比例参考，选择共鸣使（`hero_hc06_surge_conductor` / `f6_sister`），内置 imagegen 两步完成[下一张与原图对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/surge-conductor/README.md)。深色轮廓、短发与毛领、哑光裤装和人体比例为重点，身份与大型护臂保持。输出1024×1536，已目视查看、读取PNG规格并核对复制哈希，35张源立绘未变。当前只等待共鸣使反馈，不自动生成第三张；未替换运行资源，也未启动Godot。下方“济世医师待评价”属于采纳前历史。

### 2026-09-29：整套绘法认可与济世医师单张试改

最新反馈及修订：用户评价济世医师“好了不少了”，指出身体比例为当前缺陷。已以原试稿为输入，只针对头身、肩胯及腿部关系生成[比例修正版](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/healer-pilot/proportions.html)，保留上一版供对照；新稿1024×1536，已查看且复制哈希通过，待用户评价。比例数值为提示目标，不记作实测或用户验收；正式立绘及运行引用未替换。

用户认可外轮廓加深、略粗后的质感，并明确“可以用这些一整套的方案来优化我们的立绘了，先拿其中一个试试水”。本轮采用线条层级、整齐发束、必要衣褶、连续布面、克制装饰与安静背景的组合；[通用提示词](../../outputs/imagegen/anime-style-transfer-20260929/shared-style.txt)保留适量体积，不继承此前整体极简平涂的失败方向。

选择济世医师（`hero_hc18_healing_reader` / `f3_aymarahealer`），保留身份、白紫黑金配色、金环头饰、法杖及手势。内置 imagegen 两步完成[单张试改与对照](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/healer-pilot/README.md)。最终为1024×1536不透明PNG，已查看图像及浏览器对照，尺寸和复制哈希检查通过；原35张立绘哈希未变。当前仅交付独立试稿，未替换正式立绘、透明图或肖像引用，未启动Godot、导入或构建。新稿待用户判断，此前绘法认可不等于本张或游戏内卡面已验收。

### 2026-09-29：琴参考定向收敛（待反馈）

用户再次指出前两张油光发亮、头发杂乱，并提供琴立绘要求收敛。以该图为首要画法参考、上一轮双卡为身份和构图参考，内置 image_gen 完成一张[定向修订](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/jean-restrained-direction.png)：减少高光、碎发和碎装饰，保留满框／局部出框。完整提示和参考留在同目录。已查看结果并核对 1536×1024 RGB、复制哈希一致；男发仍有少量上挑轮廓，质量与审美等待用户判断。未替换正式素材或修改运行时，不启动引擎／导入。下节“两张待反馈”为此前阶段，现已被要求返修。

### 2026-09-29：现代二游立绘参照小样（待反馈）

用户授权多画风对照后，首轮四张被指出写实太强，第二轮三张仍未获认可，随后明确参照《原神》《鸣潮》。已实际查看 HoYoLAB 娜维娅／钟离对照图及鸣潮官方忌炎／今汐立绘，以图片参考输入内置 image_gen，完成两张新稿；完整提示、原件、来源及已否定记录见[小样索引](../../design-discussion/04-content-validation/artifacts/portrait-style-20260929/README.md)。两张均 1536×1024 RGB，复制哈希一致，已查看结果，未运行引擎或替换正式素材。不能宣称画风已达要求；最终取舍待用户。

用户另认可立绘满框／局部出框，而非认可其所截首轮图的画风。该构图与分层弹出单位 UI 的新倾向归 Q25 / D53 / UI14，不扩展成当前全量角色重绘或 UI 实施。下节保留最初仅诊断阶段。

### 2026-09-29：造型与画风反馈（仅诊断）

用户当前反馈及授权边界统一见[Q25 / D53](../../design-discussion/04-content-validation/decisions.md)：身份匹配尚可，但立绘单薄、质感不足、男性脸型同质化，轻浮画风不协调。下方 2026-09-12“精致日漫脸”等记录是当时制作方向，不得据其否定最新反馈；本轮没有批量重画授权，也不将 Agent 建议记为新风格已定。

本轮只读查看现存卡框内 `runtime/card-art-1.png` 至 `card-art-4.png`，并查看 `f1_general`、`f3_dunecaster`、`f6_freeblade`、`f1_sunforgelancer` 原图。前者是既有裁切检查图，不冒充本轮最新 UI 截图。Agent 观察：白发剑士、黑金法师的尖下颌、细颈、纤细四肢和光洁脸面容易读成同一种美型人物；金甲统帅虽有胡茬，长脸和装饰化甲面仍压过成熟结构。人物／衣甲大量細边与碎高光，主体厚度、承重和材质明暗不够突出；相比之下部分兽形具明确大体块，不需要全部同级推翻。这是局部样本判断，不是逐张全量验收。

候选方向尚待用户判断：角色脸型、年龄感、体态与职业个性分别设计；女性保留美感，男性允许英俊但不统一偶像脸。绘法倾向体块扎实、材质分明、明暗成组的风格化奇幻插画，让甲片有厚度、布料有重量，避免仅靠加暗、加噪点或加胡茬解释质感。下一步建议先用一名成熟男性与一名女性作卡框内小样，沿用角色身份和 UI 基准，再决定是否扩展；该建议未执行，未启动引擎／构建或修改图像。

### 狐面双刃刺客像素动画（已交付初版）

用户要求以最终无袖短和服立绘制作轻盈刺客动画，纠正首轮平滑动漫风，明确要求参考现有单位的像素素材。风格参考为f2_melee（80×80图集帧）和f6_freeblade（100×100图集帧），新图身份采用f2_ranged.png最终立绘而非旧射手动画。动作接口沿用idle/move/attack/skill_cast/hit/defeated，左右翻转遵循UnitAnimationComponent；仅新增美术资源和预览，不自动绑定现行英雄或更改玩法。

内置image_gen生成36帧6×6像素图集，原件exec-a3d96a39-d031-42bd-8b03-4afffeee47bc.png。图像工具生成背景为实色棋盘格；用户明确授权程序去底/对齐/切帧，已完成透明化。此前平滑动漫风试图不作为交付素材。

### 2026-09-12 飞刃使五套服装方案（已交付对比）

用户认可当前红黑金配色/光泽，以新增旧版截图仅参考较薄后脑；先局部收薄后脑，使用同一底图生成5套不同服装。固定当前狐面、头脸、黑长直、背身、手型/手臂、武器、腿脚姿势和背景。服装保持饱满红黑金与清洁光泽，不退回掉色杂斑。本轮另存方案，不覆盖当前 `f2_ranged.png`。

共同底图 `assets/portraits/illustrations/f2-outfits/base.png`，原件 `exec-dabdd316-e872-4811-ad11-bd13c4e21834.png`。5套方向为轻装忍衣、短和服、长战袍、武士轻甲、非对称裙装；各自PNG/JSON不交叉写入，主Agent统一建立对比页和清单。完成后记录角色固定项复核及图片规格，不启动Godot/构建/导入。

5/5方案已保存，入口 `assets/portraits/illustrations/f2-outfits/index.html`，完整提示词与来源见同目录manifest及batch JSON。主Agent逐图查看：服装方向各异，头脸、狐面、主要黑发流线、两手双武器和脚位保留；05腰臀短裤褶皱仍有前后读法歧义，记录为待用户选择，不宣称像素级不变。04追加一次后视甲片修正，03外侧长摆靠近画面边界。均解码为1024×1536不透明RGB且与生成原件哈希一致，35张正式立绘与既有manifest哈希全部一致，未替换。已查看真实对比页渲染并点击02打开完整PNG，未启动Godot或构建。发现旧预览服务未运行，已在127.0.0.1:8767恢复同目录本地服务，供继续比较。

### 2026-09-12 飞刃使衣料与面具可读性（已保存）

用户指出衣服像掉色、面具太小且像插入头发，授权清理衣料并放大/调整面具；此前冻结面具的要求被此局部修改覆盖。已清除红黑布面的随机浅色斑块、理顺金边纹样，放大狐面并表现外壳厚度与下缘贴扣关系，保留黑长直和人物姿势。当前原件 `exec-963499c3-d591-445c-bef5-7e7e02e0f88d.png` 已替换项目PNG，1024×1536 RGB，manifest/总览缓存已更新，其他34图哈希未变。提示词及来源 `ranged-fabric-mask.json`；备份 `.godot/portrait-generation/ranged-fabric-mask-before/`。用户审美待反馈。

### 2026-09-12 飞刃使黑亮轻薄长发（已保存）

后续要求减少毛躁、后脑更薄、黑长直且飘逸不乱。已将卷曲交叉发束整理为统一流向的顺滑长发，后脑发量收薄；当前最终原件为 `exec-b71c9f28-bb60-470f-8ef1-86679b374797.png`。1024×1536 RGB已替换，manifest与总览缓存同步；提示词 `ranged-straight-hair.json`，备份 `.godot/portrait-generation/ranged-straight-hair-before/`。面具位置及其他造型保留，未声明用户认可最终发型。

用户结束面具位置调整，明确“就这样”，仅要求头发更黑、有光泽、薄一些。当前面具位置冻结，不再按先前左右方位讨论移动。已定向调整黑发色泽和发束厚度，保留其他造型；新图 `exec-7bb0bd2f-25fe-4f76-a29a-1ba0d2884bbd.png` 已保存到项目并更新总览缓存，1024×1536 RGB，其他34图哈希未变。提示词/输入/原件在 `ranged-hair.json`，修前备份 `.godot/portrait-generation/ranged-hair-before/`。

### 2026-09-12 飞刃使掌心与头顶面具（已保存）

最新纠正：用户明确指人物头的左上边，不是后脑勺。前一步对后脑位置的理解错误已撤回；面具现上移到画面中头部左上侧的顶发，以斜向角度固定，保留脸、手臂位置与背身。当前最终图为 `exec-6c0a57be-dc83-42a6-9a62-10524514936b.png`，已更新项目PNG及总览缓存；前一步备份在 `.godot/portrait-generation/ranged-upper-mask-before/`，各步骤仍归 `ranged-palm-mask.json`。用户审美未确认。

用户认可当前手臂位置，明确要求角色左手（画面右侧）掌心朝前；后续要求头顶换成面具。已顺序执行手型局部重绘和头饰替换：头顶采用红绳固定的黑红描金狐面，原脸露出，保留背身、手臂位置、另一只伸展手和飘带结构。此前大月轮帽偏好由面具要求覆盖。最终用户审美未确认。

最终PNG已保存并更新manifest及总览版本；34张其他图哈希未变，PNG头部为1024×1536 RGB。完整两步提示词与原件路径见 `ranged-palm-mask.json`；修前图和目录备份在 `.godot/portrait-generation/ranged-palm-before/`。无需Godot导入或构建。

### 2026-09-12 飞刃使左手、头饰和红布结构

用户要求调整角色左手位置并避免右手手型，理清头饰和红飘带。**这里的角色左手指画面右侧、原先在身后下垂的手**；首轮错误修改画面左侧伸出的手，且小型竖月牙冠被用户否定，该生成版弃用，未替换项目PNG。后续恢复画面左侧原伸展姿势，将画面右侧角色左手抬到腰外侧；头饰恢复大体量黑金月轮帽，红布区分领口扣件连接的披巾和腰部侧结连接的系带。背身回眸、脚位、日漫脸和红黑色不变。

本轮内置image_gen提示词及两次生成记录在 `assets/portraits/illustrations/ranged-logic.json`；修前图片及manifest/index备份在 `.godot/portrait-generation/ranged-logic-before/`。仅修该图及资源记录，保留其他34张与现有游戏接入状态。

新版已保存并更新总览缓存，1024×1536不透明RGB，解码通过；其余34图哈希未变。定向只读复核未见明确多指/断腕，背身和另一只伸展手保留；拇指根/掌面部分被握柄遮挡，左右手型未能完全确认，不能记为用户认可。

### 2026-09-12 浏览器批注修整（已完成）

用户对总览中的7张给出明确批注，授权定向优化：①f2_ranged修复第三只手、握武器方式和腰臀服装结构；②f2_spellthief理清腿部服饰；③f3_aymarahealer增大法杖；④f1_general与⑥f3_general加强成熟统帅年龄感；⑤f1_ranged优化站姿并收束头发；⑦f6_freeblade纠正体态并明确男性或女性表达。继续保持精致日漫风、低噪点简单灰底及干净收边。

主Agent负责f2_ranged与最终整合；3个Agent分别负责成熟统帅2张、腿装/法杖2张、弓手/游刃体态2张，仅写各自图片及review-fix JSON记录。修前备份统一保存于 `.godot/portrait-generation/review-fix-before/`，禁止覆盖已有备份；新图逐张查看并通过后替换，生成原件留存。仅更新美术和现有目录记录，不改玩法或运行时接入，不启动Godot/构建/导入。

用户即时纠正：f2_ranged必须保持原来的背身回眸，不能借修手和服装改成正面。首轮朝向错误版本弃用并留记录，返回修前原图重做。除用户点名要改站姿的弓手/游刃外，各图锁定原朝向与姿态。f6_freeblade源帧无法确定性别，当前平坦胸廓/肩形更近纤细男性，本轮按成年男性剑士明确肩腰髋、重心和低跟靴；不作为游戏性别设定变更。

已完成7/7张并替换。主Agent逐张查看全部最终图；f2_ranged另由Agent对照修前图复核两臂两手、握柄与背身臀部衣片结构，无明显遗留问题。f3_aymarahealer扩大法杖后再整体增加留白，杖顶约157px；f3_general追加一次中年面部结构编辑。逐图完整过程在 `review-fix-anatomy.json`、`review-fix-costume-staff.json`、`review-fix-mature.json`、`review-fix-pose.json`，生成原件及被替代步骤保留。

整合与验证：manifest保留35源键及全部旧版本来源，7张新图与生成原件一致，7张备份与修前manifest哈希一致，其余28张图片和条目未改。7张最终PNG均解码成功，统一1024×1536不透明RGB。总览已更新7张的图片/原图链接版本，CUA已查看实际总览并真实点击飞刃使，确认加载最终背身版本。未启动Godot、导入、构建或改变游戏接入；用户审美仍以反馈为准。

### 2026-09-12 毛边修整（已完成）

用户要求继续制作时减少刺眼毛边，指出灯火灵狐、赤影双镰统帅、赤笠长刀客。核对后现有35套动画已全部覆盖，没有新的遗漏源帧。3个Agent逐图只读复查其余32张，确定额外7张存在同级问题：f3_general、f4_general、f5_general、f5_firebreather、f6_freeblade、f6_sister、boss_shadowlord。本轮10/10张毛边修整已完成，其他25张保留；总量仍为35套独立动画外形，全部为1024×1536不透明PNG。

统一处理：毛发收成少量顺滑大束，布面和飘带完整收边，减少破洞、碎布尾、密集细尖刺与无意义磨损亮点；不模糊糊边、不消除物种必要的角爪鳞片/机甲结构，保留精致日漫脸、身份、主色和简单灰底。主Agent完成f2三张，其他Agent分别完成额外3/3/1张，文件归属不重叠；仅主Agent更新总览和统一manifest。10张修整结果均已由负责Agent逐图检视，未启动Godot或改变游戏接入；用户审美认可仍待反馈。

修前备份保存在 `.godot/portrait-generation/edge-cleanup-before/<animation-key>.png`，旧 `manifest.json` 和 `index.html` 也备份于该目录；各次生成原件仍留在 `.codex/generated_images` 对应路径。本轮4份记录位于 `assets/portraits/illustrations/`：`edge-cleanup-red-shadow.json`、`edge-cleanup-desert.json`、`edge-cleanup-beasts.json`、`edge-cleanup-frost-bosses.json`；每项包含动画键、完整编辑提示词、输入备份、生成原件、最终文件、视觉检视备注和尺寸。恢复或继续修整时按记录找到原图，保留当前角色身份与上述约束。

本轮验证：主Agent已更新统一manifest和总览，确认源帧映射无遗漏，新图与生成原件一致、修前备份与旧记录哈希一致，其余图片及记录未改；所有PNG均为1024×1536的8-bit RGB。已通过CUA查看更新总览，并真实点击灯火灵狐原图，确认带新版本参数的图片正常显示。以上是资产与实际浏览验证，不代表用户审美认可。

### 首版交付

2026-09-12 已完成 35/35 张，均为 1024×1536 PNG，约69 MiB。前10张复杂背景全部换成简洁浅灰底；人脸按用户指定金轮女使参考返修。三个Agent完成其余8/8/9张；另定向修复弓手整弓裁切、金冠统帅冠顶、霜翼鹰手羽翼和冰刃统帅剑尖。弓手最终改为腰侧持弓待命姿势。

全部图片已逐张由负责Agent检视，主Agent复查代表图、35套源键覆盖、PNG头部尺寸及SHA-256，零漏项。各Agent完成所属图片解码检查；没有启动Godot、导入或构建。静态浏览总览已查看实际渲染，并通过真实鼠标点击打开弓手完整原图。用户审美认可仍待反馈，部分衣摆／装饰靠近边缘；f5_tank的低分辨率前突部件采用兽面／炮口复合解释，详见资产记录。

交付入口：`assets/portraits/illustrations/index.html`、`README.md` 和统一 `manifest.json`；manifest逐图包含当前单位关联、源帧、最终提示词、图片尺寸与哈希，批次细节保存于同目录batch JSON。当前单位关联读取整理时的工作区内容，包含另一任务正在加入的英雄，只读收录，不修改其内容。全部图片为不透明浅灰底，未接入游戏UI。

本地只读总览服务：`http://127.0.0.1:8767/index.html`，进程信息保存在 `.godot/portrait-generation/gallery-server.json`。即使服务结束，也可直接用浏览器打开项目内index.html；无外部发布。

最终提示词集与源帧／图片映射保存于资产目录 `manifest.json`，生成方式为内置 image_gen。


### 短和服丝袜优化

用户选定02短和服，要求丝袜更柔美。使用内置image_gen仅编辑丝袜材质，改为较薄的黑色织物与柔和透肤明暗，减少膝弯硬褶；已查看并解码1024×1536 RGB。新图另存f2-outfits/02-short-kimono-soft-stockings.png，对比页02已指向新版，旧版保留，正式f2_ranged未替换。提示词和来源在kimono-soft-stockings.json，修改审美待用户反馈。

用户进一步要求丝袜无褶皱，已用内置image_gen去除两膝堆褶，另存f2-outfits/02-short-kimono-smooth-stockings.png并更新对比页02，旧图保留。已查看与解码1024×1536 RGB，提示词和来源见kimono-smooth-stockings.json。

用户要求清理头发/衣服掉色并收腰约10%，已使用内置image_gen生成f2-outfits/02-short-kimono-rich-color-waist.png，查看确认黑发和红黑衣料更饱满、腰部适当收窄；10%为视觉近似，未声称精确测量。保留无褶皱丝袜，对比页02更新，旧图保留。PNG解码1024×1536 RGB通过，提示词/来源见kimono-rich-color-waist.json。

用户要求去掉上衣长袖和上衣红飘带、腰间长飘带缩短约1/4。已使用内置image_gen另存f2-outfits/02-short-kimono-sleeveless.png并更新对比页02，旧图保留。已查看并解码1024×1536 RGB，飘带长度为视觉近似；提示词/来源在kimono-sleeveless.json。


用户确认无袖短和服并要求只保留此版。已将选定图设为正式f2_ranged.png；清理f2-outfits候选PNG及对应import，比较页改为单张最终图。其他34张正式图片哈希保持不变。最终提示词/来源保留在f2-outfits/kimono-sleeveless.json；未接入游戏UI。

像素动画交付：assets/units/fox-mask-assassin/，36张80×80 RGBA独立帧、480×480透明图集、6组SpriteFrames动作、独立preview.tscn、GIF、交互预览与ZIP。源图保留，脚本完成中性棋盘格去除、最近邻缩放、32色量化及脚底对齐；未生成程序姿势。全部帧尺寸/真实透明/四边留白/图集逐帧一致性通过，Godot资源静态结构通过。已实际操作浏览器播放、暂停、逐帧、朝向和深浅背景，确认倒下停在第6帧；未运行Godot，未绑定现有单位。预览http://127.0.0.1:8768/index.html，用户动作观感与审美待反馈。
