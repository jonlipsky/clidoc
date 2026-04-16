# Architecture

clidoc is split into two packages:

```
Clidoc.SystemCommandLine   ← library  (net8.0)  — walks a Command tree, emits commands.json
clidoc                      ← tool     (net10.0) — reads commands.json, renders a static site
```

The pivot point is `commands.json`. This makes clidoc language- and framework-agnostic:
anything that emits the schema can drive the renderer.

## Pipeline

```
  clidoc generate commands             clidoc generate docs
  (.dll / .csproj → JSON)              (JSON + YAML → site)

  ┌────────────────────────┐           ┌────────────────────────┐
  │ AssemblyCommandLoader  │           │ CommandsJsonLoader     │
  │ + CommandExtractor     │           │ (reads commands.json)  │
  └──────────┬─────────────┘           └──────────┬─────────────┘
             │                                    │
             ▼                                    ▼
     commands.json file  ──────────────►  CommandsOutput (in-memory)
                                                  │
                                      merge cli-docs.yaml metadata
                                                  │
                                                  ▼
                          ┌───────────────────────┼────────────────────────┐
                          ▼                       ▼                        ▼
                   SiteRenderer           CliDocExporter           LlmsTxtRenderer
                   (HTML + JS + CSS       (serializes JSON         (plain-text for
                    + embedded data)       for commands.json)       LLM consumption)
```

The pivot point is `commands.json`. `generate commands` is the optional producer
(for simple apps without the companion NuGet); `generate docs` is the renderer.
Apps that reference [`Clidoc.SystemCommandLine`](companion-nuget.md) skip
`generate commands` entirely — they emit the JSON themselves via `mycli commands`
and hand it to `generate docs`.

## Key types

- `Clidoc.SystemCommandLine.Schema.CommandsOutput` — the root document type.
- `Clidoc.SystemCommandLine.Schema.OutputCommand` — one command in the tree.
- `Clidoc.SystemCommandLine.Extraction.CommandExtractor` — walks `System.CommandLine.Command` to produce `OutputCommand`s.
- `Clidoc.SystemCommandLine.CliDocExporter` — serializes `CommandsOutput` to JSON (or takes a `Command` and does both).
- `Clidoc.SystemCommandLine.CommandExtensions.AddCommandsSubcommand` — adds the dogfood subcommand.
- `CliDoc.Input.CommandsJsonLoader` — reads and validates `commands.json` on disk.
- `CliDoc.Commands.GenerateCommandsCommand` — the `generate commands` action (assembly → JSON).
- `CliDoc.Commands.GenerateDocsCommand` — the `generate docs` action (JSON → site).
- `CliDoc.Merging.CommandMerger` — applies YAML metadata to a `CommandsOutput`'s command list.
- `CliDoc.Output.SiteRenderer` — renders the static site.
- `CliDoc.Output.LlmsTxtRenderer` — renders `llms.txt`.

## Why two subcommands?

The rendering pipeline (`generate docs`) is decoupled from the JSON producer by design:
anything that emits a conforming `commands.json` can drive it, including non-.NET tools.

`generate commands` is the .NET-specific escape hatch. Point it at a DLL or csproj, get
a `commands.json` back, pipe it into `generate docs`. It does not support DI because it
reflects over types and instantiates them with null/default parameters — which breaks
for any constructor that requires a live service provider.

Apps that want DI-correct output reference [`Clidoc.SystemCommandLine`](companion-nuget.md),
add `root.AddCommandsSubcommand()`, and emit the JSON from inside their own process
after DI is wired up. That bypasses `generate commands` entirely.

## Why a separate NuGet?

- The renderer (`clidoc`) is a .NET global tool.
- The emitter (`Clidoc.SystemCommandLine`) is a library your app references.

Separating them means:

1. Your app doesn't drag in any rendering/template code.
2. The tool can be updated independently of your app.
3. Other ecosystems can emit the same JSON with their own libraries — we don't have to
   ship one giant thing.
