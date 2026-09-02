# Battle Lab Test Cases

## Stage 0 And Core Contracts

- The initial RED scene must fail only for named missing Lab capabilities while the existing build and completed-platform regressions remain unchanged.
- Compare the pre-extraction Run preparation and the shared assembler adapter for fixed Run/Encounter fixtures: complete `BattleConfig`, spawn cells/identities/snapshots, floor rule, Equipment, Relics, Traits, commands, summons, Boss timeline, seed, identity, and deterministic battle digest/result must match.
- Publish the Lab index from `CompiledGamePackage`; every player hero, legal PvE unit, Equipment, and Relic must derive from typed published metadata. Elite/summon membership cannot use concrete ids, paths, or id prefixes.
- Fingerprint the complete authored/compiled Resource graph before edit, preset round-trip, battle, reset, and exit. Every fingerprint must remain unchanged.
- Snapshot the real production Meta, Settings, Active Run, and schema-v4 files plus save-service call counters before Lab entry. Opening, editing, starting, completing, resetting, preset saving/loading, exiting, and re-entering must add zero production-save calls and preserve byte/semantic identity.

## Placement And Real Input

1. At both supported resolutions, enter `战斗实验室` through the real main-menu button. Drag one published player hero and one published PvE unit from their library cards to legal cells using `Viewport.PushInput` mouse motion/press/release events.
2. Reposition both pieces; swap occupied cells; recall each piece to its originating library; explicitly delete a piece; clear one side and clear all. Each successful edit changes the canonical configuration once and every copy retains a unique Lab instance id.
3. Attempt out-of-bounds, forbidden, occupied non-swap, wrong-side formal cells, population overflow, duplicate-cell, missing-content, and same-source drops. Every rejection is non-mutating, deterministic, and exposes a concise Chinese reason plus a non-colour shape/icon/motion/cursor signal.
4. In formal mode, verify player columns `0..2`, the production enemy region, current/effective population, floor legality, and one-unit-per-cell. In free mode, place either side at both board extremes while bounds/forbidden/occupancy remain enforced and `自由实验配置` stays visible.
5. Cancel a drag, release outside the board, switch mode during selection, reset, leave, and re-enter. No hover state, focus target, subscription, Tween, node, or session mutation may leak.

## Equipment, Relics, And Inspection

1. Equip three slots on one concrete player instance, replace and remove an item, and reject a fourth slot. Equip the same definition on a second hero and prove distinct Equipment/source instance identity. Enemy selection must show equipment as not applicable and expose no editing control.
2. Add, remove, and change positive player-team Relic stacks through production legality. Reject zero/negative stacks without inventing a maximum. Enemy instances receive no Relics.
3. After every edit, assert atomic refresh of player count/population, per-hero Equipment, team Relics, Trait contributions/tiers, prepared health/damage/attack speed/reach/control resistance, readiness, and all Chinese failure reasons.
4. During battle, inspect multi-source Status facts including definition, stacks, remaining duration, source ids, and source contributions through the formal read-only runtime snapshot.

## Battle Lifecycle And Determinism

1. Start through real UI input. Pause, continue, step exactly one fixed tick while paused, and select x1/x2/x4. A step must advance one simulation tick, consume/present its events, refresh inspection, and handle terminal state once.
2. Run the same canonical configuration and seed twice. Terminal outcome/tick, deterministic event projection, digest, movement order, Status facts, and report facts must match.
3. Return to configuration after partial and terminal battles. Placement, mode, population, seed, Equipment, and Relics remain identical; damage, Statuses, cooldowns, counters, and runtime modifiers are absent from the edit session.
4. Reset during running, paused, and terminal states; start again; leave and re-enter. Every Battle scope reports zero retained subscriptions/reactions/runtime entries/Tweens/Nodes.

## Presets And Frost Validation

- Round-trip versioned user JSON containing stable ids, unique instance configuration, cells, Equipment instances, Relic stacks, mode, population input, and seed. Reject unknown versions, invalid ids, duplicate instance ids/cells, illegal stacks, and malformed JSON without partial publication or Resource serialization.
- Load `冰霜体系验证`. It must contain two Equipment-capable player units with two independent `霜痕战刃` instances, an active `凛冬盟约` tier, a normal high-health target, and a published development-only target with non-zero authored control resistance that is absent from campaign pools.
- Observe attack-speed growth, two-source Frost attribution, three-stack conversion, shorter Freeze on the resistant target, and apply/stack/expire cues. Source guards must prove these content ids occur only in preset/authored data, never generic runtime dispatch.

## Serial Verification

Run the focused Lab core and real-input scenes first, then the existing seventeen-scene completed-platform regression in this exact order: ScalableAuthoring, ProjectComposition, Relic, AbilityStatus, EffectKernel, Content, Fixture, FormationDeployment, Gameplay, MovementPresentation, AlphaRun, Ui, GameUiInteractionReliability, DeploymentInputHeroSelection, SemanticPresentation, GameUiVisualLanguage, VisualHierarchy. Run `CleanStartup` separately afterward, followed by the low-concurrency build, source guards, `git diff --check`, production-save fingerprint comparison, shared-Resource fingerprint comparison, and process audit confirming user Godot PID `23260` was not controlled.
