using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Screna.Native
{
    /// <summary>
    ///     Provides extended argument data for the <see cref='KeyListener.KeyDown' /> or
    ///     <see cref='KeyListener.KeyUp' /> event.
    /// </summary>
    public class KeyEventArgsExt : KeyEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KeyEventArgsExt" /> class.
        /// </summary>
        /// <param name="keyData"></param>
        public KeyEventArgsExt(Keys keyData) : base(keyData) { }

        internal KeyEventArgsExt(Keys keyData, int timestamp, bool isKeyDown, bool isKeyUp)
            : this(keyData)
        {
            Timestamp = timestamp;
            IsKeyDown = isKeyDown;
            IsKeyUp = isKeyUp;
        }

        /// <summary>
        ///     The system tick count of when the event occurred.
        /// </summary>
        public int Timestamp { get; private set; }

        /// <summary>
        ///     True if event signals key down..
        /// </summary>
        public bool IsKeyDown { get; private set; }

        /// <summary>
        ///     True if event signals key up.
        /// </summary>
        public bool IsKeyUp { get; private set; }

        internal static KeyEventArgsExt FromRawDataGlobal(CallbackData data)
        {
            var wParam = data.WParam;
            var lParam = data.LParam;
            var keyboardHookStruct =
                (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
            var keyData = AppendModifierStates((Keys)keyboardHookStruct.VirtualKeyCode);

            var keyCode = (WindowsMessage)wParam;
            bool isKeyDown = (keyCode == WindowsMessage.WM_KEYDOWN || keyCode == WindowsMessage.WM_SYSKEYDOWN);
            bool isKeyUp = (keyCode == WindowsMessage.WM_KEYUP || keyCode == WindowsMessage.WM_SYSKEYUP);

            return new KeyEventArgsExt(keyData, keyboardHookStruct.Time, isKeyDown, isKeyUp);
        }

        // # It is not possible to distinguish Keys.LControlKey and Keys.RControlKey when they are modifiers
        // Check for Keys.Control instead
        // Same for Shift and Alt(Menu)
        // See more at http://www.tech-archive.net/Archive/DotNet/microsoft.public.dotnet.framework.windowsforms/2008-04/msg00127.html #

        // A shortcut to make life easier
        static bool CheckModifier(int vKey) { return (User32.GetKeyState(vKey) & 0x8000) > 0; }

        static Keys AppendModifierStates(Keys keyData)
        {
            // Is Control being held down?
            bool control = CheckModifier(User32.VK_CONTROL);
            // Is Shift being held down?
            bool shift = CheckModifier(User32.VK_SHIFT);
            // Is Alt being held down?
            bool alt = CheckModifier(User32.VK_MENU);

            // Windows keys
            // # combine LWin and RWin key with other keys will potentially corrupt the data
            // notable F5 | Keys.LWin == F12, see https://globalmousekeyhook.codeplex.com/workitem/1188
            // and the KeyEventArgs.KeyData don't recognize combined data either

            // Function (Fn) key
            // # CANNOT determine state due to conversion inside keyboard
            // See http://en.wikipedia.org/wiki/Fn_key#Technical_details #

            return keyData |
                   (control ? Keys.Control : Keys.None) |
                   (shift ? Keys.Shift : Keys.None) |
                   (alt ? Keys.Alt : Keys.None);
        }
    }
}