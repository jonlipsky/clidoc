# Getting started

clidoc renders a static documentation site from a `commands.json` file that describes
your CLI. You get `commands.json` in one of two ways, depending on how your app is built.

## 1. Install clidoc

```bash
dotnet tool install --global clidoc
```

Update later with `dotnet tool update --global clidoc`.

## 2. Produce a `commands.json`

### Option A — DI-based app (recommended)

Add the companion NuGet to your CLI and call `AddCommandsSubcommand()` on your root command:

```bash
dotnet add package Clidoc.SystemCommandLine
```

```csharp
using System.CommandLine;
using Clidoc.SystemCommandLine;

var root = new RootCommand("My CLI");
// ... register your own subcommands, including DI-injected ones ...
root.AddCommandsSubcommand();

return await root.Parse(args).InvokeAsync();
```

Then run your own CLI to emit `commands.json`:

```bash
mycli commands --output commands.json
```

This walks your already-constructed command tree — dependency injection is fully live,
nothing is reflected over.

### Option B — Simple app (no DI)

Point `clidoc` at your compiled assembly. Works for apps whose `Command` classes can be
constructed without services (no DI required):

```bash
clidoc generate --project src/MyCli/MyCli.csproj --output docs
```

or, if you already have a built DLL:

```bash
clidoc generate --assembly bin/Release/net8.0/MyCli.dll --output docs
```

## 3. Render the site

Run `clidoc generate` in a directory with a `commands.json`:

```bash
clidoc generate --output docs
```

Or point at an explicit path:

```bash
clidoc generate --commands-json path/to/commands.json --output docs
```

If the root command's name in the JSON is the assembly name (e.g. `ProcessStack.CLI`)
rather than the tool's invocation name (e.g. `processstack`), add `--root-name`:

```bash
clidoc generate --root-name processstack --output docs
```

## 4. Add examples and descriptions (optional)

Scaffold a `cli-docs.yaml` file:

```bash
clidoc init
```

Edit it to add usage examples, taglines, and prose sections. Re-run `clidoc generate` to
see the changes. See [`cli-docs.yaml` metadata](metadata-yaml.md) for the full reference.

## 5. Open the site

```bash
open docs/commands.html
```

## Next steps

- [Companion NuGet reference](companion-nuget.md) — deeper dive on `Clidoc.SystemCommandLine`.
- [`commands.json` schema](commands-json-schema.md) — if you want to emit it from a
  non-.NET toolchain.
- [CLI reference](cli-reference.md) — every flag.
