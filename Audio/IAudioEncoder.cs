using System;

namespace Screna.Audio
{
    public interface IAudioEncoder : IDisposable
    {
        /// <summary>
        /// Wave Format including Specific Data
        /// </summary>
        WaveFormat WaveFormat { get; }

        /// <summary>
        /// Gets the maximum number of bytes in encoded data for a given number of source bytes.
        /// </summary>
        /// <param name="sourceCount">Number of source bytes. Specify <c>0</c> for a flush buffer size.</param>
        /// <seealso cref="EncodeBlock"/>
        /// <seealso cref="Flush"/>
        int GetMaxEncodedLength(int SourceCount);

        /// <summary>
        /// Encodes block of audio data.
        /// </summary>
        /// <param name="source">Buffer with audio data.</param>
        /// <param name="sourceOffset">Offset to start reading <paramref name="source"/>.</param>
        /// <param name="sourceCount">Number of bytes to read from <paramref name="source"/>.</param>
        /// <param name="destination">Buffer for encoded audio data.</param>
        /// <param name="destinationOffset">Offset to start writing to <paramref name="destination"/>.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <seealso cref="GetMaxEncodedLength"/>
        int Encode(byte[] Source, int SourceOffset, int SourceCount, byte[] Destination, int DestinationOffset);

        /// <summary>
        /// Flushes internal encoder buffers if any.
        /// </summary>
        /// <param name="destination">Buffer for encoded audio data.</param>
        /// <param name="destinationOffset">Offset to start writing to <paramref name="destination"/>.</param>
        /// <returns>The number of bytes written to <paramref name="destination"/>.</returns>
        /// <seealso cref="GetMaxEncodedLength"/>
        int Flush(byte[] Destination, int DestinationOffset);

        void EnsureBufferIsSufficient(ref byte[] Buffer, int sourceCount);
    }
}
