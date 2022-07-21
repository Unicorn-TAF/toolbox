using System.Collections.Generic;
using System.Linq;

namespace Unicorn.Toolbox.Stats.Filtering
{
    public class TagsFilter : ISuitesFilter
    {
        private readonly IEnumerable<string> _features;

        public TagsFilter(IEnumerable<string> features)
        {
            _features = features;
        }

        public List<SuiteInfo> FilterSuites(List<SuiteInfo> suitesInfos) =>
            suitesInfos
            .Where(s => s.Tags.Intersect(_features).Any())
            .ToList();
    }
}
