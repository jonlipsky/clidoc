using System.CommandLine;

namespace CliDoc.Commands;

/// <summary>
/// <c>clidoc generate</c> — grouping for the two generation subcommands:
/// <c>commands</c> (extract commands.json from a System.CommandLine assembly/project)
/// and <c>docs</c> (render a static site from commands.json).
/// </summary>
public static class GenerateCommand
{
    public static Command Create()
    {
        var command = new Command("generate", "Generate commands.json or a documentation site.")
        {
            GenerateCommandsCommand.Create(),
            GenerateDocsCommand.Create()
        };
        return command;
    }
}
