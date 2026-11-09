namespace FSharp.Control

// note: these are *not* experimental features anymore, but they forgot to switch off the flag
#nowarn "57" // Experimental library feature, requires '--langversion:preview'.
#nowarn "1204" // This construct is for use by compiled F# code and should not be used directly.
#nowarn "3513" // Resumable code invocation: intentionally calling ResumableCode as a regular delegate in the dynamic path.

open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Threading
open System.Threading.Tasks

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers // raises warning FS0057
open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators

[<AutoOpen>]
module TaskExtensions =

    // Add asynchronous for loop to the 'task' and 'backgroundTask' computation builders
    type TaskBuilderBase with

        /// Used by `For`. F# currently doesn't support `while!`, so this cannot be called directly from the task CE
        /// This code is mostly a copy of TaskSeq.WhileAsync.
        member inline _.WhileAsync([<InlineIfLambda>] condition: unit -> ValueTask<bool>, body: TaskCode<_, unit>) : TaskCode<_, _> =
            let mutable condition_res = true

            // ResumableCode.While has both a dynamic and static implementation, but the body below
            // needs its own dynamic implementation (tested in TaskSeq.Dynamic.Forced.Tests.CE.fs).
            ResumableCode.While(
                (fun () -> condition_res),
                TaskCode<_, _>(fun sm ->
                    if __useResumableCode then
                        //-- RESUMABLE CODE START
                        let mutable __stack_condition_fin = true
                        let __stack_vtask = condition ()

                        let mutable awaiter = __stack_vtask.GetAwaiter()

                        if awaiter.IsCompleted then
                            Debug.logInfo "at Task.WhileAsync: returning completed task"

                            __stack_condition_fin <- true
                            condition_res <- awaiter.GetResult()
                        else
                            Debug.logInfo "at Task.WhileAsync: awaiting non-completed task"

                            // This will yield with __stack_fin = false
                            // This will resume with __stack_fin = true

                            // NOTE (AB): if this extra let-binding isn't here, we get NRE exceptions, infinite loops (end of seq not signaled) and warning FS3513
                            let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                            __stack_condition_fin <- __stack_yield_fin

                            if __stack_condition_fin then
                                condition_res <- awaiter.GetResult()


                        if __stack_condition_fin then
                            if condition_res then body.Invoke(&sm) else true
                        else
                            sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)
                            false
                    //-- RESUMABLE CODE END
                    else
                        // Dynamic path: follow the protocol of FSharp.Core's dynamic task implementation, which
                        // expects the awaiter in ResumptionData and does the AwaitUnsafeOnCompleted call itself.
                        // ResumableCode.Yield() can't be used here, as in dynamic code it resumes with a continuation
                        // that just returns 'true', skipping the code that reads the result of the condition.
                        let awaiter = condition().GetAwaiter()

                        if awaiter.IsCompleted then
                            condition_res <- awaiter.GetResult()
                            if condition_res then body.Invoke(&sm) else true
                        else
                            sm.ResumptionDynamicInfo.ResumptionData <- (awaiter :> ICriticalNotifyCompletion)

                            sm.ResumptionDynamicInfo.ResumptionFunc <-
                                TaskResumptionFunc<_>(fun sm ->
                                    condition_res <- awaiter.GetResult()
                                    if condition_res then body.Invoke(&sm) else true)

                            false)
            )

        member inline this.For(source: TaskSeq<'T>, body: 'T -> TaskCode<_, unit>) : TaskCode<_, unit> =
            TaskCode<'TOverall, unit>(fun sm ->
                this
                    .Using(
                        source.GetAsyncEnumerator CancellationToken.None,
                        (fun e ->
                            this.WhileAsync(
                                // __debugPoint is only available from FSharp.Core 6.0.4
                                //(fun () ->
                                //    Microsoft.FSharp.Core.CompilerServices.StateMachineHelpers.__debugPoint
                                //        "ForLoop.InOrToKeyword"

                                //    e.MoveNextAsync()),
                                e.MoveNextAsync,
                                (fun sm -> (body e.Current).Invoke(&sm))
                            ))
                    )
                    .Invoke(&sm))
