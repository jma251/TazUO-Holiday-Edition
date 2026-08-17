# Range probe
#
# Measures the distance at which the server first hands this client a mobile.
# Nothing here asks the client what it thinks its view range is - it watches for
# serials it has never seen before and records how far away they were the
# instant they appeared. The largest number it ever reports is the range the
# server actually sends within.
#
# Run it, then ride out somewhere empty and back through a populated area a few
# times. Watch the "furthest" figure settle.

ALL_NOTORIETY = [
    API.Notoriety.Unknown,
    API.Notoriety.Innocent,
    API.Notoriety.Ally,
    API.Notoriety.Gray,
    API.Notoriety.Criminal,
    API.Notoriety.Enemy,
    API.Notoriety.Murderer,
    API.Notoriety.Invulnerable,
]

# Well past anything the server could plausibly be sending, so the search itself
# never becomes the thing that limits the answer.
SEARCH_RADIUS = 100

seen = set()
furthest = 0
counts = {}

API.SysMsg("Range probe running. Ride out and back a few times.", 66)

while True:
    for m in API.NearestMobiles(ALL_NOTORIETY, SEARCH_RADIUS):
        if m.Serial in seen:
            continue

        seen.add(m.Serial)
        d = m.Distance

        counts[d] = counts.get(d, 0) + 1

        if d > furthest:
            furthest = d
            name = m.Name if m.Name else "?"
            API.SysMsg("furthest first sighting: %d tiles  (%s)" % (d, name), 33)

    API.Pause(0.1)
