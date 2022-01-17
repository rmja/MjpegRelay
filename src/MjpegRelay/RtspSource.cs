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
        private readonly ILogger<RtspSource> _logger;

        public RtspSource(IStreamSink sink, IOptions<RtspOptions> options, ILogger<RtspSource> logger)
        {
            _sink = sink;
            _options = options.Value;
            _logger = logger;
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

            var imageBytes = new ReadOnlySequence<byte>(jpeg.FrameSegment);
            _sink.ImageReceived(jpeg.Timestamp, imageBytes);
        }
    }
}
