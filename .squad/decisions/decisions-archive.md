# Decisions Archive — Sage DES Engine

Archived decisions (older than 30 days). See decisions.md for current decisions.

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

