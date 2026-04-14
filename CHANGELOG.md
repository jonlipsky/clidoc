# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
