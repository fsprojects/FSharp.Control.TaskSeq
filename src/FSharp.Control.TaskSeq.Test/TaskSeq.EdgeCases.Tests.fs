module TaskSeq.Tests.EdgeCases

open Xunit
open FsUnit.Xunit
open System
open System.Threading
open System.Threading.Tasks
open FSharp.Control

//
// Tests for edge cases and boundary conditions
//

module NullHandling =
    [<Fact>]
    let ``TaskSeq.toList with null source throws ArgumentNullException`` () =
        (fun () -> TaskSeq.toList null |> ignore) |> should throw typeof<ArgumentNullException>

    [<Fact>]
    let ``TaskSeq.toArray with null source throws ArgumentNullException`` () =
        (fun () -> TaskSeq.toArray null |> ignore) |> should throw typeof<ArgumentNullException>

    [<Fact>]
    let ``TaskSeq.toSeq with null source throws ArgumentNullException`` () =
        (fun () -> TaskSeq.toSeq null |> ignore) |> should throw typeof<ArgumentNullException>

    [<Fact>]
    let ``TaskSeq.isEmpty with null source throws ArgumentNullException`` () = task {
        let! ex = Assert.ThrowsAsync<ArgumentNullException>(fun () -> TaskSeq.isEmpty null)
        ex.ParamName |> should equal "source"
    }

module BoundaryValues =
    [<Fact>]
    let ``TaskSeq.init with zero count creates empty sequence`` () = task {
        let seq = TaskSeq.init 0 (fun i -> i)
        let! isEmpty = TaskSeq.isEmpty seq
        isEmpty |> should be True
    }

    [<Fact>]
    let ``TaskSeq.init with negative count throws ArgumentException`` () =
        (fun () -> TaskSeq.init -1 (fun i -> i) |> ignore) |> should throw typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq.take with zero count creates empty sequence`` () = task {
        let source = taskSeq { yield 1; yield 2; yield 3 }
        let taken = TaskSeq.take 0 source
        let! isEmpty = TaskSeq.isEmpty taken
        isEmpty |> should be True
    }

    [<Fact>]
    let ``TaskSeq.skip with zero count returns original sequence`` () = task {
        let source = taskSeq { yield 1; yield 2; yield 3 }
        let skipped = TaskSeq.skip 0 source
        let! result = TaskSeq.toArray skipped
        result |> should equal [| 1; 2; 3 |]
    }

    [<Fact>]
    let ``TaskSeq.take with negative count throws ArgumentException`` () =
        (fun () -> 
            let source = taskSeq { yield 1 }
            TaskSeq.take -1 source |> ignore
        ) |> should throw typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq.skip with negative count throws ArgumentException`` () =
        (fun () -> 
            let source = taskSeq { yield 1 }
            TaskSeq.skip -1 source |> ignore
        ) |> should throw typeof<ArgumentException>

module LargeSequences =
    [<Fact>]
    let ``TaskSeq.length handles large sequences`` () = task {
        let largeSeq = TaskSeq.init 10000 id
        let! length = TaskSeq.length largeSeq
        length |> should equal 10000
    }

    [<Fact>]
    let ``TaskSeq.take handles taking more than available`` () = task {
        let source = taskSeq { yield 1; yield 2 }
        let taken = TaskSeq.take 5 source
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.toArray taken :> Task)
        ex.Message |> should contain "insufficient"
    }

module CancellationEdgeCases =
    [<Fact>]
    let ``TaskSeq with cancelled token disposes properly`` () = task {
        use cts = new CancellationTokenSource()
        cts.Cancel()

        let seq = taskSeq {
            yield 1
            yield 2
        }

        let! ex = Assert.ThrowsAsync<TaskCanceledException>(fun () -> task {
            use e = seq.GetAsyncEnumerator(cts.Token)
            let! _ = e.MoveNextAsync()
            return ()
        })
        
        ex |> should not (be null)
    }

module DisposalEdgeCases =
    [<Fact>]
    let ``Multiple disposal calls don't throw`` () = task {
        let seq = taskSeq { yield 1; yield 2 }
        let e = seq.GetAsyncEnumerator()
        
        // First disposal
        do! e.DisposeAsync()
        
        // Second disposal should not throw
        do! e.DisposeAsync()
        
        // Test passes if no exception
        Assert.True(true)
    }

    [<Fact>]
    let ``Using enumerator after disposal throws`` () = task {
        let seq = taskSeq { yield 1; yield 2 }
        let e = seq.GetAsyncEnumerator()
        
        // Dispose first
        do! e.DisposeAsync()
        
        // Attempting to use after disposal should throw
        let! ex = Assert.ThrowsAsync<ObjectDisposedException>(fun () -> e.MoveNextAsync().AsTask())
        ex |> should not (be null)
    }

module EmptySequenceOperations =
    [<Fact>]
    let ``TaskSeq.head on empty sequence throws`` () = task {
        let emptySeq = TaskSeq.empty<int>
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.head emptySeq)
        ex.Message |> should contain "empty"
    }

    [<Fact>]
    let ``TaskSeq.last on empty sequence throws`` () = task {
        let emptySeq = TaskSeq.empty<int>
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.last emptySeq)
        ex.Message |> should contain "empty"
    }

    [<Fact>]
    let ``TaskSeq.tail on empty sequence throws`` () = task {
        let emptySeq = TaskSeq.empty<int>
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.tail emptySeq)
        ex.Message |> should contain "empty"
    }

    [<Fact>]
    let ``TaskSeq.exactlyOne on empty sequence throws`` () = task {
        let emptySeq = TaskSeq.empty<int>
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.exactlyOne emptySeq)
        ex.Message |> should contain "empty"
    }

    [<Fact>]
    let ``TaskSeq.exactlyOne on multi-element sequence throws`` () = task {
        let multiSeq = taskSeq { yield 1; yield 2 }
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.exactlyOne multiSeq)
        ex.ParamName |> should equal "source"
    }

module IndexOutOfBounds =
    [<Fact>]
    let ``TaskSeq.item with negative index throws`` () = task {
        let seq = taskSeq { yield 1; yield 2; yield 3 }
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.item -1 seq)
        ex.ParamName |> should equal "index"
    }

    [<Fact>]
    let ``TaskSeq.item with index beyond sequence throws`` () = task {
        let seq = taskSeq { yield 1; yield 2 }
        let! ex = Assert.ThrowsAsync<ArgumentException>(fun () -> TaskSeq.item 5 seq)
        ex.Message |> should contain "bounds"
    }

    [<Fact>]
    let ``TaskSeq.insertAt with negative index throws`` () =
        (fun () -> 
            let seq = taskSeq { yield 1 }
            TaskSeq.insertAt -1 42 seq |> ignore
        ) |> should throw typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq.removeAt with negative index throws`` () =
        (fun () -> 
            let seq = taskSeq { yield 1 }
            TaskSeq.removeAt -1 seq |> ignore
        ) |> should throw typeof<ArgumentException>

    [<Fact>]
    let ``TaskSeq.updateAt with negative index throws`` () =
        (fun () -> 
            let seq = taskSeq { yield 1 }
            TaskSeq.updateAt -1 42 seq |> ignore
        ) |> should throw typeof<ArgumentException>

module TypeCasting =
    [<Fact>]
    let ``TaskSeq.cast handles valid casts`` () = task {
        let objSeq = taskSeq { yield box 1; yield box 2; yield box 3 }
        let intSeq = TaskSeq.cast<int> objSeq
        let! result = TaskSeq.toArray intSeq
        result |> should equal [| 1; 2; 3 |]
    }

    [<Fact>]
    let ``TaskSeq.cast throws on invalid cast`` () = task {
        let objSeq = taskSeq { yield box "string"; yield box 2 }
        let intSeq = TaskSeq.cast<int> objSeq
        
        let! ex = Assert.ThrowsAsync<InvalidCastException>(fun () -> TaskSeq.toArray intSeq :> Task)
        ex |> should not (be null)
    }

    [<Fact>]
    let ``TaskSeq.unbox handles value types`` () = task {
        let objSeq = taskSeq { yield box 1; yield box 2; yield box 3 }
        let intSeq = TaskSeq.unbox<int> objSeq
        let! result = TaskSeq.toArray intSeq
        result |> should equal [| 1; 2; 3 |]
    }

module ConversionEdgeCases =
    [<Fact>]
    let ``TaskSeq.ofArray with empty array creates empty TaskSeq`` () = task {
        let emptyArray = [||]
        let seq = TaskSeq.ofArray emptyArray
        let! isEmpty = TaskSeq.isEmpty seq
        isEmpty |> should be True
    }

    [<Fact>]
    let ``TaskSeq.ofList with empty list creates empty TaskSeq`` () = task {
        let emptyList = []
        let seq = TaskSeq.ofList emptyList
        let! isEmpty = TaskSeq.isEmpty seq
        isEmpty |> should be True
    }

    [<Fact>]
    let ``TaskSeq.ofSeq with empty seq creates empty TaskSeq`` () = task {
        let emptySeq = Seq.empty<int>
        let seq = TaskSeq.ofSeq emptySeq
        let! isEmpty = TaskSeq.isEmpty seq
        isEmpty |> should be True
    }