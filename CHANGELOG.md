# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.4] - 2026-04-15

### Added
- **`site.icon`** in `cli-docs.yaml` — displays an image to the left of the
  title in the nav bar on both `index.html` and `commands.html`. Paths are
  resolved relative to the yaml file, copied into the output directory, and
  support any browser-renderable format. Absolute `http(s)://` URLs are
  referenced in place.
- **`site.favicon`** is now wired up — same resolution rules as `icon`;
  emitted as `<link rel="icon">` on every page.
- **`site.packageId`** — correct NuGet package id for the landing-page
  install snippet. Previously the snippet derived from the lowercased title,
  which is usually wrong when the package id differs from the tool's
  invocation name (e.g. `ProcessStack.Cli` vs `processstack`).

### Changed
- **`clidoc generate docs` always emits `index.html`** now, even without a
  `cli-docs.yaml`. With no metadata, the landing page falls back to a minimal
  shape (title + tagline from the root command's description + "Browse
  Commands" CTA); the Installation section is omitted in that minimal mode.
- **Site title cascade on `commands.html`** now prefers
  `metadata.Site.Title` over the root command's name. Previously the root
  name always won in the nav, even when the yaml set an explicit title.
- **Landing page uses a fixed-height flex layout**, so the scrollbar is
  confined to the content area between the sticky nav and the footer instead
  of spanning the whole viewport.
- **Markdown renderer merges hard-wrapped lines into a single `<p>`** and
  applies inline markdown (`` `code` `` / `**bold**`) inside quick-start step
  titles and descriptions.

### Fixed
- **Tree-view alignment.** Leaf commands now reserve the same 16px width as
  a disclosure triangle, so their labels line up with sibling groups that
  have toggles. Previously leaves were shifted left.

## [1.1.3] - 2026-04-15

### Changed
- **`clidoc generate` is now a command group** with two subcommands:
  - `clidoc generate commands` — extract `commands.json` from a System.CommandLine
    assembly (or a `.csproj`). Takes `--assembly` / `--project` / `--entry-type` /
    `--root-name` / `--output` / `--pretty`.
  - `clidoc generate docs` — render a static site from a `commands.json`, optionally
    enriched with a `cli-docs.yaml`. Takes `--commands-json` / `--metadata` /
    `--output` / `--title` / `--root-name` / `--base-url` / `--no-llms-txt` /
    `--no-commands-json`.

  The previous single `clidoc generate` command is gone; the replacement is the
  two-step `clidoc generate commands … && clidoc generate docs …`. Breaking for
  anyone on 1.1.0–1.1.2, but only one day old.
- **`clidoc init` no longer accepts `--assembly` / `--project`.** It strictly
  scaffolds a `cli-docs.yaml` from an existing `commands.json`. To start from an
  assembly, run `clidoc generate commands` first, then `clidoc init`.

### Added
- **`clidoc generate docs --no-commands-json`** — skip copying the input JSON into
  the output directory (and hide its nav link). Pairs with the existing
  `--no-llms-txt`.

## [1.1.2] - 2026-04-15

### Added
- **Automatic root-name detection** in `Clidoc.SystemCommandLine`. The NuGet now
  ships an MSBuild targets file (`build/Clidoc.SystemCommandLine.targets`) that
  bakes the consuming project's `<ToolCommandName>` into the assembly as an
  `AssemblyMetadataAttribute("ClidocToolName", ...)`. At runtime `CliDocExporter`
  reads this attribute and uses it as the default root name for the emitted
  `commands.json` — no explicit `rootName` parameter or `--name` flag needed.
  Falls back to the filename of `Environment.ProcessPath` when the attribute
  isn't present (works for installed `dotnet tool` shims). The explicit
  `rootName` parameter and `--name` flag still override auto-detection.
- New docs section: [Root name auto-detection](docs/companion-nuget.md#root-name-auto-detection).

## [1.1.1] - 2026-04-15

### Changed
- **`commands.json` input is now a named option.** `clidoc generate` and `clidoc init`
  take `--commands-json, -c <path>` instead of a positional argument. The implicit
  fallback to `./commands.json` is unchanged.

### Added
- **`clidoc generate --root-name <name>`** — override the root command's name
  (breadcrumbs, tree root, subcommand full names). Mirrors the existing option on
  `clidoc init`.
- **`Clidoc.SystemCommandLine`: root-name overrides** for the exporter subcommand.
  - `AddCommandsSubcommand(rootName: "myapp")` sets the emitted root name at code time.
  - `mycli commands --name myapp` sets it at runtime (wins over the code-time default).
  - `CliDocExporter.RenderJson(root, rootName: ...)` and `Export(...)` accept the same parameter.

### Fixed
- **Tree view indentation is no longer compounding.** Previously each node carried an
  inline `margin-left: depth × 1.5rem` *in addition to* the `.tree-children` container's
  own margin, producing very deep indents. Now only `.tree-children` contributes
  (reduced to `1rem`).

## [1.1.0] - 2026-04-15

### Added
- **New companion NuGet `Clidoc.SystemCommandLine`.** Reference it from your
  System.CommandLine CLI and call `root.AddCommandsSubcommand()` to expose a
  `commands` subcommand that emits a clidoc-compatible `commands.json`. This walks
  the already-constructed command tree and works for apps that use dependency
  injection (the old assembly-reflection path fails on those).
- **`commands.json` schema v1.0** — now formalized as clidoc's public input
  contract. `clidoc generate` and `clidoc init` accept a positional JSON path
  (or find `./commands.json` automatically). A `clidoc commands` subcommand is
  also available on clidoc itself via dogfooding.
- **New docs tree under `docs/`** — separate pages for getting started, companion
  NuGet usage, CLI reference, schema reference, metadata YAML, and architecture.
  The repo README is now a short landing page with links.
- **Sample app** at `samples/HelloCli` — a minimal DI-based System.CommandLine CLI
  that emits `commands.json` via the companion NuGet.

### Changed
- **Schema field renamed:** `version` → `schemaVersion` in `commands.json`.
  Technically breaking for anyone parsing the old 1.0.0 output.
- **Render pipeline unified.** Both input paths (new JSON, legacy `--assembly` /
  `--project`) now flow into the same `CommandsOutput` model before rendering.
- **`--assembly` / `--project` are now the "simple-app" shortcut**, not the
  primary input. They still work for non-DI apps.

## [1.0.0] - 2026-04-14

### Added
- Initial release of clidoc
- **Core Pipeline**
  - Command extraction from System.CommandLine assemblies
  - YAML metadata parsing with YamlDotNet
  - Command merging (structure from assembly + metadata enrichment)
  - JSON output renderer
- **Static Site Generation**
  - Beautiful HTML/CSS/JS templates with dark/light theme
  - Column view with SVG connectors
  - Tree view with collapsible nodes
  - List view for flat browsing
  - Real-time search filtering
  - Copy-to-clipboard for examples
  - Responsive mobile layout
- **CLI Commands**
  - `clidoc init` - Generate metadata scaffold
  - `clidoc generate` - Generate static documentation site
  - Assembly loading with isolation via AssemblyLoadContext
  - Automatic entry point discovery
- **Documentation Features**
  - LLMs.txt generation for AI consumption
  - Metadata enrichment via YAML (examples, sections, taglines)
  - Landing page generation
  - Machine-readable commands.json
- **Dogfooding**
  - Self-documenting with comprehensive examples
  - GitHub Actions for CI/CD
  - GitHub Pages deployment
- **Package**
  - Published as .NET global tool
  - MIT licensed
  - Full README and documentation

[1.0.0]: https://github.com/jonlipsky/clidoc/releases/tag/v1.0.0
