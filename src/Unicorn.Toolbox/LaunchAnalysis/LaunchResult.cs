using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class LaunchResult
    {
        public LaunchResult()
        {
            Executions = new List<Execution>();
        }

        public List<Execution> Executions { get; }

        public double LaunchDuration
        {
            get
            {
                var utcStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                var earliestTime = double.MaxValue;
                var latestTime = double.MinValue;

                foreach (var execution in Executions)
                {
                    var min = execution.TestResults.Min(r => r.StartTime).ToUniversalTime().Subtract(utcStart).TotalMilliseconds;
                    earliestTime = Math.Min(earliestTime, min);

                    var max = execution.TestResults.Max(r => r.EndTime).ToUniversalTime().Subtract(utcStart).TotalMilliseconds;
                    latestTime = Math.Max(latestTime, max);
                }

                return latestTime - earliestTime;
            }
        }

        private double ExecutionsSumMinutes => Executions.Sum(e => e.DurationFull.TotalMinutes);

        private int ExecutedTests => Executions.Sum(e => e.TestsCount);

        private int FailedTests => Executions.Sum(e => e.FailedTests);

        private int SkippedTests => Executions.Sum(e => e.SkippedTests);

        private int ExecutedSuites => Executions.Sum(e => e.SuitesCount);
        
        public void AppendResultsFromTrx(string trxFile)
        {
            TrxParser trxParser = new TrxParser(trxFile);
            var results = trxParser.AllTests;

            if (results.Any())
            {
                var exeution = new Execution(Path.GetFileNameWithoutExtension(trxFile), results, trxParser.TrxDuration);
                Executions.Add(exeution);
            }
        }

        public override string ToString()
        {
            var durationMinutes = LaunchDuration / 60000;
            var durationHours = durationMinutes / 60;
            var executionSumHours = ExecutionsSumMinutes / 60;
            return new StringBuilder()
                .AppendFormat("Threads: {0}\n", Executions.Count)
                .AppendFormat("Executed suites: {0}\n", ExecutedSuites)
                .AppendFormat("Executed tests: {0} (Failed: {1}, Skipped: {2})\n", ExecutedTests, FailedTests, SkippedTests)
                .AppendFormat("Launch duration: {0:F1} minutes ({1:F1} hrs.)\n", durationMinutes, durationHours)
                .AppendFormat("Total execution time: {0:F1} minutes ({1:F1} hrs.)", ExecutionsSumMinutes, executionSumHours)
                .ToString();
        }
    }
}
