using System.Reflection;
using Castle.DynamicProxy;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace rnd001.Customization
{
    public class SimpleRetryInterceptor : IInterceptor
    {
        private readonly LoggerService _loggerService; 

        public SimpleRetryInterceptor(LoggerService loggerService){
            this._loggerService = loggerService;
        }

        public void Intercept(IInvocation invocation)
        {

            var retryableAttribute = invocation.MethodInvocationTarget.DeclaringType.GetCustomAttribute<RetryableAttribute>();
            var retryAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<RetryAttribute>();


            if(retryableAttribute!=null & retryAttribute!=null){

                Boolean isAsync = typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);
                if(isAsync){
                    IAsyncPolicy asyncPolicy = CreateAsyncRetryPolicy(3);

                    // Execute the intercepted method within the Polly policy
                    asyncPolicy.ExecuteAsync(async () =>
                        {
                            // Logic before the method call
                            Console.WriteLine($"Before calling method: {invocation.Method.Name}");

                            // Proceed with the original method call asynchronously
                            await Task.Run(() => invocation.Proceed());

                            // Logic after the method call
                            Console.WriteLine($"After calling method: {invocation.Method.Name}");
                        }).GetAwaiter().GetResult();
                }
                else{
                    ISyncPolicy syncPolicy = CreateSyncRetryPolicy(3);

                    syncPolicy.Execute(() =>
                            {
                                // Logic before the method call
                                Console.WriteLine($"Before calling method: {invocation.Method.Name}");

                                // Proceed with the original method call
                                invocation.Proceed();

                                // Logic after the method call
                                Console.WriteLine($"After calling method: {invocation.Method.Name}");
                            });
                }
                
            }
            else{
                invocation.Proceed();
            }
        }

         private ISyncPolicy CreateSyncRetryPolicy(int retryCount)
        {
            return Policy
                .Handle<Exception>()
                //.WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
                .WaitAndRetry(
                    Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), retryCount),
                    onRetry: onRetryAction()
                  );
        }
        private IAsyncPolicy CreateAsyncRetryPolicy(int retryCount)
        {
            return Policy
                .Handle<Exception>()
                //.WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
                .WaitAndRetryAsync(
                    Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), retryCount),
                    onRetry: onRetryAction()
                  );
        }

        private Action<Exception, TimeSpan, int, Context> onRetryAction()
        {
            return (exception, sleepDuration, attemptNumber, context) =>
            {
                _loggerService.Log("Exception: {0}, Attempt: {1}, SleepDuration : {2} ", exception.Message, attemptNumber, sleepDuration);
            };
        }
    }
}
