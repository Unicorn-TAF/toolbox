using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Analysis.Filtering
{
    public class CategoriesFilter : ISuitesFilter, ITestsFilter
    {
        private readonly IEnumerable<string> _categories;

        public CategoriesFilter(IEnumerable<string> categories)
        {
            _categories = categories;
        }

        public List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos) =>
            suitesInfos
            .Where(s => s.TestsInfos.Any(t => _categories.Intersect(t.Categories).Any()))
            .ToList();

        public List<TestInfo> FilterTests(List<TestInfo> testInfos) =>
            testInfos
            .Where(t => _categories.Intersect(t.Categories).Any())
            .ToList();
    }
}
