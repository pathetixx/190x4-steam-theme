<div align="center">

# 190×4

**Cyberpunk command-center theme for the Steam Client — red neon over graphite.**

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

---

## How it works

Steam renders its client with **hashed** CSS class names (`._3cI5TX…`) that change with
every build — so you can't target them directly and have it survive updates. But the
source classes are **semantically named**, and Steam keeps that registry inside its own
webpack modules (CSS-module objects whose exports are all strings).

This theme reads that registry at runtime (`client/resolver.js`), resolves each **stable
name → current hash**, and tags matching elements with our own stable attributes
(`data-x4="play-button"`, …). The CSS (`client/client.css`) then styles those attributes.
No copied hash lists, no chasing Steam builds.

```
skin.json            manifest (RootColors, Steam-WebKit, Patches)
theme/colors.css     palette (RootColors) — edit colors here
client/resolver.js   reads Steam's class registry → tags elements data-x4
client/client.css    foundation + rules on the data-x4 hooks + logo
webkit/store.css     store & community (stable classes, targeted directly)
tools/dump-classmap.js  console snippet: dump the full name→hash registry
design/              bespoke reference mockups (north-star)
```

**Brand rules:** red `#ff2530` = accent / Play / selected / connected (Steam blue is
remapped); green `#38e07b` = online status only; graphite surfaces, sharp corners.

---

## Installation

Millennium must already be installed ([guide](https://docs.steambrew.app/users/installing)).

1. Download the release zip, extract the `190x4/` folder into:
   ```
   <Steam>/millennium/themes/190x4/
   ```
   On Windows usually `C:\Program Files (x86)\Steam\millennium\themes\190x4\`.
2. In Steam open **Millennium → Themes**, select **190x4**, restart Steam.
3. Edit colors in `theme/colors.css`.

---

## Mapping the client (DevTools)

The resolver targets stable names; to pin them precisely for the current build:

1. Enable Millennium **developer mode** → right-click the Steam window → **Inspect** →
   **Console**.
2. With the theme installed, run `x4.dump()` (or paste `tools/dump-classmap.js`). It copies
   the full `{ ClassName: hash }` registry to the clipboard.
3. Save it as `classmap.json` — from that list the `TARGETS` in `client/resolver.js` get
   finalized to the exact names your build exposes.

`x4.findClass("PlayButton")` resolves a single name in the console.
