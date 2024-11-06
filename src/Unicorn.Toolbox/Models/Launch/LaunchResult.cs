﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Unicorn.Toolbox.Models.Launch;

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
            DateTime utcStart = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        StringBuilder launch = new StringBuilder();

        launch.Append($"threads: {Executions.Count}  |  ")
            .Append($"suites: {ExecutedSuites}  |  ")
            .Append($"tests: {ExecutedTests} ({FailedTests} failed, {SkippedTests} skipped)  |  ")
            .Append($"launch duration: {durationMinutes:F1} min. ({durationHours:F1} hrs.)  |  ")
            .Append($"total execution time: {ExecutionsSumMinutes:F1} min. ({executionSumHours:F1} hrs.)");

        return launch.ToString();
    }
}
