using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Nerdbank.Streams;
using System.Buffers;
using System.IO.Pipelines;

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

            boundary = HeaderUtilities.RemoveQuotes(boundary).ToString();

            using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var multipartReader = new MultipartReader(boundary, responseStream);

            var pipe = new Pipe(new PipeOptions(useSynchronizationContext: false));

            while (!cancellationToken.IsCancellationRequested)
            {
                var section = await multipartReader.ReadNextSectionAsync(cancellationToken);
                var timestamp = DateTime.UtcNow; // Maybe get from header if available?

                var sectionStream = section.Body;

                var writeTask = CopyAndCompleteAsync(sectionStream, pipe.Writer, cancellationToken).AsTask();
                var readTask = pipe.Reader.FullReadAsync(cancellationToken).AsTask();

                await Task.WhenAll(writeTask, readTask);

                var imageBytes = await readTask;
                _sink.ImageReceived(timestamp, imageBytes);

                await pipe.Reader.CompleteAsync();

                pipe.Reset();
            }
        }

        private static async ValueTask CopyAndCompleteAsync(Stream source, PipeWriter writer, CancellationToken cancellationToken)
        {
            await PipeReader.Create(source).CopyToAsync(writer, cancellationToken);
            await writer.CompleteAsync();
        }
    }
}
