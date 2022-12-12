module TaskSeq.Tests.Singleton

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.singleton
//

module EmptySeq =

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-singleton with empty has length one`` variant =
        taskSeq {
            yield! TaskSeq.singleton 10
            yield! Gen.getEmptyVariant variant
        }
        |> TaskSeq.exactlyOne
        |> Task.map (should equal 10)

module SideEffects =
    [<Fact>]
    let ``TaskSeq-singleton with a mutable value`` () =
        let mutable x = 0
        let ts = TaskSeq.singleton x
        x <- x + 1

        // mutable value is dereferenced when passed to a function
        ts |> TaskSeq.exactlyOne |> Task.map (should equal 0)

    [<Fact>]
    let ``TaskSeq-singleton with a ref cell`` () =
        let x = ref 0
        let ts = TaskSeq.singleton x
        x.Value <- x.Value + 1

        ts
        |> TaskSeq.exactlyOne
        |> Task.map (fun x -> x.Value |> should equal 1)

module Other =
    [<Fact>]
    let ``TaskSeq-singleton creates a sequence of one`` () =
        TaskSeq.singleton 42
        |> TaskSeq.exactlyOne
        |> Task.map (should equal 42)

    [<Fact>]
    let ``TaskSeq-singleton with null as value`` () =
        TaskSeq.singleton null
        |> TaskSeq.exactlyOne
        |> Task.map (should be Null)

    [<Fact>]
    let ``TaskSeq-singleton can be yielded multiple times`` () =
        let singleton = TaskSeq.singleton 42

        taskSeq {
            yield! singleton
            yield! singleton
            yield! singleton
            yield! singleton
        }
        |> TaskSeq.toList
        |> should equal [ 42; 42; 42; 42 ]

    [<Fact>]
    let ``TaskSeq-singleton with isEmpty`` () =
        TaskSeq.singleton 42
        |> TaskSeq.isEmpty
        |> Task.map (should be False)

    [<Fact>]
    let ``TaskSeq-singleton with append`` () =
        TaskSeq.singleton 42
        |> TaskSeq.append (TaskSeq.singleton 42)
        |> TaskSeq.toList
        |> should equal [ 42; 42 ]

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-singleton with collect`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.collect TaskSeq.singleton
        |> verify1To10

    [<Fact>]
    let ``TaskSeq-singleton does not throw when getting Current before MoveNext`` () = task {
        let enumerator = (TaskSeq.singleton 42).GetAsyncEnumerator()
        let defaultValue = enumerator.Current // should return the default value for int
        defaultValue |> should equal 0
    }

    [<Fact>]
    let ``TaskSeq-singleton does not throw when getting Current after last MoveNext`` () = task {
        let enumerator = (TaskSeq.singleton 42).GetAsyncEnumerator()
        let! isNext = enumerator.MoveNextAsync()
        isNext |> should be True
        let value = enumerator.Current // the first and only value
        value |> should equal 42

        // move past the end
        let! isNext = enumerator.MoveNextAsync()
        isNext |> should be False
        let defaultValue = enumerator.Current // should return the default value for int
        defaultValue |> should equal 0
    }

    [<Fact>]
    let ``TaskSeq-singleton multiple MoveNext is fine`` () = task {
        let enumerator = (TaskSeq.singleton 42).GetAsyncEnumerator()
        let! isNext = enumerator.MoveNextAsync()
        isNext |> should be True
        let! _ = enumerator.MoveNextAsync()
        let! _ = enumerator.MoveNextAsync()
        let! _ = enumerator.MoveNextAsync()
        let! isNext = enumerator.MoveNextAsync()
        isNext |> should be False

        // should return the default value for int after moving past the end
        let defaultValue = enumerator.Current
        defaultValue |> should equal 0
    }
