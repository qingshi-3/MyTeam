# Window Baseline And Unit Portrait UI

Status: Completed  
Confirmed: 2026-08-29

## Goal

Restore the intended desktop visual scale and make unit identity immediately readable wherever the player chooses or reviews units. The game should start in a normal resizable 1600×900 window, preserve 1280×720 as a supported lower bound, and present all heroes, soldiers, and enemies through reusable, independently cropped unit portraits without changing combat, progression, or non-unit choice cards.

## Confirmed Solution

### Default window and responsive baseline

- Start in a normal resizable `1600×900` desktop window.
- Startup is explicitly windowed: not fullscreen and not maximized. The implementation and automated contract must not depend on the user's previous editor/window state.
- Keep the design viewport at `1600×900` and retain Godot stretch settings `canvas_items` plus `expand`.
- Treat `1600×900` as the intended presentation baseline and `1280×720` as the supported compatibility and acceptance lower bound. Layouts must adapt through authored containers, anchors, size flags, and scroll ownership rather than through a second fixed layout or runtime-built control tree.

### Resource-authored unit portraits

- Add a reusable authored `UnitPortrait` component scene with a narrow binding API. It renders unit portrait presentation only and does not own selection, content lookup, gameplay facts, or run state.
- Add an independently tunable portrait resource for every concrete unit: 8 heroes, 24 soldiers, and 13 enemies, for 45 portrait resources total.
- Each portrait resource reuses the unit's existing local animation art. No image generation and no bespoke replacement illustration are part of this task.
- Each unit's crop must be editable in isolation without changing its battle animation scene or every consuming UI. The resource contract must support the source/frame selection and the minimum crop/zoom/offset controls needed to remove transparent padding and center the readable body. Any facing normalization belongs to portrait presentation and must not alter battle-facing behavior.
- Missing or invalid authored portrait data fails content/presentation validation visibly. A semantic role fallback may protect non-production or diagnostic rendering, but every one of the 45 production units must ship with its own validated portrait resource.
- The same `UnitPortrait` component is reused with context-authored sizing in hero selection, recruitment, deployment lists, Army details, and battle-report rows. Consumers bind an existing unit/content view model; they do not recrop textures independently or copy portrait extraction logic.

### Unit-specific choice hierarchy

- Add an authored unit-specific selection-card scene for unit choices. Do not globally enlarge or reshape the shared general-purpose `ChoiceCard`.
- Hero selection and recruitment use the unit-specific card where a large readable portrait and unit facts are the primary decision. Route choices, item reward/shop cards, and other non-unit choices retain the existing `ChoiceCard` contract and current sizing.
- The unit card remains one focusable typed choice with mouse, keyboard, and gamepad activation, disabled state, tooltip behavior, and exactly one stable-id selection signal. Its child hierarchy separately owns portrait, name, responsibility/role, gameplay tags, key attributes, and contextual footer/meta.
- Long descriptions must not collapse the portrait or displace essential role/tag/stat facts. Color remains semantic support rather than the only carrier of meaning.

### Recruitment layout

- Recruitment presents its three unit choices in one vertical column rather than three compressed cards across the width.
- Each recruitment choice is approximately `148–150` pixels high at the 1600×900 design baseline, with an approximately `104–108` pixel portrait region.
- Information is layered in this order: portrait and unit name; responsibility/precise role; localized gameplay tags; compact key attributes; contextual action/meta.
- The bottom skip or hero-authored conversion action remains fixed and visible while the three-choice region owns any scrolling required at 1280×720. Choosing or scrolling cards must not move, cover, or disable the bottom action.

## Read-Only Audit And Root Cause

- `project.godot` already authors a `1600×900` design viewport with `window/stretch/mode="canvas_items"` and `window/stretch/aspect="expand"`, but overrides the startup window to `1280×720`. The default launch therefore displays the entire design at a `0.8` scale before the player changes the window, making every card, font, icon, and spacing value appear smaller than the authored baseline.
- The shared `ChoiceCard.tscn` is a general-purpose `250×112` button whose icon/portrait slot is only `58×58`. Unit portrait extraction currently feeds that generic slot.
- Existing animation textures contain substantial transparent padding around differently sized bodies. Fitting the full frame into a 58-pixel slot without a unit-authored crop reduces the visible character body to roughly `16–36` pixels in representative assets. The primary defect is the combination of the 0.8 startup scale, a small generic icon slot, and uncropped transparent animation bounds—not missing source art.
- The catalog contains exactly 45 concrete unit scenes in scope: 8 heroes, 24 soldiers, and 13 enemies. Existing reusable portrait resolution is distributed between unit content, `GameRoot`, and battle-report fallback handling; the new component/resource contract must centralize presentation without adding concrete-id dispatch.
- Existing authority already requires authored reusable UI scenes, independent unit content, structured cards, semantic Theme use, and acceptance at both 1600×900 and 1280×720. This solution extends those contracts rather than replacing them.

## Authority Impact

- Before runtime/resource implementation, update `gameplay-design/tower-autobattler-core.md` with the 1600×900 default window, 1280×720 lower bound, unit portrait readability, unit-specific selection hierarchy, and single-column recruitment behavior.
- Update `system-design/tower-autobattler-architecture.md` with window/stretch ownership, `UnitPortrait` scene/resource ownership, the 45-resource validation/catalog boundary, shared context binding, and the separation between unit cards and general `ChoiceCard`.
- Extend `docs/testcases/alpha-manual-qa.md` with normal-window startup, resizing, portrait crops across all contexts, recruitment fixed-action behavior, focus/input, and dual-resolution checks.
- This work does not change the independent concrete unit/item scene contract. Portrait resources are read-only presentation data and must never receive mutable run or battle state.

## Scope

- Window configuration required to launch at normal resizable 1600×900 while retaining the existing design viewport and `canvas_items + expand` behavior.
- A reusable authored `UnitPortrait` scene/component, its read-only portrait-definition resource contract, content binding/validation, and stable fallback behavior.
- Forty-five independently tunable portrait resources derived from the existing local animation assets: 8 hero, 24 soldier, and 13 enemy portraits.
- A reusable authored unit-selection card and the focused binding/view-model changes needed by hero selection and recruitment.
- Single-column three-choice recruitment layout, fixed skip/conversion action, responsive card scrolling, and localized name/role/tag/stat hierarchy.
- Reuse of `UnitPortrait` in hero selection, recruitment, deployment lists, Army details, and battle-report unit rows with context-specific authored sizes.
- Focused content/UI/responsive contracts, dual-resolution visual evidence, authority/manual-QA updates, full existing regression, and independent verification.

## Non-Goals

- No global size increase or redesign of the general-purpose `ChoiceCard`.
- No change to route cards, item reward cards, shop item cards, item artwork, or item choice flow.
- No change to battle-board unit sprites, animation timing/cues, motion presentation, facing, health/readability markers, selection hit testing, or battlefield layout.
- No gameplay values, hero/soldier/enemy mechanics, recruitment odds, rewards, route generation, encounter composition, combat balance, save schema, or mutable run-state change.
- No new heroes, soldiers, enemies, items, animations, bespoke illustrations, generated images, or donor-project dependency.
- No fullscreen-first, maximized-first, fixed-size-only, or mobile layout work.
- No runtime construction of complete UI trees and no concrete unit-id switch for portrait selection.

## Hard Constraints

- Work only on `main`; do not create or switch branches and do not commit unless separately authorized.
- Preserve the existing all-untracked workspace and unrelated user files.
- `D:\godot\rpg` remains read-only and is not a runtime dependency. Reuse only the animation assets already present inside this repository.
- Use authored `.tscn`, `.tres`, Theme variations, containers, anchors, and size flags. Controllers may instantiate and bind authored templates but must not assemble replacement interfaces ad hoc.
- Every concrete hero, soldier, enemy, item, and command scene remains independently instantiable. Portrait resources cannot introduce a hidden GameRoot, level, UI, or autoload dependency.
- Each of the 45 production units owns one independently editable portrait resource. Shared mutable crop state or runtime writes into resources are forbidden.
- Preserve the existing semantic color/icon hierarchy, Chinese player-facing labels, typed stable-id selection, and mouse/keyboard/gamepad focus contracts.
- Preserve the 1600×900 design viewport, `canvas_items + expand`, and usable 1280×720 lower bound. Do not solve scaling by changing the simulation, camera, battle board, or global UI scale at runtime.
- Do not edit generated `.godot/imported` files. Original local resources remain authoritative.
- During later execution, avoid concurrent Godot/import/build processes, use the required low-concurrency build command, and do not interfere with a user-open Godot editor or game process.

## Acceptance Criteria

### Window behavior

- A fresh game launch opens as a normal, resizable 1600×900 window and is neither fullscreen nor maximized.
- The design viewport remains 1600×900 with `canvas_items + expand`; resizing to 1280×720 preserves usable layouts without overlap, inaccessible actions, or a second fixed-layout branch.
- Existing settings/save data remain compatible and cannot silently force the confirmed default into fullscreen or maximized startup.

### Portrait authoring and coverage

- A reusable `UnitPortrait` authored scene binds portrait presentation without content-id branching or gameplay ownership.
- Exactly 45 production portrait resources exist and validate against the catalog's 8 heroes, 24 soldiers, and 13 enemies; no production unit falls back because its authored resource is missing.
- Every portrait resource can be opened and adjusted independently. Representative small, tall, wide, mounted, hero, soldier, and enemy assets visibly remove transparent padding and center the unit without modifying battle animation frames.
- Portrait binding does not mutate shared resources, battle-facing state, animation playback, content definitions, run state, saves, or combat digest.

### Unit cards and recruitment

- Hero selection and recruitment use the unit-specific authored card with a readable large portrait and separate name, role/responsibility, localized tags, attributes, and meta regions.
- Recruitment shows exactly three choices in one vertical column. At 1600×900 each card is approximately 148–150 pixels high with a 104–108 pixel portrait region.
- At both 1600×900 and 1280×720, all three choices remain inspectable and the bottom skip/conversion action stays fixed, visible, focusable, and clickable while only the choice list scrolls when needed.
- Multi-tag units retain every localized gameplay tag, technical tags remain hidden, and long Chinese descriptions do not overwrite role/tag/stat facts.
- Mouse, keyboard, and gamepad traversal, disabled heroes, tooltips, and stable-id activation continue to produce exactly one typed selection.

### Cross-context reuse

- The same `UnitPortrait` component/resource path renders the appropriate authored crop in hero selection, recruitment, deployment lists, Army details, and battle-report rows.
- Each context controls only its authored display size/presentation variant; it does not duplicate crop coordinates or portrait extraction logic.
- Route and item cards retain the existing general `ChoiceCard` size and behavior, proving the unit-card change is not a project-wide enlargement.

### Verification

- Static/source validation proves 45/45 portrait coverage, no concrete-id portrait dispatch, no generated-import edits, and no gameplay/save/battle-board scope drift.
- Focused UI/content contracts cover startup window flags and size, portrait resource validation, independent crop tuning, all five consuming contexts, recruitment ordering/scroll ownership/fixed action, and typed focus/selection behavior.
- Low-concurrency build and the complete existing Fixture, Content, Gameplay, MovementPresentation, UI, AlphaRun, visual-capture, and short main-scene startup matrix pass with documented expected warnings only.
- Visual evidence at 1600×900 and 1280×720 includes hero selection, recruitment with all three choices plus the fixed bottom action, deployment list, Army details, and battle-report rows using representative hero/soldier/enemy body shapes.
- Independent verification reviews window state, resource coverage, crop readability, responsive/focus behavior, unchanged general cards, scope boundaries, and both resolution sets before completion.

## Progress

- 2026-08-29: User confirmed the normal 1600×900 window, resource-authored portrait, unit-specific card, single-column recruitment, five-context reuse, and 45-unit coverage solution. No implementation is authorized outside this confirmed scope.
- 2026-08-29: Read-only authority review completed across project rules, gameplay/system authority, manual QA, the completed Alpha/playability/movement work items, and the completed battle-report/visual-hierarchy task. Existing independent-scene, authored-template, semantic Theme, focus, report-row, and dual-resolution contracts remain authoritative.
- 2026-08-29: Read-only root-cause audit confirmed the project authors a 1600×900 design viewport but launches at a 1280×720 override, producing a default 0.8 presentation scale. The generic unit path then fits transparent-padded animation art into a 58×58 `ChoiceCard` slot, leaving representative visible bodies at roughly 16–36 pixels.
- 2026-08-29: Catalog scope confirmed as 8 hero scenes, 24 soldier scenes, and 13 enemy scenes, for 45 independently cropped production portrait resources. Existing local animation assets are sufficient; image generation is neither required nor authorized.
- 2026-08-29: Activity document created as the only state change in this turn. No code, scene, resource, authority, QA, project setting, import, build, Godot, branch, commit, or external-process action has occurred.
- 2026-08-29: Execution resumed on `main` with the existing all-untracked workspace preserved. Architecture preflight assigns startup scale to project configuration, crop ownership to read-only `UnitPortraitDefinition` resources, portrait rendering to one authored `UnitPortrait` component, unit decision hierarchy to a separate authored `UnitChoiceCard`, and consumer binding to existing UI composition roots. Battle simulation, battle-board presentation, saves, and general route/item `ChoiceCard` remain outside the change.
- 2026-08-29: Gameplay, system, and manual-QA authority now record the normal resizable 1600×900 launch, 1280×720 lower bound, 45-resource portrait contract, five-context reuse, unit-specific choice hierarchy, single-column recruitment, and fixed skip/conversion action before runtime or resource implementation.
- 2026-08-29: Focused RED authored as `WindowPortraitContractSmoke`: it checks the explicit normal-window baseline and requires separate authored `UnitPortrait`/`UnitChoiceCard` scenes while reporting the existing general `ChoiceCard` unit slot size. The RED is ready to compile and run before production changes.
- 2026-08-29: Focused RED executed after a zero-warning build and failed as intended: startup overrides were `1280×720` instead of `1600×900`, `UnitPortrait`/`UnitChoiceCard` were absent, and the only unit path was the general `ChoiceCard`'s `58×58` slot. The same contract is now GREEN with explicit normal windowed/resizable settings while preserving the 1600×900 viewport and `canvas_items + expand`.
- 2026-08-29: Portrait/content milestone is GREEN. `UnitPortraitDefinition` owns read-only SpriteFrames animation/frame, zoom, normalized offset, and facing normalization; `UnitPortrait` renders that data without content lookup or gameplay ownership. Exactly 45 external portrait resources are bound one-to-one to 8 heroes, 24 soldiers, and 13 enemies, and Content validation rejects missing, duplicate, mismatched, invalid, or out-of-catalog portrait resources. Focused Content exits zero with `portraits=45(8,24,13)` and the five expected negative lifecycle fixtures.
- 2026-08-29: Consumer/UI milestone is GREEN in focused smoke. Hero selection and recruitment use authored `UnitChoiceCard`; recruitment owns a single `ScrollContainer > VBoxContainer` of three 150px cards with 106px portraits while bottom actions remain outside the scroll. Deployment cards, Army detail rows, and battle-report rows bind the same portrait definitions at context-authored sizes. UI smoke verifies all five contexts, fixed recruitment actions, localized multi-tags, typed selection/focus, and unchanged structured route cards.
- 2026-08-29: Dual-resolution visual review covered hero selection, recruitment, deployment, Army details, and player/enemy battle reports at both 1280×720 and 1600×900. It found two scoped presentation defects: long hero mechanics touched the 150px card edge, and `soldier_aegis_guard` at zoom 1.9 rendered as an oversized shield fragment. The soldier's independent portrait zoom is now 1.1, and hero selection uses a shallow authored `HeroUnitChoiceCard` variant at 190px with four visible description lines; recruitment retains the shared 150px/106px baseline. Refreshed captures show complete on-card army rule and command text, readable representative portraits, three recruitment cards, and the fixed action at both resolutions.
- 2026-08-29: Focused post-adjustment verification is GREEN: low-concurrency build completed with 0 warnings and 0 errors; `WINDOW_PORTRAIT_CONTRACT_OK`; and `UI_SMOKE_OK` with the existing test-only modal focus warning. The focused contract now requires the inherited hero presentation variant, while UI smoke requires visible rule/command labels in its authored four-line region. Two subsequent dual-resolution VisualCapture runs passed; one intervening 1600×900 run hit the pre-existing real-production-hitch timing assertion, then passed unchanged on the next serial run and remains recorded as a nondeterministic verification risk rather than a portrait/UI code change.
- 2026-08-29: The full matrix first exposed that the legal `fixture_unit` still lacked the now-required external portrait. A test-only `fixture_unit_portrait.tres` now satisfies the same independent authoring contract without entering the production portrait directory or changing the 45-resource count; focused Fixture returned `FIXTURE_CONTRACT_OK`.
- 2026-08-29: Final serial regression is GREEN: build 0 warnings/0 errors; WindowPortrait; Fixture; Content with all five intentional negative lifecycle markers and `portraits=45(8,24,13)`; Gameplay; MovementPresentation; UI with its existing test-only focus warning; AlphaRun; VisualCapture at 1280×720 and 1600×900; and a clean five-frame main-scene startup. A final screen-by-screen review of the twelve required captures found no overlap, inaccessible action, hero-mechanics clipping, or unreadable representative portrait.
- 2026-08-29: Final `godot-code-review` found no blocker. Scenes retain one responsibility and shallow inheritance; `UnitPortrait` has no content lookup or per-frame work; all new C# nodes are typed partial classes with cached child references; choice signals are connected and disconnected explicitly; resources are never mutated at runtime; and the production UI contains no concrete hero/soldier/enemy-id dispatch. The general choice card, battle board, simulation, values, saves, source animations, donor boundary, and generated-import sources remain outside the change.
- 2026-08-29: Idle MSBuild/Roslyn servers were shut down after verification. The user's visible Godot editor remains untouched at PID 23260. No branch, commit, donor-project write, or generated `.godot/imported` edit was made.
- 2026-08-29: Independent visual verification rejected the handoff. In both `.godot/qa/UI_1280x720_DeploymentScreen.png` and `.godot/qa/UI_1600x900_DeploymentScreen.png`, every left-roster `DeploymentUnitCard` clips the right end of `UnitFacts`, losing the complete responsibility/range fact and its distance value. Static review confirms the authored width chain is insufficient: `RosterPanel` is 270px, the card is 250px, and after 72px portrait plus horizontal margins/separation the single-line, non-wrapping facts label receives only about 154px before the scroll bar. The task resumes for a narrow deployment-layout/test correction only; portrait reuse, the other four consumers, battle board, gameplay, and general choice cards remain frozen.
- 2026-08-29: Focused rendered-layout RED now instantiates the real deployment flow, waits for container layout, measures each `UnitFacts` string with its resolved Theme font, and compares that width against the intersection of the label, card, and roster-scroll rects. The unchanged layout failed as required: `生命 100% · 远程 · 远程 3.5` needs 194px while only 182px is visibly authored. This proves clipping geometrically rather than merely asserting that the full value remains in `Text`.
- 2026-08-29: A second independent scope audit rejected the shared reward layout. `RewardScreen.tscn` was enlarged from the established ordinary-reward baseline of about 900×650 to 980×760 so recruitment could hold three vertical unit cards, but ordinary item rewards and recruitment still route through that same screen and choice container. This widens the ordinary reward parent and stretches general `ChoiceCard` instances, violating the confirmed non-goal that item reward presentation/current sizing remains unchanged. The narrow correction must split authored ordinary-reward and recruitment layouts while reusing the existing reward business commands; ordinary battle victory must still route to the ordinary reward screen and tower recruitment nodes to the recruitment variant.
- 2026-08-29: Focused reward-layout RED exercises the actual ordinary reward mode before recruitment. The unchanged shared screen failed with `panel=(980, 760), cards=944,944,944` against the established ordinary 900×650 panel and no-more-than-865px visible-card allocation. The same contract also requires a separate 980×760 recruitment scene with three unit cards and fixed actions, so the eventual GREEN cannot simply shrink the shared screen.
- 2026-08-29: The deployment rendered-layout RED failed on the real longest visible fact: `生命 100% · 远程 · 远程 3.5` required 194px while only 182px intersected the card and roster-scroll viewport. The authored `RosterPanel` width is now 340px instead of 270px; the existing 72px portrait, fact content, battle grid, controller, and deployment behavior remain unchanged. The same geometric contract is GREEN.
- 2026-08-29: The ordinary/recruitment isolation RED failed on the real shared layout at `panel=(980, 760), cards=944,944,944`. `RewardScreen` is restored to its established 900×650 ordinary-reward baseline, while a shallow authored `RecruitmentScreen` inherits its structure and overrides only the recruitment panel to 980×760. `GameRoot` now routes ordinary victories to `RewardScreen` and recruitment tower nodes to `RecruitmentScreen`; both screens reuse the same existing claim, skip, conversion, and Army-overlay commands. UI smoke verifies 13 application screens, ordinary cards at no more than 865px, three 150px recruitment cards, and fixed recruitment actions.
- 2026-08-29: The architecture contract now states the actual portrait ownership precisely: `UnitPortraitDefinition` owns SpriteFrames source, animation/frame, zoom, normalized offset, and horizontal flip, while each consumer-authored square `UnitPortrait` owns its visible clipping rect and display size. Definition fingerprinting now includes the portrait SpriteFrames resource path; a focused synthetic contract proves changing only that path changes the fingerprint without modifying production content.
- 2026-08-29: Final focused serial verification is GREEN: low-concurrency build reports 0 warnings and 0 errors; WindowPortrait, Fixture, Content, and UI contracts pass. Content remains exactly `portraits=45(8,24,13)`, and UI reports `screens=13` with isolated ordinary reward and recruitment layouts. The existing intentional modal-focus warning remains non-failing.
- 2026-08-29: Fresh VisualCapture runs at 1280×720 and 1600×900 both exited zero. Manual review of the six refreshed Deployment, Reward, and Recruitment screenshots confirms complete deployment fact tails, the ordinary 900×650 reward presentation, the independent 980×760 recruitment presentation, three single-column unit cards, fixed bottom actions, and no simultaneous visibility of the two reward-mode screens.
- 2026-08-29: Idle MSBuild and Roslyn compiler servers were shut down after the final captures. The user's Godot editor at PID 23260 was not controlled, closed, or modified; no branch, commit, donor-project write, or generated-import edit occurred.
- 2026-08-29: Second-round independent runtime verification PASS. WindowPortrait, Content, and UI each exited zero; UI reported `screens=13` and `isolated-screen`. The only diagnostic was the existing intentional modal-focus warning, and the verification runs left no residual Godot, .NET, MSBuild, or compiler-server process. The user's editor process remained outside the verifier's control.
- 2026-08-29: Second-round independent visual verification PASS at both 1280×720 and 1600×900. Deployment retains every numeric fact tail while preserving the battle grid and bottom operations; ordinary Reward remains 900×650; Recruitment remains independently authored at 980×760; and the refreshed evidence shows no responsive-layout regression.
- 2026-08-29: Second-round independent scope verification PASS. Both blocking findings and both non-blocking review observations are closed, with no gameplay, simulation, progression, save, battle-board, general `ChoiceCard`, donor, generated-import, or mutable-resource drift. The task is complete and ready for archive.

## Current State And Resume Condition

Completed and independently accepted on `main`. The confirmed window baseline, 45 independently authored portraits, five-context portrait reuse, unit-specific choice hierarchy, single-column recruitment, deployment fact fit, and ordinary/recruitment layout isolation all meet their acceptance contracts. No execution resume is required; later changes should begin as a new work item against the accepted gameplay and system authority.

## Verification Handoff

Independent verification accepted the final implementation, focused corrections, visual evidence, and scope boundaries. This section is retained as the completed evidence handoff for archive history.

### Changed surface

- Authority and QA: `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, and `docs/testcases/alpha-manual-qa.md`.
- Window: `project.godot` now explicitly authors a normal resizable 1600×900 window while preserving the 1600×900 viewport and `canvas_items + expand`.
- Portrait contract: `src/Content/UnitPortraitDefinition.cs`, `src/Content/UnitDefinition.cs`, `src/Content/ContentValidator.cs`, `src/Content/DefinitionFingerprint.cs`, `src/Content/UnitContentRoot.cs`, `src/UI/UnitPortrait.cs`, and `scenes/ui/components/UnitPortrait.tscn`.
- Production manifest: all 45 unit definitions reference one unique external resource under `content/portraits/`: 8 files in `heroes`, 24 in `soldiers`, and 13 in `enemies`. `soldier_aegis_guard.tres` received the final visual zoom adjustment to 1.1.
- Choice UI: `src/UI/UnitChoiceCard.cs`, `scenes/ui/components/UnitChoiceCard.tscn`, the inherited `scenes/ui/components/HeroUnitChoiceCard.tscn`, ordinary `scenes/ui/RewardScreen.tscn`, recruitment-only `scenes/ui/RecruitmentScreen.tscn`, `scenes/app/GameRoot.tscn`, and `src/App/GameRoot.cs`.
- Reuse consumers: deployment model/card, Army overview model/row, and battle-report screen/row C# plus their authored component scenes under `src/UI/` and `scenes/ui/components/`.
- Focused corrections: `scenes/ui/DeploymentScreen.tscn` widens only the roster panel; `tests/UiSmoke.cs` measures real rendered deployment facts and isolates ordinary reward from recruitment; `tests/VisualCapture.cs` captures the real recruitment screen and rejects simultaneous reward-screen visibility.
- Contracts and evidence: `tests/WindowPortraitContractSmoke.*`, `tests/ContentContractSmoke.cs`, `tests/UiSmoke.cs`, `tests/VisualCapture.cs`, and the updated legal unit fixture plus its test-only portrait under `tests/fixtures/`.

### Commands and accepted markers

```powershell
$godotExe = 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
& $godotExe --headless --path . tests/WindowPortraitContractSmoke.tscn
& $godotExe --headless --path . tests/FixtureContractSmoke.tscn
& $godotExe --headless --path . tests/ContentContractSmoke.tscn
& $godotExe --headless --path . tests/UiSmoke.tscn
& $godotExe --path . --resolution 1280x720 tests/VisualCapture.tscn
& $godotExe --path . --resolution 1600x900 tests/VisualCapture.tscn
```

- Build: `0 warnings`, `0 errors`.
- Window: `WINDOW_PORTRAIT_CONTRACT_OK window=1600x900,windowed,resizable portraits=authored-unit-card`.
- Fixture: `FIXTURE_CONTRACT_OK`.
- Content: `CONTENT_CONTRACT_OK entries=57 floors=5 events=90 portraits=45(8,24,13)` plus the five intentional `CONTENT_GATE_*_FAILURE` probes.
- UI: `UI_SMOKE_OK screens=13 ... recruitment=localized-traits,single-column,fixed-actions,isolated-screen ... portraits=hero,recruitment,deployment,army,report` with the pre-existing intentional modal-focus warning.
- Both fresh visual runs: `VISUAL_CAPTURE_OK ... flows=report,reward,recruitment,shop,victory,defeat ... path=res://.godot/qa`.
- Gameplay, MovementPresentation, AlphaRun, and clean startup were already GREEN in the preceding full matrix. They were not rerun for these two authored-layout corrections because the correction scope freezes gameplay, simulation, progression, and startup behavior.
- Independent second-round runtime: WindowPortrait, Content, and UI all exited zero; UI retained `screens=13` and `isolated-screen`, with only the existing modal-focus warning and no residual verification process.

### Visual evidence and review result

The refreshed correction evidence is under `.godot/qa/`:

- `UI_1280x720_DeploymentScreen.png` and `UI_1600x900_DeploymentScreen.png`: every visible roster row retains its complete health, responsibility, near/ranged classification, and numeric reach; the battle grid and deployment actions remain usable.
- `UI_1280x720_RewardScreen.png` and `UI_1600x900_RewardScreen.png`: ordinary item rewards use the restored 900×650 panel and established general-card allocation.
- `UI_1280x720_RecruitmentScreen.png` and `UI_1600x900_RecruitmentScreen.png`: recruitment uses its independent 980×760 panel, three 150px unit cards in one vertical column, and a fixed visible bottom action.
- The real navigation/capture flow rejects simultaneous visibility of ordinary Reward and Recruitment screens; the Army overlay remains available from either composition.
- Earlier required HeroSelect, ArmyDrawer, and BattleReport evidence remains under the same directory and was not altered by the two narrow corrections.

### Static boundary evidence and remaining review

- Filesystem and Content validation both report exactly 45 production portraits and 45/45 unit-definition bindings, grouped 8/24/13. No concrete unit ID occurs in production portrait/UI dispatch.
- Portrait fingerprinting includes the SpriteFrames resource path, and the architecture document no longer claims an unimplemented portrait crop-size field.
- No runtime source or project setting references `D:\godot\rpg`; no external absolute dependency was introduced. No source animation or generated `.godot/imported` file was edited.
- Focused window and UI checks prove the general `ChoiceCard` remains 250×112 with a 58×58 icon slot, the ordinary reward baseline is 900×650, and recruitment alone owns 980×760. Gameplay, MovementPresentation, AlphaRun, fingerprints, and clean startup provide preceding regression evidence for battle-board behavior, simulation/digest, progression, and saves.
- Optional player-experience follow-up: the user may personally feel the fresh native 1600×900 window, manually resize between 1600×900 and 1280×720, and traverse the UI with mouse, keyboard, and gamepad. These experiential checks are recommendations, not completion blockers.
- Historical non-blocking note: one intermediate 1600×900 VisualCapture invocation failed the pre-existing real-production-hitch timing assertion, then passed unchanged on every subsequent serial run, including the final matrix and independent acceptance. If it ever recurs, investigate it as movement-test nondeterminism rather than reopening this completed portrait/UI scope.
