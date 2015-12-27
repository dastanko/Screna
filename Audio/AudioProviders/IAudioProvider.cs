using System;

namespace Screna.Audio
{
    /// <summary>
    /// Generic interface for wave recording
    /// </summary>
    public interface IAudioProvider : IDisposable
    {
        /// <summary>
        /// Recording WaveFormat
        /// </summary>
        WaveFormat WaveFormat { get; set; }

        /// <summary>
        /// Start Recording
        /// </summary>
        void Start();

        /// <summary>
        /// Stop Recording
        /// </summary>
        void Stop();

        bool IsSynchronizable { get; }

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        event Action<byte[], int> DataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        event Action<Exception> RecordingStopped;
    }
}
