using System;
using System.Collections.Generic;

namespace Screna.Audio
{
    public sealed class WasapiLoopbackCapture : WasapiCapture
    {
        WasapiSilenceOut SilencePlayer;

        public WasapiLoopbackCapture(bool IncludeSilence) : this(DefaultDevice, IncludeSilence) { }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="LoopbackDevice">Capture device to use</param>
        public WasapiLoopbackCapture(WasapiAudioDevice LoopbackDevice, bool IncludeSilence = true)
            : base(LoopbackDevice)
        {
            if (IncludeSilence)
                SilencePlayer = new WasapiSilenceOut(LoopbackDevice, 100);
        }

        public override void Start()
        {
            if (SilencePlayer != null) SilencePlayer.Play();

            base.Start();
        }

        public override void Stop()
        {
            base.Stop();

            if (SilencePlayer != null) SilencePlayer.Stop();
        }

        public override void Dispose()
        {
            base.Dispose();

            if (SilencePlayer != null)
            {
                SilencePlayer.Dispose();
                SilencePlayer.Stop();
            }
        }

        /// <summary>
        /// Gets the default audio loopback capture device
        /// </summary>
        /// <returns>The default audio loopback capture device</returns>
        public new static WasapiAudioDevice DefaultDevice
        {
            get { return WasapiAudioDevice.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia); }
        }

        public new static IEnumerable<WasapiAudioDevice> EnumerateDevices()
        {
            return WasapiAudioDevice.EnumerateAudioEndPoints(DataFlow.Render);
        }

        /// <summary>
        /// Capturing wave format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return base.WaveFormat; }
            set { throw new InvalidOperationException("WaveFormat cannot be set for WASAPI Loopback Capture"); }
        }

        /// <summary>
        /// Specify loopback
        /// </summary>
        protected override int AudioClientStreamFlags => 0x00020000;
    }
}
