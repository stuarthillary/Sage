## Core Context

**Current Status (April 2026):** 351/351 passing tests. Phase 3 graph API opening scope gate approved; Phase 1 collections recovery complete.

**Key Project Learnings:**
- ExecFactory singleton captures options at creation time; requires reflection reset if Configure() called post-creation
- Static field `_ignoreCausalityViolations` created cross-test contamination — parallel tests must lock/reset
- Daemon events do NOT fire when only daemons remain; loop condition `_numEventsInQueue > _numDaemonEventsInQueue`
- EFL causality enforcement is non-functional (logs only, throw is commented out)
- `PortSet` GUID-backed `Hashtable` is part of XML persistence contract; key semantics ambiguous in docs vs. implementation
- Graph interfaces are tightly coupled to concrete `Edge`/`Vertex` types; no abstract seam (`IGraph`) exists yet

## Learnings

### 2026-04-28 — Phase 3 Resource Manager Review (`p3-resource-manager`) ✅

**Status:** Complete

**Mandate:** Review Parker's resource-manager typed read-only batch, add regression coverage only where the new public seam was still unguarded, and decide whether the batch is shippable.

**Learning:**
- The implementation change itself was clean, but the regression net was too soft. Fixing `TestResources` compile fallout was not enough; this batch needed explicit tripwires on the public `Resources` / `GetResourceManagers()` signatures so a later backslide to legacy `IList` / `ICollection` shapes fails fast.
- `SelfManagingResource.Resources` still exposes the wrapped base resource rather than the wrapper instance itself. That is existing behavior and should stay out of this batch's scope; coverage should only lock read-only typed shape plus stable membership semantics.

**Coverage added (Review):**
- Added `TestResourceManagerApiCollectionsAreTypedAndReadOnly` to assert `IResourceManager`, `ResourceManager`, `SelfManagingResource`, `MaterialResourceItem`, `IResourceManagerCollection`, and `ResourceManagerCollection` now expose typed read-only collection signatures and read-only runtime collections

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet build .\src\Sage.Materials\Sage.Materials.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore --filter "FullyQualifiedName~Highpoint.Sage.Resources.ResourceTester|FullyQualifiedName~Highpoint.Sage.Resources.ResourceTesterExt|FullyQualifiedName~Highpoint.Sage.ItemBased.Blocks.ServerTester"` ✅ (20/20 passing)

**Verdict:** Approve after adding the missing API-shape regression lock. Parker stayed inside the scoped public-surface break, and the resource/server slice is green with the new guardrail in place.

### 2026-04-28 — Phase 3 Reactions Gate (`p3-reactions`) - Map & Review ✅

**Status:** Complete

**Mandate (Map Pass):** Map the compile and test fallout surface for the reactions batch scope gate. Identify hotspots and verify pre-change gates.

**Mandate (Review Pass):** Review Parker's reaction collection API batch, add regression coverage only where the new public seam was still unguarded, and decide whether the batch is shippable.

**Learning:** 
- **Map:** Direct compile fallout was limited to `Reaction.cs`, `ReactionProcessor.cs`, and `ReactionInstance.cs`; test fallout centered on `TestChemistry.cs`. Pre-change gates were green.
- **Review:** The changed seam was real, but the tests were too soft. Simply compiling `CombineMaterials(...)` against typed locals was not enough; this batch needed assertions that the public `Reaction` and `ReactionProcessor` surfaces are explicitly typed/read-only, and that the observed reaction outputs stay aligned with the emitted `ReactionInstance` objects.

**Coverage added (Review):**
- Strengthened `TestRP_CombineAPI` to assert observed reaction/result behavior for both reacting and non-reacting inputs
- Added `TestReactionApiCollectionsAreTypedAndReadOnly` to lock `Reaction.Reactants` / `Products`, `ReactionProcessor.Reactions`, and `GetReactionsBy*` to typed read-only shapes

**Validation:**
- `dotnet build .\src\Sage.Materials\Sage.Materials.csproj --no-restore` ✅
- `dotnet test .\tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj --no-restore --no-build` ✅ (22/22 passing)
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore --filter "FullyQualifiedName~Highpoint.Sage.Materials.Chemistry.Chemistry101"` ✅ (4/4 passing)

**Verdict:** Approve after adding the missing regression net. Parker's implementation stays inside the scoped typed collection/query break, and the new tests now make that contract loud enough to catch backsliding.

---

### 2026-04-28 — Phase 3 Graph API Opening — Regression / Compile Fallout Map ✅

**Status:** Complete

**Mandate:** Map the regression and compile fallout surface for Phase 3 opening batch (`IEdge`, `IVertex`, `Edge`, `Vertex`).

**Learning:** The current graph interfaces are still tightly coupled to concrete `Edge`/`Vertex` types. There is no cheap adapter layer or abstract seam (`IGraph` doesn't exist). The first interface break will ripple immediately through production code (12 files) and tests (7 files).

**Hotspots identified:**
- **Production (12):** `ChannelMonitor`, `MultiChannelEdgeReceiptManager`, `Ligature`, `PathLength`, `VertexSynchronizer`, `DagCycleChecker`, `DagDeadlockChecker`, `DagStructureError`, `CPMAnalyst`, `PertAnalyst`, `DiagnosticAids`, `Task`
- **Tests (7):** `TestDAGCycleChecker`, `TestGraphBranching`, `TestGraphAlgorithmRegressions`, `TestGraphValidities`, `TestGraphPersistence`, `TestTasks`, `TestTasks2`

**Why it matters:** Knowing the exact surface lets Parker plan fixes in scope and Hudson plan regression coverage. The lack of an abstract seam means Batch 1 (interface only) will immediately require fixing consumers; we can't defer consumer updates to a later batch without accumulating blocking errors.

**Decision:** Do NOT add characterization tests now. The interface contracts are not yet stable, so low-level tests would only prevent the intended breaking changes instead of protecting stable behavior.

**Validation:**
- Baseline build: ✅ Clean
- Baseline tests: ✅ Passing

**Documentation:**
- Decision merged to `.squad/decisions.md`
- Orchestration log: `.squad/orchestration-log/2026-04-28T21-53-46Z-hudson.md`
- Session log: `.squad/log/2026-04-28T21-53-46Z-phase3-start.md`

---

### 2026-04-28 — Phase 3 Batch 1 Review (`p3-interfaces`) ✅

**Status:** Complete

**Mandate:** Review Parker's interface-only graph batch, add regression coverage if needed, and decide whether Batch 1 is shippable.

**Learning:** The safe regression seam for this batch is the interface boundary itself. Tests must cast through `IEdge` / `IVertex` to prove the new signatures are usable, while avoiding over-locking the temporary concrete `Edge` / `Vertex` public shapes that Parker intentionally preserved with explicit interface implementations.

**Coverage added:**
- `TestVertexInterfaceEdgesTypedAsReadOnlyList`
- `TestEdgeInterfaceExposesTypedEndpointsAndChildren`

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-build` → 290/290 passing ✅

**Verdict:** Approve Batch 1. Parker stayed inside Ripley's scope gate, and the new regression tests now lock the intended interface behavior without freezing the later `p3-edge-vertex` follow-up work.

---

### 2026-07-17 — Phase 3 Batch 2 Review (`p3-edge-vertex`) ❌

**Status:** Rejected

**Mandate:** Review the concrete `Edge`/`Vertex` API batch, add regression coverage for the public-surface break, and decide whether the batch is shippable.

**Learning:** The concrete batch needs its own API-shape tripwires. Interface tests are not enough once `p3-edge-vertex` starts; we need reflection-based assertions on the declared public property types so the batch fails fast if `Edge`/`Vertex` stay on legacy `Vertex`/`IList` signatures.

**Coverage added:**
- `TestVertexConcreteEdgesAreTypedAsReadOnlyList`
- `TestEdgeConcreteApiMatchesInterfaceSurface`

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --filter "FullyQualifiedName~GraphLoopingTester"` ❌
  - `Vertex.PredecessorEdges` / `SuccessorEdges` are still declared as `IList`
  - `Edge.PreVertex` / `PostVertex` are still declared as `Vertex`

**Verdict:** Reject Batch 2 as currently staged. The new regression tests prove the concrete public API break has not been implemented yet, so this is still Batch 1 behavior with no shippable `p3-edge-vertex` surface.

---

### 2026-07-17 — Phase 3 Batch 2 Review (`p3-edge-vertex`) Revision Pass ✅

**Status:** Complete

**Mandate:** Re-review Ripley's replacement concrete `Edge`/`Vertex` API batch, add regression coverage only if still needed, and decide whether the batch is now shippable.

**Learning:** The concrete public break is now actually in place without reopening the forbidden persistence/storage scope. The existing interface-behavior tests plus the concrete reflection tripwire now give the right coverage balance: they lock the public shape change while leaving `PrincipalEdge`, XML persistence, and `GetParent()` alone.

**Coverage added:** None. Existing regression coverage was sufficient once `Edge.PredecessorEdges` / `SuccessorEdges` joined the concrete API assertions.

**Validation:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore` ✅ (293/293 passing)

**Verdict:** Approve the revised Batch 2. Ripley's replacement revision lands the intended concrete public API break, limits fallout repairs to genuine `Vertex`-only seams, and passes the full graph test project cleanly.

---

### 2026-04-28 — Phase 3 Reactions Opening — Regression / Compile Fallout Map ✅

**Status:** Complete

**Mandate:** Map the regression and compile fallout surface for the opening reactions batch (`p3-reactions`) around `Reaction` and `ReactionProcessor`.

**Learning:** The safe opening seam is the typed collection/query surface Ripley scoped, not the execution engine. In-repo fallout is concentrated in `Reaction.cs`, `ReactionProcessor.cs`, `ReactionInstance.cs`, and the chemistry-oriented tests that still declare `ArrayList` locals for `CombineMaterials(...)` observations.

**Hotspots identified:**
- **Direct production fallout:** `Reaction.cs`, `ReactionProcessor.cs`, `ReactionInstance.cs`
- **Supporting production seams to keep compiling:** `BasicReactionSupporter.cs`, `ISupportsReactions.cs`, `MassVolumeTracker.cs`, `Mixture.cs`
- **Test fallout:** `TestChemistry.cs` (definite `ArrayList observedReactions` / `observedReactionInstances` compile break), plus reaction coverage in `TestMaterials.cs`, `TestTransferSpecScaling.cs`, `TestTemperatureController.cs`, `TestXmlSerialization.cs`, and `TestCyclicalMVTTracker.cs`
- **Not currently exercised in-repo:** `ReactionProcessor.Reactions`, `GetReaction(...)`, `GetReactionsBy*`, `RemoveReaction(...)`, `ReactionAddedEvent`, `ReactionRemovedEvent`

**Decision:** Do **not** add new characterization tests in this mapping pass. The untested processor query/event members are exactly the legacy collection surface Ripley intends to retarget to typed read-only collections; locking them now would freeze the wrong public shape instead of protecting stable simulation behavior.

**Quality gates for Batch 1:**
- `dotnet build .\src\Sage.Materials\Sage.Materials.csproj --no-restore` ✅
- `dotnet test .\tests\Sage.Materials.Tests\Sage.Materials.Tests.csproj --no-restore` ✅ (22/22 passing)
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore --filter "FullyQualifiedName~Highpoint.Sage.Materials.Chemistry.Chemistry101|FullyQualifiedName~Highpoint.Sage.Persistence.PersistenceTester|FullyQualifiedName~Highpoint.Sage.Materials.Chemistry.TransferSpecTester101|FullyQualifiedName~Highpoint.Sage.Thermodynamics.TemperatureControllerTester101|FullyQualifiedName~Highpoint.Sage.Tests.Scheduling.MVTTrackerTester"` ✅ (26/26 passing)

**Verdict:** Batch 1 can move if it stays inside the typed return-surface changes and fixes the `TestChemistry` `ArrayList` fallout without touching reaction math, event order, or XML field names.

---

### 2026-04-28 — Phase 3 Resource Manager Opening — Regression / Compile Fallout Map ✅

**Status:** Complete

**Mandate:** Map the regression and compile fallout surface for the opening resource-manager batch (`p3-resource-manager`) around `ResourceManager`, `IResourceManager`, `IResourceManagerCollection`, and nearby public resource APIs.

**Learning:** The first break is not centered on acquisition math; it is centered on the legacy collection/event seam. In-repo production fallout is concentrated in the `IResourceManager` consumers that enumerate `Resources` or subscribe to manager events, while concrete `ResourceManager`-only members (`Add`, `Remove`, `Clear`, indexer, `IEnumerable`) are mostly exercised by tests and samples.

**Hotspots identified:**
- **Direct production fallout:** `MaterialConduitManager.cs` (`Resources`, `ResourceRequested`, `ResourceAdded`, `ResourceRemoved`), `ResourceServer.cs` (`ResourceReleased`), `MaterialService.cs` (`Reserve`, `Unreserve`, `Acquire`)
- **Supporting resource implementations that must stay aligned:** `SelfManagingResource.cs`, `MaterialResourceItem.cs`, `ResourceRequest.cs`, `MaterialResourceRequest.cs`, `ResourceManagerCollection.cs`
- **Concrete `ResourceManager` compile fallout:** `TestResources.cs`, `TestServers.cs`, and `samples\Sage_SampleCode\6_Resources.cs` (subclassing plus `Resources` enumeration/casts)
- **Not currently exercised in-repo:** `IResourceManagerCollection.GetResourceManagers()`, `ResourceManagerAdded`, and `ResourceManagerRemoved`

**Coverage added:**
- `TestResourceManagerCollectionLifecycleAndLookup`

**Decision:** Do **not** add API-shape characterization tests for `IResourceManager.Resources` or `IResourceManagerCollection.GetResourceManagers()` in this mapping pass. Those are exactly the legacy collection seams most likely to change, so type-locking them now would freeze the wrong public surface. The one safe addition was a behavioral test for `ResourceManagerCollection` lookup/event semantics, because it protects stable behavior without asserting the collection type.

**Quality gates for Batch 1:**
- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-restore --filter "FullyQualifiedName~Highpoint.Sage.Resources.ResourceTester|FullyQualifiedName~Highpoint.Sage.Resources.ResourceTesterExt|FullyQualifiedName~Highpoint.Sage.ItemBased.Blocks.ServerTester"` ✅ (19/19 passing)

**Verdict:** Batch 1 can move if it stays on collection/event surface modernization, updates the `IResourceManager` consumers listed above, and leaves acquisition ordering/selection semantics alone. No merge without keeping the resource/server regression slice green.

---

**Archived history:** Detailed entries from March 2026–April 26, 2026 preserved in `hudson-history-archive.md`.
