using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.SignalR.Protocol;
using Polly;
using Polly.Contrib.WaitAndRetry;
using rnd001.Customization;

namespace rnd001.Customization
{
    public class RetryInterceptorV3 : IInterceptor,IAsyncInterceptor
    {
        // private static readonly MethodInfo HandleAsyncMethodInfo =
        // typeof(AsyncDeterminationInterceptor)
        //         .GetMethod(nameof(HandleAsyncWithResult), BindingFlags.Static | BindingFlags.NonPublic)!;

        // private static readonly ConcurrentDictionary<Type, GenericAsyncHandler> GenericAsyncHandlers = new();

        private delegate void GenericAsyncHandler(IInvocation invocation, IAsyncInterceptor asyncInterceptor);

        private enum MethodType
        {
            Synchronous,
            AsyncAction,
            AsyncFunction,
        }

        // /// <summary>
        // /// Gets the underlying async interceptor.
        // /// </summary>
        // public IAsyncInterceptor AsyncInterceptor { get; }

        /// <summary>
        /// This method is created as a delegate and used to make the call to the generic
        /// <see cref="IAsyncInterceptor.InterceptAsynchronous{T}"/> method.
        /// </summary>
        /// <typeparam name="TResult">The type of the <see cref="Task{T}"/> <see cref="Task{T}.Result"/> of the method
        /// <paramref name="invocation"/>.</typeparam>
        private void HandleAsyncWithResult<TResult>(IInvocation invocation, IAsyncInterceptor asyncInterceptor)
        {
            this.InterceptAsynchronous<TResult>(invocation);
        }


        private readonly LoggerService _loggerService;
        // // private readonly PolicyExecutor _policyExecutor;

        public RetryInterceptorV3(LoggerService loggerService)//, PolicyExecutor policyExecutor)
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

        /// <summary>
        /// Gets the <see cref="MethodType"/> based upon the <paramref name="returnType"/> of the method invocation.
        /// </summary>
        private static MethodType GetMethodType(Type returnType)
        {
            // If there's no return type, or it's not a task, then assume it's a synchronous method.
            if (returnType == typeof(void) || !typeof(Task).IsAssignableFrom(returnType))
                return MethodType.Synchronous;

            // The return type is a task of some sort, so assume it's asynchronous
            return returnType.GetTypeInfo().IsGenericType ? MethodType.AsyncFunction : MethodType.AsyncAction;
        }

        // /// <summary>
        // /// Gets the <see cref="GenericAsyncHandler"/> for the method invocation <paramref name="returnType"/>.
        // /// </summary>
        // private static GenericAsyncHandler GetHandler(Type returnType)
        // {
        //     // GenericAsyncHandler handler = GenericAsyncHandlers.GetOrAdd(returnType, CreateHandler);
        //     // return handler;
        //     return CreateHandler(returnType);
        // }

        // /// <summary>
        // /// Creates the generic delegate for the <paramref name="returnType"/> method invocation.
        // /// </summary>
        // private static GenericAsyncHandler CreateHandler(Type returnType)
        // {
        //     Type taskReturnType = returnType.GetGenericArguments()[0];
        //     MethodInfo method = InvocationBindingFailureMessage.me.MakeGenericMethod(taskReturnType);
        //     return (GenericAsyncHandler)method.CreateDelegate(typeof(GenericAsyncHandler));
        // }


        public void Intercept(IInvocation invocation)
        {
             MethodType methodType = GetMethodType(invocation.Method.ReturnType);

            switch (methodType)
            {
                case MethodType.AsyncAction:
                    this.InterceptAsynchronous(invocation);
                    return;
                case MethodType.AsyncFunction:
                    Type taskReturnType = invocation.Method.ReturnType.GetGenericArguments()[0];
                    MethodInfo method = invocation.Method.MakeGenericMethod(taskReturnType);
                    GenericAsyncHandler genericHandler = (GenericAsyncHandler)method.CreateDelegate(typeof(GenericAsyncHandler));
                    genericHandler.Invoke(invocation, this);
                    
                    // this.InterceptAsynchronous
                    return;
                case MethodType.Synchronous:
                default:
                    this.InterceptSynchronous(invocation);
                    return;
            }
        }
    }
}