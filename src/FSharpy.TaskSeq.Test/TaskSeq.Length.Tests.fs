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

        do!
            TaskSeq.lengthBy (fun _ -> true) ts
            |> Task.map (should equal 10)

        do!
            TaskSeq.lengthBy (fun _ -> true) ts
            |> Task.map (should equal 10) // twice is fine
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length`` variant = task {
        let! len =
            Gen.getSeqImmutable variant
            |> TaskSeq.lengthByAsync (Task.apply (fun _ -> true))

        len |> should equal 10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthBy returns proper length when filtering`` variant = task {
        let run f = Gen.getSeqImmutable variant |> TaskSeq.lengthBy f
        let! len = run (fun x -> x % 3 = 0)
        len |> should equal 3 // [3; 6; 9]
        let! len = run (fun x -> x % 3 = 1)
        len |> should equal 4 // [1; 4; 7; 10]
        let! len = run (fun x -> x % 3 = 2)
        len |> should equal 3 // [2; 5; 8]
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length when filtering`` variant = task {
        let run f =
            Gen.getSeqImmutable variant
            |> TaskSeq.lengthByAsync (Task.apply f)

        let! len = run (fun x -> x % 3 = 0)
        len |> should equal 3 // [3; 6; 9]
        let! len = run (fun x -> x % 3 = 1)
        len |> should equal 4 // [1; 4; 7; 10]
        let! len = run (fun x -> x % 3 = 2)
        len |> should equal 3 // [2; 5; 8]
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
        let! len = run (fun x -> x % 3 = 0)
        len |> should equal 3 // [3; 6; 9]
        let! len = run (fun x -> x % 3 = 1)
        len |> should equal 4 // [1; 4; 7; 10]
        let! len = run (fun x -> x % 3 = 2)
        len |> should equal 3 // [2; 5; 8]
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-lengthByAsync returns proper length when filtering - side-effect`` variant = task {
        let run f =
            Gen.getSeqImmutable variant
            |> TaskSeq.lengthByAsync (Task.apply f)

        let! len = run (fun x -> x % 3 = 0)
        len |> should equal 3 // [3; 6; 9]
        let! len = run (fun x -> x % 3 = 1)
        len |> should equal 4 // [1; 4; 7; 10]
        let! len = run (fun x -> x % 3 = 2)
        len |> should equal 3 // [2; 5; 8]
    }
