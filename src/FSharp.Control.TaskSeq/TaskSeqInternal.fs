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

module internal TaskSeqInternal =
    let inline raiseEmptySeq () =
        ArgumentException("The asynchronous input sequence was empty.", "source")
        |> raise

    let inline raiseCannotBeNegative (name: string) =
        ArgumentException("The value cannot be negative", name)
        |> raise

    let inline raiseInsufficient () =
        ArgumentException("The asynchronous input sequence was has an insufficient number of elements.", "source")
        |> raise

    let inline raiseNotFound () =
        KeyNotFoundException("The predicate function or index did not satisfy any item in the async sequence.")
        |> raise

    let isEmpty (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())
        let! step = e.MoveNextAsync()
        return not step
    }

    let singleton (source: 'T) =
        { new IAsyncEnumerable<'T> with
            member _.GetAsyncEnumerator(_) =
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
                        | WithCurrent -> source
                        | _ -> Unchecked.defaultof<'T>

                    member _.DisposeAsync() = ValueTask.CompletedTask
                }
        }

    /// Returns length unconditionally, or based on a predicate
    let lengthBy predicate (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())
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

        | Some (Predicate predicate) ->
            while go do
                if predicate e.Current then
                    i <- i + 1

                let! step = e.MoveNextAsync()
                go <- step

        | Some (PredicateAsync predicate) ->
            while go do
                match! predicate e.Current with
                | true -> i <- i + 1
                | false -> ()

                let! step = e.MoveNextAsync()
                go <- step

        return i
    }

    /// Returns length unconditionally, or based on a predicate
    let lengthBeforeMax max (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())
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

    let tryExactlyOne (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())

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
            | Some c -> if c >= 0 then c else raiseCannotBeNegative (nameof count)
            | None -> Int32.MaxValue

        match initializer with
        | InitAction init ->
            while i < count do
                // using Lazy gives us locking and safe multiple access to the cached value, if
                // multiple threads access the same item through the same enumerator (which is
                // bad practice, but hey, who're we to judge).
                if isNull value then
                    value <- Lazy<_>.Create (fun () -> init i)

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

    let iter action (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())
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

    let fold folder initial (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())
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

    let toResizeArrayAsync source = task {
        let res = ResizeArray()
        do! source |> iter (SimpleAction(fun item -> res.Add item))
        return res
    }

    let toResizeArrayAndMapAsync mapper source = (toResizeArrayAsync >> Task.map mapper) source

    let map mapper (taskSequence: taskSeq<_>) =
        match mapper with
        | CountableAction mapper -> taskSeq {
            let mutable i = 0

            for c in taskSequence do
                yield mapper i c
                i <- i + 1
          }

        | SimpleAction mapper -> taskSeq {
            for c in taskSequence do
                yield mapper c
          }

        | AsyncCountableAction mapper -> taskSeq {
            let mutable i = 0

            for c in taskSequence do
                let! result = mapper i c
                yield result
                i <- i + 1
          }

        | AsyncSimpleAction mapper -> taskSeq {
            for c in taskSequence do
                let! result = mapper c
                yield result
          }

    let zip (source1: taskSeq<_>) (source2: taskSeq<_>) = taskSeq {
        use e1 = source1.GetAsyncEnumerator(CancellationToken())
        use e2 = source2.GetAsyncEnumerator(CancellationToken())
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

    let collect (binder: _ -> #IAsyncEnumerable<_>) (source: taskSeq<_>) = taskSeq {
        for c in source do
            yield! binder c :> IAsyncEnumerable<_>
    }

    let collectSeq (binder: _ -> #seq<_>) (source: taskSeq<_>) = taskSeq {
        for c in source do
            yield! binder c :> seq<_>
    }

    let collectAsync (binder: _ -> #Task<#IAsyncEnumerable<_>>) (source: taskSeq<_>) = taskSeq {
        for c in source do
            let! result = binder c
            yield! result :> IAsyncEnumerable<_>
    }

    let collectSeqAsync (binder: _ -> #Task<#seq<_>>) (source: taskSeq<_>) = taskSeq {
        for c in source do
            let! result = binder c
            yield! result :> seq<_>
    }

    let tryLast (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())
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

    let tryHead (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())

        match! e.MoveNextAsync() with
        | true -> return Some e.Current
        | false -> return None
    }

    let tryTail (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())

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

    let tryItem index (source: taskSeq<_>) = task {
        if index < 0 then
            // while the loop below wouldn't run anyway, we don't want to call MoveNext in this case
            // to prevent side effects hitting unnecessarily
            return None
        else
            use e = source.GetAsyncEnumerator(CancellationToken())
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

    let tryPick chooser (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())

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

    let tryFind predicate (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())

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

    let tryFindIndex predicate (source: taskSeq<_>) = task {
        use e = source.GetAsyncEnumerator(CancellationToken())

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

    let choose chooser (source: taskSeq<_>) = taskSeq {
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

    let filter predicate (source: taskSeq<_>) = taskSeq {
        match predicate with
        | Predicate predicate ->
            for item in source do
                if predicate item then
                    yield item

        | PredicateAsync predicate ->
            for item in source do
                match! predicate item with
                | true -> yield item
                | false -> ()
    }

    let takeWhile inclusive predicate (source: taskSeq<_>) = taskSeq {
        use e = source.GetAsyncEnumerator(CancellationToken())
        let! step = e.MoveNextAsync()
        let mutable more = step

        match predicate with
        | Predicate predicate ->
            while more do
                let value = e.Current
                more <- predicate value
                if more || inclusive then
                    yield value
                if more then
                    let! ok = e.MoveNextAsync()
                    more <- ok
        | PredicateAsync predicate ->
            while more do
                let value = e.Current
                let! passed = predicate value
                more <- passed
                if more || inclusive then
                    yield value
                if more then
                    let! ok = e.MoveNextAsync()
                    more <- ok
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

        member _.AddManyAsync(source: taskSeq<'T>) = task {
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

        interface IAsyncDisposable with
            override _.DisposeAsync() =
                if not (isNull _rwLock) then
                    _rwLock.Dispose()

                ValueTask.CompletedTask

    let except itemsToExclude (source: taskSeq<_>) = taskSeq {
        use e = source.GetAsyncEnumerator(CancellationToken())
        let mutable go = true
        let! step = e.MoveNextAsync()
        go <- step

        if step then
            // only create hashset by the time we actually start iterating
            use hashSet = new ConcurrentHashSet<_>(CancellationToken())
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

    let exceptOfSeq itemsToExclude (source: taskSeq<_>) = taskSeq {
        use e = source.GetAsyncEnumerator(CancellationToken())
        let mutable go = true
        let! step = e.MoveNextAsync()
        go <- step

        if step then
            // only create hashset by the time we actually start iterating
            use hashSet = new ConcurrentHashSet<_>(CancellationToken())
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
