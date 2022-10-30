module FSharpy.Tests.Last

open System
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy


[<Theory; ClassData(typeof<TestEmptyVariants>)>]
let ``TaskSeq-last throws on empty sequences`` variant = task {
    fun () -> getEmptyVariant variant |> TaskSeq.last |> Task.ignore
    |> should throwAsyncExact typeof<ArgumentException>
}

[<Theory; ClassData(typeof<TestEmptyVariants>)>]
let ``TaskSeq-tryLast returns None on empty sequences`` variant = task {
    let! nothing = getEmptyVariant variant |> TaskSeq.tryLast
    nothing |> should be None'
}

[<Theory; ClassData(typeof<TestSmallVariants>)>]
let ``TaskSeq-last gets the last item in a longer sequence`` variant = task {
    let! last = getSmallVariant variant |> TaskSeq.last
    last |> should equal 10
}

[<Fact>]
let ``TaskSeq-last gets the only item in a singleton sequence`` () = task {
    let! last = taskSeq { yield 42 } |> TaskSeq.last
    last |> should equal 42
}

[<Theory; ClassData(typeof<TestSmallVariants>)>]
let ``TaskSeq-tryLast gets the last item in a longer sequence`` variant = task {
    let! last = getSmallVariant variant |> TaskSeq.tryLast

    last |> should be Some'
    last |> should equal (Some 10)
}

[<Fact>]
let ``TaskSeq-tryLast gets the only item in a singleton sequence`` () = task {
    let! last = taskSeq { yield 10 } |> TaskSeq.tryLast
    last |> should be Some'
    last |> should equal (Some 10)
}
