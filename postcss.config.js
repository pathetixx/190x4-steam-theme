import postcssSassPlugin from "@csstools/postcss-sass";
import removeEmpty from "postcss-discard-empty";
import postcssSassParser from "postcss-scss";
import {
	appendImportantPlugin,
	selectorReplacerPlugin,
} from "steam-theming-utils/postcss-plugins";

// Скоуп клиентских правил под body.DesktopUI: главное окно клиента имеет класс
// DesktopUI на <body>, а окно друзей (.friendsui-container) и попап-окна — нет.
// Префикс гарантирует, что клиентские правила (в т.ч. деструктивные вроде лого
// #SteamButton{display:none у детей}) НЕ протекают в чужие окна, даже если
// Millennium инжектит CSS туда. Надёжнее скоупа на уровне патча (MatchRegex).
const scopeClientPlugin = () => ({
	postcssPlugin: "x4-scope-client",
	Once(root) {
		root.walkRules((rule) => {
			const p = rule.parent;
			if (p && p.type === "atrule" && /keyframes/i.test(p.name)) return;
			rule.selectors = rule.selectors.map((sel) => {
				const s = sel.trim();
				if (!s || s.includes("body.DesktopUI")) return s;
				if (s.startsWith(":root")) return s.replace(/^:root/, "body.DesktopUI");
				if (/^html\b/.test(s)) return "body.DesktopUI" + s.replace(/^html\b/, "");
				if (/^body\b/.test(s)) return "body.DesktopUI" + s.replace(/^body\b/, "");
				return "body.DesktopUI " + s;
			});
		});
	},
});
scopeClientPlugin.postcss = true;

/** @type {import("postcss-load-config").Config} */
export default {
	map: false,
	parser: postcssSassParser,
	plugins: [
		postcssSassPlugin({
			includePaths: ["src"],
			silenceDeprecations: ["legacy-js-api"],
		}),
		selectorReplacerPlugin(),
		scopeClientPlugin(),
		appendImportantPlugin({ filter: [/^body\.DesktopUI$/] }),
		removeEmpty(),
	],
};
