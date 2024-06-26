namespace FSharp.Control

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

[<Struct>]
type internal AsyncEnumStatus =
    | BeforeAll
    | WithCurrent
    | AfterAll

[<Struct>]
type internal TakeOrSkipKind =
    /// use the Seq.take semantics, raises exception if not enough elements
    | Take
    /// use the Seq.skip semantics, raises exception if not enough elements
    | Skip
    /// use the Seq.truncate semantics, safe operation, returns all if count exceeds the seq
    | Truncate
    /// no Seq equiv, but like Stream.drop in Scala: safe operation, return empty if not enough elements
    | Drop

[<Struct>]
type internal Action<'T, 'U, 'TaskU when 'TaskU :> Task<'U>> =
    | CountableAction of countable_action: (int -> 'T -> 'U)
    | SimpleAction of simple_action: ('T -> 'U)
    | AsyncCountableAction of async_countable_action: (int -> 'T -> 'TaskU)
    | AsyncSimpleAction of async_simple_action: ('T -> 'TaskU)

[<Struct>]
type internal FolderAction<'T, 'State, 'TaskState when 'TaskState :> Task<'State>> =
    | FolderAction of state_action: ('State -> 'T -> 'State)
    | AsyncFolderAction of async_state_action: ('State -> 'T -> 'TaskState)

[<Struct>]
type internal ChooserAction<'T, 'U, 'TaskOption when 'TaskOption :> Task<'U option>> =
    | TryPick of try_pick: ('T -> 'U option)
    | TryPickAsync of async_try_pick: ('T -> 'TaskOption)

[<Struct>]
type internal PredicateAction<'T, 'TaskBool when 'TaskBool :> Task<bool>> =
    | Predicate of try_filter: ('T -> bool)
    | PredicateAsync of async_try_filter: ('T -> 'TaskBool)

[<Struct>]
type internal InitAction<'T, 'TaskT when 'TaskT :> Task<'T>> =
    | InitAction of init_item: (int -> 'T)
    | InitActionAsync of async_init_item: (int -> 'TaskT)

[<Struct>]
type internal ManyOrOne<'T> =
    | Many of source_seq: TaskSeq<'T>
    | One of source_item: 'T

module internal TaskSeqInternal =
    /// Raise an NRE for arguments that are null. Only used for 'source' parameters, never for function parameters.
    let inline checkNonNull argName arg =
        if isNull arg then
            nullArg argName

    let inline raiseEmptySeq () = invalidArg "source" "The input task sequence was empty."

    /// Moves the enumerator to its first element, assuming it has just been allocated.
    /// Raises "The input sequence was empty" if there was no first element.
    let inline moveFirstOrRaiseUnsafe (e: IAsyncEnumerator<_>) = task {
        let! hasFirst = e.MoveNextAsync()

        if not hasFirst then
            invalidArg "source" "The input task sequence was empty."
    }

    /// Tests the given integer value and raises if it is -1 or lower.
    let inline raiseCannotBeNegative name value =
        if value >= 0 then
            ()
        else
            invalidArg name $"The value must be non-negative, but was {value}."

    let inline raiseOutOfBounds name =
        invalidArg name "The value or index must be within the bounds of the task sequence."

    let inline raiseInsufficient () =
        // this is correct, it is NOT an InvalidOperationException (see Seq.fs in F# Core)
        // but instead, it's an ArgumentException... FWIW lol
        invalidArg "source" "The input task sequence was has an insufficient number of elements."

    let inline raiseNotFound () =
        KeyNotFoundException("The predicate function or index did not satisfy any item in the task sequence.")
        |> raise

    let isEmpty (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! step = e.MoveNextAsync()
            return not step
        }

    let empty<'T> =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator _ =
                { new IAsyncEnumerator<'T> with
                    member _.MoveNextAsync() = ValueTask.False
                    member _.Current = Unchecked.defaultof<'T>
                    member _.DisposeAsync() = ValueTask.CompletedTask
                }
        }

    let singleton (value: 'T) =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator _ =
                let mutable status = BeforeAll

                { new IAsyncEnumerator<'T> with
                    member _.MoveNextAsync() =
                        match status with
                        | BeforeAll ->
                            status <- WithCurrent
                            ValueTask.True
                        | WithCurrent ->
                            status <- AfterAll
                            ValueTask.False
                        | AfterAll -> ValueTask.False

                    member _.Current: 'T =
                        match status with
                        | WithCurrent -> value
                        | _ -> Unchecked.defaultof<'T>

                    member _.DisposeAsync() = ValueTask.CompletedTask
                }
        }

    /// Returns length unconditionally, or based on a predicate
    let lengthBy predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {

            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable i = 0
            let! step = e.MoveNextAsync()
            go <- step

            match predicate with
            | None ->
                while go do
                    let! step = e.MoveNextAsync()
                    i <- i + 1 // update before moving: we are counting, not indexing
                    go <- step

            | Some(Predicate predicate) ->
                while go do
                    if predicate e.Current then
                        i <- i + 1

                    let! step = e.MoveNextAsync()
                    go <- step

            | Some(PredicateAsync predicate) ->
                while go do
                    match! predicate e.Current with
                    | true -> i <- i + 1
                    | false -> ()

                    let! step = e.MoveNextAsync()
                    go <- step

            return i
        }

    /// Returns length unconditionally, or based on a predicate
    let lengthBeforeMax max (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable i = 0
            let! step = e.MoveNextAsync()
            go <- step

            while go && i < max do
                i <- i + 1 // update before moving: we are counting, not indexing
                let! step = e.MoveNextAsync()
                go <- step

            return i
        }

    let inline maxMin ([<InlineIfLambda>] maxOrMin) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            do! moveFirstOrRaiseUnsafe e

            let mutable acc = e.Current

            while! e.MoveNextAsync() do
                acc <- maxOrMin e.Current acc

            return acc
        }

    // 'compare' is either `<` or `>` (i.e, less-than, greater-than resp.)
    let inline maxMinBy ([<InlineIfLambda>] compare) ([<InlineIfLambda>] projection) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            do! moveFirstOrRaiseUnsafe e

            let value = e.Current
            let mutable accProjection = projection value
            let mutable accValue = value

            while! e.MoveNextAsync() do
                let value = e.Current
                let currentProjection = projection value

                if compare accProjection currentProjection then
                    accProjection <- currentProjection
                    accValue <- value

            return accValue
        }

    // 'compare' is either `<` or `>` (i.e, less-than, greater-than resp.)
    let inline maxMinByAsync ([<InlineIfLambda>] compare) ([<InlineIfLambda>] projectionAsync) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            do! moveFirstOrRaiseUnsafe e

            let value = e.Current
            let! projValue = projectionAsync value
            let mutable accProjection = projValue
            let mutable accValue = value

            while! e.MoveNextAsync() do
                let value = e.Current
                let! currentProjection = projectionAsync value

                if compare accProjection currentProjection then
                    accProjection <- currentProjection
                    accValue <- value

            return accValue
        }

    let tryExactlyOne (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            match! e.MoveNextAsync() with
            | true ->
                // grab first item and test if there's a second item
                let current = e.Current

                match! e.MoveNextAsync() with
                | true -> return None // 2 or more items
                | false -> return Some current // exactly one

            | false ->
                // zero items
                return None
        }


    let init count initializer = taskSeq {
        let mutable i = 0
        let mutable value: Lazy<'T> = Unchecked.defaultof<_>

        let count =
            match count with
            | Some c ->
                raiseCannotBeNegative (nameof count) c
                c

            | None -> Int32.MaxValue

        match initializer with
        | InitAction init ->
            while i < count do
                // using Lazy gives us locking and safe multiple access to the cached value, if
                // multiple threads access the same item through the same enumerator (which is
                // bad practice, but hey, who're we to judge).
                if isNull value then
                    value <- Lazy<_>.Create(fun () -> init i)

                yield value.Force()
                value <- Unchecked.defaultof<_>
                i <- i + 1

        | InitActionAsync asyncInit ->
            while i < count do
                // using Lazy gives us locking and safe multiple access to the cached value, if
                // multiple threads access the same item through the same enumerator (which is
                // bad practice, but hey, who're we to judge).
                if isNull value then
                    // TODO: is there a 'Lazy' we can use with Task?
                    let! value' = asyncInit i
                    value <- Lazy<_>.CreateFromValue value'

                yield value.Force()
                value <- Unchecked.defaultof<_>
                i <- i + 1

    }

    let iter action (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step = e.MoveNextAsync()
            go <- step

            // this ensures that the inner loop is optimized for the closure
            // though perhaps we need to split into individual functions after all to use
            // InlineIfLambda?
            match action with
            | CountableAction action ->
                let mutable i = 0

                while go do
                    do action i e.Current
                    let! step = e.MoveNextAsync()
                    i <- i + 1
                    go <- step

            | SimpleAction action ->
                while go do
                    do action e.Current
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncCountableAction action ->
                let mutable i = 0

                while go do
                    do! action i e.Current
                    let! step = e.MoveNextAsync()
                    i <- i + 1
                    go <- step

            | AsyncSimpleAction action ->
                while go do
                    do! action e.Current
                    let! step = e.MoveNextAsync()
                    go <- step
        }

    let fold folder initial (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable result = initial
            let! step = e.MoveNextAsync()
            go <- step

            match folder with
            | FolderAction folder ->
                while go do
                    result <- folder result e.Current
                    let! step = e.MoveNextAsync()
                    go <- step

            | AsyncFolderAction folder ->
                while go do
                    let! tempResult = folder result e.Current
                    result <- tempResult
                    let! step = e.MoveNextAsync()
                    go <- step

            return result
        }

    let toResizeArrayAsync source =
        checkNonNull (nameof source) source

        task {
            let res = ResizeArray()
            do! source |> iter (SimpleAction(fun item -> res.Add item))
            return res
        }

    let toResizeArrayAndMapAsync mapper source = (toResizeArrayAsync >> Task.map mapper) source

    let map mapper (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        match mapper with
        | CountableAction mapper -> taskSeq {
            let mutable i = 0

            for c in source do
                yield mapper i c
                i <- i + 1
          }

        | SimpleAction mapper -> taskSeq {
            for c in source do
                yield mapper c
          }

        | AsyncCountableAction mapper -> taskSeq {
            let mutable i = 0

            for c in source do
                let! result = mapper i c
                yield result
                i <- i + 1
          }

        | AsyncSimpleAction mapper -> taskSeq {
            for c in source do
                let! result = mapper c
                yield result
          }

    let zip (source1: TaskSeq<_>) (source2: TaskSeq<_>) =
        checkNonNull (nameof source1) source1
        checkNonNull (nameof source2) source2

        taskSeq {
            use e1 = source1.GetAsyncEnumerator CancellationToken.None
            use e2 = source2.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step1 = e1.MoveNextAsync()
            let! step2 = e2.MoveNextAsync()
            go <- step1 && step2

            while go do
                yield e1.Current, e2.Current
                let! step1 = e1.MoveNextAsync()
                let! step2 = e2.MoveNextAsync()
                go <- step1 && step2
        }

    let collect (binder: _ -> #IAsyncEnumerable<_>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                yield! binder c :> IAsyncEnumerable<_>
        }

    let collectSeq (binder: _ -> #seq<_>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                yield! binder c :> seq<_>
        }

    let collectAsync (binder: _ -> #Task<#IAsyncEnumerable<_>>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! result = binder c
                yield! result :> IAsyncEnumerable<_>
        }

    let collectSeqAsync (binder: _ -> #Task<#seq<_>>) (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            for c in source do
                let! result = binder c
                yield! result :> seq<_>
        }

    let tryLast (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let mutable last = ValueNone
            let! step = e.MoveNextAsync()
            go <- step

            while go do
                last <- ValueSome e.Current
                let! step = e.MoveNextAsync()
                go <- step

            match last with
            | ValueSome value -> return Some value
            | ValueNone -> return None
        }

    let tryHead (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            match! e.MoveNextAsync() with
            | true -> return Some e.Current
            | false -> return None
        }

    let tryTail (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            match! e.MoveNextAsync() with
            | false -> return None
            | true ->
                return
                    taskSeq {
                        let mutable go = true
                        let! step = e.MoveNextAsync()
                        go <- step

                        while go do
                            yield e.Current
                            let! step = e.MoveNextAsync()
                            go <- step
                    }
                    |> Some
        }

    let tryItem index (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            if index < 0 then
                // while the loop below wouldn't run anyway, we don't want to call MoveNext in this case
                // to prevent side effects hitting unnecessarily
                return None
            else
                use e = source.GetAsyncEnumerator CancellationToken.None
                let mutable go = true
                let mutable idx = 0
                let mutable foundItem = None
                let! step = e.MoveNextAsync()
                go <- step

                while go && idx <= index do
                    if idx = index then
                        foundItem <- Some e.Current
                        go <- false
                    else
                        let! step = e.MoveNextAsync()
                        go <- step
                        idx <- idx + 1

                return foundItem
        }

    let tryPick chooser (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            let mutable go = true
            let mutable foundItem = None
            let! step = e.MoveNextAsync()
            go <- step

            match chooser with
            | TryPick picker ->
                while go do
                    match picker e.Current with
                    | Some value ->
                        foundItem <- Some value
                        go <- false
                    | None ->
                        let! step = e.MoveNextAsync()
                        go <- step

            | TryPickAsync picker ->
                while go do
                    match! picker e.Current with
                    | Some value ->
                        foundItem <- Some value
                        go <- false
                    | None ->
                        let! step = e.MoveNextAsync()
                        go <- step

            return foundItem
        }

    let tryFind predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            let mutable go = true
            let mutable foundItem = None
            let! step = e.MoveNextAsync()
            go <- step

            match predicate with
            | Predicate predicate ->
                while go do
                    let current = e.Current

                    match predicate current with
                    | true ->
                        foundItem <- Some current
                        go <- false
                    | false ->
                        let! step = e.MoveNextAsync()
                        go <- step

            | PredicateAsync predicate ->
                while go do
                    let current = e.Current

                    match! predicate current with
                    | true ->
                        foundItem <- Some current
                        go <- false
                    | false ->
                        let! step = e.MoveNextAsync()
                        go <- step

            return foundItem
        }

    let tryFindIndex predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        task {
            use e = source.GetAsyncEnumerator CancellationToken.None

            let mutable go = true
            let mutable isFound = false
            let mutable index = -1
            let! step = e.MoveNextAsync()
            go <- step

            match predicate with
            | Predicate predicate ->
                while go && not isFound do
                    index <- index + 1
                    isFound <- predicate e.Current

                    if not isFound then
                        let! step = e.MoveNextAsync()
                        go <- step

            | PredicateAsync predicate ->
                while go && not isFound do
                    index <- index + 1
                    let! predicateResult = predicate e.Current
                    isFound <- predicateResult

                    if not isFound then
                        let! step = e.MoveNextAsync()
                        go <- step

            if isFound then return Some index else return None
        }

    let choose chooser (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {

            match chooser with
            | TryPick picker ->
                for item in source do
                    match picker item with
                    | Some value -> yield value
                    | None -> ()

            | TryPickAsync picker ->
                for item in source do
                    match! picker item with
                    | Some value -> yield value
                    | None -> ()
        }

    let filter predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            match predicate with
            | Predicate syncPredicate ->
                for item in source do
                    if syncPredicate item then
                        yield item

            | PredicateAsync asyncPredicate ->
                for item in source do
                    match! asyncPredicate item with
                    | true -> yield item
                    | false -> ()
        }

    let forall predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        match predicate with
        | Predicate syncPredicate -> task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable state = true
            let! cont = e.MoveNextAsync()
            let mutable hasMore = cont

            while state && hasMore do
                state <- syncPredicate e.Current

                if state then
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            return state
          }

        | PredicateAsync asyncPredicate -> task {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable state = true
            let! cont = e.MoveNextAsync()
            let mutable hasMore = cont

            while state && hasMore do
                let! pred = asyncPredicate e.Current
                state <- pred

                if state then
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            return state
          }

    let skipOrTake skipOrTake count (source: TaskSeq<_>) =
        checkNonNull (nameof source) source
        raiseCannotBeNegative (nameof count) count

        match skipOrTake with
        | Skip ->
            // don't create a new sequence if count = 0
            if count = 0 then
                source
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    for _ in 1..count do
                        let! hasMore = e.MoveNextAsync()

                        if not hasMore then
                            raiseInsufficient ()

                    while! e.MoveNextAsync() do
                        yield e.Current

                }
        | Drop ->
            // don't create a new sequence if count = 0
            if count = 0 then
                source
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    let! step = e.MoveNextAsync()
                    let mutable cont = step
                    let mutable pos = 0

                    // skip, or stop looping if we reached the end
                    while cont do
                        pos <- pos + 1

                        if pos < count then
                            let! moveNext = e.MoveNextAsync()
                            cont <- moveNext
                        else
                            cont <- false

                    // return the rest
                    while! e.MoveNextAsync() do
                        yield e.Current

                }
        | Take ->
            // don't initialize an empty task sequence
            if count = 0 then
                empty
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    for _ in count .. - 1 .. 1 do
                        let! step = e.MoveNextAsync()

                        if not step then
                            raiseInsufficient ()

                        yield e.Current
                }

        | Truncate ->
            // don't create a new sequence if count = 0
            if count = 0 then
                empty
            else
                taskSeq {
                    use e = source.GetAsyncEnumerator CancellationToken.None

                    let! step = e.MoveNextAsync()
                    let mutable cont = step
                    let mutable pos = 0

                    // return items until we've exhausted the seq
                    while cont do
                        yield e.Current
                        pos <- pos + 1

                        if pos < count then
                            let! moveNext = e.MoveNextAsync()
                            cont <- moveNext
                        else
                            cont <- false

                }

    let takeWhile isInclusive predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! notEmpty = e.MoveNextAsync()
            let mutable hasMore = notEmpty

            match predicate with
            | Predicate synchronousPredicate ->
                while hasMore && synchronousPredicate e.Current do
                    yield e.Current
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            | PredicateAsync asyncPredicate ->
                let mutable predicateHolds = true

                while hasMore && predicateHolds do // TODO: check perf if `while!` is going to be better or equal
                    let! predicateIsTrue = asyncPredicate e.Current

                    if predicateIsTrue then
                        yield e.Current
                        let! cont = e.MoveNextAsync()
                        hasMore <- cont

                    predicateHolds <- predicateIsTrue

            // "inclusive" means: always return the item that we pulled, regardless of the result of applying the predicate
            // and only stop thereafter. The non-inclusive versions, in contrast, do not return the item under which the predicate is false.
            if hasMore && isInclusive then
                yield e.Current
        }

    let skipWhile isInclusive predicate (source: TaskSeq<_>) =
        checkNonNull (nameof source) source

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let! notEmpty = e.MoveNextAsync()
            let mutable hasMore = notEmpty

            match predicate with
            | Predicate synchronousPredicate ->
                while hasMore && synchronousPredicate e.Current do
                    // keep skipping
                    let! cont = e.MoveNextAsync()
                    hasMore <- cont

            | PredicateAsync asyncPredicate ->
                let mutable predicateHolds = true

                while hasMore && predicateHolds do // TODO: check perf if `while!` is going to be better or equal
                    let! predicateIsTrue = asyncPredicate e.Current

                    if predicateIsTrue then
                        // keep skipping
                        let! cont = e.MoveNextAsync()
                        hasMore <- cont

                    predicateHolds <- predicateIsTrue

            // "inclusive" means: always skip the item that we pulled, regardless of the result of applying the predicate
            // and only stop thereafter. The non-inclusive versions, in contrast, do not skip the item under which the predicate is false.
            if hasMore && not isInclusive then
                yield e.Current // don't skip, unless inclusive

            // propagate the rest
            while! e.MoveNextAsync() do
                yield e.Current
        }

    /// InsertAt or InsertManyAt
    let insertAt index valueOrValues (source: TaskSeq<_>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0

            for item in source do
                if i = index then
                    match valueOrValues with
                    | Many values -> yield! values
                    | One value -> yield value

                yield item
                i <- i + 1

            // allow inserting at the end
            if i = index then
                match valueOrValues with
                | Many values -> yield! values
                | One value -> yield value

            if i < index then
                raiseOutOfBounds (nameof index)
        }

    let removeAt index (source: TaskSeq<'T>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0

            for item in source do
                if i <> index then
                    yield item

                i <- i + 1

            // cannot remove past end of sequence
            if i <= index then
                raiseOutOfBounds (nameof index)
        }

    let removeManyAt index count (source: TaskSeq<'T>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0
            let indexEnd = index + count

            for item in source do
                if i < index || i >= indexEnd then
                    yield item

                i <- i + 1

            // cannot remove past end of sequence
            if i <= index then
                raiseOutOfBounds (nameof index)
        }

    let updateAt index value (source: TaskSeq<'T>) =
        raiseCannotBeNegative (nameof index) index

        taskSeq {
            let mutable i = 0

            for item in source do
                if i <> index then // most common scenario on top (cpu prediction)
                    yield item
                else
                    yield value

                i <- i + 1

            // cannot update past end of sequence
            if i <= index then
                raiseOutOfBounds (nameof index)
        }

    // Consider turning using an F# version of this instead?
    // https://github.com/i3arnon/ConcurrentHashSet
    type ConcurrentHashSet<'T when 'T: equality>(ct) =
        let _rwLock = new ReaderWriterLockSlim()
        let hashSet = HashSet<'T>(Array.empty, HashIdentity.Structural)

        member _.Add item =
            _rwLock.EnterWriteLock()

            try
                hashSet.Add item
            finally
                _rwLock.ExitWriteLock()

        member _.AddMany items =
            _rwLock.EnterWriteLock()

            try
                for item in items do
                    hashSet.Add item |> ignore

            finally
                _rwLock.ExitWriteLock()

        member _.AddManyAsync(source: TaskSeq<'T>) = task {
            use e = source.GetAsyncEnumerator(ct)
            let mutable go = true
            let! step = e.MoveNextAsync()
            go <- step

            while go do
                // NOTE: r/w lock cannot cross thread boundaries. Should we use SemaphoreSlim instead?
                // or alternatively, something like this: https://github.com/StephenCleary/AsyncEx/blob/8a73d0467d40ca41f9f9cf827c7a35702243abb8/src/Nito.AsyncEx.Coordination/AsyncReaderWriterLock.cs#L16
                // not sure how they compare.

                _rwLock.EnterWriteLock()

                try
                    hashSet.Add e.Current |> ignore
                finally
                    _rwLock.ExitWriteLock()

                let! step = e.MoveNextAsync()
                go <- step
        }

        interface IDisposable with
            override _.Dispose() =
                if not (isNull _rwLock) then
                    _rwLock.Dispose()

    let except itemsToExclude (source: TaskSeq<_>) =
        checkNonNull (nameof source) source
        checkNonNull (nameof itemsToExclude) itemsToExclude

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step = e.MoveNextAsync()
            go <- step

            if step then
                // only create hashset by the time we actually start iterating
                use hashSet = new ConcurrentHashSet<_>(CancellationToken.None)
                do! hashSet.AddManyAsync itemsToExclude

                while go do
                    let current = e.Current

                    // if true, it was added, and therefore unique, so we return it
                    // if false, it existed, and therefore a duplicate, and we skip
                    if hashSet.Add current then
                        yield current

                    let! step = e.MoveNextAsync()
                    go <- step

        }

    let exceptOfSeq itemsToExclude (source: TaskSeq<_>) =
        checkNonNull (nameof source) source
        checkNonNull (nameof itemsToExclude) itemsToExclude

        taskSeq {
            use e = source.GetAsyncEnumerator CancellationToken.None
            let mutable go = true
            let! step = e.MoveNextAsync()
            go <- step

            if step then
                // only create hashset by the time we actually start iterating
                use hashSet = new ConcurrentHashSet<_>(CancellationToken.None)
                do hashSet.AddMany itemsToExclude

                while go do
                    let current = e.Current

                    // if true, it was added, and therefore unique, so we return it
                    // if false, it existed, and therefore a duplicate, and we skip
                    if hashSet.Add current then
                        yield current

                    let! step = e.MoveNextAsync()
                    go <- step

        }
