# CLI reference

Every flag on every `clidoc` subcommand.

## Command shape

```
clidoc init                    # scaffold cli-docs.yaml from commands.json
clidoc generate commands       # extract commands.json from a .NET assembly or project
clidoc generate docs           # render a static site from commands.json
clidoc commands                # dogfood: emit clidoc's own commands.json
```

The `commands` subcommand at the root is contributed by
[`Clidoc.SystemCommandLine`](companion-nuget.md) — the same one your own System.CommandLine
app gets when it calls `root.AddCommandsSubcommand()`.

---

## `clidoc init [options]`

Scaffold a `cli-docs.yaml` file (human-edited metadata — site config, examples, prose
sections) from a `commands.json` file.

| Option | Default | Description |
| --- | --- | --- |
| `--commands-json, -c <path>` | `./commands.json` | Path to commands.json. |
| `--output, -o <path>` | `cli-docs.yaml` | Where to write the scaffold. |
| `--root-name <name>` | root name from the JSON | Override the root command's name used as the YAML map key. |

### Example

```bash
clidoc init -c build/commands.json -o docs/cli-docs.yaml
```

---

## `clidoc generate commands [options]`

Extract a `commands.json` from a compiled .NET assembly or a `.csproj`. Useful when
the target CLI doesn't reference [`Clidoc.SystemCommandLine`](companion-nuget.md) and
therefore can't emit its own JSON at runtime. Does **not** support dependency injection;
for DI-heavy apps, use the companion NuGet instead.

### Input (pick one)

| Option | Description |
| --- | --- |
| `--assembly, -a <dll>` | Path to the compiled CLI assembly. |
| `--project, -p <csproj>` | Path to a .csproj file. clidoc will build it and reflect over the output. |

### Options

| Option | Default | Description |
| --- | --- | --- |
| `--entry-type, -t <type>` | auto-discovered | Fully-qualified type name with a static method returning `RootCommand`. |
| `--root-name <name>` | from `<ToolCommandName>` (via `--project`), else the assembly-default root name | Override the root command's name in the emitted JSON. |
| `--output, -o <path>` | `commands.json` | Output path for the generated JSON. |
| `--pretty` | `true` | Indent the JSON. |

### Examples

```bash
# From an already-built assembly
clidoc generate commands --assembly bin/Release/net8.0/MyCli.dll

# From a csproj (builds it first, picks up <ToolCommandName> automatically)
clidoc generate commands --project src/MyCli/MyCli.csproj
```

---

## `clidoc generate docs [options]`

Render a static documentation site from a `commands.json`. Optionally enriched with a
hand-edited `cli-docs.yaml` (see [`init`](#clidoc-init-options) and
[cli-docs.yaml metadata](metadata-yaml.md)).

| Option | Default | Description |
| --- | --- | --- |
| `--commands-json, -c <path>` | `./commands.json` | Path to the input commands.json. |
| `--metadata, -m <path>` | `./cli-docs.yaml` if present | Path to the metadata YAML. |
| `--output, -o <dir>` | `./clidoc-output` | Directory to write the site to. |
| `--title <string>` | from metadata / root command name | Nav brand / page title. |
| `--root-name <name>` | root name from the JSON | Overrides the root command's name everywhere (breadcrumbs, tree root, subcommand full names). |
| `--base-url <url>` | | Base URL for canonical links. |
| `--no-llms-txt` | `false` | Skip emitting `llms.txt`. |
| `--no-commands-json` | `false` | Skip copying `commands.json` into the output (and hide its nav link). |

### Examples

```bash
# Implicit inputs (./commands.json, ./cli-docs.yaml if present)
clidoc generate docs --output docs

# Explicit paths
clidoc generate docs -c build/commands.json -m config/cli-docs.yaml -o site

# Slim build: no JSON copy, no llms.txt
clidoc generate docs --no-commands-json --no-llms-txt -o site
```

---

## `clidoc commands [options]`

Dogfood: emit clidoc's own `commands.json`. This subcommand is contributed by
[`Clidoc.SystemCommandLine`](companion-nuget.md) — every app that references the
companion NuGet and calls `root.AddCommandsSubcommand()` gets the same subcommand.

| Option | Default | Description |
| --- | --- | --- |
| `--output, -o <path>` | stdout | File to write the JSON to. |
| `--pretty` | `true` | Indent the JSON output. |
| `--name <string>` | auto-detected | Override the root command's name in the emitted JSON (see [Root name auto-detection](companion-nuget.md#root-name-auto-detection)). |

### Example

```bash
clidoc commands --output clidoc.json
```
