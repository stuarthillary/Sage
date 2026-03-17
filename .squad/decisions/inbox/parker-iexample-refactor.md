# Decision: IExample Interface for Sage_SampleCode

**Date:** 2025-07  
**Requested by:** Stuart  
**Implemented by:** Parker

## Context

`samples\Sage_SampleCode` (`Highpoint.Sage.Examples` namespace) contained ~30 example classes, each with a `public static void Run()` method. `Program.cs` registered them all manually with hardcoded `Demonstrate(SomeClass.Run)` calls, requiring a developer to edit `Main()` every time a new example was added.

## Decision

Introduce an `IExample` interface with a single `void Run()` instance method. All example classes implement it. `Program.cs` uses reflection to discover and run them automatically.

## Changes Made

- **New file:** `IExample.cs` — minimal interface (`internal interface IExample { void Run(); }`)
- **31 example classes** converted: `: IExample` added, `public static void Run()` → `public void Run()`
- **10 classes** had `static` removed from their class declaration (files 4–7 used `public static class`)
- **`Program.cs` `Main()`** replaced manual list with:
  ```csharp
  Assembly.GetExecutingAssembly()
      .GetTypes()
      .Where(t => typeof(IExample).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
      .OrderBy(t => t.FullName)
  ```
- **`Demonstrate(Action)`** replaced with **`Demonstrate(IExample)`** — binds instance `Run()` as `Action` so `CreateDocs` (uses `run.Method.DeclaringType`) continues to work.

## Trade-offs

- **Order change:** Examples now run in alphabetical `FullName` order rather than the original file-number order. The original sequence was: Executive → StateManagement → RandomServer → StateMachine → Model → Resources → SequenceControl. Alphabetical gives: Executive → Model → RandomServer → Resources → SequenceControl → StateMachine → StateManagement. This is a cosmetic change with no functional impact.
- **`DefaultModelWithSelfManagingModelObjects`** previously omitted from `Main()` is now auto-discovered and runs. This is intentional.

## Build Result

✅ Build succeeded, 0 errors, 0 warnings. All 351 tests pass.
