# Squad Decisions

## Active Decisions

### Architectural Modernization: Executive Event Queue (Priority 1)

**Author:** Ripley (Lead / Architect)  
**Date:** 2025-07-15  
**Status:** Pending  
**Impact:** High — affects simulation performance across all models

**Decision:**
Replace O(n) SortedList in Executive event queue with indexed priority queue (heap-based or binary search tree).

**Rationale:**
- Current implementation scans entire list on each insertion/deletion
- Codebase contains 600+ legacy collection usage patterns (not fully type-generic)
- Modernization enables nullable reference type support
- ExecController is the visualization seed; event queue performance cascades to UI responsiveness

**Scope:**
- Core/Executive event handling
- Impact assessment on Scheduling, Resources, SystemDynamics modules

**Next Steps:**
1. Prototype priority queue implementation
2. Benchmark against current O(n) behavior
3. Integration testing with full model execution
4. Migrate remaining legacy collections

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
