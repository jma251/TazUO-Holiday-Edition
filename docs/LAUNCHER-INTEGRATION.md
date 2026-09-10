# Adding TazUO Holiday Edition to the launcher

Handoff notes for whoever is working on the launcher. Everything below is
observed from the live repository, not planned — the release side has been
running since August, the dev side since 2026-09-10.

## What is being added

Two channels, from one public repository. They are separate releases and must
stay separate: nobody should land on a dev build by accident.

| Channel | Show it as | Who it is for |
| --- | --- | --- |
| **Holiday Edition** | stable / default | everyone |
| **Holiday Edition (dev)** | opt-in, marked unstable | testing only |

## Repository

```
https://github.com/jma251/TazUO-Holiday-Edition
```

**Public.** No token, no auth. The release assets were fetched anonymously to
confirm this — `HTTP 200`, ~67 MB. Default branch is `release`.

## Where each channel lives

### Holiday Edition (stable)

| | |
| --- | --- |
| Release tag | `latest` |
| Asset | `TazUO-Holiday-Edition.zip` |
| Direct download | `https://github.com/jma251/TazUO-Holiday-Edition/releases/download/latest/TazUO-Holiday-Edition.zip` |
| API | `https://api.github.com/repos/jma251/TazUO-Holiday-Edition/releases/tags/latest` |
| Marked | normal release, carries the "Latest" badge |

The `latest` release is **deleted and recreated** on every release build, so the
download URL above never changes but the file behind it does. Do not cache the
asset id; re-read the release.

Every release is also published a second time under a permanent tag —
`v4.5.2301`, `v4.5.2302`, … — which is never updated or deleted. Those are the
rollback history. If the launcher ever offers "reinstall an older version", that
is the list to read (`/releases`, filter to tags matching `v<x>.<y>.<z>`).

### Holiday Edition (dev)

| | |
| --- | --- |
| Release tag | `dev-latest` |
| Asset | `HolidayEdition-Dev.zip` |
| Direct download | `https://github.com/jma251/TazUO-Holiday-Edition/releases/download/dev-latest/HolidayEdition-Dev.zip` |
| API | `https://api.github.com/repos/jma251/TazUO-Holiday-Edition/releases/tags/dev-latest` |
| Marked | **`prerelease: true`** |

Replaced on every push to the `legacy-dev` branch, which can be several times a
day. It is a real, fully packaged client — same build, same native-library
checks — it is just unproven.

## Versioning

Holiday continues TazUO's numbering rather than restarting:

| | |
| --- | --- |
| TazUO base | `4.5.23` |
| First Holiday release | `4.5.2301` |
| Then | `4.5.2302`, `4.5.2303`, … |
| If TazUO moves to `4.5.24` | Holiday rebases to `4.5.2401` |

**It is an ordinary three-part version and sorts correctly with normal version
comparison.** `4.5.2302` > `4.5.2301` > `4.5.23`. No custom parsing needed.

> Historical note, in case old tags are seen: releases before this scheme were
> tagged `v4.5.23-h1` … `v4.5.23-h73`. **Do not try to compare those** — sorted
> as text, `h73` lands below `h9`. They are kept only as rollback history and
> should be filtered out of any "newest version" logic. Match on
> `v<digits>.<digits>.<digits>` and reject anything with `-h<number>` on the end.

### How to read the installed version

The client writes **`v.txt`** next to `ClassicUO.exe`. It contains the bare
version and nothing else:

```
4.5.2301
```

Nothing inside the client reads this file — it exists specifically for a
launcher to compare against. It is written on every build and publish, so it is
always present and always current.

**Dev builds cannot be told apart by `v.txt`** — a dev build cut from `4.5.2301`
also reports `4.5.2301`, because it is that version plus untested commits. If
the launcher needs to know a dev build is installed, it has to record which
channel it installed rather than infer it from the file. Recording the channel is
the recommended approach in any case, since a user switching stable → dev and
back needs the launcher to know which one it is looking at.

The binary itself carries a fuller stamp readable from the file properties'
`ProductVersion` / `InformationalVersion`:

| Build | Stamp |
| --- | --- |
| Release | `v4.5.2301` |
| Dev | `v4.5.2301-dev.45fe491` — the trailing part is the git commit |

The dev stamp is also shown in-client and in crash logs, so a bug report from a
dev build names the exact commit it came from.

### Update policy — the two channels behave differently on purpose

**Version-tracked auto-update applies to the stable channel only.**

| | Stable | Dev |
| --- | --- | --- |
| Update check | **Yes** — this is the channel the launcher follows | **No** |
| How the user gets it | Offered automatically when the version rises | **Manual — the user picks "install latest dev"** |
| Audience | everyone | the maintainer |

**Stable:** read the `latest` release, compare its version against `v.txt`, and
offer the update when it is higher. Ordinary three-part version comparison; see
the versioning section above.

**Dev:** do not poll it, do not offer it, do not nag. Expose it as an explicit
action the user chooses — "install the latest dev build" — which fetches
`dev-latest` and installs it. That is the only way it should ever arrive.

Two reasons this is not just a preference:

- **A dev build's version is usually unchanged.** It is cut from the current
  release plus untested commits, so `v.txt` still reads `4.5.2301` even when the
  build is hours old. Version comparison would miss nearly every dev update, and
  an auto-updater would either do nothing or thrash.
- **Dev builds are replaced on every push to `legacy-dev`**, several times a day.
  Nothing should be chasing that automatically.

If the launcher wants to show whether an installed dev build is current, compare
the release's `published_at` or the `-dev.<sha>` stamp — but as information, not
as an update prompt.

### Switching between channels

A user who installs a dev build and later wants the stable one is going
*backwards* in some sense but not in version number — both may read `4.5.2301`.
So an update check alone cannot get them back. Whichever way the launcher records
the installed channel, moving from dev to stable should be an explicit reinstall
of `latest` rather than something the version comparison is expected to notice.

## Packaging — the parts that break installs

### The zip is deliberately flat

Both zips contain `ClassicUO.exe` and its files **at the root, with no
containing folder**. This matches what the launcher already does for TazUO:
unzip straight into the target directory. Adding a wrapper folder would produce
`<launcher>/TazUO/TazUO/` and the client would not be found.

(Windows naming the extracted folder after the zip when a user double-clicks it
is Windows' own behaviour, not something in the archive.)

### Native libraries must survive repackaging

The client will not start without three native libraries: **`SDL2.dll`,
`FNA3D.dll`, `FAudio.dll`**. Missing any one produces a client that exits
instantly with no error message and no log — it looks like a corrupt download.

In both zips these are shipped **twice**: once beside `ClassicUO.exe`, and once
in an `x64/` subfolder. That redundancy is intentional — the client calls
`SetDllDirectory(<exe path>\x64)` at startup, but the copies beside the exe mean
it still works if the `x64/` folder is lost.

**If the launcher repackages, moves, or flattens anything, either the `x64/`
folder must survive or the copies beside the exe must.** Both builds hard-fail
in CI if any of the three is missing, so a published zip always has them.

### Asset naming

The launcher's existing fallback is *"any asset whose filename starts with
`TazUO`"*. The dev zip is named `HolidayEdition-Dev.zip` specifically so it can
never be picked up by that rule. Please keep selecting by exact filename per
channel rather than by prefix:

- stable → `TazUO-Holiday-Edition.zip`
- dev → `HolidayEdition-Dev.zip`

## Things to be aware of

- **The `dev-latest` tag is not called `dev`** on purpose. A tag named `dev`
  would sit alongside the repository's inherited `dev` branch and make refs
  ambiguous.
- **Dev builds are frequent.** If the launcher polls, the dev channel will show
  a new build far more often than the stable one. Consider not nagging on it.
- **The stable channel is cut from the `release` branch**, which carries only
  work that has been run and confirmed. Investigation tooling, diagnostic logs
  and unfinished experiments are compiled out of it entirely — they exist only in
  the dev build.
- **Client identity.** Both channels report as
  `TazUO [Legacy] - <version> - Holiday Edition` in the window title. It is a
  .NET Framework 4.7.2, x64-only build — there is no x86 or ARM variant, so
  platform-suffix selection has exactly one answer on Windows, and there are no
  Linux or macOS builds published.

## Questions worth settling before wiring this up

1. Does the launcher store which channel it installed? If not, switching between
   stable and dev cannot be detected reliably (see `v.txt` above).
2. Should the dev channel be hidden behind a toggle, or listed openly?
3. Should the launcher offer the permanent `v<x>.<y>.<z>` releases for rollback,
   or only ever the newest?
