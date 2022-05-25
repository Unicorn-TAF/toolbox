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
                DateTime utcStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

                double earliestTime = double.MaxValue;
                double latestTime = double.MinValue;

                foreach (Execution execution in Executions)
                {
                    double min = execution.TestResults.Min(r => r.StartTime).ToUniversalTime().Subtract(utcStart).TotalMilliseconds;
                    earliestTime = Math.Min(earliestTime, min);

                    double max = execution.TestResults.Max(r => r.EndTime).ToUniversalTime().Subtract(utcStart).TotalMilliseconds;
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
        
        public void Clear()
        {
            Executions.Clear();
        }

        public void AppendResultsFromTrx(string trxFile)
        {
            TrxParser trxParser = new TrxParser(trxFile);
            var results = trxParser.GetTestsData();

            if (results.Any())
            {
                Execution exeution = new Execution(Path.GetFileNameWithoutExtension(trxFile), results, trxParser.GetLaunchDuration());
                Executions.Add(exeution);
            }
        }

        public override string ToString()
        {
            double durationMinutes = LaunchDuration / 60000;
            double durationHours = durationMinutes / 60;
            double executionSumHours = ExecutionsSumMinutes / 60;

            return new StringBuilder()
                .AppendFormat("Executed threads: {0}\n", Executions.Count)
                .AppendFormat("Executed suites: {0}\n", ExecutedSuites)
                .AppendFormat("Executed tests: {0} (Failed: {1}, Skipped: {2})\n", ExecutedTests, FailedTests, SkippedTests)
                .AppendFormat("Launch duration: {0:F1} minutes ({1:F1} hrs.)\n", durationMinutes, durationHours)
                .AppendFormat("Total execution time: {0:F1} minutes ({1:F1} hrs.)", ExecutionsSumMinutes, executionSumHours)
                .ToString();
        }
    }
}
