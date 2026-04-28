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

### 2026-07-17 — Versioning Release Surfaces Inventory (`p3-version-bump`)

**Inventory Summary:** The Sage library currently has **no explicit version metadata** in source — all version data comes from SDK defaults, with a critical mismatch between local build identity and published package history.

**Current State:**

1. **Assembly Version Sources:**
   - **Assembly File:** All projects default to `Version=1.0.0.0` (SDK default, not explicitly set)
   - **Location:** Generated automatically; no `.props` file or `AssemblyInfo.cs` defines it
   - **Scope:** Affects `Sage.dll`, `Sage.Materials.dll`, `Sage.PFC.dll` equally

2. **Project Version Metadata:**
   - **No explicit `<Version>`:** Neither `Directory.Build.props` nor individual `.csproj` files set version
   - **No `<PackageId>`:** Default would be `Sage`, `Sage.Materials`, `Sage.PFC`
   - **No `<PackageVersion>`:** Falls back to `<Version>` default (1.0.0)
   - **No assembly metadata:** No `<AssemblyVersion>`, `<FileVersion>`, `<InformationalVersion>` set

3. **Release Artifacts:**
   - **No `.nuspec` files:** No NuGet spec in repo
   - **No NuGet.config:** No package source or publish configuration
   - **Build Output:** `BuildOutput/` directory with platform subdirs (`win-x86`, `win-x64`, `linux-x64`) — no packaged artifacts

4. **Release Automation:**
   - **GitHub Actions:** `squad-release.yml` reads `version` from `package.json` (currently just Squad CLI config)
   - **Not tied to .NET project version:** Release automation is Node-based, not NuGet-aware
   - **No changelog:** No `CHANGELOG.md` exists in repo

5. **Public Package History:**
   - **Shipped under:** `Highpoint.Sage` 4.0.2 (external NuGet, not tied to current repo)
   - **Breaking mismatch:** Local builds are `1.0.0.0`, but consumers reference `4.0.2`

**For Version-Bump-Only Pass (No Publish):**

Minimum surfaces that must change to version-bump locally:

| Surface | Current | Action for Bump | Critical? |
|---------|---------|-----------------|-----------|
| `Directory.Build.props` | No `<Version>` | Add centralized `<Version>` property | **YES** |
| `.csproj` `<AssemblyVersion>` | SDK default (1.0.0.0) | Set explicitly from `Direction.Build.props` | YES |
| `.csproj` `<FileVersion>` | SDK default | Set explicitly from `Directory.Build.props` | YES |
| `.csproj` `<PackageVersion>` | SDK default (1.0.0) | Set explicitly if projects packable | YES |
| `.csproj` `<PackageId>` | SDK default | Set explicitly if projects packable | **NO** (skip unless package intent changes) |
| `package.json` version | Current (if present) | Update if version string used externally | **NO** (only for release coordination) |
| `CHANGELOG.md` | Does not exist | Create if release notes planned | **NO** (defer to release gate decision) |
| GitHub Actions `squad-release.yml` | Reads `package.json` | No change needed for version bump | **NO** (skip) |

**Key Constraints:**
- Ripley's scope gate (`.squad/decisions/inbox/ripley-phase3-version-bump-gate.md`) requires product decision on: package family name (keep `Highpoint.Sage` or switch to `Sage*`), artifact split strategy, GA vs preview channel, support baseline (net10.0?), assembly version policy (fixed 5.0.0.0 or full increment)
- This inventory assumes **no publishing**, so no NuGet metadata beyond identity is required
- Phase 3 intentionally preserved XML persistence shape, so no schema version bumps needed

**Decision Needed Before Implementation:**
What version number should the bump target? Current gate suggests `5.0.0` as architectural recommendation for Phase 3.

---

### 2026-07-17 — Phase 2 Collection Modernization Committed

Committed all Phase 2 code/test changes to `feature/dotnet10` branch after validation.

**Commit Details:**
- **SHA:** `f6f08c0`
- **Message:** "Complete Phase 2 collection modernization"
- **Branch:** feature/dotnet10
- **Status:** Pushed to origin ✅

**Validation Performed:**
- Build: `dotnet build .\src\Sage\Sage.csproj` → 0 errors, 0 warnings ✅
- Test: `dotnet test .\tests\SageTestLib\Sage.Tests.csproj` → 290/290 passing ✅

**Files Committed (16 total):**
- **Modified (12 files):**
  - src/Sage/Graphs/ (3): CPMAnalyst, DagDeadlockChecker, PertAnalyst
  - src/Sage/ItemBased/ (1): PortSet
  - src/Sage/Resources/ (2): MultiKeyAccessRegulator, ResourceManager
  - src/Sage/Utility/ (4): HashtableOfLists, WeakHashTable, WeakList, WeakListEnumerator
  - tests/SageTestLib/ (4): TestHashtableOfLists, TestPorts, TestResources, TestWeakRefHashtable
- **Created (2 files):**
  - tests/SageTestLib/TestGraphAlgorithmRegressions.cs (new regression test suite)
  - tests/SageTestLib/TestWeakList.cs (new regression test suite)

**Scope Gate Compliance:**
- All changes within Phase 2 narrow scope as approved in decisions.md
- No public API surface changes
- No persistence/serialization behavior changes
- All signature-preserving internal migrations complete

### 2026-03-06 — DetachableEvent.cs Debug Artifact Cleanup

Inspected `Sage/Core/DetachableEvent.cs` for leftover debug artifacts from the .NET 10 investigation phase.

**Findings:**
- 4 commented-out `_Debug.WriteLine()` lines in methods `resume()` (2 lines) and `End()` (2 lines)
- 1 active `_Debug.WriteLine()` call in exception handler (line 224) — legitimate error logging, kept
- Root cause: These debug traces were added during Parker's investigation but not included in the final fix

**Action Taken:**
- Removed all 4 commented debug lines
- Committed as `eabf539` with message "Remove debug artifacts from DetachableEvent.cs"
- File now clean; final fix remains isolated to `Exchange.cs` (ContainsKey guards only)

**Decision Rationale:**
The investigation notes (decisions.md) clearly stated "DetachableEvent.cs has debug Console.WriteLine calls (should be removed)". The final root cause fix was in Exchange.cs only, not DetachableEvent threading changes. Keeping debug artifacts violates code cleanliness standards and could confuse future maintainers about what was actually changed in the .NET 10 upgrade.

### 2026 — Test NuGet Package Upgrade

Upgraded four stale test NuGet packages in `Directory.Packages.props`:

| Package | Old Version | New Version |
|---|---|---|
| `Microsoft.NET.Test.Sdk` | 16.7.1 | 17.12.0 |
| `MSTest.TestAdapter` | 2.1.1 | 3.7.3 |
| `MSTest.TestFramework` | 2.1.1 | 3.7.3 |
| `coverlet.collector` | 1.3.0 | 6.0.4 |

`dotnet restore` resolved cleanly. `dotnet build` succeeded with 0 errors and 2826 pre-existing CA analyzer warnings (unchanged). The project uses Central Package Management (`ManagePackageVersionsCentrally=true`), so all version changes are in one file.
