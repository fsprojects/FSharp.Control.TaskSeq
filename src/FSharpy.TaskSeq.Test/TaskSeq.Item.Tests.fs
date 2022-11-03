module FSharpy.Tests.Item

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

//
// TaskSeq.item
// TaskSeq.tryItem
//

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-item throws on empty sequences`` variant = task {
        fun () -> Gen.getEmptyVariant variant |> TaskSeq.item 0 |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-item throws on empty sequence - high index`` variant = task {
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.item 50000
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryItem returns None on empty sequences`` variant = task {
        let! nothing = Gen.getEmptyVariant variant |> TaskSeq.tryItem 0
        nothing |> should be None'
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryItem returns None on empty sequence - high index`` variant = task {
        let! nothing = Gen.getEmptyVariant variant |> TaskSeq.tryItem 50000
        nothing |> should be None'
    }

module Singleton =
    [<Fact>]
    let ``TaskSeq-item gets the first item in a singleton sequence`` () = task {
        let! head = taskSeq { yield 10 } |> TaskSeq.item 0 // zero-based!
        head |> should equal 10
    }

    [<Fact>]
    let ``TaskSeq-tryItem gets the first item in a singleton sequence`` () = task {
        let! head = taskSeq { yield 10 } |> TaskSeq.tryItem 0 // zero-based!
        head |> should be Some'
        head |> should equal (Some 10)
    }

    [<Fact>]
    let ``TaskSeq-item throws when accessing 2nd item in singleton sequence`` () = task {
        fun () -> taskSeq { yield 10 } |> TaskSeq.item 1 |> Task.ignore // zero-based!
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Fact>]
    let ``TaskSeq-tryItem returns None when accessing 2nd item in singleton sequence`` () = task {
        let! nothing = taskSeq { yield 10 } |> TaskSeq.tryItem 1 // zero-based!
        nothing |> should be None'
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-item throws when not found`` variant = task {
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.item 10
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryItem returns None when not found`` variant = task {
        let! nothing = Gen.getSeqImmutable variant |> TaskSeq.tryItem 10 // zero-based index

        nothing |> should be None'
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-item can get the first and last item in a longer sequence`` variant = task {
        let! head = Gen.getSeqImmutable variant |> TaskSeq.item 0
        let! tail = Gen.getSeqImmutable variant |> TaskSeq.item 9
        head |> should equal 1
        tail |> should equal 10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryItem can get the first and last item in a longer sequence`` variant = task {
        let! head = Gen.getSeqImmutable variant |> TaskSeq.tryItem 0 // zero-based!
        let! tail = Gen.getSeqImmutable variant |> TaskSeq.tryItem 9

        head |> should equal (Some 1)
        tail |> should equal (Some 10)
    }

module SideEffect =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-item prove it searches the whole sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        fun () -> ts |> TaskSeq.item 10 |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        let! head = TaskSeq.head ts
        head |> should equal 11 // all side effects have executed
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryItem prove it searches the whole sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! item = ts |> TaskSeq.tryItem 10

        item |> should be None'
        let! head = TaskSeq.head ts
        head |> should equal 11 // all side effects have executed
    }

    [<Fact>]
    let ``TaskSeq-item prove we don't iterate further than the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            yield 1
            i <- i + 1
            yield 2
            i <- i + 1 // we never get here
        }

        // zero-based index
        do! ts |> TaskSeq.item 1 |> Task.map (should equal 2)
        i |> should equal 2 // last side effect is not executed
    }

    [<Fact>]
    let ``TaskSeq-tryItem prove we don't iterate further than the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            yield 1
            i <- i + 1
            yield 2
            i <- i + 1 // we never get here
        }

        // zero-based index
        do! ts |> TaskSeq.tryItem 1 |> Task.map (should equal (Some 2))
        i |> should equal 2 // last side effect is not executed
    }

    [<Fact>]
    let ``TaskSeq-item prove we iterate beyond the end when not found`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            yield 1
            i <- i + 1
            yield 2
            i <- i + 1 // we never get here
        }

        // zero-based
        fun () -> ts |> TaskSeq.item 2 |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        i |> should equal 3 // last side effect MUST be executed
    }

    [<Fact>]
    let ``TaskSeq-tryItem prove we iterate beyond the end when not found`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            yield 1
            i <- i + 1
            yield 2
            i <- i + 1 // we never get here
        }

        do! ts |> TaskSeq.tryItem 2 |> Task.map (should be None')
        i |> should equal 3 // last side effect MUST be executed
    }


module Performance =

    [<Theory; InlineData 10_000; InlineData 100_000; InlineData 1_000_000>]
    let ``TaskSeq-tryItem in a very long sequence -- yield variant`` total = task {
        let! head =
            taskSeq {
                for i in [ 0..total ] do
                    yield i
            }
            |> TaskSeq.tryItem total // zero-based!

        head |> should equal (Some total)
    }

    [<Theory; InlineData 10_000; InlineData 100_000; InlineData 1_000_000>]
    let ``[compare] Seq-tryItem in a very long sequence -- yield using F# Seq`` total = task {
        // this test is just for smoke-test perf comparison with TaskSeq above
        let head =
            seq {
                for i in [ 0..total ] do
                    yield i
            }
            |> Seq.tryItem total // zero-based!

        head |> should equal (Some total)
    }

    [<Theory; InlineData 10_000; InlineData 100_000; InlineData 1_000_000>]
    let ``TaskSeq-tryItem in a very long sequence -- array variant`` total = task {
        let! head = taskSeq { yield! [| 0..total |] } |> TaskSeq.tryItem total // zero-based!

        head |> should equal (Some total)
    }

    [<Theory; InlineData 10_000; InlineData 100_000; InlineData 1_000_000>]
    let ``[compare] Seq-tryItem in a very long sequence -- array using F# Seq`` total = task {
        // this test is just for smoke-test perf comparison with TaskSeq above
        let head = seq { yield! [| 0..total |] } |> Seq.tryItem total // zero-based!

        head |> should equal (Some total)
    }

module Other =
    [<Fact>]
    let ``TaskSeq-item accepts Int-MaxValue`` () = task {
        let make50 () = Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50

        fun () -> make50 () |> TaskSeq.item Int32.MaxValue |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            TaskSeq.empty<string>
            |> TaskSeq.item Int32.MaxValue
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Fact>]
    let ``TaskSeq-tryItem accepts Int-MaxValue`` () = task {
        let! nope =
            Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
            |> TaskSeq.tryItem Int32.MaxValue

        nope |> should be None'

        let! nope = TaskSeq.empty<string> |> TaskSeq.tryItem Int32.MaxValue
        nope |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-item always throws with negative values`` () = task {
        let make50 () = Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50

        fun () -> make50 () |> TaskSeq.item -1 |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> make50 () |> TaskSeq.item -10000 |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> make50 () |> TaskSeq.item Int32.MinValue |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> TaskSeq.empty<string> |> TaskSeq.item -1 |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        fun () -> TaskSeq.empty<string> |> TaskSeq.item -10000 |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

        fun () ->
            TaskSeq.empty<string>
            |> TaskSeq.item Int32.MinValue
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Fact>]
    let ``TaskSeq-tryItem throws with negative values`` () = task {
        let make50 () = Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50

        let! nothing = make50 () |> TaskSeq.tryItem -1
        nothing |> should be None'

        let! nothing = make50 () |> TaskSeq.tryItem -10000
        nothing |> should be None'

        let! nothing = make50 () |> TaskSeq.tryItem Int32.MinValue
        nothing |> should be None'

        let! nothing = TaskSeq.empty<string> |> TaskSeq.tryItem -1
        nothing |> should be None'

        let! nothing = TaskSeq.empty<string> |> TaskSeq.tryItem -10000
        nothing |> should be None'

        let! nothing = TaskSeq.empty<string> |> TaskSeq.tryItem Int32.MinValue
        nothing |> should be None'
    }
