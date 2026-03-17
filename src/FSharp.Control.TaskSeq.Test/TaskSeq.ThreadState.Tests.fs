module TaskSeq.Tests.ThreadState

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.threadState
// TaskSeq.threadStateAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg
        <| fun () -> TaskSeq.threadState (fun s _ -> 0, s) 0 null

        assertNullArg
        <| fun () -> TaskSeq.threadStateAsync (fun s _ -> Task.fromResult (0, s)) 0 null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-threadState on empty gives empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.threadState (fun s _ -> 0, s) 0
        |> verifyEmpty

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-threadStateAsync on empty gives empty`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.threadStateAsync (fun s _ -> Task.fromResult (0, s)) 0
        |> verifyEmpty


module Functionality =
    [<Fact>]
    let ``TaskSeq-threadState produces running index`` () = task {
        let ts = taskSeq { yield! [ "a"; "b"; "c" ] }

        let folder state item = (state, item), state + 1

        let! result = TaskSeq.threadState folder 0 ts |> TaskSeq.toArrayAsync
        result |> should equal [| (0, "a"); (1, "b"); (2, "c") |]
    }

    [<Fact>]
    let ``TaskSeq-threadState running sum`` () = task {
        let ts = taskSeq { yield! [ 1..5 ] }

        let folder acc x = acc + x, acc + x

        let! result = TaskSeq.threadState folder 0 ts |> TaskSeq.toArrayAsync
        result |> should equal [| 1; 3; 6; 10; 15 |]
    }

    [<Fact>]
    let ``TaskSeq-threadState state is threaded correctly`` () = task {
        let ts = taskSeq { yield! [ 10; 20; 30 ] }

        let folder state x = x * state, state + 1

        let! result = TaskSeq.threadState folder 1 ts |> TaskSeq.toArrayAsync
        // state starts at 1: 10*1=10, state=2; 20*2=40, state=3; 30*3=90, state=4
        result |> should equal [| 10; 40; 90 |]
    }

    [<Fact>]
    let ``TaskSeq-threadState with singleton`` () = task {
        let ts = TaskSeq.singleton 42

        let! result =
            TaskSeq.threadState (fun s x -> x + s, s + 1) 10 ts
            |> TaskSeq.toArrayAsync

        result |> should equal [| 52 |]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-threadState produces correct length`` variant = task {
        let ts = Gen.getSeqImmutable variant

        let! result =
            TaskSeq.threadState (fun s x -> x, s + 1) 0 ts
            |> TaskSeq.toArrayAsync

        result |> should haveLength 10
        result |> should equal [| 1..10 |]
    }

    [<Fact>]
    let ``TaskSeq-threadStateAsync running sum`` () = task {
        let ts = taskSeq { yield! [ 1..5 ] }

        let folder acc x = Task.fromResult (acc + x, acc + x)

        let! result = TaskSeq.threadStateAsync folder 0 ts |> TaskSeq.toArrayAsync
        result |> should equal [| 1; 3; 6; 10; 15 |]
    }

    [<Fact>]
    let ``TaskSeq-threadStateAsync and threadState produce same results for pure function`` () = task {
        let ts = taskSeq { yield! [ 1..10 ] }
        let ts2 = taskSeq { yield! [ 1..10 ] }

        let syncFolder acc x = x - acc, x + acc
        let asyncFolder acc x = Task.fromResult (syncFolder acc x)

        let! syncResult = TaskSeq.threadState syncFolder 0 ts |> TaskSeq.toArrayAsync

        let! asyncResult =
            TaskSeq.threadStateAsync asyncFolder 0 ts2
            |> TaskSeq.toArrayAsync

        syncResult |> should equal asyncResult
    }

    [<Fact>]
    let ``TaskSeq-threadStateAsync with genuinely async folder`` () = task {
        let ts = taskSeq { yield! [ 1..3 ] }

        let folder state x = task {
            // Use a real async operation to verify the async path works
            let! v = Task.fromResult (x * 10)
            return v, state + x
        }

        let! result = TaskSeq.threadStateAsync folder 0 ts |> TaskSeq.toArrayAsync
        // state: 0; x=1: result=10, state=1; x=2: result=20, state=3; x=3: result=30, state=6
        result |> should equal [| 10; 20; 30 |]
    }

    [<Fact>]
    let ``TaskSeq-threadState is equivalent to scan minus initial state`` () = task {
        // threadState folder 0 [1;2;3] gives the running sums [1;3;6]
        // scan (fun acc x -> acc + x) 0 [1;2;3] gives [0;1;3;6] — drop the head
        let ts = taskSeq { yield! [ 1..5 ] }
        let ts2 = taskSeq { yield! [ 1..5 ] }

        let! viaThread =
            TaskSeq.threadState (fun acc x -> acc + x, acc + x) 0 ts
            |> TaskSeq.toArrayAsync

        let! viaScan =
            TaskSeq.scan (fun acc x -> acc + x) 0 ts2
            |> TaskSeq.skip 1
            |> TaskSeq.toArrayAsync

        viaThread |> should equal viaScan
    }
