using rnd001.Customization;

namespace rnd001;

public interface IGreetingService
{
    String greet(String name);

    Task<String> greet2(String name);

    Task<String> greet3(String name);
}
