# 默认主流程、独立奖励与战前抓手运行证据

日期：2026-09-30。执行范围及恢复见[活动任务](../../../../../../work-items/active/opponent-choice-and-prebattle-spells.md)。正式规则见[玩法](../../../../../../gameplay-design/tower-autobattler-core.md)与[系统契约](../../../../../../system-design/tower-autobattler-architecture.md)。本记录证明具体机制和操作，未代表新外层主循环、任意开局供给、全路线平衡或用户体验认可。

## 本次实际执行

构建命令：`dotnet build my-team.csproj -maxcpucount:2 -v:minimal`，最终0警告、0错误。Godot为4.7.2 .NET，路径`/Users/qingshi/Downloads/Godot_mono.app/Contents/MacOS/Godot`。新环境贴图加入后执行一次`Godot --headless --editor --path . --import`，成功导入后重做受影响的渲染输入，不反复进行无关导入。

无头入口：`Godot --headless --path . tests/<入口>.tscn`。

| 入口 | 本次实际结果 |
| --- | --- |
| OpponentChoiceContractSmoke | OPPONENT_CHOICE_CONTRACT_OK；精英低频选择、新普通直接整备／旧ID拒绝、稳定候选／收据／胜败／回滚，以及v8/v9普通旧承诺／冻结法术恢复 |
| GrowthRewardGroupsContractSmoke | GROWTH_REWARD_GROUPS_CONTRACT_OK；基础收益与全队列原子胜利、分类独立领取／跳过、回滚／重复、部分领取读档、旧混合资格保留、唯一遗物、现货一次消费与非法队列 |
| GrowthRunContractSmoke | GROWTH_RUN_CONTRACT_OK；四法术费用／冻结／消费、生产／永久收益／深拷贝／迁移；新多奖励组全部处理后再投影下一战 |
| RecruitmentUniquenessContractSmoke | RECRUITMENT_UNIQUENESS_CONTRACT_OK；原招募唯一／候选不足／分支／回滚，以及遗物唯一和已售现货不再回购 |
| RunHealthContractSmoke | RUN_HEALTH_CONTRACT_OK；普通／精英／Boss生命、收益、重复、回滚、迁移与终局恢复 |

四法术真实作用的`GrowthBattleGainSmoke`沿首版通过证据，本次未改能力或重复运行该测试。

渲染输入命令：`Godot --path . --windowed --resolution 960x540 --position=-4000,-4000 tests/GrowthEntryInputSmoke.tscn -- --isolated-pointer`。

最终输出：`GROWTH_ENTRY_INPUT_OK default-menu opening ordinary-direct real-first-battle independent-rewards resume natural-research elite-choice spell-craft second-battle-consume isolated-save viewport=960x540`。输入送入隔离SubViewport，按钮／OptionButton使用真实鼠标事件或键盘，弹出菜单嵌入视口，不移动用户桌面鼠标。

实际路径：从ProjectSettings默认主场景启动，检查默认菜单／工具展开和真实新局成长六选二；随后用同一正式开局命令生成seed2的合法开局档，真实继续选择构装工坊与破盾卫士。普通节点直接整备、首场真实自动战斗获胜；装备独立领取，遗物组中途重载保持原候选／已领装备／金币，完成遗物领取；休整与安全事件各结算一次，研究自然从0→1→2→3；进入精英比较对手／单位详情，返回重进保持候选，确认与读档锁定同一阵容；兑换压制、预设／取消／重设，开始第二场真实自动战斗，库存仅消耗一次。没有研究、库存、属性或战果注入，正式growth-journey存档前后内容一致。

该固定种子路线只证明存在真实生产与法术路径，不保证每个随机开局都能此时兑换法术。第二场只检查真实开战和正常速度运行，未把它写成胜利或整局通关。

## 当前画面

- [默认主菜单](main-menu.png)：开始新征程直接进入成长，旧版兼容入口收进工具。
- [独立装备奖励](equipment-reward.png)、[独立遗物奖励](relic-reward.png)：名称和首行领取按钮完整可见，跳过当前组的按钮独立。
- [精英候选与敌军详情](opponent-choice.png)：选择仅发生在精英；右侧长内容可滚动，页脚确认完整位于视口。
- [自然研究兑换与预设](spell-preset.png)：3研究兑换压制后研究0／库存1，预设目标工蜂监工，取消不丢库存。
- [第二场自动战斗](battle-start.png)：正式盾阵贯射敌阵，库存0，无战中指令HUD；真实内容沿正式表现播放路径。

截图包含工程并行进行的场景／材质改动，本批不将其归为自己制作或用户认可，不以场景背景作为美术验收。

## 修复与限制

本批修默认入口仍为Alpha、装备遗物共抢名额、每场普通战都选敌；补遗物唯一与商店库存消费。v10只对新战后生成独立组，旧待决项／已锁对手保持承诺。领奖页缩短重复留白与非单位卡体下限，滚动从内容起点开始；选敌右侧加预制滚动容器，使长文本不顶出页脚。

一次最终构建被工程新加入的BattleEnvironment中Control不存在的ToLocal阻断，局部改为CanvasItem全局变换逆映射。随后渲染遇到同批新courtyard.png未导入，终止该唯一测试进程、清理其独立seed2存档并补导入，最终输入复验通过。未改其场景构图、贴图或玩法判定。

成长包退出仍打印2个ObjectDB／Resource清理提示，尚未归因，不宣称退出完全无警告。用户指出的三选节点→单战外层替代形式仍待R10回接；Boss暂无完整备选。法术／奖励供给频率、全路线胜率、研究留存取舍和整体观感继续由玩家验收。

## 首版历史

此前首版只在额外成长入口提供普通／精英选敌，并在法术输入夹具中补5研究；它曾通过机制合同与局部输入，但没有完成默认主流程整合。当前截图与输入路径已替换为本次默认入口及自然研究路线，不沿用旧夹具作为自然经济证明。
