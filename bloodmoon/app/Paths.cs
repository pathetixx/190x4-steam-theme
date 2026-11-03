using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Win32;

namespace Bloodmoon
{
    // Общие пути и встроенные файлы.
    static class Paths
    {
        public const string Name = "190x4 Bloodmoon";
        public static readonly string InstallDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "190x4-Bloodmoon");
        public static readonly string InstalledExe = Path.Combine(InstallDir, "190x4-Bloodmoon.exe");
        public static readonly string CacheDir = Path.Combine(InstallDir, "cache");
        public static readonly string LogFile = Path.Combine(InstallDir, "log.txt");
        public static readonly string DisabledFlag = Path.Combine(InstallDir, "disabled");
        public static string CurrentExe { get { return Assembly.GetExecutingAssembly().Location; } }
        public static Version CurrentVersion { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        public static string SteamDir()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                var p = k == null ? null : k.GetValue("SteamPath") as string;
                if (!string.IsNullOrEmpty(p)) return p.Replace('/', '\\');
            }
            using (var k = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam"))
            {
                var p = k == null ? null : k.GetValue("InstallPath") as string;
                if (!string.IsNullOrEmpty(p)) return p;
            }
            return null;
        }

        public static string FlagFile(string steamDir) { return Path.Combine(steamDir, ".cef-enable-remote-debugging"); }

        public static byte[] ResourceBytes(string name)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            using (var m = new MemoryStream())
            {
                s.CopyTo(m);
                return m.ToArray();
            }
        }

        public static string ResourceText(string name) { return new UTF8Encoding(false).GetString(ResourceBytes(name)); }

        // Журнал короткий: только события запуска и ошибки.
        public static void Log(string line)
        {
            try
            {
                Directory.CreateDirectory(InstallDir);
                if (File.Exists(LogFile) && new FileInfo(LogFile).Length > 256 * 1024) File.Delete(LogFile);
                File.AppendAllText(LogFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + line + Environment.NewLine);
            }
            catch { }
        }
    }
}
