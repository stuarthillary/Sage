# Squad Decisions

## Active Decisions

### Upgrade Sage to .NET 10

**Author:** Parker (.NET Developer)  
**Date:** 2025-07-15  
**Branch:** `feature/dotnet10`  
**Status:** Ready for review  
**Requested by:** Stuart Hillary

**Decision:** Upgrade target framework from `net8.0` to `net10.0`.

**What Changed:**
- `Directory.Build.props`: `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net10.0</TargetFramework>`

**Build Outcome:** ✅ Build succeeded — 0 errors, 2826 pre-existing warnings (all CA1xxx/CA5xxx Roslyn diagnostics, none new).

**Next Steps:**
1. Upgrade stale test NuGet packages (Microsoft.NET.Test.Sdk 16.7.1→17.x+, MSTest 2.1.1→3.x, coverlet.collector 1.3.0→6.x)
2. Run test suite on .NET 10
3. Address pre-existing CA analyzer warnings (technical debt)

---

### Architectural Modernization: Executive Event Queue (Priority 1)

**Author:** Ripley (Lead / Architect)  
**Date:** 2025-07-15  
**Status:** Pending  
**Impact:** High — affects simulation performance across all models

**Decision:**
Replace O(n) SortedList in Executive event queue with indexed priority queue (heap-based or binary search tree).

**Rationale:**
- Current implementation scans entire list on each insertion/deletion
- Codebase contains 600+ legacy collection usage patterns (not fully type-generic)
- Modernization enables nullable reference type support
- ExecController is the visualization seed; event queue performance cascades to UI responsiveness

**Scope:**
- Core/Executive event handling
- Impact assessment on Scheduling, Resources, SystemDynamics modules

**Next Steps:**
1. Prototype priority queue implementation
2. Benchmark against current O(n) behavior
3. Integration testing with full model execution
4. Migrate remaining legacy collections

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

