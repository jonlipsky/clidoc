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
        var document = CommandsJsonLoader.LoadFromString(MinimalValidJson);

        Assert.AreEqual("1.0", document.SchemaVersion);
        Assert.AreEqual(1, document.Commands.Count);
        Assert.AreEqual("demo", document.Commands[0].Name);
    }

    [TestMethod]
    public void LoadFromString_MissingSchemaVersion_Throws()
    {
        var json = MinimalValidJson.Replace("\"schemaVersion\": \"1.0\",", "");

        Assert.ThrowsExactly<InvalidDataException>(() => CommandsJsonLoader.LoadFromString(json));
    }

    [TestMethod]
    public void LoadFromString_IncompatibleMajor_Throws()
    {
        var json = MinimalValidJson.Replace("\"schemaVersion\": \"1.0\"", "\"schemaVersion\": \"2.0\"");

        Assert.ThrowsExactly<InvalidDataException>(() => CommandsJsonLoader.LoadFromString(json));
    }

    [TestMethod]
    public void LoadFromString_CompatibleMinor_Succeeds()
    {
        var json = MinimalValidJson.Replace("\"schemaVersion\": \"1.0\"", "\"schemaVersion\": \"1.5\"");

        var document = CommandsJsonLoader.LoadFromString(json);

        Assert.AreEqual("1.5", document.SchemaVersion);
    }

    [TestMethod]
    public void LoadFromString_Malformed_Throws()
    {
        Assert.ThrowsExactly<InvalidDataException>(() => CommandsJsonLoader.LoadFromString("{ not valid json"));
    }

    [TestMethod]
    public void Load_MissingFile_Throws()
    {
        var path = "/tmp/definitely-does-not-exist-" + Guid.NewGuid().ToString("N");
        Assert.ThrowsExactly<FileNotFoundException>(() => CommandsJsonLoader.Load(path));
    }

    [TestMethod]
    public void Load_RoundTripsFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"commands-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, MinimalValidJson);

        try
        {
            var document = CommandsJsonLoader.Load(path);
            Assert.AreEqual(1, document.Commands.Count);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
