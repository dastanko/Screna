namespace Screna.Avi
{
    /// <summary>
    /// Item of a RIFF file - either list or chunk.
    /// </summary>
    struct RiffItem
    {
        public const int ITEM_HEADER_SIZE = 2 * sizeof(uint);

        public RiffItem(long dataStart, int dataSize = -1)
        {
            this.DataStart = dataStart;
            DataSize = dataSize;
        }

        public long DataStart { get; }

        public long ItemStart => DataStart - ITEM_HEADER_SIZE;

        public long DataSizeStart => DataStart - sizeof(uint);

        public int DataSize { get; set; }

        public int ItemSize => DataSize < 0 ? -1 : DataSize + ITEM_HEADER_SIZE;
    }
}
