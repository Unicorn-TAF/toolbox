using System;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public enum Status
    {
        Passed, Failed, Skipped
    }

    public struct TestResult
    {
        public TestResult(string name, Status status, DateTime start, DateTime end, string testListId, string testListName, string errorMessage)
        {
            Name = name;
            Status = status;
            StartTime = start;
            EndTime = end;
            Duration = EndTime - StartTime;
            TestListId = testListId;
            TestListName = testListName;
            ErrorMessage = errorMessage;
        }

        public string Name { get; }

        public Status Status { get;}

        public DateTime StartTime { get;}

        public DateTime EndTime { get;}

        public TimeSpan Duration { get;}

        public string TestListId { get;}

        public string TestListName { get;}

        public string ErrorMessage { get;}
    }
}
