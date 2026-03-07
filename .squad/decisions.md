# Squad Decisions

## Active Decisions

### Queue/Stack Generic Migration (COMPLETE ✅)

**Author:** Parker (.NET Developer)  
**Date:** 2026-03-06  
**Status:** Complete  
**Requested by:** Stuart Hillary

## Decision
Replace `System.Collections.Queue` and `System.Collections.Stack` with generic `Queue<T>` and `Stack<T>` across the codebase, keeping public APIs backward-compatible where possible and using explicit generic types for clarity.

## Changes
- Core/Graphs: Executive removal stack -> `Stack<ExecEventRemover>`; GraphSequencer cycle stack -> `Stack<IDependencyVertex>`; CPMAnalyst trace -> `Stack<Vertex>`; DAG cycle path -> `Stack<object>`; ValidationService suspend/resume -> `Stack<string>`.
- ItemBased/Materials/Resources: FixedRateChannel queue -> `Queue<Bin>`; ItemBased Queue backing store -> `Queue<object>`; MaterialService queue -> `Queue<ReservationPair>`; MultiRequestProcessor queue -> `Queue<IResourceRequest>`; SimpleAccessManager stacks -> `Stack<IAccessRegulator>`.
- Scheduling/Persistence: Milestone ActiveStack -> `Stack<bool>` and change queue -> `Queue<Milestone>`; MilestoneRelationship enabled stack -> `Stack<bool>`; TimePeriod adjustment stack -> `Stack<TimeAdjustmentMode>`; CreationContext ParentObjectStack -> `Stack<object>`; XmlSerializationContext node cursor -> `Stack<XmlNode>`.
- Tests: TestPfcNetworks expected activations -> `Queue<IPfcNode>` (fully qualified to avoid ItemBased.Queue name collision).

## Notes
- Public API adjustments: `Milestone.ActiveStack` now returns `Stack<bool>` and `ICreationContext.ParentObjectStack` returns `Stack<object>`.
- Heterogeneous stacks remain typed as `Stack<object>` where mixed content is required.
- No changes made to `object userData`, `IDictionary graphContext`, or `WeakHashTable`.

## Verification
- `dotnet build Sage4-Everything.sln --no-incremental -v minimal` (warnings only).
- `dotnet test Sage_Aux\SageTestLib\SageTestLib.csproj` → Passed 319/319.

---

### Executive Event Queue Heap Replacement (COMPLETE ✅)

**Author:** Parker (.NET Developer)  
**Date:** 2026-03-06  
**Status:** Complete & Tested  
**Requested by:** Stuart Hillary  

**Decision:** Replace `SortedList<ExecEvent, long>` with array-backed binary min-heap in `Executive.cs` and related removal logic in `ExecEventRemover.cs`.

**What Changed:**
- `Executive.cs`: Replaced `SortedList` with `_eventHeap` (array-backed heap)
- Added `HeapEnqueue(ExecEvent)` and `HeapDequeue()` operations
- Implemented `FindEventByKey(long)` for Join reverse lookups (O(N) linear scan)
- `ExecEventRemover.cs`: Updated to filter snapshots and rebuild heap on removal

**Ordering Preserved:** DateTime asc → Priority desc → Key asc (via `CompareEvents`)

**Removal Strategy:** Full heap rebuild from filtered snapshot (O(N log N) per removal, acceptable for rare removals)

**Thread Safety:** All operations guarded by `_eventLock` (existing pattern)

**Build Outcome:** ✅ Build succeeded — 0 new errors, 2826 pre-existing warnings.

**Test Coverage:**
- ✅ 310/310 tests passing (304 existing + 6 new)
- 6 new test methods added by Hudson covering: tie-breaking, predicates, empty queue, EventList ordering, removal+reinsertion
- No regressions
- Duration: ~40 seconds

**Key Decisions:**
1. Heap rebuild per removal request (simple, avoids percolation bugs)
2. Linear scan for Join (acceptable, Join is rare)
3. EventList snapshot sorted for backward compatibility
4. No object pooling (can add in follow-up if profiling warrants)

**Files Modified:**
- `E:\source\Sage\Sage\Core\Executive.cs`
- `E:\source\Sage\Sage\Core\ExecEventRemover.cs`
- `E:\source\Sage\Sage_Aux\SageTestLib\TestExecutive.cs` (6 new test methods added by Hudson)

**Recommendations:**
- ✅ Ready for merge to main
- Consider benchmarking against SortedList if performance is critical downstream

**Benchmark Results (2026-03-06):**
- Benchmark by: Hicks (Performance Engineer)
- Sequential N=100k: 1,029ms → 17.85ms (57.6× speedup)
- Target <50ms: ✅ Exceeded (17.85ms)
- Memory allocation: 9,865 KB (1.20× vs FastLight, acceptable)
- All 310 tests pass (no regressions)
- **Status: READY TO SHIP** ✅

---

### Executive Test Coverage Enhancement for Heap Replacement (COMPLETE ✅)

**Author:** Hudson (QA Engineer)  
**Date:** 2026-03-06  
**Status:** Complete  
**Requested by:** Stuart Hillary  

**Decision:** Audit existing test coverage for `Executive.cs` and add tests for heap replacement edge cases and removal mechanics.

**Assessment Results:**
- Reviewed 16 existing test methods
- **Found:** Priority ordering already well-tested via `TestExecutivePriority`
- **Gaps identified:** 5 coverage gaps around tie-breaking, predicates, empty queue, EventList ordering, removal+reinsertion

**New Tests Added (6 total):**
1. `TestExecutiveKeyTieBreaker` — Key-based tie-breaking (When + Priority identical)
2. `TestExecutiveRemovalAndReinsertion` — Removal and reinsertion at same time slot
3. `TestExecutiveUnRequestPredicate` — Predicate-based removal (4th removal variant)
4. `TestExecutiveEmptyQueueRun` — Running empty executive
5. `TestExecutiveUnRequestOnEmpty` — Removal from empty queue
6. `TestExecutiveEventListOrdering` — EventList returns all events in order

**Helper Addition:** `TestExecEventSelector` class for predicate-based filtering

**Test Execution:**
- ✅ All 310 tests pass (100%)
- ✅ 6 new tests pass
- ✅ 304 existing tests pass (no regressions)
- Duration: ~40 seconds

**Key Findings:**
- Priority ordering semantics (higher value = later execution) already covered
- Heap rebuild strategy validated via removal tests
- Join reverse lookup (O(N)) passes all tests
- EventList snapshot sorting works correctly

**File Modified:**
- `E:\source\Sage\Sage_Aux\SageTestLib\TestExecutive.cs`

**Recommendations:**
- ✅ Test coverage is comprehensive
- All critical ordering and removal mechanics validated
- Ready for merge

---

## Decision: Upgrade Stale Test NuGet Packages

**Author:** Bishop (DevOps Engineer)  
**Date:** 2026  
**Status:** Done  
**Requested by:** stuarthillary

**Decision:** Upgraded four stale test NuGet packages in `Directory.Packages.props` to current stable versions.

| Package | Old Version | New Version |
|---|---|---|
| `Microsoft.NET.Test.Sdk` | 16.7.1 | 17.12.0 |
| `MSTest.TestAdapter` | 2.1.1 | 3.7.3 |
| `MSTest.TestFramework` | 2.1.1 | 3.7.3 |
| `coverlet.collector` | 1.3.0 | 6.0.4 |

**Build Outcome:**
- ✅ `dotnet restore` — resolved cleanly, no errors.  
- ✅ `dotnet build` — 0 errors, 2826 pre-existing CA analyzer warnings (unchanged from baseline).

**Notes:**
- All version pins live in `Directory.Packages.props` (Central Package Management).
- This fulfils the next-step listed in the `.NET 10 upgrade` decision.

---

## Hudson QA Report — Test Results on `feature/dotnet10`

**Date:** 2026-07-15  
**Branch:** `feature/dotnet10`  
**Tested by:** Hudson (QA)  
**Requested by:** stuarthillary  

### ❌ Test Run Failed — 7 Failures

**Solution:** `Sage4-Everything.sln`  
**Framework:** net10.0, MSTest 3.7.3, Microsoft.NET.Test.Sdk 17.12.0  
**Duration:** ~40 seconds  

| Status  | Count |
|---------|-------|
| Total   | 304   |
| Passed  | 297   |
| **Failed**  | **7**     |
| Skipped | 0     |

### Failing Tests

All 7 failures are in a single file and class:

**File:** `E:\source\Sage\Sage_Aux\SageTestLib\TestTuples.cs`  
**Class:** `Highpoint.Sage.Utility.TupleTester`  
**Assertion line:** 232  

**Failing test names:**
1. `TestTupleBasics`
2. `TestRead`
3. `TestTake`
4. `TestBlockingPost`
5. `TestBlockingRead`
6. `TestBlockingTake`
7. `TestBlock`

**Error message (all 7 tests):**
```
Assert.IsTrue failed. Incorrect number of elements in "Expected" results.
```

The test compares a hardcoded expected event sequence array against the actual events emitted by the simulation. The failure is a **count mismatch** — the simulation is producing a different number of events than the expected array specifies.

**Additional signal from `TestBlock`:**
```
ERROR : MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE!
```

This indicates the simulation executive is terminating before all scheduled events are dispatched — a strong indicator of a threading/concurrency behaviour change.

### Root Cause Analysis

**Most likely cause:** .NET 10 thread pool / Task scheduler changes

The `TupleSpace` implementation uses blocking synchronization (blocking post, blocking read, blocking take) coordinated via the simulation executive's thread pool. .NET 10 changed default thread pool warmup, growth, and scheduling heuristics. This can cause:
- Fewer worker threads available early in execution
- Changed ordering of thread wake-ups for blocking tuple operations
- Simulation model finishing before all blocked events complete

The tests encode hardcoded expected event sequences (tick-timestamp + operation label + hash code). A threading difference causes a different number of operations to complete within the simulation run, so `_results.Count != expected.Length`.

**Not MSTest 3.x or test framework related:**
The 297 passing tests cover a wide surface area using the same upgraded framework. No framework-level compatibility failures were found. The issues are purely in simulation execution behaviour.

**Possibly pre-existing on Linux / net8:**
Past commit `480436c` ("Changes to deal with change in string compare behaviour") shows the test suite has needed portability fixes before. Confirm by running against `origin/dotnet8` baseline.

### Recommendation for Team

**Priority:** Medium — 97.7% of the suite passes. The failures are isolated to `TupleSpace`.

**Suggested actions:**
1. **Parker / Core:** Investigate `ITupleSpace` / `TupleSpace` implementation (`Highpoint.Sage.Utility`) for threading assumptions that break under .NET 10 thread pool behaviour.
2. **Hudson:** Once fix is ready, re-run the 7 tests and verify they produce expected event counts. Consider whether the hardcoded expected arrays should be regenerated (the test has a benchmark mode at line 228 that prints the actual results).
3. **Bishop:** No test infrastructure changes needed — MSTest 3.7.3 upgrade is clean.

### What's Healthy ✅

297 tests pass across all major modules:
- Core (Executive, Scheduling, PFC graphs)
- Mathematics (Statistics, Histograms)
- SystemDynamics (Rates, Levels, Stocks)
- ItemBased (Servers, Buffers, Splitters, Joiners, Resources)
- Materials (Chemistry, Reactions, Mixtures, Boiling points)
- SmartPropertyBag
- Utility (Trees, non-Tuple components)

## Parker: TupleSpace .NET 10 Failures - RESOLVED ✅

**Date:** 2026-03-06  
**Status:** Fixed  
**Tests:** 304/304 passing  
**Commit:** `5276d47`

**Decision:** The root cause was a pre-existing race condition in `Exchange.NonBlockingPost()`, not thread pool starvation as initially suspected. Added `ContainsKey()` checks before accessing `_waitersToRead` and `_waitersToTake` dictionaries.

**Why .NET 10 Exposed It:** Different thread pool startup timing and scheduling altered execution order, hitting the race condition more frequently. Bug existed on .NET 8 but was timing-masked.

**Option 2 Evaluation:** Explicit thread replacement was tested but is NOT RECOMMENDED—it fixes the TupleSpace symptom but breaks ResourceManager tests. The Exchange.cs fix alone is sufficient and surgical.

**Files Modified:** Only `Sage/Utility/Exchange.cs` (2 `ContainsKey()` guards added)

**Recommendation:** ✅ Merge `feature/dotnet10` to main. No architectural changes needed.

---

# TupleSpace Test Failures on .NET 10 - Investigation Report (ARCHIVED)

**Date:** 2026-07-15  
**Investigated by:** Parker (.NET Developer)  
**Status:** Root cause identified, fix in progress  

## Problem Summary

7 tests in `TupleTester` fail on `feature/dotnet10` branch with:
- "Incorrect number of elements in Expected results"  
- "MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE!"

All tests pass on `dotnet8` branch. No code changes between branches - only `TargetFramework` changed from net8.0 → net10.0.

## Root Cause Analysis

### Confirmed via git diff
```bash
git diff dotnet8..feature/dotnet10 -- Sage/Core/Executive.cs Sage/Core/DetachableEvent.cs Sage/Utility/Exchange.cs Sage/Utility/TupleSpace.cs
```
Result: **No code changes.** This is a pure .NET 10 runtime behavior change.

### The Threading Architecture

The simulation executive uses a `DetachableEvent` pattern for concurrent event execution:

1. **Executive thread** (dedicated, not thread pool) services events sequentially
2. When encountering `ExecEventType.Detachable` event:
   - Creates `DetachableEvent` wrapper  
   - Calls `DetachableEvent.Begin()` which:
     - Starts `Task.Run(() => eventHandler())` on thread pool
     - Blocks executive thread on `ManualResetEventSlim.Wait()`
     - Task runs, then continuation calls `End()`
     - `End()` signals `_beginResetEvent.Set()` to unblock executive

3. Event handlers (like `PostTuple`, `ReadTuple`) run on thread pool threads
4. Handlers can call `Suspend()` to yield back to executive, or just return when done

### What Changed in .NET 10

Per Microsoft documentation and community reports:
- .NET 10 has **more aggressive thread pool starvation detection**
- Blocking primitives (`ManualResetEventSlim.Wait()`, `Monitor.Wait()`) on threads waiting for thread pool work can trigger starvation mitigations
- While the executive thread isn't a thread pool thread, the **interaction pattern** (executive blocks waiting for thread pool task) may trigger new heuristics

### Observable Symptom

Test output shows:
```
Expected: RT1, RT2b, PT1, PT2, TT1, TT2a  
Actual:   RT1, RT2b, PT1, TT1, TT2a
```

**PT2 is missing** - the `PostTuple()` method starts (PT1 logged) but never completes (PT2 never logged).

This suggests:
1. The thread pool task starts execution
2. Task begins running `PostTuple()`
3. Task adds PT1 to results
4. **Something prevents PT2 from being added**
5. Either:
   - Task is prematurely terminated
   - Continuation (`End()`) runs before task finishes
   - Deadlock/race condition in synchronization

### Attempts Made

1. ✗ **Task.Factory.StartNew with TaskCreationOptions.LongRunning**  
   *Rationale:* Use dedicated thread instead of thread pool  
   *Result:* Still fails

2. ✗ **ContinueWith with TaskScheduler.Default**  
   *Rationale:* Ensure continuation runs on default scheduler  
   *Result:* Still fails

3. ✗ **ContinueWith with TaskContinuationOptions.ExecuteSynchronously**  
   *Rationale:* Reduce scheduling latency  
   *Result:* Still fails

4. ⚠️ **Debug logging added**  
   *Issue:* Console.WriteLine calls in DetachableEvent don't appear in test output  
   *Implication:* Unable to confirm whether continuation is executing

## Hypothesis

The most likely issue is a **race condition or deadlock** in the synchronization between:
- Executive thread blocked on `ManualResetEventSlim.Wait()`
- Thread pool task calling into `Exchange.Post()` which calls `idec.Resume()`
- `Resume()` acquiring event lock and posting new events to executive

In .NET 10's stricter thread management, this circular dependency may cause:
- Detached task to hang waiting for lock
- Executive to finish event queue before task completes
- "MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE" error

## Recommended Next Steps

### Option 1: Refactor DetachableEvent to use async/await (MAJOR)
Replace `ManualResetEventSlim` blocking with `TaskCompletionSource` and async coordination.  
**Pros:** Modern, aligns with .NET 10 best practices  
**Cons:** Large refactor, affects entire simulation engine

### Option 2: Dedicated thread for detachable events (MEDIUM)
Replace thread pool usage with explicit `new Thread(() => ...) { IsBackground = true }.Start()`  
**Pros:** Explicit control, no thread pool interaction  
**Cons:** Higher thread overhead

### Option 3: Investigate Exchange.Post deadlock (TARGETED)
Add synchronization tracing to understand exact lock contention point.  
**Pros:** Surgical fix if deadlock confirmed  
**Cons:** Requires deep runtime debugging

### Option 4: Wait for .NET 10 RTM / file bug with Microsoft
Current testing is against .NET 10.0.103 preview.  
**Pros:** Issue may be fixed in RTM  
**Cons:** Blocks .NET 10 adoption

## Files Involved

- `E:\source\Sage\Sage\Core\DetachableEvent.cs` - Detachable event coordination
- `E:\source\Sage\Sage\Core\Executive.cs` - Event loop and locking (lines 554-728)
- `E:\source\Sage\Sage\Utility\Exchange.cs` - TupleSpace implementation  
- `E:\source\Sage\Sage_Aux\SageTestLib\TestTuples.cs` - Failing tests

## Code Patterns to Examine

1. `DetachableEvent.Begin()` line 85: `_beginResetEvent.Wait()` - blocking wait
2. `DetachableEvent.Resume()` line 145-147: `AcquireEventLock()` + `RequestEvent()` - potential circular dependency
3. `Executive.cs` line 656-662: Event lock waiting logic
4. `Executive.cs` line 727-728: `while (RunningDetachables.Count > 0) Thread.SpinWait(1)` - spin-wait for completion

## Current Branch State

Branch `feature/dotnet10` has partial changes from investigation:
- DetachableEvent.cs has debug Console.WriteLine calls (should be removed)
- Using `Task.Factory.StartNew` with `TaskCreationOptions.LongRunning`  
- Continuation uses `TaskContinuationOptions.ExecuteSynchronously`

**These changes did not fix the issue.**

## Recommendation

**Escalate to team discussion.** This is a non-trivial concurrency issue requiring:
- Deep understanding of simulation semantics (when can events truly run concurrently?)
- Architectural decision on threading model
- Possibly wait for .NET 10 RTM or engage with .NET team

**Do NOT merge `feature/dotnet10` until this is resolved.**


---

### 2026-03-06 — DetachableEvent.cs Debug Cleanup (COMPLETE)

**Agent:** Bishop  
**Branch:** `feature/dotnet10`  
**Commit:** `eabf539`

**Decision:** Remove debug artifacts (commented-out `_Debug.WriteLine()` calls) from `DetachableEvent.cs` left over from Parker's .NET 10 investigation.

**Findings:**
- 4 commented-out debug lines in `Resume()` and `End()` methods
- All lines were `_Debug.WriteLine()` traces from investigation phase
- 1 active `_Debug.WriteLine()` in exception handler (line 224) retained as legitimate error logging

**Action:** Removed all 4 commented debug lines and committed.

**Rationale:**
1. Code cleanliness — commented debug code violates production standards
2. Clarity — future maintainers see only the changes that actually fixed the issue (Exchange.cs)
3. Alignment — decisions.md explicitly noted "DetachableEvent debug calls (should be removed)"

**Impact:** `feature/dotnet10` branch is **merge-ready**. No functional changes; debug cleanup only.

**Related to:** TupleSpace .NET 10 fix (commit `5276d47`). Final state: only Exchange.cs changed (ContainsKey guards), DetachableEvent.cs clean.

---

### 2026-03-07 — Post-Migration Benchmark Run: Generic Collection Migration (Phases 1–3) ✅

**Author:** Hicks (Performance Engineer)  
**Date:** 2026-03-07  
**Branch:** `feature/dotnet10`  
**Requested by:** Stuart Hillary  
**Status:** ✅ Confirmed — No Performance Regression

**Decision:** The generic collection migration (Phases 1–3) introducing zero measurable performance impact to the Sage DES engine throughput or memory allocation profile. The migration is safe to merge.

**Evidence:**
- **Tool:** BenchmarkDotNet v0.15.8, `[ShortRunJob]` + `[MemoryDiagnoser]`
- **Runtime:** .NET 10.0, Windows 11
- **N=100,000 Primary Workload:**
  - Executive sequential: 17,851 µs → 19,280 µs (+8.0%, within CI ±36%)
  - ExecutiveFastLight sequential: 17,132 µs → 17,309 µs (+1.0%, noise)
  - Executive chained: 4,664 µs → 4,768 µs (+2.2%, noise)
  - ExecutiveFastLight chained: 1,431 µs → 1,407 µs (-1.7%, noise)
- **Memory @ N=100,000 (byte-for-byte identical):**
  - Executive: 9,865 KB → 9,865 KB (0 bytes)
  - ExecutiveFastLight: 8,203 KB → 8,203 KB (0 bytes)

**Rationale:** Migrated collections (`Queue<T>`, `Stack<T>`, `List<T>`) are in peripheral subsystems (resource stacks/queues, material queues, persistence stacks) — none on the event dispatch hot path. Benchmarks exercise binary min-heap event queue in `Executive.cs` and `ExecutiveFastLight.cs`, which were untouched by the migration.

**Recommendation:** ✅ Merge `feature/dotnet10`. No performance regression. No follow-up work required.

---

### Configuration Modernization — Remove System.Configuration.ConfigurationManager (PROPOSED)

**Author:** Ripley (Lead / Architect)  
**Date:** 2026-07-15  
**Status:** Proposed  
**Requested by:** Stuart Hillary

---

## Context

Sage is a **class library** (Sage.dll). It currently depends on `System.Configuration.ConfigurationManager` (NuGet package) to read XML `app.config` sections at runtime. This couples the library to a host application's config file — a pattern that's hostile to modern .NET usage (containers, serverless, test isolation, library composition).

Six files read from `ConfigurationManager`. The wrapper file `Utility/ConfigurationManager.cs` is already commented-out dead code.

## Decision

Replace all `System.Configuration.ConfigurationManager` usage with a **library-safe options pattern**: plain C# POCO options classes with sensible defaults, accepted via optional constructor parameters. No dependency on `Microsoft.Extensions.Options` or any DI container within the library itself.

## Architecture

### Core Principle: Library-Safe Options

The library defines options POCOs. Consumers pass them in. If they don't, the library uses defaults. The library never reaches into ambient configuration.

### Options Classes (new types)

```csharp
namespace Highpoint.Sage.SimCore
{
    public sealed class ExecutiveOptions
    {
        public int MaxWorkerThreads { get; set; } = 900;
        public int MinWorkerThreads { get; set; } = 100;
        public int MinIocThreads { get; set; } = 50;
        public int MaxIocThreads { get; set; } = 100;
        public bool IgnoreCausalityViolations { get; set; } = true;
    }

    public sealed class ExecFactoryOptions
    {
        public string DefaultExecutiveType { get; set; } = "Highpoint.Sage.SimCore.Executive, Sage";
    }
}

namespace Highpoint.Sage.Diagnostics
{
    public sealed class DiagnosticsOptions
    {
        public Dictionary<string, bool> Flags { get; set; } = new();
        public bool LogMissingDiagKeys { get; set; } = false;
        public DateTime? ExecBreakAt { get; set; } = null;
    }
}

namespace Highpoint.Sage.Materials.Chemistry.Emissions
{
    public sealed class EmissionsServiceOptions
    {
        public string EquationSet { get; set; } = "CTG";
        public bool IgnoreUnknownModelTypes { get; set; } = false;
        public bool Enabled { get; set; } = true;
        public bool PermitOverEmission { get; set; } = false;
        public bool PermitUnderEmission { get; set; } = false;
        public IReadOnlyList<IEmissionModel>? Models { get; set; } = null;
    }
}
```

### Constructor Signature Changes

| Class | Current | Proposed |
|---|---|---|
| `Executive` (internal) | `Executive(Guid execGuid)` | `Executive(Guid execGuid, ExecutiveOptions? options = null)` |
| `ExecutiveFastLight` (internal) | `ExecutiveFastLight(Guid execGuid)` | `ExecutiveFastLight(Guid execGuid, ExecutiveOptions? options = null, DiagnosticsOptions? diagnosticsOptions = null)` |
| `ExecFactory` (public, singleton) | `ExecFactory()` (private) | `ExecFactory(ExecFactoryOptions? options = null, ExecutiveOptions? executiveOptions = null)` — instance still created via `Instance`, but gains a `Configure(...)` static method |
| `DiagnosticAids` (public, static) | N/A (static class) | `DiagnosticAids.Configure(DiagnosticsOptions options)` static method |
| `ModelConfig` (public) | `ModelConfig(string sectionName)` | **Deprecate.** Replace with `SageModelOptions` POCO or remove entirely. |
| `EmissionsService` (public, singleton) | `EmissionsService()` (private) | `EmissionsService(EmissionsServiceOptions? options = null)` + `Configure(EmissionsServiceOptions)` static method |

### Migration Order

1. **DiagnosticsOptions + DiagnosticAids** — lowest risk, static class, no public constructor changes
2. **ExecutiveOptions + Executive + ExecutiveFastLight** — internal classes, no public API impact
3. **ExecFactoryOptions + ExecFactory** — public singleton, but `Configure()` is additive
4. **ModelConfig refactor** — public class on public interface, needs care
5. **EmissionsServiceOptions + EmissionsService** — complex but isolated subsystem
6. **Remove `System.Configuration.ConfigurationManager` PackageReference from Sage4.csproj**
7. **Delete `Utility/ConfigurationManager.cs`** (already dead code)
8. **Delete `EmissionsServiceConfigurationHandler`** class
9. **Update/remove app.config references in error messages and XML doc comments**

## What This Does NOT Change

- `IExecutive` interface — unchanged
- `IModel` interface — `ModelConfig` property type unchanged (class refactored internally)
- Event dispatch semantics — unchanged
- Threading model — unchanged (thread pool config just comes from options instead of XML)
- Any simulation determinism properties

## Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Static "configure once" semantics for DiagnosticAids | Low | Matches existing behavior. Document. |
| ExecFactory reflection now needs 2-param constructor | Medium | Both constructors (1-param and 2-param) should be supported during transition |
| EmissionsService singleton reset between tests | Medium | Add `EmissionsService.Reset()` for test isolation |
| ModelConfig public API shape change | Low | Keep the class, change only internal implementation |
| Thread pool config applied globally | Low | Already global today — no change in blast radius |

## Constraints Satisfied

- ✅ Usable without DI container (`new Executive(guid)` still works, uses defaults)
- ✅ Supports DI consumers (options are plain POCOs, injectable)
- ✅ No breaking public constructor changes (additive optional parameters only)
- ✅ ConfigurationManager usage eliminated
- ✅ No new package dependencies required (Microsoft.Extensions.Options NOT added)

**Implementation spec:** See Parker work spec for detailed implementation steps.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

