<div align="center">

# 190×4

**Cyberpunk command-center theme for the Steam Client — red neon over graphite.**

A dark, high-contrast reskin built for [Millennium](https://github.com/SteamClientHomebrew/Millennium).

`Windows` · `Linux` · Steam Client (CEF / Chromium 126)

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

> These are the reference renders. The theme's **foundation** (background, fonts, accent,
> scrollbars, focus) applies immediately; per-screen layout is refined in DevTools — see below.

---

## Features

- **Red = success.** Inverted semantics: the Play button, active tabs, download progress,
  connected/in-game indicators are all neon red. Green is reserved **only** for the online dot.
- **Orbitron** for the logo and top navigation; **Saira** for everything else.
- Glitch (RGB-split) **190×4** wordmark replacing the native "STEAM" titlebar text.
- Graphite surfaces, hairline borders, sharp 2–5px corners, neon glow instead of soft shadows.
- **Update-resilient core:** recoloring is driven through Steam's CSS custom properties, not
  hashed class names — so client updates don't wipe the base palette.
- Fully color-customizable via Millennium's theme editor (`Settings → Themes → Edit`).

---

## Installation

Millennium must already be installed ([guide](https://docs.steambrew.app/users/installing)).

1. Copy this folder into the Millennium themes directory:
   ```
   <Steam>/millennium/themes/190x4/
   ```
   On Windows that's usually `C:\Program Files (x86)\Steam\millennium\themes\190x4\`.
   The `design/` folder is for development only and can be omitted.
2. In Steam, open the **Millennium → Themes** menu, select **190x4**, and restart Steam.
3. Tweak colors in **Settings → Themes → Edit** (values defined in `colors.css`).

---

## Structure

| File | Role |
|---|---|
| `skin.json` | Manifest: window-title `Patches`, `RootColors`, `Steam-WebKit` |
| `colors.css` | **RootColors** — palette/tokens + Steam core-variable overrides (user-editable) |
| `lib/primitives.css` | `.x4-*` component primitives (buttons, inputs, toggles, badges, logo, textures) |
| `libraryroot.custom.css` | Main client window (`^Steam$`) + friends/chat/modals/menus |
| `libraryroot.custom.js` | Injects the `190×4` logo wordmark |
| `webkit/webkit.css` | Store & Community pages (`*.steampowered.com`, `steamcommunity.com`) |
| `design/` | Visual references (HTML mockups, `theme.css` source of truth, screenshots) — not injected |

The **foundation** layer in each CSS file works regardless of the Steam build. **Per-screen**
blocks are anchored to Steam's friendly class prefixes (`[class*="appdetailsplaysection_PlayButton"]`)
and marked `TODO(devtools)` for pixel-level tuning against the references.

---

## Customizing / contributing

1. Enable **developer mode** in Millennium (or attach to the CEF debugger on `localhost:8080`).
2. Inspect the real class of the element you want to style.
3. Find the matching `§` block in `libraryroot.custom.css` / `webkit.css`, replace the anchored
   `[class*="…"]` selector with the verified one, and drop the `TODO(devtools)` marker.
4. Match it against `design/<Screen>.html` and `design/screenshots/NN-*.png` pixel-for-pixel.
   Keep the color semantics: red = success/Play/connected; green = online dot only.
5. Open `design/index.html` in a browser for a lookbook of every screen.

---

## Fonts

Orbitron, Saira and Saira Semi Condensed are loaded from Google Fonts via `@import` in
`colors.css`. For offline / self-hosting, see [`assets/fonts/README.md`](assets/fonts/README.md).
