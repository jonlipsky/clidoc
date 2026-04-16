using CliDoc.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliDoc.Tests.Metadata;

[TestClass]
public class MetadataLoaderTests
{
    [TestMethod]
    public void LoadFromString_ValidYaml_ParsesCorrectly()
    {
        // Arrange
        var yaml = @"
site:
  title: My CLI Tool
  tagline: A great CLI
  baseUrl: https://example.com
  theme:
    accentColor: '#6366f1'

commands:
  'mycli':
    tagline: Root command description
  'mycli auth login':
    examples:
      - description: Login with browser
        command: mycli auth login
      - description: Login with token
        command: mycli auth login --token abc123
    sections:
      - title: Authentication
        body: Details about auth...
";

        var loader = new MetadataLoader();

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Site);
        Assert.AreEqual("My CLI Tool", result.Site.Title);
        Assert.AreEqual("A great CLI", result.Site.Tagline);
        Assert.AreEqual("https://example.com", result.Site.BaseUrl);
        Assert.AreEqual("#6366f1", result.Site.Theme?.AccentColor);

        Assert.IsNotNull(result.Commands);
        Assert.IsTrue(result.Commands.ContainsKey("mycli"));
        Assert.AreEqual("Root command description", result.Commands["mycli"].Tagline);

        Assert.IsTrue(result.Commands.ContainsKey("mycli auth login"));
        var loginCmd = result.Commands["mycli auth login"];
        Assert.IsNotNull(loginCmd.Examples);
        Assert.AreEqual(2, loginCmd.Examples.Count);
        Assert.AreEqual("Login with browser", loginCmd.Examples[0].Description);
        Assert.AreEqual("mycli auth login", loginCmd.Examples[0].Command);

        Assert.IsNotNull(loginCmd.Sections);
        Assert.AreEqual(1, loginCmd.Sections.Count);
        Assert.AreEqual("Authentication", loginCmd.Sections[0].Title);
    }

    [TestMethod]
    public void LoadFromString_EmptyYaml_ReturnsEmptyMetadata()
    {
        // Arrange
        var yaml = "";
        var loader = new MetadataLoader();

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void LoadFromString_OnlyCommands_ParsesCommands()
    {
        // Arrange
        var yaml = @"
commands:
  'test':
    tagline: Test command
    examples:
      - description: Run test
        command: test run
";

        var loader = new MetadataLoader();

        // Act
        var result = loader.LoadFromString(yaml);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Commands);
        Assert.IsTrue(result.Commands.ContainsKey("test"));
    }

    [TestMethod]
    public void LoadFromString_InvalidYaml_ThrowsException()
    {
        // Arrange
        var yaml = @"
site:
  title: 'Unclosed quote
";

        var loader = new MetadataLoader();

        // Act + Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => loader.LoadFromString(yaml));
    }

    [TestMethod]
    public void Load_NonExistentFile_ReturnsNull()
    {
        // Arrange
        var loader = new MetadataLoader();

        // Act
        var result = loader.Load("/non/existent/file.yaml");

        // Assert
        Assert.IsNull(result);
    }
}
