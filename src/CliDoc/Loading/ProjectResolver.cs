using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CliDoc.Loading;

/// <summary>
/// Resolves a .csproj file to a built assembly path by running dotnet build
/// and discovering the output DLL.
/// </summary>
public class ProjectResolver
{
    /// <summary>
    /// Builds the project and returns the path to the output assembly.
    /// </summary>
    public string BuildAndResolve(string projectPath, string configuration = "Release")
    {
        var fullProjectPath = Path.GetFullPath(projectPath);

        if (!File.Exists(fullProjectPath))
        {
            throw new FileNotFoundException($"Project file not found: {fullProjectPath}");
        }

        if (!fullProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Expected a .csproj file, got: {fullProjectPath}");
        }

        Console.WriteLine($"Building project: {fullProjectPath}");

        // Run dotnet build
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{fullProjectPath}\" -c {configuration} --nologo -v q",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build");

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"dotnet build failed (exit code {process.ExitCode}):\n{stderr}\n{stdout}");
        }

        Console.WriteLine("Build succeeded");

        // Discover the output assembly path
        return ResolveOutputAssembly(fullProjectPath, configuration);
    }

    private string ResolveOutputAssembly(string projectPath, string configuration)
    {
        var projectDir = Path.GetDirectoryName(projectPath)!;
        var projectName = Path.GetFileNameWithoutExtension(projectPath);

        // Parse the csproj to find AssemblyName and TargetFramework
        var csprojContent = File.ReadAllText(projectPath);

        var assemblyName = ExtractProperty(csprojContent, "AssemblyName") ?? projectName;
        var targetFramework = ExtractProperty(csprojContent, "TargetFramework");
        var targetFrameworks = ExtractProperty(csprojContent, "TargetFrameworks");

        // If multi-targeting, pick the first framework
        if (string.IsNullOrEmpty(targetFramework) && !string.IsNullOrEmpty(targetFrameworks))
        {
            targetFramework = targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        }

        if (string.IsNullOrEmpty(targetFramework))
        {
            throw new InvalidOperationException(
                "Could not determine TargetFramework from the .csproj file");
        }

        // Standard output path: bin/{Configuration}/{TFM}/{AssemblyName}.dll
        var outputPath = Path.Combine(projectDir, "bin", configuration, targetFramework, $"{assemblyName}.dll");

        if (!File.Exists(outputPath))
        {
            throw new FileNotFoundException(
                $"Built assembly not found at expected path: {outputPath}. " +
                "Ensure the project builds successfully.");
        }

        Console.WriteLine($"Resolved assembly: {outputPath}");
        return outputPath;
    }

    private static string? ExtractProperty(string csprojContent, string propertyName)
    {
        var match = Regex.Match(csprojContent, $"<{propertyName}>([^<]+)</{propertyName}>");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
