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

**Archived history:** Detailed entries from March 2026–April 26, 2026 preserved in `hudson-history-archive.md`.
