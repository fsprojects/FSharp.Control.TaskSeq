module FSharpy.Tests.Iter

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Fact(Timeout = 10_000)>]
let ``TaskSeq-iteri should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0
    do! tq |> TaskSeq.iteri (fun i _ -> sum <- sum + i)
    sum |> should equal 45 // index starts at 0
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-iter should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0
    do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
    sum |> should equal 55 // task-dummies started at 1
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-iteriAsync should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0

    do!
        tq
        |> TaskSeq.iteriAsync (fun i _ -> task { sum <- sum + i })

    sum |> should equal 45 // index starts at 0
}

[<Fact(Timeout = 10_000)>]
let ``TaskSeq-iterAsync should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0

    do!
        tq
        |> TaskSeq.iterAsync (fun item -> task { sum <- sum + item })

    sum |> should equal 55 // task-dummies started at 1
}
