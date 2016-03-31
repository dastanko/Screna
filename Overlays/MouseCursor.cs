using Screna.Native;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Screna
{
    /// <summary>
    /// Draws the MouseCursor on an Image
    /// </summary>
    public class MouseCursor : IOverlay
    {
        const int CURSOR_SHOWING = 1;

        IconInfo icInfo;
        IntPtr hIcon;
        CursorInfo CursorInfo;

        public MouseCursor(bool Include = true) { this.Include = Include; }

        public static Point CursorPosition
        {
            get
            {
                var P = new Point();
                User32.GetCursorPos(ref P);
                return P;
            }
        }

        public bool Include { get; set; }

        public void Draw(Graphics g, Point Offset = default(Point))
        {
            if (Include)
            {
                CursorInfo = new CursorInfo { cbSize = Marshal.SizeOf(typeof(CursorInfo)) };

                if (!User32.GetCursorInfo(out CursorInfo))
                    return;

                if (CursorInfo.flags != CURSOR_SHOWING)
                    return;

                hIcon = User32.CopyIcon(CursorInfo.hCursor);

                if (!User32.GetIconInfo(hIcon, out icInfo))
                    return;

                var Location = new Point(CursorInfo.ptScreenPos.X - Offset.X - icInfo.xHotspot,
                    CursorInfo.ptScreenPos.Y - Offset.Y - icInfo.yHotspot);

                if (hIcon != IntPtr.Zero)
                    using (var CursorBMP = Icon.FromHandle(hIcon).ToBitmap())
                        g.DrawImage(CursorBMP, new Rectangle(Location, CursorBMP.Size));

                User32.DestroyIcon(hIcon);
            }
        }

        public void Dispose() { }
    }
}