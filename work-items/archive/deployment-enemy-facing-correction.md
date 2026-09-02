# Deployment Enemy Facing Correction

Status: Complete

## Goal

Correct the pre-battle deployment board so concrete enemy idle previews face left toward the player deployment zone. Preserve the existing correct battle-facing behavior and every other `UnitPortrait` consumer.

## Confirmed Diagnosis

- The deployment board places enemies on the right side, but `EnemyDeploymentPreview` binds the same neutral `UnitPortrait` presentation used by ordinary cards and rows.
- `UnitPortrait.Bind` currently applies only `UnitPortraitDefinition.FlipHorizontal`. Current enemy portrait definitions use the default non-flipped value, so their right-authored art remains facing right during deployment.
- Battle presentation is already correct and independently owned: team defaults make players face right and enemies face left, then movement and target-facing update it. This task must not modify battle animation, simulation, targeting, coordinates, or content scenes.
- Changing every enemy portrait resource would be the wrong ownership boundary because those immutable definitions are shared by reports and other neutral portrait consumers.
- Mirroring the complete enemy-preview control would also be wrong because badges, overlays, layout, hit geometry, and tooltip ownership must remain upright and unchanged.

## Confirmed Solution

- Extend the reusable authored `UnitPortrait` scene/component with a consumer-authored horizontal-mirror option that defaults off.
- Compose that option with the portrait definition's existing authored flip rather than replacing it. The effective image flip is the definition flip XOR the context mirror for both the animated sprite and fallback texture path.
- Author the enemy deployment preview's `UnitPortrait` instance with context mirroring enabled in `EnemyDeploymentPreview.tscn`.
- Keep every other portrait instance at the default so hero selection, recruitment, player deployment cells/cards, Army details, and battle reports do not change.
- Mirror only the character image leaves owned by `UnitPortrait`; enemy/role/reach badges and the outer preview control remain unchanged.
- Add a focused regression assertion proving every deployed enemy preview uses the inverse of its definition flip and that the portrait continues independent idle playback. Preserve the existing content-resolved identity checks.
- Update the gameplay, system, and manual-QA authority with the durable deployment-facing contract before production code changes.

## Architecture And Authority Impact

- Subsystem: UI presentation, specifically the authored portrait component and deployment enemy-preview scene.
- `UnitPortraitDefinition` remains immutable asset-normalization data. Context orientation belongs to the consuming portrait scene instance.
- `UnitPortrait` continues to own only image rendering/playback and gains no stable-id, team, gameplay, lookup, or mutable run-state knowledge.
- `EnemyDeploymentPreview` remains a deployment-only authored scene receiving a composition-resolved view model.
- Player-facing authority must state that enemy deployment previews face left toward the player zone and only character art is mirrored.
- System authority must state how definition flip and consumer-authored context mirroring compose without mutating shared resources.
- Manual QA must cover enemy preview direction and upright overlays at `1600x900` and `1280x720`, plus consistency with the initial battle-facing state.

## Scope

- `src/UI/UnitPortrait.cs`.
- `scenes/ui/components/EnemyDeploymentPreview.tscn`.
- Focused deployment portrait contract coverage, preferably the existing `DeploymentInputHeroSelectionContractSmoke` path.
- Narrow additions to `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, and `docs/testcases/alpha-manual-qa.md`.
- Focused build/test and fresh deployment captures at the two supported resolutions if the existing capture harness remains compatible after the architecture refactor.

## Non-Goals

- No battle-facing, movement, attack/heal target-facing, simulation, AI, navigation, targeting, formation, spawn, coordinate, or save changes.
- No enemy, hero, portrait-definition, `SpriteFrames`, donor, generated import, icon, Theme, or semantic badge resource edits.
- No global flip of enemy portraits in reports or other neutral identity surfaces.
- No layout redesign, badge relocation, deployment interaction change, architecture cleanup, or unrelated refactor.
- No modification or resumption of the active combat-build migrations beyond preserving their current interfaces.

## Constraints

- Work on `main`; do not create or switch branches.
- Preserve the large dirty worktree and the completed architecture refactor. Inspect current diffs before every overlapping edit.
- `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, and `docs/testcases/alpha-manual-qa.md` already contain unrelated changes; add only the narrow facing contract.
- `EnemyDeploymentPreview.tscn` and `DeploymentInputHeroSelectionContractSmoke.cs` are currently untracked project work and must be patched in place, never recreated from stale context.
- Use the authored `.tscn` instance to select context mirroring; do not set node scale on the whole preview or reconstruct UI in code.
- Use low-concurrency .NET validation and avoid unnecessary editor launches or repeated imports.

## Acceptance Criteria

- On the pre-battle deployment board, every concrete enemy idle preview on the right faces left toward the player deployment zone.
- Player deployment portraits continue facing their existing direction; neutral portrait consumers remain visually unchanged.
- Effective flip composes with `UnitPortraitDefinition.FlipHorizontal`, so a future definition whose base flip is true still faces the opposite direction in the enemy deployment context.
- Both animated-sprite and fallback-texture rendering apply the same context mirror.
- Enemy/role/reach badges, tooltip, cell placement, layout, and interaction geometry remain upright and unchanged.
- Existing enemy stable-id, content-resolved portrait, and independent idle-playback assertions remain green; focused coverage rejects the old same-as-definition enemy orientation.
- Battle-facing contracts remain green without production changes.
- A low-concurrency .NET build and the focused deployment test pass. Fresh deployment evidence at `1600x900` and `1280x720` shows the corrected direction without clipping or overlay inversion.
- No files outside the confirmed scope change because of this task.

## Progress

- 2026-08-30: User reported that enemy facing is reversed during pre-battle deployment.
- 2026-08-30: Read-only diagnosis confirmed that deployment enemy previews reuse the neutral portrait flip, while battle presentation already applies the correct team-facing rule.
- 2026-08-30: User confirmed the presentation-only correction and requested execution after the architecture refactor.
- 2026-09-01: Main Agent re-read the refactored architecture, current source, active tasks, gameplay/system authority, and current dirty worktree. The diagnosis and ownership boundary remain valid: `DeploymentScreenController` now supplies the view model, while `EnemyDeploymentPreview` still binds a neutral `UnitPortrait` with no enemy-context mirror.
- 2026-09-01: Executor confirmed `main`, read the required Godot UI/C#/testing skills and current scoped diffs, and found no ownership conflict. Architecture remains presentation-only: `UnitPortrait` owns character-image leaves, the authored enemy-preview instance owns deployment context, and battle/content/layout contracts remain out of scope.
- 2026-09-01: Narrow gameplay, system, and manual-QA facing contracts were synchronized before production code changes.
- 2026-09-01: RED contract added to the existing deployment input/portrait smoke. With production unchanged, the focused scene exited 1 and reported every enemy preview lacked the authored context mirror, retained the definition flip, and `UnitPortrait` exposed no consumer-authored mirror option. The test also covers XOR fallback behavior and rejects negative-scale mirroring of the preview or badges.
- 2026-09-01: GREEN implementation added default-off `ContextMirrorHorizontal` to `UnitPortrait`, composed effective flip as definition flip XOR context for both animated and fallback image leaves, and enabled it only on the authored `EnemyPortrait` instance in `EnemyDeploymentPreview.tscn`.
- 2026-09-01: Low-concurrency build passed with 0 warnings/errors. The focused deployment input/portrait scene passed while preserving content-resolved enemy identity, independent idle playback, formation input/save coverage, and hero-selection behavior.
- 2026-09-01: Shared portrait and battle-facing regressions passed: `SEMANTIC_PRESENTATION_CONTRACT_OK` and `MOVEMENT_PRESENTATION_CONTRACT_OK`, with movement output confirming team-default facing, sprite-only presentation, and unchanged simulation.
- 2026-09-01: Fresh deployment captures completed at `1600x900` and `1280x720`. Visual review confirmed all right-side enemy character images face left while enemy/role/reach badges remain upright; player portraits, deployment geometry, interaction regions, clipping, and layout remain unchanged.
- 2026-09-01: Final scoped review found `ContextMirrorHorizontal` only in `UnitPortrait`, the authored enemy deployment portrait instance, and focused contract coverage. No negative-scale mirroring, enemy portrait-resource edits, battle-facing changes, layout changes, or changes to other portrait consumers were introduced by this task. Scoped tracked-file `git diff --check` passed.
- 2026-09-01: Main Agent independently reviewed the scoped implementation and confirmed `ContextMirrorHorizontal` defaults off, XOR composition applies to both animated and fallback image leaves, and only the authored `EnemyPortrait` scene instance enables it. The review also confirmed there is no whole-control negative scale and no battle or content modification from this task.
- 2026-09-01: Main Agent independently inspected the `1600x900` and `1280x720` deployment evidence. All right-side enemies face left, badges remain upright, and player portraits and deployment layout remain unchanged.
- 2026-09-01: Main Agent independently reran the focused deployment contract with Godot 4.7 headless. It exited 0 and printed `DEPLOYMENT_INPUT_HERO_SELECTION_CONTRACT_OK`. Verification passed and the task was accepted for archival.

## Resume Condition

None. The task is complete and independently verified.

## Verification Handoff

- Main Agent verification passed; this task is accepted and archived.
- Production behavior: `UnitPortrait` exposes a default-off authored context mirror and applies `definition.FlipHorizontal XOR ContextMirrorHorizontal` to both animated and fallback image leaves. Only `EnemyDeploymentPreview/EnemyPortrait` enables it.
- Automated evidence:
  - RED before production change: focused deployment scene exited 1 and rejected missing context mirroring, same-as-definition enemy flip, and absence of the consumer-authored property.
  - `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`: passed, 0 warnings and 0 errors.
  - Focused deployment contract: `DEPLOYMENT_INPUT_HERO_SELECTION_CONTRACT_OK`.
  - Shared portrait contract: `SEMANTIC_PRESENTATION_CONTRACT_OK`.
  - Battle movement/facing contract: `MOVEMENT_PRESENTATION_CONTRACT_OK`; simulation unchanged.
  - Fresh captures: `DEPLOYMENT_INPUT_HERO_SELECTION_CAPTURE_OK size=1600x900` and `DEPLOYMENT_INPUT_HERO_SELECTION_CAPTURE_OK size=1280x720`.
- Visual evidence:
  - `.godot/qa/deployment-input-hero-selection/UI_1600x900_DeploymentReserveSelectedEnemyPreviews.png`
  - `.godot/qa/deployment-input-hero-selection/UI_1280x720_DeploymentReserveSelectedEnemyPreviews.png`
- Independent verification confirmed the two captures show enemies facing left, overlays upright, player portraits unchanged, and no new clipping or layout displacement. Scoped implementation review and the focused Godot 4.7 headless rerun also passed.
