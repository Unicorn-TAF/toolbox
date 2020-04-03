using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unicorn.Taf.Core.Testing;

namespace Unicorn.Toolbox.LaunchAnalysis
{
    public class FailedTestsFilter
    {
        private readonly string _searchString;
        private readonly bool _isRegex;

        public FailedTestsFilter(string searchString, bool isRegex)
        {
            _searchString = searchString;
            _isRegex = isRegex;
        }

        public List<FailedSuite> FilteredResults { get; protected set; }

        public int MatchingTestsCount { get; protected set; }

        public void FilterTests(IEnumerable<TestResult> testResults)
        {
            var results = _isRegex ?
                testResults.Where(r => r.Status.Equals(Status.Failed) && Regex.IsMatch(r.ErrorMessage, _searchString)) :
                testResults.Where(r => r.Status.Equals(Status.Failed) && r.ErrorMessage.Contains(_searchString));

            var uniqueSuites = new HashSet<string>(results.Select(r => r.TestListName));

            MatchingTestsCount = 0;
            FilteredResults = new List<FailedSuite>();

            foreach (var suite in uniqueSuites)
            {
                var matchingResults = results.Where(r => r.TestListName.Equals(suite));
                var tests = matchingResults.Select(r => new FailedTest(r.Name, r.ErrorMessage));

                var filteredSuite = new FailedSuite(suite, tests);
                FilteredResults.Add(filteredSuite);
                MatchingTestsCount += tests.Count();
            }
        }
    }

    public struct FailedSuite
    {
        public FailedSuite(string suiteName, IEnumerable<FailedTest> tests)
        {
            SuiteName = suiteName;
            FailedTests = tests.ToList();
        }

        public string SuiteName { get; set; }

        public List<FailedTest> FailedTests { get; set; }
    }

    public struct FailedTest
    {
        public FailedTest(string testName, string errorMessage)
        {
            TestName = testName;
            ErrorMessage = errorMessage;
        }

        public string TestName { get; set; }

        public string ErrorMessage { get; set; }
    }
}
