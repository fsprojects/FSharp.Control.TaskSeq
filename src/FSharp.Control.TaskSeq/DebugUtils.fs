namespace FSharp.Control

open System.Threading.Tasks
open System
open System.Diagnostics
open System.Threading

type Debug =

    [<DefaultValue(false)>]
    static val mutable private verbose: bool option

    /// Setting from environment variable TASKSEQ_LOG_VERBOSE, which,
    /// when set, enables (very) verbose printing of flow and state
    static member private getVerboseSetting() =
        match Debug.verbose with
        | None ->
            let verboseEnv =
                try
                    match Environment.GetEnvironmentVariable "TASKSEQ_LOG_VERBOSE" with
                    | null -> false
                    | x ->
                        match x.ToLowerInvariant().Trim() with
                        | "1"
                        | "true"
                        | "on"
                        | "yes" -> true
                        | _ -> false

                with _ ->
                    false

            Debug.verbose <- Some verboseEnv
            verboseEnv

        | Some setting -> setting

    /// Private helper to log to stdout in DEBUG builds only
    [<Conditional("DEBUG")>]
    static member private print value =
        match Debug.getVerboseSetting () with
        | false -> ()
        | true ->
            // don't use ksprintf here, because the compiler does not remove all allocations due to
            // the way PrintfFormat types are compiled, even if we set the Conditional attribute.
            let ct = Thread.CurrentThread
            printfn "%i (%b): %s" ct.ManagedThreadId ct.IsThreadPoolThread value

    /// Log to stdout in DEBUG builds only
    [<Conditional("DEBUG")>]
    static member logInfo(str) = Debug.print str

    /// Log to stdout in DEBUG builds only
    [<Conditional("DEBUG")>]
    static member logInfo(str, data) = Debug.print $"%s{str}{data}"
