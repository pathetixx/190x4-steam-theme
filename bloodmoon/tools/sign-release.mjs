// Подписывает exe и пишет latest.json. Использование:
// node sign-release.mjs <exe> <версия> <url exe> <файл закрытого ключа PEM> > latest.json
import crypto from "node:crypto";
import fs from "node:fs";
const [exe, version, url, keyFile] = process.argv.slice(2);
const data = fs.readFileSync(exe);
const key = crypto.createPrivateKey(fs.readFileSync(keyFile));
const signature = crypto.sign("sha256", data, { key, dsaEncoding: "ieee-p1363" }).toString("base64");
const sha256 = crypto.createHash("sha256").update(data).digest("hex");
console.log(JSON.stringify({ version, url, sha256, signature }, null, 2));
