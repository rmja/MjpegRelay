using System.Buffers;

namespace MjpegRelay
{
    public interface IStreamSink
    {
        void ImageReceived(DateTime timestamp, ReadOnlySequence<byte> imageBytes);
    }
}
