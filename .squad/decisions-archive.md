# Squad Decisions - Archive

Historical entries archived from decisions.md (older than 30 days as of 2026-03-07).

---

### Upgrade Sage to .NET 10

**Author:** Parker (.NET Developer)  
**Date:** 2025-07-15  
**Branch:** `feature/dotnet10`  
**Status:** Ready for review  
**Requested by:** Stuart Hillary

**Decision:** Upgrade target framework from `net8.0` to `net10.0`.

**What Changed:**
- `Directory.Build.props`: `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net10.0</TargetFramework>`

**Build Outcome:** ✅ Build succeeded — 0 errors, 2826 pre-existing warnings (all CA1xxx/CA5xxx Roslyn diagnostics, none new).

**Next Steps:**
1. Upgrade stale test NuGet packages (Microsoft.NET.Test.Sdk 16.7.1→17.x+, MSTest 2.1.1→3.x, coverlet.collector 1.3.0→6.x)
2. Run test suite on .NET 10
3. Address pre-existing CA analyzer warnings (technical debt)

---

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

---
