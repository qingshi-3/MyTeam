# 真实 UI 接入与核对

2026-09-25。本次根据用户“少写字，多用符号 UI”的反馈，把卡面标签改为图形＋数值：心形生命上限、星标阶位、出战／后备轮廓、主被动类别符号及无标签装备槽。招募属性同样使用现有语义图标，阶位与职责放在上方两角；技能名称、必要操作按钮保留文字，完整解释可悬停／键盘聚焦或点击查看。保留外石框、立绘石环、名称纸条和连续底材，不重新给字段加框。以下为 Godot 生产场景在隔离测试状态下的实际渲染，默认 1600×900，非整页生成图。

每战满血规则保持：正式爬塔战斗按有效生命上限入场；休息保留原金币选项，事件失败无收益且不扣血。

- [英雄与装备](equipment.png)：大立绘、心形数值、星标阶位、单行属性图标、主被动符号和三个无标签装备位。
- [已穿戴状态](equipment-equipped.png) · [拖放目标反馈](equipment-drag.png)：图标与空位不显示序号，序号保留在可访问名称中；卡内槽使用轻量反馈，整卡投放保留石框并提示“放入装备”。隔离视口截图不含外层原生拖动预览；其名称另由真实拖放过程中的控件文本检查覆盖。
- [心形悬停说明](health-tooltip.png)：解释有效生命上限、基础值和开战加成；点击进入完整详情，返回恢复焦点。
- [整备完整详情](equipment-details.png) · [更多属性](equipment-attributes.png)：点击属性／技能打开；名称、返回和装备槽固定，完整规则可滚动阅读。
- [后备滚动与配装](equipment-reserve.png)：背包保持可见，后备仍可直接穿戴装备。
- [招募](recruitment.png) · [技能与完整详情](recruitment-details.png)：正面大图，查看不招募，只有明确确认提交。
- [900 宽招募](recruitment-narrow.png)：两列和末卡仍可访问，完整详情保持独立滚动。
- [商店](shop.png)：真实价格、可购买／不足状态。
- [休息](rest.png)：仅保留原金币选项，单张卡居中并限制宽度。
- [事件](event.png)：保留原成功概率、成功金币及保守金币选项；冒险失败无收益、不扣血。休息和事件仍使用通用机会图标，尚未制作专用插图。
- [战利品](reward.png)：独立预览状态，采用紧凑商品卡比例；并非自然战斗胜利证据。
- [主菜单](main-menu.png)：参考图按钮与窗口材质。
- [装备提示](equipment-tooltip.png)：轻量不透明提示，不套大型窗口框。
- [开局六选二](opening-selection.png)：六张纵向候选卡，右侧保留独立完整阅读面板。缺图英雄继续像素动画。
- [整备属性提示](portrait-tooltip.png)：立绘区域仍支持原生选择、悬停查看和装备投放。

扣除石环上缘和名称纸条后，可见开窗图高／整卡高度：招募 **60.2%**、整备 **61.0%**；开局候选扣掉环的上下缘为 **64.1%**。这是高度比，不是人物像素面积比；右侧完整阅读面板不采用大图占比。符号化修改前：[整备](before-symbols/equipment.png) · [招募](before-symbols/recruitment.png)。每战满血修改前：[整备](before-full-health/equipment.png) · [详情](before-full-health/equipment-details.png)。此前被否定的重框版：[整备](before-cohesion/equipment.png) · [招募](before-cohesion/recruitment.png)。增加石环之前的更早版：[整备](before-ring/equipment.png) · [招募](before-ring/recruitment.png) · [开局](before-ring/opening-selection.png)。更早的灰底大图和小肖像版分别保留在 `before-cutout/`、`before-large-art/`。

输入检查覆盖点击、键盘展开／返回、拖放、取消、详情与后备滚动、模态焦点和减弱动效设置，构建通过；并非全局通关或整套审美验收。当前 `--isolated-pointer` 用独立 SubViewport 渲染并处理真实 GUI 事件，不移动用户桌面鼠标，也不把这当作操作系统窗口焦点验收。执行状态与后续差距以 [活动任务](../../../../../work-items/active/ui-material-overhaul.md) 为准。

继续实施：招募／商品卡已接入独立名称纸条，商店／战利品改为紧凑居中陈列，卡内滚动轨道加宽。石框的侧边、底边、配色与已认可版保持一致。名称纸条使用内置 imagegen，生产素材与提示保存在 `assets/ui/tavern/reference-name-ribbon.png` 和 `name-ribbon-generation-manifest.json`。

立绘取景：35 份英雄绑定使用 28 张透明衍生图，其余 3 名暂无对应图。大小展示使用同一透明源与分别配置的焦点／缩放；大图按卡窗与纸条裁切，小图按椭圆窗口近景取景。原 PNG 保留，战斗继续像素动画。衍生图由内置 imagegen 编辑，有局部线条／尺寸漂移，不承诺逐像素提取；完整提示与来源见 [generation.json](../../../../../assets/portraits/illustrations/cutouts/generation.json)。

取景检查夹具：七页真实卡片框内构图 [1](card-art-1.png)、[2](card-art-2.png)、[3](card-art-3.png)、[4](card-art-4.png)、[5](card-art-5.png)、[6](card-art-6.png)、[7](card-art-7.png)，为本次去框之前的环内取景证据，卡内文字／信息为占位，不代表当前信息区布局；此前三页原图／透明小肖像对照 [1](portrait-crops-1.png)、[2](portrait-crops-2.png)、[3](portrait-crops-3.png) 保留。这些仅证明取景，不代表完整运行状态；上方整备、招募等页面才是实际内容布局。

前轮满血规则验证：低并发构建 0 警告／0 错误；满血规则回归覆盖生命上限加成、旧伤势存档、阵亡再部署、下一战、实验室受伤起点、两种生命上限视图、休息单次领取／保存失败回滚、默认事件成功／失败与旧待决项。日志为 `.godot/run-full-health-final.log`。该服务检查使用明确构造的结算结果，未声称自然战斗获胜。

前轮真实 GUI 输入检查通过招募／offer 查看与确认、900 宽布局、开局选择／取消、整备穿戴／替换／转交／卸下、图标槽拖动预览名称、详情／键盘返回、后备滚动和模态焦点；休息／事件实点击确认验证金币结算与生命不变。日志为 `.godot/ui-full-health-roster.log`、`.godot/ui-full-health-offers-final.log`。本次未改人物取景／绑定，未重复全部 28 张取景测试，也未运行整局平衡测试。前版小控件提示保留在 [card-controls-generation.json](../../../../../assets/ui/tavern/card-controls-generation.json)，技能牌和身份牌已退出运行主题。缺图英雄、机会图标、其他物品底色、跨页面一致性和完整动态对照仍需继续，整体目标未完成，新结果尚未取得用户审美认可。

最新符号 UI 验证：构建 0 警告／0 错误；`.godot/ui-symbols-roster.log` 通过心形／状态图形悬停解释、生命点击详情／返回焦点及配装、键盘、后备滚动、模态检查。`.godot/ui-symbols-offers.log` 通过开局、减弱动效和商品／招募检查；最终角部布局由 `.godot/ui-symbols-offers-final.log` 复查，包含 900 宽候选可访问性。`.godot/ui-symbols-health-contract.log` 验证有效生命上限仍正确。本轮查看整备空槽／穿戴／后备、心形提示及招募正常／窄布局截图；未改变人物取景、战斗或交易规则。整体审美目标未完成，图标识别体验仍需用户实际感受。
