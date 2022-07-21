using System.Collections.Generic;

namespace Unicorn.Toolbox.Stats.Filtering
{
    public interface ISuitesFilter
    {
        List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos);
    }
}
