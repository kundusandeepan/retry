using System.Linq.Expressions;
using System.Reflection;
using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using Polly;
using Polly.Retry;

namespace rnd001.Customization
{
    public class RetryInterceptor : IInterceptor
    {
        // private readonly IRetryService _retryService;
        private readonly ISyncPolicy _defaultRetrySyncPolicy;
        private readonly IAsyncPolicy _defaultRetryAsyncPolicy;

        public RetryInterceptor( IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy)
        {
            // _retryService = retryService;
            this._defaultRetryAsyncPolicy = asyncPolicy;
            this._defaultRetrySyncPolicy = syncPolicy;
        }

        private static ISyncPolicy CreateSyncRetryPolicy(int retryCount)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        private static IAsyncPolicy CreateAsyncRetryPolicy(int retryCount)
        {
            return Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public void Intercept(IInvocation invocation)
        {

            var retryableAttribute = invocation.MethodInvocationTarget.DeclaringType.GetCustomAttribute<RetryableAttribute>();
            var retryAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<RetryAttribute>();

            if (retryableAttribute != null && retryAttribute != null)
            {
                int retryCount = retryAttribute.RetryCount;
                // Before invoking the method
                Console.WriteLine($"Intercepting method: {invocation.Method.Name}");

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
        // IAsyncPolicy _retryAsyncPolicy = CreateAsyncRetryPolicy(3);
        // ISyncPolicy _retrySyncPolicy = CreateSyncRetryPolicy(3);

        private async void InterceptAsync(IInvocation invocation, IAsyncPolicy retryAsyncPolicy)
        {
            try
            {
                // Execute the method asynchronously within the retry policy
                await retryAsyncPolicy.ExecuteAsync(async () =>
                {
                    // Invoke the method asynchronously
                    await (Task)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);

                    // After invoking the method
                    Console.WriteLine($"Method {invocation.Method.Name} executed successfully");
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
                throw; // Re-throw the exception to trigger retry
            }
        }

        private void InterceptSync(IInvocation invocation, ISyncPolicy retrySyncPolicy)
        {
            try
            {
                // Execute the method within the retry policy
                retrySyncPolicy.Execute(() =>
                {
                    // Invoke the method synchronously
                    invocation.Proceed();

                    // After invoking the method
                    Console.WriteLine($"Method {invocation.Method.Name} executed successfully");
                });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
                throw; // Re-throw the exception to trigger retry
            }
        }
    }
}