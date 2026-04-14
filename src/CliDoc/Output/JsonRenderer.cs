using System.Text.Json;
using CliDoc.Output;

namespace CliDoc.Output;

public class JsonRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Render(List<OutputCommand> commands, string version = "1.0.0")
    {
        var output = new CommandsOutput
        {
            Version = version,
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            Generator = "clidoc",
            Commands = commands
        };

        return JsonSerializer.Serialize(output, JsonOptions);
    }

    public void RenderToFile(List<OutputCommand> commands, string filePath, string version = "1.0.0")
    {
        var json = Render(commands, version);
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, json);
    }
}
