# PFC Extraction: parker-pfc-extraction

**Date:** 2026-07-17  
**Author:** Parker  
**Status:** Complete ✅

## What Was Done

Extracted `src\Sage\Graphs\PFC\` (53 files) into a standalone class library `Sage.PFC`.

### Created
- `src\Sage.PFC\Sage.PFC.csproj` — new library; RootNamespace=`Highpoint.Sage`, AssemblyName=`Sage.PFC`, references `Sage.csproj`
- `tests\Sage.PFC.Tests\Sage.PFC.Tests.csproj` — new xUnit test project; references both `Sage.PFC.csproj` and `Sage.csproj`
- Both projects added to `Sage.slnx`

### Moved (source files)
All 53 files from `src\Sage\Graphs\PFC\` → `src\Sage.PFC\`, preserving directory structure:
- Root level: 40 `.cs` files + `ProcedureFunctionChart.xsd`
- `Execution\`: 9 `.cs` files
- `Execution\Actions\`: 2 `.cs` files

### Moved (test files)
From `tests\SageTestLib\` → `tests\Sage.PFC.Tests\`:
- `TestPfcRepository.cs` (621 lines)
- `TestPfcNetworks.cs` (857 lines)
- `TestPfcAnalyst.cs` (1,434 lines)
- `TestExecutablePFCs.cs` (457 lines, `#if NYRFPT` guarded)
- `TestExecutablePFCExtensions.cs` (122 lines, `#if NYRFPT` guarded)
- `TestData\RightPFC.xml`
- `TestData\WrongPFC.xml`

### Deleted
- `src\Sage\Graphs\PFC\` (entire directory) — types now live in `Sage.PFC.dll`
- 5 PFC test `.cs` files from `tests\SageTestLib\`
- 2 PFC XML test data files from `tests\SageTestLib\TestData\`

### Modified
- `Sage.slnx` — added `src\Sage.PFC\Sage.PFC.csproj` and `tests\Sage.PFC.Tests\Sage.PFC.Tests.csproj`
- `tests\TestDriver\Sage.Scratch.csproj` — added project references to `Sage.PFC.csproj` and `Sage.PFC.Tests.csproj` (Driver.cs directly instantiates PfcAnalystTester and PFCGraphTester)

## Decisions Made

1. **No namespace changes** — All types keep `Highpoint.Sage.Graphs.PFC.*` namespaces exactly as before.
2. **RootNamespace = `Highpoint.Sage`** — Matches the parent Sage.csproj convention; not `Highpoint.Sage.Graphs.PFC` which would be wrong.
3. **No `<Compile Include>` glob needed** — SDK-style project picks up all `.cs` files automatically.
4. **xsd file not embedded** — The schema is inlined as a string literal in `ProcedureFunctionChart.cs`; the `.xsd` file is just documentation, not a runtime resource.
5. **Sage.Scratch updated** — The scratch/driver project that calls PFC test methods directly was updated to reference the new projects rather than leaving it broken.

## Verification

- `dotnet build Sage.slnx` → **0 errors**
- `dotnet test Sage.Tests.csproj` → **293/293 passed**
- `dotnet test Sage.PFC.Tests.csproj` → **58/58 passed**
