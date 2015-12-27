using Screna.Audio;
using System;

namespace Screna.Avi
{
    /// <summary>
    /// Provides extension methods for creating encoding streams with specific encoders.
    /// </summary>
    static class EncodingStreamFactory
    {
        /// <summary>
        /// Adds new video stream with <see cref="UncompressedVideoEncoder"/>.
        /// </summary>
        /// <seealso cref="AviInternalWriter.AddEncodingVideoStream"/>
        /// <seealso cref="UncompressedVideoEncoder"/>
        public static IAviVideoStream AddUncompressedVideoStream(this AviInternalWriter writer,
                                                                    int width,
                                                                    int height)
        {
            var encoder = new UncompressedVideoEncoder(width, height);
            return writer.AddEncodingVideoStream(encoder, true, width, height);
        }

        /// <summary>
        /// Adds new video stream with <see cref="MotionJpegVideoEncoderWpf"/>.
        /// </summary>
        /// <param name="writer">Writer object to which new stream is added.</param>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="quality">Requested quality of compression.</param>
        /// <seealso cref="AviInternalWriter.AddEncodingVideoStream"/>
        /// <seealso cref="MotionJpegVideoEncoderWpf"/>
        public static IAviVideoStream AddMotionJpegVideoStream(this AviInternalWriter writer, 
                                                                int width, 
                                                                int height,
                                                                int quality = 70)
        {
            var encoder = new MotionJpegVideoEncoderWpf(width, height, quality);
            return writer.AddEncodingVideoStream(encoder, true, width, height);
        }

        /// <summary>
        /// Adds new video stream with <see cref="Mpeg4VideoEncoderVcm"/>.
        /// </summary>
        /// <param name="writer">Writer object to which new stream is added.</param>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="fps">Frames rate of the video.</param>
        /// <param name="frameCount">Number of frames if known in advance. Otherwise, specify <c>0</c>.</param>
        /// <param name="quality">Requested quality of compression.</param>
        /// <param name="codec">Specific MPEG-4 codec to use.</param>
        /// <param name="forceSingleThreadedAccess">
        /// When <c>true</c>, the created <see cref="Mpeg4VideoEncoderVcm"/> instance is wrapped into
        /// <see cref="SingleThreadedVideoEncoderWrapper"/>.
        /// </param>
        /// <seealso cref="AviInternalWriter.AddEncodingVideoStream"/>
        /// <seealso cref="Mpeg4VideoEncoderVcm"/>
        /// <seealso cref="SingleThreadedVideoEncoderWrapper"/>
        public static IAviVideoStream AddMpeg4VideoStream(this AviInternalWriter writer,
                                                            int width,
                                                            int height, 
                                                            double fps,
                                                            int frameCount = 0,
                                                            int quality = 70,
                                                            AviCodec codec = null, 
                                                            bool forceSingleThreadedAccess = false)
        {
            var encoderFactory = codec != null
                ? new Func<IVideoEncoder>(() => new Mpeg4VideoEncoderVcm(width, height, fps, frameCount, quality, codec.FourCC))
                : new Func<IVideoEncoder>(() => new Mpeg4VideoEncoderVcm(width, height, fps, frameCount, quality));
            var encoder = forceSingleThreadedAccess
                ? new SingleThreadedVideoEncoderWrapper(encoderFactory)
                : encoderFactory.Invoke();
            return writer.AddEncodingVideoStream(encoder, true, width, height);
        }

        /// <summary>
        /// Adds new audio stream with <see cref="Mp3AudioEncoderLame"/>.
        /// </summary>
        /// <seealso cref="AviInternalWriter.AddEncodingAudioStream"/>
        /// <seealso cref="Mp3AudioEncoderLame"/>
        public static IAviAudioStream AddMp3AudioStream(this AviInternalWriter writer,
                                                        int channelCount = 2,
                                                        int sampleRate = 44100,
                                                        int outputBitRateKbps = 160)
        {
            var encoder = new Mp3EncoderLame(channelCount, sampleRate, outputBitRateKbps);
            return writer.AddEncodingAudioStream(encoder, true);
        }
    }
}
