# Шрифты — 190×4 Steam Theme

Тема использует три семейства Google Fonts. Бинарные файлы (`.woff2`) **не вложены** в
бандл — положи их сюда сам (см. ниже). Пока файлов нет, тема тянет шрифты с Google Fonts
через `@import` в шапке `theme.css` — это рабочий вариант, но требует интернета.

## Семейства и начертания

| Семейство | Где используется | Нужные веса |
|---|---|---|
| **Orbitron** | ТОЛЬКО логотип + верхняя навигация | 500, 600, 700, 800, 900 |
| **Saira** | Весь UI и тело | 300, 400, 500, 600, 700 |
| **Saira Semi Condensed** | Плотные лейблы, заголовки-таблички, капсы | 400, 500, 600, 700 |

> Логотип «×4» использует доп. цвета (`#8c0d14` / `#c01622`), сам шрифт — Orbitron 900.

## Вариант A — оставить Google Fonts (по умолчанию, ничего не делать)
В `theme.css` уже есть:
```css
@import url('https://fonts.googleapis.com/css2?family=Orbitron:wght@500;600;700;800&family=Saira:wght@300;400;500;600;700&family=Saira+Semi+Condensed:wght@400;500;600;700&display=swap');
```
Минус — зависимость от сети. Для темы Steam (Millennium) обычно ок, но автономный
self-host надёжнее.

## Вариант B — локальный self-host (рекомендуется для темы)

1. Скачать семейства (любой способ):
   - https://fonts.google.com/specimen/Orbitron
   - https://fonts.google.com/specimen/Saira
   - https://fonts.google.com/specimen/Saira+Semi+Condensed
   - либо утилитой google-webfonts-helper (https://gwfh.mranftl.com/fonts) — отдаёт
     готовые `.woff2` + `@font-face` сразу по нужным весам.
2. Положить `.woff2` в эту папку (`assets/fonts/`), например:
   ```
   assets/fonts/Orbitron-700.woff2
   assets/fonts/Orbitron-900.woff2
   assets/fonts/Saira-400.woff2
   assets/fonts/Saira-600.woff2
   assets/fonts/SairaSemiCondensed-600.woff2
   ...
   ```
3. В `webkit.css` темы **убрать** `@import` Google Fonts и добавить `@font-face`
   (путь относительно темы Millennium — обычно от корня темы):
   ```css
   @font-face{ font-family:'Orbitron'; font-weight:700; font-style:normal;
     font-display:swap; src:url('assets/fonts/Orbitron-700.woff2') format('woff2'); }
   @font-face{ font-family:'Orbitron'; font-weight:900; font-style:normal;
     font-display:swap; src:url('assets/fonts/Orbitron-900.woff2') format('woff2'); }
   @font-face{ font-family:'Saira'; font-weight:400; font-style:normal;
     font-display:swap; src:url('assets/fonts/Saira-400.woff2') format('woff2'); }
   @font-face{ font-family:'Saira'; font-weight:600; font-style:normal;
     font-display:swap; src:url('assets/fonts/Saira-600.woff2') format('woff2'); }
   @font-face{ font-family:'Saira Semi Condensed'; font-weight:600; font-style:normal;
     font-display:swap; src:url('assets/fonts/SairaSemiCondensed-600.woff2') format('woff2'); }
   /* повторить для остальных нужных весов из таблицы выше */
   ```
   Токены `--f-display` / `--f-ui` / `--f-cond` в `:root` менять не нужно — они уже ссылаются
   на эти семейства по имени.

Лицензия всех трёх — SIL Open Font License 1.1 (свободно для встраивания и распространения).
