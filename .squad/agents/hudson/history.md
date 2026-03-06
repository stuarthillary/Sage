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

### 2026-03-06 — Test Results on `feature/dotnet10` with TupleSpace Fix ✅

**Branch:** `feature/dotnet10`  
**Solution tested:** `Sage4-Everything.sln`  
**Runtime:** net10.0 | **Test framework:** MSTest 3.7.3 | **Duration:** ~40s  
**Status:** ✅ ALL PASSING

#### Results Summary
| Status | Count |
|--------|-------|
| Total  | 304   |
| Passed | 304   |
| Failed | 0     |
| Skipped| 0     |

**Test Run: ✅ SUCCESS (0 failures)**

All 7 previously failing TupleTester tests now pass with Exchange.cs race condition fix.

#### Fix Applied

Root cause: Race condition in `Exchange.NonBlockingPost()` — missing `ContainsKey()` checks before dictionary access. Generic `HashtableOfLists<TKey,TValue>` throws `KeyNotFoundException` if key doesn't exist (differs from non-generic Hashtable behavior).

**File Modified:** `Sage/Utility/Exchange.cs`  
**Lines Changed:** 2 if-guards added to `NonBlockingPost()`  
**Commit:** `5276d47`

#### Health Assessment

- **✅ 304 out of 304 tests pass (100%)**
- Coverage spans all 16 modules: Core, Scheduling, Graphs, Mathematics, SystemDynamics, ItemBased, Materials, Randoms, Persistence, SmartPropertyBag, Utility
- No MSTest 3.x framework compatibility issues
- No regressions — all previously passing tests still pass
