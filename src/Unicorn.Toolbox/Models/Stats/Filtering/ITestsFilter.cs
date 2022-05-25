using System.Collections.Generic;

namespace Unicorn.Toolbox.Models.Stats.Filtering
{
    public interface ITestsFilter
    {
        List<TestInfo> FilterTests(List<TestInfo> testInfos);
    }
}
