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
 Input source
 ┌─────────────────────────────┐
 │ (a) commands.json on disk   │──► CommandsJsonLoader ──┐
 │ (b) --assembly / --project  │──► AssemblyCommandLoader│
 │     (simple apps only;      │    + CommandExtractor   │
 │      reflects over types)   │                         │
 └─────────────────────────────┘                         │
                                                         ▼
                                                 CommandsOutput (in-memory)
                                                         │
                                          merge cli-docs.yaml metadata
                                                         │
                                                         ▼
                                 ┌───────────────────────┼────────────────────────┐
                                 ▼                       ▼                        ▼
                          SiteRenderer          JsonRenderer (via        LlmsTxtRenderer
                          (HTML + JS + CSS      CliDocExporter)          (plain text for LLMs)
                           + embedded data)
```

## Key types

- `Clidoc.SystemCommandLine.Schema.CommandsOutput` — the root document type.
- `Clidoc.SystemCommandLine.Schema.OutputCommand` — one command in the tree.
- `Clidoc.SystemCommandLine.Extraction.CommandExtractor` — walks `System.CommandLine.Command` to produce `OutputCommand`s.
- `Clidoc.SystemCommandLine.CliDocExporter` — serializes `CommandsOutput` to JSON (or takes a `Command` and does both).
- `Clidoc.SystemCommandLine.CommandExtensions.AddCommandsSubcommand` — adds the dogfood subcommand.
- `CliDoc.Input.CommandsJsonLoader` — reads and validates `commands.json` on disk.
- `CliDoc.Input.InputResolver` — unifies positional/assembly input into a `CommandsOutput`.
- `CliDoc.Merging.CommandMerger` — applies YAML metadata to a `CommandsOutput`'s command list.
- `CliDoc.Output.SiteRenderer` — renders the static site.
- `CliDoc.Output.LlmsTxtRenderer` — renders `llms.txt`.

## Why two inputs?

The JSON path is the intended one. It works for everyone — including DI-heavy apps that
cannot be safely reflected over from the outside.

The `--assembly` / `--project` path is a convenience for apps with no DI: point at a
DLL, get docs, skip the NuGet. It still goes through the same `CommandsOutput`
pipeline, so there is exactly one render path.

## Why a separate NuGet?

- The renderer (`clidoc`) is a .NET global tool.
- The emitter (`Clidoc.SystemCommandLine`) is a library your app references.

Separating them means:

1. Your app doesn't drag in any rendering/template code.
2. The tool can be updated independently of your app.
3. Other ecosystems can emit the same JSON with their own libraries — we don't have to
   ship one giant thing.
