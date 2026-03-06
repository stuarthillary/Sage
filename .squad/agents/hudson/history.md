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

### 2026-07-15 — Test Run on `feature/dotnet10` (net10.0 + MSTest 3.7.3)

**Branch:** `feature/dotnet10`  
**Solution tested:** `Sage4-Everything.sln` (contains `SageTestLib`, `SageTesting/TestDriver`, `Sage_SampleCode`, `Sage4`)  
**Runtime:** net10.0 | **Test framework:** MSTest 3.7.3 | **Duration:** ~40s

#### Results Summary
| Status | Count |
|--------|-------|
| Total  | 304   |
| Passed | 297   |
| Failed | 7     |
| Skipped| 0     |

**Overall: ❌ TEST RUN FAILED (7 failures)**

#### Failing Tests — All in `SageTestLib/TestTuples.cs` (`TupleTester` class)

All 7 failures share a single root cause — assertion at **line 232**: `Assert.IsTrue failed. Incorrect number of elements in "Expected" results.`

The failure means the actual number of simulation events logged in `_results` does not match the expected event sequence array. This is a *count* mismatch (not a hash/value mismatch).

| Failing Test         | Expected elements | Notes |
|----------------------|:-----------------:|-------|
| `TestTupleBasics`    | 6                 | Read+Post+Take at t=0 |
| `TestRead`           | 4                 | Post then Read (+3 min) |
| `TestTake`           | 4                 | Post then Take (+3 min) |
| `TestBlockingPost`   | 8                 | Blocking post + concurrent reads/takes |
| `TestBlockingRead`   | varies            | Blocking read |
| `TestBlockingTake`   | varies            | Blocking take |
| `TestBlock`          | varies            | "MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE!" logged |

The `TestBlock` test additionally logs a fatal simulation model error (`ERROR : MODEL FINISHED WITH SOME TASKS STILL WAITING TO COMPLETE!`), indicating the simulation executive is exiting before all pending events are dispatched.

#### Root Cause Assessment

**Likely cause: threading/scheduler behaviour change in .NET 10.**  
The `TupleTester` tests exercise the `ITupleSpace` implementation via the simulation executive, relying on deterministic event ordering and thread-synchronised blocking operations. The simulation executive uses a thread pool; .NET 10 changed default thread pool behaviour and `Task` scheduling. This can alter the order and timing of unblocking operations in tuple-space blocking reads/posts/takes, producing a different number of emitted events than the hardcoded expected arrays.

**Pre-existing or new?** The `TestTuples.cs` file has been touched in past commits (`480436c` — "Changes to deal with change in string compare behaviour"), showing the test suite has had portability issues before. However, the `.NET 10` upgrade is the most recent systemic change and the most probable trigger. These should be investigated against `origin/dotnet8` to confirm.

**Note:** The `Sage4.sln` solution contains only `Sage4.csproj` (no test projects). Tests must be run against `Sage4-Everything.sln`.

#### Health Assessment

- **297 out of 304 tests pass (97.7%)** — the vast majority of the simulation library is healthy on .NET 10.
- Coverage spans: Core, Scheduling, Graphs, Mathematics, SystemDynamics, ItemBased (materials, reactions, chemistry), Randoms, Persistence, SmartPropertyBag, Utility.
- The 7 failures are all isolated to `TupleTester` — the `ITupleSpace` / `TupleSpace` implementation in `Highpoint.Sage.Utility`.
- No MSTest 3.x framework compatibility issues found — the upgrade itself caused no test infrastructure breakage.
