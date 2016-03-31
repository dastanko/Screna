using System;

namespace Screna.Avi
{
    /// <summary>Number of bits per pixel.</summary>
    enum BitsPerPixel
    {
        /// <summary>8 bits per pixel.</summary>
        /// <remarks>
        /// When used with uncompressed video streams,
        /// a grayscale palette is implied.
        /// </remarks>
        Bpp8 = 8,
        /// <summary>16 bits per pixel.</summary>
        Bpp16 = 16,
        /// <summary>24 bits per pixel.</summary>
        Bpp24 = 24,
        /// <summary>32 bits per pixel.</summary>
        Bpp32 = 32
    }

    enum IndexType : byte
    {
        Indexes = 0x00,
        Chunks = 0x01,
        Data = 0x80
    }

    [Flags]
    enum MainHeaderFlags : uint
    {
        HasIndex = 0x00000010U,
        MustUseIndex = 0x00000020U,
        IsInterleaved = 0x00000100U,
        TrustChunkType = 0x00000800U,
        WasCaptureFile = 0x00010000U,
        Copyrighted = 0x000200000U
    }

    enum StreamHeaderFlags : uint
    {
        Disabled = 0x00000001,
        VideoPaletteChanges = 0x00010000
    }
}
