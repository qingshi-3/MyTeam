# 整局与首版内容规划：依据

## Q25：炉石／大巴扎UI视觉参考的实际观察（2026-09-25）

承接D53，仅研究UI、图标与制作能力。回读现有语义图标任务的目录复用、信息层级和非颜色表达约束，并阅读imagegen、Godot UI／动效技能。没有按技能示例引入全局音效架构、改变布局或采用未经验证的技术细节。

### 已实际查看

1. [大巴扎官方Steam页](https://store.steampowered.com/app/1617400/The_Bazaar/?l=english)提取四张1920×1080截图并逐张查看：遭遇选择、商品选择、技能徽章与暴击词条提示、升级事件。具体素材hash依次为`3582d761ebef9a077a850170cf2d9431fae366e0`、`75a8de6841e7a260c7d4d6a451672953434361e8`、`268bd0fdc5fb3150434fb9f4e2264ac1be2f4c8b`、`4e81c57eb94c29ab29c17bfd7bc887d4bfb7e813`。可见布面／金属槽、钟盘、实物背包／金钱入口、局部蓝光、等级事件金线；技能圆徽章和内文小型暴击符号是不同尺度，不能混成一个资产用途。
2. [炉石官方主页](https://hearthstone.blizzard.com/en-us/)的两张酒馆战棋图片：[发现／选择界面](https://blz-contentstack-images.akamaized.net/v3/assets/bltc965041283bac56c/blt9665fc19c41433af/651c766e73d19153c5b7f940/midgameshop.PNG)、[英雄选择](https://blz-contentstack-images.akamaized.net/v3/assets/bltc965041283bac56c/bltac63871526480863/651c7752b456564684fc3477/playpickhero.PNG)。已实际查看木盘凹凸、厚卡框、纸卷标题、背景压暗与选中绿光／确认蓝光的层级；不能由此推断两游戏所有颜色的完整语义规范。
3. 大巴扎商店页附带[Magic Mirror片段](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/1617400/extras/e9bc1ca3a1a1732f47fe2b74f16c0bae.mp4)，本文件5.20秒、600×338、10fps，已按每秒两帧的序列采样观察。画面包含镜面展开、上排物品出现、hover提示、物品浮起与升级闪光。可说明动作阶段，低帧率宣传文件不能证明实机流畅度，不反推具体shader、引擎或制作方式。
4. 炉石官方[选人至招募片段](https://blz-contentstack-assets.akamaized.net/v3/assets/bltc965041283bac56c/blt0da2c0d1ea97bc85/651c775e9661933a6f13179c/Hearth_HomepageRedesign_BGs-PlayPickHero_HRZ.mp4)，本文件22.65秒、1920×1080、60fps、素材元数据为2023-10-03。已按每两秒一帧采样前18秒，观察头像转移／排布、木盘显现及招募横幅。文件60fps只是编码信息，不表示Agent已以原速观看、试听或验收交互手感。

研究缓存与观察序列在`C:/Users/qs/.codex/tmp/ui-reference-20260925/`，只作参考，不是项目资产或运行依赖。上面的链接是来源入口；未将商业游戏截图导入游戏、生成替代资产或制作原型。

### 限制与失败范围

浏览器创建参考页再次超时，改用公开网页／媒体读取与本地图片查看；没有进行原作真实点击／拖动。HowBazaar首页可读但keywords入口404，BazaarDB入口403；尚未逐枚检查完整词条图标库，优化建议不能冒充对全库缺陷的审计。炉石press页已读目录，未下载大体积press包。团队人员关系未核实，不沿用户说法断言是同一支制作团队。本轮尚无生图成品，能力评价是制作方法和可行性判断，不报告达到某个百分比或原作整体质量。

## Q24：大巴扎内容与轻量背包参考（2026-09-25）

范围由用户限定为内容与轻量背包，不讨论异步PvP或采用原作整局模式。本轮以`bazaar|大巴扎`检索本地研究库与讨论区，没有命中既有专门记录；回读M05三槽／重复／固定品质边界、G03-Q03趋同分析及相关跨题依赖。未修改研究原库，未做全游戏机制穷举。

- **官方概述**：[Steam商店](https://store.steampowered.com/app/1617400/The_Bazaar/?l=english)。读取正文，支持各英雄有不同玩法与物品池、把工具／武器安排在板上、单强物品与组合均为设计空间。宣传中的组合数量与“每局不同”不当作多样性或平衡验证，不凭商店简介补全规则。
- **历史机制资料**：[tatiana，Beginners’ guide to The Bazaar](https://steamcommunity.com/sharedfiles/filedetails/?id=3573365196)，页面标注2025-09-23发布、2025-11-09更新。读取Keywords、Item Tips、Strategy与Event Tips，支持Charge／Haste／Ammo、暂存与战斗板分开、大小占位、位置／数量条件、部分出售强化／暂存产出／任务推进及持物事件。该页同时显示移除／不兼容提示；按历史玩家二手资料保留，未据此认定现行规则。其强弱推荐、胜率判断、英雄关键词覆盖、精确容量、升级及日程数值均不作当前平衡事实。
- **未覆盖**：浏览器打开`thebazaar.com`超时；`playthebazaar.com`仅取到加载页；Bazaar wiki与Mobalytics请求403；另一指南入口只返回重定向。Steam appdetails没有取得可用描述，改读商店正文。未看到实机画面、核验当前补丁物品文本或验证具体连锁配方。

Q24的BZ01–BZ07据上述机制分类；所有英雄队伍适配与示例均为Agent提案。仅可据现有证据判断“值得继续参考”，不能声称已完成该游戏内容调研、确认轻量背包方案或已经解决本项目重复构筑问题。

## Q22：英雄主动、被动与定位的组合关系（2026-09-19）

承接2.3-D50。复用既有BC01–BC37的跨游戏覆盖与来源映射；本轮在原研究库`web/game-mechanics-atlas/research/deep/game-dossiers/`用`passive|被动|反击|cleave|multicast`查找相关档案，定向回读Astronarch、The Last Flame与Dota Underlords的能力／构筑段落。没有重做全部深证据检索或修改研究原库。Astronarch的固定一主动一被动与自动战斗、TLF的盾转暴击／队伍回蓝等继续提供结构参考，不自动迁移可配置能力与跨战成长。

新增直接来源：Riot官方Lucian页及Data Dragon **16.18.1**英雄JSON，该版本号来自本轮页面资源地址，不宣称最新版本；Dota 2官方`datafeed/herodata`的`desc_loc`与`notes_loc`，为本日可读文字快照，无固定补丁号。没有实机演示观察、数值平衡或完整命中例外核验。HP编号对应当前草稿和对话，表示比较项，不是正式英雄编号。

| 编号 | 原作／直接来源 | 核实事实与限制 |
| --- | --- | --- |
| HP01 | [Kai'Sa](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Kaisa.json) | 普攻施加Plasma，友军定身帮助叠层；W施加印记，进化后更多。简述未给五层爆破公式；项目五层比例爆破按用户示例，比例基数未定。 |
| HP02 | [Master Yi](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/MasterYi.json) | 连续若干普攻后双击，普攻减少Q冷却。简述不能证明Q等于两次完整普攻；该点按用户项目改编构思记录。 |
| HP03 | [Vayne](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Vayne.json) | W第三次连续攻击／合格技能对同目标造成最大生命比例真伤；Q翻滚强化下一击。Q位移本身不由本描述产生一层。 |
| HP04 | [Ursa，70](https://www.dota2.com/datafeed/herodata?language=english&hero_id=70) | Fury Swipes让同目标后续攻击更痛，中断一段时间失去加成；Overpower加快有限次攻击。区别于定层消费爆破。 |
| HP05 | [Lucian](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Lucian.json) | 施法后下一攻双射，双射减少E冷却；另有友军治疗／护盾等附伤，不必全部抽取。 |
| HP06 | [Phantom Assassin，44](https://www.dota2.com/datafeed/herodata?language=english&hero_id=44) | 当前Coup de Grace是攻击有机会取得Deadly Focus、后续攻击消费并必暴击；Phantom Strike传送到敌人时获攻速。历史“每击直接掷暴击”不能冒充本日精确规则。 |
| HP07 | [Sven，18](https://www.dota2.com/datafeed/herodata?language=english&hero_id=18) | Great Cleave让攻击劈伤周围；God's Strength阶段性加攻击伤害／抗减速。数值增益可经已有攻击覆盖放大。 |
| HP08 | [Monkey King，114](https://www.dota2.com/datafeed/herodata?language=english&hero_id=114) | Jingu同英雄第四次命中后给有限次强化吸血攻击；notes明确Boundless Strike可叠计数，主动为按攻击暴击的直线打击。如何逐目标读取／消费强化和完整命中特效仍需专项核验。 |
| HP09 | [Darius](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Darius.json) | 攻击／伤害技能叠流血至五层，自身进入高攻击状态；R随目标流血层数增伤，击杀后短时可再施放。R不写成消费流血。 |
| HP10 | [Axe，2](https://www.dota2.com/datafeed/herodata?language=english&hero_id=2) | Call使周围敌人攻击自己并加护甲；Counter Helix按攻击计数反击周围，notes说明命中时触发。抽取受击→反击关系，不照搬纯粹伤害与全部升级。 |
| HP11 | [Legion Commander，104](https://www.dota2.com/datafeed/herodata?language=english&hero_id=104) | 当前Moment of Courage为被攻击若干次后反击目标并吸血；Duel强制互攻并限制行动。原作胜者永久攻击不移植。 |
| HP12 | [Huskar，59](https://www.dota2.com/datafeed/herodata?language=english&hero_id=59) | 缺失生命带来攻速、魔抗和恢复；Life Break耗自身生命跃击并减速。原作无蓝等其他能力不随之移植。 |
| HP13 | [Sett](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Sett.json) | W被动储存承伤为Grit，主动消费为盾与区域拳击，中线真伤／侧面物理。可拆成项目主被动，不必叠上本体P左右拳。 |
| HP14 | [Necrophos，36](https://www.dota2.com/datafeed/herodata?language=english&hero_id=36) | Heartstopper持续侵蚀附近生命；Death Pulse伤敌治疗友方；R按缺失生命伤害。分别支持消耗→续航或消耗→斩杀，不继承永久成长。 |
| HP15 | [Braum](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Braum.json) | 自己先挂首层，友军普攻后续叠至四层晕，目标随后暂不能重叠；Q挂层减速。不是全队无条件首挂。 |
| HP16 | [Leona](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Leona.json) | 伤害技能施加短暂Sunlight，友方英雄伤害消费为额外魔法伤害；Q强化普攻眩晕。和多人累计四层不同。 |
| HP17 | [Crystal Maiden，5](https://www.dota2.com/datafeed/herodata?language=english&hero_id=5) | 回蓝光环，近处更多；Nova范围伤害减速，Frostbite限制移动／攻击。队伍供能与控制可共同服务辅助身份。 |
| HP18 | [Ogre Magi，84](https://www.dota2.com/datafeed/herodata?language=english&hero_id=84) | Multicast让合格技能／物品多次施放；Fireblast重复同一目标，Bloodlust扩到附近其他友军。多重施法并非全部等于同目标伤害翻倍。 |
| HP19 | [Zyra](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Zyra.json) | 周期生种子，Q／E使附近种子长成攻击植物；还存在主动种子和不同植物，项目可只抽一种。 |
| HP20 | [Illaoi](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Illaoi.json) | 被动沿附近不可通行地形生触手，W跃击并让附近触手拍击。已有对象响应指令，区别于植物激活后自主攻击。 |
| HP21 | [Blitzcrank](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Blitzcrank.json) | Q沿路径抓人拉回，P低血时按法力给盾。保命与抓人可以服务同一定位，不必直接互相触发。 |
| HP22 | [Galio](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Galio.json) | 周期范围强化普攻；E后撤再冲拳，击飞首名敌方英雄。可抽成突入控制＋前线补伤。 |
| HP23 | [Karthus](https://ddragon.leagueoflegends.com/cdn/16.18.1/data/en_US/champion/Karthus.json) | 死后灵体仍可短时施法；Q延迟爆炸，对孤立目标增伤。不等于复活、占地或无限推迟终局。 |

本轮还读取Dota Lifestealer（54）、Undying（85）、Centaur（96）、Abaddon（102）及LoL Malphite，取得比例攻击恢复、墓碑僵尸、反伤冲锋、护盾诅咒、护盾突入等候选；未完整展开其整套关系，不记成已设计英雄。清单不宣称穷尽。

MOBA主动依赖手动择时；项目需另定自动目标／时机。翻滚不是随机挪动，满蓝不保证恰有强化，斩杀刷新不默认跳过法力，额外命中不默认完整普攻／回蓝／递归触发。具体数值及资格均未被用户采用；只有文字机制研究，无实现／动态表现／体验验证。

## Q21：跨游戏的位移、拦截与战场关系机制（2026-09-19）

本轮实际读取《英雄联盟》官方英雄页的技能说明、Dota 2官方英雄数据接口、Overwatch官方英雄页的基础Abilities区；后者的Perks／Stadium不混入基础技能。采用官方角色名／技能名定向查阅，以下是文字机制证据，不是动态观察、最新逐项碰撞例外表或制作拆解。没有以视频地址存在当作已观看视频；Q21表现均标为项目动作目标，实施前须补实际演示与分层取证。

### 官方机制来源

| 编号 | 角色／官方来源 | 本轮核实内容与关键差异 |
| --- | --- | --- |
| EM01 | [LoL Blitzcrank](https://www.leagueoflegends.com/en-us/champions/blitzcrank/) | Rocket Grab：沿路径发射手臂，将抓到的对手拉回。原文“grab an opponent on its path…dragging it back”。项目的最远索敌、锁向预告、伤害和回拖碰撞为另行适配。 |
| EM02 | [LoL Thresh](https://www.leagueoflegends.com/en-us/champions/thresh/) | Death Sentence拉近目标，二次激活可把自身拉向目标；Flay按挥动方向推移敌人。钩别人过来和自己钩过去有不同接敌结果。Dark Passage原作需要同伴点击，不直接用于本项目自动救援。 |
| EM03 | [LoL Orianna](https://www.leagueoflegends.com/en-us/champions/orianna/) | R短延迟后把附近敌人拉向球；Q把球留在远处；E让球附着友军。支持远端一次聚拢与由同伴携带施法中心两种构思。 |
| EM04 | [Dota 2 Dark Seer](https://www.dota2.com/hero/darkseer) | 官方[datafeed](https://www.dota2.com/datafeed/herodata?language=english&hero_id=55)：Vacuum拉拢区域内敌人；Ion Shell让目标周围产生伤害；Wall of Replica对穿过的敌方英雄产生己方控制的复制体，注明不从幻象再复制、同墙每玩家最多一个。不能把复制墙当作实体阻路墙。 |
| EM05 | [Dota 2 Enigma](https://www.dota2.com/hero/enigma) | 官方[datafeed](https://www.dota2.com/datafeed/herodata?language=english&hero_id=33)：Black Hole是持续施法，吸入者无法移动、攻击或施法。它比一次位置聚拢多了持续行动剥夺；项目不默认整套移植。 |
| EM06 | [Overwatch Zarya](https://overwatch.blizzard.com/en-us/heroes/zarya/) | Graviton Surge发射吸引敌人的引力井；与原地一次拉拢相比，存在投送与持续区域。当前官方简述没有完整碰撞／位移豁免表。 |
| EM07 | [LoL Braum](https://www.leagueoflegends.com/en-us/champions/braum/) | E朝一个方向举盾，使弹道命中自身并销毁；首击完全抵消、后续该方向攻击减伤。W跳向友军。项目可以只借“方向接弹”而不继承全部免伤数值。 |
| EM08 | [LoL Yasuo](https://www.leagueoflegends.com/en-us/champions/yasuo/) | Wind Wall制造阻挡敌方弹道的风墙；不是替身后队友承担伤害。官方概述不能证明所有命名技能的例外资格。 |
| EM09 | [Overwatch D.Va](https://overwatch.blizzard.com/en-us/heroes/dva/) | Defense Matrix阻挡自身前方区域弹道；Eject在机甲被摧毁时弹出。前方拦截区与薄墙的覆盖几何不同，脱壳也可改变作战阶段。 |
| EM10 | [Overwatch Genji](https://overwatch.blizzard.com/en-us/heroes/genji/) | Deflect将迎面弹道反向发向瞄准方向，同时可挡近战。项目只考虑有限弹道反射；不默认近战免伤或无限反射循环。 |
| EM11 | [Overwatch Sigma](https://overwatch.blizzard.com/en-us/heroes/sigma/) | Kinetic Grasp吸收前方弹道转为额外生命；Experimental Barrier可向前投送并停驻／收回；Gravitic Flux抬起敌人后砸落。吸收转资源和直接消除不是相同反馈循环。 |
| EM12 | [LoL Anivia](https://www.leagueoflegends.com/en-us/champions/anivia/) | W暂时制造无法通行的冰墙；被动致死后化卵重生。墙影响路径，卵产生可攻击的阶段目标，不止是静态增防或多一管血。 |
| EM13 | [Overwatch Mei](https://overwatch.blizzard.com/en-us/heroes/mei/) | Ice Wall在前方造墙；官方简述不足以给出本项目墙段耐久、破坏规则和寻路实现，均另作适配。 |
| EM14 | [Dota 2 Disruptor](https://www.dota2.com/hero/disruptor) | 官方[datafeed](https://www.dota2.com/datafeed/herodata?language=english&hero_id=87)：Glimpse送目标回先前位置；Kinetic Field延迟形成敌人不能穿越的环形边界；另有Kinetic Fence墙形版本。回溯位置不代表回溯血量／冷却，环边限制也不等于内部全员眩晕。 |
| EM15 | [LoL Poppy](https://www.leagueoflegends.com/en-us/champions/poppy/) | E推退目标，撞墙附眩晕；W阻止周围敌人突进。空间条件奖励与普通击退不同；反突进不等于反所有位移。 |
| EM16 | [LoL Gnar](https://www.leagueoflegends.com/en-us/champions/gnar/) | 怒满后下次技能变大，获得另一套技能；大型R定向抛掷周围敌人，撞墙有额外结果。可参考体型／动作阶段变化。 |
| EM17 | [LoL Singed](https://www.leagueoflegends.com/en-us/champions/singed/) | Fling把近身目标抛到自身背后；Poison Trail随行留下毒迹。前者破坏前排保护关系，后者把行进路径变成短期危险空间。 |
| EM18 | [Dota 2 Vengeful Spirit](https://www.dota2.com/hero/vengefulspirit) | 官方[datafeed](https://www.dota2.com/datafeed/herodata?language=english&hero_id=20)：Nether Swap与友方或敌方英雄交换位置。换位使两端同时暴露，和单向抓取有代价差异。 |
| EM19 | [LoL Tahm Kench](https://www.leagueoflegends.com/en-us/champions/tahmkench/) | Devour可吞英雄数秒，敌人受伤、友军获盾。可分别借为短暂移除敌手和保护同伴；原作释放落点／被吞单位状态详细规则未由此简述确定。 |
| EM20 | [LoL Illaoi](https://www.leagueoflegends.com/en-us/champions/illaoi/) | E抽出灵魂，灵魂承伤的一部分传回本体；不是仅给本体挂持续伤害标记。支持伤害代理体和可见连线关系。 |
| EM21 | [LoL Shaco](https://www.leagueoflegends.com/en-us/champions/shaco/) | W设置会触发的盒子；R制造能攻击、死亡爆炸并生成小盒的分身。项目可单取分身分流攻击或可见伏击物，不默认复刻隐形、恐惧、爆炸、再召唤全套。 |
| EM22 | [LoL Zyra](https://www.leagueoflegends.com/en-us/champions/zyra/) | 种子经技能触发长成作战植物；R会强化范围内植物。适合可见前置对象、阵地成长与有限召唤的阶段变化。 |
| EM23 | [LoL Azir](https://www.leagueoflegends.com/en-us/champions/azir/) | 士兵代为攻击、可移动士兵，R士兵墙前推击退敌人。可参考移动战线；不凭本页概述推定旧版本所有阻挡／突进规则。 |

### Q23补充：直接伤害也可以有形状与节奏差异（2026-09-19）

本轮另外实际读取以下两页官方技能文字，用于五名敌人设计；没有观看技能视频或制作VFX。其余沿用EM12／13临时墙、EM18换位、EM09／16换形、EM22前置对象成长的已读依据。以下敌人的外观、锁向、攻击脉冲、阶段和周期均为项目提案，不是官方描述。

| 编号 | 官方来源 | 本轮核实内容 |
| --- | --- | --- |
| EM24 | [LoL Rumble](https://www.leagueoflegends.com/en-us/champions/rumble/) | Flamespitter官方描述为向前方锥形区域喷火、持续3秒并造成魔法伤害。本项目炉喉蜥卫只参考前方持续吐息，不采用兰博整套热量／危险温度增伤。 |
| EM25 | [LoL Sivir](https://www.leagueoflegends.com/en-us/champions/sivir/) | Boomerang Blade官方描述为投出回旋刃，在去程和回程均造成伤害。本项目返刃投手的固定起终点、每程独立命中记录及停攻接刀是项目适配，不能归为已核实的原作完整回程规则。 |

### 覆盖限制与项目检查

- 取证方式是直接官方角色／技能名查阅，非全网穷举。HotS Stitches／Leoric官方旧路径返回404，Diablo III怪物指南只返回门户文本，未把它们当作已核实证据；LoL Tahm Kench首个带连字符路径失败，改用上表实际可读官方页。没有为了凑游戏数量引入未验证机制。
- 原文快照在本机`.godot/enemy-mechanism-research/`，讨论可恢复的机制摘要与正式来源以上表为准。原作会更新，本文不照搬其伤害、时长、升级或全部例外。
- 静态核对现有`DisplacementAbilityOperationSpec`已有Pull／Gather及真实位移路径，EE03大身体基础已完成；这不等于飞行钩头命中回拖、远端引力核或吞吐状态可直接配置。`BattleProjectiles`现有权威碰撞循环处理单位／地形；本轮未发现其中有独立风墙、盾面拦截或反射处理，后续需共享弹道规则而非表现层删除图片。没有运行引擎、改技能或验证新候选。

## Q18：特色兵纸稿的自动战斗边界与复用依据

2026-09-16，仅静态阅读现行玩法/系统契约、三个敌人场景、行为/效果目标/投射物与成长相关代码；未联网、运行引擎或验证新技能。Q18的秒数、倍率与首批数量均为Agent设计初值，无实测平衡依据。

D40/D41补充静态核对：`EffectDamageType`已区分Physical/Magical/True，`BattleSimulation.ApplyDamage`分别读取护甲/魔抗；这支持物理箭和魔法光束共用结算，未证明新发射/技能逻辑已接入。`UnitAbilityLoadoutComponent.Loadout`引用`AbilityLoadoutDefinition`，后者引用多个`AbilityDefinition`；现有`loadout_hero_hc01_crossbow.tres`外引`ability_hc01_volley.tres`，因此敌人可沿用单位/技能独立资源的既有方式。`AbilityDefinition`已有触发、周期/冷却、操作和表现引用，但不能据此断言本轮全部蓄力/超远参数已有所需字段。ES01旧3普攻触发已在D40后撤下，下面的早先能力核对保留为历史范围。

- `gameplay-design/tower-autobattler-core.md`保留英雄自动战斗/自动技能与独立战术指令的边界。纸稿可依靠部署、可取得的队伍能力和自动控制，不推定玩家能战中手动移动单个英雄或随时点名打断。
- 当前`enemy_crossbow.tscn`、`enemy_hexer.tscn`、`enemy_blood_reaver.tscn`没有独立技能组；这些名字/素材只能作为内容候选，不能报告本文行为已存在。基础弩手定义当前射程5、攻击间隔1.45秒、投射前摇0.5秒；不是Q18贯阵技能的已采用参数。
- `UnitBehaviorComponent.PiercingLine`和`BattleProjectiles`有普通攻击最多穿透2目标的现有路径，但不证明“每第4次攻击、蓄力开始锁方向、允许原目标死亡后沿旧线释放”的整套技能已支持。`BattleProjectileWindups`当前释放前复核目标存活/敌我及射程等条件，具体实施必须核对锁方向与取消语义，不能直接给所有普攻勾穿透冒充Q18。
- `EffectTargetResolver`已有以主体为锚点的阵营/距离/生命比例排序/数量筛选，项目已有护盾效果。Q18护阵术士的受伤资格、同类盾排除、固定到期、不刷新与施法周期仍须逐项核对；仅有ShieldEffectSpec不表示这些组合已可直接配置。
- `AttackHitGrowthComponent`的现有导出项为每命中攻速和换目标重置等，没有本文每3次普通攻击、最多3层的配置。项目可能还有其他可复用事件/状态机制，本轮没有据此认定一定要新增底层系统，也不把现有无上限成长直接用于特色兵。

设计先明确行为和强弱边界，技术实现再复用已核对的能力；实施缺口不反向限制本轮只写已有字段，也不宣称待设计行为已通过真实交互。素材最终绑定、空间射线表现和音效均未在本轮制作。

## Q17：单局节点、初始敌人数与路线算术

2026-09-16。本轮只读核查配置/生成代码，并用JavaScript枚举种子对6取余的路线选项、核对编组求和；未构建、启动引擎、模拟战斗或收集玩家路线数据。比例建议归[Q17内容稿](content-drafts.md)，没有声称是成熟游戏统一标准。

- `content/project/alpha_campaign.tres`引用3区域，`FloorsPerRegion=5`。`TowerNodeTableDefinition`默认`BossLocalFloor=4`、`RegularOptionCount=3`、`RotationStride=2`、`FloorSeedStride=7`；当前`tower_node_table.tres`没有覆盖这些默认值。
- `TowerGenerator.Options`按`(Seed + FloorIndex*7)%6`选起点，旋转表为Combat/Recruitment/Event/Elite/Shop/Rest，步长2。因此每次非首领选路给出Combat/Event/Shop或Recruitment/Elite/Rest（展示顺序可不同），恰有1个战斗选项。
- `RunRewardEconomyService.ConfirmOpening`在6选2确认后直接设置普通战待处理。每区末尾为首领，故全程有开局1战＋3首领，另有11个可选战斗节点；按可选路线上限/下限、假定都能完成计算，为4–15战。这不是任何阵容均可通关该路线的证明。
- `EncounterDefinition`默认随区域索引增加1人；9份遭遇资源分别配置普通基础4、精英基础6、首领基础3。`TowerGenerator.Encounter`先插入LeadEnemyId再补到count，故三区首领阵容为3/4/5总人数，而非1＋3/4/5。
- 仅作中性算术：假定每次三选一等概率，期望战斗数为`4+11/3≈7.67`；全程初始敌人数期望按种子奇偶分别为38.67/38，奇偶等权约38.33。避开全部可选战斗为16只；全部选择战斗为84或82只。实际玩家不会必然均匀选路，这些不能写成实际平均体验。
- Q17选每区各1普通/1精英/1首领作为9战样例，三区初始人数为`4+6+3=13`、`5+7+4=16`、`6+8+5=19`，合48。该组合对已枚举六种种子余数均有对应选项，另有6个功能节点。草案分类求和31/11/3/3；计全部敌人约64.58%/22.92%/6.25%/6.25%，去掉首领后约68.89%/24.44%/6.67%。

现行敌人池没有本轮四类编组规则；分类比例是重新设计的建议，非当前随机池实测。初始人数不含战中召唤/复生；只次数、独立种类、含机制的战斗占比与战斗时长占比需区分。材料培养仍未实现，不能用未来成长收益声称当前阶段平衡已成立。

## Q14：框架暂够后，战斗内容的实际缺口

2026-09-16，承接D34，只读检查现有资源/生成逻辑，未联网、构建、运行战斗或模拟平衡；玩家“瞎点通关”作为体验反馈，不宣称本轮独立复现。

- `content/enemies/` 中10种普通敌人的独立场景均只有基础行为/动画/生命等组件，未挂 `UnitAbilityLoadoutComponent`；锈甲守卫、咒术师等定义仍写“塔内战斗单位”。它们仍有角色、射程、属性或普攻相关参数差异，不能据此称毫无行为。3名Boss另有配置，部分独立场景和正式BossTimeline引用技能，不能把普通敌人的结论泛化成所有敌人都没技能。
- `src/Run/TowerGenerator.cs` 按地区和节点类型取一个遭遇定义，再从敌池逐个随机抽人。`EncounterDefinition.AddRegionIndexToCount` 默认true；当前普通配置BaseEnemyCount=4，精英=6，故三区普通为4/5/6，精英为6/7/8。相同地区普通/精英使用同一敌池；没有按本区楼层编排职责组合和特定配合。增加数量与换区不自动构成有意义的战术难度曲线。
- `pool_all_items.tres` 当前21条：6装备/15遗物。装备中的先锋徽甲、精工瞄具、蓄能护符、疾攻手套分别主要给护甲、攻击、回蓝、攻速；霜羽徽章有成员资格，霜痕战刃有命中叠层/冻结/羁绊贡献。不能说全部只有加数字，但机制差异集中在少数内容。属性提高是否跨过有意义的门槛仍须在具体战斗中检查。
- 鲜血圣杯的实际Relic绑定给初始英雄+0.15吸血，猩红战衣给初始英雄1.3倍最大生命，目标范围为英雄集合。当前常驻队伍采用英雄名册，不能再按旧单主英雄解释其覆盖。它们是否令恢复压过伤害、是否数值过强，本轮没有新增测量，只提出优先验证假设。远征口粮等旧士兵目标不能仅按“全军”字面归为对当前整队生效。
- 当前三套路线已有毒积累、己方临时单位死亡殉爆、攻击命中冻结等实际机制（内容D25），不是全部重新从空白设计。EC03分矢弓、RC01毒转移、RC03新盾传递、RC05构造体补员仍是可复用的未实现讨论草案（D22/D26）；本轮没有把旧例子改记为新完成内容。

以上能支持“优先成套设计普通遭遇、选择差异与机制配套”，不能单独证明某英雄应削多少、某遗物必须删除或目标通关率应是多少。设计建议另归Q14内容规划。

## Q13：约300个备选单位的规模依据与推演边界

2026-09-16，300及去除大部分换色的口径来自用户，本轮未开展素材去重盘点；不能覆盖此前已核验正式28名／保留7名的实现数量。阶级分布复用下述VT11／VT12，不新增联网：将约22／22／22／20／14%的内容预算乘300，得到66／66／66／60／42；单局50名示例为11／11／11／10／7。240体系联动／60公共功能及每局40–60名是Agent适配建议，无外部统计结论或模拟验证支持。具体方案归当前稿Q13。

## Q12：成熟游戏的阶数与各阶英雄种类比例

检索日期：2026-09-16。用户要求明确阶数、比例并分配英雄。本题严格区分英雄种类数、同名副本数和商店出牌概率。

### VT11：TFT Set 10英雄数据的可复核计数

来源：[CommunityDragon 14.1历史数据，en_us.json](https://raw.communitydragon.org/14.1/cdragon/tft/en_us.json)。这是社区解析并托管的游戏数据，不是Riot设计师对比例意图的直接说明。已下载并读取`sets["10"]`，按TFT10前缀、非空traits、cost为1–5筛查；保留具名英雄，排除假人、虚空虫等非商店对象。四费Akali／Akali_TrueDamage是同一英雄两种形态，合并为一个身份。按原始行数会多算一个四费。

| 固有费用 | 独立可招募英雄种类 | 占59种的比例 |
| --- | ---: | ---: |
| 1 | 13 | 22.03% |
| 2 | 13 | 22.03% |
| 3 | 13 | 22.03% |
| 4 | 12 | 20.34% |
| 5 | 8 | 13.56% |

结论限于该历史赛季：前四档种类相当接近，最高档较少。不能由较多低费副本推定大多数独立英雄都是低费，不能称这是2026最新赛季或所有成熟游戏的标准。

### VT12：Riot原文核对形态与跨阶体系

来源：[Riot，TFT: Remix Rumble Gameplay Overview](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/tft-remix-rumble-gameplay-overview/)，已取得并读取全文。原文明确Akali随场上乐队改变能力，支持VT11按同一英雄而非两种独立招募身份统计。K/DA成员横跨1–4费，Ahri／Akali承担双核；Punk原生成员均在1–2费，Heartsteel成员横跨1–5费。可以支持核心不一律最高费、低中费也是体系内容，不支持每个体系都必须平均覆盖所有档。

Q11的13.23／14.9官方补丁区分商店概率与每名英雄的有限副本，继续作为另一层证据。30%／30%／25%／15%的四阶内容预算和14名归档均为Agent项目适配，不是原作数表或经验最优值。

访问限制：13.24数据请求403，14.1历史数据可正常读取；MetaBot的Set10页实际无英雄结果，LoLCHESS静态结果无可读正文，未用它们计数。StS两个Wiki入口403，未从记忆填充卡牌稀有度人数作比较；Brave部分请求429，Bing返回泛化结果，均未作为比例证据。

## 2.3-Q11：当前英雄池、分阶与TFT供给依据

核查日期：2026-09-16。只读盘点当前工作区，并补查Riot官方历史补丁；名单收缩和阶段替代等级的方案归content-outline.md，不作为原作事实。

### 当前工程证据

- `content/project/pools/pool_designed_heroes.tres`列28个英雄id，与`content/catalogs/alpha_catalog.tres`的28个Heroes引用对应。`retained_content.tres`另列HC28、HC31–HC36共7名，不在正式池；做过的英雄数不等于正式候选数。
- 逐个读取28份英雄定义，RecruitCost均为5；`src/Content/UnitDefinition.cs`没有独立供给阶级字段。`RunOfferDefaults.WithDefaults`默认招募不按RecruitCost创建支付操作，BuyItem才按物品价格创建成本。因此5既不证明五费档，也不证明正式招募每名实际支付5金币。
- `RunDecisionService.CreateOffer`按种子、FloorIndex、BattleNumber、供给定义和候选id稳定散列排序后截取，所读路径没有阶段阶级表或阵容契合加权。`RunRewardEconomyService.CreateNewRun`接收一个已解锁heroId，并从StarterPool另抽同伴；正式StarterRosterHeroCount=1，实际起步2人但只有1人由玩家选。
- `RunRulesDefinition`当前InitialPopulation=7、OrdinaryPopulationCap=10；`alpha_run_rules.tres`的InitialUnlockedHeroCount=2。人数、候选与容量是不同字段；改6选2需要同步合法资格／开局保存，不能只改画面。
- 参照R01-D01固有费用／培养品阶、R05-D01普通容量默认开放、R06-D01组池、R07-D01／D02唯一持有与软降权、G03-D02随机起步；这些讨论不表示生产已接入。未启动引擎、模拟或游玩。

### VT09：TFT等级决定费用档概率，共享池另有有限副本

第一手来源：[Riot TFT 13.23补丁，2023-11-20](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/teamfight-tactics-patch-13-23-notes/)，已取得全文并读取SHOP ODDS／UNIT POOL CHANGES。原文明说各等级有不同搜牌用途：6级搜二费、7级搜三费、8级较易找四费、9级找完成阵容的五费。该历史版本9级表为10／20／25／35／10%，10级仍保留5／10／20／40／25%。高等级仍可能出现低费，分布由等级而非关卡直接决定。共享有限副本池另承担侦察、竞争和三星策略，不与费用档概率混为一层。

### VT10：概率表与经济、实际胜利方式共同调节

第一手来源：[Riot TFT 14.9补丁，2024-04-30](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/teamfight-tactics-patch-14-9-notes/)，已取得全文并读取LEVELING COSTS／SHOP ROLL ODDS。针对三星四费过于常见，官方同时提高8升9费用、降低8／9级四费概率；9级四费从35%降至30%。不存在独立于经济、英雄数量、培养目标的万能概率表。

两页是历史机制证据，**不是2026当前赛季概率**，也未公开可逐行复用的服务器采样实现。按阶段替代等级、同阶契合权重、空阶重分和开局多配对检查均为项目建议。一条搜索遇429，另一条主要返回攻略站；未将摘要当官方证据，最终读取上述官方原文。Q10的Slay the Spire成批内容与真实选择数据证据继续适用。

## 2.3-Q10：成熟团队如何验证构筑——原型、假人、模拟与试玩

检索日期：2026-09-15。问题来自用户追问：四五个单位是否足以检验体系，是否应先用不移动的假人和一两个核心输出做模拟；随后明确要求深度调研成熟游戏的实际做法。本题研究制作与验证方法，不以玩法Wiki、玩家攻略或社区模拟器的存在替代工作室流程证据。

### 已核查的来源与可支持结论

#### VT01／VT02：TFT早期可玩原型与生产版并行

- 来源：Riot Cashmiir，2020-04-02／03，[The Story of TFT Part 1](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/tf-t-minus-eighteen-weeks-the-story-of-tft-part-1/)／[Part 2](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/tf-t-minus-ten-weeks-the-story-of-tft-part-2/)。Riot官方回顾，包含Wittrock、Wrekz、Nullarbor等开发者直接陈述；已读取全文。
- 实际做法：早期12人、8周原型，用LoL现成资产，最早靠聊天调试信息理解局面；原型已经承载招募、部署、战斗、商店及羁绊等决策。文中出现9对9战斗及后备场景，不代表第一天就完成该规模，也不提供早期独立单位总数。
- 原型通过内部“是否好玩”的判断后，另用10周生产正式版；生产版尚未可用时，继续在原型中试玩并调整英雄阵容、物品和商店。此处“原型最终丢弃”是TFT当年的事实，不是本项目应重建战斗内核的建议。
- 发现的问题：原想缩小棋盘强化部署决策，经验多寡不同的测试者却都认为太拥挤；后续旋转棋盘又产生角色遮挡。多人技能同时播放也导致难读，团队缩减VFX、放慢部分动画。这些是实际玩法／可读性反馈，不能由单个伤害数字或静止靶验证替代。
- 可支持：粗糙表现与可玩的关键决策可以同时存在；原型可先于最终美术／架构验证乐趣。不能支持：每个体系只做固定四五名、靠假人就能证明可玩、必须复制TFT的工期／规模／PvP平衡。

#### VT03／VT04：Slay the Spire按构筑成批投放，再用整局选择与遭遇数据迭代

- 来源：[开发者访谈，Game Developer，2018-02-27](https://www.gamedeveloper.com/design/how-i-slay-the-spire-i-s-devs-use-data-to-balance-their-roguelike-deck-builder)；Anthony Giovannetti的[GDC 2019演讲页面](https://www.gdcvault.com/play/1025731/-Slay-the-Spire-Metrics)及[公开原始讲义](https://media.gdcvault.com/gdc2019/presentations/Giovannetti_Anthony_SlayTheSpire.pdf)。访谈为作者直接陈述经记者编辑，讲义为第一手资料；已读取访谈全文及22页讲义的文字，没有宣称观看完整演讲。
- 实际做法：访谈明确说早期先成批加卡以形成一个deck archetype，之后逐张添加来雕琢玩法；可大幅修改甚至丢弃整个原型构筑。未公开每批具体卡数，不能换算成四五个英雄配额。
- 原型阶段就邀请Netrunner玩家试玩并记录决策；记录给出的卡、实际选择与放弃项、胜利牌组中的出现、对具体敌人的损血等。数据来自人的完整选择与游玩，不是只让预先配好的两个牌组互打。
- 敌人也参与调整。访谈谈到某Boss对Power卡玩法的压力与整体伤害分别调整，之后再检视结果；说明总胜率不足以说明不同构筑面对同一敌人的处境。
- 讲义第7页的目标为每张卡有位置，并避免过度扭曲选择；第10页记录内部试玩Slack、反馈机器人、受邀高水平玩家及近每日构建；第13页明确“Data is evidence, but not a conclusion.”；第16页以进阶等级区分玩家技能；第18／21页将反馈、数据与持续修改合并使用。
- 可支持：先做小批相互关联内容有实际先例；还要把它放进选择与敌人环境，分别收集强度和感受；不能用日志事件计数代替这些数据。单人肉鸽允许不同牌组不完全等强，不能套用PvP各阵容50%胜率为目标。
- 史料限制：2018访谈涉及旧版具体卡牌及报道表述，本文只引用明确的制作方法，不将其中Dual Wield能力描述作为当前卡牌规则；GDC第9页成品规模不是原型规模。

#### VT05／VT06：Candy Crush生产流程确实用自动代理试玩与调参

- 来源：对King AI Labs负责人Sahar Asadi的[GamesIndustry.biz访谈，2024-06-26](https://www.gamesindustry.biz/how-king-is-using-ai-to-speed-up-development-of-new-candy-crush-levels)，以及[Mobilegamer访谈，2024-04-09](https://mobilegamer.biz/how-king-balances-human-and-ai-powered-design-in-candy-crush-saga/)。均已读取全文；属于当事人直接访谈，不是技术实现的独立审计。
- 实际做法：上线前机器人反复玩真实关卡，提供难度、重排等指标；设计师给出希望达到的标准，工具可提出自动调整，候选方案再由设计师选择或否决。两篇访谈相互补充，但同一团队／负责人的材料不算两个独立工作室的证明。
- 关键条件：目标是像玩家一样选择，不是只追求最高水平。使用大量真人状态—动作数据学习，并比较机器人估计难度与真实玩家难度的相关性；技能与偏好也是继续建模的因素。不能由大量重复局数自动推定模型与真人相符。
- 可支持：自动模拟可以参与设计／平衡而不限于查bug；成熟使用依赖有意义的评价目标、真实规则、玩家模型及结果校准。不能支持：无真实行为的假人能替代玩家，或本项目需要立即训练机器学习代理。报道中的效率百分比是King自报，不是可套用的项目收益估算。

#### VT07：League of Legends自动功能测试，与设计试玩职责不同

- 来源：Jim Merrill，[Automated Testing for League of Legends，Riot官方技术博客，2016-02-26](https://technology.riotgames.com/news/automated-testing-league-legends)，已读取全文。
- 实际做法：测试框架通过游戏客户端／服务器接口生成角色和小兵，指定属性／动作，查询结果；文章展示Kog'Maw技能对两种目标的伤害测试。测试可在本地或测试集群运行，用于技能、视野、击杀奖励等规则。
- 原文强调自动化加快反馈、释放人工去做更有创造性的测试，没有声称取代人工。其每日测试量是生产QA规模，不是好玩或平衡的验收数量。
- 可支持：对具体技能用受控对象、自动下指令及比较结果是成熟做法；不能把这种行为正确性测试等同于构筑策略成立。

#### VT08：不要混淆同一公司的不同“AI测试”

- 来源：King资深测试负责人Alexander Andelkovic，[How King uses AI to test Candy Crush Saga，InfoQ，2019-12-30](https://www.infoq.com/articles/candy-crush-QA-AI-saga/)，已读正文；[GDC Europe 2016相关演讲目录](https://gdcvault.com/play/1023858/How-King-Uses-AI-in)仅核实题目、讲者和摘要。
- InfoQ的BAIT主要识别页面元素、遍历界面、发现缺失纹理／文字／渲染等问题。它与VT05／06描述的关卡难度代理和自动微调，证据对象并不相同。不能看到“AI测试Candy Crush”就把所有能力合并到同一工具，也不能由2016目录页推断具体算法或后续能力当时已存在。

### 反例、缺口与检索边界

- [HearthSim自述](https://hearthsim.info/)和[模拟器清单](https://hearthsim.info/simulators/)明确属于社区开发项目，部分历史模拟器仅实现部分卡池。它们证明社区能建立规则模拟器，不证明Blizzard采用这些模拟器做新构筑设计；不能冠以炉石官方制作流程。
- 《Monster Train》检索命中大量同名物品“Advanced Prototype”和玩家攻略；已有开发讨论链接正文访问失败，当前不将这些结果当成开发流程证据。不能用原有玩法调研库填补制作方法空白。
- 本次未找到公开来源规定“四五个单位”是通用最小值，也没有证明上述团队完全不用静止假人；公开回顾未提及某项内部工具，不等于工具不存在。
- 普通搜索的Google／DuckDuckGo遇到JS／验证码，部分Bing结果与查询不相符，未纳入事实依据；改用能返回相关来源的Brave检索并逐个读取正文，遇到429限制时不把搜索摘要当证据。GDC讲义最终从公开PDF链接获取；早先访问失败和未成功取得的转录不当作已读材料。

另读[King关卡质量评估演讲报道，2024-03-27](https://mobilegamer.biz/how-king-defines-a-good-candy-crush-saga-level-and-why-it-constantly-prunes-the-bad-ones/)：团队强调区分难度与投入感、考虑不同玩家技能及改动对后续进程的影响。该报道不是本项目应采用变现／留存优化或动态难度的依据，本文只用来限定“胜率等于好玩”的错误推断。

本地对应证据：`work-items/active/bc-hero-roster.md`首段和`tests/FirstContentPresetsContractSmoke.cs`显示三套预设使用正式发布内容及战斗模拟；NE02曾发生5召唤物零死亡，NE10曾补选到超过3个目标。只复核已有记录／检查代码，本轮没有重新执行，不能将过去特定配置结果扩大为当前全局平衡。

检索分组：TFT prototype/playtesting、Slay the Spire metrics driven design、Monster Train Shiny Shoe development、game balance automated simulation／Hearthstone、Candy Crush King automated playtesting及Magic Nuts & Bolts playtesting。Magic方向因检索限流未取得相关原文，未形成案例；不存在因常识印象补写其做法。原有deep调研目录只做方法关键词及相关游戏入口检查，其主体是玩家机制证据，没有用它冒充制作过程研究。

跨案例分析、测试适用条件、现有预设局限及本项目建议统一归[content-outline.md的Q10](content-outline.md)。本轮调研收束，未将建议记为已采用；不改代码、内容数值、现行权威或原有只读调研库。

## 2.3-Q09：整局平线体验的本地实现核查

2026-09-15，只读检查当前main工作区（已有大量未提交实现／资源改动，以工作区为依据，不以HEAD替代现状）。用户报告随意选英雄、生命与吸血遗物足以通关；本轮未读取该局存档、种子或战报，未运行游戏／模拟，不能确认跨种子的通关率、具体收益贡献或数值阈值。没有新增外部游戏机制研究。

- `content/project/alpha_campaign.tres`：三区，每区5层；开始／招募同用设计英雄池，战利品／商店同用全部物品池。`src/Run/TowerGenerator.cs`、`src/Project/EncounterDefinition.cs`及三区普通遭遇：普通BaseEnemyCount=4，默认加区域序号，故4／5／6人；逐个从本区敌池随机抽取。敌种、场地、精英及Boss存在差别，但此路径没有阶段职责组合编排。
- `src/Run/RunBattlePreparationAdapter.cs`、`src/Battle/BattlePreparationContracts.cs`及`BattleSimulation.cs`初始化：敌人从内容快照准备，敌方生命／伤害初始化倍率为1，已读路径未见楼层属性成长。不能由此推定所有敌种基础属性相同或场地规则不影响强度。
- `src/Run/RunModels.cs`保留Rank=1；`src/Project/RunOperationDefinition.cs`和`RunDecisionService.Apply`没有培养／升阶操作。`RunOfferDefaults.cs`现行兼容供给为招募、物品、金币与恢复，没有阶段培养或人口奖励配方。`BattlePreparationContracts.cs`虽能接受RetainAttackStacks测试加强，正式`RunBattlePreparationAdapter.cs`未传入该选项。结论限定为现行正式流程缺少该培养接入，不否认实验室／底层已有局部能力。
- `src/Run/RunDecisionService.CreateOffer`：按种子、楼层、战斗次数及候选ID稳定排序后截取；默认招募和物品候选各3个。未读取当前阵容的体系／缺口作倾向推荐，且默认配方不按阶段更换物品池。候选有随机性，不等于已落实G03的适配与开放供给。
- `content/relics/definitions/item_crimson_mail.tres`：初始非临时英雄生命×1.3；`item_blood_chalice.tres`：同类目标吸血+0.15。`src/Relics/RelicScopes.cs`的PlayerHeroes目标按Team=0、IsInitial、非临时及IsHero筛选。`BattleSimulation.ResolveAttackHit`在实际普攻命中伤害后触发吸血，不据此宣称所有技能／毒伤都吸血。`item_field_rations`使用PlayerArmy目标，当前筛掉IsHero，不能把其12%生命当作全英雄加成相乘。

分析与建议统一见 [content-outline.md 的2.3-Q09](content-outline.md)。沿G03-D02／I13检查探索、成长、供给与敌人压力，沿R01／R02／I24检查正式培养，保持英雄自身能力不跨战积层及R10暂缓边界。本轮不修运行时、物品或现行权威。

## 2.3-Q08：当前内容盘点与具体补充依据

2026-09-13只读核对：`content/catalogs/alpha_catalog.tres`的实际数组有28英雄、12遗物及3装备；`content/project/pools/pool_all_items.tres`包含这15件物品。通过catalog entry→item scene→Relic／Equipment定义引用区分展示资料与效果，不把重复定义路径重复计数；目录／池中存在不等于每局能取得或已经验收。

- `content/relics/definitions/`现有12项主要为属性绑定、开场盾／召唤与胜后金币。引魂灯当前全军目标属性绑定各增6%，其展示文案写士兵，存在口径差异，本轮不修运行内容；RC02明确是替换建议。构装机芯当前开场召唤引用`soldier_dummy_melee`，没有查到本定义的死亡补员，RC05是新增行为。
- `content/equipment/definitions/`：先锋徽甲加12护甲，精工瞄具加10攻击伤害，霜痕战刃命中给攻速状态／目标寒霜并提供凛冬贡献。`content/traits/definitions/trait_winterbound.tres`现有单档门槛2，属性效果攻速+0.15；`content/statuses/status_frost.tres`当前3层转冻结。与概率冻结＋层数提高概率候选不同，不能说讨论方案已实现。
- 英雄当前事实交叉核对本专题当前稿与`work-items/active/bc-hero-roster.md`，28名可用与外观不复用保持。HC07／HC10／HC16／HC20旧反馈和未采用建议继续有效；暂停英雄不能作为可用配套证据。

设计来源复用Q02–Q07及BC来源映射：毒防守／倍毒与The Specimen、Copycat亡语代触发、Set1极地攻击冻结、护盾当前量／受盾次数／死亡与治疗的不同输入。HC10己方尸爆承接用户NE02-P02方向；TC／RC／EC数值与具体效果为项目提案，不冒称原作相同技能。无新增联网、运行验证、供给概率核查或研究原库改动。候选唯一当前稿为content-drafts.md的D22清单。

本文件记录第二步内容规划的研究依据，不是现行权威。候选完整说明与建议见 [内容规划草案](content-outline.md)，用户确认的覆盖范围见 [讨论决定](decisions.md)，进度只在 [总览](../roadmap.md) 维护。研究材料不自动成为采用决定。


## 2.3-Q07：规则拆解的依据与分析边界

2026-09-12，用户要求从本质拆解羁绊、正负印记与不分主被动的英雄技能。本轮无新联网；主责回读 M01、M03、M04 与既有Q06，另由一名只读Agent检查概念是否过窄。分析 CG06–CG10 统一维护在 content-outline.md，不将其包装成新的原作研究结论、技术架构或用户决定。

M03 的多种受益方式说明羁绊不等于全队buff，成员关系可作成立条件或计算输入；M01 的固定能力属于本项目内容规格，不意味着技能必须只作用本人或只有主动动作；M04 的不同状态结算允许状态带行为和动态积累，不能只称数据容器。NE01 的毒持续／倍增、NE10 的羁绊与攻击特效、既有Copycat亡语代触发提供例证边界，不以它们证明所有来源共享相同规则。

双重施法贯穿三种承载是纸面思维实验：共同奖励、资格与重复规则要用一致假设比较，临时一次性与常驻版本本来就不同。印记按本轮语境为正负状态，不与职业／阵营标签混同；亡语可固有、可被状态赋予、也可由羁绊提供，不能天然归入其中一类。关于取得、维持、分配、失去能力的不同玩法意义为 Agent 推演，未运行或新增内容验证。

## 2.3-Q06：羁绊与负面状态共存的全局分析依据

2026-09-12，用户要求全局分析。本轮无新联网，回读 M03-D01–D03、M04-D01–D02、当前内容稿及 I16／I18／I19／I27 等现有整合依赖。

- M03 已支持可替代标签成员、外部贡献／成员身份的区别、普通开战定档、不同羁绊不同受益；M04 已允许不同状态结算和投入成形的持续控制。因此不存在必须在羁绊与状态中二选一的模型决定，也不能将“羁绊战前、状态战中”当硬分界。
- NE01 毒／催化剂／遗物转移展示具名能力通过状态配合；NE10 原作极地羁绊＋狗熊连锁攻击展示羁绊可提供状态入口、技能决定覆盖。二者证明不同承载方式实际存在，不证明本项目某个具体配方已平衡或哪一种全局最优。
- NE02 原作给敌人挂尸爆，项目已选择己方召唤物死亡爆炸；后者属于友方获得的死亡能力／事件读取，不是自动归类为敌方负面状态。护盾、恢复、召唤等也可形成无负面印记的构筑，不能将状态扩展变成每种玩法新增敌方标记。
- 供给、替代成员和阶段性可用性的风险与 I27／既有内容池讨论关联；效果来源、共同状态和资格表达与 I16／I18 关联；控制正反馈沿 M04-D02 的既有空间评估，不借本轮分析新增全局抗控保障。当前没有新技术契约冲突，不新建重复跨题问题或改写原整合状态。

完整分析、优缺点、互补／重复实例和建议 CG01–CG04 只维护于 content-outline.md。本条为依据与边界，不将分析建议升级为原作事实或用户决定。

用户本轮补充要求以互换检验不可替代之处，扩展 CG05，并新增公开来源：[CommunityDragon 10.19 TFT数据](https://raw.communitydragon.org/10.19/cdragon/tft/en_us.json)，HTTP实际读取。只取历史定性机制，不混合不同数据块的档位数值：Brawler “Brawlers gain bonus Health”；Duelist “Duelists' attacks grant Attack Speed”；Mage “Mages cast twice and have modified Spell Power”；Cultist 明确队伍损失一定生命后召唤 Galio，落向敌方最密集处并击飞。原作快照不是现版本说明，也未据数据自行补造事件顺序。Set1 极地继续引用本文件 NE10 的9.14资料，毒继续复用NE01，项目叠冰层／己方尸爆仍标项目候选。

CG05 的互换后果属于 Agent 规则推演，不是原作性能测试：加生命不等价于给部分敌人减伤，自身攻速成长不等价于目标受伤加深，重复施法不等价于额外法伤，新增单位不等价于范围爆炸；把毒由羁绊授予仍保留毒状态。没有在本项目实施或验证这些反事实方案。

## 2.3-Q05：羁绊与技能的承载比较

2026-09-12，用户选择仅召己方小兵用于尸爆，认为给敌方召兵刻意，随后要求比较羁绊主导、纯技能组合、小羁绊＋技能效果。本轮未新增联网；回读 M03-D01–D03、M01-D01、当前英雄稿与既有 NE01／NE02／NE10 证据。

- [M03-D01–D03](../02-foundation-models/trait-model/decisions.md) 已明确英雄标签为常规基础、非羁绊联动独立成立、不同羁绊可有不同受益范围；普通开战定档，召唤不凑档但可按资格受益。因而一个体系不设专属羁绊不等于全游戏取消羁绊，给召唤物技能效果也不要求它贡献人数。贡献点数与成员身份分开，不能由冰冻小羁绊自动使无成员资格的全部攻击者获益。
- NE01 原作的毒／催化剂／生物样本分别由卡牌和遗物承担，能证明状态与具名供给／改造可以分开；不能直接证明本项目固定技能的法力循环、倍毒频率或资源供给已经合理。
- NE02 原作尸爆来自一张明确施加死亡效果的卡牌；己方召兵接爆炸是项目构想。推荐用具名技能承载爆炸、其他召唤者供身体，是本轮 Agent 的内容分工，不冒称原作已有这支自动战斗阵容。
- NE10 的原作资料明确把概率控制放在极地羁绊，把多目标连锁放在 Volibear 技能，正好支持“羁绊授予一项共享资格，技能决定发挥方法”的参照。项目选低档人数、概率和受益范围仍是候选，未照搬原作全部成员与装备。
- 当前 HC08 可供毒，HC24／HC25 可供己方临时单位，属于设计／实现初稿背景，不是已具备新尸爆能力的证明。HC24 依赖虫卒存活攻击供盾，尸爆更快兑现与其长期攻击收益可能相冲，不能把所有召唤同质化。

承载比较和具体建议统一在 content-drafts.md 的 NE01-C01／NE02-C01／NE10-C01；本轮用户明确决定只有己方召唤方向。未发现需要改写 M03 的新通用规则，未修改研究原库或游戏。

**2.3-D14 后续修正**：用户提出技能叠层提高冻结概率，追问羁绊的实际好处。本轮无新联网；原作 NE10 只证明极地羁绊与狗熊技能曾这样配合，不能证明该承载方式必然优于本项目技能配合。M01 固定主动／被动使共享能力的配置位置有意义，M03 提供标签、外部连接及成员分工空间；但团队受益／持续授予等技能也可表达，不能说羁绊独占这些能力。成员替代、占位成本和招募选择是否有价值需要具体内容支持。NE10-P02 为用户新候选，不据此改写原作证据；当前取消冰冻优先用羁绊的推荐，保留两者可组合。

## 2.3-Q04：拉面熊的极地冰冻与本轮方向选择

2026-09-12，用户肯定 NE01 毒核心防守、NE02 尸爆的参考价值，并提出敌／我两侧召唤小兵接尸爆。冰冻参考在澄清后明确为早期“拉面熊”，新增原作参考编号 **NE10：Set 1 极地 Glacial＋Volibear**。这不是 S9 弗雷尔卓德，也不是沿用 Agent 上轮首推的 Hades 寒冷。

### NE10 核实来源

- [CommunityDragon 9.14 TFT 英文数据](https://raw.communitydragon.org/9.14/cdragon/tft/en_us.json)，本次公开HTTP读取成功。Glacial 描述：“Glacial attacks gain a chance to stun enemies”；9.14档位为2／4／6极地对应20%／30%／45%，控制2秒。Volibear 的 Thunder Claws 描述：“Volibear empowers his attacks to chain between enemies, applying on-hit effects”。这里足以确认普攻触发控制＋连锁传递攻击特效；未把数据数组索引臆测成所有星级精确跳数，不复刻数值。
- [Glacial (Teamfight Tactics) 历史规则页](https://leagueoflegends.fandom.com/api.php?action=parse&page=Glacial_(Teamfight_Tactics)&prop=wikitext&format=json)，本次HTTP读到wikitext，明确取 **Set 1** 段及其补丁历史，避免混入Set 2。实践段提到攻速装备如羊刀和生存装备可以增加冰冻机会；前排吸住攻击时，后排输出与刺客仍能施压；降低攻击速度则降低触发频率。9.19后控制时长改1.5秒、概率改20%／33%／50%，所以本轮9.14只作历史窗口，不说整个拉面熊时期数值一直相同。
- [CommunityDragon 9.15 数据](https://raw.communitydragon.org/9.15/cdragon/tft/en_us.json)亦可访问；本轮不需要重复提取所有相同字段。猜测的旧官方9.14／9.15补丁URL返回404，Volibear `/Set_1` Fandom页返回 missingtitle；不将它们列为有效机制证据。

**规则与推演分开**：原作数据明确连锁攻击附带命中特效，极地按攻击触发眩晕；因此高攻速＋多目标攻击增加控制机会和覆盖是有依据的组合关系。不是全场定时控制，也不是基础冰冻自带层数爆炸。本轮不声称历史唯一最优装备组合、每跳的独立随机实现或所有免控／续冻边界已逐项核实。

### 对用户反馈的解释与迁移范围

NE01 的毒本身承担主要输出、其他位置主要防守，是用户明确看重的关系；Agent 上轮把推荐重点移到 Hades 寒冷没有命中用户意图，不能因它同样依赖叠层就降低毒的参考优先级。NE02 的原作死亡事件输出与用户提出的主动生产小兵结合，可以减少完全依赖天然敌方首杀载体的限制；**召敌方小兵／己方亡语炸兵属于项目构想，原作证据没有提供这两种召唤技能**。

澄清前曾查 [S9／13.12数据](https://raw.communitydragon.org/13.12/cdragon/tft/en_us.json)，其中 Set9_Freljord 是8秒后风暴、最大生命比例真伤与破甲／破魔抗／破法／眩晕分档。用户已明确排除此参照，记录只用于避免下次再次认错，不作为本轮冰冻候选。

项目方向与最小角色讨论稿归 content-drafts.md 的 NE01／NE02-P01–P02／NE10-P01；仅记录、不改游戏或原研究库。

## 2.3-Q03：热门、好评游戏中已有的负面效果

2026-09-12，按 2.3-D11 改变检索方向。HC08-P01／HC10-P01 已被用户否定，本节先呈现原作事实和已存在的配套，不新造替代英雄。主责复用《杀戮尖塔》深记录、核对 Steam 评价并补读尸爆／毒转移；两名只读研究 Agent 分别补读 Hades 1 和 BG3 的状态与配套页。研究原库未修改。

### 游戏筛选与实际访问

口碑取 Steam 全语言、全部购买类型的全期评价接口：`https://store.steampowered.com/appreviews/<appid>?json=1&language=all&purchase_type=all&num_per_page=0&filter=all`，本次读取如下。评价量作为受众规模的代理，不等于销量或活跃人数；**游戏整体好评不证明每一个负面机制都获一致好评**。

| 游戏／appid | 总评价 | 好评数／比例（约） | API 评价标签 |
| --- | ---: | --- | --- |
| Hades／1145360 | 308,422 | 302,267／98.0% | Overwhelmingly Positive |
| Slay the Spire／646570 | 218,345 | 212,812／97.5% | Overwhelmingly Positive |
| Baldur's Gate 3／1086940 | 857,125 | 829,581／96.8% | Overwhelmingly Positive |
| Elden Ring／1245620 | 1,154,324 | 1,073,985／93.0% | Very Positive |

前三作已提供足够不同的机制，本轮未继续查 Elden Ring 技能，因此不把出血、冻伤或火清冻伤等记为此次已核规则。检索入口是具体状态、祝福、卡牌和装备名称；不是只读店页、搜索摘要或攻略标题。部分 Fandom／wiki.gg 普通页面及默认请求返回403；后改用带 Mozilla User-Agent 的 Fandom `api.php?action=parse&prop=wikitext&format=json` 读取成功，BG3 通过 `?action=raw` 读取。Hades 严格使用第一作 `/Boons (Hades)` 页面，不混入续作。以下是社区规则页和既有研究支持的原作组合，不宣称本轮启动游戏复验或已核当前最强流派排名。

### NE01–NE09：原作状态与具体配套

| 编号 | 游戏／负面效果 | 原作基础规则 | 已有配套怎样改变战斗 |
| --- | --- | --- | --- |
| **NE01** | 杀戮尖塔／毒 Poison | 敌人回合开始按当前层数损失生命，绕过格挡，随后减1层。 | Catalyst 将已有毒翻倍，升级后三倍，并消耗该牌；The Specimen 在敌人死亡时把剩余毒转移给随机敌人。防御争取结算时间，倍率做单体积累，转移遗物让投入接续到下个目标。这些是卡牌／遗物能力，不是毒天然都有的功能。 |
| **NE02** | 杀戮尖塔／尸爆 Corpse Explosion | 卡牌分别施毒与尸爆两个负面效果；尸爆令目标死时对其他敌人造成等同其最大生命的伤害，死亡原因不限于毒。 | 可以给大体型敌人挂尸爆再集中击杀，或先处理容易杀死的小怪更早清场；Burst 可重复施放技能牌，尸爆效果本身可叠强度。读的是死者最大生命与死亡事件，毒层不是爆炸伤害基数。单体敌人没有其他目标时失去群伤价值。 |
| **NE03** | Hades 1／厄运 Doom | 施加后1.1秒发生一次伤害，基础最大1层。 | Ares 的 Curse of Agony 让攻击施厄运；Dire Misfortune 才赋予爆发前重复施加增伤；Impending Doom 加伤但再延迟0.5秒；Merciful End 让可反弹的攻击提前兑现厄运。基础延时单次伤害与几种祝福改造分开。 |
| **NE04** | Hades 1／震颤 Jolted | 敌人下一次攻击使自己和附近敌人遭受雷电伤害，基础一次触发后消失，未触发也有时限。 | Static Discharge 让雷击施震颤；Cold Fusion 改为敌人攻击后状态不消失，持续10秒。伤害条件是敌方攻击，附近敌人数也影响收益，不是我方不断命中就自动触发。 |
| **NE05** | Hades 1／撕裂 Ruptured | 持续3秒，目标移动时每0.2秒受伤。 | Razor Shoals 让祝福赋予的击退施撕裂；Second Wave 增加延迟0.7秒的第二次击退。移动与击退组织这条关系；原页未核实全部脚本位移／传送判定，不能声称任何强制位移都必定结算。不能将所有武器自带击退都当施加来源。 |
| **NE06** | Hades 1／寒冷 Chill | 每层减速4%，最多10层，持续8秒；基础没有周期伤害。 | Arctic Blast 在10层时清除寒冷并爆发；Killing Freeze 要求全场敌人都带寒冷，才给予额外减速和持续伤害；Winter Harvest 让带寒冷的低血敌人在10%生命线碎裂，向附近施寒冷。分别偏好集中叠满、全体覆盖、压低生命与利用敌群，三种祝福不是互斥全局选项，也不是寒冷默认全包。 |
| **NE07** | 博德之门3／潮湿 Wet | 获得电／冰易伤、火抗，并免疫燃烧。通常易伤使对应伤害翻倍、抗性减半；原有抗性／免疫仍需按各自规则处理。 | Create Water 给施法时范围内生物施3回合潮湿，随后 Call Lightning 或冰法术利用易伤。重复召雷仍需动作并维持专注。供水者占用行动来服务队友，火系攻击同一潮湿目标反而受损；留下的水地表与潮湿状态不是同一永久效果。 |
| **NE08** | 博德之门3／光耀法球 Radiating Orb | 每剩余回合使目标攻击掷骰-1，最多-10，并令目标发光；目标每次攻击消耗2回合。基础不造成伤害。 | Luminous Armour 让穿戴者造成光耀伤害时向附近施法球；配合光耀版 Spirit Guardians 可形成近身范围压命中的玩法。Callous Glow Ring 另读取目标已被照亮而补光耀伤害，伤害来自装备。维持近身范围和专注关系到供应能否持续。 |
| **NE09** | 博德之门3／混响 Reverberation | 每剩余回合降低力量／敏捷／体质豁免1点；达到5回合及以上时造成1d4雷鸣伤害，目标进行DC10体质豁免，失败倒地，然后清除混响。 | Gloves of Belligerent Skies 通过雷鸣／电／光耀伤害供给；Boots of Stormy Clamour 通过向敌人施加状态供给，因此可连接法球等状态。先降低对某些控制的抵抗，再到阈值尝试击倒，不能写成必定眩晕；装备有每动作／效果限制，不能宣传任何范围攻击都会全场叠满。 |

### 来源与关键原文

- **NE01 既有研究**：`ev-slay-the-spire-011-poison-catalyst-build`、`ev-slay-the-spire-012-catalyst-artifact-boundary`，原库 [Slay the Spire 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/slay-the-spire.md) 的2.x规则与历史实践边界保持；Poison／Catalyst 来源在原 source-index。新增 [The Specimen](https://slay-the-spire.fandom.com/api.php?action=parse&page=The_Specimen&prop=wikitext&format=json&formatversion=2)：“Whenever an enemy dies, transfer any Poison it has to a random enemy.” 页面还说明若被毒杀，转移的毒不在同回合再次造成毒伤，不能推成毒杀瞬间无限结算。
- **NE02**：[Corpse Explosion](https://slay-the-spire.fandom.com/api.php?action=parse&page=Corpse_Explosion&prop=wikitext&format=json&formatversion=2)：“Applies 2 distinct debuffs in that order: poison and ‘Corpse Explosion’”；“The fatal damage can come from any source--it does not have to come from Poison.” 基础描述为死亡时向其余敌人造成最大生命倍数伤害；Strategy 明确讨论低血目标早清场与高最大生命目标大爆炸的选择。不是将爆炸错误归为毒的天然附属规则。
- **NE03–NE06 基础状态**：[Hades Status effects](https://hades.fandom.com/api.php?action=parse&page=Status%20effects&prop=wikitext&format=json)。Doom：“After 1.1 Seconds, victim takes a burst of damage.”；Jolted：“Victim's next attack self-inflicts lightning damage that harms itself and nearby foes.”；Ruptured：“For 3 Seconds, victim takes rapid damage every 0.2 seconds while moving.”；Chill：“Victim is slowed by 4%.” 撕裂最大叠层栏为问号，本轮不补造上限。
- **NE03 祝福**：[Ares/Boons (Hades)](https://hades.fandom.com/api.php?action=parse&page=Ares/Boons%20%28Hades%29&prop=wikitext&format=json)。Dire Misfortune 解释爆发前重复施加；Impending Doom：“deal more damage, but take +0.5 Sec. to activate”；Merciful End：“Your attacks that can Deflect immediately activate Doom effects.” 不从文字补造未核的内部结算时序。
- **NE04 祝福**：[Zeus/Boons (Hades)](https://hades.fandom.com/api.php?action=parse&page=Zeus/Boons%20%28Hades%29&prop=wikitext&format=json)。Static Discharge：“Your lightning effects also make foes Jolted.”；Cold Fusion：“Jolted status does not expire on your enemies' attacks.”，列10秒。敌人特殊招式怎样计次尚未逐项核查。
- **NE05 祝福**：[Poseidon/Boons (Hades)](https://hades.fandom.com/api.php?action=parse&page=Poseidon/Boons%20%28Hades%29&prop=wikitext&format=json)。Razor Shoals：“Using knock-away effects also Rupture foes.”，注释限定祝福击退；Second Wave：“Your knock-away effects shove foes a second time after the first.”，二次延迟0.7秒。Wave Pounding 另为击退祝福提供对Boss的增伤，不代表Boss受击退资格自动改变。
- **NE06 祝福**：[Demeter/Boons (Hades)](https://hades.fandom.com/api.php?action=parse&page=Demeter/Boons%20%28Hades%29&prop=wikitext&format=json)。Arctic Blast：“Applying 10 stacks of Chill causes a blast, clearing the effect.”；Killing Freeze：“When all foes are Chill afflicted, they become Slow and Decay.”；Winter Harvest：“Chill-affected foes shatter at 10% HP, inflicting Chill nearby.” 减速范围不擅自拆成未核的每类动画／攻速数值。
- **NE07**：[Wet](https://bg3.wiki/wiki/Wet_(Condition))、[Resistances](https://bg3.wiki/wiki/Resistances)、[Create Water](https://bg3.wiki/wiki/Create_Water)、[Call Lightning](https://bg3.wiki/wiki/Call_Lightning)。“Resistant to Fire damage.”／“Vulnerable to Lightning and Cold damage.”；造水“Creatures standing in the area when the spell is cast will become Wet.”。本轮没有把 Frozen 的回合边界分支纳入推荐：该来源冻结的持续与回合开始恢复有问题，不能直接称稳定跳过敌方完整回合。
- **NE08**：[Radiating Orb](https://bg3.wiki/wiki/Radiating_Orb_(Condition))：“-1 to Attack Rolls per remaining turn”；“Reduce the duration by 2 each time the entity makes an attack.”；[Radiant Shockwave](https://bg3.wiki/wiki/Radiant_Shockwave)、[Luminous Armour](https://bg3.wiki/wiki/Luminous_Armour)、[Spirit Guardians](https://bg3.wiki/wiki/Spirit_Guardians)、[Callous Glow Ring](https://bg3.wiki/wiki/Callous_Glow_Ring)。护甲赋予光耀伤害挂状态，戒指赋予对照亮目标附伤，不归为全局光耀规则。Coruscation Ring 的“施法者必须被照亮”有Wiki注明的实现偏差，本轮不以该bug建立必要组合。
- **NE09**：[Reverberation](https://bg3.wiki/wiki/Reverberation_(Condition))：“-1 penalty to Strength, Dexterity, and Constitution Saving Throws per remaining turn”；达到5后“must succeed a Constitution saving throw (DC 10) or fall Prone. The condition is removed afterward.”；[Gloves of Belligerent Skies](https://bg3.wiki/wiki/Gloves_of_Belligerent_Skies)、[Boots of Stormy Clamour](https://bg3.wiki/wiki/Boots_of_Stormy_Clamour)。手套／鞋子的Wiki注释记有一次动作／效果仅作用一个目标等限制，混响本身不会再触发鞋子自循环。不能忽略限制合成全场自动倒地保证。

### 对当前讨论的用途

优先对照 Hades 同一作内的四种状态：等待一次兑现、敌人攻击、敌人移动、减速与覆盖，原作差异可直接讲给玩家听；其中寒冷最适合说明“简短基础词条＋多种具名配套”怎样形成玩法。Slay 的毒／尸爆展示积累、投入转移与敌方死亡利用；BG3 展示伤害类型协作、压制命中与降低控制抵抗。以上是原作参考，**没有决定给 HC08／HC09／HC10 分配哪一项，也未形成项目数值或自动施法方案**。撕裂需要有效移动场景，卡牌与手动施法的先后选择也不能默认迁入全自动战斗；在选定参考后结合具体角色讨论这些实际适配即可，不反复要求批准通用设计自由。

## 2.3-Q02：印记是否改变构筑选择，而不只是叠层加伤

2026-09-12，用户认为蚀毒、破绽、余烬尚未呈现明确体系差异，要求回读本项目资料库。此次由主 Agent 与两名只读研究 Agent 分工回读深记录完整条目、对应游戏档案和部分来源索引；未联网复核原网页，未修改研究原库，未运行游戏或改动技能。原作版本事实与实践意见按库内限定使用，项目建议不等于用户采用。

### 当前项目诊断

- 蚀毒与余烬采用同类周期伤害状态：同样可叠至 99、继续施加刷新持续，每秒结算；分别为 3 真实伤害／8 秒与 4 魔法伤害／6 秒。主要额外区别是 HC10 消费余烬、立即兑现剩余周期伤害。伤害类型与时间确实不同，但现有内容没有充分证明它们会让玩家选择不同的供给、装备与战斗组织。上一轮仅按“拖时间／引爆”解释已过度宣称体系差异。
- 破绽实际 `StackLimit=1`，不靠印记叠层；但 HC09 的附伤读取命中者攻击力 20%。忽略防御、暴击等变量，对普通攻击举例：10 次×10 攻与 1 次×100 攻，附伤均为 20。它更接近按原攻击规模加一个比例；技能箭等仍有具体系数差异，不能宣称所有伤害都严格乘 1.2，但也不能仅凭“逐次命中”就宣称明显偏爱低单段多次命中。
- 多段也提高 HC08／HC10 普攻施加次数。若三者最终都要求更多普攻、相同坦克和同一目标，图标与结算时间不同不足以支付三个独立体系的理解成本。
- HC11 三种状态一次施加、再按种类附伤，当前只是既有状态之间的接线；不能反过来用它证明每种状态已各有独立玩法。

### 原作回读：真正改变了什么

| 编号 | 原库证据与版本 | 规则／实际配套 | 本项目启示与迁移限制 |
| --- | --- | --- | --- |
| ST-R01 | `ev-vivid-005-fool-shock-rubela-build`；[Vivid Knight 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/vivid-knight.md)，v1.1.10–1.1.25 | Topaz 给全敌 Shock，Rubela 六段命中逐次取得 **flat added damage**；Reno 站核心身后提供 Truestrike，Fool 增加行动。Topaz 可不升级，资源投入集中到多段与启动者。 | 每次固定附伤与按命中者攻击比例附伤不能混同。本地保留资料未提供完整固定附伤基数公式，不补造层数／施加者属性关系。永久符号、回合与同名收集不直接迁入。 |
| ST-R02 | 同上档案 Moonlight 段落，及 `ev-vivid-008-maze9-spike-form-anti-attack-build`（后者 v1.2.3） | Moonlight 为受命中消耗／衰减的伤害增幅，偏好单次重击，多段更快耗掉；Spike Boss 又通过反击／受击变化惩罚普通直接多段。 | 同样是加伤，配套价值可以反转：多段有利 Shock，却不利 Moonlight。未保留 Moonlight 每次衰减数或完整倍率公式；昂贵不攻击队不是本项目可直接复用的常规阵容。 |
| ST-R03 | `ev-tlf-004-no-healer-shock-stun-build`；[TLF 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/the-last-flame.md)，2024-06 EA | 此作 Shock 主要提高眩晕价值。Zotz 供状态，Arya 范围控，Orion 对位最高输出，Zephys 接杂兵；状态与控制在同一范围相接，以行动压制承担生存。 | 这里的状态改变敌人行动与我方恢复需求，不只加伤。与 Vivid 同名 Shock 的效果不同，不能合成元素通用规则；当前1.x强度未知，不能照抄六角覆盖与装备数值。 |
| ST-R04 | `ev-astro-004-burn-detonation-five-hero-build`；[Astronarch 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/astronarch.md)，2021-03-02／约 v1.3.5 | Paladin、Pyromancer、Assassin 与装备共供 Burn，Pyromancer 消费剩余 DOT；Druid／Cleric 负责撑住，邻接和刺客后排入口影响供应与击杀对象。 | 原作本身也是自动战斗：区别来自供给对象、攻击与施法投资、保护和较早删除威胁，不要求手动才能成立。若项目只剩同一前排自动叠满即炸，关系就被压扁；原五人方案不适用旧四人 C20，也不是现版强度证明。 |
| ST-R05 | `ev-astro-007-frost-cap-cashout-rework`，官方 v1.3.3 | Frost 上限5→10、每层效果减半、最大减速不变；Avalanche 由逐次耗一层改成满十层全耗，对目标与邻接敌人爆发。 | 平时减速已在保护队伍，消费会放弃当前控制以换范围伤害；目标周围敌人数改变价值。迁移有意义的是控制与爆发的取舍，不是再给每种元素加一个满层爆炸。 |
| ST-R06 | `ev-bpb-005-poison-threshold-protection`、`ev-bpb-007-venomancer-snake-scythe-build`；[Backpack Battles 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/backpack-battles.md)，2024-03 EA | 棺材／Goobert 产毒，Snake 保毒抗清除，Scythe 读取毒阈值，防具／法力／Crown 与部分 Emerald 提供防护；背包位置决定多个供给关系。 | 成型依赖产毒、保毒和活到阈值，不只是增加毒伤。35毒阈值不当现版；独立英雄装备不能照搬共享背包。项目若没有对应清除威胁，不能为凑体系硬加保毒模块。 |
| ST-R07 | `ev-slay-the-spire-011-poison-catalyst-build`、`012-catalyst-artifact-boundary`、`013-poison-phase-counters`；[杀戮尖塔档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/slay-the-spire.md)，2.x | 毒按敌人回合结算并减层；Catalyst 花能量倍增既有毒后消耗。Artifact 拦新增施加与阶段清已有状态是不同阻断。 | 手动保留倍增、有限卡牌、能量与阶段窗口共同产生时机选择；改为无限自动倍增会丢失这些条件，不能以原作毒牌组为这种自动技能背书。 |
| ST-R08 | `ev-storybook-brawl-007-copycat-good-boy-build`、`008-copyboy-nerf-counter-space`；[SSB 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/storybook-brawl.md)，67.4之后文本与2022站位实践 | Copycat 攻击代触发身后亡语；Good Boy 把自身属性给予其他 Good 角色。直接死亡路线希望供体早死，代理路线希望供体活着；一号位先手只读一人、二号位多读但更易先死。 | 即使同一个亡语，保护与牺牲、站位、供体培养和受益者存活的选择会改变。亡语体系潜力来自这些关系；67.4前自我增长反馈已过时，不恢复为规则。 |

补充完整回读：`ev-slay-the-spire-009-frost-blizzard-build`（当前存量与本战历史生成次数不同，原球槽／Focus／产盾不能缩成敌方冰标记）；`ev-dwarves-glory-death-loot-011-frost-status-defense-actions`（v2.0.10类规则＋v2.1，Frozen 涉及 Block/Dodge 资格与击碎，精确现版参数未知）；`ev-guildrun-013-poison-rider-conversion`（0.5.6／0.5.7期间，毒存在资格、属性转换与毒量分开，缺完整当前配方）；`ev-tlf-007-surge-team-battery`（1.0／1.0.1，共享供能与具体出口分工）；`ev-vivid-002-color-mark-dual-axis`（可跨标签配队，不要求路线互斥）。这些支撑比较，不是本轮新增状态清单。

另以 `ev-siralim-ultimate-014-shadowbringer-blighted-build`、`ev-monster-train-020-seraph-four-counter-packages`、`ev-backpack-hero-036-charm-action-byproduct-ownership`、`ev-dota-underlords-015-build-four-brute`、`ev-girls-of-the-tower-006-cold-touch-build` 交叉查阅事件转换、反制位置和作用资格。它们分别保留不同阵容／回合制、历史补丁与配套取得限制，不用于补造毒死亡传播、任意元素反应或当前胜率。

### 当前建议，尚未采用

1. **ST01 持续伤害先收敛**：蚀毒和余烬先按同一类持续伤害候选看待，暂不围绕它们各扩一整套英雄。引爆可以是某种持续状态的消费者；只有目标、覆盖、供给与消费后果显著改变玩法时，再讨论是否值得保留独立状态。未决定删毒、删火或修改现行资源。
2. **ST02 明确命中型配套**：若保留破绽作为高频体系，附伤基数须真正支持低单段多命中；固定附伤或供应者决定的附伤是项目候选，而非原作已核实精确公式。标记覆盖足够后，后续投入应能转向命中者，而不是继续堆同类挂印者。
3. **ST03 先改变战斗再谈读数**：优先研究已有霜缓／控制能否形成保护、目标与消费取舍，不立即新造第四个印记。重击消耗型增幅作为对照候选可解释多段与单段的相反价值，不等于本轮采用新规则。
4. 后续介绍每种状态须回答：得到它之后会换谁、怎样分配装备、打谁／怎样站、资源消失时失去什么。并非每个状态都要独立成体系；共享供给、互相组合与不同收益出口可以并存。亡语优先沿现有角色验证这些关系，不强制拓展规模。

## HC03 嘲讽与防御反伤：素材核对

2026-09-11。用户否定 HC02 当前稿后指定坦克的主被动方向，见 [2.3-D04](decisions.md)及 [HC03 当前稿](content-drafts.md)。本轮复用 BC14、旧 V01-RF05–RF08 与 G01 的 K09／K26，不重新研究全库；在原深记录中按 `taunt／嘲讽` 的规则／机制字段定向定位，并回读以下五条规则、实践、版本与限制字段。另核对现行核心文档的满蓝自动技能／命中回蓝范围及 M04 决定；现行未合并差异仍按讨论边界处理，没有检查或改动实现。

| 原库证据 | 对当前内容的支撑与限制 |
| --- | --- |
| `ev-girls-of-the-tower-008-historical-nine-stone-build` | 历史九石化由受击积 Guard、阈值后按物理＋魔法防御造成周围伤害，支持“次数决定频率、防御决定强度”的分离；后续改版，不能说成当前强度或逐次单体反伤原型。 |
| `ev-dwarves-glory-death-loot-010-thorns-reader-chain` | 基础 Thorns、扩大回应类型的 Thornward、周期范围兑现的 Crown 由不同内容承载，说明触发与覆盖须明确；不移植其近身限制或额外供盾／流血。 |
| `ev-siralim-ultimate-013-paladin-defense-retribution-build` | Retribution 读实际攻击／法术承伤，其他效果才追加最高属性或防御转生命；本稿按当前防御逐次反伤，与实际掉血比例返伤不同。不能以该作证明护盾命中应怎样算。 |
| `ev-tnt-010-fixed-pve-targeting-and-objects` | 1.2 明确嘲讽可覆盖特殊筛选技能，提示需要明确覆盖范围；本稿尚未采用所有技能都被嘲讽覆盖，也不补成通用技能打断。 |
| `ev-ggm-013-taunt-aoe-shaman-balance-chain` | 1.034 修近距离嘲讽的远程合法位置，并调整嘲讽／AOE 等，支持嘲讽改目标仍需合法射程、集中压力会放大组合收益；不照搬范围／时长或声称本稿已经平衡。 |

HC03 的核心来自用户指定，当前防御读数、近远程普攻回应、护盾承伤仍触发、反伤不递归等均为 Agent 项目细化；未冒充原作共同规则或用户逐项采用。没有新联网、动态取证、制作或试玩；仅复用旧 V01 证据，不恢复暂停样例。HC02 的负面评价按用户原意记录，具体不满意原因未给出，不由研究代为推定。

## 2.3-Q01 被动与培养核心：定向回读

当前补记：2.3-D03 已暂用 HC01 无上限被动、U01 升阶后换目标保层及发箭随攻速变化；U02 当前不使用。主动 6 箭只是旧表达用暂值，原作 Rubela 六段不构成项目箭数依据。HC01-A01 的 3 箭和普攻间隔 1/2 为 Agent 新细化建议，未获采用；本次复用 BC01／BC06 及已有攻速／主动节奏区别记录，没有新增研究或平衡证据。

后续范围更新：用户以 [2.3-D02](decisions.md)明确英雄自身能力不考虑跨战，并提出升阶可改变换目标清层、保留上限时提高上限。PB03 已退出英雄能力候选，以下跨战原作证据保留研究用途；HC01-U01／U02 来自用户提议，不是新增原作检索或已采用的完整技能。本次范围更新未补查新研究。

2026-09-11。由用户对 HC01 有上限／切目标清层的反馈进入本题，讨论见 [当前内容稿中的 PB01–PB09](content-drafts.md)。沿已有 BC01、BC06、BC07、BC10、BC12、BC16–BC18、BC20、BC22、BC29、BC34 的全库覆盖复用材料；本轮定向回读下列十条记录的规则、实践、机制与限制字段，以及 Astronarch 档案的成长队和培养分工。辅助检索原库及 The Last Flame／TFT 档案的 `same target`、`same enemy`、`attack speed`、`stack`、`permanent`、`Guinsoo`、`Rageblade`、`羊刀`、`uncapped`、`infinite` 等关系词。没有联网、修改原库或重做全量档案研究；检索到永久／无限字样不自动证明当前版本真正无上限。

| 既有证据 | 本题可支撑的关系与保留限制 |
| --- | --- |
| `ev-tlf-005-karina-shield-to-crit` | Karina 将当前盾转成暴击机会，由攻击、攻速、暴伤等继续放大，支持“转换也能成为核心”。2025 单人挑战有重复被动和 Omnicrit 条件，不能作为普通队伍数值或所有英雄无上限证明。 |
| `ev-guildrun-007-kai-shield-feedback-build` | Kai 独立受盾事件增加攻击，Shield Power 另外读取当前盾量，支持事件次数与当前存量两种不同输入。0.5.2 攻略／0.5.6 修复有版本限制；不借 permanent 一词补定项目跨战持久范围。 |
| `ev-astro-007-frost-cap-cashout-rework` | v1.3.3 的 Frost 上限由 5 到 10、每层减半、总效果未翻倍，Avalanche 改为满 10 层消费。支持积累空间与达到阈值后的作用应一起设计，不是直接替 HC01 定十层或确认无上限。 |
| `ev-astro-008-attack-active-cadence-separation` | 普攻间隔与 MP／主动节奏有不同投入，历史还修正速度上限显示。不能将层数无上限、动作无限加速和技能逐段回蓝混为一谈；不迁移其具体数值。 |
| `ev-dota-underlords-011-build-brawny-three-star` | 联盟击杀账转最大生命，有早期积累、召唤击杀权重与独立 AOE 输出者；为跨战成长的取得时间与输出归属提供参照，不支持假设个人猎手加攻击已经是原作规则。 |
| `ev-vivid-005-fool-shock-rubela-build` | 状态供给者与多段兑现者分工，支持 HC02 不自我叠属性也能成为配合支点；历史版本、额外行动与永久符号不自动迁移。 |
| `ev-skull-horde-012-sharpshooter-crit-module` | 暴击、攻击冷却重置和限定处决分别由具体模块承载；可支持事件／属性投入，不证明本项目应有无限额外攻击或所有 Boss 可处决。 |
| `ev-skull-horde-013-blood-feast-module` | 满血伤害、回血、过疗生成身体由不同所有者承担，支持维持状态与事件转换的被动方向；官方内容说明不等于最佳构筑或已验证强度。 |
| `ev-sap-006-horse-turkey-fly-summon-build` | 死亡生成、强化新召唤和后续补员职责不同，支持不自我积累的支撑者；线性五格和原作替换流程不照搬。 |
| `ev-sap-007-hedgehog-puffer-hurt-build` | 受伤触发还击与减伤支撑，支持“反复兑现效果”区别于“反复永久加属性”；具体触发次数、当前强度未独立测量。 |

用户在《将熄之焰》的相似被动体验按用户反馈记录；本轮没有确认对应英雄或版本。云顶羊刀仅按用户提出的“攻击加速、继续促进攻击”关系讨论，没有取得可用于确认当前层数／攻速上限的证据，因此不声称其所有版本都绝对无上限。PB01–PB09 的归纳、项目假设与 HC01-P01／P02 均是 Agent 设计分析，不是原作完整技能复刻或用户采用。PB09 仅复用已整理 BC29 的攻击入口与覆盖关系，不额外声称本轮独立核验了其原作被动归属。

## 2.3 首批英雄初稿的素材复用

2026-09-11，按用户允许不完整并持续迭代的 [2.3-D01](decisions.md)，为 [HC01 连弩手／HC02 引雷师](content-drafts.md)复用 BC01 与 BC06，定向回读原库中的 `ev-dota-underlords-018-hunter-cadence-item-triggers`、`ev-vivid-005-fool-shock-rubela-build`、`ev-shf-005-bleed-swarm-reader-build` 三条完整记录。没有重跑全库检索或联网；既有 37 项比较与未选择的相关分支保留。

- Underlords 素材说明攻击频率可放大持有者命中效果及原作法力循环；不证明项目技能每段都应作为完整普攻或复制所有触发。HC01 的同目标攻速、切目标清层及连射细则为项目新设计。
- Vivid Knight 素材说明 Topaz 提供 Shock、Rubela 六段技能反复读取，原作另有 Fool／Reno 等供给。HC02 只借用状态供应与多段收益的关系；没有移植其永久符号、完整阵容、全场状态、原作伤害归属或版本强度。
- ShapeHero Factory 素材保留少量施加者配多名攻击者的开放组织方式；Bleed 完整公式、穿盾与伤害归属证据不足，不用于补定项目规则。
- HC01／HC02 的职业名、主动、被动、标记时长、层数、目标关系与触发限制均属 Agent 草稿；暂定数值只帮助表达，不是研究结论、用户采用、实际平衡或实现验证。

## 2.2-Q01 检索边界与实际覆盖

日期：2026-09-11。问题是哪些不同的战斗发动、状态兑现、单位关系和局内成长机制值得成为首版的完整构筑路径；不决定技能数值或正式英雄名单，不重开已定通用系统，也不继续 R10 外层选择。

- 全库入口：[66 款候选名册](../../web/game-mechanics-atlas/research/deep/candidate-roster.json)、[深记录](../../web/game-mechanics-atlas/research/deep/mechanic-evidence.json)、[56 份档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/)、[106 条发现记录](../../web/game-mechanics-atlas/research/discovery/mechanic-evidence.json)、[来源索引](../../web/game-mechanics-atlas/research/deep/source-index.md)。深记录共 780 条，来自 42 款有深证据的游戏；候选数不等于完整调研数。
- 对 780 条记录先按 domain 的 build／engine／conversion／reader／payoff／amplifier／snowball／inheritance／trigger-chain／scaling／archetype 筛得 167 条；补 attack／cast／crit／bleed／burn／poison／frost／shock／heal／shield／control／death／summon／copy／population／formation／retali／cadence／trigger，再得不重复 138 条。
- 对其余记录的 mechanism／rule_support／practical_support 扩查 critical／multicast／duplicate／copy／poison／bleed／burn／frost／frozen／damage／attack／summon／heal／battle-start／shop-stat／kill-growth 等，得到 225 条宽候选；合计 530 条只是检索候选，包含大量界面、版本和反例记录，不等于 530 种玩法。
- 取得全量 780 条 id／game／domain／mechanism 索引供关联回查；对条目所需资料回读 **61 条深记录、28 款游戏**的规则、实践、置信度、版本与限制，完整 id 见下表。未逐条全文精读全部 780 条或重新打开所有外部来源。
- 对全部 56 档案查构筑标题／命名构筑，命中 131 行、51 份文件，含“无法成立构筑”标题，不将其算作成功案例。进一步回读 The Last Flame、Astronarch、Tiny Auto Knights、Just King、Gladiator Guild Manager 的相关段落，并复用 V01-RF01–RF10 中已保存的防护转换差异。
- 完整查看发现层 106 条的机制索引；其中 e059 等元素先后关系仍是低置信线索。Auto-Arcana、Auto GUI Battler 等未闭合原作构筑，不能从商店承诺补成已验证路线。形状背包、手动出牌、局外永久战力、同名合成等只保留结构或沿原议题路由。
- 核对上述 61 条引用的 **192 个 source id 均存在**，抽读 8 个关键来源索引块的标题、网址、版本和限制；没有联网或宣称本轮重读 192 篇外部原文。数量不表示覆盖完整性或证据质量。

## BC 条目与来源对应

BC 编号在草案与对话保持一致；BC13-a/b、BC14-a/b/c、BC22-a/b、BC27-a/b/c、BC29-a/b 明列了会改变投入的内部差异。条目是构筑素材，不是全局互斥规则；项目适配与相对成本均是 Agent 推演。

| 编号 | 相关深记录或已回读档案 |
| --- | --- |
| BC01 普攻次数与暴击触发 | `ev-dota-underlords-018-hunter-cadence-item-triggers`；`ev-skull-horde-012-sharpshooter-crit-module` |
| BC02 频繁施放原有主动技能 | `ev-auto-chess-007-build-divinity-water-shaman` |
| BC03 全队共享充能，由特定核心兑现 | `ev-tlf-007-surge-team-battery` |
| BC04 复制或代触发别人的效果 | `ev-storybook-brawl-007-copycat-good-boy-build`；`ev-bpd-005-creature-trigger-build` |
| BC05 叠持续伤害，并延长结算时间 | `ev-bpb-007-venomancer-snake-scythe-build`；`ev-slay-the-spire-011-poison-catalyst-build` |
| BC06 先挂状态，再靠大量命中读取 | `ev-vivid-005-fool-shock-rubela-build`；`ev-shf-005-bleed-swarm-reader-build` |
| BC07 先积累，再消耗状态爆发 | `ev-astro-004-burn-detonation-five-hero-build` |
| BC08 多状态汇集与跨元素触发 | `ev-d100-014-cross-element-trigger-synergies`；[the-last-flame 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/the-last-flame.md) |
| BC09 控制压制，并利用控制窗口 | `ev-tlf-004-no-healer-shock-stun-build` |
| BC10 当前护盾越多，输出越强 | `ev-tlf-005-karina-shield-to-crit`；`ev-guildrun-007-kai-shield-feedback-build` |
| BC11 把生命或防御属性转换为进攻 | [astronarch 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/astronarch.md)；`ev-tlf-006-defense-broadcast-to-payoff` |
| BC12 靠收到护盾的次数成长 | `ev-guildrun-007-kai-shield-feedback-build` |
| BC13 破盾或主动耗盾时兑现 | `ev-auto-chess-015-knight-shield-lifecycle-drift`；`ev-siralim-ultimate-024-barrier-explicit-converter` |
| BC14 受击、格挡或实际承伤转反击 | `ev-dwarves-glory-death-loot-008-block-vengeance-chain`；`ev-dwarves-glory-death-loot-010-thorns-reader-chain`；`ev-siralim-ultimate-013-paladin-defense-retribution-build`；[V01-RF07](first-content-set/evidence.md) |
| BC15 把实际治疗量变成伤害 | `ev-siralim-ultimate-017-cleric-heal-abnegation-build`；`ev-auto-chess-016-ancestor-ally-local-healing-reader` |
| BC16 维持满血或把过量治疗存成收益 | `ev-skull-horde-013-blood-feast-module`；`ev-tlf-005-karina-shield-to-crit` |
| BC17 主动伤害友军，制造受伤触发 | `ev-sap-007-hedgehog-puffer-hurt-build`；`ev-tak-008-warrior-shop-self-damage-conversion` |
| BC18 低血阈值与濒死后的爆发 | `ev-tft-014-primordian-briar-build`；`ev-bpb-006-ashbringer-phoenix-build` |
| BC19 闪避读取，与伤害续航结合 | `ev-neon-auto-party-010-zealot-vest-dodge-conversion`；`ev-mpig-005-assassin-dodge-lifesteal-core` |
| BC20 积累战斗时间或历史次数，后程发力 | [astronarch 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/astronarch.md)；`ev-slay-the-spire-009-frost-blizzard-build` |
| BC21 维持召唤军团，靠存活与攻击获益 | `ev-tlf-008-summon-commitment-and-owners` |
| BC22 友军死亡补员，或把死亡当资源 | `ev-sap-006-horse-turkey-fly-summon-build`；`ev-gods-vs-horrors-007-egypt-summon-poison-build` |
| BC23 让同一角色死亡、返场并反复供给 | `ev-monster-train-012-fire-light-reform-build`；`ev-siralim-ultimate-010-graveborn-death-resurrection-build` |
| BC24 消费友军或召唤物，将投入转给核心 | `ev-monster-train-016-primordium-husk-build` |
| BC25 击杀敌人，再把敌尸变成己方单位 | `ev-skull-horde-007-adam-corpse-build` |
| BC26 夺取存活敌人，削敌并补己 | `ev-mecha-005-arclight-hacker-defense-build` |
| BC27 集中、复制或广播已有属性 | `ev-auto-chess-017-soul-clan-team-stat-to-recipient`；`ev-tlf-006-defense-broadcast-to-payoff`；`ev-hsbg-005-elemental-boost-build` |
| BC28 让区域与聚集成为效果发动条件 | `ev-hdt-003-ea-nightblade-three-by-three-traps`；[gladiator-guild-manager 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/gladiator-guild-manager.md) |
| BC29 通过攻击入口和覆盖改变击杀顺序 | `ev-auto-chess-011-build-assassin-spatial-tempo`；`ev-monster-train-011-awoken-quick-sweep-build` |
| BC30 形态或阶段改变同一状态的用途 | `ev-hdt-008-moon-phase-mark-eclipse-engine`；`ev-slay-the-spire-014-stance-resource-rules`；[just-king 档案](../../web/game-mechanics-atlas/research/deep/game-dossiers/just-king.md) |
| BC31 招募、出售或打出单位事件带来成长 | `ev-sab-002-buy-sell-growth-build`；`ev-hsbg-005-elemental-boost-build`；`ev-hsbg-004-season13-pirate-bounty-build` |
| BC32 保留财富，按当前余额获得战力 | `ev-bpd-003-gold-crit-build` |
| BC33 把临时战斗收益保留下来 | `ev-gods-vs-horrors-009-marduk-tepeyollotl-build` |
| BC34 击杀等战斗成果累积为后续战力 | `ev-dota-underlords-011-build-brawny-three-star` |
| BC35 用人数与品阶安排投入 | `ev-tnt-007-trait-width-tier-and-bridges`；`ev-tnt-006-high-star-population-compression` |
| BC36 用有限燃料换一段强势输出 | `ev-slay-the-spire-017-corruption-exhaust-build` |
| BC37 在后备位提供特定支援 | `ev-guildrun-004-reserve-backup-ownership` |

## 反例、模式与证据限制

- **终局出口不足**：Backpack Battles 官方回顾说明早期虽然有组件，后期只能落到少数武器。依据 ev-bpb-012；这支持检查不同核心的最终输出，不证明多加内容必然解决平衡。
- **敌人考试把所有路线挤成一种**：Magicbook 不同版本玩家报告后期同时需要盾、净化、范围、控制和禁疗，构筑收敛。依据 ev-mba-018；属跨时期观察，没有普遍采用率结论。
- **存在配方不等于供给可达**：STS 攻略对强行追预设牌组有分歧；Backpack Dungeon 生物线可能因孵化太晚而转向。依据 ev-slay-the-spire-025 与 ev-bpd-005。下一步必须回接 R06／R14 资格、本次候选、有限刷新和成长窗口。
- **高投入不等于高回报**：TLF 召唤攻略认为需要多个英雄与配套，不保证比低投入路线更强。依据 ev-tlf-008；原文并非当前胜率结论。
- **自动行为的成本**：原作手动引爆、Reform、换姿态或叠陷阱可以择时，本项目初期不能依赖这些输入。相同效果移入自动技能后，启动、目标与消费时点仍需真实设计，不能认为名字相同便已等价。
- **特殊／历史方法保持边界**：Karina 单人模式、T&T 非正常存读档高星、SBB 已移除循环、旧 Shaman、旧毒／反击配方都不作为当前强度证明；不恢复同名合成、无限重抽或整局失败后续战。
- **不把工程缺陷当新玩法**：状态重入、召唤递归、来源死亡后仍生效、唯一效果重复结算等是设计和后续实现需要处理的边界，不列作玩家必须审批的新系统。实际系统支持程度本轮未检查。

## 回读记录索引

以下是本轮 61 条深记录的可追溯清单。完整规则、实践与相反证据保留在原 JSON；这里仅记定位与版本，不改写调研原库。source id 对应上方统一来源索引。

| 深记录 id | 版本／置信度 | 来源 id |
| --- | --- | --- |
| `ev-bpb-007-venomancer-snake-scythe-build` | Named March 2024 Early Access Venomancer build; not a 1.1.8 strength claim；medium | `src-bpb-ign-reaper-2024`、`src-bpb-dood-classes-2026`、`src-bpb-dood-mechanics-2026` |
| `ev-tlf-004-no-healer-shock-stun-build` | Named June 2024 Early Access No Healer? No Problem! party；high | `src-tlf-steam-synergy-2024`、`src-tlf-steam-indepth-2026`、`src-tlf-gameplay-starter-2025` |
| `ev-tlf-007-surge-team-battery` | Initial-1.0 strategy explanation plus official 1.0/1.0.1 lifecycle；high | `src-tlf-steam-indepth-2026`、`src-tlf-official-1-0`、`src-tlf-official-1-0-1` |
| `ev-hdt-008-moon-phase-mark-eclipse-engine` | 2.0 Moonhunter launch through 2.1.18 content maintenance；high | `src-hdt-official-2-0`、`src-hdt-official-2-1-18` |
| `ev-astro-004-burn-detonation-five-hero-build` | v1.3.5 around 2021-03-02; pre-Fallen；medium | `src-astro-guide-high-corruption`、`src-astro-guide-classes`、`src-astro-official-1-3-3`、`src-astro-official-1-0` |
| `ev-vivid-005-fool-shock-rubela-build` | v1.1.10-v1.1.25 launch-period Amelie；medium | `src-vivid-guide-symbol-tips`、`src-vivid-guide-symbol-list`、`src-vivid-guide-easy-start`、`src-vivid-official-1-1-10` |
| `ev-d100-014-cross-element-trigger-synergies` | 2023-03-24 test concepts confirmed in 2023-04-25 live update；high | `src-d100-official-2023-03-24-test`、`src-d100-official-2023-04-25`、`src-d100-guide-strange-builds` |
| `ev-bpd-005-creature-trigger-build` | Demo 1.5.9 item package plus full 2.3.x route practice；high | `src-bpd-patch-1-5-9`、`src-bpd-discussion-creature-pivot-2026-08-17`、`src-bpd-patch-2-0-2`、`src-bpd-patch-2-0-9` |
| `ev-shf-005-bleed-swarm-reader-build` | 1.1-era 2026 Ascension 9 community practice；medium | `src-shf-discussion-bleed-swarm`、`src-shf-guide-heroes-comment-3363095131`、`src-shf-review-current-loop-217921009` |
| `ev-skull-horde-012-sharpshooter-crit-module` | post-v1.032 2026-08 achievement guide with current review cross-check；medium | `src-skull-guide-achievements`、`src-skull-review-garg-228665998`、`src-skull-review-reroll-report-223246122` |
| `ev-storybook-brawl-007-copycat-good-boy-build` | 67.4-and-later Good Boy text with 2022 positioning practice；high | `src-sbb-official-positioning`、`src-sbb-wiki-copycat`、`src-sbb-wiki-good-boy`、`src-sbb-official-patch-67-4`、`src-sbb-steam-guide-heroes` |
| `ev-dota-underlords-018-hunter-cadence-item-triggers` | New Blood / final Hunter and Troll archive；high | `src-du-wiki-hunter`、`src-du-wiki-items`、`src-du-wiki-mana`、`src-du-wiki-troll` |
| `ev-auto-chess-007-build-divinity-water-shaman` | 2020-12 official hosted guide before 2021 Shaman rework；high | `src-ac-guide-water-shaman`、`src-ac-official-water-spirit-database`、`src-ac-guide-items`、`src-ac-official-shaman-rework` |
| `ev-tft-014-primordian-briar-build` | Set 17 Primordian Reroll page published 2026-04-11 and updated 2026-07-29, cross-checked with a Set 17 reroll guide；medium | `src-tft-flow-primordian-reroll`、`src-tft-emblem-reroll-set17` |
| `ev-sap-006-horse-turkey-fly-summon-build` | 2026 maintained Turtle Pack route with 2022 historical independent counter cross-check；high | `src-sap-tag-builds-2026`、`src-sap-tag-consistency-2026`、`src-sap-screenrant-teams-2022` |
| `ev-sap-007-hedgehog-puffer-hurt-build` | Build guide updated 2026-07-28 with 2022 counter interpretation；medium | `src-sap-tag-builds-2026`、`src-sap-screenrant-teams-2022` |
| `ev-mecha-005-arclight-hacker-defense-build` | Named 1.11-period Arclight Hacker Defense guide；high | `src-mecha-wiki-arclight-hacker-2026`、`src-mecha-wiki-targeting-2026`、`src-mecha-monarch-counters-2026`、`src-mecha-official-1-11` |
| `ev-mpig-005-assassin-dodge-lifesteal-core` | first live week, including a post-duration-fix Nightmare 1-9 video description；medium | `src-mpigdb-passive-lifesteal`、`src-mpigdb-passive-dodge`、`src-mpig-guide-dscan-2026-09-02`、`src-mpig-video-nightmare-1-9-description`、`src-mpig-review-dodge-lifesteal-233844259` |
| `ev-skull-horde-007-adam-corpse-build` | v1.013-v1.032 structure with 2026-08 current guide；high | `src-skull-guide-achievements`、`src-skull-discussion-adam-team`、`src-skull-official-v1-013`、`src-skull-review-three-population-223778112` |
| `ev-skull-horde-013-blood-feast-module` | official Blood Feast update 2026-05-23 through v1.032；high | `src-skull-official-blood-feast`、`src-skull-official-v1-032`、`src-skull-discussion-armor-tooltip` |
| `ev-gods-vs-horrors-007-egypt-summon-poison-build` | formal 1.0 Guide and launch practice; no 1.1 usage claim；high | `src-gvh-guide-strategy-3493071391`、`src-gvh-discussion-poison-builds`、`src-gvh-discussion-demo-egypt`、`src-gvh-review-poison-meta-197930014` |
| `ev-auto-chess-017-soul-clan-team-stat-to-recipient` | S20 2022 with 2025 pool-rewrite boundary；high | `src-ac-official-s20`、`src-ac-official-2025-05-29`、`src-ac-guide-transitions` |
| `ev-neon-auto-party-010-zealot-vest-dodge-conversion` | 2025-04 Demo single-run snapshot with Playtest-era automatic-combat context；high | `src-nap-video-demo-clear-2025`、`src-nap-video-first-playtest-failure-2024` |
| `ev-siralim-ultimate-010-graveborn-death-resurrection-build` | Bargain Bin 2.0/RI2 with maintained rules and 2.0.37 specialization context；high | `src-su-guide-bargain-bin-2-0`、`src-su-wiki-combat`、`src-su-guide-specializations-2-0-37`、`src-su-compendium-2-0` |
| `ev-siralim-ultimate-017-cleric-heal-abnegation-build` | 1.1.1 historical guide separated from 2.0.37 structure and a 2025-09-12 high-investment beginner-accessible ASR recipe；high | `src-su-guide-cleric-ri5-1-1-1`、`src-su-official-patch-1-1-1`、`src-su-guide-specializations-2-0-37`、`src-su-video-cleric-beginner-2-0` |
| `ev-hsbg-004-season13-pirate-bounty-build` | Season 13 / Patch 35.2.2 only；medium | `src-hsbg-fantasywarden-season13`、`src-hsbg-blizzard-intro-2019` |
| `ev-hsbg-005-elemental-boost-build` | Battlegrounds Buddy Patch 250339 current snapshot, accessed 2026-09-02; official Season 14 Patch 36.2.2 cross-check；medium | `src-hsbg-bgbuddy-elemental-boost`、`src-hsbg-bgbuddy-unleashed-mana-surge`、`src-hsbg-bgbuddy-moat-custodian`、`src-hsbg-bgbuddy-unbound-tempest`、`src-hsbg-bgbuddy-refreshing-anomaly`、`src-hsbg-blizzard-patch-36-2-2` |
| `ev-bpb-006-ashbringer-phoenix-build` | Named March 2024 Early Access Ashbringer build; not a 1.1.8 strength claim；medium | `src-bpb-ign-pyromancer-2024`、`src-bpb-dood-classes-2026`、`src-bpb-official-1-1` |
| `ev-hdt-003-ea-nightblade-three-by-three-traps` | September 2021 Early Access with 1.1 Permatrap and 2.0 occupancy cross-checks；medium | `src-hdt-steam-favorite-builds-2021`、`src-hdt-steam-favorite-builds-2024`、`src-hdt-official-1-1`、`src-hdt-official-2-0` |
| `ev-tak-008-warrior-shop-self-damage-conversion` | 1.0 community example with 1.2.0 item-pool change；medium | `src-tak-thread-self-damage`、`src-tak-thread-buff-persistence`、`src-tak-official-120`、`src-tak-official-121` |
| `ev-sab-002-buy-sell-growth-build` | 1.3.0 rules with 2023 player practice；high | `src-sab-official-html5-client-130`、`src-sab-official-130`、`src-sab-itch-buy-sell-build-2023-07-30`、`src-sab-review-kona-2021-12-29` |
| `ev-bpd-003-gold-crit-build` | full version 2.0.1-period player clear；high | `src-bpd-review-spatial-combos-2026-07-16`、`src-bpd-review-loop-combos-counter-2026-07-17`、`src-bpd-review-balance-convergence-2026-07-17` |
| `ev-gods-vs-horrors-009-marduk-tepeyollotl-build` | formal 1.0 developer example and Guide; no 1.1 current-meta claim；high | `src-gvh-guide-strategy-3493071391`、`src-gvh-discussion-build-variety`、`src-gvh-review-economy-194729657`、`src-gvh-discussion-infinite-essence` |
| `ev-storybook-brawl-009-trophy-grim-soul-build` | 65.10 Grim Soul return through its 70.6 removal；high | `src-sbb-official-patch-65-10`、`src-sbb-official-patch-70-6`、`src-sbb-wiki-slay`、`src-sbb-steam-guide-heroes`、`src-sbb-discussion-trophy-overflow` |
| `ev-monster-train-012-fire-light-reform-build` | Friends & Foes through a TLD-era logged TRUE run；high | `src-mt-wiki-little-fade`、`src-mt-guide-melting-remnant`、`src-mt-discussion-little-fade`、`src-mt-winning-runs`、`src-mt-official-friends-foes` |
| `ev-monster-train-014-corruptor-bog-chrysalis-build` | 2.0 launch through 2.2 rules；high | `src-mt-wiki-spine-chief`、`src-mt-wiki-bog-chrysalis`、`src-mt-discussion-wurmkin`、`src-mt-winning-runs`、`src-mt-wiki-pact-shards` |
| `ev-dota-underlords-011-build-brawny-three-star` | 2020-11-19 final balance plus 2020-12 guide；high | `src-du-guide-final-meta`、`src-du-wiki-brawny`、`src-du-official-pudgy`、`src-du-community-brawny-warrior`、`src-du-wiki-heroes-pool` |
| `ev-auto-chess-011-build-assassin-spatial-tempo` | 2019 mobile with 2023 historical cross-check；medium | `src-ac-guide-assassin`、`src-ac-guide-positioning`、`src-ac-guide-steam-2023` |
| `ev-bpb-012-lategame-payoff-bottleneck` | Official 1.0 retrospective and 1.1 expansion；high | `src-bpb-official-1-0`、`src-bpb-official-1-1` |
| `ev-tlf-005-karina-shield-to-crit` | Named April 2025 1.x Karina One Man Army speedrun build；medium | `src-tlf-steam-karina-2025`、`src-tlf-gameplay-starter-2025`、`src-tlf-steam-indepth-2026` |
| `ev-tlf-006-defense-broadcast-to-payoff` | Early Access and initial-1.0 guides; exact item values excluded；high | `src-tlf-steam-indepth-2026`、`src-tlf-gameplay-starter-2025`、`src-tlf-steam-endless-2024`、`src-tlf-steam-karina-2025` |
| `ev-tlf-008-summon-commitment-and-owners` | Initial-1.0 strategy guide and 1.0 hero reference；medium | `src-tlf-steam-indepth-2026`、`src-tlf-gameplay-starter-2025`、`src-tlf-steam-synergy-2024` |
| `ev-mba-018-late-build-convergence` | launch Endless observations through 2026 post-public-patch review；medium | `src-mba-thread-endless-setups`、`src-mba-review-role-convergence-2025-03-31`、`src-mba-review-late-convergence-2026-02-16`、`src-mba-official-status-contract-rework-2025-04-25`、`src-mba-official-synergy-20-2025-05-18` |
| `ev-guildrun-007-kai-shield-feedback-build` | 0.5.2 build corrected by 0.5.6 Shield Power fix；high | `src-guildrun-grind-endless-guide-0-5-2`、`src-guildrun-wiki-rank-modifiers-0-5-6`、`src-guildrun-patch-0-5-2`、`src-guildrun-patch-0-5-6`、`src-guildrun-discussion-mystics-2026-08-23` |
| `ev-dwarves-glory-death-loot-008-block-vengeance-chain` | v2.0.10 maintained rules and developer/community clarification；high | `src-dgdl-companion-stats`、`src-dgdl-companion-items`、`src-dgdl-discussion-block-cap`、`src-dgdl-discussion-thorns` |
| `ev-dwarves-glory-death-loot-010-thorns-reader-chain` | v2.0.x maintained item rules and developer/community clarification；high | `src-dgdl-companion-items`、`src-dgdl-discussion-thorns`、`src-dgdl-discussion-forge-demon` |
| `ev-slay-the-spire-025-archetype-forcing-failure` | EA/Ascension-15 history through a standard-run guide published 2025 and updated 2026；medium | `src-sts-guide-defect-lightning-standard`、`src-sts-guide-foundation`、`src-sts-guide-silent-style`、`src-sts-guide-ten-ways`、`src-sts-guide-defect-builds` |
| `ev-auto-chess-015-knight-shield-lifecycle-drift` | 2019/2020 Knight shield through 2025 mitigation rewrite；high | `src-ac-official-knight-database`、`src-ac-guide-unit-types`、`src-ac-official-civet-s13`、`src-ac-official-s16`、`src-ac-official-2025-03` |
| `ev-tnt-004-four-dragon-four-noble-party` | Early-1.0 Ladder 15 route cross-referenced to 1.4.3 roster and trait rules；high | `src-tnt-steam-ladder15-2024`、`src-tnt-steam-traits-1-4-3`、`src-tnt-official-levelup-2024` |
| `ev-tnt-006-high-star-population-compression` | 2.0 launch player route and official Uber Item design；medium | `src-tnt-steam-brutal5-carry-2026`、`src-tnt-official-2-0`、`src-tnt-steam-army-size-2026` |
| `ev-tnt-007-trait-width-tier-and-bridges` | 1.0.61 community comparison plus 1.4.3 trait guide；medium | `src-tnt-steam-upgrade-replace-2025`、`src-tnt-steam-comp-building-2025`、`src-tnt-steam-traits-1-4-3` |
| `ev-guildrun-004-reserve-backup-ownership` | Demo 0.5.5–0.5.6 active-board/reserve rules；high | `src-guildrun-wiki-economy-0-5-6`、`src-guildrun-wiki-rank-modifiers-0-5-6`、`src-guildrun-discussion-mystics-2026-08-23`、`src-guildrun-review-position-backup-2026-07-19` |
| `ev-slay-the-spire-009-frost-blizzard-build` | 2.x rules with historical Frost practice；high | `src-sts-wiki-blizzard`、`src-sts-wiki-loop`、`src-sts-wiki-capacitor`、`src-sts-guide-defect-builds` |
| `ev-slay-the-spire-011-poison-catalyst-build` | 2.x rules with cross-era Poison guidance；high | `src-sts-wiki-poison`、`src-sts-wiki-catalyst`、`src-sts-guide-poison-defense`、`src-sts-guide-silent-style`、`src-sts-official-poison-cap` |
| `ev-slay-the-spire-014-stance-resource-rules` | 2.x Watcher stance rules with launch practice；high | `src-sts-wiki-stance`、`src-sts-wiki-energy`、`src-sts-wiki-rushdown`、`src-sts-guide-watcher-archetypes`、`src-sts-official-2-0` |
| `ev-monster-train-016-primordium-husk-build` | 2.x exiled Umbra and TLD；high | `src-mt-wiki-primordium`、`src-mt-wiki-husk-hermit`、`src-mt-wiki-status-effects`、`src-mt-winning-runs`、`src-mt-discussion-emberdrain` |
| `ev-auto-chess-016-ancestor-ally-local-healing-reader` | 2025-05 Ancestor update；high | `src-ac-official-2025-05-29`、`src-ac-official-s20`、`src-ac-guide-items` |
| `ev-slay-the-spire-017-corruption-exhaust-build` | 2.x Exhaust rules and 2.2+ Ironclad A20 practice；high | `src-sts-wiki-exhaust`、`src-sts-wiki-corruption`、`src-sts-wiki-feel-no-pain`、`src-sts-wiki-dark-embrace`、`src-sts-guide-ironclad-a20` |
| `ev-monster-train-011-awoken-quick-sweep-build` | 2.x rules with launch/TLD practice；high | `src-mt-wiki-husk-hermit`、`src-mt-wiki-status-effects`、`src-mt-guide-useful-tactics`、`src-mt-review-scaling`、`src-mt-winning-runs` |
| `ev-siralim-ultimate-013-paladin-defense-retribution-build` | Bargain Bin 2.0 practice with maintained combat, specialization and owner cross-checks；high | `src-su-guide-bargain-bin-2-0`、`src-su-wiki-combat`、`src-su-guide-specializations-2-0-37`、`src-su-compendium-2-0` |
| `ev-siralim-ultimate-024-barrier-explicit-converter` | 2.0-era maintained Barrier/owner data and two community-tested interactions；high | `src-su-discussion-barrier-removal`、`src-su-discussion-barrier-health-damage`、`src-su-wiki-buffs`、`src-su-compendium-2-0` |

## 其他命中的去向

2026-09-11 后续用户以“可以 先做这五种”采用五类首批覆盖，见 [2.2-D01](decisions.md)。本轮 2.3 的职责、成长、替代与敌人考验表复用上文 BC 机制、既有决定及已记录反例，未新增机制检索；属于项目配套推演，未把它们归因于原作已有完整相同方案。F01 主要关联 BC01／BC06，F02 关联 BC02／BC07／BC20，F03 关联 BC10／BC12／BC14，F04 关联 BC21／BC22，F05 关联 BC31／BC32／BC34；这是素材路由，不代表所有对应子机制获采用。

一般招募／刷新／金币／人口成长归 R05–R09，外层经营载体归暂缓 R10，失败与解锁归 R11／R14，具体敌人／反制角色归 2.3，触发来源及状态规则归 M03／M04 与后续实施契约。普通坦克—治疗—输出分工、单核／双核、羁绊标签和元素名称是覆盖维度，不能仅换名称就算新增发动机制；BC35 保留人数／品阶读取的具体差异。界面与报告只为归因及成本提供证据，不新增本轮制作范围。
