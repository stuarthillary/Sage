# Decision: Namespace Root Changed from Sage to Highpoint.Sage

**Author:** Parker (.NET Developer)  
**Date:** 2026-07-16  
**Status:** Complete  
**Requested by:** Stuart Hillary

## Decision

The root namespace for the Sage library has been changed from `Sage` to `Highpoint.Sage`. All sub-namespaces are prefixed accordingly (e.g., `Sage.SimCore` → `Highpoint.Sage.SimCore`). The project file has been renamed and the assembly name updated.

## Changes

- `src\Sage\Sage4.csproj` renamed to `src\Sage\Sage.csproj` (via `git mv`, history preserved)
- `<RootNamespace>Highpoint.Sage</RootNamespace>` set in `Sage.csproj`
- `<AssemblyName>Highpoint.Sage</AssemblyName>` set in `Sage.csproj`
- All namespace declarations updated: `namespace Sage.*` → `namespace Highpoint.Sage.*`
- All using directives updated: `using Sage.*` → `using Highpoint.Sage.*`
- `SageOptions.cs` default executive type string updated to reference `Highpoint.Sage` assembly
- `TestExecutive.cs` hardcoded type string updated to reference `Highpoint.Sage` assembly
- `Sage.slnx` project entry updated from `Sage4.csproj` → `Sage.csproj`
- ProjectReferences updated in: SageTestLib, TestDriver, SageBenchmarks, Sage_SampleCode

## Notes

- Output DLL is now `Highpoint.Sage.dll`
- Any external code referencing `using Sage.*` or `typeof(...).Namespace == "Sage.*"` must be updated
- `Type.GetType("..., Sage")` calls must be updated to `Type.GetType("..., Highpoint.Sage")`

## Verification

- `dotnet build Sage.slnx -v minimal` → 0 errors, 0 warnings baseline
- `dotnet test tests\SageTestLib\SageTestLib.csproj` → 319/319 passing
