using Microsoft.AspNetCore.Mvc;
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
    public Task<String> sayHello3(){
        return _greetingService.greet3("beautiful");
    }
}
