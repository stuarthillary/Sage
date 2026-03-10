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

## Learnings

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
