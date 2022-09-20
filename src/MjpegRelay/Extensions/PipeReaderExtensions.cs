using System.Buffers;
using System.IO.Pipelines;

namespace MjpegRelay
{
    public static class PipeReaderExtensions
    {
        public static ValueTask<ReadOnlySequence<byte>> FullReadAsync(this PipeReader reader, CancellationToken cancellationToken = default)
        {
            if (reader.TryRead(out ReadResult result) && result.IsCompleted)
            {
                return new ValueTask<ReadOnlySequence<byte>>(result.Buffer);
            }

            reader.AdvanceTo(result.Buffer.Start);

            return ReadAwaitedAsync();

            async ValueTask<ReadOnlySequence<byte>> ReadAwaitedAsync()
            {
                while (true)
                {
                    result = await reader.ReadAsync(cancellationToken);
                    if (result.IsCompleted)
                    {
                        return result.Buffer;
                    }

                    reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
                }
            }
        }
    }
}
