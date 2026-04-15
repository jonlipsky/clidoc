using System.CommandLine;
using CliDoc.Extraction;
using CliDoc.Loading;
using CliDoc.Merging;
using CliDoc.Metadata;
using CliDoc.Output;

namespace CliDoc.Commands;

public class GenerateCommand
{
    public static Command Create()
    {
        var assemblyOption = new Option<string?>("--assembly", ["-a"])
        {
            Description = "Path to the compiled CLI assembly (.dll)"
        };

        var projectOption = new Option<string?>("--project", ["-p"])
        {
            Description = "Path to the .csproj file (builds the project and resolves the assembly)"
        };

        var metadataOption = new Option<string?>("--metadata", ["-m"])
        {
            Description = "Path to cli-docs.yaml metadata file"
        };

        var outputOption = new Option<string>("--output", ["-o"])
        {
            Description = "Output directory for generated site",
            DefaultValueFactory = _ => "./clidoc-output"
        };

        var titleOption = new Option<string?>("--title")
        {
            Description = "Site title (overrides metadata)"
        };

        var entryTypeOption = new Option<string?>("--entry-type", ["-t"])
        {
            Description = "Fully-qualified type name with a static method returning RootCommand"
        };

        var baseUrlOption = new Option<string?>("--base-url")
        {
            Description = "Base URL for canonical links"
        };

        var noLlmsTxtOption = new Option<bool>("--no-llms-txt")
        {
            Description = "Skip llms.txt generation",
            DefaultValueFactory = _ => false
        };

        var command = new Command("generate", "Generate static documentation site")
        {
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
                await GenerateAsync(assemblyPath, projectPath, metadataPath, output, title, entryType, baseUrl, noLlmsTxt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        });

        return command;
    }

    private static async Task GenerateAsync(
        string? assemblyPath,
        string? projectPath,
        string? metadataPath,
        string outputPath,
        string? title,
        string? entryType,
        string? baseUrl,
        bool noLlmsTxt)
    {
        var resolvedAssemblyPath = InitCommand.ResolveAssemblyPath(assemblyPath, projectPath);

        Console.WriteLine($"Loading assembly: {resolvedAssemblyPath}");

        // Load command from assembly
        var loader = new AssemblyCommandLoader();
        var rootCommand = loader.LoadCommand(resolvedAssemblyPath, entryType);

        Console.WriteLine($"Discovered root command: {rootCommand.Name}");

        // Extract command structure
        var extractor = new CommandExtractor();
        var extracted = extractor.Extract(rootCommand);

        Console.WriteLine($"Extracted {extracted.Count} command(s)");

        // Load metadata if provided
        MetadataFile? metadata = null;
        if (!string.IsNullOrEmpty(metadataPath))
        {
            Console.WriteLine($"Loading metadata: {metadataPath}");
            var metadataLoader = new MetadataLoader();
            metadata = metadataLoader.Load(metadataPath);
        }
        else
        {
            // Try default location
            var defaultPath = "cli-docs.yaml";
            if (File.Exists(defaultPath))
            {
                Console.WriteLine($"Loading metadata: {defaultPath}");
                var metadataLoader = new MetadataLoader();
                metadata = metadataLoader.Load(defaultPath);
            }
        }

        // Merge extracted + metadata
        var merger = new CommandMerger();
        var merged = merger.Merge(extracted, metadata);

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
            Console.WriteLine($"Generated llms.txt");
        }

        Console.WriteLine();
        Console.WriteLine("✓ Documentation generation complete!");
        Console.WriteLine();
        Console.WriteLine($"  Open: {Path.Combine(outputPath, "commands.html")}");
        
        if (metadata?.Site != null)
        {
            Console.WriteLine($"  Landing page: {Path.Combine(outputPath, "index.html")}");
        }
    }
}
