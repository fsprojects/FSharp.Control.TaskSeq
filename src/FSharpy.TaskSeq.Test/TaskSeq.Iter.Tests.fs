module FSharpy.Tests.Iter

open Xunit
open FsUnit.Xunit

open FSharpy

[<Fact>]
let ``TaskSeq-iteri does nothing on empty sequences`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = -1
    do! TaskSeq.empty |> TaskSeq.iteri (fun i _ -> sum <- sum + i)
    sum |> should equal -1
}

[<Fact>]
let ``TaskSeq-iter does nothing on empty sequences`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = -1
    do! TaskSeq.empty |> TaskSeq.iter (fun i -> sum <- sum + i)
    sum |> should equal -1
}

[<Fact>]
let ``TaskSeq-iteri should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0
    do! tq |> TaskSeq.iteri (fun i _ -> sum <- sum + i)
    sum |> should equal 45 // index starts at 0
}

[<Fact>]
let ``TaskSeq-iter should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0
    do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
    sum |> should equal 55 // task-dummies started at 1
}


[<Fact>]
let ``TaskSeq-iter multiple iterations over same sequence`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0
    do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
    do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
    do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
    do! tq |> TaskSeq.iter (fun item -> sum <- sum + item)
    sum |> should equal 220 // task-dummies started at 1
}

[<Fact>]
let ``TaskSeq-iteriAsync should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0

    do!
        tq
        |> TaskSeq.iteriAsync (fun i _ -> task { sum <- sum + i })

    sum |> should equal 45 // index starts at 0
}

[<Fact>]
let ``TaskSeq-iterAsync should go over all items`` () = task {
    let tq = createDummyTaskSeq 10
    let mutable sum = 0

    do!
        tq
        |> TaskSeq.iterAsync (fun item -> task { sum <- sum + item })

    sum |> should equal 55 // task-dummies started at 1
}
