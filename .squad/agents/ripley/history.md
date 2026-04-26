# Project Context

- **Owner:** Stuart Hillary
- **Project:** Sage® Simulation and Modeling Libraries — a long-running discrete event simulation (DES) library originally built on early .NET Framework, now targeting .NET 8.
- **Stack:** C#, .NET 8, NUnit/xUnit, GitHub Actions, NuGet
- **Key modules:** Core (event engine), Scheduling, Graphs, Mathematics, SystemDynamics, ItemBased, Randoms, Persistence, Presentation, SmartPropertyBag, Utility
- **Goals:** Continued development, .NET 8 modernization, performance improvement, future visualization layer
- **PM:** Stuart Hillary (human — sets priorities and direction)
- **Created:** 2026-03-05

## Core Context

**March 2026 — CA Rules Enablement Analysis**
- Analyzed 20 suppressed Roslyn analyzer rules
- Tier 1: 7 rules, 88 violations, 4-6 hours effort ready to enable immediately
- Tier 2: 5 rules, 332 violations deferred for future pass
- Tier 3: 4 rules, 2106 violations kept suppressed (breaking changes)

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-07-16 — Collections Recovery Needs Hard Phase Boundaries

**Status:** Recovery pass stabilized

**Learning:** The rejected collections migration had mixed low-risk Phase 1 work with Phase 2/public-contract work. For recovery, keep Phase 1 limited to signature-preserving private/internal collection swaps in core/supporting internals, and explicitly leave Graph algorithms, PFC, Materials, `PortSet`, `WeakHashtable`, `WeakList`, and `HashtableOfLists` out unless separately approved.

**Why it matters:** The immediate build failures came from graph-analysis files that should never have been part of the recovery pass. Restoring the branch to Phase 1 scope and validating with `dotnet build Sage.slnx`, `dotnet test tests\SageTestLib\Sage.Tests.csproj`, and `dotnet test tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj` returned the branch to a trustworthy state without expanding public-surface risk.

### 2026-04-26 — Collections Recovery Batch Complete ✅

**Status:** Complete

**Batch Summary:**
- Completed scope-creep reversion: removed Phase 2+ changes from working tree
- Retained Phase 1-safe internal collection conversions (StringOperations adapter cleanup, GenericPort private store migration)
- Fixed all active build breaks by reverting problematic scope-creep edits
- Coordinated with Hudson on scope classification (Phase 1, Phase 2+ creep, ambiguous)

**Verification:**
- `dotnet build Sage.slnx`: 0 errors, 0 warnings ✅
- `dotnet test tests\SageTestLib\Sage.Tests.csproj`: 271/271 passing ✅
- `dotnet test tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj`: 22/22 passing ✅
- Total: 351/351 tests passing ✅

**Outcome:** Branch restored to trustworthy state with explicit Phase 1 boundaries. Ready for next phase of work.

### 2026-03-09 — CA Rules Enablement Analysis Complete ✅

**Status:** Tier 1 analysis complete, decision pending

**Methodology:**
- Analyzed 20 suppressed Roslyn analyzer rules from .editorconfig
- For each rule: temporary severity = warning, counted actual violations
- Categorized by violation count + fix complexity
- Restored .editorconfig after each test to maintain baseline (0 errors)

**Key Findings:**

**Tier 1: 7 rules, 88 violations, 4-6 hours effort — READY TO ENABLE IMMEDIATELY**
1. CA2200 — Rethrow to preserve stack details (10 violations)
2. CA1001 — Types with disposable fields should be disposable (2 violations)
3. CA2215 — Dispose should call base.Dispose (2 violations)
4. CA1816 — Dispose should call GC.SuppressFinalize (12 violations)
5. CA2213 — Disposable fields should be disposed (18 violations)
6. CA1825 — Avoid zero-length array allocations (20 violations)
7. CA1052 — Static holder types should be static/sealed (24 violations)

**Tier 2: 5 rules, 332 violations, 8-12 hours effort — Future Pass**
- CA1063 (Implement IDisposable correctly), CA2214 (Don't call overridable in constructors), CA2000 (Dispose before losing scope), CA1822 (Mark as static), CA1032 (Exception constructors)

**Tier 3: 4 rules, 2106 violations — Keep Suppressed (Breaking Changes)**
- CA1051 (No visible fields — DTO breaking), CA1805 (No unnecessary init), CA2201 (No reserved exceptions), CA1062 (Validate arguments — 1122 violations, high false positive)

**Deliverables:**
- .squad/decisions/ripley-ca-rules-analysis.md — comprehensive analysis with per-rule friction assessment (merged to decisions.md)
- .squad/orchestration-log/2026-03-09T23-27-56Z-ripley.md — orchestration log

**Build status:** 0 errors, 0 warnings. TreatWarningsAsErrors=true.

**Decision Request:** Approve Tier 1 enablement (7 rules, 88 fixes, 4-6 hours, Parker to implement)?

**Next Steps:**
- Stuart to approve/defer Tier 1 decision
- If approved: Parker to enable rules one-by-one with per-rule commits + verification
- If deferred: Keep as future sprint

## Core Context (Summarized Old Entries)

**2025-07-15 — Full Architectural Assessment:** Single monolithic assembly Highpoint.Sage.dll (548 files, 16 modules). Two IExecutive implementations: Executive (full-featured, ~1,150 lines, SortedList-based O(n) event queue — #1 performance bottleneck) and ExecutiveFastLight (~900 lines, binary heap O(log n), single-threaded, lacks priority/rescission/detachable). Event dispatch: delegate-based ExecEventReceiver(IExecutive, object userData) — untyped userData everywhere. Three event types: Synchronous (inline), Detachable (coroutine via Task.Run+ManualResetEventSlim), Asynchronous (fire-forget). Legacy patterns: 304 ArrayList, 265 Hashtable, 31 non-generic SortedList, 250 ApplicationException throws, no nullable refs, 89 delegate declarations, System.Configuration.ConfigurationManager, MarshalByRefObject on Executive, mixed _camelCase/m_camelCase field naming, ExecEvent object pool disabled. Modernization priorities: (1) Replace SortedList with priority queue, (2) Enable nullable refs, (3) Modernize collections, (4) Replace ConfigurationManager, (5) Type-safe events, (6) Exception cleanup, (7) Observable event stream, (8) Update test infrastructure, (9) Project splitting, (10) Modern C# idioms.

**2026-03-06 — TupleSpace Failures Resolved:** Pre-existing race condition in Exchange.NonBlockingPost() (not thread pool starvation). Generic HashtableOfLists<TKey,TValue> indexer throws KeyNotFoundException if key missing. .NET 10's different thread pool timing exposed timing-dependent bug. Fix: Added ContainsKey() checks in two locations. Branch eature/dotnet10 fixed and merge-ready. Build: 0 errors, Tests: 304/304 passing.

**2026-03-06 — Collection Modernization Strategy:** 3-phase risk tiering (Phase 1: 60% internal non-breaking low-risk, Phase 2: 30% public API breaking medium-risk, Phase 3: 10% intentional never-replace). Critical exclusions locked: object userData (intentional heterogeneous payloads), IDictionary graphContext (intentional polymorphic execution context, 50+ signatures), XmlSerializationContext (serialization contract), DynamicConstruction (WIP code). Phase 1 greenlit for immediate start (private fields/locals only, zero public API changes). Collection mapping reference + risk mitigations + success criteria documented. Architectural insight: not all non-generic collections are debt.

**2026-07-15 — Phase 2 Public API Spec:** 7 change groups, 12 files. IExecutive.LiveDetachableEvents/EventList return types → IReadOnlyList<T>. Vertex edges return types. ResourceManager.Resources → IReadOnlyList<IResource>. ExecEvent is internal, covariance safe. ExecutiveFastLight._ExecEvent doesn't implement IExecEvent (broken at runtime, opportunity to fix by returning empty list). TestQueues.cs:886 calls .Clear() (already NotSupportedException). PredecessorEdges/SuccessorEdges NOT changed (deep contracts, 20+ callers). No Vertex subclasses in codebase. Highest risk: Vertex.cs XML deserialization hardcodes ArrayList cast—change to (IList) and validate with TestGraphPersistence.

**2026-07-15 — Configuration Modernization Architecture:** 6 call sites use ConfigurationManager (Executive, ExecutiveFastLight, ExecFactory, ModelConfig, DiagnosticAids, EmissionsService across 5 files). All sites have defaults—library works without app.config. Library-safe pattern chosen: POCO options classes with optional constructor parameters, no IOptions<T>, no Microsoft.Extensions dependency. No simulation determinism risk (config controls thread pool, causality enforcement, diagnostics, emissions—not event ordering/RNG). Only public API concern: ModelConfig on IModel.ModelConfig (class preserved, uses Dictionary internally, string sectionName constructor marked Obsolete). Utility/ConfigurationManager.cs is dead code (delete). EmissionsServiceConfigurationHandler implements IConfigurationSectionHandler (mark Obsolete, defer deletion). Migration order: DiagnosticAids → Executive/ExecutiveFastLight → ExecFactory → ModelConfig → EmissionsService → cleanup. Skill extracted for library-safe options pattern.

**2026-07-15 — Nullable Reference Types Migration Architecture:** 4,446 warnings when <Nullable>enable</Nullable> set. Warning distribution: CS8618 (constructor init, 31%, 1,378 violations) > CS8625 (null literal, 22%, 988) > CS8600 (null conversion, 18%, 812) = 71% of total. Module distribution: Graphs (1,160) > ItemBased (656) > Materials (568) > Core (502) > Utility (484) > Mathematics (290) > Resources (264) > Persistence (186) > others. Persistence worst per-file (186/7 = 26.6/file). Recommended approach: Global enable + #nullable disable pragma per file, remove file-by-file (Microsoft pattern, clear tracking). SageTestLib deferred (test files pass null as inputs, annotations add noise). Permanent disable files: WeakHashTable.cs, Persistence/XmlSerializationContext.cs, Persistence/CreationContext.cs (XML deserialization). Locked exclusions: object userData → object? no genericize, IDictionary graphContext → non-generic annotate nullable only. 4-phase plan: Phase 1 (Core interfaces + options + ExecEvent, ~21 files, 1-2h), Phase 2 (Core engine, ~12 files, 4-6h), Phase 3 (remaining modules, 12 batches by risk, weeks), Phase 4 (cleanup + WarningsAsErrors).

**2026-07-15 — CA Rule Analysis:** 20 suppressed rules tested (violations counted per rule). Tier 1 ready for immediate enablement: CA2200 (10), CA1001 (2), CA2215 (2), CA1816 (12), CA2213 (18), CA1825 (20), CA1052 (24) = 88 total, all mechanical, non-breaking. Tier 2 future: CA1063 (20), CA2214 (24), CA2000 (34), CA1822 (124), CA1032 (130) = 332 total, mostly mechanical, judgment required. Tier 3 defer: CA1051 (184, breaking), CA1805 (344, low value), CA2201 (456, hierarchy redesign), CA1062 (1122, false positives). Several exceptions inherit ApplicationException (legacy .NET 1.x). CA1051 violations primarily in Materials/Chemistry DTOs. CA1062 noisy—many false positives. Rules NOT tested: CA1707 (>500, previously crashed), naming rules (500+, breaking API), localization (no UI), API design (breaking). Recommendation: Start Tier 1 (7 rules, 88 fixes, 4-6h for Parker), enable one-by-one, Tier 2 in next pass.

**2026-07-15 — Core Engine Analysis for Test Coverage:** Deep analysis of src/Sage/Core/ identifying correctness-critical behaviors. 13 components across 25+ files: Executive, ExecutiveFastLight, ExecEvent/ExecEventComparer/ExecEventRemover, ExecFactory, StateMachine, EnumStateMachine, DetachableEvent, DetachableEventSynchronizer, Model, Metronome, ExecController, InitializationManager, SageOptions. 130+ testable claims identified with unique IDs (E-ORD-01 through INIT-07). Executive vs ExecutiveFastLight causality divergence: Executive throws CausalityException or returns long.MinValue; ExecutiveFastLight silently clamps to _now + logs. ExecutiveFastLight state tracking bug: _execState initialized to Stopped, never updated (always returns Stopped). StateMachine Prepare handlers don't short-circuit (intentional, allows full validation reporting). DetachableEvent thread identity assertions (Debug.Assert only, not enforced in Release). Event removal deferred + batched (not immediate, may fire between UnRequestEvent call and next loop if called from handler). Recommended test priority order: Event ordering → Causality → Lifecycle → Cancellation → StateMachine → Detachable → Model → Rest.

