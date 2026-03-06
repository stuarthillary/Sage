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

---

## Decision: Non-Generic Collection Modernization — Three-Phase Strategy

**Authors:** Ripley (Lead Architect), Parker (.NET Developer)  
**Date:** 2026-03-06  
**Status:** Analysis Complete — Ready for Phase 1 Execution  
**Requested by:** Stuart Hillary

### Executive Summary

The Sage DES library contains **~600 non-generic collection usages** across **~100 source files**. Analysis identified three risk tiers enabling phased modernization without disrupting consumers.

| Tier | Phase | Usages | Risk | API Impact | Effort | Timeline |
|------|-------|--------|------|-----------|--------|----------|
| 1 | Internal Modernization | ~360 (60%) | LOW | None | 2-3 sessions | Now |
| 2 | Public API Modernization | ~180 (30%) | MEDIUM | Breaking | 4-6 sessions | Next major release |
| 3 | Intentional Designs | ~60 (10%) | N/A | Never | — | Never |

### Phase 1: Internal Modernization (Non-Breaking) — Start Now ✅

**Scope:** Replace ~60% of non-generic collection usages with zero public API changes.

**What changes:**
- Private fields: `ArrayList` → `List<T>`, `Hashtable` → `Dictionary<K,V>`
- Local variables and internal method signatures
- Remove unnecessary casts (e.g., `(Edge)edges[i]` → `edges[i]`)
- Replace `DictionaryEntry` with `KeyValuePair<K,V>` where backing collection changes
- Replace non-generic `IComparer` with `IComparer<T>` (GraphSequencer)
- Replace non-generic `Stack` with `Stack<T>` (GraphSequencer)

**What stays:**
- All `public` method signatures and property types
- Private `ArrayList` backing public `IList` returns (Phase 2 handles this)
- All `IDictionary graphContext` patterns
- All `object userData` patterns

**Recommended sequence:**
1. `Dependencies/` (2 files, quick win)
2. Graph algorithm files (DagCheckers, CPMAnalyst, PertAnalyst)
3. Resources and Materials internal fields
4. Utility and SmartPropertyBag
5. Edge.cs and Vertex.cs (largest individual files)
6. ValidationService and remaining Graphs

**Test gate:** All 310 tests pass after each file or small batch.

**Risk:** LOW — Changes invisible to consumers. Only regression risk from type inference errors.

### Phase 2: Public API Modernization (Breaking) — Defer to Major Release

**Scope:** Modernize remaining ~30% of usages — public return types and parameters.

**What changes:**
- Public `ArrayList` returns → `IReadOnlyList<T>` (preferred) or `List<T>`
- Public `Hashtable` returns → `IReadOnlyDictionary<K,V>` or `Dictionary<K,V>`
- Public `IList` (non-generic) → `IReadOnlyList<T>`
- Public `ICollection` (non-generic) → `IReadOnlyCollection<T>`
- `out ArrayList` parameters → `out List<T>`
- `IExecutive.LiveDetachableEvents` → `IReadOnlyList<IDetachableEventController>`
- `IExecutive.EventList` → `IReadOnlyList<IExecEvent>`
- Utility classes: `WeakList : IList` → `WeakList<T> : IList<T>`

**Strategy:** 
- Batch by module — complete one module's public API before next
- Start with least-consumed modules (Materials.Chemistry, PertAnalyst)
- End with IExecutive (highest impact)
- Update test code in lockstep
- Document migration guide for consumers

**Risk:** MEDIUM — Requires SemVer major version bump. All consuming code must update simultaneously.

### Phase 3: Intentional Designs — NEVER Replace

**Scope:** ~10% of usages are architectural patterns serving specific design requirements. **Do not touch.**

| Pattern | Count | Why Intentional | Decision |
|---------|-------|-----------------|----------|
| `object userData` (event payloads) | 80+ | Executive event system intentionally carries heterogeneous data — any simulation entity passes any payload | **KEEP as `object`** |
| `IDictionary graphContext` (execution contexts) | 50+ | Graph execution contexts are polymorphic runtime state bags, used across PFC, CPM, PERT, custom models. Analogous to ASP.NET ViewData. | **KEEP as non-generic `IDictionary`** |
| `IExecEvent.UserData` | Public | Same heterogeneity requirement as userData | **KEEP as `object`** |
| `IExecutive.ClearVolatiles(IDictionary)` | Part of graphContext pattern | Execution context API | **KEEP as non-generic `IDictionary`** |
| `XmlSerializationContext.ContextEntities` (Hashtable) | 23+ | Part of serialization contract; 7+ nested classes delegate to it | **DEFER** until persistence modernization |
| `DynamicConstruction.cs` | 44 | WIP/dead code (`#if INCLUDE_WIP`) | **SKIP** modernization of unused code |
| `NameValueCollection` in Executive | 11 | Configuration concern, not collection concern | **DEFER** to configuration modernization |
| `ModelObjectDictionary : IDictionary` | — | When Model.cs is modernized | **DEFER** to core model rework |
| `ExecutionContext : IDictionary` | — | When execution context model is redesigned | **DEFER** to context rework |

### Collection Mapping Reference

| Non-Generic Type | Generic Replacement | Notes |
|-----------------|-------------------|-------|
| `ArrayList` | `List<T>` | Infer T from usage context; use `IReadOnlyList<T>` for public returns (Phase 2) |
| `ArrayList.ReadOnly(x)` | `x.AsReadOnly()` or `IReadOnlyList<T>` cast | Direct pattern swap |
| `Hashtable` | `Dictionary<TKey, TValue>` | Infer key/value types from usage; use `IReadOnlyDictionary<K,V>` for public returns (Phase 2) |
| `SortedList` (non-generic) | `SortedList<TKey, TValue>` | 2 usages in DetachableEventSynchronizer |
| `Stack` (non-generic) | `Stack<T>` | GraphSequencer cycle detection |
| `IList` (non-generic) | `IList<T>` or `IReadOnlyList<T>` | Prefer read-only for returns |
| `ICollection` (non-generic) | `ICollection<T>` or `IReadOnlyCollection<T>` | Prefer read-only for returns |
| `IDictionary` (non-generic) | **KEEP** for graphContext; `IDictionary<K,V>` elsewhere | See exclusions — graphContext is intentional |
| `IComparer` (non-generic) | `IComparer<T>` | GraphSequencer.DefaultVertexComparer |
| `DictionaryEntry` | `KeyValuePair<TKey, TValue>` | Follows from Hashtable → Dictionary replacement |
| `ListDictionary` | `Dictionary<string, object>` | Single usage in SmartPropertyBag memento |

### Key Findings

**Risk Tiering:**
- **Tier 1 (60% — LOW):** Private fields and local variables. Invisible to consumers. Safe to modernize now.
- **Tier 2 (30% — MEDIUM):** Public API surface. Visible to consumers. Requires breaking change + migration guide.
- **Tier 3 (10% — INTENTIONAL):** Architectural patterns serving flexibility requirements. Do not replace.

**Module Concentration:**
- Graphs: 33 files (Edge 19, CPMAnalyst 14, ValidationService 15)
- Materials: 23 files (ReactionProcessor 16, Substance 10)
- Persistence: XmlSerializationContext (23+22), DynamicConstruction (22+22)
- Core, Resources, Scheduling, Utility: Lower concentrations

**Critical Architectural Insights:**
- `object userData` is NOT legacy debt — it enables heterogeneous event payloads across any simulation model
- `IDictionary graphContext` is NOT legacy debt — it provides runtime flexibility for graph execution contexts (comparable to ASP.NET ViewData or HttpContext.Items)
- NOT all non-generic collections are modernization targets — distinguish between technical debt (ArrayList/Hashtable in internal storage) and intentional design patterns

### Risk Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking consumer code in Phase 2 | High | Medium | SemVer major bump, migration guide, deprecation warnings |
| Hashtable → Dictionary ordering changes | Medium | High | Hashtable has no order; Dictionary preserves insertion order. Tests should catch order-dependent code. |
| Thread safety differences (Hashtable partially thread-safe for reads; Dictionary is not) | Low | High | Review each Hashtable for concurrent access. Executive Hashtables already lock-guarded. |
| ArrayList.ReadOnly() vs List<T>.AsReadOnly() semantic differences | Low | Medium | Use `IReadOnlyList<T>` interface; mitigates ArrayList-to-List casting breakage. |
| Regression in graph execution engine | Medium | High | IDictionary graphContext is excluded. Only internal fields change. Run graph tests after each file. |

### Success Criteria

- ✅ **Phase 1 complete:** Zero `ArrayList`/`Hashtable` in private fields/local variables (excluding deferred). All 310 tests pass.
- ✅ **Phase 2 complete:** Zero non-generic collection types in public API (excluding intentional exclusions). All tests pass. Migration guide written.
- ✅ **Phase 3 tracked:** Deferred items documented with clear rationale and trigger conditions.

### Recommendation

**Start Phase 1 immediately.** It is low-risk, high-reward work that improves type safety, IntelliSense support, and eliminates boxing overhead. Each file can be done independently and tested in isolation.

**Phase 2 should be batched with the next major version release** to amortize the breaking change cost. It should NOT be done incrementally — consumers absorb all public API changes at once.

**Phase 3 items should remain tracked but unscheduled.** They are intentional designs serving the simulation engine's flexibility requirements. Revisit only when underlying system is being modernized (persistence layer, execution model redesign, etc.).
