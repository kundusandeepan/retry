using System.Reflection;
using Castle.DynamicProxy;
using Polly;
using Polly.Contrib.WaitAndRetry;
using rnd001.Customization;

namespace rnd001.Customization
{
    public class RetryInterceptorV2 : IAsyncInterceptor
    {
        // private readonly ISyncPolicy _defaultRetrySyncPolicy;
        // private readonly IAsyncPolicy _defaultRetryAsyncPolicy;
        private readonly LoggerService _loggerService;
        // // private readonly PolicyExecutor _policyExecutor;

        public RetryInterceptorV2(LoggerService loggerService)//, PolicyExecutor policyExecutor)
        {
            // IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy,
            // this._defaultRetryAsyncPolicy = asyncPolicy;
            // this._defaultRetrySyncPolicy = syncPolicy;
            this._loggerService = loggerService;
            // this._policyExecutor= policyExecutor;
        }


        public void InterceptSynchronous(IInvocation invocation)
        {
            RetryAttribute retryAttribute = checkRetry(invocation);
            // Step 1. Do something prior to invocation.

            ISyncPolicy policy = CreateSyncRetryPolicy(retryAttribute.RetryCount);

            if(retryAttribute==null)
                invocation.Proceed();
            else
                policy.Execute(invocation.Proceed);

            //policy.Execute(invocation.Proceed);

            // Step 2. Do something after invocation.
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }
        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }


        private async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            RetryAttribute retryAttribute = checkRetry(invocation);
            IAsyncPolicy policy = CreateAsyncRetryPolicy(retryAttribute.RetryCount);
            // Step 1. Do something prior to invocation.

            Task task = (retryAttribute == null)
                        ? asyncInvocation(invocation)
                        : policy.ExecuteAsync(() =>
                          {
                            return asyncInvocation(invocation);
                          });
            await task;
            // Step 2. Do something after invocation.
        }

        private static Task asyncInvocation(IInvocation invocation)
        {
            invocation.Proceed();
            var task = (Task)invocation.ReturnValue;
            return task;
        }

        private async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            RetryAttribute retryAttribute = checkRetry(invocation);
            IAsyncPolicy policy = CreateAsyncRetryPolicy(retryAttribute.RetryCount);
            // Step 1. Do something prior to invocation.

            Task<TResult> task = (retryAttribute == null)
                            ? asyncInvocation<TResult>(invocation)
                            : policy.ExecuteAsync(() =>
                            {
                                return asyncInvocation<TResult>(invocation);
                            });

            TResult result = await task;
            // Step 2. Do something after invocation.
            return result;
        }

        private static Task<TResult> asyncInvocation<TResult>(IInvocation invocation)
        {
            invocation.Proceed();
            var task = (Task<TResult>)invocation.ReturnValue;
            return task;
        }

        RetryAttribute? checkRetry(IInvocation invocation)
        {
            var retryableAttribute = invocation.MethodInvocationTarget.DeclaringType.GetCustomAttribute<RetryableAttribute>();
            var retryAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<RetryAttribute>();
            if (retryableAttribute != null && retryAttribute != null)
            {
                return retryAttribute;
            }
            return null;
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

    }
}


// public class PolicyExecutor
// {

//     private readonly LoggerService _loggerService;
//     private readonly IAsyncPolicy _retryAsyncPolicy;
//     private readonly ISyncPolicy _retrySyncPolicy;
//     public PolicyExecutor(LoggerService loggerService, IAsyncPolicy retryAsyncPolicy, ISyncPolicy retrySyncPolicy){
//         this._loggerService = loggerService;
//         this._retryAsyncPolicy = retryAsyncPolicy;
//         this._retrySyncPolicy = retrySyncPolicy;
//     }

//     public async Task ExecuteWithRetryAsync(Func<Task> func)
//     {
//         await _retryAsyncPolicy.ExecuteAsync(func);
//     }

//     public async Task ExecuteWithRetrySync(Action action)
//     {
//          _retrySyncPolicy.Execute(action);

//          await Task.CompletedTask;
//     }
// }