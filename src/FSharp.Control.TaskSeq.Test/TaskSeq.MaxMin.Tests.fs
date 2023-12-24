module TaskSeq.Tests.MaxMin

open System

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// TaskSeq.max
// TaskSeq.min
// TaskSeq.maxBy
// TaskSeq.minBy
// TaskSeq.maxByAsync
// TaskSeq.minByAsync
//

type MinMax =
    | Max = 0
    | Min = 1
    | MaxBy = 2
    | MinBy = 3
    | MaxByAsync = 4
    | MinByAsync = 5

module MinMax =
    let getFunction =
        function
        | MinMax.Max -> TaskSeq.max
        | MinMax.Min -> TaskSeq.min
        | MinMax.MaxBy -> TaskSeq.maxBy id
        | MinMax.MinBy -> TaskSeq.minBy id
        | MinMax.MaxByAsync -> TaskSeq.maxByAsync Task.fromResult
        | MinMax.MinByAsync -> TaskSeq.minByAsync Task.fromResult
        | _ -> failwith "impossible"

    let getByFunction =
        function
        | MinMax.MaxBy -> TaskSeq.maxBy
        | MinMax.MinBy -> TaskSeq.minBy
        | MinMax.MaxByAsync -> fun by -> TaskSeq.maxByAsync (by >> Task.fromResult)
        | MinMax.MinByAsync -> fun by -> TaskSeq.minByAsync (by >> Task.fromResult)
        | _ -> failwith "impossible"

    let getAll () =
        [ MinMax.Max; MinMax.Min; MinMax.MaxBy; MinMax.MinBy; MinMax.MaxByAsync; MinMax.MinByAsync ]
        |> List.map getFunction

    let getAllMin () =
        [ MinMax.Min; MinMax.MinBy; MinMax.MinByAsync ]
        |> List.map getFunction

    let getAllMax () =
        [ MinMax.Max; MinMax.MaxBy; MinMax.MaxByAsync ]
        |> List.map getFunction

    let isMin =
        function
        | MinMax.Min
        | MinMax.MinBy
        | MinMax.MinByAsync -> true
        | _ -> false

    let isMax = isMin >> not


type AllMinMaxFunctions() as this =
    inherit TheoryData<MinMax>()

    do
        this.Add MinMax.Max
        this.Add MinMax.Min
        this.Add MinMax.MaxBy
        this.Add MinMax.MinBy
        this.Add MinMax.MaxByAsync
        this.Add MinMax.MinByAsync

type JustMin() as this =
    inherit TheoryData<MinMax>()

    do
        this.Add MinMax.Min
        this.Add MinMax.MinBy
        this.Add MinMax.MinByAsync

type JustMax() as this =
    inherit TheoryData<MinMax>()

    do
        this.Add MinMax.Max
        this.Add MinMax.MaxBy
        this.Add MinMax.MaxByAsync

type JustMinMaxBy() as this =
    inherit TheoryData<MinMax>()

    do
        this.Add MinMax.MaxBy
        this.Add MinMax.MinBy
        this.Add MinMax.MaxByAsync
        this.Add MinMax.MinByAsync

module EmptySeq =
    [<Theory; ClassData(typeof<AllMinMaxFunctions>)>]
    let ``Null source raises ArgumentNullException`` (minMaxType: MinMax) =
        let minMax = MinMax.getFunction minMaxType

        assertNullArg <| fun () -> minMax (null: TaskSeq<int>)

    [<Theory; ClassData(typeof<TestEmptyVariants>)>]
    let ``Empty sequence raises ArgumentException`` variant =
        let test minMax =
            let data = Gen.getEmptyVariant variant

            fun () -> minMax data |> Task.ignore
            |> should throwAsyncExact typeof<ArgumentException>

        for minMax in MinMax.getAll () do
            test minMax

module Functionality =
    [<Fact>]
    let ``TaskSeq-max should return maximum`` () = task {
        let ts = [ 'A' .. 'Z' ] |> TaskSeq.ofList
        let! max = TaskSeq.max ts
        max |> should equal 'Z'
    }

    [<Fact>]
    let ``TaskSeq-maxBy should return maximum of input, not the projection`` () = task {
        let ts = [ 'A' .. 'Z' ] |> TaskSeq.ofList
        let! max = TaskSeq.maxBy id ts
        max |> should equal 'Z'

        let ts = [ 1..10 ] |> TaskSeq.ofList
        let! max = TaskSeq.maxBy (~-) ts
        max |> should equal 1 // as negated, -1 is highest, should not return projection, but original
    }

    [<Fact>]
    let ``TaskSeq-maxByAsync should return maximum of input, not the projection`` () = task {
        let ts = [ 'A' .. 'Z' ] |> TaskSeq.ofList
        let! max = TaskSeq.maxByAsync Task.fromResult ts
        max |> should equal 'Z'

        let ts = [ 1..10 ] |> TaskSeq.ofList
        let! max = TaskSeq.maxByAsync (fun x -> Task.fromResult -x) ts
        max |> should equal 1 // as negated, -1 is highest, should not return projection, but original
    }

    [<Fact>]
    let ``TaskSeq-min should return minimum`` () = task {
        let ts = [ 'A' .. 'Z' ] |> TaskSeq.ofList
        let! min = TaskSeq.min ts
        min |> should equal 'A'
    }

    [<Fact>]
    let ``TaskSeq-minBy should return minimum of input, not the projection`` () = task {
        let ts = [ 'A' .. 'Z' ] |> TaskSeq.ofList
        let! min = TaskSeq.minBy id ts
        min |> should equal 'A'

        let ts = [ 1..10 ] |> TaskSeq.ofList
        let! min = TaskSeq.minBy (~-) ts
        min |> should equal 10 // as negated, -10 is lowest, should not return projection, but original
    }

    [<Fact>]
    let ``TaskSeq-minByAsync should return minimum of input, not the projection`` () = task {
        let ts = [ 'A' .. 'Z' ] |> TaskSeq.ofList
        let! min = TaskSeq.minByAsync Task.fromResult ts
        min |> should equal 'A'

        let ts = [ 1..10 ] |> TaskSeq.ofList
        let! min = TaskSeq.minByAsync (fun x -> Task.fromResult -x) ts
        min |> should equal 10 // as negated, 1 is highest, should not return projection, but original
    }


module Immutable =
    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-max, maxBy, maxByAsync returns maximum`` variant = task {
        let ts = Gen.getSeqImmutable variant

        for max in MinMax.getAllMax () do
            let! max = max ts
            max |> should equal 10
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-min, minBy, minByAsync returns minimum`` variant = task {
        let ts = Gen.getSeqImmutable variant

        for min in MinMax.getAllMin () do
            let! min = min ts
            min |> should equal 1
    }

    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-maxBy, maxByAsync returns maximum after projection`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! max = ts |> TaskSeq.maxBy (fun x -> -x)
        max |> should equal 1 // because -1 maps to item '1'
    }


    [<Theory; ClassData(typeof<TestImmTaskSeq>)>]
    let ``TaskSeq-minBy, minByAsync returns minimum after projection`` variant = task {
        let ts = Gen.getSeqImmutable variant
        let! min = ts |> TaskSeq.minBy (fun x -> -x)
        min |> should equal 10 // because -10 maps to item 10
    }

module SideSeffects =
    [<Fact>]
    let ``TaskSeq-max, maxBy, maxByAsync prove we execute after-effects`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield i // 2
            i <- i + 1
            yield i // 3
            yield i + 1 // 4
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.max |> Task.map (should equal 4)
        do! ts |> TaskSeq.maxBy (~-) |> Task.map (should equal 6) // next iteration & negation "-6" maps to "6"

        do!
            ts
            |> TaskSeq.maxByAsync Task.fromResult
            |> Task.map (should equal 12) // no negation

        i |> should equal 12
    }

    [<Fact>]
    let ``TaskSeq-min, minBy, minByAsync prove we execute after-effects test`` () = task {
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 1
            i <- i + 1
            yield i // 2
            i <- i + 1
            yield i // 3
            yield i + 1 // 4
            i <- i + 1 // we should get here
        }

        do! ts |> TaskSeq.min |> Task.map (should equal 2)
        do! ts |> TaskSeq.minBy (~-) |> Task.map (should equal 8) // next iteration & negation

        do!
            ts
            |> TaskSeq.minByAsync Task.fromResult
            |> Task.map (should equal 10) // no negation

        i |> should equal 12
    }


    [<Theory; ClassData(typeof<JustMax>)>]
    let ``TaskSeq-max with sequence that changes length`` (minMax: MinMax) = task {
        let max = MinMax.getFunction minMax
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 10
            yield! [ 1..i ]
        }

        do! max ts |> Task.map (should equal 10)
        do! max ts |> Task.map (should equal 20) // mutable state dangers!!
        do! max ts |> Task.map (should equal 30) // id
    }

    [<Theory; ClassData(typeof<JustMin>)>]
    let ``TaskSeq-min with sequence that changes length`` (minMax: MinMax) = task {
        let min = MinMax.getFunction minMax
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 10
            yield! [ 1..i ]
        }

        do! min ts |> Task.map (should equal 1)
        do! min ts |> Task.map (should equal 1) // same min after changing state
        do! min ts |> Task.map (should equal 1) // id
    }

    [<Theory; ClassData(typeof<JustMinMaxBy>)>]
    let ``TaskSeq-minBy, maxBy with sequence that changes length`` (minMax: MinMax) =
        let mutable i = 0

        let ts = taskSeq {
            i <- i + 10
            yield! [ 1..i ]
        }

        let test minMaxFn v =
            if MinMax.isMin minMax then
                // this ensures the "min" version behaves like the "max" version
                minMaxFn (~-) ts |> Task.map (should equal v)
            else
                minMaxFn id ts |> Task.map (should equal v)

        task {
            do! test (MinMax.getByFunction minMax) 10
            do! test (MinMax.getByFunction minMax) 20
            do! test (MinMax.getByFunction minMax) 30
        }
