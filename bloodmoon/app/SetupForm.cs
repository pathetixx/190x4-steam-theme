using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bloodmoon
{
    // Окно установщика: арт, надпись и одна большая кнопка. Рисунок готовый, кодом только раскладка.
    sealed class SetupForm : Form
    {
        const int W = 760, H = 440;
        readonly Image bg = Art("setup-bg.jpg"), word = Art("wordmark.png"), btn = Art("button.jpg");
        readonly PrivateFontCollection fonts = new PrivateFontCollection();
        readonly bool uninstallMode;
        string status = "", button;
        string link;
        bool busy, finished, hoverBtn, hoverLink, hoverClose;
        float k = 1;

        static Image Art(string n) { return Image.FromStream(new MemoryStream(Paths.ResourceBytes(n))); }

        [DllImport("gdi32.dll")] static extern IntPtr AddFontMemResourceEx(IntPtr pb, uint cb, IntPtr pdv, out uint n);

        public SetupForm(bool uninstallMode)
        {
            this.uninstallMode = uninstallMode;
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
            if (uninstallMode) { button = "Удалить"; status = "Тема будет снята, Steam вернётся к обычному виду"; }
            else if (Setup.IsInstalled) { button = "Переустановить"; link = "Удалить тему"; status = "Тема уже установлена"; }
            else { button = "Установить"; status = "Steam перезапустится, Windows один раз спросит разрешение"; }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            k = e.DeviceDpiNew / 96f;
            ClientSize = new Size((int)(W * k), (int)(H * k));
            Invalidate();
        }

        RectangleF R(float x, float y, float w, float h) { return new RectangleF(x * k, y * k, w * k, h * k); }
        RectangleF BtnRect { get { return R(40, 322, 240, 64); } }
        RectangleF LinkRect { get { return R(300, 342, 130, 28); } }
        RectangleF CloseRect { get { return R(W - 48, 8, 40, 36); } }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
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
            if (button != null)
            {
                var r = BtnRect;
                using (var path = new GraphicsPath())
                {
                    var c = 18 * k;
                    path.AddPolygon(new[] { new PointF(r.X + c, r.Y), new PointF(r.Right, r.Y), new PointF(r.Right - c, r.Bottom), new PointF(r.X, r.Bottom) });
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
            if (link != null && !busy)
                using (var f = new Font(fonts.Families[0], 14 * k, GraphicsUnit.Pixel))
                using (var b = new SolidBrush(hoverLink ? Color.FromArgb(232, 224, 212) : Color.FromArgb(155, 146, 136)))
                using (var sf = new StringFormat { LineAlignment = StringAlignment.Center })
                    g.DrawString(link, f, b, LinkRect, sf);
            var cr = CloseRect;
            using (var p = new Pen(hoverClose ? Color.FromArgb(210, 26, 60) : Color.FromArgb(232, 224, 212), 2 * k))
            {
                g.DrawLine(p, cr.X + 13 * k, cr.Y + 11 * k, cr.X + 27 * k, cr.Y + 25 * k);
                g.DrawLine(p, cr.X + 27 * k, cr.Y + 11 * k, cr.X + 13 * k, cr.Y + 25 * k);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            var a = BtnRect.Contains(e.Location); var l = link != null && LinkRect.Contains(e.Location); var c = CloseRect.Contains(e.Location);
            if (a != hoverBtn || l != hoverLink || c != hoverClose) { hoverBtn = a; hoverLink = l; hoverClose = c; Invalidate(); }
            Cursor = (a && !busy) || l || c ? Cursors.Hand : Cursors.Default;
        }

        [DllImport("user32.dll")] static extern bool ReleaseCapture();
        [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int m, IntPtr w, IntPtr l);

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (CloseRect.Contains(e.Location)) { if (!busy) Close(); return; }
            if (BtnRect.Contains(e.Location)) { if (!busy) Act(uninstallMode); return; }
            if (link != null && !busy && LinkRect.Contains(e.Location)) { Act(true); return; }
            ReleaseCapture();
            SendMessage(Handle, 0xA1, (IntPtr)2, IntPtr.Zero);
        }

        async void Act(bool remove)
        {
            if (finished) { Close(); return; }
            busy = true; link = null; Invalidate();
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
            }
            busy = false; Invalidate();
        }
    }
}
