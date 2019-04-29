using System;

namespace Unicorn.Toolbox.LaunchAnalasys
{
    public struct TestResult
    {
        public TestResult(string name, DateTime start, DateTime end)
        {
            this.Name = name;
            this.StartTime = start;
            this.EndTime = end;
            this.Duration = this.EndTime - this.StartTime;
        }

        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
