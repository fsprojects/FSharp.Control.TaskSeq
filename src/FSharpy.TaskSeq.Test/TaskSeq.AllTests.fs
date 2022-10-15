namespace FSharpy.Tests

open System
open System.Threading.Tasks
open System.Reflection

open Xunit
open FsUnit.Xunit
open FsToolkit.ErrorHandling

open FSharpy
open Xunit.Abstractions


type AllTests(output: ITestOutputHelper) =
    let createParallelRunner () =
        let myAsm = Assembly.GetExecutingAssembly()

        let allMethods = [
            for ty in myAsm.DefinedTypes do
                for mem in ty.DeclaredMembers do
                    match mem.MemberType with
                    | MemberTypes.Method ->
                        if mem.Name.StartsWith("TaskSeq") || mem.Name.StartsWith("CE") then
                            yield ty, mem :?> MethodInfo
                    | _ -> ()
        ]

        let all = seq {
            for (ty, method) in allMethods do
                let ctor = ty.GetConstructor [| typeof<ITestOutputHelper> |]

                if isNull ctor then
                    failwith "Constructor for test not found"

                let testObj = ctor.Invoke([| output |])

                if method.ReturnType.Name.Contains "Task" then
                    //task {
                    //    let! x = Async.StartChildAsTask (Async.ofTask (method.Invoke(testObj, null) :?> Task<unit>))
                    //    return! x
                    //}
                    async {
                        return!
                            method.Invoke(testObj, null) :?> Task<unit>
                            |> Async.AwaitTask
                    }
                else
                    async { return method.Invoke(testObj, null) |> ignore }
        }

        all |> Async.Parallel |> Async.map ignore

    let multiply f x =
        seq {
            for i in [ 0..x ] do
                yield f ()
        }
        |> Async.Parallel
        |> Async.map ignore

    [<Fact>]
    let ``Run all tests 1 times in parallel`` () = task { do! multiply createParallelRunner 1 }

    [<Theory>]
    [<InlineData 1; InlineData 2; InlineData 3; InlineData 4; InlineData 5; InlineData 6; InlineData 7; InlineData 8>]
    let ``Run all tests X times in parallel`` i = task { do! multiply createParallelRunner i }

    [<Theory>]
    [<InlineData 1; InlineData 2; InlineData 3; InlineData 4; InlineData 5; InlineData 6; InlineData 7; InlineData 8>]
    let ``Run all tests again X times in parallel`` i = task { do! multiply createParallelRunner i }

    [<Theory>]
    [<InlineData 1; InlineData 2; InlineData 3; InlineData 4; InlineData 5; InlineData 6; InlineData 7; InlineData 8>]
    let ``Run all tests and once more, X times in parallel`` i = task { do! multiply createParallelRunner i }


//[<Fact>]
//let ``Run all tests 3 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 4 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 5 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 6 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 7 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 8 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 9 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 10 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 11 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 12 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 13 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 14 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously


//[<Fact>]
//let ``Run all tests 15 times in parallel`` () =
//    multiply createParallelRunner 15
//    |> Async.RunSynchronously
