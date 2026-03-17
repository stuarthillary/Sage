# Decision: Sample Code `[Order]` Attribute

**Date:** 2026-07-17  
**Author:** Parker  
**Requested by:** Stuart  
**Status:** Implemented ✅

## Context

`samples\Sage_SampleCode` was refactored to use an `IExample` interface + reflection-based discovery. The runner (`Program.cs`) ordered examples alphabetically by `FullName`, which broke the original intentional progression from basic to advanced concepts.

## Decision

Introduce an `[Order(n)]` attribute to restore the original execution order without hard-coding the example list in `Program.cs`.

## Implementation

**New file:** `samples\Sage_SampleCode\OrderAttribute.cs`  
- `internal sealed class OrderAttribute : Attribute` with `int Value` property  
- `[AttributeUsage(AttributeTargets.Class, Inherited = false)]`  
- Namespace: `Highpoint.Sage.Examples`

**31 classes annotated across 7 files:**

| Order | Class | File |
|-------|-------|------|
| 1 | `HelloWorld` | 1_Executive.cs |
| 2 | `TwoCallbacksOutOfSequence` | 1_Executive.cs |
| 3 | `CallbacksWithPriorities` | 1_Executive.cs |
| 4 | `UserData_FollowOn_SelfImposedDelay` | 1_Executive.cs |
| 5 | `ExecCatchesRuntimeExceptionFromSynchronousEvent` | 1_Executive.cs |
| 6 | `RescindingSynchEvent` | 1_Executive.cs |
| 7 | `MoreRescindingPlusAgentBased` | 1_Executive.cs |
| 8 | `BasicWithSuspends` | 1_Executive.cs |
| 9 | `SuspendsWithMixedModeAgents` | 1_Executive.cs |
| 10 | `UsesJoining` | 1_Executive.cs |
| 11 | `RescindMultipleDetachables` | 1_Executive.cs |
| 12 | `Metronomes` | 1_Executive.cs |
| 13 | `PauseAndResume` | 1_Executive.cs |
| 14 | `UseExecController` | 1_Executive.cs |
| 15 | `ExecEventModelAndStates` | 1_Executive.cs |
| 16 | `DaemonEvents` | 1_Executive.cs |
| 17 | `InAgents` | 2_StateManagement.cs |
| 18 | `InUserData` | 2_StateManagement.cs |
| 19 | `OnTheStackFrame` | 2_StateManagement.cs |
| 20 | `SimpleDefaultServer` | 3_RandomServer.cs |
| 21 | `DecorrellatedActivities` | 3_RandomServer.cs |
| 22 | `Default` | 4_StateMachine.cs |
| 23 | `SimpleCustomWithInitialization` | 4_StateMachine.cs |
| 24 | `SimpleEnumStateMachine` | 4_StateMachine.cs |
| 25 | `DefaultModel` | 5_IntroToModel.cs |
| 26 | `SimpleCustomWithInitialization` | 5_IntroToModel.cs |
| 27 | `ServicePoolExample` | 6_Resources.cs |
| 28 | `ServicePoolExampleWithSynchronousEvents` | 6_Resources.cs |
| 29 | `OptimalResourceAcquisition` | 6_Resources.cs |
| 30 | `TaskGraphDemo` | 7_SequenceControl.cs |
| 100 | `DefaultModelWithSelfManagingModelObjects` | 5_IntroToModel.cs |

**Program.cs change:**
```csharp
.OrderBy(t => t.GetCustomAttribute<OrderAttribute>()?.Value ?? int.MaxValue)
.ThenBy(t => t.FullName);  // stable secondary sort
```

## Rationale

- Attribute-based ordering keeps ordering metadata co-located with the class, not in Program.cs
- Any new example added without an `[Order]` attribute will automatically run last (via `int.MaxValue` fallback), which is safe
- `ThenBy(FullName)` provides stable ordering for unordered classes
- No `using` changes required — C# resolves parent namespace types automatically from sub-namespaces

## Build Result

0 errors, 0 warnings ✅
