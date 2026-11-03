using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Bloodmoon
{
    // Те же правила перекраски, что и в recolor.js, для стилей сайтов Steam.
    static class Recolor
    {
        static readonly Regex Color = new Regex(@"#[0-9a-fA-F]{3,8}\b|rgba?\(\s*[\d.]+[ ,]+[\d.]+[ ,]+[\d.]+(?:[ ,/]+[\d.]+%?)?\s*\)", RegexOptions.Compiled);
        static readonly Regex Decl = new Regex(@"([\w-]+)\s*:\s*([^;{}]*)", RegexOptions.Compiled);
        static readonly Regex Url = new Regex(@"url\(\s*(['""]?)(?!data:|https?:|#)([^'"")]+)\1\s*\)", RegexOptions.Compiled);
        static readonly Regex Import = new Regex(@"@import[^;]+;", RegexOptions.Compiled);
        static readonly ConcurrentDictionary<string, string> Cache = new ConcurrentDictionary<string, string>();

        public static string Sheet(string href, string css)
        {
            var baseUri = new Uri(href);
            css = Import.Replace(css, "");
            css = Url.Replace(css, m =>
            {
                Uri abs;
                return Uri.TryCreate(baseUri, m.Groups[2].Value, out abs) ? "url(\"" + abs.AbsoluteUri + "\")" : m.Value;
            });
            return Decl.Replace(css, m =>
            {
                var v = m.Groups[2].Value;
                if (v.Contains("url(") || !(v.Contains("#") || v.Contains("rgb"))) return m.Value;
                var name = m.Groups[1].Value;
                var isText = name == "color" || name == "fill";
                return name + ":" + Color.Replace(v, c => Map(c.Value, isText));
            });
        }

        static string Map(string c, bool isText)
        {
            return Cache.GetOrAdd((isText ? "t" : "b") + c, _ => Compute(c, isText));
        }

        static string Compute(string c, bool isText)
        {
            double r, g, b, a;
            if (!Parse(c, out r, out g, out b, out a)) return c;
            double h, s, l;
            ToHsl(r, g, b, out h, out s, out l);
            int[] n = null;
            if (s >= 0.3 && h >= 70 && h <= 160)
                n = isText ? FromHsl(36, 0.24, 0.86) : FromHsl(349, Math.Min(0.8, s), Math.Min(0.42, Math.Max(0.2, l * 0.8)));
            else if (s >= 0.04 && h >= 170 && h <= 275)
            {
                if (l >= 0.8) n = FromHsl(36, 0.24, Math.Min(0.9, l));
                else if (s >= 0.45) n = isText ? FromHsl(349, 0.68, 0.66) : l < 0.26 ? FromHsl(345, 0.08, l * 0.45) : FromHsl(349, Math.Min(0.8, s), Math.Min(0.5, l * 0.82));
                else n = FromHsl(340, Math.Min(0.1, s * 0.35), l < 0.35 ? l * 0.62 : l * 0.95);
            }
            if (n == null) return c;
            var rgb = n[0] + "," + n[1] + "," + n[2];
            return a >= 1 ? "rgb(" + rgb + ")" : "rgba(" + rgb + "," + a.ToString(CultureInfo.InvariantCulture) + ")";
        }

        static bool Parse(string c, out double r, out double g, out double b, out double a)
        {
            r = g = b = 0; a = 1;
            if (c[0] == '#')
            {
                var h = c.Substring(1);
                if (h.Length == 3 || h.Length == 4)
                {
                    var e = "";
                    foreach (var ch in h) e += new string(ch, 2);
                    h = e;
                }
                if (h.Length != 6 && h.Length != 8) return false;
                r = Convert.ToInt32(h.Substring(0, 2), 16);
                g = Convert.ToInt32(h.Substring(2, 2), 16);
                b = Convert.ToInt32(h.Substring(4, 2), 16);
                if (h.Length == 8) a = Convert.ToInt32(h.Substring(6, 2), 16) / 255.0;
                return true;
            }
            var m = Regex.Matches(c, @"[\d.]+%?");
            if (m.Count < 3) return false;
            var inv = CultureInfo.InvariantCulture;
            if (!double.TryParse(m[0].Value, NumberStyles.Float, inv, out r) || !double.TryParse(m[1].Value, NumberStyles.Float, inv, out g) || !double.TryParse(m[2].Value, NumberStyles.Float, inv, out b)) return false;
            if (m.Count > 3)
            {
                var t = m[3].Value;
                if (!double.TryParse(t.TrimEnd('%'), NumberStyles.Float, inv, out a)) return false;
                if (t.EndsWith("%")) a /= 100;
            }
            return true;
        }

        static void ToHsl(double r, double g, double b, out double h, out double s, out double l)
        {
            r /= 255; g /= 255; b /= 255;
            double M = Math.Max(r, Math.Max(g, b)), m = Math.Min(r, Math.Min(g, b)), d = M - m;
            l = (M + m) / 2; h = 0; s = 0;
            if (d > 0)
            {
                s = d / (1 - Math.Abs(2 * l - 1));
                h = M == r ? ((g - b) / d) % 6 : M == g ? (b - r) / d + 2 : (r - g) / d + 4;
                h = (h * 60 + 360) % 360;
            }
        }

        static int[] FromHsl(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s, x = c * (1 - Math.Abs((h / 60) % 2 - 1)), m = l - c / 2, r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; } else if (h < 120) { r = x; g = c; } else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; } else if (h < 300) { r = x; b = c; } else { r = c; b = x; }
            return new[] { (int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255) };
        }
    }
}
