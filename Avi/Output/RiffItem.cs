namespace Screna.Avi
{
    /// <summary>
    /// Item of a RIFF file - either list or chunk.
    /// </summary>
    struct RiffItem
    {
        public const int ITEM_HEADER_SIZE = 2 * sizeof(uint);

        readonly long dataStart;

        public RiffItem(long dataStart, int dataSize = -1)
        {
            this.dataStart = dataStart;
            this.DataSize = dataSize;
        }

        public long DataStart => dataStart;

        public long ItemStart => dataStart - ITEM_HEADER_SIZE;

        public long DataSizeStart => dataStart - sizeof(uint);

        public int DataSize { get; set; }

        public int ItemSize => DataSize < 0 ? -1 : DataSize + ITEM_HEADER_SIZE;
    }
}
