using System;

namespace Screna.Native
{
    public delegate IntPtr WindowProcedureHandler(IntPtr hwnd, uint uMsg, IntPtr wparam, IntPtr lparam);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public delegate bool Callback(CallbackData data);

    public delegate IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam);
}