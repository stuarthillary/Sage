# Squad Decisions

## Active Decisions

### 2026-04-28: Phase 3 Graph API Opening — Regression / Compile Fallout Map (COMPLETE ✅)

**By:** Hudson

**Date:** 2026-04-28

**Status:** Complete

**Summary:** Mapped the current regression and compile fallout surface for the opening Phase 3 graph API batch focused on `IEdge`, `IVertex`, `Edge`, and `Vertex`.

**Decision:** Do **not** add new characterization tests yet. The current graph interfaces are still tightly coupled to concrete `Edge`/`Vertex` types, so any low-level signature test would only handcuff the intended API break instead of protecting stable behavior.

**Findings:**

1. `IGraph` currently has no in-repo footprint — `src\Sage\Graphs\IGraph.cs` does not exist
2. Interfaces are not abstract seams yet — `IEdge` exposes concrete `Vertex?` types; `IVertex` exposes concrete `Edge?`
3. Production compile hotspots (12 files): `ChannelMonitor.cs`, `MultiChannelEdgeReceiptManager.cs`, `Ligature.cs`, `PathLength.cs`, `VertexSynchronizer.cs`, `DagCycleChecker.cs`, `DagDeadlockChecker.cs`, `DagStructureError.cs`, `CPMAnalyst.cs`, `PertAnalyst.cs`, `DiagnosticAids.cs`, `Task.cs`
4. Test fallout hotspots (7 files): `TestDAGCycleChecker.cs`, `TestGraphBranching.cs`, `TestGraphAlgorithmRegressions.cs`, `TestGraphValidities.cs`, `TestGraphPersistence.cs`, `TestTasks.cs`, `TestTasks2.cs`

**Validation:**
- Baseline build: ✅ Clean
- Baseline tests: ✅ Passing

---

### 2026-04-28: Phase 3 Graph Interfaces — Breaking API Batch 1 Scope Gate (ACTIVE ✅)

**By:** Ripley

**Date:** 2026-04-28

**Status:** Active — First batch scoped, Parker ready to implement

**Decision:** Split `p3-interfaces` and `p3-edge-vertex` into two separate batches:

**Batch 1 (this scope gate): `p3-interfaces` — Interface signature changes ONLY**

Parker is authorized to change the following **interface signatures only**, with no implementation/serialization changes:

1. **`IVertex.PredecessorEdges` and `IVertex.SuccessorEdges`** → `IReadOnlyList<Edge>`
   - Current: `IList`
   - Breaking changes: Source/binary breaking for callers using mutating methods
   - Implementation: Properties already return `PreEdges.AsReadOnly()` and `PostEdges.AsReadOnly()`
   - No internal storage changes required

2. **`IEdge.PreVertex` and `IEdge.PostVertex`** → `IVertex?`
   - Current: `Vertex?`
   - Breaking changes: Source/binary breaking for callers casting to concrete `Vertex`
   - Implementation: Covariance-safe, properties can return as `IVertex?`
   - No internal field type changes required

3. **`IEdge.ChildEdges`** → `IReadOnlyList<Edge>`
   - Current: `IList`
   - Breaking changes: Source/binary breaking for callers using mutating methods
   - Implementation: Replace `ArrayList.ReadOnly(ArrayList.Adapter(_childEdges))` with `.AsReadOnly()`
   - No serialization changes required

**Rationale for Split:**
- Binary/source breaking scope isolation
- Risk tiering: Interface vs implementation changes are different risk classes
- XML serialization boundary: Don't change serialization shape and interface in same batch
- Test/validation checkpoint: Validate interface changes before internal storage changes
- Subclass exposure: `Task : Edge` and `Ligature : Edge` are in the wild

**Explicitly Out of Scope for Batch 1:**
- ❌ `IEdge.GetParent()` signature change
- ❌ Internal storage modernization in `Vertex` or `Edge`
- ❌ XML serialization shape changes
- ❌ `IVertex.PrincipalEdge` return type change
- ❌ `Vertex.AddPreEdge()` / `AddPostEdge()` parameter types

**Migration Order:**
1. Update `IVertex` interface
2. Update `IEdge` interface
3. Update `Vertex` class
4. Update `Edge` class
5. Fix consumer compilation errors
6. Run targeted test suite

**Success Criteria:**
- ✅ All graph interface return types use `IReadOnlyList<T>` or covariant `IVertex`/`IEdge`
- ✅ Zero changes to serialization shape
- ✅ Zero changes to internal storage types
- ✅ All graph tests pass
- ✅ Subclasses compile and pass tests
- ✅ PFC integration tests pass

**Authorization:**
- Parker: Proceed with Batch 1 (`p3-interfaces`) only
- Hudson: Add regression tests before Parker starts
- Coordinator: Do not approve Batch 2 until Batch 1 is validated

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

---

### 2026-04-26: Phase 2 Graph Algorithms — Internal Modernization, Public Surface Preservation (COMPLETE ✅)

**By:** Parker

**Date:** 2026-04-26

**Status:** Complete

**Context:** Phase 2 graph-algorithms slice targeting `src\Sage\Graphs\PertAnalyst.cs`, `CPMAnalyst.cs`, and `DagDeadlockChecker.cs`.

**Decision:** Treat this batch as **internal collection modernization only**. Preserve public/protected legacy collection shapes where they are part of the contract or likely subclass touchpoints.

**Applied Guardrails:**
- Kept `PertAnalyst.CriticalPath` returning `ArrayList`
- Did not change `DagDeadlockChecker.GetSuccessors(object)` or `Errors`
- Did not convert `CPMAnalyst` protected `Hashtable` fields in this batch
- Continued honoring intentional public `IDictionary graphContext` signatures elsewhere

**Implementation Pattern:** Prefer `List<T>`, `HashSet<T>`, and `Dictionary<TKey,TValue>` behind existing boundaries, adapting back to legacy collection types only at the API edge when needed.

**Validation:** `dotnet build .\src\Sage\Sage.csproj --no-restore` and targeted graph tests for `GraphValidityTester` + `DAGCycleCheckerTester` passed.

**Result:**
- Build: 0 errors, 0 warnings ✅
- Graph algorithm tests: 12/12 passing ✅

---

### 2026-04-26: Hudson Graph Algorithm Regression Coverage (COMPLETE ✅)

**By:** Hudson

**Date:** 2026-04-26

**Status:** Complete

**Context:** Adding regression test coverage for Phase 2 graph-algorithms batch to lock legacy public surfaces during internal modernization.

**Decision:** Add comprehensive regression tests for `CpmAnalyst`, `PertAnalyst`, and `DagDeadlockChecker` in `tests\SageTestLib\TestGraphAlgorithmRegressions.cs`.

**Test Semantics Locked:**
- `PertAnalyst.CriticalPath` remains read-only `ArrayList`
- `DagDeadlockChecker.Errors` stays read-only/non-generic at the boundary
- Duplicate successor references must not create duplicate frontier/error targets
- Simple reachable cycle must report one residual frontier target
- Implementation-sensitive ordering/exhaustiveness for complex deadlock sets left intentionally flexible

**Result:**
- New regression tests: 4/4 passing ✅
- Targeted graph algorithm suite: 13/13 passing ✅
- Full `Sage.Tests`: 285/285 passing ✅

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
