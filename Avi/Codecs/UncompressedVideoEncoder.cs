namespace Screna.Avi
{
    /// <summary>
    /// Encodes frames in BGR24 format without compression.
    /// </summary>
    /// <remarks>
    /// The main purpose of this encoder is to flip bitmap vertically (from top-down to bottom-up)
    /// and to convert pixel format to 24 bits.
    /// </remarks>
    class UncompressedVideoEncoder : IVideoEncoder
    {
        readonly int width, height;
        readonly byte[] sourceBuffer;

        /// <summary>
        /// Creates a new instance of <see cref="UncompressedVideoEncoder"/>.
        /// </summary>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        public UncompressedVideoEncoder(int width, int height)
        {
            this.width = width;
            this.height = height;
            sourceBuffer = new byte[width * height * 4];
        }

        #region IVideoEncoder Members
        /// <summary>Video codec.</summary>
        public FourCC Codec => AviCodec.Uncompressed.FourCC;

        /// <summary>
        /// Number of bits per pixel in encoded image.
        /// </summary>
        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp24;

        /// <summary>
        /// Maximum size of encoded frame.
        /// </summary>
        public int MaxEncodedSize => width * height * 3;

        /// <summary>
        /// Encodes a frame.
        /// </summary>
        /// <seealso cref="IVideoEncoder.EncodeFrame"/>
        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            Extensions.FlipVertical(source, srcOffset, sourceBuffer, 0, height, width * 4);
            Extensions.Bgr32ToBgr24(sourceBuffer, 0, destination, destOffset, width * height);
            isKeyFrame = true;
            return MaxEncodedSize;
        }
        #endregion
    }
}
