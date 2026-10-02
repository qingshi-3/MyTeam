# 战利品挂件 · 去冗余修订

## 最新反馈：保留名称栏，恢复原图标体系

用户认可本稿的实物风格和底部名称栏，但否定新造的词条图标语言，明确要求沿用项目现有图标，仅调整材质。名称栏左侧攻击 22 同样必须恢复当前图标，不能继续使用箭头。下面旧生成提示保留为错误原因与历史，不再作为执行指令。

- [原路径材质局部稿](native-icon-material-proof.png)：19 项属性＋主动／被动类别共 21 项。原图与材质示意并列，下方保留紧凑显示；说明文字仅属于审稿板，不是游戏内词条标签。
- [名称栏局部修订](name-ribbon-native-attack.png)：复用原稿的名称栏像素，仅在攻击图标区修补被否定的箭头并贴入现用战斧；名称、数值、右侧生命区域与条形保持。
- [渲染源](render-native-icons.cjs)、[资源映射与源校验和](native-icon-proof-sources.json)：按语义目录和真实绑定读取 SVG；校验路径、基本几何、描边宽度、填充规则一致，不另画形状。仅调整画笔／渐变和轻微浮雕投影，不使用生成模型重解释图标。
- 已查看输出并完成源几何对照。材质只是静态局部示意，尚未获用户认可，也未接入正式资源或做小尺寸实机／交互验收。未运行 Godot 或构建；不存在需要据此扩大回归的运行改动。

## 上轮生成记录

用户澄清倾向的是实体盾牌、药瓶、皮革装备扣的 D 版（生成展示顺序里的第二张），不是 E 星仪。认可范围仅为方向倾向；图标细节均需优化，攻击／生命数值上附加交叉剑被明确否定。本轮内置 `image_gen` 以 D 原稿为编辑目标，两张用户裁图作为反例参考；仅修静态稿，不替换游戏资源。

修订意图：保留实物质感，去除不承载含义的交叉武器、套框、宝石与细碎装饰；攻击、生命、防御不要共用盾牌容器；各回蓝项以明显的结构记号区分。所有具体图形仍是候选，未获用户逐项认可。

## 输出与反馈依据

- [去冗余修订稿](d-relic-refined.png)：已生成并由 Agent 查看。攻击和生命两端取消交叉武器与盾形徽章，箭头／心形和数字并入姓名条；回蓝系列加入较大的结构记号，技能去除红宝石等装饰。
- 用户反例：[攻击裁图](feedback-attack.png)、[生命裁图](feedback-health.png)。用户反对多余交叉剑，是整套细节问题的举例，不表示只需删除这两处便可完成。
- 尚待评价：生成稿仍有局部亮面和小铆钉；攻击与暴击箭头的语义区分、小尺寸回蓝辨识仍需在后续精修中检查。这是绘图候选，未接入、未做实际小尺寸运行或交互验收，不能称全部图标完成。

## 完整提示词

```text
Use case: precise-object-edit / ui-mockup refinement.
Input image 1 is the SELECTED D "战利品挂件" game-card design to edit. Input images 2 and 3 are close-ups of the USER-REJECTED excess decoration: crossed swords attached to the bottom attack and health badges. They are negative examples, NOT decoration to reproduce.
Primary request: refine the selected tactile mini-object UI system through deliberate subtraction and clearer meanings. Preserve its core physical-object art direction and accepted card architecture. This is ONE focused revision, not another alternative style. The user specifically objects that putting crossed swords on both attack and life is pointless and semantically wrong. Remove those crossed weapons completely. Do not merely paint them a different color. Audit all the small glyphs for the same mistake.
LOCK: same red-haired archer, pose, costume, broad gray-stone card border, full-height portrait reaching the bottom name ribbon; top remains two skills then exactly three empty leather equipment loops, side attributes remain eight pairs on each side INSIDE the frame, no per-attribute backing panels. Keep the bottom name "连弩手", attack22 left, life155 right, spell-power0 above; "招募 · 3 金币" remains OUTSIDE below the frame. Do not redesign the character, narrow the card, add a bottom equipment zone, or add a large reading side panel.
Most important bottom correction: do NOT mount either number in a shield-shaped armored medal decorated with weapons. For the left attack value, ONE simple small matte ivory/iron blade or spearhead silhouette adjacent to a clear 22; no crossed swords, no second weapon, no surrounding shield, no crest, no wreath. For the right life value, ONE simple sculpted matte deep-red HEART silhouette adjacent to 155, no shield-shaped backing, no weapon anywhere near life. These two icon-number pairs sit directly at the ends of the SAME parchment name ribbon, visually integrated with it; do not attach a new separate panel or giant hanging medallion. Numerals large and legible, label centered. The small single blade is the attack pictogram itself, not a decorative sword stuck behind another badge. Health cannot be confused with defense.
Refine the SIDE ICONS without flattening them into generic SVGs: individually painted mini objects, using broad readable silhouettes, 2-3 shaded planes, short controlled edge highlights and soft contact shadows. Remove extraneous studs, double metal edging, jewel settings, etched filigree, micro scratches and dangling bits. Texture comes from form and material, not accessories. One icon, one semantic purpose, consistent optical size and lighting. No separate plates behind attribute pairs.
Keep every numeric value and row: left defense 1 (simple wooden buckler, only here is a shield appropriate), attack speed ×1 (two brisk fletched arrow strokes), range 5.2 (compact sight), crit chance 0% (clean sharpened arrowhead), crit damage150% (impact/split-tip version of same arrowhead), lifesteal0% (small red vessel with a clear inward feed notch, not the same full heart as maximum life), dodge0% (boot with one flowing motion cut, different from standing movement boot), control resistance0% (two distinctly broken chain links). Right movement×1 (standing leather boot), max mana60, starting mana20, passive mana5, on-attack mana10, on-damage mana50, per-hit cap20, healing0. Mana family retains tactile blue/teal ceramic flask motif from selected D, but has one LARGE integrated and readable structural mark for each meaning: plain flask; initial/start slit; small clock-face cutout; arrow-shaped cutout; shield-shaped cutout; broad cap/bar across the vessel. Not six nearly identical bottles differentiated only by tiny corks or jewelry. Healing a simple warm-bone cross-shaped object, no second bottle or frame.
Top skills: keep arrow-volley and precision-sight concepts, remove decorative star points and red gemstone. A compact fan of three broad arrowheads held by ONE simple dark-bronze band for the first, a clean circular sight with four structural pins for the second. No random weapons or ornament around them. Three equipment receivers remain EMPTY U-shaped leather loops with plain iron end fasteners, broad quiet leather planes, plus sign centered; do not add ornamental rivets or new slots. Top proportions must fit above the face.
Output a polished landscape design sheet 1536x1024. Left: complete updated card at large readable scale. Right: pale parchment sample board, marked "细节放大", NOT an actual attached UI. Right upper: a LARGE exact enlargement of the updated complete name ribbon and its two icon-number pairs, so the removal of both crossed swords and shield-shaped badges is obvious. Right middle: six to eight representative improved symbols with their actual numbers, including different mana subtypes, crit chance vs crit damage, and shield vs heart. Right lower: the two simplified skills and three empty leather sockets as exact matching enlarged samples. No decorative room still life, extra props, fake slogans or English filler. Title "战利品挂件 · 去冗余修订". Tiny explanatory headings are okay only on the right sample board; full stat names never appear beside values on the card.
Priorities: meaningful silhouette > ornament, readable numbers > framing, cohesive tangible materials > shiny trinkets. No crossed swords ANYWHERE, no health shield, no decorative weapons behind numbers, no oily gloss, no neon, no glow, no microscopic detail, no new gameplay information. Retain the physical-object character of selected D, not a bland flat-symbol redesign.
```
