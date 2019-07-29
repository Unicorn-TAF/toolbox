using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Analysis.Filtering
{
    public class AuthorsFilter : ISuitesFilter, ITestsFilter
    {
        private readonly IEnumerable<string> authors;

        public AuthorsFilter(IEnumerable<string> authors)
        {
            this.authors = authors;
        }

        public List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos) =>
            suitesInfos
            .Where(s => s.TestsInfos.Any(t => authors.Contains(t.Author)))
            .ToList();

        public List<TestInfo> FilterTests(List<TestInfo> testInfos) =>
            testInfos
            .Where(t => this.authors.Contains(t.Author))
            .ToList();
    }
}
