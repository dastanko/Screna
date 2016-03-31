// Adapted from SharpAvi Screencast Sample by Vasilli Masillov
using Screna.Audio;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Screna
{
    public class Recorder : IRecorder
    {
        #region Fields
        IAudioProvider AudioProvider = null;
        IVideoFileWriter VideoEncoder = null;
        IImageProvider ImageProvider = null;

        Thread RecordThread;

        ManualResetEvent StopCapturing = new ManualResetEvent(false),
            ContinueCapturing = new ManualResetEvent(false);
        AutoResetEvent VideoFrameWritten = new AutoResetEvent(false),
            AudioBlockWritten = new AutoResetEvent(false);

        SynchronizationContext syncContext;
        #endregion

        ~Recorder() { Stop(); }

        public event Action<Exception> RecordingStopped;

        void RaiseRecordingStopped(Exception e)
        {
            var handler = RecordingStopped;

            if (handler != null)
            {
                if (syncContext != null) syncContext.Post(s => handler(e), null);
                else handler(e);
            }
        }

        public Recorder(IVideoFileWriter Encoder, IImageProvider ImageProvider, IAudioProvider AudioProvider = null)
        {
            // Init Fields
            this.ImageProvider = ImageProvider;
            VideoEncoder = Encoder;
            this.AudioProvider = AudioProvider;

            syncContext = SynchronizationContext.Current;

            // Audio Init
            if (VideoEncoder.RecordsAudio
                && AudioProvider != null)
                AudioProvider.DataAvailable += AudioDataAvailable;
            else this.AudioProvider = null;

            // RecordThread Init
            if (ImageProvider != null)
                RecordThread = new Thread(Record)
                {
                    Name = "Captura.Record",
                    IsBackground = true
                };

            // Not Actually Started, Waits for ContinueThread to be Set
            RecordThread?.Start();
        }

        public void Start(int Delay = 0)
        {
            new Thread(() =>
                {
                    try
                    {
                        Thread.Sleep(Delay);

                        if (RecordThread != null)
                            ContinueCapturing.Set();

                        if (AudioProvider != null)
                        {
                            VideoFrameWritten.Set();
                            AudioBlockWritten.Reset();

                            AudioProvider.Start();
                        }
                    }
                    catch (Exception E) { RaiseRecordingStopped(E); }
                }).Start();
        }

        public void Pause()
        {
            if (RecordThread != null)
                ContinueCapturing.Reset();

            AudioProvider?.Stop();
        }

        public void Stop()
        {
            // Resume if Paused
            ContinueCapturing?.Set();

            // Video
            if (RecordThread != null)
            {
                if (StopCapturing != null
                    && !StopCapturing.SafeWaitHandle.IsClosed)
                    StopCapturing.Set();

                if (!RecordThread.Join(500))
                    RecordThread.Abort();

                RecordThread = null;
            }

            if (ImageProvider != null)
            {
                ImageProvider.Dispose();
                ImageProvider = null;
            }

            // Audio Source
            if (AudioProvider != null)
            {
                AudioProvider.Dispose();
                AudioProvider = null;
            }

            // WaitHandles
            if (StopCapturing != null
                && !StopCapturing.SafeWaitHandle.IsClosed)
            {
                StopCapturing.Dispose();
                StopCapturing = null;
            }

            if (ContinueCapturing != null
                && !ContinueCapturing.SafeWaitHandle.IsClosed)
            {
                ContinueCapturing.Dispose();
                ContinueCapturing = null;
            }

            // Writers
            if (VideoEncoder == null)
                return;

            VideoEncoder.Dispose();
            VideoEncoder = null;
        }

        void Record()
        {
            try
            {
                var FrameInterval = TimeSpan.FromSeconds(1 / (double)VideoEncoder.FrameRate);
                Task FrameWriteTask = null;
                var TimeTillNextFrame = TimeSpan.Zero;

                while (!StopCapturing.WaitOne(TimeTillNextFrame)
                    && ContinueCapturing.WaitOne())
                {
                    var Timestamp = DateTime.Now;

                    var Frame = ImageProvider.Capture();

                    // Wait for the previous frame is written
                    if (FrameWriteTask != null)
                    {
                        FrameWriteTask.Wait();
                        VideoFrameWritten.Set();
                    }

                    if (AudioProvider != null
                        && AudioProvider.IsSynchronizable)
                        if (WaitHandle.WaitAny(new WaitHandle[] { AudioBlockWritten, StopCapturing }) == 1)
                            break;

                    // Start asynchronous (encoding and) writing of the new frame
                    FrameWriteTask = VideoEncoder.WriteFrameAsync(Frame);

                    TimeTillNextFrame = Timestamp + FrameInterval - DateTime.Now;
                    if (TimeTillNextFrame < TimeSpan.Zero)
                        TimeTillNextFrame = TimeSpan.Zero;
                }

                // Wait for the last frame is written
                FrameWriteTask?.Wait();
            }
            catch (Exception E)
            {
                Stop();

                RaiseRecordingStopped(E);
            }
        }

        void AudioDataAvailable(byte[] Buffer, int BytesRecorded)
        {
            if (AudioProvider.IsSynchronizable)
            {
                if (WaitHandle.WaitAny(new WaitHandle[] {VideoFrameWritten, StopCapturing}) != 0)
                    return;

                VideoEncoder.WriteAudio(Buffer, BytesRecorded);

                AudioBlockWritten.Set();
            }
            else VideoEncoder.WriteAudio(Buffer, BytesRecorded);
        }
    }
}
