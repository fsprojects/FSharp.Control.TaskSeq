namespace FSharp.Control.Operators

open System.Threading.Tasks
open System

open FSharp.Control

// "The '&&' should not normally be redefined. Consider using a different operator name.
// "The '||' should not normally be redefined. Consider using a different operator name.
#nowarn "86"

[<Struct>]
type TaskAndOperator =
    | TaskAndOperator

    // original. In release builds, this is optimized just like the original '&&' operator and the SCDU disappears
    static member inline (?<-)(_: TaskAndOperator, leftOp: bool, rightOp: bool) = leftOp && rightOp

    static member (?<-)(TaskAndOperator, leftOp: bool, rightOp: ValueTask<bool>) = if leftOp then rightOp else ValueTask.False

    static member (?<-)(TaskAndOperator, leftOp: ValueTask<bool>, rightOp: bool) =
        // while it may be more efficient to evaluate rh-side first, we should honor order of operations!
        if leftOp.IsCompletedSuccessfully && leftOp.Result then
            ValueTask.fromResult rightOp
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return rightOp else return false
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskAndOperator, leftOp: bool, rightOp: #Task<bool>) = if leftOp then ValueTask.ofTask rightOp else ValueTask.False

    static member (?<-)(TaskAndOperator, leftOp: #Task<bool>, rightOp: bool) =
        // while it may be more efficient to evaluate rh-side first, we should honor order of operations!
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then
                ValueTask.fromResult rightOp
            else
                ValueTask.False
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return rightOp else return false
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskAndOperator, leftOp: ValueTask<_>, rightOp: ValueTask<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then rightOp else ValueTask.False
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return! rightOp else return false
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskAndOperator, leftOp: #Task<_>, rightOp: ValueTask<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then rightOp else ValueTask.False
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return! rightOp else return false
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskAndOperator, leftOp: ValueTask<_>, rightOp: #Task<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then
                ValueTask.ofTask rightOp
            else
                ValueTask.False
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return! rightOp else return false
            }
            |> ValueTask.ofTask

    static member (?<-)(_: TaskAndOperator, leftOp: #Task<_>, rightOp: #Task<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then
                ValueTask.ofTask rightOp
            else
                ValueTask.False
        else
            task {
                let! leftOperand = leftOp
                if not leftOperand then return false else return! rightOp
            }
            |> ValueTask.ofTask


[<Struct>]
type TaskOrOperator =
    | TaskOrOperator

    // original. In release builds, this is optimized just like the original '||' operator and the SCDU disappears
    static member inline (?<-)(_, leftOp: bool, rightOp: bool) = leftOp || rightOp

    static member (?<-)(TaskOrOperator, leftOp: bool, rightOp: ValueTask<bool>) =
        // simple
        if leftOp then ValueTask.True else rightOp

    static member (?<-)(TaskOrOperator, leftOp: ValueTask<bool>, rightOp: bool) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then
                ValueTask.True
            else
                ValueTask.fromResult rightOp
        else
            task {
                let! leftOperand = leftOp
                return leftOperand || rightOp
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskOrOperator, leftOp: bool, rightOp: #Task<bool>) = if leftOp then ValueTask.True else ValueTask.ofTask rightOp

    static member (?<-)(TaskOrOperator, leftOp: #Task<bool>, rightOp: bool) =
        // while it may be more efficient to evaluate rh-side first, we should honor order of operations!
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then
                ValueTask.True
            else
                ValueTask.fromResult rightOp
        else
            task {
                let! leftOperand = leftOp
                return leftOperand || rightOp
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskOrOperator, leftOp: ValueTask<_>, rightOp: ValueTask<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then ValueTask.True else rightOp
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return true else return! rightOp
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskOrOperator, leftOp: #Task<_>, rightOp: ValueTask<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then ValueTask.True else rightOp
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return true else return! rightOp
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskOrOperator, leftOp: ValueTask<_>, rightOp: #Task<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then
                ValueTask.True
            else
                ValueTask.ofTask rightOp
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return true else return! rightOp
            }
            |> ValueTask.ofTask

    static member (?<-)(TaskOrOperator, leftOp: #Task<_>, rightOp: #Task<_>) =
        if leftOp.IsCompletedSuccessfully then
            if leftOp.Result then
                ValueTask.True
            else
                ValueTask.ofTask rightOp
        else
            task {
                let! leftOperand = leftOp
                if leftOperand then return true else return! rightOp
            }
            |> ValueTask.ofTask

[<AutoOpen>]
module OperatorOverloads =

    /// Binary 'and'. When used as a binary operator, the right-hand operand is evaluated only on demand.
    let inline (&&) leftOp rightOp : 'T = ((?<-) TaskAndOperator leftOp rightOp) // SCDU will get erased in release builds

    /// Binary 'or'. When used as a binary operator, the right-hand operand is evaluated only on demand.
    let inline (||) leftOp rightOp : 'T = ((?<-) TaskOrOperator leftOp rightOp) // SCDU will get erased in release builds
