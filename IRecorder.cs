using System;

namespace Screna
{
    public interface IRecorder
    {
        event Action<Exception> RecordingStopped;

        void Start(int Delay = 0);

        void Stop();

        void Pause();
    }
}
