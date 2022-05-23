using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class ExecutedTestsFilter
    {
        //private readonly string _searchString;
        //private readonly bool _isRegex;

        //public ExecutedTestsFilter(string searchString, bool isRegex)
        //{
        //    _searchString = searchString;
        //    _isRegex = isRegex;
        //}

        //public ExecutedTestsFilter(string dateTimeString)
        //{
        //    _searchString = dateTimeString;
        //}

        public Dictionary<string, IEnumerable<TestResult>> FilteredResults { get; protected set; }

        public int MatchingTestsCount { get; protected set; }

        public void FilterTestsByFailMessage(IEnumerable<TestResult> testResults, string searchString, bool isRegex)
        {
            var results = isRegex ?
                testResults.Where(r => r.Status.Equals(Status.Failed) && Regex.IsMatch(r.ErrorMessage, searchString)) :
                testResults.Where(r => r.Status.Equals(Status.Failed) && r.ErrorMessage.Contains(searchString));

            var uniqueSuites = new HashSet<string>(results.Select(r => r.TestListName));

            MatchingTestsCount = 0;
            FilteredResults = new Dictionary<string, IEnumerable<TestResult>>();

            foreach (var suite in uniqueSuites)
            {
                var matchingResults = results.Where(r => r.TestListName.Equals(suite));
                FilteredResults.Add(suite, matchingResults);
                MatchingTestsCount += matchingResults.Count();
            }
        }

        public void FilterTestsByTime(IEnumerable<TestResult> testResults, string dateTimeString)
        {
            var time = Convert.ToDateTime(dateTimeString);
            var results = testResults.Where(r => r.StartTime < time && r.EndTime > time);

            MatchingTestsCount = results.Count();
            FilteredResults = new Dictionary<string, IEnumerable<TestResult>>();

            foreach (var test in results)
            {
                FilteredResults.Add(test.TestListName, new[] { test });
            }
        }

        public static IEnumerable<IEnumerable<TestResult>> GetTopErrors(IEnumerable<TestResult> testResults) =>
            (from test in testResults
            where test.Status.Equals(Status.Failed)
            group test by test.ErrorMessage into testGroup
            orderby testGroup.Count() descending
            select testGroup).Take(Math.Min(5, testResults.Count()));
    }
}
