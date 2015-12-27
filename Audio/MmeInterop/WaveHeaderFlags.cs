using System;

namespace Screna.Audio
{
    [Flags]
    enum WaveHeaderFlags
    {
        /// <summary>
        /// Set by the device driver to indicate that it is finished with the buffer and is returning it to the application.
        /// </summary>
        Done = 0x00000001,
        /// <summary>
        /// Set by Windows to indicate that the buffer is queued for playback.
        /// </summary>
        InQueue = 0x00000010,
    }
}
