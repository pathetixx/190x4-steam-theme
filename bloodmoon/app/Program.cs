using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bloodmoon
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var cmd = args.FirstOrDefault() ?? "";
            try
            {
                switch (cmd)
                {
                    case "--flag-on": File.WriteAllBytes(Paths.FlagFile(args[1]), new byte[0]); return 0;
                    case "--flag-off": File.Delete(Paths.FlagFile(args[1])); return 0;
                    case "--apply-update": Setup.ApplyUpdate(); return 0;
                    case "--agent": return RunAgent();
                }
            }
            catch (Exception e) { Paths.Log(cmd + ": " + e); return 1; }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new SetupForm(cmd == "--uninstall"));
            if (Setup.CleanupOnExit) Setup.RunCleanup();
            return 0;
        }

        static int RunAgent()
        {
            using (var mutex = new Mutex(false, @"Local\190x4-Bloodmoon-agent"))
            {
                if (!mutex.WaitOne(0)) return 0;
                Application.EnableVisualStyles();
                var agent = new Agent(!File.Exists(Paths.DisabledFlag));
                using (var tray = new TrayIcon(agent))
                {
                    Task.Run(() => agent.Run());
                    Task.Run(async () =>
                    {
                        await Task.Delay(Updater.TestFeed != null ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(1)).ConfigureAwait(false);
                        while (true)
                        {
                            await Updater.Check(false).ConfigureAwait(false);
                            await Task.Delay(TimeSpan.FromHours(6)).ConfigureAwait(false);
                        }
                    });
                    Application.Run();
                }
                agent.Stop();
            }
            return 0;
        }
    }

    sealed class TrayIcon : IDisposable
    {
        readonly NotifyIcon icon = new NotifyIcon();
        readonly Agent agent;
        readonly ToolStripMenuItem toggle;

        public TrayIcon(Agent agent)
        {
            this.agent = agent;
            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem(Paths.Name + " " + Paths.CurrentVersion.ToString(3)) { Enabled = false });
            menu.Items.Add(new ToolStripSeparator());
            toggle = new ToolStripMenuItem("", null, (s, e) => Toggle());
            menu.Items.Add(toggle);
            menu.Items.Add("Проверить обновления", null, async (s, e) => icon.ShowBalloonTip(3000, Paths.Name, await Updater.Check(true), ToolTipIcon.None));
            menu.Items.Add("Удалить тему", null, (s, e) => System.Diagnostics.Process.Start(Paths.CurrentExe, "--uninstall"));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Закрыть до перезагрузки", null, (s, e) => Application.Exit());
            icon.ContextMenuStrip = menu;
            icon.Icon = new Icon(new MemoryStream(Paths.ResourceBytes("app.ico")), SystemInformation.SmallIconSize);
            icon.Text = Paths.Name;
            icon.Visible = true;
            UpdateLabel();
        }

        void Toggle()
        {
            var off = !File.Exists(Paths.DisabledFlag);
            try
            {
                if (off) { Directory.CreateDirectory(Paths.InstallDir); File.WriteAllBytes(Paths.DisabledFlag, new byte[0]); }
                else File.Delete(Paths.DisabledFlag);
            }
            catch { }
            agent.SetEnabled(!off);
            UpdateLabel();
        }

        void UpdateLabel() { toggle.Text = File.Exists(Paths.DisabledFlag) ? "Включить тему" : "Выключить тему"; }

        public void Dispose() { icon.Visible = false; icon.Dispose(); }
    }
}
