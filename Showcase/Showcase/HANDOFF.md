# Showcase handoff (for agents)

Read this before changing anything in `Showcase/`. `DESIGN.md` holds the design decisions, the PixelLab art list and the capture list. This file covers how the page works and the owner's rules.

## What this is

A standalone, art-led tour of Road of the Old King for peer developers, instructors and portfolio readers. It is separate from the technical guides in `README.md` and `Docs/`. It is static HTML, CSS and JavaScript with no build step and no external requests.

- From the repository root, run `python -m http.server 8000 --directory Showcase/Showcase`, then open `http://localhost:8000/`. This inner folder contains the site; the outer folder contains pointers to these notes.
- It is a **draft**. All twelve capture plates are filled (23 Nov); adding `class="is-draft"` to `<html>` would show dashed placeholders for any new, empty plate.
- It **stays in the GitHub repository only, for now** (owner, 23 Nov): it is not hosted on roadoftheoldking.com and not published. See "Publishing" in `DESIGN.md`.

## Owner's rules (from review feedback; follow them)

1. **Only the game's fonts.** `RotOK Title` (headings), `RotOK Heading` (subheads, labels, emphasis) and `RotOK Pixel` (body), always at 16 px multiples. No web fonts and no serif. Italics don't exist in these fonts.
2. **Plain, factual copy. No poetry.** No taglines, lyrical lines, invented lore or "flavour" subtitles. Each line states what the game does. If a line wouldn't fit in a technical summary, cut it.
3. **Interaction serves navigation and reading, never play.** No mini-games or toys. Allowed: the rail, the map, keyboard travel, next-chapter links, tabs that switch previews, the architecture explorer, and ambient motion (weather, the wind-only grass strip, bonfire light).
4. **Lead with core technical design.** Prefer facts like 16 PPU, URP 2D lighting, mesh-chunked GPU grass and single-writer components over trivia such as plant counts.
5. **Every implementation claim comes from the repo** (`Docs/SystemsOverview.md`, `Docs/World.md`, `Docs/PlayerAnimation.md`, `CHANGELOG.md`, `README.md`) and the code. Use the local development handoff and world decisions for planned work; label it planned and keep private-note links out of the rendered page.
6. **Credit Dark Souls** with an `.influence` line ("Inspired by …") where a system directly parallels it (currently Enemies, Bonfires and Interface).
7. **The Roadmap distinguishes implemented and planned work.** Reconciled against the local development notes on 23 Nov. Future changes must follow the current handoff and approved world decisions; names and layouts can still change.

## Chapters

| # | id | Region | Content |
| --- | --- | --- | --- |
| · | `prologue` | edge | Title, diorama, Download |
| I | `axe` | edge | Throw / Reposition / Recall tabs, combat and Recall-puzzle cards |
| II | `art` | edge | Art pipeline cards, hero turnaround, palette swatches |
| III | `field` | lowlands | Vegetation (grass system) cards, wind-only grass strip |
| IV | `foes` | lowlands | Enemy tab cards (with AI policy), rats/Watcher/contract cards, Dark Souls note |
| V | `bonfire` | barrow | Diorama, animated fire, six bonfire actions, Dark Souls note |
| VI | `atmosphere` | barrow | Lighting and graphics cards, a draggable Restrained/Cinematic comparison of one frame |
| VII | `interface` | barrow | 2×2 UI quadrant (diegetic, meta, spatial, non-diegetic), UI cards, Dark Souls note |
| VIII | `craft` | barrow | Architecture explorer and tenets |
| IX | `ahead` | highlands | Route status, build-log timeline, Download |

The region (`data-region`: `edge`, `lowlands`, `barrow`, `highlands`) drives the page's colours, weather and the place-name card.

## How it's built

- **`index.html`**: all content. Each chapter is a `<section class="chapter" data-region data-title [data-short]>`. `data-short` is an optional shorter map label.
- **`css/showcase.css`**: colours are tokens re-pointed per region on `:root[data-region]`, animated through `@property`. Only palette colours from `Docs/Art/Palettes` are used. The game's 9-slice UI kit (`Panel.png`, `HearthPanel.png`) is applied via `border-image`.
- **`js/showcase.js`**: one setup function per feature.
  - Rail stops are generated from the chapters.
  - **Map:** `MAP_PTS` holds one `[x, y]` per chapter in a 400 × 160 space. They alternate between a dip (y ≈ 116) and a crest (y ≈ 56), and the road is drawn through them. Labels go above crests and below dips.
  - **Tabs:** `setupTabs` drives any `[data-tabs]` group of `role="tab"` buttons with `aria-controls` panels (arrow keys, Home and End).
  - Also here: `setupNextLinks`, the grass strip and the architecture explorer (`NODES` / `EDGES`).
- `[data-fit]` chooses whole-number device-pixel scales where the art fits. On very narrow 1× displays, CSS fits oversized art to the container to preserve the whole composition; that is an intentional exception, not pixel-perfect scaling.
- Canvas loops run only while visible and pause when the document is hidden. `prefers-reduced-motion` selects still banner PNGs through `<picture>`, stops canvas motion and suppresses CSS animation; live preference changes stop/restart the canvas loops without reloading.

### Adding or reordering a chapter

1. Add the `<section>` with `data-region` and `data-title`, plus `data-short` if the title is long.
2. Renumber every `<p class="kicker">Chapter …</p>` and the section comments that follow it.
3. Extend `NUMERALS` in `showcase.js` if you go past IX.
4. Add a point to `MAP_PTS`, keeping the crest/dip alternation and roughly 70+ units between labels on the same side.
5. Resize the region bands in the map `<svg>` (`rect` widths) and match `.map__zones` `grid-template-columns` in the CSS.
6. Update the tour table in `DESIGN.md` and this file.

### Filling a capture slot

Each `<figure class="plate" data-slot="…">` holds an `.plate__empty` placeholder. Replace its contents with `<video autoplay muted loop playsinline poster="…">` or an `<img>`, and add `class="is-filled"` to the figure. Slot names and shot descriptions are in `DESIGN.md`. Plates inside tab groups keep their `id`, `role="tabpanel"` and `hidden` attributes.

### Assets

Everything in `assets/` is copied from the repo:
- **Fonts:** `RoadOfTheOldKing/Assets/UI/Fonts`.
- **Banners and title:** `Docs/Branding`. `assets/art/banners/*-still.png` are exact first-frame extracts of the neighboring accepted GIFs, for reduced motion. Re-extract them if a banner changes; do not generate replacement artwork for this fallback.
- **Sprites and UI kit:** `RoadOfTheOldKing/Assets/Art/Sprites/...`.
- **Crown:** cropped from `Title.png`.

New PixelLab art should go in `assets/art/` and match the README banners: floating dioramas, transparent edges, 384 px wide, region palette.

## Verify before handing back

The page has no tests. To check a change:
1. Serve it and load it in headless Chromium (Playwright). Expect no console errors, and `document.documentElement.scrollWidth` equal to the viewport width at 390 px (no horizontal overflow).
2. Check the map with `M` pressed and both widths (1440 and 390 px) by script; no screenshots for the owner, who looks at the page themselves.
3. Check that map labels don't overlap and that text stays readable on the light regions. Gold text only works on the dark regions; light regions use `var(--ink)`.

## Open items

- Keep the Roadmap reconciled with the local development notes (rule 7); the 23 Nov reconciliation is recorded below.
- Add the PixelLab art in an art session.
- `assets/art/sprites/hero/rotations/north-east.png` and `north-west.png` predate the game's current hero art; re-copy them from `Assets/Art/Sprites/Player/Rotations` in an asset pass.
- Publishing is on hold: the showcase stays in the repository (owner, 23 Nov). A README link is optional once it is published.
- Two UI placements to confirm with the owner: the prompt chip (now anchored over the object, with a key or button icon) is under non-diegetic though it could be argued spatial, and the dialogue camera lean-in is under meta.
- Open questions are listed in `DESIGN.md`.

## Content — 23 Nov

Copy reconciled with the public docs, `CHANGELOG.md` (through `0.5.1-dev.1` and Unreleased), the code and the local development notes. The weapon is the bronze double-bladed axe (no halberd); the browser build is gone, so Play buttons became Download links to the Releases page. Added cards for the combo, cleave, action clock, stamina and Recall puzzles (I); cloak colour, drawn height and the pipeline (II); rats, the Broken Watcher, the attack contract, ground warnings, groups and leash (IV); lighting, travel and upgrade fires (V); water, dark interiors and reward feedback (VI); boss bar, save flame, device prompts, menus and settings (VII). The colour-grade card no longer claims ACES: the region looks override the base profile's tonemapping with none. Architecture nodes use real class names (`PredatorBrain + PursuitBrain`, `AxeView + AxeMeleeFeedback`, `PlayerPrefsProgressStore`). The build log lists every changelog version plus Unreleased and the planned 0.6. Checked only that the HTML parses and the script compiles; the layout checks below were not rerun.

## Review — 24 Oct

**Verification:** headless Chrome runs at the viewports below. See the accessibility pass below for the checks.

**Remaining review findings:**
- Gameplay captures were filled on 23 Nov (see `DESIGN.md`).
- The UI taxonomy still has the two owner questions listed above. Keep them as presentation decisions rather than silently changing categories.

### Accessibility and small screens — 24 Oct

Implemented still banner sources, live reduced-motion switching, visible-only canvas animation and document-hidden pauses. First-frame PNGs preserve all visible GIF pixels and alpha. Existing GIFs and Unity art are unchanged. The pointer lantern stops following movement under reduced motion.

Typography uses 16 px steps, including discrete 32/48/64 px chapter headings. Phone architecture nodes use one readable column; long detail names wrap. The map becomes a scrollable list below 1024 px or on screens at most 600 px tall, with a reachable Close control. This fixes overlapping tablet map labels. Responsive art retains the fit-to-container exception described above.

**Passing checks:** 1440, 1024, 768, 390 and 320 px viewports; no console errors or page overflow, no map-label overlap, all 12 tabs and all 12 architecture nodes, keyboard Home/End and map M/Escape, and the last map link reachable on a 320×568 screen. The three still banners load with reduced motion; canvas output stays still, restarts when motion is enabled, and stops again on a live preference change. Visible text uses 16 px font steps.

These checks establish local draft behavior, not publication readiness or live game-build parity.
