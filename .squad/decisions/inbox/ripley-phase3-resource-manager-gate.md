# Phase 3 Resource Manager Scope Gate (`p3-resource-manager`)

**By:** Ripley

**Date:** 2026-04-28

**Status:** Active — first slice scoped, no implementation authorized beyond this boundary

## Decision

The first breaking slice for `p3-resource-manager` is **typed read-only collection exposure only**. We are opening the public surface where the implementation is already read-only today, and we are explicitly not mixing that with request-flow, waiter, persistence, or construction redesign.

## Exact Breaking API Changes

1. `IResourceManager.Resources`
   - Current: `IList`
   - New: `IReadOnlyList<IResource>`

2. `ResourceManager.Resources`
   - Current: `IList`
   - New: `IReadOnlyList<IResource>`

3. `SelfManagingResource.Resources`
   - Current: `IList`
   - New: `IReadOnlyList<IResource>`

4. `MaterialResourceItem.Resources`
   - Current: `IList`
   - New: `IReadOnlyList<IResource>`

5. `IResourceManagerCollection.GetResourceManagers()`
   - Current: `ICollection`
   - New: `IReadOnlyCollection<IResourceManager>`

6. `ResourceManagerCollection.GetResourceManagers()`
   - Current: `ICollection`
   - New: `IReadOnlyCollection<IResourceManager>`

## Why this is the first slice

This is the narrowest real break with architectural payoff:

- `ResourceManager` already stores resources in `List<IResource>` and already publishes a read-only wrapper
- `SelfManagingResource` and `MaterialResourceItem` are tightly coupled implementers of the same `IResourceManager` boundary, so they must move in the same batch
- in-repo consumers are light and local: tests use `Count`, `Contains`, `Empty`, `Single`, and `foreach`; `MaterialConduitManager` only enumerates
- `ResourceManagerCollection.GetResourceManagers()` has no visible in-repo consumers, so pulling it into the same batch finishes the manager-collection read surface at minimal risk

This is a contract cleanup, not a behavior change. That matters because resource acquisition and release sit on simulation control flow; we do not spend determinism budget unless the public value is worth it.

## Explicit Boundaries — do not change in this slice

- `Reserve`, `Acquire`, `Unreserve`, `Release`, or waiter ordering behavior
- prioritized request semantics in `RscWaiterList`
- `Add`, `Remove`, or `Clear` mutator methods
- `ResourceRequested`, `ResourceAcquired`, `ResourceReleased`, `ResourceAdded`, `ResourceRemoved` delegate shapes
- `ResourceManager` indexer `this[Guid guid]`
- `ResourceManager : IEnumerable` non-generic enumeration shape
- internal storage fields/backing types beyond what is needed to adapt to the new return types
- `AccessRegulator` shape or semantics

## Direct Fallout Expected

- `MaterialConduitManager` must continue to enumerate `IResourceManager.Resources` without assuming `IList`
- resource tests that currently bind to `rm.Resources` must continue to validate `Count`, membership, and enumeration semantics through the new read-only typed shape
- all `IResourceManager` implementers in repo must compile against the new interface

## Deferred to `p3-dynamic-construction`

The following must stay out:

- constructor redesign for `ResourceManager`, `SelfManagingResource`, and `MaterialResourceItem`
- removal or reshaping of serializer-facing parameterless constructors
- `InitializeIdentity` contract changes
- model-registration sequencing changes (`ModelObjects.Add` / `Remove`)
- XML reconstitution redesign
- serialization payload/key changes, including the persisted `"Resources"` collection shape

Those are construction and persistence concerns. Mixing them with the collection-surface break would destroy isolation and make it impossible to attribute fallout cleanly.

## Deferred to `p3-version-bump`

- SemVer major release coordination for the `IList` / `ICollection` to `IReadOnly*` breaks
- downstream consumer migration guidance and release notes
- package/release packaging decisions
- any cross-batch cleanup that should happen only once all Phase 3 public breaks are known

We can stage the source changes now, but the public release story belongs in the version-bump batch. I will not let package semantics get entangled with scope control.

## Validation Baseline

- `dotnet build .\src\Sage\Sage.csproj --no-restore` ✅
- `dotnet test .\tests\SageTestLib\Sage.Tests.csproj --no-build --filter "FullyQualifiedName~TestResources|FullyQualifiedName~TestServers"` ✅

## Authorization

- Implementation owner may proceed on this slice only
- Hudson should lock regression coverage on the typed read-only boundary before any broader resource-manager work begins
- Any attempt to fold in persistence, constructor, waiter, or event redesign is out of scope and should be rejected
