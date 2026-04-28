# Squad Decisions

## Active Decisions

### 2026-04-28: Phase 3 Resource Manager — Typed Read-Only Public Surfaces (`p3-resource-manager`) (COMPLETE ✅)

**By:** Ripley (gate), Hudson (map, review), Parker (implement), Hudson (review & approval)

**Date:** 2026-04-28

**Status:** Complete — Approved for merge

**Decision:** Implement the first `p3-resource-manager` slice as a typed read-only collection exposure only, modernizing public surfaces where the implementation is already read-only today while deferring lifecycle, persistence, and versioning work.

**Scope Gate (Ripley):** Narrow to typed read-only collection surfaces on the approved resource-manager boundaries.

**Exact Breaking API Changes:**
- `IResourceManager.Resources` → `IReadOnlyList<IResource>`
- `ResourceManager.Resources` → `IReadOnlyList<IResource>`
- `SelfManagingResource.Resources` → `IReadOnlyList<IResource>`
- `MaterialResourceItem.Resources` → `IReadOnlyList<IResource>`
- `IResourceManagerCollection.GetResourceManagers()` → `IReadOnlyCollection<IResourceManager>`
- `ResourceManagerCollection.GetResourceManagers()` → `IReadOnlyCollection<IResourceManager>`

**Explicit Boundaries (do not change):**
- ❌ `Reserve`, `Acquire`, `Unreserve`, `Release` behavior
- ❌ Waiter ordering and prioritized request semantics
- ❌ Event delegate shapes
- ❌ Add/Remove/Clear mutators
- ❌ Indexer semantics
- ❌ Constructor/serialization identity work
- ❌ XML payload/shape changes
- ❌ Versioning/release coordination

**Fallout Map (Hudson):**
- Production hotspots: `MaterialConduitManager.cs`, `ResourceServer.cs`, `MaterialService.cs`, resource implementations, test/sample files
- Regression coverage added: `TestResourceManagerCollectionLifecycleAndLookup` (behavioral only, no type assertion)
- Decision: Do not add public-shape tests during fallout map; those are the intended break seams

**Implementation (Parker):**
- Updated `ResourceManager` to expose `_resources.AsReadOnly()` directly
- Updated `SelfManagingResource` to forward typed read-only list
- Updated `MaterialResourceItem` to return singleton resource via `Array.AsReadOnly(...)`
- Fixed `TestResources.cs` off `IList.Contains(...)` patterns
- Scope adherence: Stayed within typed read-only public surface boundary; no behavioral changes

**Review & Approval (Hudson):**
- Added `TestResourceManagerApiCollectionsAreTypedAndReadOnly` with reflection-based API-shape assertions and read-only runtime verification
- Provides explicit tripwires so future backsliding to `IList`/`ICollection` fails fast
- Validation: Sage build ✅, Sage.Materials build ✅, resource/server slice 20/20 ✅
- Verdict: Approved for merge. No revision handoff required.

**Quality Metrics:**
- Build: ✅ Clean
- `Sage.Tests` resource/server slice: ✅ 20/20 passing
- Regression coverage: ✅ Tripwire added for public-surface break
- Scope adherence: ✅ 100%

**Key Learning:** For public-surface breaks, simple compile-fallout fixes are not sufficient. Explicit tripwires (reflection-based API-shape assertions + read-only verification) are needed so later backsliding fails fast.

---

### 2026-07-17: Phase 3 Batch 2 (`p3-edge-vertex`) — Concrete Graph API Alignment (COMPLETE ✅)

**By:** Parker, Hudson, Ripley (multi-turn cycle)

**Date:** 2026-07-17

**Status:** Complete — Approved for merge

**Decision:** Align concrete `Edge` and `Vertex` public API with interface break from `p3-interfaces` batch. Full public surface modernization with fallout remediation contained to direct consumers.

**Scope Gate:**

Concrete public API must expose:
- `Vertex.PredecessorEdges` / `Vertex.SuccessorEdges` → `IReadOnlyList<Edge>`
- `Edge.PreVertex` / `Edge.PostVertex` → `IVertex?`
- `Edge.ChildEdges` → `IReadOnlyList<Edge>`
- `Edge.PredecessorEdges` / `Edge.SuccessorEdges` → `IReadOnlyList<Edge>`

**Explicit Boundaries (do not change):**
- Internal storage fields and backing collection types
- XML serialization payload/shape
- `IEdge.GetParent()` signature
- `Vertex.PrincipalEdge` concrete type
- Existing add/remove mutator methods

**Execution Timeline:**

1. **Parker Initial Attempt:** Implemented scope but concrete public surface still exposed legacy `IList`/`Vertex` shapes
2. **Hudson Rejection:** Added concrete API regression tests; found implementation incomplete. Locked Parker out; assigned to Ripley
3. **Ripley Revision:** Re-scoped as public-surface-only pass; fully aligned concrete API to interface break; remediated fallout
4. **Hudson Approval:** Validated concrete surface, fallout repairs, regression coverage. Approved for merge

**Coverage Decision:** Did NOT add extra tests. Current suite (interface behavior, concrete reflection, end-to-end graph tests) is the right tripwire level. Adding low-level tests would over-lock out-of-scope seams.

**Validation:**
- Build: ✅ Clean
- `Sage.Tests`: ✅ 293/293 passing

**Result:** Batch approved and ready for merge.

---

### 2026-04-28: Phase 3 Reactions — Typed Read-Only Public Surfaces (`p3-reactions`) (COMPLETE ✅)

**By:** Ripley (gate), Hudson (map, review), Parker (implement)

**Date:** 2026-04-28

**Status:** Complete — Approved for merge

**Scope Gate (by Ripley):** Narrow to typed read-only public surfaces only. Defer reaction math, sequencing, XML/persistence, construction seams, resource-manager, and versioning.

**Decision:** Implement the opening reactions batch as a typed read-only public-surface pass only:
- `Reaction.Reactants` / `Reaction.Products` → `IReadOnlyList<Reaction.ReactionParticipant>`
- `ReactionProcessor.Reactions` → `IReadOnlyList<Reaction>`
- `ReactionProcessor.GetReactionsByParticipant` / `GetReactionsByReactant` / `GetReactionsByProduct` → `IReadOnlyList<Reaction>`
- `ReactionProcessor.CombineMaterials(... out observedReactions, out observedReactionInstances)` → typed read-only collections

**Explicit Boundaries (do not change):**
- ❌ Reaction math and catalyst handling
- ❌ Event sequencing
- ❌ XML field-name or payload-shape changes
- ❌ Constructor/deserialization seams
- ❌ Resource-manager/versioning work

**Fallout Map (by Hudson):**
- Production compile hotspots: `Reaction.cs`, `ReactionProcessor.cs`, `ReactionInstance.cs`
- Test fallout: `TestChemistry.cs` and supporting regression hotspots
- Pre-change gates: ✅ Green

**Implementation (by Parker):**
- `Reaction.Reactants`/`Products`: Changed to `IReadOnlyList<ReactionParticipant>`
- `ReactionProcessor.Reactions`: Changed to `IReadOnlyList<Reaction>`
- Query methods: All return `IReadOnlyList<Reaction>`
- `CombineMaterials(...)`: Typed observable outputs
- Fixed immediate fallout: `ReactionInstance` string rendering, `TestChemistry` off legacy `ArrayList`

**Review & Approval (by Hudson):**
- Added regression coverage: assertions locking API shape and observed reaction/result behavior
- Tightened `TestChemistry` with typed/read-only reaction surface locks
- Validation: `Sage.Materials` build ✅, `Sage.Materials.Tests` 22/22 ✅, chemistry slice 4/4 ✅
- Verdict: Approve for merge

**Quality Metrics:**
- Build: ✅ Clean
- `Sage.Materials.Tests`: ✅ 22/22 passing
- Chemistry regression slice: ✅ 4/4 passing
- Test suite status: Approved with explicit regression lock

---

### 2026-04-28: Phase 3 Graph API Opening — Regression / Compile Fallout Map (COMPLETE ✅)

**By:** Hudson

**Date:** 2026-04-28

**Status:** Complete

**Summary:** Mapped the current regression and compile fallout surface for the opening Phase 3 graph API batch focused on `IEdge`, `IVertex`, `Edge`, and `Vertex`.

**Decision:** Do **not** add new characterization tests yet. The current graph interfaces are still tightly coupled to concrete `Edge`/`Vertex` types, so any low-level signature test would only handcuff the intended API break instead of protecting stable behavior.

**Findings:**

1. `IGraph` currently has no in-repo footprint — `src\Sage\Graphs\IGraph.cs` does not exist
2. Interfaces are not abstract seams yet — `IEdge` exposes concrete `Vertex?` types; `IVertex` exposes concrete `Edge?`
3. Production compile hotspots (12 files): `ChannelMonitor.cs`, `MultiChannelEdgeReceiptManager.cs`, `Ligature.cs`, `PathLength.cs`, `VertexSynchronizer.cs`, `DagCycleChecker.cs`, `DagDeadlockChecker.cs`, `DagStructureError.cs`, `CPMAnalyst.cs`, `PertAnalyst.cs`, `DiagnosticAids.cs`, `Task.cs`
4. Test fallout hotspots (7 files): `TestDAGCycleChecker.cs`, `TestGraphBranching.cs`, `TestGraphAlgorithmRegressions.cs`, `TestGraphValidities.cs`, `TestGraphPersistence.cs`, `TestTasks.cs`, `TestTasks2.cs`

**Validation:**
- Baseline build: ✅ Clean
- Baseline tests: ✅ Passing

---

### 2026-04-28: Phase 3 Graph Interfaces — Breaking API Batch 1 Scope Gate (ACTIVE ✅)

**By:** Ripley

**Date:** 2026-04-28

**Status:** Active — First batch scoped, Parker ready to implement

**Decision:** Split `p3-interfaces` and `p3-edge-vertex` into two separate batches:

**Batch 1 (this scope gate): `p3-interfaces` — Interface signature changes ONLY**

Parker is authorized to change the following **interface signatures only**, with no implementation/serialization changes:

1. **`IVertex.PredecessorEdges` and `IVertex.SuccessorEdges`** → `IReadOnlyList<Edge>`
   - Current: `IList`
   - Breaking changes: Source/binary breaking for callers using mutating methods
   - Implementation: Properties already return `PreEdges.AsReadOnly()` and `PostEdges.AsReadOnly()`
   - No internal storage changes required

2. **`IEdge.PreVertex` and `IEdge.PostVertex`** → `IVertex?`
   - Current: `Vertex?`
   - Breaking changes: Source/binary breaking for callers casting to concrete `Vertex`
   - Implementation: Covariance-safe, properties can return as `IVertex?`
   - No internal field type changes required

3. **`IEdge.ChildEdges`** → `IReadOnlyList<Edge>`
   - Current: `IList`
   - Breaking changes: Source/binary breaking for callers using mutating methods
   - Implementation: Replace `ArrayList.ReadOnly(ArrayList.Adapter(_childEdges))` with `.AsReadOnly()`
   - No serialization changes required

**Rationale for Split:**
- Binary/source breaking scope isolation
- Risk tiering: Interface vs implementation changes are different risk classes
- XML serialization boundary: Don't change serialization shape and interface in same batch
- Test/validation checkpoint: Validate interface changes before internal storage changes
- Subclass exposure: `Task : Edge` and `Ligature : Edge` are in the wild

**Explicitly Out of Scope for Batch 1:**
- ❌ `IEdge.GetParent()` signature change
- ❌ Internal storage modernization in `Vertex` or `Edge`
- ❌ XML serialization shape changes
- ❌ `IVertex.PrincipalEdge` return type change
- ❌ `Vertex.AddPreEdge()` / `AddPostEdge()` parameter types

**Migration Order:**
1. Update `IVertex` interface
2. Update `IEdge` interface
3. Update `Vertex` class
4. Update `Edge` class
5. Fix consumer compilation errors
6. Run targeted test suite

**Success Criteria:**
- ✅ All graph interface return types use `IReadOnlyList<T>` or covariant `IVertex`/`IEdge`
- ✅ Zero changes to serialization shape
- ✅ Zero changes to internal storage types
- ✅ All graph tests pass
- ✅ Subclasses compile and pass tests
- ✅ PFC integration tests pass

**Authorization:**
- Parker: Proceed with Batch 1 (`p3-interfaces`) only
- Hudson: Add regression tests before Parker starts
- Coordinator: Do not approve Batch 2 until Batch 1 is validated

---

### 2026-04-28: Parker — Phase 3 Batch 1 (`p3-interfaces`) Implementation (COMPLETE ✅)

**By:** Parker

**Date:** 2026-04-28

**Status:** Complete

**Decision:** Land the Phase 3 interface break with explicit interface adapters in `Edge` and `Vertex`, rather than immediately changing the concrete public property types.

**Why**

Ripley's scope gate for Batch 1 named the interface contracts only and explicitly warned against widening into `p3-edge-vertex`. Explicit interface implementation lets the interface break happen now while keeping the concrete-class blast radius narrow.

**Applied Shape**

- `IEdge.PreVertex` / `PostVertex` → `IVertex?`
- `IEdge.ChildEdges` → `IReadOnlyList<Edge>`
- `IVertex.PredecessorEdges` / `SuccessorEdges` → `IReadOnlyList<Edge>`
- `Edge` keeps its current public `Vertex?` / `IList` members for now
- `Vertex` keeps its current public `IList` members for now

**Consequence for Later Batches**

Later `p3-edge-vertex` work can still choose to align the concrete `Edge`/`Vertex` public properties with the new interface shapes, but that is now a separate, deliberate step instead of accidental fallout from Batch 1.

---

### 2026-04-28: Hudson — Review of Phase 3 Batch 1 (`p3-interfaces`) (COMPLETE ✅)

**By:** Hudson

**Date:** 2026-04-28

**Status:** Complete

**Decision:** Approve Parker's Batch 1 implementation and lock the regression net at the interface boundary.

**Why**

Parker changed only the scoped interface signatures:
- `IVertex.PredecessorEdges` / `SuccessorEdges` → `IReadOnlyList<Edge>`
- `IEdge.PreVertex` / `PostVertex` → `IVertex?`
- `IEdge.ChildEdges` → `IReadOnlyList<Edge>`

The explicit interface implementations in `Edge` and `Vertex` keep the legacy concrete public properties alive for now, which matches the scope gate and avoids dragging Batch 2 work into Batch 1.

**QA Action Taken**

Added regression coverage in `tests\SageTestLib\TestGraphBranching.cs` that:
- Casts through `IEdge` and `IVertex`
- Verifies the new typed interface signatures are directly usable
- Verifies the returned collections are read-only
- Verifies interface endpoints still reference the same concrete vertex instances

**Validation**

- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-build` ✅ `290/290`

**Consequence**

Batch 1 is clear to merge. Any later work that changes the concrete `Edge` / `Vertex` public property types should happen in the separate follow-up batch, with this interface-focused regression coverage kept as the guardrail.

---


### 2026-04-26: Phase 2 Graph Algorithms — Internal Modernization, Public Surface Preservation (COMPLETE ✅)

**By:** Parker

**Date:** 2026-04-26

**Status:** Complete

**Context:** Phase 2 graph-algorithms slice targeting `src\Sage\Graphs\PertAnalyst.cs`, `CPMAnalyst.cs`, and `DagDeadlockChecker.cs`.

**Decision:** Treat this batch as **internal collection modernization only**. Preserve public/protected legacy collection shapes where they are part of the contract or likely subclass touchpoints.

**Applied Guardrails:**
- Kept `PertAnalyst.CriticalPath` returning `ArrayList`
- Did not change `DagDeadlockChecker.GetSuccessors(object)` or `Errors`
- Did not convert `CPMAnalyst` protected `Hashtable` fields in this batch
- Continued honoring intentional public `IDictionary graphContext` signatures elsewhere

**Implementation Pattern:** Prefer `List<T>`, `HashSet<T>`, and `Dictionary<TKey,TValue>` behind existing boundaries, adapting back to legacy collection types only at the API edge when needed.

**Validation:** `dotnet build .\src\Sage\Sage.csproj --no-restore` and targeted graph tests for `GraphValidityTester` + `DAGCycleCheckerTester` passed.

**Result:**
- Build: 0 errors, 0 warnings ✅
- Graph algorithm tests: 12/12 passing ✅

---

### 2026-04-26: Hudson Graph Algorithm Regression Coverage (COMPLETE ✅)

**By:** Hudson

**Date:** 2026-04-26

**Status:** Complete

**Context:** Adding regression test coverage for Phase 2 graph-algorithms batch to lock legacy public surfaces during internal modernization.

**Decision:** Add comprehensive regression tests for `CpmAnalyst`, `PertAnalyst`, and `DagDeadlockChecker` in `tests\SageTestLib\TestGraphAlgorithmRegressions.cs`.

**Test Semantics Locked:**
- `PertAnalyst.CriticalPath` remains read-only `ArrayList`
- `DagDeadlockChecker.Errors` stays read-only/non-generic at the boundary
- Duplicate successor references must not create duplicate frontier/error targets
- Simple reachable cycle must report one residual frontier target
- Implementation-sensitive ordering/exhaustiveness for complex deadlock sets left intentionally flexible

**Result:**
- New regression tests: 4/4 passing ✅
- Targeted graph algorithm suite: 13/13 passing ✅
- Full `Sage.Tests`: 285/285 passing ✅

---

### 2026-07-17: Phase 3 Dynamic Construction — Typed Read-Only Child Traversal (`p3-dynamic-construction`) (COMPLETE ✅)

**By:** Ripley (gate), Hudson (map, review), Parker (implement), Hudson (review & approval)

**Date:** 2026-07-17

**Status:** Complete — Approved for merge

**Decision:** Implement the opening `p3-dynamic-construction` batch as a typed read-only child-traversal surface pass only.

**Scope Gate (Ripley):** Narrow to typed read-only child-traversal surfaces in `src\Sage\Persistence\DynamicConstruction.cs`:

- `IBindsToCreationContext.BindableChildren` → `IReadOnlyList<IBindsToCreationContext>`
- `IHasSubRequirements.SubRequirements` → `IReadOnlyList<IRequirement>`
- `ISpecification.GetChildRequirements(bool)` → `IReadOnlyList<IRequirement>`
- `ISpecification.GetChildSpecifications(bool)` → `IReadOnlyList<ISpecification>`

**Explicit Boundaries (do not change):**
- ❌ Constructors and `CreationContext`
- ❌ Activation flags and `CREATION_CONTEXTS` / `INCLUDE_WIP` gating
- ❌ Persistence seams and XML shape changes
- ❌ Object-typed factory contracts
- ❌ SemVer coordination and versioning decisions

**Fallout Map (Hudson):**
- Key finding: There is no in-repo `IBindableChildren` symbol; the actual seam is `IBindsToCreationContext.BindableChildren`
- The entire dynamic-construction stack is dormant behind `#if INCLUDE_WIP`
- Direct production file: `src\Sage\Persistence\DynamicConstruction.cs`
- Compile-time consumer: `src\Sage\Core\Model.cs` (gated hook only)
- No current test coverage or external consumers
- Decision: Do **not** add characterization tests — code is dormant and has no live consumer seam, so tests would lock implementation detail instead of protecting shipped behavior

**Implementation (Parker):**
- Replaced internal `ArrayList` child-tracking fields with typed `List<T>` storage
- Exposed `.AsReadOnly()` at public boundaries
- Kept constructors, `CreationContext`, activation, and factory contracts untouched
- Did not widen into dormant `CREATION_CONTEXTS` / `INCLUDE_WIP` activation debt
- Validation: `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅, `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore --no-build` ✅ (296/296)

**Review & Approval (Hudson):**
- Confirmed scope adherence: no tests added, dormant code with no live seam
- Validated baseline Sage build and Sage.Tests remain green
- Confirmed known activation blockers (`ICreationContext.Model` mismatch, missing `AddCreationContext` seam, unrelated `TimeBasedSelection` debt) are not caused by Parker's changes
- Verdict: Approved for merge. Dormant-feature activation debt remains deferred to later work.

**Quality Metrics:**
- Build: ✅ Clean
- `Sage.Tests`: ✅ 296/296 passing
- Scope adherence: ✅ 100%
- Test additions: ✅ None (code is dormant)

**Key Learning:** When working with dormant (#if-gated) code that has no live in-repo consumer, do not add characterization tests — they lock pre-activation implementation detail instead of protecting shipped behavior. Focus scope gates on keeping future activation unblocked.

---

### 2026-04-28: Phase 3 Version Bump — Non-Publishing Closeout (`p3-version-bump`) (COMPLETE ✅)

**By:** Copilot, Ripley, Bishop

**Date:** 2026-04-28

**Status:** Complete — Approved and implemented

**Directive captured:** Do **not** publish anything to NuGet or GitHub in this phase. A version bump is allowed if needed.

**Decision:** Close Phase 3 with an internal metadata-only major version alignment to **`5.0.0`**. Do not use a preview suffix, and do not expand this pass into package identity, release automation, migration-doc, or publication work.

**Why:**
- Phase 3 already introduced deliberate public API breaking changes
- Leaving the repo on SDK-default `1.0.0` metadata would misrepresent the codebase state
- A preview suffix has no value when nothing is being externally published

**Implementation boundary:**
- Centralize explicit version metadata for the shipping libraries only
- Stamp:
  - `Version` = `5.0.0`
  - `PackageVersion` = `5.0.0`
  - `AssemblyVersion` = `5.0.0.0`
  - `FileVersion` = `5.0.0.0`
  - `InformationalVersion` = `5.0.0`
- Keep the change metadata-only:
  - ❌ no serializer/XML/PFC format version change
  - ❌ no TFM/runtime-floor change
  - ❌ no package-ID/family decision
  - ❌ no package split/release automation/publishing/docs batch

**Implementation result:**
- `Directory.Build.props` now centrally stamps `Sage`, `Sage.Materials`, and `Sage.PFC` to the 5.x line
- Solution build/test remained green after the metadata update

**Deferred to a future real release batch:**
- final package identity/family
- artifact split strategy
- public support/runtime-floor messaging
- external GA vs preview release-channel decision
- release-facing migration/changelog documentation

---

### 2026-04-28: Phase 3 Version Bump — Non-Publishing Closeout (`p3-version-bump`) (COMPLETE ✅)

**By:** Copilot, Ripley, Bishop

**Date:** 2026-04-28

**Status:** Complete — Approved and implemented

**Directive captured:** Do **not** publish anything to NuGet or GitHub in this phase. A version bump is allowed if needed.

**Decision:** Close Phase 3 with an internal metadata-only major version alignment to **`5.0.0`**. Do not use a preview suffix, and do not expand this pass into package identity, release automation, migration-doc, or publication work.

**Why:**
- Phase 3 already introduced deliberate public API breaking changes
- Leaving the repo on SDK-default `1.0.0` metadata would misrepresent the codebase state
- A preview suffix has no value when nothing is being externally published

**Implementation boundary:**
- Centralize explicit version metadata for the shipping libraries only
- Stamp:
  - `Version` = `5.0.0`
  - `PackageVersion` = `5.0.0`
  - `AssemblyVersion` = `5.0.0.0`
  - `FileVersion` = `5.0.0.0`
  - `InformationalVersion` = `5.0.0`
- Keep the change metadata-only:
  - ❌ no serializer/XML/PFC format version change
  - ❌ no TFM/runtime-floor change
  - ❌ no package-ID/family decision
  - ❌ no package split/release automation/publishing/docs batch

**Implementation result:**
- `Directory.Build.props` now centrally stamps `Sage`, `Sage.Materials`, and `Sage.PFC` to the 5.x line
- Solution build/test remained green after the metadata update

**Deferred to a future real release batch:**
- final package identity/family
- artifact split strategy
- public support/runtime-floor messaging
- external GA vs preview release-channel decision
- release-facing migration/changelog documentation

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
