using System.Collections.Generic;
using System.Windows.Forms;
using Screna.Native;

namespace Screna.Native
{
    public class KeyListener : BaseListener
    {
        public KeyListener() : base(HookHelper.HookGlobalKeyboard) { }

        public event KeyEventHandler KeyDown;
        public event KeyPressEventHandler KeyPress;
        public event KeyEventHandler KeyUp;

        public void InvokeKeyDown(KeyEventArgsExt e)
        {
            var handler = KeyDown;
            if (handler == null || e.Handled || !e.IsKeyDown) return;
            handler(this, e);
        }

        public void InvokeKeyPress(KeyPressEventArgsExt e)
        {
            var handler = KeyPress;
            if (handler == null || e.Handled || e.IsNonChar) return;
            handler(this, e);
        }

        public void InvokeKeyUp(KeyEventArgsExt e)
        {
            var handler = KeyUp;
            if (handler == null || e.Handled || !e.IsKeyUp) return;
            handler(this, e);
        }

        protected override bool Callback(CallbackData data)
        {
            var eDownUp = GetDownUpEventArgs(data);
            var pressEventArgs = GetPressEventArgs(data);

            InvokeKeyDown(eDownUp);
            foreach (var pressEventArg in pressEventArgs) InvokeKeyPress(pressEventArg);
            InvokeKeyUp(eDownUp);

            return !eDownUp.Handled;
        }

        IEnumerable<KeyPressEventArgsExt> GetPressEventArgs(CallbackData data) => KeyPressEventArgsExt.FromRawDataGlobal(data);

        KeyEventArgsExt GetDownUpEventArgs(CallbackData data) => KeyEventArgsExt.FromRawDataGlobal(data);
    }
}