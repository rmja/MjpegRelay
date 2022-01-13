using System.Buffers;

namespace MjpegRelay
{
    public interface IStreamSink
    {
        void ImageReceived(ReadOnlySequence<byte> imageBytes);
    }
}
