# System Design

当前运行入口为正常 GrowthGameRoot；独立实验场景不创建征程存档。配置身份、原子结算与恢复以[会话合同](run-progression-and-sessions.md)为准，战斗职责见[模块说明](battle-runtime-modules.md)。执行与验证进度统一看[工作入口](../work-items/README.md)。

Accepted implementation architecture lives here.

- Core architecture and content-scene contract: `tower-autobattler-architecture.md`
- Default Expedition / legacy Tower profiles, action ownership and persistence: `run-progression-and-sessions.md`
- Shared content primitives, grants, ability entry points, and run decisions: `content-composition-foundation.md`

Documents in this directory own runtime responsibilities, boundaries, contracts, and failure semantics. They do not track task progress.

Player intent and confirmed-but-unspecified directions remain in `../gameplay-design/`; technical support for an effect is not approval to add that gameplay. Existing compatibility paths must be labelled as such rather than promoted to product rules. Current contracts and historical migration requirements are distinct; execution/verification evidence is routed through `../work-items/README.md`.
