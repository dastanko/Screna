using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Screna.Avi
{
    /// <summary>
    /// Encodes frames in Motion JPEG format.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The implementation relies on <see cref="JpegBitmapEncoder"/>.
    /// </para>
    /// <para>
    /// This encoder is not fully conformant to the Motion JPEG standard, as each encoded frame is a full JPEG picture 
    /// with its own Huffman tables, and not those fixed Huffman tables defined by the Motion JPEG standard. 
    /// However, (at least most) modern decoders for Motion JPEG properly handle this situation.
    /// This also produces a little overhead on the file size.
    /// </para>
    /// </remarks>
    class MotionJpegVideoEncoderWpf : IVideoEncoder
    {
        readonly Int32Rect rect;
        readonly int quality;
        readonly ThreadLocal<WriteableBitmap> bitmapHolder;

        /// <summary>
        /// Creates a new instance of <see cref="MotionJpegVideoEncoderWpf"/>.
        /// </summary>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="quality">
        /// Compression quality in the range [1..100].
        /// Less values mean less size and lower image quality.
        /// </param>
        public MotionJpegVideoEncoderWpf(int width, int height, int quality)
        {
            rect = new Int32Rect(0, 0, width, height);
            this.quality = quality;

            bitmapHolder = new ThreadLocal<WriteableBitmap>(
                () => new WriteableBitmap(rect.Width, rect.Height, 96, 96, PixelFormats.Bgr32, null));
        }
        
        #region IVideoEncoder Members
        /// <summary>Video codec.</summary>
        public FourCC Codec => AviCodec.MotionJpeg.FourCC;

        /// <summary>
        /// Number of bits per pixel in encoded image.
        /// </summary>
        public BitsPerPixel BitsPerPixel => Screna.Avi.BitsPerPixel.Bpp24;

        /// <summary>
        /// Maximum size of encoded frmae.
        /// </summary>
        public int MaxEncodedSize => Math.Max(rect.Width * rect.Height * 4, 1024);
        // Assume that JPEG is always less than raw bitmap when dimensions are not tiny

        /// <summary>
        /// Encodes a frame.
        /// </summary>
        /// <seealso cref="IVideoEncoder.EncodeFrame"/>
        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            var bitmap = bitmapHolder.Value;
            bitmap.WritePixels(rect, source, rect.Width * 4, srcOffset);

            var encoderImpl = new JpegBitmapEncoder { QualityLevel = quality };
            encoderImpl.Frames.Add(BitmapFrame.Create(bitmap));

            using (var stream = new MemoryStream(destination))
            {
                stream.Position = srcOffset;
                encoderImpl.Save(stream);
                stream.Flush();
                var length = stream.Position - srcOffset;
                stream.Close();

                isKeyFrame = true;

                return (int)length;
            }
        }
        #endregion
    }
}
