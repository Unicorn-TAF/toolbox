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

        public void AppendResultsFromTrx(string trxFile)
        {
            var results = new TrxParser(trxFile).GetAllTests();

            if (results.Any())
            {
                var exeution = new Execution(Path.GetFileNameWithoutExtension(trxFile));
                exeution.TestResults.AddRange(results);
                Executions.Add(exeution);
            }
        }

        public override string ToString()
        {
            var durationMinutes = LaunchDuration / 60000;
            var durationHours = durationMinutes / 60;
            return new StringBuilder()
                .AppendFormat("Threads: {0}\n", Executions.Count)
                .AppendFormat("Launch duration: {0:F1} minutes ({1:F1} h.)\n", durationMinutes, durationHours)
                .AppendFormat("Total execution time: {0:F1} minutes", Executions.SelectMany(e => e.TestResults).Sum(tr => tr.Duration.TotalMinutes))
                .ToString();
        }

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
    }
}
