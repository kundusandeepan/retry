using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.Contrib.WaitAndRetry;
using rnd001.Customization;

namespace rnd001.Controller;

[ApiController]
[Produces("application/json", new []{ "application/json"})]
public class HelloController
{
    private readonly IGreetingService _greetingService;

    public HelloController(IGreetingService greetingService){
        this._greetingService = greetingService;
    }

    [HttpGet("/api/sayhello")]
    public String sayHello(){
        return _greetingService.greet("beautiful");
    }

    [HttpGet("/api/sayhello2")]
    public Task<String> sayHello2(){
        return _greetingService.greet2("beautiful");
    }

    [HttpGet("/api/sayhello3")]
    public async Task<String> sayHello3(){
        Task<String> aa = _greetingService.greet3("beautiful");

        return await aa;
    }


    [HttpGet("/api/sayhello4")]
    public async Task<String> sayHello4(){
        
        IAsyncPolicy retryPolicy= CreateAsyncRetryPolicy(5);
        
        Task<String> actionTask =  retryPolicy.ExecuteAsync(async () =>
        {
            return await _greetingService.greet2("beautiful");
        });

        return await actionTask;
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
                Console.WriteLine("Exception: {0}, Attempt: {1}, SleepDuration : {2} ", exception.Message, attemptNumber, sleepDuration);
            };
        }
}
