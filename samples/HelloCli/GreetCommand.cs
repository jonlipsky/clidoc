using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace HelloCli;

public class GreetCommand : Command
{
    private readonly IServiceProvider _services;

    public GreetCommand(IServiceProvider services) : base("greet", "Say hello to someone.")
    {
        _services = services;

        var nameArg = new Argument<string>("name") { Description = "Who to greet." };
        var loudOpt = new Option<bool>("--loud", ["-l"])
        {
            Description = "Shout the greeting in uppercase.",
            DefaultValueFactory = _ => false
        };

        Arguments.Add(nameArg);
        Options.Add(loudOpt);

        SetAction(parseResult =>
        {
            var greeter = _services.GetRequiredService<IGreeter>();
            greeter.Greet(parseResult.GetValue(nameArg)!, parseResult.GetValue(loudOpt));
            return 0;
        });
    }
}
