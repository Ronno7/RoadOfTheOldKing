# Showcase design brief

A guided visual tour of Road of the Old King for peer developers, instructors and portfolio readers. It sits apart from the technical guides: the README and `Docs/` explain how the game works, while the showcase shows what it feels like and why it is built the way it is.

**Status:** a working draft. The layout, interactions and copy are in place; the copy was reconciled with the game on 23 Nov (`0.5.1-dev.1` plus the Unreleased changes). Gameplay captures (23 Nov): all twelve plates are filled from the engine. The axe, enemy and grass clips are lossless animated WebP at 3× the 320 × 180 native frame; the enemy fights are played by rule (the hero walks to the nearest enemy, swings at it and sidesteps each attack as its lane locks), with every other enemy out of the take. Restrained and Cinematic are two stills of one moment behind a draggable split. The four Interface plates are full-screen MP4 loops with the HUD, each with a PNG poster. Open `index.html` through any static server (for example, `python3 -m http.server` from this folder).

## Decisions so far

| Question | Decision |
| --- | --- |
| Structure | **The Road plus a map.** One continuous scroll that walks the journey region by region. A rail with the walking hero and an overlay map (key `M`) allow quick travel. |
| Audience | **Balanced.** The feel of the game leads, and one chapter (Architecture) gives architecture depth for developers and instructors. |
| Mood | **Day to storm.** Warm Ardan's Edge, then the cool Lowlands, the firelit Barrow and finally the Storm Highlands. The whole page cross-fades as you walk. |
| Hosting | **GitHub Pages**, https://ronno7.github.io/RoadOfTheOldKing/ (owner, 23 Nov), linked from the README; not on roadoftheoldking.com (see [Publishing](#publishing)). |
| Technology | **Vanilla HTML, CSS and JavaScript.** No build step and no external dependencies. |
| Audio | **None.** |
| Unbuilt regions | **Shown as "the road ahead":** concept art with honest status labels. |

## Principles

- **It lives in the world.** Every colour comes from the game's palettes (`Docs/Art/Palettes`). Every frame, gem and crest is the game's own UI kit. All type uses the game's fonts: `RotOK Title` for headings, `RotOK Heading` for subheads and emphasis, and `RotOK Pixel` for body text, always at 16 px multiples so the glyphs stay crisp.
- **Pixel scaling.** Art uses whole device-pixel multiples where it fits (`data-fit`), with `image-rendering: pixelated`. On narrow 1× screens, oversized art fits the container so the composition remains visible; this is the responsive exception.
- **Plain, factual copy.** Say what the game does in a short sentence. No taglines, no lyrical lines, and no invented lore. Anything deeper links to `Docs/`.
- **Interaction serves the journey.** Interactions help readers move through and explore the page: the rail, the map, keyboard travel, next-chapter links, and tabs that switch gameplay previews. There are no mini-games or toys.
- **Restraint in motion.** Weather is a handful of 1-pixel particles. Animation loops run only while their element is on screen. `prefers-reduced-motion` selects exact still banner frames and stops motion, weather and cloud shadows. Changes to the preference take effect without reloading.

## The tour

| # | Chapter | Region | Content | Interaction |
| --- | --- | --- | --- | --- |
| · | Prologue | Ardan's Edge | Title, the Ardan's Edge diorama and a one-line description | Download, or start the tour |
| I | The Axe | Ardan's Edge | Throw, Reposition and Recall; the light combo, charged cleave, action clock, stamina, the Recall stone and gate, and the sluice | Tabs: each action switches the gameplay capture below |
| II | Art | Ardan's Edge | 16 PPU, shared palettes, PixelLab characters, the separate axe sprite, cloak colour, RuleTile terrain, layers vs collision, drawn height, the import pipeline; the hero's eight directions and both region palettes as swatches | Hover a swatch for its name and hex value |
| III | Vegetation | Green Lowlands | Wind, movement, cutting, depth, rendering and placement of the grass system | A decorative grass strip that sways in the wind and gusts in the scroll direction |
| IV | Enemies | Green Lowlands | Wolf, Mossback Boar and Road Scavenger cards; rats, the Broken Watcher, the attack contract, ground warnings, groups and leash as text; Dark Souls influence | Tabs: each card switches the capture; the selected card shows the "!" mark |
| V | Bonfires | Barrow Woods | The Barrow diorama, an animated bonfire, lighting, resting, travel and upgrade fires, plus Dark Souls influence | Warm light follows the cursor |
| VI | Atmosphere and Graphics | Barrow Woods | 2D lighting, colour grade, region looks, the two atmosphere modes, baked shadows, water, dark interiors, hit and reward feedback | Tabs: Restrained and Cinematic switch the comparison capture |
| VII | Interface | Barrow Woods | Non-diegetic, diegetic, spatial and meta UI on a 2 × 2 grid (in the world space × in the fiction), plus the contextual HUD, the UI kit, anchored chips, device prompts, menus and settings; Dark Souls influence | Tabs: each quadrant shows its examples and capture |
| VIII | Architecture | Barrow Woods | "Gameplay owns state. Presentation only samples it." Twelve components named after their classes, and six tenets | Select a component to see what it owns, its pattern and its wiring |
| IX | Roadmap | Storm Highlands | Highlands diorama, route status, what's next, and a build log of every changelog version plus the Unreleased and planned 0.6 entries | Ends on Download |

Ambient systems that run across the page:
- The background, ink and card colours cross-fade per region through registered CSS custom properties (`@property`).
- A place-name card announces each new region, as the game does.
- Each region has its own weather: gold motes, seeds on the wind, embers, rain and lightning.
- Cloud shadows drift across the Lowlands.
- The hero walks the rail as you scroll, facing the direction of travel.
- Each chapter ends with a link to the next one.

Keyboard: `M` opens the map, `J` and `K` (or `]` and `[`) move to the next or previous chapter, and `Esc` closes the map. Tabs follow the standard pattern: arrow keys, `Home` and `End`.

## PixelLab art list

All new art should match the README banners. That means floating dioramas with transparent, irregular edges, native 16 px per unit, the region's palette, and the blue-cloaked hero where a figure is needed. The banners are 384 px wide; keep that width so they share a scale with the existing ones.

| ID | Asset | Size | Where | Direction |
| --- | --- | --- | --- | --- |
| D1 | **The Throw** diorama | 384 × 128, 12–24 frames | I · The Axe, above the demo | The hero mid-throw on the old road, the axe spinning toward a wolf; Ardan's Edge cottages behind |
| D2 | **Recall** diorama | 384 × 128, 12 frames | I · The Axe, or a plate | The axe lodged in the sluice carriage, pulled home in translucent cyan with a trail |
| D3 | **Lowlands** diorama | 384 × 144, 12 frames | III · Vegetation header | Meadow, watermill and drying shed under a passing cloud shadow, with seeds on the wind |
| D4 | **Wolf pack at the treeline** | 384 × 128, 8–12 frames | IV · Enemies header | Three wolves circling at dusk on the Lowlands edge, one lane drawn on the ground |
| D5 | **The Cartographer's table** | 384 × 128 | VIII · Architecture header | A lantern-lit table with a map, a compass, the axe and notes: the craft behind the world |
| B1–B4 | **Region backdrop bands** | 480 × 120, 3 layers each (far / mid / near) | Behind each region, for subtle parallax | Ardan's Edge: hills and smoke. Lowlands: rolling fields. Barrow: tree line. Highlands: ridge and storm clouds. Edges fade to transparent |
| S1 | **Chapter sigils** | 24 × 24 × 6 | Chapter kickers and map pins | Axe, leaf, wolf head, flame, compass, crown in storm |
| M1 | **Painted map** | 400 × 160 | Map overlay, replacing the flat colour bands | The four regions left to right, with the road winding through |
| F1 | **Favicon / crown** | 32 × 32 | Browser tab | The title crown, gold on transparent |

Slots are marked in the markup with `data-pixellab="…"` (existing dioramas) and `data-slot="…"` (capture plates).

## Gameplay capture list (Unity session)

Plates are 16:9. An empty plate shows a dashed placeholder only while `<html>` has `class="is-draft"`. Pixel-art clips without the HUD: lossless WebP at an integer scale of the native frame. Clips with the HUD: silent MP4 at 960 × 540 with a PNG poster.

| Slot | Shot |
| --- | --- |
| `axe-throw` | Throwing the axe and lodging it behind a wolf |
| `axe-reposition` | Moving around a lodged axe during a fight |
| `axe-recall` | Recalling the axe through a wolf pack (the hit pause should be visible) |
| `wolf-gameplay` | Two wolves in open grass: lanes locking, the hero sidestepping and striking back |
| `boar-gameplay` | The barn-yard Mossback Boar: charges sidestepped, punished, and the boar circling back |
| `scavenger-gameplay` | Two Road Scavengers in the workshop yard |
| `atmosphere-restrained` | Still: the mill bonfire in the default Restrained mode (PNG, the comparison's left side) |
| `atmosphere-cinematic` | Still: the same frame in Cinematic mode (the comparison's right side) |
| `ui-non-diegetic` | HUD vitals in a scavenger fight, then the inventory panel |
| `ui-diegetic` | Lighting the North Bank bonfire: Light Bonfire prompt, the flame, BONFIRE LIT, then Rest |
| `ui-spatial` | Stamina arc, enemy health bars, awareness marks and attack warnings in a wolf fight |
| `ui-meta` | Taking hits at low health: the red heartbeat edges, hit-stop and camera kick |
| `lowlands-gameplay` | The hero walking through Lowlands grass under cloud shadow, then cutting a path with a charged cleave |
| *(new)* `bonfire-gameplay` | Resting at a Lowlands upgrade fire: warm light on the grass, then its upgrade menu |
| *(new)* `sluice-gameplay` | The sluice puzzle at the Lowlands sluice house: the carriage moving the lodged axe, then one Recall draining the pond |

To fill a plate, replace its `.plate__empty` contents with `<video autoplay muted loop playsinline poster="…">` or an `<img>`, and add `class="is-filled"` to the `<figure>`.

## Pending content

The copy and the Roadmap were reconciled with the game and the local development notes on 23 Nov: the Tutorial and Green Lowlands are playable (Windows download); a demo polish pass is next, then Barrow Woods in 0.6; the Highlands and the final area remain planned. Gameplay captures, new dioramas and the open presentation choices below remain pending. See [HANDOFF](HANDOFF.md#content--23-nov).

## Open questions

1. **Foes roster.** Thorn and Field Rats and the Broken Watcher are in the game and described as text under the three enemy cards. Should they get cards of their own? That needs their sprites copied into `assets/art/sprites/` and two new capture slots. The Briar Wolf is deferred past 0.5.
2. **Plates or inline video.** Should the captures stay as framed plates under each chapter, or replace the dioramas as each chapter's hero image?
3. **README link.** Add a "Showcase" link to the README header row once it is published?

## Publishing

Published on GitHub Pages at https://ronno7.github.io/RoadOfTheOldKing/ (owner, 23 Nov). `.github/workflows/showcase.yml` deploys this folder whenever it changes on `main` or `dev` (or by hand from the Actions tab); Pages is set to build from workflows, and the `github-pages` environment allows both branches. The README links it. It is not on roadoftheoldking.com.

## Files

```
Showcase/
  index.html          markup and copy
  css/showcase.css    tokens, regions, layout, UI kit framing
  js/showcase.js      rail, map, regions, weather, tabs, next links, grass strip, architecture explorer
  assets/fonts/       the game's TTFs, copied from Assets/UI/Fonts
  assets/art/         banners, title, UI kit, hero/enemy/bonfire sprites (copied from runtime sprites)
```
