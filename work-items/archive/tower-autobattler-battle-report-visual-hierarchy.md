# Battle Pace, End Sequence, Report, and Visual Hierarchy

Status: Completed  
Confirmed: 2026-08-28

## Goal

Make combat readable at its normal speed, give the final action enough time to land, transition through a controlled fade to black, and show a useful single-battle report before rewards or run results. Replace the project's flat black-background/white-text presentation with a reusable semantic color and icon system that reduces information density without turning long-form text into multicolored noise.

## Confirmed Product Behavior

### Battle pace

- Keep the player-facing `x1`, `x2`, and `x4` choices and their relative ratios.
- The new normal pace advances simulation at 80% of the current real-time rate. Effective real-time simulation scales are `0.8`, `1.6`, and `3.2` for the displayed `x1`, `x2`, and `x4` modes.
- Do not change `BattleSimulation.TickSeconds`, authored attack/move cooldowns, deterministic tick order, outcome, digest, or balance. Existing settings/save values remain compatible; an existing saved `x1` automatically receives the slower baseline.
- Presentation remains continuous across pause and speed changes. The selected speed mode, not an invented balance value, remains the player-facing setting.

### End sequence and navigation

- On the terminal simulation tick, stop all future simulation stepping immediately and capture one immutable `BattleResult`/report snapshot.
- Disable battle commands, pause/speed controls, unit selection, and board interaction during the end sequence.
- Commit run resolution and save exactly once as soon as the outcome is authoritative, before the report can be abandoned or the application can exit. Do not change screens yet.
- Preserve the final battlefield for approximately `1.1` real-time seconds so the terminal attack/defeat presentation remains visible, then fade a full-screen authored overlay to black over approximately `0.45` seconds.
- The end transition uses a single owned Tween that is killed/reset on battle replacement, scene exit, or a new battle. It is independent of battle speed and pause.
- Confirm input may fast-forward the hold/fade to the report, but never skip the report itself. Repeated input and callbacks are idempotent.
- At full black, navigate to a new authored `BattleReportScreen`; do not repurpose `RewardScreen` or the run-ending `ResultScreen`.
- The report's continue action routes once to combat rewards after an ordinary victory, to the run-success result after the final boss, and to the run-failure result after defeat/timeout.

### Battle report

- The report header shows outcome, encounter name, and deterministic simulation duration.
- Summary metrics show at least survivors, casualties, effective team damage, effective team healing, and command gold spent. Zero-value metrics remain understandable and may be visually muted.
- The report exposes separate `我方` and `敌方` views rather than rendering both full rosters at once. Unit rows live in a scrollable authored container and remain usable at 1280×720 and 1600×900.
- Each row shows a unit portrait or authored/fallback icon, Chinese name, hero/summon identity, alive/defeated state, final health, effective damage dealt, effective damage taken, effective healing done, and kills.
- Temporary summons are retained and explicitly tagged rather than silently folded into their summoner. Environment contributions are not falsely attributed to a unit.
- Objective highlights may label highest damage, highest damage taken, and highest healing. There is no weighted MVP score in this task.

### Statistics semantics

- `BattleSimulation` owns deterministic per-runtime-id combat statistics. Presentation events and drained event history are not report authority.
- Effective damage is the actual amount removed from health plus shield, capped by available health/shield; overkill is not counted. Damage dealt and damage taken mirror the same authoritative change.
- Shield absorption is tracked as part of the damage application facts and may be retained separately even if the first report does not display it.
- Effective healing is the actual health restored; overhealing is not counted. Ordinary healing, lifesteal, and floor-rule healing share one source-aware authoritative path. Environment healing is not credited to a unit.
- Kills are credited only when a concrete source runtime id caused the terminal transition. On-death effects may credit their defeated source; floor/environment kills remain unowned.
- Summons receive their own statistics record on spawn. Initial and final unit/report data are immutable snapshots in the result, not live `BattleUnitState` references.
- Report-stat collection must not enter the battle digest or alter combat decisions, health changes, cooldowns, floor rules, or outcomes.

### Visual hierarchy and icons

- Extend the shared Theme with reusable semantic type variations and centralized colors. Avoid new per-node color overrides except a genuinely unique authored case.
- Use gold for heroes, currency, legendary identity, and major headings; blue for mana, shield, and player information; green for healing, survival, and positive gains; red for enemies, casualties, errors, and defeat; amber for risk, warning, and timeout; and neutral near-white/blue-gray for body and secondary text.
- Color is never the only carrier of meaning. Keep Chinese labels/icons for hero, near/ranged, victory/defeat, risk, rarity, and status.
- Long descriptions and rules remain neutral. Apply semantic color to icons, compact values, badges, headings, and status words rather than coloring entire paragraphs.
- Add reusable authored scenes for icon text/stat chips, battle-report metrics, and battle-report unit rows. Refactor the reusable `ChoiceCard` from one concatenated `Button.Text` string into icon/portrait, title, body, and footer/meta regions while preserving keyboard/gamepad focus and one typed choice signal.
- The `ChoiceCard` improvement applies to its current hero selection, tower route, reward/recruitment, and shop consumers so the hierarchy improvement is project-wide at the highest-leverage entry point.
- Add a small set of original, monochrome, tintable SVG UI icons for health, damage, shield, healing, mana, gold, time, kills/deaths, hero, melee, ranged, risk, and loot. Keep originals under `assets/ui/icons/`; do not edit generated `.godot/imported` files.
- Unit content may use its authored icon when present and otherwise fall back to a stable portrait extracted through the content presentation contract or a role icon. Item content may use an optional authored icon and otherwise a category/loot fallback. This task does not create bespoke illustrations for every item.

## Architecture and Authority Impact

- This is a Combat statistics, Runtime State/navigation-flow, Presentation, UI, Content Presentation, and Asset Pipeline change.
- `BattleSimulation` remains authoritative for all combat changes and now also owns report statistic aggregation at the same mutation points.
- `BattleResult` becomes an immutable battle/report snapshot. It carries stable ids and numeric facts only; it never carries textures, Nodes, or mutable shared resources.
- `BattleScreenController` owns fixed-step simulation coordination, terminal freeze, final battlefield hold, input lock, and fade transition. It does not commit run progression or choose rewards.
- `GameRoot` owns exactly-once run completion/save, pending post-report navigation, report binding, and final routing.
- `BattleReportScreen` and its repeated row/metric children are authored scenes. Their controller renders a view model and emits a typed continue request; it does not infer combat facts or mutate run state.
- `gameplay-design/tower-autobattler-core.md` must record the normal pace, end sequence, report content, and statistics semantics before runtime code changes.
- `system-design/tower-autobattler-architecture.md` must record report/stat ownership, immutable result boundaries, exactly-once resolution/navigation, transition ownership, and semantic UI/icon resource contracts before runtime code changes.
- `docs/testcases/alpha-manual-qa.md` must add player-visible pace, end sequence, report, color hierarchy, icon, focus, and resolution-size acceptance cases.

## Scope

- Implement the slower real-time pace without changing simulation time or balance.
- Implement a terminal presentation state with exactly-once resolution, final-action hold, fade-to-black, fast-forward, cleanup, and report navigation.
- Add authoritative per-unit battle statistics and immutable result snapshots.
- Add the report screen, metric/row templates, outcome-aware continue flow, focus handling, and responsive scrolling.
- Add the semantic Theme variations, SVG icon set, reusable icon/stat widgets, optional/fallback content icon path, and structured `ChoiceCard` hierarchy.
- Update affected automated contracts, visual capture flow, authority documents, manual QA, and this task's progress/evidence.

## Non-goals

- No combat balance, targeting, navigation, hero/soldier/item mechanics, floor-rule tuning, encounter composition, reward amount, or save-schema change.
- No weighted MVP system, combat log replay, timeline graph, damage-number system, audio redesign, screen shake, or camera work.
- No bespoke illustrated icon for every existing item, hero, soldier, or enemy. The first pass supplies semantic SVGs, stable unit portraits/fallbacks, and optional content icon hooks.
- No full redesign of every screen. Theme variations and the shared `ChoiceCard` must produce broad improvement, while unrelated screen-specific layouts remain outside scope.
- No runtime construction of complete UI trees; repeated content must instantiate authored `.tscn` templates.

## Constraints

- Work only on `main`; do not create or switch branches and do not commit.
- Preserve the existing all-untracked project workspace and unrelated user files.
- `D:\godot\rpg` remains read-only and is not required for this task.
- Use `apply_patch` for file edits. SVG originals, Theme resources, scenes, and reusable components remain diffable text resources.
- Do not start concurrent Godot/import/build processes. Build at low concurrency and close idle build servers after verification.
- Follow the `godot-ui-teardown` Tween lifecycle, container, focus, and Theme-variation rules; follow the asset-pipeline rule that original resources are authoritative and `.godot/imported` is generated; extend the project's existing headless smoke style with RED/GREEN behavioral contracts.

## Acceptance Criteria

- At displayed `x1`, a fixed wall-clock interval advances approximately 80% as many simulation ticks as before; `x2` and `x4` remain exact relative multiples, and pause/speed changes remain continuous.
- Same seed/setup/commands at any display speed produce the same terminal tick, outcome, digest, movement events, and report statistics.
- A terminal tick cannot be followed by another simulation step. Run completion/save and report transition each occur once even with repeated frames, repeated input, battle replacement, or teardown.
- The last terminal action remains visible during the bounded hold. The overlay reaches opaque black before the report appears; fast-forward reaches the same report without bypassing resolution.
- Ordinary victory, final victory, player defeat, and timeout all show a correctly bound report and route correctly after continue.
- Damage, shield, overkill, splash, pierce, death effects, environment damage, ordinary healing, overheal, lifesteal, and floor healing produce the confirmed statistics without changing battle health/outcome behavior.
- Report rows distinguish heroes, soldiers, enemies, and summons and correctly show alive/defeated/final-health facts. Long rosters scroll without clipping at both supported resolutions.
- Hero selection, route choices, recruitment/reward, and shop cards expose independent title/body/meta regions plus an icon or fallback, retain typed selection, mouse, keyboard/gamepad focus, disabled state, and tooltips.
- Semantic colors and icons are consistent, centralized, and never the sole meaning carrier. Long prose remains neutral and readable.
- Build, focused Gameplay/MovementPresentation/UI tests, complete existing smoke matrix, all three 15-floor AlphaRun paths, visual captures at 1280×720 and 1600×900, and short headless startup pass with only documented expected warnings.

## Progress

- 2026-08-28: User confirmed the complete pace, end-sequence, report, semantic-color, and icon plan after read-only audits of battle coordination, run navigation, result data, Theme/resources, shared cards, screenshots, and existing tests.
- 2026-08-28: Activity document created on `main`. No runtime, scene, resource, authority, or test implementation has started.
- 2026-08-28: Execution resumed on `main` with the all-untracked workspace preserved. Architecture preflight confirmed simulation owns report facts, `BattleScreenController` owns real-time terminal presentation, `GameRoot` owns exactly-once run resolution and post-report routing, and authored reusable scenes/Theme/icons own visual hierarchy. Gameplay, system, and manual-QA authority now record the confirmed pace, immutable statistics, end sequence, report, semantic palette, icon fallback, and structured choice-card contracts before runtime changes.
- 2026-08-28: RED contracts compile with `0 warnings / 0 errors`. The pre-change Gameplay suite exits 1 because displayed x1 advances 9 fixed ticks over the one-real-second probe instead of the confirmed approximately 8; the same suite also contains terminal lock/exactly-once and immutable per-unit statistics probes. Reordering the focused probe previously reproduced the separate live-state defect with `battle result retained live BattleUnitState references`. The pre-change UI suite exits 1 at main composition because the authored battle-report screen, semantic resources, structured ChoiceCard hierarchy, and twelfth screen are absent.
- 2026-08-28: Runtime/statistics milestone is GREEN. Display modes now advance at `0.8/1.6/3.2` real-time scales without changing `TickSeconds`; terminal stepping freezes on the authoritative tick; one owned real-time Tween provides the 1.1-second hold and 0.45-second fade with idempotent confirm fast-forward and lifecycle reset. `BattleSimulation` records effective health-plus-shield damage, mirrored taken damage, shield absorption, effective source healing, kills, and independent summon rows, then creates immutable value snapshots. `RunApplication` consumes stable snapshot facts with no save-schema change. Focused Gameplay passes.
- 2026-08-28: Report/routing/UI milestone is GREEN. `GameRoot` commits run resolution exactly once before navigation, waits for the fade-complete boundary, binds the authored twelfth `BattleReportScreen`, and routes ordinary victory/final victory/defeat/timeout only after one report continue. Authored metric, row, icon-text, and structured choice-card scenes; shared semantic Theme variations; optional content icon hooks; portrait/role/loot fallback; and fourteen original tintable SVGs are integrated. Headless asset import completed once without editing generated files. Focused UI passes all four post-report route probes with its pre-existing deliberate modal-focus warning.
- 2026-08-29: Primary screenshot review found three presentation blockers and the correction slice is GREEN. Report allegiance tabs now use an authored local `ButtonGroup` plus the shared `ReportTabButton` Theme variation, so the current tab has a gold pressed/active treatment while both tabs remain enabled. The report header now exposes deterministic duration only as `模拟时长 N.N 秒`. Metric binding normalizes an empty value to `0`; the defeat fixture and both rendered resolutions show `有效伤害 0`. UiSmoke asserts initial and switched active states, enabled tabs, the seconds-only duration, five non-empty metrics, and the defeat zero-damage value.
- 2026-08-29: Final static review found no task-scoped blocker. Immutable report values remain separated from live simulation state; repeated report/card content still instances authored scenes; the semantic Theme owns the new tab treatment; no full UI tree is constructed at runtime; signal/Tween cleanup remains paired; no navigation, balance, save-schema, reward, donor, or generated-import boundary changed.
- 2026-08-29: Final ordered verification is GREEN: build `0 warnings / 0 errors`; `FIXTURE_CONTRACT_OK`; `CONTENT_CONTRACT_OK entries=57 floors=5 events=90` with the five intentional negative lifecycle markers; `GAMEPLAY_CONTRACT_OK` with immutable/effective report facts, `0.8/1.6/3.2`, and terminal hold/fade/fast-forward; `MOVEMENT_PRESENTATION_CONTRACT_OK`; `UI_SMOKE_OK ... report=authored-responsive,active-tabs,seconds-only,explicit-zero`; `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`; `VISUAL_CAPTURE_OK` at 1280×720 and 1600×900; and clean five-frame headless main-scene startup. Idle Roslyn/MSBuild servers were shut down after the matrix.
- 2026-08-29: Independent verification rejected the handoff on two concrete findings. First, `BloodRushCommandRuntime` mutates hero health directly and therefore preserves the gameplay heal while bypassing `BattleSimulation`'s source-aware effective-healing statistic. Second, `BattleReportMetric` duplicates semantic/zero icon colors in C# instead of resolving the bound Theme variation, and outcome binding replaces the authored `TitleLabel` variation with body-sized semantic variations. Execution resumed for this narrow correction and focused/full regression only; no balance, digest, transaction, save, navigation, or unrelated-screen change is authorized.
- 2026-08-29: Narrow RED contracts compile with `0 warnings / 0 errors`. Gameplay exits 1 with `blood-rush full effective healing was not attributed to the hero: expected 50, got 0` while retaining the existing health, damage-multiplier, cooldown, mana/gold, tick, and digest assertions. UiSmoke exits 1 with `shared Theme omitted semantic variation HealingTitleLabel`; the same contract also checks metric icon tint against the bound value variation and the 36px semantic outcome title.
- 2026-08-29: Narrow correction is GREEN. `BattleCommandContext` now exposes one injected source/target-aware heal operation backed by `BattleSimulation.HealLiving`; Blood Rush requests its existing maximum-health-ratio heal with the hero as both source and target. Focused Gameplay proves 50 full effective healing, 10 capped overheal, unchanged final health/damage/cooldown/mana/gold behavior, unchanged tick, and the same standard command-event digest. `BattleReportMetric` now tints its icon from the final value label variation's `font_color`, and outcome binding uses `HealingTitleLabel`, `EnemyTitleLabel`, or `WarningTitleLabel`, each inheriting the authored 36px `TitleLabel`. Focused build, Gameplay, and UiSmoke pass; the latter retains only its expected modal-focus probe warning.
- 2026-08-29: VisualCapture was refreshed at 1280×720 and 1600×900. Player, enemy, defeat, and final-victory report frames show the restored large semantic outcome title, Theme-owned icon tints (including secondary zero values), active tabs, seconds-only duration, and explicit zero metrics without overlap or clipping.
- 2026-08-29: Post-rejection full ordered matrix is GREEN: build `0 warnings / 0 errors`; `FIXTURE_CONTRACT_OK`; `CONTENT_CONTRACT_OK entries=57 floors=5 events=90` with all five expected negative lifecycle markers; `GAMEPLAY_CONTRACT_OK ... report=immutable,effective-damage,shield,healing,command-healing,kills`; `MOVEMENT_PRESENTATION_CONTRACT_OK`; `UI_SMOKE_OK ... report=authored-responsive,active-tabs,seconds-only,explicit-zero,theme-tints,title-semantics` with its expected modal-focus warning; `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`; both `VISUAL_CAPTURE_OK` runs; and clean five-frame headless main-scene startup. Build servers were shut down successfully. No user Godot editor/game process was operated or closed.
- 2026-08-29: Final independent Gameplay/statistics verification passed, including the corrected source-aware command-healing facts and immutable report semantics. Independent UI verification exited zero with `report=authored-responsive,active-tabs,seconds-only,explicit-zero,theme-tints,title-semantics`; the separate scope/document review found no blocker.
- 2026-08-29: The only remaining log item is non-blocking and intentional: `UiSmoke` deliberately performs its modal-focus escape probe, which produces the documented Godot focus warning before the success marker. Native subjective observation of the approximately 1.1-second terminal battlefield hold and 0.45-second fade-to-black remains recommended, but it is not an implementation or verification blocker.
- 2026-08-29: Primary handoff accepted. The task is completed and archived with no further code, resource, build, process, branch, or Git-commit action.

## Recovery Condition

Read this document, project `AGENTS.md`, `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, `docs/testcases/alpha-manual-qa.md`, and the applicable UI/Tween, asset-pipeline, testing, and review guidance before changing state. Resume from the first incomplete Progress milestone. If correct implementation requires changing combat balance, save schema, targeting/navigation, report MVP weighting, full-screen redesign beyond shared cards, or bespoke per-content illustration scope, mark this task `Needs Discussion` and stop rather than expanding silently.

## Verification Handoff

Status: Completed. Independent Gameplay/statistics, UI, scope, and documentation verification passed; the primary handoff was accepted and this task is archived.

### Changed files

- Authority and QA: `gameplay-design/tower-autobattler-core.md`, `system-design/tower-autobattler-architecture.md`, `docs/testcases/alpha-manual-qa.md`, and this activity document.
- Battle/runtime/navigation flow: `src/Battle/BattleModels.cs`, `src/Battle/BattleSimulation.cs`, `src/Battle/HeroCommandContracts.cs`, `src/Presentation/BattleScreenController.cs`, `src/App/GameRoot.cs`, `src/Run/RunApplication.cs`, `scenes/ui/BattleScreen.tscn`, and `scenes/app/GameRoot.tscn`.
- Content presentation hooks: `src/Content/UnitDefinition.cs`, `src/Content/ItemDefinition.cs`, and `src/Content/UnitContentRoot.cs`.
- Authored report/UI hierarchy: `src/UI/BattleReportScreen.cs`, `src/UI/BattleReportMetric.cs`, `src/UI/BattleReportUnitRow.cs`, `src/UI/IconText.cs`, `src/UI/ChoiceCard.cs`, `scenes/ui/BattleReportScreen.tscn`, `scenes/ui/components/BattleReportMetric.tscn`, `scenes/ui/components/BattleReportUnitRow.tscn`, `scenes/ui/components/IconText.tscn`, `scenes/ui/components/ChoiceCard.tscn`, and `content/ui/game_theme.tres`.
- Original semantic assets: the fourteen `assets/ui/icons/{health,damage,shield,healing,mana,gold,time,kills,deaths,hero,melee,ranged,risk,loot}.svg` files. Godot generated their `.import` sidecars and compiled cache from those originals; `.godot/imported` was not edited.
- Automated evidence: `tests/GameplayContractSmoke.cs`, `tests/MovementPresentationContractSmoke.cs`, `tests/UiSmoke.cs`, `tests/AlphaRunSmoke.cs`, and `tests/VisualCapture.cs`.

### RED and GREEN evidence

- Pre-change RED reproduced x1 advancing nine ticks in the one-real-second probe, a result boundary retaining live `BattleUnitState`, and the absence of the authored twelfth report screen/semantic UI contract.
- Independent-verification RED reproduced Blood Rush effective healing as `expected 50, got 0`, and UiSmoke rejected the absent `HealingTitleLabel`. The expanded contracts additionally cover capped 10-point overheal, unchanged command health/damage/cooldown/mana/gold/tick/digest behavior, Theme-derived metric icon tint, and 36px victory/defeat/timeout semantic titles.
- The final serial commands were:

```powershell
dotnet build .\my-team.csproj -maxcpucount:2 -v:minimal
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/FixtureContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/ContentContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/MovementPresentationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/UiSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlphaRunSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --path . --resolution 1280x720 tests/VisualCapture.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --path . --resolution 1600x900 tests/VisualCapture.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . --quit-after 5
dotnet build-server shutdown
```

- Every command exited zero. The build reported zero warnings and zero errors. Content deliberately emits and captures `CONTENT_GATE_STRUCTURAL_INSTANTIATE_FAILURE`, `CONTENT_GATE_INSTANTIATE_FAILURE`, `CONTENT_GATE_READY_FAILURE`, `CONTENT_GATE_PROCESS_FAILURE`, and `CONTENT_GATE_EXIT_FAILURE` before its success marker. UiSmoke deliberately attempts one focus escape while the Army drawer is modal; Godot logs the known focus warning and the suite exits zero.

### Visual evidence and review focus

- Player/enemy/defeat report evidence: `.godot/qa/UI_1280x720_BattleReportPlayer.png`, `.godot/qa/UI_1280x720_BattleReportEnemy.png`, `.godot/qa/UI_1280x720_BattleReportDefeat.png`, `.godot/qa/UI_1600x900_BattleReportPlayer.png`, `.godot/qa/UI_1600x900_BattleReportEnemy.png`, and `.godot/qa/UI_1600x900_BattleReportDefeat.png`.
- Final-victory evidence: `.godot/qa/UI_1280x720_BattleReportFinalVictory.png` and `.godot/qa/UI_1600x900_BattleReportFinalVictory.png`.
- Primary review should independently confirm current-tab gold active treatment is not inverted, inactive tabs remain enabled, the header contains no `tick`, the defeat damage metric visibly reads `0`, metric icons resolve their final value variation color from Theme, and outcome titles retain the authored large semantic hierarchy. It should also verify Blood Rush full/overheal attribution through `BattleSimulation.HealLiving`, sample immutable statistics/exactly-once routing, and confirm no simulation/digest/balance, transaction, or save-schema drift.
- No task-scoped implementation remains. Native observation of the real-time 1.1-second terminal hold and 0.45-second fade remains the only subjective manual acceptance item beyond the automated lifecycle/timing contracts. If verification rejects a concrete check, return this same document to `In Progress`, record the failing evidence, and resume from that check without expanding scope.
