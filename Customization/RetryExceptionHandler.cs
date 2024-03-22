// using Polly;
// using rnd001.Customization;

// public class RetryExceptionHandler
// {
//     private readonly LoggerService _loggerService; 
//     public RetryExceptionHandler(LoggerService loggerService){
//         this._loggerService = loggerService;
//     }

//     // public void HandleRetry(Action<Exception, TimeSpan> onRetry){
        
//     // }
//     public void HandleRetry(Exception exception, TimeSpan delay)
//     {
//         // Your retry handling logic here
//         Console.WriteLine($"Exception occurred: {exception.Message}");
//         Console.WriteLine($"Delay before retry: {delay.TotalMilliseconds} milliseconds");
        
//         // Optionally, you can throw the exception again to retry
//         // throw exception;
//     }
//     public void HandleRetry(Exception exception, TimeSpan delay, int retryCount, Context context)
//     {
//         // Your retry handling logic here
//         Console.WriteLine($"Exception occurred: {exception.Message}");
//         Console.WriteLine($"Retry attempt: {retryCount}");
//         Console.WriteLine($"Delay before retry: {delay.TotalMilliseconds} milliseconds");
        
//         // You can access additional context information if needed
//         if (context != null)
//         {
//             Console.WriteLine($"Context information: {context}");
//         }
        
//         // Optionally, you can throw the exception again to retry
//         // throw exception;
//     }
// }

// // public class CosmosPolicy : Policy
// // {
// //     protected override TResult Implementation<TResult>(Func<Context, CancellationToken, TResult> action, Context context, CancellationToken cancellationToken)
// //     {
// //         throw new NotImplementedException();
// //     }
// // }