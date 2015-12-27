using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPropertyStore
    {
        int GetCount(out int propCount);
        int GetAt(int property, out PropertyKey key);
        int GetValue(ref PropertyKey key, out PropString value);
        int SetValue(ref PropertyKey key, ref PropString value);
        int Commit();
    }
}
