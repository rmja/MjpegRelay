using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Nerdbank.Streams;
using System.Buffers;

namespace MjpegRelay
{
    public class MjpegSource : IStreamSource
    {
        private readonly HttpClient _client;
        private readonly IStreamSink _sink;
        private readonly MjpegOptions _options;

        public MjpegSource(HttpClient client, IStreamSink sink, IOptions<MjpegOptions> options)
        {
            _client = client;
            _sink = sink;
            _options = options.Value;
        }

        public async Task StreamAsync(CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, _options.StreamUrl);
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            // Read boundary
            var contentType = response.Content.Headers.ContentType;
            if (contentType.MediaType != "multipart/x-mixed-replace")
            {
                throw new Exception("Invalid content type");
            }
            var boundary = contentType.Parameters.FirstOrDefault(x => x.Name == "boundary")?.Value;
            if (boundary is null)
            {
                throw new Exception("Boundary was not found");
            }

            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var multipartReader = new CustomMultipartReader(boundary, responseStream, headersLengthLimit: 64 * 1024)
            {
                BodyLengthLimit = null,
                HeadersCountLimit = 64,
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                var section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                var timestamp = DateTime.UtcNow; // Maybe get from header if available?

                var sectionLength = int.Parse(section.Headers[HeaderNames.ContentLength]);
                var buffer = ArrayPool<byte>.Shared.Rent(sectionLength);

                var stream = section.Body;
                using var sequence = new Sequence<byte>(ArrayPool<byte>.Shared);

                while (true)
                {
                    var count = await stream.ReadAsync(buffer, cancellationToken);
                    if (count == 0)
                    {
                        break;
                    }

                    sequence.Write(buffer.AsSpan(0, count));
                }
                
                _sink.ImageReceived(timestamp, sequence.AsReadOnlySequence);
            }
        }
    }
}
