# Session Log — Causality Investigation

**Timestamp:** 2026-03-17T19:55:00Z  
**Session:** Hudson causality equivalence investigation  
**Status:** ✅ Complete

## Summary

Investigated causality violation handling in Executive and ExecutiveFastLight. Found significant behavioral divergence:

- **Executive + IgnoreCausalityViolations=true:** Returns `long.MinValue` — event is **dropped**
- **EFL + IgnoreCausalityViolations=true:** Clamps to `_now` — event **fires at _now**

When `IgnoreCausalityViolations=false`:
- **Executive:** Throws `CausalityException` (wrapped in `RuntimeException`)
- **EFL:** Calls `Console.WriteLine()` — **throw is commented out**, then still fires event

## Tests Added

3 tests added to `TestExecutive.cs` (#region Causality):
1. EFL ignore mode clamps to Now
2. EFL enforce mode (broken throw) still fires
3. Cross-impl divergence: Executive drops, EFL fires

**Result:** 348 → 351 passing tests ✅

## Key Issue

EFL's causality enforcement is non-functional — the throw in `RequestEvent()` with `_ignore=false` is commented out. Users expecting exceptions will get silent console logging instead.

**Recommendation:** Document this or implement proper enforcement (breaking change).
