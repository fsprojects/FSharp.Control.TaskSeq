module FSharpy.Tests.Iter

open Xunit
open FsUnit.Xunit

open FSharpy

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iteri does nothing on empty sequences`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable sum = -1
        do! tq |> TaskSeq.iteri (fun i _ -> sum <- sum + i)
        sum |> should equal -1
    }

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-iter does nothing on empty sequences`` variant = task {
        let tq = Gen.getEmptyVariant variant
        let mutable sum = -1
        do! tq |> TaskSeq.iter (fun i -> sum <- sum + i)
        sum |> should equal -1
    }

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iteri should go over all items`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable sum = 0
        do! tq |> TaskSeq.iteri (fun i _ -> sum <- sum + i)
        sum |> should equal 45 // index starts at 0
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iter should go over all items`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable sum = 0
        do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
        sum |> should equal 55 // task-dummies started at 1
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iter multiple iterations over same sequence`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable sum = 0
        do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
        do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
        do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
        do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
        sum |> should equal 220 // immutable tasks, so 'item' does not change, just 4 x 55
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iteriAsync should go over all items`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable sum = 0

        do!
            tq
            |> TaskSeq.iteriAsync (fun i _ -> task { sum <- sum + i })

        sum |> should equal 45 // index starts at 0
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-iterAsync should go over all items`` variant = task {
        let tq = Gen.getSeqImmutable variant
        let mutable sum = 0

        do!
            tq
            |> TaskSeq.iterAsync (fun item -> task { sum <- sum + item })

        sum |> should equal 55 // task-dummies started at 1
    }

module SideEffects =
    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iteri should go over all items`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let mutable sum = 0
        do! tq |> TaskSeq.iteri (fun i _ -> sum <- sum + i)
        sum |> should equal 45 // index starts at 0
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iter should go over all items`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let mutable sum = 0
        do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
        sum |> should equal 55 // task-dummies started at 1
    }


    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iter multiple iterations over same sequence`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let mutable sum = 0
        do! tq |> TaskSeq.iter (fun item -> printfn "A:%i" item; sum <- sum + item)
        do! tq |> TaskSeq.iter (fun item -> printfn "B:%i" item; sum <- sum + item)
        do! tq |> TaskSeq.iter (fun item -> printfn "C:%i" item; sum <- sum + item)
        do! tq |> TaskSeq.iter (fun item -> printfn "D:%i" item; sum <- sum + item)
        sum |> should equal 820 // side-effected tasks, so 'item' DOES CHANGE, each next iteration starts 10 higher
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iteriAsync should go over all items`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let mutable sum = 0

        do!
            tq
            |> TaskSeq.iteriAsync (fun i _ -> task { sum <- sum + i })

        sum |> should equal 45 // index starts at 0
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iterAsync should go over all items`` variant = task {
        let tq = Gen.getSeqWithSideEffect variant
        let mutable sum = 0

        do!
            tq
            |> TaskSeq.iterAsync (fun item -> task { sum <- sum + item })

        sum |> should equal 55 // task-dummies started at 1
    }
