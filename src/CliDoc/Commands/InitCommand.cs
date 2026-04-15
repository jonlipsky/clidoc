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
        var assemblyOption = new Option<string?>("--assembly", ["-a"])
        {
            Description = "Path to the compiled CLI assembly (.dll)"
        };

        var projectOption = new Option<string?>("--project", ["-p"])
        {
            Description = "Path to the .csproj file (builds the project and resolves the assembly)"
        };

        var outputOption = new Option<string>("--output", ["-o"])
        {
            Description = "Output file path",
            DefaultValueFactory = _ => "cli-docs.yaml"
        };

        var entryTypeOption = new Option<string?>("--entry-type", ["-t"])
        {
            Description = "Fully-qualified type name with a static method returning RootCommand"
        };

        var rootNameOption = new Option<string?>("--root-name")
        {
            Description = "Override the root command name"
        };

        var command = new Command("init", "Generate a cli-docs.yaml scaffold from an assembly")
        {
            assemblyOption,
            projectOption,
            outputOption,
            entryTypeOption,
            rootNameOption
        };

        command.SetAction(async (ParseResult parseResult) =>
        {
            var assemblyPath = parseResult.GetValue(assemblyOption);
            var projectPath = parseResult.GetValue(projectOption);
            var output = parseResult.GetValue(outputOption)!;
            var entryType = parseResult.GetValue(entryTypeOption);
            var rootName = parseResult.GetValue(rootNameOption);
            try
            {
                await InitializeAsync(assemblyPath, projectPath, output, entryType, rootName);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        return command;
    }

    private static async Task InitializeAsync(string? assemblyPath, string? projectPath, string outputPath, string? entryType, string? rootName)
    {
        var resolvedAssemblyPath = ResolveAssemblyPath(assemblyPath, projectPath);

        Console.WriteLine($"Loading assembly: {resolvedAssemblyPath}");
        
        var loader = new AssemblyCommandLoader();
        var rootCommand = loader.LoadCommand(resolvedAssemblyPath, entryType);

        Console.WriteLine($"Discovered root command: {rootCommand.Name}");

        var extractor = new CommandExtractor();
        var extracted = extractor.Extract(rootCommand);

        // Apply root name override if specified
        var effectiveName = rootCommand.Name;
        if (!string.IsNullOrEmpty(rootName))
        {
            effectiveName = rootName;
            var oldRootName = rootCommand.Name;
            for (int i = 0; i < extracted.Count; i++)
            {
                var cmd = extracted[i];
                if (cmd.IsRoot)
                {
                    extracted[i] = cmd with { Name = rootName, FullName = rootName };
                }
                else if (cmd.FullName.StartsWith(oldRootName + " "))
                {
                    extracted[i] = cmd with { FullName = rootName + cmd.FullName.Substring(oldRootName.Length) };
                }
            }
        }

        Console.WriteLine($"Extracted {extracted.Count} command(s)");

        // Generate YAML
        var yaml = GenerateYaml(extracted, effectiveName);

        await File.WriteAllTextAsync(outputPath, yaml);

        Console.WriteLine($"✓ Generated {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("1. Edit the YAML file to add examples and custom descriptions");
        Console.WriteLine($"2. Run: clidoc generate --assembly {resolvedAssemblyPath}");
    }

    internal static string ResolveAssemblyPath(string? assemblyPath, string? projectPath)
    {
        if (!string.IsNullOrEmpty(projectPath))
        {
            var resolver = new ProjectResolver();
            return resolver.BuildAndResolve(projectPath);
        }

        if (!string.IsNullOrEmpty(assemblyPath))
        {
            return assemblyPath;
        }

        throw new InvalidOperationException("Either --assembly or --project must be specified.");
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
