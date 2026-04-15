using System.CommandLine;
using System.Text.Json;
using Clidoc.SystemCommandLine;
using Clidoc.SystemCommandLine.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clidoc.SystemCommandLine.Tests;

[TestClass]
public class CommandExtensionsTests
{
    [TestMethod]
    public void AddCommandsSubcommand_AddsCommandWithDefaultName()
    {
        var root = new Command("demo", "Demo CLI");

        var added = root.AddCommandsSubcommand();

        Assert.IsNotNull(added);
        Assert.AreEqual("commands", added.Name);
        Assert.IsTrue(root.Subcommands.Contains(added));
    }

    [TestMethod]
    public void AddCommandsSubcommand_HonoursCustomName()
    {
        var root = new Command("demo", "Demo CLI");

        var added = root.AddCommandsSubcommand(name: "clidoc-export");

        Assert.AreEqual("clidoc-export", added.Name);
    }

    [TestMethod]
    public async Task AddCommandsSubcommand_WritesJsonToOutputFile()
    {
        var root = new Command("demo", "Demo CLI");
        root.Subcommands.Add(new Command("run", "Run the thing"));
        root.AddCommandsSubcommand();

        var tempPath = Path.Combine(Path.GetTempPath(), $"clidoc-ext-{Guid.NewGuid():N}.json");
        try
        {
            var parseResult = root.Parse(["commands", "--output", tempPath]);
            var exitCode = await parseResult.InvokeAsync();

            Assert.AreEqual(0, exitCode);
            Assert.IsTrue(File.Exists(tempPath));

            var document = JsonSerializer.Deserialize<CommandsOutput>(
                File.ReadAllText(tempPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.IsNotNull(document);
            Assert.AreEqual("1.0", document.SchemaVersion);

            // The exporter subcommand should not appear in the output
            Assert.IsFalse(document.Commands.Any(c => c.Name == "commands"));
            Assert.IsTrue(document.Commands.Any(c => c.Name == "run"));
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
