# Parker — Phase 3 Batch 1 (`p3-interfaces`)

**Date:** 2026-07-17

**Decision:** Land the Phase 3 interface break with explicit interface adapters in `Edge` and `Vertex`, rather than immediately changing the concrete public property types.

## Why

Ripley's scope gate for Batch 1 named the interface contracts only and explicitly warned against widening into `p3-edge-vertex`. Explicit interface implementation lets the interface break happen now while keeping the concrete-class blast radius narrow.

## Applied Shape

- `IEdge.PreVertex` / `PostVertex` → `IVertex?`
- `IEdge.ChildEdges` → `IReadOnlyList<Edge>`
- `IVertex.PredecessorEdges` / `SuccessorEdges` → `IReadOnlyList<Edge>`
- `Edge` keeps its current public `Vertex?` / `IList` members for now
- `Vertex` keeps its current public `IList` members for now

## Consequence for Later Batches

Later `p3-edge-vertex` work can still choose to align the concrete `Edge`/`Vertex` public properties with the new interface shapes, but that is now a separate, deliberate step instead of accidental fallout from Batch 1.
