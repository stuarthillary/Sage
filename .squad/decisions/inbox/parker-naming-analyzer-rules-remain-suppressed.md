# Decision: Naming Analyzer Rules Remain Suppressed (CA1707, CA1708, CA1710, CA1711, CA1713, CA1716, CA1720, CA1721, CA1724, CA1700)

**Date:** 2026-07-16  
**Author:** Parker (.NET Developer)  
**Status:** Confirmed

## Context

During an interrupted refactoring session, an agent changed naming analyzer rules in `.editorconfig` from `severity = none` to `severity = error`. This caused 500+ analyzer errors to become build-breaking.

The rules affected:
- **CA1707:** Identifiers should not contain underscores (122 violations)
- **CA1720:** Identifier contains type name (e.g., "Guid", "Object") (418 violations)
- **CA1716:** Identifiers should not match keywords (54 violations)
- **CA1708:** Identifiers should differ by more than case (30 violations)
- **CA1710, CA1711, CA1713, CA1721, CA1724, CA1700:** Various naming conventions (50+ violations)

## Decision

**Reverted all naming analyzer rules back to `severity = none`.**

## Rationale

1. **Scope too large:** Fixing 500+ naming violations would require:
   - Renaming 122+ identifiers with underscores (public API surface)
   - Changing 418+ properties/parameters named "Guid" or "Object"
   - Breaking changes to legacy API contracts
   - Major refactoring across the entire codebase

2. **Prior design decision:** The comment in `.editorconfig` explicitly states:
   > "Suppressed rules for naming conventions, type name conflicts, and keyword collisions. These are legacy API contracts, or breaking changes that would require major refactoring."

3. **Build restored:** Reverting these rules allowed the build to succeed (0 errors, 322/322 tests passing).

4. **Not a regression:** The previous commit (bdee0a4) had these rules set to `none`, so reverting restores the intended state.

## Future Consideration

If the team decides to enforce stricter naming conventions:
1. Create a phased migration plan
2. Use `severity = suggestion` first to identify violations without breaking builds
3. Fix violations incrementally by module/namespace
4. Consider using Roslyn code fixers/refactoring tools to automate renames
5. Coordinate with the team on breaking change policy (major version bump?)

## Related Files

- `.editorconfig` lines 257-267 (naming analyzer rules)
- Previous commit: bdee0a4 "Enable TreatWarningsAsErrors; suppress design-choice CA rules via .editorconfig"
