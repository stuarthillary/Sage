# Project Context

- **Owner:** Stuart Hillary
- **Project:** Sage® Simulation and Modeling Libraries — a long-running discrete event simulation (DES) library originally built on early .NET Framework, now targeting .NET 8.
- **Stack:** C#, .NET 8, NUnit/xUnit, GitHub Actions, NuGet
- **Key modules:** Core (event engine), Scheduling, Graphs, Mathematics, SystemDynamics, ItemBased, Randoms, Persistence, Presentation, SmartPropertyBag, Utility
- **Goals:** Continued development, .NET 8 modernization, performance improvement, future visualization layer
- **PM:** Stuart Hillary (human — sets priorities and direction)
- **Created:** 2026-03-05

## Core Context

**Current Status (April 2026):** Phase 3 graph API opening scope gate approved; Phase 1 collections recovery complete; 351/351 tests passing.

**Key learnings:**
- `PortSet` GUID-backed `Hashtable` is part of XML persistence contract; key semantics ambiguous in docs vs. implementation
- Splitting interface changes from implementation changes isolates breaking-change risk classes
- Graph interfaces are tightly coupled to concrete `Edge`/`Vertex` types; no abstract seam (`IGraph`) exists yet
- Interface changes ripple to all implementers and consumers
- Phase boundaries must be hard and explicit; even Phase 1 recovery can suffer scope creep

## Learnings

### 2026-04-28 — Phase 3 Graph Interfaces Scope Gate Approved ✅

**Status:** Batch 1 (`p3-interfaces`) scoped and approved

**Learning:** Phase 3 graph interface modernization (`IEdge`, `IVertex`, `Edge`, `Vertex`) must be split into two batches:

**Batch 1 (`p3-interfaces`) — Interface signature changes ONLY:**
- `IVertex.PredecessorEdges` / `IVertex.SuccessorEdges` → `IReadOnlyList<Edge>`
- `IEdge.PreVertex` / `IEdge.PostVertex` → `IVertex?`
- `IEdge.ChildEdges` → `IReadOnlyList<Edge>`

All changes are source and binary breaking but covariant-safe. No serialization shape changes, no internal storage changes, no `GetParent()` signature change in Batch 1.

**Why it matters:** Splitting interface changes from implementation changes isolates breaking-change risk classes. Interface changes ripple to all implementers (`Task`, `Ligature`) and consumers (PFC, graph algorithms). Implementation changes (internal storage, serialization modernization) require separate XML round-trip validation and different subclass impact analysis. The split gives us a validation checkpoint before touching serialization contracts.

**Authorization:** Parker may proceed with Batch 1 only. Hudson adds regression tests first. Batch 2 requires separate scope gate.

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration log: `.squad/orchestration-log/2026-04-28T21-53-46Z-ripley.md`
- Session log: `.squad/log/2026-04-28T21-53-46Z-phase3-start.md`

---

**Archived history:** Detailed entries from March 2026–April 26, 2026 preserved in `ripley-history-archive.md`.
