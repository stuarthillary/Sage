# Decision: Materials Subsystem Extraction into Sage.Materials

**By:** Parker  
**Date:** 2026-07-17  
**Status:** Complete

## What Was Done

Extracted the `Highpoint.Sage.Materials` subsystem from `src\Sage\` into a standalone class library `src\Sage.Materials\`. This mirrors the PFC extraction (commit 82d2573).

## Scope

**New projects:**
- `src\Sage.Materials\Sage.Materials.csproj` — 66 source files across root + Chemistry, Emissions, Thermodynamics, VaporPressure subdirectories
- `tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj` — 2 test files (TestMaterials.cs, TestMaterialService.cs) moved from `tests\SageTestLib\`

**`Sage.slnx` updated** to include both new projects.

**`tests\TestDriver\Sage.Scratch.csproj`** already had a `Sage.Materials.Tests` project reference.

## Blocker Resolutions

### 1. EmissionsServiceOptions
`EmissionsServiceOptions` was defined in `src\Sage\Core\SageOptions.cs` in the `Highpoint.Sage.Materials.Chemistry.Emissions` namespace. It referenced `IEmissionModel`, a Materials type. Keeping it in Sage core would require Sage->Materials, creating a circular dependency.

**Resolution:** Class moved to `src\Sage.Materials\Emissions\EmissionsServiceOptions.cs`. Removed from `SageOptions.cs`.

### 2. DiagnosticAids.DumpMaterial
`src\Sage\Utility\DiagnosticAids.cs` contained `DumpMaterial(IMaterial)`, `Dump(Mixture)`, `Dump(Substance)` — depend on Materials types.

**Resolution:**
- Methods and Materials `using` directives removed from `DiagnosticAids.cs`.
- `MaterialDiagnosticAids` static class created at `src\Sage.Materials\MaterialDiagnosticAids.cs` in namespace `Highpoint.Sage.Diagnostics` providing `DumpMaterial` for future callers.
- Test file calls to `DiagnosticAids.DumpMaterial(...)` stripped from moved test files.

## Project Reference Topology

Sage (core) <- Sage.Materials <- Sage.Materials.Tests <- Sage.Scratch

No circular references. Sage core is independent of Materials.

## Results

- Build: 0 errors, 0 warnings
- Sage.Tests: 271/271 passing
- Sage.PFC.Tests: 58/58 passing
- Sage.Materials.Tests: 22/22 passing
- Total: 351/351
