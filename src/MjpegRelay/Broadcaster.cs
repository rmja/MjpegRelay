using Microsoft.Net.Http.Headers;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace MjpegRelay
{
    public class ImageItem
    {
        public DateTime Received { get; set; }
        public byte[] Buffer { get; set; }
        public int Size { get; set; }
    }

    public class Broadcaster : BackgroundService, IStreamSink
    {
        public const string Boundary = "frame-boundary";
        private const string Newline = "\r\n";

        private readonly ConcurrentDictionary<HttpContext, bool> _clients = new();
        private readonly ILogger<Broadcaster> _logger;
        private ImageItem _lastImageWrite = new();
        private ImageItem _lastImageMailbox = new();
        private ImageItem _lastImageRead = new();
        private readonly SemaphoreSlim _imageAvailable = new(0, maxCount: 1);
        private readonly byte[] _boundaryBuffer = new byte[1024];
        private readonly ArrayPool<byte> _arrayPool = ArrayPool<byte>.Shared;


        public int ClientCount => _clients.Count;

        public Broadcaster(ILogger<Broadcaster> logger)
        {
            _logger = logger;
        }

        public void AddClient(HttpContext client)
        {
            client.Response.Headers[HeaderNames.CacheControl] = "no-store, no-cache, max-age=0";
            client.Response.Headers[HeaderNames.ContentType] = $"multipart/x-mixed-replace; boundary={Broadcaster.Boundary}";

            _clients.TryAdd(client, false);
        }

        public void RemoveClient(HttpContext client)
        {
            _clients.Remove(client, out _);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var lastLog = DateTime.UtcNow;
            var logInterval = TimeSpan.FromSeconds(10);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _imageAvailable.WaitAsync(stoppingToken);

                    // Get the last image written from the mailbox
                    _lastImageRead = Interlocked.Exchange(ref _lastImageMailbox, _lastImageRead);

                    var now = DateTime.UtcNow;
                    if (now - lastLog >= logInterval)
                    {
                        var ago = now - _lastImageRead.Received;
                        _logger.LogInformation("Last image was received {TimeSinceLastImage}ms ago.", ago.TotalMilliseconds);

                        var connectedTimes = _clients.Keys.Select(client => now - (DateTime)client.Items["RequestStarted"]).ToArray();
                        _logger.LogInformation("There are currently {ClientCount} connected clients with connection times: {ConnectedTimes}.", ClientCount, connectedTimes);

                        lastLog = now;
                    }

                    await WriteLastImageAsync(_lastImageRead, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Something bad happened writing last image to clients.");
                    throw;
                }
            }
        }

        public void ImageReceived(DateTime timestamp, ReadOnlySequence<byte> imageBytes)
        {
            if (_lastImageWrite.Buffer?.Length < imageBytes.Length)
            {
                _logger.LogInformation("Current image buffer of size {OldBufferSize} is not sufficiently large.", _lastImageWrite.Buffer.Length);
                _arrayPool.Return(_lastImageWrite.Buffer);
                _lastImageWrite.Buffer = null;
            }

            if (_lastImageWrite.Buffer is null)
            {
                _lastImageWrite.Buffer = _arrayPool.Rent((int)imageBytes.Length);
                _logger.LogInformation("Got image buffer of size {NewBufferSize} for image of size {ImageSize}.", _lastImageWrite.Buffer.Length, imageBytes.Length);
            }

            // Copy image into the buffer
            _lastImageWrite.Received = timestamp;
            imageBytes.CopyTo(_lastImageWrite.Buffer);
            _lastImageWrite.Size = (int)imageBytes.Length;

            // Set the last image in the mailbox
            _lastImageWrite = Interlocked.Exchange(ref _lastImageMailbox, _lastImageWrite);

            // Signal the reader
            _imageAvailable.TryRelease();
        }

        private async Task WriteLastImageAsync(ImageItem image, CancellationToken cancellationToken)
        {
            var boundary = CreateBoundary(image.Size);
            var boundarySize = Encoding.ASCII.GetBytes(boundary, _boundaryBuffer);

            foreach (var client in _clients.Keys)
            {
                var body = client.Response.BodyWriter;

                body.Write(_boundaryBuffer.AsSpan(0, boundarySize));
                body.Write(image.Buffer.AsSpan(0, image.Size));

                await body.FlushAsync(cancellationToken);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var item in new[] { _lastImageRead, _lastImageWrite, _lastImageMailbox })
            {
                if (item.Buffer is not null)
                {
                    _arrayPool.Return(item.Buffer);
                    item.Buffer = null;
                }
            }
        }

        private static string CreateBoundary(int contentLength)
        {
            var builder = new StringBuilder();

            builder.Append("--");
            builder.Append(Boundary);
            builder.Append(Newline);

            builder.Append("Content-Type: image/jpeg");
            builder.Append(Newline);

            builder.AppendFormat("Content-Length: {0}", contentLength);
            builder.Append(Newline);
            builder.Append(Newline);

            return builder.ToString();
        }
    }
}
