module TaskSeq.Tests.Builder

open System
open System.Reflection
open System.Threading.Tasks
open System.Collections.Generic
open System.Threading

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// Tests for TaskSeq computation expression builder edge cases
// These test specific edge cases and internal builder functionality
//

[<Fact>]
let ``taskSeq builder should handle empty computation expression`` () = task {
    let emptySeq = taskSeq { () }
    
    let! items = TaskSeq.toListAsync emptySeq
    items |> should be Empty
}

[<Fact>] 
let ``taskSeq builder should handle single yield`` () = task {
    let seq = taskSeq {
        yield 42
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [42]
}

[<Fact>]
let ``taskSeq builder should handle multiple yields`` () = task {
    let seq = taskSeq {
        yield 1
        yield 2  
        yield 3
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [1; 2; 3]
}

[<Fact>]
let ``taskSeq builder should handle yield! with another TaskSeq`` () = task {
    let innerSeq = taskSeq {
        yield 1
        yield 2
    }
    
    let outerSeq = taskSeq {
        yield 0
        yield! innerSeq
        yield 3
    }
    
    let! items = TaskSeq.toListAsync outerSeq
    items |> should equal [0; 1; 2; 3]
}

[<Fact>]
let ``taskSeq builder should handle yield! with regular sequence`` () = task {
    let seq = taskSeq {
        yield 0
        yield! [1; 2; 3]
        yield 4
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [0; 1; 2; 3; 4]
}

[<Fact>]
let ``taskSeq builder should handle async operations in do!`` () = task {
    let mutable sideEffect = 0
    
    let seq = taskSeq {
        do! Task.Delay(1)
        sideEffect <- 42
        yield sideEffect
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [42]
    sideEffect |> should equal 42
}

[<Fact>]
let ``taskSeq builder should handle let! with async values`` () = task {
    let seq = taskSeq {
        let! value = Task.FromResult(42)
        yield value * 2
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [84]
}

[<Fact>]
let ``taskSeq builder should handle conditional yields`` () = task {
    let seq = taskSeq {
        for i in 1..5 do
            if i % 2 = 0 then
                yield i
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [2; 4]
}

[<Fact>]
let ``taskSeq builder should handle nested for loops`` () = task {
    let seq = taskSeq {
        for i in 1..2 do
            for j in 1..2 do
                yield (i, j)
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [(1, 1); (1, 2); (2, 1); (2, 2)]
}

[<Fact>]
let ``taskSeq builder should handle try-with exception handling`` () = task {
    let seq = taskSeq {
        try
            yield 1
            failwith "test error"
            yield 2
        with
        | _ -> yield 999
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [1; 999]
}

[<Fact>]
let ``taskSeq builder should handle try-finally`` () = task {
    let mutable finalized = false
    
    let seq = taskSeq {
        try
            yield 1
            yield 2
        finally
            finalized <- true
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [1; 2]
    finalized |> should equal true
}

[<Fact>]
let ``taskSeq builder should handle use for disposable resources`` () = task {
    let mutable disposed = false
    
    let disposable = { new IDisposable with
        member _.Dispose() = disposed <- true }
    
    let seq = taskSeq {
        use d = disposable
        yield 42
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [42]
    disposed |> should equal true
}

[<Fact>]
let ``taskSeq builder should handle use! for async disposable resources`` () = task {
    let mutable disposed = false
    
    let asyncDisposable = { new IAsyncDisposable with
        member _.DisposeAsync() = 
            disposed <- true
            ValueTask.CompletedTask }
    
    let seq = taskSeq {
        use! d = Task.FromResult(asyncDisposable)
        yield 42
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [42]
    disposed |> should equal true
}

[<Fact>]
let ``taskSeq builder should handle while loops`` () = task {
    let seq = taskSeq {
        let mutable i = 0
        while i < 3 do
            yield i
            i <- i + 1
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [0; 1; 2]
}

[<Fact>]
let ``taskSeq builder should handle match expressions`` () = task {
    let getValue x = 
        match x with
        | 1 -> "one"
        | 2 -> "two"
        | _ -> "other"
    
    let seq = taskSeq {
        for i in 1..3 do
            yield getValue i
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal ["one"; "two"; "other"]
}

[<Fact>]
let ``taskSeq builder should handle complex async computations`` () = task {
    let asyncComputation x = task {
        do! Task.Delay(1)
        return x * x
    }
    
    let seq = taskSeq {
        for i in 1..3 do
            let! squared = asyncComputation i
            yield squared
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [1; 4; 9]
}

[<Fact>]
let ``taskSeq builder should handle cancellation token propagation`` () = task {
    use cts = new CancellationTokenSource()
    cts.CancelAfter(100)
    
    let seq = taskSeq {
        for i in 1..1000 do
            do! Task.Delay(10, cts.Token)
            yield i
    }
    
    let asyncEnumerator = seq.GetAsyncEnumerator(cts.Token)
    
    let testAction() = task {
        let mutable count = 0
        let mutable hasMore = true
        
        while hasMore do
            let! moveNext = asyncEnumerator.MoveNextAsync()
            hasMore <- moveNext
            if hasMore then count <- count + 1
        
        return count
    }
    
    // Should be cancelled before reaching 1000 items
    let ex = Assert.ThrowsAsync<OperationCanceledException>(fun () -> testAction())
    let! _ = ex
    ()
}

[<Fact>]
let ``taskSeq builder should handle empty yield! with empty sequence`` () = task {
    let seq = taskSeq {
        yield 1
        yield! TaskSeq.empty<int>
        yield 2
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [1; 2]
}

[<Fact>]
let ``taskSeq builder should handle multiple yield! operations`` () = task {
    let seq1 = taskSeq { yield 1; yield 2 }
    let seq2 = taskSeq { yield 3; yield 4 }
    
    let combined = taskSeq {
        yield 0
        yield! seq1
        yield! seq2
        yield 5
    }
    
    let! items = TaskSeq.toListAsync combined
    items |> should equal [0; 1; 2; 3; 4; 5]
}

[<Fact>]
let ``taskSeq builder should handle async functions with side effects`` () = task {
    let mutable callCount = 0
    
    let asyncSideEffect () = task {
        callCount <- callCount + 1
        return callCount
    }
    
    let seq = taskSeq {
        let! first = asyncSideEffect()
        yield first
        let! second = asyncSideEffect() 
        yield second
    }
    
    let! items = TaskSeq.toListAsync seq
    items |> should equal [1; 2]
    callCount |> should equal 2
}

[<Fact>]
let ``taskSeq builder should handle nested taskSeq expressions`` () = task {
    let innerSeq x = taskSeq {
        for i in 1..x do
            yield i * 10
    }
    
    let outerSeq = taskSeq {
        for x in 1..2 do
            yield! innerSeq x
    }
    
    let! items = TaskSeq.toListAsync outerSeq
    items |> should equal [10; 10; 20]
}

[<Fact>]
let ``taskSeq builder should handle computation expression return`` () = task {
    let seq = taskSeq {
        if true then
            return 42
        else
            yield 0
    }
    
    // Should complete immediately with no items when using return
    let! items = TaskSeq.toListAsync seq
    items |> should be Empty
}