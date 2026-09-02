# Game Mechanics Atlas Visual Grammar Refinement

Status: Complete

## Goal

Correct the published mechanism library's weak information hierarchy and ambiguous rule diagrams. The library must use readable type, compact page structure, content-sized cards, and explicit rule storyboards so a player can understand actor, target, affected space, movement direction, and before/after outcome without guessing.

This round validates the revised visual grammar with three representative mechanisms—charge, threat redirection/taunt, and AOE—before any rollout to the remaining catalog.

## Confirmed Solution

- Treat the screenshot defects as information-design failures, not a color-polish problem: 8–11px copy is too small, the filter/header stack is too tall, mixed illustrated/text cards create empty stretched surfaces, and abstract marks do not explain rule results.
- Raise player-facing library typography to a normal readable scale. Body copy targets 14–16px, compact metadata and tags target at least 11–12px, and card titles remain visually dominant.
- Compress the library introduction and filter area. Keep search and essential filtering immediately reachable; place secondary filters behind an explicit, keyboard-operable advanced-filter disclosure.
- Separate illustrated mechanisms from text-only mechanisms inside the filtered result. Illustrated cards use a consistent visual-card layout; remaining mechanisms use a compact content-sized layout instead of sharing the tallest illustrated row.
- Remove fixed card-body heights and avoid stretched blank card interiors. Responsive layouts must remain readable at desktop and narrow widths without horizontal page scrolling.
- Upgrade the shared diagram styling for all currently registered prototypes with stronger contrast, larger units/arrows/impact marks, and readable in-diagram Chinese labels.
- Structurally redesign exactly three representative diagrams as explicit `命中前 → 命中后` storyboards:
  - `battlefield.engagement.charge`
  - `battlefield.targeting.threat-lock`
  - `army.role.aoe-casters`
- The charge prototype depicts the confirmed baseline branch for this atlas entry: the charger travels in a straight line to first contact; the struck front target moves one cell along the charge direction. If the destination is occupied or outside the board, it does not move and the result becomes collision damage or a disable cue. The diagram must label `首次接敌`, `沿冲锋方向击退1格`, and the blocked/edge result directly; it must not imply lateral throw, off-board removal, or an unconfirmed chain push.
- The taunt storyboard must show original target lines in the before panel and converging redirected lines in the after panel, with direct `原目标` and `嘲讽后` labels.
- The AOE storyboard must show a dense group before resolution and multiple targets affected within one clearly bounded area after resolution, with a direct `范围内同时结算` label.
- Detail views retain accessible play/pause controls. Motion may emphasize the transition but is never required to understand it; `prefers-reduced-motion` leaves the full static result readable.
- The remaining nine registered prototypes are not structurally expanded in this round. They inherit only the shared legibility improvements; the three storyboards are the acceptance samples for deciding the next rollout.
- Keep the public URL, public/no-login access, `noindex` directives, device-local persistence, schema version, localStorage key, JSON import/export, all 72 catalog records, workspace decisions/notes, relationships, and Godot runtime unchanged.
- Update the website module README with the durable visual-grammar boundary: rule diagrams state before/after and direction explicitly, unresolved variants are not silently invented, and exploratory diagrams do not promote rules into gameplay authority.

## Architecture And Authority Impact

- Subsystem: Web presentation and interaction only.
- Reuse the existing `AtlasApp`, `MechanicDiagram`, stable-id diagram registry, and CSS-native 6×4 board grammar.
- The authored catalog remains read-only; no persistence or backend capability is added.
- The charge wording is the confirmed representation for this exploratory atlas prototype. It does not modify `gameplay-design/tower-autobattler-core.md` or become formal runtime authority in this task.
- No Godot scene, resource, C# file, gameplay authority document, or runtime system may change.

## Scope

- Mechanism-library page hierarchy, filtering disclosure, result grouping, card density, typography, and responsive layout.
- Shared HTML/CSS diagram presentation and the three representative storyboard scenes.
- Diagram-specific Chinese labels and charge boundary explanation.
- Focused automated tests and the website README's durable presentation contract.
- Local validation, publication-readiness checks, and user-confirmed publication as Sites version 6.

## Non-Goals

- Redesigning all twelve diagrams as storyboards.
- Extending illustration to the remaining sixty catalog mechanics.
- Generated bitmap art, atmospheric illustration, model-authored SVG, Canvas, or remote image dependencies.
- Final gameplay balance, numeric collision damage, disable duration, chain-push rules, or formal promotion of charge into gameplay authority.
- Persistence schema changes, accounts, cloud synchronization, databases, authentication, or Godot implementation.
- Unrelated cleanup or refactoring.

## Constraints

- Work on `main`; do not create or switch branches.
- Preserve unrelated user changes in the dirty worktree.
- Site code remains under `web/game-mechanics-atlas/`; `web/.gdignore` remains intact.
- Use authored HTML/CSS and accessible semantic controls. Do not add a visual dependency solely for these diagrams.
- Meaning must survive grayscale/reduced-motion conditions through shape, labels, position, line style, and before/after separation.
- Keyboard, mouse, and touch operation remain supported for filters, cards, detail navigation, and playback.
- The production site remains public without login and remains discouraged from indexing.

## Acceptance Criteria

- At the desktop reference width, the library title/filter stack occupies materially less vertical space than the published screenshot and does not dominate the first viewport.
- Search and essential filters are immediately usable; secondary filters are reachable through a labelled, keyboard-operable disclosure.
- Illustrated and text-only results are visibly separated, and text cards no longer stretch to illustrated-card height or contain large empty interiors.
- Library body copy is at least 14px, compact tags/meta are at least 11px, and no mechanism card depends on 8–9px text for primary understanding.
- The three confirmed stable ids render distinct before/after storyboards on summary cards and detail views with direct Chinese labels.
- Charge clearly shows a front target moving one cell along the attack direction. Blocked and edge outcomes explicitly show `不位移` plus a collision/daze outcome; no lateral or off-board interpretation remains.
- Taunt clearly distinguishes original and redirected targets. AOE clearly distinguishes the packed pre-state and simultaneous affected post-state.
- Actor, target, affected cells/path, direction, and before/after outcome remain understandable while paused and without color alone.
- Detail play/pause remains keyboard/touch operable; reduced-motion mode has no required animation.
- The other nine diagram registrations remain valid and inherit shared legibility improvements without receiving speculative new rules.
- The 72-mechanic catalog, schema v3, `tower-atlas.workspace.v2` storage key, JSON/workspace behavior, relationships, public access, and noindex metadata do not regress.
- `npm test`, production build, and lint pass. Production assets contain the new storyboard labels and the existing persistence/noindex markers.

## Progress

- 2026-08-30: User rejected the published library as visually weak, too small, too sparse, and semantically ambiguous, using charge displacement as the representative failure.
- 2026-08-30: Discussion identified the combined causes: undersized type, oversized filter/header stack, stretched mixed-height cards, low-contrast abstract marks, and no explicit before/after or direction labels.
- 2026-08-30: User confirmed the density/type correction, illustrated/text result separation, three-storyboard validation round, and deterministic charge baseline described above.
- 2026-08-30: Activity document created. Execution is ready for the named executor.
- 2026-08-30: Executor started on `main` after confirming the Web-only boundary and dirty-worktree isolation. Implementation was validated locally before publication remained gated on independent verification and user confirmation.
- 2026-08-30: Mechanism-library hierarchy implemented with a compact purpose header, persistent primary filters, native advanced-filter disclosure, separate illustrated/text result groups, content-sized cards, and readable card typography.
- 2026-08-30: Charge, taunt, and AOE now use explicit `命中前 → 命中后` storyboards. Charge also exposes the blocked/edge `不位移 · 碰撞 / 眩晕` branch without lateral throw, removal, or chain-push semantics.
- 2026-08-30: All twelve registered diagram families received shared contrast, scale, direction-label, caption, playback, and reduced-motion legibility improvements. The other nine retain their existing single-scene structure.
- 2026-08-30: Website README now records the durable diagram visual-grammar and exploratory-authority boundary.
- 2026-08-30: Focused tests passed with 17/17 checks, including 72 catalog records, schema v3, `tower-atlas.workspace.v2`, advanced-filter disclosure, result grouping, typography floors, exact three-storyboard scope, required Chinese labels, README boundary, persistence migrations, and rendered noindex metadata.
- 2026-08-30: Production build passed and emitted the new storyboard labels plus the unchanged device-local storage marker. ESLint completed with zero findings.
- 2026-08-30: Implementation completed locally and remained unpublished until independent verification and explicit user confirmation authorized the main Agent to publish it.
- 2026-08-30: Independent static geometry review found two release-blocking presentation gaps: charge branch labels could overlap/crop, and the AOE storyboard needed to constrain its defeat state to low-health swarms. Executor resumed for those two corrections only.
- 2026-08-30: Charge blocked/edge labels were moved into a bounded two-line explanation area at the lower-left of the after panel, clear of the enemy, impact mark, obstacle, and left crop boundary. Focused assertions reject the former coordinates.
- 2026-08-30: AOE before-label, accessible description, caption, steps, audit, and pressure copy now consistently state `密集低生命敌群`, so the defeated marks do not imply an unconditional kill against arbitrary targets.
- 2026-08-30: Final pre-publication verification after both corrections passed: 17/17 tests, production build, and lint with zero findings.
- 2026-08-30: After the user confirmed publication, the main Agent published Sites version 6 to the existing public project. Project `appgprj_6a931d9d3c608191a395abc5081c8944` reported `active`, `latest_version_number: 6`, and `access_mode: public`.
- 2026-08-30: Deployment `appgdep_6a940e7dacf481919df1f95a12710c48` succeeded at `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`.
- 2026-08-30: Independent production checks returned HTTP 200 for anonymous-homepage and Googlebot requests with no login redirect. All seven referenced JavaScript/CSS resources returned HTTP 200.
- 2026-08-30: Production HTML/assets preserved `noindex,nofollow,nocache`, Googlebot `noimageindex`, `图解机制`, `文字机制`, `更多筛选`, `命中前`, `命中后`, `沿冲锋方向击退1格`, `不位移 · 碰撞 / 眩晕`, `密集低生命敌群`, and `tower-atlas.workspace.v2`.

## Resume Condition

Current resume entry: none. The confirmed visual-grammar refinement is implemented, independently verified, and publicly deployed as Sites version 6. Any follow-up visual revision or rollout beyond the three confirmed storyboards begins as a new discussed and confirmed activity.

## Verification Handoff

Complete. The main Agent independently verified the implementation and, after explicit user confirmation, published Sites version 6.

Implemented behavior:

- The mechanism library now uses a compact page-purpose header. Search, domain, and decision state remain immediately visible; dependency, agency, complexity, and depth use a native keyboard-operable `details` disclosure labelled `更多筛选`.
- Filtered results are split into `图解机制` and `文字机制`. Both use content-sized cards, while illustrated cards have their own two-column desktop grid. Card body copy is 14px, metadata/tags are 11px, titles are 22px, and the former 250/330/40/54px card-content minimum heights are gone.
- Charge, taunt, and AOE alone use structural before/after storyboards. Charge explicitly shows first contact, one-cell displacement along the charge direction, and the blocked/edge no-displacement collision/daze branch. Taunt shows original target lines before and converging redirected lines after. AOE preserves one bounded area while multiple targets resolve simultaneously.
- All twelve prototypes inherit larger units, arrows, impact marks, stronger board contrast, and direct Chinese semantic labels. Playback remains a semantic button, and reduced-motion mode keeps every static result visible.
- The README records that diagrams must state before/after and direction, cannot invent unresolved branches, and do not promote exploratory ideas into gameplay authority.

Verification evidence:

- `npm test`: 17 passed, 0 failed, including coordinate regression coverage for the charge branch labels and low-health semantic coverage for the AOE defeat storyboard.
- `npm run build`: passed with Cloudflare-compatible output.
- `npm run lint`: passed with 0 findings.
- Production output contains `范围内同时结算` and `tower-atlas.workspace.v2`; rendered-server tests confirm the existing robots and Googlebot noindex directives.
- Catalog count remains 72; state schema remains 3; storage key remains `tower-atlas.workspace.v2`; no dependency or hosting configuration changed.
- Sites project `appgprj_6a931d9d3c608191a395abc5081c8944`: status `active`, latest version 6, access mode `public`.
- Deployment `appgdep_6a940e7dacf481919df1f95a12710c48`: status `succeeded`; production URL `https://tower-mechanics-atlas.lingqi0131.chatgpt.site`.
- Anonymous homepage and Googlebot requests: HTTP 200 with no login redirect. All seven JavaScript/CSS resources referenced by the production page also returned HTTP 200.
- Production HTML/assets contain the required indexing, hierarchy, storyboard, charge-boundary, AOE low-health, and persistence markers: `noindex,nofollow,nocache`, Googlebot `noimageindex`, `图解机制`, `文字机制`, `更多筛选`, `命中前`, `命中后`, `沿冲锋方向击退1格`, `不位移 · 碰撞 / 眩晕`, `密集低生命敌群`, and `tower-atlas.workspace.v2`.

User-facing final review:

1. Open `https://tower-mechanics-atlas.lingqi0131.chatgpt.site` at a desktop width and confirm the purpose/filter stack no longer dominates the first viewport, advanced filters open from the native disclosure, and text cards do not stretch to illustrated-card height.
2. Inspect charge, taunt, and AOE in both summary and detail. Confirm all required labels remain legible while paused and that charge cannot be read as lateral throw, off-board removal, or chain push.
3. At narrow width, confirm result sections, filters, and the three storyboards reflow without horizontal page scrolling. Check play/pause by keyboard and touch and repeat with reduced motion enabled.
4. Change filters, decisions, and a note, refresh, and confirm the existing device-local workspace restores them.

Known limitation: the Sites snapshot exposed only the homepage, and no usable in-app browser instance was available. The mechanism library's actual visual and interaction quality therefore still needs user review at the production URL. This does not block the completed static validation, successful deployment, anonymous-access checks, or production-resource verification.
