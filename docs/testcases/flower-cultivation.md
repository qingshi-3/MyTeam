# 花灵治疗与定向培养首步

2026-10-06，承接首批成长设计后的“继续”。仅接入U16治疗重做及固定双属性培养，保持12人／8羁绊与现有供给；古树、铭记和完整13人方案未实施。玩家行为归[能力说明](../../gameplay-design/ability-prototype.md)与[征程规则](../../gameplay-design/expedition-progression.md#花灵定向培养)。

## 已取得证据

全部输出位于 `outputs/flower-cultivation-20261006/`。主Agent统一低并发构建与运行；两名GPT-6.1 Sol（high）分别实现成长／治疗，GPT-6 Astra（high）只读审查冻结、双属性保存、旧档拒绝和表现事件回滚，未发现阻塞项。最终构建为0警告、0错误。

- `heal-contract.log`：`FLOWER_HEALING_CONTRACT_OK`。正式U16资源、无合法受伤目标不耗蓝／不发施法盾、范围与存活资格、最低生命比例／稳定同值排序、自疗、3步蓄势、锁定目标移动、过量治疗、硬控取消、施法失败回滚、森林副本独立法力／冷却和同种子确定性通过。
- `growth-contract.log`：`FLOWER_CULTIVATION_CONTRACT_OK`。固定4／40同笔账本、未分配／后备／新人无追溯、冻结后改投不改当次、可培养自己、无旧4%重复、失败保存整笔回滚、精确恢复／去重、非法配置和旧内容指纹拒绝且原文件不变通过。
- 同一日志中的 `FLOWER_CULTIVATION_BATTLE_BOUNDARIES_OK`：普通付费非终局败战、前两个Boss胜利各一次4／40；最终Boss零产出。普通败战和Boss结果使用正式模拟器配置生成的受控战果、来源身份及遗物receipt，通过真实Run冻结／ResolveBattle／保存入口；它们是事务证据，不是自然胜负或强度证据。
- **正式第三Boss部署仍阻塞。** `FLOWER_FINAL_AUTHORED_BOSS_BLOCKED`明确记录原错误“敌方部署区无法容纳本场大型单位与同伴”。仅该错误且正式状态未变时，测试复制最终整备状态到独立内存profile，并以第一Boss已合法的3人组成替换最终测试敌阵，保留终局策略验证零生产／清档。没有修改正式资源，也不能宣布正式第三Boss可战或已自然通关。
- `prototype-contract.log`：`ABILITY_PROTOTYPE_CONTRACT_OK`及`PROTOTYPE_RULES_OK`，四个预设同种子结果一致、全部12人实际施法、现有8羁绊相关规则通过。Matrix有效治疗现在补发既有`heal`表现事件，包含此类治疗的摘要会相应改变；不主张与缺少事件的旧摘要相同。
- `pve-compatibility.log`：`EXPEDITION_PVE_CALIBRATION_OK battles=1`。保留既有零永久成长的固定人数工具，允许当前U16生产定义但不模拟行动。`forest_shape`两人、`ember_combat_profile`、种子20261006局部样本可运行，13.1秒胜利、2人存活。这只补验工具接入兼容，没有重跑全部144个样本或作成长强度对照。

## 实机实际输入

`input-final.log`的`FLOWER_CULTIVATION_INPUT_OK`已通过。960×540宿主、1600×900隔离SubViewport，实际按钮／键盘事件，未移动桌面鼠标、注入候选、直接调用生产命令或制造战果：

1. 从正式新征程的自然六选二取得花灵与炉喉火蝎；有界重新开局只为取得本次可测队伍，不用于估算出现率。
2. 打开军团工坊，确认默认选项只是预览；未保存分配，正常服务提交后两人永久收益仍为零。
3. 用实际下拉键盘选择与按钮保存对象；取消敌情整备预览不改存档，随后按正常1倍速度开战。
4. 实际普通战19.3秒获胜；花灵3次有效治疗共145.72583，火蝎战后得到一次4攻击／40生命，花灵未误领。详见 `input-final/flower-battle-result.json`。
5. 查看绑定的双属性账本，实际改投花灵自己；重新创建正式根并继续，完整存档JSON相等，旧对象已有收益保留，新分配恢复，不重复发放。

对应 `input-final/evidence.json`、未分配／已分配／改投／继续截图及4秒原始战斗帧均保留。该4秒片段主要覆盖入场，不作为治疗特效清晰度或最终美术验收。首次输入驱动在鼠标展开的下拉菜单之后未能关闭工坊；改用已有的键盘展开路径并显式检查菜单收起后通过，生产UI关闭逻辑未修改。初次治疗检查发现Matrix治疗缺表现事件，修复通用Heal分支后严格原断言通过；没有为通过测试放宽治疗判定。

首轮账本截图停在滚动区域顶部，未完整展示累计量，因此接续同一隔离存档做了局部补验，未重跑战斗：`ledger.log`的`FLOWER_CULTIVATION_LEDGER_INPUT_OK`已通过，实际滚轮露出完整账本，`ledger/flower-ledger-visible.png`可见火蝎“攻击 +4 · 生命 +40”，关闭后完整存档不变。使用 `--flower-ledger-only --journey-namespace=tests/expedition-journey/8c34c959af0547a4a842b8635c19bc1b` 可接续这份本机隔离证据。

## 复现与观察

设置 `DOTNET_ROOT=/Users/qingshi/.dotnet`，引擎为 `/Users/qingshi/Downloads/Godot_mono.app/Contents/MacOS/Godot`。逻辑入口使用 `--headless --path . --scene tests/FlowerHealingContractSmoke.tscn` 或 `tests/FlowerCultivationContractSmoke.tscn`。正常输入入口去掉 `--headless`，使用 `--scene tests/ExpeditionJourneyInputSmoke.tscn -- --flower-only --output=res://outputs/flower-cultivation-review`；只有显式成功marker及证据JSON成功才记为通过，不能仅凭进程退出码。

试玩从新征程取得花灵后，打开「军团→成长工坊」，选对象并保存，让双方上阵，完成一次服务或非终局战斗，检查累计属性。随后改投副核，观察旧主力保留成长、下一次只投新对象。旧内容指纹的活动局会被拒绝并保留原文件；新开局成功仍按原规则替换活动局，本批没有兼容迁移。

本批培养只在行动结算处理固定分配，不增加逐帧永久成长采集；常规征程继续关闭详细战况采集。没有新增性能基准或证明所有规模的开销上限。服务临时换入花灵的成本、治疗过量比例、成长与即战力的阶段比较，以及培养是否有期待感仍待后续对照和用户体验确认。
