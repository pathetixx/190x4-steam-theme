using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bloodmoon
{
    // Окно программы: установка, а для установленной темы проверка обновлений и управление. Рисунок готовый, кодом только раскладка.
    sealed class SetupForm : Form
    {
        const int W = 760, H = 440;
        enum Mode { Install, Update, Manage, Uninstall }
        sealed class Link { public string Text; public Action Do; public RectangleF Rect; }

        readonly Image bg = Art("setup-bg.png"), word = Art("wordmark.png"), btn = Art("button.jpg");
        readonly PrivateFontCollection fonts = new PrivateFontCollection();
        readonly List<Link> links = new List<Link>();
        Mode mode;
        string status = "", button;
        bool busy, finished, hoverBtn, hoverClose;
        Link hoverLink;
        int progress = -1;
        float k = 1;

        static Image Art(string n) { return Image.FromStream(new MemoryStream(Paths.ResourceBytes(n))); }

        [DllImport("gdi32.dll")] static extern IntPtr AddFontMemResourceEx(IntPtr pb, uint cb, IntPtr pdv, out uint n);

        public SetupForm(bool uninstall)
        {
            var f = Paths.ResourceBytes("Oswald-700.ttf");
            var h = Marshal.AllocCoTaskMem(f.Length);
            Marshal.Copy(f, 0, h, f.Length);
            uint nf;
            AddFontMemResourceEx(h, (uint)f.Length, IntPtr.Zero, out nf);
            fonts.AddMemoryFont(h, f.Length);
            Text = Paths.Name;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(5, 4, 5);
            Icon = new Icon(new MemoryStream(Paths.ResourceBytes("app.ico")));
            AutoScaleMode = AutoScaleMode.None;
            k = DeviceDpi / 96f;
            ClientSize = new Size((int)(W * k), (int)(H * k));
            if (uninstall) mode = Mode.Uninstall;
            else if (!Setup.IsInstalled) mode = Mode.Install;
            else if (!Setup.RunningInstalled && Paths.CurrentVersion.ToString(3) != Setup.InstalledVersion.ToString(3) && Paths.CurrentVersion > Setup.InstalledVersion) mode = Mode.Update;
            else mode = Mode.Manage;
            Configure();
        }

        void Configure()
        {
            links.Clear();
            switch (mode)
            {
                case Mode.Uninstall:
                    button = "Удалить"; status = "Тема будет снята, Steam вернётся к обычному виду";
                    break;
                case Mode.Install:
                    button = "Установить"; status = "Steam перезапустится, Windows один раз спросит разрешение";
                    break;
                case Mode.Update:
                    button = "Обновить"; status = "Установлена версия " + Setup.InstalledVersion.ToString(3);
                    links.Add(new Link { Text = "Удалить тему", Do = () => Act(true) });
                    break;
                default:
                    button = "Проверить обновления";
                    status = File.Exists(Paths.DisabledFlag) ? "Тема выключена" : "Тема работает";
                    links.Add(new Link { Text = File.Exists(Paths.DisabledFlag) ? "Включить тему" : "Выключить тему", Do = Toggle });
                    links.Add(new Link { Text = "Переустановить", Do = () => Act(false) });
                    links.Add(new Link { Text = "Удалить тему", Do = () => Act(true) });
                    break;
            }
            var x = 44f;
            foreach (var l in links)
            {
                l.Rect = new RectangleF(x, 396, 8.2f * l.Text.Length + 16, 28);
                x = l.Rect.Right + 18;
            }
            Invalidate();
        }

        void Toggle()
        {
            Setup.ToggleTheme();
            Configure();
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            k = e.DeviceDpiNew / 96f;
            ClientSize = new Size((int)(W * k), (int)(H * k));
            Invalidate();
        }

        RectangleF R(float x, float y, float w, float h) { return new RectangleF(x * k, y * k, w * k, h * k); }
        RectangleF Scaled(RectangleF r) { return R(r.X, r.Y, r.Width, r.Height); }
        RectangleF BtnRect { get { return R(40, 322, button != null && button.Length > 12 ? 340 : 240, 64); } }
        RectangleF CloseRect { get { return R(W - 48, 8, 40, 36); } }

        GraphicsPath Slanted(RectangleF r)
        {
            var c = 18 * k;
            var path = new GraphicsPath();
            path.AddPolygon(new[] { new PointF(r.X + c, r.Y), new PointF(r.Right, r.Y), new PointF(r.Right - c, r.Bottom), new PointF(r.X, r.Bottom) });
            return path;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.DrawImage(bg, new RectangleF(0, 0, ClientSize.Width, ClientSize.Height));
            var wr = 620f * k;
            g.DrawImage(word, new RectangleF(40 * k, 60 * k, wr, wr * word.Height / word.Width));
            using (var f = new Font(fonts.Families[0], 14 * k, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(Color.FromArgb(155, 146, 136)))
                g.DrawString("190x4  ·  ТЕМА ДЛЯ STEAM  ·  v" + Paths.CurrentVersion.ToString(3), f, b, 44 * k, 250 * k);
            using (var f = new Font(fonts.Families[0], 15 * k, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(Color.FromArgb(232, 224, 212)))
                g.DrawString(status, f, b, new RectangleF(44 * k, 280 * k, (W - 88) * k, 30 * k));
            if (progress >= 0) PaintProgress(g);
            else if (button != null) PaintButton(g);
            if (!busy)
                foreach (var l in links)
                    using (var f = new Font(fonts.Families[0], 14 * k, GraphicsUnit.Pixel))
                    using (var b = new SolidBrush(l == hoverLink ? Color.FromArgb(232, 224, 212) : Color.FromArgb(155, 146, 136)))
                    using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
                        g.DrawString(l.Text, f, b, Scaled(l.Rect), sf);
            using (var edge = new Pen(Color.FromArgb(90, 179, 18, 46), 1)) g.DrawRectangle(edge, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
            var cr = CloseRect;
            using (var p = new Pen(hoverClose ? Color.FromArgb(210, 26, 60) : Color.FromArgb(232, 224, 212), 2 * k))
            {
                g.DrawLine(p, cr.X + 13 * k, cr.Y + 11 * k, cr.X + 27 * k, cr.Y + 25 * k);
                g.DrawLine(p, cr.X + 27 * k, cr.Y + 11 * k, cr.X + 13 * k, cr.Y + 25 * k);
            }
        }

        void PaintButton(Graphics g)
        {
            var r = BtnRect;
            using (var path = Slanted(r))
            {
                var old = g.Clip;
                g.SetClip(path);
                g.DrawImage(btn, r);
                if (busy) using (var d = new SolidBrush(Color.FromArgb(140, 5, 4, 5))) g.FillPath(d, path);
                else if (hoverBtn) using (var d = new SolidBrush(Color.FromArgb(40, 255, 255, 255))) g.FillPath(d, path);
                g.Clip = old;
            }
            using (var f = new Font(fonts.Families[0], 24 * k, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(Color.FromArgb(232, 224, 212)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(button.ToUpperInvariant(), f, b, r, sf);
        }

        // Полоса загрузки: тот же мазок, что на кнопке, проявляется слева направо в той же косой форме.
        void PaintProgress(Graphics g)
        {
            var r = R(40, 322, 680, 64);
            using (var path = Slanted(r))
            {
                using (var d = new SolidBrush(Color.FromArgb(150, 5, 4, 5))) g.FillPath(d, path);
                var old = g.Clip;
                g.SetClip(path);
                g.SetClip(new RectangleF(r.X, r.Y, r.Width * progress / 100f, r.Height), CombineMode.Intersect);
                g.DrawImage(btn, r);
                g.Clip = old;
                using (var p = new Pen(Color.FromArgb(120, 232, 224, 212), 1)) g.DrawPath(p, path);
            }
            using (var f = new Font(fonts.Families[0], 24 * k, GraphicsUnit.Pixel))
            using (var b = new SolidBrush(Color.FromArgb(232, 224, 212)))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(progress + " %", f, b, r, sf);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var a = progress < 0 && BtnRect.Contains(e.Location);
            var c = CloseRect.Contains(e.Location);
            Link l = null;
            if (!busy) foreach (var x in links) if (Scaled(x.Rect).Contains(e.Location)) l = x;
            if (a != hoverBtn || l != hoverLink || c != hoverClose) { hoverBtn = a; hoverLink = l; hoverClose = c; Invalidate(); }
            Cursor = (a && !busy) || l != null || c ? Cursors.Hand : Cursors.Default;
        }

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, IntPtr l);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (CloseRect.Contains(e.Location)) { if (!busy) Close(); return; }
            if (progress < 0 && BtnRect.Contains(e.Location)) { if (!busy) Main(); return; }
            if (!busy)
                foreach (var l in links)
                    if (Scaled(l.Rect).Contains(e.Location)) { l.Do(); return; }
            ReleaseCapture();
            SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
        }

        void Main()
        {
            if (finished) { Close(); return; }
            if (mode == Mode.Manage) CheckUpdates();
            else Act(mode == Mode.Uninstall);
        }

        async void CheckUpdates()
        {
            busy = true; status = "Проверяю обновления"; Invalidate();
            Action<int> report = p => BeginInvoke((Action)(() =>
            {
                progress = p;
                status = "Скачиваю обновление";
                Invalidate();
            }));
            var result = await Task.Run(() => Updater.Check(report));
            busy = false; progress = -1; status = result; Invalidate();
        }

        async void Act(bool remove)
        {
            busy = true; links.Clear(); Invalidate();
            Action<string> say = s => BeginInvoke((Action)(() => { status = s; Invalidate(); }));
            var last = "";
            Action<string> track = s => { last = s; say(s); };
            try
            {
                await Task.Run(() => { if (remove) Setup.Uninstall(track); else Setup.Install(track); });
                var warned = last.StartsWith("Игра") || last.StartsWith("Закройте");
                status = remove ? "Тема удалена" : warned ? last : "Готово. Обновления будут приходить сами";
                button = "Закрыть"; finished = true;
            }
            catch (Exception e)
            {
                Paths.Log("setup: " + e);
                status = e.Message;
                Configure();
                status = e.Message;
            }
            busy = false; Invalidate();
        }
    }
}
