namespace MjpegRelay
{
    public interface IStreamSource
    {
        Task StreamAsync(CancellationToken cancellationToken);
    }
}
