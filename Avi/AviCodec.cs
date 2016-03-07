namespace Screna.Avi
{
    public class AviCodec
    {
        internal FourCC FourCC { get; }

        public string Name { get; }

        internal AviCodec(FourCC FourCC, string Name)
        {
            this.FourCC = FourCC;
            this.Name = Name;
        }

        public AviCodec(string Name)
        {
            this.FourCC = new FourCC("____");
            this.Name = Name;
        }

        /// <summary>Identifier used for non-compressed data.</summary>
        public static readonly AviCodec Uncompressed = new AviCodec(new FourCC(0), "Uncompressed");

        /// <summary>Motion JPEG.</summary>
        public static readonly AviCodec MotionJpeg = new AviCodec(new FourCC("MJPG"), "Motion Jpeg");

        /// <summary>Microsoft MPEG-4 V3.</summary>
        public static readonly AviCodec MicrosoftMpeg4V3 = new AviCodec(new FourCC("MP43"), "Microsoft Mpeg-4 v3");

        /// <summary>Microsoft MPEG-4 V2.</summary>
        public static readonly AviCodec MicrosoftMpeg4V2 = new AviCodec(new FourCC("MP42"), "Microsoft Mpeg-4 v2");

        /// <summary>Xvid MPEG-4.</summary>
        public static readonly AviCodec Xvid = new AviCodec(new FourCC("XVID"), "Xvid Mpeg-4");

        /// <summary>DivX MPEG-4.</summary>
        public static readonly AviCodec DivX = new AviCodec(new FourCC("DIVX"), "DivX Mpeg-4");

        /// <summary>x264 H.264/MPEG-4 AVC.</summary>
        public static readonly AviCodec X264 = new AviCodec(new FourCC("X264"), "x264 H.264/Mpeg-4 AVC");
    }
}
