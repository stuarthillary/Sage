# Parker Decision: Nullable Phase 5 (ItemBased) Complete

**Date:** 2026-03-07  
**Agent:** Parker (. NET Developer)  
**Status:** ✅ Complete

## Summary

Successfully completed nullable reference type migration for the entire ItemBased module (73 files). All files now have `#nullable enable` and all nullable warnings resolved. Build clean, all 319 tests passing.

## Scope

- **Target:** `Sage/ItemBased/` — all 73 .cs files
- **Modules affected:**
  - Connectors (8 files) — BasicNonBufferedConnector, ConnectorFactory, FixedRateChannel, Nexus
  - Ports (37 files) — GenericPort, SimpleInputPort, SimpleOutputPort, InputPortManager, OutputPortManager, port interfaces
  - Queues (6 files) — Queue, IQueue, MultiQueueHead, selection strategies, data collectors
  - Servers (7 files) — SimpleServer, BufferedServer, MultiChannelDelayServer, ResourceServer, ServerPlus
  - SourcesAndSinks (2 files) — ItemSource, ItemSink
  - SplittersAndJoiners (9 files) — Splitter, Joiner, branch blocks
  - Tags (4 files) — Tag, TagList, TagType, TagComparers

## Key Changes

### 1. Nullable Annotations
- All `#nullable disable` directives removed
- `IPort?`, `IConnector?`, `IModel?` annotations propagated throughout
- Event delegates made nullable: `event EventHandler? PortDataPresented;`
- Optional parameters: `string? name`, `object? userData`

### 2. Queue.cs Naming Collision
- **Issue:** Class named `Queue` in `Sage.ItemBased.Queues` namespace collides with `System.Collections.Generic.Queue<T>`
- **Resolution:** All references to generic `Queue<T>` within Queue.cs fully-qualified as `System.Collections.Generic.Queue<T>`
- **Class name:** Unchanged (per requirement — no public API breaks)

### 3. Critical Bug Fix — Connectors.cs
- **Original code:** `Debug.Assert(p1.Model == p2.Model); return ForModel(p1.Model)._Connect(...);`
- **Incorrect nullable change:** Added `throw new ApplicationException("port with no model")` for null models
- **Impact:** Broke 9 tests that use ports without models (ManagementFacadeTester.*)
- **Fix:** Restored original `Debug.Assert` behavior, used null-forgiving operator (`p1.Model!`) where needed
- **Rationale:** Tests intentionally create ports without models; runtime check was too strict

### 4. Null-Forgiving Operators
- Used sparingly with inline comments:
  - `ForModel(p1.Model!)._Connect(...)` — Model checked by Debug.Assert
  - `p1.Model!` in constructors — Model already validated upstream

## Test Results

- **Build:** `dotnet build Sage4.csproj` — clean, 0 errors, baseline warnings only
- **Tests:** `dotnet test SageTestLib` — 319/319 passing (100%)
- **Duration:** ~40 seconds

## Progress

- **Before Phase 5:** 370 files with `#nullable disable` (178 enabled)
- **After Phase 5:** 297 files with `#nullable disable` (251 enabled)
- **Change:** +73 files enabled

## Files Modified (73 total)

All files in `Sage/ItemBased/`:
- Connectors: BasicNonBufferedConnector, ConnectorType, Connectors, FixedRateChannel, IChannel, IConnector, IRoute, Nexus
- Ports: GeneralPortChannelInfo, GenericPort, IAddsTagsToServiceObjects, IChangesTagsOnServiceObjects, IInputPort, IOutputPort, IPeriodicity, IPort, IPortChannelInfo, IPortEvents, IPortOwner, IPortSelector, IPortSet, IPulseSource, IReadOnlyTag, IServiceItem, ITag, ITagHolder, ITagType, InputPortManager, InputPortProxy, OutputPortManager, OutputPortProxy, Periodicity, PortDirection, PortManagementFacade, PortManager, PortOwnerProxy, PortSet, PulseSource, SimpleInputPort, SimpleOutputPort, SimplePortActivityLogger, SimplePortOwner
- Queues: IQueue, ISelectionStrategy, MultiQueueHead, OldestShortestQueueStrategy, Queue, ShortestQueueStrategy, WaitingTime
- Servers: BufferedServer, IServer, IServiceObject, MultiChannelDelayServer, ResourceServer, ServerPlus, SimpleServer
- SourcesAndSinks: ItemSink, ItemSource
- SplittersAndJoiners: IJoiner, ISplitter, Joiner, PushJoiner, SimpleBranchBlock, SimpleDelegatedTwoChoiceBranchBlock, SimpleStochasticTwoChoiceBranchBlock, SimpleTwoChoiceBranchBlock, SimultaneousPushSplitter, Splitter
- Tags: Tag, TagComparers, TagList, TagType, Ticker

## Learnings

1. **Debug.Assert vs. Exceptions:** Debug assertions allow tests to run with nullable models; exceptions enforce stricter contracts. Preserve original behavior unless explicitly changing API contracts.

2. **Naming Collisions:** When a class name collides with a BCL generic type, fully-qualify the generic type rather than renaming the class (public API constraint).

3. **Test-Driven Nullable:** Always run tests after nullable changes — they reveal runtime assumptions about nullable behavior.

4. **Null-Forgiving Justification:** Every `!` operator should have an inline comment explaining why null is impossible at that point.

## Next Steps

- **Phase 6 candidates:** Materials, Resources, Utility (if not already done), Graphs (larger module)
- **Remaining:** 297 files across ~10 modules
- **Estimated completion:** 3-4 more phases

## Commit

```
feat(nullable): Phase 5 — ItemBased module

Fix nullable warnings in Sage/ItemBased/.
Queue.cs naming collision handled with fully-qualified generic Queue<T>.

73 ItemBased files now #nullable enable, 251 total enabled, 297 remaining.
All 319 tests pass. 0 build errors.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

---
**Reviewed by:** —  
**Merged to decisions.md:** ❌ Pending review
