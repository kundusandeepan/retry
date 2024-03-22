using rnd001.Customization;

namespace rnd001;

[Retryable]
public class RandomGreetingService : IGreetingService
{
    [Retry(5)]
    public String greet(String name)
    {
        Console.Write("invoking greet");
        String lotteryNumber = $"{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}";
        return $"hello {name}!, your lucky lottery number is {lotteryNumber}";
    }

    public Task<String> greet2(string name)
    {
        Console.Write("invoking greet");
        String lotteryNumber = $"{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}-{GetRandomNumber()}";
        return Task.FromResult( $"hello {name}!, your lucky lottery number is {lotteryNumber}");
    }

    [Retry(10)]
    public Task<String> greet3(string name)
    {
        return greet2(name);
    }

    private int GetRandomNumber()
    {
        Random random = new Random();
        int number = random.Next(100);
        if(number <10) throw new Exception();
        return number;
    }
}
