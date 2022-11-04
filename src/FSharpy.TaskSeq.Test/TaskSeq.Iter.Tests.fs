module TaskSeq.Tests.Iter

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.iter
// TaskSeq.iteri
// TaskSeq.iterAsync
// TaskSeq.iteriAsync
//

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
    [<Fact>]
    let ``TaskSeq-iter prove we execute empty-seq side-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iter (fun _ -> ())
        do! ts |> TaskSeq.iter (fun _ -> ())
        do! ts |> TaskSeq.iter (fun _ -> ())
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-iteri prove we execute empty-seq side-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-iterAsync prove we execute empty-seq side-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iterAsync (fun _ -> task { return () })
        do! ts |> TaskSeq.iterAsync (fun _ -> task { return () })
        do! ts |> TaskSeq.iterAsync (fun _ -> task { return () })
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-iteriAsync prove we execute empty-seq side-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iteriAsync (fun _ _ -> task { return () })
        do! ts |> TaskSeq.iteriAsync (fun _ _ -> task { return () })
        do! ts |> TaskSeq.iteriAsync (fun _ _ -> task { return () })
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-iter prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iter (fun _ -> ())
        do! ts |> TaskSeq.iter (fun _ -> ())
        do! ts |> TaskSeq.iter (fun _ -> ())
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-iteri prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-iterAsync prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iterAsync (fun _ -> task { return () })
        do! ts |> TaskSeq.iterAsync (fun _ -> task { return () })
        do! ts |> TaskSeq.iterAsync (fun _ -> task { return () })
        i |> should equal 9
    }

    [<Fact>]
    let ``TaskSeq-iteriAsync prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield 42
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.iteriAsync (fun _ _ -> task { return () })
        do! ts |> TaskSeq.iteriAsync (fun _ _ -> task { return () })
        do! ts |> TaskSeq.iteriAsync (fun _ _ -> task { return () })
        i |> should equal 9
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iter should go over all items`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        do! ts |> TaskSeq.iter (fun _ -> ())
        do! ts |> TaskSeq.iter (fun _ -> ())
        do! ts |> TaskSeq.iter (fun _ -> ())
        // incl. the iteration of 'last', we reach 40
        do! ts |> TaskSeq.last |> Task.map (should equal 40)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iteri show that side effects are executed multiple times`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        do! ts |> TaskSeq.iteri (fun _ _ -> ())
        // incl. the iteration of 'last', we reach 40
        do! ts |> TaskSeq.last |> Task.map (should equal 40)
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iter multiple iterations over same sequence`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let mutable sum = 0

        do! TaskSeq.iter (fun item -> sum <- sum + item) ts
        do! TaskSeq.iter (fun item -> sum <- sum + item) ts
        do! TaskSeq.iter (fun item -> sum <- sum + item) ts
        do! TaskSeq.iter (fun item -> sum <- sum + item) ts

        sum |> should equal 820 // side-effected tasks, so 'item' DOES CHANGE, each next iteration starts 10 higher
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iteriAsync should go over all items`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let mutable sum = 0

        do!
            ts
            |> TaskSeq.iteriAsync (fun i _ -> task { sum <- sum + i })

        sum |> should equal 45 // index starts at 0
    }

    [<Theory; ClassData(typeof<TestSideEffectTaskSeq>)>]
    let ``TaskSeq-iterAsync should go over all items`` variant = task {
        let ts = Gen.getSeqWithSideEffect variant
        let mutable sum = 0
        do! TaskSeq.iterAsync (fun item -> task { sum <- sum + item }) ts
        sum |> should equal 55 // task-dummies started at 1
    }
