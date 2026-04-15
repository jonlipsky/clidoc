# `cli-docs.yaml` metadata

`commands.json` describes the *structure* of your CLI. `cli-docs.yaml` enriches it with
the things you'd rather edit by hand than emit from code: examples, prose sections,
site-level config, quick-start scenarios.

It's optional. If present (either at the default path `cli-docs.yaml` or passed via
`--metadata`), clidoc merges it over `commands.json` at render time.

## Minimal example

```yaml
site:
  title: "mycli"
  tagline: "Do the thing"

commands:
  "mycli run":
    examples:
      - description: Run with defaults
        command: mycli run
      - description: Run with a config file
        command: mycli run --config app.yaml
```

## Top-level keys

### `site`

Site-wide configuration. All fields optional.

| Field | Type | Notes |
| --- | --- | --- |
| `title` | string | Shown in the nav bar and page title. |
| `tagline` | string | Sub-heading on the landing page. |
| `logo` | string | Path (relative to output) to a logo image. |
| `favicon` | string | Path to a favicon. |
| `baseUrl` | string | Base URL for canonical links. |
| `githubUrl` | string | Shown as a "GitHub" link in the nav. |
| `quickstart` | array | See [Quick-start scenarios](#quick-start-scenarios). |
| `theme.accentColor` | string | CSS color for the accent. |

### `commands`

A map keyed by the command's `fullName` (e.g. `"mycli auth login"`). Each entry may set:

| Field | Type | Effect |
| --- | --- | --- |
| `tagline` | string | Overrides the command's `description` in the rendered site. |
| `examples` | array | Replaces any examples already in `commands.json` for this command. |
| `sections` | array | Replaces any sections already in `commands.json` for this command. |

`examples[]`:

| Field | Type |
| --- | --- |
| `description` | string |
| `command` | string |

`sections[]`:

| Field | Type |
| --- | --- |
| `title` | string |
| `body` | string (markdown) |

Metadata **cannot** change structure: command names, argument/option lists, or
hierarchy. Those always come from `commands.json`.

## Quick-start scenarios

Renders a tabbed "what do you want to do?" block on the landing page.

```yaml
site:
  quickstart:
    - name: "Install"
      steps:
        - title: "Install the tool"
          command: "dotnet tool install --global mycli"
          description: "Installs globally via the .NET SDK."
        - title: "Verify"
          command: "mycli --version"

    - name: "Upgrade"
      steps:
        - title: "Update to the latest version"
          command: "dotnet tool update --global mycli"
```

Each scenario has a `name` and a `steps` array. Each step has:

| Field | Type | Notes |
| --- | --- | --- |
| `title` | string | Heading for the step. |
| `command` | string | Optional. Rendered as a copyable code block. |
| `description` | string | Optional. Prose under the command. |

## Scaffolding

`clidoc init` produces a starter `cli-docs.yaml` pre-populated with every discovered
command, with commented-out example/section blocks you can uncomment and edit:

```bash
clidoc init commands.json
```

## Usage with `clidoc generate`

- **Default path.** If `./cli-docs.yaml` exists, it's picked up automatically.
- **Explicit path.** `clidoc generate commands.json --metadata docs/cli-docs.yaml`.
- **No metadata.** Pass nothing; the site renders using only the JSON.
