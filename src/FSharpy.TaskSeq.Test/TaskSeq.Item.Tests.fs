module FSharpy.Tests.Item

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact>]
let ``TaskSeq-item throws on empty sequences`` () = task {
    fun () -> TaskSeq.empty<string> |> TaskSeq.item 0 |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-item throws on empty sequence - variant`` () = task {
    fun () -> taskSeq { do () } |> TaskSeq.item 50000 |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-item throws when not found`` () = task {
    fun () ->
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.item 51
        |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-item throws when not found - variant`` () = task {
    fun () ->
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.item Int32.MaxValue
        |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Fact>]
let ``TaskSeq-item throws when accessing 2nd item in singleton sequence`` () = task {
    fun () -> taskSeq { yield 10 } |> TaskSeq.item 1 |> Task.ignore // zero-based!
    |> should throwAsyncExact typeof<ArgumentException>
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
let ``TaskSeq-tryItem returns None on empty sequences`` () = task {
    let! nothing = TaskSeq.empty<string> |> TaskSeq.tryItem 0
    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryItem returns None on empty sequence - variant`` () = task {
    let! nothing = taskSeq { do () } |> TaskSeq.tryItem 50000
    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryItem returns None when not found`` () = task {
    let! nothing =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryItem 50 // zero-based index, so a sequence of 50 items has its last item at index 49

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryItem returns None when not found - variant`` () = task {
    let! nothing =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryItem Int32.MaxValue

    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryItem returns None when accessing 2nd item in singleton sequence`` () = task {
    let! nothing = taskSeq { yield 10 } |> TaskSeq.tryItem 1 // zero-based!
    nothing |> should be None'
}

[<Fact>]
let ``TaskSeq-tryItem returns None throws with negative values`` () = task {
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

[<Fact>]
let ``TaskSeq-item can get the first item in a longer sequence`` () = task {
    let! head =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.item 0

    head |> should equal 1
}

[<Fact>]
let ``TaskSeq-item can get the last item in a longer sequence`` () = task {
    let! head =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.item 49

    head |> should equal 50
}

[<Fact>]
let ``TaskSeq-item can get the first item in a singleton sequence`` () = task {
    let! head = taskSeq { yield 10 } |> TaskSeq.item 0 // zero-based index!
    head |> should equal 10
}

[<Fact>]
let ``TaskSeq-tryItem can get the first item in a longer sequence`` () = task {
    let! head =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
        |> TaskSeq.tryItem 0 // zero-based!

    head |> should be Some'
    head |> should equal (Some 1)
}

[<Fact>]
let ``TaskSeq-tryItem in a very long sequence (5_000 items - slow variant)`` () = task {
    let! head = Gen.sideEffectTaskSeq_Sequential 5_001 |> TaskSeq.tryItem 5_000 // zero-based!

    head |> should be Some'
    head |> should equal (Some 5_001)
}

[<Fact>]
let ``TaskSeq-tryItem in a very long sequence (50_000 items - slow variant)`` () = task {
    let! head = Gen.sideEffectTaskSeq_Sequential 50_001 |> TaskSeq.tryItem 50_000 // zero-based!

    head |> should be Some'
    head |> should equal (Some 50_001)
}

[<Fact>]
let ``TaskSeq-tryItem in a very long sequence (50_000 items - fast variant)`` () = task {
    let! head =
        // using taskSeq instead of the delayed-task approach above, which creates an extra closure for each
        // task, we can really see the speed of the 'taskSeq' CE!! This is
        taskSeq {
            for i in [ 0..50_000 ] do
                yield i
        }
        |> TaskSeq.tryItem 50_000 // zero-based!

    head |> should be Some'
    head |> should equal (Some 50_000)
}

[<Fact>]
let ``TaskSeq-tryItem in a very long sequence (50_000 items - using sync Seq)`` () = task {
    // this test is just for smoke-test perf comparison with TaskSeq above
    let head =
        seq {
            for i in [ 0..50_000 ] do
                yield i
        }
        |> Seq.tryItem 50_000 // zero-based!

    head |> should be Some'
    head |> should equal (Some 50_000)
}

[<Fact>]
let ``TaskSeq-tryItem in a very long sequence (500_000 items - fast variant)`` () = task {
    let! head =
        taskSeq {
            for i in [ 0..500_000 ] do
                yield i
        }
        |> TaskSeq.tryItem 500_000 // zero-based!

    head |> should be Some'
    head |> should equal (Some 500_000)
}

[<Fact>]
let ``TaskSeq-tryItem in a very long sequence (500_000 items - using sync Seq)`` () = task {
    // this test is just for smoke-test perf comparison with TaskSeq above
    let head =
        seq {
            for i in [ 0..500_000 ] do
                yield i
        }
        |> Seq.tryItem 500_000 // zero-based!

    head |> should be Some'
    head |> should equal (Some 500_000)
}

[<Fact>]
let ``TaskSeq-tryItem gets the first item in a singleton sequence`` () = task {
    let! head = taskSeq { yield 10 } |> TaskSeq.tryItem 0 // zero-based!
    head |> should be Some'
    head |> should equal (Some 10)
}
