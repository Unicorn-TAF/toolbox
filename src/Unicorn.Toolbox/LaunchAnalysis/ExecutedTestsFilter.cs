using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class ExecutedTestsFilter
    {
        public Dictionary<string, IEnumerable<TestResult>> FilteredData { get; } = 
            new Dictionary<string, IEnumerable<TestResult>>();

        public int MatchingTestsCount { get; private set; }

        public void FilterByFailMessage(IEnumerable<TestResult> testResults, string searchString, bool isRegex)
        {
            MatchingTestsCount = 0;
            FilteredData.Clear();

            IEnumerable<TestResult> results = 
                testResults.Where(r => r.Status.Equals(Status.Failed) && ErrorMatch(r.ErrorMessage));

            IEnumerable<string> uniqueSuites = results.Select(r => r.SuiteName).Distinct();

            foreach (string suite in uniqueSuites)
            {
                IEnumerable<TestResult> matchingResults = results.Where(r => r.SuiteName.Equals(suite));
                FilteredData.Add(suite, matchingResults);
                MatchingTestsCount += matchingResults.Count();
            }

            bool ErrorMatch(string error) => 
                isRegex ? Regex.IsMatch(error, searchString) : error.Contains(searchString);
        }

        public void FilterByTime(IEnumerable<TestResult> testResults, string dateTimeString)
        {
            DateTime time = Convert.ToDateTime(dateTimeString);
            IEnumerable<TestResult> results = testResults.Where(r => r.StartTime < time && r.EndTime > time);

            MatchingTestsCount = results.Count();
            FilteredData.Clear();

            foreach (TestResult test in results)
            {
                FilteredData.Add(test.SuiteName, new[] { test });
            }
        }

        public static IEnumerable<IEnumerable<TestResult>> GetTopErrors(IEnumerable<TestResult> testResults) =>
            (from test in testResults
            where test.Status.Equals(Status.Failed)
            group test by test.ErrorMessage into testGroup
            orderby testGroup.Count() descending
            select testGroup)
            .Take(Math.Min(5, testResults.Count()));
    }
}
