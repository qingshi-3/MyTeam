# Alpha Manual QA

## 血鼓与医师身体覆盖施法（2026-09-16）

自动入口：`tests/SourceCastVfxSmoke.tscn` 检查正式技能事实、贴帧/翻转/变换、材质隔离及生命周期；`--reduced` 渲染浅底减少动态，`--battle` 使用正式 BattleScreen，`--preview` 以鼠标/键盘事件检查正式特效预览室选择、暂停、单步、倍速和重播。`HealingReaderVisualSmoke.tscn` 保留双目标不重复、普通治疗动作次数及暂停/恢复检查。日志和正常速度对照见 VFX 活动任务。

玩家复查：预览室搜索“血鼓”或“回春”；血鼓应出现贴近胸腹的短促红色能量释放，医师应出现绿白上升柔带，人物仍为原 idle。检查覆盖有无错位、体型是否仍清楚、释放是否可辨；正式战斗中双目标治疗只亮一次源端。源光强度、混战可读性与玩家观感待试玩。下节的旧通用短光已被这两项独立效果替换，其他主动技能仍保留通用回退。

## 济世医师满蓝与施法者反馈（2026-09-16）

自动入口：`HealingReaderContractSmoke.tscn`（正式 HC18 接近、目标／法力、控制、回滚）；`HealingReaderVisualSmoke.tscn`（正式 BattleScreen 私有战斗，真实暂停／加速／恢复输入，双目标治疗与身体短光）。相关移动及法力检查、录制和限制见 BC 英雄活动任务最新段。

玩家复查：让满蓝济世医师与受伤队友相距超过4格，确认靠近后释放续命针；全员满血应保留法力。施放时本人身体短亮，两个受治疗者各自显示治疗；暂停后短光停住，恢复后消退。其他满蓝主动施法也复用源端短光，特效预览可选“施法短光”。玩家观感、强度和整局节奏尚未验收。

动作复查：续命针采用中性姿态＋身体短光，随后16点普通治疗才播放原普通动作；不能再把两个动作当作相同skill_cast排队连播。`HealingReaderVisualSmoke --trace-animation`核对一次主动、两笔70治疗与各次16治疗的实际动作开头，包含真实暂停／倍速／恢复和死亡／重置清理。前后录制与旧移动夹具门禁失败记录见BC活动任务最新段。

## 英雄信息场景复用（2026-09-16）

`tests/UnitInformationInputSmoke.tscn` 用当前发布内容及独立 authored 场景检查紧凑卡片、开局详情、准备详情的共用信息：基础值／准备值分离、准备数值复制、有效技能／成长被动、实际生命／法力／护盾、换人清理。真实鼠标／键盘输入检查属性与技能说明、返回焦点、紧凑卡片键盘提示、页面出征意图及锁定状态。`tests/RosterLoadoutInputSmoke.tscn` 继续覆盖正式组合根中的卡片边界、选人、配装拖放、悬停仅一个提示、滚动和模态操作。两个入口均通过，截图已查看；结果见 UI 活动任务顶部。

玩家复查：在选英雄页、部署详情和“英雄与装备”中查看同一英雄，确认信息易读；准备加成应有明确上下文，升级后的被动按有效技能说明展示。战斗实时面板和招募候选仍有各自的展示布局。本次不代表全项目 UI 或玩家体验已验收。


## 默认最大化与底部备战席（2026-09-16）

`tests/FormationDragOnlyInputSmoke.tscn` 已改用正式默认启动模式，检查原生窗口最大化并保留边框；用真实指针事件／命中检测完成底部备战席上场、场上移动／交换、空备战席撤回、拖到已有备战英雄上撤回、替换、满员拒绝、敌区取消、Esc取消及Tab／Enter只检查不移动。使用正式GameRoot、隔离命名空间及内存征程，合法动作恰提交一次。恢复1280×720后检查备战席、卡片、开始按钮边界及肖像点选。结果与截图见[UI活动任务](../../work-items/active/battle-lab-ui.md)最新段。

玩家复查：正常启动应占满桌面可用区域，标题栏可直接关闭／还原；进入部署即看到下方备战席，无需右上角入口。上场后卡片离开备战席，下场后回来，空席仍可接收英雄。全体英雄与装备入口保留。固定尺寸的旧UI输入入口需加`--windowed`，避免把逻辑坐标误当成最大化后的窗口像素。

下方2026-09-15的非模态后备窗记录为历史，本节取代其入口与布局；实验室和全屏装备管理边界保持。

## 后备与仅拖动布置（2026-09-15）

`tests/FormationDragOnlyInputSmoke.tscn`：正式 GameRoot + 内存存档，实际打开后备名册、滚到新招英雄、拖肖像上场、场上移动／交换、拖进底部撤回区、后备替换占用格。连续点英雄和空／占用格、点击撤回区都不能改变阵型或保存；拖到敌区或按 Esc 取消不提交，合法拖放恰保存一次。专项已通过，名册卡片和滚动截图已查看。后备名册是非模态工具窗；英雄与装备管理仍全屏。

实验室在 `EquipmentDragInputSmoke` 中补查点模板再点空格不新增、点棋子再点空格不移动，实际拖动新增和移动仍可用；已通过。旧“先选后点格”操作记录被本节替代。验证只用隔离数据，不代表玩家体验确认。

## 当前全屏名册配装检查（2026-09-15）

入口：备战“英雄与装备”或右上资源条→军团默认页。`tests/RosterLoadoutInputSmoke.tscn` 使用真实 GameRoot、隔离命名空间与内存征程；检查整卡肖像选择、属性／技能／装备 hover、同屏穿戴／替换／跨英雄转交／拖回图标和空白卸下、滚到最后后备配装、关闭／Esc／Tab。装备库存和英雄列表各自滚动，根场景资源条不能从模态窗穿透点击，卡片子控件必须在卡内。

1600×900 专项已通过，截图及限制见 [UI 活动任务](../../work-items/active/battle-lab-ui.md)最新段。后备／全局军团明确为基础属性；备战出战单位为准备投影。未做全分辨率／完整征程。下方“正式装备工具窗可移动／窗外棋盘配装”和“军团小型居中窗”是被本轮替代的历史路径；实验室工具窗路径保持。

## 当前弹窗与战场检查（2026-09-15）

使用 `tests/ContextPopupInputSmoke.tscn` 检查部署／军团／战斗信息窗；`tests/EquipmentDragInputSmoke.tscn` 检查工具窗到棋盘的真实拖放及实验室信息入口。均使用隔离内存数据，不写真实存档。下方旧固定侧栏路径属于历史记录，当前恢复以本节和活动任务为准。

- 备战／实验室默认无展开的大侧栏；选择与移动棋子不会强开详情。战斗默认只有必要状态、紧凑指令和查看入口。
- 详情、规则、统计、团队设置按需弹出；关闭按钮、Esc、点击遮罩能退出。模态 Tab 留在窗口中，点击遮罩不穿透触发底层按钮；关闭后焦点回到入口。开关前后棋盘矩形不变。
- 装备／名册／单位库是工具窗，窗外可继续拖放；可拖动标题露出被遮住的格子。穿戴、转交、替换回包和卸下保持此前语义。
- 看属性、技能、装备、遗物等内容是否能清楚区分主次、滚到完整说明；军团是有边界的居中弹窗。此次不更改候选选择页与战斗结算报告的核心版式。

## 当前主流程 UI 检查（2026-09-13，信息层级修订）

入口：低并发编译后，用带显示的 Godot 运行 `res://tests/MainFlowUiInputCapture.tscn`。使用正式内容与唯一测试存档，不使用 Computer Use，不读写真实玩家存档。`-- --offers-only` 只补查独立候选/部署界面，不能报告为正常战斗或完整征程通过；执行记录与截图见[活动任务当前段](../../work-items/active/battle-lab-ui.md)。

- 当前候选界面契约：1600 宽并排三张，按可用宽度换行；卡体、属性与详情按钮仅查看，每卡确认按钮才领取／购买。卡内长文单独滚动，确认按钮固定；额外候选由外层滚动访问。已用独立合法征程 fixture 实际操作上述路径，并补查 900×900 两列、滚到最后候选打开说明、恢复三列。
- 军团／备战共用紧凑详情：首屏肖像、生命／法力、四项关键属性和主被动；检查属性 hover 的真实提示与点击／Space 内容一致，次要属性能展开，技能解释可访问，前后征程数据不变。
- 招募所得后备经真实名册点击及空格点击加入已保存阵型；商店合法购买扣款与列表刷新；奖励预览滚动与继续。预览未伪造胜利或领取。
- 战斗侧栏保持点选、属性／技能解释、正文滚动与统计折叠的真实输入路径；本次信息修订复查用实验室战斗，不冒充自然征程。上一轮新征程、路线、真实胜败报告与胜利奖励页的记录在活动任务历史段，本轮不重新声称全部自然随机分支已验收。
- 已查看最终渲染：修复新卡片肖像只露头的取景问题，确认物品折叠正式效果可读；属性提示不再留下整屏高度空白，并以真实悬停检查其高度。最终低并发编译0警告／0错误，两个入口退出0。未执行多分辨率扫测、满背包与完整通塔；历史套件未批量运行，玩家体验仍由用户确认。

## 当前使用边界（2026-09-06）

下方是历次 Alpha 的混合历史检查记录，不是现行完整验收清单。旧“英雄统领士兵”“英雄自带手动命令/共享 MP”和具体商人技能配方已不能作为产品要求；旧通过记录也不代表新版本通过。

当前玩家规则读 `../../gameplay-design/README.md`；英雄独立法力/装备检查读 `formal-equipment-and-hero-mana.md`，通用模型/保存检查读 `content-system-foundation.md`，远程行为读 `ranged-attacks.md`，连续移动读 `continuous-space-combat.md`。其他仍适用的历史检查只按本次变更风险挑选，不默认执行全局/多分辨率扫测，也不为满足旧断言恢复过期行为。

## 历史检查项

## Complete Loop

1. Start from the main menu in Chinese and open settings once to verify volume and default battle speed persist and apply on the next launch/battle. The page must not expose an unsupported damage-number option.
2. Focus an unlocked hero, inspect its detail panel, and enter a seeded run through the single `以该英雄出征` action. Verify the detail identifies its army rule, named battlefield command, structured mana cost, and concrete effect. The merchant detail must additionally show a separate `5 金币` cost badge.
3. Choose tower nodes through all three regions. Exercise combat, elite, recruitment, shop, event, rest, and each boss.
4. Recruit soldiers and verify each card shows Chinese gameplay traits and role. In particular, `深渊爬兽` must show both `亡灵` and `野兽`, without exposing `soldier`/`undead`/`beast`. Move the hero and soldiers among legal cells in the first three columns, move soldiers between deployment and reserve, take items, and verify identities, exact cells, currency, health, and roster changes persist after returning to the menu and resuming.
5. Before each fight, verify enemy composition and the floor rule preview are visible. During combat verify pause, 1x/2x/4x speed, and that the limited hero-command button uses the selected hero's concrete command name. For `加急雇佣`, verify the HUD shows `5 金币/次` and remaining gold: at 5 gold a use changes MP 3/3→2/3, gold 5→0, and summons once; at 4 gold it reports `金币不足` without changing MP, gold, or summons.
6. Defeat the final boss and verify the victory result and meta unlock. Repeat a defeat path and verify defeat/restart.

## Architecture And Presentation

- Open a representative hero, soldier, enemy, and item scene independently; each must instantiate without the game root.
- Inject the one-shot structural test scene during the first catalog-validation instantiation, then inject one ready-pass test scene at a time that calls `GD.PushError` from `NotificationSceneInstantiated`, `_Ready`, its first `_Process`, and `_ExitTree`; every marker must appear exactly as authored in the validation report and prevent registry publication.
- Open each hero-command component scene directly and tune a non-default value. Its generated Chinese effect text and the next runtime execution must reflect that same value; zero/negative counts, scales, durations, divisors, tags, or paid-command costs must fail authoring validation.
- Verify constrained lanes, periodic hazard cells, an objective device, and a boss targeting rule visibly alter combat.
- Verify player formation armor never reduces damage received by an adjacent enemy formation; a lethal floor hazard emits a defeat attributed to `floor`; and the second-region boss summons at least one but no more than two temporary minions.
- At 1600×900 and 1280×720, verify route, roster, deployment, battle HUD, reward, and result text do not overlap.
- At 1280×720 with nine roster members, verify the deployment header, enemy/floor preview, status, and both bottom actions remain fixed and visible while the roster cards scroll independently.
- Verify player-facing cards and results use Chinese role, rarity, and battle-result labels; internal values such as `Ranged`, `Legendary`, or `PlayerDefeat` must not appear.
- Confirm no runtime error mentions `D:\godot\rpg`.

### Visual Language And Real Input

1. On deployment at both supported resolutions, inspect the untouched board, a roster selection, a legal destination, an illegal destination, a swap target, an active drag, a completed placement, and a rejected placement. Empty cells must contain no persistent `可部署` or row/column text; zone, hero, soldier role/reach, enemy, hazard, blocked, objective, focus, and action legality must remain distinguishable without relying only on color. Exact identity, coordinates, rules, and one concise invalid reason must remain reachable through the detail/tooltip layer.
2. Drive real viewport mouse press/release events through a reserve roster card and then a legal cell. Selection and placement/swap must complete without `Object is locked`, invalid `free`, disposed-object, duplicate activation, or focus loss; one successful command saves exactly once. Repeat selection, deselection, withdrawal, back, start, drag cancellation, and illegal drop; rejected/cancelled actions mutate and save nothing.
3. Traverse every candidate cell and fixed deployment action with keyboard/gamepad focus. Confirm selected, legal, illegal, occupied/swap, disabled, success, and failure feedback remains visible and equivalent to mouse interaction, and that disabled/empty presentation never removes a legal focus target.
4. Activate route, recruitment, reward, and shop decisions through real mouse input, including a purchase that refreshes the available list. The active card must survive its event dispatch, refreshed cards must remain clickable/focusable, and no dynamic list may log locked-object, invalid-free, disposed-object, duplicate-action, or stale-focus errors.
5. Traverse route, hero selection, recruitment, reward, shop, event, rest, Army overview, deployment, battle, report, result, and settings at `1280×720` and `1600×900`. Confirm first glance exposes objective/resources/primary action, hover/focus exposes comparison state, precise rules remain on demand, and the same fact is not repeated in page prose, summary text, card labels, and action copy. Record every remaining instructional paragraph that cannot yet be replaced safely.

### Window Baseline And Unit Portraits

1. Clear any test-only window override and launch the game normally. Confirm it opens in a resizable `1600×900` window and is neither fullscreen nor maximized; resize it to `1280×720` and back without losing required actions or creating overlapping UI.
2. Inspect all 45 production portrait resources grouped as 8 heroes, 24 soldiers, and 13 enemies. Confirm each resource can be tuned independently, uses existing local animation art, removes distracting transparent padding, centers the readable body, and does not alter the corresponding battle sprite, animation, or facing.
3. At both `1600×900` and `1280×720`, inspect representative small, tall, wide, mounted, hero, soldier, and enemy portraits in hero selection, recruitment, the deployment roster, Army details, and battle-report rows. Confirm every context uses the same authored crop for a unit and varies only its display size.
4. Confirm hero selection uses compact animated library tiles plus one detail panel, while recruitment uses its unit-specific row. Both keep portrait, name, precise responsibility, localized gameplay tags, and key attributes separate from long Chinese descriptions.
5. In recruitment, confirm exactly three choices appear in one vertical column. At `1600×900`, each card is about 148–150 pixels tall with a 104–108 pixel portrait region. At `1280×720`, only the choice list scrolls; the skip or hero-authored conversion action remains fixed, visible, focusable, and clickable.
6. Traverse hero and recruitment choices using mouse, keyboard, and gamepad. Hero focus must change preview only; the fixed detail action and each recruitment row must produce exactly one stable-id activation. Verify disabled heroes, tooltips, focus order, and scrolling. Confirm multi-tag units retain every localized gameplay tag while technical tags remain hidden.
7. Recheck tower-route and item reward/shop cards at both resolutions. Their general `ChoiceCard` size, icon hierarchy, focus behavior, and selection semantics must remain unchanged.

### Hero Library And Semantic Hierarchy

1. At `1600×900`, confirm several compact animated hero tiles fit in multiple columns beside one focused detail panel. At `1280×720`, confirm the library reduces columns without hiding the detail action or return action.
2. Move mouse, keyboard, and gamepad focus across at least three heroes, including one locked hero. Confirm focus changes preview only, locked state is readable without color alone, and only `以该英雄出征` starts a run. One activation must submit one stable id exactly once.
3. Confirm the focused detail groups portrait/identity, health, damage, reach, army rule, command, cost, and availability before paragraph reading. Health and damage each appear exactly once.
4. Inspect the same command in hero detail and battle HUD. Mana must be a blue icon-labelled structured badge and optional gold a separate gold badge; neither cost may be embedded in or recovered from neutral effect prose.
5. Across touched screens, confirm health is green, attack/damage red, mana blue, shield steel gray, healing teal-green, gold/hero identity gold, range light cyan, death/danger crimson, and risk amber. Every colored value retains an icon and/or Chinese label.
6. Open `StatBlock.tscn`, `TraitBadge.tscn`, `ResourceCostBadge.tscn`, and `HeroAbilityPanel.tscn` independently. Each must bind without a level, battle, autoload, or concrete hero id.
7. At both resolutions, recruitment must show exactly three compact vertical choices with readable portrait, identity badges, separated health/damage/reach values, neutral description, and one clear recruit action while bottom actions remain fixed.

## Playability Polish

### Combat Animation And Readability

1. At 1x, 2x, and 4x battle speed, watch at least one melee attack, ranged attack, skill cast, hit, and defeat. Each attack/hit/skill cue must remain visible for its authored one-shot instead of returning to idle in the same rendered frame.
2. Confirm damage and defeat cues play only on their targets, while movement and attack cues play only on their sources. A defeated unit must enter defeat once, briefly hold, fade/hide once, and never reappear during later synchronization.
3. Confirm every player hero carries a high-contrast gold `★ 英雄` identity cue that is readable without color alone. Confirm each unit displays `近` or `远`, with `原兽母皇` classified as near and `骸骨摄政` classified as ranged.
4. Select representative heroes, soldiers, and enemies during battle. The selected-unit panel must show current health, precise role, and exact attack reach, and must refresh when selection changes or the selected unit is defeated.

### Army Summary And Drawer

1. On route, recruitment/reward, shop, event, rest, and deployment screens, verify the compact summary shows hero health, deployed/reserve counts, item count, and gold without displacing the current decision UI.
2. Open the Army drawer on each screen. Verify hero army rule and generated command effect/cost, soldier health/deployment/role/reach, and item effects match the current run. Formation controls must not appear in the drawer.
3. Repeat at 1280x720 and 1600x900. Scroll the drawer to its final row, confirm no clipping, click-through, or pointer leakage, close it, and verify focus/input returns to the underlying screen.
4. Recheck after recruitment, purchase, rest, battle casualty, deployment change, and item gain; both summary and drawer must refresh without leaving the current decision.

### Hero Command Mana And Transactions

1. Enter battle with each hero and confirm the HUD shows 3/3 blue mana segments, the concrete command name, generated effect, one-mana cost, optional gold cost, and an empty/current failure reason as appropriate.
2. Use a legal command three times and verify mana changes 3/3 → 2/3 → 1/3 → 0/3. A fourth attempt must fail with an explicit reason and leave mana and gold unchanged. Pause and 1x/2x/4x speed changes must not alter mana.
3. Start the next battle and confirm mana is restored to 3/3 without being stored in the run save.
4. Exercise insufficient mana, insufficient gold, dead hero, and missing/blocked summon failures. Each failure must preserve both mana and gold and must not create a partial summon or effect.
5. For `加急雇佣`, test 5 gold with an available summon (one summon, 3/3 → 2/3, 5 → 0) and 4 gold or no available summon (no summon and no mana/gold change).

### Battlefield Deployment

1. Confirm the deployment preview shows the real `10×6` logical floor, current floor rule, unchanged enemy start cells, and all 18 candidate player cells in columns `0..2`. Every concrete enemy must show its own independently advancing idle portrait plus compact enemy/role/reach or boss redundancy; hazards, objectives, and blocked terrain remain semantic markers. Verify legal cells, blocked cells, non-player cells, occupied cells, hero, soldiers, enemies, hazards, and objectives remain distinguishable without color alone.
2. Move the hero to every legal row/column extreme through mouse select-then-cell, then swap it with a deployed soldier. The hero must never leave columns `0..2`, enter a blocked cell, overlap a piece, withdraw, or be replaced by a reserve soldier.
3. In an isolated four-soldier state with three deployed and one reserve, test both real drag/drop and real select-unit-then-select-cell for reserve-to-empty placement; confirm the selected identity appears in the application run, persisted save, rebound roster card, and destination cell. Then cover reserve-to-occupied replacement, deployed-to-empty movement, and deployed-to-occupied atomic swap. Withdraw a deployed soldier to reserve; with three reserves, withdrawal must be rejected.
4. During one drag, traverse at least three candidate cells. Exactly one current destination may show drag-hover, every previous cell must clear immediately, and cancellation, outside-board release, drop, rebind, back, and teardown must leave no hover trail. Legal-target, selected-source, swap, illegal, success, and failure states must remain visually distinct without color alone.
5. Reopen deployment after every successful operation and confirm exact identities and cells persist. Each success saves exactly once. Cancellation, a drop outside the board, a blocked/non-player target, reserve-on-hero, same-cell action, and focus activation duplication save zero times and leave the formation unchanged. For every rejected command, the visible concise reason must match the authoritative non-mutating evaluation; no destination presented as legal may be rejected by unchanged application state.
6. Force one save failure for hero move, hero/soldier swap, soldier move, soldier swap, reserve replacement, and withdrawal. Verify the exact prior hero cell, six soldier identities/cells, reserve membership, and rebound selection-visible state are restored, with one failed save attempt and no partial formation.
7. Load an isolated representative version-2 JSON run and verify automatic version-3 migration uses hero `(0,3)` plus soldier cells `(1,1)`, `(1,2)`, `(1,3)`, `(2,1)`, `(2,2)`, `(2,3)` without losing hero, roster, health, items, gold, route, seed, battle count, or progression. Save/reload arbitrary legal version-3 cells. Duplicate, out-of-zone, out-of-bounds, wrong-count, and stale-identity version-3 formations must fail safely without touching the real active run.
8. Enter battle after each successful rearrangement and confirm the hero and every soldier spawn on the exact previewed cell with no fallback relocation or duplicate living-cell occupancy. Enemy spawns remain unchanged. Defeat one soldier and confirm only that identity leaves deployment while every other cell and the hero cell remain stable; empty-slot bonuses still count six minus deployed soldiers.
9. Traverse all 18 candidate cells and fixed actions using keyboard/gamepad focus, then repeat representative mouse and drag/drop operations. Focus must not escape, skip a legal cell, or submit one activation twice.
10. At `1600×900` and `1280×720`, confirm every concrete enemy preview on the right faces left toward the player zone while its enemy/role/reach badges, tooltip, cell, clipping, and interaction geometry remain upright and unchanged. Player and neutral portrait consumers retain their authored definition direction. Start battle and confirm the enemies' independently owned initial battle facing is still left rather than being double-flipped by deployment presentation.

### Responsive Battlefield Projection

1. At `1600×900`, verify battle uses the previously empty board area with all ten columns and six rows visible and cell pitch at least approximately `108×82`. Units, health/readability markers, hero/near/ranged cues, pointer hit targets, movement endpoints, and character lift must grow and remain aligned rather than leaving original-sized units in larger empty cells.
2. At `1280×720`, verify the complete board, inspector, status, pause, speed, command HUD, deployment fixed actions, and all required text remain visible without overlap or clipping. The projection may contract toward approximately `88×68`; it must not transform-scale text or the full UI tree.
3. Select living units at player/enemy corners and along the inspector-facing edge. The selected-unit inspector must never cover a playable cell or unit, and pointer selection must use the visible presenter position with projection-relative radius.
4. Resize repeatedly between the two accepted resolutions and intermediate widths. Cell centers, board drawing, deployment targets, hit testing, initial snaps, queued movement endpoints, markers, unit scale, and selection radius must update together and reversibly without stale offsets, teleporting, diagonal shortcuts, distorted text, or focus loss.

### Movement Presentation Readability

1. At 1x, observe a unit receive several consecutive continuous positions. Confirm the unit root advances linearly through intermediate positions, never overshoots or reverses, and does not pause, ease, or restart at each 0.1-second simulation sample. An ordinary idle/health refresh during travel must not pull it to the newest authority position early.
2. Observe a `right → down → left` route around blocked terrain. Confirm the route retains both turn vertices in order and never substitutes a diagonal or curved simulation path. At low rendering cadence one frame may advance across more than one queued segment, but the presenter must consume the ordered polyline internally and must not build an ever-growing x4 queue. Trigger a 100-300ms hitch and confirm a newly created move still exposes an intermediate travel frame; after input stops, the visible position catches up within roughly 0.25 seconds at a supported rendering rate.
3. Watch the character art, health bar, gold hero marker, near/ranged marker, and pointer selection origin during sustained travel. Only the character art may rise by roughly three pixels. Its travel weight must remain continuous across authority samples and return to the authored offset only when travel settles; the unit root and every readability marker remain on the projected path without bobbing.
4. Follow sustained movement at 1x, 2x, and 4x, including 4x near 30 FPS. The move clip and motion state must start once, remain active across consecutive samples, and stop once after travel settles. At 4x, require bounded visual lag and non-flashing behavior rather than every authored animation frame; action one-shots may override the move clip while root travel continues.
5. Pause midway through a visible segment and hold for at least one second. Root position, character lift, and movement animation must remain frozen. Resume and confirm travel continues from the same point. Switch 1x → 2x → 4x → 1x during a segment and confirm each change retimes continuously without restart, reversal, snap, or lift discontinuity.
6. Trigger attack, skill, and hit actions while a unit is traveling. The spatial path and character-only lift must continue underneath the one-shot; when the action ends, the base animation must return to `move` if travel remains or `idle` if it has completed.
7. Observe an initial deployment and at least one summon. Both must appear directly at their authored cells with zero decorative lift rather than sliding or bobbing in from the origin. Defeat a unit midway through travel and confirm it stops at its current visible position, clears pending movement, restores the decorative lift to zero, plays defeat once, fades/hides once, and never slides or reappears. Rebind/battle replacement must likewise begin cleanly.
8. Repeat representative sustained-route, turn-route, burst, action-overlap, pause/resume, speed-switch, and defeat-interruption observations at 1280x720 and 1600x900. Treat sample-cadence linear travel at 1x, followability at 2x, and bounded non-flashing 4x fast-forward as the acceptance standard; static screenshots alone are insufficient.

### Engagement, Navigation, Facing, And Action State

1. At 1x and 2x, observe a crowded fight containing melee, ranged, healer, hero, enemy, and summon units. Both teams must begin participating immediately, advance coherently, and only queue briefly behind allies; no unit may pace between two cells, reverse repeatedly, or detour around a visually empty future engagement goal.
2. Select stationary living units during seeking, route obstruction, attack cooldown, healing cooldown, disable, and defeat. The existing selected-unit panel must show an understandable Chinese action reason, and cooldown values must be expressed as player-facing seconds. A dead unit must stop being selectable immediately even while its defeat animation remains visible.
3. Place a low-health unreachable ally and a higher-health reachable ally near a healer. The healer must choose the reachable legal heal, respect line access, retain that protected ally through cooldown, and join ordinary enemy engagement when no wounded ally has a legal plan. A non-healer and otherwise equivalent ranged healer with no wounded ally must both enter combat.
4. Observe units moving right, vertically, then left. Facing changes only when each horizontal segment actually begins; vertical motion retains the prior direction. Player units begin facing right and enemies left. Attacks and heals face their real targets even while visible movement lags behind simulation cells.
5. Confirm defeat locks the last facing and cancels travel. Rebinding the same content restores its team default. Character art may mirror, but health bars, hero identity, near/ranged markers, labels, and other readability UI never mirror.
6. Kill a unit after it has queued movement but before movement resolution. It must emit no move, remain defeated, free its cell for later use, and leave no follower waiting on its dead target or ghost engagement goal.
7. Let a low-health lifesteal unit kill an enemy whose death effect damages the attacker lethally. The attacker must remain at zero health and defeated, perform no later action, and leave its cell usable; ordinary healing or floor healing must not revive it.
8. In a layout where a unit has retained a legal target but its route is temporarily occupied by a stationary ally, observe several movement-ready decisions. The unit must wait on the same target for the bounded lease, then retarget to a reachable alternate at the lease boundary instead of switching immediately or waiting forever.
7. Put a stationary ally on a unit's shortest first step while leaving a longer empty side route. The mover must take the side route within the bounded wait lease rather than repeatedly selecting the blocked step; if that target remains sealed and another enemy is reachable, it must switch at the lease boundary. Also place an attack or heal target exactly two cells away behind one blocked middle cell: neither the range-2 attack nor the range-2 heal may pass through the wall.

### Battle Pace, End Sequence, And Report

1. Compare equal wall-clock intervals at displayed x1, x2, and x4. Verify x1 advances about eight 0.1-second simulation ticks per real second, with x2 and x4 remaining exact 2× and 4× display-mode multiples. Pause and mid-motion speed changes must remain continuous, and the same seeded setup must keep the same terminal tick, outcome, digest, movement-event order, and report facts at every display speed.
2. Observe a terminal attack at normal speed. No later simulation step may occur; commands, pause/speed controls, selection, and board input must lock immediately. Keep the final battlefield visible for about 1.1 real-time seconds, then reach fully opaque black over about 0.45 seconds before the report appears. Repeat with confirm during hold and fade: it may accelerate arrival at the same report, never skip it, duplicate resolution, or route twice.
3. Exercise ordinary victory, final-boss victory, player defeat, and timeout. Each must first show the dedicated report. Continue must route once to combat reward, run-success result, or run-failure result as appropriate; closing or abandoning the screen after terminal resolution must not lose or duplicate the authoritative save mutation.
4. Verify the fixed report banner shows outcome, encounter, deterministic duration, and player command-gold cost. The two fixed team summaries must remain the single readable owner of survivors, casualties/kills, effective damage, effective healing, and remaining health; command gold must never appear as an enemy-comparable value.
5. Switch independently among `战局总览`、`输出`、`生存`、`治疗` and `我方`/`敌方`. Identity, portrait, name, hero/role/summon label, and alive/defeated state must remain recognizable while the leaderboard reorders deterministically. Output must show damage/share/DPS/kills; survival taken/shield/final health/lifetime; healing healing/share/HPS/effective events. A side with zero healing must show a deliberate Chinese empty state and overview must still expose every unit.
6. Use tied positive fixtures and all-zero categories. In each overview matchup, every tied positive leader must remain visible in deterministic order with its authoritative value and own-team contribution share; zero categories must invent no leader. Both-zero healing must collapse to `双方均无有效治疗`, while one-sided zero healing must label only that side `无有效治疗`. No composite MVP/score may appear. Verify positive environment damage is labelled as a team-level fact that belongs to no unit row or detail, and reconciles target-side taken minus opposing credited damage without false unit attribution.
7. Build focused shield, overkill, splash, pierce, death-effect, hazard, heal, overheal, lifesteal, floor-heal, late-summon, and hero-command cases. Effective damage must equal actual health-plus-shield removed, effective healing actual health restored, kills only concrete lethal transitions, and summons use independent leaderboard rows. Initial join tick must be zero; a late summon must use its actual join tick for DPS/HPS; defeat tick writes once; one splash/pierce action counts once; zero/overheal creates no healing event; only successful commands increment command uses. Re-running prior deterministic fixtures must prove these counters do not alter health, outcome, digest, saves, or routing.
8. At 1280×720 and 1600×900, inspect player/enemy overview, output, survival, healing/zero state, ordinary victory, defeat/timeout, and a temporary summon when available. The fixed header, dimension/allegiance controls, leaderboard header, selected-row indication, detail access, and continue must not clip or overlap; only the authored report-content owner scrolls. Continue must still route exactly once.
9. On the default overview, require a three-second read: identify outcome and directly compare the player-versus-enemy output, damage-taken, and healing core units without opening unit detail. Each positive side must expose identity, authoritative absolute value, and own-team contribution percentage; tied positive leaders remain explainable, zero healing is compact and explicit, both roster strips remain recognizable, and positive environment damage is labelled without unit attribution. The old team-total bars, duplicated survivor/casualty/kill sentence, and separate selected-side leader cards must be absent. No composite MVP, rating, fake timeline, skill split, or damage taxonomy may appear.
10. Switch among `输出`、`生存`、`治疗` for both allegiances. Each page must replace the complete fixed column set: output shows rank/unit/effective damage/share/DPS/kills/attack actions; survival shows rank/unit/effective damage taken/shield/final health/lifetime/remaining ratio; healing shows rank/unit/effective healing/share/HPS/effective events. For a representative six-unit side, every row uses identical column widths and numeric alignment, deterministic order, and a contribution bar normalized to the same selected-side maximum; zero healing uses the deliberate Chinese empty state.
11. Click and focus several leaderboard rows with mouse, keyboard, and gamepad. Exactly one compact row becomes selected and exactly one authored detail panel exposes that unit's complete existing facts and awards. Switch dimension/allegiance while a row is focused: retain selection only when the same runtime id remains valid, otherwise select the first available row, never target a freed control, and keep rows, controls, detail, and continue reachable.
12. Bind the longest current Chinese name/responsibility plus large values, tied core leaders, simultaneous positive awards, summons, and mixed alive/defeated states; then resize repeatedly between 1280×720 and 1600×900 and intermediate widths. Overview matchup identity/value/share columns and detailed ranking columns must stay contained; fixed columns must not drift between header and rows, names/identity must truncate or wrap into their authored regions without crossing numeric columns, common-scale ranking bars must remain comparable, detail content may grow only into the report scroll owner, and repeated resize must preserve dimension, allegiance, exact row count, and a valid selection/focus without transform-scaling text or the report tree.

### Semantic Theme, Icons, And Choice Cards

1. Inspect hero/gold/legendary headings, mana/shield/player facts, healing/survival gains, enemy/casualty/defeat facts, and warning/timeout states across screens. Confirm their gold, blue, green, red, amber, and neutral treatments are consistent and centralized; every meaning also retains a Chinese word, icon, or symbol, and paragraphs remain neutral rather than fully colorized.
2. Verify health, damage, shield, healing, mana, gold, time, kills/deaths, hero, melee, ranged, risk, and loot icons remain sharp and tint correctly at 1280×720 and 1600×900. Missing optional unit/item artwork must fall back to a stable portrait or semantic role/loot icon without an empty or broken texture.
3. On hero selection, tower route, recruitment/reward, and shop screens, confirm every choice card exposes an independent icon/portrait, title, body, and footer/meta region. Long descriptions must not overwrite metadata. On hero selection specifically, move the mouse across several tiles and move keyboard/gamepad focus without activation: selected styling, detail content, selected stable id, and run state must remain unchanged. Mouse click or `ui_accept` must select exactly one hero and update detail once; only the fixed detail action may start the explicitly selected unlocked hero once.
4. Compare responsibility chips for 前卫、战士、远程、辅助、刺客、召唤、炮手、首领 and faction/trait chips for 秩序、沙海、亡灵、野兽、机械、霜寒、中立、敌军. Each meaning must retain Chinese text, use its differentiated catalog icon, and remain identical across hero selection, recruitment, deployment, Army details, reports, and selected-unit facts where it appears. `深渊爬兽` must show both 亡灵 and 野兽 with their matching faction icons.
5. Inspect combat, elite, recruitment, shop, event, rest, and boss tower choices at 1600×900 and 1280×720. All seven primary node icons must be visually distinguishable. Risk must remain a separate icon-plus-`风险` fact; a zero-risk route must keep its node identity rather than using the warning triangle as its main image.
6. Confirm unit cards show responsibility/faction or trait plus health, damage, and exact reach as icon-plus-Chinese-text facts. Hero health and damage must appear once, while army rule, battlefield command/cost, unlocked or locked state, tooltip, and typed activation remain readable and functional.
7. Watch representative multi-frame hero, soldier, and enemy portraits in all five portrait consumers. Each UI portrait must advance through its existing idle animation at a calm pace, pause when its card or ancestor is hidden, and resume when shown. One-frame portraits remain naturally static, and opening/hiding UI must not change any battlefield sprite, cue, frame, facing, or playback state.
8. Recheck dense fact groups, route cards, deployment roster, Army drawer, selected-unit panel, and battle-report rows at both accepted resolutions. No chip may clip, overlap prose, consume an action button, remove Chinese text, or break mouse, keyboard, or gamepad focus traversal.

### RealmTheme Full-Flow Presentation

1. Traverse main menu, hero selection, tower route, recruitment, deployment, battle HUD, battle report, reward, shop, event, rest, result, and settings in one run. Confirm every screen inherits the same project-local RealmTheme: dark flat surfaces, thin restrained borders, modest rounded corners, and gold emphasis. No pixel texture, ornamental rail, oversized corner hardware, missing style, or external donor dependency may appear.
2. On the main menu and every modal, confirm hierarchy uses one clear primary gold action, ordinary secondary actions, an explicit danger treatment where applicable, and compact controls for dense repeated actions. Normal, hover, keyboard/gamepad focus, pressed, selected, locked, cooldown, and disabled states must remain distinct without changing exact-once activation or typed command behavior.
3. At `1600×900` and `1280×720`, inspect every screen plus the Army drawer, hero detail/library tiles, recruitment cards, deployment roster/cells, selected-unit panel, command HUD, and all report dimensions/allegiances. No label, semantic icon, animated portrait, value, progress bar, tooltip, focus indicator, or fixed action may clip or overlap; only the already authored `ScrollContainer` owners may scroll.
4. Recheck health green, attack/damage red, mana blue, shield steel gray, healing teal, gold/hero gold, range cyan, danger crimson, risk amber, player/enemy distinction, secondary text, and neutral prose. Every meaning retains its icon and/or Chinese label, and the Theme—not per-screen path maps or paragraph parsing—owns its stable color role.
5. Open `StatBlock.tscn`, `TraitBadge.tscn`, `ResourceCostBadge.tscn`, `HeroAbilityPanel.tscn`, `HeroLibraryTile.tscn`, `DeploymentCell.tscn`, and representative report leaderboard-row, summary, and detail scenes independently beneath the local Theme. Confirm each retains its typed binding API and readable normal/selected/disabled state without requiring a level, autoload, concrete content id, runtime-built Theme, or runtime-built control tree.
6. Exercise settings sliders and every long scroll owner. Slider tracks/grabbers, scrollbar tracks/grabbers, report health/contribution bars, and keyboard/gamepad focus must remain visible in RealmTheme's flat language. Values, focus, and scroll position must behave as before.
7. Inspect the root composition and the local Theme resource. The background must be an authored flat dark `ColorRect`; the live Theme path must be `res://content/ui/RealmTheme.tres`; all styles must be `StyleBoxFlat`; and no live scene, source, test, system/QA authority, or Theme may reference a removed pixel path, old visual-role vocabulary, or an external absolute donor path.
8. Run the RealmTheme focused contract before the hierarchy, semantic, portrait/window, responsive/dynamic report, fixture, content, gameplay, movement, UI, alpha-run, and clean-startup checks. Generate and manually inspect the complete paired VisualCapture set at both supported resolutions, including fixed actions and selected/focused/disabled states.
