# Squad Decisions

## Active Decisions

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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
