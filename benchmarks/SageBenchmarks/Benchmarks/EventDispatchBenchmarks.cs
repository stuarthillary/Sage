// Sage DES Engine — Event Dispatch Baseline Benchmarks
// -------------------------------------------------------
// Hicks / Performance Engineer — baseline established ahead of Executive event-queue
// optimization work (replacing O(n) SortedList with heap-based priority queue).
//
// HOT-PATH OBSERVATIONS (from code review — measure, don't speculate):
//
//   Executive (full-featured, Highpoint.Sage.Core.Executive):
//     - Event queue: System.Collections.SortedList (ExecEventComparer)
//     - Insert:  SortedList.Add()    — binary search O(log n) + element shift O(n) worst case
//     - Dequeue: SortedList.GetKey(0) + RemoveAt(0) — the RemoveAt(0) shifts ALL remaining
//               elements, making each dequeue O(n). For N events this is O(N²) total.
//     - Locking: lock(_events) on EVERY RequestEvent AND inside the dispatch loop.
//     - Extras:  causality checks, diagnostics checks, pause/resume support, Monitor bookkeeping.
//
//   ExecutiveFastLight (heap-based, Highpoint.Sage.Core.ExecutiveFastLight):
//     - Event queue: binary min-heap backed by _ExecEvent[]
//     - Insert:  Enqueue() sift-up — O(log n); O(1) amortised for pre-sorted (ascending) input
//     - Dequeue: Dequeue() sift-down — O(log n) always
//     - Object pool: ExecEventCache re-uses _ExecEvent instances across calls (avoids GC churn)
//     - No locks, no pause/resume, no rescindable events.
//
// BENCHMARKS IN THIS FILE:
//   SequentialEvents — bulk schedule N events (ascending time), run to completion.
//                      Stresses dequeue path; RemoveAt(0) vs heap sift-down.
//   ChainedEvents    — each handler schedules the next event (queue depth stays ~1).
//                      Stresses the combined schedule+dispatch cost per event.
//                      Most representative of a real DES simulation loop.
//
// WHAT TO LOOK FOR IN RESULTS:
//   - Executive vs ExecutiveFastLight mean time ratio (expect 5–50× faster for FastLight at N=100k)
//   - Allocated bytes per operation (MemoryDiagnoser) — FastLight should show far lower allocs
//     due to ExecEventCache pooling
//   - Scaling: Executive should show super-linear (O(N²)) scaling; FastLight O(N log N)

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Highpoint.Sage.Core;
using System;

namespace Highpoint.Sage.Benchmarks;

/// <summary>
/// Baseline throughput benchmarks for the Sage discrete event simulation engine.
/// <para>
/// <b>ShortRunJob</b> keeps warm-up and iteration counts low so Stuart can do quick
/// validation builds. For publication-quality numbers, remove [ShortRunJob] and let
/// BenchmarkDotNet auto-choose the job configuration (or add [SimpleJob(launchCount:3)]).
/// </para>
/// </summary>
[ShortRunJob]
[MemoryDiagnoser]
public class EventDispatchBenchmarks
{
    // Fixed simulation epoch — avoids DateTime.Now overhead and keeps runs reproducible.
    private static readonly DateTime SimStart = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Cached delegate instances — prevents per-call ExecEventReceiver allocation from
    // polluting the allocation measurements in the chained-event benchmarks.
    private readonly ExecEventReceiver _noOp;
    private readonly ExecEventReceiver _chainHandlerFull;
    private readonly ExecEventReceiver _chainHandlerFast;

    // Shared counter for chained-event benchmarks; reset at start of each call.
    private int _chainCount;

    public EventDispatchBenchmarks()
    {
        _noOp = static (_, _) => { };
        _chainHandlerFull = ChainHandler_Executive;
        _chainHandlerFast = ChainHandler_FastLight;
    }

    /// <summary>Number of events to schedule per benchmark invocation.</summary>
    [Params(1_000, 10_000, 100_000)]
    public int EventCount { get; set; }

    // -------------------------------------------------------------------------
    // SEQUENTIAL EVENTS — bulk pre-load N events then run to completion
    // -------------------------------------------------------------------------

    /// <summary>
    /// Baseline: schedule <see cref="EventCount"/> events at 1-second intervals (ascending),
    /// then run the full-featured <c>Executive</c> (SortedList queue) to completion.
    /// <para>
    /// Bottleneck: <c>SortedList.RemoveAt(0)</c> shifts all remaining elements on every
    /// dequeue — O(n) per dequeue, O(N²) total for N events.
    /// </para>
    /// </summary>
    [Benchmark(Baseline = true, Description = "Executive — sequential N events")]
    public DateTime Executive_SequentialEvents()
    {
        IExecutive exec = ExecFactory.Instance.CreateExecutive();
        exec.SetStartTime(SimStart);

        DateTime when = SimStart;
        for (int i = 0; i < EventCount; i++)
        {
            when = when.AddSeconds(1);
            exec.RequestEvent(_noOp, when);
        }

        exec.Start();
        return exec.Now; // return value prevents dead-code elimination
    }

    /// <summary>
    /// Comparison: schedule <see cref="EventCount"/> events at 1-second intervals (ascending),
    /// then run <c>ExecutiveFastLight</c> (binary min-heap) to completion.
    /// <para>
    /// Ascending insertion hits the O(1) amortised best-case for heap sift-up.
    /// Dequeue is always O(log n) sift-down. Object pool (<c>ExecEventCache</c>) eliminates
    /// per-event heap allocation after the first pass.
    /// </para>
    /// </summary>
    [Benchmark(Description = "ExecutiveFastLight — sequential N events")]
    public DateTime ExecutiveFastLight_SequentialEvents()
    {
        IExecutive exec = ExecFactory.Instance.CreateExecutive(ExecType.SingleThreaded);
        exec.SetStartTime(SimStart);

        DateTime when = SimStart;
        for (int i = 0; i < EventCount; i++)
        {
            when = when.AddSeconds(1);
            exec.RequestEvent(_noOp, when);
        }

        exec.Start();
        return exec.Now;
    }

    // -------------------------------------------------------------------------
    // CHAINED EVENTS — each handler schedules the next event
    // This pattern keeps the queue depth at ~1 throughout the run, so insertion
    // cost is always O(1) for the heap and O(log n)+O(1) for the SortedList.
    // The benchmark isolates the per-event overhead of schedule → dispatch → callback.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Chained pattern on <c>Executive</c>: the first event is pre-scheduled; each
    /// subsequent handler schedules the next one. Queue depth stays at 1.
    /// <para>
    /// This is the most representative micro-benchmark for a DES simulation where
    /// entities schedule their own future events (e.g. a process step schedules its
    /// own completion event).
    /// </para>
    /// </summary>
    [Benchmark(Description = "Executive — chained (depth-1 queue)")]
    public int Executive_ChainedEvents()
    {
        _chainCount = 0;

        IExecutive exec = ExecFactory.Instance.CreateExecutive();
        exec.SetStartTime(SimStart);
        exec.RequestEvent(_chainHandlerFull, SimStart.AddSeconds(1));
        exec.Start();

        return _chainCount;
    }

    /// <summary>
    /// Chained pattern on <c>ExecutiveFastLight</c>: identical logic to
    /// <see cref="Executive_ChainedEvents"/> but uses the heap-based executive.
    /// </summary>
    [Benchmark(Description = "ExecutiveFastLight — chained (depth-1 queue)")]
    public int ExecutiveFastLight_ChainedEvents()
    {
        _chainCount = 0;

        IExecutive exec = ExecFactory.Instance.CreateExecutive(ExecType.SingleThreaded);
        exec.SetStartTime(SimStart);
        exec.RequestEvent(_chainHandlerFast, SimStart.AddSeconds(1));
        exec.Start();

        return _chainCount;
    }

    // -------------------------------------------------------------------------
    // Event handler implementations
    // -------------------------------------------------------------------------

    private void ChainHandler_Executive(IExecutive exec, object? userData)
    {
        _chainCount++;
        if (_chainCount < EventCount)
            exec.RequestEvent(_chainHandlerFull, exec.Now.AddSeconds(1));
    }

    private void ChainHandler_FastLight(IExecutive exec, object? userData)
    {
        _chainCount++;
        if (_chainCount < EventCount)
            exec.RequestEvent(_chainHandlerFast, exec.Now.AddSeconds(1));
    }
}
