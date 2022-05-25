using System.Collections.Generic;

namespace Unicorn.Toolbox.Models.Stats.Filtering
{
    public interface ISuitesFilter
    {
        List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos);
    }
}
