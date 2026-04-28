# Session: EFL Causality Fix

**Date:** 2026-03-17T20-30-00Z  
**Agent:** Parker  
**Status:** ✅ Complete

## Summary

Parker restored causality enforcement to ExecutiveFastLight, correcting a long-standing divergence from Executive where causality violations were silently ignored instead of throwing exceptions.

## Work Completed

### Changes Made

1. **ExecutiveFastLight.RequestEvent()** — Replaced `Console.WriteLine()` dead-end with proper `throw new CausalityException()` matching Executive's message format
2. **ExecutiveFastLight.RequestDaemonEvent()** — Applied same fix as RequestEvent()
3. **ExecutiveFastLight.StartWcv()** — Removed `if (true)` guard that prevented throw path from executing; now throws `CausalityException` on causality violation

### Test Updates

- Renamed `ExecutiveFastLight_CausalityViolation_WhenNotIgnored_StillFiresAtNow` to `ExecutiveFastLight_CausalityViolation_WhenNotIgnored_Throws`
- Updated test to assert `Assert.Throws<CausalityException>()` correctly
- Verified outer event fires before violation detection
- WhenIgnored_ClampsToNow test remains unchanged (clamping behavior preserved when enforcement disabled)

### Files Modified

- `src/Sage/Core/ExecutiveFastLight.cs` (3 sites: RequestEvent, RequestDaemonEvent, StartWcv)
- `tests/SageTestLib/TestExecutive.cs` (test renamed and assertion updated)
- `.squad/agents/parker/history.md` (session recorded)
- `.squad/agents/hudson/history.md` (related work noted)
- `.squad/decisions.md` (decision appended)

## Current State

- **Branch:** `feature/dotnet10`
- **Tests:** **351/351 passing** ✅
- **Build:** 0 errors, 0 warnings
- **Commit:** `0469a43` — "fix: restore EFL causality enforcement"

## Behavior Change

When `IgnoreCausalityViolations=false`:
- **Before:** Causality violations logged to console but ignored
- **After:** CausalityException thrown, enforcing causality constraints

When `IgnoreCausalityViolations=true`:
- **Behavior unchanged:** Violations still clamped to `_now` (no exceptions)

## Known Divergence

Executive wraps CausalityException in RuntimeException (due to multi-threaded dispatch loop with try/catch), while EFL throws CausalityException directly. This is a minor implementation detail; both enforce causality correctly now.

## Backlog Impact

- ✅ EFL causality enforcement restored (completed)
- ⏳ Remaining: Tier 2 CA rules (~332 violations: CA1063, CA2214, CA2000, CA1822, CA1032)
- ⏳ Remaining: 3 `[Ignore]`'d Phase 2 prep tests (blocked on Phase 2 API work)
