# Third-party UI icon attribution

Icons made by **Lorc**, from [Game-icons.net](https://game-icons.net/), licensed under [Creative Commons Attribution 3.0 Unported](https://creativecommons.org/licenses/by/3.0/).

| Production asset | Original name and source | Selected use |
|---|---|---|
| `keyword-thorns.svg` | [Shield reflect](https://game-icons.net/1x1/lorc/shield-reflect.html) | R1 — 反伤 |
| `keyword-true-damage.svg` | [Pierced heart](https://game-icons.net/1x1/lorc/pierced-heart.html) | T2 — 真实伤害 |
| `keyword-physical-damage.svg` | [Sword clash](https://game-icons.net/1x1/lorc/sword-clash.html) | P2 — 物理伤害 |

Adaptations: removed the solid black background and set the SVG intrinsic dimensions to 24×24 while preserving the original 512×512 viewBox and foreground paths. Game UI applies semantic tint. The gameplay meanings are this project's mappings, not additional claims by the original author.

Original SVGs and upstream license text are preserved under `candidates/game-icons/`. Retain this attribution and license link when distributing these icons or derivative builds.

## Project-drawn icons with selected game references

On 2026-09-21 the user selected `111121` from the mature-game comparison, meaning AS1 / MG1 / AT1 / FU1 / LS2 / DF1. The following monochrome 24×24 SVGs use newly authored paths, taking the selected shape ideas as references; they do not embed or trace the games' raster files.

| Project asset | Selected shape reference |
|---|---|
| `keyword-attack-speed.svg` | AS1 — LoL attack-speed icon: diagonal sword and speed lines |
| `keyword-magic-damage.svg` | MG1 — LoL ability-power icon: energy comet; shared magic presentation, not a new formula |
| `keyword-attack.svg` | AT1 — LoL attack-damage icon: broad axe blade |
| `keyword-follow-up.svg` | FU1 — Master Yi Double Strike: successive curved slashes; project follow-up timing remains unchanged |
| `keyword-lifesteal.svg` | LS2 — TFT omnivamp icon: paired fangs; project life-steal eligibility remains unchanged |
| `keyword-armor.svg` | DF1 — LoL armor icon: inset shield, distinct from the existing peaked, half-filled shield resource icon |

Reference evidence: [LoL UI elements and atlas coordinates](https://raw.communitydragon.org/latest/game/clientstates/gameplay/ux/lol/playerstats/uibase.cdtb.bin.json), [LoL atlas](https://raw.communitydragon.org/latest/game/assets/ux/lol/statspanel_atlas.png), [Master Yi passive](https://raw.communitydragon.org/latest/plugins/rcp-be-lol-game-data/global/default/v1/champions/11.json), [TFT UI elements](https://raw.communitydragon.org/latest/game/clientstates/gameplay/ux/tft/tftunitinfo/uibase.cdtb.bin.json), [TFT text-icon atlas](https://raw.communitydragon.org/latest/game/assets/ux/fonts/texticons.png). Research originals and the comparison remain in `.godot/ui-review/mature-game-icons/`; they are not production resources.
