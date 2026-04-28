# Project Context

- **Owner:** Stuart Hillary
- **Project:** Sage® Simulation and Modeling Libraries — a long-running discrete event simulation (DES) library originally built on early .NET Framework, now targeting .NET 8.
- **Stack:** C#, .NET 8, NUnit/xUnit, GitHub Actions, NuGet
- **Key modules:** Core (event engine), Scheduling, Graphs, Mathematics, SystemDynamics, ItemBased, Randoms, Persistence, Presentation, SmartPropertyBag, Utility
- **Goals:** Continued development, .NET 8 modernization, performance improvement, future visualization layer
- **PM:** Stuart Hillary (human — sets priorities and direction)
- **Created:** 2026-03-05

## Core Context

**Current Status (April 2026):** Phase 3 graph API opening scope gate approved; Phase 1 collections recovery complete; 351/351 tests passing.

**Key learnings:**
- `PortSet` GUID-backed `Hashtable` is part of XML persistence contract; key semantics ambiguous in docs vs. implementation
- Splitting interface changes from implementation changes isolates breaking-change risk classes
- Graph interfaces are tightly coupled to concrete `Edge`/`Vertex` types; no abstract seam (`IGraph`) exists yet
- Interface changes ripple to all implementers and consumers
- Phase boundaries must be hard and explicit; even Phase 1 recovery can suffer scope creep

## Learnings

### 2026-04-28 — Phase 3 Reactions Scope Gate (`p3-reactions`) ✅

**Status:** Complete — Scope gate set, batch approved for merge

**Learning:** The narrow scope gate for the opening reactions batch is typed read-only public surfaces only. Defer reaction math, sequencing, XML/persistence, construction seams, resource-manager, and versioning.

**Scope Gate Decision:**
- `Reaction.Reactants` / `Reaction.Products` → `IReadOnlyList<Reaction.ReactionParticipant>`
- `ReactionProcessor.Reactions` → `IReadOnlyList<Reaction>`
- `ReactionProcessor.GetReactionsByParticipant` / `GetReactionsByReactant` / `GetReactionsByProduct` → `IReadOnlyList<Reaction>`
- `ReactionProcessor.CombineMaterials(... out observedReactions, out observedReactionInstances)` → typed read-only collections

**Explicit Boundaries (no changes):**
- ❌ Reaction math and catalyst handling
- ❌ Event sequencing
- ❌ XML field names and payload shape
- ❌ Constructor/deserialization seams
- ❌ Resource-manager/versioning work

**Why it matters:** Underlying reaction participant storage is already `List<ReactionParticipant>`, so exposing typed read-only views is a clean public-surface modernization with minimal behavioral risk. Keeping the existing overload set and legacy `_reactions` backing store keeps the batch about API typing instead of widening into storage, sequencing, or persistence work.

**Quality Gates:**
1. `dotnet build .\src\Sage.Materials\Sage.Materials.csproj --no-restore`
2. `dotnet test .\tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj --no-restore`
3. Chemistry regression slice (4 tests)

**Sign-off rule:** If Parker keeps the batch to typed read-only return surfaces and fixes only direct compile fallout, Ripley is satisfied. No changes to reaction math, catalyst handling, event order, or XML field names.

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration log: `.squad/orchestration-log/2026-04-28T22-31-29Z-ripley.md`
- Session log: `.squad/log/2026-04-28T22-31-29Z-p3-reactions.md`

**Outcome:** Scope gate set. Hudson assigned to map fallout. Parker authorized for implementation.

---

### 2026-04-28 — Phase 3 Resource Manager Scope Gate Approved (`p3-resource-manager`) ✅

**Status:** Opening slice scoped

**Learning:** The first `p3-resource-manager` break must stay on the already-read-only collection boundary, not on lifecycle or construction semantics.

- `IResourceManager.Resources` / `ResourceManager.Resources` / `SelfManagingResource.Resources` / `MaterialResourceItem.Resources` should move from `IList` to `IReadOnlyList<IResource>`
- `IResourceManagerCollection.GetResourceManagers()` / `ResourceManagerCollection.GetResourceManagers()` should move from `ICollection` to `IReadOnlyCollection<IResourceManager>`
- Keep `Add` / `Remove` / `Clear`, waiter behavior, acquisition/reservation flow, event delegates, indexer-by-`Guid`, and XML payload shape unchanged in this slice

**Why it matters:** `ResourceManager` already exposes a read-only adapter over an internal `List<IResource>`, tests only rely on read semantics (`Count`, `Contains`, `foreach`), and the one in-repo interface consumer (`MaterialConduitManager`) only enumerates. That makes typed read-only exposure the narrowest useful public break while avoiding determinism-sensitive work in request blocking, event dispatch, and persistence reconstitution.

### 2026-04-28 — Phase 3 Graph Interfaces Scope Gate Approved ✅

**Status:** Batch 1 (`p3-interfaces`) scoped and approved

**Learning:** Phase 3 graph interface modernization (`IEdge`, `IVertex`, `Edge`, `Vertex`) must be split into two batches:

**Batch 1 (`p3-interfaces`) — Interface signature changes ONLY:**
- `IVertex.PredecessorEdges` / `IVertex.SuccessorEdges` → `IReadOnlyList<Edge>`
- `IEdge.PreVertex` / `IEdge.PostVertex` → `IVertex?`
- `IEdge.ChildEdges` → `IReadOnlyList<Edge>`

All changes are source and binary breaking but covariant-safe. No serialization shape changes, no internal storage changes, no `GetParent()` signature change in Batch 1.

**Why it matters:** Splitting interface changes from implementation changes isolates breaking-change risk classes. Interface changes ripple to all implementers (`Task`, `Ligature`) and consumers (PFC, graph algorithms). Implementation changes (internal storage, serialization modernization) require separate XML round-trip validation and different subclass impact analysis. The split gives us a validation checkpoint before touching serialization contracts.

**Authorization:** Parker may proceed with Batch 1 only. Hudson adds regression tests first. Batch 2 requires separate scope gate.

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration log: `.squad/orchestration-log/2026-04-28T21-53-46Z-ripley.md`
- Session log: `.squad/log/2026-04-28T21-53-46Z-phase3-start.md`

---

### 2026-07-17 — Phase 3 Batch 2 Revision Path (`p3-edge-vertex`) 🔧

**Status:** Re-scoped after Hudson rejection

**Learning:** Hudson's tripwires are correct: `p3-edge-vertex` is not done until the declared concrete public members move, not just the interfaces. The required concrete break is:

- `Vertex.PredecessorEdges` / `Vertex.SuccessorEdges` → `IReadOnlyList<Edge>`
- `Edge.PreVertex` / `Edge.PostVertex` → `IVertex?`
- `Edge.ChildEdges` → `IReadOnlyList<Edge>`
- `Edge.PredecessorEdges` / `Edge.SuccessorEdges` should move to `IReadOnlyList<Edge>` in the same revision to avoid leaving a parallel legacy mutation seam on the same collections

**Boundary:** Keep internal storage, XML persistence shape, `GetParent()`, `PrincipalEdge`, and add/remove mutator methods unchanged in this batch. Fix only the direct compile fallout where callers truly require `Vertex` or `IList`, preferring interface-typed locals and explicit casts only at `Vertex`-only seams such as `VertexSynchronizer`.

**Execution:** Parker remains locked out. Ripley owns the scope correction, but a fresh non-Parker implementation owner is required for the code revision itself.

---

### 2026-07-17 — Phase 3 Batch 2 Implemented and Validated (`p3-edge-vertex`) ✅

**Status:** Concrete public surface aligned and regression batch validated

**Learning:** The concrete `Edge` / `Vertex` batch is shippable once the public signatures move and the fallout is repaired at the true `Vertex`-only seams instead of by backing away from the interface break.

- `Vertex.PredecessorEdges` / `Vertex.SuccessorEdges` now expose `IReadOnlyList<Edge>`
- `Edge.PreVertex` / `Edge.PostVertex` now expose `IVertex?`
- `Edge.ChildEdges`, `Edge.PredecessorEdges`, and `Edge.SuccessorEdges` now expose `IReadOnlyList<Edge>`
- Direct fallout was limited to consumer casts and collection typing in graph analyzers, branch/receipt helpers, diagnostics, synchronizer callers, and graph tests
- Storage, XML shape, `GetParent()`, and `PrincipalEdge` remained untouched

**Why it matters:** This preserves the intended public break without destabilizing persistence or execution semantics. The right remediation pattern is local casting at `Vertex`-specific seams, not reintroducing concrete `Vertex` / `IList` types onto the public API.

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore` ✅ `293/293`

---

### 2026-04-28 — Phase 3 Reactions Scope Gate Approved (`p3-reactions`) ✅

**Status:** Opening Batch 3 slice scoped

**Learning:** The opening reactions modernization batch must stay on the typed read-only public surface only:

- `Reaction.Reactants` / `Reaction.Products` → `IReadOnlyList<Reaction.ReactionParticipant>`
- `ReactionProcessor.Reactions` → `IReadOnlyList<Reaction>`
- `ReactionProcessor.GetReactionsByParticipant` / `GetReactionsByReactant` / `GetReactionsByProduct` → `IReadOnlyList<Reaction>`
- `ReactionProcessor.CombineMaterials(... out observedReactions, out observedReactionInstances)` moves its observed outputs off `ArrayList` onto typed read-only reaction/reaction-instance collections, while keeping the current overload set and `IMaterial[]` input shape

**Boundary:** Do not change reaction math, event order, XML field names, default constructors, `InitializeIdentity`, `ReactionParticipant` shape, or the `CombineMaterials` input array in this batch. Those seams are either determinism-sensitive or tied to construction/persistence concerns.

**Why it matters:** `Reaction` already stores participants in `List<ReactionParticipant>`, and the processor mostly exposes read-only query/observation results; that makes this the narrowest public break with the best payoff. The real fallout is local and known (`ReactionInstance`, chemistry tests, and any callers still binding to `IList`/`ArrayList`), so we can modernize the public typing without dragging in resource lifecycle or construction redesign.

**Deferrals:**
- `p3-resource-manager`: no resource/module-adjacent API cleanup, waiter/lifecycle work, or unrelated collection modernization
- `p3-dynamic-construction`: no constructor/factory redesign, no removal of parameterless constructors, no persistence-path repair
- `p3-version-bump`: SemVer major coordination, release packaging, and consumer migration messaging

---

### 2026-04-28 — Phase 3 Dynamic Construction Scope Gate Approved (`p3-dynamic-construction`) ✅

**Status:** Opening slice scoped

**Learning:** `DynamicConstruction.cs` is still dark code: the entire file is behind `#if INCLUDE_WIP`, the only in-repo integration point in `Model` is behind `#if CREATION_CONTEXTS`, neither symbol is defined in the current build, and there is no active test coverage or in-repo consumer footprint beyond that dormant hook. That means the safe Phase 3 batch is a public-surface typing pass only, not feature activation or construction redesign.

**Scope Gate Decision:**
- `IBindsToCreationContext.BindableChildren` plus `Settings` / `Requirement` / `Specification` implementations → `IReadOnlyList<IBindsToCreationContext>`
- `IHasSubRequirements.SubRequirements` plus `Settings` / `Requirement` / `Specification` implementations → `IReadOnlyList<IRequirement>`
- `ISpecification.GetChildRequirements(bool)` → `IReadOnlyList<IRequirement>`
- `ISpecification.GetChildSpecifications(bool)` → `IReadOnlyList<ISpecification>`

**Explicit Boundaries (no changes):**
- ❌ `Bind(CreationContext cc)` parameter or `CreationContext` property typing
- ❌ `ISettings.PerformSettings`, `IRequirement.Meet`, or `ISpecification.Create` object-typed construction contract
- ❌ `ICreationContext` mutable ambient surfaces (`Model`, `ParentObjectStack`, `Whiteboard`, `Specifications`, `ProvisionAll`)
- ❌ GUID generation, requirement satisfaction semantics, or model-registration sequencing
- ❌ parameterless constructors, serializer/deserializer seams, or compile-symbol activation (`INCLUDE_WIP` / `CREATION_CONTEXTS`)

**Why it matters:** The only clearly isolated modernization seam here is collection exposure. `BindableChildren`, `SubRequirements`, and the recursive child enumeration APIs already behave as read-mostly traversal surfaces, while the rest of the file is tightly coupled to dormant construction flow, ambient mutable state, and incomplete `Model` integration. If we mix constructor or activation work into this batch, we lose the ability to distinguish contract cleanup from feature resurrection.

**Quality Gates:**
1. `dotnet build .\src\Sage\Sage.csproj --no-restore`
2. `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-build`

**Sign-off rule:** If implementation stays confined to typed read-only child traversal surfaces and does not enable the dormant feature flags, Ripley is satisfied. Any attempt to turn on dynamic construction, reshape `CreationContext`, or redesign construction/persistence flow belongs in a later batch.

---

**Archived history:** Detailed entries from March 2026–April 26, 2026 preserved in `ripley-history-archive.md`.
