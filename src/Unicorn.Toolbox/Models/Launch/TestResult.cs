using System;

namespace Unicorn.Toolbox.Models.Launch;

public enum Status
{
    Passed, 
    Failed, 
    Skipped
}

public readonly struct TestResult
{
    public TestResult(string name, 
        Status status, 
        DateTime start, 
        DateTime end, 
        string suiteId, 
        string suiteName, 
        string errorMessage)
    {
        Name = name;
        Status = status;
        StartTime = start;
        EndTime = end;
        Duration = EndTime - StartTime;
        SuiteId = suiteId;
        SuiteName = suiteName;
        ErrorMessage = errorMessage;
    }

    public string Name { get; }

    public Status Status { get; }

    public DateTime StartTime { get; }

    public DateTime EndTime { get; }

    public TimeSpan Duration { get; }

    public string SuiteId { get; }

    public string SuiteName { get; }

    public string ErrorMessage { get; }
}
