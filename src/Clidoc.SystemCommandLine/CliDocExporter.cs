using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Clidoc.SystemCommandLine.Extraction;
using Clidoc.SystemCommandLine.Schema;

namespace Clidoc.SystemCommandLine;

public static class CliDocExporter
{
    public const string SchemaVersion = "1.0";
    public const string Generator = "Clidoc.SystemCommandLine";

    /// <summary>
    /// Assembly-metadata key that our shipped MSBuild targets file writes with the
    /// consumer's <c>$(ToolCommandName)</c>. Read at runtime to auto-detect the tool's
    /// invocation name without requiring explicit configuration in code.
    /// </summary>
    internal const string AssemblyMetadataKey = "ClidocToolName";

    private static readonly JsonSerializerOptions PrettyOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CompactOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Walks the <paramref name="rootCommand"/> tree and returns a clidoc-compatible
    /// commands.json document as a string.
    /// </summary>
    /// <param name="rootCommand">The root of the command tree to export.</param>
    /// <param name="exclude">A subcommand to omit from the output (e.g. the exporter command itself).</param>
    /// <param name="pretty">If true, produce indented JSON. Defaults to true.</param>
    /// <param name="rootName">
    /// If non-null, the root command is renamed to this value (and descendants' <c>fullName</c>
    /// is rewritten to match). Useful when the root's <see cref="Command.Name"/> is the assembly
    /// name rather than the tool's invocation name.
    /// </param>
    public static string RenderJson(
        Command rootCommand,
        Command? exclude = null,
        bool pretty = true,
        string? rootName = null)
    {
        var extractor = new CommandExtractor();
        var commands = extractor.Extract(rootCommand, exclude);

        var effectiveRootName = rootName ?? DetectToolName();
        if (!string.IsNullOrEmpty(effectiveRootName))
        {
            commands = ApplyRootRename(commands, effectiveRootName);
        }

        var output = new CommandsOutput
        {
            SchemaVersion = SchemaVersion,
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            Generator = Generator,
            Commands = commands
        };

        return RenderJson(output, pretty);
    }

    /// <summary>
    /// Best-effort detection of the tool's invocation name, so users don't have to pass
    /// <c>rootName</c> or <c>--name</c> when the consuming project already sets
    /// <c>&lt;ToolCommandName&gt;</c>.
    /// Order of attempts:
    /// 1. <see cref="AssemblyMetadataAttribute"/> with key <c>"ClidocToolName"</c> on the
    ///    entry assembly. Our shipped <c>build/Clidoc.SystemCommandLine.targets</c> adds
    ///    this from MSBuild's <c>$(ToolCommandName)</c>.
    /// 2. The file name of <see cref="Environment.ProcessPath"/>, unless it's the dotnet
    ///    host (indicating <c>dotnet run</c> or similar).
    /// Returns <c>null</c> if nothing useful is found, in which case the caller leaves
    /// the root's existing <see cref="Command.Name"/> unchanged.
    /// </summary>
    internal static string? DetectToolName()
    {
        try
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry is not null)
            {
                var metadata = entry.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(a => string.Equals(a.Key, AssemblyMetadataKey, StringComparison.Ordinal));
                if (!string.IsNullOrWhiteSpace(metadata?.Value))
                {
                    return metadata.Value;
                }
            }

            var path = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrWhiteSpace(name) &&
                    !string.Equals(name, "dotnet", StringComparison.OrdinalIgnoreCase))
                {
                    return name;
                }
            }
        }
        catch
        {
            // Detection is best-effort; fall through to null.
        }
        return null;
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

    /// <summary>
    /// Serializes an already-populated <see cref="CommandsOutput"/> to JSON.
    /// Useful when the caller has assembled the document itself (for example, after
    /// merging editor-supplied metadata into the extracted commands).
    /// </summary>
    public static string RenderJson(CommandsOutput document, bool pretty = true)
    {
        if (document is null) throw new ArgumentNullException(nameof(document));
        return JsonSerializer.Serialize(document, pretty ? PrettyOptions : CompactOptions);
    }

    /// <summary>
    /// Walks the <paramref name="rootCommand"/> tree and writes the clidoc-compatible
    /// commands.json document to <paramref name="outputPath"/>.
    /// </summary>
    public static void Export(
        Command rootCommand,
        string outputPath,
        Command? exclude = null,
        bool pretty = true,
        string? rootName = null)
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentException("Output path must be provided.", nameof(outputPath));
        }

        var json = RenderJson(rootCommand, exclude, pretty, rootName);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(outputPath, json);
    }
}
