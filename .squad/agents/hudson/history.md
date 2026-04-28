## Core Context

**Current Status (April 2026):** 351/351 passing tests. Phase 3 graph API opening scope gate approved; Phase 1 collections recovery complete.

**Key Project Learnings:**
- ExecFactory singleton captures options at creation time; requires reflection reset if Configure() called post-creation
- Static field `_ignoreCausalityViolations` created cross-test contamination — parallel tests must lock/reset
- Daemon events do NOT fire when only daemons remain; loop condition `_numEventsInQueue > _numDaemonEventsInQueue`
- EFL causality enforcement is non-functional (logs only, throw is commented out)
- `PortSet` GUID-backed `Hashtable` is part of XML persistence contract; key semantics ambiguous in docs vs. implementation
- Graph interfaces are tightly coupled to concrete `Edge`/`Vertex` types; no abstract seam (`IGraph`) exists yet

## Learnings

### 2026-04-28 — Phase 3 Graph API Opening — Regression / Compile Fallout Map ✅

**Status:** Complete

**Mandate:** Map the regression and compile fallout surface for Phase 3 opening batch (`IEdge`, `IVertex`, `Edge`, `Vertex`).

**Learning:** The current graph interfaces are still tightly coupled to concrete `Edge`/`Vertex` types. There is no cheap adapter layer or abstract seam (`IGraph` doesn't exist). The first interface break will ripple immediately through production code (12 files) and tests (7 files).

**Hotspots identified:**
- **Production (12):** `ChannelMonitor`, `MultiChannelEdgeReceiptManager`, `Ligature`, `PathLength`, `VertexSynchronizer`, `DagCycleChecker`, `DagDeadlockChecker`, `DagStructureError`, `CPMAnalyst`, `PertAnalyst`, `DiagnosticAids`, `Task`
- **Tests (7):** `TestDAGCycleChecker`, `TestGraphBranching`, `TestGraphAlgorithmRegressions`, `TestGraphValidities`, `TestGraphPersistence`, `TestTasks`, `TestTasks2`

**Why it matters:** Knowing the exact surface lets Parker plan fixes in scope and Hudson plan regression coverage. The lack of an abstract seam means Batch 1 (interface only) will immediately require fixing consumers; we can't defer consumer updates to a later batch without accumulating blocking errors.

**Decision:** Do NOT add characterization tests now. The interface contracts are not yet stable, so low-level tests would only prevent the intended breaking changes instead of protecting stable behavior.

**Validation:**
- Baseline build: ✅ Clean
- Baseline tests: ✅ Passing

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration log: `.squad/orchestration-log/2026-04-28T21-53-46Z-hudson.md`
- Session log: `.squad/log/2026-04-28T21-53-46Z-phase3-start.md`

---

### 2026-04-28 — Phase 3 Batch 1 Review (`p3-interfaces`) ✅

**Status:** Complete

**Mandate:** Review Parker's interface-only graph batch, add regression coverage if needed, and decide whether Batch 1 is shippable.

**Learning:** The safe regression seam for this batch is the interface boundary itself. Tests must cast through `IEdge` / `IVertex` to prove the new signatures are usable, while avoiding over-locking the temporary concrete `Edge` / `Vertex` public shapes that Parker intentionally preserved with explicit interface implementations.

**Coverage added:**
- `TestVertexInterfaceEdgesTypedAsReadOnlyList`
- `TestEdgeInterfaceExposesTypedEndpointsAndChildren`

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-build` → 290/290 passing ✅

**Verdict:** Approve Batch 1. Parker stayed inside Ripley's scope gate, and the new regression tests now lock the intended interface behavior without freezing the later `p3-edge-vertex` follow-up work.

---

### 2026-07-17 — Phase 3 Batch 2 Review (`p3-edge-vertex`) ❌

**Status:** Rejected

**Mandate:** Review the concrete `Edge`/`Vertex` API batch, add regression coverage for the public-surface break, and decide whether the batch is shippable.

**Learning:** The concrete batch needs its own API-shape tripwires. Interface tests are not enough once `p3-edge-vertex` starts; we need reflection-based assertions on the declared public property types so the batch fails fast if `Edge`/`Vertex` stay on legacy `Vertex`/`IList` signatures.

**Coverage added:**
- `TestVertexConcreteEdgesAreTypedAsReadOnlyList`
- `TestEdgeConcreteApiMatchesInterfaceSurface`

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --filter "FullyQualifiedName~GraphLoopingTester"` ❌
  - `Vertex.PredecessorEdges` / `SuccessorEdges` are still declared as `IList`
  - `Edge.PreVertex` / `PostVertex` are still declared as `Vertex`

**Verdict:** Reject Batch 2 as currently staged. The new regression tests prove the concrete public API break has not been implemented yet, so this is still Batch 1 behavior with no shippable `p3-edge-vertex` surface.

---

### 2026-07-17 — Phase 3 Batch 2 Review (`p3-edge-vertex`) Revision Pass ✅

**Status:** Complete

**Mandate:** Re-review Ripley's replacement concrete `Edge`/`Vertex` API batch, add regression coverage only if still needed, and decide whether the batch is now shippable.

**Learning:** The concrete public break is now actually in place without reopening the forbidden persistence/storage scope. The existing interface-behavior tests plus the concrete reflection tripwire now give the right coverage balance: they lock the public shape change while leaving `PrincipalEdge`, XML persistence, and `GetParent()` alone.

**Coverage added:** None. Existing regression coverage was sufficient once `Edge.PredecessorEdges` / `SuccessorEdges` joined the concrete API assertions.

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore` ✅ (293/293 passing)

**Verdict:** Approve the revised Batch 2. Ripley's replacement revision lands the intended concrete public API break, limits fallout repairs to genuine `Vertex`-only seams, and passes the full graph test project cleanly.

---

**Archived history:** Detailed entries from March 2026–April 26, 2026 preserved in `hudson-history-archive.md`.
