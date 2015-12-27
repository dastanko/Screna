using Screna.Audio;
using System;

namespace Screna.Avi
{
    /// <summary>
    /// Wrapper on the <see cref="IAviAudioStreamInternal"/> object to provide encoding.
    /// </summary>
    class EncodingAudioStreamWrapper : AudioStreamWrapperBase
    {
        readonly IAudioEncoder encoder;
        readonly bool ownsEncoder;
        byte[] encodedBuffer;
        readonly object syncBuffer = new object();

        public EncodingAudioStreamWrapper(IAviAudioStreamInternal baseStream, IAudioEncoder encoder, bool ownsEncoder)
            : base(baseStream)
        {
            this.encoder = encoder;
            this.ownsEncoder = ownsEncoder;
        }

        public override void Dispose()
        {
            if (ownsEncoder)
            {
                var encoderDisposable = encoder as IDisposable;
                if (encoderDisposable != null)
                    encoderDisposable.Dispose();
            }

            base.Dispose();
        }

        public override WaveFormat WaveFormat
        {
            get { return encoder.WaveFormat; }
            set { ThrowPropertyDefinedByEncoder(); }
        }

        /// <summary>
        /// Encodes and writes a block of audio data.
        /// </summary>
        public override void WriteBlock(byte[] data, int startIndex, int length)
        {
            // Prevent accessing encoded buffer by multiple threads simultaneously
            lock (syncBuffer)
            {
                EnsureBufferIsSufficient(length);
                var encodedLength = encoder.Encode(data, startIndex, length, encodedBuffer, 0);
                if (encodedLength > 0)
                    base.WriteBlock(encodedBuffer, 0, encodedLength);
            }
        }

        public override System.Threading.Tasks.Task WriteBlockAsync(byte[] data, int startIndex, int length)
        {
            throw new NotSupportedException("Asynchronous writes are not supported.");
        }

        public override void PrepareForWriting()
        {
            // Set properties of the base stream
            BaseStream.WaveFormat = WaveFormat;

            base.PrepareForWriting();
        }

        public override void FinishWriting()
        {
            // Prevent accessing encoded buffer by multiple threads simultaneously
            lock (syncBuffer)
            {
                // Flush the encoder
                EnsureBufferIsSufficient(0);
                var encodedLength = encoder.Flush(encodedBuffer, 0);
                if (encodedLength > 0)
                    base.WriteBlock(encodedBuffer, 0, encodedLength);
            }

            base.FinishWriting();
        }


        void EnsureBufferIsSufficient(int sourceCount)
        {
            var maxLength = encoder.GetMaxEncodedLength(sourceCount);
            if (encodedBuffer != null && encodedBuffer.Length >= maxLength)
                return;

            var newLength = encodedBuffer == null ? 1024 : encodedBuffer.Length * 2;
            while (newLength < maxLength)
                newLength *= 2;

            encodedBuffer = new byte[newLength];
        }

        void ThrowPropertyDefinedByEncoder() { throw new NotSupportedException("The value of the property is defined by the encoder."); }
    }
}
