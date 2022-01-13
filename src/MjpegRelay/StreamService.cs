namespace MjpegRelay
{
    public class StreamService : BackgroundService
    {
        private readonly IStreamSource _source;

        public StreamService(IStreamSource source)
        {
            _source = source;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _source.StreamAsync(stoppingToken);
        }
    }
}
