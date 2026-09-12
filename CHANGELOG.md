# Changelog

Notable changes to TazUO Holiday Edition.

**This file is the release notes.** The release workflow publishes the section
matching the version being released and nothing else, so an entry here is what
people see on the download page. Write it for someone using the client, not for
someone reading the repository: what behaves differently, what stopped
crashing. Work that cancels out - a change and its revert, a fix for a fix -
belongs in the history, not here. No section for a version means that release
publishes with no notes, which is the better of the two failures.

Entries below 4.5.23xx are inherited from upstream TazUO.

---
## [4.5.2301]

First Holiday Edition release. The version continues TazUO's numbering from
4.5.23 rather than restarting, so a launcher can compare the two.

### Fixes
- Fixed a crash when the walking step counter ran past the end of its array.
- Fixed a crash when closing a UI element that was still walking its own
  children.

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