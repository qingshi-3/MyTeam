# 免费成品特效筛选预览

2026-09-25新增制作结果入口：`crafted.html`，对应EE07喷火、HC38蓄怒重拳、EE03冲锋。3项已接入正式共享资源，单项放大视频/同视角新旧总览/正式战斗片段均为Godot实录，素材挑选页仍保留源样例以供参考。当前结果不是网页另写一套特效。视频来自`.godot/three-actions-review/`，主体以30 fps编码，正式片段15 fps；没有音轨。详见项目VFX活动任务。


2026-09-25：用户同意先拿免费样例对比斩击、治疗、爆发，并在查看素材时明确反馈质量比此前自制效果好。本页是本地选材工具，不是正式技能播放器或游戏接入，不改变现有共享特效系统。

启动：在项目根目录运行 `node web/vfx-sample-viewer/server.mjs`，打开 <http://127.0.0.1:5187>。服务只监听本机；免费样例播放器无外部依赖请求；匹配表的作者GIF在点击后从itch.io加载。`.gdignore`阻止Godot导入研究素材。既有后台服务本轮保持运行。

## 当前候选

共9项：Free Archives的Slash／Cure／Burst三项PNG；EVFX Slash的Twin Edge／Quick Blade／Eruption Claw；EVFX Medic的Medic Emblem／Diagnostic Eye；EVFX Blazeforge的Blaze Conjuration。

- 优先考虑斩击包的短斩与快刃；需要区别目标命中与施放者中心横扫。
- Cure可作低接入成本的目标治疗基线。Medic的两个免费样例采用注射器／扫描盘，暂不推荐直接用于现有奇幻医师。
- Burst是聚能后爆散的魔法效果，Blaze Conjuration是火焰圆阵；不称为通用烟火爆炸。
- 下载的EVFX Blast免费包是光束／导弹／无人机，未纳入这三类。
- 首轮Searing Breath因烟火覆盖撤出通用清单；后续专门动作页调整取景后可见火流及暗层，仍未核实暗层的源效果/混合原因，不据此判断素材质量或认定已适配。

## 原件与授权

来源、下载日期、文件哈希见`assets/provenance.json`，条款保留在各包目录的原始Manual中。素材均来自作者公开免费样例，允许商用／修改、要求署名，不能脱离项目单独再分发；本页未采购、上传或推送。`assets/runtime`为Effekseer官方WebGL运行时，许可证同目录。

未执行或纳入RPG Maker演示程序、其引擎和地图资产。PNG播放的cell编排来自作者演示数据库，保留逐帧透明度、叠加、screen及旋转；仅省略目标闪白与音效。Cure手册、图集格数和演示帧数不一致，当前依演示数据库60帧／60fps播放，不擅自补帧。

`prepare.py`可从本项目`.godot/vfx-market-study`及`.godot/hero-vfx-research`下载缓存重建选材文件，并生成哈希清单。成品预览文件已单独保存，运行预览不依赖该缓存。角色参照是项目内`f3_aymarahealer`第一张idle帧的只读裁切，不修改原件。

## 播放与验证边界

粒子时长由固定随机种子、60Hz逐帧运行至实例结束得到，最大保护上限30秒；循环额外留0.9秒空档。尺寸通过相机缩放控制，不改变粒子节点的缩放继承。源演示的个别旋转保留，但相机是本地筛选用正交视角，不等于RPG Maker原相机或项目战场投影。

已实际操作浏览器选择样例、暂停、进度拖动、灰绿／浅底切换、尺寸切换，并查看关键阶段渲染。原速循环已运行，不能将离散截图核对称为完整连续视频观感验收；最终主观选材由用户判断。页面读取系统reduced-motion时默认暂停；本轮没有另行模拟该系统偏好。未构建／启动Godot、替换技能、写存档、提交或推送。

## 现有英雄匹配（2026-09-25补充）

入口：<http://127.0.0.1:5187/matches.html>，可按英雄/技能搜索、按筛选状态过滤，默认31名正式英雄，可显示7名保留英雄。名单由当前目录及实际技能定义核对；`matches.json`是本轮选材快照，不是玩法权威。敌人炉喉蜥/破阵巨兽另列在优先卡片。

动作页：<http://127.0.0.1:5187/?set=actions>，新增8项原样例/对照（与原页共享Blaze Conjuration，共16个不同的本地样例）：Searing Breath、Blaze Conjuration；Thrusting Stroke、Qi Blast、One Two Strike、Orbital Smash；Lifting Gust、Stormforge Aura。原件均不修改，取景缩放不表示游戏尺寸。

- 已实际查看Searing Breath定向火流及覆盖暗层；Thrusting Stroke起段亮条到中心接触爆点；Qi Blast亮核到球形气团；Lifting Gust绕中心上扬。此为离散阶段观察，不能冒充完整作者视频或技术拆解。
- 喷火优先试火流本体，核对口部起点/70°范围/三段供给/中断；重拳优先试Strike释放/命中分层，不能将球形Qi Blast或双击整段套入单次宽拳劲。
- Dash作者公开GIF已查看烟尘与前侧气环画面；Air/Dash三份公开GIF已查看横向气环、地面径向气环、白色冲击体画面。仅按需引用远端，不复制付费包；两包页面价均US$1.90（2026-09-25），未购买。
- Sanctuary、Shoot、Frostforge、Shadeforge、Mindbender等更多免费文件在研究缓存，表中明确为待看动作；毒云、时停、吞食、飞钩等未找到合适成品的项保留缺口，不强行按元素色套用。
- Strike官方免费包误放了署名Slash的Manual，原文完整保留；正确署名是`EVFX Strike © Dreams Circle`，以Strike作者商品页条款为依据，已在provenance.json注明。
- 本轮更新后资源清单含135文件/17,922,011字节。仅播放库/必要素材纳入本地预览，不复制RPG Maker演示程序。

本轮页面验证：实际点击匹配页与动作页导航、搜索HC38、切换保留HC31和状态筛选、键盘Space切换复选框；公开GIF点击播放/停止及画面已检查。动作页选择/暂停/拖动后的渲染已查看。两份JS语法检查通过，8个动作资源与页面文件可读取、135个原件哈希一致。没有完整动态观感/战场验收，不重复引擎测试。


2026-09-25细化：`crafted.html`默认本轮`polish-*`实录；版本选择器保留用户认可方向的`iteration1-*`。修订为喷火无硬线聚热前摇、重拳更慢的展开/消散及次层收敛。下方正式战斗视频为上一版，页面已显式标记。当前原始帧在`.godot/three-actions-polish/`，均30 fps编码，无音轨。运行资源与游戏共享。


2026-09-25角色动作同步：当前默认版本为 `animation`，重拳／冲锋／总览读取 `animation-punch/rush/final.mp4`，喷火继续使用 `polish-fire.mp4`。`polish`明确为动作调整前，`iteration1`为初版；全部旧录制保留。新版角色使用正式定时动作和位移姿态路径，详见共享VFX活动任务及S52。下方正式实录仅HC38更新为本轮，另两项标明此前版本。当前离屏录制来源 `.godot/three-actions-animation/`。


2026-09-25后续六项：`refined.html` / `refined.js`，返刃、飞钩、地裂、岩垒、换位、酸液。`refined/before-*.mp4`和`after-*.mp4`为同尺寸同视角30fps近景；`battle-*.mp4`为本轮15fps正式实录，岩垒与地裂共用EB01。支持新旧／正式切换、暂停／重播／倍速和减少动态下默认不自动播放。前一组三项页面仅添加入口，旧视频保持。原帧、验证、授权与素材依据归共享VFX活动任务及P01-S53。启动仍用`node web/vfx-sample-viewer/server.mjs`，仅本机5187端口，无外部部署。
