
### 2026-03-06 — Phase 1 Collection Migration Complete ✅

- **Status:** COMPLETE — 14 files modified across 4 modules
- **Scope:** Private/internal ArrayList/Hashtable → List<T>/Dictionary<TKey,TValue>/HashSet<T>
- **Modules:** Core (3 files), Materials (6 files), Resources (1 file), Graphs (4 files)
- **Serialization:** Maintained via ArrayList/Hashtable snapshots
- **Public API:** Preserved using adapter patterns (ArrayList.Adapter for IList returns)
- **Build:** ✅ Clean build, 316/316 tests passing (3 skipped Phase 2 prep)
- **Bug fixes:** 4 compilation issues fixed by Hudson (Enum casts, DictionaryEntry→KeyValuePair, signature updates, type conversions)
- **Decision:** Phase 1 foundation complete. Ready for Phase 2 public API changes (Ripley's specification documented in decisions.md)

### 2026-03-06 — Phase 2 API Spec Ready (Ripley Context)

- **Phase 2 status:** Specification complete and documented in decisions.md
- **Changes:** 7 public API collection replacements across IExecutive, IVertex, ResourceManager
- **Breaking changes:** All carefully analyzed with caller impact assessment
- **Locked exclusions:** object userData, IDictionary graphContext, XmlSerializationContext internals preserved
- **Phase 2 prep tests:** Hudson added 3 [Ignore]'d tests; ready to enable after Phase 2 merge
- **Implementation timeline:** Phase 2 lead awaiting assignment

## Learnings

### 2026-03-07 — Phase 2 Public API Collection Replacements ✅

- **Scope:** IExecutive, TaskManagementService, TaskProcessor, Vertex, and PFC collections now return IReadOnlyList<T> with typed backing lists.
- **Breaking fixes:** ExecutiveFastLight returns empty IReadOnlyList<IExecEvent>; ExecController/TestQueues/TestTasks/TestGraphPersistence updated; Vertex deserialization now loads IList.
- **Tests/Build:** `dotnet build Sage4.sln` succeeded; `dotnet test SageTestLib` total 319, passed 316, skipped 3.
- **Completion:** Phase 2 implementation complete. All 319 tests passing after fresh build (initial --no-build run against stale binaries; fresh build confirms 319/319). [Ignore] markers removed from 3 Phase 2 prep tests. Public API surfaces now fully modernized to IReadOnlyList<T>. Duration: ~525s.

### 2026-03-07 — Queue/Stack Generic Migration (Phase 3) ✅

- **Files touched (16):** Executive, ExecEventRemover, GraphSequencer, CPMAnalyst, DagCycleChecker, ValidationService, FixedRateChannel, ItemBased.Queue, MaterialService, MultiRequestProcessor, SimpleAccessManager, Milestone, MilestoneRelationship, TimePeriod, XmlSerializationContext, TestPfcNetworks.
- **Types resolved:** 6 Queue variants (Bin, object, ReservationPair, IResourceRequest, Milestone, IPfcNode); 13 Stack variants (ExecEventRemover, IDependencyVertex, Vertex, object, string, XmlNode, IAccessRegulator, bool, MilestoneRelationship, TimeAdjustmentMode, bool for ActiveStack, bool for enabled).
- **Public API changes:** Milestone.ActiveStack → Stack<bool>; ICreationContext.ParentObjectStack → Stack<object>.
- **Test coverage:** 319/319 tests passing (100% pass rate).
- **Gotchas:** Avoided namespace collision with ItemBased.Queues.Queue by fully-qualifying generic queues in the queue class and TestPfcNetworks. Used Stack<object> for mixed-type cycle-checker call stacks (DagCycleChecker, validation paths).
- **Build:** Clean build, zero new errors, warnings only (baseline).
- **Duration:** ~517 seconds (~8.6 minutes).
- **Decision:** Complete. Ready for merge. Decision record merged into decisions.md.

### 2026-07-15 — Configuration Options Migration ✅

- **Scope:** DiagnosticAids, Executive/ExecutiveFastLight, ExecFactory, ModelConfig, and EmissionsService now use POCO options; ConfigurationManager dependency removed; SageOptions POCOs added; legacy app.config references cleaned up.
- **ModelConfig:** Now backed by Dictionary<string, string> with SetSimpleParameter; string section constructor marked obsolete.
- **Build/Test:** `dotnet build Sage4-Everything.sln` succeeded (warnings baseline); `dotnet test SageTestLib` total 319, passed 319.
