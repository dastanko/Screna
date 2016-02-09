using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Screna.Native
{
    public static class User32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32", SetLastError = true)]
        public static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc proc, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SetWindowPositionFlags wFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, [Out] StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        public static extern WindowStyles GetWindowLong(IntPtr hWnd, GetWindowLongValue nIndex);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        public static extern bool GetIconInfo(IntPtr hIcon, out IconInfo piconinfo);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, GetWindowEnum uCmd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorPos(ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")] 
        public static extern IntPtr GetWindowDC(IntPtr hWnd); 

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern HookProcedureHandle SetWindowsHookEx(int idHook, HookProcedure lpfn, IntPtr hMod, int dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        public static extern int UnhookWindowsHookEx(IntPtr idHook);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [DllImport("user32")]
        public static extern int GetDoubleClickTime();

        //values from Winuser.h in Microsoft SDK.
        public const byte VK_SHIFT = 0x10;

        //may be possible to use these aggregates instead of L and R separately (untested)
        public const byte VK_CONTROL = 0x11,
            VK_MENU = 0x12,
            VK_PACKET = 0xE7;

        //Used to pass Unicode characters as if they were keystrokes. The VK_PACKET key is the low word of a 32-bit Virtual Key value used for non-keyboard input methods
        static int lastVirtualKeyCode = 0,
            lastScanCode = 0;
        static byte[] lastKeyState = new byte[255];
        static bool lastIsDead = false;

        public static void TryGetCharFromKeyboardState(int virtualKeyCode, int scanCode, int fuState, out char[] chars)
        {
            var dwhkl = GetActiveKeyboard(); //get the active keyboard layout
            TryGetCharFromKeyboardState(virtualKeyCode, scanCode, fuState, dwhkl, out chars);
        }

        public static void TryGetCharFromKeyboardState(int virtualKeyCode, int scanCode, int fuState, IntPtr dwhkl, out char[] chars)
        {
            StringBuilder pwszBuff = new StringBuilder(64);
            KeyboardState keyboardState = KeyboardState.GetCurrent();
            byte[] currentKeyboardState = keyboardState.GetNativeState();
            bool isDead = false;

            if (keyboardState.IsDown(Keys.ShiftKey))
                currentKeyboardState[(byte)Keys.ShiftKey] = 0x80;

            if (keyboardState.IsToggled(Keys.CapsLock))
                currentKeyboardState[(byte)Keys.CapsLock] = 0x01;

            var relevantChars = ToUnicodeEx(virtualKeyCode, scanCode, currentKeyboardState, pwszBuff, pwszBuff.Capacity, fuState, dwhkl);

            switch (relevantChars)
            {
                case -1:
                    isDead = true;
                    ClearKeyboardBuffer(virtualKeyCode, scanCode, dwhkl);
                    chars = null;
                    break;

                case 0:
                    chars = null;
                    break;

                case 1:
                    if (pwszBuff.Length > 0) chars = new[] { pwszBuff[0] };
                    else chars = null;
                    break;

                // Two or more (only two of them is relevant)
                default:
                    if (pwszBuff.Length > 1) chars = new[] { pwszBuff[0], pwszBuff[1] };
                    else chars = new[] { pwszBuff[0] };
                    break;
            }

            if (lastVirtualKeyCode != 0 && lastIsDead)
            {
                if (chars != null)
                {
                    StringBuilder sbTemp = new StringBuilder(5);
                    ToUnicodeEx(lastVirtualKeyCode, lastScanCode, lastKeyState, sbTemp, sbTemp.Capacity, 0, dwhkl);
                    lastIsDead = false;
                    lastVirtualKeyCode = 0;
                }

                return;
            }

            lastScanCode = scanCode;
            lastVirtualKeyCode = virtualKeyCode;
            lastIsDead = isDead;
            lastKeyState = (byte[])currentKeyboardState.Clone();
        }

        static void ClearKeyboardBuffer(int vk, int sc, IntPtr hkl)
        {
            var sb = new StringBuilder(10);

            int rc;

            do
            {
                byte[] lpKeyStateNull = new Byte[255];
                rc = ToUnicodeEx(vk, sc, lpKeyStateNull, sb, sb.Capacity, 0, hkl);
            }
            while (rc < 0);
        }

        static IntPtr GetActiveKeyboard()
        {
            IntPtr hActiveWnd = User32.GetForegroundWindow(); //handle to focused window
            int dwProcessId;
            int hCurrentWnd = User32.GetWindowThreadProcessId(hActiveWnd, out dwProcessId);
            //thread of focused window
            return GetKeyboardLayout(hCurrentWnd); //get the layout identifier for the thread whose window is focused
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicodeEx(int wVirtKey,
            int wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder pwszBuff,
            int cchBuff,
            int wFlags,
            IntPtr dwhkl);

        [DllImport("user32.dll")]
        public static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern short GetKeyState(int vKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetKeyboardLayout(int dwLayout);
    }
}
