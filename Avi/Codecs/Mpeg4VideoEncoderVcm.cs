using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace Screna.Avi
{
    /// <summary>
    /// Encodes video stream in MPEG-4 format using one of VCM codecs installed on the system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supported codecs include Microsoft MPEG-4 V3 and V2, Xvid, DivX and x264vfw.
    /// The codec to be used is selected from the ones installed on the system.
    /// The encoder can be forced to use MPEG-4 codecs that are not explicitly supported. However, in this case
    /// it is not guaranteed to work properly.
    /// </para>
    /// <para>
    /// For <c>x264vfw</c> codec, it is recommended to enable <c>Zero Latency</c> option in its settings.
    /// 64-bit support is limited, as there are no 64-bit versions of Microsoft and DivX codecs, 
    /// and Xvid can produce some errors.
    /// </para>
    /// <para>
    /// In multi-threaded scenarios, like asynchronous encoding, it is recommended to wrap this encoder into
    /// <see cref="SingleThreadedVideoEncoderWrapper"/> for the stable work.
    /// </para>
    /// </remarks>
    class Mpeg4VideoEncoderVcm : IVideoEncoder, IDisposable
    {
        /// <summary>
        /// Default preferred order of the supported codecs.
        /// </summary>
        public static ReadOnlyCollection<FourCC> DefaultCodecPreference { get; } = new ReadOnlyCollection<FourCC>(
            new[]
            {
                AviCodec.MicrosoftMpeg4V3.FourCC,
                AviCodec.MicrosoftMpeg4V2.FourCC,
                AviCodec.Xvid.FourCC,
                AviCodec.X264.FourCC,
                AviCodec.DivX.FourCC
            });

        /// <summary>
        /// Gets info about the supported codecs that are installed on the system.
        /// </summary>
        public static IEnumerable<AviCodec> GetAvailableCodecs()
        {
            var inBitmapInfo = CreateBitmapInfo(8, 8, 32, AviCodec.Uncompressed.FourCC);
            inBitmapInfo.ImageSize = 4;

            foreach (var codec in DefaultCodecPreference)
            {
                var outBitmapInfo = CreateBitmapInfo(8, 8, 24, codec);
                VfwApi.CompressorInfo compressorInfo;
                var compressorHandle = GetCompressor(inBitmapInfo, outBitmapInfo, out compressorInfo);
                if (compressorHandle != IntPtr.Zero)
                {
                    VfwApi.ICClose(compressorHandle);
                    yield return new AviCodec(codec, compressorInfo.Description);
                }
            }
        }

        static IntPtr GetCompressor(VfwApi.BitmapInfoHeader inBitmapInfo, VfwApi.BitmapInfoHeader outBitmapInfo, out VfwApi.CompressorInfo compressorInfo)
        {
            // Using ICLocate is time-consuming. Besides, it does not clean up something, so the process does not terminate on exit.
            // Instead open a specific codec and query it for needed features.

            var CodecType_Video = new FourCC("VIDC");

            var compressorHandle = VfwApi.ICOpen((uint)CodecType_Video, outBitmapInfo.Compression, VfwApi.ICMODE_COMPRESS);

            if (compressorHandle != IntPtr.Zero)
            {
                var inHeader = inBitmapInfo;
                var outHeader = outBitmapInfo;
                var result = VfwApi.ICSendMessage(compressorHandle, VfwApi.ICM_COMPRESS_QUERY, ref inHeader, ref outHeader);

                if (result == VfwApi.ICERR_OK)
                {
                    var infoSize = VfwApi.ICGetInfo(compressorHandle, out compressorInfo, Marshal.SizeOf(typeof(VfwApi.CompressorInfo)));
                    if (infoSize > 0 && compressorInfo.SupportsFastTemporalCompression)
                        return compressorHandle;
                }

                VfwApi.ICClose(compressorHandle);
            }

            compressorInfo = new VfwApi.CompressorInfo();
            return IntPtr.Zero;
        }

        static VfwApi.BitmapInfoHeader CreateBitmapInfo(int width, int height, ushort bitCount, FourCC codec)
        {
            return new VfwApi.BitmapInfoHeader
            {
                SizeOfStruct = (uint)Marshal.SizeOf(typeof(VfwApi.BitmapInfoHeader)),
                Width = width,
                Height = height,
                BitCount = bitCount,
                Planes = 1,
                Compression = (uint)codec
            };
        }


        readonly int width, height;
        readonly byte[] sourceBuffer;
        readonly VfwApi.BitmapInfoHeader inBitmapInfo, outBitmapInfo;
        readonly IntPtr compressorHandle;
        readonly VfwApi.CompressorInfo compressorInfo;
        readonly int quality, keyFrameRate;


        int frameIndex = 0, framesFromLastKey;
        bool isDisposed, needEnd;

        /// <summary>
        /// Creates a new instance of <see cref="Mpeg4VideoEncoderVcm"/>.
        /// </summary>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="fps">Frame rate.</param>
        /// <param name="frameCount">
        /// Number of frames to be encoded.
        /// If not known, specify 0.
        /// </param>
        /// <param name="quality">
        /// Compression quality in the range [1..100].
        /// Less values mean less size and lower image quality.
        /// </param>
        /// <param name="codecPreference">
        /// List of codecs that can be used by this encoder, in preferred order.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// No compatible codec was found in the system.
        /// </exception>
        /// <remarks>
        /// <para>
        /// It is not guaranteed that the codec will respect the specified <paramref name="quality"/> value.
        /// This depends on its implementation.
        /// </para>
        /// <para>
        /// If no preferred codecs are specified, then <see cref="DefaultCodecPreference"/> is used.
        /// MPEG-4 codecs that are not explicitly supported can be specified. However, in this case
        /// the encoder is not guaranteed to work properly.
        /// </para>
        /// </remarks>
        public Mpeg4VideoEncoderVcm(int width, int height, double fps, int frameCount, int quality, params FourCC[] codecPreference)
        {
            this.width = width;
            this.height = height;
            sourceBuffer = new byte[width * height * 4];

            inBitmapInfo = CreateBitmapInfo(width, height, 32, AviCodec.Uncompressed.FourCC);
            inBitmapInfo.ImageSize = (uint)sourceBuffer.Length;

            if (codecPreference == null || codecPreference.Length == 0)
                codecPreference = DefaultCodecPreference.ToArray();

            foreach (var codec in codecPreference)
            {
                outBitmapInfo = CreateBitmapInfo(width, height, 24, codec);
                compressorHandle = GetCompressor(inBitmapInfo, outBitmapInfo, out compressorInfo);
                if (compressorHandle != IntPtr.Zero)
                    break;
            }

            if (compressorHandle == IntPtr.Zero)
                throw new InvalidOperationException("No compatible MPEG-4 encoder found.");

            try
            {
                MaxEncodedSize = GetMaxEncodedSize();

                // quality for ICM ranges from 0 to 10000
                this.quality = compressorInfo.SupportsQuality ? quality * 100 : 0;

                // typical key frame rate ranges from FPS to 2*FPS
                keyFrameRate = (int)Math.Round((2 - 0.01 * quality) * fps);

                if (compressorInfo.RequestsCompressFrames)
                    InitCompressFramesInfo(fps, frameCount);

                StartCompression();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Performs any necessary cleanup before this instance is garbage-collected.
        /// </summary>
        ~Mpeg4VideoEncoderVcm() { Dispose(); }

        int GetMaxEncodedSize()
        {
            var inHeader = inBitmapInfo;
            var outHeader = outBitmapInfo;
            return VfwApi.ICSendMessage(compressorHandle, VfwApi.ICM_COMPRESS_GET_SIZE, ref inHeader, ref outHeader);
        }

        void InitCompressFramesInfo(double fps, int frameCount)
        {
            var info = new VfwApi.CompressFramesInfo
            {
                StartFrame = 0,
                FrameCount = frameCount,
                Quality = quality,
                KeyRate = keyFrameRate
            };
            Extensions.SplitFrameRate((decimal)fps, out info.FrameRateNumerator, out info.FrameRateDenominator);

            var result = VfwApi.ICSendMessage(compressorHandle, VfwApi.ICM_COMPRESS_FRAMES_INFO, ref info, Marshal.SizeOf(typeof(VfwApi.CompressFramesInfo)));
            CheckICResult(result);
        }

        void StartCompression()
        {
            var inHeader = inBitmapInfo;
            var outHeader = outBitmapInfo;
            var result = VfwApi.ICSendMessage(compressorHandle, VfwApi.ICM_COMPRESS_BEGIN, ref inHeader, ref outHeader);
            CheckICResult(result);

            needEnd = true;
            framesFromLastKey = keyFrameRate;
        }

        void EndCompression()
        {
            var result = VfwApi.ICSendMessage(compressorHandle, VfwApi.ICM_COMPRESS_END, IntPtr.Zero, IntPtr.Zero);
            CheckICResult(result);
        }


        #region IVideoEncoder Members

        /// <summary>Video codec.</summary>
        public FourCC Codec => outBitmapInfo.Compression;

        /// <summary>Number of bits per pixel in the encoded image.</summary>
        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp24;

        /// <summary>
        /// Maximum size of the encoded frame.
        /// </summary>
        public int MaxEncodedSize { get; }

        /// <summary>Encodes a frame.</summary>
        /// <seealso cref="IVideoEncoder.EncodeFrame"/>
        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            Extensions.FlipVertical(source, srcOffset, sourceBuffer, 0, height, width * 4);

            var sourceHandle = GCHandle.Alloc(sourceBuffer, GCHandleType.Pinned);
            var encodedHandle = GCHandle.Alloc(destination, GCHandleType.Pinned);

            try
            {
                var outInfo = outBitmapInfo;
                outInfo.ImageSize = (uint)destination.Length;
                var inInfo = inBitmapInfo;
                int outFlags, chunkID,
                    flags = framesFromLastKey >= keyFrameRate ? VfwApi.ICCOMPRESS_KEYFRAME : 0;

                var result = VfwApi.ICCompress(compressorHandle, flags,
                    ref outInfo, encodedHandle.AddrOfPinnedObject(), ref inInfo, sourceHandle.AddrOfPinnedObject(),
                    out chunkID, out outFlags, frameIndex,
                    0, quality, IntPtr.Zero, IntPtr.Zero);
                CheckICResult(result);
                frameIndex++;

                isKeyFrame = (outFlags & VfwApi.AVIIF_KEYFRAME) == VfwApi.AVIIF_KEYFRAME;

                if (isKeyFrame) framesFromLastKey = 1;
                else framesFromLastKey++;

                return (int)outInfo.ImageSize;
            }
            finally
            {
                sourceHandle.Free();
                encodedHandle.Free();
            }
        }
        #endregion

        /// <summary>
        /// Releases all unmanaged resources used by the encoder.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                if (needEnd) EndCompression();

                if (compressorHandle != IntPtr.Zero)
                    VfwApi.ICClose(compressorHandle);

                isDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        void CheckICResult(int result)
        {
            if (result != VfwApi.ICERR_OK)
                throw new InvalidOperationException($"Encoder operation returned an error: {result}.");
        }
    }
}
