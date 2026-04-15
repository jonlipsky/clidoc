using System.Reflection;
using System.Runtime.Loader;

namespace CliDoc.Loading;

/// <summary>
/// Custom AssemblyLoadContext that resolves dependencies from the target assembly's directory,
/// its .deps.json, and the .NET shared framework directories.
/// </summary>
public class CliDocAssemblyLoadContext : AssemblyLoadContext
{
    private readonly string _basePath;
    private readonly AssemblyDependencyResolver? _resolver;
    private readonly string[] _sharedFrameworkPaths;

    public CliDocAssemblyLoadContext(string mainAssemblyPath)
        : base($"CliDoc_{Path.GetFileName(mainAssemblyPath)}")
    {
        _basePath = Path.GetDirectoryName(mainAssemblyPath)!;

        // Use the runtime's built-in dependency resolver which reads .deps.json
        try
        {
            _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
        }
        catch
        {
            _resolver = null;
        }

        // Discover shared framework paths for resolving framework assemblies
        _sharedFrameworkPaths = DiscoverSharedFrameworkPaths();
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try the deps.json-based resolver
        if (_resolver != null)
        {
            var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (resolvedPath != null)
            {
                return LoadFromAssemblyPath(resolvedPath);
            }
        }

        // Probe the assembly's directory
        var candidatePath = Path.Combine(_basePath, $"{assemblyName.Name}.dll");
        if (File.Exists(candidatePath))
        {
            return LoadFromAssemblyPath(candidatePath);
        }

        // Probe shared framework directories (for FrameworkReference assemblies)
        foreach (var frameworkPath in _sharedFrameworkPaths)
        {
            candidatePath = Path.Combine(frameworkPath, $"{assemblyName.Name}.dll");
            if (File.Exists(candidatePath))
            {
                return LoadFromAssemblyPath(candidatePath);
            }
        }

        // Let the default context try
        return null;
    }

    protected override nint LoadUnmanagedDll(string unmanagedDllName)
    {
        if (_resolver != null)
        {
            var resolvedPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (resolvedPath != null)
            {
                return LoadUnmanagedDllFromPath(resolvedPath);
            }
        }

        return nint.Zero;
    }

    public static string[] GetSharedFrameworkPaths()
    {
        return DiscoverSharedFrameworkPaths();
    }

    private static string[] DiscoverSharedFrameworkPaths()
    {
        var paths = new List<string>();

        // RuntimeEnvironment.GetRuntimeDirectory() gives us the base runtime path
        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        if (Directory.Exists(runtimeDir))
        {
            paths.Add(runtimeDir);
        }

        // Also look for ASP.NET Core and other shared frameworks
        // The runtime dir is like: /usr/local/share/dotnet/shared/Microsoft.NETCore.App/10.0.x/
        // We want to also check:   /usr/local/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.x/
        var runtimeVersion = Path.GetFileName(runtimeDir.TrimEnd(Path.DirectorySeparatorChar));
        var sharedRoot = Path.GetDirectoryName(Path.GetDirectoryName(runtimeDir.TrimEnd(Path.DirectorySeparatorChar)));

        if (sharedRoot != null && Directory.Exists(sharedRoot))
        {
            foreach (var frameworkDir in Directory.GetDirectories(sharedRoot))
            {
                var versionedDir = Path.Combine(frameworkDir, runtimeVersion);
                if (Directory.Exists(versionedDir) && !paths.Contains(versionedDir))
                {
                    paths.Add(versionedDir);
                }

                // Also try matching major.minor version if exact match doesn't exist
                if (!Directory.Exists(versionedDir))
                {
                    var majorMinor = string.Join('.', runtimeVersion.Split('.').Take(2));
                    try
                    {
                        var candidates = Directory.GetDirectories(frameworkDir)
                            .Where(d => Path.GetFileName(d).StartsWith(majorMinor))
                            .OrderByDescending(d => d);
                        foreach (var candidate in candidates)
                        {
                            if (!paths.Contains(candidate))
                            {
                                paths.Add(candidate);
                                break;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore directory enumeration errors
                    }
                }
            }
        }

        return paths.ToArray();
    }
}
