# Firebreak — Greybox Core-Loop Prototype (Design Spec)

**Date:** 2026-07-26
**Status:** Approved for planning
**Parent design:** `Docs/firebreak-game-design.md` (full game vision)

---

## 1. Purpose

Build a **playable greybox prototype** whose single job is to prove that the
**Tight/Wide toggle is fun** under the game's two simultaneous risk axes (fire
ring vs. crows, falling branches vs. farmer). Zero art investment. If the toggle
isn't compelling with primitives on a flat plane, no amount of art will save it —
so we validate the core loop first, then build outward.

This spec covers **only** the greybox. Art, terrain, VFX, audio, the full
5-minute wave curve, and the boids upgrade are explicitly out of scope and will
be scoped separately once the loop is proven.

## 2. Success Criteria

The prototype succeeds if a playtester can, with no text instructions:

1. Discover that holding **Wide** roots the farmer and sends crows to collect parts.
2. Feel the tension of the **two risks** — losing crows to the fire line, and
   getting struck while rooted too long.
3. Experience the full loop: **sprint (Tight) → plant & command (Wide) → recall
   → deposit at sprinkler → repeat**, under escalating pressure.
4. Reach a clear **win** (sprinklers activated in time) or **loss** (fire reaches
   house, or branch strike), then **instantly retry**.

The prototype is a tuning instrument: the escalation curve, radii, and timings
must be trivially adjustable in the Inspector.

## 3. Scope

### In scope
- Orthographic camera, locked isometric angle, whole play space visible. Flat ground plane.
- **Farmer:** capsule, real-time movement. Rooted while Wide held. Placeholder "commanding" visual on entering Wide (e.g. color flash / raised marker).
- **Input:** two inputs only — Move + hold-Wide. KB/M (WASD + hold Space) and gamepad (left stick + hold trigger), via the existing `InputSystem_Actions` asset.
- **Crows (~16, tunable):** primitives, simple per-crow steering with noise/banking. Tight = orbit farmer; Wide = fan out and seek nearest unclaimed parts; carry back on recall.
- **Parts:** fixed scatter, no respawn. Placement includes edge-adjacent (fire-risky), interior (safe from fire), and **one dead-center** (validates the branch-risk axis).
- **Sprinklers (1–2 for greybox, tunable):** walk-to-deposit; activate when fed their required part count.
- **Fire Ring:** advances inward from map edges; kills any crow caught in Wide outside the farmer's safe radius; triggers loss when it reaches the house.
- **Branch hazard:** after a grace period while rooted in Wide, telegraph-then-strike circles around the farmer's fixed position; a hit = instant fail. Frequency rises with hold duration and overall intensity.
- **Escalation:** a single intensity value ramps over the run (compressed timeline), driving both fire speed and branch frequency from one place.
- **Win/Loss + instant retry** (no menus).

### Out of scope (deferred)
- All art, animation, terrain, VFX, audio.
- The full 5-minute wave-pacing curve (greybox uses a compressed, single-curve ramp).
- Boids flocking upgrade (greybox uses simple steering).
- Polished win/recap screens (greybox shows a minimal result + retry).
- The "losing all crows = hard fail" rule (left as natural unwinnability; revisit after playtest).
- Late-wave branches threatening crows (kept as farmer-only per parent design).

## 4. Architecture

Small showcase → a handful of focused, single-purpose components. Loose
relationships communicate through **ScriptableObject event channels**; tight
relationships (Farmer→Flock→Crow) use direct references.

### Core loop / state
- **`RunManager`** — owns run phase (`Playing` / `Won` / `Lost`), performs win/loss resolution, and handles instant retry (scene/state reset). Listens to result events rather than polling every system. Kept thin.
- **`EscalationCurve`** (ScriptableObject + a runtime driver) — single source of `Intensity` (0→1) over run time. Fire speed and branch frequency both read from it, so all escalation tuning lives in one asset.

### Player
- **`InputReader`** — wraps the Input System asset; exposes `MoveInput` (Vector2) and `WideHeld` (bool). The only script that touches raw input.
- **`FarmerController`** — movement + rooted-while-Wide. Exposes `Position`, `IsWide`. Raises `WideEntered` / `WideExited` events. Reads `InputReader`.

### Flock
- **`FlockController`** — owns the crows; while Wide, assigns part-targets (via `PartRegistry`) and recalls on release. Reads `FarmerController` (`IsWide`, `Position`). Holds direct references to its `Crow`s.
- **`Crow`** — one bird's steering + state machine (`Orbiting` / `Seeking` / `Carrying` / `Dead`). Knows nothing about game rules; just moves toward an assigned target and reports arrival/pickup. Raises `CrowDied` when caught by fire.

### World / objects
- **`Part`** — pickup with `Claimed` flag and carry state.
- **`PartRegistry`** — answers "nearest unclaimed part to point X within radius." Keeps `FlockController` simple; single place that tracks live parts.
- **`Sprinkler`** — required part count, `Deposit(count)`, activation. Raises `SprinklerActivated`.

### Hazards
- **`FireRing`** — advancing boundary driven by `EscalationCurve`. Each frame, tests Wide crows against the farmer's safe radius and kills those caught (`Crow` → `CrowDied`). Raises `FireReachedHouse` (→ loss).
- **`BranchHazard`** — active only while farmer is rooted in Wide past the grace period. Telegraph→strike around `FarmerController.Position`; frequency from `EscalationCurve` + current hold duration. On a hit that overlaps the farmer, raises `FarmerStruck` (→ loss). Cleared/reset on Wide exit.

### Event channels (ScriptableObject assets)
Loose, broadcast-style signals; listeners subscribe without knowing the sender:
- `WideEntered`, `WideExited` — farmer command-state transitions.
- `CrowDied` — a crow lost to fire (flock size, later UI).
- `PartDeposited`, `SprinklerActivated` — progress toward win.
- `FarmerStruck`, `FireReachedHouse` — loss triggers.
- `RunWon`, `RunLost`, `RunRestarted` — top-level run lifecycle from `RunManager`.

Pattern: a generic `GameEvent` SO (raise + subscriber list) plus a small
`GameEventListener` component, or typed variants where a payload is needed
(e.g. `SprinklerActivated` carrying which sprinkler). Kept minimal — one generic
plus the couple of typed ones actually used.

### Data flow
```
InputReader → FarmerController → FlockController → Crow
                     │                                │
                     ├── WideEntered/Exited ─────────►│ (via FlockController)
                     ▼                                ▼
                BranchHazard                     FireRing (kills Wide crows)
                     │                                │
EscalationCurve ─────┴───────────────┬────────────────┘
                                     ▼
                                 RunManager  ◄── SprinklerActivated / FarmerStruck /
                                                  FireReachedHouse  → RunWon / RunLost
```

## 5. Key Behaviors

- **Tight (default):** farmer moves freely; crows orbit/idle near farmer and do **not** collect. Crows returning from a prior Wide finish returning safely (recall is always safe for en-route crows).
- **Wide (held):** farmer **cannot move at all**; commanding visual plays; crows fan out and seek nearest unclaimed parts. Crows only collect while Wide is active.
- **Grace period (~1–2s, tunable):** short Wide commands are always safe from branches. After grace, branch telegraphs begin and grow more frequent the longer Wide is held; releasing to Tight clears any pending telegraph and resets the timer.
- **Fire ring:** continuous inward advance, accelerating via `EscalationCurve`. A crow outside the farmer's safe radius while Wide is caught and lost permanently.
- **Deposit:** on recall, crows return carrying parts to the farmer; farmer then walks to a sprinkler to deposit. Sprinkler activates at its required count.

## 6. Win / Loss

- **Win:** required sprinklers activated before fire reaches the house → `RunWon`.
- **Loss:** fire reaches the house (`FireReachedHouse`) **or** farmer struck by a branch (`FarmerStruck`) → `RunLost`.
- **Retry:** minimal result indication, then instant reset of the same run (`RunRestarted`) — no menu detour.

## 7. Testing Approach

Feel is validated by hand, but the deterministic pieces get edit-mode/play-mode tests:
- **`PartRegistry`** — nearest-unclaimed queries, claim/release, radius filtering.
- **`Sprinkler`** — accumulates to required count, activates once, ignores extra.
- **`EscalationCurve`** — intensity mapping over time is monotonic and clamped [0,1].
- **`FireRing`** — a crow position inside/outside the safe radius resolves to caught/safe correctly.
- **Event channels** — raise notifies all listeners; unsubscribe stops delivery.

State-machine transitions (`Crow`, `FarmerController` rooted state) get lightweight play-mode checks. Steering *aesthetics* are tuned by hand, not tested.

## 8. Tunables (Inspector-exposed)

Crow count; orbit radius; farmer safe radius; part scatter/count; sprinkler count
& required parts; grace-period length; branch telegraph→strike delay and radius;
branch frequency curve; fire advance speed & acceleration; total run length; the
`EscalationCurve` asset shape. All expected to change during play-tuning.

## 9. Open Questions (revisit after first playtest)

- Whether zero remaining crows should be an explicit hard-fail.
- Whether later-intensity branches should also threaten crows.
- Win threshold: all sprinklers vs. majority.
- Exact numbers for everything in §8.
