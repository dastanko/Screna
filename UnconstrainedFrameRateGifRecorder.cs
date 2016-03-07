using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Screna
{
    public class UnconstrainedFrameRateGifRecorder : IRecorder
    {
        #region Fields
        GifWriter VideoEncoder = null;
        IImageProvider ImageProvider = null;

        Thread RecordThread;

        ManualResetEvent StopCapturing = new ManualResetEvent(false),
            ContinueCapturing = new ManualResetEvent(false);
        #endregion

        ~UnconstrainedFrameRateGifRecorder() { Stop(); }

        public UnconstrainedFrameRateGifRecorder(GifWriter Encoder, IImageProvider ImageProvider)
        {
            // Init Fields
            this.ImageProvider = ImageProvider;
            this.VideoEncoder = Encoder;

            // RecordThread Init
            if (ImageProvider != null)
                RecordThread = new Thread(Record)
                {
                    Name = "Captura.Record",
                    IsBackground = true
                };


            // Not Actually Started, Waits for ContinueThread to be Set
            if (RecordThread != null) RecordThread.Start();
        }

        public event Action<Exception> RecordingStopped;

        public void Start(int Delay = 0)
        {
            new Thread(new ParameterizedThreadStart((e) =>
            {
                try
                {
                    Thread.Sleep((int)e);

                    if (RecordThread != null) ContinueCapturing.Set();
                }
                catch (Exception E) { RecordingStopped?.Invoke(E); }
            })).Start(Delay);
        }

        public void Stop()
        {
            // Resume if Paused
            if (ContinueCapturing != null) ContinueCapturing.Set();

            // Video
            if (RecordThread != null)
            {
                if (StopCapturing != null && !StopCapturing.SafeWaitHandle.IsClosed)
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

            // WaitHandles
            if (StopCapturing != null && !StopCapturing.SafeWaitHandle.IsClosed)
            {
                StopCapturing.Dispose();
                StopCapturing = null;
            }

            if (ContinueCapturing != null && !ContinueCapturing.SafeWaitHandle.IsClosed)
            {
                ContinueCapturing.Dispose();
                ContinueCapturing = null;
            }

            // Writers
            if (VideoEncoder != null)
            {
                VideoEncoder.Dispose();
                VideoEncoder = null;
            }
        }

        public void Pause() { if (RecordThread != null) ContinueCapturing.Reset(); }

        void Record()
        {
            try
            {
                DateTime LastFrameWriteTime = DateTime.MinValue;
                Bitmap Frame = null;
                Task LastFrameWriteTask = null;

                while (!StopCapturing.WaitOne(0) && ContinueCapturing.WaitOne())
                {
                    Frame = ImageProvider.Capture();

                    int Delay = LastFrameWriteTime == DateTime.MinValue ? 0
                        : (int)(DateTime.Now - LastFrameWriteTime).TotalMilliseconds;

                    LastFrameWriteTime = DateTime.Now;

                    LastFrameWriteTask = VideoEncoder.WriteFrameAsync(Frame, Delay);
                }

                // Wait for the last frame is written
                LastFrameWriteTask?.Wait();
            }
            catch (Exception E)
            {
                Stop();
                RecordingStopped?.Invoke(E);
            }
        }
    }
}
