(**

*)
#r "nuget: FSharp.Control.TaskSeq,1.1.1"
(**
# Combining Task Sequences

This page covers operations that combine multiple sequences or reshape a single sequence: append,
zip, zipWith, concat, slicing with take/skip/splitAt, chunking and windowing.

*)
open FSharp.Control
(**
-----------------------

## Append

`TaskSeq.append` produces all elements of the first sequence followed by all elements of the
second. The second sequence does not start until the first is exhausted:

*)
let first = TaskSeq.ofList [ 1; 2; 3 ]
let second = TaskSeq.ofList [ 4; 5; 6 ]

let appended : TaskSeq<int> = TaskSeq.append first second // 1, 2, 3, 4, 5, 6
(**
Inside `taskSeq { ... }`, `yield!` is the natural way to concatenate:

*)
let combined = taskSeq {
    yield! first
    yield! second
}
(**
`TaskSeq.appendSeq` appends a plain `seq<'T>` after a task sequence.
`TaskSeq.prependSeq` prepends a plain `seq<'T>` before a task sequence:

*)
let withPrefix : TaskSeq<int> = TaskSeq.prependSeq [ 0 ] first // 0, 1, 2, 3
let withSuffix : TaskSeq<int> = TaskSeq.appendSeq first [ 4; 5 ] // 1, 2, 3, 4, 5
(**
-----------------------

## concat

`TaskSeq.concat` flattens a task sequence of task sequences into a single flat sequence.
Each inner sequence is consumed fully before the next one begins:

*)
let nested : TaskSeq<TaskSeq<int>> =
    TaskSeq.ofList
        [ TaskSeq.ofList [ 1; 2 ]
          TaskSeq.ofList [ 3; 4 ]
          TaskSeq.ofList [ 5; 6 ] ]

let flat : TaskSeq<int> = TaskSeq.concat nested // 1, 2, 3, 4, 5, 6
(**
Overloads also exist for `TaskSeq<seq<'T>>`, `TaskSeq<'T list>`, `TaskSeq<'T[]>`, and
`TaskSeq<ResizeArray<'T>>`.

-----------------------

## zip and zip3

`TaskSeq.zip` pairs up elements from two sequences, stopping when the shorter sequence ends:

*)
let letters : TaskSeq<char> = TaskSeq.ofList [ 'a'; 'b'; 'c' ]
let nums : TaskSeq<int> = TaskSeq.ofList [ 1; 2; 3; 4 ]

let pairs : TaskSeq<char * int> = TaskSeq.zip letters nums
// ('a',1), ('b',2), ('c',3)  — stops when letters runs out
(**
`TaskSeq.zip3` does the same for three sequences:

*)
let booleans : TaskSeq<bool> = TaskSeq.ofList [ true; false; true ]

let triples : TaskSeq<char * int * bool> = TaskSeq.zip3 letters nums booleans
(**
-----------------------

## zipWith and zipWithAsync

`TaskSeq.zipWith` is like `zip` but applies a mapping function to produce a result instead of
yielding a tuple.  The result sequence stops when the shorter source ends:

*)
let addPairs : TaskSeq<int> = TaskSeq.zipWith (+) nums nums
// 2, 4, 6, 8
(**
`TaskSeq.zipWithAsync` accepts an asynchronous mapping function:

*)
let asyncProduct : TaskSeq<int> =
    TaskSeq.zipWithAsync (fun a b -> Task.fromResult (a * b)) nums nums
// 1, 4, 9, 16, ...
(**
-----------------------

## zipWith3 and zipWithAsync3

`TaskSeq.zipWith3` combines three sequences with a three-argument mapping function, stopping at
the shortest:

*)
let sumThree : TaskSeq<int> =
    TaskSeq.zipWith3 (fun a b c -> a + b + c) nums nums nums
// 3, 6, 9, 12, ...
(**
`TaskSeq.zipWithAsync3` takes an asynchronous three-argument mapper:

*)
let asyncSumThree : TaskSeq<int> =
    TaskSeq.zipWithAsync3 (fun a b c -> Task.fromResult (a + b + c)) nums nums nums
(**
-----------------------

## pairwise

`TaskSeq.pairwise` produces a sequence of consecutive pairs.  An input with fewer than two elements
produces an empty result:

*)
let consecutive : TaskSeq<int> = TaskSeq.ofList [ 1; 2; 3; 4; 5 ]

let pairs2 : TaskSeq<int * int> = consecutive |> TaskSeq.pairwise
// (1,2), (2,3), (3,4), (4,5)
(**
-----------------------

## take and truncate

`TaskSeq.take count` yields exactly `count` elements and throws if the source is shorter:

*)
let first3 : TaskSeq<int> = consecutive |> TaskSeq.take 3 // 1, 2, 3
(**
`TaskSeq.truncate count` yields **at most** `count` elements without throwing when the source is
shorter:

*)
let atMost10 : TaskSeq<int> = consecutive |> TaskSeq.truncate 10 // 1, 2, 3, 4, 5
(**
-----------------------

## splitAt

`TaskSeq.splitAt count` splits a sequence into a prefix array and a lazy remainder sequence.  The
prefix always contains **at most** `count` elements — it never throws when the sequence is shorter.
The remainder sequence is a lazy view over the unconsumed tail and can be iterated once:

*)
let splitData : TaskSeq<int> = TaskSeq.ofList [ 1..10 ]

let splitExample : Task<int[] * TaskSeq<int>> = TaskSeq.splitAt 4 splitData
// prefix = [|1;2;3;4|], rest = lazy 5,6,7,8,9,10
(**
Unlike `take`/`skip`, a single `splitAt` call evaluates elements only once — the prefix is
materialised eagerly and the rest is yielded lazily without re-reading the source.

-----------------------

## skip and drop

`TaskSeq.skip count` skips exactly `count` elements and throws if the source is shorter:

*)
let afterFirst2 : TaskSeq<int> = consecutive |> TaskSeq.skip 2 // 3, 4, 5
(**
`TaskSeq.drop count` drops **at most** `count` elements without throwing:

*)
let safeAfter10 : TaskSeq<int> = consecutive |> TaskSeq.drop 10 // empty
(**
-----------------------

## takeWhile and takeWhileInclusive

`TaskSeq.takeWhile predicate` yields elements while the predicate is `true`, then stops (the
element that caused the stop is **not** yielded):

*)
let lessThan4 : TaskSeq<int> = consecutive |> TaskSeq.takeWhile (fun n -> n < 4)
// 1, 2, 3
(**
`TaskSeq.takeWhileInclusive` yields the first element for which the predicate is `false` and
then stops — so at least one element is always yielded from a non-empty source:

*)
let upToFirstGe4 : TaskSeq<int> =
    consecutive |> TaskSeq.takeWhileInclusive (fun n -> n < 4)
// 1, 2, 3, 4
(**
Async variants: `TaskSeq.takeWhileAsync`, `TaskSeq.takeWhileInclusiveAsync`.

-----------------------

## skipWhile and skipWhileInclusive

`TaskSeq.skipWhile predicate` skips elements while the predicate is `true`, then yields the
rest (the first failing element **is** yielded):

*)
let from3 : TaskSeq<int> = consecutive |> TaskSeq.skipWhile (fun n -> n < 3)
// 3, 4, 5
(**
`TaskSeq.skipWhileInclusive` also skips the first element for which the predicate is `false`:

*)
let afterFirst3 : TaskSeq<int> =
    consecutive |> TaskSeq.skipWhileInclusive (fun n -> n < 3)
// 4, 5
(**
Async variants: `TaskSeq.skipWhileAsync`, `TaskSeq.skipWhileInclusiveAsync`.

-----------------------

## chunkBySize

`TaskSeq.chunkBySize chunkSize` divides the sequence into non-overlapping arrays of at most
`chunkSize` elements.  The last chunk may be smaller if the sequence does not divide evenly:

*)
let chunks : TaskSeq<int[]> = consecutive |> TaskSeq.chunkBySize 2
// [|1;2|], [|3;4|], [|5|]
(**
-----------------------

## chunkBy and chunkByAsync

`TaskSeq.chunkBy projection` groups **consecutive** elements with the same key into `(key, elements[])` pairs.
A new group starts each time the key changes.  Unlike `groupBy`, elements that are not adjacent are
**not** merged, so the source order is preserved and the sequence can be infinite:

*)
let words : TaskSeq<string> = TaskSeq.ofList [ "apple"; "apricot"; "banana"; "blueberry"; "cherry" ]

let byFirstLetter : TaskSeq<char * string[]> =
    words |> TaskSeq.chunkBy (fun w -> w[0])
// ('a', [|"apple";"apricot"|]), ('b', [|"banana";"blueberry"|]), ('c', [|"cherry"|])
(**
`TaskSeq.chunkByAsync` accepts an asynchronous projection:

*)
let byFirstLetterAsync : TaskSeq<char * string[]> =
    words |> TaskSeq.chunkByAsync (fun w -> Task.fromResult w[0])
(**
-----------------------

## windowed

`TaskSeq.windowed windowSize` produces a sliding window of exactly `windowSize` consecutive
elements.  The result is empty if the source has fewer elements than the window size:

*)
let windows : TaskSeq<int[]> = consecutive |> TaskSeq.windowed 3
// [|1;2;3|], [|2;3;4|], [|3;4;5|]
(**
`windowed` uses a ring buffer internally, so each window allocation is separate — safe to
store the windows independently.

*)

