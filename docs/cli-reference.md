# CLI reference

Every flag on every `clidoc` subcommand.

## `clidoc generate [commands-json] [options]`

Render a static documentation site.

### Arguments

| Argument | Description |
| --- | --- |
| `commands-json` | Optional path to a `commands.json`. If omitted, clidoc looks for `./commands.json` in the current directory. |

### Input options (pick one)

| Option | Description |
| --- | --- |
| *(positional)* | Use a pre-built `commands.json` (recommended; works for any app). |
| `--assembly, -a <dll>` | Simple-app shortcut: reflect over a compiled assembly. Does not work for DI-heavy apps. |
| `--project, -p <csproj>` | Simple-app shortcut: build a `.csproj` then reflect over its output. |
| `--entry-type, -t <type>` | Fully-qualified type name with a static `GetRootCommand()`-style factory (assembly/project paths only). |

You may not combine the positional argument with `--assembly` / `--project`.

### Render options

| Option | Default | Description |
| --- | --- | --- |
| `--metadata, -m <path>` | `./cli-docs.yaml` if present | Path to the metadata YAML. |
| `--output, -o <dir>` | `./clidoc-output` | Directory to write the site to. |
| `--title <string>` | from metadata / root command name | Nav brand / page title. |
| `--base-url <url>` | | Base URL for canonical links. |
| `--no-llms-txt` | `false` | Skip emitting `llms.txt`. |

### Examples

```bash
# Pre-built JSON
clidoc generate commands.json --output docs

# Implicit ./commands.json
clidoc generate --output docs

# Simple-app shortcut (no DI)
clidoc generate --project src/MyCli/MyCli.csproj --output docs
```

---

## `clidoc init [commands-json] [options]`

Scaffold a `cli-docs.yaml` for editing.

### Arguments & input options

Same as `generate` — takes a `commands.json` positionally, or falls back to
`--assembly` / `--project` / `--entry-type`.

### Options

| Option | Default | Description |
| --- | --- | --- |
| `--output, -o <path>` | `cli-docs.yaml` | Output file path. |
| `--root-name <name>` | | Override the root command's name in the scaffolded YAML. |

### Example

```bash
clidoc init commands.json --output docs/cli-docs.yaml
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

### Example

```bash
clidoc commands --output clidoc.json
```
