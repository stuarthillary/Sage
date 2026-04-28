## Core Context

**Q1 2026 — Foundation & Framework Refactor**

Throughout Q1 2026, Parker led the infrastructure modernization foundation work:
- Project renaming series (TestDriver→Sage.Scratch, SageBenchmarks→Sage.Benchmarks, Examples namespace standardization) establishing team naming conventions
- Comprehensive solution restructure to src/tests/benchmarks/samples directory layout  
- Namespace migration: Highpoint.Sage.SimCore → Highpoint.Sage.Core across all modules
- Phase 1 collection-type conversions in Core, Utility, Scheduling, SmartPropertyBag
- Nullable reference type (CS8xxx) analysis and multi-phase migration planning

**Q2 2026 — Framework Modernization Complete**

From mid-Q2 onward, Parker completed major framework upgrades:
- MSTest → xUnit 2.x full migration (60 test files)
- All CS8xxx nullable warnings eliminated (1,244 total, all modules)
- Thread-safety bug fix in ParticipantDirectory._knownMacros (static dictionary race condition)
- PFC extraction to Sage.PFC standalone library (53 files + tests)
- Materials extraction to Sage.Materials standalone library (66 files + tests)
- 8 project renames completed: TestDriver→Sage.Scratch, SageBenchmarks→Sage.Benchmarks, Sage_SampleCode→Sage.Examples, SageTestLib→Sage.Tests
- Phase 2 public API collection modernization across Core, PFC, and Materials modules
- Phase 3 Batch 1 (`p3-interfaces`) graph interface signatures updated
- Phase 2 graph algorithm internal collection modernization (PertAnalyst, CPMAnalyst, DagDeadlockChecker)
- Phase 3 Resource Manager (`p3-resource-manager`) typed read-only collection surfaces
- Phase 3 Reactions (`p3-reactions`) typed read-only public surfaces

---

## Learnings

### 2026-04-28 — Phase 3 Resource Manager Batch: Typed Read-Only Collection Surfaces (`p3-resource-manager`) ✅

**Status:** Complete

**Mandate:** Implement the first `p3-resource-manager` slice as a direct public-surface break only, staying within Ripley's typed read-only scope gate.

**Changes Applied:**

**Resource Manager & Implementations**
- `ResourceManager.Resources`: Changed from `IList` to `IReadOnlyList<IResource>`, exposing `_resources.AsReadOnly()` directly
- `SelfManagingResource.Resources`: Changed to `IReadOnlyList<IResource>`, forwarding from wrapped ResourceManager
- `MaterialResourceItem.Resources`: Changed to `IReadOnlyList<IResource>`, returning `Array.AsReadOnly(new IResource[] { this })`
- `IResourceManager.Resources`: Interface updated to `IReadOnlyList<IResource>`
- `IResourceManagerCollection.GetResourceManagers()` / `ResourceManagerCollection.GetResourceManagers()`: Changed from `ICollection` to `IReadOnlyCollection<IResourceManager>`

**Test Updates**
- `tests\SageTestLib\TestResources.cs`: Moved off `IList.Contains(...)` patterns to typed collection assertions

**Deferrals Honored:**
- ❌ `Reserve`, `Acquire`, `Unreserve`, `Release` behavior: unchanged
- ❌ Waiter ordering and prioritized request semantics: unchanged
- ❌ Event delegate shapes: unchanged
- ❌ Add/Remove/Clear mutators: unchanged
- ❌ Indexer semantics: unchanged
- ❌ Constructor/serialization identity work: deferred
- ❌ XML payload/shape: unchanged
- ❌ Versioning: deferred

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet build .\src\Sage.Materials\Sage.Materials.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore` ✅
- Targeted resource/server regression slice ✅

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration logs: `.squad/orchestration-log/2026-04-28T22-58-19Z-parker.md`
- Session log: `.squad/log/2026-04-28T22-58-19Z-p3-resource-manager.md`

**Outcome:** Opening resource-manager batch complete. Implementation stays inside the scoped typed read-only public surface break with direct fallout fixes in place. Staged for review and approval.

---

### 2026-04-28 — Phase 3 Reactions Batch: Typed Read-Only Public Surfaces (`p3-reactions`) ✅

**Status:** Complete

**Mandate:** Implement the opening reactions batch as a typed read-only public-surface pass only, staying within Ripley's scope gate.

**Changes Applied:**

**Reaction (`src\Sage.Materials\Chemistry\Reaction.cs`)**
- `Reactants` property: Changed from untyped collection to `IReadOnlyList<Reaction.ReactionParticipant>`
- `Products` property: Changed from untyped collection to `IReadOnlyList<Reaction.ReactionParticipant>`

**ReactionProcessor (`src\Sage.Materials\Chemistry\ReactionProcessor.cs`)**
- `Reactions` property: Changed to `IReadOnlyList<Reaction>`
- `GetReactionsByParticipant()`: Now returns `IReadOnlyList<Reaction>`
- `GetReactionsByReactant()`: Now returns `IReadOnlyList<Reaction>`
- `GetReactionsByProduct()`: Now returns `IReadOnlyList<Reaction>`
- `CombineMaterials(...)`: Typed observable outputs for `out observedReactions` and `out observedReactionInstances`

**ReactionInstance (`src\Sage.Materials\Chemistry\ReactionInstance.cs`)**
- String rendering: Removed casts/indexing through legacy non-generic lists; now uses typed read-only outputs

**TestChemistry (`tests\SageTestLib\TestChemistry.cs`)**
- Moved off `ArrayList observedReactions, observedReactionInstances` to typed read-only outputs from `CombineMaterials()`

**Deferrals Honored:**
- ❌ Reaction math and catalyst handling: unchanged
- ❌ Event sequencing: unchanged
- ❌ XML field-name or payload-shape: unchanged
- ❌ Constructor/deserialization seams: unchanged
- ❌ Resource-manager/versioning work: deferred

**Validation:**
- `dotnet build .\src\Sage.Materials\Sage.Materials.csproj --no-restore` ✅
- `dotnet test .\tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj --no-restore` ✅ (22/22)
- Targeted regression slice ✅ (28/28)

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration log: `.squad/orchestration-log/2026-04-28T22-31-29Z-parker.md`
- Session log: `.squad/log/2026-04-28T22-31-29Z-p3-reactions.md`

**Outcome:** Opening reactions batch complete and staged for review. Implementation stays inside the scoped typed collection/query break with direct fallout fixes in place.

---

### 2026-04-26 — Phase 2 PortSet/Resources Batch: Signature-Preserving Internal Cleanup ✅

**Status:** Complete

**Mandate:** Implement final Phase 2 batch within Ripley's scope boundary. Signature-preserving internal collection cleanup only.

**Changes Applied:**

**PortSet (`src\Sage\ItemBased\PortSet.cs`)**
- Private listener lists: `ArrayList` → `List<EventHandler<PortEventArgs>>`
- Typed clear snapshots for safe event iteration
- Preserved: GUID-backed storage, public constructors, `ICollection PortKeys`, indexer/event behavior, XML payload

**MultiKeyAccessRegulator (`src\Sage\Resources\MultiKeyAccessRegulator.cs`)**
- Constructor keys: now copied into private `List<object>`
- Internal key storage: strongly typed
- Preserved: public `ArrayList` constructor signature, `.Equals()`-based membership semantics

**ResourceManager (`src\Sage\Resources\ResourceManager.cs`)**
- Internal snapshots: typed `List<IResource>` with pre-sized allocation
- Deserialization list handling: modernized
- Preserved: `public IList Resources`, waiter ordering, priority semantics

**Deferrals Honored:**
- ❌ PortSet `Hashtable` persistence format: unchanged
- ❌ ResourceManager waiter ordering: unchanged
- ❌ Public API shapes: unchanged

**Validation:**
- Build: 0 errors, 0 warnings ✅
- Targeted PortSet/resources regression tests: 7/7 passed ✅
- Full test suite: all tests passed ✅

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration log: `.squad/orchestration-log/2026-04-26T13-43-08Z-parker.md`
- Session log: `.squad/log/2026-04-26T13-43-08Z-phase2-final-batch.md`

**Outcome:** Phase 2 implementation complete and verified. Scope boundaries honored. Shippable without persistence or scheduling behavior changes.

---

### 2026-07-17 — Phase 2 Graph Algorithms Slice: Internal Collections Only ✅

- **Scope:** Started the graph-algorithms Phase 2 pass in `PertAnalyst`, `CPMAnalyst`, and `DagDeadlockChecker` with collection modernization limited to private/internal implementation details.
- **Public API guardrails honored:** Kept `PertAnalyst.CriticalPath` as `ArrayList`, left intentional non-generic public/protected surface shapes alone, and avoided touching `IDictionary graphContext` patterns.
- **PertAnalyst:** Swapped the internal critical-path accumulator from `ArrayList` to `List<Edge>` and wrapped it back to a read-only `ArrayList` at the property boundary.
- **DagDeadlockChecker:** Replaced internal dedupe/lookup work with `HashSet<Node>` and replaced predecessor construction `Hashtable`/`ArrayList` plumbing with `Dictionary<Node, HashSet<Node>>`, preserving `GetSuccessors`/`Errors` legacy surface.
- **CPMAnalyst:** Updated the private contemporaneous-vertex traversal helper to use `List<Vertex>` + `HashSet<Vertex>` instead of `ArrayList`.
- **Validation:** `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅ and targeted graph tests `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore --filter "FullyQualifiedName~GraphValidityTester|FullyQualifiedName~DAGCycleCheckerTester"` ✅ (12/12).

### 2026-07-17 — Sample Code `[Order]` Attribute for Intentional Run Order ✅

- **Scope:** Added `OrderAttribute` to `samples\Sage_SampleCode` to restore the original intentional demo execution order after IExample + reflection-based discovery was introduced.
- **New file:** `OrderAttribute.cs` — `internal sealed class OrderAttribute : Attribute` with a single `int Value` property, `[AttributeUsage(AttributeTargets.Class, Inherited = false)]`.
- **31 classes annotated:** 16 Executive (orders 1–16), 3 StateManagement (17–19), 2 RandomServer (20–21), 3 StateMachine (22–24), 2 Model (25–26), 3 Resources (27–29), 1 SequenceControl (30). `DefaultModelWithSelfManagingModelObjects` (auto-discovered extra) assigned order 100.
- **Program.cs:** Replaced `OrderBy(t => t.FullName)` with `OrderBy(t => t.GetCustomAttribute<OrderAttribute>()?.Value ?? int.MaxValue).ThenBy(t => t.FullName)` for stable secondary sort.
- **Namespace resolution:** `OrderAttribute` lives in `Highpoint.Sage.Examples`; all example files use sub-namespaces. C# compiler resolves parent namespace types without explicit `using` directives — no import changes needed.
- **Build:** 0 errors, 0 warnings. ✅

---
### 2026-07-17 — Materials Extraction into Sage.Materials Standalone Library ✅

- **Scope:** Extracted all 66 files from `src\Sage\Materials\` (and subdirs Chemistry, Emissions, Thermodynamics, VaporPressure) into new `src\Sage.Materials\` class library. Moved 2 test files from `tests\SageTestLib\` to `tests\Sage.Materials.Tests\`.
- **Partial work inherited:** Project files, slnx entries, file deletions from Sage, and test file moves had all been done in a prior partial attempt. The work completed was: creating `MaterialDiagnosticAids.cs` and verifying the build.
- **Two blockers were pre-resolved:** `EmissionsServiceOptions` was already moved from `SageOptions.cs` to `src\Sage.Materials\Emissions\EmissionsServiceOptions.cs`. `DumpMaterial` and related helpers were already removed from `DiagnosticAids.cs` (Materials `using` directives also stripped), and `DiagnosticAids.DumpMaterial(...)` calls were already removed from the moved test files.
- **DiagnosticAids pattern:** `DumpMaterial` was in `DiagnosticAids` (Sage core) but depends on `IMaterial`, `Mixture`, `Substance`. After extraction, it would cause a Sage→Materials circular dep. Solution: remove from core, add `MaterialDiagnosticAids` static class in Materials project with same method. Partial extraction had already stripped test calls entirely rather than redirecting them.
- **Sage.Scratch:** Already had `Sage.Materials.Tests` project reference — no changes needed.
- **Results:** Build: 0 errors, 0 warnings. Sage.Tests: 271/271 passing. Sage.PFC.Tests: 58/58 passing. Sage.Materials.Tests: 22/22 passing. Total: 351/351 ✅

---


### 2026-07-17 — Materials Extraction into Sage.Materials Standalone Library ✅

- **Scope:** Extracted 65 files from `src\Sage\Materials\` into a new `src\Sage.Materials\` class library. Moved 2 test files (`TestMaterials.cs`, `TestMaterialService.cs`) from `tests\SageTestLib\` to `tests\Sage.Materials.Tests\`.
- **CS0436 trap (same as PFC):** Delete `src\Sage\Materials\` BEFORE building Sage.Materials. Order matters — if both projects compile the same types, CS0436 floods.
- **Inbound dep #1 — EmissionsServiceOptions:** `SageOptions.cs` in Core contained `EmissionsServiceOptions` class in a `namespace Highpoint.Sage.Materials.Chemistry.Emissions` block (no using statements needed — just a namespace declaration). Moved verbatim to `src\Sage.Materials\Emissions\EmissionsServiceOptions.cs` and removed the block from `SageOptions.cs`. SageOptions.cs now has zero Materials dependency.
- **Inbound dep #2 — DiagnosticAids:** `DumpMaterial`, `Dump(Mixture)`, `Dump(Substance)` methods in `DiagnosticAids.cs` were only called from `TestMaterials.cs` (which moved). Removed them from DiagnosticAids.cs along with `using Highpoint.Sage.Materials` and `using Highpoint.Sage.Materials.Chemistry` directives. Also removed the DumpMaterial call sites from TestMaterials.cs (they were diagnostic print statements, not assertions).
- **Sage.Scratch dependency:** `Sage.Scratch.csproj` already had a reference to `Sage.csproj`; no additional references needed for Materials since Sage.Scratch doesn't instantiate Materials test classes directly.
- **Results:** Build: 0 errors, 0 warnings. Sage.Materials.Tests: 22/22 passing. Sage.PFC.Tests: 58/58 passing. Sage.Tests: 271/271 passing. Total: 351/351.

---

### 2026-07-17 — PFC Extraction into Sage.PFC Standalone Library ✅

- **Scope:** Extracted all 53 files from `src\Sage\Graphs\PFC\` into a new `src\Sage.PFC\` class library project. Moved 5 PFC test files + 2 XML test data files from `tests\SageTestLib\` to `tests\Sage.PFC.Tests\`.
- **Key finding:** Hudson's analysis was accurate — zero inbound refs from the rest of Sage to PFC. Deletion was safe.
- **CS0436 trap:** New Sage.PFC project initially had 1554 CS0436 errors because PFC types still existed in Sage (referenced assembly). Fix: delete PFC files from Sage first, then build Sage.PFC. Order matters.
- **Sage.Scratch dependency:** Driver.cs in the Scratch project (tests\TestDriver\) directly instantiated `PfcAnalystTester` and `PFCGraphTester` using their fully qualified names from `Highpoint.Sage.Tests.Graphs.PFC` and `Highpoint.Sage.Graphs.PFC` namespaces. Had to add `Sage.PFC.csproj` and `Sage.PFC.Tests.csproj` project references to `Sage.Scratch.csproj`.
- **RootNamespace:** Set to `Highpoint.Sage` (same as parent Sage project) — NOT `Highpoint.Sage.Graphs.PFC`. The namespace on each file stays unchanged; RootNamespace just affects the default for new files added via IDE.
- **xsd file:** `ProcedureFunctionChart.xsd` content is inlined as a string literal in `ProcedureFunctionChart.cs` — not loaded at runtime as an embedded resource. Just copied as-is alongside source files.
- **Two `#if NYRFPT` test files** compile cleanly because all their content is inside the guard; the `NYRFPT` symbol is never defined.
- **Results:** Build: 0 errors. SageTestLib: 293/293 passing. Sage.PFC.Tests: 58/58 passing. Full solution clean.

---

### 2026-07-17 — ExecutiveFastLight `_execState` Never Transitioned ✅

- **Bug:** `_execState` was set to `Stopped` in `Reset()` and never changed — it stayed `Stopped` regardless of whether the executive was running or had finished normally.
- **Root cause:** `StartWcv()` and `StartWocv()` had no state transitions. `Start()` had no post-loop state setting.
- **Fix (4 changes):**
  1. `StartWcv()`: Added `_execState = ExecState.Running;` after `_runNumber++`
  2. `StartWocv()`: Added `_execState = ExecState.Running;` after `_runNumber++`
  3. `Start()`: After the dispatch returns, added `_execState = ExecState.Stopped` inside the `if (_stopRequested)` block; added `else { _execState = ExecState.Finished; }` to cover normal completion
  4. `RequestEvent()`: Added `ExecState.Finished` guard matching `Executive.cs` behavior — throws `ApplicationException` if called after the executive has finished
- **Pattern:** EFL is structurally simpler than `Executive` (no lock, no daemon event count in loop guard), but must mirror the same state machine. Always refer to `Executive.cs` as the reference implementation for state semantics.
- **Build/Test:** 0 errors, **344/344 tests passing**

---

### 2026-07-16 — Complete Nullable Migration — 0 CS8xxx Warnings ✅

- **Scope:** 1,244 CS8xxx nullable reference type warnings eliminated across all Sage modules
- **Approach:** Module by module, smallest-to-largest: Utility → Materials → SmartPropertyBag → Core → ItemBased → Resources → Graphs → Mathematics
- **Most common patterns per module:**
  - **Utility/Core:** CS8618 (uninitialized fields) fixed with `= null!`; CS8766/CS8767 interface mismatches on `IHasName.Name`, `IModelObject.InitializeIdentity`
  - **SmartPropertyBag:** CS8618 deferred-init fields; CS8600/CS8602 null dereferences from Hashtable lookups
  - **Mathematics:** CS8618 on distribution classes; CS8600/CS8602 from interpolator nullable chains; CS8714 on generic Dictionary key constraints (added `where T : notnull`)
  - **Resources:** CS8767 interface mismatches on `IResourceRequest.Reserve/Acquire`; CS8618 on ResourceManager fields
  - **ItemBased:** CS8618 on server/connector fields; CS8602 on nullable chain traversal in port proxies
  - **Graphs:** CS8600/CS8602 on CPMAnalyst edge/vertex lookups; CS8618 on ValidationService
- **Tricky cases:**
  - `IDictionary graphContext` and `object userData` in event delegates preserved intentionally
  - `CriticalPathAnalyst<T>`: added `where T : notnull` constraint to fix CS8714
  - `ProcedureFunctionChart.InitializeIdentity`: `model!` null-forgiving needed due to nullable model param
- **Bonus fix:** Discovered and fixed pre-existing thread-safety bug in `ParticipantDirectory._knownMacros` (static dictionary accessed without a lock caused `ArgumentException` under parallel xUnit execution). Added `_knownMacrosLock` for safe `TryGetValue + Add` pattern.
- **Build/Test:** 0 CS8xxx warnings; **319/319 tests passing**

---

### 2026-07-16 — Migrate Sage.Tests from MSTest to xUnit 2.x ✅

- **Scope:** Full test framework migration — 60 .cs files modified
- **Packages:** Removed MSTest.TestAdapter + MSTest.TestFramework; added xunit 2.9.3 + xunit.runner.visualstudio 2.8.2 (central package management in Directory.Packages.props)
- **Attribute pattern:** `[TestClass]` removed, `[TestMethod]` → `[Fact]`, `[TestInitialize]` → constructor (most classes already had ctors calling Init()), `[TestCleanup]` → `IDisposable.Dispose()`
- **Assert pattern:** All MSTest Assert.* calls converted to xUnit equivalents; note xUnit `Contains(item, collection)` has FLIPPED arg order vs MSTest; `Assert.IsInstanceOfType(obj, typeof(T))` → `Assert.IsAssignableFrom<T>(obj)` (not IsType — MSTest checks assignability)
- **Key fix — UnitTestDetector:** `UnitTestDetector.IsInUnitTest` previously checked only for MSTest assembly; updated to a lazy computed property that also detects xunit.core and xunit.execution assemblies. Static constructor approach was unreliable; lazy check resolves it.
- **Key fix — SageOptions:** `DefaultExecutiveType` assembly name corrected from `Highpoint.Sage` to `Sage` (actual AssemblyName in Sage.csproj)
- **Key fix — duplicate method:** TestExtensions.cs had duplicate `TestSigmaBounding` method renamed to `RunSigmaBoundingTest` (xUnit1024 rule)
- **Build/Test:** 0 errors; **319/319 tests passing** (all pre-existing EmissionModel failures resolved by UnitTestDetector fix)

---

### 2026-03-07 — TestDriver Project Rename to Sage.Scratch ✅

- **Scope:** Renamed TestDriver project from TestDriver.csproj to Sage.Scratch.csproj with namespace refactoring.
- **Project file:** 	ests\TestDriver\TestDriver.csproj → 	ests\TestDriver\Sage.Scratch.csproj (via git mv, history preserved)
- **RootNamespace/AssemblyName:** Set RootNamespace = Highpoint.Sage.Scratch and AssemblyName = Sage.Scratch in project file
- **Sage.slnx:** Updated project reference from TestDriver.csproj → Sage.Scratch.csproj
- **Namespace migration:** Driver.cs updated from 
amespace Highpoint.Sage.Testing → 
amespace Highpoint.Sage.Scratch
- **Build/Test:** dotnet restore + dotnet build Sage.slnx 0 errors; dotnet test Sage.Tests.csproj 316/319 passing (3 pre-existing failures)
- **Decision:** Scratch project now follows team naming convention (Sage.Scratch) and Highpoint.Sage.Scratch namespace convention, completing the entire project rename series

---
### 2026-03-07 — Project Renames: Benchmarks, Examples, Tests ✅

**Scope:** Three project renames coordinated as naming-convention rollout  
**Status:** All three renames complete; build clean, 319/319 tests passing

#### Benchmarks Rename: `SageBenchmarks.csproj` → `Sage.Benchmarks.csproj`
- Project file renamed via `git mv`
- AssemblyName: `Sage.Benchmarks` | RootNamespace: `Highpoint.Sage.Benchmarks`
- Updated `Sage.slnx` reference path
- EventDispatchBenchmarks.cs already used correct namespace
- Program.cs path comments updated

#### Examples Rename: `Sample.Examples.csproj` → `Sage.Examples.csproj` (correction)
- Previous rename to `Sample.Examples` was inconsistent with established pattern
- Corrected to `Sage.Examples.csproj` to align with main library (`Sage.csproj`) and benchmarks
- AssemblyName: `Sage.Examples` | RootNamespace: `Highpoint.Sage.Examples` (preserved)
- Updated `Sage.slnx` reference
- Folder `samples\Sage_SampleCode\` remains unchanged

#### Test Project Rename: `SageTestLib.csproj` → `Sage.Tests.csproj`
- Project file renamed via `git mv`
- AssemblyName: `Sage.Tests` | RootNamespace: `Highpoint.Sage.Tests`
- Updated `Sage.slnx` and `TestDriver.csproj` references

**Namespace Standardization in Test Files (8 files):**
- Scheduling tests (3 files): `SchedulerDemoMaterial` → `Highpoint.Sage.Tests.Scheduling`
- PFC tests (2 files): `PFCDemoMaterial`, `SageTestLib` → `Highpoint.Sage.Tests.Graphs.PFC`
- Utility tests (2 files): `SageTestLib` → `Highpoint.Sage.Tests.Utility`
- Mathematics tests (1 file): `SageTestLib` → `Highpoint.Sage.Tests.Mathematics`
- Updated cross-references in TestPfcAnalyst.cs (alias) and TestDriver/Driver.cs (7 fully-qualified types)

**Key Insight:** AssemblyName (short, for DLL output) and RootNamespace (full, for code) don't need to match. Main library uses `Highpoint.Sage` for both; other projects use shortened assembly names with full namespaces.

**Rationale:** Consolidates all four projects (main library, benchmarks, examples, tests) under consistent naming pattern `Sage.{Component}.csproj` with `Highpoint.Sage.{Component}` namespaces.

### 2026-07-16 — Rename namespace Highpoint.Sage.SimCore → Highpoint.Sage.Core ✅

- **Scope:** 268 files changed across src, tests, benchmarks, and samples
- **Replacements:** `Highpoint.Sage.SimCore` (full prefix) replaced in 259 .cs files via bulk replace
- **Partial qualifiers:** 9 additional files used bare `SimCore.X` references — fixed to `Core.X` or dropped prefix where `using Highpoint.Sage.Core;` was already present (IEdge.cs, IProcedureFunctionChart.cs)
- **Ambiguity fix:** `IProcedureFunctionChart` used `ICloneable` which became ambiguous between `Highpoint.Sage.Core.ICloneable` and `System.ICloneable`; qualified as `Core.ICloneable`
- **Config:** `tests\TestDriver\app.config` `ExecutiveType` value updated from `Highpoint.Sage.SimCore.Executive` → `Highpoint.Sage.Core.Executive`
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing
- **Decision:** Namespace for Core module is `Highpoint.Sage.Core` (not `SimCore`)



- **Project file:** `src\Sage\Sage4.csproj` → `src\Sage\Sage.csproj` (via `git mv`, history preserved)
- **RootNamespace:** `Highpoint.Sage` added to Sage.csproj `<PropertyGroup>`
- **AssemblyName:** Updated to `Highpoint.Sage` (and `SageOptions.cs` default type string + `TestExecutive.cs` hardcoded type string updated accordingly)
- **Sage.slnx:** Updated project entry from `Sage4.csproj` → `Sage.csproj`
- **ProjectReferences updated:** SageTestLib, TestDriver, SageBenchmarks, Sage_SampleCode
- **Namespace migration:** All namespaces now prefixed with `Highpoint.Sage.*` (e.g., `Highpoint.Sage.SimCore`)
- **Assembly name:** Output DLL is now `Highpoint.Sage.dll`
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing

### 2026-07-16 — Solution Restructure to src/tests/benchmarks/samples Layout ✅

- **Scope:** Entire repository layout restructured; .sln replaced with .slnx.
- **New layout:**
  - `src\Sage\` ← main library (was `Sage\`)
  - `tests\SageTestLib\` ← unit tests (was `Sage_Aux\SageTestLib\`)
  - `tests\TestDriver\` ← test runner (was `Sage_Aux\SageTesting\`)
  - `benchmarks\SageBenchmarks\` ← benchmarks (was `Sage_Aux\SageBenchmarks\`)
  - `samples\Sage_SampleCode\` ← samples (was `Sage_SampleCode\`)
- **ProjectReference updates:** All four consumer projects updated; key deltas:
  - `benchmarks\SageBenchmarks` → `..\..\src\Sage\Sage4.csproj`
  - `tests\SageTestLib` → `..\..\src\Sage\Sage4.csproj`
  - `tests\TestDriver` → `..\..\src\Sage\Sage4.csproj` (SageTestLib ref unchanged: `..\SageTestLib\...`)
  - `samples\Sage_SampleCode` → `..\..\src\Sage\Sage4.csproj`
- **slnx approach:** `dotnet sln migrate` (SDK 10.0.103) generated `Sage4-Everything.slnx` with old paths; paths updated manually and saved as `Sage.slnx`. Old `.sln` removed.
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing.
- **git mv:** All moves used `git mv` to preserve history.

### 2026-03-06 — Phase 1 Collection Migration Complete ✅

- **Status:** COMPLETE — 14 files modified across 4 modules
- **Scope:** Private/internal ArrayList/Hashtable → List<T>/Dictionary<TKey,TValue>/HashSet<T>
- **Modules:** Core (3 files), Materials (6 files), Resources (1 file), Graphs (4 files)
- **Serialization:** Maintained via ArrayList/Hashtable snapshots
- **Public API:** Preserved using adapter patterns (ArrayList.Adapter for IList returns)
- **Build:** ✅ Clean build, 316/316 tests passing (3 skipped Phase 2 prep)
- **Bug fixes:** 4 compilation issues fixed by Hudson (Enum casts, DictionaryEntry→KeyValuePair, signature updates, type conversions)
- **Decision:** Phase 1 foundation complete. Ready for Phase 2 public API changes (Ripley's specification documented in decisions.md)

### 2026-03-06 — Phase 2 API Spec Ready (Ripley Context)

- **Phase 2 status:** Specification complete and documented in decisions.md
- **Changes:** 7 public API collection replacements across IExecutive, IVertex, ResourceManager
- **Breaking changes:** All carefully analyzed with caller impact assessment
- **Locked exclusions:** object userData, IDictionary graphContext, XmlSerializationContext internals preserved
- **Phase 2 prep tests:** Hudson added 3 [Ignore]'d tests; ready to enable after Phase 2 merge
- **Implementation timeline:** Phase 2 lead awaiting assignment

## Learnings

### 2026-04-28 — Phase 3 `p3-edge-vertex`: Concrete Graph API Alignment ✅

- **Scope:** Aligned `Edge` and `Vertex` concrete public APIs with the already-approved Phase 3 interface contracts.
- **API changes:** `Edge.PreVertex` / `PostVertex` now return `IVertex?`; `Edge.ChildEdges`, `Edge.PredecessorEdges`, `Edge.SuccessorEdges`, `Vertex.PredecessorEdges`, and `Vertex.SuccessorEdges` now return `IReadOnlyList<Edge>`.
- **Compatibility pattern:** Added narrow bridge overloads where graph callers still naturally speak in terms of interfaces (`Edge.Connect`, `Edge.Disconnect`, `Edge.AddLigature`, `Edge.RemoveLigature`, `VertexSynchronizer`), but kept the implementation anchored on concrete `Vertex` instances internally.
- **Immediate fallout fixed:** Updated graph analyzers, diagnostics, branch management, synchronizer call sites, and graph/task tests to cast only where a concrete `Vertex` is actually required (manager setters, synchronizer arrays, helper methods).
- **Validation:** `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅ and targeted `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-build --filter "FullyQualifiedName~Graph|FullyQualifiedName~Task"` ✅ (30/30).
- **Deferred / out-of-scope note:** A full `Sage.Tests` run still hit unrelated `ItemBased` connector failures (`ConnectorFactory.Connect` / `ManagementFacadeTester`) outside the graph slice.

---

### 2026-07-17 — Sample Code `[Order]` Attribute for Intentional Run Order ✅

- **Scope:** Added `OrderAttribute` to `samples\Sage_SampleCode` to restore the original intentional demo execution order after IExample + reflection-based discovery was introduced.
- **New file:** `OrderAttribute.cs` — `internal sealed class OrderAttribute : Attribute` with a single `int Value` property, `[AttributeUsage(AttributeTargets.Class, Inherited = false)]`.
- **31 classes annotated:** 16 Executive (orders 1–16), 3 StateManagement (17–19), 2 RandomServer (20–21), 3 StateMachine (22–24), 2 Model (25–26), 3 Resources (27–29), 1 SequenceControl (30). `DefaultModelWithSelfManagingModelObjects` (auto-discovered extra) assigned order 100.
- **Program.cs:** Replaced `OrderBy(t => t.FullName)` with `OrderBy(t => t.GetCustomAttribute<OrderAttribute>()?.Value ?? int.MaxValue).ThenBy(t => t.FullName)` for stable secondary sort.
- **Namespace resolution:** `OrderAttribute` lives in `Highpoint.Sage.Examples`; all example files use sub-namespaces. C# compiler resolves parent namespace types without explicit `using` directives — no import changes needed.
- **Build:** 0 errors, 0 warnings. ✅

---
### 2026-07-17 — Materials Extraction into Sage.Materials Standalone Library ✅

- **Scope:** Extracted all 66 files from `src\Sage\Materials\` (and subdirs Chemistry, Emissions, Thermodynamics, VaporPressure) into new `src\Sage.Materials\` class library. Moved 2 test files from `tests\SageTestLib\` to `tests\Sage.Materials.Tests\`.
- **Partial work inherited:** Project files, slnx entries, file deletions from Sage, and test file moves had all been done in a prior partial attempt. The work completed was: creating `MaterialDiagnosticAids.cs` and verifying the build.
- **Two blockers were pre-resolved:** `EmissionsServiceOptions` was already moved from `SageOptions.cs` to `src\Sage.Materials\Emissions\EmissionsServiceOptions.cs`. `DumpMaterial` and related helpers were already removed from `DiagnosticAids.cs` (Materials `using` directives also stripped), and `DiagnosticAids.DumpMaterial(...)` calls were already removed from the moved test files.
- **DiagnosticAids pattern:** `DumpMaterial` was in `DiagnosticAids` (Sage core) but depends on `IMaterial`, `Mixture`, `Substance`. After extraction, it would cause a Sage→Materials circular dep. Solution: remove from core, add `MaterialDiagnosticAids` static class in Materials project with same method. Partial extraction had already stripped test calls entirely rather than redirecting them.
- **Sage.Scratch:** Already had `Sage.Materials.Tests` project reference — no changes needed.
- **Results:** Build: 0 errors, 0 warnings. Sage.Tests: 271/271 passing. Sage.PFC.Tests: 58/58 passing. Sage.Materials.Tests: 22/22 passing. Total: 351/351 ✅

---


### 2026-03-07 — Phase 2 Public API Collection Replacements ✅

- **Scope:** IExecutive, TaskManagementService, TaskProcessor, Vertex, and PFC collections now return IReadOnlyList<T> with typed backing lists.
- **Breaking fixes:** ExecutiveFastLight returns empty IReadOnlyList<IExecEvent>; ExecController/TestQueues/TestTasks/TestGraphPersistence updated; Vertex deserialization now loads IList.
- **Tests/Build:** `dotnet build Sage.slnx` succeeded; `dotnet test SageTestLib` total 319, passed 316, skipped 3.
- **Completion:** Phase 2 implementation complete. All 319 tests passing after fresh build (initial --no-build run against stale binaries; fresh build confirms 319/319). [Ignore] markers removed from 3 Phase 2 prep tests. Public API surfaces now fully modernized to IReadOnlyList<T>. Duration: ~525s.

### 2026-03-07 — Queue/Stack Generic Migration (Phase 3) ✅

- **Files touched (16):** Executive, ExecEventRemover, GraphSequencer, CPMAnalyst, DagCycleChecker, ValidationService, FixedRateChannel, ItemBased.Queue, MaterialService, MultiRequestProcessor, SimpleAccessManager, Milestone, MilestoneRelationship, TimePeriod, XmlSerializationContext, TestPfcNetworks.
- **Types resolved:** 6 Queue variants (Bin, object, ReservationPair, IResourceRequest, Milestone, IPfcNode); 13 Stack variants (ExecEventRemover, IDependencyVertex, Vertex, object, string, XmlNode, IAccessRegulator, bool, MilestoneRelationship, TimeAdjustmentMode, bool for ActiveStack, bool for enabled).
- **Public API changes:** Milestone.ActiveStack → Stack<bool>; ICreationContext.ParentObjectStack → Stack<object>.
- **Test coverage:** 319/319 tests passing (100% pass rate).
- **Gotchas:** Avoided namespace collision with ItemBased.Queues.Queue by fully-qualifying generic queues in the queue class and TestPfcNetworks. Used Stack<object> for mixed-type cycle-checker call stacks (DagCycleChecker, validation paths).
- **Build:** Clean build, zero new errors, warnings only (baseline).
- **Duration:** ~517 seconds (~8.6 minutes).
- **Decision:** Complete. Ready for merge. Decision record merged into decisions.md.

### 2026-07-15 — Configuration Options Migration ✅

- **Scope:** DiagnosticAids, Executive/ExecutiveFastLight, ExecFactory, ModelConfig, and EmissionsService now use POCO options; ConfigurationManager dependency removed; SageOptions POCOs added; legacy app.config references cleaned up.
- **ModelConfig:** Now backed by Dictionary<string, string> with SetSimpleParameter; string section constructor marked obsolete.
- **Build/Test:** `dotnet build Sage4-Everything.sln` succeeded (warnings baseline); `dotnet test SageTestLib` total 319, passed 319.

### 2026-07-16 — Nullable Phase 1 Scaffold ✅
- **Scope:** Enabled nullable globally, added #nullable disable to Sage sources, and annotated Core interfaces plus ExecEvent/SageOptions/DetachableEvent.
- **Build/Test:** `dotnet build Sage4-Everything.sln` succeeded (warnings baseline); `dotnet test SageTestLib` total 319, passed 319.
- **Notes:** 23 files now #nullable enable; 525 files still #nullable disable.

### 2026-07-16 — Nullable Phase 2 Engine Internals ✅

- **Scope:** Executive, ExecutiveFastLight, ExecFactory, ModelConfig, Model, and ExecEventRemover now rely on global nullable with warnings fixed (object? userData, nullable events/fields).
- **Special handling:** ExecutiveFastLight heap internals preserved; nullable annotations and null-forgiving applied without altering queue logic.
- **Build/Test:** `dotnet build Sage4-Everything.sln --no-incremental` succeeded; `dotnet test SageTestLib` total 319, passed 319.
- **Notes:** 23 files now #nullable enable; 519 files still #nullable disable.

### 2026-03-07 — Nullable Phase 4 (Dependencies/Randoms/SystemDynamics) ✅

- **Scope:** Removed `#nullable disable` from Dependencies, Randoms, and SystemDynamics (incl. Design/Utility).
- **Nullability fixes:** GraphSequencer/GraphCycleException annotations, Randoms buffering fields, StateBase Configure fields/collections and distro cache, RunProgram optional parameters.
- **Build/Test:** `dotnet build Sage\Sage4.csproj` clean; `dotnet build Sage4-Everything.sln` blocked by permission prompt; `dotnet test SageTestLib` 319/319.

---

### 2026-07-17 — Phase 3 Batch 1 (`p3-interfaces`) Narrow Interface Cut ✅

- **Scope:** Applied Ripley's Batch 1 graph contract break only: `IVertex.PredecessorEdges`/`SuccessorEdges` now `IReadOnlyList<Edge>`, and `IEdge.PreVertex`/`PostVertex`/`ChildEdges` now expose `IVertex?` and `IReadOnlyList<Edge>`.
- **Compatibility adaptation:** Kept `Edge` and `Vertex` concrete public properties unchanged for now and satisfied the new interfaces with explicit interface implementations. That kept the batch inside `p3-interfaces` instead of spilling into `p3-edge-vertex`.
- **Concrete-class changes:** `Edge` now explicitly maps `IEdge.PreVertex`, `IEdge.PostVertex`, and `IEdge.ChildEdges`; `Vertex` explicitly maps `IVertex.PredecessorEdges` and `IVertex.SuccessorEdges`.
- **Fallout:** No broader graph/task source updates were required once the explicit adapters were in place; the only compile break encountered was a small `ReadOnlyCollection<Edge>`/`Array.Empty<Edge>()` coalescing mismatch while wiring `IEdge.ChildEdges`.
- **Validation:** `dotnet build .\Sage.slnx --no-restore` ✅ and `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-build --filter "FullyQualifiedName~Graph|FullyQualifiedName~Task" -v minimal` ✅ (27/27).
- **Still deferred to later Phase 3 work:** `Edge`/`Vertex` concrete public property shapes are still legacy (`Vertex?`/`IList`). Batch 2+ can decide whether to fully align those concrete APIs with the new interface surface.
- **Remaining:** 370 files still `#nullable disable` (178 enabled total).

### 2026-03-07 — Nullable Phase 5 (ItemBased) ✅

- **Scope:** Removed `#nullable disable` from all 73 files in Sage/ItemBased/ (Connectors, Ports, Queues, Servers, SourcesAndSinks, SplittersAndJoiners).
- **Nullability fixes:** IPort nullable annotations, ConnectorFactory nullable handling, Queue.cs naming collision resolved with fully-qualified System.Collections.Generic.Queue<T>, event delegates made nullable, IModel? propagated throughout.
- **Critical fix:** Corrected Connectors.cs to preserve original Debug.Assert behavior for null models instead of throwing exceptions (test compatibility).
- **Build/Test:** `dotnet build Sage4.csproj` clean; `dotnet test SageTestLib` 319/319 passing.
- **Remaining:** 297 files still `#nullable disable` (251 enabled total).

### 2026-07-16 — Test Project Rename to Sage.Tests ✅

- **Scope:** Renamed test project from `SageTestLib.csproj` to `Sage.Tests.csproj` with comprehensive namespace refactoring.
- **Project file:** `tests\SageTestLib\SageTestLib.csproj` → `tests\SageTestLib\Sage.Tests.csproj` (via `git mv`, history preserved)
- **RootNamespace/AssemblyName:** Set `RootNamespace = Highpoint.Sage.Tests` and `AssemblyName = Sage.Tests` in project file
- **Sage.slnx:** Updated project reference from `SageTestLib.csproj` → `Sage.Tests.csproj`
- **TestDriver.csproj:** Updated ProjectReference from `SageTestLib.csproj` → `Sage.Tests.csproj`
- **Namespace migration:** Fixed messy namespaces in 8 test files:
  - `SageTestLib` → appropriate sub-namespaces under `Highpoint.Sage.Tests.*`
  - `PFCDemoMaterial` → `Highpoint.Sage.Tests.Graphs.PFC`
  - `SchedulerDemoMaterial` → `Highpoint.Sage.Tests.Scheduling`
- **File-by-file namespace mapping:**
  - ProtoActions.cs: `SchedulerDemoMaterial` → `Highpoint.Sage.Tests.Scheduling`
  - TestCyclicalMVTTracker.cs: `SchedulerDemoMaterial` → `Highpoint.Sage.Tests.Scheduling`
  - TestTasks2.cs: `SchedulerDemoMaterial` → `Highpoint.Sage.Tests.Scheduling`
  - TestPfcAnalyst.cs: `PFCDemoMaterial` → `Highpoint.Sage.Tests.Graphs.PFC`
  - TestEventedList.cs: `SageTestLib` → `Highpoint.Sage.Tests.Utility` (tests EventedList)
  - TestHeap.cs: `SageTestLib` → `Highpoint.Sage.Tests.Utility` (tests Heap)

---

### 2026-07-17 — Phase 3 Reactions Opening Batch: Typed Read-Only Surface Only ✅

- **Scope:** Implemented Ripley's opening `p3-reactions` batch in `Reaction`, `ReactionProcessor`, `ReactionInstance`, and the direct chemistry test fallout only.
- **Public API changes:** `Reaction.Reactants` / `Products` now return `IReadOnlyList<Reaction.ReactionParticipant>`. `ReactionProcessor.Reactions` and `GetReactionsByParticipant` / `GetReactionsByReactant` / `GetReactionsByProduct` now return `IReadOnlyList<Reaction>`. `CombineMaterials(... out observedReactions, out observedReactionInstances)` now reports typed read-only `Reaction` / `ReactionInstance` collections while keeping the existing overload set and `IMaterial[]` input shape.
- **Implementation constraint honored:** Kept `ReactionProcessor`'s backing `_reactions` as `ArrayList` and left reaction math, event sequencing, XML field names, and construction/deserialization seams alone. The only internal fallout fix was removing obsolete casts in `Reaction` / `ReactionInstance` once the participant lists became typed.
- **Test fallout fixed:** Updated `tests\SageTestLib\TestChemistry.cs` off `ArrayList observedReactions, observedReactionInstances` to the new typed read-only output variables.
- **Validation:** `dotnet build .\src\Sage.Materials\Sage.Materials.csproj --no-restore` ✅, `dotnet test .\tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj --no-restore` ✅ `22/22`, and the scoped `Sage.Tests` chemistry/persistence/transfer-spec/temperature-controller/MVT slice ✅ `28/28`.
  - TestPfcRepository.cs: `SageTestLib` → `Highpoint.Sage.Tests.Graphs.PFC` (creates PFCs)
  - TestRationalizer.cs: `SageTestLib` → `Highpoint.Sage.Tests.Mathematics` (tests Rationalizer)
- **Cross-references fixed:**
  - TestPfcAnalyst.cs using alias: `using Pfcs = SageTestLib.TestPfcRepository;` → `using Pfcs = Highpoint.Sage.Tests.Graphs.PFC.TestPfcRepository;`
  - TestDriver/Driver.cs: 7 fully-qualified type references updated from old namespaces to new namespaces
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test Sage.Tests.csproj` 319/319 passing
- **Decision:** Test project now fully aligned with team naming convention (Sage.Tests) matching pattern of other projects (Sage.csproj, Sage.Benchmarks.csproj, Sage.Examples.csproj)

### 2026-03-07 — Nullable Phase 6 (Materials) ✅

- **Scope:** Removed `#nullable disable` from all 66 files in Sage/Materials/ (base + Chemistry, Emissions, Thermodynamics, VaporPressure subdirectories).
- **Nullability fixes:** MaterialType IModel/EmissionsClassifications nullable, Substance event delegates, MassVolumeTracker ReactionProcessor nullable, MaterialChangeListener event handlers, IMemento.Parent nullability, IComparer nullability signatures.
- **Notable files:** MaterialType.cs (IModel/IDictionary nullable, InitializeIdentity signature), Substance.cs (large file, event delegates, comparers, memento), Mixture.cs (complex state management), emission models (Hashtable parameters, out parameters).
- **Patterns applied:** Event delegates nullable (`event MaterialChangeListener?`), IModel fields nullable for deserialization, `null!` for deferred-init fields with comments, nullable cast patterns (`as Type?`), unboxing with `!` for DictionaryEntry values.
- **Build/Test:** `dotnet build Sage4.csproj` clean; `dotnet test SageTestLib` 319/319 passing.
- **Remaining:** 232 files still `#nullable disable` (316 enabled total, 548 total in Sage/).
- **Phase 6 completion:** All Materials module files migrated successfully. Ready for Phase 7 (next target TBD).

### 2026-03-07 — Nullable Phase 7 (Graphs) ✅

- **Scope:** Removed `#nullable disable` from all 96 files in Sage/Graphs/ (base + PFC, PFC/Execution, PFC/Execution/Actions, Tasks subdirectories). This was the LARGEST module with ~1,160 warnings.
- **Architectural constraints respected:** `IDictionary graphContext` kept non-generic and non-nullable throughout (architectural design - never null in methods). `object userData` → `object?` but kept non-generic.
- **Core files:** Edge.cs (1,453 lines), Vertex.cs (482 lines), Task.cs (633 lines) - fundamental graph structure components.
- **Major PFC files:** ProcedureFunctionChart.cs (2,946 lines - largest file in Graphs), PfcValidator.cs (1,087 lines), PfcAnalyst.cs (1,003 lines), StepStateMachine.cs (608 lines), PfcNode.cs (440 lines), plus 30+ supporting PFC files.
- **Analysis files:** CPMAnalyst.cs (730 lines - Critical Path Method), ValidationService.cs (656 lines), CriticalPathAnalyst, PertAnalyst, DagCycleChecker, DagDeadlockChecker.
- **Nullability patterns applied:**
  - Event delegates all nullable (`event VertexEvent?`, `event PfcAction?`, etc.)
  - `Vertex? PreVertex`, `Vertex? PostVertex` on IEdge (edges can have null vertices)
  - `IEdge? GetParent()`, `IHasValidity? GetParent()` (nullable return types where documented)
  - `null!` for deferred-init fields in deserialization scenarios with comments
  - `!` null-forgiving operator used sparingly with explanatory comments (e.g., `_vm!.Suspend() // hasVm guarantees non-null`)
  - Dictionary lookups and `as` casts → nullable types throughout
  - PFC expression system: `Expression?`, `ExecutableCondition?`, `ParticipantDirectory?` nullable where appropriate
  - State machines: nullable references for runtime-assigned fields in StepStateMachine/TransitionStateMachine
- **Systematic approach:** Fixed files in priority order: interfaces/enums first, then core structure (Vertex/Edge), then implementations (Task, PFC nodes), then large analysis files, then execution subsystems.
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing (0 failures).
- **Remaining:** 136 files still `#nullable disable` (412 enabled total, 548 total in Sage/).
- **Phase 7 completion:** All Graphs module files migrated successfully. Largest and most complex module done. Ready for Phase 8.

### 2026-07-16 — Nullable Phase 8 (Persistence + Final Cleanup) ✅ **MIGRATION COMPLETE**

- **Scope:** Removed `#nullable disable` from final 134 files across Core (35), Mathematics (53), Persistence (6), Resources (30), SmartPropertyBag (9), Presentation (1).
- **Core files:** BaseModelObject, DefaultModelStates, DetachableEventSynchronizer, EnumStateMachine, ExceptionHandler, ExecController, IMOHelper, InitializationManager, InitializationException, InitializerArgAttribute, InitializerAttribute, InvalidTransitionHandler, MergedTransitionHandler, MetronomeBase, ModelExceptionError, ModelObjectDictionary, SimpleMetronome, StateMachine, TransitionFailureException, TransitionHandler, plus all enums (ExecEventType, ExecState, ExecType, InitializationType, RefType) and attributes (DefaultValueAttribute, TaskGraphVolatileAttribute, VolatileKey).
- **Mathematics module:** All 53 files migrated — distributions (Binomial, Cauchy, Exponential, Normal, Lognormal, Poisson, Triangular, Uniform, Weibull, etc.), CDFs, histograms (1D for Double/DateTime/TimeSpan), interpolators, scaling adapters, regression, operations, extensions.
- **Persistence module:** DeserializationContext, DynamicConstruction, IDirtyable, ISerializer, IXElementSerializable, IXmlPersistable (6 files). **Permanent disables preserved:** XmlSerializationContext, WeakHashTable (architectural complexity).
- **Resources module:** All 30 files migrated — interfaces (IAccessManager, IAccessRegulator, IResource, IResourceManager, etc.), implementations (Resource, ResourceManager, ResourceTracker, etc.), requests, events, exceptions.
- **SmartPropertyBag module:** All 9 files migrated — HierarchicalDictionaryEntry, SmartPropertyBag, WriteLock, SPBInitializer, IHasWriteLock, ISPBTreeNode, exceptions.
- **Presentation:** Converters.cs migrated.
- **Nullability patterns applied:**
  - Event delegates all nullable (`event EventHandler? Name;`)
  - `object userData` → `object?` throughout (kept non-generic as architectural decision)
  - `IModel?`, `string?` for fields/properties that can be null
  - `= null!` for deferred-init fields with comments
  - `!` null-forgiving operator used sparingly with explanatory comments
  - IComparer implementations: `Compare(object? x, object? y)`
  - Generic model error/warning: nullable `subject` and `innerException`
  - Dictionary lookups and `as` casts → nullable types
- **Build/Test:** `dotnet build Sage.slnx --no-incremental` 0 errors; `dotnet test SageTestLib` 319/319 passing (0 failures).
- **Final count:** 2 files with `#nullable disable` (WeakHashTable.cs, XmlSerializationContext.cs) — both permanent exclusions by design.
- **Coverage:** 546 of 548 files nullable-enabled (99.6% coverage).
- **Phase 8 completion:** ✅ **NULLABLE REFERENCE TYPE MIGRATION COMPLETE.** All planned files migrated successfully. Zero regressions, full test coverage maintained.

### 2026-07-16 — Samples Project Rename to Sample.Examples ✅

- **Scope:** Renamed samples project from `Sage_SampleCode.csproj` to `Sample.Examples.csproj` with namespace refactoring.
- **Project file:** `samples\Sage_SampleCode\Sage_SampleCode.csproj` → `samples\Sage_SampleCode\Sample.Examples.csproj` (via `git mv`, history preserved)
- **RootNamespace/AssemblyName:** Set `RootNamespace = Highpoint.Sage.Examples` and `AssemblyName = Sample.Examples` in project file
- **Sage.slnx:** Updated project reference from `Sage_SampleCode.csproj` → `Sample.Examples.csproj`
- **Namespace migration:** All `Demo.*` namespaces renamed to `Highpoint.Sage.Examples.*` in 9 .cs files (1_Executive, 2_StateManagement, 3_RandomServer, 4_StateMachine, 5_IntroToModel, 6_Resources, 7_SequenceControl, Domain, Program)
- **Code references:** Updated fully-qualified type references in Program.cs from `Demo.Executive.*` → `Highpoint.Sage.Examples.Executive.*` and reflection logic from `Demo.` prefix (5 chars) → `Highpoint.Sage.Examples.` prefix (24 chars)
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing
- **Decision:** Samples project now follows team naming pattern (Sample.Examples) and Highpoint.Sage.Examples namespace convention

### 2026-07-16 — Benchmarks Project Rename to Sage.Benchmarks ✅

- **Scope:** Renamed benchmarks project from `SageBenchmarks.csproj` to `Sage.Benchmarks.csproj` with namespace refactoring.
- **Project file:** `benchmarks\SageBenchmarks\SageBenchmarks.csproj` → `benchmarks\SageBenchmarks\Sage.Benchmarks.csproj` (via `git mv`, history preserved)
- **RootNamespace/AssemblyName:** Set `RootNamespace = Highpoint.Sage.Benchmarks` and `AssemblyName = Sage.Benchmarks` in project file
- **Sage.slnx:** Updated project reference from `SageBenchmarks.csproj` → `Sage.Benchmarks.csproj`
- **Namespace migration:** EventDispatchBenchmarks.cs already had correct `namespace Highpoint.Sage.Benchmarks;` declaration
- **Comment updates:** Updated Program.cs comment from old path `Sage_Aux\SageBenchmarks\SageBenchmarks.csproj` → `benchmarks\SageBenchmarks\Sage.Benchmarks.csproj`
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing
- **Decision:** Benchmarks project now follows team naming pattern (Sage.Benchmarks) and Highpoint.Sage.Benchmarks namespace convention

### 2026-07-16 — Corrected Examples Project Name to Sage.Examples ✅

- **Scope:** Renamed samples project from `Sample.Examples.csproj` to `Sage.Examples.csproj` to align with team naming conventions.
- **Project file:** `samples\Sage_SampleCode\Sample.Examples.csproj` → `samples\Sage_SampleCode\Sage.Examples.csproj` (via `git mv`, history preserved)
- **AssemblyName:** Updated from `Sample.Examples` to `Sage.Examples` in project file
- **RootNamespace:** Preserved as `Highpoint.Sage.Examples` (unchanged)
- **Sage.slnx:** Updated project reference from `Sample.Examples.csproj` → `Sage.Examples.csproj`
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing
- **Decision:** Examples project now fully aligned with team naming convention (Sage.Examples) matching pattern of other projects (Sage.csproj, Sage.Benchmarks.csproj)

### 2026-07-16 — Scratch Project Rename to Sage.Scratch ✅

- **Scope:** Renamed TestDriver project from `TestDriver.csproj` to `Sage.Scratch.csproj` with namespace refactoring.
- **Project file:** `tests\TestDriver\TestDriver.csproj` → `tests\TestDriver\Sage.Scratch.csproj` (already renamed, added metadata)
- **RootNamespace/AssemblyName:** Set `RootNamespace = Highpoint.Sage.Scratch` and `AssemblyName = Sage.Scratch` in project file
- **Sage.slnx:** Already updated to reference `Sage.Scratch.csproj`
- **Namespace migration:** Driver.cs updated from `namespace Highpoint.Sage.Testing` → `namespace Highpoint.Sage.Scratch`
- **Build/Test:** `dotnet restore` + `dotnet build Sage.slnx` 0 errors; `dotnet test Sage.Tests.csproj` 316/319 passing (3 pre-existing failures)
- **Decision:** Scratch project now follows team naming convention (Sage.Scratch) and Highpoint.Sage.Scratch namespace convention, completing the project rename series




### 2026-07-16 — Nullable Phase 9 (CS8xxx Complete Migration) ✅

- **Scope:** Eliminated ALL 1,244 CS8xxx nullable reference type warnings across all 8 modules.
- **Modules fixed:** Utility (5), Materials (21), SmartPropertyBag (46), Core (56), ItemBased (71), Resources (76), Graphs (156), Mathematics (191) unique warnings.
- **Key patterns applied:**
  - CS8600/CS8601: Changed local variable types to T? for dictionary lookups and casts
  - CS8602: Added null-conditional operators and null guards before dereferences
  - CS8603/CS8604: Changed parameters/return types to T? or added null-forgiving operator
  - CS8605: Used null-forgiving for guaranteed-non-null unboxing
  - CS8618: Used = null! for fields initialized before use but not in constructor
  - CS8625: Changed non-nullable types to T? where null was being assigned
  - CS8766/CS8767: Updated implementing members to match interface nullability signatures
- **Module-by-module approach:** Worked smallest to largest (Utility→Materials→SmartPropertyBag→Core→ItemBased→Resources→Graphs→Mathematics)
- **Critical decisions:** Kept IDictionary graphContext non-nullable (architectural), kept object userData in event delegates (intentional design)
- **Build/Test:** dotnet build Sage.csproj --no-incremental 0 CS8 warnings; dotnet test Sage.Tests.csproj 319/319 passing.
- **Final count:** 0 CS8xxx warnings across all 548 source files.

## Learnings

### 2026-07-17 — Sample Code `[Order]` Attribute for Intentional Run Order ✅

- **Scope:** Added `OrderAttribute` to `samples\Sage_SampleCode` to restore the original intentional demo execution order after IExample + reflection-based discovery was introduced.
- **New file:** `OrderAttribute.cs` — `internal sealed class OrderAttribute : Attribute` with a single `int Value` property, `[AttributeUsage(AttributeTargets.Class, Inherited = false)]`.
- **31 classes annotated:** 16 Executive (orders 1–16), 3 StateManagement (17–19), 2 RandomServer (20–21), 3 StateMachine (22–24), 2 Model (25–26), 3 Resources (27–29), 1 SequenceControl (30). `DefaultModelWithSelfManagingModelObjects` (auto-discovered extra) assigned order 100.
- **Program.cs:** Replaced `OrderBy(t => t.FullName)` with `OrderBy(t => t.GetCustomAttribute<OrderAttribute>()?.Value ?? int.MaxValue).ThenBy(t => t.FullName)` for stable secondary sort.
- **Namespace resolution:** `OrderAttribute` lives in `Highpoint.Sage.Examples`; all example files use sub-namespaces. C# compiler resolves parent namespace types without explicit `using` directives — no import changes needed.
- **Build:** 0 errors, 0 warnings. ✅

---
### 2026-07-17 — Materials Extraction into Sage.Materials Standalone Library ✅

- **Scope:** Extracted all 66 files from `src\Sage\Materials\` (and subdirs Chemistry, Emissions, Thermodynamics, VaporPressure) into new `src\Sage.Materials\` class library. Moved 2 test files from `tests\SageTestLib\` to `tests\Sage.Materials.Tests\`.
- **Partial work inherited:** Project files, slnx entries, file deletions from Sage, and test file moves had all been done in a prior partial attempt. The work completed was: creating `MaterialDiagnosticAids.cs` and verifying the build.
- **Two blockers were pre-resolved:** `EmissionsServiceOptions` was already moved from `SageOptions.cs` to `src\Sage.Materials\Emissions\EmissionsServiceOptions.cs`. `DumpMaterial` and related helpers were already removed from `DiagnosticAids.cs` (Materials `using` directives also stripped), and `DiagnosticAids.DumpMaterial(...)` calls were already removed from the moved test files.
- **DiagnosticAids pattern:** `DumpMaterial` was in `DiagnosticAids` (Sage core) but depends on `IMaterial`, `Mixture`, `Substance`. After extraction, it would cause a Sage→Materials circular dep. Solution: remove from core, add `MaterialDiagnosticAids` static class in Materials project with same method. Partial extraction had already stripped test calls entirely rather than redirecting them.
- **Sage.Scratch:** Already had `Sage.Materials.Tests` project reference — no changes needed.
- **Results:** Build: 0 errors, 0 warnings. Sage.Tests: 271/271 passing. Sage.PFC.Tests: 58/58 passing. Sage.Materials.Tests: 22/22 passing. Total: 351/351 ✅

---


### 2026-03-08 — Completed Interrupted Naming Refactor ✅
- **Scope:** Fixed broken build from incomplete naming refactoring (16 errors)
- **Files fixed:**
  - Histogram1D_Base.cs: Fixed constructor name to match renamed class (Histogram1DBase)
  - Histogram1D_DateTime/TimeSpan/Double.cs: Updated base class references
  - GenericPort.cs: Updated private event field invocations (_connectionMadePending, _connectionMadeOccurred, _connectionBrokenPending, _connectionBrokenOccurred)
  - OutputPortProxy.cs, InputPortProxy.cs, PortSet.cs: Completed IPortEvents implementation with new event names
  - MilestoneRelationship*.cs: Staged new files (Gte, Lte, Pin, Strut) - renamed from underscore versions
- **Event naming pattern:** BeforeConnectionMade → ConnectionMadePending, AfterConnectionMade → ConnectionMadeOccurred, BeforeConnectionBroken → ConnectionBrokenPending, AfterConnectionBroken → ConnectionBrokenOccurred
- **Class naming pattern:** Histogram1D_Base → Histogram1DBase (PascalCase, no underscores)
- **Build/Test:** 0 errors, 322/322 tests passing
- **Note:** CA1707 and other naming analyzers remain at severity=none (122+ violations would require major refactoring)



---

### 2026-01-XX — Enable 7 Tier 1 CA Rules — Code Quality Hardening ✅

- **Scope:** Enabled and fixed 7 Tier 1 CA code analysis rules across Sage and test projects
- **Process:** Rule-by-rule enable → fix → build → test → commit workflow
- **Rules enabled:**
  1. **CA2200** (5 violations): Changed 	hrow ex; → 	hrow; to preserve stack traces in TimePeriod.cs and ProcedureFunctionChart.cs
  2. **CA1001** (5 violations): Added IDisposable to types owning disposable fields (DetachableEvent, 4 test classes)
  3. **CA2215** (1 violation): Added ase.Dispose() call in BufferedRandomChannel.Dispose()
  4. **CA1816** (56 violations): Added GC.SuppressFinalize(this) to all Dispose() methods (7 core classes, 49 test classes)
  5. **CA2213** (11 violations): Added disposal calls for IDisposable fields in Dispose() methods (Executive, 8 test classes)
  6. **CA1825** (13 violations): Replaced 
ew T[0] and 
ew T[]{} with Array.Empty<T>() for performance
  7. **CA1052** (12 violations): Made static holder types static or sealed (10 static, 2 sealed due to inheritance)
- **Total fixes:** 103 violations across 77 files
- **Build/Test:** 0 CA errors, 0 warnings; **324/324 tests passing**
- **Impact:** Improved code quality, resource management, and performance; proper disposal patterns enforced throughout


---

### 2026-03-17 — Executive vs ExecutiveFastLight Causality Divergence ⚠️

**Note from Hudson's Investigation (2026-03-17T19:55:00Z):**

ExecutiveFastLight's causality enforcement is non-functional. When IgnoreCausalityViolations=false (enforce mode), the throw in RequestEvent() is commented out (line ~350). The event still fires at _now via the dequeue-time clamp in StartWcv()/StartWocv(), and users see only a Console.WriteLine() log message — no exception.

By contrast, Executive.cs properly throws CausalityException (wrapped in RuntimeException) when violations occur with enforce mode disabled.

**Action item:** 
- Investigate whether EFL's enforce mode should be uncommented (breaking change) or documented as "log-only" mode
- Consider adding XML doc clarifying this divergence from Executive
- Tests cover both implementations now (3 new causality tests added to TestExecutive.cs)

**Status:** May need follow-up investigation or design decision (RFC).

---

### 2026-03-17 — EFL Causality Enforcement Fix ✅

**Problem:** ExecutiveFastLight._ignoreCausalityViolations=false was non-functional. Both RequestEvent() and RequestDaemonEvent() had the throw commented out and replaced with Console.WriteLine(). Additionally, StartWcv() had an if (true) guard that always clamped the dequeue-time violation — the lse branch with the throw was dead code.

**Fix (3 surgical changes in ExecutiveFastLight.cs):**
1. RequestDaemonEvent(): Replaced Console.WriteLine(msg) + commented 	hrow with 	hrow new CausalityException(msg) using Executive's message format.
2. RequestEvent(): Same fix.
3. StartWcv(): Replaced if (true) { _currentEvent.When = _now.Ticks; } else { //throw } with 	hrow new CausalityException(...) — eliminates dead code and enforces at dequeue time too.

**Test Update (TestExecutive.cs):**
- Renamed ExecutiveFastLight_CausalityViolation_WhenNotIgnored_StillFiresAtNow → ExecutiveFastLight_CausalityViolation_WhenNotIgnored_Throws
- Updated from "no throw expected, event fires" to Assert.Throws<CausalityException>(() => exec.Start())
- Note: EFL throws CausalityException directly (no RuntimeException wrapper), unlike Executive which stores in _terminationException and wraps post-loop.

**Key learnings:**
- EFL has no try/catch in its dispatch loop, so exceptions thrown from event handlers propagate directly out of Start() as-is.
- Executive catches exceptions in the dispatch loop, stores in _terminationException, and wraps in RuntimeException post-loop. Different machinery, same logical intent.
- The if (true) pattern in StartWcv() was a code smell hiding dead code — dead else branch with a commented throw.

**Build/Test:** 0 errors, 0 warnings; **351/351 tests passing**


---

### 2026-03-17 — EFL Causality Enforcement Fix ✅

**Scope:** Fixed broken causality enforcement in ExecutiveFastLight

**Problem:** When IgnoreCausalityViolations=false, RequestEvent() and RequestDaemonEvent() in EFL had the throw commented out and replaced with Console.WriteLine(msg). The event was still enqueued and fired via the dequeue-time clamp in StartWcv(). The setting had zero effect.

**Fix applied:**
- Both methods (RequestEvent, RequestDaemonEvent) now throw CausalityException matching Executive's exact message format: "Event requested for time {when}, but executive is at time {now}..."
- Removed who/method/msg string-building code — no longer needed
- The dequeue-time clamp in StartWcv() remains as a safety net for the _ignoreCausalityViolations=true path

**Behavior after fix:**
- IgnoreCausalityViolations=false: throws CausalityException directly from RequestEvent() (propagates out of Start() — no RuntimeException wrapper unlike Executive)
- IgnoreCausalityViolations=true: clamps event to _now and fires (existing correct behavior, unchanged)

**Test updated:**
- Renamed ExecutiveFastLight_CausalityViolation_WhenNotIgnored_StillFiresAtNow → ExecutiveFastLight_CausalityViolation_WhenNotIgnored_Throws
- Test now asserts Assert.Throws<CausalityException>(() => exec.Start())

**Build/Test:** 0 errors, **351/351 tests passing**

**Note:** The fix was already partially applied (file timestamp 17/03/2026 19:47:53) before this session confirmed it. This session verified correctness and updated the test + documentation.

## Materials Extraction Commit
**Commit SHA:** 9f9a4e8

Materials subsystem extraction has been successfully committed to git.

**What was committed:**
- All Materials source files moved to src/Sage.Materials/
- New Sage.Materials.csproj class library created
- 22 Materials unit tests in tests/Sage.Materials.Tests/
- EmissionsServiceOptions moved to Materials project
- DiagnosticAids cleaned up (DumpMaterial methods moved)
- Sage.slnx updated with new projects

**Final Test Results:** 351/351 tests passing
- 271 Sage tests
- 58 PFC tests  
- 22 Materials tests

**Status:** ✅ Materials extraction complete and committed.

## Learnings

### 2026-07-17 — Sample Code `[Order]` Attribute for Intentional Run Order ✅

- **Scope:** Added `OrderAttribute` to `samples\Sage_SampleCode` to restore the original intentional demo execution order after IExample + reflection-based discovery was introduced.
- **New file:** `OrderAttribute.cs` — `internal sealed class OrderAttribute : Attribute` with a single `int Value` property, `[AttributeUsage(AttributeTargets.Class, Inherited = false)]`.
- **31 classes annotated:** 16 Executive (orders 1–16), 3 StateManagement (17–19), 2 RandomServer (20–21), 3 StateMachine (22–24), 2 Model (25–26), 3 Resources (27–29), 1 SequenceControl (30). `DefaultModelWithSelfManagingModelObjects` (auto-discovered extra) assigned order 100.
- **Program.cs:** Replaced `OrderBy(t => t.FullName)` with `OrderBy(t => t.GetCustomAttribute<OrderAttribute>()?.Value ?? int.MaxValue).ThenBy(t => t.FullName)` for stable secondary sort.
- **Namespace resolution:** `OrderAttribute` lives in `Highpoint.Sage.Examples`; all example files use sub-namespaces. C# compiler resolves parent namespace types without explicit `using` directives — no import changes needed.
- **Build:** 0 errors, 0 warnings. ✅

---

### IExample Interface Refactor (Sage_SampleCode)
- All 31 example classes in `samples\Sage_SampleCode\` now implement `IExample` with `void Run()`.
- 10 classes were `public static class` (files 4–7) and needed `static` removed before they could implement the interface: `Default`, `SimpleCustomWithInitialization`, `SimpleEnumStateMachine` (StateMachine), `DefaultModel`, `SimpleCustomWithInitialization`, `DefaultModelWithSelfManagingModelObjects` (Model), `ServicePoolExample`, `ServicePoolExampleWithSynchronousEvents`, `OptimalResourceAcquisition` (Resources), `TaskGraphDemo` (SequenceControl).
- `DefaultModelWithSelfManagingModelObjects.Run()` called `StateMachine.Basic.SimpleEnumStateMachine.Run()` as a static call — needed updating to `new StateMachine.Basic.SimpleEnumStateMachine().Run()` after the refactor.
- `Program.cs` now uses reflection (`Assembly.GetExecutingAssembly().GetTypes()`) to discover all `IExample` implementors automatically, ordered by `t.FullName`. Adding a new example class only requires implementing `IExample` — no manual registration needed.
- The `Demonstrate(IExample)` overload binds the instance `Run()` as `Action runAction = example.Run` so the existing `CreateDocs` helper (`run.Method.DeclaringType?.Namespace`) continues to work correctly.


### 2026-07-17 — Phase 2 Wrapper Internals Use Generic Collections ✅

- **Scope:** Modernized `HashtableOfLists`, `WeakHashtable`, and `WeakList` internals without changing their public wrapper surfaces.
- **HashtableOfLists (non-generic):** Replaced `Hashtable`/`ArrayList` storage with `Dictionary<object, object?>` plus `List<object>`-backed wrapper buckets, while preserving the scalar-vs-wrapper shape that drives duplicate suppression and prune-on-empty behavior.
- **HashtableOfLists (generic):** Kept duplicate-preserving list semantics, but filled in missing `ICollection<KeyValuePair<TKey, List<TValue>>>` members and made `Remove(key, item)` return `false` instead of throwing for missing keys.
- **Weak collections:** `WeakHashtable` now uses `Dictionary<object, WeakReference>` internally, and `WeakList` now uses `List<MyWeakReference>` so `Contains`, `IndexOf`, `Remove`, enumeration, and `CopyTo` all operate on target values instead of wrapper objects.
- **Validation:** `dotnet build src\Sage\Sage.csproj --no-restore` succeeded; targeted wrapper tests in `tests\SageTestLib\Sage.Tests.csproj` passed 14/14.

---

### 2026-04-26 — Phase 2 Graph Algorithms: Internal Modernization, Public Surface Preservation ✅

- **Scope:** Phase 2 graph-algorithms batch targeting `src\Sage\Graphs\PertAnalyst.cs`, `CPMAnalyst.cs`, and `DagDeadlockChecker.cs` with collection modernization limited to private/internal implementation details.
- **Guardrails honored:** Kept `PertAnalyst.CriticalPath` as `ArrayList`, left intentional non-generic public/protected surface shapes alone, and avoided touching `IDictionary graphContext` patterns.
- **PertAnalyst:** Swapped the internal critical-path accumulator from `ArrayList` to `List<Edge>` and wrapped it back to a read-only `ArrayList` at the property boundary.
- **DagDeadlockChecker:** Replaced internal dedupe/lookup work with `HashSet<Node>` and replaced predecessor construction `Hashtable`/`ArrayList` plumbing with `Dictionary<Node, HashSet<Node>>`, preserving `GetSuccessors`/`Errors` legacy surface.
- **CPMAnalyst:** Updated the private contemporaneous-vertex traversal helper to use `List<Vertex>` + `HashSet<Vertex>` instead of `ArrayList`.
- **Validation:** `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅ and targeted graph tests `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore --filter "FullyQualifiedName~GraphValidityTester|FullyQualifiedName~DAGCycleCheckerTester"` ✅ (12/12).

---

### 2026-07-17 — Phase 2 PortSet/Resources Internal Collection Cleanup ✅

- **Scope:** Finished the Ripley-approved PortSet/resources slice without changing public constructors, collection shapes, key semantics, XML payloads, or waiter ordering.
- **PortSet:** Swapped the private listener buckets (`PortData*` + connection listeners) from `ArrayList` to typed `List<>` storage and replaced `ClearPorts()`'s legacy snapshot copy with a typed `List<IPort>` snapshot before mutation.
- **MultiKeyAccessRegulator:** Kept the public `ArrayList` constructor contract, but copied keys into a private `List<object?>` because the regulator only needs membership checks internally.
- **ResourceManager:** Kept `Resources`, XML persistence, and waiter machinery intact; only changed `Clear()` to iterate over a typed snapshot and pre-sized the deserialization list from the legacy `ArrayList` payload.
- **Deferred on purpose:** `PortSet` still uses `Hashtable` storage because key semantics and XML shape are scope-locked, and `ResourceManager` waiter internals remain untouched because ordering changes are out of bounds for this phase.
- **Validation:** `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅ and targeted resource/port regressions in `tests\SageTestLib\Sage.Tests.csproj` ✅ (7/7).

---

### 2026-04-28 — Phase 3 Resource Manager Read-Only Surface Batch ✅

- **Scope:** Landed only the first `p3-resource-manager` public-surface break: `IResourceManager.Resources` and concrete manager/resource implementers now expose `IReadOnlyList<IResource>`, and `IResourceManagerCollection.GetResourceManagers()` now returns `IReadOnlyCollection<IResourceManager>`.
- **Concrete adapters:** `ResourceManager` now returns `_resources.AsReadOnly()`, `SelfManagingResource` delegates the typed read-only list through its wrapped manager, and `MaterialResourceItem` exposes a one-item `Array.AsReadOnly(...)` wrapper to keep its singleton-manager semantics intact.
- **Fallout fixed:** Updated resource tests off `IList.Contains(...)` to collection assertions that work against the new read-only typed boundary. No waiter behavior, event shapes, mutators, indexer behavior, or XML-facing construction seams were changed.
- **Validation:** `dotnet build E:\source\Sage\src\Sage\Sage.csproj --no-restore` ✅, `dotnet build E:\source\Sage\src\Sage.Materials\Sage.Materials.csproj --no-restore` ✅, targeted `Sage.Tests` resource/server slice ✅ (19/19), `Sage.Examples` build ✅, and `Sage.Materials.Tests` ✅ (22/22).
