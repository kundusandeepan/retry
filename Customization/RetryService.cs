using Polly;
namespace rnd001.Customization
{
    public class RetryService : IRetryService
    {
        // private readonly IAsyncPolicy _retryPolicy;
        // public void ExecuteWithRetry(Action method, int retryCount)
        // {
        //     var policy = Policy
        //     .Handle<Exception>()
        //     .Retry(retryCount, (exception, retryAttempt) =>
        //     {
        //         // Log or handle exception if needed
        //         Console.WriteLine($"Attempt {retryAttempt} failed: {exception.Message}");
        //         Console.WriteLine("Retrying...");
        //     });

        //     policy.Execute(method);
        // }

        public T ExecuteWithRetry<T>(Func<T> method, int retryCount)
        {
            var policy = Policy
            .Handle<Exception>()
            .Retry(retryCount, (exception, retryAttempt) =>
            {
                // Log or handle exception if needed
                Console.WriteLine($"Attempt {retryAttempt} failed: {exception.Message}");
                Console.WriteLine("Retrying...");
            });
            return policy.Execute(method);
        }
    }
}