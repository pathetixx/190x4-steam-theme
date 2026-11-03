# 190x4 Bloodmoon

Тема оформления Steam: чёрная бумага, тушь, багровый серп и цепи. Работает без Millennium и без правки файлов Steam:
маленький скрипт подключается к локальному порту отладки клиента и добавляет стили в его окна.

## Как устроено

- `src/client.css` окна клиента. Классы пишутся как `{{Имя}}` или `{{Имя@Сосед}}`, подключатель берёт их текущие хеши из реестра CSS-модулей живого Steam, поэтому обновления Steam хешей не ломают.
- `src/web.css` магазин и сообщество.
- `loader/recolor.js` перекрашивает синее и зелёное в самих стилях Steam. Стили с чужого домена подключатель скачивает и подменяет перекрашенной копией.
- `loader/resolver.js` читает реестр классов.
- `loader/x4-steam-bloodmoon.ps1` подключатель: следит за окнами, применяет тему, сам подхватывает изменённые файлы.
- `build.mjs` собирает `out/`: шрифты и картинки встраиваются в CSS.

## Сборка и запуск

```bash
node bloodmoon/build.mjs
```

Содержимое `out/` копируется на Windows в одну папку. Steam запускается с `-cef-enable-debugging`
или с файлом `.cef-enable-remote-debugging` в папке Steam.

```powershell
powershell -ExecutionPolicy Bypass -File x4-steam-bloodmoon.ps1          # работает, пока не закрыт
powershell -ExecutionPolicy Bypass -File x4-steam-bloodmoon.ps1 -Once    # один проход
powershell -ExecutionPolicy Bypass -File x4-steam-bloodmoon.ps1 -Remove  # убрать тему с открытых окон
```

Порт отладки работает без пароля и слушает только `127.0.0.1`: программы на этом ПК могут управлять клиентом.
