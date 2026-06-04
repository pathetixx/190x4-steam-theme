<div align="center">

# 190×4

**Киберпанк command-center тема для клиента Steam — красный неон по графиту.**

Тёмный высококонтрастный рескин на [Millennium](https://github.com/SteamClientHomebrew/Millennium).

`Windows` · `Linux` · клиент Steam (CEF / Chromium 126)

[English version →](README.md)

</div>

---

## Превью

| Страница игры | Библиотека |
|---|---|
| ![Игра](design/screenshots/03-game.png) | ![Библиотека](design/screenshots/02-library.png) |

| Магазин | Дизайн-система |
|---|---|
| ![Магазин](design/screenshots/01-store.png) | ![Дизайн-система](design/screenshots/10-design-system.png) |

> Это эталонные рендеры. **Фундамент** темы (фон, шрифты, акцент, скроллбары, focus)
> применяется сразу; per-screen раскладка доводится в DevTools — см. ниже.

---

## Особенности

- **Красный = успех.** Инверсия семантики: кнопка «Играть», активные табы, прогресс загрузки,
  индикаторы connected/в игре — красный неон. Зелёный — **только** точка «в сети».
- **Orbitron** для логотипа и верхней навигации; **Saira** — для всего остального.
- Glitch-вордмарк **190×4** (RGB-расслоение) на месте нативного «STEAM» в титулбаре.
- Графитовые поверхности, hairline-границы, острые углы 2–5px, неоновый glow вместо мягких теней.
- **Устойчивый к апдейтам core:** recolor идёт через CSS-переменные Steam, а не через хеш-классы —
  обновления клиента не сбивают базовую палитру.
- Полностью настраиваемые цвета через редактор Millennium (`Settings → Themes → Edit`).

---

## Установка

Millennium должен быть уже установлен ([инструкция](https://docs.steambrew.app/users/installing)).

1. Скопировать папку в каталог скинов Steam:
   ```
   <Steam>/steamui/skins/190x4/
   ```
   На Windows обычно `C:\Program Files (x86)\Steam\steamui\skins\190x4\`.
   Папку `design/` можно не копировать — она для разработки.
2. В Steam открыть меню **Millennium → Themes**, выбрать **190x4**, перезапустить Steam.
3. Цвета правятся в **Settings → Themes → Edit** (значения из `colors.css`).

---

## Структура

| Файл | Роль |
|---|---|
| `skin.json` | Манифест: `Patches` по regex заголовка окна, `RootColors`, `Steam-WebKit` |
| `colors.css` | **RootColors** — палитра/токены + оверрайды core-переменных Steam (правит юзер) |
| `lib/primitives.css` | Примитивы `.x4-*` (кнопки, поля, тогглы, бейджи, лого, текстуры) |
| `libraryroot.custom.css` | Главное окно (`^Steam$`) + друзья/чат/модалки/меню |
| `libraryroot.custom.js` | Инъекция лого-вордмарка `190×4` |
| `webkit/webkit.css` | Магазин и сообщество (`*.steampowered.com`, `steamcommunity.com`) |
| `design/` | Визуальные референсы (HTML-макеты, `theme.css`, скрины) — не инжектится |

**Фундамент** в каждом CSS работает независимо от сборки Steam. **Per-screen** блоки заякорены
на «дружелюбные» класс-префиксы Steam (`[class*="appdetailsplaysection_PlayButton"]`) и помечены
`TODO(devtools)` для пиксельной доводки по референсам.

---

## Доводка / вклад

1. Включить **developer mode** в Millennium (или подключиться к CEF debugger на `localhost:8080`).
2. Снять реальный класс нужного элемента.
3. Найти `§`-блок в `libraryroot.custom.css` / `webkit.css`, заменить заякоренный `[class*="…"]`
   на проверенный селектор, снять пометку `TODO(devtools)`.
4. Сверить с `design/<Screen>.html` и `design/screenshots/NN-*.png` пиксель-в-пиксель.
   Цвет-семантику не нарушать: красный = успех/Play/connected; зелёный = только точка «в сети».
5. Открыть `design/index.html` в браузере — лукбук всех экранов.

---

## Шрифты

Orbitron, Saira и Saira Semi Condensed грузятся с Google Fonts через `@import` в `colors.css`.
Для офлайн/self-host — см. [`assets/fonts/README.md`](assets/fonts/README.md).
