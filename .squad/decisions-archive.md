# Squad Decisions - Archive

Historical entries archived from decisions.md (older than 30 days as of 2026-03-07).

---

### 2026-04-26: Phase 2 Utility-Wrapper Internals Migration (COMPLETE ✅)

**By:** Parker

**Date:** 2026-04-26

**Status:** Complete

**Decision:** Migrate `HashtableOfLists`, `WeakHashtable`, and `WeakList` onto generic collection internals (`Dictionary<TKey, TValue>`, `List<T>`) while preserving all public wrapper API shapes.

**Key Design Principle:** Keep the non-generic `HashtableOfLists` public surface intact by maintaining the scalar entry vs. wrapped-list distinction, which preserves legacy duplicate-suppression and prune-empty-list behavior without forcing public API changes.

**Implementation Details:**
- `HashtableOfLists`: Non-generic public wrapper; generic `Dictionary<TKey, TValue>` internals
- `WeakHashtable`: Generic dictionary storage with lazy cleanup (dead entries disappear on indexer/`Values` access)
- `WeakList`: Generic list storage with target-based semantics (`Contains`, `IndexOf`, `Remove`, `Insert`, `CopyTo`, `Collapse`)
- `WeakListEnumerator`: Enhanced to support generic `List<T>` enumerator

**Result:**
- Build: 0 errors, 0 warnings ✅
- Sage.csproj build: `dotnet build src\Sage\Sage.csproj --no-restore` ✅
- Wrapper regression tests: 14/14 passing ✅
- Full Sage.Tests: 281/281 passing ✅

**Scope Boundary:** Internals-only refactor; all public contracts unchanged.

---

### 2026-04-26: Phase 2 Utility-Wrapper Regression Test Semantics Lock (COMPLETE ✅)

**By:** Hudson

**Date:** 2026-04-26

**Status:** Complete

**Decision:** Lock Phase 2 utility-wrapper regression coverage to the following semantics while Parker's wrapper migration is in flight:

1. **`HashtableOfLists` (non-generic)** keeps its legacy behavior of de-duplicating identical values for the same key.
2. **`HashtableOfLists<TKey, TValue>` (generic)** keeps duplicate values, sorts each per-key list when constructed with a comparer, and leaves empty keyed lists in place until `PruneEmptyLists()`/enumeration.
3. **`WeakHashtable` cleanup** is still lazy: dead entries disappear when callers touch the indexer or `Values`, and enumeration exposes only live entries.
4. **`WeakList` operations** must stay target-based, not wrapper-instance-based: `Contains`, `IndexOf`, `Remove`, `Insert`, `CopyTo`, and `Collapse` all work on underlying targets.

**QA-Triggered Safe Fixes (to keep wrapper migration testable without changing intended behavior):**
- `WeakList.Insert(...)` wraps inserted items with `MyWeakReference` so inserted values behave the same as values added through `Add(...)`.
- `WeakList.Add(...)` returns the inserted index after the backing store migrated off `ArrayList`.
- `HashtableOfLists.Add(...)` uses a null-safe equality check to preserve duplicate-suppression behavior under the new dictionary-backed implementation.

**Why This Matters:** These tests are now the tripwire for Parker's Phase 2 batch. If the migration changes any of the above semantics, we treat that as an explicit behavior decision and not an accidental side effect.

**Result:**
- Wrapper regression tests: 14/14 passing ✅
- Full Sage.Tests: 281/281 passing ✅

**Scope Boundary:** Wrapper public contracts and backward-compatibility semantics locked via regression tests.

---

### 2026-03-17: Executive vs ExecutiveFastLight Causality Divergence Investigation (COMPLETE ✅)

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
