# NetExtension.OpenAPI.SdkGen

A code generator that produces strongly-typed API client SDKs for .NET APIs.

**The source of truth is the annotations on the API's own handler methods**, read by reflection over
the compiled assembly. No OpenAPI or Swagger document takes part in the pipeline — there is no spec
generation, no spec parsing, and no dependency on Swashbuckle, NSwag or Kiota. The repository name is
historical branding only.

Why: a spec-first generator needs a document round-trip, and that document drifts from the API. When
the declared verb and route live on the handler itself, discovery never depends on how the endpoint
was mapped, and the generator's input is just "a compiled assembly plus options".

## Status

Built phase by phase under strict TDD. What is in the tree today:

| Phase | Scope | State |
|---|---|---|
| 0 | Toolchain, solution scaffolding, coverage gate | Done |
| 1 | `Core` domain model — no reflection, no I/O | Done |
| 2 | `Scanning` — assembly → SDK model by reflection | Not started |
| 3 | `Emit.CSharp` — SDK model → C#/Refit source | Not started |
| 4 | Contract tests — emit, compile with Roslyn, reflect | Not started |
| 5 | `Cli` + MSBuild targets + CI determinism gate | Not started |
| 6 | Sample API, generated client, consumer app | Not started |

Ports (`IAssemblyScanner`, `ISdkEmitter`, `IFileWriter`) and the `GenerateSdkUseCase` are
deliberately **not** in the tree yet: their signatures are determined by the adapters in phases 2–3
and the CLI wiring in phase 5, so writing them now would be guesswork rather than test-driven.

## Prerequisites

- **.NET SDK 10.0** (`net10.0`). Verified against SDK **10.0.112** with the ASP.NET Core **10.0.12**
  runtime.
- A git client.

### IDE support

`net10.0` projects **do not load in Visual Studio 2022**. Use one of:

| IDE | Minimum version |
|---|---|
| Visual Studio | 2026 (18.0) or later |
| JetBrains Rider | 2025.3 or later |
| VS Code | with the C# Dev Kit extension |

The solution is a classic `.sln` with `src`, `samples` and `tests` solution folders.

The generated client and the annotations package multi-target `netstandard2.0;net10.0`, so a
*consumer* of the generated SDK can stay on .NET Framework 4.8 / WinForms and older tooling. Only
this repository's own projects need the modern IDE.

## Installing dependencies

### Linux (Ubuntu 24.04)

The SDK is in Ubuntu's own archive:

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0
dotnet --version   # 10.0.112
```

> Refresh the index first. Installing against a stale index fails with `404 Not Found` on the
> `dotnet-sdk-10.0` `.deb`, because the pool has already moved to a newer patch.

### Windows / macOS

Install the .NET 10 SDK from <https://dotnet.microsoft.com/download/dotnet/10.0>.

### Restoring packages

```bash
dotnet restore
```

`NuGet.config` clears inherited sources and uses only nuget.org, so a machine-level feed cannot
change a restore. Package versions are centrally managed in `Directory.Packages.props`; project
files carry no version numbers.

## Building, testing, packing

```bash
# Build everything (warnings are errors)
dotnet build

# Run the test suite
dotnet test

# Run it with the coverage gate enforced
dotnet test /p:CollectCoverage=true

# Produce NuGet packages into ./artifacts/packages
dotnet pack src/NetExtension.OpenAPI.SdkGen.Core -o artifacts/packages
```

Publish and SDK-generation commands arrive with phase 5 and are documented here when they work.

### Coverage gate

The bar is **100% line and 90% branch**, measured over this repository's own assemblies and enforced
by coverlet: `dotnet test /p:CollectCoverage=true` exits non-zero below the threshold. Coverage is
off by default so that a plain `dotnet test` stays fast. Reports are written to
`artifacts/coverage/<project>/` as Cobertura and JSON.

Current: `NetExtension.OpenAPI.SdkGen.Core` — **100% line, 100% branch**, 40 tests.

The CLI's process entry point will be excluded with `[ExcludeFromCodeCoverage]` in source rather
than by a filter, so the exclusion is visible where the code lives.

## Versioning

CalVer, from `Directory.Build.props`:

```
$(CalVerEpoch).$(BuildNumber)     e.g. 2026.9.0
```

`CalVerEpoch` is bumped per release month. `BuildNumber` comes from `GITHUB_RUN_NUMBER` in CI and
defaults to `0` locally. Override it with `-p:BuildNumber=…`.

This version applies to the **engine** packages. The version stamped into a *generated* client
project is a generator parameter with its own stable default, so regenerating an unchanged API
produces no diff regardless of which build number is in effect.

Packages are built with `dotnet pack` and are never pushed to a feed from this repository.

## Design decisions worth knowing

**Refit is pinned to 11.2.0.** It is the last release that ships **both** a `netstandard2.0` and a
`net10.0` asset. Refit 12 and later dropped `netstandard2.0`, which the generated client needs so it
can be consumed from .NET Framework 4.8 without a DI container. Refit over a hand-emitted
`HttpClient` because the verb and route template become a declarative interface attribute that the
contract tests can assert directly, instead of URIs string-built inside emitted method bodies. Do not
bump this pin without re-checking that constraint.

**Method naming.** The declared operation name is PascalCased into an identifier. When a handler
declares no operation name, the fallback is the verb followed by the route's segments, with a
`{token}` rendered as `By` plus the token name — `GET /orders/{id}` becomes `GetOrdersById`.
Collisions are resolved by giving the first claimant the bare name and later ones the lowest free
numeric suffix (`GetOrder`, `GetOrder2`, `GetOrder3`), which also holds when a suffixed name would
itself collide with one declared explicitly. Same input, same output, every run.

**Stream beats a declared body.** An operation flagged as streaming returns a `Stream` even if it
also declares a response body type: the caller asked for the bytes, not for a deserialized object.

## Troubleshooting

**`dotnet: command not found` after install** — the package installs to `/usr/lib/dotnet`. Open a new
shell, or add it to `PATH`.

**`404 Not Found` fetching `dotnet-sdk-10.0`** — stale apt index. Run `sudo apt-get update` first.

**The SDK download host is unreachable** — `dotnet-install.sh` pulls from
`builds.dotnet.microsoft.com`. Behind a restrictive egress policy that host is often blocked while
the distro archive is not; on Ubuntu, prefer `apt-get install dotnet-sdk-10.0` as above.

**A build fails on a warning** — `TreatWarningsAsErrors` is on repository-wide. Fix the warning
rather than suppressing it; that includes unresolved XML-doc `cref`s.

**`dotnet test` reports no coverage numbers** — coverage is opt-in. Pass
`/p:CollectCoverage=true`.

**A restore resolves an unexpected package version** — versions live only in
`Directory.Packages.props`. A `Version` attribute on a `PackageReference` is an error under central
package management.
