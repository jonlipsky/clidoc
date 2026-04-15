using System.CommandLine;
using Clidoc.SystemCommandLine;
using HelloCli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection()
    .AddLogging(b => b.AddConsole())
    .AddSingleton<IGreeter, ConsoleGreeter>()
    .BuildServiceProvider();

var root = new RootCommand("A tiny DI-based sample CLI for clidoc.");
root.Subcommands.Add(new GreetCommand(services));
root.Subcommands.Add(new WaveCommand(services));

// Add `hello commands` so that `hello commands --output commands.json`
// produces a clidoc-compatible document.
root.AddCommandsSubcommand();

return await root.Parse(args).InvokeAsync();
