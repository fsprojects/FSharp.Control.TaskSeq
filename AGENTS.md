# AGENTS.md — FSharp.Control.TaskSeq

## Project Overview

FSharp.Control.TaskSeq is an F# library providing a `taskSeq` computation expression for `IAsyncEnumerable<'T>`, along with a comprehensive `TaskSeq` module of combinators. It targets `netstandard2.1`.

## Repository Layout

- `src/FSharp.Control.TaskSeq/` — Main library (netstandard2.1)
- `src/FSharp.Control.TaskSeq.Test/` — xUnit test project (net6.0)
- `src/FSharp.Control.TaskSeq.SmokeTests/` — Smoke/integration tests
- `src/FSharp.Control.TaskSeq.sln` — Solution file
- `Version.props` — Single source of truth for the package version
- `build.cmd` — Windows build/test script used by CI

## Build

The solution uses the .NET SDK. Restore tools first, then build:

```bash
dotnet tool restore
dotnet build src/FSharp.Control.TaskSeq.sln -c Release
```

Or use the provided script (Windows):

```cmd
./build.cmd            # default: release build
./build.cmd debug      # debug build
```

`build.cmd` modes: `build` (default), `test`, `ci`. Configurations: `release` (default), `debug`.

## Test

Tests use **xUnit** with `FsUnit.xUnit` assertions. The test project is at `src/FSharp.Control.TaskSeq.Test/FSharp.Control.TaskSeq.Test.fsproj`.

Run tests locally:

```bash
dotnet test src/FSharp.Control.TaskSeq.Test/FSharp.Control.TaskSeq.Test.fsproj -c Release
```

Or via the build script:

```cmd
./build.cmd test              # runs tests without TRX logging
./build.cmd test -debug       # debug configuration
./build.cmd ci                # CI mode: adds --blame-hang-timeout 60000ms and TRX logging
./build.cmd ci -release       # CI mode, release config
./build.cmd ci -debug         # CI mode, debug config
```

CI runs both debug and release test configurations on `windows-latest`. Test results are output as TRX files to `src/FSharp.Control.TaskSeq.Test/TestResults/`.

## Code Formatting

Formatting is enforced by **Fantomas** (version 6.3.0-alpha-004, configured as a dotnet local tool).

Check formatting (CI runs this on every PR):

```bash
dotnet tool restore
dotnet fantomas . --check
```

Apply formatting:

```bash
dotnet fantomas .
```

Fantomas settings are in `src/.editorconfig` under the `[*.{fs,fsx}]` section. Key settings:

- `max_line_length = 140`
- `indent_size = 4`
- `fsharp_space_before_parameter = true`
- `fsharp_space_before_lowercase_invocation = true`
- `fsharp_max_if_then_else_short_width = 60`
- `fsharp_max_record_width = 80`
- `fsharp_max_array_or_list_width = 100`

## CI Workflows

All workflows are in `.github/workflows/`:

| Workflow | File | Trigger | Purpose |
|---|---|---|---|
| **ci-build** | `build.yaml` | Pull requests | Verify formatting (`dotnet fantomas . --check`) then build release on Windows |
| **ci-test** | `test.yaml` | Pull requests | Run tests in both debug and release on Windows, upload TRX artifacts |
| **ci-report** | `test-report.yaml` | After `ci-test` completes | Publish test results via `dorny/test-reporter` |
| **Build main** | `main.yaml` | Push to `main` | Build + test release on Windows |
| **Publish** | `publish.yaml` | Push to `main` | Build, then push NuGet package (skip-duplicate) |

## Conventions

- F# source files use `.fs` extension; signature files use `.fsi`.
- `TreatWarningsAsErrors` is enabled for all projects.
- File ordering matters in F# — the `<Compile>` order in `.fsproj` files defines compilation order.
- The library targets `netstandard2.1`; tests target `net6.0` with `FSharp.Core` pinned to `6.0.1`.
- NuGet packages are output to the `packages/` directory.

## Release Notes

**Required**: Every PR that adds features, fixes bugs, or makes user-visible changes **must** include an update to `release-notes.txt`. Add a bullet under the appropriate version heading (currently `0.5.0`). The format is:

```
0.5.0
    - adds TaskSeq.myFunction and TaskSeq.myFunctionAsync, #<issue>
    - fixes <description>, #<issue>
```

If you are bumping to a new version, also update `Version.props`. PRs that touch library source (`src/FSharp.Control.TaskSeq/`) without updating `release-notes.txt` are incomplete.
