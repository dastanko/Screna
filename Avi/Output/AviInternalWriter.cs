using Screna.Audio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Screna.Avi
{
    /// <summary>
    /// Used to write an AVI file.
    /// </summary>
    /// <remarks>
    /// After writing begin to any of the streams, no property changes or stream addition are allowed.
    /// </remarks>
    class AviInternalWriter : IDisposable, IAviStreamWriteHandler
    {
        const int MAX_SUPER_INDEX_ENTRIES = 256;
        const int MAX_INDEX_ENTRIES = 15000;
        const int INDEX1_ENTRY_SIZE = 4 * sizeof(uint);
        const int RIFF_AVI_SIZE_TRESHOLD = 512 * 1024 * 1024;
        const int RIFF_AVIX_SIZE_TRESHOLD = int.MaxValue - 1024 * 1024;

        static readonly FourCC ListType_Riff = new FourCC("RIFF");
        static class RIFFListFourCCs
        {
            /// <summary>Top-level AVI list.</summary>
            public static readonly FourCC Avi = new FourCC("AVI");

            /// <summary>Top-level extended AVI list.</summary>
            public static readonly FourCC AviExtended = new FourCC("AVIX");

            /// <summary>Header list.</summary>
            public static readonly FourCC Header = new FourCC("hdrl");

            /// <summary>List containing stream information.</summary>
            public static readonly FourCC Stream = new FourCC("strl");

            /// <summary>List containing OpenDML headers.</summary>
            public static readonly FourCC OpenDml = new FourCC("odml");

            /// <summary>List with content chunks.</summary>
            public static readonly FourCC Movie = new FourCC("movi");
        }

        readonly BinaryWriter fileWriter;
        bool isClosed = false;
        bool startedWriting = false;
        readonly object syncWrite = new object();

        bool isFirstRiff = true;
        RiffItem currentRiff, currentMovie, header;
        int riffSizeTreshold, riffAviFrameCount = -1, index1Count = 0;
        readonly List<IAviStreamInternal> streams = new List<IAviStreamInternal>();
        StreamInfo[] streamsInfo;

        /// <summary>
        /// Creates a new instance of <see cref="AviInternalWriter"/>.
        /// </summary>
        /// <param name="fileName">Path to an AVI file being written.</param>
        public AviInternalWriter(string fileName)
        {
            var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024);
            fileWriter = new BinaryWriter(fileStream);
        }

        /// <summary>Frame rate.</summary>
        /// <remarks>
        /// The value of the property is rounded to 3 fractional digits.
        /// Default value is <c>1</c>.
        /// </remarks>
        public decimal FramesPerSecond
        {
            get { return framesPerSecond; }
            set
            {
                lock (syncWrite)
                {
                    CheckNotStartedWriting();
                    framesPerSecond = Decimal.Round(value, 3);
                }
            }
        }
        decimal framesPerSecond = 1;
        uint frameRateNumerator, frameRateDenominator;

        /// <summary>
        /// Whether to emit index used in AVI v1 format.
        /// </summary>
        /// <remarks>
        /// By default, only index conformant to OpenDML AVI extensions (AVI v2) is emitted. 
        /// Presence of v1 index may improve the compatibility of generated AVI files with certain software, 
        /// especially when there are multiple streams.
        /// </remarks>
        public bool EmitIndex1
        {
            get { return emitIndex1; }
            set
            {
                lock (syncWrite)
                {
                    CheckNotStartedWriting();
                    emitIndex1 = value;
                }
            }
        }
        bool emitIndex1;

        /// <summary>AVI streams that have been added so far.</summary>
        ReadOnlyCollection<IAviStreamInternal> Streams { get { return streams.AsReadOnly(); } }

        /// <summary>Adds new video stream.</summary>
        /// <param name="width">Frame's width.</param>
        /// <param name="height">Frame's height.</param>
        /// <param name="bitsPerPixel">Bits per pixel.</param>
        /// <returns>Newly added video stream.</returns>
        /// <remarks>
        /// Stream is initialized to be ready for uncompressed video (bottom-up BGR) with specified parameters.
        /// However, properties (such as <see cref="IAviVideoStream.Codec"/>) can be changed later if the stream is
        /// to be fed with pre-compressed data.
        /// </remarks>
        public IAviVideoStream AddVideoStream(int width = 1, int height = 1, BitsPerPixel bitsPerPixel = BitsPerPixel.Bpp32)
        {
            return AddStream<IAviVideoStreamInternal>(index =>
                {
                    var stream = new AviVideoStream(index, this, width, height, bitsPerPixel);
                    var asyncStream = new AsyncVideoStreamWrapper(stream);
                    return asyncStream;
                });
        }

        /// <summary>Adds new encoding video stream.</summary>
        /// <param name="encoder">Encoder to be used.</param>
        /// <param name="ownsEncoder">Whether encoder should be disposed with the writer.</param>
        /// <param name="width">Frame's width.</param>
        /// <param name="height">Frame's height.</param>
        /// <returns>Newly added video stream.</returns>
        /// <remarks>
        /// <para>
        /// Stream is initialized to be to be encoded with the specified encoder.
        /// Method <see cref="IAviVideoStream.WriteFrame"/> expects data in the same format as encoders,
        /// that is top-down BGR32 bitmap. It is passed to the encoder and the encoded result is written
        /// to the stream.
        /// Parameters <c>isKeyFrame</c> and <c>length</c> are ignored by encoding streams,
        /// as encoders determine on their own which frames are keys, and the size of input bitmaps is fixed.
        /// </para>
        /// <para>
        /// Properties <see cref="IAviVideoStream.Codec"/> and <see cref="IAviVideoStream.BitsPerPixel"/> 
        /// are defined by the encoder, and cannot be modified.
        /// </para>
        /// </remarks>
        public IAviVideoStream AddEncodingVideoStream(IVideoEncoder encoder, bool ownsEncoder = true, int width = 1, int height = 1)
        {
            return AddStream<IAviVideoStreamInternal>(index =>
                {
                    var stream = new AviVideoStream(index, this, width, height, BitsPerPixel.Bpp32);
                    var encodingStream = new EncodingVideoStreamWrapper(stream, encoder, ownsEncoder);
                    var asyncStream = new AsyncVideoStreamWrapper(encodingStream);
                    return asyncStream;
                });
        }

        /// <summary>Adds new audio stream.</summary>
        /// <param name="channelCount">Number of channels.</param>
        /// <param name="samplesPerSecond">Sample rate.</param>
        /// <param name="bitsPerSample">Bits per sample (per single channel).</param>
        /// <returns>Newly added audio stream.</returns>
        /// <remarks>
        /// Stream is initialized to be ready for uncompressed audio (PCM) with specified parameters.
        /// However, properties (such as <see cref="IAviAudioStream.Format"/>) can be changed later if the stream is
        /// to be fed with pre-compressed data.
        /// </remarks>
        public IAviAudioStream AddAudioStream(WaveFormat wf)
        {
            return AddStream<IAviAudioStreamInternal>(index =>
                {
                    var stream = new AviAudioStream(index, this, wf);
                    var asyncStream = new AsyncAudioStreamWrapper(stream);
                    return asyncStream;
                });
        }

        /// <summary>Adds new encoding audio stream.</summary>
        /// <param name="encoder">Encoder to be used.</param>
        /// <param name="ownsEncoder">Whether encoder should be disposed with the writer.</param>
        /// <returns>Newly added audio stream.</returns>
        /// <remarks>
        /// <para>
        /// Stream is initialized to be to be encoded with the specified encoder.
        /// Method <see cref="IAviAudioStream.WriteBlock"/> expects data in the same format as encoder (see encoder's docs). 
        /// The data is passed to the encoder and the encoded result is written to the stream.
        /// </para>
        /// <para>
        /// The encoder defines the following properties of the stream:
        /// <see cref="IAviAudioStream.ChannelCount"/>, <see cref="IAviAudioStream.SamplesPerSecond"/>,
        /// <see cref="IAviAudioStream.BitsPerSample"/>, <see cref="IAviAudioStream.BytesPerSecond"/>,
        /// <see cref="IAviAudioStream.Granularity"/>, <see cref="IAviAudioStream.Format"/>,
        /// <see cref="IAviAudioStream.FormatSpecificData"/>.
        /// These properties cannot be modified.
        /// </para>
        /// </remarks>
        public IAviAudioStream AddEncodingAudioStream(IAudioEncoder encoder, bool ownsEncoder = true)
        {
            return AddStream<IAviAudioStreamInternal>(index =>
                {
                    var stream = new AviAudioStream(index, this, new WaveFormat(44100, 16, 1));
                    var encodingStream = new EncodingAudioStreamWrapper(stream, encoder, ownsEncoder);
                    return new AsyncAudioStreamWrapper(encodingStream);
                });
        }

        TStream AddStream<TStream>(Func<int, TStream> streamFactory)
            where TStream : IAviStreamInternal
        {
            lock (syncWrite)
            {
                CheckNotClosed();
                CheckNotStartedWriting();

                var stream = streamFactory.Invoke(Streams.Count);

                streams.Add(stream);

                return stream;
            }
        }

        /// <summary>
        /// Closes the writer and AVI file itself.
        /// </summary>
        public void Close()
        {
            try
            {
                if (!isClosed)
                {
                    bool finishWriting;
                    lock (syncWrite) finishWriting = startedWriting;

                    // Call FinishWriting without holding the lock
                    // because additional writes may be performed inside
                    if (finishWriting)
                        foreach (var stream in streams)
                            stream.FinishWriting();

                    lock (syncWrite)
                    {
                        if (startedWriting)
                        {
                            foreach (var stream in streams)
                                FlushStreamIndex(stream);

                            CloseCurrentRiff();

                            // Rewrite header with actual data like frames count, super index, etc.
                            fileWriter.BaseStream.Position = header.ItemStart;
                            WriteHeader();
                        }

                        fileWriter.Close();
                        isClosed = true;
                    }

                    foreach (var disposableStream in streams.OfType<IDisposable>())
                        disposableStream.Dispose();
                }
            }
            catch (ObjectDisposedException) { }
        }

        void IDisposable.Dispose() { Close(); }

        void CheckNotStartedWriting()
        {
            if (startedWriting)
                throw new InvalidOperationException("No stream information can be changed after starting to write frames.");
        }

        void CheckNotClosed() { if (isClosed) throw new ObjectDisposedException(typeof(AviInternalWriter).Name); }

        void PrepareForWriting()
        {
            startedWriting = true;
            foreach (var stream in streams) stream.PrepareForWriting();

            Extensions.SplitFrameRate(FramesPerSecond, out frameRateNumerator, out frameRateDenominator);

            streamsInfo = streams.Select(s => new StreamInfo(RIFFChunksFourCCs.IndexData(s.Index))).ToArray();

            riffSizeTreshold = RIFF_AVI_SIZE_TRESHOLD;

            currentRiff = fileWriter.OpenList(RIFFListFourCCs.Avi, ListType_Riff);
            WriteHeader();
            currentMovie = fileWriter.OpenList(RIFFListFourCCs.Movie);
        }

        void CreateNewRiffIfNeeded(int approximateSizeOfNextChunk)
        {
            var estimatedSize = fileWriter.BaseStream.Position + approximateSizeOfNextChunk - currentRiff.ItemStart;
            if (isFirstRiff && emitIndex1) estimatedSize += RiffItem.ITEM_HEADER_SIZE + index1Count * INDEX1_ENTRY_SIZE;
            if (estimatedSize > riffSizeTreshold)
            {
                CloseCurrentRiff();

                currentRiff = fileWriter.OpenList(RIFFListFourCCs.AviExtended, ListType_Riff);
                currentMovie = fileWriter.OpenList(RIFFListFourCCs.Movie);
            }
        }

        void CloseCurrentRiff()
        {
            fileWriter.CloseItem(currentMovie);

            // Several special actions for the first RIFF (AVI)
            if (isFirstRiff)
            {
                riffAviFrameCount = streams.OfType<IAviVideoStream>().Max(s => streamsInfo[s.Index].FrameCount);
                if (emitIndex1) WriteIndex1();
                riffSizeTreshold = RIFF_AVIX_SIZE_TRESHOLD;
            }

            fileWriter.CloseItem(currentRiff);
            isFirstRiff = false;
        }

        #region IAviStreamDataHandler implementation
        void IAviStreamWriteHandler.WriteVideoFrame(AviVideoStream stream, bool isKeyFrame, byte[] frameData, int startIndex, int count)
        {
            WriteStreamFrame(stream, isKeyFrame, frameData, startIndex, count);
        }

        void IAviStreamWriteHandler.WriteAudioBlock(AviAudioStream stream, byte[] blockData, int startIndex, int count)
        {
            WriteStreamFrame(stream, true, blockData, startIndex, count);
        }

        void WriteStreamFrame(AviStreamBase stream, bool isKeyFrame, byte[] frameData, int startIndex, int count)
        {
            lock (syncWrite)
            {
                CheckNotClosed();

                if (!startedWriting)
                    PrepareForWriting();

                var si = streamsInfo[stream.Index];
                if (si.SuperIndex.Count == MAX_SUPER_INDEX_ENTRIES)
                    throw new InvalidOperationException("Cannot write more frames to this stream.");

                if (ShouldFlushStreamIndex(si.StandardIndex))
                    FlushStreamIndex(stream);

                var shouldCreateIndex1Entry = emitIndex1 && isFirstRiff;

                CreateNewRiffIfNeeded(count + (shouldCreateIndex1Entry ? INDEX1_ENTRY_SIZE : 0));

                var chunk = fileWriter.OpenChunk(stream.ChunkId, count);
                fileWriter.Write(frameData, startIndex, count);
                fileWriter.CloseItem(chunk);

                si.OnFrameWritten(chunk.DataSize);
                var dataSize = (uint)chunk.DataSize;
                // Set highest bit for non-key frames according to the OpenDML spec
                if (!isKeyFrame)
                    dataSize |= 0x80000000U;

                var newEntry = new StandardIndexEntry
                {
                    DataOffset = chunk.DataStart,
                    DataSize = dataSize
                };

                si.StandardIndex.Add(newEntry);

                if (shouldCreateIndex1Entry)
                {
                    var index1Entry = new Index1Entry
                    {
                        IsKeyFrame = isKeyFrame,
                        DataOffset = (uint)(chunk.ItemStart - currentMovie.DataStart),
                        DataSize = dataSize
                    };
                    si.Index1.Add(index1Entry);
                    index1Count++;
                }
            }
        }

        void IAviStreamWriteHandler.WriteStreamHeader(AviVideoStream videoStream)
        {
            // See AVISTREAMHEADER structure
            fileWriter.Write((uint)videoStream.StreamType);
            fileWriter.Write((uint)videoStream.Codec);
            fileWriter.Write(0U); // StreamHeaderFlags
            fileWriter.Write((ushort)0); // priority
            fileWriter.Write((ushort)0); // language
            fileWriter.Write(0U); // initial frames
            fileWriter.Write(frameRateDenominator); // scale (frame rate denominator)
            fileWriter.Write(frameRateNumerator); // rate (frame rate numerator)
            fileWriter.Write(0U); // start
            fileWriter.Write((uint)streamsInfo[videoStream.Index].FrameCount); // length
            fileWriter.Write((uint)streamsInfo[videoStream.Index].MaxChunkDataSize); // suggested buffer size
            fileWriter.Write(0U); // quality
            fileWriter.Write(0U); // sample size
            fileWriter.Write((short)0); // rectangle left
            fileWriter.Write((short)0); // rectangle top
            short right = (short)(videoStream != null ? videoStream.Width : 0);
            short bottom = (short)(videoStream != null ? videoStream.Height : 0);
            fileWriter.Write(right); // rectangle right
            fileWriter.Write(bottom); // rectangle bottom
        }

        void IAviStreamWriteHandler.WriteStreamHeader(AviAudioStream audioStream)
        {
            var wf = audioStream.WaveFormat;

            // See AVISTREAMHEADER structure
            fileWriter.Write((uint)audioStream.StreamType);
            fileWriter.Write(0U); // no codec
            fileWriter.Write(0U); // StreamHeaderFlags
            fileWriter.Write((ushort)0); // priority
            fileWriter.Write((ushort)0); // language
            fileWriter.Write(0U); // initial frames
            fileWriter.Write((uint)wf.BlockAlign); // scale (sample rate denominator)
            fileWriter.Write((uint)wf.AverageBytesPerSecond); // rate (sample rate numerator)
            fileWriter.Write(0U); // start
            fileWriter.Write((uint)streamsInfo[audioStream.Index].TotalDataSize); // length
            fileWriter.Write((uint)(wf.AverageBytesPerSecond / 2)); // suggested buffer size (half-second)
            fileWriter.Write(-1); // quality
            fileWriter.Write(wf.BlockAlign); // sample size
            fileWriter.SkipBytes(sizeof(short) * 4);
        }

        void IAviStreamWriteHandler.WriteStreamFormat(AviVideoStream videoStream)
        {
            // See BITMAPINFOHEADER structure
            fileWriter.Write(40U); // size of structure
            fileWriter.Write(videoStream.Width);
            fileWriter.Write(videoStream.Height);
            fileWriter.Write((short)1); // planes
            fileWriter.Write((ushort)videoStream.BitsPerPixel); // bits per pixel
            fileWriter.Write((uint)videoStream.Codec); // compression (codec FOURCC)
            var sizeInBytes = videoStream.Width * videoStream.Height * (((int)videoStream.BitsPerPixel) / 8);
            fileWriter.Write((uint)sizeInBytes); // image size in bytes
            fileWriter.Write(0); // X pixels per meter
            fileWriter.Write(0); // Y pixels per meter

            // Writing grayscale palette for 8-bit uncompressed stream
            // Otherwise, no palette
            if (videoStream.BitsPerPixel == BitsPerPixel.Bpp8 && videoStream.Codec == AviCodec.Uncompressed.FourCC)
            {
                fileWriter.Write(256U); // palette colors used
                fileWriter.Write(0U); // palette colors important
                for (int i = 0; i < 256; i++)
                {
                    fileWriter.Write((byte)i);
                    fileWriter.Write((byte)i);
                    fileWriter.Write((byte)i);
                    fileWriter.Write((byte)0);
                }
            }
            else
            {
                fileWriter.Write(0U); // palette colors used
                fileWriter.Write(0U); // palette colors important
            }
        }

        void IAviStreamWriteHandler.WriteStreamFormat(AviAudioStream audioStream)
        {
            audioStream.WaveFormat.Serialize(fileWriter);
        }
        #endregion

        #region Header
        void WriteHeader()
        {
            header = fileWriter.OpenList(RIFFListFourCCs.Header);
            WriteFileHeader();
            foreach (var stream in streams) WriteStreamList(stream);
            WriteOdmlHeader();
            WriteJunkInsteadOfMissingSuperIndexEntries();
            fileWriter.CloseItem(header);
        }

        void WriteJunkInsteadOfMissingSuperIndexEntries()
        {
            var missingEntriesCount = streamsInfo.Sum(si => MAX_SUPER_INDEX_ENTRIES - si.SuperIndex.Count);
            if (missingEntriesCount > 0)
            {
                var junkDataSize = missingEntriesCount * sizeof(uint) * 4 - RiffItem.ITEM_HEADER_SIZE;
                var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.Junk, junkDataSize);
                fileWriter.SkipBytes(junkDataSize);
                fileWriter.CloseItem(chunk);
            }
        }

        void WriteFileHeader()
        {
            // See AVIMAINHEADER structure
            var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.AviHeader);
            fileWriter.Write((uint)Decimal.Round(1000000m / FramesPerSecond)); // microseconds per frame
            // TODO: More correct computation of byterate
            fileWriter.Write((uint)Decimal.Truncate(FramesPerSecond * streamsInfo.Sum(s => s.MaxChunkDataSize))); // max bytes per second
            fileWriter.Write(0U); // padding granularity
            var flags = MainHeaderFlags.IsInterleaved | MainHeaderFlags.TrustChunkType;
            if (emitIndex1) flags |= MainHeaderFlags.HasIndex;
            fileWriter.Write((uint)flags); // MainHeaderFlags
            fileWriter.Write(riffAviFrameCount); // total frames (in the first RIFF list containing this header)
            fileWriter.Write(0U); // initial frames
            fileWriter.Write((uint)Streams.Count); // stream count
            fileWriter.Write(0U); // suggested buffer size
            var firstVideoStream = streams.OfType<IAviVideoStream>().First();
            fileWriter.Write(firstVideoStream.Width); // video width
            fileWriter.Write(firstVideoStream.Height); // video height
            fileWriter.SkipBytes(4 * sizeof(uint)); // reserved
            fileWriter.CloseItem(chunk);
        }

        void WriteOdmlHeader()
        {
            var list = fileWriter.OpenList(RIFFListFourCCs.OpenDml);
            var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.OpenDmlHeader);
            fileWriter.Write(streams.OfType<IAviVideoStream>().Max(s => streamsInfo[s.Index].FrameCount)); // total frames in file
            fileWriter.SkipBytes(61 * sizeof(uint)); // reserved
            fileWriter.CloseItem(chunk);
            fileWriter.CloseItem(list);
        }

        void WriteStreamList(IAviStreamInternal stream)
        {
            var list = fileWriter.OpenList(RIFFListFourCCs.Stream);
            WriteStreamHeader(stream);
            WriteStreamFormat(stream);
            WriteStreamName(stream);
            WriteStreamSuperIndex(stream);
            fileWriter.CloseItem(list);
        }

        void WriteStreamHeader(IAviStreamInternal stream)
        {
            var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.StreamHeader);
            stream.WriteHeader();
            fileWriter.CloseItem(chunk);
        }

        void WriteStreamFormat(IAviStreamInternal stream)
        {
            var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.StreamFormat);
            stream.WriteFormat();
            fileWriter.CloseItem(chunk);
        }

        void WriteStreamName(IAviStream stream)
        {
            if (!string.IsNullOrEmpty(stream.Name))
            {
                var bytes = Encoding.ASCII.GetBytes(stream.Name);
                var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.StreamName);
                fileWriter.Write(bytes);
                fileWriter.Write((byte)0);
                fileWriter.CloseItem(chunk);
            }
        }

        void WriteStreamSuperIndex(IAviStream stream)
        {
            var superIndex = streamsInfo[stream.Index].SuperIndex;

            // See AVISUPERINDEX structure
            var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.StreamIndex);
            fileWriter.Write((ushort)4); // DWORDs per entry
            fileWriter.Write((byte)0); // index sub-type
            fileWriter.Write((byte)IndexType.Indexes); // index type
            fileWriter.Write((uint)superIndex.Count); // entries count
            fileWriter.Write((uint)((IAviStreamInternal)stream).ChunkId); // chunk ID of the stream
            fileWriter.SkipBytes(3 * sizeof(uint)); // reserved

            // entries
            foreach (var entry in superIndex)
            {
                fileWriter.Write((ulong)entry.ChunkOffset); // offset of sub-index chunk
                fileWriter.Write((uint)entry.ChunkSize); // size of sub-index chunk
                fileWriter.Write((uint)entry.Duration); // duration of sub-index data (number of frames it refers to)
            }

            fileWriter.CloseItem(chunk);
        }
        #endregion

        #region Index
        void WriteIndex1()
        {
            var chunk = fileWriter.OpenChunk(RIFFChunksFourCCs.Index1);

            var indices = streamsInfo.Select((si, i) => new { si.Index1, ChunkId = (uint)streams.ElementAt(i).ChunkId }).
                Where(a => a.Index1.Count > 0)
                .ToList();
            while (index1Count > 0)
            {
                var minOffset = indices[0].Index1[0].DataOffset;
                var minIndex = 0;
                for (var i = 1; i < indices.Count; i++)
                {
                    var offset = indices[i].Index1[0].DataOffset;
                    if (offset < minOffset)
                    {
                        minOffset = offset;
                        minIndex = i;
                    }
                }

                var index = indices[minIndex];
                fileWriter.Write(index.ChunkId);
                fileWriter.Write(index.Index1[0].IsKeyFrame ? 0x00000010U : 0);
                fileWriter.Write(index.Index1[0].DataOffset);
                fileWriter.Write(index.Index1[0].DataSize);

                index.Index1.RemoveAt(0);
                if (index.Index1.Count == 0)
                    indices.RemoveAt(minIndex);

                index1Count--;
            }

            fileWriter.CloseItem(chunk);
        }

        bool ShouldFlushStreamIndex(IList<StandardIndexEntry> index)
        {
            // Check maximum number of entries
            if (index.Count >= MAX_INDEX_ENTRIES)
                return true;

            // Check relative offset
            if (index.Count > 0 && fileWriter.BaseStream.Position - index[0].DataOffset > uint.MaxValue)
                return true;

            return false;
        }

        void FlushStreamIndex(IAviStreamInternal stream)
        {
            var si = streamsInfo[stream.Index];
            var index = si.StandardIndex;
            var entriesCount = index.Count;
            if (entriesCount == 0)
                return;

            var baseOffset = index[0].DataOffset;
            var indexSize = 24 + entriesCount * 8;

            CreateNewRiffIfNeeded(indexSize);

            // See AVISTDINDEX structure
            var chunk = fileWriter.OpenChunk(si.StandardIndexChunkId, indexSize);
            fileWriter.Write((ushort)2); // DWORDs per entry
            fileWriter.Write((byte)0); // index sub-type
            fileWriter.Write((byte)IndexType.Chunks); // index type
            fileWriter.Write((uint)entriesCount); // entries count
            fileWriter.Write((uint)stream.ChunkId); // chunk ID of the stream
            fileWriter.Write((ulong)baseOffset); // base offset for entries
            fileWriter.SkipBytes(sizeof(uint)); // reserved

            foreach (var entry in index)
            {
                fileWriter.Write((uint)(entry.DataOffset - baseOffset)); // chunk data offset
                fileWriter.Write(entry.DataSize); // chunk data size
            }

            fileWriter.CloseItem(chunk);

            var superIndex = streamsInfo[stream.Index].SuperIndex;
            var newEntry = new SuperIndexEntry
            {
                ChunkOffset = chunk.ItemStart,
                ChunkSize = chunk.ItemSize,
                Duration = entriesCount
            };
            superIndex.Add(newEntry);

            index.Clear();
        }
        #endregion
    }
}
