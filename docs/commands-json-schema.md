# `commands.json` schema (v1.0)

`commands.json` is the input contract clidoc consumes. This document describes
schema version `1.0`. Anything that emits a conforming file — the
[`Clidoc.SystemCommandLine`](companion-nuget.md) NuGet, a hand-written script in
Python/Go/Rust, or a third-party adapter — can feed clidoc.

## Top-level shape

```json
{
  "schemaVersion": "1.0",
  "generatedAt": "2026-04-15T20:25:05.826Z",
  "generator": "Clidoc.SystemCommandLine",
  "commands": [ /* command objects, flat list */ ]
}
```

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `schemaVersion` | string | ✅ | Semver-ish. clidoc matches on the major version; minor bumps are forward-compatible. |
| `generatedAt` | string | ✅ | ISO 8601 timestamp. Purely informational. |
| `generator` | string | ✅ | Free-form name of the emitter. Shown in debug output. |
| `commands` | array | ✅ | Flat array of command objects. Hierarchy is encoded via `parentId` + `children`. |

## Command object

```json
{
  "id": "mycli-auth-login",
  "name": "login",
  "fullName": "mycli auth login",
  "description": "Authenticate with the server",
  "isGroup": false,
  "isRoot": false,
  "depth": 2,
  "parentId": "mycli-auth",
  "arguments": [ /* argument objects */ ],
  "options":   [ /* option objects   */ ],
  "examples":  [ /* example objects, optional */ ],
  "sections":  [ /* section objects, optional */ ],
  "children":  [ /* string IDs of direct subcommands */ ]
}
```

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `id` | string | ✅ | Stable, URL-safe identifier. Convention: dash-joined path (`mycli-auth-login`). |
| `name` | string | ✅ | Just this command's name (e.g. `login`). |
| `fullName` | string | ✅ | Full invocation path (e.g. `mycli auth login`). Used as lookup key for `cli-docs.yaml` metadata. |
| `description` | string | ✅ | One-line summary. May be empty string. |
| `isGroup` | bool | ✅ | `true` if this command has subcommands. |
| `isRoot` | bool | ✅ | `true` exactly once (the root). |
| `depth` | number | ✅ | Distance from root (root = 0). |
| `parentId` | string \| null | ✅ | The root's `parentId` is `null`. |
| `arguments` | array | ✅ | Positional arguments. |
| `options` | array | ✅ | Flags / named options. |
| `examples` | array | optional | Usage examples. Often filled in from `cli-docs.yaml`. |
| `sections` | array | optional | Long-form markdown sections. Often filled in from `cli-docs.yaml`. |
| `children` | array | ✅ | `id` values of direct subcommands, in declaration order. Empty if `!isGroup`. |

### `arguments[]`

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `name` | string | ✅ | |
| `description` | string | ✅ | May be empty string. |
| `isRequired` | bool | ✅ | |
| `isVariadic` | bool | ✅ | `true` if it accepts multiple values. |

### `options[]`

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `name` | string | ✅ | Long form with leading dashes, e.g. `--output`. |
| `description` | string | ✅ | May be empty string. |
| `shortName` | string \| null | optional | Short alias with leading dash, e.g. `-o`. |
| `valueType` | string | ✅ | One of `string`, `number`, `boolean`, `path`, `array`. |
| `isRequired` | bool | ✅ | |
| `defaultValue` | string \| null | optional | |
| `allowedValues` | array of string | optional | Enum-like constraints. |

### `examples[]`

| Field | Type | Required |
| --- | --- | --- |
| `description` | string | ✅ |
| `command` | string | ✅ |

### `sections[]`

| Field | Type | Required |
| --- | --- | --- |
| `title` | string | ✅ |
| `body` | string | ✅ (markdown) |

## Compatibility

clidoc checks the **major** version of `schemaVersion`. A clidoc release that
supports `1.x` will accept any `1.0`, `1.1`, `1.2`, etc. document. A `2.x` document
would need a clidoc release that explicitly supports it.

## Hand-rolling a `commands.json`

You don't have to use `Clidoc.SystemCommandLine` (or even .NET). Here is a minimal
document with a single command:

```json
{
  "schemaVersion": "1.0",
  "generatedAt": "2026-04-15T00:00:00Z",
  "generator": "my-custom-script",
  "commands": [
    {
      "id": "demo",
      "name": "demo",
      "fullName": "demo",
      "description": "A minimal demo CLI",
      "isGroup": false,
      "isRoot": true,
      "depth": 0,
      "parentId": null,
      "arguments": [],
      "options": [],
      "children": []
    }
  ]
}
```

Pass it to clidoc: `clidoc generate demo.json`.
