using System;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public struct TestResult
    {
        public TestResult(string name, Status status, DateTime start, DateTime end, string testListId, string testListName)
        {
            Name = name;
            Status = status;
            StartTime = start;
            EndTime = end;
            Duration = EndTime - StartTime;
            TestListId = testListId;
            TestListName = testListName;
        }

        public string Name { get; set; }

        public Status Status { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string TestListId { get; set; }

        public string TestListName { get; set; }
    }
}
