# Project Context

- **Owner:** Stuart Hillary
- **Project:** Sage® Simulation and Modeling Libraries — a long-running discrete event simulation (DES) library originally built on early .NET Framework, now targeting .NET 8.
- **Stack:** C#, .NET 8, NUnit/xUnit, GitHub Actions, NuGet
- **Key modules:** Core (event engine), Scheduling, Graphs, Mathematics, SystemDynamics, ItemBased, Randoms, Persistence, Presentation, SmartPropertyBag, Utility
- **Goals:** Continued development, .NET 8 modernization, performance improvement, future visualization layer
- **PM:** Stuart Hillary (human — sets priorities and direction)
- **Created:** 2026-03-05

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-15 — Full Architectural Assessment

**Solution structure:**
- Single monolithic assembly `Sage.dll` from `Sage/Sage4.csproj` targeting .NET 8
- 548 .cs source files across 16 module directories
- ~304 MSTest tests in `Sage_Aux/SageTestLib/`
- Builds clean (0 errors, ~3,746 warnings)

**Core engine architecture:**
- Two `IExecutive` implementations: `Executive` (full-featured, ~1,150 lines) and `ExecutiveFastLight` (single-threaded heap-based, ~900 lines)
- Both are `internal sealed` classes, instantiated via `ExecFactory` singleton using reflection
- `Executive` uses `SortedList` (non-generic) as event queue — O(n) insert/dequeue. This is the #1 performance bottleneck
- `ExecutiveFastLight` uses a proper binary min-heap — O(log n) — but lacks priority, rescission, detachable events
- Event dispatch: delegate-based `ExecEventReceiver(IExecutive exec, object userData)` — untyped userData everywhere
- Three event types: Synchronous (inline), Detachable (coroutine-like via Task.Run + ManualResetEventSlim), Asynchronous (fire-and-forget)
- `DetachableEvent` in `Sage/Core/DetachableEvent.cs` — suspend/resume semantics via thread signaling

**Key files:**
- `Sage/Core/IExecutive.cs` — primary interface
- `Sage/Core/Executive.cs` — full executive (SortedList-based)
- `Sage/Core/ExecutiveFastLight.cs` — fast executive (heap-based)
- `Sage/Core/ExecEvent.cs` — event record with disabled object pool
- `Sage/Core/ExecFactory.cs` — singleton factory with reflection-based construction
- `Sage/Core/ExecController.cs` — rate throttling + render frame dispatch (visualization support)
- `Sage/Core/Model.cs` — simulation container, owns executive + state machine + services
- `Sage/Core/StateMachine.cs` — two-phase-commit state machine
- `Sage/Core/DetachableEvent.cs` — coroutine implementation

**Legacy patterns identified:**
- 304 ArrayList, 265 Hashtable, 31 non-generic SortedList uses
- 250 ApplicationException throws
- No nullable reference types enabled
- 89 public delegate declarations (vs modern EventHandler<T>/Action<T>)
- Configuration via System.Configuration.ConfigurationManager (app.config XML)
- MarshalByRefObject on Executive class
- Mix of `_camelCase` and `m_camelCase` field naming
- ExecEvent object pool exists but is disabled (`_usePool = false`)

**Visualization readiness:**
- `ExecController` already supports frame rate + time scaling + Render event
- `EventAboutToFire` / `EventHasCompleted` monitors exist
- Missing: structured event stream, snapshot mechanism, change notifications, spatial model

**Modernization priorities (ordered):**
1. Replace SortedList with priority queue in Executive
2. Enable nullable reference types
3. Replace non-generic collections with generic equivalents
4. Modernize configuration (remove ConfigurationManager)
5. Type-safe event data (replace `object userData`)
6. Exception hierarchy cleanup (replace ApplicationException)
7. Observable event stream for visualization
8. Update test infrastructure (MSTest 3.x, SDK 17.x)
9. Project splitting (monolith → focused packages)
10. Modern C# idioms (file-scoped namespaces, records, patterns)

### 2026-03-06 — TupleSpace .NET 10 Root Cause Identified

Parker's investigation **confirmed the root cause** of 7 failing TupleTester tests on `feature/dotnet10`:

**Root Cause:** The DetachableEvent pattern (ManualResetEventSlim blocking while waiting for thread pool tasks) triggers .NET 10's more aggressive thread pool starvation detection. Tasks start but don't complete, simulation exits prematurely, hardcoded test expectations fail.

**Three attempted fixes unsuccessful** — requires architectural guidance.

**Escalation:** Thread pool interaction pattern needs team decision:
1. **Option 1:** Refactor to async/await (MAJOR - affects entire simulation engine)
2. **Option 2:** Dedicated threads instead of thread pool (MEDIUM - higher overhead)
3. **Option 3:** Synchronization tracing for deadlock confirmation (TARGETED)
4. **Option 4:** Wait for .NET 10 RTM or file bug with Microsoft

**Decision Owner:** Ripley (Lead/Architect) — Define threading strategy before proceeding.

**Branch Status:** `feature/dotnet10` has debug artifacts; do NOT merge until resolved.

**Files Involved:**
- Sage/Core/DetachableEvent.cs
- Sage/Core/Executive.cs  
- Sage/Utility/TupleSpace.cs
- Sage_Aux/SageTestLib/TestTuples.cs
