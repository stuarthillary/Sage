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

### 2026-04-26: Phase 2 PortSet/Resources Scope Gate (COMPLETE ✅)

**By:** Ripley

**Date:** 2026-04-26

**Status:** Complete — Scope gate approved with hard exclusions

**Decision:** Parker may proceed with a **narrow Phase 2 batch** limited to signature-preserving internal modernization in `ResourceManager`, `PortSet`, and `MultiKeyAccessRegulator` **only where behavior, serialized shape, and public collection types remain unchanged**.

**Approved Changes:**
- Private-field/internal helper cleanups that do **not** change any public/member signatures
- `PortSet` private non-persisted helper/listener cleanups preserving GUID-backed storage, public constructors, `ICollection PortKeys`, indexer/event behavior, and XML payload under `"Ports"`
- `ResourceManager` internal maintenance excluding changes to `public IList Resources`, constructors, XML field names, or waiter wake order / priority semantics
- `MultiKeyAccessRegulator` internal cleanup keeping constructor signature `MultiKeyAccessRegulator(object subject, ArrayList keys)` and `.Equals(...)`-based membership semantics

**Explicitly Rejected:**
- ❌ `PortSet` key-semantics work: No constructor semantic correction, no switch from `Hashtable` to `Dictionary<,>`, no change to name lookup semantics or XML persistence format
- ❌ Public collection/API shape changes: No `ArrayList`→`IList<T>` constructor/property changes, no change to `IResourceManager.Resources : IList`
- ❌ `ResourceManager` persistence/waiter behavior changes: No change to serialized `"Resources"` payload, no change to deserialization assumptions, no change to waiter ordering/resumption policy

**Rationale:** `PortSet` persists the raw `Hashtable`, and the serializer recreates it as a plain `Hashtable`; comparer/key-semantics changes can silently alter deserialized behavior. `ResourceManager.Resources` is an explicit public `IList` contract and waiter ordering is simulation-behavior-critical. These areas require dedicated characterization before broader refactors.

**Result:**
- Scope gate approved
- Parker proceeded with implementation within boundary
- Hudson added regression coverage
- Build: 0 errors, 0 warnings ✅
- All targeted tests passed ✅

---

### 2026-04-26: PortSet/Resources Phase 2 Regression Map (COMPLETE ✅)

**By:** Hudson

**Date:** 2026-04-26

**Status:** Complete

**Decision:** Add only behavior-safe regression nets now, and leave ambiguous PortSet/resource semantics unpinned until Parker gets guidance.

**Tests Added (5):**
1. `PortSet_AddRemoveAndClear_UpdateLookupsAndTypedViews`
2. `PortSet_DuplicateInstanceIsIgnored_ButDuplicateNameThrows`
3. `PortSet_PortAddedAndRemovedEventsFireOncePerMutation`
4. `TestResourceManagerManagerLinksAndLifecycleEvents`
5. `TestMultiKeyAccessRegulatorMatchesOnKeyAndSymmetricSubjectEquality`

**Why these were safe:** These assertions match explicit, local behavior in `PortSet`, `ResourceManager`, and `MultiKeyAccessRegulator` without choosing among conflicting higher-level interpretations. They give Parker a regression tripwire for collection-migration work while staying out of product-contract fights.

**Coverage still needed before broader PortSet/resources refactors:**
1. **PortSet case-sensitivity contract** — decide whether name lookup is supposed to be case-sensitive or case-insensitive; current constructor flag/docs and implementation disagree
2. **PortSet ordering contract** — decide whether enumeration and integer indexing are allowed to be hashtable-order dependent or must be stable by insertion/index/sort key
3. **Port event fan-out semantics** — clarify whether rejected-data listeners should be distinct from presented-data listeners before changing wiring
4. **ResourceManager explicit-selection semantics** — decide what `ResourceSelectionStrategy` must do beyond returning a non-null choice
5. **ResourceManager absent-remove semantics** — decide whether removing a resource not in the pool should be a no-op, warning, or lifecycle event

**Validation:**
- Targeted port/resource regression slice: 33 tests passed ✅
- Full `tests\SageTestLib\Sage.Tests.csproj`: all tests passed ✅

---

### 2026-04-26: Phase 2 PortSet/Resources Implementation — Signature-Preserving Internal Cleanup (COMPLETE ✅)

**By:** Parker

**Date:** 2026-04-26

**Status:** Complete

**Decision:** Treat this batch as internal collection cleanup only, honoring Ripley's scope boundary.

**Applied Changes:**

**PortSet (`src\Sage\ItemBased\PortSet.cs`)**
- Private listener lists moved from `ArrayList` to typed `List<EventHandler<PortEventArgs>>`
- Typed clear snapshots for safe event iteration
- Preserved: GUID-backed storage behavior, current public constructor set, `ICollection PortKeys`, indexer/event behavior, XML payload under `"Ports"`

**MultiKeyAccessRegulator (`src\Sage\Resources\MultiKeyAccessRegulator.cs`)**
- Constructor keys copied into private generic `List<object>`
- Internal key storage now strongly typed
- Preserved: public `ArrayList` constructor signature, `.Equals(...)`-based membership semantics, null/subject behavior

**ResourceManager (`src\Sage\Resources\ResourceManager.cs`)**
- Internal snapshots use typed `List<IResource>` with pre-sized allocation
- Deserialization list handling modernized
- Preserved: `public IList Resources`, waiter ordering, priority semantics, serialized shape

**Explicit Deferrals Honored:**
- ❌ `PortSet` `Hashtable` storage: untouched (preserves key semantics and XML payload)
- ❌ `ResourceManager.RscWaiterList` and `Resources`: untouched (preserves waiter ordering and public collection shape)
- ❌ Public API shapes: unchanged

**Validation:**
- Build: 0 errors, 0 warnings ✅
- Targeted PortSet/resources regression tests: 7/7 passed ✅
- Full test suite: all tests passed ✅

**Result:** Implementation complete and verified. Phase 2 batch shippable without persistence or scheduling behavior changes.

---

### 2026-07-17: Collections Recovery Scope Reset (COMPLETE ✅)

**By:** Ripley

**Date:** 2026-07-17

**Status:** Complete

**Decision:** Keep the current collections recovery pass inside **Phase 1 only**:
- Retain signature-preserving private/internal collection migrations already in progress
- Revert Graph algorithm, PFC, Materials, and known wrapper/public-surface-adjacent changes from this pass
- Treat ambiguous files as out-of-scope until separately coordinated

**Files explicitly out-of-scope for this recovery:**
- `src\Sage.Materials\**`
- `src\Sage.PFC\PfcAnalyst.cs`
- `src\Sage\Dependencies\GraphSequencer.cs`
- `src\Sage\Graphs\**`
- `src\Sage\ItemBased\PortSet.cs`
- `src\Sage\Utility\WeakHashTable.cs`, `WeakList.cs`, `HashtableOfLists.cs`

**Rationale:** The branch's active build breaks were coming from graph-analysis changes (Phase 2 bucket), not from Phase 1 recovery work. Constraining to internal/private conversions preserves forward progress while avoiding accidental public contract churn.

**Result:**
- Build: 0 errors, 0 warnings
- Sage.Tests: 271/271 passing
- Sage.Materials.Tests: 22/22 passing
- Sage.PFC.Tests: 58/58 passing
- Total: 351/351 ✅

---

### 2026-07-17: Sample Code `[Order]` Attribute for Intentional Run Order (COMPLETE ✅)

**By:** Parker

**Date:** 2026-07-17

**Status:** Complete

**Context:** `samples\Sage_SampleCode` was refactored to use an `IExample` interface + reflection-based discovery. The runner (`Program.cs`) ordered examples alphabetically by `FullName`, which broke the original intentional progression from basic to advanced concepts.

**Decision:** Introduce an `[Order(n)]` attribute to restore the original execution order without hard-coding the example list in `Program.cs`.

**Implementation:**
- **New file:** `samples\Sage_SampleCode\OrderAttribute.cs` — `internal sealed class OrderAttribute : Attribute` with `int Value` property
- **31 classes annotated:** Orders 1–16 (Executive), 17–19 (StateManagement), 20–21 (RandomServer), 22–24 (StateMachine), 25–26 (Model), 27–29 (Resources), 30 (SequenceControl), 100 (DefaultModelWithSelfManagingModelObjects — auto-discovered extra)
- **Program.cs:** `OrderBy(t => t.GetCustomAttribute<OrderAttribute>()?.Value ?? int.MaxValue).ThenBy(t => t.FullName)`
- **Namespace resolution:** `OrderAttribute` lives in `Highpoint.Sage.Examples`; C# compiler resolves parent namespace types without explicit `using` directives

**Result:**
- Build: 0 errors, 0 warnings ✅
- Examples execute in original intentional order

---

### 2026-07-17: Materials Subsystem Extraction into Sage.Materials (COMPLETE ✅)

**By:** Parker

**Date:** 2026-07-17

**Status:** Complete

**Scope:** Extracted 66 source files from `src\Sage\Materials\` (Chemistry, Emissions, Thermodynamics, VaporPressure) into standalone class library `src\Sage.Materials\`. Moved 2 test files to `tests\Sage.Materials.Tests\`.

**Blocker Resolutions:**

1. **EmissionsServiceOptions** — Defined in `SageOptions.cs` in namespace `Highpoint.Sage.Materials.Chemistry.Emissions`, referenced `IEmissionModel` (Materials type). Moved to `src\Sage.Materials\Emissions\EmissionsServiceOptions.cs`. Removed from `SageOptions.cs`.

2. **DiagnosticAids.DumpMaterial** — Contained `DumpMaterial(IMaterial)`, `Dump(Mixture)`, `Dump(Substance)` depending on Materials types. Removed from `DiagnosticAids.cs` along with Materials `using` directives. Created `MaterialDiagnosticAids` static class in `src\Sage.Materials\MaterialDiagnosticAids.cs` for future callers.

**Project Reference Topology:**
```
Sage (core) ← Sage.Materials ← Sage.Materials.Tests ← Sage.Scratch
```
No circular references.

**Result:**
- Build: 0 errors, 0 warnings
- Sage.Tests: 271/271 passing
- Sage.PFC.Tests: 58/58 passing
- Sage.Materials.Tests: 22/22 passing
- Total: 351/351 ✅

---

### 2026-07-17: IExample Interface for Sage_SampleCode (COMPLETE ✅)

**By:** Parker

**Date:** 2026-07-17

**Status:** Complete

**Context:** `samples\Sage_SampleCode` had ~30 example classes with `public static void Run()` methods. `Program.cs` registered them manually with hardcoded `Demonstrate(SomeClass.Run)` calls, requiring developer edit to `Main()` for each new example.

**Decision:** Introduce `IExample` interface with `void Run()` instance method. All example classes implement it. `Program.cs` uses reflection to discover and run them automatically.

**Changes:**
- **New file:** `IExample.cs` — `internal interface IExample { void Run(); }`
- **31 example classes:** Added `: IExample`, converted `public static void Run()` → `public void Run()`
- **10 classes:** Removed `static` from class declaration (files 4–7 used `public static class`)
- **Program.cs `Main()`:** Reflection-based discovery with alphabetical ordering
- **`Demonstrate(Action)`** → **`Demonstrate(IExample)`**

**Build Result:**
- 0 errors, 0 warnings ✅
- All 351 tests pass

---

### 2026-07-17: Materials Extraction - Committed (COMPLETE ✅)

**Status:** Complete

**Commit SHA:** 9f9a4e8

**Message:** Extract Materials subsystem to Sage.Materials class library
- Move 65+ source files from src/Sage/Materials/ to new src/Sage.Materials/
- Create Sage.Materials.csproj referencing core Sage library
- Create tests/Sage.Materials.Tests/ with 22 test cases
- Move EmissionsServiceOptions out of SageOptions.cs into new project
- Remove DumpMaterial methods from DiagnosticAids.cs (moved to MaterialDiagnosticAids.cs)
- Update Sage.slnx to include both new projects
- All 351 tests pass (271 Sage + 58 PFC + 22 Materials)

**Tests:** 351/351 passing ✅
**Branch:** feature/dotnet10
