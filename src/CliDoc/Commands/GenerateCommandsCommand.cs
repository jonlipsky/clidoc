using System.CommandLine;
using Clidoc.SystemCommandLine;
using Clidoc.SystemCommandLine.Extraction;
using Clidoc.SystemCommandLine.Schema;
using CliDoc.Loading;

namespace CliDoc.Commands;

/// <summary>
/// <c>clidoc generate commands</c> — extract a commands.json from a System.CommandLine
/// assembly or project. Primarily for apps that don't reference Clidoc.SystemCommandLine
/// and therefore can't emit their own commands.json at runtime.
/// </summary>
public static class GenerateCommandsCommand
{
    public static Command Create()
    {
        var assemblyOption = new Option<string?>("--assembly", ["-a"])
        {
            Description = "Path to the compiled CLI assembly (.dll) to reflect over."
        };

        var projectOption = new Option<string?>("--project", ["-p"])
        {
            Description = "Path to a .csproj file; clidoc will build it and reflect over the output."
        };

        var entryTypeOption = new Option<string?>("--entry-type", ["-t"])
        {
            Description = "Fully-qualified type name with a static method returning RootCommand."
        };

        var rootNameOption = new Option<string?>("--root-name")
        {
            Description = "Override the root command's name in the emitted JSON. " +
                          "Defaults to the csproj's <ToolCommandName> (via --project)."
        };

        var outputOption = new Option<string>("--output", ["-o"])
        {
            Description = "Output path for the generated commands.json.",
            DefaultValueFactory = _ => "commands.json"
        };

        var prettyOption = new Option<bool>("--pretty")
        {
            Description = "Pretty-print the JSON (default: true).",
            DefaultValueFactory = _ => true
        };

        var command = new Command("commands", "Extract commands.json from a System.CommandLine assembly or project.")
        {
            assemblyOption,
            projectOption,
            entryTypeOption,
            rootNameOption,
            outputOption,
            prettyOption
        };

        command.SetAction(parseResult =>
        {
            try
            {
                Generate(
                    parseResult.GetValue(assemblyOption),
                    parseResult.GetValue(projectOption),
                    parseResult.GetValue(entryTypeOption),
                    parseResult.GetValue(rootNameOption),
                    parseResult.GetValue(outputOption)!,
                    parseResult.GetValue(prettyOption));
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

    private static void Generate(
        string? assemblyPath,
        string? projectPath,
        string? entryType,
        string? rootNameOverride,
        string outputPath,
        bool pretty)
    {
        if (string.IsNullOrEmpty(assemblyPath) && string.IsNullOrEmpty(projectPath))
        {
            throw new InvalidOperationException(
                "Specify either --assembly or --project to locate the CLI to extract.");
        }

        // Resolve the target assembly (build the project first if necessary).
        string resolvedAssemblyPath;
        string? toolCommandName = null;
        if (!string.IsNullOrEmpty(projectPath))
        {
            var resolver = new ProjectResolver();
            var result = resolver.BuildAndResolve(projectPath);
            resolvedAssemblyPath = result.AssemblyPath;
            toolCommandName = result.ToolCommandName;
        }
        else
        {
            resolvedAssemblyPath = assemblyPath!;
        }

        Console.WriteLine($"Loading assembly: {resolvedAssemblyPath}");
        var loader = new AssemblyCommandLoader();
        var rootCommand = loader.LoadCommand(resolvedAssemblyPath, entryType);
        Console.WriteLine($"Discovered root command: {rootCommand.Name}");

        var extractor = new CommandExtractor();
        var commands = extractor.Extract(rootCommand);

        var effectiveRootName = rootNameOverride ?? toolCommandName;
        if (!string.IsNullOrEmpty(effectiveRootName))
        {
            commands = ApplyRootRename(commands, effectiveRootName);
        }

        var document = new CommandsOutput
        {
            SchemaVersion = CliDocExporter.SchemaVersion,
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            Generator = $"clidoc generate commands (assembly: {Path.GetFileName(resolvedAssemblyPath)})",
            Commands = commands
        };

        Console.WriteLine($"Extracted {commands.Count} command(s)");

        var json = CliDocExporter.RenderJson(document, pretty);
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"✓ Wrote {outputPath}");
    }

    private static List<OutputCommand> ApplyRootRename(List<OutputCommand> commands, string newRootName)
    {
        var root = commands.FirstOrDefault(c => c.IsRoot);
        if (root is null || string.Equals(root.Name, newRootName, StringComparison.Ordinal))
        {
            return commands;
        }

        var oldName = root.Name;
        return commands.Select(cmd =>
        {
            if (cmd.IsRoot)
            {
                return cmd with { Name = newRootName, FullName = newRootName };
            }
            if (cmd.FullName.StartsWith(oldName + " ", StringComparison.Ordinal))
            {
                return cmd with { FullName = newRootName + cmd.FullName.Substring(oldName.Length) };
            }
            return cmd;
        }).ToList();
    }
}
