using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;

namespace Screna.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct IconInfo
    {
        public bool fIcon;
        public int xHotspot;
        public int yHotspot;
        public IntPtr hbmMask;
        public IntPtr hbmColor;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CursorInfo
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public Point ptScreenPos;
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public RECT(int Left, int Top, int Right, int Bottom)
        {
            this = new RECT()
            {
                Left = Left,
                Top = Top,
                Right = Right,
                Bottom = Bottom
            };
        }
    }

    public struct CallbackData
    {
        readonly IntPtr m_LParam, m_WParam;

        public CallbackData(IntPtr wParam, IntPtr lParam)
        {
            m_WParam = wParam;
            m_LParam = lParam;
        }

        public IntPtr WParam { get { return m_WParam; } }

        public IntPtr LParam { get { return m_LParam; } }
    }

    static class HookHelper
    {
        enum HookId
        {
            LowLevelMouse = 14,
            LowLevelKeyboard = 13,
        }

        public static HookResult HookGlobalMouse(Callback callback) { return HookGlobal(HookId.LowLevelMouse, callback); }

        public static HookResult HookGlobalKeyboard(Callback callback) { return HookGlobal(HookId.LowLevelKeyboard, callback); }

        static HookResult HookGlobal(HookId hookId, Callback callback)
        {
            HookProcedure hookProcedure = (code, param, lParam) => HookProcedure(code, param, lParam, callback);

            var hookHandle = User32.SetWindowsHookEx(
                (int)hookId,
                hookProcedure,
                Process.GetCurrentProcess().MainModule.BaseAddress,
                0);

            if (hookHandle.IsInvalid) ThrowLastUnmanagedErrorAsException();

            return new HookResult(hookHandle, hookProcedure);
        }

        static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam, Callback callback)
        {
            var passThrough = nCode != 0;
            if (passThrough) return CallNextHookEx(nCode, wParam, lParam);

            var callbackData = new CallbackData(wParam, lParam);
            var continueProcessing = callback(callbackData);

            if (!continueProcessing) return new IntPtr(-1);

            return CallNextHookEx(nCode, wParam, lParam);
        }

        static IntPtr CallNextHookEx(int nCode, IntPtr wParam, IntPtr lParam)
        {
            return User32.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        static void ThrowLastUnmanagedErrorAsException()
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode);
        }
    }

    public class HookProcedureHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        static bool _closing;

        static HookProcedureHandle() { Application.ApplicationExit += (sender, e) => { _closing = true; }; }

        public HookProcedureHandle() : base(true) { }

        protected override bool ReleaseHandle()
        {
            //NOTE Calling Unhook during processexit causes deley
            if (_closing) return true;
            return User32.UnhookWindowsHookEx(handle) != 0;
        }
    }

    public class HookResult : IDisposable
    {
        readonly HookProcedureHandle m_Handle;
        readonly HookProcedure m_Procedure;

        public HookResult(HookProcedureHandle handle, HookProcedure procedure)
        {
            m_Handle = handle;
            m_Procedure = procedure;
        }

        public HookProcedureHandle Handle { get { return m_Handle; } }

        public HookProcedure Procedure { get { return m_Procedure; } }

        public void Dispose() { m_Handle.Dispose(); }
    }

    [StructLayout(LayoutKind.Sequential)]
    struct KeyboardHookStruct
    {
        public int VirtualKeyCode;
        public int ScanCode;
        public int Flags;
        public int Time;
        public int ExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct MouseStruct
    {
        [FieldOffset(0x00)]
        public Point Point;
        [FieldOffset(0x0A)]
        public short MouseData;
        [FieldOffset(0x10)]
        public int Timestamp;
    }

    class KeyboardState
    {
        readonly byte[] m_KeyboardStateNative;

        KeyboardState(byte[] keyboardStateNative) { m_KeyboardStateNative = keyboardStateNative; }

        public static KeyboardState GetCurrent()
        {
            byte[] keyboardStateNative = new byte[256];
            User32.GetKeyboardState(keyboardStateNative);
            return new KeyboardState(keyboardStateNative);
        }

        public byte[] GetNativeState() { return m_KeyboardStateNative; }

        public bool IsDown(Keys key)
        {
            byte keyState = GetKeyState(key);
            bool isDown = GetHighBit(keyState);
            return isDown;
        }

        public bool IsToggled(Keys key)
        {
            byte keyState = GetKeyState(key);
            bool isToggled = GetLowBit(keyState);
            return isToggled;
        }

        byte GetKeyState(Keys key)
        {
            int virtualKeyCode = (int)key;
            if (virtualKeyCode < 0 || virtualKeyCode > 255)
                throw new ArgumentOutOfRangeException("key", key, "The value must be between 0 and 255.");
            return m_KeyboardStateNative[virtualKeyCode];
        }

        static bool GetHighBit(byte value) { return (value >> 7) != 0; }

        static bool GetLowBit(byte value) { return (value & 1) != 0; }
    }
}