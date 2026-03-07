
### 2026-07-16 — Rename namespace Highpoint.Sage.SimCore → Highpoint.Sage.Core ✅

- **Scope:** 268 files changed across src, tests, benchmarks, and samples
- **Replacements:** `Highpoint.Sage.SimCore` (full prefix) replaced in 259 .cs files via bulk replace
- **Partial qualifiers:** 9 additional files used bare `SimCore.X` references — fixed to `Core.X` or dropped prefix where `using Highpoint.Sage.Core;` was already present (IEdge.cs, IProcedureFunctionChart.cs)
- **Ambiguity fix:** `IProcedureFunctionChart` used `ICloneable` which became ambiguous between `Highpoint.Sage.Core.ICloneable` and `System.ICloneable`; qualified as `Core.ICloneable`
- **Config:** `tests\TestDriver\app.config` `ExecutiveType` value updated from `Highpoint.Sage.SimCore.Executive` → `Highpoint.Sage.Core.Executive`
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing
- **Decision:** Namespace for Core module is `Highpoint.Sage.Core` (not `SimCore`)



- **Project file:** `src\Sage\Sage4.csproj` → `src\Sage\Sage.csproj` (via `git mv`, history preserved)
- **RootNamespace:** `Highpoint.Sage` added to Sage.csproj `<PropertyGroup>`
- **AssemblyName:** Updated to `Highpoint.Sage` (and `SageOptions.cs` default type string + `TestExecutive.cs` hardcoded type string updated accordingly)
- **Sage.slnx:** Updated project entry from `Sage4.csproj` → `Sage.csproj`
- **ProjectReferences updated:** SageTestLib, TestDriver, SageBenchmarks, Sage_SampleCode
- **Namespace migration:** All namespaces now prefixed with `Highpoint.Sage.*` (e.g., `Highpoint.Sage.SimCore`)
- **Assembly name:** Output DLL is now `Highpoint.Sage.dll`
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing

### 2026-07-16 — Solution Restructure to src/tests/benchmarks/samples Layout ✅

- **Scope:** Entire repository layout restructured; .sln replaced with .slnx.
- **New layout:**
  - `src\Sage\` ← main library (was `Sage\`)
  - `tests\SageTestLib\` ← unit tests (was `Sage_Aux\SageTestLib\`)
  - `tests\TestDriver\` ← test runner (was `Sage_Aux\SageTesting\`)
  - `benchmarks\SageBenchmarks\` ← benchmarks (was `Sage_Aux\SageBenchmarks\`)
  - `samples\Sage_SampleCode\` ← samples (was `Sage_SampleCode\`)
- **ProjectReference updates:** All four consumer projects updated; key deltas:
  - `benchmarks\SageBenchmarks` → `..\..\src\Sage\Sage4.csproj`
  - `tests\SageTestLib` → `..\..\src\Sage\Sage4.csproj`
  - `tests\TestDriver` → `..\..\src\Sage\Sage4.csproj` (SageTestLib ref unchanged: `..\SageTestLib\...`)
  - `samples\Sage_SampleCode` → `..\..\src\Sage\Sage4.csproj`
- **slnx approach:** `dotnet sln migrate` (SDK 10.0.103) generated `Sage4-Everything.slnx` with old paths; paths updated manually and saved as `Sage.slnx`. Old `.sln` removed.
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing.
- **git mv:** All moves used `git mv` to preserve history.

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
- **Tests/Build:** `dotnet build Sage.slnx` succeeded; `dotnet test SageTestLib` total 319, passed 316, skipped 3.
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

### 2026-07-16 — Nullable Phase 1 Scaffold ✅
- **Scope:** Enabled nullable globally, added #nullable disable to Sage sources, and annotated Core interfaces plus ExecEvent/SageOptions/DetachableEvent.
- **Build/Test:** `dotnet build Sage4-Everything.sln` succeeded (warnings baseline); `dotnet test SageTestLib` total 319, passed 319.
- **Notes:** 23 files now #nullable enable; 525 files still #nullable disable.

### 2026-07-16 — Nullable Phase 2 Engine Internals ✅

- **Scope:** Executive, ExecutiveFastLight, ExecFactory, ModelConfig, Model, and ExecEventRemover now rely on global nullable with warnings fixed (object? userData, nullable events/fields).
- **Special handling:** ExecutiveFastLight heap internals preserved; nullable annotations and null-forgiving applied without altering queue logic.
- **Build/Test:** `dotnet build Sage4-Everything.sln --no-incremental` succeeded; `dotnet test SageTestLib` total 319, passed 319.
- **Notes:** 23 files now #nullable enable; 519 files still #nullable disable.

### 2026-03-07 — Nullable Phase 4 (Dependencies/Randoms/SystemDynamics) ✅

- **Scope:** Removed `#nullable disable` from Dependencies, Randoms, and SystemDynamics (incl. Design/Utility).
- **Nullability fixes:** GraphSequencer/GraphCycleException annotations, Randoms buffering fields, StateBase Configure fields/collections and distro cache, RunProgram optional parameters.
- **Build/Test:** `dotnet build Sage\Sage4.csproj` clean; `dotnet build Sage4-Everything.sln` blocked by permission prompt; `dotnet test SageTestLib` 319/319.
- **Remaining:** 370 files still `#nullable disable` (178 enabled total).

### 2026-03-07 — Nullable Phase 5 (ItemBased) ✅

- **Scope:** Removed `#nullable disable` from all 73 files in Sage/ItemBased/ (Connectors, Ports, Queues, Servers, SourcesAndSinks, SplittersAndJoiners).
- **Nullability fixes:** IPort nullable annotations, ConnectorFactory nullable handling, Queue.cs naming collision resolved with fully-qualified System.Collections.Generic.Queue<T>, event delegates made nullable, IModel? propagated throughout.
- **Critical fix:** Corrected Connectors.cs to preserve original Debug.Assert behavior for null models instead of throwing exceptions (test compatibility).
- **Build/Test:** `dotnet build Sage4.csproj` clean; `dotnet test SageTestLib` 319/319 passing.
- **Remaining:** 297 files still `#nullable disable` (251 enabled total).

### 2026-03-07 — Nullable Phase 6 (Materials) ✅

- **Scope:** Removed `#nullable disable` from all 66 files in Sage/Materials/ (base + Chemistry, Emissions, Thermodynamics, VaporPressure subdirectories).
- **Nullability fixes:** MaterialType IModel/EmissionsClassifications nullable, Substance event delegates, MassVolumeTracker ReactionProcessor nullable, MaterialChangeListener event handlers, IMemento.Parent nullability, IComparer nullability signatures.
- **Notable files:** MaterialType.cs (IModel/IDictionary nullable, InitializeIdentity signature), Substance.cs (large file, event delegates, comparers, memento), Mixture.cs (complex state management), emission models (Hashtable parameters, out parameters).
- **Patterns applied:** Event delegates nullable (`event MaterialChangeListener?`), IModel fields nullable for deserialization, `null!` for deferred-init fields with comments, nullable cast patterns (`as Type?`), unboxing with `!` for DictionaryEntry values.
- **Build/Test:** `dotnet build Sage4.csproj` clean; `dotnet test SageTestLib` 319/319 passing.
- **Remaining:** 232 files still `#nullable disable` (316 enabled total, 548 total in Sage/).
- **Phase 6 completion:** All Materials module files migrated successfully. Ready for Phase 7 (next target TBD).

### 2026-03-07 — Nullable Phase 7 (Graphs) ✅

- **Scope:** Removed `#nullable disable` from all 96 files in Sage/Graphs/ (base + PFC, PFC/Execution, PFC/Execution/Actions, Tasks subdirectories). This was the LARGEST module with ~1,160 warnings.
- **Architectural constraints respected:** `IDictionary graphContext` kept non-generic and non-nullable throughout (architectural design - never null in methods). `object userData` → `object?` but kept non-generic.
- **Core files:** Edge.cs (1,453 lines), Vertex.cs (482 lines), Task.cs (633 lines) - fundamental graph structure components.
- **Major PFC files:** ProcedureFunctionChart.cs (2,946 lines - largest file in Graphs), PfcValidator.cs (1,087 lines), PfcAnalyst.cs (1,003 lines), StepStateMachine.cs (608 lines), PfcNode.cs (440 lines), plus 30+ supporting PFC files.
- **Analysis files:** CPMAnalyst.cs (730 lines - Critical Path Method), ValidationService.cs (656 lines), CriticalPathAnalyst, PertAnalyst, DagCycleChecker, DagDeadlockChecker.
- **Nullability patterns applied:**
  - Event delegates all nullable (`event VertexEvent?`, `event PfcAction?`, etc.)
  - `Vertex? PreVertex`, `Vertex? PostVertex` on IEdge (edges can have null vertices)
  - `IEdge? GetParent()`, `IHasValidity? GetParent()` (nullable return types where documented)
  - `null!` for deferred-init fields in deserialization scenarios with comments
  - `!` null-forgiving operator used sparingly with explanatory comments (e.g., `_vm!.Suspend() // hasVm guarantees non-null`)
  - Dictionary lookups and `as` casts → nullable types throughout
  - PFC expression system: `Expression?`, `ExecutableCondition?`, `ParticipantDirectory?` nullable where appropriate
  - State machines: nullable references for runtime-assigned fields in StepStateMachine/TransitionStateMachine
- **Systematic approach:** Fixed files in priority order: interfaces/enums first, then core structure (Vertex/Edge), then implementations (Task, PFC nodes), then large analysis files, then execution subsystems.
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing (0 failures).
- **Remaining:** 136 files still `#nullable disable` (412 enabled total, 548 total in Sage/).
- **Phase 7 completion:** All Graphs module files migrated successfully. Largest and most complex module done. Ready for Phase 8.

### 2026-07-16 — Nullable Phase 8 (Persistence + Final Cleanup) ✅ **MIGRATION COMPLETE**

- **Scope:** Removed `#nullable disable` from final 134 files across Core (35), Mathematics (53), Persistence (6), Resources (30), SmartPropertyBag (9), Presentation (1).
- **Core files:** BaseModelObject, DefaultModelStates, DetachableEventSynchronizer, EnumStateMachine, ExceptionHandler, ExecController, IMOHelper, InitializationManager, InitializationException, InitializerArgAttribute, InitializerAttribute, InvalidTransitionHandler, MergedTransitionHandler, MetronomeBase, ModelExceptionError, ModelObjectDictionary, SimpleMetronome, StateMachine, TransitionFailureException, TransitionHandler, plus all enums (ExecEventType, ExecState, ExecType, InitializationType, RefType) and attributes (DefaultValueAttribute, TaskGraphVolatileAttribute, VolatileKey).
- **Mathematics module:** All 53 files migrated — distributions (Binomial, Cauchy, Exponential, Normal, Lognormal, Poisson, Triangular, Uniform, Weibull, etc.), CDFs, histograms (1D for Double/DateTime/TimeSpan), interpolators, scaling adapters, regression, operations, extensions.
- **Persistence module:** DeserializationContext, DynamicConstruction, IDirtyable, ISerializer, IXElementSerializable, IXmlPersistable (6 files). **Permanent disables preserved:** XmlSerializationContext, WeakHashTable (architectural complexity).
- **Resources module:** All 30 files migrated — interfaces (IAccessManager, IAccessRegulator, IResource, IResourceManager, etc.), implementations (Resource, ResourceManager, ResourceTracker, etc.), requests, events, exceptions.
- **SmartPropertyBag module:** All 9 files migrated — HierarchicalDictionaryEntry, SmartPropertyBag, WriteLock, SPBInitializer, IHasWriteLock, ISPBTreeNode, exceptions.
- **Presentation:** Converters.cs migrated.
- **Nullability patterns applied:**
  - Event delegates all nullable (`event EventHandler? Name;`)
  - `object userData` → `object?` throughout (kept non-generic as architectural decision)
  - `IModel?`, `string?` for fields/properties that can be null
  - `= null!` for deferred-init fields with comments
  - `!` null-forgiving operator used sparingly with explanatory comments
  - IComparer implementations: `Compare(object? x, object? y)`
  - Generic model error/warning: nullable `subject` and `innerException`
  - Dictionary lookups and `as` casts → nullable types
- **Build/Test:** `dotnet build Sage.slnx --no-incremental` 0 errors; `dotnet test SageTestLib` 319/319 passing (0 failures).
- **Final count:** 2 files with `#nullable disable` (WeakHashTable.cs, XmlSerializationContext.cs) — both permanent exclusions by design.
- **Coverage:** 546 of 548 files nullable-enabled (99.6% coverage).
- **Phase 8 completion:** ✅ **NULLABLE REFERENCE TYPE MIGRATION COMPLETE.** All planned files migrated successfully. Zero regressions, full test coverage maintained.

### 2026-07-16 — Samples Project Rename to Sample.Examples ✅

- **Scope:** Renamed samples project from `Sage_SampleCode.csproj` to `Sample.Examples.csproj` with namespace refactoring.
- **Project file:** `samples\Sage_SampleCode\Sage_SampleCode.csproj` → `samples\Sage_SampleCode\Sample.Examples.csproj` (via `git mv`, history preserved)
- **RootNamespace/AssemblyName:** Set `RootNamespace = Highpoint.Sage.Examples` and `AssemblyName = Sample.Examples` in project file
- **Sage.slnx:** Updated project reference from `Sage_SampleCode.csproj` → `Sample.Examples.csproj`
- **Namespace migration:** All `Demo.*` namespaces renamed to `Highpoint.Sage.Examples.*` in 9 .cs files (1_Executive, 2_StateManagement, 3_RandomServer, 4_StateMachine, 5_IntroToModel, 6_Resources, 7_SequenceControl, Domain, Program)
- **Code references:** Updated fully-qualified type references in Program.cs from `Demo.Executive.*` → `Highpoint.Sage.Examples.Executive.*` and reflection logic from `Demo.` prefix (5 chars) → `Highpoint.Sage.Examples.` prefix (24 chars)
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing
- **Decision:** Samples project now follows team naming pattern (Sample.Examples) and Highpoint.Sage.Examples namespace convention

### 2026-07-16 — Benchmarks Project Rename to Sage.Benchmarks ✅

- **Scope:** Renamed benchmarks project from `SageBenchmarks.csproj` to `Sage.Benchmarks.csproj` with namespace refactoring.
- **Project file:** `benchmarks\SageBenchmarks\SageBenchmarks.csproj` → `benchmarks\SageBenchmarks\Sage.Benchmarks.csproj` (via `git mv`, history preserved)
- **RootNamespace/AssemblyName:** Set `RootNamespace = Highpoint.Sage.Benchmarks` and `AssemblyName = Sage.Benchmarks` in project file
- **Sage.slnx:** Updated project reference from `SageBenchmarks.csproj` → `Sage.Benchmarks.csproj`
- **Namespace migration:** EventDispatchBenchmarks.cs already had correct `namespace Highpoint.Sage.Benchmarks;` declaration
- **Comment updates:** Updated Program.cs comment from old path `Sage_Aux\SageBenchmarks\SageBenchmarks.csproj` → `benchmarks\SageBenchmarks\Sage.Benchmarks.csproj`
- **Build/Test:** `dotnet build Sage.slnx` 0 errors; `dotnet test SageTestLib` 319/319 passing
- **Decision:** Benchmarks project now follows team naming pattern (Sage.Benchmarks) and Highpoint.Sage.Benchmarks namespace convention


