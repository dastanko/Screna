using System;
using System.Collections.Generic;

namespace Screna.Avi
{
    class StreamInfo
    {
        readonly FourCC standardIndexChunkId;
        readonly List<StandardIndexEntry> standardIndex = new List<StandardIndexEntry>();
        readonly List<SuperIndexEntry> superIndex = new List<SuperIndexEntry>();
        readonly List<Index1Entry> index1 = new List<Index1Entry>();

        public StreamInfo(FourCC standardIndexChunkId)
        {
            this.standardIndexChunkId = standardIndexChunkId;
            FrameCount = 0;
            MaxChunkDataSize = 0;
            TotalDataSize = 0;
        }

        public int FrameCount { get; private set; }

        public int MaxChunkDataSize { get; private set; }

        public long TotalDataSize { get; private set; }

        public IList<SuperIndexEntry> SuperIndex => superIndex;

        public IList<StandardIndexEntry> StandardIndex => standardIndex;

        public IList<Index1Entry> Index1 => index1;

        public FourCC StandardIndexChunkId => standardIndexChunkId;

        public void OnFrameWritten(int chunkDataSize)
        {
            FrameCount++;
            MaxChunkDataSize = Math.Max(MaxChunkDataSize, chunkDataSize);
            TotalDataSize += chunkDataSize;
        }
    }
}
