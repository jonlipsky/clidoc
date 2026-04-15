using Clidoc.SystemCommandLine.Schema;
using CliDoc.Merging;
using CliDoc.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliDoc.Tests.Merging;

[TestClass]
public class CommandMergerTests
{
    private static OutputCommand MakeRoot(string name = "testcli", string description = "Test CLI") => new()
    {
        Id = name,
        Name = name,
        FullName = name,
        Description = description,
        IsGroup = false,
        IsRoot = true,
        Depth = 0,
        ParentId = null,
        Options = new List<OutputOption>(),
        Arguments = new List<OutputArgument>(),
        Children = new List<string>()
    };

    [TestMethod]
    public void Merge_NoMetadata_PreservesExtractedData()
    {
        var root = MakeRoot() with
        {
            Options = new List<OutputOption>
            {
                new()
                {
                    Name = "--verbose",
                    Description = "Verbose output",
                    ShortName = "-v",
                    ValueType = "boolean",
                    IsRequired = false,
                    DefaultValue = null,
                    AllowedValues = null
                }
            }
        };

        var merger = new CommandMerger();
        var result = merger.Merge(new List<OutputCommand> { root }, null);

        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.AreEqual("testcli", cmd.Id);
        Assert.AreEqual("Test CLI", cmd.Description);
        Assert.AreEqual(1, cmd.Options.Count);
        Assert.AreEqual("--verbose", cmd.Options[0].Name);
        Assert.IsNotNull(cmd.Examples);
        Assert.AreEqual(0, cmd.Examples.Count);
        Assert.IsNotNull(cmd.Sections);
        Assert.AreEqual(0, cmd.Sections.Count);
    }

    [TestMethod]
    public void Merge_WithTagline_OverridesDescription()
    {
        var root = MakeRoot(description: "Original description");
        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                ["testcli"] = new CommandMetadata { Tagline = "Custom tagline" }
            }
        };

        var merger = new CommandMerger();
        var result = merger.Merge(new List<OutputCommand> { root }, metadata);

        Assert.AreEqual("Custom tagline", result[0].Description);
    }

    [TestMethod]
    public void Merge_WithExamples_AddsExamples()
    {
        var cmd = new OutputCommand
        {
            Id = "testcli-run",
            Name = "run",
            FullName = "testcli run",
            Description = "Run command",
            IsGroup = false,
            IsRoot = false,
            Depth = 1,
            ParentId = "testcli",
            Options = new List<OutputOption>(),
            Arguments = new List<OutputArgument>(),
            Children = new List<string>()
        };

        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                ["testcli run"] = new CommandMetadata
                {
                    Examples = new List<Example>
                    {
                        new() { Description = "Run with defaults", Command = "testcli run" },
                        new() { Description = "Run with config", Command = "testcli run --config app.yaml" }
                    }
                }
            }
        };

        var merger = new CommandMerger();
        var result = merger.Merge(new List<OutputCommand> { cmd }, metadata);

        Assert.AreEqual(1, result.Count);
        Assert.IsNotNull(result[0].Examples);
        Assert.AreEqual(2, result[0].Examples!.Count);
        Assert.AreEqual("Run with defaults", result[0].Examples![0].Description);
        Assert.AreEqual("testcli run", result[0].Examples![0].Command);
    }

    [TestMethod]
    public void Merge_WithSections_AddsSections()
    {
        var root = MakeRoot();
        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                ["testcli"] = new CommandMetadata
                {
                    Sections = new List<Section>
                    {
                        new() { Title = "Getting Started", Body = "Install and run..." }
                    }
                }
            }
        };

        var merger = new CommandMerger();
        var result = merger.Merge(new List<OutputCommand> { root }, metadata);

        Assert.AreEqual(1, result.Count);
        Assert.IsNotNull(result[0].Sections);
        Assert.AreEqual(1, result[0].Sections!.Count);
        Assert.AreEqual("Getting Started", result[0].Sections![0].Title);
        Assert.AreEqual("Install and run...", result[0].Sections![0].Body);
    }

    [TestMethod]
    public void Merge_PreservesStructure_FromExtraction()
    {
        var root = MakeRoot(description: "Root") with
        {
            IsGroup = true,
            Children = new List<string> { "testcli-sub" }
        };

        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                ["testcli"] = new CommandMetadata { Tagline = "Custom description" }
            }
        };

        var merger = new CommandMerger();
        var result = merger.Merge(new List<OutputCommand> { root }, metadata);

        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.IsTrue(cmd.IsGroup);
        Assert.AreEqual(1, cmd.Children.Count);
        Assert.AreEqual("testcli-sub", cmd.Children[0]);
    }

    [TestMethod]
    public void Merge_ExamplesFromSourceCommand_AreRetainedWhenNoMetadata()
    {
        var cmd = MakeRoot() with
        {
            Examples = new List<OutputExample>
            {
                new() { Description = "Source-embedded", Command = "testcli --help" }
            }
        };

        var merger = new CommandMerger();
        var result = merger.Merge(new List<OutputCommand> { cmd }, null);

        Assert.AreEqual(1, result[0].Examples!.Count);
        Assert.AreEqual("Source-embedded", result[0].Examples![0].Description);
    }
}
