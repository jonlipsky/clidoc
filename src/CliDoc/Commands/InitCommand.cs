using System.CommandLine;
using System.Text;
using Clidoc.SystemCommandLine.Schema;
using CliDoc.Input;

namespace CliDoc.Commands;

public class InitCommand
{
    public static Command Create()
    {
        var commandsJsonOption = new Option<string?>("--commands-json", ["-c"])
        {
            Description = "Path to commands.json. Defaults to ./commands.json if it exists."
        };

        var assemblyOption = new Option<string?>("--assembly", ["-a"])
        {
            Description = "(Simple apps only) Path to the compiled CLI assembly (.dll) to reflect over."
        };

        var projectOption = new Option<string?>("--project", ["-p"])
        {
            Description = "(Simple apps only) Path to a .csproj file; clidoc will build it and reflect over the output."
        };

        var outputOption = new Option<string>("--output", ["-o"])
        {
            Description = "Output file path.",
            DefaultValueFactory = _ => "cli-docs.yaml"
        };

        var entryTypeOption = new Option<string?>("--entry-type", ["-t"])
        {
            Description = "Fully-qualified type name with a static method returning RootCommand (assembly path only)."
        };

        var rootNameOption = new Option<string?>("--root-name")
        {
            Description = "Override the root command name."
        };

        var command = new Command("init", "Generate a cli-docs.yaml scaffold from a commands.json file.")
        {
            commandsJsonOption,
            assemblyOption,
            projectOption,
            outputOption,
            entryTypeOption,
            rootNameOption
        };

        command.SetAction(async (ParseResult parseResult) =>
        {
            var commandsJson = parseResult.GetValue(commandsJsonOption);
            var assemblyPath = parseResult.GetValue(assemblyOption);
            var projectPath = parseResult.GetValue(projectOption);
            var output = parseResult.GetValue(outputOption)!;
            var entryType = parseResult.GetValue(entryTypeOption);
            var rootName = parseResult.GetValue(rootNameOption);
            try
            {
                await InitializeAsync(commandsJson, assemblyPath, projectPath, output, entryType, rootName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        return command;
    }

    private static async Task InitializeAsync(
        string? commandsJsonPath,
        string? assemblyPath,
        string? projectPath,
        string outputPath,
        string? entryType,
        string? rootName)
    {
        var resolver = new InputResolver();
        var resolved = resolver.Resolve(commandsJsonPath, assemblyPath, projectPath, entryType, rootName);

        var rootCommand = resolved.Document.Commands.FirstOrDefault(c => c.IsRoot);
        var effectiveName = rootCommand?.Name ?? "cli";

        var yaml = GenerateYaml(resolved.Document.Commands, effectiveName);

        await File.WriteAllTextAsync(outputPath, yaml);

        Console.WriteLine($"✓ Generated {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Edit the YAML file to add examples and custom descriptions");
        Console.WriteLine("2. Run: clidoc generate");
    }

    private static string GenerateYaml(List<OutputCommand> commands, string rootName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("site:");
        sb.AppendLine($"  title: \"{rootName} Documentation\"");
        sb.AppendLine($"  tagline: \"Command-line documentation for {rootName}\"");
        sb.AppendLine("  # logo: assets/logo.svg");
        sb.AppendLine("  # favicon: assets/favicon.svg");
        sb.AppendLine("  # baseUrl: https://docs.example.com");
        sb.AppendLine("  # theme:");
        sb.AppendLine("  #   accentColor: \"#6366f1\"");
        sb.AppendLine();

        sb.AppendLine("commands:");

        foreach (var cmd in commands)
        {
            sb.AppendLine($"  \"{cmd.FullName}\":");
            sb.AppendLine($"    # tagline: \"{cmd.Description}\"");

            if (!cmd.IsGroup || cmd.Options.Count > 0)
            {
                sb.AppendLine("    examples:");
                sb.AppendLine("      # Add usage examples here");
                sb.AppendLine("      # - description: Basic usage");
                sb.AppendLine($"      #   command: {cmd.FullName}");
            }

            sb.AppendLine("    # sections:");
            sb.AppendLine("    #   - title: Additional Information");
            sb.AppendLine("    #     body: |");
            sb.AppendLine("    #       Markdown content goes here...");
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
