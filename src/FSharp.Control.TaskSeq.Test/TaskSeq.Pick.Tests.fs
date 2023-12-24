module TaskSeq.Tests.Pick

open System.Collections.Generic

open Xunit
open FsUnit.Xunit

open FSharp.Control


//
// TaskSeq.pick
// TaskSeq.pickAsync
// TaskSeq.tryPick
// TaskSeq.tryPickAsync
//

let picker equalTo x = if x = equalTo then Some x else None
let pickerAsync equalTo x = task { return if x = equalTo then Some x else None }


module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.pick (picker 0) null
        assertNullArg <| fun () -> TaskSeq.tryPick (picker 0) null

        assertNullArg
        <| fun () -> TaskSeq.pickAsync (pickerAsync 0) null

        assertNullArg
        <| fun () -> TaskSeq.tryPickAsync (pickerAsync 0) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-pick on an empty sequence raises KeyNotFoundException`` variant = task {
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.pick (picker 12)
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-pickAsync on an empty sequence raises KeyNotFoundException`` variant = task {
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.pickAsync (pickerAsync 12)
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryPick on an empty sequence returns None`` variant = task {
        let! nothing = Gen.getEmptyVariant variant |> TaskSeq.tryPick (picker 12)

        nothing |> should be None'
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryPickAsync on an empty sequence returns None`` variant = task {
        let! nothing =
            Gen.getEmptyVariant variant
            |> TaskSeq.tryPickAsync (pickerAsync 12)

        nothing |> should be None'
    }

module Immutable =

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pick sad path raises KeyNotFoundException`` variant = task {
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.pick (picker 0) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pickAsync sad path raises KeyNotFoundException`` variant = task {
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.pickAsync (fun x -> task { return if x < 0 then Some x else None }) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pick sad path raises KeyNotFoundException variant`` variant = task {
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.pick (picker 11) // dummy tasks sequence ends at 50
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pickAsync sad path raises KeyNotFoundException variant`` variant = task {
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.pickAsync (pickerAsync 11) // dummy tasks sequence ends at 50
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pick happy path middle of seq`` variant = task {
        let! twentyFive =
            Gen.getSeqImmutable variant
            |> TaskSeq.pick (fun x -> if x < 6 && x > 4 then Some "foo" else None)

        twentyFive |> should equal "foo"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pickAsync happy path middle of seq`` variant = task {
        let! twentyFive =
            Gen.getSeqImmutable variant
            |> TaskSeq.pickAsync (fun x -> task { return if x < 6 && x > 4 then Some "foo" else None })

        twentyFive |> should equal "foo"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pick happy path first item of seq`` variant = task {
        // TODO use variant
        let! first =
            Gen.sideEffectTaskSeqMicro 50L<µs> 1000L<µs> 50
            |> TaskSeq.pick (fun x -> if x = 1 then Some $"first{x}" else None) // dummy tasks seq starts at 1

        first |> should equal "first1"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pickAsync happy path first item of seq`` variant = task {
        let! first =
            Gen.getSeqImmutable variant
            |> TaskSeq.pickAsync (fun x -> task { return if x = 1 then Some $"first{x}" else None }) // dummy tasks seq starts at 1

        first |> should equal "first1"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pick happy path last item of seq`` variant = task {
        let! last =
            Gen.getSeqImmutable variant
            |> TaskSeq.pick (fun x -> if x = 10 then Some $"last{x}" else None) // dummy tasks seq ends at 50

        last |> should equal "last10"
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-pickAsync happy path last item of seq`` variant = task {
        let! last =
            Gen.getSeqImmutable variant
            |> TaskSeq.pickAsync (fun x -> task { return if x = 10 then Some $"last{x}" else None }) // dummy tasks seq ends at 50

        last |> should equal "last10"
    }

    //
    //
    // tryXXX stuff
    //      |
    //      |
    //      V


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPick sad path returns None`` variant = task {
        let! nothing = Gen.getSeqImmutable variant |> TaskSeq.tryPick (picker 0) // dummy tasks sequence starts at 1

        nothing |> should be None'
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPickAsync sad path return None`` variant = task {
        let! nothing =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPickAsync (pickerAsync 0) // dummy tasks sequence starts at 1

        nothing |> should be None'
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPick sad path returns None variant`` variant = task {
        let! nothing =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPick (fun x -> if x >= 11 then Some x else None) // dummy tasks sequence ends at 50 (inverted sign in lambda!)

        nothing |> should be None'
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPickAsync sad path return None - variant`` variant = task {
        let! nothing =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPickAsync (fun x -> task { return if x >= 11 then Some x else None }) // dummy tasks sequence ends at 50

        nothing |> should be None'
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPick happy path middle of seq`` variant = task {
        let! twentyFive =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPick (fun x -> if x < 6 && x > 4 then Some $"foo{x}" else None)

        twentyFive |> should equal (Some "foo5")
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPickAsync happy path middle of seq`` variant = task {
        let! twentyFive =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPickAsync (fun x -> task { return if x < 6 && x > 4 then Some $"foo{x}" else None })

        twentyFive |> should equal (Some "foo5")
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPick happy path first item of seq`` variant = task {
        let! first =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPick (sprintf "foo%i" >> Some) // dummy tasks seq starts at 1

        first |> should equal (Some "foo1")
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPickAsync happy path first item of seq`` variant = task {
        let! first =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPickAsync (fun x -> task { return (sprintf "foo%i" >> Some) x }) // dummy tasks seq starts at 1

        first |> should equal (Some "foo1")
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPick happy path last item of seq`` variant = task {
        let! last =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPick (fun x -> if x = 10 then Some $"foo{x}" else None) // dummy tasks seq ends at 50

        last |> should equal (Some "foo10")
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryPickAsync happy path last item of seq`` variant = task {
        let! last =
            Gen.getSeqImmutable variant
            |> TaskSeq.tryPickAsync (fun x -> task { return if x = 10 then Some $"foo{x}" else None }) // dummy tasks seq ends at 50

        last |> should equal (Some "foo10")
    }


module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-pick KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        // first: error, item is not there
        fun () -> TaskSeq.pick (picker 11) ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

        // pick again: no error, because of side effects
        let! found = TaskSeq.pick (picker 11) ts
        found |> should equal 11

        // pick once more: error, item is not there anymore.
        fun () -> TaskSeq.pick (picker 11) ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-pickAsync KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant

        // first: error, item is not there
        fun () -> TaskSeq.pickAsync (pickerAsync 11) ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

        // pick again: no error, because of side effects
        let! found = TaskSeq.pickAsync (pickerAsync 11) ts
        found |> should equal 11

        // pick once more: error, item is not there anymore.
        fun () -> TaskSeq.pickAsync (pickerAsync 11) ts |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>
    }

    [<Fact>]
    let ``TaskSeq-pick _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.pick (picker 3)
        found |> should equal 3
        i |> should equal 3 // only partial evaluation!

        // pick next item. We do get a new iterator, but mutable state is now starting at '3'
        let! found = ts |> TaskSeq.pick (picker 4)
        found |> should equal 4
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-pickAsync _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.pickAsync (pickerAsync 3)
        found |> should equal 3
        i |> should equal 3 // only partial evaluation!

        // pick next item. We do get a new iterator, but mutable state is now starting at '3'
        let! found = ts |> TaskSeq.pickAsync (pickerAsync 4)
        found |> should equal 4
        i |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-pick _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.pick (picker 42)
        found |> should equal 42
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-pickAsync _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.pickAsync (pickerAsync 42)
        found |> should equal 42
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-pick _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.pick (picker 0)
        found |> should equal 0
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // pick some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.pick (picker 4)
        found |> should equal 4
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-pickAsync _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.pickAsync (pickerAsync 0)
        found |> should equal 0
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // pick some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.pickAsync (pickerAsync 4)
        found |> should equal 4
        i |> should equal 4 // only partial evaluation!
    }


    //
    //
    // tryXXX stuff
    //      |
    //      |
    //      V

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryPick KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let picker x = if x = 11 then Some x else None

        // first: None
        let! found = TaskSeq.tryPick picker ts
        found |> should be None'

        // pick again: found now, because of side effects
        let! found = TaskSeq.tryPick picker ts
        found |> should equal (Some 11)

        // pick once more: None
        let! found = TaskSeq.tryPick picker ts
        found |> should be None'
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-tryPickAsync KeyNotFoundException only sometimes for mutated state`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let picker x = task { return if x = 11 then Some x else None }

        // first: None
        let! found = TaskSeq.tryPickAsync picker ts
        found |> should be None'

        // pick again: found now, because of side effects
        let! found = TaskSeq.tryPickAsync picker ts
        found |> should equal (Some 11)

        // pick once more: None
        let! found = TaskSeq.tryPickAsync picker ts
        found |> should be None'
    }

    [<Fact>]
    let ``TaskSeq-tryPick _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.tryPick (picker 3)
        found |> should equal (Some 3)
        i |> should equal 3 // only partial evaluation!

        // pick next item. We do get a new iterator, but mutable state is now starting at '3'
        let! found = ts |> TaskSeq.tryPick (picker 4)
        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-tryPickAsync _specialcase_ prove we don't read past the found item`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                i <- i + 1
                yield i
        }

        let! found = ts |> TaskSeq.tryPickAsync (pickerAsync 3)
        found |> should equal (Some 3)
        i |> should equal 3 // only partial evaluation!

        // pick next item. We do get a new iterator, but mutable state is now starting at '3'
        let! found = ts |> TaskSeq.tryPickAsync (pickerAsync 4)
        found |> should equal (Some 4)
        i |> should equal 4
    }

    [<Fact>]
    let ``TaskSeq-tryPick _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.tryPick (picker 42)
        found |> should equal (Some 42)
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-tryPickAsync _specialcase_ prove we don't read past the found item v2`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            yield 42
            i <- i + 1
            i <- i + 1
        }

        let! found = ts |> TaskSeq.tryPickAsync (pickerAsync 42)
        found |> should equal (Some 42)
        i |> should equal 0 // because no MoveNext after found item, the last statements are not executed
    }

    [<Fact>]
    let ``TaskSeq-tryPick _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.tryPick (picker 0)
        found |> should equal (Some 0)
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // pick some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.tryPick (picker 4)
        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }

    [<Fact>]
    let ``TaskSeq-tryPickAsync _specialcase_ prove statement after yield is not evaluated`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            for _ in 0..9 do
                yield i
                i <- i + 1
        }

        let! found = ts |> TaskSeq.tryPickAsync (pickerAsync 0)
        found |> should equal (Some 0)
        i |> should equal 0 // notice that it should be one higher if the statement after 'yield' is evaluated

        // pick some next item. We do get a new iterator, but mutable state is now starting at '1'
        let! found = ts |> TaskSeq.tryPickAsync (pickerAsync 4)
        found |> should equal (Some 4)
        i |> should equal 4 // only partial evaluation!
    }
