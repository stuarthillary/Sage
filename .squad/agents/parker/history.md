# Project Context

- **Owner:** Stuart Hillary
- **Project:** Sage® Simulation and Modeling Libraries — a long-running discrete event simulation (DES) library originally built on early .NET Framework, now targeting .NET 8.
- **Stack:** C#, .NET 8, NUnit/xUnit, GitHub Actions, NuGet
- **Key modules:** Core (event engine), Scheduling, Graphs, Mathematics, SystemDynamics, ItemBased, Randoms, Persistence, Presentation, SmartPropertyBag, Utility
- **Goals:** Continued development, .NET 8 modernization, performance improvement, future visualization layer
- **PM:** Stuart Hillary (human — sets priorities and direction)
- **Created:** 2026-03-05

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-15 — .NET 10 Upgrade

- **Branch:** `feature/dotnet10` (from `main`)
- **Files changed:** `Directory.Build.props` — `<TargetFramework>` changed from `net8.0` to `net10.0`
- **No .csproj overrides found:** all projects inherit `TargetFramework` from `Directory.Build.props`; no individual files needed updating
- **Build status:** ✅ **Build succeeded** — 0 errors, 2826 warnings (all pre-existing CA analyzer warnings, none introduced by the .NET 10 upgrade)
- **SDK installed:** .NET 10.0.103 was already present on the machine
- **NuGet packages to watch:**
  - `Microsoft.NET.Test.Sdk` 16.7.1 — very old; should be updated to 17.x+ for .NET 10 test runs
  - `MSTest.TestAdapter` / `MSTest.TestFramework` 2.1.1 — old; recommend upgrading to 3.x
  - `coverlet.collector` 1.3.0 — old; recommend upgrading to 6.x
  - `System.Configuration.ConfigurationManager` 8.0.1 — an in-box .NET 9/10 package; the `8.0.1` pin is fine but could be bumped to `10.0.x` once released on NuGet
- **Blockers:** None — the upgrade was clean
