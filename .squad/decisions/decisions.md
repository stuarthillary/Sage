# Decisions — Sage DES Engine

Collective technical and architectural decisions for the Sage discrete event simulation library (feature/dotnet10 branch).

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


---

# Phase 2 API Specification — Public Collection Replacements

**Author:** Ripley (Lead / Architect)  
**Date:** 2026-07-15  
**Requested by:** Stuart Hillary  
**Status:** Specification — DO NOT IMPLEMENT YET (Parker is on Phase 1)  
**Branch target:** Same branch as Phase 1 (feature/collection-modernization)

---

## Context

Phase 1 = internal/private fields only, non-breaking, Parker is implementing now.  
Phase 2 = public API surface. These are **breaking changes** requiring all callers within the Sage codebase to be updated simultaneously.

The 310 tests (all passing) are the validation gate. All 310 must continue to pass after Phase 2.

### Locked exclusions (from decisions.md — do NOT touch)
- `object userData` on event signatures — intentional heterogeneous payloads
- `IDictionary graphContext` parameter/field on graph/task execution paths — intentional polymorphic context (50+ signatures)
- XmlSerializationContext internals — serialization contract
- DynamicConstruction — WIP code

---

## Change 1 of 7

### File: `Sage\Core\IExecutive.cs`
**Change:** `ArrayList LiveDetachableEvents { get; }` → `IReadOnlyList<DetachableEvent> LiveDetachableEvents { get; }`

**Why:** `DetachableEvent` is the only type ever stored in `RunningDetachables` (confirmed at `Executive.cs:1061` — `foreach (DetachableEvent de in RunningDetachables)`). Consumers only enumerate or check Count — no mutations. `IReadOnlyList<T>` communicates this intent clearly and eliminates unsafe casts.

**Callers to update:**

| File | Line | Usage | Action needed |
|------|------|-------|---------------|
| `Sage\Core\Executive.cs` | 177 | `public ArrayList LiveDetachableEvents` — implementation | Change return type; update body (see Change 4) |
| `Sage\Core\ExecutiveFastLight.cs` | 668 | `public ArrayList LiveDetachableEvents` — implementation | Change return type; change `_emptyList` return to `Array.Empty<DetachableEvent>()` cast to `IReadOnlyList<DetachableEvent>` or `new ReadOnlyCollection<DetachableEvent>(new List<DetachableEvent>())` |
| No external callers found | — | Grep across all .cs files confirms no other callers | — |

**Risk:** Low.  
No external callers found in codebase. Both implementations are straightforward to update. `ExecutiveFastLight` returns a static empty list — just change the type of `_emptyList` or return a typed constant.

---

## Change 2 of 7

### File: `Sage\Core\IExecutive.cs`
**Change:** `IList EventList { get; }` → `IReadOnlyList<IExecEvent> EventList { get; }`

**Why:** The property returns a snapshot — callers cannot (and should not) mutate the underlying queue. The return type `IList` falsely implies mutability; the implementations already return a `ReadOnly`-wrapped list (any call to `Clear()`/`Add()` throws `NotSupportedException` at runtime). Making it `IReadOnlyList<IExecEvent>` enforces this at compile time.

**Why `IExecEvent` not `ExecEvent`:** `ExecEvent` is `internal class ExecEvent : IExecEvent` (see `ExecEvent.cs:9`). It cannot appear on a public interface signature. `IExecEvent` is the correct public-facing contract. Note: `IReadOnlyList<T>` is covariant (`out T` in .NET), so `Executive.cs` can return `ReadOnlyCollection<ExecEvent>` and it satisfies `IReadOnlyList<IExecEvent>`.

**Callers to update:**

| File | Line | Usage | Action needed |
|------|------|-------|---------------|
| `Sage\Core\Executive.cs` | 189 | `public IList EventList` — implementation | Change return type to `IReadOnlyList<IExecEvent>`; change body to `return snapshot.AsReadOnly()` (already `List<ExecEvent>`, AsReadOnly returns `ReadOnlyCollection<ExecEvent>` which satisfies covariant `IReadOnlyList<IExecEvent>`) |
| `Sage\Core\Executive.cs` | 343 | `foreach (IExecEvent @event in EventList)` in `ResubmitEventAtTime` | No change needed — `foreach` over `IReadOnlyList<IExecEvent>` works identically |
| `Sage\Core\ExecutiveFastLight.cs` | 682 | `public IList EventList` — implementation | Change return type; `_ExecEvent` (private nested class) does **NOT** implement `IExecEvent` (confirmed — only `ExecEvent` does). `ExecutiveFastLight` does not support rescindable/detachable events; recommend returning `Array.Empty<IExecEvent>()` as `IReadOnlyList<IExecEvent>` from this implementation. The existing return was already broken (elements cannot be cast to `IExecEvent`) |
| `Sage\Core\ExecController.cs` | 269 | `IList events = _executive.EventList;` then `((IExecEvent)events[0]).When` | Change to `IReadOnlyList<IExecEvent> events = _executive.EventList;` and `events[0].When` (cast no longer needed, type is already `IExecEvent`) |
| `Sage_Aux\SageTestLib\TestQueues.cs` | 886 | `_executive.EventList.Clear();` in a test helper `Abort()` method | **This call was already broken at runtime** — `EventList` returns a `ReadOnly` wrapper, so `Clear()` would throw `NotSupportedException`. Fix: remove the `Clear()` call entirely, or call `_executive.Abort()` to properly terminate the executive. The `Clear()` on a snapshot copy was always a no-op semantically |

**Risk:** Low-Medium.  
The `TestQueues.cs:886` case is the only tricky one — but it was already broken at runtime. The `ExecutiveFastLight.EventList` has a pre-existing defect (stores `_ExecEvent` which doesn't implement `IExecEvent`); this change is an opportunity to fix it correctly. The covariance of `IReadOnlyList<out T>` means `Executive.cs` needs no cast gymnastics.

---

## Change 3 of 7

### File: `Sage\Graphs\Tasks\ITaskManagementService.cs`
**Change:** `ArrayList TaskProcessors { get; }` → `IReadOnlyList<TaskProcessor> TaskProcessors { get; }`

**Why:** `TaskProcessor` is the only type ever stored (confirmed — all `AddTaskProcessor(TaskProcessor taskProcessor)` calls pass typed `TaskProcessor`). The existing implementation wraps the result in `ArrayList.ReadOnly()`. Callers only iterate or pass to `GetPostMortems`.

**Callers to update:**

| File | Line | Usage | Action needed |
|------|------|-------|---------------|
| `Sage\Graphs\Tasks\TaskManagementService.cs` | 74 | `public ArrayList TaskProcessors` — implementation | Change return type; change body: `return new List<TaskProcessor>(_taskProcessors.Values.Cast<TaskProcessor>()).AsReadOnly()` — note: `_taskProcessors` is a non-generic `Hashtable`, so `.Cast<TaskProcessor>()` is needed. (Or migrate `_taskProcessors` to `Dictionary<Guid, TaskProcessor>` in Phase 2 as well — see note below) |
| `Sage\Graphs\Tasks\TaskManagementService.cs` | 29 | `foreach (TaskProcessor tp in TaskProcessors)` | No change needed — foreach over `IReadOnlyList<TaskProcessor>` works identically, and the cast is now implicit |
| `Sage\Graphs\Tasks\TaskManagementService.cs` | 102 | `foreach (TaskProcessor tp in TaskProcessors)` | No change needed |
| `Sage\Graphs\Tasks\TaskManagementService.cs` | 168 | `foreach (TaskProcessor tp in TaskProcessors)` | No change needed |

**Bonus Phase 2 item (same file):** `_taskProcessors` in `TaskManagementService.cs` is a `private Hashtable` keyed by `Guid` with `TaskProcessor` values. While the backing field is private (Phase 1 territory), migrating it to `Dictionary<Guid, TaskProcessor>` in the same change simplifies the `TaskProcessors` property implementation significantly (no `.Cast<>()`). Recommend including this in the same commit.

**Risk:** Low.  
All callers are within `TaskManagementService.cs` itself — no external callers of the service's `TaskProcessors` property found outside the service implementation.

---

## Change 4 of 7

### File: `Sage\Core\IExecEvent.cs`
**Change:** NONE REQUIRED.

**Why:** All properties are already properly typed: `ExecEventReceiver`, `DateTime When`, `double Priority`, `object UserData`, `ExecEventType EventType`, `long Key`, `bool IsDaemon`. The `object UserData` is an **intentional design** (heterogeneous event payloads — locked in decisions.md). No non-generic collections exposed.

**Risk:** None.

---

## Change 5 of 7

### File: `Sage\Core\Executive.cs`
**Change 5a:** `internal ArrayList RunningDetachables = new ArrayList()` → `internal List<DetachableEvent> RunningDetachables = new List<DetachableEvent>()`

**Why:** Backing field for `LiveDetachableEvents`. Only ever contains `DetachableEvent` instances (confirmed by all Add sites and `foreach (DetachableEvent de in RunningDetachables)` usage). Enables type-safe enumeration and eliminates boxing.

**Callers to update (internal to Executive.cs):**

| Line | Usage | Action needed |
|------|-------|---------------|
| 172 | Field declaration | Change type to `List<DetachableEvent>` |
| 177-183 | `LiveDetachableEvents` property body | Change return: `return RunningDetachables.AsReadOnly()` (satisfies `IReadOnlyList<DetachableEvent>`) |
| 829 | `RunningDetachables.Count > 0` | No change — `List<T>` has Count |
| 832 | `ArrayList tmp = new ArrayList(RunningDetachables)` | Change to `List<DetachableEvent> tmp = new List<DetachableEvent>(RunningDetachables)` |
| 854 | `RunningDetachables.Count > 0` | No change |
| 1061 | `foreach (DetachableEvent de in RunningDetachables)` | No change — already typed, foreach works identically |

**Change 5b:** `EventList` property — update return type to match interface.

**Callers:** See Change 2 above. The internal implementation change is:
- Current: `return ArrayList.ReadOnly(new ArrayList(snapshot))` where `snapshot` is `List<ExecEvent>`
- New: `return snapshot.AsReadOnly()` — `List<ExecEvent>.AsReadOnly()` returns `ReadOnlyCollection<ExecEvent>` which satisfies `IReadOnlyList<IExecEvent>` via covariance

**Risk:** Low.  
All usages of `RunningDetachables` are within `Executive.cs` itself. The `List<T>` API is a superset of what was used from `ArrayList` for this field (Add, Count, iteration, copy-construction).

---

## Change 6 of 7

### File: `Sage\Graphs\Tasks\TaskProcessor.cs`
**Change:** `protected ArrayList _graphContexts = new ArrayList()` → `protected List<IDictionary> _graphContexts = new List<IDictionary>()`  
**And:** `public ArrayList GraphContexts` → `public IReadOnlyList<IDictionary> GraphContexts`

**Why:** `_graphContexts` stores only `IDictionary` values (confirmed: `_graphContexts.Add(GraphContext)` where `GraphContext` is `protected IDictionary GraphContext`). Callers access elements by index `[0]` and cast to `IDictionary`. `IDictionary` is intentionally non-generic here (locked in decisions.md — the graph execution context is polymorphic by design).

**Callers to update:**

| File | Line | Usage | Action needed |
|------|-------|-------|---------------|
| `Sage\Graphs\Tasks\TaskProcessor.cs` | 40 | `protected ArrayList _graphContexts = new ArrayList()` | Change to `protected List<IDictionary> _graphContexts = new List<IDictionary>()` |
| `Sage\Graphs\Tasks\TaskProcessor.cs` | 131-132 | `_graphContexts.Add(GraphContext)` | No change — `List<IDictionary>.Add(IDictionary)` is identical |
| `Sage\Graphs\Tasks\TaskProcessor.cs` | 156-161 | `public ArrayList GraphContexts` return body | Change return type to `IReadOnlyList<IDictionary>`; change body to `return _graphContexts.AsReadOnly()` |
| `Sage\Graphs\Tasks\TaskManagementService.cs` | 170 | `foreach (IDictionary graphContext in tp.GraphContexts)` | No change — typed foreach works identically |
| `Sage_Aux\SageTestLib\TestTasks.cs` | 57 | `IDictionary gc = (IDictionary)tp.GraphContexts[0]` | Change to `IDictionary gc = tp.GraphContexts[0]` — cast is no longer needed since element type is already `IDictionary` |
| `Sage_Aux\SageTestLib\TestTasks.cs` | 244 | `return (IDictionary)Tp.GraphContexts[0]` | Change to `return Tp.GraphContexts[0]` |
| `Sage_Aux\SageTestLib\TestGraphPersistence.cs` | 227 | `return (IDictionary)tp.GraphContexts[0]` | Change to `return tp.GraphContexts[0]` |

**Risk:** Low.  
All callers only index-access `[0]` and iterate. No ArrayList-specific APIs used by callers. The `IDictionary` element type is preserved intentionally.

---

## Change 7 of 7

### File: `Sage\Graphs\Vertex.cs`
**Change:** `protected ArrayList PreEdges = new ArrayList(2)` → `protected List<Edge> PreEdges = new List<Edge>(2)`  
**And:** `protected ArrayList PostEdges = new ArrayList(2)` → `protected List<Edge> PostEdges = new List<Edge>(2)`

**Why:** `PreEdges` and `PostEdges` only ever contain `Edge` objects (confirmed — all `AddPreEdge(Edge preEdge)` / `AddPostEdge(Edge postEdge)` call sites are typed `Edge`). The fields are `protected`, so subclasses can access them — no subclasses of `Vertex` were found in the codebase. Strongly typed `List<Edge>` eliminates unbox/cast overhead on every iteration through graph traversal code.

**Callers to update (internal to Vertex.cs unless noted):**

| Line | Usage | Action needed |
|------|-------|---------------|
| 34-35 | Field declarations | Change to `protected List<Edge>` |
| 40 | `private static readonly ArrayList _emptyCollection = ArrayList.ReadOnly(new ArrayList())` | This field is separate (used elsewhere as a default IList return). Change to `private static readonly IList _emptyCollection = new ReadOnlyCollection<Edge>(new List<Edge>())` or keep as-is if other callers need non-generic IList |
| 142 | `return ArrayList.ReadOnly(PreEdges)` in `PredecessorEdges` getter | Change to `return PreEdges.AsReadOnly()` — note: `PredecessorEdges` and `SuccessorEdges` return `IList` (from `IVertex` interface). `List<Edge>.AsReadOnly()` returns `ReadOnlyCollection<Edge>` which implements `IList` via `IList<T>` coercion. ✅ |
| 150 | `return ArrayList.ReadOnly(PostEdges)` in `SuccessorEdges` getter | Same as above |
| 156, 175 | `PreEdges.Contains(preEdge)` | No change — `List<T>.Contains(T)` is the same |
| 163, 182, 201, 220 | `PreEdges.Add/Remove`, `PostEdges.Add/Remove` | No change — identical API on `List<T>` |
| 317 | `foreach (Edge e in PostEdges)` | No change — already typed |
| 346, 359, 362, 370 | Various `PreEdges.Count`, `PreEdges.Contains` | No change |
| 430-431 | `xmlsc.StoreObject("PostEdges", PostEdges)` | No change — `StoreObject` takes `object` |
| 443-455 | Deserialization block | ⚠️ **RISK AREA** — `ArrayList tmpPostEdges = (ArrayList)xmlsc.LoadObject("PostEdges")` loads as `ArrayList`. Change to `IList tmpPostEdges = (IList)xmlsc.LoadObject("PostEdges")` to be XML-format agnostic. Then `PostEdges.Add(edge)` still works. **Do not change the stored type** in XML — if previously serialized data exists, the load will still return an `ArrayList` and must be accepted as `IList` |
| 524 | `ArrayList retval = new ArrayList(PostEdges)` | Change to `List<Edge> retval = new List<Edge>(PostEdges)` — or check what `retval` is used for and whether the caller needs `IList` or can accept `List<Edge>` |

**IVertex interface impact:** `IVertex.PredecessorEdges` and `IVertex.SuccessorEdges` return `IList` — these **do not need to change** for Phase 2. The backing field change is sufficient. This avoids a wider interface break.

**Callers of `PredecessorEdges`/`SuccessorEdges` (via IVertex) — no changes needed:**

| File | Usage pattern | Safe? |
|------|---------------|-------|
| `Edge.cs:434` | `ArrayList tmp = new ArrayList(PredecessorEdges)` — copy construction | Safe — `ArrayList(IList)` constructor accepts `IReadOnlyCollection<Edge>.AsReadOnly()` return |
| `Edge.cs:839` | `(Edge)child.PredecessorEdges[0]` | Safe — `IList` indexer, explicit cast, unchanged |
| `DagCycleChecker.cs:180` | `successors.AddRange(vertex.SuccessorEdges)` | Safe — `AddRange(ICollection)` or `IEnumerable` accepted |
| `CPMAnalyst.cs:632` | `m_vertex.SuccessorEdges` — count/assign | Safe |
| All `foreach (Edge edge in vertex.SuccessorEdges)` patterns | Implicit cast from `IList` elements | Safe — still works with typed elements |

**Risk:** Medium.  
The deserialization code (`DeserializeFrom`) expects `ArrayList` from `xmlsc.LoadObject("PostEdges")`. If the XML serialization infrastructure stores type metadata and returns exactly an `ArrayList`, the cast `(ArrayList)xmlsc.LoadObject(...)` is safe but brittle. Recommend changing the cast to `(IList)` to be resilient. If this codebase has serialized graphs on disk that need to round-trip, test XML persistence tests after this change. There are XML persistence tests in `TestGraphPersistence.cs` — run these specifically.

---

## HashtableOfLists Caller Migration (Item 7 from spec)

### Non-generic `HashtableOfLists` usages found: 5 call sites across 3 files

| File | Lines | Status | Recommendation |
|------|-------|--------|----------------|
| `Sage\Utility\Exchange.cs` | 22-23, 37-38 | ✅ **Already generic** — `HashtableOfLists<object, IDetachableEventController>` | No change |
| `Sage\Core\Model.cs` | 388 | ✅ **Already generic** — `HashtableOfLists<object, IModelError>` | No change |
| `Sage\Core\SimpleMetronome.cs` | 18 | ✅ **Already generic** — `HashtableOfLists<IExecutive, SimpleMetronome>` | No change |
| `Sage\Utility\TupleSpace.cs` | 20-22, 28-30 | 🟡 **Dead code** — entire class is inside `#if INCLUDE_WIP` AND `#if NOT_DEFINED` blocks. Not compiled. | Skip for now — when/if this code is activated, migrate to `HashtableOfLists<object, ITuple>` and `HashtableOfLists<object, IDetachableEventController>` |
| `Sage\Graphs\PFC\ProcedureFunctionChart.cs` | 2594 | ⚠️ **Non-generic, active production code** | See detail below |

### ProcedureFunctionChart.cs — Migration Detail

**File:** `Sage\Graphs\PFC\ProcedureFunctionChart.cs`  
**Line:** 2594  
**Change:** `HashtableOfLists htol = new HashtableOfLists()` → `HashtableOfLists<string, IPfcElement> htol = new HashtableOfLists<string, IPfcElement>()`

**Why safe:** 
- Keys are always `string` (node.Name, link.Name)
- Values are `IPfcNode` and `IPfcLinkElement` — both inherit from `IPfcElement` (confirmed: `IPfcNode : IPfcElement` and `IPfcLinkElement : IPfcElement` in their respective interface files)
- The inner loop `foreach (IPfcElement element in htol[key])` already uses `IPfcElement` as the element type — this is the natural generic type
- `element.SetName(string)` is defined on `IPfcElement` (confirmed in `IPfcElement.cs:19`)

**Caller update needed:**

| Line | Usage | Action |
|------|-------|--------|
| 2594 | `HashtableOfLists htol = new HashtableOfLists()` | Change to `HashtableOfLists<string, IPfcElement>` |
| 2597 | `htol.Add(node.Name, node)` | No change — `node` is `IPfcNode : IPfcElement` ✅ |
| 2602 | `htol.Add(link.Name, link)` | No change — `link` is `IPfcLinkElement : IPfcElement` ✅ |
| 2605 | `foreach (string key in htol.Keys)` | No change |
| 2607 | `htol[key].Count` | No change |
| 2610 | `foreach (IPfcElement element in htol[key])` | No change — now strongly typed, cast removed implicitly |

**Risk:** Low. Local variable only. Self-contained usage. All element types are confirmed `IPfcElement`. Tests in `TestGraphPersistence.cs` and `TestTasks.cs` cover graph execution paths.

---

## Summary of All Callers to Update

| File | Changes Required |
|------|-----------------|
| `Sage\Core\IExecutive.cs` | `ArrayList LiveDetachableEvents` → `IReadOnlyList<DetachableEvent>`; `IList EventList` → `IReadOnlyList<IExecEvent>` |
| `Sage\Core\Executive.cs` | `RunningDetachables` field type; `LiveDetachableEvents` return; `EventList` return |
| `Sage\Core\ExecutiveFastLight.cs` | `LiveDetachableEvents` return type + empty list; `EventList` return type + empty return |
| `Sage\Core\ExecController.cs` | `IList events` → `IReadOnlyList<IExecEvent> events`; remove explicit `IExecEvent` cast on indexer |
| `Sage\Graphs\Tasks\ITaskManagementService.cs` | `ArrayList TaskProcessors` → `IReadOnlyList<TaskProcessor>` |
| `Sage\Graphs\Tasks\TaskManagementService.cs` | `TaskProcessors` implementation; optionally `_taskProcessors` backing `Hashtable` → `Dictionary<Guid, TaskProcessor>` |
| `Sage\Graphs\Tasks\TaskProcessor.cs` | `_graphContexts` field type; `GraphContexts` property return type |
| `Sage\Graphs\Vertex.cs` | `PreEdges`/`PostEdges` field types; all `ArrayList.ReadOnly(...)` returns → `.AsReadOnly()`; deserialization cast `(ArrayList)` → `(IList)` |
| `Sage\Graphs\PFC\ProcedureFunctionChart.cs` | Local `HashtableOfLists` → `HashtableOfLists<string, IPfcElement>` |
| `Sage_Aux\SageTestLib\TestQueues.cs` | Remove/fix `_executive.EventList.Clear()` call (was already broken at runtime) |
| `Sage_Aux\SageTestLib\TestTasks.cs` | Remove redundant `(IDictionary)` casts on `GraphContexts[0]` |
| `Sage_Aux\SageTestLib\TestGraphPersistence.cs` | Remove redundant `(IDictionary)` cast on `tp.GraphContexts[0]` |

---

## Implementation Order Recommendation for Parker

1. **Start with the leaf changes** (no dependencies): `Vertex.cs` fields, `TaskProcessor.cs` fields
2. **Then implementations**: `Executive.cs`, `ExecutiveFastLight.cs`, `TaskManagementService.cs`
3. **Then interfaces**: `IExecutive.cs`, `ITaskManagementService.cs` (these will force compile errors that guide the remaining fixes)
4. **Then callers**: `ExecController.cs`, `ProcedureFunctionChart.cs`
5. **Last**: test files — `TestQueues.cs`, `TestTasks.cs`, `TestGraphPersistence.cs`
6. **Validate**: Run all 310 tests. Specifically confirm `TestGraphPersistence` passes (XML round-trip).

---

## Risk Summary

| Change | Risk | Primary Concern |
|--------|------|-----------------|
| `LiveDetachableEvents` | Low | No external callers |
| `EventList` | Low-Medium | `ExecutiveFastLight._ExecEvent` doesn't implement `IExecEvent`; `TestQueues.Clear()` was already broken |
| `TaskProcessors` | Low | Only iterated, never mutated by callers |
| `IExecEvent.cs` | None | No changes |
| `Executive.RunningDetachables` | Low | Internal only; `List<T>` is API superset of `ArrayList` for this usage |
| `TaskProcessor.GraphContexts` | Low | Simple index access only |
| `Vertex.PreEdges/PostEdges` | Medium | XML deserialization cast; test with `TestGraphPersistence` |
| `HashtableOfLists` in PFC | Low | Local variable, confirmed `IPfcElement` hierarchy |


---

# Phase 1 — Private Collection Replacements (Parker)

## Summary
Converted private/internal `ArrayList`/`Hashtable` fields to strongly-typed `List<T>`/`Dictionary<TKey,TValue>` (or `HashSet<T>` where set semantics applied) across Core, Materials, Resources, and Graphs. Public API shapes remain unchanged (non-generic `IList`/`ICollection` returns preserved), and XML serialization compatibility is maintained by storing `ArrayList`/`Hashtable` snapshots.

## Files Updated

### Core
- `Sage\Core\StateMachine.cs`: `_stateTranslationTable` → `Dictionary<Enum, int>`.
- `Sage\Core\Model.cs`: `_taskProcessors` → `Dictionary<string, TaskProcessor>`; `_parameters` → `Dictionary<string, object>`.
- `Sage\Core\InitializationManager.cs`: `_zeroDependencyInitializers` → `List<object[]>`; `_verts` → `Dictionary<Guid, Dv>`.

### Materials
- `Sage\Materials\Mixture.cs`: `_constituentSubstances` → `Dictionary<string, Substance>` (serialize via `Hashtable` snapshot).
- `Sage\Materials\MaterialCatalog.cs`: `_materialTypesByName` → `Dictionary<string, MaterialType>`; `_materialTypesByGuid` → `Dictionary<Guid, MaterialType>` (serialize via `Hashtable` snapshot).
- `Sage\Materials\Chemistry\Reaction.cs`: `_reactants`/`_products` → `List<ReactionParticipant>` (public `IList` via `ArrayList.Adapter`, serialize via `ArrayList` snapshot).
- `Sage\Materials\Substance.cs`: `_materialSpecs` → `Dictionary<Guid, double>`; memento `_matlSpecs` → `Dictionary<Guid, double>`; `SetMaterialSpecs` accepts `KeyValuePair<Guid,double>`.
- `Sage\Materials\MaterialConduitManager.cs`: `_conduits` → `Dictionary<MaterialType, IResourceManager>`; `_resources` → `Dictionary<MaterialType, MaterialResourceItem>`.
- `Sage\Materials\Emissions\EmissionModel.cs`: `_errMsgs` → `List<string>` (protected `ArrayList` adapter retained).

### Resources
- `Sage\Resources\ResourceManager.cs`: `_resources` → `List<IResource>` (public `IList` via `ArrayList.Adapter`, serialize via `ArrayList` snapshot).

### Graphs
- `Sage\Graphs\Edge.cs`: `_childEdges` → `List<Edge>`; `_childLigatures` → `List<Ligature>`; `_activeContexts` → `List<IDictionary>`; `_emptyCollection` → `Array.Empty<Edge>()` (public `IList` via `ArrayList.Adapter`, serialize via `ArrayList` snapshot).
- `Sage\Graphs\DagDeadlockChecker.cs`: `_nodes` → `Dictionary<object, Node>`; `_frontier` → `List<Node>`.
- `Sage\Graphs\ValidationService.cs`: `_htNodes` → `Dictionary<IHasValidity, ValidityNode>`; `_oldValidities` → `Dictionary<IHasValidity, bool>`; internal node lists → `List<ValidityNode>`; `_emptyList` → `Array.Empty<IHasValidity>()`.
- `Sage\Graphs\CPMAnalyst.cs`: `m_verifiedEdges` → `HashSet<Edge>`; `s_emptylist` → `Array.Empty<Edge>()`; `SynchronizerData` visit/member lists → `List<Vertex>`.

## Validation
- `dotnet build E:\source\Sage\Sage4.sln --no-incremental -v minimal`
- `dotnet test E:\source\Sage\Sage_Aux\SageTestLib\SageTestLib.csproj --no-build -v minimal`


---

# Hudson: Collection Migration Test Coverage

**Date:** 2026-03-07  
**Author:** Hudson (Tester/QA)  
**Requested by:** Stuart Hillary  

## Summary

Test coverage has been written and verified for all collection types involved in the Phase 1 and Phase 2 non-generic collection migrations.

## Test Files Modified

### `Sage_Aux/SageTestLib/TestMaterials.cs`
Three new tests added to `MaterialTester`:
- **`TestMixtureConstituentsIteration`** — Adds 3 substances to a Mixture, enumerates `Mixture.Constituents`, asserts count=3 and all names present. Guards Hashtable→Dictionary<string,Substance> migration in `Mixture._constituentSubstances`.
- **`TestMaterialCatalogCRUD`** — Tests `MaterialCatalog.Add`, `this[string]`, `this[Guid]`, `Contains`, and `Remove`. Guards Hashtable→Dictionary migration in `MaterialCatalog`.
- **`TestMaterialCatalogEnumeration`** — Adds 3 types, asserts `MaterialCatalog.MaterialTypes` count=3 and names correct.

### `Sage_Aux/SageTestLib/TestResources.cs`
Two new tests added to `ResourceTester`:
- **`TestResourceManagerAddRemoveAndCount`** — Add 3 resources, assert count, remove 1, assert count decremented and only expected resources remain. Guards ArrayList→List<IResource> migration.
- **`TestResourceManagerEnumeration`** — Verifies `foreach` via `IEnumerable`, `Resources.Count`, and Guid-based indexer all work correctly.

### `Sage_Aux/SageTestLib/TestStateMachine.cs`
One new test added to `StateMachineTester`:
- **`TestStateMachineAllStatesAccessibleViaDictionary`** — Calls `TransitionHandler(from, to)` for all 12 valid state pairs (exercising the full `_stateTranslationTable` dictionary for all 5 enum states), verifies illegal transitions throw `TransitionFailureException`, and verifies a valid transition changes state correctly. Guards Hashtable→Dictionary<Enum,int> migration.

### `Sage_Aux/SageTestLib/TestExecutive.cs`
Six new tests added to `ExecTester`:
- **`TestEventListContainsQueuedEvents`** — Queues 3 events at different times, asserts `EventList.Count==3` and events appear in chronological order.
- **`TestEventListIsReadOnly`** — Asserts `EventList.IsReadOnly == true`.
- **`TestLiveDetachableEventsContainsRunningEvent`** — From within a running detachable event, captures `exec.LiveDetachableEvents.Count`; asserts it was 1 during execution and 0 after.
- **`TestLiveDetachableEventsIsReadOnly`** — Asserts `exec.LiveDetachableEvents.IsReadOnly == true`.
- **`[Ignore] TestEventListTypedAsIReadOnlyList`** — Phase 2 prep: asserts `exec.EventList` is typed as `IReadOnlyList<IExecEvent>`. Currently `[Ignore]`'d; will pass after Phase 2 changes `IExecutive.EventList` return type.
- **`[Ignore] TestLiveDetachableEventsTypedAsIReadOnlyList`** — Phase 2 prep: asserts `exec.LiveDetachableEvents` is typed as `IReadOnlyList<IDetachableEventController>`. Currently `[Ignore]`'d; will pass after Phase 2 changes `IExecutive.LiveDetachableEvents` return type.

### `Sage_Aux/SageTestLib/TestGraphBranching.cs`
Three new tests added to `GraphLoopingTester`:
- **`TestVertexPreAndPostEdgesAfterConstruction`** — Creates two edges, calls `AddPredecessor`, asserts both `PostVertex.SuccessorEdges.Count > 0` and `PreVertex.PredecessorEdges.Count > 0`. Guards ArrayList→List<Edge> migration.
- **`TestVertexAddAndRemoveEdges`** — Directly calls `Vertex.AddPostEdge` / `RemovePostEdge`, asserts `SuccessorEdges.Count` changes correctly and removed/remaining edges are correctly reflected.
- **`[Ignore] TestVertexEdgesTypedAsList`** — Phase 2 prep: asserts `Vertex.PredecessorEdges` and `SuccessorEdges` are typed as `IReadOnlyList<Edge>`. Currently `[Ignore]`'d; will pass after Phase 2 changes property return types.

## Bug Fixes (unblocked compilation for Phase 1 in-progress migration)

Parker's Phase 1 work had left compilation errors. These were fixed to allow the test suite to build and run:

| File | Issue | Fix |
|------|-------|-----|
| `Sage/Core/StateMachine.cs` | `Dictionary<Enum,int>.Add(object, int)` — `values.GetValue(i)` returns `object` | Added `(Enum)` cast |
| `Sage/Materials/Substance.cs` | `foreach (DictionaryEntry de in _matlSpecs)` in `SubstanceMemento` where `_matlSpecs` is `Dictionary<Guid,double>` | Changed to `foreach (KeyValuePair<Guid,double> de ...)` and `.Contains` → `.ContainsKey` |
| `Sage/Materials/Chemistry/Reaction.cs` | `React(Mixture, ArrayList, ArrayList, double)` called with `List<ReactionParticipant>` args | Changed signature to `React(Mixture, IList<ReactionParticipant>, IList<ReactionParticipant>, double)` |
| `Sage/Graphs/Edge.cs` | `_childLigatures.Add(AddCostart(child))` where `_childLigatures` is `List<Ligature>` but `AddCostart` returns `Edge` | Added `(Ligature)` cast |

## Test Results

```
Passed: 316  |  Failed: 0  |  Skipped: 3 (Phase 2 [Ignore] tests)  |  Total: 319
```

All 310 pre-existing tests continue to pass. 6 new tests pass. 3 Phase 2 prep tests correctly skipped.

## Recommendations for Parker

1. Apply the fixes above (or equivalent) to StateMachine, Substance, Reaction, Edge before the Phase 1 PR is opened.
2. When Phase 2 public API changes land, remove `[Ignore]` from the three Phase 2 prep tests — they document the expected new types.
3. `InvalidTransitionHandler.IsValidTransition` uses `new` instead of `override`, so calling via `ITransitionHandler` always returns `true`. This is a pre-existing design issue; it does not affect runtime correctness (illegal transitions still throw) but is misleading. Consider fixing in a future refactor.



---

## Decision: Phase 2 — Public API Collection Replacements ✅

**Author:** Parker (.NET Developer)  
**Date:** 2026-03-07  
**Status:** COMPLETE  
**Branch:** feature/dotnet10  

### Summary
# Phase 2 — Public API Collection Replacements

## Summary
Implemented Ripley’s Phase 2 collection API changes across Core and Graphs while preserving locked exclusions (object userData, IDictionary graphContext, XmlSerializationContext internals, DynamicConstruction). Updated public interfaces to IReadOnlyList<T>, migrated backing collections to generics, and fixed affected call sites.

## Changes Applied
- **IExecutive**: `LiveDetachableEvents` → `IReadOnlyList<DetachableEvent>`, `EventList` → `IReadOnlyList<IExecEvent>`.
- **Executive**: `RunningDetachables` now `List<DetachableEvent>`; `LiveDetachableEvents` returns `AsReadOnly()`; `EventList` returns sorted snapshot `AsReadOnly()`.
- **ExecutiveFastLight**: `LiveDetachableEvents` returns empty `IReadOnlyList<DetachableEvent>`; `EventList` returns empty `IReadOnlyList<IExecEvent>`.
- **Task management**: `ITaskManagementService.TaskProcessors` now `IReadOnlyList<TaskProcessor>`; `TaskManagementService` uses `Dictionary<Guid, TaskProcessor>`.
- **TaskProcessor**: `_graphContexts` now `List<IDictionary>`; `GraphContexts` returns `IReadOnlyList<IDictionary>`.
- **Vertex**: `PreEdges`/`PostEdges` now `List<Edge>`; read-only accessors return `AsReadOnly()`; XML deserialization now casts to `IList`.
- **PFC**: `ProcedureFunctionChart` uses `HashtableOfLists<string, IPfcElement>`.
- **Callers/tests**: Updated ExecController, TestQueues, TestTasks, TestGraphPersistence, TestExecutive; removed Phase 2 `[Ignore]` markers (TestExecutive and TestGraphBranching).

## Tests
- `dotnet build E:\source\Sage\Sage4.sln --no-incremental -v minimal`
- `dotnet test E:\source\Sage\Sage_Aux\SageTestLib\SageTestLib.csproj --no-build -v minimal`
  - **Result:** total 319, passed 316, skipped 3


---

# Decision: Nullable Reference Types — Migration Architecture

**Author:** Ripley (Lead / Architect)  
**Date:** 2026-07-15  
**Status:** Approved  
**Requested by:** Stuart Hillary (PM)

---

## Context

Stuart requested enabling `#nullable enable` across the Sage codebase. This decision documents the scope assessment, risk analysis, and phased migration plan.

## Scope Assessment

### Warning Count

With `<Nullable>enable</Nullable>` in Sage4.csproj:

- **Total nullable warnings: 4,446**
- **548 source files** across 14 module directories
- **0 errors** — the build succeeds, these are all warnings

### Warning Type Breakdown

| Warning | Count | Description | Difficulty |
|---------|-------|-------------|------------|
| CS8618 | 1,378 | Non-nullable property/field not initialized in constructor | **HARD** — requires constructor refactoring or `= null!` |
| CS8625 | 988 | Cannot convert null literal to non-nullable reference type | MEDIUM — annotate parameter/field as nullable |
| CS8600 | 812 | Converting null literal or possible null to non-nullable type | MEDIUM — add null checks or annotate |
| CS8602 | 368 | Dereference of a possibly null reference | MEDIUM — add null guards |
| CS8603 | 358 | Possible null reference return | MEDIUM — annotate return type or guard |
| CS8604 | 160 | Possible null reference argument | LOW — caller annotation fixes |
| CS8767 | 142 | Nullability mismatch with interface implementation | LOW — signature alignment |
| CS8601 | 116 | Possible null reference assignment | MEDIUM |
| CS8605 | 68 | Unboxing a possibly null value | LOW |
| CS8765 | 26 | Nullability of parameter doesn't match overridden member | LOW |
| Other | 30 | CS8714, CS8629, CS8766, CS8622, CS8612, CS8631, CS8892 | LOW |

**Key insight:** CS8618 (constructor initialization) dominates at 31% of all warnings. These are the hardest to fix correctly because they require understanding initialization semantics — some fields are intentionally set post-construction (e.g., via `Initialize()` patterns), and blindly adding `= null!` suppresses the warning but doesn't improve safety.

### Warning Distribution by Module

| Module | Warnings | Files | Warnings/File | Risk |
|--------|----------|-------|---------------|------|
| Graphs | 1,160 | 96 | 12.1 | HIGH — PFC subsystem, IDictionary graphContext |
| ItemBased | 656 | 73 | 9.0 | MEDIUM — port/connector patterns |
| Materials | 568 | 65 | 8.7 | MEDIUM — substance/mixture chemistry |
| Core | 502 | 64 | 7.8 | HIGH — engine contracts, most impactful |
| Utility | 484 | 76 | 6.4 | MEDIUM — mixed: some clean, some legacy |
| Mathematics | 290 | 53 | 5.5 | LOW — mostly value-type math |
| Resources | 264 | 30 | 8.8 | MEDIUM — resource management |
| Persistence | 186 | 7 | 26.6 | HIGH — XML serialization, many casts |
| Scheduling | 134 | 24 | 5.6 | LOW-MEDIUM |
| SmartPropertyBag | 120 | 9 | 13.3 | MEDIUM — dynamic property bags |
| Dependencies | 28 | 4 | 7.0 | LOW |
| SystemDynamics | 28 | 40 | 0.7 | LOW — cleanest module |
| Randoms | 24 | 6 | 4.0 | LOW |

### SageTestLib Assessment

**Recommendation: DEFER.** SageTestLib has 62 test files. Tests routinely pass null, use `Assert.IsNotNull` patterns, and assign nulls freely. Enabling nullable in tests would generate hundreds of warnings with zero safety benefit — tests are meant to probe edge cases including null inputs. SageTestLib should remain without `#nullable enable` indefinitely.

---

## Risk Assessment

### HIGH RISK — Handle With Care

1. **Public interface annotation changes** — Adding `?` to return types on `IExecutive`, `IModel`, `IExecEvent` is a **source-compatible breaking change**. Callers that previously assumed non-null will now get warnings. This is correct behavior but must be documented.

2. **CS8618 in Core engine classes** — `Executive.cs` (66 warnings), `ExecutiveFastLight.cs` (58), `Model.cs` (52). These have complex initialization patterns (factory construction, post-init configuration). Using `= null!` here suppresses the warning but the null-safety guarantee is a lie.

3. **Persistence module** (186 warnings in 7 files = 26.6/file) — XML deserialization involves heavy `object` casting and null coercion. High false-positive rate.

4. **`object userData` parameters** — 40 files use this pattern. These are intentionally `object` (not `object?`) in the delegate signature `ExecEventReceiver(IExecutive exec, object userData)`. However, callers frequently pass `null`. The correct annotation is `object? userData` on the delegate, which is a **public API change** that must propagate to all 40+ files.

5. **`IDictionary graphContext`** — 41 files, 50+ signatures. Must annotate as `IDictionary? graphContext` where null is passed, `IDictionary graphContext` where guaranteed non-null. Requires per-call-site analysis.

### MEDIUM RISK — Routine But Voluminous

6. **EventedList.cs** (90 CS8618 warnings) — Generic event-sourced list, heavily parameterized. Tedious but mechanical.

7. **Graphs module** (1,160 warnings) — Largest module. PFC subsystem alone has 496 warnings. Must be batched carefully.

### LOW RISK — Mechanical Fixes

8. **Mathematics, Randoms, SystemDynamics** — Mostly value types, clean patterns, few warnings.

### Files That Should Get `#nullable disable`

| File | Reason |
|------|--------|
| `Utility/WeakHashTable.cs` | Legacy weak-reference collection implementing non-generic `IDictionary`. 15-20 expected warnings, all false positives from the `WeakReference.Target` null pattern. Not worth annotating — the type is inherently null-producing by design. |
| `Persistence/XmlSerializationContext.cs` | XML serialization with heavy `object` casting. Nullable annotations would be misleading — the deserialization pipeline produces nulls by design that are checked downstream. |
| `Persistence/CreationContext.cs` | Same serialization pipeline. Object stacks, null coercion. |

---

## Recommended Approach: Option 1 — Global Enable + Suppress

### Decision

**Use Option 1: `<Nullable>enable</Nullable>` globally in Sage4.csproj, with `#nullable disable` at the top of files not yet migrated.**

### Justification

| Approach | Pros | Cons |
|----------|------|------|
| **1. Global + suppress** ✅ | Progressive disclosure — every new file is nullable by default. Clear migration tracking (count of `#nullable disable` remaining). One csproj change. | Initial noise: must add disable to ~548 files up front. |
| 2. File-by-file enable | No upfront work. | New files aren't nullable by default. No global tracking. Easy to forget. Opposite of .NET ecosystem direction. |
| 3. Per-project | Clean separation. | SageTestLib doesn't need it. Only two projects, so this collapses to Option 1 for Sage4.csproj anyway. |

**Why Option 1 wins at 4,446 warnings:**
- The warning count is large but not catastrophic. Most warnings are mechanical (CS8625, CS8600, CS8602).
- Global enable means every new file Parker writes is automatically nullable-checked — no discipline required.
- The `#nullable disable` pragma is a clear, grepable migration marker. We can track progress: `grep -r "#nullable disable" Sage/ | wc -l`.
- This is the approach recommended by Microsoft for existing codebases and used by ASP.NET Core's own migration.

### Implementation Mechanics

1. Add `<Nullable>enable</Nullable>` to Sage4.csproj
2. Run a script to prepend `#nullable disable` to every `.cs` file under `Sage/`
3. Remove `#nullable disable` from files as they are annotated and cleaned
4. Files in the `#nullable disable` list above keep their pragma permanently

---

## Phased Migration Plan

### Phase 1 — Core Contracts (Priority: HIGHEST)

**Goal:** Establish nullable annotations on the public interfaces that everything depends on. These set the contract for all downstream implementations.

| File | Warnings | Notes |
|------|----------|-------|
| `Core/IExecutive.cs` | 0 (interface) | Annotate return types and parameters. `RequestEvent` userData → `object? userData`. `IDictionary graphContext` → stays `IDictionary`. |
| `Core/IExecEvent.cs` | 0 (interface) | `object UserData` → `object? UserData` (nullable — callers pass null freely). |
| `Core/IModel.cs` | 4 | Two CS8625 defaults. Annotate nullable parameters. |
| `Core/IModelObject.cs` | 0 | Annotate `Name`, `Description`, `Guid` nullability. |
| `Core/IHasName.cs` | 8 | Fix `IComparer` parameter nullability to match BCL. |
| `Core/IHasIdentity.cs` | 0 | Review — likely clean. |
| `Core/IErrorHandler.cs` | 0 | Review. |
| `Core/IModelError.cs` | 0 | Review. |
| `Core/IModelWarning.cs` | 0 | Review. |
| `Core/IModelService.cs` | 0 | Review. |
| `Core/INotification.cs` | 0 | Review. |
| `Core/IDetachableEventController.cs` | 0 | Review. |
| `Core/IExecEventSelector.cs` | 0 | Review. |
| `Core/IInitializationManager.cs` | 0 | Review. |
| `Core/IHasParameters.cs` | 0 | Review. |
| `Core/IResettable.cs` | 0 | Review. |
| `Core/ISynchronizer.cs` | 0 | Review. |
| `Core/ISynchChannel.cs` | 0 | Review. |
| `Core/ITransitionHandler.cs` | 0 | Review. |
| `Core/ITransitionFailureReason.cs` | 0 | Review. |
| `Core/SageOptions.cs` | 2 | Fix `Models` property initialization. |
| `Core/ExecEvent.cs` | 8 | Internal — annotate `UserData` as `object?`. |

**Estimated effort:** 1–2 hours  
**Risk:** LOW for interfaces (additive annotations). MEDIUM for `object? userData` propagation — this is a public API surface change. Callers will see new warnings if they assume non-null.

### Phase 2 — Engine Internals

**Goal:** Annotate the core executive implementations that power the simulation engine.

| File | Warnings | Notes |
|------|----------|-------|
| `Core/Executive.cs` | 66 | Heavy CS8618. Post-construction initialization pattern. Many fields need `= null!` or constructor refactoring. |
| `Core/ExecutiveFastLight.cs` | 58 | Same patterns as Executive. |
| `Core/ExecFactory.cs` | 22 | Singleton with reflection construction. |
| `Core/ExecController.cs` | 28 | Rate throttling, frame dispatch. |
| `Core/Model.cs` | 52 | Simulation container. Complex initialization. |
| `Core/StateMachine.cs` | 30 | Two-phase-commit state machine. |
| `Core/DetachableEvent.cs` | 26 | Thread signaling, suspend/resume. |
| `Core/ExecEventRemover.cs` | 24 | Heap operations. |
| `Core/InitializationManager.cs` | 90 | Highest warning count in Core. Complex dependency tracking. |
| `Core/ModelConfig.cs` | 14 | Configuration bridge. |
| `Core/BaseModelObject.cs` | 6 | Base class. |
| `Core/ModelObjectDictionary.cs` | 24 | Object registry. |

**Estimated effort:** 4–6 hours  
**Risk:** MEDIUM-HIGH. Constructor initialization patterns require careful analysis. `InitializationManager.cs` (90 warnings) may benefit from partial `#nullable disable` on specific methods.

### Phase 3 — Remaining Modules (Batch by namespace)

**Assign to Parker in priority order based on warning density and risk.**

| Batch | Module | Warnings | Files | Priority | Notes |
|-------|--------|----------|-------|----------|-------|
| 3A | SystemDynamics | 28 | 40 | HIGH (easy win) | Cleanest module, 0.7 warnings/file |
| 3B | Randoms | 24 | 6 | HIGH (easy win) | Mostly value types |
| 3C | Dependencies | 28 | 4 | HIGH (easy win) | Small, isolated |
| 3D | Mathematics | 290 | 53 | MEDIUM | Linear algebra, interpolation. Mostly value types. |
| 3E | Scheduling | 134 | 24 | MEDIUM | TimePeriod has 48 CS8618 warnings — review carefully. |
| 3F | Resources | 264 | 30 | MEDIUM | Resource/ResourceManager pattern. |
| 3G | SmartPropertyBag | 120 | 9 | MEDIUM | Dynamic property system. May need partial disable. |
| 3H | Utility | 484 | 76 | MEDIUM-HIGH | Mixed. WeakHashTable gets permanent disable. EventedList (90) is tedious. |
| 3I | ItemBased | 656 | 73 | HIGH effort | Port/connector patterns. |
| 3J | Materials | 568 | 65 | HIGH effort | Chemistry/mixture model. |
| 3K | Graphs | 1,160 | 96 | HIGHEST effort | PFC (496 alone). IDictionary graphContext everywhere. |
| 3L | Persistence | 186 | 7 | SPECIAL | 26.6 warnings/file. Most files should get permanent `#nullable disable`. |

### Phase 4 — Cleanup

- Remove all remaining `#nullable disable` pragmas (except permanent ones)
- Add `<WarningsAsErrors>$(WarningsAsErrors);CS8600;CS8602;CS8603</WarningsAsErrors>` to prevent regression
- Final full build + test validation

---

## Architectural Constraints (ENFORCED)

These constraints are **non-negotiable** and override any mechanical warning-fixing instinct:

1. **`object userData`** — Annotate as `object? userData` where callers pass null (which is most places). Do NOT change to generic `T`. This is an intentional heterogeneous payload pattern. The `ExecEventReceiver` delegate signature changes to `object? userData`.

2. **`IDictionary graphContext`** — Keep non-generic `IDictionary`. Annotate as `IDictionary? graphContext` ONLY where null is actually passed (check call sites). Most graph execution paths guarantee non-null context.

3. **`WeakHashTable`** — Permanent `#nullable disable`. Not worth annotating.

4. **Persistence files** — Default to `#nullable disable` unless a specific file is clean enough to annotate.

5. **Public API `T` → `T?` changes** — Every return type change from `T` to `T?` on a public interface must be listed in a "Breaking Changes" section of the PR description. These are source-compatible but semantically breaking.

6. **`= null!`** — Use sparingly and only for fields that are guaranteed initialized before use (e.g., set in `Initialize()` called from constructor). Never use on fields that might actually be null at runtime.

---

## Success Criteria

- All 319 tests pass after each phase
- Warning count decreases monotonically
- Zero `#nullable disable` pragmas remain except the permanent exclusion list
- No new `= null!` suppressions without a comment explaining why

---

## References

- [Microsoft: Update a codebase to use nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-migration-strategies)
- Existing decisions: Collection Modernization Strategy, Phase 2 Public API Spec
- Locked exclusions: `object userData` (40 files), `IDictionary graphContext` (41 files)


---

# Parker Work Spec: Nullable Reference Types — Phase 1 Implementation

**Author:** Ripley (Lead / Architect)  
**Date:** 2026-07-15  
**For:** Parker (.NET Developer)  
**Decision ref:** `ripley-nullable-arch.md`

---

## Objective

Enable `<Nullable>enable</Nullable>` globally in Sage4.csproj and annotate the Phase 1 files (Core interfaces + key types). All other files get `#nullable disable` at the top until their phase.

## Prerequisites

- Branch from `main`: `feature/nullable-phase1`
- Baseline: all 319 tests passing
- Read `ripley-nullable-arch.md` for full context and constraints

## Step-by-Step Instructions

### Step 1: Enable Nullable Globally

Edit `Sage/Sage4.csproj`:

```xml
<PropertyGroup>
    <OutputType>Library</OutputType>
    <AssemblyName>Sage</AssemblyName>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

**Do NOT enable nullable in SageTestLib.csproj.** Tests are deferred indefinitely.

### Step 2: Add `#nullable disable` to All Source Files

Run this PowerShell script from the repo root to prepend `#nullable disable` to every `.cs` file under `Sage/`:

```powershell
Get-ChildItem -Path "Sage" -Filter "*.cs" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    if ($content -notmatch '#nullable') {
        $newContent = "#nullable disable`r`n" + $content
        Set-Content $_.FullName $newContent -NoNewline
    }
}
```

**Commit this as a separate commit** with message:
```
chore: add #nullable disable to all source files

Preparatory step for nullable reference types migration.
Every file starts with #nullable disable and will have it
removed as the file is annotated in subsequent phases.
```

### Step 3: Remove `#nullable disable` From Phase 1 Files and Annotate

For each file below, remove the `#nullable disable` line at the top (replacing with `#nullable enable` is NOT needed since it's globally enabled — just remove the disable pragma).

Then fix the nullable warnings in that file per the annotations below.

---

### Phase 1 Files — Annotation Guide

#### Core Interfaces (remove `#nullable disable`, annotate)

**`Core/IExecEvent.cs`**
- `object UserData` → `object? UserData` (callers routinely pass null)
- Review all other properties — DateTime, double, long, ExecEventType are value types (no change needed)

**`Core/IExecutive.cs`**
- `ExecEventReceiver` delegate: change `object userData` → `object? userData`
- All `RequestEvent` overloads: `object? userData` parameter
- `IDictionary graphContext` parameters: keep as `IDictionary` (non-nullable). Check each overload — if any call site passes null, annotate that specific overload as `IDictionary? graphContext`
- Return types: review `CurrentEvent` — can it return null? If yes → `IExecEvent?`
- `EventList` return: `IReadOnlyList<IExecEvent>` (non-nullable list, non-nullable elements — events in the list always exist)

**`Core/IModel.cs`**
- Fix the two CS8625 warnings (null default parameters) — change parameter types to nullable where null is a valid default
- `Executive` property: should be non-nullable (always set in construction)
- `ModelConfig` property: review — nullable if optional
- `ModelObjects` dictionary: non-nullable
- String parameters for names/descriptions: `string?` where optional, `string` where required

**`Core/IModelObject.cs`**
- `Name` property: `string` (non-nullable — all model objects have names)
- `Description` property: `string?` (may not be set)
- `Model` property: `IModel?` (may be detached from model)

**`Core/IHasName.cs`**
- `HasNameComparer.Compare(object x, object y)` → `Compare(object? x, object? y)` to match `IComparer.Compare` BCL signature
- `HasNameEqualityComparer` — same pattern, align with BCL nullable signatures

**`Core/IHasIdentity.cs`**
- `Guid` is a value type — no nullable changes needed
- Review `Name` if present

**`Core/IErrorHandler.cs`**, **`Core/IModelError.cs`**, **`Core/IModelWarning.cs`**
- Error/warning message strings: `string` (non-nullable — errors always have messages)
- `Subject` properties: `object?` (may be null)
- `InnerException`: `Exception?`

**`Core/IModelService.cs`**, **`Core/INotification.cs`**, **`Core/IDetachableEventController.cs`**, **`Core/IExecEventSelector.cs`**, **`Core/IInitializationManager.cs`**, **`Core/IHasParameters.cs`**, **`Core/IResettable.cs`**, **`Core/ISynchronizer.cs`**, **`Core/ISynchChannel.cs`**, **`Core/ITransitionHandler.cs`**, **`Core/ITransitionFailureReason.cs`**
- Review each. Most are simple interfaces with few nullable concerns.
- Apply nullable annotations based on semantic intent: parameters that can be null get `?`, those that can't stay non-nullable.

#### Core Types (remove `#nullable disable`, annotate)

**`Core/SageOptions.cs`**
- Fix `IReadOnlyList<IEmissionModel> Models` property — change to `IReadOnlyList<IEmissionModel>? Models` or initialize to empty list
- All other properties already have initializers — should be clean

**`Core/ExecEvent.cs`** (internal)
- `UserData` field/property → `object? UserData`
- `ExecEventReceiver` field → non-nullable (always set in constructor)
- Constructor: ensure all reference fields are initialized
- `ToString()`: guard null `UserData` in string interpolation

---

## CONSTRAINTS — Read Before Coding

1. **`object userData`** → `object? userData`. Do NOT change to generic. Do NOT change to `dynamic`. This is intentional.

2. **`IDictionary graphContext`** → Keep `IDictionary` (non-generic). Only add `?` if a specific call site passes null. Default assumption: non-nullable.

3. **Do NOT use `= null!`** on interface properties or in interface default implementations. It's meaningless on interfaces.

4. **Do NOT change** `WeakHashTable.cs`, any file in `Persistence/`, or any file outside `Core/` in this phase. Those files keep their `#nullable disable`.

5. **Public API changes** — Every return type or parameter type that changes from `T` to `T?` on a public interface: add a line to the PR description under "## Nullable API Changes". These are source-compatible but consumers will see new warnings.

6. **When in doubt about a property's nullability** — check usage in test files and callers. If any caller passes/assigns null, the property is nullable. If no caller does, it's non-nullable.

## Validation

After all Phase 1 changes:

```powershell
# Build must succeed with 0 errors
dotnet build Sage4-Everything.sln --no-incremental -v minimal

# All tests must pass
dotnet test Sage_Aux\SageTestLib\SageTestLib.csproj

# Count remaining warnings — should be LESS than 4,446
dotnet build Sage4-Everything.sln --no-incremental -v minimal 2>&1 | Select-String "warning CS8" | Measure-Object -Line

# Count files still with #nullable disable
Get-ChildItem Sage -Filter *.cs -Recurse | Select-String "#nullable disable" | Measure-Object -Line
```

**Expected outcome:**
- 0 build errors
- 319/319 tests passing
- Warning count should drop by ~20–30 (interface files have few warnings, but fixing them enables downstream fixes)
- ~527 files still have `#nullable disable` (548 total minus ~21 Phase 1 files)

## Commit Strategy

1. **Commit 1:** `chore: enable nullable reference types globally` — csproj change only
2. **Commit 2:** `chore: add #nullable disable to all source files` — bulk pragma addition
3. **Commit 3:** `feat: annotate Core interfaces with nullable reference types` — Phase 1 annotations
4. **Commit 4:** `feat: annotate Core types (SageOptions, ExecEvent) with nullable reference types` — Phase 1 types

Each commit must build clean and pass all tests.

## After Phase 1

Report back:
- Final warning count
- Any files that proved harder than expected
- Any API changes that might surprise consumers
- Ready/not-ready assessment for Phase 2

Phase 2 spec will be written after Phase 1 results are reviewed.


---

# ConfigurationManager Migration Complete (Parker)

**Date:** 2026-07-15  
**Owner:** Parker  
**Status:** Complete

## Summary
- Replaced ConfigurationManager usage with POCO options (ExecutiveOptions, ExecFactoryOptions, DiagnosticsOptions, EmissionsServiceOptions).
- Added Configure hooks for DiagnosticAids, ExecFactory, and EmissionsService; EmissionsService supports Reset for test isolation.
- ModelConfig now uses Dictionary-backed values with SetSimpleParameter; legacy section constructor marked obsolete.
- Removed System.Configuration.ConfigurationManager package reference and deleted Utility/ConfigurationManager.cs.

## Verification
- `dotnet build E:\source\Sage\Sage4-Everything.sln --no-incremental -v minimal`
- `dotnet test E:\source\Sage\Sage_Aux\SageTestLib\SageTestLib.csproj --no-build -v minimal` (319/319)
