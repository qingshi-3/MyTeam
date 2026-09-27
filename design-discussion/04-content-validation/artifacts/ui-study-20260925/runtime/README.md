# 真实 UI 接入与核对

## 当前：C 英雄检视台已接入

随后按用户要求统一移除点击后的黄色焦点描边；主题更新后的 [预设输入框实机检查](preset-input-no-focus-border.png) 使用正式弹窗与隔离输入，未绑定存储，列表为空。文字输入、Tab、按钮点击、空格激活、Escape 及返回焦点通过，日志 `.godot/ui-focus-review.log`。下方检视台图片记录布局恢复时的界面，未因这次主题小改重跑整套截图。

最新拖动：[英雄拿起／移动／交换／取消](hero-drag-motion.gif) · [装备穿戴／替换／转交／卸下](equipment-drag-motion.gif)。正式组件、内存征程、真实 GUI 输入逐帧采样；按采样时间播放，段间增加 650ms 审阅停顿，长截图保存间隔压到最多 500ms。预览来自同一视口的动态组件，不再是静态复制控件。支持来源淡影、抬起投影、速度倾摆、目标符号、成功落下和取消返回；未增加发光。实验室另通过单位库／棋子／装备实输入检查。构建通过；具体行为、日志和已知体验边界归活动任务，用户手感未验收。

弹窗动效：[弹窗与按钮动态预览](popup-motion.gif)。来自正式实验室预设组件在隔离视口的真实渲染，依次展示打开、关闭、入场中 Esc 打断、按钮按住与移出取消回弹；按实际采样时间播放，每段末尾增加 650ms 审阅停顿。预设未接保存仓库。ContextPopup 和军团共用此过渡，整备实输入另有 `.godot/ui-motion-roster.log` 通过证据。减弱动效、关闭期间防穿透、提示框 hover/focus 交接已检查；用户的实际手感评价尚待确认。检查桌面窗口限制为 960×540，内部隔离视口采样 1600×900。

最新布局纠偏：按用户重新提供的原稿恢复右侧标题／阶位、两个技能图槽、带图形分组的两栏属性与底部装备托盘；技能图暂时留白。正文使用常规宋体，数值集中对齐。出战／后备与阶位分别使用专用盾形、圆形卡角徽章。上一轮偏离稿的 [实机截图](before-detail-restore.png) 留作对照。左侧正圆头像、文字安全区和背景 HUD 隐去／恢复继续保留。

采用用户确认稿的左侧紧凑列表／中间大立绘／右侧一体技能属性装备。外层透明，截图背景来自实际游戏。以下均为 Godot 生产组件在隔离征程中的真实运行结果（1600×900）：

- [整备主页](workbench.png) · [已穿戴](workbench-equipped.png)
- [右侧完整说明，立绘保留](workbench-details.png) · [生命上限提示](workbench-health-hint.png)
- [后备选择与配装](workbench-reserve.png) · [全局军团共用入口](workbench-army.png)

卡面六项核心值＋暴击对，右侧 19 项完整可见；法强零值保留。扣除石环上缘及纸条后的可见立绘高度占卡片 60.9%。正式输入验证通过选择、只读查看、穿戴／替换／转交／卸下、换装时说明刷新、后备滚动、空技能槽点击、缩写属性完整说明、键盘与焦点返回、透明模态阻挡及军团总览切换。非零法强和两种暴击数值另用明确的演示数值夹具检查，未写生产定义。最新日志 `.godot/ui-workbench-reference-final.log`；构建 0 警告／0 错误。

四份专用素材由内置 imagegen 制作后接入原生节点，完整提示和生产路径见 [素材清单](../../../../../assets/ui/tavern/workbench-detail-polish-generation.json)。没有把设计稿整图当作运行界面。

当前未做全分辨率／自然通关验证，整体成品审美仍待用户评判。下方是其他页面的现有运行结果和整备改版历史，不是当前整备布局。

## 此前运行证据

2026-09-25。用户认可符号化方向，但认为仍然只是铺陈、没有设计感。本版重做整备／招募卡的构图：攻击与生命成为姓名纸条两端的手绘数值标记，阶位与状态／职责退到上角，三项次要属性低于主数值，主被动采用不同字号并将图形与名称作为整体居中。三装备位集中到底部，以很浅的轮廓提示投放位置。保留认可的石框、石环与原角色立绘；不是逐字段加厚框。以下为 Godot 生产场景在隔离测试状态下的实际渲染，默认 1600×900，非整页生成图。

每战满血规则保持：正式爬塔战斗按有效生命上限入场；休息保留原金币选项，事件失败无收益且不扣血。

- [英雄与装备](equipment.png)：大立绘、纸条两端的攻击／生命标记、上角阶位／状态、次要属性、主次技能和集中装备位。
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

扣除石环上缘和名称纸条后，可见开窗图高／整卡高度：招募 **60.0%**、整备 **63.0%**；开局候选扣掉环的上下缘为 **64.1%**。这是高度比，不是人物像素面积比；右侧完整阅读面板不采用大图占比。本轮构图修改前：[整备](before-composition/equipment.png) · [招募](before-composition/recruitment.png)。符号化修改前：[整备](before-symbols/equipment.png) · [招募](before-symbols/recruitment.png)。每战满血修改前：[整备](before-full-health/equipment.png) · [详情](before-full-health/equipment-details.png)。此前被否定的重框版：[整备](before-cohesion/equipment.png) · [招募](before-cohesion/recruitment.png)。增加石环之前的更早版：[整备](before-ring/equipment.png) · [招募](before-ring/recruitment.png) · [开局](before-ring/opening-selection.png)。更早的灰底大图和小肖像版分别保留在 `before-cutout/`、`before-large-art/`。

输入检查覆盖点击、键盘展开／返回、拖放、取消、详情与后备滚动、模态焦点和减弱动效设置，构建通过；并非全局通关或整套审美验收。当前 `--isolated-pointer` 用独立 SubViewport 渲染并处理真实 GUI 事件，不移动用户桌面鼠标，也不把这当作操作系统窗口焦点验收。执行状态与后续差距以 [活动任务](../../../../../work-items/active/ui-material-overhaul.md) 为准。

继续实施：招募／商品卡已接入独立名称纸条，商店／战利品改为紧凑居中陈列，卡内滚动轨道加宽。石框的侧边、底边、配色与已认可版保持一致。名称纸条使用内置 imagegen，生产素材与提示保存在 `assets/ui/tavern/reference-name-ribbon.png` 和 `name-ribbon-generation-manifest.json`。

立绘取景：35 份英雄绑定使用 28 张透明衍生图，其余 3 名暂无对应图。大小展示使用同一透明源与分别配置的焦点／缩放；大图按卡窗与纸条裁切，小图按椭圆窗口近景取景。原 PNG 保留，战斗继续像素动画。衍生图由内置 imagegen 编辑，有局部线条／尺寸漂移，不承诺逐像素提取；完整提示与来源见 [generation.json](../../../../../assets/portraits/illustrations/cutouts/generation.json)。

取景检查夹具：七页真实卡片框内构图 [1](card-art-1.png)、[2](card-art-2.png)、[3](card-art-3.png)、[4](card-art-4.png)、[5](card-art-5.png)、[6](card-art-6.png)、[7](card-art-7.png)，为本次去框之前的环内取景证据，卡内文字／信息为占位，不代表当前信息区布局；此前三页原图／透明小肖像对照 [1](portrait-crops-1.png)、[2](portrait-crops-2.png)、[3](portrait-crops-3.png) 保留。这些仅证明取景，不代表完整运行状态；上方整备、招募等页面才是实际内容布局。

前轮满血规则验证：低并发构建 0 警告／0 错误；满血规则回归覆盖生命上限加成、旧伤势存档、阵亡再部署、下一战、实验室受伤起点、两种生命上限视图、休息单次领取／保存失败回滚、默认事件成功／失败与旧待决项。日志为 `.godot/run-full-health-final.log`。该服务检查使用明确构造的结算结果，未声称自然战斗获胜。

前轮真实 GUI 输入检查通过招募／offer 查看与确认、900 宽布局、开局选择／取消、整备穿戴／替换／转交／卸下、图标槽拖动预览名称、详情／键盘返回、后备滚动和模态焦点；休息／事件实点击确认验证金币结算与生命不变。日志为 `.godot/ui-full-health-roster.log`、`.godot/ui-full-health-offers-final.log`。本次未改人物取景／绑定，未重复全部 28 张取景测试，也未运行整局平衡测试。前版小控件提示保留在 [card-controls-generation.json](../../../../../assets/ui/tavern/card-controls-generation.json)，技能牌和身份牌已退出运行主题。缺图英雄、机会图标、其他物品底色、跨页面一致性和完整动态对照仍需继续，整体目标未完成，新结果尚未取得用户审美认可。

最新符号 UI 验证：构建 0 警告／0 错误；`.godot/ui-symbols-roster.log` 通过心形／状态图形悬停解释、生命点击详情／返回焦点及配装、键盘、后备滚动、模态检查。`.godot/ui-symbols-offers.log` 通过开局、减弱动效和商品／招募检查；最终角部布局由 `.godot/ui-symbols-offers-final.log` 复查，包含 900 宽候选可访问性。`.godot/ui-symbols-health-contract.log` 验证有效生命上限仍正确。本轮查看整备空槽／穿戴／后备、心形提示及招募正常／窄布局截图；未改变人物取景、战斗或交易规则。整体审美目标未完成，图标识别体验仍需用户实际感受。

本轮构图验证：低并发构建 0 警告／0 错误；`.godot/ui-composition-roster-final.log` 通过新攻击标记悬停、生命查看、详情焦点返回、配装拖放／替换／转交／卸下和后备操作；`.godot/ui-composition-offers-final.log` 通过生命标记查看不招募、技能展开／返回、明确招募／购买及 900 宽布局。首轮招募图高降至 59.3%，已缩小次要属性字号并恢复至至少 60%，未降低检查门槛。已查看正常、穿戴、窄布局实际渲染；没有宣称整体审美验收或完整通关。

数值标记通过内置 image_gen 制作：[攻击](../../../../../assets/ui/tavern/card-attack-stat.png)、[生命](../../../../../assets/ui/tavern/card-health-stat.png)，参考已认可石框的灰棕倒角与柔和面；运行文字由控件绘制。完整提示与源文件见 [card-stat-generation.json](../../../../../assets/ui/tavern/card-stat-generation.json)。原 PNG 保留透明 alpha，未用截图替代界面。技能与装备位的排版和反馈由 authored 场景／Theme 实现。
