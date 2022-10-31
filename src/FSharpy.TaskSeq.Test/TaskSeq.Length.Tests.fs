module FSharpy.Tests.Length

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy

module EmptySeq =
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
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-length returns proper length - side-effect`` variant = task {
        let! len = Gen.getSeqWithSideEffect variant |> TaskSeq.length
        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthBy returns proper length - side-effect`` variant = task {
        let! len =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.lengthBy (fun _ -> true)

        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length - side-effect`` variant = task {
        let! len =
            Gen.getSeqWithSideEffect variant
            |> TaskSeq.lengthByAsync (Task.apply (fun _ -> true))

        len |> should equal 10
    }


    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthBy returns proper length when filtering - side-effect`` variant = task {
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
