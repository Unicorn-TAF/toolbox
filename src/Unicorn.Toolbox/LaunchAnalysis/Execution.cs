using System;
using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class Execution
    {
        public Execution(string name)
        {
            Name = name;
            TestResults = new List<TestResult>();
        }

        public string Name { get; protected set; }

        public List<TestResult> TestResults { get; }

        public double DurationMin => TimeSpan.FromSeconds(TestResults.Sum(tr => tr.Duration.TotalSeconds)).TotalMinutes;

        public int TestsCount => TestResults.Count;

        public int SuitesCount => new HashSet<string>(TestResults.Select(tr => tr.TestListId)).Count;
    }
}
