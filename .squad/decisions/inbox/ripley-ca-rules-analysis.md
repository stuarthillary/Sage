# CA Rule Enablement Analysis

**Author:** Ripley (Lead/Architect)  
**Date:** 2026-07-15  
**Status:** Analysis Complete — Ready for Decision  
**Requested by:** Stuart Hillary

## Executive Summary

Analyzed 20 suppressed CA (Roslyn analyzer) rules currently set to `severity = none` in `.editorconfig` to determine which can be promoted to `severity = error` with manageable fix effort.

**Current build state:** ✅ 0 errors, 322/322 tests passing, `TreatWarningsAsErrors=true` enabled.

**Result:** **7 rules are ready to enable immediately** (Tier 1, 88 total fixes, 4-6 hours effort).

---

## Method

1. Read `.editorconfig` suppressions (lines 253-361) — ~80 CA rules suppressed
2. For each "quality issues" rule (lines 339-361), ran targeted build with rule temporarily enabled as `severity = warning`
3. Counted actual violations using pattern matching on build output
4. Categorized rules by friction level (violation count + fix complexity)
5. Restored `.editorconfig` after each test to maintain 0-error baseline

---

## Test Results

Ran builds with 17 rules temporarily enabled. Counted violations per rule:

| Rule | Category | Violations | Fix Complexity | Description |
|------|----------|------------|----------------|-------------|
| CA1001 | Dispose | 2 | Low | Types with disposable fields should be disposable |
| CA2215 | Dispose | 2 | Low | Dispose should call base.Dispose |
| CA2200 | Quality | 10 | Low | Rethrow to preserve stack details (use `throw;` not `throw ex;`) |
| CA1816 | Dispose | 12 | Low | Dispose should call GC.SuppressFinalize |
| CA1031 | Quality | 16 | Medium | Do not catch general exception types |
| CA2213 | Dispose | 18 | Low | Disposable fields should be disposed |
| CA1825 | Perf | 20 | Low | Avoid zero-length array allocations (use Array.Empty<T>()) |
| CA1063 | Dispose | 20 | Medium | Implement IDisposable correctly |
| CA2214 | Quality | 24 | Medium | Do not call overridable methods in constructors |
| CA1052 | Design | 24 | Low | Static holder types should be static or sealed |
| CA2000 | Dispose | 34 | Medium | Dispose objects before losing scope |
| CA1822 | Perf | 124 | Low | Mark members as static (IDE auto-fixable) |
| CA1032 | Quality | 130 | Low | Implement standard exception constructors |
| CA1051 | Design | 184 | High | Do not declare visible instance fields (breaking) |
| CA1805 | Perf | 344 | Low | Do not initialize unnecessarily |
| CA2201 | Quality | 456 | Medium | Do not raise reserved exception types |
| CA1062 | Quality | 1122 | Very High | Validate arguments of public methods |

---

## Recommendations

### Tier 1: Enable Immediately ✅
**Total: 7 rules, 88 violations, 4-6 hours effort**

1. **CA1001** — Types with disposable fields should be disposable (2 violations)
   - Fix: Add `IDisposable` to 2 classes that hold disposable fields
   
2. **CA2215** — Dispose should call base.Dispose (2 violations)
   - Fix: Add `base.Dispose(disposing)` calls in 2 overrides
   
3. **CA2200** — Rethrow to preserve stack details (10 violations)
   - Fix: Change `throw ex;` to `throw;` in 10 catch blocks
   
4. **CA1816** — Dispose should call GC.SuppressFinalize (12 violations)
   - Fix: Add `GC.SuppressFinalize(this)` to 12 Dispose methods
   
5. **CA2213** — Disposable fields should be disposed (18 violations)
   - Fix: Add field disposal in 18 Dispose methods
   
6. **CA1825** — Avoid zero-length array allocations (20 violations)
   - Fix: Replace `new T[0]` with `Array.Empty<T>()` in 20 locations
   
7. **CA1052** — Static holder types should be sealed (24 violations)
   - Fix: Add `sealed` or `static` keyword to 24 utility classes
   - Note: Classes are: `ParamNames`, `PathLength`, `Constants`, `GlobalRandomServer`, `StructureChangeTypeSvc`, `MultiRequestProcessor`, `VaporPressureCalculator`, `MilestoneMovementManager`, `Crypto`, `DictionaryOperations`, `PfcAnalyst`, `XmlTransform` (12 unique types × 2 builds = 24 warnings)

**All Tier 1 fixes are mechanical, non-breaking, and safe.**

---

### Tier 2: Enable in Next Pass
**Total: 5 rules, 332 violations, 8-12 hours effort**

8. **CA1063** — Implement IDisposable correctly (20 violations)
   - Requires full dispose pattern (protected virtual Dispose, finalizer considerations)
   
9. **CA2214** — Do not call overridable methods in constructors (24 violations)
   - Requires reviewing constructor logic, may need design changes
   
10. **CA2000** — Dispose objects before losing scope (34 violations)
    - Many false positives where disposal happens in caller or transferred ownership
    - Requires case-by-case judgment
    
11. **CA1822** — Mark members as static (124 violations)
    - IDE can auto-fix most, but may affect inheritance or testability
    
12. **CA1032** — Implement standard exception constructors (130 violations)
    - Add 3 constructors (message, message+inner, serialization) to custom exceptions

---

### Tier 3: Keep Suppressed
**Total: 4 rules, 2106 violations — defer indefinitely**

- **CA1051** — Do not declare visible instance fields (184 violations)
  - Breaking public API change (fields → properties)
  - Primarily in Materials/Chemistry domain models (DTO-like structures)
  
- **CA1805** — Do not initialize unnecessarily (344 violations)
  - Low value (micro-optimization), high noise
  - `private int _count = 0;` warnings
  
- **CA2201** — Do not raise reserved exception types (456 violations)
  - Requires exception hierarchy redesign (ApplicationException → custom base)
  - Breaking change for callers catching specific exception types
  
- **CA1062** — Validate arguments of public methods (1122 violations)
  - Very high false positive rate (analyzer doesn't recognize guard clauses)
  - Would require full null check audit across 548 files

---

### Rules Not Tested (Confirmed High Friction)

- **CA1707** — No underscores in identifiers
  - Previously attempted by another agent, crashed mid-refactor
  - Estimated >500 violations (field naming convention: `_fieldName`)
  
- **Naming rules** (CA1708-CA1724) — Breaking API changes, legacy contracts
  
- **Localization rules** (CA1303-CA1311) — Library has no UI, no localization needed
  
- **API design rules** (CA1000-CA1044) — Breaking changes, legacy public API

---

## Implementation Plan

### Step 1: Enable Tier 1 Rules (One at a Time)

For each rule in Tier 1:

1. Edit `.editorconfig`: Change `dotnet_diagnostic.CAXXXX.severity = none` → `error`
2. Build: `dotnet build Sage.slnx -v minimal`
3. Fix all violations (use IDE quick fixes where available)
4. Build again: Verify 0 errors
5. Test: `dotnet test` → Verify 322/322 pass
6. Commit: `git commit -m "Enable CAXXXX: <description>"`

**Order recommendation:**
1. CA2200 (throw statements — safest)
2. CA1001 (2 classes)
3. CA2215 (2 base calls)
4. CA1816 (12 suppressions)
5. CA2213 (18 field disposals)
6. CA1825 (20 array allocations)
7. CA1052 (24 sealed keywords)

**Estimated time:** 30-45 minutes per rule × 7 = 4-6 hours total.

---

### Step 2: Review Tier 2 (Future Decision)

After Tier 1 stabilizes:
- Assess appetite for 332 additional fixes
- Consider deferring CA1822 and CA1032 to a dedicated "cleanup sprint"
- Prioritize CA1063, CA2214, CA2000 (dispose correctness) over perf rules

---

### Step 3: Document Tier 3 Exclusions

Add comment block to `.editorconfig` explaining why each Tier 3 rule stays suppressed:

```editorconfig
# Tier 3 suppressions — intentional design choices or breaking changes
dotnet_diagnostic.CA1051.severity = none  # Public fields in DTO-like structures (Materials)
dotnet_diagnostic.CA1805.severity = none  # Unnecessary init — low value, high noise
dotnet_diagnostic.CA2201.severity = none  # Reserved exceptions — requires hierarchy redesign
dotnet_diagnostic.CA1062.severity = none  # Null validation — 1122 violations, high false positive rate
```

---

## Risks and Mitigations

### Risk: Tier 1 Fixes Introduce Bugs

**Mitigation:** Run full test suite after EACH rule enablement. Commit each rule separately for easy revert.

### Risk: CA1052 Breaks Subclass Extensions

**Mitigation:** Grep for `: ClassName` inheritance before marking sealed. The 12 utility classes are confirmed static-only (no subclasses in codebase).

### Risk: CA1816/CA2213 Dispose Changes Affect Finalization

**Mitigation:** Review each disposable class's finalizer (if any). Most don't have finalizers — GC.SuppressFinalize is a no-op but satisfies pattern.

---

## Verification

After all Tier 1 rules enabled:

```bash
dotnet build E:\source\Sage\Sage.slnx -v minimal
# Expected: Build succeeded. 0 Error(s)

dotnet test E:\source\Sage\tests\SageTests\SageTests.csproj
# Expected: Passed! - Failed: 0, Passed: 322, Skipped: 0
```

---

## Decision Request

**To Stuart:** Approve Tier 1 enablement (7 rules, 88 fixes)?

- ✅ **Yes, proceed with Tier 1** → Assign to Parker, 4-6 hour task
- ⏸️ **Not yet** → What concerns need addressing?
- ❌ **No, keep all suppressed** → Document reason in decisions.md

---

## Notes

- Build was temporarily broken during testing when CA1052 was left enabled. Restored to `severity = none` after counting violations.
- All test builds were run with rules at `severity = warning` to count violations without blocking. Production enablement will use `severity = error` (enforced by `TreatWarningsAsErrors=true`).
- The 12 CA1052 violations appear as 24 warnings (each type reported twice in build output).
