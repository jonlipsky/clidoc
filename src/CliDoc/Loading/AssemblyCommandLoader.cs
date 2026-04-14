using System.CommandLine;
using System.Reflection;
using System.Runtime.Loader;

namespace CliDoc.Loading;

public class AssemblyCommandLoader
{
    public Command LoadCommand(string assemblyPath, string? entryTypeName = null)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
        }

        var fullPath = Path.GetFullPath(assemblyPath);
        var loadContext = new AssemblyLoadContext($"CliDoc_{Path.GetFileName(assemblyPath)}", isCollectible: true);

        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);
            return DiscoverCommand(assembly, entryTypeName);
        }
        catch (Exception ex)
        {
            loadContext.Unload();
            throw new InvalidOperationException($"Failed to load command from assembly: {ex.Message}", ex);
        }
    }

    private Command DiscoverCommand(Assembly assembly, string? entryTypeName)
    {
        // Try entry type if specified
        if (!string.IsNullOrEmpty(entryTypeName))
        {
            var type = assembly.GetType(entryTypeName);
            if (type != null)
            {
                var command = TryGetCommandFromType(type);
                if (command != null)
                    return command;
            }

            throw new InvalidOperationException($"Could not find or load command from type: {entryTypeName}");
        }

        // Scan all types for a static method returning Command or RootCommand
        foreach (var type in assembly.GetTypes())
        {
            if (!type.IsPublic)
                continue;

            var command = TryGetCommandFromType(type);
            if (command != null)
                return command;
        }

        throw new InvalidOperationException(
            "Could not discover command entry point. " +
            "Specify --entry-type or ensure your assembly has a public static method " +
            "returning RootCommand or Command (e.g., GetRootCommand, CreateRootCommand, BuildCommandLine)");
    }

    private Command? TryGetCommandFromType(Type type)
    {
        // Look for well-known method names
        string[] methodNames = { "GetRootCommand", "CreateRootCommand", "BuildCommandLine", "CreateCommand" };

        foreach (var methodName in methodNames)
        {
            var method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            if (method != null && typeof(Command).IsAssignableFrom(method.ReturnType))
            {
                try
                {
                    var command = method.Invoke(null, null) as Command;
                    if (command != null)
                        return command;
                }
                catch
                {
                    // Continue searching
                }
            }
        }

        // Look for any static method returning Command/RootCommand
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            if (typeof(Command).IsAssignableFrom(method.ReturnType) &&
                method.GetParameters().Length == 0)
            {
                try
                {
                    var command = method.Invoke(null, null) as Command;
                    if (command != null)
                        return command;
                }
                catch
                {
                    // Continue searching
                }
            }
        }

        return null;
    }
}
