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
        var assemblyOption = new Option<string>(
            aliases: new[] { "--assembly", "-a" },
            description: "Path to the compiled CLI assembly (.dll)")
        {
            IsRequired = true
        };

        var metadataOption = new Option<string?>(
            aliases: new[] { "--metadata", "-m" },
            description: "Path to cli-docs.yaml metadata file");

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Output directory for generated site",
            getDefaultValue: () => "./clidoc-output");

        var titleOption = new Option<string?>(
            aliases: new[] { "--title" },
            description: "Site title (overrides metadata)");

        var entryTypeOption = new Option<string?>(
            aliases: new[] { "--entry-type", "-t" },
            description: "Fully-qualified type name with a static method returning RootCommand");

        var baseUrlOption = new Option<string?>(
            aliases: new[] { "--base-url" },
            description: "Base URL for canonical links");

        var noLlmsTxtOption = new Option<bool>(
            aliases: new[] { "--no-llms-txt" },
            description: "Skip llms.txt generation",
            getDefaultValue: () => false);

        var command = new Command("generate", "Generate static documentation site")
        {
            assemblyOption,
            metadataOption,
            outputOption,
            titleOption,
            entryTypeOption,
            baseUrlOption,
            noLlmsTxtOption
        };

        command.SetHandler(async (
            string assemblyPath,
            string? metadataPath,
            string output,
            string? title,
            string? entryType,
            string? baseUrl,
            bool noLlmsTxt) =>
        {
            try
            {
                await GenerateAsync(assemblyPath, metadataPath, output, title, entryType, baseUrl, noLlmsTxt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }, assemblyOption, metadataOption, outputOption, titleOption, entryTypeOption, baseUrlOption, noLlmsTxtOption);

        return command;
    }

    private static async Task GenerateAsync(
        string assemblyPath,
        string? metadataPath,
        string outputPath,
        string? title,
        string? entryType,
        string? baseUrl,
        bool noLlmsTxt)
    {
        Console.WriteLine($"Loading assembly: {assemblyPath}");

        // Load command from assembly
        var loader = new AssemblyCommandLoader();
        var rootCommand = loader.LoadCommand(assemblyPath, entryType);

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
