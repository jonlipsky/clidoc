using System.CommandLine;
using Clidoc.SystemCommandLine.Schema;
using CliDoc.Input;
using CliDoc.Merging;
using CliDoc.Metadata;
using CliDoc.Output;

namespace CliDoc.Commands;

/// <summary>
/// <c>clidoc generate docs</c> — render a static documentation site from a
/// commands.json file, optionally enriched with a cli-docs.yaml metadata file.
/// </summary>
public static class GenerateDocsCommand
{
    public static Command Create()
    {
        var commandsJsonOption = new Option<string?>("--commands-json", "-c")
        {
            Description = "Path to commands.json. Defaults to ./commands.json if it exists."
        };

        var metadataOption = new Option<string?>("--metadata", "-m")
        {
            Description = "Path to cli-docs.yaml metadata file. Defaults to ./cli-docs.yaml if it exists."
        };

        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output directory for generated site.",
            DefaultValueFactory = _ => "./clidoc-output"
        };

        var titleOption = new Option<string?>("--title")
        {
            Description = "Site title (overrides metadata)."
        };

        var rootNameOption = new Option<string?>("--root-name")
        {
            Description = "Override the root command's name (affects breadcrumbs, tree root, and subcommand full names)."
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

        var noCommandsJsonOption = new Option<bool>("--no-commands-json")
        {
            Description = "Don't copy commands.json into the output directory (and hide its nav link).",
            DefaultValueFactory = _ => false
        };

        var command = new Command("docs", "Render a static documentation site from commands.json.")
        {
            commandsJsonOption,
            metadataOption,
            outputOption,
            titleOption,
            rootNameOption,
            baseUrlOption,
            noLlmsTxtOption,
            noCommandsJsonOption
        };

        command.SetAction(parseResult =>
        {
            try
            {
                Generate(
                    parseResult.GetValue(commandsJsonOption),
                    parseResult.GetValue(metadataOption),
                    parseResult.GetValue(outputOption)!,
                    parseResult.GetValue(titleOption),
                    parseResult.GetValue(rootNameOption),
                    parseResult.GetValue(baseUrlOption),
                    parseResult.GetValue(noLlmsTxtOption),
                    parseResult.GetValue(noCommandsJsonOption));
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private static void Generate(
        string? commandsJsonPath,
        string? metadataPath,
        string outputPath,
        string? title,
        string? rootName,
        string? baseUrl,
        bool noLlmsTxt,
        bool noCommandsJson)
    {
        var document = LoadCommandsJson(commandsJsonPath, rootName);

        MetadataFile? metadata = null;
        string? metadataDir = null;
        var metadataLoader = new MetadataLoader();
        if (!string.IsNullOrEmpty(metadataPath))
        {
            Console.WriteLine($"Loading metadata: {metadataPath}");
            metadata = metadataLoader.Load(metadataPath);
            metadataDir = Path.GetDirectoryName(Path.GetFullPath(metadataPath));
        }
        else if (File.Exists("cli-docs.yaml"))
        {
            Console.WriteLine("Loading metadata: cli-docs.yaml");
            metadata = metadataLoader.Load("cli-docs.yaml");
            metadataDir = Directory.GetCurrentDirectory();
        }

        var merger = new CommandMerger();
        var merged = merger.Merge(document.Commands, metadata);
        Console.WriteLine("Merged command data with metadata");

        // Ensure output directory exists so we can copy referenced assets into it.
        if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);

        var navIconFileName = CopyReferencedAsset(metadata?.Site?.Icon, metadataDir, outputPath, label: "icon");
        var faviconFileName = CopyReferencedAsset(metadata?.Site?.Favicon, metadataDir, outputPath, label: "favicon");

        var siteRenderer = new SiteRenderer();
        siteRenderer.RenderSite(
            merged,
            outputPath,
            metadata,
            title,
            writeCommandsJson: !noCommandsJson,
            navIconFileName: navIconFileName,
            faviconFileName: faviconFileName);
        Console.WriteLine($"Generated site to: {outputPath}");

        if (!noLlmsTxt)
        {
            var llmsRenderer = new LlmsTxtRenderer();
            llmsRenderer.RenderToFile(merged, Path.Combine(outputPath, "llms.txt"));
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
    }

    /// <summary>
    /// Resolves an asset path declared in cli-docs.yaml (relative to the yaml file's
    /// directory), copies it into the output directory, and returns the output-relative
    /// filename. Accepts absolute URLs (http/https) as-is and skips copying.
    /// Returns null when no path is provided or the file can't be found.
    /// </summary>
    private static string? CopyReferencedAsset(string? configuredPath, string? metadataDir, string outputPath, string label)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) return null;

        // Pass URLs through unchanged — they don't need to be copied into the output.
        if (configuredPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            configuredPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return configuredPath;
        }

        var searchBase = metadataDir ?? Directory.GetCurrentDirectory();
        var sourcePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(searchBase, configuredPath));

        if (!File.Exists(sourcePath))
        {
            Console.Error.WriteLine($"Warning: {label} not found at {sourcePath}; skipping.");
            return null;
        }

        var outputFileName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(outputPath, outputFileName);
        File.Copy(sourcePath, destPath, overwrite: true);
        Console.WriteLine($"Copied {label}: {sourcePath} -> {destPath}");
        return outputFileName;
    }

    internal static CommandsOutput LoadCommandsJson(string? explicitPath, string? rootNameOverride)
    {
        var path = ResolveCommandsJsonPath(explicitPath);

        Console.WriteLine($"Loading commands.json: {path}");
        var loader = new CommandsJsonLoader();
        var document = loader.Load(path);
        Console.WriteLine($"Loaded {document.Commands.Count} command(s) from {path}");

        if (!string.IsNullOrEmpty(rootNameOverride))
        {
            document = ApplyRootRename(document, rootNameOverride);
        }

        return document;
    }

    internal static string ResolveCommandsJsonPath(string? explicitPath)
    {
        if (!string.IsNullOrEmpty(explicitPath))
        {
            return explicitPath;
        }

        const string defaultPath = "commands.json";
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        throw new InvalidOperationException(
            "No commands.json found. Pass --commands-json <path> or place a commands.json " +
            "in the current directory. To produce one, run `clidoc generate commands --assembly ...` " +
            "or add the Clidoc.SystemCommandLine NuGet to your app and run `yourcli commands --output commands.json`.");
    }

    private static CommandsOutput ApplyRootRename(CommandsOutput document, string newRootName)
    {
        var root = document.Commands.FirstOrDefault(c => c.IsRoot);
        if (root is null || string.Equals(root.Name, newRootName, StringComparison.Ordinal))
        {
            return document;
        }

        var oldName = root.Name;
        var renamed = document.Commands.Select(cmd =>
        {
            if (cmd.IsRoot)
            {
                return cmd with { Name = newRootName, FullName = newRootName };
            }
            if (cmd.FullName.StartsWith(oldName + " ", StringComparison.Ordinal))
            {
                return cmd with { FullName = newRootName + cmd.FullName.Substring(oldName.Length) };
            }
            return cmd;
        }).ToList();

        return new CommandsOutput
        {
            SchemaVersion = document.SchemaVersion,
            GeneratedAt = document.GeneratedAt,
            Generator = document.Generator,
            Commands = renamed
        };
    }
}
