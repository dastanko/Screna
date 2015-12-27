using Screna.Audio;
using System;
using System.Threading.Tasks;

namespace Screna.Avi
{
    /// <summary>
    /// Base class for wrappers around <see cref="IAviAudioStreamInternal"/>.
    /// </summary>
    /// <remarks>
    /// Simply delegates all operations to wrapped stream.
    /// </remarks>
    abstract class AudioStreamWrapperBase : IAviAudioStreamInternal, IDisposable
    {
        protected AudioStreamWrapperBase(IAviAudioStreamInternal baseStream)
        {
            this.baseStream = baseStream;
        }

        protected IAviAudioStreamInternal BaseStream { get { return baseStream; } }
        readonly IAviAudioStreamInternal baseStream;

        public virtual void Dispose()
        {
            var baseStreamDisposable = baseStream as IDisposable;
            if (baseStreamDisposable != null)
                baseStreamDisposable.Dispose();
        }

        public virtual WaveFormat WaveFormat
        {
            get { return baseStream.WaveFormat; }
            set { baseStream.WaveFormat = value; }
        }

        public virtual void WriteBlock(byte[] data, int startIndex, int length)
        {
            baseStream.WriteBlock(data, startIndex, length);
        }

        public virtual Task WriteBlockAsync(byte[] data, int startIndex, int length)
        {
            return baseStream.WriteBlockAsync(data, startIndex, length);
        }

        public int BlocksWritten { get { return baseStream.BlocksWritten; } }

        public int Index { get { return baseStream.Index; } }

        public virtual string Name
        {
            get { return baseStream.Name; }
            set { baseStream.Name = value; }
        }

        public FourCC StreamType { get { return baseStream.StreamType; } }

        public FourCC ChunkId { get { return baseStream.ChunkId; } }

        public virtual void PrepareForWriting() { baseStream.PrepareForWriting(); }

        public virtual void FinishWriting() { baseStream.FinishWriting(); }

        public void WriteHeader() { baseStream.WriteHeader(); }

        public void WriteFormat() { baseStream.WriteFormat(); }
    }
}
