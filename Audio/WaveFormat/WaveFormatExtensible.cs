using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 2)]
    public class WaveFormatExtensible : WaveFormat
    {
        short wValidBitsPerSample; // bits of precision, or is wSamplesPerBlock if wBitsPerSample==0
        int dwChannelMask; // which channels are present in stream
        Guid subFormat;

        static readonly Guid MEDIASUBTYPE_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71"),
            MEDIASUBTYPE_IEEE_FLOAT = new Guid("00000003-0000-0010-8000-00aa00389b71");

        /// <summary>
        /// Parameterless constructor for marshalling
        /// </summary>
        WaveFormatExtensible() { }

        /// <summary>
        /// Creates a new WaveFormatExtensible for PCM or IEEE
        /// </summary>
        public WaveFormatExtensible(int rate, int bits, int channels)
            : base(rate, bits, channels)
        {
            waveFormatTag = WaveFormatEncoding.Extensible;
            extraSize = 22;
            wValidBitsPerSample = (short)bits;

            for (var i = 0; i < channels; i++)
                dwChannelMask |= (1 << i);

            subFormat = bits == 32 ? MEDIASUBTYPE_IEEE_FLOAT : MEDIASUBTYPE_PCM;
        }

        /// <summary>
        /// Serialize
        /// </summary>
        public override void Serialize(System.IO.BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(wValidBitsPerSample);
            writer.Write(dwChannelMask);
            var guid = subFormat.ToByteArray();
            writer.Write(guid, 0, guid.Length);
        }
    }
}
