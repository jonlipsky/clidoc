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

        // Generate data.js (wraps JSON as window.__CLIDOC_DATA__)
        var dataJs = GenerateDataJs(commands);
        File.WriteAllText(Path.Combine(outputPath, "data.js"), dataJs);

        // Copy template files (app logic + HTML + CSS)
        CopyEmbeddedResource("CliDoc.Templates.commands.js", Path.Combine(outputPath, "commands.js"));
        CopyEmbeddedResource("CliDoc.Templates.commands.html", Path.Combine(outputPath, "commands.html"));
        CopyEmbeddedResource("CliDoc.Templates.style.css", Path.Combine(outputPath, "style.css"));

        // Generate index.html if site config exists
        if (metadata?.Site != null)
        {
            var indexHtml = GenerateIndexHtml(metadata.Site, title, commands);
            File.WriteAllText(Path.Combine(outputPath, "index.html"), indexHtml);
        }
    }

    private string GenerateDataJs(List<OutputCommand> commands)
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

    private string GenerateIndexHtml(SiteConfig site, string? title, List<OutputCommand> commands)
    {
        var template = GetEmbeddedResourceAsString("CliDoc.Templates.index.html");
        
        // Build sections HTML from root command
        var sectionsHtml = "";
        var rootCommand = commands.FirstOrDefault(c => c.IsRoot);
        if (rootCommand?.Sections != null && rootCommand.Sections.Count > 0)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var section in rootCommand.Sections)
            {
                sb.AppendLine($"<section class=\"landing-section\">");
                sb.AppendLine($"  <h2>{EscapeHtml(section.Title)}</h2>");
                sb.AppendLine($"  <div class=\"markdown-content\">{RenderMarkdown(section.Body)}</div>");
                sb.AppendLine($"</section>");
            }
            sectionsHtml = sb.ToString();
        }

        var html = template
            .Replace("{{SITE_TITLE}}", EscapeHtml(title ?? site.Title ?? "CLI Documentation"))
            .Replace("{{TAGLINE}}", EscapeHtml(site.Tagline ?? ""))
            .Replace("{{LOGO}}", site.Logo ?? "")
            .Replace("{{GITHUB_URL}}", site.GitHubUrl ?? "")
            .Replace("{{PACKAGE_ID}}", title?.ToLowerInvariant() ?? "cli-tool")
            .Replace("{{SECTIONS}}", sectionsHtml);

        // Remove handlebars-style conditionals (simple implementation)
        html = ProcessConditionals(html, new Dictionary<string, bool>
        {
            { "LOGO", !string.IsNullOrEmpty(site.Logo) },
            { "TAGLINE", !string.IsNullOrEmpty(site.Tagline) },
            { "GITHUB_URL", !string.IsNullOrEmpty(site.GitHubUrl) }
        });

        return html;
    }

    private string RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        var lines = markdown.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
        var sb = new System.Text.StringBuilder();
        var i = 0;

        while (i < lines.Count)
        {
            var line = lines[i];

            // Empty line — skip
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Ordered list item — collect as step card with optional code block
            if (line.Length > 2 && char.IsDigit(line[0]) && line[1] == '.')
            {
                var stepNum = line[0] - '0';
                var stepText = line[2..].TrimStart();
                i++;

                // Look ahead for a code block belonging to this step
                string? codeContent = null;
                while (i < lines.Count && string.IsNullOrWhiteSpace(lines[i])) i++;
                if (i < lines.Count && lines[i].TrimStart().StartsWith("```"))
                {
                    i++; // skip opening ```
                    var codeSb = new System.Text.StringBuilder();
                    while (i < lines.Count && !lines[i].TrimStart().StartsWith("```"))
                    {
                        codeSb.AppendLine(lines[i]);
                        i++;
                    }
                    if (i < lines.Count) i++; // skip closing ```
                    codeContent = codeSb.ToString().TrimEnd();
                }

                sb.AppendLine("<div class=\"step-card\">");
                sb.AppendLine($"  <div class=\"step-number\">{stepNum}</div>");
                sb.AppendLine("  <div class=\"step-body\">");
                sb.AppendLine($"    <div class=\"step-title\">{RenderInlineMarkdown(stepText)}</div>");
                if (codeContent != null)
                {
                    sb.AppendLine($"    <pre class=\"step-code\"><code>{EscapeHtml(codeContent)}</code></pre>");
                }
                sb.AppendLine("  </div>");
                sb.AppendLine("</div>");
                continue;
            }

            // Standalone code block (not attached to a step)
            if (line.TrimStart().StartsWith("```"))
            {
                i++;
                var codeSb = new System.Text.StringBuilder();
                while (i < lines.Count && !lines[i].TrimStart().StartsWith("```"))
                {
                    codeSb.AppendLine(lines[i]);
                    i++;
                }
                if (i < lines.Count) i++;
                sb.AppendLine($"<pre><code>{EscapeHtml(codeSb.ToString().TrimEnd())}</code></pre>");
                continue;
            }

            // Unordered list items
            if (line.StartsWith("- "))
            {
                sb.AppendLine("<ul class=\"feature-list\">");
                while (i < lines.Count && lines[i].StartsWith("- "))
                {
                    sb.AppendLine($"<li>{RenderInlineMarkdown(lines[i][2..])}</li>");
                    i++;
                }
                sb.AppendLine("</ul>");
                continue;
            }

            // Regular paragraph
            sb.AppendLine($"<p>{RenderInlineMarkdown(line)}</p>");
            i++;
        }

        return sb.ToString();
    }

    private string RenderInlineMarkdown(string text)
    {
        // Bold: **text**
        text = System.Text.RegularExpressions.Regex.Replace(
            text, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
        // Inline code: `text`
        text = System.Text.RegularExpressions.Regex.Replace(
            text, @"`(.+?)`", "<code>$1</code>");
        return text;
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
