"""Arrival road layout (23 Oct): derives Lowlands border terrain tiles from a cell-class grid.

Classes: U blocked upland (+1, forest), W walkable upland (+1), L lowland (0, walkable), S stair (face row, up north),
F blocked flat forest (0; along diagonal river banks, where ridge edges would staircase), R river or river bank
(never touched), K keep (outside the edit region, read from the scene).

Rules (TilemapStandard.md, measured on the 22 Oct scene):
- Ridge tops on U/W/S. Front face in the L cell directly south of U/W (CliffEarthFace_Continue when U/W lies east,
  else Paint_CliffEarthFace) with a full Collision_Block in Height boundaries.
- Narrow rims on U/W cells next to lower ground: east open CliffCollision_2, west 4, north (rear) 1, NE corner 3,
  NW corner 5. South-open upland cells get none (the face below blocks).
- Border collision on U and F; F counts as lower ground and takes a face when U/W lies north. Stairs: Ridge top + StoneStairs_Single_Left/Center/Right, no collision, guide Structure,
  transitions UpNorth.

Modes:
    python arrival_layout.py verify <terrain_before.json>      rules vs. current scene (must be ~0 mismatches)
    python arrival_layout.py build  <terrain_before.json> <out_ops.json> <preview.png>
"""
import json
import sys

UP = {"U", "W", "S"}  # ridge-height cells (stairs carry a ridge top)


def load(path):
    d = json.load(open(path, encoding="utf8"))
    cells = {tuple(map(int, k.split(","))): v for k, v in d["cells"].items()}
    return d["region"], cells


def classify_current(cells):
    cls = {}
    for c, v in cells.items():
        if v.get("water") or v.get("heightColl") == "SleepingRiverBlock":
            cls[c] = "R"
        elif v.get("ridgeTop") and v.get("connector"):
            cls[c] = "S"
        elif v.get("ridgeTop"):
            cls[c] = "U" if v.get("borderColl") else "W"
        elif v.get("borderColl"):
            cls[c] = "F"
        else:
            cls[c] = "L"
    return cls


def derive(cls):
    """Expected tiles per cell for the maps the generator owns."""
    out = {}
    g = lambda x, y: cls.get((x, y), "K")
    up = lambda x, y: g(x, y) in UP or g(x, y) == "KU"
    low = lambda x, y: g(x, y) in ("L", "F", "KL", "R")   # water is lower ground: uplands get rims along it too
    for (x, y), k in cls.items():
        if k in ("R", "K", "KU", "KL"):
            continue
        t = {}
        if k in UP:
            t["ridgeTop"] = "Paint_CliffEarthTop"
        if k in ("U", "F"):
            t["borderColl"] = "Collision_Block"
        if k in ("U", "W"):
            n, e, w = low(x, y + 1), low(x + 1, y), low(x - 1, y)
            if n and e: t["heightColl"] = "CliffCollision_3"
            elif n and w: t["heightColl"] = "CliffCollision_5"
            elif n: t["heightColl"] = "CliffCollision_1"
            elif e: t["heightColl"] = "CliffCollision_2"
            elif w: t["heightColl"] = "CliffCollision_4"
        if k in ("L", "F") and g(x, y + 1) in ("U", "W", "KU"):
            t["ridgeFace"] = "CliffEarthFace_Continue" if g(x + 1, y) in ("U", "W", "KU") else "Paint_CliffEarthFace"
            t["heightColl"] = "Collision_Block"
        if k == "S":
            left, right = g(x - 1, y) == "S", g(x + 1, y) == "S"
            t["connector"] = "StoneStairs_Single_Center" if left and right else "StoneStairs_Single_Left" if right else "StoneStairs_Single_Right"
        out[(x, y)] = t
    return out


OWNED = ("ridgeTop", "ridgeFace", "heightColl", "borderColl", "connector")


def verify(path):
    _, cells = load(path)
    cls = classify_current(cells)
    exp = derive(cls)
    from collections import Counter
    mism = Counter(); examples = {}
    for c, t in exp.items():
        for m in OWNED:
            a, b = cells[c].get(m), t.get(m)
            if a != b:
                key = f"{m}: scene={a} rule={b}"
                mism[key] += 1; examples.setdefault(key, c)
    total = sum(len([m for m in OWNED if cells[c].get(m) or t.get(m)]) for c, t in exp.items())
    print(f"cells checked {len(exp)}, owned tiles {total}, mismatches {sum(mism.values())}")
    for k, n in mism.most_common(25):
        print(f"  {n:5d}  {k}  e.g. {examples[k]}")


if __name__ == "__main__":
    if sys.argv[1] == "verify":
        verify(sys.argv[2])


# --- Build: the approved arrival road (PacingDesign.md) ---------------------------------------------------------
def rect(x0, x1, y0, y1):
    return {(x, y) for x in range(x0, x1 + 1) for y in range(y0, y1 + 1)}


def in_edit(c):
    """Edit region: border road west of the farm lip, plus the east bank up to the farm plain."""
    x, y = c
    if x < -28 or x > 46 or y < -66 or y > -1:
        return False
    if y >= -4 and x <= 26:   # the farm's southern lip stays as it is
        return False
    return True


def layout(cls):
    """Returns (new classes, lane cells, beats). Shapes are approved-proposal geometry, world cell coordinates."""
    new = dict(cls)
    for c in list(new):
        if in_edit(c) and new[c] != "R":
            new[c] = "U"
    shapes = []
    shapes.append(("L", rect(3, 8, -66, -48)))          # 1 birth canal from the Tutorial road
    shapes.append(("L", rect(-12, 8, -52, -48)))        #   turns west along the cliff foot
    shapes.append(("W", rect(-13, -9, -46, -31)))       # 2 stair up onto the western upland path
    shapes.append(("W", rect(-13, 12, -30, -26)))       #   overlook ledge above the hollow
    shapes.append(("L", rect(-6, 12, -44, -31)))        # 3 the hollow (waystation, rat swarm)
    shapes.append(("L", rect(13, 22, -40, -36)))        #   east exit
    shapes.append(("L", rect(18, 22, -36, -25)))        #   lane north to the cart
    shapes.append(("L", rect(15, 33, -24, -15)))        # 4 cart pocket (lone scavenger), west bank
    shapes.append(("L", rect(36, 46, -24, -10)))        # 5 east landing (wolf + rats): riverside meadow, ~10 x 9 open
    for k, cells in shapes:
        for c in cells:
            if in_edit(c) and new.get(c) != "R":
                new[c] = k
    # East-bank lane north to the farm plain: a 5-wide sunken lane hugging the river's east edge.
    for y in range(-16, 0):
        river = [x for x in range(15, 47) if new.get((x, y)) == "R"]
        if not river: continue
        e = max(river) + 1
        for x in range(e, e + 5):
            if in_edit((x, y)) and new.get((x, y)) != "R": new[(x, y)] = "L"
    # Stairs (in face rows, rising north): up from the canal passage, down from the ledge into the hollow.
    for c in rect(-12, -10, -47, -47) | rect(8, 10, -31, -31):
        new[c] = "S"
    # Bridge over the river at the cart pocket: deck rows y -22..-21.
    deck = [x for x in range(28, 46) for y in (-22, -21) if new.get((x, y)) == "R"]
    bx0 = min(deck) - 2
    bridge = dict(origin=(bx0, -23), size=(10, 4))
    for x in range(bx0, bx0 + 10):          # landings either side of the water must be walkable lowland
        for y in (-22, -21):
            if new.get((x, y)) != "R": new[(x, y)] = "L"
    # North-east block (river banks, cart pocket surrounds, east bank): flat forest instead of ridge, so the
    # diagonal river and lane edges do not step in one-cell cliff faces.
    for c in list(new):
        if in_edit(c) and new[c] == "U" and c[0] >= 13 and c[1] >= -24:
            new[c] = "F"
    lane = set()
    def line(a, b, w=1):
        (ax, ay), (bx, by) = a, b; n = max(abs(bx - ax), abs(by - ay)) or 1
        for i in range(n + 1):
            px, py = round(ax + (bx - ax) * i / n), round(ay + (by - ay) * i / n)
            for dx in range(-w, w + 1):
                for dy in range(-w, w + 1):
                    if abs(dx) + abs(dy) <= w: lane.add((px + dx, py + dy))
    route = [(6, -66), (6, -50), (-11, -50), (-11, -38), (-11, -28), (9, -28), (9, -34), (17, -38), (20, -30), (21, -20), (bx0, -21)]
    for a, b in zip(route, route[1:]): line(a, b)
    ys = sorted({y for y in range(-21, 0)})
    prev = (bx0 + 10, -21)
    for y in ys:
        river = [x for x in range(15, 47) if new.get((x, y)) == "R"]
        if not river: continue
        cur = (max(river) + 3, y); line(prev, cur); prev = cur
    lane = {c for c in lane if new.get(c) in ("L", "W", "S")}
    beats = {"vista ledge": (0, -28), "hollow / rats": (3, -38), "cart / scavenger": (24, -19), "bridge": (bx0 + 5, -21.5),
             "east landing / wolf + rats": (43, -19), "farm plain": (29, 0)}
    return new, lane, beats, bridge


def preview(new, lane, beats, bridge, path, region):
    from PIL import Image, ImageDraw
    x0, x1, y0, y1 = region; S = 8
    img = Image.new("RGB", ((x1 - x0 + 1) * S, (y1 - y0 + 1) * S), (30, 30, 30)); d = ImageDraw.Draw(img)
    col = {"U": (52, 82, 48), "F": (70, 100, 60), "W": (150, 190, 110), "L": (186, 206, 160), "S": (220, 160, 60), "R": (70, 120, 180)}
    exp = derive(new)
    for (x, y), k in new.items():
        px, py = (x - x0) * S, (y1 - y) * S
        c = col.get(k, (60, 60, 60))
        if exp.get((x, y), {}).get("ridgeFace"): c = (120, 90, 60)
        if (x, y) in lane: c = (205, 170, 120)
        if not in_edit((x, y)): c = tuple(int(v * .6) for v in c)
        d.rectangle([px, py, px + S - 1, py + S - 1], fill=c)
    bx, by = bridge["origin"]
    d.rectangle([(bx - x0) * S, (y1 - by - 3) * S, (bx + 10 - x0) * S, (y1 - by + 1) * S], outline=(255, 255, 255), width=2)
    for name, (x, y) in beats.items():
        px, py = (x - x0) * S, (y1 - y) * S
        d.ellipse([px - 6, py - 6, px + 6, py + 6], fill=(220, 40, 40)); d.text((px + 8, py - 6), name, fill=(255, 255, 255))
    img.save(path)


def east_of_river(new, c):
    x, y = c
    river = [rx for rx in range(15, 48) if new.get((rx, y)) == "R"]
    return bool(river) and x > max(river)


def build(path, out_ops, out_png):
    region, cells = load(path)
    cls = classify_current(cells)
    new, lane, beats, bridge = layout(cls)
    preview(new, lane, beats, bridge, out_png, region)
    exp_new = derive(new)
    ops = []
    touched = {c for c in new if in_edit(c)}
    ring = {(x + dx, y + dy) for (x, y) in touched for dx in (-1, 0, 1) for dy in (-1, 0, 1)}
    for c in sorted(touched | ring):
        if c not in cells or new.get(c) == "R": continue
        t = exp_new.get(c, {})
        for m in OWNED:
            cur, want = cells[c].get(m), t.get(m)
            if c in ring - touched and m == "borderColl": continue   # outside the edit region: only rims/faces may follow
            if cur != want: ops.append([m, c[0], c[1], want])
    for c in touched:
        if new.get(c) == "R" or c not in cells: continue
        h = "Height_1" if new[c] in ("U", "W", "S") else "Height_0"
        if cells[c].get("plan") != h: ops.append(["plan", c[0], c[1], h])
        face = bool(exp_new.get(c, {}).get("ridgeFace"))
        e = "Structure" if face or new[c] == "S" else "Upper" if new[c] in ("U", "W") else "Normal"
        if cells[c].get("elev") != e: ops.append(["elev", c[0], c[1], e])
        tr = "UpNorth" if new[c] == "S" else None
        if cells[c].get("trans") != tr: ops.append(["trans", c[0], c[1], tr])
        if cells[c].get("marks"): ops.append(["marks", c[0], c[1], None])
        # East of the river the lane lives on the farm's DirtLane map, so it joins the farm road without a RuleTile
        # seam; the two maps meet under the stone bridge deck.
        want_lane = "Paint_DirtLane" if c in lane else None
        own, other = ("dirtLane", "lane") if east_of_river(new, c) else ("lane", "dirtLane")
        if cells[c].get(own) != want_lane: ops.append([own, c[0], c[1], want_lane])
        if want_lane and cells[c].get(other): ops.append([other, c[0], c[1], None])
    # Bridge deck: river collision off on the two deck rows (the bridge prefab supplies its rails).
    bx, by = bridge["origin"]
    for x in range(bx, bx + 10):
        for y in (by + 1, by + 2):
            v = cells.get((x, y), {})
            for m in ("heightColl", "borderColl"):
                if v.get(m): ops.append([m, x, y, None])
    json.dump({"ops": ops, "bridge": bridge, "beats": beats}, open(out_ops, "w"), indent=0)
    from collections import Counter
    print("ops", len(ops), dict(Counter(o[0] for o in ops)))
    print("bridge", bridge)
    walk = {c for c, k in new.items() if k in ("L", "W", "S") and not exp_new.get(c, {}).get("ridgeFace")}
    print("walkable cells in edit region", len([c for c in walk if in_edit(c)]))


if __name__ == "__main__" and sys.argv[1] == "build":
    build(sys.argv[2], sys.argv[3], sys.argv[4])
