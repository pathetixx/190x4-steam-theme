// 190x4 · DEV LOADER — вставить ОДИН раз в DevTools Console главного окна Steam.
// Тянет dev/live.css из main и инжектит/обновляет <style id="x4live"> в конце head
// (последний в head + !important => бьёт уже инжектнутую тему). Повторный запуск
// (стрелка вверх + Enter) перетягивает свежий live.css — мгновенный hot-reload.
(async () => {
  const url = 'https://raw.githubusercontent.com/pathetixx/190x4-steam-theme/main/dev/live.css?t=' + Date.now();
  const css = await (await fetch(url, { cache: 'no-store' })).text();
  const s = document.getElementById('x4live') || document.head.appendChild(
    Object.assign(document.createElement('style'), { id: 'x4live' })
  );
  s.textContent = css;
  console.log('%c[x4 live] ' + css.length + ' chars · ' + new Date().toLocaleTimeString(), 'color:#ff2530');
})();
