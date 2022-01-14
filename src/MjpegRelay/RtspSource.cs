using Microsoft.Extensions.Options;
using RtspClientSharp;
using RtspClientSharp.RawFrames;
using RtspClientSharp.RawFrames.Video;
using System.Buffers;

namespace MjpegRelay
{
    public class RtspSource : IStreamSource
    {
        private readonly IStreamSink _sink;
        private readonly RtspOptions _options;
        private byte[] _imageBuffer;

        public RtspSource(IStreamSink sink, IOptions<RtspOptions> options)
        {
            _sink = sink;
            _options = options.Value;
        }

        public async Task StreamAsync(CancellationToken cancellationToken = default)
        {
            var connectionParameters = new ConnectionParameters(new Uri(_options.StreamUrl))
            {
                RtpTransport = _options.Transport
            };
            using var client = new RtspClient(connectionParameters);
            client.FrameReceived += FrameReceived;
            
            await client.ConnectAsync(cancellationToken);
            await client.ReceiveAsync(cancellationToken);
        }

        private void FrameReceived(object sender, RawFrame e)
        {
            if (e is not RawJpegFrame jpeg)
            {
                throw new NotSupportedException();
            }

            if (jpeg.Type != FrameType.Video)
            {
                return;
            }

            var imageSize = jpeg.FrameSegment.Count;
            if (_imageBuffer is null || _imageBuffer.Length < imageSize)
            {
                // There is no buffer or it is too small
                _imageBuffer = jpeg.FrameSegment.ToArray();
            }
            else
            {
                // Reuse existing buffer
                jpeg.FrameSegment.CopyTo(_imageBuffer);
            }

            var imageBytes = new ReadOnlySequence<byte>(_imageBuffer, 0, imageSize);
            _sink.ImageReceived(jpeg.Timestamp, imageBytes);
        }
    }
}
