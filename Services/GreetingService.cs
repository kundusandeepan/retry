
namespace rnd001;

public class GreetingService : IGreetingService
{
    public String greet(String name){
        return $"hello {name}";
    }

    public Task<string> greet2(string name)
    {
         return Task.FromResult( greet(name));
    }

    public Task<string> greet3(string name)
    {
        return greet2(name);
    }
}
