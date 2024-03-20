namespace rnd001;

public class GreetingService : IGreetingService
{
    public String greet(String name){
        return $"hello {name}";
    }

}
