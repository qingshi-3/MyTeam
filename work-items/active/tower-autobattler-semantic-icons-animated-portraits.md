# Semantic Icons And Animated Unit Portraits

Status: Awaiting Verification  
Confirmed: 2026-08-29  
Visual hierarchy correction confirmed: 2026-08-30

## Goal

Make dense unit and tower information readable at a glance without removing Chinese text. Unit statistics, responsibilities, factions, traits, and tower-node identities must use differentiated, reusable semantic icons. Unit portraits must play their existing authored idle animation in UI contexts instead of freezing on a single frame, while remaining independent from battlefield animation state.

The icon rollout alone did not meet visual acceptance: replacing glyphs with small inline icons left the hero-selection screen as a dense text list. The confirmed correction must establish mature-game information hierarchy, semantic color, and a scalable hero-library workflow rather than applying isolated font-color patches to the rejected layout.

## Confirmed Visual Hierarchy Correction

### Hero library and detail panel

- Replace the full-width stack of verbose hero rows with a responsive master-detail composition suitable for a growing hero roster.
- The library side uses compact, focusable hero tiles with the existing animated portrait, hero name, responsibility, and faction/trait identity. It wraps responsively rather than embedding every rule and command in every tile.
- The detail side presents the currently focused/selected hero with a larger animated portrait, name and identity badges, visually separated core-stat blocks, army rule, battlefield-command panel, structured cost badge, lock/availability state, and one primary `以该英雄出征` action.
- Moving focus changes preview only. The primary action emits the existing stable hero id exactly once. Preserve mouse, keyboard, gamepad, disabled-state, tooltip, and return-navigation behavior.
- At 1600×900 the layout should comfortably expose a multi-column hero library and detail panel. At 1280×720 it may reduce library columns and internal spacing, but the detail action and critical facts remain visible; scroll ownership must be explicit and must not move the fixed primary actions off-screen.

### Reusable visual vocabulary

- Keep the existing semantic-icon catalog as the single icon source, but stop using one undifferentiated inline chip for every information class.
- Add focused authored component scenes for these distinct responsibilities:
  - `StatBlock`: prominent icon and value with a subordinate Chinese stat label;
  - `TraitBadge`: compact responsibility/faction identity with restrained backing and outline;
  - `ResourceCostBadge`: strongly localized mana/gold or other structured cost;
  - `HeroAbilityPanel`: command icon/name, structured cost badge, and neutral Chinese effect copy.
- Author structure and styles in `.tscn`, `.tres`, and the shared Theme. Runtime code binds typed presentation data and instantiates templates; it must not construct whole control trees, parse prose with regular expressions, or dispatch on concrete hero ids.
- The compact existing `SemanticChip` remains valid for dense deployment, Army, selected-unit, tower-risk, and report contexts. Consumers choose the appropriate presentation component while retaining one semantic key and icon resource per meaning.

### Semantic palette and text discipline

- Centralize semantic colors in Theme type variations. Required high-salience meanings are: health green, attack/damage red, mana blue, shield steel gray, healing teal-green, gold gold, range light cyan, danger/death crimson, risk amber, and hero identity gold.
- The icon and primary numeric value carry semantic color. The subordinate Chinese label is quieter; paragraphs, army rules, and effect prose remain neutral. Responsibility and faction badges use restrained identity treatment so the screen does not become a rainbow of equally loud text.
- Color is never the only carrier: every value retains an icon and/or Chinese label. The palette must remain legible on the existing dark surfaces and in normal, hover, focus, pressed, locked, and disabled states.
- Costs such as `1 MP` are structured blue mana badges adjacent to the command name. Do not leave them embedded in neutral prose and do not color them by matching substrings. Gold command costs use the shared gold semantic in the same component contract.
- Correct the current misuse where attack/damage and several unrelated facts inherit the generic blue `PlayerLabel`; presentation roles must reflect their stable semantic meaning across hero selection, recruitment, deployment, Army details, selected-unit facts, command HUD, and battle reports.

### Context-specific hierarchy

- Recruitment remains exactly three choices in one vertical column, but each row uses a compact mature-game hierarchy: readable portrait and identity badges, visually separated health/attack/range values, short neutral description, and one clear recruit action.
- Deployment and Army rows keep their compact footprint; they use small badges/chips rather than hero-detail stat blocks and must not reduce board space or fixed action visibility.
- Battle reports continue to use the shared semantic palette, semantic icons, Chinese labels, and independent portrait contract. Their former tabular-layout constraint is superseded by the separately confirmed dynamic battle-report work item; this task no longer owns report information architecture or roster layout.
- The command HUD uses the same structured mana-cost treatment and palette as the hero detail without changing command transactions, mana ownership, or gameplay.

### Interaction polish

- Library tiles and choice rows must have clear authored normal, hover, focused, selected, locked, and disabled states. Selected hero identity uses the existing gold language without relying on color alone.
- Keep motion restrained: the existing portrait idle is the primary ambient animation. Optional hover/focus transitions may use short interruptible Tween feedback, but no continuous layout motion or effect may compete with readability.

## Confirmed Solution

### One semantic icon vocabulary

- Introduce one read-only, validated semantic-icon catalog resource. A semantic meaning resolves to one icon and optional presentation role in that catalog; screens and controllers must not duplicate SVG paths or maintain their own switches.
- Preserve Chinese labels alongside icons. Color supports meaning but is never the only carrier.
- Reuse the existing health, damage, shield, healing, mana, gold, time, kills, deaths, hero, melee, ranged, risk, and loot icons where their meanings already match.
- Add differentiated tintable SVG icons for exact attack reach, all eight unit responsibilities, all eight factions, and all seven tower-node types:
  - responsibilities: vanguard, fighter, ranged, support, assassin, summoner, artillery, boss;
  - factions: order, desert, undead, beast, machine, frost, neutral, enemy;
  - tower nodes: combat, elite, recruitment, shop, event, rest, boss.
- Gameplay tags that carry the same faction meaning reuse the matching faction icon. No screen-specific substitute is allowed for the same semantic.
- Tower cards use the node-type icon as their primary identity. Risk remains a separate risk-labelled fact and no longer substitutes for node identity.

### Authored semantic chip component

- Add a focused reusable `.tscn` chip component containing an icon and Chinese text, suitable for compact facts and wrap-capable groups.
- Repeated facts are instances of that authored template. Controllers bind semantic keys and values; they do not construct replacement control trees in code.
- The component must support contextual text/theme variation without changing the globally resolved icon.

### Unit information hierarchy

- Refactor the authored unit-choice card into:
  1. unit name, hero/lock/selection metadata;
  2. responsibility and faction/trait chips;
  3. health, damage, and exact attack-reach chips;
  4. neutral description, army rule, and battlefield-command copy.
- Remove the current hero-card duplication where health and damage appear both in the fact region and the header/footer metadata. Locked state remains visible without duplicating core stats.
- Reuse the same semantics in recruitment, deployment roster facts, Army details, battle-report unit rows, and the in-battle selected-unit panel where those facts already appear. Contexts may choose size and which relevant facts to show, but may not redefine icons.
- Do not replace long prose or action explanations with icon-only presentation.

### Independent animated portraits

- Extend the existing authored `UnitPortrait` component to use an independent UI `AnimatedSprite2D` instance bound to the portrait definition's existing `SpriteFrames` and animation name.
- Portrait playback starts from the authored frame index, preserves zoom, normalized offset, and horizontal flip, and uses a calm UI playback scale around `0.75` of the authored idle speed.
- The UI instance never reuses or controls a battlefield `AnimatedSprite2D`; changing card visibility or animation state cannot alter combat presentation.
- Pause portrait playback when not visible in the scene tree. A valid one-frame idle naturally remains static. Missing or invalid portrait data retains the existing explicit fallback behavior.
- Existing 45 portrait resources remain independently tunable and read-only. Do not duplicate frames or generate replacement art.

## Architecture Impact

- Update `gameplay-design/tower-autobattler-core.md` with the global semantic-icon reuse and animated-portrait readability contract.
- Update `system-design/tower-autobattler-architecture.md` with catalog ownership, semantic-key binding, authored chip reuse, and independent UI animation boundaries.
- Extend `docs/testcases/alpha-manual-qa.md` with icon consistency, differentiated route/unit meanings, portrait motion/pause, responsive layout, and input regression checks.
- Presentation owns icons, chips, colors, and portrait playback. Content continues to own read-only unit identity and portrait definitions. Battle simulation, run state, and saves remain authoritative and unchanged.

## Scope

- A centralized semantic-icon resource contract, authored catalog, validation, and lookup API.
- New monochrome/tintable SVG assets for the confirmed responsibility, faction, reach, and tower-node meanings.
- A reusable authored semantic chip scene and focused binding API.
- Unit-choice card hierarchy and binding changes for hero selection and recruitment.
- Reuse in deployment facts, Army details, battle-report unit rows, selected-unit facts, and tower-route choices where the same semantics appear.
- Independent idle playback in the shared `UnitPortrait` scene/component across its existing five portrait consumers.
- Focused automated contracts, low-concurrency build/regression checks, and visual evidence at 1600x900 and 1280x720.

## Non-Goals

- No gameplay, combat AI, navigation, targeting, balance, rewards, route generation, economy, progression, save schema, or battle digest change.
- No battle-board animation, animation-cue, movement, facing, defeat, or unit-marker change.
- No new units, items, mechanics, animations, portrait crops, generated illustrations, or donor-project dependency.
- No icon-only UI, replacement of descriptive prose, or removal of Chinese labels.
- No redesign of reward/shop/tower general choice cards, window baseline, deployment board geometry, or battle-board layout. Hero selection is explicitly redesigned; recruitment receives only the confirmed unit-row hierarchy correction.
- No requirement to iconize every verb or transient action sentence; only stable reusable semantics are cataloged.

## Hard Constraints

- Work only on the current `main` branch; create no branch or commit.
- Preserve the all-untracked workspace and unrelated user files. Do not edit generated `.godot/imported` files.
- Keep `D:\godot\rpg` read-only and introduce no runtime absolute-path dependency.
- Author UI structure in `.tscn`, shared presentation data in `.tres`, and monochrome vector artwork in source `.svg` files. Runtime code loads, validates, instantiates, and binds these resources.
- Do not add concrete unit-id dispatch. Responsibility, faction, tower-node type, and stable UI semantic keys are allowed presentation categories.
- Static catalog and portrait resources remain read-only at runtime. Mutable playback state belongs only to each instantiated portrait node.
- Preserve mouse, keyboard, gamepad, disabled-state, tooltip, stable-id activation, scroll ownership, and the accepted 1600x900 / 1280x720 layouts.
- Avoid concurrent Godot/import/build processes, use low-concurrency .NET builds, and do not control or close a user-open Godot editor or game.

## Acceptance Criteria

### Semantic catalog and assets

- One validated resource catalog resolves every confirmed semantic and rejects blank keys, duplicates, or missing textures.
- Existing matching SVGs are reused; all new responsibility, faction, reach, and tower-node SVGs are differentiated, monochrome/tintable, sharp, and present as authored source assets.
- Production UI contains no duplicate hard-coded path map or per-screen meaning switch for the cataloged semantics.

### Unit cards and shared facts

- Every hero-selection and recruitment unit card visibly shows icon-plus-Chinese-text responsibility, faction/trait, health, damage, and exact reach facts without clipping at 1600x900 or 1280x720.
- A multi-trait unit such as `深渊爬兽` retains both `亡灵` and `野兽`, and both meanings use the same faction icons used elsewhere.
- Hero health and damage appear exactly once in the decision hierarchy. Hero rule, command effect/cost, lock state, and stable-id selection remain readable and functional.
- Deployment, Army details, battle-report rows, and selected-unit facts use the same semantic icons for meanings they share, without losing their existing values or state information.

### Tower routes

- Combat, elite, recruitment, shop, event, rest, and boss route cards have seven visually distinguishable primary icons.
- Risk remains independently labelled with the shared risk semantic. A zero-risk route still keeps its node identity and does not display the generic warning triangle as that identity.

### Animated portraits

- The shared `UnitPortrait` plays a valid multi-frame authored idle animation from the authored starting frame and visibly advances while on screen.
- Hiding the portrait or an ancestor pauses its playback; showing it resumes without affecting any battle animation instance.
- Zoom, normalized offset, flip, fallback, one-frame portrait behavior, all 45 portrait bindings, and the five existing consumer contexts remain valid.
- Many visible portrait instances perform no per-frame content lookup, resource duplication, or runtime mutation of shared `SpriteFrames`/portrait resources.

### Verification

- Focused source/runtime contracts cover catalog completeness/uniqueness, cross-context resource identity, route differentiation, authored chip structure, hero-stat deduplication, portrait advance/pause, and unchanged typed choice behavior.
- Low-concurrency build and relevant Content, UI, WindowPortrait, visual-capture, and clean-startup checks pass with documented expected warnings only.
- Fresh visual evidence at 1600x900 and 1280x720 covers hero selection, recruitment, tower routes, deployment, Army details, selected-unit facts where practical, and battle-report rows; no fact clipping, broken icon, layout overlap, or inaccessible action remains.
- Final review confirms no gameplay/save/battle-board/donor/generated-import scope drift.

### Visual hierarchy correction

- A player can identify the focused hero, responsibility/faction, health, attack, reach, army rule, command, and command cost by visual grouping before reading paragraph copy.
- Hero-library tiles scale to the existing roster without repeating rules and command prose. Preview focus and final stable-id activation remain separate and accessible.
- Health is consistently green, attack/damage red, mana blue, shield steel gray, healing teal-green, gold gold, range light cyan, danger/death crimson, risk amber, and hero identity gold in every touched context.
- `1 MP` and other command costs are structured semantic badges, not neutral inline substrings. Paragraphs remain neutral and no interface becomes fully colorized.
- Authored `StatBlock`, `TraitBadge`, `ResourceCostBadge`, and `HeroAbilityPanel` scenes are reusable and inspectable in isolation. No runtime prose parsing, concrete hero-id UI dispatch, or ad-hoc whole-tree construction is introduced.
- Fresh visual captures at 1600×900 and 1280×720 cover the hero library with several different focused heroes, locked/disabled state, recruitment, deployment, Army details, command HUD, and player/enemy battle reports. They show no clipped values, overlapping prose, hidden primary actions, ambiguous selection state, or inaccessible scroll/focus target.

## Progress

- 2026-08-29: User confirmed a global reusable semantic-icon system, differentiated unit/tower meanings, structured unit-card facts, deduplicated hero stats, and independent calm idle animation in unit portraits.
- 2026-08-29: Read-only architecture preflight assigned the change to UI Presentation and Content Pipeline. Existing combat, run, save, general choice-card selection, and portrait-resource ownership remain authoritative.
- 2026-08-29: Read-only audit confirmed 14 existing semantic SVGs, one basic `IconText` scene, static-frame `UnitPortrait` rendering over reusable `SpriteFrames`, three plain-text fact labels in `UnitChoiceCard`, duplicated hero health/damage, and one shared `risk.svg` used as every tower-node identity.
- 2026-08-29: Activity document created after confirmation. No production code, scene, resource, icon, authority, QA, test, build, Godot, branch, commit, donor, or external-state change has occurred yet.
- 2026-08-29: Execution architecture preflight passed on `main`. Presentation owns one read-only semantic catalog, authored chip scenes, route identity icons, and per-instance UI portrait playback; content continues to own immutable definitions and SpriteFrames, while combat, run state, saves, general choice selection, and battlefield animation remain frozen. Existing user editor PID 23260 remains outside executor control.
- 2026-08-29: Gameplay, system, and manual-QA authority now record the single icon vocabulary, Chinese-labelled semantic chips, node-identity/risk separation, cross-context reuse, and independent visible-only UI idle playback before production implementation.
- 2026-08-29: Focused source-level RED authored as `SemanticPresentationContractSmoke`. It requires the 38-entry catalog surface, 24 differentiated new SVG sources, authored `SemanticChip`, unit-card fact groups, seven route identities, and independent visible-only `AnimatedSprite2D` portrait playback without depending on not-yet-created production C# types.
- 2026-08-29: Focused RED executed after a low-concurrency build completed with 0 warnings and 0 errors. It exited 1 and reported every intended absence: catalog/resource API, authored chip, reach plus 8 responsibility/8 faction/7 tower SVGs, unit-card fact groups, route-node catalog binding, and visible-only animated portrait playback. No unrelated failure or authority conflict appeared.
- 2026-08-30: Execution resumed as the sole writer after fully rereading this work item, project rules, the confirmed gameplay/system/QA authority, and the required Godot UI, asset, scene, resource, C#, and testing skills. The workspace already contains an unrecorded RED-following partial implementation dated 2026-08-29: the 38-entry catalog/resource API, authored `SemanticChip`, 24 differentiated SVG sources, cross-context chip wiring, route identity binding, and animated `UnitPortrait` surfaces. Because the repository is intentionally all-untracked, ownership cannot be inferred from Git; these files are preserved and treated as interrupted implementation pending static/runtime verification rather than overwritten or accepted without evidence.
- 2026-08-30: The resumed implementation reached focused GREEN. Low-concurrency `dotnet build` reports 0 warnings/0 errors; `SEMANTIC_PRESENTATION_CONTRACT_OK` confirms 38 validated shared semantics, 24 differentiated authored SVGs, catalog identity reuse, seven route identities, typed choice preservation, and independent visible-only portrait playback; `WINDOW_PORTRAIT_CONTRACT_OK` remains green. Static review added Theme-derived icon tinting to `SemanticChip`. The first `UiSmoke` exposed a real deployment regression (`246px` of semantic facts in a `235px` visible rect); a context-authored 17px chip text override fixed it without widening the roster, changing the board, or altering scroll/input ownership, and the rerun reports `UI_SMOKE_OK`. Its deliberate modal-focus escape warning remains the existing expected non-failing diagnostic.
- 2026-08-30: Relevant serial regression is green: Fixture, Content, Gameplay, MovementPresentation, UI, AlphaRun, and clean five-frame startup all exit zero. Content retains exactly `portraits=45(8,24,13)` and deliberately emits/captures its five negative lifecycle markers before `CONTENT_CONTRACT_OK`; Gameplay and MovementPresentation confirm unchanged simulation, save/deployment, battlefield movement, animation, and selection behavior.
- 2026-08-30: Authorized `VisualCapture.tscn` runs at `1280×720` and `1600×900` both report `VISUAL_CAPTURE_OK`. Manual review of hero selection, recruitment, tower routes, deployment, Army details, selected-unit facts, and player/enemy/defeat/final-victory report frames found no clipped fact, broken icon, overlapping prose, hidden required action, or route-risk identity collision. Unit portraits remain readable in all five consumer contexts; runtime advance/pause/resume is covered by the focused semantic contract rather than inferred from static screenshots.
- 2026-08-30: Final `godot-code-review` found and corrected one catalog-boundary weakness: production `SemanticIconCatalog.Validate()` now rejects any missing confirmed key through the same 38-key vocabulary generated from stable base semantics and the responsibility/faction/tower enums. Final build, SemanticPresentation, WindowPortrait, UI, and clean-startup reruns are green. Static scope audit found no donor path, per-screen SVG map, portrait `_Process` lookup, gameplay/save/battle-board drift, or shared `SpriteFrames` mutation. The user editor PID 23260 remained open and untouched; idle MSBuild and compiler servers were shut down safely.
- 2026-08-30: User visual acceptance rejected the iconized hero list. Evidence showed that inline 18px icon-plus-text chips preserved the old prose flow, attack/range/responsibility shared an undifferentiated blue treatment, and command costs such as `1 MP` remained buried in neutral copy. The user confirmed a responsive hero-library/detail redesign, separate stat/trait/cost/ability presentation components, and a centralized semantic palette. Prior build and contract evidence remains a regression baseline, not acceptance of the rejected hierarchy.
- 2026-08-30: Correction execution resumed on `main` after rechecking the active task, stable project constraints, all-untracked workspace, and current authored UI/code surfaces. Architecture ownership remains UI Presentation only: gameplay, content schema, independent unit/item scenes, run activation, saves, tower/deployment/battle boards, and user editor PID 23260 remain unchanged. Gameplay, system, and manual-QA authority are being synchronized with the responsive preview-versus-activation contract, four reusable authored component roles, structured command costs, and exact semantic palette before production changes.
- 2026-08-30: Gameplay, system, and manual-QA authority synchronization completed. A focused `VisualHierarchyContractSmoke` RED now requires the responsive authored library/detail composition, six typed presentation surfaces including the four confirmed reusable components, recruitment stat/trait usage, structured HUD costs, exact shared Theme variations, and removal of the verbose hero-row route without referencing not-yet-created production types.
- 2026-08-30: The focused correction RED was executed after a low-concurrency build completed with 0 warnings/0 errors. It failed only for the intended missing master-detail surfaces, four reusable components, typed bindings, structured HUD costs, and semantic Theme variations; no gameplay/content-schema or unrelated failure appeared.
- 2026-08-30: Correction implementation reached focused GREEN. Hero selection now binds eight compact animated `HeroLibraryTile` scenes to one `HeroDetailPanel`; tile mouse/focus/press changes preview only, locked heroes retain explicit preview/disabled action state, and the fixed detail action emits one unlocked stable id exactly once. The responsive runtime contract confirms two columns at a direct 1280-wide layout and three at 1600, while production captures remain unclipped at both supported window resolutions.
- 2026-08-30: Authored `StatBlock`, `TraitBadge`, `ResourceCostBadge`, and `HeroAbilityPanel` scenes now own distinct presentation roles. Recruitment uses trait badges and three stat blocks; hero detail, battle HUD, and Army hero detail use typed mana/gold cost badges. The shared Theme/catalog now distinguish health green, damage red, mana blue, shield steel gray, healing teal-green, gold/hero gold, reach cyan, danger crimson, and risk amber. Visual review caught and corrected old blue damage in reports, prose-form Army command cost, and an ambiguous preview tile; final captures show red damage, structured Army cost, and a gold surface plus `◆` text for the active preview.
- 2026-08-30: Final serial verification is green: build 0 warnings/0 errors; `VISUAL_HIERARCHY_CONTRACT_OK`; `SEMANTIC_PRESENTATION_CONTRACT_OK`; `WINDOW_PORTRAIT_CONTRACT_OK`; Fixture, Content, Gameplay, MovementPresentation, UI, AlphaRun, and clean startup all exit zero. Content retains its five deliberate captured gate-error probes and `portraits=45(8,24,13)`; UI retains its established deliberate modal-focus warning before `UI_SMOKE_OK`.
- 2026-08-30: Fresh `VisualCapture` runs at 1280×720 and 1600×900 both report `VISUAL_CAPTURE_OK`. Manual review covered default, merchant, and locked hero focus; recruitment; deployment; Army; command mana; selected-unit facts; and both report allegiances. Required actions remain visible, no facts/prose overlap, structured costs remain legible, and the selected preview is unambiguous at both resolutions.
- 2026-08-30: Final `godot-code-review` found no ad-hoc whole-tree construction, concrete hero-id dispatch, regex/prose cost parsing, signal lifecycle mismatch, shared `SpriteFrames` mutation, donor path, gameplay/save/battle-board drift, or runtime external dependency. The only UI `SpriteFrames` assignment binds the existing read-only portrait frames and remains covered by identity/mutation tests. User editor PID 23260 remains responsive and untouched.
- 2026-08-30: The user separately confirmed a dynamic four-dimension battle-report redesign. `work-items/active/tower-autobattler-dynamic-battle-report.md` now owns battle-report information architecture, result-statistic extensions, and roster layout. This task retains only the cross-screen semantic icon, palette, and independent portrait contracts for report consumers; any earlier tabular-layout wording is obsolete.

## Current State And Resume Condition

The confirmed correction is implemented and awaiting independent verification on `main`. Resume with `tests/VisualHierarchyContractSmoke.tscn`, then the semantic/window/UI contracts and the listed full serial matrix. Inspect the final paired hero-selection, recruitment, Army, HUD, deployment, selected-unit, and report captures under `.godot/qa/`; perform the remaining subjective native observation of one animated portrait through visible → hidden ancestor → visible while confirming battle animation remains independent. Do not modify gameplay/content schema, reward/shop/tower or board layouts, the donor project, generated imports, or user editor PID 23260.

## Verification Handoff

The handoff below records the rejected first implementation and remains useful only as regression evidence. The correction executor must append a new changed-surface list, RED/GREEN evidence, build/regression markers, paired visual captures, remaining risks, and an independent-verification entry point before returning the task to `Awaiting Verification`.

### Changed Surfaces

- Central vocabulary and binding: `src/UI/SemanticIconEntry.cs`, `SemanticIconCatalog.cs`, `SemanticFacts.cs`, `SemanticChip.cs`, `content/ui/semantic_icon_catalog.tres`, and authored `scenes/ui/components/SemanticChip.tscn`.
- Differentiated source artwork: `assets/ui/icons/reach.svg`; eight `role-{vanguard,fighter,ranged,support,assassin,summoner,artillery,boss}.svg`; eight `faction-{order,desert,undead,beast,machine,frost,neutral,enemy}.svg`; and seven `tower-{combat,elite,recruitment,shop,event,rest,boss}.svg`. Godot-created SVG import sidecars exist, but neither they nor `.godot/imported` cache files were hand-edited.
- Unit decisions and tower routes: `src/App/GameRoot.cs`; `src/UI/UnitChoiceCard.cs` and `ChoiceCard.cs`; `scenes/ui/components/UnitChoiceCard.tscn`, `HeroUnitChoiceCard.tscn`, and `ChoiceCard.tscn`.
- Shared fact consumers: deployment (`src/UI/DeploymentUnitCard.cs`, `scenes/ui/components/DeploymentUnitCard.tscn`); Army details (`src/UI/ArmyOverviewModels.cs`, `ArmyDrawerRow.cs`, `scenes/ui/components/ArmyDrawerRow.tscn`); reports (`src/UI/BattleReportScreen.cs`, `BattleReportUnitRow.cs`, `scenes/ui/components/BattleReportUnitRow.tscn`); and selection (`src/UI/SelectedUnitPanel.cs`, `scenes/ui/components/SelectedUnitPanel.tscn`).
- Independent portraits: `src/UI/UnitPortrait.cs` and `scenes/ui/components/UnitPortrait.tscn`. The 45 existing portrait `.tres` resources and source `SpriteFrames` were reused without modification.
- Contracts: `tests/SemanticPresentationContractSmoke.cs/.tscn` and the relevant semantic extensions in `tests/UiSmoke.cs`. Existing `VisualCapture.tscn` was used unchanged.
- Task memory: this activity document. The already-completed gameplay/system/QA authority updates were not redone.

### Commands And Markers

```powershell
dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/SemanticPresentationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/WindowPortraitContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/FixtureContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/ContentContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/MovementPresentationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/UiSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlphaRunSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . --quit-after 5
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --path . --resolution 1280x720 tests/VisualCapture.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --path . --resolution 1600x900 tests/VisualCapture.tscn
dotnet build-server shutdown
```

- Build: `0 warnings`, `0 errors`.
- Focused: `SEMANTIC_PRESENTATION_CONTRACT_OK catalog=38 chips=authored routes=7 portraits=independent-idle` and `WINDOW_PORTRAIT_CONTRACT_OK`.
- Regression: `FIXTURE_CONTRACT_OK`; `CONTENT_CONTRACT_OK entries=57 floors=5 events=90 portraits=45(8,24,13)`; `GAMEPLAY_CONTRACT_OK`; `MOVEMENT_PRESENTATION_CONTRACT_OK`; `UI_SMOKE_OK`; `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`; clean startup exits zero.
- Visual: both resolutions report `VISUAL_CAPTURE_OK screens=12 extras=15 movement_frames=27 ... information=selected-unit,zero-mana,semantic-icons`.
- Expected diagnostics only: Content deliberately emits `CONTENT_GATE_STRUCTURAL_INSTANTIATE_FAILURE`, `CONTENT_GATE_INSTANTIATE_FAILURE`, `CONTENT_GATE_READY_FAILURE`, `CONTENT_GATE_PROCESS_FAILURE`, and `CONTENT_GATE_EXIT_FAILURE`; UI deliberately attempts one modal focus escape and logs the established non-failing focus warning before its success marker.

### Visual Evidence

Fresh output is under `.godot/qa/`. The manually reviewed paired frames include:

- `UI_{1280x720,1600x900}_HeroSelectScreen.png`
- `UI_{1280x720,1600x900}_RecruitmentScreen.png`
- `UI_{1280x720,1600x900}_TowerScreen.png`
- `UI_{1280x720,1600x900}_DeploymentScreen.png`
- `UI_{1280x720,1600x900}_ArmyDrawerTower.png`
- `UI_{1280x720,1600x900}_SelectedUnitDetails.png`
- `UI_{1280x720,1600x900}_BattleReportPlayer.png`
- `UI_{1280x720,1600x900}_BattleReportEnemy.png`
- `UI_{1280x720,1600x900}_BattleReportDefeat.png`
- `UI_{1280x720,1600x900}_BattleReportFinalVictory.png`

### Remaining Risks And Independent Entry Point

- Git cannot provide a meaningful diff because the repository intentionally has no commits and every project surface is untracked. Independent review must use the explicit surface list above and preserve unrelated user files.
- The screenshots prove layout and current-frame readability, while calm frame advance, ancestor-hide pause, resume, shared-`SpriteFrames` identity, and no shared mutation are asserted by `SemanticPresentationContractSmoke`. Independent native observation of representative hero, soldier, and enemy motion across the five consumers remains the subjective final check.
- `VisualCapture` includes the historically timing-sensitive real-production-hitch probe. Both fresh serial runs passed unchanged; a future isolated hitch failure should be reproduced before attributing it to this presentation-only task.
- Begin independent verification with `tests/SemanticPresentationContractSmoke.tscn`, then run the listed build/WindowPortrait/Content/UI matrix serially. Inspect the paired evidence above, and finally observe one multi-frame portrait through visible → hidden ancestor → visible while confirming a battlefield presenter is unaffected. Keep PID 23260 outside verifier control.

## Correction Verification Handoff

### Correction Changed Surfaces

- Responsive hero selection: `src/UI/HeroSelectScreen.cs`, `HeroSelectionViewModel.cs`, `HeroLibraryTile.cs`, `HeroDetailPanel.cs`; `scenes/ui/HeroSelectScreen.tscn`, `components/HeroLibraryTile.tscn`, and `components/HeroDetailPanel.tscn`; `src/App/GameRoot.cs` now binds typed hero models and no longer uses verbose hero unit-choice rows.
- Reusable authored vocabulary: `src/UI/StatBlock.cs`, `TraitBadge.cs`, `ResourceCostBadge.cs`, and `HeroAbilityPanel.cs` with matching isolated scenes under `scenes/ui/components/`; semantic roles and selected/disabled surfaces in the authoritative `content/ui/RealmTheme.tres`; corrected catalog presentation roles in `content/ui/semantic_icon_catalog.tres` and typed unit facts in `src/UI/SemanticFacts.cs`.
- Context integration: recruitment `UnitChoiceCard`; battle `HeroCommandHud`; Army overview models/rows; battle-report metrics/rows. No content definition, unit/item scene, run, save, battle simulation, route, reward/shop, tower-board, deployment-board, or battle-board ownership changed.
- Contracts/evidence: new `tests/VisualHierarchyContractSmoke.cs/.tscn`; updated SemanticPresentation, WindowPortrait, UI, and VisualCapture contracts; updated gameplay/system/manual-QA authority and this activity document.

### Correction Markers And Evidence

- Build: `0 warnings`, `0 errors`.
- Focused: `VISUAL_HIERARCHY_CONTRACT_OK master-detail=responsive components=4 costs=structured palette=semantic`; prior semantic and portrait focused markers remain green.
- Full regression: Fixture, Content, Gameplay, MovementPresentation, UI, AlphaRun, and clean startup exit zero with only the already documented deliberate diagnostics.
- Visual: both final resolutions report `VISUAL_CAPTURE_OK ... movement_frames=29 ...`; paired evidence adds `HeroSelectMerchant.png`, `HeroSelectLocked.png`, and `BattleCommandMana.png` alongside the prior screen set.
- Manual review confirms clear gold-surface plus `◆` preview selection, explicit locked/disabled detail action, health/damage/range grouping, neutral rules/effects, separate `1 MP`/optional gold badges, three readable recruitment rows, compact structured Army costs, red report damage, and fixed actions at both supported resolutions.

### Remaining Verification

- The repository remains intentionally all-untracked, so verification must use the correction surface list rather than Git diff statistics.
- Independent subjective native observation of representative animated portraits and selected/focused input feel remains. Automated coverage already verifies preview-only tile behavior, exact-once stable-id activation, locked action, responsive columns, independent portrait frame advance/pause/resume, and no shared-frame mutation.
- Start with `tests/VisualHierarchyContractSmoke.tscn`; then run SemanticPresentation, WindowPortrait, UI, Content, and the remaining serial matrix. Inspect `.godot/qa/UI_{1280x720,1600x900}_HeroSelect{Screen,Merchant,Locked}.png`, recruitment, Army, command mana, deployment, selected-unit, and player/enemy reports. Keep user editor PID 23260 outside verifier control.
