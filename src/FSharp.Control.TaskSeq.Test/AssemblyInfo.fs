namespace TaskSeq.Tests

open System.Runtime.CompilerServices

// this prevents an XUnit bug to break over itself on CI
// tests themselves can be run in parallel just fine.
[<assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)>]

do ()
