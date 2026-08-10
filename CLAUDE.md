# TazUO Holiday Edition — Guide for Claude

Read this first. It describes **this fork**, which is not the same as upstream
TazUO and has one hard constraint that is easy to break by accident.

## What this repository is

A private personal fork of [TazUO](https://github.com/PlayTazUO/TazUO), which is
itself a fork of ClassicUO — an open-source reimplementation of the Ultima
Online Classic Client, written in C# on top of FNA (an XNA reimplementation).

## Branches: `legacy` is the one that matters

| Branch | Framework | Version | Role |
| --- | --- | --- | --- |
| **`legacy`** | .NET Framework **4.7.2** (`net472`) | 4.5.23 | **The only branch that is developed, built, or released here.** |
| `main` | .NET **10** (`net10.0`) | 5.24.5 | Reference only — a mirror of upstream, kept so fixes can be read out of it. |

**Rules:**

- All work happens on `legacy`. Branch from it, and merge back into it.
- **Never build, modify, or release `main`.** It exists to be read.
- Feature branches should be cut from `legacy`, never from `main`.

### Porting a fix from `main` to `legacy`

This is the main reason `main` is present. It is rarely a clean cherry-pick,
because the two branches have diverged structurally:

- `main` targets `net10.0`; `legacy` targets `net472`. Modern C#/BCL APIs that
  compile on `main` may not exist on 4.7.2.
- `main` keeps its MSBuild config at `src/Directory.Build.props`; `legacy` keeps
  it at the repo root as `Directory.Build.props`.
- The two are ~1 major version apart (4.5.x vs 5.24.x), so surrounding code
  often differs.

Expect to read the change on `main` and **re-apply it by hand** to `legacy`,
rather than cherry-picking the commit.

## The .NET Framework 4.7.2 constraint

Set in `Directory.Build.props` at the repo root:

```xml
<TargetFramework>net472</TargetFramework>
<PlatformTarget>x64</PlatformTarget>
```

This is deliberate — 4.7.2 is what keeps old-style plugins working. When writing
code for this branch:

- **Do not raise `TargetFramework`.** Not to `net8.0`, not to `net10.0`.
- Only use language/library features available on .NET Framework 4.7.2. Newer
  BCL surface reachable on `main` is frequently absent here. Span/Memory APIs
  are available, but only via the `System.Memory` / `System.Buffers` NuGet
  packages already referenced in `Directory.Build.props`.
- Builds are **x64 only**. There is no AnyCPU or x86 configuration.
- `System.Text.Json` is pinned to `8.0.5` (the last version supporting 4.7.2).
  Per repo convention, every JSON serialize/deserialize needs a generated
  serializer context.

## Layout

```
ClassicUO.sln                     # solution; Debug/Release x64
Directory.Build.props             # net472 + x64 + shared package refs  <-- the constraint lives here
src/
  ClassicUO.Client/               # the executable -> ClassicUO.exe
    ClassicUO.Client.csproj       #   AssemblyName is "ClassicUO", not "ClassicUO.Client"
    Main.cs                       #   entry point; calls SetDllDirectory("x64") for natives
    DllMap.cs                     #   maps managed names -> native .dll/.so/.dylib
    Game/                         #   game systems, managers, UI gumps
    Network/                      #   packet handlers + outgoing packets
    LegionScripting/              #   custom scripting language + IronPython API
  ClassicUO.Assets/               # UO file-format loaders (art, anims, maps, sounds)
  ClassicUO.Renderer/             # FNA-based rendering
  ClassicUO.IO/                   # low-level file I/O
  ClassicUO.Utility/              # shared helpers
  APIToMarkdown/                  # generates scripting API docs
external/
  FNA/                            # git submodule
  MP3Sharp/                       # git submodule
  x64/                            # Windows natives: SDL2.dll, FNA3D.dll, FAudio.dll, ...
  lib64/                          # Linux natives
  osx/                            # macOS natives
  iplib/                          # IronPython runtime, copied next to the exe
tests/ClassicUO.UnitTests/        # MSTest
tools/                            # monokickstart, ManifestCreator
```

Submodules are required. After a fresh clone:

```bash
git submodule update --init --recursive
```

## Build

From the repository root:

```bash
# Build everything
dotnet build -c Release

# Build just the client, laid out ready to run (this is what CI does)
dotnet publish src/ClassicUO.Client/ClassicUO.Client.csproj -c Release -o bin/dist -p:IS_DEV_BUILD=true

# Tests
dotnet test tests/ClassicUO.UnitTests/
```

Notes:

- Output goes to `bin/Release/` (build) or `bin/dist/` (publish).
- A modern .NET SDK (8.x is what CI uses) builds the `net472` target fine; the
  SDK version and the target framework are separate things.
- `-p:IS_DEV_BUILD=true` switches `OutputType` to `WinExe`, so the client runs
  without a console window attached.
- **Building on Linux/macOS does not fully work.** `ClassicUO.Client.csproj` has
  hardcoded `HintPath`s into `Program Files (x86)\Reference Assemblies` for
  `System.Net.Http` and `System.Windows.Forms`. Build on Windows.

### Native libraries — the thing that breaks launches

The client is useless without the FNA natives: **SDL2, FNA3D, and FAudio**.

They are not NuGet packages. They live in `external/x64/` (Windows) and are
copied into the output by the `CopyExternalDeps_build` / `CopyExternalDeps_publish`
targets at the bottom of `ClassicUO.Client.csproj`, which place them in an
**`x64/` subfolder** of the output — not next to the exe.

At runtime `Main.cs` calls `SetDllDirectory(<exe path>\x64)` so Windows can find
them there. This means: **if you repackage, move, or flatten the output, the
`x64/` folder must survive, or the natives must sit beside `ClassicUO.exe`.**
Losing them produces a client that exits immediately on launch with no useful
error. The release workflow copies them to both places for safety and hard-fails
the build if any of the three is missing.

## CI / releases

`.github/workflows/build-legacy.yml` is the one that matters for this fork:

- Triggers on push to `legacy`, or manually from the Actions tab.
- Builds on `windows-latest`, checks out submodules recursively, publishes the
  client, verifies the natives are present, and zips `bin/dist` into
  `TazUO-Holiday-Edition.zip`.
- Needs `permissions: contents: write` to manage releases and tags.

**It is the only workflow here that publishes a release automatically, and it
should stay that way.**

### Two releases per build

Every build publishes the same zip twice, to two different releases:

| Release | Tag | Lifetime | Purpose |
| --- | --- | --- | --- |
| Rolling | **`latest`** | Deleted and recreated each build | Fixed download URL, carries the "Latest" badge |
| Permanent | **`v<base>-h<N>`** e.g. `v4.5.23-h4` | Never updated, never deleted | Rollback history |

`<base>` is TazUO's version, read from `ClassicUO.Client.csproj` as before. `<N>`
is the Holiday increment, and it is **not stored anywhere in the repo** — it is
derived at build time from the tags that already exist:

```bash
git tag -l "v${VERSION}-h*"   # highest N wins, +1 for this build
```

That keeps the counter durable (tags are never pruned) without the workflow
having to commit a counter file back to `legacy`, which would retrigger itself.

Two consequences worth knowing:

- **Numbering is per base version.** If TazUO's version moves to 4.5.24, the next
  Holiday build is `v4.5.24-h1`, not a continuation of the 4.5.23 series.
- **Re-running a build for an already-tagged commit does not mint a new number.**
  The workflow detects a Holiday tag on `HEAD` and skips the permanent release;
  `latest` still refreshes. This stops manual re-runs filling the releases page
  with identical entries.

Both releases' notes carry the commit SHA and the commits since the previous
Holiday tag, so the releases page reads as a running changelog.

Only the `latest` tag is ever deleted (`gh release delete latest --cleanup-tag`).
**Holiday tags and releases must never be pruned** — they are the rollback
history. `makeLatest: false` on the permanent release keeps the "Latest" badge
on `latest`.

The other deploy workflows (`net472-deploy.yml`, `net9-deploy.yml`,
`tuo-deploy.yml`, `tuo-dev-deploy.yml`) are inherited from upstream and target
upstream's repo/Discord. They have been deliberately reduced to
`workflow_dispatch:` only — **do not re-add their `workflow_run:` triggers.**
They used to chain off `Build-Test` completing:

- `net472-deploy.yml` fired on `legacy`, which would double-build every push and
  publish a competing `TazUO-Legacy` release alongside `latest`.
- `tuo-deploy.yml` fired on `main` — the branch that must never be built — and
  published with `makeLatest: true`, which would steal the "Latest" badge from
  the legacy release.
- `tuo-dev-deploy.yml` fired on `dev`, which is not a release path here.

`Build-Test` still runs on every push and PR. That is intentional: it only
compiles and uploads artifacts, and never publishes a release.

### Zip layout — deliberately flat, do not "fix" it

`build-legacy.yml` zips the **contents** of `bin/dist`, so `ClassicUO.exe` and
friends sit at the root of the zip with no containing folder. That is what the
launcher expects: it unzips straight into `<launcher dir>/TazUO`. Adding a
`TazUO/` folder inside the zip would produce `<launcher>/TazUO/TazUO/` and the
launcher would not find the client.

Windows naming the extracted folder after the zip file when you double-click it
is Windows' own behaviour, not something to work around in the build.

This will be revisited when the launcher fork is worked on — the likely end
state is a `TazUO/` folder inside the zip **plus** a matching launcher change to
step into it. That decision belongs with the launcher. Do not pre-empt it here.

One related constraint: the launcher picks its download by platform zip suffix,
falling back to **any asset whose filename starts with `TazUO`**. If a second zip
is ever attached to a release, it must **not** start with `TazUO`, or asset
selection becomes ambiguous.

## Conventions

- Match the surrounding code's style, naming, and comment density.
- Do not add license headers to new files.
- Every JSON serialize/deserialize needs a generated serializer context.
- Scripting features should ideally be exposed to both Legion Script and Python.
