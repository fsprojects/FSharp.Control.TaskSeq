namespace FSharpy.Tests

open System
open System.IO
open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open System.Threading.Tasks
open System.Diagnostics
open System.Collections.Generic
open Xunit.Abstractions

/// Just a naive, simple in-memory reader that acts as an IAsyncEnumerable to use with tests
/// IMPORTANT: currently this is not thread-safe!!!
type AsyncBufferedReader(output: ITestOutputHelper, data, blockSize) =
    let stream = new MemoryStream(data: byte[])
    let buffered = new BufferedStream(stream, blockSize)
    let mutable current = ValueNone

    interface IAsyncEnumerable<byte[]> with
        member reader.GetAsyncEnumerator(ct) = reader :> IAsyncEnumerator<_>

    interface IAsyncEnumerator<byte[]> with
        member _.Current =
            match current with
            | ValueSome x -> x
            | ValueNone -> failwith "Not a current item!"

        member _.MoveNextAsync() =
            task {
                let mem = Array.zeroCreate blockSize

                // this advances the "current" position automatically. However, this is clearly NOT threadsafe!!!
                let! bytesRead = buffered.ReadAsync(mem, 0, mem.Length) // offset refers to offset in target buffer, not source

                if bytesRead > 0 then
                    current <- ValueSome mem
                    return true
                else
                    current <- ValueNone
                    return false
            }
            |> Task.toValueTask

    interface IAsyncDisposable with
        member _.DisposeAsync() =
            try
                // this disposes of the mem stream
                buffered.DisposeAsync()
            finally
                // if the previous block raises, we should still try to get rid of the underlying stream
                stream.DisposeAsync().AsTask().Wait()

[<Fact(Skip = "Currently fails")>]
type ``Real world tests``(output: ITestOutputHelper) =
    [<Fact>]
    let ``Reading a 10MB buffered IAsync stream from start to finish`` () = task {
        let mutable count = 0
        use reader = AsyncBufferedReader(output, Array.init 10_485_76 byte, 256)
        let expected = Array.init 256 byte

        let ts = taskSeq {
            for data in reader do
                do count <- count + 1

                if count > 40960 then
                    failwith "Too far!!!!!!" // ensuring we don't end up in an endless loop

                yield data
        }

        // the following is extremely slow, which is why we just use F#'s comparison instead
        // Using this takes 67s, compared to 0.25s using normal F# comparison.
        do! ts |> TaskSeq.iter (should equal expected)
        do! ts |> TaskSeq.iter ((=) expected >> (should be True))
        do! task { do count |> should equal 4096 }
    }

    [<Fact>]
    let ``Reading a 10MB buffered IAsync stream from start to finish comparison`` () = task {
        // NOTE:
        // this test is meant to compare the test above for performance reasons
        // and for soundness checks

        let expected = Array.init 256 byte
        let stream = new MemoryStream(Array.init 10_485_760 byte)
        let buffered = new BufferedStream(stream, 256)
        let mutable current = true
        let mutable count = 0

        while current do
            let mem = Array.zeroCreate 256

            // this advances the "current" position automatically. However, this is clearly NOT threadsafe!!!
            let! bytesRead = buffered.ReadAsync(mem, 0, mem.Length)

            if bytesRead > 0 then
                count <- count + 1
                mem = expected |> should be True
            // the following is extremely slow
            //mem |> should equal (Array.init 256 byte)

            current <- bytesRead > 0

        count |> should equal 40960
    }


    //System.InvalidOperationException: An attempt was made to transition a task to a final state when it had already completed.
    //   at <StartupCode$FSharpy-TaskSeq-Test>.$TaskSeq.Realworld.clo@58-4.MoveNext() in D:\Projects\OpenSource\Abel\TaskSeq\src\FSharpy.TaskSeq.Test\TaskSeq.Realworld.fs:line 77
    //   at Xunit.Sdk.TestInvoker`1.<>c__DisplayClass48_0.<<InvokeTestMethodAsync>b__1>d.MoveNext() in /_/src/xunit.execution/Sdk/Frameworks/Runners/TestInvoker.cs:line 264
    //--- End of stack trace from previous location ---
    //   at Xunit.Sdk.ExecutionTimer.AggregateAsync(Func`1 asyncAction) in /_/src/xunit.execution/Sdk/Frameworks/ExecutionTimer.cs:line 48
    //   at Xunit.Sdk.ExceptionAggregator.RunAsync(Func`1 code) in /_/src/xunit.core/Sdk/ExceptionAggregator.cs:line 90\
    [<Fact(Skip = "Currently fails")>]
    let ``Reading a 1MB buffered IAsync stream from start to finish InvalidOperationException`` () = task {
        let mutable count = 0
        use reader = AsyncBufferedReader(output, Array.init 1_048_576 byte, 256)
        let expected = Array.init 256 byte

        let ts = taskSeq {
            for data in reader do
                do count <- count + 1

                if count > 40960 then
                    failwith "Too far!!!!!!" // ensuring we don't end up in an endless loop

                yield data
        }

        // the following is extremely slow, which is why we just use F#'s comparison instead
        // Using this takes 67s, compared to 0.25s using normal F# comparison.
        do! ts |> TaskSeq.iter (should equal expected)
        do! ts |> TaskSeq.iter ((=) expected >> (should be True))
        do! task { do count |> should equal 4096 }
    }
