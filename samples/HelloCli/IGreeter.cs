namespace HelloCli;

public interface IGreeter
{
    void Greet(string name, bool loud);
}

public class ConsoleGreeter : IGreeter
{
    public void Greet(string name, bool loud)
    {
        var greeting = $"Hello, {name}!";
        Console.WriteLine(loud ? greeting.ToUpperInvariant() : greeting);
    }
}
