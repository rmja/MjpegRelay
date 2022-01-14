namespace MjpegRelay
{
    public class StreamService : BackgroundService
    {
        private readonly IStreamSource _source;
        private readonly ILogger<StreamService> _logger;

        public StreamService(IStreamSource source, ILogger<StreamService> logger)
        {
            _source = source;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Connecting to stream");
                    await _source.StreamAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Stream error");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
    }
}
