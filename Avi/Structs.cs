namespace Screna.Avi
{
    /// <summary>
    /// Entry of AVI v1 index.
    /// </summary>
    sealed class Index1Entry
    {
        public bool IsKeyFrame;
        public uint DataOffset, DataSize;
    }

    sealed class StandardIndexEntry
    {
        public long DataOffset;
        public uint DataSize;
    }

    sealed class SuperIndexEntry
    {
        public long ChunkOffset;
        public int ChunkSize, Duration;
    }
}
