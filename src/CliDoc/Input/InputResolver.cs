using Clidoc.SystemCommandLine;
using Clidoc.SystemCommandLine.Extraction;
using Clidoc.SystemCommandLine.Schema;
using CliDoc.Loading;

namespace CliDoc.Input;

/// <summary>
/// Resolves the <see cref="CommandsOutput"/> document that downstream rendering consumes,
/// regardless of whether it came from a pre-built <c>commands.json</c> file (preferred) or
/// was derived via assembly reflection (the legacy "easy button" path for simple apps).
/// </summary>
public class InputResolver
{
    public record Result(CommandsOutput Document, string SourceDescription);

    public Result Resolve(
        string? commandsJsonArg,
        string? assemblyPath,
        string? projectPath,
        string? entryType,
        string? rootNameOverride = null)
    {
        var hasAssembly = !string.IsNullOrEmpty(assemblyPath) || !string.IsNullOrEmpty(projectPath);
        var hasJson = !string.IsNullOrEmpty(commandsJsonArg);

        if (hasJson && hasAssembly)
        {
            throw new InvalidOperationException(
                "Specify either a commands.json path or --assembly/--project, not both.");
        }

        if (hasAssembly)
        {
            return ResolveFromAssembly(assemblyPath, projectPath, entryType, rootNameOverride);
        }

        if (hasJson)
        {
            return ResolveFromJson(commandsJsonArg!, rootNameOverride);
        }

        // Implicit default: look for ./commands.json in the current directory.
        const string defaultPath = "commands.json";
        if (File.Exists(defaultPath))
        {
            return ResolveFromJson(defaultPath, rootNameOverride);
        }

        throw new InvalidOperationException(
            "No input specified. Pass a commands.json path as the first argument, " +
            "or use --assembly/--project for simple non-DI apps, " +
            "or place a commands.json file in the current directory.");
    }

    private Result ResolveFromJson(string path, string? rootNameOverride)
    {
        Console.WriteLine($"Loading commands.json: {path}");
        var loader = new CommandsJsonLoader();
        var document = loader.Load(path);

        if (!string.IsNullOrEmpty(rootNameOverride))
        {
            document = ApplyRootRename(document, rootNameOverride);
        }

        Console.WriteLine($"Loaded {document.Commands.Count} command(s) from {path}");
        return new Result(document, $"commands.json ({path})");
    }

    private Result ResolveFromAssembly(
        string? assemblyPath,
        string? projectPath,
        string? entryType,
        string? rootNameOverride)
    {
        var (resolvedAssemblyPath, toolName) = ResolveAssemblyPath(assemblyPath, projectPath);

        Console.WriteLine($"Loading assembly: {resolvedAssemblyPath}");
        var loader = new AssemblyCommandLoader();
        var rootCommand = loader.LoadCommand(resolvedAssemblyPath, entryType);

        Console.WriteLine($"Discovered root command: {rootCommand.Name}");

        var extractor = new CommandExtractor();
        var commands = extractor.Extract(rootCommand);

        var document = new CommandsOutput
        {
            SchemaVersion = CliDocExporter.SchemaVersion,
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            Generator = $"clidoc (assembly: {Path.GetFileName(resolvedAssemblyPath)})",
            Commands = commands
        };

        var effectiveName = rootNameOverride ?? toolName;
        if (!string.IsNullOrEmpty(effectiveName))
        {
            document = ApplyRootRename(document, effectiveName);
        }

        Console.WriteLine($"Extracted {document.Commands.Count} command(s)");
        return new Result(document, $"assembly ({resolvedAssemblyPath})");
    }

    internal static (string assemblyPath, string? toolName) ResolveAssemblyPath(
        string? assemblyPath,
        string? projectPath)
    {
        if (!string.IsNullOrEmpty(projectPath))
        {
            var resolver = new ProjectResolver();
            var result = resolver.BuildAndResolve(projectPath);
            return (result.AssemblyPath, result.ToolCommandName);
        }

        if (!string.IsNullOrEmpty(assemblyPath))
        {
            return (assemblyPath, null);
        }

        throw new InvalidOperationException("Either --assembly or --project must be specified.");
    }

    private static CommandsOutput ApplyRootRename(CommandsOutput document, string newRootName)
    {
        var root = document.Commands.FirstOrDefault(c => c.IsRoot);
        if (root is null || string.Equals(root.Name, newRootName, StringComparison.Ordinal))
        {
            return document;
        }

        var oldRootName = root.Name;
        var renamed = document.Commands.Select(cmd =>
        {
            if (cmd.IsRoot)
            {
                return cmd with { Name = newRootName, FullName = newRootName };
            }

            if (cmd.FullName.StartsWith(oldRootName + " ", StringComparison.Ordinal))
            {
                return cmd with { FullName = newRootName + cmd.FullName.Substring(oldRootName.Length) };
            }

            return cmd;
        }).ToList();

        return new CommandsOutput
        {
            SchemaVersion = document.SchemaVersion,
            GeneratedAt = document.GeneratedAt,
            Generator = document.Generator,
            Commands = renamed
        };
    }
}
