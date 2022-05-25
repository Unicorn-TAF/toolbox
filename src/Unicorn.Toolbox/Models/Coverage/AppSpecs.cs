using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Unicorn.Toolbox.Models.Coverage
{
    [DataContract]
    public class AppSpecs
    {
        [DataMember(Name = "name")]
        private string name;

        public string Name => name.ToUpper();

        [DataMember(Name = "modules")]
        public List<CoverageModule> Modules { get; set; }
    }
}
