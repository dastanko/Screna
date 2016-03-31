using System;

namespace Screna.Native
{
    public abstract class BaseListener : IDisposable
    {
        protected BaseListener(Func<Callback, HookResult> subscribe) { Handle = subscribe(Callback); }

        protected HookResult Handle { get; }

        public void Dispose() => Handle.Dispose();

        protected abstract bool Callback(CallbackData data);
    }
}