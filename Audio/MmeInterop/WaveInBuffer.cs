using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    /// <summary>
    /// A buffer of Wave samples
    /// </summary>
    class WaveInBuffer : IDisposable
    {
        readonly WaveHeader header;
        readonly byte[] buffer;
        GCHandle hBuffer,
            hHeader, // we need to pin the header structure
            hThis; // for the user callback
        IntPtr waveInHandle;

        /// <summary>
        /// creates a new wavebuffer
        /// </summary>
        /// <param name="waveInHandle">WaveIn device to write to</param>
        /// <param name="bufferSize">Buffer size in bytes</param>
        public WaveInBuffer(IntPtr waveInHandle, int bufferSize)
        {
            this.buffer = new byte[bufferSize];
            this.hBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            this.waveInHandle = waveInHandle;

            hThis = GCHandle.Alloc(this);

            header = new WaveHeader()
            {
                dataBuffer = hBuffer.AddrOfPinnedObject(),
                bufferLength = bufferSize,
                loops = 1,
                userData = (IntPtr)hThis
            };

            hHeader = GCHandle.Alloc(header, GCHandleType.Pinned);

            MmException.Try(WaveInterop.waveInPrepareHeader(waveInHandle, header, Marshal.SizeOf(header)), "waveInPrepareHeader");
        }

        /// <summary>
        /// Place this buffer back to record more audio
        /// </summary>
        public void Reuse()
        {
            // TEST: we might not actually need to bother unpreparing and repreparing
            MmException.Try(WaveInterop.waveInUnprepareHeader(waveInHandle, header, Marshal.SizeOf(header)), "waveUnprepareHeader");
            MmException.Try(WaveInterop.waveInPrepareHeader(waveInHandle, header, Marshal.SizeOf(header)), "waveInPrepareHeader");
            MmException.Try(WaveInterop.waveInAddBuffer(waveInHandle, header, Marshal.SizeOf(header)), "waveInAddBuffer");
        }

        /// <summary>
        /// Releases resources held by this WaveBuffer
        /// </summary>
        public void Dispose()
        {
            // free unmanaged resources
            if (waveInHandle != IntPtr.Zero)
            {
                WaveInterop.waveInUnprepareHeader(waveInHandle, header, Marshal.SizeOf(header));
                waveInHandle = IntPtr.Zero;
            }

            if (hHeader.IsAllocated) hHeader.Free();
            if (hBuffer.IsAllocated) hBuffer.Free();
            if (hThis.IsAllocated) hThis.Free();
        }

        /// <summary>
        /// Provides access to the actual record buffer (for reading only)
        /// </summary>
        public byte[] Data { get { return buffer; } }

        /// <summary>
        /// Indicates whether the Done flag is set on this buffer
        /// </summary>
        public bool Done { get { return (header.flags & WaveHeaderFlags.Done) == WaveHeaderFlags.Done; } }
        
        /// <summary>
        /// Indicates whether the InQueue flag is set on this buffer
        /// </summary>
        public bool InQueue { get { return (header.flags & WaveHeaderFlags.InQueue) == WaveHeaderFlags.InQueue; } }

        /// <summary>
        /// Number of bytes recorded
        /// </summary>
        public int BytesRecorded { get { return header.bytesRecorded; } }
    }
}
