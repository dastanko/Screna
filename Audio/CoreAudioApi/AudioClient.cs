﻿using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    /// <summary>
    /// Windows CoreAudio AudioClient
    /// </summary>
    class AudioClient : IDisposable
    {
        IAudioClient audioClientInterface;
        WaveFormat mixFormat;
        AudioRenderClient audioRenderClient;
        AudioCaptureClient audioCaptureClient;
        AudioClientShareMode shareMode;

        public AudioClient(IAudioClient audioClientInterface)
        {
            this.audioClientInterface = audioClientInterface;
        }

        /// <summary>
        /// Retrieves the stream format that the audio engine uses for its internal processing of shared-mode streams.
        /// Can be called before initialize
        /// </summary>
        public WaveFormat MixFormat
        {
            get
            {
                if (mixFormat == null)
                {
                    IntPtr waveFormatPointer;
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetMixFormat(out waveFormatPointer));
                    var waveFormat = WaveFormat.MarshalFromPtr(waveFormatPointer);
                    Marshal.FreeCoTaskMem(waveFormatPointer);
                    mixFormat = waveFormat;
                }
                return mixFormat;
            }
        }

        /// <summary>
        /// Initializes the Audio Client
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="streamFlags">Stream Flags</param>
        /// <param name="bufferDuration">Buffer Duration</param>
        /// <param name="periodicity">Periodicity</param>
        /// <param name="waveFormat">Wave Format</param>
        /// <param name="audioSessionGuid">Audio Session GUID (can be null)</param>
        public void Initialize(AudioClientShareMode shareMode, int streamFlags, long bufferDuration, long periodicity, WaveFormat waveFormat, Guid audioSessionGuid)
        {
            this.shareMode = shareMode;
            var hresult = audioClientInterface.Initialize(shareMode, streamFlags, bufferDuration, periodicity, waveFormat, ref audioSessionGuid);
            Marshal.ThrowExceptionForHR(hresult);
            // may have changed the mix format so reset it
            mixFormat = null;
        }

        /// <summary>
        /// Retrieves the size (maximum capacity) of the audio buffer associated with the endpoint. (must initialize first)
        /// </summary>
        public int BufferSize
        {
            get
            {
                uint bufferSize;
                Marshal.ThrowExceptionForHR(audioClientInterface.GetBufferSize(out bufferSize));
                return (int)bufferSize;
            }
        }

        /// <summary>
        /// Retrieves the maximum latency for the current stream and can be called any time after the stream has been initialized.
        /// </summary>
        public long StreamLatency => audioClientInterface.GetStreamLatency();

        /// <summary>
        /// Retrieves the number of frames of padding in the endpoint buffer (must initialize first)
        /// </summary>
        public int CurrentPadding
        {
            get
            {
                int currentPadding;
                Marshal.ThrowExceptionForHR(audioClientInterface.GetCurrentPadding(out currentPadding));
                return currentPadding;
            }
        }

        /// <summary>
        /// Gets the AudioRenderClient service
        /// </summary>
        public AudioRenderClient AudioRenderClient
        {
            get
            {
                if (audioRenderClient == null)
                {
                    object audioRenderClientInterface;
                    var audioRenderClientGuid = new Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioRenderClientGuid, out audioRenderClientInterface));
                    audioRenderClient = new AudioRenderClient((IAudioRenderClient)audioRenderClientInterface);
                }
                return audioRenderClient;
            }
        }

        /// <summary>
        /// Gets the AudioCaptureClient service
        /// </summary>
        public AudioCaptureClient AudioCaptureClient
        {
            get
            {
                if (audioCaptureClient == null)
                {
                    object audioCaptureClientInterface;
                    var audioCaptureClientGuid = new Guid("c8adbd64-e71e-48a0-a4de-185c395cd317");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioCaptureClientGuid, out audioCaptureClientInterface));
                    audioCaptureClient = new AudioCaptureClient((IAudioCaptureClient)audioCaptureClientInterface);
                }
                return audioCaptureClient;
            }
        }

        /// <summary>
        /// Determines whether if the specified output format is supported
        /// </summary>
        /// <param name="shareMode">The share mode.</param>
        /// <param name="desiredFormat">The desired format.</param>
        /// <returns>True if the format is supported</returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode, WaveFormat desiredFormat)
        {
            WaveFormatExtensible closestMatchFormat;
            return IsFormatSupported(shareMode, desiredFormat, out closestMatchFormat);
        }

        /// <summary>
        /// Determines if the specified output format is supported in shared mode
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="desiredFormat">Desired Format</param>
        /// <param name="closestMatchFormat">Output The closest match format.</param>
        /// <returns>True if the format is supported</returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode, WaveFormat desiredFormat, out WaveFormatExtensible closestMatchFormat)
        {
            var hresult = audioClientInterface.IsFormatSupported(shareMode, desiredFormat, out closestMatchFormat);
            
            const int UnsupportedFormat = unchecked((int)0x88890008);

            switch (hresult)
            {
                case 0:
                    return true;
                case 1:
                case UnsupportedFormat:
                    return false;
                default:
                    throw new Exception(hresult.ToString());
            }
        }

        /// <summary>
        /// Starts the audio stream
        /// </summary>
        public void Start() => audioClientInterface.Start();

        /// <summary>
        /// Stops the audio stream.
        /// </summary>
        public void Stop() => audioClientInterface.Stop();

        /// <summary>
        /// Set the Event Handle for buffer synchro.
        /// </summary>
        /// <param name="eventWaitHandle">The Wait Handle to setup</param>
        public void SetEventHandle(IntPtr eventWaitHandle) => audioClientInterface.SetEventHandle(eventWaitHandle);

        /// <summary>
        /// Resets the audio stream
        /// Reset is a control method that the client calls to reset a stopped audio stream. 
        /// Resetting the stream flushes all pending data and resets the audio clock stream 
        /// position to 0. This method fails if it is called on a stream that is not stopped
        /// </summary>
        public void Reset() => audioClientInterface.Reset();

        public void Dispose()
        {
            if (audioClientInterface != null)
            {
                if (audioRenderClient != null)
                {
                    audioRenderClient.Dispose();
                    audioRenderClient = null;
                }
         
                if (audioCaptureClient != null)
                {
                    audioCaptureClient.Dispose();
                    audioCaptureClient = null;
                }
                
                Marshal.ReleaseComObject(audioClientInterface);
                audioClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }
    }
}
