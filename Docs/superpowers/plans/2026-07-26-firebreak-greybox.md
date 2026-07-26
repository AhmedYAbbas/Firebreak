# Firebreak Greybox Core-Loop Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a playable greybox prototype that proves the Tight/Wide toggle is fun under both risk axes (fire ring vs. crows, falling branches vs. farmer), using primitives on a flat plane.

**Architecture:** Focused single-purpose components. Tight relationships (Farmer→Flock→Crow, hazards→RunManager) use direct references; loose signals (crow died, sprinkler activated, run won/lost) go through ScriptableObject `GameEvent` channels. Deterministic logic (registry queries, sprinkler counting, escalation curve, fire catch, branch hit) is TDD'd with the Unity Test Framework; steering/feel is tuned by hand.

**Tech Stack:** Unity 6000.5.5f1, URP, new Input System (`com.unity.inputsystem` 1.19.0), Unity Test Framework 1.7.0 (NUnit), C#.

## Global Constraints

- Unity editor version: **6000.5.5f1** (path: `C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe`).
- Namespace for all runtime code: **`Firebreak`**. Test code: **`Firebreak.Tests`**.
- All game code lives under `Assets/Firebreak/`. Do not modify the existing `Assets/TutorialInfo` or `Assets/Settings` content.
- Two inputs only: Move + hold-Wide. No other player verbs.
- Farmer is **fully rooted** (zero movement) while Wide is held — this is load-bearing, never relax it.
- Crows collect parts **only** while Wide is active.
- Loss/win resolution funnels exclusively through `RunManager` via events — hazards never end the run directly.
- Every new `.cs`, `.asmdef`, folder, and `.asset` needs a Unity `.meta` file. If Unity is open it generates these on import; if working headless, generate them (GUIDs must be 32 lowercase hex chars — use the `unity-meta-file` skill).
- Escalation is single-sourced: fire speed and branch frequency both derive from `EscalationCurve` via `RunManager.Intensity`. Never hardcode a second escalation clock.

**Running tests (used throughout this plan):**
EditMode via CLI —
```
& "C:\Program Files\Unity\Hub\Editor\6000.5.5f1\Editor\Unity.exe" -batchmode -runTests -projectPath "C:\Ahmed\Dev\Unity\Firebreak" -testPlatform EditMode -testResults "C:\Ahmed\Dev\Unity\Firebreak\Temp\editmode-results.xml" -logFile -
```
PlayMode: replace `-testPlatform EditMode` with `-testPlatform PlayMode`. Filter one test with `-testFilter "Firebreak.Tests.FullyQualified.TestName"`.
Equivalent GUI path: Unity → Window → General → Test Runner → run EditMode/PlayMode. Unity must not be open in another instance when using the CLI (it locks the project).

---

## File Structure

```
Assets/Firebreak/
  Scripts/
    Firebreak.Runtime.asmdef        # runtime asm, refs Unity.InputSystem
    Events/
      GameEvent.cs                  # SO channel: Register/Unregister/Raise
      GameEventListener.cs          # MonoBehaviour bridge → UnityEvent
    Core/
      EscalationCurve.cs            # SO: Evaluate(elapsed) → intensity 0..1
      RunManager.cs                 # phase, intensity clock, win/loss, retry
    Player/
      InputReader.cs                # wraps Input System → MoveInput/WideHeld
      FarmerController.cs           # movement, rooted-in-Wide, Wide events
    Flock/
      Crow.cs                       # one bird: steering + state machine
      FlockController.cs            # owns crows, assigns targets, recalls
    World/
      Part.cs                       # pickup: Claimed flag, Position
      PartRegistry.cs               # nearest-unclaimed queries
      Sprinkler.cs                  # required count, Deposit, activation
    Hazards/
      FireRing.cs                   # shrinking safe interior, crow catch
      BranchHazard.cs               # telegraph→strike while rooted
  Tests/
    EditMode/
      Firebreak.Tests.EditMode.asmdef
      GameEventTests.cs
      EscalationCurveTests.cs
      PartRegistryTests.cs
      SprinklerTests.cs
      RunManagerTests.cs
      FireRingTests.cs
      BranchHazardTests.cs
    PlayMode/
      Firebreak.Tests.PlayMode.asmdef
      FarmerControllerTests.cs
      FlockControllerTests.cs
  Events/                            # SO instances (created in Task 11)
  Settings/                          # EscalationCurve asset (Task 11)
  Scenes/
    Greybox.unity                    # the prototype scene (Task 11)
```

---

## Task 1: Project scaffolding & assembly definitions

**Files:**
- Create: `Assets/Firebreak/Scripts/Firebreak.Runtime.asmdef`
- Create: `Assets/Firebreak/Tests/EditMode/Firebreak.Tests.EditMode.asmdef`
- Create: `Assets/Firebreak/Tests/PlayMode/Firebreak.Tests.PlayMode.asmdef`
- Create: `Assets/Firebreak/Tests/EditMode/ScaffoldSmokeTest.cs` (deleted at end of task)

**Interfaces:**
- Consumes: nothing.
- Produces: assembly `Firebreak.Runtime` (namespace `Firebreak`), test assemblies `Firebreak.Tests.EditMode` and `Firebreak.Tests.PlayMode` (namespace `Firebreak.Tests`), both referencing `Firebreak.Runtime`.

- [ ] **Step 1: Create the runtime asmdef**

`Assets/Firebreak/Scripts/Firebreak.Runtime.asmdef`:
```json
{
    "name": "Firebreak.Runtime",
    "rootNamespace": "Firebreak",
    "references": [
        "Unity.InputSystem"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "autoReferenced": true
}
```

- [ ] **Step 2: Create the EditMode test asmdef**

`Assets/Firebreak/Tests/EditMode/Firebreak.Tests.EditMode.asmdef`:
```json
{
    "name": "Firebreak.Tests.EditMode",
    "rootNamespace": "Firebreak.Tests",
    "references": [
        "Firebreak.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.InputSystem"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "autoReferenced": false
}
```

- [ ] **Step 3: Create the PlayMode test asmdef**

`Assets/Firebreak/Tests/PlayMode/Firebreak.Tests.PlayMode.asmdef`:
```json
{
    "name": "Firebreak.Tests.PlayMode",
    "rootNamespace": "Firebreak.Tests",
    "references": [
        "Firebreak.Runtime",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Unity.InputSystem"
    ],
    "includePlatforms": [],
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "autoReferenced": false
}
```

- [ ] **Step 4: Write a scaffold smoke test**

`Assets/Firebreak/Tests/EditMode/ScaffoldSmokeTest.cs`:
```csharp
using NUnit.Framework;

namespace Firebreak.Tests
{
    public class ScaffoldSmokeTest
    {
        [Test]
        public void Assemblies_Compile_And_Run()
        {
            Assert.Pass();
        }
    }
}
```

- [ ] **Step 5: Let Unity import (generates .meta files) and run EditMode tests**

Open the project in Unity once so it compiles the asmdefs and generates `.meta` files (or generate metas headlessly). Then run the EditMode suite (see "Running tests" above).
Expected: `ScaffoldSmokeTest.Assemblies_Compile_And_Run` PASSES. This confirms the three assemblies compile and reference each other.

- [ ] **Step 6: Delete the smoke test and commit**

Delete `Assets/Firebreak/Tests/EditMode/ScaffoldSmokeTest.cs` and its `.meta`.
```bash
git add Assets/Firebreak
git commit -m "chore: scaffold Firebreak asmdefs and test assemblies"
```

---

## Task 2: GameEvent channel + listener

**Files:**
- Create: `Assets/Firebreak/Scripts/Events/GameEvent.cs`
- Create: `Assets/Firebreak/Scripts/Events/GameEventListener.cs`
- Test: `Assets/Firebreak/Tests/EditMode/GameEventTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `Firebreak.GameEvent : ScriptableObject` — `void Register(System.Action)`, `void Unregister(System.Action)`, `void Raise()`.
  - `Firebreak.GameEventListener : MonoBehaviour` — serialized `GameEvent Event`, `UnityEngine.Events.UnityEvent Response`; subscribes on enable, unsubscribes on disable.

- [ ] **Step 1: Write the failing test**

`Assets/Firebreak/Tests/EditMode/GameEventTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class GameEventTests
    {
        [Test]
        public void Raise_InvokesAllRegisteredListeners()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int a = 0, b = 0;
            System.Action la = () => a++;
            System.Action lb = () => b++;
            evt.Register(la);
            evt.Register(lb);

            evt.Raise();

            Assert.AreEqual(1, a);
            Assert.AreEqual(1, b);
        }

        [Test]
        public void Unregister_StopsDelivery()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int count = 0;
            System.Action l = () => count++;
            evt.Register(l);
            evt.Unregister(l);

            evt.Raise();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Register_IsIdempotent()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int count = 0;
            System.Action l = () => count++;
            evt.Register(l);
            evt.Register(l);

            evt.Raise();

            Assert.AreEqual(1, count);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests. Expected: FAIL — `GameEvent` does not exist / does not compile.

- [ ] **Step 3: Implement GameEvent**

`Assets/Firebreak/Scripts/Events/GameEvent.cs`:
```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Firebreak
{
    /// <summary>Broadcast-style ScriptableObject event channel. Listeners
    /// subscribe without knowing the sender.</summary>
    [CreateAssetMenu(menuName = "Firebreak/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> _listeners = new();

        public void Register(Action listener)
        {
            if (listener != null && !_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void Unregister(Action listener)
        {
            _listeners.Remove(listener);
        }

        public void Raise()
        {
            // Iterate backwards so a listener can safely unregister during Raise.
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i]?.Invoke();
        }
    }
}
```

- [ ] **Step 4: Implement GameEventListener**

`Assets/Firebreak/Scripts/Events/GameEventListener.cs`:
```csharp
using UnityEngine;
using UnityEngine.Events;

namespace Firebreak
{
    /// <summary>Inspector bridge from a GameEvent to a UnityEvent response.</summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent _event;
        [SerializeField] private UnityEvent _response;

        private void OnEnable() => _event?.Register(OnRaised);
        private void OnDisable() => _event?.Unregister(OnRaised);

        private void OnRaised() => _response?.Invoke();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run EditMode tests. Expected: all three `GameEventTests` PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add GameEvent ScriptableObject channel and listener"
```

---

## Task 3: EscalationCurve

**Files:**
- Create: `Assets/Firebreak/Scripts/Core/EscalationCurve.cs`
- Test: `Assets/Firebreak/Tests/EditMode/EscalationCurveTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `Firebreak.EscalationCurve : ScriptableObject` — `float Evaluate(float elapsedSeconds)` returning intensity clamped to `[0,1]`; serialized `AnimationCurve` and `float RunLength`.

- [ ] **Step 1: Write the failing test**

`Assets/Firebreak/Tests/EditMode/EscalationCurveTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class EscalationCurveTests
    {
        private static EscalationCurve LinearCurve(float runLength)
        {
            var c = ScriptableObject.CreateInstance<EscalationCurve>();
            c.SetForTests(AnimationCurve.Linear(0f, 0f, 1f, 1f), runLength);
            return c;
        }

        [Test]
        public void Evaluate_AtStart_IsZero()
        {
            var c = LinearCurve(300f);
            Assert.AreEqual(0f, c.Evaluate(0f), 0.001f);
        }

        [Test]
        public void Evaluate_AtEnd_IsOne()
        {
            var c = LinearCurve(300f);
            Assert.AreEqual(1f, c.Evaluate(300f), 0.001f);
        }

        [Test]
        public void Evaluate_PastEnd_ClampsToOne()
        {
            var c = LinearCurve(300f);
            Assert.AreEqual(1f, c.Evaluate(999f), 0.001f);
        }

        [Test]
        public void Evaluate_IsMonotonicNonDecreasing_ForRisingCurve()
        {
            var c = LinearCurve(300f);
            float prev = -1f;
            for (float t = 0f; t <= 300f; t += 15f)
            {
                float v = c.Evaluate(t);
                Assert.GreaterOrEqual(v, prev);
                prev = v;
            }
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests. Expected: FAIL — `EscalationCurve` / `SetForTests` not defined.

- [ ] **Step 3: Implement EscalationCurve**

`Assets/Firebreak/Scripts/Core/EscalationCurve.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    /// <summary>Single source of run "intensity" (0..1) over time. Fire speed
    /// and branch frequency both derive from this so escalation tunes in one place.</summary>
    [CreateAssetMenu(menuName = "Firebreak/Escalation Curve")]
    public class EscalationCurve : ScriptableObject
    {
        [SerializeField]
        private AnimationCurve _intensityOverNormalizedTime =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Total run length in seconds. Compressed for greybox tuning.")]
        [SerializeField] private float _runLength = 300f;

        public float RunLength => _runLength;

        /// <summary>Intensity at the given elapsed seconds, clamped to [0,1].</summary>
        public float Evaluate(float elapsedSeconds)
        {
            float t = _runLength <= 0f ? 1f : Mathf.Clamp01(elapsedSeconds / _runLength);
            return Mathf.Clamp01(_intensityOverNormalizedTime.Evaluate(t));
        }

        /// <summary>Test-only injection of curve + length.</summary>
        public void SetForTests(AnimationCurve curve, float runLength)
        {
            _intensityOverNormalizedTime = curve;
            _runLength = runLength;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests. Expected: all four `EscalationCurveTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add EscalationCurve single-source intensity"
```

---

## Task 4: Part + PartRegistry

**Files:**
- Create: `Assets/Firebreak/Scripts/World/Part.cs`
- Create: `Assets/Firebreak/Scripts/World/PartRegistry.cs`
- Test: `Assets/Firebreak/Tests/EditMode/PartRegistryTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces:
  - `Firebreak.Part : MonoBehaviour` — `bool Claimed { get; set; }`, `Vector3 Position { get; }`, `bool Collected { get; set; }`.
  - `Firebreak.PartRegistry : MonoBehaviour` — `void Add(Part)`, `void Remove(Part)`, `Part FindNearestUnclaimed(Vector3 from, float radius)` (returns null if none unclaimed within radius), `int UnclaimedCount { get; }`.

- [ ] **Step 1: Write the failing test**

`Assets/Firebreak/Tests/EditMode/PartRegistryTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class PartRegistryTests
    {
        private PartRegistry _registry;
        private readonly System.Collections.Generic.List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            _registry = new GameObject("Registry").AddComponent<PartRegistry>();
            _spawned.Add(_registry.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private Part MakePart(Vector3 pos)
        {
            var go = new GameObject("Part");
            go.transform.position = pos;
            _spawned.Add(go);
            return go.AddComponent<Part>();
        }

        [Test]
        public void FindNearestUnclaimed_ReturnsClosestWithinRadius()
        {
            _registry.Add(MakePart(new Vector3(5, 0, 0)));
            var near = MakePart(new Vector3(1, 0, 0));
            _registry.Add(near);

            var result = _registry.FindNearestUnclaimed(Vector3.zero, 10f);

            Assert.AreSame(near, result);
        }

        [Test]
        public void FindNearestUnclaimed_IgnoresClaimed()
        {
            var near = MakePart(new Vector3(1, 0, 0));
            near.Claimed = true;
            _registry.Add(near);
            var far = MakePart(new Vector3(4, 0, 0));
            _registry.Add(far);

            var result = _registry.FindNearestUnclaimed(Vector3.zero, 10f);

            Assert.AreSame(far, result);
        }

        [Test]
        public void FindNearestUnclaimed_ReturnsNull_WhenNoneInRadius()
        {
            _registry.Add(MakePart(new Vector3(50, 0, 0)));

            var result = _registry.FindNearestUnclaimed(Vector3.zero, 10f);

            Assert.IsNull(result);
        }

        [Test]
        public void UnclaimedCount_ExcludesClaimedAndCollected()
        {
            var p1 = MakePart(Vector3.zero); _registry.Add(p1);
            var p2 = MakePart(Vector3.one); p2.Claimed = true; _registry.Add(p2);
            var p3 = MakePart(Vector3.up); p3.Collected = true; _registry.Add(p3);

            Assert.AreEqual(1, _registry.UnclaimedCount);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests. Expected: FAIL — `Part` / `PartRegistry` not defined.

- [ ] **Step 3: Implement Part**

`Assets/Firebreak/Scripts/World/Part.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    /// <summary>A scattered pickup. Claimed = a crow is en route; Collected =
    /// carried/deposited and no longer a valid target.</summary>
    public class Part : MonoBehaviour
    {
        public bool Claimed { get; set; }
        public bool Collected { get; set; }
        public Vector3 Position => transform.position;

        private PartRegistry _registry;

        private void OnEnable()
        {
            _registry = FindFirstObjectByType<PartRegistry>();
            _registry?.Add(this);
        }

        private void OnDisable() => _registry?.Remove(this);
    }
}
```

- [ ] **Step 4: Implement PartRegistry**

`Assets/Firebreak/Scripts/World/PartRegistry.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Firebreak
{
    /// <summary>Answers "nearest unclaimed part" queries so the flock stays simple.</summary>
    public class PartRegistry : MonoBehaviour
    {
        private readonly List<Part> _parts = new();

        public void Add(Part part)
        {
            if (part != null && !_parts.Contains(part)) _parts.Add(part);
        }

        public void Remove(Part part) => _parts.Remove(part);

        public int UnclaimedCount
        {
            get
            {
                int n = 0;
                foreach (var p in _parts)
                    if (p != null && !p.Claimed && !p.Collected) n++;
                return n;
            }
        }

        public Part FindNearestUnclaimed(Vector3 from, float radius)
        {
            Part best = null;
            float bestSqr = radius * radius;
            foreach (var p in _parts)
            {
                if (p == null || p.Claimed || p.Collected) continue;
                float sqr = (p.Position - from).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = p;
                }
            }
            return best;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run EditMode tests. Expected: all four `PartRegistryTests` PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add Part pickup and PartRegistry nearest-query"
```

---

## Task 5: Sprinkler

**Files:**
- Create: `Assets/Firebreak/Scripts/World/Sprinkler.cs`
- Test: `Assets/Firebreak/Tests/EditMode/SprinklerTests.cs`

**Interfaces:**
- Consumes: `Firebreak.GameEvent` (Task 2).
- Produces: `Firebreak.Sprinkler : MonoBehaviour` — `int RequiredParts` (serialized), `bool IsActive { get; }`, `int Deposited { get; }`, `bool Deposit(int count)` returns `true` on the deposit that activates it (false otherwise); raises its `_onActivated` GameEvent exactly once.

- [ ] **Step 1: Write the failing test**

`Assets/Firebreak/Tests/EditMode/SprinklerTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class SprinklerTests
    {
        private Sprinkler MakeSprinkler(int required, GameEvent evt)
        {
            var s = new GameObject("Sprinkler").AddComponent<Sprinkler>();
            s.ConfigureForTests(required, evt);
            return s;
        }

        [Test]
        public void Deposit_BelowRequired_DoesNotActivate()
        {
            var s = MakeSprinkler(2, ScriptableObject.CreateInstance<GameEvent>());
            bool activated = s.Deposit(1);
            Assert.IsFalse(activated);
            Assert.IsFalse(s.IsActive);
        }

        [Test]
        public void Deposit_ReachingRequired_ActivatesOnce_AndRaisesEvent()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int raised = 0;
            evt.Register(() => raised++);
            var s = MakeSprinkler(2, evt);

            s.Deposit(1);
            bool activated = s.Deposit(1);

            Assert.IsTrue(activated);
            Assert.IsTrue(s.IsActive);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void Deposit_AfterActive_ReturnsFalse_AndDoesNotRaiseAgain()
        {
            var evt = ScriptableObject.CreateInstance<GameEvent>();
            int raised = 0;
            evt.Register(() => raised++);
            var s = MakeSprinkler(1, evt);

            s.Deposit(1);
            bool again = s.Deposit(1);

            Assert.IsFalse(again);
            Assert.AreEqual(1, raised);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests. Expected: FAIL — `Sprinkler` not defined.

- [ ] **Step 3: Implement Sprinkler**

`Assets/Firebreak/Scripts/World/Sprinkler.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    /// <summary>A perimeter sprinkler. Accepts deposited parts; activates once
    /// its required count is met and raises its activation event.</summary>
    public class Sprinkler : MonoBehaviour
    {
        [SerializeField] private int _requiredParts = 2;
        [SerializeField] private GameEvent _onActivated;

        public int RequiredParts => _requiredParts;
        public int Deposited { get; private set; }
        public bool IsActive { get; private set; }

        /// <summary>Deposits parts. Returns true only on the call that activates it.</summary>
        public bool Deposit(int count)
        {
            if (IsActive) return false;
            Deposited += Mathf.Max(0, count);
            if (Deposited >= _requiredParts)
            {
                IsActive = true;
                _onActivated?.Raise();
                return true;
            }
            return false;
        }

        /// <summary>Test-only configuration.</summary>
        public void ConfigureForTests(int required, GameEvent onActivated)
        {
            _requiredParts = required;
            _onActivated = onActivated;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests. Expected: all three `SprinklerTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add Sprinkler deposit/activation"
```

---

## Task 6: RunManager (phase, intensity clock, win/loss, retry)

**Files:**
- Create: `Assets/Firebreak/Scripts/Core/RunManager.cs`
- Test: `Assets/Firebreak/Tests/EditMode/RunManagerTests.cs`

**Interfaces:**
- Consumes: `EscalationCurve` (Task 3), `GameEvent` (Task 2).
- Produces: `Firebreak.RunManager : MonoBehaviour` —
  - enum `Firebreak.RunPhase { Playing, Won, Lost }`; `RunPhase Phase { get; }`.
  - `float Intensity { get; }` (from EscalationCurve at current elapsed).
  - handlers `void NotifySprinklerActivated()`, `void NotifyFarmerStruck()`, `void NotifyFireReachedHouse()` (wired to the corresponding GameEvents in-scene; also directly callable in tests).
  - serialized `int _requiredSprinklers`; raises `_onRunWon` / `_onRunLost` once each.
  - `void ConfigureForTests(EscalationCurve, int requiredSprinklers, GameEvent won, GameEvent lost)` and `void SetElapsedForTests(float)`.

- [ ] **Step 1: Write the failing test**

`Assets/Firebreak/Tests/EditMode/RunManagerTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class RunManagerTests
    {
        private RunManager MakeManager(int requiredSprinklers,
            out GameEvent won, out GameEvent lost)
        {
            won = ScriptableObject.CreateInstance<GameEvent>();
            lost = ScriptableObject.CreateInstance<GameEvent>();
            var curve = ScriptableObject.CreateInstance<EscalationCurve>();
            curve.SetForTests(AnimationCurve.Linear(0f, 0f, 1f, 1f), 100f);
            var rm = new GameObject("RunManager").AddComponent<RunManager>();
            rm.ConfigureForTests(curve, requiredSprinklers, won, lost);
            return rm;
        }

        [Test]
        public void ActivatingAllRequiredSprinklers_Wins()
        {
            var rm = MakeManager(2, out var won, out var lost);
            int wonCount = 0, lostCount = 0;
            won.Register(() => wonCount++);
            lost.Register(() => lostCount++);

            rm.NotifySprinklerActivated();
            Assert.AreEqual(RunPhase.Playing, rm.Phase);
            rm.NotifySprinklerActivated();

            Assert.AreEqual(RunPhase.Won, rm.Phase);
            Assert.AreEqual(1, wonCount);
            Assert.AreEqual(0, lostCount);
        }

        [Test]
        public void FarmerStruck_Loses()
        {
            var rm = MakeManager(2, out var won, out var lost);
            int lostCount = 0;
            lost.Register(() => lostCount++);

            rm.NotifyFarmerStruck();

            Assert.AreEqual(RunPhase.Lost, rm.Phase);
            Assert.AreEqual(1, lostCount);
        }

        [Test]
        public void FireReachedHouse_Loses()
        {
            var rm = MakeManager(2, out _, out var lost);
            int lostCount = 0;
            lost.Register(() => lostCount++);

            rm.NotifyFireReachedHouse();

            Assert.AreEqual(RunPhase.Lost, rm.Phase);
            Assert.AreEqual(1, lostCount);
        }

        [Test]
        public void AfterLoss_FurtherNotifications_DoNotChangePhaseOrReRaise()
        {
            var rm = MakeManager(1, out var won, out var lost);
            int wonCount = 0, lostCount = 0;
            won.Register(() => wonCount++);
            lost.Register(() => lostCount++);

            rm.NotifyFarmerStruck();
            rm.NotifySprinklerActivated(); // would otherwise win with required=1

            Assert.AreEqual(RunPhase.Lost, rm.Phase);
            Assert.AreEqual(1, lostCount);
            Assert.AreEqual(0, wonCount);
        }

        [Test]
        public void Intensity_ReflectsElapsedThroughCurve()
        {
            var rm = MakeManager(1, out _, out _);
            rm.SetElapsedForTests(50f); // linear, runLength 100 => 0.5
            Assert.AreEqual(0.5f, rm.Intensity, 0.001f);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests. Expected: FAIL — `RunManager` / `RunPhase` not defined.

- [ ] **Step 3: Implement RunManager**

`Assets/Firebreak/Scripts/Core/RunManager.cs`:
```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Firebreak
{
    public enum RunPhase { Playing, Won, Lost }

    /// <summary>Owns run phase, the intensity clock, win/loss resolution and
    /// instant retry. All win/loss decisions funnel through here.</summary>
    public class RunManager : MonoBehaviour
    {
        [SerializeField] private EscalationCurve _escalation;
        [SerializeField] private int _requiredSprinklers = 2;
        [SerializeField] private GameEvent _onRunWon;
        [SerializeField] private GameEvent _onRunLost;

        [Header("Wire these scene events to the Notify* handlers via listeners")]
        [SerializeField] private float _retryDelaySeconds = 1.5f;

        public RunPhase Phase { get; private set; } = RunPhase.Playing;

        private float _elapsed;
        private int _sprinklersActive;

        public float Intensity => _escalation != null ? _escalation.Evaluate(_elapsed) : 0f;

        private void Update()
        {
            if (Phase == RunPhase.Playing)
                _elapsed += Time.deltaTime;
        }

        public void NotifySprinklerActivated()
        {
            if (Phase != RunPhase.Playing) return;
            _sprinklersActive++;
            if (_sprinklersActive >= _requiredSprinklers)
                Win();
        }

        public void NotifyFarmerStruck()
        {
            if (Phase != RunPhase.Playing) return;
            Lose();
        }

        public void NotifyFireReachedHouse()
        {
            if (Phase != RunPhase.Playing) return;
            Lose();
        }

        public void Retry()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Win()
        {
            Phase = RunPhase.Won;
            _onRunWon?.Raise();
        }

        private void Lose()
        {
            Phase = RunPhase.Lost;
            _onRunLost?.Raise();
            Invoke(nameof(Retry), _retryDelaySeconds);
        }

        public void ConfigureForTests(EscalationCurve curve, int requiredSprinklers,
            GameEvent won, GameEvent lost)
        {
            _escalation = curve;
            _requiredSprinklers = requiredSprinklers;
            _onRunWon = won;
            _onRunLost = lost;
        }

        public void SetElapsedForTests(float elapsed) => _elapsed = elapsed;
    }
}
```

Note: `Lose()` schedules `Retry` via `Invoke`. In edit-mode tests `Invoke` does not tick (no runtime loop), so `Retry`/scene reload never fires during tests — the phase/event assertions are unaffected.

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests. Expected: all five `RunManagerTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add RunManager phase, intensity clock, win/loss, retry"
```

---

## Task 7: InputReader + FarmerController

**Files:**
- Create: `Assets/Firebreak/Scripts/Player/InputReader.cs`
- Create: `Assets/Firebreak/Scripts/Player/FarmerController.cs`
- Test: `Assets/Firebreak/Tests/PlayMode/FarmerControllerTests.cs`

**Interfaces:**
- Consumes: `GameEvent` (Task 2), the existing `Assets/InputSystem_Actions.inputactions` asset.
- Produces:
  - `Firebreak.InputReader : MonoBehaviour` — `Vector2 MoveInput { get; }`, `bool WideHeld { get; }`; `void SetForTests(Vector2 move, bool wide)` to drive input in play-mode tests.
  - `Firebreak.FarmerController : MonoBehaviour` — `Vector3 Position { get; }`, `bool IsWide { get; }`; raises `_wideEntered` / `_wideExited` on transitions; roots movement while Wide.

- [ ] **Step 1: Write the failing play-mode test**

`Assets/Firebreak/Tests/PlayMode/FarmerControllerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Firebreak.Tests
{
    public class FarmerControllerTests
    {
        private static (FarmerController farmer, InputReader input) MakeFarmer(
            out GameEvent entered, out GameEvent exited)
        {
            entered = ScriptableObject.CreateInstance<GameEvent>();
            exited = ScriptableObject.CreateInstance<GameEvent>();
            var go = new GameObject("Farmer");
            var input = go.AddComponent<InputReader>();
            var farmer = go.AddComponent<FarmerController>();
            farmer.ConfigureForTests(input, entered, exited, moveSpeed: 5f);
            return (farmer, input);
        }

        [UnityTest]
        public IEnumerator MovesInTight_WhenNotWide()
        {
            var (farmer, input) = MakeFarmer(out _, out _);
            input.SetForTests(new Vector2(1f, 0f), wide: false);
            Vector3 start = farmer.Position;

            yield return null; yield return null;

            Assert.Greater((farmer.Position - start).magnitude, 0f);
            Object.Destroy(farmer.gameObject);
        }

        [UnityTest]
        public IEnumerator RootedInWide_DoesNotMove_AndRaisesEnter()
        {
            var (farmer, input) = MakeFarmer(out var entered, out _);
            int enters = 0; entered.Register(() => enters++);
            input.SetForTests(new Vector2(1f, 0f), wide: true);
            yield return null; // process transition
            Vector3 afterEnter = farmer.Position;

            yield return null; yield return null;

            Assert.IsTrue(farmer.IsWide);
            Assert.AreEqual(afterEnter.x, farmer.Position.x, 0.0001f);
            Assert.AreEqual(1, enters);
            Object.Destroy(farmer.gameObject);
        }

        [UnityTest]
        public IEnumerator ReleasingWide_RaisesExit_AndMovesAgain()
        {
            var (farmer, input) = MakeFarmer(out _, out var exited);
            int exits = 0; exited.Register(() => exits++);
            input.SetForTests(Vector2.right, wide: true);
            yield return null;
            input.SetForTests(Vector2.right, wide: false);
            yield return null;
            Vector3 start = farmer.Position;
            yield return null; yield return null;

            Assert.AreEqual(1, exits);
            Assert.Greater((farmer.Position - start).magnitude, 0f);
            Object.Destroy(farmer.gameObject);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run PlayMode tests. Expected: FAIL — `InputReader` / `FarmerController` not defined.

- [ ] **Step 3: Implement InputReader**

`Assets/Firebreak/Scripts/Player/InputReader.cs`:
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

namespace Firebreak
{
    /// <summary>The only script that touches the raw Input System. Exposes the
    /// two game inputs: a move vector and the Wide hold.</summary>
    public class InputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _wide;

        private bool _overridden;
        private Vector2 _testMove;
        private bool _testWide;

        public Vector2 MoveInput =>
            _overridden ? _testMove :
            (_move != null && _move.action != null ? _move.action.ReadValue<Vector2>() : Vector2.zero);

        public bool WideHeld =>
            _overridden ? _testWide :
            (_wide != null && _wide.action != null && _wide.action.IsPressed());

        private void OnEnable()
        {
            _move?.action?.Enable();
            _wide?.action?.Enable();
        }

        private void OnDisable()
        {
            _move?.action?.Disable();
            _wide?.action?.Disable();
        }

        /// <summary>Test-only input override (bypasses the Input System).</summary>
        public void SetForTests(Vector2 move, bool wide)
        {
            _overridden = true;
            _testMove = move;
            _testWide = wide;
        }
    }
}
```

- [ ] **Step 4: Implement FarmerController**

`Assets/Firebreak/Scripts/Player/FarmerController.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    /// <summary>Real-time farmer movement. Fully rooted while Wide is held.
    /// Raises Wide enter/exit transition events.</summary>
    public class FarmerController : MonoBehaviour
    {
        [SerializeField] private InputReader _input;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private GameEvent _wideEntered;
        [SerializeField] private GameEvent _wideExited;

        public Vector3 Position => transform.position;
        public bool IsWide { get; private set; }

        private void Update()
        {
            bool wantWide = _input != null && _input.WideHeld;

            if (wantWide && !IsWide)
            {
                IsWide = true;
                _wideEntered?.Raise();
            }
            else if (!wantWide && IsWide)
            {
                IsWide = false;
                _wideExited?.Raise();
            }

            if (IsWide) return; // rooted: no movement at all while Wide

            Vector2 move = _input != null ? _input.MoveInput : Vector2.zero;
            // Map 2D input to the ground plane (x, z) for the iso view.
            Vector3 delta = new Vector3(move.x, 0f, move.y) * (_moveSpeed * Time.deltaTime);
            transform.position += delta;
        }

        public void ConfigureForTests(InputReader input, GameEvent entered,
            GameEvent exited, float moveSpeed)
        {
            _input = input;
            _wideEntered = entered;
            _wideExited = exited;
            _moveSpeed = moveSpeed;
        }
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run PlayMode tests. Expected: all three `FarmerControllerTests` PASS.

- [ ] **Step 6: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add InputReader and rooted-in-Wide FarmerController"
```

---

## Task 8: Crow + FlockController

**Files:**
- Create: `Assets/Firebreak/Scripts/Flock/Crow.cs`
- Create: `Assets/Firebreak/Scripts/Flock/FlockController.cs`
- Test: `Assets/Firebreak/Tests/PlayMode/FlockControllerTests.cs`

**Interfaces:**
- Consumes: `FarmerController` (Task 7), `PartRegistry`/`Part` (Task 4), `GameEvent` (Task 2).
- Produces:
  - `Firebreak.CrowState { Orbiting, Seeking, Carrying, Dead }`.
  - `Firebreak.Crow : MonoBehaviour` — `CrowState State { get; }`, `bool CarryingPart { get; }`; `void SetOrbit(Vector3 center, Vector3 offset)`, `void AssignSeek(Part part)`, `void Recall(Transform returnTo)`, `void Kill()`. Steers toward its current target each frame.
  - `Firebreak.FlockController : MonoBehaviour` — spawns/owns crows around the farmer; while Wide, assigns unclaimed parts to idle crows; on recall returns crows to the farmer and deposits carried parts back into the farmer's carried tally. `int CarriedParts { get; }`, `int LivingCrows { get; }`; `IReadOnlyList<Crow> Crows { get; }` (used by FireRing); `int TakeCarriedParts()` returns and zeroes the carried tally (used by deposit at sprinkler).

- [ ] **Step 1: Write the failing play-mode test**

`Assets/Firebreak/Tests/PlayMode/FlockControllerTests.cs`:
```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Firebreak.Tests
{
    public class FlockControllerTests
    {
        [UnityTest]
        public IEnumerator Crow_SeekingReachesPart_BecomesCarrying()
        {
            var crow = new GameObject("Crow").AddComponent<Crow>();
            crow.transform.position = Vector3.zero;
            var partGo = new GameObject("Part");
            partGo.transform.position = new Vector3(1f, 0f, 0f);
            var part = partGo.AddComponent<Part>();

            crow.AssignSeek(part);
            Assert.AreEqual(CrowState.Seeking, crow.State);

            // Let it steer to the part.
            float timeout = 3f;
            while (crow.State == CrowState.Seeking && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(CrowState.Carrying, crow.State);
            Assert.IsTrue(crow.CarryingPart);
            Assert.IsTrue(part.Claimed);

            Object.Destroy(crow.gameObject);
            Object.Destroy(partGo);
        }

        [UnityTest]
        public IEnumerator Kill_SetsDeadState()
        {
            var crow = new GameObject("Crow").AddComponent<Crow>();
            crow.Kill();
            yield return null;
            Assert.AreEqual(CrowState.Dead, crow.State);
            Object.Destroy(crow.gameObject);
        }

        [UnityTest]
        public IEnumerator Wide_AssignsPartToCrow_ThenRecallReturnsCarried()
        {
            // Registry + one part
            var registry = new GameObject("Registry").AddComponent<PartRegistry>();
            var partGo = new GameObject("Part");
            partGo.transform.position = new Vector3(2f, 0f, 0f);
            var part = partGo.AddComponent<Part>();
            registry.Add(part);

            // Farmer (Wide driven manually) + flock with a single crow
            var farmerGo = new GameObject("Farmer");
            var input = farmerGo.AddComponent<InputReader>();
            var farmer = farmerGo.AddComponent<FarmerController>();
            farmer.ConfigureForTests(input,
                ScriptableObject.CreateInstance<GameEvent>(),
                ScriptableObject.CreateInstance<GameEvent>(), 5f);

            var flock = farmerGo.AddComponent<FlockController>();
            flock.ConfigureForTests(farmer, registry, crowCount: 1,
                seekRadius: 20f, moveSpeed: 12f);

            input.SetForTests(Vector2.zero, wide: true);

            // Run until the crow carries the part or timeout.
            float timeout = 4f;
            while (flock.CarriedParts == 0 && flock.LivingCrows > 0 && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            // Release Wide → recall
            input.SetForTests(Vector2.zero, wide: false);
            yield return null;

            Assert.GreaterOrEqual(flock.CarriedParts, 1);

            Object.Destroy(farmerGo);
            Object.Destroy(registry.gameObject);
            Object.Destroy(partGo);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run PlayMode tests. Expected: FAIL — `Crow` / `FlockController` / `CrowState` not defined.

- [ ] **Step 3: Implement Crow**

`Assets/Firebreak/Scripts/Flock/Crow.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    public enum CrowState { Orbiting, Seeking, Carrying, Dead }

    /// <summary>One bird. Knows nothing about game rules — it steers toward the
    /// target the FlockController gives it and reports pickups/arrival.</summary>
    public class Crow : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 12f;
        [SerializeField] private float _turnLerp = 6f;
        [SerializeField] private float _arriveDistance = 0.4f;
        [SerializeField] private float _wanderNoise = 0.6f;

        public CrowState State { get; private set; } = CrowState.Orbiting;
        public bool CarryingPart { get; private set; }

        private Vector3 _orbitCenter;
        private Vector3 _orbitOffset;
        private Part _targetPart;
        private Transform _returnTo;
        private Vector3 _velocity;
        private float _noiseSeed;

        private void Awake() => _noiseSeed = Random.value * 100f;

        public void SetOrbit(Vector3 center, Vector3 offset)
        {
            _orbitCenter = center;
            _orbitOffset = offset;
            if (State != CrowState.Dead && State != CrowState.Carrying)
                State = CrowState.Orbiting;
        }

        public void AssignSeek(Part part)
        {
            if (State == CrowState.Dead || part == null) return;
            _targetPart = part;
            part.Claimed = true;
            State = CrowState.Seeking;
        }

        public void Recall(Transform returnTo)
        {
            if (State == CrowState.Dead) return;
            _returnTo = returnTo;
            if (State == CrowState.Seeking)
            {
                // Drop the claim if we hadn't picked it up yet.
                if (_targetPart != null && !CarryingPart) _targetPart.Claimed = false;
                _targetPart = null;
                State = CrowState.Orbiting;
            }
        }

        public void Kill()
        {
            State = CrowState.Dead;
            if (_targetPart != null && !CarryingPart) _targetPart.Claimed = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (State == CrowState.Dead) return;

            Vector3 target = ResolveTarget();
            SteerToward(target);

            if (State == CrowState.Seeking && _targetPart != null &&
                Flat(transform.position, _targetPart.Position) <= _arriveDistance)
            {
                CarryingPart = true;
                _targetPart.Collected = true;
                _targetPart.gameObject.SetActive(false);
                _targetPart = null;
                State = CrowState.Carrying;
            }
        }

        private Vector3 ResolveTarget()
        {
            switch (State)
            {
                case CrowState.Seeking when _targetPart != null:
                    return _targetPart.Position;
                case CrowState.Carrying:
                    return _returnTo != null ? _returnTo.position : _orbitCenter + _orbitOffset;
                default:
                    Vector3 nudge = new Vector3(
                        Mathf.PerlinNoise(_noiseSeed, Time.time) - 0.5f, 0f,
                        Mathf.PerlinNoise(Time.time, _noiseSeed) - 0.5f) * _wanderNoise;
                    return _orbitCenter + _orbitOffset + nudge;
            }
        }

        private void SteerToward(Vector3 target)
        {
            Vector3 to = target - transform.position;
            Vector3 desired = to.sqrMagnitude > 0.0001f
                ? to.normalized * _moveSpeed : Vector3.zero;
            _velocity = Vector3.Lerp(_velocity, desired, _turnLerp * Time.deltaTime);
            transform.position += _velocity * Time.deltaTime;
            if (_velocity.sqrMagnitude > 0.01f)
                transform.forward = _velocity.normalized; // simple banking/facing
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f; return Vector3.Distance(a, b);
        }
    }
}
```

- [ ] **Step 4: Implement FlockController**

`Assets/Firebreak/Scripts/Flock/FlockController.cs`:
```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Firebreak
{
    /// <summary>Owns the flock. In Tight the crows orbit the farmer; in Wide it
    /// assigns nearest unclaimed parts to idle crows and recalls on release.</summary>
    public class FlockController : MonoBehaviour
    {
        [SerializeField] private FarmerController _farmer;
        [SerializeField] private PartRegistry _registry;
        [SerializeField] private Crow _crowPrefab;
        [SerializeField] private int _crowCount = 16;
        [SerializeField] private float _seekRadius = 20f;
        [SerializeField] private float _orbitRadius = 2.5f;
        [SerializeField] private float _crowMoveSpeed = 12f;

        private readonly List<Crow> _crows = new();

        public int CarriedParts { get; private set; }
        public int LivingCrows
        {
            get { int n = 0; foreach (var c in _crows) if (c != null && c.State != CrowState.Dead) n++; return n; }
        }

        private void Start()
        {
            if (_crows.Count == 0) SpawnCrows();
        }

        private void SpawnCrows()
        {
            for (int i = 0; i < _crowCount; i++)
            {
                Crow crow = _crowPrefab != null
                    ? Instantiate(_crowPrefab, OrbitPoint(i), Quaternion.identity, transform)
                    : new GameObject($"Crow_{i}").AddComponent<Crow>();
                if (_crowPrefab == null) crow.transform.SetParent(transform);
                _crows.Add(crow);
            }
        }

        private Vector3 OrbitPoint(int i)
        {
            float ang = (i / Mathf.Max(1f, _crowCount)) * Mathf.PI * 2f;
            return _farmer.Position + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * _orbitRadius;
        }

        private void Update()
        {
            bool wide = _farmer != null && _farmer.IsWide;

            for (int i = 0; i < _crows.Count; i++)
            {
                var crow = _crows[i];
                if (crow == null || crow.State == CrowState.Dead) continue;

                crow.SetOrbit(_farmer.Position, OrbitOffset(i));

                if (wide)
                {
                    if (crow.State == CrowState.Orbiting && _registry != null)
                    {
                        var part = _registry.FindNearestUnclaimed(crow.transform.position, _seekRadius);
                        if (part != null) crow.AssignSeek(part);
                    }
                }
                else
                {
                    crow.Recall(_farmer.transform);
                }

                // A crow that has returned home while carrying deposits into the tally.
                if (!wide && crow.CarryingPart &&
                    Vector3.Distance(crow.transform.position, _farmer.Position) <= _orbitRadius + 0.5f)
                {
                    CarriedParts++;
                    ClearCarry(crow);
                }
            }
        }

        private static void ClearCarry(Crow crow)
        {
            // Reflectionless carry-clear: mark it orbiting with no part.
            crow.SetOrbit(crow.transform.position, Vector3.zero);
            crow.SendMessage("OnDeposited", SendMessageOptions.DontRequireReceiver);
            crow.ForceDropCarry();
        }

        private Vector3 OrbitOffset(int i)
        {
            float ang = (i / Mathf.Max(1f, (float)_crows.Count)) * Mathf.PI * 2f;
            return new Vector3(Mathf.Cos(ang), 0.3f, Mathf.Sin(ang)) * _orbitRadius;
        }

        /// <summary>Called by the deposit flow when the farmer feeds a sprinkler.</summary>
        public int TakeCarriedParts()
        {
            int n = CarriedParts;
            CarriedParts = 0;
            return n;
        }

        /// <summary>Kills any living crow currently outside a safe area (called by FireRing).</summary>
        public IReadOnlyList<Crow> Crows => _crows;

        public void ConfigureForTests(FarmerController farmer, PartRegistry registry,
            int crowCount, float seekRadius, float moveSpeed)
        {
            _farmer = farmer;
            _registry = registry;
            _crowCount = crowCount;
            _seekRadius = seekRadius;
            _crowMoveSpeed = moveSpeed;
            _crowPrefab = null;
            SpawnCrows();
        }
    }
}
```

Add a `ForceDropCarry()` method to `Crow` (used by the deposit flow):
```csharp
        /// <summary>Clears the carried-part flag after depositing at the farmer.</summary>
        public void ForceDropCarry()
        {
            CarryingPart = false;
            if (State == CrowState.Carrying) State = CrowState.Orbiting;
        }
```
(Insert this inside the `Crow` class from Step 3. Remove the `SendMessage("OnDeposited", …)` line from `ClearCarry` if you prefer — it is a no-op hook for future FX and requires no receiver.)

- [ ] **Step 5: Run tests to verify they pass**

Run PlayMode tests. Expected: all three `FlockControllerTests` PASS. If `Crow_SeekingReachesPart` is flaky on timing, raise the crow `_moveSpeed`/`_turnLerp` or the test timeout — do not weaken the assertion.

- [ ] **Step 6: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add Crow steering state machine and FlockController"
```

---

## Task 9: FireRing

**Files:**
- Create: `Assets/Firebreak/Scripts/Hazards/FireRing.cs`
- Test: `Assets/Firebreak/Tests/EditMode/FireRingTests.cs`

**Interfaces:**
- Consumes: `RunManager` (Task 6, for `Intensity`), `FlockController`/`Crow` (Task 8), `GameEvent` (Task 2).
- Produces: `Firebreak.FireRing : MonoBehaviour` —
  - `float CurrentSafeRadius { get; }` (shrinks as intensity rises, from `_startRadius` down to `_houseRadius`).
  - `bool IsCaught(Vector3 worldPos)` → true when the position is outside the current unburned interior.
  - each frame while the run is Playing and the flock is Wide, kills living crows for which `IsCaught` is true (raises `_crowDied` per kill); raises `_fireReachedHouse` once when the safe radius reaches the house.
  - `void ConfigureForTests(float startRadius, float houseRadius)` and `void SetIntensityForTests(float)`.

- [ ] **Step 1: Write the failing test**

`Assets/Firebreak/Tests/EditMode/FireRingTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class FireRingTests
    {
        private FireRing MakeRing(float start, float house)
        {
            var r = new GameObject("Fire").AddComponent<FireRing>();
            r.transform.position = Vector3.zero; // map center
            r.ConfigureForTests(start, house);
            return r;
        }

        [Test]
        public void SafeRadius_ShrinksFromStartToHouse_AsIntensityRises()
        {
            var r = MakeRing(50f, 5f);
            r.SetIntensityForTests(0f);
            Assert.AreEqual(50f, r.CurrentSafeRadius, 0.01f);
            r.SetIntensityForTests(1f);
            Assert.AreEqual(5f, r.CurrentSafeRadius, 0.01f);
        }

        [Test]
        public void IsCaught_TrueOutsideSafeRadius()
        {
            var r = MakeRing(50f, 5f);
            r.SetIntensityForTests(0.5f); // radius = 27.5
            Assert.IsFalse(r.IsCaught(new Vector3(10f, 0f, 0f)));
            Assert.IsTrue(r.IsCaught(new Vector3(40f, 0f, 0f)));
        }

        [Test]
        public void IsCaught_IgnoresHeight()
        {
            var r = MakeRing(50f, 5f);
            r.SetIntensityForTests(0f); // radius 50
            Assert.IsFalse(r.IsCaught(new Vector3(3f, 20f, 3f)));
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests. Expected: FAIL — `FireRing` not defined.

- [ ] **Step 3: Implement FireRing**

`Assets/Firebreak/Scripts/Hazards/FireRing.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    /// <summary>The advancing fire. Modeled as an unburned interior circle
    /// centered on the map that shrinks as intensity rises. A crow is "caught"
    /// when it is outside that interior while the flock is Wide.</summary>
    public class FireRing : MonoBehaviour
    {
        [SerializeField] private RunManager _run;
        [SerializeField] private FarmerController _farmer;
        [SerializeField] private FlockController _flock;
        [SerializeField] private GameEvent _crowDied;
        [SerializeField] private GameEvent _fireReachedHouse;

        [SerializeField] private float _startRadius = 50f;
        [SerializeField] private float _houseRadius = 5f;

        private float _testIntensity = -1f;
        private bool _houseReached;

        private float Intensity =>
            _testIntensity >= 0f ? _testIntensity : (_run != null ? _run.Intensity : 0f);

        public float CurrentSafeRadius =>
            Mathf.Lerp(_startRadius, _houseRadius, Mathf.Clamp01(Intensity));

        public bool IsCaught(Vector3 worldPos)
        {
            Vector3 c = transform.position; c.y = 0f;
            worldPos.y = 0f;
            return Vector3.Distance(worldPos, c) > CurrentSafeRadius;
        }

        private void Update()
        {
            if (_run == null || _run.Phase != RunPhase.Playing) return;

            if (!_houseReached && CurrentSafeRadius <= _houseRadius + 0.001f)
            {
                _houseReached = true;
                _fireReachedHouse?.Raise();
                return;
            }

            if (_farmer != null && _farmer.IsWide && _flock != null)
            {
                foreach (var crow in _flock.Crows)
                {
                    if (crow == null || crow.State == CrowState.Dead) continue;
                    if (IsCaught(crow.transform.position))
                    {
                        crow.Kill();
                        _crowDied?.Raise();
                    }
                }
            }
        }

        public void ConfigureForTests(float startRadius, float houseRadius)
        {
            _startRadius = startRadius;
            _houseRadius = houseRadius;
        }

        public void SetIntensityForTests(float intensity) => _testIntensity = intensity;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests. Expected: all three `FireRingTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add FireRing shrinking-interior crow catch"
```

---

## Task 10: BranchHazard

**Files:**
- Create: `Assets/Firebreak/Scripts/Hazards/BranchHazard.cs`
- Test: `Assets/Firebreak/Tests/EditMode/BranchHazardTests.cs`

**Interfaces:**
- Consumes: `RunManager` (Task 6, `Intensity`), `FarmerController` (Task 7), `GameEvent` (Task 2).
- Produces: `Firebreak.BranchHazard : MonoBehaviour` —
  - pure helpers: `static bool IsHit(Vector3 strikePos, Vector3 farmerPos, float strikeRadius)`; `float TelegraphInterval(float intensity)` mapping intensity→seconds (higher intensity = shorter interval, clamped to `[_minInterval,_maxInterval]`).
  - runtime: while farmer is rooted in Wide past `_gracePeriod`, spawns telegraphs at `TelegraphInterval`; after `_telegraphDelay`, resolves a strike; if it hits the farmer, raises `_farmerStruck`. Resets on Wide exit.
  - `void ConfigureForTests(float grace, float minInterval, float maxInterval, float strikeRadius)`.

- [ ] **Step 1: Write the failing test**

`Assets/Firebreak/Tests/EditMode/BranchHazardTests.cs`:
```csharp
using NUnit.Framework;
using UnityEngine;

namespace Firebreak.Tests
{
    public class BranchHazardTests
    {
        private BranchHazard MakeHazard()
        {
            var h = new GameObject("Branches").AddComponent<BranchHazard>();
            h.ConfigureForTests(grace: 1.5f, minInterval: 0.4f, maxInterval: 2.5f, strikeRadius: 1.2f);
            return h;
        }

        [Test]
        public void IsHit_TrueWithinStrikeRadius()
        {
            Assert.IsTrue(BranchHazard.IsHit(new Vector3(0.5f, 0, 0), Vector3.zero, 1.2f));
            Assert.IsFalse(BranchHazard.IsHit(new Vector3(3f, 0, 0), Vector3.zero, 1.2f));
        }

        [Test]
        public void TelegraphInterval_ShorterAtHigherIntensity()
        {
            var h = MakeHazard();
            float low = h.TelegraphInterval(0f);
            float high = h.TelegraphInterval(1f);
            Assert.Greater(low, high);
        }

        [Test]
        public void TelegraphInterval_ClampedToRange()
        {
            var h = MakeHazard();
            Assert.AreEqual(2.5f, h.TelegraphInterval(0f), 0.001f);
            Assert.AreEqual(0.4f, h.TelegraphInterval(1f), 0.001f);
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run EditMode tests. Expected: FAIL — `BranchHazard` not defined.

- [ ] **Step 3: Implement BranchHazard**

`Assets/Firebreak/Scripts/Hazards/BranchHazard.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    /// <summary>Falling-branch danger. Active only while the farmer is rooted in
    /// Wide past the grace period. Telegraph → strike around the farmer's fixed
    /// position; a hit is an instant fail. Frequency scales with intensity.</summary>
    public class BranchHazard : MonoBehaviour
    {
        [SerializeField] private RunManager _run;
        [SerializeField] private FarmerController _farmer;
        [SerializeField] private GameEvent _farmerStruck;
        [SerializeField] private GameObject _telegraphPrefab; // optional visual

        [SerializeField] private float _gracePeriod = 1.5f;
        [SerializeField] private float _minInterval = 0.4f;   // at intensity 1
        [SerializeField] private float _maxInterval = 2.5f;   // at intensity 0
        [SerializeField] private float _telegraphDelay = 0.8f;
        [SerializeField] private float _spawnRadius = 3.5f;
        [SerializeField] private float _strikeRadius = 1.2f;

        private float _heldTime;
        private float _nextTelegraphIn;
        private bool _strikePending;
        private float _strikeIn;
        private Vector3 _pendingStrikePos;
        private GameObject _activeTelegraph;

        public static bool IsHit(Vector3 strikePos, Vector3 farmerPos, float strikeRadius)
        {
            strikePos.y = 0f; farmerPos.y = 0f;
            return Vector3.Distance(strikePos, farmerPos) <= strikeRadius;
        }

        public float TelegraphInterval(float intensity)
        {
            return Mathf.Lerp(_maxInterval, _minInterval, Mathf.Clamp01(intensity));
        }

        private void Update()
        {
            if (_run == null || _run.Phase != RunPhase.Playing) return;

            bool rooted = _farmer != null && _farmer.IsWide;
            if (!rooted)
            {
                ResetState();
                return;
            }

            _heldTime += Time.deltaTime;
            if (_heldTime < _gracePeriod) return; // short commands are always safe

            // Resolve a pending strike.
            if (_strikePending)
            {
                _strikeIn -= Time.deltaTime;
                if (_strikeIn <= 0f)
                {
                    _strikePending = false;
                    if (_activeTelegraph != null) Destroy(_activeTelegraph);
                    if (IsHit(_pendingStrikePos, _farmer.Position, _strikeRadius))
                        _farmerStruck?.Raise();
                }
                return;
            }

            // Schedule the next telegraph.
            _nextTelegraphIn -= Time.deltaTime;
            if (_nextTelegraphIn <= 0f)
            {
                Vector2 r = Random.insideUnitCircle * _spawnRadius;
                _pendingStrikePos = _farmer.Position + new Vector3(r.x, 0f, r.y);
                _strikePending = true;
                _strikeIn = _telegraphDelay;
                _nextTelegraphIn = TelegraphInterval(_run.Intensity);
                if (_telegraphPrefab != null)
                    _activeTelegraph = Instantiate(_telegraphPrefab, _pendingStrikePos, Quaternion.identity);
            }
        }

        private void ResetState()
        {
            _heldTime = 0f;
            _nextTelegraphIn = 0f;
            _strikePending = false;
            if (_activeTelegraph != null) Destroy(_activeTelegraph);
        }

        public void ConfigureForTests(float grace, float minInterval, float maxInterval, float strikeRadius)
        {
            _gracePeriod = grace;
            _minInterval = minInterval;
            _maxInterval = maxInterval;
            _strikeRadius = strikeRadius;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run EditMode tests. Expected: all three `BranchHazardTests` PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Firebreak
git commit -m "feat: add BranchHazard telegraph-strike while rooted"
```

---

## Task 11: Greybox scene assembly, wiring & deposit loop

This task has few unit tests — it wires the pieces into a playable scene and
verifies the whole loop by hand. It also adds the small **deposit trigger** that
closes the loop (farmer walks onto a sprinkler → carried parts feed it).

**Files:**
- Create: `Assets/Firebreak/Scripts/World/SprinklerDeposit.cs`
- Create: `Assets/Firebreak/Scenes/Greybox.unity`
- Create SO assets: `Assets/Firebreak/Events/WideEntered.asset`, `WideExited.asset`, `CrowDied.asset`, `SprinklerActivated.asset`, `FarmerStruck.asset`, `FireReachedHouse.asset`, `RunWon.asset`, `RunLost.asset` (all `GameEvent`), and `Assets/Firebreak/Settings/MainEscalation.asset` (`EscalationCurve`).
- Modify: `ProjectSettings/EditorBuildSettings` (add the Greybox scene) — via the editor.

**Interfaces:**
- Consumes: everything above.
- Produces: `Firebreak.SprinklerDeposit : MonoBehaviour` — on a physics trigger with the farmer, if the farmer's flock has carried parts, feeds them to the co-located `Sprinkler` via `Sprinkler.Deposit(flock.TakeCarriedParts())`.

- [ ] **Step 1: Implement SprinklerDeposit**

`Assets/Firebreak/Scripts/World/SprinklerDeposit.cs`:
```csharp
using UnityEngine;

namespace Firebreak
{
    /// <summary>Trigger volume on a sprinkler. When the farmer enters carrying
    /// parts (via the flock), deposits them into the sprinkler.</summary>
    [RequireComponent(typeof(Sprinkler))]
    public class SprinklerDeposit : MonoBehaviour
    {
        [SerializeField] private FlockController _flock;
        private Sprinkler _sprinkler;

        private void Awake() => _sprinkler = GetComponent<Sprinkler>();

        private void OnTriggerEnter(Collider other)
        {
            TryDeposit(other.GetComponentInParent<FarmerController>());
        }

        private void OnTriggerStay(Collider other)
        {
            TryDeposit(other.GetComponentInParent<FarmerController>());
        }

        private void TryDeposit(FarmerController farmer)
        {
            if (farmer == null || _flock == null || _sprinkler.IsActive) return;
            if (_flock.CarriedParts <= 0) return;
            _sprinkler.Deposit(_flock.TakeCarriedParts());
        }
    }
}
```

- [ ] **Step 2: Commit the deposit script**

```bash
git add Assets/Firebreak/Scripts/World/SprinklerDeposit.cs
git commit -m "feat: add SprinklerDeposit trigger to close the deposit loop"
```

- [ ] **Step 3: Create the ScriptableObject assets (in Unity)**

In the Project window: right-click → Create → Firebreak → Game Event, once for each of the eight events, named exactly `WideEntered`, `WideExited`, `CrowDied`, `SprinklerActivated`, `FarmerStruck`, `FireReachedHouse`, `RunWon`, `RunLost`, saved under `Assets/Firebreak/Events/`.
Create → Firebreak → Escalation Curve → `Assets/Firebreak/Settings/MainEscalation.asset`. Set `Run Length` to `120` (compressed greybox timeline) and shape the curve ease-in so intensity ramps slowly then accelerates.

- [ ] **Step 4: Build the Greybox scene**

Create `Assets/Firebreak/Scenes/Greybox.unity` with:
- **Camera:** Projection = Orthographic; rotation `(30, 45, 0)`; size ~18; positioned back so the whole ~50-unit play area is visible. Tag MainCamera.
- **Directional light** (default is fine).
- **Ground:** a Plane scaled to cover ~50×50 units at origin.
- **House:** a cube near center marking the farmhouse; place the `FireRing` component on an empty at world origin (map center) with `_startRadius=50`, `_houseRadius=5`.
- **Farmer:** capsule at origin-ish. Add `InputReader` (assign Move/Wide `InputActionReference`s from `Assets/InputSystem_Actions.inputactions` — Move = the Move action, Wide = a hold action e.g. Attack/Interact; if none suits, add a `Wide` action bound to Space and Gamepad Right Trigger), `FarmerController` (assign `_input`, `_wideEntered=WideEntered`, `_wideExited=WideExited`), and a `FlockController` (assign `_farmer`, `_registry`, a simple sphere `Crow` prefab or leave prefab empty to auto-generate primitives, `_crowCount=16`). Add a `CharacterController` or leave transform-based movement.
- **Crow prefab:** small sphere with the `Crow` component, saved under `Assets/Firebreak/` and assigned to `FlockController._crowPrefab` (optional; without it the flock spawns primitive-less empties — add a small sphere child for visibility).
- **PartRegistry:** empty GameObject with `PartRegistry`. Scatter ~10 `Part` objects (small cubes): several near the edge (~radius 20–24, fire-risky), several interior, and **one at exact map center**. Each Part is a small cube with the `Part` component; give it a collider only if needed for visuals.
- **Sprinklers (2):** cubes near the house perimeter, each with `Sprinkler` (`_requiredParts=2`, `_onActivated=SprinklerActivated`), a trigger `Collider` (isTrigger), and `SprinklerDeposit` (assign `_flock`).
- **RunManager:** empty GameObject with `RunManager` (`_escalation=MainEscalation`, `_requiredSprinklers=2`, `_onRunWon=RunWon`, `_onRunLost=RunLost`).
- **FireRing** (on the map-center empty): assign `_run`, `_farmer`, `_flock`, `_crowDied=CrowDied`, `_fireReachedHouse=FireReachedHouse`.
- **BranchHazard:** empty GameObject with `BranchHazard`; assign `_run`, `_farmer`, `_farmerStruck=FarmerStruck`. Optional: a flat quad telegraph prefab.
- **Event wiring:** add `GameEventListener` components (or a small dispatcher) so:
  - `SprinklerActivated` → `RunManager.NotifySprinklerActivated`
  - `FarmerStruck` → `RunManager.NotifyFarmerStruck`
  - `FireReachedHouse` → `RunManager.NotifyFireReachedHouse`
  - `RunLost`/`RunWon` → a placeholder UI text toggle (a world-space Canvas showing "WON"/"LOST"). Retry is automatic via `RunManager.Lose()`'s scheduled `Retry`; on win, optionally also call `Retry` from a listener or leave the win screen up.
- Add the scene to Build Settings (File → Build Settings → Add Open Scenes).

- [ ] **Step 5: Manual verification — the full loop**

Enter Play mode and confirm, checking each against the spec's success criteria:
1. In Tight, farmer moves (WASD/left-stick); crows orbit and do **not** collect.
2. Hold Wide: farmer is fully rooted; crows fan out and seek parts; on release they return and the carried tally rises.
3. Walk onto a sprinkler trigger with carried parts → sprinkler activates (its `SprinklerActivated` fires).
4. Activating both sprinklers → `RunWon` (WON shown).
5. Send crows toward an edge part while Wide as intensity rises → a caught crow dies (`CrowDied`) and `LivingCrows` drops.
6. Hold Wide near map center well past the grace period → branch telegraphs appear and eventually a strike hits → `RunLost` → auto-retry reloads the scene.
7. Let the fire reach the house (idle) → `RunLost` → auto-retry.

Fix wiring/tuning until all seven behaviors hold. Tune radii/intervals/curve in the Inspector — these are expected to change (spec §8).

- [ ] **Step 6: Commit the scene and assets**

```bash
git add Assets/Firebreak ProjectSettings/EditorBuildSettings.asset
git commit -m "feat: assemble Greybox scene, event assets, and deposit loop"
```

---

## Self-Review Notes (for the implementer)

- **Spec coverage:** camera/farmer/input (T7,T11), crows+steering (T8), parts incl. center (T4,T11), sprinklers+deposit (T5,T11), fire ring crow-kill (T9), branch telegraph/grace/strike (T10), single-source escalation (T3,T6), win/loss+retry (T6,T11), event channels (T2), tests per spec §7 (T2–T10). All spec §3 in-scope items map to a task. Out-of-scope items (art, terrain, boids, wave curve, hard-fail-on-zero-crows) are intentionally absent.
- **Deferred by design:** typed/payload events (parameterless suffices for count-based win), boids, wave-by-wave pacing (single compressed curve instead).
- **Known tuning risks:** crow steering timing in T8 play-mode tests; if flaky, adjust speeds/timeouts, never the assertions. Branch/fire feel is Inspector-tuned in T11.
