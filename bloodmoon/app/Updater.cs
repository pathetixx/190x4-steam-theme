using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Bloodmoon
{
    // Обновление: новый exe ставится, только если совпали SHA-256 и подпись ECDSA P-256 над всем файлом.
    static class Updater
    {
        const string PublicKey = "d741L+9j+I7JkPneifbcjKbFC3NNrWqhFxbW3BVEF+ezRwoOBkB+nbj50LGRr/rgvRGj7i4nofAhbzaKcfkNTA==";
        static readonly string[] Feeds =
        {
            "https://direct.190x4.pw/updates/bloodmoon/latest.json",
            "https://github.com/pathetixx/190x4-steam-theme/releases/download/bloodmoon-latest/latest.json"
        };

        // Для проверки на тестовой сборке. Подпись обязательна и здесь.
        public static readonly string TestFeed = Environment.GetEnvironmentVariable("BLOODMOON_TEST_FEED");

        public static async Task<string> Check(bool userAsked)
        {
            foreach (var feed in TestFeed != null ? new[] { TestFeed } : Feeds)
            {
                try
                {
                    string text;
                    using (var w = new WebClient { Proxy = WebRequest.DefaultWebProxy })
                        text = await w.DownloadStringTaskAsync(feed).ConfigureAwait(false);
                    var m = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(text);
                    var version = new Version((string)m["version"]);
                    if (version <= Paths.CurrentVersion) return "Установлена последняя версия";
                    await Apply((string)m["url"], (string)m["sha256"], (string)m["signature"]).ConfigureAwait(false);
                    return "Обновляю до " + version;
                }
                catch (Exception e) { Paths.Log("update " + feed + ": " + e.Message); }
            }
            return "Не удалось проверить обновления";
        }

        static async Task Apply(string url, string sha256, string signature)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "190x4-Bloodmoon-update.exe");
            using (var w = new WebClient())
                await w.DownloadFileTaskAsync(url, tmp).ConfigureAwait(false);
            var data = File.ReadAllBytes(tmp);
            if (!Verify(data, sha256, signature))
            {
                File.Delete(tmp);
                throw new InvalidDataException("подпись обновления не совпала");
            }
            Paths.Log("update verified");
            Process.Start(new ProcessStartInfo(tmp, "--apply-update") { UseShellExecute = false });
            Environment.Exit(0);
        }

        public static bool Verify(byte[] data, string sha256, string signature)
        {
            using (var sha = SHA256.Create())
            {
                var hex = BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "");
                if (!string.Equals(hex, sha256, StringComparison.OrdinalIgnoreCase)) return false;
            }
            // BCRYPT_ECDSA_PUBLIC_P256_MAGIC + длина координаты + X и Y
            var blob = new byte[] { 0x45, 0x43, 0x53, 0x31, 32, 0, 0, 0 }.Concat(Convert.FromBase64String(PublicKey)).ToArray();
            using (var key = CngKey.Import(blob, CngKeyBlobFormat.EccPublicBlob))
            using (var ec = new ECDsaCng(key) { HashAlgorithm = CngAlgorithm.Sha256 })
                return ec.VerifyData(data, Convert.FromBase64String(signature));
        }
    }
}
