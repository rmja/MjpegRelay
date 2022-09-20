using Microsoft.AspNetCore.WebUtilities;
using System.Reflection;

namespace MjpegRelay
{
    public class CustomMultipartReader : MultipartReader
    {
        private static readonly FieldInfo _currentStreamField = typeof(MultipartReader).GetField("_currentStream", BindingFlags.Instance | BindingFlags.NonPublic);

        public CustomMultipartReader(string boundary, Stream stream, int headersLengthLimit = DefaultHeadersLengthLimit) : base(boundary, stream)
        {
            HeadersLengthLimit = headersLengthLimit;

            // https://github.com/dotnet/aspnetcore/blob/main/src/Http/WebUtilities/src/MultipartReader.cs#L69
            SetCurrentStreamLengthLimit(headersLengthLimit);
        }

        private Stream GetCurrentStream() => (Stream)_currentStreamField.GetValue(this);

        private void SetCurrentStreamLengthLimit(long? value)
        {
            var currentStream = GetCurrentStream();
            var property = currentStream.GetType().GetProperty("LengthLimit", BindingFlags.Instance | BindingFlags.Public);
            property.SetValue(currentStream, value);
        }
    }
}
