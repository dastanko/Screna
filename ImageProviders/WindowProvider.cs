using Screna.Native;
using System;
using System.Drawing;
using System.Windows.Interop;
using SysParams = System.Windows.SystemParameters;

namespace Screna
{
    public class WindowProvider : IImageProvider
    {
        public static readonly int DesktopHeight, DesktopWidth;

        public static readonly Rectangle DesktopRectangle;

        public static readonly IntPtr DesktopHandle = User32.GetDesktopWindow(),
            TaskbarHandle = User32.FindWindow("Shell_TrayWnd", null);

        static WindowProvider()
        {
            using (var source = new HwndSource(new HwndSourceParameters()))
            {
                var toDevice = source.CompositionTarget.TransformToDevice;

                DesktopHeight = (int)Math.Round(SysParams.VirtualScreenHeight * toDevice.M22);
                DesktopWidth = (int)Math.Round(SysParams.VirtualScreenWidth * toDevice.M11);

                DesktopRectangle = new Rectangle((int)SysParams.VirtualScreenLeft, (int)SysParams.VirtualScreenTop, DesktopWidth, DesktopHeight);
            }
        }

        Func<IntPtr> hWnd;
        Color BackgroundColor;
        IOverlay[] Overlays;

        public WindowProvider(IntPtr hWnd = default(IntPtr), Color BackgroundColor = default(Color), params IOverlay[] Overlays)
            : this(() => hWnd, BackgroundColor, Overlays) { }

        public WindowProvider(Func<IntPtr> hWnd, Color BackgroundColor = default(Color), params IOverlay[] Overlays)
        {
            this.hWnd = hWnd;
            this.Overlays = Overlays;
            this.BackgroundColor = BackgroundColor;
        }

        public Bitmap Capture()
        {
            IntPtr WindowHandle = hWnd();

            Rectangle Rect = DesktopRectangle;

            if (WindowHandle != DesktopHandle && WindowHandle != IntPtr.Zero)
            {
                RECT r;

                if (User32.GetWindowRect(WindowHandle, out r))
                    Rect = r.ToRectangle();
            }

            var BMP = new Bitmap(DesktopWidth, DesktopHeight);

            using (var g = Graphics.FromImage(BMP))
            {
                if (BackgroundColor != Color.Transparent)
                    g.FillRectangle(new SolidBrush(BackgroundColor), DesktopRectangle);

                g.CopyFromScreen(Rect.Location, Rect.Location,
                    new Size(Rect.Width, Rect.Height),
                    CopyPixelOperation.SourceCopy);

                foreach (var overlay in Overlays)
                    overlay.Draw(g);
            }

            return BMP;
        }

        public int Height => DesktopHeight;

        public int Width => DesktopWidth;

        public void Dispose()
        {
            foreach (var overlay in Overlays)
                overlay.Dispose();
        }
    }
}
