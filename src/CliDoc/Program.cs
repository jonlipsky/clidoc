using System.CommandLine;
using CliDoc.Commands;

namespace CliDoc;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Generate beautiful static documentation for System.CommandLine CLI tools");
        rootCommand.Name = "clidoc";

        // Add subcommands
        rootCommand.AddCommand(InitCommand.Create());
        rootCommand.AddCommand(GenerateCommand.Create());

        return await rootCommand.InvokeAsync(args);
    }

    // Expose this method for documentation generation (dogfooding)
    public static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand("Generate beautiful static documentation for System.CommandLine CLI tools");
        rootCommand.Name = "clidoc";

        rootCommand.AddCommand(InitCommand.Create());
        rootCommand.AddCommand(GenerateCommand.Create());

        return rootCommand;
    }
}
