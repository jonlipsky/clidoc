using System.Text;
using Clidoc.SystemCommandLine.Schema;

namespace CliDoc.Output;

public class LlmsTxtRenderer
{
    public static string Render(List<OutputCommand> commands)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# CLI Command Reference");
        sb.AppendLine();
        sb.AppendLine("This is a plain-text reference of all available commands.");
        sb.AppendLine();

        foreach (var command in commands.OrderBy(c => c.FullName))
        {
            RenderCommand(sb, command);
        }

        return sb.ToString();
    }

    public static void RenderToFile(List<OutputCommand> commands, string filePath)
    {
        var content = Render(commands);
        
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);
    }

    private static void RenderCommand(StringBuilder sb, OutputCommand command)
    {
        // Command header
        sb.AppendLine($"## {command.FullName}");
        sb.AppendLine();

        // Description
        if (!string.IsNullOrEmpty(command.Description))
        {
            sb.AppendLine(command.Description);
            sb.AppendLine();
        }

        // Arguments
        if (command.Arguments.Count > 0)
        {
            sb.AppendLine("**Arguments:**");
            sb.AppendLine();
            foreach (var arg in command.Arguments)
            {
                var required = arg.IsRequired ? " (required)" : "";
                var variadic = arg.IsVariadic ? " (variadic)" : "";
                sb.AppendLine($"- `{arg.Name}`{required}{variadic}");
                if (!string.IsNullOrEmpty(arg.Description))
                {
                    sb.AppendLine($"  {arg.Description}");
                }
            }
            sb.AppendLine();
        }

        // Options
        if (command.Options.Count > 0)
        {
            sb.AppendLine("**Options:**");
            sb.AppendLine();
            foreach (var opt in command.Options)
            {
                var names = opt.ShortName != null ? $"{opt.Name}, {opt.ShortName}" : opt.Name;
                var required = opt.IsRequired ? " (required)" : "";
                var defaultValue = opt.DefaultValue != null ? $" [default: {opt.DefaultValue}]" : "";
                
                sb.AppendLine($"- `{names}` ({opt.ValueType}){required}{defaultValue}");
                if (!string.IsNullOrEmpty(opt.Description))
                {
                    sb.AppendLine($"  {opt.Description}");
                }

                if (opt.AllowedValues != null && opt.AllowedValues.Count > 0)
                {
                    sb.AppendLine($"  Allowed values: {string.Join(", ", opt.AllowedValues)}");
                }
            }
            sb.AppendLine();
        }

        // Examples
        if (command.Examples is { Count: > 0 })
        {
            sb.AppendLine("**Examples:**");
            sb.AppendLine();
            foreach (var example in command.Examples)
            {
                if (!string.IsNullOrEmpty(example.Description))
                {
                    sb.AppendLine($"{example.Description}:");
                }
                sb.AppendLine($"```");
                sb.AppendLine(example.Command);
                sb.AppendLine($"```");
                sb.AppendLine();
            }
        }

        // Subcommands
        if (command.IsGroup && command.Children.Count > 0)
        {
            sb.AppendLine($"**Subcommands:** {string.Join(", ", command.Children.Select(c => $"`{c.Split('-').Last()}`"))}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine();
    }
}
