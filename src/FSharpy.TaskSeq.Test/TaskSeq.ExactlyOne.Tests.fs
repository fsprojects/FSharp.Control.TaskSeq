module FSharpy.Tests.ExactlyOne

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-exactlyOne throws`` variant = task {
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.exactlyOne
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryExactlyOne returns None`` variant = task {
        let! nothing = Gen.getEmptyVariant variant |> TaskSeq.tryExactlyOne
        nothing |> should be None'
    }

module Other =
    [<Fact>]
    let ``TaskSeq-exactlyOne throws for a sequence of length = two`` () = task {
        fun () ->
            taskSeq {
                yield 1
                yield 2
            }
            |> TaskSeq.exactlyOne
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Fact>]
    let ``TaskSeq-exactlyOne throws for a sequence of length = two - variant`` () = task {
        fun () ->
            Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 2
            |> TaskSeq.exactlyOne
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Fact>]
    let ``TaskSeq-tryExactlyOne returns None for sequence of length = two`` () =
        taskSeq {
            yield 1
            yield 2
        }
        |> TaskSeq.tryExactlyOne
        |> Task.map (should be None')

    [<Fact>]
    let ``TaskSeq-tryExactlyOne returns None for sequence of length = two - variant`` () =
        Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 2
        |> TaskSeq.tryExactlyOne
        |> Task.map (should be None')

    [<Fact>]
    let ``TaskSeq-exactlyOne throws with a larger sequence`` () = task {
        fun () ->
            Gen.sideEffectTaskSeqMicro 50L<µs> 300L<µs> 200
            |> TaskSeq.exactlyOne
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>
    }

    [<Fact>]
    let ``TaskSeq-tryExactlyOne returns None with a larger sequence`` () = task {
        let! nothing =
            Gen.sideEffectTaskSeqMicro 50L<µs> 300L<µs> 20
            |> TaskSeq.tryExactlyOne

        nothing |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-exactlyOne gets the only item in a singleton sequence`` () = task {
        let! exactlyOne = taskSeq { yield 10 } |> TaskSeq.exactlyOne
        exactlyOne |> should equal 10
    }

    [<Fact>]
    let ``TaskSeq-tryExactlyOne gets the only item in a singleton sequence`` () = task {
        let! exactlyOne = taskSeq { yield 10 } |> TaskSeq.tryExactlyOne
        exactlyOne |> should be Some'
        exactlyOne |> should equal (Some 10)
    }

    [<Fact>]
    let ``TaskSeq-exactlyOne gets the only item in a singleton sequence - variant`` () = task {
        let! exactlyOne =
            Gen.sideEffectTaskSeqMs 50<ms> 300<ms> 1
            |> TaskSeq.exactlyOne

        exactlyOne |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-tryExactlyOne gets the only item in a singleton sequence - variant`` () = task {
        let! exactlyOne =
            Gen.sideEffectTaskSeqMs 50<ms> 300<ms> 1
            |> TaskSeq.tryExactlyOne

        exactlyOne |> should be Some'
        exactlyOne |> should equal (Some 1)
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exactlyOne throws`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.exactlyOne
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryExactlyOne returns None`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! head1 = TaskSeq.tryExactlyOne ts
        let! head2 = TaskSeq.tryExactlyOne ts
        head1 |> should be None'
        head2 |> should be None'
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exactlyOne throws`` variant =
        fun () ->
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.exactlyOne
            |> Task.ignore
        |> should throwAsyncExact typeof<ArgumentException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-exactlyOne throws, but sequence remains accessible`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        let! nothing = task {
            try
                return! TaskSeq.exactlyOne ts
            with ex ->
                ex |> should be ofExactType<ArgumentException>
                return -42
        }

        nothing |> should equal -42

        // Test that side-effect has executed. Different sequence variants
        // increase the counter differently but they're never 1
        let! head1 = TaskSeq.head ts
        head1 |> should not' (equal 1)
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryExactlyOne returns None`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let! head1 = TaskSeq.tryExactlyOne ts
        let! head2 = TaskSeq.tryExactlyOne ts
        head1 |> should be None'
        head2 |> should be None'

        // Test that side-effect has executed. Different sequence variants
        // increase the counter differently but they're never 1
        let! head3 = TaskSeq.head ts
        head3 |> should not' (equal 1)
    }
