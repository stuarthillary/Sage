# Decisions — Sage DES Engine

Collective technical and architectural decisions for the Sage discrete event simulation library (feature/dotnet10 branch).

---

## Decision: Executive Event Queue Replacement — Heap-Based Priority Queue

**Authors:** Hicks (Performance Engineer), Parker (Code Analyst)  
**Date:** 2026-01-24  
**Branch:** `feature/dotnet10`  
**Status:** Design Complete — Ready for Implementation  
**Requested by:** Stuart Hillary

### The Problem

`Executive.cs` uses `SortedList<ExecEvent, long>` with custom comparer for event queue management. The dispatch loop dequeues from index 0 using `RemoveAt(0)`, which shifts all remaining elements one position left — **O(N) per dequeue**. For N events, total complexity is **O(N²)**:

- **Benchmark:** 100,000 sequential events take ~1,029ms (Executive with SortedList)
- **Reference:** Same workload takes ~16.75ms with `ExecutiveFastLight` heap (61× faster)

### The Decision

**Replace `SortedList` with a custom binary min-heap** (array-backed, 1-indexed), matching `ExecutiveFastLight`'s proven implementation:

- **Target complexity:** O(N log N) enqueue + O(N log N) dequeue = O(N log N) total
- **Expected gain:** 50–60× speedup for bulk-event workloads
- **Preserved:** All full-featured capabilities (rescindable events, priority handling, join semantics, detachable events, pause/resume)

### Why NOT System.Collections.Generic.PriorityQueue<T, TPriority>?

`PriorityQueue` requires a single `TPriority` type. Executive's sort order is **composite** (3-level):

1. **Level 1:** `When` (DateTime) — ascending (chronological)
2. **Level 2:** `Priority` (double) — **descending** (higher priority fires first at same time)
3. **Level 3:** `Key` (long) — ascending (unique tie-breaker for determinism)

While you could wrap this in a custom struct, the allocation overhead defeats the optimization goal. ExecutiveFastLight already proves the pattern works.

### Core Changes

#### Data Structure

- **Remove:** `SortedList<ExecEvent, long> _events`
- **Add:** `ExecEvent[] _eventHeap` (1-indexed), `int _eventHeapCapacity`
- **Initialization:** Heap size = 16, matching ExecutiveFastLight

#### Comparison Logic

New `CompareEvents` method implements composite ordering:

```csharp
private static int CompareEvents(ExecEvent ee1, ExecEvent ee2)
{
    // Level 1: When (ascending)
    if (ee1.When < ee2.When) return -1;
    if (ee1.When > ee2.When) return 1;
    
    // Level 2: Priority (descending — higher priority first)
    if (ee1.Priority > ee2.Priority) return -1;  // Higher priority is "less" in heap terms
    if (ee1.Priority < ee2.Priority) return 1;
    
    // Level 3: Key (ascending, for determinism)
    if (ee1.Key < ee2.Key) return -1;
    if (ee1.Key > ee2.Key) return 1;
    
    return 0;
}
```

#### Heap Operations

- **HeapEnqueue:** Sift-up on insertion, O(log N), dynamic capacity (2× growth)
- **HeapDequeue:** Sift-down on removal from root, O(log N)
- **EventList property:** Return snapshot of heap contents (iteration order differs from SortedList)

#### Thread Safety

- **Locking Change:** All `lock (_events)` → `lock (_eventLock)` (existing object at line 47)
- **Why:** Arrays cannot be locked directly; must use stable reference

#### Event Removal (UnRequest Variants)

Current code uses SortedList methods (IndexOfValue, RemoveAt). Replacement uses **rebuild-on-remove** strategy:

1. Linear scan heap to collect events matching predicate
2. Clear heap and re-enqueue retained events
3. Complexity: O(N) scan + O(N log N) rebuild (acceptable, removal is rare)

For `Join` reverse lookup by event key: Linear scan of heap (O(N)), no auxiliary index yet (optimization for later if profiling shows contention).

#### EventList Public API

`EventList` property (line 188) currently exposes sorted keys via `GetKeyList()`. Replacement:

```csharp
public IList EventList
{
    get
    {
        ExecEvent[] snapshot = new ExecEvent[_numEventsInQueue];
        Array.Copy(_eventHeap, 1, snapshot, 0, _numEventsInQueue);
        return ArrayList.ReadOnly(new ArrayList(snapshot));
    }
}
```

**Note:** Heap contents are not in sorted order (only min at root). Tests iterating `EventList` expecting sorted order may break—will validate with full test suite.

### What Stays the Same

To contain scope and avoid regressions:

- Detachable event logic unchanged
- Pause/Resume/Abort logic unchanged
- Causality violation checks unchanged
- All event monitor infrastructure unchanged
- ThreadPool configuration unchanged

### Object Pooling — Defer

`ExecutiveFastLight` shows ~30-40% allocation savings with pooling. **Out of scope for this change.**

**Current state:** ExecEvent.Get() has pooling infrastructure but `_usePool = false`. After benchmarking the heap, if allocations are high, enable pooling as a one-line follow-up.

### Test Coverage

Existing tests must all pass:

| Test | Validates | Critical |
|------|-----------|----------|
| TestExecutiveCount | Event count tracking | ✅ |
| TestExecutivePriority | Priority ordering (same time) | ⚠️ Heap must handle descending priority |
| TestExecutiveWhen | Chronological ordering | ✅ |
| TestExecutiveUnRequestHash/Target/Delegate | All removal paths | ⚠️ Must test removal rebuilds |
| TestHeap | Heap integrity | ✅ Reference suite |

**New Benchmark:** Add `Executive_PriorityStress` (10k events, same time, random priority) to validate priority-heavy workloads complete in <10ms.

### Success Criteria

- ✅ All existing tests pass
- ✅ `Executive_SequentialEvents` (100k) drops from ~1,029ms to <50ms
- ✅ `Executive_PriorityStress` (10k same-time events) completes in <10ms
- ✅ Memory allocations unchanged (heap storage, not additional GC pressure)
- ✅ Public APIs (EventList, Join, UnRequest variants) work identically

### Implementation Checklist

1. Replace field: SortedList → ExecEvent[] _eventHeap
2. Add HeapEnqueue, HeapDequeue, CompareEvents methods
3. Update RequestEvent to call HeapEnqueue
4. Update dispatch loop dequeue (line 595–596) to call HeapDequeue
5. Update peek logic (line 668) to access _eventHeap[1].When
6. Fix locking: lock(_events) → lock(_eventLock) at 4 sites
7. Update EventList property to return heap snapshot
8. Update Reset() to initialize heap
9. Refactor event removal (ExecEventRemover integration or RemoveWhere predicate)
10. Update Join reverse lookup with FindEventByKey
11. Run all unit tests
12. Run benchmarks and validate performance

### Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Priority comparison inverted | Medium | High | Run TestExecutivePriority early; unit test CompareEvents |
| Event removal breaks (UnRequest paths) | Medium | High | Test all 4 variants; consider rebuild approach first |
| EventList sorted order break | Low | Medium | Check tests; sort snapshot if needed |
| Lock contention changes | Low | Low | Semantically equivalent locking |
| Heap growth suboptimal | Low | Low | Start 2×; profile if issues arise |

---

## Decision: BenchmarkDotNet Baseline — Sage DES Engine

**Author:** Hicks (Performance Engineer)  
**Date:** 2026-07-15  
**Branch:** `feature/dotnet10`  
**Status:** Baseline established — ready for optimization work  
**Requested by:** Stuart Hillary

### What Was Benchmarked and Why

#### Project created
`Sage_Aux\SageBenchmarks\SageBenchmarks.csproj` — BenchmarkDotNet 0.15.8, targeting `net10.0`, referencing `Sage4.csproj`.

#### Benchmarks defined

| Benchmark | Hot Path Exercised | Why |
|---|---|---|
| `Executive_SequentialEvents` (baseline) | `SortedList.Add` + `RemoveAt(0)` × N | Exposes O(N²) dequeue cost in `Executive` |
| `ExecutiveFastLight_SequentialEvents` | Heap `Enqueue` + `Dequeue` × N | Establishes O(N log N) target for optimization |
| `Executive_ChainedEvents` | per-event: lock + SortedList + dispatch | Realistic DES loop, isolates per-event overhead |
| `ExecutiveFastLight_ChainedEvents` | per-event: heap insert + dispatch + pool return | Establishes realistic-scenario baseline |

Parameters: `EventCount` ∈ {1_000, 10_000, 100_000}. Attributes: `[ShortRunJob]`, `[MemoryDiagnoser]`.

#### Run command

```sh
dotnet run -c Release --project Sage_Aux\SageBenchmarks\SageBenchmarks.csproj
```

### Hot-Path Findings (Code Review)

#### The O(N²) bottleneck in Executive

`Executive.Start()` (dispatch loop, `Sage\Core\Executive.cs` ~line 595):

```csharp
currentEvent = (ExecEvent)_events.GetKey(0);   // O(1)
_events.RemoveAt(0);                            // O(n) — shifts ALL remaining elements
```

`SortedList.RemoveAt(0)` has to shift every remaining element one position left. For N events this is N × O(N) = **O(N²)** total cost. This is the primary bottleneck Ripley's architectural decision targets.

**Additional overhead in Executive per event:**
- `Monitor.Enter/Exit(_runLock)` — one round-trip per event
- `lock(_events)` — held during scheduling AND selected during dispatch
- `_clockAboutToChange` null check + possible `_events.GetKey(0)` call after each dispatch
- Causality check comparing `_now` to requested time
- Diagnostics check (static bool)

#### ExecutiveFastLight is already correct

`ExecutiveFastLight.Enqueue()` is a standard min-heap sift-up: O(log n), O(1) for ascending-sorted input.  
`Dequeue()` is O(log n) sift-down always. `ExecEventCache` pools `_ExecEvent` objects, eliminating allocations after warm-up.

The `StartWocv()` path (no-causality-violation mode, the default) is clean: dequeue → update `_now` → invoke delegate → return to pool. No locks, no event monitors, no extra checks.

### Recommendations for Optimization Work (for Parker)

#### Priority 1 — Replace `SortedList` in Executive

**Target:** `Sage\Core\Executive.cs`, field `_events` (line 27), and `Start()` dispatch loop (line 595).

Replace `SortedList` with a proper priority queue. Options:
1. `System.Collections.Generic.PriorityQueue<TElement, TPriority>` (built into .NET 6+) — zero-dependency, O(log n) enqueue and dequeue
2. A custom array-backed min-heap matching `ExecutiveFastLight`'s pattern

The `ExecEventComparer` sorts by `(DateTime when, double priority, long key)`. The replacement must preserve this three-level ordering.

**Expected gain:** O(N²) → O(N log N). At N=100,000 events this should be a 100–1000× speedup.

#### Priority 2 — Object pooling for ExecEvent in Executive

`Executive.RequestEvent()` calls `ExecEvent.Get(...)` which may or may not pool (needs profiling with MemoryDiagnoser output). If it allocates, add an `ExecEventCache`-style pool matching `ExecutiveFastLight`.

**Expected gain:** Reduced GC pressure, lower Gen0/Gen1 collection rate under high-throughput simulations.

#### Priority 3 — Evaluate lock granularity in Executive

`lock(_events)` is held on every `RequestEvent()` call. For single-threaded simulations (the common case), this is pure overhead. Consider a fast path that bypasses locking when the executive is not running or has no concurrent access.

#### Priority 4 — Do NOT change ExecutiveFastLight

It is already near-optimal. Use its implementation as the reference model for the Executive refactor.

### What NOT to Benchmark Yet

- **DetachableEvent overhead** — requires `Executive` only, not `ExecutiveFastLight`. Involves `Task.Run`, `ManualResetEventSlim`, and thread pool scheduling. Worthwhile after Priority 1 is done and the Executive baseline is re-run.
- **Resource scheduling** (`Sage\Resources\`) — depends on event dispatch. Profile after the event queue is fixed.
- **Exchange/TupleSpace** — not a dispatch hot path; only relevant for concurrent tuple-based simulations.

---

## Decision: BenchmarkDotNet Build Fix — Local Directory.Build.props

**Author:** Hicks (Performance Engineer)  
**Date:** 2026-07-15  
**Branch:** `feature/dotnet10`  
**Status:** Done  
**Requested by:** Stuart Hillary

### Problem

Running BenchmarkDotNet benchmarks failed with:

```
error NETSDK1004: Assets file 'E:\source\Sage\BuildOutput\obj\Release\BenchmarkDotNet.Autogenerated\project.assets.json' not found.
Run a NuGet package restore to generate this file. [BenchmarkDotNet.Autogenerated.csproj]
```

**Root cause:** The repo-root `Directory.Build.props` redirects all MSBuild output paths to `BuildOutput\`:

```xml
<BaseIntermediateOutputPath>$(BuildOutput)obj\$(Configuration)\$(MSBuildProjectName)\</BaseIntermediateOutputPath>
<OutputPath>$(BuildOutput)$(Platform)\$(Configuration)</OutputPath>
<OutDir>$(OutputPath)</OutDir>
<!-- ... -->
```

BenchmarkDotNet generates a temporary project (`BenchmarkDotNet.Autogenerated.csproj`) inside the benchmark project's `bin\Release\net10.0\SageBenchmarks-ShortRun-1\` folder. When MSBuild builds that temp project, it walks up the directory tree, finds and applies the repo-root `Directory.Build.props`, and redirects the temp project's `obj\` path to `BuildOutput\obj\Release\BenchmarkDotNet.Autogenerated\`. However, NuGet restore already ran against the default SDK `obj\` path, so `project.assets.json` was not at the redirected location → NETSDK1004.

### Decision

Add a local `Directory.Build.props` at `Sage_Aux\SageBenchmarks\Directory.Build.props` that:

1. **Chains the repo-root props** using `$([MSBuild]::GetPathOfFileAbove(...))` to preserve shared settings (TargetFramework, company metadata, compiler options).
2. **Resets all output-path properties** to empty/default values so BenchmarkDotNet's temp projects use standard SDK layout.

**File created:** `Sage_Aux\SageBenchmarks\Directory.Build.props`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Project>
  <!-- Import parent Directory.Build.props (repo root) for shared settings. -->
  <Import Project="$([MSBuild]::GetPathOfFileAbove('Directory.Build.props', '$(MSBuildThisFileDirectory)../'))" />

  <!-- Reset output-path properties to SDK defaults. -->
  <PropertyGroup>
    <ArtifactsPath />
    <BaseIntermediateOutputPath />
    <IntermediateOutputPath />
    <MSBuildProjectExtensionsPath />
    <BaseOutputPath />
    <OutputPath />
    <OutDir />
    <PublishDir />
  </PropertyGroup>
</Project>
```

**Why this works:** This file is the *closest* `Directory.Build.props` in the directory hierarchy for both `SageBenchmarks.csproj` and BDN's temp project (which lives under `bin\`). Both receive the shared repo settings but with output paths reset to SDK defaults, so NuGet restore and build agree on where `project.assets.json` lives.

### Outcome

All 12 benchmark cases ran successfully. Sample results (ShortRun, N=1000 events):

| Benchmark | Mean |
|---|---|
| `Executive_SequentialEvents` | ~287 µs |
| `ExecutiveFastLight_SequentialEvents` | ~80 µs |
| `Executive_ChainedEvents` | ~179 µs |
| `ExecutiveFastLight_ChainedEvents` | ~82 µs |

FastLight is ~3.6× faster than Executive at N=1000 — consistent with O(N log N) vs O(N²) theoretical expectations documented in history.md.

### Notes

- The `SageBenchmarks.csproj` itself continues to inherit `TargetFramework`, company metadata, `Optimize=true` for Release, and all other non-path settings from the repo root.
- Only `BuildOutput\`-routing properties are cleared; no output paths are hardcoded.
- This pattern (chain parent + reset) is the standard approach for MSBuild tool projects that need shared settings but cannot tolerate output-path redirection.
