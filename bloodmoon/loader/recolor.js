// Перекрашивает синие цвета стилей Steam: акцент в багровый, сине-серое в тёплый чёрный.
// Так не надо охотиться за каждым классом, и новые экраны после обновлений Steam тоже становятся тёмными.
// В окнах клиента оригинальные стили не трогаем: Chromium держит их одной копией на все окна,
// а правка через CSSOM размножила бы их. Поверх кладём только правила оформления.
(function () {
  if (window.__x4recolor) { window.__x4recolor.run(); return; }
  // окна клиента тоже бывают на https (steamloopback.host), поэтому проверяем сам сайт
  var isWeb = /(^|\.)(steampowered|steamcommunity)\.com$/.test(location.hostname);
  // Пока тема не встала, окно остаётся чёрным: родной вид не успевает мелькнуть. Таймер снимет шторку в любом случае.
  var gate = document.createElement("style");
  gate.id = "x4gate";
  gate.textContent = "html{background:#050405!important}body{visibility:hidden!important}";
  // В момент старта документа корня ещё нет: вставлять раньше него нельзя, страница останется пустой.
  function whenRoot(fn) {
    if (document.documentElement) { fn(); return; }
    var o = new MutationObserver(function () { if (document.documentElement) { o.disconnect(); fn(); } });
    o.observe(document, { childList: true });
  }
  whenRoot(function () { if (gate) (document.head || document.documentElement).appendChild(gate); });
  function ready() { if (gate) { gate.remove(); gate = null; } }
  setTimeout(ready, isWeb ? 1500 : 700);
  if (isWeb) whenRoot(function () { document.documentElement.classList.add("x4-web"); });
  var done = {};
  var RE = /#[0-9a-fA-F]{3,8}\b|rgba?\(\s*[\d.]+[ ,]+[\d.]+[ ,]+[\d.]+(?:[ ,/]+[\d.]+%?)?\s*\)/g;
  var cache = {};
  var undo = [];
  var seen = typeof WeakSet === "function" ? new WeakSet() : null;

  function parse(c) {
    if (c[0] === "#") {
      var h = c.slice(1);
      if (h.length === 3 || h.length === 4) h = h.split("").map(function (x) { return x + x; }).join("");
      if (h.length !== 6 && h.length !== 8) return null;
      return [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16), h.length === 8 ? parseInt(h.slice(6, 8), 16) / 255 : 1];
    }
    var n = c.match(/[\d.]+%?/g);
    if (!n || n.length < 3) return null;
    var a = n[3] === undefined ? 1 : (/%$/.test(n[3]) ? parseFloat(n[3]) / 100 : +n[3]);
    return [+n[0], +n[1], +n[2], a];
  }
  function toHsl(r, g, b) {
    r /= 255; g /= 255; b /= 255;
    var M = Math.max(r, g, b), m = Math.min(r, g, b), l = (M + m) / 2, d = M - m, h = 0, s = 0;
    if (d) {
      s = d / (1 - Math.abs(2 * l - 1));
      h = M === r ? ((g - b) / d) % 6 : M === g ? (b - r) / d + 2 : (r - g) / d + 4;
      h = (h * 60 + 360) % 360;
    }
    return [h, s, l];
  }
  function fromHsl(h, s, l) {
    var c = (1 - Math.abs(2 * l - 1)) * s, x = c * (1 - Math.abs((h / 60) % 2 - 1)), m = l - c / 2, r = 0, g = 0, b = 0;
    if (h < 60) { r = c; g = x; } else if (h < 120) { r = x; g = c; } else if (h < 180) { g = c; b = x; }
    else if (h < 240) { g = x; b = c; } else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
    return [Math.round((r + m) * 255), Math.round((g + m) * 255), Math.round((b + m) * 255)];
  }
  function map(c, isText) {
    var key = (isText ? "t" : "b") + c;
    if (cache[key] !== undefined) return cache[key];
    var p = parse(c), out = c;
    if (p) {
      var t = toHsl(p[0], p[1], p[2]), h = t[0], s = t[1], l = t[2], n = null;
      // зелёный клиента остаётся статусом «в игре», а заливки и кнопки становятся багровыми
      if (s >= 0.3 && h >= 70 && h <= 160 && (!isText || isWeb)) {
        n = isText ? fromHsl(36, 0.24, 0.86) : fromHsl(349, Math.min(0.8, s), Math.min(0.42, Math.max(0.2, l * 0.8)));
      } else if (s >= 0.04 && h >= 170 && h <= 275) {
        if (l >= 0.8) n = fromHsl(36, 0.24, Math.min(0.9, l));
        else if (s >= 0.45) n = isText ? fromHsl(349, 0.68, 0.66) : l < 0.26 ? fromHsl(345, 0.08, l * 0.45) : fromHsl(349, Math.min(0.8, s), Math.min(0.5, l * 0.82));
        else n = fromHsl(340, Math.min(0.1, s * 0.35), l < 0.35 ? l * 0.62 : l * 0.95);
      }
      if (n) out = p[3] === 1 ? "rgb(" + n.join(",") + ")" : "rgba(" + n.join(",") + "," + p[3] + ")";
    }
    cache[key] = out;
    return out;
  }
  function walk(rules) {
    for (var i = 0; i < rules.length; i++) {
      var r = rules[i];
      if (r.cssRules && r.cssRules.length) walk(r.cssRules);
      if (!r.style) continue;
      for (var j = 0; j < r.style.length; j++) {
        var k = r.style[j], v = r.style.getPropertyValue(k);
        if (!v || !(v.indexOf("#") >= 0 || v.indexOf("rgb") >= 0)) continue;
        if (v.indexOf("url(") >= 0) continue;
        var txt = k === "color" || k === "fill" || /color$/.test(k) && k.indexOf("--") === 0 && /text|fg|font/i.test(k);
        var w = v.replace(RE, function (c) { return map(c, txt); });
        if (w === v) continue;
        var prio = r.style.getPropertyPriority(k);
        undo.push([r, k, v, prio]);
        r.style.setProperty(k, w, prio);
      }
    }
  }
  var PAINT = /^(color|fill|stroke|caret-color|accent-color|background(-color|-image)?|border(-(top|right|bottom|left))?(-color)?|outline(-color)?|box-shadow|text-shadow|text-decoration-color)$/;
  function closeBrace(css, from) {
    var d = 1, j = from;
    while (j < css.length && d) { var c = css[j++]; if (c === "{") d++; else if (c === "}") d--; }
    return j;
  }
  function splitDecls(body) {
    var out = [], d = 0, q = "", st = 0;
    for (var i = 0; i < body.length; i++) {
      var c = body[i];
      if (q) { if (c === q && body[i - 1] !== "\\") q = ""; continue; }
      if (c === '"' || c === "'") q = c;
      else if (c === "(") d++;
      else if (c === ")") d--;
      else if (c === ";" && !d) { out.push(body.slice(st, i)); st = i + 1; }
    }
    out.push(body.slice(st));
    return out;
  }
  // Все правила оформления в исходном порядке: каскад для цвета и фона остаётся таким же, как у Steam.
  var paintedHeads = {};
  function paintRules(css) {
    var out = "", i = 0;
    while (i < css.length) {
      var a = css.indexOf("{", i);
      if (a < 0) break;
      var semi = css.indexOf(";", i);
      if (css[i] === "@" && semi >= 0 && semi < a) { i = semi + 1; continue; }
      var head = css.slice(i, a).trim(), end = closeBrace(css, a + 1), body = css.slice(a + 1, end - 1);
      i = end;
      if (/^@(media|supports|container)/.test(head)) {
        var inner = paintRules(body);
        if (inner) out += head + "{" + inner + "}";
      } else if (/^@(-webkit-)?keyframes/.test(head)) {
        out += head + "{" + body.replace(RE, function (c) { return map(c, false); }) + "}";
      } else if (head[0] !== "@") {
        var kept = "";
        splitDecls(body).forEach(function (d) {
          var k = d.indexOf(":");
          if (k < 0) return;
          var name = d.slice(0, k).trim().toLowerCase(), v = d.slice(k + 1);
          var custom = name.indexOf("--") === 0;
          if (custom ? !(v.indexOf("#") >= 0 || v.indexOf("rgb") >= 0) : !PAINT.test(name)) return;
          // встроенные картинки занимают мегабайты, а цвета в них нет: оставляем только там, где правило их перекрывает
          if (v.indexOf("url(data:") >= 0 || v.indexOf('url("data:') >= 0) { if (!paintedHeads[head]) return; }
          else paintedHeads[head] = 1;
          var txt = name === "color" || name === "fill";
          kept += name + ":" + (v.indexOf("url(") >= 0 ? v : v.replace(RE, function (c) { return map(c, txt); })) + ";";
        });
        if (kept) out += head + "{" + kept + "}";
      }
    }
    return out;
  }
  function absoluteUrls(href, text) {
    return text.replace(/\/\*[\s\S]*?\*\//g, "").replace(/url\(\s*(['"]?)(?!data:|https?:|#)([^'")]+)\1\s*\)/g, function (_, q, u) {
      try { return 'url("' + new URL(u, href).href + '")'; } catch (e) { return _; }
    });
  }
  // Готовый текст один на все окна: он хранится в общем окне Steam.
  var shared = window;
  try { if (window.opener && window.opener.document) shared = window.opener; } catch (e) {}
  var inflight = 0;
  function overlay(href) {
    var all = shared.__x4over || (shared.__x4over = {});
    if (!all[href]) all[href] = fetch(href).then(function (r) { return r.text(); }).then(function (t) { return paintRules(absoluteUrls(href, t)); });
    return all[href];
  }
  function paintLinks() {
    [].forEach.call(document.querySelectorAll("link[rel=stylesheet]"), function (l) {
      if (l.__x4p || !l.href) return;
      l.__x4p = 1;
      inflight++;
      overlay(l.href).then(function (css) {
        var st = document.createElement("style");
        st.setAttribute("data-x4copy", "1");
        st.textContent = css;
        if (l.parentNode) l.parentNode.insertBefore(st, l.nextSibling);
      }).catch(function () {}).then(function () { inflight--; later(); });
    });
  }
  function run() {
    if (!isWeb) paintLinks();
    var list = document.styleSheets;
    for (var i = 0; i < list.length; i++) {
      var s = list[i];
      if (!isWeb && s.href) continue;
      if (seen.has(s) || (s.ownerNode && (s.ownerNode.id === "x4bloodmoon" || s.ownerNode.getAttribute("data-x4copy")))) continue;
      var rules;
      try { rules = s.cssRules; } catch (e) { continue; }
      if (!rules) continue;
      seen.add(s);
      walk(rules);
    }
    swapKnown();
    var wait = false;
    [].forEach.call(document.querySelectorAll("link[rel=stylesheet]"), function (l) {
      if (!l.sheet && l.media !== "not all") wait = true;
    });
    if (isWeb) markNav();
    if (!wait && !inflight && document.readyState !== "loading" && (!isWeb || !pending().length)) ready();
  }
  // Верхнее меню магазина: классы у него хешированные, поэтому находим его по форме поиска.
  function markNav() {
    var f = document.querySelector("form input");
    f = f && f.closest("form");
    if (!f || f.getBoundingClientRect().top > 60) return;
    var e = f;
    for (var i = 0; e && i < 4; i++) e = e.parentElement;
    if (e && e !== document.body) e.setAttribute("data-x4nav", "1");
  }
  // Стили с чужого домена читать нельзя: подключатель скачивает их сам и передаёт текст сюда.
  function pending() {
    var out = [];
    [].forEach.call(document.styleSheets, function (s) {
      if (!s.href || done[s.href]) return;
      try { if (s.cssRules) return; } catch (e) {}
      out.push(s.href);
    });
    return out;
  }
  // Копия чужого стиля с заменёнными цветами встаёт на место оригинала: порядок правил Steam не меняется.
  function rewrite(href, text) {
    var base = href;
    return text
      .replace(/@import[^;]+;/g, "")
      .replace(/url\(\s*(['"]?)(?!data:|https?:|#)([^'")]+)\1\s*\)/g, function (_, q, u) {
        try { return 'url("' + new URL(u, base).href + '")'; } catch (e) { return _; }
      })
      .replace(/([\w-]+)\s*:\s*([^;{}]*)/g, function (m, k, v) {
        if (v.indexOf("url(") >= 0 || !(v.indexOf("#") >= 0 || v.indexOf("rgb") >= 0)) return m;
        var txt = k === "color" || k === "fill";
        return k + ":" + v.replace(RE, function (c) { return map(c, txt); });
      });
  }
  function swapLink(link, css) {
    if (link.getAttribute("data-x4off")) return;
    var st = document.createElement("style");
    st.setAttribute("data-x4copy", "1");
    st.textContent = css;
    link.parentNode.insertBefore(st, link.nextSibling);
    link.setAttribute("data-x4off", link.media || "all");
    link.media = "not all";
  }
  function swap(href, css) {
    done[href] = 1;
    [].forEach.call(document.querySelectorAll("link[rel=stylesheet]"), function (l) {
      if (l.href === href) swapLink(l, css);
    });
    later();
  }
  // Подключатель заранее кладёт сюда перекрашенные копии: подмена происходит до загрузки оригинала.
  function swapKnown() {
    var c = window.__x4c;
    if (!c || !document.head) return;
    [].forEach.call(document.querySelectorAll("link[rel=stylesheet]"), function (l) {
      if (c[l.href]) { done[l.href] = 1; swapLink(l, c[l.href]); }
    });
  }
  function addText(href, text) {
    var css = rewrite(href, text);
    swap(href, css);
    return css;
  }
  function addRaw(href, css) { swap(href, css); }
  // Аватарка своего аккаунта крупнее родной: берём полноразмерную картинку, CSS её подставит.
  function avatar() {
    var img = document.querySelector(".avatarHolder.X-Small img.avatar, .currentUserAvatar img.avatar");
    if (!img || !img.src) return;
    var full = img.src.replace(/(_medium|_full)?\.jpg.*$/, "_full.jpg");
    document.documentElement.style.setProperty("--x4-me", 'url("' + full + '")');
    // обёртки блока аккаунта растягиваем под большую аватарку, иначе клик по ней не попадает
    var e = img.closest(".avatarHolder.X-Small");
    for (var i = 0; e && i < 4; i++) {
      e = e.parentElement;
      if (e) e.setAttribute("data-x4-acc", "1");
      if (e && e.classList.contains("tool-tip-source")) break;
    }
  }
  if (!isWeb) { avatar(); setInterval(avatar, 5000); }
  // Big Picture убран из меню Steam. Меню это отдельные окна, поэтому следим только за ними.
  function hideBigPicture() {
    [].forEach.call(document.querySelectorAll(".contextMenuItem"), function (n) {
      if (/Big Picture/i.test(n.textContent)) n.style.display = "none";
    });
  }
  // Скрытые окна меню почти не получают таймеров, поэтому проверяем сразу и в момент показа.
  function watchMenus() {
    if (!document.body) return false;
    hideBigPicture();
    // меню это маленькие окна; в главном и настройках искать незачем, там всё время идут изменения
    new MutationObserver(function () { if (window.innerWidth <= 700) hideBigPicture(); })
      .observe(document.body, { childList: true, subtree: true });
    document.addEventListener("visibilitychange", hideBigPicture);
    return true;
  }
  if (!isWeb && !watchMenus()) {
    var menuWait = setInterval(function () { if (watchMenus()) clearInterval(menuWait); }, 100);
  }
  var timer = 0;
  function later() { clearTimeout(timer); timer = setTimeout(run, 150); }
  var obs = new MutationObserver(function (ms) {
    ms.forEach(function (m) {
      [].forEach.call(m.addedNodes, function (n) { if (n.tagName === "LINK") n.addEventListener("load", later); });
    });
    swapKnown();
    var t = document.getElementById("x4bloodmoon");
    if (t && document.head && t !== document.head.lastElementChild) document.head.appendChild(t);
    later();
  });
  function watchHead() { if (document.head) { obs.observe(document.head, { childList: true }); return true; } return false; }
  if (!watchHead()) {
    var rootObs = new MutationObserver(function () { if (watchHead()) { rootObs.disconnect(); run(); } });
    rootObs.observe(document, { childList: true, subtree: true });
  }
  document.addEventListener("DOMContentLoaded", later);
  // Стили, вставленные до нас, сигнала о добавлении не дадут: пока шторка закрыта, проверяем их часто.
  var poll = setInterval(function () { run(); if (!gate) clearInterval(poll); }, 40);
  window.__x4recolor = {
    run: run,
    pending: pending,
    addText: addText,
    addRaw: addRaw,
    revert: function () {
      obs.disconnect();
      ready();
      undo.forEach(function (u) { try { u[0].style.setProperty(u[1], u[2], u[3]); } catch (e) {} });
      undo = [];
      try { delete shared.__x4over; } catch (e) {}
      [].forEach.call(document.querySelectorAll("style[data-x4copy]"), function (n) { n.remove(); });
      [].forEach.call(document.querySelectorAll("[data-x4off]"), function (n) { n.media = n.getAttribute("data-x4off"); n.removeAttribute("data-x4off"); });
      done = {};
      delete window.__x4recolor;
    }
  };
  run();
})();
