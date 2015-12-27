using System;
using System.Threading;

namespace Screna.Audio
{
    public class AudioRecorder : IRecorder
    {
        IAudioFileWriter Writer;
        IAudioProvider AudioProvider;
        SynchronizationContext syncContext;

        public AudioRecorder(IAudioProvider provider, IAudioFileWriter writer)
        {
            AudioProvider = provider;
            Writer = writer;

            syncContext = SynchronizationContext.Current;

            AudioProvider.DataAvailable += (data, length) => Writer.Write(data, 0, length);
            AudioProvider.RecordingStopped += (e) =>
            {
                var handler = RecordingStopped;

                if (handler != null)
                {
                    if (syncContext != null) syncContext.Post((s) => handler(e), null);
                    else handler(e);
                }
            };
        }

        public void Start(int Delay = 0) { AudioProvider.Start(); }

        public void Stop()
        {
            if (AudioProvider != null) AudioProvider.Dispose();

            if (Writer != null)
            {
                Writer.Dispose();
                Writer = null;
            }
        }

        public void Pause() { AudioProvider.Stop(); }

        public event Action<Exception> RecordingStopped;
    }
}
