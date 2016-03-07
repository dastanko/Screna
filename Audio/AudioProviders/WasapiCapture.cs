using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Screna.Audio
{
    /// <summary>
    /// Represents state of a capture device
    /// </summary>
    public enum CaptureState
    {
        /// <summary>
        /// Not recording
        /// </summary>
        Stopped,
        /// <summary>
        /// Beginning to record
        /// </summary>
        Starting,
        /// <summary>
        /// Recording in progress
        /// </summary>
        Capturing,
        /// <summary>
        /// Requesting stop
        /// </summary>
        Stopping
    }

    /// <summary>
    /// Audio Capture using Wasapi
    /// </summary>
    public class WasapiCapture : IAudioProvider
    {
        const long ReftimesPerSec = 10000000,
            ReftimesPerMillisec = 10000;
        volatile CaptureState captureState;
        byte[] recordBuffer;
        Thread captureThread;
        AudioClient audioClient;
        int bytesPerFrame;
        WaveFormat waveFormat;
        bool initialized;
        readonly SynchronizationContext syncContext;
        readonly int audioBufferMillisecondsLength;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event Action<byte[], int> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event Action<Exception> RecordingStopped;

        public WasapiCapture() : this(DefaultDevice) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WasapiCapture" /> class.
        /// </summary>
        /// <param name="captureDevice">The capture device.</param>
        /// <param name="useEventSync">true if sync is done with event. false use sleep.</param>
        /// <param name="audioBufferMillisecondsLength">Length of the audio buffer in milliseconds. A lower value means lower latency but increased CPU usage.</param>
        public WasapiCapture(WasapiAudioDevice captureDevice, int audioBufferMillisecondsLength = 100)
        {
            syncContext = SynchronizationContext.Current;
            audioClient = captureDevice.AudioClient;
            this.audioBufferMillisecondsLength = audioBufferMillisecondsLength;

            waveFormat = audioClient.MixFormat;
        }

        /// <summary>
        /// Current Capturing State
        /// </summary>
        public CaptureState CaptureState => captureState;

        /// <summary>
        /// Capturing wave format
        /// </summary>
        public virtual WaveFormat WaveFormat
        {
            get
            {
                // for convenience, return a WAVEFORMATEX, instead of the real
                // WAVEFORMATEXTENSIBLE being used
                return waveFormat;
            }
            set { waveFormat = value; }
        }

        /// <summary>
        /// Gets the default audio capture device
        /// </summary>
        /// <returns>The default audio capture device</returns>
        public static WasapiAudioDevice DefaultDevice
        {
            get { return WasapiAudioDevice.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Console); }
        }

        public static IEnumerable<WasapiAudioDevice> EnumerateDevices()
        {
            return WasapiAudioDevice.EnumerateAudioEndPoints(DataFlow.Capture);
        }

        void Init()
        {
            if (initialized)
                return;

            long requestedDuration = ReftimesPerMillisec * audioBufferMillisecondsLength;

            if (!audioClient.IsFormatSupported(AudioClientShareMode.Shared, waveFormat))
                throw new ArgumentException("Unsupported Wave Format");

            var streamFlags = AudioClientStreamFlags;

            audioClient.Initialize(AudioClientShareMode.Shared,
                streamFlags,
                requestedDuration,
                0,
                waveFormat,
                Guid.Empty);
            
            int bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = waveFormat.Channels * waveFormat.BitsPerSample / 8;
            recordBuffer = new byte[bufferFrameCount * bytesPerFrame];

            initialized = true;
        }

        /// <summary>
        /// To allow overrides to specify different flags (e.g. loopback)
        /// </summary>
        protected virtual int AudioClientStreamFlags => 0;

        /// <summary>
        /// Start Capturing
        /// </summary>
        public virtual void Start()
        {
            if (captureState != CaptureState.Stopped)
                throw new InvalidOperationException("Previous recording still in progress");

            captureState = CaptureState.Starting;
            Init();
            ThreadStart start = () => CaptureThread(audioClient);
            captureThread = new Thread(start);
            captureThread.Start();
        }

        /// <summary>
        /// Stop Capturing (requests a stop, wait for RecordingStopped event to know it has finished)
        /// </summary>
        public virtual void Stop()
        {
            if (captureState != CaptureState.Stopped)
                captureState = CaptureState.Stopping;
        }

        void CaptureThread(AudioClient client)
        {
            Exception exception = null;
            try { DoRecording(client); }
            catch (Exception e) { exception = e; }
            finally
            {
                client.Stop();
                // don't dispose - the AudioClient only gets disposed when WasapiCapture is disposed
            }

            captureThread = null;
            captureState = CaptureState.Stopped;
            RaiseRecordingStopped(exception);
        }

        void DoRecording(AudioClient client)
        {
            int bufferFrameCount = client.BufferSize;

            // Calculate the actual duration of the allocated buffer.
            long actualDuration = (long)((double)ReftimesPerSec *
                             bufferFrameCount / waveFormat.SampleRate);

            int sleepMilliseconds = (int)(actualDuration / ReftimesPerMillisec / 2),
                waitMilliseconds = (int)(3 * actualDuration / ReftimesPerMillisec);

            var capture = client.AudioCaptureClient;

            client.Start();
            captureState = CaptureState.Capturing;

            while (captureState == CaptureState.Capturing)
            {
                Thread.Sleep(sleepMilliseconds);

                if (captureState != CaptureState.Capturing)
                    break;

                ReadNextPacket(capture);
            }
        }

        void RaiseRecordingStopped(Exception e)
        {
            var handler = RecordingStopped;
            if (handler == null) return;
            if (syncContext == null) handler(e);
            else syncContext.Post(state => handler(e), null);
        }

        void ReadNextPacket(AudioCaptureClient capture)
        {
            int packetSize = capture.GetNextPacketSize(),
                recordBufferOffset = 0;

            while (packetSize != 0)
            {
                int framesAvailable, flags;
                IntPtr buffer = capture.GetBuffer(out framesAvailable, out flags);

                int bytesAvailable = framesAvailable * bytesPerFrame;

                // apparently it is sometimes possible to read more frames than we were expecting?
                int spaceRemaining = Math.Max(0, recordBuffer.Length - recordBufferOffset);
                if (spaceRemaining < bytesAvailable && recordBufferOffset > 0)
                {
                    if (DataAvailable != null) DataAvailable(recordBuffer, recordBufferOffset);
                    recordBufferOffset = 0;
                }

                int AudioClientBufferFlags_Silent = 0x2;

                // if not silence...
                if ((flags & AudioClientBufferFlags_Silent) != AudioClientBufferFlags_Silent)
                    Marshal.Copy(buffer, recordBuffer, recordBufferOffset, bytesAvailable);
                else Array.Clear(recordBuffer, recordBufferOffset, bytesAvailable);

                recordBufferOffset += bytesAvailable;
                capture.ReleaseBuffer(framesAvailable);
                packetSize = capture.GetNextPacketSize();
            }

            DataAvailable?.Invoke(recordBuffer, recordBufferOffset);
        }

        public virtual void Dispose()
        {
            Stop();

            if (captureThread != null)
            {
                captureThread.Join();
                captureThread = null;
            }

            if (audioClient != null)
            {
                audioClient.Dispose();
                audioClient = null;
            }
        }

        public bool IsSynchronizable => false;
    }
}
