# 真实 UI 接入与核对

2026-09-25。本次针对用户“各小字段深度隔离，十分割裂”的反馈修正真实 UI。保留外石框、立绘石环、名称纸条；撤掉身份牌、生命槽壳、主被动铭牌及卡内独立装备框。下半部使用连续卡面：状态／生命共行、核心属性一行、主被动各一行、三装备位共用底区。以下为 Godot 生产场景在隔离测试状态下的实际渲染，默认 1600×900，非整页生成图。

- [英雄与装备](equipment.png)：大立绘、生命条、单行核心属性、主被动和三个装备槽。
- [已穿戴状态](equipment-equipped.png) · [拖放目标反馈](equipment-drag.png)：图标与空位保留小序号；卡内槽使用轻量反馈，整卡投放保留石框并提示“放入装备”。隔离视口截图不含外层原生拖动预览；其名称另由真实拖放过程中的控件文本检查覆盖。
- [整备完整详情](equipment-details.png) · [更多属性](equipment-attributes.png)：点击属性／技能打开；名称、返回和装备槽固定，完整规则可滚动阅读。
- [后备滚动与配装](equipment-reserve.png)：背包保持可见，后备仍可直接穿戴装备。
- [招募](recruitment.png) · [技能与完整详情](recruitment-details.png)：正面大图，查看不招募，只有明确确认提交。
- [900 宽招募](recruitment-narrow.png)：两列和末卡仍可访问，完整详情保持独立滚动。
- [商店](shop.png)：真实价格、可购买／不足状态。
- [战利品](reward.png)：独立预览状态，采用紧凑商品卡比例；并非自然战斗胜利证据。
- [主菜单](main-menu.png)：参考图按钮与窗口材质。
- [装备提示](equipment-tooltip.png)：轻量不透明提示，不套大型窗口框。
- [开局六选二](opening-selection.png)：六张纵向候选卡，右侧保留独立完整阅读面板。缺图英雄继续像素动画。
- [整备属性提示](portrait-tooltip.png)：立绘区域仍支持原生选择、悬停查看和装备投放。

扣除石环上缘和名称纸条后，可见开窗图高／整卡高度：招募 **60.2%**、整备 **61.3%**；开局候选扣掉环的上下缘为 **64.1%**。这是高度比，不是人物像素面积比；右侧完整阅读面板不采用大图占比。本次被否定的重框前版：[整备](before-cohesion/equipment.png) · [招募](before-cohesion/recruitment.png)。增加石环之前的更早版：[整备](before-ring/equipment.png) · [招募](before-ring/recruitment.png) · [开局](before-ring/opening-selection.png)。更早的灰底大图和小肖像版分别保留在 `before-cutout/`、`before-large-art/`。

输入检查覆盖点击、键盘展开／返回、拖放、取消、详情与后备滚动、模态焦点和减弱动效设置，构建通过；并非全局通关或整套审美验收。当前 `--isolated-pointer` 用独立 SubViewport 渲染并处理真实 GUI 事件，不移动用户桌面鼠标，也不把这当作操作系统窗口焦点验收。执行状态与后续差距以 [活动任务](../../../../../work-items/active/ui-material-overhaul.md) 为准。

继续实施：招募／商品卡已接入独立名称纸条，商店／战利品改为紧凑居中陈列，卡内滚动轨道加宽。石框的侧边、底边、配色与已认可版保持一致。名称纸条使用内置 imagegen，生产素材与提示保存在 `assets/ui/tavern/reference-name-ribbon.png` 和 `name-ribbon-generation-manifest.json`。

立绘取景：35 份英雄绑定使用 28 张透明衍生图，其余 3 名暂无对应图。大小展示使用同一透明源与分别配置的焦点／缩放；大图按卡窗与纸条裁切，小图按椭圆窗口近景取景。原 PNG 保留，战斗继续像素动画。衍生图由内置 imagegen 编辑，有局部线条／尺寸漂移，不承诺逐像素提取；完整提示与来源见 [generation.json](../../../../../assets/portraits/illustrations/cutouts/generation.json)。

取景检查夹具：七页真实卡片框内构图 [1](card-art-1.png)、[2](card-art-2.png)、[3](card-art-3.png)、[4](card-art-4.png)、[5](card-art-5.png)、[6](card-art-6.png)、[7](card-art-7.png)，为本次去框之前的环内取景证据，卡内文字／信息为占位，不代表当前信息区布局；此前三页原图／透明小肖像对照 [1](portrait-crops-1.png)、[2](portrait-crops-2.png)、[3](portrait-crops-3.png) 保留。这些仅证明取景，不代表完整运行状态；上方整备、招募等页面才是实际内容布局。

最新验证：低并发构建 0 警告／0 错误；招募／offer 查看与确认、900 宽布局、开局选择／取消、整备穿戴／替换／转交／卸下、图标槽拖动预览名称、详情／键盘返回、后备滚动和模态焦点通过。日志为 `.godot/ui-cohesion-offers.log`、`.godot/ui-cohesion-roster-final.log`。本次未改人物取景／绑定，未重复全部 28 张取景测试。前版小控件提示保留在 [card-controls-generation.json](../../../../../assets/ui/tavern/card-controls-generation.json)，技能牌和身份牌已退出运行主题。缺图英雄、其他物品底色、跨页面一致性和完整动态对照仍需继续，整体目标未完成，新结果尚未取得用户审美认可。
