import postcssSassPlugin from "@csstools/postcss-sass";
import removeEmpty from "postcss-discard-empty";
import postcssSassParser from "postcss-scss";
import {
	appendImportantPlugin,
	selectorReplacerPlugin,
} from "steam-theming-utils/postcss-plugins";

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
		appendImportantPlugin({ filter: [/^(:where\()?:root/] }),
		removeEmpty(),
	],
};
