using System;
using System.Runtime.InteropServices;

namespace Screna.Avi
{
    /// <summary>
    /// Selected constants, structures and functions from Video for Windows APIs.
    /// </summary>
    /// <remarks>
    /// Useful for implementing stream encoding using VCM codecs.
    /// See Windows API documentation on the meaning and usage of all this stuff.
    /// </remarks>
    static class VfwApi
    {
        public const int ICERR_OK = 0;

        public const short ICMODE_COMPRESS = 1;

        public const int ICCOMPRESS_KEYFRAME = 0x00000001;

        public const int AVIIF_KEYFRAME = 0x00000010;

        const int VIDCF_QUALITY = 0x0001;
        const int VIDCF_COMPRESSFRAMES = 0x0008;
        const int VIDCF_FASTTEMPORALC = 0x0020;

        public const int ICM_COMPRESS_GET_SIZE = 0x4005;
        public const int ICM_COMPRESS_QUERY = 0x4006;
        public const int ICM_COMPRESS_BEGIN = 0x4007;
        public const int ICM_COMPRESS_END = 0x4009;
        public const int ICM_COMPRESS_FRAMES_INFO = 0x4046;


        /// <summary>
        /// Corresponds to the <c>BITMAPINFOHEADER</c> structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BitmapInfoHeader
        {
            public uint SizeOfStruct;
            public int Width;
            public int Height;
            public ushort Planes;
            public ushort BitCount;
            public uint Compression;
            public uint ImageSize;
            public int PixelsPerMeterX;
            public int PixelsPerMeterY;
            public uint ColorsUsed;
            public uint ColorsImportant;
        }

        /// <summary>
        /// Corresponds to the <c>ICINFO</c> structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public unsafe struct CompressorInfo
        {
            uint sizeOfStruct;
            uint fccType;
            uint fccHandler;
            uint flags;
            uint version;
            uint versionIcm;
            fixed char szName[16];
            fixed char szDescription[128];
            fixed char szDriver[128];

            public bool SupportsQuality
            {
                get { return (flags & VIDCF_QUALITY) == VIDCF_QUALITY; }
            }

            public bool SupportsFastTemporalCompression
            {
                get { return (flags & VIDCF_FASTTEMPORALC) == VIDCF_FASTTEMPORALC; }
            }

            public bool RequestsCompressFrames
            {
                get { return (flags & VIDCF_COMPRESSFRAMES) == VIDCF_COMPRESSFRAMES; }
            }

            public string Name
            {
                get { fixed (char* name = szName) return new string(name); }
            }

            public string Description
            {
                get
                {
                    fixed (char* desc = szDescription) return new string(desc);
                }
            }
        }

        /// <summary>
        /// Corresponds to the <c>ICCOMPRESSFRAMES</c> structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CompressFramesInfo
        {
            uint flags;
            public IntPtr OutBitmapInfoPtr;
            int outputSize;
            public IntPtr InBitmapInfoPtr;
            int inputSize;
            public int StartFrame;
            public int FrameCount;
            /// <summary>Quality from 0 to 10000.</summary>
            public int Quality;
            int dataRate;
            /// <summary>Interval between key frames.</summary>
            /// <remarks>Equal to 1 if each frame is a key frame.</remarks>
            public int KeyRate;
            /// <summary></summary>
            public uint FrameRateNumerator;
            public uint FrameRateDenominator;
            uint overheadPerFrame;
            uint reserved2;
            IntPtr getDataFuncPtr;
            IntPtr setDataFuncPtr;
        }

        const string VFW_DLL = "msvfw32.dll";

        [DllImport(VFW_DLL, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr ICOpen(uint fccType, uint fccHandler, int mode);

        [DllImport(VFW_DLL, CallingConvention = CallingConvention.Winapi)]
        public static extern int ICClose(IntPtr handle);

        [DllImport(VFW_DLL, CallingConvention = CallingConvention.Winapi)]
        public static extern int ICSendMessage(IntPtr handle, int message, IntPtr param1, IntPtr param2);

        [DllImport(VFW_DLL, CallingConvention = CallingConvention.Winapi)]
        public static extern int ICSendMessage(IntPtr handle, int message, ref BitmapInfoHeader inHeader, ref BitmapInfoHeader outHeader);

        [DllImport(VFW_DLL, CallingConvention = CallingConvention.Winapi)]
        public static extern int ICSendMessage(IntPtr handle, int message, ref CompressFramesInfo info, int sizeOfInfo);

        [DllImport(VFW_DLL, CallingConvention = CallingConvention.Winapi)]
        public static extern int ICGetInfo(IntPtr handle, out CompressorInfo info, int infoSize);

        [DllImport(VFW_DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ICCompress(IntPtr handle, int inFlags,
                                             ref BitmapInfoHeader outHeader, IntPtr encodedData,
                                             ref BitmapInfoHeader inHeader, IntPtr frameData,
                                             out int chunkID, out int outFlags, int frameNumber,
                                             int requestedFrameSize, int requestedQuality,
                                             IntPtr prevHeaderPtr, IntPtr prevFrameData);
    }
}
