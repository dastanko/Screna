﻿using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace Screna
{
    /// <summary>
    /// Uses default .net GIF encoding and adds animation headers.
    /// </summary>
    public class GifWriter : IVideoFileWriter
    {
        #region Header Constants
        const byte FileTrailer = 0x3b,
            ApplicationBlockSize = 0x0b,
            GraphicControlExtensionBlockSize = 0x04;

        const int ApplicationExtensionBlockIdentifier = 0xff21,
            GraphicControlExtensionBlockIdentifier = 0xf921;

        const long SourceGlobalColorInfoPosition = 10,
            SourceGraphicControlExtensionPosition = 781,
            SourceGraphicControlExtensionLength = 8,
            SourceImageBlockPosition = 789,
            SourceImageBlockHeaderLength = 11,
            SourceColorBlockPosition = 13,
            SourceColorBlockLength = 768;

        const string ApplicationIdentification = "NETSCAPE2.0",
            FileType = "GIF",
            FileVersion = "89a";
        #endregion

        BinaryWriter Writer;
        bool FirstFrame = true;
        readonly object SyncLock = new object();

        public GifWriter(Stream OutStream, int DefaultFrameDelay = 500, int Repeat = -1)
        {
            Writer = new BinaryWriter(OutStream);
            this.DefaultFrameDelay = DefaultFrameDelay;
            this.Repeat = Repeat;
        }

        public GifWriter(string FileName, int DefaultFrameDelay = 500, int Repeat = -1)
            : this(new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), DefaultFrameDelay, Repeat) { }

        #region Properties
        public int DefaultWidth { get; set; }

        public int DefaultHeight { get; set; }

        /// <summary>
        /// Default Delay in Milliseconds
        /// </summary>
        public int DefaultFrameDelay { get; set; }

        /// <summary>
        /// The Number of Times the Animation must repeat.
        /// -1 indicates no repeat. 0 indicates repeat indefinitely
        /// </summary>
        public int Repeat { get; private set; }
        #endregion

        public int FrameRate { get { return 1000 / DefaultFrameDelay; } }

        public void WriteAudio(byte[] Buffer, int Count) { }

        /// <summary>
        /// Adds a frame to this animation.
        /// </summary>
        /// <param name="Image">The image to add</param>
        /// <param name="XOffset">The positioning x offset this image should be displayed at.</param>
        /// <param name="YOffset">The positioning y offset this image should be displayed at.</param>
        public void WriteFrame(Image Image, int Delay = 0)
        {
            lock (SyncLock)
                using (var gifStream = new MemoryStream())
                {
                    Image.Save(gifStream, ImageFormat.Gif);

                    // Steal the global color table info
                    if (FirstFrame) InitHeader(gifStream, Writer, Image.Width, Image.Height);

                    WriteGraphicControlBlock(gifStream, Writer, Delay == 0 ? DefaultFrameDelay : Delay);
                    WriteImageBlock(gifStream, Writer, !FirstFrame, 0, 0, Image.Width, Image.Height);
                }

            if (FirstFrame) FirstFrame = false;
        }

        public Task WriteFrameAsync(Bitmap Image, int Delay = 0) { return Task.Factory.StartNew(() => WriteFrame(Image, Delay)); }

        public Task WriteFrameAsync(Bitmap Image) { return Task.Factory.StartNew(() => WriteFrame(Image)); }

        public bool RecordsAudio { get { return false; } }

        #region Write
        void InitHeader(Stream sourceGif, BinaryWriter Writer, int w, int h)
        {
            // File Header
            Writer.Write(FileType.ToCharArray());
            Writer.Write(FileVersion.ToCharArray());

            Writer.Write((short)(DefaultWidth == 0 ? w : DefaultWidth)); // Initial Logical Width
            Writer.Write((short)(DefaultHeight == 0 ? h : DefaultHeight)); // Initial Logical Height

            sourceGif.Position = SourceGlobalColorInfoPosition;
            Writer.Write((byte)sourceGif.ReadByte()); // Global Color Table Info
            Writer.Write((byte)0); // Background Color Index
            Writer.Write((byte)0); // Pixel aspect ratio
            WriteColorTable(sourceGif, Writer);

            // App Extension Header for Repeating
            if (Repeat != -1)
            {
                unchecked { Writer.Write((short)ApplicationExtensionBlockIdentifier); };
                Writer.Write((byte)ApplicationBlockSize);
                Writer.Write(ApplicationIdentification.ToCharArray());
                Writer.Write((byte)3); // Application block length
                Writer.Write((byte)1);
                Writer.Write((short)Repeat); // Repeat count for images.
                Writer.Write((byte)0); // terminator
            }
        }

        void WriteColorTable(Stream sourceGif, BinaryWriter Writer)
        {
            sourceGif.Position = SourceColorBlockPosition; // Locating the image color table
            var colorTable = new byte[SourceColorBlockLength];
            sourceGif.Read(colorTable, 0, colorTable.Length);
            Writer.Write(colorTable, 0, colorTable.Length);
        }

        void WriteGraphicControlBlock(Stream sourceGif, BinaryWriter Writer, int frameDelay)
        {
            sourceGif.Position = SourceGraphicControlExtensionPosition; // Locating the source GCE
            var blockhead = new byte[SourceGraphicControlExtensionLength];
            sourceGif.Read(blockhead, 0, blockhead.Length); // Reading source GCE

            unchecked { Writer.Write((short)GraphicControlExtensionBlockIdentifier); }; // Identifier
            Writer.Write((byte)GraphicControlExtensionBlockSize); // Block Size
            Writer.Write((byte)(blockhead[3] & 0xf7 | 0x08)); // Setting disposal flag
            Writer.Write((short)(frameDelay / 10)); // Setting frame delay
            Writer.Write((byte)blockhead[6]); // Transparent color index
            Writer.Write((byte)0); // Terminator
        }

        void WriteImageBlock(Stream sourceGif, BinaryWriter Writer, bool includeColorTable, int x, int y, int w, int h)
        {
            sourceGif.Position = SourceImageBlockPosition; // Locating the image block
            var header = new byte[SourceImageBlockHeaderLength];
            sourceGif.Read(header, 0, header.Length);
            Writer.Write((byte)header[0]); // Separator
            Writer.Write((short)x); // Position X
            Writer.Write((short)y); // Position Y
            Writer.Write((short)w); // Width
            Writer.Write((short)h); // Height

            if (includeColorTable) // If first frame, use global color table - else use local
            {
                sourceGif.Position = SourceGlobalColorInfoPosition;
                Writer.Write((byte)(sourceGif.ReadByte() & 0x3f | 0x80)); // Enabling local color table
                WriteColorTable(sourceGif, Writer);
            }
            else Writer.Write((byte)(header[9] & 0x07 | 0x07)); // Disabling local color table

            Writer.Write((byte)header[10]); // LZW Min Code Size

            // Read/Write image data
            sourceGif.Position = SourceImageBlockPosition + SourceImageBlockHeaderLength;

            var dataLength = sourceGif.ReadByte();
            while (dataLength > 0)
            {
                var imgData = new byte[dataLength];
                sourceGif.Read(imgData, 0, dataLength);

                Writer.Write((byte)dataLength);
                Writer.Write(imgData, 0, dataLength);
                dataLength = sourceGif.ReadByte();
            }

            Writer.Write((byte)0); // Terminator
        }
        #endregion

        public void Dispose()
        {
            // Complete File
            Writer.Write(FileTrailer);

            Writer.BaseStream.Dispose();
            Writer.Dispose();
        }
    }
}