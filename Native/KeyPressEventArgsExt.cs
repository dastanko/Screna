using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Screna.Native
{
    /// <summary>
    ///     Provides extended data for the <see cref='KeyListener.KeyPress' /> event.
    /// </summary>
    public class KeyPressEventArgsExt : KeyPressEventArgs
    {
        internal KeyPressEventArgsExt(char keyChar, int timestamp)
            : base(keyChar)
        {
            IsNonChar = keyChar == (char)0x0;
            Timestamp = timestamp;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref='KeyPressEventArgsExt' /> class.
        /// </summary>
        /// <param name="keyChar">
        ///     Character corresponding to the key pressed. 0 char if represents a system or functional non char
        ///     key.
        /// </param>
        public KeyPressEventArgsExt(char keyChar) : this(keyChar, Environment.TickCount) { }

        /// <summary>
        ///     True if represents a system or functional non char key.
        /// </summary>
        public bool IsNonChar { get; }

        /// <summary>
        ///     The system tick count of when the event occurred.
        /// </summary>
        public int Timestamp { get; }

        internal static IEnumerable<KeyPressEventArgsExt> FromRawDataGlobal(CallbackData data)
        {
            var wParam = data.WParam;
            var lParam = data.LParam;

            if ((WindowsMessage)wParam != WindowsMessage.WM_KEYDOWN) yield break;

            KeyboardHookStruct keyboardHookStruct =
                (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

            var virtualKeyCode = keyboardHookStruct.VirtualKeyCode;
            var scanCode = keyboardHookStruct.ScanCode;
            var fuState = keyboardHookStruct.Flags;

            if (virtualKeyCode == User32.VK_PACKET)
            {
                var ch = (char)scanCode;
                yield return new KeyPressEventArgsExt(ch, keyboardHookStruct.Time);
            }
            else
            {
                char[] chars;
                User32.TryGetCharFromKeyboardState(virtualKeyCode, scanCode, fuState, out chars);
                if (chars == null) yield break;
                foreach (var current in chars)
                    yield return new KeyPressEventArgsExt(current, keyboardHookStruct.Time);
            }
        }
    }
}