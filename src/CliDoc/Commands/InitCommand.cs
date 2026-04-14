using System.CommandLine;
using System.Text;
using CliDoc.Extraction;
using CliDoc.Loading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CliDoc.Commands;

public class InitCommand
{
    public static Command Create()
    {
        var assemblyOption = new Option<string>(
            aliases: new[] { "--assembly", "-a" },
            description: "Path to the compiled CLI assembly (.dll)")
        {
            IsRequired = true
        };

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output file path",
            getDefaultValue: () => "cli-docs.yaml");

        var entryTypeOption = new Option<string?>(
            aliases: new[] { "--entry-type", "-t" },
            description: "Fully-qualified type name with a static method returning RootCommand");

        var rootNameOption = new Option<string?>(
            aliases: new[] { "--root-name" },
            description: "Override the root command name");

        var command = new Command("init", "Generate a cli-docs.yaml scaffold from an assembly")
        {
            assemblyOption,
            outputOption,
            entryTypeOption,
            rootNameOption
        };

        command.SetHandler(async (string assemblyPath, string output, string? entryType, string? rootName) =>
        {
            try
            {
                await InitializeAsync(assemblyPath, output, entryType, rootName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, assemblyOption, outputOption, entryTypeOption, rootNameOption);

        return command;
    }

    private static async Task InitializeAsync(string assemblyPath, string outputPath, string? entryType, string? rootName)
    {
        Console.WriteLine($"Loading assembly: {assemblyPath}");
        
        var loader = new AssemblyCommandLoader();
        var rootCommand = loader.LoadCommand(assemblyPath, entryType);

        if (!string.IsNullOrEmpty(rootName))
        {
            rootCommand.Name = rootName;
        }

        Console.WriteLine($"Discovered root command: {rootCommand.Name}");

        var extractor = new CommandExtractor();
        var extracted = extractor.Extract(rootCommand);

        Console.WriteLine($"Extracted {extracted.Count} command(s)");

        // Generate YAML
        var yaml = GenerateYaml(extracted, rootCommand.Name);

        await File.WriteAllTextAsync(outputPath, yaml);

        Console.WriteLine($"✓ Generated {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Edit the YAML file to add examples and custom descriptions");
        Console.WriteLine($"2. Run: clidoc generate --assembly {assemblyPath}");
    }

    private static string GenerateYaml(List<ExtractedCommand> commands, string rootName)
    {
        var sb = new StringBuilder();

        // Site configuration
        sb.AppendLine("site:");
        sb.AppendLine($"  title: \"{rootName} Documentation\"");
        sb.AppendLine($"  tagline: \"Command-line documentation for {rootName}\"");
        sb.AppendLine("  # logo: assets/logo.svg");
        sb.AppendLine("  # favicon: assets/favicon.svg");
        sb.AppendLine("  # baseUrl: https://docs.example.com");
        sb.AppendLine("  # theme:");
        sb.AppendLine("  #   accentColor: \"#6366f1\"");
        sb.AppendLine();

        // Commands
        sb.AppendLine("commands:");

        foreach (var cmd in commands)
        {
            sb.AppendLine($"  \"{cmd.FullName}\":");
            sb.AppendLine($"    # tagline: \"{cmd.Description}\"");
            
            if (!cmd.IsGroup || cmd.Options.Count > 0)
            {
                sb.AppendLine("    examples:");
                sb.AppendLine("      # Add usage examples here");
                sb.AppendLine($"      # - description: Basic usage");
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
