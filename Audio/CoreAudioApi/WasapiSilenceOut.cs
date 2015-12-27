using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Screna.Audio
{
    enum PlaybackState { Stopped, Playing, Paused }

    class WasapiSilenceOut
    {
        AudioClient audioClient;
        readonly WasapiAudioDevice mmDevice;
        AudioRenderClient renderClient;
        int latencyMilliseconds, bufferFrameCount, bytesPerFrame;
        EventWaitHandle frameEventWaitHandle;
        volatile PlaybackState playbackState;
        Thread playThread;
        WaveFormat outputFormat;
        readonly SynchronizationContext syncContext;

        public event Action<Exception> PlaybackStopped;

        public WasapiSilenceOut(WasapiAudioDevice device, int latency)
        {
            audioClient = device.AudioClient;
            mmDevice = device;
            latencyMilliseconds = latency;
            syncContext = SynchronizationContext.Current;
            outputFormat = audioClient.MixFormat; // allow the user to query the default format for shared mode streams

            long latencyRefTimes = latencyMilliseconds * 10000;

            int EventCallback = 0x00040000;

            // With EventCallBack and Shared, both latencies must be set to 0 (update - not sure this is true anymore)
            audioClient.Initialize(AudioClientShareMode.Shared, EventCallback, latencyRefTimes, 0, outputFormat, Guid.Empty);

            // Windows 10 returns 0 from stream latency, resulting in maxing out CPU usage later
            var streamLatency = audioClient.StreamLatency;
            if (streamLatency != 0)
                // Get back the effective latency from AudioClient
                latencyMilliseconds = (int)(streamLatency / 10000);

            // Create the Wait Event Handle
            frameEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            audioClient.SetEventHandle(frameEventWaitHandle.SafeWaitHandle.DangerousGetHandle());

            // Get the RenderClient
            renderClient = audioClient.AudioRenderClient;
        }

        void PlayThread()
        {
            Exception e = null;
            try
            {
                // fill a whole buffer
                bufferFrameCount = audioClient.BufferSize;
                bytesPerFrame = outputFormat.Channels * outputFormat.BitsPerSample / 8;
                FillBuffer(bufferFrameCount);

                // Create WaitHandle for sync
                var waitHandles = new WaitHandle[] { frameEventWaitHandle };

                audioClient.Start();

                while (playbackState != PlaybackState.Stopped)
                {
                    // If still playing and notification is ok
                    if (frameEventWaitHandle.WaitOne(3 * latencyMilliseconds) && playbackState == PlaybackState.Playing)
                    {
                        // See how much buffer space is available.
                        int numFramesAvailable = bufferFrameCount - audioClient.CurrentPadding;
                        if (numFramesAvailable > 10) FillBuffer(numFramesAvailable);
                    }
                }

                Thread.Sleep(latencyMilliseconds / 2);
                audioClient.Stop();

                if (playbackState == PlaybackState.Stopped) audioClient.Reset();
            }
            catch (Exception ex) { e = ex; }
            finally
            {
                var handler = PlaybackStopped;
                if (handler != null)
                {
                    if (syncContext == null) handler(e);
                    else syncContext.Post(state => handler(e), null);
                }
            }
        }

        void FillBuffer(int frameCount)
        {
            var buffer = renderClient.GetBuffer(frameCount);
            var readLength = frameCount * bytesPerFrame;

            for (int i = 0; i < readLength; ++i)
                Marshal.WriteByte(buffer, i, 0);

            renderClient.ReleaseBuffer(frameCount);
        }

        public void Play()
        {
            if (playbackState != PlaybackState.Playing)
            {
                if (playbackState == PlaybackState.Stopped)
                {
                    playThread = new Thread(PlayThread);
                    playbackState = PlaybackState.Playing;
                    playThread.Start();
                }
                else playbackState = PlaybackState.Playing;
            }
        }

        public void Stop()
        {
            if (playbackState != PlaybackState.Stopped)
            {
                playbackState = PlaybackState.Stopped;
                playThread.Join();
                playThread = null;
            }
        }

        public void Pause()
        {
            if (playbackState == PlaybackState.Playing)
                playbackState = PlaybackState.Paused;
        }

        public void Dispose()
        {
            if (audioClient != null)
            {
                Stop();

                audioClient.Dispose();
                audioClient = null;
                renderClient = null;
            }
        }
    }
}
