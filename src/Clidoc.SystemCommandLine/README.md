# Clidoc.SystemCommandLine

Emit a [clidoc](https://github.com/jonlipsky/clidoc)-compatible `commands.json` from a
[System.CommandLine](https://github.com/dotnet/command-line-api) application.

Works with any app layout, including apps that use dependency injection — no assembly
reflection is performed; the already-constructed `Command` tree is walked directly.

## Install

```bash
dotnet add package Clidoc.SystemCommandLine
```

## Use

```csharp
using System.CommandLine;
using Clidoc.SystemCommandLine;

var root = new RootCommand("My CLI");
// ... add your commands ...
root.AddCommandsSubcommand();   // exposes: mycli commands --output commands.json

return await root.Parse(args).InvokeAsync();
```

Then:

```bash
mycli commands --output commands.json
clidoc generate commands.json --output docs
```

See the [clidoc docs](https://github.com/jonlipsky/clidoc/tree/main/docs) for details.
