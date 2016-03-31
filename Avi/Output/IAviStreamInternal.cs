using System;

namespace Screna.Avi
{
    /// <summary>
    /// Interface of streams used for internal workings of <see cref="AviInternalWriter"/>.
    /// </summary>
    interface IAviStreamInternal : IAviStream
    {
        /// <summary>
        /// Stream type written in <c>AVISTREAMHEADER</c>.
        /// </summary>
        FourCC StreamType { get; }

        /// <summary>
        /// Chunk ID for stream data.
        /// </summary>
        FourCC ChunkId { get; }

        /// <summary>
        /// Prepares the stream for writing.
        /// </summary>
        /// <remarks>
        /// Called by <see cref="AviInternalWriter"/> when writing starts. More exactly,
        /// on the first call to the <c>Write</c> method of any stream, before any data is actually written.
        /// </remarks>
        void PrepareForWriting();

        /// <summary>
        /// Finishes writing of the stream.
        /// </summary>
        /// <remarks>
        /// Called by <see cref="AviInternalWriter"/> just before it closes (if writing had started).
        /// Allows to write a final data to the stream.
        /// This is not appropriate place for freeing resources, better to implement <see cref="IDisposable"/>.
        /// All streams are disposed on disposing of <see cref="AviInternalWriter"/> even if writing had not yet started.
        /// </remarks>
        void FinishWriting();

        /// <summary>
        /// Called to delegate writing of the stream header to a proper overload
        /// of <c>IAviStreamWriteHandler.WriteStreamHeader</c>.
        /// </summary>
        void WriteHeader();

        /// <summary>
        /// Called to delegate writing of the stream format to a proper overload
        /// of <c>IAviStreamWriteHandler.WriteStreamFormat</c>.
        /// </summary>
        void WriteFormat();
    }
}
