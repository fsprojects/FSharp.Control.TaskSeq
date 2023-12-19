namespace TaskSeq.Tests

open System
open System.Threading.Tasks

open FsUnit
open NHamcrest.Core
open Microsoft.FSharp.Reflection

open Xunit
open Xunit.Sdk


[<AutoOpen>]
module ExtraCustomMatchers =
    /// Tee operator, combine multiple FsUnit-style test assertions:
    /// x |>> should be (greaterThan 12) |> should be (lessThan 42)
    let (|>>) x sideEffect =
        sideEffect x |> ignore
        x

    let private baseResultTypeTest value =
        match value with
        | null ->
            EqualException.ForMismatchedValues("Result type", "<null>", "Value <null> or None is never Result.Ok or Result.Error")
            |> raise

        | _ ->
            let ty = value.GetType()

            if ty.FullName.StartsWith "Microsoft.FSharp.Core.FSharpResult" then
                FSharpValue.GetUnionFields(value, ty) |> fst
            else
                EqualException.ForMismatchedValues("Result type", ty.Name, "Type must be Result<_, _>")
                |> raise

    let private baseOptionTypeTest value =
        match value with
        | null ->
            // An option type interpreted as obj will be <null> for None
            None

        | _ ->
            let ty = value.GetType()

            if ty.FullName.StartsWith "Microsoft.FSharp.Core.FSharpOption" then
                match (FSharpValue.GetUnionFields(value, ty) |> fst).Name with
                | "Some" -> Some()
                | "None" -> None
                | _ ->
                    raise
                    <| EqualException.ForMismatchedValues("Option type", ty.Name, "Unexpected field name for F# option type")
            else
                EqualException.ForMismatchedValues("Option type", ty.Name, "Type must be Option<_>")
                |> raise


    /// Type must be Result, value must be Result.Ok. Use with `not`` only succeeds if using the correct type.
    let Ok' =
        let check value =
            let field = baseResultTypeTest value

            match field.Name with
            | "Ok" -> true
            | _ -> false

        CustomMatcher<obj>("Result.Ok", check)

    /// Type must be Result, value must be Result.Error. Use with `not`` only succeeds if using the correct type.
    let Error' =
        let check value =
            let field = baseResultTypeTest value

            match field.Name with
            | "Error" -> true
            | _ -> false

        CustomMatcher<obj>("Result.Error", check)

    /// Succeeds for None or <null>
    let None' =
        let check value =
            baseOptionTypeTest value
            |> Option.map (fun _ -> false)
            |> Option.defaultValue true

        CustomMatcher<obj>("Option.None", check)

    /// Succeeds for any value Some. Use with `not`` only succeeds if using the correct type.
    let Some' =
        let check value =
            baseOptionTypeTest (unbox value)
            |> Option.map (fun _ -> true)
            |> Option.defaultValue false

        CustomMatcher<obj>("Option.Some", check)


    /// Succeeds if item-under-test contains any of the items in the sequence
    let anyOf (lst: 'T seq) =
        CustomMatcher<obj>($"anyOf: %A{lst}", (fun item -> lst |> Seq.contains (item :?> 'T)))

    /// <summary>
    /// Asserts any exception that matches, or is derived from the given exception <see cref="Type" />.
    /// Async exceptions are almost always nested in an <see cref="AggregateException" />, however, in an
    /// async try/catch in F#, the exception is typically unwrapped. But this is not foolproof, and
    /// in cases where we just call <see cref="Task.Wait" />, an <see cref="AggregateException" /> will be raised regardless.
    /// This assertion will go over all nested exceptions and 'self', to find a matching exception.
    /// Function to evaluate MUST return a <see cref="System.Threading.Tasks.Task" />, not a generic
    /// <see cref="Task&lt;'T>" />.
    /// Calls <see cref="Assert.ThrowsAnyAsync&lt;Exception>" /> of xUnit to ensure proper evaluation of async.
    /// </summary>
    let throwAsync (ex: Type) =
        let testForThrowing (fn: unit -> Task) = task {
            let! actualExn = Assert.ThrowsAnyAsync<Exception> fn

            match actualExn with
            | :? AggregateException as aggregateEx ->
                if Object.ReferenceEquals(ex, typeof<AggregateException>) then
                    // in case the assertion is for AggregateException itself, just accept it as Passed.
                    return true

                else
                    for ty in aggregateEx.InnerExceptions do
                        Assert.IsAssignableFrom(expectedType = ex, object = ty)

                    //Assert.Contains<Type>(expected = ex, collection = types)
                    return true // keep FsUnit happy
            | _ ->
                // checks if object is of a certain type
                Assert.IsAssignableFrom(ex, actualExn)
                return true //keep FsUnit happy
        }

        CustomMatcher<obj>(
            $"Throws %s{ex.Name} (Below, XUnit does not show actual value properly)",
            (fun fn -> (testForThrowing (fn :?> (unit -> Task))).Result)
        )

    /// <summary>
    /// This makes a test BLOCKING!!! (TODO: get a better test framework?)
    ///
    /// Asserts any exception that exactly matches the given exception <see cref="Type" />.
    /// Async exceptions are almost always nested in an <see cref="AggregateException" />, however, in an
    /// async try/catch in F#, the exception is typically unwrapped. But this is not foolproof, and
    /// in cases where we just call <see cref="Task.Wait" />, and <see cref="AggregateException" /> will be raised regardless.
    /// This assertion will go over all nested exceptions and 'self', to find a matching exception.
    ///
    /// Function to evaluate MUST return a <see cref="System.Threading.Tasks.Task" />, not a generic
    /// <see cref="Task&lt;'T>" />.
    /// Calls <see cref="Assert.ThrowsAnyAsync&lt;Exception>" /> of xUnit to ensure proper evaluation of async.
    /// </summary>
    let throwAsyncExact (ex: Type) =
        let testForThrowing (fn: unit -> Task) = task {
            let! actualExn = Assert.ThrowsAnyAsync<Exception> fn

            match actualExn with
            | :? AggregateException as aggregateEx ->
                let types =
                    aggregateEx.InnerExceptions
                    |> Seq.map (fun x -> x.GetType())

                Assert.Contains<Type>(expected = ex, collection = types)
                return true // keep FsUnit happy
            | _ ->
                // checks if object is of a certain type
                Assert.IsType(ex, actualExn)
                return true //keep FsUnit happy
        }

        CustomMatcher<obj>(
            $"Throws %s{ex.Name} (Below, XUnit does not show actual value properly)",
            (fun fn -> (testForThrowing (fn :?> (unit -> Task))).Result)
        )

    let inline assertThrows ty (f: unit -> 'U) = f >> ignore |> should throw ty
    let inline assertNullArg f = assertThrows typeof<ArgumentNullException> f
