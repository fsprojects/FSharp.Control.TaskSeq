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

type ``Real world tests``(output: ITestOutputHelper) =
    [<Fact>]
    let ``Reading a 10MB buffered IAsync stream from start to finish`` () = task {
        let mutable count = 0
        use reader = AsyncBufferedReader(output, Array.init 10_485_760 byte, 256)
        let expected = Array.init 256 byte

        let ts = taskSeq {
            for data in reader do
                do count <- count + 1

                if count > 40960 then
                    failwith "Too far!!!!!!" // ensuring we don't end up in an endless loop

                yield data
        }

        // used fold as a `TaskSeq.iter`
        let! all = TaskSeq.toArrayAsync ts

        for a in all do
            a = expected |> should be True

        // the following is extremely slow:
        //a |> should equal expected

        do! task { do count |> should equal 40960 }
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
