using System;
using System.Drawing;
using System.Threading.Tasks;

namespace Screna
{
    public interface IVideoFileWriter : IDisposable
    {
        Task WriteFrameAsync(Bitmap Image);

        bool RecordsAudio { get; }

        int FrameRate { get; }

        void WriteAudio(byte[] Buffer, int Count);
    }
}
