# TupleSpace .NET 10 Investigation — Brief Summary

**Date:** 2026-03-06  
**Investigated by:** Parker  
**Issue:** 7 TupleTester tests fail on `feature/dotnet10`, pass on `dotnet8`  

## Finding

Root cause identified: **DetachableEvent/ManualResetEventSlim blocking pattern triggers .NET 10's thread pool starvation detection, preventing tasks from completing.**

## Impact

- Simulation events start but don't finish
- Test hardcoded expected arrays no longer match actual count
- Model exits prematurely with "TASKS STILL WAITING TO COMPLETE" error

## Status

Escalated. Three attempted fixes failed. Architectural guidance needed before resolving.

## Next Owner

Ripley (Lead/Architect) — decide threading model and strategy.
