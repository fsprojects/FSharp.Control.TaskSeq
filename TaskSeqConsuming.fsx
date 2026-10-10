(**

*)
#r "nuget: FSharp.Control.TaskSeq,1.1.1"
(**
# Consuming Task Sequences

All `TaskSeq<'T>` values are **lazy** — they produce elements only when actively consumed. This
page covers all the ways to consume a task sequence: iteration with side effects, collection
into arrays and lists, folding, aggregation, searching, and element access.

*)
open System.Threading.Tasks
open FSharp.Control

let numbers : TaskSeq<int> = TaskSeq.ofSeq (seq { 1..5 })
(**
-----------------------

## Iterating with a for loop

Inside a `task { ... }` or `taskSeq { ... }` computation expression, you can iterate a
`TaskSeq<'T>` with a plain `for` loop.  The loop body may contain `let!` and `do!`:

*)
task {
    for n in numbers do
        printfn "Got %d" n
}
|> Task.WaitAll
(**
-----------------------

## iter and iterAsync

`TaskSeq.iter` applies a synchronous side-effecting function to every element:

*)
let printAll : Task<unit> = numbers |> TaskSeq.iter (printfn "item: %d")
(**
`TaskSeq.iterAsync` awaits the returned task before consuming the next element — ideal for
actions that themselves do IO, such as writing to a database:

*)
let processItem (n: int) : Task<unit> =
    task { printfn "processing %d" n }

let processAll : Task<unit> = numbers |> TaskSeq.iterAsync processItem
(**
### iteri and iteriAsync

`TaskSeq.iteri` and `TaskSeq.iteriAsync` additionally pass the zero-based index to the action:

*)
let printWithIndex : Task<unit> =
    numbers |> TaskSeq.iteri (fun i n -> printfn "[%d] %d" i n)
(**
-----------------------

## Collecting into arrays and lists

`TaskSeq.toArrayAsync` and `TaskSeq.toListAsync` consume the whole sequence into an in-memory
collection.  The blocking (synchronous) variants `toArray`, `toList`, and `toSeq` are also
available when you need them in a synchronous context:

*)
let arr : Task<int[]> = numbers |> TaskSeq.toArrayAsync
let lst : Task<int list> = numbers |> TaskSeq.toListAsync
let rz : Task<System.Collections.Generic.List<int>> = numbers |> TaskSeq.toResizeArrayAsync

// Blocking variants (avoid in async code)
let arrSync : int[] = numbers |> TaskSeq.toArray
let lstSync : int list = numbers |> TaskSeq.toList
(**
-----------------------

## fold and foldAsync

`TaskSeq.fold` threads an accumulator through the sequence, returning the final state:

*)
let sum : Task<int> =
    numbers |> TaskSeq.fold (fun acc n -> acc + n) 0

let concat : Task<string> =
    TaskSeq.ofList [ "hello"; " "; "world" ]
    |> TaskSeq.fold (fun acc s -> acc + s) ""
(**
`TaskSeq.foldAsync` is the same but the folder returns `Task<'State>`:

*)
let sumAsync : Task<int> =
    numbers
    |> TaskSeq.foldAsync (fun acc n -> task { return acc + n }) 0
(**
-----------------------

## scan and scanAsync

`TaskSeq.scan` is like `fold` but emits each intermediate state as a new element.  The output
sequence has `N + 1` elements when the input has `N`, because the initial state is also
emitted:

*)
let runningTotals : TaskSeq<int> =
    numbers |> TaskSeq.scan (fun acc n -> acc + n) 0

// yields 0, 1, 3, 6, 10, 15
(**
-----------------------

## reduce and reduceAsync

`TaskSeq.reduce` uses the first element as the initial state — there is no extra zero argument:

*)
let product : Task<int> = numbers |> TaskSeq.reduce (fun acc n -> acc * n)
(**
-----------------------

## Aggregation: sum, average, min, max

Numeric aggregates follow the same pattern as the `Seq` module:

*)
let total : Task<int> = numbers |> TaskSeq.sum

let avg : Task<float> =
    TaskSeq.ofList [ 1.0; 2.0; 3.0 ] |> TaskSeq.average

let biggest : Task<int> = numbers |> TaskSeq.max
let smallest : Task<int> = numbers |> TaskSeq.min
(**
`sumBy`, `averageBy`, `maxBy`, and `minBy` apply a projection first.  Async variants
`sumByAsync`, `averageByAsync`, `maxByAsync`, and `minByAsync` are also available:

*)
let sumOfSquares : Task<int> = numbers |> TaskSeq.sumBy (fun n -> n * n)
(**
-----------------------

## length and isEmpty

`TaskSeq.length` consumes the whole sequence.  Use `TaskSeq.lengthOrMax` to avoid evaluating
more than a known upper bound — useful for infinite sequences or for early termination:

*)
let len : Task<int> = numbers |> TaskSeq.length // 5

let atMost3 : Task<int> = numbers |> TaskSeq.lengthOrMax 3 // 3 (stops early)

let empty : Task<bool> = numbers |> TaskSeq.isEmpty // false
(**
`TaskSeq.lengthBy` counts elements satisfying a predicate in a single pass:

*)
let countEvens : Task<int> = numbers |> TaskSeq.lengthBy (fun n -> n % 2 = 0)
(**
-----------------------

## Element access: head, last, item, exactlyOne, firstOrDefault, lastOrDefault

*)
let first : Task<int> = numbers |> TaskSeq.head // 1
let last : Task<int> = numbers |> TaskSeq.last // 5
let third : Task<int> = numbers |> TaskSeq.item 2 // 3 (zero-based)
let only : Task<int> = TaskSeq.singleton 42 |> TaskSeq.exactlyOne // 42

// Safe "try" variants return None instead of throwing:
let tryFirst : Task<int option> = numbers |> TaskSeq.tryHead
let tryLast : Task<int option> = numbers |> TaskSeq.tryLast
let tryThird : Task<int option> = numbers |> TaskSeq.tryItem 2
let tryOnly : Task<int option> = TaskSeq.singleton 42 |> TaskSeq.tryExactlyOne
(**
`TaskSeq.firstOrDefault` and `TaskSeq.lastOrDefault` return a caller-supplied default when the
sequence is empty — useful as a concise alternative to `tryHead`/`tryLast` when `None` would
need to be immediately unwrapped:

*)
let firstOrZero : Task<int> = TaskSeq.empty<int> |> TaskSeq.firstOrDefault 0 // 0
let lastOrZero : Task<int> = TaskSeq.empty<int> |> TaskSeq.lastOrDefault 0 // 0

let firstOrMinus1 : Task<int> = numbers |> TaskSeq.firstOrDefault -1 // 1
let lastOrMinus1 : Task<int> = numbers |> TaskSeq.lastOrDefault -1 // 5
(**
-----------------------

## Searching: find, pick, contains, exists, forall

`TaskSeq.find` returns the first element satisfying a predicate, or throws if none is found.
`TaskSeq.tryFind` returns `None` instead of throwing:

*)
let firstEven : Task<int> = numbers |> TaskSeq.find (fun n -> n % 2 = 0)

let maybeEven : Task<int option> = numbers |> TaskSeq.tryFind (fun n -> n % 2 = 0)
(**
`TaskSeq.findIndex` and `TaskSeq.tryFindIndex` return the zero-based index:

*)
let indexOfFirst3 : Task<int> = numbers |> TaskSeq.findIndex (fun n -> n = 3)
(**
`TaskSeq.pick` and `TaskSeq.tryPick` are like `find` but the predicate also projects to a new
value — equivalent to a combined `choose` + `head`:

*)
let firstSquareOver10 : Task<int option> =
    numbers
    |> TaskSeq.tryPick (fun n ->
        let sq = n * n
        if sq > 10 then Some sq else None)
(**
`TaskSeq.contains` tests membership by equality. `TaskSeq.exists` tests with a predicate.
`TaskSeq.forall` tests that all elements satisfy a predicate:

*)
let has3 : Task<bool> = numbers |> TaskSeq.contains 3

let anyNegative : Task<bool> = numbers |> TaskSeq.exists (fun n -> n < 0)

let allPositive : Task<bool> = numbers |> TaskSeq.forall (fun n -> n > 0)
(**
All search/predicate operations have `Async` variants (`findAsync`, `existsAsync`,
`forallAsync`, etc.) that accept a predicate returning `Task<bool>`.

*)

