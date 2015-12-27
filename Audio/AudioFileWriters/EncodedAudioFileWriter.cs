using Screna.Audio;
using System;
using System.IO;

namespace Screna.Audio
{
    public class EncodedAudioFileWriter : IAudioFileWriter
    {
        readonly object SyncLock = new object();
        BinaryWriter Writer;

        IAudioEncoder Encoder;
        byte[] EncodedBuffer;

        public EncodedAudioFileWriter(Stream OutStream, IAudioEncoder Encoder)
        {
            Writer = new BinaryWriter(OutStream);

            this.Encoder = Encoder;

            Encoder.WaveFormat.Serialize(Writer);
        }

        public EncodedAudioFileWriter(string FilePath, IAudioEncoder Encoder)
            : this(new FileStream(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), Encoder) { }
        
        /// <summary>
        /// Encodes and writes a block of audio data.
        /// </summary>
        public void Write(byte[] Data, int Offset, int Length)
        {
            // Prevent accessing encoded buffer by multiple threads simultaneously
            lock (SyncLock)
            {
                Encoder.EnsureBufferIsSufficient(ref EncodedBuffer, Length);

                var EncodedLength = Encoder.Encode(Data, Offset, Length, EncodedBuffer, 0);

                if (EncodedLength > 0)
                    Writer.Write(EncodedBuffer, 0, EncodedLength);
            }
        }

        public void Flush()
        {
            lock (SyncLock)
            {
                // Flush the encoder
                Encoder.EnsureBufferIsSufficient(ref EncodedBuffer, 0);

                var EncodedLength = Encoder.Flush(EncodedBuffer, 0);

                if (EncodedLength > 0)
                    Writer.Write(EncodedBuffer, 0, EncodedLength);
            }
        }

        public void Dispose()
        {
            Flush();

            Writer.Dispose();
            Encoder.Dispose();
        }
    }
}
