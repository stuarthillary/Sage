# Hudson Test / QA — Archived History

Detailed notes from March 2026 through April 26, 2026.

## Phase 1: Core Executive Test Coverage (March 6–10, 2026)

Added 16 tests for collection migration safety plus 21 comprehensive tests across Priority 1/2/3 buckets covering causality violations, pause/resume mechanics, determinism, FIFO ordering, daemon event behavior, and static field contamination.

**Key learnings:**
- ExecFactory singleton captures options at creation time; requires reflection reset if Configure() called post-creation
- Static field `_ignoreCausalityViolations` created cross-test contamination — parallel tests must lock/reset via ExecFactory.Configure() + ResetExecFactorySingleton()
- Daemon events do NOT fire when only daemons remain; loop condition `_numEventsInQueue > _numDaemonEventsInQueue`
- Pause/Resume API names (not Suspend); sync handler pause needs `Thread.Sleep(50)` for PauseManager catchup
- FIFO tiebreaker via `_nextReqHashCode`; determinism verified across 1000-event runs
- Executive drops past events (long.MinValue) when IgnoreCausalityViolations=true; EFL clamps to Now and fires
- EFL causality throw is commented out — "enforce mode" only logs to Console, doesn't actually enforce

## Phase 2: Graph Algorithm and Wrapper Regression Coverage (April 26, 2026)

Added 14 regression tests across graph algorithms (4 tests for CPMAnalyst, PertAnalyst, DagDeadlockChecker) and utility wrappers (10 tests for HashtableOfLists, WeakHashtable, WeakList).

**Graph algorithm coverage locked in:**
- `PertAnalyst.CriticalPath` remains read-only `ArrayList`
- `DagDeadlockChecker.Errors` stays read-only/non-generic at boundary
- Duplicate successor references no longer create duplicate frontier/error targets

**Wrapper coverage locked in:**
- `HashtableOfLists` non-generic de-duplicates and prunes empty keys
- `HashtableOfLists<TK,TV>` preserves duplicates and handles per-key sorting
- `WeakHashtable` removes dead entries on indexer/Values access
- `WeakList` is target-based for Contains/IndexOf/Remove, honors CopyTo offset

## Phase 2: PortSet/Resources Regression Coverage (April 26, 2026)

Added 5 regression tests for PortSet/ResourceManager modernization batch. Tests locked in behavior for add/remove/clear, Guid/name lookups, duplicate handling, and event firing without resolving ambiguous contracts (case-sensitivity, ordering, event wiring).

**Tests added:**
1. `PortSet_AddRemoveAndClear_UpdateLookupsAndTypedViews`
2. `PortSet_DuplicateInstanceIsIgnored_ButDuplicateNameThrows`
3. `PortSet_PortAddedAndRemovedEventsFireOncePerMutation`
4. `TestResourceManagerManagerLinksAndLifecycleEvents`
5. `TestMultiKeyAccessRegulatorMatchesOnKeyAndSymmetricSubjectEquality`

## Phase 1 Recovery: Collections Scope Reset (July 17, 2026)

Classified 30+ files into Phase 1-safe (signature-preserving internals) vs. Phase 2+ (graph algorithms, PFC, Materials, PortSet, WeakHashTable). Reverted Phase 2 creep from collections migration branch and restored baseline to 351/351 passing.

## Causality Equivalence Study (March 2026)

Deep analysis of causality violation handling across Executive and ExecutiveFastLight:

| Scenario | Executive (default) | EFL (default) |
|----------|-------------------|---------------|
| Past event | Drops (returns long.MinValue) | Fires at _now |
| Throw configured | Yes, CausalityException | No, only logs |

**Key divergence:** Executive DROPS, EFL FIRES when `IgnoreCausalityViolations=true` (default).
