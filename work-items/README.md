# Work Items

`active/` contains confirmed, self-contained work that still needs discussion resolution, execution, or independent verification. It records task scope, progress, evidence, and resume state without overriding accepted gameplay or system authority.

`archive/` contains completed historical work and explicitly superseded/stopped work. Archiving does not itself certify implementation or acceptance: `Completed` retains its actual evidence; `Superseded` records its replacement and unresolved work. Genuine pending execution/verification remains active. Historical instructions, scope and acceptance criteria do not override current authority or authorize resuming an obsolete plan.

## 当前入口（2026-09-16）

| 事项 | 入口 | 当前边界 |
| --- | --- | --- |
| 统一伤害与防御 | [active/unified-damage-defense.md](active/unified-damage-defense.md) | 普通伤害统一防御；公式因子保留攻击／法强，魔抗与双类伤害词条已清理。构建、相关行为与真实UI输入／渲染通过；技能系数不变，平衡待试玩。 |
| 军团／瑟提／机器人三英雄 | [active/duel-grit-hook-heroes.md](active/duel-grit-hook-heroes.md) | HC37–HC39独立内容、决斗／怒劲／实体飞钩、正式招募与实验室已接入；专项机制与画面验证见任务，强度和观感待试玩。 |
| 首批贯穿特色兵 | [active/enemy-piercing-skills.md](active/enemy-piercing-skills.md) | ES01飞行箭／ES04光束（现均为统一防御的普通伤害）、独立单位与技能／VFX、有限正式遭遇及地区数值已接入。构建、专项规则、正式渲染和暂停输入检查通过；观感与平衡待试玩。 |
| 开局与分阶招募 | [active/recruitment-opening-and-tiers.md](active/recruitment-opening-and-tiers.md) | 保存式6选2、阶段阶级供给；当前名单以受控池及招募资源为准，后续英雄追加由对应内容任务记录。材料培养、敌人成长与推荐另议。 |
| 英雄信息场景复用 | [active/battle-lab-ui.md](active/battle-lab-ui.md) | 紧凑信息、完整详情与公共数值子场景已接入部署／军团／选英雄；构建、针对性输入与渲染检查通过，范围见任务顶部。 |
| 代码与场景架构检查及解耦 | [archive/architecture-decoupling.md](archive/architecture-decoupling.md) | 三个职责边界已抽取并完成当前内容的编译、启动、输入及生命周期检查；旧冻结名册回归失配单独保留。 |
| 六种位移与空间控制英雄 | [active/hero-displacement.md](active/hero-displacement.md) | HC31–HC36／MV01–MV06、共享位移、表现与预设入口实现接入完成；未构建／未运行验证，待用户体验。 |
| 战斗实验室 UI 与交互优化 | [active/battle-lab-ui.md](active/battle-lab-ui.md) | 三栏工作区、预设浮层、选择／拖放与撤销实现整合完成；未构建、未运行交互或渲染验证，待用户体验。 |
| BC01–BC27 英雄实现 | [active/bc-hero-roster.md](active/bc-hero-roster.md) | 首版三体系接入及当前内容基础检查已完成；最新修复HC06／HC18满蓝不放，HC18按主动范围接近伤者，并共享施法者短光。专项逻辑、移动／法力及正式画面输入检查通过，玩家体验待反馈；具体覆盖见任务顶部。 |
| 首批设计英雄与测试假人 | [active/designed-heroes-and-test-dummies.md](active/designed-heroes-and-test-dummies.md) | 本批接入和局部验证完成，等待 HC01／HC03 体验反馈；不合并其他讨论决定。 |
| 长期玩法讨论：全局边界、基础模型、整局机制与内容检验 | `../design-discussion/roadmap.md` | 讨论进程唯一入口；逐题记录，不边讨论边改权威，最终统一整合。 |
| 上一轮游戏文档对齐与已知兼容问题来源 | `active/combat-build-population-framework.md` | 对齐已完成；后续讨论改走 design-discussion，不恢复旧的逐题权威同步流程。 |
| 通用内容模型与结算链路 | `active/content-system-foundation.md` | 已实现、编译通过；行为执行和玩家验收仍待完成。 |
| 正式装备与英雄独立法力 | `active/formal-equipment-and-hero-mana.md` | 已实现、编译通过；玩家体验待验收。 |
| 远程攻击与基础特效 | `active/ranged-attacks-and-basic-effects.md` | 按该任务的最新证据恢复，不从旧 Alpha 重新实施。 |
| Web 关系图与模型 | `active/gameplay-model-lab.md` | 保留工具/草案；规则快照未全面同步，不是另一份正式权威，不自动继续扩建。 |
| 独立体验切片 | `active/gameplay-experience-slice.md` | 保留已接入成果；体验工作转正式游戏，历史下一节点已失效。 |
| 图标与肖像 | `active/tower-autobattler-semantic-icons-animated-portraits.md` | 保留其待验收状态；不因本轮文档整理宣称完成。 |

上述入口是导航，不重复维护规则和详细验收。调研任务/资产维持原状，不纳入本次游戏文档清理。
