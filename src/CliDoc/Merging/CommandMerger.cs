using Clidoc.SystemCommandLine.Schema;
using CliDoc.Metadata;

namespace CliDoc.Merging;

public class CommandMerger
{
    public List<OutputCommand> Merge(List<OutputCommand> commands, MetadataFile? metadata)
    {
        var output = new List<OutputCommand>(commands.Count);

        foreach (var command in commands)
        {
            var commandMetadata = metadata?.Commands?.GetValueOrDefault(command.FullName);

            var examples = commandMetadata?.Examples?.Select(e => new OutputExample
            {
                Description = e.Description,
                Command = e.Command
            }).ToList() ?? command.Examples ?? new List<OutputExample>();

            var sections = commandMetadata?.Sections?.Select(s => new OutputSection
            {
                Title = s.Title,
                Body = s.Body
            }).ToList() ?? command.Sections ?? new List<OutputSection>();

            output.Add(new OutputCommand
            {
                Id = command.Id,
                Name = command.Name,
                FullName = command.FullName,
                Description = commandMetadata?.Tagline ?? command.Description,
                IsGroup = command.IsGroup,
                IsRoot = command.IsRoot,
                Depth = command.Depth,
                ParentId = command.ParentId,
                Arguments = command.Arguments,
                Options = command.Options,
                Examples = examples,
                Sections = sections,
                Children = command.Children
            });
        }

        return output;
    }
}
