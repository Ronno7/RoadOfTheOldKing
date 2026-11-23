// Road of the Old King · Showcase
// Vanilla, dependency-free. Each feature is a small self-contained setup function;
// animation loops run only while their element is on screen.

(() => {
  "use strict";

  const root = document.documentElement;
  const motionPreference = matchMedia("(prefers-reduced-motion: reduce)");
  let reduceMotion = motionPreference.matches;
  const $ = (sel, el = document) => el.querySelector(sel);
  const $$ = (sel, el = document) => [...el.querySelectorAll(sel)];
  const clamp = (v, lo, hi) => Math.min(hi, Math.max(lo, v));
  const lerp = (a, b, t) => a + (b - a) * t;
  const ART = "assets/art/";

  const REGIONS = {
    edge: "Ardan's Edge",
    lowlands: "Green Lowlands",
    barrow: "Barrow Woods",
    highlands: "Storm Highlands",
  };
  const NUMERALS = ["", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX"];

  // Runs `tick(dt, t)` every frame while `el` is visible.
  function loopWhileVisible(el, tick) {
    let raf = 0, last = 0, visible = false;
    const frame = (now) => {
      const dt = Math.min(0.05, (now - (last || now)) / 1000);
      last = now;
      tick(dt, now / 1000);
      if (visible && !motionPreference.matches && !document.hidden) raf = requestAnimationFrame(frame);
    };
    const refresh = () => {
      cancelAnimationFrame(raf);
      if (visible && !motionPreference.matches && !document.hidden) { last = 0; raf = requestAnimationFrame(frame); }
    };
    new IntersectionObserver(([entry]) => {
      visible = entry.isIntersecting;
      refresh();
    }).observe(el);
    motionPreference.addEventListener("change", refresh);
    document.addEventListener("visibilitychange", refresh);
  }

  // ── Pixel-perfect sizing ─────────────────────────────────────────────
  // Art is only ever shown at whole multiples of its native size (in device pixels).
  function fitPixels() {
    const dpr = window.devicePixelRatio || 1;
    for (const el of $$("[data-fit]")) {
      const native = Number(el.getAttribute("width"));
      const container = el.closest("picture")?.parentElement || el.parentElement;
      const avail = container.clientWidth * Number(el.dataset.fit || 1);
      const steps = Math.max(1, Math.floor((avail * dpr) / native));
      el.style.width = `${(native * steps) / dpr}px`;
    }
  }

  // ── Chapters, rail, map and the region shift ─────────────────────────
  const chapters = $$(".chapter").map((el, i) => ({
    el, id: el.id, title: el.dataset.title, short: el.dataset.short || el.dataset.title, region: el.dataset.region, numeral: NUMERALS[i] || "",
  }));

  const rail = $(".rail");
  const railPath = $("#rail-path");
  const railHero = $(".rail__hero");
  const railX = (t) => 20 + 12 * Math.sin(t * Math.PI * 4);  // the road's meander, in viewBox units

  railPath.setAttribute("d", Array.from({ length: 101 }, (_, i) =>
    `${i ? "L" : "M"}${railX(i / 100).toFixed(2)} ${i * 10}`).join(" "));

  const railStops = $(".rail__stops");
  const mapStops = $(".map__stops");
  for (const ch of chapters) {
    railStops.insertAdjacentHTML("beforeend",
      `<li><a href="#${ch.id}" aria-label="${ch.numeral ? `Chapter ${ch.numeral}: ` : ""}${ch.title}"><span>${ch.title}</span></a></li>`);
    mapStops.insertAdjacentHTML("beforeend",
      `<li><a href="#${ch.id}" aria-label="${ch.title}">${ch.numeral ? `<em>${ch.numeral}</em>` : ""}${ch.short}</a></li>`);
  }
  const railLinks = $$("a", railStops);
  const mapLinks = $$("a", mapStops);

  // Each stop sits where the hero will be when its chapter reaches the top of the view.
  function layoutRail() {
    const max = Math.max(1, document.documentElement.scrollHeight - innerHeight);
    const h = rail.clientHeight;
    chapters.forEach((ch, i) => {
      const t = clamp(ch.el.offsetTop / max, 0, 1);
      const li = railLinks[i].parentElement;
      li.style.left = `${(railX(t) / 40) * 100}%`;
      li.style.top = `${t * h}px`;
    });
  }

  // The map's road winds through each stop, alternating crest and dip, so every
  // label sits on the open side of its bend.
  const MAP_PTS = [[16, 118], [54, 58], [94, 116], [134, 56], [174, 116], [212, 56], [245, 116], [285, 56], [325, 116], [368, 48]];
  (() => {
    const pts = [[0, 124], ...MAP_PTS, [400, 40]];
    let d = `M${pts[0][0]} ${pts[0][1]}`;
    for (let i = 0; i < pts.length - 1; i++) {  // Catmull-Rom through every point, as cubic Béziers
      const [p0, p1, p2, p3] = [pts[i - 1] || pts[i], pts[i], pts[i + 1], pts[i + 2] || pts[i + 1]];
      d += ` C${p1[0] + (p2[0] - p0[0]) / 6} ${p1[1] + (p2[1] - p0[1]) / 6}, ${p2[0] - (p3[0] - p1[0]) / 6} ${p2[1] - (p3[1] - p1[1]) / 6}, ${p2[0]} ${p2[1]}`;
    }
    $(".map__road").setAttribute("d", d);
  })();
  function layoutMap() {
    mapLinks.forEach((a, i) => {
      const [x, y] = MAP_PTS[i] ?? [(i / (mapLinks.length - 1)) * 400, 80];
      const li = a.parentElement;
      li.style.left = `${(x / 400) * 100}%`;
      li.style.top = `${(y / 160) * 100}%`;
      li.classList.toggle("is-above", y < 80);
    });
  }

  // Hero walks the rail: frames advance with distance scrolled, facing the scroll direction.
  const walkFrames = { south: [], north: [] };
  for (const dir of ["south", "north"]) {
    for (let i = 0; i < 8; i++) {
      const src = `${ART}sprites/hero/walk-${dir}-${i}.png`;
      walkFrames[dir].push(src);
      new Image().src = src;
    }
  }
  let lastY = scrollY, stride = 0, idleTimer = 0;
  function onScroll() {
    const max = Math.max(1, document.documentElement.scrollHeight - innerHeight);
    const t = clamp(scrollY / max, 0, 1);
    railHero.style.left = `${(railX(t) / 40) * 100}%`;
    railHero.style.top = `${t * rail.clientHeight}px`;
    if (!reduceMotion) {
      const dy = scrollY - lastY;
      stride += Math.abs(dy);
      const dir = dy < 0 ? "north" : "south";
      railHero.src = walkFrames[dir][Math.floor(stride / 28) % 8];
      clearTimeout(idleTimer);
      idleTimer = setTimeout(() => { railHero.src = walkFrames[dir][0]; }, 160);
    }
    lastY = scrollY;
  }

  // Region and chapter tracking.
  const chapterLabel = $(".topbar__chapter");
  const place = $(".place");
  let currentRegion = null;

  function showPlace(region) {
    $(".place__name").textContent = REGIONS[region];
    place.classList.remove("is-shown");
    void place.offsetWidth;  // restart the animation
    place.classList.add("is-shown");
  }

  function setChapter(ch) {
    railLinks.forEach((a, i) => a.setAttribute("aria-current", String(chapters[i] === ch)));
    mapLinks.forEach((a, i) => a.setAttribute("aria-current", String(chapters[i] === ch)));
    $("[data-chapter-numeral]").textContent = ch.numeral || "·";
    $("[data-chapter-name]").textContent = ch.title;
    chapterLabel.classList.toggle("is-shown", ch.id !== "prologue");
    if (ch.region !== currentRegion) {
      const first = currentRegion === null;
      currentRegion = ch.region;
      root.dataset.region = ch.region;
      setWeather(ch.region);
      if (!first) showPlace(ch.region);
    }
  }

  const chapterObserver = new IntersectionObserver((entries) => {
    for (const e of entries) {
      if (e.isIntersecting) setChapter(chapters.find((c) => c.el === e.target));
    }
  }, { rootMargin: "-45% 0px -50% 0px" });
  chapters.forEach((c) => chapterObserver.observe(c.el));

  // Content rises gently into view.
  const revealObserver = new IntersectionObserver((entries) => {
    for (const e of entries) {
      if (e.isIntersecting) { e.target.classList.add("is-in"); revealObserver.unobserve(e.target); }
    }
  }, { rootMargin: "0px 0px -10% 0px" });
  for (const ch of chapters) {
    [...ch.el.children].filter((c) => !c.matches(".lantern")).forEach((c, i) => {
      c.classList.add("reveal");
      c.style.transitionDelay = `${Math.min(i, 4) * 70}ms`;
      revealObserver.observe(c);
    });
  }

  // Map dialog and keyboard travel.
  const map = $("#map");
  $("[data-open-map]").addEventListener("click", () => { map.showModal(); layoutMap(); });
  map.addEventListener("click", (e) => {
    if (e.target === map || e.target.closest(".map__stops a")) map.close();
  });

  function jump(delta) {
    const at = chapters.findIndex((c) => c.el.getBoundingClientRect().bottom > innerHeight * 0.5);
    const next = chapters[clamp(at + delta, 0, chapters.length - 1)];
    next.el.scrollIntoView({ behavior: reduceMotion ? "auto" : "smooth" });
  }
  addEventListener("keydown", (e) => {
    if (e.target.closest("input, textarea, [role=tab]") || e.metaKey || e.ctrlKey || e.altKey) return;
    if (e.key === "m" || e.key === "M") { e.preventDefault(); if (map.open) map.close(); else { map.showModal(); layoutMap(); } }
    else if (map.open) return;
    else if (e.key === "j" || e.key === "]") jump(1);
    else if (e.key === "k" || e.key === "[") jump(-1);
  });

  // ── Weather: region particles on a low-resolution canvas ─────────────
  const weather = $("#weather");
  const wctx = weather.getContext("2d");
  const flash = $(".world__flash");
  let motes = [], weatherKind = "edge", strikeTimer = 0;
  const W_SCALE = 3;

  function sizeWeather() {
    weather.width = Math.ceil(innerWidth / W_SCALE);
    weather.height = Math.ceil(innerHeight / W_SCALE);
  }

  function spawnMote(kind, anywhere) {
    const w = weather.width, h = weather.height;
    const m = { x: Math.random() * w, y: anywhere ? Math.random() * h : h + 2, life: Math.random() };
    if (kind === "edge") Object.assign(m, { vx: 1 + Math.random() * 2, vy: -2 - Math.random() * 3, c: "#e7b85c", a: .55 });
    if (kind === "lowlands") Object.assign(m, { x: anywhere ? m.x : -2, y: Math.random() * h, vx: 10 + Math.random() * 12, vy: -1 + Math.random() * 2, c: "#f1deb0", a: .8 });
    if (kind === "barrow") Object.assign(m, { vx: -1 + Math.random() * 2, vy: -5 - Math.random() * 6, c: Math.random() < .5 ? "#e7b85c" : "#d99a67", a: .9 });
    if (kind === "highlands") Object.assign(m, { y: anywhere ? m.y : -4, vx: -28, vy: 110 + Math.random() * 40, c: "#9bc0ae", a: .35, len: 3 + Math.random() * 3 });
    return m;
  }

  function setWeather(kind) {
    weatherKind = kind;
    const count = { edge: 26, lowlands: 34, barrow: 40, highlands: 90 }[kind];
    motes = Array.from({ length: count }, () => spawnMote(kind, true));
    clearTimeout(strikeTimer);
    if (kind === "highlands" && !reduceMotion) scheduleStrike();
  }

  function scheduleStrike() {
    strikeTimer = setTimeout(() => {
      flash.classList.remove("is-striking");
      void flash.offsetWidth;
      flash.classList.add("is-striking");
      if (weatherKind === "highlands") scheduleStrike();
    }, 6000 + Math.random() * 9000);
  }

  function weatherTick(dt, t) {
    const w = weather.width, h = weather.height;
    wctx.clearRect(0, 0, w, h);
    for (let i = 0; i < motes.length; i++) {
      const m = motes[i];
      m.life += dt;
      const sway = weatherKind === "lowlands" ? Math.sin(t * 2 + i) * 6 : Math.sin(t + i) * 2;
      m.x += (m.vx + sway) * dt;
      m.y += m.vy * dt;
      if (m.x > w + 4 || m.x < -6 || m.y < -6 || m.y > h + 6) { motes[i] = spawnMote(weatherKind, false); continue; }
      const twinkle = weatherKind === "barrow" ? .5 + .5 * Math.sin(t * 8 + i * 3) : 1;
      wctx.globalAlpha = m.a * twinkle;
      wctx.fillStyle = m.c;
      if (m.len) wctx.fillRect(Math.round(m.x), Math.round(m.y), 1, Math.round(m.len));
      else wctx.fillRect(Math.round(m.x), Math.round(m.y), 1, 1);
    }
    wctx.globalAlpha = 1;
  }

  loopWhileVisible(weather, weatherTick);

  // ── Bonfire lantern: warm light follows the cursor through the dusk ──
  // It fades in as the pointer enters the section and out as it leaves, and stays under the pointer while the page scrolls.
  for (const section of $$(".chapter--dark")) {
    const lantern = $(".lantern", section);
    if (!lantern) continue;
    let pointer = null;
    const place = () => {
      if (!pointer) return;
      const r = section.getBoundingClientRect();
      lantern.style.setProperty("--mx", `${pointer.x - r.left}px`);
      lantern.style.setProperty("--my", `${pointer.y - r.top}px`);
    };
    const follow = (e) => {
      if (reduceMotion || e.pointerType !== "mouse") return;
      pointer = { x: e.clientX, y: e.clientY };
      place();
      lantern.classList.add("is-lit");
    };
    section.addEventListener("pointerenter", follow);
    section.addEventListener("pointermove", follow);
    section.addEventListener("pointerleave", () => { pointer = null; lantern.classList.remove("is-lit"); });
    addEventListener("scroll", place, { passive: true });
  }

  // ── Select-to-preview tabs ──────────────────────────────────────────
  // Each tab shows its gameplay capture; arrows, Home and End move between tabs.
  function setupTabs() {
    for (const group of $$("[data-tabs]")) {
      const tabs = $$('[role="tab"]', group);
      const select = (tab, focus) => {
        for (const t of tabs) {
          const on = t === tab;
          t.setAttribute("aria-selected", String(on));
          t.tabIndex = on ? 0 : -1;
          document.getElementById(t.getAttribute("aria-controls")).hidden = !on;
        }
        if (focus) tab.focus();
      };
      tabs.forEach((tab, i) => {
        tab.addEventListener("click", () => select(tab));
        tab.addEventListener("keydown", (e) => {
          const step = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 }[e.key];
          if (step) { e.preventDefault(); select(tabs[(i + step + tabs.length) % tabs.length], true); }
          else if (e.key === "Home") { e.preventDefault(); select(tabs[0], true); }
          else if (e.key === "End") { e.preventDefault(); select(tabs[tabs.length - 1], true); }
        });
      });
    }
  }

  // ── Next-chapter links close each chapter ────────────────────────────
  function setupNextLinks() {
    chapters.slice(1, -1).forEach((ch, i) => {
      const next = chapters[i + 2];
      const a = document.createElement("a");
      a.className = "next";
      a.href = `#${next.id}`;
      a.innerHTML = `<span>Next · ${next.numeral}</span><b></b><i aria-hidden="true">↓</i>`;
      a.querySelector("b").textContent = next.title;
      ch.el.append(a);
    });
  }

  // ── III · Vegetation: a strip of grass moved by wind and by scrolling ─
  function setupGrass() {
    const strip = $(".field");
    const canvas = $("canvas", strip);
    const ctx = canvas.getContext("2d");
    const COLORS = ["#384b3c", "#60724d", "#8b9b5e", "#beca82", "#b5c97c"];
    const H = canvas.height;
    let W = canvas.width, blades = [], gust = 0, lastScroll = scrollY;
    const soil = document.createElement("canvas");

    function build() {
      const scale = innerWidth < 640 ? 2 : 3;
      W = Math.ceil(strip.clientWidth / scale);
      canvas.width = W;
      soil.width = W; soil.height = H;
      const g = soil.getContext("2d");
      for (let x = 0; x < W; x++) {
        const top = 18 + Math.round(Math.sin(x / 23) * 2 + Math.sin(x / 7.3));
        g.fillStyle = "#60724d"; g.fillRect(x, top, 1, 2);
        g.fillStyle = "#7f995a"; g.fillRect(x, top + 2, 1, H);
      }
      blades = Array.from({ length: Math.round(W * 1.4) }, () => {
        const y = 20 + Math.random() * (H - 21);
        const depth = (y - 20) / (H - 21);
        return { x: Math.random() * W, y, h: 4 + Math.random() * 4 + depth * 5, phase: Math.random() * 6.28,
          c: COLORS[Math.min(4, Math.floor(depth * 2.2 + Math.random() * 2.6))] };
      }).sort((a, b) => a.y - b.y);
      draw(0);
    }

    // Scrolling stirs the wind in the scroll direction, then it settles.
    addEventListener("scroll", () => {
      gust = clamp(gust + (scrollY - lastScroll) * .004, -.6, .6);
      lastScroll = scrollY;
    }, { passive: true });

    function draw(t) {
      ctx.clearRect(0, 0, W, H);
      ctx.drawImage(soil, 0, 0);
      for (const b of blades) {
        const sway = Math.max(0, Math.sin(t * .6 - b.x * .02)) * .2;
        const a = .12 * Math.sin(t * 1.4 + b.x * .05 + b.phase * .3) + sway + gust;
        const sa = Math.sin(a), ca = Math.cos(a);
        ctx.fillStyle = b.c;
        for (let i = 0; i < b.h; i++) {
          const bend = i / b.h;
          ctx.fillRect(Math.round(b.x + sa * i * (.6 + bend * .6)), Math.round(b.y - ca * i), i < b.h * .55 ? 2 : 1, 1);
        }
      }
    }

    build();
    addEventListener("resize", build);
    loopWhileVisible(canvas, (dt, t) => { gust *= Math.pow(.15, dt); draw(t); });
  }

  // ── VIII · Architecture explorer ────────────────────────────────────────
  function setupArch() {
    const arch = $("#arch");
    const cols = $$(".arch__col", arch);
    const svg = $(".arch__wires", arch);
    const nameEl = $("[data-arch-name]", arch);
    const bodyEl = $("[data-arch-body]", arch);

    const NODES = [
      { id: "MI", col: 0, name: "PlayerMovementInput", body: "Reads keyboard, mouse or controller and hands gameplay an IMovementInput. Gameplay never sees hardware.", owns: "Bindings", pattern: "Interface adapter" },
      { id: "CI", col: 0, name: "PlayerCombatInput", body: "Attack, charge, throw, Recall and flask intent behind ICombatInput.", owns: "Bindings", pattern: "Interface adapter" },
      { id: "PM", col: 1, name: "PlayerMovement", body: "The only writer of the player's velocity. Each physics step one driver wins: stagger, dodge, attack movement, rope or free movement.", owns: "Velocity", pattern: "Single writer" },
      { id: "PC", col: 1, name: "PlayerCombatController", body: "Turns intent into weapon commands, and spawns the player's own axe on every scene load once the save owns it.", owns: "Attack sequencing", pattern: "Mediator" },
      { id: "AW", col: 1, name: "AxeWeapon", body: "Held, swinging, thrown, lodged, recalling. Its action clock drives hit pauses and the animation that samples it.", owns: "Weapon state", pattern: "Finite state machine" },
      { id: "HP", col: 1, name: "Damageable + HitReaction", body: "Health, poise and knockback. Raises HitReceived and Defeated without knowing what it belongs to.", owns: "Health, poise", pattern: "Observer" },
      { id: "EN", col: 1, name: "PredatorBrain + PursuitBrain", body: "PredatorBrain runs wolves: stalk, commit, punish, reposition. PursuitBrain runs the others from per-enemy data. Each picks one IEnemyAction; EnemyActionRunner owns its commitment, contact and cancellation.", owns: "Decisions", pattern: "Strategy" },
      { id: "AN", col: 2, name: "PlayerSpriteAnimator", body: "The only component that sets the body sprite. It samples movement and the weapon clock; it never writes back.", owns: "Body sprite", pattern: "Single writer" },
      { id: "FX", col: 2, name: "AxeView + AxeMeleeFeedback", body: "AxeView poses the weapon and gives Recall its translucent cyan and trail. AxeMeleeFeedback draws the swing's ground footprint from the same action clock.", owns: "Weapon visuals", pattern: "Sampler" },
      { id: "HUD", col: 2, name: "GameHud", body: "Samples health, stamina and flask state each frame, and listens to events for HUD reveals and feedback.", owns: "Screen UI", pattern: "Sampler + observer" },
      { id: "CS", col: 3, name: "GameSession", body: "One session across every scene. Captures and restores anything that implements IProgressParticipant. CheckpointService owns rest, travel and respawn; RewardService owns rewards and purchases.", owns: "Permanent progress", pattern: "Participants" },
      { id: "ST", col: 3, name: "PlayerPrefsProgressStore", body: "One versioned JSON record in PlayerPrefs, behind IProgressStore. ProgressMigrations upgrades old saves step by step so they keep their progress.", owns: "Storage", pattern: "Versioned migrations" },
    ];
    const EDGES = [
      ["MI", "PM", "IMovementInput"], ["CI", "PC", "ICombatInput"], ["PC", "AW"], ["AW", "HP", "CombatHit"], ["EN", "HP", "CombatHit"],
      ["PM", "AN", "samples", true], ["AW", "FX", "samples", true], ["HP", "HUD", "events", true],
      ["CS", "PM", "capture / restore"], ["CS", "ST"],
    ];

    const els = {};
    for (const n of NODES) {
      const b = document.createElement("button");
      b.type = "button"; b.className = "node"; b.textContent = n.name;
      b.setAttribute("aria-pressed", "false");
      b.addEventListener("click", () => select(n.id));
      cols[n.col].append(b);
      els[n.id] = b;
    }

    function wire() {
      const box = arch.getBoundingClientRect();
      const twoCol = getComputedStyle(arch).gridTemplateColumns.split(" ").length < 4;
      svg.setAttribute("viewBox", `0 0 ${box.width} ${box.height}`);
      svg.innerHTML = "";
      if (twoCol) return;  // on narrow screens, linked nodes highlight instead
      for (const [a, b, , dashed] of EDGES) {
        const ra = els[a].getBoundingClientRect(), rb = els[b].getBoundingClientRect();
        const ay = ra.top + ra.height / 2 - box.top, by = rb.top + rb.height / 2 - box.top;
        let d;
        if (Math.abs(ra.left - rb.left) < 4) {  // same column: loop out to the right
          const x = ra.right - box.left;
          d = `M${x} ${ay} C${x + 26} ${ay}, ${x + 26} ${by}, ${x} ${by}`;
        } else if (ra.left > rb.left) {         // backwards (persistence → gameplay): arc under
          const x1 = ra.left - box.left, x2 = rb.right - box.left, sag = Math.max(ay, by) + 40;
          d = `M${x1} ${ay} C${x1 - 40} ${sag}, ${x2 + 40} ${sag}, ${x2} ${by}`;
        } else {
          const x1 = ra.right - box.left, x2 = rb.left - box.left, mid = (x1 + x2) / 2;
          d = `M${x1} ${ay} C${mid} ${ay}, ${mid} ${by}, ${x2} ${by}`;
        }
        const p = document.createElementNS("http://www.w3.org/2000/svg", "path");
        p.setAttribute("d", d);
        p.dataset.a = a; p.dataset.b = b;
        if (dashed) p.classList.add("is-dashed");
        svg.append(p);
      }
      if (selected) select(selected, true);
    }

    let selected = null;
    function select(id, keep) {
      if (!keep && selected === id) id = null;
      selected = id;
      arch.classList.toggle("has-focus", !!id);
      for (const n of NODES) {
        els[n.id].setAttribute("aria-pressed", String(n.id === id));
        els[n.id].classList.toggle("is-linked", !!id && EDGES.some(([a, b]) => (a === id && b === n.id) || (b === id && a === n.id)));
      }
      for (const p of $$("path", svg)) {
        const on = id && (p.dataset.a === id || p.dataset.b === id);
        p.classList.toggle("is-lit", !!on);
        p.classList.toggle("is-dim", !!id && !on);
      }
      const n = NODES.find((x) => x.id === id);
      $(".arch__detail-kicker", arch).textContent = n ? ["Input", "Gameplay", "Presentation", "Persistence"][n.col] : "Select a system";
      nameEl.textContent = n ? n.name : "Every mechanic has one owner";
      if (!n) { bodyEl.textContent = "Click any component to see what it owns, what it emits and who listens."; return; }
      const talks = EDGES.filter(([a, b]) => a === id || b === id)
        .map(([a, b, label]) => `${a === id ? "→" : "←"} ${NODES.find((x) => x.id === (a === id ? b : a)).name}${label ? ` (${label})` : ""}`);
      bodyEl.innerHTML = "";
      bodyEl.append(n.body);
      const dl = document.createElement("dl");
      dl.innerHTML = `<dt>Owns</dt><dd></dd><dt>Pattern</dt><dd></dd><dt>Talks to</dt><dd></dd>`;
      const dds = $$("dd", dl);
      dds[0].textContent = n.owns; dds[1].textContent = n.pattern; dds[2].textContent = talks.join(" · ");
      bodyEl.append(dl);
    }

    wire();
    new ResizeObserver(wire).observe(arch);
  }

  // ── Boot ─────────────────────────────────────────────────────────────
  function layout() { fitPixels(); layoutRail(); layoutMap(); sizeWeather(); onScroll(); }
  addEventListener("scroll", onScroll, { passive: true });
  addEventListener("resize", layout);
  addEventListener("load", layout);
  document.fonts?.ready.then(layout);
  layout();
  setWeather(root.dataset.region);
  setupTabs();
  // Restrained / Cinematic: the range input moves the split between the two stills.
  for (const compare of $$("[data-compare]")) {
    const range = $(".compare__range", compare);
    const split = () => compare.style.setProperty("--split", `${range.value}%`);
    range.addEventListener("input", split);
    split();
  }
  // Gameplay videos loop on their own unless the reader prefers reduced motion; then they wait, with controls.
  const syncVideos = () => {
    const still = motionPreference.matches;
    for (const video of $$(".plate video")) {
      video.controls = still;
      if (still) video.pause(); else video.play().catch(() => {});
    }
  };
  syncVideos();
  motionPreference.addEventListener("change", () => syncVideos());
  setupNextLinks();
  setupGrass();
  setupArch();
  motionPreference.addEventListener("change", () => {
    reduceMotion = motionPreference.matches;
    setWeather(currentRegion || root.dataset.region);
    if (reduceMotion) {
      clearTimeout(idleTimer);
      flash.classList.remove("is-striking");
      wctx.clearRect(0, 0, weather.width, weather.height);
      railHero.src = walkFrames.south[0];
    }
  });
  if (!reduceMotion) setTimeout(() => { if (!reduceMotion) showPlace(currentRegion || root.dataset.region); }, 700);
})();
