using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    [Guid("D666063F-1587-4E43-81F1-B948E807363F"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDevice
    {
        // activationParams is a propvariant
        int Activate(ref Guid id, int clsCtx, IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
        
        int OpenPropertyStore(int stgmAccess, out IPropertyStore properties);
        
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        
        int GetState(out int state);
    }

    /// <summary>
    /// implements IMMDeviceEnumerator
    /// </summary>
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    class MMDeviceEnumeratorComObject { }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(DataFlow dataFlow, int stateMask, out IMMDeviceCollection devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice endpoint);

        int GetDevice(string id, out IMMDevice deviceName);

        int RegisterEndpointNotificationCallback(IMMNotificationClient client);

        int UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMMDeviceCollection
    {
        int GetCount(out int numDevices);
        int Item(int deviceNumber, out IMMDevice device);
    }
}
