module TaskSeq.Tests.Utils

open System
open System.Threading.Tasks
open Xunit
open FsUnit.Xunit

open FSharp.Control


module AsyncBind =
    [<Fact>]
    let ``Async.bind awaits the async and passes the value to the binder`` () =
        let result =
            async { return 21 }
            |> Async.bind (fun n -> async { return n * 2 })
            |> Async.RunSynchronously

        result |> should equal 42

    [<Fact>]
    let ``Async.bind propagates exceptions from the source async`` () =
        let run () =
            async { return raise (InvalidOperationException "source error") }
            |> Async.bind (fun (_: int) -> async { return 0 })
            |> Async.RunSynchronously

        (fun () -> run () |> ignore)
        |> should throw typeof<InvalidOperationException>

    [<Fact>]
    let ``Async.bind propagates exceptions from the binder`` () =
        let run () =
            async { return 1 }
            |> Async.bind (fun _ -> async { return raise (InvalidOperationException "binder error") })
            |> Async.RunSynchronously

        (fun () -> run () |> ignore)
        |> should throw typeof<InvalidOperationException>

    [<Fact>]
    let ``Async.bind chains correctly`` () =
        let result =
            async { return 1 }
            |> Async.bind (fun n -> async { return n + 10 })
            |> Async.bind (fun n -> async { return n + 100 })
            |> Async.RunSynchronously

        result |> should equal 111

    [<Fact>]
    let ``Async.bind passes the unwrapped value, not the Async wrapper`` () =
        // This test specifically verifies the bug fix: binder receives 'T, not Async<'T>
        let mutable receivedType = typeof<unit>

        async { return 42 }
        |> Async.bind (fun (n: int) ->
            receivedType <- n.GetType()
            async { return () })
        |> Async.RunSynchronously

        receivedType |> should equal typeof<int>


module TaskBind =
    [<Fact>]
    let ``Task.bind awaits the task and passes the value to the binder`` () = task {
        let result =
            task { return 21 }
            |> Task.bind (fun n -> task { return n * 2 })

        let! v = result
        v |> should equal 42
    }

    [<Fact>]
    let ``Task.bind chains correctly`` () = task {
        let result =
            task { return 1 }
            |> Task.bind (fun n -> task { return n + 10 })
            |> Task.bind (fun n -> task { return n + 100 })

        let! v = result
        v |> should equal 111
    }


module AsyncMap =
    [<Fact>]
    let ``Async.map transforms the result`` () =
        let result =
            async { return 21 }
            |> Async.map (fun n -> n * 2)
            |> Async.RunSynchronously

        result |> should equal 42

    [<Fact>]
    let ``Async.map chains correctly`` () =
        let result =
            async { return 1 }
            |> Async.map (fun n -> n + 10)
            |> Async.map (fun n -> n + 100)
            |> Async.RunSynchronously

        result |> should equal 111


module TaskMap =
    [<Fact>]
    let ``Task.map transforms the result`` () = task {
        let result = task { return 21 } |> Task.map (fun n -> n * 2)

        let! v = result
        v |> should equal 42
    }
