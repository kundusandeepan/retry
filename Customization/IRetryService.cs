namespace rnd001.Customization
{
    public interface IRetryService
    {
        // void ExecuteWithRetry(Action method, int retryCount);
        T ExecuteWithRetry<T>(Func<T> method, int retryCount);
    }
}