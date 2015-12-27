using System.Threading.Tasks;

namespace Screna.Avi
{
    /// <summary>
    /// Adds asynchronous writes support for underlying stream.
    /// </summary>
    class AsyncAudioStreamWrapper : AudioStreamWrapperBase
    {
        readonly SequentialInvoker writeInvoker = new SequentialInvoker();

        public AsyncAudioStreamWrapper(IAviAudioStreamInternal baseStream) : base(baseStream) { }

        public override void WriteBlock(byte[] data, int startIndex, int length)
        {
            writeInvoker.Invoke(() => base.WriteBlock(data, startIndex, length));
        }

        public override Task WriteBlockAsync(byte[] data, int startIndex, int length)
        {
            return writeInvoker.InvokeAsync(() => base.WriteBlock(data, startIndex, length));
        }

        public override void FinishWriting()
        {
            // Perform all pending writes and then let the base stream to finish
            // (possibly writing some more data synchronously)
            writeInvoker.WaitForPendingInvocations();

            base.FinishWriting();
        }
    }
}
