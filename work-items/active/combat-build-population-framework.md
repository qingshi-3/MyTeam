# 构筑、培养与人口框架：当前决策与文档对齐

Status: Documentation Aligned — Gameplay Details And Scoped Compatibility Checks Remain Open

## 后续讨论入口变更（2026-09-06，优先于下方旧恢复方式）

用户确认后续是长期、分层的基础模型与机制讨论，不仅是原 18 项，也不立即制作具体英雄/羁绊。新进程统一进入 [讨论总览](../../design-discussion/roadmap.md)，记录约定见 [讨论区入口](../../design-discussion/README.md)。本文件保留上一轮对齐证据，不重复维护新阶段的状态。

新讨论逐题保存在 design-discussion，不再边讨论边更新玩法/系统权威；整体讨论与冲突复核结束，经确认再统一合入。原有已对齐权威不回退。已知 Lab/临时单位容量/Web 差异由新问题台账引用，不视为已修复或实现授权。

本次目录搭建范围为新讨论 Markdown、项目 AGENTS 路由及任务导航；保留工作区既有未提交修改，不修改游戏规则正文、技术契约、代码、资源、调研或浏览器草案。新目录的议题入口不是检索/方案完成声明。

## 当前目标、范围与恢复入口（2026-09-06）

用户要求对齐游戏自身文档，避免有价值的讨论与旧规则冲突。授权仅包括文档整理和必要的静态核对；不修改代码、场景、配置、存档、Web 模型/浏览器草案或调研库，不提交/推送。仅在 main 工作；开始时工作区干净。

规则单一归属：

- `../../gameplay-design/combat-build-framework.md`：讨论 1–18 的培养/替换、特殊内容、构筑、供给池和风险收益定位，以及人口里程碑。
- `../../gameplay-design/tower-autobattler-core.md`：当前基础规则、重点地图方向、Boss 预告与主动介入边界。
- `../../system-design/tower-autobattler-architecture.md` 与 `../../system-design/content-composition-foundation.md`：技术契约、兼容边界和 v6 保存支持；不替玩法补参数。
- `../README.md`：后续工作的导航，不以本文件复制其他任务的实现/验收结论。

本轮验收：确认最新用户意图有归属；未定参数不写成已定；旧任务失效或归档有明确提示；有效链接和变更范围检查通过；旧研究/代码/配置未改。只进行文档检查，不启动构建、Godot 或试玩。

### 尚未解决的设计与实现核对

- 培养返还内容/比例、蒸馏与继承边界、分支进化细节、备用容量、是否英雄分级、成长/获取经济、每局内容池与风险收益具体规则仍待设计。
- 地图是重点方向；尺寸、分区/多层/守点等方案、胜负目标与实施顺序未定。现有 10/18 人口契约和地图实现基线继续保持，不能自行取消。
- 主动介入要区分战前调整与战中操作；现有两指令/三战术点与英雄自动法力施法在明确替代前保持。
- Lab 仍有 `PrimaryHeroInstanceId` 兼容字段；主英雄规则与统一名册的准备链是否完全一致，需要后续限定范围的实现核对。不能通过删文档声称已经修复。
- 旧系统文字把 18 写为 persistent/temporary 的共同 physical-unit ceiling，而玩法框架将临时单位容量留给 authoring。当前整理不裁决临时单位与部署容量关系；新增地图/召唤内容前需核对真实容量规则并明确口径，不能静默放宽或收紧。
- Web 规则快照仍可能滞后于正式契约；本轮仅纠正文档入口与状态，不声称已同步 Web 数据或用户草案。

### 本轮进度与证据

- 已将讨论结论写入现有玩法归属章节，区分已定规则、已确认方向、特殊内容和暂缓事项；未新增玩法配置或技术原语。
- 已将原 Alpha/可玩性润色的 Completed 任务归档，将旧重设计任务标为 Superseded 后归档；保留原有证据，不把迁移计划归档等同完成实现验收。
- 2026-09-06 静态检查通过：15 份修改/新增位置的文档内引用路径可解析；3 份移入归档的任务不再有 active 副本，原 Verification Handoff 保留；所有变更仅在玩法、系统、任务和验收 Markdown 范围，`git diff --check` 通过。没有修改调研/代码/配置，没有构建、Godot 或试玩。此证据只说明文档对齐，不证明待核对的运行时行为正确。

### 当前恢复条件

文档对齐完成后，继续用户选择的设计讨论或明确授权的实现；不得把本轮 Agent 建议推断成地图优先实施授权。实际代码/行为验收按各现行任务证据处理，不重跑旧整体迁移。

## 历史记录：2026-08-31 文档确认

以下 Goal、Confirmed Rules、Resume Condition 和 Verification Handoff 保留当时记录，不是当前指令。其“实现整体暂缓”和旧目录引用已被上方入口替代；不单独复制为有效规则。

## Goal

Record one self-contained long-term player-facing framework that connects hero-role hierarchy, build grammar, population progression, population-based builds, and the `10 / 18 / 30` landmarks without starting system design or implementation.

The resulting authority must prevent two design failures: treating every hero as a supposed core, and treating population as free bodies plus unrestricted team-wide scaling.

## Confirmed Rules

- The product combines readable autobattler population/trait composition, `The Last Flame`-like ability/status/equipment/relic build grammar, and tower-run decisions. References inspire qualities and are never implementation authority.
- A run grows current population toward an ordinary endgame ceiling of `10`; it does not start at `10` by contract.
- Every persistent hero consumes exactly `1` population. Historical tier/rarity framing is superseded by the current unresolved tier-adoption decision; any future tier does not change population cost.
- The eighteen legal player candidate cells are the physical ceiling. Explicit build sources may raise persistent population above `10` and may high-roll toward all `18` cells.
- Temporary units require explicit authored sources, occupy actual free cells, consume no persistent roster/reserve population, and cannot break occupancy or defeat rules.
- Formations contain readable cores/payoffs, engines/enablers, and functional/bridge heroes. Every recruitable hero still needs legible acquisition-stage, immediate-use, replacement, and build value.
- A formed archetype contains driver/engine, state/resource, payoff, survival, and spatial condition.
- Population is a cross-archetype army/force chassis. Expansion, headcount-to-power scaling, and payoff/settlement are separate ordinary responsibilities; only a deliberate rare capstone may compress all three.
- Every population effect names whether it counts starting deployed roster heroes, current living friendlies, temporary units, or trait value. Unbounded all-team offense/health-per-head scaling is excluded.
- `10` is the ordinary endgame formation ceiling; `18` is the full-board population-build goal; `30` is a possible hidden single-trait-value achievement rather than a physical body target or routine breakpoint.
- Above-`10` growth consumes real hero, ability, equipment, or relic opportunity. Dense-combat readability and performance remain future acceptance risks.

## Authority Impact

- Create `gameplay-design/combat-build-framework.md` as the focused authority for population, hero role hierarchy, build grammar, and landmarks.
- Route the module from `gameplay-design/README.md`.
- Update `gameplay-design/tower-autobattler-core.md` only enough to remove obsolete `5 deployed + 3 reserve` authority, state the `10 / 18 / current population` relationship, and route detailed build rules to the focused module.
- Historical update target was the redesign task now stored at `work-items/archive/hero-roster-independent-tactics-redesign.md`; its former `5+3` baseline was superseded.
- Do not modify `AGENTS.md`, `system-design/`, implementation files, resources, persistence, tests, QA, or the separately owned content-platform task.

## Scope

- Long-term player-facing population and build grammar.
- Relationship between persistent hero population, legal cells, temporary units, and trait value.
- Hero content responsibility hierarchy and anti-junk-offer requirement.
- Documentation routing and removal of contradictory current capacity authority.
- A future implementation handoff that remains explicitly deferred.

## Non-Goals

- No code, resources, save schema, system ownership, UI, balancing tables, tests, or migration.
- No exact starting population, growth curve, tier taxonomy, tier odds, reserve capacity, or recruitment economy.
- No detailed frost, burn, shock, death, barrier, reaction, resistance, or status-stack design.
- No normal trait breakpoint table, final achievement name/reward, or confirmed secret transformation.
- No claim that every build must use population, temporary units, or the `18`-cell ceiling.
- No thirty-body battle target and no routine balance breakpoint at trait value `30`.

## Unresolved Numbers And Rules

- starting population and per-floor/per-region growth cadence;
- hero tier names, unlock stages, offer odds, and role-distribution rules;
- reserve capacity and reserve/deployment exchange behavior;
- recruitment price, replacement value, and how expansion opportunities enter reward pools;
- ordinary population/trait breakpoints below the `18` physical ceiling;
- how many above-`10` sources exist and how much opportunity each consumes;
- which traits can receive multi-contribution, inheritance, multiplier, or hidden-transformation content;
- `30`-value achievement naming, reward, signaling, and persistence;
- dense-combat readability, path contention, report density, and performance budgets at high population.

## Progress

- 2026-08-31: User rejected the fixed `5 deployed + 3 reserve` direction in favor of run-grown population with an ordinary endgame ceiling of `10`, an explicit-build physical ceiling of `18`, and a possible hidden trait-value landmark of `30`.
- 2026-08-31: User fixed every persistent hero at one population regardless of tier and left the starting curve, tier taxonomy/odds, reserve size, and recruitment economy unresolved.
- 2026-08-31: User confirmed the core/engine/function hierarchy, the five-part archetype grammar, and population as a cross-archetype chassis with explicit count bases and anti-quadratic safeguards.
- 2026-08-31: Documentation-only synchronization authorized. No architecture, code, resource, persistence, UI, test, QA, build, Godot, or migration work is part of this turn.
- 2026-08-31: Documentation synchronization completed across `gameplay-design/README.md`, `gameplay-design/tower-autobattler-core.md`, new authority `gameplay-design/combat-build-framework.md`, the deferred hero-roster task, and this discussion task. No forbidden or separately owned surface was edited.
- 2026-08-31: Documentation verification passed: both gameplay authority routes exist; current accepted sections contain no obsolete five-deployed/three-reserve claim; `10 / 18 / 30`, one-population-per-hero, and unresolved reserve/curve/tier/economy markers are present; touched documents have no trailing whitespace; repository `git diff --check` exits clean. No build or Godot command was run.

## Resume Condition

Implementation remains deferred. Resume design discussion by resolving one bounded topic at a time, starting with the ordinary starting/growth curve, tier availability, reserve contract, or recruitment economy. Resume implementation only after the separately owned architecture work completes or explicitly hands off, system authority is reconciled, and the user confirms the remaining capacity/economy rules needed by that implementation slice.

## Verification Handoff

- Confirm `gameplay-design/combat-build-framework.md` contains all confirmed rules and marks every unresolved number as deferred.
- Confirm `gameplay-design/README.md` routes both core and build authority.
- Confirm current gameplay authority and the hero-roster deferred task no longer state five deployed heroes or three reserves as accepted capacity.
- Confirm `10` means ordinary endgame formation ceiling, `18` means physical full-board population-build ceiling, and `30` means possible hidden trait value rather than body count.
- Confirm no implementation or separately owned task was modified.
- Verification for this turn is documentation scans plus `git diff --check`; no build or Godot evidence is expected.
