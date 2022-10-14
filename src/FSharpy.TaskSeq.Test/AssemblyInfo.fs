namespace FSharpy.Tests

open System.Runtime.CompilerServices

[<assembly: Xunit.CollectionBehavior(DisableTestParallelization = false)>]
[<assembly: Xunit.TestCaseOrderer("FSharpy.Tests.AlphabeticalOrderer", "FSharpy.TaskSeq.Test")>]

do ()
