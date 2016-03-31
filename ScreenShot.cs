using Screna.Native;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;

namespace Screna
{
    /// <summary>
    /// Contains methods for taking ScreenShots
    /// </summary>
    public static class ScreenShot
    {
        public static Bitmap Capture(Screen Screen, bool IncludeCursor = false, bool Managed = true)
        {
            return Capture(Screen.Bounds, IncludeCursor, Managed);
        }

        public static Bitmap Capture(IntPtr WindowHandle, bool IncludeCursor = false)
        {
            RECT r;
            User32.GetWindowRect(WindowHandle, out r);
            var Region = r.ToRectangle();

            IntPtr hSrc = User32.GetWindowDC(WindowHandle),
                hDest = Gdi32.CreateCompatibleDC(hSrc),
                hBmp = Gdi32.CreateCompatibleBitmap(hSrc, Region.Width, Region.Height),
                hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            Gdi32.BitBlt(hDest, 0, 0,
                Region.Width, Region.Height,
                hSrc,
                Region.Left, Region.Top,
                CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            var bmp = Image.FromHbitmap(hBmp);

            Gdi32.SelectObject(hDest, hOldBmp);
            Gdi32.DeleteObject(hBmp);
            Gdi32.DeleteDC(hDest);
            Gdi32.DeleteDC(hSrc);

            var clone = bmp.Clone(new Rectangle(Point.Empty, bmp.Size), PixelFormat.Format24bppRgb);

            if (IncludeCursor)
                new MouseCursor().Draw(clone, Region.Location);

            return clone;
        }

        public static Bitmap Capture(Form Form, bool IncludeCursor = false)
        {
            return Capture(Form.Handle, IncludeCursor);
        }

        public static Bitmap Capture(System.Windows.Window Window, bool IncludeCursor = false)
        {
            var WIH = new WindowInteropHelper(Window);
            return Capture(WIH.Handle, IncludeCursor);
        }

        public static Bitmap Capture(bool IncludeCursor = false, bool Managed = true)
        {
            return Capture(WindowProvider.DesktopRectangle, IncludeCursor, Managed);
        }

        public static unsafe Bitmap CaptureTransparent(IntPtr WindowHandle, bool IncludeCursor = false)
        {
            var tmpColour = Color.White;

            var backdrop = new Form
            {
                AllowTransparency = true,
                BackColor = tmpColour,
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                Opacity = 0
            };

            var r = new RECT();

            if (DWMApi.DwmGetWindowAttribute(WindowHandle, DwmWindowAttribute.ExtendedFrameBounds, ref r, sizeof(RECT)) != 0)
                // DwmGetWindowAttribute() failed, usually means Aero is disabled so we fall back to GetWindowRect()
                User32.GetWindowRect(WindowHandle, out r);

            var R = r.ToRectangle();

            // Add a 100px margin for window shadows. Excess transparency is trimmed out later
            R.Inflate(100, 100);

            // This check handles if the window is outside of the visible screen
            R.Intersect(WindowProvider.DesktopRectangle);

            User32.ShowWindow(backdrop.Handle, 4);
            User32.SetWindowPos(backdrop.Handle, WindowHandle,
                R.Left, R.Top,
                R.Width, R.Height,
                SetWindowPositionFlags.NoActivate);
            backdrop.Opacity = 1;
            Application.DoEvents();

            // Capture screenshot with white background
            using (var whiteShot = CaptureRegionUnmanaged(R))
            {
                backdrop.BackColor = Color.Black;
                Application.DoEvents();

                // Capture screenshot with black background
                using (var blackShot = CaptureRegionUnmanaged(R))
                {
                    backdrop.Dispose();

                    var transparentImage = Extensions.DifferentiateAlpha(whiteShot, blackShot);
                    if (IncludeCursor)
                        new MouseCursor().Draw(transparentImage, R.Location);
                    return transparentImage.CropEmptyEdges();
                }
            }
        }

        public static Bitmap CaptureTransparent(IntPtr hWnd, bool IncludeCursor, bool DoResize, int ResizeWidth, int ResizeHeight)
        {
            var StartButtonHandle = User32.FindWindow("Button", "Start");
            var TaskbarHandle = User32.FindWindow("Shell_TrayWnd", null);

            var CanResize = DoResize && User32.GetWindowLong(hWnd, GetWindowLongValue.GWL_STYLE).HasFlag(WindowStyles.WS_SIZEBOX);

            try
            {
                // Hide the taskbar, just incase it gets in the way
                if (hWnd != StartButtonHandle && hWnd != TaskbarHandle)
                {
                    User32.ShowWindow(StartButtonHandle, 0);
                    User32.ShowWindow(TaskbarHandle, 0);
                    Application.DoEvents();
                }

                if (User32.IsIconic(hWnd))
                {
                    User32.ShowWindow(hWnd, 1);
                    Thread.Sleep(300); // Wait for window to be restored
                }
                else
                {
                    User32.ShowWindow(hWnd, 5);
                    Thread.Sleep(100);
                }

                User32.SetForegroundWindow(hWnd);

                var r = new RECT();

                if (CanResize)
                {
                    User32.GetWindowRect(hWnd, out r);

                    User32.SetWindowPos(hWnd, IntPtr.Zero, r.Left, r.Top, ResizeWidth, ResizeHeight, SetWindowPositionFlags.ShowWindow);

                    Thread.Sleep(100);
                }

                var s = CaptureTransparent(hWnd, IncludeCursor);

                var R = r.ToRectangle();

                if (CanResize)
                    User32.SetWindowPos(hWnd, IntPtr.Zero,
                        R.Left, R.Top,
                        R.Width, R.Height,
                        SetWindowPositionFlags.ShowWindow);

                return s;
            }
            finally
            {
                if (hWnd != StartButtonHandle && hWnd != TaskbarHandle)
                {
                    User32.ShowWindow(StartButtonHandle, 1);
                    User32.ShowWindow(TaskbarHandle, 1);
                }
            }
        }

        #region Region
        static Bitmap CaptureRegionUnmanaged(Rectangle Region, bool IncludeCursor = false)
        {
            IntPtr hSrc = Gdi32.CreateDC("DISPLAY", null, null, 0),
                hDest = Gdi32.CreateCompatibleDC(hSrc),
                hBmp = Gdi32.CreateCompatibleBitmap(hSrc, Region.Width, Region.Height),
                hOldBmp = Gdi32.SelectObject(hDest, hBmp);

            Gdi32.BitBlt(hDest, 0, 0,
                Region.Width, Region.Height,
                hSrc,
                Region.Left, Region.Top,
                CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            var bmp = Image.FromHbitmap(hBmp);

            Gdi32.SelectObject(hDest, hOldBmp);
            Gdi32.DeleteObject(hBmp);
            Gdi32.DeleteDC(hDest);
            Gdi32.DeleteDC(hSrc);

            var clone = bmp.Clone(new Rectangle(Point.Empty, bmp.Size), PixelFormat.Format24bppRgb);

            if (IncludeCursor)
                new MouseCursor().Draw(clone, Region.Location);

            return clone;
        }

        static Bitmap CaptureRegionManaged(Rectangle Region, bool IncludeCursor = false)
        {
            var BMP = new Bitmap(Region.Width, Region.Height);

            using (var g = Graphics.FromImage(BMP))
            {
                g.CopyFromScreen(Region.Location, Point.Empty, Region.Size, CopyPixelOperation.SourceCopy);

                if (IncludeCursor) new MouseCursor().Draw(g, Region.Location);

                g.Flush();
            }

            return BMP;
        }

        public static Bitmap Capture(Rectangle Region, bool IncludeCursor = false, bool Managed = true)
        {
            return Managed ? CaptureRegionManaged(Region, IncludeCursor) : CaptureRegionUnmanaged(Region, IncludeCursor);
        }
        #endregion
    }
}
