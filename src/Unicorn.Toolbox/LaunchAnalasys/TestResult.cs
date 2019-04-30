using System;

namespace Unicorn.Toolbox.LaunchAnalasys
{
    public struct TestResult
    {
        public TestResult(string name, DateTime start, DateTime end, string testListId)
        {
            this.Name = name;
            this.StartTime = start;
            this.EndTime = end;
            this.Duration = this.EndTime - this.StartTime;
            this.TestListId = testListId;
        }

        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan Duration { get; set; }

        public string TestListId { get; set; }
    }
}
