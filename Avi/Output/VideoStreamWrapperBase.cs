using System;

namespace Screna.Avi
{
    /// <summary>
    /// Base class for wrappers around <see cref="IAviVideoStreamInternal"/>.
    /// </summary>
    /// <remarks>
    /// Simply delegates all operations to wrapped stream.
    /// </remarks>
    abstract class VideoStreamWrapperBase : IAviVideoStreamInternal, IDisposable
    {
        protected VideoStreamWrapperBase(IAviVideoStreamInternal baseStream)
        {
            this.BaseStream = baseStream;
        }

        protected IAviVideoStreamInternal BaseStream { get; }

        public virtual void Dispose()
        {
            var baseStreamDisposable = BaseStream as IDisposable;
            baseStreamDisposable?.Dispose();
        }

        public virtual int Width
        {
            get { return BaseStream.Width; }
            set { BaseStream.Width = value; }
        }

        public virtual int Height
        {
            get { return BaseStream.Height; }
            set { BaseStream.Height = value; }
        }

        public virtual BitsPerPixel BitsPerPixel
        {
            get { return BaseStream.BitsPerPixel; }
            set { BaseStream.BitsPerPixel = value; }
        }

        public virtual FourCC Codec
        {
            get { return BaseStream.Codec; }
            set { BaseStream.Codec = value; }
        }

        public virtual void WriteFrame(bool isKeyFrame, byte[] frameData, int startIndex, int length)
        {
            BaseStream.WriteFrame(isKeyFrame, frameData, startIndex, length);
        }

        public virtual System.Threading.Tasks.Task WriteFrameAsync(bool isKeyFrame, byte[] frameData, int startIndex, int length)
        {
            return BaseStream.WriteFrameAsync(isKeyFrame, frameData, startIndex, length);
        }

        public int FramesWritten => BaseStream.FramesWritten;

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
