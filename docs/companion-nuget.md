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

### `Command.AddCommandsSubcommand(string name = "commands")`

Extension on `Command`. Adds a subcommand that emits `commands.json` on invocation.
The generated subcommand is omitted from its own output.

```csharp
var root = new RootCommand("My CLI");
// ... add your commands ...
root.AddCommandsSubcommand();          // `mycli commands --output commands.json`
root.AddCommandsSubcommand("docs");    // or pick a different name: `mycli docs --output ...`
```

The subcommand accepts:

| Flag | Default | Description |
| --- | --- | --- |
| `--output, -o <path>` | (stdout) | File to write the JSON to. Parent directories are created if needed. |
| `--pretty` | `true` | Indent the JSON output. |

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
    clidoc generate site/commands.json --metadata cli-docs.yaml --output site
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
