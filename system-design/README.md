# System Design

Accepted implementation architecture lives here.

- Core architecture and content-scene contract: `tower-autobattler-architecture.md`
- Shared content primitives, grants, ability entry points, and run decisions: `content-composition-foundation.md`

Documents in this directory own runtime responsibilities, boundaries, contracts, and failure semantics. They do not track task progress.

Player intent and confirmed-but-unspecified directions remain in `../gameplay-design/`; technical support for an effect is not approval to add that gameplay. Existing compatibility paths must be labelled as such rather than promoted to product rules. Current contracts and historical migration requirements are distinct; execution/verification evidence is routed through `../work-items/README.md`.
