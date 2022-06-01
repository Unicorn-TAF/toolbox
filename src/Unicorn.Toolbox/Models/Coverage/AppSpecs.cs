using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Unicorn.Toolbox.Models.Coverage
{
    [DataContract]
    public class AppSpecs
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "modules")]
        public List<CoverageModule> Modules { get; set; }
    }
}
