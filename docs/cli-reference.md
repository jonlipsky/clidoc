# CLI reference

Every flag on every `clidoc` subcommand.

## `clidoc generate [options]`

Render a static documentation site.

### Input options (pick one)

| Option | Description |
| --- | --- |
| `--commands-json, -c <path>` | Pre-built `commands.json` (recommended; works for any app). Defaults to `./commands.json` if the flag is omitted. |
| `--assembly, -a <dll>` | Simple-app shortcut: reflect over a compiled assembly. Does not work for DI-heavy apps. |
| `--project, -p <csproj>` | Simple-app shortcut: build a `.csproj` then reflect over its output. |
| `--entry-type, -t <type>` | Fully-qualified type name with a static `GetRootCommand()`-style factory (assembly path only). |

You may not combine `--commands-json` with `--assembly` / `--project`.

### Render options

| Option | Default | Description |
| --- | --- | --- |
| `--metadata, -m <path>` | `./cli-docs.yaml` if present | Path to the metadata YAML. |
| `--output, -o <dir>` | `./clidoc-output` | Directory to write the site to. |
| `--title <string>` | from metadata / root command name | Nav brand / page title. |
| `--root-name <name>` | root command's name in the JSON | Overrides the root command's name everywhere it appears (breadcrumbs, tree root, subcommand full names). Useful when the JSON was emitted with the assembly name. |
| `--base-url <url>` | | Base URL for canonical links. |
| `--no-llms-txt` | `false` | Skip emitting `llms.txt`. |

### Examples

```bash
# Implicit ./commands.json in cwd
clidoc generate --output docs

# Explicit commands.json path
clidoc generate --commands-json build/commands.json --output docs

# Override the root command's display name
clidoc generate -c ps.json --root-name processstack --title ProcessStack --output docs

# Simple-app shortcut (no DI)
clidoc generate --project src/MyCli/MyCli.csproj --output docs
```

---

## `clidoc init [options]`

Scaffold a `cli-docs.yaml` for editing.

### Input options

Same as `generate` — `--commands-json, -c <path>` (defaults to `./commands.json`),
or fall back to `--assembly` / `--project` / `--entry-type`.

### Options

| Option | Default | Description |
| --- | --- | --- |
| `--output, -o <path>` | `cli-docs.yaml` | Output file path. |
| `--root-name <name>` | | Override the root command's name in the scaffolded YAML. |

### Example

```bash
clidoc init -c commands.json --output docs/cli-docs.yaml
```

---

## `clidoc commands [options]`

Dogfood: emit clidoc's own `commands.json`. Installed by
[`Clidoc.SystemCommandLine`](companion-nuget.md) — the same subcommand is available
on any app that references that package.

### Options

| Option | Default | Description |
| --- | --- | --- |
| `--output, -o <path>` | stdout | File to write the JSON to. |
| `--pretty` | `true` | Indent the JSON output. |
| `--name <string>` | root command's `Name` | Override the root command's name in the emitted JSON. Useful when the runtime `Name` is the assembly name (e.g. `ProcessStack.CLI`) but the tool is invoked as something else (e.g. `processstack`). |

### Example

```bash
processstack commands --output commands.json --name processstack
```
