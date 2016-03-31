using Screna.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Screna
{
    public class WindowHandler
    {
        public WindowHandler(IntPtr hWnd) { Handle = hWnd; }

        public bool IsVisible => User32.IsWindowVisible(Handle);

        public IntPtr Handle { get; }

        public string Title
        {
            get
            {
                var title = new StringBuilder(User32.GetWindowTextLength(Handle) + 1);
                User32.GetWindowText(Handle, title, title.Capacity);
                return title.ToString();
            }
        }

        public static IEnumerable<WindowHandler> Enumerate()
        {
            var list = new List<WindowHandler>();

            User32.EnumWindows((hWnd, lParam) =>
                {
                    list.Add(new WindowHandler(hWnd));

                    return true;
                }, IntPtr.Zero);

            return list;
        }

        public static IEnumerable<WindowHandler> EnumerateVisible()
        {
            foreach (var hWnd in from win in Enumerate() let hWnd = win.Handle where win.IsVisible select hWnd)
            {
                if (!(User32.GetWindowLong(hWnd, GetWindowLongValue.GWL_EXSTYLE).HasFlag(WindowStyles.WS_EX_APPWINDOW)))
                {
                    if (User32.GetWindow(hWnd, GetWindowEnum.Owner) != IntPtr.Zero)
                        continue;

                    if (User32.GetWindowLong(hWnd, GetWindowLongValue.GWL_EXSTYLE).HasFlag(WindowStyles.WS_EX_TOOLWINDOW))
                        continue;

                    if (User32.GetWindowLong(hWnd, GetWindowLongValue.GWL_STYLE).HasFlag(WindowStyles.WS_CHILD))
                        continue;
                }

                yield return new WindowHandler(hWnd);
            }
        }
    }
}