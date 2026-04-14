namespace CliDoc.Extraction;

public record ExtractedCommand
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required string Description { get; init; }
    public required bool IsGroup { get; init; }
    public required bool IsRoot { get; init; }
    public required int Depth { get; init; }
    public string? ParentId { get; init; }
    public required List<ExtractedOption> Options { get; init; }
    public required List<ExtractedArgument> Arguments { get; init; }
    public required List<string> Children { get; init; }
}

public record ExtractedOption
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? ShortName { get; init; }
    public required string ValueType { get; init; }
    public required bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public List<string>? AllowedValues { get; init; }
}

public record ExtractedArgument
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required bool IsRequired { get; init; }
    public required bool IsVariadic { get; init; }
}
