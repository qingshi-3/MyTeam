# Engagement, Navigation, Facing, and Combat-State Repair

Status: Completed  
Confirmed: 2026-08-28

## Goal

Repair the combat decision and presentation defects that make units wander, oscillate, wait without an understandable reason, or fail to join combat. Add reliable horizontal facing, preserve immediate death cleanup, and expose the selected unit's current action reason without changing the game's deterministic grid-combat identity.

## Confirmed Product Behavior

- Every living unit participates from the first simulation tick. There is no distance-gated combat activation state.
- A living unit first performs a legal action from its current cell. It does not walk past or keep pursuing a distant target while a valid enemy is already attackable.
- A healer prioritizes wounded allies that it can actually reach and legally heal. Healing requires range and line access. If the lowest-health ally is not a legal target, the healer tries other wounded allies. If no wounded ally has a legal healing plan, the healer participates in ordinary enemy engagement.
- A healer with a legal wounded-ally plan keeps protecting that ally through movement or cooldown, consistent with the accepted healer contract. A temporarily blocked heal plan may wait briefly, but cannot wait forever while other legal wounded targets or ordinary combat actions exist.
- Strategic target identity and the selected engagement/staging cell are separate. Temporary traffic does not immediately discard a living, relevant target.
- Enemy selection uses the actual path cost to a legal attack position as its primary spatial cost. Authored role preferences remain valid secondary priorities. Straight-line distance alone is not authoritative.
- A newly attackable enemy may interrupt a distant pursuit. Equal choices use deterministic, team-neutral tie-breaking and target hysteresis so units do not switch every tick.
- A unit blocked for a bounded number of movement-ready decisions waits while retaining its target; after the bound it replans its goal or chooses another reachable target. It never oscillates between fallback cells merely because reservations changed.
- Selected-unit details explain legitimate inactivity in Chinese, including at least seeking/engaging, moving, waiting for a route, attacking, healing/casting, attack cooldown, disabled, and defeated. Cooldown feedback should use player-facing time rather than internal tick jargon where practical.

## Confirmed Simulation and Navigation Contract

### Two-phase deterministic planning

1. At tick start, capture one living-unit occupancy/terrain snapshot.
2. Each living unit derives legal immediate actions, target candidates, engagement candidates, and optional next-step candidates from that same snapshot. Candidate generation must not mutate reservations in iteration order.
3. A central deterministic arbitration pass assigns unique goals and next cells, then commits accepted adjacent moves together.
4. Raw `RuntimeId` lexical ordering must not grant an entire team first access to the battlefield. Waiting age plus a stable team-neutral initiative/tie-break may be used; the result must remain reproducible for the same battle seed and setup.

### Occupancy, goals, and traffic

- One living unit occupies one legal grid cell. Movement remains orthogonal and one cell per accepted move event.
- An engagement reservation is exclusive ownership of a desired final cell; it is not a terrain wall and cannot make that cell globally untraversable along every path.
- Current enemy-occupied cells are never traversable. A next step into a friendly-occupied cell is accepted only when that occupant has an accepted move that vacates it in the same resolution chain.
- Friendly follow chains are allowed. Same-cell commits, direct swaps, and dependency cycles are rejected deterministically.
- A unit already in a legal attack/heal position releases any obsolete future-cell reservation immediately. Every retained reservation must correspond to a living owner, a living/relevant target, and observable progress or a bounded wait lease.
- When no final attack cell is currently assignable, a unit may choose a reachable staging cell/step that strictly improves path distance or line access. It must not wander to an equal/worse cell merely to emit movement.
- Movement arbitration age represents actual route obstruction only. It resets or decays after a successful move, attack, heal, target replacement, defeat, or another completed action, so stale waiting history cannot grant false priority.

### Death cleanup

- All production damage paths use the authoritative damage/death transition rather than directly mutating health through an alternate path.
- Death is terminal unless a separately confirmed resurrection mechanic explicitly says otherwise. Ordinary healing, lifesteal, and later status writes cannot restore health to a defeated unit.
- Death immediately releases the dead unit's goal, target, reservation, and queued intent, plus target links, reservations, and invalid queued intents of units pursuing it.
- Dead units are excluded from occupancy, path traversal checks, selection, and input hit testing in the same tick.
- A dead queued mover is discarded without changing `Defeated` back to `Waiting`.
- The following tick, battle replacement, and disposal retain defensive cleanup of all stale planning state.
- The visible corpse/defeat animation is presentation only: it has no collision or navigation authority, cancels spatial travel at the visible point, and cannot slide or become selectable afterward.

## Confirmed Facing Contract

- Facing is presentation state; it does not enter the battle digest or influence combat legality.
- `UnitAnimationComponent` owns horizontal sprite mirroring and exposes an editor-authored `AuthoredFacingRight` setting, defaulting to the current asset convention while allowing per-content-scene override.
- Player units initially face right and enemy units initially face left.
- Facing updates when an actual movement segment begins, not when a future waypoint is merely queued. Horizontal movement sets facing; a purely vertical segment keeps the last facing.
- An attack or heal/cast faces its real target before the action cue. Direction is derived from current visible presenter position versus target world position so presentation lag does not produce a backwards action.
- Defeat locks facing and rejects later updates. Bind/reset restores the team default.
- Only the character `AnimatedSprite2D` is mirrored. Health bars, hero identity, near/ranged markers, labels, and other readable UI are never mirrored.

## Architecture and Authority Impact

- This is a Combat / AI / Navigation simulation repair plus Presentation and battle-inspection UI work.
- `BattleSimulation` remains authoritative for action legality, state, damage, healing, death, cooldowns, and semantic events.
- The replaceable deterministic grid movement service owns snapshot planning, target/goal state, reservations, staging, dependency-safe arbitration, and planning cleanup.
- Presentation motion continues to consume ordered move events and never chooses cells. Animation presentation alone owns sprite facing.
- The selected-unit panel renders simulation facts and does not infer or mutate combat decisions.
- Before runtime changes, update `gameplay-design/tower-autobattler-core.md` and `system-design/tower-autobattler-architecture.md` to replace the old sequential-reservation contract with this confirmed behavior. Extend `docs/testcases/alpha-manual-qa.md` with the player-visible acceptance cases.

## Scope

- Refactor target selection and deterministic grid movement as required to implement two-phase planning.
- Repair ordinary ranged and healer engagement behavior and line-access consistency.
- Add bounded target/goal stability, staging, fair movement arbitration, and reservation leases/cleanup.
- Add horizontal facing through existing reusable unit presentation components and authored scene resources.
- Add selected-unit action-state feedback using the existing authored panel scene; do not construct a new control tree in business code.
- Add focused automated regression coverage and update existing affected expectations/digests only where the confirmed behavior intentionally changes them.
- Maintain this document at execution start, important milestones, waiting-for-verification handoff, and final disposition.

## Non-goals

- No `NavigationAgent2D`, navigation mesh, continuous free movement, diagonal movement, stacking, pushing, direct unit swapping, or physics avoidance.
- No behavior-tree addon or per-unit node-state-machine rewrite. The existing compact simulation enum may be extended or clarified; simulation and presentation states remain separate concerns.
- No new heroes, soldiers, enemies, items, levels, save schema, formation capacity, or battle command design.
- No balance pass beyond behavior changes necessarily produced by fixing participation and path choice.
- No change to the accepted movement interpolation timings, pause/speed continuity, animation cue priority, or independent content-scene contract.

## Required Automated Regressions

- Two melee units beginning at `(0,2)` and `(4,2)` with range/move interval 1 must not enter an `A -> B -> A` positional loop and must eventually attack.
- A range-4 unit at `(0,2)` versus a melee unit at `(6,2)` must not step off-axis and then return to its origin before attacking on an otherwise empty board.
- The production-like 7v8 opening must not let lexical ID order make the whole second team wait. Both sides must receive legal action/planning outcomes, and a ranged unit with a valid enemy plan cannot remain `target none + Waiting` for repeated unexplained ticks.
- Another unit's future engagement goal must not be a global path wall. Reaching attack range must release/re-anchor an obsolete future goal, and no live ghost reservation may persist without progress.
- Target choice must cover straight-line-near/path-far versus straight-line-farther/path-shorter, plus an immediately attackable new threat interrupting a distant pursuit.
- Friendly follow chains must preserve unique cells and event order while rejecting swaps and cycles.
- `HealPower == 0` and `HealPower > 0` otherwise-equivalent ranged units must both join combat when nobody is wounded. Time Arbiter versus a longer-range enemy cannot stand still forever while only self-healing.
- Healing must cover line access, unreachable lowest-health ally with a reachable alternative, cooldown protection of a valid heal target, and fallback to combat when no wounded ally has a legal plan.
- A unit queued to move and killed later in the same tick emits no move, retains `Defeated`, releases every dependent target/reservation/intent immediately, and leaves its cell reusable. Next-tick, disposal, and battle-replacement cleanup remain empty.
- A lifesteal attacker killed by its victim's on-death damage remains at zero health and `Defeated`, performs no next-tick action, retains no target or planning state, and leaves its cell reusable. Ordinary healing still works for living units and is a no-op for dead units.
- The bounded goal-wait lease regression must enter a real retained-goal wait: several movement-ready decisions keep the same target without moving, and only the lease-boundary decision retargets to a legal alternate.
- Determinism, repeated-seed digest equality, one-living-unit-per-cell, orthogonal adjacent moves, no waiting move event, floor-rule damage, summons, and battle outcomes remain covered.
- Facing covers team defaults; left/right moves; vertical retention; queued `right -> down -> left` segment timing; attack/heal toward a target during presentation lag; mutual attacks; defeat lock; rebind reset; and non-mirrored readability labels.
- Selected-unit state text covers every supported simulation mode and distinguishes attack cooldown from route waiting.

## Manual Acceptance

- In representative crowded fights at 1x and 2x, units advance coherently, briefly queue behind allies, and do not pace, reverse repeatedly, or route around visually empty reserved cells.
- Melee, ranged, healer, hero, enemy, and summon units visibly participate according to role. Any stationary living unit has an understandable selected-unit status.
- Units face their direction of travel and face attack/heal targets; vertical travel and defeat do not cause arbitrary flips. Hero/range markers and health UI stay readable.
- A corpse may finish its defeat animation but never blocks a route, receives selection, resumes movement, or causes a follower to retain a dead target.
- Pause and 1x/2x/4x movement presentation remain continuous and ordered.

## Verification

- Run static review before engine/build work.
- Use the project's low-concurrency build command: `dotnet build my-team.csproj -maxcpucount:2 -v:minimal`.
- Run the focused gameplay/navigation/presentation/UI contracts first, then the complete existing contract matrix and a short headless main-scene startup.
- Do not repeatedly launch the editor or trigger concurrent builds/imports. Shut down idle .NET build servers after verification when appropriate.
- Independent review must check simulation correctness/determinism, facing and UI ownership, scope/doc consistency, and the recorded test evidence before completion.

## Progress

- 2026-08-28: User confirmed the complete repair scope after read-only audits of facing, navigation, ranged/healer engagement, and death occupancy.
- 2026-08-28: Confirmed the dominant causes are sequential future-cell reservations acting as global walls, lexical team-order bias, target/goal coupling, path-cost mismatch, healer early return, obsolete live reservations, absent facing state, and absent action-reason feedback. Normal production death already releases occupancy, but defensive same-tick cleanup remains in scope.
- 2026-08-28: Activity document created. Implementation has not started.
- 2026-08-28: Execution started on `main`. Read the active task, project authority, prior movement/polish work items, manual QA, and the applicable navigation/state/testing/debugging/UI skills. The existing worktree is entirely untracked project content and is being preserved in place.
- 2026-08-28: Architecture check passed: Combat simulation retains legality/action/death ownership; the replaceable grid movement service will own tick-snapshot planning and arbitration; motion/animation own visible segment direction and sprite-only facing; the existing authored selected-unit panel will render explicit simulation facts. Updated gameplay, system, and manual-QA authority before runtime edits.
- 2026-08-28: Focused RED reproduced the healer participation defect after a low-concurrency build. `GameplayContractSmoke` exited 1 with `full-health healer failed to join ordinary combat when no legal heal target existed`; the otherwise-equivalent non-healer control remains part of the regression. No runtime implementation had been changed at this point.
- 2026-08-28: First GREEN milestone implemented. Strategic targets are now separate from centrally assigned engagement goals; all move candidates read a tick snapshot, goals are not path walls, movement uses stable team-neutral initiative, and the resolution supports friendly follow chains while rejecting conflicts, swaps, and cycles. Attack/heal completion releases obsolete goals, and authoritative death clears same-tick requests and dependent planning state.
- 2026-08-28: Healers now select reachable wounded allies by legal range/LOS plans, retain a valid protected ally through cooldown, try reachable alternatives, and fall back to ordinary engagement when no legal heal exists. Explicit battle action target/kind facts drive the existing selected-unit panel's Chinese state and second-based cooldown text.
- 2026-08-28: Presentation facing is implemented in reusable components: team defaults bind/reset correctly; real horizontal segment starts and attack/heal targets update sprite-only facing; vertical motion retains direction; defeat locks facing. No facing data enters simulation events or digest.
- 2026-08-28: Expanded focused regressions now pass. `GameplayContractSmoke` covers two-phase navigation, both-team crowded participation, path-cost targeting, immediate-threat interruption, follow chains, cycle rejection, healer fallback/LOS/alternative/cooldown, queued-mover death, and every selected action string. `MovementPresentationContractSmoke` covers team defaults, queued segment timing, vertical retention, target facing, defeat lock, rebind, and sprite-only mirroring. `UiSmoke` confirms the authored action label in the production selection flow. Low-concurrency build reports 0 warnings and 0 errors.
- 2026-08-28: Full-matrix testing initially exposed one integration regression: the commander path lost floor 15 because a nominally neutral hash still reused one permanent runtime-id priority across every battle. Movement initiative is now salted by the battle seed, preserving exact same-seed reproducibility while removing the cross-battle permanent side/order bias. All three 15-floor paths pass again without balance-data changes.
- 2026-08-28: Final static review against the Godot review checklist found no scene-ownership, resource-construction, signal-lifecycle, digest, or runtime-path violations. Disabled units now also release obsolete goals and stale waiting age immediately. Final build and focused replay passed, and idle MSBuild/Roslyn servers were shut down.
- 2026-08-28: Execution is complete and awaiting independent verification. No Git commit was created and no donor, save-schema, content, item, hero, soldier, enemy, level, or balance resource was changed.
- 2026-08-28: Independent verification returned FAIL and reopened execution. Confirmed blockers are: candidate generation still reads/mutates shared goal claims instead of producing a request-order-independent tick snapshot plan; central arbitration does not retry a request's next-best goal/step after scarcity, conflict, or dependency rejection; blocked routes can repeatedly select the same failed first step instead of taking a bounded detour or retargeting at the lease boundary; range-2 attacks/heals bypass intermediate terrain LOS; death cleanup leaves target-facing facts and lifecycle planning residue under queued-mover, replacement, or disposal cases; and several required regressions used synthetic or insufficiently production-like setups for 7v8 ranged participation, new-threat interruption, future-goal traversal/lease release, Time Arbiter engagement, attack/heal facing routing, and authored-left sprite mirroring.
- 2026-08-28: Correction execution started on `main`. The confirmed healer exception remains authoritative: a healer with a legal wounded-ally plan protects that ally before ordinary enemy attacks. The correction is limited to the accepted two-phase deterministic planner, bounded replanning/retargeting, uniform LOS, same-tick death/lifecycle cleanup, production presentation routing, and truthful regression coverage; no gameplay direction, content balance, save schema, or donor boundary change is authorized.
- 2026-08-28: Correction RED contracts now include a flexible-versus-constrained scarce-goal counterexample with reversed request enumeration, a static-friendly shortest-step detour, lease-boundary retargeting, range-2 attack/heal walls, a genuinely different newly entering threat, ranged-inclusive production-like 7v8 inactivity, same-tick dependent death cleanup and dead-cell reuse, disposal cleanup, and actual Time Arbiter content versus a longer-range enemy. The low-concurrency build remained clean; the pre-fix Gameplay suite reproduced the first expected failure with `range-two attack ignored the intermediate terrain wall`.
- 2026-08-28: Correction GREEN milestone completed. All queued requests now finish ordered candidate generation from the same tick snapshot before any shared goal mutation. Central maximum-cardinality matching jointly assigns scarce goals and next cells; dependency or reciprocal-chase non-progress rejection bans only the blocking candidate and reruns so a next-best goal/step can succeed. Static-friendly detours use actual movable-friend knowledge, bounded wait leases force a reachable alternate target, and future goals never enter terrain traversal. Uniform terrain LOS now applies at every range.
- 2026-08-28: Authoritative death now clears every unit's target-facing facts for the dead runtime id before dependent planning cleanup; disposal clears pending events, action-target facts, goals, requests, retarget leases, and snapshots. Regressions prove a queued dead mover stays `Defeated`, dependent planning reaches zero, and another unit can actually move into the freed death cell in the same tick. Production battle replacement also clears the old simulation facts and planning service.
- 2026-08-28: Production presentation regressions now drive real `BattleScreenController` attack/heal event routing: reversed-position mutual attackers face each other in the same tick, a real healer faces its protected ally, and `AuthoredFacingRight=false` preserves logical team facing while mirroring only the sprite. The low-concurrency build, focused Gameplay, and MovementPresentation suites pass with zero warnings/errors and their expected success markers.
- 2026-08-28: Final correction static review passed. Candidate generation contains no shared-goal write, future goals do not enter traversal, request-order neutrality is explicit, matching/retry work is bounded by the finite candidate set, and per-candidate requesting-set copies were removed from the hot path. The accepted healer priority, simulation/presentation ownership, deterministic grid identity, independent content scenes, donor boundary, save schema, and content balance remain unchanged.
- 2026-08-28: Final strict serial verification passed: low-concurrency build `0 warnings / 0 errors`; focused Gameplay, MovementPresentation, and UI; complete Fixture, Content (including all five intentional negative lifecycle gate markers), and AlphaRun; then clean five-second headless startup. Gameplay reports `two-phase,scarcity,request-order,detour,retarget,...los-range2,death-cleanup,dead-cell-reuse,time-arbiter`; MovementPresentation reports `authored-left,...production-attack-heal,mutual-attack,...replacement-planning`; all three 15-floor paths still complete. Idle MSBuild/Roslyn servers were shut down. Correction is awaiting independent reverification; no commit was created.
- 2026-08-28: Second independent reverification returned FAIL and reopened correction execution. A nested on-death damage callback can kill and release a lifesteal attacker inside `ApplyDamage`, after which the outer attack path unconditionally restores its health, creating an `Alive` unit whose mode and planning state remain defeated/released. The accepted correction makes death terminal across every production health-increase path without changing splash, pierce, slow, healer priority, or content balance.
- 2026-08-28: The same reverification found the lease-retarget regression did not actually wait: its blocked target had no assignable engagement goal, so candidate scoring discarded it on the first decision. The replacement contract uses a genuinely retained open goal and asserts multiple movement-ready waits before lease-boundary retargeting.
- 2026-08-28: Correction execution resumed on `main` after rereading project rules, this activity, gameplay/system authority, the completed movement/polish activities, and the applicable debugging/testing/navigation review guidance. The all-untracked workspace remains preserved in place.
- 2026-08-28: New RED coverage compiled with `0 warnings / 0 errors` and reproduced the terminal-death defect before runtime repair: the synthetic on-death/lifesteal counterexample exited through `GAMEPLAY_CONTRACT_FAILED` with attacker `health=10, mode=Defeated, target=`. The stricter lease test reached this later test, confirming four retained-target waits and fifth-decision retargeting under the current `GoalWaitLease=4` contract.
- 2026-08-28: Terminal-death correction is GREEN. Ordinary healer and floor-rule healing now share a living-target gate; lifesteal checks the attacker again after nested damage resolution; Blood Rush refuses a dead hero even outside the authoritative command gate. Synthetic and real `soldier_blood_baroness` versus `soldier_abyss_crawler` regressions prove zero health, `Defeated`, empty action/planning state, same-tick dead-cell reuse, and no following-tick action, while living lifesteal remains unchanged.
- 2026-08-28: Static health-write review found no other current-battle resurrection path. Battle construction and summons initialize new runtime instances; run-level `HealthRatio` recovery belongs to between-encounter progression and does not mutate a defeated `BattleUnitState`. The focused low-concurrency build is clean and Gameplay now reports `death-terminal` with the full success marker.
- 2026-08-28: Final correction review found no ownership or scope regression. The terminal-health guard remains inside combat simulation and the existing Blood Rush runtime; the movement-service additions expose only the accepted lease constant and a focused internal lifecycle diagnostic for regression coverage. Landed splash, pierce, slow, healer priority, content data, save schema, presentation, donor boundary, and one-unit-per-cell behavior are unchanged.
- 2026-08-28: Final strict serial verification passed: low-concurrency build `0 warnings / 0 errors`; Fixture; Content with all five intentional structural/instantiate/ready/process/exit negative markers; Gameplay with `death-terminal`; MovementPresentation; UI with its accepted modal-focus warning; all commander/carry/solo 15-floor paths; and clean five-second main-scene startup. Idle MSBuild/Roslyn servers were shut down. This correction is awaiting independent reverification; no commit was created.
- 2026-08-28: Final independent reverification passed. Navigation verification independently confirmed build `0 warnings / 0 errors`, Gameplay, Content, and AlphaRun success; authoritative death releases occupancy in the same tick; and the real blocked-route lease retains its target for the first four movement-ready decisions before retargeting on the fifth. Scope verification confirmed lifesteal multipliers, on-death damage, healing rules, content data, and combat balance were not changed. All task acceptance criteria are satisfied.

## Completion State

This task is complete and independently accepted. There is no remaining resume action; any later defect or direction change starts a new discussion and work item under the accepted authority boundaries.

## Verification Handoff

Status: Completed. Final navigation and scope reverification passed on 2026-08-28.

Changed runtime/resources:

- `src/Battle/DeterministicGridMovementService.cs`
- `src/Battle/BattleSimulation.cs`
- `src/Battle/BattleModels.cs`
- `src/Battle/HeroCommandContracts.cs`
- `src/Components/UnitAnimationComponent.cs`
- `src/Components/UnitMotionPresentationComponent.cs`
- `src/Content/UnitContentRoot.cs`
- `src/Presentation/BattleScreenController.cs`
- `src/UI/SelectedUnitPanel.cs`
- `scenes/ui/components/SelectedUnitPanel.tscn`

Changed authority/tests:

- `gameplay-design/tower-autobattler-core.md`
- `system-design/tower-autobattler-architecture.md`
- `docs/testcases/alpha-manual-qa.md`
- `tests/GameplayContractSmoke.cs`
- `tests/MovementPresentationContractSmoke.cs`
- `tests/UiSmoke.cs`
- this active work item

Exact verification commands:

```powershell
dotnet build my-team.csproj -maxcpucount:2 -v:minimal
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/FixtureContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/ContentContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/GameplayContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/MovementPresentationContractSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/UiSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . tests/AlphaRunSmoke.tscn
& 'C:\Users\qs\Desktop\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe' --headless --path . --quit-after 5
```

Recorded success markers:

- Build: 0 warnings, 0 errors.
- `FIXTURE_CONTRACT_OK`
- `CONTENT_CONTRACT_OK entries=57 floors=5 events=90`
- `GAMEPLAY_CONTRACT_OK ... navigation,two-phase,scarcity,request-order,detour,retarget,fairness,follow-chains,cycle-rejection ... healing,los-range2,death-cleanup,death-terminal,dead-cell-reuse,time-arbiter ...`
- `MOVEMENT_PRESENTATION_CONTRACT_OK ... facing=team-defaults,authored-left,segment-timing,vertical-retention,production-attack-heal,mutual-attack,defeat-lock,sprite-only ... replacement-planning ...`
- `UI_SMOKE_OK screens=11 ... interaction=modal-focus,battle-selection,zero-mana`
- `ALPHA_RUN_OK paths=commander,carry,solo regions=3 floors=15`
- Headless main-scene startup exited 0.

Known residual verification:

- Automated contracts cover state and routing but cannot judge crowded-fight readability. Independently perform the `Engagement, Navigation, Facing, And Action State` manual section at 1x/2x, including a visible `right -> down -> left` route, a static-friendly blocked-shortest-step detour, four-wait/fifth-decision lease retargeting, lethal on-death damage against a lifesteal attacker, corpse selection/occupancy, and the selected panel at both target resolutions.
- `UiSmoke` retains its pre-existing expected focus warning during its deliberate modal-focus escape probe; the scene exits 0 with the accepted success marker.
- Independent review should pay special attention to request-order reversal, scarce-goal displacement, candidate retry after dependency rejection, reciprocal melee non-progress, battle-seeded initiative neutrality, goal/retarget-lease cleanup, the healer protection exception, and sprite-only mirroring. No other known runtime failure remains.
