using System.CommandLine;
using CliDoc.Input;
using CliDoc.Merging;
using CliDoc.Metadata;
using CliDoc.Output;

namespace CliDoc.Commands;

public class GenerateCommand
{
    public static Command Create()
    {
        var commandsJsonArg = new Argument<string?>("commands-json")
        {
            Description = "Path to commands.json (produced by `your-cli commands` or by clidoc itself).",
            Arity = ArgumentArity.ZeroOrOne
        };

        var assemblyOption = new Option<string?>("--assembly", ["-a"])
        {
            Description = "(Simple apps only) Path to the compiled CLI assembly (.dll) to reflect over."
        };

        var projectOption = new Option<string?>("--project", ["-p"])
        {
            Description = "(Simple apps only) Path to a .csproj file; clidoc will build it and reflect over the output."
        };

        var metadataOption = new Option<string?>("--metadata", ["-m"])
        {
            Description = "Path to cli-docs.yaml metadata file."
        };

        var outputOption = new Option<string>("--output", ["-o"])
        {
            Description = "Output directory for generated site.",
            DefaultValueFactory = _ => "./clidoc-output"
        };

        var titleOption = new Option<string?>("--title")
        {
            Description = "Site title (overrides metadata)."
        };

        var entryTypeOption = new Option<string?>("--entry-type", ["-t"])
        {
            Description = "Fully-qualified type name with a static method returning RootCommand (assembly path only)."
        };

        var baseUrlOption = new Option<string?>("--base-url")
        {
            Description = "Base URL for canonical links."
        };

        var noLlmsTxtOption = new Option<bool>("--no-llms-txt")
        {
            Description = "Skip llms.txt generation.",
            DefaultValueFactory = _ => false
        };

        var command = new Command("generate", "Generate a static documentation site from a commands.json file.")
        {
            commandsJsonArg,
            assemblyOption,
            projectOption,
            metadataOption,
            outputOption,
            titleOption,
            entryTypeOption,
            baseUrlOption,
            noLlmsTxtOption
        };

        command.SetAction(async (ParseResult parseResult) =>
        {
            var commandsJson = parseResult.GetValue(commandsJsonArg);
            var assemblyPath = parseResult.GetValue(assemblyOption);
            var projectPath = parseResult.GetValue(projectOption);
            var metadataPath = parseResult.GetValue(metadataOption);
            var output = parseResult.GetValue(outputOption)!;
            var title = parseResult.GetValue(titleOption);
            var entryType = parseResult.GetValue(entryTypeOption);
            var baseUrl = parseResult.GetValue(baseUrlOption);
            var noLlmsTxt = parseResult.GetValue(noLlmsTxtOption);
            try
            {
                await GenerateAsync(commandsJson, assemblyPath, projectPath, metadataPath, output, title, entryType, baseUrl, noLlmsTxt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        return command;
    }

    private static Task GenerateAsync(
        string? commandsJsonPath,
        string? assemblyPath,
        string? projectPath,
        string? metadataPath,
        string outputPath,
        string? title,
        string? entryType,
        string? baseUrl,
        bool noLlmsTxt)
    {
        var resolver = new InputResolver();
        var resolved = resolver.Resolve(commandsJsonPath, assemblyPath, projectPath, entryType);

        // Load metadata if provided (or the default file exists)
        MetadataFile? metadata = null;
        var metadataLoader = new MetadataLoader();
        if (!string.IsNullOrEmpty(metadataPath))
        {
            Console.WriteLine($"Loading metadata: {metadataPath}");
            metadata = metadataLoader.Load(metadataPath);
        }
        else if (File.Exists("cli-docs.yaml"))
        {
            Console.WriteLine("Loading metadata: cli-docs.yaml");
            metadata = metadataLoader.Load("cli-docs.yaml");
        }

        // Merge extracted + metadata
        var merger = new CommandMerger();
        var merged = merger.Merge(resolved.Document.Commands, metadata);

        Console.WriteLine("Merged command data with metadata");

        // Render static site
        var siteRenderer = new SiteRenderer();
        siteRenderer.RenderSite(merged, outputPath, metadata, title);

        Console.WriteLine($"Generated site to: {outputPath}");

        // Render llms.txt
        if (!noLlmsTxt)
        {
            var llmsRenderer = new LlmsTxtRenderer();
            var llmsPath = Path.Combine(outputPath, "llms.txt");
            llmsRenderer.RenderToFile(merged, llmsPath);
            Console.WriteLine("Generated llms.txt");
        }

        Console.WriteLine();
        Console.WriteLine("✓ Documentation generation complete!");
        Console.WriteLine();
        Console.WriteLine($"  Open: {Path.Combine(outputPath, "commands.html")}");

        if (metadata?.Site != null)
        {
            Console.WriteLine($"  Landing page: {Path.Combine(outputPath, "index.html")}");
        }

        return Task.CompletedTask;
    }
}
