using System.CommandLine;
using CliDoc.Extraction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliDoc.Tests.Extraction;

[TestClass]
public class CommandExtractorTests
{
    [TestMethod]
    public void Extract_SimpleRootCommand_ReturnsCorrectStructure()
    {
        // Arrange — use Command instead of RootCommand since Name is read-only
        var rootCommand = new Command("testcli", "Test CLI");
        
        var extractor = new CommandExtractor();

        // Act
        var result = extractor.Extract(rootCommand);

        // Assert
        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.AreEqual("testcli", cmd.Id);
        Assert.AreEqual("testcli", cmd.Name);
        Assert.AreEqual("testcli", cmd.FullName);
        Assert.AreEqual("Test CLI", cmd.Description);
        Assert.IsTrue(cmd.IsRoot);
        Assert.IsFalse(cmd.IsGroup);
        Assert.AreEqual(0, cmd.Depth);
        Assert.IsNull(cmd.ParentId);
        Assert.AreEqual(0, cmd.Children.Count);
    }

    [TestMethod]
    public void Extract_CommandWithOptions_ExtractsOptions()
    {
        // Arrange
        var rootCommand = new Command("testcli", "Test CLI");
        
        var verboseOption = new Option<bool>("--verbose", "-v")
        {
            Description = "Enable verbose output"
        };
        
        var outputOption = new Option<string>("--output", "-o")
        {
            Description = "Output file path",
            Required = true
        };

        rootCommand.Options.Add(verboseOption);
        rootCommand.Options.Add(outputOption);

        var extractor = new CommandExtractor();

        // Act
        var result = extractor.Extract(rootCommand);

        // Assert
        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.AreEqual(2, cmd.Options.Count);

        var verbose = cmd.Options.FirstOrDefault(o => o.Name == "--verbose");
        Assert.IsNotNull(verbose);
        Assert.AreEqual("-v", verbose.ShortName);
        Assert.AreEqual("boolean", verbose.ValueType);
        Assert.IsFalse(verbose.IsRequired);

        var output = cmd.Options.FirstOrDefault(o => o.Name == "--output");
        Assert.IsNotNull(output);
        Assert.AreEqual("-o", output.ShortName);
        Assert.AreEqual("string", output.ValueType);
        Assert.IsTrue(output.IsRequired);
    }

    [TestMethod]
    public void Extract_CommandWithArguments_ExtractsArguments()
    {
        // Arrange
        var rootCommand = new Command("testcli", "Test CLI");
        
        var fileArgument = new Argument<string>("file")
        {
            Description = "Input file"
        };

        rootCommand.Arguments.Add(fileArgument);

        var extractor = new CommandExtractor();

        // Act
        var result = extractor.Extract(rootCommand);

        // Assert
        Assert.AreEqual(1, result.Count);
        var cmd = result[0];
        Assert.AreEqual(1, cmd.Arguments.Count);
        Assert.AreEqual("file", cmd.Arguments[0].Name);
        Assert.AreEqual("Input file", cmd.Arguments[0].Description);
    }

    [TestMethod]
    public void Extract_NestedCommands_ExtractsHierarchy()
    {
        // Arrange
        var rootCommand = new Command("testcli", "Test CLI");

        var authCommand = new Command("auth", "Authentication commands");
        var loginCommand = new Command("login", "Login to the service");
        var logoutCommand = new Command("logout", "Logout from the service");

        authCommand.Subcommands.Add(loginCommand);
        authCommand.Subcommands.Add(logoutCommand);
        rootCommand.Subcommands.Add(authCommand);

        var extractor = new CommandExtractor();

        // Act
        var result = extractor.Extract(rootCommand);

        // Assert
        Assert.AreEqual(4, result.Count); // root + auth + login + logout

        var root = result.FirstOrDefault(c => c.Id == "testcli");
        Assert.IsNotNull(root);
        Assert.IsTrue(root.IsGroup);
        Assert.AreEqual(1, root.Children.Count);
        Assert.AreEqual("testcli-auth", root.Children[0]);

        var auth = result.FirstOrDefault(c => c.Id == "testcli-auth");
        Assert.IsNotNull(auth);
        Assert.AreEqual("testcli", auth.ParentId);
        Assert.AreEqual(1, auth.Depth);
        Assert.IsTrue(auth.IsGroup);
        Assert.AreEqual(2, auth.Children.Count);

        var login = result.FirstOrDefault(c => c.Id == "testcli-auth-login");
        Assert.IsNotNull(login);
        Assert.AreEqual("testcli-auth", login.ParentId);
        Assert.AreEqual(2, login.Depth);
        Assert.AreEqual("testcli auth login", login.FullName);
    }

    [TestMethod]
    public void Extract_ValueTypes_DetectsCorrectTypes()
    {
        // Arrange
        var rootCommand = new Command("testcli", "Test CLI");
        
        rootCommand.Options.Add(new Option<bool>("--bool"));
        rootCommand.Options.Add(new Option<int>("--int"));
        rootCommand.Options.Add(new Option<string>("--string"));
        rootCommand.Options.Add(new Option<FileInfo>("--file"));

        var extractor = new CommandExtractor();

        // Act
        var result = extractor.Extract(rootCommand);

        // Assert
        var cmd = result[0];
        Assert.AreEqual("boolean", cmd.Options.First(o => o.Name == "--bool").ValueType);
        Assert.AreEqual("number", cmd.Options.First(o => o.Name == "--int").ValueType);
        Assert.AreEqual("string", cmd.Options.First(o => o.Name == "--string").ValueType);
        Assert.AreEqual("path", cmd.Options.First(o => o.Name == "--file").ValueType);
    }
}
