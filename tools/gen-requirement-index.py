import re, sys

path = sys.argv[1]
text = open(path, encoding="utf-8").read()
start, end = "<!-- REQUIREMENT-INDEX:START -->", "<!-- REQUIREMENT-INDEX:END -->"
body = text.split(start)[0]

section = ""
rows, seen = [], set()
lines = body.splitlines()
for i, line in enumerate(lines):
    h = re.match(r"^#{1,2} (\d+(?:\.\d+)?)\.? (.+)$", line)
    if h:
        section = h.group(1)
        continue
    m = re.match(r"^- \*\*([A-Z]{2,4}-\d{2})\*\* (.*)$", line)
    if not m:
        continue
    rid, desc = m.group(1), m.group(2)
    j = i + 1
    while j < len(lines) and lines[j].startswith("  ") and not desc.rstrip().endswith("."):
        desc += " " + lines[j].strip()
        j += 1
    desc = re.split(r"(?<=\.)\s", desc, maxsplit=1)[0].rstrip(".")
    desc = desc.replace("**", "").replace("|", "\\|")
    if len(desc) > 90:
        desc = desc[:87].rsplit(" ", 1)[0] + "..."
    if rid in seen:
        print("DUPLICATE", rid, file=sys.stderr)
    seen.add(rid)
    rows.append((rid, desc, section))

out = ["", f"{len(rows)} requirements.", "", "| ID | Summary | Section |", "|---|---|---|"]
out += [f"| {r} | {d} | {s} |" for r, d, s in rows]
prefixes = {}
for r, _, _ in rows:
    prefixes.setdefault(r.split("-")[0], []).append(r)
out += ["", "**By prefix:** " + ", ".join(f"`{p}` ({len(v)})" for p, v in prefixes.items()), ""]

head, rest = text.split(start)
_, tail = rest.split(end)
open(path, "w", encoding="utf-8", newline="\n").write(head + start + "\n".join(out) + end + tail)
print(len(rows), "rows")
