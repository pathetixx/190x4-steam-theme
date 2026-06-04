<div align="center">

# 190×4

**Cyberpunk command-center colorway for the Steam Client — red neon over graphite.**

A dark [Millennium](https://github.com/SteamClientHomebrew/Millennium) theme.

`Windows` · `Linux` · Steam Client

[Русская версия →](README.ru.md)

</div>

---

## Preview

| Game page | Library |
|---|---|
| ![Game](design/screenshots/03-game.png) | ![Library](design/screenshots/02-library.png) |

| Store | Design system |
|---|---|
| ![Store](design/screenshots/01-store.png) | ![Design system](design/screenshots/10-design-system.png) |

> These renders in `design/` are the **target identity** (the long-term bespoke layout).
> The current release is a 190×4 **colorway** — see *How it works* below.

---

## How it works

The Steam client is rendered with fully **hashed** CSS class names that change with
every Steam build, so a recolor can't be authored blind. This theme therefore runs the
190×4 palette on top of the **[SpaceTheme](https://github.com/SpaceTheme/Steam)** selector
engine (MIT) — a current, maintained map of those classes. All colors come from a single
palette file (`src/css/root.css`); everything else is SpaceTheme's structural CSS.

Brand rules applied in the palette:
- **Red `#ff2530` = accent / Play / selected / connected** (Steam's blue is remapped to red).
- **Green `#38e07b` — online status only.**
- Graphite surfaces, sharp 3px corners.

---

## Installation

Millennium must already be installed ([guide](https://docs.steambrew.app/users/installing)).

1. Download the release zip and extract the `190x4/` folder into:
   ```
   <Steam>/millennium/themes/190x4/
   ```
   On Windows that's usually `C:\Program Files (x86)\Steam\millennium\themes\190x4\`.
2. In Steam, open **Millennium → Themes**, select **190x4**, and restart Steam.
3. Tweak options (layout toggles, sidebar, fonts, border radius) in the Millennium theme
   settings; recolor by editing `src/css/root.css`.

---

## Customizing

- **Colors:** edit the `--st-*` variables in `src/css/root.css` (values are `R, G, B`).
- **Layout/behavior:** the Millennium theme settings expose SpaceTheme's option toggles
  (sidebar position, what's-new, banner, fonts, etc.).
- `design/` holds the bespoke 190×4 reference mockups (HTML + screenshots) — the design
  north-star for future passes.

---

## Credits

- Selector engine & structural CSS: **[SpaceTheme/Steam](https://github.com/SpaceTheme/Steam)**
  by SpaceEnergy, MIT — see [`LICENSE`](LICENSE). This project is a 190×4 palette/colorway
  on top of it.
- Framework: **[Millennium](https://github.com/SteamClientHomebrew/Millennium)**.
