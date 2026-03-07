# Examples Project Rename Correction

**Author:** Parker (.NET Developer)  
**Date:** 2026-07-16  
**Status:** Complete ✅  
**Requested by:** Stuart Hillary

## Decision

Corrected the examples project name from `Sample.Examples.csproj` to `Sage.Examples.csproj` to align with team naming conventions established for other projects (Sage.csproj, Sage.Benchmarks.csproj).

## What Changed

- **Project file:** Renamed `samples\Sage_SampleCode\Sample.Examples.csproj` → `samples\Sage_SampleCode\Sage.Examples.csproj` (via `git mv`)
- **AssemblyName:** Updated from `Sample.Examples` to `Sage.Examples`
- **RootNamespace:** Preserved as `Highpoint.Sage.Examples` (no change needed)
- **Sage.slnx:** Updated project reference from `Sample.Examples.csproj` → `Sage.Examples.csproj`

## Rationale

The previous rename (to Sample.Examples) was inconsistent with the established naming pattern where project files match the assembly name prefix. The main library is `Sage.csproj` producing `Highpoint.Sage.dll`, benchmarks are `Sage.Benchmarks.csproj`, so examples should be `Sage.Examples.csproj`.

## Verification

- `dotnet build Sage.slnx` → 0 errors
- `dotnet test tests\SageTestLib\SageTestLib.csproj` → 319/319 passing

## Notes

- Only the project file name and AssemblyName changed
- RootNamespace remains `Highpoint.Sage.Examples` as intended
- The containing folder `Sage_SampleCode` was not renamed (Stuart's guidance)
- Git history preserved via `git mv`
