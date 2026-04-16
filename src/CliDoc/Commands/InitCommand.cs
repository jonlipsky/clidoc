using System.CommandLine;
using System.Text;
using Clidoc.SystemCommandLine.Schema;

namespace CliDoc.Commands;

/// <summary>
/// <c>clidoc init</c> — scaffold a <c>cli-docs.yaml</c> from a commands.json file.
/// </summary>
public static class InitCommand
{
    public static Command Create()
    {
        var commandsJsonOption = new Option<string?>("--commands-json", "-c")
        {
            Description = "Path to commands.json. Defaults to ./commands.json if it exists."
        };

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output file path.",
            DefaultValueFactory = _ => "cli-docs.yaml"
        };

        var rootNameOption = new Option<string?>("--root-name")
        {
            Description = "Override the root command's name used as the YAML map key."
        };

        var command = new Command("init", "Generate a cli-docs.yaml scaffold from a commands.json file.")
        {
            commandsJsonOption,
            outputOption,
            rootNameOption
        };

        command.SetAction(async parseResult =>
        {
            try
            {
                await InitializeAsync(
                    parseResult.GetValue(commandsJsonOption),
                    parseResult.GetValue(outputOption)!,
                    parseResult.GetValue(rootNameOption));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private static async Task InitializeAsync(string? commandsJsonPath, string outputPath, string? rootName)
    {
        var document = GenerateDocsCommand.LoadCommandsJson(commandsJsonPath, rootName);

        var rootCommand = document.Commands.FirstOrDefault(c => c.IsRoot);
        var effectiveName = rootCommand?.Name ?? "cli";

        var yaml = GenerateYaml(document.Commands, effectiveName);

        await File.WriteAllTextAsync(outputPath, yaml);

        Console.WriteLine($"✓ Generated {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Edit the YAML file to add examples and custom descriptions");
        Console.WriteLine("2. Run: clidoc generate docs");
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
