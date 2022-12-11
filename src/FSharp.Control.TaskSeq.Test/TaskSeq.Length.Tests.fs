module TaskSeq.Tests.Length

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharp.Control

//
// TaskSeq.length
// TaskSeq.lengthBy
// TaskSeq.lengthByAsync
//

module EmptySeq =
    [<Fact>]
    let ``Null source is invalid`` () =
        assertNullArg <| fun () -> TaskSeq.length null

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

module SideSeffects =
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
