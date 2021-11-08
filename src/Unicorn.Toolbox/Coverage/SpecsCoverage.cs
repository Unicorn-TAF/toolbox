using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Unicorn.Toolbox.Analysis;

namespace Unicorn.Toolbox.Coverage
{
    public class SpecsCoverage
    {
        public SpecsCoverage(string jsonFile)
        {
            var formatter = new DataContractJsonSerializer(typeof(AppSpecs));

            using (FileStream fs = new FileStream(jsonFile, FileMode.Open))
            {
                Specs = formatter.ReadObject(fs) as AppSpecs;
            }
        }

        public AppSpecs Specs { get; set; }

        public void Analyze(List<SuiteInfo> suites)
        {
            foreach (var module in Specs.Modules)
            {
                module.Suites = suites.Where(s => s.Tags.Intersect(module.Features).Any()).ToList();
            }
        }
    }
}
