namespace Screna.Avi
{
    /// <summary>
    /// Item of a RIFF file - either list or chunk.
    /// </summary>
    struct RiffItem
    {
        public const int ITEM_HEADER_SIZE = 2 * sizeof(uint);

        readonly long dataStart;
        int dataSize;

        public RiffItem(long dataStart, int dataSize = -1)
        {
            this.dataStart = dataStart;
            this.dataSize = dataSize;
        }

        public long DataStart { get { return dataStart; } }

        public long ItemStart { get { return dataStart - ITEM_HEADER_SIZE; } }

        public long DataSizeStart { get { return dataStart - sizeof(uint); } }

        public int DataSize
        {
            get { return dataSize; }
            set { dataSize = value; }
        }

        public int ItemSize { get { return dataSize < 0 ? -1 : dataSize + ITEM_HEADER_SIZE; } }
    }
}
