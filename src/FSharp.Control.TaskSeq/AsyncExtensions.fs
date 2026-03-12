namespace FSharp.Control

[<AutoOpen>]
module AsyncExtensions =

    // Awaits a Task<unit> without wrapping exceptions in AggregateException.
    // Async.AwaitTask wraps task exceptions in AggregateException, which breaks try/catch
    // blocks in async {} expressions that expect the original exception type.
    // See: https://github.com/fsprojects/FSharp.Control.TaskSeq/issues/129
    let private awaitTaskCorrect (task: System.Threading.Tasks.Task<unit>) : Async<unit> =
        Async.FromContinuations(fun (cont, econt, ccont) ->
            task.ContinueWith(fun (t: System.Threading.Tasks.Task<unit>) ->
                if t.IsFaulted then
                    let exn = t.Exception

                    if exn.InnerExceptions.Count = 1 then
                        econt exn.InnerExceptions.[0]
                    else
                        econt exn
                elif t.IsCanceled then
                    ccont (System.OperationCanceledException "The operation was cancelled.")
                else
                    cont ())
            |> ignore)

    // Add asynchronous for loop to the 'async' computation builder
    type Microsoft.FSharp.Control.AsyncBuilder with

        member _.For(source: TaskSeq<'T>, action: 'T -> Async<unit>) =
            source
            |> TaskSeq.iterAsync (action >> Async.StartImmediateAsTask)
            |> awaitTaskCorrect
