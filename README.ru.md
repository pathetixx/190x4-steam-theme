<div align="center">

# 190×4

**Киберпанк command-center колорвей для клиента Steam — красный неон по графиту.**

Тёмная тема для [Millennium](https://github.com/SteamClientHomebrew/Millennium).

`Windows` · `Linux` · клиент Steam

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

> Рендеры в `design/` — это **целевая айдентика** (долгосрочная кастомная раскладка).
> Текущий релиз — 190×4 **колорвей**, см. «Как устроено» ниже.

---

## Как устроено

Клиент Steam рисуется **хешированными** CSS-классами, которые меняются на каждой сборке,
поэтому перекрасить его «вслепую» нельзя. Тема накладывает палитру 190×4 на **движок
селекторов [SpaceTheme](https://github.com/SpaceTheme/Steam)** (MIT) — актуальную
поддерживаемую карту этих классов. Все цвета берутся из одного файла палитры
(`src/css/root.css`); остальное — структурный CSS SpaceTheme.

Правила бренда в палитре:
- **Красный `#ff2530` = акцент / Play / выбранное / connected** (синий Steam перекрашен в красный).
- **Зелёный `#38e07b` — только статус «в сети».**
- Графитовые поверхности, острые углы 3px.

---

## Установка

Millennium должен быть установлен ([инструкция](https://docs.steambrew.app/users/installing)).

1. Скачать zip релиза, распаковать папку `190x4/` в:
   ```
   <Steam>/millennium/themes/190x4/
   ```
   На Windows обычно `C:\Program Files (x86)\Steam\millennium\themes\190x4\`.
2. В Steam открыть **Millennium → Themes**, выбрать **190x4**, перезапустить Steam.
3. Опции (раскладка, сайдбар, шрифты, радиус) — в настройках темы Millennium; цвета —
   правкой `src/css/root.css`.

---

## Кастомизация

- **Цвета:** переменные `--st-*` в `src/css/root.css` (значения `R, G, B`).
- **Раскладка/поведение:** в настройках темы Millennium доступны тогглы SpaceTheme
  (позиция сайдбара, what's-new, баннер, шрифты и т.д.).
- `design/` — кастомные референс-макеты 190×4 (HTML + скрины), ориентир на будущее.

---

## Кредиты

- Движок селекторов и структурный CSS: **[SpaceTheme/Steam](https://github.com/SpaceTheme/Steam)**
  (SpaceEnergy, MIT) — см. [`LICENSE`](LICENSE). Этот проект — палитра/колорвей 190×4 поверх него.
- Фреймворк: **[Millennium](https://github.com/SteamClientHomebrew/Millennium)**.
