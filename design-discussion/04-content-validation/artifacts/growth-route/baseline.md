# 当前正式征程与 MX 职业属性基线

本目录记录 **2026-09-27 当前实现事实**，用于后续成长路线设计的输入；它不是未来方案、目标曲线或平衡验收。结构化数据见 [current-baseline.json](current-baseline.json)。

## 征程机会边界

正式征程有三个区域、每区五层，共十五层；区域及每区层数见 `content/project/alpha_campaign.tres:4-16`，区域名称见 `content/tower/region_ember_foundry.tres:10-14`、`region_gloam_crypt.tres:10-14`、`region_crown_engine.tres:10-14`。每区第 5 层固定 Boss；前四层每层提供三个互斥选项。节点轮转源为 `content/project/tower_node_table.tres:51-54`，取三项、步长 2、Boss 层号 4 见 `src/Project/TowerNodeTableDefinition.cs:10-13`，实际选择算法见 `src/Run/TowerGenerator.cs:14-33`。

因此每区各有两层提供“普通战斗／事件／商店”三选一，另两层提供“招募／精英／营火”三选一。整局招募、商店、事件、营火、普通战斗、精英各被提供 6 次，但同层只能选一个；再加 3 场强制 Boss，实际战斗数为 **3～15 场**。招募、商店、事件、营火可不战推进；普通／精英失败扣全局生命后仍推进，Boss 失败直接终局，见 `src/Run/RunNodeResolutionService.cs:128-162` 与 `src/Run/RunHealthPolicy.cs:16-21`。

## 经济、生命与招募

运行规则默认值定义于 `src/Project/RunRulesDefinition.cs:22-40`：开局 16 金，普通／精英／Boss 胜利为 7／12／18 金；事件为 65% 获得 18 金或稳妥获得 6 金；营火为恢复 25 全局生命或获得 8 金；招募、商店、战利品均展示 3 个候选。当前资源明确覆盖全局生命 100、普通败扣 20、精英败扣 30，见 `content/project/alpha_run_rules.tres:16-20`。商店物品价为 10～34 金；战斗胜利产生免费三选一物品，最终 Boss 胜利直接终局，不再建立后续奖励选择。默认选择的具体构造见 `src/Project/RunOfferDefaults.cs:38-85`。

开局从 6 名候选中选 2 名，常量见 `src/Project/CompiledRecruitmentSupply.cs:14-15`。四阶段的阶位权重分别为 0～2 层 `65/35/0/0`、3～5 层 `35/45/20/0`、6～9 层 `15/30/45/10`、10～14 层 `0/15/50/35`，见 `content/project/alpha_recruitment_supply.tres:7-47`。

## MX 职业基准与减伤

49 名 MX 按职业使用统一基础模板：护卫 `400/10/0/8/0.667`、斗士 `300/20/0/4/0.769`、射手 `175/22/0/1/0.833`、突袭者 `200/24/0/1/0.909`、术士 `185/12/0/1/0.625`、祝祷者 `235/12/0/2/0.667`、统御者 `210/10/0/2/0.667`；顺序为生命／攻击／法强／防御／每秒攻击。代表资源字段见 `content/validation_matrix/definitions/hero_mx01.tres:26-29`，其余职业代表为 MX02、MX03、MX08、MX04、MX05、MX15 的同位置字段。

所有 MX 均未显式写入 `SpellPower`，因此使用 `src/Content/UnitDefinition.cs:40` 的默认值 0；这只是当前验证包事实，不能视为后续法强成长预算。普通伤害减伤为 `max(1, raw × 100 / (100 + armor × 7))`；真实伤害绕过护甲，但仍经过共享修正和护盾，见 `src/Battle/BattleSimulation.cs:1550-1561`。

当前异常与限制：职业内属性完全同模；最短三战路线尚未经过经济或通关验证；当前默认营火使用全局生命／金币选择，资源中保留的旧英雄、士兵恢复字段不代表现行营火行为。
