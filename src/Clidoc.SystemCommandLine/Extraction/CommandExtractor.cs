using System.CommandLine;
using Clidoc.SystemCommandLine.Schema;

namespace Clidoc.SystemCommandLine.Extraction;

public class CommandExtractor
{
    public List<OutputCommand> Extract(Command rootCommand, Command? exclude = null)
    {
        var commands = new List<OutputCommand>();
        var rootId = SanitizeId(rootCommand.Name);

        ExtractRecursive(rootCommand, null, 0, rootId, commands, exclude);

        return commands;
    }

    private void ExtractRecursive(
        Command command,
        string? parentId,
        int depth,
        string commandId,
        List<OutputCommand> commands,
        Command? exclude)
    {
        var children = new List<string>();
        var childEntries = new List<(Command sub, string id)>();

        foreach (var subcommand in command.Subcommands)
        {
            if (ReferenceEquals(subcommand, exclude)) continue;

            var childId = $"{commandId}-{SanitizeId(subcommand.Name)}";
            children.Add(childId);
            childEntries.Add((subcommand, childId));
        }

        var extracted = new OutputCommand
        {
            Id = commandId,
            Name = command.Name,
            FullName = BuildFullName(command, parentId, commands),
            Description = command.Description ?? string.Empty,
            IsGroup = childEntries.Count > 0,
            IsRoot = parentId == null,
            Depth = depth,
            ParentId = parentId,
            Options = ExtractOptions(command),
            Arguments = ExtractArguments(command),
            Children = children
        };

        commands.Add(extracted);

        foreach (var (subcommand, childId) in childEntries)
        {
            ExtractRecursive(subcommand, commandId, depth + 1, childId, commands, exclude);
        }
    }

    private static string BuildFullName(Command command, string? parentId, List<OutputCommand> commands)
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

    private List<OutputOption> ExtractOptions(Command command)
    {
        var options = new List<OutputOption>();

        foreach (var option in command.Options)
        {
            var longName = option.Name;
            var shortName = option.Aliases.FirstOrDefault(a => a.StartsWith("-") && !a.StartsWith("--"));

            if (!longName.StartsWith("--") && longName.StartsWith("-"))
            {
                shortName = longName;
                longName = option.Aliases.FirstOrDefault(a => a.StartsWith("--")) ?? longName;
            }

            options.Add(new OutputOption
            {
                Name = longName,
                Description = option.Description ?? string.Empty,
                ShortName = shortName,
                ValueType = GetValueType(option.ValueType),
                IsRequired = option.Required,
                DefaultValue = null,
                AllowedValues = null
            });
        }

        return options;
    }

    private List<OutputArgument> ExtractArguments(Command command)
    {
        var arguments = new List<OutputArgument>();

        foreach (var argument in command.Arguments)
        {
            arguments.Add(new OutputArgument
            {
                Name = argument.Name,
                Description = argument.Description ?? string.Empty,
                IsRequired = !argument.HasDefaultValue,
                IsVariadic = argument.Arity.MaximumNumberOfValues > 1
            });
        }

        return arguments;
    }

    private static string GetValueType(Type type)
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

    private static string SanitizeId(string name)
    {
        return name.ToLowerInvariant().Replace(" ", "-");
    }
}
