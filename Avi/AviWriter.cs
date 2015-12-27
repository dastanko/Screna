using Screna.Audio;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Screna.Avi
{
    public class AviWriter : IVideoFileWriter
    {
        #region Fields
        AviInternalWriter Writer;
        IAviVideoStream VideoStream;
        IAviAudioStream AudioStream;
        IAudioProvider AudioFacade;
        byte[] VideoBuffer;

        public int FrameRate { get; private set; }

        public bool RecordsAudio { get { return AudioStream != null; } }
        #endregion

        public AviWriter(string FileName,
            IImageProvider ImageProvider,
            AviCodec Codec,
            int Quality = 70,
            int FrameRate = 10,
            IAudioProvider AudioFacade = null,
            IAudioEncoder AudioEncoder = null)
        {
            this.FrameRate = FrameRate;
            this.AudioFacade = AudioFacade;

            Writer = new AviInternalWriter(FileName)
            {
                FramesPerSecond = FrameRate,
                EmitIndex1 = true,
            };

            CreateVideoStream(ImageProvider.Width, ImageProvider.Height, Quality, Codec);

            if (AudioFacade != null) CreateAudioStream(AudioFacade, AudioEncoder);

            VideoBuffer = new byte[ImageProvider.Width * ImageProvider.Height * 4];
        }

        public Task WriteFrameAsync(Bitmap Image)
        {
            var bits = Image.LockBits(new Rectangle(Point.Empty, Image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            Marshal.Copy(bits.Scan0, VideoBuffer, 0, VideoBuffer.Length);
            Image.UnlockBits(bits);

            return VideoStream.WriteFrameAsync(true, VideoBuffer, 0, VideoBuffer.Length);
        }

        #region Private Methods
        void CreateVideoStream(int Width, int Height, int Quality, AviCodec Codec)
        {
            // Select encoder type based on FOURCC of codec
            if (Codec == AviCodec.Uncompressed)
                VideoStream = Writer.AddUncompressedVideoStream(Width, Height);
            else if (Codec == AviCodec.MotionJpeg)
                VideoStream = Writer.AddMotionJpegVideoStream(Width, Height, Quality);
            else
            {
                VideoStream = Writer.AddMpeg4VideoStream(Width, Height,
                    (double)Writer.FramesPerSecond,
                    // It seems that all tested MPEG-4 VfW codecs ignore the quality affecting parameters passed through VfW API
                    // They only respect the settings from their own configuration dialogs, and Mpeg4VideoEncoder currently has no support for this
                    quality: Quality,
                    codec: Codec,
                    // Most of VfW codecs expect single-threaded use, so we wrap this encoder to special wrapper
                    // Thus all calls to the encoder (including its instantiation) will be invoked on a single thread although encoding (and writing) is performed asynchronously
                    forceSingleThreadedAccess: true);
            }

            VideoStream.Name = "ScrenaVideo";
        }

        void CreateAudioStream(IAudioProvider AudioFacade, IAudioEncoder AudioEncoder)
        {
            var wf = AudioFacade.WaveFormat;

            // Create encoding or simple stream based on settings
            AudioStream = AudioEncoder != null 
                ? Writer.AddEncodingAudioStream(AudioEncoder)
                : Writer.AddAudioStream(wf);

            AudioStream.Name = "ScrenaAudio";
        }
        #endregion

        public static IEnumerable<AviCodec> EnumerateEncoders()
        {
            yield return AviCodec.Uncompressed;
            yield return AviCodec.MotionJpeg;
            foreach (var Codec in Mpeg4VideoEncoderVcm.GetAvailableCodecs())
                yield return Codec;
        }

        public void WriteAudio(byte[] Buffer, int Length)
        {
            if (AudioStream != null)
                AudioStream.WriteBlock(Buffer, 0, Length);
        }

        public void Dispose()
        {
            Writer.Close();
            Writer = null;

            if (AudioFacade != null)
            {
                AudioFacade.Dispose();
                AudioFacade = null;
            }
        }
    }
}
