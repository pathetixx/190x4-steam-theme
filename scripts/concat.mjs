import fs from "node:fs";
import path from "node:path";

const dir = "dist/client";
const out = "client/theme.css";
const header = `/* 190x4 Steam theme - GENERATED from src/client/*.scss via steam-theming-utils.
   DO NOT EDIT directly; edit the SCSS sources and run "npm run build". */\n`;
const strip = (s) => s.replace(/^@charset[^;]*;\s*/gim, "").trim();
const files = fs.existsSync(dir) ? fs.readdirSync(dir).filter((f) => f.endsWith(".css")).sort() : [];
const body = files.map((f) => `/* == ${f} == */\n` + strip(fs.readFileSync(path.join(dir, f), "utf8"))).join("\n\n");
fs.writeFileSync(out, header + "\n" + body + "\n");
console.log(`concat: ${files.length} files -> ${out} (${header.length + body.length} bytes)`);
