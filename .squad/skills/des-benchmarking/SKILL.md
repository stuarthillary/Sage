# SKILL: Benchmarking Discrete Event Simulation (DES) Engines with BenchmarkDotNet

**Category:** Performance / Benchmarking  
**Applies to:** Sage® engine and any DES library with a priority-queue event loop  
**Author:** Hicks  

---

## The Two Canonical DES Benchmark Patterns

### 1. Sequential (Bulk-Load) Benchmark

Pre-schedule N events with strictly increasing timestamps, then run to completion.

```csharp
[Benchmark]
public DateTime BulkLoad_Sequential()
{
    IExecutive exec = ExecFactory.Instance.CreateExecutive();
    exec.SetStartTime(SimStart);

    DateTime when = SimStart;
    for (int i = 0; i < N; i++)
    {
        when = when.AddSeconds(1);
        exec.RequestEvent(_noOp, when);
    }

    exec.Start();
    return exec.Now; // prevent dead-code elimination
}
```

**What this exposes:**
- Queue insertion complexity for worst-case (pre-sorted) input
- Queue dequeue complexity under high depth
- Object allocation per event (GC pressure)

**Gotcha — ascending order is heap-friendly:** A min-heap sift-up completes in O(1) when inserting the maximum seen so far. This is the *best case* for heaps. Use random timestamps (`_rng.Next()`) to benchmark worst-case insertion.

### 2. Chained (Self-Scheduling) Benchmark

Each event handler schedules the next event. Queue depth stays at ~1.

```csharp
private int _count;

[Benchmark]
public int Chained()
{
    _count = 0;
    IExecutive exec = ExecFactory.Instance.CreateExecutive();
    exec.SetStartTime(SimStart);
    exec.RequestEvent(_chainHandler, SimStart.AddSeconds(1));
    exec.Start();
    return _count;
}

private void ChainHandler(IExecutive exec, object? _)
{
    _count++;
    if (_count < N)
        exec.RequestEvent(_chainHandler, exec.Now.AddSeconds(1));
}
```

**What this exposes:**
- Per-event overhead with minimal queue contention
- Lock/synchronization overhead per schedule+dispatch pair
- Object allocation per schedule (delegate + event object)
- Most representative of real simulation entity behaviour

---

## Benchmark Configuration Choices

### `[ShortRunJob]` — for validation
Use when you just want to confirm the code compiles and runs without errors. Reduces warm-up and iteration counts. Remove for publication-quality results.

### `[MemoryDiagnoser]` — always include
Shows `Allocated` bytes per operation. Critical for spotting GC pressure from:
- Per-event object allocations (event wrappers, closures)
- Missing or broken object pooling
- Delegate allocations (cache delegates as fields, not inline `new ExecEventReceiver(...)`)

### Cache delegates as fields
```csharp
// BAD — allocates a new delegate on every benchmark call
exec.RequestEvent(new ExecEventReceiver(MyHandler), when);

// GOOD — reuse cached delegate instance
private readonly ExecEventReceiver _handler;
// in constructor: _handler = MyHandler;
exec.RequestEvent(_handler, when);
```

### Return a value to prevent dead-code elimination
```csharp
return exec.Now;   // DateTime struct — no allocation, prevents DCE
```

---

## DES Executive Patterns and Their Benchmark Implications

| Queue Implementation | Insert | Dequeue | Real-world N=100k |
|---|---|---|---|
| `SortedList` + `RemoveAt(0)` | O(log n) search + O(1) if appending | **O(n)** shift | Very slow (~O(N²) total) |
| `SortedList` random insert | O(log n) search + **O(n)** shift | O(n) shift | Very slow |
| Binary min-heap | O(log n) sift-up; **O(1)** for ascending | O(log n) sift-down | Fast |
| `PriorityQueue<T,P>` (.NET 6+) | O(log n) | O(log n) | Fast |

**Key insight:** Even if insertion looks fast, `SortedList.RemoveAt(0)` makes every dequeue O(n). For N events this gives O(N²) total time — visible as super-linear scaling in `[Params]` results.

---

## How to Spot O(N²) vs O(N log N) in BenchmarkDotNet Output

Run with `[Params(1_000, 10_000, 100_000)]`.

If the executive is O(N²):
- 1k → 10k: ~100× slower (not 10×)
- 10k → 100k: ~100× slower

If the executive is O(N log N):
- 1k → 10k: ~11× slower
- 10k → 100k: ~13× slower

---

## Executive Lifecycle: What to Know for Benchmarks

- After `exec.Start()` completes, the executive is in `ExecState.Finished`. It **cannot be reused** without a `Reset()` call (and `Reset()` is not on `IExecutive`).
- For each benchmark invocation, create a new executive instance via `ExecFactory.Instance.CreateExecutive()`.
- `ExecFactory` uses reflection (`ConstructorInfo.Invoke`) — this is included in the benchmark measurement if creation is in the benchmark body. Use `[IterationSetup]` to separate creation overhead if needed.
- `ExecutiveFastLight` does not support: `UnRequestEvent`, `Pause/Resume`, `Abort`, `DetachableEvent`, `Join`. Benchmarking these features requires the full `Executive`.

---

## Directory Layout for BDN Project in This Repo

```
Sage_Aux\
  SageBenchmarks\
    SageBenchmarks.csproj        ← OutputType=Exe, ref BenchmarkDotNet + Sage4.csproj
    Program.cs                   ← BenchmarkSwitcher.FromAssembly(...).Run(args)
    Benchmarks\
      EventDispatchBenchmarks.cs ← [ShortRunJob][MemoryDiagnoser] classes
```

**Run command:**
```sh
dotnet run -c Release --project Sage_Aux\SageBenchmarks\SageBenchmarks.csproj
```

**Always use `-c Release`** — BDN will refuse to run or warn loudly in Debug mode.
