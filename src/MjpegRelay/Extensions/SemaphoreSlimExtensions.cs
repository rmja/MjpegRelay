namespace System.Threading
{
    public static class SemaphoreSlimExtensions
    {
        public static int TryRelease(this SemaphoreSlim semaphore)
        {
            try
            {
                return semaphore.Release();
            }
            catch (SemaphoreFullException)
            {
                return -1;
            }
        }
    }
}
