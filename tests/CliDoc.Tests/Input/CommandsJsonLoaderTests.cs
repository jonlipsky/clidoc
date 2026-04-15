using CliDoc.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CliDoc.Tests.Input;

[TestClass]
public class CommandsJsonLoaderTests
{
    private static string MinimalValidJson => """
        {
          "schemaVersion": "1.0",
          "generatedAt": "2026-01-01T00:00:00Z",
          "generator": "test",
          "commands": [
            {
              "id": "demo",
              "name": "demo",
              "fullName": "demo",
              "description": "Demo",
              "isGroup": false,
              "isRoot": true,
              "depth": 0,
              "arguments": [],
              "options": [],
              "children": []
            }
          ]
        }
        """;

    [TestMethod]
    public void LoadFromString_ValidDocument_Succeeds()
    {
        var loader = new CommandsJsonLoader();

        var document = loader.LoadFromString(MinimalValidJson);

        Assert.AreEqual("1.0", document.SchemaVersion);
        Assert.AreEqual(1, document.Commands.Count);
        Assert.AreEqual("demo", document.Commands[0].Name);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public void LoadFromString_MissingSchemaVersion_Throws()
    {
        var json = MinimalValidJson.Replace("\"schemaVersion\": \"1.0\",", "");
        var loader = new CommandsJsonLoader();

        loader.LoadFromString(json);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public void LoadFromString_IncompatibleMajor_Throws()
    {
        var json = MinimalValidJson.Replace("\"schemaVersion\": \"1.0\"", "\"schemaVersion\": \"2.0\"");
        var loader = new CommandsJsonLoader();

        loader.LoadFromString(json);
    }

    [TestMethod]
    public void LoadFromString_CompatibleMinor_Succeeds()
    {
        var json = MinimalValidJson.Replace("\"schemaVersion\": \"1.0\"", "\"schemaVersion\": \"1.5\"");
        var loader = new CommandsJsonLoader();

        var document = loader.LoadFromString(json);

        Assert.AreEqual("1.5", document.SchemaVersion);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidDataException))]
    public void LoadFromString_Malformed_Throws()
    {
        var loader = new CommandsJsonLoader();
        loader.LoadFromString("{ not valid json");
    }

    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public void Load_MissingFile_Throws()
    {
        var loader = new CommandsJsonLoader();
        loader.Load("/tmp/definitely-does-not-exist-" + Guid.NewGuid().ToString("N"));
    }

    [TestMethod]
    public void Load_RoundTripsFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"commands-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, MinimalValidJson);

        try
        {
            var loader = new CommandsJsonLoader();
            var document = loader.Load(path);
            Assert.AreEqual(1, document.Commands.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
