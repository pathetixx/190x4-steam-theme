// ============================================================================
// 190x4 - STEAM THEME · client/rail.js
// Кастомная фишка темы: вертикальный иконочный нав-рельс слева (как Fluenty,
// но в стиле 190x4). Техника проверенная: ждём рамку клиента, переносим
// ContentFrame в flex-обёртку рядом с рельсом (DOM-move сохраняет React-слушатели).
// Фейл-безопасно: если рамка не найдена — рельс не строится, клиент не трогается.
// Навигация через SteamUIStore (как в Fluenty); всё в try/catch.
// ============================================================================
(function () {
  "use strict";
  var SEL_OUTER = '[class*="steamdesktop_OuterFrame_"]';
  var SEL_CONTENT = '[class*="steamdesktop_ContentFrame_"]';

  // доступ к API главного окна (в разных контекстах лежит на window или opener)
  function api(name) {
    try { return window[name] || (window.opener && window.opener[name]); } catch (e) { return null; }
  }
  function store() { return api("SteamUIStore"); }
  function browser() { return api("MainWindowBrowserManager"); }
  function urls() { return api("urlStore"); }

  function go(fn) { return function () { try { fn(); } catch (e) { /* no-op */ } }; }
  function navLib() { store().Navigate("/library/home"); }
  function navColl() { store().Navigate("/library/collections"); }
  function navDl() { store().Navigate("/library/downloads"); }
  function navWeb(key) {
    return function () {
      var u = urls().m_steamUrls[key].url;
      store().Navigate("/browser", browser().LoadURL(u));
    };
  }
  function navSettings() { try { window.location.href = "steam://open/settings"; } catch (e) {} }

  // минимальные иконки (stroke=currentColor)
  var IC = {
    lib: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>',
    store: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 9h18l-1.5 10.5a2 2 0 0 1-2 1.5H6.5a2 2 0 0 1-2-1.5L3 9z"/><path d="M8 9V6a4 4 0 0 1 8 0v3"/></svg>',
    comm: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="9" cy="8" r="3"/><circle cx="17" cy="10" r="2.4"/><path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6"/><path d="M15.5 14.5c2.5.4 4.5 2.4 4.5 5.5"/></svg>',
    dl: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 3v12"/><path d="M7 11l5 5 5-5"/><path d="M5 21h14"/></svg>',
    set: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.6 1.6 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.6 1.6 0 0 0-2.7 1.1V21a2 2 0 1 1-4 0v-.1A1.6 1.6 0 0 0 6.6 19l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1A1.6 1.6 0 0 0 4 13.4H3.9a2 2 0 1 1 0-4H4a1.6 1.6 0 0 0 1.5-2.7l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1A1.6 1.6 0 0 0 11 4V3.9a2 2 0 1 1 4 0V4a1.6 1.6 0 0 0 2.7 1.1l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1A1.6 1.6 0 0 0 20 11h.1a2 2 0 1 1 0 4H20a1.6 1.6 0 0 0-.6.9z"/></svg>'
  };

  var ITEMS = [
    { ic: "lib", title: "Библиотека", on: go(navLib), match: "library/home" },
    { ic: "store", title: "Магазин", on: go(navWeb("StoreFrontPage")), match: null },
    { ic: "comm", title: "Сообщество", on: go(navWeb("CommunityHome")), match: null },
    { ic: "dl", title: "Загрузки", on: go(navDl), match: "library/downloads" },
    { ic: "set", title: "Настройки", on: go(navSettings), match: null }
  ];

  function setActive() {
    try {
      var path = browser().m_history.location.pathname || "";
      var btns = document.querySelectorAll(".x4-rail-btn");
      for (var i = 0; i < btns.length; i++) {
        var m = btns[i].getAttribute("data-match");
        btns[i].classList.toggle("is-active", !!m && path.indexOf(m) !== -1);
      }
    } catch (e) {}
  }

  function buildRail() {
    if (document.querySelector(".x4-rail")) return true;
    var outer = document.querySelector(SEL_OUTER);
    var content = document.querySelector(SEL_CONTENT);
    if (!outer || !content) return false;

    var wrap = document.createElement("div");
    wrap.className = "x4-railwrap";
    var rail = document.createElement("div");
    rail.className = "x4-rail";

    ITEMS.forEach(function (it, idx) {
      var b = document.createElement("div");
      b.className = "x4-rail-btn";
      b.title = it.title;
      if (it.match) b.setAttribute("data-match", it.match);
      b.innerHTML = IC[it.ic];
      b.addEventListener("click", function () { it.on(); setTimeout(setActive, 80); });
      rail.appendChild(b);
      if (idx === ITEMS.length - 2) {
        var sep = document.createElement("div");
        sep.className = "x4-rail-sep";
        rail.appendChild(sep);
      }
    });

    outer.insertBefore(wrap, content);
    wrap.appendChild(rail);
    wrap.appendChild(content); // перенос контента рядом с рельсом (flex)
    setActive();
    return true;
  }

  // ждём рамку клиента; строим один раз
  var tries = 0;
  function tick() {
    if (buildRail() || tries++ > 60) return;
    setTimeout(tick, 250);
  }
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", tick);
  } else {
    tick();
  }
})();
