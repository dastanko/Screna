using System;
using System.IO;

namespace Screna.Audio
{
    /// <summary>
    /// This class writes WAV data to a .wav file on disk
    /// </summary>
    public class WaveFileWriter : IAudioFileWriter
    {
        Stream outStream;
        readonly BinaryWriter writer;
        long dataSizePos, factSampleCountPos, dataChunkSize;
        readonly WaveFormat format;

        /// <summary>
        /// WaveFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public WaveFileWriter(Stream outStream, WaveFormat format)
        {
            this.outStream = outStream;
            this.format = format;
            writer = new BinaryWriter(outStream, System.Text.Encoding.UTF8);
            writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            writer.Write((int)0); // placeholder
            writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

            writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            
            writer.Write((int)(18 + format.ExtraSize)); // wave format length
            format.Serialize(writer);

            // CreateFactChunk
            if (HasFactChunk)
            {
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fact"));
                writer.Write((int)4);
                factSampleCountPos = outStream.Position;
                writer.Write((int)0); // number of samples
            }

            // WriteDataChunkHeader
            writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            dataSizePos = outStream.Position;
            writer.Write((int)0); // placeholder
        }

        /// <summary>
        /// Creates a new WaveFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public WaveFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format) { }

        bool HasFactChunk
        {
            get
            {
                return format.Encoding != WaveFormatEncoding.Pcm &&
                    format.BitsPerSample != 0;
            }
        }

        /// <summary>
        /// Number of bytes of audio in the data chunk
        /// </summary>
        public long Length => dataChunkSize;

        /// <summary>
        /// WaveFormat of this wave file
        /// </summary>
        public WaveFormat WaveFormat => format;

        /// <summary>
        /// Gets the Position in the WaveFile (i.e. number of bytes written so far)
        /// </summary>
        public long Position => dataChunkSize;

        /// <summary>
        /// Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public void Write(byte[] data, int offset, int count)
        {
            if (outStream.Length + count > UInt32.MaxValue)
                throw new ArgumentException("WAV file too large", "count");
            outStream.Write(data, offset, count);
            dataChunkSize += count;
        }

        readonly byte[] value24 = new byte[3]; // keep this around to save us creating it every time

        /// <summary>
        /// Writes a single sample to the Wave file
        /// </summary>
        /// <param name="sample">the sample to write (assumed floating point with 1.0f as max value)</param>
        public void WriteSample(float sample)
        {
            if (WaveFormat.BitsPerSample == 16)
            {
                writer.Write((short)(short.MaxValue * sample));
                dataChunkSize += 2;
            }
            else if (WaveFormat.BitsPerSample == 24)
            {
                var value = BitConverter.GetBytes((int)(Int32.MaxValue * sample));
                value24[0] = value[1];
                value24[1] = value[2];
                value24[2] = value[3];
                writer.Write(value24);
                dataChunkSize += 3;
            }
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                writer.Write(ushort.MaxValue * (int)sample);
                dataChunkSize += 4;
            }
            else if (WaveFormat.Encoding == WaveFormatEncoding.Float)
            {
                writer.Write(sample);
                dataChunkSize += 4;
            }
            else throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
        }

        /// <summary>
        /// Writes 32 bit floating point samples to the Wave file
        /// They will be converted to the appropriate bit depth depending on the WaveFormat of the WAV file
        /// </summary>
        /// <param name="samples">The buffer containing the floating point samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of floating point samples to write</param>
        public void WriteSamples(float[] samples, int offset, int count)
        {
            for (int n = 0; n < count; n++)
                WriteSample(samples[offset + n]);
        }

        /// <summary>
        /// Writes 16 bit samples to the Wave file
        /// </summary>
        /// <param name="samples">The buffer containing the 16 bit samples</param>
        /// <param name="offset">The offset from which to start writing</param>
        /// <param name="count">The number of 16 bit samples to write</param>
        public void WriteSamples(short[] samples, int offset, int count)
        {
            // 16 bit PCM data
            if (WaveFormat.BitsPerSample == 16)
            {
                for (int sample = 0; sample < count; sample++)
                    writer.Write(samples[sample + offset]);
                dataChunkSize += (count * 2);
            }
            // 24 bit PCM data
            else if (WaveFormat.BitsPerSample == 24)
            {
                byte[] value;

                for (int sample = 0; sample < count; sample++)
                {
                    value = BitConverter.GetBytes(ushort.MaxValue * (int)samples[sample + offset]);
                    value24[0] = value[1];
                    value24[1] = value[2];
                    value24[2] = value[3];
                    writer.Write(value24);
                }
                dataChunkSize += (count * 3);
            }
            // 32 bit PCM data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Extensible)
            {
                for (int sample = 0; sample < count; sample++)
                    writer.Write(ushort.MaxValue * (int)samples[sample + offset]);
                dataChunkSize += (count * 4);
            }
            // IEEE float data
            else if (WaveFormat.BitsPerSample == 32 && WaveFormat.Encoding == WaveFormatEncoding.Float)
            {
                for (int sample = 0; sample < count; sample++)
                    writer.Write((float)samples[sample + offset] / (float)(short.MaxValue + 1));
                dataChunkSize += (count * 4);
            }
            else throw new InvalidOperationException("Only 16, 24 or 32 bit PCM or IEEE float audio data supported");
        }

        /// <summary>
        /// Ensures data is written to disk
        /// Also updates header, so that WAV file will be valid up to the point currently written
        /// </summary>
        public void Flush()
        {
            var pos = writer.BaseStream.Position;
            UpdateHeader(writer);
            writer.BaseStream.Position = pos;
        }

        public void Dispose()
        {
            DoDispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Actually performs the close, making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        void DoDispose()
        {
            if (outStream != null)
            {
                try { UpdateHeader(writer); }
                finally
                {
                    // in a finally block as we don't want the FileStream to run its disposer in
                    // the GC thread if the code above caused an IOException (e.g. due to disk full)
                    outStream.Close(); // will close the underlying base stream
                    outStream = null;
                }
            }
            if (writer != null) writer.Dispose();
        }

        /// <summary>
        /// Updates the header with file size information
        /// </summary>
        void UpdateHeader(BinaryWriter writer)
        {
            writer.Flush();

            // UpdateRiffChunk
            writer.Seek(4, SeekOrigin.Begin);
            writer.Write((uint)(outStream.Length - 8));

            // UpdateFactChunk
            if (HasFactChunk)
            {
                int bitsPerSample = (format.BitsPerSample * format.Channels);
                if (bitsPerSample != 0)
                {
                    writer.Seek((int)factSampleCountPos, SeekOrigin.Begin);

                    writer.Write((int)((dataChunkSize * 8) / bitsPerSample));
                }
            }

            // UpdateDataChunk
            writer.Seek((int)dataSizePos, SeekOrigin.Begin);
            writer.Write((uint)dataChunkSize);
        }

        /// <summary>
        /// Finaliser - should only be called if the user forgot to close this WaveFileWriter
        /// </summary>
        ~WaveFileWriter() { DoDispose(); }
    }
}
