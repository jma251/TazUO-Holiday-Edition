# Why this fork is on net472, and what it would take not to be

Recorded 2026-09-14. Read from source, not from recollection. Includes two
corrections to claims made earlier in the same session.

## The runtimes, as they actually are

| | framework | shipped as |
| --- | --- | --- |
| ClassicUO `main` / `beta` | **net10.0** | net472 bootstrap `.exe` + the client AOT'd to a **native shared library** |
| TazUO `main` | **net10.0** | plain managed `WinExe`, `PublishAot=false` |
| TazUO `legacy` | **net472** | frozen at `c5b59c8737`, 2026-08-05 - our fork base |
| **Holiday Edition** | **net472** | the only net472 client in this family still being developed |
| RazorEnhanced | **v4.7.2** | `release/1.0`, July 2026 |

## The mechanism, in one line

`Assembly.LoadFile` can only load an assembly into its own runtime. RazorEnhanced
is a .NET Framework assembly, so it loads into a .NET Framework process and
nothing else.

The plugin **contract** is runtime-agnostic and always was. `PluginHeader` is a
flat struct of ~30 `IntPtr` function pointers plus blittable ints; nothing
managed crosses it. The client fills in its half, hands over `void* func =
&header`, and the plugin writes its half. There are two load paths:

```csharp
IntPtr assptr = Native.LoadLibrary(PluginPath);               // 1. native DLL
IntPtr installPtr = Native.GetProcessAddress(assptr, "Install");
...
Assembly asm = Assembly.LoadFile(PluginPath);                 // 2. managed
Type type = asm.GetType("Assistant.Engine");
MethodInfo meth = type.GetMethod("Install", Public | Static);
meth.Invoke(null, new object[] { (IntPtr)func });
```

RazorEnhanced has no native `Install` export, so it always lands on path 2. Path
2 is the whole constraint.

## ClassicUO solved this and still ships the solution

`src/ClassicUO.Bootstrap` is present on `cuo/main` today:

```xml
<OutputType>Exe</OutputType>
<TargetFramework>net472</TargetFramework>
<AssemblyName>ClassicUO</AssemblyName>
```

**The executable a player launches is a .NET Framework 4.7.2 process.** Its
`deploy.yml` builds two halves into one folder:

```
Build Bootstrap app:  dotnet publish src/ClassicUO.Bootstrap/... /p:OutputType=WinExe
Build ClassicUO:      dotnet publish src/ClassicUO.Client/...    /p:NativeLib=Shared /p:OutputType=Library
```

The bootstrap `LoadLibrary`s the net10 client, which is AOT-compiled to native
code with no managed entry point. **RazorEnhanced is loaded by the bootstrap** -
same runtime, so path 2 succeeds - and the two halves then call each other
through `PluginHeader` / `HostBindings` function pointers across the native
boundary.

So RE works on a .NET 10 ClassicUO **today**, with no change to RE, and always
could have.

## Why it does not work on TazUO main

TazUO deleted the bootstrap. `1e87db16da`, **2025-09-11**, "Remove unused
bootstrap project and other unused scripts and files" - `Program.cs`,
`Plugin.cs` (650 lines), `LibraryLoader.cs`, `CuoInternal.cs`. Three months
before the move to net10 on 2025-12-17.

The client half survives: `taz/main` still has `PluginHost.cs` with the
`HostBindings` / `ClientBindings` structs. Only the host was removed.

So RE does not run on modern TazUO because of a **deletion**, not because of
.NET 10.

## Two corrections to earlier claims in this session

**1. "RazorEnhanced has a .NET 9 version."** Wrong. `RazorEnhanced/ClassicUO` is
that organisation's fork of the *client*, and its `net9.0` target is inherited
from upstream CUO. Their branches:

| branch | target | props location |
| --- | --- | --- |
| `main` | net9.0 | `src/` - upstream layout, no RE-specific commits at all |
| **`build47`** | **net472** | repo root - the pre-net8 layout, the one they build |

RazorEnhanced itself is `v4.7.2` on `release/1.0` (2026-07-16) and
`release/0.8`. Both fork branches stopped 2025-03-20.

**2. "AOT imposes constraints and IronPython is unlikely to survive it"** - said
as though it described modern TazUO. It does not. `taz/main` sets
`<PublishAot>false</PublishAot>` and `<OutputType>WinExe</OutputType>`. AOT
belongs to ClassicUO's build only.

The IronPython point is real but narrower than stated: CUO has **0** IronPython
or Legion files and can AOT freely; TazUO has **98**, plus
`<PackageReference Include="IronPython" Version="3.4.2" />` and the whole
`iplib` standard library copied beside the exe. IronPython is a DLR runtime and
builds code at runtime, which NativeAOT cannot do. **But that only bites if you
copy CUO's exact route.** The bootstrap has to load *something*; a native
library is one way, not the only one.

## What it would actually take

A net472 host process that loads RE the way it always has, bridged to a net10
client. Three shapes, none of which touch RazorEnhanced:

1. **CUO's shape** - AOT the client to a native library, `LoadLibrary` it from a
   net472 bootstrap. Proven, shipping. Costs IronPython.
2. **Side-by-side CLR hosting** - the net472 bootstrap hosts CoreCLR through the
   standard .NET hosting APIs and loads the managed net10 client in-process. No
   AOT, so Python survives.
3. **Out-of-process** - the client runs as its own net10 process and talks to a
   net472 assistant host over IPC. The commented-out
   `Client.Game.AssistantHost.Connect("127.0.0.1", 7777)` in `Plugin.cs`
   suggests someone started down this road.

All three are assembly rather than invention, and all three are a real project.

## Why the pin stands meanwhile

Every current client in this family is net10: ClassicUO, TazUO main, and the ECS
branch. `taz/legacy` has had no commit since 2026-08-05. **This fork is the only
net472 client still moving**, which is also why there is no upstream left to
inherit fixes from on this runtime - see `candidate-fixes.md`.

The pin is not nostalgia and not "staying behind Taz". It is the only runtime a
Framework assistant can load into, held by the only project still holding it.
