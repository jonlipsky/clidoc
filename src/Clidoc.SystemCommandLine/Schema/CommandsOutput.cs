using System.Text.Json.Serialization;

namespace Clidoc.SystemCommandLine.Schema;

public record CommandsOutput
{
    [JsonPropertyName("schemaVersion")]
    public required string SchemaVersion { get; init; }

    [JsonPropertyName("generatedAt")]
    public required string GeneratedAt { get; init; }

    [JsonPropertyName("generator")]
    public required string Generator { get; init; }

    [JsonPropertyName("commands")]
    public required List<OutputCommand> Commands { get; init; }
}

public record OutputCommand
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("isGroup")]
    public required bool IsGroup { get; init; }

    [JsonPropertyName("isRoot")]
    public required bool IsRoot { get; init; }

    [JsonPropertyName("depth")]
    public required int Depth { get; init; }

    [JsonPropertyName("parentId")]
    public string? ParentId { get; init; }

    [JsonPropertyName("arguments")]
    public required List<OutputArgument> Arguments { get; init; }

    [JsonPropertyName("options")]
    public required List<OutputOption> Options { get; init; }

    [JsonPropertyName("examples")]
    public List<OutputExample>? Examples { get; init; }

    [JsonPropertyName("sections")]
    public List<OutputSection>? Sections { get; init; }

    [JsonPropertyName("children")]
    public required List<string> Children { get; init; }
}

public record OutputOption
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("shortName")]
    public string? ShortName { get; init; }

    [JsonPropertyName("valueType")]
    public required string ValueType { get; init; }

    [JsonPropertyName("isRequired")]
    public required bool IsRequired { get; init; }

    [JsonPropertyName("defaultValue")]
    public string? DefaultValue { get; init; }

    [JsonPropertyName("allowedValues")]
    public List<string>? AllowedValues { get; init; }
}

public record OutputArgument
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("isRequired")]
    public required bool IsRequired { get; init; }

    [JsonPropertyName("isVariadic")]
    public required bool IsVariadic { get; init; }
}

public record OutputExample
{
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("command")]
    public required string Command { get; init; }
}

public record OutputSection
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("body")]
    public required string Body { get; init; }
}
