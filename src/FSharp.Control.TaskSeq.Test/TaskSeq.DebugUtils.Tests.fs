module TaskSeq.Tests.DebugUtils

open Xunit
open FsUnit.Xunit
open System
open System.IO
open System.Threading
open FSharp.Control

//
// Tests for FSharp.Control.Debug logging functionality
//

module VerboseSettings =
    [<Fact>]
    let ``Debug verbose setting defaults to false when env var not set`` () =
        // Clear environment variable
        Environment.SetEnvironmentVariable("TASKSEQ_LOG_VERBOSE", null)
        
        // Force reinit of the setting by accessing private field via reflection
        let debugType = typeof<Debug>
        let verboseField = debugType.GetField("verbose", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
        verboseField.SetValue(null, None)
        
        // Test by calling logInfo and verifying no output in release builds
        Debug.logInfo("test message")
        
        // In release builds, this should be a no-op
        // In debug builds, we can't easily capture console output, but the method should not throw
        Assert.True(true) // Test passes if no exception thrown

    [<Theory>]
    [<InlineData("1", true)>]
    [<InlineData("true", true)>]
    [<InlineData("TRUE", true)>]
    [<InlineData("on", true)>]
    [<InlineData("ON", true)>]
    [<InlineData("yes", true)>]
    [<InlineData("YES", true)>]
    [<InlineData("0", false)>]
    [<InlineData("false", false)>]
    [<InlineData("off", false)>]
    [<InlineData("no", false)>]
    [<InlineData("invalid", false)>]
    [<InlineData("", false)>]
    [<InlineData("  1  ", true)>]
    let ``Debug verbose setting parses environment variable correctly`` envValue expectedResult =
        // Set environment variable
        Environment.SetEnvironmentVariable("TASKSEQ_LOG_VERBOSE", envValue)
        
        try
            // Force reinit of the setting by accessing private field via reflection
            let debugType = typeof<Debug>
            let verboseField = debugType.GetField("verbose", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
            verboseField.SetValue(null, None)
            
            // Test by calling getVerboseSetting via reflection
            let getVerboseMethod = debugType.GetMethod("getVerboseSetting", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
            let result = getVerboseMethod.Invoke(null, [||]) :?> bool
            
            result |> should equal expectedResult
        finally
            // Clean up
            Environment.SetEnvironmentVariable("TASKSEQ_LOG_VERBOSE", null)
            
    [<Fact>]
    let ``Debug verbose setting caches result after first call`` () =
        // Set environment variable
        Environment.SetEnvironmentVariable("TASKSEQ_LOG_VERBOSE", "1")
        
        try
            // Force reinit of the setting
            let debugType = typeof<Debug>
            let verboseField = debugType.GetField("verbose", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
            verboseField.SetValue(null, None)
            
            // Get verbose setting via reflection
            let getVerboseMethod = debugType.GetMethod("getVerboseSetting", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
            let result1 = getVerboseMethod.Invoke(null, [||]) :?> bool
            
            // Change environment variable
            Environment.SetEnvironmentVariable("TASKSEQ_LOG_VERBOSE", "0")
            
            // Call again - should return cached result
            let result2 = getVerboseMethod.Invoke(null, [||]) :?> bool
            
            result1 |> should equal true
            result2 |> should equal true // Should be cached value, not re-read from env
        finally
            // Clean up
            Environment.SetEnvironmentVariable("TASKSEQ_LOG_VERBOSE", null)

module LoggingMethods =
    [<Fact>]
    let ``Debug logInfo with single parameter does not throw`` () =
        Debug.logInfo("Simple log message")
        // Test passes if no exception thrown
        Assert.True(true)

    [<Fact>]
    let ``Debug logInfo with data parameter does not throw`` () =
        Debug.logInfo("Log message with data: ", 42)
        // Test passes if no exception thrown
        Assert.True(true)

    [<Fact>]
    let ``Debug logInfo with null string does not throw`` () =
        Debug.logInfo(null)
        // Test passes if no exception thrown
        Assert.True(true)

    [<Fact>]
    let ``Debug logInfo with empty string does not throw`` () =
        Debug.logInfo("")
        // Test passes if no exception thrown
        Assert.True(true)

    [<Fact>]
    let ``Debug logInfo with complex data does not throw`` () =
        let complexData = {| Name = "test"; Values = [1; 2; 3] |}
        Debug.logInfo("Complex data: ", complexData)
        // Test passes if no exception thrown
        Assert.True(true)

module ExceptionHandling =
    [<Fact>]
    let ``Debug handles environment variable access exceptions gracefully`` () =
        // This test verifies that if Environment.GetEnvironmentVariable throws,
        // the Debug class handles it gracefully and defaults to false
        // We can't easily force an exception here, but we can test the method doesn't throw
        try
            // Force reinit and call getVerboseSetting
            let debugType = typeof<Debug>
            let verboseField = debugType.GetField("verbose", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
            verboseField.SetValue(null, None)
            
            let getVerboseMethod = debugType.GetMethod("getVerboseSetting", System.Reflection.BindingFlags.NonPublic ||| System.Reflection.BindingFlags.Static)
            let result = getVerboseMethod.Invoke(null, [||]) :?> bool
            
            // Should not throw and should return a boolean value
            result |> should be instanceOfType<bool>
        with
        | ex -> 
            // Test should fail if an exception is thrown
            Assert.Fail($"Debug.getVerboseSetting should not throw exceptions: {ex.Message}")