namespace Screna.Native
{
    public enum GetWindowEnum { Owner = 4 }

    public enum SetWindowPositionFlags
    {
        NoMove = 0x2,
        NoSize = 1,
        NoZOrder = 0x4,
        ShowWindow = 0x400,
        NoActivate = 0x0010
    }

    public enum WindowStyles : long
    {
        WS_CHILD = 0x40000000,
        WS_EX_TOOLWINDOW = 0x00000080,
        WS_EX_APPWINDOW = 0x00040000,
        WS_SIZEBOX = 0x00040000L
    }

    public enum GetWindowLongValue
    {
        GWL_STYLE = -16,
        GWL_EXSTYLE = -20
    }

    public enum WindowsMessage
    {
        WM_LBUTTONDOWN = 0x201,
        WM_RBUTTONDOWN = 0x204,
        WM_MBUTTONDOWN = 0x207,
        WM_LBUTTONUP = 0x202,
        WM_RBUTTONUP = 0x205,
        WM_MBUTTONUP = 0x208,
        WM_LBUTTONDBLCLK = 0x203,
        WM_RBUTTONDBLCLK = 0x206,
        WM_MBUTTONDBLCLK = 0x209,
        WM_MOUSEWHEEL = 0x020A,
        WM_XBUTTONDOWN = 0x20B,
        WM_XBUTTONUP = 0x20C,
        WM_XBUTTONDBLCLK = 0x20D,
        WM_MOUSEHWHEEL = 0x20E,
        WM_KEYDOWN = 0x100,
        WM_KEYUP = 0x101,
        WM_SYSKEYDOWN = 0x104,
        WM_SYSKEYUP = 0x105
    }
}