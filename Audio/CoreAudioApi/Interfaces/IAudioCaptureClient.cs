using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioCaptureClient
    {
        int GetBuffer(
            out IntPtr dataBuffer,
            out int numFramesToRead,
            out int bufferFlags,
            out long devicePosition,
            out long qpcPosition);

        int ReleaseBuffer(int numFramesRead);

        int GetNextPacketSize(out int numFramesInNextPacket);
    }
}
