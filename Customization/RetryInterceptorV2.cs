using System.Reflection;
using Castle.DynamicProxy;
using Polly;
using Polly.Contrib.WaitAndRetry;
using rnd001.Customization;

namespace rnd001.Customization
{
    public class RetryInterceptorV2 : IInterceptor
    {
        private readonly ISyncPolicy _defaultRetrySyncPolicy;
        private readonly IAsyncPolicy _defaultRetryAsyncPolicy;
        private readonly LoggerService _loggerService; 
        private readonly PolicyExecutor _policyExecutor;

        public RetryInterceptorV2( IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy, LoggerService loggerService, PolicyExecutor policyExecutor)
        {
            this._defaultRetryAsyncPolicy = asyncPolicy;
            this._defaultRetrySyncPolicy = syncPolicy;
            this._loggerService = loggerService;
            this._policyExecutor= policyExecutor;
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

            if (retryableAttribute != null && retryAttribute != null)
            {
                int retryCount = retryAttribute.RetryCount;
                // Before invoking the method
                _loggerService.Log($"Intercepting method: {invocation.Method.Name}");

                // Check if the method is asynchronous
                if (typeof(Task).IsAssignableFrom(invocation.Method.ReturnType))
                {
                    IAsyncPolicy retryAsyncPolicy = retryCount == 3 ? _defaultRetryAsyncPolicy : CreateAsyncRetryPolicy(retryCount);
                    InterceptAsync(invocation, retryAsyncPolicy);
                }
                else
                {
                    ISyncPolicy retrySyncPolicy = retryCount == 3 ? _defaultRetrySyncPolicy : CreateSyncRetryPolicy(retryCount);
                    InterceptSync(invocation, retrySyncPolicy);
                }
            }

        }

        public delegate Task InvocationDelegate(IInvocation invocation);
        private async void InterceptAsync(IInvocation invocation, IAsyncPolicy retryAsyncPolicy)
        {
            try
            {
                await _policyExecutor.ExecuteWithRetryAsync(async () =>{
                    // Invoke the method asynchronously
                    await (Task)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);
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

        private void InterceptSync(IInvocation invocation, ISyncPolicy retrySyncPolicy)
        {
            try
            {
                // Execute the method within the retry policy
                retrySyncPolicy.Execute(NewMethod(invocation));
            }
            catch (Exception ex)
            {
                // Log the exception
                _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
                throw; // Re-throw the exception to trigger retry
            }
        }

        private Action NewMethod(IInvocation invocation)
        {
            return () =>
            {
                // Invoke the method synchronously
                invocation.Proceed();

                // After invoking the method
                _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
            };
        }
    }
}

public class PolicyExecutor
{

    private readonly LoggerService _loggerService;
    private readonly IAsyncPolicy _retryAsyncPolicy;
    private readonly ISyncPolicy _retrySyncPolicy;
    public PolicyExecutor(LoggerService loggerService, IAsyncPolicy retryAsyncPolicy, ISyncPolicy retrySyncPolicy){
        this._loggerService = loggerService;
        this._retryAsyncPolicy = retryAsyncPolicy;
        this._retrySyncPolicy = retrySyncPolicy;
    }

    public async Task ExecuteWithRetryAsync(Func<Task> func)
    {
        await _retryAsyncPolicy.ExecuteAsync(func);
    }

    public async Task ExecuteWithRetrySync(Action action)
    {
         _retrySyncPolicy.Execute(action);

         await Task.CompletedTask;
    }
}