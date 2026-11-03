using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Bloodmoon
{
    // Подключается к порту отладки Steam и применяет тему к окнам. Новых окон ждёт по событиям, без опроса.
    sealed class Agent
    {
        const int Port = 8080;
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        static readonly Regex Token = new Regex(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);
        readonly JavaScriptSerializer json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        readonly string clientRaw = Paths.ResourceText("client.css");
        readonly string resolver = Paths.ResourceText("resolver.js");
        readonly string recolor = Paths.ResourceText("recolor.js");
        readonly LocalServer server = new LocalServer(Paths.ResourceText("web.css"));
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        volatile bool enabled;
        volatile bool reset;

        const string PopupHook = @"(function(){
  if(window.__x4bmhooked)return;
  window.__x4bmhooked=1;
  g_PopupManager.AddPopupCreatedCallback(function(p){
    try{
      if(!window.__x4bm)return;
      // оверлей лежит поверх игры и должен оставаться прозрачным
      if(/^SP (?!Desktop)/.test(p.m_strName||''))return;
      var w=p.window||p.m_popup;
      if(w&&w.document)w.eval(window.__x4bm.code);
    }catch(e){}
  });
})();";
        const string Clear = "(function(){if(window.__x4recolor)window.__x4recolor.revert();return 1;})()";

        public Agent(bool enabled)
        {
            this.enabled = enabled;
            server.Start();
        }

        public void SetEnabled(bool on) { enabled = on; reset = true; }
        public void Stop() { stop.Cancel(); }

        public async Task Run()
        {
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    // пока Steam не запущен, просто спим: никаких подключений
                    if (Process.GetProcessesByName("steamwebhelper").Length == 0 || !enabled)
                    {
                        await Task.Delay(enabled ? 3000 : 1000, stop.Token).ConfigureAwait(false);
                        reset = false;
                        continue;
                    }
                    await Session().ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return; }
                catch (Exception e) { Paths.Log("session: " + e.GetType().Name + " " + e.Message); }
                try { await Task.Delay(1000, stop.Token).ConfigureAwait(false); } catch { return; }
            }
        }

        Dictionary<string, object> GetJson(string path)
        {
            var r = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + Port + path);
            r.Timeout = 3000; r.Proxy = null;
            using (var a = r.GetResponse())
            using (var s = new System.IO.StreamReader(a.GetResponseStream(), Utf8))
                return json.Deserialize<Dictionary<string, object>>(s.ReadToEnd());
        }

        sealed class Target { public string Id, Type, Title, Url; public bool Themed; public string Session; public DateTime Attached; public int Reloads; }

        async Task Session()
        {
            string wsUrl;
            try { wsUrl = (string)GetJson("/json/version")["webSocketDebuggerUrl"]; }
            catch (WebException) { await Task.Delay(1500, stop.Token).ConfigureAwait(false); return; }

            var targets = new Dictionary<string, Target>();
            string sharedId = null;
            bool sharedReady = false;
            using (var ws = new ClientWebSocket())
            {
                await ws.ConnectAsync(new Uri(wsUrl), stop.Token).ConfigureAwait(false);
                Paths.Log("connected");
                await Send(ws, new Dictionary<string, object> { { "id", 1 }, { "method", "Target.setDiscoverTargets" }, { "params", new Dictionary<string, object> { { "discover", true } } } }).ConfigureAwait(false);
                var msgId = 10;
                var buf = new byte[64 * 1024];
                Task<string> pending = Receive(ws, buf);
                var lastReady = DateTime.MinValue;
                while (!stop.IsCancellationRequested)
                {
                    if (reset || !enabled) { reset = false; if (!enabled) await ClearAll(targets.Values).ConfigureAwait(false); return; }
                    if (sharedId != null && !sharedReady && DateTime.UtcNow - lastReady > TimeSpan.FromSeconds(1))
                    {
                        lastReady = DateTime.UtcNow;
                        sharedReady = await PrepareShared(sharedId).ConfigureAwait(false);
                        if (sharedReady)
                            foreach (var c in targets.Values.Where(x => IsClient(x)).ToList())
                                await ThemeClient(c).ConfigureAwait(false);
                    }
                    // Запрет чужих стилей снимается только со следующей загрузки. Страница, открытая при запуске Steam,
                    // успевает начать загрузку раньше нас, поэтому её перезагружаем, пока на ней чёрная шторка.
                    foreach (var w in targets.Values.Where(x => x.Session != null && x.Reloads < 3 && DateTime.UtcNow - x.Attached < TimeSpan.FromSeconds(25)).ToList())
                    {
                        var blocked = await Evaluate(w.Id, "window.__x4blocked===1", 1000).ConfigureAwait(false);
                        if (blocked is bool && (bool)blocked)
                        {
                            w.Reloads++;
                            Paths.Log("reload blocked page");
                            await Send(ws, new Dictionary<string, object> { { "id", msgId++ }, { "method", "Page.reload" }, { "params", new Dictionary<string, object>() }, { "sessionId", w.Session } }).ConfigureAwait(false);
                        }
                    }
                    var done = await Task.WhenAny(pending, Task.Delay(300, stop.Token)).ConfigureAwait(false);
                    if (done != pending) continue;
                    var text = await pending.ConfigureAwait(false);
                    if (text == null) return;
                    pending = Receive(ws, buf);
                    // страницы шлют много своих событий: разбираем только нужные
                    if (text.IndexOf("\"Target.", StringComparison.Ordinal) < 0) continue;
                    var m = json.Deserialize<Dictionary<string, object>>(text);
                    object o;
                    if (!m.TryGetValue("method", out o)) continue;
                    var method = (string)o;
                    var p = (Dictionary<string, object>)m["params"];
                    if (method == "Target.targetDestroyed") { targets.Remove((string)p["targetId"]); continue; }
                    if (method == "Target.attachedToTarget")
                    {
                        var info = (Dictionary<string, object>)p["targetInfo"];
                        Target t2;
                        if (targets.TryGetValue((string)info["targetId"], out t2))
                        {
                            t2.Session = (string)p["sessionId"];
                            t2.Attached = DateTime.UtcNow;
                            msgId = await SetupWeb(ws, t2.Session, msgId).ConfigureAwait(false);
                        }
                        continue;
                    }
                    if (method != "Target.targetCreated" && method != "Target.targetInfoChanged") continue;
                    var ti = (Dictionary<string, object>)p["targetInfo"];
                    var id = (string)ti["targetId"];
                    Target t;
                    if (!targets.TryGetValue(id, out t)) targets[id] = t = new Target { Id = id };
                    t.Type = (string)ti["type"]; t.Title = (string)ti["title"]; t.Url = (string)ti["url"];
                    if (t.Type != "page") continue;
                    if (t.Title == "SharedJSContext" || t.Url.StartsWith("https://steamloopback.host/index.html"))
                    {
                        if (sharedId != id) { sharedId = id; sharedReady = false; foreach (var x in targets.Values) x.Themed = false; }
                        continue;
                    }
                    if (IsWeb(t))
                    {
                        if (t.Session == null && !t.Themed)
                        {
                            t.Themed = true;
                            await Send(ws, new Dictionary<string, object> { { "id", msgId++ }, { "method", "Target.attachToTarget" }, { "params", new Dictionary<string, object> { { "targetId", id }, { "flatten", true } } } }).ConfigureAwait(false);
                        }
                        continue;
                    }
                    // заголовок оверлея появляется позже создания окна: если тема успела встать, снимаем
                    if (t.Title.StartsWith("SP ") && !t.Title.StartsWith("SP Desktop"))
                    {
                        if (t.Themed) { t.Themed = false; await Evaluate(id, Clear, 3000).ConfigureAwait(false); }
                        continue;
                    }
                    if (sharedReady && IsClient(t)) await ThemeClient(t).ConfigureAwait(false);
                }
            }
        }

        static bool IsClient(Target t) { return t.Type == "page" && t.Url != null && t.Url.StartsWith("about:blank?") && (t.Title == null || !t.Title.StartsWith("SP ") || t.Title.StartsWith("SP Desktop")); }
        static bool IsWeb(Target t) { return t.Url != null && Regex.IsMatch(t.Url, @"^https://([a-z0-9-]+\.)?(steampowered|steamcommunity)\.com(/|$)"); }

        async Task ThemeClient(Target t)
        {
            if (t.Themed) return;
            t.Themed = true;
            // окно из перехвата уже с темой, повторный код только проверит это
            var ok = await Evaluate(t.Id, "(function(){var s=window.opener;if(!s||!s.__x4bm)return 0;eval(s.__x4bm.code);return 1;})()", 5000).ConfigureAwait(false);
            if (ok == null) t.Themed = false;
        }

        async Task ClearAll(IEnumerable<Target> list)
        {
            foreach (var t in list.ToList())
                if (t.Type == "page" && t.Themed && t.Session == null)
                    await Evaluate(t.Id, Clear, 3000).ConfigureAwait(false);
        }

        async Task<int> SetupWeb(ClientWebSocket ws, string session, int id)
        {
            var boot = "window.__x4srv='http://127.0.0.1:" + server.Port + "';" + recolor;
            Func<string, Dictionary<string, object>, Task> cmd = (m, p) =>
                Send(ws, new Dictionary<string, object> { { "id", id++ }, { "method", m }, { "params", p }, { "sessionId", session } });
            await cmd("Page.enable", new Dictionary<string, object>()).ConfigureAwait(false);
            await cmd("Page.setBypassCSP", new Dictionary<string, object> { { "enabled", true } }).ConfigureAwait(false);
            await cmd("Page.addScriptToEvaluateOnNewDocument", new Dictionary<string, object> { { "source", "if(window===top){" + boot + "}" } }).ConfigureAwait(false);
            await cmd("Runtime.evaluate", new Dictionary<string, object> { { "expression", boot } }).ConfigureAwait(false);
            return id;
        }

        async Task<bool> PrepareShared(string id)
        {
            var ready = await Evaluate(id, "typeof webpackChunksteamui!=='undefined'&&typeof g_PopupManager!=='undefined'", 3000).ConfigureAwait(false);
            if (!(ready is bool) || !(bool)ready) return false;
            var tokens = Token.Matches(clientRaw).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToArray();
            await Evaluate(id, resolver, 3000).ConfigureAwait(false);
            var res = await Evaluate(id, "__x4resolve(" + json.Serialize(tokens) + ")", 15000).ConfigureAwait(false) as string;
            if (res == null) return false;
            var map = json.Deserialize<Dictionary<string, object>>(res);
            var css = Token.Replace(clientRaw, m =>
            {
                object v;
                var h = map.TryGetValue(m.Groups[1].Value, out v) ? ((System.Collections.ArrayList)v).Cast<string>().ToList() : new List<string>();
                if (h.Count == 0) return ".x4-missing";
                return h.Count == 1 ? "." + h[0] : ":is(" + string.Join(",", h.Select(x => "." + x)) + ")";
            });
            var missing = tokens.Where(t => { object v; return !map.TryGetValue(t, out v) || ((System.Collections.ArrayList)v).Count == 0; }).ToArray();
            if (missing.Length > 0) Paths.Log("missing classes: " + string.Join(",", missing));
            var script = "if(window.__x4bm)URL.revokeObjectURL(window.__x4bm.cssUrl);window.__x4bm={cssUrl:URL.createObjectURL(new Blob([" + json.Serialize(css) + "],{type:'text/css'})),code:" + json.Serialize(recolor) + "};" + PopupHook + "1";
            var ok = await Evaluate(id, script, 8000).ConfigureAwait(false);
            Paths.Log("shared ready: " + (ok != null));
            return ok != null;
        }

        // Короткое подключение к одному окну: выполнить код и сразу отключиться. Время выполнения ограничено.
        async Task<object> Evaluate(string id, string expression, int timeoutMs)
        {
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
            using (var ws = new ClientWebSocket())
            {
                cts.CancelAfter(timeoutMs + 3000);
                try
                {
                    await ws.ConnectAsync(new Uri("ws://127.0.0.1:" + Port + "/devtools/page/" + id), cts.Token).ConfigureAwait(false);
                    await Send(ws, new Dictionary<string, object> { { "id", 1 }, { "method", "Runtime.evaluate" }, { "params", new Dictionary<string, object> { { "expression", expression }, { "returnByValue", true }, { "timeout", timeoutMs } } } }).ConfigureAwait(false);
                    var buf = new byte[64 * 1024];
                    while (true)
                    {
                        var t = await Receive(ws, buf, cts.Token).ConfigureAwait(false);
                        if (t == null) return null;
                        var m = json.Deserialize<Dictionary<string, object>>(t);
                        object mid;
                        if (!m.TryGetValue("id", out mid) || Convert.ToInt32(mid) != 1) continue;
                        object r;
                        if (!m.TryGetValue("result", out r)) return null;
                        var rd = (Dictionary<string, object>)r;
                        if (rd.ContainsKey("exceptionDetails")) { Paths.Log("eval failed in " + id.Substring(0, 6)); return null; }
                        var inner = (Dictionary<string, object>)rd["result"];
                        object v;
                        return inner.TryGetValue("value", out v) ? (v ?? (object)true) : true;
                    }
                }
                catch (OperationCanceledException) when (!stop.IsCancellationRequested) { return null; }
                catch (WebSocketException) { return null; }
            }
        }

        Task Send(ClientWebSocket ws, object msg)
        {
            var b = Utf8.GetBytes(json.Serialize(msg));
            return ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Text, true, stop.Token);
        }

        Task<string> Receive(ClientWebSocket ws, byte[] buf) { return Receive(ws, buf, stop.Token); }

        static async Task<string> Receive(ClientWebSocket ws, byte[] buf, CancellationToken ct)
        {
            var sb = new StringBuilder();
            while (true)
            {
                WebSocketReceiveResult r;
                try { r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct).ConfigureAwait(false); }
                catch (WebSocketException) { return null; }
                if (r.MessageType == WebSocketMessageType.Close) return null;
                sb.Append(Utf8.GetString(buf, 0, r.Count));
                if (r.EndOfMessage) return sb.ToString();
            }
        }
    }
}
