using System;

namespace Screna.Audio
{
    [Flags]
    public enum SupportedWaveFormat
    {
        /// <summary>
        /// 11.025 kHz, Mono,   8-bit
        /// </summary>
        WF1M08 = 0x00000001,
        /// <summary>
        /// 11.025 kHz, Stereo, 8-bit
        /// </summary>
        WF1S08 = 0x00000002,
        /// <summary>
        /// 11.025 kHz, Mono,   16-bit
        /// </summary>
        WF1M16 = 0x00000004,
        /// <summary>
        /// 11.025 kHz, Stereo, 16-bit
        /// </summary>
        WF1S16 = 0x00000008,
        /// <summary>
        /// 22.05  kHz, Mono,   8-bit
        /// </summary>
        WF2M08 = 0x00000010,
        /// <summary>
        /// 22.05  kHz, Stereo, 8-bit 
        /// </summary>
        WF2S08 = 0x00000020,
        /// <summary>
        /// 22.05  kHz, Mono,   16-bit
        /// </summary>
        WF2M16 = 0x00000040,
        /// <summary>
        /// 22.05  kHz, Stereo, 16-bit
        /// </summary>
        WF2S16 = 0x00000080,
        /// <summary>
        /// 44.1   kHz, Mono,   8-bit 
        /// </summary>
        WF4M08 = 0x00000100,
        /// <summary>
        /// 44.1   kHz, Stereo, 8-bit 
        /// </summary>
        WF4S08 = 0x00000200,
        /// <summary>
        /// 44.1   kHz, Mono,   16-bit
        /// </summary>
        WF4M16 = 0x00000400,
        /// <summary>
        ///  44.1   kHz, Stereo, 16-bit
        /// </summary>
        WF4S16 = 0x00000800,

        /// <summary>
        /// 44.1   kHz, Mono,   8-bit 
        /// </summary>
        WF44M08 = 0x00000100,
        /// <summary>
        /// 44.1   kHz, Stereo, 8-bit 
        /// </summary>
        WF44S08 = 0x00000200,
        /// <summary>
        /// 44.1   kHz, Mono,   16-bit
        /// </summary>
        WF44M16 = 0x00000400,
        /// <summary>
        /// 44.1   kHz, Stereo, 16-bit
        /// </summary>
        WF44S16 = 0x00000800,
        /// <summary>
        /// 48     kHz, Mono,   8-bit 
        /// </summary>
        WF48M08 = 0x00001000,
        /// <summary>
        ///  48     kHz, Stereo, 8-bit
        /// </summary>
        WF48S08 = 0x00002000,
        /// <summary>
        /// 48     kHz, Mono,   16-bit
        /// </summary>
        WF48M16 = 0x00004000,
        /// <summary>
        /// 48     kHz, Stereo, 16-bit
        /// </summary>
        WF48S16 = 0x00008000,
        /// <summary>
        /// 96     kHz, Mono,   8-bit 
        /// </summary>
        WF96M08 = 0x00010000,
        /// <summary>
        /// 96     kHz, Stereo, 8-bit
        /// </summary>
        WF96S08 = 0x00020000,
        /// <summary>
        /// 96     kHz, Mono,   16-bit
        /// </summary>
        WF96M16 = 0x00040000,
        /// <summary>
        /// 96     kHz, Stereo, 16-bit
        /// </summary>
        WF96S16 = 0x00080000
    }
}
