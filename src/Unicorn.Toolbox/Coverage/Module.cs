using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unicorn.Toolbox.Analysis;

namespace Unicorn.Toolbox.Coverage
{
    [DataContract]
    public class Module
    {
        [DataMember(Name = "name")]
        private string name;

        [DataMember(Name = "features")]
        private List<string> features;

        public string Name => name.ToUpper();

        public List<string> Features => features.Select(f => f.ToUpperInvariant()).ToList();

        public List<SuiteInfo> Suites { get; set; } = new List<SuiteInfo>();

        public bool Covered => this.Suites.Any();

        public override string ToString() =>
            $"Module '{Name}'";
    }
}
