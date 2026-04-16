using System.Text.Json;
using Clidoc.SystemCommandLine.Schema;

namespace CliDoc.Input;

public class CommandsJsonLoader
{
    public const string SupportedMajor = "1";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CommandsOutput Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"commands.json not found: {path}");
        }

        var json = File.ReadAllText(path);
        return LoadFromString(json, source: path);
    }

    public static CommandsOutput LoadFromString(string json, string source = "<input>")
    {
        CommandsOutput? document;
        try
        {
            document = JsonSerializer.Deserialize<CommandsOutput>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Failed to parse {source}: {ex.Message}", ex);
        }

        if (document is null)
        {
            throw new InvalidDataException($"{source} is empty or not a valid commands.json document.");
        }

        if (string.IsNullOrWhiteSpace(document.SchemaVersion))
        {
            throw new InvalidDataException(
                $"{source} is missing the required \"schemaVersion\" field. " +
                $"Regenerate it with a current version of Clidoc.SystemCommandLine.");
        }

        var major = document.SchemaVersion.Split('.')[0];
        if (!string.Equals(major, SupportedMajor, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"{source} has schemaVersion \"{document.SchemaVersion}\", but this version of clidoc " +
                $"only supports schemaVersion {SupportedMajor}.x. " +
                $"Update clidoc or regenerate the file with a compatible emitter.");
        }

        return document;
    }
}
