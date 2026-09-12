# Changelog

Notable changes to TazUO Holiday Edition.

Entries below 4.5.23xx are inherited from upstream TazUO.

---
## [4.5.2301]

First release of TazUO Holiday Edition. The version continues TazUO's
numbering from 4.5.23 rather than restarting, so a launcher can compare the
two and tell which is newer.

### Fixed

- Items could vanish from the world. A map cell kept pointing at an object
  that had already been removed.
- A house you were standing next to could lose its contents, and a custom
  house could be replaced by the generic shell a frame after it finished
  building.
- Several crashes: closing a window while its own children were still being
  walked, the walking step counter running past the end of its array,
  pathfinding, item selection under the mouse, and text that could not be
  drawn.
- Fonts no longer bring the client down over a glyph the atlas cannot fit, a
  missing font name, or a size too large to rasterize.
- Running two clients at once no longer trips over shared state in the
  language and static-filter files.
- A corrupt settings or profile file no longer stops the client starting.
- Stuck modifier keys after the window loses focus.

### Added

- A chat input font setting, separate from the rest of the interface.
- Nameplate font changes now apply to nameplates already on screen.
- The journal keeps 5000 entries, and the limit it reports is the real one.
- Drag select filters by notoriety, respects zoom, and lines its selection up
  with what the mouse actually picks.
- Music follows the region the server describes, stops when the server says
  the region is silent, ends the combat track when you leave war mode, and can
  be pointed at a different era's music folder.
- The window title and login screen name the build.

### Changed

- The client is built and published from a `release` branch, separate from
  where work happens.

### From upstream TazUO

Backported: reconnect no longer sticks on a stale packet buffer (#891),
external image loading handles hex filenames and BMP (#797), bounds checks in
the animation loader (#749), the bandage agent no longer double-applies or
leaves a stuck healing buff (#826), and several null guards (#755, #780, #834,
#835).

---
## [4.5.23]
- Fixed an IndexOutOfRangeException crash when the mouse hovered over items/corpses with a graphic id outside the animation data index bounds

## [4.5.22]
- More fixes for EA publish

## [4.5.21]
- Fixed an issue with recent animation loading causing a crash after latest EA publish

## [4.5.20]
- Only send login metric once per session(Swapping chars won't count as additional logins until client is closed/reopened)

### Fixes
- Fixed a crash in legion API when setting display range during logout

## [4.5.19]

### Fixes
- Crash fix when checking buffs in API on client logout
- Ensure metrics isn't sending account names in server name(Likely by connecting to stealth, also added server-side prevention)