using System.Reflection;
using Castle.DynamicProxy;
using Polly;
using Polly.Contrib.WaitAndRetry;

namespace rnd001.Customization
{
    public class RetryInterceptor : IInterceptor
    {
        private readonly ISyncPolicy _defaultRetrySyncPolicy;
        private readonly IAsyncPolicy _defaultRetryAsyncPolicy;
        private readonly LoggerService _loggerService; 

        public RetryInterceptor( IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy, LoggerService loggerService)
        {
            this._defaultRetryAsyncPolicy = asyncPolicy;
            this._defaultRetrySyncPolicy = syncPolicy;
            this._loggerService = loggerService;
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
        
        public void Intercept(IInvocation invocation)
        {

            var retryableAttribute = invocation.MethodInvocationTarget.DeclaringType.GetCustomAttribute<RetryableAttribute>();
            var retryAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<RetryAttribute>();

            ProcessType processType = ProcessType.ProcessSync;
            Boolean isRetryable = retryableAttribute != null;
            Boolean hasRetry = retryAttribute != null;
            Boolean isAsync = typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);

            int retryCount= retryAttribute!=null ? retryAttribute.RetryCount:1;
         
            if (isRetryable)
            {
                processType = isAsync ? 
                    (hasRetry ? ProcessType.ProcessAsyncWithRetry : ProcessType.ProcessAsync) :
                    (hasRetry ? ProcessType.ProcessSyncWithRetry : ProcessType.ProcessSync);
            }
            else
            {
                processType = isAsync ? ProcessType.ProcessAsync : ProcessType.ProcessSync;
            }

           /* if (isRetryable && isAsync && hasRetry)
            {
                processType = ProcessType.ProcessAsyncWithRetry;
            }
            else if (isRetryable && isAsync)
            {
                processType = ProcessType.ProcessAsync;
            }
            else if (isRetryable && hasRetry)
            {
                processType = ProcessType.ProcessSyncWithRetry;
            }
            else
            {
                processType = isAsync ? ProcessType.ProcessAsync : ProcessType.ProcessSync;
            }*/

            switch(processType){
                case ProcessType.ProcessSync:
                case ProcessType.ProcessAsync:
                    invocation.Proceed();
                    break;
                case ProcessType.ProcessSyncWithRetry:
                    ISyncPolicy retrySyncPolicy = retryCount == 3 ? _defaultRetrySyncPolicy : CreateSyncRetryPolicy(retryCount);
                    InterceptSync(invocation, retrySyncPolicy);
                    break;
                case ProcessType.ProcessAsyncWithRetry:
                    IAsyncPolicy retryAsyncPolicy = retryCount == 3 ? _defaultRetryAsyncPolicy : CreateAsyncRetryPolicy(retryCount);
                    InterceptAsync(invocation, retryAsyncPolicy);
                    break;
            }

            // if (retryableAttribute != null )
            // {
                
            //     int retryCount = retryAttribute.RetryCount;
            //     // Before invoking the method
            //     _loggerService.Log($"Intercepting method: {invocation.Method.Name}");

            //     // Check if the method is asynchronous
            //     if (typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
            //     {
            //         if(retryableAttribute!=null){
            //             IAsyncPolicy retryAsyncPolicy = retryCount == 3 ? _defaultRetryAsyncPolicy : CreateAsyncRetryPolicy(retryCount);
            //             InterceptAsync(invocation, retryAsyncPolicy);
            //         }else{
            //             invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);
            //         }
            //     }
            //     else
            //     {
            //         if(retryableAttribute!=null){
            //             ISyncPolicy retrySyncPolicy = retryCount == 3 ? _defaultRetrySyncPolicy : CreateSyncRetryPolicy(retryCount);
            //             InterceptSync(invocation, retrySyncPolicy);
            //         }
            //         else{
            //             invocation.Proceed();
            //         }
            //     }
               
            // }

        }

        private async void InterceptAsync(IInvocation invocation, IAsyncPolicy retryAsyncPolicy)
        {
            try
            {
                object returnValue = null;
                // Execute the method asynchronously within the retry policy
                await retryAsyncPolicy.ExecuteAsync(async () =>
                {
                    _loggerService.Log("Async call");
                    // Invoke the method asynchronously
                    returnValue = await (Task)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);

                    // After invoking the method
                    _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
                });
                // Set the return value after the method invocation
                invocation.ReturnValue = returnValue;
            }
            catch (Exception ex)
            {
                // Log the exception
                _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
                throw; // Re-throw the exception to trigger retry
            }
        }

        private void InterceptSync(IInvocation invocation, ISyncPolicy retrySyncPolicy)
        {
            try
            {
                _loggerService.Log("sync call");
                // Execute the method within the retry policy
                retrySyncPolicy.Execute(() =>
                {
                    // Invoke the method synchronously
                    invocation.Proceed();

                    // After invoking the method
                    _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
                throw; // Re-throw the exception to trigger retry
            }
        }
    }
}

public enum ProcessType{
    ProcessSync,
    ProcessAsync,
    ProcessSyncWithRetry,
    ProcessAsyncWithRetry,
}
