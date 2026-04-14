using YamlDotNet.Serialization;

namespace CliDoc.Metadata;

public class MetadataFile
{
    [YamlMember(Alias = "site")]
    public SiteConfig? Site { get; set; }

    [YamlMember(Alias = "commands")]
    public Dictionary<string, CommandMetadata>? Commands { get; set; }
}

public class SiteConfig
{
    [YamlMember(Alias = "title")]
    public string? Title { get; set; }

    [YamlMember(Alias = "tagline")]
    public string? Tagline { get; set; }

    [YamlMember(Alias = "logo")]
    public string? Logo { get; set; }

    [YamlMember(Alias = "favicon")]
    public string? Favicon { get; set; }

    [YamlMember(Alias = "baseUrl")]
    public string? BaseUrl { get; set; }

    [YamlMember(Alias = "theme")]
    public ThemeConfig? Theme { get; set; }
}

public class ThemeConfig
{
    [YamlMember(Alias = "accentColor")]
    public string? AccentColor { get; set; }
}

public class CommandMetadata
{
    [YamlMember(Alias = "tagline")]
    public string? Tagline { get; set; }

    [YamlMember(Alias = "examples")]
    public List<Example>? Examples { get; set; }

    [YamlMember(Alias = "sections")]
    public List<Section>? Sections { get; set; }
}

public class Example
{
    [YamlMember(Alias = "description")]
    public required string Description { get; set; }

    [YamlMember(Alias = "command")]
    public required string Command { get; set; }
}

public class Section
{
    [YamlMember(Alias = "title")]
    public required string Title { get; set; }

    [YamlMember(Alias = "body")]
    public required string Body { get; set; }
}
