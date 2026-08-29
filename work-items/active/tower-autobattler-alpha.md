# Complete Playable Tower Autobattler Alpha

Status: Completed

## Goal

Build a complete, Chinese-language, playable Alpha in `C:\Users\qs\godot\my-team`: choose a hero, build and deploy an army while climbing three tower regions, complete automatic battles with limited hero commands, defeat the final boss, receive victory/defeat results, persist progress, and start or resume another run.

## Confirmed Solution

- Godot 4.7 .NET/C# target.
- One hero, up to six deployed soldiers, and three reserve soldiers.
- Real-time automatic combat with pre-battle deployment, readable enemy/floor information, pause/speed controls, and limited hero commands.
- Eight mechanically distinct heroes, at least twenty-four soldiers, independently authored items, three tower regions, route events, rewards, shops, rest, elites, bosses, unlocks, settings, and active-run persistence.
- Commander, carry/hybrid, summoner, and solo army shapes are supported. Solo is an explicit authored exception and continues to use recruitment rewards through conversion or support rules.
- `D:\godot\rpg` supplies selected existing unit animation assets and patterns only; it remains read-only.

## Hard Architecture Contract

- Every concrete hero, soldier, enemy, and item is an independently instantiable Godot `.tscn` scene.
- Each scene can be opened and tuned without editing the battle root or a central content switch.
- Static definitions are focused `.tres` resources; per-run and per-battle mutable state is separate.
- Shared behavior uses independently instanced component scenes and typed signals/interfaces.
- UI, repeated rows/cards/slots, floor rules, and other reusable presentation structures are authored scenes/resources rather than programmatically assembled trees.
- No runtime dependency may point outside the target repository.

## Authority Impact

- Player-facing rules are owned by `gameplay-design/tower-autobattler-core.md`.
- Runtime and scene contracts are owned by `system-design/tower-autobattler-architecture.md`.
- This task owns execution progress, evidence, remaining work, and verification handoff only.

## Scope

1. Initialize the current repository on `main` and migrate the blank project to Godot 4.7 C#.
2. Establish source, scene, resource, asset, UI, test, and content-catalog boundaries.
3. Import a curated, license-traceable subset of donor unit animations and implement the decoupled animation presenter.
4. Implement content definitions, independent scenes, components, catalogs, validation, seeded run state, save/load, tower generation, rewards, recruitment, roster, items, and meta unlocks.
5. Implement deterministic real-time battle simulation, deployment, unit AI, attacks, abilities, statuses, hero commands, floor rules, outcomes, and presentation events.
6. Author the complete Alpha content and Chinese UI.
7. Add automated validation and headless smoke coverage, then perform low-concurrency build and visual/runtime QA.

## Non-Goals

- Multiplayer or PvP autobattler economy.
- Seven hundred mechanically unique units.
- Strategic overworld, city management, branching narrative campaign, live service, or commercial launch certification.
- Modification, cleanup, or refactoring of the donor repository.
- Final resolution of third-party asset distribution rights beyond retaining provenance and recording any release blocker.

## Constraints

- Preserve any user work in both repositories.
- Do not create development branches.
- Use `apply_patch` for authored file edits; use scripts/formatters only for justified mechanical generation or asset copying.
- Do not repeatedly launch Godot or build. Prefer static/content checks first; build with `-maxcpucount:2 -v:minimal`.
- Do not use code-generated UI, item trees, or unit trees as a shortcut around the independent-scene contract.
- Player-visible content is Chinese; machine ids and code symbols are English.

## Acceptance Criteria

- The project opens under installed Godot 4.7 .NET and the C# solution builds cleanly.
- From the main menu, a player can select an unlocked hero, begin a seeded run, choose routes and rewards, recruit/manage units and items, deploy, fight, reach the final boss, and receive victory or defeat.
- Save/load preserves meta progress, settings, and a valid active run without mutating content definitions.
- There are eight selectable heroes, at least twenty-four recruitable soldier scenes, three tower-region rule sets, and independently authored item scenes.
- At least one commander army, one hero-centered army, and one solo build can complete a deterministic validation run.
- Every cataloged unit and item scene passes isolated load/instantiate validation.
- Battle outcome comes from runtime simulation; animation/UI do not create combat facts.
- Floor rules are previewed and visibly change combat behavior.
- No absolute donor path, central concrete-content switch, hidden scene dependency, or mutable shared definition remains.
- Headless smoke tests, content validation, save round-trip tests, and low-concurrency build pass.
- A visual QA pass confirms readable deployment, combat, tower navigation, rewards, and results at the target desktop layout.

## Progress

- 2026-08-28: Discussion confirmed, including the independent unit/item scene and decoupling requirement.
- 2026-08-28: Read-only donor audit found 697 `SpriteFrames`/unit visual packages (~53 MB) using the shared cue vocabulary. The donor subsystem is clean in its repository, while unrelated donor work remains dirty and untouched.
- 2026-08-28: Gameplay and system authority established. Implementation is ready to begin.
- 2026-08-28: Execution started on `main`. Architecture check assigns combat truth to a fixed-step simulation, run/tower/save state to an application service, and authored scene composition to the content/UI layers. The donor remains read-only; only selected `SpriteFrames` and matching PNG packages will be copied.
- 2026-08-28: Loaded the GodotPrompter routing plus scene organization, component, resource, C#, typed signal, event bus, state machine, asset pipeline, ability, AI, save/load, UI, responsive UI, and HUD guidance before implementation.
- 2026-08-28: Imported 35 explicitly selected donor visual packages into local `res://assets/donor-units/` paths. OpenDuelyst upstream provenance was located; its README and root license identify CC0 1.0, and an exact license copy plus source/hash record is retained under `assets/provenance/`. The earlier distribution-license blocker is resolved for this curated set.
- 2026-08-28: Pre-generation architecture audit tightened the accepted implementation contract without changing product direction: catalog entries pair scene plus authoritative definition without repeated ids; content roots use explicit lifecycle/binding contexts; item registration is symmetric with separate mutable instance state; validation is bidirectional and gates run creation/headless exit; definition fingerprints, deterministic digest, presenter-free settlement, source guards, UI smoke, and three build paths are required evidence.
- 2026-08-28: P0 content fixture passed in Godot 4.7 headless (`FIXTURE_CONTRACT_OK`): one unit and one item loaded through CatalogEntry, survived a bare-host frame, shared the exact authoritative definition reference, and item modifier registration returned to zero after deactivation. C# build passed with zero warnings/errors before bulk generation.
- 2026-08-28: Execution resumed after the bulk-content/P0 behavior migration. Current checkpoint is to restore compilation across battle, floor-rule, hero-command, run DTO, and setup-factory contracts before UI work; all existing repository files are preserved and the donor remains read-only.
- 2026-08-28: Post-migration build restored at zero warnings/errors. All 45 concrete unit scenes now instance `UnitBehaviorComponent`; all eight heroes instance their own command scene; definitions carry category/faction/gameplay tags; twelve item scenes use split hero/army, formation, economy, shield, and summon fields. The one-shot bulk generator was removed after migration.
- 2026-08-28: Full catalog headless contract passed (`CONTENT_CONTRACT_OK entries=57 floors=5 events=90`): structured catalog gate, bare-host frame, closed Unit/Item lifecycle, typed binding communication, floor lifecycle, presenter-free combat, save DTO round-trip, deep definition fingerprint, and source guards. Godot still reports exit-time retained RID/ObjectDB warnings from the smoke process; cleanup is required before this evidence is considered final.
- 2026-08-28: Exit-time RID/ObjectDB leaks were eliminated. UI smoke passed all eleven authored screens (`UI_SMOKE_OK screens=11 navigation=menu-hero-tower-result-settings`), and deterministic full-run validation passed commander, carry, and solo paths through three regions and fifteen floors (`ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`).
- 2026-08-28: Visual capture produced all eleven target screenshots (`VISUAL_CAPTURE_OK screens=11 path=res://.godot/qa`). Review found one remaining presentation defect: `AnimatedSprite2D` was separated from the unit's CanvasItem transform by a plain `Node` component root, so sprites overlapped at viewport origin while health bars followed battle cells. Final visual correction and regression are in progress.
- 2026-08-28: Battle presentation was corrected without changing simulation truth. `UnitAnimationComponent` now remains in the CanvasItem transform chain as `Node2D`; the presenter no longer shrinks the entire unit root, while authored sprite and health-bar anchors keep bodies legible inside cells. Normal, hazard-pulse, and final boss-ward captures show units on their battle cells with no viewport-corner residue.
- 2026-08-28: Deployment at 1280×720 now keeps title, encounter/floor preview, status, and actions visible while a nine-unit roster scrolls inside its own authored `ScrollContainer`. Player-facing role, rarity, and battle-result enums use centralized Chinese presentation mappings; stable ids, enum values, simulation, and saves remain unchanged.
- 2026-08-28: C# signal review closed every `+=` lifecycle with explicit `-=` teardown, including dynamic choice cards. Temporary content nodes in battle setup and UI inspection now use exception-safe cleanup. No remaining critical issue was found against the Godot review checklist.
- 2026-08-28: Final verification evidence passed: low-concurrency build `0 warnings, 0 errors`; `FIXTURE_CONTRACT_OK`; `CONTENT_CONTRACT_OK entries=57 floors=5 events=90`; `UI_SMOKE_OK screens=11 navigation=menu-hero-tower-result-settings`; `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`; and `VISUAL_CAPTURE_OK screens=11 extras=5 flows=reward,recruitment,shop,victory,defeat path=res://.godot/qa`.
- 2026-08-28: Independent verification rejected the handoff on concrete gameplay ownership, lifecycle rollback/abort semantics, incomplete source guards/startup readiness, missing focused gameplay contracts, and several inaccurate player-facing descriptions. Execution resumed under the same confirmed Alpha scope; no new gameplay or content direction is authorized.
- 2026-08-28: Independent-verification findings were closed without changing the accepted product direction. Player-only formation modifiers and kill growth are team-scoped; healers pursue wounded allies; floor rules end exactly once across failure, abort, replacement, teardown, and completion; content activation and save instance identities now fail safely.
- 2026-08-28: Registry creation now requires one hidden, batched real-tree ready-frame pass for every unit, item, and floor scene. Engine errors are captured into the existing validation report, and source guards cover concrete one-segment ids, global/group discovery, parent traversal, and cross-root paths only in the content/runtime ownership layers.
- 2026-08-28: Focused gameplay coverage now verifies formation/aura boundaries, growth/death ordering, non-recursive death summons, hazard/shield death, narrow-lane routing and LOS, healer movement, merchant command economy, all three boss contracts, recruit conversion, settings application, and exactly-once floor lifecycle.
- 2026-08-28: Player-facing consistency was corrected: hero cards show army rules and named commands; recruitment shows Chinese faction and role; deployment names the six formation positions; shops and elites promise only implemented rewards; item/floor/construct descriptions match runtime behavior; unsupported damage-number settings were removed while retaining DTO compatibility.
- 2026-08-28: Final regression passed with zero build warnings/errors, all five automated suites, clean main-scene startup, and refreshed sixteen-image visual evidence. The task is returned to independent verification rather than marked complete.
- 2026-08-28: Independent verification rejected the second handoff. The startup gate did not guarantee an actual `_Process` cycle or capture detach/free errors; source-guard coverage and negative sentinels were incomplete; hero-command effect/cost/gold metadata and Chinese gameplay tags were not fully player-readable; and three focused combat counterexamples plus corresponding UI/manual QA evidence were missing. The same Alpha task resumed for minimal correction with no new gameplay scope.
- 2026-08-28: The startup gate now keeps a scoped engine logger active across attach, `_Ready`, a proven `_Process` cycle, detach, `_ExitTree`, and free. Three real failure fixtures verify that errors from `_Ready`, `_Process`, and `_ExitTree` enter the authoritative report and prevent Registry publication; their `CONTENT_GATE_*` console errors are intentional negative-test evidence.
- 2026-08-28: Production source protection is now a pure checker. Concrete content ids are scanned across every production C# source file, while hidden root/group/parent/cross-root discovery is enforced at the Content/Battle/Components ownership boundary. Negative sentinels cover one-segment ids and each forbidden discovery family without exempting the checker itself.
- 2026-08-28: All eight independently authored command scenes now own player-readable effect metadata; the merchant scene also owns its 5-gold price used by both runtime and presentation. Hero cards show army rule plus actual command effect, merchant battle HUD shows price/balance/success/insufficient-gold state, and recruitment cards present deduplicated Chinese faction/gameplay traits while filtering technical tags.
- 2026-08-28: Focused counterexamples now prove enemies do not inherit player formation armor, lethal hazards emit floor-attributed defeat events, and the second boss summons within its authored 1–2 range. UI smoke covers hero effect text, merchant 5/4-gold invariants, and the abyss crawler's simultaneous `亡灵`/`野兽` labels; manual QA mirrors these checks.
- 2026-08-28: Final handoff matrix passed after the second-verification corrections: build `0 warnings, 0 errors`; all five automated suites; clean main-scene startup; and refreshed sixteen-image visual evidence. `HeroSelectScreen`, `BattleScreen`, and `RecruitmentScreen` were visually reviewed with no clipping or overlap. The task is again returned to independent verification.
- 2026-08-28: Independent architecture verification found one remaining startup-gate blocker: `ContentValidator` installed its engine logger only after all `PackedScene.Instantiate` calls, so a construction/`NotificationSceneInstantiated` `PushError` that did not throw could evade the structured report and allow Registry publication. Minimal correction resumed; no gameplay or content expansion is authorized.
- 2026-08-28: The remaining architecture blocker is closed. The scoped logger now installs before every catalog, floor, or additional scene instantiation and stays active through attach, ready, process, detach, exit, and free; logger installation/removal and every lifecycle-stage exception are converted into the structured report. A fourth real fixture emits `CONTENT_GATE_INSTANTIATE_FAILURE` from `NotificationSceneInstantiated`; its backtrace occurs inside `PackedScene.Instantiate`, and the contract asserts the marker is captured and Registry remains null.
- 2026-08-28: Post-fix handoff matrix passed in required order: build `0 warnings, 0 errors`; Fixture, Content, Gameplay, UI, and AlphaRun contracts; clean main-scene startup; and refreshed VisualCapture. The task is returned to independent verification with only the expected four lifecycle negative-fixture errors in Content output.
- 2026-08-28: Independent gameplay architecture verification found one final decoupling blocker: the eight command scenes owned names and descriptions, but most effective parameters still lived as centralized constants in `HeroCommandContracts.cs`. Minimal resource migration resumed to make each command scene author and validate its existing values, generate its player description from those same parameters, and prove runtime behavior with non-default scene sentinels; no command behavior or balance change is authorized.
- 2026-08-28: Architecture verification also found that `CreateReadyAsync` called catalog/structural validation before entering the logged ready-frame pass. A one-shot error during `ValidateEntry` or floor-rule instantiation could therefore disappear before the second pass and still publish Registry. The same minimal correction must lift one logger scope across structural validation, ready/process validation, cleanup, and report merge, with a real first-pass-only injected scene proving rejection.
- 2026-08-28: All eight independent command scenes now author every existing runtime parameter explicitly. Their content scripts validate those values, generate Chinese effect text from the same fields, and pass the fields into runtime constructors with no production balance defaults. Gameplay coverage loads every real command scene, substitutes non-default sentinels, executes the resulting runtime, verifies every shield/cooldown/summon/tag/heal/damage/freeze/cost multiplier, and confirms invalid authoring is rejected; the merchant's original 5-gold success/4-gold failure test now also uses its real scene.
- 2026-08-28: Registry gating now owns exactly one engine logger across static catalog/structural validation, ready-pass staging and processing, cleanup, and publication decision. A first-pass-only structural fixture reports once from `NotificationSceneInstantiated`; the marker is captured exactly once, the second pass is skipped, and Registry remains null. The existing ready-pass instantiate, ready, process, and exit fixtures remain independently covered.
- 2026-08-28: Final post-resourceization matrix passed in required order: build `0 warnings, 0 errors`; Fixture, Content, Gameplay, UI, and AlphaRun; clean main-scene startup; and refreshed VisualCapture. Hero-card screenshots show the longer generated descriptions without clipping, while UI smoke verifies all eight cards expose the authored numeric effects. The task returns to final independent acceptance.
- 2026-08-28: Final independent acceptance passed. The primary low-concurrency build completed with `0 warnings, 0 errors`; Fixture, Content (all five expected negative markers captured and Registry rejected), Gameplay (`commands=scene-parameters,economy`), UI, AlphaRun, main-scene startup, and VisualCapture all passed. All sixteen screenshots passed screen-by-screen review. Independent `architecture_audit` and `gameplay_audit` both returned `PASS`.

## Skills

- Used for architecture: `using-godot-prompter`, `scene-organization`, `component-system`, `resource-pattern`, `csharp-godot`.
- Execution must load matching skills before implementing each subsystem, including signals/events, abilities, UI, persistence, assets, testing, debugging, and review as applicable.

## Resume Condition

Read this task, both authority documents, current `git status`, and the newest verification evidence. Resume from the first incomplete scope item without redesigning the confirmed direction or expanding non-goals.

Current resume entry: task completed after final independent acceptance; no resume action remains.

## Verification Handoff

Implementation and final independent acceptance are complete. The evidence below is the accepted handoff record.

### Commands And Results

```powershell
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/FixtureContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/ContentContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/UiSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlphaRunSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . --quit-after 5
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --path . tests/VisualCapture.tscn
```

- Build: `0 warnings, 0 errors`.
- Fixture: `FIXTURE_CONTRACT_OK`.
- Content: `CONTENT_CONTRACT_OK entries=57 floors=5 events=90`.
- Gameplay: `GAMEPLAY_CONTRACT_OK combat=team-aura,growth,death,hazard,navigation,healing,bosses commands=scene-parameters,economy lifecycle=exactly-once run=conversion,settings`.
- UI: `UI_SMOKE_OK screens=11 navigation=menu-hero-tower-result-settings hero=command-details recruitment=localized-traits merchant=economy-hud`.
- Runs: `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`.
- Main scene: clean five-second headless startup with no runtime error.
- Visual: `VISUAL_CAPTURE_OK screens=11 extras=5 flows=reward,recruitment,shop,victory,defeat path=res://.godot/qa`.
- Content negative-fixture note: the five intentional `CONTENT_GATE_STRUCTURAL_INSTANTIATE_FAILURE`, `CONTENT_GATE_INSTANTIATE_FAILURE`, `CONTENT_GATE_READY_FAILURE`, `CONTENT_GATE_PROCESS_FAILURE`, and `CONTENT_GATE_EXIT_FAILURE` engine errors are expected console output; the suite confirms each is captured into the report, blocks Registry publication, and still exits successfully with `CONTENT_CONTRACT_OK`.

### Completed Content Map

- 8 independent hero scenes with distinct hero-rule and command scenes.
- 24 independent recruitable soldier scenes, 13 independent enemy/Boss scenes, and 12 independent item scenes.
- 5 independent floor-rule scenes across 3 tower regions and a 15-floor completion path.
- 11 authored Chinese UI screens plus reusable choice-card, unit-animation, health-view, behavior, hero-rule, command, and modifier-provider scenes.
- Versioned meta/settings/active-run persistence, deterministic fixed-tick simulation, route/reward/shop/event/rest flow, and victory/defeat results.

### Visual Evidence

- The eleven screen captures are `MainMenuScreen.png`, `HeroSelectScreen.png`, `TowerScreen.png`, `DeploymentScreen.png`, `BattleScreen.png`, `RewardScreen.png`, `ShopScreen.png`, `EventScreen.png`, `RestScreen.png`, `ResultScreen.png`, and `SettingsScreen.png` under `C:\Users\qs\godot\my-team\.godot\qa\`.
- Focused evidence adds `RecruitmentScreen.png`, `BattleHazard.png`, `BattleBossWard.png`, `ResultVictory.png`, and `ResultDefeat.png` in the same directory.

### Known Limitations And Provenance

- This is an Alpha slice: balance, encounter duration, final art/audio polish, accessibility breadth, and commercial release certification remain outside this task.
- Visual capture requires a rendered desktop run; the four contract/run suites are headless.
- The 35 copied unit-animation packages remain local repository assets with exact OpenDuelyst CC0 1.0 license and source/hash records under `assets/provenance/`. `D:\godot\rpg` remains read-only and is not a runtime dependency.

### Remaining Work

- No implementation or verification work remains within this task. The Alpha limitations and later product work listed under Known Limitations And Provenance remain explicitly outside this completed scope.
