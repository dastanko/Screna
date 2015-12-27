using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct WaveInCapabilities
    {
        short manufacturerId, productId;
        int driverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string ProductName;
        public SupportedWaveFormat SupportedFormats;
        short channels, reserved;
        Guid manufacturerGuid, productGuid, nameGuid;
    }
}
