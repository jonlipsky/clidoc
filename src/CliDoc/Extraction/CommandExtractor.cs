using System.CommandLine;
using System.CommandLine.Parsing;

namespace CliDoc.Extraction;

public class CommandExtractor
{
    public List<ExtractedCommand> Extract(Command rootCommand)
    {
        var commands = new List<ExtractedCommand>();
        var rootId = SanitizeId(rootCommand.Name);
        
        ExtractRecursive(rootCommand, null, 0, rootId, commands);
        
        return commands;
    }

    private void ExtractRecursive(Command command, string? parentId, int depth, string commandId, List<ExtractedCommand> commands)
    {
        var children = new List<string>();
        
        // Extract child command IDs first
        foreach (var subcommand in command.Subcommands)
        {
            var childId = parentId == null 
                ? $"{commandId}-{SanitizeId(subcommand.Name)}"
                : $"{commandId}-{SanitizeId(subcommand.Name)}";
            children.Add(childId);
        }

        var extracted = new ExtractedCommand
        {
            Id = commandId,
            Name = command.Name,
            FullName = BuildFullName(command, parentId, commands),
            Description = command.Description ?? string.Empty,
            IsGroup = command.Subcommands.Count > 0,
            IsRoot = parentId == null,
            Depth = depth,
            ParentId = parentId,
            Options = ExtractOptions(command),
            Arguments = ExtractArguments(command),
            Children = children
        };

        commands.Add(extracted);

        // Recursively extract subcommands
        for (int i = 0; i < command.Subcommands.Count; i++)
        {
            var subcommand = command.Subcommands[i];
            var childId = children[i];
            ExtractRecursive(subcommand, commandId, depth + 1, childId, commands);
        }
    }

    private string BuildFullName(Command command, string? parentId, List<ExtractedCommand> commands)
    {
        if (parentId == null)
        {
            return command.Name;
        }

        var parent = commands.FirstOrDefault(c => c.Id == parentId);
        if (parent != null)
        {
            return $"{parent.FullName} {command.Name}";
        }

        return command.Name;
    }

    private List<ExtractedOption> ExtractOptions(Command command)
    {
        var options = new List<ExtractedOption>();

        foreach (var option in command.Options)
        {
            // In 2.0.5, Name is the primary name; Aliases contains additional aliases
            var longName = option.Name;
            var shortName = option.Aliases.FirstOrDefault(a => a.StartsWith("-") && !a.StartsWith("--"));

            // If the primary name is a short form, look for a long form in aliases
            if (!longName.StartsWith("--") && longName.StartsWith("-"))
            {
                shortName = longName;
                longName = option.Aliases.FirstOrDefault(a => a.StartsWith("--")) ?? longName;
            }

            var valueType = GetValueType(option.ValueType);
            var defaultValue = GetDefaultValue(option);

            options.Add(new ExtractedOption
            {
                Name = longName,
                Description = option.Description ?? string.Empty,
                ShortName = shortName,
                ValueType = valueType,
                IsRequired = option.Required,
                DefaultValue = defaultValue,
                AllowedValues = null // TODO: Extract from completion sources
            });
        }

        return options;
    }

    private List<ExtractedArgument> ExtractArguments(Command command)
    {
        var arguments = new List<ExtractedArgument>();

        foreach (var argument in command.Arguments)
        {
            arguments.Add(new ExtractedArgument
            {
                Name = argument.Name,
                Description = argument.Description ?? string.Empty,
                IsRequired = !argument.HasDefaultValue,
                IsVariadic = argument.Arity.MaximumNumberOfValues > 1
            });
        }

        return arguments;
    }

    private string GetValueType(Type type)
    {
        if (type == typeof(bool))
            return "boolean";
        if (type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(decimal))
            return "number";
        if (type == typeof(FileInfo) || type == typeof(DirectoryInfo))
            return "path";
        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            return "array";
        
        return "string";
    }

    private string? GetDefaultValue(Option option)
    {
        // System.CommandLine doesn't expose default values easily
        // We'll need to inspect via reflection or leave null for now
        return null;
    }

    private string SanitizeId(string name)
    {
        return name.ToLowerInvariant().Replace(" ", "-");
    }
}
