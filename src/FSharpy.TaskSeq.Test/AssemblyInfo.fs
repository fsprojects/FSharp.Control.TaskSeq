namespace TaskSeq.Tests

open System.Runtime.CompilerServices

[<assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)>]
[<assembly: Xunit.TestCaseOrderer("FSharpy.Tests.AlphabeticalOrderer", "FSharpy.TaskSeq.Test")>]

do ()
