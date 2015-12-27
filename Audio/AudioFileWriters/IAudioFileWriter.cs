using System;

namespace Screna.Audio
{
    public interface IAudioFileWriter : IDisposable
    {
        void Write(byte[] data, int offset, int count);

        void Flush();
    }
}
