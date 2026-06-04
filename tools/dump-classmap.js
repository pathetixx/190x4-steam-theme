// ============================================================================
// 190x4 — дамп реестра классов Steam
// Скопируй ВЕСЬ этот файл в консоль DevTools главного окна Steam (Millennium →
// developer mode → правый клик → Inspect → вкладка Console) и нажми Enter.
// Он выгрузит {ИмяКласса: текущийХеш} в буфер и в консоль.
// Вставь результат в файл classmap.json и пришли — по нему я пропишу точные
// цели в client/resolver.js под твою сборку.
//
// Если тема уже установлена, можно просто выполнить:  x4.dump()
// ============================================================================
(() => {
  const list = [];
  const wp = window.webpackChunksteamui;
  if (!wp) { console.warn("webpackChunksteamui не найден — выполни в ГЛАВНОМ окне Steam"); return; }
  let req;
  wp.push([[Symbol("190x4-dump")], {}, (r) => { req = r; }]);
  const seen = new Set();
  for (const id of Object.keys(req.m)) {
    let mod; try { mod = req(id); } catch { continue; }
    if (!mod) continue;
    for (const m of [mod.default, mod]) {
      if (typeof m !== "object" || m === null || m.__esModule || seen.has(m)) continue;
      const keys = Object.keys(m);
      if (!keys.length) continue;
      if (keys.length === 1 && m.version) continue;
      if (keys.length > 1000 && m.AboutSettings) continue;
      if (keys.every((k) => typeof m[k] === "string" && !Object.getOwnPropertyDescriptor(m, k)?.get)) {
        seen.add(m); list.push(m);
      }
    }
  }
  const map = Object.assign({}, ...list);
  const json = JSON.stringify(map, null, 2);
  try { copy(json); console.log("Скопировано в буфер."); } catch {}
  console.log("190x4 · классов в реестре:", Object.keys(map).length);
  return map;
})();
