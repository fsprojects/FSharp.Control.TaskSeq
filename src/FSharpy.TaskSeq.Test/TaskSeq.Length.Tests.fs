module FSharpy.Tests.Length

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


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

[<Theory; ClassData(typeof<TestImmTaskSeq>)>]
let ``TaskSeq-length returns proper length`` variant = task {
    let! len = Gen.getSeqImmutable variant |> TaskSeq.length
    len |> should equal 10
}

[<Theory; ClassData(typeof<TestImmTaskSeq>)>]
let ``TaskSeq-lengthBy returns proper length`` variant = task {
    let! len =
        Gen.getSeqImmutable variant
        |> TaskSeq.lengthBy (fun _ -> true)

    len |> should equal 10
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
