using System;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public struct TestResult
    {
        public TestResult(string name, DateTime start, DateTime end, string testListId, string testListName)
        {
            Name = name;
            StartTime = start;
            EndTime = end;
            Duration = EndTime - StartTime;
            TestListId = testListId;
            TestListName = testListName;
        }

        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string TestListId { get; set; }

        public string TestListName { get; set; }
    }
}
