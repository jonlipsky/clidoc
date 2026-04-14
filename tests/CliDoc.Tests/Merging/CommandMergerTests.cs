using CliDoc.Extraction;
using CliDoc.Metadata;
using CliDoc.Merging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliDoc.Tests.Merging;

[TestClass]
public class CommandMergerTests
{
    [TestMethod]
    public void Merge_NoMetadata_PreservesExtractedData()
    {
        // Arrange
        var extracted = new List<ExtractedCommand>
        {
            new ExtractedCommand
            {
                Id = "testcli",
                Name = "testcli",
                FullName = "testcli",
                Description = "Test CLI",
                IsGroup = false,
                IsRoot = true,
                Depth = 0,
                ParentId = null,
                Options = new List<ExtractedOption>
                {
                    new ExtractedOption
                    {
                        Name = "--verbose",
                        Description = "Verbose output",
                        ShortName = "-v",
                        ValueType = "boolean",
                        IsRequired = false,
                        DefaultValue = null,
                        AllowedValues = null
                    }
                },
                Arguments = new List<ExtractedArgument>(),
                Children = new List<string>()
            }
        };

        var merger = new CommandMerger();

        // Act
        var result = merger.Merge(extracted, null);

        // Assert
        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.AreEqual("testcli", cmd.Id);
        Assert.AreEqual("Test CLI", cmd.Description);
        Assert.AreEqual(1, cmd.Options.Count);
        Assert.AreEqual("--verbose", cmd.Options[0].Name);
        Assert.AreEqual(0, cmd.Examples.Count);
        Assert.AreEqual(0, cmd.Sections.Count);
    }

    [TestMethod]
    public void Merge_WithTagline_OverridesDescription()
    {
        // Arrange
        var extracted = new List<ExtractedCommand>
        {
            new ExtractedCommand
            {
                Id = "testcli",
                Name = "testcli",
                FullName = "testcli",
                Description = "Original description",
                IsGroup = false,
                IsRoot = true,
                Depth = 0,
                ParentId = null,
                Options = new List<ExtractedOption>(),
                Arguments = new List<ExtractedArgument>(),
                Children = new List<string>()
            }
        };

        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                {
                    "testcli", new CommandMetadata
                    {
                        Tagline = "Custom tagline"
                    }
                }
            }
        };

        var merger = new CommandMerger();

        // Act
        var result = merger.Merge(extracted, metadata);

        // Assert
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("Custom tagline", result[0].Description);
    }

    [TestMethod]
    public void Merge_WithExamples_AddsExamples()
    {
        // Arrange
        var extracted = new List<ExtractedCommand>
        {
            new ExtractedCommand
            {
                Id = "testcli-run",
                Name = "run",
                FullName = "testcli run",
                Description = "Run command",
                IsGroup = false,
                IsRoot = false,
                Depth = 1,
                ParentId = "testcli",
                Options = new List<ExtractedOption>(),
                Arguments = new List<ExtractedArgument>(),
                Children = new List<string>()
            }
        };

        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                {
                    "testcli run", new CommandMetadata
                    {
                        Examples = new List<Example>
                        {
                            new Example
                            {
                                Description = "Run with defaults",
                                Command = "testcli run"
                            },
                            new Example
                            {
                                Description = "Run with config",
                                Command = "testcli run --config app.yaml"
                            }
                        }
                    }
                }
            }
        };

        var merger = new CommandMerger();

        // Act
        var result = merger.Merge(extracted, metadata);

        // Assert
        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.AreEqual(2, cmd.Examples.Count);
        Assert.AreEqual("Run with defaults", cmd.Examples[0].Description);
        Assert.AreEqual("testcli run", cmd.Examples[0].Command);
    }

    [TestMethod]
    public void Merge_WithSections_AddsSections()
    {
        // Arrange
        var extracted = new List<ExtractedCommand>
        {
            new ExtractedCommand
            {
                Id = "testcli",
                Name = "testcli",
                FullName = "testcli",
                Description = "Test CLI",
                IsGroup = false,
                IsRoot = true,
                Depth = 0,
                ParentId = null,
                Options = new List<ExtractedOption>(),
                Arguments = new List<ExtractedArgument>(),
                Children = new List<string>()
            }
        };

        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                {
                    "testcli", new CommandMetadata
                    {
                        Sections = new List<Section>
                        {
                            new Section
                            {
                                Title = "Getting Started",
                                Body = "Install and run..."
                            }
                        }
                    }
                }
            }
        };

        var merger = new CommandMerger();

        // Act
        var result = merger.Merge(extracted, metadata);

        // Assert
        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.AreEqual(1, cmd.Sections.Count);
        Assert.AreEqual("Getting Started", cmd.Sections[0].Title);
        Assert.AreEqual("Install and run...", cmd.Sections[0].Body);
    }

    [TestMethod]
    public void Merge_PreservesStructure_FromExtraction()
    {
        // Arrange
        var extracted = new List<ExtractedCommand>
        {
            new ExtractedCommand
            {
                Id = "testcli",
                Name = "testcli",
                FullName = "testcli",
                Description = "Root",
                IsGroup = true,
                IsRoot = true,
                Depth = 0,
                ParentId = null,
                Options = new List<ExtractedOption>(),
                Arguments = new List<ExtractedArgument>(),
                Children = new List<string> { "testcli-sub" }
            }
        };

        // Metadata can't change structure
        var metadata = new MetadataFile
        {
            Commands = new Dictionary<string, CommandMetadata>
            {
                {
                    "testcli", new CommandMetadata
                    {
                        Tagline = "Custom description"
                    }
                }
            }
        };

        var merger = new CommandMerger();

        // Act
        var result = merger.Merge(extracted, metadata);

        // Assert
        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.IsTrue(cmd.IsGroup);
        Assert.AreEqual(1, cmd.Children.Count);
        Assert.AreEqual("testcli-sub", cmd.Children[0]);
    }
}
