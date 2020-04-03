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

        public Dictionary<string, IEnumerable<TestResult>> FilteredResults { get; protected set; }

        public int MatchingTestsCount { get; protected set; }

        public void FilterTests(IEnumerable<TestResult> testResults)
        {
            var results = _isRegex ?
                testResults.Where(r => r.Status.Equals(Status.Failed) && Regex.IsMatch(r.ErrorMessage, _searchString)) :
                testResults.Where(r => r.Status.Equals(Status.Failed) && r.ErrorMessage.Contains(_searchString));

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
    }
}
