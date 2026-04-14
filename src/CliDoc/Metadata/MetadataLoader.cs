using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CliDoc.Metadata;

public class MetadataLoader
{
    private readonly IDeserializer _deserializer;

    public MetadataLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public MetadataFile? Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var yaml = File.ReadAllText(filePath);
            return LoadFromString(yaml);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load metadata from {filePath}", ex);
        }
    }

    public MetadataFile LoadFromString(string yaml)
    {
        try
        {
            var metadata = _deserializer.Deserialize<MetadataFile>(yaml);
            return metadata ?? new MetadataFile();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to parse YAML metadata", ex);
        }
    }
}
