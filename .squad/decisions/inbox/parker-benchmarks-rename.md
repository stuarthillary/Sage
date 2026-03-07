# Benchmarks Project Rename

**Date:** 2026-07-16  
**Agent:** Parker (by request from Stuart)  
**Status:** ✅ Complete

## Decision

Renamed the benchmarks project from `SageBenchmarks.csproj` to `Sage.Benchmarks.csproj` to align with team naming conventions.

## Implementation

- **Project file:** `benchmarks\SageBenchmarks\SageBenchmarks.csproj` → `benchmarks\SageBenchmarks\Sage.Benchmarks.csproj` (via `git mv`, history preserved)
- **AssemblyName:** `Sage.Benchmarks`
- **RootNamespace:** `Highpoint.Sage.Benchmarks`
- **Sage.slnx:** Updated project reference path
- **Namespace:** EventDispatchBenchmarks.cs already used `Highpoint.Sage.Benchmarks` (correct)
- **Comments:** Updated path references in Program.cs

## Verification

- **Build:** `dotnet build Sage.slnx` — 0 errors
- **Tests:** `dotnet test SageTestLib` — 319/319 passing

## Rationale

This change aligns the benchmarks project with the team's naming pattern established for the main library (`Sage.csproj`), samples (`Sample.Examples.csproj`), and the `Highpoint.Sage.*` namespace convention. The folder name `benchmarks\SageBenchmarks\` remains unchanged to maintain compatibility with existing paths, while only the .csproj filename changes.
