# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
