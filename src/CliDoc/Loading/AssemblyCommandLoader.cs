using System.CommandLine;
using System.Reflection;
using System.Runtime.Loader;

namespace CliDoc.Loading;

public static class AssemblyCommandLoader
{
    public static Command LoadCommand(string assemblyPath, string? entryTypeName = null)
    {
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Assembly not found: {assemblyPath}");
        }

        var fullPath = Path.GetFullPath(assemblyPath);
        var loadContext = new CliDocAssemblyLoadContext(fullPath);

        try
        {
            var assembly = loadContext.LoadFromAssemblyPath(fullPath);
            return DiscoverCommand(assembly, entryTypeName);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load command from assembly: {ex.Message}", ex);
        }
    }

    private static Command DiscoverCommand(Assembly assembly, string? entryTypeName)
    {
        // Try entry type if specified
        if (!string.IsNullOrEmpty(entryTypeName))
        {
            Type? type;
            try
            {
                type = assembly.GetType(entryTypeName);
                if (type == null)
                {
                    type = assembly.GetTypes().FirstOrDefault(t => t.FullName == entryTypeName);
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                type = ex.Types.Where(t => t != null).FirstOrDefault(t => t!.FullName == entryTypeName);
                if (type == null)
                {
                    var loadErrors = ex.LoaderExceptions?.Select(e => e?.Message).Where(m => m != null);
                    throw new InvalidOperationException(
                        $"Could not load types from assembly. Loader errors:\n  " +
                        string.Join("\n  ", loadErrors ?? Array.Empty<string?>()));
                }
            }

            if (type != null)
            {
                var command = TryGetCommandFromType(type);
                if (command != null)
                    return command;
            }

            throw new InvalidOperationException($"Could not find or load command from type: {entryTypeName}");
        }

        // Scan all types (including non-public) for a static method returning Command or RootCommand
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray()!;
        }

        // Pass 1: Scan for static factory methods returning Command/RootCommand
        foreach (var type in types)
        {
            var command = TryGetCommandFromType(type);
            if (command != null)
                return command;
        }

        // Pass 2: Try constructing types that inherit from Command/RootCommand
        // This handles DI-based apps where commands are classes extending Command
        var constructedRoot = TryBuildCommandTreeFromTypes(types);
        if (constructedRoot != null)
            return constructedRoot;

        throw new InvalidOperationException(
            "Could not discover command entry point. " +
            "Specify --entry-type or ensure your assembly has a public static method " +
            "returning RootCommand or Command (e.g., GetRootCommand, CreateRootCommand, BuildCommandLine)");
    }

    private static bool IsCommandType(Type type)
    {
        var current = type;
        while (current != null)
        {
            if (current.FullName == "System.CommandLine.Command" ||
                current.FullName == "System.CommandLine.RootCommand")
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private static bool IsCommandSubclass(Type type)
    {
        // Returns true if the type directly extends Command/RootCommand (not Command itself)
        if (type.IsAbstract || type.IsInterface) return false;
        return IsCommandType(type) &&
               type.FullName != "System.CommandLine.Command" &&
               type.FullName != "System.CommandLine.RootCommand";
    }

    /// <summary>
    /// For DI-based apps: scan all Command-derived types, try to construct them
    /// with null/default parameters, and build a synthetic command tree.
    /// </summary>
    private static Command? TryBuildCommandTreeFromTypes(Type[] types)
    {
        var commandTypes = types.Where(IsCommandSubclass).ToList();
        if (commandTypes.Count == 0) return null;

        var constructed = new List<object>();
        foreach (var cmdType in commandTypes)
        {
            var instance = TryConstruct(cmdType);
            if (instance != null)
                constructed.Add(instance);
        }

        if (constructed.Count == 0) return null;

        // Find the root: look for RootCommand subclasses first
        var rootObj = constructed.FirstOrDefault(c =>
        {
            var t = c.GetType();
            while (t != null)
            {
                if (t.FullName == "System.CommandLine.RootCommand") return true;
                t = t.BaseType;
            }
            return false;
        });

        // If we found constructed commands, wrap them into a tree
        if (rootObj != null)
        {
            // Root was constructed with its subcommands wired up
            if (rootObj is Command nativeRoot)
                return nativeRoot;
            return WrapCommandFromReflection(rootObj);
        }

        // No RootCommand found — build a synthetic root from all top-level commands
        var root = new RootCommand("CLI application");
        foreach (var cmd in constructed)
        {
            if (cmd is Command nativeCmd)
            {
                root.Subcommands.Add(nativeCmd);
            }
            else
            {
                var wrapped = WrapCommandFromReflection(cmd);
                if (wrapped != null)
                    root.Subcommands.Add(wrapped);
            }
        }

        return root.Subcommands.Count > 0 ? root : null;
    }

    private static object? TryConstruct(Type type)
    {
        // Try parameterless constructor first
        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor != null)
        {
            try { return parameterlessCtor.Invoke(null); } catch { }
        }

        // Try constructors with parameters, passing null/defaults
        foreach (var ctor in type.GetConstructors()
            .OrderBy(c => c.GetParameters().Length))
        {
            try
            {
                var parameters = ctor.GetParameters();
                var args = new object?[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    args[i] = parameters[i].ParameterType.IsValueType
                        ? Activator.CreateInstance(parameters[i].ParameterType)
                        : null;
                }
                return ctor.Invoke(args);
            }
            catch
            {
                // Constructor failed, try next
            }
        }

        return null;
    }

    private static Command? TryGetCommandFromType(Type type)
    {
        // Look for well-known method names
        string[] methodNames = { "GetRootCommand", "CreateRootCommand", "BuildCommandLine", "CreateCommand" };

        foreach (var methodName in methodNames)
        {
            var method = type.GetMethod(methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);

            if (method != null)
            {
                if (IsCommandType(method.ReturnType))
                {
                    var result = TryInvokeAndWrap(method);
                    if (result != null)
                        return result;
                }
            }
        }

        // Look for any static method returning Command/RootCommand
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (IsCommandType(method.ReturnType) && method.GetParameters().Length == 0)
            {
                var result = TryInvokeAndWrap(method);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    private static Command? TryInvokeAndWrap(MethodInfo method)
    {
        try
        {
            var result = method.Invoke(null, null);
            if (result == null) return null;

            // If the result is already a Command in our ALC, use it directly
            if (result is Command command)
                return command;

            // Cross-ALC: the object won't cast to our Command type,
            // but we can use it via reflection to build our own Command tree
            return WrapCommandFromReflection(result);
        }
        catch (Exception ex)
        {
            // Log but don't fail — continue searching other methods
            var inner = ex is TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            Console.Error.WriteLine($"  Warning: {method.DeclaringType?.Name}.{method.Name} threw: {inner.Message}");
            return null;
        }
    }

    /// <summary>
    /// When loading assemblies in a separate ALC, the System.CommandLine types 
    /// might be different instances. This wraps the foreign Command object by
    /// reading its properties via reflection and building a native Command tree.
    /// </summary>
    private static Command? WrapCommandFromReflection(object foreignCommand)
    {
        var type = foreignCommand.GetType();

        // Read basic properties
        var name = type.GetProperty("Name")?.GetValue(foreignCommand) as string ?? "";
        var description = type.GetProperty("Description")?.GetValue(foreignCommand) as string ?? "";

        var isRoot = type.FullName == "System.CommandLine.RootCommand" ||
                     type.BaseType?.FullName == "System.CommandLine.RootCommand";

        // Use Command (not RootCommand) when a name is provided, since RootCommand.Name is read-only
        Command command = isRoot && string.IsNullOrEmpty(name)
            ? new RootCommand(description)
            : new Command(string.IsNullOrEmpty(name) ? "root" : name, description);

        // Copy options (resilient to property name differences across versions)
        try
        {
            var options = type.GetProperty("Options")?.GetValue(foreignCommand);
            if (options is System.Collections.IEnumerable optionsList)
            {
                foreach (var opt in optionsList)
                {
                    try
                    {
                        var optType = opt.GetType();
                        var aliases = GetAliases(opt);
                        var optDesc = optType.GetProperty("Description")?.GetValue(opt) as string ?? "";
                        var isRequired = (bool)(optType.GetProperty("IsRequired")?.GetValue(opt) ?? false);

                        var optName = optType.GetProperty("Name")?.GetValue(opt) as string ?? "";
                        var primaryName = aliases.FirstOrDefault(a => a.StartsWith("--"))
                                          ?? (aliases.Length > 0 ? aliases[0] : $"--{optName}");
                        var additionalAliases = aliases.Where(a => a != primaryName).ToArray();
                        var newOpt = new Option<string>(primaryName, additionalAliases)
                        {
                            Description = optDesc,
                            Required = isRequired
                        };
                        command.Options.Add(newOpt);
                    }
                    catch { /* Skip options that fail to resolve */ }
                }
            }
        }
        catch { /* Options property unavailable */ }

        // Copy arguments
        try
        {
            var arguments = type.GetProperty("Arguments")?.GetValue(foreignCommand);
            if (arguments is System.Collections.IEnumerable argsList)
            {
                foreach (var arg in argsList)
                {
                    try
                    {
                        var argType = arg.GetType();
                        var argName = argType.GetProperty("Name")?.GetValue(arg) as string ?? "";
                        var argDesc = argType.GetProperty("Description")?.GetValue(arg) as string ?? "";
                        var newArg = new Argument<string>(argName) { Description = argDesc };
                        command.Arguments.Add(newArg);
                    }
                    catch { /* Skip arguments that fail to resolve */ }
                }
            }
        }
        catch { /* Arguments property unavailable */ }

        // Copy subcommands recursively
        try
        {
            var subcommands = type.GetProperty("Subcommands")?.GetValue(foreignCommand);
            if (subcommands is System.Collections.IEnumerable subcmdList)
            {
                foreach (var subcmd in subcmdList)
                {
                    var wrapped = WrapCommandFromReflection(subcmd);
                    if (wrapped != null)
                    {
                        command.Subcommands.Add(wrapped);
                    }
                }
            }
        }
        catch { /* Subcommands property unavailable */ }

        return command;
    }

    private static string[] GetAliases(object symbolObj)
    {
        var aliasesProp = symbolObj.GetType().GetProperty("Aliases");
        if (aliasesProp?.GetValue(symbolObj) is System.Collections.IEnumerable aliases)
        {
            return aliases.Cast<object>().Select(a => a.ToString()!).ToArray();
        }
        return Array.Empty<string>();
    }
}
