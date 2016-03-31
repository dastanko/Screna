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
            this.BaseStream = baseStream;
        }

        protected IAviAudioStreamInternal BaseStream { get; }

        public virtual void Dispose()
        {
            var baseStreamDisposable = BaseStream as IDisposable;
            baseStreamDisposable?.Dispose();
        }

        public virtual WaveFormat WaveFormat
        {
            get { return BaseStream.WaveFormat; }
            set { BaseStream.WaveFormat = value; }
        }

        public virtual void WriteBlock(byte[] data, int startIndex, int length)
        {
            BaseStream.WriteBlock(data, startIndex, length);
        }

        public virtual Task WriteBlockAsync(byte[] data, int startIndex, int length)
        {
            return BaseStream.WriteBlockAsync(data, startIndex, length);
        }

        public int BlocksWritten => BaseStream.BlocksWritten;

        public int Index => BaseStream.Index;

        public virtual string Name
        {
            get { return BaseStream.Name; }
            set { BaseStream.Name = value; }
        }

        public FourCC StreamType => BaseStream.StreamType;

        public FourCC ChunkId => BaseStream.ChunkId;

        public virtual void PrepareForWriting() => BaseStream.PrepareForWriting();

        public virtual void FinishWriting() => BaseStream.FinishWriting();

        public void WriteHeader() => BaseStream.WriteHeader();

        public void WriteFormat() => BaseStream.WriteFormat();
    }
}
