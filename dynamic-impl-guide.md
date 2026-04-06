# Dynamic TaskSeq Implementation Guide (issue #246)

## Solution Summary

The fix requires two key changes:

### 1. TaskSeqDynamic - FSI-ONLY, ResumptionDynamicInfo-based

**Remove** static-machine fields/methods: `_initialMachine`, `InitMachineData`, `InitFromStaticMachine`.
**Keep only**: `_machine`, `_initialResumptionFunc`, `InitDynamicMachineData`, `SetResumptionFuncFromCode`.

`GetAsyncEnumerator` simplified to only handle FSI path:
- if Data is null and same thread: InitDynamicMachineData(ct); return this
- else: clone with same _initialResumptionFunc, InitDynamicMachineData, return clone

`IAsyncStateMachine.MoveNext` = `moveNextRef &this._machine` (routes via ResumptionDynamicInfo)

### 2. TaskSeqDynamicBuilder - Just inherits, no Run override

```fsharp
type TaskSeqDynamicBuilder() =
    inherit TaskSeqBuilder()
```

No Run override. In compiled code = identical to taskSeq.
In FSI the inherited else-branch creates TaskSeqDynamic.

## Key Insight (Root Cause of Failures)

Storing compiler-generated machine struct in TaskSeqStateMachine (base type) field
LOSES the specialized IAsyncStateMachine.MoveNext. The specialized MoveNext is baked
into the compiler-generated subtype. Storing in base type = library default MoveNext =
calls ResumptionDynamicInfo.MoveNext = null -> NullReferenceException.

TaskSeq avoids this by using generic 'Machine type parameter.
TaskSeqDynamic avoids this by not using __stateMachine at all (FSI-only).

## Test File

Add to: src/FSharp.Control.TaskSeq.Test/TaskSeq.Dynamic.Tests.CE.fs
Module: TaskSeq.Tests.CE.DynamicBuilder
~40 tests covering: empty, single/multi yield, for loop, async bind,
yield!, tryWith, tryFinally, use, cancellation, re-enumeration.

Add to .fsproj: Compile Include="TaskSeq.Dynamic.Tests.CE.fs"

## Branch Status

Branch repo-assist/feat-dynamic-246-2026-04-06 was committed locally in run 24014588386
but safeoutputs session expired before PR creation. LOCAL COMMITS LOST after run.
Next run must re-implement from scratch using this guide.
