# Dynamic TaskSeq Implementation Guide (issue #246) - COMPLETED

## Status: PR submitted in run 24059016414 (2026-04-07)
## Branch: repo-assist/feat-dynamic-246-2026-04-07

## Implementation Summary

### TaskSeqDynamicInfo<'T>
- Inherits ResumptionDynamicInfo<TaskSeqStateMachineData<'T>>
- Overrides MoveNext(sm: byref<TaskSeqStateMachine<'T>>):
  - Calls this.ResumptionFunc.Invoke(&sm)
  - If returns true: SetResult(false), builder.Complete(), completed=true
  - If current.IsSome: SetResult(true)
  - Else: schedule via awaiter.UnsafeOnCompleted
  - try/catch for exceptions -> promiseOfValueOrEnd.SetException
- Overrides SetStateMachine as no-op

### TaskSeqDynamic<'T>
- Inherits TaskSeqBase<'T>
- Fields: _machine: TaskSeqStateMachine<'T>, _initialResumptionFunc: TaskSeqResumptionFunc<'T>
- InitDynamicMachineData(ct): creates Data, sets boxedSelf, cancellationToken, builder, ResumptionDynamicInfo
- GetAsyncEnumerator: same pattern as TaskSeq (first-call optimization, clone otherwise)
- IAsyncStateMachine.MoveNext: moveNextRef &this._machine (calls ResumptionDynamicInfo.MoveNext)
- All other interfaces identical to TaskSeq<'Machine,'T>

### TaskSeqBuilder.Run else-branch
- Creates TaskSeqDynamic, sets _initialResumptionFunc from code.Invoke
- Added #nowarn "3513" to suppress FS3513 for the delegate call

### TaskSeqDynamicBuilder
- Inherits TaskSeqBuilder, no overrides
- taskSeqDynamic = TaskSeqDynamicBuilder()

## Key Insight
ResumableStateMachine.IAsyncStateMachine.MoveNext() calls
ResumptionDynamicInfo.MoveNext(ref this) when ResumptionDynamicInfo is set.
The ResumptionFunc in ResumptionDynamicInfo is mutable and updated by the
compiler-generated FSI code on each yield point.
