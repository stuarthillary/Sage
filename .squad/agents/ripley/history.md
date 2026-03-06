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

### 2026-03-06 — TupleSpace Failures Resolved ✅

**Status:** Fixed and merge-ready  
**Tests:** 304/304 passing  
**Branch:** `feature/dotnet10`  
**Commit:** `5276d47`

**Root Cause:** Pre-existing race condition in `Exchange.NonBlockingPost()` (not thread pool starvation). Generic `HashtableOfLists<TKey,TValue>` indexer throws `KeyNotFoundException` if key doesn't exist. .NET 10's different thread pool timing exposed this timing-dependent bug.

**Fix Applied:** Added `ContainsKey()` checks in `Exchange.NonBlockingPost()` before accessing `_waitersToRead` and `_waitersToTake` dictionaries.

**Option 2 (Explicit Thread) Evaluation:** Tested but NOT RECOMMENDED—fixes TupleSpace but breaks ResourceManager tests. Exchange.cs fix alone is sufficient and surgical.

**Files Modified:** Only `Sage/Utility/Exchange.cs` (2 guard checks)

**Recommendation:** ✅ Merge `feature/dotnet10` to main immediately. No architectural changes needed.

### 2026-03-06 — Collection Modernization Strategy (COMPLETE ✅)

**Status:** Strategy merged to decisions.md

**Deliverable:** `.squad/decisions/decisions.md` → "Decision: Non-Generic Collection Modernization — Three-Phase Strategy" (deduplicated, comprehensive)

**Key decision captured:**
- 3-phase risk tiering: Phase 1 (60%, internal, non-breaking, low risk), Phase 2 (30%, public API, breaking, medium risk), Phase 3 (10%, intentional, never replace)
- **Critical exclusions locked:** `object userData` (intentional heterogeneous payloads), `IDictionary graphContext` (intentional polymorphic execution context — 50+ signatures), XmlSerializationContext (serialization contract), DynamicConstruction (WIP code)
- **Phase 1 greenlit for immediate start:** Private fields/local variables only, zero public API changes, all 310 tests as validation gate
- **Collection mapping reference** provided (ArrayList→List, Hashtable→Dictionary, etc.)
- **Risk mitigations** documented (thread safety, ordering differences, casting differences)
- **Success criteria** established for each phase

**Architectural insight:** Not all non-generic collections are technical debt. `object userData` enables heterogeneous event payloads across any simulation model. `IDictionary graphContext` provides runtime flexibility for graph execution contexts (analogous to ASP.NET ViewData). These are intentional design patterns, not modernization targets.

**Parker's detailed inventory** (.squad/decisions/inbox/parker-collection-inventory.md) provides file-by-file implementation guide with difficulty tiers.
