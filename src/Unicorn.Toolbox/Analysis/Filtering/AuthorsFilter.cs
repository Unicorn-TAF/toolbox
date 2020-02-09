using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Analysis.Filtering
{
    public class AuthorsFilter : ISuitesFilter, ITestsFilter
    {
        private readonly IEnumerable<string> _authors;

        public AuthorsFilter(IEnumerable<string> authors)
        {
            _authors = authors;
        }

        public List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos) =>
            suitesInfos
            .Where(s => s.TestsInfos.Any(t => _authors.Contains(t.Author)))
            .ToList();

        public List<TestInfo> FilterTests(List<TestInfo> testInfos) =>
            testInfos
            .Where(t => _authors.Contains(t.Author))
            .ToList();
    }
}
