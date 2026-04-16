using System.Reflection;
using Clidoc.SystemCommandLine;
using Clidoc.SystemCommandLine.Schema;
using CliDoc.Metadata;
using Markdig;

namespace CliDoc.Output;

public class SiteRenderer
{
    // Full Markdig pipeline — used for user-supplied prose in section bodies,
    // quick-start step titles/descriptions, etc. HTML is disabled so YAML
    // authors can't inject raw markup into the rendered site.
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UseAutoLinks()
        .Build();

    public void RenderSite(
        List<OutputCommand> commands,
        string outputPath,
        MetadataFile? metadata = null,
        string? title = null,
        bool writeCommandsJson = true,
        string? navIconFileName = null,
        string? faviconFileName = null)
    {
        // Create output directory
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        var document = BuildDocument(commands);

        // Generate commands.json (pretty) — unless the caller opted out.
        if (writeCommandsJson)
        {
            File.WriteAllText(
                Path.Combine(outputPath, "commands.json"),
                CliDocExporter.RenderJson(document, pretty: true));
        }

        // Generate data.js (compact, wrapped for the client-side app)
        var dataJs = $"window.__CLIDOC_DATA__ = {CliDocExporter.RenderJson(document, pretty: false)};";
        File.WriteAllText(Path.Combine(outputPath, "data.js"), dataJs);

        // Copy template files (app logic + CSS)
        CopyEmbeddedResource("CliDoc.Templates.commands.js", Path.Combine(outputPath, "commands.js"));
        CopyEmbeddedResource("CliDoc.Templates.style.css", Path.Combine(outputPath, "style.css"));

        // Generate commands.html with placeholders replaced
        // For the nav brand, prefer: --title flag, then metadata title, then root command name
        var rootName = commands.FirstOrDefault(c => c.IsRoot)?.Name;
        var siteTitle = title ?? metadata?.Site?.Title ?? rootName ?? "CLI Documentation";
        var githubUrl = metadata?.Site?.GitHubUrl ?? "";
        var navIconImg = string.IsNullOrEmpty(navIconFileName)
            ? ""
            : $"<img src=\"{EscapeHtml(navIconFileName)}\" alt=\"\" class=\"nav-icon\">";
        var faviconLink = string.IsNullOrEmpty(faviconFileName)
            ? ""
            : $"<link rel=\"icon\" href=\"{EscapeHtml(faviconFileName)}\">";
        var commandsHtml = GetEmbeddedResourceAsString("CliDoc.Templates.commands.html");
        commandsHtml = commandsHtml
            .Replace("{{SITE_TITLE}}", EscapeHtml(siteTitle))
            .Replace("{{NAV_ICON_IMG}}", navIconImg)
            .Replace("{{FAVICON_LINK}}", faviconLink)
            .Replace("{{GITHUB_URL}}", EscapeHtml(githubUrl));
        // Hide GitHub link if no URL configured
        if (string.IsNullOrEmpty(githubUrl))
        {
            commandsHtml = commandsHtml.Replace(
                "<a href=\"\" class=\"nav-link\" target=\"_blank\">GitHub</a>", "");
        }
        // Hide the commands.json nav link if we aren't emitting the file.
        if (!writeCommandsJson)
        {
            commandsHtml = commandsHtml.Replace(
                "<a href=\"commands.json\" class=\"nav-link\">commands.json</a>", "");
        }
        File.WriteAllText(Path.Combine(outputPath, "commands.html"), commandsHtml);

        // Always emit index.html. When there's no site metadata, synthesize a minimal
        // SiteConfig from the root command so the landing page still renders.
        var effectiveSite = metadata?.Site ?? BuildMinimalSiteConfig(commands);
        var hasMetadataSite = metadata?.Site != null;
        var indexHtml = GenerateIndexHtml(effectiveSite, title, commands, writeCommandsJson, hasMetadataSite, navIconImg, faviconLink);
        File.WriteAllText(Path.Combine(outputPath, "index.html"), indexHtml);
    }

    private static SiteConfig BuildMinimalSiteConfig(List<OutputCommand> commands)
    {
        var root = commands.FirstOrDefault(c => c.IsRoot);
        return new SiteConfig
        {
            Title = root?.Name,
            Tagline = string.IsNullOrWhiteSpace(root?.Description) ? null : root.Description
        };
    }

    private static CommandsOutput BuildDocument(List<OutputCommand> commands) => new()
    {
        SchemaVersion = CliDocExporter.SchemaVersion,
        GeneratedAt = DateTime.UtcNow.ToString("O"),
        Generator = "clidoc",
        Commands = commands
    };

    private string GenerateIndexHtml(SiteConfig site, string? title, List<OutputCommand> commands, bool includeCommandsJsonLink, bool includeInstallSection, string navIconImg, string faviconLink)
    {
        var template = GetEmbeddedResourceAsString("CliDoc.Templates.index.html");
        
        // Build Quick Start HTML from site config
        var quickstartHtml = "";
        if (site.QuickStart != null && site.QuickStart.Count > 0)
        {
            quickstartHtml = RenderQuickStart(site.QuickStart);
        }

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
            .Replace("{{NAV_ICON_IMG}}", navIconImg)
            .Replace("{{FAVICON_LINK}}", faviconLink)
            .Replace("{{GITHUB_URL}}", site.GitHubUrl ?? "")
            .Replace("{{PACKAGE_ID}}", site.PackageId ?? title?.ToLowerInvariant() ?? "cli-tool")
            .Replace("{{QUICKSTART}}", quickstartHtml)
            .Replace("{{SECTIONS}}", sectionsHtml);

        // Remove handlebars-style conditionals (simple implementation)
        html = ProcessConditionals(html, new Dictionary<string, bool>
        {
            { "LOGO", !string.IsNullOrEmpty(site.Logo) },
            { "TAGLINE", !string.IsNullOrEmpty(site.Tagline) },
            { "GITHUB_URL", !string.IsNullOrEmpty(site.GitHubUrl) },
            { "QUICKSTART", site.QuickStart != null && site.QuickStart.Count > 0 },
            { "COMMANDS_JSON", includeCommandsJsonLink },
            { "INSTALL", includeInstallSection }
        });

        return html;
    }

    private string RenderQuickStart(List<QuickStartScenario> scenarios)
    {
        var sb = new System.Text.StringBuilder();

        // Prompt sits above the two-column layout
        sb.AppendLine("<p class=\"qs-prompt\">What do you want to do?</p>");

        // Two-column layout
        sb.AppendLine("<div class=\"qs-columns\">");

        // Scenario selector buttons
        sb.AppendLine("<div class=\"qs-selector\">");
        sb.AppendLine("  <div class=\"qs-options\">");
        for (var i = 0; i < scenarios.Count; i++)
        {
            var active = i == 0 ? " active" : "";
            sb.AppendLine($"    <button class=\"qs-option{active}\" data-scenario=\"{i}\">{EscapeHtml(scenarios[i].Name)}</button>");
        }
        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        // Scenario step panels
        sb.AppendLine("<div class=\"qs-panels\">");
        for (var i = 0; i < scenarios.Count; i++)
        {
            var hidden = i == 0 ? "" : " style=\"display:none\"";
            sb.AppendLine($"  <div class=\"qs-panel\" data-scenario=\"{i}\"{hidden}>");
            for (var s = 0; s < scenarios[i].Steps.Count; s++)
            {
                var step = scenarios[i].Steps[s];
                sb.AppendLine("    <div class=\"step-card\">");
                sb.AppendLine($"      <div class=\"step-number\">{s + 1}</div>");
                sb.AppendLine("      <div class=\"step-body\">");
                sb.AppendLine($"        <div class=\"step-title\">{RenderInlineMarkdown(step.Title)}</div>");
                if (!string.IsNullOrEmpty(step.Command))
                {
                    sb.AppendLine("        <div class=\"step-code-wrapper\">");
                    sb.AppendLine($"          <pre class=\"step-code\"><code>{EscapeHtml(step.Command)}</code></pre>");
                    sb.AppendLine("          <button class=\"copy-btn\" onclick=\"copyToClipboard(this)\" title=\"Copy to clipboard\">");
                    sb.AppendLine("            <svg width=\"16\" height=\"16\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M10 8V7C10 6.05719 10 5.58579 10.2929 5.29289C10.5858 5 11.0572 5 12 5H17C17.9428 5 18.4142 5 18.7071 5.29289C19 5.58579 19 6.05719 19 7V12C19 12.9428 19 13.4142 18.7071 13.7071C18.4142 14 17.9428 14 17 14H16M7 19H12C12.9428 19 13.4142 19 13.7071 18.7071C14 18.4142 14 17.9428 14 17V12C14 11.0572 14 10.5858 13.7071 10.2929C13.4142 10 12.9428 10 12 10H7C6.05719 10 5.58579 10 5.29289 10.2929C5 10.5858 5 11.0572 5 12V17C5 17.9428 5 18.4142 5.29289 18.7071C5.58579 19 6.05719 19 7 19Z\"/></svg>");
                    sb.AppendLine("          </button>");
                    sb.AppendLine("        </div>");
                }
                if (!string.IsNullOrEmpty(step.Description))
                {
                    sb.AppendLine($"        <div class=\"step-desc\">{RenderInlineMarkdown(step.Description)}</div>");
                }
                sb.AppendLine("      </div>");
                sb.AppendLine("    </div>");
            }
            sb.AppendLine("  </div>");
        }
        sb.AppendLine("</div>");

        // Close qs-columns
        sb.AppendLine("</div>");

        return sb.ToString();
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

            // Unordered list items (with optional indented description line)
            if (line.StartsWith("- "))
            {
                sb.AppendLine("<ul class=\"feature-list\">");
                while (i < lines.Count && lines[i].StartsWith("- "))
                {
                    var itemText = RenderInlineMarkdown(lines[i][2..]);
                    i++;
                    // Check for indented continuation line (description)
                    string? descText = null;
                    if (i < lines.Count && lines[i].Length > 0 && (lines[i][0] == ' ' || lines[i][0] == '\t'))
                    {
                        descText = lines[i].Trim();
                        i++;
                    }
                    if (descText != null)
                    {
                        sb.AppendLine($"<li><div class=\"feature-title\">{itemText}</div><div class=\"feature-desc\">{RenderInlineMarkdown(descText)}</div></li>");
                    }
                    else
                    {
                        sb.AppendLine($"<li>{itemText}</li>");
                    }
                }
                sb.AppendLine("</ul>");
                continue;
            }

            // Regular paragraph — accumulate consecutive non-blank lines so that
            // hard-wrapped source text renders as a single <p>, matching standard
            // Markdown behavior. A blank line or the start of a special construct
            // ends the paragraph.
            var paragraphLines = new List<string>();
            while (i < lines.Count)
            {
                var current = lines[i];
                if (string.IsNullOrWhiteSpace(current)) break;
                if (current.TrimStart().StartsWith("```")) break;
                if (current.StartsWith("- ")) break;
                if (current.Length > 2 && char.IsDigit(current[0]) && current[1] == '.') break;
                paragraphLines.Add(current.Trim());
                i++;
            }
            if (paragraphLines.Count > 0)
            {
                sb.AppendLine($"<p>{RenderInlineMarkdown(string.Join(" ", paragraphLines))}</p>");
            }
        }

        return sb.ToString();
    }

    private static string RenderInlineMarkdown(string text)
    {
        // Run the text through Markdig so links, bold, inline code, autolinks,
        // etc. all render correctly. Markdig wraps a single line in <p>…</p>;
        // strip that wrapper so callers can embed the result inside other
        // elements (card titles, step descriptions, list items, …).
        if (string.IsNullOrEmpty(text)) return string.Empty;

        var html = Markdown.ToHtml(text, MarkdownPipeline).Trim();
        if (html.StartsWith("<p>", StringComparison.Ordinal) &&
            html.EndsWith("</p>", StringComparison.Ordinal))
        {
            html = html.Substring(3, html.Length - 7);
        }
        return html;
    }

    private static void CopyEmbeddedResource(string resourceName, string targetPath)
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

    private static string GetEmbeddedResourceAsString(string resourceName)
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

    private static string EscapeHtml(string text)
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

    private static string ProcessConditionals(string html, Dictionary<string, bool> conditions)
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
