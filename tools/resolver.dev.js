// ============================================================================
// 190x4 — STEAM THEME · class resolver
// Steam рисует UI хешированными CSS-классами (._3cI5TX...), которые меняются на
// каждой сборке. НО исходные классы семантически именованы, и Steam держит их
// реестр в своих webpack-модулях (CSS-модули = объекты, где все экспорты строки).
// Здесь мы повторяем механизм Millennium: собираем этот реестр и резолвим
// СТАБИЛЬНОЕ имя → текущий хеш. Затем вешаем на элементы наши стабильные метки
// data-x4="...", а стилизуем их в client/client.css. Так тема переживает апдейты.
//
// Реестр доступен и в консоли: x4.dump() — скопирует весь {ИмяКласса: хеш}.
// ============================================================================
(function () {
  "use strict";

  // ---- 1. Собрать реестр CSS-модулей из webpack (как @steambrew/client) ----
  function buildClassMapList() {
    const list = [];
    const wp = window.webpackChunksteamui;
    if (!wp || typeof wp.push !== "function") return list; // не главное окно (напр. friends)
    let req;
    try {
      wp.push([[Symbol("190x4")], {}, (r) => { req = r; }]);
    } catch (e) { return list; }
    if (!req || !req.m) return list;
    const seen = new Set();
    for (const id of Object.keys(req.m)) {
      let mod;
      try { mod = req(id); } catch { continue; }
      if (!mod) continue;
      for (const m of [mod.default, mod]) {
        if (typeof m !== "object" || m === null || m.__esModule) continue;
        if (seen.has(m)) continue;
        const keys = Object.keys(m);
        if (keys.length === 0) continue;
        if (keys.length === 1 && m.version) continue;                 // версии библиотек
        if (keys.length > 1000 && m.AboutSettings) continue;          // локализация
        const allStrings = keys.every(
          (k) => typeof m[k] === "string" &&
                 !Object.getOwnPropertyDescriptor(m, k)?.get
        );
        if (allStrings) { seen.add(m); list.push(m); }
      }
    }
    return list;
  }

  const classMapList = buildClassMapList();
  const classMap = Object.assign({}, ...classMapList);

  // резолв имени → текущий хеш (без точки). Имя может прийти строкой или массивом
  // кандидатов (берём первое, что нашлось в реестре текущей сборки).
  function findClass(name) {
    const names = Array.isArray(name) ? name : [name];
    for (const n of names) {
      const m = classMapList.find((x) => x[n]);
      if (m) return m[n];
    }
    return undefined;
  }

  // ---- 2. Карта целей: СТАБИЛЬНОЕ имя класса Steam → наша метка data-x4 ----
  // ВАЖНО: финальный список имён берём из дампа реестра на твоей сборке
  // (tools/dump-classmap.js → x4.dump()). Ниже — стартовый набор; пополняется.
  // Имена сверены с дампом реестра боевой сборки (classmap, 22156 классов,
  // 2026-06-04). Берём только настоящие CSS-классы (значение = хеш), i18n-ключи
  // с тем же именем отброшены. Порядок = приоритет (findClass берёт первое).
  const TARGETS = [
    // titlebar / навигация
    { tag: "titlebar",   names: ["TitleBar", "Draggable", "WindowControls"] },
    { tag: "nav-tab",    names: ["SteamPageHeaderTopLink", "TabRow", "Tab"] },
    { tag: "nav-active", names: ["ActiveTab", "Selected"] },
    // библиотека
    { tag: "sidebar",    names: ["GameList", "LeftColumn", "LeftCol"] },
    { tag: "game-row",   names: ["GameListEntryContainer", "GameListEntryName", "GameListEntryLabels"] },
    { tag: "game-cover", names: ["CapsuleImage", "AssetImage", "Capsule"] },
    // страница игры
    { tag: "play-button", names: ["PlayButton"] },
    { tag: "app-header",  names: ["Header", "TopCapsule"] },
    { tag: "progress",    names: ["ProgressBar", "Bar"] },
    // контролы
    { tag: "toggle-on",   names: ["ToggleOn", "On"] },
    { tag: "primary-btn", names: ["DialogButton", "Button"] },
  ];

  // развернуть TARGETS в карту {currentHash: tag}
  function resolveTargets() {
    const map = {};
    for (const t of TARGETS) {
      const cls = findClass(t.names);
      if (cls) map[cls] = t.tag;
    }
    return map;
  }
  let resolved = resolveTargets();

  // ---- 3. Развесить метки data-x4 на элементы ----
  function tagAll(root) {
    for (const [cls, tag] of Object.entries(resolved)) {
      const nodes = (root || document).querySelectorAll("." + CSS.escape(cls));
      for (const el of nodes) {
        if (el.getAttribute("data-x4") !== tag) el.setAttribute("data-x4", tag);
      }
    }
  }

  // ---- 4. Логотип-вордмарк «190×4» вместо нативного STEAM ----
  function injectLogo() {
    if (document.getElementById("x4-logo")) return;
    const barCls = findClass(["TitleBar", "titlebar"]);
    const anchor = barCls
      ? document.querySelector("." + CSS.escape(barCls))
      : document.querySelector('[class*="titlebar" i]');
    if (!anchor) return;
    const logo = document.createElement("span");
    logo.id = "x4-logo";
    logo.className = "x4-logo";
    logo.setAttribute("data-t", "190×4");
    logo.innerHTML = '190<span class="x4-x">×4</span>';
    anchor.prepend(logo);
  }

  // ---- 5. Наблюдатель за перемонтированием React (debounce) ----
  let pending = null;
  function schedule() {
    if (pending) return;
    pending = requestAnimationFrame(() => {
      pending = null;
      tagAll();
      injectLogo();
    });
  }

  function start() {
    tagAll();
    injectLogo();
    new MutationObserver(schedule).observe(document.body, {
      childList: true, subtree: true,
    });
  }
  if (document.readyState === "loading")
    document.addEventListener("DOMContentLoaded", start);
  else start();

  // ---- 6. Консольные хелперы для разработки ----
  window.x4 = {
    classMap,
    classMapList,
    findClass,
    targets: () => resolved,
    // x4.dump() — скопировать весь реестр {ИмяКласса: хеш} в буфер
    dump() {
      const json = JSON.stringify(classMap, null, 2);
      try { copy(json); } catch {}
      console.log("[190x4] классов в реестре:", Object.keys(classMap).length,
                  "— скопировано в буфер");
      return json;
    },
    retag: () => { resolved = resolveTargets(); tagAll(); },
  };
  console.log("[190x4] resolver готов. Классов:", Object.keys(classMap).length,
              "· x4.dump() для выгрузки реестра");
})();
