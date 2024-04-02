// using System.Reflection;
// using Castle.DynamicProxy;
// using Polly;
// using Polly.Contrib.WaitAndRetry;

// namespace rnd001.Customization
// {
//     public class RetryInterceptorV4 : IInterceptor
//     {
//         private readonly ISyncPolicy _defaultRetrySyncPolicy;
//         private readonly IAsyncPolicy _defaultRetryAsyncPolicy;
//         private readonly LoggerService _loggerService; 

//         private enum MethodType
//         {
//             Synchronous,
//             AsyncAction,
//             AsyncFunction,
//         }

//         /// <summary>
//         /// Gets the <see cref="MethodType"/> based upon the <paramref name="returnType"/> of the method invocation.
//         /// </summary>
//         private static MethodType GetMethodType(Type returnType)
//         {
//             // If there's no return type, or it's not a task, then assume it's a synchronous method.
//             if (returnType == typeof(void) || !typeof(Task).IsAssignableFrom(returnType))
//                 return MethodType.Synchronous;

//             // The return type is a task of some sort, so assume it's asynchronous
//             return returnType.GetTypeInfo().IsGenericType ? MethodType.AsyncFunction : MethodType.AsyncAction;
//         }

//         public RetryInterceptor( IAsyncPolicy asyncPolicy, ISyncPolicy syncPolicy, LoggerService loggerService)
//         {
//             this._defaultRetryAsyncPolicy = CreateAsyncRetryPolicy(3);//asyncPolicy;
//             this._defaultRetrySyncPolicy = CreateSyncRetryPolicy(3);//syncPolicy;
//             this._loggerService = loggerService;
//         }

//         private ISyncPolicy CreateSyncRetryPolicy(int retryCount)
//         {
//             return Policy
//                 .Handle<Exception>()
//                 //.WaitAndRetry(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
//                 .WaitAndRetry(
//                     Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), retryCount),
//                     onRetry: onRetryAction()
//                   );
//         }
//         private IAsyncPolicy CreateAsyncRetryPolicy(int retryCount)
//         {
//             return Policy
//                 .Handle<Exception>()
//                 //.WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
//                 .WaitAndRetryAsync(
//                     Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromSeconds(1), retryCount),
//                     onRetry: onRetryAction()
//                   );
//         }

//         private Action<Exception, TimeSpan, int, Context> onRetryAction()
//         {
//             return (exception, sleepDuration, attemptNumber, context) =>
//             {
//                 _loggerService.Log("Exception: {0}, Attempt: {1}, SleepDuration : {2} ", exception.Message, attemptNumber, sleepDuration);
//             };
//         }
        
//         public void Intercept(IInvocation invocation)
//         {
//             var retryableAttribute = invocation.MethodInvocationTarget.DeclaringType.GetCustomAttribute<RetryableAttribute>();
//             var retryAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<RetryAttribute>();

//             if(retryableAttribute ==null || retryAttribute==null){
//                 invocation.Proceed();
//                 return;
//             }

//             MethodType methodType = GetMethodType(invocation.Method.ReturnType);
//             switch (methodType)
//             {
//                 case MethodType.AsyncAction:
//                     this.InterceptAsynchronous(invocation);
//                     return;
//                 case MethodType.AsyncFunction:
//                     Type taskReturnType = invocation.Method.ReturnType.GetGenericArguments()[0];
//                     MethodInfo method = invocation.Method.MakeGenericMethod(taskReturnType);
//                     GenericAsyncHandler genericHandler = (GenericAsyncHandler)method.CreateDelegate(typeof(GenericAsyncHandler));
//                     genericHandler.Invoke(invocation, this);
//                     return;
//                 case MethodType.Synchronous:
//                 default:
//                     this.InterceptSynchronous(invocation);
//                     return;
//             }

//             var retryableAttribute = invocation.MethodInvocationTarget.DeclaringType.GetCustomAttribute<RetryableAttribute>();
//             var retryAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<RetryAttribute>();

//             ProcessType processType = ProcessType.ProcessSync;
//             Boolean isRetryable = retryableAttribute != null;
//             Boolean hasRetry = retryAttribute != null;
//             Boolean isAsync = typeof(Task).IsAssignableFrom(invocation.Method.ReturnType);

//             int retryCount= retryAttribute!=null ? retryAttribute.RetryCount:1;
         
//             if (isRetryable)
//             {
//                 processType = isAsync ? 
//                     (hasRetry ? ProcessType.ProcessAsyncWithRetry : ProcessType.ProcessAsync) :
//                     (hasRetry ? ProcessType.ProcessSyncWithRetry : ProcessType.ProcessSync);
//             }
//             else
//             {
//                 processType = isAsync ? ProcessType.ProcessAsync : ProcessType.ProcessSync;
//             }


//             switch(processType){
//                 case ProcessType.ProcessSync:
//                 case ProcessType.ProcessAsync:
//                     invocation.Proceed();
//                     break;
//                 case ProcessType.ProcessSyncWithRetry:
//                     ISyncPolicy retrySyncPolicy = _defaultRetrySyncPolicy;//retryCount == 3 ? _defaultRetrySyncPolicy : CreateSyncRetryPolicy(retryCount);
//                     InterceptSync(invocation, retrySyncPolicy);
//                     break;
//                 case ProcessType.ProcessAsyncWithRetry:
//                     IAsyncPolicy retryAsyncPolicy = _defaultRetryAsyncPolicy;//retryCount == 3 ? _defaultRetryAsyncPolicy : CreateAsyncRetryPolicy(retryCount);
//                     InterceptAsync2(invocation);//, retryAsyncPolicy);
//                     break;
//             }
//         }

//         private async void InterceptAsync(IInvocation invocation, IAsyncPolicy retryAsyncPolicy)
//         {
//             try
//             {
//                 _loggerService.Log("==========>>> async call");

//                 var result =  await retryAsyncPolicy.ExecuteAsync(async () =>
//                 {
//                     // Invoke the method asynchronously
//                     //var task = await (Task)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);
//                     var task = (Task)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);
                    
//                     // Wait for the task to complete and get the result
//                     await task.ConfigureAwait(false);
//                     var res= GetTaskResult(task);
//                     Console.WriteLine(" inside >> Result : {0}" , res);
//                     return res;
//                 });

//                 Console.WriteLine("outer << Result : {0}" , result);

//                 // Set the return value after the method invocation
//                 // invocation.ReturnValue = result;
//                 Console.WriteLine("outer << ReturnValue : {0}" , invocation.ReturnValue);

//                 // After invoking the method
//                 _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
                    
                
//                 // // Execute the method within the async retry policy
//                 // await retryAsyncPolicy.ExecuteAsync(async () => {
//                 //     // Invoke the method
//                 //     invocation.Proceed();

//                 //     // After invoking the method
//                 //     _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
//                 // });
                
//             }
//             catch (Exception ex)
//             {
//                 // Log the exception
//                 _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
//                 throw; // Re-throw the exception to trigger retry
//             }
            
//         }

//         // Helper method to get the result of a Task
//         private static object GetTaskResult(Task task)
//         {
//             if (task == null)
//             {
//                 return null;
//             }
//             if (task.GetType().IsGenericType && task.GetType().GetGenericTypeDefinition() == typeof(Task<>))
//             {
//                 var resultProperty = task.GetType().GetProperty("Result");
//                 return resultProperty?.GetValue(task);
//             }
//             return null;
//         }
//         private void InterceptSync(IInvocation invocation, ISyncPolicy retrySyncPolicy)
//         {
//             try
//             {
//                 _loggerService.Log("==========>>> sync call");
//                 // Execute the method within the sync retry policy
//                 retrySyncPolicy.Execute(() =>
//                 {
//                     // Invoke the method
//                     invocation.Proceed();

//                     // After invoking the method
//                     _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
//                 });
//             }
//             catch (Exception ex)
//             {
//                 // Log the exception
//                 _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
//                 throw; // Re-throw the exception to trigger retry
//             }
//         }
        
//         private async void InterceptAsync2(IInvocation invocation)
//         {
//             try
//             {
//                 // Execute the method asynchronously within the retry policy
//                 object result = await _defaultRetryAsyncPolicy.ExecuteAsync(async () =>
//                 {
//                     // Invoke the method asynchronously
//                     var task = (Task)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);

//                     // Wait for the task to complete and get the result
//                     await task.ConfigureAwait(false);
//                     return GetTaskResult(task);
//                 });

//                 // Set the return value after the method invocation
//                 invocation.ReturnValue = Task.FromResult(result);

//                 // After invoking the method
//                 _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
//             }
//             catch (Exception ex)
//             {
//                 // Log the exception
//                 _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
//                 throw; // Re-throw the exception to trigger retry
//             }
//         }

//         // public void InterceptAsync3(IInvocation invocation)
//         // {
//         //     // Before invoking the method
//         //     _loggerService.Log($"Intercepting method: {invocation.Method.Name}");

//         //     // Wrap the retry policy with asynchronous execution
//         //     var wrapAsyncPolicy = Policy.WrapAsync(_defaultRetryAsyncPolicy);
//         //     wrapAsyncPolicy.ExecuteAsync(async () =>
//         //     {
//         //         try
//         //         {
//         //             // Invoke the method asynchronously
//         //             invocation.Proceed();

//         //             // After invoking the method
//         //             _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
//         //         }
//         //         catch (Exception ex)
//         //         {
//         //             // Log the exception
//         //             _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
//         //             throw; // Re-throw the exception to trigger retry
//         //         }
//         //     }).Wait(); // Wait for the asynchronous execution to complete
//         // }

//         // private async void InterceptAsync4(IInvocation invocation)
//         // {
//         //     try
//         //     {
//         //         // Execute the method asynchronously within the retry policy
//         //        Task<PolicyResult> resultTask =  _defaultRetryAsyncPolicy.ExecuteAndCaptureAsync(async () =>
//         //         {
//         //              invocation.Proceed();
//         //             // Invoke the method asynchronously
//         //             // var task = (Task)invocation.Method.Invoke(invocation.InvocationTarget, invocation.Arguments);

//         //             // Wait for the task to complete and get the result
//         //             // return await task;
//         //         });
//         //         PolicyResult<object> result = null;// await resultTask;

//         //         if (result.Outcome == OutcomeType.Successful)
//         //         {
//         //             // Operation succeeded, read the result value
//         //             string value = (String)result.Result;
//         //             Console.WriteLine($"Operation succeeded with result: {value}");
//         //         }
//         //         else if (result.Outcome == OutcomeType.Failure)
//         //         {
//         //             // Operation failed
//         //             Exception exception = result.FinalException;
//         //             Console.WriteLine($"Operation failed with exception: {exception.Message}");
//         //         }
//         //         else if (result.Outcome == OutcomeType.None)
//         //         {
//         //             // Policy was canceled
//         //             Console.WriteLine("Policy execution was canceled.");
//         //         }

//         //         // Set the return value after the method invocation
//         //         invocation.ReturnValue = Task.FromResult(result);

//         //         // After invoking the method
//         //         _loggerService.Log($"Method {invocation.Method.Name} executed successfully");
//         //     }
//         //     catch (Exception ex)
//         //     {
//         //         // Log the exception
//         //         _loggerService.Log($"Exception occurred in method {invocation.Method.Name}: {ex.Message}");
//         //         throw; // Re-throw the exception to trigger retry
//         //     }
//         // }
        
//     }
// }
