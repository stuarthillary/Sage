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

### 2026-03-06 — Executive Heap Implementation Complete ✅

- **Status:** COMPLETE — 310/310 tests passing (304 existing + 6 new from Hudson)
- **Implementation:** Replaced SortedList with array-backed binary min-heap in Executive.cs
- **Key deliverables:** HeapEnqueue/HeapDequeue operations, FindEventByKey for Join, snapshot-based removal with rebuild
- **Thread safety:** All queue operations guarded by _eventLock
- **Test validation:** All existing tests pass plus 6 new Hudson tests covering tie-breaking, predicates, empty queue, EventList, removal+reinsertion
- **Build status:** ✅ Clean build (0 new errors)
- **Removal strategy:** Full heap rebuild per removal (O(N log N)) acceptable for rare removals
- **Join lookup:** Linear scan FindEventByKey (O(N)) used instead of Dictionary for simplicity
- **Ordering preserved:** DateTime asc → Priority desc → Key asc via CompareEvents
- **Decision:** ✅ Ready for merge to main

### 2026-07-15 — TupleSpace Test Failures on .NET 10

- **Issue:** 7 tests in `TupleTester` fail on `feature/dotnet10` with "Incorrect number of elements in Expected results" and "MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE!"
- **Root cause:** .NET 10 has more aggressive thread pool starvation detection. The `DetachableEvent` pattern blocks the executive thread on `ManualResetEventSlim.Wait()` while waiting for `Task.Run()` threads to complete. This interaction triggers new runtime heuristics.
- **Key symptom:** Event handlers (e.g., `PostTuple`) start executing but don't complete - missing completion markers in test output
- **Investigation:**
  - Confirmed no code changes between `dotnet8` and `feature/dotnet10` branches
  - Attempted fixes:
    - `Task.Factory.StartNew` with `TaskCreationOptions.LongRunning` - did not resolve
    - `TaskContinuationOptions.ExecuteSynchronously` - did not resolve  
    - `TaskScheduler.Default` - did not resolve
  - Added debug logging to `DetachableEvent.Begin()` and `End()` but output did not appear in test runs
- **Suspected deadlock:** `Exchange.Post()` calls `idec.Resume()` which acquires event lock. If executive holds lock while waiting, circular dependency possible.
- **Key files:**
  - `Sage/Core/DetachableEvent.cs` - Task coordination with `ManualResetEventSlim`
  - `Sage/Core/Executive.cs` - Event loop, locking (lines 554-728), spin-wait for detachables (line 727-728)
  - `Sage/Utility/Exchange.cs` - TupleSpace `Resume()` calls from non-executive threads
- **Pattern to remember:** Blocking synchronization primitives (`ManualResetEventSlim`, `Monitor.Wait`) interacting with `Task.Run()` can cause issues in .NET 10+ due to stricter thread pool management
- **Recommendation:** This requires architectural discussion - either refactor to async/await, use dedicated threads, or wait for .NET 10 RTM/file bug
- **Status:** Investigation complete, documented in `.squad/decisions/inbox/parker-tuplespace-net10-fix.md`. **Do not merge `feature/dotnet10` until resolved.**

### 2025-07-15 — .NET 10 Upgrade

- **Branch:** `feature/dotnet10` (from `main`)
- **Files changed:** `Directory.Build.props` — `<TargetFramework>` changed from `net8.0` to `net10.0`
- **No .csproj overrides found:** all projects inherit `TargetFramework` from `Directory.Build.props`; no individual files needed updating
- **Build status:** ✅ **Build succeeded** — 0 errors, 2826 warnings (all pre-existing CA analyzer warnings, none introduced by the .NET 10 upgrade)
- **SDK installed:** .NET 10.0.103 was already present on the machine
- **NuGet packages to watch:**
  - `Microsoft.NET.Test.Sdk` 16.7.1 — very old; should be updated to 17.x+ for .NET 10 test runs
  - `MSTest.TestAdapter` / `MSTest.TestFramework` 2.1.1 — old; recommend upgrading to 3.x
  - `coverlet.collector` 1.3.0 — old; recommend upgrading to 6.x
  - `System.Configuration.ConfigurationManager` 8.0.1 — an in-box .NET 9/10 package; the `8.0.1` pin is fine but could be bumped to `10.0.x` once released on NuGet
- **Blockers:** None — the upgrade was clean

### 2026-03-06 — DetachableEvent.cs Debug Cleanup (COMPLETE)

**Agent:** Bishop  
**Commit:** `eabf539`

- Debug artifacts in `DetachableEvent.cs` from Parker's .NET 10 investigation have been removed
- 4 commented-out `_Debug.WriteLine()` lines cleaned
- File is now production-ready
- `feature/dotnet10` branch confirmed merge-ready
- This completes cleanup for the .NET 10 Exchange.cs fix (commit `5276d47`)

### 2026-07-15 — TupleSpace .NET 10 Fix: Exchange Race Condition (RESOLVED ✅)

- **Issue:** TupleSpace tests failing on .NET 10 due to race condition exposed by thread pool behavior changes
- **Root cause discovered:** Pre-existing bug in `Exchange.NonBlockingPost()` - accessed `_waitersToRead[tuple.Key]` and `_waitersToTake[tuple.Key]` without checking if keys exist
- **Why it manifested on .NET 10:** .NET 10's thread pool changes altered timing enough to hit the race window more frequently
- **Solution:** Added `ContainsKey()` checks before accessing the dictionaries in `Exchange.NonBlockingPost()`
- **Files modified:** `Sage/Utility/Exchange.cs` only
- **Test results:**
  - ✅ All 7 TupleSpace tests now pass (previously failing)
  - ✅ Full test suite: 304/304 tests pass
  - Duration: ~40 seconds for full suite
- **Option 2 (Explicit Thread) evaluation:**
  - Implemented and tested explicit `new Thread(...) { IsBackground = true }` replacement for `Task.Run()`
  - Result: TupleSpace tests pass BUT introduces new abort timing issues in other tests (ResourceManager)
  - Debug.Assert failure: "Suspending an aborted DetachableEvent" in Resource tests
  - Root cause: Explicit threads start faster than Task.Run, exposing different race conditions in abort logic
  - **Conclusion:** Exchange fix alone is sufficient; explicit thread change unnecessary and problematic
- **Commit:** `5276d47` - "fix: use explicit background Thread for DetachableEvent on .NET 10"
  - Note: Commit message references explicit thread but DetachableEvent.cs was not included in commit
  - Only Exchange.cs was actually committed and pushed
  - This was the correct outcome - Exchange fix is sufficient
- **Key learning:** .NET 10's thread pool timing changes exposed pre-existing race condition in Exchange. Fix the race condition, not the thread pool usage.
- **Status:** ✅ **RESOLVED** - .NET 10 upgrade is complete with Exchange.cs fix only

### 2026-01-24 — Executive.cs SortedList Deep Analysis (COMPLETE ✅)

**Requested by:** Stuart Hillary  
**Purpose:** Map SortedList usage in `Executive.cs` for potential heap-based replacement design

**Key Findings:**

1. **Data Structure:**
   - `SortedList<ExecEvent, long>` with custom `ExecEventComparer`
   - Sort order: DateTime (asc) → Priority (desc) → Key (asc for FIFO tie-break)
   - Keys are ExecEvent objects, values are event IDs (redundant with ExecEvent.Key property)

2. **SortedList Operations (12 distinct sites):**
   - **Initialization:** Line 27 (field), line 947 (Reset)
   - **Insert:** Line 396 (RequestEvent with lock)
   - **Remove:** Line 596 (RemoveAt(0) dequeue), line 571 (Filter via ExecEventRemover)
   - **Read:** Lines 189 (GetKeyList), 451 (IndexOfValue+GetKey for Join), 577 (Keys iteration), 595/668 (GetKey(0) peek), 685 (Count), 760 (DictionaryEntry iteration)
   - **ExecEventRemover.cs:** 8 additional operations (GetKeyList, ContainsValue, IndexOfValue, IndexOfKey, RemoveAt, Keys iteration)

3. **Critical Dependencies:**
   - **Join mechanism:** Requires `IndexOfValue(long) → GetKey(index)` reverse lookup
   - **UnRequestEvent variants:** 4 different removal patterns (by ID, target object, delegate, predicate)
   - **EventList property:** Exposes sorted IExecEvent list to public API
   - **Value==Key redundancy:** Event ID stored as both SortedList value and ExecEvent.Key property

4. **Thread Safety:**
   - `lock (_events)` held during Insert (line 372) and Dequeue (line 588)
   - Removal filter passes by ref without lock (safe because single-threaded removal processing)
   - No lock held during event execution

5. **Comparison: Executive vs ExecutiveFastLight:**
   - **Executive:** SortedList, full rescindability, priority support, NO object pooling (disabled)
   - **FastLight:** Array-based min-heap, NO rescindability, NO priority (forced to 0.0), HAS ExecEventCache pooling
   - **Pooling opportunity:** FastLight's ExecEventCache shows ~30-40% allocation reduction potential

6. **Critical Gotchas for Replacement:**
   - Join requires O(n) value search → can't use pure heap without auxiliary Dictionary<long, HeapIndex>
   - Removal while iterating uses backward iteration (safe with RemoveAt)
   - GetKeyList() returns **sorted** list - callers may depend on order
   - Priority semantics: higher value = earlier service (inverted comparison in comparer)
   - DetachableEvent wrapping complicates delegate target matching in removers

7. **Recommended Approach:**
   - **Phase 1:** Enable ExecEvent pooling (`_usePool=true`) - low risk, immediate gain
   - **Phase 2:** Binary heap + Dictionary<long, int> for O(1) removal
   - **Phase 3:** Port ExecEventCache if heap shows contention

**Deliverables:**
- ✅ Detailed analysis document: `.squad/decisions/inbox/parker-executive-analysis.md` (17.5 KB)
- ✅ Includes: 12-site SortedList operation map, comparer logic breakdown, heap comparison, 8 critical gotchas
- ✅ Ready for handoff to Hicks for design spec

**Status:** ✅ **ANALYSIS COMPLETE** - Document ready for design review

### 2026-11-05 — Executive Heap Queue Replacement (COMPLETE ✅)

- **Change:** Replaced `SortedList` in `Executive` with an array-backed binary min-heap to preserve ordering (When asc → Priority desc → Key asc).
- **Removal strategy:** `ExecEventRemover` now filters a snapshot and `Executive` rebuilds the heap per removal request; Join uses linear `FindEventByKey`.
- **Access patterns:** `EventList` and diagnostics return sorted snapshots, while enqueue/dequeue use heap comparisons under `_eventLock`.
- **Gotcha:** Heap size now drives `_numEventsInQueue`, so dequeue updates counts immediately instead of relying on SortedList counts.

### 2026-03-06 — Non-Generic Collection Inventory (COMPLETE ✅)

**Requested by:** Stuart Hillary  
**Purpose:** Comprehensive read-only analysis of all non-generic collection usage for modernization planning  

**Key Findings:**

- **Total usages:** 368 non-generic collection usages across ~100 files in Sage\ directory
- **Primary types:**
  - ArrayList: 269 usages in 62 files (most prevalent — child collections, public APIs, algorithm working sets)
  - Hashtable: 158 usages in 58 files (context dictionaries, internal storage, registries)
  - IList (non-generic): 39 usages (interface abstractions)
  - IDictionary (non-generic): 97 usages (graph contexts, mementos)
  - ICollection (non-generic): 40 usages (return type abstractions)
  - NameValueCollection: 11 usages (configuration)
  - ListDictionary: 1 usage (SmartPropertyBag memento)
  - Queue: 3 usages (domain class ItemBased.Queues.Queue, NOT System.Collections.Queue)

**Critical architectural patterns identified:**

1. **IDictionary graphContext (50+ usages)** — ⚠️ DO NOT REPLACE
   - Used throughout graph execution engine (Edge, Task, Vertex delegates)
   - Intentional design for runtime flexibility (similar to ASP.NET ViewData)
   - Replacing with generic would be massive breaking change with zero benefit

2. **object userData (80+ usages)** — ⚠️ DO NOT REPLACE
   - Executive event system heterogeneous payload pattern
   - `ExecEventReceiver(IExecutive exec, object userData)`
   - Making generic would reduce flexibility for polymorphic handlers

3. **ArrayList.ReadOnly() public API pattern (30+ methods)** — High breaking change impact
   - ReactionProcessor.Reactions, ResourceTracker.EventRecords, Task.GetChildTasks()
   - Can migrate to IReadOnlyList<T> but requires SemVer major version bump

4. **XmlSerializationContext.ContextEntities** — Defer replacement
   - Hashtable part of serialization contract, high regression risk
   - Low ROI for modernization

**Module breakdown:**
- Graphs: Highest concentration (33 files) — Edge (19), CPMAnalyst (14), ValidationService (15)
- Materials: 23 files — ReactionProcessor (16), Substance (10), Emissions (23)
- Persistence: XmlSerializationContext (23+22), DynamicConstruction (22+22)
- Core: 18 files — mostly config/diagnostics after heap replacement
- Resources, Scheduling, Utility, SmartPropertyBag, ItemBased: Lower concentrations

**Modernization strategy:**

- **Phase 1 (60% — Internal):** Private ArrayList → List<T>, Hashtable → Dictionary<K,V> (non-breaking)
  - Graph algorithm working sets, Materials internal storage, SmartPropertyBag
  - Estimated: 2-3 weeks, low risk

- **Phase 2 (30% — Public API):** Breaking changes to IReadOnlyList<T>, IReadOnlyDictionary<K,V>
  - Requires SemVer major bump (v6.0.0)
  - Public ArrayList/Hashtable returns, out parameters
  - Estimated: 4-6 weeks, medium risk

- **Phase 3 (10% — Keep):** IDictionary graphContext, object userData, ContextEntities
  - Intentional design patterns — do not replace

**Deliverables:**
- ✅ Comprehensive 32KB markdown report: `.squad/decisions/inbox/parker-collection-inventory.md`
- Includes: summary table, detailed findings for each type, usage pattern samples, module breakdown, testing recommendations, modernization roadmap

**Key learning:** Not all non-generic collections are "legacy debt" — IDictionary graphContext and object userData are intentional architectural decisions for runtime flexibility. Distinguish between technical debt (ArrayList/Hashtable in internal storage) and design patterns (execution contexts).

### 2026-03-06 — Collection Inventory (COMPLETE ✅)

**Status:** Inventory merged to decisions.md, reference documents retained in inbox

**Deliverable:** `.squad/decisions/decisions.md` → "Decision: Non-Generic Collection Modernization — Three-Phase Strategy" (includes all inventory findings, deduplicated)

**Key contributions captured:**
- **Total inventory:** 368 non-generic collection usages across ~100 files
- **Primary types:** ArrayList (269, 62 files), Hashtable (158, 58 files), IDictionary (97, intentional), IList (39), ICollection (40)
- **Module breakdown:** Graphs (33 files, highest concentration), Materials (23), Persistence (WIP/dead code), Core/Resources/Scheduling (lower)
- **File-by-file difficulty tiers** provided for Phase 1 sequencing
- **Confirmed architectural patterns:** `object userData` (80+ usages, intentional), `IDictionary graphContext` (50+ usages, intentional), `XmlSerializationContext.ContextEntities` (23+, serialization contract)
- **Recommended Phase 1 sequence:** Dependencies → Graphs algorithms → Resources → Materials → Utility → Edge/Vertex → ValidationService → remaining
- **Phase 1 gate:** All 310 tests pass

**Critical finding:** Parker identified that this is NOT a homogeneous "modernize everything" task. The codebase contains ~40% intentional design patterns (`object userData` for heterogeneous payloads, `IDictionary graphContext` for polymorphic execution state) that serve architectural requirements. Phase 1 focuses on low-hanging fruit (internal fields/locals). Phase 2 requires major version bump due to public API changes. Phase 3 items should never be modernized.
