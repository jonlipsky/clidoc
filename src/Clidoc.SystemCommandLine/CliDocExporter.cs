using System.CommandLine;
using System.Text.Json;
using Clidoc.SystemCommandLine.Extraction;
using Clidoc.SystemCommandLine.Schema;

namespace Clidoc.SystemCommandLine;

public static class CliDocExporter
{
    public const string SchemaVersion = "1.0";
    public const string Generator = "Clidoc.SystemCommandLine";

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
    public static string RenderJson(Command rootCommand, Command? exclude = null, bool pretty = true)
    {
        var extractor = new CommandExtractor();
        var commands = extractor.Extract(rootCommand, exclude);

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
    public static void Export(Command rootCommand, string outputPath, Command? exclude = null, bool pretty = true)
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            throw new ArgumentException("Output path must be provided.", nameof(outputPath));
        }

        var json = RenderJson(rootCommand, exclude, pretty);

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(outputPath, json);
    }
}
