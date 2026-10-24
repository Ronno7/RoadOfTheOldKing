"""Pacing simulation (23 Oct): walks a scene's critical path over its real collision and models each beat.

Input: JSON from Tools/Pacing/PacingExport.cs.txt (beats + walkability grid).
Output: per-tier timelines (JSON) and charts (PNG) for DeveloperNotes/PacingDesign.md.

This is a model, not a playtest. Every assumption lives in TIERS / ENEMY / BEAT_TIME below so it can be tuned
against human playtests and opt-in data (#75). Usage:
    python pacing_sim.py <export dir> <out dir> [route set: before|after|built]
    python pacing_sim.py compare <out dir> [a=before] [b=after]
"""
import heapq
import json
import math
import sys
from pathlib import Path

# --- Player and skill assumptions -------------------------------------------------------------------------------
WALK, SPRINT = 4.5, 7.2           # u/s, Player prefab
MAX_HP, FLASKS, FLASK_HEAL = 100, 3, 40
TIERS = {
    # route_factor: extra distance from looking around/detours; sprint_share: share of travel spent sprinting;
    # dmg_per_opening: damage landed per enemy opening (one chop 10, two chops 20, combo/cleave ~40);
    # p_hit: chance an enemy attack lands; drink_below: HP at which the player drinks after a fight.
    "newcomer":  dict(route_factor=1.35, sprint_share=0.0, dmg_per_opening=10, sweep=2, p_hit=0.45, drink_below=60, lesson=1.6, puzzle=1.6),
    "learning":  dict(route_factor=1.15, sprint_share=0.3, dmg_per_opening=20, sweep=3, p_hit=0.25, drink_below=50, lesson=1.0, puzzle=1.0),
    "confident": dict(route_factor=1.00, sprint_share=0.7, dmg_per_opening=40, sweep=3, p_hit=0.10, drink_below=35, lesson=0.6, puzzle=0.5),
}
# --- Enemy timing: seconds per attack cycle (approach/stalk + windup + active + recovery), damage per landed attack.
ENEMY = {
    "wolf":      dict(cycle=3.0, dmg=None),   # dmg from export: mean of snap/lunge
    "scavenger": dict(cycle=2.6, dmg=18),
    "boar":      dict(cycle=4.0, dmg=24),
    "rat":       dict(cycle=1.8, dmg=5, hp=20),
}
# Seconds for non-combat beats at the "learning" tier (scaled by lesson/puzzle factors).
BEAT_TIME = {"pickup": 3, "lesson": 10, "awaken": 8, "light_rest": 10, "trader": 20, "puzzle": 240, "upgrade": 25, "look": 4}

# --- Grid and pathfinding ---------------------------------------------------------------------------------------
class Grid:
    def __init__(self, g):
        self.ox, self.oy = g["origin"]; self.c = g["cell"]; self.w = g["w"]; self.h = g["h"]
        self.blocked = [ch == "1" for ch in g["data"]]

    def cell(self, p):
        return (min(self.w - 1, max(0, int((p[0] - self.ox) / self.c))), min(self.h - 1, max(0, int((p[1] - self.oy) / self.c))))

    def free(self, x, y):
        return 0 <= x < self.w and 0 <= y < self.h and not self.blocked[y * self.w + x]

    def nearest_free(self, x, y, r=12):
        if self.free(x, y): return x, y
        for d in range(1, r):
            for dx in range(-d, d + 1):
                for dy in (-d, d):
                    if self.free(x + dx, y + dy): return x + dx, y + dy
                    if self.free(x + dy, y + dx): return x + dy, y + dx
        return None

    def reachable_from(self, p):
        start = self.nearest_free(*self.cell(p)); seen = {start}; stack = [start]
        while stack:
            x, y = stack.pop()
            for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if (nx, ny) not in seen and self.free(nx, ny):
                    seen.add((nx, ny)); stack.append((nx, ny))
        return seen

    def approach(self, p, allowed, radius=6.0):
        """Nearest reachable cell to a beat that sits on solid geometry (a lever, a building): where a player stands."""
        cx, cy = self.cell(p); r = int(radius / self.c); best = None
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                n = (cx + dx, cy + dy)
                if n in allowed and (best is None or dx * dx + dy * dy < best[0]): best = (dx * dx + dy * dy, n)
        return best[1] if best else None

    def path_length(self, a, b, allowed):
        s = self.approach(a, allowed); t = self.approach(b, allowed)
        if s is None or t is None: return None
        D, D2 = 1.0, math.sqrt(2)
        h = lambda n: (abs(n[0] - t[0]) + abs(n[1] - t[1])) + (D2 - 2) * min(abs(n[0] - t[0]), abs(n[1] - t[1]))
        best = {s: 0.0}; pq = [(h(s), 0.0, s)]
        while pq:
            _, g, n = heapq.heappop(pq)
            if n == t: return g * self.c
            if g > best.get(n, 1e18): continue
            x, y = n
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    if not dx and not dy: continue
                    m = (x + dx, y + dy)
                    if m not in allowed: continue
                    if dx and dy and ((x + dx, y) not in allowed or (x, y + dy) not in allowed): continue
                    ng = g + (D2 if dx and dy else D)
                    if ng < best.get(m, 1e18):
                        best[m] = ng; heapq.heappush(pq, (ng + h(m), ng, m))
        return None

# --- Encounter model ---------------------------------------------------------------------------------------------
def enemy_profile(e):
    name = e["name"].lower()
    if "rat" in name: kind = "rat"
    elif "boar" in name: kind = "boar"
    elif e.get("brain") == "PredatorBrain": kind = "wolf"
    else: kind = "scavenger"
    p = dict(ENEMY[kind]); p["kind"] = kind
    p["hp"] = e.get("hp") or p.get("hp", 50)
    if kind == "wolf": p["dmg"] = (e["snapDamage"] + e["lungeDamage"]) / 2
    p["tokened"] = kind == "wolf"     # wolves share one attack token; pursuit enemies attack independently today
    return p

MID_FIGHT_DRINK = 30   # a player drinks mid-fight below this; the drink spends one enemy cycle without attacking

def fight(group, tier, hp, flasks, t=None):
    """Expected values, not a random roll. Returns (duration, net damage, attacker counts, flasks left, drinks, died)."""
    t = t or TIERS[tier]; alive = [dict(g) for g in group]; dur = 0.0; taken = 0.0; pressure = []; drinks = 0; cur = hp
    while alive:
        attackers = 1 if all(a["tokened"] for a in alive) else sum(1 for a in alive if not a["tokened"]) + (1 if any(a["tokened"] for a in alive) else 0)
        cycle = max(a["cycle"] for a in alive)
        incoming = sum(a["dmg"] for a in sorted(alive, key=lambda a: -a["dmg"])[:attackers]) * t["p_hit"]
        dur += cycle; taken += incoming; cur -= incoming; pressure += [attackers] * int(round(cycle))
        if cur <= 0: return dur, taken, pressure, flasks, drinks, True
        if cur < MID_FIGHT_DRINK and flasks > 0:
            flasks -= 1; drinks += 1; cur = min(MAX_HP, cur + FLASK_HEAL); taken -= FLASK_HEAL; continue
        target = min(alive, key=lambda a: a["hp"])
        if target["kind"] == "rat":   # a sweep catches clustered rats
            for r in sorted((a for a in alive if a["kind"] == "rat"), key=lambda a: a["hp"])[:t["sweep"]]: r["hp"] -= t["dmg_per_opening"]
        else:
            target["hp"] -= t["dmg_per_opening"]
        alive = [a for a in alive if a["hp"] > 0]
    return dur, taken, pressure, flasks, drinks, False

# --- Route walk --------------------------------------------------------------------------------------------------
def simulate(export, route, tier, state):
    grid = Grid(export["grid"]); allowed = grid.reachable_from(route[0]["pos"])
    enemies = {e["name"]: e for e in export["items"] if e["kind"] == "enemy"}
    t = TIERS[tier]; speed = WALK * (1 - t["sprint_share"]) + SPRINT * t["sprint_share"]
    timeline = []; clock = 0.0; pos = route[0]["pos"]; notes = []
    hp, flasks = state["hp"], state["flasks"]; last_rest = 0.0; deaths = 0

    def add(kind, label, dur, intensity, extra=None):
        nonlocal clock
        seg = dict(kind=kind, label=label, start=round(clock, 1), dur=round(dur, 1), intensity=intensity, hp=round(hp), flasks=flasks)
        if extra: seg.update(extra)
        timeline.append(seg); clock += dur

    for beat in route:
        if "tiers" in beat and tier not in beat["tiers"]: continue   # optional beat only some players take
        if beat["pos"] != pos:
            d = grid.path_length(pos, beat["pos"], allowed)
            if d is None:
                d = math.dist(pos, beat["pos"]) * 1.3; notes.append(f"no grid path to {beat['label']} (water state?); used 1.3x straight line")
            d += beat.get("extra", 0)   # proposed road length not yet in the scene
            dur = d * t["route_factor"] / speed
            add("travel", "to " + beat["label"], dur, beat.get("travel_intensity", 1), dict(distance=round(d, 1)))
            pos = beat["pos"]
        k = beat["kind"]
        if k == "fight":
            group = [enemy_profile(enemies[n]) for n in beat.get("enemies", []) if n in enemies]
            group += [dict(ENEMY["rat"], kind="rat", tokened=False) for _ in range(beat.get("rats", 0))]
            group += [dict(dict(cycle=3.0, dmg=20, hp=60), **e, tokened=False) for e in beat.get("synthetic", [])]   # proposed enemies
            if beat.get("coordinated"):   # approved rule: one committed attack sequence at a time
                for g in group: g["tokened"] = True
            scale = beat.get("opening_scale", 1)   # long, promised recoveries let a player land more per opening
            if scale != 1: t = dict(t, dmg_per_opening=t["dmg_per_opening"] * scale)
            dur, taken, pressure, flasks, drinks, died = fight(group, tier, hp, flasks, t)
            peak = max(pressure) if pressure else 1
            hp = max(0, hp - taken)
            add("fight", beat["label"], dur, min(10, 4 + 1.5 * (peak - 1) + (3 if beat.get("boss") else 0) + (2 if taken + drinks * FLASK_HEAL > 30 else 0)),
                dict(damage=round(taken + drinks * FLASK_HEAL), peak_attackers=peak, died=died, drinks=drinks))
            if died:
                deaths += 1; retry = clock - last_rest
                add("death", "death at " + beat["label"], 6 + retry, 0, dict(retry_cost=round(retry)))
                hp, flasks = MAX_HP, FLASKS
                d2, t2, _, flasks, dr2, died2 = fight(group, tier, hp, flasks, t); hp = max(1, hp - t2)
                add("fight", beat["label"] + " (retry)", d2, 7, dict(damage=round(t2 + dr2 * FLASK_HEAL), drinks=dr2, died=died2))
            while hp < t["drink_below"] and flasks > 0 and hp < MAX_HP:
                hp = min(MAX_HP, hp + FLASK_HEAL); flasks -= 1
                add("recover", "flask", 1.5, 0.5)
        elif k == "rest":
            t = TIERS[tier]
            hp, flasks = MAX_HP, FLASKS; last_rest = clock
            add("rest", beat["label"], BEAT_TIME["light_rest"], 0)
        else:
            base = BEAT_TIME.get(k, 5) * (t["puzzle"] if k == "puzzle" else t["lesson"] if k in ("lesson", "awaken") else 1)
            add(k, beat["label"], beat.get("time", base), beat.get("intensity", {"lesson": 2, "puzzle": 3, "awaken": 4, "look": 1.5}.get(k, 1)))
    state.update(hp=hp, flasks=flasks)
    return timeline, notes, deaths

def metrics(tl):
    total = sum(s["dur"] for s in tl) or 1
    by = lambda k: sum(s["dur"] for s in tl if s["kind"] == k)
    first = next((s["start"] for s in tl if s["kind"] == "fight"), None)
    gaps, last = [], 0.0
    for s in tl:
        if s["kind"] == "fight":
            gaps.append(s["start"] - last); last = s["start"] + s["dur"]
    return dict(total_s=round(total), travel_pct=round(100 * by("travel") / total), fight_pct=round(100 * by("fight") / total),
                learn_pct=round(100 * (by("lesson") + by("puzzle") + by("awaken")) / total), rest_pct=round(100 * (by("rest") + by("recover") + by("trader")) / total),
                first_threat_s=first, longest_quiet_s=round(max(gaps + [total - last])), fights=sum(1 for s in tl if s["kind"] == "fight"),
                damage=round(sum(s.get("damage", 0) for s in tl)), deaths=sum(1 for s in tl if s["kind"] == "death"),
                flasks_used=sum(1 for s in tl if s["kind"] == "recover") + sum(s.get("drinks", 0) for s in tl))

def booth_stress(tl):
    """Booth-style intensity: rises with damage taken and kills, decays when not fighting (per second samples)."""
    out, stress = [], 0.0
    for s in tl:
        n = max(1, int(round(s["dur"])))
        for i in range(n):
            if s["kind"] == "fight":
                stress += s.get("damage", 0) / n * 1.2 + 1.5 * s.get("peak_attackers", 1)
            else:
                stress *= 0.92
            stress = min(100, stress); out.append(round(stress, 1))
    return out

def chart(path, title, runs):
    from PIL import Image, ImageDraw
    W, H, L, B = 1500, 230 * len(runs) + 40, 70, 30
    img = Image.new("RGB", (W, H), (250, 249, 245)); d = ImageDraw.Draw(img)
    colors = {"travel": (184, 196, 170), "fight": (198, 70, 52), "lesson": (90, 130, 200), "puzzle": (70, 90, 170), "awaken": (150, 90, 190),
              "rest": (232, 160, 50), "recover": (240, 200, 120), "trader": (232, 160, 50), "death": (30, 30, 30), "pickup": (240, 210, 60),
              "upgrade": (240, 210, 60), "look": (160, 200, 200), "exit": (120, 120, 120)}
    d.text((L, 8), title, fill=(30, 30, 30))
    maxt = max(sum(s["dur"] for s in tl) for _, tl, _ in runs)
    for r, (name, tl, stress) in enumerate(runs):
        top = 40 + r * 230; sx = (W - L - 20) / maxt
        d.text((8, top + 90), name, fill=(30, 30, 30))
        for s in tl:   # designed intensity bars
            x0 = L + s["start"] * sx; x1 = L + (s["start"] + s["dur"]) * sx; hgt = s["intensity"] * 16
            d.rectangle([x0, top + 170 - hgt, max(x0 + 1, x1), top + 170], fill=colors.get(s["kind"], (150, 150, 150)))
        pts = [(L + i * sx, top + 170 - v * 1.6) for i, v in enumerate(stress)]   # Booth stress line
        if len(pts) > 1: d.line(pts, fill=(20, 20, 20), width=2)
        for s in tl:   # health
            x = L + s["start"] * sx; d.ellipse([x - 1, top + 222 - s["hp"] * .2 - 1, x + 1, top + 222 - s["hp"] * .2 + 1], fill=(40, 140, 60))
        for m in range(0, int(maxt) + 1, 60):
            x = L + m * sx; d.line([x, top + 172, x, top + 178], fill=(80, 80, 80)); d.text((x - 8, top + 180), f"{m // 60}m", fill=(80, 80, 80))
    img.save(path)

def compare(out, a="before", b="after"):
    """One chart per tier: variant a above variant b, same time scale."""
    ra = json.loads((out / f"pacing_{a}.json").read_text(encoding="utf8")); rb = json.loads((out / f"pacing_{b}.json").read_text(encoding="utf8"))
    for tier in TIERS:
        chart(out / f"compare_{tier}.png", f"{tier}: {a} (top) vs {b} (bottom). Bars = beat intensity by type, line = Booth-style stress, dots = health",
              [(a, ra[tier]["timeline"], booth_stress(ra[tier]["timeline"])), (b, rb[tier]["timeline"], booth_stress(rb[tier]["timeline"]))])

if __name__ == "__main__":
    if sys.argv[1] == "compare":
        compare(Path(sys.argv[2]), *sys.argv[3:5]); sys.exit()
    src, out = Path(sys.argv[1]), Path(sys.argv[2]); variant = sys.argv[3] if len(sys.argv) > 3 else "before"
    routes = json.loads((Path(__file__).with_name("routes.json")).read_text(encoding="utf8"))[variant]
    exports = {sc: json.loads((src / f"pacing_{sc}.json").read_text(encoding="utf8")) for sc in {r["scene"] for r in routes}}
    results = {}
    for tier in TIERS:
        state = dict(hp=MAX_HP, flasks=FLASKS); tl_all, notes_all = [], []
        for leg in routes:
            tl, notes, _ = simulate(exports[leg["scene"]], leg["beats"], tier, state)
            offset = sum(s["dur"] for s in tl_all)
            for s in tl: s["start"] = round(s["start"] + offset, 1); s["leg"] = leg["name"]
            tl_all += tl; notes_all += notes
        results[tier] = dict(timeline=tl_all, metrics=metrics(tl_all), notes=sorted(set(notes_all)),
                             legs={leg["name"]: metrics([s for s in tl_all if s["leg"] == leg["name"]]) for leg in routes})
    out.mkdir(parents=True, exist_ok=True)
    (out / f"pacing_{variant}.json").write_text(json.dumps(results, indent=1), encoding="utf8")
    chart(out / f"pacing_{variant}.png", f"Pacing model, {variant}: bars = designed intensity by beat type, line = Booth-style stress, dots = health",
          [(tier, r["timeline"], booth_stress(r["timeline"])) for tier, r in results.items()])
    for tier, r in results.items():
        print(tier, r["metrics"]); [print("  ", leg, m) for leg, m in r["legs"].items()]; [print("   note:", n) for n in r["notes"]]
