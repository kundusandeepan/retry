using Autofac.Extras.DynamicProxy;
using rnd001.Customization;

namespace rnd001;

[Retryable]
public class RandomGreetingService : IGreetingService, IRetryableService
{
    [Retry(2)]
    public String greet(String name)
    {
        Console.Write("invoking greet");
        String lotteryNumber = $"{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}";
        return $"hello {name}!, your lucky lottery number is {lotteryNumber}";
    }

    private int GetRandomNumber()
    {
        Random random = new Random();
        int number = random.Next(100);
        if(number <10) throw new Exception();
        return number;
    }
}
