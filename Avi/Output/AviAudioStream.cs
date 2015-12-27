using Screna.Audio;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Screna.Avi
{
    class AviAudioStream : AviStreamBase, IAviAudioStreamInternal
    {
        readonly IAviStreamWriteHandler writeHandler;
        WaveFormat wf;
        int blocksWritten;

        public AviAudioStream(int index, IAviStreamWriteHandler writeHandler, WaveFormat format)
            : base(index)
        {
            this.writeHandler = writeHandler;

            wf = format;
        }

        public WaveFormat WaveFormat
        {
            get { return wf; }
            set
            {
                CheckNotFrozen();
                wf = value;
            }
        }

        public void WriteBlock(byte[] buffer, int startIndex, int count)
        {
            writeHandler.WriteAudioBlock(this, buffer, startIndex, count);
            Interlocked.Increment(ref blocksWritten);
        }

        public Task WriteBlockAsync(byte[] data, int startIndex, int length) { throw new NotSupportedException("Asynchronous writes are not supported."); }

        public int BlocksWritten { get { return blocksWritten; } }

        static readonly FourCC Audio = new FourCC("auds");

        public override FourCC StreamType { get { return Audio; } }

        protected override FourCC GenerateChunkId() { return RIFFChunksFourCCs.AudioData(Index); }

        public override void WriteHeader() { writeHandler.WriteStreamHeader(this); }

        public override void WriteFormat() { writeHandler.WriteStreamFormat(this); }
    }
}
