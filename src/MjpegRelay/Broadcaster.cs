using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Text;

namespace MjpegRelay
{
    public class Broadcaster : IStreamSink
    {
        public const string Boundary = "frame-boundary";
        private const string Newline = "\r\n";

        private readonly ConcurrentDictionary<PipeWriter, bool> _clients = new();

        public int ClientCount => _clients.Count;

        public void AddClient(PipeWriter client)
        {
            _clients.TryAdd(client, false);
        }

        public void RemoveClient(PipeWriter client)
        {
            _clients.Remove(client, out _);
        }

        public void ImageReceived(ReadOnlySequence<byte> imageBytes)
        {
            var boundary = CreateBoundary((int)imageBytes.Length);
            var boundarySize = Encoding.ASCII.GetByteCount(boundary);
            Span<byte> boundaryBytes = stackalloc byte[boundarySize];
            Encoding.ASCII.GetBytes(boundary, boundaryBytes);

            foreach (var client in _clients.Keys)
            {
                client.Write(boundaryBytes);

                foreach (var slice in imageBytes)
                {
                    client.Write(slice.Span);
                }

                _ = client.FlushAsync();
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
