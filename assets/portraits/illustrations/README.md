# 单位动漫立绘

> 35 张独立动画外形，1024 × 1536 不透明 PNG；内置 image_gen 制作。

[打开立绘总览](index.html) · [源帧映射与提示词](manifest.json)

飞刃使最新：清理红黑衣料的掉色感杂斑，恢复连续布色与清晰金边；面具放大并调整外壳角度，呈贴扣在发面外侧的结构。保留黑长直与人物姿势，已更新总览。提示词见 [衣料与面具](ranged-fabric-mask.json)，此前发型记录见 [黑长直](ranged-straight-hair.json)。

按 35 套独立动画外形各生成一张；多个游戏单位共用同套动画时共用同一美术身份。立绘保留源帧物种、主色及标志装备，并按用户最新要求采用精致日漫面部与极简浅灰背景。背景为不透明 RGB，不是抠图。

本轮只交付独立图片，未替换现有 UI 动态肖像。所有生成图由负责 Agent 逐张检视；主 Agent 复查代表图、源键覆盖、文件尺寸与完整性。审美认可仍以用户反馈为准。

飞刃使最新追加：用户认可手臂位置后，要求左手掌心朝前，并将头顶装饰改为面具；已重绘画面右侧握柄手型、替换为红绳固定在头部左上侧的黑红描金狐面（额前、后脑位置均已按用户反馈纠正），保留脸和背身。最新提示词与分步记录见 [掌心与面具](ranged-palm-mask.json)，修前备份位于 `.godot/portrait-generation/ranged-palm-before/`；最终审美待用户反馈。

此前飞刃使结构修整：调整画面右侧角色左手至腰外侧，保留另一只伸展的手与背身回眸，红布明确领口与腰部连接。首轮改错手及小型角状冠被否定；后续大型月轮帽又按最新要求改为面具。历史记录见 [手臂与布料](ranged-logic.json)，备份位于 `.godot/portrait-generation/ranged-logic-before/`。

2026-09-12 浏览器批注修整已完成 7/7 张：金轮飞刃使保留背身回眸，修复多余手臂、握柄和腰臀服装；窃法者理清长裤与靴子；白袍医师增大法杖并留足边距；两位统帅加强成熟面容；弓手优化站姿、收束马尾；寒霜游刃明确成年男性剑士体态与低跟战靴。总量仍为35张，其余28张保持本轮修前版本。新图均已检视和解码，总览已更新缓存版本并实际点击核对。

本轮修前图片及旧目录记录位于 `.godot/portrait-generation/review-fix-before/`。完整提示词与编辑过程见 [手臂／服装](review-fix-anatomy.json)、[腿装／法杖](review-fix-costume-staff.json)、[成熟面容](review-fix-mature.json)、[体态／头发](review-fix-pose.json)。飞刃使第一版改变身体朝向，已按用户纠正弃用并从原图重做；后续定向修图应锁定原朝向，除非用户明确要求改变体态。

2026-09-12 毛边修整已完成 10/10 张，其余 25 张保留。此次收整碎毛、破布边和无意义碎亮点，保留源帧必要的角、爪、鳞片、武器与机甲结构，以及精致日漫脸和简单灰底；没有用模糊处理代替清晰收边。总量仍为 35 套独立动画外形，未新增或改变单位身份。

修整前图片保存在项目内 `.godot/portrait-generation/edge-cleanup-before/<animation-key>.png`。本轮提示词、输入备份、生成原件与逐图检视记录见 [赤影组](edge-cleanup-red-shadow.json)、[流沙组](edge-cleanup-desert.json)、[龙兽组](edge-cleanup-beasts.json) 和 [寒霜／首领组](edge-cleanup-frost-bosses.json)。修整结果已由负责 Agent 检视，不代表用户已认可审美效果。

最初复杂背景版本已由简背景版替换；原生成文件留在各记录指定的 .codex/generated_images。历次返修记录保存在 batch-*.json、edge-cleanup-*.json 和 review-fix-*.json，最终图片与完整提示词入口以 manifest.json 为准。部分衣摆或装饰接近画布边缘；f5_tank 的像素前突部件采用兽面／炮口复合解释。

以下名称只是画面识别标签，不是新增玩法设定。

## 金甲与日耀

| 画面 | 动画资源键 | 图片 |
| --- | --- | --- |
| 金刃统帅 | f1_general | [原图](f1_general.png) |
| 金甲弓手 | f1_ranged | [原图](f1_ranged.png) |
| 日耀战锤修女 | f1_sister | [原图](f1_sister.png) |
| 日铸枪骑 | f1_sunforgelancer | [原图](f1_sunforgelancer.png) |
| 日轮重盾卫 | f1_tank | [原图](f1_tank.png) |

## 赤影与秘术

| 画面 | 动画资源键 | 图片 |
| --- | --- | --- |
| 赤影双镰统帅 | f2_general | [原图](f2_general.png) |
| 灯火灵狐 | f2_lanternfox | [原图](f2_lanternfox.png) |
| 赤笠长刀客 | f2_melee | [原图](f2_melee.png) |
| 金轮飞刃使 | f2_ranged | [原图](f2_ranged.png) |
| 青焰窃法者 | f2_spellthief | [原图](f2_spellthief.png) |

## 流沙与构装

| 画面 | 动画资源键 | 图片 |
| --- | --- | --- |
| 金环白袍医者 | f3_aymarahealer | [原图](f3_aymarahealer.png) |
| 风沙旋灵 | f3_dervish | [原图](f3_dervish.png) |
| 流沙咒师 | f3_dunecaster | [原图](f3_dunecaster.png) |
| 金冠长戟统帅 | f3_general | [原图](f3_general.png) |
| 黄铜旋舞构装 | f3_irondervish | [原图](f3_irondervish.png) |

## 暗影与血族

| 画面 | 动画资源键 | 图片 |
| --- | --- | --- |
| 血族女爵 | f4_bloodbaronette | [原图](f4_bloodbaronette.png) |
| 暗影爬兽 | f4_crawler | [原图](f4_crawler.png) |
| 暗脊异兽 | f4_darkspine | [原图](f4_darkspine.png) |
| 紫晶魔将 | f4_general | [原图](f4_general.png) |
| 虚空机甲 | f4_mech | [原图](f4_mech.png) |

## 龙兽与重装

| 画面 | 动画资源键 | 图片 |
| --- | --- | --- |
| 翡翠幼龙 | f5_drogon | [原图](f5_drogon.png) |
| 青鳞喷火兽 | f5_firebreather | [原图](f5_firebreather.png) |
| 赤鬃龙将 | f5_general | [原图](f5_general.png) |
| 灵魂收割者 | f5_spiritharvester | [原图](f5_spiritharvester.png) |
| 青鳞重装 | f5_tank | [原图](f5_tank.png) |

## 寒霜与水晶

| 画面 | 动画资源键 | 图片 |
| --- | --- | --- |
| 水晶甲虫 | f6_crystalbeetle | [原图](f6_crystalbeetle.png) |
| 寒霜游刃 | f6_freeblade | [原图](f6_freeblade.png) |
| 霜翼鹰手 | f6_frostbitehawker | [原图](f6_frostbitehawker.png) |
| 冰刃统帅 | f6_general | [原图](f6_general.png) |
| 冰晶修女 | f6_sister | [原图](f6_sister.png) |

## 首领

| 画面 | 动画资源键 | 图片 |
| --- | --- | --- |
| 北境冰甲巨兽 | boss_borealjuggernaut | [原图](boss_borealjuggernaut.png) |
| 欺诈机神 | boss_decepticleprime | [原图](boss_decepticleprime.png) |
| 暗影领主 | boss_shadowlord | [原图](boss_shadowlord.png) |
| 血族魔环君主 | boss_vampire | [原图](boss_vampire.png) |
| 赤猿战王 | boss_wujin | [原图](boss_wujin.png) |


飞刃使五套换装方案：[对比页](f2-outfits/index.html)。图片、共同底图、内置 image_gen 完整提示词与生成记录位于 `f2-outfits/`；正式立绘未替换，方案待用户选择。


用户已选定无袖短和服最终版，统一保存为 `f2_ranged.png`；`f2-outfits/index.html` 展示同一图片，候选图已按用户要求清理。最终生成记录：`f2-outfits/kimono-sleeveless.json`。
