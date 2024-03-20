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
}
