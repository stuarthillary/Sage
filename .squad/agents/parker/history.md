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
