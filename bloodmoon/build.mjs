// Builds out/*.css: inlines fonts and images from src/*.css as data URIs.
// Class placeholders {{Name}} stay in place: the loader resolves them on the live client.
import fs from "node:fs";
import path from "node:path";
const root = path.dirname(new URL(import.meta.url).pathname);
const mime = { ".webp": "image/webp", ".png": "image/png", ".jpg": "image/jpeg", ".woff2": "font/woff2", ".svg": "image/svg+xml" };
const inline = (rel) => `url("data:${mime[path.extname(rel)]};base64,${fs.readFileSync(path.join(root, rel)).toString("base64")}")`;
fs.mkdirSync(path.join(root, "out"), { recursive: true });
const fonts = fs.readFileSync(path.join(root, "src/_fonts.css"), "utf8");
for (const f of fs.readdirSync(path.join(root, "src"))) {
  if (f.startsWith("_") || !f.endsWith(".css")) continue;
  let css = fonts + fs.readFileSync(path.join(root, "src", f), "utf8");
  // каждая картинка лежит в CSS один раз, остальные места ссылаются на неё через переменную
  const vars = new Map();
  css = css.replace(/url\(assets\/([^)]+)\)/g, (_, n) => {
    const v = "--x4a-" + n.replace(/\W/g, "-");
    vars.set(v, inline(`assets/${n}`));
    return `var(${v})`;
  });
  css = css.replace(/url\(fonts\/([^)]+)\)/g, (_, n) => inline(`fonts/${n}`));
  css = `:root{${[...vars].map(([k, v]) => `${k}:${v}`).join(";")}}\n` + css;
  fs.writeFileSync(path.join(root, "out", f), css);
  console.log(f, (css.length / 1024).toFixed(0) + " KB");
}
for (const f of ["resolver.js", "recolor.js", "x4-steam-bloodmoon.ps1"]) fs.copyFileSync(path.join(root, "loader", f), path.join(root, "out", f));
