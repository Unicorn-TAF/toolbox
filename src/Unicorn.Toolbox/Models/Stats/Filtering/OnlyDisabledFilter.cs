using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Models.Stats.Filtering
{
    public class OnlyDisabledFilter : ISuitesFilter, ITestsFilter
    {
        public List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos) =>
            suitesInfos
            .Where(s => s.TestsInfos.Any(t => t.Disabled))
            .ToList();

        public List<TestInfo> FilterTests(List<TestInfo> testInfos) =>
            testInfos
            .Where(t => t.Disabled)
            .ToList();
    }
}
