using System;
using System.Threading.Tasks;

namespace Screna.Avi
{
    class AviVideoStream : AviStreamBase, IAviVideoStreamInternal
    {
        readonly IAviStreamWriteHandler writeHandler;
        FourCC streamCodec;
        int width, height;
        BitsPerPixel bitsPerPixel;
        int framesWritten;

        public AviVideoStream(int index, IAviStreamWriteHandler writeHandler,
            int width, int height, BitsPerPixel bitsPerPixel)
            : base(index)
        {
            this.writeHandler = writeHandler;
            this.width = width;
            this.height = height;
            this.bitsPerPixel = bitsPerPixel;
            this.streamCodec = AviCodec.Uncompressed.FourCC;
        }


        public int Width
        {
            get { return width; }
            set
            {
                CheckNotFrozen();
                width = value;
            }
        }

        public int Height
        {
            get { return height; }
            set
            {
                CheckNotFrozen();
                height = value;
            }
        }

        public BitsPerPixel BitsPerPixel
        {
            get { return bitsPerPixel; }
            set
            {
                CheckNotFrozen();
                bitsPerPixel = value;
            }
        }

        public FourCC Codec
        {
            get { return streamCodec; }
            set
            {
                CheckNotFrozen();
                streamCodec = value;
            }
        }

        public void WriteFrame(bool isKeyFrame, byte[] frameData, int startIndex, int count)
        {
            writeHandler.WriteVideoFrame(this, isKeyFrame, frameData, startIndex, count);
            System.Threading.Interlocked.Increment(ref framesWritten);
        }

        public Task WriteFrameAsync(bool isKeyFrame, byte[] frameData, int startIndex, int count)
        {
            throw new NotSupportedException("Asynchronous writes are not supported.");
        }

        public int FramesWritten { get { return framesWritten; } }

        static readonly FourCC Video = new FourCC("vids");

        public override FourCC StreamType { get { return Video; } }

        protected override FourCC GenerateChunkId() { return RIFFChunksFourCCs.VideoFrame(Index, Codec != AviCodec.Uncompressed.FourCC); }

        public override void WriteHeader() { writeHandler.WriteStreamHeader(this); }

        public override void WriteFormat() { writeHandler.WriteStreamFormat(this); }
    }
}
