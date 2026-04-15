# clidoc

> Generate beautiful static documentation websites for CLI tools.

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**clidoc** renders a searchable, themeable static site from a
[`commands.json`](docs/commands-json-schema.md) document that describes your CLI.

**📖 [Live demo →](https://jonlipsky.github.io/clidoc/)** *(clidoc documents itself)*

## Features

- 🎨 **Beautiful UI** — dark/light theme, responsive, column / tree / list views.
- 🔍 **Smart search** — filter by name and description.
- 🤖 **LLM-friendly** — emits `llms.txt` alongside the site.
- 🧩 **Framework-agnostic** — any tool that can emit `commands.json` works. Ships with a
  companion NuGet for System.CommandLine apps (including DI-heavy ones).
- 🎭 **Metadata enrichment** — add examples, taglines, and prose sections via
  [`cli-docs.yaml`](docs/metadata-yaml.md).

## Hello world (System.CommandLine)

Add the companion NuGet to your CLI:

```bash
dotnet add package Clidoc.SystemCommandLine
```

```csharp
using System.CommandLine;
using Clidoc.SystemCommandLine;

var root = new RootCommand("My CLI");
// ... register your commands (DI-based or not) ...
root.AddCommandsSubcommand();

return await root.Parse(args).InvokeAsync();
```

Install clidoc, emit `commands.json`, render the site:

```bash
dotnet tool install --global clidoc
mycli commands --output commands.json
clidoc generate --output docs
open docs/commands.html
```

See [docs/getting-started.md](docs/getting-started.md) for more.

## Documentation

- [Getting started](docs/getting-started.md)
- [Companion NuGet (`Clidoc.SystemCommandLine`)](docs/companion-nuget.md)
- [CLI reference](docs/cli-reference.md)
- [`commands.json` schema](docs/commands-json-schema.md)
- [`cli-docs.yaml` metadata](docs/metadata-yaml.md)
- [Architecture](docs/architecture.md)

## Contributing

Contributions welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[MIT](LICENSE).

## Acknowledgments

- UI design inspired by [SonarQube CLI Documentation](https://cli.sonarqube.com/commands.html), designed by [Clifford Goh](https://www.cliffordgoh.com).
- Schema shape inspired by SonarQube's `commands.json`.
- Built on [System.CommandLine](https://github.com/dotnet/command-line-api) and [YamlDotNet](https://github.com/aaubry/YamlDotNet).
