using Microsoft.Net.Http.Headers;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;

namespace MjpegRelay
{
    public class Broadcaster : BackgroundService, IStreamSink
    {
        public const string Boundary = "frame-boundary";
        private const string Newline = "\r\n";

        private readonly ConcurrentDictionary<HttpContext, bool> _clients = new();
        private readonly ILogger<Broadcaster> _logger;
        private DateTime? _lastImageTimestamp;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var ago = now - _lastImageTimestamp;
                _logger.LogInformation("Last image was received {TimeSinceLastImage}ms ago.", ago?.TotalMilliseconds);

                var connectedTimes = _clients.Keys.Select(client => now - (DateTime)client.Items["RequestStarted"]).ToArray();
                _logger.LogInformation("There are currently {ClientCount} connected clients with connection times: {ConnectedTimes}.", ClientCount, connectedTimes);
                //foreach (var client in _clients.Keys)
                //{
                //    var requestStarted = (DateTime)client.Items["RequestStarted"];
                //    var connected = now - requestStarted;
                //    _logger.LogInformation("Client {ClientIp} has been connected for {ConnectedMinutes} minutes and {Connected} seconds.", client.Connection.RemoteIpAddress, (int)connected.TotalMinutes, (int)connected.Seconds);
                //}
                await Task.Delay(5000, stoppingToken);
            }
        }

        public void ImageReceived(DateTime timestamp, ReadOnlySequence<byte> imageBytes)
        {
            _lastImageTimestamp = timestamp;

            var boundary = CreateBoundary((int)imageBytes.Length);
            var boundarySize = Encoding.ASCII.GetByteCount(boundary);
            Span<byte> boundaryBytes = stackalloc byte[boundarySize];
            Encoding.ASCII.GetBytes(boundary, boundaryBytes);

            foreach (var client in _clients.Keys)
            {
                var body = client.Response.BodyWriter;

                body.Write(boundaryBytes);

                foreach (var slice in imageBytes)
                {
                    body.Write(slice.Span);
                }

                _ = body.FlushAsync();
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
