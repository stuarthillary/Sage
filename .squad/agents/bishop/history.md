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

### 2026 — Test NuGet Package Upgrade

Upgraded four stale test NuGet packages in `Directory.Packages.props`:

| Package | Old Version | New Version |
|---|---|---|
| `Microsoft.NET.Test.Sdk` | 16.7.1 | 17.12.0 |
| `MSTest.TestAdapter` | 2.1.1 | 3.7.3 |
| `MSTest.TestFramework` | 2.1.1 | 3.7.3 |
| `coverlet.collector` | 1.3.0 | 6.0.4 |

`dotnet restore` resolved cleanly. `dotnet build` succeeded with 0 errors and 2826 pre-existing CA analyzer warnings (unchanged). The project uses Central Package Management (`ManagePackageVersionsCentrally=true`), so all version changes are in one file.
