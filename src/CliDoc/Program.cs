using System.CommandLine;
using Clidoc.SystemCommandLine;
using CliDoc.Commands;

namespace CliDoc;

static class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = GetRootCommand();
        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync(new InvocationConfiguration(), CancellationToken.None);
    }

    // Expose this method for documentation generation (dogfooding)
    public static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand("Generate beautiful static documentation for System.CommandLine CLI tools");

        rootCommand.Subcommands.Add(InitCommand.Create());
        rootCommand.Subcommands.Add(GenerateCommand.Create());
        rootCommand.AddCommandsSubcommand();

        return rootCommand;
    }
}
