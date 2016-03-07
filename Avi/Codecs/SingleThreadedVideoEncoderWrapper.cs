using System;
using System.Threading;
using System.Windows.Threading;

namespace Screna.Avi
{
    /// <summary>
    /// Ensures that all access to the enclosed <see cref="IVideoEncoder"/> instance is made
    /// on a single thread.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Especially useful for unmanaged encoders like <see cref="Mpeg4VideoEncoderVcm"/> in multi-threaded scenarios
    /// like asynchronous encoding.
    /// </para>
    /// <para>
    /// Uses <see cref="Dispatcher"/> under the hood.
    /// </para>
    /// </remarks>
    class SingleThreadedVideoEncoderWrapper : IVideoEncoder, IDisposable
    {
        readonly IVideoEncoder encoder;
        readonly Thread thread;
        readonly Dispatcher dispatcher;

        /// <summary>
        /// Creates a new instance of <see cref="SingleThreadedVideoEncoderWrapper"/>.
        /// </summary>
        /// <param name="encoderFactory">
        /// Factory for creating an encoder instance.
        /// It will be invoked on the same thread as all subsequent operations of the <see cref="IVideoEncoder"/> interface.
        /// </param>
        public SingleThreadedVideoEncoderWrapper(Func<IVideoEncoder> encoderFactory)
        {
            this.thread = new Thread(RunDispatcher)
                {
                    IsBackground = true,
                    Name = typeof(SingleThreadedVideoEncoderWrapper).Name
                };

            var dispatcherCreated = new AutoResetEvent(false);
            thread.Start(dispatcherCreated);
            dispatcherCreated.WaitOne();
            this.dispatcher = Dispatcher.FromThread(thread);

            // TODO: Create encoder on the first frame
            this.encoder = (IVideoEncoder)dispatcher.Invoke(encoderFactory);
            if (encoder == null)
                throw new InvalidOperationException("Encoder factory has created no instance.");
        }

        /// <summary>
        /// Disposes the enclosed encoder and stops the internal thread.
        /// </summary>
        public void Dispose()
        {
            if (thread.IsAlive)
            {
                var encoderDisposable = encoder as IDisposable;
                if (encoderDisposable != null)
                    dispatcher.Invoke(new Action(encoderDisposable.Dispose));

                dispatcher.InvokeShutdown();
                thread.Join();
            }
        }

        /// <summary>Codec ID.</summary>
        public FourCC Codec => dispatcher.Invoke(() => encoder.Codec);

        /// <summary>
        /// Number of bits per pixel in encoded image.
        /// </summary>
        public BitsPerPixel BitsPerPixel => dispatcher.Invoke(() => encoder.BitsPerPixel);

        /// <summary>
        /// Determines the amount of space needed in the destination buffer for storing the encoded data of a single frame.
        /// </summary>
        public int MaxEncodedSize => dispatcher.Invoke(() => encoder.MaxEncodedSize);

        /// <summary>
        /// Encodes video frame.
        /// </summary>
        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            var result = (EncodeResult)dispatcher.Invoke(
                new Func<byte[], int, byte[], int, EncodeResult>(EncodeFrame),
                source, srcOffset, destination, destOffset);
            isKeyFrame = result.IsKeyFrame;
            return result.EncodedLength;
        }

        EncodeResult EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset)
        {
            bool isKeyFrame;
            var result = encoder.EncodeFrame(source, srcOffset, destination, destOffset, out isKeyFrame);
            return new EncodeResult
                {
                    EncodedLength = result,
                    IsKeyFrame = isKeyFrame
                };
        }

        struct EncodeResult
        {
            public int EncodedLength;
            public bool IsKeyFrame;
        }


        void RunDispatcher(object parameter)
        {
            AutoResetEvent dispatcherCreated = (AutoResetEvent)parameter;
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcherCreated.Set();

            Dispatcher.Run();
        }
    }
}