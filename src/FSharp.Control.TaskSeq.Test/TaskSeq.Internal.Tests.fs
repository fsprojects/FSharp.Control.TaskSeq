module TaskSeq.Tests.Internal

open System
open System.Reflection
open System.Threading.Tasks
open System.Collections.Generic
open System.Threading

open Xunit
open FsUnit.Xunit

open FSharp.Control

//
// NOTE: This test module tests internal functions in TaskSeqInternal module
// using reflection where necessary, as these functions are not exposed publicly
//

[<Fact>]
let ``checkNonNull should throw ArgumentNullException for null argument`` () =
    // Get internal module type and method
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let checkNonNullMethod = internalType.GetMethod("checkNonNull", BindingFlags.Static ||| BindingFlags.Public)
    
    // Test with null argument
    let testAction() = 
        checkNonNullMethod.Invoke(null, [| "testArg"; null |]) |> ignore
    
    testAction |> should throwWithMessage<ArgumentNullException> "testArg"

[<Fact>]
let ``checkNonNull should not throw for non-null argument`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let checkNonNullMethod = internalType.GetMethod("checkNonNull", BindingFlags.Static ||| BindingFlags.Public)
    
    // Test with non-null argument
    let result = checkNonNullMethod.Invoke(null, [| "testArg"; "validValue" |])
    result |> should equal null // void return

[<Fact>]
let ``raiseEmptySeq should throw correct ArgumentException`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let raiseEmptySeqMethod = internalType.GetMethod("raiseEmptySeq", BindingFlags.Static ||| BindingFlags.Public)
    
    let testAction() = raiseEmptySeqMethod.Invoke(null, [||]) |> ignore
    
    let ex = Assert.Throws<TargetInvocationException>(testAction)
    let innerEx = ex.InnerException :?> ArgumentException
    innerEx.ParamName |> should equal "source"
    innerEx.Message |> should contain "empty"

[<Fact>]
let ``raiseCannotBeNegative should throw for negative values`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("raiseCannotBeNegative", BindingFlags.Static ||| BindingFlags.Public)
    
    let testAction() = method.Invoke(null, [| "count"; -1 |]) |> ignore
    
    let ex = Assert.Throws<TargetInvocationException>(testAction)
    let innerEx = ex.InnerException :?> ArgumentException
    innerEx.ParamName |> should equal "count"
    innerEx.Message |> should contain "non-negative"

[<Fact>]
let ``raiseCannotBeNegative should not throw for zero`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("raiseCannotBeNegative", BindingFlags.Static ||| BindingFlags.Public)
    
    let result = method.Invoke(null, [| "count"; 0 |])
    result |> should equal null

[<Fact>]
let ``raiseCannotBeNegative should not throw for positive values`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("raiseCannotBeNegative", BindingFlags.Static ||| BindingFlags.Public)
    
    let result = method.Invoke(null, [| "count"; 42 |])
    result |> should equal null

[<Fact>]
let ``raiseOutOfBounds should throw correct ArgumentException`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("raiseOutOfBounds", BindingFlags.Static ||| BindingFlags.Public)
    
    let testAction() = method.Invoke(null, [| "index" |]) |> ignore
    
    let ex = Assert.Throws<TargetInvocationException>(testAction)
    let innerEx = ex.InnerException :?> ArgumentException
    innerEx.ParamName |> should equal "index"
    innerEx.Message |> should contain "bounds"

[<Fact>]
let ``raiseInsufficient should throw correct ArgumentException`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("raiseInsufficient", BindingFlags.Static ||| BindingFlags.Public)
    
    let testAction() = method.Invoke(null, [||]) |> ignore
    
    let ex = Assert.Throws<TargetInvocationException>(testAction)
    let innerEx = ex.InnerException :?> ArgumentException
    innerEx.ParamName |> should equal "source"
    innerEx.Message |> should contain "insufficient"

[<Fact>]
let ``raiseNotFound should throw KeyNotFoundException`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("raiseNotFound", BindingFlags.Static ||| BindingFlags.Public)
    
    let testAction() = method.Invoke(null, [||]) |> ignore
    
    let ex = Assert.Throws<TargetInvocationException>(testAction)
    let innerEx = ex.InnerException :?> KeyNotFoundException
    innerEx.Message |> should contain "predicate"

[<Fact>]
let ``isEmpty should return true for empty sequence`` () = task {
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("isEmpty", BindingFlags.Static ||| BindingFlags.Public)
    
    let emptySeq = TaskSeq.empty<int>
    let result = method.Invoke(null, [| emptySeq |]) :?> Task<bool>
    let! isEmpty = result
    isEmpty |> should equal true
}

[<Fact>]
let ``isEmpty should return false for non-empty sequence`` () = task {
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("isEmpty", BindingFlags.Static ||| BindingFlags.Public)
    
    let nonEmptySeq = TaskSeq.singleton 42
    let result = method.Invoke(null, [| nonEmptySeq |]) :?> Task<bool>
    let! isEmpty = result
    isEmpty |> should equal false
}

[<Fact>]
let ``empty should create proper empty sequence`` () = task {
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let property = internalType.GetProperty("empty", BindingFlags.Static ||| BindingFlags.Public)
    
    let emptySeq = property.GetValue(null) :?> TaskSeq<int>
    
    use enumerator = emptySeq.GetAsyncEnumerator(CancellationToken.None)
    let! hasItems = enumerator.MoveNextAsync()
    hasItems |> should equal false
}

[<Fact>]
let ``singleton should create sequence with single item`` () = task {
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("singleton", BindingFlags.Static ||| BindingFlags.Public)
    
    let singletonSeq = method.Invoke(null, [| 42 |]) :?> TaskSeq<int>
    
    use enumerator = singletonSeq.GetAsyncEnumerator(CancellationToken.None)
    
    // Should have first item
    let! hasFirst = enumerator.MoveNextAsync()
    hasFirst |> should equal true
    enumerator.Current |> should equal 42
    
    // Should not have second item
    let! hasSecond = enumerator.MoveNextAsync()
    hasSecond |> should equal false
}

[<Fact>]
let ``moveFirstOrRaiseUnsafe should not throw for non-empty sequence`` () = task {
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("moveFirstOrRaiseUnsafe", BindingFlags.Static ||| BindingFlags.Public)
    
    let seq = TaskSeq.singleton 42
    use enumerator = seq.GetAsyncEnumerator(CancellationToken.None)
    
    let result = method.Invoke(null, [| enumerator |]) :?> Task
    do! result
    
    enumerator.Current |> should equal 42
}

[<Fact>]
let ``moveFirstOrRaiseUnsafe should throw for empty sequence`` () = task {
    let assembly = typeof<TaskSeq<int>>.Assembly
    let internalType = assembly.GetType("FSharp.Control.TaskSeqInternal")
    let method = internalType.GetMethod("moveFirstOrRaiseUnsafe", BindingFlags.Static ||| BindingFlags.Public)
    
    let emptySeq = TaskSeq.empty<int>
    use enumerator = emptySeq.GetAsyncEnumerator(CancellationToken.None)
    
    let result = method.Invoke(null, [| enumerator |]) :?> Task
    
    let ex = Assert.ThrowsAsync<ArgumentException>(fun () -> result)
    let! exception = ex
    exception.ParamName |> should equal "source"
    exception.Message |> should contain "empty"
}

// Test internal discriminated union types
[<Fact>]
let ``AsyncEnumStatus enum values should exist`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let statusType = assembly.GetType("FSharp.Control.AsyncEnumStatus")
    
    statusType |> should not' (equal null)
    statusType.IsEnum |> should equal false // It's a DU, not an enum
    statusType.IsValueType |> should equal true // Struct DU

[<Fact>] 
let ``TakeOrSkipKind enum values should exist`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let kindType = assembly.GetType("FSharp.Control.TakeOrSkipKind")
    
    kindType |> should not' (equal null)
    kindType.IsValueType |> should equal true

[<Fact>]
let ``Action union type should exist`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let actionType = assembly.GetTypes() |> Array.find (fun t -> t.Name.StartsWith("Action") && t.IsValueType)
    
    actionType |> should not' (equal null)
    actionType.IsValueType |> should equal true

[<Fact>]
let ``FolderAction union type should exist`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let folderType = assembly.GetTypes() |> Array.find (fun t -> t.Name.StartsWith("FolderAction") && t.IsValueType)
    
    folderType |> should not' (equal null)
    folderType.IsValueType |> should equal true

[<Fact>]
let ``ChooserAction union type should exist`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly 
    let chooserType = assembly.GetTypes() |> Array.find (fun t -> t.Name.StartsWith("ChooserAction") && t.IsValueType)
    
    chooserType |> should not' (equal null)
    chooserType.IsValueType |> should equal true

[<Fact>]
let ``PredicateAction union type should exist`` () =
    let assembly = typeof<TaskSeq<int>>.Assembly
    let predType = assembly.GetTypes() |> Array.find (fun t -> t.Name.StartsWith("PredicateAction") && t.IsValueType)
    
    predType |> should not' (equal null)
    predType.IsValueType |> should equal true