# `Clidoc.SystemCommandLine`

A tiny library that walks an already-constructed System.CommandLine `Command` tree and
emits a clidoc-compatible `commands.json`. Because it runs **inside your app** (after
DI is wired up), it works for any apps — including those whose command classes take
constructor-injected services.

## Install

```bash
dotnet add package Clidoc.SystemCommandLine
```

Target framework: `net8.0`. Consumable from any `net8.0`+ or `net10.0` app.

## API

### `Command.AddCommandsSubcommand(string name = "commands", string? rootName = null)`

Extension on `Command`. Adds a subcommand that emits `commands.json` on invocation.
The generated subcommand is omitted from its own output.

```csharp
var root = new RootCommand("My CLI");
// ... add your commands ...
root.AddCommandsSubcommand();                              // mycli commands --output commands.json
root.AddCommandsSubcommand("docs");                         // or pick a different subcommand name
root.AddCommandsSubcommand(rootName: "mycli");              // explicit override (rarely needed)
```

In most cases you don't need the `rootName` parameter — see
[Root name auto-detection](#root-name-auto-detection) below.

The subcommand accepts:

| Flag | Default | Description |
| --- | --- | --- |
| `--output, -o <path>` | (stdout) | File to write the JSON to. Parent directories are created if needed. |
| `--pretty` | `true` | Indent the JSON output. |
| `--name <string>` | auto-detected (see below) | Override the root command's name in the emitted JSON. |

## Root name auto-detection

`System.CommandLine`'s `RootCommand` uses your **assembly name** as its `Name`. That's
often wrong for documentation: your assembly might be `MyApp.CLI` but your tool is
installed as `myapp`. Without help, the generated docs would show `MyApp.CLI` in
breadcrumbs, the tree root, and every subcommand's full name.

`Clidoc.SystemCommandLine` detects the correct name automatically. You almost never
need to pass anything explicit. Here's how it works, highest priority first:

1. **`--name <value>` on the command line.** Always wins. Use when you want to override
   everything for a single invocation.
2. **`rootName:` parameter on `AddCommandsSubcommand(...)`.** Set in your code when
   detection is impossible (e.g. you don't set `ToolCommandName` in your csproj and
   you're running via `dotnet run` during dev).
3. **`ClidocToolName` assembly metadata (automatic from your csproj).** When
   `Clidoc.SystemCommandLine` is installed via `PackageReference`, it ships an MSBuild
   targets file (`build/Clidoc.SystemCommandLine.targets`) that runs at build time and
   bakes `<ToolCommandName>` into the assembly as an `AssemblyMetadataAttribute`. At
   runtime the library reads that attribute. **This is the happy path for dotnet tool
   projects** — just having `<PackAsTool>true</PackAsTool>` and `<ToolCommandName>myapp</ToolCommandName>`
   in your csproj is enough.
4. **Executable file name.** If the assembly metadata isn't present, the library falls
   back to the filename of `Environment.ProcessPath`. When your tool is installed via
   `dotnet tool install`, the shim is named after the tool (e.g. `/Users/you/.dotnet/tools/myapp`),
   so this usually Just Works even without the targets file. The dotnet host itself
   (`dotnet`) is explicitly skipped so `dotnet run` and `dotnet MyApp.dll` fall through.
5. **The root command's `Name`.** Last-resort default — the raw assembly name.

### Example: no code changes needed

If your `MyApp.Cli.csproj` already has:

```xml
<PropertyGroup>
  <PackAsTool>true</PackAsTool>
  <ToolCommandName>myapp</ToolCommandName>
</PropertyGroup>
```

…and you call `root.AddCommandsSubcommand();`, running `myapp commands` emits JSON
with `"name": "myapp"`, `"fullName": "myapp"` for the root. Done.

### How the targets file works

Shipped inside the NuGet at `build/Clidoc.SystemCommandLine.targets`. NuGet auto-imports
anything at `build/<PackageId>.targets` into the consuming project. It adds one item:

```xml
<ItemGroup Condition=" '$(ToolCommandName)' != '' ">
  <AssemblyAttribute Include="System.Reflection.AssemblyMetadataAttribute">
    <_Parameter1>ClidocToolName</_Parameter1>
    <_Parameter2>$(ToolCommandName)</_Parameter2>
  </AssemblyAttribute>
</ItemGroup>
```

The .NET SDK's `GenerateAssemblyInfo` target picks that up and emits
`[assembly: AssemblyMetadata("ClidocToolName", "myapp")]` into your generated
`AssemblyInfo`. If you disabled `GenerateAssemblyInfo`, this step is a no-op and
the library falls back to the other detection paths (or the explicit parameters).

> **Note: `ProjectReference` consumers.** Targets files in `build/` are a NuGet
> convention; `ProjectReference` doesn't import them. If you reference the library
> directly from another project in the same repo (rare outside this repo's own
> samples), either (a) manually `<Import Project="...build/Clidoc.SystemCommandLine.targets" />`,
> or (b) pass `rootName:` explicitly.

### `CliDocExporter.RenderJson(Command root, Command? exclude = null, bool pretty = true)`

Produces the JSON as a string. Use it when you want to write custom glue rather than
relying on the auto-generated subcommand.

### `CliDocExporter.Export(Command root, string outputPath, Command? exclude = null, bool pretty = true)`

Same as `RenderJson`, but writes directly to a file. Creates the parent directory if it
doesn't exist.

## DI-based example

```csharp
using System.CommandLine;
using Clidoc.SystemCommandLine;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddSingleton<IGreeter, ConsoleGreeter>()
    .BuildServiceProvider();

var root = new RootCommand("My CLI");
root.Subcommands.Add(new GreetCommand(services));   // ctor takes IServiceProvider
root.AddCommandsSubcommand();

return await root.Parse(args).InvokeAsync();
```

Running `mycli commands --output commands.json` now produces a document that includes
`greet` and all its options — even though `GreetCommand` requires a live service
provider that assembly-reflection can't supply.

See [samples/HelloCli](../samples/HelloCli) for a runnable version.

## Regenerating docs in CI

Add a step to your release workflow that emits `commands.json` and hands it to clidoc:

```yaml
- name: Build CLI
  run: dotnet build -c Release

- name: Export commands.json
  run: |
    dotnet run -c Release --no-build --project src/MyCli -- \
      commands --output site/commands.json

- name: Render documentation site
  run: |
    dotnet tool install --global clidoc
    clidoc generate docs --commands-json site/commands.json --metadata cli-docs.yaml --output site
```

## FAQ

**Does the exporter subcommand show up in my own docs?**
No. `AddCommandsSubcommand` passes the subcommand it creates to the exporter as the
`exclude` argument, so it's filtered out of `commands.json`.

**Can I hide other internal commands?**
Yes — call `CliDocExporter.RenderJson(root, exclude: yourInternalCommand)` directly
instead of using the shipped subcommand, or use `Export(...)` for a file.

**What if my app doesn't have a `RootCommand`?**
Any `Command` works. `AddCommandsSubcommand` is an extension on `Command`, not
`RootCommand`. The exporter walks whatever subtree you give it.
