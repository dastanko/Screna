using Screna.Audio;
using System;
using System.Threading.Tasks;

namespace Screna.Avi
{
    /// <summary>
    /// Audio stream of AVI file.
    /// </summary>
    interface IAviAudioStream : IAviStream
    {
        /// <summary>
        /// WaveFomrat including Format Specific Data
        /// </summary>
        WaveFormat WaveFormat { get; set; }

        /// <summary>
        /// Writes a block of audio data.
        /// </summary>
        /// <param name="data">Data buffer.</param>
        /// <param name="startIndex">Start index of data.</param>
        /// <param name="length">Length of data.</param>
        /// <remarks>
        /// Division of audio data into blocks may be arbitrary.
        /// However, it is reasonable to write blocks of approximately the same duration
        /// as a single video frame.
        /// </remarks>
        void WriteBlock(byte[] data, int startIndex, int length);

        /// <summary>
        /// Asynchronously writes a block of audio data.
        /// </summary>
        /// <param name="data">Data buffer.</param>
        /// <param name="startIndex">Start index of data.</param>
        /// <param name="length">Length of data.</param>
        /// <returns>
        /// A task representing the asynchronous write operation.
        /// </returns>
        /// <remarks>
        /// Division of audio data into blocks may be arbitrary.
        /// However, it is reasonable to write blocks of approximately the same duration
        /// as a single video frame.
        /// The contents of <paramref name="data"/> should not be modified until this write operation ends.
        /// </remarks>
        Task WriteBlockAsync(byte[] data, int startIndex, int length);

        /// <summary>
        /// Number of blocks written.
        /// </summary>
        int BlocksWritten { get; }
    }
}
