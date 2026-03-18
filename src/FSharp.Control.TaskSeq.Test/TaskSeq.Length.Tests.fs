module TaskSeq.Tests.Length

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.length
// TaskSeq.lengthOrMax
// TaskSeq.lengthBy
// TaskSeq.lengthByAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.length null
        assertNullArg <| fun () -> TaskSeq.lengthOrMax 10 null

        assertNullArg
        <| fun () -> TaskSeq.lengthBy (fun _ -> false) null

        assertNullArg
        <| fun () -> TaskSeq.lengthByAsync (fun _ -> Task.fromResult false) null

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-length returns zero on empty sequences`` variant = task {
        let! len = Gen.getEmptyVariant variant |> TaskSeq.length
        len |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-lengthBy returns zero on empty sequences`` variant = task {
        let! len =
            Gen.getEmptyVariant variant
            |> TaskSeq.lengthBy (fun _ -> true)

        len |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-lengthByAsync returns zero on empty sequences`` variant = task {
        let! len =
            Gen.getEmptyVariant variant
            |> TaskSeq.lengthByAsync (Task.apply (fun _ -> true))

        len |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-lengthOrMax on empty sequence returns 0 regardless of max`` variant = task {
        let! len = Gen.getEmptyVariant variant |> TaskSeq.lengthOrMax 100
        len |> should equal 0
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-lengthOrMax on empty sequence with max=0 returns 0`` variant = task {
        let! len = Gen.getEmptyVariant variant |> TaskSeq.lengthOrMax 0
        len |> should equal 0
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-length returns proper length`` variant = task {
        let ts = Gen.getSeqImmutable variant
        do! TaskSeq.length ts |> Task.map (should equal 10)
        do! TaskSeq.length ts |> Task.map (should equal 10) // twice is fine
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthBy returns proper length`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let run () = TaskSeq.lengthBy (fun _ -> true) ts
        do! run () |> Task.map (should equal 10)
        do! run () |> Task.map (should equal 10) // twice is fine
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let run () = TaskSeq.lengthByAsync (Task.apply (fun _ -> true)) ts
        do! run () |> Task.map (should equal 10)
        do! run () |> Task.map (should equal 10) // twice is fine
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthBy returns proper length when filtering`` variant = task {
        let run f = Gen.getSeqImmutable variant |> TaskSeq.lengthBy f
        do! run (fun x -> x % 3 = 0) |> Task.map (should equal 3) // [3; 6; 9]
        do! run (fun x -> x % 3 = 1) |> Task.map (should equal 4) // [1; 4; 7; 10]
        do! run (fun x -> x % 3 = 2) |> Task.map (should equal 3) // [2; 5; 8]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length when filtering`` variant = task {
        let run f =
            Gen.getSeqImmutable variant
            |> TaskSeq.lengthByAsync (Task.apply f)

        do! run (fun x -> x % 3 = 0) |> Task.map (should equal 3) // [3; 6; 9]
        do! run (fun x -> x % 3 = 1) |> Task.map (should equal 4) // [1; 4; 7; 10]
        do! run (fun x -> x % 3 = 2) |> Task.map (should equal 3) // [2; 5; 8]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthOrMax returns actual length when sequence is shorter than max`` variant = task {
        // source has 10 items; max=100 → actual length 10 is returned
        let! len = Gen.getSeqImmutable variant |> TaskSeq.lengthOrMax 100
        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthOrMax returns max when sequence is longer than max`` variant = task {
        // source has 10 items; max=5 → capped at 5
        let! len = Gen.getSeqImmutable variant |> TaskSeq.lengthOrMax 5
        len |> should equal 5
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthOrMax returns max when sequence is exactly max`` variant = task {
        // source has 10 items; max=10 → returns 10
        let! len = Gen.getSeqImmutable variant |> TaskSeq.lengthOrMax 10
        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthOrMax with max=1 returns 1 for any non-empty sequence`` variant = task {
        let! len = Gen.getSeqImmutable variant |> TaskSeq.lengthOrMax 1
        len |> should equal 1
    }

    [<Fact>]
    let ``TaskSeq-lengthOrMax with max=0 always returns 0 regardless of source`` () = task {
        // max=0: the while loop condition (i < max) is false from the start → 0 returned
        // NOTE: the implementation still calls MoveNextAsync once before the loop
        let! len = TaskSeq.ofList [ 1..100 ] |> TaskSeq.lengthOrMax 0
        len |> should equal 0
    }

module SideEffects =
    [<Fact>]
    let ``TaskSeq-length prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.length |> Task.ignore
        do! ts |> TaskSeq.length |> Task.ignore
        do! ts |> TaskSeq.length |> Task.ignore
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-lengthBy prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.lengthBy (fun _ -> true) |> Task.ignore
        do! ts |> TaskSeq.lengthBy (fun _ -> true) |> Task.ignore
        do! ts |> TaskSeq.lengthBy (fun _ -> true) |> Task.ignore
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-lengthByAsync prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        let lenBy =
            TaskSeq.lengthByAsync (fun _ -> task { return true })
            >> Task.ignore

        do! lenBy ts
        do! lenBy ts
        do! lenBy ts

        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-length with sequence that changes length`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 10
            yield! [ 1..i ]
        }

        do! TaskSeq.length ts |> Task.map (should equal 10)
        do! TaskSeq.length ts |> Task.map (should equal 20) // mutable state dangers!!
        do! TaskSeq.length ts |> Task.map (should equal 30) // id
        do! TaskSeq.length ts |> Task.map (should equal 40) // id
        do! TaskSeq.length ts |> Task.map (should equal 50) // id
    }

    [<Fact>]
    let ``TaskSeq-lengthBy with sequence that changes length`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 10
            yield! [ 1..i ]
        }

        do! TaskSeq.lengthBy ((<) 10) ts |> Task.map (should equal 0)
        do! TaskSeq.lengthBy ((<) 20) ts |> Task.map (should equal 0) // mutable state dangers!!
        do! TaskSeq.lengthBy ((<) 30) ts |> Task.map (should equal 0) // id
        do! TaskSeq.lengthBy ((<) 10) ts |> Task.map (should equal 30) // id
        do! TaskSeq.lengthBy ((<) 10) ts |> Task.map (should equal 40) // id
    }

    [<Fact>]
    let ``TaskSeq-lengthByAsync with sequence that changes length`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 10
            yield! [ 1..i ]
        }

        let notBefore x = TaskSeq.lengthByAsync (Task.apply ((<) x)) ts
        do! notBefore 10 |> Task.map (should equal 0)
        do! notBefore 20 |> Task.map (should equal 0) // mutable state dangers!!
        do! notBefore 30 |> Task.map (should equal 0) // id
        do! notBefore 10 |> Task.map (should equal 30) // id
        do! notBefore 10 |> Task.map (should equal 40) // id
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-length returns proper length`` variant = task {
        let! len = Gen.getSeqWithSideEffect variant |> TaskSeq.length
        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthBy returns proper length`` variant = task {
        let! len =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.lengthBy (fun _ -> true)

        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length`` variant = task {
        let! len =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.lengthByAsync (Task.apply (fun _ -> true))

        len |> should equal 10
    }


    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthBy returns proper length when filtering`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let run f = ts |> TaskSeq.lengthBy f

        do! run (fun x -> x % 3 = 0) |> Task.map (should equal 3) // [3; 6; 9]
        do! run (fun x -> x % 3 = 1) |> Task.map (should equal 3) // [13; 16; 19]  // because of side-effect run again!
        do! run (fun x -> x % 3 = 2) |> Task.map (should equal 3) // [23; 26; 29]  // id
        do! run (fun x -> x % 3 = 1) |> Task.map (should equal 4) // [31; 34; 37; 40]  // id
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length when filtering - side-effect`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let run f = ts |> TaskSeq.lengthByAsync (Task.apply f)

        do! run (fun x -> x % 3 = 0) |> Task.map (should equal 3) // [3; 6; 9]
        do! run (fun x -> x % 3 = 1) |> Task.map (should equal 3) // [13; 16; 19]  // because of side-effect run again!
        do! run (fun x -> x % 3 = 2) |> Task.map (should equal 3) // [23; 26; 29]  // id
        do! run (fun x -> x % 3 = 1) |> Task.map (should equal 4) // [31; 34; 37; 40]  // id
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthOrMax returns correct length when below max`` variant = task {
        // side-effect sequence yields 10 items on first run
        let! len = Gen.getSeqWithSideEffect variant |> TaskSeq.lengthOrMax 100
        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthOrMax returns max and stops evaluation when sequence exceeds max`` variant = task {
        // source has 10 items; max=5 → should stop early after exactly 5 elements
        let mutable evaluated = 0

        let ts = taskSeq {
            for item in Gen.getSeqWithSideEffect variant do
                evaluated <- evaluated + 1
                yield item
        }

        let! len = ts |> TaskSeq.lengthOrMax 5
        len |> should equal 5
        // exactly max elements are pulled from the source
        evaluated |> should equal 5
    }

    [<Fact>]
    let ``TaskSeq-lengthOrMax stops evaluating source after reaching max`` () = task {
        let mutable sideEffects = 0

        let ts = taskSeq {
            for i in 1..100 do
                sideEffects <- sideEffects + 1
                yield i
        }

        let! len = ts |> TaskSeq.lengthOrMax 7
        len |> should equal 7
        // exactly max elements are evaluated
        sideEffects |> should equal 7
    }

    [<Fact>]
    let ``TaskSeq-lengthOrMax with max=0 evaluates zero elements`` () = task {
        let mutable sideEffects = 0

        let ts = taskSeq {
            sideEffects <- sideEffects + 1
            yield 1
            sideEffects <- sideEffects + 1
            yield 2
        }

        let! len = ts |> TaskSeq.lengthOrMax 0
        len |> should equal 0
        // no elements evaluated when max=0
        sideEffects |> should equal 0
    }
