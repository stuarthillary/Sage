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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
