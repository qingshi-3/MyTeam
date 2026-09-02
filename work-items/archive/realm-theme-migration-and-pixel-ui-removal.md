# Realm Theme Migration And Pixel UI Removal

Status: Completed  
Confirmed: 2026-08-30

## Goal

Permanently remove the generated pixel-fantasy UI system and replace the live game's visual Theme with a project-local adaptation of `D:\godot\realm\resources\ui\RealmTheme.tres`.

The result must use RealmTheme's restrained dark flat surfaces, thin borders, rounded corners, and gold emphasis across the complete playable flow, without retaining pixel-metal textures or runtime dependencies on the external Realm project. Gameplay, Chinese content, semantic colors/icons, animated portraits, responsive layout, input/focus, reports, saves, and typed scene contracts must remain intact.

## Confirmed Destructive Scope

The user explicitly confirmed irreversible removal after being told that most targets are uncommitted and cannot rely on Git recovery.

Delete these exact project-owned pixel UI surfaces:

- Entire `assets/ui/pixel_fantasy/` tree: current audit found 186 files and approximately 14.4 MB.
- Entire `content/ui/pixel_fantasy/` tree: current audit found 74 Theme/StyleBox resources.
- `scenes/ui/dev/PixelFantasyUiPreview.tscn` and the empty directory if no unrelated file remains.
- `src/UI/Dev/PixelFantasyUiPreview.cs` plus its UID and the empty directory if no unrelated file remains.
- `tools/process_pixel_fantasy_ui.py` and `tools/process_pixel_fantasy_live.py`.
- `tests/PixelFantasyUiContractSmoke.*`, `tests/PixelFantasyLiveSkinContractSmoke.*`, and `tests/PixelFantasyVisualRestraintContractSmoke.*`.
- `work-items/archive/pixel-fantasy-ui-assets.md`, `work-items/archive/pixel-fantasy-live-ui-rollout.md`, and `work-items/archive/pixel-fantasy-ui-visual-restraint.md`.
- Current `content/ui/game_theme.tres` after all required non-pixel semantic/report contracts have been migrated into the new Theme.
- Pixel-review-only caches under `.godot/qa/live_review_border/` and `.godot/qa/restraint_review/`, plus only explicitly pixel-preview-named QA artifacts if present. Do not clean unrelated `.godot/qa` captures or `.godot/imported` manually.

Before each recursive deletion, resolve the exact absolute target, verify it is inside `C:\Users\qs\godot\my-team`, enumerate/count it, and use one PowerShell process with `Remove-Item -LiteralPath`. Never delete a computed broad parent, workspace root, wildcard target, or external Realm path.

## Confirmed RealmTheme Integration

- Treat `D:\godot\realm` as a read-only donor for this task. Never edit it and never leave a runtime dependency on its absolute path.
- Source `RealmTheme.tres` is self-contained: it has no external `res://` font, texture, or resource reference. Its confirmed source SHA-256 is `C7709B1B2843A0F85A9CBCDADF96D4EEB03887A90B2CCD7720A32DD40DE11D9E`.
- Copy its Theme content into project-local `content/ui/RealmTheme.tres` and make that the one authoritative Theme assigned by `GameRoot`.
- Preserve RealmTheme's core style subresources and provided roles: default `PanelContainer`/`Button`, `TopBarPanel`, `ContextSidebarPanel`, `ModalPanel`, `NotificationPanel`, `TitleLabel`, `SectionTitleLabel`, `PrimaryButton`, `SecondaryButton`, `DangerButton`, and `CompactButton`.
- Extend the local Theme only for project-required semantics and missing control types. Keep health green, damage red, mana blue, shield gray, healing teal, gold/hero gold, range cyan, danger crimson, risk amber, player/enemy distinction, secondary text, report progress bars, and any necessary slider/scroll treatment. These extensions must use RealmTheme's flat visual language and must not reintroduce pixel textures or old visual-role names.
- Replace the pixel tiled `TextureRect` background with an authored flat `ColorRect` matching RealmTheme's dark palette.
- Remap all 13 live screens and shared components away from `Live*`, `Quiet*`, `Tactical*`, `Pixel*`, `PrimaryDecisionButton`, `StatSurface`, `TraitSurface`, `AbilitySurface`, and other removed visual roles. Use RealmTheme roles or project semantic/typographic roles defined in the local RealmTheme.
- Update the minimal presentation-only runtime role restore in `HeroLibraryTile.cs` to a RealmTheme role. Runtime code must not construct Theme resources, StyleBoxes, or full Control trees.

## Architecture Impact

- UI Presentation owns the migration. `content/ui/RealmTheme.tres` becomes the sole live Theme authority.
- Update `system-design/tower-autobattler-architecture.md` before code/resource migration: delete the pixel source/production/live processor ownership model and replace it with the project-local RealmTheme donor contract plus semantic extension boundary.
- Update `docs/testcases/alpha-manual-qa.md` before implementation: remove pixel preview/live/restraint QA sections and add RealmTheme full-flow, semantic, state, focus, responsive, and no-external-dependency checks.
- Delete obsolete pixel task records instead of preserving misleading historical authority.
- Player-facing gameplay rules do not change. Existing semantic-icon, animated-portrait, battle-report, navigation, deployment, and save contracts remain authoritative.

## Scope

- Exact destructive cleanup listed above.
- Local RealmTheme resource plus project semantic/control extensions.
- GameRoot background and Theme reference migration.
- Theme-role remapping across all live screens and reusable UI component scenes.
- Minimal presentation-only runtime role-name changes.
- Replacement `RealmThemeContractSmoke` and updates to existing UI/hierarchy/semantic/report tests that currently assert removed pixel roles.
- Low-concurrency build, serial focused/full regressions, clean startup, and fresh `1600x900` plus `1280x720` complete visual captures.
- Authority, QA, activity progress, deletion evidence, validation evidence, and independent handoff.

## Non-Goals

- No gameplay, combat, AI, navigation, targeting, economy, progression, save, content definition, report derivation/layout algorithm, route generation, deployment command, or input behavior change.
- No deletion or replacement of semantic SVG icons, unit animations, portrait definitions, report data, content scenes, or `web/`.
- No copy of unrelated Realm scenes/scripts/resources and no runtime link to `D:\godot\realm`.
- No new image generation, pixel asset regeneration, or use of old pixel textures as fallback.
- No global resolution/stretch, fullscreen, or window-mode change.
- No branch, commit, push, unrelated cleanup, or reversion of the intentionally dirty worktree.

## Protected Worktree

- Work only on `main`; do not create or switch branches.
- Current pre-task Git porcelain contains 73 entries. Record its exact digest, all deleted/untracked ownership, and protected file/tree hashes before writing because Git does not provide recovery for most task surfaces.
- Preserve all existing non-pixel modified/deleted/untracked work, including dynamic reports, semantic presentation, responsive layout, gameplay tests, other active/archived tasks, and `web/`.
- Record and preserve hashes for semantic icons/catalog, 45 portraits, report sources/contracts, gameplay authority/runtime, project settings, and user processes.
- Do not launch, close, or control the user's existing Godot/editor/game processes. Use isolated headless tests and the dedicated capture scene only.
- Never edit or delete `D:\godot\realm`; read/copy only the confirmed source Theme.

## Acceptance Criteria

### Removal and ownership

- Every confirmed pixel asset/resource/preview/processor/test/archive target is absent, with before/after counts and resolved paths recorded.
- No live scene, code, Theme, test, system/QA authority, or project resource path references `res://assets/ui/pixel_fantasy`, `res://content/ui/pixel_fantasy`, the deleted processors/previews, or the external absolute donor path.
- `GameRoot` assigns only `res://content/ui/RealmTheme.tres` and uses a flat authored background without the deleted texture.
- The local Theme retains the donor core visual definitions and contains only necessary project semantic/control extensions. No old pixel visual-role vocabulary or `StyleBoxTexture` reference remains in the live Theme.

### Visual and interaction behavior

- All 13 screens plus Army overlay, hero selection/detail, recruitment, deployment, battle HUD/report, route choices, settings, and result screens consistently use RealmTheme's dark flat language.
- Normal, hover, focus, pressed, selected, disabled, locked, danger, primary, secondary, and compact roles remain clear. No control becomes unstyled, invisible, or dependent on removed Theme variations.
- Health/damage/mana/shield/healing/gold/range/danger/risk and player/enemy meanings retain their established icon/Chinese-text carriers and semantic colors.
- At `1600x900` and `1280x720`, no text, icon, portrait, value, progress bar, tooltip, focus indicator, or fixed action clips or overlaps. Existing scroll owners and responsive breakpoints remain unchanged.
- Animated portraits, exact-once activation, mouse/keyboard/gamepad focus, Army modal behavior, deployment drag/state, report tabs, and gameplay commands behave as before.

### Verification

- A focused `RealmThemeContractSmoke` proves local authoritative ownership, donor-path absence, deleted pixel path absence, 13-screen coverage, required Realm roles, preserved semantic extensions, flat background, and no runtime Theme/StyleBox construction.
- Existing hierarchy, semantic, portrait/window, responsive/dynamic report, fixture, content, gameplay, movement, UI, alpha-run, and clean-startup checks remain green after pixel-only expectations are removed or replaced.
- Low-concurrency build reports zero warnings and zero errors.
- Fresh paired complete captures pass manual review for RealmTheme consistency, readability, interaction state, and fixed-action reachability.
- Protected non-pixel hashes and the source donor hash remain unchanged.

## Progress

- 2026-08-30: User rejected the pixel-fantasy UI direction, explicitly confirmed irreversible removal of the audited pixel UI system, and selected `D:\godot\realm\resources\ui\RealmTheme.tres` as the replacement visual authority.
- 2026-08-30: Read-only inspection confirmed RealmTheme is a 13,975-byte self-contained Theme with no external resource dependencies. It supplies flat panel/button/title roles but lacks project semantic/report/slider roles, so the confirmed migration uses a project-local copy with semantic/control extensions rather than losing health/damage/mana/report presentation.
- 2026-08-30: Destructive targets were audited before authorization: 186 files under the pixel asset tree, 74 pixel Theme/StyleBox resources, one preview scene, one preview source plus UID, two processors, three pixel contract families, and three obsolete pixel archive tasks.
- 2026-08-30: Execution began on `main` under the confirmed destructive scope. The protected pre-task porcelain baseline is the 73 entries that predate this activity (`39` modified, `7` deleted, `27` untracked), with SHA-256 `ce8f8f0859da0bd49a1c1f8d24358157acf79b1b9670877afc045702861d9e78` over UTF-8/LF porcelain lines. The current 74th entry is this activity document itself; no existing dirty surface may be reset, reverted, or broadly cleaned.
- 2026-08-30: Every exact destructive target was resolved before writing. `C:\Users\qs\godot\my-team\assets\ui\pixel_fantasy` contains 186 files / 14,391,082 bytes; `content\ui\pixel_fantasy` 74 / 33,412; the preview scene 1 / 21,864; preview source plus UID 2 / 2,228; processors 2 / 30,160; three pixel test families 9 / 31,313; three obsolete archive tasks 3 / 76,860; old `game_theme.tres` 1 / 26,157; `.godot\qa\live_review_border` 7 / 59,643,429; and `.godot\qa\restraint_review` 7 / 14,769,405. No other pixel-named QA artifact is present. Recursive targets resolve inside the project root; the external donor is not a delete target.
- 2026-08-30: The read-only donor remains exactly 13,975 bytes with SHA-256 `C7709B1B2843A0F85A9CBCDADF96D4EEB03887A90B2CCD7720A32DD40DE11D9E` and zero `res://`, `uid://`, `ExtResource`, or external-path dependency match. User-owned Godot editors remain outside executor control: PID `23260` is Godot 4.7 editing this project and PID `49048` is Godot 4.5.1 editing `the-war`; neither may be launched, closed, focused, or controlled by this task.
- 2026-08-30: Protected pre-write fingerprints were recorded with deterministic relative-path/length/file-hash aggregation: semantic icons `fe2ba4903107613a1a1ff07c290e97b282763bcbd86467e3aeb6753970ce5d65` (76 files), semantic catalog `fd79e59516ebdda1d4db8cade21d723ebb12b770ef047d0422b5ee068f758ee3`, semantic runtime `3beca4068b056d1067864c687ac51d93adfc632546da7298950269890e96cf7c`, 45 portraits `69b4b12ff767f738d2122450d866c66cae11d89c5190c79eb115a0e9865830fa`, four report sources `d522dec26b426f2548c1270ab09b5886ee20fb6f67868ad2cb4c98977f5bc91f`, four dynamic/responsive report contracts `1a2d4965c6ec0b83b7c1f5bc2bc7ecb4fd37b9400355bc28330737f0fc41d74e`, gameplay authority `d8839e1c96e81c28c6ae49e9fd4408fdde8bfcb960971239285dabfd101e1499`, gameplay/content/presentation runtime `5712f8f9b655d8950a7d729608e364923267b40498bf2af0c03b0f9eff17bf5d`, and `project.godot` `151f08f0a0ffaad297b1ec678dffbab52ddcd78220a0dfbbe8a9220f629fde69`.
- 2026-08-30: UI architecture and manual QA were synchronized before production migration. `content/ui/RealmTheme.tres` is now the sole live Theme authority; `GameRoot` owns a flat dark `ColorRect` background; the 13 screens and shared components use donor-native Realm roles plus flat semantic/report/control extensions. The local Theme contains 44 `StyleBoxFlat` subresources and no `ExtResource`, `StyleBoxTexture`, or `Texture2D` dependency. The semantic/portrait activity handoff was updated only to name the new authoritative Theme.
- 2026-08-30: The focused contract followed the required safety sequence. Its initial RED identified the missing local Theme/mappings/removal, the project then built with zero warnings/errors, and its pre-delete rerun failed only because the confirmed obsolete ownership still existed. This established that the local Theme, all 13 screen loads, role mappings, flat background, and runtime-construction boundary were valid before deletion.
- 2026-08-30: Exact destructive cleanup completed in one persistent PowerShell process using only explicit `Remove-Item -LiteralPath` operations: 292 files and 25 directories were removed, followed by exact absence verification for all 292/25 targets. The removed ownership is limited to the two pixel UI trees, preview scene/source/UID, two processors, three pixel contract families, three obsolete pixel archive tasks, old `game_theme.tres`, two pixel-review caches, and now-empty preview directories. No wildcard, computed recursive parent, external donor path, unrelated QA capture, imported cache, semantic icon, portrait, gameplay/runtime, report, `web/`, or project-setting target was deleted.
- 2026-08-30: Post-delete checks are clean. All confirmed paths are absent. A 33-token live/reference scan across content, scenes, runtime, tests, system/manual/gameplay authority, active tasks, and project settings found zero old pixel paths, obsolete preview/processors, `game_theme.tres`, old visual-role names, or external Realm path. The replacement focused marker is `REALM_THEME_CONTRACT_OK authority=local screens=13 core=donor semantics=preserved background=flat cleanup=complete`.
- 2026-08-30: The first full UI regression exposed one migration-local layout defect: donor `ModalPanel` had no content inset, widening reward cards from their preserved 864px allocation to 896px. The local adaptation retained the Realm flat surface and added only the established 18/14px modal content margins. The interrupted test left one isolated `tests/ui-smoke/active_run.json`; that exact test-only file was removed before rerun, while the real player save namespace was untouched. `UI_SMOKE_OK` then passed with the existing deliberate modal-focus warning.
- 2026-08-30: Final serial verification is green: `VISUAL_HIERARCHY_CONTRACT_OK`, `SEMANTIC_PRESENTATION_CONTRACT_OK`, `WINDOW_PORTRAIT_CONTRACT_OK`, `BATTLE_REPORT_RESPONSIVE_CONTRACT_OK`, `DYNAMIC_BATTLE_REPORT_CONTRACT_OK`, `FIXTURE_CONTRACT_OK`, `CONTENT_CONTRACT_OK entries=57 floors=5 events=90 portraits=45(8,24,13)`, `GAMEPLAY_CONTRACT_OK`, `MOVEMENT_PRESENTATION_CONTRACT_OK`, `UI_SMOKE_OK`, and `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`. Clean five-frame startup exits zero. Content's five deliberate negative gate diagnostics and UI's deliberate focus-escape warning remain expected passing-contract evidence. Final low-concurrency build and `git diff --check` pass with zero warnings/errors; build servers were shut down without touching Godot.
- 2026-08-30: Fresh native captures pass at both supported resolutions. Each resolution has exactly 35 UI images and 29 movement images at the asserted native dimensions; both complete runs report `VISUAL_CAPTURE_OK ... movement_frames=29 ...`, and the non-headless stress capture reports `BATTLE_REPORT_RESPONSIVE_CONTRACT_OK`. Full contact review plus original-size checks of main menu, hero available/selected/locked states, recruitment, deployment, Army drawers, battle HUD/mana states, reports and stress cards, route/shop/event/rest/reward/settings/result screens found no clipping, overlap, hidden fixed action, lost semantic color/icon carrier, unstyled slider/scroll/control state, or pixel texture. Review sheets are `.godot/qa/_realm_theme_contact_1280.png` and `_realm_theme_contact_1600.png`.
- 2026-08-30: Final protection audit confirms unchanged donor and protected ownership. Donor SHA-256 remains `c7709b1b2843a0f85a9cbcdadf96d4eeb03887a90b2ccd7720a32dd40de11d9e`; local RealmTheme SHA-256 is `a5375c3c4295ab58d2f964067dc45362aa8e1aba3ec38cea1c272a1fed7da7ab`. Semantic icons/catalog/runtime, 45 portraits, four report sources, gameplay authority, gameplay/content/presentation runtime, and `project.godot` exactly match their pre-write fingerprints. The four report-contract aggregate is intentionally `57722a63e73cefab5462c5acb37f0dde1f9cb4d4e65cf7175ae8bb306a83e870`; simulating only the approved `RealmTheme.tres` → old `game_theme.tres` path reversal reproduces the pre-write `1a2d4965...` aggregate exactly, proving the controlled Theme-path-only delta. User Godot PIDs `23260` and `49048` remain responsive and untouched.
- 2026-08-30: Independent verification passed. `dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal` completed with 0 warnings and 0 errors. Godot 4.7 headless `tests/RealmThemeContractSmoke.tscn` emitted `REALM_THEME_CONTRACT_OK authority=local screens=13 core=donor semantics=preserved background=flat cleanup=complete`. The main Agent accepted the implementation and authorized completion and archive.

## Resume Condition

This task is complete, independently verified, and archived. It has no resume path. Any later RealmTheme, UI, gameplay, report, or input change requires a new discussion and work item; do not reopen this completed record or recreate the deleted pixel UI ownership.

## Verification Record

### Changed ownership

- Local authoritative Theme: `content/ui/RealmTheme.tres`; root Theme/background binding in `GameRoot`; Theme-role remapping across all 13 screens and shared components; one presentation-only runtime restore in `HeroLibraryTile.cs`.
- Authority and QA: UI architecture, alpha manual QA, this activity, and the stale Theme name in the active semantic/portrait handoff.
- Contracts: new `RealmThemeContractSmoke`; existing hierarchy, semantic, report-responsive, and UI smoke expectations point to RealmTheme roles. No gameplay rule, report derivation/layout algorithm, content definition, save schema, navigation, battle simulation, or project/window setting changed.
- Removed ownership: the exact 292-file/25-directory pixel UI surface documented above. It is intentionally unrecoverable from the current dirty worktree.

### Independent evidence

1. `dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal` completed with 0 warnings and 0 errors.
2. Godot 4.7 headless `tests/RealmThemeContractSmoke.tscn` emitted `REALM_THEME_CONTRACT_OK authority=local screens=13 core=donor semantics=preserved background=flat cleanup=complete`.

### Acceptance

Independent verification accepted the recorded implementation, destructive cleanup, ownership migration, and validation evidence on 2026-08-30. No required work or known implementation defect remains.
