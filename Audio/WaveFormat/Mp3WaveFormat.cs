using System.IO;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    public enum Mp3Padding
    {
        /// <summary>
        /// Insert padding as needed to achieve the stated average bitrate.
        /// </summary>
        ISO = 0x00000000,

        /// <summary>
        /// Always insert padding. The average bit rate may be higher than stated.
        /// </summary>
        On = 0x00000001,

        /// <summary>
        /// Never insert padding. The average bit rate may be lower than stated.
        /// </summary>
        Off = 0x00000002
    }

    [StructLayout(LayoutKind.Sequential)]
    public class Mp3WaveFormat : WaveFormat
    {
        ushort ID = 1; // MPEGLAYER3_ID_MPEG
        Mp3Padding Padding;
        ushort BlockSize,
            FramePerBlock,
            EncoderDelay;

        public Mp3WaveFormat(int SampleRate, int Channels, int BlockSize, int FramesPerBlock = 1, Mp3Padding Padding = Mp3Padding.Off, int EncoderDelay = 0)
            : base(SampleRate, Channels)
        {
            waveFormatTag = WaveFormatEncoding.Mp3;
            extraSize = 4 * sizeof(ushort) + sizeof(uint);

            this.BlockSize = (ushort)BlockSize;
            this.EncoderDelay = (ushort)EncoderDelay;
            this.Padding = Padding;
        }

        public override void Serialize(BinaryWriter Writer)
        {
            base.Serialize(Writer);

            Writer.Write(ID); // MPEGLAYER3_ID_MPEG
            Writer.Write((int)Padding); // MPEGLAYER3_FLAG_PADDING_OFF
            Writer.Write(BlockSize); // nBlockSize
            Writer.Write(FramePerBlock); // nFramesPerBlock
            Writer.Write(EncoderDelay); // Encoder Delay;
        }
    }
}
