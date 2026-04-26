## Core Context

**Executive Test Coverage (March 2026):**
- **Current:** 351/351 passing tests (baseline 316 in early March)
- **Progress:** +35 new tests across 3 phases:
  - Phase 1 (Mar 6): 16 tests for collection migration test coverage
  - Phase 2 (Mar 9-10): 21 new tests: Priority 1 (7), Priority 2 (8), Priority 3 (6)
  - Phase 3 (Mar 17): 4 EFL state tracking + 3 causality divergence tests
- **Scope:** Comprehensive coverage of Executive and ExecutiveFastLight state management, error paths, causality handling
- **Key learnings:**
  - ExecFactory singleton captures options at creation time; requires reflection reset if Configure() called post-creation
  - Static field `_ignoreCausalityViolations` created cross-test contamination — parallel tests must lock/reset via ExecFactory.Configure() + ResetExecFactorySingleton()
  - Daemon events do NOT fire when only daemons remain; loop condition `_numEventsInQueue > _numDaemonEventsInQueue`
  - Pause/Resume API names (not Suspend); sync handler pause needs `Thread.Sleep(50)` for PauseManager catchup
  - FIFO tiebreaker via `_nextReqHashCode`; determinism verified across 1000-event runs
  - Executive drops past events (long.MinValue) when IgnoreCausalityViolations=true; EFL clamps to Now and fires
  - EFL causality throw is commented out — "enforce mode" only logs to Console, doesn't actually enforce

---

### 2026-04-26 — Collections Recovery Scope Audit Complete ✅

**Status:** Complete

**Batch Summary:**
- Classified all touched files into valid Phase 1, Phase 2+ creep, and ambiguous items
- Provided technical foundation for Phase 1 boundary enforcement
- Identified graph-analysis changes as root cause of build breaks

**Scope Classification:**
- **Phase 1 (Safe):** Signature-preserving private/internal collection swaps in core/supporting internals
- **Phase 2+ (Out-of-Scope):** Graph algorithms, PFC, Materials, public-contract-adjacent changes (PortSet, WeakHashtable, WeakList, HashtableOfLists)
- **Ambiguous:** Items deferred for separate coordination

**Coordination:** Results provided to Ripley for scope-reset decision and implementation.

**Outcome:** Scope audit complete; Phase 1 boundaries established for collections recovery batch.

---

### 2026-03-17 — Causality Divergence Investigation Complete ✅

**Status:** COMPLETE — 3 tests added, 351/351 passing

**Findings:** Deep analysis of causality violation handling across Executive and ExecutiveFastLight. Both implementations diverge intentionally by design:

- **Executive + IgnoreCausalityViolations=true (default):** Returns `long.MinValue` — event is **dropped**
- **EFL + IgnoreCausalityViolations=true (default):** Clamps to `_now` — event **fires at _now**

When `IgnoreCausalityViolations=false`:
- **Executive:** Throws `CausalityException` (wrapped in `RuntimeException`)
- **EFL:** Logs to `Console.WriteLine()` (throw is commented out) and fires event anyway

**Tests added:** 3 in #region Causality in TestExecutive.cs
1. EFL ignore mode clamps past event to Now and fires
2. EFL enforce mode (broken throw) still fires, only logs
3. Cross-impl divergence: Executive drops, EFL fires

**Code smell:** EFL's causality enforcement is non-functional — users configuring `IgnoreCausalityViolations=false` expecting exceptions get silent console logging instead.

**Recommendation:** Document divergence in XML doc, fix or clarify EFL's "enforce" mode (either implement throw or rename to log-only).

---

### 2026-03-10 — Priority 1 Executive Tests Written ✅

**Status:** COMPLETE — 6 new Priority 1 tests added to TestExecutive.cs

**Test suite:** 330/330 passing (was 324 before)

**Tests added:**
1. `Executive_EventHandlerException_PropagatesToCaller` — Exception propagation verified
2. `Executive_CausalityViolation_BehaviorTest` — Causality enforcement tested (throw + ignore modes)
3. `Executive_Reset_ClearsQueueAndResetsNow` — Reset correctness verified  
4. `Executive_SameTimeEqualPriority_DispatchedBySubmissionOrder` — FIFO tiebreaker confirmed
5. `Executive_EmptyQueue_StartsAndFinishesGracefully` — Empty queue handling verified
6. `Executive_Determinism_SameSeedProducesSameOutput` — Deterministic replay confirmed

**Orchestration artifacts:**
- Orchestration log: `.squad/orchestration-log/2026-03-10T00-12-00Z-hudson.md`
- Session log: `.squad/log/2026-03-10T00-12-00Z-priority1-tests.md`
- Decision merged to `.squad/decisions.md` (inbox deleted)

**Next:** Priority 2 tests (8 tests) for error handling and RequestImmediateEvent

### 2026-03-09 — Core Engine Test Coverage Audit Complete ✅

**Status:** Analysis complete, findings documented

**Key Findings:**
- **CausalityException:** Zero test coverage — highest-risk gap. A regression silently breaks DES correctness (events processed out of order).
- **Static field contamination:** `Executive._ignoreCausalityViolations` is modified by instance constructors. Tests running in parallel can mutate each other's causality enforcement setting.
- **Untested error paths:** Exception propagation from handlers, Executive.Abort() mid-dispatch, RequestEvent on Finished executive.
- **Untested public APIs:** Executive.Reset(), Executive.RequestImmediateEvent, Executive.ResubmitEventAtTime, Model lifecycle flags.

**Coverage Summary:**
- 24 well-covered behaviors
- 6 partially covered areas
- 18 uncovered behaviors (critical gaps)
- 21 new tests recommended (Priority 1: 7 critical, Priority 2: 8 important, Priority 3: 6 nice-to-have)

**Deliverables:**
- `.squad/decisions/hudson-core-test-gaps.md` — comprehensive gap report (merged to decisions.md)
- `.squad/orchestration-log/2026-03-09T23-27-56Z-hudson.md` — orchestration log
- `.squad/log/2026-03-09T23-27-56Z-core-engine-test-coverage-audit.md` — session log

**Build status:** 0 errors, 0 warnings.

**Next:** Prioritize Priority 1 tests (7 tests) before any Executive refactoring.

### 2026-03-06 — Collection Migration Test Coverage Complete ✅

- **Status:** COMPLETE — 16 new tests added across 5 test files
- **Test suite:** 316/316 passing, 3 Phase 2 prep tests [Ignore]'d (temporarily disabled)
- **Coverage:** CRUD operations and enumeration on all Collection migrations
- **Files modified:** TestMaterials.cs, TestResources.cs, TestStateMachine.cs, TestExecutive.cs, TestGraphBranching.cs
- **Bug fixes:** Fixed 4 compilation errors in Parker's Phase 1 work (Enum casts, type conversions, generic signatures)
- **Phase 2 prep:** TestEventListTypedAsIReadOnlyList, TestLiveDetachableEventsTypedAsIReadOnlyList, TestVertexEdgesTypedAsList — staged for Phase 2
- **Quality:** 100% pass rate, 310 existing tests unaffected
- **Decision:** Test infrastructure comprehensive. Phase 2 API changes can proceed.

### 2026-05-30 — Core Engine Test Coverage Audit Complete ✅

### 2026-05-30 — EFL State Tracking Regression Tests Written ✅

**Status:** COMPLETE — 4 new regression tests added to TestExecutive.cs in `#region EFL State Tracking`

**Test suite:** 348/348 passing (was 344 before)

**Tests added:**
1. `ExecutiveFastLight_State_IsRunningDuringDispatch` — asserts `State == Running` inside handler, `State == Finished` after Start()
2. `ExecutiveFastLight_State_IsFinishedAfterNormalCompletion` — 5-event run verifies Finished state
3. `ExecutiveFastLight_State_IsStoppedAfterStop` — Stop() from handler yields Stopped state
4. `ExecutiveFastLight_RequestEvent_AfterFinished_Throws` — ApplicationException with "Finished" message verified

**Context:** Locks in Parker's four-part fix to ExecutiveFastLight state tracking (Running/Finished/Stopped transitions and RequestEvent Finished guard).

## Learnings

### 2026-04-26 — Phase 2 Graph Algorithm Regression Coverage Complete ✅

**Status:** COMPLETE — 4 graph-algorithm regression tests added; targeted graph slice and full `Sage.Tests` suite passing

**Test results:**
- New graph regression tests: **4/4 passing**
- Targeted graph slice (`GraphAlgorithmRegressionTests`, `GraphLoopingTester`, `DAGCycleCheckerTester`): **13/13 passing**
- Full `tests\SageTestLib\Sage.Tests.csproj`: **285/285 passing**

**Coverage locked in:**
- `CpmAnalyst` still computes earliest/latest times, acceptable slip, and critical-path membership correctly across a branching graph after the internal stack/list updates.
- `PertAnalyst` still walks ligature-linked successors to the real critical edges, preserves `ArrayList`/read-only `CriticalPath` behavior, and aggregates mean/variance from only the critical path.
- `DagDeadlockChecker` still suppresses duplicate successor/frontier entries and reports a single residual deadlock target for a simple reachable cycle instead of duplicating entries after the internal `Dictionary`/`List<Node>` migration.

**Remaining gaps:**
- I did not add synchronizer-specific `DagDeadlockChecker` coverage in this pass; deadlock target selection for more complex cycles/synchronizer meshes is still implementation-sensitive and needs explicit product guidance before I widen assertions.
- No new regression coverage yet for CPM/PERT behavior on synchronized vertices or pegged vertices in this Phase 2 slice.

### 2026-04-26 — Phase 2 Utility Wrapper Regression Coverage Complete ✅

**Status:** COMPLETE — 10 wrapper-focused regression tests added/updated; targeted wrapper tests and full `Sage.Tests` suite passing

**Test results:**
- Targeted Phase 2 wrapper tests: **14/14 passing**
- Full `tests\SageTestLib\Sage.Tests.csproj`: **281/281 passing**

**Coverage locked in:**
- `HashtableOfLists` non-generic still de-duplicates identical values for the same key and only prunes empty wrapped keys on enumeration/prune.
- `HashtableOfLists<TKey, TValue>` preserves duplicate values, sorts per-key when a comparer is supplied, and requires explicit prune after the last removal.
- `WeakHashtable` removes dead entries on indexer/`Values` access and enumerates only live entries when driven via `IDictionaryEnumerator`.
- `WeakList` list operations are target-based (`Contains`/`IndexOf`/`Remove`), `CopyTo` honors the destination offset, and `Collapse()` removes dead targets.

**Tiny behavior-safe production adjustments needed to keep Phase 2 wrappers testable:**
- `WeakList.Insert(...)` must wrap with `MyWeakReference`, not plain `WeakReference`, or indexer/enumerator/collapse semantics break for inserted items.
- `WeakList.Add(...)` must still return the inserted index after the internal storage migrates from `ArrayList` to `List<MyWeakReference>`.
- `HashtableOfLists.Add(...)` needed a null-safe equality check after the `Hashtable` → `Dictionary<object, object?>` migration to satisfy nullable analysis without changing semantics.

**Remaining gaps:**
- No direct regression coverage yet for `WeakHashtable.CopyTo`, `Keys` ordering assumptions, or `WeakList` indexer-set behavior after GC.
- I kept scope tight to the Phase 2 utility-wrapper batch; no broader collection-migration coverage added here.

### 2026-05-30 — Causality Equivalence Investigation Complete ✅

**Status:** COMPLETE — 3 new tests added to `#region Causality` in TestExecutive.cs

**Test suite:** 351/351 passing (was 348 before)

**Tests added:**
1. `ExecutiveFastLight_CausalityViolation_WhenIgnored_ClampsToNow` — EFL default behavior
2. `ExecutiveFastLight_CausalityViolation_WhenNotIgnored_StillFiresAtNow` — EFL enforce mode
3. `CausalityHandling_Executive_Drops_EFL_Clamps_WhenIgnoring` — explicit divergence doc test

**Exact causality behavior matrix (verified from source):**

| Scenario | Executive (`_ignore=false`) | Executive (`_ignore=true`, DEFAULT) | EFL (`_ignore=false`) | EFL (`_ignore=true`, DEFAULT) |
|----------|------------------------------|--------------------------------------|------------------------|-------------------------------|
| `RequestEvent` with `when < _now` | Throws `CausalityException` → wrapped in `RuntimeException` by exec loop | Returns `long.MinValue`; event **DROPPED**, never fires | Logs to Console (throw is commented out!); enqueues at past `when`; `StartWcv()` clamps to `_now` at dequeue → event **fires at `_now`** | Clamps `when = _now` immediately in `RequestEvent`; enqueues at `_now` → event **fires at `_now`** |
| Key divergence | Strict DES enforcement | Silent drop | Soft enforcement (log only) + dequeue clamp | Best-effort clamp at schedule time |

**Key findings:**
- **Executive DROPS, EFL FIRES:** When `_ignoreCausalityViolations = true`, Executive returns `long.MinValue` (event gone); EFL clamps to `_now` and fires it. This is the primary behavioral divergence.
- **EFL never throws `CausalityException`:** Even with `_ignore=false`, EFL only calls `Console.WriteLine()` (the `throw` is commented out in source). The event is still scheduled at the past time; `StartWcv()` silently clamps it at dequeue.
- **`StartWcv()` is EFL's second-chance clamp:** When `_ignore=false`, EFL uses `StartWcv()` which detects `_now.Ticks > _currentEvent.When` and sets `_currentEvent.When = _now.Ticks` before dispatch. This is a belt-and-suspenders clamp.
- **Static field `_ignoreCausalityViolations` is STATIC on both impls:** Creating any Executive or EFL instance sets the global static for that class. Tests that change it must save/restore via `ExecFactory.Configure()` + `ResetExecFactorySingleton()` under `_execFactoryLock`.
- **Default `_ignoreCausalityViolations = true`:** Both Executive (line 44) and EFL (line 170) default to `true`. A new executive with default options silently ignores violations.

---

### 2026-03-10 — Priority 3 Tests Implementation

- **Abort() from a synchronous handler — confirmed behavior:** `_abortRequested = true` → `Reset()` called (clears queue, `_now = DateTime.MinValue`, state = Stopped) → handler returns → dispatch loop exits (`!_abortRequested` = false) → `_stopRequested` is false so else-branch: `_state = ExecState.Finished`. Start() does NOT throw because the RuntimeException throw guard checks `!_abortRequested`. `ExecutiveAborted` event does NOT fire for sync aborts (it's inside `if (RunningDetachables.Count > 0)`). Final state: Finished, Now = DateTime.MinValue, no exception.

- **EventCount is uint:** `exec.EventCount` returns `uint`. Tests must assert `(uint)N` not `int N` to avoid type mismatch in Assert.Equal.

- **RunNumber starts at -1:** `_runNumber` is initialized to `-1` in the field declaration. It becomes 0 after the first `Start()` call (`_runNumber++` at line 612). Each Reset()+Start() cycle increments it by 1.

- **CurrentEventType is None outside dispatch:** `_currentEventType` is set to `ExecEventType.None` in the `finally` block after each event dispatch. Before any Start(), it returns `ExecEventType.None` (the default enum value 0 = Synchronous? No — None = 3). Confirmed: None before, Synchronous during, None after.

- **ExecEventType.Detachable vs Synchronous:** Detachable events run on a thread-pool thread (not the exec thread). The exec dispatch thread suspends awaiting completion or suspension of that thread. `CurrentEventType` on the exec while a Detachable runs = Detachable. Existing tests (JoinDetachable, LiveDetachableEvents) already cover this path.

- **Large-volume heap ordering verified:** 1000 events at random times (seed 42) all fire in correct non-decreasing order. The binary min-heap correctly handles duplicate timestamps and priority tiebreakers at scale.

- **UnRequestEvent(long) double-cancel causes ApplicationException from Start():** `FilterOnEventId` (ExecEventRemover.cs line 73) throws `ApplicationException` if the event ID is not found. If the same key is pushed to `_removals` twice (LIFO), the second filter processes first (finds and removes the event), then the first filter can't find the already-removed event and throws. This exception bubbles out of the dispatch loop and out of Start() unwrapped (not caught by the inner try/catch which only wraps event dispatch at line 691).

### 2026-03-10 — Priority 2 Tests Implementation

- **Daemon event behavior (confirmed):** Daemon events do NOT fire when they are the only events remaining in the queue. The dispatch loop condition in Executive.cs (line 636) is `_numEventsInQueue > _numDaemonEventsInQueue`. When only daemons remain, the condition is `n > n` (false) and the loop exits without firing the daemon events.

- **Pause/Resume API names:** The methods are `exec.Pause()` and `exec.Resume()` (not `Suspend`/`Resume`). Internally, Pause works by pulsing `_runLock` to wake a PauseManager background thread, which then acquires `_runLock` and holds it, blocking the exec loop's next iteration. When calling `Pause()` from inside a synchronous event handler (on the exec thread), a `Thread.Sleep(50)` is needed after calling Pause() to ensure PauseManager acquires `_runLock` before the handler returns and the exec advances — otherwise there is a race where the exec can fire more events before PauseManager catches up.

- **RequestEvent on a Finished executive throws:** Calling `RequestEvent(...)` on an executive in `ExecState.Finished` throws `ApplicationException("Event service cannot be requested from an Executive that is in the \"Finished\" state.")`. It does NOT silently ignore. This is implemented at Executive.cs line ~359 inside the `RequestEvent` private overload.

- **UnRequestEvent with selector on empty queue:** `UnRequestEvents(IExecEventSelector)` is safe to call even when no events are queued. The removal is pushed to the `_removals` stack; when Start() runs and processes the stack, the selector filter produces an empty "remaining" list — no exception. This is in contrast to `UnRequestEvent(long)` (by key), which would throw `ApplicationException` if the key is not found (via `FilterOnEventId`).

- **ResubmitEventAtTime signature:** `ResubmitEventAtTime(long eventID, DateTime newTime, bool deleteOldOne)`. The `eventID` must refer to an event currently IN the queue (not the event currently being dispatched). It creates a new event with the same properties at `newTime`; `deleteOldOne:false` keeps the original, `true` removes it. Returns the new event's key.



### 2026-03-10 — Priority 1 Tests Implementation

- **ExecFactory singleton constraint:** The ExecFactory singleton captures `ExecutiveOptions` at creation time in `_instanceExecutiveOptions` (line 32-38 of ExecFactory.cs). Calling `Configure()` after the singleton is created does NOT affect existing instances. Tests must use reflection to reset the singleton: `typeof(ExecFactory).GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null)`.

- **Static field contamination confirmed in practice:** `Executive._ignoreCausalityViolations` is static (line 44) but set by instance constructors (line 66). This means creating ANY executive sets the static field for ALL executives globally, including those created in other tests running in parallel. Tests 2 and 3 had to be combined with explicit locking and singleton reset to prevent race conditions.

- **Exception propagation behavior:** When an event handler throws, the Executive catches it at line 721-728, stores in `_terminationException`, sets `_stopRequested = true`, and after the event loop finishes, re-throws as `RuntimeException` with the original exception as InnerException (line 826). The executive state transitions to Finished or Stopped depending on queue state.

- **Reset() implementation:** `Reset()` (lines 1027-1042) sets `_state = ExecState.Stopped`, `_now = DateTime.MinValue`, allocates a new event heap, clears counters, calls `Resume()` to clear pause state, and fires the ExecutiveReset event. Post-reset, a new simulation can be scheduled and run.

- **FIFO tiebreaker mechanism:** Events submitted at the same time with the same priority are ordered by `_nextReqHashCode` (line 343), an incrementing `long` counter. This guarantees FIFO (submission order) dispatch for ties, which is critical for simulation determinism.

- **Determinism verification:** With a fixed `Random` seed, event submission times and priorities are reproducible. The heap-based event queue then dispatches events in a deterministic order (time, then priority, then submission order). Two runs with `Random(42)` produced identical event sequences.

- **CausalityException has zero test coverage.** `Executive.RequestEvent` throws `CausalityException` when `when < _now` and `IgnoreCausalityViolations = false`, but no test exercises this path. This is the highest-risk gap — a regression here silently breaks DES correctness.

- **Static fields in Executive create cross-test contamination risk.** `_clrConfigDone` and `_ignoreCausalityViolations` are `static bool` fields modified by instance constructors. Tests running in parallel or sequence can mutate each other's causality enforcement setting with no isolation.

- **Exception propagation from handlers is untested.** When a synchronous handler throws, the Executive stores `_terminationException` and re-throws as `RuntimeException` after the loop. No test verifies this path, the resulting exec state, or that the exception message includes the original.

- **Executive.Reset() is called but never asserted.** Two tests use `Reset()` as setup scaffolding, but none assert the post-reset state (empty queue, `Now == DateTime.MinValue`, `State == Stopped`).

- **Model lifecycle flags (IsRunning, IsCompleted, IsReady) are never asserted.** The flags are set but no test reads them back.

- **21 new tests recommended** in gap report at `.squad/decisions/inbox/hudson-core-test-gaps.md`. Priority 1 (7 tests) covers correctness-critical paths that must be addressed before any Executive refactoring.

- **Build status at time of audit:** 0 errors, 0 warnings. Test count not re-run but prior state was 316 passing.

### 2026-03-06 — Phase 2 API Spec Context (Ripley)

- **Specification:** 7 public API breaking changes documented in decisions.md
- **Caller impact:** Most are internal-only (low risk); 2 require external caller updates
- **IExecutive changes:** LiveDetachableEvents and EventList return types (IReadOnlyList<T>)
- **Vertex changes:** SuccessorEdges, PredecessorEdges, VertexContext return types
- **ResourceManager:** Resources return type (IReadOnlyList<IResource>)
- **Test readiness:** 3 prep tests ready to validate Phase 2 types (currently [Ignore]'d)
- **Next:** Phase 2 lead to implement API changes and enable prep tests

---

### 2026-04-26 — Graph Algorithm Regression Coverage ✅

**Status:** Complete

**Batch Summary:**
- Added Phase 2 regression tests for `CpmAnalyst`, `PertAnalyst`, and `DagDeadlockChecker` in `tests\SageTestLib\TestGraphAlgorithmRegressions.cs`.
- Locked in Parker's guardrail that legacy public surfaces stay put: `PertAnalyst.CriticalPath` remains read-only `ArrayList`, and `DagDeadlockChecker.Errors` stays read-only/non-generic at the boundary.

**Test Semantics Locked:**
- Duplicate successor references must not create duplicate frontier/error targets
- Simple reachable cycle must report one residual frontier target
- Intentionally did **not** over-assert full target ordering/exhaustiveness for complex deadlock sets because that remains implementation-sensitive

**Result:**
- New regression tests: 4/4 passing ✅
- Targeted graph algorithm suite: 13/13 passing ✅
- Full `Sage.Tests`: 285/285 passing ✅

**Coordination:** Ready for Phase 3 planning or additional collection-type migration work.
