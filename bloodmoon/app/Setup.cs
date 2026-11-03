using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.Win32;

namespace Bloodmoon
{
    // Установка, удаление и замена программы. Пишет только в профиль пользователя, кроме одного файла-флага Steam.
    static class Setup
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\190x4-Bloodmoon";

        public static bool IsInstalled { get { return File.Exists(Paths.InstalledExe); } }

        public static void Install(Action<string> status)
        {
            var steam = Paths.SteamDir();
            if (steam == null || !File.Exists(Path.Combine(steam, "steam.exe"))) throw new InvalidOperationException("Steam не найден на этом компьютере");
            status("Копирую файлы");
            StopAgents();
            Directory.CreateDirectory(Paths.InstallDir);
            if (!string.Equals(Paths.CurrentExe, Paths.InstalledExe, StringComparison.OrdinalIgnoreCase))
                File.Copy(Paths.CurrentExe, Paths.InstalledExe, true);
            status("Включаю подключение к Steam");
            SetFlag(steam, true);
            Register();
            try { File.Delete(Paths.DisabledFlag); } catch { }
            StartAgent();
            status("Перезапускаю Steam");
            RestartSteam(steam, status);
        }

        public static void Uninstall(Action<string> status)
        {
            status("Удаляю тему");
            StopAgents();
            var steam = Paths.SteamDir();
            if (steam != null) SetFlag(steam, false);
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true)) if (k != null) k.DeleteValue("190x4-Bloodmoon", false);
            Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);
            if (steam != null && SteamRunning() && !GameRunning()) { status("Перезапускаю Steam"); RestartSteam(steam, status); }
            // сам себя exe удалить не может: это сделает cmd после выхода
            Process.Start(new ProcessStartInfo("cmd.exe", "/c timeout /t 3 /nobreak >nul & rmdir /s /q \"" + Paths.InstallDir + "\"") { CreateNoWindow = true, UseShellExecute = false });
        }

        // Обновление: временный exe ждёт выхода старого, заменяет его и запускает новую версию.
        public static void ApplyUpdate()
        {
            StopAgents();
            for (var i = 0; ; i++)
            {
                try { File.Copy(Paths.CurrentExe, Paths.InstalledExe, true); break; }
                catch (IOException) when (i < 20) { Thread.Sleep(500); }
            }
            StartAgent();
            // временный файл удалит cmd после выхода этого процесса
            Process.Start(new ProcessStartInfo("cmd.exe", "/c timeout /t 3 /nobreak >nul & del /q \"" + Paths.CurrentExe + "\"") { CreateNoWindow = true, UseShellExecute = false });
        }

        // Папка Steam обычно требует прав администратора: только ради этого файла и появляется запрос UAC.
        public static void SetFlag(string steam, bool on)
        {
            var file = Paths.FlagFile(steam);
            if (File.Exists(file) == on) return;
            try
            {
                if (on) File.WriteAllBytes(file, new byte[0]); else File.Delete(file);
                return;
            }
            catch (UnauthorizedAccessException) { }
            var p = new ProcessStartInfo(Paths.CurrentExe, (on ? "--flag-on " : "--flag-off ") + "\"" + steam + "\"") { Verb = "runas", UseShellExecute = true };
            try
            {
                using (var e = Process.Start(p)) e.WaitForExit();
            }
            catch (Win32Exception) { throw new InvalidOperationException("Нужно разрешить изменение в папке Steam"); }
            if (File.Exists(file) != on) throw new InvalidOperationException("Не удалось изменить файл в папке Steam");
        }

        static void Register()
        {
            using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
                k.SetValue("190x4-Bloodmoon", "\"" + Paths.InstalledExe + "\" --agent");
            using (var k = Registry.CurrentUser.CreateSubKey(UninstallKey))
            {
                k.SetValue("DisplayName", Paths.Name);
                k.SetValue("DisplayVersion", Paths.CurrentVersion.ToString(3));
                k.SetValue("Publisher", "190x4");
                k.SetValue("DisplayIcon", Paths.InstalledExe);
                k.SetValue("InstallLocation", Paths.InstallDir);
                k.SetValue("UninstallString", "\"" + Paths.InstalledExe + "\" --uninstall");
                k.SetValue("NoModify", 1, RegistryValueKind.DWord);
                k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        static void StartAgent() { Process.Start(new ProcessStartInfo(Paths.InstalledExe, "--agent") { UseShellExecute = false }); }

        static void StopAgents()
        {
            var me = Process.GetCurrentProcess().Id;
            foreach (var p in Process.GetProcessesByName("190x4-Bloodmoon").Where(x => x.Id != me))
                try { p.Kill(); p.WaitForExit(3000); } catch { }
        }

        static bool SteamRunning() { return Process.GetProcessesByName("steam").Length > 0; }

        static bool GameRunning()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam"))
            {
                var v = k == null ? null : k.GetValue("RunningAppID");
                return v is int && (int)v != 0;
            }
        }

        // Запущенную игру не трогаем: порт откроется при следующем запуске Steam.
        static void RestartSteam(string steam, Action<string> status)
        {
            var exe = Path.Combine(steam, "steam.exe");
            if (GameRunning()) { status("Игра запущена: тема появится после перезапуска Steam"); return; }
            if (SteamRunning())
            {
                using (Process.Start(exe, "-shutdown")) { }
                for (var i = 0; i < 60 && SteamRunning(); i++) Thread.Sleep(500);
                if (SteamRunning()) { status("Закройте Steam вручную и запустите снова"); return; }
                Thread.Sleep(1500);
            }
            using (Process.Start(exe)) { }
        }
    }
}
