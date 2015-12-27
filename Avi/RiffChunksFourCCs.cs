namespace Screna.Avi
{
    /// <summary>
    /// RIFF chunk indentifiers used in AVI format.
    /// </summary>
    static class RIFFChunksFourCCs
    {
        /// <summary>Main AVI header.</summary>
        public static readonly FourCC AviHeader = new FourCC("avih");

        /// <summary>Stream header.</summary>
        public static readonly FourCC StreamHeader = new FourCC("strh");

        /// <summary>Stream format.</summary>
        public static readonly FourCC StreamFormat = new FourCC("strf");

        /// <summary>Stream name.</summary>
        public static readonly FourCC StreamName = new FourCC("strn");

        /// <summary>Stream index.</summary>
        public static readonly FourCC StreamIndex = new FourCC("indx");

        /// <summary>Index v1.</summary>
        public static readonly FourCC Index1 = new FourCC("idx1");

        /// <summary>OpenDML header.</summary>
        public static readonly FourCC OpenDmlHeader = new FourCC("dmlh");

        /// <summary>Junk chunk.</summary>
        public static readonly FourCC Junk = new FourCC("JUNK");

        /// <summary>Gets the identifier of a video frame chunk.</summary>
        /// <param name="streamIndex">Sequential number of the stream.</param>
        /// <param name="compressed">Whether stream contents is compressed.</param>
        public static FourCC VideoFrame(int streamIndex, bool compressed)
        {
            return string.Format(compressed ? "{0:00}dc" : "{0:00}db", streamIndex);
        }

        /// <summary>Gets the identifier of an audio data chunk.</summary>
        /// <param name="streamIndex">Sequential number of the stream.</param>
        public static FourCC AudioData(int streamIndex) { return string.Format("{0:00}wb", streamIndex); }

        /// <summary>Gets the identifier of an index chunk.</summary>
        /// <param name="streamIndex">Sequential number of the stream.</param>
        public static FourCC IndexData(int streamIndex) { return string.Format("ix{0:00}", streamIndex); }
    }
}
