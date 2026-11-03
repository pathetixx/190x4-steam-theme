// Runs in SharedJSContext. Maps stable class names from Steam's webpack CSS modules to the
// hashes of the current build. Token: "Name" or "Name@Sibling" (module must also export Sibling).
window.__x4resolve = function (tokens) {
  var req;
  webpackChunksteamui.push([[Symbol()], {}, function (r) { req = r; }]);
  webpackChunksteamui.pop();
  var mods = [];
  Object.keys(req.m).forEach(function (id) {
    var m;
    try { m = req(id); } catch (e) { return; }
    if (!m || typeof m !== "object") return;
    var ks = Object.keys(m);
    if (ks.length < 2 || ks.length > 800) return;
    if (ks.every(function (k) { return typeof m[k] === "string"; })) mods.push(m);
  });
  var out = {};
  tokens.forEach(function (t) {
    var p = t.split("@"), name = p[0], sib = p[1], seen = {};
    mods.forEach(function (m) {
      if (typeof m[name] !== "string" || (sib && typeof m[sib] !== "string")) return;
      seen[m[name].split(" ")[0]] = 1;
    });
    out[t] = Object.keys(seen);
  });
  return JSON.stringify(out);
};
