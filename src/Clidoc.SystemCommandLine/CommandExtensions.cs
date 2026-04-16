using System.CommandLine;

namespace Clidoc.SystemCommandLine;

public static class CommandExtensions
{
    /// <summary>
    /// Adds a subcommand that emits a clidoc-compatible commands.json for the tree rooted at
    /// <paramref name="parent"/>. The generated subcommand itself is omitted from the output.
    /// </summary>
    /// <param name="parent">
    /// The command to add the subcommand to. Typically your <see cref="RootCommand"/>.
    /// </param>
    /// <param name="name">The subcommand name. Defaults to <c>"commands"</c>.</param>
    /// <param name="rootName">
    /// Optional override for the root command's name in the emitted JSON. Useful when the
    /// root <see cref="Command.Name"/> (typically the assembly name, e.g. <c>ProcessStack.CLI</c>)
    /// doesn't match the tool's invocation name (e.g. <c>processstack</c>). Callers can still
    /// override this at runtime with the <c>--name</c> flag.
    /// </param>
    /// <returns>The created subcommand, already added to <paramref name="parent"/>.</returns>
    public static Command AddCommandsSubcommand(
        this Command parent,
        string name = "commands",
        string? rootName = null)
    {
        if (parent is null) throw new ArgumentNullException(nameof(parent));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name must be provided.", nameof(name));

        var outputOption = new Option<string?>("--output", "-o")
        {
            Description = "Output file path. If omitted, JSON is written to standard output."
        };

        var prettyOption = new Option<bool>("--pretty")
        {
            Description = "Pretty-print JSON (default: true).",
            DefaultValueFactory = _ => true
        };

        var nameOption = new Option<string?>("--name")
        {
            Description = "Override the root command's name in the emitted JSON."
        };

        var command = new Command(name, "Export the CLI command tree as commands.json (for clidoc).")
        {
            outputOption,
            prettyOption,
            nameOption
        };

        command.SetAction(parseResult =>
        {
            var output = parseResult.GetValue(outputOption);
            var pretty = parseResult.GetValue(prettyOption);
            var effectiveRootName = parseResult.GetValue(nameOption) ?? rootName;

            if (string.IsNullOrEmpty(output))
            {
                var json = CliDocExporter.RenderJson(parent, exclude: command, pretty: pretty, rootName: effectiveRootName);
                Console.WriteLine(json);
            }
            else
            {
                CliDocExporter.Export(parent, output, exclude: command, pretty: pretty, rootName: effectiveRootName);
                Console.Error.WriteLine($"Wrote {output}");
            }

            return 0;
        });

        parent.Subcommands.Add(command);
        return command;
    }
}
