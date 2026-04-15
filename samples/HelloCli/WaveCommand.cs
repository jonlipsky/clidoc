using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HelloCli;

public class WaveCommand : Command
{
    private readonly IServiceProvider _services;

    public WaveCommand(IServiceProvider services) : base("wave", "Wave at someone (logs at info level).")
    {
        _services = services;

        var nameArg = new Argument<string>("name") { Description = "Who to wave at." };
        Arguments.Add(nameArg);

        SetAction(parseResult =>
        {
            var logger = _services.GetRequiredService<ILoggerFactory>().CreateLogger<WaveCommand>();
            logger.LogInformation("Waving at {Name}", parseResult.GetValue(nameArg));
            return 0;
        });
    }
}
