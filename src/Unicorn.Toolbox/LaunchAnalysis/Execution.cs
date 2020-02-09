using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class Execution
    {
        /// <summary>
        /// Represents execution i.e. one TRX file
        /// </summary>
        /// <param name="name">Name of the TRX file</param>
        /// <param name="testResults">List of objects representing individual tests in TRX, before suite and i.e. not included</param>
        /// <param name="durationFull">TRX finish - TRX start</param>
        public Execution(string name, ImmutableList<TestResult> testResults, TimeSpan durationFull)
        {
            Name = name;
            TestResults = testResults;
            DurationFull = durationFull;
        }

        public string Name { get; }

        public ImmutableList<TestResult> TestResults { get; private set; }

        /// <summary>
        /// Just a sum of tests without before suites and time gaps between
        /// <para>Used for data binding</para>
        /// </summary>
        public double DurationMin => TimeSpan.FromSeconds(TestResults.Sum(tr => tr.Duration.TotalSeconds)).TotalMinutes;

        /// <summary>
        /// TRX finish time - TRX start time
        /// <para>Includes before suites and time between tests</para>
        /// </summary>
        public TimeSpan DurationFull { get; }

        /// <summary>
        /// Used for data binding
        /// </summary>
        public double DurationFullMin => DurationFull.TotalMinutes;

        /// <summary>
        /// Used for data binding
        /// </summary>
        public int TestsCount => TestResults.Count;

        /// <summary>
        /// Used for data binding
        /// </summary>
        public int SuitesCount => new HashSet<string>(TestResults.Select(tr => tr.TestListId)).Count;
    }
}
