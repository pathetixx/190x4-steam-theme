using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bloodmoon
{
    // Отдаёт страницам Steam стили темы и перекрашенные копии стилей сайта. Слушает только 127.0.0.1.
    sealed class LocalServer
    {
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);
        readonly TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        readonly byte[] webCss;
        readonly string cacheDir;
        readonly SemaphoreSlim downloads = new SemaphoreSlim(4);

        public int Port { get; private set; }

        public LocalServer(string webCss)
        {
            this.webCss = Utf8.GetBytes(webCss);
            // копии зависят от правил перекраски, поэтому у каждой версии программы своя папка
            cacheDir = Path.Combine(Paths.CacheDir, Paths.CurrentVersion.ToString());
            Directory.CreateDirectory(cacheDir);
            foreach (var old in Directory.GetDirectories(Paths.CacheDir).Where(d => !string.Equals(d, cacheDir, StringComparison.OrdinalIgnoreCase)))
                try { Directory.Delete(old, true); } catch { }
            foreach (var f in new DirectoryInfo(cacheDir).GetFiles().Where(f => f.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-30)))
                try { f.Delete(); } catch { }
        }

        public void Start()
        {
            listener.Start();
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task.Run(async () =>
            {
                while (true)
                {
                    TcpClient c;
                    try { c = await listener.AcceptTcpClientAsync().ConfigureAwait(false); }
                    catch { return; }
                    var _ = Task.Run(() => Handle(c));
                }
            });
        }

        async Task Handle(TcpClient c)
        {
            using (c)
            {
                try
                {
                    var s = c.GetStream();
                    var head = await ReadHead(s).ConfigureAwait(false);
                    if (head == null) return;
                    var parts = head.Split(' ');
                    if (parts.Length < 2) return;
                    if (parts[0] == "OPTIONS") { await Send(s, 204, new byte[0]).ConfigureAwait(false); return; }
                    var target = parts[1];
                    if (target == "/web.css") { await Send(s, 200, webCss).ConfigureAwait(false); return; }
                    if (target.StartsWith("/css?u="))
                    {
                        var body = await Copy(Uri.UnescapeDataString(target.Substring(7))).ConfigureAwait(false);
                        await Send(s, body == null ? 404 : 200, body ?? new byte[0]).ConfigureAwait(false);
                        return;
                    }
                    await Send(s, 404, new byte[0]).ConfigureAwait(false);
                }
                catch (Exception e) { Paths.Log("server: " + e.Message); }
            }
        }

        static async Task<string> ReadHead(NetworkStream s)
        {
            var buf = new byte[8192];
            var n = 0;
            while (n < buf.Length)
            {
                var r = await s.ReadAsync(buf, n, buf.Length - n).ConfigureAwait(false);
                if (r <= 0) return null;
                n += r;
                var t = Encoding.ASCII.GetString(buf, 0, n);
                if (t.Contains("\r\n\r\n")) return t.Substring(0, t.IndexOf('\r'));
            }
            return null;
        }

        static async Task Send(NetworkStream s, int code, byte[] body)
        {
            var head = "HTTP/1.1 " + code + (code < 300 ? " OK" : " Not Found") +
                "\r\nContent-Type: text/css; charset=utf-8\r\nContent-Length: " + body.Length +
                "\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Private-Network: true\r\nConnection: close\r\n\r\n";
            var h = Encoding.ASCII.GetBytes(head);
            await s.WriteAsync(h, 0, h.Length).ConfigureAwait(false);
            if (body.Length > 0) await s.WriteAsync(body, 0, body.Length).ConfigureAwait(false);
        }

        // Качаем только стили с адресов Steam: сервер не должен работать прокси для чужих ссылок.
        static bool Allowed(Uri u)
        {
            if (u.Scheme != "https") return false;
            var h = u.Host.ToLowerInvariant();
            return h.EndsWith(".steamstatic.com") || h == "store.steampowered.com" || h == "steamcommunity.com" || h.EndsWith(".steampowered.com") || h.EndsWith(".steamcommunity.com");
        }

        async Task<byte[]> Copy(string href)
        {
            Uri u;
            if (!Uri.TryCreate(href, UriKind.Absolute, out u) || !Allowed(u)) return null;
            string name;
            using (var sha = SHA1.Create())
                name = BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(href))).Replace("-", "") + ".css";
            var path = Path.Combine(cacheDir, name);
            if (File.Exists(path)) return File.ReadAllBytes(path);
            await downloads.WaitAsync().ConfigureAwait(false);
            try
            {
                if (File.Exists(path)) return File.ReadAllBytes(path);
                string css;
                using (var w = new WebClient { Encoding = Utf8 })
                {
                    // у WebClient нет своего срока ожидания: зависшая загрузка не должна держать страницу и очередь
                    var job = w.DownloadStringTaskAsync(u);
                    if (await Task.WhenAny(job, Task.Delay(15000)).ConfigureAwait(false) != job)
                    {
                        w.CancelAsync();
                        throw new TimeoutException("нет ответа 15 с");
                    }
                    css = await job.ConfigureAwait(false);
                }
                if (string.IsNullOrWhiteSpace(css)) throw new InvalidDataException("пустой ответ");
                var body = Utf8.GetBytes(Recolor.Sheet(href, css));
                Paths.Log("css ok " + u.Host + " " + body.Length);
                // кеш только ускоряет: если записать не вышло, стиль всё равно отдаём
                try
                {
                    Directory.CreateDirectory(cacheDir);
                    var tmp = path + ".tmp";
                    File.WriteAllBytes(tmp, body);
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tmp, path);
                }
                catch (Exception e) { Paths.Log("cache: " + e.Message); }
                return body;
            }
            catch (Exception e) { Paths.Log("css " + u.Host + ": " + e.Message); return null; }
            finally { downloads.Release(); }
        }
    }
}
