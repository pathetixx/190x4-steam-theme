// ============================================================================
// 190x4 · LIVE DEV JS — кастом-рельс слева. Грузится JS-loader'ом, идемпотентно.
// Навигация: клик по родным (скрытым CSS) нав-кнопкам = реальный роут без угадайки.
// Загрузки/Друзья/Настройки — через opener.SteamUIStore / SteamClient steam://.
// ============================================================================
(function () {
  "use strict";
  var prev = document.getElementById("x4rail");
  if (prev) prev.remove();
  if (window.__x4railObs) { window.__x4railObs.disconnect(); window.__x4railObs = null; }

  var O = window.opener || window;
  var SC = window.SteamClient || O.SteamClient;

  function nativeNav(label) {
    var b = [].slice.call(document.querySelectorAll("._19axKcqYRuaJ8vdYKYmtTQ"));
    for (var i = 0; i < b.length; i++) {
      if (b[i].textContent.trim().toUpperCase().indexOf(label) === 0) return b[i];
    }
    return null;
  }
  function clickNav(label) { var n = nativeNav(label); if (n) n.click(); }
  function navTo(route) { try { O.SteamUIStore.Navigate(route); } catch (e) {} }
  function steamUrl(u) { try { SC.URL.ExecuteSteamURL(u); } catch (e) {} }

  var ICON = {
    lib: '<rect x="3" y="3" width="7" height="7"/><rect x="14" y="3" width="7" height="7"/><rect x="3" y="14" width="7" height="7"/><rect x="14" y="14" width="7" height="7"/>',
    store: '<path d="M3 9h18l-1.5 11h-15z"/><path d="M8 9V6a4 4 0 0 1 8 0v3"/>',
    comm: '<circle cx="9" cy="8" r="3"/><circle cx="17" cy="9" r="2.2"/><path d="M3 20c0-3.3 2.7-5 6-5s6 1.7 6 5"/><path d="M16 14c2.4.2 5 1.7 5 4.5"/>',
    dl: '<path d="M12 3v12"/><path d="M7 11l5 5 5-5"/><path d="M4 20h16"/>',
    friends: '<path d="M21 11.5a8.4 8.4 0 0 1-9 8.4L3 21l1.1-2.9A8.4 8.4 0 1 1 21 11.5z"/>',
    settings: '<circle cx="12" cy="12" r="3"/><path d="M19.4 13a7.5 7.5 0 0 0 0-2l2-1.6-2-3.4-2.4 1a7.5 7.5 0 0 0-1.7-1l-.4-2.5h-4l-.4 2.5a7.5 7.5 0 0 0-1.7 1l-2.4-1-2 3.4 2 1.6a7.5 7.5 0 0 0 0 2l-2 1.6 2 3.4 2.4-1a7.5 7.5 0 0 0 1.7 1l.4 2.5h4l.4-2.5a7.5 7.5 0 0 0 1.7-1l2.4 1 2-3.4z"/>'
  };
  function svg(p) {
    return '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round">' + p + "</svg>";
  }

  var items = [
    { k: "lib",      t: "Библиотека", f: function () { clickNav("БИБЛИОТЕКА"); } },
    { k: "store",    t: "Магазин",    f: function () { clickNav("МАГАЗИН"); } },
    { k: "comm",     t: "Сообщество", f: function () { clickNav("СООБЩЕСТВО"); } },
    { k: "dl",       t: "Загрузки",   f: function () { navTo("/library/downloads"); } },
    { sep: true },
    { k: "friends",  t: "Друзья",     f: function () { steamUrl("steam://open/friends"); } },
    { k: "settings", t: "Настройки",  f: function () { steamUrl("steam://open/settings"); } }
  ];

  var rail = document.createElement("div");
  rail.id = "x4rail";
  items.forEach(function (it) {
    if (it.sep) { var s = document.createElement("div"); s.className = "x4rail-sep"; rail.appendChild(s); return; }
    var b = document.createElement("div");
    b.className = "x4rail-btn";
    b.title = it.t;
    b.innerHTML = svg(ICON[it.k]);
    b.addEventListener("click", it.f);
    rail.appendChild(b);
  });
  document.body.appendChild(rail);

  // держим рельс живым, если Steam перерисует body
  window.__x4railObs = new MutationObserver(function () {
    if (!document.getElementById("x4rail")) document.body.appendChild(rail);
  });
  window.__x4railObs.observe(document.body, { childList: true });

  console.log("[x4 rail] built, buttons=" + rail.querySelectorAll(".x4rail-btn").length);
})();
