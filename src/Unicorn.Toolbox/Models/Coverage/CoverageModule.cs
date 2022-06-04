using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unicorn.Toolbox.Models.Stats;

namespace Unicorn.Toolbox.Models.Coverage
{
    [DataContract]
    public class CoverageModule : IEqualityComparer<CoverageModule>
    {
        [DataMember(Name = "name")]
        private string name;

        [DataMember(Name = "features")]
        private List<string> features;

        public string Name => name.ToLowerInvariant();

        public IEnumerable<string> Features => features.Select(f => f.ToUpperInvariant());

        public IEnumerable<SuiteInfo> Suites { get; set; }

        public bool Equals(CoverageModule x, CoverageModule y) =>
            x.Name == y.Name;

        public int GetHashCode(CoverageModule obj) =>
            obj.Name.GetHashCode();

        public override string ToString() =>
            $"Module '{Name}'";
    }
}
