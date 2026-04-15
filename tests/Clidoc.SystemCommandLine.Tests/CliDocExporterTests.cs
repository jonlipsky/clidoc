using System.CommandLine;
using System.Text.Json;
using Clidoc.SystemCommandLine;
using Clidoc.SystemCommandLine.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clidoc.SystemCommandLine.Tests;

[TestClass]
public class CliDocExporterTests
{
    [TestMethod]
    public void RenderJson_ProducesValidSchemaWithExpectedVersion()
    {
        var root = new Command("demo", "Demo CLI");
        root.Subcommands.Add(new Command("run", "Run something"));

        var json = CliDocExporter.RenderJson(root);

        using var doc = JsonDocument.Parse(json);
        Assert.AreEqual("1.0", doc.RootElement.GetProperty("schemaVersion").GetString());
        Assert.AreEqual(CliDocExporter.Generator, doc.RootElement.GetProperty("generator").GetString());
        Assert.AreEqual(2, doc.RootElement.GetProperty("commands").GetArrayLength());
    }

    [TestMethod]
    public void RenderJson_ExcludesSpecifiedSubcommand()
    {
        var root = new Command("demo", "Demo CLI");
        var visible = new Command("visible", "Visible");
        var hidden = new Command("hidden", "Hidden");
        root.Subcommands.Add(visible);
        root.Subcommands.Add(hidden);

        var json = CliDocExporter.RenderJson(root, exclude: hidden);

        using var doc = JsonDocument.Parse(json);
        var names = doc.RootElement.GetProperty("commands").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString()).ToList();
        CollectionAssert.Contains(names, "visible");
        CollectionAssert.DoesNotContain(names, "hidden");
    }

    [TestMethod]
    public void RenderJson_RoundTripsThroughDeserialization()
    {
        var root = new Command("demo", "Demo CLI");
        root.Options.Add(new Option<bool>("--verbose", "-v") { Description = "Verbose" });
        var sub = new Command("sub", "A subcommand");
        sub.Arguments.Add(new Argument<string>("input") { Description = "Input file" });
        root.Subcommands.Add(sub);

        var json = CliDocExporter.RenderJson(root);

        var document = JsonSerializer.Deserialize<CommandsOutput>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.IsNotNull(document);
        Assert.AreEqual("1.0", document.SchemaVersion);
        Assert.AreEqual(2, document.Commands.Count);

        var rootDoc = document.Commands.Single(c => c.IsRoot);
        Assert.AreEqual("demo", rootDoc.Name);
        Assert.AreEqual(1, rootDoc.Options.Count);
        Assert.AreEqual("--verbose", rootDoc.Options[0].Name);

        var subDoc = document.Commands.Single(c => c.Name == "sub");
        Assert.AreEqual(1, subDoc.Arguments.Count);
        Assert.AreEqual("input", subDoc.Arguments[0].Name);
    }

    [TestMethod]
    public void Export_WritesFileToDisk()
    {
        var root = new Command("demo", "Demo CLI");
        var tempPath = Path.Combine(Path.GetTempPath(), $"clidoc-export-{Guid.NewGuid():N}.json");

        try
        {
            CliDocExporter.Export(root, tempPath);
            Assert.IsTrue(File.Exists(tempPath));
            var content = File.ReadAllText(tempPath);
            StringAssert.Contains(content, "\"schemaVersion\": \"1.0\"");
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [TestMethod]
    public void Export_CreatesMissingDirectories()
    {
        var root = new Command("demo", "Demo CLI");
        var tempDir = Path.Combine(Path.GetTempPath(), $"clidoc-{Guid.NewGuid():N}", "nested");
        var tempPath = Path.Combine(tempDir, "out.json");

        try
        {
            CliDocExporter.Export(root, tempPath);
            Assert.IsTrue(File.Exists(tempPath));
        }
        finally
        {
            if (Directory.Exists(Path.GetDirectoryName(tempDir)!))
                Directory.Delete(Path.GetDirectoryName(tempDir)!, recursive: true);
        }
    }
}
