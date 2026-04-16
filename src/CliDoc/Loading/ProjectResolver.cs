using System.Diagnostics;
using System.Text.RegularExpressions;

namespace CliDoc.Loading;

/// <summary>
/// Result of building and resolving a project.
/// </summary>
public record ProjectBuildResult(string AssemblyPath, string? ToolCommandName);

/// <summary>
/// Resolves a .csproj file to a built assembly path by running dotnet build
/// and discovering the output DLL.
/// </summary>
public class ProjectResolver
{
    /// <summary>
    /// Builds the project and returns the path to the output assembly
    /// along with metadata extracted from the csproj.
    /// </summary>
    public ProjectBuildResult BuildAndResolve(string projectPath, string configuration = "Release")
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

        var csprojContent = File.ReadAllText(fullProjectPath);
        var toolCommandName = ExtractProperty(csprojContent, "ToolCommandName")
                           ?? ExtractProperty(csprojContent, "AssemblyName")
                           ?? Path.GetFileNameWithoutExtension(fullProjectPath);

        var assemblyPath = ResolveOutputAssembly(fullProjectPath, configuration, csprojContent);
        Console.WriteLine($"Resolved assembly: {assemblyPath}");

        return new ProjectBuildResult(assemblyPath, toolCommandName);
    }

    private static string ResolveOutputAssembly(string projectPath, string configuration, string csprojContent)
    {
        var projectDir = Path.GetDirectoryName(projectPath)!;
        var projectName = Path.GetFileNameWithoutExtension(projectPath);

        var assemblyName = ExtractProperty(csprojContent, "AssemblyName") ?? projectName;
        var targetFramework = ExtractProperty(csprojContent, "TargetFramework");
        var targetFrameworks = ExtractProperty(csprojContent, "TargetFrameworks");

        if (string.IsNullOrEmpty(targetFramework) && !string.IsNullOrEmpty(targetFrameworks))
        {
            targetFramework = targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries).First().Trim();
        }

        if (string.IsNullOrEmpty(targetFramework))
        {
            throw new InvalidOperationException(
                "Could not determine TargetFramework from the .csproj file");
        }

        var outputPath = Path.Combine(projectDir, "bin", configuration, targetFramework, $"{assemblyName}.dll");

        if (!File.Exists(outputPath))
        {
            throw new FileNotFoundException(
                $"Built assembly not found at expected path: {outputPath}. " +
                "Ensure the project builds successfully.");
        }

        return outputPath;
    }

    private static string? ExtractProperty(string csprojContent, string propertyName)
    {
        var match = Regex.Match(csprojContent, $"<{propertyName}>([^<]+)</{propertyName}>");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
}
