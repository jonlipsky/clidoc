using System.Reflection;
using System.Text.Json;
using CliDoc.Metadata;

namespace CliDoc.Output;

public class SiteRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void RenderSite(
        List<OutputCommand> commands,
        string outputPath,
        MetadataFile? metadata = null,
        string? title = null)
    {
        // Create output directory
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // Generate commands.json
        var jsonRenderer = new JsonRenderer();
        var commandsJson = jsonRenderer.Render(commands);
        File.WriteAllText(Path.Combine(outputPath, "commands.json"), commandsJson);

        // Generate commands.js (wraps JSON as window.__CLIDOC_DATA__)
        var commandsJs = GenerateCommandsJs(commands);
        File.WriteAllText(Path.Combine(outputPath, "commands.js"), commandsJs);

        // Copy template files
        CopyEmbeddedResource("CliDoc.Templates.commands.html", Path.Combine(outputPath, "commands.html"));
        CopyEmbeddedResource("CliDoc.Templates.style.css", Path.Combine(outputPath, "style.css"));

        // Generate index.html if site config exists
        if (metadata?.Site != null)
        {
            var indexHtml = GenerateIndexHtml(metadata.Site, title);
            File.WriteAllText(Path.Combine(outputPath, "index.html"), indexHtml);
        }
    }

    private string GenerateCommandsJs(List<OutputCommand> commands)
    {
        var output = new CommandsOutput
        {
            Version = "1.0.0",
            GeneratedAt = DateTime.UtcNow.ToString("O"),
            Generator = "clidoc",
            Commands = commands
        };

        var json = JsonSerializer.Serialize(output, JsonOptions);
        return $"window.__CLIDOC_DATA__ = {json};";
    }

    private string GenerateIndexHtml(SiteConfig site, string? title)
    {
        var template = GetEmbeddedResourceAsString("CliDoc.Templates.index.html");
        
        var html = template
            .Replace("{{SITE_TITLE}}", EscapeHtml(title ?? site.Title ?? "CLI Documentation"))
            .Replace("{{TAGLINE}}", EscapeHtml(site.Tagline ?? ""))
            .Replace("{{LOGO}}", site.Logo ?? "")
            .Replace("{{PACKAGE_ID}}", title?.ToLowerInvariant() ?? "cli-tool");

        // Remove handlebars-style conditionals (simple implementation)
        html = ProcessConditionals(html, new Dictionary<string, bool>
        {
            { "LOGO", !string.IsNullOrEmpty(site.Logo) },
            { "TAGLINE", !string.IsNullOrEmpty(site.Tagline) }
        });

        return html;
    }

    private void CopyEmbeddedResource(string resourceName, string targetPath)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        }

        using var fileStream = File.Create(targetPath);
        stream.CopyTo(fileStream);
    }

    private string GetEmbeddedResourceAsString(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName);
        
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    private string ProcessConditionals(string html, Dictionary<string, bool> conditions)
    {
        // Simple implementation: remove {{#if X}}...{{/if}} blocks where condition is false
        foreach (var condition in conditions)
        {
            var pattern = $"{{{{#if {condition.Key}}}}}";
            var endPattern = "{{/if}}";
            
            var startIndex = html.IndexOf(pattern);
            while (startIndex >= 0)
            {
                var endIndex = html.IndexOf(endPattern, startIndex);
                if (endIndex < 0) break;

                var blockStart = startIndex;
                var blockEnd = endIndex + endPattern.Length;
                var contentStart = startIndex + pattern.Length;
                var contentEnd = endIndex;

                if (condition.Value)
                {
                    // Keep content, remove tags
                    var content = html.Substring(contentStart, contentEnd - contentStart);
                    html = html.Remove(blockStart, blockEnd - blockStart);
                    html = html.Insert(blockStart, content);
                }
                else
                {
                    // Remove entire block
                    html = html.Remove(blockStart, blockEnd - blockStart);
                }

                startIndex = html.IndexOf(pattern, blockStart);
            }
        }

        return html;
    }
}
