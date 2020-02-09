using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Analysis.Filtering
{
    public class FeaturesFilter : ISuitesFilter
    {
        private readonly IEnumerable<string> _features;

        public FeaturesFilter(IEnumerable<string> features)
        {
            _features = features;
        }

        public List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos) =>
            suitesInfos
            .Where(s => s.Features.Intersect(_features).Any())
            .ToList();
    }
}
