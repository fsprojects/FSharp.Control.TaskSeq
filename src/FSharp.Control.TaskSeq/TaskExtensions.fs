namespace FSharp.Control

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Microsoft.FSharp.Core.CompilerServices
open Microsoft.FSharp.Core.LanguagePrimitives.IntrinsicOperators

#nowarn "57"
#nowarn "1204"
#nowarn "3513"

[<AutoOpen>]
module TaskExtensions =

    // Add asynchronous for loop to the 'task' computation builder
    type Microsoft.FSharp.Control.TaskBuilder with

        /// Used by `For`. F# currently doesn't support `while!`, so this cannot be called directly from the task CE
        /// This code is mostly a copy of TaskSeq.WhileAsync.
        member inline _.WhileAsync
            (
                [<InlineIfLambda>] condition: unit -> ValueTask<bool>,
                body: TaskCode<_, unit>
            ) : TaskCode<_, _> =
            let mutable condition_res = true

            // note that this While itself has both a dynamic and static implementation
            // so we don't need to add that here (TODO: how to verify?).
            ResumableCode.While(
                (fun () -> condition_res),
                TaskCode<_, _>(fun sm ->
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
                        let __stack_yield_fin = ResumableCode.Yield().Invoke(&sm)
                        __stack_condition_fin <- __stack_yield_fin

                        if __stack_condition_fin then
                            condition_res <- awaiter.GetResult()


                    if __stack_condition_fin then
                        if condition_res then body.Invoke(&sm) else true
                    else
                        sm.Data.MethodBuilder.AwaitUnsafeOnCompleted(&awaiter, &sm)
                        false)
            )

        member inline this.For(source: taskSeq<'T>, body: 'T -> TaskCode<_, unit>) : TaskCode<_, unit> =
            TaskCode<'TOverall, unit>(fun sm ->
                this
                    .Using(
                        source.GetAsyncEnumerator(CancellationToken()),
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
