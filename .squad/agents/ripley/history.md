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

### 2025-07-15 — Full Architectural Assessment

**Solution structure:**
- Single monolithic assembly `Highpoint.Sage.dll` from `src/Sage/Sage.csproj` targeting .NET 8
- 548 .cs source files across 16 module directories
- ~304 MSTest tests in `Sage_Aux/SageTestLib/`
- Builds clean (0 errors, ~3,746 warnings)

**Core engine architecture:**
- Two `IExecutive` implementations: `Executive` (full-featured, ~1,150 lines) and `ExecutiveFastLight` (single-threaded heap-based, ~900 lines)
- Both are `internal sealed` classes, instantiated via `ExecFactory` singleton using reflection
- `Executive` uses `SortedList` (non-generic) as event queue — O(n) insert/dequeue. This is the #1 performance bottleneck
- `ExecutiveFastLight` uses a proper binary min-heap — O(log n) — but lacks priority, rescission, detachable events
- Event dispatch: delegate-based `ExecEventReceiver(IExecutive exec, object userData)` — untyped userData everywhere
- Three event types: Synchronous (inline), Detachable (coroutine-like via Task.Run + ManualResetEventSlim), Asynchronous (fire-and-forget)
- `DetachableEvent` in `Sage/Core/DetachableEvent.cs` — suspend/resume semantics via thread signaling

**Key files:**
- `Sage/Core/IExecutive.cs` — primary interface
- `Sage/Core/Executive.cs` — full executive (SortedList-based)
- `Sage/Core/ExecutiveFastLight.cs` — fast executive (heap-based)
- `Sage/Core/ExecEvent.cs` — event record with disabled object pool
- `Sage/Core/ExecFactory.cs` — singleton factory with reflection-based construction
- `Sage/Core/ExecController.cs` — rate throttling + render frame dispatch (visualization support)
- `Sage/Core/Model.cs` — simulation container, owns executive + state machine + services
- `Sage/Core/StateMachine.cs` — two-phase-commit state machine
- `Sage/Core/DetachableEvent.cs` — coroutine implementation

**Legacy patterns identified:**
- 304 ArrayList, 265 Hashtable, 31 non-generic SortedList uses
- 250 ApplicationException throws
- No nullable reference types enabled
- 89 public delegate declarations (vs modern EventHandler<T>/Action<T>)
- Configuration via System.Configuration.ConfigurationManager (app.config XML)
- MarshalByRefObject on Executive class
- Mix of `_camelCase` and `m_camelCase` field naming
- ExecEvent object pool exists but is disabled (`_usePool = false`)

**Visualization readiness:**
- `ExecController` already supports frame rate + time scaling + Render event
- `EventAboutToFire` / `EventHasCompleted` monitors exist
- Missing: structured event stream, snapshot mechanism, change notifications, spatial model

**Modernization priorities (ordered):**
1. Replace SortedList with priority queue in Executive
2. Enable nullable reference types
3. Replace non-generic collections with generic equivalents
4. Modernize configuration (remove ConfigurationManager)
5. Type-safe event data (replace `object userData`)
6. Exception hierarchy cleanup (replace ApplicationException)
7. Observable event stream for visualization
8. Update test infrastructure (MSTest 3.x, SDK 17.x)
9. Project splitting (monolith → focused packages)
10. Modern C# idioms (file-scoped namespaces, records, patterns)

### 2026-03-06 — TupleSpace Failures Resolved ✅

**Status:** Fixed and merge-ready  
**Tests:** 304/304 passing  
**Branch:** `feature/dotnet10`  
**Commit:** `5276d47`

**Root Cause:** Pre-existing race condition in `Exchange.NonBlockingPost()` (not thread pool starvation). Generic `HashtableOfLists<TKey,TValue>` indexer throws `KeyNotFoundException` if key doesn't exist. .NET 10's different thread pool timing exposed this timing-dependent bug.

**Fix Applied:** Added `ContainsKey()` checks in `Exchange.NonBlockingPost()` before accessing `_waitersToRead` and `_waitersToTake` dictionaries.

**Option 2 (Explicit Thread) Evaluation:** Tested but NOT RECOMMENDED—fixes TupleSpace but breaks ResourceManager tests. Exchange.cs fix alone is sufficient and surgical.

**Files Modified:** Only `Sage/Utility/Exchange.cs` (2 guard checks)

**Recommendation:** ✅ Merge `feature/dotnet10` to main immediately. No architectural changes needed.

### 2026-03-06 — Collection Modernization Strategy (COMPLETE ✅)

**Status:** Strategy merged to decisions.md

**Deliverable:** `.squad/decisions/decisions.md` → "Decision: Non-Generic Collection Modernization — Three-Phase Strategy" (deduplicated, comprehensive)

**Key decision captured:**
- 3-phase risk tiering: Phase 1 (60%, internal, non-breaking, low risk), Phase 2 (30%, public API, breaking, medium risk), Phase 3 (10%, intentional, never replace)
- **Critical exclusions locked:** `object userData` (intentional heterogeneous payloads), `IDictionary graphContext` (intentional polymorphic execution context — 50+ signatures), XmlSerializationContext (serialization contract), DynamicConstruction (WIP code)
- **Phase 1 greenlit for immediate start:** Private fields/local variables only, zero public API changes, all 310 tests as validation gate
- **Collection mapping reference** provided (ArrayList→List, Hashtable→Dictionary, etc.)
- **Risk mitigations** documented (thread safety, ordering differences, casting differences)
- **Success criteria** established for each phase

**Architectural insight:** Not all non-generic collections are technical debt. `object userData` enables heterogeneous event payloads across any simulation model. `IDictionary graphContext` provides runtime flexibility for graph execution contexts (analogous to ASP.NET ViewData). These are intentional design patterns, not modernization targets.

**Parker's detailed inventory** (.squad/decisions/inbox/parker-collection-inventory.md) provides file-by-file implementation guide with difficulty tiers.

### 2026-07-15 — Phase 2 Public API Spec (COMPLETE ✅)

**Deliverable:** `.squad/decisions/inbox/ripley-phase2-api-spec.md`

**Scope confirmed:** 12 files, 7 change groups covering all public API collection replacements.

**Key findings during analysis:**

- `ExecEvent` is `internal` — confirmed `ExecEvent.cs:9`. The public interface `IExecutive.EventList` must return `IReadOnlyList<IExecEvent>`, NOT `IReadOnlyList<ExecEvent>`. The covariance of `IReadOnlyList<out T>` means `Executive.cs` can return `ReadOnlyCollection<ExecEvent>` (from `snapshot.AsReadOnly()`) and it satisfies the covariant `IReadOnlyList<IExecEvent>` — no casting needed.

- `ExecutiveFastLight._ExecEvent` is a private nested class that does NOT implement `IExecEvent`. The existing `EventList` implementation on `ExecutiveFastLight` is already broken at runtime (elements can't be cast to `IExecEvent`). Phase 2 is an opportunity to fix this by returning an empty `IReadOnlyList<IExecEvent>` from that implementation (the fast executive doesn't support rescindable events anyway).

- `TestQueues.cs:886` calls `_executive.EventList.Clear()` — this was already a `NotSupportedException` at runtime since `EventList` has always returned a `ReadOnly`-wrapped list. Compile-time fix is a bonus of the type change.

- `IVertex.PredecessorEdges` / `IVertex.SuccessorEdges` return `IList` — these are intentionally NOT changed in Phase 2 since they're deep interface contracts with 20+ callers. Only the backing `protected ArrayList` fields in `Vertex.cs` are updated to `List<Edge>`.

- No subclasses of `Vertex` exist in the codebase — the `protected` field change is safe without a broader subclass audit.

- Non-generic `HashtableOfLists` appears in only 1 active production call site (`ProcedureFunctionChart.cs:2594`) — migrates cleanly to `HashtableOfLists<string, IPfcElement>` since both `IPfcNode` and `IPfcLinkElement` inherit `IPfcElement`. The remaining 3 non-generic usages are dead code (`#if NOT_DEFINED` in `TupleSpace.cs`).

- Highest risk item: `Vertex.cs` XML deserialization (`DeserializeFrom`) hardcodes `(ArrayList)xmlsc.LoadObject(...)`. Change the cast to `(IList)` to be resilient. Validate with `TestGraphPersistence`.

**Implementation order spec:** Leaf changes first → implementations → interfaces (forces compile errors) → callers → test files → validate all 310 tests.

### 2026-07-15 — Configuration Modernization Architecture Assessment (COMPLETE ✅)

**Status:** Architecture assessment complete. Decision record and Parker work spec written.

**Deliverables:**
- `.squad/decisions/inbox/ripley-config-modernization-arch.md` — full architecture decision
- `.squad/decisions/inbox/ripley-config-modernization-parker-spec.md` — detailed implementation spec for Parker
- `.squad/skills/library-safe-options/SKILL.md` — reusable pattern for library-safe options

**Key findings:**

1. **6 call sites** use `System.Configuration.ConfigurationManager` across 5 files:
   - `Executive.cs` — reads `WorkerThreads`, `IgnoreCausalityViolations` from "Sage" section (constructor)
   - `ExecutiveFastLight.cs` — reads `IgnoreCausalityViolations` from "Sage" + `ExecBreakAt` from "diagnostics" (constructor)
   - `ExecFactory.cs` — reads `ExecutiveType` from "Sage" section (lazy, in `CreateExecutive(Guid)`)
   - `ModelConfig.cs` — reads arbitrary keys from named section (constructor)
   - `DiagnosticAids.cs` — reads `diagnostics` section for per-key trace flags (static, lazy-init)
   - `EmissionsService.cs` — reads `EmissionsService` custom section via `IConfigurationSectionHandler` (singleton constructor)

2. **All 6 sites have defaults** when config is missing — the library already works without app.config. This makes the migration safe: replace with options POCOs whose defaults match no-config behavior.

3. **No simulation determinism risk** — config keys control thread pool sizing, causality enforcement, diagnostic output, and emissions model selection. None affect event ordering or RNG seeds.

4. **Library-safe pattern chosen:** POCO options classes with optional constructor parameters (`options = null`, coalesced to `new Options()` in body). No `IOptions<T>`, no `Microsoft.Extensions.*` dependency in the library itself. DI extension methods deferred to a follow-up.

5. **Only one public API concern:** `ModelConfig` is on `IModel.ModelConfig`. The class is preserved but rewritten to use `Dictionary<string, string>` internally. The `string sectionName` constructor is marked `[Obsolete]`.

6. **`Utility/ConfigurationManager.cs`** is already dead code (entirely commented out). Delete during cleanup.

7. **`EmissionsServiceConfigurationHandler`** implements `IConfigurationSectionHandler` — a System.Configuration artifact. Mark `[Obsolete]`, defer deletion.

8. **Migration order:** DiagnosticAids (lowest risk) → Executive/ExecutiveFastLight (internal) → ExecFactory (public singleton, additive) → ModelConfig (public interface) → EmissionsService (complex but isolated) → remove package reference + cleanup.

**Skill extracted:** `.squad/skills/library-safe-options/SKILL.md` — reusable pattern for removing ConfigurationManager from .NET class libraries without introducing DI dependencies.

### 2026-07-15 — Nullable Reference Types Migration Architecture (COMPLETE ✅)

**Status:** Architecture assessment complete. Decision record and Parker work spec written.

**Deliverables:**
- `.squad/decisions/inbox/ripley-nullable-arch.md` — full architecture decision with phased plan
- `.squad/decisions/inbox/ripley-nullable-parker-spec.md` — Phase 1 implementation spec for Parker

**Key findings:**

1. **4,446 nullable warnings** when `<Nullable>enable</Nullable>` is set in Sage.csproj. Build succeeds (0 errors).

2. **Warning type distribution:** CS8618 (constructor init) dominates at 1,378 (31%). CS8625 (null literal) at 988 (22%). CS8600 (null conversion) at 812 (18%). These three account for 71% of all warnings.

3. **Module distribution:** Graphs (1,160) > ItemBased (656) > Materials (568) > Core (502) > Utility (484) > Mathematics (290) > Resources (264) > Persistence (186) > Scheduling (134) > SmartPropertyBag (120) > Dependencies (28) > SystemDynamics (28) > Randoms (24).

4. **Persistence is the worst per-file:** 186 warnings in 7 files = 26.6 warnings/file. XML serialization code is inherently null-heavy.

5. **Recommended approach: Global enable + suppress (Option 1).** Add `<Nullable>enable</Nullable>` to csproj, prepend `#nullable disable` to all files, remove pragma file-by-file as each is annotated. This is Microsoft's recommended approach and provides clear migration tracking.

6. **SageTestLib deferred indefinitely.** Test files routinely pass null as test inputs — nullable annotations in tests add noise without safety benefit.

7. **Permanent `#nullable disable` files identified:** WeakHashTable.cs (weak reference collection), Persistence/XmlSerializationContext.cs, Persistence/CreationContext.cs (XML deserialization pipelines).

8. **Locked exclusions respected:** `object userData` → annotate as `object?` but do NOT genericize. `IDictionary graphContext` → keep non-generic, annotate as nullable only where null is actually passed.

9. **4-phase plan:** Phase 1 (Core interfaces + SageOptions + ExecEvent, ~21 files, 1-2 hours), Phase 2 (Core engine implementations, ~12 files, 4-6 hours), Phase 3 (remaining modules in 12 batches by risk, weeks), Phase 4 (cleanup + WarningsAsErrors enforcement).
