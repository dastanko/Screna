using System;
using System.IO;

namespace Screna.Avi
{
    static class RiffWriterExtensions
    {
        public static RiffItem OpenChunk(this BinaryWriter writer, FourCC fourcc, int expectedDataSize = -1)
        {
            writer.Write((uint)fourcc);
            writer.Write((uint)(expectedDataSize >= 0 ? expectedDataSize : 0));

            return new RiffItem(writer.BaseStream.Position, expectedDataSize);
        }

        public static RiffItem OpenList(this BinaryWriter writer, FourCC fourcc)
        {
            FourCC ListType_List = new FourCC("LIST");

            return writer.OpenList(fourcc, ListType_List);
        }

        public static RiffItem OpenList(this BinaryWriter writer, FourCC fourcc, FourCC listType)
        {
            var result = writer.OpenChunk(listType);
            writer.Write((uint)fourcc);
            return result;
        }

        public static void CloseItem(this BinaryWriter writer, RiffItem item)
        {
            try
            {
                var position = writer.BaseStream.Position;

                var dataSize = position - item.DataStart;
                if (dataSize > int.MaxValue - RiffItem.ITEM_HEADER_SIZE)
                    throw new InvalidOperationException("Item size is too big.");


                if (item.DataSize < 0)
                {
                    item.DataSize = (int)dataSize;
                    writer.BaseStream.Position = item.DataSizeStart;
                    writer.Write((uint)dataSize);
                    writer.BaseStream.Position = position;
                }
                else if (dataSize != item.DataSize)
                    throw new InvalidOperationException("Item size is not equal to what was previously specified.");

                // Pad to the WORD boundary according to the RIFF spec
                if (position % 2 > 0)
                    writer.SkipBytes(1);
            }
            catch { }
        }

        static readonly byte[] cleanBuffer = new byte[1024];

        public static void SkipBytes(this BinaryWriter writer, int count)
        {
            while (count > 0)
            {
                var toWrite = Math.Min(count, cleanBuffer.Length);
                writer.Write(cleanBuffer, 0, toWrite);
                count -= toWrite;
            }
        }
    }
}
