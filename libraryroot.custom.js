// ============================================================================
// 190x4 — STEAM THEME · libraryroot.custom.js
// Инжектится Millennium в главное окно (^Steam$). Задача — DOM-инъекции, которые
// нельзя сделать чистым CSS:
//   1) вставить логотип-вордмарк «190×4» (glitch) на место нативного «STEAM».
// Стиль вставленного элемента — класс .x4-logo из lib/primitives.css.
// React в Steam перемонтирует узлы → держим MutationObserver и переинъектим.
// ============================================================================
(function () {
  "use strict";

  const LOGO_ID = "x4-logo-slot";

  // Кандидаты-контейнеры титулбара слева, куда вставлять лого.
  // TODO(devtools): подтвердить актуальный селектор в DevTools и оставить рабочий.
  const TITLE_SELECTORS = [
    '[class*="titlebar"] [class*="title_text"]',
    '[class*="titlebar"] .title',
    ".main_navbar .title-area",
    '[class*="titlebar_TitleText"]',
  ];

  function buildLogo() {
    const el = document.createElement("span");
    el.id = LOGO_ID;
    el.className = "x4-logo";
    el.setAttribute("data-t", "190×4");
    el.style.cssText = "font-size:15px;margin:0 12px;align-self:center";
    el.innerHTML = '190<span class="x4-x">×4</span>';
    return el;
  }

  function inject() {
    if (document.getElementById(LOGO_ID)) return true; // уже стоит
    for (const sel of TITLE_SELECTORS) {
      const anchor = document.querySelector(sel);
      if (anchor) {
        // прячем нативный текст и ставим наш вордмарк рядом
        anchor.style.visibility = "hidden";
        anchor.style.width = "0";
        const logo = buildLogo();
        anchor.parentElement.insertBefore(logo, anchor);
        return true;
      }
    }
    return false;
  }

  // Первая попытка + наблюдатель за перемонтированием React.
  function start() {
    inject();
    const obs = new MutationObserver(() => {
      if (!document.getElementById(LOGO_ID)) inject();
    });
    obs.observe(document.body, { childList: true, subtree: true });
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", start);
  } else {
    start();
  }
})();
