# TazUO Holiday Edition — Guide for Claude

Read this first. It describes **this fork**, which is not the same as upstream
TazUO and has one hard constraint that is easy to break by accident.

## What this repository is

A personal fork of [TazUO](https://github.com/PlayTazUO/TazUO), which is
itself a fork of ClassicUO — an open-source reimplementation of the Ultima
Online Classic Client, written in C# on top of FNA (an XNA reimplementation).

## Branches: `legacy` develops, `release` ships

Two branches, and both are this fork's. The other two came with it.

| Branch | Framework | Version | Role |
| --- | --- | --- | --- |
| **`legacy`** | .NET Framework **4.7.2** (`net472`) | 4.5.2301 | **Development.** Where work goes. Also the default branch, and where the `v4.5.23-h1`…`h73` tags point. |
| **`release`** | .NET Framework **4.7.2** (`net472`) | 4.5.2301 | **Release.** What other people download. Only tested, confirmed work lands here, and landing here *is* the release. |
| `main` | .NET **10** (`net10.0`) | 5.24.5 | Upstream's. Reference only — a mirror kept so fixes can be read out of it. |
| `dev` | .NET **10** (`net10.0`) | 5.24.5 | Upstream's. Not used here, not built, not a release path. |

There was briefly a third branch of ours, `legacy-dev`, created on 2026-09-10
when `legacy` still shipped. `release` was added hours later and `legacy-dev`
kept a name describing a branch it no longer fed. It was folded back into
`legacy` on 2026-09-12 and removed. Nothing was lost: it was a fast-forward.

**Rules:**

- Work goes on **`legacy`**, as commits. A separate branch per change is not
  the convention here - it produced ninety-odd leftovers that nothing deleted.
- `legacy` merges into `release` **only** when the work has been built, run,
  and confirmed good. That merge publishes to real users, so it is not a routine
  step — it is the decision to ship, and it needs a version bump to go with it.
- **Never commit directly to `release`.** Everything reaches it through a merge
  from `legacy`.
- **Never build, modify, or release `main` or `dev`.** They exist to be read.
- Keep `legacy` current with `release` (merge `release` in) so the two do not
  drift; a release cut from a stale dev branch silently reverts things.

Dev-only work does **not** need holding back from `release` by hand. Anything
behind `HOLIDAY_DEV` is compiled out of the release build wherever it lands, so
the branches stay mergeable rather than diverging. See the flag's description in
`Directory.Build.props`.

The one exception to going through `legacy`: a fix for something that is
broken *in the wild right now*. Those may go straight to a branch off `release`,
because routing an emergency through a dev branch full of untested work would
ship that work alongside it. Merge `release` back down into `legacy`
afterwards.

### Porting a fix from `main` to the 4.7.2 branches

This is the main reason `main` is present. It is rarely a clean cherry-pick,
because the two branches have diverged structurally:

- `main` targets `net10.0`; the 4.7.2 branches target `net472`. Modern C#/BCL APIs that
  compile on `main` may not exist on 4.7.2.
- `main` keeps its MSBuild config at `src/Directory.Build.props`; here it is at
  the repo root as `Directory.Build.props`.
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

Two workflows build this fork, one per branch. They are deliberately separate:
the dev one must never be able to touch what users download.

`.github/workflows/build-legacy.yml` — **the release path**:

- Triggers on push to `release`, or manually from the Actions tab.
- Builds on `windows-latest`, checks out submodules recursively, publishes the
  client, verifies the natives are present, and zips `bin/dist` into
  `TazUO-Holiday-Edition.zip`.
- Publishes the rolling `latest` release **and** a permanent `v<version>` one.
- Needs `permissions: contents: write` to manage releases and tags.

**It is the only workflow here that publishes a release to users, and the only
one that publishes a numbered version. It should stay that way.**

`.github/workflows/build-legacy-dev.yml` — **the testing path**:

- Triggers on push to `legacy`, or manually.
- Same build and the same hard-fail check on the natives, so a dev build is a
  real, launchable client and not a half-packaged one.
- Publishes to a single **prerelease** tagged `dev-latest`, replaced every build.
  It does **not** touch `latest` and is **never numbered**, so the version line
  only ever moves when a release is cut.
- The zip is `HolidayEdition-Dev.zip`. The name deliberately avoids the `TazUO`
  prefix — the launcher falls back to picking any asset starting with that, and
  a dev zip must never be selectable as a release download.
- The tag is `dev-latest`, not `dev`, because a tag named `dev` would collide
  with the inherited `dev` branch and make `git checkout dev` ambiguous.
- Stamps the commit into the binary as `v<version>-dev.<short sha>`, so a crash
  log from a dev build says which dev build. The leading `v` is load-bearing:
  `CUOEnviroment.ReadBuildTag` uses it to tell a stamped build from an unstamped
  one, and reads anything without it as `local build`.

Both run only on their own branch, and their `concurrency` groups are separate,
so a dev build can never cancel a release build.

### Versioning — Holiday continues TazUO's numbering

Holiday Edition is a fork with its own version line that **starts from TazUO's
number instead of restarting at 1**. Taz is on `4.5.23`; Holiday builds on that
base read `4.5.23xx`:

| | |
| --- | --- |
| TazUO base | `4.5.23` |
| First Holiday release | `4.5.2301` |
| Then | `4.5.2302`, `4.5.2303`, … |
| If Taz moves to `4.5.24` | rebase to `4.5.2401` |

Two things this buys, both deliberate:

- **A launcher can compare it.** `4.5.2302` sorts above `4.5.2301` under ordinary
  version rules, so "is there an update?" needs no custom logic. The retired
  `-hN` scheme could not do this: sorted as text, `h73` came out *below* `h9`.
- **It cannot collide with upstream.** Taz would have to reach patch `2301` for
  two different clients to claim one number, and the base version stays readable
  inside it.

**The version lives in exactly one place**: `<AssemblyVersion>` (and
`<FileVersion>`) in `src/ClassicUO.Client/ClassicUO.Client.csproj`. Everything
downstream follows it — the window title, the login-screen label, the crash-log
header, the release tag, and `v.txt` (see below).

**Bumping that file is how a release is cut.** Nothing auto-increments. Merging
to `release` without a bump refreshes `latest` and publishes no new numbered
release, and the workflow says so in its log rather than failing. That is the
point: a release should be a decision, not a side effect of merging.

The retired `v4.5.23-h1` … `v4.5.23-h73` tags stay exactly where they are as
rollback history. **Never prune them.** The counter simply stops at 73.

#### `v.txt` — how a launcher reads the installed version

`ClassicUO.Client.csproj` writes `v.txt` next to the exe on both build and
publish, containing the bare `AssemblyVersion` (e.g. `4.5.2301`). **Nothing in
the client reads it.** It exists for outside consumers — a launcher comparing
what is installed against what is published. It follows `AssemblyVersion`
automatically, so it needs no separate maintenance, but do not remove those two
targets: a launcher depends on that file existing.

#### Two releases per numbered build

Every release build publishes the same zip twice:

| Release | Tag | Lifetime | Purpose |
| --- | --- | --- | --- |
| Rolling | **`latest`** | Deleted and recreated each build | Fixed download URL, carries the "Latest" badge |
| Permanent | **`v<version>`** e.g. `v4.5.2301` | Never updated, never deleted | Rollback history |

Dev builds publish once, to the **`dev-latest`** prerelease, and are replaced
every push. They are never numbered.

Only `latest` and `dev-latest` are ever deleted
(`gh release delete <tag> --cleanup-tag`). Numbered releases are immutable.
`makeLatest: false` on the permanent release keeps the "Latest" badge on
`latest`.

Both releases' notes carry the commit SHA and the commits since the previous
release, so the releases page reads as a running changelog.

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

`features-bot.yml` and its `FeaturesBot.py` were **deleted** rather than reduced.
That one was not a deploy path at all — it posted feature advertisements to
upstream's Discord on a twice-daily cron, so in this fork it woke up at 07:00 and
19:00 to fail on a `DISCORD_WEBHOOK` secret that will never exist here. There is
no Discord to announce to, so there is nothing to keep.

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
