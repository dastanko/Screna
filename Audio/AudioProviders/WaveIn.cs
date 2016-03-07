using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Screna.Audio
{
    public class WaveInDevice
    {
        public int DeviceNumber { get; }

        public WaveInDevice(int deviceNumber) { DeviceNumber = deviceNumber; }

        public string Name => GetCapabilities(DeviceNumber).ProductName;

        /// <summary>
        /// Returns the number of Wave In devices available in the system
        /// </summary>
        public static int DeviceCount => WaveInterop.waveInGetNumDevs();

        /// <summary>
        /// Retrieves the capabilities of a waveIn device
        /// </summary>
        /// <param name="devNumber">Device to test</param>
        /// <returns>The WaveIn device capabilities</returns>
        static WaveInCapabilities GetCapabilities(int devNumber)
        {
            WaveInCapabilities caps = new WaveInCapabilities();
            int structSize = Marshal.SizeOf(caps);
            MmException.Try(WaveInterop.waveInGetDevCaps((IntPtr)devNumber, out caps, structSize), "waveInGetDevCaps");
            return caps;
        }

        /// <summary>
        /// Checks to see if a given SupportedWaveFormat is supported
        /// </summary>
        /// <param name="waveFormat">The SupportedWaveFormat</param>
        /// <returns>true if supported</returns>
        public bool SupportsWaveFormat(SupportedWaveFormat waveFormat) => GetCapabilities(DeviceNumber).SupportedFormats.HasFlag(waveFormat);

        public static IEnumerable<WaveInDevice> Enumerate()
        {
            int n = WaveInDevice.DeviceCount;

            for (var i = 0; i < n; i++)
                yield return new WaveInDevice(i);
        }

        public static WaveInDevice DefaultDevice => new WaveInDevice(0);
    }

    /// <summary>
    /// Recording using waveIn api with event callbacks.
    /// Use this for recording in non-gui applications
    /// Events are raised as recorded buffers are made available
    /// </summary>
    public class WaveIn : IAudioProvider
    {
        readonly AutoResetEvent callbackEvent;
        readonly SynchronizationContext syncContext;
        readonly int DeviceNumber;
        IntPtr waveInHandle;
        volatile bool recording;
        WaveInBuffer[] buffers;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event Action<byte[], int> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event Action<Exception> RecordingStopped;

        /// <summary>
        /// Prepares a Wave input device for recording
        /// </summary>
        public WaveIn(int DeviceNumber = 0)
        {
            callbackEvent = new AutoResetEvent(false);
            syncContext = SynchronizationContext.Current;
            this.DeviceNumber = DeviceNumber;
            WaveFormat = new WaveFormat(8000, 16, 1);
            BufferMilliseconds = 100;
            NumberOfBuffers = 3;
            IsSynchronizable = false;
        }

        public WaveIn(int DeviceNumber, int FrameRate, WaveFormat wf) : this(DeviceNumber)
        {
            WaveFormat = wf;
            // Buffer size to store duration of 1 frame 
            BufferMilliseconds = (int)Math.Ceiling(1000 / (decimal)FrameRate);
            IsSynchronizable = true;
        }

        /// <summary>
        /// Milliseconds for the buffer. Recommended value is 100ms
        /// </summary>
        public int BufferMilliseconds { get; set; }

        /// <summary>
        /// Number of Buffers to use (usually 2 or 3)
        /// </summary>
        public int NumberOfBuffers { get; set; }

        void CreateBuffers()
        {
            // Default to three buffers of 100ms each
            int bufferSize = BufferMilliseconds * WaveFormat.AverageBytesPerSecond / 1000;
            if (bufferSize % WaveFormat.BlockAlign != 0)
                bufferSize -= bufferSize % WaveFormat.BlockAlign;

            buffers = new WaveInBuffer[NumberOfBuffers];
            for (int n = 0; n < buffers.Length; n++)
                buffers[n] = new WaveInBuffer(waveInHandle, bufferSize);
        }

        void OpenWaveInDevice()
        {
            int CallbackEvent = 0x50000;

            CloseWaveInDevice();
            MmResult result = WaveInterop.waveInOpen(out waveInHandle, (IntPtr)DeviceNumber, WaveFormat,
                callbackEvent.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, CallbackEvent);
            MmException.Try(result, "waveInOpen");
            CreateBuffers();
        }

        /// <summary>
        /// Start recording
        /// </summary>
        public void Start()
        {
            if (recording)
                throw new InvalidOperationException("Already recording");
            OpenWaveInDevice();
            MmException.Try(WaveInterop.waveInStart(waveInHandle), "waveInStart");
            recording = true;
            ThreadPool.QueueUserWorkItem((state) => RecordThread(), null);
        }

        void RecordThread()
        {
            Exception exception = null;
            try { DoRecording(); }
            catch (Exception e) { exception = e; }
            finally
            {
                recording = false;
                RaiseRecordingStoppedEvent(exception);
            }
        }

        void DoRecording()
        {
            foreach (var buffer in buffers)
                if (!buffer.InQueue)
                    buffer.Reuse();

            while (recording)
            {
                if (callbackEvent.WaitOne())
                {
                    // requeue any buffers returned to us
                    if (recording)
                    {
                        foreach (var buffer in buffers)
                        {
                            if (buffer.Done)
                            {
                                DataAvailable?.Invoke(buffer.Data, buffer.BytesRecorded);

                                buffer.Reuse();
                            }
                        }
                    }
                }
            }
        }

        void RaiseRecordingStoppedEvent(Exception e)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                if (this.syncContext == null)
                    handler(e);
                else syncContext.Post(state => handler(e), null);
            }
        }

        public void Stop()
        {
            recording = false;
            this.callbackEvent.Set(); // signal the thread to exit
            MmException.Try(WaveInterop.waveInStop(waveInHandle), "waveInStop");
        }

        /// <summary>
        /// WaveFormat we are recording in
        /// </summary>
        public WaveFormat WaveFormat { get; set; }

        /// <summary>
        /// Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (recording)
                    Stop();

                CloseWaveInDevice();
            }
        }

        void CloseWaveInDevice()
        {
            // Some drivers need the reset to properly release buffers
            WaveInterop.waveInReset(waveInHandle);

            if (buffers != null)
            {
                for (int n = 0; n < buffers.Length; n++)
                    buffers[n].Dispose();
                buffers = null;
            }
            
            WaveInterop.waveInClose(waveInHandle);
            waveInHandle = IntPtr.Zero;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsSynchronizable { get; }
    }
}
