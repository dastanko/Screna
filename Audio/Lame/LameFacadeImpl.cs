using System;
using System.Runtime.InteropServices;

namespace Screna.Audio
{
    public class LameFacadeImpl : ILameFacade, IDisposable
    {
        readonly IntPtr Context;
        bool IsClosed;

        public LameFacadeImpl()
        {
            Context = lame_init();
            CheckResult(Context != IntPtr.Zero, "lame_init");
        }

        ~LameFacadeImpl() { Dispose(); }

        public void Dispose()
        {
            if (!IsClosed)
            {
                lame_close(Context);
                IsClosed = true;
            }
        }
        
        public int ChannelCount
        {
            get { return lame_get_num_channels(Context); }
            set { lame_set_num_channels(Context, value); }
        }

        public int InputSampleRate
        {
            get { return lame_get_in_samplerate(Context); }
            set { lame_set_in_samplerate(Context, value); }
        }

        public int OutputBitRate
        {
            get { return lame_get_brate(Context); }
            set { lame_set_brate(Context, value); }
        }

        public int OutputSampleRate { get { return lame_get_out_samplerate(Context); } }

        public int FrameSize { get { return lame_get_framesize(Context); } }

        public int EncoderDelay { get { return lame_get_encoder_delay(Context); } }

        public void PrepareEncoding()
        {
            // Set mode
            switch (ChannelCount)
            {
                case 1:
                    lame_set_mode(Context, MpegMode.Mono);
                    break;
                case 2:
                    lame_set_mode(Context, MpegMode.Stereo);
                    break;
                default:
                    ThrowInvalidChannelCount();
                    break;
            }

            // Disable VBR
            lame_set_VBR(Context, VbrMode.Off);

            // Prevent output of redundant headers
            lame_set_write_id3tag_automatic(Context, false);
            lame_set_bWriteVbrTag(Context, 0);

            // Ensure not decoding
            lame_set_decode_only(Context, 0);

            // Finally, initialize encoding process
            CheckResult(lame_init_params(Context) == 0, "lame_init_params");
        }

        public int Encode(byte[] Source, int SourceOffset, int SampleCount, byte[] Destination, int DestinationOffset)
        {
            GCHandle SourceHandle = GCHandle.Alloc(Source, GCHandleType.Pinned),
                DestinationHandle = GCHandle.Alloc(Destination, GCHandleType.Pinned);

            try
            {
                IntPtr SourcePtr = new IntPtr(SourceHandle.AddrOfPinnedObject().ToInt64() + SourceOffset),
                    DestinationPtr = new IntPtr(DestinationHandle.AddrOfPinnedObject().ToInt64() + DestinationOffset);

                int OutputSize = Destination.Length - DestinationOffset,
                    Result = -1;

                switch (ChannelCount)
                {
                    case 1:
                        Result = lame_encode_buffer(Context, SourcePtr, SourcePtr, SampleCount, DestinationPtr, OutputSize);
                        break;
                    case 2:
                        Result = lame_encode_buffer_interleaved(Context, SourcePtr, SampleCount / 2, DestinationPtr, OutputSize);
                        break;
                    default:
                        ThrowInvalidChannelCount();
                        break;
                }

                CheckResult(Result >= 0, "lame_encode_buffer");

                return Result;
            }
            finally
            {
                SourceHandle.Free();
                DestinationHandle.Free();
            }
        }

        public int FinishEncoding(byte[] Destination, int DestinationOffset)
        {
            GCHandle DestinationHandle = GCHandle.Alloc(Destination, GCHandleType.Pinned);

            try
            {
                IntPtr DestinationPtr = new IntPtr(DestinationHandle.AddrOfPinnedObject().ToInt64() + DestinationOffset);

                int DestinationLength = Destination.Length - DestinationOffset,
                    Result = lame_encode_flush(Context, DestinationPtr, DestinationLength);

                CheckResult(Result >= 0, "lame_encode_flush");
                return Result;
            }
            finally { DestinationHandle.Free(); }
        }

        static void CheckResult(bool passCondition, string routineName)
        {
            if (!passCondition)
                throw new ExternalException(string.Format("{0} failed", routineName));
        }

        static void ThrowInvalidChannelCount() { throw new InvalidOperationException("Set ChannelCount to 1 or 2"); }

        #region LAME DLL API
        const string DLL_NAME = "lame_enc.dll";

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr lame_init();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_close(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_in_samplerate(IntPtr context, int value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_in_samplerate(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_num_channels(IntPtr context, int value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_num_channels(IntPtr context);

        enum MpegMode : int
        {
            Stereo = 0,
            JointStereo = 1,
            DualChannel = 2,
            Mono = 3,
            NotSet = 4,
        }

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_mode(IntPtr context, MpegMode value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern MpegMode lame_get_mode(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_brate(IntPtr context, int value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_brate(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_out_samplerate(IntPtr context, int value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_out_samplerate(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern void lame_set_write_id3tag_automatic(IntPtr context, bool value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern bool lame_get_write_id3tag_automatic(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_bWriteVbrTag(IntPtr context, int value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_bWriteVbrTag(IntPtr context);

        enum VbrMode : int
        {
            Off = 0,
            MarkTaylor = 1,
            RogerHegemann = 2,
            AverageBitRate = 3,
            MarkTaylorRogerHegemann = 4,
        }

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_VBR(IntPtr context, VbrMode value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern VbrMode lame_get_VBR(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_set_decode_only(IntPtr context, int value);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_decode_only(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_encoder_delay(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_get_framesize(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_init_params(IntPtr context);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_encode_buffer(IntPtr context,
            IntPtr buffer_l, IntPtr buffer_r, int nsamples,
            IntPtr mp3buf, int mp3buf_size);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_encode_buffer_interleaved(IntPtr context,
            IntPtr buffer, int nsamples,
            IntPtr mp3buf, int mp3buf_size);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        static extern int lame_encode_flush(IntPtr context, IntPtr mp3buf, int mp3buf_size);
        #endregion
    }
}
