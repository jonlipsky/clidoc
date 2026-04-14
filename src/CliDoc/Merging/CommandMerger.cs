using CliDoc.Extraction;
using CliDoc.Metadata;
using CliDoc.Output;

namespace CliDoc.Merging;

public class CommandMerger
{
    public List<OutputCommand> Merge(List<ExtractedCommand> extracted, MetadataFile? metadata)
    {
        var output = new List<OutputCommand>();

        foreach (var command in extracted)
        {
            var commandMetadata = metadata?.Commands?.GetValueOrDefault(command.FullName);
            
            var outputCommand = new OutputCommand
            {
                Id = command.Id,
                Name = command.Name,
                FullName = command.FullName,
                Description = commandMetadata?.Tagline ?? command.Description,
                IsGroup = command.IsGroup,
                IsRoot = command.IsRoot,
                Depth = command.Depth,
                ParentId = command.ParentId,
                Arguments = command.Arguments.Select(a => new OutputArgument
                {
                    Name = a.Name,
                    Description = a.Description,
                    IsRequired = a.IsRequired,
                    IsVariadic = a.IsVariadic
                }).ToList(),
                Options = command.Options.Select(o => new OutputOption
                {
                    Name = o.Name,
                    Description = o.Description,
                    ShortName = o.ShortName,
                    ValueType = o.ValueType,
                    IsRequired = o.IsRequired,
                    DefaultValue = o.DefaultValue,
                    AllowedValues = o.AllowedValues
                }).ToList(),
                Examples = commandMetadata?.Examples?.Select(e => new OutputExample
                {
                    Description = e.Description,
                    Command = e.Command
                }).ToList() ?? new List<OutputExample>(),
                Sections = commandMetadata?.Sections?.Select(s => new OutputSection
                {
                    Title = s.Title,
                    Body = s.Body
                }).ToList() ?? new List<OutputSection>(),
                Children = command.Children
            };

            output.Add(outputCommand);
        }

        return output;
    }
}
