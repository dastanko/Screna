using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IAudioRenderClient
    {
        int GetBuffer(int numFramesRequested, out IntPtr dataBufferPointer);
        int ReleaseBuffer(int numFramesWritten, int bufferFlags);
    }
}
