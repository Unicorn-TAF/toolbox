using System.Collections.Generic;

namespace Unicorn.Toolbox.Stats.Filtering
{
    public interface ITestsFilter
    {
        List<TestInfo> FilterTests(List<TestInfo> testInfos);
    }
}
