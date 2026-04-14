# clidoc

> Generate beautiful static documentation websites for System.CommandLine CLI tools

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**clidoc** is a .NET global tool that automatically generates beautiful, searchable static documentation sites from CLI tools built with [System.CommandLine](https://github.com/dotnet/command-line-api). 

Inspired by [SonarQube CLI Docs](https://cli.sonarqube.com/commands.html), clidoc creates professional documentation with minimal effort.

## ✨ Features

- 🎨 **Beautiful UI** - Professional interface with dark/light theme support
- 🔍 **Smart Search** - Filter commands by name and description
- 📱 **Responsive** - Works seamlessly on desktop and mobile
- 🎯 **Multiple Views** - Column, tree, and list layouts for browsing commands
- 📋 **Copy-to-Clipboard** - Easily copy example commands
- 🤖 **LLM-Friendly** - Generates `llms.txt` for AI consumption
- 🎭 **Metadata Enrichment** - Add examples and custom sections via YAML
- 🚀 **Zero Config** - Works out-of-the-box with auto-discovery

## 📦 Installation

Install as a .NET global tool:

```bash
dotnet tool install --global clidoc
```

Update to the latest version:

```bash
dotnet tool update --global clidoc
```

## 🚀 Quick Start

### 1. Generate Metadata Scaffold

```bash
clidoc init --assembly path/to/your-cli.dll
```

This creates a `cli-docs.yaml` file with all your commands pre-filled.

### 2. Add Examples and Descriptions

Edit `cli-docs.yaml` to add usage examples, custom descriptions, and additional sections:

```yaml
commands:
  "mycli run":
    examples:
      - description: Run with default settings
        command: mycli run app.yaml
      - description: Run with verbose output
        command: mycli run app.yaml --verbose
```

### 3. Generate Documentation Site

```bash
clidoc generate --assembly path/to/your-cli.dll
```

### 4. Open in Browser

```bash
open clidoc-output/commands.html
```

## 📖 Commands

### `clidoc init`

Generate a `cli-docs.yaml` scaffold from an assembly.

**Options:**
- `--assembly, -a` (required) - Path to the compiled CLI assembly (.dll)
- `--output, -o` - Output file path (default: `cli-docs.yaml`)
- `--entry-type, -t` - Fully-qualified type name with entry point
- `--root-name` - Override the root command name

**Example:**

```bash
clidoc init --assembly MyApp.dll --output docs/cli-docs.yaml
```

### `clidoc generate`

Generate static documentation site.

**Options:**
- `--assembly, -a` (required) - Path to the compiled CLI assembly (.dll)
- `--metadata, -m` - Path to cli-docs.yaml metadata file
- `--output, -o` - Output directory (default: `./clidoc-output`)
- `--title` - Site title (overrides metadata)
- `--entry-type, -t` - Fully-qualified type name with entry point
- `--base-url` - Base URL for canonical links
- `--no-llms-txt` - Skip llms.txt generation

**Example:**

```bash
clidoc generate --assembly MyApp.dll --metadata cli-docs.yaml --output docs
```

## 🎨 Features Overview

### Multiple View Modes

- **Column View** - Hierarchical layout with SVG connectors
- **Tree View** - Collapsible tree structure
- **List View** - Flat list with descriptions

### Dark/Light Theme

Automatic theme detection with manual toggle. Theme preference persists in localStorage.

### Search

Real-time filtering of commands by name and description.

### Copy-to-Clipboard

One-click copy for all example commands.

### Responsive Design

Mobile-friendly layout with adaptive sidebar.

## 📝 Metadata File Format

The `cli-docs.yaml` file enriches auto-discovered command structure:

```yaml
site:
  title: "My CLI Tool"
  tagline: "One-line description"
  baseUrl: https://docs.mycli.dev
  theme:
    accentColor: "#6366f1"

commands:
  "mycli":
    tagline: "Override auto-discovered description"
    sections:
      - title: Installation
        body: |
          Markdown content goes here...
  
  "mycli run":
    examples:
      - description: Basic usage
        command: mycli run app.yaml
      - description: With options
        command: mycli run app.yaml --verbose
    sections:
      - title: Configuration
        body: |
          Details about configuration...
```

**Important:** Metadata can only add documentation (examples, sections, taglines). Command structure (options, arguments, subcommands) always comes from the assembly.

## 🔧 Entry Point Discovery

clidoc automatically discovers your command entry point:

1. If `--entry-type` is specified, looks for a static method in that type
2. Scans for well-known method names: `GetRootCommand`, `CreateRootCommand`, `BuildCommandLine`
3. Searches all public types for static methods returning `RootCommand` or `Command`

**Example entry point:**

```csharp
public class Program
{
    public static RootCommand GetRootCommand()
    {
        var rootCommand = new RootCommand("My CLI tool");
        // ... add commands
        return rootCommand;
    }
}
```

## 🌐 Generated Output

The documentation site includes:

- **commands.html** - Main documentation browser
- **commands.json** - Machine-readable command reference
- **commands.js** - JavaScript bundle with embedded data
- **style.css** - Complete stylesheet with theming
- **index.html** - Landing page (if site config exists)
- **llms.txt** - Plain-text reference for LLMs

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by [SonarQube CLI Documentation](https://cli.sonarqube.com/commands.html)
- Built with [System.CommandLine](https://github.com/dotnet/command-line-api)
- Uses [YamlDotNet](https://github.com/aaubry/YamlDotNet) for YAML parsing

---

**Made with ❤️ for the .NET CLI community**
