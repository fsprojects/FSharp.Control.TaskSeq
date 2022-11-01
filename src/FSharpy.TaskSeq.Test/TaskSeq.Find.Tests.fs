module FSharpy.Tests.Find

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open System.Collections.Generic

module EmptySeq =
    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-find raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.find ((=) 12)
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-findAsync raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getEmptyVariant variant
            |> TaskSeq.findAsync (fun x -> task { return x = 12 })
            |> Task.ignore
        |> should throwAsyncExact typeof<KeyNotFoundException>


    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFind returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFind ((=) 12)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``TaskSeq-tryFindAsync returns None`` variant =
        Gen.getEmptyVariant variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 12 })
        |> Task.map (should be None')

module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.find ((=) 0) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync sad path raises KeyNotFoundException`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findAsync (fun x -> task { return x = 0 }) // dummy tasks sequence starts at 1
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find sad path raises KeyNotFoundException variant`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.find ((=) 11)
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync sad path raises KeyNotFoundException variant`` variant =
        fun () ->
            Gen.getSeqImmutable variant
            |> TaskSeq.findAsync (fun x -> task { return x = 11 })
            |> Task.ignore

        |> should throwAsyncExact typeof<KeyNotFoundException>


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.find (fun x -> x < 6 && x > 4)
        |> Task.map (should equal 5)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findAsync (fun x -> task { return x < 6 && x > 4 })
        |> Task.map (should equal 5)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.find ((=) 1)
        |> Task.map (should equal 1)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findAsync (fun x -> task { return x = 1 })
        |> Task.map (should equal 1)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-find happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.find ((=) 10)
        |> Task.map (should equal 10)

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-findAsync happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.findAsync (fun x -> task { return x = 10 }) // dummy tasks seq ends at 50
        |> Task.map (should equal 10)


    //
    //
    // tryXXX stuff
    //      |
    //      |
    //      V

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind sad path returns None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind ((=) 0)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync sad path return None`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 0 })
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind sad path returns None variant`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind ((<=) 11)
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync sad path return None - variant`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x >= 11 })
        |> Task.map (should be None')

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind (fun x -> x < 6 && x > 4)
        |> Task.map (should equal (Some 5))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync happy path middle of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x < 6 && x > 4 })
        |> Task.map (should equal (Some 5))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind ((=) 1)
        |> Task.map (should equal (Some 1))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync happy path first item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 1 })
        |> Task.map (should equal (Some 1))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFind happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFind ((=) 10)
        |> Task.map (should equal (Some 10))

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-tryFindAsync happy path last item of seq`` variant =
        Gen.getSeqImmutable variant
        |> TaskSeq.tryFindAsync (fun x -> task { return x = 10 })
        |> Task.map (should equal (Some 10))
